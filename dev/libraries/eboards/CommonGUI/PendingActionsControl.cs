using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;

using LibCore;
using Network;

using CoreUtils;
using TransitionObjects;

namespace CommonGUI
{
	/// <summary>
	/// Summary description for PendingActionsControl.
	/// </summary>
	public class PendingActionsControl : FlickerFreePanel
	{
		protected IDataEntryControlHolder _mainPanel;
		protected ImageTextButton cancelButton;
		protected Label title;
		protected Label errorlabel;
		protected NodeTree MyNodeTreeHandle;
		protected Node AppUpgradeQueueNode;
		protected Node ServerUpgradeQueueNode;
		protected Node projectIncomingRequestQueueHandle;
		protected int xoffset=0;
		protected int yoffset=0;
		protected ProjectManager _prjmanager;
		protected int CountPendingOPs = 0;
		protected int CountDisplayOPs = 0;

		protected ArrayList PendingAction_Times = new ArrayList();
		protected Hashtable PendingAction_DisplayStrings = new Hashtable();
		protected Hashtable PendingAction_Objects = new Hashtable();

		protected string filename_ok = "\\images\\buttons\\ok_blank_small.png";

		Boolean MyIsTrainingMode = false;

		protected FocusJumper focusJumper;

		//skin stuff
		protected Color MyGroupPanelBackColor;
		protected Color MyOperationsBackColor;
		protected Color MyTitleForeColor = Color.Black;
		protected Color DisplayTextForeColor = Color.Black;

		protected Font MyDefaultSkinFontNormal8 = null;
		protected Font MyDefaultSkinFontNormal10 = null;
		protected Font MyDefaultSkinFontBold10 = null;
		protected Font MyDefaultSkinFontBold12 = null;
		protected Font MyDefaultSkinFontBold24 = null;
		protected string install_title_name = "Install";
		protected string install_name = "install";
	    protected string install_pastname =  "installed";
		protected string install_name_multiple = "Installs";
		protected bool auto_translate = true;
	
		protected virtual string BuildTimeString(int timevalue)
		{
			int time_mins = timevalue / 60;
			int time_secs = timevalue % 60;
			string displaystr = CONVERT.ToStr(time_mins)+":";
			if (time_secs<10)
			{
				displaystr += "0";
			}
			displaystr += CONVERT.ToStr(time_secs);
			return displaystr;
		}

		protected virtual void BuildControls()
		{
			Color upColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_text_colour", Color.Black);
			Color downColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_down_text_colour", Color.White);
			Color hoverColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_hover_text_colour", Color.Green);
			Color disabledColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_disabled_text_colour", Color.DarkGray);


            string pendingUpgradesReplacementText = SkinningDefs.TheInstance.GetData("esm_pending_upgrades_title", "Pending Upgrades");
			//Create the Title 
		    title = new Label
		    {
		        Font = SkinningDefs.TheInstance.GetFont(12,
		            SkinningDefs.TheInstance.GetBoolData("ops_title_use_bold_font", true)
		                ? FontStyle.Bold : FontStyle.Regular),
		        Text = pendingUpgradesReplacementText + " and " + install_name_multiple,
		        TextAlign = ContentAlignment.MiddleLeft,
		        Size = new Size(330, 20),
		        BackColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("popup_title_background_colour",
		            MyOperationsBackColor),
		        ForeColor = MyTitleForeColor,
		        Location = new Point(0, 0)
		    };
		    Controls.Add(title);		

			string pending_popup_title = SkinningDefs.TheInstance.GetData("pending_incidents_popup_title");
			if (string.IsNullOrEmpty(pending_popup_title)==false)
			{
				title.Text = pending_popup_title;
			}

			Color ErrorTextColor = SkinningDefs.TheInstance.GetColorData("errortextcolor");

			//Create the Error Message
			errorlabel = new Label();
			errorlabel.Text = "";
			errorlabel.TextAlign = ContentAlignment.MiddleLeft;
			errorlabel.Size = new Size(350,30);
			errorlabel.BackColor = Color.Transparent;
			errorlabel.ForeColor = ErrorTextColor;
			errorlabel.Location = new Point(20,135);
			errorlabel.Visible = false;
			errorlabel.Font = MyDefaultSkinFontBold10;
			Controls.Add(errorlabel);	

			cancelButton = new StyledDynamicButtonCommon ("standard", "Close");
			cancelButton.Font = MyDefaultSkinFontBold10;
			cancelButton.Size = new Size(80,20);

			cancelButton.Location = SkinningDefs.TheInstance.GetPointData("ops_popup_cancel_button_position", 515, 150);

			cancelButton.Click += cancelButton_Click;
			cancelButton.Visible = true;
			Controls.Add(cancelButton);

			focusJumper.Add(cancelButton);
		}

