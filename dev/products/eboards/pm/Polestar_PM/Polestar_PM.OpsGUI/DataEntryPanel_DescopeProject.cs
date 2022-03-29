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

using GameManagement;

namespace Polestar_PM.OpsGUI
{
	public class DataEntryPanel_DescopeProject : FlickerFreePanel
	{
		protected IDataEntryControlHolder _mainPanel;

		protected NodeTree MyNodeTree;
		protected Node queueNode;
		protected Hashtable existingProjectsBySlot = new Hashtable();

		protected Font MyDefaultSkinFontNormal8 = null;
		protected Font MyDefaultSkinFontNormal10 = null;
		protected Font MyDefaultSkinFontNormal12 = null;
		protected Font MyDefaultSkinFontBold8 = null;
		protected Font MyDefaultSkinFontBold10 = null;
		protected Font MyDefaultSkinFontBold12 = null;
		protected Font MyDefaultSkinFontBold20 = null;

		private ImageTextButton[] btnProjectsSlots = new ImageTextButton[7];

		private ImageTextButton newBtnOK = new ImageTextButton(0);
		private ImageTextButton newBtnCancel = new ImageTextButton(0);
		private ImageTextButton newBtnYes = new ImageTextButton(0);
		private ImageTextButton newBtnNo = new ImageTextButton(0);
		private ImageTextButton newBtnProject = new ImageTextButton(0);

		private ImageTextButton newBtnDrop90 = new ImageTextButton(0);
		private ImageTextButton newBtnDrop80 = new ImageTextButton(0);
		private ImageTextButton newBtnDrop70 = new ImageTextButton(0);
		private ImageTextButton newBtnDrop60 = new ImageTextButton(0);
		private ImageTextButton newBtnDrop50 = new ImageTextButton(0);
		private ImageTextButton newBtnDrop40 = new ImageTextButton(0);
		private ImageTextButton newBtnDrop30 = new ImageTextButton(0);
		private ImageTextButton newBtnDrop20 = new ImageTextButton(0);
		private ImageTextButton newBtnDrop10 = new ImageTextButton(0);

		private System.Windows.Forms.Panel pnl_ChooseSlot;
		private System.Windows.Forms.Panel pnl_DropPercent;
		private System.Windows.Forms.Panel pnl_DropNamed;
		private System.Windows.Forms.Label titleLabel;
		private System.Windows.Forms.Label helpLabel;
		private System.Windows.Forms.Label projectLabel;
		private System.Windows.Forms.Label projectDropPercentage;

		private int SlotDisplay =0;
		private Node projectNodeToDropTasks = null;
		private string ChosenProject_NodeName;
		private int chosen_drop_percentage=0;
		bool display_seven_projects = false;

		int round;
		string projectTerm;
		
