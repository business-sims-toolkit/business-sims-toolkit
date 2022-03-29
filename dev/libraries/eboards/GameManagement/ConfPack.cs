using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ICSharpCode.SharpZipLib.Checksum;
using ICSharpCode.SharpZipLib.Tar;
using ICSharpCode.SharpZipLib.Zip;

namespace GameManagement
{
	public class ConfPack
	{
		public class ExtractProgressUpdateArgs : EventArgs
		{
			public string ZipFile;
			public int ItemsHandled;
			public int TotalItems;

			public ExtractProgressUpdateArgs (string zipFile, int itemsHandled, int totalItems)
			{
				ZipFile = zipFile;
				ItemsHandled = itemsHandled;
				TotalItems = totalItems;
			}
		}

		public delegate void ExtractProgressUpdateHandler (ConfPack sender, ExtractProgressUpdateArgs args);
		public event ExtractProgressUpdateHandler ExtractProgressUpdate;

		public ConfPack()
		{
		}

		public int GetTotalItemsInZip (string zipfile, string password)
		{
			int totalItems = 0;

			using (FileStream stream = File.OpenRead(zipfile))
			{
				using (ZipInputStream s = new ZipInputStream (stream))
				{
					if ("" != password)
					{
						s.Password = password;
					}

					ZipEntry theEntry;
					while ((theEntry = s.GetNextEntry()) != null)
					{
						totalItems++;
					}
				}
			}

			return totalItems;
		}

		/// <summary>
		/// This only works for zipfiles that do not have absolute paths in them!
		/// </summary>
		/// <param name="zipfile"></param>
		/// <param name="outDir"></param>
		/// <returns></returns>
		public List<string> ExtractAllFilesFromZip(string zipfile, string outDir, string password)
		{
			List<string> extractedFiles = new List<string> ();

			try
			{
				int totalItems = GetTotalItemsInZip(zipfile, password);

				using (FileStream stream = File.OpenRead(zipfile))
				{
					using (ZipInputStream s = new ZipInputStream (stream))
					{
						if ("" != password)
						{
							s.Password = password;
						}

						ZipEntry theEntry;
						int itemsHandled = 0;
						while ((theEntry = s.GetNextEntry()) != null)
						{
							OnExtractProgressUpdate(zipfile, itemsHandled, totalItems);
							Console.WriteLine(theEntry.Name);

							string directoryName = Path.GetDirectoryName(theEntry.Name);
							string fileName = Path.GetFileName(theEntry.Name);

							string fullDirectoryName = outDir + @"\" + directoryName;
							Directory.CreateDirectory(fullDirectoryName);

							if (fileName != String.Empty)
							{
								try
								{
									string fullFileName = outDir + @"\" + theEntry.Name;

									using (FileStream streamWriter = File.Create(fullFileName))
									{
										int size = 2048;
										byte [] data = new byte [size];
										while (true)
										{
											size = s.Read(data, 0, data.Length);
											if (size > 0)
											{
												streamWriter.Write(data, 0, size);
											}
											else
											{
												break;
											}
										}
									}

									extractedFiles.Add(fullFileName);
								}
								catch
								{
								}
							}

							itemsHandled++;
						}

						OnExtractProgressUpdate(zipfile, itemsHandled, totalItems);
					}
				}
			}
			catch
			{
			}

			return extractedFiles;
		}

		void OnExtractProgressUpdate (string zipFile, int itemsHandled, int totalItems)
		{
			if (ExtractProgressUpdate != null)
			{
				ExtractProgressUpdate(this, new ExtractProgressUpdateArgs (zipFile, itemsHandled, totalItems));
			}
		}

