using System;
using System.Windows.Forms;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using Charts;
using GameManagement;
using ReportBuilder;
using Logging;
using CoreUtils;
using Network;

namespace ChartScreens
{
	/// <summary>
	/// Summary description for NewTabbedChartScreen.
	/// This is actually the H version
	/// </summary>
	public class NewTabbedChartScreen : BaseTabbedChartScreen
	{
		public NewTabbedChartScreen(NetworkProgressionGameFile gameFile, SupportSpendOverrides _spend_overrides) 
			: base(gameFile, _spend_overrides)
		{
			ShowHoverRevenue = false;
			// add the tabs
			tabBar.AddTab("Leaderboard", 0, true);
			tabBar.AddTab("Race Points", 1, true);
			tabBar.AddTab("Score Card", 2, true);
			tabBar.AddTab("Gantt Chart", 3, true);
			tabBar.AddTab("Maturity", 4, true);
			tabBar.AddTab("Comparison", 5, true);
			tabBar.AddTab("Transition", 6, true);
			tabBar.AddTab("Network", 7, true);
			tabBar.AddTab("Incidents", 8, true);

			scorecardTabs.AddTab("Game", 0, true);
			scorecardTabs.AddTab("Process", 1, true);
			scorecardTabs.AddTab("Support Spend", 2, true);

			//add the new tab bar and the sub tab bar (invisible)
			tabBar.Location = new Point(5,0);
			scorecardTabs.Location = new Point(5,35);
			scorecardTabs.Visible = false;

			this.SuspendLayout();
			this.Controls.Add(tabBar);
			this.Controls.Add(scorecardTabs);
			this.ResumeLayout(false);

			tabBar.TabPressed += tabBar_TabPressed;
			scorecardTabs.TabPressed += scorecardTabs_TabPressed;

			InitialisePanels();
			HidePanels();

			this.Resize += TabbedChartScreen_Resize;
			this.BackColor = Color.White;
		}


		protected override void HidePanels()
		{
			pnlLeaderBoard.Visible = false;

			if (pnlIncidents != null)
			{
				pnlIncidents.Visible = false;
			}
			
			pnlPoints.Visible = false;
			
			base.HidePanels();
		}

