using System;
using System.Collections;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Algorithms;
using CommonGUI;
using CoreUtils;
using DiscreteSimGUI;
using GameManagement;
using LibCore;
using Media;
using Network;
using Polestar_Retail.OpsEngine;
using ResizingUi;

namespace Polestar_Retail.Application
{
	public class PolestarOperationsScreen : OpsPhaseScreen, ITimedClass
	{
	    readonly NetworkProgressionGameFile _gameFile;
		TradingOpsEngine opsEngine;
	    readonly NodeTree _Network;

	    readonly CompleteGamePanel parent;

	    readonly ControlBar controlBar;

	    readonly SoundPlayer soundPlayer;

	    readonly IWatermarker watermarker;

	    readonly BusinessServiceLozengeGroup serviceLozengeGroup;

	    readonly TransactionViewPanel transactionsFinancialPanel;
        
	    readonly PicturePanel backgroundImage;
        readonly RoundTimeViewPanel clockPanel;

	    readonly LogoStrip logoStripPanel;
        
        AwtCollectionPanel awtPanel;
		PowerLevelPanel powerPanel;

	    readonly ErrorPanel errorPanel;

		Control popup;

		ImageTextButton installServiceButton;
		ImageTextButton setMtrsButton;
		ImageTextButton upgradeAppButton;
		ImageTextButton upgradeServerButton;
		ImageTextButton mirrorButton;
		ImageTextButton monitorButton;
		ImageTextButton activeIncidentsButton;

	    

		public override void FastForward (double speed)
		{
		}

		public override void ImportIncidents (string incidentsFile)
		{
			using (var file = new StreamReader(incidentsFile))
			{
				opsEngine.TheIncidentApplier.SetIncidentDefinitions(file.ReadToEnd(), _Network);
			}
		}

		void AddControlPanelButtons ()
		{
			controlBar.AddIncidentPanel(2, false);
			controlBar.IncidentApplier = opsEngine.TheIncidentApplier;

			installServiceButton = controlBar.AddButton("Install Service",
				() => ShowPopup(new InstallBusinessServiceControl (controlBar,
					_Network, _gameFile.CurrentRound,
					opsEngine.getProjectManager(), _gameFile.IsTrainingGame,
					Color.White, Color.White)));

			setMtrsButton = controlBar.AddButton("MTRS",
				() => ShowPopup(new ImpactBasedSlaEditor (controlBar,
					_gameFile.CurrentRound, _Network, _gameFile.IsTrainingGame) { BackColor = Color.White }));

			upgradeAppButton = controlBar.AddButton("Upgrade App",
				() => ShowPopup(new UpgradeAppControl (controlBar, opsEngine.getIncidentApplier(), _Network, true,
					_gameFile.IsTrainingGame, Color.White, Color.White)
					{ BackColor = Color.White }));

			upgradeServerButton = controlBar.AddButton("Upgrade Server",
				() => ShowPopup(new UpgradeMemDiskControl (controlBar, _Network, true, opsEngine.getIncidentApplier(),
					_gameFile.IsTrainingGame, Color.White, Color.White)
					{ BackColor = Color.White }));

			mirrorButton = controlBar.AddButton("Mirror",
				() => ShowPopup(new AddOrRemoveMirrorControl (controlBar, _Network, opsEngine.TheMirrorApplier,
					_gameFile.IsTrainingGame, Color.White, Color.White)
				{ BackColor = Color.White }));

			monitorButton = controlBar.AddButton("AWT",
				() =>
				{
					ToggleAwt();
					return null;
				});

			activeIncidentsButton = controlBar.AddButton("Change / Incidents",
				() => ShowPopup(new PendingActionsControlWithIncidents (controlBar, _Network, opsEngine.getProjectManager(),
					_gameFile.IsTrainingGame, Color.White, Color.White, _gameFile) { ForeColor = Color.Black, BackColor = Color.White }));
		}

		public PolestarOperationsScreen (NetworkProgressionGameFile gameFile, ControlBar controlBar, CompleteGamePanel parent)
		{
			this.parent = parent;

		    soundPlayer = new SoundPlayer ();

			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.DoubleBuffer,true);
			SetStyle(ControlStyles.UserPaint, true);

