using System;
using System.Collections;
using System.IO;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

using Network;
using CommonGUI;
using LibCore;
using CoreUtils;
using BusinessServiceRules;
using Polestar_PM.DataLookup;
using Polestar_PM.OpsEngine;

namespace Polestar_PM.OpsGUI
{
	/// <summary>
	/// The Change Card allows the Players to select a particular defined Change 
	/// The Change represents a predefined application that needs to be installed.
	/// They must specify the Location that the application needs to be installed at
	/// They must specify the day that the installation needs to take place on.
	/// 
	/// It is a bit like the FSC system and internally handled in a very similar fashion
	/// 
	/// The definitions are currently in the network file for ease of access 
	/// and we need to prevent the players using the same one twice.
	/// </summary>

	public class DataEntryPanel_ChangeCard  : FlickerFreePanel
	{
		protected IDataEntryControlHolder _mainPanel;

		protected NodeTree MyNodeTree = null;
		protected Node queueNode = null;
		protected Node CurrDayNode = null;
		protected Node changecard_list_node;
		protected Hashtable cc_nodes = new Hashtable();

		//Data represeting what was selected 
		protected Node requested_cc_node = null;
		protected string requested_cc_nodename = null;
		protected int requested_cc_id = 0;
		protected string requested_location = "";
		protected int requested_day = 1;

		protected Font MyDefaultSkinFontNormal8 = null;
		protected Font MyDefaultSkinFontNormal10 = null;
		protected Font MyDefaultSkinFontNormal12 = null;
		protected Font MyDefaultSkinFontBold8 = null;
		protected Font MyDefaultSkinFontBold10 = null;
		protected Font MyDefaultSkinFontBold12 = null;
		protected Font MyDefaultSkinFontBold24 = null;

		private ImageTextButton newBtnCC1 = new ImageTextButton(0);
		private ImageTextButton newBtnCC2 = new ImageTextButton(0);
		private ImageTextButton newBtnCC3 = new ImageTextButton(0);
		private ImageTextButton newBtnCC4 = new ImageTextButton(0);
		private ImageTextButton newBtnCC5 = new ImageTextButton(0);
		private ImageTextButton newBtnCC6 = new ImageTextButton(0);
		private ImageTextButton newBtnCC7 = new ImageTextButton(0);
		private ImageTextButton newBtnCC8 = new ImageTextButton(0);

		private ImageTextButton newBtnOK = new ImageTextButton(0);
		private ImageTextButton newBtnCancel = new ImageTextButton(0);

		private System.Windows.Forms.Panel chooseSlotPanel;
		private System.Windows.Forms.Panel locationPanel;

		private System.Windows.Forms.TextBox locTextBox;
		private System.Windows.Forms.TextBox dayTextBox;
		private System.Windows.Forms.Label titleLocation;
		private System.Windows.Forms.Label titleDay;

		private System.Windows.Forms.Label titleLabel;
		private System.Windows.Forms.Label helpLabel;

		private int MaxGameDays = 25;

		private Node comms_list_node = null;

		
		public DataEntryPanel_ChangeCard(IDataEntryControlHolder mainPanel, NodeTree tree)
		{
			MyNodeTree = tree;
			_mainPanel = mainPanel;
			queueNode = tree.GetNamedNode("TaskManager");
			changecard_list_node = tree.GetNamedNode("change_list");
			CurrDayNode = tree.GetNamedNode("CurrentDay");
			comms_list_node = tree.GetNamedNode("comms_list");

			Build_CC_data();

			string fontname =  SkinningDefs.TheInstance.GetData("fontname");
			MyDefaultSkinFontNormal8 = ConstantSizeFont.NewFont(fontname,8);
			MyDefaultSkinFontNormal10 = ConstantSizeFont.NewFont(fontname,10);
			MyDefaultSkinFontNormal12 = ConstantSizeFont.NewFont(fontname,12);
			MyDefaultSkinFontBold8 = ConstantSizeFont.NewFont(fontname,8,FontStyle.Bold);
			MyDefaultSkinFontBold10 = ConstantSizeFont.NewFont(fontname,10,FontStyle.Bold);
			MyDefaultSkinFontBold12 = ConstantSizeFont.NewFont(fontname,12,FontStyle.Bold);
			MyDefaultSkinFontBold24 = ConstantSizeFont.NewFont(fontname,24,FontStyle.Bold);

			this.BackgroundImage = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "images\\panels\\PM_opsback.png");

