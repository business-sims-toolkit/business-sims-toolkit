using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Data;
using System.Windows.Forms;
using System.Xml;
using System.Globalization;
using System.Drawing.Text;
using System.IO;
using System.Runtime.InteropServices;

using LibCore;
using Network;
using CoreUtils;
using CommonGUI;
using Polestar_PM.DataLookup;

namespace Polestar_PM.OpsGUI
{
	/// <summary>
	/// This is the user interface that provides the staff resource editor panel for all the stages
	/// We handle the stages with arrays of user interface elements (addressed by the stage) 
	/// </summary>
	public class ProjectResourceEditorPanel : FlickerFreePanel
	{

		public enum emDisplayStages
		{
			stageA=0,
			stageB,
			stageC,
			stageD,
			stageE,
			stageF,
			stageG,
			stageH
		}

		public static string GetStageNameFromDisplayStage (emDisplayStages displayStage)
		{
			switch (displayStage)
			{
				case emDisplayStages.stageA:
					return "A";

				case emDisplayStages.stageB:
					return "B";

				case emDisplayStages.stageC:
					return "C";

				case emDisplayStages.stageD:
					return "D";

				case emDisplayStages.stageE:
					return "E";

				case emDisplayStages.stageF:
					return "F";

				case emDisplayStages.stageG:
					return "G";

				case emDisplayStages.stageH:
					return "H";
			}

			return "";
		}

		private char c1 = (char)8;	//back
		protected Font MyDefaultSkinFontNormal8 = null;
		protected Font MyDefaultSkinFontNormal10 = null;
		protected Font MyDefaultSkinFontNormal12 = null;
		protected Font MyDefaultSkinFontBold8 = null;
		protected Font MyDefaultSkinFontBold10 = null;
		protected Font MyDefaultSkinFontBold12 = null;

		protected Hashtable displayStageLetters = new Hashtable();

		//Text Entry boxes for all stages (both Internal and External Staff) 
		private System.Windows.Forms.TextBox[] tbStaffInt = new TextBox[8];
		private System.Windows.Forms.TextBox[] tbStaffExt = new TextBox[8];

		//The stage titles and the top of each column
		private System.Windows.Forms.Label[] lblStageTitles = new Label[8];
		//The predicted days at the bottom of each column (may be hidden)
		private System.Windows.Forms.Label[] lblStage_PredictedDays = new Label[8];
		private System.Windows.Forms.Label lblStage_Total_Days;

		private System.Windows.Forms.TextBox tbProjectBudget;
		private System.Windows.Forms.TextBox tbDelayedStartDay;

		private System.Windows.Forms.Label lblDelayedStartDay;
		private System.Windows.Forms.Label lblInternalStaffTitle;
		private System.Windows.Forms.Label lblContractersTitle;
		
		private System.Windows.Forms.Label lblPhaseDesignTitle;
		private System.Windows.Forms.Label lblPhaseBuildTitle;
		private System.Windows.Forms.Label lblPhaseTestTitle;
		private System.Windows.Forms.Label lblDaysTitle;
		private System.Windows.Forms.Label lblTotalTitle;
		private System.Windows.Forms.Label lblBudgetTitle;
		
		work_stage[] stage_tasks = new work_stage[8];

		private int DesignTeamLimit =0;
		private int BuildTeamLimit =0;
		private int TestTeamLimit =0;
		private bool UseDataEntryChecks = false;

		string projectTerm;

		NodeTree myNodeTree = null;
		int current_round = 1;
		bool hide_predicted_days_override = false;
		bool Allow_ResourceReduction_inCurrentStage = false;
		bool showDelayedStartOption = false;


		public ProjectResourceEditorPanel (NodeTree model, int round)
		{
			current_round = round;
			myNodeTree = model;
			string fontname =  SkinningDefs.TheInstance.GetData("fontname");
			MyDefaultSkinFontNormal8 = ConstantSizeFont.NewFont(fontname,8);
			MyDefaultSkinFontNormal10 = ConstantSizeFont.NewFont(fontname,10);
			MyDefaultSkinFontNormal12 = ConstantSizeFont.NewFont(fontname,12);
			MyDefaultSkinFontBold8 = ConstantSizeFont.NewFont(fontname,8,FontStyle.Bold);
			MyDefaultSkinFontBold10 = ConstantSizeFont.NewFont(fontname,10,FontStyle.Bold);
			MyDefaultSkinFontBold12 = ConstantSizeFont.NewFont(fontname,12,FontStyle.Bold);

			projectTerm = SkinningDefs.TheInstance.GetData("project_term_round_" + LibCore.CONVERT.ToStr(round), "Project");

			//Wether we should show the Predicted Days underneath each stage (based on tasks and people)
			hide_predicted_days_override = true;
			//Hide the predicted days for each stage in Round 1 
			if (current_round > 1)
			{
				hide_predicted_days_override = false;
				showDelayedStartOption = true;
			}
			//check the skin file for the override 
			string show_predicted_days_in_round1_str = SkinningDefs.TheInstance.GetData("show_predicted_days_in_round1");
			if (show_predicted_days_in_round1_str.ToLower().IndexOf("true") > -1)
			{
				hide_predicted_days_override = false;
			}

			displayStageLetters.Clear();
			displayStageLetters.Add(emDisplayStages.stageA, "A");
			displayStageLetters.Add(emDisplayStages.stageB, "B");
			displayStageLetters.Add(emDisplayStages.stageC, "C");
			displayStageLetters.Add(emDisplayStages.stageD, "D");
			displayStageLetters.Add(emDisplayStages.stageE, "E");
			displayStageLetters.Add(emDisplayStages.stageF, "F");
			displayStageLetters.Add(emDisplayStages.stageG, "G");
			displayStageLetters.Add(emDisplayStages.stageH, "H");

			BuildControls();
		}

