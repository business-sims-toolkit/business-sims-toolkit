using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;

namespace LibCore
{
	/// <summary>
	/// Repository is a singleton that prevents the application replicating
	/// loaded data from multiple sources.
	/// </summary>
	public sealed class Repository
	{
		//
		// We place the AI component in here becuase we are only allowed one
		// and this is a singleton so it's _the_ place for it until we adapt
		// the code to allow multiple AI engines in a single process.
		//
		Hashtable _images;

		Repository()
		{
			_images = new Hashtable();
		}

		public static readonly Repository TheInstance = new Repository();

		bool GetImageFromFile(string file, bool transparent, Color c)
		{
			if(_images.ContainsKey(file)) return true;

			if (! File.Exists(file))
			{
				return false;
			}

			using (Bitmap bmp = new Bitmap(file))
			{
				if (transparent) bmp.MakeTransparent(c);

				var copy = new Bitmap(bmp);

				_images.Add(file, copy);
			}

			return true;
		}

		public Bitmap GetImage(string file, Color transparent)
		{
			if(!GetImageFromFile(file,true,transparent)) return null;

			return (Bitmap) _images[file];
		}

		public Bitmap GetImage (string file)
		{
			if (! GetImageFromFile(file, false, Color.Black))
			{
				file = Paths.ChangeExtension(file, ".png");
			}

			if (! GetImageFromFile(file, false, Color.Black))
			{
				file = Paths.ChangeExtension(file, ".jpg");
			}

			if (! GetImageFromFile(file, false, Color.Black))
			{
				file = Paths.ChangeExtension(file, ".jpeg");
			}

			if (! GetImageFromFile(file, false, Color.Black))
			{
				return null;
			}

			return (Bitmap) _images[file];
		}

		public Bitmap GetEncryptedImage (string file)
		{
			return GetEncryptedImage(file, false, Color.Black);
		}

		public Bitmap GetEncryptedImage (string file, bool transparent, Color c)
		{
			if (_images.Contains(file))
			{
				return (Bitmap) _images[file];
			}

			try
			{
				System.Diagnostics.Debug.WriteLine("Loading image from file : " + file);
				Bitmap bmp = (Bitmap)ExtractDecryptImageFromFile(file, @"h00ray");

				if (transparent) bmp.MakeTransparent(c);
				_images.Add(file, bmp);
				return (Bitmap) _images[file];
			}
			catch (Exception e)
			{
				System.Diagnostics.Debug.WriteLine("Exception loading image from file : " + e.ToString());
				string st = "Repository Image Fail [" + file + "] ##" + e.ToString();
				//				LoggerSimple.TheInstance.Error(st);
			}
			return null;
		}

		Bitmap ExtractDecryptImageFromFile (string src_filename, string password)
		{
			int numRead = 0;
			int fileLength = 0;
			byte[] bCipher = null;
			FileStream fileIn = null;
			Bitmap bb = null;

			//fileOut = new FileStream(dest_filename, System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.Write);
			fileIn = new FileStream(src_filename, System.IO.FileMode.Open, System.IO.FileAccess.Read);
			fileLength = (int)fileIn.Length;
			//Extract the Source Information into a byte array
			if (fileLength > 0)
			{
				bCipher = new byte[fileLength];
				do
				{
					numRead += fileIn.Read(bCipher, 0, fileLength);
				} while (numRead < fileLength);
			}
			fileIn.Close();  //close input file stream


			// In debug mode only, allow unencrypted files too.
#if DEBUG
			try
			{
#endif
				string result = String.Empty;
				PasswordDeriveBytes key = new PasswordDeriveBytes(password, null);

				using (RijndaelManaged cipher = new RijndaelManaged())
				using (ICryptoTransform enc = cipher.CreateDecryptor(key.GetBytes(32), key.GetBytes(16)))
				using (MemoryStream inStream = new MemoryStream(bCipher))
				using (CryptoStream cryptStream = new CryptoStream(inStream, enc, CryptoStreamMode.Read))
				{
					//byte[] bytes = new byte[bCipher.Length];
					bb = new Bitmap(cryptStream);
					//int length = cryptStream.Read(bytes, 0, bytes.Length);
					cryptStream.Close();
					inStream.Close();
					//fileOut.Write(bytes, 0, bytes.Length);
				}
#if DEBUG
			}
			catch
			{
				bb = new Bitmap (src_filename);
			}
#endif

			return bb;
		}

