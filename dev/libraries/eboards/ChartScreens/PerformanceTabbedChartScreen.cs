using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;

using GameManagement;
using ReportBuilder;
using CoreUtils;
using LibCore;
using Charts;
using Logging;
using Network;
using TransitionScreens;

namespace ChartScreens
{
	/// <summary>
	/// Summary description for PerformanceTabbedChartScreen.
	/// CA Implementation of the tabbed chart screen
	/// </summary>
	public class PerformanceTabbedChartScreen : BaseTabbedChartScreen
	{
		protected PictureBox subway_panel;
		protected PictureBox subway_flash;
		protected PictureBox bubble_view;
		protected ComboBox cmb_bubble;
		protected int bubbleChartLastChangedWhenPlayingRound = -1;

		public PerformanceTabbedChartScreen(NetworkProgressionGameFile gameFile, SupportSpendOverrides _spend_overrides) : base(gameFile, _spend_overrides)
		{
			this.SuspendLayout();

			//add the tabs
			tabBar.AddTab("Score Card", 0, true);
			tabBar.AddTab("Maturity", 1, true);
			tabBar.AddTab("Processes", 2, true);
			tabBar.AddTab("Project Portfolio", 3, true);
			tabBar.AddTab("Comparison", 4, true);
			tabBar.AddTab("Transition", 5, true);

			string game_tab_title = CoreUtils.SkinningDefs.TheInstance.GetData("game_tab_title");
			if("" == game_tab_title) game_tab_title = "Game";

			string process_tab_title = CoreUtils.SkinningDefs.TheInstance.GetData("process_tab_title");
			if("" == process_tab_title) process_tab_title = "Process";

			scorecardTabs.AddTab(game_tab_title, 0, true);
			scorecardTabs.AddTab(process_tab_title, 1, true);

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
			return new LogoPanel_PS();
		}

