using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;

using Charts;
using Events;
using ResizingUi;

namespace DevOpsReportCharts
{
	public delegate string CloudTimeChartReportBuilderHandler(int round, string business);
	public class CloudTimeChartReportPanel : SharedMouseEventControl
	{
		public CloudTimeChartReportPanel(XmlElement xml)
		{
			scrollPanel = new Panel
			{
				AutoScroll = true
			};
			Controls.Add(scrollPanel);
			scrollPanel.BringToFront();

			cloudTimeChart = new CloudTimeChart(xml, false);
			scrollPanel.Controls.Add(cloudTimeChart);
			cloudTimeChart.BringToFront();

			cloudTimeChart.MouseEventFired += cloudTimeChart_MouseEventFired;

			var legendBounds = new Rectangle
			{
				X = 0,
				Y = Height - 30,
				Width = cloudTimeChart.Width,
				Height = 25
			};

			legendPanel = cloudTimeChart.CreateLegendPanel(legendBounds, Color.Transparent, Color.Black);
			Controls.Add(legendPanel);
			legendPanel.BringToFront();
			legendPanel.Bounds = legendBounds;

			DoSize();
		}

		public override List<KeyValuePair<string, Rectangle>> BoundIdsToRectangles => cloudTimeChart.BoundIdsToRectangles;
		
		public override void ReceiveMouseEvent(SharedMouseEventArgs args)
		{
			cloudTimeChart.ReceiveMouseEvent(args);
		}

		void cloudTimeChart_MouseEventFired(object sender, SharedMouseEventArgs e)
		{
			OnMouseEventFired(e);
		}

		protected override void OnSizeChanged(EventArgs e)
		{
			DoSize();
		}

		void DoSize()
		{
			legendPanel.Width = Width;
			legendPanel.Bounds = new Rectangle(0, Height - legendPanel.PreferredHeight, Width, legendPanel.PreferredHeight);

			var previousScrollBarValue = scrollPanel.AutoScrollPosition;
			scrollPanel.AutoScrollPosition = new Point(0,0);
			scrollPanel.Bounds = new Rectangle(0, 0, Width, legendPanel.Top);

			cloudTimeChart.Bounds = new Rectangle(0, 0, Width, scrollPanel.Bounds.Height);
			
			var chartWidth = scrollPanel.Width - 20;

			cloudTimeChart.Bounds = new Rectangle(0, 0, chartWidth, cloudTimeChart.PreferredHeight);
			cloudTimeChart.SectionLeading = 6;
			cloudTimeChart.RowLeading = 2;

			scrollPanel.AutoScrollPosition = new Point(-previousScrollBarValue.X, -previousScrollBarValue.Y);
			

			scrollPanel.Invalidate();

			Invalidate();
		}

		readonly Panel scrollPanel;
		readonly CloudTimeChart cloudTimeChart;
		readonly TimeChartLegendPanel legendPanel;
	}
}