		protected virtual void InitialisePanels()
		{
			pnlMain = new Panel();
			pnlLeaderBoard = new Panel();
			pnlGantt = new Panel();
			pnlNetwork = new Panel();
			pnlMaturity = new Panel();
			pnlPoints = new Panel();
			pnlComparison = new Panel();
			pnlScoreCard = new Panel();
			pnlSupportCosts = new Panel();
			pnlTransition = new Panel();
			pnlProcess = new Panel();
			pnlIncidents = new Panel();
			transitionControlsDisplayPanel = new Panel();

			// 
			// pnlMain
			// 
			this.SuspendLayout();

			pnlMain.BackColor = System.Drawing.Color.White;
			this.pnlMain.Controls.Add(pnlPoints);
			this.pnlMain.Controls.Add(pnlComparison);
			this.pnlMain.Controls.Add(pnlScoreCard);
			this.pnlMain.Controls.Add(pnlSupportCosts);
			this.pnlMain.Controls.Add(pnlLeaderBoard);
			this.pnlMain.Controls.Add(pnlMaturity);
			this.pnlMain.Controls.Add(pnlTransition);
			this.pnlMain.Controls.Add(pnlNetwork);
			this.pnlMain.Controls.Add(pnlGantt);
			this.pnlMain.Controls.Add(pnlProcess);
			this.pnlMain.Controls.Add(pnlIncidents);
			this.pnlMain.DockPadding.All = 4;
			this.pnlMain.Name = "pnlMain";
			this.pnlMain.TabIndex = 6;
			this.Controls.Add(this.pnlMain);

			//pnlLeaderBoard
			this.pnlLeaderBoard.BackColor = System.Drawing.Color.White;
			this.pnlLeaderBoard.Location = new System.Drawing.Point(0, 0);
			this.pnlLeaderBoard.Name = "pnlLeaderBoard";
			this.pnlLeaderBoard.TabIndex = 0;

			LeaderboardRoundSelector = new ComboBox();
			LeaderboardRoundSelector.DropDownStyle = ComboBoxStyle.DropDownList;
			for (int i = 1; i <= SkinningDefs.TheInstance.GetIntData("roundcount", 5); i++)
			{
				LeaderboardRoundSelector.Items.Add("Round " + LibCore.CONVERT.ToStr(i));
			}
			LeaderboardRoundSelector.Location = new Point(10,10);
			LeaderboardRoundSelector.Size = new Size(100,100);
			LeaderboardRoundSelector.SelectedIndexChanged += LeaderboardRoundSelector_SelectedIndexChanged;

			pnlLeaderBoard.SuspendLayout();
			pnlLeaderBoard.Controls.Add(LeaderboardRoundSelector);
			pnlLeaderBoard.ResumeLayout(false);

			//pnlGantt
			this.pnlGantt.BackColor = System.Drawing.Color.White;
			this.pnlGantt.Location = new System.Drawing.Point(0, 0);
			this.pnlGantt.Name = "pnlGantt";
			this.pnlGantt.TabIndex = 0;

			//set up the combo box for the gantt chart car selections
			GanttCarSelector = new ComboBox();
			GanttCarSelector.DropDownStyle = ComboBoxStyle.DropDownList;
			string allbiz = CoreUtils.SkinningDefs.TheInstance.GetData("allbiz");
			GanttCarSelector.Items.Add(allbiz);
			string biz = CoreUtils.SkinningDefs.TheInstance.GetData("biz");
			GanttCarSelector.Items.Add(biz + " 1");
			GanttCarSelector.Items.Add(biz + " 2");
			GanttCarSelector.Items.Add(biz + " 3");
			GanttCarSelector.Items.Add(biz + " 4");
			GanttCarSelector.Text = allbiz;
			GanttCarSelector.Location = new Point(450,10);
			GanttCarSelector.Size = new Size(100,100);
			GanttCarSelector.SelectedIndexChanged += GanttCarSelector_SelectedIndexChanged;

			pnlGantt.SuspendLayout();
			pnlGantt.Controls.Add(GanttCarSelector);

			GanttRoundSelector = new ComboBox();
			GanttRoundSelector.DropDownStyle = ComboBoxStyle.DropDownList;
			for (int i = 1; i <= SkinningDefs.TheInstance.GetIntData("roundcount", 5); i++)
			{
				GanttRoundSelector.Items.Add("Round " + LibCore.CONVERT.ToStr(i));
			}
			GanttRoundSelector.Location = new Point(250,10);
			GanttRoundSelector.Size = new Size(100,100);
			GanttRoundSelector.SelectedIndexChanged += GanttRoundSelector_SelectedIndexChanged;
			pnlGantt.Controls.Add(GanttRoundSelector);

			pnlGantt.ResumeLayout(false);

			//pnlIncidents
			this.pnlIncidents.BackColor = System.Drawing.Color.White;
			this.pnlIncidents.Location = new System.Drawing.Point(0, 0);
			this.pnlIncidents.Name = "pnlIncidents";
			this.pnlIncidents.TabIndex = 0;

			IncidentsRoundSelector = new ComboBox();
			IncidentsRoundSelector.DropDownStyle = ComboBoxStyle.DropDownList;
			for (int i = 1; i <= SkinningDefs.TheInstance.GetIntData("roundcount", 5); i++)
			{
				IncidentsRoundSelector.Items.Add("Round " + LibCore.CONVERT.ToStr(i));
			}
			IncidentsRoundSelector.Location = new Point(10, 10);
			IncidentsRoundSelector.Size = new Size(100, 100);
			IncidentsRoundSelector.SelectedIndexChanged += IncidentsRoundSelector_SelectedIndexChanged;

			pnlIncidents.SuspendLayout();
			pnlIncidents.Controls.Add(IncidentsRoundSelector);
			pnlIncidents.ResumeLayout(false);

			//pnlNetwork
			pnlNetwork.BackColor = System.Drawing.Color.White;
			pnlNetwork.Location = new System.Drawing.Point(0, 0);
			pnlNetwork.Name = "pnlNetwork";
			pnlNetwork.TabIndex = 0;

			NetworkRoundSelector = new ComboBox();
			NetworkRoundSelector.DropDownStyle = ComboBoxStyle.DropDownList;
			for (int i = 1; i <= SkinningDefs.TheInstance.GetIntData("roundcount", 5); i++)
			{
				NetworkRoundSelector.Items.Add("Round " + LibCore.CONVERT.ToStr(i));
			}
			NetworkRoundSelector.Location = new Point(5,10);
			NetworkRoundSelector.Size = new Size(100,100);
			NetworkRoundSelector.SelectedIndexChanged += NetworkRoundSelector_SelectedIndexChanged;

			pnlNetwork.SuspendLayout();
			pnlNetwork.Controls.Add(NetworkRoundSelector);
			pnlNetwork.ResumeLayout(false);

			//pnlMaturity
			pnlMaturity.BackColor = System.Drawing.Color.White;
			pnlMaturity.Location = new System.Drawing.Point(0, 0);
			pnlMaturity.Name = "pnlMaturity";
			pnlMaturity.TabIndex = 0;

			MaturityRoundSelector = new ComboBox();
			MaturityRoundSelector.DropDownStyle = ComboBoxStyle.DropDownList;
			for (int i = 1; i <= SkinningDefs.TheInstance.GetIntData("roundcount", 5); i++)
			{
				MaturityRoundSelector.Items.Add("Round " + LibCore.CONVERT.ToStr(i));
			}
			MaturityRoundSelector.Location = new Point(10,10);
			MaturityRoundSelector.Size = new Size(100,100);
			MaturityRoundSelector.SelectedIndexChanged += MaturityRoundSelector_SelectedIndexChanged;

			pnlMaturity.SuspendLayout();
			pnlMaturity.Controls.Add(MaturityRoundSelector);
			pnlMaturity.ResumeLayout(false);

			//pnlPoints
			pnlPoints.BackColor = System.Drawing.Color.White;
			pnlPoints.Location = new System.Drawing.Point(0, 0);
			pnlPoints.Name = "pnlPoints";
			pnlPoints.TabIndex = 0;

			PointsRoundSelector = new ComboBox();
			PointsRoundSelector.DropDownStyle = ComboBoxStyle.DropDownList;
			for (int i = 1; i <= SkinningDefs.TheInstance.GetIntData("roundcount", 5); i++)
			{
				PointsRoundSelector.Items.Add("Round " + LibCore.CONVERT.ToStr(i));
			}
			PointsRoundSelector.Items.Add("Championship");
			PointsRoundSelector.Location = new Point(100,10);
			PointsRoundSelector.Size = new Size(100,100);
			PointsRoundSelector.SelectedIndexChanged += PointsRoundSelector_SelectedIndexChanged;

			pnlPoints.SuspendLayout();
			pnlPoints.Controls.Add(PointsRoundSelector);
			pnlPoints.ResumeLayout(false);

			//pnlScoreCard
			pnlScoreCard.BackColor = System.Drawing.Color.White;
			pnlScoreCard.Location = new System.Drawing.Point(0, 0);
			pnlScoreCard.Name = "pnlScoreCard";
			pnlScoreCard.TabIndex = 0;

			//pnlSupportCosts
			pnlSupportCosts.BackColor = System.Drawing.Color.White;
			pnlSupportCosts.Location = new System.Drawing.Point(0, 0);
			pnlSupportCosts.Name = "pnlSupportCosts";
			pnlSupportCosts.TabIndex = 0;

			//pnlProcess
			pnlProcess.BackColor = System.Drawing.Color.White;
			pnlProcess.Location = new System.Drawing.Point(0, 0);
			pnlProcess.Name = "pnlProcess";
			pnlProcess.TabIndex = 0;
			pnlProcess.AutoScroll = true;

			//pnlComparison
			pnlComparison.BackColor = System.Drawing.Color.White;
			pnlComparison.Location = new System.Drawing.Point(0, 0);
			pnlComparison.Name = "pnlComparison";
			pnlComparison.TabIndex = 0;

			ComparisonSelector = new ComboBox();
			ComparisonSelector.DropDownStyle = ComboBoxStyle.DropDownList;
			for (int i = 1; i <= SkinningDefs.TheInstance.GetIntData("roundcount", 5); i++)
			{
				ComparisonSelector.Items.Add("Round " + LibCore.CONVERT.ToStr(i));
			}
			ComparisonSelector.Location = new Point(50,40);
			ComparisonSelector.Size = new Size(100,100);
			ComparisonSelector.SelectedIndexChanged += ComparisonSelector_SelectedIndexChanged;

			pnlComparison.SuspendLayout();
			this.pnlComparison.Controls.Add(ComparisonSelector);

			Series1 = new ComboBox();
			Series1.DropDownStyle = ComboBoxStyle.DropDownList;
			Series1.Items.Add("Points");
			Series1.Items.Add("Revenue");
			Series1.Items.Add("Fixed Costs");
			Series1.Items.Add("New Service Costs");
			Series1.Items.Add("Regulation Fines");
			Series1.Items.Add("Profit / Loss");
			Series1.Items.Add("Gain / Loss");
			Series1.Items.Add("Support Budget");
			Series1.Items.Add("Support Spend");
			Series1.Items.Add("Support Profit/Loss");
			Series1.Items.Add("New Services Implemented");
			Series1.Items.Add("New Services Implemented Before Race");
			Series1.Items.Add("Availability");
			Series1.Items.Add("MTRS");
			Series1.Items.Add("Total Failures");
			Series1.Items.Add("Prevented Failures");
			Series1.Items.Add("Recurring Failures");
			Series1.Items.Add("Workarounds");
			Series1.Items.Add("SLA Breaches");

			//overall score
			Series1.Items.Add("Maturity Indicator");
			Series1.Text = "Points";
			Series1.Size = new Size(220,120);
			Series1.Location = new Point(360,40);
			Series1.SelectedIndexChanged += Series1_SelectedIndexChanged;
			pnlComparison.Controls.Add(Series1);

			Series2 = new ComboBox();
			Series2.DropDownStyle = ComboBoxStyle.DropDownList;
			Series2.Items.Add("NONE");
			Series2.Items.Add("Points");
			Series2.Items.Add("Revenue");
			Series2.Items.Add("Fixed Costs");
			Series2.Items.Add("New Service Costs");
			Series2.Items.Add("Regulation Fines");
			Series2.Items.Add("Profit / Loss");
			Series2.Items.Add("Gain / Loss");
			Series2.Items.Add("Support Budget");
			Series2.Items.Add("Support Spend");
			Series2.Items.Add("Support Profit/Loss");
			Series2.Items.Add("New Services Implemented");
			Series2.Items.Add("New Services Implemented Before Race");
			Series2.Items.Add("Availability");
			Series2.Items.Add("MTRS");
			Series2.Items.Add("Total Failures");
			Series2.Items.Add("Prevented Failures");
			Series2.Items.Add("Recurring Failures");
			Series2.Items.Add("Workarounds");
			Series2.Items.Add("SLA Breaches");
			//overall score
			Series2.Items.Add("Maturity Indicator");
			Series2.Text = "NONE";
			Series2.Size = new Size(220,120);
			Series2.Location = new Point(655,40);
			Series2.SelectedIndexChanged += Series2_SelectedIndexChanged;
			pnlComparison.Controls.Add(Series2);

			pnlComparison.ResumeLayout(false);

			//add maturity items
			if (Scores.Count > 0) 
			{
				foreach(string section in ((RoundScores)Scores[0]).MaturityHashOrder)
				{
					ArrayList points = (ArrayList)ReportUtils.TheInstance.Maturity_Names[section];

					if (points != null)
					{
						foreach (string pt in points)
						{
							string[] vals = pt.Split(':');

							Series1.Items.Add( vals[0] );
							Series2.Items.Add( vals[0] );
						}
					}
				}
			}

			//pnlTransition
			pnlTransition.BackColor = System.Drawing.Color.White;
			pnlTransition.Location = new System.Drawing.Point(0, 0);
			pnlTransition.Name = "pnlTransition";
			pnlTransition.TabIndex = 0;

			TransitionSelector = new ComboBox();
			TransitionSelector.DropDownStyle = ComboBoxStyle.DropDownList;
			for (int i = 2; i <= SkinningDefs.TheInstance.GetIntData("roundcount", 5); i++)
			{
				TransitionSelector.Items.Add("Round " + LibCore.CONVERT.ToStr(i));
			}
			TransitionSelector.Location = new Point(5,40);
			TransitionSelector.Size = new Size(100,100);
			TransitionSelector.SelectedIndexChanged += TransitionSelector_SelectedIndexChanged;

			transitionControlsDisplayPanel.BackColor = SkinningDefs.TheInstance.GetColorData("transition_basebackcolor");
			transitionControlsDisplayPanel.Location = new Point(0,95);
			transitionControlsDisplayPanel.Width = this.pnlTransition.Width;
			transitionControlsDisplayPanel.Height = 200;
			transitionControlsDisplayPanel.Visible = false;
			this.pnlTransition.Controls.Add(transitionControlsDisplayPanel);

			pnlTransition.SuspendLayout();
			this.pnlTransition.Controls.Add(TransitionSelector);
			pnlTransition.ResumeLayout(false);

			this.ResumeLayout(false);
		}	

