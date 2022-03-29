using System;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using CommonGUI;
using Charts;
using CoreUtils;
using GameManagement;
using LibCore;
using ReportBuilder;
using TransitionScreens;
using Network;
using Logging;

namespace ChartScreens
{
	/// <summary>
	/// Summary description for BaseTabbedChartScreen.
	/// </summary>
	public abstract class BaseTabbedChartScreen : PureTabbedChartScreen
	{
		protected LogoPanelBase logoPanel;

		protected NetworkProgressionGameFile _gameFile;

		protected System.Windows.Forms.Panel pnlMain;
		protected System.Windows.Forms.Panel pnlLeaderBoard;
		protected System.Windows.Forms.Panel pnlScoreCard;
		protected System.Windows.Forms.Panel pnlSupportCosts;
		protected System.Windows.Forms.Panel pnlComparison;
		protected System.Windows.Forms.Panel pnlPoints;
		protected System.Windows.Forms.Panel pnlTransition;
		protected System.Windows.Forms.Panel pnlGantt;
		protected System.Windows.Forms.Panel pnlMaturity;
        protected System.Windows.Forms.Panel pnlNetwork;
		protected System.Windows.Forms.Panel pnlProcess;
		protected System.Windows.Forms.Panel pnlIncidents;
		protected System.Windows.Forms.Panel pnlKB;

		protected Table ScoreCard;
		protected Table process;
		protected LineGraph comparisonChart;
		protected NetworkControl network;
		protected Panel networkBasePanel;

		//sub controls
		protected ComboBox LeaderboardRoundSelector;
		protected ComboBox GanttCarSelector;
		protected ComboBox GanttRoundSelector;
		protected ComboBox NetworkRoundSelector;
		protected ComboBox MaturityRoundSelector;
		protected ComboBox PointsRoundSelector;
		protected ComboBox ComparisonSelector;
		protected ComboBox Series1;
		protected ComboBox Series2;
		protected ComboBox TransitionSelector;
		protected ComboBox IncidentsRoundSelector;
		protected Panel transitionControlsDisplayPanel;

		protected OpsGanttChart ganttChart;
		protected Panel ganttChartKeyPanel;

		protected PieChart maturity;
		
		protected TabBar tabBar = new TabBar();
		protected TabBar scorecardTabs = new TabBar();

		protected ArrayList Scores;

		protected bool RedrawScorecard = false;
		protected bool RedrawSupportcosts = false;
		protected bool RedrawProcessScores = false;
		protected bool RedrawGantt = false;
		protected bool RedrawComparison = false;
		protected bool RedrawMaturity = false;
		protected bool RedrawLeaderboard = false;
		protected bool RedrawPoints = false;
		protected bool RedrawNetwork = false;
		protected bool RedrawTransition = false;
		protected bool RedrawIncidents = false;
		protected bool RedrawKB = false;

		protected Table Costs;

		protected SupportSpendOverrides supportOverrides;

		protected ProjectsViewer projectsViewer;

		public WorkScheduleViewer calendarViewer;

		public SupportSpendOverrides TheSupportOverrides
		{
			get
			{
				return supportOverrides;
			}
		}

		public BaseTabbedChartScreen(NetworkProgressionGameFile gameFile, SupportSpendOverrides _spend_overrides)
		{
			_gameFile = gameFile;
			supportOverrides = _spend_overrides;

			TabBar = tabBar;

			GetRoundScores();

			this.VisibleChanged += BaseTabbedChartScreen_VisibleChanged;
		}

		protected virtual void GetRoundScores()
		{
			Scores = new ArrayList();

			int prevprofit = 0;
			int newservices = 0;
			for (int i=1; i<=_gameFile.LastRoundPlayed; i++)
			{
				RoundScores score = new RoundScores(_gameFile, i, prevprofit, newservices, supportOverrides);
				Scores.Add(score);
				prevprofit = score.Profit;
				newservices = score.NumNewServices;
			}
		}

		protected abstract void ShowPanel(int panel);

    public override void Init(ChartPanel screen)
		{
			this.RedrawScorecard = true;
			this.RedrawSupportcosts = true;
			this.RedrawProcessScores = true;
			
			ShowPanel((int)screen);
		}

