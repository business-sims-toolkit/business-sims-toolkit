using System;
using System.IO;

using System.Windows.Forms;
using System.Drawing;

using LibCore;
using CoreUtils;
using GameManagement;

using ReportBuilder;
using Charts;

namespace NetworkScreens
{
	public class FlashToolsScreenWithNetworkReport : FlashToolsScreen
	{
		Panel networkPanel;
		ComboBox networkRoundSelector;
		NetworkControl networkControl;

		public FlashToolsScreenWithNetworkReport (NetworkProgressionGameFile gameFile, Control gamePanel, bool enableOptions, SupportSpendOverrides spendOverrides)
			: base (gameFile, gamePanel, enableOptions, spendOverrides)
		{
			tabBar.AddTab("Network", (int) PanelToShow.NetworkReport, true);

			networkPanel = new Panel ();
			Controls.Add(networkPanel);
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
			networkRoundSelector.SelectedIndexChanged += networkRoundSelector_SelectedIndexChanged;
		}

		void networkRoundSelector_SelectedIndexChanged (object sender, EventArgs e)
		{
			if (networkControl != null)
			{
				networkPanel.Controls.Remove(networkControl);
			}

			networkControl = new NetworkControl ();

			NetworkReport report = new NetworkReport ();
			string file = report.BuildReport(_gameFile, 1 + networkRoundSelector.SelectedIndex);
			using (StreamReader reader = new StreamReader(file))
			{
				string xml = reader.ReadToEnd();
				networkControl.LoadData(xml);
			}

			networkPanel.Controls.Add(networkControl);
			networkControl.Location = new Point (10, networkRoundSelector.Bottom + 10);
			networkControl.Size = new Size (networkPanel.Width - (2 * networkControl.Left), networkPanel.Height - networkControl.Top);
		}

		protected override void DoSize ()
		{
			base.DoSize();

			networkPanel.Location = new Point (0, tabBar.Bottom);
			networkPanel.Size = new Size (Width - (networkPanel.Left * 2), Height - networkPanel.Top);
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