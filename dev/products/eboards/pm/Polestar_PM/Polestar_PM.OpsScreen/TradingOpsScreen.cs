using System;
using System.IO;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;

using LibCore;
using Network;
using Polestar_PM.OpsGUI;
using Polestar_PM.OpsEngine;

using Logging;
using Polestar_PM.DataLookup;

using GameEngine;
using CoreUtils;

using GameManagement;
using CommonGUI;
using DiscreteSimGUI;

using Media;

namespace Polestar_PM.OpsScreen
{
	/// <summary>
	/// This is the TradingOps Screen for the CA PM  Game 
	/// </summary>
	public class TradingOpsScreen : OpsPhaseScreen, ITimedClass
	{
		int timeToStopSkipping = -1;
		Node timeNode;

		public delegate void ModalActionHandler(bool entering);
		public event ModalActionHandler ModalAction;

		//Core Game Variables
		protected NetworkProgressionGameFile _gameFile;
		protected bool _isTrainingGame = false;
		protected PM_OpsEngine opsEngine;
		protected NodeTree MyNetworkNodeTree = null;
		//protected Node MyWindowManagerNode = null;
		protected GlassGameControl MyGameControlPanelHandle=null;
		//Helper Methods
		private Random random;

		//Facilities
		protected SoundPlayer endBuzzer;
		protected SoundPlayer audioTrack1;
		Timer hububResumeTimer;
		
		//Core Panels
		protected OpsScreenDayAndTimeBanner MyTimeBanner = null;
		//protected BC_QuadStatusLozengeGroup newBizServicesDisplay;
		protected ErrorPanel_PM errorPanel;
		protected IncidentEntryBox ieb;
		protected PM_OpsControlPanel rcp;

		protected GameStatsDisplay MyGameStatsDisplay = null;
		protected PM_DayActivityView dayactivityView;
		protected bool time_delay_display_countdown = false;
		protected ProjectsStatusDisplay mainProjectsback = null;
		protected PM_LogoPanel MyLogoPanel;
		protected PM_CalendarView calendarView = null;
		GamePlanPanel resourceView = null;

		//protected StopControlledTimer timer;
		protected int currentTick;
		protected int currentRound;
		protected Panel m_parent;
		
		protected bool HideNonHRAlerts = false;
		protected bool _ViewMonitor;
		protected bool started = false;

		//presentation images
		protected Image new_back = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location 
			+ "images\\panels\\raceBack.png");
	
		//presentation colors
		protected Color RevPanelBackground = Color.Black;
		protected Color RevPanelNormalTextColor = Color.White;
		protected Color RevPanelRevenueLostTextColor = Color.White;
		protected Color RevPanelRevenueGainedTextColor = Color.White;
		protected Color BackgroundColor = Color.FromArgb(0,0,104);
		protected Color RacePanelBackgroundColor = Color.Black;
		protected Color AWTAltColor = Color.FromArgb(175,187,188);
		protected Color SeaColorForeText = Color.FromArgb(107,127,127);
		protected Color SeaColor = Color.FromArgb(151,185,185);
		protected Color DarkSeaColor = Color.FromArgb(61,81,88);
		protected Color HighLightDaySeaColor = Color.FromArgb(129,160,161);

		protected OpsBusinessDisplay MyGameTargetsDisplay;

		public override void ImportIncidents(string incidentsFile)
		{
			System.IO.StreamReader file = new System.IO.StreamReader(incidentsFile);
			string xmldata = file.ReadToEnd();
			file.Close();
			this.opsEngine.TheIncidentApplier.SetIncidentDefinitions(xmldata,MyNetworkNodeTree);
		}

