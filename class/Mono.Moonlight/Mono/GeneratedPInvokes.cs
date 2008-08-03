/* 
	this file was autogenerated. do not edit this file 
 */

using System;
using System.Runtime.InteropServices;

namespace Mono {
	public static partial class NativeMethods
	{
		/* moonplugin methods */
	
	
		/* libmoon methods */
	
		[DllImport ("moon", EntryPoint="dependency_object_get_value_with_error")]
		// Value* dependency_object_get_value_with_error (DependencyObject* instance, Surface* surface, DependencyProperty* property, MoonError* error);
		private extern static IntPtr dependency_object_get_value_with_error_ (IntPtr instance, IntPtr surface, IntPtr property, out MoonError error);
		public static IntPtr dependency_object_get_value (IntPtr instance, IntPtr property)
		{
			MoonError error;
			IntPtr result = dependency_object_get_value_with_error_ (instance, Mono.Xaml.XamlLoader.SurfaceInDomain, property, out error);
			if (error.Number != 0)
				throw CreateManagedException (error);
			return result;
		}

		[DllImport ("moon", EntryPoint="dependency_object_get_default_value_with_error")]
		// Value* dependency_object_get_default_value_with_error (DependencyObject* instance, Surface* surface, DependencyProperty* property, MoonError* error);
		private extern static IntPtr dependency_object_get_default_value_with_error_ (IntPtr instance, IntPtr surface, IntPtr property, out MoonError error);
		public static IntPtr dependency_object_get_default_value (IntPtr instance, IntPtr property)
		{
			MoonError error;
			IntPtr result = dependency_object_get_default_value_with_error_ (instance, Mono.Xaml.XamlLoader.SurfaceInDomain, property, out error);
			if (error.Number != 0)
				throw CreateManagedException (error);
			return result;
		}

		[DllImport ("moon", EntryPoint="dependency_object_get_value_no_default_with_error")]
		// Value* dependency_object_get_value_no_default_with_error (DependencyObject* instance, Surface* surface, DependencyProperty* property, MoonError* error);
		private extern static IntPtr dependency_object_get_value_no_default_with_error_ (IntPtr instance, IntPtr surface, IntPtr property, out MoonError error);
		public static IntPtr dependency_object_get_value_no_default (IntPtr instance, IntPtr property)
		{
			MoonError error;
			IntPtr result = dependency_object_get_value_no_default_with_error_ (instance, Mono.Xaml.XamlLoader.SurfaceInDomain, property, out error);
			if (error.Number != 0)
				throw CreateManagedException (error);
			return result;
		}

		// This method is already defined manually in NativeMethods.cs. Remove the import from there, and regenerate.
		// [DllImport ("moon", EntryPoint="dependency_object_get_name")]
		// const char* dependency_object_get_name (DependencyObject* instance);
		// private extern static IntPtr dependency_object_get_name_ (IntPtr instance);
		// public static string dependency_object_get_name (IntPtr instance)
		// {
		// 	IntPtr p = dependency_object_get_name_  (instance);
		// 	return p == IntPtr.Zero ? null : Marshal.PtrToStringAnsi (p);
		// }

		[DllImport ("moon", EntryPoint="dependency_property_get_name")]
		// const char* dependency_property_get_name (DependencyProperty* instance);
		private extern static IntPtr dependency_property_get_name_ (IntPtr instance);
		public static string dependency_property_get_name (IntPtr instance)
		{
			IntPtr p = dependency_property_get_name_  (instance);
			return p == IntPtr.Zero ? null : Marshal.PtrToStringAnsi (p);
		}

		[DllImport ("moon")]
		// Type::Kind dependency_property_get_property_type (DependencyProperty* instance);
		public extern static Kind dependency_property_get_property_type (IntPtr instance);

		[DllImport ("moon")]
		// bool dependency_property_is_nullable (DependencyProperty* instance);
		public extern static bool dependency_property_is_nullable (IntPtr instance);

		[DllImport ("moon")]
		// DependencyProperty* dependency_property_register_full (Surface* surface, Type::Kind type, const char* name, Value* default_value, Type::Kind vtype, bool attached, bool read_only, bool always_change, NativePropertyChangedHandler* changed_callback);
		public extern static IntPtr dependency_property_register_full (IntPtr surface, Kind type, string name, IntPtr default_value, Kind vtype, bool attached, bool read_only, bool always_change, Mono.NativePropertyChangedHandler changed_callback);

		[DllImport ("moon")]
		// DependencyProperty* dependency_property_get_dependency_property (Type::Kind type, const char* name);
		public extern static IntPtr dependency_property_get_dependency_property (Kind type, string name);

		[DllImport ("moon")]
		// DependencyProperty *dependency_property_register_managed_property (Surface *surface, const char *name, Type::Kind property_type, Type::Kind owner_type, bool attached, NativePropertyChangedHandler *callback);
		public extern static IntPtr dependency_property_register_managed_property (IntPtr surface, string name, Kind property_type, Kind owner_type, bool attached, Mono.NativePropertyChangedHandler callback);

	}
}
