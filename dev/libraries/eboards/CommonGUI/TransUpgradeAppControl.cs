using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;

using LibCore;
using Network;

using CoreUtils;
using IncidentManagement;

namespace CommonGUI
{
	/// <summary>
	/// Summary description for UpgradeAppControl.
	/// </summary>
	public class TransUpgradeAppControl : FlickerFreePanel
	{
		protected IDataEntryControlHolder _mainPanel;
		protected IncidentApplier _iApplier;
		protected NodeTree _Network;
		protected Node AppUpgradeQueueNode;
		protected Hashtable apps = new Hashtable();
		protected Hashtable AllowedApps = new Hashtable();
		protected ArrayList AllowedAppSortList = new ArrayList();

		bool specialUpgrade;

		protected Node CoolingSystemNode;
		protected Boolean CoolingSystemUpgraded = false;
		
		protected ImageTextButton okButton;
		protected ImageTextButton cancelButton;

		protected ArrayList buttonArray = new ArrayList();
		protected Hashtable PendingUpgradeByName = new Hashtable();
		
		protected string selectedUpgrade;

		protected Label title;
		protected Label timeLabel;
		protected EntryBox whenTextBox;
		protected Label errorlabel;

		protected string filename_long = "\\images\\buttons\\blank_big.png";
		protected string filename_mid = "\\images\\buttons\\blank_med.png";
		protected string filename_short = "\\images\\buttons\\blank_small.png";
		protected string filename_ok = "\\images\\buttons\\ok_blank_small.png";

		protected FocusJumper focusJumper;
		
		//skin stuff
		protected Color MyGroupPanelBackColor;
		protected Color MyOperationsBackColor;
		protected Font MyDefaultSkinFontNormal12 = null;
		protected Font MyDefaultSkinFontNormal11 = null;
		protected Font MyDefaultSkinFontNormal8 = null;
		protected Font MyDefaultSkinFontBold12 = null;
		protected Font MyDefaultSkinFontBold11 = null;
		protected Font MyDefaultSkinFontBold10 = null;
		protected Font MyDefaultSkinFontBold9 = null;
		
		protected bool UsingMinutes = false; //Control in Race Phase Using Minutes 
		public Color errorLabel_foreColor = Color.Red;

		public TransUpgradeAppControl(IDataEntryControlHolder mainPanel, IncidentApplier iApplier, NodeTree nt, 
			bool usingmins, Color OperationsBackColor, Color GroupPanelBackColor)
		{
            focusJumper = new FocusJumper();

			string errmsg_overridecolor = SkinningDefs.TheInstance.GetData("race_errormsg_override_color");
			if (errmsg_overridecolor != "")
			{
				errorLabel_foreColor = SkinningDefs.TheInstance.GetColorData("race_errormsg_override_color");
			}

			MyOperationsBackColor = OperationsBackColor;
			MyGroupPanelBackColor = GroupPanelBackColor;

			UsingMinutes = usingmins;
			buttonArray = new ArrayList();
			_Network = nt;
			_iApplier = iApplier;
			_mainPanel = mainPanel;

			string fontname =  SkinningDefs.TheInstance.GetData("fontname");
			MyDefaultSkinFontNormal12 = ConstantSizeFont.NewFont(fontname,12);
			MyDefaultSkinFontNormal11 = ConstantSizeFont.NewFont(fontname,11);
			MyDefaultSkinFontNormal8 = ConstantSizeFont.NewFont(fontname,8);
			MyDefaultSkinFontBold12 = ConstantSizeFont.NewFont(fontname,12,FontStyle.Bold);
			MyDefaultSkinFontBold11 = ConstantSizeFont.NewFont(fontname,11,FontStyle.Bold);
			MyDefaultSkinFontBold10 = ConstantSizeFont.NewFont(fontname,10,FontStyle.Bold);
			MyDefaultSkinFontBold9 = ConstantSizeFont.NewFont(fontname,9,FontStyle.Bold);
			
			AppUpgradeQueueNode = _Network.GetNamedNode("AppUpgradeQueue");
			CoolingSystemNode = _Network.GetNamedNode("CoolingSystem");
			CoolingSystemUpgraded = CoolingSystemNode.GetBooleanAttribute("upgraded", false);

            BuildControls();

			if (! usingmins)
			{
				BackColor = MyOperationsBackColor;
			}
			BorderStyle = BorderStyle.None;

			GotFocus += TransUpgradeAppControl_GotFocus;
		}

