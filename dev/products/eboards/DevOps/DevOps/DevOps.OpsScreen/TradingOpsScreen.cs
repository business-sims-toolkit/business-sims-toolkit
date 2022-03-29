using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;
using LibCore;
using Network;
using DevOps.OpsEngine;
using CoreUtils;

using GameManagement;
using CommonGUI;
using DiscreteSimGUI;
using IncidentManagement;
using Media;

namespace DevOps.OpsScreen
{
    public class TradingOpsScreen : OpsPhaseScreen, ITimedClass, IDataEntryControlHolderWithShowPanel
    {
	    readonly SoundPlayer soundPlayer = new SoundPlayer();

	    readonly NetworkProgressionGameFile _gameFile;

        protected bool _isTrainingGame = false;

	    readonly DevOpsErrorPanel errorPanel;

	    readonly TradingOpsEngine opsEngine;
	    readonly NodeTree _Network;

	    readonly AwtPanel awtPanel;
	    readonly NewServicesDisplay newServicesDisplay;

	    readonly DevMetricView devMetricView;

	    readonly DevOpsQuadStatusLozengeGroup incidentPanel;
	    readonly DevOpsMetricView metricPanel;

	    readonly TransactionViewPanel transactionsViewPanel;

	    ZonePowerDisplay zoneGraph = null;

	    TransTypeDisplay TransHeaderDisplayPanel = null;

	    public OpsControlPanelBar OpsControlBar { get; }

	    RequestsPanel requestsPanel;
        DevOpsImpactBasedSlaEditor slaEditorPanel;
        ActiveIncidentsPanel activeIncidentsPanel;
        ServiceProgressPanel serviceProgressPanel;

	    readonly IWatermarker watermarker;

	    /// <summary>
        /// View the AWT monitor
        /// </summary>
        public  bool ViewMonitor { get; set; }

	    PoleStarAWT warningLevelsDisplay;

        Control currentlyShownControl;

        public override void ImportIncidents(string incidentsFile)
        {
            var file = new System.IO.StreamReader(incidentsFile);
            var xmldata = file.ReadToEnd();
            file.Close();
            this.opsEngine.TheIncidentApplier.SetIncidentDefinitions(xmldata,_Network);
        }

        public TradingOpsScreen(NetworkProgressionGameFile gameFile, bool isTrainingGame, OpsControlPanelBar opsControlBar, string imgDir, TradingOpsEngine opsEngine)
        {
            this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            this.SetStyle(ControlStyles.DoubleBuffer,true);
            this.SetStyle(ControlStyles.UserPaint, true);

			if (isTrainingGame)
			{
				watermarker = new Watermarker (Color.FromArgb(255, 153, 0), Color.White, new Point (0, 0), Math.PI / 4, 60, 240, 750, "TRAINING MODE: NOT FOR COMMERCIAL USE", "For facilitator's personal use only");
			}

			//Determine the main images and colors
			var BackgroundColor = Color.FromArgb(240,242,245);
	        if (isTrainingGame)
	        {
		        BackgroundColor = Color.FromArgb(255, 153, 0);
	        }
			else
			{
				BackgroundColor = Color.Transparent;
            }

            _isTrainingGame = isTrainingGame;
            _gameFile = gameFile;
            _Network = _gameFile.NetworkModel;

            transactionsViewPanel = new TransactionViewPanel(_Network) { BackColor = Color.White, Watermarker = watermarker };
            Controls.Add(transactionsViewPanel);

            metricPanel = new DevOpsMetricView(_Network);
			Controls.Add(metricPanel);

            incidentPanel = new DevOpsQuadStatusLozengeGroup(_Network) { Watermarker = watermarker };
            Controls.Add(incidentPanel);

            var AdvancedWarningTechnology = _Network.GetNamedNode("AdvancedWarningTechnology");
            AdvancedWarningTechnology.AttributesChanged += AdvancedWarningTechnology_AttributesChanged;
            this.SetAWT();

            awtPanel = new AwtPanel(_Network)
            {
	            Bounds = new Rectangle (incidentPanel.Left, incidentPanel.Bottom, incidentPanel.Width, 148),
				Watermarker = watermarker
            };
            Controls.Add(awtPanel);

	        this.opsEngine = opsEngine;
            SetBaseOpsEngine(opsEngine);

            if (_gameFile.IsSalesGame)
            {
                opsEngine.SetRoundMinutes(10);
            }

            opsEngine.PhaseFinished += opsEngine_PhaseFinished;

            this.OpsControlBar = opsControlBar;

            opsControlBar.RequestsButtonClicked += requestButton_Click;
            opsControlBar.SlaEditorButtonClicked += SetSlaEditorPanel;
            opsControlBar.ActiveIncidentsButtonClicked += SetActiveIncidentsPanel;

            newServicesDisplay = new NewServicesDisplay(_Network, opsEngine.TheRequestsApplier, _gameFile.CurrentRound)
            {
                NumServicesHorizontally = 6,
				Watermarker = watermarker
            };
            newServicesDisplay.NumberOfServicesChanged += newServicesDisplay_NumberOfServicesChanged;
            newServicesDisplay.BlankServiceIconClicked += newServicesDisplay_BlankServiceIconClicked;
            newServicesDisplay.ServiceIconClicked += newServicesDisplay_ServiceIconClicked; 
            newServicesDisplay.ServiceRemoved += newServicesDisplay_ServiceRemoved;

            Controls.Add(newServicesDisplay);
            newServicesDisplay.BringToFront();

            devMetricView = new DevMetricView(_Network, _gameFile.CurrentRound);
            Controls.Add(devMetricView);
            devMetricView.BringToFront();

			errorPanel = new DevOpsErrorPanel(_Network, isTrainingGame)
                         {
                             Size = new Size(511, 295),
                             Location = new Point(0, devMetricView.Bottom + 1),
							 Watermarker = watermarker
                         };
            errorPanel.PanelClosed += errorPanel_PanelClosed;
            Controls.Add(errorPanel);

            var voice_mode_str = CoreUtils.SkinningDefs.TheInstance.GetData("voice_mode");
            var voice_files_str = CoreUtils.SkinningDefs.TheInstance.GetData("voice_files");
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

            this.BackColor = Color.Transparent;
            this.BackgroundImage = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location +
                "\\images\\panels\\darkgrey_metal.jpg");

