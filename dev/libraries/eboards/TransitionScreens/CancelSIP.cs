using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using LibCore;
using Network;
using CoreUtils;
using CommonGUI;

using StyledDynamicButton = ResizingUi.Button.StyledDynamicButton;

namespace TransitionScreens
{
	/// <summary>
	/// This is the Cancel SIP Operations Panel
	/// It allows the entire SIP to be cancelled or just the install day for that SIP
	/// 
	/// This needs to be refactored to use common methods from ProjectManager and ProjectRunner
	/// These Project Objects should contain all the important methods and status information 
	/// Currently we have business rules in 2 places which we need to rationalise
	/// </summary>
	public class CancelSIP : FlickerFreePanel
	{
		//The non cancellable stages of a project
		protected const string AttrName_Stage_HANDOVER = "handover";
		protected const string AttrName_Stage_READY= "ready";
		protected const string AttrName_Stage_INSTALL_OK= "installed_ok";
		protected const string AttrName_Stage_INSTALL_FAIL= "installed_fail";

		protected NodeTree _network;
		protected IDataEntryControlHolder _tcp;
		protected int _current_round = 1;
		protected ImageTextButton cancelButton;

		protected string filename_long = "\\images\\buttons\\blank_big.png";
		protected string filename_mid = "\\images\\buttons\\blank_med.png";
		protected string filename_short = "\\images\\buttons\\blank_small.png";
		protected string filename_ok = "\\images\\buttons\\OK_blank_small.png";

		protected Label ErrMsgDisplay = null;
		
		protected Node projectIncomingRequestQueueHandle;

		protected FocusJumper focusJumper;

		protected Color MyOperationsBackColor;
		protected Font MyDefaultSkinFontBold12 = null;
		protected Font MyDefaultSkinFontBold11 = null;
		protected Font MyDefaultSkinFontBold10 = null;
		protected Font MyDefaultSkinFontBold9 = null;
		protected Font MyDefaultSkinFontNormal10 = null;
		protected int CancelProductButtonWidth = 180;
		protected int CancelProductButtonOffsetX = 10;
		protected int CancelInstallButtonWidth = 290;
		protected int CancelInstallButtonOffsetX = 180;
		protected int ButtonGap = 55;
		protected Boolean UseShortInstallMsg = false;
		public Color errorLabel_foreColor = Color.Red;

		Label header, header2;
		List<Control> leftButtons;
		List<Control> rightButtons;

		protected class CancelProject
		{
			public string id;
			public Node project;

			public CancelProject(string id, Node project)
			{
				this.id = id;
				this.project = project;
			}
		}

		protected class CancelBooking
		{
			public string id;
			public int day;
			public Node project;


			public CancelBooking(string id, int day, Node projectInvolved)
			{
				this.id = id;
				this.day = day;
				project = projectInvolved;
			}
		}

		/// <summary>
		/// Cancel a Service Improvement Project
		/// </summary>
		/// <param name="tcp"></param>
		/// <param name="nt"></param>
		public CancelSIP(IDataEntryControlHolder tcp, NodeTree nt, int current_round, Color OperationsBackColor)
		{
			MyOperationsBackColor = OperationsBackColor;
			string fontname =  SkinningDefs.TheInstance.GetData("fontname");
			MyDefaultSkinFontNormal10 = ConstantSizeFont.NewFont(fontname,10);
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
			_current_round = current_round;

			projectIncomingRequestQueueHandle = nt.GetNamedNode("ProjectsIncomingRequests");
			
			SetStyle(ControlStyles.Selectable, true);
			BorderStyle = BorderStyle.None;//.Fixed3D;
			BackColor = MyOperationsBackColor;

			BuildScreenControls();

			focusJumper.Add(cancelButton);

			ResumeLayout(false);

			leftButtons = new List<Control> ();
			rightButtons = new List<Control> ();

			BuildCancelProjects();

			GotFocus += CancelSIP_GotFocus;

			DoSize();
		}

