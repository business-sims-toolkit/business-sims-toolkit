using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using LibCore;
using Network;

using CoreUtils;
using IncidentManagement;

namespace CommonGUI
{
	public interface IDataEntryControlHolder
	{
		void DisposeEntryPanel();

		void DisposeEntryPanel_indirect(int which);
		
		//This used to ask the control to open a second popup based on a passed choice
		void SwapToOtherPanel(int which_operation);

		IncidentApplier IncidentApplier { get; }
	}

	public interface IDataEntryControlHolderWithShowPanel : IDataEntryControlHolder
	{
		void ShowEntryPanel (Control panel);
	}

	/// <summary>
	/// Summary description for UpgradeAppControl.
	/// </summary>
	public class UpgradeAppControl : FlickerFreePanel
	{
		protected IDataEntryControlHolder _mainPanel;
		protected IncidentApplier _iApplier;
		protected NodeTree _Network;
		protected Node AppUpgradeQueueNode;
		protected Hashtable apps = new Hashtable();
		protected Hashtable AllowedApps = new Hashtable();
		protected ArrayList AllowedAppSortList = new ArrayList();
		protected Node CoolingSystemNode;
		protected Boolean CoolingSystemUpgraded = false;
		
		protected ImageTextButton okButton;
		protected ImageTextButton cancelButton;
		protected Panel chooseTimePanel;

		protected ArrayList buttonArray = new ArrayList();
		protected Hashtable PendingUpgradeByName = new Hashtable();
		
		protected string selectedUpgrade;

		protected Label title;
		protected Label timeLabel;
		protected Panel whenEntryPanel;
		protected EntryBox whenTextBox;
		protected Label errorlabel;
		protected Label AutoTimeLabel= null;


		protected bool UsingMinutes = false; //Control in Race Phase Using Minutes 
		protected int maxmins = 24;
		protected int autoTimeSecs = 0; 

		protected Boolean MyIsTrainingMode = false;

		protected FocusJumper focusJumper;
		
		protected Color upColor = Color.Black;
		protected Color downColor = Color.White;
		protected Color hoverColor = Color.Green;
		protected Color disabledColor = Color.DarkGray;

		protected Color MyGroupPanelBackColor;
		protected Color MyOperationsBackColor;
		protected Color MyTextLabelBackColor;

	    protected Color TitleForeColor = Color.Black;
	    protected Color LabelForeColor = Color.Black;
		protected Color AutoTimeBackColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("autotimebackcolor", Color.Silver);
		protected Color AutoTimeTextColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("autotimetextcolor", Color.Black);

		protected Font MyDefaultSkinFontNormal12 = null;
		protected Font MyDefaultSkinFontNormal11 = null;
		protected Font MyDefaultSkinFontNormal8 = null;
		protected Font MyDefaultSkinFontBold12 = null;
		protected Font MyDefaultSkinFontBold11 = null;
		protected Font MyDefaultSkinFontBold10 = null;
		protected Font MyDefaultSkinFontBold9 = null;
	    Font ciNameFont;

		protected string filename_long = "\\images\\buttons\\blank_big.png";
		protected string filename_mid = "\\images\\buttons\\blank_med.png";
		protected string filename_short = "\\images\\buttons\\blank_small.png";
		protected string filename_ok = "\\images\\buttons\\ok_blank_small.png";

		protected int bottomCornerOffset = 8;
		protected ImageTextButton ManualTimeButton = null;
		protected List<ImageTextButton> AutoTimeButtons = new List<ImageTextButton> ();
		protected bool playing = false;
		public bool ResizeAppPanels = false;
		public Color errorLabel_foreColor = Color.Red;

		bool onlyNodesTaggedAsAssets;

		bool specialUpgrade;

		public UpgradeAppControl(IDataEntryControlHolder mainPanel, IncidentApplier iApplier, NodeTree nt, 
			bool usingmins, Boolean IsTrainingMode, Color OperationsBackColor, Color GroupPanelBackColor):this(
			mainPanel, iApplier, nt, usingmins, IsTrainingMode, OperationsBackColor, GroupPanelBackColor, true)
		{
		}

