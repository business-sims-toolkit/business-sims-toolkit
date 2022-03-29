using System.Xml;
using System.Drawing;

using LibCore;

namespace GameBoardView
{
	public class LocationInfo
	{
		string name;
		PointF location;
		string defaultType;

		public string Name
		{
			get
			{
				return name;
			}
		}

		public PointF Location
		{
			get
			{
				return location;
			}

			set
			{
				location = value;
			}
		}

		public string DefaultType
		{
			get
			{
				return defaultType;
			}
		}

		public LocationInfo (XmlElement root)
		{
			if (! string.IsNullOrEmpty(root.GetAttribute("name")))
			{
				name = root.GetAttribute("name");

				location = new PointF ((float) root.GetDoubleAttribute("x", 0),
				                       (float) root.GetDoubleAttribute("y", 0));

				defaultType = root.GetAttribute("default_icon_type");
			}
			else
			{
				name = root.SelectSingleNode("name").InnerText;
				location = new PointF ((float) CONVERT.ParseDouble(root.SelectSingleNode("x").InnerText),
				                       (float) CONVERT.ParseDouble(root.SelectSingleNode("y").InnerText));
				defaultType = root.SelectSingleNode("deftype").InnerText;
			}
		}

		public void ApplyOffset (double x, double y)
		{
			location = new PointF ((float) (location.X + x), (float) (location.Y + y));
		}

		public void Move (double scale, PointF amount)
		{
			ApplyOffset(scale * amount.X, scale * amount.Y);
		}

		public XmlElement ToXml (BasicXmlDocument xml)
		{
			XmlElement root = xml.CreateElement("location");

			root.AppendAttribute("name", name);

			root.AppendAttribute("x", location.X);
			root.AppendAttribute("y", location.Y);

			root.AppendAttribute("default_icon_type", defaultType);

			return root;
		}
	}
}