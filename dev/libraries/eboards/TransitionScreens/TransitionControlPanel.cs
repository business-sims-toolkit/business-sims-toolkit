using System;
using System.Drawing;
using System.Windows.Forms;
using Network;

using CoreUtils;
using CommonGUI;

using IncidentManagement;

namespace TransitionScreens
{
	/// <summary>
	/// Summary description for TransitionControlPanel.
	/// </summary>
	public class TransitionControlPanel : FlickerFreePanel, ITimedClass, IDataEntryControlHolder
	{
		public delegate void OperationPanelStatusHandler(Boolean IsPanelOpen);
		public event OperationPanelStatusHandler PanelStatusChange;

		// Main top level buttons...
		public ImageButton startSIPButton;
		public ImageButton cancelSIPButton;
		public ImageButton installSIPButton;
		public ImageButton slaButton;
		public ImageButton upgradeAppButton;
		public ImageButton upgradeMemDiskButton;
		public ImageButton addMirrorButton;
		// End of main buttons.
		protected Control shownControl;
		protected NodeTree _network;

		public BaseSLAPanel definesla;

		protected int round;

		//public ImageButton toggleView;
		public TransitionScreen _ts;

		protected bool playing = false;
        protected bool _impactBasedSLA = false;

		protected IncidentApplier _iApplier;
		protected MirrorApplier _mirrorApplier;
		protected int _currentround = 1;
		protected Color MyGroupPanelBackColor;
		protected Color MyOperationsBackColor;

		protected int ButtonOffsetY = 3;
		protected int popup_xposition = 4;
		protected int popup_yposition = 0;
		protected int popup_width = 550;
		protected int popup_height = 237;
		protected int buttonwidth = 70;
		protected int buttonheight = 33;
		protected int buttonseperation = 7;
        bool disableMirroring;

		protected Node projectsNode;

		/// <summary>
		/// Constructor 
		/// </summary>
		/// <param name="ts"></param>
		/// <param name="nt"></param>
		/// <param name="iApplier"></param>
		/// <param name="mirrorApplier"></param>
		/// <param name="round"></param>
		/// <param name="OperationsBackColor"></param>
		/// <param name="GroupPanelBackColor"></param>
        /// 

        public TransitionControlPanel (TransitionScreen ts, NodeTree nt, IncidentApplier iApplier,
		                               MirrorApplier mirrorApplier, int round, Color OperationsBackColor, Color GroupPanelBackColor)
			: this (ts, nt, iApplier, mirrorApplier, round, OperationsBackColor, GroupPanelBackColor, SkinningDefs.TheInstance.GetBoolData("use_impact_based_slas", false))
        {
        }

