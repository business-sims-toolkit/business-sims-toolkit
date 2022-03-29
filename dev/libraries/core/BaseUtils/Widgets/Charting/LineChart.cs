using System.Drawing;

namespace BaseUtils
{
	/// <summary>
	/// Extends Chart to provide an implementation that
	/// draws a Line Chart.
	/// </summary>
	public class LineChart : Chart
	{
		/// <summary>
		/// Creates an instance of LineChart.
		/// </summary>
		/// <param name="axis"></param>
		/// <param name="series"></param>
		public LineChart(Axis axis, Series series) : base(axis, series)
		{
		}

		/// <summary>
		/// Draws the Line Chart.
		/// </summary>
		/// <param name="container"></param>
		public override void Draw(ChartContainer container)
		{
			if (series.Values.Count == 0)
				return;

			if (series.Values.Count > container.BottomAxis.Categories.Count)
				return;

			container.CurrentBrush = new SolidBrush(series.Color);
			container.CurrentPen = new Pen(series.Color, series.Weight);

			for (int i = 0; i < series.Values.Count; i++)
			{
				Category cat1 = container.BottomAxis.Categories[i];
				float x1 = container.BottomAxis.ChartUnits(i);
				float y1 = axis.ChartUnits(series.Values[i].Value);
				float adjust = container.BottomAxis.PlotUnit / 2;

				if (i+1 < series.Values.Count)
				{
					Category cat2 = container.BottomAxis.Categories[i+1];
					float x2 = container.BottomAxis.ChartUnits(i+1);
					float y2 = axis.ChartUnits(series.Values[i+1].Value);
					container.DrawLine(x1 + adjust, y1, x2 + adjust, y2);
				}

				container.FillCircle(x1 + adjust, y1, series.Weight);
			}
		}
	}
}
