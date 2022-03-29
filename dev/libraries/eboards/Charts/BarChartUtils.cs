using System;
using System.Xml;
using System.Drawing;

using LibCore;

namespace Charts
{
	public interface ICategoryCollector
	{
		Category GetCategoryByName (string name);
	}

	public class Category
	{
		public string Name;
		public string Legend;
		public Color Colour;
		public Color TextColour;
		public Color BorderColour;
		public double BorderInset;
		public double BorderThickness;
		public bool ShowInKey;

		public Category (XmlElement xml)
		{
			Name = BasicXmlDocument.GetStringAttribute(xml, "name");
			Colour = CONVERT.ParseComponentColor(BasicXmlDocument.GetStringAttribute(xml, "colour"));

			TextColour = Color.Black;

			if (string.IsNullOrEmpty(BasicXmlDocument.GetStringAttribute(xml, "text_colour"))==false)
			{
				 TextColour = CONVERT.ParseComponentColor(BasicXmlDocument.GetStringAttribute(xml, "text_colour"));
			}

			ShowInKey = BasicXmlDocument.GetBoolAttribute(xml, "show_in_key", true);

			string borderColourAttribute = BasicXmlDocument.GetStringAttribute(xml, "border_colour");
			if (borderColourAttribute != "")
			{
				BorderColour = CONVERT.ParseComponentColor(borderColourAttribute);
			}
			else
			{
				BorderColour = Color.Transparent;
			}

			Legend = BasicXmlDocument.GetStringAttribute(xml, "legend", Name);

			BorderInset = BasicXmlDocument.GetDoubleAttribute(xml, "border_inset", 0);

			BorderThickness = BasicXmlDocument.GetDoubleAttribute(xml, "border_thickness", 0);
		}
	}

	public class HorizontalAxis
	{
		public int Min;
		public int Max;
		public string Legend;
		public bool Visible;
		public StringAlignment LabelAlignment;

		public HorizontalAxis (XmlElement xml)
		{
			Min = BasicXmlDocument.GetIntAttribute(xml, "min", 0);
			Max = BasicXmlDocument.GetIntAttribute(xml, "max", 10);
			Legend = BasicXmlDocument.GetStringAttribute(xml, "legend");
			Visible = BasicXmlDocument.GetBoolAttribute(xml, "visible", true);

			switch (xml.GetStringAttribute("label_alignment", "").ToLowerInvariant())
			{
				case "left":
					LabelAlignment = StringAlignment.Near;
					break;

				case "right":
					LabelAlignment = StringAlignment.Far;
					break;

				default:
					LabelAlignment = StringAlignment.Center;
					break;
			}
		}
	}

	public class VerticalAxis
	{
		public double Min;
		public double Max;
		public double TickInterval;
		public double NumberInterval;
		public string Legend;
		public bool Visible;
		public Color TickColour;

		public VerticalAxis (XmlElement xml)
		{
			Min = BasicXmlDocument.GetDoubleAttribute(xml, "min", 0);
			Max = BasicXmlDocument.GetDoubleAttribute(xml, "max", 10);
			double interval = BasicXmlDocument.GetDoubleAttribute(xml, "interval", 1);
			TickInterval = BasicXmlDocument.GetDoubleAttribute(xml, "tick_interval", interval);
			NumberInterval = BasicXmlDocument.GetDoubleAttribute(xml, "number_interval", interval);
			Legend = BasicXmlDocument.GetStringAttribute(xml, "legend");
			Visible = BasicXmlDocument.GetBoolAttribute(xml, "visible", true);
			TickColour = BasicXmlDocument.GetColourAttribute(xml, "tick_colour", Color.Black);
		}
	}

	public class MouseoverAnnotation
	{
		public string Text;

		public MouseoverAnnotation (XmlElement xml)
		{
			Text = BasicXmlDocument.GetStringAttribute(xml, "text");
		}
	}

	public class Bar
	{
		public Category Category;
		public double Height;
		public string Legend;
	    public bool ShouldDisplayText;
	    public string DisplayText;

		public Bar (ICategoryCollector barChart, XmlElement xml)
		{
			Category = barChart.GetCategoryByName(BasicXmlDocument.GetStringAttribute(xml, "category"));
			Height = BasicXmlDocument.GetDoubleAttribute(xml, "height", 0);
			Legend = BasicXmlDocument.GetStringAttribute(xml, "legend", "");
            ShouldDisplayText = BasicXmlDocument.GetBoolAttribute(xml, "display_height", false);
		    DisplayText = BasicXmlDocument.GetStringAttribute(xml, "display_text", "");
		}
	}

	internal class Line : Bar
	{
		public double Y;
		public bool show_above_line_at_end = false;

		public Line (ICategoryCollector collector, XmlElement xml)
			: base (collector, xml)
		{
			Y = BasicXmlDocument.GetDoubleAttribute(xml, "y", 0.0f);
			show_above_line_at_end = BasicXmlDocument.GetBoolAttribute(xml, "show_above_line", false);
		}
	}

	public static class BarChartUtils
	{
		/// <summary>
		/// Return x + increment, unless that exceeds max, in which case return max,
		/// unless x already is or exceeds max, in which case return max + increment.
		/// </summary>
		public static double CheckedAdvance (double x, double max, double increment)
		{
			if (x >= max)
			{
				return max + increment;
			}

			return Math.Min(max, x + increment);
		}
	}
}