		public TransitionControlPanel(TransitionScreen ts, NodeTree nt, IncidentApplier iApplier, 
			MirrorApplier mirrorApplier, int round, Color OperationsBackColor, Color GroupPanelBackColor, bool impactBasedSLA)
		{
			SuspendLayout();

             _impactBasedSLA = impactBasedSLA;
			this.round = round;

			_mirrorApplier = mirrorApplier;
			_iApplier = iApplier;
			_network = nt;
			_ts = ts;
			_currentround = round;
			MyOperationsBackColor = OperationsBackColor;
			MyGroupPanelBackColor = GroupPanelBackColor;

            disableMirroring = SkinningDefs.TheInstance.GetBoolData("disable_mirror", false);

			BackColor = Color.Transparent;
			//Size = new Size(540,278);
			Size = new Size(550,40);
			definesla = null;

			startSIPButton = new ImageButton(0);
			startSIPButton.Size = new Size(buttonwidth,buttonheight);
			startSIPButton.Location = new Point(0+10,ButtonOffsetY);
			startSIPButton.ButtonPressed += startSIPButton_ButtonPressed;
			startSIPButton.Name = "Start SIP Button";
            startSIPButton.SetVariants("/images/buttons/add.png");
            Controls.Add(startSIPButton);

            if (SkinningDefs.TheInstance.GetBoolData("transition_auto_size_buttons", false))
            {
                startSIPButton.SetAutoSize();
                buttonwidth = startSIPButton.Width;
                buttonheight = startSIPButton.Height;
            }

			cancelSIPButton = new ImageButton(0);
			cancelSIPButton.Size = new Size(buttonwidth,buttonheight);
			cancelSIPButton.Location = new Point(10+buttonwidth+buttonseperation*1,ButtonOffsetY);
			cancelSIPButton.ButtonPressed += cancelSIPButton_ButtonPressed;
			cancelSIPButton.Name = "Cancel SIP Button";
			Controls.Add(cancelSIPButton);

			installSIPButton = new ImageButton(0);
			installSIPButton.Size = new Size(buttonwidth,buttonheight);
			installSIPButton.Location = new Point(10+buttonwidth*2+buttonseperation*2,ButtonOffsetY);
			installSIPButton.ButtonPressed += installSIPButton_ButtonPressed;
			installSIPButton.Name = "Install SIP Button";
			Controls.Add(installSIPButton);

			slaButton = new ImageButton(0);
			slaButton.Size = new Size(buttonwidth,buttonheight);
			slaButton.Location = new Point(10+buttonwidth*3+buttonseperation*3,ButtonOffsetY);
			slaButton.ButtonPressed += slaButton_ButtonPressed;
			slaButton.Name = "SLA Button";
			Controls.Add(slaButton);

			upgradeAppButton = new ImageButton(0);
			upgradeAppButton.Size = new Size(buttonwidth,buttonheight);
			upgradeAppButton.Location = new Point(10+buttonwidth*4+buttonseperation*4,ButtonOffsetY);
			upgradeAppButton.ButtonPressed += upgradeAppButton_ButtonPressed;
			upgradeAppButton.Name = "Upgrade App Button";
			Controls.Add(upgradeAppButton);

			upgradeMemDiskButton = new ImageButton(0);
			upgradeMemDiskButton.Size = new Size(buttonwidth,buttonheight);
			upgradeMemDiskButton.Location = new Point(10+buttonwidth*5+buttonseperation*5,ButtonOffsetY);
			upgradeMemDiskButton.ButtonPressed += upgradeMemDiskButton_ButtonPressed;
			upgradeMemDiskButton.Name = "Upgrade Server Button";
			Controls.Add(upgradeMemDiskButton);

			addMirrorButton = new ImageButton(0);
			addMirrorButton.Size = new Size(buttonwidth,buttonheight);
			addMirrorButton.Location = new Point(10+buttonwidth*6+(buttonseperation*6)-1,ButtonOffsetY);
			addMirrorButton.ButtonPressed += addMirrorButton_ButtonPressed;
			addMirrorButton.Name = "Mirrors Button";
			Controls.Add(addMirrorButton);

			cancelSIPButton.SetVariants("/images/buttons/cancel.png");
			installSIPButton.SetVariants("/images/buttons/install.png");
			slaButton.SetVariants("/images/buttons/set_sla.png");
			upgradeAppButton.SetVariants("/images/buttons/upgrade_app.png");
			upgradeMemDiskButton.SetVariants("/images/buttons/upgrade_server.png");
			addMirrorButton.SetVariants("/images/buttons/mirror.png");

			startSIPButton.Enabled = false;
			cancelSIPButton.Enabled = false;
			installSIPButton.Enabled = false;
			slaButton.Enabled = true;
			upgradeAppButton.Enabled = false;
			upgradeMemDiskButton.Enabled = false;
			addMirrorButton.Enabled = false;

			startSIPButton.GotFocus += startSIPButton_GotFocus;
			cancelSIPButton.GotFocus += cancelSIPButton_GotFocus;
			installSIPButton.GotFocus += installSIPButton_GotFocus;
			slaButton.GotFocus += slaButton_GotFocus;
			upgradeAppButton.GotFocus += upgradeAppButton_GotFocus;
			upgradeMemDiskButton.GotFocus += upgradeMemDiskButton_GotFocus;
			addMirrorButton.GotFocus += addMirrorButton_GotFocus;

			ResumeLayout(false);

			projectsNode = nt.GetNamedNode("projects");
			if (projectsNode == null)
			{
				projectsNode = nt.GetNamedNode("Projects");
			}
			projectsNode.ChildAdded += projectsNode_ChildrenChanged;
			projectsNode.ChildRemoved += projectsNode_ChildrenChanged;

			TimeManager.TheInstance.ManageClass(this);
		}
		#region ITimedClass Members