		public virtual void BuildScreenControls()
		{
			Color upColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_text_colour", Color.Black);
			Color downColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_down_text_colour", Color.White);
			Color hoverColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_hover_text_colour", Color.Green);
			Color disabledColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_disabled_text_colour", Color.DarkGray);

			//Create the Title 
			title = new Label();
			title.Font = MyDefaultSkinFontBold12;
			title.Text = "Upgrade " + SkinningDefs.TheInstance.GetData("appname", "Application");
			title.TextAlign = ContentAlignment.MiddleLeft;
			title.Size = new Size(Width,20);
			title.BackColor = MyOperationsBackColor;
            title.ForeColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("transition_text_colour", Color.Black);
			title.Location = new Point(10,10);
			Controls.Add(title);		

			//Create the Error Message
			errorlabel = new Label();
			errorlabel.Text = "";
			errorlabel.TextAlign = ContentAlignment.MiddleLeft;
			errorlabel.BackColor = MyOperationsBackColor;
			errorlabel.ForeColor = errorLabel_foreColor;
			errorlabel.Bounds = new Rectangle (10, 190, 375, 35);
			errorlabel.Visible = false;

            if (SkinningDefs.TheInstance.GetBoolData("errormessage_isSmallerFont", false))
            {
                errorlabel.Font = MyDefaultSkinFontBold9;
            }
            else
            {
                errorlabel.Font = MyDefaultSkinFontBold10;
            }
			Controls.Add(errorlabel);

			clearErrorMessage();

			okButton = new StyledDynamicButtonCommon ("standard", "OK");
			okButton.Font = MyDefaultSkinFontBold9;
			okButton.Size = new Size(80,20);
			okButton.Location = new Point(350,160+20);
			okButton.Click += okButton_Click;
			okButton.Visible = false;
			Controls.Add(okButton);

			cancelButton = new StyledDynamicButtonCommon ("standard", "Close");
			cancelButton.Font = MyDefaultSkinFontBold9;
			cancelButton.Size = new Size(80,20);
			cancelButton.Location = new Point(445,160+20);
			cancelButton.Click += cancelButton_Click;
			Controls.Add(cancelButton);

			//
			timeLabel = new Label();
			if(UsingMinutes)
			{
				timeLabel.Text = "Install On Min";
			}
			else
			{
				timeLabel.Text = "Install On Day";
			}
			timeLabel.Font = MyDefaultSkinFontNormal12;
			timeLabel.BackColor = MyOperationsBackColor;
            timeLabel.ForeColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("transition_text_colour", Color.Black);
            timeLabel.Size = new Size(130, 20);
			timeLabel.Visible = false;
			timeLabel.TextAlign = ContentAlignment.MiddleLeft;
			timeLabel.Location = new Point(100,170);
			Controls.Add(timeLabel);
			//
			whenTextBox = new EntryBox();
			whenTextBox.DigitsOnly = true;
			whenTextBox.Visible = false;
			whenTextBox.Font = MyDefaultSkinFontNormal11;
			whenTextBox.Size = new Size(80,30);
			whenTextBox.MaxLength = 2;

			if(UsingMinutes)
			{
				whenTextBox.Text = "Now";
			}
			else
			{
				whenTextBox.Text = "Next Day";
			}
			whenTextBox.Location = new Point(230, 165);
			whenTextBox.TextAlign = HorizontalAlignment.Center;
			Controls.Add(whenTextBox);
			whenTextBox.GotFocus += whenTextBox_GotFocus;
			whenTextBox.LostFocus += whenTextBox_LostFocus;
			whenTextBox.KeyUp +=whenTextBox_KeyUp;
		}