            ViewMonitor = false;

            DoSize();

            if(isTrainingGame)
            {
                // Go faster...
                TimeManager.TheInstance.FastForward(2);
            }
        }

        void newServicesDisplay_ServiceIconClicked(object sender, Events.EventArgs<Node> e)
        {
            OpenServiceProgressPanel(e.Parameter);
        }

        void newServicesDisplay_ServiceRemoved(object sender, EventArgs e)
        {
            if (currentlyShownControl == serviceProgressPanel)
            {
                DisposeEntryPanel();
            }
        }
        
        void newServicesDisplay_BlankServiceIconClicked (object sender, EventArgs eventArgs)
        {
            OpenNewRequestPanel();
        }

        void newServicesDisplay_NumberOfServicesChanged(object sender, EventArgs e)
        {
            var numLocations = ((NewServicesDisplay) sender).NumberOfRemainingLocations;

            OpsControlBar.EnableOrDisableRequestsButton(numLocations);
        }

	    void requestButton_Click(object sender, EventArgs eventArgs)
        {
           OpenNewRequestPanel();
        }

        void OpenServiceProgressPanel (Node serviceNode)
        {
            serviceProgressPanel = new ServiceProgressPanel(_Network, serviceNode, opsEngine.TheRequestsApplier, this, _gameFile.CurrentRound)
            {
                BackColor = SkinningDefs.TheInstance.GetColorData("pop_up_panel_background_colour"),
            };

            ShowEntryPanel(serviceProgressPanel);
        }

        void OpenNewRequestPanel ()
        {
            requestsPanel = new RequestsPanel(opsEngine.TheRequestsApplier, _gameFile.CurrentRound, _Network, this)
            {
                Location = new Point(0, newServicesDisplay.Bottom),
                Size = new Size(512, Height - newServicesDisplay.Bottom),
                BackColor = SkinningDefs.TheInstance.GetColorData("pop_up_panel_background_colour", Color.GhostWhite)
            };

            ShowEntryPanel(requestsPanel);
        }

	    void SetSlaEditorPanel(object sender, EventArgs eventArgs)
        {
            {

                slaEditorPanel = new DevOpsImpactBasedSlaEditor(_gameFile.CurrentRound, _Network, this)
                                 {
                                     BackColor = SkinningDefs.TheInstance.GetColorData("pop_up_panel_background_colour", Color.GhostWhite),
                                     Location = new Point(0, devMetricView.Bottom),
                                     Size = new Size(Width / 2, 296)
                                 };

                ShowEntryPanel(slaEditorPanel);

            }
        }

