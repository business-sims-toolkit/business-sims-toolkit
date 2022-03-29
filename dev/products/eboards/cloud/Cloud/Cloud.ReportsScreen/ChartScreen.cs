using System;
using System.Windows.Forms;
using System.Drawing;
using System.IO;

using GameManagement;

using CommonGUI;

using ChartScreens;
using ReportBuilder;

using CoreUtils;
using LibCore;
using Network;

using Charts;
using System.Collections;
using System.Collections.Generic;

namespace Cloud.ReportsScreen
{
	public class ChartScreen : PureTabbedChartScreen
	{
		public delegate Control GameScreenCreator (NetworkProgressionGameFile gameFile);

		GameScreenCreator gameScreenCreator;

		NetworkProgressionGameFile gameFile;
		SupportSpendOverrides spendOverrides;

		TabBar tabBar;

		List<Cloud_RoundScores> roundScores;

		Panel mainPanel;

		Panel gameScreenPanel;
		Control gameScreen;

		Color ContentPanelBackColor = Color.Black;
		bool PreventScreenUpdate = false;

		Panel businessScoreCardPanel;
		Table businessScoreCardTable;
		bool redrawBusinessScoreCard;
		VerticalLabel VerticalLabel_BusinessAreaOverall = null;
		VerticalLabel VerticalLabel_BusinessAreaAmerica = null;
		VerticalLabel VerticalLabel_BusinessAreaAfrica = null;
		VerticalLabel VerticalLabel_BusinessAreaAsia = null;
		VerticalLabel VerticalLabel_BusinessAreaEurope = null;

		Panel operationsScoreCardPanel;
		Table operationsScoreCardTable;
		bool redrawOperationsScoreCard;
		VerticalLabel VerticalLabel_OperationsAreaAmerica = null;
		VerticalLabel VerticalLabel_OperationsAreaAfrica = null;
		VerticalLabel VerticalLabel_OperationsAreaAsia = null;
		VerticalLabel VerticalLabel_OperationsAreaEurope = null;

		Panel cpuUsagePanel;
		StackBarChartWithBackground cpuUsageGraph;
		bool redrawCpuUsageGraph;
		ToggleButtonBar CpuUsage_Round_ToggleBar;
		ToggleButtonBar CpuUsage_DataCenter_ToggleBar;
		ToggleButtonBar CpuUsage_Owner_ToggleBar;

		Panel newServicesPanel;
		CloudTimeChart newServicesChart;
		bool redrawNewServicesChart;
		FlickerFreePanel newServicesKey;
		ToggleButtonBar NewServices_Round_ToggleBar;
		ToggleButtonBar NewServices_BusinessRegion_ToggleBar;

		Panel opexPanel;
		Table opexTable;
		bool redrawOpexTable;
		VerticalLabel VerticalLabel_OpexAreaAmerica = null;
		VerticalLabel VerticalLabel_OpexAreaAfrica = null;
		VerticalLabel VerticalLabel_OpexAreaAsia = null;
		VerticalLabel VerticalLabel_OpexAreaEurope = null;

		Panel leaderboardPanel;
		Table leaderboardTable;
		bool redrawLeaderboardTable;
		ComboBox leaderboardRoundSelector;

		Panel bubblePanel;
		IntentImagePanel bubbleGraph;
		ToggleButtonBar BubbleChart_Round_ToggleBar;
		ToggleButtonBar BubbleChart_BusinessRegion_ToggleBar;

		Panel serviceCatalogPanel;
		Table serviceCatalogTable;
		bool redrawServiceCatalog;
		ComboBox serviceCatalogRoundSelector;

		Panel chargebackPanel;
		ToggleButtonBar ChargeBack_Round_ToggleBar;
		ToggleButtonBar ChargeBack_Type_ToggleBar;
		Table chargebackTable;
		bool redrawChargeback;
		VerticalLabel VerticalLabel_chargeBackAreaAmerica = null;
		VerticalLabel VerticalLabel_chargeBackAreaAfrica = null;
		VerticalLabel VerticalLabel_chargeBackAreaAsia = null;
		VerticalLabel VerticalLabel_chargeBackAreaEurope = null;

		Panel intentPanel;
		ToggleButtonBar intentToggleBar;
		IntentImagePanel intentImage;

		bool suppressUpdates;
		Font Font_BusinessAreaTitle;

		public ChartScreen (NetworkProgressionGameFile gameFile, SupportSpendOverrides spendOverrides, GameScreenCreator gameScreenCreator)
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.DoubleBuffer, true);

			this.gameFile = gameFile;
			this.spendOverrides = spendOverrides;
			this.gameScreenCreator = gameScreenCreator;

			string font = SkinningDefs.TheInstance.GetData("fontname");
			Font_BusinessAreaTitle = FontRepository.GetFont(font, 12, FontStyle.Bold);

			GetRoundScores();

			mainPanel = new Panel ();
			mainPanel.BackColor = ContentPanelBackColor;
			Controls.Add(mainPanel);

			tabBar = new TabBar ();
			tabBar.AddTab("Business", 1, true);
			tabBar.AddTab("Operations", 2, true);
			tabBar.AddTab("New Services", 4, true);
			tabBar.AddTab("CPU Usage", 3, true);
			tabBar.AddTab("Opex", 5, true);
			tabBar.AddTab("Showback", 10, true);
			tabBar.AddTab("Intent", 11, true);
			tabBar.AddTab("Portfolio", 12, true);

			tabBar.Location = new Point (10, 5);
			tabBar.TabPressed += new TabBar.TabBarEventArgsHandler (tabBar_TabPressed);
			Controls.Add(tabBar);

			CreateBusinessScoreCardPanel();
			CreateOperationsScoreCardPanel();
			CreateLeaderboardPanel();
			CreateNewServicesPanel();
			CreateCpuUsagePanel();
			CreateOpexPanel();
			CreateChargebackPanel();
			CreateIntentPanel();
			CreateBubblePanel();