		protected virtual void InitialisePanels()
		{
			string transactionName =  SkinningDefs.TheInstance.GetData("transactionname");

			pnlMain = new Panel();
			pnlMaturity = new Panel();
			pnlComparison = new Panel();
			pnlScoreCard = new Panel();
			pnlTransition = new Panel();
			pnlProcess = new Panel();
			pnlIncidents = new Panel();
			pnlSupportCosts = new Panel();
			pnlProcess.AutoScroll = true;
			// 
			// pnlMain
			// 
			pnlMain.BackColor = System.Drawing.Color.White;

			MaturityRoundSelector = new ComboBox();
			ComparisonSelector = new ComboBox();
			Series1 = new ComboBox();
			Series2 = new ComboBox();
			TransitionSelector = new ComboBox();
			IncidentsRoundSelector = new ComboBox();
			transitionControlsDisplayPanel = new Panel();

			this.SuspendLayout();
			pnlMain.SuspendLayout();
			pnlMaturity.SuspendLayout();
			pnlComparison.SuspendLayout();
			pnlTransition.SuspendLayout();
			pnlSupportCosts.SuspendLayout();

			this.pnlMain.Controls.Add(pnlComparison);
			this.pnlMain.Controls.Add(pnlScoreCard);
			this.pnlMain.Controls.Add(pnlSupportCosts);
			this.pnlMain.Controls.Add(pnlMaturity);
			this.pnlMain.Controls.Add(pnlTransition);
			this.pnlMain.Controls.Add(pnlProcess);
			this.pnlMain.DockPadding.All = 4;
			this.pnlMain.Name = "pnlMain";
			this.pnlMain.TabIndex = 6;
			this.Controls.Add(this.pnlMain);

			//pnlMaturity
			pnlMaturity.BackColor = System.Drawing.Color.White;
			pnlMaturity.Location = new System.Drawing.Point(0, 0);
			pnlMaturity.Name = "pnlMaturity";
			pnlMaturity.TabIndex = 0;

			MaturityRoundSelector.DropDownStyle = ComboBoxStyle.DropDownList;
			for (int i = 1; i <= SkinningDefs.TheInstance.GetIntData("roundcount", 5); i++)
			{
				MaturityRoundSelector.Items.Add("Round " + CONVERT.ToStr(i));
			}
			MaturityRoundSelector.Location = new Point(10,10);
			MaturityRoundSelector.Size = new Size(100,100);
			MaturityRoundSelector.SelectedIndexChanged += MaturityRoundSelector_SelectedIndexChanged;
			pnlMaturity.Controls.Add(MaturityRoundSelector);

			//pnlScoreCard
			pnlScoreCard.BackColor = System.Drawing.Color.White;
			pnlScoreCard.Location = new System.Drawing.Point(0, 0);
			pnlScoreCard.Name = "pnlScoreCard";
			pnlScoreCard.TabIndex = 0;

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
			ComparisonSelector.Location = new Point(50,40);
			ComparisonSelector.Size = new Size(100,100);
			ComparisonSelector.SelectedIndexChanged += ComparisonSelector_SelectedIndexChanged;
			this.pnlComparison.Controls.Add(ComparisonSelector);

			Series1.DropDownStyle = ComboBoxStyle.DropDownList;
			Series1.Items.Add(transactionName+" Handled");
			Series1.Items.Add("Max "+transactionName);
			Series1.Items.Add("Max Revenue");
			Series1.Items.Add("Actual Revenue");
			Series1.Items.Add("Fixed Costs");
			Series1.Items.Add("New Service Costs");
			Series1.Items.Add("Regulation Fines");
			Series1.Items.Add("Profit / Loss");
			Series1.Items.Add("Gain / Loss");
			Series1.Items.Add("Support Budget");
			Series1.Items.Add("Support Spend");
			Series1.Items.Add("Support Profit/Loss");
			Series1.Items.Add("New Services Implemented");
			Series1.Items.Add("New Services Implemented Before Round");
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
			Series1.Size = new Size(160,120);
			Series1.Location = new Point(450,40);
			Series1.SelectedIndexChanged += Series1_SelectedIndexChanged;
			pnlComparison.Controls.Add(Series1);

			Series2.DropDownStyle = ComboBoxStyle.DropDownList;
			Series2.Items.Add("NONE");
			Series2.Items.Add(transactionName+" Handled");
			Series2.Items.Add("Max "+transactionName);
			Series2.Items.Add("Max Revenue");
			Series2.Items.Add("Actual Revenue");
			Series2.Items.Add("Fixed Costs");
			Series2.Items.Add("New Service Costs");
			Series2.Items.Add("Regulation Fines");
			Series2.Items.Add("Profit/Loss");
			Series2.Items.Add("Gain/Loss");
			Series2.Items.Add("Support Budget");
			Series2.Items.Add("Support Spend");
			Series2.Items.Add("Support Profit/Loss");
			Series2.Items.Add("New Services Implemented");
			Series2.Items.Add("New Services Implemented Before Round");
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
			Series2.Size = new Size(160,120);
			Series2.Location = new Point(650,40);
			Series2.SelectedIndexChanged += Series2_SelectedIndexChanged;
			pnlComparison.Controls.Add(Series2);

			//add maturity items
			RoundScores scores = new RoundScores();
			if ((Scores != null) && (Scores.Count > 0))
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

			TransitionSelector.DropDownStyle = ComboBoxStyle.DropDownList;
			for (int i = 2; i <= SkinningDefs.TheInstance.GetIntData("roundcount", 5); i++)
			{
				TransitionSelector.Items.Add("Round " + CONVERT.ToStr(i));
			}
			TransitionSelector.Location = new Point(5,40);
			TransitionSelector.Size = new Size(100,100);
			TransitionSelector.SelectedIndexChanged += TransitionSelector_SelectedIndexChanged;
			this.pnlTransition.Controls.Add(TransitionSelector);

			transitionControlsDisplayPanel.BackColor = SkinningDefs.TheInstance.GetColorData("transition_basebackcolor");
			transitionControlsDisplayPanel.Location = new Point(0,95);
			transitionControlsDisplayPanel.Width = this.pnlTransition.Width;
			transitionControlsDisplayPanel.Height = 200;
			transitionControlsDisplayPanel.Visible = false;
			this.pnlTransition.Controls.Add(transitionControlsDisplayPanel);

			subway_panel = new PictureBox();

			ComboBox cmb_subway = new ComboBox();
			cmb_subway.DropDownStyle = ComboBoxStyle.DropDownList;
			cmb_subway.Items.Add("Service Strategy");
			cmb_subway.Items.Add("Service Design");
			cmb_subway.Items.Add("Service Transition");
			cmb_subway.Items.Add("Service Operation");
			cmb_subway.Items.Add("ITIL");
			cmb_subway.Location = new Point(5,1);
			cmb_subway.Size = new Size(170,25);
			cmb_subway.SelectedIndexChanged += cmb_subway_SelectedIndexChanged;

			subway_flash = new PictureBox ();
			subway_flash.Size = new Size(1000, 600);
            
			cmb_subway.SelectedIndex = 0;

			subway_panel.SuspendLayout();
			subway_panel.Controls.Add(cmb_subway);
			subway_panel.Controls.Add(subway_flash);
			subway_panel.ResumeLayout(false);

			pnlMain.Controls.Add(subway_panel);

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

			pnlTransition.ResumeLayout(false);
			pnlComparison.ResumeLayout(false);
			pnlMaturity.ResumeLayout(false);
			pnlSupportCosts.ResumeLayout(false);
			pnlMain.ResumeLayout(false);
			this.ResumeLayout(false);
		}

