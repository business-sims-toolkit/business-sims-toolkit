using System;
using System.Windows.Forms;
using System.Drawing;

using CommonGUI;
using GameManagement;
using Charts;
using ReportBuilder;

using CoreUtils;

using System.Collections;

namespace NetworkScreens
{
	/// <summary>
	/// EditCostsToolsScreen is an updated tools screen initally for CA and Microsoft where you
	/// don't have the board view but you have the screen the Facilitator has to edit instead.
	/// </summary>
	public class EditCostsToolsScreen : ToolsScreenBase
	{
		protected TabBar tabBar = new TabBar();

		public enum PanelToShow
		{
			Maturity,
			Costs,
			Options,
			Assessments
		}
		protected PanelToShow PanelSelected;

		protected ScoreCardWizard scorecard;
		protected Panel edit_costs_panel;

		protected CostsEditor costs;
		protected NetworkProgressionGameFile _gameFile;
		protected FacilitatorOptionsScreen options;

		protected Table Costs;

		protected ArrayList Scores;
		protected SupportSpendOverrides spend_overrides;
		protected string _left_title = "";

		protected AssessmentsScreen assessmentsPanel = null;

		protected virtual void GetRoundScores()
		{
			Scores = new ArrayList();

			int prevprofit = 0;
			int newservices = 0;
			for (int i=1; i<=_gameFile.LastRoundPlayed; i++)
			{
				RoundScores score = new RoundScores(_gameFile, i, prevprofit, newservices, spend_overrides);
				Scores.Add(score);
				prevprofit = score.Profit;
				newservices = score.NumNewServices;
			}
		}

		public EditCostsToolsScreen(string leftTitle, NetworkProgressionGameFile gameFile, SupportSpendOverrides _spend_overrides, Control gamePanel)
		{
			_left_title = leftTitle;
			Setup(gameFile, _spend_overrides, gamePanel);
		}

		public EditCostsToolsScreen(NetworkProgressionGameFile gameFile, SupportSpendOverrides _spend_overrides, Control gamePanel)
		{
			Setup(gameFile, _spend_overrides, gamePanel);
		}

		protected virtual void RefreshCosts()
		{
			GetRoundScores();
			
			if (Costs != null)
			{
				edit_costs_panel.SuspendLayout();
				edit_costs_panel.Controls.Remove(Costs);
				edit_costs_panel.ResumeLayout(false);

				Costs.Dispose();
			}

			{
				Costs = new Table();

				Costs.Location = new Point(30,50);
				Costs.Size = new Size(this.Width - 80,512);
				Costs.AutoScroll = true;
				Costs.BorderStyle = BorderStyle.FixedSingle;
				Costs.Selectable = true;
				Costs.CellTextChanged += Costs_CellTextChanged;

				edit_costs_panel.SuspendLayout();
				edit_costs_panel.Controls.Add(Costs);
				edit_costs_panel.ResumeLayout(false);
			}

			OpsSupportCostsReport costs_report = new OpsSupportCostsReport(Scores, spend_overrides);
			costs_report.SetLeftColumnHeader(_left_title);

			string[] xmldataArray = new string[_gameFile.LastRoundPlayed];
			for(int i = 0; i < _gameFile.LastRoundPlayed; i++)
			{
				if (i < Scores.Count)
				{
					xmldataArray[i] = costs_report.BuildReport(_gameFile, i+1, (RoundScores)Scores[i]);
				}
			}

			System.IO.StreamReader file = new System.IO.StreamReader(costs_report.CombineRoundResults(xmldataArray, _gameFile, _gameFile.LastRoundPlayed));
			string xmldata = file.ReadToEnd();
			file.Close();
			file = null;

			Costs.LoadData(xmldata);
		}

		protected virtual void Setup(NetworkProgressionGameFile gameFile, SupportSpendOverrides _spend_overrides, Control gamePanel)
		{
			spend_overrides = _spend_overrides;
			_gameFile = gameFile;

			GetRoundScores();

			this.SuspendLayout();

			scorecard = new ScoreCardWizard(gameFile);
			this.Controls.Add(scorecard);

			edit_costs_panel = new Panel();
	
			this.Controls.Add(edit_costs_panel);

			options = new FacilitatorOptionsScreen (gamePanel, gameFile);
			this.Controls.Add(options);

			edit_costs_panel.SuspendLayout();
			costs = new CostsEditor(gameFile);
			edit_costs_panel.Controls.Add(costs);

			///////
			///
			if(null != Costs) Costs.Flush();
			OpsSupportCostsReport costs_report = new OpsSupportCostsReport(Scores, spend_overrides);
			costs_report.SetLeftColumnHeader(_left_title);

			string[] xmldataArray = new string[_gameFile.LastRoundPlayed];
			for(int i = 0; i < _gameFile.LastRoundPlayed; i++)
			{
				if (i < Scores.Count)
				{
					xmldataArray[i] = costs_report.BuildReport(_gameFile, i+1, (RoundScores)Scores[i]);
				}
			}

			Costs = new Table();

			System.IO.StreamReader file = new System.IO.StreamReader(costs_report.CombineRoundResults(xmldataArray, _gameFile, _gameFile.LastRoundPlayed));
			string xmldata = file.ReadToEnd();
			file.Close();
			file = null;

			Costs.LoadData(xmldata);

			Costs.Selectable = true;
			Costs.CellTextChanged += Costs_CellTextChanged;

			Costs.Location = new Point(30,200); // was (30, 50);
			Costs.Size = new Size(this.Width - 80,512);
			Costs.AutoScroll = true;
			Costs.BorderStyle = BorderStyle.FixedSingle;

			edit_costs_panel.Controls.Add(Costs);

			edit_costs_panel.ResumeLayout(false);

			this.Controls.Add(tabBar);

			tabBar.AddTab("Process Maturity Scores", (int) PanelToShow.Maturity, true);
			tabBar.AddTab("Support Costs Baseline", (int) PanelToShow.Costs, true);
//			tabBar.AddTab("Options", (int) PanelToShow.Options, true);

			tabBar.TabPressed += tabBar_TabPressed;
			this.Resize += ToolsScreen_Resize;

			this.ResumeLayout(false);

			PanelSelected = 0;

			RefreshScreen();
		}

