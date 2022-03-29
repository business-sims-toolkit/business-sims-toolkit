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

using GameManagement;

using Polestar_PM.DataLookup;

namespace Polestar_PM.OpsGUI
{
	public enum emProjectStaffSelection 
	{
		/// <summary>
		/// The players need to define the staff requirements by each individual stage (A-H)
		/// </summary>
		USESTAGEDEFINITION,

		/// <summary>
		/// The players need to define the staff requirements by each Phase (Design, Build, Test)
		/// </summary>
		USEPHASEDEFINITION,

		/// <summary>
		/// The players will not get the chance to change the staff as we will use the predefined values 
		/// </summary>
		USEPREDEFINITION
	}

	public class DataEntryPanel_SetupProject : FlickerFreePanel
	{
		protected emProjectStaffSelection StaffSelectionMode = emProjectStaffSelection.USEPHASEDEFINITION;

		protected NodeTree MyNodeTree;
		protected IDataEntryControlHolder _mainPanel;

		protected Node queueNode;
		protected Node projectsNode;
		protected Node expertsNode;
		protected Hashtable existingProjectsBySlot = new Hashtable();
		protected Hashtable existingProjectsByID = new Hashtable();

		protected GlobalTeamEditorPanel MyGlobalTeamEditor = null; 

		protected Font MyDefaultSkinFontNormal8 = null;
		protected Font MyDefaultSkinFontNormal10 = null;
		protected Font MyDefaultSkinFontNormal12 = null;
		protected Font MyDefaultSkinFontBold8 = null;
		protected Font MyDefaultSkinFontBold10 = null;
		protected Font MyDefaultSkinFontBold12 = null;

		private ImageTextButton btnNext = new ImageTextButton(0);
		private ImageTextButton btnOK = new ImageTextButton(0);
		private ImageTextButton btnCancel = new ImageTextButton(0);
		private ImageTextButton btnYes = new ImageTextButton(0);
		private ImageTextButton btnNo = new ImageTextButton(0);
		//private ImageTextButton btnProject = new ImageTextButton(0);

		private ImageTextButton newBtnFixTeamSizes = new ImageTextButton(0);

		private System.Windows.Forms.Panel pnl_ChooseSlot;
		private System.Windows.Forms.Panel pnl_ChooseProject;
		private System.Windows.Forms.Panel pnl_ChooseProduct;
		private System.Windows.Forms.Panel pnl_ChoosePlatform;
		private System.Windows.Forms.Panel pnl_ChooseResource;
		private System.Windows.Forms.Panel pnl_ChooseExperts;
		private System.Windows.Forms.Panel pnl_ConfirmPredefine;

		private ProjectResourceEditorPanel MyPrjResStageEditor;

		private System.Windows.Forms.Panel pnl_FixTeamSizes;
		private System.Windows.Forms.Label titleLabel;
		private System.Windows.Forms.Label helpLabel;

		private System.Windows.Forms.Label slotLabel;
		private System.Windows.Forms.Label projectLabel;
		private System.Windows.Forms.Label productLabel;
		private System.Windows.Forms.Label platformLabel;
		private System.Windows.Forms.Label expertsLabel;

		private ImageTextButton[] btnSlotSelection = new ImageTextButton[7];
		private ImageTextButton[] btnProjectSelection = new ImageTextButton[12];
		private ImageTextButton[] btnProductSelection = new ImageTextButton[4];
		private ImageTextButton[] btnPlatformSelection = new ImageTextButton[3];

		private ImageTextButton btnSlot = new ImageTextButton(0);
		private ImageTextButton btnProject = new ImageTextButton(0);
		private ImageTextButton btnProduct = new ImageTextButton(0);
		private ImageTextButton btnPlatform = new ImageTextButton(0);
		private ImageTextButton btnExperts = new ImageTextButton(0);

		private System.Windows.Forms.Label lblDesignTeamSize;
		private System.Windows.Forms.Label lblBuildTeamSize;
		private System.Windows.Forms.Label lblTestTeamSize;
		private System.Windows.Forms.Label lblBudget;
		
		private System.Windows.Forms.TextBox tbDesignTeamSize;
		private System.Windows.Forms.TextBox tbBuildTeamSize;
		private System.Windows.Forms.TextBox tbTestTeamSize;
		private System.Windows.Forms.TextBox tbBudget;

		private System.Windows.Forms.ComboBox cbExperts_DA = null;
		private System.Windows.Forms.ComboBox cbExperts_BM = null;
		private System.Windows.Forms.ComboBox cbExperts_TM = null;
		private System.Windows.Forms.Label lbl_Experts_DA_Title;
		private System.Windows.Forms.Label lbl_Experts_BM_Title;
		private System.Windows.Forms.Label lbl_Experts_TM_Title;
		//private ImageTextButton btnUseExperts = new ImageTextButton(0);
		
		private Image normal_back;
		private Image wide_back;

		private int ChosenSlotNumber;
		private int ChosenProjectNumber;
		private int ChosenProductNumber;
		private int ChosenPlatformNumber;
		
		private int globalStaffLimit_DevInt =0;
		private int globalStaffLimit_DevExt =0;
		private int globalStaffLimit_TestInt =0;
		private int globalStaffLimit_TestExt =0;
		private int pmo_budgettotal = 0;
		private int pmo_budgetspent = 0;
		private int pmo_budgetleft =  0;

		private bool UseDataEntryChecks = true;	
		private bool doGobalLimitsIncludeConsultants = false;
		private int CurrentRound = 1;
		private bool use_single_staff_section = false;
		private bool showHiddenProjectChoices = false;
		private bool allow_seventh_project = false;
		private bool use_experts_round3 = false;
		private bool experts_system_enabled = false;

		string projectTerm;

		NetworkProgressionGameFile gameFile;
		
		public DataEntryPanel_SetupProject (IDataEntryControlHolder mainPanel, NetworkProgressionGameFile gameFile, NodeTree tree, int round)
		{
			MyNodeTree = tree;
			projectsNode = tree.GetNamedNode("pm_projects_running");
			expertsNode = tree.GetNamedNode("experts");
			this.gameFile = gameFile;

			queueNode = tree.GetNamedNode("TaskManager");
			_mainPanel = mainPanel;
			CurrentRound = round;

			showHiddenProjectChoices = projectsNode.GetBooleanAttribute("show_hidden", false);
			allow_seventh_project = projectsNode.GetBooleanAttribute("allow_seventh_project", false);

			//Handle the experts system 
			if (round == 3)
			{
				use_experts_round3 = SkinningDefs.TheInstance.GetBoolData("use_experts_round3", false);
				if (expertsNode != null)
				{
					experts_system_enabled = expertsNode.GetBooleanAttribute("enabled", false);
				}
			}
			else
			{
				use_experts_round3 = false;
				experts_system_enabled = false;
			}

			switch (CurrentRound)
			{ 
				case 1:
					StaffSelectionMode = emProjectStaffSelection.USEPHASEDEFINITION;
					allow_seventh_project = false; //Never allow 7 projects in R1
					break;
				case 2:
				case 3:
					StaffSelectionMode = emProjectStaffSelection.USESTAGEDEFINITION;
					allow_seventh_project = false; //Never allow 7 projects in R2
					break;
				//case 3:
				//  StaffSelectionMode = emProjectStaffSelection.USEPREDEFINITION;
				//  break;
				default:
					break;
			}

			use_single_staff_section = false;
			string UseSingleStaffSection_str = SkinningDefs.TheInstance.GetData("use_single_staff_section", "false");
			if (UseSingleStaffSection_str.IndexOf("true") > -1)
			{
				use_single_staff_section = true;
			}

			projectTerm = SkinningDefs.TheInstance.GetData("project_term_round_" + LibCore.CONVERT.ToStr(CurrentRound), "Project");

			UseDataEntryChecks = true;
			string dec =  SkinningDefs.TheInstance.GetData("usedataentrychecks");
			if (dec.ToLower().IndexOf("false")>-1)
			{
				UseDataEntryChecks = false;
			}

			//Extract the Staff Limits 
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

			//In the single section mode, we just have one group of staff (dev people)
			//so the limits for the test ui inputs as the same as the dev ones
			if (use_single_staff_section)
			{
				globalStaffLimit_TestInt = globalStaffLimit_DevInt;
				globalStaffLimit_TestExt = globalStaffLimit_DevExt;
			}

			getPMOBudgetData();

			BuildCurrentProjectLookupList();

			MyGlobalTeamEditor = new GlobalTeamEditorPanel(tree, round);

			string fontname =  SkinningDefs.TheInstance.GetData("fontname");
			MyDefaultSkinFontNormal8 = ConstantSizeFont.NewFont(fontname,8);
			MyDefaultSkinFontNormal10 = ConstantSizeFont.NewFont(fontname,10);
			MyDefaultSkinFontNormal12 = ConstantSizeFont.NewFont(fontname,12);
			MyDefaultSkinFontBold8 = ConstantSizeFont.NewFont(fontname,8,FontStyle.Bold);
			MyDefaultSkinFontBold10 = ConstantSizeFont.NewFont(fontname,10,FontStyle.Bold);
			MyDefaultSkinFontBold12 = ConstantSizeFont.NewFont(fontname,12,FontStyle.Bold);

			normal_back = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "images\\panels\\PM_opsback.png");
			wide_back = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "images\\panels\\PM_opsback2.png");
			this.BackgroundImage = normal_back;

			//this.ShowInTaskbar = false;
			//this.ClientSize = new Size (520,280);
			//this.FormBorderStyle = FormBorderStyle.None;
			//this.Opacity = 1.00;
			this.Size = new Size (520,255);

			BuildPanelGeneral();
			Build_All_Panels();
			
