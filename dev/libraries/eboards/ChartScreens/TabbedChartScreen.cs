using System;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using System.Collections;

using CommonGUI;
using Charts;
using GameManagement;
using ReportBuilder;
using LibCore;
using TransitionScreens;
using Network;

namespace ChartScreens
{
	/// <summary>
	/// Summary description for TabbedChartScreen.
	/// </summary>
	public class TabbedChartScreen : Panel
	{
		//new tab control
		private TabBar tabBar = new TabBar();
		private TabBar scorecardTabs = new TabBar();
		private TabBar transitionTabs = new TabBar();
		private ComboBox Selector;
		private ComboBox LeaderboardRoundSelector;
		private ComboBox PointsRoundSelector;
		private ComboBox Series1;
		private ComboBox Series2;

		private Label Label1;
		private Label Label2;

		protected Chart chartControl;
		protected Table table;
		protected ProjectsViewer projects;
		protected WorkScheduleViewer calendar;

		protected NetworkProgressionGameFile gameFile;

		private int PanelSelected;
		private int ScorecardSelected;
		private int TransitionSelected;

	//	private RoundScores roundscores;
		private ArrayList Scores;

		public NetworkProgressionGameFile TheGameFile
		{
			set
			{
				gameFile = value;
				//changed game file, so refresh screen
				//RefreshScreen();
				this.ReloadData();
			}

			get
			{
				return gameFile;
			}
		}

