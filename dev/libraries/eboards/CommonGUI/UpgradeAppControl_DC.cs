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
	public class UpgradeAppControl_DC : UpgradeAppControl
	{
		EntryBox zoneTextBox = null;
		ImageTextButton coolingButton = null;

		protected Hashtable PendingUpgradeTimesByType = new Hashtable ();

		public UpgradeAppControl_DC(IDataEntryControlHolder mainPanel, IncidentApplier iApplier, NodeTree nt, 
			bool usingmins, Boolean IsTrainingMode, Color OperationsBackColor, Color GroupPanelBackColor, 
			bool _playing):base
			(mainPanel, iApplier, nt, usingmins, IsTrainingMode, OperationsBackColor, GroupPanelBackColor, _playing)
		{
			//override the standard overall cooling system 
			CoolingSystemUpgraded = false;
		}

		public bool AllZonesCoolingUpgraded ()
		{
			for (int zone = 1; zone <= 7; zone++)
			{
				Node zoneNode = _Network.GetNamedNode("Zone" + CONVERT.ToStr(zone));
				if (((zoneNode == null) || zoneNode.GetBooleanAttribute("activated", false)) && ! ExtractZoneUpgradeStatusByNumber(zone))
				{
					return false;
				}
			}

			return true;
		}

		protected override void RebuildUpgradeButtons(int xOffset, int yOffset)
		{
			focusJumper.Dispose();
			focusJumper = new FocusJumper();
			focusJumper.Add(okButton);
			focusJumper.Add(this.cancelButton);

			//Extract the possisble app upgrades and build the buttons 
			PendingUpgradeTimesByType.Clear();
			PendingUpgradeByName.Clear();
			ArrayList existingkids = AppUpgradeQueueNode.getChildren();
			foreach (Node kid in existingkids)
			{
				string pendingAppName = kid.GetAttribute("appname");
				int pendingAppTime = kid.GetIntAttribute("when",0);
				string type = kid.GetAttribute("upgrade_option");

				if (! PendingUpgradeTimesByType.ContainsKey(type))
				{
					PendingUpgradeTimesByType.Add(type, new Hashtable ());
				}
				PendingUpgradeByName = PendingUpgradeTimesByType[type] as Hashtable;

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
				bool canFirmwareUpgrade = app.GetBooleanAttribute("can_upgrade_firmware", false);

				string appname = app.GetAttribute("name");

				if(!appname.EndsWith("(M)"))
				{
					if ((canAppUpGrade && canUserUpGrade) || canFirmwareUpgrade)
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

			int panelwidth = Width;
			int border = 10;
			int panelusablewidth = panelwidth;
			int buttonwidth = (panelwidth - (count+1)*border) / Math.Min(6, Math.Max(3, count));

			int xpos = xOffset + 50;
			int ypos = 50;
			int column = 0;

			foreach (string appname in AllowedAppSortList)
			{
				if (AllowedApps.ContainsKey(appname))
				{
					Node app = (Node)AllowedApps[appname];
					if (app != null)
					{
						Boolean canUserUpGrade = app.GetBooleanAttribute("userupgrade",false);
						Boolean canAppUpGrade = app.GetBooleanAttribute("canupgrade",false);
						bool canFirmwareUpgrade = app.GetBooleanAttribute("can_upgrade_firmware", false);

						ImageTextButton button = new ImageTextButton (0);
						button.SetVariants(filename_short);
						button.Tag = app;
						button.ButtonFont = MyDefaultSkinFontBold9;

						string text = appname;
						Node appNode = _Network.GetNamedNode(appname);
						if ((appNode != null) && appNode.GetBooleanAttribute("showByLocation", false))
						{
							text = appNode.GetAttribute("location");
						}

						button.SetButtonText(text, upColor, upColor, hoverColor, disabledColor);
						button.Click += button_Click;
						button.Location = new Point (xpos, ypos);
						button.Size = new Size (buttonwidth, 20);
						focusJumper.Add(button);
						Controls.Add(button);

						xpos += buttonwidth + border;
						column++;
						if (column >= 5)
						{
							column = 0;
							xpos = xOffset + 50;
							ypos += 40;
						}
					}
				}
			}

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
					cancelButton.Size = new Size(80,20);
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
			if (coolingButton != null)
			{
				AppUpgrade_button_Click(coolingButton, null);
			}
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
			zoneLabel.Text = "Upgrade Zone CRAC:";
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
				Node zoneNode = _Network.GetNamedNode("Zone" + CONVERT.ToStr(1 + step));
				//mind that zones start with c1 not c0
				if (((zoneNode == null) || (zoneNode.GetBooleanAttribute("activated", false))) && ! ExtractZoneUpgradeStatusByNumber(step+1))
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

			//this.cancelButton.Focus();
			//this.whenTextBox.Visible = true;
			//this.timeLabel.Visible = true;
			okButton.Visible = true;
			cancelButton.SetButtonText("Cancel", upColor, upColor, hoverColor, disabledColor);

			selectedUpgrade = (string) button.Tag;
			string name;
			if (selectedUpgrade == "Cooling")
			{
				name = "CRAC";
			}
			else
			{
				name = selectedUpgrade;
			}

			title.Text = "Upgrade " + SkinningDefs.TheInstance.GetData("appname", "Application") + " > "+name;

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
			bool zoneDisabled = false;
			string Desc = "";

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
				string zoneCoolingNodeName = "C" + zoneTextBox.Text;

				AppName = "Zone Cooling " + zoneTextBox.Text;
				Desc = "CRAC Zone " + zoneTextBox.Text;

				if (ExtractZoneUpgradeStatusByName(zoneCoolingNodeName) == false)
				{
					Node zoneNode = _Network.GetNamedNode("Zone" + zoneTextBox.Text);
					if ((zoneNode == null) || (zoneNode.GetBooleanAttribute("activated", true)))
					{
						proceed = true;
					}
					else
					{
						proceed = false;
						zoneDisabled = true;
					}
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
				if (Desc != "")
				{
					attrs.Add( new AttributeValuePair("displayname", Desc) );
				}
				Node newEvent = new Node(AppUpgradeQueueNode, "app_upgrade", "", attrs);
			}
			else
			{
				string showName = AppName;
				if (Desc != "")
				{
					showName = Desc;
				}

				if (zoneDisabled)
				{
					setErrorMessage("Zone Not Active");
				}
				else if (pending_exists)
				{
					setErrorMessage("" + showName + " upgrade already requested");
				}
				else
				{
					setErrorMessage("" + showName + " already upgraded");
				}
			}
			return proceed;
		}

		void button_Click (object sender, EventArgs e)
		{
			ImageTextButton button = sender as ImageTextButton;
			Node app = button.Tag as Node;

			UpgradeAppFirmHardwareControl panel = new UpgradeAppFirmHardwareControl (_mainPanel, _iApplier, _Network, UsingMinutes, false, MyOperationsBackColor, MyGroupPanelBackColor, playing, app.GetAttribute("name"));
			panel.SetMaxMins(maxmins);
			SuspendLayout();
			panel.Size = Size;
			Controls.Add(panel);
			panel.BringToFront();
			ResumeLayout(false);
		}
	}
}