		public virtual void BuildScreenControls()
		{
			Color upColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_text_colour", Color.Black);
			Color downColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_down_text_colour", Color.White);
			Color hoverColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_hover_text_colour", Color.Green);
			Color disabledColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_disabled_text_colour", Color.DarkGray);

			header = new Label();
			header.Text = "Cancel Product";
			header.Size = new Size(200,25);
			header.Font = MyDefaultSkinFontBold12;
			header.Location = new Point(0,0);
			header.TextAlign = ContentAlignment.MiddleLeft;
            header.ForeColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("transition_text_colour", Color.Black);
            Controls.Add(header);

			header2 = new Label();
			header2.Text = "Cancel Install";
			header2.Size = new Size(200,25);
			header2.Font = MyDefaultSkinFontBold12;
			header2.TextAlign = ContentAlignment.MiddleLeft;
			header2.Location = new Point(235,10);
            header2.ForeColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("transition_text_colour", Color.Black);
            Controls.Add(header2);

			cancelButton = new StyledDynamicButton ("standard", "Close");
			cancelButton.Font = MyDefaultSkinFontBold9;
			cancelButton.Size = new Size(80,20);
			cancelButton.Location = new Point(445,180);
			cancelButton.Click += cancelButton_Click;
			Controls.Add(cancelButton);
		}

		protected void BuildCancelProjects()
		{
			Color upColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_text_colour", Color.Black);
			Color downColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_down_text_colour", Color.White);
			Color hoverColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_hover_text_colour", Color.Green);
			Color disabledColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_disabled_text_colour", Color.DarkGray);

			SuspendLayout();

			int y = 40;
			Node projects = _network.GetNamedNode("projects");
			Node calendar = _network.GetNamedNode("Calendar");

			foreach(Node project in projects)
			{
				string id = project.GetAttribute("productid");
				string stage = project.GetAttribute("stage");

				int project_round = project.GetIntAttribute("round",1);

				if (project_round == _current_round)
				{
					//are we in the early stages, can we still cancell
					Boolean cancellAllowed = true;
					bool hasAButton = false;

					stage = stage.ToLower();
					if ((stage==AttrName_Stage_HANDOVER)|(stage == AttrName_Stage_READY)|
						(stage==AttrName_Stage_INSTALL_OK)|(stage == AttrName_Stage_INSTALL_FAIL))
					{
						cancellAllowed = false;
					}

					if (cancellAllowed)
					{
						string disp = "Cancel product "+id;

						ImageTextButton cancel = new StyledDynamicButton ("standard", disp);
						cancel.Font = MyDefaultSkinFontBold9;
						cancel.Tag = new CancelProject(project.GetAttribute("projectid"),project);
						cancel.Size = new Size(CancelProductButtonWidth ,25);
						cancel.Location = new Point(CancelProductButtonOffsetX,y);
						cancel.Click += cancel_Click;
						Controls.Add(cancel);
						leftButtons.Add(cancel);

						hasAButton = true;

						focusJumper.Add(cancel);
					}

					// : Fix for 3606 (Cancel install disappears once development is complete)
					if (stage != AttrName_Stage_INSTALL_OK)
					{
						foreach (Node calendarNode in calendar)
						{
							if ((calendarNode.GetAttribute("type").ToLower() == "install")
								&& (calendarNode.GetAttribute("productid") == id)
								&& (calendarNode.GetAttribute("status").ToLower() == "active"))
							{
								BuildCancelInstall(project.GetAttribute("projectid"),y, project);
								hasAButton = true;
							}
						}
					}

					if (hasAButton)
					{
						y += 30;
					}
				}
			}

			ResumeLayout(false);
		}

