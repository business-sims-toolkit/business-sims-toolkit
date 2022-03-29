using System;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Reflection;
using System.IO;
using System.Linq;
using GameManagement;
using TransitionScreens;

using LibCore;
using Network;
using Logging;

using ChartScreens;

using IncidentManagement;

using CoreUtils;
using NetworkScreens;
using CommonGUI;
using GameDetails;
using ReportBuilder;
using GameEngine;
using Licensor;
using Environment = System.Environment;

namespace CoreScreens
{
	public abstract class BaseCompleteGamePanel : BasePanel, IGameLoader
	{
		IProductLicensor productLicensor;
		IProductLicence productLicence;

		protected OpsScreenBanner screenBanner;
		System.ComponentModel.Container components = null;

		protected Font MyDefaultSkinFontBold12;
		protected Font MyDefaultSkinFontBold14;

		protected NetworkProgressionGameFile gameFile;
		protected Label pageTitle;

		protected PureTabbedChartScreen chartsScreen;
		protected PureTabbedChartScreen IT_charts;

		protected TransitionScreens.TransitionScreen transScreen;
		protected ToolsScreenBase toolsScreen;

		protected Image top_bar_game;
		protected Image top_bar_game_clean;
		protected Image top_bar_normal;
		protected Image bottom_bar;
		protected Image training_top_bar_game;
		protected Image training_bottom_bar_normal;
		protected int screenBannerLeft = 210;
		protected int screenBannerTop = SkinningDefs.TheInstance.GetIntData("timer_y_offset", 5);
		protected int pageTitleLeft = 210;
		protected int pageTitleTop = 5;

		protected BaseOpsPhaseEngine baseOpsEngine;

		protected abstract PureTabbedChartScreen CreateChartScreen ();
		protected abstract EditGamePanel CreateGameDetailsScreen ();

		protected string override_pdf = "";
		protected Node _PrePlayStatusNode = null;
		protected bool ManualPrePlayEscape = false;
		protected Color bannerForeColor = Color.White;
		protected Color bannerPrePlayForeColor = Color.Orange;
		protected Label teamLabel;
		public bool IncidentsImported = false;

		public void OverridePDF (string file)
		{
			override_pdf = file;
		}

		public virtual void EnableReportsButton (bool enable)
		{
			_reportsButton.Enabled = enable;
		}

		public NetworkProgressionGameFile TheGameFile
		{
			get
			{
				return gameFile;
			}
		}

		protected string AppAssemblyDir = string.Empty;
		protected string AppExeDir = string.Empty;

		protected ImageButton _loadButton;
		protected ImageButton _infoButton;
		protected ImageButton _raceButton;
		protected ImageButton _boardButton;
		protected ImageButton _reportsButton;
		protected ImageButton _reportsButton2;

		protected bool _usePass;

		protected ImageButton _minButton;
		protected ImageButton sizeButton;
		protected ImageButton _closeButton;
		protected GameSelectionScreen gameSelectionScreen;

		protected GlassGameControl gameControl;
		protected Panel linerule;
		protected bool playing = false;
		protected GameDetails.EditGamePanel gameDetailsScreen;

		public delegate void PlayPressedHandler (GameFile game);

		public event PlayPressedHandler PlayPressed;

		public enum GameStartType
		{
			MintedNewCoupon,
			CouponAlreadyExisted,
			Failed
		}

		public delegate GameStartType GameSetupdHandler (GameFile game);

		public event GameSetupdHandler GameSetup;

		protected bool _isTrainingGame = false;

		protected bool inGame = false;

		protected OpsPhaseScreen raceScreen;

		public enum ViewScreen
		{
			GAME_SELECTION_SCREEN,
			GAME_DETAILS_SCREEN,
			RACING_SCREEN,
			TRANSITION_SCREEN,
			NETWORK_SCREEN,
			REPORT_SCREEN,
			IT_REPORT_SCREEN
		}

		public delegate bool PasswordCheckHandler (string password);

		public event PasswordCheckHandler PasswordCheck;

		public delegate bool IncidentsImportedHandler (bool incidents);

		public event IncidentsImportedHandler incidentsimported;

		protected virtual void CreateToolsScreen ()
		{
			toolsScreen = new ToolsScreen(gameFile, this, false, supportOverrides);
			toolsScreen.Hide();
			this.SuspendLayout();
			this.Controls.Add(toolsScreen);
			this.ResumeLayout(false);
			DoSize();
		}

		protected ViewScreen _CurrentView;

		public ViewScreen CurrentView
		{
			get
			{
				return _CurrentView;
			}
			set
			{
				SetCurrentView(value);
			}
		}

		protected virtual BaseTabbedChartScreen CreateITChartScreen ()
		{
			return null;
		}

		public abstract void ClearToolTips ();

		protected virtual GameSelectionScreen CreateGameSelectionScreen (IGameLoader gameLoader, IProductLicence productLicence, IProductLicensor productLicensor)
		{
			return new GameSelectionScreen(gameLoader, productLicence, productLicensor);
		}

		protected bool DetermineGameExpired ()
		{
			bool game_expired = true;

			if (this.gameFile != null)
			{
				return ! gameFile.Licence.IsPlayable;
			}
			return game_expired;
		}

		protected bool DetermineGameSales ()
		{
			return gameFile.IsSalesGame;
		}

		protected bool DetermineGameTraining ()
		{
			return gameFile.IsTrainingGame;
		}

		protected bool changeImportIncidentsLabel (bool status)
		{
			if (null != incidentsimported)
			{
				IncidentsImported = false;
				return incidentsimported(status);
			}
			return true;
		}

		protected virtual bool DisconnectAndDispose_GameDetails ()
		{
			if (gameDetailsScreen != null)
			{
				if (! gameDetailsScreen.SetupGame()) return false;
				teamLabel.Text = GetTeamName();
				//DISCONNECT THE EVENT HANDLERS 
				gameDetailsScreen.PlayPressed -= gameDetailsScreen_PlayPressed;
				gameDetailsScreen.SkipPressed -= gameDetailsScreen_SkipPressed;
				gameDetailsScreen.GameSetup -= gameDetailsScreen_GameSetup;
				//Dispose of the game details 
				LibCore.WinUtils.Dispose(gameDetailsScreen);
				gameDetailsScreen = null;

				return true;
			}

			return false;
		}