		public TabbedChartScreen()
		{
			//set up the combo box for the gantt chart car selections
			Selector = new ComboBox();
			Selector.Visible = false;
			Selector.Size = new Size(120,20);
			Selector.Items.Add("All Cars");
			Selector.Items.Add("Car 1");
			Selector.Items.Add("Car 2");
			Selector.Items.Add("Car 3");
			Selector.Items.Add("Car 4");
			Selector.SelectedIndex = 0;

			Selector.SelectedIndexChanged += new EventHandler(Selector_SelectedIndexChanged);
			this.Controls.Add(Selector);

			//need a round selector box
			LeaderboardRoundSelector = new ComboBox();
			LeaderboardRoundSelector.Visible = false;
			LeaderboardRoundSelector.Size = new Size(120,120);
			LeaderboardRoundSelector.Items.Add("Race 1");
			LeaderboardRoundSelector.Items.Add("Race 2");
			LeaderboardRoundSelector.Items.Add("Race 3");
			LeaderboardRoundSelector.Items.Add("Race 4");
			LeaderboardRoundSelector.Items.Add("Race 5");
			LeaderboardRoundSelector.SelectedIndex = 0;

			LeaderboardRoundSelector.SelectedIndexChanged += new EventHandler(LeaderboardRoundSelector_SelectedIndexChanged);
			this.Controls.Add(LeaderboardRoundSelector);

			//set up race and championship points round selector
			PointsRoundSelector = new ComboBox();
			PointsRoundSelector.Visible = false;
			PointsRoundSelector.Size = new Size(120,120);
			PointsRoundSelector.Items.Add("Race 1");
			PointsRoundSelector.Items.Add("Race 2");
			PointsRoundSelector.Items.Add("Race 3");
			PointsRoundSelector.Items.Add("Race 4");
			PointsRoundSelector.Items.Add("Race 5");
			PointsRoundSelector.Items.Add("Championship");
			PointsRoundSelector.SelectedIndex = 0;

			PointsRoundSelector.SelectedIndexChanged += new EventHandler(PointsRoundSelector_SelectedIndexChanged);
			this.Controls.Add(PointsRoundSelector);

			//add comboboxes for comparison graph
			Series1 = new ComboBox();
			Series1.Visible = false;
			Series1.Size = new Size(160,120);
	//		Series1.Items.Add("NONE");
			Series1.Items.Add("Points");
			Series1.Items.Add("Revenue");
			Series1.Items.Add("Fixed Costs");
			Series1.Items.Add("Support Costs");
			Series1.Items.Add("Profit / Loss");
			Series1.Items.Add("Gain / Loss");
			Series1.Items.Add("Availability");
			Series1.Items.Add("MTRS");
			Series1.Items.Add("Total Incidents");
			Series1.Items.Add("Prevented Incidents");
			Series1.Items.Add("Recurring Incidents");
			Series1.Items.Add("Workarounds");

			//people
			Series1.Items.Add("Customer Satisfaction");
			Series1.Items.Add("Processing Rate");
			Series1.Items.Add("Communication");
			Series1.Items.Add("People Total");

			//products
			Series1.Items.Add("Tools");
			Series1.Items.Add("Product Selection");

			//service support
			Series1.Items.Add("Incident Management");
			Series1.Items.Add("Problem Management");
			Series1.Items.Add("Change Management");
			Series1.Items.Add("Configuration Management");
			Series1.Items.Add("Release Management");

			//service delivery
			Series1.Items.Add("Availability Management");
			Series1.Items.Add("Service Level Management");
			Series1.Items.Add("ITSCM Management");
			Series1.Items.Add("Capacity Management");
			Series1.Items.Add("Financial Management");

			//overall score
			Series1.Items.Add("Maturity Indicator");

			Series1.SelectedIndex = 0;

			Series1.SelectedIndexChanged += new EventHandler(Series1_SelectedIndexChanged);
			this.Controls.Add(Series1);

			Series2 = new ComboBox();
			Series2.Visible = false;
			Series2.Size = new Size(160,120);
			Series2.Items.Add("NONE");
			Series2.Items.Add("Points");
			Series2.Items.Add("Revenue");
			Series2.Items.Add("Fixed Costs");
			Series2.Items.Add("Support Costs");
			Series2.Items.Add("Profit / Loss");
			Series2.Items.Add("Gain / Loss");
			Series2.Items.Add("Availability");
			Series2.Items.Add("MTRS");
			Series2.Items.Add("Total Incidents");
			Series2.Items.Add("Prevented Incidents");
			Series2.Items.Add("Recurring Incidents");
			Series2.Items.Add("Workarounds");

			//people
			Series2.Items.Add("Customer Satisfaction");
			Series2.Items.Add("Processing Rate");
			Series2.Items.Add("Communication");
			Series2.Items.Add("People Total");

			//products
			Series2.Items.Add("Tools");
			Series2.Items.Add("Product Selection");

			//service support
			Series2.Items.Add("Incident Management");
			Series2.Items.Add("Problem Management");
			Series2.Items.Add("Change Management");
			Series2.Items.Add("Configuration Management");
			Series2.Items.Add("Release Management");

			//service delivery
			Series2.Items.Add("Availability Management");
			Series2.Items.Add("Service Level Management");
			Series2.Items.Add("ITSCM Management");
			Series2.Items.Add("Capacity Management");
			Series2.Items.Add("Financial Management");

			//overall score
			Series2.Items.Add("Maturity Indicator");
			Series2.SelectedIndex = 0;

			Series2.SelectedIndexChanged += new EventHandler(Series2_SelectedIndexChanged);
			this.Controls.Add(Series2);

			//add labels for comparison graph
			Label1 = new Label();
			Label1.Size = new Size(50, 20);
			Label1.Text = "Series 1";
			Label1.ForeColor = Color.Blue;
			Label1.Visible = false;
			this.Controls.Add(Label1);

			Label2 = new Label();
			Label2.Size = new Size(50, 20);
			Label2.Text = "Series 2";
			Label2.ForeColor = Color.Green;
			Label2.Visible = false;
			this.Controls.Add(Label2);

			//add the new tab bar and the sub tab bars (invisible)
			tabBar.Location = new Point(0,0);
			this.transitionTabs.Location = new Point(0,30);
			transitionTabs.Visible = false;
			scorecardTabs.Location = new Point(0,30);
			scorecardTabs.Visible = false;

			this.Controls.Add(tabBar);
			this.Controls.Add(transitionTabs);
			this.Controls.Add(scorecardTabs);

			this.Resize += new EventHandler(TabbedChartScreen_Resize);

			//add the tabs
			tabBar.AddTab("Score Card", 0, true);
			tabBar.AddTab("Gantt Chart", 1, true);
			tabBar.AddTab("Comparison Graph", 2, true);
			tabBar.AddTab("Indicator Chart", 3, true);
			tabBar.AddTab("Leaderboard", 4, true);
			tabBar.AddTab("Race Points", 5, true);
			tabBar.AddTab("Transition Report", 6, true);

			scorecardTabs.AddTab("Balanced Score Card", 0, true);
			scorecardTabs.AddTab("Support Costs", 1, true);

			transitionTabs.AddTab("Transition Screen", 0, true);
			transitionTabs.AddTab("Network", 1, true);

			//event handlers for when selected tab changed
			tabBar.TabPressed += new CommonGUI.TabBar.TabBarEventArgsHandler(tabBar_TabPressed);
			scorecardTabs.TabPressed += new CommonGUI.TabBar.TabBarEventArgsHandler(scorecardTabs_TabPressed);
			transitionTabs.TabPressed += new CommonGUI.TabBar.TabBarEventArgsHandler(transitionTabs_TabPressed);
			
			// No chart selected yet...
			chartControl = null;
			table = null;
			projects = null;
			calendar = null;

			//set the default selected panel to the first one
			PanelSelected = 0;
			ScorecardSelected = 0;
			TransitionSelected = 0;

			this.BackColor = Color.White;

			Scores = new ArrayList();
		}

