using System;
using System.Drawing;
using System.Windows.Forms;

using CommonGUI;
using GameManagement;
using NetworkScreens;
using ReportBuilder;
using ChartScreens;
using Charts;
using LibCore;
using CoreUtils;
using Logging;

namespace Polestar_PM.ReportsScreen
{
	public class PM_ToolsScreen : FlashToolsScreen
	{
		Panel networkPanel;
		ComboBox networkRoundSelector;
		NetworkControl network;

		public PM_ToolsScreen (NetworkProgressionGameFile gameFile, Control gamePanel, TacPermissions tacPermissions, SupportSpendOverrides supportOverrides)
			: base (gameFile, gamePanel, false, supportOverrides)
		{
			if (true)
			{
				tabBar.AddTab("Network", (int)PanelToShow.NetworkReport, true);
			}

			tabBar.RemoveTabByCode((int) PanelToShow.PathfinderSurvey);

			RemoveTab(PanelToShow.Costs);
			RemoveTab(PanelToShow.SupportCosts);

			networkPanel = new Panel ();
			networkRoundSelector = new ComboBox ();
			networkRoundSelector.DropDownStyle = ComboBoxStyle.DropDownList;
			for (int i = 1; i <= 3; i++)
			{
				networkRoundSelector.Items.Add("Round " + LibCore.CONVERT.ToStr(i));
			}
			networkRoundSelector.Location = new Point (5, 10);
			networkRoundSelector.Size = new Size (100, networkRoundSelector.Height);
			networkRoundSelector.SelectedIndexChanged += new EventHandler (networkRoundSelector_SelectedIndexChanged);
			networkPanel.Controls.Add(networkRoundSelector);
			this.Controls.Add(networkPanel);
			networkRoundSelector.SelectedIndex = Math.Min(networkRoundSelector.Items.Count, gameFile.LastRoundPlayed) - 1;

			ShowDefaultPanel();

			DoSize();
		}

		protected override void ShowDefaultPanel()
		{
			if (true)
			{
				PanelSelected = PanelToShow.Board;
			}
			else
			{
				PanelSelected = PanelToShow.Maturity;
			}
			RefreshScreen();
		}

		protected override void DoSize ()
		{
			networkPanel.Location = new Point (0, 30);
			networkPanel.Size = new Size (this.Width, this.Height - networkPanel.Top);

			base.DoSize();
		}

		protected override void RefreshScreen()
		{
			switch (PanelSelected)
			{
				case PanelToShow.NetworkReport:
					RefreshNetworkReport();
					break;

				default:
					if (networkPanel != null)
					{
						networkPanel.Hide();
					}
					base.RefreshScreen();
					break;
			}
		}

		void RefreshNetworkReport ()
		{
			networkPanel.SuspendLayout();

#if !PASSEXCEPTIONS
			try
			{
#endif
				NetworkReport nr = new NetworkReport();
				string ganttXmlFile = nr.BuildReport(_gameFile, networkRoundSelector.SelectedIndex + 1);

				//
				System.IO.StreamReader file = new System.IO.StreamReader(ganttXmlFile);
				string xmldata = file.ReadToEnd();
				file.Close();

				// : Fix for 3754 (among others).  Rather than scan all our controls for
				// one whose type name matches what we expect (which breaks when we subclass),
				// we just keep an object reference.
				if (network != null)
				{
					networkPanel.Controls.Remove(network);
					network.Dispose();
				}

				network = new NetworkControl ();
				network.SetProcessingFlagVisible(false);
				network.LoadData(xmldata);

				network.Location = new Point(5,40);
				network.Size = new Size(this.Width - 10,this.Height - 30);

				networkPanel.Controls.Add(network);
#if !PASSEXCEPTIONS
			}
			catch(Exception ex)
			{
				AppLogger.TheInstance.WriteLine("Timer Level Exception : " + ex.Message + ":\r\n" + ex.StackTrace);
			}
#endif

			networkPanel.Show();
			networkPanel.BringToFront();

			networkPanel.ResumeLayout(false);
		}

		void networkRoundSelector_SelectedIndexChanged (object sender, EventArgs args)
		{
			RefreshNetworkReport();
		}
	}
}