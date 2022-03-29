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

namespace Polestar_PM.OpsGUI
{
	public class DataEntryPanel_Cancel_Actions : FlickerFreePanel
	{
		public enum emCancelAction
		{
			NONE_SELECTED = 0,
			CANCEL_PROJECT_EXECUTION = 1,
			CLEAR_PROJECT_INSTALL = 2,
			CANCEL_CHANGE = 3,
			CANCEL_UPGRADE = 4,
			CANCEL_ALL_DAY = 5
		}

		protected IDataEntryControlHolder _mainPanel;

		protected int round;
		protected NodeTree MyNodeTree;
		protected Node queueNode;
		protected Node workListNode; 
		protected Hashtable existingProjectsBySlot = new Hashtable();
		protected Hashtable existingOpsChanges = new Hashtable();
		protected Hashtable existingOpsUpgrades = new Hashtable();

		protected Font MyDefaultSkinFontNormal8 = null;
		protected Font MyDefaultSkinFontNormal10 = null;
		protected Font MyDefaultSkinFontNormal12 = null;
		protected Font MyDefaultSkinFontBold8 = null;
		protected Font MyDefaultSkinFontBold10 = null;
		protected Font MyDefaultSkinFontBold12 = null;
		
		private ImageTextButton newBtnOK = new ImageTextButton(0);
		private ImageTextButton newBtnCancel = new ImageTextButton(0);
		private ImageTextButton newBtnYes = new ImageTextButton(0);
		private ImageTextButton newBtnNo = new ImageTextButton(0);
		private emCancelAction selected_action = emCancelAction.NONE_SELECTED;

		//Action selection Elements 
		private ImageTextButton newBtnSelectProjects = new ImageTextButton(0);
		private ImageTextButton newBtnSelectPrjInstalls = new ImageTextButton(0);
		private ImageTextButton newBtnSelectChanges = new ImageTextButton(0);
		private ImageTextButton newBtnSelectUpgrades = new ImageTextButton(0);
		private ImageTextButton newBtnSelectDayList = new ImageTextButton(0);

		private System.Windows.Forms.Panel pnl_ChooseAction;
		private System.Windows.Forms.Panel pnl_ProjectCancelExecList;
		private System.Windows.Forms.Panel pnl_ProjectClearInstallList;
		private System.Windows.Forms.Panel pnl_RunningChangeList;
		private System.Windows.Forms.Panel pnl_RunningUpgradeList;
		private System.Windows.Forms.Panel pnl_DayOperationsList;
		private System.Windows.Forms.Panel pnl_YesNoChoice;

		private System.Windows.Forms.Label titleLabel;
		private System.Windows.Forms.Label helpLabel;

		private System.Windows.Forms.Label actionLabel;
		private ImageTextButton newBtnAction = new ImageTextButton(0);
		private System.Windows.Forms.Label whichLabel;
		private ImageTextButton newBtnWhich = new ImageTextButton(0);

		private int SlotDisplay =0;
		private Node projectNodeToCancel = null;
		private Node changeNodeToCancel = null;
		private Node upgradeNodeToCancel = null;
		private string ChosenProject_NodeName;
		private string ChosenChangeDisplayName;
		private string ChosenUpgradeDisplayName;
		private string ChosenUpgradeDay;

		protected int UseChangeCardRound = 1;
		string projectTerm;
		protected bool display_seven_projects = false;

		public DataEntryPanel_Cancel_Actions(IDataEntryControlHolder mainPanel, NodeTree tree, int current_round)
		{
			MyNodeTree = tree;
			round = current_round;
			_mainPanel = mainPanel;
			queueNode = tree.GetNamedNode("TaskManager");
			workListNode = tree.GetNamedNode("ops_worklist");

			projectTerm = SkinningDefs.TheInstance.GetData("project_term_round_" + LibCore.CONVERT.ToStr(round), "Project");

			Node projectsNode = tree.GetNamedNode("pm_projects_running");
			display_seven_projects = projectsNode.GetBooleanAttribute("display_seven_projects", false);

			BuildCurrentProjectLookupList();
			if (round == UseChangeCardRound)
			{
				BuildCurrentChangesLookupList();
			}
			else
			{
				BuildCurrentUpgradesLookupList();
			}

			string fontname =  SkinningDefs.TheInstance.GetData("fontname");
			MyDefaultSkinFontNormal8 = ConstantSizeFont.NewFont(fontname,8);
			MyDefaultSkinFontNormal10 = ConstantSizeFont.NewFont(fontname,10);
			MyDefaultSkinFontNormal12 = ConstantSizeFont.NewFont(fontname,12);
			MyDefaultSkinFontBold8 = ConstantSizeFont.NewFont(fontname, 8, FontStyle.Bold);
			MyDefaultSkinFontBold10 = ConstantSizeFont.NewFont(fontname, 10, FontStyle.Bold);
			MyDefaultSkinFontBold12 = ConstantSizeFont.NewFont(fontname,12,FontStyle.Bold);

			this.BackgroundImage = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "images\\panels\\PM_opsback.png");

			//this.ShowInTaskbar = false;
			this.Size = new Size (520,280);
			//this.ClientSize = new Size (520,280);
			//this.FormBorderStyle = FormBorderStyle.None;

			Build_PanelTitles();
			Build_SidebarControls();
			BuildPanel_OKCancel_Buttons();
			Build_AllPanels();

			this.pnl_ChooseAction.Visible = true;
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
			base.Dispose();
		}

