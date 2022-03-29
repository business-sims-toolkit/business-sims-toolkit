using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;

using LibCore;
using Network;
using IncidentManagement;
using CoreUtils;
using CommonGUI;

using StyledDynamicButton = ResizingUi.Button.StyledDynamicButton;

namespace TransitionScreens
{
	/// <summary>
	/// Summary description for InstallSIP.
	/// </summary>
	public class InstallSIP : FlickerFreePanel
	{
		protected EntryBox productNumber;
		protected EntryBox locationBox;
		protected EntryBox dayBox;
		protected EntryBox slaNumber;

		protected NodeTree _network;
		protected int _current_round = 1;

		protected Label errorDisplayText;
		protected Label header;
		protected Label location_title;
		protected Label product_label;

		protected IDataEntryControlHolder _tcp;

		protected ImageTextButton okButton;
		protected ImageTextButton cancelButton;

		protected Node projectIncomingRequestQueueHandle;
		protected Node currentProjectsHandle;
		protected Hashtable CurrentProjectLookup = new Hashtable();
		protected Hashtable UpgradeLocationLookup = new Hashtable();

		protected string filename_long = "\\images\\buttons\\blank_big.png";
		protected string filename_mid = "\\images\\buttons\\blank_med.png";
		protected string filename_short = "\\images\\buttons\\blank_small.png";
		protected string filename_ok = "\\images\\buttons\\OK_blank_small.png";

		protected FocusJumper focusJumper;

		protected Color MyOperationsBackColor;
		protected Font MyDefaultSkinFontBold12 = null;
		protected Font MyDefaultSkinFontBold11 = null;
		protected Font MyDefaultSkinFontBold10 = null;
		protected Font MyDefaultSkinFontBold9 = null;
		protected Font MyDefaultSkinFontBold8 = null;
		protected Font MyDefaultSkinFontNormal10 = null;
		protected Font MyDefaultSkinFontNormal12 = null;
		
		protected string install_title_name = "Install";
		protected string install_name = "install";
		public Color errorLabel_foreColor = Color.Red;

		/// <summary>
		/// Pick a day / location to install a sip to
		/// </summary>
		/// <param name="tcp"></param>
		/// <param name="nt"></param>
		/// <param name="round"></param>
		public InstallSIP(IDataEntryControlHolder tcp, NodeTree nt, int round, Color OperationsBackColor)
		{
			MyOperationsBackColor = OperationsBackColor;
			string fontname =  SkinningDefs.TheInstance.GetData("fontname");
			MyDefaultSkinFontNormal10 = ConstantSizeFont.NewFont(fontname,10);
			MyDefaultSkinFontNormal12 = ConstantSizeFont.NewFont(fontname,12);
			MyDefaultSkinFontBold8 = ConstantSizeFont.NewFont(fontname,8,FontStyle.Bold);
			MyDefaultSkinFontBold9 = ConstantSizeFont.NewFont(fontname,9,FontStyle.Bold);
			MyDefaultSkinFontBold10 = ConstantSizeFont.NewFont(fontname,10,FontStyle.Bold);
			MyDefaultSkinFontBold11 = ConstantSizeFont.NewFont(fontname,11,FontStyle.Bold);
			MyDefaultSkinFontBold12 = ConstantSizeFont.NewFont(fontname,12,FontStyle.Bold);

			string errmsg_overridecolor = SkinningDefs.TheInstance.GetData("race_errormsg_override_color");
			if (errmsg_overridecolor != "")
			{
				errorLabel_foreColor = SkinningDefs.TheInstance.GetColorData("race_errormsg_override_color");
			}

			SuspendLayout();

			focusJumper = new FocusJumper();

			_tcp = tcp;
			_network = nt;
			_current_round = round;
			BorderStyle = BorderStyle.None;//.Fixed3D;
			projectIncomingRequestQueueHandle = nt.GetNamedNode("ProjectsIncomingRequests");
			currentProjectsHandle = nt.GetNamedNode("Projects");
			Build_ActiveSIPList();
			Build_UpgradeLocationList();
			SetStyle(ControlStyles.Selectable, true);
			//
			BackColor = MyOperationsBackColor;

			install_title_name = SkinningDefs.TheInstance.GetData("transition_install_title","Install");
			install_name = SkinningDefs.TheInstance.GetData("transition_install_name","install");

			BuildScreenControls();

			GotFocus += StartSIP_GotFocus;

			productNumber.NextControl = locationBox;
			locationBox.PrevControl = productNumber;
			dayBox.NextControl = slaNumber;
			slaNumber.NextControl = okButton;

			focusJumper.Add(okButton);
			focusJumper.Add(cancelButton);

			ResumeLayout(false);
		}

