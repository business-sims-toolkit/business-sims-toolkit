using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;

using LibCore;
using Network;

using CoreUtils;
using CommonGUI;
using TransitionObjects;

using StyledDynamicButton = ResizingUi.Button.StyledDynamicButton;

namespace TransitionScreens
{
	/// <summary>
	/// The StartSIP allows th facilitator to start off a SIP.
	/// </summary>
	public class StartSIP : FlickerFreePanel
	{
		protected EntryBox productNumber;
		protected EntryBox platformID;

		protected ImageTextButton okButton;
		protected ImageTextButton cancelButton;
		protected NodeTree _network;
		protected int _current_round = 1;

		Label header;
		protected Label error;

		protected IDataEntryControlHolder _tcp;

		protected Node projectIncomingRequestQueueHandle;

		protected string filename_long = "\\images\\buttons\\blank_big.png";
		protected string filename_mid = "\\images\\buttons\\blank_med.png";
		protected string filename_short = "\\images\\buttons\\blank_small.png";
		protected string filename_ok = "\\images\\buttons\\OK_blank_small.png";

		protected FocusJumper focusJumper;

		protected Color MyOperationsBackColor;
		protected Font MyDefaultSkinFontNormal12 = null;
		protected Font MyDefaultSkinFontNormal10 = null;
		protected Font MyDefaultSkinFontBold12 = null;
		protected Font MyDefaultSkinFontBold10 = null;
		protected Font MyDefaultSkinFontBold9 = null;
		public Color errorLabel_foreColor = Color.Red;

		/// <summary>
		/// Allows a user to Start a Service Improvement Project
		/// </summary>
		/// <param name="tcp"></param>
		/// <param name="nt"></param>
		/// <param name="Round"></param>
		public StartSIP(IDataEntryControlHolder tcp, NodeTree nt, int Round, Color OperationsBackColor)
		{
			MyOperationsBackColor = OperationsBackColor;

			string errmsg_overridecolor = SkinningDefs.TheInstance.GetData("race_errormsg_override_color");
			if (errmsg_overridecolor != "")
			{
				errorLabel_foreColor = SkinningDefs.TheInstance.GetColorData("race_errormsg_override_color");
			}

			SuspendLayout();
			string fontname =  SkinningDefs.TheInstance.GetData("fontname");
			MyDefaultSkinFontNormal12 = ConstantSizeFont.NewFont(fontname,12);
			MyDefaultSkinFontNormal10 = ConstantSizeFont.NewFont(fontname,10);
			MyDefaultSkinFontBold9 = ConstantSizeFont.NewFont(fontname,9,FontStyle.Bold);
			MyDefaultSkinFontBold10 = ConstantSizeFont.NewFont(fontname,10,FontStyle.Bold);
			MyDefaultSkinFontBold12 = ConstantSizeFont.NewFont(fontname,12,FontStyle.Bold);
			
			focusJumper = new FocusJumper();
			_tcp = tcp;
			_current_round = Round;
			_network = nt;
			projectIncomingRequestQueueHandle = nt.GetNamedNode("ProjectsIncomingRequests");

			SetStyle(ControlStyles.Selectable, true);
			//
			BackColor = MyOperationsBackColor;
			BorderStyle = BorderStyle.None;//Fixed3D;

			BuildScreenControls();

			ResumeLayout(false);

			focusJumper.Add(okButton);
			focusJumper.Add(cancelButton);

			platformID.PrevControl = productNumber;
			platformID.NextControl = okButton;

			GotFocus += StartSIP_GotFocus;

			DoSize();
		}


