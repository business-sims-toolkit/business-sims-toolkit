using System;
using System.Collections;

using System.Windows.Forms;
using System.Drawing;
using Network;

using LibCore;
using CoreUtils;

namespace CommonGUI
{
	public class AOSE_CritAdvOptionsPanel : FlickerFreePanel
	{
		NodeTree model;
		Node CriticalAdvantageRequestNode = null;
		
		IDataEntryControlHolder mainPanel;
		Color MyTitleForeColor, MyOperationsBackColor, MyGroupPanelBackColor;
		Font MyDefaultSkinFontNormal10, MyDefaultSkinFontBold10, MyDefaultSkinFontBold12, MyDefaultSkinFontBold24;
		bool IsTrainingMode;
		string fontname;
		Color upColor, downColor, hoverColor, disabledColor;
		int round;
		int display_items = 3;

		Label lbl_Panel_Title_IMVS;
		Label lbl_Panel_Title_CritAdvan;

		ImageTextButton cancelButton = null;
		ImageTextButton okButton = null; 

		ArrayList RBList = new ArrayList();
		Hashtable lookup = new Hashtable();

		public void ConfigRadioButton(ref RadioButton rb, Panel pnl_host, Font tmpFont, Color ForeCLR, Color BackCLR, 
			int x, int y, int Width, int Height, string txt, bool rb_checked, string tag)
		{
			rb = new RadioButton();
			rb.Font = tmpFont;
			rb.Checked = rb_checked;
			rb.Location = new Point(x, y);
			rb.Size = new Size(Width, Height);
			rb.Text = txt;
			rb.ForeColor = ForeCLR;
			rb.BackColor = BackCLR;
			rb.TextAlign = ContentAlignment.MiddleLeft;
			rb.Visible = true;
			rb.Tag = tag;
			pnl_host.Controls.Add(rb);
			rb.BringToFront();
		}

		public void ConfigLabel(ref Label lbl, Panel pnl_host, Font tmpFont, Color ForeCLR, Color BackCLR, 
			int x, int y, int Width, int Height, string txt)
		{
			lbl = new Label();
			lbl.Font = tmpFont;
			lbl.Location = new Point(x, y);
			lbl.Size = new Size(Width, Height);
			lbl.Text = txt;
			lbl.ForeColor = ForeCLR;
			lbl.BackColor = BackCLR;
			lbl.TextAlign = ContentAlignment.MiddleLeft;
			lbl.Visible = true;
			pnl_host.Controls.Add(lbl);
			lbl.BringToFront();
		}

		public AOSE_CritAdvOptionsPanel(IDataEntryControlHolder mainPanel, NodeTree model, Boolean IsTrainingMode,
			Color OperationsBackColor, Color GroupPanelBackColor, int round)
		{
			this.mainPanel = mainPanel;
			this.round = round;

			string racetitlecolour =  SkinningDefs.TheInstance.GetData("race_paneltitleforecolor");
			if (racetitlecolour != "")
			{
				MyTitleForeColor =  SkinningDefs.TheInstance.GetColorData("race_paneltitleforecolor");
			}

			CriticalAdvantageRequestNode = model.GetNamedNode("ca_requests");

			MyOperationsBackColor = OperationsBackColor;
			MyGroupPanelBackColor = GroupPanelBackColor;
			MyTitleForeColor = Color.White;

			fontname = SkinningDefs.TheInstance.GetData("fontname");
			MyDefaultSkinFontNormal10 = ConstantSizeFont.NewFont(fontname,10);
			MyDefaultSkinFontBold10 = ConstantSizeFont.NewFont(fontname,10,FontStyle.Bold);
			MyDefaultSkinFontBold12 = ConstantSizeFont.NewFont(fontname,12,FontStyle.Bold);
			MyDefaultSkinFontBold24 = ConstantSizeFont.NewFont(fontname,24,FontStyle.Bold);

			this.IsTrainingMode = IsTrainingMode;

			upColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_text_colour", Color.Black);
			downColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_down_text_colour", Color.White);
			hoverColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_hover_text_colour", Color.Green);
			disabledColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_disabled_text_colour", Color.DarkGray);

			SetStyle(ControlStyles.Selectable, true);

			
			if (IsTrainingMode)
			{
				BackgroundImage = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location +
					"\\images\\panels\\race_panel_back_training.png");
			}
			else
			{
				BackgroundImage = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location +
					"\\images\\panels\\race_panel_back_normal.png");
			}