		protected virtual void IncidentsRoundSelector_SelectedIndexChanged(object sender, EventArgs e)
		{
			this.ShowIncidents(IncidentsRoundSelector.SelectedIndex+1);
		}

		protected override void ShowPanel(int panel)
		{
			if(null != Costs) Costs.Flush();
			this.scorecardTabs.Visible = false;

			switch(panel)
			{				
				case 1:
					ShowMaturityPanel();
					break;
				case 2:
					// Subway.
					ShowSubway();
					break;
				case 3:
					// Bubble.
					ShowBubble();
					break;
				case 4:
					ShowComparisonPanel();
					break;
				case 5:
					ShowTransitionPanel();
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

		protected override void HidePanels()
		{
			bubble_view.Visible = false;
			this.subway_panel.Visible = false;
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

		protected void ShowSubway()
		{
			this.subway_panel.Visible = true;
		}

		protected void ShowIncidentsPanel()
		{
			this.pnlIncidents.Visible = true;
			if(null != Costs) Costs.Flush();

			int round = _gameFile.LastRoundPlayed;

			IncidentsRoundSelector.SelectedIndex = round-1;

			if (RedrawIncidents == true)
			{
				ShowIncidents(round);
			}
		}

		void ShowIncidents(int round)
		{
#if !PASSEXCEPTIONS
			try
			{
#endif
				if(null != Costs) Costs.Flush();
				OpsIncidentsReport oir = new OpsIncidentsReport();
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
					foreach(Control c in tokill)
					{
						this.pnlIncidents.Controls.Remove(c);
						c.Dispose();
					}
					pnlIncidents.ResumeLayout(false);
				}

				if (incidents != null)
				{
					pnlIncidents.SuspendLayout();
					this.pnlIncidents.Controls.Add(incidents);
					pnlIncidents.ResumeLayout(false);

					incidents.Location = new Point(5,70);
					incidents.Size = new Size(this.Width - 100,this.Height - 150);

					this.RedrawIncidents = false;
				}
#if !PASSEXCEPTIONS
			}
			catch(Exception ex)
			{
				AppLogger.TheInstance.WriteException("Timer Level Exception", ex);
			}
#endif
		}

		public override void Init(ChartPanel screen)
		{
			this.RedrawScorecard = true;
			this.RedrawSupportcosts = true;
			this.RedrawProcessScores = true;
			this.RedrawKB = true;
			
			ShowPanel((int)screen);
		}

		protected override void DoSize()
		{
			tabBar.Size = new Size(this.Width-21, 29);
			scorecardTabs.Size = new Size(399,29);

			pnlMain.Location = new Point(10, this.tabBar.Bottom + 5);
			pnlMain.Size = new Size(this.Width - 20,this.Height-40);

			subway_panel.Size = pnlMain.Size;
			bubble_view.Size = pnlMain.Size;

			pnlMaturity.Size = pnlMain.Size;
			pnlComparison.Size = pnlMain.Size;
			pnlScoreCard.Size = pnlMain.Size;
			pnlTransition.Size = pnlMain.Size;
			pnlProcess.Size = pnlMain.Size;
			pnlProcess.Top = this.scorecardTabs.Height;
			pnlProcess.Height = pnlMain.Size.Height - (2*this.scorecardTabs.Height);
		}	

		//public override void ReloadData()
		public override void ReloadDataAndShow(bool reload)
		{
			if (reload)
			{
				this.GetRoundScores();
			}

			RedrawScorecard = true;
			RedrawComparison = true;
			RedrawMaturity = true;
			RedrawTransition = true;
			RedrawSupportcosts = true;
			RedrawProcessScores = true;
			RedrawGantt = true;

			ShowPanel(this.tabBar.SelectedTab);
		}

		public virtual void cmb_subway_SelectedIndexChanged(object sender, EventArgs e)
		{
			string subwayFile = null;
			switch (((ComboBox) sender).SelectedIndex)
			{
				case 0:
					subwayFile = LibCore.AppInfo.TheInstance.Location + "\\images\\subway\\strategy.png";
					break;

				case 1:
					subwayFile = LibCore.AppInfo.TheInstance.Location + "\\images\\subway\\design.png";
					break;

				case 2:
					subwayFile = LibCore.AppInfo.TheInstance.Location + "\\images\\subway\\transition.png";
					break;

				case 3:
					subwayFile = LibCore.AppInfo.TheInstance.Location + "\\images\\subway\\ops.png";
					break;

				case 4:
					subwayFile = LibCore.AppInfo.TheInstance.Location + "\\images\\subway\\itilv3.png";
					break;
			}

			if ((! string.IsNullOrEmpty(subwayFile))
				&& System.IO.File.Exists(subwayFile))
			{
				subway_flash.Load(subwayFile);
			}
		}

		public void cmb_bubble_SelectedIndexChanged(object sender, EventArgs e)
		{
			ComboBox cmb_bubble = sender as ComboBox;
			this.bubble_view.SizeMode = PictureBoxSizeMode.CenterImage;

			switch(cmb_bubble.SelectedIndex)
			{
				case 0:
					bubble_view.Image = LoadBitmapIfExists(LibCore.AppInfo.TheInstance.Location + "\\images\\bubbles\\bubble2.png");
					break;

				case 1:
					bubble_view.Image = LoadBitmapIfExists(LibCore.AppInfo.TheInstance.Location + "\\images\\bubbles\\bubble3.png");
					break;

				case 2:
					bubble_view.Image = LoadBitmapIfExists(LibCore.AppInfo.TheInstance.Location + "\\images\\bubbles\\bubble4.png");
					break;

				case 3:
					bubble_view.Image = LoadBitmapIfExists(LibCore.AppInfo.TheInstance.Location + "\\images\\bubbles\\bubble5.png");
					break;
			}
		}

		Bitmap LoadBitmapIfExists (string filename)
		{
			if (System.IO.File.Exists(filename))
			{
				return Repository.TheInstance.GetImage(filename);
			}

			return null;
		}

		protected override void Dispose (bool disposing)
		{
			if (subway_flash != null)
			{
				subway_flash.Dispose();
			}
		}

		public void ShowBubbleChartRound (int round)
		{
			// Keep it within sensible values.
			cmb_bubble.SelectedIndex = Math.Max(0, Math.Min(4, round - 2));
			bubbleChartLastChangedWhenPlayingRound = _gameFile.CurrentRound;
		}

		public override void ShowReport (ChartScreenTabOption report)
		{
			tabBar.SelectedTabCode = report.Tab.code;

			switch (report.Tab.code)
			{
				case 1:
					MaturityRoundSelector.SelectedIndex = report.Round.Value;
					break;

				case 5:
					TransitionSelector.SelectedIndex = report.Round.Value;
					break;

				default:
					break;
			}
		}

		public override IList<ChartScreenTabOption> GetAllAvailableReports ()
		{
			var rounds = _gameFile.LastRoundPlayed;

			var results = new List<ChartScreenTabOption> ();

			tabBar.AddTab("Score Card", 0, true);
			tabBar.AddTab("Maturity", 1, true);
			tabBar.AddTab("Processes", 2, true);
			tabBar.AddTab("Project Portfolio", 3, true);
			tabBar.AddTab("Comparison", 4, true);
			tabBar.AddTab("Transition", 5, true);

			results.Add(new ChartScreenTabOption
			{
				Tab = tabBar.GetTabByCode(0),
				Name = "Score",
				Business = null,
				Round = null
			});

			results.Add(new ChartScreenTabOption
			{
				Tab = tabBar.GetTabByCode(2),
				Name = "Processes",
				Business = null,
				Round = null
			});

			for (var round = 1; round <= rounds; round++)
			{
				results.Add(new ChartScreenTabOption
				{
					Tab = tabBar.GetTabByCode(1),
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
			}

			return results;
		}
	}
}