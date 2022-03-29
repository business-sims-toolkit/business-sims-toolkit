using System;
using System.Collections.Generic;
using System.Drawing;

using Events;
using ResizingUi;

namespace Charts
{
	public class PieChartWrapper : SharedMouseEventControl
	{
		public PieChartWrapper(PieChart pieChart)
		{
			this.pieChart = pieChart;
			Controls.Add(pieChart);
		}

		public override List<KeyValuePair<string, Rectangle>> BoundIdsToRectangles =>
			new List<KeyValuePair<string, Rectangle>>
			{
				new KeyValuePair<string, Rectangle>("piechart", RectangleToScreen(pieChart.Bounds))
			};

		public override void ReceiveMouseEvent(SharedMouseEventArgs args)
		{
			throw new NotImplementedException();
		}

		protected override void OnSizeChanged(EventArgs e)
		{
			pieChart.Bounds = new Rectangle(new Point(0, 0), Size);
		}

		readonly PieChart pieChart;
	}
}