		public DataEntryPanel_DescopeProject (IDataEntryControlHolder mainPanel, NodeTree tree, NetworkProgressionGameFile gameFile)
		{
			MyNodeTree = tree;
			_mainPanel = mainPanel;
			queueNode = tree.GetNamedNode("TaskManager");

			Node projectsNode = tree.GetNamedNode("pm_projects_running");
			display_seven_projects = projectsNode.GetBooleanAttribute("display_seven_projects", false);

			round = gameFile.CurrentRound;
			projectTerm = SkinningDefs.TheInstance.GetData("project_term_round_" + LibCore.CONVERT.ToStr(round), "Project");

			BuildCurrentProjectLookupList();

			string fontname =  SkinningDefs.TheInstance.GetData("fontname");
			MyDefaultSkinFontNormal8 = ConstantSizeFont.NewFont(fontname,8);
			MyDefaultSkinFontNormal10 = ConstantSizeFont.NewFont(fontname,10);
			MyDefaultSkinFontNormal12 = ConstantSizeFont.NewFont(fontname,12);
			MyDefaultSkinFontBold8 = ConstantSizeFont.NewFont(fontname,8,FontStyle.Bold);
			MyDefaultSkinFontBold10 = ConstantSizeFont.NewFont(fontname,10,FontStyle.Bold);
			MyDefaultSkinFontBold12 = ConstantSizeFont.NewFont(fontname,12,FontStyle.Bold);
			MyDefaultSkinFontBold20 = ConstantSizeFont.NewFont(fontname,20,FontStyle.Bold);

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
			titleLabel.Text = "Descope " + projectTerm;
			titleLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.Controls.Add(titleLabel);

			helpLabel = new System.Windows.Forms.Label();
			helpLabel.BackColor = System.Drawing.Color.Transparent;
			helpLabel.Font = MyDefaultSkinFontNormal10;
			helpLabel.ForeColor = System.Drawing.Color.White;
			helpLabel.Location = new System.Drawing.Point(110-25, 50-20-1);
			helpLabel.Name = "helpLabel";
			helpLabel.Size = new System.Drawing.Size(380,18);
			helpLabel.TabIndex = 20;
			helpLabel.Text = "Select " + SkinningDefs.TheInstance.GetData("project_term_round_" + LibCore.CONVERT.ToStr(round), "Project") + " Number ";
			helpLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.Controls.Add(helpLabel);

			projectLabel = new System.Windows.Forms.Label();
			projectLabel.BackColor = System.Drawing.Color.Transparent;
			projectLabel.Font = MyDefaultSkinFontNormal10;
			projectLabel.ForeColor = System.Drawing.Color.Black;
			projectLabel.Location = new System.Drawing.Point(3 + 5, 10);
			projectLabel.Name = "titleLabel";
			projectLabel.Size = new System.Drawing.Size(70, 20);
			projectLabel.TabIndex = 11;
			projectLabel.Text = projectTerm;
			projectLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			projectLabel.Visible = false;
			this.Controls.Add(projectLabel);

			newBtnProject.SetButton(AppInfo.TheInstance.Location+"\\images\\buttons\\selection_tab.png");
			newBtnProject.Location = new System.Drawing.Point(15, 30);
			newBtnProject.Name = "newBtnNo";
			newBtnProject.Size = new System.Drawing.Size(65, 27);
			newBtnProject.TabIndex = 26;
			newBtnProject.ButtonFont = this.MyDefaultSkinFontBold10;
			newBtnProject.SetButtonText("No",
				System.Drawing.Color.Black,System.Drawing.Color.Black,
				System.Drawing.Color.Green,System.Drawing.Color.Gray);
			newBtnProject.Click += new System.EventHandler(newBtnProject_Click);
			newBtnProject.Text = "";
			newBtnProject.Visible = false; 
			this.Controls.Add(newBtnProject);

			pnl_ChooseSlot.Visible = true;
		}

		new public void Dispose()
		{
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
			if (MyDefaultSkinFontBold20 != null)
			{
				MyDefaultSkinFontBold20.Dispose();
				MyDefaultSkinFontBold20 = null;
			}
		}

		private void BuildCurrentProjectLookupList()
		{
			Hashtable ht = new Hashtable();
			ArrayList types = new ArrayList();
			
			existingProjectsBySlot.Clear();

			types.Clear();
			types.Add("project");
			ht = this.MyNodeTree.GetNodesOfAttribTypes(types);
			foreach (Node n in ht.Keys)
			{
				//for each travel plan
				string project_name = n.GetAttribute("name");
				string project_slot = n.GetAttribute("slot");
				string project_status = n.GetAttribute("status");

				//				bool project_cancelled = (project_status == "cancelled");
				//				bool project_precancelled = (project_status == "precancelled");
				//
				//				if ((project_cancelled==false)&(project_precancelled==false))
				//				{
				existingProjectsBySlot.Add(project_slot, n);
				//				}
			}
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
			int offset_x=10;
			int button_width=50;
			int button_height=40;
			int button_sep=15;
			int number_of_Projects = 6;

			if (this.display_seven_projects)
			{
				number_of_Projects = 7;
				button_width = 45;
				button_sep = 10;
			}

			for (int step = 0; step < number_of_Projects; step++)
			{
				string display_text = CONVERT.ToStr(step+1);  
				bool project_active = false;
				Node project_node = null;
				//determine the 
				if (existingProjectsBySlot.ContainsKey(step.ToString()))
				{
					project_node = (Node) existingProjectsBySlot[step.ToString()];
					display_text = project_node.GetAttribute("project_id");
					project_active = true;
				}

				//Build the button 
				btnProjectsSlots[step] = new ImageTextButton(0);
				//btnProjectsSlots[step].SetButton(AppInfo.TheInstance.Location+"\\images\\buttons\\button_50x40.png");
				btnProjectsSlots[step].SetVariants("images\\buttons\\button_50x40.png");
				btnProjectsSlots[step].Location = new System.Drawing.Point(offset_x+(button_width+button_sep)*(step), 10);
				btnProjectsSlots[step].Name = "Button1";
				btnProjectsSlots[step].Size = new System.Drawing.Size(button_width, button_height);
				btnProjectsSlots[step].TabIndex = 8;
				btnProjectsSlots[step].ButtonFont = MyDefaultSkinFontBold8;
				btnProjectsSlots[step].Tag = project_node;
				btnProjectsSlots[step].SetButtonText(display_text,
					System.Drawing.Color.Black,System.Drawing.Color.Black,
					System.Drawing.Color.Green,System.Drawing.Color.Gray);
				btnProjectsSlots[step].Click += new System.EventHandler(this.Slot_Button_Click);
				btnProjectsSlots[step].Enabled = project_active;
				this.pnl_ChooseSlot.Controls.Add(btnProjectsSlots[step]);
			}
		}

