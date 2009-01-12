//
// Application.cs
//
// Contact:
//   Moonlight List (moonlight-list@lists.ximian.com)
//
// Copyright 2008 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
using Mono;
using Mono.Xaml;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Windows.Controls;
using System.Windows.Resources;
using System.Windows.Interop;
using System.Collections;
using System.Collections.Generic;
using System.Resources;
using System.Windows.Markup;

namespace System.Windows {

	public partial class Application {
		static Application current;
		static Assembly [] assemblies;
		static Assembly startup;
		
		//
		// Controls access to the s_ static fields, which are used
		// by the Application constructor to initialize these fields
		// and any other fields that require initialization by passing
		// data before the derived class executes.
		//
		static object creation = new object ();
		static string s_xap_dir;
		internal static IntPtr s_surface;
		
		//
		// Application instance fields
		//
		string xap_dir;
		IntPtr surface;
		UIElement root_visual;
		SilverlightHost host;

		static Application ()
		{
			ImportXamlNamespace ("clr-namespace:System.Windows;assembly:System.Windows.dll");
			ImportXamlNamespace ("clr-namespace:System.Windows.Controls;assembly:System.Windows.dll");
		}

		public Application ()
		{
			xap_dir = s_xap_dir;
			surface = s_surface;

			if (current == null) {
				current = this;

				if (Host.Source != null) {
					// IsolatedStorage (inside mscorlib.dll) needs some information about the XAP file
					// to initialize it's application and site directory storage.
					AppDomain ad = AppDomain.CurrentDomain;
					ad.SetData ("xap_uri", Host.Source.AbsoluteUri);
					ad.SetData ("xap_host", Host.Source.Host);
				}
			} else {
				root_visual = current.root_visual;
			}
		}

		internal void Terminate ()
		{
			if (xap_dir == null)
				return;

			if (Exit != null)
				Exit (this, EventArgs.Empty);
			
			try {
				Helper.DeleteDirectory (xap_dir);
				xap_dir = null;
			} catch {
			}
		}				


		Dictionary<Assembly, ResourceDictionary> assemblyToGenericXaml = new Dictionary<Assembly, ResourceDictionary>();

		internal Style GetGenericXamlStyleFor (Type type)
		{
			ResourceDictionary rd = null;

			if (assemblyToGenericXaml.ContainsKey (type.Assembly)) {
				rd = assemblyToGenericXaml[type.Assembly];
			}
			else {
				Console.WriteLine ("trying to load: /{0};component/themes/generic.xaml",
						   type.Assembly.GetName().Name);
				StreamResourceInfo info = GetResourceStream (new Uri (string.Format ("/{0};component/themes/generic.xaml",
												     type.Assembly.GetName().Name), UriKind.Relative));
				if (info != null) {
					using (StreamReader sr = new StreamReader (info.Stream)) {
						string generic_xaml = sr.ReadToEnd();

						ManagedXamlLoader loader = new ManagedXamlLoader (surface, PluginHost.Handle);

						try {
							rd = loader.CreateDependencyObjectFromString (generic_xaml, false) as ResourceDictionary;
						}
						catch (Exception e) {
							Console.WriteLine ("failed generic.xaml parsing:");
							Console.WriteLine (e);
						}
					}
				}

				if (rd == null)
					// create an empty one so we don't fall into this block again for this assembly
					rd = new ResourceDictionary();

				assemblyToGenericXaml[type.Assembly] = rd;
			}

			return rd[type.FullName] as Style;
		}

		/// <summary>
		///    Initializes the Application singleton by creating the Application from the XAP file
		/// </summary>
		/// <remarks>
		///   This is consumed by the plugin, should not be used by user code.
		/// </remarks>
		internal static bool LaunchFromXap (IntPtr plugin, IntPtr surface, string xapPath)
		{
			if (current != null)
				throw new Exception ("Should only be called once per AppDomain");

			return CreateFromXap (plugin, surface, xapPath) != null;
		}
		
