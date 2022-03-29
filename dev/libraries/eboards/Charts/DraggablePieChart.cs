using System.Drawing;

namespace Charts
{
	public class DraggablePieChart : PieChart
	{
		public event PieChartSectorDragHandler DragFinished;
		public event PieChartColourChangeHandler ColourChanged;

		public DraggablePieChart ()
		{
		}

		protected override void CreatePieChartControl ()
		{
			DraggablePieChartControl control = new DraggablePieChartControl ();
			pieChartCtrl = control;
			control.DragFinished += control_DragFinished;
			control.ColourChanged += control_ColourChanged;
		}

		void control_ColourChanged (string sectorName, Color colour)
		{
			OnColourChanged(sectorName, colour);
		}

		void OnColourChanged (string sectorName, Color colour)
		{
			if (ColourChanged != null)
			{
				ColourChanged(sectorName, colour);
			}
		}

		void control_DragFinished (int sourceSectorIndex, int destSectorIndex)
		{
			OnDragFinished(sourceSectorIndex, destSectorIndex);
		}

		void OnDragFinished (int sourceSectorIndex, int destSectorIndex)
		{
			if (DragFinished != null)
			{
				DragFinished(sourceSectorIndex, destSectorIndex);
			}
		}
	}
}