		protected override void ShowPanel(int panel)
		{
			if(null != Costs) Costs.Flush();
			this.scorecardTabs.Visible = false;

			switch(panel)
			{
				case 0:
					ShowLeaderBoardPanel();
					break;
				case 1:
					ShowPointsPanel();
					break;
				case 2:
					//special case for scorecard sub tabs
					this.scorecardTabs.Visible = true;
					if (scorecardTabs.SelectedTab == 0) ShowScoreCardPanel();
					else if (scorecardTabs.SelectedTab == 1) this.ShowProcessScoresPanel();
					else if (scorecardTabs.SelectedTab == 2) this.ShowSupportCostsPanel();
					break;
				case 3:
					ShowGanttChartPanel();
					break;
				case 4:
					ShowMaturityPanel();
					break;
				case 5:
					ShowComparisonPanel();
					break;
				case 6:
					ShowTransitionPanel();
					break;
				case 7:
					ShowNetworkPanel();
					break;
				case 8:
					ShowIncidentsPanel();
					break;
				default:
					ShowLeaderBoardPanel();
					break;
			}
		}		

		protected void LeaderboardRoundSelector_SelectedIndexChanged(object sender, EventArgs e)
		{
			this.ShowLeaderBoard(LeaderboardRoundSelector.SelectedIndex+1);
		}
		
