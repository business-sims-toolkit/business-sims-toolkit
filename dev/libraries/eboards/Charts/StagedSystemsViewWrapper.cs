using System;
using System.Collections.Generic;
using System.Drawing;

using Events;
using ResizingUi;

namespace Charts
{
	public class StagedSystemsViewWrapper : SharedMouseEventControl
	{
		StagedSystemsView systemsView;

		public StagedSystemsViewWrapper (StagedSystemsView systemsView)
		{
			this.systemsView = systemsView;
			Controls.Add(systemsView);
		}

		public override List<KeyValuePair<string, Rectangle>> BoundIdsToRectangles =>
			new List<KeyValuePair<string, Rectangle>>
			{
				new KeyValuePair<string, Rectangle>("systems", RectangleToScreen(systemsView.Bounds)),
			};

		public override void ReceiveMouseEvent (SharedMouseEventArgs args)
		{
			throw new NotImplementedException();
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			systemsView.Bounds = new Rectangle (0, 0, Width, Height);
		}
	}
}