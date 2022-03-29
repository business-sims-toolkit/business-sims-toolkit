using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;

using LibCore;
using Network;
using CoreUtils;
using CommonGUI;
using TransitionObjects;
using IncidentManagement;
using Polestar_PM.OpsGUI;

using GameManagement;

namespace Polestar_PM.OpsScreen
{

	/// <summary>
	/// Summary description for ControlPanel.
	/// This is just the start of the new buttons (PlayVideoMsg, AddNewTask and ListTasks)
	/// There are more buttons to add 
	/// </summary>
	public class PM_OpsControlPanel : FlickerFreePanel, ITimedClass, IDataEntryControlHolderWithShowPanel
	{
		public delegate void PanelClosedHandler();
		public event PanelClosedHandler PanelClosed;

		public delegate void ModalActionHandler(bool entering);
		public event ModalActionHandler ModalAction;

		public ImageTextButton ChangePMO_Button;
		public ImageTextButton SetUpProject_Button;
		public ImageTextButton SetStaff_Button;
		public ImageTextButton DropTasks_Button;
		public ImageTextButton Handover_Button;
		public ImageTextButton PauseProject_Button;
		public ImageTextButton CancelProject_Button;
		public ImageTextButton FSC_Button;
		public ImageTextButton Upgrade_Button;
		public ImageTextButton ChangeCard_Button;
		public ImageTextButton PlanProject_Button;
		public ImageTextButton Bubble_Button;
		public ImageTextButton Misc_Button;
		ImageTextButton activityLogButton;
		protected ArrayList allbuttons = new ArrayList();
		
		public bool useChangeCard = true;	

		protected Control shownControl;
		protected NodeTree _network;
		protected bool playing = false;

		protected IncidentApplier _iApplier;
		protected MirrorApplier _mirrorApplier;
		protected ProjectManager _prjmanager;
		protected int popup_xposition = 5;
		protected int popup_yposition = 475;
		protected int popup_width = 484;
		protected int popup_height = 255;
		
		protected int buttonwidth = 50;
		protected int buttonheight = 27;
		protected int buttonseperation = 0;
		protected int _round;
		protected int _round_maxmins;
		protected Boolean MyIsTrainingFlag;
		protected Color MyGroupPanelBackColor;
		protected Color MyOperationsBackColor;
		protected Font MyDefaultSkinFontNormal8 = null;
		protected Font MyDefaultSkinFontBold8 = null;

		protected bool AllowResourcePlaninRound1 = false;
		protected bool InsideModalAction = false;


		//Use the Change Cards in the named Round and normal Upgrade otherwise
		protected int UseChangeCardRound = 1;
		protected NetworkProgressionGameFile _gameFile;

		Polestar_PM.OpsEngine.PM_OpsEngine opsEngine;

		public PM_OpsControlPanel(NetworkProgressionGameFile gameFile, NodeTree nt, IncidentApplier iApplier, 
			MirrorApplier mirrorApplier, int round, int round_length_mins, Boolean IsTrainingFlag, 
			Color OperationsBackColor, 	Color GroupPanelBackColor, Polestar_PM.OpsEngine.PM_OpsEngine opsEngine)
		{
			_gameFile = gameFile;
			MyIsTrainingFlag = IsTrainingFlag;
			MyOperationsBackColor = OperationsBackColor;
			MyGroupPanelBackColor = GroupPanelBackColor;

			this.opsEngine = opsEngine;

			string fontname =  SkinningDefs.TheInstance.GetData("fontname");
			MyDefaultSkinFontNormal8 = ConstantSizeFont.NewFont(fontname,8);
			MyDefaultSkinFontBold8 = ConstantSizeFont.NewFont(fontname,8,FontStyle.Bold);

			string AllowResourcePlaninRound1_str = SkinningDefs.TheInstance.GetData("show_resource_plan_in_round1","false");
			if (AllowResourcePlaninRound1_str.IndexOf("true")>-1)
			{
				AllowResourcePlaninRound1 = true;
			}

			_round = round;
			_round_maxmins = round_length_mins;
			_mirrorApplier = mirrorApplier;
			_iApplier = iApplier;
			
			_network = nt;
			BackColor = OperationsBackColor;

			Build_Buttons();

			TimeManager.TheInstance.ManageClass(this);
		}