			//this.ShowInTaskbar = false;
			//this.ClientSize = new Size (520,280);
			//this.FormBorderStyle = FormBorderStyle.None;
			//this.Opacity = 1.00;
			this.Size = new Size(520,255);
			BuildBaseControls();
			BuildPanelButtons();

			titleLabel = new System.Windows.Forms.Label();
			titleLabel.BackColor = System.Drawing.Color.Transparent;
			titleLabel.Font = MyDefaultSkinFontBold12;
			titleLabel.ForeColor = System.Drawing.Color.White;
			titleLabel.Location = new System.Drawing.Point(110-25, 10-2);
			titleLabel.Name = "titleLabel";
			titleLabel.Size = new System.Drawing.Size(380, 18);
			titleLabel.TabIndex = 11;
			titleLabel.Text = "Change Card";
			titleLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.Controls.Add(titleLabel);

			helpLabel = new System.Windows.Forms.Label();
			helpLabel.BackColor = System.Drawing.Color.Transparent;
			helpLabel.Font = MyDefaultSkinFontNormal10;
			helpLabel.ForeColor = System.Drawing.Color.White;
			helpLabel.Location = new System.Drawing.Point(110-25, 50-20-1);
			helpLabel.Name = "helpLabel";
			helpLabel.Size = new System.Drawing.Size(380, 18);
			helpLabel.TabIndex = 20;
			helpLabel.Text = "Select Change ID to implement ";
			helpLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.Controls.Add(helpLabel);

			chooseSlotPanel.Visible = true;
			locationPanel.Visible = false;
		}

		new public void Dispose()
		{
			changecard_list_node = null;
			cc_nodes.Clear();

			if (MyDefaultSkinFontNormal8 != null)
			{
				MyDefaultSkinFontNormal8.Dispose();
				MyDefaultSkinFontNormal8 = null;
			}
			if (MyDefaultSkinFontNormal10 != null)
			{
				MyDefaultSkinFontNormal10.Dispose();
				MyDefaultSkinFontNormal10 = null;
			}
			if (MyDefaultSkinFontNormal12 != null)
			{
				MyDefaultSkinFontNormal12.Dispose();
				MyDefaultSkinFontNormal12 = null;
			}
			if (MyDefaultSkinFontBold8 != null)
			{
				MyDefaultSkinFontBold8.Dispose();
				MyDefaultSkinFontBold8 = null;
			}
			if (MyDefaultSkinFontBold10 != null)
			{
				MyDefaultSkinFontBold10.Dispose();
				MyDefaultSkinFontBold10 = null;
			}
			if (MyDefaultSkinFontBold12 != null)
			{
				MyDefaultSkinFontBold12.Dispose();
				MyDefaultSkinFontBold12 = null;
			}
			if (MyDefaultSkinFontBold24 != null)
			{
				MyDefaultSkinFontBold24.Dispose();
				MyDefaultSkinFontBold24 = null;
			}
		}

		private int getcurrentDay()
		{
			int current_day =0;
			if (CurrDayNode != null)
			{
				current_day = CurrDayNode.GetIntAttribute("day",0);
			}
			return current_day;
		}

		public void Build_CC_data()
		{
			foreach (Node cc_node in changecard_list_node.getChildren())
			{
				string cc_name = cc_node.GetAttribute("name");
				if (cc_nodes.ContainsKey(cc_name)==false)
				{
					cc_nodes.Add(cc_name, cc_node);
				}
			}
		}

		public bool isCC_notDone(int cc_number)
		{
			string cc_name = "cc"+CONVERT.ToStr(cc_number);
			bool isNotDone = false;

			if (cc_nodes.ContainsKey(cc_name))
			{
				Node cc_node = (Node) cc_nodes[cc_name];
				if (cc_node != null)
				{
					string status = cc_node.GetAttribute("status");
					if (status.ToLower()=="todo")
					{
						isNotDone = true;
					}
				}
			}
			return isNotDone;
		}