		protected virtual void SetCurrentView (ViewScreen value)
		{
			using (WaitCursor cursor = new WaitCursor(this))
			{
				if ((_CurrentView == value)
				    && ((value != ViewScreen.GAME_SELECTION_SCREEN)
				        && (value != ViewScreen.RACING_SCREEN)))
				{
					return;
				}

				if (value == ViewScreen.GAME_SELECTION_SCREEN)
				{
					_isTrainingGame = false;
				}

				if (value != ViewScreen.NETWORK_SCREEN)
				{
					if (toolsScreen != null)
					{
						toolsScreen.DisposeEditBox();
					}
				}

				ClearToolTips();

				this.SuspendLayout();

				ImageButton buttonToHighlight = null;

				if (value == ViewScreen.RACING_SCREEN)
				{
					buttonToHighlight = _raceButton;

					if (this.raceScreen == null)
					{
						this.ResumeLayout(false);
						return;
					}

					if (null != gameDetailsScreen)
					{
						if (! DisconnectAndDispose_GameDetails())
						{
							return;
						}
					}

					LibCore.WinUtils.Show(raceScreen);
					LibCore.WinUtils.Hide(transScreen);
					LibCore.WinUtils.Hide(toolsScreen);
					LibCore.WinUtils.Hide(chartsScreen);

					// : Fix for 3631 (can't switch correctly out of IT charts screen)
					if (IT_charts != null)
					{
						IT_charts.Hide();
					}
					LibCore.WinUtils.Hide(pageTitle);

					if (gameSelectionScreen != null)
					{
						Controls.Remove(gameSelectionScreen);
						gameSelectionScreen.Dispose();
						gameSelectionScreen = null;
						//WinUtils.Dispose(gameSelectionScreen);
					}

					SetMainButtonsLoadedGame(true);
					gameControl.Visible = true;

					if (null != screenBanner)
					{
						screenBanner.Visible = true;
					}

					ShowTeamLabel(true);
				}
				else if (value == ViewScreen.TRANSITION_SCREEN)
				{
					buttonToHighlight = _raceButton;
					LibCore.WinUtils.Hide(pageTitle);


					if (this.transScreen == null)
					{
						this.ResumeLayout(false);
						return;
					}

					if (null != gameDetailsScreen)
					{
						if (! DisconnectAndDispose_GameDetails())
						{
							return;
						}
					}

					if (null != raceScreen) raceScreen.Hide();
					transScreen.Show();
					if (null != toolsScreen && toolsScreen.Visible) toolsScreen.Hide();
					if (null != chartsScreen && chartsScreen.Visible) chartsScreen.Hide();
					if (null != IT_charts && IT_charts.Visible) IT_charts.Hide();

					if (gameSelectionScreen != null)
					{
						Controls.Remove(gameSelectionScreen);
						gameSelectionScreen.Dispose();
						gameSelectionScreen = null;
						//WinUtils.Dispose(gameSelectionScreen);
					}

					SetMainButtonsLoadedGame(true);
					gameControl.Visible = true;

					if (null != screenBanner)
					{
						screenBanner.Visible = true;
					}

					ShowTeamLabel(true);
				}
				else if (value == ViewScreen.NETWORK_SCREEN)
				{
					buttonToHighlight = _boardButton;

					if (toolsScreen != null)
					{
						toolsScreen.RefreshSupportCosts();
						toolsScreen.Hide();
					}

					if (null != gameDetailsScreen)
					{
						if (! DisconnectAndDispose_GameDetails())
						{
							return;
						}
					}
					//
					if (gameSelectionScreen != null)
					{
						Controls.Remove(gameSelectionScreen);
						gameSelectionScreen.Dispose();
						gameSelectionScreen = null;
						//WinUtils.Dispose(gameSelectionScreen);
					}

					LibCore.WinUtils.Hide(raceScreen);
					LibCore.WinUtils.Hide(transScreen);

					if (null != toolsScreen)
					{
						toolsScreen.readNetwork();
					}
					else
					{
						CreateToolsScreen();
					}

					// : workaround for bug 3549: on becoming visible, reset the zoom
					// on the board view, so that the flash icons don't get repositioned crazily.
					toolsScreen.ResetView();

					LibCore.WinUtils.Hide(chartsScreen);
					if (IT_charts != null)
					{
						IT_charts.Hide();
					}

					SetMainButtonsLoadedGame(_raceButton.Enabled);
					gameControl.Visible = false;

					ShowPageTitle(SkinningDefs.TheInstance.GetData("tools_screen_name", "Tools Screen"));

					if (null != screenBanner)
					{
						screenBanner.Visible = false;
					}

					ShowTeamLabel(true);

					_CurrentView = value;

					DoSize();

					toolsScreen.Show();
					toolsScreen.BringToFront();
				}
				else if ((value == ViewScreen.REPORT_SCREEN) || (value == ViewScreen.IT_REPORT_SCREEN))
				{

					buttonToHighlight = PickButtonToHighlight(value);

					if (_reportsButton2 == null
					) // Pulling through the IT Reports Screen button so that we can highlight it correctly (Bug #10446)
					{
						_reportsButton2 = PickButtonToHighlight(ViewScreen.IT_REPORT_SCREEN);
					}

					if (null != gameDetailsScreen)
					{
						if (! DisconnectAndDispose_GameDetails()) return;
					}

					LibCore.WinUtils.Hide(raceScreen);
					LibCore.WinUtils.Hide(transScreen);
					LibCore.WinUtils.Hide(toolsScreen);
					LibCore.WinUtils.Hide(screenBanner);

					if (value == ViewScreen.REPORT_SCREEN)
					{
						if (chartsScreen == null)
						{
							//DT_A1 = DateTime.Now;
							chartsScreen = CreateChartScreen();
							//DT_A2 = DateTime.Now;
							this.SuspendLayout();
							this.Controls.Add(chartsScreen);
							this.ResumeLayout(false);

							//DT_A3 = DateTime.Now;
							SizeChartScreen(chartsScreen, false);
							chartsScreen.Init(0);
							//DT_A4 = DateTime.Now;
						}
						else
						{
							chartsScreen.ReloadDataAndShow(true);
						}
						chartsScreen.Show();
						chartsScreen.BringToFront();
						if (IT_charts != null)
						{
							IT_charts.Hide();
						}

						DoSize();

						ShowPageTitle("Reports Screen");
					}
					else if (value == ViewScreen.IT_REPORT_SCREEN)
					{
						if (IT_charts == null)
						{
							IT_charts = CreateITChartScreen();
							this.SuspendLayout();
							this.Controls.Add(IT_charts);
							this.ResumeLayout(false);

							SizeChartScreen(IT_charts);
							IT_charts.Init(0);
						}
						else
						{
							IT_charts.ReloadDataAndShow(true);
						}

						IT_charts.Show();
						IT_charts.BringToFront();
						if (chartsScreen != null)
						{
							chartsScreen.Hide();
						}
						ShowPageTitle("IT Reports Screen");
					}

					if (gameSelectionScreen != null)
					{
						Controls.Remove(gameSelectionScreen);
						gameSelectionScreen.Dispose();
						gameSelectionScreen = null;
					}

					SetMainButtonsLoadedGame(_raceButton.Enabled);
					gameControl.Visible = false;

					if (null != screenBanner)
					{
						screenBanner.Visible = false;
					}

					ShowTeamLabel(true);
				}
				else if (value == ViewScreen.GAME_SELECTION_SCREEN)
				{
					buttonToHighlight = _loadButton;

					if (gameSelectionScreen != null) return;


					DisposeOpsScreen();

					if (null != gameDetailsScreen)
					{
						if (! DisconnectAndDispose_GameDetails())
						{
							return;
						}
					}

					if (gameFile != null)
					{
						gameFile.Save(true, false);
						gameFile.Dispose();
						gameFile = null;
					}

					gameSelectionScreen = CreateGameSelectionScreen(this, productLicence, productLicensor);
					gameSelectionScreen.Location = new Point(0, 50 - 10);
					gameSelectionScreen.Size = new Size(1024, this.Height - 40 - gameSelectionScreen.Top);
					gameSelectionScreen.PasswordCheck += gameSelectionScreen_PasswordCheck;
					gameSelectionScreen.Name = "The Game Selection Screen";
					this.Controls.Add(gameSelectionScreen);

					this.DisposePrePlaySystem();

					DisposeGameScreen();

					if (null != transScreen)
					{
						transScreen.Hide();
						transScreen.Dispose();
						transScreen = null;
					}

					if (null != toolsScreen)
					{
						LibCore.WinUtils.Hide(toolsScreen);
						toolsScreen.Dispose();
						toolsScreen = null;
					}

					if (null != this.chartsScreen)
					{
						LibCore.WinUtils.Hide(chartsScreen);
						chartsScreen.Dispose();
						chartsScreen = null;
					}

					if (IT_charts != null)
					{
						IT_charts.Hide();
						IT_charts.Dispose();
						IT_charts = null;
					}

					if (gameFile != null)
					{
						gameFile.Save(true);
						gameFile.Dispose();
						gameFile = null;
					}

					_loadButton.Enabled = true;
					_infoButton.Enabled = false;
					_raceButton.Enabled = false;

					if (_boardButton != null)
					{
						_boardButton.Enabled = false;
					}

					LibCore.WinUtils.Hide(gameControl);
					LibCore.WinUtils.Hide(screenBanner);

					ShowPageTitle("Game Selection Screen");

					if (null != screenBanner)
					{
						screenBanner.Visible = false;
						//dispose 
						if (screenBanner != null)
						{
							this.Controls.Remove(screenBanner);
							screenBanner.Dispose();
							screenBanner = null;
						}
					}
					KlaxonSingleton.TheInstance.Stop();
					changeImportIncidentsLabel(false);

					ShowTeamLabel(false);
				}
				else if (value == ViewScreen.GAME_DETAILS_SCREEN)
				{
					buttonToHighlight = _infoButton;

					DisposeOpsScreen();

					if (null != gameDetailsScreen)
					{
						return;
					}
					// Save the game whenever we hit this screen.
					this.gameFile.Save(true);

					if (gameFile.IsSalesGame)
					{
						gameFile.RevertSalesChanges();
					}

					//
					gameDetailsScreen = CreateGameDetailsScreen();
					gameDetailsScreen.OverridePDF(override_pdf);
					gameDetailsScreen.PlayPressed += gameDetailsScreen_PlayPressed;
					gameDetailsScreen.SkipPressed += gameDetailsScreen_SkipPressed;
					gameDetailsScreen.GameSetup += gameDetailsScreen_GameSetup;
					gameDetailsScreen.GameEvalTypeChanged += gameDetailsScreen_GameEvalTypeChanged;
					gameDetailsScreen.Name = "Game Details Screen Panel";
					this.Controls.Add(gameDetailsScreen);

					LibCore.WinUtils.Hide(chartsScreen);
					if (IT_charts != null)
					{
						IT_charts.Hide();
					}
					if (gameSelectionScreen != null)
					{
						Controls.Remove(gameSelectionScreen);
						gameSelectionScreen.Dispose();
						gameSelectionScreen = null;
						//WinUtils.Dispose(gameSelectionScreen);
					}

					DisposeGameScreen();

					if (null != transScreen)
					{
						LibCore.WinUtils.Dispose(transScreen);
						transScreen = null;
					}
					if (null != toolsScreen)
					{
						LibCore.WinUtils.Hide(toolsScreen);
					}

					this.DisposePrePlaySystem();

					_CurrentView = value;
					SetMainButtonsLoadedGame(false);
					gameControl.Visible = false;

					ShowPageTitle("Game Details Screen");

					if (null != screenBanner)
					{
						screenBanner.Visible = false;
						if (screenBanner != null)
						{
							this.Controls.Remove(screenBanner);
							screenBanner.Dispose();
							screenBanner = null;
						}
					}

					teamLabel.Text = GetTeamName();
					ShowTeamLabel(false);
					changeImportIncidentsLabel(false);
					KlaxonSingleton.TheInstance.Stop();
				}

				// If we're showing anything apart from the game screen, then interruptions are allowed.
				if (value != ViewScreen.RACING_SCREEN)
				{
					if ((gameFile == null))
					{
						SetPlaying(HasGameStarted() && inGame);
					}
					else if (gameFile.CurrentPhase == GameFile.GamePhase.OPERATIONS)
					{
						SetPlaying(HasGameStarted() && inGame);
					}
					else
					{
						SetPlaying(inGame);
					}

					if (value != ViewScreen.GAME_SELECTION_SCREEN)
					{
						SetMainButtonsLoadedGame(false);
					}
				}

				_CurrentView = value;

				DoSize();

				if (buttonToHighlight != null)
				{
					buttonToHighlight.Enabled = true;
				}

				foreach (ImageButton button in new ImageButton []
					{ _loadButton, _infoButton, _raceButton, _boardButton, _reportsButton, _reportsButton2 })
				{
					if (button != null)
					{
						if (button != buttonToHighlight)
						{
							button.Active = false;
							continue;
						}

						button.Active = true;
						button.Focus();
					}
				}

				this.ResumeLayout(true);
				DoSize();
			}
		}

		protected void gameDetailsScreen_GameEvalTypeChanged (object sender, EventArgs e)
		{
			// If the maturity metrics have changed, discard the tools screen, as it will now contain a scorecard
			// for the wrong type.
			if (toolsScreen != null)
			{
				toolsScreen.Dispose();
				toolsScreen = null;
			}
		}

		protected virtual ImageButton PickButtonToHighlight (ViewScreen value)
		{
			return ((value == ViewScreen.REPORT_SCREEN) ? _reportsButton : _reportsButton2);
		}

		protected virtual void DisposeOpsScreen ()
		{
			EmptyScreens();
		}

		protected string GetTeamName ()
		{
			string team_name = "";

			if (gameFile != null)
			{
				BasicXmlDocument xml = BasicXmlDocument.LoadOrCreate(gameFile.Dir + @"\global\team.xml");
				System.Xml.XmlElement root = XMLUtils.GetOrCreateElement(xml, "team");
				System.Xml.XmlElement nameElement = XMLUtils.GetOrCreateElement(root, "team_name");
				team_name = nameElement.InnerText;
			}

			return team_name;
		}