		public void Build_DescopePercentage_Controls() 
		{
			int stepx=70;
			int stepy=50;

			projectDropPercentage = new System.Windows.Forms.Label();
			projectDropPercentage.BackColor = System.Drawing.Color.Transparent;
			projectDropPercentage.Font = MyDefaultSkinFontBold20;
			projectDropPercentage.ForeColor = System.Drawing.Color.Black;
			projectDropPercentage.Location = new System.Drawing.Point((5+stepx*3)+75-10, (5+stepy*1));
			projectDropPercentage.Name = "titleLabel";
			projectDropPercentage.Size = new System.Drawing.Size(110, 40);
			projectDropPercentage.TabIndex = 11;
			projectDropPercentage.Text = "0%";
			projectDropPercentage.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			projectDropPercentage.Visible = true;
			//projectDropPercentage.BackColor = Color.Violet;
			this.pnl_DropPercent.Controls.Add(projectDropPercentage);

			//newBtnDrop90.SetButton(AppInfo.TheInstance.Location+"\\images\\buttons\\button_50x40.png");
			newBtnDrop90.SetVariants("images\\buttons\\button_50x40.png");
			newBtnDrop90.Location = new System.Drawing.Point((5 + stepx * 0), (5 + stepy * 0));
			newBtnDrop90.Name = "newBtnDrop90";
			newBtnDrop90.Size = new System.Drawing.Size(50, 40);
			newBtnDrop90.TabIndex = 22;
			newBtnDrop90.ButtonFont = MyDefaultSkinFontBold10;
			//newBtn.ForeColor = System.Drawing.Color.Black;
			newBtnDrop90.SetButtonText("90%",
				System.Drawing.Color.Black, System.Drawing.Color.Black,
				System.Drawing.Color.Green, System.Drawing.Color.Gray);
			//newBtn.ButtonPressed += new ProjectMngmntGamePanels.ImageButton.ImageButtonEventArgsHandler(playButton_ButtonPressed);
			newBtnDrop90.Click += new System.EventHandler(this.newBtnDrop90_Click);
			this.pnl_DropPercent.Controls.Add(newBtnDrop90);


			//newBtnDrop80.SetButton(AppInfo.TheInstance.Location+"\\images\\buttons\\button_50x40.png");
			newBtnDrop80.SetVariants("images\\buttons\\button_50x40.png");
			newBtnDrop80.Location = new System.Drawing.Point((5 + stepx * 1), (5 + stepy * 0));
			newBtnDrop80.Name = "newBtnDrop80";
			newBtnDrop80.Size = new System.Drawing.Size(50, 40);
			newBtnDrop80.TabIndex = 22;
			newBtnDrop80.ButtonFont = MyDefaultSkinFontBold10;
			//newBtn.ForeColor = System.Drawing.Color.Black;
			newBtnDrop80.SetButtonText("80%",
				System.Drawing.Color.Black, System.Drawing.Color.Black,
				System.Drawing.Color.Green, System.Drawing.Color.Gray);
			//newBtn.ButtonPressed += new ProjectMngmntGamePanels.ImageButton.ImageButtonEventArgsHandler(playButton_ButtonPressed);
			newBtnDrop80.Click += new System.EventHandler(this.newBtnDrop80_Click);
			this.pnl_DropPercent.Controls.Add(newBtnDrop80);

			//newBtnDrop70.SetButton(AppInfo.TheInstance.Location+"\\images\\buttons\\button_50x40.png");
			newBtnDrop70.SetVariants("images\\buttons\\button_50x40.png");
			newBtnDrop70.Location = new System.Drawing.Point((5 + stepx * 2), (5 + stepy * 0));
			newBtnDrop70.Name = "newBtnDrop70";
			newBtnDrop70.Size = new System.Drawing.Size(50, 40);
			newBtnDrop70.TabIndex = 22;
			newBtnDrop70.ButtonFont = MyDefaultSkinFontBold10;
			//newBtn.ForeColor = System.Drawing.Color.Black;
			newBtnDrop70.SetButtonText("70%",
				System.Drawing.Color.Black, System.Drawing.Color.Black,
				System.Drawing.Color.Green, System.Drawing.Color.Gray);
			//newBtn.ButtonPressed += new ProjectMngmntGamePanels.ImageButton.ImageButtonEventArgsHandler(playButton_ButtonPressed);
			newBtnDrop70.Click += new System.EventHandler(this.newBtnDrop70_Click);
			this.pnl_DropPercent.Controls.Add(newBtnDrop70);

			//newBtnDrop60.SetButton(AppInfo.TheInstance.Location+"\\images\\buttons\\button_50x40.png");
			newBtnDrop60.SetVariants("images\\buttons\\button_50x40.png");
			newBtnDrop60.Location = new System.Drawing.Point((5 + stepx * 0), (5 + stepy * 1));
			newBtnDrop60.Name = "newBtnDrop0";
			newBtnDrop60.Size = new System.Drawing.Size(50, 40);
			newBtnDrop60.TabIndex = 22;
			newBtnDrop60.ButtonFont = MyDefaultSkinFontBold10;
			//newBtn.ForeColor = System.Drawing.Color.Black;
			newBtnDrop60.SetButtonText("60%",
				System.Drawing.Color.Black, System.Drawing.Color.Black,
				System.Drawing.Color.Green, System.Drawing.Color.Gray);
			//newBtn.ButtonPressed += new ProjectMngmntGamePanels.ImageButton.ImageButtonEventArgsHandler(playButton_ButtonPressed);
			newBtnDrop60.Click += new System.EventHandler(this.newBtnDrop60_Click);
			this.pnl_DropPercent.Controls.Add(newBtnDrop60);

			//newBtnDrop50.SetButton(AppInfo.TheInstance.Location+"\\images\\buttons\\button_50x40.png");
			newBtnDrop50.SetVariants("images\\buttons\\button_50x40.png");
			newBtnDrop50.Location = new System.Drawing.Point((5 + stepx * 1), (5 + stepy * 1));
			newBtnDrop50.Name = "newBtnDrop0";
			newBtnDrop50.Size = new System.Drawing.Size(50, 40);
			newBtnDrop50.TabIndex = 22;
			newBtnDrop50.ButtonFont = MyDefaultSkinFontBold10;
			//newBtn.ForeColor = System.Drawing.Color.Black;
			newBtnDrop50.SetButtonText("50%",
				System.Drawing.Color.Black, System.Drawing.Color.Black,
				System.Drawing.Color.Green, System.Drawing.Color.Gray);
			//newBtn.ButtonPressed += new ProjectMngmntGamePanels.ImageButton.ImageButtonEventArgsHandler(playButton_ButtonPressed);
			newBtnDrop50.Click += new System.EventHandler(this.newBtnDrop50_Click);
			this.pnl_DropPercent.Controls.Add(newBtnDrop50);

			//newBtnDrop40.SetButton(AppInfo.TheInstance.Location+"\\images\\buttons\\button_50x40.png");
			newBtnDrop40.SetVariants("images\\buttons\\button_50x40.png");
			newBtnDrop40.Location = new System.Drawing.Point((5 + stepx * 2), (5 + stepy * 1));
			newBtnDrop40.Name = "newBtnDrop0";
			newBtnDrop40.Size = new System.Drawing.Size(50, 40);
			newBtnDrop40.TabIndex = 22;
			newBtnDrop40.ButtonFont = MyDefaultSkinFontBold10;
			//newBtn.ForeColor = System.Drawing.Color.Black;
			newBtnDrop40.SetButtonText("40%",
				System.Drawing.Color.Black, System.Drawing.Color.Black,
				System.Drawing.Color.Green, System.Drawing.Color.Gray);
			//newBtn.ButtonPressed += new ProjectMngmntGamePanels.ImageButton.ImageButtonEventArgsHandler(playButton_ButtonPressed);
			newBtnDrop40.Click += new System.EventHandler(this.newBtnDrop40_Click);
			this.pnl_DropPercent.Controls.Add(newBtnDrop40);

			//newBtnDrop30.SetButton(AppInfo.TheInstance.Location+"\\images\\buttons\\button_50x40.png");
			newBtnDrop30.SetVariants("images\\buttons\\button_50x40.png");
			newBtnDrop30.Location = new System.Drawing.Point((5 + stepx * 0), (5 + stepy * 2));
			newBtnDrop30.Name = "newBtnDrop0";
			newBtnDrop30.Size = new System.Drawing.Size(50, 40);
			newBtnDrop30.TabIndex = 22;
			newBtnDrop30.ButtonFont = MyDefaultSkinFontBold10;
			//newBtn.ForeColor = System.Drawing.Color.Black;
			newBtnDrop30.SetButtonText("30%",
				System.Drawing.Color.Black, System.Drawing.Color.Black,
				System.Drawing.Color.Green, System.Drawing.Color.Gray);
			//newBtn.ButtonPressed += new ProjectMngmntGamePanels.ImageButton.ImageButtonEventArgsHandler(playButton_ButtonPressed);
			newBtnDrop30.Click += new System.EventHandler(this.newBtnDrop30_Click);
			this.pnl_DropPercent.Controls.Add(newBtnDrop30);

			//newBtnDrop20.SetButton(AppInfo.TheInstance.Location+"\\images\\buttons\\button_50x40.png");
			newBtnDrop20.SetVariants("images\\buttons\\button_50x40.png");
			newBtnDrop20.Location = new System.Drawing.Point((5 + stepx * 1), (5 + stepy * 2));
			newBtnDrop20.Name = "newBtnDrop0";
			newBtnDrop20.Size = new System.Drawing.Size(50, 40);
			newBtnDrop20.TabIndex = 22;
			newBtnDrop20.ButtonFont = MyDefaultSkinFontBold10;
			//newBtn.ForeColor = System.Drawing.Color.Black;
			newBtnDrop20.SetButtonText("20%",
				System.Drawing.Color.Black, System.Drawing.Color.Black,
				System.Drawing.Color.Green, System.Drawing.Color.Gray);
			//newBtn.ButtonPressed += new ProjectMngmntGamePanels.ImageButton.ImageButtonEventArgsHandler(playButton_ButtonPressed);
			newBtnDrop20.Click += new System.EventHandler(this.newBtnDrop20_Click);
			this.pnl_DropPercent.Controls.Add(newBtnDrop20);

			//newBtnDrop10.SetButton(AppInfo.TheInstance.Location+"\\images\\buttons\\button_50x40.png");
			newBtnDrop10.SetVariants("images\\buttons\\button_50x40.png");
			newBtnDrop10.Location = new System.Drawing.Point((5 + stepx * 2), (5 + stepy * 2));
			newBtnDrop10.Name = "newBtnDrop0";
			newBtnDrop10.Size = new System.Drawing.Size(50, 40);
			newBtnDrop10.TabIndex = 22;
			newBtnDrop10.ButtonFont = MyDefaultSkinFontBold10;
			//newBtn.ForeColor = System.Drawing.Color.Black;
			newBtnDrop10.SetButtonText("10%",
				System.Drawing.Color.Black,System.Drawing.Color.Black,
				System.Drawing.Color.Green,System.Drawing.Color.Gray);
			//newBtn.ButtonPressed += new ProjectMngmntGamePanels.ImageButton.ImageButtonEventArgsHandler(playButton_ButtonPressed);
			newBtnDrop10.Click += new System.EventHandler(this.newBtnDrop10_Click);
			this.pnl_DropPercent.Controls.Add(newBtnDrop10);
		}

