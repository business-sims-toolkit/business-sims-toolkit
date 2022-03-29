using System;
using System.Windows.Forms;
using System.Drawing;
using System.IO;

using CommonGUI;
using ChartScreens;
using ReportBuilder;
using LibCore;
using Network;
using Charts;
using System.Collections;
using System.Collections.Generic;
using Logging;
using GameManagement;
using CoreUtils;
using Polestar_PM.OpsGUI;

namespace Polestar_PM.ReportsScreen
{
	public class PM_TabbedChartScreen : PureTabbedChartScreen
	{
		//protected LogoPanelBase logoPanel;
		protected NetworkProgressionGameFile _gameFile;

		protected System.Windows.Forms.Panel pnlMain;
		protected System.Windows.Forms.Panel pnlLeaderBoard;
		protected System.Windows.Forms.Panel pnlScoreCard;
		protected System.Windows.Forms.Panel pnlProjectSpend;
		protected System.Windows.Forms.Panel pnlGameScreen;
		protected System.Windows.Forms.Panel pnlProjectPerformance;
		protected System.Windows.Forms.Panel pnlOpsChartScreen;
		protected System.Windows.Forms.Panel pnlResourceLevels;
		protected System.Windows.Forms.Panel pnlBubbles;
		protected System.Windows.Forms.Panel pnl_ProjectsBenefitChart;

		protected bool RedrawLeaderBoard = true;
		protected bool RedrawScoreCard = true;
		protected bool RedrawGameScreen = false;
		protected bool RedrawProjectPerformance = false;
		protected bool RedrawOpsChart = false;
		protected bool RedrawResourceLevels = false;
		protected bool RedrawBubbles = false;
		protected bool RedrawProjectSpend = false;
		protected bool RedrawProgramBenefits = false;

		protected ArrayList Scores;
		
		protected TabBar tabBar = new TabBar();
		protected TabBar scorecardTabs = new TabBar();

		//Game Screen Extra Controls
		protected Panel psd_ProjectsDisplay = null;
		protected GameStatsDisplay psd_GameStatsDisplay;
		protected GameCommsDisplay psd_GameCommsDisplay;
		protected GameTargetsDisplay psd_GameTargetsDisplay;
		protected ComboBox cmb_gamescreen;
		protected int gameScreenLastChangedWhenPlayingRound = -1;

		//Bubbles Extra Controls 
		protected PictureBox bubble_view;
		protected ComboBox cmb_bubble;
		protected int bubbleChartLastChangedWhenPlayingRound = -1;

		//Project Performance Screen Extra Controls
		protected PrjPerfContainer ppc_PrjPerfContainer = null;
		protected ComboBox cmb_projectPerfRound;
		protected int projectPerformanceLastChangedWhenPlayingRound = -1;
		
		//OpsGanttChart Screen Extra Controls
		protected OpsGanttContainer ogc_OpsGanntContainer = null;
		protected ComboBox cmb_opsGanttRound;
		protected int OpsGanttLastChangedWhenPlayingRound = -1;

		//Resource Level Screen Extra Controls
		protected ResourceLevelsContainer res_ResourceLevelsContainer = null;
		protected int ResLevelLastChangedWhenPlayingRound = -1;

		// Program benefit chart controls
		//ComboBox cmbProgramBenefitMetric;

		ComboBox cmbLeaderboardRound;
		Table leaderboardTable;
		Table scoreCardTable;
		//LineGraph programBenefitChart;
		//PortfolioAchievementsContainer myProjectsBenefitChart  = null;
		PM_ProjectsBenefitsByDayContainer myProjectsBenefitChart  = null;
		protected ComboBox cmb_opsProjectsBenefit;
		protected int ProjectsBenefitLastChangedWhenPlayingRound = -1;

		bool redrawPerception;
		Panel perceptionPanel;
		GroupedBarChart perceptionBarChart;
		ComboBox perceptionRoundSelector;

		bool redrawMaturity;
		Panel maturityPanel;
		PieChart maturityChart;
		ComboBox maturityRoundSelector;

		bool dont_handle = false;

		SupportSpendOverrides supportOverrides;

		public PM_TabbedChartScreen (NetworkProgressionGameFile gameFile, SupportSpendOverrides _spend_overrides)
			: this (gameFile, _spend_overrides, null)
		{
		}

		public PM_TabbedChartScreen(NetworkProgressionGameFile gameFile, SupportSpendOverrides _spend_overrides, TacPermissions tacPermissions)
		{
			_gameFile = gameFile;
			this.SuspendLayout();
			tabBar.AddTab("Game Screen", 1, true);
			tabBar.AddTab("Leaderboard", 2, true);
			tabBar.AddTab("Scorecard", 3, true);
			tabBar.AddTab("Projects", 4, true);
			tabBar.AddTab("Maturity", 9, true);

			if (true)
			{
				tabBar.AddTab("Ops Chart", 5, true);
			}

			tabBar.AddTab("Resource Levels", 6, true);
			tabBar.AddTab("Portfolio", 7, true);

			tabBar.Location = new Point(5,0);
			this.Controls.Add(tabBar);

			this.ResumeLayout(false);

			this.BackColor = Color.White;

			InitialisePanels();

			_gameFile = gameFile;
			supportOverrides = _spend_overrides;
			GetRoundScores();
			this.VisibleChanged += new EventHandler(PM_TabbedChartScreen_VisibleChanged);
			tabBar.TabPressed += new CommonGUI.TabBar.TabBarEventArgsHandler(tabBar_TabPressed);
			this.Resize += new EventHandler(TabbedChartScreen_Resize);

			ReloadDataAndShow(true);
		}