		public virtual void BuildScreenControls()
		{
			Color upColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_text_colour", Color.Black);
			Color downColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_down_text_colour", Color.White);
			Color hoverColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_hover_text_colour", Color.Green);
			Color disabledColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_disabled_text_colour", Color.DarkGray);

			header = new Label();
			header.Text = "Add Product";
			header.Font = MyDefaultSkinFontBold12;
			header.TextAlign = ContentAlignment.MiddleLeft;
			header.Location = new Point(0,0);
			header.Size = new Size(150,25);
            header.ForeColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("transition_text_colour", Color.Black);
			Controls.Add(header);

			error = new Label();
			error.ForeColor = errorLabel_foreColor;
			error.Font = MyDefaultSkinFontBold12; 
			error.Location = new Point(10,110);
			error.TextAlign = ContentAlignment.MiddleCenter;
			error.Size = new Size(500,20);
			Controls.Add(error);

			Label platform_label = new Label();
			platform_label.Text = "Platform";
			platform_label.Size = new Size(90,20);
			platform_label.Font = MyDefaultSkinFontBold12;
			platform_label.Location = new Point(310-45,58);
            platform_label.ForeColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("transition_text_colour", Color.Black);
            Controls.Add(platform_label);

			platformID = new EntryBox();
			platformID.TextAlign = HorizontalAlignment.Center;
			platformID.Location = new Point(310-45,75+5);
			platformID.Size = new Size(80,30);
			platformID.Font = MyDefaultSkinFontNormal12;
			platformID.GotFocus += platformID_GotFocus;
			platformID.numChars = 1;
			platformID.CharIsShortFor('1','X');
			platformID.CharIsShortFor('2','Y');
			platformID.CharIsShortFor('3','Z');
			platformID.CharIsShortFor('x','X');
			platformID.CharIsShortFor('y','Y');
			platformID.CharIsShortFor('z','Z');
			Controls.Add(platformID);
		
			Label product_label = new Label();
			product_label.Text = "Product";
			product_label.Size = new Size(90,20);
			product_label.Font = MyDefaultSkinFontBold12;
			product_label.Location = new Point(225-45,58);
			Controls.Add(product_label);
            product_label.ForeColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("transition_text_colour", Color.Black);

			productNumber = new EntryBox();
			productNumber.TextAlign = HorizontalAlignment.Center;
			productNumber.Location = new Point(225-45,75+5);
			productNumber.Size = new Size(80,30);
			productNumber.Font = MyDefaultSkinFontNormal12;
			productNumber.GotFocus += productNumber_GotFocus;
			productNumber.DigitsOnly = true;
			productNumber.AcceptsReturn = false;
			productNumber.KeyPress += productNumber_KeyPress;
			Controls.Add(productNumber);

			okButton = new StyledDynamicButton ("standard", "OK");
			okButton.Font = MyDefaultSkinFontBold9;
			okButton.Size = new Size(80,20);
			okButton.Location = new Point(350,180);
			okButton.Click += okButton_Click;
			Controls.Add(okButton);

			cancelButton = new StyledDynamicButton("standard", "Cancel");
			cancelButton.Font = MyDefaultSkinFontBold9;
			cancelButton.Size = new Size(80,20);
			cancelButton.Location = new Point(445,180);
			cancelButton.Click += cancelButton_Click;
			Controls.Add(cancelButton);
		}

		protected void StartSIP_GotFocus(object sender, EventArgs e)
		{
			//sipNumber.Focus();
			productNumber.Focus();
		}
		
		/// <summary>
		/// Handling the OK Button 
		/// Added very simple checks for mis-typing data 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		protected void okButton_Click(object sender, EventArgs e)
		{
			ArrayList helper_al = null;
			Boolean SIPNotFound = true;
			Boolean ProductNotFound = true;
			Boolean PlatformNotFound = true;
			string installaction = string.Empty;
			// Collect and check the data entered by the facilitator...
			//Old system, user enters sip, product and platform
			//int sip = Number(sipNumber.Text);
			//int product = Number(productNumber.Text);
			//new system, user enters product and platform, we derive the sip from the product entered

			string ProductNumberEntered = productNumber.Text.Trim();
			string PlatformEntered = platformID.Text.Trim();

			if ((ProductNumberEntered == string.Empty)||((PlatformEntered == string.Empty)))
			{
				if (PlatformEntered == string.Empty)
				{
					error.Text = "Please provide a valid platform.";
				}
				if (ProductNumberEntered == string.Empty)
				{
					error.Text = "Please provide a valid product.";
				}
			}
			else
			{
				int product = CONVERT.ParseInt(ProductNumberEntered);
				int sip = product / 10;

				if ((sip / 100) != _current_round)
				{
					error.Text = " Invalid product specified. Please enter a valid product.";
					productNumber.Focus();
				}
				else if(! SkinningDefs.TheInstance.GetData("allowed_platforms", "XYZ").Contains(PlatformEntered))
				{
					error.Text = " Invalid platform specified. Please enter a valid platform.";
					platformID.Focus();
				}
				else
				{
					Boolean check = ProjectSIP_Repository.TheInstance.getSIP_Data(CONVERT.ToStr(sip),
						CONVERT.ToStr(product),	PlatformEntered, out helper_al, out installaction, 
						out SIPNotFound, out ProductNotFound, out PlatformNotFound);
				
					if ((check==true) && (SIPNotFound==false) && (ProductNotFound==false) && (PlatformNotFound==false))
					{
						ArrayList al = new ArrayList();
						if(BuildAVPs(sip, product, PlatformEntered, out al))
						{
							al.Add(new AttributeValuePair("type","Create") );
							Node incident = new Node(projectIncomingRequestQueueHandle, "Create","", al);
						}
						_tcp.DisposeEntryPanel();
					}
					else
					{
						string errormessage = string.Empty;
						if ((check)||(SIPNotFound)||(ProductNotFound))
						{
							errormessage = "Please enter a valid product.";
						}
						else
						{
							errormessage = "Please enter a valid platform.";
						}
						error.Text = errormessage;
					}
				}
			}
		}

