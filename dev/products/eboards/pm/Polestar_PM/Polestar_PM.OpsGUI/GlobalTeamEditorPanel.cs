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

namespace Polestar_PM.OpsGUI
{
	/// <summary>
	/// This panel alows the players to see the Resources for each Phase (Design,Build,Test) across all projects
	/// This allows the players to edit the resources give to each phase overall projects
	/// </summary>
	public class GlobalTeamEditorPanel : FlickerFreePanel
	{
		protected NodeTree MyNodeTree;
		protected Node projectsNode;
		protected char c1 = (char)8;	//back space
		protected int currentround = 1;

		protected Font MyDefaultSkinFontNormal8 = null;
		protected Font MyDefaultSkinFontNormal10 = null;
		protected Font MyDefaultSkinFontNormal12 = null;
		protected Font MyDefaultSkinFontBold8 = null;
		protected Font MyDefaultSkinFontBold10 = null;
		protected Font MyDefaultSkinFontBold12 = null;

		private System.Windows.Forms.Label lblFixTeam_DesignTeamLabel;
		private System.Windows.Forms.Label lblFixTeam_BuildTeamLabel;
		private System.Windows.Forms.Label lblFixTeam_TestTeamLabel;
		private System.Windows.Forms.Label[] lblFixTeam_PrjSlots; 
		private System.Windows.Forms.TextBox[] tbFixTeam_DesignTeamSizePrjs;
		private System.Windows.Forms.TextBox[] tbFixTeam_BuildTeamSizePrjs;
		private System.Windows.Forms.TextBox[] tbFixTeam_TestTeamSizePrjs;

		private int globalStaffLimit_DevInt =0;
		private int globalStaffLimit_DevExt =0;
		private int globalStaffLimit_TestInt =0;
		private int globalStaffLimit_TestExt =0;

		private int globalStaffLimit_TotalDev =0;
		private int globalStaffLimit_TotalTest =0;
		
		public GlobalTeamEditorPanel(NodeTree tree, int round)
		{
			MyNodeTree = tree;
			currentround = round;
			projectsNode = tree.GetNamedNode("pm_projects_running");

			string fontname =  SkinningDefs.TheInstance.GetData("fontname");
			MyDefaultSkinFontNormal8 = ConstantSizeFont.NewFont(fontname,8);
			MyDefaultSkinFontNormal10 = ConstantSizeFont.NewFont(fontname,10);
			MyDefaultSkinFontNormal12 = ConstantSizeFont.NewFont(fontname,12);
			MyDefaultSkinFontBold8 = ConstantSizeFont.NewFont(fontname,8,FontStyle.Bold);
			MyDefaultSkinFontBold10 = ConstantSizeFont.NewFont(fontname,10,FontStyle.Bold);
			MyDefaultSkinFontBold12 = ConstantSizeFont.NewFont(fontname,12,FontStyle.Bold);

			BuildControls();
		}