		public void BuildPanelButtons()
		{
			//newBtnCancel.SetButton(AppInfo.TheInstance.Location+"\\images\\buttons\\button_70x25.png");
			newBtnCancel.SetVariants("images\\buttons\\button_70x25.png");
			newBtnCancel.Location = new System.Drawing.Point(400, 220);
			newBtnCancel.Name = "newBtnCancel";
			newBtnCancel.Size = new System.Drawing.Size(70, 25);
			newBtnCancel.TabIndex = 22;
			newBtnCancel.ButtonFont = MyDefaultSkinFontBold10;
			//newBtn.ForeColor = System.Drawing.Color.Black;
			newBtnCancel.SetButtonText("Cancel",
				System.Drawing.Color.Black,System.Drawing.Color.Black,
				System.Drawing.Color.Green,System.Drawing.Color.Gray);
			//newBtn.ButtonPressed += new ProjectMngmntGamePanels.ImageButton.ImageButtonEventArgsHandler(playButton_ButtonPressed);
			newBtnCancel.Click += new System.EventHandler(this.newBtnCancel_Click);
			this.Controls.Add(newBtnCancel);

			//newBtnOK.SetButton(AppInfo.TheInstance.Location+"\\images\\buttons\\button_70x25.png");
			newBtnOK.SetVariants("images\\buttons\\button_70x25.png");
			newBtnOK.Location = new System.Drawing.Point(300, 220);
			newBtnOK.Name = "newBtnOK";
			newBtnOK.Size = new System.Drawing.Size(70, 25);
			newBtnOK.TabIndex = 21;
			newBtnOK.ButtonFont = MyDefaultSkinFontBold10;
			//newBtn.ForeColor = System.Drawing.Color.Black;
			newBtnOK.SetButtonText("OK",
				System.Drawing.Color.Black,System.Drawing.Color.Black,
				System.Drawing.Color.Green,System.Drawing.Color.Gray);
			//newBtn.ButtonPressed += new ProjectMngmntGamePanels.ImageButton.ImageButtonEventArgsHandler(playButton_ButtonPressed);
			newBtnOK.Click += new System.EventHandler(this.newBtnOK_Click);
			//newBtnOK.Visible = false;
			newBtnOK.Visible = true;
			newBtnOK.Enabled = false;
			this.Controls.Add(newBtnOK);
		}