		public void AddAssessmentsTab ()
		{
			tabBar.AddTab("Assessments", (int) PanelToShow.Assessments, true);

			if (assessmentsPanel == null)
			{
				assessmentsPanel = new AssessmentsScreen (_gameFile);
				assessmentsPanel.Location = new Point (0, tabBar.Bottom);
				assessmentsPanel.Size = new Size (this.Width, this.Height - assessmentsPanel.Top);

				this.Controls.Add(assessmentsPanel);
			}

			assessmentsPanel.Visible = false;
		}		

		protected void ToolsScreen_Resize(object sender, EventArgs e)
		{
			DoSize();
		}

		protected override void DoSize()
		{
			tabBar.Size = new Size(this.Width-307,30);

			scorecard.Size = new Size(this.Width,this.Height-30);
			scorecard.Location = new Point(0,30);

			edit_costs_panel.Size = new Size(this.Width,this.Height-30);
			edit_costs_panel.Location = new Point(0,30);

			int costs_height = SkinningDefs.TheInstance.GetIntData("top_costs_height", 130);
			// Set to 180 in IBM.

			costs.Size = new Size(this.Width-10, costs_height); // was (this.Width-10, 130)
			costs.Location = new Point(5,0);

			Costs.Location = new Point(5,costs_height); // was (5,140)
			Costs.Size = new Size(this.Width-10,this.Height-Costs.Top);
			Costs.BorderStyle = BorderStyle.None;

			options.Size = new Size (this.Width, this.Height - 30);
			options.Location = new Point (0, 30);

			if (assessmentsPanel != null)
			{
				assessmentsPanel.Location = new Point (0, tabBar.Bottom);
				assessmentsPanel.Size = new Size (this.Width, this.Height - assessmentsPanel.Top);
			}
		}

		protected void tabBar_TabPressed(object sender, TabBarEventArgs args)
		{
			PanelSelected = (PanelToShow) args.Code;
			RefreshScreen();
		}

		protected virtual void RefreshScreen ()
		{
			switch (PanelSelected)
			{
				case PanelToShow.Maturity:
					scorecard.Show();
					edit_costs_panel.Hide();
					options.Hide();

					if (assessmentsPanel != null)
					{
						assessmentsPanel.Hide();
					}
					break;

				case PanelToShow.Costs:
					scorecard.Hide();
					RefreshCosts();
					DoSize();
					edit_costs_panel.Show();
					options.Hide();
					if (assessmentsPanel != null)
					{
						assessmentsPanel.Hide();
					}
					break;

				case PanelToShow.Options:
					scorecard.Hide();
					edit_costs_panel.Hide();
					options.Show();
					if (assessmentsPanel != null)
					{
						assessmentsPanel.Hide();
					}
					break;

				case PanelToShow.Assessments:
					scorecard.Hide();
					edit_costs_panel.Hide();
					options.Hide();
					assessmentsPanel.Show();
					break;
			}

			if ((Costs != null)
				&& ! Costs.Visible)
			{
				Costs.DisposeEditBox();
			}
		}

		protected void Costs_CellTextChanged(Table sender, TextTableCell cell, string val)
		{
			spend_overrides.SetOverride(cell.CellName, val);
			spend_overrides.Save();

			GetRoundScores();
			RefreshScreen();
		}

		public override void ResetView ()
		{
			RefreshScreen();
		}

		public override void RemoveTabByCode (int tabCode)
		{
			tabBar.RemoveTabByCode(tabCode);
		}

		public override void RemoveTabByName (string tabName)
		{
			tabBar.RemoveTab(tabName);
		}

		public override void RefreshMaturityScoreSet ()
		{
			SuspendLayout();
			Controls.Remove(scorecard);
			scorecard = new ScoreCardWizard(_gameFile);
			Controls.Add(scorecard);
			DoSize();
			ResumeLayout(false);
		}
	}
}