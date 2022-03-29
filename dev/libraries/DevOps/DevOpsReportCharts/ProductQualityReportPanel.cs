using System;
using System.Collections.Generic;
using System.Drawing;

using Charts;
using LibCore;
using ResizingUi;

namespace DevOpsReportCharts
{
	public class ProductQualityReportPanel : SharedMouseEventControl
	{
		public ProductQualityReportPanel (string reportFilename)
		{
			var xml = BasicXmlDocument.CreateFromFile(reportFilename).DocumentElement;

			productQualityReportChart = new BusinessServiceHeatMap();
			productQualityReportChart.LoadData(xml);

			Controls.Add(productQualityReportChart);
		}

		protected override void OnSizeChanged(EventArgs e)
		{
			DoSize();
		}

		void DoSize()
		{
			productQualityReportChart.RightColumnWidth = 200;
			productQualityReportChart.Bounds = new Rectangle(0, 10, Width, productQualityReportChart.NaturalHeight);

			Invalidate();
		}

		readonly BusinessServiceHeatMap productQualityReportChart;

		public override List<KeyValuePair<string, Rectangle>> BoundIdsToRectangles =>
			productQualityReportChart.BoundIdsToRectangles;
	}
}