		private void GetRoundScores()
		{
			Scores.Clear();

			for (int i=1; i<=gameFile.CurrentRound; i++)
			{
				RoundScores score = new RoundScores(gameFile, i, 0);
				Scores.Add(score);
			}
		}


		private void RefreshScreen()
		{
			scorecardTabs.Visible = false;
			transitionTabs.Visible = false;

			if(PanelSelected == 0)
			{
				//show scorecard sub tabs
				scorecardTabs.Visible = true;
				if (ScorecardSelected == 0)
				{
					ShowScoreCard();
				}
				if (ScorecardSelected == 1)
				{
					ShowSupportCosts();
				}
			}
			if (PanelSelected == 1)
			{
				ShowGanttChart();
			}
			if (PanelSelected == 2)
			{
				ShowComparisonGraph();
			}
			if (PanelSelected == 3)
			{
				ShowMaturityGraph();
			}
			if (PanelSelected == 4)
			{
				ShowLeaderBoard();
			}
			if (PanelSelected == 5)
			{
				ShowRacePoints();
			}
			if (PanelSelected == 6)
			{
				//show transitions sub tabs
				transitionTabs.Visible = true;

				if (TransitionSelected == 0)
				{
					ShowTransitionScreen();
				}
				if (TransitionSelected == 1)
				{
					ShowNetwork();
				}
			}
		}

		public void ReloadData()
		{
			GetRoundScores();

			LeaderboardRoundSelector.SelectedIndex = gameFile.CurrentRound-1;
			PointsRoundSelector.SelectedIndex = gameFile.CurrentRound-1;

			this.RefreshScreen();
		}

		private void TabbedChartScreen_Resize(object sender, EventArgs e)
		{
			DoSize();
		}

		private void Selector_SelectedIndexChanged(object sender, EventArgs e)
		{
			ShowGanttChart();
		}

		public void ShowLeaderBoardTab()
		{
			PanelSelected = 4;
			this.RefreshScreen();
		}

		private void PointsRoundSelector_SelectedIndexChanged(object sender, EventArgs e)
		{
			ShowRacePoints();
		}

		private void Series1_SelectedIndexChanged(object sender, EventArgs e)
		{
			ShowComparisonGraph();
		}

		private void Series2_SelectedIndexChanged(object sender, EventArgs e)
		{
			ShowComparisonGraph();
		}

