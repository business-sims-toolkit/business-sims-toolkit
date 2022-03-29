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
	public class UpgradeAppFirmHardwareControl : UpgradeAppControl
	{
		EntryBox zoneTextBox = null;
		ImageTextButton coolingButton = null;

		protected Hashtable PendingFirmwareUpgradeByName = new Hashtable();

		bool inFirmwareMode = false;

		string appName;
		Node app;

		public UpgradeAppFirmHardwareControl (IDataEntryControlHolder mainPanel, IncidentApplier iApplier, NodeTree nt, 
			bool usingmins, Boolean IsTrainingMode, Color OperationsBackColor, Color GroupPanelBackColor, 
			bool _playing, string app)
			: base (mainPanel, iApplier, nt, usingmins, IsTrainingMode, OperationsBackColor, GroupPanelBackColor, _playing)
		{
			appName = app;
			this.app = nt.GetNamedNode(appName);

			string showName;
			if ((this.app != null) && this.app.GetBooleanAttribute("showByLocation", false))
			{
				showName = this.app.GetAttribute("location");
			}
			else
			{
				showName = appName;
			}

			title.Text = "Upgrade " + SkinningDefs.TheInstance.GetData("appname", "Application") + " > " + showName;

			//override the standard overall cooling system 
			CoolingSystemUpgraded = false;

			RebuildUpgradeButtons(0, 0);
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

		protected override void RebuildUpgradeButtons(int xOffset, int yOffset)
		{
			if (app == null)
			{
				return;
			}

			int xpos = xOffset + 50;
			int ypos = yOffset;

			focusJumper.Dispose();
			focusJumper = new FocusJumper();
			focusJumper.Add(okButton);
			focusJumper.Add(this.cancelButton);

			//Extract the possisble app upgrades and build the buttons 
			PendingUpgradeByName.Clear();
			PendingFirmwareUpgradeByName.Clear();
			ArrayList existingkids = AppUpgradeQueueNode.getChildren();
			foreach (Node kid in existingkids)
			{
				string pendingAppName = kid.GetAttribute("appname");
				int pendingAppTime = kid.GetIntAttribute("when",0);

				if (kid.GetAttribute("upgrade_option").ToLower() == "firmware")
				{
					PendingFirmwareUpgradeByName.Add(pendingAppName,pendingAppTime);
				}
				else
				{
					PendingUpgradeByName.Add(pendingAppName,pendingAppTime);
				}
			}
			
			//Extract the possisble app upgrades and build the buttons 
			ArrayList types = new ArrayList();
			types.Add("App");
			apps.Clear();
			apps = _Network.GetNodesOfAttribTypes(types);

			//need to order the Apps by name 
			AllowedApps.Clear();
			AllowedAppSortList.Clear();

			int count = 3;
			int panelwidth = Width;
			int border = 10;
			int panelusablewidth = panelwidth;
			int buttonwidth = (panelwidth - ((count+1)*border) - xpos) / count; 

			bool canUpgradeHardware = app.GetBooleanAttribute("canupgrade", false);
			bool showHardware = PendingUpgradeByName.ContainsKey(appName) || canUpgradeHardware;
			bool canUpgradeFirmware = app.GetBooleanAttribute("can_upgrade_firmware", false);
			bool showFirmware = PendingFirmwareUpgradeByName.ContainsKey(appName) || canUpgradeFirmware;

			if (showFirmware)
			{
				//add a button 
				Panel p = new Panel();
				p.Size = new Size(buttonwidth,100);
				p.Location = new Point(xpos,40-10);
				//p.BackColor = Color.LightGray;
				p.BackColor = MyGroupPanelBackColor;
				//p.BackColor = Color.Transparent;
				Controls.Add(p);

				Label l = new Label();
				l.Text = "Firmware";
				l.Font = MyDefaultSkinFontBold10;
				l.TextAlign = ContentAlignment.MiddleCenter;
				l.Location = new Point(1,5);
				l.Size = new Size(buttonwidth-2,20);
				p.Controls.Add(l);
			
				if (PendingFirmwareUpgradeByName.ContainsKey(appName))
				{
					int time = (int)PendingFirmwareUpgradeByName[appName];

					Label t = new Label();
					t.Location = new Point(5,30);
					t.Size = new Size(buttonwidth-10,40);
					t.Text = "Upgrade due at " + BuildTimeString(time);
					t.Font = MyDefaultSkinFontNormal8;
					p.Controls.Add(t);

					ImageTextButton cancelButton = new ImageTextButton(0);
					cancelButton.ButtonFont = MyDefaultSkinFontBold9;
					cancelButton.SetVariants(filename_short);
					cancelButton.Size = new Size(buttonwidth-10,20);
					cancelButton.Location = new Point(5,75); 
					cancelButton.Tag = appName;
					cancelButton.ButtonFont = MyDefaultSkinFontBold9;
					cancelButton.SetButtonText("Cancel", upColor, upColor, hoverColor, disabledColor);
					cancelButton.Click += CancelPendingFirmwareUpgrade_button_Click;
					cancelButton.Visible = true;
					p.Controls.Add(cancelButton);

					focusJumper.Add(cancelButton);

				}
				else if (canUpgradeFirmware)
				{
					ImageTextButton button = new ImageTextButton(0);
					button.ButtonFont = MyDefaultSkinFontBold9;
					button.SetVariants(filename_long);
					button.Size = new Size(buttonwidth-10,20);
					button.Location = new Point(5,30); 
					button.Tag = appName;

					button.SetButtonText("Upgrade", upColor, upColor,	hoverColor, disabledColor);
					button.Click += AppUpgradeFirmware_button_Click;
					button.Enabled = playing;
					button.Visible = true;

					button.Enabled = playing;
					p.Controls.Add(button);
					focusJumper.Add(button);

					button.Enabled = playing;
				}
			}
			xpos += (buttonwidth + border) * 2;

			if (showHardware)
			{
				//add a button 
				Panel p = new Panel();
				p.Size = new Size(buttonwidth,100);
				p.Location = new Point(xpos,40-10);
				//p.BackColor = Color.LightGray;
				p.BackColor = MyGroupPanelBackColor;
				//p.BackColor = Color.Transparent;
				Controls.Add(p);

				Label l = new Label();
				l.Text = "Hardware";
				l.Font = MyDefaultSkinFontBold10;
				l.TextAlign = ContentAlignment.MiddleCenter;
				l.Location = new Point(1,5);
				l.Size = new Size(buttonwidth-2,20);
				p.Controls.Add(l);
			
				if (PendingUpgradeByName.ContainsKey(appName))
				{
					int time = (int)PendingUpgradeByName[appName];

					Label t = new Label();
					t.Location = new Point(5,30);
					t.Size = new Size(buttonwidth-10,40);
					t.Text = "Upgrade due at " + BuildTimeString(time);
					t.Font = MyDefaultSkinFontNormal8;
					p.Controls.Add(t);

					ImageTextButton cancelButton = new ImageTextButton(0);
					cancelButton.ButtonFont = MyDefaultSkinFontBold9;
					cancelButton.SetVariants(filename_short);
					cancelButton.Size = new Size(buttonwidth-10,20);
					cancelButton.Location = new Point(5,75); 
					cancelButton.Tag = appName;
					cancelButton.ButtonFont = MyDefaultSkinFontBold9;
					cancelButton.SetButtonText("Cancel", upColor, upColor, hoverColor, disabledColor);
					cancelButton.Click += CancelPendingUpgrade_button_Click;
					cancelButton.Visible = true;
					p.Controls.Add(cancelButton);

					focusJumper.Add(cancelButton);

				}
				else if (canUpgradeHardware)
				{
					ImageTextButton button = new ImageTextButton(0);
					button.ButtonFont = MyDefaultSkinFontBold9;
					button.SetVariants(filename_long);
					button.Size = new Size(buttonwidth-10,20);
					button.Location = new Point(5,30); 
					button.Tag = appName;

					button.SetButtonText("Upgrade", upColor, upColor,	hoverColor, disabledColor);
					button.Enabled = playing;
					button.Click += AppUpgrade_button_Click;
					button.Visible = true;

					button.Enabled = playing;
					p.Controls.Add(button);
					focusJumper.Add(button);
				}
			}
			xpos += buttonwidth + border;

			// Keep the rigmarole of creating the cooling upgrade, but don't actually add it to our controls.
			if ((SkinningDefs.TheInstance.GetIntData("cooling_upgrade", 1) != 0) && ! AllZonesCoolingUpgraded())
			{
				//add a button 
				Panel cp = new Panel();
				cp.Size = new Size(buttonwidth,100);
				cp.Location = new Point(xpos,40-10);
				//p.BackColor = Color.LightGray;
				cp.BackColor = MyGroupPanelBackColor;
				//cp.BackColor = Color.Transparent;

				Label cl = new Label();
				cl.Text = "Cooling";
				cl.Font = MyDefaultSkinFontBold10;
				cl.Location = new Point(1,5);
				cl.Size = new Size(buttonwidth-2,20);
				cl.TextAlign = ContentAlignment.MiddleCenter;
				cp.Controls.Add(cl);

				if (PendingUpgradeByName.ContainsKey("Cooling"))
				{
					int time = (int)PendingUpgradeByName["Cooling"];

					Label ct = new Label();
					ct.Location = new Point(5,30);
					ct.Size = new Size(buttonwidth-10,40);
					ct.Text = "Upgrade due at " + BuildTimeString(time);
					ct.Font = MyDefaultSkinFontNormal8;
					cp.Controls.Add(ct);

					// Add a cancel button...
					ImageTextButton cancelButton = new ImageTextButton(0);
					cancelButton.ButtonFont = MyDefaultSkinFontBold9;
					cancelButton.SetVariants(filename_short);
					cancelButton.Size = new Size(buttonwidth-10,20);
					cancelButton.Location = new Point(5,75); 
					cancelButton.Tag = "Cooling";
					cancelButton.SetButtonText("Cancel", upColor, upColor, hoverColor, disabledColor);
					cancelButton.Click += CancelPendingUpgrade_button_Click;
					cancelButton.Visible = true;
					cp.Controls.Add(cancelButton);

					focusJumper.Add(cancelButton);
				}
				else 
				{
					if (! AllZonesCoolingUpgraded())
					{
						ImageTextButton button = new ImageTextButton(0);
						button.ButtonFont = MyDefaultSkinFontBold9;
						button.SetVariants(filename_short);
						button.Size = new Size(buttonwidth-10,20);
						button.Location = new Point(5,30); 
						button.Tag = "Cooling";
						button.SetButtonText("Upgrade",	upColor, upColor, hoverColor, disabledColor);
						button.Click += AppUpgrade_button_Click;
						button.Visible = true;
						cp.Controls.Add(button);

						focusJumper.Add(button);

						coolingButton = button;
					}
					xpos += buttonwidth + border;
				}
			}
		}

		public void PressCoolingButton ()
		{
			AppUpgrade_button_Click(coolingButton, null);
		}

		protected void shiftForZoneCoolerUpgrade()
		{
			if (timeLabel != null)
			{
				timeLabel.Location = new Point(178,25-20+40);
			}
			if (AutoTimeLabel != null)
			{
				AutoTimeLabel.Location = new Point(200, 50-20+40);
			}
			if (whenTextBox != null)
			{
				whenTextBox.Location = new Point(200, 50-20+40);
			}
			//Build the time controls 
			Label zoneLabel = new Label();
			zoneLabel.Text = "Upgrade Zone Cooling:";
			zoneLabel.Font = MyDefaultSkinFontNormal12;
			zoneLabel.BackColor = MyTextLabelBackColor;
			zoneLabel.ForeColor = LabelForeColor;
			zoneLabel.Size = new Size(190,20);
			zoneLabel.Visible = true;
			zoneLabel.TextAlign = ContentAlignment.MiddleLeft;
			zoneLabel.Location = new Point(178+160,25-20+40);
			chooseTimePanel.Controls.Add(zoneLabel);

			zoneTextBox = new EntryBox();
			zoneTextBox.DigitsOnly = true;
			zoneTextBox.Visible = true;
			zoneTextBox.Font = MyDefaultSkinFontNormal11;
			zoneTextBox.Size = new Size(80,30);
			zoneTextBox.MaxLength = 1;
			zoneTextBox.Text = "1";
			zoneTextBox.CharToIgnore('8');
			zoneTextBox.CharToIgnore('9');
			zoneTextBox.CharToIgnore('0');

			zoneTextBox.Location = new Point(200+160, 50-20+40);
			zoneTextBox.TextAlign = HorizontalAlignment.Center;
			zoneTextBox.ForeColor = LabelForeColor;
			chooseTimePanel.Controls.Add(zoneTextBox);
			zoneTextBox.GotFocus += zoneTextBox_GotFocus;
			zoneTextBox.LostFocus += zoneTextBox_LostFocus;
			zoneTextBox.KeyUp +=zoneTextBox_KeyUp;

			//Build the time controls 
			Label zoneStatusLabel = new Label();
			zoneStatusLabel.Text = "Allowed Upgrades: ";
			zoneStatusLabel.Font = MyDefaultSkinFontBold12;
			zoneStatusLabel.BackColor = MyTextLabelBackColor;
			zoneStatusLabel.ForeColor = LabelForeColor;
			zoneStatusLabel.Size = new Size(320,20);
			zoneStatusLabel.Visible = true;
			zoneStatusLabel.TextAlign = ContentAlignment.MiddleLeft;
			zoneStatusLabel.Location = new Point(178,25-20);
			chooseTimePanel.Controls.Add(zoneStatusLabel);

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


		/// <summary>
		/// Application selection 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		protected override void AppUpgrade_button_Click(object sender, EventArgs e)
		{
			ImageTextButton button = (ImageTextButton) sender;

			focusJumper.Dispose();
			focusJumper = new FocusJumper();

			chooseTimePanel.Visible = true;
			chooseTimePanel.BringToFront();

			//this.cancelButton.Focus();
			//this.whenTextBox.Visible = true;
			//this.timeLabel.Visible = true;
			okButton.Visible = true;
			cancelButton.SetButtonText("Cancel", upColor, upColor, hoverColor, disabledColor);

			selectedUpgrade = (string) button.Tag;
			title.Text = "Upgrade " + SkinningDefs.TheInstance.GetData("appname", "Application") + " > "+selectedUpgrade + " > Hardware";

			clearErrorMessage();

			//have we selected the Cooling Upgrade
			if (selectedUpgrade.ToLower().IndexOf("cool")>-1)
			{
				shiftForZoneCoolerUpgrade();
			}

			if (playing == false)
			{
				if (ManualTimeButton != null)
				{
					ManualTimeButton.Enabled = false;
					ManualTimeButton.SetButtonText("");
				}
				foreach(ImageTextButton itb in AutoTimeButtons)
				{
					itb.Enabled = false;
					itb.SetButtonText("");
				}
				whenTextBox.ReadOnly = true;

				zoneTextBox.Focus();			
				focusJumper.Add(zoneTextBox);
				focusJumper.Add(okButton);
				focusJumper.Add(cancelButton);
			}
			else
			{
				whenTextBox.Focus();
				focusJumper.Add(whenTextBox);
				focusJumper.Add(okButton);
				focusJumper.Add(cancelButton);
			}
		}

		protected void AppUpgradeFirmware_button_Click(object sender, EventArgs e)
		{
			ImageTextButton button = (ImageTextButton) sender;

			inFirmwareMode = true;

			focusJumper.Dispose();
			focusJumper = new FocusJumper();

			chooseTimePanel.Visible = true;
			chooseTimePanel.BringToFront();

			//this.cancelButton.Focus();
			//this.whenTextBox.Visible = true;
			//this.timeLabel.Visible = true;
			okButton.Visible = true;
			cancelButton.SetButtonText("Cancel", upColor, upColor, hoverColor, disabledColor);

			selectedUpgrade = (string) button.Tag;

			string text = selectedUpgrade;
			Node appNode = _Network.GetNamedNode(selectedUpgrade);
			if ((appNode != null) && appNode.GetBooleanAttribute("showByLocation", false))
			{
				text = appNode.GetAttribute("location");
			}

			title.Text = "Upgrade " + SkinningDefs.TheInstance.GetData("appname", "Application") + " > "+text + " > Firmware";

			clearErrorMessage();

			//have we selected the Cooling Upgrade
			if (selectedUpgrade.ToLower().IndexOf("cool")>-1)
			{
				shiftForZoneCoolerUpgrade();
			}

			if (playing == false)
			{
				if (ManualTimeButton != null)
				{
					ManualTimeButton.Enabled = false;
					ManualTimeButton.SetButtonText("");
				}
				foreach(ImageTextButton itb in AutoTimeButtons)
				{
					itb.Enabled = false;
					itb.SetButtonText("");
				}
				whenTextBox.ReadOnly = true;

				zoneTextBox.Focus();			
				focusJumper.Add(zoneTextBox);
				focusJumper.Add(okButton);
				focusJumper.Add(cancelButton);
			}
			else
			{
				whenTextBox.Focus();
				focusJumper.Add(whenTextBox);
				focusJumper.Add(okButton);
				focusJumper.Add(cancelButton);
			}
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

		protected bool ExtractZoneUpgradeStatusByName(string ZoneCoolerName)
		{
			bool zcs = false;
			Node ZoneCoolerNode = _Network.GetNamedNode(ZoneCoolerName);
			if (ZoneCoolerNode != null)
			{
				zcs = ZoneCoolerNode.GetBooleanAttribute("upgraded",false);
			}
			return zcs;
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
				Boolean canAppUpGrade;

				if (inFirmwareMode)
				{
					canAppUpGrade = app.GetBooleanAttribute("can_upgrade_firmware", false);
				}
				else
				{
					canAppUpGrade = app.GetBooleanAttribute("canupgrade", false);
				}

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
				string zoneCoolingNodeName = "C" + zoneTextBox.Text;
				if (ExtractZoneUpgradeStatusByName(zoneCoolingNodeName)==false)
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

				if (inFirmwareMode)
				{
					attrs.Add( new AttributeValuePair("upgrade_option","firmware") );
				}
				else
				{
					attrs.Add( new AttributeValuePair("upgrade_option","hardware") );
				}
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

		protected void CancelPendingFirmwareUpgrade_button_Click(object sender, EventArgs e)
		{
			ImageTextButton button = sender as ImageTextButton;
			
			string cancelled_app_name = (string)button.Tag;

			//Remove from request
			Node Found_Node = null;
			foreach (Node n1 in AppUpgradeQueueNode.getChildren())
			{
				string node_app_name = n1.GetAttribute("appname");
				if ((cancelled_app_name.ToLower() == node_app_name.ToLower()) && (n1.GetAttribute("upgrade_option").ToLower() == "firmware"))
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

		protected override void CancelPendingUpgrade_button_Click(object sender, EventArgs e)
		{
			ImageTextButton button = sender as ImageTextButton;
			
			string cancelled_app_name = (string)button.Tag;

			//Remove from request
			Node Found_Node = null;
			foreach (Node n1 in AppUpgradeQueueNode.getChildren())
			{
				string node_app_name = n1.GetAttribute("appname");
				if ((cancelled_app_name.ToLower() == node_app_name.ToLower()) && (n1.GetAttribute("upgrade_option").ToLower() == "hardware"))
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
	}
}