		protected virtual void BuildCoreComponents(NetworkProgressionGameFile gameFile, bool isTrainingGame, string ImgDir)
		{
			audioTrack1 = new SoundPlayer();
			// - commented out using office.wav due to bug Case 6750:   add the polestar pm sound to the game 
			// We probably don't want the office audio in the background but can review.
			audioTrack1.Play( AppInfo.TheInstance.Location + "/audio/hubub.wav", true);
			audioTrack1.Pause();

			m_parent.SuspendLayout();
			ieb = new IncidentEntryBox(27,66,50,2);
			ieb.Location = new Point(5,m_parent.Height- (40-5)+3-2);
			ieb.Size = new Size(205-104,30);
			ieb.BackColor = BackgroundColor;
			//ieb.BackColor = Color.Thistle;
			ieb.ReduceTextBox();
			ieb.SetTextEntryBorderFlat();
			//ieb.BackgroundImage = new Bitmap(LibCore.AppInfo.TheInstance.Location + "images\\panels\\barback.png");
			//ieb.BackColor = Color.Red;
			ieb.ShowRemoveButton(false);
			m_parent.Controls.Add(ieb);

			ieb.IncidentEntryQueue = (MyNetworkNodeTree.GetNamedNode("enteredIncidents") != null)
				? MyNetworkNodeTree.GetNamedNode("enteredIncidents")
				: new Node(MyNetworkNodeTree.Root, "enteredIncidents","enteredIncidents", (ArrayList)null);

			Color OpPanelBackColor = Color.FromArgb(218,218,218);
			Color GroupHighLightColor = CoreUtils.SkinningDefs.TheInstance.GetColorData("race_upgradebackcolor");

			time_delay_display_countdown = false;
			string display_countdown_str = CoreUtils.SkinningDefs.TheInstance.GetData("time_delay_display_countdown");
			if (display_countdown_str.ToLower().IndexOf("true")>-1)
			{
				time_delay_display_countdown = true;
			}

			rcp =  new PM_OpsControlPanel(_gameFile, MyNetworkNodeTree, opsEngine.getIncidentApplier(), 
				opsEngine.TheMirrorApplier, gameFile.CurrentRound, opsEngine.getRoundLengthMins(),isTrainingGame,
				OpPanelBackColor, GroupHighLightColor, opsEngine);
			rcp.Location = new Point(210-100-2, m_parent.Height- 32);
			rcp.Size = new Size(640-70+60+62,28);
			rcp.RePositionButtons (74, 28, 2);
			rcp.BackColor = Color.Transparent;
			//rcp.BackColor = Color.BlueViolet;
			//rcp.BackgroundImage = new Bitmap(LibCore.AppInfo.TheInstance.Location + "images\\panels\\barback2.png");
			rcp.SetPopUpPosition(0,470);
			rcp.SetPopUpSize(482,258);
			rcp.PanelClosed += new Polestar_PM.OpsScreen.PM_OpsControlPanel.PanelClosedHandler(rcp_PanelClosed);
			m_parent.Controls.Add(rcp);

			rcp.ModalAction += new PM_OpsControlPanel.ModalActionHandler(rcp_ModalAction);

			MyGameTargetsDisplay = new OpsBusinessDisplay(MyNetworkNodeTree, isTrainingGame);
			MyGameTargetsDisplay.Location = new Point(1024 - (322), 432-2);
			MyGameTargetsDisplay.Size = new Size(322, 141);
			this.Controls.Add(MyGameTargetsDisplay);

			mainProjectsback = new ProjectsStatusDisplay(MyNetworkNodeTree, gameFile.CurrentRound, isTrainingGame, ! true);
			mainProjectsback.Location = new Point(0,0);
			mainProjectsback.Size = new Size(1024,430);
			this.Controls.Add(mainProjectsback);

			MyGameStatsDisplay = new GameStatsDisplay(MyNetworkNodeTree, isTrainingGame);
			MyGameStatsDisplay.Location = new Point(482,430);
			MyGameStatsDisplay.Size = new Size(220,141);
			//MyGameStatsDisplay.BackColor = Color.Tan;
			this.Controls.Add(MyGameStatsDisplay);

			MyLogoPanel = new PM_LogoPanel();
			MyLogoPanel.SetTrainingMode(isTrainingGame);
			MyLogoPanel.SetImageDir(ImgDir);
			MyLogoPanel.Location = new Point(482, 571);
			MyLogoPanel.Size = new Size(220, 117);
			MyLogoPanel.BuildLogoContents();
			this.Controls.Add(MyLogoPanel);

			calendarView = new PM_CalendarView(MyNetworkNodeTree, isTrainingGame, opsEngine.getRoundLengthSecs());
			calendarView.Location = new Point(0, 430);
			calendarView.Size = new Size(482, 258);
			calendarView.MouseClick += new MouseEventHandler (calendarView_MouseClick);
			this.Controls.Add(calendarView);

			if (currentRound > 1)
			{
				resourceView = new GamePlanPanel(currentRound, false);
				resourceView.Bounds = calendarView.Bounds;
				resourceView.SetCompactMode(true);
				resourceView.MouseClick += new MouseEventHandler (resourceView_MouseClick);
				Controls.Add(resourceView);

				UpdatePlanView();
			}

			dayactivityView = new PM_DayActivityView(MyNetworkNodeTree, isTrainingGame, currentRound, opsEngine.getRoundLengthSecs());
			dayactivityView.Location = new Point(1024 - (322), 654-80-3);
			dayactivityView.Size = new Size(322, 117);
			this.Controls.Add(dayactivityView);

			errorPanel = new ErrorPanel_PM(MyNetworkNodeTree, isTrainingGame);
			errorPanel.Size = new Size(482, 258);
			errorPanel.Location = new Point(0, 430);
			errorPanel.PanelClosed +=new CommonGUI.ErrorPanel_PM.PanelClosedHandler(errorPanel_PanelClosed);
			this.Controls.Add(errorPanel);
			m_parent.ResumeLayout(false);
		}

