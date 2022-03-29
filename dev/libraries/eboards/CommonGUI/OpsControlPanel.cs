using System;
using System.Drawing;
using Network;
using CoreUtils;
using TransitionObjects;

using IncidentManagement;

namespace CommonGUI
{
	/// <summary>
	/// Summary description for standard Operations Control panel 
	/// </summary>
    /// 
   

	public class OpsControlPanel : OpsControlPanelBase
	{
		//public delegate void PanelClosedHandler();
		//public event PanelClosedHandler PanelClosed;

		public ImageButton upgradeAppButton;
		public ImageButton upgradeMemDiskButton;
		public ImageButton installBusinessServiceButton;
		public ImageButton addRemoveMirrorsButton;
		public ImageButton pendingActions;
		public ImageToggleButton toggleMonitor;
		public ImageButton MTRSButton;
        bool disableMirror;
        bool visibleAddRemoveMirrorsButton;
	    bool disableMonitor;

        protected int MTRS_popup_width = 607;
        protected int MTRS_popup_height = 185;
	
		//protected Control shownControl;
		//protected NodeTree _network;
		//protected bool playing = false;

		//protected int popup_xposition = 9;
		//protected int popup_yposition = 440;
		//protected int popup_width = 607;
		//protected int popup_height = 185;
		//protected int buttonwidth = 60;
		//protected int buttonheight = 27;
		//protected int buttonseperation = 0;

		//protected int _round;
		//protected int _round_maxmins;
		//protected Boolean MyIsTrainingFlag;
		//protected Color MyGroupPanelBackColor;
		//protected Color MyOperationsBackColor;

		//protected GameManagement.NetworkProgressionGameFile gameFile;
		
