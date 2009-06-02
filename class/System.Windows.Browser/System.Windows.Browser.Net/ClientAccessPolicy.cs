//
// ClientAccessPolicy.cs
//
// Authors:
//	Atsushi Enomoto <atsushi@ximian.com>
//	Moonlight List (moonlight-list@lists.ximian.com)
//
// Copyright (C) 2009 Novell, Inc.  http://www.novell.com
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

#if NET_2_1

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace System.Windows.Browser.Net {

	partial class ClientAccessPolicy : BaseDomainPolicy {

		public class AccessPolicy {

			public const short MinPort = 4502;
			public const short MaxPort = 4534;

			public List<AllowFrom> AllowedServices { get; private set; }
			public List<GrantTo> GrantedResources { get; private set; }
			public long PortMask { get; set; }

			public AccessPolicy ()
			{
				AllowedServices = new List<AllowFrom> ();
				GrantedResources = new List<GrantTo> ();
			}

			public bool PortAllowed (int port)
			{
				if ((port < MinPort) || (port > MaxPort))
					return false;

				return (((PortMask >> (port - MinPort)) & 1) == 1);
			}
		}

		public ClientAccessPolicy ()
		{
			AccessPolicyList = new List<AccessPolicy> ();
		}

		public List<AccessPolicy> AccessPolicyList { get; private set; }

		public bool IsAllowed (IPEndPoint endpoint)
		{
			foreach (AccessPolicy policy in AccessPolicyList) {
				// does something allow our URI in this policy ?
				foreach (AllowFrom af in policy.AllowedServices) {
					if (af.IsAllowed (ApplicationUri, null)) {
						// if so, is our request port allowed ?
						if (policy.PortAllowed (endpoint.Port))
							return true;
					}
				}
			}
			// no policy allows this socket connection
			return false;
		}

		public override bool IsAllowed (Uri uri, params string [] headerKeys)
		{
			// is a policy applies then we're not allowed to use './' or '../' inside the URI
			if (uri.OriginalString.Contains ("./"))
				return false;

			foreach (AccessPolicy policy in AccessPolicyList) {
				// does something allow our URI in this policy ?
				foreach (AllowFrom af in policy.AllowedServices) {
					// is the application (XAP) URI allowed by the policy ?
					if (af.IsAllowed (ApplicationUri, headerKeys)) {
						foreach (GrantTo gt in policy.GrantedResources) {
							// is the requested access to the Uri granted under this policy ?
							if (gt.IsGranted (uri))
								return true;
						}
					}
				}
			}
			// no policy allows this web connection
			return false;
		}

		public class AllowFrom {

			public AllowFrom ()
			{
				Domains = new List<Uri> ();
				HttpRequestHeaders = new Headers ();
				Scheme = String.Empty;
			}

			public bool AllowAllHeaders { get; private set; }

			public bool AllowAnyDomain { get; set; }

			public List<Uri> Domains { get; private set; }

			public Headers HttpRequestHeaders { get; private set; }

			public string Scheme { get; internal set; }

			public bool IsAllowed (Uri uri, string [] headerKeys)
			{
				// check headers
				if (!HttpRequestHeaders.IsAllowed (headerKeys))
					return false;

				// check scheme
				if ((Scheme.Length > 0) && (Scheme == uri.Scheme)) {
					switch (Scheme) {
					case "http":
						return (uri.Port == 80);
					case "https":
						return (uri.Port == 443);
					case "file":
						return true;
					default:
						return false;
					}
				}
				// check domains
				if (AllowAnyDomain)
					return true;
				if (Domains.All (domain => domain.DnsSafeHost != uri.DnsSafeHost))
					return false;
				return true;
			}
		}

		public class GrantTo
		{
			public GrantTo ()
			{
				Resources = new List<Resource> ();
			}

			public List<Resource> Resources { get; private set; }

			public bool IsGranted (Uri uri)
			{
				foreach (var gr in Resources) {
					if (gr.IncludeSubpaths) {
						if (uri.LocalPath.StartsWith (gr.Path, StringComparison.Ordinal))
							return true;
					} else {
						if (uri.LocalPath == gr.Path)
							return true;
					}
				}
				return false;
			}
		}

		public class Resource
		{
			public string Path { get; set; }
			public bool IncludeSubpaths { get; set; }
		}
	}
}

#endif

