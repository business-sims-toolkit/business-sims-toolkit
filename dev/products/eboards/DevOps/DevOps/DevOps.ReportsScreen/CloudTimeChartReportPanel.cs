using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;

using Charts;
using ChartScreens;
using CommonGUI;
using CoreUtils;
using Events;
using DevOps.ReportsScreen.Interfaces;
using GameManagement;
using LibCore;
using ResizingUi;

namespace DevOps.ReportsScreen
{
    internal delegate string CloudTimeChartReportBuilderHandler(int round, string business);

    internal class CloudTimeChartWithLegend : SharedMouseEventControl
    {
        public CloudTimeChartWithLegend (XmlElement xml)
        {
            cloudTimeChart = new CloudTimeChart(xml, false);
            Controls.Add(cloudTimeChart);
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
		  //  .Concat(
				//new List<KeyValuePair<string, Rectangle>>
			 //   {
				//    new KeyValuePair<string, Rectangle>("cloud_legend", legendPanel.Bounds)
					
			 //   }).ToList();

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

        void DoSize ()
        {
            var legendPanelHeight = legendPanel.PreferredHeight;

            cloudTimeChart.Bounds = new Rectangle(0, 0, Width, Height - legendPanelHeight);
            
            legendPanel.Bounds = new Rectangle(0, Height - legendPanelHeight, Width, legendPanelHeight);

            Invalidate();
        }
        
        readonly CloudTimeChart cloudTimeChart;
        readonly TimeChartLegendPanel legendPanel;
    }

    internal class CloudTimeChartReportPanel : Panel
    {
        public CloudTimeChartReportPanel(NetworkProgressionGameFile gameFile, IRoundScoresUpdater<DevOpsRoundScores> roundScoresUpdater, List<DevOpsRoundScores> roundScores, CloudTimeChartReportBuilderHandler reportBuilder)
        {
            this.gameFile = gameFile;
            this.roundScores = roundScores;
            
            roundScoresUpdater.RoundScoresChanged += roundScoresUpdater_RoundScoresChanged;

            this.roundScoresUpdater = roundScoresUpdater;

            this.reportBuilder = reportBuilder;

            roundComboBox = DevOpsComboBoxBuilder.CreateRoundComboBox(gameFile);
            Controls.Add(roundComboBox);
            roundComboBox.SelectedIndexChanged += comboBox_SelectedIndexChanged;
            roundComboBox.Location = SkinningDefs.TheInstance.GetPointData("round_combo_box_position", new Point(0, 15));

            businessComboBox = DevOpsComboBoxBuilder.CreateBusinessComboBox(gameFile, true);
            Controls.Add(businessComboBox);
            businessComboBox.SelectedIndexChanged += comboBox_SelectedIndexChanged;
            businessComboBox.Location = new Point(roundComboBox.Right + 50, roundComboBox.Top);

            roundComboBox.SelectedIndex = Math.Max(gameFile.LastRoundPlayed - 1, 0);

        }

        void UpdateReport()
        {
            if (cloudTimeChart != null)
            {
                cloudTimeChart.Dispose();
                cloudTimeChart = null;
            }

            if (legendPanel != null)
            {
                legendPanel.Dispose();
                legendPanel = null;
            }

            if (roundComboBox.SelectedIndex + 1 > gameFile.LastRoundPlayed)
            {
                return;
            }

            if (reportBuilder == null)
            {
                return;
            }

            if (roundScores[roundComboBox.SelectedIndex]?.WasUnableToGetData ?? true)
            {
                return;
            }

            // ReSharper disable once UnusedVariable
            using (var cursor = new WaitCursor(this))
            {
	            var reportFilePath = reportBuilder.Invoke(roundComboBox.SelectedIndex + 1, businessComboBox.SelectedItem.Text);

                cloudTimeChart = new CloudTimeChart(BasicXmlDocument.CreateFromFile(reportFilePath).DocumentElement, false);
                Controls.Add(cloudTimeChart);
                cloudTimeChart.BringToFront();

                cloudTimeChart.Location = new Point(0, roundComboBox.Bottom + 10);
                cloudTimeChart.Size = new Size(Width - 10, Height - cloudTimeChart.Top - 20);

                var keyBounds = new Rectangle
                {
                    X = 0,
                    Y = cloudTimeChart.Bottom - 27,
                    Width = cloudTimeChart.Width,
                    Height = 25
                };

                legendPanel = cloudTimeChart.CreateLegendPanel(keyBounds, Color.Transparent, Color.Black);

                Controls.Add(legendPanel);
                legendPanel.BringToFront();
            }

            DoSize();
            legendPanel.Show();
        }

        protected override void Dispose (bool disposing)
        {
            if (disposing)
            {
                roundScoresUpdater.RoundScoresChanged -= roundScoresUpdater_RoundScoresChanged;
            }

            base.Dispose(disposing);
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            if (Visible)
            {
                UpdateReport();
            }
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            DoSize();
        }

        void DoSize()
        {
            if (cloudTimeChart == null || legendPanel == null)
            {
                return;
            }


            var chartTop = roundComboBox.Bottom + 10;

            cloudTimeChart.Size = new Size(Width, Height - chartTop);
            cloudTimeChart.Location = new Point(0, chartTop);

            legendPanel.Width = Width;
            legendPanel.Height = legendPanel.PreferredHeight;

            legendPanel.Location = new Point(0, cloudTimeChart.Height - legendPanel.Height);
        }

        void comboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateReport();
        }

        void roundScoresUpdater_RoundScoresChanged(object sender, EventArgs<List<DevOpsRoundScores>> e)
        {
            roundScores = e.Parameter;

            UpdateReport();
        }



        readonly IRoundScoresUpdater<DevOpsRoundScores> roundScoresUpdater;

        readonly CloudTimeChartReportBuilderHandler reportBuilder;

        readonly ComboBoxRow businessComboBox;
        readonly ComboBoxRow roundComboBox;

        CloudTimeChart cloudTimeChart;
        TimeChartLegendPanel legendPanel;

        readonly NetworkProgressionGameFile gameFile;
        List<DevOpsRoundScores> roundScores;

	    public ComboBoxRow BusinessSelector => businessComboBox;
	    public ComboBoxRow RoundSelector => roundComboBox;

    }
}