		public virtual void DisableAllButtons()
		{
			DisposeEntryPanel();
			foreach (ImageTextButton itb in allbuttons)
			{
				itb.Enabled = false;
			}
		}

		protected virtual void Build_Buttons()
		{
			bool showIT = true;

			SetUpProject_Button = new ImageTextButton(0);
			SetUpProject_Button.SetVariants("images/buttons/blank.png");
			SetUpProject_Button.ButtonPressed +=new CommonGUI.ImageButton.ImageButtonEventArgsHandler(SetUpProject_ButtonPressed);
			SetUpProject_Button.SetButtonText("Setup", Color.White, Color.Black, Color.White, Color.DimGray);
			SetUpProject_Button.ButtonFont = this.MyDefaultSkinFontBold8;
			this.Controls.Add(SetUpProject_Button);
			allbuttons.Add(SetUpProject_Button);

			//Round dendant display name
			SetStaff_Button = new ImageTextButton (0);
			SetStaff_Button.SetVariants("images/buttons/blank.png");
			SetStaff_Button.ButtonPressed +=new CommonGUI.ImageButton.ImageButtonEventArgsHandler(SetStaff_ButtonPressed);
			SetStaff_Button.SetButtonText("Resources", Color.White, Color.Black, Color.White, Color.DimGray);
			SetStaff_Button.ButtonFont = this.MyDefaultSkinFontBold8;
			this.Controls.Add(SetStaff_Button);
			allbuttons.Add(SetStaff_Button);

			DropTasks_Button = new ImageTextButton (0);
			DropTasks_Button.SetVariants("images/buttons/blank.png");
			DropTasks_Button.ButtonPressed += new CommonGUI.ImageButton.ImageButtonEventArgsHandler(DropTasks_ButtonPressed);
			DropTasks_Button.SetButtonText("Descope", Color.White, Color.Black, Color.White, Color.DimGray);
			DropTasks_Button.ButtonFont = this.MyDefaultSkinFontBold8;
			this.Controls.Add(DropTasks_Button);
			allbuttons.Add(DropTasks_Button);

			Handover_Button = new ImageTextButton (0);
			Handover_Button.SetVariants("images/buttons/blank.png");
			Handover_Button.ButtonPressed += new CommonGUI.ImageButton.ImageButtonEventArgsHandler(Handover_Button_ButtonPressed);
			Handover_Button.SetButtonText("Handover", Color.White, Color.Black, Color.White, Color.DimGray);
			Handover_Button.ButtonFont = this.MyDefaultSkinFontBold8;
			this.Controls.Add(Handover_Button);
			allbuttons.Add(Handover_Button);

			if (! showIT)
			{
				if (_round >= 3)
				{
					Handover_Button.SetButtonText("Iteration");
				}
				else
				{
					//Handover_Button.Hide();
					Handover_Button.Enabled = false;
				}
			}

			PauseProject_Button = new ImageTextButton (0);
			PauseProject_Button.SetVariants("images/buttons/blank.png");
			PauseProject_Button.ButtonPressed += new CommonGUI.ImageButton.ImageButtonEventArgsHandler(PauseProject_Button_ButtonPressed);
			PauseProject_Button.SetButtonText("Pause", Color.White, Color.Black, Color.White, Color.DimGray);
			PauseProject_Button.ButtonFont = this.MyDefaultSkinFontBold8;
			this.Controls.Add(PauseProject_Button);
			allbuttons.Add(PauseProject_Button);

			CancelProject_Button = new ImageTextButton (0);
			CancelProject_Button.SetVariants("images/buttons/blank.png");
			CancelProject_Button.ButtonPressed += new CommonGUI.ImageButton.ImageButtonEventArgsHandler(CancelProject_Button_ButtonPressed);
			CancelProject_Button.SetButtonText("Cancel", Color.White, Color.Black, Color.White, Color.DimGray);
			CancelProject_Button.ButtonFont = this.MyDefaultSkinFontBold8;
			this.Controls.Add(CancelProject_Button);
			allbuttons.Add(CancelProject_Button);

			if (_round==UseChangeCardRound)
			{
				if (useChangeCard)
				{
					ChangeCard_Button = new ImageTextButton (0);
					ChangeCard_Button.SetVariants("images/buttons/blank.png");
					ChangeCard_Button.ButtonPressed += new CommonGUI.ImageButton.ImageButtonEventArgsHandler(ChangeCard_Button_ButtonPressed);
					ChangeCard_Button.SetButtonText("Changes", Color.White, Color.Black, Color.White, Color.DimGray);
					ChangeCard_Button.ButtonFont = this.MyDefaultSkinFontBold8;
					this.Controls.Add(ChangeCard_Button);
					allbuttons.Add(ChangeCard_Button);

					if (! showIT)
					{
						//ChangeCard_Button.Hide();
						ChangeCard_Button.Enabled = false;
					}
				}
				else
				{
					FSC_Button = new ImageTextButton (0);
					FSC_Button.SetVariants("images/buttons/blank.png");
					FSC_Button.ButtonPressed += new CommonGUI.ImageButton.ImageButtonEventArgsHandler(FSC_Button_ButtonPressed);
					FSC_Button.SetButtonText("F.S.C", Color.White, Color.Black, Color.White, Color.DimGray);
					FSC_Button.ButtonFont = this.MyDefaultSkinFontBold8;
					this.Controls.Add(FSC_Button);
					allbuttons.Add(FSC_Button);

					if (! showIT)
					{
						//FSC_Button.Hide();
						FSC_Button.Enabled = false;
					}
				}
			}
			else
			{
				Upgrade_Button = new ImageTextButton (0);
				Upgrade_Button.SetVariants("images/buttons/blank.png");
				Upgrade_Button.ButtonPressed += new CommonGUI.ImageButton.ImageButtonEventArgsHandler(Upgrade_Button_ButtonPressed);
				Upgrade_Button.SetButtonText("Upgrades", Color.White, Color.Black, Color.White, Color.DimGray);
				Upgrade_Button.ButtonFont = this.MyDefaultSkinFontBold8;
				this.Controls.Add(Upgrade_Button);
				allbuttons.Add(Upgrade_Button);

				if (! showIT)
				{
					//Upgrade_Button.Hide();
					Upgrade_Button.Enabled = false;
				}
			}

			Misc_Button = new ImageTextButton(0);
			Misc_Button.SetVariants("images/buttons/blank.png");
			Misc_Button.ButtonPressed += new CommonGUI.ImageButton.ImageButtonEventArgsHandler(Misc_Button_ButtonPressed);
			Misc_Button.SetButtonText("Options", Color.White, Color.Black, Color.White, Color.DimGray); 
			Misc_Button.ButtonFont = this.MyDefaultSkinFontBold8;
			this.Controls.Add(Misc_Button);
			allbuttons.Add(Misc_Button);

			activityLogButton = new ImageTextButton(0);
			activityLogButton.SetVariants("images/buttons/blank.png");
			activityLogButton.ButtonPressed += new ImageButton.ImageButtonEventArgsHandler(activityLogButton_ButtonPressed);
			activityLogButton.SetButtonText("Log", Color.White, Color.Black, Color.White, Color.DimGray);
			activityLogButton.ButtonFont = MyDefaultSkinFontBold8;
			Controls.Add(activityLogButton);
			allbuttons.Add(activityLogButton);
		}

