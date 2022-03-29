using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Algorithms;
using LibCore;
using Network;
using Cloud.OpsEngine;
using CoreUtils;
using GameManagement;
using CommonGUI;
using ResizingUi;
using ZoomMode = ResizingUi.ZoomMode;

namespace Cloud.OpsScreen
{
	public class TradingOpsScreen : OpsPhaseScreen, ITimedClass
	{
		NetworkProgressionGameFile gameFile;
		NodeTree model;

		CloudOpsEngine opsEngine;

		public CloudOpsEngine OpsEngine => opsEngine;
		public CloudOpsControlPanel ControlPanel => controlPanel;

		Node americaBusiness;
		Node asiaBusiness;
		Node europeBusiness;
		Node africaBusiness;
		List<Node> businesses;

		List<Node> datacenters;
		Node africaDatacenter;
		Node europeDatacenter;
		Node asiaDatacenter;
		Node americaDatacenter;

		int GeneralBorderAndGap = 3;

		Control parent;
		CloudOpsControlPanel controlPanel;
		
		PicturePanel WorldBackDisplay;

		BusinessServicesPanel americaBusinessServicesPanel;
		BusinessServicesPanel europeBusinessServicesPanel;
		BusinessServicesPanel africaBusinessServicesPanel;
		BusinessServicesPanel asiaBusinessServicesPanel;

		RegionFinancePanel americaFinancePanel;
		RegionFinancePanel europeFinancePanel;
		RegionFinancePanel africaFinancePanel;
		RegionFinancePanel asiaFinancePanel;

		RackUtilisationPanel americaRacksPanel;
		RackUtilisationPanel europeRacksPanel;
		RackUtilisationPanel africaRacksPanel;
		RackUtilisationPanel asiaRacksPanel;

		ShadedViewPanel_PublicVendor svpPV_PublicVendor1;
		ShadedViewPanel_PublicVendor svpPV_PublicVendor2;
		ShadedViewPanel_PublicVendor svpPV_PublicVendor3;

		ShadedViewPanel_Leaderboard svpLB_MainLeaderboard;

		Node publicVendor1;
		Node publicVendor2;
		Node publicVendor3;

		Node timeNode;
		Node roundVariablesNode;

		PopupPanel popup;

		bool isReport;
		bool isTraining = false;

		public TradingOpsScreen(CompleteGamePanel completeGamePanel, NetworkProgressionGameFile gameFile, bool isTraining)
			: this (completeGamePanel, gameFile, null, isTraining)
		{
		}

