using System;
using System.Drawing;
using System.Windows.Forms;

using Algorithms;
using CommonGUI;
using CoreUtils;
using Events;
using DevOps.OpsEngine;
using DevOps.OpsScreen.FacilitatorControls;
using DevOps.OpsScreen.FacilitatorControls.AppDevelopmentUi;
using DevOps.OpsScreen.SecondaryDisplay;
using GameManagement;
using IncidentManagement;
using LibCore;
using Media;
using Network;
using ResizingUi;


namespace DevOps.OpsScreen
{
    internal class FacilitatorOpsScreen : OpsPhaseScreen, IDataEntryControlHolderWithShowPanel
    {
	    readonly NetworkProgressionGameFile gameFile;
	    readonly NodeTree networkModel;

	    readonly TradingOpsEngine opsEngine;

	    readonly OpsControlPanelBar opsControlBar;

	    readonly StartAppDevelopmentPanel startAppDevelopmentPanel;
        readonly DevelopingAppsList developingAppsView;
        
        readonly DetailedIncidentList incidentList;

	    readonly SoundPlayer soundPlayer;

        readonly GameScreenPanel gameScreen;

        readonly DevOpsErrorPanel errorPanel;
		
	    readonly AttributeDisplayPanel transactionView;
        readonly DevOpsImpactBasedSlaEditor slaEditor;

        public FacilitatorOpsScreen (NetworkProgressionGameFile gameFile, OpsControlPanelBar opsControlBar, TradingOpsEngine opsEngine, GameScreenPanel gameScreen)
        {
            this.gameFile = gameFile;
            networkModel = gameFile.NetworkModel;

            this.opsControlBar = opsControlBar;
            opsControlBar.Visible = false;
            
            this.opsEngine = opsEngine;
            
            SetBaseOpsEngine(opsEngine);

            this.gameScreen = gameScreen;
            gameScreen.AppIconClicked += gameScreen_AppIconClicked;
			gameScreen.NumberOfAvailableLocationsChanged += gameScreen_NumberOfAvailableLocationsChanged;


            if (gameFile.IsSalesGame)
            {
                opsEngine.SetRoundMinutes(10);
            }

			soundPlayer = new SoundPlayer ();

            opsEngine.PhaseFinished += opsEngine_PhaseFinished;
            
            startAppDevelopmentPanel = new StartAppDevelopmentPanel(opsEngine.TheRequestsApplier, networkModel)
            {
                BackColor = SkinningDefs.TheInstance.GetColorData("facilitator_developing_apps_back_colour", Color.IndianRed)
            };
            Controls.Add(startAppDevelopmentPanel);
            startAppDevelopmentPanel.PreferredHeightChanged += (sender, args) => DoSize();
			startAppDevelopmentPanel.Click += panel_Clicked;

			developingAppsView = new DevelopingAppsList(networkModel, opsEngine.AppTerminator)
            {
                BackColor = SkinningDefs.TheInstance.GetColorData("facilitator_developing_apps_back_colour", Color.IndianRed)
            };
            Controls.Add(developingAppsView);
            developingAppsView.ServiceSelected += DevelopingAppsView_ServiceSelected;

		    developingAppsView.Click += panel_Clicked;

			incidentList = new DetailedIncidentList(opsEngine)
	        {
		        TabIndex = 0
	        };
			Controls.Add(incidentList);
	        

            errorPanel = new DevOpsErrorPanel(networkModel, false)
            {
                Size = new Size(511, 295)
            };
            Controls.Add(errorPanel);

            slaEditor = new DevOpsImpactBasedSlaEditor(gameFile.CurrentRound, gameFile.NetworkModel, this, false, 10)
            {
                BackColor = SkinningDefs.TheInstance.GetColorData("pop_up_panel_background_colour", Color.GhostWhite)
            };
            Controls.Add(slaEditor);
	        slaEditor.Click += panel_Clicked;

	        transactionView = new AttributeDisplayPanel(gameFile.NetworkModel.GetNamedNode("Transactions"),
		        "Transactions",
		        node =>
		        {
			        var handledTransactions = node.GetIntAttribute("count_good", 0);
			        var maximumTransactions = node.GetIntAttribute("count_max", 0);
			        return $"{handledTransactions}/{maximumTransactions}";
		        }, "99/99", "TRANSACTIONS", 0.3f)
	        {
				TitleForeColour = Color.Black,
				ValueForeColour = Color.Black,
				ValueFontStyle = FontStyle.Bold,
				TitleAlignment = StringAlignment.Near,
				ValueAlignment = StringAlignment.Near,
				TitleBackColour = Color.LightGray,
				ValueBackColour = Color.LightGray
	        };
				
			Controls.Add(transactionView);
	        transactionView.Click += panel_Clicked;
        }

		void gameScreen_NumberOfAvailableLocationsChanged(object sender, EventArgs<int> e)
		{
			startAppDevelopmentPanel.Enabled = e.Parameter > 0;
		}

		void gameScreen_AppIconClicked(object sender, EventArgs<Node> e)
        {
            ShowAppDevelopmentPanel(e.Parameter);
        }