		protected virtual void RebuildUpgradeButtons(int xOffset, int yOffset)
		{
			Color upColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_text_colour", Color.Black);
			Color downColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_down_text_colour", Color.White);
			Color hoverColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_hover_text_colour", Color.Green);
			Color disabledColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_disabled_text_colour", Color.DarkGray);

			int xpos = xOffset;
			int ypos = yOffset;

			focusJumper.Dispose();
			focusJumper = new FocusJumper();
			focusJumper.Add(okButton);
			focusJumper.Add(this.cancelButton);

			//Extract the possible app upgrades and build the buttons 
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
					if (canUserUpGrade)
					{
						count++;
						AllowedApps.Add(appname, app);
						AllowedAppSortList.Add(appname);
					}
				}
			}
			AllowedAppSortList.Sort();

			//Mind to add a button for the Cooling system upgrade, if needed
			if (SkinningDefs.TheInstance.GetIntData("cooling_upgrade", 1) != 0)
			{
				//we need a extra panel for the cooling system upgrade panel
				count++;
			}

			int i = 0;
			int panelwidth = Width;
			int border = 4;
			int panelusablewidth = panelwidth;
			int buttonwidth = (panelwidth - ((count+1)*border)) / count;

            int runningBottom = 0;
            int paddingBetweenComponents = 5;
            int labelheight = SkinningDefs.TheInstance.GetIntData("upgrade_app_labelheight",20);
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

						i++;
						//add a button 
						Panel p = new Panel();
						p.Size = new Size(buttonwidth,panelheight);
						p.Location = new Point(xpos,40);
						p.BackColor = MyGroupPanelBackColor;
						Controls.Add(p);

						Label l = new Label();
						l.Text = appname;
                        l.Font = SkinningDefs.TheInstance.GetFont((float)SkinningDefs.TheInstance.GetDoubleData("upgrade_popup_ci_name_font_size", 10));
						l.Location = new Point(1,5);
						l.Size = new Size(buttonwidth-2,labelheight);
                        l.TextAlign = labelTextAlignment;                        
						p.Controls.Add(l);
                        l.ForeColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("transition_text_colour", Color.Black);
                        runningBottom = l.Bottom;
						