		public OpsControlPanel(NodeTree nt, IncidentApplier iApplier, MirrorApplier mirrorApplier, 
			ProjectManager prjmanager, int round, int round_length_mins, Boolean IsTrainingFlag, 
			Color OperationsBackColor, Color GroupPanelBackColor,
			GameManagement.NetworkProgressionGameFile gameFile)
		{
			MyIsTrainingFlag = IsTrainingFlag;
			MyOperationsBackColor = OperationsBackColor;
			MyGroupPanelBackColor = GroupPanelBackColor;

			this.gameFile = gameFile;

			_round = round;
			_round_maxmins = round_length_mins;
			_mirrorApplier = mirrorApplier;
			_iApplier = iApplier;
			_prjmanager = prjmanager;
			_network = nt;
			BackColor = Color.Transparent;

            disableMirror = SkinningDefs.TheInstance.GetBoolData("disable_mirror", false);
			
            visibleAddRemoveMirrorsButton = true;

            if (disableMirror)
            {
                PreResizeButtons();
            }

		    disableMonitor = SkinningDefs.TheInstance.GetBoolData("disable_monitor", false);
		    
            if (disableMonitor)
            {
                PreResizeButtonsWithoutMonitor();
            }

		    installBusinessServiceButton = new ImageButton(0);
			installBusinessServiceButton.Size = new Size(buttonwidth,buttonheight);
			installBusinessServiceButton.Location = new Point(0,0);
			installBusinessServiceButton.SetTransparent();
			installBusinessServiceButton.SetVariants("/images/buttons/install.png");
			installBusinessServiceButton.ButtonPressed += installBusinessServiceButton_ButtonPressed;
			installBusinessServiceButton.Name = "Install Service Button";
			Controls.Add(installBusinessServiceButton);

			MTRSButton = new ImageButton(0);
			MTRSButton.Size = new Size(buttonwidth,buttonheight);
			MTRSButton.Location = new Point(buttonwidth+buttonseperation,0);
			MTRSButton.SetTransparent();
			MTRSButton.SetVariants("/images/buttons/set_sla.png");
			MTRSButton.ButtonPressed +=MTRSButton_ButtonPressed;
			MTRSButton.Name = "MTRS Button";
			Controls.Add(MTRSButton);

			upgradeAppButton = new ImageButton(0);
			upgradeAppButton.Enabled = false;
			upgradeAppButton.Size = new Size(buttonwidth,buttonheight);
			upgradeAppButton.Location = new Point(buttonwidth*2+buttonseperation*2,0);
			upgradeAppButton.SetVariants("/images/buttons/upgrade_app.png");
			upgradeAppButton.GotFocus += upgradeAppButton_GotFocus;
			upgradeAppButton.ButtonPressed += upgradeAppButton_ButtonPressed;
			upgradeAppButton.Name = "Upgrade App Button";
			Controls.Add(upgradeAppButton);

			upgradeMemDiskButton = new ImageButton(0);
			upgradeMemDiskButton.Enabled = false;
			upgradeMemDiskButton.Size = new Size(buttonwidth,buttonheight);
			upgradeMemDiskButton.Location = new Point(buttonwidth*3+buttonseperation*3,0);		
			upgradeMemDiskButton.SetVariants("/images/buttons/upgrade_server.png");
			upgradeMemDiskButton.GotFocus += upgradeMemDiskButton_GotFocus;
			upgradeMemDiskButton.ButtonPressed += upgradeMemDiskButton_ButtonPressed;
			upgradeMemDiskButton.Name = "Upgrade Server Button";
			Controls.Add(upgradeMemDiskButton);

			addRemoveMirrorsButton = new ImageButton(0);
			addRemoveMirrorsButton.Enabled = false;
			addRemoveMirrorsButton.Size = new Size(buttonwidth,buttonheight);
			addRemoveMirrorsButton.Location = new Point(buttonwidth*4+buttonseperation*4,0);
			addRemoveMirrorsButton.SetVariants("/images/buttons/mirror.png");
			addRemoveMirrorsButton.ButtonPressed += addRemoveMirrorsButton_ButtonPressed;
			addRemoveMirrorsButton.Name = "Mirrors Button";
            addRemoveMirrorsButton.Visible = visibleAddRemoveMirrorsButton;
			Controls.Add(addRemoveMirrorsButton);

			toggleMonitor = new ImageToggleButton(0, "/images/buttons/show_monitor.png", "/images/buttons/hide_monitor.png");
			toggleMonitor.Enabled = (_round > 1);
			toggleMonitor.Size = new Size(buttonwidth,buttonheight);
			toggleMonitor.Location = new Point(buttonwidth*5+buttonseperation*5,0);
			toggleMonitor.SetTransparent();
			toggleMonitor.ButtonPressed += toggleMonitor_ButtonPressed;
			toggleMonitor.Name = "Toggle Monitor Button";
		    toggleMonitor.Visible = !disableMonitor;
			Controls.Add(toggleMonitor);

			pendingActions = new ImageButton(0);
			pendingActions.Size = new Size(27,27);
			pendingActions.Location = new Point(buttonwidth*6+buttonseperation*6,0);
			pendingActions.SetTransparent();
			pendingActions.SetVariants("/images/buttons/question.png");
			pendingActions.GotFocus += pendingActions_GotFocus;
			pendingActions.ButtonPressed +=pendingActions_ButtonPressed;
			pendingActions.Name = "Pending Actions Button";
			Controls.Add(pendingActions);

            if (disableMirror)
            {
                PostResizeButtons();
            }

		    if (disableMonitor)
		    {
                PostResizeButtonsWithoutMonitorAndPossiblyNoMirroring();
		    }

		    TimeManager.TheInstance.ManageClass(this);
		}
		#region ITimedClass Members

	    void PreResizeButtonsWithoutMonitor ()
	    {
            buttonseperation = 11;
	    }

		void PreResizeButtons()
        {
            buttonseperation = 11;
            visibleAddRemoveMirrorsButton = false;
        }

		void PostResizeButtonsWithoutMonitorAndPossiblyNoMirroring()
	    {
            if(disableMirror)
            {
                pendingActions.Location = addRemoveMirrorsButton.Location;
            }
            else
            {
                pendingActions.Location = toggleMonitor.Location ;
            }
	    }

		void PostResizeButtons()
        {
            pendingActions.Location = toggleMonitor.Location;
            toggleMonitor.Location = addRemoveMirrorsButton.Location;
        }

