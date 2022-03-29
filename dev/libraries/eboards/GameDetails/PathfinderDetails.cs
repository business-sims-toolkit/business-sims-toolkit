using System;
using System.Windows.Forms;
using System.Drawing;
using CoreUtils;
using GameManagement;

namespace GameDetails
{
	public class PathfinderDetails : GameDetailsSection
	{
		NetworkProgressionGameFile gameFile;
		IGameLoader gamePanel;

		Label preamble;
		CheckBox itsmBox;

		public PathfinderDetails (NetworkProgressionGameFile gameFile, IGameLoader gamePanel)
		{
			this.gameFile = gameFile;
			this.gamePanel = gamePanel;

			Title = "Pathfinder Options";

			BuildControls();
			LoadData();
		}

		void BuildControls ()
		{
			preamble = new Label ();
			preamble.Font = SkinningDefs.TheInstance.GetFont(10);
			preamble.Text = "This option activates the Pathfinder Perception Survey in the Reports Screen Section.";
			panel.Controls.Add(preamble);

			itsmBox = new CheckBox();
			itsmBox.Font = SkinningDefs.TheInstance.GetFont(10);
			itsmBox.Text = "ITSM";
			itsmBox.CheckedChanged += itsmBox_CheckedChanged;
			panel.Controls.Add(itsmBox);

			DoLayout();
		}

		void itsmBox_CheckedChanged (object sender, EventArgs e)
		{
			UpdateChoices();
		}
     
		void UpdateChoices ()
		{
			gamePanel.RefreshMaturityScoreSet();
		}

		void DoLayout ()
		{
			preamble.Location = new Point (20, 0);
			preamble.Size = new Size(490 - preamble.Left, 50);

			itsmBox.Location = new Point (20, preamble.Bottom - 4);
			itsmBox.Size = new Size (200, 25);
		}

		public override void LoadData ()
		{
			itsmBox.Checked = gameFile.GetBoolGlobalOption("pathfinder_itsm_enabled", false);
		}

		public override bool SaveData ()
		{
			gameFile.SetGlobalOption("pathfinder_itsm_enabled", itsmBox.Checked);

			UpdateChoices();

			return true;
		}
	}
}