						if (PendingUpgradeByName.ContainsKey(appname))
						{
							int time = (int)PendingUpgradeByName[appname];

							Label t = new Label();
							t.Location = new Point(5,runningBottom + paddingBetweenComponents);                            
							t.Size = new Size(buttonwidth-10,40);
                            t.Text = "Upgrade due on day " + CONVERT.ToStr(time);
							t.Font = MyDefaultSkinFontNormal8;
                            t.ForeColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("transition_text_colour", Color.Black);
                            p.Controls.Add(t);
                            runningBottom = t.Bottom;
							

							// Add a cancel button...
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
							string upgradeType = app.GetAttribute("upgrade_as_special");

							if (string.IsNullOrEmpty(upgradeType))
							{
								upgradeType = "Upgrade";
							}

							ImageTextButton button = new StyledDynamicButtonCommon ("standard", upgradeType);
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

					}
				}
			}

			// : Make cooling upgrade skinnable.
			if (SkinningDefs.TheInstance.GetIntData("cooling_upgrade", 1) != 0)
			{
				//=======================================================================
				//==Adding the Cooling Application======================================= 
				//=======================================================================
				//add a button 
				Panel cp = new Panel();
				cp.Size = new Size(buttonwidth,panelheight);
				cp.Location = new Point(xpos,40);
				//cp.BackColor = Color.LightGray;
				cp.BackColor = MyGroupPanelBackColor;
				Controls.Add(cp);

				Label cl = new Label();
				cl.Text = "Cooling";
                cl.Font = SkinningDefs.TheInstance.GetFont((float)SkinningDefs.TheInstance.GetDoubleData("upgrade_popup_ci_name_font_size", 10));
				cl.Location = new Point(1,5);
				//cl.BackColor = Color.Aqua;
                cl.TextAlign = labelTextAlignment;
				cl.Size = new Size(buttonwidth-2,labelheight);
                cl.ForeColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("transition_text_colour", Color.Black);
                cp.Controls.Add(cl);
                runningBottom = cl.Bottom;

				if (PendingUpgradeByName.ContainsKey("Cooling"))
				{
					int time = (int)PendingUpgradeByName["Cooling"];

					Label ct = new Label();
					ct.Font = MyDefaultSkinFontNormal8;
					ct.Location = new Point(5,runningBottom + paddingBetweenComponents);
					ct.Size = new Size(buttonwidth-10,40);
					ct.Text = "Upgrade due on day " + CONVERT.ToStr(time);
                    ct.ForeColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("transition_text_colour", Color.Black);
                    cp.Controls.Add(ct);
                    runningBottom = ct.Bottom;

					// Add a cancel button...
					ImageTextButton cancelButton = new StyledDynamicButtonCommon ("standard", "Cancel");
					cancelButton.Font = MyDefaultSkinFontBold9;
					cancelButton.Size = new Size(buttonwidth-10,20);
					cancelButton.Location = new Point(5,runningBottom + paddingBetweenComponents); 
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
						button.Location = new Point(5,runningBottom + paddingBetweenComponents); 
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

		/// <summary>
		/// Application selection 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		protected virtual void AppUpgrade_button_Click(object sender, EventArgs e)
		{
			Color upColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_text_colour", Color.Black);
			Color downColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_down_text_colour", Color.White);
			Color hoverColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_hover_text_colour", Color.Green);
			Color disabledColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_disabled_text_colour", Color.DarkGray);

			ImageTextButton button = (ImageTextButton) sender;

			string appName = (string) button.Tag;
			Node app;
            if (appName == "Cooling")
            {
                 
                app = _Network.GetNamedNode("CoolingSystem");
            }
            else
            {
                app = _Network.GetNamedNode(appName);
            }

			string upgradeType = app.GetAttribute("upgrade_as_special");
			selectedUpgrade = appName;

			if (! string.IsNullOrEmpty(upgradeType))
			{
				whenTextBox.Visible = true;
				timeLabel.Visible = false;
				whenTextBox.Visible = false;
				whenTextBox.Clear();
				title.Text = "Upgrade " + SkinningDefs.TheInstance.GetData("appname", "Application") + " " + selectedUpgrade + " to " + upgradeType;
				specialUpgrade = true;
			}
			else
			{
				whenTextBox.Visible = true;
				timeLabel.Visible = true;
				whenTextBox.Visible = true;
				whenTextBox.Focus();
				title.Text = "Upgrade " + SkinningDefs.TheInstance.GetData("appname", "Application") + " > " + selectedUpgrade;
				specialUpgrade = false;
			}

			okButton.Visible = true;
			cancelButton.SetButtonText("Cancel",
				upColor,upColor,
				hoverColor,disabledColor);
			
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
			int CalendarLastDay = CalendarNode.GetIntAttribute("days",-1);

			if((day > (CalendarLastDay-1)))
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
			//
			return true;
		}


		protected virtual Boolean CreateUpgradeAppRequest(string AppName, int whenValue)
		{
		  //need to check if the app can still be 	
			Boolean proceed = false;

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
			CoolingSystemUpgraded = CoolingSystemNode.GetBooleanAttribute("upgraded", false);
			if (CoolingSystemUpgraded == false)
			{
				proceed = true;
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

		protected void okButton_Click(object sender, EventArgs e)
		{
			string errmsg = string.Empty;

			if (specialUpgrade)
			{
				CreateUpgradeAppRequest(selectedUpgrade, 0);
				_mainPanel.DisposeEntryPanel();
			}
			else
			{
				//Has user used the predefined values
				if ((whenTextBox.Text == "Next Day") | (whenTextBox.Text == "Now"))
				{
					if (UsingMinutes)
					{
						//Executing in Minutes, create a immediate Request 
						if (CreateUpgradeAppRequest(selectedUpgrade, 0))
						{
							_mainPanel.DisposeEntryPanel();
							return;
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
					int time = 0;
					Boolean conversionfail = false;
					try
					{
						time = CONVERT.ParseInt(whenTextBox.Text);
					}
					catch (Exception)
					{
						conversionfail = true;
					}
					if (conversionfail)
					{
						setErrorMessage("Invalid day requested");
					}
					else
					{
						if (UsingMinutes)
						{
							int timeToFire = 0;
							timeToFire = (time * 60);
							if (CreateUpgradeAppRequest(selectedUpgrade, timeToFire))
							{
								_mainPanel.DisposeEntryPanel();
							}
						}
						else
						{
							if (time <= GetToday())
							{
								setErrorMessage("Specified day is in the past. Please enter a valid day.");
							}
							else
							{
								//Need to check that provided day is free
								if (IsDayFreeInCalendar(time, out errmsg))
								{
									if (CreateUpgradeAppRequest(selectedUpgrade, time))
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

		public virtual void CancelPendingUpgrade (ImageTextButton button, string cancelled_app_name)
		{
			bool success = false;

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
				success = true;
			}
			else
			{
				setErrorMessage("Application upgrade has already started.");
			}

			focusJumper.Remove(button);

			if (button != null)
			{
				button.Parent.Controls.Remove(button);
				button.Dispose();
			}

			if (success)
			{
				_mainPanel.DisposeEntryPanel();
			}
		}

		protected void CancelPendingUpgrade_button_Click(object sender, EventArgs e)
		{
			ImageTextButton button = sender as ImageTextButton;
			
			string cancelled_app_name = (string)button.Tag;

			CancelPendingUpgrade(button, cancelled_app_name);
		}
	
		protected override void Dispose(bool disposing)
		{
			if(disposing)
			{
				focusJumper.Dispose();
			}
			base.Dispose (disposing);
		}

		protected void TransUpgradeAppControl_GotFocus(object sender, EventArgs e)
		{
			cancelButton.Focus();
		}

		protected void whenTextBox_KeyUp(object sender, KeyEventArgs e)
		{
			if(whenTextBox.Text.Length == 2)
			{
				if (whenTextBox.NextControl != null)
				{
					whenTextBox.NextControl.Focus();
				}
			}
		}

	    void BuildControls ()
	    {
	        Controls.Clear();
	        focusJumper.Clear();

	        BuildScreenControls();
	        RebuildUpgradeButtons(5, 20);

	        focusJumper.Add(whenTextBox);
	        focusJumper.Add(okButton);
	        focusJumper.Add(cancelButton);
        }

        protected override void OnSizeChanged (EventArgs e)
	    {
	        base.OnSizeChanged(e);

	        if (focusJumper != null)
	        {
	            BuildControls();
	        }

		    DoSize();
	    }

		void DoSize ()
		{
			int instep = 20;
			cancelButton.Location = new Point(Width - instep - cancelButton.Width, Height - instep - cancelButton.Height);
			okButton.Location = new Point(cancelButton.Left - instep - okButton.Width, cancelButton.Top);

			title.Bounds = new Rectangle(0, 0, Width, 25);
			title.BackColor = SkinningDefs.TheInstance.GetColorData("popup_title_background_colour", Color.White);
			title.ForeColor = SkinningDefs.TheInstance.GetColorData("popup_title_foreground_colour", Color.Black);
		}
	}
}