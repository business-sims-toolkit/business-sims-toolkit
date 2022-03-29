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
using DevOpsReportCharts;
using Events;
using DevOps.ReportsScreen.Interfaces;
using GameManagement;
using LibCore;
using Network;
using ReportBuilder;
using ResizingUi;

// ReSharper disable UnusedVariable

using TableReportBuilderHandler = DevOpsReportCharts.TableReportBuilderHandler;

namespace DevOps.ReportsScreen
{
    public class ReportsScreenPanel : PureTabbedChartScreen, IRoundScoresUpdater<DevOpsRoundScores>
    {
        public ReportsScreenPanel (NetworkProgressionGameFile gameFile, SupportSpendOverrides spendOverrides, bool includeBars)
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.DoubleBuffer, true);

            this.gameFile = gameFile;
            this.spendOverrides = spendOverrides;
            this.includeBars = includeBars;
            
            currentTab = ReportsTabOrder.GameScreen;

            tabBar = new TabBar();
            tabBar.AddTab("Hold", (int)ReportsTabOrder.GameScreen, true);
            tabBar.AddTab("Business", (int)ReportsTabOrder.BusinessTab, true);
            tabBar.AddTab("Process", (int)ReportsTabOrder.Process, true);
            tabBar.AddTab("Maturity", (int)ReportsTabOrder.Maturity, true);
            tabBar.AddTab("Apps", (int)ReportsTabOrder.NewApps, true);
            tabBar.AddTab("Product", (int)ReportsTabOrder.ProductQuality, true);
            tabBar.AddTab("Operations", (int)ReportsTabOrder.Operations, true);
            tabBar.AddTab("Incidents", (int)ReportsTabOrder.Incidents, true);
            tabBar.AddTab("CPU", (int)ReportsTabOrder.CpuReport, true);
            tabBar.AddTab("Network", (int)ReportsTabOrder.Network, true);
            tabBar.AddTab("CSAT", (int)ReportsTabOrder.Css, true);
	        tabBar.AddTab("Intent", (int) ReportsTabOrder.Intent, true);
	        tabBar.AddTab("Comparison", (int) ReportsTabOrder.Comparison, true);

			if (gameFile.CustomContentSource != CustomContentSource.None)
            {
                tabBar.AddTab("Custom", (int)ReportsTabOrder.CustomReport, true);
            }

            tabBar.TabPressed += tabBar_TabPressed;
            Controls.Add(tabBar);

            tabBar.BackColor = Color.White;

            GetRoundScores();

            reportTabToScreenProperties =
                new Dictionary<ReportsTabOrder, ReportScreenProperties>
                {
                    [ReportsTabOrder.GameScreen] = CreateHoldingScreenProperties()
                };

            var panelBackColour = SkinningDefs.TheInstance.GetColorData("main_background_colour", Color.White);

            reportScreen = new ReportScreen(this, gameFile, roundScores,
                reportTabToScreenProperties[ReportsTabOrder.GameScreen])
            {
                BackColor = panelBackColour
            };
            Controls.Add(reportScreen);

            reportScreen.SelectedRoundChanged += reportScreen_SelectedRoundChanged;
            reportScreen.SelectedBusinessChanged += reportScreen_SelectedBusinessChanged;

