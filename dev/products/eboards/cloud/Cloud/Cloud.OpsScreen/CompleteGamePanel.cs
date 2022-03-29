using System;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;
using Algorithms;
using GameManagement;
using TransitionScreens;
using LibCore;
using ChartScreens;
using CoreUtils;
using CommonGUI;
using GameDetails;
using CoreScreens;
using Cloud.ReportsScreen;
using Licensor;
using maturity_check;
using ReportBuilder;

namespace Cloud.OpsScreen
{
	public class CompleteGamePanel : BaseCompleteGamePanel
	{
		Image poweredByLogo;
		Image trainingLogo;
		Image cloudLogo;

		TradingOpsScreen top;

		public CompleteGamePanel (IProductLicence productLicence, IProductLicensor productLicensor)
			: base(productLicence, productLicensor)
		{
			poweredByLogo = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\panels\\low_poweredby_logo.png");
			trainingLogo = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\panels\\t_trainingmode.png");
			cloudLogo = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\panels\\cloud_logo.png");

			//Changing the Navigation buttons to match the PoleStar Images
			_loadButton.Size = new Size(37,30);
			_infoButton.Size = new Size(37,30);
			_raceButton.Size = new Size(37,30);
			_boardButton.Size = new Size(37,30);
			_reportsButton.Size = new Size(37,30);

			//Adjust the position to butt the buttons together
			_loadButton.Left = 8 + 37*0;
			_infoButton.Left = 8 + 37*1;
			_raceButton.Left = 8 + 37*2;
			_boardButton.Left = 8 + 37*3;
			_reportsButton.Left = 8 + 37*4;

			_loadButton.Top = 5;
			_infoButton.Top = 5;
			_raceButton.Top = 5;
			_boardButton.Top = 5;
			_reportsButton.Top = 5;

			//Adjust the Screen Banner position
			screenBannerLeft = 240;
			screenBannerTop = 8;
			pageTitle.Left = pageTitle.Left + 20;
			
			gameControl.AdjustPositionAndWidthResize(37,33,1);

			// Our base constructor may have done things to the first reports button before
			// the second one was created, so sync the status now.
			EnableReportsButton(_reportsButton.Enabled);

			gameControl.BringToFront();
		}

		protected override void CreateGameControl()
		{
			gameControl = new GlassGameControl();
			gameControl.Location = new Point(830, 728);
			gameControl.Size = new Size(153, 33);
			gameControl.BackColor = Color.FromArgb(50, 55, 62);
			gameControl.Name = "Game Play/Pause/etc Buttons";
			Controls.Add(gameControl);
		}

		protected override void CreateOpsBanner(string DefaultPhaseName, bool showDay, bool isRaceScreen)
		{
			screenBanner = new CloudOpsScreenBanner (gameFile.NetworkModel, false);
			screenBanner.Round = gameFile.CurrentRound;
			//screenBanner.Location = new Point(screenBannerLeft,screenBannerTop);
			screenBanner.Location = new Point(260,screenBannerTop);
			screenBanner.Size = new Size(570-80,28);
			screenBanner.ChangeBannerTextForeColour(Color.Black);
			screenBanner.ChangeBannerPrePlayTextForeColour(Color.DarkRed);
			screenBanner.SetRaceViewOn(isRaceScreen);
			screenBanner.Phase = DefaultPhaseName;
			screenBanner.Name = "Ops Screen Banner";
			Controls.Add(screenBanner);
			screenBanner.BringToFront();
		}

		public override void EnableReportsButton (bool enable)
		{
			_reportsButton.Enabled = enable;
		}

		protected override TransitionScreen BuildTranisitionScreen(int round)
		{
			return null;
		}

		protected override EditGamePanel CreateGameDetailsScreen()
		{
			var egp = new PS_EditGamePanel (gameFile, this);
			egp.ReportsInvalidated += egp_ReportsInvalidated;
			return egp;
		}