			pnl_ChooseSlot.Visible = true;
		}

		new public void Dispose()
		{
			MyNodeTree = null;
			queueNode = null;
			projectsNode = null;

			if (MyGlobalTeamEditor != null)
			{
				MyGlobalTeamEditor.Dispose();
				MyGlobalTeamEditor = null;
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
			base.Dispose();
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
				string project_id = n.GetAttribute("project_id");
				string project_slot = n.GetAttribute("slot");
				string project_status = n.GetAttribute("status");

				existingProjectsBySlot.Add(project_slot, n);
				existingProjectsByID.Add(project_id, n);
			}
		}

		private void BuildCurrentExpertsLookupList()
		{
			//Local structure to sort the names into alphabetical order 
			ArrayList Experts_names_DA = new ArrayList();
			ArrayList Experts_names_BM = new ArrayList();
			ArrayList Experts_names_TM = new ArrayList();

			if (expertsNode != null)
			{
				foreach (Node expert_node in this.expertsNode.getChildren())
				{
					string expert_name = expert_node.GetAttribute("expert_name");
					string skill_type = expert_node.GetAttribute("skill_type");
					string assigned_project = expert_node.GetAttribute("assigned_project");

					//only allow not assigned project to be selected 
					if (assigned_project == "")
					{
						//Assign the DA to the correct combobox 
						if (skill_type.ToLower() == "design architect")
						{
							Experts_names_DA.Add(expert_name);
						}
						//Assign the BM to the correct combobox 
						if (skill_type.ToLower() == "build manager")
						{
							Experts_names_BM.Add(expert_name);
						}
						//Assign the TM to the correct combobox 
						if (skill_type.ToLower() == "test manager")
						{
							Experts_names_TM.Add(expert_name);
						}
					}
				}
				if (Experts_names_DA.Count > 0)
				{
					if (this.cbExperts_DA != null)
					{
						Experts_names_DA.Sort();
						foreach (string name in Experts_names_DA)
						{
							this.cbExperts_DA.Items.Add(name);
						}
					}
				}
				if (Experts_names_BM.Count > 0)
				{
					if (this.cbExperts_BM != null)
					{
						Experts_names_BM.Sort();
						foreach (string name in Experts_names_BM)
						{
							this.cbExperts_BM.Items.Add(name);
						}
					}
				}
				if (Experts_names_TM.Count > 0)
				{
					if (this.cbExperts_TM != null)
					{
						Experts_names_TM.Sort();
						foreach (string name in Experts_names_TM)
						{
							this.cbExperts_TM.Items.Add(name);
						}
					}
				}
			}
		}

		#region General Countrols Methods 

		public void Build_All_Panels() 
		{
			pnl_ChooseSlot = new System.Windows.Forms.Panel();
			pnl_ChooseProject = new System.Windows.Forms.Panel();
			pnl_ChooseProduct = new System.Windows.Forms.Panel();
			pnl_ChoosePlatform = new System.Windows.Forms.Panel();
			pnl_ChooseResource = new System.Windows.Forms.Panel();
			pnl_ConfirmPredefine = new System.Windows.Forms.Panel();
			pnl_FixTeamSizes = new System.Windows.Forms.Panel();
			pnl_ChooseExperts = new System.Windows.Forms.Panel();

			MyPrjResStageEditor = new ProjectResourceEditorPanel(this.MyNodeTree,CurrentRound);
			
			pnl_ChooseSlot.SuspendLayout();
			pnl_ChooseProject.SuspendLayout();
			pnl_ChooseProduct.SuspendLayout();
			pnl_ChoosePlatform.SuspendLayout();
			pnl_ChooseResource.SuspendLayout();
			pnl_ConfirmPredefine.SuspendLayout();
			pnl_FixTeamSizes.SuspendLayout();
			pnl_ChooseExperts.SuspendLayout();

			MyPrjResStageEditor.SuspendLayout();
			this.SuspendLayout();

			pnl_ChooseSlot.Location = new System.Drawing.Point(78, 50);
			pnl_ChooseSlot.Name = "pnl_ChooseSlot";
			pnl_ChooseSlot.Size = new System.Drawing.Size(230+90+80, 110);
			pnl_ChooseSlot.BackColor = Color.Transparent;
			pnl_ChooseSlot.TabIndex = 13;
			pnl_ChooseSlot.Visible = true;

			pnl_ChooseProject.Location = new System.Drawing.Point(78, 50);
			pnl_ChooseProject.Name = "pnl_ChooseProject";
			pnl_ChooseProject.Size = new System.Drawing.Size(230+90+80, 140);
			pnl_ChooseProject.BackColor = Color.Transparent;
			pnl_ChooseProject.TabIndex = 13;
			pnl_ChooseProject.Visible = false;

			pnl_ChooseProduct.Location = new System.Drawing.Point(78, 50);
			pnl_ChooseProduct.Name = "pnl_ChooseProduct";
			pnl_ChooseProduct.Size = new System.Drawing.Size(230+90+80, 140);
			pnl_ChooseProduct.BackColor = Color.Transparent;
			pnl_ChooseProduct.TabIndex = 13;
			pnl_ChooseProduct.Visible = false;

			pnl_ChoosePlatform.Location = new System.Drawing.Point(78, 50);
			pnl_ChoosePlatform.Name = "pnl_ChoosePlatform";
			pnl_ChoosePlatform.Size = new System.Drawing.Size(230+90+80, 140);
			pnl_ChoosePlatform.BackColor = Color.Transparent;
			pnl_ChoosePlatform.TabIndex = 13;
			pnl_ChoosePlatform.Visible = false;

			pnl_ChooseResource.Location = new System.Drawing.Point(78, 50);
			pnl_ChooseResource.Name = "pnl_ChooseResource";
			pnl_ChooseResource.Size = new System.Drawing.Size(230+90+80, 140);
			pnl_ChooseResource.BackColor = Color.Transparent;
			pnl_ChooseResource.TabIndex = 13;
			pnl_ChooseResource.Visible = false;

			pnl_ConfirmPredefine.Location = new System.Drawing.Point(78, 50);
			pnl_ConfirmPredefine.Name = "pnl_ConfirmPredefine";
			pnl_ConfirmPredefine.Size = new System.Drawing.Size(230+90+80, 140);
			pnl_ConfirmPredefine.BackColor = Color.Transparent;
			pnl_ConfirmPredefine.TabIndex = 13;
			pnl_ConfirmPredefine.Visible = false;

			pnl_ChooseExperts.Location = new System.Drawing.Point(78, 50);
			pnl_ChooseExperts.Name = "pnl_ConfirmPredefine";
			pnl_ChooseExperts.Size = new System.Drawing.Size(230 + 90 + 80, 140);
			pnl_ChooseExperts.BackColor = Color.Transparent;
			pnl_ChooseExperts.TabIndex = 13;
			pnl_ChooseExperts.Visible = false;

			MyPrjResStageEditor = new ProjectResourceEditorPanel(this.MyNodeTree, CurrentRound);
			MyPrjResStageEditor.Name = "MyPrjResStageEditor";
			MyPrjResStageEditor.Location = new System.Drawing.Point(79, 30+75-40);
			MyPrjResStageEditor.Size = new System.Drawing.Size(401, 150);
			MyPrjResStageEditor.BackColor = Color.Transparent;
			MyPrjResStageEditor.TabIndex = 14;
			MyPrjResStageEditor.Visible = false;

			pnl_FixTeamSizes.Location = new System.Drawing.Point(10, 60);
			pnl_FixTeamSizes.Name = "pnl_FixTeamSizes";
			pnl_FixTeamSizes.Size = new System.Drawing.Size(470, 155);
			pnl_FixTeamSizes.BackColor = Color.Transparent;
			pnl_FixTeamSizes.TabIndex = 14;
			pnl_FixTeamSizes.Visible = false;

			MyGlobalTeamEditor = new GlobalTeamEditorPanel(this.MyNodeTree, this.CurrentRound);
			MyGlobalTeamEditor.Location = new Point(0,0);
			MyGlobalTeamEditor.Size = new System.Drawing.Size(470, 150);
			MyGlobalTeamEditor.BackColor = Color.FromArgb(235,235,235);

			BuildSlotButtonControls();
			Build_ProjectButtonsPanel();
			Build_ProductButtonsPanel();
			Build_PlatformButtonsPanel();
			if ((use_experts_round3)&(experts_system_enabled))
			{
				Build_ExpertsPanel();
			}
			Build_ResourceDefinitionPanel();

			this.Controls.Add(pnl_ChooseSlot);
			this.Controls.Add(pnl_ChooseProject);
			this.Controls.Add(pnl_ChooseProduct);
			this.Controls.Add(pnl_ChoosePlatform);
			this.Controls.Add(pnl_ChooseResource);
			this.Controls.Add(pnl_ConfirmPredefine);
			this.Controls.Add(pnl_FixTeamSizes);
			this.Controls.Add(MyPrjResStageEditor);
			this.Controls.Add(pnl_ChooseExperts);
			this.Name = "ForwardScheduleControl";
			this.Size = new System.Drawing.Size(520,280);

			this.pnl_ChooseSlot.ResumeLayout(false);
			this.pnl_ChooseProject.ResumeLayout(false);
			this.pnl_ChooseProduct.ResumeLayout(false);
			this.pnl_ChoosePlatform.ResumeLayout(false);
			this.pnl_ChooseResource.ResumeLayout(false);
			this.pnl_ConfirmPredefine.ResumeLayout(false);
			this.pnl_FixTeamSizes.ResumeLayout(false);
			this.pnl_ChooseExperts.ResumeLayout(false);

			MyPrjResStageEditor.ResumeLayout(false);

			this.ResumeLayout(false);
		}

		public void BuildPanelGeneral()
		{
			titleLabel = new System.Windows.Forms.Label();
			titleLabel.BackColor = System.Drawing.Color.Transparent;
			titleLabel.Font = MyDefaultSkinFontBold12;
			titleLabel.ForeColor = System.Drawing.Color.White;
			titleLabel.Location = new System.Drawing.Point(110-25, 10-2);
			titleLabel.Name = "titleLabel";
			titleLabel.Size = new System.Drawing.Size(380, 18);
			titleLabel.TabIndex = 11;
			titleLabel.Text = projectTerm + " Setup";
			//titleLabel.BackColor = Color.Violet;
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
			helpLabel.Text = "Select Slot Number ";
			//helpLabel.BackColor = Color.Violet;
			helpLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.Controls.Add(helpLabel);

			slotLabel = new System.Windows.Forms.Label();
			slotLabel.BackColor = System.Drawing.Color.Transparent;
			//slotLabel.BackColor = System.Drawing.Color.Violet;
			slotLabel.Font = MyDefaultSkinFontNormal10;
			slotLabel.ForeColor = System.Drawing.Color.Black;
			slotLabel.Location = new System.Drawing.Point(3+5, 0+4);
			slotLabel.Name = "titleLabel";
			slotLabel.Size = new System.Drawing.Size(70, 20);
			slotLabel.TabIndex = 11;
			slotLabel.Text = "Slot";
			slotLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			slotLabel.Visible = false;
			//slotLabel.BackColor = Color.SpringGreen;
			this.Controls.Add(slotLabel);

			btnSlot.SetButton(AppInfo.TheInstance.Location+"\\images\\buttons\\selection_tab.png");
			btnSlot.Location = new System.Drawing.Point(15, 20+4);
			btnSlot.Name = "newBtnNo";
			btnSlot.Size = new System.Drawing.Size(65, 27);
			btnSlot.TabIndex = 26;
			btnSlot.ButtonFont = this.MyDefaultSkinFontBold12;
			btnSlot.SetButtonText("No",
				System.Drawing.Color.Black,System.Drawing.Color.Black,
				System.Drawing.Color.Green,System.Drawing.Color.Gray);
			btnSlot.Click += new System.EventHandler(btnSlot_Click);
			btnSlot.Text = "";
			btnSlot.Visible = false; 
			this.Controls.Add(btnSlot);

			projectLabel = new System.Windows.Forms.Label();
			projectLabel.BackColor = System.Drawing.Color.Transparent;
			projectLabel.Font = MyDefaultSkinFontNormal10;
			projectLabel.ForeColor = System.Drawing.Color.Black;
			projectLabel.Location = new System.Drawing.Point(3 + 5, 50 + 4);
			projectLabel.Name = "titleLabel";
			projectLabel.Size = new System.Drawing.Size(70, 20);
			projectLabel.TabIndex = 11;
			projectLabel.Text = projectTerm;
			projectLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			projectLabel.Visible = false;
			//projectLabel.BackColor = Color.CadetBlue;
			this.Controls.Add(projectLabel);

			btnProject.SetButton(AppInfo.TheInstance.Location+"\\images\\buttons\\selection_tab.png");
			btnProject.Location = new System.Drawing.Point(15, 70+4);
			btnProject.Name = "newBtnNo";
			btnProject.Size = new System.Drawing.Size(65,27);
			btnProject.TabIndex = 26;
			btnProject.ButtonFont = this.MyDefaultSkinFontBold12;
			btnProject.SetButtonText("No",
				System.Drawing.Color.Black,System.Drawing.Color.Black,
				System.Drawing.Color.Green,System.Drawing.Color.Gray);
			btnProject.Click += new System.EventHandler(btnProject_Click);
			btnProject.Text = "";
			btnProject.Visible = false; 
			this.Controls.Add(btnProject);

			productLabel = new System.Windows.Forms.Label();
			productLabel.BackColor = System.Drawing.Color.Transparent;
			productLabel.Font = MyDefaultSkinFontNormal10;
			productLabel.ForeColor = System.Drawing.Color.Black;
			productLabel.Location = new System.Drawing.Point(3 + 5, 100 + 4);
			productLabel.Name = "titleLabel";
			productLabel.Size = new System.Drawing.Size(70, 20);
			productLabel.TabIndex = 11;
			productLabel.Text = "Product";
			productLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			productLabel.Visible = false;
			//productLabel.BackColor = Color.CadetBlue;
			this.Controls.Add(productLabel);

			btnProduct.SetButton(AppInfo.TheInstance.Location+"\\images\\buttons\\selection_tab.png");
			btnProduct.Location = new System.Drawing.Point(15, 120+4);
			btnProduct.Name = "newBtnNo";
			btnProduct.Size = new System.Drawing.Size(65, 27);
			btnProduct.TabIndex = 26;
			btnProduct.ButtonFont = this.MyDefaultSkinFontBold12;
			btnProduct.SetButtonText("No",
				System.Drawing.Color.Black,System.Drawing.Color.Black,
				System.Drawing.Color.Green,System.Drawing.Color.Gray);
			btnProduct.Click += new System.EventHandler(btnProduct_Click);
			btnProduct.Text = "Product";
			btnProduct.Visible = false; 
			this.Controls.Add(btnProduct);

			platformLabel = new System.Windows.Forms.Label();
			platformLabel.BackColor = System.Drawing.Color.Transparent;
			platformLabel.Font = MyDefaultSkinFontNormal10;
			platformLabel.ForeColor = System.Drawing.Color.Black;
			platformLabel.Location = new System.Drawing.Point(3 + 5, 150 + 4);
			platformLabel.Name = "titleLabel";
			platformLabel.Size = new System.Drawing.Size(70, 20);
			platformLabel.TabIndex = 11;
			platformLabel.Text = "Platform";
			platformLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			platformLabel.Visible = false;
			//platformLabel.BackColor = Color.CadetBlue;
			this.Controls.Add(platformLabel);

			btnPlatform.SetButton(AppInfo.TheInstance.Location+"\\images\\buttons\\selection_tab.png");
			btnPlatform.Location = new System.Drawing.Point(15, 170+4);
			btnPlatform.Name = "newBtnNo";
			btnPlatform.Size = new System.Drawing.Size(65,27);
			btnPlatform.TabIndex = 26;
			btnPlatform.ButtonFont = this.MyDefaultSkinFontBold12;
			btnPlatform.SetButtonText("No",
				System.Drawing.Color.Black,System.Drawing.Color.Black,
				System.Drawing.Color.Green,System.Drawing.Color.Gray);
			btnPlatform.Click += new System.EventHandler(btnPlatform_Click);
			btnPlatform.Text = "";
			btnPlatform.Visible = false; 
			this.Controls.Add(btnPlatform);

			expertsLabel = new System.Windows.Forms.Label();
			expertsLabel.BackColor = System.Drawing.Color.Transparent;
			expertsLabel.Font = MyDefaultSkinFontNormal10;
			expertsLabel.ForeColor = System.Drawing.Color.Black;
			expertsLabel.Location = new System.Drawing.Point(3 + 5, 200 + 2);
			expertsLabel.Name = "titleLabel";
			expertsLabel.Size = new System.Drawing.Size(70, 20);
			expertsLabel.TabIndex = 11;
			expertsLabel.Text = "Experts";
			expertsLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			expertsLabel.Visible = false;
			//platformLabel.BackColor = Color.CadetBlue;
			this.Controls.Add(expertsLabel);

			btnExperts.SetButton(AppInfo.TheInstance.Location+"\\images\\buttons\\selection_tab.png");
			btnExperts.Location = new System.Drawing.Point(15, 220+0);
			btnExperts.Name = "newBtnNo";
			btnExperts.Size = new System.Drawing.Size(65,27);
			btnExperts.TabIndex = 26;
			btnExperts.ButtonFont = this.MyDefaultSkinFontBold12;
			btnExperts.SetButtonText("Set",
				System.Drawing.Color.Black,System.Drawing.Color.Black,
				System.Drawing.Color.Green,System.Drawing.Color.Gray);
			btnExperts.Click += new System.EventHandler(btnExperts_Click);
			btnExperts.Text = "";
			btnExperts.Visible = false; 
			this.Controls.Add(btnExperts);

			btnOK.SetVariants("images\\buttons\\button_70x25.png");
			btnOK.Location = new System.Drawing.Point(300, 220);
			btnOK.Name = "newBtnOK";
			btnOK.Size = new System.Drawing.Size(70, 25);
			btnOK.TabIndex = 21;
			btnOK.ButtonFont = MyDefaultSkinFontBold10;
			btnOK.SetButtonText("OK",
				System.Drawing.Color.Black,System.Drawing.Color.Black,
				System.Drawing.Color.Green,System.Drawing.Color.DimGray);
			//newBtn.ButtonPressed += new ProjectMngmntGamePanels.ImageButton.ImageButtonEventArgsHandler(playButton_ButtonPressed);
			btnOK.Click += new System.EventHandler(this.btnOK_Click);
			btnOK.Enabled = false;
			this.Controls.Add(btnOK);

			btnNext.SetVariants("images\\buttons\\button_70x25.png");
			btnNext.Location = new System.Drawing.Point(300, 220);
			btnNext.Name = "newBtnNext";
			btnNext.Size = new System.Drawing.Size(70, 25);
			btnNext.TabIndex = 21;
			btnNext.ButtonFont = MyDefaultSkinFontBold10;
			btnNext.SetButtonText("Next",
				System.Drawing.Color.Black, System.Drawing.Color.Black,
				System.Drawing.Color.Green, System.Drawing.Color.DimGray);
			//newBtn.ButtonPressed += new ProjectMngmntGamePanels.ImageButton.ImageButtonEventArgsHandler(playButton_ButtonPressed);
			btnNext.Click += new System.EventHandler(this.btnNext_Click);
			btnNext.Visible = false;
			btnNext.Enabled = false;
			this.Controls.Add(btnNext);
			btnNext.BringToFront();

			btnCancel.SetVariants("images\\buttons\\button_70x25.png");
			btnCancel.Location = new System.Drawing.Point(400, 220);
			btnCancel.Name = "newBtnCancel";
			btnCancel.Size = new System.Drawing.Size(70, 25);
			btnCancel.TabIndex = 22;
			btnCancel.ButtonFont = MyDefaultSkinFontBold10;
			btnCancel.SetButtonText("Cancel",
				System.Drawing.Color.Black, System.Drawing.Color.Black,
				System.Drawing.Color.Green, System.Drawing.Color.DimGray);
			//newBtn.ButtonPressed += new ProjectMngmntGamePanels.ImageButton.ImageButtonEventArgsHandler(playButton_ButtonPressed);
			btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
			this.Controls.Add(btnCancel);
		}

		private void btnSlot_Click(object sender, System.EventArgs e)
		{
			slotLabel.Visible = false;
			btnSlot.Visible = false;
			projectLabel.Visible = false;
			btnProject.Visible = false; 
			productLabel.Visible = false;
			btnProduct.Visible = false; 
			platformLabel.Visible = false;
			btnPlatform.Visible = false; 

			//Rechoose the Project 
			this.pnl_ChooseSlot.Visible = true;
			this.pnl_ChooseProject.Visible = false;
			this.pnl_ChooseProduct.Visible = false;
			this.pnl_ChoosePlatform.Visible = false;
			this.pnl_ChooseResource.Visible = false;
			this.pnl_ConfirmPredefine.Visible = false;
			this.MyPrjResStageEditor.Visible = false;
			
			helpLabel.Text = "Select Slot Number";
			//btnOK.Visible = false;
			btnOK.Enabled = false;
		}

		private void btnProject_Click(object sender, System.EventArgs e)
		{
			projectLabel.Visible = false;
			btnProject.Visible = false; 
			productLabel.Visible = false;
			btnProduct.Visible = false; 
			platformLabel.Visible = false;
			btnPlatform.Visible = false;
			expertsLabel.Visible = false;
			btnExperts.Visible = false;

			//Rechoose the Project 
			this.pnl_ChooseSlot.Visible = false;
			this.pnl_ChooseProject.Visible = true;
			this.pnl_ChooseProduct.Visible = false;
			this.pnl_ChoosePlatform.Visible = false;
			this.pnl_ChooseResource.Visible = false;
			this.pnl_ConfirmPredefine.Visible = false;
			this.pnl_ChooseExperts.Visible = false;
			this.btnNext.Visible = false;
			this.MyPrjResStageEditor.Visible = false;

			helpLabel.Text = "Select Project Number";
			//btnOK.Visible = false;
			btnOK.Enabled = false;
		}

		private void btnProduct_Click(object sender, System.EventArgs e)
		{
			productLabel.Visible = false;
			btnProduct.Visible = false; 
			platformLabel.Visible = false;
			btnPlatform.Visible = false;
			expertsLabel.Visible = false;
			btnExperts.Visible = false;

			//Rechoose the Product 
			this.pnl_ChooseSlot.Visible = false;
			this.pnl_ChooseProject.Visible = false;
			this.pnl_ChooseProduct.Visible = true;
			this.pnl_ChoosePlatform.Visible = false;
			this.pnl_ChooseResource.Visible = false;
			this.pnl_ConfirmPredefine.Visible = false;
			this.pnl_ChooseExperts.Visible = false;
			this.btnNext.Visible = false;
			this.MyPrjResStageEditor.Visible = false;

			helpLabel.Text = "Select Product Number";
			//btnOK.Visible = false;
			btnOK.Enabled = false;
		}

		private void btnPlatform_Click(object sender, System.EventArgs e)
		{
			platformLabel.Visible = false;
			btnPlatform.Visible = false;
			expertsLabel.Visible = false;
			btnExperts.Visible = false;

			//Rechoose the Platform 
			this.pnl_ChooseSlot.Visible = false;
			this.pnl_ChooseProject.Visible = false;
			this.pnl_ChooseProduct.Visible = false;
			this.pnl_ChoosePlatform.Visible = true;
			this.pnl_ChooseResource.Visible = false;
			this.pnl_ConfirmPredefine.Visible = false;
			this.pnl_ChooseExperts.Visible = false;
			this.btnNext.Visible = false;
			this.MyPrjResStageEditor.Visible = false;

			helpLabel.Text = "Select Platform Number";

			//btnOK.Visible = false;
			btnOK.Enabled = false;
		}

		private void btnExperts_Click(object sender, System.EventArgs e)
		{
			expertsLabel.Visible = false;
			btnExperts.Visible = false; 

			//Rechoose the Platform 
			this.pnl_ChooseSlot.Visible = false;
			this.pnl_ChooseProject.Visible = false;
			this.pnl_ChooseProduct.Visible = false;
			this.pnl_ChoosePlatform.Visible = false;
			this.pnl_ChooseExperts.Visible = true;
			this.btnNext.Visible = true;
			this.pnl_ChooseResource.Visible = false;
			this.pnl_ConfirmPredefine.Visible = false;
			this.MyPrjResStageEditor.Visible = false;

			helpLabel.Text = "Select Experts";
			//btnOK.Visible = false;
			btnOK.Enabled = false;
		}

		#endregion General Countrols Methods 

		#region Project Slot Methods 

		public void BuildSlotButtonControls() 
		{
			int offset_x=10;
			int button_width=50;
			int button_sep=15;

			int active_projects = 0;

			int NumberOfSlots =6;
			if (allow_seventh_project)
			{ 
				NumberOfSlots = 7;
				button_sep = 10;
				button_width = 45;
			}

			for (int step = 0; step < NumberOfSlots; step++)
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
					active_projects++;
				}

				//Build the button 
				btnSlotSelection[step] = new ImageTextButton(0);
				//btnSlotSelection[step].SetButton(AppInfo.TheInstance.Location+"\\images\\buttons\\button_50x40.png");
				btnSlotSelection[step].SetVariants("images\\buttons\\button_50x40.png");
				btnSlotSelection[step].Location = new System.Drawing.Point(offset_x+(button_width+button_sep)*(step), 10);
				btnSlotSelection[step].Name = "Button1";
				btnSlotSelection[step].Size = new System.Drawing.Size(button_width, 40);
				btnSlotSelection[step].TabIndex = 8;
				btnSlotSelection[step].ButtonFont = MyDefaultSkinFontBold8;
				btnSlotSelection[step].Tag = step;
				btnSlotSelection[step].SetButtonText(display_text,
					System.Drawing.Color.Black,System.Drawing.Color.Black,
					System.Drawing.Color.Green,System.Drawing.Color.Gray);
				btnSlotSelection[step].Click += new System.EventHandler(this.SlotSelect_Button_Click);
				btnSlotSelection[step].Enabled = !project_active;
				this.pnl_ChooseSlot.Controls.Add(btnSlotSelection[step]);
			}

			if (active_projects>0)
			{
				if (UseDataEntryChecks)
				{
					//we only need this if there are projects and we are checking against team sizes
					//newBtnFixTeamSizes.SetButton(AppInfo.TheInstance.Location+"\\images\\buttons\\button_255x25.png");
					newBtnFixTeamSizes.SetVariants("images\\buttons\\button_50x40.png");
					newBtnFixTeamSizes.Location = new System.Drawing.Point(10, 70);
					newBtnFixTeamSizes.Name = "newBtnFixTeamSizes";
					newBtnFixTeamSizes.Size = new System.Drawing.Size(160, 30);
					newBtnFixTeamSizes.TabIndex = 21;
					newBtnFixTeamSizes.ButtonFont = MyDefaultSkinFontBold10;
					//newBtn.ForeColor = System.Drawing.Color.Black;
					newBtnFixTeamSizes.SetButtonText("Fix Team Allocations",
						System.Drawing.Color.Black,System.Drawing.Color.Black,
						System.Drawing.Color.Green,System.Drawing.Color.Gray);
					//newBtn.ButtonPressed += new ProjectMngmntGamePanels.ImageButton.ImageButtonEventArgsHandler(playButton_ButtonPressed);
					newBtnFixTeamSizes.Click += new System.EventHandler(this.newBtnFixTeamSizes_Click);
					newBtnFixTeamSizes.Visible = true;
					this.pnl_ChooseSlot.Controls.Add(newBtnFixTeamSizes);
				}
			}
		}

		private void SlotSelect_Button_Click(object sender, System.EventArgs e)
		{
			//Extract out the FSC from the control that was used
			if (sender is ImageTextButton)
			{
				ImageTextButton ib = (ImageTextButton)sender;
				if (ib != null)
				{
					//Extract the chosen Slot Number 
					ChosenSlotNumber = (int)(ib.Tag);

					fill_ProjectSelection(ChosenSlotNumber);

					//Need to display the Choice of Projects 
					//projectNodeToCancel= (Node)(ib.Tag);
					//SlotDisplay = projectNodeToCancel.GetIntAttribute("slot",0);
					//ChosenProject_NodeName = projectNodeToCancel.GetAttribute("name"); 
					helpLabel.Text = "Select Project Number";
					this.pnl_ChooseSlot.Visible = false;
					this.pnl_ChooseProject.Visible = true;

					//Need to display the Slot Display Side Button 
					string display_text = CONVERT.ToStr(ChosenSlotNumber+1);
					this.slotLabel.Visible = true;
					this.btnSlot.SetButtonText(display_text,
						System.Drawing.Color.Black,System.Drawing.Color.Black,
						System.Drawing.Color.Green,System.Drawing.Color.Gray);
					this.btnSlot.Visible = true;
				}
			}
		}


		private void ProjectSelect_Click(object sender, System.EventArgs e)
		{
			//Extract out the FSC from the control that was used
			if (sender is ImageTextButton)
			{
				ImageTextButton ib = (ImageTextButton)sender;
				if (ib != null)
				{
					//Extract the chosen Slot Number 
					ChosenProjectNumber = (int)(ib.Tag);

					//Need to display the Slot Display Side Button 
					projectLabel.Visible = true;
					btnProject.SetButtonText(CONVERT.ToStr(ChosenProjectNumber),
						System.Drawing.Color.Black,System.Drawing.Color.Black,
						System.Drawing.Color.Green,System.Drawing.Color.Gray);
					btnProject.Visible = true;

					switch (StaffSelectionMode)
					{
						case emProjectStaffSelection.USEPHASEDEFINITION:
						case emProjectStaffSelection.USESTAGEDEFINITION:

							fill_ProductSelection(ChosenProjectNumber);
							//Need to display the Choice of Projects 
							//projectNodeToCancel= (Node)(ib.Tag);
							//SlotDisplay = projectNodeToCancel.GetIntAttribute("slot",0);
							//ChosenProject_NodeName = projectNodeToCancel.GetAttribute("name"); 
							helpLabel.Text = "Select Product Number";
							this.pnl_ChooseProject.Visible = false;
							this.pnl_ChooseProduct.Visible = true;

							break;
						case emProjectStaffSelection.USEPREDEFINITION:
							this.pnl_ChooseProject.Visible = false; 
							this.pnl_ConfirmPredefine.Visible = true;
							helpLabel.Text = "Please confirm Project Selection";

							btnOK.Visible = true;
							btnOK.Enabled = true;
							break;
					}
				}
			}
		}

		private void ProductSelect_Click(object sender, System.EventArgs e)
		{
			//Extract out the FSC from the control that was used
			if (sender is ImageTextButton)
			{
				ImageTextButton ib = (ImageTextButton)sender;
				if (ib != null)
				{
					//Extract the chosen Slot Number 
					ChosenProductNumber = (int)(ib.Tag);

					//Need to display the Slot Display Side Button 
					productLabel.Visible = true;
					btnProduct.SetButtonText(CONVERT.ToStr(ChosenProductNumber),
						System.Drawing.Color.Black,System.Drawing.Color.Black,
						System.Drawing.Color.Green,System.Drawing.Color.Gray);
					btnProduct.Visible = true;

					fill_PlatformSelection(ChosenProjectNumber, ChosenProductNumber);
					//Need to display the Choice of Projects 
					//projectNodeToCancel= (Node)(ib.Tag);
					//SlotDisplay = projectNodeToCancel.GetIntAttribute("slot",0);
					//ChosenProject_NodeName = projectNodeToCancel.GetAttribute("name"); 
					helpLabel.Text = "Select Platform";
					this.pnl_ChooseProduct.Visible = false;
					this.pnl_ChoosePlatform.Visible = true;
				}
			}
		}

		private void SetupExpertsDefinition()
		{
			helpLabel.Text = "Select the Project Experts";
			this.pnl_ChooseExperts.Visible = true;
			this.btnNext.Enabled = true;
			this.btnNext.Visible = true;
			this.btnNext.SetButtonText("OK");
		}

		private void SetupResourceDefinition(bool IsFinalPanel)
		{
			work_stage ws_stage_a = null;
			work_stage ws_stage_b = null;
			work_stage ws_stage_c = null;
			work_stage ws_stage_d = null;
			work_stage ws_stage_e = null;
			work_stage ws_stage_f = null;
			work_stage ws_stage_g = null;
			work_stage ws_stage_h = null;

			Def_Project project_data = null;
			Def_Product product_data = null;
			Def_Platform platform_data = null;

			//Need to display the Choice of Projects 
			//projectNodeToCancel= (Node)(ib.Tag);
			//SlotDisplay = projectNodeToCancel.GetIntAttribute("slot",0);
			//ChosenProject_NodeName = projectNodeToCancel.GetAttribute("name"); 
			helpLabel.Text = "Enter Your Selected Resources";
			this.pnl_ChoosePlatform.Visible = false;

			switch (StaffSelectionMode)
			{
				case emProjectStaffSelection.USEPHASEDEFINITION:
					this.pnl_ChooseResource.Visible = true;
					pnl_ChooseResource.Focus();
					tbDesignTeamSize.Focus();
					break;
				case emProjectStaffSelection.USESTAGEDEFINITION:
					//Need to extract the number of tasks from the picked definition 
					DataLookup.ProjectLookup.TheInstance.getProjectData(
						this.ChosenProjectNumber, this.ChosenProductNumber, ChosenPlatformNumber,
						out project_data, out product_data, out platform_data);

				//Fill the staff with a sample to allow the predicted days calculator to work
					int [] internals = new int [] { 1, 1, 1, 1, 1, 1, 1, 1 };
					int [] externals = new int [] { 0, 0, 0, 0, 0, 0, 0, 0 };
					bool [] locked = new bool [] { false, false, false, false, false, false, false, false };

					for (int i = 0; i < GameConstants.GetAllStageNames().Length; i++)
					{
						work_stage workStage = platform_data.getProjectWorkStage((emPHASE_STAGE) (i + ((int) emPHASE_STAGE.STAGE_A)));
						int numberOfSubTasks;

						if (workStage.DoWeConsistOnlyOfSequentialSubtasks(out numberOfSubTasks))
						{
							internals[i] = numberOfSubTasks;
							externals[i] = 0;
							locked[i] = true;
						}
					}

					this.MyPrjResStageEditor.setDefaultResourceLevels(internals, externals, locked);

					//TODO Handle the recycle stages as well 
					ws_stage_a = platform_data.getProjectWorkStage(emPHASE_STAGE.STAGE_A);
					ws_stage_b = platform_data.getProjectWorkStage(emPHASE_STAGE.STAGE_B);
					ws_stage_c = platform_data.getProjectWorkStage(emPHASE_STAGE.STAGE_C);
					ws_stage_d = platform_data.getProjectWorkStage(emPHASE_STAGE.STAGE_D);
					ws_stage_e = platform_data.getProjectWorkStage(emPHASE_STAGE.STAGE_E);
					ws_stage_f = platform_data.getProjectWorkStage(emPHASE_STAGE.STAGE_F);
					ws_stage_g = platform_data.getProjectWorkStage(emPHASE_STAGE.STAGE_G);
					ws_stage_h = platform_data.getProjectWorkStage(emPHASE_STAGE.STAGE_H);

					this.MyPrjResStageEditor.setProjectTaskLengthsDirect(ws_stage_a, ws_stage_b, 
						ws_stage_c, ws_stage_d, ws_stage_e, ws_stage_f, ws_stage_g, ws_stage_h);

					this.MyPrjResStageEditor.handleDaysDisplays();

					this.MyPrjResStageEditor.Visible = true;
					MyPrjResStageEditor.SetFocus();
					break;
				case emProjectStaffSelection.USEPREDEFINITION:
					helpLabel.Text = "Press OK to accept selected Project";
					this.pnl_ConfirmPredefine.Visible = true;
					pnl_ConfirmPredefine.Focus();
					this.btnOK.Focus();
					break;
			}
			btnOK.Visible = true;
			btnOK.Enabled = true;
			if (IsFinalPanel)
			{
				btnOK.SetButtonText("OK");
			}
			else
			{
				btnOK.SetButtonText("Next");
			}
		}

		private void PlatformSelect_Click(object sender, System.EventArgs e)
		{
			//Extract out the FSC from the control that was used
			if (sender is ImageTextButton)
			{
				ImageTextButton ib = (ImageTextButton)sender;
				if (ib != null)
				{
					//=======================================================================
					//Handle the chosen Platform 
					//=======================================================================
					//Extract the chosen Slot Number 
					ChosenPlatformNumber = (int)(ib.Tag);
					//Need to display the Slot Display Side Button 
					platformLabel.Visible = true;
					string display_test = ProjectLookup.TheInstance.TranslatePlatformToStr(ChosenPlatformNumber);
					btnPlatform.SetButtonText(display_test,
						System.Drawing.Color.Black,System.Drawing.Color.Black,
						System.Drawing.Color.Green,System.Drawing.Color.Gray);
					btnPlatform.Visible = true;

					this.pnl_ChoosePlatform.Visible = false;

					//if ((use_experts_round3) & (experts_system_enabled))
					//{
					//  SetupExpertsDefinition();
					//}
					//else
					//{
					//  SetupResourceDefinition();
					//}

					bool isFinal = false;
					if ((use_experts_round3) & (experts_system_enabled))
					{
						SetupResourceDefinition(isFinal);
					}
					else
					{
						isFinal = true;
						SetupResourceDefinition(isFinal);
					}

				}
			}
		}

		//private void btnUseExperts_Click(object sender, System.EventArgs e)
		//{
		//  //Extract out the FSC from the control that was used
		//  if (sender is ImageTextButton)
		//  {
		//    ImageTextButton ib = (ImageTextButton)sender;
		//    if (ib != null)
		//    {
		//      //Do not proceed unless all 3 experts selected 
		//      if ((this.cbExperts_DA.SelectedIndex == -1) |
		//        (this.cbExperts_BM.SelectedIndex == -1) |
		//        (this.cbExperts_TM.SelectedIndex == -1))
		//      {
		//        MessageBox.Show("Please select 3 experts.", "Project Experts Selection Incomplete");
		//      }
		//      else
		//      { 
		//        //Need to display the Slot Display Side Button 
		//        expertsLabel.Visible = true;
		//        string display_test = ProjectLookup.TheInstance.TranslatePlatformToStr(ChosenPlatformNumber);
		//        btnExperts.SetButtonText("SET",
		//          System.Drawing.Color.Black, System.Drawing.Color.Black,
		//          System.Drawing.Color.Green, System.Drawing.Color.Gray);
		//        btnExperts.Visible = true;
		//        this.pnl_ChooseExperts.Visible = false;

		//        SetupResourceDefinition();
		//      }
		//    }
		//  }
		//}

		public void fill_ProjectSelection(int slot_id)
		{
		
		}

		public void fill_ProductSelection(int project_id)
		{
			ArrayList prd_list = ProjectLookup.TheInstance.getProductList(this.ChosenProjectNumber);
			prd_list.Sort();

			int index = 0;
			foreach (int product_id in prd_list)
			{
				string display_text = CONVERT.ToStr(product_id);
				btnProductSelection[index].Tag =product_id;
				btnProductSelection[index].SetButtonText(display_text,
					System.Drawing.Color.Black,System.Drawing.Color.Black,
					System.Drawing.Color.Green,System.Drawing.Color.Gray);
				btnProductSelection[index].Enabled = true;
				btnProductSelection[index].Visible = true;
				index++;
			}
			if (index<btnProductSelection.Length)
			{
				for (int step=index; step < btnProductSelection.Length; step ++)
				{
					btnProductSelection[step].Enabled = false;
					btnProductSelection[step].Visible = false;
				}
			}
		}

		private void handleValidPlatforms()
		{
			int stepX = 0;
			int stepY = 0;
			int offset_x = 0 + 115;
			int offset_y = 20;
			int buttonwidth = 48;
			int buttonheight = 37;
			int button_sep_x = 25;
			int button_sep_y = 12;

			ArrayList plt_list = ProjectLookup.TheInstance.getPlatformList(this.ChosenProjectNumber, this.ChosenProductNumber);
			plt_list.Sort();

			for (int step = 0; step < btnPlatformSelection.Length; step++)
			{
				if (btnPlatformSelection[step] != null)
				{
					if (plt_list.Contains(step + 1))
					{
						btnPlatformSelection[step].Visible = true;
						Point tmpPoint = new Point(offset_x + (buttonwidth + button_sep_x) * stepX, offset_y + (buttonheight + button_sep_y) * stepY);
						btnPlatformSelection[step].Location = tmpPoint;
						stepX++;
					}
					else
					{
						btnPlatformSelection[step].Visible = false;
					}
				}
			}
		}

		public void fill_PlatformSelection(int project_id, int product_id)
		{
			handleValidPlatforms();
		}

		#endregion Project Slot Methods 

		#region Project Button Methods 

		private void Build_ProjectButtonsPanel()
		{

			ArrayList al = ProjectLookup.TheInstance.getProjectList(showHiddenProjectChoices);
			
			int step=0;
			int stepX=0;
			int stepY=0;
			int offset_x=5;
			int offset_y=10;
			int buttonwidth=50;
			int buttonheight=40;
			int button_sep_x=10;
			int button_sep_y=10;
			int number_in_row = 6;

			if (al.Count > 12)
			{
				buttonwidth = 45;
				buttonheight = 40;
				button_sep_x = 10;
				button_sep_y = 10;
				number_in_row = 7;
			}
			al.Sort();

			foreach(int project_id in al)
			{
				string project_id_str = CONVERT.ToStr(project_id);
				bool isAlreadyActive = existingProjectsByID.ContainsKey(project_id_str);
				string display = project_id_str;

				ImageTextButton btnProjectSelect = new ImageTextButton(0);
				//btnProjectSelect.SetButton(AppInfo.TheInstance.Location+"\\images\\buttons\\button_50x40.png");
				btnProjectSelect.SetVariants("images\\buttons\\button_50x40.png");

				Point tmpPoint = new Point(offset_x+(buttonwidth+button_sep_x)*stepX,offset_y+(buttonheight+button_sep_y)*stepY);
				btnProjectSelect.Location = tmpPoint;
				btnProjectSelect.Name = "newBtnNo";
				btnProjectSelect.Size = new System.Drawing.Size(buttonwidth, buttonheight);
				btnProjectSelect.TabIndex = 26;
				btnProjectSelect.Tag = project_id;
				btnProjectSelect.ButtonFont = this.MyDefaultSkinFontBold10;
				//newBtn.ForeColor = System.Drawing.Color.Black;
				btnProjectSelect.SetButtonText(display,
					System.Drawing.Color.Black,System.Drawing.Color.Black,
					System.Drawing.Color.Green,System.Drawing.Color.Gray);
				//newBtn.ButtonPressed += new ProjectMngmntGamePanels.ImageButton.ImageButtonEventArgsHandler(playButton_ButtonPressed);
				btnProjectSelect.Click += new System.EventHandler(ProjectSelect_Click);

				btnProjectSelect.Enabled = !(isAlreadyActive);

				pnl_ChooseProject.Controls.Add(btnProjectSelect);
				step++;
				stepX++;
				if (step == number_in_row)
				{
					stepX=0;
					stepY++;
				}
			}
		}

		private void Build_ProductButtonsPanel()
		{
			int stepX=0;
			int stepY=0;
			int offset_x=5+90;
			int offset_y=10;
			int buttonwidth=50;
			int buttonheight=40;
			int button_sep_x=25;
			int button_sep_y=12;

			for (int step=0; step < 4; step++)
			{
				if (step < btnProductSelection.Length)
				{
					string display = "prd"+step;
					btnProductSelection[step] = new ImageTextButton(0);
					//btnProductSelection[step].SetButton(AppInfo.TheInstance.Location+"\\images\\buttons\\button_50x40.png");
					btnProductSelection[step].SetVariants("images\\buttons\\button_50x40.png");
					Point tmpPoint = new Point(offset_x+(buttonwidth+button_sep_x)*stepX,offset_y+(buttonheight+button_sep_y)*stepY);
					btnProductSelection[step].Location = tmpPoint;
					btnProductSelection[step].Name = "newBtnNo";
					btnProductSelection[step].Size = new System.Drawing.Size(buttonwidth, buttonheight);
					btnProductSelection[step].TabIndex = 26;
					btnProductSelection[step].Tag = 1;//project_id;
					btnProductSelection[step].ButtonFont = this.MyDefaultSkinFontBold10;
					//newBtn.ForeColor = System.Drawing.Color.Black;
					btnProductSelection[step].SetButtonText(display,
						System.Drawing.Color.Black,System.Drawing.Color.Black,
						System.Drawing.Color.Green,System.Drawing.Color.Gray);
					//newBtn.ButtonPressed += new ProjectMngmntGamePanels.ImageButton.ImageButtonEventArgsHandler(playButton_ButtonPressed);
					btnProductSelection[step].Click += new System.EventHandler(ProductSelect_Click);
					//btnProductSelection[step].Enabled = !(isAlreadyActive);
					btnProductSelection[step].Enabled = true;
					this.pnl_ChooseProduct.Controls.Add(btnProductSelection[step]);
				}
				stepX++;
				if (step==6)
				{
					stepX=0;
					stepY++;
				}
			}
		}

		private void Build_PlatformButtonsPanel()
		{
			int stepX=0;
			int stepY=0;
			int offset_x=0+115;
			int offset_y=10;
			int buttonwidth=50;
			int buttonheight=40;
			int button_sep_x=25;
			int button_sep_y=12;

			for (int step=0; step < 4; step++)
			{
				if (step < btnPlatformSelection.Length)
				{
					string display = "plt"+step;
					display = DataLookup.ProjectLookup.TheInstance.TranslatePlatformToStr(step+1);

					btnPlatformSelection[step] = new ImageTextButton(0);
					//btnPlatformSelection[step].SetButton(AppInfo.TheInstance.Location+"\\images\\buttons\\button_50x40.png");
					btnPlatformSelection[step].SetVariants("images\\buttons\\button_50x40.png");

					Point tmpPoint = new Point(offset_x+(buttonwidth+button_sep_x)*stepX,offset_y+(buttonheight+button_sep_y)*stepY);
					btnPlatformSelection[step].Location = tmpPoint;
					btnPlatformSelection[step].Name = "newBtnNo";
					btnPlatformSelection[step].Size = new System.Drawing.Size(buttonwidth, buttonheight);
					btnPlatformSelection[step].TabIndex = 26;
					btnPlatformSelection[step].Tag = step+1;
					btnPlatformSelection[step].ButtonFont = this.MyDefaultSkinFontBold10;
					//newBtn.ForeColor = System.Drawing.Color.Black;
					btnPlatformSelection[step].SetButtonText(display,
						System.Drawing.Color.Black,System.Drawing.Color.Black,
						System.Drawing.Color.Green,System.Drawing.Color.Gray);
					//newBtn.ButtonPressed += new ProjectMngmntGamePanels.ImageButton.ImageButtonEventArgsHandler(playButton_ButtonPressed);
					btnPlatformSelection[step].Click += new System.EventHandler(PlatformSelect_Click);
					//btnPlatformSelection[step].Enabled = !(isAlreadyActive);
					btnPlatformSelection[step].Enabled = true;
					btnPlatformSelection[step].Visible = false;
					this.pnl_ChoosePlatform.Controls.Add(btnPlatformSelection[step]);
				}
				stepX++;
				if (step==6)
				{
					stepX=0;
					stepY++;
				}
			}
		}

		private void Build_ExpertsPanel()
		{ 
			lbl_Experts_DA_Title = new System.Windows.Forms.Label();
			lbl_Experts_DA_Title.BackColor = System.Drawing.Color.Transparent;
			lbl_Experts_DA_Title.Font = MyDefaultSkinFontNormal10;
			lbl_Experts_DA_Title.ForeColor = System.Drawing.Color.Black;
			lbl_Experts_DA_Title.Location = new System.Drawing.Point(10, 10);
			lbl_Experts_DA_Title.Name = "titleLabel";
			lbl_Experts_DA_Title.Size = new System.Drawing.Size(190, 20);
			lbl_Experts_DA_Title.TabIndex = 11;
			lbl_Experts_DA_Title.Text = "Design Architect:";
			lbl_Experts_DA_Title.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			lbl_Experts_DA_Title.Visible = true;
			//slotLabel.BackColor = Color.CadetBlue;
			pnl_ChooseExperts.Controls.Add(lbl_Experts_DA_Title);

			cbExperts_DA = new ComboBox();
			cbExperts_DA.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			cbExperts_DA.Font = MyDefaultSkinFontNormal10;
			cbExperts_DA.ForeColor = System.Drawing.Color.Black;
			cbExperts_DA.Location = new System.Drawing.Point(200, 10);
			cbExperts_DA.Name = "titleLabel";
			cbExperts_DA.Size = new System.Drawing.Size(180, 20);
			cbExperts_DA.TabIndex = 11;
			cbExperts_DA.Visible = true;
			//slotLabel.BackColor = Color.CadetBlue;
			pnl_ChooseExperts.Controls.Add(cbExperts_DA);

			lbl_Experts_BM_Title = new System.Windows.Forms.Label();
			lbl_Experts_BM_Title.BackColor = System.Drawing.Color.Transparent;
			lbl_Experts_BM_Title.Font = MyDefaultSkinFontNormal10;
			lbl_Experts_BM_Title.ForeColor = System.Drawing.Color.Black;
			lbl_Experts_BM_Title.Location = new System.Drawing.Point(10, 40);
			lbl_Experts_BM_Title.Name = "titleLabel";
			lbl_Experts_BM_Title.Size = new System.Drawing.Size(190, 20);
			lbl_Experts_BM_Title.TabIndex = 11;
			lbl_Experts_BM_Title.Text = "Build Manager:";
			lbl_Experts_BM_Title.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			lbl_Experts_BM_Title.Visible = true;
			//slotLabel.BackColor = Color.CadetBlue;
			pnl_ChooseExperts.Controls.Add(lbl_Experts_BM_Title);

			cbExperts_BM = new ComboBox();
			cbExperts_BM.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			cbExperts_BM.Font = MyDefaultSkinFontNormal10;
			cbExperts_BM.ForeColor = System.Drawing.Color.Black;
			cbExperts_BM.Location = new System.Drawing.Point(200, 40);
			cbExperts_BM.Name = "titleLabel";
			cbExperts_BM.Size = new System.Drawing.Size(180, 20);
			cbExperts_BM.TabIndex = 11;
			cbExperts_BM.Visible = true;
			//slotLabel.BackColor = Color.CadetBlue;
			pnl_ChooseExperts.Controls.Add(cbExperts_BM);

			lbl_Experts_TM_Title = new System.Windows.Forms.Label();
			lbl_Experts_TM_Title.BackColor = System.Drawing.Color.Transparent;
			lbl_Experts_TM_Title.Font = MyDefaultSkinFontNormal10;
			lbl_Experts_TM_Title.ForeColor = System.Drawing.Color.Black;
			lbl_Experts_TM_Title.Location = new System.Drawing.Point(10, 70);
			lbl_Experts_TM_Title.Name = "titleLabel";
			lbl_Experts_TM_Title.Size = new System.Drawing.Size(190, 20);
			lbl_Experts_TM_Title.TabIndex = 11;
			lbl_Experts_TM_Title.Text = "Test Manager:";
			lbl_Experts_TM_Title.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			lbl_Experts_TM_Title.Visible = true;
			//slotLabel.BackColor = Color.CadetBlue;
			pnl_ChooseExperts.Controls.Add(lbl_Experts_TM_Title);

			cbExperts_TM = new ComboBox();
			cbExperts_TM.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			cbExperts_TM.Font = MyDefaultSkinFontNormal10;
			cbExperts_TM.ForeColor = System.Drawing.Color.Black;
			cbExperts_TM.Location = new System.Drawing.Point(200, 70);
			cbExperts_TM.Name = "titleLabel";
			cbExperts_TM.Size = new System.Drawing.Size(180, 20);
			cbExperts_TM.TabIndex = 11;
			cbExperts_TM.Visible = true;
			//slotLabel.BackColor = Color.CadetBlue;
			pnl_ChooseExperts.Controls.Add(cbExperts_TM);

			BuildCurrentExpertsLookupList();
		}

		private void Build_ResourceDefinitionPanel()
		{
			lblDesignTeamSize = new System.Windows.Forms.Label();
			lblDesignTeamSize.BackColor = System.Drawing.Color.Transparent;
			lblDesignTeamSize.Font = MyDefaultSkinFontNormal10;
			lblDesignTeamSize.ForeColor = System.Drawing.Color.Black;
			lblDesignTeamSize.Location = new System.Drawing.Point(10, 10);
			lblDesignTeamSize.Name = "titleLabel";
			lblDesignTeamSize.Size = new System.Drawing.Size(230, 20);
			lblDesignTeamSize.TabIndex = 11;
			lblDesignTeamSize.Text = "Allocate Design Resource:";
			lblDesignTeamSize.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			lblDesignTeamSize.Visible = true;
			//slotLabel.BackColor = Color.CadetBlue;
			this.pnl_ChooseResource.Controls.Add(lblDesignTeamSize);

			tbDesignTeamSize = new FilteredTextBox (TextBoxFilterType.Digits);
			tbDesignTeamSize.BackColor = System.Drawing.Color.Black;
			tbDesignTeamSize.ForeColor = System.Drawing.Color.White;
			tbDesignTeamSize.Font = this.MyDefaultSkinFontBold10;
			tbDesignTeamSize.Location = new System.Drawing.Point(240+68, 10);
			tbDesignTeamSize.Name = "locTextBox";
			tbDesignTeamSize.Size = new System.Drawing.Size(90, 22);
			tbDesignTeamSize.TabIndex = 28;
			tbDesignTeamSize.Text = "";
			tbDesignTeamSize.MaxLength = 2;
			tbDesignTeamSize.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
			this.pnl_ChooseResource.Controls.Add(tbDesignTeamSize);

			lblBuildTeamSize = new System.Windows.Forms.Label();
			lblBuildTeamSize.BackColor = System.Drawing.Color.Transparent;
			lblBuildTeamSize.Font = MyDefaultSkinFontNormal10;
			lblBuildTeamSize.ForeColor = System.Drawing.Color.Black;
			lblBuildTeamSize.Location = new System.Drawing.Point(10, 40);
			lblBuildTeamSize.Name = "titleLabel";
			lblBuildTeamSize.Size = new System.Drawing.Size(230, 20);
			lblBuildTeamSize.TabIndex = 11;
			lblBuildTeamSize.Text = "Allocate Build Resource:";
			lblBuildTeamSize.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			lblBuildTeamSize.Visible = true;
			//slotLabel.BackColor = Color.CadetBlue;
			this.pnl_ChooseResource.Controls.Add(lblBuildTeamSize);

			tbBuildTeamSize = new FilteredTextBox (TextBoxFilterType.Digits);
			tbBuildTeamSize.BackColor = System.Drawing.Color.Black;
			tbBuildTeamSize.ForeColor = System.Drawing.Color.White;
			tbBuildTeamSize.Font = this.MyDefaultSkinFontBold10;
			tbBuildTeamSize.Location = new System.Drawing.Point(240+68, 40);
			tbBuildTeamSize.Name = "locTextBox";
			tbBuildTeamSize.Size = new System.Drawing.Size(90, 22);
			tbBuildTeamSize.TabIndex = 28;
			tbBuildTeamSize.Text = "";
			tbBuildTeamSize.MaxLength = 2;
			tbBuildTeamSize.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
			this.pnl_ChooseResource.Controls.Add(tbBuildTeamSize);

			lblTestTeamSize = new System.Windows.Forms.Label();
			lblTestTeamSize.BackColor = System.Drawing.Color.Transparent;
			lblTestTeamSize.Font = MyDefaultSkinFontNormal10;
			lblTestTeamSize.ForeColor = System.Drawing.Color.Black;
			lblTestTeamSize.Location = new System.Drawing.Point(10, 70);
			lblTestTeamSize.Name = "titleLabel";
			lblTestTeamSize.Size = new System.Drawing.Size(230, 20);
			lblTestTeamSize.TabIndex = 11;
			lblTestTeamSize.Text = "Allocate Test Resource:";
			lblTestTeamSize.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			lblTestTeamSize.Visible = true;
			//slotLabel.BackColor = Color.CadetBlue;
			this.pnl_ChooseResource.Controls.Add(lblTestTeamSize);

			tbTestTeamSize = new FilteredTextBox (TextBoxFilterType.Digits);
			tbTestTeamSize.BackColor = System.Drawing.Color.Black;
			tbTestTeamSize.Font = this.MyDefaultSkinFontBold10;
			tbTestTeamSize.ForeColor = System.Drawing.Color.White;
			tbTestTeamSize.Location = new System.Drawing.Point(240+68, 70);
			tbTestTeamSize.Name = "locTextBox";
			tbTestTeamSize.Size = new System.Drawing.Size(90, 22);
			tbTestTeamSize.TabIndex = 28;
			tbTestTeamSize.Text = "";
			tbTestTeamSize.MaxLength = 2;
			tbTestTeamSize.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
			this.pnl_ChooseResource.Controls.Add(tbTestTeamSize);

			lblBudget = new System.Windows.Forms.Label();
			lblBudget.BackColor = System.Drawing.Color.Transparent;
			lblBudget.Font = MyDefaultSkinFontNormal10;
			lblBudget.ForeColor = System.Drawing.Color.Black;
			lblBudget.Location = new System.Drawing.Point(10, 100);
			lblBudget.Name = "titleLabel";
			lblBudget.Size = new System.Drawing.Size(200, 20);
			lblBudget.TabIndex = 11;
			lblBudget.Text = projectTerm + " Budget ($K):";
			lblBudget.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			lblBudget.Visible = true;
			//lblBudget.BackColor = Color.CadetBlue;
			this.pnl_ChooseResource.Controls.Add(lblBudget);

			tbBudget = new FilteredTextBox (TextBoxFilterType.Digits);
			tbBudget.BackColor = System.Drawing.Color.Black;
			tbBudget.Font = this.MyDefaultSkinFontBold10;
			tbBudget.ForeColor = System.Drawing.Color.White;
			tbBudget.Location = new System.Drawing.Point(240 + 68, 100);
			tbBudget.Name = "locTextBox";
			tbBudget.Size = new System.Drawing.Size(90, 22);
			tbBudget.TabIndex = 28;
			tbBudget.Text = "";
			tbBudget.MaxLength = 4;
			tbBudget.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
			this.pnl_ChooseResource.Controls.Add(tbBudget);

//			System.Windows.Forms.Label lblBudgetK = new System.Windows.Forms.Label();
//			lblBudgetK.BackColor = System.Drawing.Color.Transparent;
//			lblBudgetK.Font = MyDefaultSkinFontNormal10;
//			lblBudgetK.ForeColor = System.Drawing.Color.Black;
//			lblBudgetK.Location = new System.Drawing.Point(335, 100);
//			lblBudgetK.Name = "titleLabel";
//			lblBudgetK.Size = new System.Drawing.Size(20, 20);
//			lblBudgetK.TabIndex = 11;
//			lblBudgetK.Text = "K";
//			lblBudgetK.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
//			lblBudgetK.Visible = true;
//			//slotLabel.BackColor = Color.CadetBlue;
//			this.pnl_ChooseResource.Controls.Add(lblBudgetK);

		}

		#endregion Project Button Methods 

		private void newBtnFixTeamSizes_Click(object sender, System.EventArgs e)
		{
			titleLabel.Text = "CHANGE THE TEAM ALLOCATIONS";
			helpLabel.Text = "";

			titleLabel.Location = new System.Drawing.Point(10, 20);
			titleLabel.Size = new System.Drawing.Size(500, 20);
			helpLabel.Location = new System.Drawing.Point(0, 50);
			helpLabel.Size = new System.Drawing.Size(510, 40);

			MyGlobalTeamEditor.setGlobalLimits(globalStaffLimit_DevInt, globalStaffLimit_DevExt,
				globalStaffLimit_DevInt, globalStaffLimit_DevExt);
			MyGlobalTeamEditor.FillControls();

			this.BackgroundImage = wide_back;
			this.pnl_ChooseSlot.Visible = false;

			this.pnl_FixTeamSizes.Visible = true;
			this.pnl_FixTeamSizes.BringToFront();
			this.btnOK.Visible = true;
			this.btnOK.Enabled = true;
		}

		private int getBudgetLeft()
		{
			Node BudgetNode = MyNodeTree.GetNamedNode("pmo_budget"); 
			int budget_value = BudgetNode.GetIntAttribute("budget_left",0);
			return budget_value;
		}

