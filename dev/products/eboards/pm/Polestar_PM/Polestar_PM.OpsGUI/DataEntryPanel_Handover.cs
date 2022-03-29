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
using Polestar_PM.DataLookup;
using BusinessServiceRules;
using Polestar_PM.OpsEngine;

using GameManagement;

namespace Polestar_PM.OpsGUI
{
	public class DataEntryPanel_Handover  : FlickerFreePanel
	{
		protected NodeTree MyNodeTree;
		protected IDataEntryControlHolder _mainPanel;
		protected Node queueNode;
		protected Node CurrDayNode = null;
		protected Hashtable existingProjectsBySlot = new Hashtable();

		protected Font MyDefaultSkinFontNormal8 = null;
		protected Font MyDefaultSkinFontNormal10 = null;
		protected Font MyDefaultSkinFontNormal12 = null;
		protected Font MyDefaultSkinFontBold8 = null;
		protected Font MyDefaultSkinFontBold10 = null;
		protected Font MyDefaultSkinFontBold12 = null;
		protected Font MyDefaultSkinFontBold20 = null;

		bool inTestMode;

		private ImageTextButton[] btnProjectsSlots = new ImageTextButton[7];

		private ImageTextButton newBtnOK = new ImageTextButton(0);
		private ImageTextButton newBtnCancel = new ImageTextButton(0);
		private ImageTextButton newBtnYes = new ImageTextButton(0);
		private ImageTextButton newBtnNo = new ImageTextButton(0);
		private ImageTextButton newBtnProject = new ImageTextButton(0);
		private ImageTextButton newBtnInstall = new ImageTextButton(0);
		//private ImageTextButton newBtnRebuild = new ImageTextButton(0);
		private ImageTextButton newBtnAddIterativeRequest = new ImageTextButton(0);
		private ImageTextButton newBtnClearIterativeRequest = new ImageTextButton(0);

		private System.Windows.Forms.TextBox locTextBox;
		private System.Windows.Forms.TextBox dayTextBox;
		private System.Windows.Forms.Label titleLocation;
		private System.Windows.Forms.Label titleDay;
		private System.Windows.Forms.Label titleRebuild;

		private System.Windows.Forms.Panel pnl_ChooseSlot;
		private System.Windows.Forms.Panel pnl_OptionChoice;
		private System.Windows.Forms.Label titleLabel;
		private System.Windows.Forms.Label helpLabel;
		private System.Windows.Forms.Label projectLabel;

		private int SlotDisplay =0;
		private Node projectNodeToUse = null;
		private string ChosenProject_NodeName;
		private string install_location = "";
		private int install_day = 0;

		private int MaxGameDays = 25;

		int round;
		string projectTerm;
		bool performLocationValidationChecks = false;
		bool display_seven_projects = false;
		bool showRebuild = false;

		Panel errorPanel;

		bool showIT;
		
		/// <summary>
		/// The handover information is 
		///   The Location that we will be installing the project into
		///   The Day that we will attempt the install 
		/// This version has the iterative development otion 
		/// </summary>
		/// <param name="mainPanel"></param>
		/// <param name="tree"></param>
		public DataEntryPanel_Handover(IDataEntryControlHolder mainPanel, NodeTree tree, 
			NetworkProgressionGameFile gameFile, bool inTestMode)
		{
			MyNodeTree = tree;
			queueNode = tree.GetNamedNode("TaskManager");
			CurrDayNode = tree.GetNamedNode("CurrentDay");
			_mainPanel = mainPanel;
			this.inTestMode = inTestMode;

			showIT = true;

			Node projectsNode = tree.GetNamedNode("pm_projects_running");
			display_seven_projects = projectsNode.GetBooleanAttribute("display_seven_projects", false);

			round = gameFile.CurrentRound;
			projectTerm = SkinningDefs.TheInstance.GetData("project_term_round_" + LibCore.CONVERT.ToStr(round), "Project");

			if ((round == 3) && ! inTestMode)
			{
				showRebuild = true;
			}

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
			this.Size = new Size (520,255);
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

			if (showIT)
			{
				if (inTestMode)
				{
					titleLabel.Text = "Test Handover";
				}
				else
				{
					titleLabel.Text = "Handover";
				}
			}
			else
			{
				titleLabel.Text = "Iteration";
			}

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

			errorPanel = new Panel ();
			errorPanel.Location = new Point (85, pnl_OptionChoice.Bottom);
			errorPanel.Size = new Size (350, 200 - errorPanel.Top);
			errorPanel.BackColor = Color.Transparent;
			Controls.Add(errorPanel);
		}