		new public void Dispose()
		{
			myNodeTree = null;
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

		public void SetAllowResourceReductionInCurrentStage()
		{
			Allow_ResourceReduction_inCurrentStage = true;
		}

		public void Build_Label(System.Windows.Forms.Label lbl, string txt, int x, int y, int w, int h, int tabindex) 
		{
			lbl.Font = this.MyDefaultSkinFontNormal8;
			lbl.Location = new System.Drawing.Point(x,y);
			lbl.Size = new System.Drawing.Size(w, h);
			//lbl.TabIndex = tabindex;
			lbl.Text = txt;
			lbl.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		}

		public void BuildControls()
		{
			//starying point for the stage controls 
			int xoffset = 80;

			foreach (emDisplayStages item in Enum.GetValues(typeof(emDisplayStages)))
			{
				this.tbStaffInt[(int)item] = new System.Windows.Forms.TextBox();
				this.tbStaffExt[(int)item] = new System.Windows.Forms.TextBox();
			}

			this.tbProjectBudget = new System.Windows.Forms.TextBox();

			this.lblInternalStaffTitle = new System.Windows.Forms.Label();
			this.lblContractersTitle = new System.Windows.Forms.Label();

			foreach (emDisplayStages item in Enum.GetValues(typeof(emDisplayStages)))
			{
				this.lblStageTitles[(int)item] = new System.Windows.Forms.Label();
			}

			this.lblPhaseDesignTitle = new System.Windows.Forms.Label();
			this.lblPhaseBuildTitle = new System.Windows.Forms.Label();
			this.lblPhaseTestTitle = new System.Windows.Forms.Label();
			this.lblBudgetTitle = new System.Windows.Forms.Label();
			this.lblDaysTitle = new System.Windows.Forms.Label();
			this.lblTotalTitle = new System.Windows.Forms.Label();

			foreach (emDisplayStages item in Enum.GetValues(typeof(emDisplayStages)))
			{
				this.lblStage_PredictedDays[(int)item] = new System.Windows.Forms.Label();
			}

			this.lblStage_Total_Days = new System.Windows.Forms.Label();

			//top sections
			this.lblPhaseDesignTitle.Font = this.MyDefaultSkinFontBold10;
			this.lblPhaseDesignTitle.Location = new System.Drawing.Point(80, 0);
			this.lblPhaseDesignTitle.Name = "label4";
			this.lblPhaseDesignTitle.Size = new System.Drawing.Size(119, 18);
			//this.lblPhaseDesignTitle.TabIndex = 66;
			this.lblPhaseDesignTitle.Text = "Design";
			this.lblPhaseDesignTitle.BackColor = Color.Silver;
			this.lblPhaseDesignTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

			this.lblPhaseBuildTitle.Font = this.MyDefaultSkinFontBold10;
			this.lblPhaseBuildTitle.Location = new System.Drawing.Point(200, 0);
			this.lblPhaseBuildTitle.Name = "lblBuildTitle";
			this.lblPhaseBuildTitle.Size = new System.Drawing.Size(79, 18);
			//this.lblPhaseBuildTitle.TabIndex = 67;
			this.lblPhaseBuildTitle.Text = "Build";
			this.lblPhaseBuildTitle.BackColor = Color.Silver;
			this.lblPhaseBuildTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

			this.lblPhaseTestTitle.Font = this.MyDefaultSkinFontBold10;
			this.lblPhaseTestTitle.Location = new System.Drawing.Point(280, 0);
			this.lblPhaseTestTitle.Name = "lblInternalStaffTitle3";
			this.lblPhaseTestTitle.Size = new System.Drawing.Size(120, 18);
			//this.lblPhaseTestTitle.TabIndex = 68;
			this.lblPhaseTestTitle.Text = "Test";
			this.lblPhaseTestTitle.BackColor = Color.Silver;
			this.lblPhaseTestTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

			this.lblTotalTitle.Font = this.MyDefaultSkinFontBold10;
			this.lblTotalTitle.Location = new System.Drawing.Point(400, 0);
			this.lblTotalTitle.Name = "lblInternalStaffTitle3";
			this.lblTotalTitle.Size = new System.Drawing.Size(40, 18);
			//this.lblTotalTitle.TabIndex = 68;
			this.lblTotalTitle.Text = "Total";
			this.lblTotalTitle.BackColor = Color.Silver;
			this.lblTotalTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

			//side labels
			this.lblInternalStaffTitle.Font = this.MyDefaultSkinFontNormal8;
			this.lblInternalStaffTitle.Location = new System.Drawing.Point(0+6, 50-12);
			this.lblInternalStaffTitle.Name = "lblInternalStaffTitle";
			this.lblInternalStaffTitle.Size = new System.Drawing.Size(60, 20);
			//this.lblInternalStaffTitle.TabIndex = 61;
			this.lblInternalStaffTitle.SendToBack();
			this.lblInternalStaffTitle.Text = "Internal";

			this.lblContractersTitle.Font = this.MyDefaultSkinFontNormal8;
			this.lblContractersTitle.Location = new System.Drawing.Point(0+6, 80-12);
			this.lblContractersTitle.Name = "lblContractersTitle";
			this.lblContractersTitle.Size = new System.Drawing.Size(80, 20);
			//this.lblContractersTitle.TabIndex = 62;
			this.lblContractersTitle.SendToBack();
			this.lblContractersTitle.Text = "Contractors";

			this.lblDaysTitle.Font = this.MyDefaultSkinFontNormal8;
			this.lblDaysTitle.Location = new System.Drawing.Point(0+6, 100-7);
			this.lblDaysTitle.Name = "lblContractersTitle";
			this.lblDaysTitle.Size = new System.Drawing.Size(80, 20);
			//this.lblDaysTitle.TabIndex = 62;
			this.lblDaysTitle.SendToBack();
			this.lblDaysTitle.Text = "Days";

			//altering the display for later rounds where the predicted days are important 
			if (this.hide_predicted_days_override == true)
			{
				//hide the Days 
				this.lblDaysTitle.Visible = false;
				this.lblTotalTitle.Visible = false;
			}
			else
			{
				this.lblDaysTitle.Visible = true;
				this.lblTotalTitle.Visible = true;
			}

			//Stages Titles
			Build_Label(this.lblStageTitles[(int)emDisplayStages.stageA], "A", xoffset + 40 * 0, 30 - 12, 40, 20, 53);
			Build_Label(this.lblStageTitles[(int)emDisplayStages.stageB], "B", xoffset + 40 * 1, 30 - 12, 40, 20, 60);
			Build_Label(this.lblStageTitles[(int)emDisplayStages.stageC], "C", xoffset + 40 * 2, 30 - 12, 40, 20, 59);
			Build_Label(this.lblStageTitles[(int)emDisplayStages.stageD], "D", xoffset + 40 * 3, 30 - 12, 40, 20, 58);
			Build_Label(this.lblStageTitles[(int)emDisplayStages.stageE], "E", xoffset + 40 * 4, 30 - 12, 40, 20, 57);
			Build_Label(this.lblStageTitles[(int)emDisplayStages.stageF], "F", xoffset + 40 * 5, 30 - 12, 40, 20, 56);
			Build_Label(this.lblStageTitles[(int)emDisplayStages.stageG], "G", xoffset + 40 * 6, 30 - 12, 40, 20, 55);
			Build_Label(this.lblStageTitles[(int)emDisplayStages.stageH], "H", xoffset + 40 * 7, 30 - 12, 40, 20, 54); 

			//Stages Days
			int step = 0;
			foreach (emDisplayStages item in Enum.GetValues(typeof(emDisplayStages)))
			{
				this.lblStage_PredictedDays[(int)item].Font = MyDefaultSkinFontNormal8;
				this.lblStage_PredictedDays[(int)item].Location = new System.Drawing.Point(xoffset + 40 * step, 90);
				this.lblStage_PredictedDays[(int)item].Name = "lblStage_A_Days";
				this.lblStage_PredictedDays[(int)item].Size = new System.Drawing.Size(40, 20);
				//this.lblStage_PredictedDays[(int)item].TabIndex = 53;
				this.lblStage_PredictedDays[(int)item].Text = item.ToString();
				this.lblStage_PredictedDays[(int)item].TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
				this.lblStage_PredictedDays[(int)item].Visible = !hide_predicted_days_override;
				step++;
			}

			step = 0;
			int newTabIndex = 1;
			foreach (emDisplayStages item in Enum.GetValues(typeof(emDisplayStages)))
			{
				this.tbStaffInt[(int)item].BackColor = System.Drawing.Color.Black;
				this.tbStaffInt[(int)item].Font = this.MyDefaultSkinFontBold8;
				this.tbStaffInt[(int)item].ForeColor = System.Drawing.Color.White;
				this.tbStaffInt[(int)item].Location = new System.Drawing.Point(xoffset + 40 * step, 50 - 12);
				this.tbStaffInt[(int)item].Name = "";
				this.tbStaffInt[(int)item].Size = new System.Drawing.Size(40, 21);
				this.tbStaffInt[(int)item].TabIndex = newTabIndex;
				this.tbStaffInt[(int)item].Text = "";
				this.tbStaffInt[(int)item].MaxLength = 2;
				this.tbStaffInt[(int)item].TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
				this.tbStaffInt[(int)item].KeyDown += new System.Windows.Forms.KeyEventHandler(this.MyTB_KeyDown);
				this.tbStaffInt[(int)item].KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.MyTB_KeyPress);
				this.tbStaffInt[(int)item].LostFocus +=new EventHandler(tb_LostFocus);
				newTabIndex++;

				this.tbStaffExt[(int)item].BackColor = System.Drawing.Color.Black;
				this.tbStaffExt[(int)item].Font = this.MyDefaultSkinFontBold8;
				this.tbStaffExt[(int)item].ForeColor = System.Drawing.Color.White;
				this.tbStaffExt[(int)item].Location = new System.Drawing.Point(xoffset + 40 * step, 80 - 12);
				this.tbStaffExt[(int)item].Name = "";
				this.tbStaffExt[(int)item].Size = new System.Drawing.Size(40, 21);
				this.tbStaffExt[(int)item].TabIndex = newTabIndex;
				this.tbStaffExt[(int)item].Text = "";
				this.tbStaffExt[(int)item].MaxLength = 2;
				this.tbStaffExt[(int)item].TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
				this.tbStaffExt[(int)item].KeyDown += new System.Windows.Forms.KeyEventHandler(this.MyTB_KeyDown);
				this.tbStaffExt[(int)item].KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.MyTB_KeyPress);
				this.tbStaffExt[(int)item].LostFocus += new EventHandler(tb_LostFocus);
				//System.Diagnostics.Debug.WriteLine("TEXT BOX :"+CONVERT.ToStr(((int)item)));
				step++;
				newTabIndex++;
			}