//		private int getEmployeeMaximu()
//		{
//			Node BudgetNode = MyNodeTree.GetNamedNode("pmo_budget"); 
//			int budget_value = BudgetNode.GetIntAttribute("budget_left",0);
//			return budget_value;
//		}

		private bool doNumbersFail(ArrayList errs, int request_resource, int dev_limit, string name)
		{
			bool errorFlag = false;

			if ((request_resource <1)|(request_resource > dev_limit))
			{
				if (request_resource ==-1)
				{
					errorFlag = true;
					errs.Add ("Requested "+name+" Resource is not allowed");
				}
				else
				{
					if (request_resource==0)
					{
						errorFlag = true;
						errs.Add ("Requested "+name+" Resource is not allowed to be 0");
					}
					if (request_resource>dev_limit)
					{
						errorFlag = true;
						//errs.Add ("Requested "+name+" Resource is too high (>"+CONVERT.ToStr(dev_limit)+")");
						errs.Add("Requested " + name + " Resource is too high");
					}
				}
			}
			return errorFlag;
		}


		private bool CheckNormalProjectSelection(out ArrayList errs)
		{
			bool proceed = true;
			errs = new ArrayList();

			int requestSlotNumber = this.ChosenSlotNumber;
			int requestProjectNumber = this.ChosenProjectNumber;
			int requestProductNumber = this.ChosenProductNumber;
			int requestPlatformNumber = this.ChosenPlatformNumber;
			int requestDesignResource = CONVERT.ParseIntSafe(this.tbDesignTeamSize.Text,-1);
			int requestBuildResource = CONVERT.ParseIntSafe(this.tbBuildTeamSize.Text,-1);
			int requestTestResource = CONVERT.ParseIntSafe(this.tbTestTeamSize.Text,-1);
			int requestBudget = CONVERT.ParseIntSafe(this.tbBudget.Text,-1);		

			//Check the budget 
			if (requestBudget<0)
			{
				proceed = false;
				errs.Add ("Requested Budget must be defined");
			}
			requestBudget = requestBudget * 1000;

			int dev_limit = globalStaffLimit_DevInt;
			int test_limit = globalStaffLimit_TestInt;

			//In the single section mode, we just have one group of staff (dev people)
			//so the limits for the test ui inputs as the same as the dev ones
			if (use_single_staff_section)
			{
				test_limit = globalStaffLimit_DevInt;
			}

			//Check the People 
			if (doGobalLimitsIncludeConsultants)
			{
				dev_limit = globalStaffLimit_DevInt + globalStaffLimit_DevExt;
				test_limit = globalStaffLimit_TestInt + globalStaffLimit_TestExt;
			}

			doNumbersFail(errs, requestDesignResource, dev_limit, "Design");
			doNumbersFail(errs, requestBuildResource, dev_limit, "Build");
			doNumbersFail(errs, requestTestResource, test_limit, "Test");
			if (errs.Count>0)
			{
				proceed = false;
			}

			if (requestBudget>getBudgetLeft())
			{
				proceed = false;
				errs.Add ("Insufficient Money for Project Request");
			}
			return proceed;
		}


		private bool HandleNormalProjectSelection()
		{
			bool proceed = true;
			ArrayList errs = new ArrayList();
			string design_expert = "";
			string build_expert = "";
			string test_expert = "";

			int requestSlotNumber = this.ChosenSlotNumber;
			int requestProjectNumber = this.ChosenProjectNumber;
			int requestProductNumber = this.ChosenProductNumber;
			int requestPlatformNumber = this.ChosenPlatformNumber;
			int requestDesignResource = CONVERT.ParseIntSafe(this.tbDesignTeamSize.Text,-1);
			int requestBuildResource = CONVERT.ParseIntSafe(this.tbBuildTeamSize.Text,-1);
			int requestTestResource = CONVERT.ParseIntSafe(this.tbTestTeamSize.Text,-1);
			int requestBudget = CONVERT.ParseIntSafe(this.tbBudget.Text,-1);
			requestBudget = requestBudget * 1000;

			if (proceed)
			{
				//The command requires each stage to be defined seperatly as this is standard PM behaviour 
				//The new CA PM is different in that we define levels in Design,Build,Test
				//
				//If we are defining just Dev and Test (old PM Interface)
				//then Design=Dev, Build=Dev and Test=Test

				if ((use_experts_round3) & (experts_system_enabled))
				{
					//extract out the defined experts
					design_expert = (string)this.cbExperts_DA.SelectedItem;
					build_expert = (string)this.cbExperts_BM.SelectedItem;
					test_expert = (string)this.cbExperts_TM.SelectedItem;
				}

				ArrayList attrs = new ArrayList ();
				attrs.Add(new AttributeValuePair ("cmd_type", "request_new_project"));
				attrs.Add(new AttributeValuePair ("slotid", CONVERT.ToStr(requestSlotNumber)));
				attrs.Add(new AttributeValuePair ("prjid", CONVERT.ToStr(requestProjectNumber)));
				attrs.Add(new AttributeValuePair ("prdid", CONVERT.ToStr(requestProductNumber)));
				attrs.Add(new AttributeValuePair ("pltid", CONVERT.ToStr(requestPlatformNumber)));

				attrs.Add(new AttributeValuePair ("design_res_level", CONVERT.ToStr(requestDesignResource)));
				attrs.Add(new AttributeValuePair ("build_res_level", CONVERT.ToStr(requestBuildResource)));
				attrs.Add(new AttributeValuePair ("test_res_level", CONVERT.ToStr(requestTestResource)));

				attrs.Add(new AttributeValuePair("design_expert", design_expert));
				attrs.Add(new AttributeValuePair("build_expert", build_expert));
				attrs.Add(new AttributeValuePair("test_expert", test_expert));

				attrs.Add(new AttributeValuePair ("stage_a_internal", CONVERT.ToStr(requestDesignResource)));
				attrs.Add(new AttributeValuePair ("stage_a_external", CONVERT.ToStr(0)));
				attrs.Add(new AttributeValuePair ("stage_b_internal", CONVERT.ToStr(requestDesignResource)));
				attrs.Add(new AttributeValuePair ("stage_b_external", CONVERT.ToStr(0)));
				attrs.Add(new AttributeValuePair ("stage_c_internal", CONVERT.ToStr(requestDesignResource)));
				attrs.Add(new AttributeValuePair ("stage_c_external", CONVERT.ToStr(0)));
				attrs.Add(new AttributeValuePair ("stage_d_internal", CONVERT.ToStr(requestBuildResource)));
				attrs.Add(new AttributeValuePair ("stage_d_external", CONVERT.ToStr(0)));
				attrs.Add(new AttributeValuePair ("stage_e_internal", CONVERT.ToStr(requestBuildResource)));
				attrs.Add(new AttributeValuePair ("stage_e_external", CONVERT.ToStr(0)));
				attrs.Add(new AttributeValuePair ("stage_f_internal", CONVERT.ToStr(requestTestResource)));
				attrs.Add(new AttributeValuePair ("stage_f_external", CONVERT.ToStr(0)));
				attrs.Add(new AttributeValuePair ("stage_g_internal", CONVERT.ToStr(requestTestResource)));
				attrs.Add(new AttributeValuePair ("stage_g_external", CONVERT.ToStr(0)));
				attrs.Add(new AttributeValuePair ("stage_h_internal", CONVERT.ToStr(requestTestResource)));
				attrs.Add(new AttributeValuePair ("stage_h_external", CONVERT.ToStr(0)));
				attrs.Add(new AttributeValuePair ("budget", CONVERT.ToStr(requestBudget)));
				attrs.Add(new AttributeValuePair ("delay_days", CONVERT.ToStr(0)));
				attrs.Add(new AttributeValuePair ("use_prefered_staff_levels", "false"));
				//attrs.Add(new AttributeValuePair ("day", day_number));
				new Node (queueNode, "task", "", attrs);	
			}
			return true;
		}

		private bool CheckFullProjectSelection(out ArrayList errs)
		{
			bool proceed = true;
			errs = new ArrayList();

			int requestSlotNumber = this.ChosenSlotNumber;
			int requestProjectNumber = this.ChosenProjectNumber;
			int requestProductNumber = this.ChosenProductNumber;
			int requestPlatformNumber = this.ChosenPlatformNumber;
			int requestDesignResource = CONVERT.ParseIntSafe(this.tbDesignTeamSize.Text,-1);
			int requestBuildResource = CONVERT.ParseIntSafe(this.tbBuildTeamSize.Text,-1);
			int requestTestResource = CONVERT.ParseIntSafe(this.tbTestTeamSize.Text,-1);
			int requestBudget = CONVERT.ParseIntSafe(this.tbBudget.Text,-1);		

			bool checkProjectBudgetAlteration = false;
			bool checkBudgetCreation = true;

			this.MyPrjResStageEditor.CheckOrder(checkProjectBudgetAlteration, checkBudgetCreation, 
				pmo_budgettotal, pmo_budgetspent, pmo_budgetleft, 0,0,0,
				globalStaffLimit_DevInt, globalStaffLimit_DevExt, 
				globalStaffLimit_TestInt, globalStaffLimit_TestExt, 
				out errs);

			if (errs.Count>0)
			{
				proceed = false;
			}
			
			return proceed;
		}

		private bool HandleFullProjectSelection()
		{
			bool proceed = true;
			ArrayList errs = new ArrayList();
			string design_expert = "";
			string build_expert = "";
			string test_expert = "";
			int newbudget = 1;
			int delay_days = 0;
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

			int requestSlotNumber = this.ChosenSlotNumber;
			int requestProjectNumber = this.ChosenProjectNumber;
			int requestProductNumber = this.ChosenProductNumber;
			int requestPlatformNumber = this.ChosenPlatformNumber;

			this.MyPrjResStageEditor.ExtractDataFromControls(out newbudget, out delay_days,
				out staff_a_internal, out staff_a_external, out staff_b_internal, out staff_b_external,
				out staff_c_internal, out staff_c_external, out staff_d_internal, out staff_d_external,
				out staff_e_internal, out staff_e_external, out staff_f_internal, out staff_f_external,
				out staff_g_internal, out staff_g_external, out staff_h_internal, out staff_h_external);

			int requestDesignResource = 0;
			int requestBuildResource = 0;
			int requestTestResource = 0;

			if (staff_a_internal>requestDesignResource)
			{
				requestDesignResource = staff_a_internal;
			}
			if (staff_b_internal>requestDesignResource)
			{
				requestDesignResource = staff_b_internal;
			}
			if (staff_c_internal>requestDesignResource)
			{
				requestDesignResource = staff_c_internal;
			}

			if (staff_d_internal>requestBuildResource)
			{
				requestBuildResource = staff_d_internal;
			}
			if (staff_e_internal>requestBuildResource)
			{
				requestBuildResource = staff_e_internal;
			}

			if (staff_f_internal>requestTestResource)
			{
				requestTestResource = staff_f_internal;
			}
			if (staff_g_internal>requestTestResource)
			{
				requestTestResource = staff_g_internal;
			}
			if (staff_h_internal>requestTestResource)
			{
				requestTestResource = staff_h_internal;
			}

			if (proceed)
			{
				if ((use_experts_round3) & (experts_system_enabled))
				{
					//extract out the defined experts
					design_expert = (string) this.cbExperts_DA.SelectedItem;
					build_expert = (string) this.cbExperts_BM.SelectedItem;
					test_expert = (string) this.cbExperts_TM.SelectedItem;
				}

				//The command requires each stage to be defined seperatly as this is standard PM behaviour 
				//The new CA PM is different in that we define levels in Design,Build,Test
				//
				//If we are defining just Dev and Test (old PM Interface)
				//then Design=Dev, Build=Dev and Test=Test

				ArrayList attrs = new ArrayList ();
				attrs.Add(new AttributeValuePair ("cmd_type", "request_new_project"));
				attrs.Add(new AttributeValuePair ("slotid", CONVERT.ToStr(requestSlotNumber)));
				attrs.Add(new AttributeValuePair ("prjid", CONVERT.ToStr(requestProjectNumber)));
				attrs.Add(new AttributeValuePair ("prdid", CONVERT.ToStr(requestProductNumber)));
				attrs.Add(new AttributeValuePair ("pltid", CONVERT.ToStr(requestPlatformNumber)));

				attrs.Add(new AttributeValuePair ("design_res_level", CONVERT.ToStr(requestDesignResource)));
				attrs.Add(new AttributeValuePair ("build_res_level", CONVERT.ToStr(requestBuildResource)));
				attrs.Add(new AttributeValuePair ("test_res_level", CONVERT.ToStr(requestTestResource)));

				attrs.Add(new AttributeValuePair("design_expert", design_expert));
				attrs.Add(new AttributeValuePair("build_expert", build_expert));
				attrs.Add(new AttributeValuePair("test_expert", test_expert));
				
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
				attrs.Add(new AttributeValuePair ("budget", CONVERT.ToStr(newbudget)));
				attrs.Add(new AttributeValuePair ("delay_days", CONVERT.ToStr(delay_days)));
				attrs.Add(new AttributeValuePair ("use_prefered_staff_levels", "false"));
				//attrs.Add(new AttributeValuePair ("day", day_number));
				new Node (queueNode, "task", "", attrs);	
			}
			return true;
		}

		private bool CheckPredefinedProjectSelection(out ArrayList errs)
		{
			bool proceed = true;
			errs = new ArrayList();

			int requestSlotNumber = this.ChosenSlotNumber;
			int requestProjectNumber = this.ChosenProjectNumber;
			int requestProductNumber = this.ChosenProductNumber;
			int requestPlatformNumber = this.ChosenPlatformNumber;
			int requestDesignResource = CONVERT.ParseIntSafe(this.tbDesignTeamSize.Text, -1);
			int requestBuildResource = CONVERT.ParseIntSafe(this.tbBuildTeamSize.Text, -1);
			int requestTestResource = CONVERT.ParseIntSafe(this.tbTestTeamSize.Text, -1);
			int requestBudget = CONVERT.ParseIntSafe(this.tbBudget.Text, -1);

			return proceed;
		}

		private bool HandlePredefinedProjectSelection()
		{
			bool proceed = true;
			ArrayList errs = new ArrayList();
			int newbudget = 1;
			int delay_days = 0;
			string design_expert = "";
			string build_expert = "";
			string test_expert = "";

			int requestSlotNumber = this.ChosenSlotNumber;
			int requestProjectNumber = this.ChosenProjectNumber;
			int requestProductNumber = 0;
			int requestPlatformNumber = 0;

			if (proceed)
			{
				if ((use_experts_round3) & (experts_system_enabled))
				{
					//extract out the defined experts
					design_expert = (string)this.cbExperts_DA.SelectedItem;
					build_expert = (string)this.cbExperts_BM.SelectedItem;
					test_expert = (string)this.cbExperts_TM.SelectedItem;
				}

				//In this request, we are requesting that the task manager use the predefined staff levels
				//These staff levels are defined in the SIP XML for each stage and define both internal and external

				ArrayList attrs = new ArrayList();
				attrs.Add(new AttributeValuePair("cmd_type", "request_new_project"));
				attrs.Add(new AttributeValuePair("slotid", CONVERT.ToStr(requestSlotNumber)));
				attrs.Add(new AttributeValuePair("prjid", CONVERT.ToStr(requestProjectNumber)));
				attrs.Add(new AttributeValuePair("prdid", CONVERT.ToStr(requestProductNumber)));
				attrs.Add(new AttributeValuePair("pltid", CONVERT.ToStr(requestPlatformNumber)));

				attrs.Add(new AttributeValuePair("design_res_level", "0"));
				attrs.Add(new AttributeValuePair("build_res_level", "0"));
				attrs.Add(new AttributeValuePair("test_res_level", "0"));

				attrs.Add(new AttributeValuePair("design_expert", design_expert.ToLower()));
				attrs.Add(new AttributeValuePair("build_expert", build_expert.ToLower()));
				attrs.Add(new AttributeValuePair("test_expert", test_expert.ToLower()));

				attrs.Add(new AttributeValuePair("stage_a_internal", "0")); 
				attrs.Add(new AttributeValuePair("stage_a_external", "0")); 
				attrs.Add(new AttributeValuePair("stage_b_internal", "0")); 
				attrs.Add(new AttributeValuePair("stage_b_external", "0")); 
				attrs.Add(new AttributeValuePair("stage_c_internal", "0")); 
				attrs.Add(new AttributeValuePair("stage_c_external", "0")); 
				attrs.Add(new AttributeValuePair("stage_d_internal", "0")); 
				attrs.Add(new AttributeValuePair("stage_d_external", "0")); 
				attrs.Add(new AttributeValuePair("stage_e_internal", "0")); 
				attrs.Add(new AttributeValuePair("stage_e_external", "0")); 
				attrs.Add(new AttributeValuePair("stage_f_internal", "0")); 
				attrs.Add(new AttributeValuePair("stage_f_external", "0")); 
				attrs.Add(new AttributeValuePair("stage_g_internal", "0")); 
				attrs.Add(new AttributeValuePair("stage_g_external", "0")); 
				attrs.Add(new AttributeValuePair("stage_h_internal", "0"));
				attrs.Add(new AttributeValuePair("stage_h_external", "0")); 
				attrs.Add(new AttributeValuePair("budget", CONVERT.ToStr(newbudget)));
				attrs.Add(new AttributeValuePair("delay_days", CONVERT.ToStr(delay_days)));
				attrs.Add(new AttributeValuePair("use_prefered_staff_levels", "true"));
				attrs.Add(new AttributeValuePair("use_auto_start", "true"));
				//attrs.Add(new AttributeValuePair ("day", day_number));
				new Node(queueNode, "task", "", attrs);
			}
			return true;
		}

		private bool CheckFixTeamsRequest(out ArrayList errs)
		{
			bool proceed = true;
			errs= new ArrayList();

			proceed = MyGlobalTeamEditor.checkData(out errs);
			return proceed;
		}

		private bool HandleFixTeamsRequest()
		{
			bool proceed = true;
			ArrayList errs = new ArrayList();

			proceed = MyGlobalTeamEditor.HandleRequest(queueNode);
			return proceed;
		}

		private void btnNext_Click(object sender, System.EventArgs e)
		{
			//Do not proceed unless all 3 experts selected 
			if ((this.cbExperts_DA.SelectedIndex == -1) |
				(this.cbExperts_BM.SelectedIndex == -1) |
				(this.cbExperts_TM.SelectedIndex == -1))
			{
				MessageBox.Show("Please select 3 experts.", "Project Experts Selection Incomplete");
			}
			else
			{
				this.btnNext.Visible = false;
				//Need to display the Slot Display Side Button 

				expertsLabel.Visible = true;
				string display_test = ProjectLookup.TheInstance.TranslatePlatformToStr(ChosenPlatformNumber);
				btnExperts.SetButtonText("SET",
					System.Drawing.Color.Black, System.Drawing.Color.Black,
					System.Drawing.Color.Green, System.Drawing.Color.Gray);
				btnExperts.Visible = true;
				this.pnl_ChooseExperts.Visible = false;

				//SetupResourceDefinition();
				this.HandledFinalOK();
			}
		}

		private void HandledFinalOK()
		{
			bool good_order_placed = false;
			ArrayList errs = new ArrayList();

			switch (StaffSelectionMode)
			{
				case emProjectStaffSelection.USEPHASEDEFINITION:
					//We are defining Resources on a Phase Basis 
					good_order_placed = CheckNormalProjectSelection(out errs);
					if (good_order_placed)
					{
						good_order_placed = HandleNormalProjectSelection();
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
					break;
				case emProjectStaffSelection.USESTAGEDEFINITION:
					//Using the full stage resource definition 
					good_order_placed = CheckFullProjectSelection(out errs);
					if (good_order_placed)
					{
						good_order_placed = HandleFullProjectSelection();
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
					break;
				case emProjectStaffSelection.USEPREDEFINITION:
					//Using the predefined stage resource definition 
					good_order_placed = CheckPredefinedProjectSelection(out errs);
					if (good_order_placed)
					{
						good_order_placed = HandlePredefinedProjectSelection();
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
					break;
			}
			if (good_order_placed)
			{
				if (! true)
				{
					HandleAutomaticHandover();
				}

				_mainPanel.DisposeEntryPanel();
			}
		}

		private void btnOK_Click(object sender, System.EventArgs e)
		{
			//we might need to proceed to set the experts system 
			//as the panel order has been changed 

			if ((use_experts_round3) & (experts_system_enabled))
			{
				//Mind to hide all the panels for the different ways of defining staff
				if (this.pnl_ConfirmPredefine != null)
				{
					this.pnl_ConfirmPredefine.Visible = false;
				}
				if (this.MyPrjResStageEditor != null)
				{
					this.MyPrjResStageEditor.Visible = false;
				}
				if (this.pnl_ChooseResource != null)
				{
					this.pnl_ChooseResource.Visible = false;
				}
				//Now setup the expets system 
				SetupExpertsDefinition();
			}
			else
			{
				//No experts, so handle the ok (verify and issue order)
				HandledFinalOK();
			}
		}


		//private void btnOK_Click(object sender, System.EventArgs e)
		//{
		//  bool good_order_placed = false;
		//  ArrayList errs = new ArrayList();

		//  switch (StaffSelectionMode)
		//  { 
		//    case emProjectStaffSelection.USEPHASEDEFINITION:
		//      //We are defining Resources on a Phase Basis 
		//      good_order_placed = CheckNormalProjectSelection(out errs);
		//      if (good_order_placed)
		//      {
		//        good_order_placed = HandleNormalProjectSelection();
		//      }
		//      else
		//      {
		//        string disp = "";
		//        foreach (string err in errs)
		//        {
		//          disp += err + "\r\n";
		//        }
		//        MessageBox.Show(disp, "Error");
		//      }
		//      break;
		//    case emProjectStaffSelection.USESTAGEDEFINITION:
		//      //Using the full stage resource definition 
		//      good_order_placed = CheckFullProjectSelection(out errs);
		//      if (good_order_placed)
		//      {
		//        good_order_placed = HandleFullProjectSelection();
		//      }
		//      else
		//      {
		//        string disp = "";
		//        foreach (string err in errs)
		//        {
		//          disp += err + "\r\n";
		//        }
		//        MessageBox.Show(disp, "Error");
		//      }
		//      break;
		//    case emProjectStaffSelection.USEPREDEFINITION:
		//      //Using the predefined stage resource definition 
		//      good_order_placed = CheckPredefinedProjectSelection(out errs);
		//      if (good_order_placed)
		//      {
		//        good_order_placed = HandlePredefinedProjectSelection();
		//      }
		//      else
		//      {
		//        string disp = "";
		//        foreach (string err in errs)
		//        {
		//          disp += err + "\r\n";
		//        }
		//        MessageBox.Show(disp, "Error");
		//      }
		//      break;
		//  }
		//  if (good_order_placed)
		//  {
		//    if (! gameFile.GetGlobalOption("it_present", true))
		//    {
		//      HandleAutomaticHandover();
		//    }

		//    _mainPanel.DisposeEntryPanel();
		//  }
		//}

		private void btnCancel_Click(object sender, System.EventArgs e)
		{
			_mainPanel.DisposeEntryPanel();
		}

		void HandleAutomaticHandover ()
		{
			ArrayList types = new ArrayList ();
			types.Add("project");

			Hashtable nodes = MyNodeTree.GetNodesOfAttribTypes(types);
			foreach (Node project in nodes.Keys)
			{
				int slot = project.GetIntAttribute("slot", 0);
				if (slot == ChosenSlotNumber)
				{
					AutoHandover(project);
				}
			}
		}

		string GetPlatformNameByNumber (int platform)
		{
			return ((char) (((int) 'X') - 1 + platform)).ToString();
		}

		int GetPlatformNumberByName (string name)
		{
			for (int platform = 0; platform <= 5; platform++)
			{
				if (GetPlatformNameByNumber(platform).ToLower() == name.ToLower())
				{
					return platform;
				}
			}

			return -1;
		}

		void AutoHandover (Node project)
		{
			Node slot = null;

			Node magicRouter = MyNodeTree.GetNamedNode("MagicRouter");
			foreach (Node magicServer in magicRouter.GetChildrenOfType("Server"))
			{
				if (magicServer.GetAttribute("platform") == GetPlatformNameByNumber(ChosenPlatformNumber))
				{
					ArrayList existingMagicSlots = magicServer.getChildren();

					string name = CONVERT.Format("{0}{1}", magicServer.GetAttribute("platform"), 1000 + existingMagicSlots.Count + 1);

					ArrayList slotAttributes = new ArrayList ();
					slotAttributes.Add(new AttributeValuePair ("proczone", magicServer.GetAttribute("proczone")));
					slotAttributes.Add(new AttributeValuePair ("proccap", 0));
					slotAttributes.Add(new AttributeValuePair ("name", name));
					slotAttributes.Add(new AttributeValuePair ("location", name));
					slotAttributes.Add(new AttributeValuePair ("type", "Slot"));
					slot = new Node (magicServer, "Slot", name, slotAttributes);

					break;
				}
			}

			int installDay = project.GetIntAttribute("project_golive_day", 1) - 1;

			ArrayList installAttributes = new ArrayList ();
			installAttributes.Add(new AttributeValuePair ("cmd_type", "install_project"));
			installAttributes.Add(new AttributeValuePair ("nodename", project.GetAttribute("name")));
			installAttributes.Add(new AttributeValuePair ("install_location", slot.GetAttribute("name")));
			installAttributes.Add(new AttributeValuePair ("install_day", installDay));
			new Node (queueNode, "task", "", installAttributes);

			Node projectData = MyNodeTree.GetNamedNode(project.GetAttribute("name") + "_project_data");
			projectData.SetAttribute("install_day_auto_update", true);
		}
	}
}