using System;
using System.Windows.Forms;
using System.Drawing;

using GameManagement;

using CommonGUI;

using ChartScreens;
using ReportBuilder;

using LibCore;
using Network;

using TransitionScreens;
using Polestar_PM.TransScreen;

using Charts;

using System.Collections;
using Logging;

namespace Polestar_PM.ReportsScreen
{
	/// <summary>
	/// Summary description for MS_IT_TabbedChartScreen.
	/// </summary>
	public class MS_IT_TabbedChartScreen : IT_TabbedChartScreen
	{
		new NetworkControl_5_Zones network;

		public MS_IT_TabbedChartScreen(NetworkProgressionGameFile gameFile, SupportSpendOverrides _spend_overrides) 
		: base(gameFile, _spend_overrides)
		{
			tabBar.SetTabTitle(0,"SLA Analysis");
		}

		protected override void ShowNetwork(int round)
		{
#if !PASSEXCEPTIONS
			try
			{
#endif
				if(null != Costs) Costs.Flush();
				NetworkReport nr = new NetworkReport();
				string ganttXmlFile = nr.BuildReport(_gameFile, round);

				//
				System.IO.StreamReader file = new System.IO.StreamReader(ganttXmlFile);
				string xmldata = file.ReadToEnd();
				file.Close();

				// : Fix for 3754 (among others).  Rather than scan all our controls for
				// one whose type name matches what we expect (which breaks when we subclass),
				// we just keep an object reference.
				if (network != null)
				{
					pnlNetwork.SuspendLayout();
					pnlNetwork.Controls.Remove(network);
					network.Dispose();
					pnlNetwork.ResumeLayout(false);
				}

				network = new NetworkControl_5_Zones();
				network.LoadData(xmldata);

				network.Location = new Point(5,40);
				network.Size = new Size(this.Width - 10,this.Height - 30);

				pnlNetwork.SuspendLayout();
				this.pnlNetwork.Controls.Add(network);
				pnlNetwork.ResumeLayout(false);

				this.RedrawNetwork = false;
#if !PASSEXCEPTIONS
			}
			catch(Exception ex)
			{
				AppLogger.TheInstance.WriteLine("Timer Level Exception : " + ex.Message + ":\r\n" + ex.StackTrace);
			}
#endif
		}

		protected override void ShowGanttChart(int round, string car, RoundScores rs)
		{
			base.ShowGanttChart(round, car, rs);

			ganttChart.SetFillColour( Color.FromArgb(159,204,136) );
			ganttChart.SetGridWidth(8,1);
			ganttChart.SetGridColour(Color.White);
		}
	}
}
