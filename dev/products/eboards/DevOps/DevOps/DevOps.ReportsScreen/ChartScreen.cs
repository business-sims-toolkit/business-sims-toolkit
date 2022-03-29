using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

using Algorithms;
using Charts;
using ChartScreens;
using CommonGUI;
using CoreUtils;
using DevOpsReportBuilders;
using Events;
using DevOps.ReportsScreen.Interfaces;
using GameManagement;
using LibCore;
using Network;
using ReportBuilder;
using ResizingUi;
// ReSharper disable UnusedVariable

namespace DevOps.ReportsScreen
{
	public class ChartScreen : PureTabbedChartScreen, IRoundScoresUpdater<DevOpsRoundScores>
	{
        Bitmap fullBackgroundImage;
		Bitmap extractedBackgroundImage;

	    readonly NetworkProgressionGameFile gameFile;
	    readonly SupportSpendOverrides spendOverrides;

	    readonly TabBar tabBar;

		List<DevOpsRoundScores> roundScores;

	    readonly Panel mainPanel;

	    readonly Color contentPanelBackColor = SkinningDefs.TheInstance.GetColorData("main_background_colour", Color.White);
		bool preventScreenUpdate;

	    DevOpsHoldingPanel holdingPanel;
        
        

		Panel portfolioBubblePanel;
		ImagePanel bubbleImagePanel;
		ImagePanel bubbleBackground;

		ComboBoxRow portfolioRoundComboBox;
		ComboBoxRow portfolioBusinessComboBox;

	    TableReportPanel businessScorecardReportPanel;

	    TableReportPanel processScorecardReportPanel;

        CloudTimeChartReportPanel newServicesReportPanel;

	    TableReportPanel operationsScorecardReportPanel;

	    CloudTimeChartReportPanel incidentGanttReportPanel;


		Panel cpuReportPanel;
		GroupedBarChart cpuReportChart;
		GroupedBarChart bladeReportChart;
		bool redrawCpuReportChart;

		ComboBoxRow cpuReportRoundComboBox;

	    Panel networkReportPanel;
	    ImagePanel networkReportBack;
	    
	    GroupedBoxChart networkBoxChart;
	    
	    Panel maturityPanel;
        ComboBoxRow maturityRoundComboBox;
	    PieChart maturityChart;
	    bool redrawMaturity;
        

        Panel devErrorReportPanel;
        Panel devErrorReportChart;
        ComboBoxRow devErrorReportRoundComboBox;

		Panel productQualityReportPanel;
		Panel productQualityReportScrollingPanel;
		BusinessServiceHeatMap productQualityReportChart;
		ComboBoxRow productQualityReportRoundComboBox;

		Panel intentPanel;
		IntentImagePanel intentImage;
		ImagePanel newIntentBack;
		ComboBoxRow intentRoundComboBox;

		Panel customReportPanel;
		RevealablePanel customReportImage;
		ComboBoxRow customReportRoundComboBox;

		Panel npsSurveyReportPanel;
	    GroupedBarChart npsReportChart;
	    bool redrawNpsReport;
        
		enum TabOrder
		{
			GameScreen = 1,
			Businesstab,
			Portfolio,
			NewApps,
            DevErrors,
			ProductQuality,
			Operations,
			Incidents,
			CpuReport,
            Network,
            Maturity,
            Process,
            Intent,
			CustomReport,
            Css
		}

		TabOrder currentTab = TabOrder.GameScreen;

		public ChartScreen (NetworkProgressionGameFile gameFile, SupportSpendOverrides spendOverrides)
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.DoubleBuffer, true);

			this.gameFile = gameFile;
			this.spendOverrides = spendOverrides;

			GenerateBackBitmaps();
            
			GetRoundScores();

			mainPanel = new Panel { BackColor = contentPanelBackColor };
			Controls.Add(mainPanel);

			tabBar = new TabBar();
			tabBar.AddTab("Hold",       (int) TabOrder.GameScreen,     true);
			tabBar.AddTab("Business",   (int) TabOrder.Businesstab,    true);
            tabBar.AddTab("Process",    (int) TabOrder.Process,        true);
            tabBar.AddTab("Maturity",   (int) TabOrder.Maturity,       true);
            tabBar.AddTab("Apps",       (int) TabOrder.NewApps,        true);
			tabBar.AddTab("Product",    (int) TabOrder.ProductQuality, true);
            tabBar.AddTab("Operations", (int) TabOrder.Operations,      true);
            tabBar.AddTab("Incidents",  (int) TabOrder.Incidents,      true);
            tabBar.AddTab("CPU",        (int) TabOrder.CpuReport,      true);
		    tabBar.AddTab("Network",    (int) TabOrder.Network,        true);
            tabBar.AddTab("CSAT",       (int) TabOrder.Css,             true);
            tabBar.AddTab("Intent",     (int) TabOrder.Intent,          true);

			if (gameFile.CustomContentSource != CustomContentSource.None)
			{
				tabBar.AddTab("Custom", (int) TabOrder.CustomReport, true);
			}


			tabBar.TabPressed += tabBar_TabPressed;
			Controls.Add(tabBar);

            tabBar.BackColor = Color.White;

			CreateHoldingPanel();
			
            CreateDevErrorPanel();
			CreateProductQualityReportPanel();
			CreateBubblePanel();
			CreateIntentPanel();
			CreateCustomReportPanel();
			CreateCpuReportPanel();
		    CreateNetworkReportPanel();
		    CreateMaturityPanel();
		    CreateNpsSurveyReportPanel();

