using System;
using System.Collections;
using System.Windows.Forms;
using System.Drawing;

namespace Charts
{
	public delegate void PieChartSectorDragHandler (int sourceSectorIndex, int destSectorIndex);
	public delegate void PieChartColourChangeHandler (string sectorName, Color colour);

	internal class DraggablePieChartControl : PieChartControl
	{
		Timer dragTimer;
		int dragTicks;
		bool dragTimerActive;

		int hoverSectorIndex;
		int selectedSectorIndex;

		public event PieChartSectorDragHandler DragFinished;
		public event PieChartColourChangeHandler ColourChanged;

		public DraggablePieChartControl ()
		{
			dragTimer = new Timer ();
			dragTimer.Interval = 100;
			dragTimer.Tick += dragTimer_Tick;
			this.AllowDrop = true;

			hoverSectorIndex = -1;
			selectedSectorIndex = -1;
			dragTimerActive = false;
		}

		protected override void Dispose (bool disposing)
		{
			dragTimer.Dispose();

			base.Dispose(disposing);
		}

		void dragTimer_Tick (object sender, EventArgs e)
		{
			dragTicks++;

			if (dragTicks >= 2)
			{
				BeginDragging();
			}
		}

		void BeginDragging ()
		{
			dragTimer.Stop();
			dragTimerActive = false;

			// Force an update so the cursor and highlight are correct (since we might have
			// already moved the pointer over a valid destination segment by the time the drag
			// gets started).
			UpdateHoverIndexByScreenPosition(Cursor.Position);

			DoDragDrop(selectedSectorIndex, DragDropEffects.Move);
		}

		double NormaliseAngle (double angle)
		{
			while (angle < 0)
			{
				angle += 360;
			}
			while (angle >= 360)
			{
				angle -= 360;
			}

			return angle;
		}

		/// <summary>
		/// Return true iff the given angle lies in the range [min, max], allowing for wrapping
		/// round at 360 degrees.
		/// </summary>
		bool AngleInRange (double angle, double min, double max)
		{
			angle = NormaliseAngle(angle);

			min = NormaliseAngle(min);
			max = NormaliseAngle(max);

			if (min < max)
			{
				return ((angle >= min) && (angle <= max));
			}
			else
			{
				return ((angle >= min) || (angle <= max));
			}
		}

		int GetSectorFromPoint (int x, int y)
		{
			double xOffset = x - (px + prad);
			double yOffset = y - (py + prad);
			double angle = Math.Atan2(yOffset, xOffset) * 360 / (2 * Math.PI);
			double radius = Math.Sqrt((xOffset * xOffset) + (yOffset * yOffset));

			for (int i = 0; i < sectors.Count; i++)
			{
				Sector sector = sectors[i] as Sector;
				if ((radius <= sector.radius) && AngleInRange(angle, sector.angle0, sector.angle1))
				{
					return i;
				}
			}

			return -1;
		}

		protected override void OnMouseDown (MouseEventArgs e)
		{
			base.OnMouseDown(e);

			int oldIndex = selectedSectorIndex;
			selectedSectorIndex = GetSectorFromPoint(e.X, e.Y);
			hoverSectorIndex = -1;
			if (selectedSectorIndex != oldIndex)
			{
				//Refresh();
				Invalidate();
			}

			if (selectedSectorIndex != -1)
			{
				StartDragStartTimer();
			}
		}

		void StartDragStartTimer ()
		{
			dragTicks = 0;
			dragTimer.Start();
			dragTimerActive = true;

			hoverSectorIndex = -1;
		}

		protected override void OnMouseUp (MouseEventArgs e)
		{
			base.OnMouseUp(e);

			StopDragStartTimer();
		}

		protected override void OnMouseMove (MouseEventArgs e)
		{
			base.OnMouseMove(e);

			// If we're waiting to start a drag...
			if (dragTimerActive)
			{
				UpdateHoverIndexByScreenPosition(PointToScreen(new Point (e.X, e.Y)));

				// ...and we've moved to point at another section...
				if (hoverSectorIndex != -1)
				{
					// ...then we have begun the drag.
					BeginDragging();
				}
			}
		}

		Color ColourFromInt (int intColour)
		{
			return Color.FromArgb((intColour >> 0) & 255, (intColour >> 8) & 255, (intColour >> 16) & 255);
		}

		int IntFromColour (Color colour)
		{
			return (colour.R << 0) | (colour.G << 8) | (colour.B << 16);
		}

		protected override void OnDoubleClick (EventArgs e)
		{
			base.OnDoubleClick(e);

			if (selectedSectorIndex != -1)
			{
				ColorDialog dialog = new ColorDialog ();
				dialog.Color = (Color) segmentColours[selectedSectorIndex];
				dialog.FullOpen = true;

				ArrayList colours = new ArrayList ();
				foreach (Color colour in pastelTones)
				{
					if (colours.IndexOf(colour) == -1)
					{
						colours.Add(colour);
					}
				}

				int [] customColours = new int [colours.Count];
				int i = 0;
				foreach (Color colour in colours)
				{
					customColours[i] = IntFromColour(colour);
					i++;
				}
				dialog.CustomColors = customColours;

				if (dialog.ShowDialog(this.FindForm()) == DialogResult.OK)
				{
					OnColourChanged((string) segments[selectedSectorIndex], dialog.Color);

					selectedSectorIndex = -1;
				}
			}
		}

		void OnColourChanged (string sectorName, Color colour)
		{
			if (ColourChanged != null)
			{
				ColourChanged(sectorName, colour);
			}
		}

		void StopDragStartTimer ()
		{
			dragTimer.Stop();
			dragTimerActive = false;

			hoverSectorIndex = -1;
		}

		protected override void OnDragOver (DragEventArgs args)
		{
			base.OnDragOver(args);

			UpdateDrag(args);
		}

		protected override void OnDragEnter (DragEventArgs args)
		{
			base.OnDragEnter(args);

			UpdateDrag(args);
		}

		void UpdateHoverIndexByScreenPosition (Point location)
		{
			int oldHoverIndex = hoverSectorIndex;

			Point point = PointToClient(location);
			hoverSectorIndex = GetSectorFromPoint(point.X, point.Y);

			if (hoverSectorIndex != oldHoverIndex)
			{
				//Refresh();
				Invalidate();
			}
		}

		void UpdateDrag (DragEventArgs args)
		{
			UpdateHoverIndexByScreenPosition(new Point (args.X, args.Y));

			if ((hoverSectorIndex != -1) && (hoverSectorIndex != selectedSectorIndex))
			{
				args.Effect = DragDropEffects.Move;
			}
			else
			{
				args.Effect = DragDropEffects.None;
			}
		}

		protected override void OnDragDrop (DragEventArgs args)
		{
			base.OnDragDrop(args);

			if ((hoverSectorIndex != -1) && (hoverSectorIndex != selectedSectorIndex))
			{
				OnDragFinished(selectedSectorIndex, hoverSectorIndex);
			}
		}

		void OnDragFinished (int sourceSectorIndex, int destSectorIndex)
		{
			if (DragFinished != null)
			{
				DragFinished(sourceSectorIndex, destSectorIndex);
			}
		}

		protected override Color GetSectorColour (int sectorIndex)
		{
			Color colour = base.GetSectorColour(sectorIndex);

			if ((sectorIndex == selectedSectorIndex) || (sectorIndex == hoverSectorIndex))
			{
				colour = Color.FromArgb((colour.R + 255) / 2, (colour.G + 255) / 2, (colour.B + 255) / 2);
			}

			return colour;
		}
	}
}