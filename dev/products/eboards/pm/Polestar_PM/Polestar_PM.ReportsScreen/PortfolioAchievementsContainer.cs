using System;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;

using CommonGUI;
using ChartScreens;
using ReportBuilder;
using LibCore;
using Network;
using Charts;
using System.Collections;
using Logging;
using GameManagement;
using CoreUtils;
using Polestar_PM.OpsGUI;
using Polestar_PM.DataLookup;

namespace Polestar_PM.ReportsScreen 
{
	/// <summary>
	/// This is the display container for showing the Different aspects of Round 3 Performance
	/// We have 4 graphs (Transaction, Cost Reduction, People Used and Budget Used)
	/// The "PM_PortfolioReportMultiTrace.cs" builds the data for the different metrics 
	/// We just need a group of 4 line graphs on a panel
	/// </summary>
	public class PortfolioAchievementsContainer : FlickerFreePanel
	{
		NetworkProgressionGameFile myGameFile = null;

		public PortfolioAchievementsContainer()
		{ 
		}

		new public void Dispose()
		{
		}

		public void SetGameFile(NetworkProgressionGameFile _gameFile)
		{
			myGameFile = _gameFile;

			PM_PortfolioReportMultiTrace report = new PM_PortfolioReportMultiTrace();
			string xml;

			PM_PortfolioReportMultiTrace.Metric metric1 = PM_PortfolioReportMultiTrace.Metric.Transactions;
			PM_PortfolioReportMultiTrace.Metric metric2 = PM_PortfolioReportMultiTrace.Metric.CostReduction;
			PM_PortfolioReportMultiTrace.Metric metric3 = PM_PortfolioReportMultiTrace.Metric.PeopleEmployed;
			PM_PortfolioReportMultiTrace.Metric metric4 = PM_PortfolioReportMultiTrace.Metric.BudgetEmployed;

			//The Line Graph needs a back panel to sit on to avoid a bug 
			//The height of the graph Axis is affected by the y position of the graph on it's parent panel
			//so we create a dummmy panel (p1 etc) for now while we work a good fix for Line Graph

			//==========================================================
			//==Build the Transactions report, back Panel and line Graph
			//==========================================================
			string filename1 = report.BuildReport(_gameFile, metric1);
			using (StreamReader reader1 = new StreamReader(filename1))
			{
				xml = reader1.ReadToEnd();
			}
			Panel p1 = new Panel();
			p1.Location = new Point(0, 0);
			p1.Size = new Size(485, 295);
			this.Controls.Add(p1);
			LineGraph programBenefitChart1 = new LineGraph();
			programBenefitChart1.SetMainTitleVisibility(false);
			programBenefitChart1.LoadData(xml);
			programBenefitChart1.Location = new Point(5, 5);
			programBenefitChart1.Size = new Size(480, 290);
			//programBenefitChart1.BackColor = Color.LightPink;
			p1.Controls.Add(programBenefitChart1);

			//==========================================================
			//==Build the Cost Reduction report, back Panel and line Graph
			//==========================================================
			string filename2 = report.BuildReport(_gameFile, metric2);
			using (StreamReader reader2 = new StreamReader(filename2))
			{
				xml = reader2.ReadToEnd();
			}
			Panel p2 = new Panel();
			p2.Location = new Point(0+480+5, 0);
			p2.Size = new Size(485, 295);
			this.Controls.Add(p2);
			LineGraph programBenefitChart2 = new LineGraph();
			programBenefitChart2.SetMainTitleVisibility(false);
			programBenefitChart2.LoadData(xml);
			programBenefitChart2.Location = new Point(5,5);
			programBenefitChart2.Size = new Size(480, 290);
			//programBenefitChart2.BackColor = Color.LightCoral;
			p2.Controls.Add(programBenefitChart2);

			//==========================================================
			//==Build the People Employed report, back Panel and line Graph
			//==========================================================
			string filename3 = report.BuildReport(_gameFile, metric3);
			using (StreamReader reader3 = new StreamReader(filename3))
			{
				xml = reader3.ReadToEnd();
			}
			Panel p3 = new Panel();
			p3.Location = new Point(5, 300);
			p3.Size = new Size(480, 295);
			this.Controls.Add(p3);
			LineGraph programBenefitChart3 = new LineGraph();
			programBenefitChart3.SetMainTitleVisibility(false);
			programBenefitChart3.LoadData(xml);
			programBenefitChart3.Location = new Point(0, 5);
			programBenefitChart3.Size = new Size(480, 290);
			//programBenefitChart3.BackColor = Color.LightBlue;
			p3.Controls.Add(programBenefitChart3);

			//==========================================================
			//==Build the Budget Employed report, back Panel and line Graph
			//==========================================================
			string filename4 = report.BuildReport(_gameFile, metric4);
			using (StreamReader reader4 = new StreamReader(filename4))
			{
				xml = reader4.ReadToEnd();
			}
			Panel p4 = new Panel();
			p4.Location = new Point(5 + 480 + 5, 300);
			p4.Size = new Size(480, 295);
			this.Controls.Add(p4);
			LineGraph programBenefitChart4 = new LineGraph();
			programBenefitChart4.SetMainTitleVisibility(false);
			programBenefitChart4.LoadData(xml);
			programBenefitChart4.Location = new Point(0,5);
			programBenefitChart4.Size = new Size(480, 290);
			//programBenefitChart4.BackColor = Color.LightSeaGreen;
			p4.Controls.Add(programBenefitChart4);
		}
	}

}