		public UpgradeAppControl(IDataEntryControlHolder mainPanel, IncidentApplier iApplier, NodeTree nt, 
			bool usingmins, Boolean IsTrainingMode, Color OperationsBackColor, Color GroupPanelBackColor, bool _playing, bool onlyNodesTaggedAsAssets = false)
		{
			upColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_text_colour", Color.Black);
			downColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_down_text_colour", Color.White);
			hoverColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_hover_text_colour", Color.Green);
			disabledColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_disabled_text_colour", Color.DarkGray);
			playing = _playing;

			this.onlyNodesTaggedAsAssets = onlyNodesTaggedAsAssets;

			//Is there an overriding Title Foreground colour
			string racetitlecolour =  SkinningDefs.TheInstance.GetData("race_paneltitleforecolor");
			if (racetitlecolour != "")
			{
				TitleForeColor =  SkinningDefs.TheInstance.GetColorData("race_paneltitleforecolor");
			}

		    LabelForeColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("race_panellabelforecolor", TitleForeColor);

			string errmsg_overridecolor = SkinningDefs.TheInstance.GetData("race_errormsg_override_color");
			if (errmsg_overridecolor != "")
			{
				errorLabel_foreColor = SkinningDefs.TheInstance.GetColorData("race_errormsg_override_color");
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

			focusJumper = new FocusJumper();

			MyOperationsBackColor = OperationsBackColor;
			MyGroupPanelBackColor = GroupPanelBackColor;
			MyTextLabelBackColor = OperationsBackColor;

			//Check if we are wish the Text Labels with Transparent Backs (Used for the steel)
			if (SkinningDefs.TheInstance.GetIntData("race_panels_transparent_backs", 0) == 1)
			{
				MyTextLabelBackColor = Color.Transparent;
			}

			//We may wish to offset the ok and cancel buttons from the bottom right corner (standard is 5)
			bottomCornerOffset = SkinningDefs.TheInstance.GetIntData("race_panels_upgrade_app_corneroffset", 5);

			string fontname =  SkinningDefs.TheInstance.GetData("fontname");
			MyDefaultSkinFontNormal12 = ConstantSizeFont.NewFont(fontname,12);
			MyDefaultSkinFontNormal11 = ConstantSizeFont.NewFont(fontname,11);
			MyDefaultSkinFontNormal8 = ConstantSizeFont.NewFont(fontname,8);
			MyDefaultSkinFontBold12 = ConstantSizeFont.NewFont(fontname,12,FontStyle.Bold);
			MyDefaultSkinFontBold11 = ConstantSizeFont.NewFont(fontname,11,FontStyle.Bold);
		    MyDefaultSkinFontBold10 = ConstantSizeFont.NewFont(fontname, 10, FontStyle.Bold);
		    ciNameFont = ConstantSizeFont.NewFont(fontname, (float)SkinningDefs.TheInstance.GetDoubleData("upgrade_popup_ci_name_font_size", 10), FontStyle.Bold);
			MyDefaultSkinFontBold9 = ConstantSizeFont.NewFont(fontname, 9, FontStyle.Bold);

			UsingMinutes = usingmins;
			buttonArray = new ArrayList();
			_Network = nt;
			_iApplier = iApplier;
			_mainPanel = mainPanel;

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

			//connect up to Queue Node 
			AppUpgradeQueueNode = _Network.GetNamedNode("AppUpgradeQueue");
			CoolingSystemNode = _Network.GetNamedNode("CoolingSystem");
			if (CoolingSystemNode != null)
			{
				CoolingSystemUpgraded = CoolingSystemNode.GetBooleanAttribute("upgraded", false);
			}

			CompleteConstructPanels(usingmins);

			GotFocus += UpgradeAppControl_GotFocus;
			Resize += UpgradeAppControl_Resize;
		}

		
		/// <summary>
		/// This method handles the construction of the functional panels 
		/// Allows descedant classes to define thier own panels
		/// </summary>
		public virtual void CompleteConstructPanels(bool usingmins)
		{
			int xOffset = 10;
			int yOffset = SkinningDefs.TheInstance.GetIntData("upgrade_panel_item_y_start", 40);

			Width = 590;
			Height = 185;

			//Create the Title 
		    title = new Label
		    {
		        Font = SkinningDefs.TheInstance.GetFont(SkinningDefs.TheInstance.GetFloatData("ops_popup_title_font_size", 12), SkinningDefs.TheInstance.GetBoolData("ops_title_use_bold_font", true) ? FontStyle.Bold : FontStyle.Regular),
		        Text = "Upgrade " + SkinningDefs.TheInstance.GetData("appname", "Application"),
		        TextAlign = ContentAlignment.MiddleLeft,
		        BackColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("popup_title_background_colour",
		            MyTextLabelBackColor),
		        ForeColor = TitleForeColor,
		        Location = new Point (0, 0)
            };
		    title.Size = SkinningDefs.TheInstance.GetSizeData("ops_popup_title_size", new Size(Width - (2 * title.Left), 20));
			Controls.Add(title);

			//Build the Choose Time, SLA  and Location Panel
			chooseTimePanel = new Panel();
			chooseTimePanel.Location = new Point(10, title.Bottom + SkinningDefs.TheInstance.GetIntData("popup_title_gap", 5));
            chooseTimePanel.Size = new Size(Width - (2 * chooseTimePanel.Left), 150);
			//chooseTimePanel.Size = new Size(this.Width - (2 * chooseTimePanel.Left), 110);
			chooseTimePanel.Visible = false;
			chooseTimePanel.BackColor = MyOperationsBackColor;
			Controls.Add(chooseTimePanel);

			//
			if (usingmins==false)
			{
				BackColor = Color.White;
			}


			okButton = new StyledDynamicButtonCommon ("standard", "OK");
			okButton.Font = MyDefaultSkinFontBold9;
			okButton.Size = new Size(80,20);
			okButton.Location = new Point(415,155);
			okButton.Click += okButton_Click;
			okButton.Visible = false;
			Controls.Add(okButton);

			cancelButton = new StyledDynamicButtonCommon ("standard", "Close");
			cancelButton.Font = MyDefaultSkinFontBold10;
			cancelButton.Size = new Size(80,20);
			cancelButton.Location = new Point(510,155);
			cancelButton.Click += cancelButton_Click;
			Controls.Add(cancelButton);

			//Create the Error Message
			errorlabel = new Label();
			errorlabel.Text = "";
			errorlabel.TextAlign = ContentAlignment.MiddleLeft;
			errorlabel.Size = new Size(400, 20);
			errorlabel.BackColor = MyTextLabelBackColor;
			errorlabel.ForeColor = errorLabel_foreColor;
			errorlabel.Font = MyDefaultSkinFontBold11;
			//errorlabel.Location = new Point(40 + 110 + 65 - 30, chooseTimePanel.Top + chooseTimePanel.Height);
			errorlabel.Location = new Point(10, 155);
			//errorlabel.Visible = false;
			Controls.Add(errorlabel);
			clearErrorMessage();
			errorlabel.SendToBack();

			RebuildUpgradeButtons(xOffset, yOffset);
			
			//Build the time controls 
			timeLabel = new Label();
			timeLabel.Text = GetUpgradeTimeLabelText();
			timeLabel.Font = MyDefaultSkinFontNormal12;
			timeLabel.BackColor = MyTextLabelBackColor;
			timeLabel.ForeColor = LabelForeColor;
			timeLabel.Size = new Size(130,20);
			timeLabel.Visible = true;
			timeLabel.TextAlign = ContentAlignment.MiddleLeft;
			timeLabel.Location = new Point(178,25-20);
			chooseTimePanel.Controls.Add(timeLabel);
			//
			whenEntryPanel = CreateWhenEntryPanel();
			chooseTimePanel.Controls.Add(whenEntryPanel);

			string AllowedChangeWindowsActions_str  = SkinningDefs.TheInstance.GetData("changewindowactions");
			if (AllowedChangeWindowsActions_str.ToLower()=="true")
			{
				//extract the change windows times 
				string AllowedChangeWindowTimes_str  = SkinningDefs.TheInstance.GetData("changewindowtimes");
				string[] time_strs = AllowedChangeWindowTimes_str.Split(',');

				int yoffset = 5;


				ManualTimeButton = new StyledDynamicButtonCommon ("standard", "Manual");
				ManualTimeButton.Font = MyDefaultSkinFontBold9;
				ManualTimeButton.Size = new Size(160,20);
				ManualTimeButton.Location = new Point(10,yoffset);
				ManualTimeButton.Click += ManualTimeButton_Click;
				chooseTimePanel.Controls.Add(ManualTimeButton);
				yoffset += ManualTimeButton.Height + 5;
				int count = 1;

				AutoTimeButtons.Clear();
				foreach (string st in time_strs)
				{
					if (count==1)
					{
						AutoTimeLabel = new Label();
						AutoTimeLabel.Text = "";
						AutoTimeLabel.Font = MyDefaultSkinFontNormal11;
						AutoTimeLabel.TextAlign = ContentAlignment.MiddleCenter;
						AutoTimeLabel.BackColor = AutoTimeBackColor;
						AutoTimeLabel.ForeColor = AutoTimeTextColor;
						AutoTimeLabel.Size = new Size(80,30);
						AutoTimeLabel.Location = new Point(200, 50-20);
						AutoTimeLabel.Visible = false;
						chooseTimePanel.Controls.Add(AutoTimeLabel);
						
					}

					string displayname = "Auto "+CONVERT.ToStr(count);
					ImageTextButton AutoTimeButton = new StyledDynamicButtonCommon ("standard", displayname);
					AutoTimeButton.Font = MyDefaultSkinFontBold9;
					AutoTimeButton.Size = new Size(160,20);
					AutoTimeButton.Location = new Point(10,yoffset);
					AutoTimeButton.Tag = CONVERT.ParseInt(st);
					AutoTimeButton.Click += AutoTimeButton_Click;
					chooseTimePanel.Controls.Add(AutoTimeButton);
					AutoTimeButtons.Add(AutoTimeButton);
					yoffset += AutoTimeButton.Height + 5;
					count++;
				}
			}

			focusJumper.Add(whenTextBox);
			focusJumper.Add(okButton);
			focusJumper.Add(cancelButton);

			SelectTimeButton(ManualTimeButton);
		}

		void SelectTimeButton (object sender)
		{
			ManualTimeButton.Active = (sender == ManualTimeButton);

			foreach (var button in AutoTimeButtons)
			{
				button.Active = (button == sender);
			}
		}

		protected virtual Panel CreateWhenEntryPanel ()
		{
			Panel panel = new Panel ();
			panel.Location = new Point (200, 30);
			panel.Size = new Size (80, 30);

			whenTextBox = new EntryBox();
			whenTextBox.DigitsOnly = true;
			whenTextBox.Visible = true;
			whenTextBox.Font = MyDefaultSkinFontNormal11;
			whenTextBox.Size = panel.Size;
			whenTextBox.MaxLength = 2;

			if (UsingMinutes)
			{
				whenTextBox.Text = "Now";
			}
			else
			{
				whenTextBox.Text = "Next Day";
			}
			whenTextBox.TextAlign = HorizontalAlignment.Center;
			whenTextBox.ForeColor = LabelForeColor;
			panel.Controls.Add(whenTextBox);
			whenTextBox.GotFocus += whenTextBox_GotFocus;
			whenTextBox.LostFocus += whenTextBox_LostFocus;
			whenTextBox.KeyUp += whenTextBox_KeyUp;

			return panel;
		}

		protected virtual string GetUpgradeTimeLabelText ()
		{
            if (UsingMinutes)
            {
                return SkinningDefs.TheInstance.GetData("esm_install_at_min", "Install At Min");
            }
            else
            {
                return SkinningDefs.TheInstance.GetData("esm_install_on_day", "Install On Day");
            }
		}

		protected void AutoTimeButton_Click(object sender, EventArgs e)
		{
			ImageTextButton b1 = (ImageTextButton) sender;
			autoTimeSecs = (int) b1.Tag;

			ShowAutoTimeLabel(FormatAutoTime(autoTimeSecs));
			SelectTimeButton(sender);
		}

		protected virtual string FormatAutoTime (int time)
		{
			int minutes = time / 60;
			int seconds = time % 60;

			return CONVERT.Format("{0}:{1:00}", minutes, seconds);
		}

		protected virtual void ShowAutoTimeLabel (string time)
		{
			if (AutoTimeLabel != null)
			{
				AutoTimeLabel.Text = time;
				AutoTimeLabel.Visible = true;
			}
			whenEntryPanel.Visible = false;
			clearErrorMessage();
		}

        protected virtual void ManualTimeButton_Click(object sender, EventArgs e)
        {
            autoTimeSecs = 0;
            if (AutoTimeLabel != null)
            {
                AutoTimeLabel.Visible = false;
            }
            whenEntryPanel.Visible = true;
            whenTextBox.Text = "Now";
            whenTextBox.Focus();
            clearErrorMessage();

			SelectTimeButton(sender);
        }

		protected virtual void RebuildUpgradeButtons(int xOffset, int yOffset)
		{
			int xpos = xOffset;
			int ypos = yOffset;

			focusJumper.Dispose();
			focusJumper = new FocusJumper();
			focusJumper.Add(okButton);
			focusJumper.Add(this.cancelButton);

			//Extract the possisble app upgrades and build the buttons 
			PendingUpgradeByName.Clear();
			ArrayList existingkids = AppUpgradeQueueNode.getChildren();
			foreach (Node kid in existingkids)
			{
				string pendingAppName = kid.GetAttribute("appname");
				int pendingAppTime = kid.GetIntAttribute("when",0);
				PendingUpgradeByName.Add(pendingAppName,pendingAppTime);
			}
			
			//Extract the possisble app upgrades and build the buttons 
			ArrayList types = new ArrayList();
			types.Add("App");
			apps.Clear();
			apps = _Network.GetNodesOfAttribTypes(types);

			//need to order the Apps by name 
			AllowedApps.Clear();
			AllowedAppSortList.Clear();

			//Need to count through to get the number of buttons to display 
			int count = 0;			
			foreach(Node app in apps.Keys)
			{
				Boolean canUserUpGrade = app.GetBooleanAttribute("userupgrade",false);
				Boolean canAppUpGrade = app.GetBooleanAttribute("canupgrade",false);
				string appname = app.GetAttribute("name");

				if(!appname.EndsWith("(M)"))
				{
					if (canUserUpGrade && (app.GetBooleanAttribute("show_as_asset", false) == onlyNodesTaggedAsAssets))
					{
						count++;
						AllowedApps.Add(appname, app);
						AllowedAppSortList.Add(appname);
					}
				}
			}
			AllowedAppSortList.Sort();

			//Mind to account for the Cooling system upgrade, if needed
			if (SkinningDefs.TheInstance.GetIntData("cooling_upgrade", 1) != 0)
			{
				//we need a extra panel for the cooling system upgrade panel
				count++;
			}

			int i = 0;
			int panelwidth = Width;
            int border = 10;
			int panelusablewidth = panelwidth;
			int buttonwidth = Math.Max(90, (panelwidth - ((count+1)*border)) / count);

            int runningBottom = 0;
            int paddingBetweenComponents = 5;
            int labelheight = SkinningDefs.TheInstance.GetIntData("upgrade_app_labelheight", 20);
            int panelheight = SkinningDefs.TheInstance.GetIntData("upgrade_app_panelheight", 100);

            ContentAlignment labelTextAlignment;
            if (SkinningDefs.TheInstance.GetBoolData("upgrade_app_isBottomAlignment", false))
            {
                labelTextAlignment = ContentAlignment.BottomCenter;
            }
            else
            {
                labelTextAlignment = ContentAlignment.MiddleCenter;
            }


			foreach (string appname in AllowedAppSortList)
			{
				if (AllowedApps.ContainsKey(appname))
				{
					Node app = (Node)AllowedApps[appname];
					if (app != null)
					{
						Boolean canUserUpGrade = app.GetBooleanAttribute("userupgrade",false);
						Boolean canAppUpGrade = app.GetBooleanAttribute("canupgrade",false);
						string isUpgradeSpecial = app.GetAttribute("upgrade_as_special");

						i++;
						//add a button 
						Panel p = new Panel();
						p.Size = new Size(buttonwidth,panelheight);
						p.Location = new Point(xpos, ypos);
						p.BackColor = MyGroupPanelBackColor;
						Controls.Add(p);

						Label l = new Label();
						l.Text = appname;
					    l.Font = ciNameFont;
                        l.TextAlign = labelTextAlignment;
						l.Location = new Point(1,5);
						l.Size = new Size(buttonwidth-2,labelheight);
						p.Controls.Add(l);
                        runningBottom = l.Bottom;

						if (PendingUpgradeByName.ContainsKey(appname))
						{
							int time = (int)PendingUpgradeByName[appname];

							Label t = new Label();
							t.Location = new Point(5, runningBottom + paddingBetweenComponents);
                            t.Size = new Size(buttonwidth - 10, 40);
                            t.Text = "Upgrade due at " + BuildTimeString(time);
							t.Font = MyDefaultSkinFontNormal8;
							p.Controls.Add(t);
                            runningBottom = t.Bottom;

                            ImageTextButton cancelButton = new StyledDynamicButtonCommon ("standard", "Cancel");
							cancelButton.Font = MyDefaultSkinFontBold9;
							cancelButton.Size = new Size(buttonwidth-10,20);
							cancelButton.Location = new Point(5, runningBottom + paddingBetweenComponents);
							cancelButton.Tag = appname;
							cancelButton.Click += CancelPendingUpgrade_button_Click;
							p.Controls.Add(cancelButton);
                            runningBottom = cancelButton.Bottom;

							focusJumper.Add(cancelButton);
						}
						else if (canAppUpGrade)
						{
							//extract special upgrade names (if provided)
							string btn_str = "Upgrade";
							if (! string.IsNullOrEmpty(isUpgradeSpecial))
							{
								btn_str = isUpgradeSpecial;
							}

							ImageTextButton button = new StyledDynamicButtonCommon ("standard", btn_str);
							button.Font = MyDefaultSkinFontBold9;
							button.Size = new Size(buttonwidth-10,20);
							button.Location = new Point(5, runningBottom + paddingBetweenComponents);                            
							button.Tag = appname;
							button.Click += AppUpgrade_button_Click;
							p.Controls.Add(button);
                            runningBottom = button.Bottom;
							focusJumper.Add(button);
						}
						xpos += buttonwidth + border;

						if ((xpos + buttonwidth) >= (Width - border))
						{
							xpos = xOffset;
							ypos += panelheight;
						}
					}
				}
			}

			// : Make cooling upgrade skinnable.
			if (SkinningDefs.TheInstance.GetIntData("cooling_upgrade", 1) != 0)
			{
				//add a button 
				Panel cp = new Panel();
				cp.Size = new Size(buttonwidth,100);
				cp.Location = new Point(xpos,40);
				//p.BackColor = Color.LightGray;
				cp.BackColor = MyGroupPanelBackColor;
				//cp.BackColor = Color.Transparent;
				Controls.Add(cp);

				Label cl = new Label();
				cl.Text = "Cooling";
				cl.Font = MyDefaultSkinFontBold10;
				cl.Location = new Point(1,5);
                cl.Size = new Size(buttonwidth - 2, labelheight);
                cl.TextAlign = labelTextAlignment;
				cp.Controls.Add(cl);
                runningBottom = cl.Bottom;

                if (PendingUpgradeByName.ContainsKey("Cooling"))
				{
					int time = (int)PendingUpgradeByName["Cooling"];

					Label ct = new Label();
					ct.Location = new Point(5, runningBottom + paddingBetweenComponents);
					ct.Size = new Size(buttonwidth-10,40);
					ct.Text = "Upgrade due at " + BuildTimeString(time);
					ct.Font = MyDefaultSkinFontNormal8;
					cp.Controls.Add(ct);
                    runningBottom = ct.Bottom;

					// Add a cancel button...
					ImageTextButton cancelButton = new StyledDynamicButtonCommon ("standard", "Cancel");
					cancelButton.ButtonFont = MyDefaultSkinFontBold9;
					cancelButton.Size = new Size(buttonwidth-10,20);
                    cancelButton.Location = new Point(5, runningBottom + paddingBetweenComponents); 
					cancelButton.Tag = "Cooling";
					cancelButton.Click += CancelPendingUpgrade_button_Click;
					cp.Controls.Add(cancelButton);
                    runningBottom = cancelButton.Bottom;

					focusJumper.Add(cancelButton);
				}
				else 
				{
					if (CoolingSystemUpgraded == false)
					{
						ImageTextButton button = new StyledDynamicButtonCommon ("standard", "Upgrade");
						button.Font = MyDefaultSkinFontBold9;
						button.Size = new Size(buttonwidth-10,20);
                        button.Location = new Point(5, runningBottom + paddingBetweenComponents); 
						button.Tag = "Cooling";
						button.Click += AppUpgrade_button_Click;
						cp.Controls.Add(button);
                        runningBottom = button.Bottom;

						focusJumper.Add(button);
					}
					xpos += buttonwidth + border;
				}
			}
		}

		protected virtual string BuildTimeString(int timevalue)
		{
			return FormatAutoTime(timevalue);
		}

		/// <summary>
		/// Setting the permitted limit for the requested action time
		/// </summary>
		/// <param name="maxminutes"></param>
		public void SetMaxMins(int maxminutes)
		{
			maxmins = maxminutes;
		}

		/// <summary>
		/// Application selection 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		protected virtual void AppUpgrade_button_Click(object sender, EventArgs e)
		{
			ImageTextButton button = (ImageTextButton) sender;

			focusJumper.Dispose();
			focusJumper = new FocusJumper();
			focusJumper.Add(whenTextBox);
			focusJumper.Add(okButton);
			focusJumper.Add(cancelButton);

			selectedUpgrade = (string) button.Tag;
            Node appToUpgade;
            if (selectedUpgrade == "Cooling")
            {
                appToUpgade = _Network.GetNamedNode("CoolingSystem");
            }
            else
            {
                appToUpgade = _Network.GetNamedNode(selectedUpgrade);
            }
			string upgradeType = appToUpgade.GetAttribute("upgrade_as_special");
			if (! string.IsNullOrEmpty(upgradeType))
			{
				chooseTimePanel.Visible = true;
				foreach (Control child in chooseTimePanel.Controls)
				{
					child.Hide();
				}
				title.Text = "Upgrade " + SkinningDefs.TheInstance.GetData("appname", "Application") + " " + selectedUpgrade + " to " + upgradeType;
				specialUpgrade = true;
			}
			else
			{
				chooseTimePanel.Visible = true;
				foreach (Control child in chooseTimePanel.Controls)
				{
					child.Show();
				}
				whenTextBox.Focus();
				title.Text = "Upgrade " + SkinningDefs.TheInstance.GetData("appname", "Application") + " > " + selectedUpgrade;
				specialUpgrade = false;
			}

			chooseTimePanel.Size = new Size (Width - (2 * chooseTimePanel.Left), Height - 10 - chooseTimePanel.Top);
			okButton.Visible = true;
			okButton.BringToFront();
			cancelButton.BringToFront();
			cancelButton.SetButtonText("Cancel", upColor, upColor, hoverColor, disabledColor);

			clearErrorMessage();
		}

		protected void cancelButton_Click(object sender, EventArgs e)
		{
			_mainPanel.DisposeEntryPanel();
		}

		protected int GetToday()
		{
			Node currentDayNode = _Network.GetNamedNode("CurrentDay");
			int CurrentDay = currentDayNode.GetIntAttribute("day",0);
			return CurrentDay;
		}

		/// <summary>
		/// This is used to prewarn the user that the day is booked
		/// </summary>
		/// <param name="day"></param>
		/// <returns></returns>
		protected Boolean IsDayFreeInCalendar(int day, out string errormessage)
		{
			Node CalendarNode = _Network.GetNamedNode("Calendar");
			errormessage = string.Empty;
			string daystr = CONVERT.ToStr(day);
			
			if(day > CalendarNode.GetIntAttribute("days",-1))
			{
				errormessage = "Specified day is outwith the round. Please enter a valid day.";
				return false;
			}

			//Need to iterate over children 
			foreach(Node calendarEvent in CalendarNode.getChildren())
			{
				int cday = calendarEvent.GetIntAttribute("day",0);
				string block = calendarEvent.GetAttribute("block");
				if (day == cday)
				{
					if (block.ToLower() == "true")
					{
						errormessage = "Specified day is already booked. Please enter a valid day.";
						return false;
					}
				}
			}
			return true;
		}

		protected virtual Boolean CreateUpgradeAppRequest(string AppName, int whenValue)
		{
			//need to check if the app can still be 	
			Boolean proceed = false;

			//need to check if the app can still be upgraded  	
			//Just in case something else has done the deed
			foreach(Node app in apps.Keys)
			{
				Boolean canAppUpGrade = app.GetBooleanAttribute("canupgrade",false);
				string appnodename = app.GetAttribute("name");
				if ((canAppUpGrade)&&(appnodename.ToLower() == AppName.ToLower()))
				{
					proceed = true;
				}
			}

			//Check that you can still upgrade this app (Cooling App)
			if (CoolingSystemNode != null)
			{
				CoolingSystemUpgraded = CoolingSystemNode.GetBooleanAttribute("upgraded", false);
				if (CoolingSystemUpgraded == false)
				{
					proceed = true;
				}
			}

			if (proceed)
			{
				ArrayList attrs = new ArrayList();
				attrs.Add( new AttributeValuePair("appname",AppName) );
				attrs.Add( new AttributeValuePair("when",whenValue) );
				attrs.Add( new AttributeValuePair("type","app_upgrade") );
				Node newEvent = new Node(AppUpgradeQueueNode, "app_upgrade", "", attrs);
			}
			else
			{
				setErrorMessage("Application "+AppName+" already upgraded");
			}
			return proceed;
		}

		protected void setErrorMessage(string errmsg)
		{
			errorlabel.Text = errmsg;
			errorlabel.Visible = true;
		}

		protected void clearErrorMessage()
		{
			errorlabel.Visible = false;
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

		protected virtual void okButton_Click(object sender, EventArgs e)
		{
			string errmsg = string.Empty;
			int currentGameSecond = getCurrentSecond();

			if (specialUpgrade)
			{
				CreateUpgradeAppRequest(selectedUpgrade, currentGameSecond + 1);
				_mainPanel.DisposeEntryPanel();
			}
			else
			{
				if (autoTimeSecs == 0)
				{
					//Has user used the predefined values
					if ((whenTextBox.Text == "Next Day") | (whenTextBox.Text == "Now"))
					{
						//Executing in Minutes, create a immediate Request 
						if (UsingMinutes)
						{
							currentGameSecond = getCurrentSecond();
							if (currentGameSecond > ((maxmins * 60) - SkinningDefs.TheInstance.GetIntData("ops_install_time", 120)))
							{
								setErrorMessage("Not enough time to complete operation.");
							}
							else
							{
								//Executing in Minutes, create a immediate Request 
								if (CreateUpgradeAppRequest(selectedUpgrade, 0))
								{
									_mainPanel.DisposeEntryPanel();
									return;
								}
							}
						}
						else
						{

							//Need to check that Tomorrow is Free
							int day = GetToday();
							if (IsDayFreeInCalendar(day + 1, out errmsg))
							{
								if (CreateUpgradeAppRequest(selectedUpgrade, day + 1))
								{
									_mainPanel.DisposeEntryPanel();
									return;
								}
							}
							else
							{
								setErrorMessage(errmsg);
							}
						}
					}
					else
					{
						Boolean proceed = true;
						int Requested_time = 0;
						try
						{
							Requested_time = GetRequestedTimeOffset();
						}
						catch (Exception)
						{
							setErrorMessage("Invalid Time requested.");
							proceed = false;
						}
						if (proceed)
						{
							if (UsingMinutes)
							{
								int timeToFire = 0;
								timeToFire = (Requested_time * 60);

								//New section for Time Range and Prevention in the last 2 mins
								if (Requested_time >= maxmins)
								{
									setErrorMessage("Specified time is outwith the round. Please enter a valid time.");
                                    // The reason for the *60 is that ServiceNow is treating the offset time as seconds in this section.
								}
								else
								{
									bool proceed_check = true;

									int time_taken_for_install = (SkinningDefs.TheInstance.GetIntData("ops_install_time", 120) / 60);
									int last_install_time = (maxmins - time_taken_for_install);

									if (Requested_time > (last_install_time))
									{
										setErrorMessage("Not enough time to complete operation.");
										proceed_check = false;
									}
									else if (Requested_time * 60 < currentGameSecond)
									{
										setErrorMessage("Specified time is in the past. Please enter a valid time.");
										proceed_check = false;
									}

									if (proceed_check)
									{
										if (CreateUpgradeAppRequest(selectedUpgrade, timeToFire))
										{
											_mainPanel.DisposeEntryPanel();
										}
									}

									
								}


							}
							else
							{
								if (Requested_time <= GetToday())
								{
									setErrorMessage("Specified day is in the past. Please enter a valid day.");
								}
								else
								{
									//Need to check that provided day is free
									if (IsDayFreeInCalendar(Requested_time, out errmsg))
									{
										if (CreateUpgradeAppRequest(selectedUpgrade, Requested_time))
										{
											_mainPanel.DisposeEntryPanel();
										}
									}
									else
									{
										setErrorMessage(errmsg);
									}
								}
							}
						}
					}
				}
				else
				{
                    if (autoTimeSecs >= currentGameSecond)
                    {
                        int timeToFire = 0;
                        timeToFire = autoTimeSecs;
                        if (CreateUpgradeAppRequest(selectedUpgrade, timeToFire))
                        {
                            _mainPanel.DisposeEntryPanel();
                        }
                    }
                    else
                    {
						setErrorMessage("Specified time is in the past. Please enter a valid time.");
                    }
				}
			}
		}

		protected void whenTextBox_GotFocus(object sender, EventArgs e)
		{
			string displaystr = string.Empty;

			if(UsingMinutes)
			{
				displaystr = "Now";
			}
			else
			{
				displaystr = "Next Day";
			}
			//dayTextBox.SelectAll();
			if(whenTextBox.Text == displaystr)
			{
				whenTextBox.SelectAll();
				//whenTextBox.Text = "";
			}
		}

		protected void whenTextBox_LostFocus(object sender, EventArgs e)
		{
			string displaystr = string.Empty;
			
			if(UsingMinutes)
			{
				displaystr = "Now";
			}
			else
			{
				displaystr = "Next Day";
			}
			try
			{
				if(whenTextBox.Text == "")
				{
					whenTextBox.Text = displaystr;
				}
			}
			catch
			{
				whenTextBox.Text = displaystr;
			}
		}

		protected virtual void CancelPendingUpgrade_button_Click(object sender, EventArgs e)
		{
			ImageTextButton button = sender as ImageTextButton;
			
			string cancelled_app_name = (string)button.Tag;

			//Remove from request
			Node Found_Node = null;
			foreach (Node n1 in AppUpgradeQueueNode.getChildren())
			{
				string node_app_name = n1.GetAttribute("appname");
				if (cancelled_app_name.ToLower() == node_app_name.ToLower())
				{
					Found_Node = n1;
				}
			}
			if (Found_Node != null)
			{
				Found_Node.Parent.DeleteChildTree(Found_Node);
				button.Dispose();
				//re-enabled the selected button 
				_mainPanel.DisposeEntryPanel();
			}
			else
			{
				setErrorMessage(Strings.SentenceCase(SkinningDefs.TheInstance.GetData("appname", "Application")) + " upgrade has already started.");
			}
		}
	
		protected override void Dispose(bool disposing)
		{
			if(disposing)
			{
				focusJumper.Dispose();
			}
			base.Dispose (disposing);
		}

		protected void UpgradeAppControl_GotFocus(object sender, EventArgs e)
		{
			cancelButton.Focus();
		}

		protected virtual void whenTextBox_KeyUp(object sender, KeyEventArgs e)
		{
			if(whenTextBox.Text.Length == 2)
			{
				if (whenTextBox.NextControl != null)
				{
					whenTextBox.NextControl.Focus();
				}
			}
		}

		protected void UpgradeAppControl_Resize(object sender, EventArgs e)
		{
			if (string.IsNullOrEmpty(SkinningDefs.TheInstance.GetData("ops_popup_cancel_button_position")))
			{
				cancelButton.Location = new Point(Width - cancelButton.Width - bottomCornerOffset, Height - cancelButton.Height - bottomCornerOffset);
				okButton.Location = new Point(cancelButton.Left - okButton.Width - bottomCornerOffset, cancelButton.Top);
			}

		    if (SkinningDefs.TheInstance.GetBoolData("ops_popup_title_use_full_width", false))
		    {
		        title.Size = new Size (Width, 25);
		    }
        }

		protected virtual int GetRequestedTimeOffset ()
		{
			return CONVERT.ParseInt(whenTextBox.Text);
		}
	}
}