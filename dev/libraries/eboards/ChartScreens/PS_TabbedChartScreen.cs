using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;

using GameManagement;
using ReportBuilder;
using CoreUtils;
using Charts;
using Network;
using LibCore;
using TransitionScreens;

namespace ChartScreens
{
	/// <summary>
	/// Summary description for PS_TabbedChartScreen.
	/// This is actually the PoleStar version
	/// </summary>
	public class PS_TabbedChartScreen : BaseTabbedChartScreen
	{
		protected PictureBox bubble_view;
		protected ComboBox cmb_bubble;
		protected int bubbleChartLastChangedWhenPlayingRound = -1;

		protected bool redrawPerception;
		protected Panel perceptionPanel;
		protected GroupedBarChart perceptionBarChart;
		protected ComboBox perceptionRoundSelector;

		ResizingProjectsViewer pipeline;
		TrafficLightMaturityReport trafficLightMaturityReport;

		public PS_TabbedChartScreen (NetworkProgressionGameFile gameFile, SupportSpendOverrides _spend_overrides) : base(gameFile, _spend_overrides)
		{
			this.SuspendLayout();

			//add the tabs
			tabBar.AddTab("Score Card", 0, true);
			tabBar.AddTab("Gantt Chart", 1, true);
			tabBar.AddTab("Maturity", 2, true);

            if (SkinningDefs.TheInstance.GetBoolData("uses_transition_rounds", true)
				|| SkinningDefs.TheInstance.GetBoolData("uses_bubble_charts", false))
            {
                tabBar.AddTab("Portfolio", 3, true);
            }

			tabBar.AddTab("Comparison", 4, true);
			tabBar.AddTab("Transition", 5, true);
			tabBar.AddTab("Network", 6, true);

			scorecardTabs.AddTab("Game", 0, true);
			scorecardTabs.AddTab("Process", 1, true);

			//add the new tab bar and the sub tab bar (invisible)
			tabBar.Location = new Point(5,0);
			scorecardTabs.Location = new Point(5,35);
			scorecardTabs.Visible = false;

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

		protected override LogoPanelBase CreateLogoPanel()
		{
            string foldername = SkinningDefs.TheInstance.GetData("scrolling_image_folder_name");
            if (! string.IsNullOrEmpty( foldername ))
            {
                return new LogoPanelScrollingImage(null);
            }

			return new LogoPanel_IBM();
		}

	    string GetStoreName (int storeIndex)
	    {
            var storeName = CONVERT.Format("{0} {1}", SkinningDefs.TheInstance.GetData("biz"), storeIndex);
	        var storeNode = _gameFile.NetworkModel.GetNamedNode(storeName);

	        if (SkinningDefs.TheInstance.GetBoolData("gantt_show_store_descriptions", false))
	        {
	            return storeNode.GetAttribute("desc");
	        }

	        return storeName;
	    }

		protected virtual void InitialisePanels()
		{
			string transactionName =  SkinningDefs.TheInstance.GetData("transactionname");

			pnlMain = new Panel ();
			pnlGantt = new Panel { AutoScroll = true };
			pnlNetwork = new Panel { AutoScroll = true };
			pnlMaturity = new Panel();
			pnlComparison = new Panel();
			pnlScoreCard = new Panel { AutoScroll = true };
			pnlSupportCosts = new Panel { AutoScroll = true };
			pnlTransition = new Panel();
            pnlProcess = new Panel();
			pnlProcess.AutoScroll = true;
			perceptionPanel = new Panel();
			// 
			// pnlMain
			// 
			pnlMain.BackColor = System.Drawing.Color.White;

			GanttCarSelector = new ComboBox();
			GanttRoundSelector = new ComboBox();
			NetworkRoundSelector = new ComboBox();
			MaturityRoundSelector = new ComboBox();
			ComparisonSelector = new ComboBox();
			Series1 = new ComboBox();
			Series2 = new ComboBox();
			TransitionSelector = new ComboBox();
			transitionControlsDisplayPanel = new Panel();

			this.SuspendLayout();
			pnlMain.SuspendLayout();
			pnlGantt.SuspendLayout();
			pnlNetwork.SuspendLayout();
			pnlMaturity.SuspendLayout();
			pnlComparison.SuspendLayout();
			pnlTransition.SuspendLayout();
			perceptionPanel.SuspendLayout();

			this.pnlMain.Controls.Add(pnlComparison);
			this.pnlMain.Controls.Add(pnlScoreCard);
			this.pnlMain.Controls.Add(pnlSupportCosts);
			this.pnlMain.Controls.Add(pnlMaturity);
			this.pnlMain.Controls.Add(pnlTransition);
			this.pnlMain.Controls.Add(pnlGantt);
			this.pnlMain.Controls.Add(pnlNetwork);
			this.pnlMain.Controls.Add(pnlProcess);
			pnlMain.Controls.Add(perceptionPanel);
			this.pnlMain.DockPadding.All = 4;
			this.pnlMain.Name = "pnlMain";
			this.pnlMain.TabIndex = 6;
			this.Controls.Add(this.pnlMain);
			//
			// pnlGantt
			//
			this.pnlGantt.BackColor = System.Drawing.Color.White;
			this.pnlGantt.Location = new System.Drawing.Point(0, 0);
			this.pnlGantt.Name = "pnlGantt";
			this.pnlGantt.TabIndex = 0;

			//set up the combo box for the gantt chart car selections
			GanttCarSelector.DropDownStyle = ComboBoxStyle.DropDownList;
			string allbiz = SkinningDefs.TheInstance.GetData("allbiz_displayname", SkinningDefs.TheInstance.GetData("allbiz"));
			GanttCarSelector.Items.Add(allbiz);
		    string biz = SkinningDefs.TheInstance.GetData("GanttBiz", SkinningDefs.TheInstance.GetData("biz"));

		    for (int i = 1; i <= 4; i++)
		    {
		        GanttCarSelector.Items.Add(GetStoreName(i));
		    }

			GanttCarSelector.Text = allbiz;
			GanttCarSelector.Location = new Point(450,10);
            GanttCarSelector.Size = new Size (GanttCarSelector.PreferredSize.Width + 25, GanttCarSelector.PreferredHeight);
			GanttCarSelector.SelectedIndexChanged += GanttCarSelector_SelectedIndexChanged;

			pnlGantt.Controls.Add(GanttCarSelector);

			GanttRoundSelector.DropDownStyle = ComboBoxStyle.DropDownList;
			for (int i = 1; i <= SkinningDefs.TheInstance.GetIntData("roundcount", 5); i++)
			{
				GanttRoundSelector.Items.Add("Round " + CONVERT.ToStr(i));
			}
			GanttRoundSelector.Location = new Point(250,10);
			GanttRoundSelector.Size = new Size(100, 30);
			GanttRoundSelector.SelectedIndexChanged += GanttRoundSelector_SelectedIndexChanged;
			pnlGantt.Controls.Add(GanttRoundSelector);

			//pnlNetwork
			pnlNetwork.BackColor = System.Drawing.Color.White;
			pnlNetwork.Location = new System.Drawing.Point(0, 0);
			pnlNetwork.Name = "pnlNetwork";
			pnlNetwork.TabIndex = 0;

			NetworkRoundSelector.DropDownStyle = ComboBoxStyle.DropDownList;
			for (int i = 1; i <= SkinningDefs.TheInstance.GetIntData("roundcount", 5); i++)
			{
				NetworkRoundSelector.Items.Add("Round " + CONVERT.ToStr(i));
			}
			NetworkRoundSelector.Location = new Point(5,10);
			NetworkRoundSelector.Size = new Size(100, 30);
			NetworkRoundSelector.SelectedIndexChanged += NetworkRoundSelector_SelectedIndexChanged;
			pnlNetwork.Controls.Add(NetworkRoundSelector);

			//pnlMaturity
			pnlMaturity.BackColor = System.Drawing.Color.White;
			pnlMaturity.Location = new System.Drawing.Point(0, 0);
			pnlMaturity.Name = "pnlMaturity";
			pnlMaturity.TabIndex = 0;

			MaturityRoundSelector.DropDownStyle = ComboBoxStyle.DropDownList;
            if (_gameFile.Game_Eval_Type != em_GameEvalType.ISO_20K)
            {
                for (int i = 1; i <= SkinningDefs.TheInstance.GetIntData("roundcount", 5); i++)
                {
                    MaturityRoundSelector.Items.Add("Round " + CONVERT.ToStr(i));
                }
            }
            else
            {
                for (int i = 1; i <= SkinningDefs.TheInstance.GetIntData("roundcount", 5) - 1; i++)
                {
                    MaturityRoundSelector.Items.Add("Round " + CONVERT.ToStr(i));
                }
            }
			MaturityRoundSelector.Location = new Point(10,10);
			MaturityRoundSelector.Size = new Size(100,30);
			MaturityRoundSelector.SelectedIndexChanged += MaturityRoundSelector_SelectedIndexChanged;
			pnlMaturity.Controls.Add(MaturityRoundSelector);

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
			//pnlProcess.BackColor = Color.PowderBlue;

			//pnlComparison
			pnlComparison.BackColor = System.Drawing.Color.White;
			pnlComparison.Location = new System.Drawing.Point(0, 0);
			pnlComparison.Name = "pnlComparison";
			pnlComparison.TabIndex = 0;

			ComparisonSelector.DropDownStyle = ComboBoxStyle.DropDownList;
			for (int i = 1; i <= SkinningDefs.TheInstance.GetIntData("roundcount", 5); i++)
			{
				ComparisonSelector.Items.Add("Round " + CONVERT.ToStr(i));
			}
			ComparisonSelector.Location = new Point(50,10);
			ComparisonSelector.Size = new Size(100,30);
			ComparisonSelector.SelectedIndexChanged += ComparisonSelector_SelectedIndexChanged;
			this.pnlComparison.Controls.Add(ComparisonSelector);

            ArrayList items = new ArrayList();
            items.Add(transactionName + " Handled");
            items.Add("Max " + transactionName);
            items.Add("Max Revenue");
            items.Add("Actual Revenue");
            items.Add("Fixed Costs");
            items.Add("New Service Costs");
            items.Add("Regulation Fines");
            items.Add("Profit / Loss");
            items.Add("Gain / Loss");
            items.Add("Support Budget");
            items.Add("Support Spend");
            items.Add("Support Profit/Loss");
            items.Add("New Services Implemented");
            items.Add("New Services Implemented Before Round");
            items.Add("Availability");
            items.Add("MTRS");
            items.Add("Total Failures");
            items.Add("Prevented Failures");
            items.Add("Recurring Failures");
            items.Add("Workarounds");
            items.Add("SLA Breaches");
            items.Add("Maturity Indicator");
            
			//add maturity items
			RoundScores scores = new RoundScores();
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

							items.Add( vals[0] );
						}
					}
				}
			}

            items.Sort();

            foreach (string s in items)
            {
                Series1.Items.Add(s);
                Series2.Items.Add(s);
            }

            Series1.DropDownStyle = ComboBoxStyle.DropDownList;

            Series1.Text = "Points";
            Series1.Size = new Size(160, 120);
            Series1.Location = new Point(450, 10);
            Series1.SelectedIndexChanged += Series1_SelectedIndexChanged;
            pnlComparison.Controls.Add(Series1);

            Series2.DropDownStyle = ComboBoxStyle.DropDownList;

            Series2.Items.Insert(0, "NONE");
            Series2.Text = "NONE";
            Series2.Size = new Size(160, 120);
            Series2.Location = new Point(650, 10);
            Series2.SelectedIndexChanged += Series2_SelectedIndexChanged;
            //Series2.Sorted = true;
            pnlComparison.Controls.Add(Series2);

			//pnlTransition
			pnlTransition.BackColor = System.Drawing.Color.White;
			pnlTransition.Location = new System.Drawing.Point(0, 0);
			pnlTransition.Name = "pnlTransition";
			pnlTransition.TabIndex = 0;

			TransitionSelector.DropDownStyle = ComboBoxStyle.DropDownList;
			for (int i = 2; i <= SkinningDefs.TheInstance.GetIntData("roundcount", 5); i++)
			{
				TransitionSelector.Items.Add("Round " + CONVERT.ToStr(i));
			}
			TransitionSelector.Location = new Point(5,10);
			TransitionSelector.Size = new Size(100,30);
			TransitionSelector.SelectedIndexChanged += TransitionSelector_SelectedIndexChanged;
			this.pnlTransition.Controls.Add(TransitionSelector);

			transitionControlsDisplayPanel.BackColor = SkinningDefs.TheInstance.GetColorData("transition_basebackcolor");
			transitionControlsDisplayPanel.Visible = false;
			this.pnlTransition.Controls.Add(transitionControlsDisplayPanel);

			//bubble
			this.bubble_view = new PictureBox();
			bubble_view.SuspendLayout();

			cmb_bubble = new ComboBox();
			cmb_bubble.DropDownStyle = ComboBoxStyle.DropDownList;
			for (int i = 2; i <= SkinningDefs.TheInstance.GetIntData("roundcount", 5); i++)
			{
				cmb_bubble.Items.Add("Round " + CONVERT.ToStr(i));
			}
			cmb_bubble.Location = new Point(5,1);
			cmb_bubble.Size = new Size(170,25);
			cmb_bubble.SelectedIndexChanged += cmb_bubble_SelectedIndexChanged;

			ShowBubbleChartRound(2);

			bubble_view.Controls.Add(cmb_bubble);

			bubble_view.ResumeLayout(false);

			bubble_view.Visible = false;
			pnlMain.Controls.Add(bubble_view);

			// Perception.
			perceptionRoundSelector = new ComboBox();
			perceptionRoundSelector.DropDownStyle = ComboBoxStyle.DropDownList;
			perceptionPanel.Controls.Add(perceptionRoundSelector);
			for (int i = 1; i <= SkinningDefs.TheInstance.GetIntData("roundcount", 5); i++)
			{
				perceptionRoundSelector.Items.Add("Round " + CONVERT.ToStr(i));
			}
			perceptionRoundSelector.Location = new Point(5, 1);
			perceptionRoundSelector.Size = new Size(170, 25);
			perceptionRoundSelector.SelectedIndexChanged += perceptionRoundSelector_SelectedIndexChanged;

			pnlTransition.ResumeLayout(false);
			pnlComparison.ResumeLayout(false);
			pnlMaturity.ResumeLayout(false);
			pnlNetwork.ResumeLayout(false);
			pnlGantt.ResumeLayout(false);
			pnlMain.ResumeLayout(false);
			perceptionPanel.ResumeLayout(false);
			this.ResumeLayout(false);
		}

		protected override void ShowPanel(int panel)
		{
			if(null != Costs) Costs.Flush();
			this.scorecardTabs.Visible = false;

			switch(panel)
			{
				
				case 1:
					ShowGanttChartPanel();
					break;
				case 2:
					ShowMaturityPanel();
					break;
				case 3:
					ShowBubble();
					break;
				case 4:
					ShowComparisonPanel();
					break;
				case 5:
					ShowTransitionPanel();
					break;
                case 6:
                    ShowNetworkPanel();
                    break;
				case 8:
					ShowPerceptionPanel();
					break;
				case 0:
				default:
					//special case for scorecard sub tabs
					this.scorecardTabs.Visible = true;
					if (scorecardTabs.SelectedTab == 0) ShowScoreCardPanel();
					else if (scorecardTabs.SelectedTab == 1) this.ShowProcessScoresPanel();
					else if (scorecardTabs.SelectedTab == 2) this.ShowSupportCostsPanel();
					break;
			}
		}

		protected override void ShowGanttChartPanel ()
		{
			base.ShowGanttChartPanel();
		}

		protected override void HidePanels()
		{
			bubble_view.Visible = false;

			if (perceptionPanel != null)
			{
				perceptionPanel.Visible = false;
			}

			base.HidePanels();
		}

		protected void ShowBubble()
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

		protected override void DoSize()
		{
            if (SkinningDefs.TheInstance.GetBoolData("fullsize_tabbar", false))
            {
                tabBar.Size = new Size(this.Width, 30);
                tabBar.Location = new Point(0, 0);
                pnlMain.Location = new Point(0, this.tabBar.Bottom + 5);
                pnlMain.Size = new Size(this.Width, this.Height - 40);
                scorecardTabs.Size = new Size(300, 29);
            }
            else
            {
                tabBar.Size = new Size(this.Width - 50, 29);
                pnlMain.Location = new Point(10, this.tabBar.Bottom + 5);
                pnlMain.Size = new Size(this.Width - 20, this.Height - 40);
                scorecardTabs.Size = new Size(399, 29);
            }

			pnlProcess.Top = this.scorecardTabs.Height;

			if (pnlMaturity.Visible)
			{
				ShowMaturity(MaturityRoundSelector.SelectedIndex + 1);
			}

			foreach (var report in new Table [] { ScoreCard, process })
			{
				SetSize(report, false, 0);

				if (report != null)
				{
					report.TextScaleFactor = Math.Min(report.Width * 1.0f / 940, report.Parent.Height * 1.0f / 600);
					report.Height = Math.Min(report.TableHeight, report.Parent.Height);
				}
			}

			foreach (var report in new Control[] { ganttChart, comparisonChart })
			{
				SetSize(report, true, (report == ganttChart) ? 40 : 0);
			}

			if (ganttChart != null)
			{
				if (ganttChartKeyPanel != null)
				{
					ganttChartKeyPanel.Dispose();
				}

				ganttChartKeyPanel = new Panel();
				ganttChartKeyPanel.Bounds = new Rectangle (ganttChart.Left, ganttChart.Bottom, ganttChart.Width, pnlGantt.Height - ganttChart.Bottom);
				ganttChart.AddKey(ganttChartKeyPanel);

				pnlGantt.Controls.Add(ganttChartKeyPanel);
			}

			if (network != null)
			{
				pnlNetwork.Bounds = new Rectangle ((pnlMain.Width - network.Width) / 2, 0, network.Width + 30, Math.Max(network.Height, pnlMain.Height));
			}

			pnlSupportCosts.Size = pnlMain.Size;

			pnlTransition.Size = pnlMain.Size;
			transitionControlsDisplayPanel.Width = pnlTransition.Width;

			bubble_view.Size = pnlMain.Size;

			transitionControlsDisplayPanel.Bounds = new Rectangle (0, TransitionSelector.Bottom + 10, pnlTransition.Width, pnlTransition.Height - (TransitionSelector.Bottom + 10));
			if (pipeline != null)
			{
				pipeline.Bounds = new Rectangle(0, transitionControlsDisplayPanel.Height / 4, transitionControlsDisplayPanel.Width / 2, transitionControlsDisplayPanel.Height / 2);
				if (calendarViewer != null)
				{
					calendarViewer.Bounds = new Rectangle(pipeline.Right, 0, transitionControlsDisplayPanel.Width / 2, transitionControlsDisplayPanel.Height);
				}
			}

			if (perceptionPanel != null)
			{
				perceptionPanel.Size = pnlMain.Size;
			}
		}

		public override void ReloadDataAndShow(bool reload)
		{
			this.GetRoundScores();

			RedrawScorecard = true;
			RedrawGantt = true;
			RedrawComparison = true;
			RedrawMaturity = true;
			RedrawNetwork = true;
			RedrawTransition = true;
			RedrawSupportcosts = true;
			RedrawProcessScores = true;
			redrawPerception = true;

			ShowPanel(tabBar.SelectedTabCode);
		}

		protected virtual void cmb_bubble_SelectedIndexChanged(object sender, EventArgs e)
		{
			ComboBox cmb_bubble = sender as ComboBox;
			this.bubble_view.SizeMode = PictureBoxSizeMode.CenterImage;

			switch(cmb_bubble.SelectedIndex)
			{
				case 0:
					bubble_view.Image = Repository.TheInstance.GetImage(LibCore.AppInfo.TheInstance.Location + "\\images\\bubbles\\bubble2.png");
					break;

				case 1:
					bubble_view.Image = Repository.TheInstance.GetImage(LibCore.AppInfo.TheInstance.Location + "\\images\\bubbles\\bubble3.png");
					break;

				case 2:
					bubble_view.Image = Repository.TheInstance.GetImage(LibCore.AppInfo.TheInstance.Location + "\\images\\bubbles\\bubble4.png");
					break;

				case 3:
					bubble_view.Image = Repository.TheInstance.GetImage(LibCore.AppInfo.TheInstance.Location + "\\images\\bubbles\\bubble5.png");
					break;
			}
		}

		protected virtual void ShowBubbleChartRound (int round)
		{
			// Keep it within sensible values.
			cmb_bubble.SelectedIndex = Math.Max(0, Math.Min(4, round - 2));
			bubbleChartLastChangedWhenPlayingRound = _gameFile.CurrentRound;
		}

		void ShowPerceptionPanel ()
		{
			perceptionPanel.Show();

			if (Costs != null)
			{
				Costs.Flush();
			}

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

			if (round <= _gameFile.LastRoundPlayed && proceed)
			{
				PerceptionSurveyReport report = new PerceptionSurveyReport();
				BasicXmlDocument xml = BasicXmlDocument.CreateFromFile(report.BuildReport(_gameFile, round));
				perceptionBarChart = new GroupedBarChart(xml.DocumentElement);
				perceptionPanel.Controls.Add(perceptionBarChart);
				perceptionBarChart.Size = perceptionPanel.Size;
				perceptionBarChart.XAxisHeight = 50;
				perceptionBarChart.YAxisWidth = 50;
				perceptionBarChart.LegendX = perceptionRoundSelector.Right + 50;
				perceptionBarChart.LegendHeight = 40;
			}

			perceptionPanel.ResumeLayout(false);
		}

		protected void perceptionRoundSelector_SelectedIndexChanged (object sender, EventArgs args)
		{
			ShowPerception(1 + perceptionRoundSelector.SelectedIndex);
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="round"></param>
		/// <returns>Whether we have allocated controls for the transistion data</returns>
		protected override bool ShowTransition (int round)
		{
			NodeTree model = GetModelForTransitionRound(round);
			if (model == null)
			{
				// Not played this round yet, so nothing to show.
				return false;
			}

			RemoveTransitionControls();

			pipeline = new ResizingProjectsViewer (model, round) { DrawTitle = false, DrawHeaders = false };
			transitionControlsDisplayPanel.Controls.Add(pipeline);

			calendarViewer = new WorkScheduleViewer (model);
			calendarViewer.SetCalendarRowsAndCols(5, 5);
			calendarViewer.CalendarOffsetTopY = 35;
			calendarViewer.CalendarOffsetBottomY = 10;
			transitionControlsDisplayPanel.Controls.Add(calendarViewer);

			RedrawTransition = false;
			transitionControlsDisplayPanel.Visible = true;

			DoSize();

			return true;
		}

		protected override void RemoveTransitionControls ()
		{
			base.RemoveTransitionControls();
			pipeline?.Dispose();
		}

		protected override void ShowMaturity (int round)
		{
			if (maturity != null)
			{
				pnlMaturity.Controls.Remove(maturity);
				maturity.Dispose();
				maturity = null;
			}

			if (trafficLightMaturityReport != null)
			{
				pnlMaturity.Controls.Remove(trafficLightMaturityReport);
				trafficLightMaturityReport.Dispose();
				trafficLightMaturityReport = null;
			}

			pnlMaturity.Bounds = new Rectangle(0, 0, pnlMain.Width, pnlMain.Height);

			if (_gameFile.Game_Eval_Type == em_GameEvalType.ISO_20K)
			{
				string imagePathEnd = @"\images\iso20k_maturity\";
				Iso20KMaturityScores scores = new Iso20KMaturityScores(_gameFile,
					(RoundScores []) Scores.ToArray(typeof (RoundScores)), imagePathEnd);

				string imagePath = AppInfo.TheInstance.Location + imagePathEnd;

				List<string> keyLabels = new List<string>(new string [] { "Non-Compliant", "In Progress", "Compliant" });
				Dictionary<string, Image> keyLabelToIcon = new Dictionary<string, Image>();
				keyLabelToIcon.Add("Non-Compliant", Repository.TheInstance.GetImage(imagePath + "icon_red.png"));
				keyLabelToIcon.Add("In Progress", Repository.TheInstance.GetImage(imagePath + "icon_amber.png"));
				keyLabelToIcon.Add("Compliant", Repository.TheInstance.GetImage(imagePath + "icon_green.png"));

				trafficLightMaturityReport = new TrafficLightMaturityReport(imagePath + "background.png",
					keyLabels, keyLabelToIcon,
					scores.GetRatings(round),
					true);
				trafficLightMaturityReport.Location = new Point(5, 0);
				trafficLightMaturityReport.Size = new Size(Width - (2 * trafficLightMaturityReport.Left), Height);
				pnlMaturity.Controls.Add(trafficLightMaturityReport);
				RedrawMaturity = false;
			}
			else
			{
				base.ShowMaturity(round);
			}
		}

		public override void ShowReport (ChartScreenTabOption report)
		{
			tabBar.SelectedTabCode = report.Tab.code;
			var businessIndex = (report.Business == null) ? 0 : _gameFile.NetworkModel.GetNamedNode(report.Business).GetIntAttribute("shortdesc", 0);

			switch (report.Tab.code)
			{
				case 1:
					GanttRoundSelector.SelectedIndex = report.Round.Value;
					GanttCarSelector.SelectedIndex = businessIndex;
					break;

				case 2:
					MaturityRoundSelector.SelectedIndex = report.Round.Value;
					break;

				case 5:
					TransitionSelector.SelectedIndex = report.Round.Value;
					break;

				case 6:
					NetworkRoundSelector.SelectedIndex = report.Round.Value;
					break;

				default:
					break;
			}
		}

		public override IList<ChartScreenTabOption> GetAllAvailableReports ()
		{
			var businesses = (Node[]) _gameFile.NetworkModel.GetNodesWithAttributeValue("type", "BU").ToArray(typeof(Node));
			var rounds = _gameFile.LastRoundPlayed;

			var results = new List<ChartScreenTabOption> ();

			results.Add(new ChartScreenTabOption
			{
				Tab = tabBar.GetTabByCode(0),
				Name = "Score Card",
				Business = null,
				Round = null
			});

			for (var round = 1; round <= rounds; round++)
			{
				results.Add(new ChartScreenTabOption
				{
					Tab = tabBar.GetTabByCode(2),
					Name = "Maturity",
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
					Tab = tabBar.GetTabByCode(5),
					Name = "Transition",
					Business = null,
					Round = round
				});

				results.Add(new ChartScreenTabOption
				{
					Tab = tabBar.GetTabByCode(6),
					Name = "Network",
					Business = null,
					Round = round
				});

				foreach (var business in businesses)
				{
					results.Add(new ChartScreenTabOption
					{
						Tab = tabBar.GetTabByCode(1),
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