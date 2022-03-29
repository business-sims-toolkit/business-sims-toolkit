using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections;

using LibCore;
using CoreUtils;

using Network;
using IncidentManagement;

namespace CommonGUI
{
	public class TransUpgradeAppControl_DC : TransUpgradeAppControl
	{
		Label zoneLabel = null;
		Label zoneStatusLabel = null;
		EntryBox zoneTextBox = null;

		public TransUpgradeAppControl_DC (IDataEntryControlHolder mainPanel, IncidentApplier iApplier,
		                                  NodeTree nt, bool usingmins,
		                                  Color OperationsBackColor, Color GroupPanelBackColor)
			: base (mainPanel, iApplier, nt, usingmins, OperationsBackColor, GroupPanelBackColor)
		{
			errorlabel.Location = new Point (330, errorlabel.Top + 5);
			errorlabel.Size = new Size (Width - errorlabel.Left - 10, Height - errorlabel.Top - 10);
		}

		public bool AllZonesCoolingUpgraded ()
		{
			for (int zone = 1; zone <= 7; zone++)
			{
				if (! ExtractZoneUpgradeStatusByNumber(zone))
				{
					return false;
				}
			}

			return true;
		}

		protected bool ExtractZoneUpgradeStatusByNumber(int ZoneNumber)
		{
			bool zcs = false;
			string ZoneCoolerNodeName = "C"+CONVERT.ToStr(ZoneNumber);
			Node ZoneCoolerNode = _Network.GetNamedNode(ZoneCoolerNodeName);
			if (ZoneCoolerNode != null)
			{
				zcs = ZoneCoolerNode.GetBooleanAttribute("upgraded",false);
			}
			return zcs;
		}

		protected override void RebuildUpgradeButtons(int xOffset, int yOffset)
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
						p.Size = new Size(buttonwidth,100);
						p.Location = new Point(xpos,40);
						//p.BackColor = Color.LightGray;
						p.BackColor = MyGroupPanelBackColor;
						Controls.Add(p);

						Label l = new Label();
						l.Text = appname;
						l.Font = MyDefaultSkinFontBold10;
						l.Location = new Point(1,5);
						l.Size = new Size(buttonwidth-2,20);
						l.TextAlign = ContentAlignment.MiddleCenter;
						p.Controls.Add(l);