		protected virtual bool IsGameScreenAccessible ()
		{
			return ((raceScreen != null) || (transScreen != null));
		}

		protected virtual void SetMainButtonsLoadedGame (bool overrideRaceEnabled)
		{
			if (gameFile != null)
			{
				//Normal Behaviour
				if (playing || HasGameStarted())
				{
					bool canExitGame = ((gameFile == null)
					                    || ! gameFile.IsNormalGame);
					_loadButton.Enabled = canExitGame;
					_infoButton.Enabled = canExitGame;
					_raceButton.Enabled = IsGameScreenAccessible();

					if (_boardButton != null)
					{
						_boardButton.Enabled = true;
					}

					if (_reportsButton != null)
					{
						EnableReportsButton(true);
					}

				}
				else
				{
					_loadButton.Enabled = true;
					_infoButton.Enabled = true;
					_raceButton.Enabled = ((CurrentView == ViewScreen.TRANSITION_SCREEN) || (CurrentView == ViewScreen.RACING_SCREEN));

					if (_boardButton != null)
					{
						_boardButton.Enabled = true;
					}

					if (_reportsButton != null)
					{
						EnableReportsButton(true);
					}

				}

				if (overrideRaceEnabled)
				{
					_raceButton.Enabled = true;
				}
			}
			else
			{
				_loadButton.Enabled = true;
				_infoButton.Enabled = false;
				_raceButton.Enabled = false;

				if (_boardButton != null)
				{
					_boardButton.Enabled = false;
				}

				if (_reportsButton != null)
				{
					_reportsButton.Enabled = false;
				}

				if (_reportsButton2 != null)
				{
					_reportsButton2.Enabled = false;
				}
			}
		}

		protected abstract OpsPhaseScreen CreateOpsPhaseScreen (NetworkProgressionGameFile gameFile, bool isTrainingGame,
		                                                        string gameDir);

		public virtual OpsPhaseScreen CreateOpsScreenForDisplayOnly (NetworkProgressionGameFile gameFile, int round,
		                                                             bool isTrainingGame, string gameDir)
		{
			return CreateOpsPhaseScreen(gameFile, isTrainingGame, gameDir);
		}

		/// <summary>
		/// The OpScreenBanner needs refactoring into a base class and 2 child classes 
		/// It handles the display of information at the top of the screen (Phase Title, Day Time etc)
		/// This was the same for both Race and Transition at the start of V3 but has changed. 
		/// The most flexible idea would be a central base class with update methods 
		/// and 2 child classs to handle the Race display and the transition requirements 
		/// These classes could then be sub-classed for other projects
		/// </summary>
		/// <param name="PhaseName"></param>
		/// <param name="showDay"></param>
		/// <param name="isRaceView"></param>
		protected virtual void CreateOpsBanner (string PhaseName, bool showDay, bool isRaceView)
		{
			screenBanner = new OpsScreenBanner(gameFile.NetworkModel, showDay);
			screenBanner.Round = gameFile.CurrentRound;
			screenBanner.Location = new Point(screenBannerLeft, screenBannerTop);
			if (isRaceView)
			{
				screenBanner.SetHourMode(CONVERT.ParseInt(CoreUtils.SkinningDefs.TheInstance.GetData("show_hour")));
			}
			screenBanner.ChangeBannerTextForeColour(bannerForeColor);
			screenBanner.ChangeBannerPrePlayTextForeColour(bannerPrePlayForeColor);
			screenBanner.SetRaceViewOn(isRaceView);

			screenBanner.FixColon(800);

			screenBanner.Phase = PhaseName;
			screenBanner.Name = "Ops Screen Banner";
			this.Controls.Add(screenBanner);
		}

		protected virtual void DoOperationsPreConnectWork (int round, bool rewind)
		{
			// NOP
		}

		protected virtual void clearOtherDataFiles (int round, string current_round_dir)
		{
			// NOP
		}

		/// <summary>
		/// Runs the operations (race) phase for the round.
		/// </summary>
		/// <param name="round">The round to run the operations phase for.</param>
		/// <param name="rewind">Going Backwards.</param>
		public virtual void RunRace (int round, bool rewind, bool skip_transition)
		{
			var permissions =
				productLicence.GetPermissionToStartPlay(gameFile.Licence,
					gameFile.RoundToPhase(round, GameFile.GamePhase.OPERATIONS));
			if (! permissions)
			{
				MessageBox.Show(this, "Error", "Licensing Error");
				return;
			}

			using (WaitCursor cursor = new WaitCursor(this))
			{
				GameFile.GamePhase rewindGamePhase = GameFile.GamePhase.OPERATIONS;
				if (skip_transition)
				{
					rewindGamePhase = GameFile.GamePhase.TRANSITION;
				}
				//
				if (rewind && ! skip_transition)
				{
					if (! gameFile.CanPlayNow(round, rewindGamePhase))
					{
						MessageBox.Show(this, "This round cannot be reset again, the maximum number of resets has been reached.");
						CurrentView = ViewScreen.GAME_DETAILS_SCREEN;
						return;
					}
				}

				EmptyScreens();

				if (toolsScreen != null)
				{
					toolsScreen.Hide();
					toolsScreen.Dispose();
					toolsScreen = null;
				}

				if ((gameFile.CurrentRound != round)
				    || (gameFile.CurrentPhase != rewindGamePhase))
				{
					gameFile.SetCurrentRound(round, rewindGamePhase, rewind);
				}

				DoOperationsPreConnectWork(round, rewind);

				// If we are skipping the transition phase then use the IncidentApplier to squirt
				// some new incidents into the model for this "skipped" phase, then move onto the
				// next phase.
				if (skip_transition)
				{
					//New code to record the incident in the skipped phase
					gameFile.SetCurrentRound(round, GameFile.GamePhase.TRANSITION, rewind);
					BasicIncidentLogger biLog = new BasicIncidentLogger();
					biLog.LogTreeToFile(gameFile.NetworkModel, gameFile.CurrentRoundDir + "\\NetworkIncidents.log");

					//Build a new IApplier to apply the required incidents involed in a skip 
					IncidentApplier iApplier = new IncidentApplier(gameFile.NetworkModel);

					//Read the Required skip incidents xml file
					string skipDefsFile = AppInfo.TheInstance.Location + "\\data\\skip" + LibCore.CONVERT.ToStr(round) + ".xml";
					System.IO.StreamReader file = new System.IO.StreamReader(skipDefsFile);
					string xmldata = file.ReadToEnd();
					file.Close();
					file = null;

					iApplier.SetIncidentDefinitions(xmldata, gameFile.NetworkModel);

					//mind to close and dispose
					iApplier.Dispose();
					biLog.CloseLog();
					biLog.Dispose();

					// Flick license if required to otherwise the game license manager wont let us play
					// the race as it will think that the transition hasn't been played yet...

					int phase = gameFile.RoundToPhase(round, GameFile.GamePhase.TRANSITION);

					if (ShouldPlayingThisRoundIncrementLicencePlayCount())
					{
						gameFile.Licence.PlayPhase(phase);
						gameFile.Save(false);
					}

					DoAdditionalSkipTransitionWork();

					gameFile.SetCurrentRound(round, GameFile.GamePhase.OPERATIONS, rewind);
				}
				//
				if (rewind)
				{
					//used to clear extra data files used in PM 
					clearOtherDataFiles(round, gameFile.CurrentRoundDir);
					// Reset the game controls for a REWIND
					gameControl.SetState(false, true, false, false, true);
					TimeManager.TheInstance.Reset();
					changeImportIncidentsLabel(false);
					SetPlaying(false);
				}

				gameControl.ResetButtons(false);

				// If we are a training game then tell the Race Screen.
				raceScreen = CreateOpsPhaseScreen(gameFile, this._isTrainingGame, gameFile.Dir);
				raceScreen.PlayPressed += raceScreen_PlayPressed;
				raceScreen.GameStarted += raceScreen_GameStarted;
				raceScreen.Name = "Ops Race Screen";
				TimeManager.TheInstance.Stop();
				this.Controls.Add(raceScreen);

				raceScreen.PhaseFinished += raceScreen_PhaseFinished;
				raceScreen.Visible = false;

				if (screenBanner != null)
				{
					this.Controls.Remove(screenBanner);
					screenBanner.Dispose();
				}

				string propername = SkinningDefs.TheInstance.GetData("race_screen_name");
				if (propername == null)
				{
					propername = "Game Screen";
				}

				if (! SkinningDefs.TheInstance.GetBoolData("esm_sim", false))
				{
					CreateOpsBanner(propername, false, true);
				}

				Boolean preplayflag = false;
				int preplaytime = 0;
				ExtractPrePlaySystemValues(true, out preplayflag, out preplaytime);
				this.AttachPrePlaySystem(preplayflag, preplaytime);

				CurrentView = ViewScreen.RACING_SCREEN;

				DoSize();
			}
		}

		protected virtual void DoAdditionalSkipTransitionWork ()
		{
		}

		public virtual bool ShouldPlayingThisRoundIncrementLicencePlayCount ()
		{
			return true;
		}

		protected virtual void handleRaceScreenHasGameStarted ()
		{
			if (inGame != true)
			{
				inGame = true;
				_loadButton.Enabled = false;
				_infoButton.Enabled = false;
			}
		}

		protected void raceScreen_GameStarted (object sender, EventArgs e)
		{
			handleRaceScreenHasGameStarted();
		}

		public OpsPhaseScreen GetOpsPhaseScreen ()
		{
			return raceScreen;
		}

		protected virtual TransitionScreen BuildTranisitionScreen (int round)
		{
			TransitionScreen ts = new TransitionScreen(gameFile, AppInfo.TheInstance.Location + "\\data");
			ts.BuildObjects(round, ! gameFile.IsSalesGame);

			if (isUserInteractionDisabled)
			{
				ts.DisableUserInteraction();
			}

			return ts;
		}