		protected virtual bool ShouldUpgradeButtonsBeEnabled ()
		{
			return (_round >= 2);
		}

		public override void Start()
		{
			//We always show the Install and Pending Buttons 

			//installBusinessServiceButton.SetButton("/images/"install_sip.png","/images/"install_sip_active.png","/images/"install_sip_hover.png");
			installBusinessServiceButton.Enabled = true;
			//pendingActions.SetButton("/images/"pending.png","/images/"pending_active.png","/images/"pending_hover.png");
			pendingActions.Enabled = true;
			//MTRSButton.SetButton("/images/"blank_grey_pill.png","/images/"blank_grey_pill.png","/images/"blank_grey_pill.png");
			MTRSButton.Enabled = true;

			if (ShouldUpgradeButtonsBeEnabled())
			{
				//upgradeAppButton.SetButton("/images/"upgrade_app_pill.png","/images/"upgrade_app_pill_active.png","/images/"upgrade_app_pill_hover.png");
				upgradeAppButton.Enabled = true;
				//upgradeMemDiskButton.SetButton("/images/"mem_disk_pill.png","/images/"mem_disk_pill_active.png","/images/"mem_disk_pill_hover.png");
				upgradeMemDiskButton.Enabled = true;
				//addRemoveMirrorsButton.SetButton("/images/"mirror.png","/images/"mirror_active.png","/images/"mirror_hover.png");
				addRemoveMirrorsButton.Enabled = !disableMirror;

				CheckMonitorStatus();

				toggleMonitor.Enabled = true;

				//upgradeAppButton.ButtonPressed += new CommonGUI.ImageButton.ImageButtonEventArgsHandler(upgradeAppButton_ButtonPressed);
				//upgradeMemDiskButton.ButtonPressed += new CommonGUI.ImageButton.ImageButtonEventArgsHandler(upgradeMemDiskButton_ButtonPressed);
				//addRemoveMirrorsButton.ButtonPressed += new CommonGUI.ImageButton.ImageButtonEventArgsHandler(addRemoveMirrorsButton_ButtonPressed);
			}
			else
			{
				toggleMonitor.Enabled = false;
			}

			playing = true;
		}

		public override void FastForward(double timesRealTime)
		{
			// TODO:  Add RacingControlPanel.FastForward implementation
		}

		public override void Reset()
		{
			// TODO:  Add RacingControlPanel.Reset implementation
		}

		public override void Stop()
		{
			MTRSButton.Enabled = true;

			//Disable all other buttons during stop 
			pendingActions.Enabled = false;
			upgradeAppButton.Enabled = false;
			upgradeMemDiskButton.Enabled = false;
			installBusinessServiceButton.Enabled = false;
			addRemoveMirrorsButton.Enabled = false;
			//
			DisposeEntryPanel();
			playing = false;
		}

		#endregion

		public virtual void ResetButtons ()
		{
			installBusinessServiceButton.Active = false;
			upgradeAppButton.Active = false;
			upgradeMemDiskButton.Active = false;
			MTRSButton.Active = false;
			addRemoveMirrorsButton.Active = false;
			pendingActions.Active = false;
		}

		public virtual void RePositionButtons (int NewButtonsWidth, int NewButtonHeight, int buttonseperation)
		{
			buttonwidth = NewButtonsWidth;
			buttonheight = NewButtonHeight;

			installBusinessServiceButton.Size = new Size(buttonwidth,buttonheight);

			MTRSButton.Size = new Size(buttonwidth,buttonheight);
			MTRSButton.Location = new Point(buttonwidth+buttonseperation*1,0);

			upgradeAppButton.Size = new Size(buttonwidth,buttonheight);
			upgradeAppButton.Location = new Point(buttonwidth*2+buttonseperation*2,0);

			upgradeMemDiskButton.Size = new Size(buttonwidth,buttonheight);
			upgradeMemDiskButton.Location = new Point(buttonwidth*3+buttonseperation*3,0);		

			addRemoveMirrorsButton.Size = new Size(buttonwidth,buttonheight);
			addRemoveMirrorsButton.Location = new Point(buttonwidth*4+buttonseperation*4,0);

			toggleMonitor.Size = new Size(buttonwidth,buttonheight);
			toggleMonitor.Location = new Point(buttonwidth*5+buttonseperation*5,0);

			pendingActions.Size = new Size(SkinningDefs.TheInstance.GetBoolData("control_panel_pending_button_is_regular_width", false) ? buttonwidth : NewButtonHeight, NewButtonHeight);
			pendingActions.Location = new Point(buttonwidth*6+buttonseperation*6,0);
		}