		private void DoSize()
		{
			tabBar.Size = new Size(this.Width,30);
			this.scorecardTabs.Size = new Size(400,30);
			transitionTabs.Size = new Size(500,30);

			Selector.Visible = false;
			LeaderboardRoundSelector.Visible = false;
			PointsRoundSelector.Visible = false;
			Series1.Visible = false;
			Series2.Visible = false;
			Label1.Visible = false;
			Label2.Visible = false;

			int yoffset = 0;

			//check if table on subtab and if so then need to position under those
			if (PanelSelected == 0 || PanelSelected == 5 || PanelSelected == 2)
			{
				yoffset = 70;
			}
			else
			{
				yoffset = 40;
			}

			if(null != chartControl)
			{
				//default location and size of chart control
				chartControl.Location = new Point(0,yoffset + 30);
				chartControl.Size = new Size(this.Width,this.Height-yoffset-30);

				if (PanelSelected == 1)
				{
					Selector.Location = new Point(10,yoffset);
					Selector.Visible = true;
					LeaderboardRoundSelector.Location = new Point(Selector.Left + Selector.Width + 20,yoffset);
					LeaderboardRoundSelector.Visible = true;
				}
				else if (PanelSelected == 2)
				{
					LeaderboardRoundSelector.Location = new Point(90,yoffset);
					LeaderboardRoundSelector.Visible = true;
					Label1.Location = new Point(LeaderboardRoundSelector.Left + LeaderboardRoundSelector.Width + 150, yoffset);
					Label1.Visible = true;
					Series1.Location = new Point(Label1.Left + Label1.Width + 10, yoffset);
					Series1.Visible = true;
					Label2.Location = new Point(Series1.Left + Series1.Width + 50, yoffset);
					Label2.Visible = true;
					Series2.Location = new Point(Label2.Left + Label2.Width + 10, yoffset);
					Series2.Visible = true;

					chartControl.Location = new Point(30,yoffset);
					chartControl.Size = new Size(this.Width-60, this.Height-(yoffset*2));
				}
				else if (PanelSelected == 3)
				{
					LeaderboardRoundSelector.Location = new Point(10,yoffset);
					LeaderboardRoundSelector.Visible = true;

					chartControl.Location = new Point(10,yoffset + 30);
					chartControl.Size = new Size(this.Width,this.Height-yoffset-30);
				}
				else if (PanelSelected == 5)
				{
					PointsRoundSelector.Location = new Point(10,yoffset);
					PointsRoundSelector.Visible = true;
				}
				else if (PanelSelected == 6)
				{
					LeaderboardRoundSelector.Location = new Point(600,yoffset);
					LeaderboardRoundSelector.Visible = true;

					chartControl.Location = new Point(10,yoffset + 30);
					chartControl.Size = new Size(this.Width,this.Height-yoffset-30);
				}
			}
			if(null != table)
			{
				if (PanelSelected == 4)
				{
					LeaderboardRoundSelector.Location = new Point(10,yoffset);
					LeaderboardRoundSelector.Visible = true;

					table.Location = new Point(220,yoffset + 60);
					table.Size = new Size(500,500);

				}
				else
				{
					table.Location = new Point(30,yoffset);
					table.Size = new Size( (this.Width-60)-table.AutoScrollMargin.Width, this.Height-yoffset-10);
					
					table.AutoScroll = true;
				}
			}
			if (projects != null)
			{
				LeaderboardRoundSelector.Location = new Point(600,yoffset);
				LeaderboardRoundSelector.Visible = true;

				projects.Location = new Point(0,yoffset + 100);
			}
			if (calendar != null)
			{
				calendar.Location = new Point(projects.Left + projects.Width + 10,yoffset + 100);
				calendar.Size = new Size(429,420);
			
			}
		}

		private void DisposeControls()
		{
			if(null != chartControl)
			{
				chartControl.Dispose();
				chartControl = null;
			}
			if (null != table)
			{
				table.Dispose();
				table = null;
			}
			if (projects != null)
			{
				projects.Dispose();
				projects = null;
			}
			if (calendar != null)
			{
				calendar.Dispose();
				calendar = null;
			}
		}

		private void ShowNetwork()
		{
			//dipose of controls to ensure reports updated
			DisposeControls();

			string rnd = LeaderboardRoundSelector.Text;
			int round = gameFile.CurrentRound;
			if (rnd == string.Empty)
			{
				round = gameFile.CurrentRound;
			}
			else
			{
				string tmp = rnd.Substring(5);
				round = CONVERT.ParseInt(tmp);
			}

			NetworkReport nr = new NetworkReport();
			string ganttXmlFile = nr.BuildReport(gameFile, round);

			NetworkControl net = new NetworkControl();
			chartControl = net;
			//
			System.IO.StreamReader file = new System.IO.StreamReader(ganttXmlFile);
			string xmldata = file.ReadToEnd();
			file.Close();
			//
			chartControl.LoadData(xmldata);

			this.Controls.Add(chartControl);
			DoSize();

		}