			int offset = 0;
			if (showDelayedStartOption)
			{
				offset = -170;
			}

			this.lblBudgetTitle.Font = this.MyDefaultSkinFontNormal10;
			this.lblBudgetTitle.Location = new System.Drawing.Point(180 + offset, 110 + 10);
			this.lblBudgetTitle.Name = "lblBudgetTitle";
			this.lblBudgetTitle.Size = new System.Drawing.Size(150, 20);
			//this.lblBudgetTitle.TabIndex = 71;
			this.lblBudgetTitle.Text = projectTerm + " Budget ($K):";
			this.lblBudgetTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

			this.tbProjectBudget.BackColor = System.Drawing.Color.Black;
			this.tbProjectBudget.Font = this.MyDefaultSkinFontBold10;
			this.tbProjectBudget.ForeColor = System.Drawing.Color.White;
			this.tbProjectBudget.Location = new System.Drawing.Point(330 + offset, 110 + 10);
			this.tbProjectBudget.Name = "tbProjectBudget";
			this.tbProjectBudget.Size = new System.Drawing.Size(50, 22);
			this.tbProjectBudget.TabIndex = newTabIndex;
			this.tbProjectBudget.Text = "0";
			this.tbProjectBudget.MaxLength = 4;
			this.tbProjectBudget.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
			this.tbProjectBudget.KeyDown += new System.Windows.Forms.KeyEventHandler(this.MyTB_KeyDown);
			this.tbProjectBudget.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.MyTB_KeyPress);
			newTabIndex++;
			