		protected void BuildCancelInstall(string projId, int y, Node projectInvolved)
		{
			Color upColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_text_colour", Color.Black);
			Color downColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_down_text_colour", Color.White);
			Color hoverColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_hover_text_colour", Color.Green);
			Color disabledColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_disabled_text_colour", Color.DarkGray);

			SuspendLayout();

			Node CalendarNode = _network.GetNamedNode("Calendar");
			int installDay = 0;
			
			// Pick the latest install, if any
			foreach(Node calendarEvent in CalendarNode.getChildren())
			{
				string id = calendarEvent.GetAttribute("projectid");
				if(id == projId)
				{
					installDay = calendarEvent.GetIntAttribute("day",0);
				}
			}

			//We need to allowed the cancel of an install if declared
			if(installDay != 0)
			{
				Boolean notAlreadyPassed = false;
				//has it passed, what stage is the project at 
				string stage = projectInvolved.GetAttribute("stage");

				// : To fix bug 3185 (can't cancel an install until after the day when it
				// was added to the schedule), we just always do the following;
				// previously, it was conditional on the stage attribute being non-null.
				stage = stage.ToLower();
				if ((stage==AttrName_Stage_INSTALL_OK)|(stage == AttrName_Stage_INSTALL_FAIL))
				{
					notAlreadyPassed = true;
				}

				if (notAlreadyPassed==false)
				{
					string disp = "Cancel product "+projId+" install on day "+CONVERT.ToStr(installDay);

					if(UseShortInstallMsg)
					{
						disp = "Cancel product "+projId+" install (Day "+CONVERT.ToStr(installDay)+")";
					}

					ImageTextButton cancel = new StyledDynamicButton ("standard", disp);
					cancel.Font = MyDefaultSkinFontBold9;
					cancel.Size = new Size(CancelInstallButtonWidth,25);
					cancel.Tag = new CancelBooking(projId, installDay, projectInvolved);
					cancel.Location = new Point(CancelInstallButtonOffsetX+ButtonGap,y);
					cancel.Click += cancelinstall_Click;
					Controls.Add(cancel);
					rightButtons.Add(cancel);

					focusJumper.Add(cancel);
				}
			}

			ResumeLayout(false);
		}


		protected void cancelButton_Click(object sender, EventArgs e)
		{
			_tcp.DisposeEntryPanel();
		}

		protected virtual void confirm(string str, object cancelobject)
		{
			SuspendLayout();

			Color upColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_text_colour", Color.Black);
			Color downColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_down_text_colour", Color.White);
			Color hoverColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_hover_text_colour", Color.Green);
			Color disabledColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_disabled_text_colour", Color.DarkGray);

			foreach(Control c in Controls)
			{
				if(c!=null) c.Dispose();
			}
			Controls.Clear();

			Label header = new Label();
			header.Text = "Confirm Cancel";
			header.Size = new Size(220,25);
			header.Font = MyDefaultSkinFontBold9;
			header.Location = new Point(10,10);
			Controls.Add(header);

			Label header2 = new Label();
			header2.Text = "Are you sure you want to "+str + "?";
			header2.Size = new Size(500,15);
			header2.TextAlign = ContentAlignment.MiddleCenter;
			header2.Font = MyDefaultSkinFontNormal10;
			header2.Location = new Point(0,100);
			Controls.Add(header2);

			ImageTextButton okButton = new StyledDynamicButton ("standard", "Yes");
			okButton.Font = MyDefaultSkinFontBold9;
			okButton.Size = new Size(80,20);
			okButton.Location = new Point(180,120);
			okButton.Tag = cancelobject;
			okButton.Click += ok_Click;
			Controls.Add(okButton);

			ImageTextButton cancelButton = new StyledDynamicButton ("standard", "No");
			cancelButton.Font = MyDefaultSkinFontBold9;
			cancelButton.Size = new Size(80,20);
			cancelButton.Location = new Point(270,120);
			cancelButton.Click += dispose_Click;
			Controls.Add(cancelButton);

			ErrMsgDisplay = new Label();
			ErrMsgDisplay.ForeColor = errorLabel_foreColor;
			ErrMsgDisplay.Text = "Cancel Product";
			ErrMsgDisplay.Size = new Size(500,15);
			ErrMsgDisplay.Font = MyDefaultSkinFontBold9;
			ErrMsgDisplay.Location = new Point(10,155);
			ErrMsgDisplay.TextAlign = ContentAlignment.MiddleCenter;
			ErrMsgDisplay.Visible = false;
			Controls.Add(ErrMsgDisplay);

			focusJumper.Dispose();
			focusJumper = new FocusJumper();
			focusJumper.Add(okButton);
			focusJumper.Add(cancelButton);

			ResumeLayout(false);

			cancelButton.Focus();
		}

		protected Boolean CheckProjectInstallCancellationIsAllowed(Node Project, out string ErrMsg)
		{
			//need to check that project 
			string projectid = Project.GetAttribute("projectid");
			string productid = Project.GetAttribute("productid");
			string stage = Project.GetAttribute("stage");
			Boolean permission = true;
			ErrMsg = "";

			if (stage != "")
			{
				stage = stage.ToLower();
				if ((stage==AttrName_Stage_INSTALL_OK)|(stage == AttrName_Stage_INSTALL_FAIL))
				{
					if (stage==AttrName_Stage_INSTALL_OK)
					{
						ErrMsg = "Install already completed OK. No cancellation allowed";
					}
					else
					{
						ErrMsg = "Install attempt failed. No cancellation allowed";
					}
					permission = false;
				}
			}
			return permission;
		}

