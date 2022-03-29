using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;

using LibCore;
using Network;

using CoreUtils;
using IncidentManagement;
using TransitionObjects;
using GameManagement;

namespace CommonGUI
{
	/// <summary>
	/// We have some addied functionality for the Install process 
	///  A, The Nevis services (SIPs 209 2nd Brake Pedal and 210 Composite Fuel Tank) which can be installed 
	///  B, Location display issues  
	/// </summary>

	public class InstallBusinessServiceControl_AOSE : InstallBusinessServiceControl
	{
		bool usePreferInteractionModel = false;
		bool showNevisServicesButton = false;
        NetworkProgressionGameFile gameFile;

		public InstallBusinessServiceControl_AOSE(IDataEntryControlHolder mainPanel, NodeTree model, int round, 
			ProjectManager prjmanager, Boolean IsTrainingMode, Color OperationsBackColor,
            Color GroupPanelBackColor, NetworkProgressionGameFile gameFile) :
			base(mainPanel, model, round, prjmanager, IsTrainingMode, OperationsBackColor, GroupPanelBackColor)
		{
            this.gameFile = gameFile;
		}

		protected override void BuildServicePanel()
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

			foreach (ProjectRunner pr in ReadyProjects)
			{
				string installname = pr.getInstallName();
				ReadyProjectsLookup.Add(installname, pr);
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
			foreach (string install_name in ReadyProjectNames)
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

			//determine whether we have installed 301 and 305 yet 
			bool BonnierExists = _Network.GetNamedNode("Bonnier") != null;
			bool JarierExists = _Network.GetNamedNode("Jarier") != null;

			//Is nevis already ordered 
			Node NevisOrderNode = _Network.GetNamedNode("nevis_ordered");
			bool NevisOrdered = NevisOrderNode.GetBooleanAttribute("value", false);

			if (current_round > 1)
			{
				if (NevisOrdered == false)
				{
					if ((BonnierExists == false) | ((JarierExists == false)))
					{
						showNevisServicesButton = true;
					}

					if (showNevisServicesButton)
					{
						ImageTextButton button = new ImageTextButton(0);
						button.ButtonFont = MyDefaultSkinFontBold10;
						button.SetVariants(filename_huge);
						button.Size = new Size(220, 20);
						button.Location = new Point(xoffset, yoffset);
						button.Tag = 99;
						button.SetButtonText("Nevis Services", upColor, upColor, hoverColor, disabledColor);
						button.Click += handle_Nevis_Click;
						button.Visible = true;
						disposeControls.Add(button);
						chooseServicePanel.Controls.Add(button);
						xoffset += button.Width + 5;
						buttonArray.Add(button);

						focusJumper.Add(button);

						++numOnRow;
						if (numOnRow == 2)
						{
							numOnRow = 0;
							xoffset = 5;
							yoffset += 25;
						}
					}
				}
			}
		}