		/// <summary>
		/// Runs the transition phase for a particular round.
		/// </summary>
		/// <param name="round">The round to run the transition phase for.</param>
		/// <param name="rewind">Wether we are rewinding</param>
		public void RunTransition (int round, bool rewind)
		{
			var permissions =
				productLicence.GetPermissionToStartPlay(gameFile.Licence,
					gameFile.RoundToPhase(round, GameFile.GamePhase.TRANSITION));
			if (! permissions)
			{
				MessageBox.Show(this, "Error", "Licensing Error");
				return;
			}

			if (rewind)
			{
				if (! gameFile.CanPlayNow(round, GameFile.GamePhase.TRANSITION))
				{
					MessageBox.Show(this, "This round cannot be reset again, the maximum number of resets has been reached.");
					return;
				}
			}

			EmptyScreens();

			gameFile.SetCurrentRound(round, NetworkProgressionGameFile.GamePhase.TRANSITION, rewind);

			if (rewind)
			{
				// Reset the game controls for a REWIND
				gameControl.SetState(false, true, false, false, true);
				TimeManager.TheInstance.Reset();
				SetPlaying(false);
			}

			gameControl.ResetButtons(false);

			StreamReader sr = File.OpenText(AppInfo.TheInstance.Location + "\\data\\visual.xml");
			string data = sr.ReadToEnd();
			sr.Close();
			sr = null;

			NodeTree config = new NodeTree(data);

			if (toolsScreen != null)
			{
				toolsScreen.Hide();
				toolsScreen.Dispose();
				toolsScreen = null;
			}

			transScreen = this.BuildTranisitionScreen(round);

			if (gameFile.IsSalesGame)
			{
				// Sales game to force to 4 times speed.
				TimeManager.TheInstance.FastForward(4);
				TimeManager.TheInstance.Stop();
			}

			transScreen.PhaseFinished += transScreen_PhaseFinished;
			transScreen.Name = "The Transition Screen Panel";
			this.Controls.Add(transScreen);

			CurrentView = ViewScreen.TRANSITION_SCREEN;

			if (screenBanner != null)
			{
				this.Controls.Remove(screenBanner);
				screenBanner.Dispose();
			}

			string propername = SkinningDefs.TheInstance.GetData("transition_screen_name");
			if (propername == null)
			{
				propername = "Transition Screen";
			}
			CreateOpsBanner(propername, true, false);

			Boolean preplayflag = false;
			int preplaytime = 0;
			ExtractPrePlaySystemValues(false, out preplayflag, out preplaytime);
			this.AttachPrePlaySystem(preplayflag, preplaytime);

			DoSize();
		}

		public void TryCloseWindow ()
		{
			if (CanDispose())
			{
				((Form) TopLevelControl).Close();
			}
		}

