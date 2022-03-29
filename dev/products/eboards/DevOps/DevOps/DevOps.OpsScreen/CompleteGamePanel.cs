using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

using ChartScreens;
using CommonGUI;
using CoreScreens;
using CoreUtils;
using DevOps.OpsEngine;
using DevOps.OpsScreen.SecondaryDisplay;
using GameDetails;
using GameManagement;
using LibCore;
using TransitionScreens;

using DevOps.ReportsScreen;
using Licensor;
using maturity_check;
using ReportBuilder;
using DevOpsRoundScores = DevOps.ReportsScreen.DevOpsRoundScores;

namespace DevOps.OpsScreen
{
	public class CompleteGamePanel : BaseCompleteGamePanel
	{
		OpsControlPanelBar opsControlBar;
		PureTabbedChartScreen chartScreen;
		TimerViewer timerViewer;
		TimeLine timeLine;
	    readonly Label roundNumber;

	    GameScreenPanel secondaryGameScreenPanel;
	    bool wasSecondaryDisplayMaximised;
		SecondaryDisplayForm secondaryDisplay;
	    ReportsScreenPanel secondaryReportsScreen;

        TradingOpsEngine opsEngine;

        public CompleteGamePanel (IProductLicence productLicence, IProductLicensor productLicensor)
	        : base(productLicence, productLicensor)
		{
			// Changing the Navigation buttons to match the PoleStar Images
			_loadButton.Size = new Size(37,30);
			_infoButton.Size = new Size(37,30);
			_raceButton.Size = new Size(37,30);
			_boardButton.Size = new Size(37,30);
			_reportsButton.Size = new Size(37,30);

			// Adjust the position of the navigation buttons
			_loadButton.Location = new Point(5 + 37 * 0, 5);
			_infoButton.Location = new Point(5 + 37 * 1, 5);
			_raceButton.Location = new Point(5 + 37 * 2, 5);
			_boardButton.Location = new Point(5 + 37 * 3, 5);
			_reportsButton.Location = new Point(5 + 37 * 4, 5);

			// Adjust the game control buttons
			gameControl.AdjustPosition(37, 33, 0);

            roundNumber = new Label()
            {
                Bounds = new Rectangle(335, 0, 100, 40),
				TextAlign = ContentAlignment.MiddleLeft,
				BackColor = Color.Transparent,
				ForeColor = Color.White,
                Font = SkinningDefs.TheInstance.GetFont(14)
			};

			WindowDraggingExtensions.EnableDragging();
        }

		SecondaryDisplayForm CreateSecondaryDisplay (NetworkProgressionGameFile gameFile)
		{
			var secondaryScreen = Screen.AllScreens[0];

			if (CCDWrapper.GetDisplayConfigBufferSizes(CCDWrapper.QueryDisplayFlags.OnlyActivePaths,
				    out var numPathArrayElements, out var numModeInfoArrayElements) == 0)
			{
				var pathInfoArray = new CCDWrapper.DisplayConfigPathInfo[numPathArrayElements];
				var modeInfoArray = new CCDWrapper.DisplayConfigModeInfo[numModeInfoArrayElements];

				CCDWrapper.QueryDisplayConfig(CCDWrapper.QueryDisplayFlags.DatabaseCurrent,
					ref numPathArrayElements, pathInfoArray, ref numModeInfoArrayElements, modeInfoArray,
					out var currentTopologyId);

				if ((Screen.AllScreens.Length > 1)
				    && (currentTopologyId == CCDWrapper.DisplayConfigTopologyId.Extend))
				{
					secondaryScreen = Screen.AllScreens[1];
				}
			}

			var secondaryDisplay = new SecondaryDisplayForm(gameFile)
            {
				StartPosition = FormStartPosition.Manual,
				ShowInTaskbar = false,
				MinimumSize = new Size(1024, 768),
				Size = new Size(secondaryScreen.Bounds.Width, Math.Min(768, secondaryScreen.Bounds.Height)),
				Location = secondaryScreen.Bounds.Location,
				Icon = Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetEntryAssembly().Location),
				Text = Application.ProductName + " (Public)"
			};
			secondaryDisplay.Hide();
			TopLevelControl.Text = Application.ProductName + " (Facilitator)";