			this.lblDelayedStartDay = new System.Windows.Forms.Label();
			this.lblDelayedStartDay.BackColor = System.Drawing.Color.Transparent;
			this.lblDelayedStartDay.Font = MyDefaultSkinFontNormal10;
			this.lblDelayedStartDay.ForeColor = System.Drawing.Color.Black;
			this.lblDelayedStartDay.Location = new System.Drawing.Point(210, 110 + 10);
			this.lblDelayedStartDay.Name = "lblDelayedStartDay";
			this.lblDelayedStartDay.Size = new System.Drawing.Size(140, 20);
			//this.lblDelayedStartDay.TabIndex = 11;
			this.lblDelayedStartDay.Text = "Delay Start to Day";
			this.lblDelayedStartDay.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.lblDelayedStartDay.Visible = showDelayedStartOption;
			
			this.tbDelayedStartDay = new FilteredTextBox(TextBoxFilterType.Digits);
			this.tbDelayedStartDay.BackColor = System.Drawing.Color.Black;
			this.tbDelayedStartDay.Font = this.MyDefaultSkinFontBold10;
			this.tbDelayedStartDay.ForeColor = System.Drawing.Color.White;
			this.tbDelayedStartDay.Location = new System.Drawing.Point(350, 110 + 10);
			this.tbDelayedStartDay.Name = "locDelayedStart";
			this.tbDelayedStartDay.Size = new System.Drawing.Size(50, 22);
			if (showDelayedStartOption)
			{
				this.tbDelayedStartDay.TabIndex = newTabIndex;
			}
			this.tbDelayedStartDay.Text = "0";
			this.tbDelayedStartDay.Visible = showDelayedStartOption;
			this.tbDelayedStartDay.MaxLength = 2;
			this.tbDelayedStartDay.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
			newTabIndex++;