        void DevelopingAppsView_ServiceSelected(object sender, EventArgs<Node> e)
        {
            
            ShowAppDevelopmentPanel(e.Parameter);
        }

        void ShowAppDevelopmentPanel (Node serviceNode)
        {
            var appDevelopmentPanel = new AppDevelopmentPanel(networkModel, serviceNode, opsEngine.TheRequestsApplier, gameFile.CurrentRound, this, opsEngine.AppTerminator);
            ShowEntryPanel(appDevelopmentPanel);
            appDevelopmentPanel.BackColor = SkinningDefs.TheInstance.GetColorData("facilitator_app_development_back_colour", Color.HotPink);

            DoSize();
        }

	    void panel_Clicked (object sender, EventArgs e)
	    {
			incidentList.SetFocus();
	    }

        protected override void Dispose (bool disposing)
	    {
		    if (disposing)
		    {
				soundPlayer.Dispose();

		        gameScreen.AppIconClicked -= gameScreen_AppIconClicked;
		    }
	    }

		public override void Play ()
        {
	        FirePlayPressed(gameFile);

	        int currentTime = networkModel.GetNamedNode("CurrentTime").GetIntAttribute("seconds", -1);
	        if (currentTime == 0)
	        {
		        soundPlayer.Play(AppInfo.TheInstance.Location + "\\audio\\start.wav", false);
	        }

	        TimeManager.TheInstance.Start();

	        // We are actually starting the game so wind our licence on.
	        if (networkModel.GetNamedNode("CurrentTime").GetIntAttribute("seconds", -1) == 0)
	        {
		        gameFile.PlayNow(gameFile.CurrentRound, gameFile.CurrentPhase);
	        }

	        incidentList.Enabled = true;
			incidentList.SetFocus();
        }

		public override void Pause ()
		{
			incidentList.Enabled = false;
		}

        public override void Reset ()
        {
	        opsEngine.Reset();
        }

		public override void FastForward (double speed)
        {
        }

        public override void ImportIncidents (string incidentsFile)
        {
	        opsEngine.TheIncidentApplier.SetIncidentDefinitions(System.IO.File.ReadAllText(incidentsFile), networkModel);
        }

		public override void DisableUserInteraction ()
        {
            throw new NotImplementedException();
        }

        protected override void DoSize ()
        {
            startAppDevelopmentPanel.Bounds = new Rectangle(0, 0, Width / 2, 220);
            startAppDevelopmentPanel.Height = startAppDevelopmentPanel.PreferredHeight;
            
            var popupHeight = Math.Max(375,Height * 4 / 7);

            var popupBounds = new Rectangle(0, Height - popupHeight, Width / 2, popupHeight);

            if (currentlyShownControl != null)
            {
                currentlyShownControl.Bounds = popupBounds;
            }
            
            developingAppsView.Bounds = new Rectangle(0, startAppDevelopmentPanel.Bottom, 
                startAppDevelopmentPanel.Width, (currentlyShownControl?.Top ?? Height) - startAppDevelopmentPanel.Height);

	        incidentList.Bounds = new RectangleFromBounds
	        {
		        Left = Width / 2,
		        Top = 0,
		        Right = Width,
		        Bottom = (int)(Height * 0.65f)
			}.ToRectangle();

	        transactionView.Bounds = new RectangleFromBounds
	        {
		        Left = incidentList.Left,
		        Right = incidentList.Right,
		        Top = incidentList.Bottom,
		        Height = (int) (Height * 0.05f)
	        }.ToRectangle();

            slaEditor.Bounds = new RectangleFromBounds
            {
                Left = incidentList.Left,
                Right = incidentList.Right,
                Top = transactionView.Bottom,
                Bottom = Height
            }.ToRectangle();

            errorPanel.Bounds = new Rectangle(0, 0, Width, Height).AlignRectangle(incidentList.Width, Height * 3/7, StringAlignment.Far, StringAlignment.Far);

            Invalidate();
        }

        void opsEngine_PhaseFinished (object sender)
        {
	        soundPlayer.Play(AppInfo.TheInstance.Location + "\\audio\\end.wav", false);
	        opsControlBar.DisableButtons();

	        OnPhaseFinished();
        }

        public void DisposeEntryPanel ()
        {
            if (currentlyShownControl != null)
            {
                currentlyShownControl.Dispose();
                currentlyShownControl = null;
            }

            DoSize();
        }

        public void DisposeEntryPanel_indirect (int which)
        {
            throw new NotImplementedException();
        }

        public void SwapToOtherPanel (int whichOperation)
        {
            throw new NotImplementedException();
        }

        public IncidentApplier IncidentApplier => opsEngine.TheIncidentApplier;
        public void ShowEntryPanel (Control panel)
        {
            DisposeEntryPanel();

            currentlyShownControl = panel;
            Controls.Add(currentlyShownControl);
            currentlyShownControl.BringToFront();

			panel.Click += panel_Clicked;

			DoSize();
        }

        Control currentlyShownControl;
    }
}