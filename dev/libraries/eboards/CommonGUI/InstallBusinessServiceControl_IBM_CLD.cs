using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;

using LibCore;
using Network;

using CoreUtils;
using IncidentManagement;
using TransitionObjects;

namespace CommonGUI
{
	/// <summary>
	/// Summary description for InstallBusinessServiceControl_IBM_CLD
	/// This install system handles the difference of installing AppsOnly and AppswithHW
	/// There is a secondary screen to allow the players to select the hardware 
	/// 
	/// </summary>
	public class InstallBusinessServiceControl_IBM_CLD : FlickerFreePanel
	{
		protected IDataEntryControlHolder _mainPanel;
		protected NodeTree _Network;
		protected ProjectManager _prjmanager;
		protected Node projectIncomingRequestQueueHandle;
		protected Node install_server_locations_node;
		protected Node chosen_install_data_node;
		protected bool hardware_system_selected = false;

		protected ImageTextButton okButton;
		protected ImageTextButton cancelButton;

		protected ArrayList buttonArray;

		protected Panel chooseUpgradePanel;
		protected Panel chooseServicePanel;
		protected Panel chooseTimePanel;
		protected Panel chooseHardwareLocationPanel;

		protected Label timeLabel;
		protected Label locationLabel;
		protected Label PanelTitleLabel;
		protected Label ChosenServiceLabel;
		protected Label SLA_TimeLabel;
		protected Label ChosenServiceTitleLabel;
		protected Label AutoTimeLabel= null;
		//protected ComboBox ServiceLevelSelector;
		protected EntryBox ServiceLevelPriority;

		protected EntryBox whenTextBox;
		protected EntryBox locationBox;
		protected EntryBox sla_TimeBox;

		protected ArrayList disposeControls = new ArrayList();

		protected ProjectRunner servicePicked = null;
		protected int maxmins = 24;
		protected int autoTimeSecs = 0; 
		//protected Hashtable UpgradeLocationLookup = new Hashtable();
		protected Hashtable UpgradeLocationLookup_AppOnly = new Hashtable();
		protected Hashtable UpgradeLocationLookup_AppHW = new Hashtable();
		protected Hashtable UpgradePriorityLookup_AppOnly = new Hashtable();

		protected Color AutoTimeBackColor = Color.Silver;
		protected Color AutoTimeTextColor = Color.Black;

		string filename_huge = "\\images\\buttons\\blank_huge.png";
		string filename_mid = "\\images\\buttons\\blank_med.png";
		string filename_short = "\\images\\buttons\\blank_small.png";

		Boolean MyIsTrainingMode = false;

		protected FocusJumper focusJumper;

		protected string install_title_name = "Install";
		protected string install_name = "install";
		protected bool requireHWSelection = false;
		protected int app_only_ServiceLevelPriority = 1;

		//skin stuff
		Color upColor = Color.Black;

		Color downColor = Color.White;
		Color hoverColor = Color.Green;
		Color disabledColor = Color.DarkGray;
		Color MyGroupPanelBackColor;
		Color MyOperationsBackColor;
		Color MyTitleForeColor = Color.Black;
		protected Font MyDefaultSkinFontNormal10 = null;
		protected Font MyDefaultSkinFontBold10 = null;
		protected Font MyDefaultSkinFontBold12 = null;
		protected Font MyDefaultSkinFontBold24 = null;

		int servicebtn_xoffset = 10;	//Overall Offset of server buttons  
		int servicebtn_yoffset = 20;	//Overall Offset of server buttons  
		int servicebtn_x_step = 230;	//horizontal step between different columns
		int servicebtn_y_step = 23;		//vertical stepping between sever buttons  
		int numBtns_AppOnly = 0;
		int numBtns_AppHW = 0;
		bool auto_translate = true;

		Label errorLabel;
		public Color errorLabel_foreColor = Color.Red;

		public InstallBusinessServiceControl_IBM_CLD(IDataEntryControlHolder mainPanel, NodeTree model, 
			ProjectManager prjmanager, Boolean IsTrainingMode, Color OperationsBackColor, 
			Color GroupPanelBackColor)
		{
			SetStyle(ControlStyles.Selectable, true);

			install_title_name = SkinningDefs.TheInstance.GetData("transition_install_title","Install");
			install_name = SkinningDefs.TheInstance.GetData("transition_install_name","install");

			string errmsg_overridecolor  =  SkinningDefs.TheInstance.GetData("race_errormsg_override_color");
			if (errmsg_overridecolor != "")
			{
				errorLabel_foreColor =  SkinningDefs.TheInstance.GetColorData("race_errormsg_override_color");
			}

			string autotimebackcolor =  SkinningDefs.TheInstance.GetData("autotimebackcolor");
			if (autotimebackcolor != "")
			{
				AutoTimeBackColor =  SkinningDefs.TheInstance.GetColorData("autotimebackcolor");
			}
			
			string autotimetextcolor =  SkinningDefs.TheInstance.GetData("autotimetextcolor");
			if (autotimetextcolor != "")
			{
				AutoTimeTextColor =  SkinningDefs.TheInstance.GetColorData("autotimetextcolor");
			}

			//Is there an overriding Title Foreground colour
			string racetitlecolour =  SkinningDefs.TheInstance.GetData("race_paneltitleforecolor");
			if (racetitlecolour != "")
			{
				MyTitleForeColor =  SkinningDefs.TheInstance.GetColorData("race_paneltitleforecolor");
			}

			//all transition panel 
			MyOperationsBackColor = OperationsBackColor;
			MyGroupPanelBackColor = GroupPanelBackColor;
			string fontname =  SkinningDefs.TheInstance.GetData("fontname");
			if (auto_translate)
			{
				fontname = TextTranslator.TheInstance.GetTranslateFont(fontname);
			}
			MyDefaultSkinFontNormal10 = ConstantSizeFont.NewFont(fontname,10);
			MyDefaultSkinFontBold10 = ConstantSizeFont.NewFont(fontname,10,FontStyle.Bold);
			MyDefaultSkinFontBold12 = ConstantSizeFont.NewFont(fontname,12,FontStyle.Bold);
			MyDefaultSkinFontBold24 = ConstantSizeFont.NewFont(fontname,24,FontStyle.Bold);

			focusJumper = new FocusJumper();

			GotFocus += InstallBusinessServiceControl_IBM_CLD_GotFocus;

			//connect up handles
			_Network = model;
			_prjmanager = prjmanager;
			_mainPanel = mainPanel;
			projectIncomingRequestQueueHandle = _Network.GetNamedNode("ProjectsIncomingRequests");

			//Determine the training Mode and hence the Background Image
			MyIsTrainingMode = IsTrainingMode;
			if (MyIsTrainingMode) 
			{
				BackgroundImage = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location +
					"\\images\\panels\\race_panel_back_training.png");
			}
			else
			{
				BackgroundImage = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location +
					"\\images\\panels\\race_panel_back_normal.png");
			}