		protected virtual void InitialisePanels()
		{
			pnlMain = new Panel();
			pnlLeaderBoard = new Panel();
			pnlScoreCard = new Panel();
			pnlGameScreen = new Panel();
			pnlProjectPerformance = new Panel();
			pnlOpsChartScreen = new Panel();
			pnlResourceLevels = new Panel();
			pnlBubbles = new Panel();
			pnlProjectSpend = new Panel();
			pnl_ProjectsBenefitChart = new Panel ();
			perceptionPanel = new Panel ();
			maturityPanel = new Panel ();

			pnlMain.BackColor = System.Drawing.Color.White;		

			this.SuspendLayout();
			pnlMain.SuspendLayout();
			perceptionPanel.SuspendLayout();

			pnlMain.BackColor = Color.White;
			pnlLeaderBoard.BackColor = Color.White;
			pnlScoreCard.BackColor = Color.White;
			pnlGameScreen.BackColor = Color.White;
			pnlProjectPerformance.BackColor = Color.White;
			pnlOpsChartScreen.BackColor = Color.White;
			pnlResourceLevels.BackColor = Color.White;
			pnlBubbles.BackColor = Color.White;
			pnlProjectSpend.BackColor = Color.White;
			pnl_ProjectsBenefitChart.BackColor = Color.White;

			pnlGameScreen.BackColor = Color.FromArgb(218,218,203);
			pnlGameScreen.BackColor = Color.White;
			
			pnlProjectPerformance.BackColor = Color.White;

			this.pnlMain.Controls.Add(pnlLeaderBoard);
			this.pnlMain.Controls.Add(pnlScoreCard);
			this.pnlMain.Controls.Add(pnlGameScreen);
			this.pnlMain.Controls.Add(pnlProjectPerformance);
			this.pnlMain.Controls.Add(pnlOpsChartScreen);
			this.pnlMain.Controls.Add(pnlResourceLevels);
			this.pnlMain.Controls.Add(pnlBubbles);
			this.pnlMain.Controls.Add(pnlProjectSpend);
			this.pnlMain.Controls.Add(pnl_ProjectsBenefitChart);
			pnlMain.Controls.Add(perceptionPanel);
			pnlMain.Controls.Add(maturityPanel);

			this.pnlMain.DockPadding.All = 4;
			this.pnlMain.Name = "pnlMain";
			this.pnlMain.TabIndex = 6;
			this.Controls.Add(this.pnlMain);

			/// ===========================================
			/// Handling the bubble stuff==================
			/// ===========================================
			this.bubble_view = new PictureBox();
			this.bubble_view.Location = new Point(0,0);
			this.bubble_view.Size = new Size(870,650);
			bubble_view.SuspendLayout();

			cmb_bubble = new ComboBox();
			cmb_bubble.DropDownStyle = ComboBoxStyle.DropDownList;
			buildBubbleComboList(cmb_bubble);
			cmb_bubble.Location = new Point(5, 1);
			cmb_bubble.Size = new Size(240,25);
			cmb_bubble.SelectedIndexChanged += new EventHandler(cmb_bubble_SelectedIndexChanged);
			cmb_bubble.SelectedIndex = Math.Min(cmb_bubble.Items.Count - 1, _gameFile.LastRoundPlayed - 1);
			cmb_bubble.Visible = false;
			//bubble_view.Controls.Add(cmb_bubble);
			bubble_view.ResumeLayout(false);
			bubble_view.Visible = true;
			this.pnlBubbles.Controls.Add(bubble_view);
			this.pnlMain.Controls.Add(cmb_bubble);

			/// ===========================================
			/// Handling the game screen===================
			/// ===========================================
			cmb_gamescreen = new ComboBox();
			cmb_gamescreen.DropDownStyle = ComboBoxStyle.DropDownList;
			for (int i = 1; i <= 3; i++)
			{
				cmb_gamescreen.Items.Add("Round " + CONVERT.ToStr(i));
			}
			cmb_gamescreen.Location = new Point(0,1);
			cmb_gamescreen.Size = new Size(170,25);
			cmb_gamescreen.SelectedIndexChanged += new EventHandler(cmb_gamescreen_SelectedIndexChanged);

			ShowGameScreenRound(1);
			this.pnlMain.Controls.Add(cmb_gamescreen);

			/// ===========================================
			/// Handling the project performance screen====
			/// ===========================================
			cmb_projectPerfRound = new ComboBox();
			cmb_projectPerfRound.DropDownStyle = ComboBoxStyle.DropDownList;
			for (int i = 1; i <= 3; i++)
			{
				cmb_projectPerfRound.Items.Add("Round " + CONVERT.ToStr(i));
			}
			cmb_projectPerfRound.Location = new Point(30, 30);
			cmb_projectPerfRound.Size = new Size(170,25);
			cmb_projectPerfRound.SelectedIndexChanged += new EventHandler(cmb_projectPerfRound_SelectedIndexChanged);

			ShowProjectPerformanceScreenRound(1);
			this.pnlMain.Controls.Add(cmb_projectPerfRound);

			// Leaderboard
			cmbLeaderboardRound = new ComboBox ();
			cmbLeaderboardRound.DropDownStyle = ComboBoxStyle.DropDownList;
			for (int i = 1; i <= 3; i++)
			{
				cmbLeaderboardRound.Items.Add("Round " + CONVERT.ToStr(i));
			}
			cmbLeaderboardRound.Items.Add("Yearly Results");
			cmbLeaderboardRound.Location = new Point (30, 30);
			cmbLeaderboardRound.Size = new Size (170, 25);
			cmbLeaderboardRound.SelectedIndexChanged += new EventHandler (cmbLeaderboardRound_SelectedIndexChanged);
			this.pnlMain.Controls.Add(cmbLeaderboardRound);
			cmbLeaderboardRound.SelectedIndex = Math.Min(cmbLeaderboardRound.Items.Count, _gameFile.LastRoundPlayed) - 1;
			cmbLeaderboardRound.BringToFront();

			/// ===========================================
			/// Handling the Ops Gantt Chart screen========
			/// ===========================================
			cmb_opsGanttRound = new ComboBox();
			cmb_opsGanttRound.DropDownStyle = ComboBoxStyle.DropDownList;
			for (int i = 1; i <= 3; i++)
			{
				cmb_opsGanttRound.Items.Add("Round " + CONVERT.ToStr(i));
			}
			cmb_opsGanttRound.Location = new Point(30, 30);
			cmb_opsGanttRound.Size = new Size(170,25);
			cmb_opsGanttRound.SelectedIndexChanged += new EventHandler(cmb_opsGanttRound_SelectedIndexChanged);

			ShowOpsGanttScreenRound(1);
			this.pnlMain.Controls.Add(cmb_opsGanttRound);


			cmb_opsProjectsBenefit = new ComboBox();
			cmb_opsProjectsBenefit.DropDownStyle = ComboBoxStyle.DropDownList;
			for (int i = 1; i <= 3; i++)
			{
				cmb_opsProjectsBenefit.Items.Add("Round " + CONVERT.ToStr(i));
			}
			cmb_opsProjectsBenefit.Location = new Point(30, 20);
			cmb_opsProjectsBenefit.Size = new Size(170, 25);
			cmb_opsProjectsBenefit.SelectedIndexChanged += new EventHandler(cmb_opsProjectsBenefit_SelectedIndexChanged);
			cmb_opsProjectsBenefit.Visible = false;
			ShowProjectBenefits(1);
			this.pnlMain.Controls.Add(cmb_opsProjectsBenefit);

			// Maturity.
			maturityRoundSelector = new ComboBox ();
			maturityRoundSelector.DropDownStyle = ComboBoxStyle.DropDownList;
			maturityPanel.Controls.Add(maturityRoundSelector);
			for (int i = 1; i <= SkinningDefs.TheInstance.GetIntData("roundcount", 5); i++)
			{
				maturityRoundSelector.Items.Add("Round " + CONVERT.ToStr(i));
			}
			maturityRoundSelector.Location = new Point(5, 1);
			maturityRoundSelector.Size = new Size(170, 25);
			maturityRoundSelector.SelectedIndexChanged += new EventHandler (maturityRoundSelector_SelectedIndexChanged);

			// Perception.
			perceptionRoundSelector = new ComboBox ();
			perceptionRoundSelector.DropDownStyle = ComboBoxStyle.DropDownList;
			perceptionPanel.Controls.Add(perceptionRoundSelector);
			for (int i = 1; i <= SkinningDefs.TheInstance.GetIntData("roundcount", 5); i++)
			{
				perceptionRoundSelector.Items.Add("Round " + CONVERT.ToStr(i));
			}
			perceptionRoundSelector.Location = new Point(5, 1);
			perceptionRoundSelector.Size = new Size(170, 25);
			perceptionRoundSelector.SelectedIndexChanged += new EventHandler (perceptionRoundSelector_SelectedIndexChanged);

			pnlMain.ResumeLayout(false);
			perceptionPanel.ResumeLayout(false);
			this.ResumeLayout(false);
		}

