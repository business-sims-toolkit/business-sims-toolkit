using System;
using System.Drawing;
using System.Windows.Forms;

using LibCore;
using Network;
using CoreUtils;
using TransitionObjects;

using IncidentManagement;

namespace CommonGUI
{
	/// <summary>
	/// Summary description for RacingControlPanel.
	/// </summary>
	public class OpsControlPanel_DC : OpsControlPanel
	{
		protected bool Allow_AWT_In_Round1 = false;

		protected ImageButton unplugButton;
		protected ImageButton thermalButton;
		protected ImageButton optionsButton;
		protected ImageButton upgradeButton;
		protected ImageTextButton coolingButton;

		protected IOpsScreen screen;

		Color backColour;

		int round;
		
		public OpsControlPanel_DC (IOpsScreen screen, NodeTree nt, IncidentApplier iApplier, MirrorApplier mirrorApplier, 
			ProjectManager prjmanager, int round, int round_length_mins, Boolean IsTrainingFlag, 
			Color OperationsBackColor, Color GroupPanelBackColor,
		    GameManagement.NetworkProgressionGameFile gameFile)
			:base(
				nt, iApplier, mirrorApplier, prjmanager, round, round_length_mins, 
				IsTrainingFlag, OperationsBackColor,  GroupPanelBackColor, gameFile)
		{
			this.screen = screen;
			this.round = round;

			string fontname =  SkinningDefs.TheInstance.GetData("fontname");
			Font MyDefaultSkinFontBold9 = ConstantSizeFont.NewFont(fontname,9, FontStyle.Bold);
			Font MyDefaultSkinFontBold12 = ConstantSizeFont.NewFont(fontname,12, FontStyle.Bold);
			Color upColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_text_colour", Color.Black);
			Color downColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_down_text_colour", Color.White);
			Color hoverColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_hover_text_colour", Color.Green);
			Color disabledColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_disabled_text_colour", Color.DarkGray);

			backColour = OperationsBackColor;

			Allow_AWT_In_Round1 = true;

			Controls.Remove(pendingActions);
			Controls.Remove(toggleMonitor);

			pendingActions = new ImageButton(0);
			pendingActions.Size = new Size(buttonwidth,buttonheight);
			pendingActions.Location = new Point(toggleMonitor.Left,0);
			pendingActions.SetTransparent();
			pendingActions.SetVariants("/images/buttons/status.png");
			pendingActions.GotFocus += pendingActions_GotFocus;
			pendingActions.ButtonPressed +=pendingActions_ButtonPressed;
			pendingActions.Name = "Pending Actions Button";
            if (!playing)
            {
                pendingActions.Enabled = false;
            }
			Controls.Add(pendingActions);

			unplugButton = ImageButton.FromAutoVariants(0, "/images/buttons/unplug.png");
			unplugButton.Click += unplugButton_Click;
			unplugButton.Size = new Size (buttonwidth, buttonheight);
			unplugButton.Location = new Point (addRemoveMirrorsButton.Left + buttonseperation, 0);
			Controls.Add(unplugButton);

			// hack: remove the mirrors button
			addRemoveMirrorsButton.Hide();

			optionsButton = ImageButton.FromAutoVariants(0, "/images/buttons/options.png");
			optionsButton.Click += optionsButton_Click;
			optionsButton.Size = new Size (buttonwidth, buttonheight);
			optionsButton.Location = new Point (unplugButton.Right + buttonseperation, 0);
			Controls.Add(optionsButton);
			
			pendingActions.Location = new Point (optionsButton.Right + buttonseperation, 0);

			upgradeButton = ImageButton.FromAutoVariants(0, "/images/buttons/upgrade.png");
			upgradeButton.Click += upgradeButton_Click;
			upgradeButton.Size = new Size (buttonwidth, buttonheight);
			upgradeButton.Location = upgradeAppButton.Location;
			upgradeButton.Enabled = false;
			Controls.Add(upgradeButton);

			thermalButton = ImageButton.FromAutoVariants(0, "/images/buttons/facilities.png");
			thermalButton.Click += thermalButton_Click;
			thermalButton.Size = new Size (buttonwidth, buttonheight);
			thermalButton.Location = new Point (upgradeMemDiskButton.Left, 0);
			Controls.Add(thermalButton);

			Controls.Remove(upgradeAppButton);
			Controls.Remove(upgradeMemDiskButton);

			upgradeAppButton = new ImageTextButton (0);
			upgradeAppButton.SetVariants("/images/buttons/blank_med.png");
			(upgradeAppButton as ImageTextButton).SetButtonText("Upgrade " + SkinningDefs.TheInstance.GetData("appname", "App"), upColor, upColor, hoverColor, disabledColor);
			(upgradeAppButton as ImageTextButton).ButtonFont = MyDefaultSkinFontBold9;
			upgradeAppButton.Enabled = false;
			upgradeAppButton.Size = new Size (175, 20);
			upgradeAppButton.Location = new Point(buttonwidth*2+buttonseperation*2,0);
			upgradeAppButton.GotFocus += upgradeAppButton_GotFocus;
			upgradeAppButton.ButtonPressed += upgradeAppButton_ButtonPressed;
			upgradeAppButton.Name = "Upgrade App Button";

			upgradeMemDiskButton = new ImageTextButton(0);
			upgradeMemDiskButton.SetVariants("/images/buttons/blank_med.png");
			(upgradeMemDiskButton as ImageTextButton).SetButtonText("Upgrade " + SkinningDefs.TheInstance.GetData("servername", "Server"), upColor, upColor, hoverColor, disabledColor);
			(upgradeMemDiskButton as ImageTextButton).ButtonFont = MyDefaultSkinFontBold9;
			upgradeMemDiskButton.Enabled = false;
			upgradeMemDiskButton.Size = new Size (175, 20);
			upgradeMemDiskButton.Location = new Point(buttonwidth*3+buttonseperation*3,0);		
			upgradeMemDiskButton.GotFocus += upgradeMemDiskButton_GotFocus;
			upgradeMemDiskButton.ButtonPressed += upgradeMemDiskButton_ButtonPressed;
			upgradeMemDiskButton.Name = "Upgrade Server Button";

			upgradeAppButton.EnabledChanged += upgradeButton_EnabledChanged;
			upgradeMemDiskButton.EnabledChanged += upgradeButton_EnabledChanged;

			// Force an update.
			upgradeButton_EnabledChanged(null, null);

			BringToFront();
		}