	    void SetActiveIncidentsPanel(object sender, EventArgs eventArgs)
        {
            {
                var panelWidth = 256;
                activeIncidentsPanel = new ActiveIncidentsPanel(incidentPanel, this)
                                       {
                                           BackColor = SkinningDefs.TheInstance.GetColorData("pop_up_panel_background_colour", Color.Transparent),
                                           Location = new Point((Width / 2) - panelWidth, awtPanel.Top),
                                           Size = new Size(panelWidth, awtPanel.Height)
                                       };

                ShowEntryPanel(activeIncidentsPanel);
            }
        }
        

        protected override void Dispose(bool disposing)
        {
	        if (disposing)
	        {
		        TimeManager.TheInstance.UnmanageClass(this);

		        var AdvancedWarningTechnology = _Network.GetNamedNode("AdvancedWarningTechnology");
		        AdvancedWarningTechnology.AttributesChanged -= AdvancedWarningTechnology_AttributesChanged;

		        if (null != warningLevelsDisplay)
		        {
			        warningLevelsDisplay.Dispose();
		        }

		        transactionsViewPanel.Dispose();

		        metricPanel.Dispose();

		        incidentPanel.Dispose();

		        if (TransHeaderDisplayPanel != null)
		        {
			        TransHeaderDisplayPanel.Dispose();
			        TransHeaderDisplayPanel = null;
		        }

		        newServicesDisplay.Dispose();
		        devMetricView.Dispose();
	        }

	        base.Dispose(disposing);
        }

	    void SetAWT()
        {
            var AdvancedWarningTechnology = _Network.GetNamedNode("AdvancedWarningTechnology");

            if(AdvancedWarningTechnology.GetAttribute("enabled") == "true")
            {
                if (null == zoneGraph)
                {
                    zoneGraph = new ZonePowerDisplay(_Network);
                    zoneGraph.Size = new Size(120,170);
                    zoneGraph.Location = new Point(320,465);
                    zoneGraph.SetDisplayColors(Color.Black, Color.White, Color.White,
                        Color.Green, Color.Yellow, Color.Orange, Color.Red, Color.Black);
                    this.Controls.Add(zoneGraph);
                    //zoneGraph.BringToFront();
                }

                if(null == warningLevelsDisplay)
                {
                    warningLevelsDisplay = new PoleStarAWT(this._Network, Color.Black,
                        Color.White, Color.Black, Color.DarkGray);
                    warningLevelsDisplay.Location = new Point(9,465);
                    warningLevelsDisplay.Size = new Size(311, 170);
                    warningLevelsDisplay.BackColor = Color.FromArgb(0, 0, 51);
                    this.Controls.Add(warningLevelsDisplay);
                    //warningLevelsDisplay.BringToFront();
                    //revenueDisplay.BringToFront();
                    //this.trackFlash.Visible = false;
                }
            }
            else
            {
                if (null != zoneGraph)
                {
                    this.Controls.Remove(zoneGraph);
                    zoneGraph.Dispose();
                    zoneGraph = null;
                }

                if (null != warningLevelsDisplay)
                {
                    //this.trackFlash.Visible = true;
                    Controls.Remove(warningLevelsDisplay);
                    warningLevelsDisplay.Dispose();
                    warningLevelsDisplay = null;
                }
            }
        }

        public void Start()
        {
        }

        public void Stop()
        {
        }

        public override void Reset()
        {
            opsEngine.Reset();
        }

        public override void Play()
        {
            // Tell our encompassing form that we we are playing in case we need to do some licensing stuff.
            FirePlayPressed(this._gameFile);

            var currentTime = _Network.GetNamedNode("CurrentTime").GetIntAttribute("seconds", -1);

            if (currentTime >= 0 && currentTime < 1)
            {
                soundPlayer.Play(AppInfo.TheInstance.Location + "\\audio\\start.wav", false);
            }
            

            TimeManager.TheInstance.Start();
            
            // We are actually starting the game so wind our licence on.
            if(this._Network.GetNamedNode("CurrentTime").GetIntAttribute("seconds",-1) == 0)
            {
                if(!this._gameFile.PlayNow(_gameFile.CurrentRound, _gameFile.CurrentPhase))
                {
                    // Should never get here! Something terribly wrong as we should have checked
                    // our license earlier.
                    // TODO : Log!
                }
            }
        }