		public virtual void BuildScreenControls()
		{
			Color upColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_text_colour", Color.Black);
			Color downColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_down_text_colour", Color.White);
			Color hoverColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_hover_text_colour", Color.Green);
			Color disabledColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_disabled_text_colour", Color.DarkGray);

			errorDisplayText = new Label();
			errorDisplayText.Text = "";
			errorDisplayText.Size = new Size(470,25);
			errorDisplayText.Font = MyDefaultSkinFontBold10;
			errorDisplayText.TextAlign = ContentAlignment.MiddleLeft;
			errorDisplayText.ForeColor = errorLabel_foreColor;
			errorDisplayText.Location = new Point(30,110);
			errorDisplayText.Visible = false;
			Controls.Add(errorDisplayText);

			header = new Label();
			header.Text = "Install Product";
			header.Size = new Size(200,25);
			header.Font = MyDefaultSkinFontBold12;
			header.TextAlign = ContentAlignment.MiddleLeft;
			header.Location = new Point(10,10);
            header.ForeColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("transition_text_colour", Color.Black);
            Controls.Add(header);

			Label day_label = new Label();
			day_label.Text = "Day";
			day_label.Size = new Size(50,15);
			day_label.Location = new Point(260,55);
			day_label.Font = MyDefaultSkinFontBold10;
            day_label.ForeColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("transition_text_colour", Color.Black);
            Controls.Add(day_label);

			dayBox = new EntryBox();
			dayBox.TextAlign = HorizontalAlignment.Center;
			dayBox.Location = new Point(260,75);
			dayBox.Size = new Size(80,30);
			dayBox.Font = MyDefaultSkinFontNormal12;
			dayBox.numChars = 2;
			dayBox.DigitsOnly = true;
			dayBox.GotFocus +=dayBox_GotFocus;
			Controls.Add(dayBox);

			location_title = new Label();
			location_title.Text = "Location";
			location_title.Size = new Size(90,15);
			location_title.Location = new Point(175,55);
			location_title.Font = MyDefaultSkinFontBold10;
            location_title.ForeColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("transition_text_colour", Color.Black);
            Controls.Add(location_title);

			locationBox = new EntryBox();
			locationBox.TextAlign = HorizontalAlignment.Center;
			locationBox.Location = new Point(175,75);
			locationBox.Size = new Size(80,30);
			locationBox.Font = MyDefaultSkinFontNormal12;
			locationBox.NextControl = dayBox;
			locationBox.numChars = 4;
			locationBox.DigitsOnly = false;
			locationBox.GotFocus +=locationBox_GotFocus;
			Controls.Add(locationBox);

			dayBox.PrevControl = locationBox;

			product_label = new Label();
			product_label.Text = "Product";
			product_label.Size = new Size(80,15);
			product_label.Location = new Point(90,55);
			product_label.GotFocus +=product_label_GotFocus;
			product_label.Font = MyDefaultSkinFontBold10;
            product_label.ForeColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("transition_text_colour", Color.Black);
            Controls.Add(product_label);

			productNumber = new EntryBox();
			productNumber.TextAlign = HorizontalAlignment.Center;
			productNumber.Location = new Point(90,75);
			productNumber.Size = new Size(80,30);
			productNumber.Font = MyDefaultSkinFontNormal12;
			productNumber.numChars = 4;
			productNumber.KeyPress += productNumber_KeyPress;
			productNumber.KeyUp += productNumber_KeyUp;
			productNumber.GotFocus +=productNumber_GotFocus;
			productNumber.DigitsOnly = true;
			Controls.Add(productNumber);

			Label sla_label = new Label();
			sla_label.Text = "MTRS";
			sla_label.Size = new Size(50,15);
			sla_label.Location = new Point(345,55);
			sla_label.Font =  MyDefaultSkinFontBold10;
			Controls.Add(sla_label);

			slaNumber = new EntryBox();
			slaNumber.TextAlign = HorizontalAlignment.Center;
			slaNumber.Location = new Point(345,75);
			slaNumber.KeyPress += slaNumber_KeyPress;
			slaNumber.Size = new Size(80,30);
			slaNumber.Font = MyDefaultSkinFontNormal12;
			slaNumber.DigitsOnly = true;
			slaNumber.MouseUp += slaNumber_MouseUp;
			slaNumber.GotFocus +=slaNumber_GotFocus;
			Controls.Add(slaNumber);


			if (SkinningDefs.TheInstance.GetBoolData("use_impact_based_slas", false))
            {
                sla_label.Visible = false;
                slaNumber.Visible = false;
            }

			okButton = new StyledDynamicButton ("standard", "OK");
			okButton.Font = MyDefaultSkinFontBold9;
			okButton.Size = new Size(80,20);
			okButton.Location = new Point(350,180);
			okButton.Click += okButton_Click;
			Controls.Add(okButton);

			cancelButton = new StyledDynamicButton ("standard", "Cancel");
			cancelButton.Font = MyDefaultSkinFontBold9;
			cancelButton.Size = new Size(80,20);
			cancelButton.Location = new Point(445,180);
			cancelButton.Click += cancelButton_Click;
			Controls.Add(cancelButton);
		}