		/// <summary>
		/// Race functions, on Starting the race
		/// </summary>
		public void Start()
		{
			installSIPButton.Enabled = true;//.SetButton("/images/"install_sip.png","/images/"install_sip_active.png","/images/"install_sip_hover.png");
			slaButton.Enabled = true;//.SetButton("/images/"set_sla.png","/images/"set_sla_active.png","/images/"set_sla_hover.png");
			upgradeAppButton.Enabled = true;//.SetButton("/images/"upgrade_app_pill.png","/images/"upgrade_app_pill_active.png","/images/"upgrade_app_pill_hover.png");
			upgradeMemDiskButton.Enabled = true;//.SetButton("/images/"mem_disk_pill.png","/images/"mem_disk_pill_active.png","/images/"mem_disk_pill_hover.png");
           
            addMirrorButton.Enabled = !disableMirroring;
			playing = true;

			UpdateSIPButtons();
		}

		/// <summary>
		/// Race functions, on FastForward
		/// </summary>
		/// <param name="timesRealTime"></param>
		public void FastForward(double timesRealTime)
		{
			// TODO:  Add TransitionControlPanel.FastForward implementation
		}

		/// <summary>
		/// Race functions, on Round Reset
		/// </summary>
		public void Reset()
		{
			// TODO:  Add TransitionControlPanel.Reset implementation
		}

		/// <summary>
		/// Race functions, on Stop
		/// </summary>
		public void Stop()
		{
			startSIPButton.Enabled = false;//.SetButton("/images/"blank_grey_pill.png","/images/"blank_grey_pill.png","/images/"blank_grey_pill.png");
			cancelSIPButton.Enabled = false;//.SetButton("/images/"blank_grey_pill.png","/images/"blank_grey_pill.png","/images/"blank_grey_pill.png");
			installSIPButton.Enabled = false;//.SetButton("/images/"blank_grey_pill.png","/images/"blank_grey_pill.png","/images/"blank_grey_pill.png");
			slaButton.Enabled = true;//.SetButton("/images/"blank_grey_pill.png","/images/"blank_grey_pill.png","/images/"blank_grey_pill.png");
			upgradeAppButton.Enabled = false;//.SetButton("/images/"blank_grey_pill.png","/images/"blank_grey_pill.png","/images/"blank_grey_pill.png");
			upgradeMemDiskButton.Enabled = false;//.SetButton("/images/"blank_grey_pill.png","/images/"blank_grey_pill.png","/images/"blank_grey_pill.png");
			addMirrorButton.Enabled = false;//.SetButton("/images/"blank_grey_pill.png","/images/"blank_grey_pill.png","/images/"blank_grey_pill.png");
			//
			DisposeEntryPanel();
			playing = false;
		}

		#endregion


		//Not Used
		public virtual void DisposeEntryPanel_indirect(int which_operation)
		{}

		/// <summary>
		/// Dispose ...
		/// </summary>
		public virtual void DisposeEntryPanel()
		{
			if(shownControl != null)
			{
				//toggleView.Visible = true;
				shownControl.Dispose();
				startSIPButton.Focus();
				shownControl = null;
				resetButtons();
				Invalidate();

				if(PanelStatusChange != null)
				{
					PanelStatusChange(false);
				}
			}
		}

		public virtual void SwapToOtherPanel(int which_operation)
		{
		}

		public IncidentApplier IncidentApplier
		{
			get => _iApplier;
		}