			GetRoundScores();
		}

	    public event EventHandler<EventArgs<List<DevOpsRoundScores>>> RoundScoresChanged;

        void CreateHoldingPanel ()
		{
		    holdingPanel = new DevOpsHoldingPanel(gameFile);

		    mainPanel.Controls.Add(holdingPanel);
		}
        
		protected void GenerateBackBitmaps ()
		{
			fullBackgroundImage = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location +
																	"\\images\\panels\\popup_1024x688_dark.png");
			extractedBackgroundImage = new Bitmap(fullBackgroundImage.Width, fullBackgroundImage.Height - 40);

			using (var graphics = Graphics.FromImage(extractedBackgroundImage))
			{
				graphics.FillRectangle(Brushes.White, 0, 0, extractedBackgroundImage.Width,
					extractedBackgroundImage.Height);
			}
		}

	    void OnRoundScoresChanged ()
	    {
	        RoundScoresChanged?.Invoke(this, new EventArgs<List<DevOpsRoundScores>>(roundScores));
	    }
        
        void CreateBubblePanel ()
		{
		    portfolioBubblePanel = new Panel { BackColor = Color.Transparent };
			mainPanel.Controls.Add(portfolioBubblePanel);

			// Build the background
			bubbleBackground = BuildBackgroundImagePanel();

			portfolioBubblePanel.Controls.Add(bubbleBackground);
			bubbleBackground.SendToBack();

			portfolioRoundComboBox = DevOpsComboBoxBuilder.CreateRoundComboBox(gameFile);
		    portfolioRoundComboBox.SelectedIndexChanged += comboBox_SelectedIndexChanged;
			portfolioBubblePanel.Controls.Add(portfolioRoundComboBox);
			portfolioRoundComboBox.BringToFront();

            portfolioRoundComboBox.Location = SkinningDefs.TheInstance.GetPointData("round_combo_box_position",
		        new Point(0, 15));

			portfolioBusinessComboBox = DevOpsComboBoxBuilder.CreateBusinessComboBox(gameFile);
		    portfolioBusinessComboBox.SelectedIndexChanged += comboBox_SelectedIndexChanged;
			portfolioBubblePanel.Controls.Add(portfolioBusinessComboBox);
			portfolioBusinessComboBox.BringToFront();

            portfolioBusinessComboBox.Location = new Point(portfolioRoundComboBox.Right + 50, portfolioRoundComboBox.Top);

		}

		
	    void CreateDevErrorPanel ()
	    {
	        devErrorReportPanel = new Panel { BackColor = Color.Transparent };
            mainPanel.Controls.Add(devErrorReportPanel);

            devErrorReportRoundComboBox = DevOpsComboBoxBuilder.CreateRoundComboBox(gameFile);
            devErrorReportRoundComboBox.Location = new Point(0, 15);
            devErrorReportRoundComboBox.SelectedIndexChanged += devErrorReportRoundComboBox_SelectedIndexChanged;

            devErrorReportPanel.Controls.Add(devErrorReportRoundComboBox);
            devErrorReportRoundComboBox.BringToFront();
	    }

        void devErrorReportRoundComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!preventScreenUpdate)
            {
                UpdateDevErrorReportPanel();
            }
        }

	    void CreateIntentPanel ()
		{
		    intentPanel = new Panel { BackColor = Color.Transparent };
			mainPanel.Controls.Add(intentPanel);

			// Build the background
			newIntentBack = BuildBackgroundImagePanel();

			intentPanel.Controls.Add(newIntentBack);
			newIntentBack.SendToBack();

			intentRoundComboBox = DevOpsComboBoxBuilder.CreateRoundComboBox(gameFile, false);
		    intentRoundComboBox.SelectedIndexChanged += comboBox_SelectedIndexChanged;
			intentPanel.Controls.Add(intentRoundComboBox);

		    intentRoundComboBox.Location = SkinningDefs.TheInstance.GetPointData("round_combo_box_position", new Point(0, 15));
		}

		void CreateCustomReportPanel ()
		{
		    customReportPanel = new Panel { BackColor = Color.Transparent };
			mainPanel.Controls.Add(customReportPanel);

			customReportRoundComboBox = DevOpsComboBoxBuilder.CreateRoundComboBox(gameFile);
            customReportPanel.Controls.Add(customReportRoundComboBox);
			customReportRoundComboBox.SelectedIndexChanged += customReportRoundComboBox_SelectedIndexChanged;

			customReportRoundComboBox.Location = SkinningDefs.TheInstance.GetPointData("round_combo_box_position", new Point(0, 15));
		}

		void customReportRoundComboBox_SelectedIndexChanged (object sender, EventArgs e)
		{
			if (! preventScreenUpdate)
			{
				UpdateCustomReportPanel();
			}
		}

		void CreateCpuReportPanel ()
		{
		    cpuReportPanel = new Panel { BackColor = Color.Transparent };
			mainPanel.Controls.Add(cpuReportPanel);
            
            cpuReportRoundComboBox = DevOpsComboBoxBuilder.CreateRoundComboBox(gameFile);
		    cpuReportRoundComboBox.SelectedIndexChanged += comboBox_SelectedIndexChanged;
            cpuReportRoundComboBox.Location = SkinningDefs.TheInstance.GetPointData("round_combo_box_position", new Point(0, 15));
            cpuReportPanel.Controls.Add(cpuReportRoundComboBox);
		}

        void CreateNetworkReportPanel()
        {
            networkReportPanel = new Panel { BackColor = Color.Transparent };
            mainPanel.Controls.Add(networkReportPanel);

            networkReportBack = BuildBackgroundImagePanel();
            networkReportPanel.Controls.Add(networkReportBack);
            networkReportBack.SendToBack();
            
        }

        void CreateMaturityPanel()
        {
            maturityPanel = new Panel { BackColor = Color.Transparent };
            mainPanel.Controls.Add(maturityPanel);

            maturityRoundComboBox = DevOpsComboBoxBuilder.CreateRoundComboBox(gameFile);
            maturityRoundComboBox.SelectedIndexChanged += comboBox_SelectedIndexChanged;
            maturityRoundComboBox.Location = SkinningDefs.TheInstance.GetPointData("round_combo_box_position", new Point(0, 15));

            maturityPanel.Controls.Add(maturityRoundComboBox);
            maturityRoundComboBox.BringToFront();
        }
        
        void CreateNpsSurveyReportPanel()
        {
            npsSurveyReportPanel = new Panel()
                                   {
                                       BackColor = Color.Transparent
                                   };
            mainPanel.Controls.Add(npsSurveyReportPanel);
        }

        void ShowNpsSurveyReportPanel()
        {
            HidePanels();
            npsSurveyReportPanel.Show();

            if (redrawNpsReport)
            {
                UpdateNpsSurveyReport();
            }
        }

        void ShowNpsSurveyReport()
        {
            RemoveOldPanelControls(npsSurveyReportPanel, npsReportChart);

            var report = new NpsSurveyReport(gameFile, "nps_survey_wizard.xml");

            var reportFile = report.BuildReport();
            if (reportFile != "")
            {
                var xml = BasicXmlDocument.CreateFromFile(reportFile);

                var chartHeight = (int)(npsSurveyReportPanel.Height * 0.75f);
                var yOffset = 20;

                npsReportChart = new GroupedBarChart(xml.DocumentElement)
                                 {
                                     Size = new Size(npsSurveyReportPanel.Width, chartHeight),
                                     Location = new Point(0, yOffset),
                                     XAxisHeight = 50,
                                     YAxisWidth = 35,
                                     LegendX = 100,
                                     LegendY = 20,
                                     LegendHeight = 50,
                                     BarPadding = 15,
                                     GroupMargin = 30
                                 };
                npsSurveyReportPanel.Controls.Add(npsReportChart);
                npsReportChart.BringToFront();
                

                npsReportChart.Invalidate();

                redrawNpsReport = false;
            }
        }

        void UpdateNpsSurveyReport()
        {
            ShowNpsSurveyReport();
        }

        protected ImagePanel BuildBackgroundImagePanel ()
		{
		    var backPanel = new ImagePanel(Color.Transparent, mainPanel.Width, mainPanel.Height)
		    {
		        Location = new Point(0, 0),
		        Size = new Size(mainPanel.Width, 685)
		    };
			// Where does this 685 value come from?? (GC) TODO
			return backPanel;
		}

        void comboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!preventScreenUpdate)
            {
                switch (currentTab)
                {
                    case TabOrder.Portfolio:
                        UpdateBubblePanel();
                        break;
                    
                    case TabOrder.DevErrors:
                        UpdateDevErrorReportPanel();
                        break;
					case TabOrder.ProductQuality:
						UpdateProductQualityReportPanel();
						break;
                    case TabOrder.CpuReport:
                        UpdateCpuReportPanel();
                        break;
                    case TabOrder.Intent:
                        UpdateIntentPanel();
                        break;
	                case TabOrder.CustomReport:
		                UpdateCustomReportPanel();
		                break;
                    case TabOrder.Network:
                        UpdateNetworkReportPanel();
                        break;
                    case TabOrder.Maturity:
                        UpdateMaturityPanel();
                        break;
                    
                    case TabOrder.Css:
                        UpdateNpsSurveyReport();
                        break;
                }
            }
        }
        
		protected virtual void tabBar_TabPressed (object sender, TabBarEventArgs args)
		{
			ShowPanel(args.Code);
		}

		protected void ShowPanel (int panel)
		{
            using (var cursor = new WaitCursor(this))
			{
				HidePanels();

				switch ((TabOrder) panel)
				{
					case TabOrder.GameScreen:
						ShowHoldingPanel();
						break;
					case TabOrder.Businesstab:
                        ShowBusinessScorecardReport();
						break;
					case TabOrder.Portfolio:
						ShowBubblePanel();
						break;
					case TabOrder.NewApps:
					    ShowNewServicesReport();
						break;
                    case TabOrder.DevErrors:
                        ShowDevErrorReportPanel();
                        break;
					case TabOrder.ProductQuality:
						ShowProductQualityReportPanel();
						break;
					case TabOrder.Operations:
                        ShowOperationsScorecardReport();
						break;
                    case TabOrder.Incidents:
                        ShowIncidentGanttReport();
                        break;
                    case TabOrder.CpuReport:
						ShowCpuReportPanel();
						break;
					case TabOrder.Intent:
						ShowIntentPanel();
						break;
					case TabOrder.CustomReport:
						ShowCustomReportPanel();
						break;
                    case TabOrder.Network:
				        ShowNetworkReportPanel();
                        break;
                    case TabOrder.Maturity:
                        ShowMaturityPanel();
                        break;
                    case TabOrder.Process:
                        ShowProcessScorecardReport();
                        break;
                    case TabOrder.Css:
				        ShowNpsSurveyReportPanel();
                        break;
				}
			}
			currentTab = (TabOrder)panel;

			DoSize();
		}

		public void ShowHold ()
	    {
            holdingPanel.Show();

			DoSize();
	    }

	    public void ShowHoldingPanel ()
		{
			HidePanels();
			ShowHold();
		}

		protected void HidePanels ()
		{
		    holdingPanel?.Hide();

		    businessScorecardReportPanel?.Hide();

		    portfolioBubblePanel?.Hide();

		    operationsScorecardReportPanel?.Hide();

		    devErrorReportPanel?.Hide();

		    productQualityReportPanel?.Hide();

		    newServicesReportPanel?.Hide();

		    incidentGanttReportPanel?.Hide();

		    cpuReportPanel?.Hide();

		    networkReportPanel?.Hide();

		    maturityPanel?.Hide();

		    processScorecardReportPanel?.Hide();

			intentPanel?.Hide();

			customReportPanel?.Hide();

		    npsSurveyReportPanel?.Hide();
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
			Invalidate();
		}

		protected void DoSize ()
		{
            BackColor = Color.Transparent;
            
			tabBar.Bounds = new Rectangle(0, 0, Width, 25);

			mainPanel.Location = new Point(10, tabBar.Bottom);
			mainPanel.Size = new Size(Width - (2 * mainPanel.Left), Height - mainPanel.Top);

            holdingPanel.Bounds = new Rectangle(0, 30, mainPanel.Width, mainPanel.Height - 50);

		    if (businessScorecardReportPanel != null)
		    {
		        businessScorecardReportPanel.Bounds = new Rectangle(new Point(0, 0), mainPanel.Size);
		    }

		    if (processScorecardReportPanel != null)
		    {
		        processScorecardReportPanel.Bounds = new Rectangle(new Point(0, 0), mainPanel.Size);
		    }
            
			maturityPanel.Size = mainPanel.Size;
			if (maturityChart != null)
			{
				maturityChart.Bounds = new Rectangle (0, maturityRoundComboBox.Bottom, maturityPanel.Width, maturityPanel.Height - maturityRoundComboBox.Bottom);
			}
			if (currentTab == TabOrder.Maturity)
			{
				ShowMaturity();
			}

		    if (newServicesReportPanel != null)
		    {
		        newServicesReportPanel.Bounds = new Rectangle(0, 0, mainPanel.Width, mainPanel.Height);
		    }

			productQualityReportPanel.Size = mainPanel.Size;
			if (productQualityReportScrollingPanel != null)
			{
				productQualityReportScrollingPanel.Size = new Size (productQualityReportPanel.Width - (2 * productQualityReportScrollingPanel.Left), productQualityReportPanel.Height - (2 * productQualityReportScrollingPanel.Top));
			}
			if (productQualityReportChart != null)
			{
				productQualityReportChart.Width = productQualityReportPanel.Width - (2 * productQualityReportChart.Left);
				productQualityReportChart.Size = new Size(productQualityReportPanel.Width - 25, productQualityReportChart.NaturalHeight);
			}

		    if (operationsScorecardReportPanel != null)
		    {
		        operationsScorecardReportPanel.Bounds = new Rectangle(new Point(0,0), mainPanel.Size);
		    }
            
		    if (incidentGanttReportPanel != null)
		    {
                incidentGanttReportPanel.Bounds = new Rectangle(0, 0, mainPanel.Width, mainPanel.Height);
		    }

			cpuReportPanel.Size = mainPanel.Size;
			if (cpuReportChart != null)
			{
				cpuReportChart.Width = cpuReportPanel.Width - (2 * cpuReportChart.Left);
			}
			if (bladeReportChart != null)
			{
				bladeReportChart.Width = cpuReportPanel.Width - (2 * bladeReportChart.Left);
			}

			networkReportPanel.Size = mainPanel.Size;
			if (networkBoxChart != null)
			{
				networkBoxChart.Size = new Size (networkReportPanel.Width - (2 * networkBoxChart.Left), networkReportPanel.Height - networkBoxChart.Top);
			}

			npsSurveyReportPanel.Size = mainPanel.Size;
			if (npsReportChart != null)
			{
				npsReportChart.Size = new Size (npsSurveyReportPanel.Width - (2 * npsReportChart.Left), npsSurveyReportPanel.Height - npsReportChart.Top);
			}

			intentPanel.Size = mainPanel.Size;
			if (intentImage != null)
			{
				intentImage.Location = new Point ((intentPanel.Width - intentImage.Width) / 2, (intentPanel.Height - intentImage.Height) / 2);
			}

			customReportPanel.Size = mainPanel.Size;
			if (customReportImage != null)
			{
				customReportImage.Bounds = new RectangleFromBounds { Left = customReportPanel.Width / 20, Top = customReportPanel.Height / 20, Right = customReportPanel.Width * 19 / 20, Bottom = customReportPanel.Height * 19 / 20 }.ToRectangle();
			}
		}

		protected void GetRoundScores ()
		{
			if (roundScores != null)
			{
				foreach (var scores in roundScores)
				{
					scores.Dispose();
				}
			}

			roundScores = new List<DevOpsRoundScores>();
			// Won't be profit
			var previousProfit = 0;
			var newServices = 0;
			var previousRevenue = 0;
			for (var i = 1; i <= gameFile.LastRoundPlayed; i++)
			{
				var scores = new DevOpsRoundScores(gameFile, i, previousProfit, newServices, previousRevenue, spendOverrides);
				roundScores.Add(scores);
				previousProfit = scores.Profit;
				newServices = scores.NumNewServices;
				previousRevenue = scores.Revenue;

				if (i > 1)
				{
					scores.inner_sections = (Hashtable) (roundScores[i - 2].outer_sections.Clone());
				}
				else
				{
					scores.inner_sections = null;
				}
			}

            OnRoundScoresChanged();
		}

		public override void ReloadDataAndShow (bool reload)
		{
			if (reload)
			{
				GetRoundScores();
			}

			redrawCpuReportChart = true;
            redrawMaturity = true;
            redrawNpsReport = true;
            

			tabBar.SelectedTabCode = 0;
		}

		static void RemoveOldPanelControls(Panel panel, Control control)
		{
			if (control != null)
			{
				if (panel.Controls.Contains(control))
				{
					panel.Controls.Remove(control);
					control.Dispose();
				}
			}
		}

	    void ShowBusinessScorecardReport ()
	    {
	        if (businessScorecardReportPanel == null)
	        {
                businessScorecardReportPanel = new TableReportPanel(this, roundScores,
                    () =>
                    {
                        var scorecardReport = new DevOpsBusinessScorecard(gameFile, roundScores);

                        return new TableReportBuildResult
                        {
                            ReportFilename = scorecardReport.CreateReportAndFilename(),
                            PreferredTableHeight = scorecardReport.Height
                        };
                    });

                mainPanel.Controls.Add(businessScorecardReportPanel);
	        }

	        businessScorecardReportPanel.Show();
            businessScorecardReportPanel.BringToFront();

	        DoSize();
	    }
        
        void UpdateBubblePanel ()
		{
			ShowBubble();
		}

		public void ShowBubblePanel()
		{
			HidePanels();

			preventScreenUpdate = true;
			portfolioRoundComboBox.SelectedIndex = gameFile.LastRoundPlayed - 1;
			portfolioBusinessComboBox.SelectedIndex = 0;
			preventScreenUpdate = false;
			ShowBubble();

			portfolioBubblePanel.Show();
		}

		void ShowBubble ()
		{
			RemoveOldPanelControls(portfolioBubblePanel, bubbleImagePanel);

			var bubbleRoundIndex = portfolioRoundComboBox.SelectedIndex + 1;
			var bubbleRegionIndex = portfolioBusinessComboBox.SelectedIndex + 1;

			var imageFilename = CONVERT.Format("\\images\\bubbles\\r{0}u{1}.png",bubbleRoundIndex, bubbleRegionIndex);

			if (!string.IsNullOrEmpty(imageFilename))
			{
				var fullBubblePath = (CONVERT.Format("{0}{1}", AppInfo.TheInstance.Location, imageFilename));
				var bubbleBmp = Repository.TheInstance.GetImage(fullBubblePath);
				bubbleImagePanel = new ImagePanel(bubbleBmp, mainPanel.Width, 625);
				bubbleImagePanel.Location = new Point(0, portfolioRoundComboBox.Bottom + 15);

				portfolioBubblePanel.Controls.Add(bubbleImagePanel);
				bubbleImagePanel.BringToFront();
			}
			else
			{
				bubbleImagePanel = null;
			}
		}

	    void ShowIncidentGanttReport ()
	    {
	        if (incidentGanttReportPanel == null)
	        {
                incidentGanttReportPanel = new CloudTimeChartReportPanel(gameFile, this, roundScores,
                    (round, business) =>
                    {
                        var report = new IncidentGanttReport(business, true);

                        return report.BuildReport(gameFile, round, true, roundScores[round - 1]);
                    });

                mainPanel.Controls.Add(incidentGanttReportPanel);
	        }
	        incidentGanttReportPanel.Show();
	        incidentGanttReportPanel.BringToFront();

            DoSize();
        }


		void UpdateCpuReportPanel()
		{
			ShowCpuReport();
		}

		void ShowCpuReportPanel()
		{
			HidePanels();
			cpuReportPanel.Show();

			if (redrawCpuReportChart)
			{
                cpuReportRoundComboBox.SelectedIndex = Math.Max(0, gameFile.LastRoundPlayed - 1);
                UpdateCpuReportPanel();
			}
		}

		void ShowCpuReport()
		{
			RemoveOldPanelControls(cpuReportPanel, cpuReportChart);
			RemoveOldPanelControls(cpuReportPanel, bladeReportChart);

			var isData = true;
			foreach(var rdata in roundScores)
			{
				if (rdata != null)
				{
					if (rdata.WasUnableToGetData)
					{
						isData = false;
					}
				}
			}

			if (isData)
			{
				using(var cursor = new WaitCursor(this))
				{
                    var cpuReport = new CpuUsageReport(gameFile, cpuReportRoundComboBox.SelectedIndex + 1);

					

					var heightPadding = 5;

                    var panelHeight = cpuReportPanel.Height - cpuReportRoundComboBox.Bottom - (3 * heightPadding); //Padding

					var cpuReportHeight = (int)Math.Floor(panelHeight * 0.5);

				    cpuReportChart =
				        new GroupedBarChart(BasicXmlDocument.CreateFromFile(cpuReport.BuildReport()).DocumentElement)
				        {
				            Size = new Size(cpuReportPanel.Width, cpuReportHeight),
				            Location = new Point(0, cpuReportRoundComboBox.Bottom + heightPadding),
				            XAxisHeight = 50,
				            YAxisWidth = 35,
				            LegendX = cpuReportRoundComboBox.Right + 50,
				            LegendY = 10.0,
				            LegendHeight = 50
				        };

				    var bladeReportHeight = (int)Math.Ceiling(panelHeight * 0.5);

                    var bladeReport = new ServerBladeUsageReport(gameFile, cpuReportRoundComboBox.SelectedIndex + 1);

				    bladeReportChart =
				        new GroupedBarChart(BasicXmlDocument.CreateFromFile(bladeReport.BuildReport())
				            .DocumentElement)
				        {
				            Size = new Size(cpuReportPanel.Width, bladeReportHeight),
				            Location = new Point(0, cpuReportChart.Bottom + heightPadding),
				            XAxisHeight = 50,
				            YAxisWidth = 35,
				            LegendX = cpuReportRoundComboBox.Right + 50,
				            LegendY = 10.0,
				            LegendHeight = 50
				        };


					redrawCpuReportChart = false;

                    cpuReportPanel.Controls.Add(cpuReportChart);
                    cpuReportPanel.Controls.Add(bladeReportChart);
                    cpuReportChart.BringToFront();
                    cpuReportRoundComboBox.BringToFront();
                    bladeReportChart.BringToFront();
				}
			}
		}

	    void ShowOperationsScorecardReport ()
	    {
	        if (operationsScorecardReportPanel == null)
	        {
	            operationsScorecardReportPanel = new TableReportPanel(this, roundScores,
	                () =>
	                {
	                    var scorecardReport = new DevOpsOperationsScorecard(gameFile, roundScores);

	                    return new TableReportBuildResult
	                    {
	                        ReportFilename = scorecardReport.CreateReportAndFilename(),
	                        PreferredTableHeight = scorecardReport.Height
	                    };
	                });

                mainPanel.Controls.Add(operationsScorecardReportPanel);
	        }

            operationsScorecardReportPanel.Show();
            operationsScorecardReportPanel.BringToFront();

	        DoSize();
	    }
        
	    void ShowNewServicesReport()
	    {
	        if (newServicesReportPanel == null)
	        {
	            newServicesReportPanel = new CloudTimeChartReportPanel(gameFile, this, roundScores, (round, business) =>
	            {
	                var report = new NewServicesReport(gameFile, round);

	                return report.BuildReport(business, true, false, false);
	            });

	            mainPanel.Controls.Add(newServicesReportPanel);
	        }

	        newServicesReportPanel.Show();
	        newServicesReportPanel.BringToFront();

	        DoSize();
	    }


        void ShowDevErrorReportPanel()
        {
            HidePanels();
            preventScreenUpdate = true;
            devErrorReportRoundComboBox.SelectedIndex = Math.Min(Math.Max(0, gameFile.LastRoundPlayed - 1), devErrorReportRoundComboBox.Items.Count - 1);
            preventScreenUpdate = false;
            ShowDevErrorReport();
        }

        protected void ShowDevErrorReport()
        {
            if (devErrorReportChart != null)
            {
                devErrorReportPanel.Controls.Remove(devErrorReportChart);
                devErrorReportChart.Dispose();
                devErrorReportChart = null;
            }

            var isData = true;
            foreach (var rdata in roundScores)
            {
                if (rdata != null)
                {
                    if (rdata.WasUnableToGetData)
                    {
                        isData = false;
                    }
                }
            }

            if (isData)
            {
                using (var cursor = new WaitCursor(this))
                {
                    var devErrorReport = new DevOps_DevErrorReport(gameFile.NetworkModel);

                    var roundSelected = devErrorReportRoundComboBox.SelectedIndex;
                    if (roundSelected < gameFile.LastRoundPlayed)
                    {
                        var reportFile = devErrorReport.BuildReport(gameFile, roundSelected + 1);

                        var heightPadding = 5;
                        var xml = BasicXmlDocument.CreateFromFile(reportFile).DocumentElement;

                        devErrorReportChart = new DevErrorReport(gameFile.NetworkModel, xml)
                        {
                            Size = new Size(devErrorReportPanel.Width, 600),
                            Location = new Point(0, devErrorReportRoundComboBox.Bottom + heightPadding)
                        };
                        devErrorReportPanel.Controls.Add(devErrorReportChart);

                        devErrorReportPanel.Show();
                        devErrorReportRoundComboBox.BringToFront();
                        devErrorReportChart.BringToFront();
                    }
                }
            }
        }

        void UpdateDevErrorReportPanel()
        {
            ShowDevErrorReport();
        }

        void UpdateNetworkReportPanel()
        {
            ShowNetworkReport();
        }

        void ShowNetworkReportPanel()
        {
            HidePanels();
            networkReportPanel.Show();
            ShowNetworkReport();
        }

	    void ShowNetworkReport()
	    {
	        RemoveOldPanelControls(networkReportPanel, networkReportBack);
	        RemoveOldPanelControls(networkReportPanel, networkBoxChart);

	        networkReportBack = BuildBackgroundImagePanel();
	        networkReportPanel.Controls.Add(networkReportBack);
	        networkReportBack.SendToBack();
            
	        var networkReport = new NetworkReport(gameFile, gameFile.NetworkModel, gameFile.CurrentRound);

	        var reportFile = networkReport.BuildReport();

	        networkBoxChart =
	            new GroupedBoxChart(BasicXmlDocument.CreateFromFile(reportFile).DocumentElement)
	            {
	                Location = new Point(0, 15)
	            };


	        var height = Height - 30;

	        networkBoxChart.Size = new Size(Width, height);

	        networkReportPanel.Controls.Add(networkBoxChart);
            
	        networkBoxChart.BringToFront();

	    }

        void ShowMaturityPanel()
        {
            HidePanels();
            maturityPanel.Show();

            if (redrawMaturity)
            {
                preventScreenUpdate = true;
                maturityRoundComboBox.SelectedIndex = Math.Max(0, gameFile.LastRoundPlayed - 1);
                preventScreenUpdate = false;

                ShowMaturity();
            }
        }

        protected void ShowMaturity()
        {
            RemoveOldPanelControls(maturityPanel, maturityChart);

            if ((maturityRoundComboBox.SelectedIndex + 1) > gameFile.LastRoundPlayed)
            {
                return;
            }

            var maturityReport = new OpsMaturityReport();

            var maturityXmlFile = maturityReport.BuildReport(gameFile, maturityRoundComboBox.SelectedIndex + 1, new ArrayList(roundScores)); // it wants an ArrayList .. ugh!

            if (!string.IsNullOrEmpty(maturityXmlFile))
            {
                maturityChart = new PieChart
                                {
                                    Location = new Point(0, maturityRoundComboBox.Bottom + 10),
                    Size = new Size(maturityPanel.Width, maturityPanel.Height),
                    ShowDropShadow = false,
                    KeyYOffset = 32
                                };
	            maturityChart.SetBackColorOverride(DefaultBackColor);

				maturityChart.LoadData(File.ReadAllText(maturityXmlFile));

                maturityPanel.Controls.Add(maturityChart);
                maturityChart.BringToFront();
            }

            redrawMaturity = false;
        }

        void UpdateMaturityPanel()
        {
            ShowMaturity();
        }

	    void ShowProcessScorecardReport ()
	    {
	        if (processScorecardReportPanel == null)
	        {
	            processScorecardReportPanel = new TableReportPanel(this, roundScores,
	                () =>
	                {
                        
                        var scorecardReport = new OpsProcessScorecardReportBuilder(gameFile, roundScores.Select(r => (RoundScores)r).ToList());

                        var reportFilename = scorecardReport.CreateReportAndFilename();
                        var tableHeight = scorecardReport.Height;

                        return new TableReportBuildResult
	                    {
                            ReportFilename = reportFilename,
                            PreferredTableHeight = tableHeight
                        };
	                });

                mainPanel.Controls.Add(processScorecardReportPanel);
	        }
            
            processScorecardReportPanel.Show();
            processScorecardReportPanel.BringToFront();
	    }
        
		void ShowIntentPanel()
		{
			HidePanels();
			preventScreenUpdate = true;
            intentRoundComboBox.SelectedIndex = Math.Min(Math.Max(0, gameFile.LastRoundPlayed - 1), intentRoundComboBox.Items.Count - 1);
			preventScreenUpdate = false;
			ShowIntent();
			intentPanel.Show();
		}

		protected void ShowIntent()
		{
			RemoveOldPanelControls(intentPanel, intentImage);

			var selectedItem = intentRoundComboBox.SelectedIndex + 1;

			var issuesName = "\\images\\intent\\Intent_Issues_R" + CONVERT.ToStr(selectedItem) + ".png";
			var actionsName = "\\images\\intent\\Intent_Actions_R" + CONVERT.ToStr(selectedItem) + ".png";
			var issuesBmp = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + issuesName);
			var actionsBmp = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + actionsName);

			intentImage = new IntentImagePanel(issuesBmp, actionsBmp)
                {
                    BackColor = Color.Orange
                };
			intentPanel.Controls.Add(intentImage);
			intentImage.BringToFront();

			intentImage.Size = new Size (Math.Max(issuesBmp.Width, actionsBmp.Width), Math.Max(issuesBmp.Height, actionsBmp.Height));
			intentImage.Location = new Point(0, intentRoundComboBox.Bottom + 10);

			intentPanel.Controls.Add(intentRoundComboBox);
			intentRoundComboBox.BringToFront();

			DoSize();
		}

		void UpdateIntentPanel()
		{
			ShowIntent();
		}

		void ShowProductQualityReportPanel ()
		{
			HidePanels();
			preventScreenUpdate = true;
			productQualityReportRoundComboBox.SelectedIndex = Math.Min(Math.Max(0, gameFile.LastRoundPlayed - 1), productQualityReportRoundComboBox.Items.Count - 1);
			preventScreenUpdate = false;
			ShowProductQualityReport();
		}

		protected void ShowProductQualityReport()
		{
			if (productQualityReportScrollingPanel != null)
			{
				productQualityReportPanel.Controls.Remove(productQualityReportScrollingPanel);
				productQualityReportScrollingPanel.Dispose();
				productQualityReportScrollingPanel = null;
			}

			if (! roundScores.Any(roundScore => roundScore.WasUnableToGetData))
			{
				using (var cursor = new WaitCursor (this))
				{
					var round = productQualityReportRoundComboBox.SelectedIndex + 1;
					if (round <= gameFile.LastRoundPlayed)
					{
						var builder = new ProductQualityReportBuilder (gameFile);
						var reportFile = builder.BuildReport(round);

						var topPadding = 25;
						var bottomPadding = 10;
						var xml = BasicXmlDocument.CreateFromFile(reportFile).DocumentElement;

						productQualityReportScrollingPanel = new Panel
						{
							Location = new Point (0, productQualityReportRoundComboBox.Bottom + topPadding),
							Size = new Size (productQualityReportPanel.Width, productQualityReportPanel.Height - (productQualityReportRoundComboBox.Bottom + topPadding + bottomPadding)),
							AutoScroll = true
						};

						productQualityReportChart = new BusinessServiceHeatMap
						{
							Location = new Point (0, 0)
						};

						productQualityReportChart.LoadData(xml);

						productQualityReportPanel.Controls.Add(productQualityReportScrollingPanel);
						productQualityReportScrollingPanel.Controls.Add(productQualityReportChart);
						productQualityReportChart.BringToFront();
					}

					productQualityReportPanel.Show();
					productQualityReportRoundComboBox.BringToFront();
				}
			}

			DoSize();
		}

		void UpdateProductQualityReportPanel ()
		{
			ShowProductQualityReport();
		}

		void CreateProductQualityReportPanel ()
		{
		    productQualityReportPanel = new Panel { BackColor = Color.Transparent };
		    mainPanel.Controls.Add(productQualityReportPanel);

			productQualityReportRoundComboBox = DevOpsComboBoxBuilder.CreateRoundComboBox(gameFile);
			productQualityReportRoundComboBox.Location = SkinningDefs.TheInstance.GetPointData("round_combo_box_position", new Point(0, 15));
            productQualityReportRoundComboBox.SelectedIndexChanged += productQualityReportRoundComboBox_SelectedIndexChanged;

			productQualityReportPanel.Controls.Add(productQualityReportRoundComboBox);
			productQualityReportRoundComboBox.BringToFront();
		}

		void productQualityReportRoundComboBox_SelectedIndexChanged (object sender, EventArgs e)
		{
			if (! preventScreenUpdate)
			{
				UpdateProductQualityReportPanel();
			}
		}

		void ShowCustomReportPanel ()
		{
			HidePanels();
			preventScreenUpdate = true;
			customReportRoundComboBox.SelectedIndex = Math.Min(Math.Max(0, gameFile.LastRoundPlayed - 1), customReportRoundComboBox.Items.Count - 1);
			preventScreenUpdate = false;
			ShowCustomReport();
			customReportPanel.Show();
		}

		protected void ShowCustomReport ()
		{
			RemoveOldPanelControls(customReportPanel, customReportImage);

			var round = customReportRoundComboBox.SelectedIndex + 1;

			customReportImage = new RevealablePanel();
			customReportPanel.Controls.Add(customReportImage);
			customReportImage.LoadImages(gameFile.GetCustomContentFilename(CONVERT.Format("report_round_{0}", round)),
				gameFile.GetCustomContentFilename(CONVERT.Format("report_round_{0}_revealed", round)));

			customReportPanel.Controls.Add(customReportRoundComboBox);
			customReportRoundComboBox.BringToFront();

			DoSize();
		}

		void UpdateCustomReportPanel ()
		{
			ShowCustomReport();
		}

		public override void ShowReport (ChartScreenTabOption report)
		{
			tabBar.SelectedTabCode = report.Tab.code;
			var businessIndex = (report.Business == null) ? 0 : gameFile.NetworkModel.GetNamedNode(report.Business).GetIntAttribute("shortdesc", 0);

			switch ((TabOrder) report.Tab.code)
			{
				case TabOrder.Businesstab:
				case TabOrder.Operations:
				case TabOrder.Css:
				case TabOrder.Network:
					break;

				case TabOrder.CpuReport:
					cpuReportRoundComboBox.SelectedIndex = report.Round.Value - 1;
					break;

				case TabOrder.Maturity:
					maturityRoundComboBox.SelectedIndex = report.Round.Value - 1;
					break;

				case TabOrder.ProductQuality:
					productQualityReportRoundComboBox.SelectedIndex = report.Round.Value - 1;
					break;

				case TabOrder.Incidents:
					incidentGanttReportPanel.BusinessSelector.SelectedIndex = businessIndex;
					incidentGanttReportPanel.RoundSelector.SelectedIndex = report.Round.Value - 1;
					break;

				case TabOrder.NewApps:
					newServicesReportPanel.BusinessSelector.SelectedIndex = businessIndex;
					newServicesReportPanel.RoundSelector.SelectedIndex = report.Round.Value - 1;
					break;
			}
		}

		public override IList<ChartScreenTabOption> GetAllAvailableReports ()
		{
			var businesses = new List<Node>((Node[]) gameFile.NetworkModel.GetNodesWithAttributeValue("type", "BU").ToArray(typeof(Node)));
			businesses.Insert(0, null);
			var rounds = gameFile.LastRoundPlayed;

			var results = new List<ChartScreenTabOption>();

			results.Add(new ChartScreenTabOption { Tab = tabBar.GetTabByCode((int) TabOrder.Businesstab), Name = "Business", Business = null, Round = null });
			results.Add(new ChartScreenTabOption { Tab = tabBar.GetTabByCode((int) TabOrder.Operations), Name = "Operations", Business = null, Round = null });
			results.Add(new ChartScreenTabOption { Tab = tabBar.GetTabByCode((int) TabOrder.Css), Name = "NPS", Business = null, Round = null });
			results.Add(new ChartScreenTabOption { Tab = tabBar.GetTabByCode((int) TabOrder.Network), Name = "Network", Business = null, Round = null });

			for (var round = 1; round <= rounds; round++)
			{
				results.Add(new ChartScreenTabOption { Tab = tabBar.GetTabByCode((int) TabOrder.CpuReport), Name = $"CPU_Round_{round}", Business = null, Round = round });
				results.Add(new ChartScreenTabOption { Tab = tabBar.GetTabByCode((int) TabOrder.Maturity), Name = $"Maturity_Round_{round}", Business = null, Round = round });
				results.Add(new ChartScreenTabOption { Tab = tabBar.GetTabByCode((int) TabOrder.ProductQuality), Name = $"ProductQuality_Round_{round}", Business = null, Round = round });

				foreach (var business in businesses)
				{
					var businessName = business?.GetAttribute("name");
					var displayBusinessName = businessName ?? "All BUs";
					results.Add(new ChartScreenTabOption { Tab = tabBar.GetTabByCode((int) TabOrder.NewApps), Name = $"NewServices_{displayBusinessName}_Round_{round}", Business = businessName, Round = round });
					results.Add(new ChartScreenTabOption { Tab = tabBar.GetTabByCode((int) TabOrder.Incidents), Name = $"Incidents_{displayBusinessName}_Round_{round}", Business = businessName, Round = round });
				}
			}

			return results;
		}
	}
}