		protected void Build_ActiveSIPList()
		{
			CurrentProjectLookup.Clear();
			//Need to build the active projects 
			ArrayList existing_projects = currentProjectsHandle.getChildren();
			foreach (Node activeProject in existing_projects)
			{
				string projectid = activeProject.GetAttribute("name");
				string project_fixedlocation = activeProject.GetAttribute("fixedlocation");
				CurrentProjectLookup.Add(projectid,project_fixedlocation);
			}
		}

		protected void Build_UpgradeLocationList()
		{
			UpgradeLocationLookup.Clear();
			//Need to build the active projects 
			ArrayList existing_projects = currentProjectsHandle.getChildren();
			foreach (Node activeProject in existing_projects)
			{
				string projectid = activeProject.GetAttribute("name");
				string upgradeName = activeProject.GetAttribute("upgradename");
				if (upgradeName != "")
				{
					Node nn = _network.GetNamedNode(upgradeName);
					if (nn != null)
					{
						string location = nn.GetAttribute("location");
						if (location != "")
						{
							UpgradeLocationLookup.Add(projectid, location);
						}
					}
				}
			}
		}		

		protected void StartSIP_GotFocus(object sender, EventArgs e)
		{
			//sipNumber.Focus();
			productNumber.Focus();
		}

		protected void SetErrorDisplayText(string errmsg)
		{
			errorDisplayText.Text = errmsg;
			errorDisplayText.Visible = true;
		}

		protected void ClearErrorDisplayText()
		{
			errorDisplayText.Visible = false;
		}

		protected Boolean BuildAVPs(int ProjectNumber, int ProductNumber, string PlatformStr, out ArrayList Attrs)
		{
			Attrs = new ArrayList();
			
			Attrs.Add(new AttributeValuePair("projectid", ProjectNumber) );
			Attrs.Add(new AttributeValuePair("productid", ProductNumber) );
			Attrs.Add(new AttributeValuePair("platformid", PlatformStr) );

			return true;
		}

		protected int GetToday()
		{
			Node currentDayNode = _network.GetNamedNode("CurrentDay");
			int CurrentDay = currentDayNode.GetIntAttribute("day",0);
			return CurrentDay;
		}

