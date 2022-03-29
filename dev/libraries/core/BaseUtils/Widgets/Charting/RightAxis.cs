using System;
using System.Drawing;

namespace BaseUtils
{
	/// <summary>
	/// Extends Axis to implement a Right Axis.
	/// </summary>
	public class RightAxis : Axis
	{
		/// <summary>
		/// Creates an instance of LeftAxis.
		/// </summary>
		/// <param name="container"></param>
		public RightAxis(ChartContainer container) : base(container)
		{
		}

		/// <summary>
		/// Draws the Right Axis.
		/// </summary>
		public override void Draw()
		{
			if (!DrawAxis)
				return;

			// Draw the label
			SizeF size = Container.MeasureString(this.Label, Container.CurrentFont);
			Container.CurrentBrush = new SolidBrush(this.LabelColor);
			Container.DrawVerticalText(Container.PlotBounds.X + Container.PlotBounds.Width + Container.PadRight - 15, Container.PlotBounds.Y + ((Container.PlotBounds.Height - size.Width) / 2), this.Label);
			
			// Start at the bottom and work up
			float y = Container.Location.Y + Container.PadTop + Container.PlotHeight;
			int tick = MinValue;

			while (y >= Container.Location.Y + Container.PadTop - 1)
			{
				if (tick == 0 && (MinValue < 0 || MaxValue < 0))
					Container.CurrentPen.Width = 2f;
				else
					Container.CurrentPen.Width = 1f;

				if (tick % MajorInterval == 0)
				{
					Container.DrawLine(Container.PlotBounds.X + Container.PlotBounds.Width, y, Container.PlotBounds.X + Container.PlotBounds.Width + 8, y);
					Container.DrawText(Container.PlotBounds.X + Container.PlotBounds.Width + 12, y - (Container.CurrentFont.Height / 2), tick.ToString());
				}
				else
				{
					Container.DrawLine(Container.PlotBounds.X + Container.PlotBounds.Width, y, Container.PlotBounds.X + Container.PlotBounds.Width + 4, y);
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
