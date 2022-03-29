using System;
using System.Windows.Forms;
using System.Drawing;
using CoreUtils;
using GameManagement;

namespace GameDetails
{
	public class PM_ITStyleSelection : GameDetailsSection
	{
		RadioButton withITButton;
		RadioButton withoutITButton;

		Label withITLabel;
		Label withoutITLabel;

		NetworkProgressionGameFile gameFile;
		IGameLoader gamePanel;
		bool canChange;
		bool optionConfirmed = false; 

		public PM_ITStyleSelection (NetworkProgressionGameFile gameFile, IGameLoader gamePanel,
			bool preSelectionSystemON, bool preSelectionSystem_Value)
		{
			this.gameFile = gameFile;
			this.gamePanel = gamePanel;

			Collapsible = false;
			SetSize(500, 120);

			Title = "IT Infrastructure";
			if (preSelectionSystemON)
			{
				if (gameFile.DoesGlobalOptionExist("it_present") == false)
				{
					gameFile.SetGlobalOption("it_present", preSelectionSystem_Value);
				}
			}

			if (DetermineGameTraining())
			{
				gameFile.SetGlobalOption("it_choice_confirmed", false);
			}

			canChange = ! gameFile.GetBoolGlobalOption("it_choice_confirmed", false);

			BuildControls();
			LoadData();
		}

		protected virtual void BuildControls ()
		{
			withITButton = new RadioButton ();
			withITButton.Font = SkinningDefs.TheInstance.GetFont(12, FontStyle.Bold);
			withITButton.Text = "With IT Infrastructure";
			withITButton.CheckAlign = ContentAlignment.MiddleLeft;
			withITButton.TextAlign = ContentAlignment.MiddleLeft;
			withITButton.CheckedChanged += withITButton_CheckedChanged;
			panel.Controls.Add(withITButton);

			withoutITButton = new RadioButton ();
			withoutITButton.Font = SkinningDefs.TheInstance.GetFont(12, FontStyle.Bold);
			withoutITButton.Text = "Without IT Infrastructure";
			withoutITButton.CheckAlign = ContentAlignment.MiddleLeft;
			withoutITButton.TextAlign = ContentAlignment.MiddleLeft;
			withoutITButton.CheckedChanged += withoutITButton_CheckedChanged;
			panel.Controls.Add(withoutITButton);

			withITLabel = new Label ();
			withITLabel.Font = SkinningDefs.TheInstance.GetFont(8);
			withITLabel.Text = SkinningDefs.TheInstance.GetData("help_game_with_it");
			panel.Controls.Add(withITLabel);

			withoutITLabel = new Label ();
			withoutITLabel.Font = SkinningDefs.TheInstance.GetFont(8);
			withoutITLabel.Text = SkinningDefs.TheInstance.GetData("help_game_without_it");
			panel.Controls.Add(withoutITLabel);

			DoLayout();
		}

		void withITButton_CheckedChanged (object sender, EventArgs e)
		{
			UpdateButtons();
		}

		void withoutITButton_CheckedChanged (object sender, EventArgs e)
		{
			UpdateButtons();
		}

		void UpdateButtons ()
		{
			withITLabel.Visible = withITButton.Enabled || withITButton.Checked;
			withoutITLabel.Visible = withoutITButton.Enabled || withoutITButton.Checked;
		}

		protected virtual void DoLayout ()
		{
			withITButton.Location = new Point (30, 0);
			withITButton.Size = new Size (270, 50);

			withITLabel.Location = new Point (withITButton.Right, withITButton.Top);
			withITLabel.Size = new Size (190, withITButton.Height);

			withoutITButton.Location = new Point (withITButton.Left, withITButton.Bottom + 15);
			withoutITButton.Size = withITButton.Size;

			withoutITLabel.Location = new Point (withoutITButton.Right, withoutITButton.Top);
			withoutITLabel.Size = new Size (190, withoutITButton.Height);
		}

		protected bool DetermineGameTraining()
		{
			return gameFile.IsTrainingGame;
		}

		public override bool SaveData ()
		{
			if (! ValidateFields())
			{
				return false;
			}

			if (withoutITButton.Checked)
			{
				if (! gameFile.GetBoolGlobalOption("it_choice_confirmed", false))
				{
					if (optionConfirmed == false)
					{
						if (MessageBox.Show(this, SkinningDefs.TheInstance.GetData("confirm_game_without_it"),
											 "Game Setup", MessageBoxButtons.YesNo) != DialogResult.Yes)
						{
							return false;
						}
						else
						{
							optionConfirmed = true;
						}
					}
				}
			}
			else
			{
				optionConfirmed = true;
			}

			gameFile.SetGlobalOption("it_present", withITButton.Checked);

			return true;
		}

		public override void LoadData ()
		{
			bool? itPresent = gameFile.GetBoolGlobalOption("it_present");

			if (itPresent.HasValue)
			{
				withITButton.Checked = itPresent.Value;
				withoutITButton.Checked = ! itPresent.Value;
			}
			else
			{
				withITButton.Checked = false;
				withoutITButton.Checked = false;
			}

			bool canChange = ! gameFile.GetBoolGlobalOption("it_choice_confirmed", false);
			withITButton.Enabled = canChange;
			withoutITButton.Enabled = canChange;

			UpdateButtons();
		}
	}
}