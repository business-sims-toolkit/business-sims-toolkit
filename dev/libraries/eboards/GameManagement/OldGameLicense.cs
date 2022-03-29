using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using System.Xml;
using Licensor;

namespace GameManagement
{
	public class OldGameLicense
	{
		string pin;
		string file_name;

		bool locked = false;
		DateTime created;
		Guid couponGuid = Guid.Empty;

		public Guid CouponGuid
		{
			get { return couponGuid; }
		}

		public string GameFilename
		{
			get { return file_name; }
		}

		protected void Setup ()
		{
			pin = "????";
			couponGuid = Guid.Empty;
		}

		protected OldGameLicense ()
		{
			Setup();
		}

		public void ChangeFileName (string new_file_name)
		{
			file_name = new_file_name;
		}

		public XmlElement ToXml (XmlDocument xdoc)
		{
			return ToXml(xdoc, xdoc.DocumentElement);
		}

		public XmlElement ToXml (XmlElement parent)
		{
			return ToXml(parent.OwnerDocument, parent);
		}

		public XmlElement ToXml (XmlDocument xdoc, XmlElement parent)
		{
			XmlElement root = xdoc.CreateElement("lic");

			if (parent == null)
			{
				xdoc.AppendChild(root);
			}
			else
			{
				parent.AppendChild(root);
			}

			CoreUtils.XMLUtils.CreateElementString(root, "pin", pin);
			CoreUtils.XMLUtils.CreateElementBool(root, "locked", locked);
			CoreUtils.XMLUtils.CreateElementString(root, "file_name", file_name);
			CoreUtils.XMLUtils.CreateElementDateTime(root, "created", created);
			CoreUtils.XMLUtils.CreateElementGuid(root, "coupon_guid", couponGuid);

			XmlElement history = CoreUtils.XMLUtils.CreateElement(root, "play_history");

			return root;
		}

		public void Save (GameFile gameFile)
		{
			Save(gameFile, gameFile.Dir);
		}

		public void Save (string dir)
		{
			Save(null, dir);
		}

		public void Save (GameFile gameFile, string dir)
		{
			XmlDocument xdoc = new XmlDocument();
			ToXml(xdoc);

			System.IO.MemoryStream tw = new MemoryStream();
			XmlTextWriter w = new XmlTextWriter(tw, System.Text.Encoding.ASCII);
			xdoc.Save(w);
			w.Flush();
			w.Close();

			byte[] bytOut = tw.GetBuffer();
			System.Text.ASCIIEncoding utf = new ASCIIEncoding();
			char[] charArray = new char[utf.GetCharCount(bytOut, 0, bytOut.Length)];
			utf.GetDecoder().GetChars(bytOut, 0, bytOut.Length, charArray, 0);

			string str = new string(charArray);

			string estr = str;

			string licenseFilename = dir + "\\global\\license.xml";

			using (StreamWriter sw = File.CreateText(licenseFilename))
			{
				sw.Write(estr);
				sw.Flush();
			}
		}

		public static OldGameLicense CreateNewLicense (string users_pin, string fileName)
		{
			OldGameLicense lic = new OldGameLicense();
			lic.pin = users_pin;
			lic.file_name = fileName;
			lic.created = DateTime.Now;
			return lic;
		}

		public static OldGameLicense OpenLicense (string fileName, string gameFileName)
		{
			System.IO.FileInfo myFile = new System.IO.FileInfo(fileName);
			StreamReader myData = myFile.OpenText();
			string data = myData.ReadToEnd();
			myData.Close();
			myData = null;

			string dec = data;

			XmlDocument xdoc = new XmlDocument();
			xdoc.LoadXml(dec);

			OldGameLicense lic = CreateFromXml(xdoc.DocumentElement);

			return lic;
		}

		public static OldGameLicense CreateFromXml (XmlElement xdoc)
		{
			OldGameLicense lic = new OldGameLicense();

			lic.pin = CoreUtils.XMLUtils.GetElementString(xdoc, "pin");
			lic.locked = CoreUtils.XMLUtils.GetElementBool(xdoc, "locked");
			lic.file_name = CoreUtils.XMLUtils.GetElementString(xdoc, "file_name");
			lic.created = CoreUtils.XMLUtils.GetElementDateTime(xdoc, "created");
			lic.couponGuid = CoreUtils.XMLUtils.GetElementGuid(xdoc, "coupon_guid", Guid.Empty);

			return lic;
		}

		public void LinkToCoupon (GameFile gameFile, string tac, Guid guid)
		{
			pin = tac;
			couponGuid = guid;

			Save(gameFile);
		}


		public string Tac
		{
			get { return pin; }
		}

		public DateTime CreatedDate
		{
			get { return created; }
		}

		public DateTime LastPlayableDate
		{
			get { return created.AddDays(7); }
		}
	}
}