		//not used 
		public override void DisposeEntryPanel_indirect(int which)
		{
		}

		public override void DisposeEntryPanel()
		{
			ResetButtons();
			Invalidate();

			if(shownControl != null)
			{
				shownControl.Dispose();
				shownControl = null;
			}
			RaisePanelClosed();
		}

		public override void SwapToOtherPanel(int which_operation)
		{
		}

		/// <summary>
		/// Dispose ...
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose(bool disposing)
		{
			if(disposing)
			{
				DisposeEntryPanel();
				TimeManager.TheInstance.UnmanageClass(this);
			}
			base.Dispose (disposing);
		}

		public void SetPopUpPosition(int x, int y)
		{
			popup_xposition = x;
			popup_yposition = y;

			if (shownControl != null)
			{
				shownControl.Location = new Point(x, y);
			}
		}

		public void SetPopUpSize(int width, int height)
		{
            popup_width = width;
            popup_height = height;
            MTRS_popup_width = width;
            MTRS_popup_height = height;

			if (shownControl != null)
			{
				shownControl.Bounds = new Rectangle (popup_xposition, popup_yposition, width, height);
			}
		}

		public void SetMTRSPopUpSize(int width, int height)
        {
            MTRS_popup_width = width;
            MTRS_popup_height = height;
        }

		protected void startSIPButton_GotFocus(object sender, EventArgs e)
		{
			DisposeEntryPanel();
		}

		protected void cancelSIPButton_GotFocus(object sender, EventArgs e)
		{
			DisposeEntryPanel();
		}

		protected virtual void upgradeAppButton_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			if(playing)
			{
				// upgradeAppButton
				UpgradeAppControl upgradePanel = new UpgradeAppControl(this,_iApplier,_network,true,
					MyIsTrainingFlag, MyOperationsBackColor, MyGroupPanelBackColor);
				upgradePanel.Size = new Size(popup_width,popup_height);
				upgradePanel.Location = new Point(popup_xposition,popup_yposition);
				upgradePanel.SetMaxMins(_round_maxmins);
				Parent.Controls.Add(upgradePanel);
				upgradePanel.BringToFront();
				//
				DisposeEntryPanel();
				shownControl = upgradePanel;

				// : Fix for 2661: make buttons stay active
				((ImageButton) sender).Active = true;

				upgradePanel.Focus();
			}
		}

		protected void installSIPButton_GotFocus(object sender, EventArgs e)
		{
			DisposeEntryPanel();
		}

		protected void slaButton_GotFocus(object sender, EventArgs e)
		{
			DisposeEntryPanel();
		}

		protected void upgradeAppButton_GotFocus(object sender, EventArgs e)
		{
			DisposeEntryPanel();
		}

