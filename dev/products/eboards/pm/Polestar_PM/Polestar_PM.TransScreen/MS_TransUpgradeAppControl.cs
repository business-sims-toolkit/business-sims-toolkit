using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;

using LibCore;
using Network;

using CoreUtils;
using IncidentManagement;
using CommonGUI;
using TransitionScreens;

namespace Polestar_PM.TransScreen
{
	/// <summary>
	/// Summary description for UpgradeAppControl.
	/// </summary>
	public class MS_TransUpgradeAppControl : TransUpgradeAppControl
	{

		public MS_TransUpgradeAppControl(IDataEntryControlHolder mainPanel, IncidentApplier iApplier, NodeTree nt, 
			bool usingmins, Color OperationsBackColor, Color GroupPanelBackColor)
			:base (mainPanel, iApplier, nt, usingmins, OperationsBackColor, GroupPanelBackColor)
		{
		}

		public override void BuildScreenControls()
		{
			this.Width = 380;
			this.Height = 350;

			//Create the Title 
			title = new Label();
			title.Font = this.MyDefaultSkinFontBold12;
			title.Text = "Upgrade Application";
			title.TextAlign = ContentAlignment.MiddleLeft;
			title.Size = new Size(this.Width,20);
			title.BackColor = MyOperationsBackColor;
			title.Location = new Point(10,10);
			this.Controls.Add(title);		

			//Create the Error Message
			errorlabel = new Label();
			errorlabel.Text = "";
			errorlabel.TextAlign = ContentAlignment.MiddleCenter;
			errorlabel.Size = new Size(this.Width-60,20);
			errorlabel.BackColor = MyOperationsBackColor;
			errorlabel.Location = new Point(30,195+60+15);
			errorlabel.Visible = false;
			errorlabel.Font = this.MyDefaultSkinFontBold10;
			this.Controls.Add(errorlabel);	

			clearErrorMessage();

			okButton = new ImageTextButton(0);
			okButton.ButtonFont = this.MyDefaultSkinFontBold9;
			okButton.SetVariants(filename_short);
			okButton.Size = new Size(80,20);
			okButton.Location = new Point(195-10,320-15);
			okButton.SetButtonText("OK",
				System.Drawing.Color.Black,System.Drawing.Color.Black,
				System.Drawing.Color.Green,System.Drawing.Color.DarkGray);
			okButton.Click += new EventHandler(okButton_Click);
			okButton.Visible = false;
			this.Controls.Add(okButton);

			cancelButton = new ImageTextButton(0);
			cancelButton.ButtonFont = this.MyDefaultSkinFontBold9;
			cancelButton.SetVariants(filename_short);
			cancelButton.Size = new Size(80,20);
			cancelButton.Location = new Point(274,320-15);
			cancelButton.SetButtonText("Close",
				System.Drawing.Color.Black,System.Drawing.Color.Black,
				System.Drawing.Color.Green,System.Drawing.Color.DarkGray);
			cancelButton.Click += new EventHandler(cancelButton_Click);
			cancelButton.Visible = true;
			this.Controls.Add(cancelButton);

			//
			timeLabel = new Label();
			if(UsingMinutes)
			{
				timeLabel.Text = "Install On Min:";
			}
			else
			{
				timeLabel.Text = "Install On Day:";
			}
			timeLabel.Font = this.MyDefaultSkinFontNormal12;
			timeLabel.BackColor = MyOperationsBackColor;
			timeLabel.Size = new Size(130,20);
			timeLabel.Visible = false;
			timeLabel.TextAlign = ContentAlignment.MiddleLeft;
			timeLabel.Location = new Point(100,170+70+5);
			this.Controls.Add(timeLabel);
			//
			whenTextBox = new EntryBox();
			whenTextBox.DigitsOnly = true;
			whenTextBox.Visible = false;
			whenTextBox.Font = this.MyDefaultSkinFontNormal11;
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
			whenTextBox.Location = new Point(230, 165+70+5);
			whenTextBox.TextAlign = HorizontalAlignment.Center;
			this.Controls.Add(whenTextBox);
			whenTextBox.GotFocus += new EventHandler(whenTextBox_GotFocus);
			whenTextBox.LostFocus += new EventHandler(whenTextBox_LostFocus);
			whenTextBox.KeyUp +=new KeyEventHandler(whenTextBox_KeyUp);
		}


