using System;
using System.Drawing;
using System.Windows.Forms;

using LibCore;
using Network;

using CoreUtils;
using IncidentManagement;

namespace CommonGUI
{

	/// <summary>
	/// This is only need to correctly set the new width for this popup control
	/// The MS version needs to be 550 rather than 590 of the original control width
	/// There are 2 ways to handle this without this subclass
	///   A, Extend the size event handler to handle the already constructed pnale and internal controls
	///   B, The constructor should not construct panels just take data 
	///      There could be public method to build the UI after constructor has completed 
	///      Different sizes would then be set in the gap between construction and buildUI
	///      No sub class needed just the calling control defining the size before the BuildUI.
	/// </summary>
	public class UpgradeAppControl_MS : UpgradeAppControl
	{
		public UpgradeAppControl_MS(IDataEntryControlHolder mainPanel, IncidentApplier iApplier, NodeTree nt, 
			bool usingmins, Boolean IsTrainingMode, Color OperationsBackColor, Color GroupPanelBackColor, 
			bool _playing):base
		(mainPanel, iApplier, nt, usingmins, IsTrainingMode, OperationsBackColor, GroupPanelBackColor, _playing)
		{
		}

		/// <summary>
		/// This method handles the construction of the functional panels 
		/// Allows descedant classes to define thier own panels
		/// 
		/// We need to refactor thsi and extract out the common bits as this is mostly a restatement of the base method.
		/// Just the hieght and positioning changes a little for MK
		/// </summary>
		public override void CompleteConstructPanels(bool usingmins)
		{
			int xOffset = 5;
			int yOffset = 30;

			Width = 550;
			Height = 185;
            
			//Create the Title 
			title = new Label();
			title.Font = MyDefaultSkinFontBold12;
			title.Text = "Upgrade Application";
			title.TextAlign = ContentAlignment.MiddleLeft;
			title.Size = new Size(Width,20);
			title.BackColor = MyTextLabelBackColor;
			title.ForeColor = TitleForeColor;
			title.Location = new Point(10,5);
			Controls.Add(title);

			//Build the Choose Time, SLA  and Location Panel
			chooseTimePanel = new Panel();
			chooseTimePanel.Size = new Size(Width-10, 120);
			chooseTimePanel.Location = new Point(5, title.Bottom + 5);
			chooseTimePanel.Visible = false;
			chooseTimePanel.BackColor = MyOperationsBackColor;
			Controls.Add(chooseTimePanel);

			//RebuildUpgradeButtons(xOffset, yOffset);

			//
			if (usingmins==false)
			{
				BackColor = Color.White;
			}
			//Create the Error Message
			errorlabel = new Label();
			errorlabel.Text = "";
			errorlabel.TextAlign = ContentAlignment.MiddleCenter;
			errorlabel.Size = new Size(450+30,20);
			errorlabel.BackColor = MyTextLabelBackColor;
			errorlabel.ForeColor = Color.Red;
			errorlabel.Font = MyDefaultSkinFontBold11;
			errorlabel.Visible = false;
			Controls.Add(errorlabel);	

			clearErrorMessage();

			cancelButton = new ImageTextButton(0);
			cancelButton.ButtonFont = MyDefaultSkinFontBold10;
			cancelButton.SetVariants(filename_short);
			cancelButton.Size = new Size(80,20);
			cancelButton.Location = SkinningDefs.TheInstance.GetPointData("ops_popup_cancel_button_position", 510, 155);
			cancelButton.SetButtonText("Close",
				upColor,upColor,
				hoverColor,disabledColor);
			cancelButton.Click += cancelButton_Click;
			cancelButton.Visible = true;
			Controls.Add(cancelButton);

			okButton = new ImageTextButton(0);
			okButton.ButtonFont = MyDefaultSkinFontBold10;
			okButton.SetVariants(filename_short);
			okButton.Size = new Size(80, 20);
			okButton.Location = new Point(cancelButton.Left - 10 - okButton.Width, cancelButton.Top);
			okButton.SetButtonText("OK",
				upColor, upColor,
				hoverColor, disabledColor);
			okButton.Click += okButton_Click;
			okButton.Visible = false;
			Controls.Add(okButton);

			errorlabel.Location = new Point(10, okButton.Top - 30);
			errorlabel.BringToFront();

			RebuildUpgradeButtons(xOffset, yOffset);
			
			//Build the time controls 
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
			timeLabel.BackColor = MyTextLabelBackColor;
			timeLabel.ForeColor = LabelForeColor;
			timeLabel.Size = new Size(130,20);
			timeLabel.Visible = true;
			timeLabel.TextAlign = ContentAlignment.MiddleLeft;
			timeLabel.Location = new Point(178,25-20);
			chooseTimePanel.Controls.Add(timeLabel);
          
			//
			whenTextBox = new EntryBox();
			whenTextBox.DigitsOnly = true;
			whenTextBox.Visible = true;
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
			whenTextBox.Location = new Point(200, 50-20);
			whenTextBox.TextAlign = HorizontalAlignment.Center;
			whenTextBox.ForeColor = LabelForeColor;
			whenTextBox.ForeColor = Color.Black;
			chooseTimePanel.Controls.Add(whenTextBox);
			whenTextBox.GotFocus += whenTextBox_GotFocus;
			whenTextBox.LostFocus += whenTextBox_LostFocus;
			whenTextBox.KeyUp +=whenTextBox_KeyUp;

			string AllowedChangeWindowsActions_str  = SkinningDefs.TheInstance.GetData("changewindowactions");
			if (AllowedChangeWindowsActions_str.ToLower()=="true")
			{
				//extract the change windows times 
				string AllowedChangeWindowTimes_str  = SkinningDefs.TheInstance.GetData("changewindowtimes");
				string[] time_strs = AllowedChangeWindowTimes_str.Split(',');
				//Build the controls 

				int yoffset = 5; //29
				//General Manual Timing Button

				ManualTimeButton = new ImageTextButton(0);
				ManualTimeButton.ButtonFont = MyDefaultSkinFontBold9;
				ManualTimeButton.SetVariants(filename_mid);
				ManualTimeButton.Size = new Size(160,20);
				ManualTimeButton.Location = new Point(10,yoffset);
				ManualTimeButton.SetButtonText("Manual",
					upColor,upColor,
					hoverColor,disabledColor);
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
					ImageTextButton AutoTimeButton = new ImageTextButton(0);
					AutoTimeButton.ButtonFont = MyDefaultSkinFontBold9;
					AutoTimeButton.SetVariants(filename_mid);
					AutoTimeButton.Size = new Size(160,20);
					AutoTimeButton.Location = new Point(10,yoffset);
					AutoTimeButton.Tag = CONVERT.ParseInt(st);
					AutoTimeButton.SetButtonText(displayname,
						upColor,upColor,
						hoverColor,disabledColor);
					AutoTimeButton.Click += AutoTimeButton_Click;
					AutoTimeButton.Visible = true;
					chooseTimePanel.Controls.Add(AutoTimeButton);
					AutoTimeButtons.Add(AutoTimeButton);
					yoffset += AutoTimeButton.Height + 5;
					count++;
				}
			}