		protected virtual void HidePanels()
		{
			if(pnlGantt != null) pnlGantt.Visible = false;
			if(ganttChart != null)
			{
				ganttChart.Visible = false;
				ganttChart.HideBars();
			}
			if (ganttChartKeyPanel != null)
			{
				ganttChartKeyPanel.Visible = false;
			}

			if(pnlNetwork != null) pnlNetwork.Visible = false;
			if(pnlMaturity != null) pnlMaturity.Visible = false;
			if(pnlComparison != null) pnlComparison.Visible = false;
			if(pnlScoreCard != null) pnlScoreCard.Visible = false;
			if(pnlSupportCosts != null) pnlSupportCosts.Visible = false;
			if(pnlTransition != null) pnlTransition.Visible = false;
			if(pnlProcess != null) pnlProcess.Visible = false;
		}

		protected virtual void tabBar_TabPressed(object sender, TabBarEventArgs args)
		{
			HidePanels();

			ShowPanel(args.Code);
		}

		public virtual void ShowScoreCardPanel()
		{
			HidePanels();
			this.pnlScoreCard.Visible = true;
			if(null != Costs) Costs.Flush();

			if (this.RedrawScorecard == true)
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
				 if(null != Costs) Costs.Flush();
				PS_OpsScoreCardReport scr = new PS_OpsScoreCardReport();

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

				ScoreCard = new Table();
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

					pnlScoreCard.SuspendLayout();
					this.pnlScoreCard.Controls.Add(ScoreCard);
					pnlScoreCard.ResumeLayout(false);

					ScoreCard.Location = new Point(30,35);
					ScoreCard.AutoScroll = true;
					ScoreCard.Height = ScoreCard.TableHeight;

				    if (SkinningDefs.TheInstance.GetBoolData("tables_have_outer_border", true))
				    {
				        ScoreCard.BorderStyle = BorderStyle.FixedSingle;
				    }

				    RedrawScorecard = false;
					DoSize();
				}
#if !PASSEXCEPTIONS
			}
			catch(Exception ex)
			{
				AppLogger.TheInstance.WriteException("Timer Level Exception", ex);
			}
#endif
		}

		public virtual void ShowProcessScoresPanel()
			{
				HidePanels();
				this.pnlProcess.Visible = true;
				if(null != Costs) Costs.Flush();

				if (this.RedrawProcessScores == true)
				{
					ShowProcessScores();
				}
			}

		protected virtual void ShowProcessScores()
		{
#if !PASSEXCEPTIONS
			try
			{
#endif
				if(null != Costs) Costs.Flush();
				OpsProcessScoresReport procScores = new OpsProcessScoresReport();

				string[] xmldataArray = new string[_gameFile.LastRoundPlayed];
				for(int i = 0; i < _gameFile.LastRoundPlayed; i++)
				{
					if (i < Scores.Count)
					{
						xmldataArray[i] = procScores.BuildReport(_gameFile, i+1, (RoundScores)Scores[i]);
					}
				}

				process = new Table();

				int TableHeight = 0;

				string report_file_name = procScores.CombineRoundResults(xmldataArray, _gameFile, _gameFile.LastRoundPlayed, (RoundScores)Scores[_gameFile.LastRoundPlayed-1],out TableHeight);

				System.IO.StreamReader file = new System.IO.StreamReader(report_file_name);
				string xmldata = file.ReadToEnd();
				file.Close();
				file = null;

				process.LoadData(xmldata);

				if (process != null)
				{
					ArrayList tokill = new ArrayList();
					foreach (Control c in this.pnlProcess.Controls)
					{
						if (c.GetType().ToString() == "Charts.Table") 
						{
							tokill.Add(c);
						}
					}

					pnlProcess.SuspendLayout();
					foreach(Control c in tokill)
					{
						this.pnlProcess.Controls.Remove(c);
						c.Dispose();
					}
					pnlProcess.ResumeLayout(false);
				}

				if (process != null)
				{
					pnlProcess.SuspendLayout();
					this.pnlProcess.Controls.Add(process);
					pnlProcess.ResumeLayout(false);

					process.Location = new Point(30, 0);
					process.Size = new Size(this.Width - 80 - 40, process.TableHeight);
				    pnlProcess.Height = Math.Min(process.Height, 1024 - 40 - pnlProcess.Top);
					pnlProcess.AutoScroll = true;

				    if (SkinningDefs.TheInstance.GetBoolData("tables_have_outer_border", true))
				    {
				        process.BorderStyle = BorderStyle.FixedSingle;
				    }

                    this.RedrawProcessScores = false;
					DoSize();
				}
#if !PASSEXCEPTIONS
			}
			catch(Exception ex)
			{
				AppLogger.TheInstance.WriteException("Timer Level Exception", ex);
			}
#endif
		}

		public virtual void ShowSupportCostsPanel()
		{
			HidePanels();
			this.pnlSupportCosts.Visible = true;

			if (this.RedrawSupportcosts == true)
			{
				ShowSupportCosts();
			}
		}

		protected virtual void ShowSupportCosts()
		{
#if !PASSEXCEPTIONS
			try
			{
#endif
				if(null != Costs) Costs.Flush();
				OpsSupportCostsReport costs = new OpsSupportCostsReport(Scores, supportOverrides);

				string[] xmldataArray = new string[_gameFile.LastRoundPlayed];
				for(int i = 0; i < _gameFile.LastRoundPlayed; i++)
				{
					if (i < Scores.Count)
					{
						xmldataArray[i] = costs.BuildReport(_gameFile, i+1, (RoundScores)Scores[i]);
					}
				}

				Costs = new Table();

				System.IO.StreamReader file = new System.IO.StreamReader(costs.CombineRoundResults(xmldataArray, _gameFile, _gameFile.LastRoundPlayed));
				string xmldata = file.ReadToEnd();
				file.Close();
				file = null;

				Costs.LoadData(xmldata);

				Costs.Selectable = true;
				Costs.CellTextChanged += Costs_CellTextChanged;

				if (Costs != null)
				{
					ArrayList tokill = new ArrayList();
					foreach (Control c in this.pnlSupportCosts.Controls)
					{
						if (c.GetType().ToString() == "Charts.Table")
						{
							tokill.Add(c);
						}
					}
					pnlSupportCosts.SuspendLayout();
					foreach(Control c in tokill)
					{
						this.pnlSupportCosts.Controls.Remove(c);
						c.Dispose();
					}
					pnlSupportCosts.ResumeLayout(false);
				}

				if (Costs != null)
				{
					pnlSupportCosts.SuspendLayout();
					this.pnlSupportCosts.Controls.Add(Costs);
					pnlSupportCosts.ResumeLayout(false);

					Costs.Location = new Point(30,50);
					Costs.Size = new Size(this.Width - 80,512);
					Costs.AutoScroll = true;
					Costs.BorderStyle = BorderStyle.FixedSingle;
				

					this.RedrawSupportcosts = false;
				}
#if !PASSEXCEPTIONS
			}
			catch(Exception ex)
			{
				AppLogger.TheInstance.WriteException("Timer Level Exception", ex);
			}
#endif
		}		

		protected virtual void scorecardTabs_TabPressed(object sender, TabBarEventArgs args)
		{
			HidePanels();

			if (args.Code == 0)
			{
				this.ShowScoreCardPanel();
			}
			else if (args.Code == 1)
			{
				this.ShowProcessScoresPanel();
			}
			else if (args.Code == 2)
			{
				this.ShowSupportCostsPanel();
			}
		}
	
		protected virtual void ShowGanttChartPanel()
		{
			pnlGantt.Visible = true;
			if(ganttChart != null) ganttChart.Visible = true;
			if (ganttChartKeyPanel != null)
			{
				ganttChartKeyPanel.Visible = true;
			}
			if(null != Costs) Costs.Flush();

			int round = _gameFile.LastRoundPlayed;
			string car = GetSelectedStoreNameForGantt();
			GanttRoundSelector.SelectedIndex = Math.Min(GanttRoundSelector.Items.Count, round) - 1;

			if (this.RedrawGantt == true)
			{
				ShowGanttChart(round, car, (RoundScores) Scores[round-1]);
			}

			DoSize();
		}

		protected bool ShowHoverRevenue = true;

		protected virtual void ShowGanttChart(int round, string car, RoundScores rs)
		{
#if !PASSEXCEPTIONS
			try
			{
#endif
				if(null != Costs) Costs.Flush();
				OpsGanttReport ogp = new OpsGanttReport();
				string ganttXmlFile = ogp.BuildReport(_gameFile, round, car, ShowHoverRevenue, rs);
				//
				System.IO.StreamReader file = new System.IO.StreamReader(ganttXmlFile);
				string xmldata = file.ReadToEnd();
				file.Close();
				file = null;
				//

				pnlGantt.SuspendLayout();
				if (ganttChart != null)
				{
					pnlGantt.Controls.Remove(ganttChart);
					ganttChart.Dispose();
				}
				if (ganttChartKeyPanel != null)
				{
					pnlGantt.Controls.Remove(ganttChartKeyPanel);
					ganttChartKeyPanel.Dispose();
				}
				pnlGantt.ResumeLayout(false);

				ganttChart = new OpsGanttChart();
			    ganttChart.GraduateBars = SkinningDefs.TheInstance.GetBoolData("gantt_graduate_bars", true);
				ganttChart.LoadData(xmldata);

				if (! string.IsNullOrEmpty(SkinningDefs.TheInstance.GetData("gantt_left_axis_width")))
				{
					ganttChart.SetLeftAxisWidth(SkinningDefs.TheInstance.GetIntData("gantt_left_axis_width"));
				}

				if (! string.IsNullOrEmpty(SkinningDefs.TheInstance.GetData("gantt_left_axis_alignment")))
				{
					ganttChart.TheLeftAxis.SetLabelAlignment(SkinningDefs.TheInstance.GetData("gantt_left_axis_alignment"));
				}

				if (!string.IsNullOrEmpty(SkinningDefs.TheInstance.GetData("gantt_left_axis_font_size")))
				{
					ganttChart.TheLeftAxis.Font = SkinningDefs.TheInstance.GetFont(SkinningDefs.TheInstance.GetFloatData("gantt_left_axis_font_size", 8));
				}

				bool showKey = CoreUtils.SkinningDefs.TheInstance.GetBoolData("show_gantt_key", false);
				int keyHeight = 110;
				if (showKey)
				{
					keyHeight += 20;
				}

				ganttChart.Location = new Point(5,40);
				ganttChart.Size = new Size(pnlGantt.Width - ganttChart.Left - 5,this.Height - keyHeight);

				ganttChartKeyPanel = new Panel ();
				ganttChartKeyPanel.Location = new Point (ganttChart.Left, ganttChart.Bottom);
				ganttChartKeyPanel.Size = new Size (ganttChart.Width, pnlGantt.Height - ganttChartKeyPanel.Top);
				if (showKey)
				{
					ganttChart.AddKey(ganttChartKeyPanel);
				}

				pnlGantt.SuspendLayout();
				this.pnlGantt.Controls.Add(ganttChart);
				pnlGantt.Controls.Add(ganttChartKeyPanel);
				pnlGantt.ResumeLayout(false);

				this.RedrawGantt = false;
#if !PASSEXCEPTIONS
			}
			catch(Exception ex)
			{
				AppLogger.TheInstance.WriteException("Timer Level Exception", ex);
			}
#endif
		}
	
		protected virtual void GanttRoundSelector_SelectedIndexChanged(object sender, EventArgs e)
		{
			RoundScores score = null;

			if (GanttRoundSelector.SelectedIndex < Scores.Count)
			{
				score = (RoundScores) Scores[GanttRoundSelector.SelectedIndex];
			}
            this.ShowGanttChart(GanttRoundSelector.SelectedIndex + 1, GetSelectedStoreNameForGantt(), score);
		}

		protected virtual void GanttCarSelector_SelectedIndexChanged(object sender, EventArgs e)
		{
			RoundScores score = null;

			if (GanttRoundSelector.SelectedIndex < Scores.Count)
			{
				score = (RoundScores) Scores[GanttRoundSelector.SelectedIndex];
			}

            this.ShowGanttChart(GanttRoundSelector.SelectedIndex + 1, GetSelectedStoreNameForGantt(), score);
		}

	    string GetSelectedStoreNameForGantt ()
	    {
	        if (GanttCarSelector.SelectedIndex == 0)
	        {
                return SkinningDefs.TheInstance.GetData("allbiz");
	        }

	        return CONVERT.Format("{0} {1}", SkinningDefs.TheInstance.GetData("biz"), GanttCarSelector.SelectedIndex);
	    }

	    protected virtual void ShowNetworkPanel()
		{
			pnlNetwork.Visible = true;
			if(null != Costs) Costs.Flush();

			int round = _gameFile.CurrentRound;
			NetworkRoundSelector.SelectedIndex = Math.Min(NetworkRoundSelector.Items.Count, round) -1;

			if (this.RedrawNetwork == true)
			{
				ShowNetwork(round);
			}
		}

		protected virtual void ShowNetwork(int round)
		{
#if !PASSEXCEPTIONS
			try
			{
#endif
				if (Costs != null)
				{
					Costs.Flush();
				}

				NetworkReport nr = new NetworkReport ();
				string xmlFile = nr.BuildReport(_gameFile, round);

				if (networkBasePanel != null)
				{
					networkBasePanel.Dispose();
				}

				if (network != null)
				{
					network.Dispose();
				}

				network = new NetworkControl { Bounds = new Rectangle (5, 40, 980, 600) };
				network.LoadData(File.ReadAllText(xmlFile));

				networkBasePanel = new Panel { Size = new Size (network.Right, network.Bottom) };
				pnlNetwork.Controls.Add(networkBasePanel);
				networkBasePanel.Controls.Add(network);

				this.RedrawNetwork = false;
				DoSize();
#if !PASSEXCEPTIONS
			}
			catch(Exception ex)
			{
				AppLogger.TheInstance.WriteException("Timer Level Exception", ex);
			}
#endif
		}

		protected virtual void ShowMaturityPanel()
		{
			pnlMaturity.Visible = true;
			if(null != Costs) Costs.Flush();

			int round = _gameFile.LastRoundPlayed;
			MaturityRoundSelector.SelectedIndex = Math.Min(MaturityRoundSelector.Items.Count - 1, round - 1);

			ShowMaturity(round);
		}

		protected bool show_maturity_drop_shadow = true;
		protected int key_y_offset = 32;

		protected virtual void ShowMaturity(int round)
		{
			pnlMaturity.SuspendLayout();

			if (maturity != null)
			{
				pnlMaturity.Controls.Remove(maturity);
				maturity.Dispose();
				maturity = null;
			}

#if !PASSEXCEPTIONS
			try
			{
#endif
				if(null != Costs) Costs.Flush();
				OpsMaturityReport omr = new OpsMaturityReport();
				string maturityXmlFile = omr.BuildReport(_gameFile, round, Scores);
                if (maturityXmlFile != "")
                {
                    System.IO.StreamReader file = new System.IO.StreamReader(maturityXmlFile);
                    string xmldata = file.ReadToEnd();
                    file.Close();
                    file = null;

                    maturity = new PieChart();
                    maturity.ShowDropShadow = show_maturity_drop_shadow;
                    maturity.KeyYOffset = key_y_offset;

					if (maturity != null)
                    {
	                    var instep = 0;
	                    maturity.Bounds = new Rectangle(instep, instep, pnlMaturity.Width - (2 * instep), pnlMaturity.Height - (2 * instep));
                    }
					maturity.LoadData(xmldata);

	                if (! string.IsNullOrEmpty(SkinningDefs.TheInstance.GetData("maturity_background_colour")))
	                {
		                maturity.SetBackColorOverride(SkinningDefs.TheInstance.GetColorData("maturity_background_colour"));
	                }

	                if (maturity != null)
                    {
                        this.pnlMaturity.Controls.Add(maturity);
                        this.RedrawMaturity = false;
                    }
                }
#if !PASSEXCEPTIONS
			}
			catch(Exception ex)
			{
				AppLogger.TheInstance.WriteException("Timer Level Exception", ex);
			}
#endif

			pnlMaturity.ResumeLayout(false);
		}

		protected virtual void ShowComparisonPanel()
		{
			this.pnlComparison.Visible = true;
			if(null != Costs) Costs.Flush();

			int round = _gameFile.LastRoundPlayed;
			string series1 = Series1.Text;
			string series2 = Series2.Text;
			this.ComparisonSelector.SelectedIndex = Math.Min(ComparisonSelector.Items.Count, round) - 1;

			if (this.RedrawComparison == true)
			{
				ShowComparison(round, series1, series2);
			}
		}

		protected virtual OpsComparisonReport CreateComparisonReport ()
		{
			return new OpsComparisonReport ();
		}

		protected virtual void ShowComparison(int round, string series1, string series2)
		{
#if !PASSEXCEPTIONS
			try
			{
#endif
				if (null != Costs) Costs.Flush();
				OpsComparisonReport ocp = CreateComparisonReport();
				string XmlFile = ocp.BuildReport(_gameFile, round, Series1.Text, Series2.Text, Scores);

				comparisonChart = new LineGraph();
				//
				System.IO.StreamReader file = new System.IO.StreamReader(XmlFile);
				string xmldata = file.ReadToEnd();
				file.Close();
				file = null;
				//
				comparisonChart.LoadData(xmldata);

				ArrayList tokill = new ArrayList();
				foreach (Control c in this.pnlComparison.Controls)
				{
					if (c.GetType().ToString() == "Charts.LineGraph")
					{
						tokill.Add(c);
					}
				}

				pnlComparison.SuspendLayout();
				foreach (Control c in tokill)
				{
					this.pnlComparison.Controls.Remove(c);
					c.Dispose();
				}
				pnlComparison.ResumeLayout(false);
				pnlComparison.SuspendLayout();
				this.pnlComparison.Controls.Add(comparisonChart);
				pnlComparison.ResumeLayout(false);

				comparisonChart.Location = new Point(5, 70);
				comparisonChart.Size = new Size(this.Width - 100, this.Height - 150);

				this.RedrawComparison = false;

				Label round1Label = new Label();
				round1Label.Text = "1";
				round1Label.Font = CoreUtils.SkinningDefs.TheInstance.GetFont(10);
				round1Label.Location = new Point(comparisonChart.leftAxis.Width - 2,
					comparisonChart.Height - comparisonChart.xAxis.Height + 2);
				round1Label.Size = round1Label.GetPreferredSize(Size.Empty);
				comparisonChart.Controls.Add(round1Label);
				round1Label.BringToFront();

				DoSize();
#if !PASSEXCEPTIONS
			}
			catch (Exception ex)
			{
				AppLogger.TheInstance.WriteException("Timer Level Exception", ex);
			}
#endif
		}
		
		public virtual void ShowTransitionPanel()
		{
			this.pnlTransition.Visible = true;
			if(null != Costs) Costs.Flush();

			int round = _gameFile.LastRoundPlayed;
			TransitionSelector.SelectedIndex = round-2;

			//force to redraw the board
			if (this.RedrawTransition == true)
			{
				ShowTransition(round);
			}
		}

		protected NodeTree GetModelForTransitionRound (int round)
		{
			if (null != Costs) Costs.Flush();

			if (round == _gameFile.LastRoundPlayed && _gameFile.LastPhasePlayed == GameFile.GamePhase.TRANSITION)
			{
				return _gameFile.NetworkModel;
			}
			else
			{
				//read the network.xml file from the correct round
				string NetworkFile = _gameFile.GetRoundFile(round, "Network.xml", GameFile.GamePhase.TRANSITION);
				if (File.Exists(NetworkFile))
				{
					System.IO.StreamReader file = new System.IO.StreamReader(NetworkFile);
					string xmldata = file.ReadToEnd();
					file.Close();
					file = null;
					return new NodeTree(xmldata);
				}
			}

			// Not played this round yet, so nothing to show.
			return null;
		}

		protected virtual void RemoveTransitionControls ()
		{
			pnlTransition.SuspendLayout();
			if (calendarViewer != null)
			{
				this.pnlTransition.Controls.Remove(calendarViewer);
				calendarViewer.Dispose();
				calendarViewer = null;
			}
			if (projectsViewer != null)
			{
				this.pnlTransition.Controls.Remove(projectsViewer);
				projectsViewer.Dispose();
				projectsViewer = null;
			}
			pnlTransition.ResumeLayout(false);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="round"></param>
		/// <returns>Whether we have allocated controls for the transistion data</returns>
		protected virtual bool ShowTransition (int round)
		{
			NodeTree model = GetModelForTransitionRound(round);
			if (model == null)
			{
				// Not played this round yet, so nothing to show.
				return false;
			}

			RemoveTransitionControls();

			// Build the projects and calendar views...
			projectsViewer = new ProjectsViewer(model, round);
			//projectsViewer.PositionLabels(40);
			
			calendarViewer = new WorkScheduleViewer(model);
			calendarViewer.Size = new Size(429,435);

			pnlTransition.SuspendLayout();
			transitionControlsDisplayPanel.Width = pnlTransition.Width;
			transitionControlsDisplayPanel.Height = projectsViewer.Height+25 ;
			transitionControlsDisplayPanel.Controls.Add(projectsViewer);
			pnlTransition.ResumeLayout(false);
			projectsViewer.Location = new Point(0,10);
			this.RedrawTransition = false;

			pnlTransition.SuspendLayout();
			transitionControlsDisplayPanel.Width = pnlTransition.Width;
			transitionControlsDisplayPanel.Height = calendarViewer.Height +25 ;
			transitionControlsDisplayPanel.Controls.Add(calendarViewer);
			pnlTransition.ResumeLayout(false);
			calendarViewer.Location = new Point(projectsViewer.Left + projectsViewer.Width + 10,10);

			// ...and the logo panel.
			logoPanel = CreateLogoPanel();
			if (logoPanel!=null)
			{
				pnlTransition.SuspendLayout();
				transitionControlsDisplayPanel.Width = pnlTransition.Width;	
				transitionControlsDisplayPanel.Height = calendarViewer.Height +25 ;
				logoPanel.Location = new Point(0,projectsViewer.Height+40);
				logoPanel.Size = new Size(560,100);
				logoPanel.SetImageDir(_gameFile.Dir);				
				logoPanel.SetTrainingMode(false);
				logoPanel.BackColor = CoreUtils.SkinningDefs.TheInstance.GetColorData("transition_basebackcolor");
						
				transitionControlsDisplayPanel.Controls.Add(logoPanel);
				pnlTransition.ResumeLayout(false);
			}	    
			transitionControlsDisplayPanel.Visible = true;
			return true;
		}

		protected virtual LogoPanelBase CreateLogoPanel()
		{
			return new LogoPanel_HP();
		}

		protected virtual void Costs_CellTextChanged(Table sender, TextTableCell cell, string val)
		{
			this.supportOverrides.SetOverride(cell.CellName, val);
			supportOverrides.Save();

			GetRoundScores();
			this.RedrawScorecard = true;
		}
	
		protected virtual void TabbedChartScreen_Resize(object sender, EventArgs e)
		{
			DoSize();
		}

		protected abstract void DoSize();

		protected virtual void NetworkRoundSelector_SelectedIndexChanged(object sender, EventArgs e)
		{
			this.ShowNetwork(NetworkRoundSelector.SelectedIndex+1);
		}

		protected virtual void MaturityRoundSelector_SelectedIndexChanged(object sender, EventArgs e)
		{
			this.ShowMaturity(MaturityRoundSelector.SelectedIndex+1);
		}

		protected virtual void ComparisonSelector_SelectedIndexChanged(object sender, EventArgs e)
		{
			this.ShowComparison(ComparisonSelector.SelectedIndex+1, Series1.Text, Series2.Text);
		}

		protected virtual void Series1_SelectedIndexChanged(object sender, EventArgs e)
		{
			this.ShowComparison(ComparisonSelector.SelectedIndex+1, Series1.Text, Series2.Text);
		}

		protected virtual void Series2_SelectedIndexChanged(object sender, EventArgs e)
		{
			this.ShowComparison(ComparisonSelector.SelectedIndex+1, Series1.Text, Series2.Text);
		}

		protected virtual void TransitionSelector_SelectedIndexChanged(object sender, EventArgs e)
		{
			this.ShowTransition(TransitionSelector.SelectedIndex+2);
		}

		void BaseTabbedChartScreen_VisibleChanged(object sender, EventArgs e)
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
				if(null != ganttChart)
				{
					ganttChart.Dispose();
					ganttChart = null;
				}
			}
			base.Dispose (disposing);
		}

		protected void SetSize (Control report, bool setHeight, int spaceToReserveAtBottom)
		{
			if (report != null)
			{
				report.Parent.Size = new Size (pnlMain.Width - (2 * report.Parent.Left), pnlMain.Height - report.Parent.Top);

				var height = (setHeight ? (report.Parent.Height - report.Top - spaceToReserveAtBottom) : report.Height);

				report.Size = new Size (report.Parent.Width - (2 * report.Left), height);
			}
		}
	}
}