			CreateGameScreenPanel();
			CreateServiceCatalogPanel();
		}

		void CreateChargebackPanel ()
		{
			chargebackPanel = new Panel ();
			chargebackPanel.BackColor = ContentPanelBackColor;
			mainPanel.Controls.Add(chargebackPanel);

			ArrayList al = new ArrayList();
			int t_count = 0;
			string first_name = string.Empty;
			for (int i = 3; i <= SkinningDefs.TheInstance.GetIntData("roundcount", 1); i++)
			{
				string name = CONVERT.Format("{0}", i);
				al.Add(new ToggleButtonBarItem(t_count, name, i));
				t_count++;

				if (first_name == string.Empty)
				{
					first_name = name;
				}
			}

			int pos_x = 0;
			int pos_y = 5;
			Color tmpBackColor = Color.FromArgb(142, 136, 150);

			ChargeBack_Round_ToggleBar = new ToggleButtonBar(false);
			ChargeBack_Round_ToggleBar.SetControlAlignment(emToggleButtonBarAlignment.HORIZONTAL);
			ChargeBack_Round_ToggleBar.SetLabel("Round", new Point(2, 4), new Size(60, 20), Color.White, tmpBackColor, Font_BusinessAreaTitle);
			ChargeBack_Round_ToggleBar.SetAllowNoneSelected(false);
			ChargeBack_Round_ToggleBar.SetOptions(al, 32, 25, 4, 4, "images/buttons/button_65x25_on.png", "images/buttons/button_65x25_active.png",
							"images/buttons/button_65x25_disabled.png", "images/buttons/button_65x25_hover.png", Color.White, first_name);
			ChargeBack_Round_ToggleBar.Size = new Size(160, 30);
			ChargeBack_Round_ToggleBar.Location = new Point(pos_x, pos_y);
			ChargeBack_Round_ToggleBar.BackColor = tmpBackColor;
			ChargeBack_Round_ToggleBar.sendItemSelected += new ToggleButtonBar.ItemSelectedHandler(ChargeBack_Round_ToggleBar_sendItemSelected);
			chargebackPanel.Controls.Add(ChargeBack_Round_ToggleBar);

			pos_x += ChargeBack_Round_ToggleBar.Width + 10;
			pos_y += 0;

			ArrayList al2 = new ArrayList();
			al2.Add(new ToggleButtonBarItem(0, "Existing Services", 0));
			al2.Add(new ToggleButtonBarItem(1, "New Services", 1));

			ChargeBack_Type_ToggleBar = new ToggleButtonBar(false);
			ChargeBack_Type_ToggleBar.SetControlAlignment(emToggleButtonBarAlignment.HORIZONTAL);
			ChargeBack_Type_ToggleBar.SetLabel("Show", new Point(2, 4), new Size(60, 20), Color.White, tmpBackColor, Font_BusinessAreaTitle);
			ChargeBack_Type_ToggleBar.SetAllowNoneSelected(false);
			ChargeBack_Type_ToggleBar.SetOptions(al2, 100, 25, 4, 4, "images/buttons/button_100x25_on.png", "images/buttons/button_100x25_active.png",
							"images/buttons/button_100x25_disabled.png", "images/buttons/button_100x25_hover.png", Color.White, first_name);
			ChargeBack_Type_ToggleBar.Size = new Size(280, 30);
			ChargeBack_Type_ToggleBar.Location = new Point(pos_x, pos_y);
			ChargeBack_Type_ToggleBar.BackColor = tmpBackColor;
			ChargeBack_Type_ToggleBar.sendItemSelected += new ToggleButtonBar.ItemSelectedHandler(ChargeBack_Type_ToggleBar_sendItemSelected);
			chargebackPanel.Controls.Add(ChargeBack_Type_ToggleBar);
		}

		void CreateIntentPanel ()
		{
			intentPanel = new Panel ();
			intentPanel.BackColor = ContentPanelBackColor;
			mainPanel.Controls.Add(intentPanel);

			ArrayList al = new ArrayList();
			int t_count = 0;
			string first_name = string.Empty;
			for (int i = 1; i < SkinningDefs.TheInstance.GetIntData("roundcount", 1); i++)
			{
				string name = CONVERT.Format("{0}", i);
				al.Add(new ToggleButtonBarItem(t_count, name, i));
				t_count++;

				if (first_name == string.Empty)
				{
					first_name = name;
				}
			}

			Color tmpBackColor = Color.FromArgb(142, 136, 150);
			intentToggleBar = new ToggleButtonBar(false);
			intentToggleBar.SetControlAlignment(emToggleButtonBarAlignment.HORIZONTAL);
			intentToggleBar.SetLabel("Round", new Point(2, 4), new Size(60, 20), Color.White, tmpBackColor, Font_BusinessAreaTitle);
			intentToggleBar.SetAllowNoneSelected(false);
			intentToggleBar.SetOptions(al, 32, 25, 4, 4, "images/buttons/button_65x25_on.png", "images/buttons/button_65x25_active.png",
							"images/buttons/button_65x25_disabled.png", "images/buttons/button_65x25_hover.png", Color.White, first_name);
			intentToggleBar.Size = new Size(290, 30);
			intentToggleBar.Location = new Point(0, 5);
			intentToggleBar.BackColor = tmpBackColor;
			intentToggleBar.sendItemSelected += new ToggleButtonBar.ItemSelectedHandler(intentToggleBar_sendItemSelected);
			intentPanel.Controls.Add(intentToggleBar);
		}

		void CreateGameScreenPanel ()
		{
			gameScreenPanel = new Panel ();
			gameScreenPanel.BackColor = ContentPanelBackColor;
			mainPanel.Controls.Add(gameScreenPanel);
		}

		void CreateBusinessScoreCardPanel ()
		{
			businessScoreCardPanel = new Panel ();
			businessScoreCardPanel.BackColor = ContentPanelBackColor;
			mainPanel.Controls.Add(businessScoreCardPanel);
		}

		void CreateOperationsScoreCardPanel ()
		{
			operationsScoreCardPanel = new Panel();
			operationsScoreCardPanel.BackColor = ContentPanelBackColor;
			mainPanel.Controls.Add(operationsScoreCardPanel);
		}

		void CreateOpexPanel ()
		{
			opexPanel = new Panel ();
			opexPanel.BackColor = ContentPanelBackColor;
			mainPanel.Controls.Add(opexPanel);
		}

		void CreateCpuUsagePanel ()
		{
			cpuUsagePanel = new Panel ();
			cpuUsagePanel.BackColor = ContentPanelBackColor;
			mainPanel.Controls.Add(cpuUsagePanel);

			//=============================================================
			//Build the Round Toggle Bar Data 
			//=============================================================
			ArrayList al = new ArrayList();
			int t_count = 0;
			string first_name = string.Empty;
			for (int i = 1; i <= SkinningDefs.TheInstance.GetIntData("roundcount", 1); i++)
			{
				string name = CONVERT.Format("{0}", i);
				al.Add(new ToggleButtonBarItem(t_count, name, i));
				t_count++;
				if (first_name == string.Empty)
				{
					first_name = name;
				}
			}

			int pos_x = 0;
			int pos_y = 5;
			Color tmpBackColor = Color.FromArgb(142, 136, 150);

			CpuUsage_Round_ToggleBar = new ToggleButtonBar(false);
			CpuUsage_Round_ToggleBar.SetControlAlignment(emToggleButtonBarAlignment.HORIZONTAL);
			CpuUsage_Round_ToggleBar.SetLabel("Round", new Point(2, 4), new Size(55, 20), Color.White, tmpBackColor, Font_BusinessAreaTitle);
			CpuUsage_Round_ToggleBar.SetAllowNoneSelected(false);
			CpuUsage_Round_ToggleBar.SetOptions(al, 32, 25, 3, 3, "images/buttons/button_65x25_on.png", "images/buttons/button_65x25_active.png",
							"images/buttons/button_65x25_disabled.png", "images/buttons/button_65x25_hover.png", Color.White, first_name);
			CpuUsage_Round_ToggleBar.Size = new Size(200, 30);
			CpuUsage_Round_ToggleBar.Location = new Point(pos_x, pos_y);
			CpuUsage_Round_ToggleBar.BackColor = tmpBackColor;
			CpuUsage_Round_ToggleBar.sendItemSelected += new ToggleButtonBar.ItemSelectedHandler(CpuUsage_Round_ToggleBar_sendItemSelected);
			cpuUsagePanel.Controls.Add(CpuUsage_Round_ToggleBar);

			pos_x += CpuUsage_Round_ToggleBar.Width + 10;
			pos_y += 0;

			//=============================================================
			//Build the DataCenter Toggle Bar Data 
			//=============================================================
			//get The Data
			ArrayList al2 = new ArrayList();
			int t_count2 = 0;
			string first_name2 = string.Empty;

			List<Node> datacenters2 = new List<Node>((Node[])gameFile.NetworkModel.GetNodesWithAttributeValue("type", "datacenter").ToArray(typeof(Node)));
			datacenters2.Sort(delegate(Node a, Node b)
			{
				Node businessA = gameFile.NetworkModel.GetNamedNode(a.GetAttribute("business"));
				Node businessB = gameFile.NetworkModel.GetNamedNode(b.GetAttribute("business"));

				if ((businessA == null) && (businessB != null))
				{
					return 1;
				}
				else if ((businessA != null) && (businessB == null))
				{
					return -1;
				}
				else if ((businessA == null) && (businessB == null))
				{
					return 0;
				}

				return businessA.GetIntAttribute("order", 0).CompareTo(businessB.GetIntAttribute("order", 0));
			});

			List<string> allDatacenters2 = new List<string>();
			foreach (Node datacenter2 in datacenters2)
			{
				if (!datacenter2.GetBooleanAttribute("hidden", false))
				{
					allDatacenters2.Add(datacenter2.GetAttribute("name"));
				}
			}

			//adding the "All DataCentre" Item 
			al2.Add(new ToggleButtonBarItem(t_count2, "All", allDatacenters2));
			t_count2++;
			first_name2 = "All";

			//adding individual ones 
			foreach (Node datacenter2 in datacenters2)
			{
					al2.Add(new ToggleButtonBarItem(t_count2, 
						datacenter2.GetAttribute("desc"), 
						new List<string>(new string [] { datacenter2.GetAttribute("name") })						
						));
					t_count2++;
			}

			CpuUsage_DataCenter_ToggleBar = new ToggleButtonBar(false);
			CpuUsage_DataCenter_ToggleBar.SetControlAlignment(emToggleButtonBarAlignment.HORIZONTAL);
			CpuUsage_DataCenter_ToggleBar.SetLabel("Exchange", new Point(2, 4), new Size(75, 20), Color.White, tmpBackColor, Font_BusinessAreaTitle);
			CpuUsage_DataCenter_ToggleBar.SetAllowNoneSelected(false);
			CpuUsage_DataCenter_ToggleBar.SetOptions(al2, 65, 25, 3, 3, "images/buttons/button_65x25_on.png", "images/buttons/button_65x25_active.png",
							"images/buttons/button_65x25_disabled.png", "images/buttons/button_65x25_hover.png", Color.White, first_name2);
			CpuUsage_DataCenter_ToggleBar.Size = new Size(485, 30);
			CpuUsage_DataCenter_ToggleBar.Location = new Point(pos_x, pos_y);
			CpuUsage_DataCenter_ToggleBar.BackColor = tmpBackColor;
			CpuUsage_DataCenter_ToggleBar.sendItemSelected += new ToggleButtonBar.ItemSelectedHandler(CpuUsage_DataCenter_ToggleBar_sendItemSelected);
			cpuUsagePanel.Controls.Add(CpuUsage_DataCenter_ToggleBar);

			pos_x += CpuUsage_DataCenter_ToggleBar.Width + 10;
			pos_y += 0;

			//=============================================================
			//Build the DataCenter Toggle Bar Data 
			//=============================================================
			//get The Data
			ArrayList al3 = new ArrayList();
			string first_name3 = string.Empty;

			al3.Add(new ToggleButtonBarItem(0, "All", null));
			al3.Add(new ToggleButtonBarItem(1, "Dev / Test", new string[] { "dev&test" }));
			al3.Add(new ToggleButtonBarItem(2, "Production", new string[] { "floor", "online" }));
			first_name3 = "All owners";

			CpuUsage_Owner_ToggleBar = new ToggleButtonBar(false);
			CpuUsage_Owner_ToggleBar.SetControlAlignment(emToggleButtonBarAlignment.HORIZONTAL);
			CpuUsage_Owner_ToggleBar.SetLabel("Owner", new Point(2, 4), new Size(55, 20), Color.White, tmpBackColor, Font_BusinessAreaTitle);
			CpuUsage_Owner_ToggleBar.SetAllowNoneSelected(false);
			CpuUsage_Owner_ToggleBar.SetOptions(al3, 70, 25, 3, 3, "images/buttons/button_65x25_on.png", "images/buttons/button_65x25_active.png",
							"images/buttons/button_65x25_disabled.png", "images/buttons/button_65x25_hover.png", Color.White, first_name3);
			CpuUsage_Owner_ToggleBar.Size = new Size(280, 30);
			CpuUsage_Owner_ToggleBar.Location = new Point(pos_x, pos_y);
			CpuUsage_Owner_ToggleBar.BackColor = tmpBackColor;
			//CpuUsage_Owner_ToggleBar.BackColor = Color.LightSeaGreen;
			CpuUsage_Owner_ToggleBar.sendItemSelected += new ToggleButtonBar.ItemSelectedHandler(CpuUsage_Owner_ToggleBar_ToggleBar_sendItemSelected);
			cpuUsagePanel.Controls.Add(CpuUsage_Owner_ToggleBar);

			pos_x += CpuUsage_Owner_ToggleBar.Width + 10;
			pos_y += 0;
		}

		void CreateNewServicesPanel ()
		{
			newServicesPanel = new Panel ();
			newServicesPanel.BackColor = ContentPanelBackColor;
			mainPanel.Controls.Add(newServicesPanel);

			//=============================================================
			//Build the Round Toggle Bar Data 
			//=============================================================
			ArrayList al = new ArrayList();
			int t_count = 0;
			string first_name = string.Empty;
			for (int i = 1; i <= SkinningDefs.TheInstance.GetIntData("roundcount", 1); i++)
			{
				string name = CONVERT.Format("{0}", i);
				al.Add(new ToggleButtonBarItem(t_count, name, i));
				t_count++;
				if (first_name == string.Empty)
				{
					first_name = name;
				}
			}

			int pos_x = 0;
			int pos_y = 5;
			Color tmpBackColor = Color.FromArgb(142, 136, 150);

			NewServices_Round_ToggleBar = new ToggleButtonBar(false);
			NewServices_Round_ToggleBar.SetControlAlignment(emToggleButtonBarAlignment.HORIZONTAL);
			NewServices_Round_ToggleBar.SetLabel("Round", new Point(2, 4), new Size(60, 20), Color.White, tmpBackColor, Font_BusinessAreaTitle);
			NewServices_Round_ToggleBar.SetAllowNoneSelected(false);
			NewServices_Round_ToggleBar.SetOptions(al, 32, 25, 4, 4, "images/buttons/button_65x25_on.png", "images/buttons/button_65x25_active.png",
							"images/buttons/button_65x25_disabled.png", "images/buttons/button_65x25_hover.png", Color.White, first_name);
			NewServices_Round_ToggleBar.Size = new Size(230, 30);
			NewServices_Round_ToggleBar.Location = new Point(pos_x, pos_y);
			NewServices_Round_ToggleBar.BackColor = tmpBackColor;
			NewServices_Round_ToggleBar.sendItemSelected += new ToggleButtonBar.ItemSelectedHandler(NewServices_Round_ToggleBar_sendItemSelected);
			newServicesPanel.Controls.Add(NewServices_Round_ToggleBar);

			pos_x += NewServices_Round_ToggleBar.Width + 10;
			pos_y += 0;

			//=============================================================
			//Build the Business Region Bar Data  
			//=============================================================
			ArrayList al2 = new ArrayList();
			int t2_count = 0;
			string first_name2 = string.Empty;

			List<Node> businesses2 = new List<Node>((Node[])gameFile.NetworkModel.GetNodesWithAttributeValue("type", "business").ToArray(typeof(Node)));
			businesses2.Sort(delegate(Node a, Node b) { return a.GetAttribute("order").CompareTo(b.GetAttribute("order")); });
			foreach (Node business in businesses2)
			{
				string name = business.GetAttribute("desc");
				string node_name = business.GetAttribute("name");
				al2.Add(new ToggleButtonBarItem(t2_count, name, node_name));
				t2_count++;
				if (first_name2 == string.Empty)
				{
					first_name2 = name;
				}
			}

			NewServices_BusinessRegion_ToggleBar = new ToggleButtonBar(false);
			NewServices_BusinessRegion_ToggleBar.SetControlAlignment(emToggleButtonBarAlignment.HORIZONTAL);
			NewServices_BusinessRegion_ToggleBar.SetLabel("Exchange", new Point(2, 4), new Size(75, 20), Color.White, tmpBackColor, Font_BusinessAreaTitle);
			NewServices_BusinessRegion_ToggleBar.SetAllowNoneSelected(false);
			NewServices_BusinessRegion_ToggleBar.SetOptions(al2, 65, 25, 4, 4, "images/buttons/button_65x25_on.png", "images/buttons/button_65x25_active.png",
							"images/buttons/button_65x25_disabled.png", "images/buttons/button_65x25_hover.png", Color.White, first_name);
			NewServices_BusinessRegion_ToggleBar.Size = new Size(375, 30);
			NewServices_BusinessRegion_ToggleBar.Location = new Point(pos_x, pos_y);
			NewServices_BusinessRegion_ToggleBar.BackColor = tmpBackColor;
			NewServices_BusinessRegion_ToggleBar.sendItemSelected += new ToggleButtonBar.ItemSelectedHandler(NewServices_BusinessRegion_ToggleBar_sendItemSelected);
			newServicesPanel.Controls.Add(NewServices_BusinessRegion_ToggleBar);

			pos_x += NewServices_BusinessRegion_ToggleBar.Width + 10;
			pos_y += 0;
		}

		void CreateLeaderboardPanel ()
		{
			leaderboardPanel = new Panel ();
			leaderboardPanel.BackColor = ContentPanelBackColor;
			mainPanel.Controls.Add(leaderboardPanel);

			leaderboardRoundSelector = new ComboBox ();
			leaderboardRoundSelector.DropDownStyle = ComboBoxStyle.DropDownList;
			leaderboardRoundSelector.SelectedIndexChanged += new EventHandler (leaderboardRoundSelector_SelectedIndexChanged);
			leaderboardPanel.Controls.Add(leaderboardRoundSelector);

			for (int i = 1; i <= SkinningDefs.TheInstance.GetIntData("roundcount", 1); i++)
			{
				leaderboardRoundSelector.Items.Add(CONVERT.Format("Round {0}", i));
			}
		}

		void CreateBubblePanel ()
		{
			bubblePanel = new Panel ();
			bubblePanel.BackColor = ContentPanelBackColor;
			mainPanel.Controls.Add(bubblePanel);

			//=============================================================
			//Build the Round Toggle Bar Data 
			//=============================================================
			ArrayList al = new ArrayList();
			int t_count = 0;
			string first_name = string.Empty;
			for (int i = 1; i <= SkinningDefs.TheInstance.GetIntData("roundcount", 1); i++)
			{
				string name = CONVERT.Format("{0}", i);
				al.Add(new ToggleButtonBarItem(t_count, name, i));
				t_count++;
				if (first_name == string.Empty)
				{
					first_name = name;
				}
			}

			int pos_x = 0;
			int pos_y = 5;
			Color tmpBackColor = Color.FromArgb(142, 136, 150);

			BubbleChart_Round_ToggleBar = new ToggleButtonBar(false);
			BubbleChart_Round_ToggleBar.SetControlAlignment(emToggleButtonBarAlignment.HORIZONTAL);
			BubbleChart_Round_ToggleBar.SetLabel("Round", new Point(2, 4), new Size(60, 20), Color.White, tmpBackColor, Font_BusinessAreaTitle);
			BubbleChart_Round_ToggleBar.SetAllowNoneSelected(false);
			BubbleChart_Round_ToggleBar.SetOptions(al,32,25,4,4,"images/buttons/button_65x25_on.png", "images/buttons/button_65x25_active.png",
				"images/buttons/button_65x25_disabled.png", "images/buttons/button_65x25_hover.png", Color.White, first_name);
			BubbleChart_Round_ToggleBar.Size = new Size(350, 30);
			BubbleChart_Round_ToggleBar.Location = new Point(pos_x, pos_y);
			BubbleChart_Round_ToggleBar.BackColor = tmpBackColor;
			BubbleChart_Round_ToggleBar.sendItemSelected += new ToggleButtonBar.ItemSelectedHandler(BubbleChart_Round_ToggleBar_sendItemSelected);
			bubblePanel.Controls.Add(BubbleChart_Round_ToggleBar);
			BubbleChart_Round_ToggleBar.BringToFront();

			pos_x += NewServices_Round_ToggleBar.Width + 10;
			pos_y += 0;
			//=============================================================
			//Build the Business Region Bar Data  
			//=============================================================
			ArrayList al2 = new ArrayList();
			int t2_count = 0;
			string first_name2 = string.Empty;

			List<Node> businesses2 = new List<Node>((Node[])gameFile.NetworkModel.GetNodesWithAttributeValue("type", "business").ToArray(typeof(Node)));
			businesses2.Sort(delegate(Node a, Node b) { return a.GetAttribute("order").CompareTo(b.GetAttribute("order")); });
			foreach (Node business in businesses2)
			{
				string name = business.GetAttribute("desc");
				string node_name = business.GetAttribute("name");
				al2.Add(new ToggleButtonBarItem(t2_count, name, node_name));
				t2_count++;
				if (first_name2 == string.Empty)
				{
					first_name2 = name;
				}
			}

			BubbleChart_BusinessRegion_ToggleBar = new ToggleButtonBar(false);
			BubbleChart_BusinessRegion_ToggleBar.SetControlAlignment(emToggleButtonBarAlignment.HORIZONTAL);
			BubbleChart_BusinessRegion_ToggleBar.SetLabel("Exchange", new Point(2, 4), new Size(80, 20), Color.White, tmpBackColor, Font_BusinessAreaTitle);
			BubbleChart_BusinessRegion_ToggleBar.SetAllowNoneSelected(false);
			BubbleChart_BusinessRegion_ToggleBar.SetOptions(al2, 65, 25, 4, 4, "images/buttons/button_65x25_on.png", "images/buttons/button_65x25_active.png",
				"images/buttons/button_65x25_disabled.png", "images/buttons/button_65x25_hover.png", Color.White, first_name);
			BubbleChart_BusinessRegion_ToggleBar.Size = new Size(370, 30);
			BubbleChart_BusinessRegion_ToggleBar.Location = new Point(pos_x, pos_y);
			BubbleChart_BusinessRegion_ToggleBar.BackColor = tmpBackColor;
			BubbleChart_BusinessRegion_ToggleBar.sendItemSelected += new ToggleButtonBar.ItemSelectedHandler(BubbleChart_BusinessRegion_ToggleBar_sendItemSelected);
			bubblePanel.Controls.Add(BubbleChart_BusinessRegion_ToggleBar);
			BubbleChart_BusinessRegion_ToggleBar.BringToFront();

			pos_x += BubbleChart_BusinessRegion_ToggleBar.Width + 10;
			pos_y += 0;
		}

		void CreateServiceCatalogPanel ()
		{
			serviceCatalogPanel = new Panel ();
			serviceCatalogPanel.BackColor = ContentPanelBackColor;
			mainPanel.Controls.Add(serviceCatalogPanel);

			serviceCatalogRoundSelector = new ComboBox ();
			serviceCatalogRoundSelector.DropDownStyle = ComboBoxStyle.DropDownList;
			serviceCatalogRoundSelector.SelectedIndexChanged += new EventHandler (serviceCatalogRoundSelector_SelectedIndexChanged);
			serviceCatalogPanel.Controls.Add(serviceCatalogRoundSelector);

			for (int i = 1; i <= SkinningDefs.TheInstance.GetIntData("roundcount", 1); i++)
			{
				serviceCatalogRoundSelector.Items.Add(CONVERT.Format("Round {0}", i));
			}
		}

		protected virtual void tabBar_TabPressed (object sender, TabBarEventArgs args)
		{
			ShowPanel(args.Code);
		}

		protected void ShowPanel (int panel)
		{
			using (WaitCursor cursor = new WaitCursor(this))
			{
				HidePanels();

				switch (panel)
				{
					case 0:
						ShowGameScreenPanel();
						break;

					case 1:
						ShowBusinessScoreCardPanel();
						break;

					case 2:
						ShowOperationsScoreCardPanel();
						break;

					case 3:
						ShowCpuUsagePanel();
						break;

					case 4:
						ShowNewServicesPanel();
						break;

					case 5:
						ShowOpexPanel();
						break;

					case 6:
						ShowLeaderboardPanel();
						break;

					case 9:
						ShowServiceCatalogPanel();
						break;

					case 10:
						ShowChargebackPanel();
						break;

					case 11:
						ShowIntentPanel();
						break;

					case 12:
						ShowBubblePanel();
						break;
				}
			}
		}

		protected void HidePanels ()
		{
			if (gameScreenPanel != null)
			{
				gameScreenPanel.Hide();
			}

			if (businessScoreCardPanel != null)
			{
				businessScoreCardPanel.Hide();
			}

			if (operationsScoreCardPanel != null)
			{
				operationsScoreCardPanel.Hide();
			}

			if (cpuUsagePanel != null)
			{
				cpuUsagePanel.Hide();
			}

			if (newServicesPanel != null)
			{
				newServicesPanel.Hide();
			}

			if (opexPanel != null)
			{
				opexPanel.Hide();
			}

			if (bubblePanel != null)
			{
				bubblePanel.Hide();
			}

			if (leaderboardPanel != null)
			{
				leaderboardPanel.Hide();
			}

			if (serviceCatalogPanel != null)
			{
				serviceCatalogPanel.Hide();
			}

			if (chargebackPanel != null)
			{
				chargebackPanel.Hide();
			}

			if (intentPanel != null)
			{
				intentPanel.Hide();
			}
		}

		public override void Init (ChartPanel screen)
		{
			tabBar.SelectedTabCode = (int) screen;
			ReloadDataAndShow(false);
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);
			DoSize();
		}

		protected void DoSize ()
		{
			tabBar.Size = new Size(Width - 20, 29);

			mainPanel.Location = new Point(10, tabBar.Bottom);
			mainPanel.Size = new Size(Width - (2 * mainPanel.Left), Height - (mainPanel.Top + 9)); 

			gameScreenPanel.Size = mainPanel.Size;
			if (gameScreen != null)
			{
				gameScreen.Location = new Point(0, 0);
				gameScreen.Size = new Size(gameScreenPanel.Width - gameScreen.Left, gameScreenPanel.Height);
			}

			businessScoreCardPanel.Size = mainPanel.Size;
			if (businessScoreCardTable != null)
			{
				businessScoreCardTable.Location = new Point(50, 5);
				businessScoreCardTable.Size = new Size(businessScoreCardPanel.Width - businessScoreCardTable.Left - 40, businessScoreCardTable.TableHeight);
			}

			operationsScoreCardPanel.Size = mainPanel.Size;
			if (operationsScoreCardTable != null)
			{
				operationsScoreCardTable.Location = new Point(50, 5);
				operationsScoreCardTable.Size = new Size(operationsScoreCardPanel.Width - operationsScoreCardTable.Left - 40, operationsScoreCardTable.TableHeight);
			}

			cpuUsagePanel.Size = mainPanel.Size;
			if (cpuUsageGraph != null)
			{
				cpuUsageGraph.Location = new Point(0, CpuUsage_Owner_ToggleBar.Bottom + 10);
				cpuUsageGraph.Size = new Size(cpuUsagePanel.Width - cpuUsageGraph.Left, cpuUsagePanel.Height - cpuUsageGraph.Top);
			}

			newServicesPanel.Size = mainPanel.Size;
			if (newServicesChart != null)
			{
				newServicesChart.Location = new Point(0, NewServices_BusinessRegion_ToggleBar.Bottom + 10);
				newServicesChart.Size = new Size(newServicesPanel.Width - newServicesChart.Left, newServicesPanel.Height - newServicesChart.Top - 15);

				if (newServicesKey != null)
				{
					newServicesKey.Dispose();
				}

				Rectangle keySize = new Rectangle();
				keySize.X = 10;
				keySize.Y = newServicesChart.Bottom - (25 + 2);
				keySize.Width = newServicesChart.Width - 20;
				keySize.Height = 25;

				Color Key_BackColor = Color.FromArgb(20, 16, 28);
				newServicesKey = newServicesChart.CreateLegendPanel(keySize, Key_BackColor, Color.White);
				newServicesPanel.Controls.Add(newServicesKey);
				newServicesKey.BringToFront();
				newServicesKey.Location = keySize.Location;
			}

			opexPanel.Size = mainPanel.Size;
			if (opexTable != null)
			{
				opexTable.Location = new Point(50, 5);
				opexTable.Size = new Size(opexPanel.Width - opexTable.Left - 40, opexTable.TableHeight);
			}

			chargebackPanel.Size = mainPanel.Size;
			if (chargebackTable != null)
			{
				chargebackTable.Location = new Point(30, ChargeBack_Round_ToggleBar.Bottom + 10);
				chargebackTable.Size = new Size(chargebackPanel.Width - chargebackTable.Left - 10, chargebackTable.TableHeight);
			}

			bubblePanel.Size = mainPanel.Size;
			if (bubbleGraph != null)
			{
				bubbleGraph.Location = new Point(0, BubbleChart_Round_ToggleBar.Bottom + 5);
				bubbleGraph.Size = new Size(mainPanel.Width, 650);
			}

			leaderboardPanel.Size = mainPanel.Size;
			serviceCatalogPanel.Size = mainPanel.Size;

			intentPanel.Size = mainPanel.Size;
			if (intentImage != null)
			{
				intentImage.Location = new Point(0, intentToggleBar.Bottom + 10);
				intentImage.Size = new Size(intentPanel.Width - (2 * intentImage.Left), intentPanel.Height - (2 * intentImage.Top));
			}
		}

		protected void GetRoundScores ()
		{
			if (roundScores != null)
			{
				foreach (Cloud_RoundScores scores in roundScores)
				{
					scores.Dispose();
				}
			}

			roundScores = new List<Cloud_RoundScores> ();
			int previousProfit = 0;
			int newServices = 0;
			for (int i = 1; i <= gameFile.LastRoundPlayed; i++)
			{
				Cloud_RoundScores scores = new Cloud_RoundScores (gameFile, i, previousProfit, newServices, spendOverrides);
				roundScores.Add(scores);
				previousProfit = scores.Profit;
				newServices = scores.NumNewServices;
				if (i > 1)
				{
					scores.inner_sections = (Hashtable) (roundScores[i - 2].outer_sections.Clone());
				}
				else
				{
					scores.inner_sections = null;
				}
			}
		}

		public override void ReloadDataAndShow (bool reload)
		{
			if (reload)
			{
				GetRoundScores();
			}

			redrawBusinessScoreCard = true;
			redrawOperationsScoreCard = true;
			redrawCpuUsageGraph = true;
			redrawNewServicesChart = true;
			redrawOpexTable = true;
			redrawLeaderboardTable = true;
			redrawServiceCatalog = true;
			redrawChargeback = true;

			tabBar.SelectedTabCode = (int) 0;
			ShowPanel(tabBar.SelectedTabCode);
		}

		public override IList<ChartScreenTabOption> GetAllAvailableReports ()
		{
			throw new NotImplementedException();
		}

		public override void ShowReport (ChartScreenTabOption report)
		{
			throw new NotImplementedException();
		}

		public void ShowBusinessScoreCardPanel ()
		{
			HidePanels();
			businessScoreCardPanel.Show();

			if (redrawBusinessScoreCard)
			{
				UpdateBusinessScoreCardPanel();
			}
		}

		private void removeOldPanelControls(Panel pnl, Control ctrl)
		{
			if (ctrl != null)
			{
				if (pnl.Controls.Contains(ctrl))
				{
					pnl.Controls.Remove(ctrl);
					ctrl.Dispose();
				}
			}
		}

		protected void ShowBusinessScoreCard ()
		{
			removeOldPanelControls(businessScoreCardPanel, businessScoreCardTable);
			removeOldPanelControls(businessScoreCardPanel, VerticalLabel_BusinessAreaOverall);
			removeOldPanelControls(businessScoreCardPanel, VerticalLabel_BusinessAreaAmerica);
			removeOldPanelControls(businessScoreCardPanel, VerticalLabel_BusinessAreaEurope);
			removeOldPanelControls(businessScoreCardPanel, VerticalLabel_BusinessAreaAfrica);
			removeOldPanelControls(businessScoreCardPanel, VerticalLabel_BusinessAreaAsia);

			VerticalLabel_BusinessAreaOverall = new VerticalLabel();
			VerticalLabel_BusinessAreaOverall.setDrawingBrushColor(Color.White);
			VerticalLabel_BusinessAreaOverall.BackColor = Color.Black;
			VerticalLabel_BusinessAreaOverall.Font = Font_BusinessAreaTitle;
			VerticalLabel_BusinessAreaOverall.Text = "Overall";
			VerticalLabel_BusinessAreaOverall.Location = new Point(2 + 18, 48+3);
			VerticalLabel_BusinessAreaOverall.Size = new Size(30, 115);
			businessScoreCardPanel.Controls.Add(VerticalLabel_BusinessAreaOverall);
			VerticalLabel_BusinessAreaOverall.BringToFront();

			VerticalLabel_BusinessAreaAsia = new VerticalLabel();
			VerticalLabel_BusinessAreaAsia.setDrawingBrushColor(Color.White);
			VerticalLabel_BusinessAreaAsia.BackColor = Color.Black;
			VerticalLabel_BusinessAreaAsia.Font = Font_BusinessAreaTitle;
			VerticalLabel_BusinessAreaAsia.Text = "Asia";
			VerticalLabel_BusinessAreaAsia.Location = new Point(2 + 18, 189 + 115 * 0);
			VerticalLabel_BusinessAreaAsia.Size = new Size(30, 92);
			businessScoreCardPanel.Controls.Add(VerticalLabel_BusinessAreaAsia);
			VerticalLabel_BusinessAreaAsia.BringToFront();

			VerticalLabel_BusinessAreaAfrica = new VerticalLabel();
			VerticalLabel_BusinessAreaAfrica.setDrawingBrushColor(Color.White);
			VerticalLabel_BusinessAreaAfrica.BackColor = Color.Black;
			VerticalLabel_BusinessAreaAfrica.Font = Font_BusinessAreaTitle;
			VerticalLabel_BusinessAreaAfrica.Text = "Africa";
			VerticalLabel_BusinessAreaAfrica.Location = new Point(2 + 18, 189 + 115 * 1);
			VerticalLabel_BusinessAreaAfrica.Size = new Size(30, 92);
			businessScoreCardPanel.Controls.Add(VerticalLabel_BusinessAreaAfrica);
			VerticalLabel_BusinessAreaAfrica.BringToFront();

			VerticalLabel_BusinessAreaEurope = new VerticalLabel();
			VerticalLabel_BusinessAreaEurope.setDrawingBrushColor(Color.White);
			VerticalLabel_BusinessAreaEurope.BackColor = Color.Black;
			VerticalLabel_BusinessAreaEurope.Font = Font_BusinessAreaTitle;
			VerticalLabel_BusinessAreaEurope.Text = "Europe";
			VerticalLabel_BusinessAreaEurope.Location = new Point(2 + 18, 189 + 115 * 2);
			VerticalLabel_BusinessAreaEurope.Size = new Size(30, 92);
			businessScoreCardPanel.Controls.Add(VerticalLabel_BusinessAreaEurope);
			VerticalLabel_BusinessAreaEurope.BringToFront();

			VerticalLabel_BusinessAreaAmerica = new VerticalLabel();
			VerticalLabel_BusinessAreaAmerica.setDrawingBrushColor(Color.White);
			VerticalLabel_BusinessAreaAmerica.BackColor = Color.Black;
			VerticalLabel_BusinessAreaAmerica.Font = Font_BusinessAreaTitle;
			VerticalLabel_BusinessAreaAmerica.Text = "America";
			VerticalLabel_BusinessAreaAmerica.Location = new Point(2 + 18, 189 + 115 * 3);
			VerticalLabel_BusinessAreaAmerica.Size = new Size(30, 92);
			businessScoreCardPanel.Controls.Add(VerticalLabel_BusinessAreaAmerica);
			VerticalLabel_BusinessAreaAmerica.BringToFront();

			bool isData = true;
			foreach (Cloud_RoundScores rdata in roundScores)
			{
				if (rdata != null)
				{
					if (rdata.extract_failure == true)
					{
						isData = false;
					}
				}
			}

			if (isData)
			{
				using (WaitCursor cursor = new WaitCursor (this))
				{
					//Build the Text report 
					BusinessScoreCardReport report = new BusinessScoreCardReport(gameFile, roundScores.ToArray());
					string reportFile = report.BuildReport();

					businessScoreCardTable = new Table();
					businessScoreCardTable.SetBackImage(Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\reports\\general.png"), true);
					businessScoreCardTable.LoadData(File.ReadAllText(reportFile));

					businessScoreCardPanel.Controls.Add(businessScoreCardTable);
					businessScoreCardTable.BringToFront();

					businessScoreCardPanel.AutoScroll = true;

					redrawBusinessScoreCard = false;

					DoSize();
				}
			}
		}

		void UpdateBusinessScoreCardPanel ()
		{
			ShowBusinessScoreCard();
		}

		public void ShowOperationsScoreCardPanel ()
		{
			HidePanels();
			operationsScoreCardPanel.Show();

			if (redrawOperationsScoreCard)
			{
				UpdateOperationsScoreCardPanel();
			}
		}

		protected void ShowOperationsScoreCard ()
		{
			removeOldPanelControls(operationsScoreCardPanel, operationsScoreCardTable);

			removeOldPanelControls(operationsScoreCardPanel, VerticalLabel_OperationsAreaAmerica);
			removeOldPanelControls(operationsScoreCardPanel, VerticalLabel_OperationsAreaAfrica);
			removeOldPanelControls(operationsScoreCardPanel, VerticalLabel_OperationsAreaAsia);
			removeOldPanelControls(operationsScoreCardPanel, VerticalLabel_OperationsAreaEurope);

			VerticalLabel_OperationsAreaAsia = new VerticalLabel();
			VerticalLabel_OperationsAreaAsia.setDrawingBrushColor(Color.White);
			VerticalLabel_OperationsAreaAsia.BackColor = Color.Black;
			VerticalLabel_OperationsAreaAsia.Font = Font_BusinessAreaTitle;
			VerticalLabel_OperationsAreaAsia.Text = "Asia";
			VerticalLabel_OperationsAreaAsia.Location = new Point(2 + 18, 44 + (160 * 0));
			VerticalLabel_OperationsAreaAsia.Size = new Size(30, 140);
			operationsScoreCardPanel.Controls.Add(VerticalLabel_OperationsAreaAsia);
			VerticalLabel_OperationsAreaAsia.BringToFront();

			VerticalLabel_OperationsAreaAfrica = new VerticalLabel();
			VerticalLabel_OperationsAreaAfrica.setDrawingBrushColor(Color.White);
			VerticalLabel_OperationsAreaAfrica.BackColor = Color.Black;
			VerticalLabel_OperationsAreaAfrica.Font = Font_BusinessAreaTitle;
			VerticalLabel_OperationsAreaAfrica.Text = "Africa";
			VerticalLabel_OperationsAreaAfrica.Location = new Point(2 + 18, 44 + (160 * 1));
			VerticalLabel_OperationsAreaAfrica.Size = new Size(30, 140);
			operationsScoreCardPanel.Controls.Add(VerticalLabel_OperationsAreaAfrica);
			VerticalLabel_OperationsAreaAfrica.BringToFront();

			VerticalLabel_OperationsAreaEurope = new VerticalLabel();
			VerticalLabel_OperationsAreaEurope.setDrawingBrushColor(Color.White);
			VerticalLabel_OperationsAreaEurope.BackColor = Color.Black;
			VerticalLabel_OperationsAreaEurope.Font = Font_BusinessAreaTitle;
			VerticalLabel_OperationsAreaEurope.Text = "Europe";
			VerticalLabel_OperationsAreaEurope.Location = new Point(2 + 18, 44 + (160 * 2));
			VerticalLabel_OperationsAreaEurope.Size = new Size(30, 140);
			operationsScoreCardPanel.Controls.Add(VerticalLabel_OperationsAreaEurope);
			VerticalLabel_OperationsAreaEurope.BringToFront();

			VerticalLabel_OperationsAreaAmerica = new VerticalLabel();
			VerticalLabel_OperationsAreaAmerica.setDrawingBrushColor(Color.White);
			VerticalLabel_OperationsAreaAmerica.BackColor = Color.Black;
			VerticalLabel_OperationsAreaAmerica.Font = Font_BusinessAreaTitle;
			VerticalLabel_OperationsAreaAmerica.Text = "America";
			VerticalLabel_OperationsAreaAmerica.Location = new Point(2 + 18, 44 + (160 * 3));
			VerticalLabel_OperationsAreaAmerica.Size = new Size(30, 140);
			operationsScoreCardPanel.Controls.Add(VerticalLabel_OperationsAreaAmerica);
			VerticalLabel_OperationsAreaAmerica.BringToFront();

			bool isData = true;
			foreach (Cloud_RoundScores rdata in roundScores)
			{
				if (rdata != null)
				{
					if (rdata.extract_failure == true)
					{
						isData = false;
					}
				}
			}

			if (isData)
			{
				using (WaitCursor cursor = new WaitCursor (this))
				{
					OperationsScoreCardReport report = new OperationsScoreCardReport(gameFile, roundScores.ToArray());
					string reportFile = report.BuildReport();

					operationsScoreCardTable = new Table();
					operationsScoreCardTable.LoadData(File.ReadAllText(reportFile));

					operationsScoreCardPanel.Controls.Add(operationsScoreCardTable);
					operationsScoreCardTable.BringToFront();

					operationsScoreCardPanel.AutoScroll = true;

					redrawOperationsScoreCard = false;

					DoSize();
				}
			}
		}

		void UpdateOperationsScoreCardPanel ()
		{
			ShowOperationsScoreCard();
		}

		public void ShowOpexPanel ()
		{
			HidePanels();
			opexPanel.Show();

			if (redrawOpexTable)
			{
				UpdateOpexPanel();
			}
		}

		protected void ShowOpex ()
		{
			removeOldPanelControls(opexPanel, opexTable);
			removeOldPanelControls(opexPanel, VerticalLabel_OpexAreaAmerica);
			removeOldPanelControls(opexPanel, VerticalLabel_OpexAreaAfrica);
			removeOldPanelControls(opexPanel, VerticalLabel_OpexAreaAsia);
			removeOldPanelControls(opexPanel, VerticalLabel_OpexAreaEurope);

			VerticalLabel_OpexAreaAsia = new VerticalLabel();
			VerticalLabel_OpexAreaAsia.setDrawingBrushColor(Color.White);
			VerticalLabel_OpexAreaAsia.BackColor = Color.Black;
			VerticalLabel_OpexAreaAsia.Font = Font_BusinessAreaTitle;
			VerticalLabel_OpexAreaAsia.Text = "Asia";
			VerticalLabel_OpexAreaAsia.Location = new Point(2 + 18, 45 + 160 * 0);
			VerticalLabel_OpexAreaAsia.Size = new Size(30, 140);
			opexPanel.Controls.Add(VerticalLabel_OpexAreaAsia);
			VerticalLabel_OpexAreaAsia.BringToFront();

			VerticalLabel_OpexAreaAfrica = new VerticalLabel();
			VerticalLabel_OpexAreaAfrica.setDrawingBrushColor(Color.White);
			VerticalLabel_OpexAreaAfrica.BackColor = Color.Black;
			VerticalLabel_OpexAreaAfrica.Font = Font_BusinessAreaTitle;
			VerticalLabel_OpexAreaAfrica.Text = "Africa";
			VerticalLabel_OpexAreaAfrica.Location = new Point(2 + 18, 45 + 160 * 1);
			VerticalLabel_OpexAreaAfrica.Size = new Size(30, 140);
			opexPanel.Controls.Add(VerticalLabel_OpexAreaAfrica);
			VerticalLabel_OpexAreaAfrica.BringToFront();

			VerticalLabel_OpexAreaEurope = new VerticalLabel();
			VerticalLabel_OpexAreaEurope.setDrawingBrushColor(Color.White);
			VerticalLabel_OpexAreaEurope.BackColor = Color.Black;
			VerticalLabel_OpexAreaEurope.Font = Font_BusinessAreaTitle;
			VerticalLabel_OpexAreaEurope.Text = "Europe";
			VerticalLabel_OpexAreaEurope.Location = new Point(2 + 18, 45 + 160 * 2);
			VerticalLabel_OpexAreaEurope.Size = new Size(30, 140);
			opexPanel.Controls.Add(VerticalLabel_OpexAreaEurope);
			VerticalLabel_OpexAreaEurope.BringToFront();

			VerticalLabel_OpexAreaAmerica = new VerticalLabel();
			VerticalLabel_OpexAreaAmerica.setDrawingBrushColor(Color.White);
			VerticalLabel_OpexAreaAmerica.BackColor = Color.Black;
			VerticalLabel_OpexAreaAmerica.Font = Font_BusinessAreaTitle;
			VerticalLabel_OpexAreaAmerica.Text = "America";
			VerticalLabel_OpexAreaAmerica.Location = new Point(2 + 18, 45 + 160 * 3);
			VerticalLabel_OpexAreaAmerica.Size = new Size(30, 140);
			opexPanel.Controls.Add(VerticalLabel_OpexAreaAmerica);
			VerticalLabel_OpexAreaAmerica.BringToFront();

			bool isData = true;
			foreach (Cloud_RoundScores rdata in roundScores)
			{
				if (rdata != null)
				{
					if (rdata.extract_failure == true)
					{
						isData = false;
					}
				}
			}

			if (isData)
			{
				using (WaitCursor cursor = new WaitCursor(this))
				{
					OpExReport report = new OpExReport(gameFile, roundScores.ToArray());
					string reportFile = report.BuildReport();

					opexTable = new Table();
					opexTable.LoadData(File.ReadAllText(reportFile));
					opexPanel.Controls.Add(opexTable);
					opexTable.BringToFront();

					opexPanel.AutoScroll = true;

					redrawOpexTable = false;

					DoSize();
				}
			}
		}

		void UpdateOpexPanel ()
		{
			ShowOpex();
		}

		class DropDownItem<T>
		{
			string description;
			T value;

			public T Value
			{
				get
				{
					return value;
				}
			}

			public DropDownItem (string description, T value)
			{
				this.description = description;
				this.value = value;
			}

			public override string ToString ()
			{
				return description;
			}
		}

		public void ShowCpuUsagePanel ()
		{
			HidePanels();
			if (redrawCpuUsageGraph)
			{
				suppressUpdates = true;
				CpuUsage_Round_ToggleBar.SelectedIndex = Math.Max(0, gameFile.LastRoundPlayed - 1);
				CpuUsage_DataCenter_ToggleBar.SelectedIndex = 0;
				CpuUsage_Owner_ToggleBar.SelectedIndex = 2;
				suppressUpdates = false;
				ShowCpuUsage();
				cpuUsagePanel.Show();
			}
			else
			{
				cpuUsagePanel.Show();
			}
		}

		protected void ShowCpuUsage ()
		{
			removeOldPanelControls(cpuUsagePanel, cpuUsageGraph);

			int round = 1 + CpuUsage_Round_ToggleBar.SelectedIndex;

			if (round > gameFile.LastRoundPlayed)
			{
				return;
			}
			
			CpuUsageReport report;
			bool disposeReport = false;
			if (CpuUsage_Round_ToggleBar.SelectedIndex < roundScores.Count)
			{
				report = roundScores[round - 1].CpuUsageReport;
			}
			else
			{
				report = new CpuUsageReport (gameFile, round, false);
				disposeReport = true;
			}

			List<string> datacenters = null;
			datacenters = (List<string>) CpuUsage_DataCenter_ToggleBar.SelectedObject;

			List<string> owners = null;
			object selected_Owner = CpuUsage_Owner_ToggleBar.SelectedObject;
			if (selected_Owner != null)
			{
				owners = new List<string>();
				owners.AddRange(((string[])CpuUsage_Owner_ToggleBar.SelectedObject));
			}

			bool isData = true;
			foreach (Cloud_RoundScores rdata in roundScores)
			{
				if (rdata != null)
				{
					if (rdata.extract_failure == true)
					{
						isData = false;
					}
				}
			}

			if (isData)
			{
				using (WaitCursor cursor = new WaitCursor(this))
				{
					string reportFile = report.BuildReport(datacenters, owners);

					cpuUsageGraph = new StackBarChartWithBackground(BasicXmlDocument.CreateFromFile(reportFile).DocumentElement);
					cpuUsageGraph.setShowMouseNotes(false);

					cpuUsagePanel.Controls.Add(cpuUsageGraph);

					cpuUsagePanel.BackColor = Color.FromArgb(142, 136, 150);
					cpuUsageGraph.BackColor = Color.FromArgb(142, 136, 150);
					cpuUsageGraph.YAxisWidth = 50;
					cpuUsageGraph.XAxisHeight = 50;
					cpuUsageGraph.LeftMargin = 0;
					cpuUsageGraph.TopMargin = 50;
					cpuUsageGraph.RightMargin = 200;
					cpuUsageGraph.BottomMargin = 8;
					cpuUsageGraph.LegendX = 100;
					cpuUsageGraph.LegendY = 0;
					cpuUsageGraph.YAxisLegendMargin = 20;
					cpuUsageGraph.BringToFront();

					redrawCpuUsageGraph = false;

					DoSize();
				}
			}

			if (disposeReport)
			{
				report.Dispose();
			}
		}

		void UpdateCpuUsagePanel ()
		{
			ShowCpuUsage();
		}

		void cpuUsageRoundSelector_SelectedIndexChanged (object sender, EventArgs e)
		{
			if (! suppressUpdates)
			{
				UpdateCpuUsagePanel();
			}
		}

		void cpuUsageDatacenterSelector_SelectedIndexChanged (object sender, EventArgs e)
		{
			if (! suppressUpdates)
			{
				UpdateCpuUsagePanel();
			}
		}

		void cpuUsageOwnerSelector_SelectedIndexChanged (object sender, EventArgs e)
		{
			if (! suppressUpdates)
			{
				UpdateCpuUsagePanel();
			}
		}

		void ShowNewServicesPanel ()
		{
			HidePanels();
			if (redrawNewServicesChart)
			{
				PreventScreenUpdate = true;
				NewServices_BusinessRegion_ToggleBar.SelectedIndex = 0;
				NewServices_Round_ToggleBar.SelectedIndex = Math.Max(0, gameFile.LastRoundPlayed - 1);
				PreventScreenUpdate = false;
				ShowNewServices();
				newServicesPanel.Show();
			}
			else
			{
				newServicesPanel.Show();
			}
		}

		protected void ShowNewServices ()
		{
			removeOldPanelControls(newServicesPanel, newServicesChart);
			removeOldPanelControls(newServicesPanel, newServicesKey);

			int xpos = 10;
			NewServices_Round_ToggleBar.Location = new Point(xpos, 5);
			xpos += NewServices_Round_ToggleBar.Width + 10;
			NewServices_BusinessRegion_ToggleBar.Location = new Point(xpos, 5);
			xpos += NewServices_BusinessRegion_ToggleBar.Width + 30;

			if ((1 + NewServices_Round_ToggleBar.SelectedIndex) > gameFile.LastRoundPlayed)
			{
				return;
			}

			bool isData = true;
			foreach (Cloud_RoundScores rdata in roundScores)
			{
				if (rdata != null)
				{
					if (rdata.extract_failure == true)
					{
						isData = false;
					}
				}
			}

			if (isData)
			{
				using (WaitCursor cursor = new WaitCursor(this))
				{
					NewServicesReport report = new NewServicesReport(gameFile, 1 + NewServices_Round_ToggleBar.SelectedIndex);

					string business = null;
					business = (string) NewServices_BusinessRegion_ToggleBar.SelectedObject;

					string reportFile = report.BuildReport(business, true, true);

					newServicesChart = new CloudTimeChart(BasicXmlDocument.CreateFromFile(reportFile).DocumentElement);
					newServicesChart.SetBackImageWithBitmap(null, false, Color.White, Color.Black);
					newServicesPanel.Controls.Add(newServicesChart);
					newServicesChart.BringToFront();

					redrawNewServicesChart = false;

					DoSize();
				}
			}
		}

		void UpdateNewServicesPanel ()
		{
			ShowNewServices();
		}

		void newServicesRoundSelector_SelectedIndexChanged (object sender, EventArgs e)
		{
			UpdateNewServicesPanel();
		}

		void newServicesBusinessSelector_SelectedIndexChanged (object sender, EventArgs e)
		{
			UpdateNewServicesPanel();
		}
		
		public void ShowGameScreenPanel ()
		{
			HidePanels();
			gameScreenPanel.Show();

			ShowGameScreen();
		}

		protected void ShowGameScreen ()
		{
			if (gameScreen != null)
			{
				gameScreenPanel.Controls.Remove(gameScreen);
			}

			gameScreen = gameScreenCreator(gameFile);
			gameScreenPanel.Controls.Add(gameScreen);

			DoSize();
		}

		public void ShowLeaderboardPanel ()
		{
			HidePanels();
			leaderboardPanel.Show();

			if (redrawLeaderboardTable)
			{
				leaderboardRoundSelector.SelectedIndex = Math.Max(0, gameFile.LastRoundPlayed - 1);

				ShowLeaderboard();
			}
		}

		protected void ShowLeaderboard ()
		{
			if (leaderboardTable != null)
			{
				leaderboardPanel.Controls.Remove(leaderboardTable);
			}

			if ((1 + leaderboardRoundSelector.SelectedIndex) > gameFile.LastRoundPlayed)
			{
				return;
			}

//			LeaderboardReport report = new LeaderboardReport (gameFile, 1 + leaderboardRoundSelector.SelectedIndex);
//			string reportFile = report.BuildReport();

			leaderboardTable = new Table ();
//			leaderboardTable.LoadData(BasicXmlDocument.CreateFromFile(reportFile).OuterXml);
			leaderboardTable.LoadData("<table columns=\"1\" rowheight=\"50\"><rowdata><cell val=\"Hello\" /></rowdata></table>");
			leaderboardPanel.Controls.Add(leaderboardTable);

			leaderboardRoundSelector.Location = new Point (0, 5);

			leaderboardTable.Location = new Point (0, leaderboardRoundSelector.Bottom + 10);
			leaderboardTable.Size = new Size (leaderboardPanel.Width - leaderboardTable.Left, leaderboardTable.TableHeight);

			redrawLeaderboardTable = false;
		}

		void UpdateLeaderboardPanel ()
		{
			ShowLeaderboard();
		}

		void leaderboardRoundSelector_SelectedIndexChanged (object sender, EventArgs e)
		{
			UpdateLeaderboardPanel();
		}

		public void ShowBubblePanel ()
		{
			HidePanels();
			
			PreventScreenUpdate = true;
			BubbleChart_Round_ToggleBar.SelectedIndex = gameFile.LastRoundPlayed - 1;
			BubbleChart_BusinessRegion_ToggleBar.SelectedIndex = 0;
			PreventScreenUpdate = false;
			ShowBubble();
			bubblePanel.Show();
		}

		protected void ShowBubble ()
		{
			removeOldPanelControls(bubblePanel, bubbleGraph);

			int bubble_round_index = BubbleChart_Round_ToggleBar.SelectedIndex;
			int bubble_region_index = BubbleChart_BusinessRegion_ToggleBar.SelectedIndex;
			int lookup_index = (4 * (bubble_round_index)) + (bubble_region_index);

			string image_file_name = "";
			switch (lookup_index)
			{
				case 0:
					image_file_name = @"\images\bubble\Bubble_Charts_Asia_r1.png";
					break;
				case 1:
					image_file_name = @"\images\bubble\Bubble_Charts_Africa_r1.png";
					break;
				case 2:
					image_file_name = @"\images\bubble\Bubble_Charts_Europe_r1.png";
					break;
				case 3:
					image_file_name = @"\images\bubble\Bubble_Charts_America_r1.png";
					break;
				case 4:
					image_file_name = @"\images\bubble\Bubble_Charts_Asia_r2.png";
					break;
				case 5:
					image_file_name = @"\images\bubble\Bubble_Charts_Africa_r2.png";
					break;
				case 6:
					image_file_name = @"\images\bubble\Bubble_Charts_Europe_r2.png";
					break;
				case 7:
					image_file_name = @"\images\bubble\Bubble_Charts_America_r2.png";
					break;
				case 8:
					image_file_name = @"\images\bubble\Bubble_Charts_Asia_r3.png";
					break;
				case 9:
					image_file_name = @"\images\bubble\Bubble_Charts_Africa_r3.png";
					break;
				case 10:
					image_file_name = @"\images\bubble\Bubble_Charts_Europe_r3.png";
					break;
				case 11:
					image_file_name = @"\images\bubble\Bubble_Charts_America_r3.png";
					break;
				case 12:
					image_file_name = @"\images\bubble\Bubble_Charts_Asia_r4.png";
					break;
				case 13:
					image_file_name = @"\images\bubble\Bubble_Charts_Africa_r4.png";
					break;
				case 14:
					image_file_name = @"\images\bubble\Bubble_Charts_Europe_r4.png";
					break;
				case 15:
					image_file_name = @"\images\bubble\Bubble_Charts_America_r4.png";
					break;
			}

			if (string.IsNullOrEmpty(image_file_name) == false)
			{
				string full_bubble_path = (CONVERT.Format("{0}{1}", AppInfo.TheInstance.Location, image_file_name));
				Bitmap bubble_bmp = (Bitmap)Repository.TheInstance.GetImage(full_bubble_path);

				bubbleGraph = new IntentImagePanel(bubble_bmp, bubble_bmp);
				bubblePanel.Controls.Add(bubbleGraph);
				bubbleGraph.BringToFront();
			}
			else
			{
				bubbleGraph = null;
			}

			DoSize();
		}

		void UpdateBubblePanel ()
		{
			ShowBubble();
		}

		public void ShowServiceCatalogPanel ()
		{
			HidePanels();
			serviceCatalogPanel.Show();

			if (redrawServiceCatalog)
			{
				serviceCatalogRoundSelector.SelectedIndex = Math.Max(0, gameFile.LastRoundPlayed - 1);

				ShowServiceCatalog();
			}
		}

		protected void ShowServiceCatalog ()
		{
			if (serviceCatalogTable != null)
			{
				serviceCatalogPanel.Controls.Remove(serviceCatalogTable);
			}

			if ((1 + serviceCatalogRoundSelector.SelectedIndex) > gameFile.LastRoundPlayed)
			{
				return;
			}

//			ServiceCatalogReport report = new ServiceCatalogReport (gameFile, 1 + serviceCatalogRoundSelector.SelectedIndex);
//			string reportFile = report.BuildReport();

			serviceCatalogTable = new Table ();
//			serviceCatalogTable.LoadData(BasicXmlDocument.CreateFromFile(reportFile).OuterXml);
			serviceCatalogTable.LoadData("<table columns=\"1\" rowheight=\"50\"><rowdata><cell val=\"We dont need no steenkin serveece catalog\" /></rowdata></table>");
			serviceCatalogPanel.Controls.Add(serviceCatalogTable);

			serviceCatalogRoundSelector.Location = new Point (0, 5);

			serviceCatalogTable.Location = new Point (0, serviceCatalogRoundSelector.Bottom + 10);
			serviceCatalogTable.Size = new Size (serviceCatalogPanel.Width - serviceCatalogTable.Left, serviceCatalogTable.TableHeight);

			redrawServiceCatalog = false;
		}

		void UpdateServiceCatalogPanel ()
		{
			ShowServiceCatalog();
		}

		void serviceCatalogRoundSelector_SelectedIndexChanged (object sender, EventArgs e)
		{
			UpdateServiceCatalogPanel();
		}

		void ShowIntentPanel ()
		{
			HidePanels();
			PreventScreenUpdate = true;
			intentToggleBar.SelectedIndex = Math.Max(0, gameFile.LastRoundPlayed - 1);
			PreventScreenUpdate = false;
			ShowIntent();
			intentPanel.Show();
		}

		protected void ShowIntent ()
		{
			removeOldPanelControls(intentPanel, intentImage);
			
			int selected_item = ((int)intentToggleBar.SelectedIndex)+1;

			string issues_name = "\\images\\intent\\Intent_Issues_R" + CONVERT.ToStr(selected_item) + ".png";
			string actions_name = "\\images\\intent\\Intent_Actions_R" + CONVERT.ToStr(selected_item) + ".png";
			Bitmap issues_bmp = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + issues_name);
			Bitmap actions_bmp = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + actions_name);

			intentImage = new IntentImagePanel(issues_bmp, actions_bmp);
			intentPanel.Controls.Add(intentImage);
			intentImage.BringToFront();

			intentToggleBar.Location = new Point(0, 5);
			intentToggleBar.BringToFront();

			DoSize();
		}

		void UpdateIntentPanel ()
		{
			ShowIntent();
		}

		void intentToggleBar_sendItemSelected(object sender, string item_name_selected, object selected_object)
		{
			if (PreventScreenUpdate == false)
			{
				UpdateIntentPanel();
			}
		}

		public void ShowChargebackPanel ()
		{
			HidePanels();
			if (redrawChargeback)
			{
				PreventScreenUpdate = true;

				ChargeBack_Round_ToggleBar.SelectedIndex =  Math.Min(1,Math.Max(0, (gameFile.CurrentRound) - 3));
				ChargeBack_Type_ToggleBar.SelectedIndex = 0;
				PreventScreenUpdate = false;
				ShowChargeback();
				chargebackPanel.Show();
			}
			else
			{
				chargebackPanel.Show();
			}
		}

		protected void ShowChargeback ()
		{
			if (chargebackTable != null)
			{
				chargebackPanel.Controls.Remove(chargebackTable);
			}
			removeOldPanelControls(chargebackPanel, chargebackTable);
			removeOldPanelControls(chargebackPanel, VerticalLabel_chargeBackAreaAmerica);
			removeOldPanelControls(chargebackPanel, VerticalLabel_chargeBackAreaAfrica);
			removeOldPanelControls(chargebackPanel, VerticalLabel_chargeBackAreaAsia);
			removeOldPanelControls(chargebackPanel, VerticalLabel_chargeBackAreaEurope);

			int round = (ChargeBack_Round_ToggleBar.SelectedIndex+3);

			VerticalLabel_chargeBackAreaAmerica = new VerticalLabel();
			VerticalLabel_chargeBackAreaAmerica.setDrawingBrushColor(Color.White);
			VerticalLabel_chargeBackAreaAmerica.BackColor = Color.Black;
			VerticalLabel_chargeBackAreaAmerica.Font = Font_BusinessAreaTitle;
			VerticalLabel_chargeBackAreaAmerica.Text = "America";
			VerticalLabel_chargeBackAreaAmerica.Location = new Point(5, 79 + ((120 + 16) * (4 - 1)));
			VerticalLabel_chargeBackAreaAmerica.Size = new Size(25, 119);
			chargebackPanel.Controls.Add(VerticalLabel_chargeBackAreaAmerica);
			VerticalLabel_chargeBackAreaAmerica.BringToFront();

			VerticalLabel_chargeBackAreaEurope = new VerticalLabel();
			VerticalLabel_chargeBackAreaEurope.setDrawingBrushColor(Color.White);
			VerticalLabel_chargeBackAreaEurope.BackColor = Color.Black;
			VerticalLabel_chargeBackAreaEurope.Font = Font_BusinessAreaTitle;
			VerticalLabel_chargeBackAreaEurope.Text = "Europe";
			VerticalLabel_chargeBackAreaEurope.Location = new Point(5, 79 + ((120 + 16) * (3 - 1)));
			VerticalLabel_chargeBackAreaEurope.Size = new Size(25, 119);
			chargebackPanel.Controls.Add(VerticalLabel_chargeBackAreaEurope);
			VerticalLabel_chargeBackAreaEurope.BringToFront();

			VerticalLabel_chargeBackAreaAfrica = new VerticalLabel();
			VerticalLabel_chargeBackAreaAfrica.setDrawingBrushColor(Color.White);
			VerticalLabel_chargeBackAreaAfrica.BackColor = Color.Black;
			VerticalLabel_chargeBackAreaAfrica.Font = Font_BusinessAreaTitle;
			VerticalLabel_chargeBackAreaAfrica.Text = "Africa";
			VerticalLabel_chargeBackAreaAfrica.Location = new Point(5, 79 + ((120 + 16) * (2 - 1)));
			VerticalLabel_chargeBackAreaAfrica.Size = new Size(25, 119);
			chargebackPanel.Controls.Add(VerticalLabel_chargeBackAreaAfrica);
			VerticalLabel_chargeBackAreaAfrica.BringToFront();

			VerticalLabel_chargeBackAreaAsia = new VerticalLabel();
			VerticalLabel_chargeBackAreaAsia.setDrawingBrushColor(Color.White);
			VerticalLabel_chargeBackAreaAsia.BackColor = Color.Black;
			VerticalLabel_chargeBackAreaAsia.Font = Font_BusinessAreaTitle;
			VerticalLabel_chargeBackAreaAsia.Text = "Asia";
			VerticalLabel_chargeBackAreaAsia.Location = new Point(5, 79 + ((120 + 16) * (1 - 1)));
			VerticalLabel_chargeBackAreaAsia.Size = new Size(25, 119);
			chargebackPanel.Controls.Add(VerticalLabel_chargeBackAreaAsia);
			VerticalLabel_chargeBackAreaAsia.BringToFront();

			if (round <= (gameFile.LastRoundPlayed + 1))
			{
				using (WaitCursor cursor = new WaitCursor(this))
				{
					ChargebackReport report = new ChargebackReport(gameFile, round, roundScores.ToArray());
					string reportFile = report.BuildReport((int) ChargeBack_Type_ToggleBar.SelectedIndex != 0);

					chargebackTable = new Table();
					chargebackTable.LoadData(BasicXmlDocument.CreateFromFile(reportFile).OuterXml);
					chargebackPanel.Controls.Add(chargebackTable);
					chargebackTable.BringToFront();
				}
			}

			ChargeBack_Round_ToggleBar.Location = new Point(0, 5);

			DoSize();

			redrawChargeback = false;
		}

		void UpdateChargebackPanel ()
		{
			ShowChargeback();
		}

		void ChargeBack_Round_ToggleBar_sendItemSelected(object sender, string item_name_selected, object selected_object)
		{
			if (PreventScreenUpdate == false)
			{
				UpdateChargebackPanel();
			}
		}
		void ChargeBack_Type_ToggleBar_sendItemSelected(object sender, string item_name_selected, object selected_object)
		{
			if (PreventScreenUpdate == false)
			{
				UpdateChargebackPanel();
			}
		}

		void NewServices_Round_ToggleBar_sendItemSelected(object sender, string item_name_selected, object selected_object)
		{
			if (PreventScreenUpdate == false)
			{
				UpdateNewServicesPanel();
			}
		}
		void NewServices_BusinessRegion_ToggleBar_sendItemSelected(object sender, string item_name_selected, object selected_object)
		{
			if (PreventScreenUpdate == false)
			{
				UpdateNewServicesPanel();
			}
		}

		void BubbleChart_Round_ToggleBar_sendItemSelected(object sender, string item_name_selected, object selected_object)
		{
			if (PreventScreenUpdate == false)
			{
				UpdateBubblePanel();
			}
		}

		void BubbleChart_BusinessRegion_ToggleBar_sendItemSelected(object sender, string item_name_selected, object selected_object)
		{
			if (PreventScreenUpdate == false)
			{
				UpdateBubblePanel();
			}
		}

		void CpuUsage_Round_ToggleBar_sendItemSelected(object sender, string item_name_selected, object selected_object)
		{
			if (PreventScreenUpdate == false)
			{
				UpdateCpuUsagePanel();
			}
		}

		void CpuUsage_DataCenter_ToggleBar_sendItemSelected(object sender, string item_name_selected, object selected_object)
		{
			if (PreventScreenUpdate == false)
			{
				UpdateCpuUsagePanel();
			}
		}

		void CpuUsage_Owner_ToggleBar_ToggleBar_sendItemSelected(object sender, string item_name_selected, object selected_object)
		{
			if (PreventScreenUpdate == false)
			{
				UpdateCpuUsagePanel();
			}
		}
	}
}