		public bool CreateTarFromSingleDir(string tarfile, string dir)
		{
			try
			{
				Stream f = File.Create(tarfile);
				TarArchive ta = TarArchive.CreateOutputTarArchive(f);
				ta.RootPath = dir.Replace("\\","/");

				//TarOutputStream s = new TarOutputStream(f);
				//
				string[] filenames = Directory.GetFiles(dir);
				foreach (string file in filenames)
				{
					TarEntry te = TarEntry.CreateEntryFromFile(file);
					//s.PutNextEntry(te);
					ta.WriteEntry(te,false);
				}
				//
				//s.Close();
				ta.Close();
				f.Close();
			}
			catch
			{
				return false;
			}

			return true;
		}

		public bool ExtractTarToDir(string tarfile, string dir)
		{
			TarArchive ta = TarArchive.CreateInputTarArchive( File.OpenRead(tarfile), Encoding.Default );
			ta.ExtractContents(dir);
			ta.Close();

			return true;
		}

		public bool CreateRecursiveZip(string zipfile, string[] dirs, bool flatten, string password)
		{
			try
			{
				Crc32 crc = new Crc32();
				Stream f = File.Create(zipfile);
				ZipOutputStream s = new ZipOutputStream(f);
				if ("" != password)
				{
					s.Password = password;
				}
				s.SetLevel(9); // 0 - store only to 9 - means best compression

				foreach (string dir in dirs)
				{
					System.Console.Out.WriteLine("\t" + dir);
					//
					int slash = dir.LastIndexOf("\\");
					if (slash == -1)
					{
						RecursiveAddDirFilesToZip(s, crc, "", dir, flatten);
					}
					else
					{
						RecursiveAddDirFilesToZip(s, crc, dir.Substring(0, slash), dir.Substring(slash + 1), flatten);
					}
				}

				s.Finish();
				s.Close();
				f.Close();

				return true;
			}
			catch
			{
			}

			return false;
		}

		public bool CreateZip (string zipfile, List<string> filenames, string password)
		{
			Dictionary<string, string> filenameToZippedFilename = new Dictionary<string, string> ();
			foreach (string filename in filenames)
			{
				filenameToZippedFilename.Add(filename, Path.GetFileName(filename));
			}

			return CreateZip(zipfile, filenameToZippedFilename, password);
		}

		public bool CreateZip (string zipFilename, Dictionary<string, string> filenameToZippedFilename, string password)
		{
			Crc32 crc = new Crc32 ();

			try
			{
				using (Stream fileStream = File.Create(zipFilename))
				{
					using (ZipOutputStream zipStream = new ZipOutputStream (fileStream))
					{
						if (! string.IsNullOrEmpty(password))
						{
							zipStream.Password = password;
						}
						zipStream.SetLevel(9);

						foreach (string sourceFilename in filenameToZippedFilename.Keys)
						{
							AddFileToZip(zipStream, crc, filenameToZippedFilename[sourceFilename], sourceFilename);
						}
					}
				}

				return true;
			}
			catch
			{
				return false;
			}
		}

		public bool CreateZip(string zipfile, string[] dirs, bool flatten, string password)
		{
			try
			{
				Crc32 crc = new Crc32();
				Stream f = File.Create(zipfile);
				ZipOutputStream s = new ZipOutputStream(f);
				if("" != password)
				{
					s.Password = password;
				}
				s.SetLevel(9); // 0 - store only to 9 - means best compression

				foreach(string dir in dirs)
				{
					int slash = dir.LastIndexOf("\\");
					if(slash == -1)
					{
						AddDirFilesToZip(s,crc, "", dir, flatten);
					}
					else
					{
						AddDirFilesToZip(s,crc, dir.Substring(0,slash) , dir.Substring(slash+1), flatten );
					}
				}

				s.Finish();
				s.Close();
				f.Close();

				return true;
			}
			catch
			{
			}

			return false;
		}

		void AddDirMatchFilesToZip(ZipOutputStream s, Crc32 crc, string dir, string ext)
		{
			try
			{
				string[] filenames = Directory.GetFiles(dir);

				foreach (string file in filenames)
				{
					if(file.ToLower().EndsWith("."+ext.ToLower()))
					{
						AddFileToZip(s, crc, file, file);
					}
				}
			}
			catch
			{
				// NOP.
			}
		}