		protected void PointsRoundSelector_SelectedIndexChanged(object sender, EventArgs e)
		{
			int round = PointsRoundSelector.SelectedIndex+1;
			bool champ = false;
			if (PointsRoundSelector.Text == "Championship")
			{
				champ = true;
				round = SkinningDefs.TheInstance.GetIntData("roundcount", 5);
			}
			this.ShowPoints(round, champ);
		}	

		protected override void DoSize()
		{
			tabBar.Size = new Size(this.Width-21, 29);
			scorecardTabs.Size = new Size(408,29);

			pnlMain.Location = new Point(10, this.tabBar.Bottom + 5);
			pnlMain.Size = new Size(this.Width - 20,this.Height-40);

			pnlLeaderBoard.Size = pnlMain.Size;
			pnlGantt.Size = pnlMain.Size;
			pnlNetwork.Size = pnlMain.Size;
			pnlMaturity.Size = pnlMain.Size;
			pnlComparison.Size = pnlMain.Size;
			pnlPoints.Size = pnlMain.Size;
			pnlScoreCard.Size = pnlMain.Size;
			pnlSupportCosts.Size = pnlMain.Size;
			pnlTransition.Size = pnlMain.Size;
			pnlIncidents.Size = pnlMain.Size;
			pnlProcess.Size = pnlMain.Size;
			pnlProcess.Top = this.scorecardTabs.Height;
			pnlProcess.Height = pnlMain.Size.Height - (2*this.scorecardTabs.Height);
		}	