		#region ITimedClass Members

		public virtual void Start()
		{
			//playmsg_Button.Enabled = true;
			//newTaskButton.Enabled = true;
			//viewTasksButton.Enabled = true;

			//Check for Round Sensitive buttons 
			if(_round > 1)
			{
			}
			else
			{
			}

			playing = true;
		}

		public virtual void FastForward(double timesRealTime)
		{
			// TODO:  Add RacingControlPanel.FastForward implementation
		}

		public virtual void Reset()
		{
			// TODO:  Add RacingControlPanel.Reset implementation
		}

		public virtual void Stop()
		{
			//
			DisposeEntryPanel();
			playing = false;
		}

		#endregion

		protected virtual void ResetButtons ()
		{
			//ChangePMO_Button.Active = false;
			SetUpProject_Button.Active = false;
			SetStaff_Button.Active = false;
			DropTasks_Button.Active = false;
			Handover_Button.Active = false;
			PauseProject_Button.Active = false;
			CancelProject_Button.Active = false;
			Misc_Button.Active = false;
			activityLogButton.Active = false;

			if (PlanProject_Button != null)
			{
				PlanProject_Button.Active = false;
			}
			if (_round==UseChangeCardRound)
			{
				if (useChangeCard)
				{
					ChangeCard_Button.Active = false;
				}
				else
				{
					FSC_Button.Active = false;
				}
			}
			else
			{
				Upgrade_Button.Active = false;
				//Bubble_Button.Active = false;
			}

			//if (Upgrade_Button != null)
			//{
			//	this.Upgrade_Button.Enabled = false;
			//}
		}

