using System;
using System.Drawing;
using System.Windows.Forms;

using System.IO;

using System.Reflection;
using Charts;

using GameManagement;
using ReportBuilder;

namespace ChartScreens
{
	/// <summary>
	/// Summary description for TestChartsScreen.
	/// </summary>
	public class TestChartsScreen : Panel
	{
		/// <summary>
		/// Have a selectable list of charts/reports that the user can choose...
		/// </summary>
		protected System.Windows.Forms.ComboBox chartChoices;
		/// <summary>
		/// Have a single chart control on the page.
		/// </summary>
		protected Chart chartControl;
		protected Table table;
		protected NetworkProgressionGameFile gameFile;

		public NetworkProgressionGameFile TheGameFile
		{
			set
			{
				gameFile = value;
			}

			get
			{
				return gameFile;
			}
		}

		public TestChartsScreen()
		{
			this.SuspendLayout();
			chartChoices = new ComboBox();
			chartChoices.Size = new Size(200,30);
			this.Controls.Add(chartChoices);
			chartChoices.SelectedIndexChanged += new EventHandler(chartChoices_SelectedIndexChanged);
			chartChoices.SelectedValueChanged += new EventHandler(chartChoices_SelectedValueChanged);
			// No chart selected yet...
			chartControl = null;
			table = null;
			//
			SetupChartChoices();
			//
			this.ResumeLayout(false);
			//
			this.Resize += new EventHandler(TestChartsScreen_Resize);
			this.VisibleChanged += new EventHandler(TestChartsScreen_VisibleChanged);
		}

		protected void SetupChartChoices()
		{
			chartChoices.Items.Add("Race 1 [Car1] Operations Gantt Chart");

			chartChoices.Items.Add("Race 1 Operations Score Card");
			chartChoices.Items.Add("Race 1 Operations Support Costs");

//			chartChoices.Items.Add("Race 1 [Car2] Operations Gantt Chart");
//			chartChoices.Items.Add("Race 1 [Car3] Operations Gantt Chart");
//			chartChoices.Items.Add("Race 1 [Car4] Operations Gantt Chart");
//			chartChoices.Items.Add("Race 2 [Car1] Operations Gantt Chart");
//			chartChoices.Items.Add("Race 2 [Car2] Operations Gantt Chart");
//			chartChoices.Items.Add("Race 2 [Car3] Operations Gantt Chart");
//			chartChoices.Items.Add("Race 2 [Car4] Operations Gantt Chart");
//			chartChoices.Items.Add("Race 3 [Car1] Operations Gantt Chart");
//			chartChoices.Items.Add("Race 3 [Car2] Operations Gantt Chart");
//			chartChoices.Items.Add("Race 3 [Car3] Operations Gantt Chart");
//			chartChoices.Items.Add("Race 3 [Car4] Operations Gantt Chart");
//			chartChoices.Items.Add("Race 4 [Car1] Operations Gantt Chart");
//			chartChoices.Items.Add("Race 4 [Car2] Operations Gantt Chart");
//			chartChoices.Items.Add("Race 4 [Car3] Operations Gantt Chart");
//			chartChoices.Items.Add("Race 4 [Car4] Operations Gantt Chart");
		}

		protected void DoLayout()
		{
			if(null != chartControl)
			{
				chartControl.Location = new Point(0,30);
				chartControl.Size = new Size(this.Width,this.Height-30);
			}
			if(null != table)
			{
				table.Location = new Point(30,30);
				table.Size = new Size(this.Width-60, this.Height-60);
			}
		}

		private void TestChartsScreen_Resize(object sender, EventArgs e)
		{
			DoLayout();
		}

		private void chartChoices_SelectedIndexChanged(object sender, EventArgs e)
		{
			// User has chosen a different chart / report.
			if(null != chartControl)
			{
				chartControl.Dispose();
				chartControl = null;
			}
			if (null != table)
			{
				table.Dispose();
				table = null;
			}
			//
			if(null != gameFile)
			{
				if(chartChoices.SelectedIndex == 0)
				{
					OpsGanttReport ogp = new OpsGanttReport();
					string ganttXmlFile = ogp.BuildReport(gameFile, 1, "Car 1");
					OpsGanttChart opg = new OpsGanttChart();
					chartControl = opg;
					//
					System.IO.StreamReader file = new System.IO.StreamReader(ganttXmlFile);
					string xmldata = file.ReadToEnd();
					file.Close();
					//
					chartControl.LoadData(xmldata);
				}
				if (chartChoices.SelectedIndex == 1)
				{
					//TW - new score card report
					OpsScoreCardReport scr = new OpsScoreCardReport();

					string[] xmldataArray = new string[gameFile.CurrentRound];
					for(int i = 0; i < this.gameFile.CurrentRound; i++)
					{
						xmldataArray[i] = scr.BuildReport(gameFile, i+1);
					}

					Table scTable = new Table();
					table = scTable;

					System.IO.StreamReader file = new System.IO.StreamReader(scr.aggregateResults(xmldataArray, gameFile, gameFile.CurrentRound));
					string xmldata = file.ReadToEnd();
					file.Close();

					table.LoadData(xmldata);
				}
				if (chartChoices.SelectedIndex == 2)
				{
					//TW - new support costs report
					OpsSupportCostsReport costs = new OpsSupportCostsReport();

					string[] xmldataArray = new string[gameFile.CurrentRound];
					for(int i = 0; i < this.gameFile.CurrentRound; i++)
					{
						xmldataArray[i] = costs.BuildReport(gameFile, i+1);
					}

					Table scTable = new Table();
					table = scTable;

					System.IO.StreamReader file = new System.IO.StreamReader(costs.CombineRoundResults(xmldataArray, gameFile, gameFile.CurrentRound));
					string xmldata = file.ReadToEnd();
					file.Close();

					table.LoadData(xmldata);
				}
			}
			//
			if(null != chartControl)
			{
				this.Controls.Add(chartControl);
				DoLayout();
			}
			if(null != table)
			{
				this.Controls.Add(table);
				DoLayout();
			}
		}

		private void chartChoices_SelectedValueChanged(object sender, EventArgs e)
		{
			string str = chartChoices.SelectedText;
			int x = chartChoices.SelectedIndex;
		}

		private void TestChartsScreen_VisibleChanged(object sender, EventArgs e)
		{
			// If we have just been re-shown and we are showing a chart then update it now...
			if( (this.Visible) && (null != chartControl) )
			{
				chartChoices_SelectedIndexChanged(this,null);
			}
		}
	}
}