		public void Build_DescopeNamed_Controls()
		{

		}

		public void BuildBaseControls() 
		{
			pnl_ChooseSlot = new System.Windows.Forms.Panel();
			pnl_DropPercent = new System.Windows.Forms.Panel();
			pnl_DropNamed = new System.Windows.Forms.Panel();
			pnl_ChooseSlot.SuspendLayout();
			pnl_DropPercent.SuspendLayout();
			pnl_DropNamed.SuspendLayout();
			this.SuspendLayout();

			pnl_ChooseSlot.Location = new System.Drawing.Point(78, 50);
			pnl_ChooseSlot.Name = "pnl_ChooseSlot";
			pnl_ChooseSlot.Size = new System.Drawing.Size(390, 110);
			//pnl_ChooseSlot.BackColor = Color.FromArgb(176,196,222); //TODO SKIN
			pnl_ChooseSlot.BackColor = Color.Transparent;
			//pnl_ChooseSlot.BackColor = Color.Violet;
			pnl_ChooseSlot.TabIndex = 13;

			pnl_DropPercent.Location = new System.Drawing.Point(78, 50);
			pnl_DropPercent.Name = "pnl_YesNoChoice";
			pnl_DropPercent.Size = new System.Drawing.Size(390, 150);
			//pnl_DropPercent.BackColor = Color.FromArgb(176,196,222); //TODO SKIN
			pnl_DropPercent.BackColor = Color.Transparent;
			//pnl_DropPercent.BackColor = Color.GreenYellow;
			pnl_DropPercent.TabIndex = 14;
			pnl_DropPercent.Visible = false;

			pnl_DropNamed.Location = new System.Drawing.Point(78, 50);
			pnl_DropNamed.Name = "pnl_YesNoChoice";
			pnl_DropNamed.Size = new System.Drawing.Size(390, 120);
			//pnl_DropNamed.BackColor = Color.FromArgb(176,196,222); //TODO SKIN
			pnl_DropNamed.BackColor = Color.Transparent;
			//pnl_DropNamed.BackColor = Color.Turquoise;
			pnl_DropNamed.TabIndex = 14;
			pnl_DropNamed.Visible = false;

			BuildSlotButtonControls();
			Build_DescopePercentage_Controls();
			Build_DescopeNamed_Controls();
			
			this.Controls.Add(pnl_ChooseSlot);
			this.Controls.Add(pnl_DropPercent);
			this.Controls.Add(pnl_DropNamed);
			this.Name = "ForwardScheduleControl";
			this.Size = new System.Drawing.Size(520,280);
			this.pnl_ChooseSlot.ResumeLayout(false);
			this.pnl_DropPercent.ResumeLayout(false);
			this.pnl_DropNamed.ResumeLayout(false);
			this.ResumeLayout(false);
		}