		public virtual void RePositionButtons (int NewButtonsWidth, int NewButtonHeight, int buttonseperation)
		{
			buttonwidth = NewButtonsWidth;
			buttonheight = NewButtonHeight;

			//ChangePMO_Button.Size = new Size(buttonwidth, buttonheight);
			//ChangePMO_Button.Location = new Point((buttonwidth + buttonseperation)*0, 0);
			if (SetUpProject_Button != null)
			{
				SetUpProject_Button.Size = new Size(buttonwidth, buttonheight);
				SetUpProject_Button.Location = new Point((buttonwidth + buttonseperation) * 0, 0);
			}

			if (SetStaff_Button != null)
			{
				SetStaff_Button.Size = new Size(buttonwidth, buttonheight);
				SetStaff_Button.Location = new Point((buttonwidth + buttonseperation) * 1, 0);
			}

			if (DropTasks_Button != null)
			{
				DropTasks_Button.Size = new Size(buttonwidth, buttonheight);
				DropTasks_Button.Location = new Point((buttonwidth + buttonseperation) * 2, 0);
			}

			if (Handover_Button != null)
			{
				Handover_Button.Size = new Size(buttonwidth, buttonheight);
				Handover_Button.Location = new Point((buttonwidth + buttonseperation) * 3, 0);
			}

			if (_round==UseChangeCardRound)
			{
				if (useChangeCard)
				{
					if (ChangeCard_Button != null)
					{
						ChangeCard_Button.Size = new Size(buttonwidth, buttonheight);
						ChangeCard_Button.Location = new Point((buttonwidth + buttonseperation) * 4, 0);
					}
				}
				else
				{
					if (FSC_Button != null)
					{
						FSC_Button.Size = new Size(buttonwidth, buttonheight);
						FSC_Button.Location = new Point((buttonwidth + buttonseperation) * 4, 0);
					}
				}
			}
			else
			{
				if (Upgrade_Button != null)
				{
					Upgrade_Button.Size = new Size(buttonwidth, buttonheight);
					Upgrade_Button.Location = new Point((buttonwidth + buttonseperation) * 4, 0);
				}
			}


			if (PauseProject_Button != null)
			{
				PauseProject_Button.Size = new Size(buttonwidth, buttonheight);
				PauseProject_Button.Location = new Point((buttonwidth + buttonseperation) * 5, 0);
			}

			if (CancelProject_Button != null)
			{
				CancelProject_Button.Size = new Size(buttonwidth, buttonheight);
				CancelProject_Button.Location = new Point((buttonwidth + buttonseperation) * 6, 0);
			}

			if (Misc_Button != null)
			{
				Misc_Button.Size = new Size(buttonwidth, buttonheight);
				Misc_Button.Location = new Point((buttonwidth + buttonseperation) * 7, 0);
			}

			if (activityLogButton != null)
			{
				activityLogButton.Size = new Size(buttonwidth, buttonheight);
				activityLogButton.Location = new Point((buttonwidth + buttonseperation) * 8, 0);
			}

			int offset = (buttonwidth+buttonseperation)*1 + 5;
		}