			lbl_Panel_Title_CritAdvan = new Label();
			lbl_Panel_Title_CritAdvan.Text = "Mission Critical Services";
			lbl_Panel_Title_CritAdvan.Size = new Size(500, 20);
			lbl_Panel_Title_CritAdvan.Location = new Point(10, 10);
			lbl_Panel_Title_CritAdvan.Font = MyDefaultSkinFontBold12;
			lbl_Panel_Title_CritAdvan.BackColor = MyOperationsBackColor;
			lbl_Panel_Title_CritAdvan.ForeColor = MyTitleForeColor;
			Controls.Add(lbl_Panel_Title_CritAdvan);

			lbl_Panel_Title_IMVS = new Label();
			lbl_Panel_Title_IMVS.Text = "IMVS";
			lbl_Panel_Title_IMVS.Size = new Size(500, 20);
			lbl_Panel_Title_IMVS.Location = new Point(10, 10+100-20);
			lbl_Panel_Title_IMVS.Font = MyDefaultSkinFontBold12;
			lbl_Panel_Title_IMVS.BackColor = MyOperationsBackColor;
			lbl_Panel_Title_IMVS.ForeColor = MyTitleForeColor;
			Controls.Add(lbl_Panel_Title_IMVS);

			cancelButton = new ImageTextButton(0);
			cancelButton.ButtonFont = MyDefaultSkinFontBold10;
			cancelButton.SetVariants("/images/buttons/blank_small.png");
			cancelButton.Size = new Size(80,20);
			cancelButton.Location = new Point(510,185);
			cancelButton.SetButtonText("Close", upColor, upColor, hoverColor, disabledColor);
			cancelButton.Click +=cancelButton_Click;
			cancelButton.Visible = true;
			Controls.Add(cancelButton);

			okButton = new ImageTextButton(0);
			okButton.ButtonFont = MyDefaultSkinFontBold10;
			okButton.SetVariants("/images/buttons/blank_small.png");
			okButton.Size = new Size(80, 20);
			okButton.Location = new Point(510-100, 185);
			okButton.SetButtonText("OK", upColor, upColor, hoverColor, disabledColor);
			okButton.Click +=okButton_Click;
			okButton.Visible = true;
			Controls.Add(okButton);

			this.model = model;
			this.mainPanel = mainPanel;