		/// <summary>
		/// Define the Top Left corner of the pop up operation panel
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		public void SetPopUpPosition(int x, int y)
		{
			popup_xposition = x;
			popup_yposition = y;
		}

		/// <summary>
		/// Define the width and height of the pop up operation panel
		/// </summary>
		/// <param name="width"></param>
		/// <param name="height"></param>
		public void SetPopUpSize(int width, int height)
		{
			popup_width = width;
			popup_height = height;
		}

		/// <summary>
		/// Used to Change the Size and seperation of the Control Buttons
		/// </summary>
		/// <param name="NewButtonsWidth"></param>
		/// <param name="NewButtonHeight"></param>
		/// <param name="Newbuttonseperation"></param>
		public void RePositionButtons (int NewButtonsWidth, int NewButtonHeight, int Newbuttonseperation)
		{
			buttonwidth = NewButtonsWidth;
			buttonheight = NewButtonHeight;
			buttonseperation = Newbuttonseperation;

			startSIPButton.Size = new Size(buttonwidth,buttonheight);
			startSIPButton.Location = new Point(0+10,ButtonOffsetY);

			cancelSIPButton.Size = new Size(buttonwidth,buttonheight);
			cancelSIPButton.Location = new Point(10+buttonwidth+buttonseperation*1,ButtonOffsetY);
			
			installSIPButton.Size = new Size(buttonwidth,buttonheight);
			installSIPButton.Location = new Point(10+buttonwidth*2+buttonseperation*2,ButtonOffsetY);
			
			slaButton.Size = new Size(buttonwidth,buttonheight);
			slaButton.Location = new Point(10+buttonwidth*3+buttonseperation*3,ButtonOffsetY);
			
			upgradeAppButton.Size = new Size(buttonwidth,buttonheight);
			upgradeAppButton.Location = new Point(10+buttonwidth*4+buttonseperation*4,ButtonOffsetY);
			
			upgradeMemDiskButton.Size = new Size(buttonwidth,buttonheight);
			upgradeMemDiskButton.Location = new Point(10+buttonwidth*5+buttonseperation*5,ButtonOffsetY);
			
			addMirrorButton.Size = new Size(buttonwidth,buttonheight);
			addMirrorButton.Location = new Point(10+buttonwidth*6+(buttonseperation*6)-1,ButtonOffsetY);
		}

		public virtual void GenerateOperationPanel_StartSIP()
		{
			startSIPButton.Active = true;
			StartSIP startSIP = new StartSIP(this,_network,_currentround, MyOperationsBackColor);
			startSIP.Size = new Size(popup_width,popup_height);
			startSIP.Location  = new Point(popup_xposition,popup_yposition);
			Parent.SuspendLayout();
			Parent.Controls.Add(startSIP);
			Parent.ResumeLayout(false);
			startSIP.BringToFront();
			//
			DisposeEntryPanel();
			//toggleView.Visible = false;
			startSIPButton.Active = true;
			shownControl = startSIP;
			startSIP.Focus();		
		}

		void startSIPButton_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			if(playing)
			{
				GenerateOperationPanel_StartSIP();
				Invalidate();

				if(PanelStatusChange != null)
				{
					PanelStatusChange(true);
				}
			}
		}

		public virtual void GenerateOperationPanel_CancelSIP()
		{
			CancelSIP cancelSIP = new CancelSIP(this,_network, _currentround, MyOperationsBackColor);
			cancelSIP.Size = new Size(popup_width,popup_height);
			cancelSIP.Location  = new Point(popup_xposition,popup_yposition);
				
			Parent.SuspendLayout();
			Parent.Controls.Add(cancelSIP);
			Parent.ResumeLayout(false);

			cancelSIP.BringToFront();
			//
			DisposeEntryPanel();
			//toggleView.Visible = false;
			cancelSIPButton.Active = true;
			shownControl = cancelSIP;
			cancelSIP.Focus();
		}

