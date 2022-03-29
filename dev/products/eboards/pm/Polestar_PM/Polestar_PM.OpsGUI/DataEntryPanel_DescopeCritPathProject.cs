using System;
using System.Collections;
using System.Collections.Generic;
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
	/// <summary>
	/// This is a specialist popup dealing with the Critical Path Descoping 
	/// 
	/// </summary>
	public class DataEntryPanel_DescopeCritPathProject : FlickerFreePanel
	{
		protected IDataEntryControlHolder _mainPanel;

		protected NodeTree MyNodeTree;
		protected Node queueNode;
		protected Hashtable existingProjectsBySlot = new Hashtable();
		//protected Hashtable currentTaskBlocks = new Hashtable();
		protected Hashtable currentChangeableTaskBlocks = new Hashtable();
		Dictionary<string, CheckBox> taskNameToCheckBox = new Dictionary<string, CheckBox> ();

		protected Font MyDefaultSkinFontNormal8 = null;
		protected Font MyDefaultSkinFontNormal10 = null;
		protected Font MyDefaultSkinFontNormal12 = null;
		protected Font MyDefaultSkinFontBold8 = null;
		protected Font MyDefaultSkinFontBold10 = null;
		protected Font MyDefaultSkinFontBold12 = null;
		protected Font MyDefaultSkinFontBold20 = null;

		private ImageTextButton[] btnProjectsSlots = new ImageTextButton[7];

		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.CheckBox taskB1_CheckBox;
		private System.Windows.Forms.CheckBox taskB2_CheckBox;
		private System.Windows.Forms.CheckBox taskB3_CheckBox;
		private System.Windows.Forms.CheckBox taskD1_CheckBox;
		private System.Windows.Forms.CheckBox taskD2_CheckBox;
		private System.Windows.Forms.CheckBox taskD3_CheckBox;

		private ImageTextButton newBtnOK = new ImageTextButton(0);
		private ImageTextButton newBtnCancel = new ImageTextButton(0);
		private ImageTextButton newBtnYes = new ImageTextButton(0);
		private ImageTextButton newBtnNo = new ImageTextButton(0);
		private ImageTextButton newBtnProject = new ImageTextButton(0);

		private System.Windows.Forms.Panel pnl_ChooseSlot;
		private System.Windows.Forms.Panel pnl_DropNamedSubTask;
		//private System.Windows.Forms.Panel pnl_DropNamed;
		private System.Windows.Forms.Label titleLabel;
		private System.Windows.Forms.Label helpLabel;
		private System.Windows.Forms.Label projectLabel;
		//private System.Windows.Forms.Label projectDropPercentage;

		private int SlotDisplay =0;
		private Node projectNodeToDropTasks = null;
		private string ChosenProject_NodeName;
		bool display_seven_projects = false;

		int round;
		string projectTerm;
		
		public DataEntryPanel_DescopeCritPathProject (IDataEntryControlHolder mainPanel, NodeTree tree, 
			NetworkProgressionGameFile gameFile)
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
			titleLabel.Text = "Descope Tasks in Phases B and D";
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
			helpLabel.Text = "Select Project Number";
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
			string ErrMsg = "";
			
			existingProjectsBySlot.Clear();

			types.Clear();
			types.Add("project");
			ht = this.MyNodeTree.GetNodesOfAttribTypes(types);
			foreach (Node n in ht.Keys)
			{
				ProjectReader pr = new ProjectReader(n);
				if (pr.CanProjectBeDeScoped_ByCritPath(out ErrMsg))
				{
					//for each project
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
				pr.Dispose();
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

		public void Build_DescopeNamed_Controls()
		{
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.taskB3_CheckBox = new System.Windows.Forms.CheckBox();
			this.taskB2_CheckBox = new System.Windows.Forms.CheckBox();
			this.taskB1_CheckBox = new System.Windows.Forms.CheckBox();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.taskD3_CheckBox = new System.Windows.Forms.CheckBox();
			this.taskD2_CheckBox = new System.Windows.Forms.CheckBox();
			this.taskD1_CheckBox = new System.Windows.Forms.CheckBox();
			this.groupBox1.SuspendLayout();
			this.groupBox2.SuspendLayout();
			this.SuspendLayout();
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.taskB3_CheckBox);
			this.groupBox1.Controls.Add(this.taskB2_CheckBox);
			this.groupBox1.Controls.Add(this.taskB1_CheckBox);
			this.groupBox1.Font = MyDefaultSkinFontNormal10;
			this.groupBox1.Location = new System.Drawing.Point(20, 0);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(170, 120);
			this.groupBox1.TabIndex = 0;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Phase B Tasks";
			// 
			// taskB3_CheckBox
			// 
			this.taskB3_CheckBox.Font = MyDefaultSkinFontNormal10;
			this.taskB3_CheckBox.Location = new System.Drawing.Point(10, 90);
			this.taskB3_CheckBox.Name = "taskB3_CheckBox";
			this.taskB3_CheckBox.Size = new System.Drawing.Size(150, 20);
			this.taskB3_CheckBox.TabIndex = 2;
			this.taskB3_CheckBox.Text = "Task 1 [M]";
			this.taskB3_CheckBox.CheckedChanged += new System.EventHandler(this.taskB3_CheckBox_CheckedChanged);
			// 
			// taskB2_CheckBox
			// 
			this.taskB2_CheckBox.Font = MyDefaultSkinFontNormal10;
			this.taskB2_CheckBox.Location = new System.Drawing.Point(10, 60);
			this.taskB2_CheckBox.Name = "taskB2_CheckBox";
			this.taskB2_CheckBox.Size = new System.Drawing.Size(150, 20);
			this.taskB2_CheckBox.TabIndex = 1;
			this.taskB2_CheckBox.Text = "Task 1 [M]";
			this.taskB2_CheckBox.CheckedChanged += new System.EventHandler(this.taskB2_CheckBox_CheckedChanged);
			// 
			// taskB1_CheckBox
			// 
			this.taskB1_CheckBox.Font = MyDefaultSkinFontNormal10;
			this.taskB1_CheckBox.Location = new System.Drawing.Point(10, 30);
			this.taskB1_CheckBox.Name = "taskB1_CheckBox";
			this.taskB1_CheckBox.Size = new System.Drawing.Size(150, 20);
			this.taskB1_CheckBox.TabIndex = 0;
			this.taskB1_CheckBox.Text = "Task 1 [M]";
			this.taskB1_CheckBox.CheckedChanged += new System.EventHandler(this.taskB1_CheckBox_CheckedChanged);
			// 
			// groupBox2
			// 
			this.groupBox2.Controls.Add(this.taskD3_CheckBox);
			this.groupBox2.Controls.Add(this.taskD2_CheckBox);
			this.groupBox2.Controls.Add(this.taskD1_CheckBox);
			this.groupBox2.Font = MyDefaultSkinFontNormal10;
			this.groupBox2.Location = new System.Drawing.Point(210, 0);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(170, 120);
			this.groupBox2.TabIndex = 1;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "Phase D Tasks";
			// 
			// taskD3_CheckBox
			// 
			this.taskD3_CheckBox.Font = MyDefaultSkinFontNormal10;
			this.taskD3_CheckBox.Location = new System.Drawing.Point(10, 90);
			this.taskD3_CheckBox.Name = "taskD3_CheckBox";
			this.taskD3_CheckBox.Size = new System.Drawing.Size(150, 20);
			this.taskD3_CheckBox.TabIndex = 3;
			this.taskD3_CheckBox.Text = "Task 1 [M]";
			this.taskD3_CheckBox.CheckedChanged += new System.EventHandler(this.taskD3_CheckBox_CheckedChanged);
			// 
			// taskD2_CheckBox
			// 
			this.taskD2_CheckBox.Font = MyDefaultSkinFontNormal10;
			this.taskD2_CheckBox.Location = new System.Drawing.Point(10, 60);
			this.taskD2_CheckBox.Name = "taskD2_CheckBox";
			this.taskD2_CheckBox.Size = new System.Drawing.Size(150, 20);
			this.taskD2_CheckBox.TabIndex = 2;
			this.taskD2_CheckBox.Text = "Task 1 [M]";
			this.taskD2_CheckBox.CheckedChanged += new System.EventHandler(this.taskD2_CheckBox_CheckedChanged);
			// 
			// taskD1_CheckBox
			// 
			this.taskD1_CheckBox.Font = MyDefaultSkinFontNormal10;
			this.taskD1_CheckBox.Location = new System.Drawing.Point(10, 30);
			this.taskD1_CheckBox.Name = "taskD1_CheckBox";
			this.taskD1_CheckBox.Size = new System.Drawing.Size(150, 20);
			this.taskD1_CheckBox.TabIndex = 1;
			this.taskD1_CheckBox.Text = "Task 1 [M]";
			this.taskD1_CheckBox.CheckedChanged += new System.EventHandler(this.taskD1_CheckBox_CheckedChanged);
			// 
			// CritPathControl
			// 
			this.pnl_DropNamedSubTask.Controls.Add(this.groupBox2);
			this.pnl_DropNamedSubTask.Controls.Add(this.groupBox1);
			this.groupBox1.ResumeLayout(false);
			this.groupBox2.ResumeLayout(false);
			this.ResumeLayout(false);
		}

		public void BuildBaseControls() 
		{
			pnl_ChooseSlot = new System.Windows.Forms.Panel();
			pnl_DropNamedSubTask = new System.Windows.Forms.Panel();
			pnl_ChooseSlot.SuspendLayout();
			pnl_DropNamedSubTask.SuspendLayout();
			this.SuspendLayout();

			pnl_ChooseSlot.Location = new System.Drawing.Point(78, 50);
			pnl_ChooseSlot.Name = "pnl_ChooseSlot";
			pnl_ChooseSlot.Size = new System.Drawing.Size(390, 110);
			//pnl_ChooseSlot.BackColor = Color.FromArgb(176,196,222); //TODO SKIN
			pnl_ChooseSlot.BackColor = Color.Transparent;
			//pnl_ChooseSlot.BackColor = Color.Violet;
			pnl_ChooseSlot.TabIndex = 13;

			pnl_DropNamedSubTask.Location = new System.Drawing.Point(78, 50);
			pnl_DropNamedSubTask.Name = "pnl_YesNoChoice";
			pnl_DropNamedSubTask.Size = new System.Drawing.Size(390, 120);
			//pnl_DropNamedSubTask.BackColor = Color.FromArgb(176,196,222); //TODO SKIN
			pnl_DropNamedSubTask.BackColor = Color.Transparent;
			//pnl_DropNamedSubTask.BackColor = Color.Turquoise;
			pnl_DropNamedSubTask.TabIndex = 14;
			pnl_DropNamedSubTask.Visible = false;

			BuildSlotButtonControls();
			Build_DescopeNamed_Controls();
			
			this.Controls.Add(pnl_ChooseSlot);
			this.Controls.Add(pnl_DropNamedSubTask);

			this.Name = "DropCritPathControl";
			this.Size = new System.Drawing.Size(520,280);
			this.pnl_ChooseSlot.ResumeLayout(false);
			this.pnl_DropNamedSubTask.ResumeLayout(false);
			this.ResumeLayout(false);
		}

		/// <summary>
		/// Little helper method to set the check boxs 
		/// </summary>
		/// <param name="pr"></param>
		/// <param name="state"></param>
		/// <param name="round"></param>
		/// <param name="name"></param>
		/// <param name="cb"></param>
		private void UpdateCheckBox(ProjectReader pr, emProjectOperationalState state, int round, string letter, 
			string name, CheckBox cb)
		{
			bool sub_task_exists = false;
			bool sub_task_droppable = false;
			bool sub_task_dropped = false;
			bool sub_task_completed = false;

			pr.getSubTaskStateForCritPath(state, name, round, out sub_task_completed, out sub_task_exists, 
				out sub_task_droppable, out sub_task_dropped);

			cb.Text = name;
			cb.Tag = name;

			if (sub_task_exists)
			{
				string block_name = letter + name;
				taskNameToCheckBox.Add(block_name, cb);

				if (sub_task_completed)
				{
					cb.Checked = !sub_task_dropped;
					cb.Enabled = false;
					if (sub_task_droppable == false)
					{
						cb.Text = name + " [M]";
					}
				}
				else
				{
					cb.Checked = !sub_task_dropped;
					cb.Enabled = sub_task_droppable;
					if (sub_task_droppable == false)
					{
						cb.Text = name + " [M]";
					}
					else
					{
						currentChangeableTaskBlocks.Add(block_name, cb.Checked);
					}
				}
			}
			else
			{
				cb.Checked = false;
				cb.Enabled = false;
			}
		}

		private void UpdateDisplayControlsFromProject(ProjectReader pr)
		{ 
			bool allowed_stage_b = true;
			bool allowed_stage_d = true;

			currentChangeableTaskBlocks.Clear();
			taskNameToCheckBox.Clear();

			if (pr.isCurrentStateOrLater(emProjectOperationalState.PROJECT_STATE_B))
			{
				allowed_stage_b = false;
			}
		
			if (pr.isCurrentStateOrLater(emProjectOperationalState.PROJECT_STATE_D))
			{
				allowed_stage_d = false;
			}

			//Buld the Stage B controls 
			if (allowed_stage_b)
			{
				UpdateCheckBox(pr, emProjectOperationalState.PROJECT_STATE_B, round, "B", "i", taskB1_CheckBox);
				UpdateCheckBox(pr, emProjectOperationalState.PROJECT_STATE_B, round, "B", "ii", taskB2_CheckBox);
				UpdateCheckBox(pr, emProjectOperationalState.PROJECT_STATE_B, round, "B", "iii", taskB3_CheckBox);
			}
			else
			{
				UpdateCheckBox(pr, emProjectOperationalState.PROJECT_STATE_B, round, "B", "i", taskB1_CheckBox);
				UpdateCheckBox(pr, emProjectOperationalState.PROJECT_STATE_B, round, "B", "ii", taskB2_CheckBox);
				UpdateCheckBox(pr, emProjectOperationalState.PROJECT_STATE_B, round, "B", "iii", taskB3_CheckBox);
				taskB1_CheckBox.Enabled = false;
				taskB2_CheckBox.Enabled = false;
				taskB3_CheckBox.Enabled = false;
			}

			//Buld the Stage D controls 
			if (allowed_stage_d)
			{
				UpdateCheckBox(pr, emProjectOperationalState.PROJECT_STATE_D, round, "D", "i", taskD1_CheckBox);
				UpdateCheckBox(pr, emProjectOperationalState.PROJECT_STATE_D, round, "D", "ii", taskD2_CheckBox);
				UpdateCheckBox(pr, emProjectOperationalState.PROJECT_STATE_D, round, "D", "iii", taskD3_CheckBox);
			}
			else
			{
				UpdateCheckBox(pr, emProjectOperationalState.PROJECT_STATE_D, round, "D", "i", taskD1_CheckBox);
				UpdateCheckBox(pr, emProjectOperationalState.PROJECT_STATE_D, round, "D", "ii", taskD2_CheckBox);
				UpdateCheckBox(pr, emProjectOperationalState.PROJECT_STATE_D, round, "D", "iii", taskD3_CheckBox);
				taskD1_CheckBox.Enabled = false;
				taskD2_CheckBox.Enabled = false;
				taskD3_CheckBox.Enabled = false;
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

					helpLabel.Text = "Deselect Tasks";

					ProjectReader pr = new ProjectReader(projectNodeToDropTasks);

					UpdateDisplayControlsFromProject(pr);
					
					pr.Dispose();

					this.newBtnOK.Visible = true;
					this.newBtnOK.Enabled = true;

					//Now swap over the the Correct panel for the selection of how much 
					this.pnl_ChooseSlot.Visible = false;
					this.pnl_DropNamedSubTask.Visible = true;
					this.newBtnCancel.Visible = true;
					
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

			if (pr.CanProjectBeDeScoped_ByCritPath(out ErrMsg))
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
			string stage_b_sub_tasks_to_drop = "";
			string stage_b_sub_tasks_to_raise = "";
			string stage_d_sub_tasks_to_drop = "";
			string stage_d_sub_tasks_to_raise = "";

			ProjectReader pr = new ProjectReader(projectNodeToDropTasks);
			string desc = pr.getProjectTextDescription();
			pr.Dispose();


			//Extract the order information 
			foreach (string allowedblocks in this.currentChangeableTaskBlocks.Keys)
			{
				bool current_status = (bool) currentChangeableTaskBlocks[allowedblocks];
				bool request_status = (bool) currentChangeableTaskBlocks[allowedblocks];

				CheckBox cb = null;
				string stage_letter = "";
				switch (allowedblocks)
				{
					case "Bi":
						cb = taskB1_CheckBox;
						stage_letter = "B";
						break;
					case "Bii":
						cb = taskB2_CheckBox;
						stage_letter = "B";
						break;
					case "Biii":
						cb = taskB3_CheckBox;
						stage_letter = "B";
						break;
					case "Di":
						cb = taskD1_CheckBox;
						stage_letter = "D";
						break;
					case "Dii":
						cb = taskD2_CheckBox;
						stage_letter = "D";
						break;
					case "Diii":
						cb = taskD3_CheckBox;
						stage_letter = "D";
						break;
				}

				if (stage_letter == "B")
				{
					request_status = cb.Checked;
					if ((request_status == true) & (current_status == false))
					{
						if (stage_b_sub_tasks_to_raise.Length > 0)
						{
							stage_b_sub_tasks_to_raise = stage_b_sub_tasks_to_raise + ",";
						}
						stage_b_sub_tasks_to_raise = stage_b_sub_tasks_to_raise + cb.Tag.ToString().ToLower();
					}
					if ((request_status == false) & (current_status == true))
					{
						if (stage_b_sub_tasks_to_drop.Length > 0)
						{
							stage_b_sub_tasks_to_drop = stage_b_sub_tasks_to_drop + ",";
						}
						stage_b_sub_tasks_to_drop = stage_b_sub_tasks_to_drop + cb.Tag.ToString().ToLower();
					}
				}
				if (stage_letter == "D")
				{
					request_status = cb.Checked;
					if ((request_status == true) & (current_status == false))
					{
						if (stage_d_sub_tasks_to_raise.Length > 0)
						{
							stage_d_sub_tasks_to_raise = stage_d_sub_tasks_to_raise + ",";
						}
						stage_d_sub_tasks_to_raise = stage_d_sub_tasks_to_raise + cb.Tag.ToString().ToLower();
					}
					if ((request_status == false) & (current_status == true))
					{
						if (stage_d_sub_tasks_to_drop.Length > 0)
						{
							stage_d_sub_tasks_to_drop = stage_d_sub_tasks_to_drop + ",";
						}
						stage_d_sub_tasks_to_drop = stage_d_sub_tasks_to_drop + cb.Tag.ToString().ToLower();
					}
				}
			}

			// Raise or drop tasks as appropriate.
			{
				ArrayList attrs = new ArrayList();
				attrs.Add(new AttributeValuePair("cmd_type", "dropcritpath_project"));
				attrs.Add(new AttributeValuePair("projectnodename", ChosenProject_NodeName));
				attrs.Add(new AttributeValuePair("nodedesc", desc));
				attrs.Add(new AttributeValuePair("stage_b_sub_drop_requests", stage_b_sub_tasks_to_drop));
				attrs.Add(new AttributeValuePair("stage_b_sub_raise_requests", stage_b_sub_tasks_to_raise));
				attrs.Add(new AttributeValuePair("stage_d_sub_drop_requests", stage_d_sub_tasks_to_drop));
				attrs.Add(new AttributeValuePair("stage_d_sub_raise_requests", stage_d_sub_tasks_to_raise));
				new Node(queueNode, "task", "", attrs);

			}

			// Reassign staff.
			{
				Dictionary<string, int> stageNameToTasksActive = new Dictionary<string, int>();
				foreach (string taskName in taskNameToCheckBox.Keys)
				{
					string stageName = taskName[0].ToString();

					if (! stageNameToTasksActive.ContainsKey(stageName))
					{
						stageNameToTasksActive.Add(stageName, 0);
					}

					if (taskNameToCheckBox[taskName].Checked)
					{
						stageNameToTasksActive[stageName]++;
					}
				}

				ProjectReader projectReader = new ProjectReader (projectNodeToDropTasks);

				ArrayList attrs = new ArrayList ();
				attrs.Add(new AttributeValuePair ("cmd_type", "restaff_project"));
				attrs.Add(new AttributeValuePair ("project_node_name", ChosenProject_NodeName));
				attrs.Add(new AttributeValuePair ("nodedesc", desc));
				attrs.Add(new AttributeValuePair ("budget_value", projectReader.getPlayerDefinedBudget()));
				attrs.Add(new AttributeValuePair ("delayed_start_days", projectReader.getDelayedStartDay()));

				foreach (string stageName in GameConstants.GetAllStageNames())
				{
					work_stage stage = projectReader.getWorkStageCloneForStage(GameConstants.ProjectStateFromStageName(stageName));

					int internalStaff, externalStaff;
					bool inProgress, complete;
					stage.getRequestedResourceLevels(out internalStaff, out externalStaff, out inProgress, out complete);

					if (stageNameToTasksActive.ContainsKey(stageName))
					{
						internalStaff = stageNameToTasksActive[stageName];
						externalStaff = 0;
					}

					attrs.Add(new AttributeValuePair ("stage_" + stageName.ToLower() + "_internal", internalStaff));
					attrs.Add(new AttributeValuePair ("stage_" + stageName.ToLower() + "_external", externalStaff));
				}

				new Node (queueNode, "task", "", attrs);
			}

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

		private void newBtnProject_Click(object sender, System.EventArgs e)
		{
			//pnl_DropPercent.Visible = false;
			this.pnl_DropNamedSubTask.Visible = false;
			pnl_ChooseSlot.Visible = true;
			projectLabel.Visible = false;
			newBtnProject.Visible = false;
			
			this.newBtnOK.Visible = true;
			this.newBtnOK.Enabled = false;
		}

		private void taskB1_CheckBox_CheckedChanged(object sender, System.EventArgs e)
		{
		}

		private void taskB2_CheckBox_CheckedChanged(object sender, System.EventArgs e)
		{
		}

		private void taskB3_CheckBox_CheckedChanged(object sender, System.EventArgs e)
		{
		}

		private void taskD1_CheckBox_CheckedChanged(object sender, System.EventArgs e)
		{
		}

		private void taskD2_CheckBox_CheckedChanged(object sender, System.EventArgs e)
		{
		}

		private void taskD3_CheckBox_CheckedChanged(object sender, System.EventArgs e)
		{
		}


	}
}