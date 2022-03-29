using System;
using System.Drawing;

namespace BaseUtils
{
	/// <summary>
	/// Extends Axis to implement a Bottom Axis.
	/// </summary>
	public class BottomAxis : Axis
	{		
		/// <summary>
		/// Creates an instance of BottomAxis.
		/// </summary>
		/// <param name="container"></param>
		public BottomAxis(ChartContainer container) : base(container)
		{
		}

		/// <summary>
		/// Draws the Bottom Axis.
		/// </summary>
		public override void Draw()
		{
			base.Draw();

			if (!DrawAxis)
				return;

			if (MinorInterval < 1 || PlotUnit < 1)
				return;

			// Start at the left and work right
			int i = 0;
			int x = Container.Location.X + Container.PadLeft;
			int tick = MinValue;
			Brush labelBrush = new SolidBrush(this.LabelColor);

			while (x <= Container.Location.X + Container.PadLeft + Container.PlotWidth)
			{
				Container.DrawLine(x, Container.Location.Y + Container.PadTop + Container.PlotHeight, x, Container.Location.Y + Container.PadTop + Container.PlotHeight + 8);
				
				if (i < MaxValue)
				{
					Container.CurrentBrush = labelBrush;
					Container.DrawText(x, Container.Location.Y + Container.PadTop + Container.PlotHeight + 4, Categories[i].Label, PlotUnit, TextAlignment.Center);
				}

				tick += MinorInterval;
				x += Convert.ToInt32(MinorInterval * PlotUnit);
				i++;
			}

			if (Label.Length > 0)
			{
				Container.CurrentBrush = labelBrush;
				Container.DrawText(Container.Location.X + Container.PadLeft, Container.Location.Y + Container.PadTop + Container.PlotHeight + 18, Label, Container.PlotWidth, TextAlignment.Center);
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
					return Convert.ToSingle(Container.PlotWidth / val);
			}
		}

		/// <summary>
		/// Converts the specified value into chart coordinates.
		/// </summary>
		/// <param name="val">The value to convert.</param>
		/// <returns>float</returns>
		public override float ChartUnits(float val)
		{
			return Container.Location.X + Container.PadLeft + (val * PlotUnit);
		}

		/// <summary>
		/// The type of Axis; in this case Categorical.
		/// </summary>
		public override AxisType AxisType
		{
			get
			{
				return AxisType.Categorical;
			}
		}
	}
}
