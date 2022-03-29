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
using GameDetails;
using GameManagement;
using LibCore;
using Licensor;
using maturity_check;
using Network;
using NetworkScreens;
using ReportBuilder;
using ResizingUi;
using TransitionScreens;

namespace Polestar_Retail.Application
{
	public class CompleteGamePanel : BaseCompleteGamePanel
	{
	    readonly List<Node> timeNodesBeingWatched;

		ControlBar controlBar;

	    readonly List<Control> gameScreenControls;
        
		public CompleteGamePanel (IProductLicence productLicence, IProductLicensor productLicensor)
			: base (productLicence, productLicensor)
		{
            timeNodesBeingWatched = new List<Node> ();

			gameScreenControls = new List<Control> ();

			//Changing the Navigation buttons to match the PoleStar Images
			_loadButton.Size = new Size(37,30);
			_infoButton.Size = new Size(37,30);
			_raceButton.Size = new Size(37,30);
			_boardButton.Size = new Size(37,30);
			_reportsButton.Size = new Size(37,30);

			//Adjust the position to butt the buttons together
			_loadButton.Left = 5 + 37*0;
			_infoButton.Left = 5 + 37*1;
			_raceButton.Left = 5 + 37*2;
			_boardButton.Left = 5 + 37*3;
			_reportsButton.Left = 5 + 37*4;

			//Adjust the game control buttons
			gameControl.AdjustPosition(37, 33, 0);
            
			WindowDraggingExtensions.EnableDragging();
		}

	    protected override void Dispose (bool disposing)
	    {
	        if (disposing)
	        {
	            DetachTimeBanner();
	        }

	        base.Dispose(disposing);
	    }

	    void DetachTimeBanner ()
	    {
	        foreach (var timeNode in timeNodesBeingWatched)
	        {
	            timeNode.AttributesChanged -= currentTime_AttributesChanged;
	        }
            timeNodesBeingWatched.Clear();

	    }

		protected override EditGamePanel CreateGameDetailsScreen ()
		{
			var egp = new PS_EditGamePanel (gameFile, this);
			egp.ShowTeamName(false);
			return egp;
		}

		protected override TransitionScreen BuildTranisitionScreen(int round)
		{
			controlBar = new ControlBar (gameFile.NetworkModel, true)
			{
				BackColor = Color.White,
				TextBoxWidth = 30,
				ButtonWidth = 60
			};
			Controls.Add(controlBar);
			controlBar.BringToFront();

			var transitionScreen = new PolestarTransitionScreen (gameFile, AppInfo.TheInstance.Location + "\\data", controlBar, this);
			transitionScreen.BuildObjects(round, ! gameFile.IsSalesGame);

			if (IsUserInteractionDisabled)
			{
				transitionScreen.DisableUserInteraction();
			}

			return transitionScreen;
		}

		protected override OpsPhaseScreen CreateOpsPhaseScreen(NetworkProgressionGameFile gameFile, bool isTrainingGame, string gameDir)
		{
			controlBar = new ControlBar (gameFile.NetworkModel, true)
			{
				BackColor = Color.White,
				TextBoxWidth = 30,
				ButtonWidth = 60
			};
			Controls.Add(controlBar);
			controlBar.BringToFront();

			var screen = new PolestarOperationsScreen(gameFile, controlBar, this);

			if (IsUserInteractionDisabled)
			{
				screen.DisableUserInteraction();
			}

			return screen;
		}

		protected override PureTabbedChartScreen CreateChartScreen()
		{
			return new PS_TabbedChartScreen(gameFile, supportOverrides);
		}

		protected override void raceScreen_PhaseFinished_Timer_Tick(object sender, EventArgs e)
		{
			if (raceScreen != null)
			{
				base.raceScreen_PhaseFinished_Timer_Tick(sender, e);
				raceScreen.Pause();
			}
		}

		protected override void CreateToolsScreen ()
		{
			toolsScreen = new FlashToolsScreen(gameFile, this, false, supportOverrides);
			SuspendLayout();
			Controls.Add(toolsScreen);
			ResumeLayout(false);
		}

		protected override void CreateOpsBanner(string PhaseName, bool showDay, bool isRaceView)
		{
        }

	    void currentTime_AttributesChanged (Node sender, ArrayList attributes)
	    {
	        Invalidate();
	    }

        public override void ClearToolTips ()
		{
			DiscreteSimGUI.ItilToolTip_Quad.TheInstance.Hide();
		}

	    protected override void CreatePageTitle ()
	    {
	    }

	    protected override void ShowPageTitle (string title)
	    {
	    }

	    protected override void OnPaint (PaintEventArgs e)
	    {
	        var margin = 20;
	        var left = GetNavigationButtons().Max(a => a.Right) + margin;
	        var right = GetWindowButtons().Min(a => a.Left) - margin;

		    var topBarBounds = new Rectangle (0, 0, Width, 40);
			var bottomBarBounds = new Rectangle (0, Height - 40, Width, 40);

		    var colour = Color.White;

		    using (var brush = new SolidBrush (colour))
		    {
			    e.Graphics.FillRectangle(brush, topBarBounds);
			    e.Graphics.FillRectangle(brush, bottomBarBounds);
		    }

	        const string polestarText = "POLESTAR ITSM";
	        var polestarTextBounds = new RectangleF(Width * 0.75f, 0, 175, 40);

	        var rightDelta = right - polestarTextBounds.Right;

	        if (rightDelta < 10)
	        {
	            polestarTextBounds.X -= 10 - rightDelta;
	        }
            using (var brush = new SolidBrush(Color.FromArgb(189, 190, 188)))
            using (var font = this.GetFontToFit(FontStyle.Regular, polestarText, polestarTextBounds.Size))
            {
                e.Graphics.DrawString(polestarText, font, brush, polestarTextBounds);
            }

            if ((controlBar == null) || ! controlBar.Visible)
            {
                var poweredByImage = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + @"\images\panels\low_poweredby_logo.png");

                var bottomBarHeight = 30;
                var destinationRectangle = new Rectangle(Width - 50 - poweredByImage.Width, (Height - bottomBarHeight) + ((bottomBarHeight - poweredByImage.Height) / 2), poweredByImage.Width, poweredByImage.Height);
                e.Graphics.DrawImage(poweredByImage, destinationRectangle);
            }
		}