			foreach (emDisplayStages item in Enum.GetValues(typeof(emDisplayStages)))
			{
				this.Controls.Add(this.lblStage_PredictedDays[(int)item]);
			}

			this.Controls.Add(this.lblInternalStaffTitle);
			this.Controls.Add(this.lblContractersTitle);

			foreach (emDisplayStages item in Enum.GetValues(typeof(emDisplayStages)))
			{
				this.Controls.Add(this.lblStageTitles[(int)item]);
			}

			this.Controls.Add(this.lblPhaseDesignTitle);
			this.Controls.Add(this.lblPhaseBuildTitle);
			this.Controls.Add(this.lblPhaseTestTitle);
			this.Controls.Add(this.lblBudgetTitle);
			this.Controls.Add(this.lblDaysTitle);
			this.Controls.Add(this.lblTotalTitle);
			this.Controls.Add(this.lblDelayedStartDay);

			foreach (emDisplayStages item in Enum.GetValues(typeof(emDisplayStages)))
			{
				this.Controls.Add(this.tbStaffInt[(int)item]);
				this.Controls.Add(this.tbStaffExt[(int)item]);
			}

			this.Controls.Add(this.tbProjectBudget);
			this.Controls.Add(this.tbDelayedStartDay);

			//UI Adjustment
			//this.conA.BringToFront();
			tbStaffExt[(int)emDisplayStages.stageA].BringToFront();
		}

		private bool RecalculatePredictedDays(work_stage ws, int staff_int_count, int staff_ext_count, 
			out int predicted_days)
		{
			bool display_days = true;
			//the calculations are handled by the work_stage 
			if (ws.isStateDone())
			{
				predicted_days = ws.getCompletedDaysFromNode();
			}
			else
			{
				ws.RecalculatePredictedDaysWithDefinedStaff(false, staff_int_count, staff_ext_count, out predicted_days);
			}
			return display_days;
		}

		private void tb_LostFocus(object sender, EventArgs e)
		{
			handleDaysDisplays();
		}

		public void handleDaysDisplays()
		{
			int predicted_days=0;
			//show or hide the predicted days based on the switch 
			foreach (emDisplayStages item in Enum.GetValues(typeof(emDisplayStages)))
			{
				this.lblStage_PredictedDays[(int)item].Visible = !hide_predicted_days_override;
			}

			if (hide_predicted_days_override==false)
			{
				//If we are showing predicted days, then calculate them 
				foreach (emDisplayStages item in Enum.GetValues(typeof(emDisplayStages)))
				{
					int staff_internal = CONVERT.ParseIntSafe(tbStaffInt[(int)item].Text, 0);
					int staff_external = CONVERT.ParseIntSafe(tbStaffExt[(int)item].Text, 0);
					if (RecalculatePredictedDays(stage_tasks[(int)item],
								staff_internal, staff_external, out predicted_days))
					{
						this.lblStage_PredictedDays[(int)item].Text = CONVERT.ToStr(predicted_days);
					}
				}
			}
		}

		private void setTextBoxEnablement(bool isInProgress, bool isCompleted, 
			System.Windows.Forms.TextBox tb1, System.Windows.Forms.TextBox tb2, work_stage workStage)
		{
			int numberOfSubTasks;

			if (workStage.DoWeConsistOnlyOfSequentialSubtasks(out numberOfSubTasks))
			{
				tb1.Enabled = false;
				tb2.Enabled = false;
			}
			else if (isCompleted)
			{
				tb1.Enabled = false;
				tb2.Enabled = false;
			}
			else
			{
				tb1.Enabled = true;
				tb2.Enabled = true;
			}
		}

		public void setDefaultResourceLevels(int [] internals, int [] externals, bool [] locked)
		{
			for (int i = 0; i < internals.Length; i++)
			{
				tbStaffInt[i].Text = CONVERT.ToStr(internals[i]);
			}

			for (int i = 0; i < externals.Length; i++)
			{
				tbStaffExt[i].Text = CONVERT.ToStr(externals[i]);
			}

			for (int i = 0; i < locked.Length; i++)
			{
				tbStaffInt[i].Enabled = ! locked[i];
				tbStaffExt[i].Enabled = ! locked[i];
			}
		}

		public void setProjectTaskLengthsDirect(work_stage stage_a,	work_stage stage_b, work_stage stage_c,
			work_stage stage_d, work_stage stage_e, work_stage stage_f, work_stage stage_g, work_stage stage_h)
		{
			stage_tasks[(int)emDisplayStages.stageA] = stage_a;
			stage_tasks[(int)emDisplayStages.stageB] = stage_b;
			stage_tasks[(int)emDisplayStages.stageC] = stage_c;
			stage_tasks[(int)emDisplayStages.stageD] = stage_d;
			stage_tasks[(int)emDisplayStages.stageE] = stage_e;
			stage_tasks[(int)emDisplayStages.stageF] = stage_f;
			stage_tasks[(int)emDisplayStages.stageG] = stage_g;
			stage_tasks[(int)emDisplayStages.stageH] = stage_h;
		}

		public void LoadDataIntoControls(Node projectnode)
		{
			int requested_int=0;
			int requested_ext=0;
			bool isInProgress = false;
			bool isCompleted = false;
			
			//int tmpTaskTotalCount = 0; 
			//int tmpDroppedTaskTotalCount = 0; 
			//int tmpRemainingTaskCount = 0; 

			if (projectnode != null)
			{
				ProjectReader pr = new ProjectReader(projectnode);
				if (pr != null)
				{

					DesignTeamLimit = 0;
					BuildTeamLimit = 0;
					TestTeamLimit = 0;

					pr.getProjectStaffLimits(out DesignTeamLimit, out BuildTeamLimit, out TestTeamLimit);

					//work_stage ws = new work_stage();
					stage_tasks[(int)emDisplayStages.stageA] = pr.getWorkStageCloneForStage(emProjectOperationalState.PROJECT_STATE_A);
					stage_tasks[(int)emDisplayStages.stageB] = pr.getWorkStageCloneForStage(emProjectOperationalState.PROJECT_STATE_B);
					stage_tasks[(int)emDisplayStages.stageC] = pr.getWorkStageCloneForStage(emProjectOperationalState.PROJECT_STATE_C);
					stage_tasks[(int)emDisplayStages.stageD] = pr.getWorkStageCloneForStage(emProjectOperationalState.PROJECT_STATE_D);
					stage_tasks[(int)emDisplayStages.stageE] = pr.getWorkStageCloneForStage(emProjectOperationalState.PROJECT_STATE_E);
					stage_tasks[(int)emDisplayStages.stageF] = pr.getWorkStageCloneForStage(emProjectOperationalState.PROJECT_STATE_F);
					stage_tasks[(int)emDisplayStages.stageG] = pr.getWorkStageCloneForStage(emProjectOperationalState.PROJECT_STATE_G);
					stage_tasks[(int)emDisplayStages.stageH] = pr.getWorkStageCloneForStage(emProjectOperationalState.PROJECT_STATE_H);

					//Extract the budget (mind the conversion to $K)
					int Budget = pr.getPlayerDefinedBudget();
					Budget = Budget / 1000;
					tbProjectBudget.Text = CONVERT.ToStr(Budget);

					int delayed_start_day = pr.getDelayedStartDay();
					this.tbDelayedStartDay.Text = CONVERT.ToStr(delayed_start_day);
					this.tbDelayedStartDay.Enabled = pr.isStateBeforeStateA();

					//for all the stages 
					foreach (emDisplayStages item in Enum.GetValues(typeof(emDisplayStages)))
					{
						//Extract the levels and progress and set the labels and enablement
						stage_tasks[(int)item].getRequestedResourceLevels(
							out requested_int, out requested_ext, out isInProgress, out isCompleted);
						tbStaffInt[(int)item].Text = CONVERT.ToStr(requested_int);
						tbStaffExt[(int)item].Text = CONVERT.ToStr(requested_ext);
						tbStaffInt[(int)item].Tag = requested_int;
						tbStaffExt[(int)item].Tag = requested_ext;
						this.lblStageTitles[(int)item].Tag = isInProgress;
						setTextBoxEnablement(isInProgress, isCompleted,
							tbStaffInt[(int)item], tbStaffExt[(int)item], stage_tasks[(int) item]);
					}
				}
			}
		}

		public void ExtractDataFromControls(out int budget, out int delayed_start_days, 
			out int resIntA, out int resExtA, out int resIntB, out int resExtB, 
			out int resIntC, out int resExtC, out int resIntD, out int resExtD,
			out int resIntE, out int resExtE, out int resIntF, out int resExtF, 
			out int resIntG, out int resExtG,	out int resIntH, out int resExtH)
		{

			//Mind that display is in $K but internally we use $ units
			int newbudget = CONVERT.ParseIntSafe(this.tbProjectBudget.Text,0);
			budget = newbudget * 1000;

			delayed_start_days = CONVERT.ParseIntSafe(this.tbDelayedStartDay.Text, 0);

			resIntA = CONVERT.ParseIntSafe(tbStaffInt[(int)emDisplayStages.stageA].Text, 0);
			resExtA = CONVERT.ParseIntSafe(tbStaffExt[(int)emDisplayStages.stageA].Text, 0);
			resIntB = CONVERT.ParseIntSafe(tbStaffInt[(int)emDisplayStages.stageB].Text, 0);
			resExtB = CONVERT.ParseIntSafe(tbStaffExt[(int)emDisplayStages.stageB].Text, 0);
			resIntC = CONVERT.ParseIntSafe(tbStaffInt[(int)emDisplayStages.stageC].Text, 0);
			resExtC = CONVERT.ParseIntSafe(tbStaffExt[(int)emDisplayStages.stageC].Text, 0);
			resIntD = CONVERT.ParseIntSafe(tbStaffInt[(int)emDisplayStages.stageD].Text, 0);
			resExtD = CONVERT.ParseIntSafe(tbStaffExt[(int)emDisplayStages.stageD].Text, 0);
			resIntE = CONVERT.ParseIntSafe(tbStaffInt[(int)emDisplayStages.stageE].Text, 0);
			resExtE = CONVERT.ParseIntSafe(tbStaffExt[(int)emDisplayStages.stageE].Text, 0);
			resIntF = CONVERT.ParseIntSafe(tbStaffInt[(int)emDisplayStages.stageF].Text, 0);
			resExtF = CONVERT.ParseIntSafe(tbStaffExt[(int)emDisplayStages.stageF].Text, 0);
			resIntG = CONVERT.ParseIntSafe(tbStaffInt[(int)emDisplayStages.stageG].Text, 0);
			resExtG = CONVERT.ParseIntSafe(tbStaffExt[(int)emDisplayStages.stageG].Text, 0);
			resIntH = CONVERT.ParseIntSafe(tbStaffInt[(int)emDisplayStages.stageH].Text, 0);
			resExtH = CONVERT.ParseIntSafe(tbStaffExt[(int)emDisplayStages.stageH].Text, 0);
		}

		private bool checkForLowerCurrentStage(System.Windows.Forms.Label stageLbl, 
			System.Windows.Forms.TextBox intTextBox, System.Windows.Forms.TextBox extTextBox,	out string errmsg)
		{
			bool errflag = false;
			errmsg = "";

			if (Allow_ResourceReduction_inCurrentStage == false)
			{
				if (stageLbl.Tag != null)
				{
					Boolean isCurrentStage = (Boolean)stageLbl.Tag;
					if (isCurrentStage == true)
					{
						int previousIntLevel = (int)intTextBox.Tag;
						int previousExtLevel = (int)extTextBox.Tag;
						int newIntLevel = CONVERT.ParseIntSafe(intTextBox.Text, 0);
						int newExtLevel = CONVERT.ParseIntSafe(extTextBox.Text, 0);
						if ((previousIntLevel > newIntLevel) | (previousExtLevel > newExtLevel))
						{
							errflag = true;
							errmsg = "Can't reduce staff level in Current Stage";
						}
					}
				}
			}
			return errflag;
		}

		public bool CheckOrder(bool checkProjectBudgetAlteration, bool checkBudgetCreation, 
			int pmo_budgettotal, int pmo_budgetspent, int pmo_budgetleft, 
			int project_budget_total, int project_budget_left, int project_budget_spent, 
			int globalStaffLimit_DevInt, int globalStaffLimit_DevExt, 
			int globalStaffLimit_TestInt, int globalStaffLimit_TestExt, 
			out ArrayList errs)
		{
			bool good_order = true;
			errs = new ArrayList();
			string errmsg1;

			//Mind that display is in $K
			int newbudget = CONVERT.ParseIntSafe(this.tbProjectBudget.Text,0);
			newbudget = newbudget * 1000;

			int delayed_start_days = CONVERT.ParseIntSafe(this.tbDelayedStartDay.Text, 0);

			//check that the entered delay start day (if enabled) is OK compared to the current day
			if (tbDelayedStartDay.Enabled == true)
			{ 
				//if the text box is alterable then check that we are not entering a invalid day
				Node CurrDayNode = this.myNodeTree.GetNamedNode("CurrentDay");
				int current_day = CurrDayNode.GetIntAttribute("day", 0);
				int entered_day = delayed_start_days;

				if (current_day != 0)
				{

					if (entered_day != 0)
					{
						if (entered_day <= current_day)
						{
							errs.Add("Project delayed start day in the past");
							good_order = false;
						}
					}
				}
			}

			//WE are checking a modification to an existing project budget 
			if (checkProjectBudgetAlteration)
			{
				if (project_budget_total<newbudget)
				{
					//the user is trying to increase the project budget 
					//check that we can fund the increase from PMO 
					int increase_required = newbudget - project_budget_total;
					if (increase_required>pmo_budgetleft)
					{
						errs.Add("Not enough left in PMO to fund project budget increase");
						good_order = false;
					}
				}
				if (project_budget_total>newbudget) 
				{
					//the user is trying to decrease the project budget 
					//check that we arnt try to decrease below the currently spent mark
					if (newbudget<project_budget_spent)
					{
						errs.Add("Cannot reduce budget to below current spend");
						good_order = false;
					}
				}
			}

			if (checkBudgetCreation)
			{
				if (newbudget>pmo_budgetleft)
				{
					errs.Add("Not enough left in PMO to fund project budget");
					good_order = false;
				}
			}

			//===================================================================================================
			//Check over the staff levels (within the limits)
			//===================================================================================================
			foreach (emDisplayStages item in Enum.GetValues(typeof(emDisplayStages)))
			{
				int staff_internal = CONVERT.ParseIntSafe(tbStaffInt[(int)item].Text, 0);
				int staff_external = CONVERT.ParseIntSafe(tbStaffExt[(int)item].Text, 0);
				string stgLetter = (string) displayStageLetters[item];

				if (staff_internal>globalStaffLimit_DevInt)
				{
					errs.Add("Too many staff requested in Stage "+stgLetter+" Internal");
					good_order = false;			
				}
				else
				{
					if (this.UseDataEntryChecks)
					{
						if (staff_internal>DesignTeamLimit)
						{
							errs.Add("More than Design Team Staff limit requested in Stage "+stgLetter+" Internal");
							good_order = false;			
						}	
					}
				}

				if (staff_external>globalStaffLimit_DevExt)
				{
					errs.Add("Too many staff requested in Stage "+stgLetter+" External");
					good_order = false;			
				}
				else
				{
					if (this.UseDataEntryChecks)
					{
						if (staff_external>DesignTeamLimit)
						{
							errs.Add("More than Design Team Staff limit requested in Stage "+stgLetter+" Internal");
							good_order = false;			
						}	
					}
				}

				if (checkForLowerCurrentStage(this.lblStageTitles[(int)item],
					tbStaffInt[(int)item], tbStaffExt[(int)item], out errmsg1))
				{
					errs.Add(errmsg1);
					good_order = false;
				}
			}
			return good_order;
		}

		#region Text Box Handlers

		private Boolean CustomHandleKeyPress(System.Windows.Forms.TextBox tb, char KeyChar, Boolean IsBackspace)
		{
			double cv = char.GetNumericValue(KeyChar);
			string str2 = string.Empty;
			Boolean OpHandled = false;
			string defaultStr = "0";

			if (char.IsDigit(KeyChar))
			{
				str2 = tb.Text;
				OpHandled = false;
			}
			else
			{
				if (KeyChar == 8) 
				{
					if (tb.Text.Length >1)
					{
						int ss = tb.SelectionStart;
						int sl = tb.SelectionLength;
						int tl = tb.Text.Length;
						Boolean processed = false;
						string str = tb.Text;
						if (sl==0)
						{
							if (tl==ss)
							{
								//Deleting the last single Character  
								tb.Text = str.Substring(0,str.Length-1);
								tb.SelectionStart = ss-1;
								tb.SelectionLength = 0;
								processed= true;
							}
							if (ss==0)
							{
								//Deleting the first single Character  
								//This actually has no action 
								//Deleting the first Character  
								//textBox2.Text = str.Substring(1,str.Length);
								//textBox2.SelectionStart = 0;
								processed= true;
							}
							if(processed == false)
							{
								//deleting a middle single character
								tb.Text = str.Substring(0,ss-1);
								tb.Text += str.Substring(ss,(str.Length)-ss);
								tb.SelectionStart = ss-1;
								tb.SelectionLength = 0;
								processed= true;
							}
						}
						else
						{
							//Deleting the full string (multiple Characters)
							if (sl>=tl)
							{
								tb.Text = defaultStr;
								tb.SelectionStart = 1;
								tb.SelectionLength = 1;
							}
							else
							{
								//Deleting multiple Characters but not the full string
								tb.Text = str.Substring(0,ss);
								tb.Text += str.Substring(ss+sl,(str.Length)-ss-sl);
								tb.SelectionStart = ss;
								tb.SelectionLength = 0;
							}
						}
						OpHandled = true;
					}
					else
					{
						//Handling the action on a single character
						tb.Text = defaultStr;
						tb.SelectionStart = 0;
						tb.SelectionLength = 1;
						OpHandled = true;
						//						OpHandled = false;
						//						if (IsBackspace == false)
						//						{
						//							OpHandled = true;
						//						}
					}
				}
				else
				{
					OpHandled = true;
				}
			}
			return OpHandled;
		}

		private void MyTB_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			int cc = e.KeyValue;
			if ((e.KeyCode == Keys.Delete))
			{
				e.Handled = CustomHandleKeyPress((TextBox)sender, c1, false);
			}
		}

		private void MyTB_KeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e)
		{
			e.Handled = CustomHandleKeyPress((TextBox) sender, e.KeyChar, true);
			return;
		}

		#endregion Text Box Handlers

		public void SetFocus ()
		{
			TextBox [] boxes = new TextBox [] { 
				tbStaffInt[(int)emDisplayStages.stageA], tbStaffInt[(int)emDisplayStages.stageB],
				tbStaffInt[(int)emDisplayStages.stageC], tbStaffInt[(int)emDisplayStages.stageD],
				tbStaffInt[(int)emDisplayStages.stageE], tbStaffInt[(int)emDisplayStages.stageF],
				tbStaffInt[(int)emDisplayStages.stageG], tbStaffInt[(int)emDisplayStages.stageH],
				tbProjectBudget };

			Focus();
			foreach (TextBox box in boxes)
			{
				if (box.Enabled)
				{
					box.Focus();
					return;
				}
			}
		}
	}
}