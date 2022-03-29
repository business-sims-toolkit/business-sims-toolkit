using System.Collections.Generic;
using System.IO;

namespace LibCore
{
	public static class Paths
	{
		public static string [] Split (string path)
		{
			List<string> components = new List<string> ();

			foreach (string component in path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
			{
				if (! string.IsNullOrEmpty(component))
				{
					if ((components.Count == 0)
						&& path.StartsWith(@"\\"))
					{
						components.Add(@"\\" + component);
					}
					else
					{
						components.Add(component);
					}
				}
			}

			return components.ToArray();
		}

		public static string Combine (string [] components)
		{
			string path = null;

			foreach (string component in components)
			{
				if (! string.IsNullOrEmpty(component))
				{
					if (string.IsNullOrEmpty(path))
					{
						path = component;
					}
					else if (path.EndsWith(":"))
					{
						path = path + Path.DirectorySeparatorChar + component;
					}
					else
					{
						path = Path.Combine(path, component);
					}
				}
			}

			return path;
		}

		public static string ChangeExtension (string path, string extension)
		{
			string [] components = Split(path);
			components[components.Length - 1] = Path.GetFileNameWithoutExtension(components[components.Length - 1]) + extension;

			return Combine(components);
		}

		public static bool IsChild (string baseFolder, string filename)
		{
			return filename.ToLowerInvariant().StartsWith(baseFolder.ToLowerInvariant());
		}
	}
}