		/// <summary>
		/// This is used to prewarn the user that the day is booked
		/// </summary>
		/// <param name="day"></param>
		/// <returns></returns>
		protected Boolean IsDayFreeInCalendar(int day)
		{
			Node CalendarNode = _network.GetNamedNode("Calendar");
			//Need to iterate over children 
			foreach(Node calendarEvent in CalendarNode.getChildren())
			{
				int cday = calendarEvent.GetIntAttribute("day",0);
				string block = calendarEvent.GetAttribute("block");
				if (day == cday)
				{
					if (block.ToLower() == "true")
					{
						return false;
					}
				}
			}
			//
			return true;
		}

		protected bool IsDayFreeInCalendar (int day, string productId)
		{
			Node calendarNode = _network.GetNamedNode("Calendar");

			foreach (Node dayNode in calendarNode.getChildren())
			{
				if (dayNode.GetIntAttribute("day", 0) == day)
				{
					bool block = dayNode.GetBooleanAttribute("block", false);
					string dayProductId = dayNode.GetAttribute("productid");

					if (block && (dayProductId != productId))
					{
						return false;
					}
				}
			}

			return true;
		}


		protected void OutputError(string errorText)
		{
			Node errorsNode = _network.GetNamedNode("FacilitatorNotifiedErrors");
			Node error = new Node(errorsNode, "error", "", new AttributeValuePair( "text", errorText ) );
		}

		protected void productNumber_KeyUp(object sender, KeyEventArgs e)
		{
		}

		protected void dayBox_GotFocus(object sender, EventArgs e)
		{
			//this.ClearErrorDisplayText();
		}

		protected void locationBox_GotFocus(object sender, EventArgs e)
		{
			//this.ClearErrorDisplayText();
		}

		protected void product_label_GotFocus(object sender, EventArgs e)
		{
			//this.ClearErrorDisplayText();
		}

		protected void productNumber_GotFocus(object sender, EventArgs e)
		{
			//this.ClearErrorDisplayText();
		}

		protected void slaNumber_GotFocus(object sender, EventArgs e)
		{
			//this.ClearErrorDisplayText();
		}

		protected void slaNumber_MouseUp(object sender, MouseEventArgs e)
		{
			slaNumber.SelectAll();
		}

		protected void slaNumber_KeyPress(object sender, KeyPressEventArgs e)
		{
			char keyData = e.KeyChar;

			if(keyData == (char)Keys.D1)
				slaNumber.Text = "1";
			else if(keyData == (char)Keys.D2)
				slaNumber.Text = "2";
			else if(keyData == (char)Keys.D3)
				slaNumber.Text = "3";
			else if(keyData == (char)Keys.D4)
				slaNumber.Text = "4";
			else if(keyData == (char)Keys.D5)
				slaNumber.Text = "5";
			else if(keyData == (char)Keys.D6)
				slaNumber.Text = "6";
			else if(keyData == (char)Keys.D7)
				slaNumber.Text = "7";
			else if(keyData == (char)Keys.D8)
				slaNumber.Text = "8";
			else if(keyData == (char)Keys.D9)
				slaNumber.Text = "9";

			if(keyData == (char)Keys.Delete || keyData == (char)Keys.Back)
			{
				if(slaNumber.Text == "")
				{
					dayBox.Focus();
					dayBox.Text = dayBox.Text.Substring(0,1);
					dayBox.SelectionStart = 1;
				}
				slaNumber.Text = "";
			}
			else
			{
				if(slaNumber.Text != "")
					okButton.Focus();
			}

			e.Handled = true;
		}

		//upgraded to look for pure white space 
		protected Boolean CheckForEmptyDataField(EntryBox eb)
		{
			Boolean Check = false;
			if (eb.Text == null)
			{
				Check = true;
			}
			else
			{
				if (eb.Text == string.Empty)
				{
					Check = true;
				}
				else
				{
					if (eb.Text.Trim() == string.Empty)
					{
						Check = true;
					}
				}
			}
			return Check;
		}
		