		public bool encryptbinaryfile(string src_filename, string dest_filename, string password)
		{
			bool success = false;
			int numRead = 0;
			int fileLength = 0;
			byte[] bText = null;
			byte[] bCipher = null;
			FileStream fileIn = null;
			FileStream fileOut = null;


			fileOut = new FileStream(dest_filename, System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.Write);
			fileIn = new FileStream(src_filename, System.IO.FileMode.Open, System.IO.FileAccess.Read);
			fileLength = (int)fileIn.Length;

			//Extract the Source Information into a byte array
			if (fileLength > 0)
			{
				bText = new byte[fileLength];
				do
				{
					numRead += fileIn.Read(bText, 0, fileLength);
				} while (numRead < fileLength);
			}
			fileIn.Close();  //close input file stream

			//Encrypt the 

			PasswordDeriveBytes key = new PasswordDeriveBytes(password, null);
			using (RijndaelManaged cipher = new RijndaelManaged())
			using (ICryptoTransform enc = cipher.CreateEncryptor(key.GetBytes(32), key.GetBytes(16)))
			using (MemoryStream outStream = new MemoryStream())
			using (CryptoStream cryptStream = new CryptoStream(outStream, enc, CryptoStreamMode.Write))
			{
				cryptStream.Write(bText, 0, bText.Length);
				cryptStream.FlushFinalBlock();
				cryptStream.Close();
				outStream.Close();
				bCipher = outStream.ToArray();
				fileOut.Write(bCipher, 0, bCipher.Length);
				success = true;
			}
			fileOut.Close();
			fileIn.Close();

			return success;
		}

		public static bool decryptbinaryfile(string src_filename, string dest_filename, string password)
		{
			bool success = false;
			int numRead = 0;
			int fileLength = 0;
			byte[] bCipher = null;
			FileStream fileIn = null;
			FileStream fileOut = null;

			fileOut = new FileStream(dest_filename, System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.Write);
			fileIn = new FileStream(src_filename, System.IO.FileMode.Open, System.IO.FileAccess.Read);
			fileLength = (int)fileIn.Length;


			//Extract the Source Information into a byte array
			if (fileLength > 0)
			{
				bCipher = new byte[fileLength];
				do
				{
					numRead += fileIn.Read(bCipher, 0, fileLength);
				} while (numRead < fileLength);
			}
			fileIn.Close();  //close input file stream


			string result = String.Empty;
			PasswordDeriveBytes key = new PasswordDeriveBytes(password, null);

			using (RijndaelManaged cipher = new RijndaelManaged())
			using (ICryptoTransform enc = cipher.CreateDecryptor(key.GetBytes(32), key.GetBytes(16)))
			using (MemoryStream inStream = new MemoryStream(bCipher))
			using (CryptoStream cryptStream = new CryptoStream(inStream, enc, CryptoStreamMode.Read))
			{
				byte[] bytes = new byte[bCipher.Length];
				int length = cryptStream.Read(bytes, 0, bytes.Length);
				cryptStream.Close();
				inStream.Close();
				fileOut.Write(bytes, 0, bytes.Length);
			}
			fileOut.Close();
			fileIn.Close();
			return success;
		}

		public void DisposeImages ()
		{
			foreach (Bitmap image in _images.Values)
			{
				image.Dispose();
			}

			_images.Clear();
		}

		public void RemoveImage (string filename)
		{
			if (_images.ContainsKey(filename))
			{
				((Image) _images[filename]).Dispose();
				_images.Remove(filename);
			}
		}
	}
}