			if (round > 1)
			{
				BuildControls();
			}
			else
			{
				lbl_Panel_Title_IMVS.Visible = false;

				Label lbl = new Label();
				ConfigLabel(ref lbl, this, MyDefaultSkinFontBold10, Color.White, Color.Transparent,
					20, 40, 500, 30, "Mission Critical Services not available in Round 1");

				if (okButton != null)
				{
					okButton.Visible = false;
				}

			}
		}

		protected void BuildControls()
		{
			int offset_x = 10;
			int offset_y = 40;

			int pos_x = offset_x;
			int pos_y = offset_y;
			int row_step = 30;
			int col_step = 110;
			int gap_step = 40;
			int title_width = 230;

			int[] display_points = new int[4];

			display_points[0] = 30;
			display_points[1] = 60;
			display_points[2] = 90+30-10;
			display_points[3] = 120+30;

			//Data Structures for layout in correct order 
			Hashtable AdminAreasLookup = new Hashtable();
			ArrayList AdminAreasKeys = new ArrayList();

			lookup.Clear();

			//build the Controls for the different Infrastructure systems 
			Node admin_areas = model.GetNamedNode("ca_admin_areas");
			if (admin_areas != null)
			{
				display_items = 0;
				foreach (Node tmpNode in admin_areas.getChildren())
				{
					string round_display = tmpNode.GetAttribute("round_display");
					if (round_display.IndexOf(CONVERT.ToStr(round)) > -1)
					{
						display_items++;
					}
				}
				if (display_items == 4)
				{
					display_points[0] = 30;
					display_points[1] = 60;
					display_points[2] = 90;

					display_points[3] = 120 + 30;
					lbl_Panel_Title_IMVS.Location = new Point(10, 10 + 100 + 20);
				}

				foreach (Node tmpNode in admin_areas.getChildren())
				{
					string name = tmpNode.GetAttribute("name");
					string round_display = tmpNode.GetAttribute("round_display");
					int item_seq = tmpNode.GetIntAttribute("seq",-1);

					bool show_item = false;

					if (round_display.IndexOf(CONVERT.ToStr(round)) > -1)
					{
						show_item = true;
					}

					if (show_item)
					{
						if (AdminAreasKeys.Contains(name) == false)
						{
							AdminAreasKeys.Add(name);
							AdminAreasLookup.Add(name, tmpNode);
						}
					}
				}
			}
			//Now layout the Controls in Order 
			AdminAreasKeys.Sort();
			int display_index = 0;
			foreach (string name in AdminAreasKeys)
			{
				Node tmpNode = (Node)AdminAreasLookup[name];
				if (tmpNode != null)
				{
					string title = tmpNode.GetAttribute("display_title");
					string allowed = tmpNode.GetAttribute("allowed_levels");
					string selected = tmpNode.GetAttribute("selected");
					bool add_gap = tmpNode.GetBooleanAttribute("add_gap", false);

					if (add_gap)
					{
						pos_y += gap_step;
					}

					Panel tmp = new Panel();
					tmp.BackColor = Color.Transparent;
					//tmp.Location = new Point(0, pos_y);
					tmp.Location = new Point(0, display_points[display_index]);

					tmp.Size = new Size(560, 30);
					Controls.Add(tmp);

					pos_x = offset_x;

					Label lbl = new Label();
					ConfigLabel(ref lbl, tmp, MyDefaultSkinFontBold10, Color.White, Color.Transparent, 
						pos_x, 0, title_width, 30, title);
					pos_x += title_width;

					string[] allowed_parts = allowed.Split(',');
					foreach (string option in allowed_parts)
					{
						string displayOption = "Level " + option;
						string displayOption_select = "l" + option+"_title";
						bool selected_flag = displayOption.IndexOf(selected)>-1;

						string option_title = tmpNode.GetAttribute(displayOption_select);

						RadioButton rb = new RadioButton();
                        ConfigRadioButton(ref rb, tmp, MyDefaultSkinFontBold10, Color.White, Color.Transparent,
							pos_x, 0, col_step+20, 30, option_title, selected_flag, title);
						RBList.Add(rb);

						//build decode table to convert back to support level selected 
						if (lookup.ContainsKey(option_title) == false)
						{
							lookup.Add(option_title, option);
						}

						pos_x = pos_x + (col_step + 10);
					}
					pos_y += row_step;

				}
				display_index++;
			}			
		}

		protected void okButton_Click(object sender, EventArgs args)
		{
			//process through the Radio buttons, building up what is selected 
			Hashtable settings = new Hashtable();
			foreach (RadioButton rb in RBList)
			{
				string title = (string)rb.Tag;
				string text = (string)rb.Text;
				string level = rb.Text;

				//can have space in attributes in the message node
				title = title.Replace(" ", "_");

				//strip out the prefix, leaving the number
				level = level.Replace("Level ","");

				//Use decode table to convert back to support level selected 
				if (lookup.ContainsKey(text))
				{
					level = (string)lookup[text];
				}

				if (rb.Checked)
				{
					if (settings.ContainsKey(title)==false)
					{
						settings.Add(title, level);
					}
				}
			}
			
			//Build up and send messages node 
			ArrayList attrs = new ArrayList ();
			attrs.Add(new AttributeValuePair ("action", "change_settings"));

			foreach (string key in settings.Keys)
			{
				string level = (string) settings[key];
				attrs.Add(new AttributeValuePair(key, level));
			}
			Node n2 = new Node(CriticalAdvantageRequestNode, "task", "", attrs);

			//it's just a signelling message node, added and handled by anybody that needs it
			//Since we built it, we can now delete it 
			if (n2 != null)
			{
				n2.Parent.DeleteChildTree(n2);
			}
			//Close the Panel
			mainPanel.DisposeEntryPanel();
		}

		protected void cancelButton_Click (object sender, EventArgs args)
		{
			//Close the Panel
			mainPanel.DisposeEntryPanel();
		}
		
	}
}