		protected Boolean CheckForEmptyData()
		{
			Boolean EmptyProduct = CheckForEmptyDataField(productNumber);
			Boolean EmptyLocation = CheckForEmptyDataField(locationBox);
			Boolean EmptyDay = CheckForEmptyDataField(dayBox);
			Boolean EmptySLA = slaNumber.Visible && CheckForEmptyDataField(slaNumber);
			Boolean overall = false;

			if ((EmptyProduct)||(EmptyLocation)||(EmptyDay)||(EmptySLA))
			{
				if (EmptySLA)
				{
					errorDisplayText.Text = "MTRS value must be between 1 and 9 minutes.";
					errorDisplayText.Visible = true;
					overall = true;
				}
				if (EmptyDay)
				{
					errorDisplayText.Text = "Please enter a valid " + install_title_name.ToLower() + " day.";
					errorDisplayText.Visible = true;
					overall = true;
				}
				if (EmptyLocation)
				{
					errorDisplayText.Text = "Please enter a valid " + install_title_name.ToLower() + " location.";
					errorDisplayText.Visible = true;
					overall = true;
				}
				if (EmptyProduct)
				{
					errorDisplayText.Text = "Please enter a valid product.";
					errorDisplayText.Visible = true;
					overall = true;
				}
			}
			return overall;
		}


		protected void okButton_Click(object sender, EventArgs e)
		{
			Boolean proceed = true;
			string requested_location = string.Empty;
			int requested_day = 0;
			int requested_sla = 0;
			int product = 0;
			int sip = 0;
			string ProductNumberEntered = ""; 
			
			if (CheckForEmptyData()==false)
			{
				//===============================================================
				//==Perfrom some Quick checks that supplied information is good  
				//===============================================================
				//Check we have a Valid Product
				ProductNumberEntered = productNumber.Text.Trim();
				product = CONVERT.ParseInt(ProductNumberEntered);
				sip = product / 10;

				if ((sip / 100) != _current_round)
				{
					proceed = false;
					errorDisplayText.Text = "Invalid product specified. Please enter a valid product.";
					errorDisplayText.Visible = true;
				}
				else
				{
					if(!SLAManager.is_SIP(_network,CONVERT.ToStr(sip)))
					{
						proceed = false;
						errorDisplayText.Text = "Invalid product specified. Please enter a valid product.";
						errorDisplayText.Visible = true;
					}
				}
			}
			else
			{
				proceed = false;
			}
			//Check that we have a valid location 
			//TODO needs to be extended to identify that location exists 
			if (proceed)
			{
				requested_location = locationBox.Text.Trim();
				if (requested_location.Length != 4)
				{
					proceed = false;
					errorDisplayText.Text = "Please provide a valid " + install_title_name.ToLower() + " location.";
					errorDisplayText.Visible = true;
				}
			}

			//Check that we have a valid day
			if (proceed)
			{
				requested_day = 0;
				if ((dayBox.Text.ToLower()=="next day")|(dayBox.Text.ToLower()=="now"))
				{
					requested_day = 0;
					int CheckDay = GetToday();

					if (IsDayFreeInCalendar(CheckDay, CONVERT.ToStr(product))==false)
					{
						proceed = false;
						errorDisplayText.Visible = true;
						errorDisplayText.Text = "Specified day is already booked. Please enter a valid day.";
					}
				}
				else
				{
					if (dayBox.Text != string.Empty)
					{
						requested_day = CONVERT.ParseInt(dayBox.Text);
						if (requested_day <= GetToday())
						{
							proceed = false;
							errorDisplayText.Visible = true;
							errorDisplayText.Text = "Specified day is in the past. Please enter a valid day.";
						}
					}
					else
					{
						requested_day = -1; 
						proceed = false;
						errorDisplayText.Visible = true;
						errorDisplayText.Text = "Invalid day, please enter valid day";
					}
				}
				if (proceed)
				{
					int CalendarLength = _network.GetNamedNode("Calendar").GetIntAttribute("days", -1);

					if ((requested_day<1)||(requested_day>(CalendarLength-1)))
					{
						proceed = false;
						errorDisplayText.Visible = true;
						errorDisplayText.Text = "Specified day is outwith the round. Please enter a valid day.";
					}
					else
					{
						if (requested_day>0)
						{
							if (IsDayFreeInCalendar(requested_day, CONVERT.ToStr(product)) == false)
							{
								proceed = false;
								errorDisplayText.Visible = true;
								errorDisplayText.Text = "Specified day is already booked. Please enter a valid day.";
							}
						}
					}
				}
			}
			if (proceed)
			{			
				//Check we have a Valid SLA Value  
				requested_sla = 0;
				requested_sla = CONVERT.ParseInt(slaNumber.Text);
				if ((requested_sla<1)|(requested_sla>9))
				{
					proceed = false;
					errorDisplayText.Visible = true;
					errorDisplayText.Text = "Invalid SLA, please enter valid SLA";
				}
			}

			//===============================================================
			//==Perfrom some Quick checks that supplied information is good  
			//===============================================================
			if (proceed)
			{			
				ArrayList al = new ArrayList();
				al.Add( new AttributeValuePair("projectid", sip) );
				al.Add( new AttributeValuePair("productid",CONVERT.ToStr(product)));
				al.Add( new AttributeValuePair("installwhen", CONVERT.ToStr(requested_day) ) );
				al.Add( new AttributeValuePair("sla", CONVERT.ToStr(requested_sla)) );
				al.Add( new AttributeValuePair("location", locationBox.Text) );
				al.Add( new AttributeValuePair("type", "Install") );
				al.Add( new AttributeValuePair("phase", "transition") );

				Node incident = new Node(projectIncomingRequestQueueHandle, "install","", al);
				
				_tcp.DisposeEntryPanel();
			}
		}