		void AddPendingAction(int when, string DisplayString, object n1)
		{
			//add into hashtables
			PendingAction_Times.Add(when*100+CountPendingOPs);
			PendingAction_DisplayStrings.Add(when*100+CountPendingOPs, DisplayString);
			PendingAction_Objects.Add(when*100+CountPendingOPs, n1);
			CountPendingOPs++;
		}

		protected virtual void BuildButton(string DisplayString, object n1)
		{
			Color upColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_text_colour", Color.Black);
			Color downColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_down_text_colour", Color.White);
			Color hoverColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_hover_text_colour", Color.Green);
			Color disabledColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_disabled_text_colour", Color.DarkGray);

			if (CountDisplayOPs < 10)
			{
				Label text = new Label();
				text.Font = MyDefaultSkinFontNormal8;

				string translated_text = DisplayString;
				if (auto_translate)
				{
					translated_text = TextTranslator.TheInstance.Translate(translated_text);				
				}
				text.Text = translated_text;
				text.Size = new Size(235,20);
				text.Location = new Point(5+xoffset,yoffset+2);
				text.BackColor = MyOperationsBackColor;
				text.ForeColor = DisplayTextForeColor;

				//text.BackColor =Color.Cyan;
				Controls.Add(text);

				ImageTextButton button = new ImageTextButton(0);
				button.ButtonFont = MyDefaultSkinFontBold10;
				button.SetVariants(filename_ok);
				button.Size = new Size(60,20);
				button.Location = new Point(240+xoffset,yoffset);
				button.Tag = n1;
				button.SetButtonText("Cancel",upColor,upColor,hoverColor,disabledColor);
				button.Click += button_Click;
				button.Visible = true;
				Controls.Add(button);

				focusJumper.Add(button);

				yoffset += 25;
				CountDisplayOPs++;
				if (CountDisplayOPs==5)
				{
					xoffset = 300;
					yoffset = 30;
				}
			}
		}

		protected virtual void BuildDisplay()
		{
			if (PendingAction_Times.Count>0)
			{
				CountDisplayOPs = 0;
				PendingAction_Times.Sort();
				foreach (int whenvalue in PendingAction_Times)
				{
					string displayStr = string.Empty;
					object target = null;
					int buildcount=0;

					if (PendingAction_DisplayStrings.ContainsKey(whenvalue))
					{
						displayStr = (string)PendingAction_DisplayStrings[whenvalue];
						buildcount++;
					}
					if (PendingAction_Objects.ContainsKey(whenvalue))
					{
						target = (object)PendingAction_Objects[whenvalue];
						buildcount++;
					}
					if (buildcount==2)
					{
						BuildButton(displayStr , target);
					}
				}
			}
		}