		public virtual void SwapToOtherPanel(int which_operation)
		{
			if (shownControl != null)
			{
				shownControl.Dispose();
				shownControl = null;
			}
			if (PanelClosed != null)
			{
				PanelClosed();
			}
			switch (which_operation)
			{ 
				case 1: //PMO 
					DataEntryPanel_ChangePMO myChangePMO_EntryPanel = new DataEntryPanel_ChangePMO(this, this._network);
					//myChangePMO_EntryPanel.Owner = Parent.Parent as Form; // yuck
					myChangePMO_EntryPanel.Size = new Size(popup_width, popup_height);
					myChangePMO_EntryPanel.Location = new Point(popup_xposition, popup_yposition);
					this.Parent.Controls.Add(myChangePMO_EntryPanel);
					myChangePMO_EntryPanel.BringToFront();
					//DisposeEntryPanel();
					shownControl = myChangePMO_EntryPanel;
					//((ImageButton)sender).Active = true;
					myChangePMO_EntryPanel.SetFocus();
					break;
				case 3: //Predicted Market
					ChangePredictedMarketPos myPM_ChngeMarketPosDisplay = new ChangePredictedMarketPos(this, this._network);
					//myPM_ChngeMarketPos.Owner = Parent.Parent as Form; // yuck
					myPM_ChngeMarketPosDisplay.Size = new Size(popup_width, popup_height);
					myPM_ChngeMarketPosDisplay.Location = new Point(popup_xposition, popup_yposition);
					this.Parent.Controls.Add(myPM_ChngeMarketPosDisplay);
					myPM_ChngeMarketPosDisplay.BringToFront();
					//DisposeEntryPanel();
					shownControl = myPM_ChngeMarketPosDisplay;
					//((ImageButton)sender).Active = true;
					myPM_ChngeMarketPosDisplay.SetFocus();
					break;
				case 4: //Show Plan
					if (null != ModalAction)
					{
						bool entering = true;
						SuspendButtonsforModalAction(entering);
						InsideModalAction = entering;
						ModalAction(entering);
					}
					GamePlanDisplay myGamePlanDisplay = new GamePlanDisplay(this, _gameFile, this._network, _round);
					// Create the report - this shouldn't be in the display class.
					myGamePlanDisplay.RefreshTheTimeSheets(opsEngine.GetProjectManager());
					myGamePlanDisplay.LoadData();
					myGamePlanDisplay.Size = new Size(1024, 688);
					myGamePlanDisplay.Location = new Point(0, 38);
					this.Parent.Controls.Add(myGamePlanDisplay);
					myGamePlanDisplay.BringToFront();
					//DisposeEntryPanel();
					shownControl = myGamePlanDisplay;
					//((ImageButton)sender).Active = true;
					myGamePlanDisplay.Focus();
					break;
				case 5: //Show Analysis Charts 
					PM_PopupAnalysisChartsDisplay myPM_AChartsDisplay = new PM_PopupAnalysisChartsDisplay(this, this._network, _round);
					//myPM_AChartsDisplay.Owner = Parent.Parent as Form; // yuck
					myPM_AChartsDisplay.Size = new Size(popup_width, popup_height);
					myPM_AChartsDisplay.Location = new Point(popup_xposition, popup_yposition);
					this.Parent.Controls.Add(myPM_AChartsDisplay);
					myPM_AChartsDisplay.BringToFront();
					myPM_AChartsDisplay.SetSmallPosAndSize(popup_xposition, popup_yposition, popup_width, popup_height);
					myPM_AChartsDisplay.SetLargePosAndSize(84, 40, 731, 430);
					//DisposeEntryPanel();
					shownControl = myPM_AChartsDisplay;
					//((ImageButton)sender).Active = true;
					myPM_AChartsDisplay.Focus();
					break;
				case 6: //fix Experts Panel
					DataEntryPanel_FixExpertsPanel myFixExperts_EntryPanel = new DataEntryPanel_FixExpertsPanel(this, this._network);
					//myFixExperts_EntryPanel.Owner = Parent.Parent as Form; // yuck
					myFixExperts_EntryPanel.Size = new Size(popup_width, popup_height);
					myFixExperts_EntryPanel.Location = new Point(popup_xposition, popup_yposition);
					this.Parent.Controls.Add(myFixExperts_EntryPanel);
					myFixExperts_EntryPanel.BringToFront();
					//DisposeEntryPanel();
					shownControl = myFixExperts_EntryPanel;
					//((ImageButton)sender).Active = true;
					myFixExperts_EntryPanel.SetFocus();
					break;
				case 7: //Change Experts Status Panel
					DataEntryPanel_ChangeExpertsStatus myChangeExpertsStatus_EntryPanel = new DataEntryPanel_ChangeExpertsStatus(this, this._network);
					//myChangeExpertsStatus_EntryPanel.Owner = Parent.Parent as Form; // yuck
					myChangeExpertsStatus_EntryPanel.Size = new Size(popup_width, popup_height);
					myChangeExpertsStatus_EntryPanel.Location = new Point(popup_xposition, popup_yposition);
					this.Parent.Controls.Add(myChangeExpertsStatus_EntryPanel);
					myChangeExpertsStatus_EntryPanel.BringToFront();
					//DisposeEntryPanel();
					shownControl = myChangeExpertsStatus_EntryPanel;
					//((ImageButton)sender).Active = true;
					myChangeExpertsStatus_EntryPanel.SetFocus();
					break;
			}
		}

