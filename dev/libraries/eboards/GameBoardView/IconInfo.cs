using System.Xml;
using System.Drawing;

using LibCore;

namespace GameBoardView
{
	public class IconInfo
	{
		string name;
		string filename;
		Image image;
		SizeF scale;
		PointF anchor;

		public string Name
		{
			get
			{
				return name;
			}
		}

		public Image Image
		{
			get
			{
				return image;
			}
		}

		public SizeF Scale
		{
			get
			{
				return scale;
			}
		}

		public PointF Anchor
		{
			get
			{
				return anchor;
			}
		}

		public IconInfo (XmlElement root, string imageParentPath)
		{
			if (! string.IsNullOrEmpty(root.GetAttribute("name")))
			{
				name = root.GetAttribute("name");
				filename = root.GetAttribute("filename");
				scale = new SizeF ((float) root.GetDoubleAttribute("x_scale", 1),
				                   (float) root.GetDoubleAttribute("y_scale", 1));
				anchor = new PointF ((float) root.GetDoubleAttribute("x_anchor", 1),
								    (float) root.GetDoubleAttribute("y_anchor", 1));
			}
			else
			{
				name = root.SelectSingleNode("name").InnerText;
				filename = root.SelectSingleNode("filename").InnerText;
				scale = new SizeF ((float) CONVERT.ParseDouble(root.SelectSingleNode("xscale").InnerText),
				                   (float) CONVERT.ParseDouble(root.SelectSingleNode("yscale").InnerText));
				anchor = new PointF ((float) CONVERT.ParseDouble(root.SelectSingleNode("xanchor").InnerText),
				                     (float) CONVERT.ParseDouble(root.SelectSingleNode("yanchor").InnerText));
			}

			image = Repository.TheInstance.GetImage(imageParentPath + filename);
		}

		public XmlElement ToXml (BasicXmlDocument xml)
		{
			XmlElement root = xml.CreateElement("icon");

			root.AppendAttribute("name", name);
			root.AppendAttribute("filename", filename);

			root.AppendAttribute("x_scale", scale.Width);
			root.AppendAttribute("y_scale", scale.Height);

			root.AppendAttribute("x_anchor", anchor.X);
			root.AppendAttribute("y_anchor", anchor.Y);

			return root;
		}
	}
}