		protected Boolean CheckProjectCancellationIsAllowed(Node Project, out string ErrMsg)
		{
			//need to check that project 
			string projectid = Project.GetAttribute("projectid");
			string productid = Project.GetAttribute("productid");
			string stage = Project.GetAttribute("stage");
			Boolean permission = true;
			ErrMsg = "";

			if (stage != "")
			{
				stage = stage.ToLower();
				if ((stage==AttrName_Stage_HANDOVER)|(stage == AttrName_Stage_READY)|
					(stage==AttrName_Stage_INSTALL_OK)|(stage == AttrName_Stage_INSTALL_FAIL))
				{
					ErrMsg = "Project work completed, no cancellation allowed";
					permission = false;
				}
			}
			return permission;
		}

		protected void ok_Click(object sender, EventArgs e)
		{
			object o =  (object)((ImageTextButton) sender).Tag;
			string ErrMsg = string.Empty;
			Boolean ClosePanel = true;

			if(o.GetType().ToString() == "TransitionScreens.CancelSIP+CancelProject")
			{
				CancelProject p = (CancelProject) o;

				if (CheckProjectCancellationIsAllowed(p.project, out ErrMsg))
				{
					ArrayList al = new ArrayList();
					al.Add( new AttributeValuePair("projectid", p.id) );
					Node incident = new Node(projectIncomingRequestQueueHandle, "cancel","", al);
				}
				else
				{
					if (ErrMsgDisplay != null)
					{
						ErrMsgDisplay.Text = ErrMsg;
						ErrMsgDisplay.Visible = true;
					}
					ClosePanel = false;
				}
			}
			else
			{
				CancelBooking p = (CancelBooking) o;

				if (CheckProjectInstallCancellationIsAllowed(p.project, out ErrMsg))
				{
					ArrayList al = new ArrayList();
					al.Add( new AttributeValuePair("projectid", p.id));
					al.Add( new AttributeValuePair("when", CONVERT.ToStr(p.day)));
					Node incident = new Node(projectIncomingRequestQueueHandle, "cancelbooking","", al);
				}
				else
				{
					if (ErrMsgDisplay != null)
					{
						ErrMsgDisplay.Text = ErrMsg;
						ErrMsgDisplay.Visible = true;
					}
					ClosePanel = false;
				}
			}
			//should we close the Panel
			if (ClosePanel)
			{
				_tcp.DisposeEntryPanel();
			}
		}

		protected void cancel_Click(object sender, EventArgs e)
		{
			CancelProject p = (CancelProject) ((ImageTextButton) sender).Tag;
			confirm("cancel product "+p.id.ToString(),p);
		}

		protected void cancelinstall_Click(object sender, EventArgs e)
		{
			CancelBooking p = (CancelBooking) ((ImageTextButton) sender).Tag;
			confirm("cancel booking on day "+p.day.ToString(),p);
		}

		protected void dispose_Click(object sender, EventArgs e)
		{
			_tcp.DisposeEntryPanel();
		}
	
		protected override void Dispose(bool disposing)
		{
			if( disposing )
			{
				focusJumper.Dispose();
			}
			base.Dispose (disposing);
		}

		protected void CancelSIP_GotFocus(object sender, EventArgs e)
		{
			cancelButton.Focus();
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

			header.Bounds = new Rectangle (0, 0, Width / 2, 25);
			header.BackColor = SkinningDefs.TheInstance.GetColorData("popup_title_background_colour", Color.White);
			header.ForeColor = SkinningDefs.TheInstance.GetColorData("popup_title_foreground_colour", Color.Black);

			header2.Bounds = new Rectangle (Width / 2, 0, Width / 2, 25);
			header2.BackColor = SkinningDefs.TheInstance.GetColorData("popup_title_background_colour", Color.White);
			header2.ForeColor = SkinningDefs.TheInstance.GetColorData("popup_title_foreground_colour", Color.Black);

			foreach (var button in leftButtons)
			{
				button.Left = header.Left + instep;
				button.Width = (Width / 2) - (2 * instep);
			}

			foreach (var button in rightButtons)
			{
				button.Left = header2.Left + instep;
				button.Width = (Width / 2) - (2 * instep);
			}
		}
	}
}