		public void ShowLeaderBoardPanel()
		{
			pnlLeaderBoard.Visible = true;
			if(null != Costs) Costs.Flush();

			int round = _gameFile.LastRoundPlayed;
			LeaderboardRoundSelector.SelectedIndex = round-1;

			if (this.RedrawLeaderboard == true)
			{
				ShowLeaderBoard(round);
			}
		}		

		protected virtual void ShowPointsPanel()
		{
			pnlPoints.Visible = true;
			if(null != Costs) Costs.Flush();

			int round = _gameFile.LastRoundPlayed;
			this.PointsRoundSelector.SelectedIndex = round -1;

			if (this.RedrawPoints == true)
			{
				ShowPoints(round, false);
			}
		}			

		protected void ShowPoints(int round, bool champ)
		{
#if !PASSEXCEPTIONS
			try
			{
#endif
				if(null != Costs) Costs.Flush();
				OpsRacePointsReport orp = new OpsRacePointsReport();
				string XmlFile = orp.BuildReport(_gameFile, round, champ, Scores);
				//
				System.IO.StreamReader file = new System.IO.StreamReader(XmlFile);
				string xmldata = file.ReadToEnd();
				file.Close();
				file = null;
				//

				BarGraph points = new BarGraph();

				if (points != null)
				{
					points.Location = new Point(-5,40);
					points.Size = new Size(this.Width - 10,this.Height - 100);
				}

				points.LoadData(xmldata);

				if (points != null)
				{
					ArrayList tokill = new ArrayList();
					foreach (Control c in pnlPoints.Controls)
					{
						if (c.GetType().ToString() == "Charts.BarGraph")
						{
							tokill.Add(c);
						}
					}

					pnlPoints.SuspendLayout();
					foreach(Control c in tokill)
					{
						pnlPoints.Controls.Remove(c);
						c.Dispose();
					}
					pnlPoints.ResumeLayout(false);
				}

				if (points != null)
				{
					pnlPoints.SuspendLayout();
					this.pnlPoints.Controls.Add(points);
					pnlPoints.ResumeLayout(false);

					this.RedrawPoints = false;
					points.XAxisMarked  = false;
				}
#if !PASSEXCEPTIONS
			}
			catch(Exception ex)
			{
				AppLogger.TheInstance.WriteException("Timer Level Exception", ex);
			}
#endif
		}