		private void setButtonEnabled(ImageTextButton itb, int level, int currentScope)
		{
			if (currentScope > level)
			{
				itb.Enabled = true;
			}
			else
			{
				itb.Enabled = false;
			}
		}

		private void Slot_Button_Click(object sender, System.EventArgs e)
		{
			//Extract out the FSC from the control that was used
			if (sender is ImageTextButton)
			{
				ImageTextButton ib = (ImageTextButton)sender;
				if (ib != null)
				{
					projectNodeToDropTasks = (Node)(ib.Tag);
					SlotDisplay = projectNodeToDropTasks.GetIntAttribute("slot",0);
					ChosenProject_NodeName = projectNodeToDropTasks.GetAttribute("name"); 
					string display_text = projectNodeToDropTasks.GetAttribute("project_id");
					
					helpLabel.Text = "Select New Scope";

					ProjectReader pr = new ProjectReader(projectNodeToDropTasks);
					int current_scope = pr.getProjectScope();
					pr.Dispose();

					setButtonEnabled(newBtnDrop90, 90, current_scope);
					setButtonEnabled(newBtnDrop80, 80, current_scope);
					setButtonEnabled(newBtnDrop70, 70, current_scope);
					setButtonEnabled(newBtnDrop60, 60, current_scope);
					setButtonEnabled(newBtnDrop50, 50, current_scope);
					setButtonEnabled(newBtnDrop40, 40, current_scope);
					setButtonEnabled(newBtnDrop30, 30, current_scope);
					setButtonEnabled(newBtnDrop20, 20, current_scope);
					setButtonEnabled(newBtnDrop10, 10, current_scope);

					//Need to determine if we have already Descoped 
					//If so them we need to disable some buttons 	

					//Now swap over the the Correct panel for the selection of how much 
					this.pnl_ChooseSlot.Visible = false;
					this.pnl_DropPercent.Visible = true;
					this.newBtnCancel.Visible = true;
					//this.pnl_YesNoChoice.Visible = true;
					
					//show the project return button 
					projectLabel.Visible = true;
					newBtnProject.SetButtonText(display_text,
						System.Drawing.Color.Black,System.Drawing.Color.Black,
						System.Drawing.Color.Green,System.Drawing.Color.Gray);
					newBtnProject.Visible = true;
				}
			}
		}

