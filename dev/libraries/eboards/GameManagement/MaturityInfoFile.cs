using System.Collections;
using System.IO;

using zip = ICSharpCode.SharpZipLib;
using LibCore;

namespace GameManagement
{
	public class MaturityInfoFile
	{
		protected string fileName;
		public string FileName
		{
			get
			{
				return fileName;
			}
		}

		protected string tempDirName;

		protected bool _allowSave = true;
		protected bool _allowWriteToDisk = true;

		public bool SaveAllowed
		{
			get
			{
				return _allowSave;
			}
		}

		public bool WriteToDiskAllowed
		{
			get
			{
				return _allowWriteToDisk;
			}
		}

		Hashtable isaveClasses = new Hashtable();

		public virtual void Dispose ()
		{
			if (Directory.Exists(tempDirName))
			{
				Directory.Delete(tempDirName, true);
			}
		}

		public string Dir
		{
			get
			{
				return tempDirName;
			}
		}

		protected MaturityInfoFile ()
		{
		}

		/// <summary>
		/// Create a new game file with specific files defining initial state.
		/// </summary>
		/// <param name="filename"></param>
		/// <param name="roundOneFilesDir"></param>
		/// <param name="isNew"></param>
		public MaturityInfoFile (string filename, bool isNew, bool allowSave, bool allowWriteToDisk)
		{
			_allowWriteToDisk = allowWriteToDisk;
			_allowSave = allowSave;
			fileName = filename;
			tempDirName = Path.GetTempFileName();
			File.Delete(tempDirName);
			DirectoryInfo di = Directory.CreateDirectory(tempDirName);
			//
			if (isNew)
			{
				//CopyDirContents(roundOneFilesDir, tempDirName + "\\round1_B_operations");
				CreateBaseDirs();
				//gameLicense = GameLicense.CreateNewLicense("????", Path.GetFileName(filename) );
			}
			else
			{
				// Unzip the game file to our temp dir.
				ConfPack cp = new ConfPack();
				cp.ExtractAllFilesFromZip(filename, tempDirName, "");//"password");
				//gameLicense = GameLicense.OpenLicense( tempDirName + "\\global\\license.xml", filename );
			}
		}

		public string Name
		{
			get
			{
				return fileName;
			}
		}

		/// <summary>
		/// Open an exisitng game file.
		/// </summary>
		/// <param name="filename"></param>
		public MaturityInfoFile (string filename)
		{
			fileName = filename;

			try
			{
				tempDirName = Path.GetTempFileName();
				File.Delete(tempDirName);
				DirectoryInfo di = Directory.CreateDirectory(tempDirName);
				//
				ConfPack cp = new ConfPack();
				cp.ExtractAllFilesFromZip(fileName, this.tempDirName, "");//"password");

				//GameLicense.OpenLicense( this.Dir + "\\global\\license.xml", filename );
			}
			catch { }
		}

		protected virtual void CreateBaseDirs ()
		{
			string global = this.tempDirName + "\\global";

			if (!Directory.Exists(global))
			{
				Directory.CreateDirectory(global);
			}
		}

		protected void CopyDirContents (string srcdir, string destdirName)
		{
			// if destination directory exists then wipe it.
			if (Directory.Exists(destdirName))
			{
				Directory.Delete(destdirName, true);
			}
			//
			Directory.CreateDirectory(destdirName);
			CopyDir(srcdir, destdirName, false);
		}
		/// <summary>
		/// CopyDir only copies the file contents of a directory, not any sub-directories
		/// unless recursive is set.
		/// </summary>
		/// <param name="src">Source Directory.</param>
		/// <param name="dest">Destination Directory.</param>
		/// <param name="recursive">Sets whether to recursively sopy sub-directories.</param>
		protected void CopyDir (string src, string dest, bool recursive)
		{
			string[] files = Directory.GetFiles(src);
			foreach (string f in files)
			{
				File.Copy(f, dest + "\\" + Path.GetFileName(f));
			}
			//
			if (recursive)
			{
				string[] dirs = Directory.GetDirectories(src);
				char[] dsep = { '\\' };
				//
				foreach (string d in dirs)
				{
					string[] dirTree = d.Split(dsep);
					string newDir = dest + "\\" + dirTree[dirTree.Length - 1];
					Directory.CreateDirectory(newDir);
					CopyDir(d, newDir, true);
				}
			}
		}

		/// <summary>
		/// </summary>
		public string GetFile (string filename)
		{
			return tempDirName + "\\global\\" + filename;
		}

		public void AddDirNames (ArrayList array, string dir)
		{
			string[] dirs = Directory.GetDirectories(dir);
			foreach (string d in dirs)
			{
				array.Add(d);
				AddDirNames(array, d);
			}
		}

		public virtual void Rename (string newName)
		{
			if (this._allowSave)
			{
				Save(true);
				File.Move(fileName, newName);
				fileName = newName;
			}
		}

		public virtual void Save (bool fullSave)
		{
			if (_allowWriteToDisk)
			{
				//this.License.Save(this.tempDirName);

				foreach (ISave c in isaveClasses.Keys)
				{
					string filename = (string) isaveClasses[c];
					c.SaveToURL("", filename);
				}

				if (fullSave && _allowSave)
				{
					ArrayList aDirs = new ArrayList();
					//aDirs.Add(tempDirName);
					AddDirNames(aDirs, tempDirName);
					//
					string[] dirs = (string[]) aDirs.ToArray(typeof(string));
					ConfPack cp = new ConfPack();
					cp.CreateZip(fileName, dirs, false, "");
				}
			}
		}

		public void AddISaver (ISave isClass, string filename)
		{
			isaveClasses.Add(isClass, filename);
		}

		public void RemoveISaver (ISave isClass)
		{
			isaveClasses.Remove(isClass);
		}
	}
}