		public bool CanDispose ()
		{
			if (gameDetailsScreen != null)
			{
				bool ok = gameDetailsScreen.SetupGame();
				if (! ok) return false;
			}

			if (this.inGame && (this.gameFile != null))

			{
				if (gameFile.IsNormalGame)
				{
					using (CustomDialogBox dialog = new CustomDialogBox(CustomDialogBox.MessageType.Warning))
					{
						dialog.setText("Closing the application will lose data.\r\nAre you sure you want to close the Application?");
						dialog.ShowDialog(TopLevelControl);


						if (dialog.DialogResult != DialogResult.OK)
						{
							return false;
						}
					}
				}
			}
			else if (this.CurrentView == ViewScreen.RACING_SCREEN)
			{

				using (CustomDialogBox dialog = new CustomDialogBox(CustomDialogBox.MessageType.Warning))
				{
					dialog.setText("Are you sure you want to close the Application?");
					dialog.ShowDialog(TopLevelControl);


					if (dialog.DialogResult != DialogResult.OK)
					{
						return false;
					}
				}
			}

			return true;
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose (bool disposing)
		{
			if (disposing)
			{
				TimeManager.TheInstance.Stop();
				this.EmptyScreens();

				if (screenBanner != null)
				{
					this.Controls.Remove(screenBanner);
					screenBanner.Dispose();
					screenBanner = null;
				}

				if (raceScreen != null)
				{
					DisposeGameScreen();
				}

				if (transScreen != null)
				{
					this.Controls.Remove(transScreen);
					transScreen.Dispose();
					transScreen = null;
					//WinUtils.Dispose(transScreen);
				}
				WinUtils.Dispose(toolsScreen);
				if (gameSelectionScreen != null)
				{
					Controls.Remove(gameSelectionScreen);
					gameSelectionScreen.Dispose();
					gameSelectionScreen = null;
					//WinUtils.Dispose(gameSelectionScreen);
				}
				WinUtils.Dispose(gameDetailsScreen);

				if (gameFile != null)
				{
					gameFile.Save(true);
					gameFile.Dispose();
					gameFile = null;
				}

				components?.Dispose();

				if (IT_charts != null)
				{
					IT_charts.Dispose();
					IT_charts = null;
				}

				chartsScreen?.Dispose();
				chartsScreen = null;

				this.Controls.Clear();
			}

			base.Dispose(disposing);
		}

		protected virtual void DisposeGameScreen ()
		{
			if (baseOpsEngine != null)
			{
				baseOpsEngine.Dispose();
				baseOpsEngine = null;
			}

			if (raceScreen != null)
			{
				raceScreen.Dispose();
				raceScreen = null;
			}
		}

		protected virtual void EmptyScreens ()
		{
			TimeManager.TheInstance.Stop();
			raceScreen_PhaseFinished_Timer?.Stop();
			CleanupRaceScreen();

			DisposeGameScreen();

			if (transScreen != null)
			{
				this.Controls.Remove(transScreen);
				transScreen.Dispose();
				transScreen = null;
			}

			// Always switch off fast forward at this point.
			//
			if (TimeManager.TheInstance.TimeIsRunning)
			{
				if (_isTrainingGame)
				{
					TimeManager.TheInstance.FastForward(3);
				}
				else
				{
					TimeManager.TheInstance.FastForward(1);
				}
			}
		}

		void InstantiateScreens ()
		{
		}

		protected virtual void AddMainButtons ()
		{
			AddNavigationButtons();
			AddWindowControlButtons();
			AddTimeControlButtons();
			Location = new Point(0, 0);
		}

		protected virtual void AddNavigationButtons ()
		{
			int NavButtonWidth = 34;
			int NavButtonHeight = 34;
			int NavButtonOffsetY = 3;
			int navButtonOffsetX = 5;

			_loadButton = new ImageButton(0);
			_loadButton.Size = new Size(NavButtonWidth, NavButtonHeight);
			_loadButton.Location = new Point(5, NavButtonOffsetY);
			_loadButton.SetToolTipText = "Game Selection Screen";
			_loadButton.SetVariants("\\images\\buttons\\disk.png");
			_loadButton.ButtonPressed += _ButtonPressed;
			this.Controls.Add(_loadButton);
			//
			_infoButton = new ImageButton(1);
			_infoButton.Size = new Size(NavButtonWidth, NavButtonHeight);
			_infoButton.Location = new Point(_loadButton.Right + navButtonOffsetX, NavButtonOffsetY);
			_infoButton.SetToolTipText = "Game Details Screen";
			_infoButton.SetVariants("\\images\\buttons\\info.png");
			_infoButton.ButtonPressed += _ButtonPressed;
			this.Controls.Add(_infoButton);
			//
			_raceButton = new ImageButton(2);
			_raceButton.Size = new Size(NavButtonWidth, NavButtonHeight);
			_raceButton.Location = new Point(_infoButton.Right + navButtonOffsetX, NavButtonOffsetY);
			_raceButton.SetToolTipText = SkinningDefs.TheInstance.GetData("ops_screen_tooltip",
				SkinningDefs.TheInstance.GetData("ops_banner") + " Screen");
			_raceButton.SetVariants("\\images\\buttons\\flag.png");
			_raceButton.ButtonPressed += _ButtonPressed;
			this.Controls.Add(_raceButton);
			//
			_boardButton = new ImageButton(3);
			_boardButton.Size = new Size(NavButtonWidth, NavButtonHeight);
			_boardButton.Location = new Point(_raceButton.Right + navButtonOffsetX, NavButtonOffsetY);
			_boardButton.SetToolTipText = "Tools Screen";
			_boardButton.SetVariants("\\images\\buttons\\network.png");
			_boardButton.ButtonPressed += _ButtonPressed;
			this.Controls.Add(_boardButton);
			//
			_reportsButton = new ImageButton(4);
			_reportsButton.Size = new Size(NavButtonWidth, NavButtonHeight);
			_reportsButton.Location = new Point(_boardButton.Right + navButtonOffsetX, NavButtonOffsetY);
			_reportsButton.SetToolTipText = "Reports Screen";
			_reportsButton.SetVariants("\\images\\buttons\\doc.png");
			_reportsButton.ButtonPressed += _ButtonPressed;
			this.Controls.Add(_reportsButton);

		}

		protected virtual void AddWindowControlButtons ()
		{
			_minButton = new ImageButton(6);
			_minButton.SetVariants("/images/buttons/minimise.png");
			_minButton.ButtonPressed += _ButtonPressed;
			_minButton.SetAutoSize();
			Controls.Add(_minButton);

			if (File.Exists(AppInfo.TheInstance.Location + @"\images\buttons\maximise.png"))
			{
				sizeButton = new ImageButton(8);
				sizeButton.SetVariants("/images/buttons/maximise.png");
				sizeButton.ButtonPressed += _ButtonPressed;
				sizeButton.SetAutoSize();
				Controls.Add(sizeButton);
			}

			_closeButton = new ImageButton(7);

			if (File.Exists(AppInfo.TheInstance.Location + @"\images\buttons\exit.png"))
			{
				_closeButton.SetVariants("/images/buttons/exit.png");
			}
			else
			{
				_closeButton.SetVariants("/images/buttons/close.png");
			}

			_closeButton.ButtonPressed += _ButtonPressed;
			_closeButton.SetAutoSize();
			Controls.Add(_closeButton);
		}

		protected virtual void AddTimeControlButtons ()
		{
			CreateGameControl();

			gameControl.SetState(false, true, false, false, true);
			gameControl.ButtonPressed += gameControl_ButtonPressed;
		}

		protected virtual void CreateGameControl ()
		{
			gameControl = new GlassGameControl();
			gameControl.Location = new Point(830, 728);
			gameControl.Size = new Size(153, 33);
			gameControl.BackColor = Color.Transparent;
			gameControl.Name = "Game Play/Pause/etc Buttons";
			this.Controls.Add(gameControl);
		}

		protected virtual void SizeChartScreen ()
		{
			SizeChartScreen(chartsScreen);
		}

		protected virtual void SizeChartScreen (Control screen)
		{
			SizeChartScreen(screen, true);
		}

		protected virtual void SizeChartScreen (Control screen, bool reload)
		{
			screen.Location = new Point(3, 40);
			screen.Size = new Size(1017, 677 + 10);

			if (reload
			    && (chartsScreen != null))
			{
				chartsScreen.ReloadDataAndShow(true);
			}
		}

		protected virtual void DoSize ()
		{
			int gap = 4;
			if (_closeButton != null)
			{
				_closeButton.Location = new Point(Width - gap - _closeButton.Width, gap);

				if (sizeButton != null)
				{
					sizeButton.Location = new Point(_closeButton.Left - gap - sizeButton.Width, gap);
					_minButton.Location = new Point(sizeButton.Left - gap - _minButton.Width, gap);

					if (TopLevelControl != null)
					{
						sizeButton.SetVariants("/images/buttons/" + ((((Form) TopLevelControl).WindowState == FormWindowState.Maximized)
							                       ? "restore"
							                       : "maximise") + ".png");
					}
				}
				else
				{
					_minButton.Location = new Point(_closeButton.Left - gap - _minButton.Width, gap);
				}
			}

			foreach (var screen in new Control []
				{ gameSelectionScreen, gameDetailsScreen, raceScreen, transScreen, toolsScreen, chartsScreen, IT_charts })
			{
				if (screen != null)
				{
					screen.Bounds = new Rectangle(0, 40, ClientSize.Width, ClientSize.Height - (40 * 2));
				}
			}

			if (gameControl != null)
			{
				gameControl.Height = 33;
				gameControl.Location = new Point(ClientSize.Width - gameControl.Width, ClientSize.Height - gameControl.Height);
			}

			if (teamLabel != null)
			{
				teamLabel.Location = new Point(400, ClientSize.Height - teamLabel.Height);
			}
		}

		void transScreen_PhaseFinished (object sender)
		{
			TheGameFile.AdvanceRound();

			// Now, because we are being called from an actual event queue inside the game we
			// can't just Dispose of all the objects in the game on this thread. We must instead
			// jump out but mark ourselves ready for doing the dirty on everyone...
			inGame = false;
			gameControl.SetState(false, false, false, false, false);
			gameControl.ResetButtons(true);

			if (! isUserInteractionDisabled)
			{
				Timer timer = new Timer();
				timer.Interval = 1;
				timer.Tick += transScreen_PhaseFinished_Timer_Tick;
				timer.Start();
			}
		}

		void transScreen_PhaseFinished_Timer_Tick (object sender, EventArgs e)
		{
			Timer timer = (Timer) sender;
			timer.Dispose();
			TimeManager.TheInstance.Stop();
			SetPlaying(false);
			this.SetMainButtonsLoadedGame(false);

			gameFile.Save(true);
		}

		protected Timer raceScreen_PhaseFinished_Timer;

		protected virtual void raceScreen_PhaseFinished (object sender)
		{
			TimeManager.TheInstance.Stop();

			TheGameFile.AdvanceRound();

			// Now, because we are being called from an actual event queue inside the game we
			// can't just Dispose of all the objects in the game on this thread. We must instead
			// jump out but mark ourselves ready for doing the dirty on everyone...
			inGame = false;

			//TimeManager.TheInstance.Stop();
			gameControl.SetState(false, false, false, false, false);
			gameControl.ResetButtons(true);

			if (! isUserInteractionDisabled)
			{
				raceScreen_PhaseFinished_Timer = new Timer();
				raceScreen_PhaseFinished_Timer.Interval = 1;
				raceScreen_PhaseFinished_Timer.Tick += raceScreen_PhaseFinished_Timer_Tick;
				raceScreen_PhaseFinished_Timer.Start();
			}
		}

		protected virtual void raceScreen_PhaseFinished_Timer_Tick (object sender, EventArgs e)
		{
			Timer timer = (Timer) sender;
			timer.Dispose();
			TimeManager.TheInstance.Stop();

			CleanupRaceScreen();
		}

		void CleanupRaceScreen ()
		{
			SetPlaying(false);
			SetMainButtonsLoadedGame(true);

			if (gameFile != null)
			{
				gameFile.Save(true);

				if (screenBanner != null)
				{
					screenBanner.EndOnMinute();
				}
			}
		}

		protected bool ConfirmOkToExitGame ()
		{
			if ((gameFile != null)
			    && playing
			    && IsGameScreenAccessible()
			    && HasGameStarted())
			{
				return (MessageBox.Show(this, "Are you sure you want to exit the round that is currently playing?", "Confirm exit",
					        MessageBoxButtons.YesNo) == DialogResult.Yes);
			}

			return true;
		}

		protected virtual void _ButtonPressed (object sender, ImageButtonEventArgs args)
		{
			switch (args.Code)
			{
				case 0: // Show Load / Create Game
					if (ConfirmOkToExitGame())
					{
						TimeManager.TheInstance.Stop();
						CurrentView = ViewScreen.GAME_SELECTION_SCREEN;
					}
					break;

				case 1: // Show Info
					if (ConfirmOkToExitGame())
					{
						TimeManager.TheInstance.Stop();
						CurrentView = ViewScreen.GAME_DETAILS_SCREEN;
					}
					break;

				case 2: // Show race
					CurrentView = (gameFile.CurrentPhase == GameFile.GamePhase.OPERATIONS)
						? ViewScreen.RACING_SCREEN
						: ViewScreen.TRANSITION_SCREEN;
					break;

				case 3: // Show board
					CurrentView = ViewScreen.NETWORK_SCREEN;
					break;

				case 4: // Show reports
					CurrentView = ViewScreen.REPORT_SCREEN;
					break;

				case 6: // Minimise
					((Form) this.Parent).WindowState = FormWindowState.Minimized;
					break;

				case 7: // Close
					if (CanDispose())
					{
						((Form) TopLevelControl).Close();
					}
					break;

				case 8:
					ToggleSize((Control) sender);
					break;
			}
		}

		void ToggleSize (Control button)
		{
			var form = (Form) TopLevelControl;

#if DEBUG
			var menu = new ContextMenu();
			foreach (var size in new []
			{
				new Size(1024, 768),
				new Size(1280, 1024),
				new Size(1600, 900),
				new Size(1600, 1200),
				new Size(1920, 1080)
			})
			{
				var item = new MenuItem(LibCore.CONVERT.Format("{0} x {1}", size.Width, size.Height), sizeItem_Click)
				{
					RadioCheck = true,
					Tag = size,
					Checked = ((form.Size == size) && (form.WindowState != FormWindowState.Maximized))
				};

				menu.MenuItems.Add(item);
			}

			menu.MenuItems.Add(new MenuItem("Full screen", sizeItem_Click)
			{
				RadioCheck = true,
				Tag = null,
				Checked = (form.WindowState == FormWindowState.Maximized)
			});

			menu.Show(sizeButton, new Point(0, 0));
#else
			if (form.WindowState == FormWindowState.Maximized)
			{
				form.WindowState = FormWindowState.Normal;
			}
			else
			{
				form.WindowState = FormWindowState.Maximized;
			}
#endif
		}

		void sizeItem_Click (object sender, EventArgs args)
		{
			var item = (MenuItem) sender;
			var form = (Form) TopLevelControl;

			if (item.Tag == null)
			{
				form.WindowState = FormWindowState.Maximized;
			}
			else
			{
				if (form.WindowState == FormWindowState.Maximized)
				{
					form.WindowState = FormWindowState.Normal;
				}

				form.Size = (Size) (item.Tag);
			}
		}

		protected virtual void gameControl_ButtonPressed (object sender, CommonGUI.GlassGameControl.ButtonAction action)
		{
			// The user has pressed one of the main game control buttons...
			switch (action)
			{
				case CommonGUI.GlassGameControl.ButtonAction.PrePlay:
					if (this.gameControl.GetPrePlayStatus())
					{
						StartPrePlay();
						this.gameControl.ClearPrePlay();
						ManualPrePlayEscape = false;
					}
					break;

				case CommonGUI.GlassGameControl.ButtonAction.Rewind:
				{
					if (! HasGameStarted())
					{
						// We have not actually started the game yet so always allow a rewind.
					}
					else
					{
						if (gameFile.IsNormalGame)
						{
							var playability = gameFile.Licence.GetPhasePlayability(gameFile.CurrentPhaseNumber);
							if (! playability.IsPlayable)
							{
								var note = playability.ReasonForUnplayability;
								if (playability.HasBeenPlayedTooManyTimes)
								{
									note = "You have reached the limit for the number of times to play this round.\r\nYou cannot rewind it again.";
								}

								note += "\r\nDo you want to quit the round?";

								using (CustomDialogBox dialog = new CustomDialogBox(CustomDialogBox.MessageType.Warning))
								{
									dialog.setText(note);
									dialog.setOKButtonText("End Round");
									dialog.ShowDialog(TopLevelControl);

									if (dialog.DialogResult != DialogResult.OK)
									{
										gameControl.SelectButton(GlassGameControl.ButtonAction.Pause);
										return;
									}
								}

								// They cannot play so take them to the game info screen...
								inGame = false;
								SetPlaying(false);
								this.CurrentView = ViewScreen.GAME_DETAILS_SCREEN;
								return;
							}
							else
							{
								var limitMessage = "";

								var limit = gameFile.Licence.PhasePlayLimit;
								if (limit != null)
								{
									var playsLeft = limit.Value - gameFile.Licence.GetTimesPhasePlayed(gameFile.CurrentPhaseNumber);
									limitMessage = $"\r\nYou can play this round {(Plurals.Format(playsLeft, "more time"))}.";
								}

								string note = "Replaying this round will reset the results.\r\nDo you want to continue?" + limitMessage;

								using (CustomDialogBox dialog = new CustomDialogBox(CustomDialogBox.MessageType.Warning))
								{
									dialog.setText(note);
									dialog.setOKButtonText("Rewind");
									dialog.ShowDialog(TopLevelControl);

									if (dialog.DialogResult != DialogResult.OK)
									{
										gameControl.SelectButton(GlassGameControl.ButtonAction.Pause);
										return;
									}
								}
							}
						}
						else if (gameFile.IsTrainingGame
						         && SkinningDefs.TheInstance.GetBoolData("access_all_training_rounds", false))
						{
							gameFile.CreateTrainingNetworkFile(gameFile.CurrentRound, gameFile.CurrentPhase);
						}
					}
					// Do a rewind...
					gameControl.SetState(false, true, false, false, true);
					DisposeOpsScreen();
					TimeManager.TheInstance.Stop();
					this.gameFile.Reset();

					if (gameFile.CurrentPhase == GameFile.GamePhase.OPERATIONS)
					{
						this.RunRace(this.gameFile.CurrentRound, true, false);
					}
					else
					{
						this.RunTransition(this.gameFile.CurrentRound, true);
					}

					SetPlaying(false);
					inGame = false;

					SetMainButtonsLoadedGame(true);
				}
					IncidentsImported = false;

					break;

				case CommonGUI.GlassGameControl.ButtonAction.Play:
				{
					ManualPrePlayEscape = true;
					EscapePrePlay();

					int seconds = this.gameFile.NetworkModel.GetNamedNode("CurrentTime").GetIntAttribute("seconds", 0);
					//
					if (seconds == 0)
					{
						if (gameFile.IsNormalGame)
						{
							// 23-05-07 ; Don't do this yet as the flash may just be playing.
							if (ShouldCheckLicenceNow())
							{
								if (! this.gameFile.CanPlayNow(gameFile.CurrentRound, gameFile.CurrentPhase))
								{
									MessageBox.Show(this, " This phase cannot be played any more.");
									return;
								}
							}
						}
					}

					// Start the game playing...
					if (gameFile.IsSalesGame)
					{
						if (gameFile.CurrentPhase == GameFile.GamePhase.OPERATIONS)
						{
							SetSpeed(1);
						}
						else
						{
							SetSpeed(4);
						}
					}
					else if (gameFile.IsTrainingGame)
					{
						SetSpeed(3);
					}
					else
					{
						SetSpeed(1);
					}

					//Set controls for game RUNNING
					gameControl.SetState(false, false, true, true, false);

					if (raceScreen != null)
					{
						raceScreen.Play();
					}
					else if (transScreen != null)
					{
						transScreen.Play();
					}

					SetPlaying(true);
					inGame = true;

					SetMainButtonsLoadedGame(true);
				}
					break;

				case CommonGUI.GlassGameControl.ButtonAction.Pause:
				{
					// Pause the game
					// If you cannot rewind the game don't enable the rewind button...
					if (! HasGameStarted())
					{
						// We have not yet actually started the game so always allow rewind...
						gameControl.SetState(true, true, false, false, false);
					}
					else if (! gameFile.Licence.GetPhasePlayability(gameFile.CurrentPhaseNumber).IsPlayable)
					{
						// We are playing the game but cannot rewind.
						// 26-7-2007 - We now want to show the rewind button but warn users
						// when they press it instead.
						gameControl.SetState(true, true, false, false, false);
					}
					else
					{
						// We are playing the game and can rewind.
						gameControl.SetState(true, true, false, false, false);
					}
					if (raceScreen != null)
					{
						raceScreen.Pause();
					}
					TimeManager.TheInstance.Stop();
				}
					break;

				case CommonGUI.GlassGameControl.ButtonAction.FastForward:
				{
					if (gameFile.IsNormalGame || ShouldWeShowFastForwardWarningForSalesAndTrainingGames())
					{
						bool shouldShowWarning = SkinningDefs.TheInstance.GetBoolData("should_show_fast_forward_warning", true);
						if (shouldShowWarning)
						{
							using (CustomDialogBox dialog = new CustomDialogBox(CustomDialogBox.MessageType.Warning))
							{
								dialog.setText("Are you sure you want to fast forward the game?");
								dialog.ShowDialog(TopLevelControl);

								if (dialog.DialogResult != DialogResult.OK)
								{
									gameControl.SelectButton(GlassGameControl.ButtonAction.Play);
									return;
								}
							}
						}



					}

					gameControl.SetState(false, false, true, false, false);
					if (raceScreen != null)
					{
						raceScreen.FastForward(gameControl.FastForwardSpeed);
						TimeManager.TheInstance.FastForward(gameControl.FastForwardSpeed);
					}
					else if (transScreen != null)
					{
						TimeManager.TheInstance.FastForward(gameControl.FastForwardSpeed);
					}
				}
					break;
			}
		}

		virtual protected bool ShouldCheckLicenceNow ()
		{
			return true;
		}

		protected virtual void SetSpeed (double speed)
		{
			TimeManager.TheInstance.FastForward(speed);
		}

		protected virtual bool ShouldWeShowFastForwardWarningForSalesAndTrainingGames ()
		{
			return false;
		}

		public void PlayCurrentPhase ()
		{
			if (gameDetailsScreen != null)
			{
				PlayPhase(gameDetailsScreen.PhaseSelector.SelectedPhase);
			}
			else
			{
				PlayPhase(gameFile.CurrentPhaseNumber);
			}
		}

		void PlayPhase (int phase)
		{
			// Fix for Case 4604:   CA: clock appears on title bar when not all game details entered
			if (null != gameDetailsScreen)
			{
				if (! gameDetailsScreen.SetupGame()) return;
			}

			if (! gameFile.Licence.GetPhasePlayability(phase).IsPlayable)
			{
				MessageBox.Show(TopLevelControl, "This round is not available to play.", "Error Starting Round",
					MessageBoxButtons.OK);
				return;
			}

			int round;
			GameFile.GamePhase gamePhase;

			gameFile.PhaseToRound(phase, out round, out gamePhase);

			if (gamePhase == GameFile.GamePhase.OPERATIONS)
			{
				RunRace(round, true, false);
				CurrentView = ViewScreen.RACING_SCREEN;
			}
			else
			{
				RunTransition(round, true);
				CurrentView = ViewScreen.TRANSITION_SCREEN;
			}
		}

		protected virtual void gameDetailsScreen_PlayPressed (int phase)
		{
			PlayPhase(phase);
		}

		#region IGameLoader Members

		protected SupportSpendOverrides supportOverrides;

		public virtual void LoadGame (NetworkProgressionGameFile _gameFile, bool isTrainingGame)
		{
			Console.WriteLine($"Loading game file {_gameFile.Name}");
			_isTrainingGame = isTrainingGame;
			gameFile = _gameFile;

			supportOverrides = new SupportSpendOverrides(gameFile);
			this.CurrentView = ViewScreen.GAME_DETAILS_SCREEN;
		}

		#endregion

		protected void raceScreen_PlayPressed (GameFile game)
		{
			OnPlayPressed();
		}

		protected void OnPlayPressed ()
		{
			PlayPressed?.Invoke(gameFile);
		}

		protected void gameSelectionScreen_GameCreated (GameFile game)
		{
		}

		protected bool gameDetailsScreen_GameSetup (NetworkProgressionGameFile game)
		{
			if (! _isTrainingGame && (null != this.GameSetup))
			{
				switch (GameSetup(game))
				{
					case GameStartType.CouponAlreadyExisted:
						return true;

					case GameStartType.Failed:
						return false;

					case GameStartType.MintedNewCoupon:
						return true;
				}
			}

			return true;
		}

		protected bool gameSelectionScreen_PasswordCheck (string password)
		{
			if (null != PasswordCheck)
			{
				return PasswordCheck(password);
			}

			return true;
		}

		protected void SetPrePlayTime (int timeval)
		{
			Node _PrePlayControlNode = gameFile.NetworkModel.GetNamedNode("preplay_control");
			_PrePlayControlNode.SetAttribute("time_ref", CONVERT.ToStr(timeval));
		}

		protected virtual void StartPrePlay ()
		{
			Node _PrePlayControlNode = gameFile.NetworkModel.GetNamedNode("preplay_control");
			_PrePlayControlNode.SetAttribute("start", "true");
		}

		protected virtual void EscapePrePlay ()
		{
			using (var cursor = new WaitCursor (this))
			{
				if ((_PrePlayStatusNode != null)
				    && _PrePlayStatusNode.GetBooleanAttribute("preplay_running", false))
				{
					Node _PrePlayControlNode = gameFile.NetworkModel.GetNamedNode("preplay_control");
					_PrePlayControlNode.SetAttribute("escape", true);
				}
			}
		}

		public void ExtractPrePlaySystemValues (Boolean isRacePhase, out Boolean status, out int duration)
		{
			string statusstr = string.Empty;
			string timestr = string.Empty;

			status = false;
			duration = 0;

			if (isRacePhase)
			{
				statusstr = CoreUtils.SkinningDefs.TheInstance.GetData("race_preplay_status");
				timestr = CoreUtils.SkinningDefs.TheInstance.GetData("race_preplay_time");
			}
			else
			{
				statusstr = CoreUtils.SkinningDefs.TheInstance.GetData("transition_preplay_status");
				timestr = CoreUtils.SkinningDefs.TheInstance.GetData("transition_preplay_time");
			}

			status = false;
			duration = CONVERT.ParseInt(timestr);
			if (statusstr.ToLower() == "true")
			{
				status = true;
			}
		}

		public virtual void AttachPrePlaySystem (Boolean UseSystem, int PreplayTime)
		{
			DisposePrePlaySystem();
			if (UseSystem)
			{
				_PrePlayStatusNode = gameFile.NetworkModel.GetNamedNode("preplay_status");
				_PrePlayStatusNode.AttributesChanged += _PrePlayStatusNode_AttributesChanged;
				this.gameControl.SetPrePlayIgnore(true);
				SetPrePlayTime(PreplayTime);
			}
			else
			{
				this.gameControl.SetPrePlayIgnore(false);
				_PrePlayStatusNode = null;
			}
		}

		public void DisposePrePlaySystem ()
		{
			if (_PrePlayStatusNode != null)
			{
				_PrePlayStatusNode.AttributesChanged -= _PrePlayStatusNode_AttributesChanged;
				_PrePlayStatusNode = null;
			}
		}

		protected virtual void _PrePlayStatusNode_AttributesChanged (Node sender, ArrayList attrs)
		{
			if (attrs != null && attrs.Count > 0)
			{
				foreach (AttributeValuePair avp in attrs)
				{
					if (avp.Attribute == "time_left")
					{
						int time_left = sender.GetIntAttribute("time_left", 0);
						//System.Diagnostics.Debug.WriteLine("####PREPLAY "+time_left.ToString());
						if ((time_left == 0) && (ManualPrePlayEscape == false))
						{
							this.gameControl.ClearPrePlay();
							gameControl_ButtonPressed(null, CommonGUI.GlassGameControl.ButtonAction.Play);
						}
					}
				}
			}
		}

		Color ExtractColor (string rgbstr)
		{
			Color errColor = Color.White;
			string [] parts = null;
			int RedFactor = 0;
			int GreenFactor = 0;
			int BlueFactor = 0;

			if (rgbstr != string.Empty)
			{
				parts = rgbstr.Split(',');
				if (parts.Length == 3)
				{
					RedFactor = CONVERT.ParseInt(parts[0]);
					GreenFactor = CONVERT.ParseInt(parts[1]);
					BlueFactor = CONVERT.ParseInt(parts[2]);
					errColor = System.Drawing.Color.FromArgb(RedFactor, GreenFactor, BlueFactor);
				}
			}
			return errColor;
		}

		public BaseCompleteGamePanel (IProductLicence productLicence, IProductLicensor productLicensor)
		{
			Media.SoundPlayer.SetContainer(TopLevelControl ?? this, false);
			ModelAction_IfConfirm.TopLevelControl = this;

			BackColor = Color.White;

			this.productLicence = productLicence;
			NetworkProgressionGameFile.SetProductLicence(productLicence);
			this.productLicensor = productLicensor;

			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.DoubleBuffer, true);
			SetStyle(ControlStyles.UserPaint, true);

			string fontname = SkinningDefs.TheInstance.GetData("fontname");
			MyDefaultSkinFontBold12 = ConstantSizeFont.NewFont(fontname, 12, FontStyle.Bold);
			MyDefaultSkinFontBold14 = ConstantSizeFont.NewFont(fontname, 14, FontStyle.Bold);

			teamLabel = new Label();
			teamLabel.Size = new Size(400, 40);
			teamLabel.TextAlign = ContentAlignment.MiddleRight;
			teamLabel.Text = "";
			teamLabel.BackColor = Color.Transparent;
			teamLabel.Location = new Point(400, 768 - 40);
			teamLabel.Font = MyDefaultSkinFontBold14;
			teamLabel.Visible = false;
			if (! SkinningDefs.TheInstance.GetBoolData("esm_sim", false))
			{
				this.Controls.Add(teamLabel);
			}
			LoadTopAndBottomBars();

			//
			// If our "ignore_atts.txt" or "ignore_targets.txt" do not exit 
			// then copy over the default ones.
			// 
			//

			//Ignore (dont log) the changes to these attributes
			string default_fi_types = AppInfo.TheInstance.Location + "\\data\\default_ignore_types.txt";
			string fi_types = AppInfo.TheInstance.Location + "\\data\\ignore_types.txt";
			// User makes it read only if they don't want the default defs.
			if (File.Exists(default_fi_types))
			{
				File.Copy(default_fi_types, fi_types, true);
			}

			//Ignore (dont log) the changes to these attributes
			string default_fi_atts = AppInfo.TheInstance.Location + "\\data\\default_ignore_atts.txt";
			string fi_atts = AppInfo.TheInstance.Location + "\\data\\ignore_atts.txt";
			// User makes it read only if they don't want the default defs.
			File.Copy(default_fi_atts, fi_atts, true);

			//Ignore (dont log) the changes to these named targets
			string default_fi_targets = AppInfo.TheInstance.Location + "\\data\\default_ignore_targets.txt";
			string fi_targets = AppInfo.TheInstance.Location + "\\data\\ignore_targets.txt";
			// User makes it read only if they don't want the default defs.
			File.Copy(default_fi_targets, fi_targets, true);

			//Ignore (dont log) the node movements of these named nodes 
			string default_fi_movingtargets = AppInfo.TheInstance.Location + "\\data\\default_ignore_movingtargets.txt";
			string fi_movingtargets = AppInfo.TheInstance.Location + "\\data\\ignore_movingtargets.txt";
			// User makes it read only if they don't want the default defs.
			if (File.Exists(default_fi_movingtargets))
			{
				File.Copy(default_fi_movingtargets, fi_movingtargets, true);
			}

			AppAssemblyDir = Assembly.GetExecutingAssembly().CodeBase;
			AppExeDir = Path.GetDirectoryName(AppAssemblyDir.Replace(@"file:///", ""));

			AddMainButtons();
			InstantiateScreens();

			bannerForeColor = SkinningDefs.TheInstance.GetColorData("bannerForeColor");
			bannerPrePlayForeColor = SkinningDefs.TheInstance.GetColorData("bannerPrePlayForeColor");

			CreatePageTitle();

			CurrentView = ViewScreen.GAME_SELECTION_SCREEN;

			DoSize();
		}