			if (gameFile.IsTrainingGame)
			{
				watermarker = new Watermarker (Color.FromArgb(255, 153, 0), Color.White, new Point(0, 0), Math.PI / 4, 80, 200, 750, "TRAINING MODE: NOT FOR COMMERCIAL USE", "For facilitator's personal use only");
			}

			_gameFile = gameFile;
			_Network = _gameFile.NetworkModel;

			opsEngine = new TradingOpsEngine (_Network, _gameFile, AppInfo.TheInstance.Location + "\\data\\incidents_r" + CONVERT.ToStr(_gameFile.CurrentRound) + ".xml", _gameFile.CurrentRound, ! _gameFile.IsSalesGame);
			SetBaseOpsEngine(opsEngine);
			controlBar.SetIncidentApplier(opsEngine.getIncidentApplier());
			controlBar.Watermarker = watermarker;
			this.controlBar = controlBar;

			AddControlPanelButtons();

			if (gameFile.IsSalesGame)
			{
				opsEngine.SetRoundMinutes(10);
			}

			opsEngine.PhaseFinished += opsEngine_PhaseFinished;


			var random = new Random ();
			serviceLozengeGroup = new BusinessServiceLozengeGroup (_Network.GetNamedNode("Business Services Group"),
				businessService => new QuadStatusLozenge (businessService, random, false) { AllowResizing = true });

            logoStripPanel = new LogoStrip (gameFile, watermarker);
            Controls.Add(logoStripPanel);

			clockPanel = new RoundTimeViewPanel (gameFile);
			parent.AddGameScreenControl(clockPanel);

		    backgroundImage = new PicturePanel { Watermarker = watermarker, WatermarkOnTop = true };
		    backgroundImage.ZoomWithCropping(Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + @"\images\panels\business.png"), new PointF (0.5f, 0.5f), new PointF (0.5f, 0.5f));
		    Controls.Add(backgroundImage);

		    transactionsFinancialPanel = new TransactionViewPanel (gameFile, watermarker, new CascadedBackgroundProperties
		    {
                CascadedReferenceControl = this,
                CascadedBackgroundImageZoomMode = backgroundImage.ZoomMode,
                CascadedBackgroundImage = backgroundImage.Image
		    });
		    Controls.Add(transactionsFinancialPanel);
            transactionsFinancialPanel.BringToFront();

            errorPanel = new ErrorPanel (_Network, gameFile.IsTrainingGame) { BackColor = Color.White };
			errorPanel.VisibleChanged += errorPanel_VisibleChanged;
			Controls.Add(errorPanel);

			var voice_mode_str = SkinningDefs.TheInstance.GetData("voice_mode");
			var voice_files_str = SkinningDefs.TheInstance.GetData("voice_files");

			var MyVoiceManager = new VoiceManager(AppInfo.TheInstance.Location + "\\audio\\");

			if (voice_mode_str.IndexOf("batch")>-1)
			{
				MyVoiceManager.SetVoiceMode(_Network,VoiceManager.VoiceModeBatch,voice_files_str);
			}
			else
			{
				MyVoiceManager.SetVoiceMode(_Network,VoiceManager.VoiceModeBiz,voice_files_str);
			}

			TimeManager.TheInstance.ManageClass(this);

		    BackColor = SkinningDefs.TheInstance.GetColorData("ops_background_colour");
			
			AttachServiceMonitors();

			if (gameFile.IsTrainingGame)
			{
				TimeManager.TheInstance.FastForward(2);
			}

			var AdvancedWarningTechnology = _Network.GetNamedNode("AdvancedWarningTechnology");
			AdvancedWarningTechnology.AttributesChanged += AdvancedWarningTechnology_AttributesChanged;
			UpdateAwt();