			focusJumper.Add(whenTextBox);
			focusJumper.Add(okButton);
			focusJumper.Add(cancelButton);
		}


        protected override void ShowAutoTimeLabel(string time)
        {
            if (AutoTimeLabel != null)
            {
                AutoTimeLabel.Text = time;
                AutoTimeLabel.Visible = true;
            }
            whenTextBox.Visible = false;
            clearErrorMessage();
        }


        protected override void ManualTimeButton_Click(object sender, EventArgs e)
        {
            autoTimeSecs = 0;
            if (AutoTimeLabel != null)
            {
                AutoTimeLabel.Visible = false;
            }
            whenTextBox.Visible = true;
            whenTextBox.Text = "Now";
            whenTextBox.Focus();
            clearErrorMessage();
        }

		protected override void okButton_Click(object sender, EventArgs e)
		{
			string errmsg = string.Empty;
			int currentGameSecond = 0;
			int currentGameTime = getCurrentSecond();

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
					int time = 0;
					try
					{
						time = CONVERT.ParseInt(whenTextBox.Text);
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
							timeToFire = (time * 60);

							//New section for Time Range and Prevention in the last 2 mins
							if (time >= maxmins)
							{
								setErrorMessage("Specified time is outwith the round. Please enter a valid time.");
							}
							else
							{
								if (time > (maxmins - (SkinningDefs.TheInstance.GetIntData("ops_install_time", 120) / 60)))
								{
									setErrorMessage("Not enough time to complete operation.");
								}
								else if (currentGameTime > time * 60 )
								{
									setErrorMessage("Specified time is in the past. Please enter a valid time.");
								}
								else
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
			else
			{
				int timeToFire = 0;
				timeToFire = autoTimeSecs;
				if (currentGameTime > autoTimeSecs)
				{
					setErrorMessage("Specified time is in the past. Please enter a valid time.");
				}
				else
				{
					if (CreateUpgradeAppRequest(selectedUpgrade, timeToFire))
					{
						_mainPanel.DisposeEntryPanel();
					}
				}
			}
		}




	}
}