						if (PendingUpgradeByName.ContainsKey(appname))
						{
							int time = (int)PendingUpgradeByName[appname];

							Label t = new Label();
							t.Location = new Point(5,30);
							t.Size = new Size(buttonwidth-10,40);
							t.Text = "Upgrade due on day " + CONVERT.ToStr(time);
							t.Font = MyDefaultSkinFontNormal8;
							p.Controls.Add(t);

							// Add a cancel button...

							ImageTextButton cancelButton = new ImageTextButton(0);
							cancelButton.ButtonFont = MyDefaultSkinFontBold9;
							cancelButton.SetVariants(filename_short);
							cancelButton.Size = new Size(buttonwidth-10,20);
							cancelButton.Location = new Point(5,75); 
							cancelButton.Tag = appname;
							cancelButton.SetButtonText("Cancel",
								upColor,upColor,
								hoverColor,disabledColor);
							cancelButton.Click += CancelPendingUpgrade_button_Click;
							cancelButton.Visible = true;
							p.Controls.Add(cancelButton);

							focusJumper.Add(cancelButton);
						}
						else if (canAppUpGrade)
						{
							ImageTextButton button = new ImageTextButton(0);
							button.ButtonFont = MyDefaultSkinFontBold9;
							button.SetVariants(filename_short);
							button.Size = new Size(buttonwidth-10,20);
							button.Location = new Point(5,30); 
							button.Tag = appname;
							button.SetButtonText("Upgrade",
								upColor,upColor,
								hoverColor,disabledColor);
							button.Click += AppUpgrade_button_Click;
							button.Visible = true;
							p.Controls.Add(button);

							focusJumper.Add(button);
						}
						xpos += buttonwidth + border;

					}
				}
			}

			// : Make cooling upgrade skinnable.
			if ((SkinningDefs.TheInstance.GetIntData("cooling_upgrade", 1) != 0) && (! AllZonesCoolingUpgraded()))
			{
				//=======================================================================
				//==Adding the Cooling Application======================================= 
				//=======================================================================
				//add a button 
				Panel cp = new Panel();
				cp.Size = new Size(buttonwidth,100);
				cp.Location = new Point(xpos,40);
				//cp.BackColor = Color.LightGray;
				cp.BackColor = MyGroupPanelBackColor;
				Controls.Add(cp);

				Label cl = new Label();
				cl.Text = "Cooling";
				cl.Font = MyDefaultSkinFontBold10;
				cl.Location = new Point(1,5);
				//cl.BackColor = Color.Aqua;
				cl.TextAlign = ContentAlignment.MiddleCenter;
				cl.Size = new Size(buttonwidth-2,20);
				cp.Controls.Add(cl);

				ArrayList pendingZoneCoolingUpgrades = new ArrayList ();
				for (int zone = 1; zone <= 7; zone++)
				{
					if (PendingUpgradeByName.ContainsKey("Zone Cooling " + CONVERT.ToStr(zone)))
					{
						pendingZoneCoolingUpgrades.Add(zone);
					}
				}

				if (pendingZoneCoolingUpgrades.Count > 0)
				{
					// Add a cancel button...
					ImageTextButton cancelButton = new ImageTextButton(0);
					cancelButton.ButtonFont = MyDefaultSkinFontBold9;
					cancelButton.SetVariants(filename_short);
					cancelButton.Size = new Size(buttonwidth-10,20);
					cancelButton.Location = new Point(5,75); 
					cancelButton.SetButtonText("Cancel",
						upColor,upColor,
						hoverColor,disabledColor);
					cancelButton.Click += CancelMultiplePendingUpgrades_button_Click;
					cancelButton.Visible = true;
					focusJumper.Add(cancelButton);
					cp.Controls.Add(cancelButton);

					focusJumper.Add(cancelButton);

					Label ct = new Label();
					ct.Font = MyDefaultSkinFontNormal8;
					ct.Location = new Point(5,50);
					ct.Size = new Size(buttonwidth-10,40);
					ct.Text = "Upgrade(s) due";
					cp.Controls.Add(ct);
				}

				ImageTextButton button = new ImageTextButton(0);
				button.ButtonFont = MyDefaultSkinFontBold9;
				button.SetVariants(filename_short);
				button.Size = new Size(buttonwidth-10,20);
				button.Location = new Point(5,30); 
				button.Tag = "Cooling";
				button.SetButtonText("Upgrade",
					upColor,upColor,
					hoverColor,disabledColor);
				button.Click += AppUpgrade_button_Click;
				button.Visible = true;
				cp.Controls.Add(button);

				focusJumper.Add(button);

				xpos += buttonwidth + border;
			}
		}

		protected override void AppUpgrade_button_Click(object sender, EventArgs e)
		{
			Color upColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_text_colour", Color.Black);
			Color downColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_down_text_colour", Color.White);
			Color hoverColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_hover_text_colour", Color.Green);
			Color disabledColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_disabled_text_colour", Color.DarkGray);

			ImageTextButton button = (ImageTextButton) sender;

			whenTextBox.Visible = true;
			timeLabel.Visible = true;
			whenTextBox.Focus();
	
			okButton.Visible = true;
			cancelButton.SetButtonText("Cancel",
				upColor,upColor,
				hoverColor,disabledColor);

			selectedUpgrade = (string) button.Tag;
			title.Text = "Upgrade " + SkinningDefs.TheInstance.GetData("appname", "Application") + " > "+selectedUpgrade;
			clearErrorMessage();

			//have we selected the Cooling Upgrade
			if (selectedUpgrade.ToLower().IndexOf("cool")>-1)
			{
				shiftForZoneCoolerUpgrade();
			}
			else
			{
				RemoveZoneCoolerUpgrade();
			}
		}

		protected void RemoveZoneCoolerUpgrade ()
		{
			if (zoneLabel != null)
			{
				Controls.Remove(zoneLabel);
				zoneLabel = null;
			}

			if (zoneTextBox != null)
			{
				Controls.Remove(zoneTextBox);
				focusJumper.Remove(zoneTextBox);
				zoneTextBox = null;
			}

			if (zoneStatusLabel != null)
			{
				Controls.Remove(zoneStatusLabel);
				zoneStatusLabel = null;
			}
		}

		protected void shiftForZoneCoolerUpgrade()
		{
			Color MyTextLabelBackColor = timeLabel.BackColor;
			Color MyTitleForeColor = timeLabel.ForeColor;

			if (timeLabel != null)
			{
				timeLabel.Location = new Point(0, 180);
				timeLabel.Size = new Size (210 - timeLabel.Left, 20);
				timeLabel.TextAlign = ContentAlignment.MiddleRight;
			}
			if (whenTextBox != null)
			{
				whenTextBox.Location = new Point(230, 175);
			}
			//Build the time controls 

			if (zoneLabel == null)
			{
				zoneLabel = new Label();
			}
			zoneLabel.Text = "Upgrade Zone Cooling:";
			zoneLabel.Font = MyDefaultSkinFontNormal12;
			zoneLabel.BackColor = MyTextLabelBackColor;
			zoneLabel.ForeColor = MyTitleForeColor;
			zoneLabel.Location = new Point(0, 210);
			zoneLabel.Size = new Size (210 - zoneLabel.Left, 20);
			zoneLabel.Visible = true;
			zoneLabel.TextAlign = ContentAlignment.MiddleRight;
			Controls.Add(zoneLabel);

			if (zoneTextBox == null)
			{
				zoneTextBox = new EntryBox();
			}
			zoneTextBox.DigitsOnly = true;
			zoneTextBox.Visible = true;
			zoneTextBox.Font = MyDefaultSkinFontNormal11;
			zoneTextBox.Size = new Size(80,30);
			zoneTextBox.MaxLength = 1;
			zoneTextBox.Text = "1";
			zoneTextBox.CharToIgnore('8');
			zoneTextBox.CharToIgnore('9');
			zoneTextBox.CharToIgnore('0');

			zoneTextBox.Location = new Point(230, 205);
			zoneTextBox.TextAlign = HorizontalAlignment.Center;
			zoneTextBox.ForeColor = MyTitleForeColor;
			Controls.Add(zoneTextBox);
			focusJumper.Add(zoneTextBox);
			zoneTextBox.GotFocus += zoneTextBox_GotFocus;
			zoneTextBox.LostFocus += zoneTextBox_LostFocus;
			zoneTextBox.KeyUp +=zoneTextBox_KeyUp;

			//Build the time controls 
			if (zoneStatusLabel == null)
			{
				zoneStatusLabel = new Label();
			}
			zoneStatusLabel.Text = "Allowed Upgrades: ";
			zoneStatusLabel.Font = MyDefaultSkinFontBold12;
			zoneStatusLabel.BackColor = MyTextLabelBackColor;
			zoneStatusLabel.ForeColor = MyTitleForeColor;
			zoneStatusLabel.Size = new Size(320,20);
			zoneStatusLabel.Visible = true;
			zoneStatusLabel.TextAlign = ContentAlignment.MiddleLeft;
			zoneStatusLabel.Location = new Point(20,145);
			Controls.Add(zoneStatusLabel);

			string status_str="";
			bool atleastoneoption = false;
			for (int step =0; step < 7; step++)
			{
				//mind that zones start with c1 not c0
				if (ExtractZoneUpgradeStatusByNumber(step+1)==false)
				{
					if (atleastoneoption)
					{
						status_str = status_str + ",";
					}
					status_str = status_str + CONVERT.ToStr(step+1);
					atleastoneoption = true;
				}
			}
			zoneStatusLabel.Text = "Allowed Upgrades: " + status_str;		
		}

		protected void zoneTextBox_KeyUp(object sender, KeyEventArgs e)
		{
		}

		protected void zoneTextBox_GotFocus(object sender, EventArgs e)
		{
		}

		protected void zoneTextBox_LostFocus(object sender, EventArgs e)
		{
			string displaystr = "1";
			try
			{
				if(zoneTextBox.Text == "")
				{
					zoneTextBox.Text = displaystr;
				}
			}
			catch
			{
				zoneTextBox.Text = displaystr;
			}
		}

		protected override Boolean CreateUpgradeAppRequest(string AppName, int whenValue)
		{
			//need to check if the app can still be 	
			Boolean proceed = false;
			Boolean pending_exists = false;

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

			//This is for cooling app request only 
			if (AppName.ToLower().IndexOf("cool")>-1)
			{
				//Check that you can still upgrade this app (Cooling App)
				//we are using zone based cooling 
				if (ExtractZoneUpgradeStatusByNumber(CONVERT.ParseInt(zoneTextBox.Text))==false)
				{
					proceed = true;
					AppName = "Zone Cooling "+ zoneTextBox.Text;
				}
				else
				{
					AppName = "Zone Cooling "+ zoneTextBox.Text;
				}
				//Check if we already have a pending request 
				if (PendingUpgradeByName.ContainsKey(AppName))
				{
					pending_exists = true;
					proceed = false;
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
				if (pending_exists)
				{
					setErrorMessage(""+AppName+" upgrade already requested");
				}
				else
				{
					setErrorMessage(""+AppName+" already upgraded");
				}
			}
			return proceed;
		}

		protected void CancelMultiplePendingUpgrades_button_Click(object sender, EventArgs e)
		{
			CancelPendingCoolingUpgradePanel panel = new CancelPendingCoolingUpgradePanel (this, PendingUpgradeByName,
			                                                                               SkinningDefs.TheInstance.GetColorDataGivenDefault("button_text_colour", Color.Black),
			                                                                               SkinningDefs.TheInstance.GetColorDataGivenDefault("button_hover_text_colour", Color.Green),
			                                                                               SkinningDefs.TheInstance.GetColorDataGivenDefault("button_disabled_text_colour", Color.DarkGray),
			                                                                               MyDefaultSkinFontNormal8);
			panel.Size = Size;
			Controls.Add(panel);
			panel.BringToFront();
		}

		public void CloseCancelPendingCoolingUpgradePanel (CancelPendingCoolingUpgradePanel panel)
		{
			Controls.Remove(panel);
		}
	}
}