		private bool CheckOrder(string ChosenProject_NodeName, out ArrayList errs)
		{
			bool OpSuccess = false;
			errs = new ArrayList();
			string ErrMsg = "";
			
			ProjectReader pr = new ProjectReader(projectNodeToDropTasks);

			if (pr.CanProjectBeDeScoped_ByPercent(this.round,out ErrMsg))
			{
				OpSuccess = true;
			}
			else
			{
				errs.Add(ErrMsg);
			}
			pr.Dispose();
			return OpSuccess;
		}

		private bool HandleOrder()
		{
			bool OpSuccess = true;

			ProjectReader pr = new ProjectReader(projectNodeToDropTasks);
			string desc = pr.getProjectTextDescription();
			pr.Dispose();

			ArrayList attrs = new ArrayList ();
			attrs.Add(new AttributeValuePair ("cmd_type", "droptasks_project"));
			attrs.Add(new AttributeValuePair ("nodename", ChosenProject_NodeName));
			attrs.Add(new AttributeValuePair ("nodedesc", desc));
			attrs.Add(new AttributeValuePair ("droppercent", CONVERT.ToStr(chosen_drop_percentage)));
			new Node (queueNode, "task", "", attrs);	

			return OpSuccess;
		}

		private void newBtnOK_Click(object sender, System.EventArgs e)
		{
			bool good_order_placed = false;
			ArrayList errs = null;

			good_order_placed = CheckOrder(ChosenProject_NodeName, out errs);
			if (good_order_placed)
			{
				good_order_placed = HandleOrder();
				if (good_order_placed)
				{
					_mainPanel.DisposeEntryPanel();
				}
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

		private void setDropPercentage(int drop_value)
		{
			projectDropPercentage.Text = (drop_value.ToString())+"%";
			chosen_drop_percentage = drop_value;
			this.newBtnOK.Visible = true;
			this.newBtnOK.Enabled = true;
		}

		private void newBtnDrop0_Click(object sender, System.EventArgs e)
		{
			setDropPercentage(0);
		}
		private void newBtnDrop10_Click(object sender, System.EventArgs e)
		{
			setDropPercentage(10);
		}
		private void newBtnDrop20_Click(object sender, System.EventArgs e)
		{
			setDropPercentage(20);
		}
		private void newBtnDrop30_Click(object sender, System.EventArgs e)
		{
			setDropPercentage(30);
		}
		private void newBtnDrop40_Click(object sender, System.EventArgs e)
		{
			setDropPercentage(40);
		}
		private void newBtnDrop50_Click(object sender, System.EventArgs e)
		{
			setDropPercentage(50);
		}
		private void newBtnDrop60_Click(object sender, System.EventArgs e)
		{
			setDropPercentage(60);
		}
		private void newBtnDrop70_Click(object sender, System.EventArgs e)
		{
			setDropPercentage(70);
		}
		private void newBtnDrop80_Click(object sender, System.EventArgs e)
		{
			setDropPercentage(80);
		}
		private void newBtnDrop90_Click(object sender, System.EventArgs e)
		{
			setDropPercentage(90);
		}
		private void newBtnDrop100_Click(object sender, System.EventArgs e)
		{
			setDropPercentage(100);
		}

		private void newBtnProject_Click(object sender, System.EventArgs e)
		{
			pnl_DropPercent.Visible = false;
			pnl_ChooseSlot.Visible = true;
			projectLabel.Visible = false;
			newBtnProject.Visible = false;
			
			this.newBtnOK.Visible = true;
			this.newBtnOK.Enabled = false;
		}

	}
}