using System;
using System.Collections;
using System.Collections.Generic;
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
	/// Summary description for UpgradeMemDiskControl.
	/// </summary>
	public class InstallBusinessServiceControl : FlickerFreePanel
	{
		protected IDataEntryControlHolder _mainPanel;
		protected NodeTree _Network;
		protected ProjectManager _prjmanager;
		protected Node projectIncomingRequestQueueHandle;

		protected ImageTextButton okButton;
		protected ImageTextButton cancelButton;

		protected ArrayList buttonArray;

		protected Panel chooseUpgradePanel;
		protected Panel chooseServicePanel;
		protected Panel chooseTimePanel;

		protected Label timeLabel;
		protected Label locationLabel;
		protected Label PanelTitleLabel;
		protected Label ChosenServiceLabel;
		protected Label SLA_TimeLabel;
		protected Label ChosenServiceTitleLabel;
		protected Label AutoTimeLabel= null;

		protected Panel whenEntryPanel;

		protected EntryBox whenTextBox;

		protected EntryBox locationBox;
		protected EntryBox sla_TimeBox;

		protected ArrayList disposeControls = new ArrayList();

		protected ProjectRunner servicePicked = null;
		protected int maxmins = 24;
		protected int autoTimeSecs = 0; 
		protected Hashtable UpgradeLocationLookup = new Hashtable();
		protected Color AutoTimeBackColor = Color.Silver;
		protected Color AutoTimeTextColor = Color.Black;

		protected string filename_huge = "\\images\\buttons\\blank_huge.png";
		protected string filename_mid = "\\images\\buttons\\blank_med.png";
		protected string filename_short = "\\images\\buttons\\blank_small.png";

		protected Boolean MyIsTrainingMode = false;

		protected FocusJumper focusJumper;

        protected string install_title_name = SkinningDefs.TheInstance.GetData("Deploy", "Install");
		protected string install_name = SkinningDefs.TheInstance.GetData("Deploy", "Install");

		//skin stuff
		Color MyGroupPanelBackColor;

		Color MyOperationsBackColor;
		Color TitleForeColor = Color.Black;
		Color LabelForeColor = Color.Black;
        protected Font MyDefaultSkinFontNormal10 = null;
        protected Font MyDefaultSkinFontNormal11 = null;
		protected Font MyDefaultSkinFontBold10 = null;
		protected Font MyDefaultSkinFontBold12 = null;
		protected Font MyDefaultSkinFontBold24 = null;

		protected int xoffset;
		protected int yoffset;
		protected int numOnRow;
		protected bool auto_translate = true;

		protected Label errorLabel;
		public Color errorLabel_foreColor = Color.Red;
		protected bool showMTRS = true;
		protected int current_round = 1;

		ImageTextButton ManualTimeButton;
		List<ImageTextButton> AutoTimeButtons = new List<ImageTextButton> ();

		public InstallBusinessServiceControl(IDataEntryControlHolder mainPanel, NodeTree model, int round,
			ProjectManager prjmanager, Boolean IsTrainingMode, Color OperationsBackColor, Color GroupPanelBackColor)
		{
			current_round = round;
			SetStyle(ControlStyles.Selectable, true);
            if(SkinningDefs.TheInstance.GetBoolData("esm_sim",false))
		    {
                install_title_name = SkinningDefs.TheInstance.GetData("esm_install_title", "Install");
                install_name = SkinningDefs.TheInstance.GetData("esm_install_title", "install");
		    }
            else
		    {
		        install_title_name = SkinningDefs.TheInstance.GetData("transition_install_title", "Install");
		        install_name = SkinningDefs.TheInstance.GetData("transition_install_name", "install");
		    }
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
				TitleForeColor =  SkinningDefs.TheInstance.GetColorData("race_paneltitleforecolor");
			}

            LabelForeColor = SkinningDefs.TheInstance.GetColorData("race_panellabelforecolor", TitleForeColor);

            //all transition panel 
            MyOperationsBackColor = OperationsBackColor;
			MyGroupPanelBackColor = GroupPanelBackColor;
			string fontname =  SkinningDefs.TheInstance.GetData("fontname");
			if (auto_translate)
			{
				fontname = TextTranslator.TheInstance.GetTranslateFont(fontname);
			}
            MyDefaultSkinFontNormal10 = ConstantSizeFont.NewFont(fontname, 10);
            MyDefaultSkinFontNormal11 = ConstantSizeFont.NewFont(fontname, 11);
			MyDefaultSkinFontBold10 = ConstantSizeFont.NewFont(fontname,10,FontStyle.Bold);
			MyDefaultSkinFontBold12 = ConstantSizeFont.NewFont(fontname,12,FontStyle.Bold);
			MyDefaultSkinFontBold24 = ConstantSizeFont.NewFont(fontname,24,FontStyle.Bold);

			focusJumper = new FocusJumper();

			GotFocus += InstallBusinessServiceControl_GotFocus;

			//connect up handles
			_Network = model;
			_prjmanager = prjmanager;
			_mainPanel = mainPanel;
			projectIncomingRequestQueueHandle = _Network.GetNamedNode("ProjectsIncomingRequests");

			//Determine the training Mode and hence the Background Image
			MyIsTrainingMode = IsTrainingMode;

			if (SkinningDefs.TheInstance.GetBoolData("popups_use_image_background", true))
			{
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
			}

			//Start building this control
			buttonArray = new ArrayList();
			BackColor = OperationsBackColor;
			Resize += Panel_Resize;

		    PanelTitleLabel = new Label
		    {
		        Text = install_title_name + " Business Service",
		        Location = new Point (0, 0),
		        Font = SkinningDefs.TheInstance.GetFont(SkinningDefs.TheInstance.GetFloatData("ops_popup_title_font_size", 12), 
		            SkinningDefs.TheInstance.GetBoolData("ops_title_use_bold_font", true) ? FontStyle.Bold : FontStyle.Regular),
		        BackColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("popup_title_background_colour",
		            OperationsBackColor),
		        ForeColor = TitleForeColor,
                TextAlign = ContentAlignment.MiddleLeft
		    };
		    Controls.Add(PanelTitleLabel);

			Color upColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_text_colour", Color.Black);
			Color downColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_down_text_colour", Color.White);
			Color hoverColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_hover_text_colour", Color.Green);
			Color disabledColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_disabled_text_colour", Color.DarkGray);

			cancelButton = new StyledDynamicButtonCommon ("standard", "Close");
			cancelButton.Font = MyDefaultSkinFontBold10;
			cancelButton.Size = new Size(80,20);
			cancelButton.Location = SkinningDefs.TheInstance.GetPointData("ops_popup_cancel_button_position", 373, 113);
			cancelButton.Click += cancelButton_Click;
			Controls.Add(cancelButton);

			focusJumper.Add(cancelButton);

			okButton = new StyledDynamicButtonCommon("standard", "OK");
			okButton.Font = MyDefaultSkinFontBold10;
			okButton.Size = new Size(80, 20);
			okButton.Location = new Point(cancelButton.Left - 10 - okButton.Width, cancelButton.Top);
			okButton.Click += okButton_Click;
			okButton.Visible = false;
			Controls.Add(okButton);

			focusJumper.Add(okButton);

			//Build the Choose Service Panel
			chooseServicePanel = new Panel();
			chooseServicePanel.Location = new Point(10,45);
			chooseServicePanel.Size = new Size(500,130);
			chooseServicePanel.BackColor = MyOperationsBackColor;
			BuildServicePanel();
			Controls.Add( chooseServicePanel );

			//Build the Choose Time, SLA  and Location Panel
			chooseTimePanel = new Panel();
			chooseTimePanel.Size = new Size(500, 115);
			chooseTimePanel.Location = new Point(10,45);
			chooseTimePanel.Visible = false;
			//chooseTimePanel.BorderStyle = BorderStyle.Fixed3D;
			chooseTimePanel.BackColor = OperationsBackColor;
			Controls.Add(chooseTimePanel);

			//Are we shifthing controls right for the time buttons 
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
			ChosenServiceLabel.ForeColor = LabelForeColor;
			ChosenServiceLabel.TextAlign = ContentAlignment.MiddleCenter;
			chooseTimePanel.Controls.Add(ChosenServiceLabel);

			timeLabel = new Label();
			timeLabel.Text = GetInstallTimeLabelText();
			timeLabel.Size = new Size(120,20);
			timeLabel.Location = new Point(160+shift_right,25);
			timeLabel.Font = MyDefaultSkinFontNormal11;
			//timeLabel.BackColor = Color.CadetBlue;
			timeLabel.ForeColor = LabelForeColor;
			chooseTimePanel.Controls.Add(timeLabel);

			whenEntryPanel = CreateWhenEntryPanel(shift_right);
			chooseTimePanel.Controls.Add(whenEntryPanel);
			
			locationLabel = new Label();
			locationLabel.Text = "Location";
			locationLabel.Size = new Size(80,20);
			locationLabel.Location = new Point(290+shift_right,25);
			locationLabel.Font = MyDefaultSkinFontNormal11;
			//locationLabel.BackColor = Color.Plum;
			locationLabel.ForeColor = LabelForeColor;
			chooseTimePanel.Controls.Add(locationLabel);

			locationBox = new EntryBox();
			locationBox.TextAlign = HorizontalAlignment.Center;
			locationBox.Location = new Point(290+shift_right,60-10+2);
			locationBox.Font = MyDefaultSkinFontNormal10;
			locationBox.Size = new Size(80,30);
			locationBox.numChars = GetMaxLocationLength();
			locationBox.DigitsOnly = false;
			//locationBox.BackColor = Color.PowderBlue;
			locationBox.KeyPress += locationBox_KeyPress;
			locationBox.GotFocus += locationBox_GotFocus;
			chooseTimePanel.Controls.Add(locationBox);



			SLA_TimeLabel = new Label();
			SLA_TimeLabel.Text = "MTRS (mins)";
			SLA_TimeLabel.Size = new Size(115,15);
			SLA_TimeLabel.Font = MyDefaultSkinFontBold10;
			SLA_TimeLabel.Location = new Point(390+shift_right,30);
			//SLA_TimeLabel.BackColor = Color.AliceBlue;
			SLA_TimeLabel.ForeColor = LabelForeColor;
			SLA_TimeLabel.Visible = showMTRS;
			chooseTimePanel.Controls.Add(SLA_TimeLabel);

			sla_TimeBox = new EntryBox();
			sla_TimeBox.DigitsOnly = true;
			sla_TimeBox.TextAlign = HorizontalAlignment.Center;
			sla_TimeBox.Location = new Point(390+shift_right,60-10+2);
			sla_TimeBox.Font = MyDefaultSkinFontNormal10;
			sla_TimeBox.Size = new Size(80,30);
			sla_TimeBox.numChars = 1;
			sla_TimeBox.NextControl = okButton;
			sla_TimeBox.GotFocus += sla_TimeBox_GotFocus;
			sla_TimeBox.Text = "6";
			sla_TimeBox.DigitsOnly = true;
			sla_TimeBox.Visible = showMTRS;
			//sla_TimeBox.BackColor = Color.Aqua;
			chooseTimePanel.Controls.Add(sla_TimeBox);


			if (SkinningDefs.TheInstance.GetBoolData("use_impact_based_slas", false))
            {
                sla_TimeBox.Visible = false;
                SLA_TimeLabel.Visible = false;
            }

			errorLabel = new Label ();
			errorLabel.Location = new Point (whenEntryPanel.Left, whenEntryPanel.Bottom + 10);
			errorLabel.Size = new Size (sla_TimeBox.Right + 50 - errorLabel.Left, 50);
			errorLabel.Font = MyDefaultSkinFontBold10;
			errorLabel.ForeColor = errorLabel_foreColor;
			chooseTimePanel.Controls.Add(errorLabel);
			errorLabel.BringToFront();

			if (AllowedChangeWindowsActions_str.ToLower()=="true")
			{
				//extract the change windows times 
				string AllowedChangeWindowTimes_str  = SkinningDefs.TheInstance.GetData("changewindowtimes");
				string[] time_strs = AllowedChangeWindowTimes_str.Split(',');
				//Build the controls 

				int yoffset = 16;

				ManualTimeButton = new StyledDynamicButtonCommon("standard", "Manual");
				ManualTimeButton.Font = MyDefaultSkinFontBold10;
				ManualTimeButton.Size = new Size(145,20);
				ManualTimeButton.Location = new Point(5,yoffset);
				ManualTimeButton.Click += ManualTimeButton_Click;
				chooseTimePanel.Controls.Add(ManualTimeButton);
				yoffset += ManualTimeButton.Height + 5;
				int count = 1;

				foreach (string st in time_strs)
				{
					if (count==1)
					{
						AutoTimeLabel = new Label();
						AutoTimeLabel.Text = "";
						AutoTimeLabel.Font = MyDefaultSkinFontNormal10;
						AutoTimeLabel.TextAlign = ContentAlignment.MiddleCenter;
						AutoTimeLabel.ForeColor = AutoTimeTextColor;
						AutoTimeLabel.BackColor = AutoTimeBackColor;
						AutoTimeLabel.Size = new Size(90,25);
						AutoTimeLabel.Location = new Point(160+shift_right,60-10+2);
						AutoTimeLabel.Visible = false;
						chooseTimePanel.Controls.Add(AutoTimeLabel);
					}

					string displayname = "Auto "+CONVERT.ToStr(count);
					ImageTextButton AutoTimeButton = new StyledDynamicButtonCommon ("standard", displayname);
					AutoTimeButton.Font = MyDefaultSkinFontBold10;
					AutoTimeButton.Size = new Size(145,20);
					AutoTimeButton.Location = new Point(5,yoffset);
					AutoTimeButton.Tag = CONVERT.ParseInt(st);
					AutoTimeButton.Click += AutoTimeButton_Click;
					chooseTimePanel.Controls.Add(AutoTimeButton);
					yoffset += AutoTimeButton.Height + 5;
					count++;

					AutoTimeButtons.Add(AutoTimeButton);
				}
			}

			SelectTimeButton(ManualTimeButton);
		}

		int GetMaxLocationLength ()
		{
			int maxLength = 1;

			foreach (Node node in _Network.GetNodesWithAttribute("location").Keys)
			{
				maxLength = Math.Max(maxLength, node.GetAttribute("location").Length);
			}

			return maxLength;
		}

		protected virtual Panel CreateWhenEntryPanel (int rightShift)
		{
			Panel panel = new Panel ();
			panel.Location = new Point (160 + rightShift, 52);
			panel.Size = new Size (110, 25);

			whenTextBox = new EntryBox ();
			whenTextBox.DigitsOnly = true;
			whenTextBox.Font = MyDefaultSkinFontNormal10;
			whenTextBox.Size = panel.Size;
			whenTextBox.Text = GetDefaultInstallTime();
			whenTextBox.MaxLength = 2;
			whenTextBox.KeyUp += whenTextBox_KeyUp;
			whenTextBox.TextAlign = HorizontalAlignment.Center;
			whenTextBox.GotFocus += whenTextBox_GotFocus;
			whenTextBox.LostFocus += whenTextBox_LostFocus;
			whenTextBox.ForeColor = Color.Black;
			panel.Controls.Add(whenTextBox);

			return panel;
		}

		protected virtual string GetInstallTimeLabelText ()
		{
			return install_title_name + " On Min";
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

		public void Set_MTRS_Visibility(bool show)
		{
			showMTRS = show;
			//set the control visibility  
			SLA_TimeLabel.Visible = showMTRS;
			sla_TimeBox.Visible = showMTRS;
		}

		/// <summary>
		/// Building up the choice of possible installs
		/// </summary>
		protected virtual void BuildServicePanel()
		{
			Color upColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_text_colour", Color.Black);
			Color downColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_down_text_colour", Color.White);
			Color hoverColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_hover_text_colour", Color.Green);
			Color disabledColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_disabled_text_colour", Color.DarkGray);

			//using the project manager to get the possible installs
			//either use the project nodes in the network 
			//or use a passed through project manager object to get the information 
			//there is less duplication of coding for using a passed through object
			ArrayList ReadyProjects = _prjmanager.getInstallableProjects();
			ArrayList ReadyProjectNames = new ArrayList();
			Hashtable ReadyProjectsLookup = new Hashtable();

			UpgradeLocationLookup.Clear();

			foreach(ProjectRunner pr in ReadyProjects)
			{
				string installname = pr.getInstallName();
				ReadyProjectsLookup.Add(installname,pr);
				ReadyProjectNames.Add(installname);

				//Build the Upgrade Location information 
				string upgradeName = pr.getUpgradeName();
				if (upgradeName != "")
				{
					Node nn = _Network.GetNamedNode(upgradeName);
					if (nn != null)
					{
						string location = nn.GetAttribute("location");
						if (location != "")
						{
							UpgradeLocationLookup.Add(pr.getSipIdentifier(), location);
						}
					}
				}
			}
			
			ReadyProjectNames.Sort();
			// We can have 6 buttons wide before we have to go to a new line.
			xoffset = 5;
			yoffset = 5;
			numOnRow = 0;
			foreach(string install_name in ReadyProjectNames)
			{
				string translated_name = install_name;
				if (auto_translate)
				{
					translated_name = TextTranslator.TheInstance.Translate(translated_name);
				}

				ProjectRunner pr = (ProjectRunner)ReadyProjectsLookup[install_name];
				if (pr.ProjectVisible)
				{
					AddInstallButton(translated_name, pr, button_Click);
				}
			}
		}

		protected ImageTextButton AddInstallButton (string install_name, object tag, EventHandler handler)
		{
			Color upColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_text_colour", Color.Black);
			Color downColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_down_text_colour", Color.White);
			Color hoverColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_hover_text_colour", Color.Green);
			Color disabledColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_disabled_text_colour", Color.DarkGray);

			ImageTextButton button = new StyledDynamicButtonCommon ("standard", install_name);
			button.Font = MyDefaultSkinFontBold10;
			button.Size = SkinningDefs.TheInstance.GetSizeData("install_service_popup_button_size", new Size(220,20));
			button.Location = new Point(xoffset,yoffset);
			button.Tag = tag;
			button.Click += handler;
			disposeControls.Add(button);
			chooseServicePanel.Controls.Add(button);
			xoffset += button.Width+5;
			buttonArray.Add(button);

			focusJumper.Add(button);

			++numOnRow;
			if(numOnRow == 2)
			{
				numOnRow = 0;
				xoffset = 5;
				yoffset += 25;
			}

			return button;
		}

		void Panel_Resize(object sender, EventArgs e)
		{
			if (string.IsNullOrEmpty(SkinningDefs.TheInstance.GetData("ops_popup_cancel_button_position")))
			{
				if (okButton != null)
				{
					okButton.Location = new Point(Width - 200, Height - 30);
				}
				if (cancelButton != null)
				{
					int y = Height - cancelButton.Height - 5;
					if (okButton != null)
					{
						y = okButton.Top;
					}

					cancelButton.Location = new Point(Width - cancelButton.Width - 7, y);
				}
			}
			if (chooseServicePanel != null)
			{
				chooseServicePanel.Width = Width-(2 * chooseServicePanel.Left);
			}
			if (chooseTimePanel != null)
			{
				chooseTimePanel.Width = Width - (2 * chooseTimePanel.Left);
			}

		    if (SkinningDefs.TheInstance.GetBoolData("ops_popup_title_use_full_width", false))
		    {
			    PanelTitleLabel.Size = new Size (Width, 25);
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
			autoTimeSecs = (int) b1.Tag;

			ShowAutoTimeLabel(FormatAutoTime(autoTimeSecs));

			SelectTimeButton(sender);
		}

		void SelectTimeButton (object sender)
		{
			ManualTimeButton.Active = (sender == ManualTimeButton);

			foreach (var button in AutoTimeButtons)
			{
				button.Active = (button == sender);
			}
		}

		protected virtual string FormatAutoTime (int time)
		{
			int minutes = time / 60;
			int seconds = time % 60;

			return CONVERT.Format("{0}:{1:00}", minutes, seconds);
		}

		void ShowAutoTimeLabel (string time)
		{
			if (AutoTimeLabel != null)
			{
				AutoTimeLabel.Text = time;
				AutoTimeLabel.Visible = true;
			}
			whenEntryPanel.Visible = false;

			HideError();
		}

		void ManualTimeButton_Click(object sender, EventArgs e)
		{
			autoTimeSecs = 0;
			if (AutoTimeLabel != null)
			{
				AutoTimeLabel.Visible = false;
			}
			whenEntryPanel.Visible = true;
			whenTextBox.Text = GetDefaultInstallTime();
			whenTextBox.Focus();

			SelectTimeButton(sender);

			HideError();
		}

		protected void button_Click(object sender, EventArgs e)
		{
			handle_Service_Click(sender, e);
		}

		protected virtual void handle_Service_Click(object sender, EventArgs e)
		{
			ImageTextButton button = (ImageTextButton) sender;
			foreach(ImageTextButton b in buttonArray)
			{
				if(b == button)
				{
					ChosenServiceLabel.Text = button.GetButtonText();
					servicePicked = (ProjectRunner) b.Tag;
					ChosenServiceLabel.Hide();
					okButton.Enabled = true;

					string sip_identifier = servicePicked.getSipIdentifier();
					int sla_value = SLAManager.get_SLA(_Network,sip_identifier);
					sla_value = sla_value / 60; //convert from internally held secs to display mins
					sla_TimeBox.Text = CONVERT.ToStr(sla_value);

					string fixlocation = string.Empty;
					string fixzone = string.Empty;
					servicePicked.getFixedInformation(out fixlocation, out fixzone);
					
					if(fixlocation != string.Empty)
					{
						sla_TimeBox.PrevControl = whenTextBox;
						whenTextBox.NextControl = sla_TimeBox;
						locationBox.Text = fixlocation;
						locationBox.Enabled = false;
					}
					else
					{
						//handle the upgarde location 
						Boolean HandledbyUpgradeLocation = false;
						if (UpgradeLocationLookup.ContainsKey(sip_identifier))
						{
							string upgradelocation = (string)UpgradeLocationLookup[sip_identifier];
							if (upgradelocation != "")
							{
								HandledbyUpgradeLocation = true;
								sla_TimeBox.PrevControl = whenTextBox;
								whenTextBox.NextControl = sla_TimeBox;
								locationBox.Text = upgradelocation;
								locationBox.Enabled = false;
							}
						}
						if (HandledbyUpgradeLocation == false)
						{
							sla_TimeBox.PrevControl = locationBox;
							whenTextBox.NextControl = locationBox;
							locationBox.Text = "";
							locationBox.Enabled = true;
						}
					}

					PanelTitleLabel.Text += " > " + b.GetButtonText();
					chooseServicePanel.Visible = false;
					chooseTimePanel.Visible = true;
					okButton.Visible = true;
					cancelButton.SetButtonText("Cancel");
					whenTextBox.Focus();

					whenTextBox.Text = GetDefaultInstallTime();
					return;
				}
			}
			//okButton.Enabled = true;
		}

		void cancelButton_Click(object sender, EventArgs e)
		{
			_mainPanel.DisposeEntryPanel();
		}

		protected virtual bool ApplyIfValid(int timeVal, Node server)
		{
			return true;
		}

		protected int getCurrentSecond()
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

		void okButton_Click(object sender, EventArgs e)
		{
			handle_OK_Click(sender,e);
		}

		protected virtual void handle_OK_Click(object sender, EventArgs e)
		{
			Boolean proceed = true;
			string selected_sip = string.Empty;
			int requested_sla_time = 6;
			string msgbox_title = install_title_name+" Business Service Error";
			int currentGameTime = getCurrentSecond();

			HideError();

            string provideString = (install_title_name.ToLower().StartsWith("a") ||
                                    install_title_name.ToLower().StartsWith("e") ||
                                    install_title_name.ToLower().StartsWith("i") ||
                                    install_title_name.ToLower().StartsWith("o") ||
                                    install_title_name.ToLower().StartsWith("u")) ? "an " + install_title_name.ToLower() : "a " + install_title_name.ToLower();

			//===================================================================
			//==Check over the Provided Information (check for obvious probelms)
			//===================================================================
			if (locationBox.Text == string.Empty)
			{
				ShowError("Please provide " + provideString + " location.");
				proceed = false;
			}
			else
			{
				//We should check that the location is a valid one to help facilitator 
				//Not sure about best method Getting node with location = "XXXX" from tree seems costly
			}


			if ((sla_TimeBox.Text == string.Empty)&(proceed))
			{
				ShowError("No MTRS Value Provided");
				proceed = false;
			}
			else
			{
				//if we are not showing, perform no checks and just pass on what was placed there 
				requested_sla_time = CONVERT.ParseInt(sla_TimeBox.Text);
				if (showMTRS == true)
				{
					if ((requested_sla_time < 1) | ((requested_sla_time > 9)))
					{
						ShowError("MTRS value must be between 1 and 9 minutes.");
						proceed = false;
					}
				}
			}

			//Only need to check the time if we are not using AutoTime 
			if ((autoTimeSecs == 0)&(proceed))
			{
				if (whenTextBox.Text == string.Empty)
				{
					ShowError("Please provide a valid " + install_title_name.ToLower() + " time.");
					proceed = false;
				}
				else
				{
					if (whenTextBox.Text != "Now")
					{
						int requestedmin = 0;

						try
						{
							requestedmin = GetRequestedTimeOffset();
							if (requestedmin>maxmins)
							{
								ShowError("Specified time is outwith the round. Please enter a valid time.");
								proceed = false;
							}
							else
							{
								int time_taken_for_install = (SkinningDefs.TheInstance.GetIntData("ops_install_time", 120) / 60);
								int last_install_time = (maxmins - time_taken_for_install);

								if (requestedmin > (last_install_time))
								{
									ShowError("Not enough time to complete operation.");
									proceed = false;
								}
								else if (requestedmin * 60 < currentGameTime)
								{
									ShowError("Specified time is in the past. Please enter a valid time.");
									proceed = false;
								}

								if (proceed)
								{
									int currentGameSecond = getCurrentSecond();
									if ((requestedmin * 60) < currentGameSecond)
									{
										int timeLeft_secs = (maxmins * 60 - currentGameSecond);
										if (timeLeft_secs < (time_taken_for_install*60))
										{
											ShowError("Not enough time to complete operation.");
											proceed = false;
										}
									}
								}
							}
						}
						catch (Exception)
						{
							ShowError("Invalid Time requested");
							proceed = false;
						}
					}
					else
					{
						int currentGameSecond = getCurrentSecond();
						if (currentGameSecond > ((maxmins*60)-SkinningDefs.TheInstance.GetIntData("ops_install_time", 120)))
						{
							ShowError("Not enough time to complete operation.");
							proceed = false;
						}
					}
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
			//===================================================================
			//==Do the work 
			//===================================================================
			if (proceed)
			{
				int whenInMin = 0;
				int timeToFire =0;

				if (autoTimeSecs == 0)
				{
					if (whenTextBox.Text != "Now")
					{
						whenInMin = GetRequestedTimeOffset();
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

					if (autoTimeSecs < current_seconds)
					{
						timeToFire = 0;
					}
					else
					{
						timeToFire = autoTimeSecs;
					}
				}
					
				ArrayList al = new ArrayList();
				al.Add( new AttributeValuePair("projectid", selected_sip) );
				al.Add( new AttributeValuePair("installwhen", timeToFire.ToString()) );
				al.Add( new AttributeValuePair("sla", CONVERT.ToStr(requested_sla_time)) );
				al.Add( new AttributeValuePair("location", locationBox.Text) );
				al.Add( new AttributeValuePair("type", "Install") );
				al.Add( new AttributeValuePair("phase", "operations") );
				//
				//Node incident = new Node(projectIncomingRequestQueueHandle, "install","", al);


				if (currentGameTime > autoTimeSecs && autoTimeSecs != 0)
				{
					ShowError("Specified time is in the past. Please enter a valid time."); 
				}
				else
				{
					Node incident = new Node(projectIncomingRequestQueueHandle, "install", "", al);
					_mainPanel.DisposeEntryPanel();
				}
			}
		}

		protected virtual int GetRequestedTimeOffset ()
		{
			return CONVERT.ParseInt(whenTextBox.Text);
		}

		protected void whenTextBox_GotFocus(object sender, EventArgs e)
		{
			//dayTextBox.SelectAll();
			if(whenTextBox.Text == "Now")
			{
				whenTextBox.SelectAll();
				//whenTextBox.Text = "";
			}
			HideErrorIfRequired();
		}

		protected void whenTextBox_LostFocus(object sender, EventArgs e)
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
			HideErrorIfRequired();
		}

		protected virtual void whenTextBox_KeyUp(object sender, KeyEventArgs e)
		{
			if(whenTextBox.Text.Length == 2)
			{
				whenTextBox.NextControl.Focus();
			}
		}

		protected void InstallBusinessServiceControl_GotFocus(object sender, EventArgs e)
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

		protected virtual string GetDefaultInstallTime()
		{
			if ((servicePicked != null) && servicePicked.InstallScheduled)
			{
				return (servicePicked.InstallDueTime / 60).ToString();
			}

			return "Now";
		}

		protected void locationBox_GotFocus(object sender, EventArgs e)
		{
			HideErrorIfRequired();
		}

		protected void locationBox_KeyPress(object sender, KeyPressEventArgs e)
		{
			if ((e.KeyChar != 8) && (locationBox.Text.Length == 3))
			{
				sla_TimeBox.Focus();
			}
			HideErrorIfRequired();
		}

		protected void ShowError (string error)
		{
			errorLabel.Text = error;
			errorLabel.Show();
		}

		protected void HideError()
		{
			errorLabel.Text = "";
			errorLabel.Hide();
		}

		protected void HideErrorIfRequired()
		{
			if (errorLabel.Visible)
			{
				errorLabel.Text = "";
				errorLabel.Hide();
			}
		}


	}
}