			return secondaryDisplay;
		}

		protected override TransitionScreen BuildTranisitionScreen(int round)
		{
			return null;
		}

		protected override EditGamePanel CreateGameDetailsScreen()
		{
			EditGamePanel egp = new DevOps_EditGamePanel(gameFile, this, supportOverrides);
			egp.ShowTeamName(false);

			egp.ReportsInvalidated += egp_ReportsInvalidated;

			return egp;
		}

		void egp_ReportsInvalidated (object sender, EventArgs args)
		{
			if (null != this.chartsScreen)
			{
				LibCore.WinUtils.Hide(chartsScreen);
				chartsScreen.Dispose();
				chartsScreen = null;
			}
		}

		protected override void DisposeGameScreen ()
		{
			if (opsEngine != null)
			{
				opsEngine.Dispose();
				opsEngine = null;
			}

			if (raceScreen != null)
			{
				Controls.Remove(raceScreen);
				raceScreen.Dispose();
				raceScreen = null;
			}
		}

		protected override OpsPhaseScreen CreateOpsPhaseScreen(NetworkProgressionGameFile gameFile, bool isTrainingGame, string gameDir)
		{
            opsControlBar = new OpsControlPanelBar(gameFile.NetworkModel)
							{
								Enabled = true
							};

			Controls.Add(opsControlBar);
			opsControlBar.BringToFront();

			var gameIncidentPanel = new DevOpsQuadStatusLozengeGroup (gameFile.NetworkModel);

			opsEngine = new TradingOpsEngine (gameFile.NetworkModel, gameFile,
				AppInfo.TheInstance.Location + $@"\data\incidents_r{gameFile.CurrentRound}.xml",
				gameFile.CurrentRound, !gameFile.IsSalesGame, gameIncidentPanel);

			if (PersistentGlobalOptions.UseMultipleScreens)
			{
				secondaryGameScreenPanel = new GameScreenPanel (gameFile, gameIncidentPanel, opsEngine, isTrainingGame)
				{
                    BackColor = Color.White
				};

			    if (secondaryDisplay == null)
			    {
			        secondaryDisplay = CreateSecondaryDisplay(gameFile);
			    }
			    else
			    {
				    secondaryDisplay.GameFile = gameFile;
			    }
			
				return new FacilitatorOpsScreen (gameFile, opsControlBar, opsEngine, secondaryGameScreenPanel);
			}
			else
			{
				TopLevelControl.Text = Application.ProductName;

				return new TradingOpsScreen (gameFile, isTrainingGame, opsControlBar, gameDir, opsEngine);
			}
		}

		protected override PureTabbedChartScreen CreateChartScreen()
		{
		    if (PersistentGlobalOptions.UseMultipleScreens)
		    {
		        secondaryReportsScreen =
		            new ReportsScreenPanel(gameFile, supportOverrides, false);

		        if (secondaryDisplay == null)
		        {
		            secondaryDisplay = CreateSecondaryDisplay(gameFile);
		        }
		        else
		        {
			        secondaryDisplay.GameFile = gameFile;
		        }

				var primaryReportsScreen =
		            new ReportsScreenPanel(gameFile, supportOverrides, false)
		            {
		                LinkedReportsScreen = secondaryReportsScreen
                    };

		        secondaryReportsScreen.LinkedReportsScreen = primaryReportsScreen;
                
		        chartScreen = primaryReportsScreen;
		    }
		    else
		    {
		        chartScreen = new ChartScreen(gameFile, supportOverrides);
		    }

            return chartScreen;
		}

		protected override void raceScreen_PhaseFinished_Timer_Tick(object sender, EventArgs e)
		{
			base.raceScreen_PhaseFinished_Timer_Tick(sender,e);

			raceScreen.Pause();
		}