		protected override void DoSize ()
		{
			base.DoSize();

			if (controlBar != null)
			{
				controlBar.Bounds = new Rectangle (0, Height - 40, gameControl.Left, 40);
			}

			Invalidate();
		}

		protected override void SetCurrentView (ViewScreen value)
		{
			base.SetCurrentView(value);

			if ((_CurrentView == ViewScreen.RACING_SCREEN) || (_CurrentView == ViewScreen.TRANSITION_SCREEN))
			{
				if (controlBar != null)
				{
					Controls.Add(controlBar);
					controlBar.BringToFront();
					controlBar.Show();
				}

				foreach (var control in gameScreenControls)
				{
					control.Show();
				}
			}
			else
			{
				controlBar?.Hide();

				if (gameScreenControls != null)
				{
					foreach (var control in gameScreenControls)
					{
						control.Hide();
					}
				}
			}

			if ((_CurrentView == ViewScreen.RACING_SCREEN) || (_CurrentView == ViewScreen.TRANSITION_SCREEN) || playing)
			{
			}
			else
			{
			    EmptyScreens();
			}

			Invalidate();
		}

		protected override void EmptyScreens ()
		{
			base.EmptyScreens();

			if (controlBar != null)
			{
				controlBar.Dispose();
				controlBar = null;
			}
		}

		public void AddGameScreenControl (Control control)
		{
			Controls.Add(control);
			gameScreenControls.Add(control);
		}

		public void RemoveGameScreenControl (Control control)
		{
			Controls.Remove(control);
			gameScreenControls.Remove(control);
		}

		public override void GenerateAllReports (string folder)
		{
			var roundScores = GetRoundScores();

			var scoreCardReportBuilder = new PS_OpsScoreCardReport ();
			var processScoresReportBuilder = new OpsProcessScoresReport ();
			OpsSupportCostsReport costsReportBuilder = new OpsSupportCostsReport (new ArrayList (roundScores), supportOverrides);
			var roundScoreCards = new List<string> ();
			var processScoreCards = new List<string> ();
			var costScoreCards = new List<string> ();
			for (int round = 1; round <= gameFile.LastRoundPlayed; round++)
			{
				if (round <= gameFile.LastRoundPlayed)
				{
					if (round <= roundScores.Count)
					{
						roundScoreCards.Add(scoreCardReportBuilder.BuildReport(gameFile, round, roundScores[round - 1]));
						processScoreCards.Add(processScoresReportBuilder.BuildReport(gameFile, round, roundScores[round - 1]));
						costScoreCards.Add(costsReportBuilder.BuildReport(gameFile, round, roundScores[round - 1]));
					}
				}
			}

			File.Copy(scoreCardReportBuilder.aggregateResults(roundScoreCards.ToArray(), gameFile, gameFile.LastRoundPlayed, roundScores[gameFile.LastRoundPlayed - 1]),
				folder + @"\ScoreCard.xml", true);

			int height;
			File.Copy(processScoresReportBuilder.CombineRoundResults(processScoreCards.ToArray(), gameFile, gameFile.LastRoundPlayed, roundScores[gameFile.LastRoundPlayed - 1], out height),
				folder + @"\ProcessScores.xml", true);

			File.Copy(costsReportBuilder.CombineRoundResults(costScoreCards.ToArray(), gameFile, gameFile.LastRoundPlayed),
				folder + @"\SupportCosts.xml", true);

			for (int round = 1; round <= gameFile.LastRoundPlayed; round++)
			{
				for (int business = 0; business <= 4; business++)
				{
					OpsGanttReport ganttReportBuilder = new OpsGanttReport ();

					var businessLabel = SkinningDefs.TheInstance.GetData("biz");
					var businessTag = "All";
					var businessName = SkinningDefs.TheInstance.GetData("allbiz");
					if (business > 0)
					{
						businessName = CONVERT.Format("{0} {1}", businessLabel, business);
						businessTag = CONVERT.ToStr(business);
					}

					File.Copy(ganttReportBuilder.BuildReport(gameFile, round, businessName, true, roundScores[round - 1]),
							folder + $@"\GanttChart_Round{round}_{businessLabel}{businessTag}.xml", true);
				}
			}

			if (gameFile.Game_Eval_Type != em_GameEvalType.ISO_20K)
			{
				var maturityReportBuilder = new OpsMaturityReport ();

				for (int round = 1; round <= gameFile.LastRoundPlayed; round++)
				{
					File.Copy(maturityReportBuilder.BuildReport(gameFile, round, new ArrayList (roundScores)),
						folder + $@"\MaturityReport_Round{round}.xml", true);
				}
			}
		}

		protected override GameSelectionScreen CreateGameSelectionScreen (IGameLoader gameLoader, IProductLicence productLicence,
		                                                                  IProductLicensor productLicensor)
		{
			var gameSelectionScreen = base.CreateGameSelectionScreen(gameLoader, productLicence, productLicensor);
			gameSelectionScreen.MaturityEditorCreator = (() => new MaturityEditorForm ());

			return gameSelectionScreen;
		}
	}
}