		void egp_ReportsInvalidated (object sender, EventArgs args)
		{
			if (null != chartsScreen)
			{
				WinUtils.Hide(chartsScreen);
				chartsScreen.Dispose();
				chartsScreen = null;
			}
		}

		protected override OpsPhaseScreen CreateOpsPhaseScreen(NetworkProgressionGameFile gameFile, bool isTrainingGame, string gameDir)
		{
			top = new TradingOpsScreen(this, gameFile, isTrainingGame);

			top.SetGameControl(gameControl);
			top.Top = 40;
			return top;
		}

		protected override void DisposeOpsScreen ()
		{
			if (top != null)
			{
				top.Dispose();
			}

			top = null;
		}

		public void top_ModalAction(bool entering)
		{
			SuspendMainNavigation(entering);
			gameControl.SuspendButtonsForModal(entering);
		}

		protected override PureTabbedChartScreen CreateChartScreen ()
		{
			return new ChartScreen (gameFile, supportOverrides, new ChartScreen.GameScreenCreator (CreateGameScreenReport));
		}

		Control CreateGameScreenReport (NetworkProgressionGameFile gameFile)
		{
			if (top == null)
			{
				Panel p = new Panel();
				p.BackColor = Color.Blue;
				return p;
			}
			else
			{
				return new TradingOpsScreen(this, gameFile, top.OpsEngine, false);
			}
		}

		protected override BaseTabbedChartScreen CreateITChartScreen ()
		{
			return null;
		}

		/// <summary>
		/// This is used when the operations panel needs to performa a modal operation
		/// so we need to suspend the navigational buttons
		/// </summary>
		/// <param name="isSuspending"></param>
		public void SuspendMainNavigation(bool isSuspending)
		{
			if (isSuspending)
			{
				_loadButton.Tag = _loadButton.Enabled;
				_infoButton.Tag = _infoButton.Enabled;
				_raceButton.Tag = _raceButton.Enabled;
				_boardButton.Tag = _boardButton.Enabled;
				_reportsButton.Tag = _reportsButton.Enabled;
				_minButton.Tag = _minButton.Enabled;
				_closeButton.Tag = _closeButton.Enabled;

				_loadButton.Enabled = false;
				_infoButton.Enabled = false;
				_raceButton.Enabled = false;
				_boardButton.Enabled = false;
				_reportsButton.Enabled = false;
				_minButton.Enabled = false;
				_closeButton.Enabled = false;
			}
			else
			{
				if (_loadButton.Tag != null)
				{
					_loadButton.Enabled = (bool)_loadButton.Tag;
				}
				if (_infoButton.Tag != null)
				{
					_infoButton.Enabled = (bool)_infoButton.Tag;
				}
				if (_raceButton.Tag != null)
				{
					_raceButton.Enabled = (bool)_raceButton.Tag;
				}
				if (_boardButton.Tag != null)
				{
					_boardButton.Enabled = (bool)_boardButton.Tag;
				}
				if (_reportsButton.Tag != null)
				{
					_reportsButton.Enabled = (bool)_reportsButton.Tag;
				}
				if (_minButton.Tag != null)
				{
					_minButton.Enabled = (bool)_minButton.Tag;
				}
				if (_closeButton.Tag != null)
				{
					_closeButton.Enabled = (bool)_closeButton.Tag;
				}
			}
		}

		protected override void SizeChartScreen (Control screen)
		{
			screen.Location = new Point(0,50-10);
			//screen.Size = new Size (Width - screen.Left, Height - 40 - screen.Top);
			screen.Size = new Size(Width - screen.Left, Height - screen.Top);
		}

		protected override void handleRaceScreenHasGameStarted()
		{
			//Do we need do anything with the 
			setCityChoiceConfirmed();
			base.handleRaceScreenHasGameStarted();
		}

		protected override void clearOtherDataFiles(int round, string current_round_dir)
		{
			handleClearOnRewind(round);
		}