		protected override void CreateToolsScreen ()
		{
            toolsScreen = new DevOpsToolsScreen(gameFile, this, false, supportOverrides, false);
			toolsScreen.RemoveTabByName("Board View");
			toolsScreen.RemoveTabByName("Support Costs");

			Controls.Add(toolsScreen);
		}

	    protected override void OnParentChanged(EventArgs e)
	    {
	        ((Form)TopLevelControl)?.AddOwnedForm(secondaryDisplay);
	    }

	    protected override void Dispose (bool disposing)
	    {
            if (disposing)
            {
                if (TopLevelControl != null && secondaryDisplay != null)
	            {
                    ((Form)TopLevelControl).RemoveOwnedForm(secondaryDisplay);
	            }

                secondaryDisplay?.Dispose();
            }

	        base.Dispose(disposing);
	    }

	    void ManageSecondaryDisplay (ViewScreen value)
	    {
	        if (secondaryDisplay == null) return;

			switch (value)
			{
				case ViewScreen.RACING_SCREEN:
					secondaryDisplay.ShowGameScreen(secondaryGameScreenPanel);
					break;
				case ViewScreen.REPORT_SCREEN:
					secondaryDisplay.ShowReportScreen(secondaryReportsScreen);
					break;
				case ViewScreen.GAME_SELECTION_SCREEN:
					secondaryDisplay?.Dispose();
					secondaryDisplay = null;
					return;
				default:
					HideSecondaryDisplay();
					return;
			}

			secondaryDisplay.Show();
	        secondaryDisplay.ShowInTaskbar = true;

	        if (wasSecondaryDisplayMaximised)
	        {
	            secondaryDisplay.WindowState = FormWindowState.Maximized;
	        }
	    }

	    void HideSecondaryDisplay ()
	    {
	        if (secondaryDisplay == null)
	        {
	            return;
	        }

	        // GDC: If the form is maximised it's not hidden for some reason,
	        // so forcing its WindowState to be Normal.

	        wasSecondaryDisplayMaximised = secondaryDisplay.WindowState == FormWindowState.Maximized;

	        secondaryDisplay.WindowState = FormWindowState.Normal;
	        secondaryDisplay.Hide();
	        secondaryDisplay.ShowInTaskbar = false;
        }

        protected override void SetCurrentView(ViewScreen value)
		{
			base.SetCurrentView(value);

			ManageSecondaryDisplay(value);
			
            if (_CurrentView == ViewScreen.RACING_SCREEN)
			{
				if (opsControlBar != null)
				{
					Controls.Add(opsControlBar);
					opsControlBar.BringToFront();
				}

				if (roundNumber.Parent == null)
				{
					roundNumber.Text = "Round " + gameFile.NetworkModel.GetNamedNode("RoundVariables")
						                   .GetIntAttribute("current_round", 0);
					roundNumber.ForeColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("timer_text_colour", Color.White);
					Controls.Add(roundNumber);
					roundNumber.BringToFront();
				}

				if (timeLine != null)
                {
                    Controls.Add(timeLine);
                    timeLine.BringToFront();
                }

				timerViewer?.Show();
            }
			else
			{
			    if (opsControlBar != null)
				{
					Controls.Remove(opsControlBar);
				}

				if (roundNumber != null)
				{
					Controls.Remove(roundNumber);
				}

				timerViewer?.Hide();

				if (timeLine != null)
                {
                    Controls.Remove(timeLine);
                }

				if ((value == ViewScreen.GAME_SELECTION_SCREEN) || (value == ViewScreen.GAME_DETAILS_SCREEN))
				{
					EmptyScreens();
				}
			}

			Invalidate();
		}

		protected override void EmptyScreens()
		{
			base.EmptyScreens();

			if (opsControlBar != null)
			{
				Controls.Remove(opsControlBar);
				opsControlBar.Dispose();
				opsControlBar = null;
			}
			if (timerViewer != null)
			{
				Controls.Remove(timerViewer);
				timerViewer.Dispose();
				timerViewer = null;
			}
            

			if (timeLine != null)
			{
				Controls.Remove(timeLine);
				timeLine.Dispose();
				timeLine = null;
			}
		}