		public void BuildSlotButtonControls() 
		{
			//newBtnCC1.SetButton(AppInfo.TheInstance.Location+"\\images\\buttons\\button_50x40.png");
			newBtnCC1.SetVariants("images\\buttons\\button_50x40.png");
			newBtnCC1.Location = new System.Drawing.Point(10, 10);
			newBtnCC1.Name = "Button1";
			newBtnCC1.Size = new System.Drawing.Size(50, 40);
			newBtnCC1.TabIndex = 8;
			newBtnCC1.ButtonFont = MyDefaultSkinFontBold10;
			newBtnCC1.Tag = 1;
			newBtnCC1.SetButtonText("1",
				System.Drawing.Color.Black,System.Drawing.Color.Black,
				System.Drawing.Color.Green,System.Drawing.Color.Gray);
			newBtnCC1.Click += new System.EventHandler(this.CC_Button_Click);
			this.chooseSlotPanel.Controls.Add(newBtnCC1);
			newBtnCC1.Enabled = isCC_notDone(1);

			//newBtnCC2.SetButton(AppInfo.TheInstance.Location+"\\images\\buttons\\button_50x40.png");
			newBtnCC2.SetVariants("images\\buttons\\button_50x40.png");
			newBtnCC2.Location = new System.Drawing.Point(10+65*1, 10);
			newBtnCC2.Name = "Button2";
			newBtnCC2.Size = new System.Drawing.Size(50, 40);
			newBtnCC2.TabIndex = 14;
			newBtnCC2.ButtonFont = MyDefaultSkinFontBold10;
			newBtnCC2.Tag = 2;
			//newBtn.ForeColor = System.Drawing.Color.Black;
			newBtnCC2.SetButtonText("2",
				System.Drawing.Color.Black,System.Drawing.Color.Black,
				System.Drawing.Color.Green,System.Drawing.Color.Gray);
			//newBtn.ButtonPressed += new ProjectMngmntGamePanels.ImageButton.ImageButtonEventArgsHandler(playButton_ButtonPressed);
			newBtnCC2.Click += new System.EventHandler(this.CC_Button_Click);
			this.chooseSlotPanel.Controls.Add(newBtnCC2);
			newBtnCC2.Enabled = isCC_notDone(2);

			//newBtnCC3.SetButton(AppInfo.TheInstance.Location+"\\images\\buttons\\button_50x40.png");
			newBtnCC3.SetVariants("images\\buttons\\button_50x40.png");
			newBtnCC3.Location = new System.Drawing.Point(10 + 65 * 2, 10);
			newBtnCC3.Name = "Button3";
			newBtnCC3.Size = new System.Drawing.Size(50, 40);
			newBtnCC3.TabIndex = 13;
			newBtnCC3.ButtonFont = MyDefaultSkinFontBold10;
			newBtnCC3.Tag = 3;
			//newBtn.ForeColor = System.Drawing.Color.Black;
			newBtnCC3.SetButtonText("3",
				System.Drawing.Color.Black,System.Drawing.Color.Black,
				System.Drawing.Color.Green,System.Drawing.Color.Gray);
			//newBtn.ButtonPressed += new ProjectMngmntGamePanels.ImageButton.ImageButtonEventArgsHandler(playButton_ButtonPressed);
			newBtnCC3.Click += new System.EventHandler(this.CC_Button_Click);
			this.chooseSlotPanel.Controls.Add(newBtnCC3);
			newBtnCC3.Enabled = isCC_notDone(3);

			//newBtnCC4.SetButton(AppInfo.TheInstance.Location+"\\images\\buttons\\button_50x40.png");
			newBtnCC4.SetVariants("images\\buttons\\button_50x40.png");
			newBtnCC4.Location = new System.Drawing.Point(10 + 65 * 3, 10);
			newBtnCC4.Name = "Button4";
			newBtnCC4.Size = new System.Drawing.Size(50, 40);
			newBtnCC4.TabIndex = 10;
			newBtnCC4.ButtonFont =MyDefaultSkinFontBold10;
			newBtnCC4.Tag = 4;
			//newBtn.ForeColor = System.Drawing.Color.Black;
			newBtnCC4.SetButtonText("4",
				System.Drawing.Color.Black,System.Drawing.Color.Black,
				System.Drawing.Color.Green,System.Drawing.Color.Gray);
			//newBtn.ButtonPressed += new ProjectMngmntGamePanels.ImageButton.ImageButtonEventArgsHandler(playButton_ButtonPressed);
			newBtnCC4.Click += new System.EventHandler(this.CC_Button_Click);
			this.chooseSlotPanel.Controls.Add(newBtnCC4);
			newBtnCC4.Enabled = isCC_notDone(4);

			//newBtnCC5.SetButton(AppInfo.TheInstance.Location+"\\images\\buttons\\button_50x40.png");
			newBtnCC5.SetVariants("images\\buttons\\button_50x40.png");
			newBtnCC5.Location = new System.Drawing.Point(10 + 65 * 4, 10);
			newBtnCC5.Name = "Button5";
			newBtnCC5.Size = new System.Drawing.Size(50, 40);
			newBtnCC5.TabIndex = 11;
			newBtnCC5.ButtonFont = MyDefaultSkinFontBold10;
			newBtnCC5.Tag = 5;
			//newBtn.ForeColor = System.Drawing.Color.Black;
			newBtnCC5.SetButtonText("5",
				System.Drawing.Color.Black,System.Drawing.Color.Black,
				System.Drawing.Color.Green,System.Drawing.Color.Gray);
			//newBtn.ButtonPressed += new ProjectMngmntGamePanels.ImageButton.ImageButtonEventArgsHandler(playButton_ButtonPressed);
			newBtnCC5.Click += new System.EventHandler(this.CC_Button_Click);
			this.chooseSlotPanel.Controls.Add(newBtnCC5);
			newBtnCC5.Enabled = isCC_notDone(5);

			//newBtnCC6.SetButton(AppInfo.TheInstance.Location+"\\images\\buttons\\button_50x40.png");
			newBtnCC6.SetVariants("images\\buttons\\button_50x40.png");
			newBtnCC6.Location = new System.Drawing.Point(10 + 65 * 5, 10);
			newBtnCC6.Name = "Button6";
			newBtnCC6.Size = new System.Drawing.Size(50, 40);
			newBtnCC6.TabIndex = 12;
			newBtnCC6.ButtonFont = MyDefaultSkinFontBold10;
			newBtnCC6.Tag = 6;
			//newBtn.ForeColor = System.Drawing.Color.Black;
			newBtnCC6.SetButtonText("6",
				System.Drawing.Color.Black,System.Drawing.Color.Black,
				System.Drawing.Color.Green,System.Drawing.Color.Gray);
			//newBtn.ButtonPressed += new ProjectMngmntGamePanels.ImageButton.ImageButtonEventArgsHandler(playButton_ButtonPressed);
			newBtnCC6.Click += new System.EventHandler(this.CC_Button_Click);
			this.chooseSlotPanel.Controls.Add(newBtnCC6);
			newBtnCC6.Enabled = isCC_notDone(6);
		}