		public virtual void DisposeEntryPanel_indirect(int which)
		{ 
		}

		public void SuspendButtonsforModalAction(bool entering)
		{
			foreach (ImageButton ib in this.allbuttons)
			{
				if (entering)
				{
					ib.Tag = ib.Enabled;
					ib.Enabled = false;
				}
				else
				{
					if (ib.Tag != null)
					{
						ib.Enabled = (bool)ib.Tag;
					}
				}
			}
		}

		public virtual void DisposeEntryPanel()
		{
			if (InsideModalAction)
			{
				SuspendButtonsforModalAction(false);
				InsideModalAction = false;
				if (null != ModalAction)
				{
					ModalAction(false);
				}
			}

			ResetButtons();
			this.Invalidate();

			if(shownControl != null)
			{
				shownControl.Dispose();
				shownControl = null;
			}
			if(PanelClosed != null)
			{
				PanelClosed();
			}
		}

		/// <summary>
		/// Dispose ...
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose(bool disposing)
		{
			if (MyDefaultSkinFontBold8 != null)
			{
				MyDefaultSkinFontBold8.Dispose();
				MyDefaultSkinFontBold8 = null;
			}
			if (MyDefaultSkinFontNormal8 != null)
			{
				MyDefaultSkinFontNormal8.Dispose();
				MyDefaultSkinFontNormal8 = null;
			}

			if(disposing)
			{
				DisposeEntryPanel();
				TimeManager.TheInstance.UnmanageClass(this);
			}
			base.Dispose (disposing);
		}

		public IncidentApplier IncidentApplier
		{
			get
			{
				return _iApplier;
			}
		}

		public void SetPopUpPosition(int x, int y)
		{
			popup_xposition = x;
			popup_yposition = y;
		}

		public void SetPopUpSize(int width, int height)
		{
			popup_width = width;
			popup_height = height;
		}

		protected void ChangePMO_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			DataEntryPanel_ChangePMO myChangePMO_EntryPanel = new DataEntryPanel_ChangePMO(this, this._network);
			//myChangePMO_EntryPanel.Owner = Parent.Parent as Form; // yuck
			myChangePMO_EntryPanel.Size = new Size(popup_width, popup_height);
			myChangePMO_EntryPanel.Location = new Point(popup_xposition, popup_yposition);
			this.Parent.Controls.Add(myChangePMO_EntryPanel);
			myChangePMO_EntryPanel.BringToFront();

			DisposeEntryPanel();
			shownControl = myChangePMO_EntryPanel;
			((ImageButton)sender).Active = true;
			myChangePMO_EntryPanel.SetFocus();
		}

		protected void SetUpProject_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			DataEntryPanel_SetupProject myPrjSetup_EntryPanel = new DataEntryPanel_SetupProject (this, _gameFile, this._network, _round);
			//myPrjResEdit_EntryPanel.Owner = Parent.Parent as Form; // yuck
			myPrjSetup_EntryPanel.Size = new Size (popup_width,popup_height);
			myPrjSetup_EntryPanel.Location =  new Point (popup_xposition, popup_yposition);
			this.Parent.Controls.Add(myPrjSetup_EntryPanel);
			myPrjSetup_EntryPanel.BringToFront();