		protected virtual void TabbedChartScreen_Resize(object sender, EventArgs e)
		{
			DoSize();
		}

		protected virtual void DoSize()
		{
			tabBar.Size = new Size(this.Width-21, 29);

			this.pnlMain.Location = new Point(2, this.tabBar.Bottom + 2);
			this.pnlMain.Size = new Size(this.Width - 2,this.Height-40);
			perceptionPanel.Size = pnlMain.Size;
			maturityPanel.Size = pnlMain.Size;

			pnlScoreCard.Location = new Point(0, this.tabBar.Bottom + 5+2);
			pnlScoreCard.Size = new Size(this.Width,this.Height-40-10);

			this.pnlLeaderBoard.Location = new Point(0, this.tabBar.Bottom + 5+2);
			this.pnlLeaderBoard.Size = new Size(this.Width,this.Height-40-10);
			
			pnlGameScreen.Location = new Point(0, this.tabBar.Bottom+2);
			pnlGameScreen.Size = new Size(1024,690);

			pnlProjectPerformance.Location = new Point(0, this.tabBar.Bottom);
			pnlProjectPerformance.Size =new Size(this.Width,this.Height-40-10);

			pnlOpsChartScreen.Location = new Point(0, this.tabBar.Bottom);
			pnlOpsChartScreen.Size =new Size(Width,690);

			pnlResourceLevels.Location = new Point(0, 0);
			pnlResourceLevels.Size =new Size(pnlMain.Width,pnlMain.Height-40-10);
			pnlBubbles.Location = new Point(10+2, this.tabBar.Bottom + 5+2);
			pnlBubbles.Size =new Size(this.Width - 20-10,this.Height-40-10);

			pnlProjectSpend.Location = new Point (12, tabBar.Bottom + 7);
			pnlProjectSpend.Size = new Size (this.Width - 30, this.Height - 50);

			pnl_ProjectsBenefitChart.Location = new Point (12, tabBar.Bottom);
			pnl_ProjectsBenefitChart.Size = new Size (Width - 30, Height - 35);
		}

		public override void ReloadDataAndShow(bool reload)
		{
			ReloadData(reload);
		}

