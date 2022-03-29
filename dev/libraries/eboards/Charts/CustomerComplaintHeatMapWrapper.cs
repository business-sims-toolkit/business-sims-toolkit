using System;
using System.Collections.Generic;
using System.Drawing;

using Events;
using ResizingUi;

namespace Charts
{
	public class CustomerComplaintHeatMapWrapper : SharedMouseEventControl
	{
		CustomerComplaintHeatMap heatMap;

		public CustomerComplaintHeatMapWrapper (CustomerComplaintHeatMap heatMap)
		{
			this.heatMap = heatMap;
			Controls.Add(heatMap);
		}

		public override List<KeyValuePair<string, Rectangle>> BoundIdsToRectangles =>
			new List<KeyValuePair<string, Rectangle>>
			{
				new KeyValuePair<string, Rectangle>("heatmap", RectangleToScreen(heatMap.Bounds))
			};

		public override void ReceiveMouseEvent (SharedMouseEventArgs args)
		{
			throw new NotImplementedException();
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			heatMap.Bounds = new Rectangle(0, 0, Width, Height);
		}
	}
}