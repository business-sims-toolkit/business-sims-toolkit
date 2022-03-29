using System.Windows.Forms;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using CommonGUI;
using GameManagement;
using LibCore;
using ReportBuilder;
using Charts;
using CoreUtils;

namespace NetworkScreens
{
	/// <summary>
	/// Summary description for BoardScreen.
	/// </summary>
	public class ToolsScreen : ToolsScreenBase
	{
        protected TabBar tabBar;

		public enum PanelToShow
		{
			Board,
			Maturity,
			Costs,
			Options,
			Assessments,
			ResourceLevels,
			NetworkReport,
			PathfinderSurvey,
			CloudScores,
			CloudLogScreen,
			SupportCosts
		}
		protected PanelToShow PanelSelected;

		protected ScoreCardWizardBase scorecard;
		protected PathFinderSurveyCardWizard path_survey_card;
		protected CloudScoreCardWizard cloudScoreCard;
		protected GameBoardView.GameBoardViewWithController board;
		protected CostsEditor costs;
		protected NetworkProgressionGameFile _gameFile;
		protected FacilitatorOptionsScreen options;
		protected Table supportCostsTable;

		protected AssessmentsScreen assessmentsPanel = null;

		protected List<RoundScores> roundScores;
		protected SupportSpendOverrides spendOverrides;

		protected bool enableOptions;

