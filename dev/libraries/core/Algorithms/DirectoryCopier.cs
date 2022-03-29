using System.IO;

namespace Algorithms
{
	public static class DirectoryCopier
	{
		public static void Copy (string source, string destination)
		{
			DirectoryInfo dir = new DirectoryInfo (source);

			foreach (FileInfo file in dir.GetFiles())
			{
				string temppath = Path.Combine(destination, file.Name);
				file.CopyTo(temppath, true);
			}

			foreach (DirectoryInfo subdir in dir.GetDirectories())
			{
				string temppath = Path.Combine(destination, subdir.Name);
				Copy(subdir.FullName, temppath);
			}
		}
	}
}