		void cancelSIPButton_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			if(playing)
			{
				GenerateOperationPanel_CancelSIP();

				Invalidate();

				if(PanelStatusChange != null)
				{
					PanelStatusChange(true);
				}
			}
		}

		public virtual void GenerateOperationPanel_InstallSIP()
		{
			InstallSIP installSIP = new InstallSIP(this,_network,_currentround,MyOperationsBackColor);
			installSIP.Size = new Size(popup_width,popup_height);
			installSIP.Location  = new Point(popup_xposition,popup_yposition);
			Parent.SuspendLayout();
			Parent.Controls.Add(installSIP);
			Parent.ResumeLayout(false);
			//installSIP.PrevControl = installSIPButton;
			installSIP.BringToFront();
			//
			DisposeEntryPanel();
			//toggleView.Visible = false;
			installSIPButton.Active = true;
			shownControl = installSIP;
			installSIP.Focus();
		}

		protected void installSIPButton_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			if(playing)
			{
				GenerateOperationPanel_InstallSIP();
				Invalidate();
				if(PanelStatusChange != null)
				{
					PanelStatusChange(true);
				}
			}
		}

		public virtual void GenerateOperationPanel_DefineSLA()
		{
            if (_impactBasedSLA)
            {
                definesla = new DefineImpactBasedSLA(this, _network, MyOperationsBackColor, round);
                definesla.Location = new Point(popup_xposition - 15, popup_yposition);
            }
            else
            {
                definesla = new DefineSLA(this, _network, MyOperationsBackColor, round);
                definesla.Location = new Point(popup_xposition, popup_yposition);
            }
			
			definesla.Size = new Size(popup_width,popup_height);
			definesla.Location  = new Point(popup_xposition,popup_yposition);
			Parent.SuspendLayout();
			Parent.Controls.Add(definesla);
			Parent.ResumeLayout(false);
			definesla.BringToFront();
			DisposeEntryPanel();
			//toggleView.Visible = false;
			slaButton.Active = true;
			shownControl = definesla;
			definesla.Focus();
		}

		void slaButton_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			GenerateOperationPanel_DefineSLA();
			Invalidate();
			if(PanelStatusChange != null)
			{
				PanelStatusChange(true);
			}
		}
	
		/// <summary>
		/// Dispose
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

		void startSIPButton_GotFocus(object sender, EventArgs e)
		{
			DisposeEntryPanel();
		}

		void cancelSIPButton_GotFocus(object sender, EventArgs e)
		{
			DisposeEntryPanel();
		}

		public virtual void GenerateOperationPanel_UpgradeApp()
		{
			TransUpgradeAppControl upgradePanel = CreateTransUpgradeAppControl(this, _iApplier, _network, false,
				MyOperationsBackColor, MyGroupPanelBackColor);

			upgradePanel.Size = new Size(popup_width,popup_height);
			upgradePanel.Location  = new Point(popup_xposition,popup_yposition);

			Parent.SuspendLayout();
			Parent.Controls.Add(upgradePanel);
			Parent.ResumeLayout(false);

			//UpgradeAppControl.PrevControl = installSIPButton;
			upgradePanel.BringToFront();
			DisposeEntryPanel();
			//toggleView.Visible = false;
			upgradeAppButton.Active = true;
			shownControl = upgradePanel;
			upgradePanel.Focus();
		}

		protected void upgradeAppButton_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			if(playing)
			{
				GenerateOperationPanel_UpgradeApp();
				Invalidate();
				if(PanelStatusChange != null)
				{
					PanelStatusChange(true);
				}
			}
		}

		void installSIPButton_GotFocus(object sender, EventArgs e)
		{
			DisposeEntryPanel();
		}

		void slaButton_GotFocus(object sender, EventArgs e)
		{
			DisposeEntryPanel();
		}

		void upgradeAppButton_GotFocus(object sender, EventArgs e)
		{
			DisposeEntryPanel();
		}

		public virtual void GenerateOperationPanel_UpgradeMemDisk()
		{
			TransUpgradeMemDiskControl upgradePanel = new TransUpgradeMemDiskControl(this, _network, 
				false, _iApplier, MyOperationsBackColor, MyGroupPanelBackColor);
			upgradePanel.Size = new Size(popup_width,popup_height);
			upgradePanel.Location  = new Point(popup_xposition,popup_yposition);

			Parent.SuspendLayout();
			Parent.Controls.Add(upgradePanel);
			Parent.ResumeLayout(false);

			upgradePanel.BringToFront();
			DisposeEntryPanel();
			//toggleView.Visible = false;
			upgradeMemDiskButton.Active = true;
			shownControl = upgradePanel;
			upgradePanel.Focus();
		}


		protected void upgradeMemDiskButton_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			if(playing)
			{
				GenerateOperationPanel_UpgradeMemDisk();
				Invalidate();

				if(PanelStatusChange != null)
				{
					PanelStatusChange(true);
				}
			}
		}

		void upgradeMemDiskButton_GotFocus(object sender, EventArgs e)
		{
			DisposeEntryPanel();
		}

		public virtual void GenerateOperationPanel_Mirror()
		{
			TransAddOrRemoveMirrorControl addOrRemoveMirrorControl = new TransAddOrRemoveMirrorControl(
				this,_network, _mirrorApplier, MyOperationsBackColor, MyGroupPanelBackColor);
			addOrRemoveMirrorControl.Size= new Size(popup_width,popup_height);
			addOrRemoveMirrorControl.Location  = new Point(popup_xposition,popup_yposition);

			Parent.SuspendLayout();
			Parent.Controls.Add(addOrRemoveMirrorControl);
			Parent.ResumeLayout(false);

			addOrRemoveMirrorControl.BringToFront();
			DisposeEntryPanel();
			//toggleView.Visible = false;
			addMirrorButton.Active = !disableMirroring;
			shownControl = addOrRemoveMirrorControl;
			addOrRemoveMirrorControl.Focus();
		}

		void addMirrorButton_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			if(playing)
			{
				GenerateOperationPanel_Mirror();
				Invalidate();
				if(PanelStatusChange != null)
				{
					PanelStatusChange(true);
				}
			}
		}

		void addMirrorButton_GotFocus(object sender, EventArgs e)
		{
			DisposeEntryPanel();
		}
	
		protected virtual void resetButtons()
		{
			startSIPButton.Active = false;
			cancelSIPButton.Active = false;
			installSIPButton.Active = false;
			slaButton.Active = false;
			upgradeAppButton.Active = false;
			upgradeMemDiskButton.Active = false;
			addMirrorButton.Active = false;
		}

