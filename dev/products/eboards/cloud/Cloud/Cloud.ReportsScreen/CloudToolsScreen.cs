using System;
using System.Drawing;
using System.Windows.Forms;

using GameManagement;

using NetworkScreens;

using ReportBuilder;
using LibCore;
using CoreUtils;

namespace Cloud.ReportsScreen
{
	public class CloudToolsScreen : FlashToolsScreen
	{
		Panel networkPanel;
		ComboBox networkRoundSelector;
		NetworkReport networkReport;

		public CloudToolsScreen (NetworkProgressionGameFile gameFile, Control gamePanel, bool showOptions, SupportSpendOverrides spendOverrides)
			: base (gameFile, gamePanel, showOptions, spendOverrides)
		{
			tabBar.ClearTabs();
			tabBar.AddTab("Network report", (int) PanelToShow.NetworkReport, true);

			BuildNetworkPanel();

			PanelSelected = PanelToShow.NetworkReport;
			RefreshScreen();

			DoSize();
		}

		void BuildNetworkPanel ()
		{
			networkPanel = new Panel ();
			Controls.Add(networkPanel);
			networkPanel.Location = new Point(0, tabBar.Bottom);
			networkPanel.Size = new Size(1004, 620);
			networkPanel.Hide();

			networkRoundSelector = new ComboBox ();
			for (int i = 1; i <= SkinningDefs.TheInstance.GetIntData("roundcount", 5); i++)
			{
				networkRoundSelector.Items.Add("Round " + CONVERT.ToStr(i));
			}
			networkPanel.Controls.Add(networkRoundSelector);
			networkRoundSelector.Location = new Point (10, 10);
			networkRoundSelector.Width = 100;
			networkRoundSelector.DropDownStyle = ComboBoxStyle.DropDownList;
			networkRoundSelector.SelectedIndexChanged += new EventHandler (networkRoundSelector_SelectedIndexChanged);
		}

		void networkRoundSelector_SelectedIndexChanged (object sender, EventArgs e)
		{
			if (networkReport != null)
			{
				networkPanel.Controls.Remove(networkReport);
			}

			int round = 1 + networkRoundSelector.SelectedIndex;
			if (round <= _gameFile.LastRoundPlayed)
			{
				networkReport = new NetworkReport (_gameFile, round);

				networkPanel.Controls.Add(networkReport);
			}

			DoSize();
		}

		protected override void BuildBoardView ()
		{
		}

		protected override void ShowDefaultPanel ()
		{
			PanelSelected = PanelToShow.CloudScores;
			RefreshScreen();
		}

		protected override void DoSize ()
		{
			base.DoSize();

			networkPanel.Location = new Point (0, tabBar.Bottom);
			networkPanel.Size = new Size (Width - (networkPanel.Left * 2), Height - networkPanel.Top);

			if (networkReport != null)
			{
				networkReport.Location = new Point(10, networkRoundSelector.Bottom + 10);
				networkReport.Size = new Size(networkPanel.Width - (2 * networkReport.Left), networkPanel.Height - networkReport.Top);
			}
		}

		protected override void RefreshScreen ()
		{
			switch ((PanelToShow) PanelSelected)
			{
				case PanelToShow.NetworkReport:
					networkRoundSelector.SelectedIndex = Math.Max(0, Math.Min(networkRoundSelector.Items.Count, _gameFile.CurrentRound) - 1);
					networkPanel.BringToFront();
					networkPanel.Show();
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
	}
}