		protected override void RebuildUpgradeButtons(int xOffset, int yOffset)
		{
			int xpos = xOffset;
			int ypos = yOffset;

			focusJumper.Dispose();
			focusJumper = new FocusJumper();
			focusJumper.Add(this.okButton);
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

			//Extract the possible app upgrades and build the buttons 
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

			//Mind to add a button for the Cooling system upgrade
			count++;

			int NumberPerRow = 3;
			int PanelsCreated = 0;
			int panelwidth = this.Width;
			int border = 8;
			int panelusablewidth = panelwidth;
			//int buttonwidth = (panelwidth - ((count+1)*border)) / count;
			int buttonwidth = (panelwidth - ((NumberPerRow+1)*border)) / NumberPerRow;
			int panelheight = 90; //100;
			int panel_separation = 10;
			ypos = 40;

			foreach (string appname in AllowedAppSortList)
			{
				if (AllowedApps.ContainsKey(appname))
				{
					Node app = (Node)AllowedApps[appname];
					if (app != null)
					{
						Boolean canUserUpGrade = app.GetBooleanAttribute("userupgrade",false);
						Boolean canAppUpGrade = app.GetBooleanAttribute("canupgrade",false);

						PanelsCreated++;
						//add a button 
						Panel p = new Panel();
						p.Size = new Size(buttonwidth,panelheight);
						p.Location = new Point(xpos,ypos);
						//p.BackColor = Color.LightGray;
						p.BackColor = MyGroupPanelBackColor;
						this.Controls.Add(p);

						Label l = new Label();
						l.Text = appname;
						l.Font = this.MyDefaultSkinFontBold10;
						l.Location = new Point(1,5);
						l.Size = new Size(buttonwidth-2,20);
						l.TextAlign = ContentAlignment.MiddleCenter;
						p.Controls.Add(l);

						if (PendingUpgradeByName.ContainsKey(appname))
						{
							int time = (int)PendingUpgradeByName[appname];

							Label t = new Label();
							t.Location = new Point(10,30-5);
							t.Size = new Size(buttonwidth-20,40);
							t.Text = "Upgrade Booked on Day " + CONVERT.ToStr(time);
							t.Font = this.MyDefaultSkinFontNormal8;
							//t.BackColor = Color.Violet;
							t.TextAlign = ContentAlignment.MiddleCenter;
							p.Controls.Add(t);

							// Add a cancel button...

							ImageTextButton cancelButton = new ImageTextButton(0);
							cancelButton.ButtonFont = this.MyDefaultSkinFontBold9;
							cancelButton.SetVariants(filename_short);
							cancelButton.Size = new Size(buttonwidth-10,20);
							cancelButton.Location = new Point(5,75-5); 
							cancelButton.Tag = appname;
							cancelButton.SetButtonText("Cancel",
								System.Drawing.Color.Black,System.Drawing.Color.Black,
								System.Drawing.Color.Green,System.Drawing.Color.DarkGray);
							cancelButton.Click += new EventHandler(CancelPendingUpgrade_button_Click);
							cancelButton.Visible = true;
							p.Controls.Add(cancelButton);

							focusJumper.Add(cancelButton);
						}
						else if (canAppUpGrade)
						{
							ImageTextButton button = new ImageTextButton(0);
							button.ButtonFont = this.MyDefaultSkinFontBold9;
							button.SetVariants(filename_short);
							button.Size = new Size(buttonwidth-10,20);
							button.Location = new Point(5,30); 
							button.Tag = appname;
							button.SetButtonText("Upgrade",
								System.Drawing.Color.Black,System.Drawing.Color.Black,
								System.Drawing.Color.Green,System.Drawing.Color.DarkGray);
							button.Click += new EventHandler(AppUpgrade_button_Click);
							button.Visible = true;
							p.Controls.Add(button);

							focusJumper.Add(button);
						}
						xpos += buttonwidth + border;
						if ((xpos > panelwidth)|(xpos + buttonwidth + border > panelwidth))
						{
							xpos =  xOffset;
							ypos += panelheight + panel_separation;
						}
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
				cp.Location = new Point(xpos,ypos);
				//cp.BackColor = Color.LightGray;
				cp.BackColor = MyGroupPanelBackColor;
				this.Controls.Add(cp);

				Label cl = new Label();
				cl.Text = "Cooling";
				cl.Font = this.MyDefaultSkinFontBold10;
				cl.Location = new Point(1,5);
				//cl.BackColor = Color.Aqua;
				cl.TextAlign = ContentAlignment.MiddleCenter;
				cl.Size = new Size(buttonwidth-2,20);
				cp.Controls.Add(cl);

				if (PendingUpgradeByName.ContainsKey("Cooling"))
				{
					int time = (int)PendingUpgradeByName["Cooling"];

					Label ct = new Label();
					ct.Font = this.MyDefaultSkinFontNormal8;
					ct.Location = new Point(5,30);
					ct.Size = new Size(buttonwidth-10,40);
					ct.Text = "Upgrade due on day " + CONVERT.ToStr(time);
					ct.TextAlign = ContentAlignment.MiddleCenter;
					cp.Controls.Add(ct);

					// Add a cancel button...
					ImageTextButton cancelButton = new ImageTextButton(0);
					cancelButton.ButtonFont = this.MyDefaultSkinFontBold9;
					cancelButton.SetVariants(filename_short);
					cancelButton.Size = new Size(buttonwidth-10,20);
					cancelButton.Location = new Point(5,75-5); 
					cancelButton.Tag = "Cooling";
					cancelButton.SetButtonText("Cancel",
						System.Drawing.Color.Black,System.Drawing.Color.Black,
						System.Drawing.Color.Green,System.Drawing.Color.DarkGray);
					cancelButton.Click += new EventHandler(CancelPendingUpgrade_button_Click);
					cancelButton.Visible = true;
					cp.Controls.Add(cancelButton);

					focusJumper.Add(cancelButton);
				}
				else 
				{
					if (CoolingSystemUpgraded == false)
					{
						ImageTextButton button = new ImageTextButton(0);
						button.ButtonFont = this.MyDefaultSkinFontBold9;
						button.SetVariants(filename_short);
						button.Size = new Size(buttonwidth-10,20);
						button.Location = new Point(5,30); 
						button.Tag = "Cooling";
						button.SetButtonText("Upgrade",
							System.Drawing.Color.Black,System.Drawing.Color.Black,
							System.Drawing.Color.Green,System.Drawing.Color.DarkGray);
						button.Click += new EventHandler(AppUpgrade_button_Click);
						button.Visible = true;
						cp.Controls.Add(button);

						focusJumper.Add(button);

					}
					xpos += buttonwidth + border;
					if ((xpos > panelwidth)|(xpos + buttonwidth + border > panelwidth))
					{
						xpos =  border;
						ypos += panelheight + panel_separation + 10;
					}
				}
			}

			//move the 



		}

	}
}