//		private void toggleView_ButtonPressed(object sender, ImageButtonEventArgs args)
//		{
//			_ts.CurrentView = (_ts.CurrentView == TransitionScreen.ViewScreen.ACTIVE)
//				?  TransitionScreen.ViewScreen.INACTIVE :  TransitionScreen.ViewScreen.ACTIVE;
//		}

		protected virtual TransUpgradeAppControl CreateTransUpgradeAppControl (IDataEntryControlHolder mainPanel,
		                                                                       IncidentApplier iApplier, NodeTree nt, 
		                                                                       bool usingmins, Color OperationsBackColor, Color GroupPanelBackColor)
		{
			return new TransUpgradeAppControl(mainPanel, iApplier, nt, usingmins,
			                                  OperationsBackColor, GroupPanelBackColor);
		}

		void projectsNode_ChildrenChanged (Node sender, Node child)
		{
			UpdateSIPButtons();
		}

		public virtual void ShowEntryPanel (Control control)
		{
			shownControl = control;
		}

		void UpdateSIPButtons ()
		{
			int projects = 0;
			foreach (Node project in projectsNode)
			{
				if (project.GetIntAttribute("createdinround", 0) == round)
				{
					projects++;
				}
			}

			startSIPButton.Enabled = (projects < SkinningDefs.TheInstance.GetIntData("transition_projects_count", 4));
			cancelSIPButton.Enabled = true;
		}
	}
}