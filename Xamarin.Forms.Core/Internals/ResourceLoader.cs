using System;
using System.Reflection;

namespace Xamarin.Forms.Internals
{
	public static class ResourceLoader
	{
		static Func<AssemblyName, string, string> resourceProvider;

		//takes a resource path, returns string content
		public static Func<AssemblyName, string, string> ResourceProvider {
			get => resourceProvider;
			internal set {
				resourceProvider = value;
				if (value != null)
					ResourceProvider2 = rlq => new ResourceLoadingResponse { ResourceContent = value(rlq.AssemblyName, rlq.ResourcePath) };
				else
					ResourceProvider2 = null;
			}
		}

		static Func<ResourceLoadingQuery, ResourceLoadingResponse> _resourceProvider2;
		public static Func<ResourceLoadingQuery, ResourceLoadingResponse> ResourceProvider2 {
			get => _resourceProvider2;
			internal set {
				DesignMode.IsDesignModeEnabled = value != null;
				_resourceProvider2 = value;
			}
		}

		public class ResourceLoadingQuery
		{
			public AssemblyName AssemblyName { get; set; }
			public string ResourcePath { get; set; }
		}

		public class ResourceLoadingResponse
		{
			public string ResourceContent { get; set; }
			public bool UseDesignProperties { get; set; }
		}

		internal static Action<Exception> ExceptionHandler { get; set; }
	}
}