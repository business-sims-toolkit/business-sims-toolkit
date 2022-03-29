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
	/// This is the popup which allows the faciliator to arrange all the experts across the all the projects
	/// This is slightly refactored code from the old version of PM
	/// It doesn't have the clear buttons for each project 
	/// </summary>
	public class DataEntryPanel_FixExpertsPanel  : FlickerFreePanel
	{
		public enum expert_area
		{
			design_architect,
			build_manager,
			test_manager
		}

		protected IDataEntryControlHolder _mainPanel;

		protected NodeTree MyNodeTree = null;
		protected Node queueNode = null;
		protected Node projectsNode = null;
		protected Node expertsNode = null;
		protected Hashtable existingProjectsBySlot = new Hashtable();
		protected Hashtable existingProjectsByID = new Hashtable();
		protected Hashtable existingProjectIDByUID = new Hashtable();

		private string NobodyName = "--";
		private ArrayList Experts_names_DA = new ArrayList();
		private ArrayList Experts_names_BM = new ArrayList();
		private ArrayList Experts_names_TM = new ArrayList();
		protected Hashtable Expert_DA_Nodes = new Hashtable();
		protected Hashtable Expert_BM_Nodes = new Hashtable();
		protected Hashtable Expert_TM_Nodes = new Hashtable();
		protected Hashtable projectNodesTo_DA_CBLookup = new Hashtable();
		protected Hashtable projectNodesTo_BM_CBLookup = new Hashtable();
		protected Hashtable projectNodesTo_TM_CBLookup = new Hashtable();

		private System.Windows.Forms.Label title_DA_Label;
		private System.Windows.Forms.Label title_BM_Label;
		private System.Windows.Forms.Label title_TM_Label;
		private System.Windows.Forms.Label[] title_Project_Labels = new Label[6];
		private System.Windows.Forms.ComboBox[] cbProjectDAs = new ComboBox[6];
		private System.Windows.Forms.ComboBox[] cbProjectBMs = new ComboBox[6];
		private System.Windows.Forms.ComboBox[] cbProjectTMs = new ComboBox[6];

		protected Font MyDefaultSkinFontNormal10 = null;
		protected Font MyDefaultSkinFontBold8 = null;
		protected Font MyDefaultSkinFontBold10 = null;
		protected Font MyDefaultSkinFontBold12 = null;
		
		private ImageTextButton newBtnOK = new ImageTextButton(0);
		private ImageTextButton newBtnCancel = new ImageTextButton(0);

		private System.Windows.Forms.Panel pnl_FixExpertsPanel;
		private System.Windows.Forms.Label titleLabel;
		private System.Windows.Forms.Label helpLabel;
		private bool SelectionHandling = true;

		public DataEntryPanel_FixExpertsPanel (IDataEntryControlHolder mainPanel, NodeTree tree)
		{
			MyNodeTree = tree;
			_mainPanel = mainPanel;
			queueNode = tree.GetNamedNode("TaskManager");
			expertsNode = tree.GetNamedNode("experts");

			string fontname =  SkinningDefs.TheInstance.GetData("fontname");
			MyDefaultSkinFontNormal10 = ConstantSizeFont.NewFont(fontname,10);
			MyDefaultSkinFontBold8 = ConstantSizeFont.NewFont(fontname,8,FontStyle.Bold);
			MyDefaultSkinFontBold10 = ConstantSizeFont.NewFont(fontname,10,FontStyle.Bold);
			MyDefaultSkinFontBold12 = ConstantSizeFont.NewFont(fontname,12,FontStyle.Bold);

			this.BackgroundImage = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "images\\panels\\PM_opsback2.png");

			//this.ShowInTaskbar = false;
			//this.ClientSize = new Size (520,280);
			//this.FormBorderStyle = FormBorderStyle.None;
			//this.Opacity = 1.00;
			this.Size = new Size(520,255);

			//newBtnCancel.SetButton(AppInfo.TheInstance.Location + "\\images\\buttons\\button_70x25.png");
			newBtnCancel.SetVariants("images\\buttons\\button_70x25.png");
			newBtnCancel.Location = new System.Drawing.Point(400, 220);
			newBtnCancel.Name = "newBtnCancel";
			newBtnCancel.Size = new System.Drawing.Size(70, 25);
			newBtnCancel.TabIndex = 22;
			newBtnCancel.ButtonFont = MyDefaultSkinFontBold10;
			//newBtn.ForeColor = System.Drawing.Color.Black;
			newBtnCancel.SetButtonText("Cancel",
				System.Drawing.Color.Black, System.Drawing.Color.Black,
				System.Drawing.Color.Green, System.Drawing.Color.Gray);
			newBtnCancel.Click += new System.EventHandler(this.newBtnCancel_Click);
			this.Controls.Add(newBtnCancel);

			//newBtnOK.SetButton(AppInfo.TheInstance.Location + "\\images\\buttons\\button_70x25.png");
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
			newBtnOK.Click += new System.EventHandler(this.newBtnOK_Click);
			//newBtnOK.Visible = false;
			this.Controls.Add(newBtnOK);

			titleLabel = new System.Windows.Forms.Label();
			titleLabel.BackColor = System.Drawing.Color.Transparent;
			titleLabel.Font = MyDefaultSkinFontBold12;
			titleLabel.ForeColor = System.Drawing.Color.White;
			titleLabel.Location = new System.Drawing.Point(10, 10-2);
			titleLabel.Name = "titleLabel";
			titleLabel.Size = new System.Drawing.Size(380,18);
			titleLabel.TabIndex = 11;
			titleLabel.Text = "Project Experts";
			titleLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.Controls.Add(titleLabel);

			helpLabel = new System.Windows.Forms.Label();
			helpLabel.BackColor = System.Drawing.Color.Transparent;
			helpLabel.Font = MyDefaultSkinFontNormal10;
			helpLabel.ForeColor = System.Drawing.Color.White;
			helpLabel.Location = new System.Drawing.Point(10, 50-20-1);
			helpLabel.Name = "helpLabel";
			helpLabel.Size = new System.Drawing.Size(390,18);
			helpLabel.TabIndex = 20;
			helpLabel.Text = "Please select the project Experts";
			helpLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.Controls.Add(helpLabel);

			BuildCurrentProjectLookupList();
			Build_Experts_Data();
			BuildControls();
			ConfigureExpertCBs();

			pnl_FixExpertsPanel.Visible = true;
		}

		new public void Dispose()
		{
			queueNode = null;
			expertsNode = null;

			if (MyDefaultSkinFontNormal10 != null)
			{
				MyDefaultSkinFontNormal10.Dispose();
				MyDefaultSkinFontNormal10 = null;
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
				string project_id = n.GetAttribute("project_id");
				string project_slot = n.GetAttribute("slot");
				string project_status = n.GetAttribute("status");
				string project_uid = n.GetAttribute("uid");

				existingProjectsBySlot.Add(project_slot, n);
				existingProjectsByID.Add(project_id, n);
				existingProjectIDByUID.Add(project_uid, project_id);
			}
		}

		private bool getExpertData(Node expert_node, out string expert_name, 
			out string skill_type, out string assigned_project)
		{
			bool opsuccess = false;
			expert_name = "";
			skill_type = "";
			assigned_project = "";

			if (expert_node != null)
			{
				expert_name = expert_node.GetAttribute("expert_name");
				skill_type = expert_node.GetAttribute("skill_type");
				assigned_project = expert_node.GetAttribute("assigned_project");
				opsuccess = true;
			}
			return opsuccess;
		}

		private void Build_Experts_Data()
		{
			string expert_name = "";
			string skill_type = "";
			string assigned_project = "";

			Expert_DA_Nodes.Clear();
			Expert_BM_Nodes.Clear();
			Expert_TM_Nodes.Clear();

			//Local structure to sort the names into alphabetical order 
			Experts_names_DA.Clear();
			Experts_names_BM.Clear();
			Experts_names_TM.Clear();

			if (expertsNode != null)
			{
				foreach (Node expert_node in this.expertsNode.getChildren())
				{
					if (getExpertData(expert_node, out expert_name, out skill_type, out assigned_project))
					{
						//only allow not assigned project to be selected 
						//Assign the DA to the correct combobox 
						if (skill_type.ToLower() == "design architect")
						{
							Experts_names_DA.Add(expert_name);
							Expert_DA_Nodes.Add(expert_name, expert_node);
						}
						//Assign the BM to the correct combobox 
						if (skill_type.ToLower() == "build manager")
						{
							Experts_names_BM.Add(expert_name);
							Expert_BM_Nodes.Add(expert_name, expert_node);
						}
						//Assign the TM to the correct combobox 
						if (skill_type.ToLower() == "test manager")
						{
							Experts_names_TM.Add(expert_name);
							Expert_TM_Nodes.Add(expert_name, expert_node);
						}
					}
				}
			}
			Experts_names_DA.Sort();
			Experts_names_BM.Sort();
			Experts_names_TM.Sort();
		}

		private void BuildControls()
		{
			this.pnl_FixExpertsPanel = new Panel();
			this.pnl_FixExpertsPanel.SuspendLayout();

			pnl_FixExpertsPanel.Location = new System.Drawing.Point(10, 50);
			pnl_FixExpertsPanel.Name = "chooseUpgradeTypePanel";
			pnl_FixExpertsPanel.Size = new System.Drawing.Size(467, 170);
			pnl_FixExpertsPanel.BackColor = Color.FromArgb(176, 196, 222); //TODO SKIN COLOR
			pnl_FixExpertsPanel.BackColor = Color.Transparent;
			//pnl_FixExpertsPanel.BackColor = Color.LightSalmon;
			pnl_FixExpertsPanel.TabIndex = 13;
			this.Controls.Add(pnl_FixExpertsPanel);

			title_DA_Label = new System.Windows.Forms.Label();
			title_DA_Label.BackColor = System.Drawing.Color.Transparent;
			//title_DA_Label.BackColor = System.Drawing.Color.LightSeaGreen;
			title_DA_Label.Font = MyDefaultSkinFontBold8;
			title_DA_Label.ForeColor = System.Drawing.Color.Black;
			title_DA_Label.Location = new System.Drawing.Point(70, 5);
			title_DA_Label.Name = "titleLabel";
			title_DA_Label.Size = new System.Drawing.Size(120, 18);
			title_DA_Label.TabIndex = 11;
			title_DA_Label.Text = "Design Architect";
			title_DA_Label.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			pnl_FixExpertsPanel.Controls.Add(title_DA_Label);

			title_BM_Label = new System.Windows.Forms.Label();
			title_BM_Label.BackColor = System.Drawing.Color.Transparent;
			//title_BM_Label.BackColor = System.Drawing.Color.LightSeaGreen;
			title_BM_Label.Font = MyDefaultSkinFontBold8;
			title_BM_Label.ForeColor = System.Drawing.Color.Black;
			title_BM_Label.Location = new System.Drawing.Point(200+5, 5);
			title_BM_Label.Name = "titleLabel";
			title_BM_Label.Size = new System.Drawing.Size(120, 18);
			title_BM_Label.TabIndex = 11;
			title_BM_Label.Text = "Build Manager";
			title_BM_Label.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			pnl_FixExpertsPanel.Controls.Add(title_BM_Label);

			title_TM_Label = new System.Windows.Forms.Label();
			title_TM_Label.BackColor = System.Drawing.Color.Transparent;
			//title_TM_Label.BackColor = System.Drawing.Color.LightSeaGreen;
			title_TM_Label.Font = MyDefaultSkinFontBold8;
			title_TM_Label.ForeColor = System.Drawing.Color.Black;
			title_TM_Label.Location = new System.Drawing.Point(330+10, 5);
			title_TM_Label.Name = "titleLabel";
			title_TM_Label.Size = new System.Drawing.Size(120, 18);
			title_TM_Label.TabIndex = 11;
			title_TM_Label.Text = "Test Manager";
			title_TM_Label.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			pnl_FixExpertsPanel.Controls.Add(title_TM_Label);

			Label title_Project_Label = new System.Windows.Forms.Label();
			title_Project_Label.BackColor = System.Drawing.Color.Transparent;
			//title_Project_Label.BackColor = System.Drawing.Color.LightSeaGreen;
			title_Project_Label.Font = MyDefaultSkinFontBold8;
			title_Project_Label.ForeColor = System.Drawing.Color.Black;
			title_Project_Label.Location = new System.Drawing.Point(0, 5);
			title_Project_Label.Name = "titleLabel";
			title_Project_Label.Size = new System.Drawing.Size(60, 18);
			title_Project_Label.TabIndex = 11;
			title_Project_Label.Text = "Project";
			title_Project_Label.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			pnl_FixExpertsPanel.Controls.Add(title_Project_Label);

			projectNodesTo_DA_CBLookup.Clear();
			projectNodesTo_BM_CBLookup.Clear();
			projectNodesTo_TM_CBLookup.Clear();

			for (int step = 0; step < 6; step++)
			{
				title_Project_Labels[step] = new System.Windows.Forms.Label();
				title_Project_Labels[step].BackColor = System.Drawing.Color.Transparent;
				//title_Project_Labels[step].BackColor = System.Drawing.Color.LightSeaGreen;
				title_Project_Labels[step].Font = MyDefaultSkinFontBold8;
				title_Project_Labels[step].ForeColor = System.Drawing.Color.Black;
				title_Project_Labels[step].Location = new System.Drawing.Point(0, 30+22*step);
				title_Project_Labels[step].Name = "titleLabel";
				title_Project_Labels[step].Size = new System.Drawing.Size(60, 18);
				title_Project_Labels[step].TabIndex = 11;
				title_Project_Labels[step].Text = CONVERT.ToStr(step+1);
				title_Project_Labels[step].TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
				pnl_FixExpertsPanel.Controls.Add(title_Project_Labels[step]);

				string slot_name = CONVERT.ToStr(step);
				Node projectNode = null;
				string project_uid = "";
				if (existingProjectsBySlot.ContainsKey(slot_name))
				{
					projectNode = (Node)existingProjectsBySlot[slot_name];
					if (projectNode != null)
					{
						title_Project_Labels[step].Text = projectNode.GetAttribute("project_id");
						project_uid = projectNode.GetAttribute("uid");
					}
				}

				cbProjectDAs[step] = new ComboBox();
				cbProjectDAs[step].DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
				cbProjectDAs[step].Tag = project_uid;
				cbProjectDAs[step].Size = new Size(120,20);
				cbProjectDAs[step].Location = new System.Drawing.Point(70, 30 + 22 * step);
				cbProjectDAs[step].DropDown += new System.EventHandler(this.GeneralDropDownHandler);
				cbProjectDAs[step].SelectedIndexChanged += new System.EventHandler(this.GeneralSelectedIndexChangedHandler);
				cbProjectDAs[step].GotFocus += new EventHandler(GeneralGotFocusHandler);
				pnl_FixExpertsPanel.Controls.Add(cbProjectDAs[step]);

				if (project_uid != "")
				{
					projectNodesTo_DA_CBLookup.Add(project_uid, cbProjectDAs[step]);
				}

				cbProjectBMs[step] = new ComboBox();
				cbProjectBMs[step].DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
				cbProjectBMs[step].Tag = project_uid;
				cbProjectBMs[step].Size = new Size(120, 20);
				cbProjectBMs[step].Location = new System.Drawing.Point(205, 30 + 22 * step);
				cbProjectBMs[step].DropDown += new System.EventHandler(this.GeneralDropDownHandler);
				cbProjectBMs[step].SelectedIndexChanged += new System.EventHandler(this.GeneralSelectedIndexChangedHandler);
				cbProjectBMs[step].GotFocus += new EventHandler(GeneralGotFocusHandler);
				pnl_FixExpertsPanel.Controls.Add(cbProjectBMs[step]);

				if (project_uid != "")
				{
					projectNodesTo_BM_CBLookup.Add(project_uid, cbProjectBMs[step]);
				}

				cbProjectTMs[step] = new ComboBox();
				cbProjectTMs[step].DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
				cbProjectTMs[step].Tag = project_uid;
				cbProjectTMs[step].Size = new Size(120, 20);
				cbProjectTMs[step].Location = new System.Drawing.Point(340, 30 + 22 * step);
				cbProjectTMs[step].DropDown += new System.EventHandler(this.GeneralDropDownHandler);
				cbProjectTMs[step].SelectedIndexChanged += new System.EventHandler(this.GeneralSelectedIndexChangedHandler);
				cbProjectTMs[step].GotFocus += new EventHandler(GeneralGotFocusHandler);
				pnl_FixExpertsPanel.Controls.Add(cbProjectTMs[step]);

				if (project_uid != "")
				{
					projectNodesTo_TM_CBLookup.Add(project_uid, cbProjectTMs[step]);
				}
			}
			this.pnl_FixExpertsPanel.ResumeLayout();
		}

		private void ConfigureExpertCBs()
		{
			string expert_name = "";
			string skill_type = "";
			string assigned_project = "";
			ComboBox which_CB = null;
			int highlight;

			SelectionHandling = false;

			//adding the standard Nobody Name 
			for (int step = 0; step < 6; step++)
			{
				cbProjectDAs[step].Items.Add(NobodyName);
				cbProjectBMs[step].Items.Add(NobodyName);
				cbProjectTMs[step].Items.Add(NobodyName);
				cbProjectDAs[step].SelectedIndex = 0;
				cbProjectBMs[step].SelectedIndex = 0;
				cbProjectTMs[step].SelectedIndex = 0;
			}
			//=====================================================================
			//Iterate over the Design Architects 
			//=====================================================================
			foreach (string name in this.Expert_DA_Nodes.Keys)
			{
				Node n = (Node)Expert_DA_Nodes[name];
				if (n != null)
				{
					if (getExpertData(n, out expert_name, out skill_type, out assigned_project))
					{
						if (assigned_project == "")
						{
							//Not assigned to any one project, can be selected by any 
							for (int step = 0; step < 6; step++)
							{
								cbProjectDAs[step].Items.Add(expert_name);
							}
						}
						else
						{
							//assigned to a project, the string represents the project uid
							if (projectNodesTo_DA_CBLookup.ContainsKey(assigned_project))
							{
								which_CB = (ComboBox) projectNodesTo_DA_CBLookup[assigned_project];
								if (which_CB != null)
								{
									highlight = which_CB.Items.Add(expert_name);
									which_CB.SelectedIndex = highlight;
								}
							}
						}
					}
				}
			}
			//=====================================================================
			//Iterate over the Build Managers
			//=====================================================================
			foreach (string name in this.Expert_BM_Nodes.Keys)
			{
				Node n = (Node)Expert_BM_Nodes[name];
				if (n != null)
				{
					if (getExpertData(n, out expert_name, out skill_type, out assigned_project))
					{
						if (assigned_project == "")
						{
							//Not assigned to any one project, can be selected by any 
							for (int step = 0; step < 6; step++)
							{
								cbProjectBMs[step].Items.Add(expert_name);
							}
						}
						else
						{
							//assigned to a project, the string represents the project uid
							if (projectNodesTo_BM_CBLookup.ContainsKey(assigned_project))
							{
								which_CB = (ComboBox)projectNodesTo_BM_CBLookup[assigned_project];
								if (which_CB != null)
								{
									highlight = which_CB.Items.Add(expert_name);
									which_CB.SelectedIndex = highlight;
								}
							}
						}
					}
				}
			}

			//=====================================================================
			//Iterate over the Test Managers
			//=====================================================================
			foreach (string name in this.Expert_TM_Nodes.Keys)
			{
				Node n = (Node)Expert_TM_Nodes[name];
				if (n != null)
				{
					if (getExpertData(n, out expert_name, out skill_type, out assigned_project))
					{
						if (assigned_project == "")
						{
							//Not assigned to any one project, can be selected by any 
							for (int step = 0; step < 6; step++)
							{
								cbProjectTMs[step].Items.Add(expert_name);
							}
						}
						else
						{
							//assigned to a project, the string represents the project uid
							if (projectNodesTo_TM_CBLookup.ContainsKey(assigned_project))
							{
								which_CB = (ComboBox)projectNodesTo_TM_CBLookup[assigned_project];
								if (which_CB != null)
								{
									highlight = which_CB.Items.Add(expert_name);
									which_CB.SelectedIndex = highlight;
								}
							}
						}
					}
				}
			}
			SelectionHandling = true;
		}

		private void GeneralGotFocusHandler(object sender, System.EventArgs e)
		{
			DropDown(sender, e);
		}

		private void GeneralDropDownHandler(object sender, System.EventArgs e)
		{
			DropDown(sender, e);
		}

		private int DetermineWhichComboBox(expert_area tmp_expert_area, ComboBox tmpcb)
		{
			int which = -1;

			switch (tmp_expert_area)
			{
				case expert_area.design_architect: 
					for (int step=0;step<6; step++)
					{
						if (cbProjectDAs[step]==tmpcb)
						{
							which = step;
						}
					}
					break;
				case expert_area.build_manager:
					for (int step = 0; step < 6; step++)
					{
						if (cbProjectBMs[step] == tmpcb)
						{
							which = step;
						}
					}
					break;
				case expert_area.test_manager:
					for (int step = 0; step < 6; step++)
					{
						if (cbProjectTMs[step] == tmpcb)
						{
							which = step;
						}
					}
					break;
			}
			return which;
		}

		private void DropDown(object sender, System.EventArgs e)
		{
			string st = string.Empty;
			string[] PrjSelectedText = new string[6];
			bool[] EqualsPrjSelectedText = new bool[6];
			string currText = string.Empty;
			Boolean addflag = false;
			int index = -1;
			Boolean SetValue = false;

			if (SelectionHandling)
			{
				//Build a list of experts who aren't selected
				ComboBox cb = sender as ComboBox;
				if (null != cb)
				{
					SelectionHandling = false;

					currText = cb.Text;
					cb.Items.Clear();
					cb.Items.Add(NobodyName);
					//=====================================================================
					//==Handling DA========================================================
					//=====================================================================
					int whichDA = DetermineWhichComboBox(expert_area.design_architect, cb);
					if (whichDA != -1)
					{
						//
						for (int step = 0; step < 6; step++)
						{
							if ((cb == cbProjectDAs[step]))
							{
								PrjSelectedText[step] = currText;
							}
							else
							{
								PrjSelectedText[step] = (string)this.cbProjectDAs[step].SelectedItem;
							}
						}
						//
						index = -1;
						foreach (string ename in Experts_names_DA)
						{
							addflag = true;
							EqualsPrjSelectedText[0] = (ename == PrjSelectedText[0]);
							EqualsPrjSelectedText[1] = (ename == PrjSelectedText[1]);
							EqualsPrjSelectedText[2] = (ename == PrjSelectedText[2]);
							EqualsPrjSelectedText[3] = (ename == PrjSelectedText[3]);
							EqualsPrjSelectedText[4] = (ename == PrjSelectedText[4]);
							EqualsPrjSelectedText[5] = (ename == PrjSelectedText[5]);

							if ((EqualsPrjSelectedText[0]) & (cb != this.cbProjectDAs[0]))
							{
								addflag = false;
							}
							if ((EqualsPrjSelectedText[1]) & (cb != this.cbProjectDAs[1]))
							{
								addflag = false;
							}
							if ((EqualsPrjSelectedText[2]) & (cb != this.cbProjectDAs[2]))
							{
								addflag = false;
							}
							if ((EqualsPrjSelectedText[3]) & (cb != this.cbProjectDAs[3]))
							{
								addflag = false;
							}
							if ((EqualsPrjSelectedText[4]) & (cb != this.cbProjectDAs[4]))
							{
								addflag = false;
							}
							if ((EqualsPrjSelectedText[5]) & (cb != this.cbProjectDAs[5]))
							{
								addflag = false;
							}
							if (addflag)
							{
								index = cb.Items.Add(ename);
								if ((EqualsPrjSelectedText[0]) | (EqualsPrjSelectedText[1]) | (EqualsPrjSelectedText[2]) |
									(EqualsPrjSelectedText[3]) | (EqualsPrjSelectedText[4]) | (EqualsPrjSelectedText[5]))
								{
									cb.SelectedIndex = index;
									cb.Text = ename;
									SetValue = true;
								}
							}
						}
						if (SetValue == false)
						{
							cb.SelectedIndex = 0;
							cb.Text = NobodyName;
						}
					}
					//=====================================================================
					//==Handling BM========================================================
					//=====================================================================
					int whichBM = DetermineWhichComboBox(expert_area.build_manager, cb);
					if (whichBM != -1)
					{
						//
						for (int step = 0; step < 6; step++)
						{
							if ((cb == cbProjectBMs[step]))
							{
								PrjSelectedText[step] = currText;
							}
							else
							{
								PrjSelectedText[step] = (string)this.cbProjectBMs[step].SelectedItem;
							}
						}
						//
						index = -1;
						foreach (string ename in Experts_names_BM)
						{
							addflag = true;
							EqualsPrjSelectedText[0] = (ename == PrjSelectedText[0]);
							EqualsPrjSelectedText[1] = (ename == PrjSelectedText[1]);
							EqualsPrjSelectedText[2] = (ename == PrjSelectedText[2]);
							EqualsPrjSelectedText[3] = (ename == PrjSelectedText[3]);
							EqualsPrjSelectedText[4] = (ename == PrjSelectedText[4]);
							EqualsPrjSelectedText[5] = (ename == PrjSelectedText[5]);

							if ((EqualsPrjSelectedText[0]) & (cb != this.cbProjectBMs[0]))
							{
								addflag = false;
							}
							if ((EqualsPrjSelectedText[1]) & (cb != this.cbProjectBMs[1]))
							{
								addflag = false;
							}
							if ((EqualsPrjSelectedText[2]) & (cb != this.cbProjectBMs[2]))
							{
								addflag = false;
							}
							if ((EqualsPrjSelectedText[3]) & (cb != this.cbProjectBMs[3]))
							{
								addflag = false;
							}
							if ((EqualsPrjSelectedText[4]) & (cb != this.cbProjectBMs[4]))
							{
								addflag = false;
							}
							if ((EqualsPrjSelectedText[5]) & (cb != this.cbProjectBMs[5]))
							{
								addflag = false;
							}
							if (addflag)
							{
								index = cb.Items.Add(ename);
								if ((EqualsPrjSelectedText[0]) | (EqualsPrjSelectedText[1]) | (EqualsPrjSelectedText[2]) |
									(EqualsPrjSelectedText[3]) | (EqualsPrjSelectedText[4]) | (EqualsPrjSelectedText[5]))
								{
									cb.SelectedIndex = index;
									cb.Text = ename;
									SetValue = true;
								}
							}
						}
						if (SetValue == false)
						{
							cb.SelectedIndex = 0;
							cb.Text = NobodyName;
						}
					}
					//=====================================================================
					//==Handling TM========================================================
					//=====================================================================
					int whichTM = DetermineWhichComboBox(expert_area.test_manager, cb);
					if (whichTM != -1)
					{
						//
						for (int step = 0; step < 6; step++)
						{
							if ((cb == cbProjectTMs[step]))
							{
								PrjSelectedText[step] = currText;
							}
							else
							{
								PrjSelectedText[step] = (string)this.cbProjectTMs[step].SelectedItem;
							}
						}
						//
						index = -1;
						foreach (string ename in Experts_names_TM)
						{
							addflag = true;

							EqualsPrjSelectedText[0] = (ename == PrjSelectedText[0]);
							EqualsPrjSelectedText[1] = (ename == PrjSelectedText[1]);
							EqualsPrjSelectedText[2] = (ename == PrjSelectedText[2]);
							EqualsPrjSelectedText[3] = (ename == PrjSelectedText[3]);
							EqualsPrjSelectedText[4] = (ename == PrjSelectedText[4]);
							EqualsPrjSelectedText[5] = (ename == PrjSelectedText[5]);

							if ((EqualsPrjSelectedText[0]) & (cb != this.cbProjectTMs[0]))
							{
								addflag = false;
							}
							if ((EqualsPrjSelectedText[1]) & (cb != this.cbProjectTMs[1]))
							{
								addflag = false;
							}
							if ((EqualsPrjSelectedText[2]) & (cb != this.cbProjectTMs[2]))
							{
								addflag = false;
							}
							if ((EqualsPrjSelectedText[3]) & (cb != this.cbProjectTMs[3]))
							{
								addflag = false;
							}
							if ((EqualsPrjSelectedText[4]) & (cb != this.cbProjectTMs[4]))
							{
								addflag = false;
							}
							if ((EqualsPrjSelectedText[5]) & (cb != this.cbProjectTMs[5]))
							{
								addflag = false;
							}
							if (addflag)
							{
								index = cb.Items.Add(ename);
								if ((EqualsPrjSelectedText[0]) | (EqualsPrjSelectedText[1]) | (EqualsPrjSelectedText[2]) |
									(EqualsPrjSelectedText[3]) | (EqualsPrjSelectedText[4]) | (EqualsPrjSelectedText[5]))
								{
									cb.SelectedIndex = index;
									cb.Text = ename;
									SetValue = true;
								}
							}
						}
						if (SetValue == false)
						{
							cb.SelectedIndex = 0;
							cb.Text = NobodyName;
						}
					}
					//=====================================================================
					//=====================================================================
					//=====================================================================
				}
				SelectionHandling = true;
			}
		}

		private void GeneralSelectedIndexChangedHandler(object sender, System.EventArgs e)
		{
			if (SelectionHandling)
			{
				ComboBox cb = (System.Windows.Forms.ComboBox)(sender);
				if (cb != null)
				{
					//Rebuild this cb 
					//all 
				}
				string st = string.Empty;
			}
		}

		private bool CheckOrder(out ArrayList errs)
		{
			bool OpSuccess = true;
			ArrayList prjs_with_missing_experts = new ArrayList();

			errs = new ArrayList();
			
			//need to ensure that if we have a project declared 
			//then we need to set the experts for that project 
			foreach (string prj_uid in projectNodesTo_DA_CBLookup.Keys)
			{
				bool missing = false;
				ComboBox cb = (ComboBox)projectNodesTo_DA_CBLookup[prj_uid];
				if (cb.SelectedIndex==-1)
				{
					missing = true;
				}
				else
				{
					string sel = (string) cb.SelectedItem;
					if ((sel=="")|(sel==this.NobodyName))
					{
						missing = true;
					}
				}
				if (missing)
				{
					if (prjs_with_missing_experts.Contains(prj_uid) == false)
					{
						prjs_with_missing_experts.Add(prj_uid);
					}
				}
			}

			foreach (string prj_uid in projectNodesTo_BM_CBLookup.Keys)
			{
				bool missing = false;
				ComboBox cb = (ComboBox)projectNodesTo_BM_CBLookup[prj_uid];
				if (cb.SelectedIndex == -1)
				{
					missing = true;
				}
				else
				{
					string sel = (string)cb.SelectedItem;
					if ((sel == "") | (sel == this.NobodyName))
					{
						missing = true;
					}
				}
				if (missing)
				{
					if (prjs_with_missing_experts.Contains(prj_uid) == false)
					{
						prjs_with_missing_experts.Add(prj_uid);
					}
				}
			}

			foreach (string prj_uid in projectNodesTo_TM_CBLookup.Keys)
			{
				bool missing = false;
				ComboBox cb = (ComboBox)projectNodesTo_TM_CBLookup[prj_uid];
				if (cb.SelectedIndex == -1)
				{
					missing = true;
				}
				else
				{
					string sel = (string)cb.SelectedItem;
					if ((sel == "") | (sel == this.NobodyName))
					{
						missing = true;
					}
				}
				if (missing)
				{
					if (prjs_with_missing_experts.Contains(prj_uid) == false)
					{
						prjs_with_missing_experts.Add(prj_uid);
					}
				}
			}

			if (prjs_with_missing_experts.Count > 0)
			{
				OpSuccess = false;
				foreach (string miss_prj in prjs_with_missing_experts)
				{
					if (existingProjectIDByUID.ContainsKey(miss_prj))
					{
						string prj_id = (string)existingProjectIDByUID[miss_prj];
						errs.Add("Project " + prj_id + " missing expert assignment");
					}
				}
			}
			return OpSuccess;
		}

		private bool HandleOrder()
		{
			ArrayList used_DA_names = new ArrayList();
			ArrayList used_BM_names = new ArrayList();
			ArrayList used_TM_names = new ArrayList();

			//data matches the required entry data 
			ArrayList attrs = new ArrayList ();
			attrs.Add(new AttributeValuePair ("cmd_type", "change_all_experts"));

			//===============================================================
			//DAs
			//===============================================================
			for (int step = 0; step < 6; step++)
			{
				string da_name = (string)this.cbProjectDAs[step].SelectedItem;
				string prj_uid = (string)this.cbProjectDAs[step].Tag;
				if (da_name != this.NobodyName)
				{
					used_DA_names.Add(da_name);
					attrs.Add(new AttributeValuePair ("exp_da_"+da_name, prj_uid));
				}
			}
			foreach (string name in Experts_names_DA)
			{
				if (used_DA_names.Contains(name)== false)
				{
					attrs.Add(new AttributeValuePair ("exp_da_"+name, ""));
				}
			}

			//===============================================================
			//BMs
			//===============================================================
			for (int step = 0; step < 6; step++)
			{
				string bm_name = (string)this.cbProjectBMs[step].SelectedItem;
				string prj_uid = (string)this.cbProjectBMs[step].Tag;
				if (bm_name != this.NobodyName)
				{
					used_BM_names.Add(bm_name);
					attrs.Add(new AttributeValuePair("exp_bm_" + bm_name, prj_uid));
				}
			}
			foreach (string name in Experts_names_BM)
			{
				if (used_BM_names.Contains(name) == false)
				{
					attrs.Add(new AttributeValuePair("exp_bm_" + name, ""));
				}
			}

			//===============================================================
			//TMs
			//===============================================================
			for (int step = 0; step < 6; step++)
			{
				string tm_name = (string)this.cbProjectTMs[step].SelectedItem;
				string prj_uid = (string)this.cbProjectTMs[step].Tag;
				if (tm_name != this.NobodyName)
				{
					used_TM_names.Add(tm_name);
					attrs.Add(new AttributeValuePair("exp_tm_" + tm_name, prj_uid));
				}
			}
			foreach (string name in Experts_names_TM)
			{
				if (used_TM_names.Contains(name) == false)
				{
					attrs.Add(new AttributeValuePair("exp_tm_" + name, ""));
				}
			}
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
		}
	}
}