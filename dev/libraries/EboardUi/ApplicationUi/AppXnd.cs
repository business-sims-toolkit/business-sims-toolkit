using System;
using System.Collections.Generic;
using System.IO;
using CoreUtils;
using GameDetails;

namespace ApplicationUi
{
	public static class AppXnd
	{
		public static List<string> UnZipAppXnd (List<string> extractedFiles, ConfPack.ExtractProgressUpdateHandler updateHandler)
		{
			ConfPack confPack = new ConfPack();
			string xndPath = LibCore.AppInfo.TheInstance.InstallLocation + @"\conf.xnd";

			int totalItems = confPack.GetTotalItemsInZip(xndPath, "");

			confPack.ExtractProgressUpdate += delegate (ConfPack sender, ConfPack.ExtractProgressUpdateArgs args)
			{
				updateHandler?.Invoke(sender, new ConfPack.ExtractProgressUpdateArgs (args.ZipFile, args.ItemsHandled, totalItems));
			};

			foreach (var folderLeaf in new [] { "data", "images", "audio", "video" })
			{
				var folderPath = LibCore.AppInfo.TheInstance.Location + @"\\" + folderLeaf;
				if (Directory.Exists(folderPath))
				{
					try
					{
						Directory.Delete(folderPath, true);
					}
					catch
					{
					}
				}
			}

			extractedFiles.AddRange(confPack.ExtractAllFilesFromZip(xndPath, LibCore.AppInfo.TheInstance.Location, ""));

			SkinningDefs.Reload();

			return extractedFiles;
		}
	}
}