		new public void Dispose()
		{
			MyNodeTree = null;
			queueNode = null;
			CurrDayNode = null;
			existingProjectsBySlot.Clear();

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

		private int getcurrentDay()
		{
			int current_day = 0;
			if (CurrDayNode != null)
			{
				current_day = CurrDayNode.GetIntAttribute("day", 0);
			}
			return current_day;
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
				string project_state_name = n.GetAttribute("state_name");
				
				existingProjectsBySlot.Add(project_slot, n);
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

			if (inTestMode)
			{
				newBtnCancel.SetButtonText("Close",
					System.Drawing.Color.Black, System.Drawing.Color.Black,
					System.Drawing.Color.Green, System.Drawing.Color.Gray);
			}
			else
			{
				newBtnCancel.SetButtonText("Cancel",
					System.Drawing.Color.Black, System.Drawing.Color.Black,
					System.Drawing.Color.Green, System.Drawing.Color.Gray);
			}
			//newBtn.ButtonPressed += new ProjectMngmntGamePanels.ImageButton.ImageButtonEventArgsHandler(playButton_ButtonPressed);
			newBtnCancel.Click += new System.EventHandler(this.newBtnCancel_Click);
			this.Controls.Add(newBtnCancel);

			//newBtnOK.SetButton(AppInfo.TheInstance.Location+"\\images\\buttons\\button_70x25.png");
			newBtnOK.SetVariants("images\\buttons\\button_70x25.png");
			newBtnOK.Name = "newBtnOK";
			newBtnOK.Size = new System.Drawing.Size(70, 25);
			newBtnOK.TabIndex = 21;
			newBtnOK.ButtonFont = MyDefaultSkinFontBold10;
			//newBtn.ForeColor = System.Drawing.Color.Black;

			if (inTestMode)
			{
				newBtnOK.Location = new System.Drawing.Point(400, 60);
				newBtnOK.SetButtonText("Test",
					System.Drawing.Color.Black, System.Drawing.Color.Black,
					System.Drawing.Color.Green, System.Drawing.Color.Gray);

				locTextBox.Location = new Point(210, locTextBox.Top);
			}
			else
			{
				newBtnOK.Location = new System.Drawing.Point(300, 220);
				newBtnOK.SetButtonText("OK",
					System.Drawing.Color.Black, System.Drawing.Color.Black,
					System.Drawing.Color.Green, System.Drawing.Color.Gray);
			}
			//newBtn.ButtonPressed += new ProjectMngmntGamePanels.ImageButton.ImageButtonEventArgsHandler(playButton_ButtonPressed);
			newBtnOK.Click += new System.EventHandler(this.newBtnOK_Click);
			newBtnOK.Visible = false;
			newBtnOK.BringToFront();
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
				string display_text = (step+1).ToString();  
				bool project_active = false;
				Node project_node = null;
				//determine the 
				if (existingProjectsBySlot.ContainsKey(step.ToString()))
				{
					project_node = (Node) existingProjectsBySlot[step.ToString()];
					display_text = project_node.GetAttribute("project_id");

					////We no longer restrict the setting of install lovcation to a particular state
					////we can do this anytime (just uncomment the code to go back) 
					//ProjectReader pr = new ProjectReader(project_node);
					//if ((pr.InState(emProjectOperationalState.PROJECT_STATE_INSTALL_NO_LOCATION))|
					//	(pr.InState(emProjectOperationalState.PROJECT_STATE_INSTALLED_FAIL)))
					//{
						project_active = true;
					//}
					//pr.Dispose();
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

		public void Build_Option_Controls() 
		{
			if (showRebuild)
			{
				//Build the location Title
				this.titleLocation = new System.Windows.Forms.Label();
				this.titleLocation.BackColor = System.Drawing.Color.Transparent;
				this.titleLocation.Font = MyDefaultSkinFontBold12;
				this.titleLocation.ForeColor = System.Drawing.Color.Black;
				this.titleLocation.Location = new System.Drawing.Point(10, 14);
				this.titleLocation.Name = "titleLabel";
				this.titleLocation.Size = new System.Drawing.Size(200, 18);
				this.titleLocation.TabIndex = 11;
				this.titleLocation.Text = "Install Location";
				this.titleLocation.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
				//this.titleLocation.BackColor = System.Drawing.Color.Violet;
				pnl_OptionChoice.Controls.Add(titleLocation);

				//Build the location Test Box
				this.locTextBox = new System.Windows.Forms.TextBox();
				this.locTextBox.BackColor = System.Drawing.Color.Black;
				this.locTextBox.Font = this.MyDefaultSkinFontBold12;
				this.locTextBox.ForeColor = System.Drawing.Color.White;
				this.locTextBox.Location = new System.Drawing.Point (290, 10);
				this.locTextBox.Name = "locTextBox";
				this.locTextBox.Size = new System.Drawing.Size(100, 20);
				this.locTextBox.TabIndex = 28;
				this.locTextBox.Text = "";
				this.locTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
				this.locTextBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
				locTextBox.TextChanged += new EventHandler(locTextBox_TextChanged);
				pnl_OptionChoice.Controls.Add(locTextBox);

				this.titleDay = new System.Windows.Forms.Label();
				this.titleDay.BackColor = System.Drawing.Color.Transparent;
				this.titleDay.Font = MyDefaultSkinFontBold12;
				this.titleDay.ForeColor = System.Drawing.Color.Black;
				this.titleDay.Location = new System.Drawing.Point(10, 14+40-5);
				this.titleDay.Name = "titleLabel";
				this.titleDay.Size = new System.Drawing.Size(200, 18);
				this.titleDay.TabIndex = 11;
				this.titleDay.Text = "Install Day";
				this.titleDay.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
				//this.titleDay.BackColor = System.Drawing.Color.Thistle;
				pnl_OptionChoice.Controls.Add(titleDay);

				// 
				// dayTextBox
				// 
				this.dayTextBox = new FilteredTextBox(TextBoxFilterType.Digits);
				this.dayTextBox.BackColor = System.Drawing.Color.Black;
				this.dayTextBox.Font = this.MyDefaultSkinFontBold12;
				this.dayTextBox.ForeColor = System.Drawing.Color.White;
				this.dayTextBox.Location = new System.Drawing.Point(290, 10 + 40 - 5);
				this.dayTextBox.Name = "locTextBox";
				this.dayTextBox.Size = new System.Drawing.Size(100, 43);
				this.dayTextBox.TabIndex = 28;
				this.dayTextBox.Text = "";
				this.dayTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
				this.dayTextBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
				pnl_OptionChoice.Controls.Add(dayTextBox);

				if (inTestMode)
				{
					titleDay.Hide();
					dayTextBox.Hide();
				}

				newBtnInstall = new ImageTextButton(0);
				//newBtnInstall.SetButton(AppInfo.TheInstance.Location + "\\images\\buttons\\blank_med.png");
				newBtnInstall.SetVariants("images\\buttons\\button_100x25.png");
				newBtnInstall.Location = new System.Drawing.Point(290, 10 + 80-10);
				newBtnInstall.Name = "Button1";
				newBtnInstall.Size = new System.Drawing.Size(100, 25);
				newBtnInstall.TabIndex = 8;
				newBtnInstall.ButtonFont = MyDefaultSkinFontBold8;
				newBtnInstall.SetButtonText("Book Install",
					System.Drawing.Color.Black, System.Drawing.Color.Black,
					System.Drawing.Color.White, System.Drawing.Color.Gray);
				newBtnInstall.Click += new System.EventHandler(newBtnInstall_Click);
				pnl_OptionChoice.Controls.Add(newBtnInstall);

				if (! showIT)
				{
					titleDay.Hide();
					dayTextBox.Hide();

					titleLocation.Hide();
					locTextBox.Hide();

					newBtnInstall.Hide();
				}

				this.titleRebuild = new System.Windows.Forms.Label();
				this.titleRebuild.BackColor = System.Drawing.Color.Transparent;
				this.titleRebuild.Font = MyDefaultSkinFontBold12;
				this.titleRebuild.ForeColor = System.Drawing.Color.Black;
				this.titleRebuild.Location = new System.Drawing.Point(10, 54 + 80);
				this.titleRebuild.Name = "titleLabel";
				this.titleRebuild.Size = new System.Drawing.Size(260, 18);
				this.titleRebuild.TabIndex = 11;
				this.titleRebuild.Text = "Iteration";
				this.titleRebuild.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
				//this.titleRebuild.BackColor = System.Drawing.Color.SteelBlue;
				pnl_OptionChoice.Controls.Add(titleRebuild);

				newBtnAddIterativeRequest = new ImageTextButton(0);
				//newBtnAddIterativeRequest.SetButton(AppInfo.TheInstance.Location + "\\images\\buttons\\blank_med.png");
				newBtnAddIterativeRequest.SetVariants("images\\buttons\\button_100x25.png");
				newBtnAddIterativeRequest.Location = new System.Drawing.Point(290, 50 + 80);
				newBtnAddIterativeRequest.Name = "newBtnAddIterativeRequest";
				newBtnAddIterativeRequest.Size = new System.Drawing.Size(100, 25);
				newBtnAddIterativeRequest.TabIndex = 8;
				newBtnAddIterativeRequest.ButtonFont = MyDefaultSkinFontBold8;
				newBtnAddIterativeRequest.SetButtonText("Add Cycle",
					System.Drawing.Color.Black, System.Drawing.Color.Black,
					System.Drawing.Color.White, System.Drawing.Color.Gray);
				newBtnAddIterativeRequest.Click += new System.EventHandler(newBtnAddIterativeRequest_Click);
				pnl_OptionChoice.Controls.Add(newBtnAddIterativeRequest);

				newBtnClearIterativeRequest = new ImageTextButton(0);
				//newBtnClearIterativeRequest.SetButton(AppInfo.TheInstance.Location + "\\images\\buttons\\blank_med.png");
				newBtnClearIterativeRequest.SetVariants("images\\buttons\\button_100x25.png");
				newBtnClearIterativeRequest.Location = new System.Drawing.Point(290, 50 + 80);
				newBtnClearIterativeRequest.Name = "newBtnClearIterativeRequest";
				newBtnClearIterativeRequest.Size = new System.Drawing.Size(100, 25);
				newBtnClearIterativeRequest.TabIndex = 8;
				newBtnClearIterativeRequest.ButtonFont = MyDefaultSkinFontBold8;
				newBtnClearIterativeRequest.SetButtonText("Clear Cycle",
					System.Drawing.Color.Black, System.Drawing.Color.Black,
					System.Drawing.Color.White, System.Drawing.Color.Gray);
				newBtnClearIterativeRequest.Click += new System.EventHandler(newBtnClearIterativeRequest_Click);
				pnl_OptionChoice.Controls.Add(newBtnClearIterativeRequest);

			}
			else
			{
				//Build the location Title
				this.titleLocation = new System.Windows.Forms.Label();
				this.titleLocation.BackColor = System.Drawing.Color.Transparent;
				this.titleLocation.Font = MyDefaultSkinFontBold12;
				this.titleLocation.ForeColor = System.Drawing.Color.Black;
				this.titleLocation.Location = new System.Drawing.Point(10, 10);
				this.titleLocation.Name = "titleLabel";
				this.titleLocation.Size = new System.Drawing.Size(200, 18);
				this.titleLocation.TabIndex = 11;
				this.titleLocation.Text = "Install Location";
				this.titleLocation.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
				pnl_OptionChoice.Controls.Add(titleLocation);

				//Build the location Test Box
				this.locTextBox = new System.Windows.Forms.TextBox();
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
				locTextBox.TextChanged += new EventHandler(locTextBox_TextChanged);
				pnl_OptionChoice.Controls.Add(locTextBox);

				this.titleDay = new System.Windows.Forms.Label();
				this.titleDay.BackColor = System.Drawing.Color.Transparent;
				this.titleDay.Font = MyDefaultSkinFontBold12;
				this.titleDay.ForeColor = System.Drawing.Color.Black;
				this.titleDay.Location = new System.Drawing.Point(10, 50);
				this.titleDay.Name = "titleLabel";
				this.titleDay.Size = new System.Drawing.Size(200, 18);
				this.titleDay.TabIndex = 11;
				this.titleDay.Text = "Install Day";
				this.titleDay.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
				pnl_OptionChoice.Controls.Add(titleDay);

				// 
				// dayTextBox
				// 
				this.dayTextBox = new FilteredTextBox(TextBoxFilterType.Digits);
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
				pnl_OptionChoice.Controls.Add(dayTextBox);

				if (inTestMode)
				{
					titleDay.Hide();
					dayTextBox.Hide();

					locTextBox_TextChanged(null, null);
				}
			}
		}

		void locTextBox_TextChanged (object sender, EventArgs e)
		{
			if (inTestMode)
			{
				newBtnOK.Enabled = ! string.IsNullOrEmpty(locTextBox.Text);

				if (errorPanel != null)
				{
					errorPanel.Controls.Clear();
				}
			}
		}

		public void BuildBaseControls() 
		{
			pnl_ChooseSlot = new System.Windows.Forms.Panel();
			this.pnl_OptionChoice = new System.Windows.Forms.Panel();
			pnl_ChooseSlot.SuspendLayout();
			pnl_OptionChoice.SuspendLayout();
			this.SuspendLayout();

			pnl_ChooseSlot.Location = new System.Drawing.Point(78, 50);
			pnl_ChooseSlot.Name = "pnl_ChooseSlot";
			pnl_ChooseSlot.Size = new System.Drawing.Size(390, 160);
			//pnl_ChooseSlot.BackColor = Color.FromArgb(176,196,222); //TODO SKIN
			pnl_ChooseSlot.BackColor = Color.Transparent;
			pnl_ChooseSlot.TabIndex = 13;

			if (inTestMode)
			{
				pnl_OptionChoice.Location = new System.Drawing.Point(85, 50);
				pnl_OptionChoice.Size = new System.Drawing.Size(290, 50);
			}
			else
			{
				pnl_OptionChoice.Location = new System.Drawing.Point(78, 50);
				pnl_OptionChoice.Size = new System.Drawing.Size(398, 160);
			}
			pnl_OptionChoice.Name = "pnl_YesNoChoice";
			//pnl_OptionChoice.BackColor = Color.FromArgb(176,196,222); //TODO SKIN
			pnl_OptionChoice.BackColor = Color.Transparent;
			pnl_OptionChoice.TabIndex = 14;
			pnl_OptionChoice.Visible = false;

			BuildSlotButtonControls();
			Build_Option_Controls();
			
			this.Controls.Add(pnl_ChooseSlot);
			this.Controls.Add(pnl_OptionChoice);
			this.Name = "HandoverControl";
			this.Size = new System.Drawing.Size(520,280);
			this.pnl_ChooseSlot.ResumeLayout(false);
			this.pnl_OptionChoice.ResumeLayout(false);
			this.ResumeLayout(false);
		}

		private void Slot_Button_Click(object sender, System.EventArgs e)
		{
			//Extract out the FSC from the control that was used
			if (sender is ImageTextButton)
			{
				ImageTextButton ib = (ImageTextButton)sender;
				if (ib != null)
				{
					projectNodeToUse = (Node)(ib.Tag);
					SlotDisplay = projectNodeToUse.GetIntAttribute("slot",0);
					ChosenProject_NodeName = projectNodeToUse.GetAttribute("name"); 
					string display_text = projectNodeToUse.GetAttribute("project_id");

					ProjectReader pr = new ProjectReader(projectNodeToUse);

					locTextBox.Enabled = true;
					dayTextBox.Enabled = true;

					if ((pr.InState(emProjectOperationalState.PROJECT_STATE_INSTALL_NO_LOCATION))|
						(pr.InState(emProjectOperationalState.PROJECT_STATE_INSTALLED_FAIL)))
					{
						//In this case, the location is empty
						locTextBox.Text = "";
						dayTextBox.Text = "";
						helpLabel.Text = "Install to Location on defined Day";
					}
					else
					{
						locTextBox.Text = "";
						dayTextBox.Text = "";
						//If we have a defined location then display it 
						string stored_install_location = pr.getInstallLocation();
						if (stored_install_location != "")
						{
							locTextBox.Text = stored_install_location;
						}
						int defined_install_day = pr.getInstallDay();
						if (defined_install_day > 0)
						{
							dayTextBox.Text = CONVERT.ToStr(defined_install_day);
						}

						if (showIT)
						{
							if (inTestMode)
							{
								helpLabel.Text = "Define Install Location";
							}
							else
							{
								helpLabel.Text = "Define Install Location and Install Day";
							}
						}
						else
						{
							helpLabel.Text = "";
						}
					}

					this.pnl_OptionChoice.Visible = true;
					this.pnl_ChooseSlot.Visible = false;

					projectLabel.Visible = true;
					newBtnProject.SetButtonText(display_text,
						System.Drawing.Color.Black,System.Drawing.Color.Black,
						System.Drawing.Color.Green,System.Drawing.Color.Gray);
					newBtnProject.Visible = true;

					if (showRebuild == false)
					{
						newBtnOK.Visible = true;
						newBtnOK.Enabled = true;

						locTextBox_TextChanged(null, null);

						this.newBtnAddIterativeRequest.Visible = false;
						this.newBtnClearIterativeRequest.Visible = false;
					}
					else
					{
						if (pr.getRecycleRequestPendingStatus()==false)
						{
							this.newBtnAddIterativeRequest.Visible = true;
							this.newBtnClearIterativeRequest.Visible = false;
						}
						else
						{
							this.newBtnAddIterativeRequest.Visible = false;
							this.newBtnClearIterativeRequest.Visible = true;
						}
					}
					SetFocus();
				}
			}
		}


		private bool CheckOrder(out ArrayList errs)
		{
			bool OpSuccess = true;
			errs = new ArrayList();

			bool location_exists = false;
			bool location_empty = false;
			Node locationnode = null;
			string errormsg = "";

			//Extract the day 
			install_location = this.locTextBox.Text;
			install_day = CONVERT.ParseIntSafe(this.dayTextBox.Text, 0);

			//===============================================================
			//==Check for any Day based problems=============================
			//===============================================================
			if (install_day > 0)
			{
				if (install_day > MaxGameDays)
				{
					errs.Add("Requested Day is greater than " + CONVERT.ToStr(MaxGameDays));
					OpSuccess = false;
				}
				if (install_day <= getcurrentDay())
				{
					errs.Add("Requested Day has passed");
					OpSuccess = false;
				}

				//Check that Day is clear 
				bool totallyFree = false;
				bool projects_allowed = false;

				OpsReader myOps = new OpsReader(this.MyNodeTree);
				myOps.isDayFree(install_day, out totallyFree, out projects_allowed);
				myOps.Dispose();

				if (totallyFree == false)
				{
					errs.Add("Day has already been booked");
					OpSuccess = false;
				}
			}
			else if (! inTestMode)
			{
				errs.Add("Day not defined ");
				OpSuccess = false;
			}

			//===============================================================
			//==Check for any location based problems========================
			//===============================================================
			if (install_location != "")
			{
				//This is not used normally but I think it will come back 
				//so it switched to false for now
				if (performLocationValidationChecks || inTestMode)
				{
					AppInstaller ai = new AppInstaller (MyNodeTree);

					ai.location_checks(install_location, out location_exists, out location_empty,
								out locationnode, out errormsg);
					if (location_exists == false)
					{
						errs.Add("location " + install_location + " does not exist");
						OpSuccess = false;
					}
					if (location_empty == false)
					{
						errs.Add("location " + install_location + " already used");
						OpSuccess = false;
					}

					if (inTestMode && OpSuccess)
					{
						bool platformOk, ramOk, discOk;
						ai.hardware_checks(install_location, projectNodeToUse,
						                   out platformOk, out ramOk, out discOk);

						if (! platformOk)
						{
							errs.Add("Platform doesn't match location " + install_location);
							OpSuccess = false;
						}

						if (! ramOk)
						{
							errs.Add("Insufficient memory");
							OpSuccess = false;
						}

						if (! discOk)
						{
							errs.Add("Insufficient storage");
							OpSuccess = false;
						}

						if (errs.Count == 0)
						{
							errs.Add("Planned install OK");
							OpSuccess = false;
						}
					}
				}
			}
			else
			{
				errs.Add("location not defined");
				OpSuccess = false;
			}
			return OpSuccess;
		}

		private bool HandleOrder()
		{
			if (inTestMode)
			{
				return false;
			}
			else
			{
				//need to check that location exists 
				install_location = this.locTextBox.Text;
				install_day = CONVERT.ParseIntSafe(this.dayTextBox.Text, 0);

				ProjectReader pr = new ProjectReader(projectNodeToUse);
				string desc = pr.getProjectTextDescription();
				pr.Dispose();

				if (install_location != "")
				{
					ArrayList attrs = new ArrayList();
					attrs.Add(new AttributeValuePair("cmd_type", "install_project"));
					attrs.Add(new AttributeValuePair("nodename", ChosenProject_NodeName));
					attrs.Add(new AttributeValuePair("nodedesc", desc));
					attrs.Add(new AttributeValuePair("install_location", install_location));
					attrs.Add(new AttributeValuePair("install_day", install_day));
					new Node(queueNode, "task", "", attrs);

					_mainPanel.DisposeEntryPanel();
				}
				else
				{
					MessageBox.Show("please enter a location", "Install location Fault");
				}
				return false;
			}
		}


		private void newBtnInstall_Click(object sender, System.EventArgs e)
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

		//private void newBtnRebuild_Click(object sender, System.EventArgs e)
		//{
		//  ArrayList attrs = new ArrayList();
		//  attrs.Add(new AttributeValuePair("cmd_type", "rebuild_project"));
		//  attrs.Add(new AttributeValuePair("nodename", ChosenProject_NodeName));
		//  new Node(queueNode, "task", "", attrs);
		//  _mainPanel.DisposeEntryPanel();
		//}

		private void newBtnAddIterativeRequest_Click(object sender, System.EventArgs e)
		{
			ArrayList attrs = new ArrayList();
			attrs.Add(new AttributeValuePair("cmd_type", "rebuild_project"));
			attrs.Add(new AttributeValuePair("nodename", ChosenProject_NodeName));
			attrs.Add(new AttributeValuePair("cmd_action", "add_cycle"));
			new Node(queueNode, "task", "", attrs);
			_mainPanel.DisposeEntryPanel();
		}
		private void newBtnClearIterativeRequest_Click(object sender, System.EventArgs e)
		{
			ArrayList attrs = new ArrayList();
			attrs.Add(new AttributeValuePair("cmd_type", "rebuild_project"));
			attrs.Add(new AttributeValuePair("nodename", ChosenProject_NodeName));
			attrs.Add(new AttributeValuePair("cmd_action", "clear_cycle"));
			new Node(queueNode, "task", "", attrs);
			_mainPanel.DisposeEntryPanel();
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

				if (inTestMode)
				{
					errorPanel.Controls.Clear();
					int y = 0;

					foreach (string errorString in errs)
					{
						Label error = new Label ();
						error.Font = MyDefaultSkinFontBold10;
						error.Text = errorString;

						error.Location = new Point (0, y);
						error.Size = new Size (errorPanel.Width, (int) Math.Ceiling(error.Font.GetHeight()));
						y = error.Bottom;

						errorPanel.Controls.Add(error);
					}

					newBtnOK.Enabled = false;
				}
				else
				{
					string disp = "";
					foreach (string err in errs)
					{
						disp += err + System.Environment.NewLine;
					}

					MessageBox.Show(disp, "Error");
				}
			}
		}

		private void newBtnCancel_Click(object sender, System.EventArgs e)
		{
			_mainPanel.DisposeEntryPanel();
		}

		private void newBtnProject_Click(object sender, System.EventArgs e)
		{
			//Go back to choosing which Project
			this.pnl_OptionChoice.Visible = false;
			this.pnl_ChooseSlot.Visible = true;
			projectLabel.Visible = false;
			newBtnProject.Visible = false;
			newBtnOK.Visible = false;
		}

		public void SetFocus ()
		{
			Focus();
			locTextBox.Focus();
		}
	}
}