		public PendingActionsControl(IDataEntryControlHolder mainPanel,	NodeTree model, 
			ProjectManager prjmanager, Boolean IsTrainingMode, Color OperationsBackColor, 
			Color GroupPanelBackColor)
		{
			focusJumper = new FocusJumper();

			install_title_name = SkinningDefs.TheInstance.GetData("pending_install_title","Install");
			install_name = SkinningDefs.TheInstance.GetData("pending_install_name","install");
			install_name_multiple = SkinningDefs.TheInstance.GetData("pending_install_multiple","Installs");
			install_pastname = SkinningDefs.TheInstance.GetData("pending_install_pastname","installed");
			
			
			MyOperationsBackColor = OperationsBackColor;
			MyGroupPanelBackColor = GroupPanelBackColor;
			DisplayTextForeColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("pending_action_text_colour", Color.Black);
			

			//Is there an overriding Title Foreground colour
			string racetitlecolour =  SkinningDefs.TheInstance.GetData("race_paneltitleforecolor");
			if (racetitlecolour != "")
			{
				MyTitleForeColor =  SkinningDefs.TheInstance.GetColorData("race_paneltitleforecolor");
			}

			string fontname =  SkinningDefs.TheInstance.GetData("fontname");
			if (auto_translate)
			{
				fontname = TextTranslator.TheInstance.GetTranslateFont(fontname);
			}
			MyDefaultSkinFontNormal8 = ConstantSizeFont.NewFont(fontname,8);
			MyDefaultSkinFontNormal10 = ConstantSizeFont.NewFont(fontname,10);
			MyDefaultSkinFontBold10 = ConstantSizeFont.NewFont(fontname,10,FontStyle.Bold);
			MyDefaultSkinFontBold12 = ConstantSizeFont.NewFont(fontname,12,FontStyle.Bold);
			MyDefaultSkinFontBold24 = ConstantSizeFont.NewFont(fontname,24,FontStyle.Bold);

			MyNodeTreeHandle = model;
			_mainPanel = mainPanel;
			_prjmanager = prjmanager;
			
			CountPendingOPs = 0;
			xoffset = 10;
			yoffset = 30;

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

			PendingAction_Times.Clear();
			PendingAction_DisplayStrings.Clear();
			PendingAction_Objects.Clear();

			projectIncomingRequestQueueHandle = MyNodeTreeHandle.GetNamedNode("ProjectsIncomingRequests");

			BuildControls();

            BuildPendingActions();

			BuildDisplay();

			GotFocus += PendingActionsControl_GotFocus;
			Resize += PendingActionsControl_Resize;
		}


        protected NodeTree GetNodeTree()
        {
            return MyNodeTreeHandle;
        }

        protected NodeTree BuildPendingActions()
        {
            ArrayList PrjRequests = _prjmanager.getPendingProjects();
            foreach (ProjectRunner pr in PrjRequests)
            {
                string display_string = pr.getInstallName();
                int when = pr.getWhentoInstall();
                display_string += " " + install_name + " ";
                display_string += BuildTimeString(when);
                ////int x = pr.getWhentoInstall()/60;
                ////display_string += x.ToString()+" minutes";
                //BuildButton(display_string, pr);
                AddPendingAction(when, display_string, pr);
            }

            AppUpgradeQueueNode = MyNodeTreeHandle.GetNamedNode("AppUpgradeQueue");
            ArrayList AppRequests = AppUpgradeQueueNode.getChildren();

            foreach (Node n1 in AppRequests)
            {
                string appname = n1.GetAttribute("appname");
                string whenvalue = n1.GetAttribute("when");
                int when_time = CONVERT.ParseInt(whenvalue);
                string display_string = "";

                string displayName = n1.GetAttribute("displayname");

                Node appNode = MyNodeTreeHandle.GetNamedNode(appname);
                if ((appNode != null) && appNode.GetBooleanAttribute("showByLocation", false))
                {
                    display_string = appNode.GetAttribute("location") + " at ";
                }
                else if (displayName != "")
                {
                    display_string = displayName + " at ";
                }
                else
                {
                    display_string = SkinningDefs.TheInstance.GetData("appname", "Application") + " (" + appname + ") at ";
                }
                display_string += BuildTimeString(when_time);
                //BuildButton(display_string, n1);
                AddPendingAction(when_time, display_string, n1);
            }

            ServerUpgradeQueueNode = MyNodeTreeHandle.GetNamedNode("MachineUpgradeQueue");
            ArrayList ServerRequests = ServerUpgradeQueueNode.getChildren();

            foreach (Node n1 in ServerRequests)
            {
                string st1 = n1.GetAttribute("name");
                string option = n1.GetAttribute("upgrade_option");
                string target = n1.GetAttribute("target");
                string whenvalue = n1.GetAttribute("when");
                int when_time = CONVERT.ParseInt(whenvalue);
                string upgrade_type = n1.GetAttribute("type");
                string st2 = n1.GetAttribute("name");

                string display_string = "";
                string optionReplacementText = string.Empty;

                if (SkinningDefs.TheInstance.GetBoolData("esm_sim", false))
                {
                    optionReplacementText = GetOptionReplacementText(option);
                }
                else
                {
                    optionReplacementText = option;
                }

                display_string = SkinningDefs.TheInstance.GetData("servername", "Server") + " (" + target + ") " + optionReplacementText + " at ";
                display_string += BuildTimeString(when_time);
                //BuildButton(display_string, n1);
                AddPendingAction(when_time, display_string, n1);
            }

            return MyNodeTreeHandle;
        }