		protected override void CreateOpsBanner(string phaseName, bool showDay, bool isRaceView)
		{
			timerViewer = new TimerViewer(gameFile.NetworkModel)
						  {
							  BackColor = Color.Transparent
						  };
			Controls.Add(timerViewer);
			timeLine = new TimeLine(gameFile.NetworkModel)
					   {
						   BackColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("timer_panel_back_colour", Color.Black)
					   };
			Controls.Add(timeLine);
			timeLine.BringToFront();
		}

		public override void ClearToolTips ()
		{
			DiscreteSimGUI.ItilToolTip_Quad.TheInstance.Hide();
		}

		protected override GameSelectionScreen CreateGameSelectionScreen (IGameLoader gameLoader,
		                                                                  IProductLicence productLicence,
		                                                                  IProductLicensor productLicensor)
		{
			var gameSelectionScreen = new DevOpsGameSelectionScreen(gameLoader, productLicence, productLicensor);

			gameSelectionScreen.MaturityEditorCreator = (() => new MaturityEditorForm());

			return gameSelectionScreen;
		}

		protected override void DoSize ()
		{
			base.DoSize();

			if (gameControl != null)
			{
				gameControl.Height = 33;
				gameControl.Location = new Point(ClientSize.Width - gameControl.Width, ClientSize.Height - 4 - gameControl.Height);
			}

			if (teamLabel != null)
			{
				teamLabel.Hide();
			}

			if (timerViewer != null)
			{
				timerViewer.Bounds = new Rectangle (0, 0, Width, 40);
			}

			if (timeLine != null)
			{
				timeLine.Bounds = new Rectangle (0, timerViewer?.Bottom ?? 40, Width, SkinningDefs.TheInstance.GetSizeData("time_line_panel_size", 1024, 5).Height);
			}

			if (opsControlBar != null)
			{
				opsControlBar.Bounds = new Rectangle (0, Height - 40, gameControl.Left, 40);
			}

			Invalidate();
		}

		protected override void OnPaint (PaintEventArgs e)
		{
			var margin = 20;

			var left = GetNavigationButtons().Max(a => a.Right) + margin;
			var right = GetWindowButtons().Min(a => a.Left) - margin;

			var topBarBounds = new Rectangle(0, 0, Width, 40);
			var bottomBarBounds = new Rectangle(0, Height - 40, Width, 40);

			var colour = Color.FromArgb(37, 37, 37);
			var poweredByName = "low_poweredby_logo.png";
			if (_isTrainingGame)
			{
				colour = Color.FromArgb(255, 172, 0);
				poweredByName = "t_low_poweredby_logo.png";
			}

			using (var brush = new SolidBrush (colour))
			{
				e.Graphics.FillRectangle(brush, topBarBounds);
				e.Graphics.FillRectangle(brush, bottomBarBounds);
			}

			var poweredByImage = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + @"\images\panels\" + poweredByName);
			e.Graphics.DrawImage(poweredByImage, new Rectangle (bottomBarBounds.Left + 150, bottomBarBounds.Top + (bottomBarBounds.Height - poweredByImage.Height) / 2, poweredByImage.Width, poweredByImage.Height));

			var logoImage = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + @"\images\panels\top_devops_logo.png");
			e.Graphics.DrawImage(logoImage, new Rectangle(right - logoImage.Width, topBarBounds.Top + (topBarBounds.Height - logoImage.Height) / 2, logoImage.Width, logoImage.Height));