			//Start building this control
			buttonArray = new ArrayList();
			BackColor = OperationsBackColor;
			Resize += Panel_Resize;

			PanelTitleLabel = new Label();
			PanelTitleLabel.Text = install_title_name+" Business Service";
			PanelTitleLabel.Size = new Size(500,20);
			PanelTitleLabel.Location = new Point(10,10);
			PanelTitleLabel.Font = MyDefaultSkinFontBold12;
			PanelTitleLabel.BackColor = OperationsBackColor;
			PanelTitleLabel.ForeColor = MyTitleForeColor;
			Controls.Add(PanelTitleLabel);

			okButton = new ImageTextButton(0);
			okButton.ButtonFont = MyDefaultSkinFontBold10;
			okButton.SetVariants(filename_short);
			okButton.Size = new Size(80,20);
			okButton.Location = new Point(265,110);
			okButton.SetButtonText("OK",upColor,upColor,hoverColor,disabledColor);
			okButton.Click += okButton_Click;
			okButton.Visible = false;
			Controls.Add(okButton);

			focusJumper.Add(okButton);

			cancelButton = new ImageTextButton(0);
			cancelButton.ButtonFont = MyDefaultSkinFontBold10;
			cancelButton.SetVariants(filename_short);
			cancelButton.Size = new Size(80,20);
			cancelButton.Location = new Point(370,110);
			cancelButton.SetButtonText("Close",upColor,upColor,hoverColor,disabledColor);
			cancelButton.Click += cancelButton_Click;
			cancelButton.Visible = true;
			Controls.Add(cancelButton);

			focusJumper.Add(cancelButton);

			//Build the Choose Service Panel
			chooseServicePanel = new Panel();
			chooseServicePanel.Location = new Point(5,30);
			chooseServicePanel.Size = new Size(510,145);
			chooseServicePanel.BackColor = MyOperationsBackColor;
			//chooseServicePanel.BackColor = Color.MediumSeaGreen;
			BuildServicePanel();
			Controls.Add( chooseServicePanel );

			chooseHardwareLocationPanel = new Panel();
			chooseHardwareLocationPanel.Location = new Point(5, 30);
			chooseHardwareLocationPanel.Size = new Size(510, 145);
			chooseHardwareLocationPanel.BackColor = MyOperationsBackColor;
			//chooseHardwareLocationPanel.BackColor = Color.MediumBlue;
			Controls.Add(chooseHardwareLocationPanel);

			//Build the Choose Time, SLA  and Location Panel
			chooseTimePanel = new Panel();
			chooseTimePanel.Size = new Size(510, 115);
			chooseTimePanel.Location = new Point(5,45);
			chooseTimePanel.Visible = false;
			//chooseTimePanel.BorderStyle = BorderStyle.Fixed3D;
			chooseTimePanel.BackColor = OperationsBackColor;
			//chooseTimePanel.BackColor = Color.Violet;
			Controls.Add(chooseTimePanel);

