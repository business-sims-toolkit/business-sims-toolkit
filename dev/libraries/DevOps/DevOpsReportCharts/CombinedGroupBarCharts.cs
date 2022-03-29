using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

using Charts;
using Events;
using LibCore;
using ResizingUi;

namespace DevOpsReportCharts
{
	public class CombinedGroupBarCharts : SharedMouseEventControl
	{
		public CombinedGroupBarCharts (IEnumerable<string> reportFilepaths)
		{
			barCharts = reportFilepaths.Select(r =>
			{
				var barChart = new GroupedBarChart(BasicXmlDocument.CreateFromFile(r).DocumentElement)
				{
					XAxisHeight = 50,
					YAxisWidth = 35,
					LegendX = 250,
					LegendY = 10,
					LegendHeight = 50
				};
				Controls.Add(barChart);

				return barChart;
			}).ToList();
		}

		protected override void OnSizeChanged(EventArgs e)
		{
			DoSize();
		}

		void DoSize()
		{
			if (!barCharts.Any())
			{
				return;
			}

			const int heightPadding = 5;

			var y = heightPadding;
			var chartHeight = (int)Math.Floor((Height - (barCharts.Count + 1) * heightPadding) / (double)barCharts.Count);

			foreach (var barChart in barCharts)
			{
				barChart.Bounds = new Rectangle(0, y, Width, chartHeight);
				y += heightPadding + chartHeight;

			}

		}

		readonly List<GroupedBarChart> barCharts;

		public override List<KeyValuePair<string, Rectangle>> BoundIdsToRectangles => barCharts.SelectMany((c, i) =>
				c.BoundIdsToRectangles.Select(
					b => new KeyValuePair<string, Rectangle>($"barchart_{i}_{b.Key}", b.Value)))
			.ToList();

		public override void ReceiveMouseEvent(SharedMouseEventArgs args)
		{
			throw new NotImplementedException();
		}
	}
}
