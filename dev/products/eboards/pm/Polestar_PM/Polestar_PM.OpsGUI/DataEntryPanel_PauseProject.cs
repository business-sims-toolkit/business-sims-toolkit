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
	public class DataEntryPanel_PauseProject  : FlickerFreePanel
	{
		protected NodeTree MyNodeTree;
		protected IDataEntryControlHolder _mainPanel;

		protected Node queueNode;
		protected Hashtable existingProjectsBySlot = new Hashtable();

		protected Font MyDefaultSkinFontNormal8 = null;
		protected Font MyDefaultSkinFontNormal10 = null;
		protected Font MyDefaultSkinFontNormal12 = null;
		protected Font MyDefaultSkinFontBold8 = null;
		protected Font MyDefaultSkinFontBold10 = null;
		protected Font MyDefaultSkinFontBold12 = null;

		private ImageTextButton[] btnProjectsSlots = new ImageTextButton[7];

		private ImageTextButton newBtnOK = new ImageTextButton(0);
		private ImageTextButton newBtnCancel = new ImageTextButton(0);
		private ImageTextButton newBtnYes = new ImageTextButton(0);
		private ImageTextButton newBtnNo = new ImageTextButton(0);
		private ImageTextButton newBtnProject = new ImageTextButton(0);

		private System.Windows.Forms.Panel pnl_ChooseSlot;
		private System.Windows.Forms.Panel pnl_YesNoChoice;
		private System.Windows.Forms.Label titleLabel;
		private System.Windows.Forms.Label helpLabel;
		private System.Windows.Forms.Label projectLabel;

		private int SlotDisplay =0;
		private Node projectNodeToCancel = null;
		private string ChosenProject_NodeName;

		int round;
		string projectTerm;
		bool display_seven_projects = false;
		
		public DataEntryPanel_PauseProject (IDataEntryControlHolder mainPanel, NodeTree tree, NetworkProgressionGameFile gameFile)
		{
			MyNodeTree = tree;
			queueNode = tree.GetNamedNode("TaskManager");
			_mainPanel = mainPanel;

			round = gameFile.CurrentRound;
			projectTerm = SkinningDefs.TheInstance.GetData("project_term_round_" + LibCore.CONVERT.ToStr(round), "Project");

			Node projectsNode = tree.GetNamedNode("pm_projects_running");
			display_seven_projects = projectsNode.GetBooleanAttribute("display_seven_projects", false);

			BuildCurrentProjectLookupList();

			string fontname =  SkinningDefs.TheInstance.GetData("fontname");
			MyDefaultSkinFontNormal8 = ConstantSizeFont.NewFont(fontname,8);
			MyDefaultSkinFontNormal10 = ConstantSizeFont.NewFont(fontname,10);
			MyDefaultSkinFontNormal12 = ConstantSizeFont.NewFont(fontname,12);
			MyDefaultSkinFontBold8 = ConstantSizeFont.NewFont(fontname,8,FontStyle.Bold);
			MyDefaultSkinFontBold10 = ConstantSizeFont.NewFont(fontname,10,FontStyle.Bold);
			MyDefaultSkinFontBold12 = ConstantSizeFont.NewFont(fontname,12,FontStyle.Bold);

			this.BackgroundImage = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "images\\panels\\PM_opsback.png");

			//this.ShowInTaskbar = false;
			//this.ClientSize = new Size (520,280);
			//this.FormBorderStyle = FormBorderStyle.None;
			//this.Opacity = 1.00;
			
			this.Size = new Size (520,255);
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
			titleLabel.Text = "Pause / Resume " + projectTerm;
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
			projectLabel.Location = new System.Drawing.Point(3 + 5, 0 + 4);
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
			newBtnProject.ButtonFont = this.MyDefaultSkinFontBold12;
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

				//bool project_cancelled = (project_status == "cancelled");
				//bool project_precancelled = (project_status == "precancelled");
				//if ((project_cancelled==false)&(project_precancelled==false))
				//{
					existingProjectsBySlot.Add(project_slot, n);
				//}
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
			newBtnOK.Visible = false;
			this.Controls.Add(newBtnOK);
		}

		public void BuildSlotButtonControls() 
		{
			int offset_x=10;
			int button_width=50;
			int button_height=40;
			int button_sep=15;
			int number_of_Projects =6;

			if (this.display_seven_projects)
			{
				number_of_Projects = 7;
				button_width = 45;
				button_sep = 10;
			}

			for (int step = 0; step < number_of_Projects; step++)
			{
				string display_text = (step+1).ToString();  
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

		public void Build_YesNo_Controls() 
		{
			//newBtnYes.SetButton(AppInfo.TheInstance.Location+"\\images\\buttons\\button_70x25.png");
			newBtnYes.SetVariants("images\\buttons\\button_70x25.png");
			newBtnYes.Location = new System.Drawing.Point(20, 20);
			newBtnYes.Name = "newBtnYes";
			newBtnYes.Size = new System.Drawing.Size(70, 25);
			newBtnYes.TabIndex = 25;
			newBtnYes.ButtonFont = this.MyDefaultSkinFontBold10;
			//newBtn.ForeColor = System.Drawing.Color.Black;
			newBtnYes.SetButtonText("Yes",
				System.Drawing.Color.Black,System.Drawing.Color.Black,
				System.Drawing.Color.Green,System.Drawing.Color.Gray);
			//newBtn.ButtonPressed += new ProjectMngmntGamePanels.ImageButton.ImageButtonEventArgsHandler(playButton_ButtonPressed);
			newBtnYes.Click += new System.EventHandler(newBtnYes_Click);
			pnl_YesNoChoice.Controls.Add(newBtnYes);

			//newBtnNo.SetButton(AppInfo.TheInstance.Location+"\\images\\buttons\\button_70x25.png");
			newBtnNo.SetVariants("images\\buttons\\button_70x25.png");
			newBtnNo.Location = new System.Drawing.Point(135, 19);
			newBtnNo.Name = "newBtnNo";
			newBtnNo.Size = new System.Drawing.Size(70, 25);
			newBtnNo.TabIndex = 26;
			newBtnNo.ButtonFont = this.MyDefaultSkinFontBold10;
			//newBtn.ForeColor = System.Drawing.Color.Black;
			newBtnNo.SetButtonText("No",
				System.Drawing.Color.Black,System.Drawing.Color.Black,
				System.Drawing.Color.Green,System.Drawing.Color.Gray);
			//newBtn.ButtonPressed += new ProjectMngmntGamePanels.ImageButton.ImageButtonEventArgsHandler(playButton_ButtonPressed);
			newBtnNo.Click += new System.EventHandler(newBtnNo_Click);
			pnl_YesNoChoice.Controls.Add(newBtnNo);

		}


		public void BuildBaseControls() 
		{
			pnl_ChooseSlot = new System.Windows.Forms.Panel();
			pnl_YesNoChoice= new System.Windows.Forms.Panel();
			pnl_ChooseSlot.SuspendLayout();
			pnl_YesNoChoice.SuspendLayout();
			this.SuspendLayout();

			pnl_ChooseSlot.Location = new System.Drawing.Point(78, 50);
			pnl_ChooseSlot.Name = "pnl_ChooseSlot";
			pnl_ChooseSlot.Size = new System.Drawing.Size(230+90+80, 110);
			//pnl_ChooseSlot.BackColor = Color.FromArgb(176,196,222); //TODO SKIN
			pnl_ChooseSlot.BackColor = Color.Transparent;
			pnl_ChooseSlot.TabIndex = 13;

			pnl_YesNoChoice.Location = new System.Drawing.Point(90+90-22, 50);
			pnl_YesNoChoice.Name = "pnl_YesNoChoice";
			pnl_YesNoChoice.Size = new System.Drawing.Size(230, 70);
			//pnl_YesNoChoice.BackColor = Color.FromArgb(176,196,222); //TODO SKIN
			pnl_YesNoChoice.BackColor = Color.Transparent;
			pnl_YesNoChoice.TabIndex = 14;
			pnl_YesNoChoice.Visible = false;

			BuildSlotButtonControls();
			Build_YesNo_Controls();
			
			this.Controls.Add(pnl_ChooseSlot);
			this.Controls.Add(pnl_YesNoChoice);
			this.Name = "ForwardScheduleControl";
			this.Size = new System.Drawing.Size(520,280);
			this.pnl_ChooseSlot.ResumeLayout(false);
			this.pnl_YesNoChoice.ResumeLayout(false);
			this.ResumeLayout(false);
		}

		private void Slot_Button_Click(object sender, System.EventArgs e)
		{
			bool isPaused = false;

			//Extract out the FSC from the control that was used
			if (sender is ImageTextButton)
			{
				ImageTextButton ib = (ImageTextButton)sender;
				if (ib != null)
				{
					projectNodeToCancel= (Node)(ib.Tag);
					SlotDisplay = projectNodeToCancel.GetIntAttribute("slot",0);
					ChosenProject_NodeName = projectNodeToCancel.GetAttribute("name"); 

					//for each travel plan
					ProjectReader pr = new ProjectReader(projectNodeToCancel);
					if (pr.isPaused()|pr.isStatusRequestPreCancel())
					{
						isPaused= true;
					}
					pr.Dispose();

					string display_text = projectNodeToCancel.GetAttribute("project_id");
					helpLabel.Text = "Are you sure you want to pause " + projectTerm + " " + display_text + " ?";
					this.newBtnYes.Tag = "pause";
					if (isPaused)
					{
						helpLabel.Text = "Are you sure you want to resume " + projectTerm + " " + display_text + " ?";
						this.newBtnYes.Tag = "resume";
					}
					
					this.pnl_YesNoChoice.Visible = true;
					this.pnl_ChooseSlot.Visible = false;

					projectLabel.Visible = true;
					newBtnProject.SetButtonText(display_text,
						System.Drawing.Color.Black,System.Drawing.Color.Black,
						System.Drawing.Color.Green,System.Drawing.Color.Gray);
					newBtnProject.Visible = true;
				}
			}
		}

		private void newBtnOK_Click(object sender, System.EventArgs e)
		{
		}

		private void newBtnCancel_Click(object sender, System.EventArgs e)
		{
			_mainPanel.DisposeEntryPanel();
		}

		private bool CheckOrder(string which_operation,  out ArrayList errs)
		{
			bool good_order = true;
			errs = new ArrayList();
			//No checks just now
			return good_order;
		}
		private bool HandleOrder(string which_operation)
		{
			bool good_order = true;
			ArrayList attrs = new ArrayList ();

			ProjectReader pr = new ProjectReader(projectNodeToCancel);
			string desc = pr.getProjectTextDescription();
			pr.Dispose();

			if (which_operation.IndexOf("pause")>-1)
			{
				attrs.Add(new AttributeValuePair ("cmd_type", "pause_project"));
			}
			else
			{
				attrs.Add(new AttributeValuePair ("cmd_type", "resume_project"));
			}
			attrs.Add(new AttributeValuePair("nodedesc", desc));
			attrs.Add(new AttributeValuePair ("projectnodename", this.ChosenProject_NodeName));
			new Node (queueNode, "task", "", attrs);	
			return good_order;
		}

		private void newBtnYes_Click(object sender, System.EventArgs e)
		{
			bool good_order_placed = false;
			ArrayList errs = new ArrayList();

			string which_operation = (string) this.newBtnYes.Tag;

			good_order_placed = CheckOrder(which_operation, out errs);
			if (good_order_placed)
			{
				good_order_placed = HandleOrder(which_operation);
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
					disp = err + "\r\n";
				}
				MessageBox.Show(disp,"Error");			
			}
		}

		private void newBtnNo_Click(object sender, System.EventArgs e)
		{
			// No
			_mainPanel.DisposeEntryPanel();
		}

		private void newBtnProject_Click(object sender, System.EventArgs e)
		{
			this.pnl_YesNoChoice.Visible = false;
			this.pnl_ChooseSlot.Visible = true;
			projectLabel.Visible = false;
			newBtnProject.Visible = false;
		}

	}
}