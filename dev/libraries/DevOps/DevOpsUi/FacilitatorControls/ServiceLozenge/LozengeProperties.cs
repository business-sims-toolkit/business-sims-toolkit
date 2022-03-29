using System;
using System.Collections.Generic;
using System.Drawing;

using ResizingUi;

namespace DevOpsUi.FacilitatorControls.ServiceLozenge
{
	internal struct StatusColours
	{
		public Color ReticuleColour { get; set; }
		public Color ForeColour { get; set; }
		public Color? CircleFillColour { get; set; }
		public Color? CircleOutlineColour { get; set; }

		public HatchFillProperties HatchFillProperties { get; set; }
	}

	//internal struct IconProperties
	//{
	//	public float Width { get; set; }
	//	public float X { get; set; }
	//}

	internal struct MetricProperties
	{
		//public float X { get; set; }
		//public float Width { get; set; }
		public float FontSize { get; set; }
		public string MetricName { get; set; }
		public string Title { get; set; }
		public string DefaultString { get; set; }
		public Func<int, string> FormatterFunc { get; set; }
	}

	internal class PositioningProperties
	{
		public float SidePadding { get; set; }
		public float IconWidth { get; set; }
	}
}