		void resourceView_MouseClick (object sender, MouseEventArgs e)
		{
			calendarView.BringToFront();
		}

		void calendarView_MouseClick (object sender, MouseEventArgs e)
		{
			opsEngine.GetProjectManager().RebuildFutureTimesheets();

			if (resourceView != null)
			{
				resourceView.LoadData(_gameFile);
				resourceView.BringToFront();
			}
		}

		void UpdatePlanView ()
		{
			if (resourceView != null)
			{
				resourceView.setModel(_gameFile.NetworkModel);
				resourceView.LoadData(_gameFile);
			}
		}

		public void rcp_ModalAction(bool entering)
		{
			if (null != this.ModalAction)
			{
				ModalAction(entering);
			}
		}

		public TradingOpsScreen(Panel parent, NetworkProgressionGameFile gameFile, bool isTrainingGame, string ImgDir)
		{
			m_parent = parent;

			this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			this.SetStyle(ControlStyles.DoubleBuffer,true);
			this.SetStyle(ControlStyles.UserPaint, true);

			this.SuspendLayout();
			//Determine the main images and colors 
			if (isTrainingGame == false)
			{
				BackgroundColor = CoreUtils.SkinningDefs.TheInstance.GetColorData("race_backgroundcolor_normal");
				new_back = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "images\\panels\\raceBack.png");
			}
			else
			{
				BackgroundColor = CoreUtils.SkinningDefs.TheInstance.GetColorData("race_backgroundcolor_training");
				new_back = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "images\\panels\\raceBack_training.png");
			}

			RacePanelBackgroundColor = CoreUtils.SkinningDefs.TheInstance.GetColorData("race_panelbackgroundcolor");
			AWTAltColor = Color.FromArgb(175,187,188);

			RevPanelBackground = CoreUtils.SkinningDefs.TheInstance.GetColorData("race_revenue_background");
			RevPanelNormalTextColor = CoreUtils.SkinningDefs.TheInstance.GetColorData("race_revenue_normaltext");
			RevPanelRevenueLostTextColor = CoreUtils.SkinningDefs.TheInstance.GetColorData("race_revenue_revlostcolor");
			RevPanelRevenueGainedTextColor = CoreUtils.SkinningDefs.TheInstance.GetColorData("race_revenue_revgaincolor");

			_isTrainingGame = isTrainingGame;
			_gameFile = gameFile;
			MyNetworkNodeTree = _gameFile.NetworkModel;

			random = new Random( DateTime.Now.Second);

			opsEngine = new PM_OpsEngine(_gameFile, MyNetworkNodeTree, _gameFile.CurrentRoundDir, AppInfo.TheInstance.Location + "\\data\\incidents_r" + CONVERT.ToStr(_gameFile.CurrentRound) + ".xml", _gameFile.CurrentRound, ! _gameFile.IsSalesGame);
			SetBaseOpsEngine(opsEngine);
			opsEngine.TaskProcessed += new EventHandler(opsEngine_TaskProcessed);

			opsEngine.PhaseFinished += opsEngine_PhaseFinished;

			//Color ColorCancelled = Color.FromArgb(245,0,17);	//RED
			//Color ColorHandled = Color.FromArgb(0,128,0);			//GREEN
			//Color ColorDelayed = Color.FromArgb(0,0,128);			//Deep blue
			//Color ColorQueued =  Color.Black;									//Black 

			currentRound = _gameFile.CurrentRound;
			BuildCoreComponents(gameFile, isTrainingGame, ImgDir);

