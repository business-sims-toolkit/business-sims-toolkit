using System.Drawing;
using System.Xml;

using LibCore;

namespace Charts
{
    public class BlockProperties
    {
        public float Start { get; set; }
        public float End { get; set; }
		public string Legend { get; set; }
		public string SmallLegend { get; set; }

		public HatchFillProperties HatchFillProperties { get; set; } = null;
        public string Pattern { get; set; } = null;
        public Color Colour { get; set; } = Color.Transparent;
	    public Color TextColour { get; set; } = Color.Transparent;
    }
    public class XmlBlock
    {
        public static void AppendBlockChildToElement (XmlElement element, BlockProperties properties)
        {
            var block = element.AppendNewChild("block");

            block.AppendAttribute("start", properties.Start);
            block.AppendAttribute("end", properties.End);
	        block.AppendAttribute("legend", properties.Legend);
	        block.AppendAttribute("small_legend", properties.SmallLegend);

	        if (properties.TextColour != Color.Transparent)
	        {
		        block.AppendAttribute("textcolour", properties.TextColour);
	        }

	        if (properties.HatchFillProperties != null)
            {
                var hatchElement = block.AppendNewChild("hatch_fill");
	            //block.AppendAttribute("colour", properties.Colour);

                hatchElement.AppendAttribute("line_angle", properties.HatchFillProperties.Angle);
                hatchElement.AppendAttribute("line_width", properties.HatchFillProperties.LineWidth);
                hatchElement.AppendAttribute("alt_line_width", properties.HatchFillProperties.AltLineWidth);
                hatchElement.AppendAttribute("line_colour", properties.HatchFillProperties.LineColour);
                hatchElement.AppendAttribute("alt_line_colour", properties.HatchFillProperties.AltLineColour);
            }
            else if (! string.IsNullOrEmpty(properties.Pattern))
            {
                block.AppendAttribute("fill", properties.Pattern);
            }
            else
            {
                block.AppendAttribute("colour", properties.Colour);
            }
        }
    }
}