		public TradingOpsScreen (CompleteGamePanel completeGamePanel, NetworkProgressionGameFile gameFile, CloudOpsEngine opsEngine, bool isTrainingGame)
		{
			this.gameFile = gameFile;
			isTraining = isTrainingGame;
			isReport = (opsEngine != null);
			model = gameFile.NetworkModel;

			parent = completeGamePanel;

            string worldMapFile = "panels\\map.png";
            if (isTraining)
            {
                worldMapFile = "panels\\t_map.png";
            }
		    Image backgroundImage = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\" + worldMapFile);

			if (isReport)
			{
				this.opsEngine = opsEngine;
			}
			else
			{
				opsEngine = new CloudOpsEngine (gameFile);
				this.opsEngine = opsEngine;
				SetBaseOpsEngine(opsEngine);
				opsEngine.PhaseFinished += opsEngine_PhaseFinished;

				controlPanel = new CloudOpsControlPanel(gameFile, model, opsEngine, this, opsEngine.OrderPlanner, opsEngine.VirtualMachineManager);
				parent.Controls.Add(controlPanel);
				controlPanel.BringToFront();

				WorldBackDisplay = new PicturePanel();
				WorldBackDisplay.BackColor = Color.FromArgb(191, 222, 226);
				WorldBackDisplay.ZoomWithLetterboxing(backgroundImage);
				Controls.Add(WorldBackDisplay);

				TimeManager.TheInstance.ManageClass(this);
				if (gameFile.IsTrainingGame)
				{
					TimeManager.TheInstance.FastForward(2);
				}

				timeNode = model.GetNamedNode("CurrentTime");
				timeNode.AttributesChanged += new Node.AttributesChangedEventHandler(timeNode_AttributesChanged);
			}

			roundVariablesNode = model.GetNamedNode("RoundVariables");

			svpLB_MainLeaderboard = new ShadedViewPanel_Leaderboard(model, isTraining);
			svpLB_MainLeaderboard.SetTitle("");
			svpLB_MainLeaderboard.SetDisplayPositionAndExtent(GeneralBorderAndGap, GeneralBorderAndGap, 1024 - (2 * GeneralBorderAndGap), 30);
			Controls.Add(svpLB_MainLeaderboard);
			svpLB_MainLeaderboard.BringToFront();

			businesses = opsEngine.OrderPlanner.GetBusinesses();
			foreach (Node business in businesses)
			{
				switch (business.GetAttribute("region"))
				{
					case "America":
						americaBusiness = business;
						break;

					case "Asia":
						asiaBusiness = business;
						break;

					case "Africa":
						africaBusiness = business;
						break;

					case "Europe":
						europeBusiness = business;
						break;

					default:
						System.Diagnostics.Debug.Assert(false);
						break;
				}
			}

			americaBusinessServicesPanel = new BusinessServicesPanel(model, americaBusiness, isTraining);
			Controls.Add(americaBusinessServicesPanel);
			americaBusinessServicesPanel.BringToFront();

		    europeBusinessServicesPanel = new BusinessServicesPanel(model, europeBusiness, isTraining);
			Controls.Add(europeBusinessServicesPanel);
			europeBusinessServicesPanel.BringToFront();

			africaBusinessServicesPanel = new BusinessServicesPanel(model, africaBusiness, isTraining);
			Controls.Add(africaBusinessServicesPanel);
			africaBusinessServicesPanel.BringToFront();

			asiaBusinessServicesPanel = new BusinessServicesPanel(model, asiaBusiness, isTraining);
			Controls.Add(asiaBusinessServicesPanel);
			asiaBusinessServicesPanel.BringToFront();
            
			datacenters = opsEngine.OrderPlanner.GetDatacenters();
			datacenters.Reverse(); //to line up with the map
			foreach (Node datacenter in datacenters)
			{
				switch (datacenter.GetAttribute("desc"))
				{
					case "Africa":
						africaDatacenter = datacenter;
						break;

					case "Europe":
						europeDatacenter = datacenter;
						break;

					case "Asia":
						asiaDatacenter = datacenter;
						break;

					case "America":
						americaDatacenter = datacenter;
						break;

					default:
						System.Diagnostics.Debug.Assert(false);
						break;
				}
			}

			if (! roundVariablesNode.GetBooleanAttribute("public_iaas_cloud_deployment_allowed", false))
			{
			    africaRacksPanel = new RackUtilisationPanel(model, africaDatacenter, isTraining);
				Controls.Add(africaRacksPanel);
				africaRacksPanel.BringToFront();

				americaRacksPanel = new RackUtilisationPanel(model, americaDatacenter, isTraining);
				Controls.Add(americaRacksPanel);
				americaRacksPanel.BringToFront();

				europeRacksPanel = new RackUtilisationPanel(model, europeDatacenter, isTraining);
				Controls.Add(europeRacksPanel);
				europeRacksPanel.BringToFront();

				asiaRacksPanel = new RackUtilisationPanel(model, asiaDatacenter, isTraining);
				Controls.Add(asiaRacksPanel);
				asiaRacksPanel.BringToFront();
			}
			else
			{
				int display_slot = 0;

				publicVendor1 = model.GetNamedNode("Public Cloud Provider 1");
				publicVendor2 = model.GetNamedNode("Public Cloud Provider 2");
				publicVendor3 = model.GetNamedNode("Public Cloud Provider 3");

				svpPV_PublicVendor1 = new ShadedViewPanel_PublicVendor(model, isTraining);
				svpPV_PublicVendor1.SetTitle(publicVendor1.GetAttribute("desc"));
				svpPV_PublicVendor1.setVendorConnection(true, 1);
				Controls.Add(svpPV_PublicVendor1);
				svpPV_PublicVendor1.BringToFront();
				display_slot++;

				svpPV_PublicVendor2 = new ShadedViewPanel_PublicVendor(model, isTraining);
				svpPV_PublicVendor2.SetTitle(publicVendor2.GetAttribute("desc"));
				svpPV_PublicVendor2.setVendorConnection(true, 2);
				Controls.Add(svpPV_PublicVendor2);
				svpPV_PublicVendor2.BringToFront();
				display_slot++;

				svpPV_PublicVendor3 = new ShadedViewPanel_PublicVendor(model, isTraining);
				svpPV_PublicVendor3.SetTitle(publicVendor3.GetAttribute("desc"));
				svpPV_PublicVendor3.setVendorConnection(true, 3);
				Controls.Add(svpPV_PublicVendor3);
				svpPV_PublicVendor3.BringToFront();
				display_slot++;
			}


			//===========================================================================
			//Showing the Finance and Status over the Map Items 
			//===========================================================================

			americaFinancePanel = new RegionFinancePanel(model, opsEngine.BauManager, americaBusiness, isTraining);
			americaFinancePanel.UseCustomBackground(ZoomMode.PreserveAspectRatioWithLetterboxing, WorldBackDisplay, backgroundImage, new PointF (0.5f, 0.5f), new PointF (0.5f, 0.5f));
			americaFinancePanel.BackColor = WorldBackDisplay.BackColor;
			Controls.Add(americaFinancePanel);
			americaFinancePanel.BringToFront();

			europeFinancePanel = new RegionFinancePanel(model, opsEngine.BauManager, europeBusiness, isTraining);
			europeFinancePanel.UseCustomBackground(ZoomMode.PreserveAspectRatioWithLetterboxing, WorldBackDisplay, backgroundImage, new PointF(0.5f, 0.5f), new PointF(0.5f, 0.5f));
			europeFinancePanel.BackColor = WorldBackDisplay.BackColor;
			Controls.Add(europeFinancePanel);
			europeFinancePanel.BringToFront();

            africaFinancePanel = new RegionFinancePanel(model, opsEngine.BauManager, africaBusiness, isTraining);
			africaFinancePanel.UseCustomBackground(ZoomMode.PreserveAspectRatioWithLetterboxing, WorldBackDisplay, backgroundImage, new PointF(0.5f, 0.5f), new PointF(0.5f, 0.5f));
			africaFinancePanel.BackColor = WorldBackDisplay.BackColor;
			Controls.Add(africaFinancePanel);
			africaFinancePanel.BringToFront();

            asiaFinancePanel = new RegionFinancePanel(model, opsEngine.BauManager, asiaBusiness, isTraining);
			asiaFinancePanel.UseCustomBackground(ZoomMode.PreserveAspectRatioWithLetterboxing, WorldBackDisplay, backgroundImage, new PointF(0.5f, 0.5f), new PointF(0.5f, 0.5f));
			asiaFinancePanel.BackColor = WorldBackDisplay.BackColor;
			Controls.Add(asiaFinancePanel);
			asiaFinancePanel.BringToFront();

			DoSize();
		}