		protected virtual void CreatePageTitle ()
		{
			Color pagetitleForecolor = SkinningDefs.TheInstance.GetColorData("admin_pages_title_forecolor");

			pageTitle = new Label();
			pageTitle.Location = new Point(pageTitleLeft, pageTitleTop);
			pageTitle.Size = new Size(544, 32);
			pageTitle.Visible = false;
			pageTitle.Font = MyDefaultSkinFontBold12;
			pageTitle.TextAlign = ContentAlignment.MiddleCenter;
			pageTitle.ForeColor = pagetitleForecolor;
			pageTitle.BackColor = Color.Transparent;
			pageTitle.MouseDown += pageTitle_MouseDown;
			this.Controls.Add(pageTitle);

#if DEBUG
			LibCore.PanelLabeller.LabelControl(pageTitle);
#endif
		}

		protected virtual void LoadTopAndBottomBars ()
		{

			//string fn = AppInfo.TheInstance.Location + "\\images\\panels\\top_bar_game.png";
			top_bar_game = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\panels\\top_bar_game.png");
			top_bar_game_clean =
				Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\panels\\top_bar_game_clean.png");
			top_bar_normal =
				Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\panels\\top_bar_normal.png");
			bottom_bar = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\panels\\bottom_bar.png");

			training_top_bar_game =
				Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\panels\\t_top_bar_game.png");
			training_bottom_bar_normal =
				Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\panels\\t_bottom_bar.png");
		}