        public override void Pause()
        {
        }

        public override void FastForward(double speed)
        {
        }

	    void opsEngine_PhaseFinished(object sender)
        {
            soundPlayer.Play(AppInfo.TheInstance.Location + "\\audio\\end.wav", false);
            OpsControlBar.DisableButtons();
            newServicesDisplay.DisableButtons();
            DisposeEntryPanel();

            FirePhaseFinished();
        }

	    void AdvancedWarningTechnology_AttributesChanged(Node sender, ArrayList attrs)
        {
            SetAWT();
        }

	    void HandleDataEntryPanelClosed()
        {
            if (OpsControlBar.IncidentEntryBox != null)
            {
                if (OpsControlBar.IncidentEntryBox.Enabled)
                {
                    OpsControlBar.IncidentEntryBox.SetFocus();
                }
            }
        }

	    void errorPanel_PanelClosed()
        {
            HandleDataEntryPanelClosed();
        }

        public void ShowEntryPanel(Control panel)
        {
            DisposeEntryPanel();

            currentlyShownControl = panel;
            Controls.Add(currentlyShownControl);
            currentlyShownControl.BringToFront();

	        DoSize();
        }

        public void DisposeEntryPanel()
        {
            if (currentlyShownControl != null)
            {
                currentlyShownControl.Dispose();
                currentlyShownControl = null;
            }
        }

        public void DisposeEntryPanel_indirect(int which)
        {
        }

        public void SwapToOtherPanel(int which_operation)
        {
        }

	    public IncidentApplier IncidentApplier
	    {
		    get => opsEngine.TheIncidentApplier;
	    }

	    protected override void DoSize ()
		{
			newServicesDisplay.Bounds = new Rectangle (0, 0, Width / 2, Height * 3 / 7);
			devMetricView.Bounds = new Rectangle (0, newServicesDisplay.Bottom, newServicesDisplay.Width, Height / 7);

			transactionsViewPanel.Bounds = new Rectangle (newServicesDisplay.Right, newServicesDisplay.Top, Width - newServicesDisplay.Right, newServicesDisplay.Height);
			metricPanel.Bounds = new Rectangle (transactionsViewPanel.Left, transactionsViewPanel.Bottom, transactionsViewPanel.Width, devMetricView.Height);
			incidentPanel.Bounds = new Rectangle (metricPanel.Left, metricPanel.Bottom, Width - metricPanel.Left, (Height - metricPanel.Bottom) * 3 / 5);
			awtPanel.Bounds = new Rectangle (incidentPanel.Left, incidentPanel.Bottom, Width - incidentPanel.Left, Height - incidentPanel.Bottom);

			errorPanel.Bounds = new Rectangle (0, newServicesDisplay.Bottom, newServicesDisplay.Width, Height - newServicesDisplay.Bottom);

			if (currentlyShownControl != null)
			{
				currentlyShownControl.Bounds = errorPanel.Bounds;
			}
		}

        public override void DisableUserInteraction ()
        {
            throw new NotImplementedException();
        }

        protected override void OnPaint (PaintEventArgs e)
	    {
		    base.OnPaint(e);

			var image = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + @"\images\panels\CityScape.png");

			var viewport = new Rectangle (0, devMetricView.Bottom, metricPanel.Left, Height - devMetricView.Bottom);
		    var xScale = viewport.Width / (float) image.Width;
		    var yScale = viewport.Height / (float) image.Height;
		    var scale = Math.Max(xScale, yScale);

		    var imageReferencePoint = new Point (image.Width / 2, 0);
			var windowReferencePoint = new Point (viewport.Width / 2, 0);

			var imageRectangle = new Rectangle ((int) (imageReferencePoint.X - (windowReferencePoint.X / scale)), (int) (imageReferencePoint.Y - (windowReferencePoint.Y / scale)), (int) (viewport.Width / scale), (int) (viewport.Height / scale));

			e.Graphics.DrawImage(image, viewport, imageRectangle, GraphicsUnit.Pixel);

		    watermarker?.Draw(this, e.Graphics);
		}
	}
}