		private void DisposeSVPIfNotNull(ShadedViewPanel_Base Obj)
		{
			if (Obj != null)
			{
				Obj.Dispose();
			}
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing)
			{
				if (! isReport)
				{
					timeNode.AttributesChanged -= new Node.AttributesChangedEventHandler(timeNode_AttributesChanged);

					TimeManager.TheInstance.UnmanageClass(this);

					opsEngine.Dispose();
					WorldBackDisplay.Dispose();

					controlPanel.Dispose();
				}

				DisposeSVPIfNotNull(svpPV_PublicVendor1);
				DisposeSVPIfNotNull(svpPV_PublicVendor2);
				DisposeSVPIfNotNull(svpPV_PublicVendor3);

				DisposeSVPIfNotNull(svpLB_MainLeaderboard);
			}
			base.Dispose(disposing);
		}

		public void SetGameControl (GlassGameControl glassGameControl)
		{
		}

		public void setControlPanelVisible(bool vis)
		{
			if (controlPanel.Visible != vis)
			{
				controlPanel.Visible = vis;
				controlPanel.Parent.Invalidate();
			}
		}

		public void Start ()
		{
		}

		public void Stop ()
		{
		}

		public override void Play ()
		{
			FirePlayPressed(gameFile);

			TimeManager.TheInstance.Start();

			if (timeNode.GetIntAttribute("seconds", -1) == 0)
			{
				KlaxonSingleton.TheInstance.setIgnoreStop(true);
				KlaxonSingleton.TheInstance.PlayAudioWithAutoResume(AppInfo.TheInstance.Location + "/audio/end.wav", "");
				gameFile.PlayNow(gameFile.CurrentRound, gameFile.CurrentPhase);
			}
		}