		#region ITimedClass Members

		public override void Start()
		{
			//We always show the Install and Pending Buttons 

			installBusinessServiceButton.Enabled = true;
			pendingActions.Enabled = true;
			MTRSButton.Enabled = true;

			//Check for Round Sensitive buttons 
			upgradeAppButton.Enabled = true;
			upgradeMemDiskButton.Enabled = true;
			addRemoveMirrorsButton.Enabled = true;

			CheckMonitorStatus();

			toggleMonitor.Enabled = true;

			playing = true;
		}

		public override void FastForward(double timesRealTime)
		{
			// TODO:  Add RacingControlPanel.FastForward implementation
		}

		public override void Stop()
		{
			//MTRSButton.SetButton("/images/"blank_grey_pill.png","/images/"blank_grey_pill.png","/images/"blank_grey_pill.png");
			MTRSButton.Enabled = true;

			//Disable all other buttons during stop 

			//pendingActions.SetButton("/images/"blank_grey_pill.png","/images/"blank_grey_pill.png","/images/"blank_grey_pill.png");
			pendingActions.Enabled = false;
			//upgradeAppButton.SetButton("/images/"blank_grey_pill.png","/images/"blank_grey_pill.png","/images/"blank_grey_pill.png");
			upgradeAppButton.Enabled = false;
			//upgradeMemDiskButton.SetButton("/images/"blank_grey_pill.png","/images/"blank_grey_pill.png","/images/"blank_grey_pill.png");
			upgradeMemDiskButton.Enabled = false;
			//installBusinessServiceButton.SetButton("/images/"blank_grey_pill.png","/images/"blank_grey_pill.png","/images/"blank_grey_pill.png");
			installBusinessServiceButton.Enabled = false;
			//addRemoveMirrorsButton.SetButton("/images/"blank_grey_pill.png","/images/"blank_grey_pill.png","/images/"blank_grey_pill.png");
			addRemoveMirrorsButton.Enabled = false;
			//toggleMonitor.SetButton("/images/"blank_grey_pill.png","/images/"blank_grey_pill.png","/images/"blank_grey_pill.png");
			toggleMonitor.Enabled = false;
			//

			if(_round > 1)
			{
				upgradeAppButton.Enabled = true;
				upgradeMemDiskButton.Enabled = true;
			}

			DisposeEntryPanel();
			playing = false;
		}