			if (_isTrainingGame)
			{
				var trainingImage = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + @"\images\panels\t_trainingmode.png");
				e.Graphics.DrawImage(trainingImage, new Rectangle(gameControl.Left - 10 - trainingImage.Width, bottomBarBounds.Top + (bottomBarBounds.Height - trainingImage.Height) / 2, trainingImage.Width, trainingImage.Height));
			}
		}

		public override List<RoundScores> GetRoundScores ()
		{
			var roundScores = new List<DevOpsRoundScores>();

			var previousProfit = 0;
			var newServices = 0;
			var previousRevenue = 0;
			for (var i = 1; i <= gameFile.LastRoundPlayed; i++)
			{
				var scores = new DevOpsRoundScores (gameFile, i, previousProfit, newServices, previousRevenue, supportOverrides);
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

			return roundScores.ToList<RoundScores>();
		}

		public override void GenerateAllReports (string folder)
		{
			var roundScores = new List<DevOpsRoundScores> ();
			foreach (var roundScore in GetRoundScores())
			{
				roundScores.Add((DevOpsRoundScores) roundScore);
			}

			var businessReportBuilder = new DevOpsBusinessScorecard (gameFile, roundScores);
			var processScoresReportBuilder = new OpsProcessScoresReport ();
			var maturityReportBuilder = new OpsMaturityReport ();
			var productReportBuilder = new ProductQualityReportBuilder (gameFile);
			var opsReportBuilder = new DevOpsOperationsScorecard (gameFile, roundScores);
			var customerSatisfactionReportBuilder = new NpsSurveyReport (gameFile, "nps_survey_wizard.xml");

			File.Copy(businessReportBuilder.BuildReport(), folder + @"\BusinessReport.xml", true);
			File.Copy(opsReportBuilder.BuildReport(), folder + @"\OperationsReport.xml", true);
			File.Copy(customerSatisfactionReportBuilder.BuildReport(), folder + $@"\NpsReport.xml", true);

			var processScoreCards = new List<string>();
			for (int round = 1; round <= gameFile.LastRoundPlayed; round++)
			{
				File.Copy(maturityReportBuilder.BuildReport(gameFile, round, new ArrayList (roundScores)), folder + @"\MaturityReport_Round{round}.xml", true);
				File.Copy(productReportBuilder.BuildReport(round), folder + @"\ProductReport_Round{round}.xml", true);

				var cpuReportBuilder = new CpuUsageReport(gameFile, round);
				File.Copy(cpuReportBuilder.BuildReport(), folder + $@"\CpuReport_Round{round}.xml", true);

				var networkReportBuilder = new ReportsScreen.NetworkReport(gameFile, gameFile.GetNetworkModel(round), round);
				File.Copy(networkReportBuilder.BuildReport(), folder + $@"\NetworkReport_Round{round}.xml", true);

				for (int business = 0; business <= 4; business++)
				{
					var businessLabel = SkinningDefs.TheInstance.GetData("biz");
					var businessTag = "All";
					var businessName = SkinningDefs.TheInstance.GetData("allbiz");
					if (business > 0)
					{
						businessName = CONVERT.Format("{0} {1}", businessLabel, business);
						businessTag = CONVERT.ToStr(business);
					}

					var appsReportBuilder = new NewServicesReport (gameFile, round);
					File.Copy(appsReportBuilder.BuildReport(businessName, true, true, false), folder + $@"\AppsReport_Round{round}_{businessLabel}{businessTag}.xml", true);

					var incidentReportBuilder = new IncidentGanttReport (businessName);
					File.Copy(incidentReportBuilder.BuildReport(gameFile, round, true, roundScores[round - 1]), folder + $@"\IncidentReport_Round{round}_{businessLabel}{businessTag}.xml", true);
				}

				if (round <= roundScores.Count)
				{
					processScoreCards.Add(processScoresReportBuilder.BuildReport(gameFile, round, roundScores[round - 1]));
				}
			}

			int height;
			File.Copy(processScoresReportBuilder.CombineRoundResults(processScoreCards.ToArray(), gameFile, gameFile.LastRoundPlayed, roundScores[gameFile.LastRoundPlayed - 1], out height),
				folder + @"\ProcessScores.xml", true);

			for (int round = 1; round <= gameFile.LastRoundPlayed; round++)
			{
				File.Copy(maturityReportBuilder.BuildReport(gameFile, round, new ArrayList(roundScores)),
					folder + $@"\MaturityReport_Round{round}.xml", true);
			}
		}
	}
}