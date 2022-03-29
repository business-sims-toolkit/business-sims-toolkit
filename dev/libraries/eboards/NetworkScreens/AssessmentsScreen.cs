using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;

using LibCore;
using CoreUtils;

using BusinessServiceRules;
using GameManagement;

namespace NetworkScreens
{
	public class AssessmentsScreen : Panel
	{
		AssessmentsBiller biller;
		int round;
		GameFile gameFile;

		Font boldFont;
		Font normalFont;

		class AssessmentPanel : Panel
		{
			Label name;
			Label cost;
			public CheckBox check;

			public bool revealed;

			public ArrayList actionLabels;

			public Assessment assessment;

			public string AssessmentName
			{
				get
				{
					return name.Text;
				}
			}

			public AssessmentPanel (Assessment assessment, int round, Font boldFont, Font normalFont, int nameTab, int costTab, int checkTab)
			{
				this.assessment = assessment;

				name = new Label ();
				name.Text = assessment.name;
				name.Font = boldFont;
				name.Width = costTab - nameTab;
				name.Location = new Point (nameTab, 0);

				cost = new Label ();
				cost.Text = CONVERT.ToStr(assessment.cost);
				cost.Font = normalFont;
				cost.Width = checkTab - costTab;
				cost.Location = new Point (costTab, 0);

				check = new CheckBox ();
				check.Location = new Point (checkTab, 0);

				this.Controls.Add(name);
				this.Controls.Add(cost);
				this.Controls.Add(check);

				actionLabels = new ArrayList ();

				int y = 20;
				foreach (AssessmentAction action in assessment.ActionListForRound(round))
				{
					Label actionLabel = new Label ();
					actionLabel.Font = normalFont;
					actionLabel.Text = action.name;
					actionLabel.Location = new Point (50, y);
					actionLabel.Size = new Size (500, 30);
					y = actionLabel.Bottom;

					this.Controls.Add(actionLabel);
					actionLabels.Add(actionLabel);
				}

				revealed = false;
			}
		}

		int nameTab = 10;
		int costTab = 400;
		int checkTab = 500;

		ArrayList assessmentPanels;

		Button runButton;

		ComboBox roundSelector;
		
		public AssessmentsScreen (GameFile gameFile)
		{
			this.gameFile = gameFile;
			this.round = gameFile.CurrentRound;

			biller = new AssessmentsBiller (gameFile);
			BuildFixedContents();
			BuildContents(biller);
		}

		void BuildFixedContents ()
		{
			this.SuspendLayout();

			this.AutoScroll = true;
			this.AutoScrollPosition = new Point (0, 0);

			boldFont = CoreUtils.SkinningDefs.TheInstance.GetFont(10, FontStyle.Bold);
			normalFont = CoreUtils.SkinningDefs.TheInstance.GetFont(10);

			roundSelector = new ComboBox ();
			roundSelector.DropDownStyle = ComboBoxStyle.DropDownList;
			for (int i = 1; i <= SkinningDefs.TheInstance.GetIntData("roundcount", 5); i++)
			{
				roundSelector.Items.Add("Round " + CONVERT.ToStr(i));
			}
			roundSelector.SelectedIndexChanged += roundSelector_SelectedIndexChanged;
			roundSelector.Location = new Point (30, 0);
			this.Controls.Add(roundSelector);

			int headerY = 50;

			Label nameHeading = new Label ();
			nameHeading.Text = "Assessment";
			nameHeading.Font = boldFont;
			nameHeading.Width = costTab - nameTab;
			nameHeading.Location = new Point (nameTab, headerY);

			Label costHeading = new Label ();
			costHeading.Text = "Cost";
			costHeading.Font = boldFont;
			costHeading.Width = checkTab - costTab;
			costHeading.Location = new Point (costTab, headerY);

			this.Controls.Add(nameHeading);
			this.Controls.Add(costHeading);

			this.ResumeLayout(false);

			assessmentPanels = new ArrayList ();

			runButton = SkinningDefs.TheInstance.CreateWindowsButton();
			runButton.Name = "runButton Button";
			runButton.Text = "Run assessments";
			runButton.Size = new Size (100, runButton.Height);
			runButton.Click += runButton_Click;
			this.Controls.Add(runButton);

			int showRound;
			if (gameFile.CurrentPhase == GameFile.GamePhase.TRANSITION)
			{
				showRound = gameFile.CurrentRound - 1;
			}
			else
			{
				showRound = gameFile.CurrentRound;
			}
			roundSelector.SelectedIndex = Math.Max(0, Math.Min(4, showRound - 1));
		}

		void BuildContents (AssessmentsBiller biller)
		{
			this.SuspendLayout();

			this.AutoScroll = true;
			this.AutoScrollPosition = new Point (0, 0);

			foreach (AssessmentPanel panel in assessmentPanels)
			{
				this.Controls.Remove(panel);
			}
			assessmentPanels.Clear();
			foreach (Assessment assessment in biller.assessments)
			{
				AssessmentPanel assessmentPanel = new AssessmentPanel (assessment, round, boldFont, normalFont, nameTab, costTab, checkTab);
				assessmentPanels.Add(assessmentPanel);
				this.Controls.Add(assessmentPanel);
			}

			this.ResumeLayout(false);

			DoLayout();
		}

		public void DoLayout ()
		{
			ArrayList selection = biller.LoadSelectionFile(round, false);

			// We can edit the options provided we've not already chosen some...
			bool enableEdit = (selection.Count == 0) &&
			// ...and we're in either the ops phase, or the following transition phase.
			                  (((round == gameFile.CurrentRound) && (gameFile.CurrentPhase == GameFile.GamePhase.OPERATIONS))
			                  || (((round + 1) == gameFile.CurrentRound) && (gameFile.CurrentPhase == GameFile.GamePhase.TRANSITION)));

			this.AutoScrollPosition = new Point (0, 0);

			int y = 100;

			foreach (AssessmentPanel assessmentPanel in assessmentPanels)
			{
				assessmentPanel.Location = new Point (0, y);

				if (selection.Count > 0)
				{
					bool selected = (selection.IndexOf(assessmentPanel.AssessmentName) != -1);
					assessmentPanel.check.Checked = selected;
					assessmentPanel.revealed = selected;
				}

				int height = 50;
				foreach (Label child in assessmentPanel.actionLabels)
				{
					child.Visible = assessmentPanel.revealed;

					if (assessmentPanel.revealed)
					{
						height = Math.Max(height, child.Bottom + 10);
					}
				}

				assessmentPanel.check.Enabled = enableEdit;

				assessmentPanel.Size = new Size (800, height);
				y = assessmentPanel.Bottom;
			}

			runButton.Enabled = enableEdit;
			runButton.Location = new Point (550, y);
			Size = new Size (800, runButton.Bottom + 20);

			this.AutoScroll = true;
		}

		void runButton_Click (object sender, EventArgs e)
		{
			ArrayList selection = new ArrayList ();

			foreach (AssessmentPanel panel in assessmentPanels)
			{
				panel.revealed = panel.check.Checked;

				if (panel.check.Checked)
				{
					selection.Add(panel.AssessmentName);
				}
			}

			biller.OutputSelectionFile(round, selection);

			DoLayout();
		}

		void roundSelector_SelectedIndexChanged (object sender, EventArgs e)
		{
			round = (1 + roundSelector.SelectedIndex);
			BuildContents(biller);
		}
	}
}