		public void BuildBaseControls() 
		{
			this.chooseSlotPanel = new System.Windows.Forms.Panel();
			this.locationPanel = new System.Windows.Forms.Panel();
			this.titleLocation = new System.Windows.Forms.Label();
			this.titleDay = new System.Windows.Forms.Label();
			this.locTextBox = new System.Windows.Forms.TextBox();
			this.dayTextBox = new FilteredTextBox (TextBoxFilterType.Digits);
			this.chooseSlotPanel.SuspendLayout();
			this.locationPanel.SuspendLayout();
			this.SuspendLayout();

			BuildSlotButtonControls();
			// 
			// chooseSlotPanel
			// 
			this.chooseSlotPanel.Location = new System.Drawing.Point(78, 50);
			this.chooseSlotPanel.Name = "chooseSlotPanel";
			this.chooseSlotPanel.Size = new System.Drawing.Size(395, 160);
			//this.chooseSlotPanel.BackColor = Color.FromArgb(176,196,222); //TODO SKIN
			this.chooseSlotPanel.BackColor = Color.Transparent;
			//this.chooseSlotPanel.BackColor = Color.Violet;
			this.chooseSlotPanel.TabIndex = 13;

			this.titleLocation.BackColor = System.Drawing.Color.Transparent;
			this.titleLocation.Font = MyDefaultSkinFontNormal12;
			this.titleLocation.ForeColor = System.Drawing.Color.Black;
			this.titleLocation.Location = new System.Drawing.Point(10, 10);
			this.titleLocation.Name = "titleLabel";
			this.titleLocation.Size = new System.Drawing.Size(100, 18);
			this.titleLocation.TabIndex = 11;
			this.titleLocation.Text = "Location";
			this.titleLocation.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

			// 
			// locTextBox
			// 
			this.locTextBox.BackColor = System.Drawing.Color.Black;
			this.locTextBox.Font = this.MyDefaultSkinFontBold12;
			this.locTextBox.ForeColor = System.Drawing.Color.White;
			this.locTextBox.Location = new System.Drawing.Point(300, 10);
			this.locTextBox.Name = "locTextBox";
			this.locTextBox.Size = new System.Drawing.Size(80, 20);
			this.locTextBox.TabIndex = 28;
			this.locTextBox.Text = "";
			this.locTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
			this.locTextBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;

			// 
			// dayTitle
			// 
			this.titleDay.BackColor = System.Drawing.Color.Transparent;
			this.titleDay.Font = MyDefaultSkinFontNormal12;
			this.titleDay.ForeColor = System.Drawing.Color.Black;
			this.titleDay.Location = new System.Drawing.Point(10, 50);
			this.titleDay.Name = "titleLabel";
			this.titleDay.Size = new System.Drawing.Size(160, 18);
			this.titleDay.TabIndex = 11;
			this.titleDay.Text = "Install Day";
			this.titleDay.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

			// 
			// dayTextBox
			// 
			this.dayTextBox.BackColor = System.Drawing.Color.Black;
			this.dayTextBox.Font = this.MyDefaultSkinFontBold12;
			this.dayTextBox.ForeColor = System.Drawing.Color.White;
			this.dayTextBox.Location = new System.Drawing.Point(300, 50);
			this.dayTextBox.Name = "locTextBox";
			this.dayTextBox.Size = new System.Drawing.Size(80, 43);
			this.dayTextBox.TabIndex = 28;
			this.dayTextBox.Text = "";
			this.dayTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
			this.dayTextBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;

			// 
			// locationPanel
			// 
			this.locationPanel.Controls.Add(this.titleLocation);
			this.locationPanel.Controls.Add(this.locTextBox);
			this.locationPanel.Controls.Add(this.titleDay);
			this.locationPanel.Controls.Add(this.dayTextBox);
			this.locationPanel.Location = new System.Drawing.Point(90-7, 50);
			this.locationPanel.Name = "locationPanel";
			this.locationPanel.Size = new System.Drawing.Size(395, 160);
			//this.locationPanel.BackColor = Color.FromArgb(176,196,222); //TODO SKIN
			this.locationPanel.BackColor = Color.Transparent;
			this.locationPanel.TabIndex = 14;

			// 
			// ForwardScheduleControl
			// 
			this.Controls.Add(this.locationPanel);
			this.Controls.Add(this.chooseSlotPanel);
			this.Name = "ChangeCardControl";
			this.Size = new System.Drawing.Size(520,280);
			this.chooseSlotPanel.ResumeLayout(false);
			this.locationPanel.ResumeLayout(false);
			this.ResumeLayout(false);
		}

