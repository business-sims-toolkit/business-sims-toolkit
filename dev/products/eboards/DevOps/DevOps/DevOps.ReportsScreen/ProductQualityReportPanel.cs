using System;
using System.Collections.Generic;
using System.Drawing;

using Charts;
using GameManagement;
using LibCore;
using ResizingUi;

namespace DevOps.ReportsScreen
{
    internal class ProductQualityReportPanel : SharedMouseEventControl
    {
        public ProductQualityReportPanel (NetworkProgressionGameFile gameFile, int round)
        {
            var builder = new ProductQualityReportBuilder(gameFile);
            var reportFile = builder.BuildReport(round);

            var xml = BasicXmlDocument.CreateFromFile(reportFile).DocumentElement;

            productQualityReportChart = new BusinessServiceHeatMap();
            productQualityReportChart.LoadData(xml);

            Controls.Add(productQualityReportChart);
        }

        protected override void OnSizeChanged (EventArgs e)
        {
            DoSize();
        }

        void DoSize ()
        {
            productQualityReportChart.Bounds = new Rectangle(0, 10, Width, productQualityReportChart.NaturalHeight);

            Invalidate();
        }

        readonly BusinessServiceHeatMap productQualityReportChart;

	    public override List<KeyValuePair<string, Rectangle>> BoundIdsToRectangles =>
		    productQualityReportChart.BoundIdsToRectangles;
    }
}