		internal static Application CreateFromXap (IntPtr plugin, IntPtr surface, string xapPath)
		{			
			if (plugin != IntPtr.Zero)
				PluginHost.SetPluginHandle (plugin);
			
			string xap_dir = NativeMethods.xap_unpack (xapPath);
			if (xap_dir == null){
				Report.Error ("Failure to unpack {0}", xapPath);
				return null;
			}

			//
			// Load AppManifest.xaml, validate a bunch of properties
			//
			ManagedXamlLoader loader = new ManagedXamlLoader (surface, PluginHost.Handle);
			string app_manifest = Path.Combine (xap_dir, "AppManifest.xaml");
			if (!File.Exists (app_manifest)){
				Report.Error ("No AppManifest.xaml found on the XAP package");
				return null;
			}
			
			object result = loader.CreateDependencyObjectFromFile (app_manifest, true);
			if (result == null){
				Report.Error ("Invalid AppManifest.xaml file");
				return null;
			}

			Deployment deployment = (Deployment) result;

			if (deployment.EntryPointAssembly == null) {
				Report.Error ("AppManifest.xaml: No EntryPointAssembly found");
				return null;
			}

			if (deployment.EntryPointType == null) {
				Report.Error ("No entrypoint defined in the AppManifest.xaml");
				return null;
			}

			//
			// Load the XAML to CLR object mappings from this assembly.
			//
			LoadXmlnsDefinitionMappings (typeof (Application).Assembly);
			
			//
			// Load the assemblies from the XAP file, and find the startup assembly
			//
			Assembly entry_point = null;
			assemblies = new Assembly [deployment.Parts.Count + 1];
			assemblies [0] = typeof (Application).Assembly; // Add System.Windows.dll

			for (int i = 0; i < deployment.Parts.Count; i++) {
				var part = deployment.Parts [i];
				try {
					var assembly = Assembly.LoadFrom (Path.Combine (xap_dir, part.Source));

					if (entry_point == null && assembly.GetName ().Name == deployment.EntryPointAssembly)
						entry_point = assembly;

					assemblies [i + 1] = assembly;
					LoadXmlnsDefinitionMappings (assembly);
				} catch (Exception e) {
					Report.Error ("Error while loading the {0} assembly  {1}", part.Source, e);
					return null;
				}
			}

			if (entry_point == null) {
				Report.Error ("Could not find the entry point assembly");
				return null;
			}

			Type entry_type = entry_point.GetType (deployment.EntryPointType);
			if (entry_type == null){
				Report.Error ("Could not find the startup type {0} on the {1}",
					      deployment.EntryPointType, deployment.EntryPointAssembly);
				return null;
			}

			if (!entry_type.IsSubclassOf (typeof (Application))){
				Report.Error ("Startup type does not derive from System.Windows.Application");
				return null;
			}

			Application instance;

			lock (creation){
				s_xap_dir = xap_dir;
				s_surface = surface;
				
				try {
					instance = (Application) Activator.CreateInstance (entry_type);
				} catch (Exception e){
					Report.Error ("Error while creating the instance: {0}", e);
					return null;
				}
			}

			// TODO:
			// Get the event args to pass to startup
			if (instance.Startup != null){
				instance.Startup (instance, new StartupEventArgs ());
			}
			
			if (instance.root_visual != null) {
				NativeMethods.surface_attach (instance.surface, instance.root_visual.native);
			}

			return instance;
		}
		
		internal static Application CreateFromXap (string xapPath)
		{
			return CreateFromXap (IntPtr.Zero, IntPtr.Zero, xapPath);
		}
		
		//
		// component is used as the target type of the object
		// we are loading, makes no sense to me, sounds like a
		// hack.
		//
		[SecuritySafeCritical]
		public static void LoadComponent (object component, Uri resourceLocator)
		{
			Application app = component as Application;
			DependencyObject cdo = component as DependencyObject;
			
			if (cdo == null && app == null)
				throw new ArgumentNullException ("component");

			if (resourceLocator == null)
				throw new ArgumentNullException ("resourceLocator");

			StreamResourceInfo sr = GetResourceStream (resourceLocator);

			// Does not seem to throw.
			if (sr == null)
				return;

			string xaml = new StreamReader (sr.Stream).ReadToEnd ();
			Assembly loading_asm = component.GetType ().Assembly;
			ManagedXamlLoader loader = new ManagedXamlLoader (loading_asm, app != null ? app.surface : Application.s_surface, PluginHost.Handle);
			

			if (cdo != null) {
				// This can throw a System.Exception if the XAML file is invalid.
				
				loader.Hydrate (cdo.native, xaml);
			} else {
				ApplicationInternal temp = new ApplicationInternal ();
				loader.Hydrate (temp.native, xaml);

				// TODO: Copy the important stuff such as Resourcesfrom the temp DO to the app
			}
		}