		private void CC_Button_Click(object sender, System.EventArgs e)
		{
			int selected_cc = 1;
			string cc_name = "";
			//Extract out the FSC from the control that was used
			if (sender is ImageTextButton)
			{
				ImageTextButton ib = (ImageTextButton)sender;
				if (ib != null)
				{
					selected_cc = (int) (ib.Tag);
					cc_name = "cc"+CONVERT.ToStr(selected_cc);
					requested_cc_nodename = cc_name;
					if (cc_nodes.ContainsKey(cc_name))
					{
						requested_cc_node = (Node) cc_nodes[cc_name];
					}
					requested_cc_id = selected_cc;

					//Update the Display
					this.helpLabel.Text = "Enter Installation Location for Change "+CONVERT.ToStr(requested_cc_id);

					chooseSlotPanel.Visible = false;
					locationPanel.Visible = true;
					locTextBox.Text = "";
					newBtnOK.Visible = true;
					newBtnOK.Enabled = true;
					SetFocus();
				}
			}
		}

		private bool CheckDay(int dayToCheck, string errPrefix,  ArrayList errs)
		{
			bool OpSuccess = true;
			if (dayToCheck > MaxGameDays)
			{
				errs.Add(errPrefix+"Requested Day " + CONVERT.ToStr(dayToCheck) + " is greater than " + CONVERT.ToStr(MaxGameDays));
				OpSuccess = false;
			}
			if (requested_day <= getcurrentDay())
			{
				errs.Add(errPrefix + "Requested Day " + CONVERT.ToStr(dayToCheck) + " has passed");
				OpSuccess = false;
			}

			//Check that Day is clear 
			bool totallyFree = false;
			bool projects_allowed = false;

			OpsReader myOps = new OpsReader(this.MyNodeTree);
			myOps.isDayFree(dayToCheck, out totallyFree, out projects_allowed);
			myOps.Dispose();

			if (totallyFree == false)
			{
				errs.Add(errPrefix + "Day " + CONVERT.ToStr(dayToCheck) + " has already been booked");
				OpSuccess = false;
			}

			Node projectsNode = this.MyNodeTree.GetNamedNode("pm_projects_running");
			foreach (Node prjnode in projectsNode.getChildren())
			{
				ProjectReader pr = new ProjectReader(prjnode);
				if (pr.getInstallDay() == dayToCheck)
				{
					errs.Add(errPrefix + "Day " + CONVERT.ToStr(dayToCheck) + " has already been booked by Project " + pr.getProjectID());
					OpSuccess = false;
				}
			}
			return OpSuccess;
		}