		public override void Pause ()
		{
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
			opsEngine.SetIncidentDefinitions(System.IO.File.ReadAllText(incidentsFile), model);
		}

		void timeNode_AttributesChanged (Node sender, ArrayList attrs)
		{
		}

		void opsEngine_PhaseFinished (object sender)
		{
			KlaxonSingleton.TheInstance.setIgnoreStop(true);
			KlaxonSingleton.TheInstance.PlayAudioWithAutoResume(AppInfo.TheInstance.Location + "/audio/end.wav", "");

			FirePhaseFinished();
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);
			DoSize();
		}

		public override void DisableUserInteraction ()
		{
			throw new NotImplementedException();
		}

		static Rectangle TransformBetweenControls (Control from, Control to, Rectangle rectangle)
		{
			return new Rectangle (to.PointToClient(from.PointToScreen(rectangle.Location)), rectangle.Size);
		}

		protected override void DoSize ()
		{
			var controlPanelBounds = new RectangleFromBounds { Left = 0, Width = controlPanel.GetPreferredSize(Size.Empty).Width, Top = Height, Height = 40 }.ToRectangle();
			if (Parent != null)
			{
				controlPanel.Bounds = TransformBetweenControls(this, Parent, controlPanelBounds);
			}

			WorldBackDisplay.Bounds = new RectangleFromBounds { Left = 0, Top = 0, Right = Width, Bottom = controlPanelBounds.Top }.ToRectangle();

			svpLB_MainLeaderboard.SetDisplayPositionAndExtent(GeneralBorderAndGap, GeneralBorderAndGap, Width - (2 * GeneralBorderAndGap), 30);

			int BSP_XGap = 0;
			int BSP_YGap = GeneralBorderAndGap;
			int BSP_StartY = svpLB_MainLeaderboard.Bottom + BSP_YGap;
			int BSP_Width = Width / 4;
			int BSP_Height = (controlPanelBounds.Top - BSP_YGap - BSP_StartY - (3 * BSP_YGap)) / 4;
			int BSP_StartX = Width - (BSP_Width + BSP_XGap);

			americaBusinessServicesPanel.Bounds = new Rectangle(BSP_StartX, BSP_StartY + businesses.IndexOf(americaBusiness) * (BSP_Height + BSP_YGap), BSP_Width, BSP_Height);
			europeBusinessServicesPanel.Bounds = new Rectangle(BSP_StartX, BSP_StartY + businesses.IndexOf(europeBusiness) * (BSP_Height + BSP_YGap), BSP_Width, BSP_Height);
			africaBusinessServicesPanel.Bounds = new Rectangle(BSP_StartX, BSP_StartY + businesses.IndexOf(africaBusiness) * (BSP_Height + BSP_YGap), BSP_Width, BSP_Height);
			asiaBusinessServicesPanel.Bounds = new Rectangle(BSP_StartX, BSP_StartY + businesses.IndexOf(asiaBusiness) * (BSP_Height + BSP_YGap), BSP_Width, BSP_Height);

			int DCR_XGap = 5;
			int DCR_StartX = DCR_XGap;
			int DCR_Width = (BSP_StartX - DCR_StartX - (4 * DCR_XGap)) / 4;
			int originalDCR_Width = DCR_Width;
			int DCR_Height = BSP_Height;
			int bottomOfBizServicesPanels = BSP_StartY + (businesses.Count * BSP_Height) + ((businesses.Count - 1) * BSP_YGap);
			int DCR_StartY = bottomOfBizServicesPanels - DCR_Height;

			if (! roundVariablesNode.GetBooleanAttribute("public_iaas_cloud_deployment_allowed", false))
			{
				africaRacksPanel.Bounds = new Rectangle(DCR_StartX + datacenters.IndexOf(africaDatacenter) * (originalDCR_Width + DCR_XGap), DCR_StartY, DCR_Width, DCR_Height);
				americaRacksPanel.Bounds = new Rectangle(DCR_StartX + datacenters.IndexOf(americaDatacenter) * (originalDCR_Width + DCR_XGap), DCR_StartY, DCR_Width, DCR_Height);
				europeRacksPanel.Bounds = new Rectangle(DCR_StartX + datacenters.IndexOf(europeDatacenter) * (originalDCR_Width + DCR_XGap), DCR_StartY, DCR_Width, DCR_Height);
				asiaRacksPanel.Bounds = new Rectangle(DCR_StartX + datacenters.IndexOf(asiaDatacenter) * (originalDCR_Width + DCR_XGap), DCR_StartY, DCR_Width, DCR_Height);
			}
			else
			{
				int display_slot = 0;
				DCR_Width = (americaBusinessServicesPanel.Left - DCR_StartX - (3 * DCR_XGap)) / 3;

				svpPV_PublicVendor1.SetDisplayPositionAndExtent(DCR_StartX + display_slot * (DCR_Width + DCR_XGap), americaBusinessServicesPanel.Top, DCR_Width, americaBusinessServicesPanel.Height);
				display_slot++;

				svpPV_PublicVendor2.SetDisplayPositionAndExtent(DCR_StartX + display_slot * (DCR_Width + DCR_XGap), americaBusinessServicesPanel.Top, DCR_Width, americaBusinessServicesPanel.Height);
				display_slot++;

				svpPV_PublicVendor3.SetDisplayPositionAndExtent(DCR_StartX + display_slot * (DCR_Width + DCR_XGap), americaBusinessServicesPanel.Top, DCR_Width, americaBusinessServicesPanel.Height);
				display_slot++;
			}

			int financePanelHeight = 200;
			int financePanelWidth = originalDCR_Width;
			int financeY = 50;

			americaFinancePanel.Bounds = new Rectangle(DCR_StartX + datacenters.IndexOf(americaDatacenter) * (originalDCR_Width + DCR_XGap), financeY, financePanelWidth, financePanelHeight);
			europeFinancePanel.Bounds = new Rectangle(DCR_StartX + datacenters.IndexOf(europeDatacenter) * (originalDCR_Width + DCR_XGap), financeY, financePanelWidth, financePanelHeight);
			africaFinancePanel.Bounds = new Rectangle(DCR_StartX + datacenters.IndexOf(africaDatacenter) * (originalDCR_Width + DCR_XGap), financeY, financePanelWidth, financePanelHeight);
			asiaFinancePanel.Bounds = new Rectangle(DCR_StartX + datacenters.IndexOf(asiaDatacenter) * (originalDCR_Width + DCR_XGap), financeY, financePanelWidth, financePanelHeight);

			if (popup != null)
			{
				if (popup.IsFullScreen)
				{
					popup.Bounds = new RectangleFromBounds
					{
						Left = GeneralBorderAndGap,
						Right = Width,
						Bottom = controlPanelBounds.Top - GeneralBorderAndGap,
						Top = svpLB_MainLeaderboard.Bottom
					}.ToRectangle();

				}
				else
				{
					popup.Bounds = new RectangleFromBounds
					{
						Left = GeneralBorderAndGap,
						Right = americaBusinessServicesPanel.Left - GeneralBorderAndGap,
						Bottom = controlPanelBounds.Top - GeneralBorderAndGap,
						Height = popup.getPreferredSize().Height
					}.ToRectangle();
				}
			}
		}

		public bool CanClosePopup()
		{
			bool proceed = true;
			if (popup != null)
			{
				proceed = popup.CanClose();
			}
			return proceed;
		}

		public void ShowPopup (PopupPanel popup)
		{
			//do we already have a Popup Panel
			if (this.popup != null)
			{
				//Yes then get rid of it
				Controls.Remove(this.popup);
				controlPanel.ResetButtons();
				this.popup.Dispose();
				this.popup = null;
			}

			//Do we have a new panel to the opened
			if (popup != null)
			{
				//Connect it up
				this.popup = popup;
				popup.Closed += new EventHandler (popup_Closed);

				Controls.Add(popup);
				popup.BringToFront();
			}

			DoSize();
		}

		void popup_Closed (object sender, EventArgs e)
		{
			ShowPopup(null);
		}
	}
}