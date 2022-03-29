using System;
using System.Drawing;
using System.Collections;
using System.IO;
using System.Security.Cryptography;

namespace BaseUtils
{
	/// <summary>
	/// Repository is a singleton handling the loading of Images 
	/// As we load a requested file that is new, we store a copy in the class 
	/// So that we can just serve the local copy on any future requests
	/// It's faster and safer than all code just loading direct.
	/// Version 2 To be done
	/// Implement hash codes loaded from a encrypted build file
	/// This will ensure that images are protected from alteration.
	/// </summary>
	public sealed class Repository
	{
		Hashtable _images;
		Bitmap DummyBmp = new Bitmap(10,10);
		Boolean returnDummy = true;
		Hashtable _imageshash;

		Repository()
		{
			_images = new Hashtable();
			_imageshash = new Hashtable();
		}

		public static readonly Repository TheInstance = new Repository();

		Boolean loadHashInformation()
		{
			//TO BE DONE 
			//Loads a list of image file names and thier hash codes 
			//If we ever load an image whicxh we don't have the hash or the hash is incorrect 
			//then LoadStatusGood == false and return a blank Bitmap 
			//The application can then decide to stop 
			return true;
		}

		bool GetImageFromFile(string file, bool transparent, Color c)
		{
			if(_images.Contains(file)) return true;
			
			try
			{
				System.Diagnostics.Debug.WriteLine("Loading image from file : " + file);
				//Bitmap bmp = new Bitmap(DeviceManager.TheInstance.FileToPath(file));
				Bitmap bmp = new Bitmap(file);
				if(transparent) bmp.MakeTransparent(c);
				Image img = (Image) bmp;
				_images.Add(file,img);
				return true;
			}
			catch(Exception e)
			{
				System.Diagnostics.Debug.WriteLine("Exception loading image from file : " + e.ToString());
				string st = "Repository Image Fail [" + file + "] ##" + e.ToString();
//				LoggerSimple.TheInstance.Error(st);
			}
			return false;
		}

		public Image GetImage(string file, Color transparent)
		{
			if(!GetImageFromFile(file,true,transparent)) 
			{
				if (returnDummy)
				{
					return DummyBmp;
				}
				else
				{
					return null;
				}
			}
			return (Image) _images[file];
		}

		public Image GetImage(string file)
		{
			if(!GetImageFromFile(file,false, Color.Black)) 
			{
				if (returnDummy)
				{
				    _images[file] = DummyBmp;
					return DummyBmp;
				}
				else
				{
					return null;
				}
			}
			return (Image) _images[file];
		}

		public Image GetEncryptedImage(string file)
		{
			return GetEncryptedImage(file, false, Color.Black);
		}

		public Image GetEncryptedImage(string file, bool transparent, Color c)
		{
			if (_images.Contains(file))
			{
				return (Image)_images[file];
			}
				
			try
			{
				System.Diagnostics.Debug.WriteLine("Loading image from file : " + file);
				Bitmap bmp = (Bitmap)ExtractDecryptImageFromFile(file, "v1k1ngr0ck3ll");

				if (transparent) bmp.MakeTransparent(c);
				Image img = (Image)bmp;
				_images.Add(file, img);
				return (Image) _images[file];
			}
			catch (Exception e)
			{
				System.Diagnostics.Debug.WriteLine("Exception loading image from file : " + e.ToString());
				string st = "Repository Image Fail [" + file + "] ##" + e.ToString();
				//				LoggerSimple.TheInstance.Error(st);
			}
			return null;
		}

		Image ExtractDecryptImageFromFile(string src_filename, string password)
		{
			//bool success = false;
			int numRead = 0;
			int fileLength = 0;
			byte[] bCipher = null;
			//byte[] bPlain = null;
			FileStream fileIn = null;
			//FileStream fileOut = null;
			Image bb = null;

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
			//fileOut.Close();
			fileIn.Close();
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
			//byte[] bPlain = null;
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
	}
}