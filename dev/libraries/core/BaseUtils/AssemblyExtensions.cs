using System.IO;
using System.Reflection;

namespace BaseUtils
{
	public static class AssemblyExtensions
	{
		public static string GetTitle (this Assembly assembly)
		{
			object [] attributes = assembly.GetCustomAttributes(typeof (AssemblyTitleAttribute), false);
			if (attributes.Length > 0)
			{
				System.Reflection.AssemblyTitleAttribute titleAttribute = (AssemblyTitleAttribute) attributes[0];
				if (! string.IsNullOrEmpty(titleAttribute.Title))
				{
					return titleAttribute.Title;
				}
			}

			return Path.GetFileNameWithoutExtension(assembly.CodeBase);
		}

		public static string MainAssemblyTitle
		{
			get
			{
				return GetTitle(Assembly.GetEntryAssembly());
			}
		}
	}
}