		protected virtual void BuildBoardView()
		{
			board = new GameBoardView.GameBoardViewWithController(_gameFile.NetworkModel);
			Controls.Add(board);
		}

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (scorecard != null) scorecard.Dispose();
                if (path_survey_card != null) path_survey_card.Dispose();
                if (costs != null) costs.Dispose();
                if (options != null) options.Dispose();
                if (board != null) board.Dispose();
                if (supportCostsTable != null) supportCostsTable.Dispose();
                if (cloudScoreCard != null) cloudScoreCard.Dispose();
            }
        }

		public ToolsScreen (NetworkProgressionGameFile gameFile, Control gamePanel, bool enableOptions,SupportSpendOverrides spendOverrides)
		{
			_gameFile = gameFile;
			this.enableOptions = enableOptions;
			this.spendOverrides = spendOverrides;
			this.SuspendLayout();

			BuildBoardView();

			GetRoundScores();

            if (scorecard != null)
            {
                scorecard.Dispose();
            }
             
            if (gameFile.Game_Eval_Type == em_GameEvalType.ISO_20K)
            {
                scorecard = new ISO20kScoreCardWizard(gameFile);
            }
            else
            {
                scorecard = new ScoreCardWizard(gameFile);
            }
			Controls.Add(scorecard);
            scorecard.Select();

		    path_survey_card = CreatePathFinderSurveyCardWizard();
			if (path_survey_card != null)
			{
				path_survey_card.BackColor = Color.White;
				Controls.Add(path_survey_card);
			}

			Build_CloudScoreCardWizard();

			BuildCloudLogScreen();

			costs = CreateCostsEditor(gameFile);
			Controls.Add(costs);

			options = new FacilitatorOptionsScreen (gamePanel, gameFile);
			this.Controls.Add(options);

			BuildTabBar();

			this.ResumeLayout(false);

			ShowDefaultPanel();
		}

		protected virtual void BuildTabBar ()
		{
			tabBar = new TabBar ();
			Controls.Add(tabBar);

			if (_gameFile.GetBoolGlobalOption("it_present", true))
			{
				tabBar.AddTab("Board View", (int) PanelToShow.Board, true);
			}
			tabBar.AddTab("Maturity Scores", (int) PanelToShow.Maturity, true);

			tabBar.AddTab("Costs Editor", (int) PanelToShow.Costs, true);
			if (!SkinningDefs.TheInstance.GetBoolData("hide_support_costs", false))
			{
				tabBar.AddTab("Support Costs", (int) PanelToShow.SupportCosts, true);
			}

			if (enableOptions)
			{
				tabBar.AddTab("Options", (int) PanelToShow.Options, true);
			}

			tabBar.TabPressed += tabBar_TabPressed;
		}

		protected virtual CostsEditor CreateCostsEditor (NetworkProgressionGameFile gameFile)
		{
			return new CostsEditor (gameFile);
		}

        protected virtual PathFinderSurveyCardWizard CreatePathFinderSurveyCardWizard()
        {
            return new PathFinderSurveyCardWizard(_gameFile);
        }

		protected virtual void GetRoundScores ()
		{
			roundScores = new List<RoundScores> ();

			int oldProfit = 0;
			int oldServices = 0;

			for (int i = 1; i <= _gameFile.LastRoundPlayed; i++)
			{
				RoundScores scores = new RoundScores (_gameFile, i, oldProfit, oldServices, spendOverrides);
				roundScores.Add(scores);

				oldProfit = scores.Profit;
				oldServices = scores.NumNewServices;
			}
		}

		protected virtual void Build_CloudScoreCardWizard()
		{
			cloudScoreCard = new CloudScoreCardWizard(_gameFile);
			this.Controls.Add(cloudScoreCard);
		}

		protected virtual void BuildCloudLogScreen()
		{
		}

		protected virtual void disposeCloudLogScreen()
		{
		}
																																																																																																																																																																																																			
		protected virtual void showCloudLogScreen()
		{
		}

		protected virtual void hideCloudLogScreen()
		{
		}

		protected virtual void ResizeCloudLogScreen(Point newLocation, Size newSize)
		{
		}

		protected virtual void RefreshCloudLog()
		{
		}

		protected virtual void ShowDefaultPanel ()
		{
			PanelSelected = PanelToShow.Board;
			RefreshScreen();
		}

		public void RemoveTab (PanelToShow tab)
		{
			tabBar.RemoveTabByCode((int) tab);
		}

		protected void RefreshAssessments ()
		{
			assessmentsPanel = new AssessmentsScreen (_gameFile);
			assessmentsPanel.Location = new Point (0, tabBar.Bottom);
			assessmentsPanel.Size = new Size (this.Width, this.Height - assessmentsPanel.Top);
			assessmentsPanel.Visible = false;

			this.Controls.Add(assessmentsPanel);
		}

		public void AddAssessmentsTab ()
		{
			if (assessmentsPanel != null)
			{
				this.Controls.Remove(assessmentsPanel);
			}

			RefreshAssessments();
		}		

		public override void readNetwork()
		{
			board.ReadNetwork(_gameFile.NetworkModel);
		}

		protected override void DoSize()
		{
		    if (SkinningDefs.TheInstance.GetBoolData("fullsize_tabbar", false))
		    {
		        int tabBarOffset = SkinningDefs.TheInstance.GetIntData("tabbar_offset", 8);
		        tabBar.Size = new Size(this.Width - (tabBarOffset * 2), 30);
		        tabBar.Left = tabBarOffset;
		    }
		    else
		    {
		        tabBar.Size = new Size(this.Width - 307, 30);
		    }

		    if (board != null)
			{
				board.Size = new Size(this.Width, this.Height - 30);
				board.Location = new Point(0, 30);
			}

			scorecard.Size = new Size(this.Width,this.Height-30);
			scorecard.Location = new Point(0,30);
            scorecard.DoLayout();

			if (path_survey_card != null)
			{
				path_survey_card.Size = new Size(this.Width, this.Height - 30);
				path_survey_card.Location = new Point(0, 30);
			}

			cloudScoreCard.Size = new Size (Width, Height - 30);
			cloudScoreCard.Location = new Point (0, 30);

			ResizeCloudLogScreen(new Point(0, 30), new Size(Width, Height - 30));

			costs.Size = new Size(this.Width,this.Height-30);
			costs.Location = new Point(0,30);

			options.Size = new Size (this.Width, this.Height - 30);
			options.Location = new Point (0, 30);

			if (assessmentsPanel != null)
			{
				assessmentsPanel.Location = new Point (0, 30);
				assessmentsPanel.Size = new Size (this.Width - assessmentsPanel.Left, this.Height - assessmentsPanel.Top);
			}

		    if (supportCostsTable != null)
		    {
		        supportCostsTable.Location = new Point(50, 50);
		        supportCostsTable.Size = new Size(Width - (2 * supportCostsTable.Left), Height - (2 * supportCostsTable.Top));
            }
        }

		protected virtual void tabBar_TabPressed(object sender, TabBarEventArgs args)
		{
			PanelSelected = (PanelToShow) args.Code;
			RefreshScreen();
		}

		protected virtual void RefreshScreen ()
		{
			switch ((PanelToShow) PanelSelected)
			{
				case PanelToShow.Board:
					scorecard.Hide();
					costs.Hide();

					if (board != null)
					{
						board.Show();
					}
					options.Hide();
					path_survey_card?.Hide();
					cloudScoreCard.Hide();
					if (assessmentsPanel != null)
					{
						assessmentsPanel.Hide();
					}
					hideCloudLogScreen();
					if (supportCostsTable != null)
					{
						supportCostsTable.Hide();
					}
					break;

				case PanelToShow.Maturity:
					scorecard.Show();
					costs.Hide();
                    scorecard.Select();
                    scorecard.BringToFront();
					if (board != null)
					{
						board.Hide();
					}
					options.Hide();
					path_survey_card?.Hide();
					cloudScoreCard.Hide();
					if (assessmentsPanel != null)
					{
						assessmentsPanel.Hide();
					}
					hideCloudLogScreen();
					if (supportCostsTable != null)
					{
						supportCostsTable.Hide();
					}
					break;

				case PanelToShow.Costs:
					scorecard.Hide();
					costs.Show();
					if (board != null)
					{
						board.Hide();
					}
					options.Hide();
					path_survey_card?.Hide();
					cloudScoreCard.Hide();
					if (assessmentsPanel != null)
					{
						assessmentsPanel.Hide();
					}
					hideCloudLogScreen();
					if (supportCostsTable != null)
					{
						supportCostsTable.Hide();
					}
					break;

				case PanelToShow.Options:
					options.Show();
					scorecard.Hide();
					costs.Hide();
					if (board != null)
					{
						board.Hide();
					}
					path_survey_card.Hide();
					cloudScoreCard.Hide();
					if (assessmentsPanel != null)
					{
						assessmentsPanel.Hide();
					}
					hideCloudLogScreen();
					if (supportCostsTable != null)
					{
						supportCostsTable.Hide();
					}
					break;

				case PanelToShow.Assessments:
					scorecard.Hide();
					costs.Hide();
					if (board != null)
					{
						board.Hide();
					}
					options.Hide();
					path_survey_card.Hide();
					RefreshAssessments();
					cloudScoreCard.Hide();
					assessmentsPanel.Show();
					hideCloudLogScreen();
					if (supportCostsTable != null)
					{
						supportCostsTable.Hide();
					}
					break;

				case PanelToShow.PathfinderSurvey:
					scorecard.Hide();
					path_survey_card.Show();
					costs.Hide();
					if (board != null)
					{
						board.Hide();
					}
					options.Hide();
					cloudScoreCard.Hide();
					if (assessmentsPanel != null)
					{
						assessmentsPanel.Hide();
					}
					hideCloudLogScreen();
					if (supportCostsTable != null)
					{
						supportCostsTable.Hide();
					}
					break;

				case PanelToShow.CloudScores:
					scorecard.Hide();
					path_survey_card.Hide();
					costs.Hide();
					if (board != null)
					{
						board.Hide();
					}
					options.Hide();
					cloudScoreCard.Show();
					if (assessmentsPanel != null)
					{
						assessmentsPanel.Hide();
					}
					hideCloudLogScreen();
					cloudScoreCard.Show();
					if (supportCostsTable != null)
					{
						supportCostsTable.Hide();
					}
					break;

				case PanelToShow.CloudLogScreen:
					scorecard.Hide();
					path_survey_card.Hide();
					costs.Hide();
					if (board != null)
					{
						board.Hide();
					}
					options.Hide();
					cloudScoreCard.Hide();
					if (assessmentsPanel != null)
					{
						assessmentsPanel.Hide();
					}
					showCloudLogScreen();
					if (supportCostsTable != null)
					{
						supportCostsTable.Hide();
					}
					break;

				case PanelToShow.SupportCosts:
					scorecard.Hide();
					path_survey_card.Hide();
					costs.Hide();
					if (board != null)
					{
						board.Hide();					
					}
					options.Hide();
					cloudScoreCard.Hide();
					if (assessmentsPanel != null)
					{
						assessmentsPanel.Hide();
					}
					hideCloudLogScreen();
					ShowSupportCosts();
					break;
			}

			if ((supportCostsTable != null)
				&& ! supportCostsTable.Visible)
			{
				supportCostsTable.DisposeEditBox();
			}
		}

		public override void RefreshSupportCosts ()
		{
			if (supportCostsTable != null)
			{
				Controls.Remove(supportCostsTable);
				supportCostsTable.Dispose();
				supportCostsTable = null;
			}

			GetRoundScores();

			ArrayList roundScoresArrayList = new ArrayList(roundScores);
			OpsSupportCostsReport report = new OpsSupportCostsReport(roundScoresArrayList, spendOverrides);

			List<string> roundResultsFiles = new List<string>();
			for (int round = 1; round <= _gameFile.LastRoundPlayed; round++)
			{
				roundResultsFiles.Add(report.BuildReport(_gameFile, round, roundScores[round - 1]));
			}

			if (_gameFile.LastRoundPlayed > 0)
			{
				string combinedResultsFile = report.CombineRoundResults(roundResultsFiles.ToArray(), _gameFile,
					_gameFile.LastRoundPlayed);

				supportCostsTable = new Table();
				supportCostsTable.LoadData(System.IO.File.ReadAllText(combinedResultsFile));
				Controls.Add(supportCostsTable);

				supportCostsTable.Selectable = true;
				supportCostsTable.CellTextChanged += supportCostsTable_CellTextChanged;

				supportCostsTable.Location = new Point(50, 50);
				supportCostsTable.Size = new Size(Width - (2 * supportCostsTable.Left), Height - (2 * supportCostsTable.Top));
				supportCostsTable.AutoScroll = true;
			}
		}


		protected virtual void ShowSupportCosts ()
		{
			if (supportCostsTable != null)
			{
				Controls.Remove(supportCostsTable);
				supportCostsTable.Dispose();
				supportCostsTable = null;
			}

			GetRoundScores();

			ArrayList roundScoresArrayList = new ArrayList(roundScores);
			OpsSupportCostsReport report = new OpsSupportCostsReport (roundScoresArrayList, spendOverrides);

			List<string> roundResultsFiles = new List<string> ();
			for (int round = 1; round <= _gameFile.LastRoundPlayed; round++)
			{
				roundResultsFiles.Add(report.BuildReport(_gameFile, round, roundScores[round - 1]));
			}

			if (_gameFile.LastRoundPlayed > 0)
			{
				string combinedResultsFile = report.CombineRoundResults(roundResultsFiles.ToArray(), _gameFile,
					_gameFile.LastRoundPlayed);

				supportCostsTable = new Table();
				supportCostsTable.LoadData(System.IO.File.ReadAllText(combinedResultsFile));
				Controls.Add(supportCostsTable);

				supportCostsTable.Selectable = true;
				supportCostsTable.CellTextChanged += supportCostsTable_CellTextChanged;

				supportCostsTable.AutoScroll = true;
				supportCostsTable.BringToFront();
			}

            DoSize();
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
			Build_CloudScoreCardWizard();

		    if (_gameFile.Game_Eval_Type == em_GameEvalType.ISO_20K)
		    {
                scorecard = new ISO20kScoreCardWizard(_gameFile);
		    }
		    else
		    {
                scorecard = new ScoreCardWizard(_gameFile);
		    }
		    Controls.Add(scorecard);
		    scorecard.Select();
            scorecard.Show();
            
            DoSize();
			ResumeLayout(false);
		}

		public void RefreshPathfinderSurveyScoreSet ()
		{
			SuspendLayout();
			Controls.Remove(path_survey_card);
			path_survey_card = new PathFinderSurveyCardWizard(_gameFile);
			Controls.Add(path_survey_card);
			DoSize();
			ResumeLayout(false);
		}

		public void RefreshCloudScoreSet()
		{
			SuspendLayout();
			Controls.Remove(cloudScoreCard);
			Build_CloudScoreCardWizard();
			DoSize();
			ResumeLayout(false);
		}

		void supportCostsTable_CellTextChanged (Table sender, TextTableCell cell, string val)
		{
			spendOverrides.SetOverride(cell.CellName, val);
			spendOverrides.Save();
		}

		public override void DisposeEditBox ()
		{
			if (supportCostsTable != null)
			{
				supportCostsTable.DisposeEditBox();
			}
		}

		public override void RemoveInfrastructureBoard ()
		{
			tabBar.RemoveTabByCode((int) PanelToShow.Board);
		}

		public override void SelectTab (string tabName)
		{
			tabBar.SelectedTabName = tabName;
		}
	}
}