		protected override void ShowScoreCard()
		{
#if !PASSEXCEPTIONS
			try
			{
#endif
				if(null != Costs) Costs.Flush();
				OpsScoreCardReport scr = new OpsScoreCardReport();

				string[] xmldataArray = new string[_gameFile.LastRoundPlayed ];
				for(int i = 0; i < _gameFile.LastRoundPlayed ; i++)
				{
					if (i < Scores.Count)
					{
						xmldataArray[i] = scr.BuildReport(_gameFile, i+1, (RoundScores)Scores[i]);
					}
				}

				System.IO.StreamReader file = new System.IO.StreamReader(scr.aggregateResults(xmldataArray, _gameFile, _gameFile.LastRoundPlayed, (RoundScores)Scores[_gameFile.LastRoundPlayed-1]));
				string xmldata = file.ReadToEnd();
				file.Close();
				file = null;

				Table ScoreCard = new Table();
				ScoreCard.LoadData(xmldata);

				if (ScoreCard != null)
				{
					ArrayList tokill = new ArrayList();
					foreach (Control c in pnlScoreCard.Controls)
					{
						if (c.GetType().ToString() == "Charts.Table") 
						{
							tokill.Add(c);
						}
					}

					pnlScoreCard.SuspendLayout();
					foreach(Control c in tokill)
					{
						this.pnlScoreCard.Controls.Remove(c);
						c.Dispose();
					}
					pnlScoreCard.ResumeLayout(false);
				}

				if (ScoreCard != null)
				{
					pnlScoreCard.SuspendLayout();
					this.pnlScoreCard.Controls.Add(ScoreCard);
					pnlScoreCard.ResumeLayout(false);

					ScoreCard.Location = new Point(30,50);
					ScoreCard.Size = new Size(this.Width - 80,552);
					ScoreCard.AutoScroll = true;
					ScoreCard.BorderStyle = BorderStyle.FixedSingle;

					this.RedrawScorecard = false;
				}
#if !PASSEXCEPTIONS
			}
			catch(Exception ex)
			{
				AppLogger.TheInstance.WriteException("Timer Level Exception", ex);
			}
#endif
		}