            reportScreen.MouseEventFired += reportScreen_MouseEventFired;
 
        }

        void reportScreen_MouseEventFired(object sender, SharedMouseEventArgs e)
        {
            linkedReportsScreen.reportScreen.ReceiveMouseEvent(e);
        }

        public event EventHandler<EventArgs<List<DevOpsRoundScores>>> RoundScoresChanged;

        public override void ReloadDataAndShow (bool reload)
        {
            if (reload)
            {
                GetRoundScores();
            }

            tabBar.SelectedTabCode = 0;
        }


        ReportsScreenPanel linkedReportsScreen;
        public ReportsScreenPanel LinkedReportsScreen

        {
            set
            {
                if (linkedReportsScreen == value)
                {
                    return;
                }

                if (linkedReportsScreen != null)
                {
                    linkedReportsScreen.ReportTabChanged -= linkedReportsScreen_ReportTabChanged;
                    linkedReportsScreen.ReportSelectedRoundChanged -= linkedReportsScreen_ReportSelectedRoundChanged;
                    linkedReportsScreen.ReportSelectedBusinessChanged -= linkedReportsScreen_ReportSelectedBusinessChanged;
                }

                linkedReportsScreen = value;
                reportScreen.LinkedReportScreen = linkedReportsScreen.reportScreen;

                if (linkedReportsScreen != null)
                {
                    linkedReportsScreen.ReportTabChanged += linkedReportsScreen_ReportTabChanged;
                    linkedReportsScreen.ReportSelectedRoundChanged += linkedReportsScreen_ReportSelectedRoundChanged;
                    linkedReportsScreen.ReportSelectedBusinessChanged += linkedReportsScreen_ReportSelectedBusinessChanged;
                }
            }
        }

        event EventHandler<ReadonlyEventArgs<ReportsTabOrder>> ReportTabChanged;
        event EventHandler<ReadonlyEventArgs<int>> ReportSelectedRoundChanged;
        event EventHandler<ReadonlyEventArgs<string>> ReportSelectedBusinessChanged;

        void OnReportTabChanged ()
        {
            ReportTabChanged?.Invoke(this, ReportTabChanged.CreateReadonlyArgs(currentTab));
        }

        void OnReportSelectedRoundChanged (ReadonlyEventArgs<int> args)
        {
            ReportSelectedRoundChanged?.Invoke(this, args);
        }

        void OnReportSelectedBusinessChanged (ReadonlyEventArgs<string> args)
        {
            ReportSelectedBusinessChanged?.Invoke(this, args);
        }


	    bool tabBarChangedLocally = true;
        void linkedReportsScreen_ReportTabChanged (object sender, ReadonlyEventArgs<ReportsTabOrder> e)
        {
	        tabBarChangedLocally = false;
			tabBar.SelectedTabCode = (int) e.Parameter;
        }

        void linkedReportsScreen_ReportSelectedRoundChanged(object sender, ReadonlyEventArgs<int> e)
        {
            reportScreen.SelectedRound = e.Parameter;
        }

        void linkedReportsScreen_ReportSelectedBusinessChanged(object sender, ReadonlyEventArgs<string> e)
        {
            reportScreen.SelectedBusiness = e.Parameter;
        }

        void reportScreen_SelectedRoundChanged(object sender, ReadonlyEventArgs<int> e)
        {
            OnReportSelectedRoundChanged(e);
        }

        void reportScreen_SelectedBusinessChanged(object sender, ReadonlyEventArgs<string> e)
        {
            OnReportSelectedBusinessChanged(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // TODO
                if (linkedReportsScreen != null)
                {
                    linkedReportsScreen.ReportTabChanged -= linkedReportsScreen_ReportTabChanged;
                    linkedReportsScreen.ReportSelectedRoundChanged -= linkedReportsScreen_ReportSelectedRoundChanged;
                    linkedReportsScreen.ReportSelectedBusinessChanged -= linkedReportsScreen_ReportSelectedBusinessChanged;
                }
            }
            base.Dispose(disposing);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.Button == MouseButtons.Left)
            {
                ((Form)TopLevelControl).DragMove();
            }
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            DoSize();
        }

        protected override void OnPaint (PaintEventArgs e)
        {
            if (! includeBars)
            {
                return;
            }

            using (var barBrush =
                new SolidBrush(SkinningDefs.TheInstance.GetColorData("game_screen_top_bar_back_colour")))
            {
                e.Graphics.FillRectangle(barBrush, topBarBounds);
                e.Graphics.FillRectangle(barBrush, bottomBarBounds);
            }

            var poweredByImage = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + @"images\panels\low_poweredby_logo.png");
            e.Graphics.DrawImage(poweredByImage, new Rectangle(bottomBarBounds.Left + 150, bottomBarBounds.Top + (bottomBarBounds.Height - poweredByImage.Height) / 2, poweredByImage.Width, poweredByImage.Height));

            var topBarContentSize = new Size(Width / 3, topBarBounds.Height);
            
            var imageBounds = topBarBounds.AlignRectangle(topBarContentSize, StringAlignment.Far);

            var logoImage = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + @"images\panels\top_devops_logo.png");
            e.Graphics.DrawImage(logoImage, imageBounds.CentreSubRectangle(logoImage.Width, logoImage.Height));

            using (var titleFont = SkinningDefs.TheInstance.GetFont(14))
            {
                var textBounds = topBarBounds.AlignRectangle(topBarContentSize);

                e.Graphics.DrawString("Reports Screen", titleFont, Brushes.White, new RectangleF(textBounds.X, textBounds.Y, textBounds.Width, textBounds.Height));
            }
        }

        void GetRoundScores ()
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
                    scores.inner_sections = (Hashtable)(roundScores[i - 2].outer_sections.Clone());
                }
                else
                {
                    scores.inner_sections = null;
                }
            }

            OnRoundScoresChanged();
        }

	    ReportScreenProperties CreateReportProperties (ReportsTabOrder panel)
	    {
		    switch (panel)
		    {
			    case ReportsTabOrder.GameScreen:
				    return CreateHoldingScreenProperties();

			    case ReportsTabOrder.BusinessTab:
				    return CreateBusinessScorecardReportProperties();

			    case ReportsTabOrder.NewApps:
				    return CreateNewAppsReportProperties();

				case ReportsTabOrder.DevErrors:
				    return CreateDevErrorsReportProperties();

			    case ReportsTabOrder.ProductQuality:
					return CreateProductQualityReportProperties();

			    case ReportsTabOrder.Operations:
				    return CreateOperationsScorecardReportProperties();

			    case ReportsTabOrder.Incidents:
				    return CreateIncidentReportProperties();

			    case ReportsTabOrder.CpuReport:
					return CreateCpuReportProperties();

			    case ReportsTabOrder.Intent:
					return CreateIntentReportProperties();

			    case ReportsTabOrder.CustomReport:
					return CreateCustomReportProperties();

			    case ReportsTabOrder.Network:
					return CreateNetworkReportProperties();

			    case ReportsTabOrder.Maturity:
					return CreateMaturityReportProperties();

			    case ReportsTabOrder.Process:
					return CreateProcessReportProperties();

			    case ReportsTabOrder.Css:
					return CreateCsatReportProperties();

			    case ReportsTabOrder.Comparison:
					return CreateComparisonReportProperties();

			    default:
				    return null;
		    }
	    }

		void ShowPanel (int panelCode)
		{
			var panel = (ReportsTabOrder) panelCode;

            using (var cursor = new WaitCursor(this))
			{
				if (! reportTabToScreenProperties.ContainsKey(panel))
				{
					reportTabToScreenProperties[panel] = CreateReportProperties(panel);
				}

				ShowReport(reportTabToScreenProperties[panel]);
			}

            currentTab = panel;

            DoSize();
        }

        void DoSize ()
        {
            
            const int barHeight = 40;
            topBarBounds = new Rectangle(0, 0, Width, barHeight);
            bottomBarBounds = new Rectangle(0, Height - barHeight, Width, barHeight);

            var reportsBounds = new RectangleFromBounds
            {
                Left = 0,
                Right = Width,
                Top = includeBars ? topBarBounds.Bottom : 0,
                Bottom = includeBars ? bottomBarBounds.Top : Height
            }.ToRectangle();

            tabBar.Bounds = reportsBounds.AlignRectangle(Width, 25, StringAlignment.Center, StringAlignment.Near);
            
            
            reportScreen.Bounds = new RectangleFromBounds
                {
                    Left = 0,
                    Top = tabBar.Bottom,
                    Right = reportsBounds.Width,
                    Bottom = reportsBounds.Bottom
                }.ToRectangle();
            

            if (currentTab == ReportsTabOrder.Maturity)
            {
                ShowReport(reportTabToScreenProperties[currentTab]);
            }

            Invalidate();
        }
        
        void ShowReport (ReportScreenProperties screenProperties)
        {
            GetRoundScores();
            reportScreen.ChangeContent(screenProperties);
        }

        
        ReportScreenProperties CreateHoldingScreenProperties ()
        {
            return new ReportScreenProperties
            {
                ContentCreator = screen => new DevOpsHoldingPanel(gameFile)
            };
        }
        
        ReportScreenProperties CreateBusinessScorecardReportProperties ()
        {
            TableReportBuildResult BusinessScorecardReportBuilder()
            {
                var scorecardReport = new DevOpsBusinessScorecard(gameFile, roundScores);

                return new TableReportBuildResult
                {
                    ReportFilename = scorecardReport.CreateReportAndFilename(),
                    PreferredTableHeight = scorecardReport.Height
                };
            }

            return new ReportScreenProperties
            {
                ContentCreator = CreateTableContentCreator(BusinessScorecardReportBuilder)
            };
        }

        ReportScreenProperties CreateProcessReportProperties()
        {
            TableReportBuildResult ProcessScorecardReportBuilder()
            {
                var scorecardReport = new OpsProcessScorecardReportBuilder(gameFile, roundScores.Select(r => (RoundScores)r).ToList());

                var reportFilename = scorecardReport.CreateReportAndFilename();
                var tableHeight = scorecardReport.Height;

                return new TableReportBuildResult
                {
                    ReportFilename = reportFilename,
                    PreferredTableHeight = tableHeight
                };
            }

            return new ReportScreenProperties
            {
                ContentCreator = CreateTableContentCreator(ProcessScorecardReportBuilder)
            };
        }

        ReportScreenProperties CreateMaturityReportProperties ()
        {
            SharedMouseEventControl ContentCreator (ReportScreen screen)
            {
                var maturityReport = new OpsMaturityReport();

                var maturityXmlFile = maturityReport.BuildReport(gameFile, screen.SelectedRound, new ArrayList(roundScores)); // it wants an ArrayList .. ugh!

                if (! File.Exists(maturityXmlFile))
                {
                    return null;
                }

                var maturityChart = new PieChart
                {
                    Size = screen.ContentBounds.Size,
                    ShowDropShadow = false,
                    KeyYOffset = 32
                };
                maturityChart.SetBackColorOverride(DefaultBackColor);

                maturityChart.LoadData(File.ReadAllText(maturityXmlFile));

                return new PieChartWrapper(maturityChart);
            }
            
            return new ReportScreenProperties
            {
                ContentCreator = ContentCreator,
                RoundComboBox = DevOpsComboBoxBuilder.CreateRoundComboBox(gameFile)
            };
        }

        ReportScreenProperties CreateNewAppsReportProperties ()
        {
            string NewServicesReportBuilder (int round, string business)
            {
                var report = new NewServicesReport(gameFile, round);

                return report.BuildReport(business, true, false, false);
            }

            return new ReportScreenProperties
            {
                ContentCreator = CreateCloudTimeChartContentCreator(NewServicesReportBuilder),
                RoundComboBox = DevOpsComboBoxBuilder.CreateRoundComboBox(gameFile),
                BusinessComboBox = DevOpsComboBoxBuilder.CreateBusinessComboBox(gameFile, true)
            };
        }

        
        ReportScreenProperties CreateDevErrorsReportProperties ()
        {
            SharedMouseEventControl ContentCreator(ReportScreen screen)
            {
                var devErrorReport = new DevOps_DevErrorReport(gameFile.NetworkModel);

                var reportFile = devErrorReport.BuildReport(gameFile, screen.SelectedRound);

                return new DevOpsErrorReportPanel(BasicXmlDocument.CreateFromFile(reportFile).DocumentElement);
            }

            return new ReportScreenProperties
            {
                ContentCreator = ContentCreator,
                RoundComboBox = DevOpsComboBoxBuilder.CreateRoundComboBox(gameFile)
            };
        }

        ReportScreenProperties CreateProductQualityReportProperties ()
        {
            return new ReportScreenProperties
            {
                ContentCreator = screen => new ProductQualityReportPanel(gameFile, screen.SelectedRound) {AutoScroll = true},
                RoundComboBox = DevOpsComboBoxBuilder.CreateRoundComboBox(gameFile)
            };
        }

        ReportScreenProperties CreateOperationsScorecardReportProperties ()
        {
            TableReportBuildResult OperationsScorecardReportBuilder()
            {
                var scorecardReport = new DevOpsOperationsScorecard(gameFile, roundScores);

                return new TableReportBuildResult
                {
                    ReportFilename = scorecardReport.CreateReportAndFilename(),
                    PreferredTableHeight = scorecardReport.Height
                };
            }
            
            return new ReportScreenProperties
            {
                ContentCreator = CreateTableContentCreator(OperationsScorecardReportBuilder)
            };
        }

        ReportScreenProperties CreateIncidentReportProperties ()
        {
            string IncidentReportBuilder(int round, string business)
            {
                var report = new IncidentGanttReport(business, true);

                return ! roundScores.Any() ? "" : report.BuildReport(gameFile, round, true, roundScores[round - 1]);
            }

            return new ReportScreenProperties
            {
                ContentCreator = CreateCloudTimeChartContentCreator(IncidentReportBuilder),
                RoundComboBox = DevOpsComboBoxBuilder.CreateRoundComboBox(gameFile),
                BusinessComboBox = DevOpsComboBoxBuilder.CreateBusinessComboBox(gameFile, true)
            };
        }
        
        ReportScreenProperties CreateCpuReportProperties ()
        {
            SharedMouseEventControl ContentCreator (ReportScreen screen)
            {
                var reportBuilders = new List<GroupedBarChartReportHandler>
                {
                    (game, round) => new CpuUsageReport(gameFile, round).BuildReport(),
                    (game, round) => new ServerBladeUsageReport(game, round).BuildReport()
                };

                return new CombinedGroupBarCharts(gameFile, screen.SelectedRound, reportBuilders);
            }

            return new ReportScreenProperties
            {
                ContentCreator = ContentCreator,
                RoundComboBox = DevOpsComboBoxBuilder.CreateRoundComboBox(gameFile)
            };
        }
        
        ReportScreenProperties CreateNetworkReportProperties ()
        {
            SharedMouseEventControl ContentCreator (ReportScreen screen)
            {
                var networkReport = new NetworkReport(gameFile, gameFile.NetworkModel, gameFile.CurrentRound);

	            return new GridGroupedBoxChart(BasicXmlDocument.CreateFromFile(networkReport.BuildReport())
		            .DocumentElement, (xml) => new NetworkEnclosurePanel(xml), "Enclosures");
            }

            return new ReportScreenProperties
            {
                ContentCreator = ContentCreator,
                ReclaimFromReservedHeight = 30
            };
        }

	    ReportScreenProperties CreateComparisonReportProperties ()
	    {
		    SharedMouseEventControl ContentCreator (ReportScreen screen)
		    {
			    var report = new NpsSurveyReport(gameFile, "nps_survey_wizard.xml");

			    var reportFile = report.BuildReport();

			    if (! File.Exists(reportFile))
			    {
				    return null;
			    }

			    var xml = BasicXmlDocument.CreateFromFile(reportFile);

			    return new GroupedBarChart(xml.DocumentElement)
			    {
				    XAxisHeight = 50,
				    YAxisWidth = 35,
				    LegendX = 100,
				    LegendY = 20,
				    LegendHeight = 50,
				    BarPadding = 15,
				    GroupMargin = 30
			    };
		    }

		    return new ReportScreenProperties
		    {
			    ContentCreator = ContentCreator,
			    PreferredContentSizeFunc = size => new Size(size.Width, (int) (size.Height * 0.75f))
		    };
	    }

	    ReportScreenProperties CreateCsatReportProperties ()
        {
            SharedMouseEventControl ContentCreator (ReportScreen screen)
            {
	            return null;
            }

            return new ReportScreenProperties
            {
                ContentCreator = ContentCreator,
                PreferredContentSizeFunc = size => new Size(size.Width, (int)(size.Height * 0.75f))
            };
        }

        ReportScreenProperties CreateIntentReportProperties ()
        {
            SharedMouseEventControl ContentCreator (ReportScreen screen)
            {
                var issuesImage = Repository.TheInstance.GetImage(
                    $@"{AppInfo.TheInstance.Location}\images\intent\Intent_Issues_R{screen.SelectedRound}.png");
                var actionsImage = Repository.TheInstance.GetImage(
                    $@"{AppInfo.TheInstance.Location}\images\intent\Intent_Actions_R{screen.SelectedRound}.png");

                return new IntentImagePanel(issuesImage, actionsImage);
            }
            
            return new ReportScreenProperties
            {
                ContentCreator = ContentCreator,
                RoundComboBox = DevOpsComboBoxBuilder.CreateRoundComboBox(gameFile, false),
                PreferredContentSizeFunc = size => new Size(1004,563), // TODO hardcoded because the ordering of instantiation was being a pain
                IsSelectedRoundValidFunc = () => true
            };
        }
        
        ReportScreenProperties CreateCustomReportProperties ()
        {
            SharedMouseEventControl ContentCreator (ReportScreen screen)
            {
                var revealablePanel = new RevealablePanel();

                revealablePanel.LoadImages(gameFile.GetCustomContentFilename($"report_round_{screen.SelectedRound}"),
                    gameFile.GetCustomContentFilename($"report_round_{screen.SelectedRound}_revealed"));

                return revealablePanel;
            }

            return new ReportScreenProperties
            {
                ContentCreator = ContentCreator,
                RoundComboBox = DevOpsComboBoxBuilder.CreateRoundComboBox(gameFile)
            };
        }

        static CreateContentHandler CreateTableContentCreator(TableReportBuilderHandler reportBuilder)
        {
            SharedMouseEventControl TableContentCreator(ReportScreen reportScreen)
            {
                return new TablePanel(reportBuilder.Invoke().ReportFilename);
            }

            return TableContentCreator;
        }

        static CreateContentHandler CreateCloudTimeChartContentCreator(
            CloudTimeChartReportBuilderHandler reportBuilder)
        {
            SharedMouseEventControl CloudTimeChartCreator(ReportScreen reportScreen)
            {
                var reportFilePath = reportBuilder.Invoke(reportScreen.SelectedRound, reportScreen.SelectedBusiness);

                return ! File.Exists(reportFilePath) ? null : new CloudTimeChartWithLegend(BasicXmlDocument.CreateFromFile(reportFilePath).DocumentElement);
            }

            return CloudTimeChartCreator;
        }

        void OnRoundScoresChanged ()
        {
            RoundScoresChanged?.Invoke(this, new EventArgs<List<DevOpsRoundScores>>(roundScores));
        }

        void tabBar_TabPressed(object sender, TabBarEventArgs args)
        {
            ShowPanel(args.Code);
	        if (tabBarChangedLocally)
	        {
		        OnReportTabChanged();
	        }

	        tabBarChangedLocally = true;
        }

        readonly Dictionary<ReportsTabOrder, ReportScreenProperties> reportTabToScreenProperties;

        ReportsTabOrder currentTab;

        Rectangle topBarBounds;
        Rectangle bottomBarBounds;

        readonly ReportScreen reportScreen;
        
        readonly NetworkProgressionGameFile gameFile;
        readonly SupportSpendOverrides spendOverrides;
        readonly TabBar tabBar;

        List<DevOpsRoundScores> roundScores;

        readonly bool includeBars;
	    public override IList<ChartScreenTabOption> GetAllAvailableReports ()
	    {
		    var businesses = new List<Node>((Node[]) gameFile.NetworkModel.GetNodesWithAttributeValue("type", "BU").ToArray(typeof(Node)));
		    businesses.Insert(0, null);
		    var rounds = gameFile.LastRoundPlayed;

		    var results = new List<ChartScreenTabOption>();

		    results.Add(new ChartScreenTabOption { Tab = tabBar.GetTabByCode((int) ReportsTabOrder.BusinessTab), Name = "Business", Business = null, Round = null });
		    results.Add(new ChartScreenTabOption { Tab = tabBar.GetTabByCode((int) ReportsTabOrder.Operations), Name = "Operations", Business = null, Round = null });
		    results.Add(new ChartScreenTabOption { Tab = tabBar.GetTabByCode((int) ReportsTabOrder.Css), Name = "NPS", Business = null, Round = null });
		    results.Add(new ChartScreenTabOption { Tab = tabBar.GetTabByCode((int) ReportsTabOrder.Network), Name = "Network", Business = null, Round = null });

		    for (var round = 1; round <= rounds; round++)
		    {
			    results.Add(new ChartScreenTabOption { Tab = tabBar.GetTabByCode((int) ReportsTabOrder.CpuReport), Name = $"CPU_Round_{round}", Business = null, Round = round });
			    results.Add(new ChartScreenTabOption { Tab = tabBar.GetTabByCode((int) ReportsTabOrder.Maturity), Name = $"Maturity_Round_{round}", Business = null, Round = round });
			    results.Add(new ChartScreenTabOption { Tab = tabBar.GetTabByCode((int) ReportsTabOrder.ProductQuality), Name = $"ProductQuality_Round_{round}", Business = null, Round = round });

			    foreach (var business in businesses)
			    {
				    var businessName = business?.GetAttribute("name");
				    var displayBusinessName = businessName ?? "All BUs";
				    results.Add(new ChartScreenTabOption { Tab = tabBar.GetTabByCode((int) ReportsTabOrder.NewApps), Name = $"NewServices_{displayBusinessName}_Round_{round}", Business = businessName, Round = round });
				    results.Add(new ChartScreenTabOption { Tab = tabBar.GetTabByCode((int) ReportsTabOrder.Incidents), Name = $"Incidents_{displayBusinessName}_Round_{round}", Business = businessName, Round = round });
			    }
		    }

		    return results;

	    }

	    public override void ShowReport (ChartScreenTabOption report)
	    {
		    tabBar.SelectedTabCode = report.Tab.code;

			OnReportSelectedBusinessChanged(new ReadonlyEventArgs<string> (report.Business));

			if (report.Round.HasValue)
			{
				OnReportSelectedRoundChanged(new ReadonlyEventArgs<int> (report.Round.Value));
			}
	    }
	}
}