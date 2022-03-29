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
	/// <summary>
	/// A Data entry Panel for changing the staff level in the stages (and Budget)
	/// Could do with a refactor (mostly a straight rip out of old PM code) 
	///   Build an array of controls for the different stages 
	/// </summary>
	public class DataEntryPanel_ResourceEditProject : FlickerFreePanel
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
		private System.Windows.Forms.Label titleLabel;
		private System.Windows.Forms.Label helpLabel;
		private System.Windows.Forms.Label projectLabel;

		private ProjectResourceEditorPanel MyPrjResStageEditor;

		private int SlotDisplay =0;
		private Node ChosenProject_Node = null;
		private string ChosenProject_NodeName;
		private ProjectReader pr = null;

		private int pmo_budgettotal = 0;
		private int pmo_budgetspent = 0;
		private int pmo_budgetleft = 0;
		private int project_budget_total = 0;
		private int project_budget_left = 0; 
		private int project_budget_spent = 0;

		private int globalStaffLimit_DevInt =0;
		private int globalStaffLimit_DevExt =0;
		private int globalStaffLimit_TestInt =0;
		private int globalStaffLimit_TestExt =0;

		private bool use_single_staff_section = false;

		protected int round;
		protected string projectTerm;
		protected bool display_seven_projects = false;
		
		public DataEntryPanel_ResourceEditProject (IDataEntryControlHolder mainPanel, NodeTree tree, NetworkProgressionGameFile gameFile)
		{
			MyNodeTree = tree;
			_mainPanel = mainPanel;
			round = gameFile.CurrentRound;

			Node projectsNode = tree.GetNamedNode("pm_projects_running");
			display_seven_projects = projectsNode.GetBooleanAttribute("display_seven_projects", false);

			use_single_staff_section = false;
			string UseSingleStaffSection_str = SkinningDefs.TheInstance.GetData("use_single_staff_section", "false");
			if (UseSingleStaffSection_str.IndexOf("true") > -1)
			{
				use_single_staff_section = true;
			}

			projectTerm = SkinningDefs.TheInstance.GetData("project_term_round_" + LibCore.CONVERT.ToStr(round), "Project");

			string dec =  SkinningDefs.TheInstance.GetData("usedataentrychecks");

			queueNode = tree.GetNamedNode("TaskManager");

			//Extract the Department Staff Limits 
			Node tmpNode = null;
			tmpNode = MyNodeTree.GetNamedNode("dev_staff");
			if (tmpNode != null)
			{
				globalStaffLimit_DevInt = tmpNode.GetIntAttribute("total",0);
			}
			tmpNode = MyNodeTree.GetNamedNode("dev_contractor");
			if (tmpNode != null)
			{
				globalStaffLimit_DevExt = tmpNode.GetIntAttribute("total",0);
			}
			tmpNode = MyNodeTree.GetNamedNode("test_staff");
			if (tmpNode != null)
			{
				globalStaffLimit_TestInt = tmpNode.GetIntAttribute("total",0);
			}
			tmpNode = MyNodeTree.GetNamedNode("test_contractor");
			if (tmpNode != null)
			{
				globalStaffLimit_TestExt = tmpNode.GetIntAttribute("total",0);
			}

			if (use_single_staff_section)
			{
				globalStaffLimit_TestInt = globalStaffLimit_DevInt;
				globalStaffLimit_TestExt = globalStaffLimit_DevExt;
			}

			getPMOBudgetData();

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
			titleLabel.Text = "Resources";
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
			helpLabel.Text = "Select " + SkinningDefs.TheInstance.GetData("project_term_round_" + LibCore.CONVERT.ToStr(round), "Project") + " Number";
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
			if (pr != null)
			{
				pr.Dispose();
			}
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

		private void getPMOBudgetData()
		{
			Node pb = this.MyNodeTree.GetNamedNode("pmo_budget");
			if (pb != null)
			{
				pmo_budgettotal = pb.GetIntAttribute("budget_allowed",0);
				pmo_budgetspent = pb.GetIntAttribute("budget_spent",0); 
				pmo_budgetleft =  pb.GetIntAttribute("budget_left",0);
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
				int project_slot = n.GetIntAttribute("slot",0);
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
				if (existingProjectsBySlot.ContainsKey(step))
				{
					project_node = (Node) existingProjectsBySlot[step];
					display_text = project_node.GetAttribute("project_id");
					project_active = true;
				}

				//Build the button 
				btnProjectsSlots[step] = new ImageTextButton(0);
				btnProjectsSlots[step].SetButton(AppInfo.TheInstance.Location+"\\images\\buttons\\button_50x40.png");
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

		public void Build_Label(System.Windows.Forms.Label lbl, string txt, int x, int y, int w, int h, int tabindex) 
		{
			lbl.Font = this.MyDefaultSkinFontNormal8;
			lbl.Location = new System.Drawing.Point(x,y);
			lbl.Size = new System.Drawing.Size(w, h);
			lbl.TabIndex = tabindex;
			lbl.Text = txt;
			lbl.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		}

		public void BuildBaseControls() 
		{
			pnl_ChooseSlot = new System.Windows.Forms.Panel();
			MyPrjResStageEditor = new ProjectResourceEditorPanel(this.MyNodeTree,round);
			
			MyPrjResStageEditor.SuspendLayout();
			pnl_ChooseSlot.SuspendLayout();
			this.SuspendLayout();

			pnl_ChooseSlot.Location = new System.Drawing.Point(90+10-23, 50);
			pnl_ChooseSlot.Name = "pnl_ChooseSlot";
			pnl_ChooseSlot.Size = new System.Drawing.Size(230+90+80, 110);
			pnl_ChooseSlot.BackColor = Color.Transparent;
			//pnl_ChooseSlot.BackColor = Color.Crimson;
			pnl_ChooseSlot.TabIndex = 13;

			MyPrjResStageEditor.SetAllowResourceReductionInCurrentStage();
			MyPrjResStageEditor.Name = "MyPrjResStageEditor";
			MyPrjResStageEditor.Location = new System.Drawing.Point(79, 30+75-40);
			MyPrjResStageEditor.Size = new System.Drawing.Size(401, 150);
			//pnl_ResourceEdit.BackColor = Color.FromArgb(176,196,222); //TODO SKIN
			//MyPrjResStageEditor.BackColor = Color.FromArgb(235,235,235); //TODO SKIN
			MyPrjResStageEditor.BackColor = Color.Transparent;
			//MyPrjResStageEditor.BackColor = Color.Pink;
			//pnl_ResourceEdit.BackColor = Color.Pink;
			MyPrjResStageEditor.TabIndex = 14;
			MyPrjResStageEditor.Visible = false;

			BuildSlotButtonControls();
			
			this.Controls.Add(pnl_ChooseSlot);
			this.Controls.Add(MyPrjResStageEditor);
			this.Name = "ForwardScheduleControl";
			this.Size = new System.Drawing.Size(520,280);
			
			this.MyPrjResStageEditor.ResumeLayout(false);
			this.pnl_ChooseSlot.ResumeLayout(false);
			this.ResumeLayout(false);
		}

		private void Fill_NewEditorData(int SlotDisplay)
		{
			if (existingProjectsBySlot.ContainsKey(SlotDisplay))
			{
				Node prjNode = (Node) existingProjectsBySlot[SlotDisplay];
				if (prjNode != null)
				{
					MyPrjResStageEditor.LoadDataIntoControls(prjNode);
				}
			}
		}

		private void getProjectBudget(Node prjNode)
		{
			if (prjNode != null)
			{
				ProjectReader pr = new ProjectReader(prjNode);
				if (pr != null)
				{
					project_budget_total = pr.getPlayerDefinedBudget();
					project_budget_left = pr.getBudgetleft();
					project_budget_spent = pr.getSpend();
				}
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
					ChosenProject_Node = (Node)(ib.Tag);

					getProjectBudget(ChosenProject_Node);

					SlotDisplay = ChosenProject_Node.GetIntAttribute("slot",0);
					ChosenProject_NodeName = ChosenProject_Node.GetAttribute("name"); 
					string display_text = ChosenProject_Node.GetAttribute("project_id");
					
					helpLabel.Text = "Enter Staff Levels";

					Fill_NewEditorData(SlotDisplay);

					this.MyPrjResStageEditor.handleDaysDisplays();
					
					this.MyPrjResStageEditor.Visible = true;
					this.MyPrjResStageEditor.BringToFront();
					MyPrjResStageEditor.SetFocus();
					this.pnl_ChooseSlot.Visible = false;

					projectLabel.Visible = true;
					newBtnProject.SetButtonText(display_text,
						System.Drawing.Color.Black,System.Drawing.Color.Black,
						System.Drawing.Color.Green,System.Drawing.Color.Gray);
					newBtnProject.Visible = true;

					this.newBtnOK.Visible = true;
					this.newBtnOK.Enabled = true;
				}
			}
		}

		private bool HandleOrder()
		{
			bool good_order = true;
			int newbudget =1;
			int delayed_start_days = 0;
			int staff_a_internal = 0;
			int staff_a_external = 0; 
			int staff_b_internal = 0; 
			int staff_b_external = 0; 
			int staff_c_internal = 0; 
			int staff_c_external = 0; 
			int staff_d_internal = 0; 
			int staff_d_external = 0; 
			int staff_e_internal = 0; 
			int staff_e_external = 0; 
			int staff_f_internal = 0; 
			int staff_f_external = 0; 
			int staff_g_internal = 0; 
			int staff_g_external = 0; 
			int staff_h_internal = 0; 
			int staff_h_external = 0;

			ProjectReader pr = new ProjectReader(ChosenProject_Node);
			string desc = pr.getProjectTextDescription();
			pr.Dispose();

			//Extract the Data from the Control 
			this.MyPrjResStageEditor.ExtractDataFromControls(out newbudget, out delayed_start_days,
				out staff_a_internal, out staff_a_external, out staff_b_internal, out staff_b_external,
				out staff_c_internal, out staff_c_external, out staff_d_internal, out staff_d_external,
				out staff_e_internal, out staff_e_external, out staff_f_internal, out staff_f_external,
				out staff_g_internal, out staff_g_external, out staff_h_internal, out staff_h_external);

			ArrayList attrs = new ArrayList ();
			attrs.Add(new AttributeValuePair ("cmd_type", "restaff_project"));
			attrs.Add(new AttributeValuePair ("project_node_name", this.ChosenProject_NodeName));
			attrs.Add(new AttributeValuePair ("nodedesc", desc));
			attrs.Add(new AttributeValuePair ("budget_value", CONVERT.ToStr(newbudget)));
			attrs.Add(new AttributeValuePair ("delayed_start_days", CONVERT.ToStr(delayed_start_days)));
			attrs.Add(new AttributeValuePair ("stage_a_internal", CONVERT.ToStr(staff_a_internal)));
			attrs.Add(new AttributeValuePair ("stage_a_external", CONVERT.ToStr(staff_a_external)));
			attrs.Add(new AttributeValuePair ("stage_b_internal", CONVERT.ToStr(staff_b_internal)));
			attrs.Add(new AttributeValuePair ("stage_b_external", CONVERT.ToStr(staff_b_external)));
			attrs.Add(new AttributeValuePair ("stage_c_internal", CONVERT.ToStr(staff_c_internal)));
			attrs.Add(new AttributeValuePair ("stage_c_external", CONVERT.ToStr(staff_c_external)));
			attrs.Add(new AttributeValuePair ("stage_d_internal", CONVERT.ToStr(staff_d_internal)));
			attrs.Add(new AttributeValuePair ("stage_d_external", CONVERT.ToStr(staff_d_external)));
			attrs.Add(new AttributeValuePair ("stage_e_internal", CONVERT.ToStr(staff_e_internal)));
			attrs.Add(new AttributeValuePair ("stage_e_external", CONVERT.ToStr(staff_e_external)));
			attrs.Add(new AttributeValuePair ("stage_f_internal", CONVERT.ToStr(staff_f_internal)));
			attrs.Add(new AttributeValuePair ("stage_f_external", CONVERT.ToStr(staff_f_external)));
			
			
			attrs.Add(new AttributeValuePair ("stage_g_internal", CONVERT.ToStr(staff_g_internal)));
			attrs.Add(new AttributeValuePair ("stage_g_external", CONVERT.ToStr(staff_g_external)));
			attrs.Add(new AttributeValuePair ("stage_h_internal", CONVERT.ToStr(staff_h_internal)));
			attrs.Add(new AttributeValuePair ("stage_h_external", CONVERT.ToStr(staff_h_external)));
			//attrs.Add(new AttributeValuePair ("day", day_number));
			new Node (queueNode, "task", "", attrs);	
			return good_order;
		}

		private void newBtnOK_Click(object sender, System.EventArgs e)
		{
			bool good_order_placed = false;
			ArrayList errs = new ArrayList();

			bool CheckForBudgetAlteration = true;  
			bool CheckForBudgetCreation = false;  

			good_order_placed = this.MyPrjResStageEditor.CheckOrder(
				CheckForBudgetAlteration, CheckForBudgetCreation,
				pmo_budgettotal, pmo_budgetspent, pmo_budgetleft,
				project_budget_total, project_budget_left, project_budget_spent,
				globalStaffLimit_DevInt, globalStaffLimit_DevExt, 
				globalStaffLimit_TestInt, globalStaffLimit_TestExt, 
				out errs);
				
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
			this.pnl_ChooseSlot.Visible = true;
			projectLabel.Visible = false;
			newBtnProject.Visible = false;
			//newBtnOK.Visible = false;
			this.MyPrjResStageEditor.Visible = false;
			this.newBtnOK.Visible = true;
			this.newBtnOK.Enabled = false;
			helpLabel.Text = "Select " + SkinningDefs.TheInstance.GetData("project_term_round_" + LibCore.CONVERT.ToStr(round), "Project") + " Number";
		}
	}
}