		protected virtual void GetRoundScores ()
		{
			Scores = new ArrayList();

			int prevprofit = 0;
			int newservices = 0;
			for (int i = 1; i <= _gameFile.LastRoundPlayed; i++)
			{
				RoundScores score = new RoundScores (_gameFile, i, prevprofit, newservices, supportOverrides);
				Scores.Add(score);
				prevprofit = score.Profit;
				newservices = score.NumNewServices;
			}
		}

		public void ReloadData (bool resetView)
		{
			this.GetRoundScores();

			RedrawLeaderBoard = true;
			RedrawScoreCard = true;
			RedrawGameScreen = true;
			RedrawProjectPerformance = true;
			RedrawOpsChart = true;
			RedrawResourceLevels = true;
			RedrawBubbles = true;
			RedrawProjectSpend = true;
			RedrawProgramBenefits = true;
			redrawPerception = true;
			redrawMaturity = true;

			HidePanels();

			if (resetView)
			{
				this.tabBar.SelectedTabCode = 1;
			}
			ShowPanel(this.tabBar.SelectedTabCode);
		}

		protected virtual void ShowPanel (int panel)
		{
			switch(panel)
			{
				case 1:
					ShowGameScreenPanel();
					break;
				case 2:
					ShowLeaderBoardPanel();
					break;
				case 3:
					ShowScoreCardPanel();
					break;
				case 4:
					ShowProjectPerformancePanel();
					break;
				case 5:
					ShowOpsChartPanel();
					break;
				case 6:
					ShowResourceLevelsPanel();
					break;
				case 7:
					ShowBubblesPanel();
					break;
				case 8:
					ShowPerceptionPanel();
					break;
				case 9:
					ShowMaturityPanel();
					break;
			}
		}
		
		public override void Init(ChartPanel screen)
		{
			ReloadDataAndShow(true);
			HidePanels();
			tabBar.SelectedTabCode = 1;
			ShowPanel(tabBar.SelectedTabCode);
		}

		protected virtual void HidePanels()
		{
			//Hide the Various comboBox
			this.cmb_bubble.Visible = false; 
			this.cmb_gamescreen.Visible = false; 
			this.cmb_projectPerfRound.Visible = false; 
			this.cmb_opsGanttRound.Visible = false;
			this.cmb_opsProjectsBenefit.Visible = false;
			cmbLeaderboardRound.Visible = false;
			perceptionPanel.Visible = false;
			maturityPanel.Hide();

			//Hide the panels 
			if (pnlLeaderBoard != null) pnlLeaderBoard.Visible = false;
			if (pnlScoreCard != null) pnlScoreCard.Visible = false;
			if (pnlGameScreen != null) pnlGameScreen.Visible = false;
			if (pnlProjectPerformance != null) pnlProjectPerformance.Visible = false;
			if (pnlOpsChartScreen != null) pnlOpsChartScreen.Visible = false;
			if (pnlResourceLevels != null) pnlResourceLevels.Visible = false;
			if (pnlBubbles != null) pnlBubbles.Visible = false;
			if (pnlProjectSpend != null) pnlProjectSpend.Visible = false;
			if (pnl_ProjectsBenefitChart != null) pnl_ProjectsBenefitChart.Visible = false;
		}

		protected virtual void tabBar_TabPressed (object sender, TabBarEventArgs args)
		{
			HidePanels();
			ShowPanel(args.Code);
		}

		public virtual void ShowLeaderBoardPanel()
		{
			pnlLeaderBoard.Visible = true;
			cmbLeaderboardRound.Visible = true;
			cmbLeaderboardRound.BringToFront();
			cmbLeaderboardRound.SelectedIndex = _gameFile.CurrentRound - 1;

			if (this.RedrawLeaderBoard == true)
			{
				ShowLeaderBoard();
			}
		}		

		protected virtual void ShowLeaderBoard ()
		{
#if !PASSEXCEPTIONS
			try
			{
#endif
				pnlLeaderBoard.SuspendLayout();

				if (leaderboardTable != null)
				{
					pnlLeaderBoard.Controls.Remove(leaderboardTable);
				}

				PM_LeaderboardReport report = new PM_LeaderboardReport ();
				string xml;
				bool allowDisplay = false;

				string filename;
				int round = 1 + cmbLeaderboardRound.SelectedIndex;
				if (round == 4)
				{
					filename = report.BuildYearlyReport(_gameFile);
					if ((filename != "") && File.Exists(filename))
					{
						allowDisplay = true;
					}
				}
				else
				{
					filename = report.BuildReport(_gameFile, round);
					if (this._gameFile.LastRoundPlayed >= round)
					{
						allowDisplay = true;
					}
				}

				if (allowDisplay)
				{
					using (StreamReader reader = new StreamReader(filename))
					{
						xml = reader.ReadToEnd();
					}

					leaderboardTable = new Table();
					leaderboardTable.LoadData(xml);

					leaderboardTable.Location = new Point(30, 50);
					leaderboardTable.Size = new Size(1024 - 60, leaderboardTable.TableHeight); //552);
					leaderboardTable.AutoScroll = true;
					leaderboardTable.BorderStyle = BorderStyle.None;

					Panel underline = new Panel();
					underline.BackColor = Color.FromArgb(85, 183, 221);
					underline.Size = new Size(leaderboardTable.Width, 2);
					underline.Location = new Point(0, leaderboardTable.Height - underline.Height);
					leaderboardTable.Controls.Add(underline);

					pnlLeaderBoard.Controls.Add(leaderboardTable);
					pnlLeaderBoard.ResumeLayout(false);

				}
				this.RedrawLeaderBoard = false;
#if !PASSEXCEPTIONS
			}
			catch(Exception ex)
			{
				AppLogger.TheInstance.WriteLine("Timer Level Exception : " + ex.Message + ":\r\n" + ex.StackTrace);
			}
#endif
		}		