		public override void ReloadDataAndShow(bool reload)
		{
			if (reload)
			{
				this.GetRoundScores();
			}

			RedrawScorecard = true;
			RedrawGantt = true;
			RedrawComparison = true;
			RedrawMaturity = true;
			RedrawLeaderboard = true;
			RedrawPoints = true;
			RedrawNetwork = true;
			RedrawTransition = true;
			RedrawSupportcosts = true;
			RedrawProcessScores = true;
			RedrawIncidents = true;

			ShowPanel(this.tabBar.SelectedTab);
		}

		protected void ShowLeaderBoard(int round)
		{
#if !PASSEXCEPTIONS
			try
			{
#endif
				OpsLeaderBoardReport board = new OpsLeaderBoardReport();
				string XmlFile = "";
				if (round <= Scores.Count)
				{
					XmlFile = board.BuildReport(_gameFile, round, (RoundScores)Scores[round-1]);
				}
				else
				{
					XmlFile = board.BuildReport(_gameFile, round, null);
				}

				System.IO.StreamReader file = new System.IO.StreamReader(XmlFile);
				string xmldata = file.ReadToEnd();
				file.Close();
				file = null;

				Table LeaderboardTable = new Table();
				LeaderboardTable.LoadData(xmldata);
				

				if (LeaderboardTable != null)
				{
					ArrayList tokill = new ArrayList();
					foreach (Control c in pnlLeaderBoard.Controls)
					{
						if (c.GetType().ToString() == "Charts.Table")
						{
							tokill.Add(c);
						}
					}

					pnlLeaderBoard.SuspendLayout();
					foreach(Control c in tokill)
					{
						pnlLeaderBoard.Controls.Remove(c);
						c.Dispose();
					}

					pnlLeaderBoard.Controls.Add(LeaderboardTable);

					pnlLeaderBoard.ResumeLayout(false);

					LeaderboardTable.Location = new Point(250,40);
					LeaderboardTable.Size = new Size(500,500);
					LeaderboardTable.BorderStyle = BorderStyle.FixedSingle;

					this.RedrawLeaderboard = false;
				}
#if !PASSEXCEPTIONS
			}
			catch(Exception ex)
			{
				AppLogger.TheInstance.WriteException("Timer Level Exception", ex);
			}
#endif
		}


		protected void ShowIncidentsPanel ()
		{
			this.pnlIncidents.Visible = true;
			if (null != Costs) Costs.Flush();

			int round = _gameFile.LastRoundPlayed;

			IncidentsRoundSelector.SelectedIndex = round - 1;

			if (RedrawIncidents == true)
			{
				ShowIncidents(round);
			}
		}

		protected virtual void ShowIncidents (int round)
		{
#if !PASSEXCEPTIONS
			try
			{
#endif
				if (null != Costs) Costs.Flush();

				OpsClearingIncidentsReport oir = new OpsClearingIncidentsReport();
				//oir.enableReportNotes("Notes");

				string XmlFile = "";
				if (round <= Scores.Count)
				{
					XmlFile = oir.BuildReport(_gameFile, round, Scores);
				}
				else
				{
					XmlFile = oir.BuildReport(_gameFile, round, null);
				}

				Table incidents = new Table();
				//
				System.IO.StreamReader file = new System.IO.StreamReader(XmlFile);
				string xmldata = file.ReadToEnd();
				file.Close();
				file = null;
				//

				incidents.LoadData(xmldata);

				if (incidents != null)
				{
					ArrayList tokill = new ArrayList();
					foreach (Control c in this.pnlIncidents.Controls)
					{
						if (c.GetType().ToString() == "Charts.Table")
						{
							tokill.Add(c);
						}
					}

					pnlIncidents.SuspendLayout();
					foreach (Control c in tokill)
					{
						this.pnlIncidents.Controls.Remove(c);
						c.Dispose();
					}
					pnlIncidents.ResumeLayout(false);
				}

				if (incidents != null)
				{
					pnlIncidents.AutoScroll = false;
					pnlIncidents.SuspendLayout();
					this.pnlIncidents.Controls.Add(incidents);
					pnlIncidents.ResumeLayout(false);
					pnlIncidents.AutoScroll = true;
					pnlIncidents.AutoScrollPosition = new Point(0, 0);

					incidents.Location = new Point(5, 70);

					// : Fix for 3822 (incidents report needs a scrollbar).
					incidents.Size = new Size(this.Width - 100, incidents.TableHeight);
					incidents.AutoScroll = true;

					this.RedrawIncidents = false;
				}
#if !PASSEXCEPTIONS
			}
			catch (Exception ex)
			{
				AppLogger.TheInstance.WriteException("Timer Level Exception", ex);
			}
#endif
		}

