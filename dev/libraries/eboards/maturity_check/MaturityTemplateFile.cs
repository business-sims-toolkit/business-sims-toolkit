using System.IO;
using GameDetails;
using LibCore;

namespace maturity_check
{
	/// <summary>
	/// </summary>
	public class MaturityTemplateFile : GameManagement.MaturityInfoFile
	{
		/// <summary>
		/// Create a new game file with specific files defining initial state.
		/// </summary>
		/// <param name="filename"></param>
		/// <param name="roundOneFilesDir"></param>
		/// <param name="isNew"></param>
		public MaturityTemplateFile(string template, string filename, bool isNew, bool allowSave, bool allowWriteToDisk)
		{
			_allowWriteToDisk = allowWriteToDisk;
			_allowSave = allowSave;
			fileName = filename;
			tempDirName = Path.GetTempFileName();
			File.Delete(tempDirName);
			DirectoryInfo di = Directory.CreateDirectory(tempDirName);
			//
			if(isNew)
			{
				CreateBaseDirs(template);
			}
			else
			{
				// Unzip the game file to our temp dir.
				ConfPack cp = new ConfPack();
				cp.ExtractAllFilesFromZip(filename,tempDirName, "");
			}
		}

		protected void CreateBaseDirs(string template)
		{
			string base_template_file = AppInfo.TheInstance.InstallLocation + "\\data\\" + template;
			string global = this.tempDirName + "\\global";

			if(!Directory.Exists(global))
			{
				Directory.CreateDirectory(global);
			}

			global += "\\eval_wizard_custom.xml";
			File.Copy(base_template_file, global, true);
		}
	}
}