		void AddDirFilesToZip(ZipOutputStream s, Crc32 crc, string baseDir, string dir, bool flatten)
		{
			int slen = baseDir.Length;
			int dirLen = dir.Length;

			try
			{
				string[] filenames;
				
				if(slen > 0)
				{
					string dd = baseDir + "\\" + dir;
					if(Directory.Exists(dd))
					{
						filenames = Directory.GetFiles(baseDir + "\\" + dir);
					}
					else return;
				}
				else
				{
// LP 24/07/2007 - Oddly, the following does not _always_ work in dot net 2.0 !!!!
//#if USEDOTNET2
//					filenames = Directory.GetFiles(dir,"*",SearchOption.AllDirectories);
//#else
					filenames = Directory.GetFiles(dir);//,"*");
//#endif
				}

				foreach (string file in filenames)
				{
					if(slen > 0)
					{
						string name = file.Substring(slen + 1);
						if(flatten)
						{
							name = name.Substring(dirLen+1);
						}
						AddFileToZip(s, crc, name, file);
					}
					else
					{
						if(flatten)
						{
							AddFileToZip(s, crc, file.Substring(dirLen+1), file);
						}
						else
						{
							AddFileToZip(s, crc, file, file);
						}
					}
				}

				// Get the directories....
				string[] dirs;

				if (slen > 0)
				{
					dirs = Directory.GetDirectories(baseDir + "\\" + dir);
				}
				else
				{
					dirs = Directory.GetDirectories(dir);
				}

				foreach (string zdir in dirs)
				{
					string zdir2 = zdir.Replace(baseDir,"");
					AddDirFilesToZip(s, crc, baseDir, zdir2, flatten);
				}
			}
			catch
			{
				// NOP.
			}
		}

		void RecursiveAddDirFilesToZip(ZipOutputStream s, Crc32 crc, string baseDir, string dir, bool flatten)
		{
			int slen = baseDir.Length;
			int dirLen = dir.Length;

			System.Console.Out.WriteLine(" Recursive Add : " + baseDir + " : " + dir);

			try
			{
				string[] filenames;

				if (slen > 0)
				{
					string dd = baseDir + "\\" + dir;
					if (Directory.Exists(dd))
					{
						filenames = Directory.GetFiles(baseDir + "\\" + dir);
					}
					else return;
				}
				else
				{
					// LP 24/07/2007 - Oddly, the following does not _always_ work in dot net 2.0 !!!!
					//#if USEDOTNET2
					//					filenames = Directory.GetFiles(dir,"*",SearchOption.AllDirectories);
					//#else
					filenames = Directory.GetFiles(dir);//,"*");
					//#endif
				}

				foreach (string file in filenames)
				{
					if (slen > 0)
					{
						string name = file.Substring(slen + 1);
						if (flatten)
						{
							name = name.Substring(dirLen + 1);
						}
						AddFileToZip(s, crc, name, file);
					}
					else
					{
						if (flatten)
						{
							AddFileToZip(s, crc, file.Substring(dirLen + 1), file);
						}
						else
						{
							AddFileToZip(s, crc, file, file);
						}
					}
				}

				// Get the directories....
				string[] dirs;

				if (slen > 0)
				{
					dirs = Directory.GetDirectories(baseDir + "\\" + dir);
				}
				else
				{
					dirs = Directory.GetDirectories(dir);
				}

				foreach (string zdir in dirs)
				{
					System.Console.Out.WriteLine(" Recursive Dir : " + zdir);

					//string zdir2 = zdir.Replace(baseDir, "");
					AddDirFilesToZip(s, crc, baseDir, zdir, flatten);
				}
			}
			catch
			{
				// NOP.
			}
		}