		public virtual void ShowScoreCardPanel()
		{
			pnlScoreCard.Visible = true;

			if (this.RedrawScoreCard == true)
			{
				ShowScoreCard();
			}
		}

		protected virtual void ShowScoreCard()
		{
#if !PASSEXCEPTIONS
			try
			{
#endif
				pnlScoreCard.SuspendLayout();

				if (scoreCardTable != null)
				{
					pnlScoreCard.Controls.Remove(scoreCardTable);
				}

				PM_ScoreCardReportNew report = new PM_ScoreCardReportNew();
				string xml;

				string filename = report.BuildReport(_gameFile, _gameFile.NetworkModel, supportOverrides);
				using (StreamReader reader = new StreamReader (filename))
				{
					xml = reader.ReadToEnd();
				}

				scoreCardTable = new Table ();
				scoreCardTable.LoadData(xml);

				scoreCardTable.Location = new Point (30, 50);
				scoreCardTable.Size = new Size(1024 - 60, scoreCardTable.TableHeight); //552);
				scoreCardTable.AutoScroll = true;
				scoreCardTable.BorderStyle = BorderStyle.None;
				scoreCardTable.Selectable = true;
				scoreCardTable.CellTextChanged += new Table.CellChangedHandler (scoreCardTable_CellTextChanged);

				Panel underline = new Panel ();
				underline.BackColor = Color.FromArgb(85, 183, 221);
				underline.Size = new Size (scoreCardTable.Width, 2);
				underline.Location = new Point (0, scoreCardTable.Height - underline.Height);
				scoreCardTable.Controls.Add(underline);

				pnlScoreCard.Controls.Add(scoreCardTable);
				pnlScoreCard.ResumeLayout(false);

				this.RedrawScoreCard = false;
#if !PASSEXCEPTIONS
			}
			catch(Exception ex)
			{
				AppLogger.TheInstance.WriteLine("Timer Level Exception : " + ex.Message + ":\r\n" + ex.StackTrace);
			}
#endif
		}

		void scoreCardTable_CellTextChanged (Table sender, TextTableCell cell, string val)
		{
			this.supportOverrides.SetOverride(cell.CellName, val);
			supportOverrides.Save();

			ReloadData(false);
		}

		private void PM_TabbedChartScreen_VisibleChanged(object sender, EventArgs e)
		{
			if(!this.Visible)
			{
				this.HidePanels();
			}
		}
	
		protected override void Dispose(bool disposing)
		{
			if(disposing)
			{
				//				if(null != ganttChart)
				//				{
				//					ganttChart.Dispose();
				//					ganttChart = null;
				//				}
			}
			base.Dispose (disposing);
		}

		#region Bubbles Panel Methods 

		private void buildBubbleComboList(ComboBox cmbbubble)
		{
			if (cmbbubble != null)
			{
				for (int i = 1; i <= 3; i++)
				{
					cmbbubble.Items.Add(CONVERT.Format("Round {0}", i));
				}
			}
		}

		public void cmb_bubble_SelectedIndexChanged(object sender, EventArgs e)
		{
			ComboBox cmb_bubble = sender as ComboBox;
			this.bubble_view.SizeMode = PictureBoxSizeMode.StretchImage;

			string name = LibCore.AppInfo.TheInstance.Location + CONVERT.Format("\\images\\bubbles\\bubbles_r{0}_report_all.png", 1 + cmb_bubble.SelectedIndex);
			bubble_view.Image = LibCore.Repository.TheInstance.GetImage(name);
		}

		public void ShowBubble()
		{
			this.bubble_view.Visible = true;

			// Fix for 3923: if we're not playing the same round we were when we last showed the bubble chart,
			// then show the chart for the new current round.  If we're still playing the same round,
			// then just keep showing whatever round's bubble chart the user had selected.
			if (_gameFile.CurrentRound != bubbleChartLastChangedWhenPlayingRound)
			{
				ShowBubbleChartRound(_gameFile.CurrentRound);
			}
		}

		public void ShowBubbleChartRound (int round)
		{
			switch (round)
			{
				case 1: 
					cmb_bubble.SelectedIndex = 0;
					break;
				case 2:
					cmb_bubble.SelectedIndex = 1;
					break;
				case 3:
					cmb_bubble.SelectedIndex = 2;
					break;
			}
			// Keep it within sensible values.
			//cmb_bubble.SelectedIndex = Math.Max(0, Math.Min(4, round - 2));
			bubbleChartLastChangedWhenPlayingRound = _gameFile.CurrentRound;
		}

		public virtual void ShowBubblesPanel()
		{
			pnlBubbles.Visible = true;
			this.cmb_bubble.Visible = true;

			if (this.RedrawBubbles == true)
			{
				ShowBubble();
			}
		}
		
		protected virtual void ShowBubbles()
		{

		}

		#endregion Bubbles Panel Methods 

		#region Game Display Methods

		protected NodeTree GetModelForRound (int round)
		{
			return _gameFile.GetNetworkModel(round, GameFile.GamePhase.OPERATIONS);
		}

