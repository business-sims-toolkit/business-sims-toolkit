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
	public class DataEntryPanel_UpgradePanel  : FlickerFreePanel
	{
		protected IDataEntryControlHolder _mainPanel;

		protected NodeTree MyNodeTree;
		protected Node queueNode;
		protected Node CurrDayNode = null;

		protected Hashtable server_nodes = new Hashtable();
		protected ArrayList server_names = new ArrayList();
		protected string chosenUpgradeTypeLetter="";
		protected string chosenServerName="";
		protected int requested_day = 0;

		protected Font MyDefaultSkinFontNormal8 = null;
		protected Font MyDefaultSkinFontNormal10 = null;
		protected Font MyDefaultSkinFontNormal12 = null;
		protected Font MyDefaultSkinFontBold8 = null;
		protected Font MyDefaultSkinFontBold10 = null;
		protected Font MyDefaultSkinFontBold12 = null;
		protected Font MyDefaultSkinFontBold24 = null;

		private ImageTextButton btnChooseStorage = new ImageTextButton(0);
		private ImageTextButton btnChooseMemory = new ImageTextButton(0);
		private ImageTextButton btnChooseBoth = new ImageTextButton(0);

		private ImageTextButton newBtnOK = new ImageTextButton(0);
		private ImageTextButton newBtnCancel = new ImageTextButton(0);

		private System.Windows.Forms.Panel pnl_chooseUpgradeTypePanel;
		private System.Windows.Forms.Panel pnl_chooseServerPanel;
		private System.Windows.Forms.Panel pnl_confirmationPanel;
		//private System.Windows.Forms.TextBox tbUpgradeType;
		//private System.Windows.Forms.TextBox tbUpgradeServerName;
		//private System.Windows.Forms.Label lblConfirmMessage;
		//private System.Windows.Forms.Label lblConfirmUpgradeTypeTitle;
		//private System.Windows.Forms.Label lblConfirmUpgradeNameTitle;

		private System.Windows.Forms.Label lblDayTitle;
		private System.Windows.Forms.TextBox tbDayValue;
		
		private System.Windows.Forms.Label titleLabel;
		private System.Windows.Forms.Label helpLabel;
		private System.Windows.Forms.Label additionalLabel;

		private int MaxGameDays = 25;

		public DataEntryPanel_UpgradePanel (IDataEntryControlHolder mainPanel, NodeTree tree)
		{
			MyNodeTree = tree;
			_mainPanel = mainPanel;
			queueNode = tree.GetNamedNode("TaskManager");
			CurrDayNode = tree.GetNamedNode("CurrentDay");

			Build_Server_Data();

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
			titleLabel.Size = new System.Drawing.Size(380,18);
			titleLabel.TabIndex = 11;
			titleLabel.Text = "Upgrades";
			titleLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.Controls.Add(titleLabel);

			helpLabel = new System.Windows.Forms.Label();
			helpLabel.BackColor = System.Drawing.Color.Transparent;
			helpLabel.Font = MyDefaultSkinFontNormal10;
			helpLabel.ForeColor = System.Drawing.Color.White;
			helpLabel.Location = new System.Drawing.Point(110 - 25, 50 - 20 - 1);
			helpLabel.Name = "helpLabel";
			helpLabel.Size = new System.Drawing.Size(390, 18);
			helpLabel.TabIndex = 20;
			//helpLabel.Text = "Do You want to upgrade Storage, Memory or Both ?";
			helpLabel.Text = "Do you want to upgrade storage or memory ?";
			helpLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.Controls.Add(helpLabel);

			additionalLabel = new System.Windows.Forms.Label();
			additionalLabel.BackColor = System.Drawing.Color.Transparent;
			additionalLabel.Font = MyDefaultSkinFontNormal10;
			additionalLabel.ForeColor = System.Drawing.Color.Black;
			additionalLabel.Location = new System.Drawing.Point(110 - 25, 90+ 50 - 20 - 1);
			additionalLabel.Name = "additionalLabel";
			additionalLabel.Size = new System.Drawing.Size(390, 78);
			additionalLabel.TabIndex = 21;
			//helpLabel.Text = "Do You want to upgrade Storage, Memory or Both ?";
			additionalLabel.Text = " Upgrades deliver an additional: \n \n Storage: 100GB \n\n Memory:    2GB";
			additionalLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.Controls.Add(additionalLabel);
			additionalLabel.BringToFront();




			pnl_chooseUpgradeTypePanel.Visible = true;
			pnl_chooseServerPanel.Visible = false;
			pnl_confirmationPanel.Visible = false;
		}

		new public void Dispose()
		{
			queueNode = null;
			CurrDayNode = null;
			server_nodes.Clear();
			server_names.Clear();

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
			int current_day = 0;
			if (CurrDayNode != null)
			{
				current_day = CurrDayNode.GetIntAttribute("day", 0);
			}
			return current_day;
		}

		public void Build_Server_Data()
		{
			Hashtable ht = new Hashtable();
			ArrayList types = new ArrayList();

			//Build an Lookup for the Servers within the network
			types = new ArrayList();
			types.Clear();
			types.Add("Server");
			ht = MyNodeTree.GetNodesOfAttribTypes(types);
			foreach(Node serverNode in ht.Keys)
			{
				string server_name = serverNode.GetAttribute("name");
				bool upgrade_allowed = serverNode.GetBooleanAttribute("upgrade_allowed", false);
				if (server_nodes.ContainsKey(server_name)==false)
				{
					server_nodes.Add(server_name,serverNode);
					server_names.Add(server_name);
				}
			}
			server_names.Sort();
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
			newBtnOK.Click += new System.EventHandler(this.newBtnOK_Click);
			newBtnOK.Visible = false;
			this.Controls.Add(newBtnOK);
		}

		public void BuildChooseUpgradeTypeControls()
		{
			//btnChooseStorage.SetButton(AppInfo.TheInstance.Location+"\\images\\buttons\\button_70x25.png");
			btnChooseStorage.SetVariants("images\\buttons\\button_70x25.png");
			btnChooseStorage.Location = new System.Drawing.Point(30+50, 10);
			btnChooseStorage.Name = "Button1";
			btnChooseStorage.Size = new System.Drawing.Size(90,25);
			btnChooseStorage.TabIndex = 8;
			btnChooseStorage.ButtonFont = MyDefaultSkinFontBold10;
			btnChooseStorage.Tag = 1;
			btnChooseStorage.SetButtonText("Storage",
				System.Drawing.Color.Black,System.Drawing.Color.Black,
				System.Drawing.Color.Green,System.Drawing.Color.Gray);
			btnChooseStorage.Click += new System.EventHandler(this.btnChooseStorage_Click);
			this.pnl_chooseUpgradeTypePanel.Controls.Add(btnChooseStorage);

			//btnChooseMemory.SetButton(AppInfo.TheInstance.Location+"\\images\\buttons\\button_70x25.png");
			btnChooseMemory.SetVariants("images\\buttons\\button_70x25.png");
			btnChooseMemory.Location = new System.Drawing.Point(145 + 50, 10);
			btnChooseMemory.Name = "Button1";
			btnChooseMemory.Size = new System.Drawing.Size(90,25);
			btnChooseMemory.TabIndex = 8;
			btnChooseMemory.ButtonFont = MyDefaultSkinFontBold10;
			btnChooseMemory.Tag = 1;
			btnChooseMemory.SetButtonText("Memory",
				System.Drawing.Color.Black,System.Drawing.Color.Black,
				System.Drawing.Color.Green,System.Drawing.Color.Gray);
			btnChooseMemory.Click += new System.EventHandler(this.btnChooseMemory_Click);
			this.pnl_chooseUpgradeTypePanel.Controls.Add(btnChooseMemory);

			//We are hiding the "both" option for the time being
			//btnChooseBoth.SetButton(AppInfo.TheInstance.Location+"\\images\\buttons\\button_70x25.png");
			btnChooseBoth.SetVariants("images\\buttons\\button_70x25.png");
			btnChooseBoth.Location = new System.Drawing.Point(240+20, 10);
			btnChooseBoth.Name = "Button1";
			btnChooseBoth.Size = new System.Drawing.Size(90,25);
			btnChooseBoth.TabIndex = 8;
			btnChooseBoth.ButtonFont = MyDefaultSkinFontBold10;
			btnChooseBoth.Tag = 1;
			btnChooseBoth.Visible = false;
			btnChooseBoth.SetButtonText("Both",
				System.Drawing.Color.Black,System.Drawing.Color.Black,
				System.Drawing.Color.Green,System.Drawing.Color.Gray);
			btnChooseBoth.Click += new System.EventHandler(this.btnChooseBoth_Click);
			this.pnl_chooseUpgradeTypePanel.Controls.Add(btnChooseBoth);
		}

		public void BuildChoseServerButtonControls() 
		{
			int offset_x=5;
			int offset_y=5;
			int step_x=0;
			int step_y=0;
			int button_width=57;
			int button_height=25;
			int button_sep=8;
			
			foreach (string server_name in server_names)
			{
				Node servernode = (Node) server_nodes[server_name];

				string display_text = servernode.GetAttribute("shortdesc");
				bool upgrade_hide = servernode.GetBooleanAttribute("upgrade_hide",false);

				//Most nodes have no upgrade hide field, we default to hide false and show them 
				if (upgrade_hide == false)
				{
					Point location = new Point(
						offset_x + step_x * (button_width + button_sep),
						offset_y + step_y * (button_height + button_sep));

					//Build the button 
					ImageTextButton btnServer = new ImageTextButton(0);
					//btnServer.SetButton(AppInfo.TheInstance.Location+"\\images\\buttons\\button_70x25.png");
					btnServer.SetVariants("images\\buttons\\button_70x25.png");
					btnServer.Location = location;
					btnServer.Name = "Button1";
					btnServer.Size = new System.Drawing.Size(button_width, button_height);
					btnServer.TabIndex = 8;
					btnServer.ButtonFont = MyDefaultSkinFontNormal8;
					btnServer.Tag = server_name;
					btnServer.SetButtonText(display_text,
						System.Drawing.Color.Black, System.Drawing.Color.Black,
						System.Drawing.Color.Green, System.Drawing.Color.Gray);
					btnServer.Click += new System.EventHandler(this.Server_Button_Click);
					this.pnl_chooseServerPanel.Controls.Add(btnServer);

					step_x++;
					if (step_x > 5)
					{
						step_x = 0;
						step_y++;
					}
				}
			}
		}

		public void fillConfirmationControls()
		{
			//this.chosenServerName
			//this.chosenUpgradeTypeLetter
			string confirm_help_text = "Upgrade ";
			switch (chosenUpgradeTypeLetter)
			{
				case "S":
					//tbUpgradeType.Text = "Storage";
					confirm_help_text += "Storage";
					break;
				case "M":
					//tbUpgradeType.Text = "Memory";
					confirm_help_text += "Memory";
					break;
				case "B":
					//tbUpgradeType.Text = "Both";
					confirm_help_text += "Both";
					break;
			}
			confirm_help_text += " on " + chosenServerName;
			//this.tbUpgradeServerName.Text = chosenServerName;
			this.helpLabel.Text = confirm_help_text;
			this.additionalLabel.SendToBack();
		}

		public void BuildConfirmationControls()
		{
			//lblConfirmMessage = new System.Windows.Forms.Label();
			//lblConfirmMessage.BackColor = System.Drawing.Color.Transparent;
			//lblConfirmMessage.Font = MyDefaultSkinFontBold12;
			//lblConfirmMessage.ForeColor = System.Drawing.Color.Black;
			//lblConfirmMessage.Location = new System.Drawing.Point(10, 10 - 10);
			//lblConfirmMessage.Name = "lblConfirmMessage";
			//lblConfirmMessage.Size = new System.Drawing.Size(400, 20);
			//lblConfirmMessage.TabIndex = 11;
			//lblConfirmMessage.Text = "Apply the following upgrade";
			//lblConfirmMessage.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			//this.pnl_confirmationPanel.Controls.Add(lblConfirmMessage);

			//lblConfirmUpgradeTypeTitle = new System.Windows.Forms.Label();
			//lblConfirmUpgradeTypeTitle.BackColor = System.Drawing.Color.Transparent;
			//lblConfirmUpgradeTypeTitle.Font = MyDefaultSkinFontBold12;
			//lblConfirmUpgradeTypeTitle.ForeColor = System.Drawing.Color.Black;
			//lblConfirmUpgradeTypeTitle.Location = new System.Drawing.Point(10, 40 - 10);
			//lblConfirmUpgradeTypeTitle.Name = "lblConfirmMessage";
			//lblConfirmUpgradeTypeTitle.Size = new System.Drawing.Size(140, 20);
			//lblConfirmUpgradeTypeTitle.TabIndex = 11;
			//lblConfirmUpgradeTypeTitle.Text = "Upgrade:";
			//lblConfirmUpgradeTypeTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			//this.pnl_confirmationPanel.Controls.Add(lblConfirmUpgradeTypeTitle);

			////builds the Display Boxes
			//tbUpgradeType = new System.Windows.Forms.TextBox();
			//tbUpgradeType.BackColor = System.Drawing.Color.Black;
			//tbUpgradeType.Font = this.MyDefaultSkinFontBold12;
			//tbUpgradeType.ForeColor = System.Drawing.Color.White;
			//tbUpgradeType.Location = new System.Drawing.Point(160, 40 - 10);
			//tbUpgradeType.Name = "locTextBox";
			//tbUpgradeType.Size = new System.Drawing.Size(130, 43);
			//tbUpgradeType.TabIndex = 28;
			//tbUpgradeType.Text = "";
			//tbUpgradeType.ReadOnly = true;
			//tbUpgradeType.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
			//tbUpgradeType.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
			//this.pnl_confirmationPanel.Controls.Add(tbUpgradeType);

			//lblConfirmUpgradeNameTitle = new System.Windows.Forms.Label();
			//lblConfirmUpgradeNameTitle.BackColor = System.Drawing.Color.Transparent;
			//lblConfirmUpgradeNameTitle.Font = MyDefaultSkinFontBold12;
			//lblConfirmUpgradeNameTitle.ForeColor = System.Drawing.Color.Black;
			//lblConfirmUpgradeNameTitle.Location = new System.Drawing.Point(10, 70 - 10);
			//lblConfirmUpgradeNameTitle.Name = "lblConfirmMessage";
			//lblConfirmUpgradeNameTitle.Size = new System.Drawing.Size(140, 20);
			//lblConfirmUpgradeNameTitle.TabIndex = 11;
			//lblConfirmUpgradeNameTitle.Text = "Server Name:";
			//lblConfirmUpgradeNameTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			//this.pnl_confirmationPanel.Controls.Add(lblConfirmUpgradeNameTitle);

			////builds the Display Boxes
			//tbUpgradeServerName = new System.Windows.Forms.TextBox();
			//tbUpgradeServerName.BackColor = System.Drawing.Color.Black;
			//tbUpgradeServerName.Font = this.MyDefaultSkinFontBold12;
			//tbUpgradeServerName.ForeColor = System.Drawing.Color.White;
			//tbUpgradeServerName.Location = new System.Drawing.Point(160, 70 - 10);
			//tbUpgradeServerName.Name = "locTextBox";
			//tbUpgradeServerName.Size = new System.Drawing.Size(130, 43);
			//tbUpgradeServerName.TabIndex = 28;
			//tbUpgradeServerName.Text = "";
			//tbUpgradeServerName.ReadOnly = true;
			//tbUpgradeServerName.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
			//tbUpgradeServerName.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
			//this.pnl_confirmationPanel.Controls.Add(tbUpgradeServerName);

			lblDayTitle = new System.Windows.Forms.Label();
			lblDayTitle.BackColor = System.Drawing.Color.Transparent;
			lblDayTitle.Font = MyDefaultSkinFontBold12;
			lblDayTitle.ForeColor = System.Drawing.Color.Black;
			lblDayTitle.Location = new System.Drawing.Point(10, 40 - 10);
			lblDayTitle.Name = "lblConfirmMessage";
			lblDayTitle.Size = new System.Drawing.Size(140, 20);
			lblDayTitle.TabIndex = 11;
			lblDayTitle.Text = "Day:";
			lblDayTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.pnl_confirmationPanel.Controls.Add(lblDayTitle);

			//builds the Display Boxes
			tbDayValue = new FilteredTextBox(TextBoxFilterType.Digits);
			tbDayValue.BackColor = System.Drawing.Color.Black;
			tbDayValue.Font = this.MyDefaultSkinFontBold12;
			tbDayValue.ForeColor = System.Drawing.Color.White;
			tbDayValue.Location = new System.Drawing.Point(160, 40 - 10);
			tbDayValue.Name = "locTextBox";
			tbDayValue.Size = new System.Drawing.Size(130, 43);
			tbDayValue.TabIndex = 28;
			tbDayValue.Text = "";
			tbDayValue.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
			tbDayValue.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
			this.pnl_confirmationPanel.Controls.Add(tbDayValue);
			
		}

		public void BuildBaseControls() 
		{
			this.pnl_chooseUpgradeTypePanel = new System.Windows.Forms.Panel();
			this.pnl_chooseServerPanel = new System.Windows.Forms.Panel();
			this.pnl_confirmationPanel = new System.Windows.Forms.Panel();
			//this.locationPanel = new System.Windows.Forms.Panel();
			//this.locTextBox = new System.Windows.Forms.TextBox();
			this.pnl_chooseUpgradeTypePanel.SuspendLayout();
			this.pnl_chooseServerPanel.SuspendLayout();
			this.pnl_confirmationPanel.SuspendLayout();
			this.SuspendLayout();

			BuildChooseUpgradeTypeControls();
			BuildChoseServerButtonControls();
			BuildConfirmationControls();

			this.pnl_chooseUpgradeTypePanel.Location = new System.Drawing.Point(90+16-22, 65);
			this.pnl_chooseUpgradeTypePanel.Name = "chooseUpgradeTypePanel";
			this.pnl_chooseUpgradeTypePanel.Size = new System.Drawing.Size(230+167, 150);
			this.pnl_chooseUpgradeTypePanel.BackColor = Color.FromArgb(176,196,222); //TODO SKIN COLOR
			this.pnl_chooseUpgradeTypePanel.BackColor = Color.Transparent;
			this.pnl_chooseUpgradeTypePanel.TabIndex = 13;

			this.pnl_chooseServerPanel.Location = new System.Drawing.Point(90+16-22, 60);
			this.pnl_chooseServerPanel.Name = "chooseServerPanel";
			this.pnl_chooseServerPanel.Size = new System.Drawing.Size(230+167, 150);
			this.pnl_chooseServerPanel.BackColor = Color.FromArgb(176,196,222); //TODO SKIN COLOR
			this.pnl_chooseServerPanel.BackColor = Color.Transparent;
			this.pnl_chooseServerPanel.TabIndex = 13;

			this.pnl_confirmationPanel.Location = new System.Drawing.Point(90+16-22, 65);
			this.pnl_confirmationPanel.Name = "confirmationPanel";
			this.pnl_confirmationPanel.Size = new System.Drawing.Size(230+167, 150);
			this.pnl_confirmationPanel.BackColor = Color.FromArgb(176,196,222); //TODO SKIN COLOR
			this.pnl_confirmationPanel.BackColor = Color.Transparent;
			this.pnl_confirmationPanel.TabIndex = 13;

			this.Controls.Add(this.pnl_chooseUpgradeTypePanel);
			this.Controls.Add(this.pnl_chooseServerPanel);
			this.Controls.Add(this.pnl_confirmationPanel);
			this.Name = "UpgradeServerControl";
			this.Size = new System.Drawing.Size(520,280);
			this.pnl_chooseUpgradeTypePanel.ResumeLayout(false);
			this.pnl_chooseServerPanel.ResumeLayout(false);
			this.pnl_confirmationPanel.ResumeLayout(false);
			this.ResumeLayout(false);
		}

		private void handleChosenUpgradeType(string whichupgradetype)
		{
			chosenUpgradeTypeLetter =whichupgradetype;

			switch (chosenUpgradeTypeLetter)
			{
				case "M":
					this.helpLabel.Text = "Upgrade Memory on Server";
					break;
				case "S":
					this.helpLabel.Text = "Upgrade Storage on Server";
					break;
				case "B":
					this.helpLabel.Text = "Upgrade Memory and Storage on Server";
					break;
			}
			pnl_chooseUpgradeTypePanel.Visible = false;
			pnl_chooseServerPanel.Visible = true;
			pnl_confirmationPanel.Visible = false;
			this.additionalLabel.SendToBack();
		}

		private void btnChooseStorage_Click(object sender, System.EventArgs e)
		{
			handleChosenUpgradeType("S");
		}
		private void btnChooseMemory_Click(object sender, System.EventArgs e)
		{
			handleChosenUpgradeType("M");
		}
		private void btnChooseBoth_Click(object sender, System.EventArgs e)
		{
			handleChosenUpgradeType("B");
		}

		private void Server_Button_Click(object sender, System.EventArgs e)
		{
			bool proceed = false;
			if (sender is ImageTextButton)
			{
				ImageTextButton ib = (ImageTextButton)sender;
				if (ib != null)
				{
					proceed = true;
					this.chosenServerName = (string) (ib.Tag);
				}
			}

			if (proceed)
			{
				fillConfirmationControls();
				this.pnl_chooseServerPanel.Visible = false;
				this.pnl_confirmationPanel.Visible = true;
				this.newBtnOK.Visible = true;
				SetFocus();
			}
		}

		private bool CheckOrder(out ArrayList errs)
		{
			bool OpSuccess = true;
			errs = new ArrayList();

			//extract the data 
			this.requested_day = CONVERT.ParseIntSafe(tbDayValue.Text, 0);

			//===============================================================
			//==Check for any time based problems============================ 
			//===============================================================
			//Check that the day is valid (day>1 and Day<31)
			if (requested_day > MaxGameDays)
			{
				errs.Add("Requested Day is greater than " + CONVERT.ToStr(MaxGameDays));
				OpSuccess = false;
			}
			if (requested_day <= getcurrentDay())
			{
				errs.Add("Requested Day has passed");
				OpSuccess = false;
			}
			//Check that Day is clear 
			bool totallyFree = false;
			bool projects_allowed = false;

			OpsReader myOps = new OpsReader(this.MyNodeTree);
			myOps.isDayFree(requested_day, out totallyFree, out projects_allowed);
			myOps.Dispose();

			if (totallyFree==false)
			{
				errs.Add("Day has already been booked");
				OpSuccess = false;
			}

			Node projectsNode = this.MyNodeTree.GetNamedNode("pm_projects_running");
			foreach (Node prjnode in projectsNode.getChildren())
			{
				ProjectReader pr = new ProjectReader(prjnode);
				if (pr.getInstallDay() == requested_day)
				{
					errs.Add("Day has already been booked by Project " + pr.getProjectID());
					OpSuccess = false;
				}
			}
			//
			return OpSuccess;
		}

		private bool HandleOrder()
		{
			string upgrade_action = "upgrade_both";
			string memory_change = "0";
			string disk_change = "0";
			string money_change = "0"; // "1000";

			switch (chosenUpgradeTypeLetter)
			{
				case "M":
					upgrade_action = "upgrade_memory";
					memory_change = "2000";
					break;
				case "S":
					upgrade_action = "upgrade_disk";
					disk_change = "100";
					break;
				case "B":
					upgrade_action = "upgrade_both";
					memory_change = "2000";
					disk_change = "100";
					break;
			}

			//data matches the required entry data 
			ArrayList attrs = new ArrayList ();
			attrs.Add(new AttributeValuePair ("cmd_type", "request_opsupgrade"));
			attrs.Add(new AttributeValuePair ("action", upgrade_action));
			attrs.Add(new AttributeValuePair ("location", this.chosenServerName));
			attrs.Add(new AttributeValuePair ("day", CONVERT.ToStr(requested_day)));
			attrs.Add(new AttributeValuePair ("memory_change", memory_change));
			attrs.Add(new AttributeValuePair ("disk_change", disk_change));
			attrs.Add(new AttributeValuePair ("money_cost", money_change));
			
			//attrs.Add(new AttributeValuePair ("day", day_number));
			new Node (queueNode, "task", "", attrs);	
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
				MessageBox.Show(disp, "Error");
			}
		}

		private void newBtnCancel_Click(object sender, System.EventArgs e)
		{
			_mainPanel.DisposeEntryPanel();
		}

		public void SetFocus ()
		{
			Focus();
			tbDayValue.Focus();
		}
	}
}