		protected override void handle_Service_Click(object sender, EventArgs e)
		{
			ImageTextButton button = (ImageTextButton)sender;
			foreach (ImageTextButton b in buttonArray)
			{
				if (b == button)
				{
					ChosenServiceLabel.Text = button.GetButtonText();
					servicePicked = (ProjectRunner)b.Tag;
					ChosenServiceLabel.Hide();
					okButton.Enabled = true;

					string sip_identifier = servicePicked.getSipIdentifier();
					int sla_value = SLAManager.get_SLA(_Network, sip_identifier);
					sla_value = sla_value / 60; //convert from internally held secs to display mins
					sla_TimeBox.Text = CONVERT.ToStr(sla_value);

					string fixlocation = string.Empty;
					string fixzone = string.Empty;
					servicePicked.getFixedInformation(out fixlocation, out fixzone);
					
					servicePicked.getPreferedInformation(out usePreferInteractionModel);

			
					if (fixlocation != string.Empty)
					{
						sla_TimeBox.PrevControl = whenTextBox;
						whenTextBox.NextControl = sla_TimeBox;

						if (usePreferInteractionModel)
						{
							locationBox.Text = "";
							locationBox.Tag = fixlocation;
							locationBox.Enabled = true;
						}
						else
						{
							locationBox.Text = fixlocation;
							locationBox.Enabled = false;
						}
					}
					else
					{
						//handle the upgrade location 
						Boolean HandledbyUpgradeLocation = false;
						if (UpgradeLocationLookup.ContainsKey(sip_identifier))
						{
							string upgradelocation = (string)UpgradeLocationLookup[sip_identifier];
							if (upgradelocation != "")
							{
								HandledbyUpgradeLocation = true;
								sla_TimeBox.PrevControl = whenTextBox;
								whenTextBox.NextControl = sla_TimeBox;

								if (usePreferInteractionModel)
								{
									locationBox.Text = "";
									locationBox.Tag = upgradelocation;
									locationBox.Enabled = true;
								}
								else
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
							locationBox.Text = "";
							locationBox.Enabled = true;
						}
					}

					PanelTitleLabel.Width = 580;
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


		protected override void handle_OK_Click(object sender, EventArgs e)
		{
			Boolean proceed = true;
			string selected_sip = string.Empty;
			int requested_sla_time = 6;
			string msgbox_title = install_title_name + " Business Service Error";

			HideError();

			//===================================================================
			//==Check over the Provided Information (check for obvious probelms)
			//===================================================================
			if (locationBox.Text == string.Empty)
			{
				ShowError("Please provide an install location.");
				proceed = false;
			}
			else
			{
				//We should check that the location is a valid one to help facilitator 
				//Not sure about best method Getting node with location = "XXXX" from tree seems costly

				if (usePreferInteractionModel)
				{ 
					//we have stored ythe correct fixed location in the tag filed, compare and issue error if required 
					string required_location = (string) locationBox.Tag;
					string supplied_location = (string) locationBox.Text;

					if (required_location.Equals(supplied_location, StringComparison.InvariantCultureIgnoreCase)==false) 
					{
						ShowError("Incorrect Location Provided");
						proceed = false;
					}
				}
			}

			if ((sla_TimeBox.Text == string.Empty) & (proceed))
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
			if ((autoTimeSecs == 0) & (proceed))
			{
				if (whenTextBox.Text == string.Empty)
				{
                    ShowError("Please provide a valid " + install_title_name.ToLower() + " time.");
                    proceed = false;
				}
                else if (whenTextBox.Text == "19")
                {
					ShowError("Not enough time to complete operation.");
                    proceed = false;
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

                                if (proceed)
                                {
                                    int currentGameSecond = getCurrentSecond();
                                    if ((requestedmin * 60) < currentGameSecond)
                                    {
                                        int timeLeft_secs = (maxmins * 60 - currentGameSecond);
                                        if (timeLeft_secs < (time_taken_for_install * 60))
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
                        if (currentGameSecond > ((maxmins * 60) - SkinningDefs.TheInstance.GetIntData("ops_install_time", 120)))
                        {
							ShowError("Not enough time to complete operation.");
                            proceed = false;
                        }
                    }
                }
			}
			else
			{
				if (proceed == true)
				{
					if ((autoTimeSecs != 0))
					{
						//When using auto time, you can pick a time in the past 
						//this is executed at once but we may not have enough time left 
						int currentGameSecond = getCurrentSecond();
						if (currentGameSecond > ((maxmins * 60) - SkinningDefs.TheInstance.GetIntData("ops_install_time", 120)))
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
				int timeToFire = 0;

				if (autoTimeSecs == 0)
				{
					if (whenTextBox.Text != "Now")
					{
						whenInMin = CONVERT.ParseInt(whenTextBox.Text);
						//check if the time has passed 
						Node currentTimeNode = _Network.GetNamedNode("CurrentTime");
						int current_seconds = currentTimeNode.GetIntAttribute("seconds", 0);

						timeToFire = (whenInMin * 60) - current_seconds;
						//if it's passed ,just fire now 
						if (timeToFire < 0)
						{
							timeToFire = 0;
						}
						else
						{
							timeToFire = whenInMin * 60;
						}
					}
				}
				else
				{
					Node currentTimeNode = _Network.GetNamedNode("CurrentTime");
					int current_seconds = currentTimeNode.GetIntAttribute("seconds", 0);

					timeToFire = (autoTimeSecs) - current_seconds;
					//if it's passed ,just fire now 
					if (timeToFire < 0)
					{
						timeToFire = 0;
					}
					else
					{
						timeToFire = autoTimeSecs;
					}
				}

				ArrayList al = new ArrayList();
				al.Add(new AttributeValuePair("projectid", selected_sip));
				al.Add(new AttributeValuePair("installwhen", timeToFire.ToString()));
				al.Add(new AttributeValuePair("sla", CONVERT.ToStr(requested_sla_time)));
				al.Add(new AttributeValuePair("location", locationBox.Text));
				al.Add(new AttributeValuePair("type", "Install"));
				al.Add(new AttributeValuePair("phase", "operations"));
				//
				Node incident = new Node(projectIncomingRequestQueueHandle, "install", "", al);

				_mainPanel.DisposeEntryPanel();
			}
		}


		protected virtual void handle_Nevis_Click(object sender, EventArgs e)
		{
			ArrayList attrs = new ArrayList();

			//Is nevis already ordered 
			Node NevisOrderNode = _Network.GetNamedNode("nevis_ordered");
			bool NevisOrdered = NevisOrderNode.GetBooleanAttribute("value", false);

			Node CurrentTimeNode = _Network.GetNamedNode("CurrentTime");
			int seconds = CurrentTimeNode.GetIntAttribute("seconds",0);

			int round_length_mins = gameFile.GetRaceRoundLengthMins(20);
			int round_length_secs = round_length_mins * 60;
			int last_orders_time = round_length_secs - 65;

			if (seconds < last_orders_time)
			{
				if (NevisOrdered == false)
				{
					//Mark it as ordered
					NevisOrderNode.SetAttribute("value", "true");

					// Now install the two round 3 SIPs.
					Node installerNode = _Network.GetNamedNode("ProjectsIncomingRequests");
					string when = CONVERT.ToStr(1 + _Network.GetNamedNode("CurrentTime").GetIntAttribute("seconds", 0));

					attrs.Clear();
					attrs.Add(new AttributeValuePair("projectid", "301"));
					attrs.Add(new AttributeValuePair("installwhen", when));
					attrs.Add(new AttributeValuePair("sla", "166"));
					attrs.Add(new AttributeValuePair("location", "E555"));
					attrs.Add(new AttributeValuePair("type", "Install"));
					attrs.Add(new AttributeValuePair("phase", "operations"));
					new Node(installerNode, "install", "", attrs);

					attrs.Clear();
					attrs.Add(new AttributeValuePair("projectid", "305"));
					attrs.Add(new AttributeValuePair("installwhen", when));
					attrs.Add(new AttributeValuePair("sla", "166"));
					attrs.Add(new AttributeValuePair("location", "E556"));
					attrs.Add(new AttributeValuePair("type", "Install"));
					attrs.Add(new AttributeValuePair("phase", "operations"));
					new Node(installerNode, "install", "", attrs);

					_mainPanel.DisposeEntryPanel();
				}
				else
				{
					MessageBox.Show(TopLevelControl, "Nevis services already ordered.", "Nevis Order");
					_mainPanel.DisposeEntryPanel();
				}
			}
			else
			{
				MessageBox.Show(TopLevelControl, "Not enough time left for install.", "Nevis Order Invalid");
				_mainPanel.DisposeEntryPanel();
			}
		}
	}
}