		private void ShowGanttChart()
		{
			//dipose of controls to ensure reports updated
			DisposeControls();

			string selection = Selector.Text;

			string rnd = LeaderboardRoundSelector.Text;
			int round = gameFile.CurrentRound;
			if (rnd == string.Empty)
			{
				round = gameFile.CurrentRound;
			}
			else
			{
				string tmp = rnd.Substring(5);
				round = CONVERT.ParseInt(tmp);
			}

			OpsGanttReport ogp = new OpsGanttReport();
			string ganttXmlFile = ogp.BuildReport(gameFile, round, selection);
			OpsGanttChart opg = new OpsGanttChart();
			chartControl = opg;
			//
			System.IO.StreamReader file = new System.IO.StreamReader(ganttXmlFile);
			string xmldata = file.ReadToEnd();
			file.Close();
			//
			chartControl.LoadData(xmldata);

			this.Controls.Add(chartControl);
			DoSize();
		}

		private void ShowComparisonGraph()
		{
			//dipose of controls to ensure reports updated
			DisposeControls();

			string selection = LeaderboardRoundSelector.Text;
			int round = gameFile.CurrentRound;
			if (selection == string.Empty)
			{
				round = gameFile.CurrentRound;
			}
			else
			{
				string tmp = selection.Substring(5);
				round = CONVERT.ParseInt(tmp);
			}

			OpsComparisonReport ocp = new OpsComparisonReport();
			string XmlFile = ocp.BuildReport(gameFile, round, Series1.Text, Series2.Text, Scores);

			LineGraph line = new LineGraph();
			chartControl = line;
			//
			System.IO.StreamReader file = new System.IO.StreamReader(XmlFile);
			string xmldata = file.ReadToEnd();
			file.Close();
			//
			chartControl.LoadData(xmldata);

			this.Controls.Add(chartControl);
			DoSize();
		}

		private void ShowRacePoints()
		{
			//dipose of controls to ensure reports updated
			DisposeControls();

			//get the round to show
			string selection = PointsRoundSelector.Text;

			int round = gameFile.CurrentRound;
			bool champ = false;
			if (selection == "Championship")
			{
				round = 5;
				champ = true;
			}
			else
			{
				champ = false;
				if (selection == string.Empty)
				{
					round = gameFile.CurrentRound;
				}
				else
				{
					string tmp = selection.Substring(5);
					round = CONVERT.ParseInt(tmp);
				}
			}

			OpsRacePointsReport orp = new OpsRacePointsReport();
			string XmlFile = orp.BuildReport(gameFile, round, champ, Scores);
			BarGraph bar = new BarGraph();
			bar.XAxisMarked = false;
			chartControl = bar;
			//
			System.IO.StreamReader file = new System.IO.StreamReader(XmlFile);
			string xmldata = file.ReadToEnd();
			file.Close();
			//
			chartControl.LoadData(xmldata);

			this.Controls.Add(chartControl);
			DoSize();
		}

		private void ShowMaturityGraph()
		{
			//dipose of controls to ensure reports updated
			DisposeControls();

			string selection = LeaderboardRoundSelector.Text;
			int round = gameFile.CurrentRound;
			if (selection == string.Empty)
			{
				round = gameFile.CurrentRound;
			}
			else
			{
				string tmp = selection.Substring(5);
				round = CONVERT.ParseInt(tmp);
			}

			OpsMaturityReport omr = new OpsMaturityReport();
			string maturityXmlFile = omr.BuildReport(gameFile, round);

			PieChart mat = new PieChart();
			chartControl = mat;

			System.IO.StreamReader file = new System.IO.StreamReader(maturityXmlFile);
			string xmldata = file.ReadToEnd();
			file.Close();
			//
			chartControl.LoadData(xmldata);

			this.Controls.Add(chartControl);
			DoSize();
		}