		/// <summary>
		/// Override on Paint
		/// </summary>
		/// <param name="e"></param>
		protected override void OnPaint (PaintEventArgs e)
		{
			Graphics g = e.Graphics;

			if ((CurrentView == ViewScreen.RACING_SCREEN) || (CurrentView == ViewScreen.TRANSITION_SCREEN))
			{
				if (this._isTrainingGame)
				{
					g.DrawImage(training_top_bar_game, 0, 0, 1024, 40);
				}
				else
				{
					g.DrawImage(top_bar_game, 0, 0, 1024, 40);
				}
			}
			else
			{
				g.DrawImage(top_bar_normal, 0, 0, 1024, 40);
			}

			var bottomBar = new Rectangle(0, ClientSize.Height - 40, training_bottom_bar_normal.Width, 40);
			if ((CurrentView == ViewScreen.RACING_SCREEN) || (CurrentView == ViewScreen.TRANSITION_SCREEN))
			{
				if (this._isTrainingGame)
				{
					g.DrawImage(training_bottom_bar_normal, bottomBar);
				}
				else
				{
					g.DrawImage(bottom_bar, bottomBar);
				}
			}
			else
			{
				g.DrawImage(bottom_bar, bottomBar);
			}
		}

		protected void gameDetailsScreen_SkipPressed (int phase)
		{
			int round;
			GameFile.GamePhase gamePhase;

			gameFile.PhaseToRound(phase, out round, out gamePhase);

			if (! gameFile.Licence.GetPhasePlayability(phase).IsPlayable)
			{
				MessageBox.Show(TopLevelControl, "Round not available to play", "Error Starting Round", MessageBoxButtons.OK);
				return;
			}

			if (gamePhase == GameFile.GamePhase.OPERATIONS)
			{
				// Should not happen!
			}
			else
			{
				RunRace(round, true, true);
				this.CurrentView = ViewScreen.RACING_SCREEN;
			}
		}

		protected override void OnGotFocus (EventArgs e)
		{
			if (null != this.raceScreen)
			{
				raceScreen.Focus();
			}
			else if (null != this.transScreen)
			{
				transScreen.Focus();
			}
		}

		public bool RefreshActivation ()
		{
			productLicence.BeginRefreshStatus();
			return true;
		}

		public bool ResetActivation (string password)
		{
			try
			{
				productLicence.Deactivate();
				return true;
			}
			catch (Exception e)
			{
				MessageBox.Show(this, "Cannot Reset License. " + e.Message, "Error", MessageBoxButtons.OK);
				return false;
			}
		}

		public void QuickStartGame ()
		{
			if (CurrentView == ViewScreen.GAME_SELECTION_SCREEN)
			{
				gameSelectionScreen.QuickStartGame();
				CurrentView = ViewScreen.GAME_DETAILS_SCREEN;
			}

			if (CurrentView == ViewScreen.GAME_DETAILS_SCREEN)
			{
				gameDetailsScreen.QuickStartGame();
			}
		}

		public void RefreshMaturityScoreSet ()
		{
			if (toolsScreen != null)
			{
				toolsScreen.RefreshMaturityScoreSet();
				DoSize();
			}
		}

		protected virtual void SetPlaying (bool playing)
		{
			this.playing = playing;

			if (! playing)
			{
				inGame = false;
			}
		}

		public virtual bool HasGameStarted ()
		{
			bool knowForSureWeveStarted = ((raceScreen != null)
			                               && raceScreen.HasGameStartedToOurKnowledge());

			bool guessFromTimeWeveStarted = ((gameFile != null)
				                             && (gameFile.NetworkModel != null)
											 && ((raceScreen != null) || (transScreen != null))
											 && (gameFile.NetworkModel.GetNamedNode("CurrentTime").GetIntAttribute("seconds", -1) > 0));

			bool gameHasFinished = ((raceScreen != null) && raceScreen.IsGameFinished())
			                       || ((transScreen != null) && transScreen.IsGameFinished());

			bool onAGameScreen = (raceScreen != null) || (transScreen != null);

			return onAGameScreen
			       && (knowForSureWeveStarted || guessFromTimeWeveStarted)
				   && ! gameHasFinished;
		}