		#endregion

		protected override void upgradeMemDiskButton_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			//if(playing)
			//{
				// upgradeMemDiskButton
				UpgradeMemDiskControl_DC upgradePanel = new UpgradeMemDiskControl_DC(this,_network, true,
					_iApplier, MyIsTrainingFlag, MyOperationsBackColor, MyGroupPanelBackColor, playing);
				upgradePanel.Size = new Size(popup_width,popup_height);
				upgradePanel.Location = new Point(popup_xposition,popup_yposition);
				upgradePanel.SetMaxMins(_round_maxmins);
				Parent.Controls.Add(upgradePanel);
				upgradePanel.BringToFront();
				DisposeEntryPanel();
				shownControl = upgradePanel;
				upgradePanel.Focus();
				upgradePanel.SetMaxMins(_round_maxmins);
				// : Fix for 2661: make buttons stay active
				((ImageButton) sender).Active = true;
			//}
		}

		protected override void upgradeAppButton_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			//if(playing)
			//{
				// upgradeAppButton
				UpgradeAppControl_DC upgradePanel = new UpgradeAppControl_DC(this,_iApplier,_network,true,
					MyIsTrainingFlag, MyOperationsBackColor, MyGroupPanelBackColor, playing);
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
			//}
		}

		protected override void toggleMonitor_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
		}

		void unplugButton_Click (object sender, EventArgs e)
		{
			DisposeEntryPanel();
			UnPlugServerPanel panel = new UnPlugServerPanel (this, _network);
			panel.Location = new Point (popup_xposition, popup_yposition);
			panel.Size = new Size (popup_width, popup_height);
			Parent.Controls.Add(panel);
			panel.BringToFront();
			panel.Focus();
			shownControl = panel;
			((ImageButton) sender).Active = true;
		}

		void thermalButton_Click (object sender, EventArgs e)
		{
			DisposeEntryPanel();
			ThermalPanel panel = new ThermalPanel (this, _network);
			panel.Location = new Point (popup_xposition, popup_yposition);
			panel.Size = new Size (popup_width, popup_height);
			Parent.Controls.Add(panel);
			panel.BringToFront();
			panel.Focus();
			shownControl = panel;
			((ImageButton) sender).Active = true;
		}

		public override void ResetButtons ()
		{
			installBusinessServiceButton.Active = false;
			upgradeAppButton.Active = false;
			upgradeMemDiskButton.Active = false;
			MTRSButton.Active = false;
			addRemoveMirrorsButton.Active = false;
			pendingActions.Active = false;

			if (optionsButton != null)
			{
				optionsButton.Active = false;
			}

			if (upgradeButton != null)
			{
				upgradeButton.Active = false;
			}

			if (unplugButton != null)
			{
				unplugButton.Active = false;
			}

			if (thermalButton != null)
			{
				thermalButton.Active = false;
			}
		}

		public override void DisposeEntryPanel ()
		{
			if (shownControl != null)
			{
				shownControl.SuspendLayout();
				shownControl.Controls.Remove(upgradeAppButton);
				shownControl.Controls.Remove(upgradeMemDiskButton);
				shownControl.ResumeLayout(false);
			}

			base.DisposeEntryPanel();
		}

		public bool EntryPanelIsActive
		{
			get
			{
				return (shownControl != null);
			}
		}

		protected override void pendingActions_ButtonPressed (object sender, ImageButtonEventArgs args)
		{
			if(playing)
			{
				PendingActionsControlWithIncidents pac = new PendingActionsControlWithIncidents(this,_network, _prjmanager, 
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
	
		protected override void installBusinessServiceButton_ButtonPressed (object sender, ImageButtonEventArgs args)
		{
			if (playing)
			{
				InstallBusinessServiceControl IBS_Panel = new InstallBusinessServiceControl(this, _network, round,
					_prjmanager, MyIsTrainingFlag, MyOperationsBackColor, MyGroupPanelBackColor);
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

		void upgradeButton_Click(object sender, EventArgs e)
		{
			DisposeEntryPanel();

			string fontname =  SkinningDefs.TheInstance.GetData("fontname");
            Font MyDefaultSkinFontBold9 = ConstantSizeFont.NewFont(fontname, 9, FontStyle.Bold);
            Font MyDefaultSkinFontBold10 = ConstantSizeFont.NewFont(fontname, 10, FontStyle.Bold);
			Font MyDefaultSkinFontBold12 = ConstantSizeFont.NewFont(fontname,12, FontStyle.Bold);
			Color upColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_text_colour", Color.Black);
			Color downColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_down_text_colour", Color.White);
			Color hoverColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_hover_text_colour", Color.Green);
			Color disabledColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_disabled_text_colour", Color.DarkGray);
			
			Color MyTextLabelBackColor = backColour;
			if (SkinningDefs.TheInstance.GetIntData("race_panels_transparent_backs", 0) == 1)
			{
				MyTextLabelBackColor = Color.Transparent;
			}
			Color MyTitleForeColor = Color.Black;

			FlickerFreePanel panel = new FlickerFreePanel ();
			panel.Size = new Size (popup_width, popup_height);
			panel.Location = new Point (popup_xposition, popup_yposition);

			panel.BackgroundImage = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location +
				"\\images\\panels\\race_panel_back_normal.png");

			Label label = new Label ();
			label.Text = "Upgrade";
			label.Size = new Size (500,20);
			label.Location = new Point (5,5);
			label.Font = MyDefaultSkinFontBold12;
			label.BackColor = MyTextLabelBackColor;
			label.ForeColor = MyTitleForeColor;
			panel.Controls.Add(label);

			upgradeAppButton.Width = 140;
			panel.Controls.Add(upgradeAppButton);
			upgradeAppButton.Location = new Point (30, 60);

			upgradeMemDiskButton.Width = 140;
			panel.Controls.Add(upgradeMemDiskButton);
			upgradeMemDiskButton.Location = new Point (upgradeAppButton.Right + 20, 60);

			coolingButton = new ImageTextButton (0);
			coolingButton.SetVariants("/images/buttons/blank_med.png");
			coolingButton.SetButtonText("Upgrade CRAC", upColor, upColor, hoverColor, disabledColor);
			coolingButton.ButtonFont = MyDefaultSkinFontBold9;
			coolingButton.Enabled = true;
			coolingButton.Size = new Size (140, 20);
			coolingButton.Location = new Point (upgradeMemDiskButton.Right + 20, 60);
			coolingButton.Click += coolingButton_Click;
			bool coolingPossible = false;
			for (int zone = 1; zone <= 7; zone++)
			{
				Node zoneNode = _network.GetNamedNode("Zone" + CONVERT.ToStr(zone));
				if ((zoneNode == null) || zoneNode.GetBooleanAttribute("activated", false))
				{
					Node coolingNode = _network.GetNamedNode("C" + CONVERT.ToStr(zone));
					if ((coolingNode != null) && (! coolingNode.GetBooleanAttribute("upgraded", false)))
					{
						coolingPossible = true;
					}
				}
			}
			if (coolingPossible)
			{
				panel.Controls.Add(coolingButton);
			}

			ImageTextButton closeButton = new ImageTextButton (0);
			closeButton.SetVariants("/images/buttons/blank_small.png");
			closeButton.Location = new Point (520, 185);
			closeButton.Size = new Size (80, 20);
			closeButton.SetButtonText("Close", upColor, upColor,hoverColor,disabledColor);
			closeButton.ButtonFont = MyDefaultSkinFontBold10;
			closeButton.Click += closeButton_Click;
			panel.Controls.Add(closeButton);

			Parent.Controls.Add(panel);
			panel.BringToFront();
			panel.Focus();

			shownControl = panel;

			((ImageButton) sender).Active = true;
		}

		void upgradeButton_EnabledChanged (object sender, EventArgs args)
		{
			if (upgradeButton != null)
			{
				upgradeButton.Enabled = upgradeAppButton.Enabled || upgradeMemDiskButton.Enabled;
			}
		}

		void closeButton_Click(object sender, EventArgs e)
		{
			DisposeEntryPanel();
		}

		void optionsButton_Click (object sender, EventArgs e)
		{
			ZoneOptionsPanel panel = new ZoneOptionsPanel (this, _network, MyIsTrainingFlag, MyOperationsBackColor, MyGroupPanelBackColor, round);
			panel.Size = new Size(popup_width,popup_height);
			panel.Location = new Point(popup_xposition,popup_yposition);
			Parent.Controls.Add(panel);
			panel.BringToFront();
			DisposeEntryPanel();
			shownControl = panel;
			panel.Focus();
			// : Fix for 2661: make buttons stay active
			((ImageButton) sender).Active = true;
		}

		void coolingButton_Click (object sender, EventArgs e)
		{
			UpgradeAppControl_DC upgradePanel = new UpgradeAppControl_DC (this, _iApplier, _network, true,
			                                                              MyIsTrainingFlag,
				                                                          MyOperationsBackColor, MyGroupPanelBackColor,
				                                                          playing);

			upgradePanel.Size = new Size (popup_width, popup_height);
			upgradePanel.Location = new Point (popup_xposition, popup_yposition);
			upgradePanel.SetMaxMins(_round_maxmins);
			upgradePanel.PressCoolingButton();
			Parent.Controls.Add(upgradePanel);
			upgradePanel.BringToFront();

			DisposeEntryPanel();
			shownControl = upgradePanel;

			((ImageButton) sender).Active = true;

			upgradePanel.Focus();
		}

		public void CloseToolTips ()
		{
			screen.CloseToolTips();
		}

		protected virtual void GenerateOperationPanel_MTRSOrAutoRestore ()
		{
			IntermediateOpsPanel panel = new IntermediateOpsPanel (this, MyIsTrainingFlag, MyOperationsBackColor, 200,
			                                                       "Set MTRS or Auto Restore Times",
			                                                        new string [] { "Set MTRS", "Set Auto Restore" },
			                                                        new EventHandler [] { SetMTRS_Click,
			                                                                              SetAutoRestore_Click } );
			panel.Size = new Size(popup_width,popup_height);
			panel.Location = new Point(popup_xposition,popup_yposition);
			Parent.Controls.Add(panel);
			panel.BringToFront();
			DisposeEntryPanel();
			shownControl = panel;
			panel.Focus();
		}

		void SetMTRS_Click (object sender, EventArgs args)
		{
			GenerateOperationPanel_MTRS();
		}

		void SetAutoRestore_Click (object sender, EventArgs args)
		{
			GenerateOperationPanel_AutoRestore();
		}

		protected virtual void GenerateOperationPanel_AutoRestore ()
		{
			Race_AutoRestore_Editor pac = new Race_AutoRestore_Editor (this,_network, MyIsTrainingFlag, MyOperationsBackColor, _round);
			pac.Size = new Size(popup_width,popup_height);
			pac.Location = new Point(popup_xposition,popup_yposition);
			Parent.Controls.Add(pac);
			pac.BringToFront();
			DisposeEntryPanel();
			shownControl = pac;
			pac.Focus();
		}

		protected override void MTRSButton_ButtonPressed (object sender, ImageButtonEventArgs args)
		{
			GenerateOperationPanel_MTRSOrAutoRestore();
			((ImageButton) sender).Active = true;
		}
	}
}