			DoSize();
		}

		protected override void Dispose(bool disposing)
		{
			if(disposing)
			{
				if(null != opsEngine)
				{
					TimeManager.TheInstance.UnmanageClass(this);
					opsEngine.Dispose();
					opsEngine = null;

					var AdvancedWarningTechnology = _Network.GetNamedNode("AdvancedWarningTechnology");
					AdvancedWarningTechnology.AttributesChanged -= AdvancedWarningTechnology_AttributesChanged;

					awtPanel?.Dispose();
					powerPanel?.Dispose();
                    transactionsFinancialPanel.Dispose();
                    serviceLozengeGroup.Dispose();
                    parent.RemoveGameScreenControl(clockPanel);
                    clockPanel.Dispose();
                    logoStripPanel.Dispose();
				    soundPlayer.Dispose();
				}
			}
			base.Dispose(disposing);
		}

		public void SetupIncidents()
		{
			opsEngine.SetupIncidents();
		}

		void AttachServiceMonitors()
		{
			var BizEntityName = SkinningDefs.TheInstance.GetData("biz");
			opsEngine.AddMultipleServiceMonitor(BizEntityName+ " 1");
			opsEngine.AddMultipleServiceMonitor(BizEntityName+ " 2");
			opsEngine.AddMultipleServiceMonitor(BizEntityName+ " 3");
			opsEngine.AddMultipleServiceMonitor(BizEntityName+ " 4");
		}

		void ToggleAwt ()
		{
			var awt = _Network.GetNamedNode("AdvancedWarningTechnology");
			awt.SetAttribute("enabled", ! awt.GetBooleanAttribute("enabled", false));
		}

		void UpdateAwt ()
		{
			var AdvancedWarningTechnology = _Network.GetNamedNode("AdvancedWarningTechnology");

			if (AdvancedWarningTechnology.GetBooleanAttribute("enabled", false))
			{
				if (awtPanel == null)
				{
					awtPanel = new AwtCollectionPanel (BasicXmlDocument.CreateFromFile(AppInfo.TheInstance.Location + @"\data\monitor_items.xml").DocumentElement, _Network)
					{
						BackColor = SkinningDefs.TheInstance.GetColorData("ops_background_colour"),
						ForeColor = Color.White
					};
                    Controls.Add(awtPanel);

					awtPanel.UseCustomBackground(backgroundImage);

					foreach (var awtPanel in awtPanel.AwtPanels)
					{
						awtPanel.UseCustomBackground(backgroundImage);
						awtPanel.BackColor = Color.FromArgb(SkinningDefs.TheInstance.GetIntData("cascaded_background_transparency", 160), 0, 0, 0);
					}

					awtPanel.BringToFront();
				}

				if (powerPanel == null)
				{
					powerPanel = new PowerLevelPanel ("POWER", _Network.GetNamedNode("PowerLevel"), 7, (node, i) => 100 * node.GetIntAttribute(CONVERT.Format("z{0}_now", i), 0) / node.GetIntAttribute(CONVERT.Format("z{0}_base", i), 1))
					{
						BackColor = SkinningDefs.TheInstance.GetColorData("ops_background_colour"),
						ForeColor = Color.White,
						ColumnGap = 1,
						RowGap = 1
					};
					Controls.Add(powerPanel);

					powerPanel.UseCustomBackground(backgroundImage);
					powerPanel.BackColor = Color.FromArgb(SkinningDefs.TheInstance.GetIntData("cascaded_background_transparency", 160), 0, 0, 0);

					powerPanel.BringToFront();
				}
			}
			else
			{
				if (awtPanel != null)
				{
					awtPanel.Dispose();
					awtPanel = null;
				}

				if (powerPanel != null)
				{
					powerPanel.Dispose();
					powerPanel = null;
				}
			}

			UpdateControlPanelButtonsState();
			DoSize();
		}

		void UpdateControlPanelButtonsState ()
		{
			var running = TimeManager.TheInstance.TimeIsRunning;
			var upgradesEnabled = (_gameFile.CurrentRound > 1);
			var monitorEnabled = (_gameFile.CurrentRound > 1);

			controlBar.EnableIncidentPanel(running);

			installServiceButton.Enabled = running;
			setMtrsButton.Enabled = true;
			upgradeAppButton.Enabled = running && upgradesEnabled;
			upgradeServerButton.Enabled = running && upgradesEnabled;
			mirrorButton.Enabled = upgradesEnabled;
			monitorButton.Enabled = monitorEnabled;
			activeIncidentsButton.Enabled = running;

			monitorButton.SetButtonText((awtPanel != null) ? "Hide Monitor" : "Show Monitor");
		}

		public void Start()
		{
			UpdateControlPanelButtonsState();
		}

		public void Stop()
		{
			UpdateControlPanelButtonsState();
		}

		public override void Reset()
		{
			opsEngine.Reset();
			UpdateControlPanelButtonsState();
		}

		public override void Play()
		{
			FirePlayPressed(_gameFile);
            
		    TimeManager.TheInstance.Start();
            var currentTime = _Network.GetNamedNode("CurrentTime").GetIntAttribute("seconds", -1);
            
		    if (currentTime == 0)
		    {
		        soundPlayer.Play(AppInfo.TheInstance.Location + "\\audio\\start.wav", false);
		        _gameFile.PlayNow(_gameFile.CurrentRound, _gameFile.CurrentPhase);
            }
            

		}

		public override void Pause()
		{
			UpdateControlPanelButtonsState();
		}

		void opsEngine_PhaseFinished(object sender)
		{
		    soundPlayer.Play( AppInfo.TheInstance.Location + "\\audio\\end.wav", false);

			FirePhaseFinished();
		}

		void AdvancedWarningTechnology_AttributesChanged(Node sender, ArrayList attrs)
		{
			UpdateAwt();
		}

		void errorPanel_VisibleChanged (object sender, EventArgs args)
		{
			controlBar.Enabled = ! errorPanel.Visible;
		}

		new Control ShowPopup (Control popup)
		{
			controlBar.DisposeEntryPanel();

			this.popup = popup;
			Controls.Add(popup);
			popup.BringToFront();
			DoSize();

			return popup;
		}

        protected override void OnPaint (PaintEventArgs e)
	    {
			watermarker?.Draw(this, e.Graphics);

            transactionsFinancialPanel.Invalidate();
		}

		Rectangle serviceLozengeGroupBounds;

		protected override void DoSize ()
		{
			const int instep = 10;

		    var topSectionHeight = Height * 0.52f;
		    var midSectionHeight = Height * 0.1f;
            
            serviceLozengeGroupBounds = new Rectangle (instep, instep, Width - (2 * instep), (int)(topSectionHeight - 2 * instep));
			var innerLozengeGroupBounds =  serviceLozengeGroup.DoLayout(this, serviceLozengeGroupBounds);

		    transactionsFinancialPanel.Bounds = innerLozengeGroupBounds;
            
		    logoStripPanel.Bounds = new Rectangle(0, (int) topSectionHeight, Width, (int) midSectionHeight);

			var clockWidth = Width / 3;
			var topBarHeight = Top;
			var clockBounds = new Rectangle ((Width - clockWidth) / 2, -topBarHeight, clockWidth, topBarHeight);
			clockPanel.Bounds = clockBounds.Map(this, Parent);

			backgroundImage.Bounds = new RectangleFromBounds { Left = 0, Right = Width, Top = 0, Bottom = Height }.ToRectangle();

			if ((awtPanel != null) && (powerPanel != null))
			{
			    var awtPanelLeft = Width - (int) (Width * 0.4f);
				powerPanel.Bounds = new RectangleFromBounds
				{
					Right = Width,
					Width = (Width - awtPanelLeft) / 4,
					Top = logoStripPanel.Bottom,
					Bottom = Height
				}.ToRectangle();

				awtPanel.Bounds = new RectangleFromBounds
				{
					Left = awtPanelLeft,
					Top = powerPanel.Top,
					Right = powerPanel.Left,
					Bottom = powerPanel.Bottom
				}.ToRectangle();

				powerPanel.LeftMargin = 25;
				powerPanel.BottomMargin = 20;
				powerPanel.RightMargin = 0;
			}

			var popupBounds = new RectangleFromBounds
			{
				Left = 0,
				Top = logoStripPanel.Bottom,
				Right = (int)(Width * 0.6f),
				Bottom = Height
			}.ToRectangle();

			errorPanel.Bounds = popupBounds;
            
			if (popup != null)
			{
				popup.Bounds = popupBounds;
			}

			Invalidate();
		    Update();
		}

		public override void DisableUserInteraction ()
		{
			errorPanel.IsInteractionDisabled = true;
		}
	}
}