		protected void handleClearOnRewind(int round)
		{
			bool isSalesGame = DetermineGameSales();
			bool isTrainingGame = DetermineGameTraining();
			bool isNormalGame = (isSalesGame == false) & (isTrainingGame == false);

			if (round == 1)
			{
				if ((isNormalGame) | (isTrainingGame))
				{
					gameFile.SetGlobalOption("city_choice_confirmed", false);
				}
			}
		}

		protected void setCityChoiceConfirmed()
		{
			bool isSalesGame = DetermineGameSales();
			bool isTrainingGame = DetermineGameTraining();
			bool isNormalGame = (isSalesGame == false) & (isTrainingGame == false);

			if ((isNormalGame) | (isTrainingGame))
			{
				gameFile.SetGlobalOption("city_choice_confirmed", true);
			}
		}

		/// <summary>
		/// Override on Paint
		/// </summary>
		/// <param name="e"></param>
		protected override void OnPaint(PaintEventArgs e)
		{
			var leading = 5;
			var margin = 10;

			var topBarBounds = new Rectangle (0, 0, Width, 40);
			var cloudLogoBounds = new RectangleFromBounds
			{
				Right = _minButton.Left - margin,
				Top = topBarBounds.Top + leading,
				Bottom = topBarBounds.Bottom - leading,
				Width = Width
			}.ToRectangle();
			var cloudLogoWidth = cloudLogo.Width * cloudLogoBounds.Height / cloudLogo.Height;
			cloudLogoBounds = new Rectangle (cloudLogoBounds.Right - cloudLogoWidth, cloudLogoBounds.Top, cloudLogoWidth, cloudLogoBounds.Height);

			bool showBottomBar;
			switch (CurrentView)
			{
				case ViewScreen.REPORT_SCREEN:
					showBottomBar = false;
					break;

				case ViewScreen.GAME_DETAILS_SCREEN:
				case ViewScreen.GAME_SELECTION_SCREEN:
				case ViewScreen.NETWORK_SCREEN:
				case ViewScreen.RACING_SCREEN:
				default:
					showBottomBar = true;
					break;
			}

			var bottomBarBounds = new Rectangle (0, Height - 40, Width, 40);

			var topBarColour = Color.FromArgb(50, 55, 62);
			var bottomBarColour = Color.FromArgb(229, 229, 229);
			if (_isTrainingGame)
			{
				bottomBarColour = Color.FromArgb(230, 84, 0);
			}

			using (var brush = new SolidBrush (topBarColour))
			{
				e.Graphics.FillRectangle(brush, topBarBounds);
			}

			e.Graphics.DrawImage(cloudLogo, cloudLogoBounds);

			if (showBottomBar)
			{
				using (var brush = new SolidBrush(bottomBarColour))
				{
					e.Graphics.FillRectangle(brush, bottomBarBounds);
				}

				var poweredByLeft = 0;
				if ((top != null)
				    && (top.ControlPanel?.Visible ?? false))
				{
					poweredByLeft = top.ControlPanel.Right;
				}

				var poweredByBounds = new RectangleFromBounds
				{
					Left = poweredByLeft + margin,
					Top = bottomBarBounds.Top + leading,
					Right = gameControl.Left - margin,
					Bottom = bottomBarBounds.Bottom - leading
				}.ToRectangle();
				var scaleFactor = Math.Min(poweredByBounds.Height * 1.0f / poweredByLogo.Height, poweredByBounds.Width * 1.0f / poweredByLogo.Width);
				var imageSize = new Size ((int) (poweredByLogo.Width * scaleFactor), (int) (poweredByLogo.Height * scaleFactor));
				poweredByBounds = new Rectangle (poweredByLeft + margin, bottomBarBounds.Top + ((bottomBarBounds.Height - imageSize.Height) / 2), imageSize.Width, imageSize.Height);

				e.Graphics.DrawImage(poweredByLogo, poweredByBounds);
			}

			top?.setControlPanelVisible((CurrentView == ViewScreen.RACING_SCREEN));
		}

		public override void ClearToolTips ()
		{
			DiscreteSimGUI.ItilToolTip_Quad.TheInstance.Hide();
		}