		/*
		 * Resources take the following format:
		 * 	[/[AssemblyName;component/]]resourcename
		 * They will always be resolved in the following order:
		 * 	1. Application manifest resources
		 * 	2. XAP content
		 */
		public static StreamResourceInfo GetResourceStream (Uri uriResource)
		{
			if (uriResource == null)
				throw new ArgumentNullException ("resourceUri");
			// FIXME: URI must point to
			// - the application assembly (embedded resources)
			// - an assembly part of the application package (embedded resources)
			// - something included in the package
			if (uriResource.IsAbsoluteUri)
				throw new ArgumentException ("resourceUri");

			string loc = uriResource.ToString ();
			string aname;
			string res;
			int p = loc.IndexOf (';');

			/* We have a resource of the format /assembly;component/resourcename */
			if (p > 0) {
				aname = loc.Substring (1, p-1);
				res = loc.Substring (p+11);
			} else {
				aname = startup.GetName ().Name;
				res = loc [0] == '/' ? loc.Substring (1) : loc;	
			}
					
			Assembly assembly = assemblies.FirstOrDefault (a => a.GetName ().Name == aname);
			if (assembly == null)
				return null;

			ResourceManager rm = new ResourceManager (aname + ".g", assembly);

			rm.IgnoreCase = true;
			Stream s = rm.GetStream (res);

			if (s != null)
				return new StreamResourceInfo (s, "");

			if (File.Exists (Path.Combine (Current.xap_dir, res)))
				return StreamResourceInfo.FromFile (Path.Combine (Current.xap_dir, res));

			return null;
		}

		public static StreamResourceInfo GetResourceStream (StreamResourceInfo zipPackageStreamResourceInfo, Uri uriResource)
		{
			if (zipPackageStreamResourceInfo == null)
				throw new ArgumentNullException ("zipPackageStreamResourceInfo");
			if (uriResource == null)
				throw new ArgumentNullException ("resourceUri");

			throw new NotImplementedException ("GetResourceStream-2");
		}

		public static Application Current {
			[SecuritySafeCritical]
			get {
				return current;
			}
		}

		public ResourceDictionary Resources {
			get {
				throw new NotImplementedException ("Resources");
			}
		}

		public UIElement RootVisual {
			get {
				return root_visual;
			}

			set {
				if (value == null)
					throw new InvalidOperationException ();

				// Can only be set once according to the docs.
				if (root_visual != null)
					return;
				
				root_visual = value;
			}
		}

		public SilverlightHost Host {
			get { return host ?? (host = new SilverlightHost ()); }
		}

		public event EventHandler Exit;
		public event StartupEventHandler Startup;
		public event EventHandler<ApplicationUnhandledExceptionEventArgs> UnhandledException;


		internal static Dictionary<XmlnsDefinitionAttribute,Assembly> xmlns_definitions = new Dictionary<XmlnsDefinitionAttribute, Assembly> ();
		internal static List<string> imported_namespaces = new List<string> ();
		
		internal static void LoadXmlnsDefinitionMappings (Assembly a)
		{
			object [] xmlns_defs = a.GetCustomAttributes (typeof (XmlnsDefinitionAttribute), false);

			foreach (XmlnsDefinitionAttribute ns_mapping in xmlns_defs){
				xmlns_definitions [ns_mapping] = a;
			}
		}

		internal static void ImportXamlNamespace (string xmlns)
		{
			imported_namespaces.Add (xmlns);
		}

		internal static Type GetComponentTypeFromName (string name)
		{
			return (from def in xmlns_definitions
				where imported_namespaces.Contains (def.Key.XmlNamespace)
				let clr_namespace = def.Key.ClrNamespace
				let assembly = def.Value
				from type in assembly.GetTypes ()
					where type.Namespace == clr_namespace && type.Name == name /* && type.IsSubclassOf (typeof (DependencyObject)) */
				select type).FirstOrDefault ();
		}

		//
		// Creates the proper component by looking the namespace and name
		// in the various assemblies loaded
		//
		internal static object CreateComponentFromName (string name)
		{
			Type t = GetComponentTypeFromName (name);

			if (t == null) {
				Console.Error.WriteLine ("Application.CreateComponentFromName - could not find type: {0}", name);
				return null;
			}

			return Activator.CreateInstance (t);
		}

		internal static Assembly GetAssembly (string assembly_name)
		{
			return (from def in assemblies where def.GetName ().Name == assembly_name select def).FirstOrDefault ();
		}
	}
}
