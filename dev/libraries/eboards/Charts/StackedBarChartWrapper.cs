using System;
using System.Collections.Generic;
using System.Drawing;

using Events;
using ResizingUi;

namespace Charts
{
	public class StackedBarChartWrapper : SharedMouseEventControl
	{
		StackedBarChart barChart;

		public StackedBarChartWrapper (StackedBarChart barChart)
		{
			this.barChart = barChart;
			Controls.Add(barChart);
		}

		public override List<KeyValuePair<string, Rectangle>> BoundIdsToRectangles =>
			new List<KeyValuePair<string, Rectangle>>
			{
				new KeyValuePair<string, Rectangle>("chart", RectangleToScreen(barChart.Bounds)),
			};

		public override void ReceiveMouseEvent (SharedMouseEventArgs args)
		{
			throw new NotImplementedException();
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			barChart.Bounds = new Rectangle (0, 0, Width, Height);
		}
	}
}