		protected override void DoOperationsPreConnectWork(int round, bool rewind)
		{
			gameFile.SetCurrentRound(round, GameFile.GamePhase.OPERATIONS, rewind);
		}

		protected override GameSelectionScreen CreateGameSelectionScreen (IGameLoader gameLoader, IProductLicence productLicence,
		                                                                  IProductLicensor productLicensor)
		{
			var gameSelectionScreen = new Cloud_GameSelectionScreen (gameLoader, productLicence, productLicensor);

			gameSelectionScreen.MaturityEditorCreator = (() => new MaturityEditorForm());

			return gameSelectionScreen;
		}

		protected override void SetPlaying(bool playing)
		{
			if (playing)
			{
				setCityChoiceConfirmed();
			}
			base.SetPlaying(playing);
		}

		protected override void CreateToolsScreen()
		{
			toolsScreen = new CloudToolsScreen (gameFile, this, false, supportOverrides);
			SuspendLayout();
			Controls.Add(toolsScreen);
			ResumeLayout(false);
			DoSize();
		}

		protected override void DoSize()
		{
			base.DoSize();

			if (top != null)
			{
				top.Bounds = new Rectangle (0, 40, Width, Height - 80);
			}

			gameControl.AdjustPositionAndWidthResize(37, 33, 1);
			gameControl.Location = new Point (ClientSize.Width - gameControl.Width, ClientSize.Height - 4 - gameControl.Height);
			gameControl.BringToFront();

			if (chartsScreen != null)
			{
				chartsScreen.Bounds = new Rectangle(0, 40, Width, Height - 40);
			}

			Invalidate();
		}

		public override void GenerateAllReports (string folder)
		{
			var roundScores = new List<Cloud_RoundScores>();
			foreach (var roundScore in GetRoundScores())
			{
				roundScores.Add((Cloud_RoundScores) roundScore);
			}

//			var businessReportBuilder = new Bus(gameFile, roundScores, new[] { "All" });
			var processScoresReportBuilder = new OpsProcessScoresReport();
			var maturityReportBuilder = new OpsMaturityReport();
//			var productReportBuilder = new ProductQualityReportBuilder(gameFile);
//			var customerSatisfactionReportBuilder = new NpsSurveyReport(gameFile, "nps_survey_wizard.xml");

//			File.Copy(businessReportBuilder.BuildReport(), folder + @"\BusinessReport.xml", true);
//			File.Copy(customerSatisfactionReportBuilder.BuildReport(), folder + $@"\NpsReport.xml", true);

			var processScoreCards = new List<string>();
			for (int round = 1; round <= gameFile.LastRoundPlayed; round++)
			{
				File.Copy(maturityReportBuilder.BuildReport(gameFile, round, new ArrayList(roundScores)), folder + @"\MaturityReport_Round{round}.xml", true);
//				File.Copy(productReportBuilder.BuildReport(round), folder + @"\ProductReport_Round{round}.xml", true);

//				var cpuReportBuilder = new CpuUsageReport(gameFile, round);
//				File.Copy(cpuReportBuilder.BuildReport(), folder + $@"\CpuReport_Round{round}.xml", true);

//				var networkReportBuilder = new ReportsScreen.NetworkReport(gameFile, gameFile.GetNetworkModel(round), round);
//				File.Copy(networkReportBuilder.BuildReport(), folder + $@"\NetworkReport_Round{round}.xml", true);

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

					var appsReportBuilder = new NewServicesReport(gameFile, round);
//					File.Copy(appsReportBuilder.BuildReport(businessName, true, false, null), folder + $@"\AppsReport_Round{round}_{businessLabel}{businessTag}.xml", true);

//					var incidentReportBuilder = new IncidentGanttReport(businessName);
//					File.Copy(incidentReportBuilder.BuildReport(gameFile, round, true, roundScores[round - 1]), folder + $@"\IncidentReport_Round{round}_{businessLabel}{businessTag}.xml", true);
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