using System;
using System.IO;
using System.Reflection;

namespace BaseUtils
{
	/// <summary>
	/// Summary description for UtilFunctions.
	/// </summary>
	public class UtilFunctions
	{
		public UtilFunctions()
		{
		}

		/// <summary>
		/// Returns a stream for the specified
		/// embedded resource.
		/// </summary>
		/// <param name="resource">The fully qualified assembly path to the resource.</param>
		/// <returns>Stream</returns>
		public static Stream GetEmbeddedResource(string resource)
		{
			Assembly asm = Assembly.GetCallingAssembly();
			string st1 = asm.FullName;
			string[] pt = asm.GetManifestResourceNames();
			return asm.GetManifestResourceStream(resource);
		}

		/// <summary>
		/// Parses the specified string into a floating point number taking
		/// into account the regional number format settings.
		/// </summary>
		/// <param name="s">The string to parse.</param>
		/// <returns>float</returns>
		public static float SafeParse(string s)
		{
			double result = 0;
			if (double.TryParse(s, System.Globalization.NumberStyles.Number, System.Globalization.NumberFormatInfo.CurrentInfo, out result))
				return Convert.ToSingle(result);
			else
				return 0f;
		}

	}
}
