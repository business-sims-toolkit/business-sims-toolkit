using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

using Charts;
using Events;
using GameManagement;
using LibCore;
using ResizingUi;

namespace DevOps.ReportsScreen
{
    internal delegate string GroupedBarChartReportHandler(NetworkProgressionGameFile gameFile, int round); 
    internal class CombinedGroupBarCharts : SharedMouseEventControl
    {
        public CombinedGroupBarCharts (NetworkProgressionGameFile gameFile, int round,
                                       IEnumerable<GroupedBarChartReportHandler> reportBuilders)
        {
            barCharts = reportBuilders.Select(r =>
            {
                var report = r.Invoke(gameFile, round);

                var barChart = new GroupedBarChart(BasicXmlDocument.CreateFromFile(report).DocumentElement)
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

        protected override void OnSizeChanged (EventArgs e)
        {
            DoSize();
        }

        void DoSize ()
        {
            if (!barCharts.Any())
            {
                return;
            }

            const int heightPadding = 5;

            var y = heightPadding;
            var chartHeight = (int) Math.Floor((Height - (barCharts.Count + 1) * heightPadding) / (double)barCharts.Count);

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

		public override void ReceiveMouseEvent (SharedMouseEventArgs args)
        {
            throw new NotImplementedException();
        }
    }
}