			//BuildTimePanelControls();

		}

		protected override void Dispose(bool disposing)
		{
			if(disposing)
			{
				focusJumper.Dispose();

				foreach(Control c in disposeControls)
				{
					if(c!=null) c.Dispose();
				}
				disposeControls.Clear();
			}
			//
			base.Dispose (disposing);
		}

		void BuildTimePanelControls(bool AppOnly)
		{
			//Are we shifting controls right for the time buttons 
			int shift_right = 0;
			string AllowedChangeWindowsActions_str  = SkinningDefs.TheInstance.GetData("changewindowactions");
			if (AllowedChangeWindowsActions_str.ToLower()=="true")
			{
				shift_right = 20;
			}

			//Display the Service Name 
			ChosenServiceLabel = new Label();
			ChosenServiceLabel.Text = "";
			ChosenServiceLabel.Size = new Size(530-120,22);
			ChosenServiceLabel.Font = MyDefaultSkinFontBold10;
			ChosenServiceLabel.Location = new Point(30+shift_right+120,2);
			ChosenServiceLabel.BackColor = Color.Transparent;
			ChosenServiceLabel.ForeColor = MyTitleForeColor;
			ChosenServiceLabel.TextAlign = ContentAlignment.MiddleCenter;
			chooseTimePanel.Controls.Add(ChosenServiceLabel);

			timeLabel = new Label();
			timeLabel.Text = install_title_name+" On Min";
			timeLabel.Size = new Size(120,15+3);
			timeLabel.Location = new Point(160+shift_right,30);
			timeLabel.Font = MyDefaultSkinFontBold10;
			//timeLabel.BackColor = Color.CadetBlue;
			timeLabel.ForeColor = MyTitleForeColor;
			chooseTimePanel.Controls.Add(timeLabel);

			whenTextBox = new EntryBox();
			whenTextBox.DigitsOnly = true;
			whenTextBox.Font = MyDefaultSkinFontNormal10;
			whenTextBox.Size = new Size(110,25);
			whenTextBox.Text = GetDefaultInstallTime();
			whenTextBox.MaxLength = 2;
			whenTextBox.KeyUp += whenTextBox_KeyUp;
			whenTextBox.Location = new Point(160+shift_right,60-10+2);
			whenTextBox.TextAlign = HorizontalAlignment.Center;
			whenTextBox.GotFocus += whenTextBox_GotFocus;
			whenTextBox.LostFocus += whenTextBox_LostFocus;
			//whenTextBox.BackColor = Color.Coral;
			//whenTextBox.ForeColor = MyTitleForeColor;
			whenTextBox.ForeColor = Color.Black;
			chooseTimePanel.Controls.Add(whenTextBox);

			if (AppOnly == false)
			{
				locationLabel = new Label();
				locationLabel.Text = "App Location";
				locationLabel.Size = new Size(110, 15);
				locationLabel.Location = new Point(290 + shift_right, 30);
				locationLabel.Font = MyDefaultSkinFontBold10;
				//locationLabel.BackColor = Color.Plum;
				locationLabel.ForeColor = MyTitleForeColor;
				chooseTimePanel.Controls.Add(locationLabel);

				locationBox = new EntryBox();
				locationBox.TextAlign = HorizontalAlignment.Center;
				locationBox.Location = new Point(290 + shift_right, 60 - 10 + 2);
				locationBox.Font = MyDefaultSkinFontNormal10;
				locationBox.Size = new Size(110, 30);
				locationBox.numChars = 4;
				locationBox.DigitsOnly = false;
				//locationBox.BackColor = Color.PowderBlue;
				locationBox.KeyPress += locationBox_KeyPress;
				chooseTimePanel.Controls.Add(locationBox);
			}
			else
			{
				locationLabel = new Label();
				locationLabel.Text = "Service Priority";
				locationLabel.Size = new Size(110, 15);
				locationLabel.Location = new Point(290 + shift_right, 30);
				locationLabel.Font = MyDefaultSkinFontBold10;
				//locationLabel.BackColor = Color.Plum;
				locationLabel.ForeColor = MyTitleForeColor;
				chooseTimePanel.Controls.Add(locationLabel);

				//ServiceLevelPriority = new Label();
				//ServiceLevelPriority.Size = new Size(110, 30);
				//ServiceLevelPriority.Location = new Point(290 + shift_right, 60 - 10 + 2);
				//ServiceLevelPriority.Font = this.MyDefaultSkinFontBold10;
				////locationLabel.BackColor = Color.Plum;
				//ServiceLevelPriority.Text = CONVERT.ToStr(app_only_ServiceLevelPriority);
				//ServiceLevelPriority.ForeColor = MyTitleForeColor;
				//ServiceLevelPriority.BackColor = AutoTimeBackColor;
				//chooseTimePanel.Controls.Add(ServiceLevelPriority);

				ServiceLevelPriority = new EntryBox();
				ServiceLevelPriority.TextAlign = HorizontalAlignment.Center;
				ServiceLevelPriority.Location = new Point(290 + shift_right, 60 - 10 + 2);
				ServiceLevelPriority.Font = MyDefaultSkinFontNormal10;
				ServiceLevelPriority.Size = new Size(110, 30);
				//ServiceLevelPriority.numChars = 4;
				ServiceLevelPriority.DigitsOnly = false;
				ServiceLevelPriority.Enabled = false;
				//locationBox.BackColor = Color.PowderBlue;
				chooseTimePanel.Controls.Add(ServiceLevelPriority);

				//ServiceLevelSelector = new ComboBox();
				//ServiceLevelSelector.DropDownStyle = ComboBoxStyle.DropDownList;
				//ServiceLevelSelector.DropDownStyle = ComboBoxStyle.DropDownList;
				//ServiceLevelSelector.Items.Add("High");
				//ServiceLevelSelector.Items.Add("Medium");
				//ServiceLevelSelector.Items.Add("Low");
				//ServiceLevelSelector.Location = new Point(290 + shift_right, 60 - 10 + 2);
				//ServiceLevelSelector.Size = new Size(110, 30);
				//chooseTimePanel.Controls.Add(ServiceLevelSelector);
			}

			SLA_TimeLabel = new Label();
			SLA_TimeLabel.Text = "MTRS (mins)";
			SLA_TimeLabel.Size = new Size(115,15);
			SLA_TimeLabel.Font = MyDefaultSkinFontBold10;
			SLA_TimeLabel.Location = new Point(390 + 30 + shift_right,30);
			//SLA_TimeLabel.BackColor = Color.AliceBlue;
			SLA_TimeLabel.ForeColor = MyTitleForeColor;
			chooseTimePanel.Controls.Add(SLA_TimeLabel);

			sla_TimeBox = new EntryBox();
			sla_TimeBox.DigitsOnly = true;
			sla_TimeBox.TextAlign = HorizontalAlignment.Center;
			sla_TimeBox.Location = new Point(390 + 30 + shift_right, 60 - 10 + 2);
			sla_TimeBox.Font = MyDefaultSkinFontNormal10;
			sla_TimeBox.Size = new Size(80,30);
			sla_TimeBox.numChars = 1;
			sla_TimeBox.NextControl = okButton;
			sla_TimeBox.GotFocus += sla_TimeBox_GotFocus;
			sla_TimeBox.Text = "6";
			sla_TimeBox.DigitsOnly = true;
			//sla_TimeBox.BackColor = Color.Aqua;
			chooseTimePanel.Controls.Add(sla_TimeBox);

			errorLabel = new Label ();
			errorLabel.Location = new Point (whenTextBox.Left, whenTextBox.Bottom + 10);
			errorLabel.Size = new Size (sla_TimeBox.Right + 50 - errorLabel.Left, 50);
			errorLabel.Font = MyDefaultSkinFontBold10;
			errorLabel.ForeColor = errorLabel_foreColor;
			chooseTimePanel.Controls.Add(errorLabel);
			errorLabel.BringToFront();

			if (AllowedChangeWindowsActions_str.ToLower() == "true")
			{
				//extract the change windows times 
				string AllowedChangeWindowTimes_str = SkinningDefs.TheInstance.GetData("changewindowtimes");
				string[] time_strs = AllowedChangeWindowTimes_str.Split(',');
				//Build the controls 

				int yoffset = 16;
				//General Manual Timing Button
				ImageTextButton ManualTimeButton = new ImageTextButton(0);
				ManualTimeButton.ButtonFont = MyDefaultSkinFontBold10;
				ManualTimeButton.SetVariants(filename_mid);
				ManualTimeButton.Size = new Size(145, 20);
				ManualTimeButton.Location = new Point(5, yoffset);
				ManualTimeButton.SetButtonText("Manual", upColor, upColor, hoverColor, disabledColor);
				ManualTimeButton.Click += ManualTimeButton_Click;
				chooseTimePanel.Controls.Add(ManualTimeButton);
				yoffset += ManualTimeButton.Height + 5;
				int count = 1;

				foreach (string st in time_strs)
				{
					if (count == 1)
					{
						AutoTimeLabel = new Label();
						AutoTimeLabel.Text = "";
						AutoTimeLabel.Font = MyDefaultSkinFontNormal10;
						AutoTimeLabel.TextAlign = ContentAlignment.MiddleCenter;
						AutoTimeLabel.ForeColor = AutoTimeTextColor;
						AutoTimeLabel.BackColor = AutoTimeBackColor;
						AutoTimeLabel.Size = new Size(90, 25);
						AutoTimeLabel.Location = new Point(160 + shift_right, 60 - 10 + 2);
						AutoTimeLabel.Visible = false;
						chooseTimePanel.Controls.Add(AutoTimeLabel);
					}

					string displayname = "Auto " + CONVERT.ToStr(count);
					ImageTextButton AutoTimeButton = new ImageTextButton(0);
					AutoTimeButton.ButtonFont = MyDefaultSkinFontBold10;
					AutoTimeButton.SetVariants(filename_mid);
					AutoTimeButton.Size = new Size(145, 20);
					AutoTimeButton.Location = new Point(5, yoffset);
					AutoTimeButton.Tag = CONVERT.ParseInt(st);
					AutoTimeButton.SetButtonText(displayname, upColor, upColor, hoverColor, disabledColor);
					AutoTimeButton.Click += AutoTimeButton_Click;
					AutoTimeButton.Visible = true;
					chooseTimePanel.Controls.Add(AutoTimeButton);
					yoffset += AutoTimeButton.Height + 5;
					count++;
				}
			}
		}


