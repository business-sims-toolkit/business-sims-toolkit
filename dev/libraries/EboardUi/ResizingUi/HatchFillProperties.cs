using System;
using System.Diagnostics;
using System.Drawing;
using System.Xml;
using LibCore;

namespace ResizingUi
{
	public class HatchFillProperties
	{
		public HatchFillProperties ()
		{
		}

		public HatchFillProperties (XmlNode node)
		{
			var angle = BasicXmlDocument.GetIntAttribute(node, "line_angle", 0);

			Debug.Assert(Math.Abs(angle) % 45 == 0, "Angle needs to be a multiple of 45");

			Angle = angle;
			LineWidth = BasicXmlDocument.GetFloatAttribute(node, "line_width", 2);
			AltLineWidth = BasicXmlDocument.GetFloatAttribute(node, "alt_line_width", LineWidth);
			LineColour = BasicXmlDocument.GetColourAttribute(node, "line_colour", Color.Black);
			AltLineColour = BasicXmlDocument.GetColourAttribute(node, "alt_line_colour", Color.White);
		}

		public int Angle
		{
			get => angle;
			set
			{
				angle = value;
				AngleInRadians = (float)(angle * Math.PI / 180.0);
			}
		}
		public float AngleInRadians { get; private set; }
		public float LineWidth { get; set; }
		public float AltLineWidth { get; set; }
		public Color LineColour { get; set; }
		public Color AltLineColour { get; set; }

		public float PatternWidth => LineWidth + AltLineWidth;

		int angle;
	}
}
