using System.Xml;
using System.Drawing;

using LibCore;

namespace GameBoardView
{
	public class ZoomZone
	{
		string name;
		double left;
		double top;
		double right;
		double bottom;

		public string Name
		{
			get
			{
				return name;
			}
		}

		public ZoomZone (XmlElement root)
		{
			if (! string.IsNullOrEmpty(root.GetAttribute("name")))
			{
				name = root.GetAttribute("name");

				left = root.GetDoubleAttribute("x0", 0);
				right = root.GetDoubleAttribute("x1", 0);
				top = root.GetDoubleAttribute("y0", 0);
				bottom = root.GetDoubleAttribute("y1", 0);
			}
			else
			{
				name = root.SelectSingleNode("name").InnerText;

				left = -CONVERT.ParseDouble(root.SelectSingleNode("x").InnerText)
					     / CONVERT.ParseDouble(root.SelectSingleNode("w").InnerText);
				top = - CONVERT.ParseDouble(root.SelectSingleNode("y").InnerText)
					    / CONVERT.ParseDouble(root.SelectSingleNode("h").InnerText);
				right = left + (1.0 / CONVERT.ParseDouble(root.SelectSingleNode("w").InnerText));
				bottom = top + (1.0 / CONVERT.ParseDouble(root.SelectSingleNode("h").InnerText));
			}
		}

		public RectangleF Bounds
		{
			get
			{
				return new RectangleF ((float) left, (float) top, (float) (right - left), (float) (bottom - top));
			}
		}

		public XmlElement ToXml (BasicXmlDocument xml)
		{
			XmlElement root = xml.CreateElement("zoom");

			root.AppendAttribute("name", name);
			root.AppendAttribute("x0", left);
			root.AppendAttribute("y0", top);
			root.AppendAttribute("x1", right);
			root.AppendAttribute("y1", bottom);

			return root;
		}
	}
}