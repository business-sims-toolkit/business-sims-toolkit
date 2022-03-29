using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

using Charts;
using CommonGUI;
using DevOpsReportCharts;
using DevOps.ReportsScreen.Interfaces;

namespace DevOps.ReportsScreen
{
    public class TableReportPanel : Panel
    {
        public TableReportPanel(IRoundScoresUpdater<DevOpsRoundScores> roundScoresUpdater, List<DevOpsRoundScores> roundScores, TableReportBuilderHandler reportBuilder)
        {
            this.roundScores = roundScores;
            this.reportBuilder = reportBuilder;

            roundScoresUpdater.RoundScoresChanged += roundScoresUpdater_RoundScoresChanged;
            this.roundScoresUpdater = roundScoresUpdater;


            UpdateReport();
        }

        protected override void Dispose(bool disposing)
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
            if (table == null)
            {
                return;
            }

            table.Width = Width - 2 * table.Left;
            table.TextScaleFactor = Math.Min(table.Width * 1.0f / 700, Height * 1.0f / 500);
            table.Height = Math.Min(table.TableHeight, Height);
        }

        void UpdateReport()
        {
            if (table != null)
            {
                table.Dispose();
                table = null;
            }

            if (reportBuilder == null)
            {
                return;
            }

            if (roundScores.Any(r => r?.WasUnableToGetData ?? true))
            {
                return;
            }

            //ReSharper disable once UnusedVariable
            using (var cursor = new WaitCursor(this))
            {
                var buildResult = reportBuilder.Invoke();

                table = new Table();
                table.LoadData(File.ReadAllText(buildResult.ReportFilename));

                table.Location = new Point(30, 35);
                var tableHeight = Math.Min(buildResult.PreferredTableHeight, Height);
                table.Size = new Size(Width - 80, tableHeight);
                table.AutoScroll = true;
                table.BorderStyle = BorderStyle.None;
                Controls.Add(table);
                table.BringToFront();
            }

            DoSize();
        }

        void roundScoresUpdater_RoundScoresChanged(object sender, Events.EventArgs<List<DevOpsRoundScores>> e)
        {
            roundScores = e.Parameter;

            UpdateReport();
        }

        readonly IRoundScoresUpdater<DevOpsRoundScores> roundScoresUpdater;

        Table table;
        List<DevOpsRoundScores> roundScores;
        readonly TableReportBuilderHandler reportBuilder;

    }
}