		protected virtual void RemoveGameScreenControls ()
		{
			this.pnlGameScreen.SuspendLayout();
			if (psd_ProjectsDisplay != null)
			{
				this.pnlGameScreen.Controls.Remove(psd_ProjectsDisplay);
				psd_ProjectsDisplay.Dispose();
				psd_ProjectsDisplay = null;
			}
			if (psd_GameStatsDisplay != null)
			{
				this.pnlGameScreen.Controls.Remove(psd_GameStatsDisplay);
				psd_GameStatsDisplay.Dispose();
				psd_GameStatsDisplay = null;
			}
			if (psd_GameCommsDisplay != null)
			{
				this.pnlGameScreen.Controls.Remove(psd_GameCommsDisplay);
				psd_GameCommsDisplay.Dispose();
				psd_GameCommsDisplay = null;
			}
			if (psd_GameTargetsDisplay != null)
			{
				this.pnlGameScreen.Controls.Remove(psd_GameTargetsDisplay);
				psd_GameTargetsDisplay.Dispose();
				psd_GameTargetsDisplay = null;
			}
			this.pnlGameScreen.ResumeLayout();
		}

		public bool ShowGameScreenRound (int round)
		{
			dont_handle = true;
			// Keep it within sensible values.
			this.cmb_gamescreen.SelectedIndex = Math.Min(cmb_gamescreen.Items.Count, round)-1;
			gameScreenLastChangedWhenPlayingRound = _gameFile.CurrentRound;

			dont_handle = false;

			//Clear out the old controls 
			RemoveGameScreenControls();

			NodeTree model = GetModelForRound(round);
			if (model == null)
			{
				// Not played this round yet, so nothing to show.
				return false;
			}

			//Create new Controls 
			if (psd_ProjectsDisplay == null)
			{
				psd_ProjectsDisplay = new ProjectsStatusDisplay(model, round, false, ! true);
			}

			psd_ProjectsDisplay.Size = new Size(1024, 430);
			psd_ProjectsDisplay.Location = new Point(0, 0);
			this.pnlGameScreen.Controls.Add(psd_ProjectsDisplay);

			return true;
		}

		public virtual void ShowGameScreenPanel()
		{
			this.pnlGameScreen.Visible = true;
			this.cmb_gamescreen.Visible = true;

			int round = _gameFile.LastRoundPlayed;
			cmb_gamescreen.SelectedIndex = Math.Min(cmb_gamescreen.Items.Count, round)-1;

			//Always redrew
			this.RedrawGameScreen = true;

			//force to redraw the board
			if (this.RedrawGameScreen == true)
			{
				ShowGameScreenRound(round);
			}
		}
		
		protected virtual void ShowGameScreen()
		{
		}

		public void cmb_gamescreen_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (dont_handle==false)
			{
				ComboBox cmb_gamescreen2 = sender as ComboBox;
				ShowGameScreenRound(1 + cmb_gamescreen2.SelectedIndex);
			}
		}

		#endregion Game Display Methods

		#region Project Performance Methods

		protected virtual void RemoveProjectPerfromanceScreenControls ()
		{
			this.pnlGameScreen.SuspendLayout();
			if (ppc_PrjPerfContainer != null)
			{
				this.pnlProjectPerformance.Controls.Remove(ppc_PrjPerfContainer);
				ppc_PrjPerfContainer.Dispose();
				ppc_PrjPerfContainer = null;
			}
			this.pnlGameScreen.ResumeLayout();
		}


		public bool ShowProjectPerformanceScreenRound (int round)
		{
			dont_handle = true;
			// Keep it within sensible values.
			this.cmb_projectPerfRound.SelectedIndex = Math.Min(cmb_gamescreen.Items.Count, round)-1;
			this.projectPerformanceLastChangedWhenPlayingRound = _gameFile.CurrentRound;
			dont_handle = false;

			//Clear out the old controls 
			RemoveProjectPerfromanceScreenControls();

			NodeTree model = GetModelForRound(round);
			if (model == null)
			{
				return false;// Not played this round yet, so nothing to show.
			}

			if (ppc_PrjPerfContainer == null)
			{
				ppc_PrjPerfContainer = new PrjPerfContainer((PMNetworkProgressionGameFile) _gameFile,model,round);
				ppc_PrjPerfContainer.Size = new Size(1024 - 60, 620);
				ppc_PrjPerfContainer.Location = new Point(30,0);
				this.pnlProjectPerformance.Controls.Add(ppc_PrjPerfContainer);
			}
			this.cmb_projectPerfRound.BringToFront();
			return true;
		}

		public virtual void ShowProjectPerformancePanel()
		{
			pnlProjectPerformance.Visible = true;
			this.cmb_projectPerfRound.Visible = true;
			this.cmb_projectPerfRound.BringToFront();

			int round = _gameFile.LastRoundPlayed;
			cmb_projectPerfRound.SelectedIndex = Math.Min(cmb_projectPerfRound.Items.Count, round)-1;

			if (this.RedrawProjectPerformance == true)
			{
				ShowProjectPerformanceScreenRound(round);
			}
		}

