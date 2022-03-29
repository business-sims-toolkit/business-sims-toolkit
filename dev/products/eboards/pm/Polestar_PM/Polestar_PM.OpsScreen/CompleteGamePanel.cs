using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Reflection;
using System.IO;
using System.Data;

using GameManagement;
using TransitionScreens;
using LibCore;
using Network;
using ChartScreens;
using CoreUtils;
using NetworkScreens;
using CommonGUI;
using GameDetails;
using CoreScreens;
using Polestar_PM.TransScreen;
using Polestar_PM.ReportsScreen;
using Polestar_PM.OpsGUI;

using IncidentManagement;
using Licensor;

namespace Polestar_PM.OpsScreen
{
    /// <summary>
    /// Summary description for CompleteGamePanel.
    /// </summary>
    public class CompleteGamePanel : BaseCompleteGamePanel
    {
        protected Image top_bar_transition;
        protected Image top_bar_transition_training;
        protected Image top_bar_game_training;
        protected Image bottom_bar_game;
        protected Image bottom_bar_powered;

        protected RaceEndEventScreen RaceEndScreen = null;

        public CompleteGamePanel(IGameLicence gameLicence, TacPermissions tacPermissions, bool usepass)
            : base(null, null)
        {
            top_bar_transition = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "images\\panels\\top_bar_trans.png");
            top_bar_transition_training = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "images\\panels\\top_bar_trans_training.png");
            top_bar_game_training = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "images\\panels\\top_bar_game_training.png");
            bottom_bar_game = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "images\\panels\\bottom_bar_game.png");
            bottom_bar_powered = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "images\\panels\\bottom_bar_powered.png");

            //Changing the Navigation buttons to match the PoleStar Images
            _loadButton.Size = new Size(37, 30);
            _infoButton.Size = new Size(37, 30);
            _raceButton.Size = new Size(37, 30);
            _boardButton.Size = new Size(37, 30);
            _reportsButton.Size = new Size(37, 30);

            //Adjust the position to butt the buttons together
            _loadButton.Left = 8 + 37 * 0;
            _infoButton.Left = 8 + 37 * 1;
            _raceButton.Left = 8 + 37 * 2;
            _boardButton.Left = 8 + 37 * 3;
            _reportsButton.Left = 8 + 37 * 4;

            _loadButton.Top = 5;
            _infoButton.Top = 5;
            _raceButton.Top = 5;
            _boardButton.Top = 5;
            _reportsButton.Top = 5;

            //Application Minimise and Close Have Pole Star Images as well
            _minButton.Size = new Size(27, 23);
            _minButton.Location = new Point(981 - 15, 18 - 11);

            //Adjust the position to butt the buttons together
            _closeButton.Size = new Size(27, 23);
            _closeButton.Location = new Point((981 - 15) + 26, 18 - 11);

            //Adjust the Screen Banner position
            screenBannerLeft = 240;
            screenBannerTop = 8;
            this.pageTitle.Left = this.pageTitle.Left + 20;

            //Adjust the game control buttons
            //this.gameControl.Top = this.gameControl.Top + 6;
            this.gameControl.AdjustPositionAndWidthResize(37, 33, 1);
            // Our base constructor may have done things to the first reports button before
            // the second one was created, so sync the status now.
            EnableReportsButton(_reportsButton.Enabled);

            gameControl.BringToFront();
        }

        protected override void CreateOpsBanner(string DefaultPhaseName, bool showDay, bool isRaceScreen)
        {
            screenBanner = new PM_OpsScreenBanner(gameFile.NetworkModel, true);
            screenBanner.Round = gameFile.CurrentRound;
            //screenBanner.Location = new Point(screenBannerLeft,screenBannerTop);
            screenBanner.Location = new Point(300, screenBannerTop);
            screenBanner.Size = new Size(420, 32);
            screenBanner.ChangeBannerTextForeColour(bannerForeColor);
            screenBanner.ChangeBannerPrePlayTextForeColour(bannerPrePlayForeColor);
            screenBanner.SetRaceViewOn(isRaceScreen);
            screenBanner.Phase = DefaultPhaseName;
            screenBanner.Name = "Ops Screen Banner";
            //screenBanner.BackColor = Color.Pink;
            this.Controls.Add(screenBanner);
            screenBanner.BringToFront();
        }

        public override void EnableReportsButton(bool enable)
        {
            _reportsButton.Enabled = enable;
        }

        protected override TransitionScreen BuildTranisitionScreen(int round)
        {
            TransitionScreen ts = (TransitionScreen)new MSTransitionScreen(gameFile, AppInfo.TheInstance.Location + "\\data");
            ts.BuildObjects(round, !gameFile.IsSalesGame);
            return ts;
        }
        protected override EditGamePanel CreateGameDetailsScreen()
        {
            EditGamePanel egp = new PSPM_EditGamePanel(this.gameFile, this);
            egp.ShowTeamName(false);
            return egp;
        }

        protected override OpsPhaseScreen CreateOpsPhaseScreen(NetworkProgressionGameFile gameFile, bool isTrainingGame, string gameDir)
        {
            RaceEndScreen = new RaceEndEventScreen(this, gameFile, isTrainingGame, gameDir);
            RaceEndScreen.Location = new Point(0, 40);
            RaceEndScreen.Size = new Size(1024, 728);
            RaceEndScreen.Visible = false;
            RaceEndScreen.endeventcompleted += new Polestar_PM.OpsScreen.RaceEndEventScreen.EndEventCompletedEvent(RaceEndScreen_endeventcompleted);
            this.Controls.Add(RaceEndScreen);

            TradingOpsScreen top;
            top = new TradingOpsScreen(this, gameFile, isTrainingGame, gameDir);
            top.ModalAction += new TradingOpsScreen.ModalActionHandler(top_ModalAction);

            top.SetGameControl(this.gameControl);
            top.Top = 40;
            return top;
        }

        public void top_ModalAction(bool entering)
        {
            this.SuspendMainNavigation(entering);
            this.gameControl.SuspendButtonsForModal(entering);
        }

        protected override PureTabbedChartScreen CreateChartScreen()
        {
            return new PM_TabbedChartScreen(gameFile, this.supportOverrides, null);
        }

        protected override BaseTabbedChartScreen CreateITChartScreen()
        {
            return new PM_IT_TabbedChartScreen(gameFile, this.supportOverrides);
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

        protected override void SizeChartScreen(Control screen)
        {
            screen.Location = new Point(0, 50 - 10);
            screen.Size = new Size(Width - screen.Left, Height - 40 - screen.Top);
        }

        protected override void handleRaceScreenHasGameStarted()
        {
            setITChoiceConfirmed();
            base.handleRaceScreenHasGameStarted();
        }

        protected override void raceScreen_PhaseFinished(object sender)
        {
            // Now, because we are being called from an actual event queue inside the game we
            // can't just Dispose of all the objects in the game on this thread. We must instead
            // jump out but mark ourselves ready for doing the dirty on everyone...
            inGame = false;
            TimeManager.TheInstance.Stop();

            //need to Activate the RunRace Button
            GlassGameControl_PM gc = this.gameControl as GlassGameControl_PM;

            gc.SetState(false, false, false, false, false, true);

            this.gameFile.Save(true);

            //
            Timer timer = new Timer();
            timer.Interval = 1;
            timer.Tick += new EventHandler(raceScreen_PhaseFinished_Timer_Tick);
            timer.Start();
        }

        // old incomplete comment
        // Alert This method also exsits in the the CompleteGamePanel.cs for polestar. 
        // After some consideration it may be worh making it part of the 

        protected override void raceScreen_PhaseFinished_Timer_Tick(object sender, EventArgs e)
        {
            //The Normal Way just call the base class to stop the timeManager and tidy Up
            //base.raceScreen_PhaseFinished_Timer_Tick(sender,e); 

            //In PM, we do the usual 
            Timer timer = (Timer)sender;
            timer.Dispose();
            // Stop the game and show the cars flying over the finish line instead.
            TimeManager.TheInstance.Stop();
            this.raceScreen.Pause();

            CurrentView = ViewScreen.RACING_SCREEN;
            SetAllMainButtonsFalse();
        }

        private void HandleReaceEndButton()
        {
            RaceEndScreen.LoadData();
            RaceEndScreen.Visible = true;
            RaceEndScreen.BringToFront();
        }

        private void RaceEndScreen_endeventcompleted()
        {
            // TODO : Screen showing the end of the race.

            RaceEndScreen.Visible = false;
            RaceEndScreen.BringToFront();

            GlassGameControl_PM gc = this.gameControl as GlassGameControl_PM;
            gc.SetState(false, false, false, false, false, false);

            //Standard
            SetPlaying(false);
            this.SetMainButtonsLoadedGame(false);

            //base.raceScreen_PhaseFinished_Timer_Tick(sender,e); 
            //Standard
            CurrentView = ViewScreen.REPORT_SCREEN;

        }

        //Need and override to handle the end of round 
        protected void SetAllMainButtonsFalse()
        {
            _loadButton.Enabled = false;
            _infoButton.Enabled = false;
            _raceButton.Enabled = false;
            _boardButton.Enabled = false;
            EnableReportsButton(false);
        }

        //Need and override to handle the end of round 
        protected override void SetMainButtonsLoadedGame(bool overrideRaceEnabled)
        {
            // We need to create the tools screen to know whether its icon needs enabled.
            if (toolsScreen == null)
            {
                CreateToolsScreen();
            }

            if (playing)
            {
                _loadButton.Enabled = false;
                _infoButton.Enabled = false;
                _raceButton.Enabled = true;
                _boardButton.Enabled = true;
                EnableReportsButton(true);
            }
            else
            {
                _loadButton.Enabled = true;
                _infoButton.Enabled = true;
                _raceButton.Enabled = false;
                _boardButton.Enabled = true;
                EnableReportsButton(true);
            }

            if (overrideRaceEnabled)
            {
                _raceButton.Enabled = true;
            }
        }

        /// <summary>
        /// Override on Paint
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            //Drawing the top Bar
            if ((CurrentView == ViewScreen.RACING_SCREEN) || (CurrentView == ViewScreen.TRANSITION_SCREEN))
            {
                if (this._isTrainingGame)
                {
                    if (CurrentView == ViewScreen.RACING_SCREEN)
                    {
                        g.DrawImage(top_bar_game_training, 0, 0, 1024, 40);
                    }
                    else
                    {
                        g.DrawImage(top_bar_transition_training, 0, 0, 1024, 40);
                    }
                }
                else
                {
                    if (CurrentView == ViewScreen.RACING_SCREEN)
                    {
                        g.DrawImage(top_bar_game, 0, 0, 1024, 40);
                    }
                    else
                    {
                        g.DrawImage(top_bar_transition, 0, 0, 1024, 40);
                    }
                }
                //				if (this._isTrainingGame)
                //				{
                //					g.DrawImage(training_top_bar_game,0,0,1024,40);
                //				}
                //				else
                //				{
                //					g.DrawImage(top_bar_game,0,0,1024,40);
                //				}
            }
            else
            {
                g.DrawImage(top_bar_normal, 0, 0, 1024, 40);
            }

            //Drawing the Bottom Bar
            Image bottomBar = null;

            switch (CurrentView)
            {
                case ViewScreen.RACING_SCREEN:
                    if (_isTrainingGame)
                    {
                        bottomBar = training_bottom_bar_normal;
                    }
                    else
                    {
                        bottomBar = bottom_bar_game;
                    }
                    break;

                case ViewScreen.GAME_DETAILS_SCREEN:
                case ViewScreen.TRANSITION_SCREEN:
                case ViewScreen.GAME_SELECTION_SCREEN:
                case ViewScreen.NETWORK_SCREEN:
                case ViewScreen.REPORT_SCREEN:
                case ViewScreen.IT_REPORT_SCREEN:
                    bottomBar = bottom_bar_powered;
                    break;
            }

            if (bottomBar != null)
            {
                g.DrawImage(bottomBar, 0, 728, 1024, 40);
            }
        }

        public override void ClearToolTips()
        {
            DiscreteSimGUI.ItilToolTip_Quad.TheInstance.Hide();
        }

        protected override void clearOtherDataFiles(int round, string current_round_dir)
        {
            if (!gameFile.IsSalesGame)
            {
                string filename = "";
                //remove the time sheet files 
                filename = gameFile.CurrentRoundDir + "\\future_timesheet.xml";
                if (File.Exists(filename))
                {
                    File.Delete(filename);
                }
                filename = gameFile.CurrentRoundDir + "\\past_timesheet.xml";
                if (File.Exists(filename))
                {
                    File.Delete(filename);
                }
            }

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
                    this.gameFile.SetGlobalOption("it_choice_confirmed", false);
                }
            }
        }

        protected void setITChoiceConfirmed()
        {
            bool isSalesGame = DetermineGameSales();
            bool isTrainingGame = DetermineGameTraining();
            bool isNormalGame = (isSalesGame == false) & (isTrainingGame == false);

            if ((isNormalGame) | (isTrainingGame))
            {
                this.gameFile.SetGlobalOption("it_choice_confirmed", true);
            }
        }

        protected override void DoOperationsPreConnectWork(int round, bool rewind)
        {
            gameFile.SetCurrentRound(round, GameFile.GamePhase.OPERATIONS, rewind);

            //No skip for round one 
            if (round == 1)
            {
                return;
            }

            //Build a new IApplier to apply the required incidents involed in a skip 
            IncidentApplier iApplier = new IncidentApplier(gameFile.NetworkModel);

            //Read the Required skip incidents xml file
            string skipDefsFile = AppInfo.TheInstance.Location + "\\data\\skip" + LibCore.CONVERT.ToStr(round) + ".xml";
            System.IO.StreamReader file = new System.IO.StreamReader(skipDefsFile);
            string xmldata = file.ReadToEnd();
            file.Close();
            file = null;

            iApplier.SetIncidentDefinitions(xmldata, gameFile.NetworkModel);

            iApplier.Dispose();
        }

	    protected override GameSelectionScreen CreateGameSelectionScreen (IGameLoader gameLoader, IProductLicence productLicence,
																		IProductLicensor productLicensor)
	    {
		    return new PMGameSelectionScreen(null, null, null);
	    }

        protected override void gameControl_ButtonPressed(object sender, CommonGUI.GlassGameControl.ButtonAction action)
        {
            //we need to over the full detail as the PM has extra buttons (EndRound) and slightly different behavious
            GlassGameControl_PM gc = this.gameControl as GlassGameControl_PM;

            // The user has pressed one of the main game control buttons...
            switch (action)
            {
                case CommonGUI.GlassGameControl.ButtonAction.EndRoundAction:
                    HandleReaceEndButton();
                    break;

                case CommonGUI.GlassGameControl.ButtonAction.PrePlay:
                    if (gc.GetPrePlayStatus())
                    {
                        StartPrePlay();
                        gc.ClearPrePlay();
                        ManualPrePlayEscape = false;
                    }
                    break;

                case CommonGUI.GlassGameControl.ButtonAction.Rewind:
                    if (gameFile.NetworkModel.GetNamedNode("CurrentTime").GetIntAttribute("seconds", -1) == 0)
                    {
                        // We have not actually started the game yet so always allow a rewind.
                    }
                    else
                    {
                        if (gameFile.IsNormalGame)
                        {
	                        int played = this.gameFile.Licence.GetTimesPhasePlayed(gameFile.LastOpsRoundPlayed);
                            int numLeft = 3 - played;

#if DEBUG
                            numLeft = 3;
#endif

                            if (numLeft <= 0)
                            {
                                string note = "Are you sure you want to rewind the game?\r\n" +
                                    "You cannot play this round any more times.\r\n" +
                                    "This will permantly destroy all game information for this round.";

                                if (DialogResult.Yes != MessageBox.Show(note,
                                    "Warning", MessageBoxButtons.YesNoCancel,
                                    MessageBoxIcon.Exclamation,
                                    MessageBoxDefaultButton.Button3))
                                {
                                    return;
                                }

                                // They cannot play so take them to the game info screen...
                                inGame = false;
                                SetPlaying(false);
                                this.CurrentView = ViewScreen.GAME_DETAILS_SCREEN;
                                return;
                            }
                            else
                            {
                                string str_numLeft = CONVERT.ToStr(numLeft);
                                string note = "Are you sure you want to rewind the game?\r\n" +
                                    "You can play this round " + str_numLeft + " more times.\r\n" +
                                    "This will reset all game information for this round.";

                                if (DialogResult.Yes != MessageBox.Show(note,
                                    "Warning", MessageBoxButtons.YesNoCancel,
                                    MessageBoxIcon.Exclamation,
                                    MessageBoxDefaultButton.Button3))
                                {
                                    return;
                                }
                            }
                        }
                    }
                    // Do a rewind...
                    gc.SetState(false, true, false, false, true, false);
                    TimeManager.TheInstance.Stop();
                    this.gameFile.Reset();
                    // 3-9-2007 : removed Reset.
                    //TimeManager.TheInstance.Reset();

                    if (gameFile.CurrentPhase == GameFile.GamePhase.OPERATIONS)
                    {
                        this.RunRace(this.gameFile.CurrentRound, true, false);
                    }
                    else
                    {
                        this.RunTransition(this.gameFile.CurrentRound, true);
                    }

                    // 3-9-2007 : removed Reset.
                    //TimeManager.TheInstance.Reset();
                    SetPlaying(false);
                    inGame = false;

                    SetMainButtonsLoadedGame(true);
                    break;

                case CommonGUI.GlassGameControl.ButtonAction.Play:
                    ManualPrePlayEscape = true;
                    EscapePrePlay();

                    int seconds = this.gameFile.NetworkModel.GetNamedNode("CurrentTime").GetIntAttribute("seconds", 0);
                    //
                    if (seconds == 0)
                    {
                        if (gameFile.CurrentPhase == GameFile.GamePhase.OPERATIONS)
                        {
                            if (gameFile.IsNormalGame)
                            {
                                // 23-05-07 ; Don't do this yet as the flash may just be playing.
                                if (!this.gameFile.CanPlayNow(gameFile.CurrentRound, gameFile.CurrentPhase))
                                {
                                    MessageBox.Show(this, "Sorry, this phase has been played 3 times already.\r\nGame cannot be played");
                                    return;
                                }
                            }
                        }
                        else
                        {
                            if (gameFile.IsNormalGame)
                            {
                                // We are pressing play at the start of the game so wind our licence on.
                                if (!this.gameFile.PlayNow(gameFile.CurrentRound, gameFile.CurrentPhase))
                                {
                                    MessageBox.Show(this, "Game cannot be played");
                                    return;
                                }
                            }
                        }
                    }

                    // Start the game playing...
                    if (!gameFile.IsNormalGame)
                    {
                        // Is a training or sales game.
                        TimeManager.TheInstance.FastForward(3);
                    }
                    else
                    {
                        // Normal game so set speed back to 1 x.
                        TimeManager.TheInstance.FastForward(1);
                    }

                    //Set controls for game RUNNING
                    gc.SetState(false, false, true, true, false, false);

                    if (raceScreen != null)
                    {
                        raceScreen.Play();
                        //TimeManager.TheInstance.Start();
                    }
                    else if (transScreen != null)
                    {
                        TimeManager.TheInstance.Start();
                    }

                    SetPlaying(true);
                    inGame = true;

                    SetMainButtonsLoadedGame(true);
                    break;

                case CommonGUI.GlassGameControl.ButtonAction.Pause:
                    // Pause the game
                    // If you cannot rewind the game don't enable the rewind button...
                    if (gameFile.NetworkModel.GetNamedNode("CurrentTime").GetIntAttribute("seconds", -1) == 0)
                    {
                        // We have not yet actually started the game so always allow rewind...
                        gc.SetState(true, true, false, false, false, false);
                    }
                    else if (this.gameFile.Licence.GetTimesPhasePlayed(gameFile.LastOpsRoundPlayed) >= 3)
                    {
                        // We are playing the game but cannot rewind.
                        // 26-7-2007 - We now want to show the rewind button but warn users
                        // when they press it instead.
                        gc.SetState(true, true, false, false, false, false);
                    }
                    else
                    {
                        // We are playing the game and can rewind.
                        gc.SetState(true, true, false, false, false, false);
                    }
                    if (raceScreen != null)
                    {
                        raceScreen.Pause();
                    }
                    TimeManager.TheInstance.Stop();
                    break;

                case CommonGUI.GlassGameControl.ButtonAction.FastForward:
                    if (gameFile.IsNormalGame)
                    {
                        if (DialogResult.Yes != MessageBox.Show("Are you sure you want to fast forward the game?",
                            "Warning", MessageBoxButtons.YesNoCancel,
                            MessageBoxIcon.Exclamation,
                            MessageBoxDefaultButton.Button3))
                        {
                            return;
                        }
                    }

                    gc.SetState(false, false, true, false, false, false);
                    if (raceScreen != null)
                    {
                        raceScreen.FastForward(gc.FastForwardSpeed);
                        TimeManager.TheInstance.FastForward(gc.FastForwardSpeed);
                    }
                    else if (transScreen != null)
                    {
                        TimeManager.TheInstance.FastForward(gc.FastForwardSpeed);
                    }
                    break;
            }
        }

        protected override void CreateToolsScreen()
        {
            toolsScreen = new PM_ToolsScreen(gameFile, this, null, supportOverrides);
            this.SuspendLayout();
            this.Controls.Add(toolsScreen);
            this.ResumeLayout(false);

            DoSize();
        }

        protected override void CreateGameControl()
        {
            gameControl = new GlassGameControl_PM();
            gameControl.Height = 33;
            gameControl.BackColor = Color.Transparent;
            gameControl.Name = "Game Play/Pause/etc Buttons";
            ((GlassGameControl_PM)gameControl).setEndRoundButtonText("Trading");
            this.Controls.Add(gameControl);
        }

        protected override void DoSize()
        {
            base.DoSize();

            if (raceScreen != null)
            {
                raceScreen.Location = new Point(0, 40);
                raceScreen.Size = new Size(1024, 678 + 10);
                this.gameControl.AdjustPositionAndWidthResize(37, 33, 1);
                this.teamLabel.Visible = false;
            }

            if (transScreen != null)
            {
                transScreen.Location = new Point(0, 50 - 10);
                transScreen.Size = new Size(1024, 678 + 10 + 40);
                this.gameControl.AdjustPositionAndWidthResize(37, 33, 0);
                this.gameControl.BackColor = Color.Transparent;
                this.teamLabel.Visible = false;
            }

            if (toolsScreen != null)
            {
                toolsScreen.Location = new Point(0, 50 - 10);
                toolsScreen.Size = new Size(1024, 678 + 10);
                toolsScreen.BackColor = Color.White;
            }

            if (gameDetailsScreen != null)
            {
                gameDetailsScreen.Location = new Point(0, 50 - 10);
                gameDetailsScreen.Size = new Size(1024, 678 + 10);
                gameDetailsScreen.BackColor = Color.White;
            }

            if (gameControl != null)
            {
                var gc = this.gameControl as GlassGameControl_PM;
                gc.AdjustPositionAndWidthResize(37, 33, 1);
                gc.Location = new Point(1020 - gameControl.Width, 738 - 7);
                gc.BringToFront();
            }

        }

        protected override void SetPlaying(bool playing)
        {
            if (playing)
            {
                setITChoiceConfirmed();
            }

            base.SetPlaying(playing);
        }

	    public override void GenerateAllReports (string folder)
	    {
		    throw new NotImplementedException();
	    }
    }
}