		private void ShowTransitionScreen()
		{
			//dipose of controls to ensure reports updated
			DisposeControls();

			string selection = LeaderboardRoundSelector.Text;
			int round = gameFile.CurrentRound;
			if (selection == string.Empty)
			{
				round = gameFile.CurrentRound;
			}
			else
			{
				string tmp = selection.Substring(5);
				round = CONVERT.ParseInt(tmp);
			}

			NodeTree model;
			if (round == gameFile.CurrentRound)
			{
				model = gameFile.NetworkModel;
			}
			else
			{
				//read the network.xml file from the correct round
				string NetworkFile = gameFile.GetRoundFile(round, "Network.xml", gameFile.CurrentPhase);
				if (File.Exists(NetworkFile))
				{
					System.IO.StreamReader file = new System.IO.StreamReader(NetworkFile);
					string xmldata = file.ReadToEnd();
					file.Close();
					model = new NodeTree(xmldata);
				}
				else
				{
					//this round not been played yet, so no data
					return;
				}
			}

			projects = new ProjectsViewer(model, round);

			this.Controls.Add(projects);
			DoSize();

			calendar = new WorkScheduleViewer(model);
			this.Controls.Add(calendar);
			DoSize();
		}

		private void ShowScoreCard()
		{
			if(Scores.Count == 0) return;

			//dipose of controls to ensure reports updated
			DisposeControls();

			OpsScoreCardReport scr = new OpsScoreCardReport();

			string[] xmldataArray = new string[gameFile.CurrentRound];
			for(int i = 0; i < this.gameFile.CurrentRound; i++)
			{
				if (i < Scores.Count)
				{
					xmldataArray[i] = scr.BuildReport(gameFile, i+1, (RoundScores)Scores[i]);
				}
			}

			System.IO.StreamReader file = new System.IO.StreamReader(scr.aggregateResults(xmldataArray, gameFile, gameFile.CurrentRound, (RoundScores)Scores[gameFile.CurrentRound-1]));
			string xmldata = file.ReadToEnd();
			file.Close();

			table = new Table();
			table.LoadData(xmldata);
		
			this.Controls.Add(table);
		}

		private void ShowSupportCosts()
		{
			//dipose of controls to ensure reports updated
			DisposeControls();

			OpsSupportCostsReport costs = new OpsSupportCostsReport();

			string[] xmldataArray = new string[gameFile.CurrentRound];
			for(int i = 0; i < this.gameFile.CurrentRound; i++)
			{
				if (i < Scores.Count)
				{
					xmldataArray[i] = costs.BuildReport(gameFile, i+1, (RoundScores)Scores[i]);
				}
			}

			Table scTable = new Table();
			table = scTable;

			System.IO.StreamReader file = new System.IO.StreamReader(costs.CombineRoundResults(xmldataArray, gameFile, gameFile.CurrentRound));
			string xmldata = file.ReadToEnd();
			file.Close();

			table.LoadData(xmldata);

			this.Controls.Add(table);
			DoSize();
		}

		public void ShowLeaderBoard()
		{
			//dipose of controls to ensure reports updated
			DisposeControls();

			//get the round to show
			string selection = LeaderboardRoundSelector.Text;
			int round = gameFile.CurrentRound;
			if (selection == string.Empty)
			{
				round = gameFile.CurrentRound;
			}
			else
			{
				string tmp = selection.Substring(5);
				round = CONVERT.ParseInt(tmp);
			}

			if (Scores.Count <= round)
			{
				OpsLeaderBoardReport board = new OpsLeaderBoardReport();
				string XmlFile = board.BuildReport(gameFile, round, (RoundScores)Scores[round-1]);

				Table scTable = new Table();
				table = scTable;

				System.IO.StreamReader file = new System.IO.StreamReader(XmlFile);
				string xmldata = file.ReadToEnd();
				file.Close();

				table.LoadData(xmldata);

				if(null != table)
				{
					this.Controls.Add(table);
					DoSize();
				}
			}
		}

		private void tabBar_TabPressed(object sender, TabBarEventArgs args)
		{
			// User has chosen a different chart / report.
			PanelSelected = args.Code;

			RefreshScreen();
		}

		private void scorecardTabs_TabPressed(object sender, TabBarEventArgs args)
		{
			ScorecardSelected = args.Code;

			RefreshScreen();
		}

		private void transitionTabs_TabPressed(object sender, TabBarEventArgs args)
		{
			TransitionSelected = args.Code;

			RefreshScreen();
		}

		private void LeaderboardRoundSelector_SelectedIndexChanged(object sender, EventArgs e)
		{
			this.RefreshScreen();
		}
	}
}