//		private void Build_UpgradeLocationList()
//		{
//			UpgradeLocationLookup.Clear();
//			//Need to build the active projects 
//			ArrayList existing_projects = currentProjectsHandle.getChildren();
//			foreach (Node activeProject in existing_projects)
//			{
//				string projectid = activeProject.GetAttribute("name");
//				string upgradeName = activeProject.GetAttribute("upgradename");
//				if (upgradeName != "")
//				{
//					Node nn = this._network.GetNamedNode(upgradeName);
//					if (nn != null)
//					{
//						string location = nn.GetAttribute("location");
//						if (location != "")
//						{
//							UpgradeLocationLookup.Add(projectid, location);
//						}
//					}
//				}
//			}
//		}		

		/// <summary>
		/// Building up the choice of possible installs
		/// </summary>
		protected virtual void BuildServicePanel()
		{
			Label lblAppsWithHardwareTitle = new Label();
			lblAppsWithHardwareTitle.Text = "Applications with Hardware";
			lblAppsWithHardwareTitle.Size = new Size(220, 18);
			lblAppsWithHardwareTitle.Location = new Point(10, 2);
			lblAppsWithHardwareTitle.Font = MyDefaultSkinFontBold10;
			lblAppsWithHardwareTitle.ForeColor = MyTitleForeColor;
			chooseServicePanel.Controls.Add(lblAppsWithHardwareTitle);

			Label lblAppsOnlyTitle = new Label();
			lblAppsOnlyTitle.Text = "Applications Only";
			lblAppsOnlyTitle.Size = new Size(220, 18);
			lblAppsOnlyTitle.Location = new Point(10 + servicebtn_xoffset + servicebtn_x_step, 2);
			lblAppsOnlyTitle.Font = MyDefaultSkinFontBold10;
			lblAppsOnlyTitle.ForeColor = MyTitleForeColor;
			chooseServicePanel.Controls.Add(lblAppsOnlyTitle);

			//using the project manager to get the possible installs
			//either use the project nodes in the network 
			//or use a passed through project manager object to get the information 
			//there is less duplication of coding for using a passed through object
			ArrayList ReadyProjects = _prjmanager.getInstallableProjects();
			ArrayList ReadyProjectNames_AppOnly = new ArrayList();
			Hashtable ReadyProjectsLookup_AppOnly = new Hashtable();
			ArrayList ReadyProjectNames_AppHW = new ArrayList();
			Hashtable ReadyProjectsLookup_AppHW = new Hashtable();

			UpgradeLocationLookup_AppOnly.Clear();
			UpgradeLocationLookup_AppHW.Clear();

			foreach(ProjectRunner pr in ReadyProjects)
			{
				string installname = pr.getInstallName();
				bool isAppHWDeployment = pr.isDeploy_AppWithHardware();

				if (isAppHWDeployment == false)
				{
					ReadyProjectsLookup_AppOnly.Add(installname, pr);
					ReadyProjectNames_AppOnly.Add(installname);

					Node tmpNode = null;
					//This is AppOnly so we need to extract the predefined location and priority
					tmpNode = _Network.GetNamedNode("IR" + pr.getSipIdentifier());
					if (tmpNode != null)
					{
						string location = tmpNode.GetAttribute("predefined_app_location");
						if (location != "")
						{
							UpgradeLocationLookup_AppOnly.Add(pr.getSipIdentifier(), location);
						}
					}
					//extract the priority level from the ServicePriorityLevels
					tmpNode = _Network.GetNamedNode("ServicePriorityLevels");
					if (tmpNode != null)
					{
						foreach (Node nn in tmpNode.getChildren())
						{ 
							string desc = nn.GetAttribute("desc");
							int level = nn.GetIntAttribute("level",1);
							if (desc.ToLower() == installname.ToLower())
							{
								UpgradePriorityLookup_AppOnly.Add(pr.getSipIdentifier(), level);
							}
						}
					}
				}
				else
				{
					ReadyProjectsLookup_AppHW.Add(installname, pr);
					ReadyProjectNames_AppHW.Add(installname);

					string upgradeName = pr.getUpgradeName();
					if (upgradeName != "")
					{
						Node nn = _Network.GetNamedNode(upgradeName);
						if (nn != null)
						{
							string location = nn.GetAttribute("location");
							if (location != "")
							{
								UpgradeLocationLookup_AppHW.Add(pr.getSipIdentifier(), location);
							}
						}
					}
				}
				//ReadyProjectsLookup.Add(installname,pr);
				//ReadyProjectNames.Add(installname);
				//Build the Upgrade Location information 
			}

			//Sort the names
			ReadyProjectNames_AppOnly.Sort();
			ReadyProjectNames_AppHW.Sort();

			//Now we have 2 columns of buttons for The 2 types of Installabel Business Services
			// App with Added HW (Used in Round 1 and Round 2) 
			// App Only (Used in Round 3) 
			numBtns_AppOnly = 0;
		  numBtns_AppHW = 0;
			foreach (string install_name in ReadyProjectNames_AppHW)
			{
				string translated_name = install_name;
				if (auto_translate)
				{
					translated_name = TextTranslator.TheInstance.Translate(translated_name);
				}
				ProjectRunner pr = (ProjectRunner)ReadyProjectsLookup_AppHW[install_name];
				if (pr.ProjectVisible)
				{
					AddInstallButton(translated_name, pr, button_Click, numBtns_AppHW, true);
					numBtns_AppHW++;
				}
			}

			foreach (string install_name in ReadyProjectNames_AppOnly)
			{
				string translated_name = install_name;
				if (auto_translate)
				{
					translated_name = TextTranslator.TheInstance.Translate(translated_name);
				}
				ProjectRunner pr = (ProjectRunner)ReadyProjectsLookup_AppOnly[install_name];
				if (pr.ProjectVisible)
				{
					AddInstallButton(translated_name, pr, button_Click, numBtns_AppOnly, false);
					numBtns_AppOnly++;
				}
			}


			//ReadyProjectNames.Sort();
			//// We can have 6 buttons wide before we have to go to a new line.
			//xoffset = 5;
			//yoffset = 5;
			//numOnRow = 0;
			//foreach(string install_name in ReadyProjectNames)
			//{
			//  string translated_name = install_name;
			//  if (auto_translate)
			//  {
			//    translated_name = TextTranslator.TheInstance.Translate(translated_name);
			//  }

			//  ProjectRunner pr = (ProjectRunner)ReadyProjectsLookup[install_name];
			//  if (pr.ProjectVisible)
			//  {
			//    AddInstallButton(translated_name, pr, new EventHandler (button_Click));
			//  }
			//}
		}

		/// <summary>
		/// Building the Panel to allow the selection of Hardware Location 
		/// </summary>
		protected void Build_HardwareSelectionPanel()
		{
			int btn_loc_y = servicebtn_yoffset;
			int btn_loc_x = servicebtn_xoffset;

			Label lblChooseHardwareLocationTitle = new Label();
			lblChooseHardwareLocationTitle.Text = "Select New Server Location";
			lblChooseHardwareLocationTitle.Size = new Size(220, 18);
			lblChooseHardwareLocationTitle.Location = new Point(10, 2);
			lblChooseHardwareLocationTitle.Font = MyDefaultSkinFontBold10;
			lblChooseHardwareLocationTitle.ForeColor = MyTitleForeColor;
			chooseHardwareLocationPanel.Controls.Add(lblChooseHardwareLocationTitle);

			install_server_locations_node = _Network.GetNamedNode("NewInstallData");
			if (install_server_locations_node != null)
			{
				bool btn_active = false;
				string btn_title = "";
				int pos_index = 0;

				foreach (Node locNode in install_server_locations_node.getChildren())
				{
					string server_location = locNode.GetAttribute("new_server_location");
					string app_location = locNode.GetAttribute("app_location");
					string server_location_status = locNode.GetAttribute("status");
					btn_active = false;
					btn_loc_y = servicebtn_yoffset + pos_index * servicebtn_y_step;
					btn_loc_x = servicebtn_xoffset;

					bool allowed = true;
					Node possible_app_node = _Network.GetNamedNode(app_location);
					if (possible_app_node == null)
					{
						allowed = false;
					}
					if (allowed)
					{
						btn_active = true;
						btn_title = server_location;
						ImageTextButton button = new ImageTextButton(0);
						button.ButtonFont = MyDefaultSkinFontBold10;
						button.SetVariants(filename_huge);
						button.Size = new Size(220, 20);
						button.Location = new Point(btn_loc_x, btn_loc_y);
						button.Tag = locNode;
						button.SetButtonText(btn_title, upColor, upColor, hoverColor, disabledColor);
						button.Click += server_location_button_Click;
						button.Visible = true;
						button.Enabled = btn_active;
						disposeControls.Add(button);
						chooseHardwareLocationPanel.Controls.Add(button);
						buttonArray.Add(button);
						focusJumper.Add(button);

						pos_index++;
					}
				}
			}
		}

		void server_location_button_Click(object sender, EventArgs e)
		{
			ImageTextButton b1 = (ImageTextButton)sender;
			Node locNode = (Node)b1.Tag;

			chosen_install_data_node = locNode;

			hardware_system_selected = true;

			locationLabel.Text = "Server Location";
			locationBox.Text = locNode.GetAttribute("new_server_location");
			locationBox.Enabled = false;

			chooseHardwareLocationPanel.Visible = false;
			chooseServicePanel.Visible = false;
			chooseTimePanel.Visible = true;
			okButton.Visible = true;
			cancelButton.SetButtonText("Cancel");
			whenTextBox.Focus();
			whenTextBox.Text = GetDefaultInstallTime();
		}

		protected ImageTextButton AddInstallButton (string install_name, object tag, EventHandler handler, int pos_index, bool firstcol)
		{
			int btn_loc_y = servicebtn_yoffset + pos_index * servicebtn_y_step;
			int btn_loc_x = servicebtn_xoffset;
			if (firstcol == false)
			{
				btn_loc_x = servicebtn_xoffset + servicebtn_x_step;
			}

			ImageTextButton button = new ImageTextButton(0);
			button.ButtonFont = MyDefaultSkinFontBold10;
			button.SetVariants(filename_huge);
			button.Size = new Size(220,20);
			button.Location = new Point(btn_loc_x, btn_loc_y);
			button.Tag = tag;
			button.SetButtonText(install_name,upColor,upColor,hoverColor,disabledColor);
			button.Click += handler;
			button.Visible = true;
			disposeControls.Add(button);
			chooseServicePanel.Controls.Add(button);
			buttonArray.Add(button);
			focusJumper.Add(button);
			return button;
		}

		void Panel_Resize(object sender, EventArgs e)
		{
			if (okButton != null)
			{
				okButton.Location = new Point(Width - 200, Height-30);
			}
			if (cancelButton != null)
			{
				cancelButton.Location = new Point(Width - 100, Height-30);
			}
			if (chooseServicePanel != null)
			{
				chooseServicePanel.Width = Width-10;
			}
			if (chooseTimePanel != null)
			{
				chooseTimePanel.Width = Width-10;
			}
		}

		/// <summary>
		/// Setting the permitted limit for the requested action time
		/// </summary>
		/// <param name="maxminutes"></param>
		public void SetMaxMins(int maxminutes)
		{
			maxmins = maxminutes;
		}

		void AutoTimeButton_Click(object sender, EventArgs e)
		{
			ImageTextButton b1 = (ImageTextButton) sender;
			int time_fullsecs =(int) b1.Tag;

			autoTimeSecs = time_fullsecs;

			int time_mins = autoTimeSecs / 60;
			int time_secs = autoTimeSecs % 60;
			string timestr = CONVERT.ToStr(time_mins)+":";
			if (time_secs<10)
			{
				timestr += "0";
			}
			timestr += CONVERT.ToStr(time_secs);
			
			if (AutoTimeLabel != null)
			{
				AutoTimeLabel.Text = timestr;
				AutoTimeLabel.Visible = true;
			}
			whenTextBox.Visible = false;

			HideError();
		}

		void ManualTimeButton_Click(object sender, EventArgs e)
		{
			autoTimeSecs = 0;
			if (AutoTimeLabel != null)
			{
				AutoTimeLabel.Visible = false;
			}
			whenTextBox.Visible = true;
			whenTextBox.Text = GetDefaultInstallTime();
			whenTextBox.Focus();

			HideError();
		}

		void button_Click(object sender, EventArgs e)
		{
			
			ImageTextButton button = (ImageTextButton) sender;

			servicePicked = (ProjectRunner)button.Tag;
			//ChosenServiceLabel.Text = button.GetButtonText();
			okButton.Enabled = true;

			string sip_identifier = servicePicked.getSipIdentifier();
			int sla_value = SLAManager.get_SLA(_Network,sip_identifier);
			sla_value = sla_value / 60; //convert from internally held secs to display mins
			//sla_TimeBox.Text = CONVERT.ToStr(sla_value);


			string fixlocation = string.Empty;
			string fixzone = string.Empty;
			servicePicked.getFixedInformation(out fixlocation, out fixzone);
			requireHWSelection = servicePicked.isDeploy_AppWithHardware();

			if (requireHWSelection)
			{
				BuildTimePanelControls(false);
			}
			else
			{
				BuildTimePanelControls(true);
			}

			if(fixlocation != string.Empty)
			{
				sla_TimeBox.PrevControl = whenTextBox;
				whenTextBox.NextControl = sla_TimeBox;
				locationBox.Text = fixlocation;
				locationBox.Enabled = false;
			}
			else
			{
				//handle the upgrade location 
				Boolean HandledbyUpgradeLocation = false;
				if ((UpgradeLocationLookup_AppOnly.ContainsKey(sip_identifier))
					|(UpgradeLocationLookup_AppHW.ContainsKey(sip_identifier)))
				{
					string upgradelocation = "";
					if (UpgradeLocationLookup_AppOnly.ContainsKey(sip_identifier))
					{
						upgradelocation = (string)UpgradeLocationLookup_AppOnly[sip_identifier];
					}
					if (UpgradeLocationLookup_AppHW.ContainsKey(sip_identifier))
					{
						upgradelocation = (string)UpgradeLocationLookup_AppHW[sip_identifier];
					}
					//string upgradelocation = (string)UpgradeLocationLookup[sip_identifier];
					if (upgradelocation != "")
					{
						HandledbyUpgradeLocation = true;
						sla_TimeBox.PrevControl = whenTextBox;
						whenTextBox.NextControl = sla_TimeBox;
						if (locationBox != null)
						{
							locationBox.Text = upgradelocation;
							locationBox.Enabled = false;
						}
					}
				}
				if (HandledbyUpgradeLocation == false)
				{
					sla_TimeBox.PrevControl = locationBox;
					whenTextBox.NextControl = locationBox;
					if (locationBox != null)
					{
						locationBox.Text = "";
						locationBox.Enabled = true;
					}
				}
			}

			if (requireHWSelection)
			{
				//We need to select the hardware location
				//Build the new Panel to allow selection of the Install 
				Build_HardwareSelectionPanel();
				chooseHardwareLocationPanel.Visible = true;
				chooseServicePanel.Visible = false;
				chooseTimePanel.Visible = false;
				okButton.Visible = false;
				cancelButton.SetButtonText("Cancel");
			}
			else
			{
				//Go direct to the end screen
				PanelTitleLabel.Text += " > " + button.Text;
				chooseHardwareLocationPanel.Visible = false;
				chooseServicePanel.Visible = false;
				chooseTimePanel.Visible = true;
				okButton.Visible = true;
				cancelButton.SetButtonText("Cancel");
				whenTextBox.Focus();
				whenTextBox.Text = GetDefaultInstallTime();
			}
			ChosenServiceLabel.Text = button.GetButtonText();
			sla_TimeBox.Text = CONVERT.ToStr(sla_value);

			if (requireHWSelection == false)
			{
				ServiceLevelPriority.Text = "1";
				if (UpgradePriorityLookup_AppOnly.ContainsKey(sip_identifier))
				{
					int level = (int) UpgradePriorityLookup_AppOnly[sip_identifier];
					ServiceLevelPriority.Text = CONVERT.ToStr(level);
				}
			}
		}

		void cancelButton_Click(object sender, EventArgs e)
		{
			_mainPanel.DisposeEntryPanel();
		}

		protected virtual bool ApplyIfValid(int timeVal, Node server)
		{
			return true;
		}

		int getCurrentSecond()
		{
			int currentSecond = 0;
			if (_Network != null)
			{
				Node tn = _Network.GetNamedNode("CurrentTime");
				if (tn != null)
				{
					currentSecond = tn.GetIntAttribute("seconds",0);
				}
			}
			return currentSecond;
		}

		bool StandardErrorChecks(out int requested_sla_time)
		{
			bool success = true;
			requested_sla_time = 6;
			
			//===================================================================
			//==Check over the Provided Information (check for obvious probelms)
			//===================================================================
			if (locationBox.Text == string.Empty)
			{
				ShowError("Please provide an install location.");
				success = false;
			}
			else
			{
				//We should check that the location is a valid one to help facilitator 
				//Not sure about best method Getting node with location = "XXXX" from tree seems costly
			}

			if ((sla_TimeBox.Text == string.Empty) & (success))
			{
				ShowError("No MTRS Value Provided");
				success = false;
			}
			else
			{
				requested_sla_time = CONVERT.ParseInt(sla_TimeBox.Text);
				if ((requested_sla_time < 1) | ((requested_sla_time > 9)))
				{
					ShowError("MTRS value must be between 1 and 9 minutes.");
					success = false;
				}
			}

			//Only need to check the time if we are not using AutoTime 
			if ((autoTimeSecs == 0) & (success))
			{
				if (whenTextBox.Text == string.Empty)
				{
					ShowError("Please provide a valid " + install_title_name.ToLower() + " time.");
					success = false;
				}
				else
				{
					if (whenTextBox.Text != "Now")
					{
						int requestedmin = 0;

						try
						{
							requestedmin = CONVERT.ParseInt(whenTextBox.Text);
							if (requestedmin > maxmins)
							{
								ShowError("Specified time is outwith the round. Please enter a valid time.");
								success = false;
							}
							else
							{
								if (requestedmin > ((maxmins - (SkinningDefs.TheInstance.GetIntData("ops_install_time", 120) / 60))))
								{
									ShowError("Not enough time to complete operation.");
									success = false;
								}
							}
						}
						catch (Exception)
						{
							ShowError("Invalid Time requested");
							success = false;
						}
					}
					else
					{
						int currentGameSecond = getCurrentSecond();
						if (currentGameSecond > ((maxmins * 60) - SkinningDefs.TheInstance.GetIntData("ops_install_time", 120)))
						{
							ShowError("Not enough time to complete operation.");
							success = false;
						}
					}
				}
			}
			return success;
		}

		bool AppOnlyErrorChecks(out int requested_sla_time)
		{
			bool success = true;
			requested_sla_time = 6;

			//===================================================================
			//==Check over the Provided Information (check for obvious probelms)
			//===================================================================
			//this.ServiceLevelPriority is READ ONLY

			if ((sla_TimeBox.Text == string.Empty) & (success))
			{
				ShowError("No MTRS Value Provided");
				success = false;
			}
			else
			{
				requested_sla_time = CONVERT.ParseInt(sla_TimeBox.Text);
				if ((requested_sla_time < 1) | ((requested_sla_time > 9)))
				{
					ShowError("MTRS value must be between 1 and 9 minutes.");
					success = false;
				}
			}

			//Only need to check the time if we are not using AutoTime 
			if ((autoTimeSecs == 0) & (success))
			{
				if (whenTextBox.Text == string.Empty)
				{
					ShowError("Please provide a valid " + install_title_name.ToLower() + " time.");
					success = false;
				}
				else
				{
					if (whenTextBox.Text != "Now")
					{
						int requestedmin = 0;

						try
						{
							requestedmin = CONVERT.ParseInt(whenTextBox.Text);
							if (requestedmin > maxmins)
							{
								ShowError("Specified time is outwith the round. Please enter a valid time.");
								success = false;
							}
							else
							{
								if (requestedmin > ((maxmins - (SkinningDefs.TheInstance.GetIntData("ops_install_time", 120) / 60))))
								{
									ShowError("Not enough time to complete operation.");
									success = false;
								}
							}
						}
						catch (Exception)
						{
							ShowError("Invalid Time requested");
							success = false;
						}
					}
					else
					{
						int currentGameSecond = getCurrentSecond();
						if (currentGameSecond > ((maxmins * 60) - SkinningDefs.TheInstance.GetIntData("ops_install_time", 120)))
						{
							ShowError("Not enough time to complete operation.");
							success = false;
						}
					}
				}
			}
			return success;
		}

		bool determineAppOnlyServerName(string service_level, out string server_name)
		{
			bool success = false;
			server_name = "";

			ArrayList al = _Network.GetNodesWithAttributeValue("virtual_group", service_level);
			if (al.Count > 0)
			{
				Node server_node = (Node)al[0];
				server_name = server_node.GetAttribute("name");
				success = true;
			}
			else
			{
				success = false;
			}
			return success;
		}

		void okButton_Click(object sender, EventArgs e)
		{
			Boolean proceed = true;
			string selected_sip = string.Empty;
			int requested_sla_time = 6;
			string msgbox_title = install_title_name+" Business Service Error";
			string predefined_location = "";

			HideError();

			//Redesign for the New IBM Cloud 
			if (requireHWSelection)
			{
				//we are defining the app by where the server goes 
				proceed = StandardErrorChecks(out requested_sla_time);
			}
			else 
			{ 
				//we are defining the app by it's priority
				proceed = AppOnlyErrorChecks(out requested_sla_time);
				if (proceed)
				{
					app_only_ServiceLevelPriority = CONVERT.ParseIntSafe(ServiceLevelPriority.Text,1);
				}
			}

			if (proceed)
			{
				if (servicePicked != null)
				{
					selected_sip = servicePicked.getSipIdentifier();
				}
				else
				{
					ShowError("No Service Picked");
					proceed = false;
				}
			}

			if (requireHWSelection==false)
			{
				//Need to define which predefined slot
				Node install_record_node = _Network.GetNamedNode("IR"+selected_sip);
				if (install_record_node != null)
				{
					predefined_location = install_record_node.GetAttribute("predefined_app_location");
				}
			}

			//===================================================================
			//==Do the work====================================================== 
			//===================================================================
			if (proceed)
			{
				int whenInMin = 0;
				int timeToFire =0;

				if (autoTimeSecs == 0)
				{
					if (whenTextBox.Text != "Now")
					{
						whenInMin = CONVERT.ParseInt(whenTextBox.Text);
						//check if the time has passed 
						Node currentTimeNode = _Network.GetNamedNode("CurrentTime");
						int current_seconds = currentTimeNode.GetIntAttribute("seconds",0);

						timeToFire = (whenInMin*60) - current_seconds;
						//if it's passed ,just fire now 
						if (timeToFire<0)
						{
							timeToFire=0;
						}
						else
						{
							timeToFire=whenInMin*60;
						}
					}
				}
				else
				{
					Node currentTimeNode = _Network.GetNamedNode("CurrentTime");
					int current_seconds = currentTimeNode.GetIntAttribute("seconds",0);

					timeToFire = (autoTimeSecs) - current_seconds;
					//if it's passed ,just fire now 
					if (timeToFire<0)
					{
						timeToFire=0;
					}
					else
					{
						timeToFire=autoTimeSecs;
					}
				}
					
				ArrayList al = new ArrayList();
				al.Add( new AttributeValuePair("projectid", selected_sip) );
				al.Add( new AttributeValuePair("installwhen", timeToFire.ToString()) );
				al.Add( new AttributeValuePair("sla", CONVERT.ToStr(requested_sla_time)) );
				al.Add( new AttributeValuePair("phase", "operations") );

				if (hardware_system_selected)
				{
					al.Add(new AttributeValuePair("location", locationBox.Text));
					al.Add(new AttributeValuePair("type", "install_apphw"));

					string install_data_name = chosen_install_data_node.GetAttribute("name");
					al.Add(new AttributeValuePair("install_data_node", install_data_name));

					//install_server_locations_node.SetAttribute("status", "pending");
					//install_server_locations_node.SetAttribute("msg", ChosenServiceLabel.Text);
					//install_server_locations_node.SetAttribute("booked_by", "selected_sip");
				}
				else
				{
					//need to define location as the app location 
					//need to define the name of the server that matches the service level 
					al.Add(new AttributeValuePair("location", predefined_location));
					al.Add(new AttributeValuePair("type", "install_apponly"));
					al.Add(new AttributeValuePair("install_data_server", "Metis"));
					al.Add(new AttributeValuePair("required_priority_level", CONVERT.ToStr(app_only_ServiceLevelPriority)));
				}
				//
				Node incident = new Node(projectIncomingRequestQueueHandle, "install","", al);

				_mainPanel.DisposeEntryPanel();
			}
		}

		void whenTextBox_GotFocus(object sender, EventArgs e)
		{
			//dayTextBox.SelectAll();
			if(whenTextBox.Text == "Now")
			{
				whenTextBox.SelectAll();
				//whenTextBox.Text = "";
			}
		}

		void whenTextBox_LostFocus(object sender, EventArgs e)
		{
			try
			{
				if(whenTextBox.Text == "")
				{
					whenTextBox.Text = GetDefaultInstallTime();
				}
			}
			catch
			{
				whenTextBox.Text = "Now";
			}
		}

		void sla_TimeBox_GotFocus(object sender, EventArgs e)
		{
			sla_TimeBox.SelectAll();
		}

		void whenTextBox_KeyUp(object sender, KeyEventArgs e)
		{
			if(whenTextBox.Text.Length == 2)
			{
				whenTextBox.NextControl.Focus();
			}
		}

		void InstallBusinessServiceControl_IBM_CLD_GotFocus(object sender, EventArgs e)
		{
			foreach(Control c in Controls)
			{
				ImageTextButton itb = c as ImageTextButton;
				if(itb != null)
				{
					if(itb.Visible && itb.Enabled)
					{
						string text = itb.GetButtonText();
						itb.Focus();
						return;
					}
				}
			}
		}

		string GetDefaultInstallTime ()
		{
			if ((servicePicked != null) && servicePicked.InstallScheduled)
			{
				return (servicePicked.InstallDueTime / 60).ToString();
			}

			return "Now";
		}

		void locationBox_KeyPress(object sender, KeyPressEventArgs e)
		{
			if ((e.KeyChar != 8) && (locationBox.Text.Length == 3))
			{
				sla_TimeBox.Focus();
			}
		}

		void ShowError (string error)
		{
			errorLabel.Text = error;
			errorLabel.Show();
		}

		void HideError ()
		{
			errorLabel.Text = "";
			errorLabel.Hide();
		}
	}
}