		protected void cancelButton_Click(object sender, EventArgs e)
		{
			_tcp.DisposeEntryPanel();
		}

		protected void productNumber_KeyPress(object sender, KeyPressEventArgs e)
		{
			string product_str = productNumber.Text;

			if(e.KeyChar > 47 && e.KeyChar < 59)
				product_str += (char)e.KeyChar;

			int product = (product_str != "") ? CONVERT.ParseInt(product_str) : 0;
			int sip = product / 10;

			if(product_str.Length == 4)
			{
				if(SLAManager.is_SIP(_network,CONVERT.ToStr(sip)))
				{
					//Setup the SLA value 
					slaNumber.Text = CONVERT.ToStr((SLAManager.get_SLA(_network,CONVERT.ToStr(sip)) / 60));
					//
					if (CurrentProjectLookup.ContainsKey(sip.ToString()))
					{
						string expected_location = (string) CurrentProjectLookup[sip.ToString()];
						if (expected_location != "")
						{
							locationBox.Text = expected_location;
							locationBox.Enabled = false;
							productNumber.NextControl = dayBox;
							dayBox.Focus();
							dayBox.PrevControl = productNumber;
						}
						else
						{
							if (UpgradeLocationLookup.ContainsKey(sip.ToString()))
							{
								string upgrade_location = (string) UpgradeLocationLookup[sip.ToString()];
								locationBox.Text = upgrade_location;
								locationBox.Enabled = false;
								productNumber.NextControl = dayBox;
								dayBox.Focus();
								dayBox.PrevControl = productNumber;
							}
							else
							{
								locationBox.Text = "";
								locationBox.Enabled = true;
								productNumber.NextControl = locationBox;
								dayBox.PrevControl = locationBox;
							}
						
						}
					}
				}
				else
				{
                    errorDisplayText.Text = "Please click on 'Add' to initiate the product before attempting to " + install_title_name.ToLower() + ".";

					errorDisplayText.Visible = true;

					e.Handled = true;
					productNumber.Text = "";
					productNumber.Focus();
				}
			}
		}
	
		protected override void Dispose(bool disposing)
		{
			if( disposing )
			{
				focusJumper.Dispose();
			}
			base.Dispose (disposing);
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);
			DoSize();
		}

		void DoSize ()
		{
			int instep = 20;
			cancelButton.Location = new Point(Width - instep - cancelButton.Width, Height - instep - cancelButton.Height);
			okButton.Location = new Point(cancelButton.Left - instep - okButton.Width, cancelButton.Top);

			header.Bounds = new Rectangle(0, 0, Width, 25);
			header.BackColor = SkinningDefs.TheInstance.GetColorData("popup_title_background_colour", Color.White);
			header.ForeColor = SkinningDefs.TheInstance.GetColorData("popup_title_foreground_colour", Color.Black);

		}
	}
}