		public void cmb_projectPerfRound_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (dont_handle==false)
			{
				ComboBox cmb_prjperfscreen2 = sender as ComboBox;
				ShowProjectPerformanceScreenRound(1 + cmb_prjperfscreen2.SelectedIndex);
			}

		}


		#endregion Project Performance Methods

		#region OPSGanttChart Screen Methods

		protected virtual void RemoveOpsGanttScreenControls ()
		{
			this.pnlOpsChartScreen.SuspendLayout();
			if (this.ogc_OpsGanntContainer != null)
			{
				this.pnlOpsChartScreen.Controls.Remove(ogc_OpsGanntContainer);
				ogc_OpsGanntContainer.Dispose();
				ogc_OpsGanntContainer = null;
			}
			this.pnlOpsChartScreen.ResumeLayout();
		}


		public virtual void ShowOpsChartPanel()
		{
			this.pnlOpsChartScreen.Visible = true;
			this.cmb_opsGanttRound.Visible = true;
			this.cmb_opsGanttRound.BringToFront();

			int round = _gameFile.LastRoundPlayed;
			cmb_opsGanttRound.SelectedIndex = Math.Min(round-1, cmb_opsGanttRound.Items.Count - 1);

			if (this.RedrawOpsChart == true)
			{
				ShowOpsGanttScreenRound(round);
			}
		}

		protected virtual bool ShowOpsGanttScreenRound(int round)
		{
			dont_handle = true;
			// Keep it within sensible values.
			this.cmb_opsGanttRound.SelectedIndex = Math.Min(round-1, cmb_opsGanttRound.Items.Count - 1);
			this.OpsGanttLastChangedWhenPlayingRound = _gameFile.CurrentRound;
			dont_handle = false;

			//Clear out the old controls 
			RemoveOpsGanttScreenControls();

			NodeTree model = GetModelForRound(round);
			if (model == null)
			{
				return false;// Not played this round yet, so nothing to show.
			}

			if (ogc_OpsGanntContainer == null)
			{
				ogc_OpsGanntContainer = new OpsGanttContainer(_gameFile, model,round);
				ogc_OpsGanntContainer.Size = new Size(1024 - 60,690);
				ogc_OpsGanntContainer.Location = new Point(30,30);
				this.pnlOpsChartScreen.Controls.Add(ogc_OpsGanntContainer);
			}
			this.cmb_opsGanttRound.BringToFront();
			return true;
		}
		
		protected virtual void ShowOpsChart()
		{
			this.pnlOpsChartScreen.Visible = true;
			this.cmb_opsGanttRound.Visible = true;
			this.cmb_opsGanttRound.BringToFront();

			int round = _gameFile.LastRoundPlayed;
			cmb_opsGanttRound.SelectedIndex = Math.Min(cmb_opsGanttRound.Items.Count, round) - 1;

			if (this.RedrawOpsChart == true)
			{
				ShowOpsGanttScreenRound(round);
			}

		}

		public void cmb_opsGanttRound_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (dont_handle==false)
			{
				ComboBox cmb_opsGanttRound2 = sender as ComboBox;
				ShowOpsGanttScreenRound(1 + cmb_opsGanttRound2.SelectedIndex);
			}
		}

		public void cmbLeaderboardRound_SelectedIndexChanged (object sender, EventArgs e)
		{
			ShowLeaderBoard();
		}

		#endregion OPSGanttChart Screen Methods

		#region Resource Levels Screen Methods

		protected virtual void RemoveResLevelsScreenControls ()
		{
			this.pnlResourceLevels.SuspendLayout();
			if (this.res_ResourceLevelsContainer != null)
			{
				this.pnlResourceLevels.Controls.Remove(res_ResourceLevelsContainer);
				res_ResourceLevelsContainer.Dispose();
				res_ResourceLevelsContainer = null;
			}
			this.pnlResourceLevels.ResumeLayout();
		}


		public virtual void ShowResLevelsPanel()
		{
			this.pnlResourceLevels.Visible = true;
			int round = _gameFile.LastRoundPlayed;

			if (this.RedrawResourceLevels == true)
			{
				ShowResLevelsRound(round);
			}
		}

		protected virtual bool ShowResLevelsRound(int round)
		{
			dont_handle = true;
			// Keep it within sensible values.
			this.ResLevelLastChangedWhenPlayingRound = _gameFile.CurrentRound;
			dont_handle = false;

			NodeTree model = GetModelForRound(round);
			if (model == null)
			{
				return false;// Not played this round yet, so nothing to show.
			}
			//Clear out the old controls 
			RemoveResLevelsScreenControls();

			if (this.res_ResourceLevelsContainer == null)
			{
				res_ResourceLevelsContainer = new ResourceLevelsContainer(_gameFile, round, true);
				res_ResourceLevelsContainer.Size = new Size(1024,650);
				res_ResourceLevelsContainer.Location = new Point(0,0);
				this.pnlResourceLevels.Controls.Add(res_ResourceLevelsContainer);

				res_ResourceLevelsContainer.ShowRound(round);
			}
			//this.cmb_opsGanttRound.BringToFront();
			return true;
		}
		
		protected virtual void ShowResourceLevelsPanel()
		{
			this.pnlResourceLevels.Visible = true;
			//this.cmb_opsGanttRound.Visible = true;
			//this.cmb_opsGanttRound.BringToFront();

			int round = _gameFile.LastRoundPlayed;
			//cmb_opsGanttRound.SelectedIndex = round-1;

			if (this.RedrawResourceLevels == true)
			{
				ShowResLevelsRound(round);
			}
		}

		#endregion Resource Levels Screen Methods

		#region Portfolio Screen Methods

		protected void RemoveBenefitChart()
		{
			if (myProjectsBenefitChart != null)
			{
				pnl_ProjectsBenefitChart.Controls.Remove(myProjectsBenefitChart);
				myProjectsBenefitChart.Dispose();
				myProjectsBenefitChart = null;
			}		
		}

		protected virtual bool ShowProjectBenefits(int round)
		{
			dont_handle = true;
			// Keep it within sensible values.
			this.cmb_opsProjectsBenefit.SelectedIndex = Math.Min(round - 1, cmb_opsProjectsBenefit.Items.Count - 1);
			this.ProjectsBenefitLastChangedWhenPlayingRound = _gameFile.CurrentRound;
			dont_handle = false;

			//Clear out the old controls 
			RemoveBenefitChart();

			NodeTree model = GetModelForRound(round);
			if (model == null)
			{
				return false;// Not played this round yet, so nothing to show.
			}

			if (myProjectsBenefitChart == null)
			{
				myProjectsBenefitChart = new PM_ProjectsBenefitsByDayContainer();
				myProjectsBenefitChart.Location = new Point(5, 5);
				myProjectsBenefitChart.Size = new Size(1024 - 20, 620);
				//myProjectsBenefitChart .BackColor = Color.LightSteelBlue;
				myProjectsBenefitChart.SetGameFile(_gameFile, round);
				pnl_ProjectsBenefitChart.Controls.Add(myProjectsBenefitChart);
				pnl_ProjectsBenefitChart.Visible = true;
			}
			this.cmb_opsProjectsBenefit.BringToFront();
			return true;
		}


		//protected virtual void ShowProgramBenefits ()
		//{
		//  pnl_ProjectsBenefitChart.SuspendLayout();

		//  if (myProjectsBenefitChart  != null)
		//  {
		//    pnl_ProjectsBenefitChart.Controls.Remove(myProjectsBenefitChart );
		//  }

		//  //myProjectsBenefitChart  = new PortfolioAchievementsContainer();
		//  myProjectsBenefitChart  = new PM_ProjectsBenefitsByDayContainer();
		//  myProjectsBenefitChart .Location = new Point(5, 5);
		//  myProjectsBenefitChart .Size = new Size(1024 - 20, 620);
		//  //myProjectsBenefitChart .BackColor = Color.LightSteelBlue;
		//  myProjectsBenefitChart .SetGameFile(_gameFile);
		//  pnl_ProjectsBenefitChart.Controls.Add(myProjectsBenefitChart );

		//  pnl_ProjectsBenefitChart.ResumeLayout(false);
		//  RedrawProgramBenefits = false;
		//}

		protected virtual void ShowProgramBenefitsPanel ()
		{
			pnl_ProjectsBenefitChart.Visible = true;
			this.cmb_opsProjectsBenefit.Visible = true;

			int round = _gameFile.LastRoundPlayed;

			if (RedrawProgramBenefits)
			{
				ShowProjectBenefits(round);
			}
		}

		public void cmb_opsProjectsBenefit_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (dont_handle==false)
			{
				ComboBox cmb = sender as ComboBox;
				ShowProjectBenefits(1 + cmb.SelectedIndex);
			}
		}

		#endregion Portfolio Screen Methods

		void ShowPerceptionPanel ()
		{
			perceptionPanel.Show();

			int round = _gameFile.CurrentRound;
			perceptionRoundSelector.SelectedIndex = Math.Min(perceptionRoundSelector.Items.Count, round) - 1;

			if (redrawPerception)
			{
				ShowPerception(round);
			}
		}

		protected virtual void ShowPerception (int round)
		{
			redrawPerception = false;

			perceptionPanel.SuspendLayout();

			if (perceptionBarChart != null)
			{
				perceptionPanel.Controls.Remove(perceptionBarChart);
			}

			bool previousround = (round < _gameFile.LastRoundPlayed);
			bool playedOPERATIONS = (_gameFile.LastPhasePlayed == GameFile.GamePhase.OPERATIONS);
			bool proceed = false;

			if (previousround)
			{
				proceed = true; //we are requesting a previous so we must have played operations 
			}
			else
			{
				//we are requesting the current round, so check we have played operations otherwise don't show
				if (playedOPERATIONS)
				{
					proceed = true;
				}
			}

			//if (round <= _gameFile.LastRoundPlayed)
			if (proceed)
			{
				PerceptionSurveyReport report = new PerceptionSurveyReport();
				BasicXmlDocument xml = BasicXmlDocument.CreateFromFile(report.BuildReport(_gameFile, round));
				perceptionBarChart = new GroupedBarChart(xml.DocumentElement);
				perceptionPanel.Controls.Add(perceptionBarChart);
				perceptionBarChart.Size = perceptionPanel.Size;
				perceptionBarChart.XAxisHeight = 50;
				perceptionBarChart.YAxisWidth = 35;
				perceptionBarChart.LegendX = perceptionRoundSelector.Right + 50;
				perceptionBarChart.LegendHeight = 40;
			}

			perceptionPanel.ResumeLayout(false);
		}

		void perceptionRoundSelector_SelectedIndexChanged (object sender, EventArgs args)
		{
			ShowPerception(1 + perceptionRoundSelector.SelectedIndex);
		}


		void ShowMaturityPanel ()
		{
			maturityPanel.Show();

			int round = _gameFile.CurrentRound;
			maturityRoundSelector.SelectedIndex = Math.Min(maturityRoundSelector.Items.Count, round) - 1;

			if (redrawMaturity)
			{
				ShowMaturity(round);
			}
		}

		protected virtual void ShowMaturity (int round)
		{
			redrawMaturity = false;

			maturityPanel.SuspendLayout();

			if (maturityChart != null)
			{
				maturityPanel.Controls.Remove(maturityChart);
			}

			if (round <= _gameFile.LastRoundPlayed)
			{
				OpsMaturityReport report = new OpsMaturityReport ();
				string xml = File.ReadAllText(report.BuildReport(_gameFile, round, Scores));

				maturityChart = new PieChart ();
				maturityChart.Size = maturityPanel.Size;
				maturityChart.LoadData(xml);

				maturityPanel.Controls.Add(maturityChart);
			}

			perceptionPanel.ResumeLayout(false);
		}

		void maturityRoundSelector_SelectedIndexChanged (object sender, EventArgs args)
		{
			ShowMaturity(1 + maturityRoundSelector.SelectedIndex);
		}

		public override void ShowReport (ChartScreenTabOption report)
		{
			throw new NotImplementedException();
		}

		public override IList<ChartScreenTabOption> GetAllAvailableReports ()
		{
			throw new NotImplementedException();
		}
	}
}