//			string voice_enable_str = CoreUtils.SkinningDefs.TheInstance.GetData("voice_enable");
//			string voice_mode_str = CoreUtils.SkinningDefs.TheInstance.GetData("voice_mode");
//			string voice_files_str = CoreUtils.SkinningDefs.TheInstance.GetData("voice_files");
//
//			VoiceManager MyVoiceManager = new VoiceManager(AppInfo.TheInstance.Location + "\\audio\\");
//			if (voice_mode_str.IndexOf("batch")>-1)
//			{
//				MyVoiceManager.SetVoiceMode(MyNetworkNodeTree,VoiceManager.VoiceModeBatch,voice_files_str);
//			}
//			else
//			{
//				MyVoiceManager.SetVoiceMode(MyNetworkNodeTree,VoiceManager.VoiceModeBiz,voice_files_str);
//			}

			TimeManager.TheInstance.ManageClass(this);

			this.Paint += new PaintEventHandler(TradingOpsScreen_Paint);
			this.Resize += new EventHandler(TradingOpsScreen_Resize);
			this.Invalidated += new InvalidateEventHandler(TradingOpsScreen_Invalidated);

			_ViewMonitor = false;
			AttachServiceMonitors();
			//revenueDisplay.BringToFront();

			this.ResumeLayout(false);

			DoSize();

			if(isTrainingGame)
			{
				// Go faster...
				TimeManager.TheInstance.FastForward(2);
			}

			timeNode = MyNetworkNodeTree.GetNamedNode("CurrentTime");
			timeNode.AttributesChanged += new Network.Node.AttributesChangedEventHandler(timeNode_AttributesChanged);

			this.VisibleChanged += new EventHandler(TradingOpsScreen_VisibleChanged);
		}

		void opsEngine_TaskProcessed (object sender, EventArgs e)
		{
			UpdatePlanView();
		}

		protected override void Dispose(bool disposing)
		{
			if(disposing)
			{
				if(null != opsEngine)
				{
					if (timeNode != null)
					{
						timeNode.AttributesChanged -= new Network.Node.AttributesChangedEventHandler(timeNode_AttributesChanged);
						timeNode = null;
					}

					TimeManager.TheInstance.UnmanageClass(this);
					opsEngine.Dispose();
					opsEngine = null;

					if (null != mainProjectsback )
					{
						mainProjectsback.Dispose();
						mainProjectsback  = null;
					}

//					if(null != warningLevelsDisplay)
//					{
//						warningLevelsDisplay.Dispose();
//						warningLevelsDisplay = null;
//					}

					// to here...

					if(ieb != null)
					{
						m_parent.SuspendLayout();
						m_parent.Controls.Remove(ieb);
						ieb.Dispose();
						m_parent.ResumeLayout(false);
						ieb = null;
					}

					if (MyGameStatsDisplay != null)
					{
						MyGameStatsDisplay.Dispose();
						MyGameStatsDisplay = null;
					}

					if (MyGameTargetsDisplay != null)
					{
						MyGameTargetsDisplay.Dispose();
						MyGameTargetsDisplay = null;
					}

					if (calendarView != null)
					{
						calendarView.Dispose();
						calendarView = null;
					}

					if (dayactivityView != null)
					{
						dayactivityView.Dispose();
						dayactivityView = null;
					}

					if (MyTimeBanner != null)
					{
						MyTimeBanner = null;
					}

					if (audioTrack1 != null)
					{
						audioTrack1.Dispose();
						audioTrack1 = null;
					}

					if (rcp != null)
					{
						rcp.Dispose();
					}
				}
			}
			base.Dispose(disposing);
		}

		public void SetGameControl(GlassGameControl ggc)
		{
			MyGameControlPanelHandle = ggc;
		}

		public void SetupIncidents()
		{
			opsEngine.SetupIncidents();
		}

		private void AttachServiceMonitors()
		{
			//opsEngine.AddMultipleServiceMonitor("Business Sections");
			string BizEntityName = CoreUtils.SkinningDefs.TheInstance.GetData("biz");
			opsEngine.AddMultipleServiceMonitor(BizEntityName+ " 1");
			opsEngine.AddMultipleServiceMonitor(BizEntityName+ " 2");
			opsEngine.AddMultipleServiceMonitor(BizEntityName+ " 3");
			opsEngine.AddMultipleServiceMonitor(BizEntityName+ " 4");
			opsEngine.AddMultipleServiceMonitor(BizEntityName+ " 5");
			opsEngine.AddMultipleServiceMonitor(BizEntityName+ " 6");
		}

		public void Start()
		{
			if(this.Visible)
			{
				if (!started)
				{
					this.opsEngine_PhaseStarted();
					started = true;
				}
			}

			// Awful hack.  As we can only play one sound at once, the start buzzer will
			// kill the hubub.  So we schedule the hubub to play again in a few seconds!
			if ((hububResumeTimer == null) && (MyNetworkNodeTree.GetNamedNode("CurrentTime").GetIntAttribute("seconds", -1) < 1))
			{
				hububResumeTimer = new Timer();
				hububResumeTimer.Interval = 4 * 1000;
				hububResumeTimer.Tick += new EventHandler(hububResumeTimer_Tick);
				hububResumeTimer.Start();
			}
		}

		void hububResumeTimer_Tick (object sender, EventArgs e)
		{
			hububResumeTimer.Stop();
			hububResumeTimer = null;

			if (audioTrack1 != null)
			{
				audioTrack1.Resume();
			}
		}
	
		/// <summary>
		/// Game Stopped
		/// </summary>
		public void Stop()
		{
			if (audioTrack1 != null)
			{
				audioTrack1.Pause();
			}

			if (hububResumeTimer != null)
			{
				hububResumeTimer.Dispose();
				hububResumeTimer = null;
			}
		}

		/// <summary>
		/// round reset
		/// </summary>
		public override void Reset()
		{
			this.opsEngine.Reset();
		}


		public override void Play()
		{
			// Tell our encompassing form that we we are playing in case we need to do some licensing stuff.
			FirePlayPressed(this._gameFile);

			if (audioTrack1 != null)
			{
				audioTrack1.Resume();
			}

			TimeManager.TheInstance.Start();
			//TimeManager.TheInstance.Start();
			// We are actually starting the game so wind our licence on.
			if(MyNetworkNodeTree.GetNamedNode("CurrentTime").GetIntAttribute("seconds",-1) == 0)
			{
				if(!this._gameFile.PlayNow(_gameFile.CurrentRound, _gameFile.CurrentPhase))
				{
					// Should never get here! Something terribly wrong as we should have checked
					// our license earlier.
					// TODO : Log!
				}
			}
			//
			currentTick = 0;
		}

		public override void Pause()
		{
		}

		public override void FastForward(double speed)
		{
			//int newInterval = (int)(1000.0 / speed);
			//timer.Interval = Math.Max(1, newInterval);
		}

		protected override void DoSize()
		{
			int w = this.Width;
			int h = this.Height;
		}

		public override void DisableUserInteraction ()
		{
			throw new NotImplementedException();
		}

		private void opsEngine_PhaseFinished(object baseEngine)
		{
			this.rcp.DisableAllButtons();
			KlaxonSingleton.TheInstance.PlayAudio(AppInfo.TheInstance.Location + "\\audio\\end.wav", false);
			FirePhaseFinished();
		}

		private void opsEngine_PhaseStarted()
		{
			// Fix for 3877 (play CA bell on round start as well as end).
			string name = SkinningDefs.TheInstance.GetData("sound_on_round_start");
			if (name != "")
			{
				KlaxonSingleton.TheInstance.PlayAudio(AppInfo.TheInstance.Location + "\\audio\\" + name, false);
			}
		}

		private void TradingOpsScreen_Paint(object sender, PaintEventArgs e)
		{
			e.Graphics.DrawImage(new_back,0,0, Width, Height);
		}

		private void TradingOpsScreen_Resize(object sender, EventArgs e)
		{
			DoSize();
		}

		private void TradingOpsScreen_Invalidated(object sender, InvalidateEventArgs e)
		{
			DoSize();
		}

		private void HandleDataEntryPanelClosed()
		{
			if (this.ieb != null)
			{
				if (ieb.Enabled)
				{
					ieb.SetFocus();
				}
			}
		}

		protected void errorPanel_PanelClosed()
		{
			HandleDataEntryPanelClosed();
		}

		protected void rcp_PanelClosed()
		{
			HandleDataEntryPanelClosed();
		}

		private void TradingOpsScreen_VisibleChanged(object sender, EventArgs e)
		{
			//System.Diagnostics.Debug.WriteLine("TradingOps Visible Start ("+this.Visible.ToString()+")");
//			this.MyCoreDisplay.CheckBuild();
//			this.MyCoreDisplay.Attach();
//			this.MyCoreDisplay.SetVisible(this.Visible);
			if(ieb != null) 
			{
				ieb.Visible = this.Visible;
			}
			if(rcp != null)
			{
				//rcp.Size = new Size(490,37);
				rcp.BringToFront();
				rcp.Visible = this.Visible;
			}
			//System.Diagnostics.Debug.WriteLine("TradingOps Visible End ("+this.Visible.ToString()+")");
		}

		public override void ForwardSkip (int amount, int speed)
		{
			timeToStopSkipping = timeNode.GetIntAttribute("seconds", 0) + amount;
			TimeManager.TheInstance.FastForward(speed);
		}

		private void timeNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			if (timeToStopSkipping > 0)
			{
				if (timeNode.GetIntAttribute("seconds", 0) >= timeToStopSkipping)
				{
					timeToStopSkipping = -1;
					TimeManager.TheInstance.FastForward(1);
				}
			}
		}

		public Rectangle GetMainArea ()
		{
			return new Rectangle (0, 39, 1024, 688);
		}
	}
}