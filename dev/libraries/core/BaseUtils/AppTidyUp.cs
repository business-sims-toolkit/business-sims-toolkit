using System.Collections.Generic;
using System.IO;

namespace BaseUtils
{
	public static class AppTidyUp
	{
		public static void DeleteUnzippedFiles (ICollection<string> extractedFiles)
		{
			try
			{
				LibCore.Repository.TheInstance.DisposeImages();
			}
			catch
			{
			}

			try
			{
				BaseUtils.Repository.TheInstance.DisposeImages();
			}
			catch
			{
			}

			if (extractedFiles != null)
			{
				foreach (string file in extractedFiles)
				{
					if (File.Exists(file))
					{
						try
						{
							string folder = Path.GetDirectoryName(file);

							File.Delete(file);

							DeleteEmptyFolderChainUpwards(folder);
						}
						catch
						{
						}
					}
				}
			}
		}

		static void DeleteEmptyFolderChainUpwards (string folder)
		{
			try
			{
				while ((! string.IsNullOrEmpty(folder)) && (Directory.GetFileSystemEntries(folder).Length == 0))
				{
					Directory.Delete(folder);

					folder = Path.GetDirectoryName(folder);
				}
			}
			catch
			{
			}
		}
	}
}