	    string GetOptionReplacementText (string upgradeOption)
	    {
            string newOption = string.Empty;
	        switch (upgradeOption)
	        {
                case "memory":
	                newOption = SkinningDefs.TheInstance.GetData("esm_memory_label","memory");
	                break;
                case "storage":
                    newOption = SkinningDefs.TheInstance.GetData("esm_storage_label","storage");
                    break;
                case "hardware":
                    newOption = SkinningDefs.TheInstance.GetData("esm_hardware_label","hardware");
                    break;
                case "both":
                    newOption = SkinningDefs.TheInstance.GetData("esm_both_label", "both");
                    break;
                case "firmware":
                    newOption = SkinningDefs.TheInstance.GetData("esm_firmware_label", "firmware");
                    break;
                case "processor":
                    newOption = SkinningDefs.TheInstance.GetData("esm_processor_label", "processor");
                    break;
                default:
	                throw new Exception("Incorrect Upgrade option encountered. Most likely missing ");
	        }

            return newOption.ToLower();
	    }

		void setErrorMessage(string errmsg)
		{
			errorlabel.Text = errmsg;
			errorlabel.Visible = true;
		}

		void clearErrorMessage()
		{
			errorlabel.Visible = false;
		}

		void cancelButton_Click(object sender, EventArgs e)
		{
			_mainPanel.DisposeEntryPanel();
		}

		protected void button_Click(object sender, EventArgs e)
		{
			ImageTextButton active_button = (ImageTextButton) sender;
			
			object RequestObj = (object)active_button.Tag;

			if (RequestObj is ProjectRunner)
			{
				ProjectRunner pr = (ProjectRunner) RequestObj;
				string prdname = pr.getProductIdentifier();
				if (pr.isState_InstallOK()||pr.isState_InstallFail())
				{
					if (pr.isState_InstallOK())
					{
						setErrorMessage("Product ("+prdname+") "+install_pastname+" OK before cancellation ");
					}
					else
					{
						setErrorMessage("Product ("+prdname+") "+install_pastname+" and failed before cancellation ");
					}
				}
				else
				{
					ArrayList al = new ArrayList();
					al.Add( new AttributeValuePair("projectid", pr.getSipIdentifier()) );
					al.Add( new AttributeValuePair("when", pr.getWhentoInstall().ToString()));
					Node incident = new Node(projectIncomingRequestQueueHandle, "cancelbooking","", al);
					_mainPanel.DisposeEntryPanel();
				}
			}
			else
			{
				Node n1 = (Node)RequestObj;
				string target = n1.GetAttribute("appname");
				string opType = n1.GetAttribute("type");
				Boolean RemovePanel = false;

				if (opType.ToLower() =="upgrade_server")
				{
				
					//Remove from request
					Node Found_Node = null;
					foreach (Node node1 in ServerUpgradeQueueNode.getChildren())
					{
						string node_app_name = node1.GetAttribute("appname");
						if (target.ToLower() == node_app_name.ToLower())
						{
							Found_Node = node1;
						}
					}
					if (Found_Node != null)
					{
						Found_Node.Parent.DeleteChildTree(Found_Node);
						RemovePanel = true;
					}
					else
					{
						setErrorMessage("Application upgrade has already started.");
					}
				}

				if (opType.ToLower() =="app_upgrade")
				{
					//Remove from request
					Node Found_Node = null;
					foreach (Node node1 in AppUpgradeQueueNode.getChildren())
					{
						string node_app_name = node1.GetAttribute("appname");
						if (target.ToLower() == node_app_name.ToLower())
						{
							Found_Node = node1;
						}
					}
					if (Found_Node != null)
					{
						Found_Node.Parent.DeleteChildTree(Found_Node);
						RemovePanel = true;
					}
					else
					{
						setErrorMessage("Application upgrade has already started.");
					}
				}

				if (RemovePanel)
				{
					_mainPanel.DisposeEntryPanel();			
				}
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

		void PendingActionsControl_GotFocus(object sender, EventArgs e)
		{
			cancelButton.Focus();
		}

		void PendingActionsControl_Resize(object sender, EventArgs e)
		{
			if (string.IsNullOrEmpty(SkinningDefs.TheInstance.GetData("ops_popup_cancel_button_position")))
			{
				cancelButton.Location = new Point (Width - cancelButton.Width - 7, Height - cancelButton.Height - 10);
			}
		}
	}
}