		public static void ReadWholeArray (Stream stream, byte[] data)
		{
			int offset=0;
			int remaining = data.Length;
			while (remaining > 0)
			{
				int read = stream.Read(data, offset, remaining);
				if (read <= 0)
					throw new EndOfStreamException 
						(String.Format("End of stream reached with {0} bytes left to read", remaining));
				remaining -= read;
				offset += read;
			}
		}

		void AddFileToZip(ZipOutputStream s, Crc32 crc, string name, string file)
		{
			try
			{
				FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			
				byte[] buffer = new byte[fs.Length];
				//fs.Read(buffer, 0, buffer.Length);
				ReadWholeArray(fs, buffer);
				ZipEntry entry = new ZipEntry(name);
			
				entry.DateTime = DateTime.Now;
			
				// set Size and the crc, because the information
				// about the size and crc should be stored in the header
				// if it is not set it is automatically written in the footer.
				// (in this case size == crc == -1 in the header)
				// Some ZIP programs have problems with zip files that don't store
				// the size and crc in the header.
				entry.Size = fs.Length;
				fs.Close();
			
				crc.Reset();
				crc.Update(buffer);
			
				entry.Crc  = crc.Value;
			
				s.PutNextEntry(entry);
			
				s.Write(buffer, 0, buffer.Length);
			}
			catch
			{
				// NOP.
			}
		}

		public bool CreateRecursiveZip_SingleDir(string zipfile, string[] dirs, bool flatten, string password)
		{
			try
			{
				Crc32 crc = new Crc32();
				Stream f = File.Create(zipfile);
				ZipOutputStream s = new ZipOutputStream(f);
				if ("" != password)
				{
					s.Password = password;
				}
				s.SetLevel(9); // 0 - store only to 9 - means best compression

				foreach (string dir in dirs)
				{
					System.Console.Out.WriteLine("\t" + dir);
					//
					int slash = dir.LastIndexOf("\\");
					if (slash == -1)
					{
						RecursiveAddDirFilesToZip_SD(s, crc, "", dir, flatten);
					}
					else
					{
						RecursiveAddDirFilesToZip_SD(s, crc, dir.Substring(0, slash), dir.Substring(slash + 1), flatten);
					}
				}

				s.Finish();
				s.Close();
				f.Close();

				return true;
			}
			catch
			{
			}

			return false;
		}

		/// <summary>
		/// Helper function to recursivly traverse the directory building a list all files
		/// </summary>
		/// <param name="dir"></param>
		/// <param name="fileList"></param>
		private void ExploreDirectory(DirectoryInfo dir, ArrayList fileList)
		{
			// print the directory and the time last accessed
			System.Diagnostics.Debug.WriteLine(" ED : " + dir.Name);

			FileInfo[] files = dir.GetFiles();
			foreach (FileInfo fn in files)
			{
				fileList.Add(fn.FullName);
				System.Diagnostics.Debug.WriteLine("   --" + fn.FullName);
			}

			// get all the directories in the current directory
			// and call this method recursively on each
			DirectoryInfo[] directories = dir.GetDirectories();
			foreach (DirectoryInfo newDir in directories)
			{
				ExploreDirectory(newDir, fileList);
			}
		}

		void RecursiveAddDirFilesToZip_SD(ZipOutputStream s, Crc32 crc, string baseDir, string dir, bool flatten)
		{
			int slen = baseDir.Length;
			int dirLen = dir.Length;
			System.Diagnostics.Debug.WriteLine(" Recursive Add : " + baseDir + " : " + dir);
			string baseFulldirectory = baseDir + @"\" + dir;
			DirectoryInfo di = new DirectoryInfo(baseFulldirectory);

			//Build a full list of all files recursivly 
			ArrayList fileList = new ArrayList();
			ExploreDirectory(di, fileList);
			foreach (string fullname in fileList)
			{
				string relative_name = fullname.Replace(baseFulldirectory + @"\", "");
				System.Diagnostics.Debug.WriteLine(relative_name);
				//add them to the zip, relative to the BaseDir
				AddFileToZip(s, crc, relative_name, fullname);
			}
		}
	}
}