		protected Boolean BuildAVPs(int ProjectNumber, int ProductNumber, string PlatformStr, out ArrayList Attrs)
		{
			Attrs = new ArrayList();
			
			Attrs.Add(new AttributeValuePair("projectid", CONVERT.ToStr(ProjectNumber)) );
			Attrs.Add(new AttributeValuePair("productid", CONVERT.ToStr(ProductNumber)) );
			Attrs.Add(new AttributeValuePair("platformid", PlatformStr) );

			return true;
		}

		protected void cancelButton_Click(object sender, EventArgs e)
		{
			_tcp.DisposeEntryPanel();
		}

		protected void productNumber_GotFocus(object sender, EventArgs e)
		{
			productNumber.SelectionStart = 0;
			productNumber.SelectionLength = productNumber.Text.Length;
		}

		protected void platformID_GotFocus(object sender, EventArgs e)
		{
			platformID.SelectionStart = 0;
			platformID.SelectionLength = platformID.Text.Length;
		}

		protected void productNumber_KeyPress(object sender, KeyPressEventArgs e)
		{
			string product_str = productNumber.Text;

			bool deleting = (e.KeyChar == 8);
			// Only catches Backspace, but this event doesn't trigger when Delete is pressed anyway.

			if(e.KeyChar > 47 && e.KeyChar < 59)
				product_str += (char)e.KeyChar;

			int product = (product_str != "") ? CONVERT.ParseInt(product_str) : 0;
			int sip = product / 10;

			//Have we entered a full product code 
			if((product_str.Length == 4) && ! deleting)
			{
				string fixedlocation = string.Empty;
				string fixedzone = string.Empty;
				string flo= string.Empty;
				string PreviousAppName = string.Empty;

				//check if the sip has defined a platform loookup 
				Boolean platformLookup = ProjectSIP_Repository.TheInstance.getSIP_CheckforLookupPlatform(sip, out PreviousAppName);
				Boolean AllowingUserPlatform =false;

				if (platformLookup)
				{
					//We may have already installed the rpeevious version of this Application 
					//if we have installed it then we need to force the platform to match 
					if (PreviousAppName != null)
					{
						//we have a defined app that could have been installed, so try to find it  
						Node InstalledApp = _network.GetNamedNode(PreviousAppName);
						if (InstalledApp != null)
						{
							Node Server = InstalledApp.Parent;
							if (Server != null)
							{
								//We need to use the platform of the server that the previous version sits on 
								string serverZone = Server.GetAttribute("platform");
								platformID.Text = serverZone;
								platformID.ReadOnly = true;
								platformID.Enabled = false;
								okButton.Focus();
							}
							else
							{
								AllowingUserPlatform =true;
							}
						}
						else
						{
							AllowingUserPlatform =true;
						}
					}
					else
					{
						AllowingUserPlatform =true;
					}
				}
				else
				{
					ProjectSIP_Repository.TheInstance.getSIP_DataForFixedLocation(sip, out fixedlocation, out fixedzone);
					if (fixedzone != string.Empty)
					{
						platformID.Text = fixedzone;
						platformID.ReadOnly = true;
						platformID.Enabled = false;
						okButton.Focus();
					}
					else
					{
						AllowingUserPlatform = true;
					}
				}
				//
				if (AllowingUserPlatform)
				{
					platformID.Text = "";
					platformID.ReadOnly = false;
					platformID.Enabled = true;
					platformID.Focus();
				}
			}
			else
			{
				platformID.Text = "";
				platformID.ReadOnly = false;
				platformID.Enabled = true;
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

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);
			DoSize();
		}

		void DoSize ()
		{
			int instep = 20;
			cancelButton.Location = new Point (Width - instep - cancelButton.Width, Height - instep - cancelButton.Height);
			okButton.Location = new Point (cancelButton.Left - instep - okButton.Width, cancelButton.Top);

			header.Bounds = new Rectangle (0, 0, Width, 25);
			header.BackColor = SkinningDefs.TheInstance.GetColorData("popup_title_background_colour", Color.White);
			header.ForeColor = SkinningDefs.TheInstance.GetColorData("popup_title_foreground_colour", Color.Black);
		}
	}
}