		public virtual void ImportIncidents (string incidentsFile)
		{
			if (raceScreen != null)
			{
				raceScreen.ImportIncidents(incidentsFile);
			}
			else if (transScreen != null)
			{
				transScreen.ImportIncidents(incidentsFile);
			}
			else
			{
				MessageBox.Show(TopLevelControl, "You must be on the Game Screen to import incidents.", "Import Incidents error");
			}
		}

		public virtual void ExportIncidents ()
		{
			if (gameFile == null)
			{
				MessageBox.Show(TopLevelControl, "You must have a game open to export the incidents.", "Export Incidents error");
				return;
			}

			GenericExtractIncidents.IncidentExtractor extractor = new GenericExtractIncidents.IncidentExtractor (gameFile);
			int round = gameFile.LastRoundPlayed;

			using (SaveFileDialog dialog = new SaveFileDialog
			{
				InitialDirectory = System.Environment.GetFolderPath(System.Environment.SpecialFolder.DesktopDirectory),
				FileName = CONVERT.Format("round_{0}_incidents_{1}_{2}.xml",
												 round,
												 CoreUtils.SkinningDefs.TheInstance.GetData("gametype"),
												 gameFile.GetTitle()),

				Filter = "XML files (*.xml)|*.xml",
				RestoreDirectory = true
			})
			{
				if (dialog.ShowDialog(this) == DialogResult.OK)
				{
					extractor.ExtractRoundIncidents(gameFile.NetworkModel, round, dialog.FileName);
				}
			}
		}

		public virtual void ExportAllRoundIncidents ()
		{
			if (gameFile == null)
			{
				MessageBox.Show(TopLevelControl, "You must have a game open to export the incidents.", "Export Incidents error");
				return;
			}

			GenericExtractIncidents.IncidentExtractor extractor = new GenericExtractIncidents.IncidentExtractor(gameFile);

			using (var dialog = new FolderBrowserDialog
			{
				Description = "Select folder to save incidents to",
				SelectedPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.DesktopDirectory),
				ShowNewFolderButton = true
			})
			{
				if (dialog.ShowDialog(this) == DialogResult.OK)
				{
					for (var round = 1; round <= gameFile.LastRoundPlayed; round++)
					{
						UseWaitCursor = true;
						extractor.ExtractRoundIncidents(gameFile.GetNetworkModel(round), round, $"{dialog.SelectedPath}\\{gameFile.GetTitle()}_incidents_r{round}.xml");
						UseWaitCursor = false;
					}
				}
			}

			MessageBox.Show(TopLevelControl, "Export Incidents completed.", "Export Incidents");
		}

		public void ExportAnalytics ()
		{
			if (gameFile == null)
			{
				MessageBox.Show(TopLevelControl, "You must have a game open to export the analytics.", "Export Analytics error");
				return;
			}

			using (var dialog = new SaveFileDialog
			{
				InitialDirectory = System.Environment.GetFolderPath(System.Environment.SpecialFolder.DesktopDirectory),
				FileName = "Analytics.xlsx",
				Filter = "Excel files (*.xlsx)|*.xlsx",
				RestoreDirectory = true
			})
			{
				if (dialog.ShowDialog(TopLevelControl) == DialogResult.OK)
				{
					ExportAnalytics(dialog.FileName);
				}
			}
		}

		public virtual void ExportAnalytics (string filename)
		{
		}

		public virtual void ImportAllRoundIncidents ()
		{
			if (gameFile == null)
			{
				QuickStartGame();
			}

			var roundToIncidentsFile = new Dictionary<int, string> ();

			using (var dialog = new OpenFileDialog
			{
				InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
				Filter = "Game Phase Incident File (*.xml)|*.xml",
				FilterIndex = 1,
				RestoreDirectory = true
			})
			{
				var finished = false;
				var round = gameFile.LastRoundPlayed + 1;

				do
				{
					dialog.Title = $"Import Round {round} Incident File";
					if (dialog.ShowDialog(TopLevelControl) == DialogResult.OK)
					{
						roundToIncidentsFile.Add(round, dialog.FileName);
						round++;

						if (round > gameFile.GetTotalRounds())
						{
							finished = true;
						}
					}
					else
					{
						finished = true;
					}
				}
				while (! finished);
			}

			var orderedRounds = new List<int> (roundToIncidentsFile.Keys);
			orderedRounds.Sort();

			foreach (var round in orderedRounds)
			{
				if (raceScreen == null)
				{
					PlayCurrentPhase();
				}

				CurrentView = ViewScreen.RACING_SCREEN;

				ImportIncidents(roundToIncidentsFile[round]);
				RunToEndOfPhase();
			}

			MessageBox.Show(TopLevelControl, "Incident Import successful", "Import Incidents");
		}

		public virtual void RefreshScreenSelectionButtons ()
		{
			if (toolsScreen != null)
			{
				toolsScreen.Hide();
				toolsScreen.Dispose();
				toolsScreen = null;
			}

			if (chartsScreen != null)
			{
				chartsScreen.Dispose();
				chartsScreen = null;
			}

			if (IT_charts != null)
			{
				IT_charts.Dispose();
				IT_charts = null;
			}

			SetMainButtonsLoadedGame(false);
		}

		protected virtual string GetFastForwardWarning ()
		{
			return "Are you sure you want to fast forward the game?";
		}

		public virtual void ShowTeamLabel (bool show)
		{
			teamLabel.Visible = show;
		}

	    protected override void OnSizeChanged (EventArgs e)
	    {
	        base.OnSizeChanged(e);

            DoSize();
            Invalidate();
	    }

	    void pageTitle_MouseDown (object sender, MouseEventArgs e)
	    {
	        if (e.Button == MouseButtons.Left)
	        {
	            ((Form) TopLevelControl).DragMove();
	        }
	    }

	    protected override void OnMouseDown (MouseEventArgs e)
	    {
	        base.OnMouseDown(e);

	        if (e.Button == MouseButtons.Left)
	        {
	            ((Form) TopLevelControl).DragMove();
	        }
	    }

	    protected virtual void ShowPageTitle (string title)
	    {
	        pageTitle.Text = title;
            pageTitle.Show();
	    }

	    protected IList<Control> GetNavigationButtons ()
	    {
	        return (new Control []
	            {
	                _loadButton, _infoButton, _raceButton, _boardButton, _reportsButton, _reportsButton2
	            })
	            .Where(a => ((a != null) && a.Visible)).ToList();
	    }
	    protected IList<Control> GetWindowButtons ()
	    {
	        return (new Control[]
	            {
	                _closeButton, sizeButton, _minButton
	            })
	            .Where(a => a != null).ToList();
	    }

		public void LoadGameDetails (string filename)
		{
			File.Copy(filename, gameFile.GetGlobalFile("details.xml"), true);
		}

		public void LoadTeamDetails (string filename)
		{
			File.Copy(filename, gameFile.GetGlobalFile("team.xml"), true);
		}

		public void LoadFacilitatorLogo (string filename)
		{
			File.Copy(filename, gameFile.GetGlobalFile("facil_logo.png"), true);
		}

		public void LoadClientLogo (string filename)
		{
			File.Copy(filename, gameFile.GetGlobalFile("logo.png"), true);
		}

		bool isUserInteractionDisabled;

		public bool IsUserInteractionDisabled
		{
			get => isUserInteractionDisabled;

			set
			{
				isUserInteractionDisabled = value;
			}
		}

		public void RunToEndOfPhase ()
		{
			if (raceScreen != null)
			{
				gameControl_ButtonPressed(this, GlassGameControl.ButtonAction.PrePlay);
			}

			gameControl_ButtonPressed(this, GlassGameControl.ButtonAction.Play);

			TimeManager.TheInstance.FastForward(10000);

			while (inGame)
			{
				Application.DoEvents();
			}

			CurrentView = ViewScreen.GAME_DETAILS_SCREEN;
		}

		public void StartRoundPlaying ()
		{
			if (raceScreen != null)
			{
				gameControl_ButtonPressed(this, GlassGameControl.ButtonAction.PrePlay);
			}

			gameControl_ButtonPressed(this, GlassGameControl.ButtonAction.Play);
		}

		public virtual List<RoundScores> GetRoundScores ()
		{
			var allScores = new List<RoundScores> ();

			int previousProfit = 0;
			int newServices = 0;
			for (int round = 1; round <= gameFile.LastRoundPlayed; round++)
			{
				var scores = new RoundScores (gameFile, round, previousProfit, newServices, supportOverrides);
				allScores.Add(scores);
				previousProfit = scores.Profit;
				newServices = scores.NumNewServices;
			}

			return allScores;
		}

		public abstract void GenerateAllReports (string folder);

		public void CopyGameFile (string copyFilename)
		{
			gameFile.Save(true);
			File.Copy(gameFile.FileName, copyFilename, true);
		}

		public void CreateAndLoadGame (IGameLicence gameLicence)
		{
			gameSelectionScreen.CreateAndLoadGame(gameLicence);
		}

		public virtual void ScreenGrabAllReports (string folder)
		{
			var window = (Form) TopLevelControl;
			var oldView = CurrentView;
			var oldState = window.WindowState;

			window.WindowState = FormWindowState.Maximized;
			CurrentView = ViewScreen.REPORT_SCREEN;

			var top = new TopLevelProgressPanel { Bounds = new Rectangle (0, 0, window.ClientSize.Width, window.ClientSize.Height) };
			window.Controls.Add(top);
			top.BringToFront();

			var reports = chartsScreen.GetAllAvailableReports();
			top.ProgressMax = reports.Count;

			for (int i = 0; i < reports.Count; i++)
			{
				top.ProgressCount = i;
				top.Refresh();

				var report = reports[i];
				chartsScreen.ShowReport(report);

				using (var bitmap = new Bitmap (chartsScreen.Width, chartsScreen.Height))
				{
					chartsScreen.DrawToBitmap(bitmap, new Rectangle (0, 0, chartsScreen.Width, chartsScreen.Height));
					bitmap.Save(folder + $@"\Report_{report.Name}.png");
				}
			}

			top.Dispose();

			CurrentView = oldView;
			window.WindowState = oldState;
		}

		public virtual void AttachToParent ()
		{
		}
	}
}