		protected virtual void upgradeMemDiskButton_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			if(playing)
			{
				// upgradeMemDiskButton
				UpgradeMemDiskControl upgradePanel = new UpgradeMemDiskControl(this,_network, true,
					_iApplier, MyIsTrainingFlag, MyOperationsBackColor, MyGroupPanelBackColor);
				upgradePanel.Size = new Size(popup_width,popup_height);
				upgradePanel.Location = new Point(popup_xposition,popup_yposition);
				upgradePanel.SetMaxMins(_round_maxmins);
				Parent.Controls.Add(upgradePanel);
				upgradePanel.BringToFront();
				DisposeEntryPanel();
				shownControl = upgradePanel;
				upgradePanel.Focus();
				// : Fix for 2661: make buttons stay active
				((ImageButton) sender).Active = true;
			}
		}

		protected void upgradeMemDiskButton_GotFocus(object sender, EventArgs e)
		{
			DisposeEntryPanel();
		}
	
		protected virtual void installBusinessServiceButton_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			if(playing)
			{
				InstallBusinessServiceControl IBS_Panel = new InstallBusinessServiceControl(this, _network,
					_round, _prjmanager, MyIsTrainingFlag, MyOperationsBackColor, MyGroupPanelBackColor);
				IBS_Panel.Size = new Size(popup_width,popup_height);
				IBS_Panel.Location = new Point(popup_xposition,popup_yposition);
				IBS_Panel.SetMaxMins(_round_maxmins);
				Parent.Controls.Add(IBS_Panel);
				IBS_Panel.BringToFront();
				DisposeEntryPanel();
				shownControl = IBS_Panel;
				IBS_Panel.Focus();
				// : Fix for 2661: make buttons stay active
				((ImageButton) sender).Active = true;
			}
		}

		protected virtual void addRemoveMirrorsButton_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			if(playing)
			{
				AddOrRemoveMirrorControl addOrRemoveMirrorControl = new AddOrRemoveMirrorControl(this, _network,
					_mirrorApplier,MyIsTrainingFlag, MyOperationsBackColor, MyGroupPanelBackColor);
				addOrRemoveMirrorControl.Size = new Size(popup_width,popup_height);
				addOrRemoveMirrorControl.Location = new Point(popup_xposition,popup_yposition);
				Parent.Controls.Add(addOrRemoveMirrorControl);
				addOrRemoveMirrorControl.BringToFront();
				DisposeEntryPanel();
				shownControl = addOrRemoveMirrorControl;
				addOrRemoveMirrorControl.Focus();
				// : Fix for 2661: make buttons stay active
				((ImageButton) sender).Active = true;
			}
		}

		protected void CheckMonitorStatus()
		{
			Node AdvancedWarningTechnology = _network.GetNamedNode("AdvancedWarningTechnology");

			if ((AdvancedWarningTechnology != null)
				&& (AdvancedWarningTechnology.GetAttribute("enabled") == "false"))
			{
				toggleMonitor.State = 0;
			}
			else
			{
				toggleMonitor.State = 1;
			}
		}

		protected virtual void pendingActions_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			if(playing)
			{
				PendingActionsControl pac = new PendingActionsControlWithIncidents(this,_network, _prjmanager, 
					MyIsTrainingFlag, MyOperationsBackColor, MyGroupPanelBackColor, gameFile);
				pac.Size = new Size(popup_width,popup_height);
				pac.Location = new Point(popup_xposition,popup_yposition);
				Parent.Controls.Add(pac);
				pac.BringToFront();
				DisposeEntryPanel();
				shownControl = pac;
				pac.Focus();
				pendingActions.Active = true;
			}
		}

		protected virtual void GenerateOperationPanel_MTRS ()
		{
			Race_SLA_Editor pac = new Race_SLA_Editor(this,_network, MyIsTrainingFlag, MyOperationsBackColor, _round);
			pac.Size = new Size(MTRS_popup_width,MTRS_popup_height);

			pac.Location = new Point(popup_xposition,popup_yposition);
			Parent.Controls.Add(pac);
			pac.BringToFront();
			DisposeEntryPanel();
			shownControl = pac;
			pac.Focus();
		}

		protected virtual void MTRSButton_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			//if(playing)
			//{
				GenerateOperationPanel_MTRS();
				// : Fix for 2661: make buttons stay active
				((ImageButton) sender).Active = true;
			//}
		}

		protected void pendingActions_GotFocus(object sender, EventArgs e)
		{
			DisposeEntryPanel();
		}

		protected virtual void toggleMonitor_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			if(_round > 1)
			{
				Node AdvancedWarningTechnology = _network.GetNamedNode("AdvancedWarningTechnology");

				if(AdvancedWarningTechnology.GetAttribute("enabled") == "true")
				{
					AdvancedWarningTechnology.SetAttribute("enabled","false");
					toggleMonitor.State = 0;
				}
				else
				{
					AdvancedWarningTechnology.SetAttribute("enabled","true");
					toggleMonitor.State = 1;
				}
			}
		}
	}
}