		new public void Dispose()
		{
			MyNodeTree = null;
			projectsNode = null;

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

		public void BuildControls()
		{
			lblFixTeam_DesignTeamLabel = new System.Windows.Forms.Label();
			lblFixTeam_DesignTeamLabel.Font = this.MyDefaultSkinFontBold10;
			lblFixTeam_DesignTeamLabel.Location = new System.Drawing.Point(0, 30);
			lblFixTeam_DesignTeamLabel.Name = "lblPrjSlot1";
			lblFixTeam_DesignTeamLabel.Size = new System.Drawing.Size(130, 24);
			lblFixTeam_DesignTeamLabel.TabIndex = 33;
			lblFixTeam_DesignTeamLabel.Text = "Design Team :";
			lblFixTeam_DesignTeamLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			lblFixTeam_DesignTeamLabel.BackColor = Color.LightBlue;
			lblFixTeam_DesignTeamLabel.BackColor = Color.FromArgb(218,218,203);
			this.Controls.Add(lblFixTeam_DesignTeamLabel);

			lblFixTeam_BuildTeamLabel = new System.Windows.Forms.Label();
			lblFixTeam_BuildTeamLabel.Font = this.MyDefaultSkinFontBold10;
			lblFixTeam_BuildTeamLabel.Location = new System.Drawing.Point(0, 60);
			lblFixTeam_BuildTeamLabel.Name = "lblPrjSlot1";
			lblFixTeam_BuildTeamLabel.Size = new System.Drawing.Size(130, 24);
			lblFixTeam_BuildTeamLabel.TabIndex = 33;
			lblFixTeam_BuildTeamLabel.Text = "Build Team :";
			lblFixTeam_BuildTeamLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			lblFixTeam_BuildTeamLabel.BackColor = Color.LightBlue;
			lblFixTeam_BuildTeamLabel.BackColor = Color.FromArgb(218,218,203);
			this.Controls.Add(lblFixTeam_BuildTeamLabel);

			lblFixTeam_TestTeamLabel = new System.Windows.Forms.Label();
			lblFixTeam_TestTeamLabel.Font = this.MyDefaultSkinFontBold10;
			lblFixTeam_TestTeamLabel.Location = new System.Drawing.Point(0, 90);
			lblFixTeam_TestTeamLabel.Name = "lblPrjSlot1";
			lblFixTeam_TestTeamLabel.Size = new System.Drawing.Size(130, 24);
			lblFixTeam_TestTeamLabel.TabIndex = 33;
			lblFixTeam_TestTeamLabel.Text = "Test Team :";
			lblFixTeam_TestTeamLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			lblFixTeam_TestTeamLabel.BackColor = Color.LightBlue;
			lblFixTeam_TestTeamLabel.BackColor =  Color.FromArgb(218,218,203);
			this.Controls.Add(lblFixTeam_TestTeamLabel);
			
			int offset_x=130;
			int offset_y=0;
			int item_width=45;
			int item_height=22;
			int item_sep_x=58; //includes width
			int item_sep_y=40; //includes height

			lblFixTeam_PrjSlots = new System.Windows.Forms.Label[6];
			tbFixTeam_DesignTeamSizePrjs = new System.Windows.Forms.TextBox[6];
			tbFixTeam_BuildTeamSizePrjs = new System.Windows.Forms.TextBox[6];
			tbFixTeam_TestTeamSizePrjs = new System.Windows.Forms.TextBox[6];
			
			int tab_count=49;

			for (int step=0; step<6; step++)
			{
				//Sort out the top Label 
				lblFixTeam_PrjSlots[step] = new System.Windows.Forms.Label();
				lblFixTeam_PrjSlots[step].Font = this.MyDefaultSkinFontBold10;
				lblFixTeam_PrjSlots[step].Location = new System.Drawing.Point(offset_x+(item_sep_x)*step, offset_y+item_sep_y*0);
				lblFixTeam_PrjSlots[step].Name = "lblPrjSlot"+(step+1).ToString();
				lblFixTeam_PrjSlots[step].Size = new System.Drawing.Size(item_width, item_height);
				//lblFixTeam_PrjSlots[step].TabIndex = 22;
				lblFixTeam_PrjSlots[step].Text = CONVERT.ToStr(step+1);
				lblFixTeam_PrjSlots[step].Tag = null;
				lblFixTeam_PrjSlots[step].TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
				this.Controls.Add(lblFixTeam_PrjSlots[step]);

				//Sort out the top Label 
				tbFixTeam_DesignTeamSizePrjs[step] = new System.Windows.Forms.TextBox(); 
				tbFixTeam_DesignTeamSizePrjs[step].BackColor = System.Drawing.Color.Black;
				tbFixTeam_DesignTeamSizePrjs[step].Font = this.MyDefaultSkinFontBold10;
				tbFixTeam_DesignTeamSizePrjs[step].ForeColor = System.Drawing.Color.White;
				tbFixTeam_DesignTeamSizePrjs[step].Location = new System.Drawing.Point(offset_x+(item_sep_x)*step, offset_y+30);
				tbFixTeam_DesignTeamSizePrjs[step].Name = "tbDevTeamSizePrj5";
				tbFixTeam_DesignTeamSizePrjs[step].Size = new System.Drawing.Size(45, 22);
				tbFixTeam_DesignTeamSizePrjs[step].TabIndex = tab_count;
				tbFixTeam_DesignTeamSizePrjs[step].Text = "0";
				tbFixTeam_DesignTeamSizePrjs[step].TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
				tbFixTeam_DesignTeamSizePrjs[step].KeyDown += new System.Windows.Forms.KeyEventHandler(this.MyTB_KeyDown);
				tbFixTeam_DesignTeamSizePrjs[step].KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.MyTB_KeyPress);
				tbFixTeam_DesignTeamSizePrjs[step].Enabled = false;
				tbFixTeam_DesignTeamSizePrjs[step].MaxLength = 2;
				this.Controls.Add(tbFixTeam_DesignTeamSizePrjs[step]);

				tab_count++;

				//Sort out the mid Label 
				tbFixTeam_BuildTeamSizePrjs[step] = new System.Windows.Forms.TextBox(); 
				tbFixTeam_BuildTeamSizePrjs[step].BackColor = System.Drawing.Color.Black;
				tbFixTeam_BuildTeamSizePrjs[step].Font = this.MyDefaultSkinFontBold10;
				tbFixTeam_BuildTeamSizePrjs[step].ForeColor = System.Drawing.Color.White;
				tbFixTeam_BuildTeamSizePrjs[step].Location = new System.Drawing.Point(offset_x+(item_sep_x)*step, offset_y+60);
				tbFixTeam_BuildTeamSizePrjs[step].Name = "tbDevTeamSizePrj5";
				tbFixTeam_BuildTeamSizePrjs[step].Size = new System.Drawing.Size(45, 22);
				tbFixTeam_BuildTeamSizePrjs[step].TabIndex = tab_count;
				tbFixTeam_BuildTeamSizePrjs[step].Text = "0";
				tbFixTeam_BuildTeamSizePrjs[step].TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
				tbFixTeam_BuildTeamSizePrjs[step].KeyDown += new System.Windows.Forms.KeyEventHandler(this.MyTB_KeyDown);
				tbFixTeam_BuildTeamSizePrjs[step].KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.MyTB_KeyPress);
				tbFixTeam_BuildTeamSizePrjs[step].Enabled = false;
				tbFixTeam_BuildTeamSizePrjs[step].MaxLength = 2;
				this.Controls.Add(tbFixTeam_BuildTeamSizePrjs[step]);

				tab_count++;
				//Sort out the top Label 
				tbFixTeam_TestTeamSizePrjs[step] = new System.Windows.Forms.TextBox(); 
				tbFixTeam_TestTeamSizePrjs[step].BackColor = System.Drawing.Color.Black;
				tbFixTeam_TestTeamSizePrjs[step].Font = this.MyDefaultSkinFontBold10;
				tbFixTeam_TestTeamSizePrjs[step].ForeColor = System.Drawing.Color.White;
				tbFixTeam_TestTeamSizePrjs[step].Location = new System.Drawing.Point(offset_x+(item_sep_x)*step, offset_y+90);
				tbFixTeam_TestTeamSizePrjs[step].Name = "tbDevTeamSizePrj5";
				tbFixTeam_TestTeamSizePrjs[step].Size = new System.Drawing.Size(45, 22);
				tbFixTeam_TestTeamSizePrjs[step].TabIndex = tab_count++;
				tbFixTeam_TestTeamSizePrjs[step].Text = "0";
				tbFixTeam_TestTeamSizePrjs[step].TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
				tbFixTeam_TestTeamSizePrjs[step].KeyDown += new System.Windows.Forms.KeyEventHandler(this.MyTB_KeyDown);
				tbFixTeam_TestTeamSizePrjs[step].KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.MyTB_KeyPress);
				tbFixTeam_TestTeamSizePrjs[step].Enabled = false;
				tbFixTeam_TestTeamSizePrjs[step].MaxLength = 2;
				this.Controls.Add(tbFixTeam_TestTeamSizePrjs[step]);
			}
		}

		public void setGlobalLimits(int limitDevInt, int limitDevExt, int limitTestInt, int limitTestExt) 
		{
			globalStaffLimit_DevInt = limitDevInt;
			globalStaffLimit_DevExt = limitDevExt;
			globalStaffLimit_TestInt = limitTestInt;
			globalStaffLimit_TestExt = limitTestExt;

			globalStaffLimit_TotalDev = globalStaffLimit_DevInt + globalStaffLimit_DevExt;
			globalStaffLimit_TotalTest = globalStaffLimit_TestInt + globalStaffLimit_TestExt;
		}

		public void FillControls()
		{
			foreach (Node prjnode in projectsNode.getChildren())
			{
				string project_id = prjnode.GetAttribute("project_id");
				int project_slot_id = prjnode.GetIntAttribute("slot",0);
				int project_design_team_size = prjnode.GetIntAttribute("design_reslevel",0);
				int project_build_team_size = prjnode.GetIntAttribute("build_reslevel",0);
				int project_test_team_size = prjnode.GetIntAttribute("test_reslevel",0);

				if ((project_slot_id>-1)&(project_slot_id<tbFixTeam_TestTeamSizePrjs.Length))
				{
					lblFixTeam_PrjSlots[project_slot_id].Text = project_id;
					lblFixTeam_PrjSlots[project_slot_id].Tag = prjnode;
					tbFixTeam_DesignTeamSizePrjs[project_slot_id].Text = project_design_team_size.ToString();
					tbFixTeam_BuildTeamSizePrjs[project_slot_id].Text = project_build_team_size.ToString();
					tbFixTeam_TestTeamSizePrjs[project_slot_id].Text = project_test_team_size.ToString();
		
					tbFixTeam_DesignTeamSizePrjs[project_slot_id].Enabled = true;
					tbFixTeam_BuildTeamSizePrjs[project_slot_id].Enabled = true;
					tbFixTeam_TestTeamSizePrjs[project_slot_id].Enabled = true;
				}
			}
		}


		public bool checkData(out ArrayList errs)
		{
			bool proceed = true;
			errs = new ArrayList();

			int request_design_total = 0; 
			int request_build_total = 0; 
			int request_test_total = 0; 
			int requestDesign = 0;  
			int requestBuild = 0;  
			int requestTest = 0;  

			//we only need to check the enabled ones
			for (int step=0; step<6; step++)
			{
				if (tbFixTeam_DesignTeamSizePrjs[step].Enabled)
				{
					requestDesign = CONVERT.ParseIntSafe(tbFixTeam_DesignTeamSizePrjs[step].Text,0);
					requestBuild = CONVERT.ParseIntSafe(tbFixTeam_BuildTeamSizePrjs[step].Text,0);
					requestTest = CONVERT.ParseIntSafe(tbFixTeam_TestTeamSizePrjs[step].Text,0);

					request_design_total += requestDesign;
					request_build_total += requestBuild;
					request_test_total +=  requestTest;
				}
			}

			if (request_design_total>globalStaffLimit_TotalDev)
			{
				proceed = false;
				errs.Add ("Requested Design Resource total is higher than limit (>"+CONVERT.ToStr(globalStaffLimit_TotalDev)+"");
			}
			if (request_build_total>globalStaffLimit_TotalDev)
			{
				proceed = false;
				errs.Add ("Requested Build Resource total is higher than limit (>"+CONVERT.ToStr(globalStaffLimit_TotalDev)+"");
			}
			if (request_test_total>globalStaffLimit_TotalTest)
			{
				proceed = false;
				errs.Add ("Requested Test Resource total is higher than limit (>"+CONVERT.ToStr(globalStaffLimit_TotalTest)+"");
			}
			return proceed;
		}

		/// <summary>
		/// really dumb extraction and command but dead simple 
		/// </summary>
		/// <param name="queueNode"></param>
		public bool HandleRequest(Node queueNode)
		{
			bool proceed = true;

			int arg_slot1_prjnumber=-1;
			int arg_slot1_design_team_size=-1;
			int arg_slot1_build_team_size=-1;
			int arg_slot1_test_team_size=-1;
			int arg_slot2_prjnumber=-1;
			int arg_slot2_design_team_size=-1;
			int arg_slot2_build_team_size=-1;
			int arg_slot2_test_team_size=-1;
			int arg_slot3_prjnumber=-1;
			int arg_slot3_design_team_size=-1;
			int arg_slot3_build_team_size=-1;
			int arg_slot3_test_team_size=-1;
			int arg_slot4_prjnumber=-1;
			int arg_slot4_design_team_size=-1;
			int arg_slot4_build_team_size=-1;
			int arg_slot4_test_team_size=-1;
			int arg_slot5_prjnumber=-1;
			int arg_slot5_design_team_size=-1;
			int arg_slot5_build_team_size=-1;
			int arg_slot5_test_team_size=-1;
			int arg_slot6_prjnumber=-1;
			int arg_slot6_design_team_size=-1;
			int arg_slot6_build_team_size=-1;
			int arg_slot6_test_team_size=-1;

			if (tbFixTeam_DesignTeamSizePrjs[0].Enabled)
			{
				arg_slot1_prjnumber = CONVERT.ParseIntSafe(lblFixTeam_PrjSlots[0].Text,-1);
				arg_slot1_design_team_size = CONVERT.ParseIntSafe(tbFixTeam_DesignTeamSizePrjs[0].Text,-1);
				arg_slot1_build_team_size = CONVERT.ParseIntSafe(tbFixTeam_BuildTeamSizePrjs[0].Text,-1);
				arg_slot1_test_team_size = CONVERT.ParseIntSafe(tbFixTeam_TestTeamSizePrjs[0].Text,-1);
			}
			if (tbFixTeam_DesignTeamSizePrjs[1].Enabled)
			{
				arg_slot2_prjnumber = CONVERT.ParseIntSafe(lblFixTeam_PrjSlots[1].Text,-1);
				arg_slot2_design_team_size = CONVERT.ParseIntSafe(tbFixTeam_DesignTeamSizePrjs[1].Text,-1);
				arg_slot2_build_team_size = CONVERT.ParseIntSafe(tbFixTeam_BuildTeamSizePrjs[1].Text,-1);
				arg_slot2_test_team_size = CONVERT.ParseIntSafe(tbFixTeam_TestTeamSizePrjs[1].Text,-1);
			}
			if (tbFixTeam_DesignTeamSizePrjs[2].Enabled)
			{
				arg_slot3_prjnumber = CONVERT.ParseIntSafe(lblFixTeam_PrjSlots[2].Text,-1);
				arg_slot3_design_team_size = CONVERT.ParseIntSafe(tbFixTeam_DesignTeamSizePrjs[2].Text,-1);
				arg_slot3_build_team_size = CONVERT.ParseIntSafe(tbFixTeam_BuildTeamSizePrjs[2].Text,-1);
				arg_slot3_test_team_size = CONVERT.ParseIntSafe(tbFixTeam_TestTeamSizePrjs[2].Text,-1);
			}

			if (tbFixTeam_DesignTeamSizePrjs[3].Enabled)
			{
				arg_slot4_prjnumber = CONVERT.ParseIntSafe(lblFixTeam_PrjSlots[3].Text,-1);
				arg_slot4_design_team_size = CONVERT.ParseIntSafe(tbFixTeam_DesignTeamSizePrjs[3].Text,-1);
				arg_slot4_build_team_size = CONVERT.ParseIntSafe(tbFixTeam_BuildTeamSizePrjs[3].Text,-1);
				arg_slot4_test_team_size = CONVERT.ParseIntSafe(tbFixTeam_TestTeamSizePrjs[3].Text,-1);
			}
			if (tbFixTeam_DesignTeamSizePrjs[4].Enabled)
			{
				arg_slot5_prjnumber = CONVERT.ParseIntSafe(lblFixTeam_PrjSlots[4].Text,-1);
				arg_slot5_design_team_size = CONVERT.ParseIntSafe(tbFixTeam_DesignTeamSizePrjs[4].Text,-1);
				arg_slot5_build_team_size = CONVERT.ParseIntSafe(tbFixTeam_BuildTeamSizePrjs[4].Text,-1);
				arg_slot5_test_team_size = CONVERT.ParseIntSafe(tbFixTeam_TestTeamSizePrjs[4].Text,-1);
			}
			if (tbFixTeam_DesignTeamSizePrjs[5].Enabled)
			{
				arg_slot6_prjnumber = CONVERT.ParseIntSafe(lblFixTeam_PrjSlots[5].Text,-1);
				arg_slot6_design_team_size = CONVERT.ParseIntSafe(tbFixTeam_DesignTeamSizePrjs[5].Text,-1);
				arg_slot6_build_team_size = CONVERT.ParseIntSafe(tbFixTeam_BuildTeamSizePrjs[5].Text,-1);
				arg_slot6_test_team_size = CONVERT.ParseIntSafe(tbFixTeam_TestTeamSizePrjs[5].Text,-1);
			}

			if (proceed)
			{
				ArrayList attrs = new ArrayList ();
				attrs.Add(new AttributeValuePair ("cmd_type", "request_teamsize_change"));
				attrs.Add(new AttributeValuePair ("slot1_prjnumber", CONVERT.ToStr(arg_slot1_prjnumber)));
				attrs.Add(new AttributeValuePair ("slot1_design_team_size", CONVERT.ToStr(arg_slot1_design_team_size)));
				attrs.Add(new AttributeValuePair ("slot1_build_team_size", CONVERT.ToStr(arg_slot1_build_team_size)));
				attrs.Add(new AttributeValuePair ("slot1_test_team_size", CONVERT.ToStr(arg_slot1_test_team_size)));

				attrs.Add(new AttributeValuePair ("slot2_prjnumber", CONVERT.ToStr(arg_slot2_prjnumber)));
				attrs.Add(new AttributeValuePair ("slot2_design_team_size", CONVERT.ToStr(arg_slot2_design_team_size)));
				attrs.Add(new AttributeValuePair ("slot2_build_team_size", CONVERT.ToStr(arg_slot2_build_team_size)));
				attrs.Add(new AttributeValuePair ("slot2_test_team_size", CONVERT.ToStr(arg_slot2_test_team_size)));

				attrs.Add(new AttributeValuePair ("slot3_prjnumber", CONVERT.ToStr(arg_slot3_prjnumber)));
				attrs.Add(new AttributeValuePair ("slot3_design_team_size", CONVERT.ToStr(arg_slot3_design_team_size)));
				attrs.Add(new AttributeValuePair ("slot3_build_team_size", CONVERT.ToStr(arg_slot3_build_team_size)));
				attrs.Add(new AttributeValuePair ("slot3_test_team_size", CONVERT.ToStr(arg_slot3_test_team_size)));

				attrs.Add(new AttributeValuePair ("slot4_prjnumber", CONVERT.ToStr(arg_slot4_prjnumber)));
				attrs.Add(new AttributeValuePair ("slot4_design_team_size", CONVERT.ToStr(arg_slot4_design_team_size)));
				attrs.Add(new AttributeValuePair ("slot4_build_team_size", CONVERT.ToStr(arg_slot4_build_team_size)));
				attrs.Add(new AttributeValuePair ("slot4_test_team_size", CONVERT.ToStr(arg_slot4_test_team_size)));

				attrs.Add(new AttributeValuePair ("slot5_prjnumber", CONVERT.ToStr(arg_slot5_prjnumber)));
				attrs.Add(new AttributeValuePair ("slot5_design_team_size", CONVERT.ToStr(arg_slot5_design_team_size)));
				attrs.Add(new AttributeValuePair ("slot5_build_team_size", CONVERT.ToStr(arg_slot5_build_team_size)));
				attrs.Add(new AttributeValuePair ("slot5_test_team_size", CONVERT.ToStr(arg_slot5_test_team_size)));

				attrs.Add(new AttributeValuePair ("slot6_prjnumber", CONVERT.ToStr(arg_slot6_prjnumber)));
				attrs.Add(new AttributeValuePair ("slot6_design_team_size", CONVERT.ToStr(arg_slot6_design_team_size)));
				attrs.Add(new AttributeValuePair ("slot6_build_team_size", CONVERT.ToStr(arg_slot6_build_team_size)));
				attrs.Add(new AttributeValuePair ("slot6_test_team_size", CONVERT.ToStr(arg_slot6_test_team_size)));
				//attrs.Add(new AttributeValuePair ("day", day_number));
				new Node (queueNode, "task", "", attrs);	
			}
			return proceed;
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			Bitmap bmp = new Bitmap(this.Width,this.Height);
			Graphics g = Graphics.FromImage(bmp);

			//Extract from the normal background if we have one 
			g.SmoothingMode = SmoothingMode.AntiAlias;
			g.CompositingQuality = CompositingQuality.HighQuality;
			g.PixelOffsetMode = PixelOffsetMode.HighQuality;					

			//Extract from the normal background if we have one 
			//Brush GeneralBackBrush = new SolidBrush(BackColor);
			Brush GeneralBackBrush = new SolidBrush(Color.FromArgb(176,196,222));
			g.FillRectangle(GeneralBackBrush,0,0,this.Width,this.Height);
			GeneralBackBrush.Dispose();
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

//		private void tbFixTeam_TestTeamSizePrj1_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
//		{
//			int cc = e.KeyValue;
//			if ((e.KeyCode == Keys.Delete))
//			{
//				e.Handled = CustomHandleKeyPress(tbFixTeam_TestTeamSizePrj1, c1, false);
//			}		
//		}
//
//		private void tbTestTeamSizePrj1_KeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e)
//		{
//			e.Handled = CustomHandleKeyPress(tbFixTeam_TestTeamSizePrj1, e.KeyChar, true);
//			return;
//		}
//
//		private void tbFixTeam_DevTeamSizePrj2_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
//		{
//			int cc = e.KeyValue;
//			if ((e.KeyCode == Keys.Delete))
//			{
//				e.Handled = CustomHandleKeyPress(tbFixTeam_DevTeamSizePrj2, c1, false);
//			}				
//		}
//
//		private void tbFixTeam_DevTeamSizePrj2_KeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e)
//		{
//			e.Handled = CustomHandleKeyPress(tbFixTeam_DevTeamSizePrj2, e.KeyChar, true);
//			return;
//		}
//
//		private void tbFixTeam_TestTeamSizePrj2_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
//		{
//			int cc = e.KeyValue;
//			if ((e.KeyCode == Keys.Delete))
//			{
//				e.Handled = CustomHandleKeyPress(tbFixTeam_TestTeamSizePrj2, c1, false);
//			}						
//		}
//
//		private void tbFixTeam_TestTeamSizePrj2_KeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e)
//		{
//			e.Handled = CustomHandleKeyPress(tbFixTeam_TestTeamSizePrj2, e.KeyChar, true);
//			return;
//		}
//
//		private void tbFixTeam_DevTeamSizePrj3_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
//		{
//			int cc = e.KeyValue;
//			if ((e.KeyCode == Keys.Delete))
//			{
//				e.Handled = CustomHandleKeyPress(tbFixTeam_DevTeamSizePrj3, c1, false);
//			}						
//		}
//
//		private void tbFixTeam_DevTeamSizePrj3_KeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e)
//		{
//			e.Handled = CustomHandleKeyPress(tbFixTeam_DevTeamSizePrj3, e.KeyChar, true);
//			return;
//		}
//
//		private void tbFixTeam_TestTeamSizePrj3_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
//		{
//			int cc = e.KeyValue;
//			if ((e.KeyCode == Keys.Delete))
//			{
//				e.Handled = CustomHandleKeyPress(tbFixTeam_TestTeamSizePrj3, c1, false);
//			}						
//		}
//
//		private void tbFixTeam_TestTeamSizePrj3_KeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e)
//		{
//			e.Handled = CustomHandleKeyPress(tbFixTeam_TestTeamSizePrj3, e.KeyChar, true);
//			return;
//		}
//
//		private void tbFixTeam_DevTeamSizePrj4_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
//		{
//			int cc = e.KeyValue;
//			if ((e.KeyCode == Keys.Delete))
//			{
//				e.Handled = CustomHandleKeyPress(tbFixTeam_DevTeamSizePrj4, c1, false);
//			}						
//		}
//
//		private void tbFixTeam_DevTeamSizePrj4_KeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e)
//		{
//			e.Handled = CustomHandleKeyPress(tbFixTeam_DevTeamSizePrj4, e.KeyChar, true);
//			return;
//		}
//
//		private void tbFixTeam_TestTeamSizePrj4_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
//		{
//			int cc = e.KeyValue;
//			if ((e.KeyCode == Keys.Delete))
//			{
//				e.Handled = CustomHandleKeyPress(tbFixTeam_TestTeamSizePrj4, c1, false);
//			}						
//		}
//
//		private void tbFixTeam_TestTeamSizePrj4_KeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e)
//		{
//			e.Handled = CustomHandleKeyPress(tbFixTeam_TestTeamSizePrj4, e.KeyChar, true);
//			return;
//		}
//
//		private void tbFixTeam_DevTeamSizePrj5_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
//		{
//			int cc = e.KeyValue;
//			if ((e.KeyCode == Keys.Delete))
//			{
//				e.Handled = CustomHandleKeyPress(tbFixTeam_DevTeamSizePrj5, c1, false);
//			}						
//		}
//
//		private void tbFixTeam_DevTeamSizePrj5_KeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e)
//		{
//			e.Handled = CustomHandleKeyPress(tbFixTeam_DevTeamSizePrj5, e.KeyChar, true);
//			return;
//		}
//
//		private void tbFixTeam_TestTeamSizePrj5_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
//		{
//			int cc = e.KeyValue;
//			if ((e.KeyCode == Keys.Delete))
//			{
//				e.Handled = CustomHandleKeyPress(tbFixTeam_TestTeamSizePrj5, c1, false);
//			}						
//		}
//
//		private void tbFixTeam_TestTeamSizePrj5_KeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e)
//		{
//			e.Handled = CustomHandleKeyPress(tbFixTeam_TestTeamSizePrj5, e.KeyChar, true);
//			return;
//		}
//
//		private void tbFixTeam_DevTeamSizePrj6_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
//		{
//			int cc = e.KeyValue;
//			if ((e.KeyCode == Keys.Delete))
//			{
//				e.Handled = CustomHandleKeyPress(tbFixTeam_DevTeamSizePrj6, c1, false);
//			}						
//		}
//
//		private void tbFixTeam_DevTeamSizePrj6_KeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e)
//		{
//			e.Handled = CustomHandleKeyPress(tbFixTeam_DevTeamSizePrj6, e.KeyChar, true);
//			return;
//		}
//
//		private void tbFixTeam_TestTeamSizePrj6_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
//		{
//			int cc = e.KeyValue;
//			if ((e.KeyCode == Keys.Delete))
//			{
//				e.Handled = CustomHandleKeyPress(tbFixTeam_TestTeamSizePrj6, c1, false);
//			}	
//		}
//
//		private void tbFixTeam_TestTeamSizePrj6_KeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e)
//		{
//			e.Handled = CustomHandleKeyPress(tbFixTeam_TestTeamSizePrj6, e.KeyChar, true);
//			return;
//		}

		#endregion Text Box Handlers

	}
}