			DisposeEntryPanel();
			shownControl = myPrjSetup_EntryPanel;
			((ImageButton) sender).Active = true;
			myPrjSetup_EntryPanel.Focus();
		}

		protected virtual void SetStaff_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			DataEntryPanel_ResourceEditProject myPrjResEdit_EntryPanel = new DataEntryPanel_ResourceEditProject (this, this._network, this._gameFile);
			//myPrjResEdit_EntryPanel.Owner = Parent.Parent as Form; // yuck
			myPrjResEdit_EntryPanel.Size = new Size (popup_width,popup_height);
			myPrjResEdit_EntryPanel.Location =  new Point (popup_xposition, popup_yposition);
			this.Parent.Controls.Add(myPrjResEdit_EntryPanel);
			myPrjResEdit_EntryPanel.BringToFront();

			DisposeEntryPanel();
			shownControl = myPrjResEdit_EntryPanel;
			((ImageButton) sender).Active = true;
			myPrjResEdit_EntryPanel.Focus();
		}

		protected void DropTasks_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			if (_round == 3)
			{
				//Drop a Critical Path Section 
				DataEntryPanel_DescopeCritPathProject myPrjDropCritPath_EntryPanel = new DataEntryPanel_DescopeCritPathProject(this, this._network, this._gameFile);
				//myPrjCancel_EntryPanel.Owner = Parent.Parent as Form; // yuck
				myPrjDropCritPath_EntryPanel.Size = new Size(popup_width, popup_height);
				myPrjDropCritPath_EntryPanel.Location = new Point(popup_xposition, popup_yposition);
				this.Parent.Controls.Add(myPrjDropCritPath_EntryPanel);
				myPrjDropCritPath_EntryPanel.BringToFront();

				DisposeEntryPanel();
				shownControl = myPrjDropCritPath_EntryPanel;
				((ImageButton)sender).Active = true;
				myPrjDropCritPath_EntryPanel.Focus();
			}
			else
			{
				//Drop a percentage of tasks
				DataEntryPanel_DescopeProject myPrjDescope_EntryPanel = new DataEntryPanel_DescopeProject(this, this._network, this._gameFile);
				//myPrjCancel_EntryPanel.Owner = Parent.Parent as Form; // yuck
				myPrjDescope_EntryPanel.Size = new Size(popup_width, popup_height);
				myPrjDescope_EntryPanel.Location = new Point(popup_xposition, popup_yposition);
				this.Parent.Controls.Add(myPrjDescope_EntryPanel);
				myPrjDescope_EntryPanel.BringToFront();

				DisposeEntryPanel();
				shownControl = myPrjDescope_EntryPanel;
				((ImageButton)sender).Active = true;
				myPrjDescope_EntryPanel.Focus();
			}
		}

		protected void Handover_Button_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			DataEntryPanel_Handover myPrjHandover_EntryPanel = new DataEntryPanel_Handover(this, this._network, this._gameFile, false);
			//myPrjCancel_EntryPanel.Owner = Parent.Parent as Form; // yuck
			myPrjHandover_EntryPanel.Size = new Size (popup_width,popup_height);
			myPrjHandover_EntryPanel.Location =  new Point (popup_xposition, popup_yposition);
			this.Parent.Controls.Add(myPrjHandover_EntryPanel);
			myPrjHandover_EntryPanel.BringToFront();

			DisposeEntryPanel();
			shownControl = myPrjHandover_EntryPanel;
			((ImageButton) sender).Active = true;
			myPrjHandover_EntryPanel.Focus();
		}

		public void ShowEntryPanel (Control control)
		{
			shownControl = control;
		}

		protected void PauseProject_Button_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			DataEntryPanel_PauseProject myPrjPause_EntryPanel = new DataEntryPanel_PauseProject(this, this._network, this._gameFile);
			//myPrjPause_EntryPanel.Owner = Parent.Parent as Form; // yuck
			myPrjPause_EntryPanel.Size = new Size (popup_width,popup_height);
			myPrjPause_EntryPanel.Location =  new Point (popup_xposition, popup_yposition);
			this.Parent.Controls.Add(myPrjPause_EntryPanel);
			myPrjPause_EntryPanel.BringToFront();

			DisposeEntryPanel();
			shownControl = myPrjPause_EntryPanel;
			((ImageButton) sender).Active = true;
			myPrjPause_EntryPanel.Focus();
		}

		protected void CancelProject_Button_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			DataEntryPanel_Cancel_Actions myPrjCancel_EntryPanel = new DataEntryPanel_Cancel_Actions(this, this._network, this._round);
			//myPrjCancel_EntryPanel.Owner = Parent.Parent as Form; // yuck
			myPrjCancel_EntryPanel.Size = new Size (popup_width,popup_height);
			myPrjCancel_EntryPanel.Location =  new Point (popup_xposition, popup_yposition);
			this.Parent.Controls.Add(myPrjCancel_EntryPanel);
			myPrjCancel_EntryPanel.BringToFront();

			DisposeEntryPanel();
			shownControl = myPrjCancel_EntryPanel;
			((ImageButton) sender).Active = true;
			myPrjCancel_EntryPanel.Focus();
		}

		protected void FSC_Button_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			DataEntryPanel_FSC myFSC_EntryPanel = new DataEntryPanel_FSC (this, this._network);
			//myPrjCancel_EntryPanel.Owner = Parent.Parent as Form; // yuck
			myFSC_EntryPanel.Size = new Size (popup_width,popup_height);
			myFSC_EntryPanel.Location =  new Point (popup_xposition, popup_yposition);
			this.Parent.Controls.Add(myFSC_EntryPanel);
			myFSC_EntryPanel.BringToFront();

			DisposeEntryPanel();
			shownControl = myFSC_EntryPanel;
			((ImageButton) sender).Active = true;
			myFSC_EntryPanel.Focus();
		}

		protected void ChangeCard_Button_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			DataEntryPanel_ChangeCard myChangeCard_EntryPanel = new DataEntryPanel_ChangeCard (this, this._network);
			//myChangeCard_EntryPanel.Owner = Parent.Parent as Form; // yuck
			myChangeCard_EntryPanel.Size = new Size (popup_width,popup_height);
			myChangeCard_EntryPanel.Location =  new Point (popup_xposition, popup_yposition);
			this.Parent.Controls.Add(myChangeCard_EntryPanel);
			myChangeCard_EntryPanel.BringToFront();

			DisposeEntryPanel();
			shownControl = myChangeCard_EntryPanel;
			((ImageButton) sender).Active = true;
			myChangeCard_EntryPanel.Focus();
		}

		protected void Upgrade_Button_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			DataEntryPanel_UpgradePanel myUpgrade_EntryPanel = new DataEntryPanel_UpgradePanel (this, this._network);
			//myPrjCancel_EntryPanel.Owner = Parent.Parent as Form; // yuck
			myUpgrade_EntryPanel.Size = new Size (popup_width,popup_height);
			myUpgrade_EntryPanel.Location =  new Point (popup_xposition, popup_yposition);
			this.Parent.Controls.Add(myUpgrade_EntryPanel);
			myUpgrade_EntryPanel.BringToFront();

			DisposeEntryPanel();
			shownControl = myUpgrade_EntryPanel;
			((ImageButton) sender).Active = true;
			myUpgrade_EntryPanel.Focus();
		}

		protected void PlanProject_Button_ButtonPressed(object sender, ImageButtonEventArgs args)
		{

			GamePlanDisplay myGamePlanDisplay = new GamePlanDisplay(this, _gameFile, this._network, _round);
			// Create the report - this shouldn't be in the display class.
			myGamePlanDisplay.RefreshTheTimeSheets(opsEngine.GetProjectManager());
			myGamePlanDisplay.LoadData();

			myGamePlanDisplay.Size = new Size (1024, 688);
			myGamePlanDisplay.Location = new Point (0, 38);
			this.Parent.Controls.Add(myGamePlanDisplay);
			myGamePlanDisplay.BringToFront();

			DisposeEntryPanel();
			shownControl = myGamePlanDisplay;
			((ImageButton) sender).Active = true;
			myGamePlanDisplay.Focus();
		}

		protected void Misc_Button_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			DataEntryPanel_MiscChoice myMisc_EntryPanel = new DataEntryPanel_MiscChoice(this, this._network, _round, opsEngine, _gameFile);
			//myMisc_EntryPanel.Owner = Parent.Parent as Form; // yuck
			myMisc_EntryPanel.Size = new Size(popup_width, popup_height);
			myMisc_EntryPanel.Location = new Point(popup_xposition, popup_yposition);
			this.Parent.Controls.Add(myMisc_EntryPanel);
			myMisc_EntryPanel.BringToFront();

			DisposeEntryPanel();
			shownControl = myMisc_EntryPanel;
			((ImageButton)sender).Active = true;
			myMisc_EntryPanel.Focus();
		}

		void activityLogButton_ButtonPressed (object sender, ImageButtonEventArgs args)
		{
			ActivityLog activityLog = new ActivityLog(this, _gameFile);
			activityLog.Size = new Size(popup_width, popup_height);
			activityLog.Location = new Point(popup_xposition, popup_yposition);
			this.Parent.Controls.Add(activityLog);
			activityLog.BringToFront();

			DisposeEntryPanel();
			shownControl = activityLog;
			((ImageButton) sender).Active = true;
			activityLog.SetFocus();
		}
	}
}