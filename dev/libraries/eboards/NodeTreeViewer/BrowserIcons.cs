using System.Collections;
using System.Drawing;
using System.Xml;
using System.IO;

using LibCore;

namespace NodeTreeViewer
{
	/// <summary>
	/// Summary description for BrowserIcons.
	/// </summary>
	public sealed class BrowserIcons
	{
		public static readonly BrowserIcons TheInstance = new BrowserIcons();

		Hashtable icons = new Hashtable();

		public Image GetImage(string type)
		{
			if(icons.ContainsKey(type))
			{
				return (Image) icons[type];
			}
			return null;
		}

		BrowserIcons()
		{
			string thefile = AppInfo.TheInstance.Location + "\\data\\browser.xml";

			if(File.Exists(thefile))
			{
				System.IO.StreamReader file = new System.IO.StreamReader(thefile);
				string xmldata = file.ReadToEnd();
				file.Close();
				file = null;

				XmlDocument xdoc = new XmlDocument();
				xdoc.LoadXml(xmldata);

				foreach(XmlNode n in xdoc.DocumentElement.ChildNodes)
				{
					if(n.NodeType == XmlNodeType.Element)
					{
						string type = CoreUtils.XMLUtils.GetElementString(n,"type");
						string icon = CoreUtils.XMLUtils.GetElementString(n,"icon");

						string iconfile = AppInfo.TheInstance.Location + "\\" + icon;

						if(File.Exists(iconfile))
						{
							icons[type] = Repository.TheInstance.GetImage(iconfile);
						}
					}
				}
			}
		}
	}
}