		protected virtual void IncidentsRoundSelector_SelectedIndexChanged (object sender, EventArgs e)
		{
			ShowIncidents(IncidentsRoundSelector.SelectedIndex + 1);
		}

		public override void ShowReport (ChartScreenTabOption report)
		{
			tabBar.SelectedTabCode = report.Tab.code;
			var businessIndex = (report.Business == null) ? 0 : _gameFile.NetworkModel.GetNamedNode(report.Business).GetIntAttribute("shortdesc", 0);

			switch (report.Tab.code)
			{
				case 0:
					LeaderboardRoundSelector.SelectedIndex = report.Round.Value;
					break;

				case 3:
					GanttRoundSelector.SelectedIndex = report.Round.Value;
					GanttCarSelector.SelectedIndex = businessIndex;
					break;

				case 4:
					MaturityRoundSelector.SelectedIndex = report.Round.Value;
					break;

				case 6:
					TransitionSelector.SelectedIndex = report.Round.Value;
					break;

				case 7:
					NetworkRoundSelector.SelectedIndex = report.Round.Value;
					break;

				case 8:
					IncidentsRoundSelector.SelectedIndex = report.Round.Value;
					break;

				default:
					break;
			}
		}

		public override IList<ChartScreenTabOption> GetAllAvailableReports ()
		{
			var businesses = (Node []) _gameFile.NetworkModel.GetNodesWithAttributeValue("type", "BU").ToArray(typeof(Node));
			var rounds = _gameFile.LastRoundPlayed;

			var results = new List<ChartScreenTabOption> ();

			results.Add(new ChartScreenTabOption
			{
				Tab = tabBar.GetTabByCode(1),
				Name = "Race Points",
				Business = null,
				Round = null
			});

			results.Add(new ChartScreenTabOption
			{
				Tab = tabBar.GetTabByCode(2),
				Name = "Score Card",
				Business = null,
				Round = null
			});

			for (var round = 1; round <= rounds; round++)
			{
				results.Add(new ChartScreenTabOption
				{
					Tab = tabBar.GetTabByCode(0),
					Name = "Leaderboard",
					Business = null,
					Round = round
				});

				results.Add(new ChartScreenTabOption
				{
					Tab = tabBar.GetTabByCode(4),
					Name = "Maturity",
					Business = null,
					Round = round
				});

				results.Add(new ChartScreenTabOption
				{
					Tab = tabBar.GetTabByCode(6),
					Name = "Transition",
					Business = null,
					Round = round
				});

				results.Add(new ChartScreenTabOption
				{
					Tab = tabBar.GetTabByCode(7),
					Name = "Network",
					Business = null,
					Round = round
				});

				results.Add(new ChartScreenTabOption
				{
					Tab = tabBar.GetTabByCode(8),
					Name = "Incidents",
					Business = null,
					Round = round
				});

				foreach (var business in businesses)
				{
					results.Add(new ChartScreenTabOption
					{
						Tab = tabBar.GetTabByCode(3),
						Name = "Gantt",
						Business = business.GetAttribute("name"),
						Round = round
					});
				}
			}

			return results;
		}
	}
}