		private bool CheckOrder( out ArrayList errs)
		{
			bool OpSuccess = true;
			errs = new ArrayList();
			string errmsg = "";

			//extract the data 
			this.requested_location = locTextBox.Text;
			this.requested_day = CONVERT.ParseIntSafe(dayTextBox.Text, 0);
			//===============================================================
			//==Check for any time based problems============================ 
			//===============================================================
			int duration = requested_cc_node.GetIntAttribute("time_cost",0);

			//===============================================================
			//==Check for any time based problems============================ 
			//===============================================================
			//Check that the day is valid (day>1 and Day<31)
			OpSuccess = CheckDay(requested_day,"", errs);
			if (OpSuccess)
			{
				if (duration>1)
				{
					for (int stepp = 0; stepp < (duration - 1); stepp++)
					{
						if (OpSuccess)
						{
							OpSuccess = CheckDay(requested_day + (stepp + 1), "Change requires " + CONVERT.ToStr(duration) + " Days: ", errs);
						}
					}
				}
			}
			//===============================================================
			//==Check for any location based problems========================
			//===============================================================
			if (OpSuccess)
			{
				if (this.requested_location != "")
				{
					//extract the footprint for the required app 
					int mem_required = requested_cc_node.GetIntAttribute("memory", 0);
					int disk_required = requested_cc_node.GetIntAttribute("disk", 0);
					int money_cost = requested_cc_node.GetIntAttribute("money_cost", 0);
					string platform_required = requested_cc_node.GetAttribute("platform");

					AppInstaller ai = new AppInstaller(MyNodeTree);
					if (ai.check_change_app(mem_required, disk_required, platform_required,
						this.requested_location, out errmsg) == false)
					{
						errs.Add(errmsg);
						OpSuccess = false;
					}
					ai.Dispose();
				}
			}
			return OpSuccess;
		}

		private bool HandleOrder()
		{
			ArrayList attrs = new ArrayList ();
			attrs.Add(new AttributeValuePair ("cmd_type", "request_cc"));
			attrs.Add(new AttributeValuePair ("cc_id", CONVERT.ToStr(requested_cc_id)));
			attrs.Add(new AttributeValuePair ("cc_nodename", requested_cc_nodename));
			attrs.Add(new AttributeValuePair ("cc_day", CONVERT.ToStr(requested_day)));
			attrs.Add(new AttributeValuePair ("cc_location", requested_location));
			new Node (queueNode, "task", "", attrs);	
			//
			string TitleText = "Operations Team: Change " + CONVERT.ToStr(requested_cc_id) + " scheduled for day " + CONVERT.ToStr(requested_day);
			attrs.Clear();
			attrs.Add(new AttributeValuePair("type", "msg"));
			attrs.Add(new AttributeValuePair("subtype", "ops_msg"));
			attrs.Add(new AttributeValuePair("display_title", TitleText));
			attrs.Add(new AttributeValuePair("display_content", ""));
			attrs.Add(new AttributeValuePair("display_icon", "prg_msg"));
			new Node(comms_list_node, "msg", "", attrs);
			//
			_mainPanel.DisposeEntryPanel();
			return true;
		}

		private void newBtnOK_Click(object sender, System.EventArgs e)
		{
			bool good_order_placed = false;
			ArrayList errs = new ArrayList();

			good_order_placed = CheckOrder(out errs);
			if (good_order_placed)
			{
				good_order_placed = HandleOrder();
			}
			else
			{
				string disp = "";
				foreach (string err in errs)
				{
					disp += err + "\r\n";
				}
				MessageBox.Show(disp,"Error");
			}
		}

		private void newBtnCancel_Click(object sender, System.EventArgs e)
		{
			_mainPanel.DisposeEntryPanel();
		}

		public void SetFocus ()
		{
			Focus();
			locTextBox.Focus();
		}
	}
}