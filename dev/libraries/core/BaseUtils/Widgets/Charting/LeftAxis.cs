using System;
using System.Drawing;

namespace BaseUtils
{
	/// <summary>
	/// Extends Axis to implement a Left Axis.
	/// </summary>
	public class LeftAxis : Axis
	{
		int divider = 1000;
		Boolean UseDivider = false;
		Boolean showNonIntervalTicks = true;

		/// <summary>
		/// Creates an instance of LeftAxis.
		/// </summary>
		/// <param name="container"></param>
		public LeftAxis(ChartContainer container) : base(container)
		{
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="DividerStatus"></param>
		public void setNonIntervalTickVisibility(bool newVis)
		{
			showNonIntervalTicks = newVis; 
		}

		/// <summary>
		/// This divides the display values of the major Intervals
		/// </summary>
		/// <param name="DividerStatus"></param>
		public void setDividerhack(Boolean DividerStatus)
		{
			UseDivider = DividerStatus;
		}

		/// <summary>
		/// Draws the Left Axis.
		/// </summary>
		public override void Draw()
		{
			if (!DrawAxis)
				return;

            if (MajorInterval == 0)
            {
                return;
            }

			// Draw the label
			SizeF size = Container.MeasureString(this.Label, Container.CurrentFont);
			Container.CurrentBrush = new SolidBrush(this.LabelColor);
			Container.DrawVerticalText(Container.Location.X, Container.PlotBounds.Y + ((Container.PlotBounds.Height - size.Width) / 2), this.Label);
			
			// Start at the bottom and work up
			float y = Container.Location.Y + Container.PadTop + Container.PlotHeight;
			int tick = MinValue;
			int displaytick = tick;


			while (y >= Container.Location.Y + Container.PadTop - 1)
			{
				if (tick == 0 && (MinValue < 0 || MaxValue < 0))
					Container.CurrentPen.Width = 2f;
				else
					Container.CurrentPen.Width = 1f;

				if (tick % MajorInterval == 0)
				{
					if (UseDivider)
					{
						displaytick = tick / divider;
						Container.DrawLine(Container.Location.X + Container.PadLeft - 8, y, Container.Location.X + Container.PadLeft + Container.PlotWidth, y);
						Container.DrawText(Container.PadLeft - 12, y - (Container.CurrentFont.Height / 2), displaytick.ToString(), 100, TextAlignment.Right);
					}
					else 
					{
						Container.DrawLine(Container.Location.X + Container.PadLeft - 8, y, Container.Location.X + Container.PadLeft + Container.PlotWidth, y);
						Container.DrawText(Container.PadLeft - 12, y - (Container.CurrentFont.Height / 2), tick.ToString(), 100, TextAlignment.Right);
					}
				}
				else
				{
					if (showNonIntervalTicks)
					{
						Container.DrawLine(Container.Location.X + Container.PadLeft - 4, y, Container.Location.X + Container.PadLeft, y);
					}
				}

				tick += MinorInterval;
				y -= MinorInterval * PlotUnit;
			}
		}

		/// <summary>
		/// Used to map Series values to Chart coordinates.
		/// </summary>
		public override float PlotUnit
		{
			get
			{
				decimal val = Math.Abs(MaxValue - MinValue);
				if (val == 0)
					return 1;
				else
					return Convert.ToSingle(Container.PlotHeight / val);
			}
		}

		/// <summary>
		/// Converts the specified value into chart coordinates.
		/// </summary>
		/// <param name="val">The value to convert.</param>
		/// <returns>float</returns>
		public override float ChartUnits(float val)
		{
			float adjust = 0;

			if (MinValue < 0)
				adjust = Math.Abs(MinValue);

			return (float)Container.Location.Y + (float)Container.PadTop + (float)Container.PlotHeight - (PlotUnit * (val + adjust));
		}
	}
}