		#region Data Lookup Methods

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
				string project_name = n.GetAttribute("name");
				string project_slot = n.GetAttribute("slot");
				//for each project
				existingProjectsBySlot.Add(project_slot, n);
			}
		}

		private void BuildCurrentChangesLookupList()
		{
			foreach (Node ops_job in workListNode.getChildren())
			{
				string status = ops_job.GetAttribute("status");
				int day = ops_job.GetIntAttribute("day",0);
				string action = ops_job.GetAttribute("action");

				if (action.ToLower() == "install_cc_app")
				{
					if ((status == "inprogress")|(status == "todo"))
					{
						existingOpsChanges.Add(day, ops_job);
					}
				}
			}
		}

		private void BuildCurrentUpgradesLookupList()
		{
			foreach (Node ops_job in workListNode.getChildren())
			{
				string status = ops_job.GetAttribute("status");
				int day = ops_job.GetIntAttribute("day",0);
				string action = ops_job.GetAttribute("action");

				if ((action.ToLower() == "upgrade_memory")|
					(action.ToLower() == "upgrade_disk")|
					(action.ToLower() == "upgrade_both"))
				{
					if ((status == "inprogress")|(status == "todo"))
					{
						existingOpsUpgrades.Add(day, ops_job);
					}
				}
			}
		}

		#endregion

		#region Title Methods

		private void Build_PanelTitles()
		{
			titleLabel = new System.Windows.Forms.Label();
			titleLabel.BackColor = System.Drawing.Color.Transparent;
			titleLabel.Font = MyDefaultSkinFontBold12;
			titleLabel.ForeColor = System.Drawing.Color.White;
			titleLabel.Location = new System.Drawing.Point(110 - 25, 10 - 2);
			titleLabel.Name = "titleLabel";
			titleLabel.Size = new System.Drawing.Size(380, 18);
			titleLabel.TabIndex = 11;
			titleLabel.Text = "Cancel Action";
			titleLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.Controls.Add(titleLabel);

			helpLabel = new System.Windows.Forms.Label();
			helpLabel.BackColor = System.Drawing.Color.Transparent;
			helpLabel.Font = MyDefaultSkinFontNormal10;
			helpLabel.ForeColor = System.Drawing.Color.White;
			helpLabel.Location = new System.Drawing.Point(110 - 25, 50 - 20 - 1);
			helpLabel.Name = "helpLabel";
			helpLabel.Size = new System.Drawing.Size(380, 18);
			helpLabel.TabIndex = 20;
			helpLabel.Text = "Select item for cancellation ";
			helpLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.Controls.Add(helpLabel);
		}

		#endregion Title Methods

		#region SideBar Methods

		private void Build_SidebarControls()
		{
			//Side bar Buttons for going back 
			actionLabel = new System.Windows.Forms.Label();
			actionLabel.BackColor = System.Drawing.Color.Transparent;
			actionLabel.Font = MyDefaultSkinFontNormal10;
			actionLabel.ForeColor = System.Drawing.Color.Black;
			actionLabel.Location = new System.Drawing.Point(3 + 5, 0 + 4);
			actionLabel.Name = "actionLabel";
			actionLabel.Size = new System.Drawing.Size(70, 20);
			actionLabel.TabIndex = 11;
			actionLabel.Text = "Action";
			actionLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			actionLabel.Visible = false;
			this.Controls.Add(actionLabel);

			newBtnAction.SetButton(AppInfo.TheInstance.Location+"\\images\\buttons\\selection_tab.png");
			newBtnAction.Location = new System.Drawing.Point(15, 24);
			newBtnAction.Name = "actionBtnNo";
			newBtnAction.Size = new System.Drawing.Size(65, 27);
			newBtnAction.TabIndex = 26;
			newBtnAction.ButtonFont = this.MyDefaultSkinFontBold8;
			newBtnAction.SetButtonText("No",
				System.Drawing.Color.Black, System.Drawing.Color.Black,
				System.Drawing.Color.Green, System.Drawing.Color.Gray);
			newBtnAction.Click += new EventHandler(newBtnAction_Click);
			newBtnAction.Text = "";
			newBtnAction.Visible = false;
			this.Controls.Add(newBtnAction);

			whichLabel = new System.Windows.Forms.Label();
			whichLabel.BackColor = System.Drawing.Color.Transparent;
			whichLabel.Font = MyDefaultSkinFontNormal10;
			whichLabel.ForeColor = System.Drawing.Color.Black;
			whichLabel.Location = new System.Drawing.Point(3 + 5, 50 + 4);
			whichLabel.Name = "titleLabel";
			whichLabel.Size = new System.Drawing.Size(70, 20);
			whichLabel.TabIndex = 11;
			whichLabel.Text = SkinningDefs.TheInstance.GetData("project_term_round_" + LibCore.CONVERT.ToStr(round), "Project");
			whichLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			whichLabel.Visible = false;
			this.Controls.Add(whichLabel);

			newBtnWhich.SetButton(AppInfo.TheInstance.Location+"\\images\\buttons\\selection_tab.png");
			newBtnWhich.Location = new System.Drawing.Point(15, 70 + 4);
			newBtnWhich.Name = "newBtnNo";
			newBtnWhich.Size = new System.Drawing.Size(65, 27);
			newBtnWhich.TabIndex = 26;
			newBtnWhich.ButtonFont = this.MyDefaultSkinFontBold8;
			newBtnWhich.SetButtonText("No",
				System.Drawing.Color.Black, System.Drawing.Color.Black,
				System.Drawing.Color.Green, System.Drawing.Color.Gray);
			newBtnWhich.Click += new System.EventHandler(newBtnWhich_Click);
			newBtnWhich.Text = "";
			newBtnWhich.Visible = false;
			this.Controls.Add(newBtnWhich);
		}

		private void newBtnAction_Click(object sender, System.EventArgs e)
		{
			this.pnl_ChooseAction.Visible = true;

			this.pnl_ProjectCancelExecList.Visible = false;
			this.pnl_ProjectClearInstallList.Visible = false;
			this.pnl_RunningChangeList.Visible = false;
			this.pnl_RunningUpgradeList.Visible = false;
			this.pnl_DayOperationsList.Visible = false;

			this.actionLabel.Visible = false;
			this.newBtnAction.Visible = false;
			this.whichLabel.Visible = false;
			this.newBtnWhich.Visible = false;

			titleLabel.Text = "Cancel Action";
			helpLabel.Text = "Select product for cancellation ";

			selected_action = emCancelAction.NONE_SELECTED;
		}

		private void newBtnWhich_Click(object sender, System.EventArgs e)
		{
			this.whichLabel.Visible = false;
			this.newBtnWhich.Visible = false;

			this.pnl_YesNoChoice.Visible = false;
			switch (this.selected_action)
			{ 
				case emCancelAction.CANCEL_PROJECT_EXECUTION:
					this.pnl_ProjectCancelExecList.Visible = true;
					helpLabel.Text = "Select a project to cancel";
					break;
				case emCancelAction.CLEAR_PROJECT_INSTALL:
					this.pnl_ProjectClearInstallList.Visible = true;
					helpLabel.Text = "Select a project install to cancel";
					break;
				case emCancelAction.CANCEL_CHANGE:
					this.pnl_RunningChangeList.Visible = true;
					helpLabel.Text = "Select a change to cancel";
					break;
				case emCancelAction.CANCEL_UPGRADE:
					this.pnl_RunningUpgradeList.Visible = true;
					helpLabel.Text = "Select an upgrade to cancel";
					break;
				case emCancelAction.CANCEL_ALL_DAY:
					this.pnl_DayOperationsList.Visible = true;
					break;
			}
		}

		#endregion SideBar Methods

		#region Main Panel Constructionn Methods

		public void Build_AllPanels()
		{
			pnl_ChooseAction = new System.Windows.Forms.Panel();
			pnl_ProjectCancelExecList = new System.Windows.Forms.Panel();
			pnl_ProjectClearInstallList = new System.Windows.Forms.Panel();
			pnl_RunningChangeList = new System.Windows.Forms.Panel();
			pnl_RunningUpgradeList = new System.Windows.Forms.Panel();
			pnl_DayOperationsList = new System.Windows.Forms.Panel();
			pnl_YesNoChoice = new System.Windows.Forms.Panel();

			pnl_ChooseAction.SuspendLayout();
			pnl_ProjectCancelExecList.SuspendLayout();
			pnl_ProjectClearInstallList.SuspendLayout();
			pnl_RunningChangeList.SuspendLayout();
			pnl_RunningUpgradeList.SuspendLayout();
			pnl_DayOperationsList.SuspendLayout();
			pnl_YesNoChoice.SuspendLayout();

			pnl_ChooseAction.Location = new System.Drawing.Point(78, 50);
			pnl_ChooseAction.Name = "pnl_ChooseSlot";
			pnl_ChooseAction.Size = new System.Drawing.Size(390, 160);
			pnl_ChooseAction.BackColor = Color.FromArgb(176, 196, 222); //TODO SKIN
			pnl_ChooseAction.BackColor = Color.Transparent;
			pnl_ChooseAction.TabIndex = 13;
			pnl_ChooseAction.Visible = true;
			BuildActionSelectionControls();

			//Build the controls for the Cancelation of Projects
			pnl_ProjectCancelExecList.Location = new System.Drawing.Point(78, 50);
			pnl_ProjectCancelExecList.Name = "pnl_ProjectCancelExecList";
			pnl_ProjectCancelExecList.Size = new System.Drawing.Size(390, 160);
			pnl_ProjectCancelExecList.BackColor = Color.FromArgb(176, 196, 222); //TODO SKIN
			pnl_ProjectCancelExecList.BackColor = Color.Transparent;
			pnl_ProjectCancelExecList.TabIndex = 13;
			pnl_ProjectCancelExecList.Visible = false;
			BuildCurrentProjectsControls();

			//Build the controls for the Cancelation of Project Install Days
			pnl_ProjectClearInstallList.Location = new System.Drawing.Point(78, 50);
			pnl_ProjectClearInstallList.Name = "pnl_ProjectClearInstallList";
			pnl_ProjectClearInstallList.Size = new System.Drawing.Size(390, 160);
			pnl_ProjectClearInstallList.BackColor = Color.FromArgb(176, 196, 222); //TODO SKIN
			pnl_ProjectClearInstallList.BackColor = Color.Transparent;
			pnl_ProjectClearInstallList.TabIndex = 13;
			pnl_ProjectClearInstallList.Visible = false;
			BuildCurrentProjectInstallControls();
			
			//Build the controls for the Cancelation of Change
			pnl_RunningChangeList.Location = new System.Drawing.Point(78, 50);
			pnl_RunningChangeList.Name = "pnl_Choose_Change_Slot";
			pnl_RunningChangeList.Size = new System.Drawing.Size(390, 160);
			pnl_RunningChangeList.BackColor = Color.FromArgb(176, 196, 222); //TODO SKIN
			pnl_RunningChangeList.BackColor = Color.Transparent;
			pnl_RunningChangeList.TabIndex = 13;
			pnl_RunningChangeList.Visible = false;
			BuildCurrentChangeControls();

			//Build the controls for the Cancelation of Change
			pnl_RunningUpgradeList.Location = new System.Drawing.Point(78, 50);
			pnl_RunningUpgradeList.Name = "pnl_Choose_Upgrade_Slot";
			pnl_RunningUpgradeList.Size = new System.Drawing.Size(390, 160);
			pnl_RunningUpgradeList.BackColor = Color.FromArgb(176, 196, 222); //TODO SKIN
			pnl_RunningUpgradeList.BackColor = Color.Transparent;
			pnl_RunningUpgradeList.TabIndex = 13;
			pnl_RunningUpgradeList.Visible = false;
			BuildCurrentUpgradeControls();

			pnl_DayOperationsList.Location = new System.Drawing.Point(78, 50);
			pnl_DayOperationsList.Name = "pnl_DayList";
			pnl_DayOperationsList.Size = new System.Drawing.Size(390, 160);
			pnl_DayOperationsList.BackColor = Color.FromArgb(176, 196, 222); //TODO SKIN
			pnl_DayOperationsList.BackColor = Color.Transparent;
			pnl_DayOperationsList.TabIndex = 14;
			pnl_DayOperationsList.Visible = false;
			this.BuildDayControls();

			pnl_YesNoChoice.Location = new System.Drawing.Point(78, 50);
			pnl_YesNoChoice.Name = "pnl_Change_YesNoChoice";
			pnl_YesNoChoice.Size = new System.Drawing.Size(390, 160);
			pnl_YesNoChoice.BackColor = Color.FromArgb(176, 196, 222); //TODO SKIN
			pnl_YesNoChoice.BackColor = Color.Transparent;
			pnl_YesNoChoice.TabIndex = 14;
			pnl_YesNoChoice.Visible = false;
			BuildYesNoControls();

			this.Controls.Add(pnl_ChooseAction);
			this.Controls.Add(pnl_ProjectCancelExecList);
			this.Controls.Add(pnl_ProjectClearInstallList);
			this.Controls.Add(pnl_RunningChangeList);
			this.Controls.Add(pnl_RunningUpgradeList);
			this.Controls.Add(pnl_DayOperationsList);
			this.Controls.Add(pnl_YesNoChoice);
			this.Name = "Cancel Action Control";
			this.Size = new System.Drawing.Size(520, 280);

			pnl_ChooseAction.ResumeLayout(false);
			pnl_ProjectCancelExecList.ResumeLayout(false);
			pnl_ProjectClearInstallList.ResumeLayout(false);
			pnl_RunningChangeList.ResumeLayout(false);
			pnl_RunningUpgradeList.ResumeLayout(false);
			pnl_DayOperationsList.ResumeLayout(false);
			pnl_YesNoChoice.ResumeLayout(false);
			this.ResumeLayout(false);
		}

		#endregion Main Panel Constructionn Methods

		#region Action Selection

		public void BuildActionSelectionControls()
		{

			//newBtnSelectProjects.SetButton(AppInfo.TheInstance.Location+"\\images\\buttons\\button_70x25.png");
			newBtnSelectProjects.SetVariants("images\\buttons\\button_70x25.png");
			newBtnSelectProjects.Location = new System.Drawing.Point(10, 10);
			newBtnSelectProjects.Name = "newBtnCancel";
			newBtnSelectProjects.Size = new System.Drawing.Size(70, 25);
			newBtnSelectProjects.TabIndex = 22;
			newBtnSelectProjects.ButtonFont = MyDefaultSkinFontBold10;
			newBtnSelectProjects.SetButtonText(projectTerm,
				System.Drawing.Color.Black, System.Drawing.Color.Black,
				System.Drawing.Color.Green, System.Drawing.Color.Gray);
			newBtnSelectProjects.Click += new System.EventHandler(this.newBtnSelectProjects_Click);
			this.pnl_ChooseAction.Controls.Add(newBtnSelectProjects);

			//newBtnSelectPrjInstalls.SetButton(AppInfo.TheInstance.Location+"\\images\\buttons\\button_70x25.png");
			newBtnSelectPrjInstalls.SetVariants("images\\buttons\\button_70x25.png");
			newBtnSelectPrjInstalls.Location = new System.Drawing.Point(10 + 100+40, 10);
			newBtnSelectPrjInstalls.Name = "newBtnCancel";
			newBtnSelectPrjInstalls.Size = new System.Drawing.Size(70, 25);
			newBtnSelectPrjInstalls.TabIndex = 22;
			newBtnSelectPrjInstalls.ButtonFont = MyDefaultSkinFontBold10;
			newBtnSelectPrjInstalls.SetButtonText("Install",
				System.Drawing.Color.Black, System.Drawing.Color.Black,
				System.Drawing.Color.Green, System.Drawing.Color.Gray);
			newBtnSelectPrjInstalls.Click += new System.EventHandler(this.newBtnSelectPrjInstalls_Click);
			this.pnl_ChooseAction.Controls.Add(newBtnSelectPrjInstalls);

			//newBtnSelectChanges.SetButton(AppInfo.TheInstance.Location+"\\images\\buttons\\button_70x25.png");
			newBtnSelectChanges.SetVariants("images\\buttons\\button_70x25.png");
			newBtnSelectChanges.Location = new System.Drawing.Point(10 + 200 + 80, 10);
			newBtnSelectChanges.Name = "newBtnCancel";
			newBtnSelectChanges.Size = new System.Drawing.Size(70,25);
			newBtnSelectChanges.TabIndex = 22;
			newBtnSelectChanges.ButtonFont = MyDefaultSkinFontBold10;
			newBtnSelectChanges.SetButtonText("Change",
				System.Drawing.Color.Black, System.Drawing.Color.Black,
				System.Drawing.Color.Green, System.Drawing.Color.Gray);
			newBtnSelectChanges.Click += new System.EventHandler(this.newBtnSelectChanges_Click);
			newBtnSelectChanges.Visible = false;
			this.pnl_ChooseAction.Controls.Add(newBtnSelectChanges);

			//newBtnSelectUpgrades.SetButton(AppInfo.TheInstance.Location+"\\images\\buttons\\button_70x25.png");
			newBtnSelectUpgrades.SetVariants("images\\buttons\\button_70x25.png");
			newBtnSelectUpgrades.Location = new System.Drawing.Point(10 + 200 + 80, 10);
			newBtnSelectUpgrades.Name = "newBtnCancel";
			newBtnSelectUpgrades.Size = new System.Drawing.Size(70, 25);
			newBtnSelectUpgrades.TabIndex = 22;
			newBtnSelectUpgrades.ButtonFont = MyDefaultSkinFontBold10;
			newBtnSelectUpgrades.SetButtonText("Upgrade",
				System.Drawing.Color.Black, System.Drawing.Color.Black,
				System.Drawing.Color.Green, System.Drawing.Color.Gray);
			newBtnSelectUpgrades.Click += new System.EventHandler(this.newBtnSelectUpgrades_Click);
			newBtnSelectUpgrades.Visible = false;
			this.pnl_ChooseAction.Controls.Add(newBtnSelectUpgrades);

			if (round == UseChangeCardRound)
			{
				newBtnSelectChanges.Visible = true;
			}
			else
			{
				newBtnSelectUpgrades.Visible = true;
			}
		}

		private void newBtnSelectProjects_Click(object sender, System.EventArgs e)		
		{
			string project = SkinningDefs.TheInstance.GetData("project_term_round_" + LibCore.CONVERT.ToStr(round), "project");
			string lowerProject = project.ToLower();
			actionLabel.Visible = true;
			newBtnAction.SetButtonText(project,
				System.Drawing.Color.Black, System.Drawing.Color.Black,
				System.Drawing.Color.Green, System.Drawing.Color.Gray);
			newBtnAction.Visible = true;

			titleLabel.Text = "Cancel " + project;
			helpLabel.Text = "Select a " + lowerProject + " to cancel";

			this.pnl_ChooseAction.Visible = false;
			this.pnl_ProjectCancelExecList.Visible = true;

			selected_action = emCancelAction.CANCEL_PROJECT_EXECUTION;
		}

		private void newBtnSelectPrjInstalls_Click(object sender, System.EventArgs e)
		{
			actionLabel.Visible = true;
			newBtnAction.SetButtonText("Install",
				System.Drawing.Color.Black, System.Drawing.Color.Black,
				System.Drawing.Color.Green, System.Drawing.Color.Gray);
			newBtnAction.Visible = true;

			titleLabel.Text = "Cancel Install";
			helpLabel.Text = "Select " + SkinningDefs.TheInstance.GetData("project_term_round_" + LibCore.CONVERT.ToStr(round), "project").ToLower() + " install day to cancel";
			//helpLabel.Text = "Select project install day to cancel";

			this.pnl_ChooseAction.Visible = false;
			this.pnl_ProjectClearInstallList.Visible = true;
			
			selected_action = emCancelAction.CLEAR_PROJECT_INSTALL;
		}

		private void newBtnSelectChanges_Click(object sender, System.EventArgs e)
		{
			actionLabel.Visible = true;
			newBtnAction.SetButtonText("Change",
				System.Drawing.Color.Black, System.Drawing.Color.Black,
				System.Drawing.Color.Green, System.Drawing.Color.Gray);
			newBtnAction.Visible = true;

			titleLabel.Text = "Cancel Change";
			helpLabel.Text = "Select change number to cancel";

			this.pnl_ChooseAction.Visible = false;
			this.pnl_RunningChangeList.Visible = true;

			selected_action = emCancelAction.CANCEL_CHANGE;
		}

		private void newBtnSelectUpgrades_Click(object sender, System.EventArgs e)
		{
			actionLabel.Visible = true;
			newBtnAction.SetButtonText("Upgrade",
				System.Drawing.Color.Black, System.Drawing.Color.Black,
				System.Drawing.Color.Green, System.Drawing.Color.Gray);
			newBtnAction.Visible = true;

			titleLabel.Text = "Cancel Upgrade";
			helpLabel.Text = "Select upgrade to cancel";

			this.pnl_ChooseAction.Visible = false;
			this.pnl_RunningUpgradeList.Visible = true;

			selected_action = emCancelAction.CANCEL_UPGRADE;
		}

		private void newBtnSelectDayList_Click(object sender, System.EventArgs e)
		{
			actionLabel.Visible = true;
			newBtnAction.SetButtonText("Clear Day",
				System.Drawing.Color.Black, System.Drawing.Color.Black,
				System.Drawing.Color.Green, System.Drawing.Color.Gray);
			newBtnAction.Visible = true;

			this.pnl_ChooseAction.Visible = false;
			this.pnl_DayOperationsList.Visible = true;

			selected_action = emCancelAction.CANCEL_ALL_DAY;
		}

		#endregion Action Selection

		#region Project Methods

		public void BuildCurrentProjectsControls()
		{
			int offset_x = 10;
			int offset_y = 10;
			int button_width = 70;
			int button_height = 25;
			int button_sep = 5;

			//Clean out any old controls 
			ArrayList killList = new ArrayList();
			foreach (Control ctrl in pnl_ProjectCancelExecList.Controls)
			{
				killList.Add(ctrl);
			}
			foreach (Control ctrl in killList)
			{
				pnl_ProjectCancelExecList.Controls.Remove(ctrl);
				ctrl.Dispose();
			}
			killList.Clear();

			//Add controls for any existing projects 
			int max_projects = 6;
			if (display_seven_projects)
			{
				max_projects = 7;
			}
			int stepper_x = 0;
			int stepper_y = 0;
			for (int step = 0; step < max_projects; step++)
			{
				string display_text = (step + 1).ToString();
				Node project_node = null;
				//determine the 
				if (existingProjectsBySlot.ContainsKey(step.ToString()))
				{
					project_node = (Node)existingProjectsBySlot[step.ToString()];

					if (project_node.GetAttribute("state_name").ToUpper() == "PROJECT_STATE_COMPLETED")
					{
						continue;
					}

					display_text = projectTerm + " "+project_node.GetAttribute("project_id");

					System.Windows.Forms.Label tmpLabel = new System.Windows.Forms.Label();
					tmpLabel.BackColor = System.Drawing.Color.Transparent;
					tmpLabel.Font = MyDefaultSkinFontNormal10;
					tmpLabel.ForeColor = System.Drawing.Color.Black;
					tmpLabel.Location = new System.Drawing.Point((offset_x+stepper_x*180), offset_y + ((button_height + button_sep) * stepper_y));
					tmpLabel.Name = "tmpLabel";
					tmpLabel.Size = new System.Drawing.Size(100, 20);
					tmpLabel.TabIndex = 11;
					tmpLabel.Text = display_text;
					tmpLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
					pnl_ProjectCancelExecList.Controls.Add(tmpLabel);
					
					ImageTextButton tmpITB  = new ImageTextButton(0);
					//tmpITB.SetButton(AppInfo.TheInstance.Location+"\\images\\buttons\\button_70x25.png");
					tmpITB.SetVariants("images\\buttons\\button_70x25.png");
					tmpITB.Location = new System.Drawing.Point((offset_x + 100 + stepper_x * 180), offset_y + ((button_height + button_sep) * stepper_y));
					tmpITB.Name = "Button1";
					tmpITB.Size = new System.Drawing.Size(button_width, button_height);
					tmpITB.TabIndex = 8;
					tmpITB.ButtonFont = MyDefaultSkinFontBold8;
					tmpITB.Tag = project_node;
					tmpITB.SetButtonText("Cancel",
						System.Drawing.Color.Black, System.Drawing.Color.Black,
						System.Drawing.Color.Green, System.Drawing.Color.Gray);
					tmpITB.Click += new System.EventHandler(this.ProjectCancelButton_Click);
					tmpITB.Enabled = true;
					pnl_ProjectCancelExecList.Controls.Add(tmpITB);
					stepper_y++;
					if (stepper_y > 4)
					{
						stepper_x = 1;
						stepper_y = 0;
					}
				}
			}
		}

		private void ProjectCancelButton_Click(object sender, System.EventArgs e)
		{
			//Extract out the project node from the control that was used
			if (sender is ImageTextButton)
			{
				ImageTextButton ib = (ImageTextButton)sender;
				if (ib != null)
				{
					projectNodeToCancel = (Node)(ib.Tag);
					SlotDisplay = projectNodeToCancel.GetIntAttribute("slot", 0);
					ChosenProject_NodeName = projectNodeToCancel.GetAttribute("name");
					string display_text = projectNodeToCancel.GetAttribute("project_id");

					helpLabel.Text = "Are you sure you want to cancel " + projectTerm + " " + display_text + " ?";
					this.pnl_YesNoChoice.Visible = true;
					this.pnl_ProjectCancelExecList.Visible = false;

					whichLabel.Text = projectTerm;
					whichLabel.Visible = true;
					newBtnWhich.SetButtonText(display_text,
						System.Drawing.Color.Black, System.Drawing.Color.Black,
						System.Drawing.Color.Green, System.Drawing.Color.Gray);
					newBtnWhich.Visible = true;
				}
			}
		}

		public bool CheckOrder_CancelProjectExec(out ArrayList errs)
		{
			bool good_order = true;
			errs = new ArrayList();
			return good_order; 
		}

		public void HandleOrder_CancelProjectExec()
		{
			//Node tmpNode = null;
			//tmpNode = projectNodeToCancel;
			//tmpNode = changeNodeToCancel;
			//tmpNode = upgradeNodeToCancel;

			string desc = "";
			ProjectReader pr = new ProjectReader(projectNodeToCancel);
			desc = pr.getProjectTextDescription();
			pr.Dispose();

			ArrayList attrs = new ArrayList();
			attrs.Add(new AttributeValuePair("cmd_type", "cancel_project"));
			attrs.Add(new AttributeValuePair("nodedesc", desc));
			attrs.Add(new AttributeValuePair("projectnodename", this.ChosenProject_NodeName));
			new Node(queueNode, "task", "", attrs);
		}

		#endregion Project Methods

		#region Project Install Days Methods 

		public void BuildCurrentProjectInstallControls()
		{
			int offset_x = 10;
			int offset_y = 10;
			int button_width = 70;
			int button_height = 20;
			int button_sep = 5;

			//Clean out any old controls 
			ArrayList killList = new ArrayList();
			foreach (Control ctrl in pnl_ProjectClearInstallList.Controls)
			{
				killList.Add(ctrl);
			}
			foreach (Control ctrl in killList)
			{
				pnl_ProjectClearInstallList.Controls.Remove(ctrl);
				ctrl.Dispose();
			}
			killList.Clear();

			//Add controls for any existing projects 
			int max_projects = 6;
			if (display_seven_projects)
			{
				max_projects = 7;
			}

			if (this.round != 3)
			{
				int stepper_y = 0;
				for (int step = 0; step < max_projects; step++)
				{
					string display_text = (step + 1).ToString();
					Node project_node = null;
					//determine the 
					if (existingProjectsBySlot.ContainsKey(step.ToString()))
					{
						project_node = (Node)existingProjectsBySlot[step.ToString()];

						if (project_node.GetAttribute("state_name").ToUpper() == "PROJECT_STATE_COMPLETED")
						{
							continue;
						}

						ProjectReader pr = new ProjectReader(project_node);

						string install_location = pr.getInstallLocation();
						int install_day = pr.getInstallDay();

						display_text = "Install " + CONVERT.ToStr(pr.getProjectID());
						if (this.round < 3)
						{
							display_text += " to " + pr.getInstallLocation();
						}
						display_text += " on Day " + CONVERT.ToStr(install_day);
						pr.Dispose();

						if (install_day > 0)
						{
							System.Windows.Forms.Label tmpLabel = new System.Windows.Forms.Label();
							tmpLabel.BackColor = System.Drawing.Color.Transparent;
							tmpLabel.Font = MyDefaultSkinFontNormal10;
							tmpLabel.ForeColor = System.Drawing.Color.Black;
							tmpLabel.Location = new System.Drawing.Point(offset_x, offset_y + ((button_height + button_sep) * stepper_y));
							tmpLabel.Name = "tmpLabel";
							tmpLabel.Size = new System.Drawing.Size(290, 20);
							tmpLabel.TabIndex = 11;
							tmpLabel.Text = display_text;
							tmpLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
							pnl_ProjectClearInstallList.Controls.Add(tmpLabel);

							ImageTextButton tmpITB = new ImageTextButton(0);
							//tmpITB.SetButton(AppInfo.TheInstance.Location + "\\images\\buttons\\button_70x25.png");
							tmpITB.SetVariants("images\\buttons\\button_70x25.png");
							tmpITB.Location = new System.Drawing.Point(offset_x + 290, offset_y + ((button_height + button_sep) * stepper_y));
							tmpITB.Name = "Button1";
							tmpITB.Size = new System.Drawing.Size(button_width, button_height);
							tmpITB.TabIndex = 8;
							tmpITB.ButtonFont = MyDefaultSkinFontBold8;
							tmpITB.Tag = project_node;
							tmpITB.SetButtonText("Cancel",
								System.Drawing.Color.Black, System.Drawing.Color.Black,
								System.Drawing.Color.Green, System.Drawing.Color.Gray);
							tmpITB.Click += new System.EventHandler(this.ProjectClearInstallDayButton_Click);
							tmpITB.Enabled = true;
							pnl_ProjectClearInstallList.Controls.Add(tmpITB);
							stepper_y++;
							if (stepper_y > 5)
							{
								stepper_y = 0;
							}
						}
					}
				}
			}
		}

		private void ProjectClearInstallDayButton_Click(object sender, System.EventArgs e)
		{
			//Extract out the project node from the control that was used
			if (sender is ImageTextButton)
			{
				ImageTextButton ib = (ImageTextButton)sender;
				if (ib != null)
				{
					projectNodeToCancel = (Node)(ib.Tag);
					SlotDisplay = projectNodeToCancel.GetIntAttribute("slot", 0);
					ChosenProject_NodeName = projectNodeToCancel.GetAttribute("name");
					string display_text = projectNodeToCancel.GetAttribute("project_id");

					helpLabel.Text = "Are you sure you want to cancel the install day?";
					this.pnl_YesNoChoice.Visible = true;
					this.pnl_ProjectClearInstallList.Visible = false;

					whichLabel.Text = "Install";
					whichLabel.Visible = true;
					newBtnWhich.SetButtonText(display_text,
						System.Drawing.Color.Black, System.Drawing.Color.Black,
						System.Drawing.Color.Green, System.Drawing.Color.Gray);
					newBtnWhich.Visible = true;
				}
			}
		}

		public bool CheckOrder_ClearProjectInstall(out ArrayList errs)
		{
			bool good_order = true;
			errs = new ArrayList();
			return good_order; 
		}
		public void HandleOrder_ClearProjectInstall()
		{
			//Node tmpNode = null;
			//tmpNode = projectNodeToCancel;
			//tmpNode = changeNodeToCancel;
			//tmpNode = upgradeNodeToCancel;

			string desc = "";
			ProjectReader pr = new ProjectReader(projectNodeToCancel);
			desc = pr.getProjectTextDescription();
			pr.Dispose();

			ArrayList attrs = new ArrayList();
			attrs.Add(new AttributeValuePair("cmd_type", "clear_project_install"));
			attrs.Add(new AttributeValuePair("nodedesc", desc));
			attrs.Add(new AttributeValuePair("projectnodename", this.ChosenProject_NodeName));
			new Node(queueNode, "task", "", attrs);
		}

		#endregion Project Install Methods

		#region Change Methods

		public void BuildCurrentChangeControls()
		{
			int offset_x = 10;
			int offset_y = 10;
			int button_width = 70;
			int button_height = 20;
			int button_sep = 5;

			//Clean out any old controls 
			ArrayList killList = new ArrayList();
			foreach (Control ctrl in pnl_RunningChangeList.Controls)
			{
				killList.Add(ctrl);
			}
			foreach (Control ctrl in killList)
			{
				pnl_RunningChangeList.Controls.Remove(ctrl);
				ctrl.Dispose();
			}
			killList.Clear();

			//Add controls for any existing projects 
			int stepper = 0;
			for (int step = 0; step < 26; step++)
			{
				if (existingOpsChanges.ContainsKey(step))
				{
					Node ops_Item = (Node)existingOpsChanges[step];
					if (ops_Item != null)
					{
						string job_display = ops_Item.GetAttribute("display");
						string location = ops_Item.GetAttribute("location");
						int planned_day = ops_Item.GetIntAttribute("day",0);
						string display_text = job_display + " to " + location +" on day " + planned_day;

						System.Windows.Forms.Label tmpLabel = new System.Windows.Forms.Label();
						tmpLabel.BackColor = System.Drawing.Color.Transparent;
						tmpLabel.Font = MyDefaultSkinFontNormal10;
						tmpLabel.ForeColor = System.Drawing.Color.Black;
						tmpLabel.Location = new System.Drawing.Point(offset_x, offset_y + ((button_height + button_sep) * stepper));
						tmpLabel.Name = "tmpLabel";
						tmpLabel.Size = new System.Drawing.Size(310, 20);
						tmpLabel.TabIndex = 11;
						tmpLabel.Text = display_text;
						tmpLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
						pnl_RunningChangeList.Controls.Add(tmpLabel);							
						
						ImageTextButton tmpITB = new ImageTextButton(0);
						//tmpITB.SetButton(AppInfo.TheInstance.Location+"\\images\\buttons\\button_70x25.png");
						tmpITB.SetVariants("images\\buttons\\button_70x25.png");
						tmpITB.Location = new System.Drawing.Point(offset_x+310, offset_y + ((button_height + button_sep) * stepper));
						tmpITB.Name = "Button1";
						tmpITB.Size = new System.Drawing.Size(button_width, button_height);
						tmpITB.TabIndex = 8;
						tmpITB.ButtonFont = MyDefaultSkinFontBold8;
						tmpITB.Tag = ops_Item;
						tmpITB.SetButtonText("Cancel",
							System.Drawing.Color.Black, System.Drawing.Color.Black,
							System.Drawing.Color.Green, System.Drawing.Color.Gray);
						tmpITB.Click += new System.EventHandler(this.ChangeCancelButton_Click);
						tmpITB.Enabled = true;
						pnl_RunningChangeList.Controls.Add(tmpITB);
						stepper++;
					}
				}
			}
		}

		private void ChangeCancelButton_Click(object sender, System.EventArgs e)
		{
			//Extract out the project node from the control that was used
			if (sender is ImageTextButton)
			{
				ImageTextButton ib = (ImageTextButton)sender;
				if (ib != null)
				{
					changeNodeToCancel = (Node)(ib.Tag);
					string job_display = changeNodeToCancel.GetAttribute("display");
					this.ChosenChangeDisplayName = job_display;
					string display_text = job_display;

					helpLabel.Text = "Are you sure you want to cancel " + display_text + " ?";
					this.pnl_YesNoChoice.Visible = true;
					this.pnl_RunningChangeList.Visible = false;

					whichLabel.Text = "Change";
					display_text = display_text.Replace("Change", "");
					whichLabel.Visible = true;
					newBtnWhich.SetButtonText(display_text,
						System.Drawing.Color.Black, System.Drawing.Color.Black,
						System.Drawing.Color.Green, System.Drawing.Color.Gray);
					newBtnWhich.Visible = true;
				}
			}
		}

		public bool CheckOrder_CancelChange(out ArrayList errs)
		{
			bool good_order = true;
			errs = new ArrayList();
			return good_order;
		}
		public void HandleOrder_CancelChange()
		{
			ArrayList attrs = new ArrayList();
			attrs.Add(new AttributeValuePair("cmd_type", "remove_cc"));
			attrs.Add(new AttributeValuePair("changedisplayname", this.ChosenChangeDisplayName));
			new Node(queueNode, "task", "", attrs);
		}

		#endregion Change Methods

		#region Upgrade

		public void BuildCurrentUpgradeControls()
		{
			int offset_x = 10;
			int offset_y = 10;
			int button_width = 70;
			int button_height = 20;
			int button_sep = 5;

			//Clean out any old controls 
			ArrayList killList = new ArrayList();
			foreach (Control ctrl in this.pnl_RunningUpgradeList.Controls)
			{
				killList.Add(ctrl);
			}
			foreach (Control ctrl in killList)
			{
				pnl_RunningUpgradeList.Controls.Remove(ctrl);
				ctrl.Dispose();
			}
			killList.Clear();

			//Add controls for any existing projects 
			int stepper = 0;
			for (int step = 0; step < 26; step++)
			{
				if (this.existingOpsUpgrades.ContainsKey(step))
				{
					Node ops_Item = (Node)existingOpsUpgrades[step];
					if (ops_Item != null)
					{
						string job_display = ops_Item.GetAttribute("display");
						string job_action = ops_Item.GetAttribute("action");
						int planned_day = ops_Item.GetIntAttribute("day", 0);
						string display_text = "";

						bool if_disk_upgrade = job_action.ToLower().IndexOf("disk") > -1;
						if (if_disk_upgrade)
						{
							display_text += "Upgrade Disk on ";
						}
						else
						{
							display_text += "Upgrade Memory on ";
						}
						display_text += job_display + " on day " + planned_day;

						System.Windows.Forms.Label tmpLabel = new System.Windows.Forms.Label();
						tmpLabel.BackColor = System.Drawing.Color.Transparent;
						tmpLabel.Font = MyDefaultSkinFontNormal10;
						tmpLabel.ForeColor = System.Drawing.Color.Black;
						tmpLabel.Location = new System.Drawing.Point(offset_x, offset_y + ((button_height + button_sep) * stepper));
						tmpLabel.Name = "tmpLabel";
						tmpLabel.Size = new System.Drawing.Size(310, 20);
						tmpLabel.TabIndex = 11;
						tmpLabel.Text = display_text;
						tmpLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
						pnl_RunningUpgradeList.Controls.Add(tmpLabel);		

						ImageTextButton tmpITB = new ImageTextButton(0);
						//tmpITB.SetButton(AppInfo.TheInstance.Location+"\\images\\buttons\\button_70x25.png");
						tmpITB.SetVariants("images\\buttons\\button_70x25.png");
						tmpITB.Location = new System.Drawing.Point(offset_x+310, offset_y + ((button_height + button_sep) * stepper));
						tmpITB.Name = "Button1";
						tmpITB.Size = new System.Drawing.Size(button_width, button_height);
						tmpITB.TabIndex = 8;
						tmpITB.ButtonFont = MyDefaultSkinFontBold8;
						tmpITB.Tag = ops_Item;
						tmpITB.SetButtonText("Cancel",
							System.Drawing.Color.Black, System.Drawing.Color.Black,
							System.Drawing.Color.Green, System.Drawing.Color.Gray);
						tmpITB.Click += new System.EventHandler(this.UpgradeCancelButton_Click);
						tmpITB.Enabled = true;
						pnl_RunningUpgradeList.Controls.Add(tmpITB);
						stepper++;
					}
				}
			}
		}

		private void UpgradeCancelButton_Click(object sender, System.EventArgs e)
		{
			//Extract out the project node from the control that was used
			if (sender is ImageTextButton)
			{
				ImageTextButton ib = (ImageTextButton)sender;
				if (ib != null)
				{
					upgradeNodeToCancel = (Node)(ib.Tag);
					this.ChosenUpgradeDay = upgradeNodeToCancel.GetAttribute("day");
					this.ChosenUpgradeDisplayName = upgradeNodeToCancel.GetAttribute("display");

					string job_display = upgradeNodeToCancel.GetAttribute("display");
					string job_action = upgradeNodeToCancel.GetAttribute("action");
					int planned_day = upgradeNodeToCancel.GetIntAttribute("day", 0);
					string display_text = "Cancel " + job_display + " " + job_action + " on day " + planned_day;

					helpLabel.Text = "Are you sure you want to " + display_text + " ?";
					this.pnl_YesNoChoice.Visible = true;
					this.pnl_RunningUpgradeList.Visible = false;

					string display_op = "";
					string help_message = "Are you sure you want to ";
					bool if_disk_upgrade = job_action.ToLower().IndexOf("disk") > -1;
					if (if_disk_upgrade)
					{
						display_op = "Disk";
						help_message += "cancel "+ job_display + " disk upgrade?";
					}
					else
					{
						display_op = "Memory";
						help_message += "cancel " + job_display + " memory upgrade?";
					}

					//just simplify the message 
					help_message = "Are you sure you want to ";
					help_message += "cancel " + job_display + " upgrade?";

					helpLabel.Text = help_message;
					this.pnl_YesNoChoice.Visible = true;
					this.pnl_RunningUpgradeList.Visible = false;

					whichLabel.Text = "Upgrade";
					whichLabel.Visible = true;
					newBtnWhich.SetButtonText(display_op,
						System.Drawing.Color.Black, System.Drawing.Color.Black,
						System.Drawing.Color.Green, System.Drawing.Color.Gray);
					newBtnWhich.Visible = true;
				}
			}
		}

		public bool CheckOrder_CancelUpgrade(out ArrayList errs)
		{
			bool good_order = true;
			errs = new ArrayList();
			return good_order;
		}
		public void HandleOrder_CancelUpgrade()
		{
			ArrayList attrs = new ArrayList();
			attrs.Add(new AttributeValuePair("cmd_type", "remove_opsupgrade"));
			attrs.Add(new AttributeValuePair("upgrade_displayname", this.ChosenUpgradeDisplayName));
			attrs.Add(new AttributeValuePair("upgrade_day", this.ChosenUpgradeDay));
			new Node(queueNode, "task", "", attrs);
		}

		#endregion Upgrade Methods

		#region Day Methods

		public void BuildDayControls()
		{ 
		}

		public bool CheckOrder_CancelAllDay(out ArrayList errs)
		{
			bool good_order = true;
			errs = new ArrayList();
			return good_order; 
		}
		public void HandleOrder_CancelAllDay()
		{
		}

		#endregion Day methods

		#region YesNO and OKCancel Methods

		public void BuildYesNoControls()
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
				System.Drawing.Color.Black, System.Drawing.Color.Black,
				System.Drawing.Color.Green, System.Drawing.Color.Gray);
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
				System.Drawing.Color.Black, System.Drawing.Color.Black,
				System.Drawing.Color.Green, System.Drawing.Color.Gray);
			newBtnNo.Click += new System.EventHandler(newBtnNo_Click);
			pnl_YesNoChoice.Controls.Add(newBtnNo);
		}

		public void BuildPanel_OKCancel_Buttons()
		{
			//newBtnCancel.SetButton(AppInfo.TheInstance.Location+"\\images\\buttons\\button_70x25.png");
			newBtnCancel.SetVariants("images\\buttons\\button_70x25.png");
			newBtnCancel.Location = new System.Drawing.Point(400, 220);
			newBtnCancel.Name = "newBtnCancel";
			newBtnCancel.Size = new System.Drawing.Size(70, 25);
			newBtnCancel.TabIndex = 22;
			newBtnCancel.ButtonFont = MyDefaultSkinFontBold10;
			//newBtn.ForeColor = System.Drawing.Color.Black;
			newBtnCancel.SetButtonText("Close",
				System.Drawing.Color.Black, System.Drawing.Color.Black,
				System.Drawing.Color.Green, System.Drawing.Color.Gray);
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
				System.Drawing.Color.Black, System.Drawing.Color.Black,
				System.Drawing.Color.Green, System.Drawing.Color.Gray);
			//newBtn.ButtonPressed += new ProjectMngmntGamePanels.ImageButton.ImageButtonEventArgsHandler(playButton_ButtonPressed);
			newBtnOK.Click += new System.EventHandler(this.newBtnOK_Click);
			newBtnOK.Visible = false;
			this.Controls.Add(newBtnOK);
		}

		private void newBtnOK_Click(object sender, System.EventArgs e)
		{

		}

		private void newBtnCancel_Click(object sender, System.EventArgs e)
		{
			_mainPanel.DisposeEntryPanel();
			//Close();
		}

		private bool CheckOrder(out ArrayList errs)
		{
			bool good_order = true;
			errs = new ArrayList();

			//No checks just now

			return good_order;
		}
		private bool HandleOrder()
		{
			bool good_order = true;
			ArrayList attrs = new ArrayList();
			attrs.Add(new AttributeValuePair("cmd_type", "cancel_project"));
			attrs.Add(new AttributeValuePair("projectnodename", this.ChosenProject_NodeName));
			new Node(queueNode, "task", "", attrs);
			return good_order;
		}

		private void newBtnYes_Click(object sender, System.EventArgs e)
		{
			bool good_order_placed = false;
			ArrayList errs = new ArrayList();

			switch ((int)this.selected_action)
			{
				case (int)emCancelAction.CANCEL_PROJECT_EXECUTION:
					good_order_placed = CheckOrder_CancelProjectExec(out errs);
					break;
				case (int)emCancelAction.CLEAR_PROJECT_INSTALL:
					good_order_placed = CheckOrder_ClearProjectInstall(out errs);
					break;
				case (int)emCancelAction.CANCEL_CHANGE:
					good_order_placed = CheckOrder_CancelChange(out errs);
					break;
				case (int)emCancelAction.CANCEL_UPGRADE:
					good_order_placed = CheckOrder_CancelUpgrade(out errs);
					break;
				case (int)emCancelAction.CANCEL_ALL_DAY:
					good_order_placed = CheckOrder_CancelAllDay(out errs);
					break;
			}
			if (good_order_placed)
			{
				switch ((int)this.selected_action)
				{
					case (int)emCancelAction.CANCEL_PROJECT_EXECUTION:
						HandleOrder_CancelProjectExec();
						break;
					case (int)emCancelAction.CLEAR_PROJECT_INSTALL:
						HandleOrder_ClearProjectInstall();
						break;
					case (int)emCancelAction.CANCEL_CHANGE:
						HandleOrder_CancelChange();
						break;
					case (int)emCancelAction.CANCEL_UPGRADE:
						HandleOrder_CancelUpgrade();
						break;
					case (int)emCancelAction.CANCEL_ALL_DAY:
						HandleOrder_CancelAllDay();
						break;
				}
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
				MessageBox.Show(disp, "Error");
			}
		}

		private void newBtnNo_Click(object sender, System.EventArgs e)
		{
			_mainPanel.DisposeEntryPanel();
		}

		#endregion YesNO and OKCancel Methods

	}
}