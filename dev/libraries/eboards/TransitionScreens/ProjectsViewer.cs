using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;

using LibCore;
using Network;
using CoreUtils;

using CommonGUI;

namespace TransitionScreens
{
	/// <summary>
	/// Summary description for ProjectsViewer.
	/// </summary>
	public class ProjectsViewer : FlickerFreePanel
	{
		NodeTree _NetworkModel;
		Node projectsNode;
		Node MyDevelopmentSpendNode;
		protected ArrayList ProjectPanels = new ArrayList();
		protected int CurrentRound = 1;
		protected Color MyCommonBackColor = Color.LightGray;
		Color MyPanelBackColor = Color.DarkGray;
		Color MyTextLabelBackColor = Color.DarkGray;
		
		protected Label lblCurrentSpendAmount_Value;
		protected Label lblActualCostAmount_Value;
		protected Label lblDevBudgetLeft_Value;
		protected Label lblCurrentSpendAmount_Title;
		protected Label lblActualCostAmount_Title;
		protected Label lblDevBudgetLeft_Title;

		protected int totalBudget;
		protected int budgetLeft;
		protected int totalSpend;
		protected int budgetAllocated;

		Image fsd = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\Images\\panels\\fsd.png");
		Image t_fsd = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\Images\\panels\\t_fsd.png");

		Font MyDefaultSkinFontBold9;
		protected int TotalLabelsOffsetFromBottom = 25;

	    Brush titleBrush = new SolidBrush(SkinningDefs.TheInstance.GetColorDataGivenDefault("transition_project_title_colour", SkinningDefs.TheInstance.GetColorDataGivenDefault("transition_text_colour", Color.DarkBlue)));
	    Brush headerBrush = new SolidBrush(SkinningDefs.TheInstance.GetColorDataGivenDefault("transition_project_header_colour", SkinningDefs.TheInstance.GetColorDataGivenDefault("transition_text_colour", Color.DarkBlue)));

        protected string panelTitle = "Service Portfolio - Service Pipeline";
		protected bool auto_translate = true;
		protected bool SelfDrawTranslatedTitle = false;
		protected Font titleFont = null;
		protected Font headerFont = null;
		protected bool SelfDrawTranslatedHeaders = false;

		protected string strHeader1= "Product";
		protected string strHeader2= "Design";
		protected string strHeader3= "Build";
		protected string strHeader4= "Test";
		protected string strHeader5= "Handover";
		protected string strHeader6= "Ready";
		protected string strHeader7= "Install";
		protected string strHeader8= "Spend";
		protected string strHeader9= "Budget";

		protected int strHeader1_offset = 10+60*0+52/2;
		protected int strHeader2_offset = 10+60*1+52/2;
		protected int strHeader3_offset = 10+60*2+52/2;
		protected int strHeader4_offset = 10+60*3+52/2;
		protected int strHeader5_offset = 10+60*4+52/2;
		protected int strHeader6_offset = 10+60*5+52/2;
		protected int strHeader7_offset = 10+60*6+52/2;
		protected int strHeader8_offset = 10+60*7+52/2;
		protected int strHeader9_offset = 10+60*8+52/2;

		public ProjectsViewer(NodeTree nt, int Round)
		{
			Setup(nt,Round,SkinningDefs.TheInstance.GetIntData("transition_projects_count", 4));
		}

		public ProjectsViewer(NodeTree nt, int Round, int num_shown)
		{
			Setup(nt,Round,num_shown);
		}

		protected virtual ProjectProgressPanelBase CreateProgressPanel(NodeTree nt, Node n, Color c)
		{
			return new ProjectProgressPanel(n,c);
		}

		protected virtual void Setup(NodeTree nt, int Round, int num_shown)
		{
			string fontname =  SkinningDefs.TheInstance.GetData("fontname");
			if (auto_translate)
			{
				fontname = TextTranslator.TheInstance.GetTranslateFont(fontname);
			}
			titleFont = ConstantSizeFont.NewFont(TextTranslator.TheInstance.GetTranslateFont(fontname), (float) SkinningDefs.TheInstance.GetDoubleData("transition_title_font_size", 12), FontStyle.Bold);
			headerFont = ConstantSizeFont.NewFont(TextTranslator.TheInstance.GetTranslateFont(fontname), 10f, FontStyle.Bold);
			MyDefaultSkinFontBold9 = ConstantSizeFont.NewFont(fontname,9f,FontStyle.Bold);

			SuspendLayout();

			CurrentRound = Round;
			MyCommonBackColor = SkinningDefs.TheInstance.GetColorData("transition_basebackgroundcolor");
			MyPanelBackColor = SkinningDefs.TheInstance.GetColorData("transition_groupbackcolor");
			//use a overriding text back color, if present and the panel back if not
			//most skins have the same color 
			MyTextLabelBackColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("transition_totalstextbackcolor",MyPanelBackColor);

			Size = new Size(561,281);

			if (SkinningDefs.TheInstance.GetIntData("transition_panels_transparent_edges", 0) == 1)
			{
				// We've asked to have a background colour as well as an image (useful when the
				// image has transparent edges).
				BackColor = Color.Transparent;
			}

			if (SkinningDefs.TheInstance.GetIntData("transition_panels_transparent_textback", 0) == 1)
			{
				MyTextLabelBackColor = Color.Transparent;
			}

			BackgroundImage = fsd;

			_NetworkModel = nt;

			projectsNode = _NetworkModel.GetNamedNode("Projects");
			MyDevelopmentSpendNode = _NetworkModel.GetNamedNode("DevelopmentSpend");
			int roundbudgetleft = MyDevelopmentSpendNode.GetIntAttribute("roundbudgetleft",0);
			int currentspendtotal = MyDevelopmentSpendNode.GetIntAttribute("currentspendtotal",0);
			int actualcosttotal = MyDevelopmentSpendNode.GetIntAttribute("actualcosttotal",0);

			foreach(Node n in projectsNode)
			{
				if (n.GetIntAttribute("createdinround", 0) == CurrentRound)
				{
					var panel = CreateProgressPanel(nt, n,MyCommonBackColor);
					Controls.Add(panel);
					ProjectPanels.Add(panel);
				}
			}
			// Fill in the empty slots.
			int numStillToAdd = num_shown - ProjectPanels.Count;
			while(numStillToAdd > 0)
			{
				ProjectProgressPanelBase p = CreateProgressPanel(nt, null,MyCommonBackColor);
				p.Name = "A Project Progress Panel";
				Controls.Add(p);
				ProjectPanels.Add(p);
				--numStillToAdd;
				//
			}

			ResumeLayout(false);

			BuildLabels();

			//Set up the display 		
			roundbudgetleft = roundbudgetleft / 1000;
			lblDevBudgetLeft_Value.Text = CONVERT.ToStr(roundbudgetleft);
			currentspendtotal = currentspendtotal / 1000;
			lblCurrentSpendAmount_Value.Text = CONVERT.ToStr(currentspendtotal);
			actualcosttotal = actualcosttotal / 1000;
			lblActualCostAmount_Value.Text = CONVERT.ToStr(actualcosttotal);

			projectsNode.ChildAdded += projectsNode_ChildAdded;
			projectsNode.ChildRemoved +=projectsNode_ChildRemoved;

			MyDevelopmentSpendNode.AttributesChanged +=MyDevelopmentSpendNode_AttributesChanged;

			SizeChanged += ProjectsViewer_Resize;

			DoSize();

			Invalidate();
		}

		protected virtual void LayOutProjects ()
		{
			DoSize();
		}

		public void setlabelOffsetFromBottom(int offset)
		{
			TotalLabelsOffsetFromBottom = offset;
		}
		
		public void RePositionLabels()
		{
			lblDevBudgetLeft_Title.Location = new Point(10, Height - TotalLabelsOffsetFromBottom);
			lblDevBudgetLeft_Value.Location = new Point(142-7, Height - TotalLabelsOffsetFromBottom);
			lblCurrentSpendAmount_Title.Location = new Point(253-10, Height - TotalLabelsOffsetFromBottom);
			lblCurrentSpendAmount_Value.Location = new Point(345-15, Height - TotalLabelsOffsetFromBottom);
			lblActualCostAmount_Title.Location = new Point(410-15, Height - TotalLabelsOffsetFromBottom);
			lblActualCostAmount_Value.Location = new Point(523-20, Height - TotalLabelsOffsetFromBottom);
		}

		void BuildLabels() 
		{
			SuspendLayout();
			string strDevBudgetLeft_Title = "Budget Left";
			string strCurrentSpendAmount_Title = "Spend";
			string strActualCostAmount_Title = "Allocated";

			MyTextLabelBackColor = Color.Transparent;

			if (auto_translate)
			{
				strDevBudgetLeft_Title = TextTranslator.TheInstance.Translate(strDevBudgetLeft_Title);
				strCurrentSpendAmount_Title =TextTranslator.TheInstance.Translate(strCurrentSpendAmount_Title);
				strActualCostAmount_Title =TextTranslator.TheInstance.Translate(strActualCostAmount_Title);
			}
			strDevBudgetLeft_Title = strDevBudgetLeft_Title + " ($K)";
			strCurrentSpendAmount_Title = strCurrentSpendAmount_Title+ " ($K)";
			strActualCostAmount_Title = strActualCostAmount_Title+ " ($K)"; 


			lblCurrentSpendAmount_Value = new Label();
			lblActualCostAmount_Value = new Label();
			lblDevBudgetLeft_Value = new Label();

			lblCurrentSpendAmount_Title = new Label();
			lblActualCostAmount_Title = new Label();
			lblDevBudgetLeft_Title = new Label();

			lblDevBudgetLeft_Title.Name = "lblDevBudgetLeft_Title";
			lblDevBudgetLeft_Title.Font = MyDefaultSkinFontBold9;
			lblDevBudgetLeft_Title.Size = new Size(125, 16);
			lblDevBudgetLeft_Title.Text = strDevBudgetLeft_Title;
            lblDevBudgetLeft_Title.BackColor = MyTextLabelBackColor;
            lblDevBudgetLeft_Title.ForeColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("transition_project_footer_colour", SkinningDefs.TheInstance.GetColorDataGivenDefault("transition_text_colour", Color.Black));
            Controls.Add(lblDevBudgetLeft_Title);

			lblDevBudgetLeft_Value.Name = "lblDevBudgetLeft_Value";
			lblDevBudgetLeft_Value.Font =  MyDefaultSkinFontBold9;
			lblDevBudgetLeft_Value.Size = new Size(40, 16);
			lblDevBudgetLeft_Value.Text = "0";
			lblDevBudgetLeft_Value.BackColor = MyTextLabelBackColor;
            lblDevBudgetLeft_Value.ForeColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("transition_project_footer_colour", SkinningDefs.TheInstance.GetColorDataGivenDefault("transition_text_colour", Color.Black));
            Controls.Add(lblDevBudgetLeft_Value);

			lblCurrentSpendAmount_Title.Name = "lblCurrentSpendAmount_Title";
			lblCurrentSpendAmount_Title.Font =  MyDefaultSkinFontBold9;
			lblCurrentSpendAmount_Title.Size = new Size(87, 16);
			lblCurrentSpendAmount_Title.Text = strCurrentSpendAmount_Title;
			lblCurrentSpendAmount_Title.BackColor = MyTextLabelBackColor;
            lblCurrentSpendAmount_Title.ForeColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("transition_project_footer_colour", SkinningDefs.TheInstance.GetColorDataGivenDefault("transition_text_colour", Color.Black));
            Controls.Add(lblCurrentSpendAmount_Title);

			lblCurrentSpendAmount_Value.Name = "lblCurrentSpendAmount_Value";
			lblCurrentSpendAmount_Value.Font =  MyDefaultSkinFontBold9;
			lblCurrentSpendAmount_Value.Size = new Size(40, 16);
			lblCurrentSpendAmount_Value.Text = "0";
			lblCurrentSpendAmount_Value.BackColor = MyTextLabelBackColor;
            lblCurrentSpendAmount_Value.ForeColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("transition_project_footer_colour", SkinningDefs.TheInstance.GetColorDataGivenDefault("transition_text_colour", Color.Black));
            Controls.Add(lblCurrentSpendAmount_Value);

			lblActualCostAmount_Title.Name = "lblActualCostAmount_Title";
			lblActualCostAmount_Title.Font =  MyDefaultSkinFontBold9;
			lblActualCostAmount_Title.Size = new Size(105, 16);
			lblActualCostAmount_Title.Text = strActualCostAmount_Title;
			lblActualCostAmount_Title.BackColor = MyTextLabelBackColor;
            lblActualCostAmount_Title.ForeColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("transition_project_footer_colour", SkinningDefs.TheInstance.GetColorDataGivenDefault("transition_text_colour", Color.Black));
            Controls.Add(lblActualCostAmount_Title);

			lblActualCostAmount_Value.Name = "lblActualCostAmount_Value";
			lblActualCostAmount_Value.Font =  MyDefaultSkinFontBold9;
			lblActualCostAmount_Value.Size = new Size(40, 16);
			lblActualCostAmount_Value.Text = "0";
			lblActualCostAmount_Value.TextAlign = ContentAlignment.MiddleRight;
			lblActualCostAmount_Value.BackColor = MyTextLabelBackColor;
            lblActualCostAmount_Value.ForeColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("transition_project_footer_colour", SkinningDefs.TheInstance.GetColorDataGivenDefault("transition_text_colour", Color.Black));
            Controls.Add(lblActualCostAmount_Value);

			RePositionLabels();

			ResumeLayout(false);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose(bool disposing)
		{
			if( disposing )
			{
				if (headerFont != null)
				{
					headerFont.Dispose();
					headerFont = null;
				}
				if (titleFont != null)
				{
					titleFont.Dispose();
					titleFont = null;
				}


				if(projectsNode != null)
				{
					projectsNode.ChildAdded -= projectsNode_ChildAdded;
					projectsNode = null;
				}
			}

			base.Dispose(disposing);
		}

		public virtual void SetTrainingMode(bool _is_training_mode)
		{
			if (_is_training_mode)
			{
				BackgroundImage = t_fsd;
			}
			else
			{
				BackgroundImage = fsd;
			}
		}

		void projectsNode_ChildAdded(Node sender, Node child)
		{
			//assuming child is the new project
			AddProjectNode(child);
		}

		protected virtual void AddProjectNode(Node child)
		{
			int index = 0;
			foreach(ProjectProgressPanelBase p in ProjectPanels)
			{
				if(p.getMonitoredProjectNode() == null)
				{
					if (child.GetIntAttribute("createdinround", 0) == CurrentRound)
					{
						// We can use this slot...
						ProjectProgressPanelBase pNew = CreateProgressPanel(child.Tree, child, MyCommonBackColor);
						pNew.Location = p.Location;
						Controls.Add(pNew);
						ProjectPanels.RemoveAt(index);
						ProjectPanels.Insert(index,pNew);
						p.Dispose();
						DoSize();
						break;
					}
				}
				//
				++index;
			}

			LayOutProjects();
		}

		protected virtual void projectsNode_ChildRemoved(Node sender, Node child)
		{
			ProjectProgressPanelBase delPanel = null;

			int index = 0;
			int delIndex = -1;
			foreach (ProjectProgressPanelBase p in ProjectPanels)
			{
				if (p.getMonitoredProjectNode() == child)
				{
					delPanel = p;
					delIndex = index;
				}
				++index;
			}
			//
			if (delPanel != null)
			{
				SuspendLayout();

				ProjectProgressPanelBase pNew = CreateProgressPanel(sender.Tree, null, MyCommonBackColor);
				pNew.Location = delPanel.Location;
				Controls.Add(pNew);
				ResumeLayout(false);

				ProjectPanels.RemoveAt(delIndex);
				ProjectPanels.Insert(delIndex,pNew);

				delPanel.Dispose();
				DoSize();
			}
			DoSize();

			LayOutProjects();
		}

		void ProjectsViewer_Resize(object sender, EventArgs e)
		{
			DoSize();
		}

		/// <summary>
		/// 
		/// </summary>
		protected virtual void DoSize()
		{
			int top = 50;
			int y = top;
			int leading = 20;
			int projectHeight = (Height - top - ((ProjectPanels.Count - 1) * leading)) / ProjectPanels.Count;

			foreach (ProjectProgressPanelBase p in ProjectPanels)
			{
				p.Bounds = new Rectangle(0, y, Width, projectHeight);
				y += projectHeight + leading;
			}
			RePositionLabels();

			Invalidate();
		}

		/// <summary>
		/// Showing Changes to the Amount of Money spent by the projects 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="attrs"></param>
		void MyDevelopmentSpendNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			if (attrs != null && attrs.Count > 0)
			{
				foreach(AttributeValuePair avp in attrs)
				{
					//Extraction of the data attribute
					string attribute = avp.Attribute;
					string newValue = avp.Value;
					if ((attribute=="CurrentSpendTotal"))
					{
						int nv = CONVERT.ParseInt(newValue);
						totalSpend = nv;
						nv = nv / 1000;
						lblCurrentSpendAmount_Value.Text = CONVERT.ToStr(nv);
					}
					if ((attribute=="ActualCostTotal"))
					{
						int nv = CONVERT.ParseInt(newValue);
						budgetAllocated = nv;
						totalBudget = budgetAllocated + budgetLeft;
						nv = nv / 1000;
						lblActualCostAmount_Value.Text = CONVERT.ToStr(nv); 
					}
					if ((attribute=="RoundBudgetLeft"))
					{
						int nv = CONVERT.ParseInt(newValue);
						budgetLeft = nv;
						totalBudget = budgetAllocated + budgetLeft;
						nv = nv / 1000;
						lblDevBudgetLeft_Value.Text = CONVERT.ToStr(nv);
					}
				}
			}

			Invalidate();
		}

		public void EnableSelfDrawTitle(bool newState)
		{
			SelfDrawTranslatedTitle = newState;
		}

		public void EnableSelfDrawHeaders(bool newState)
		{
			SelfDrawTranslatedHeaders = newState;
			if (SelfDrawTranslatedHeaders)
			{
				DefineHeaderTextandPosition();
			}
		}

		protected void DefineHeaderTextandPosition()
		{
			strHeader1= "Product";
			strHeader2= "Design";
			strHeader3= "Build";
			strHeader4= "Test";
			strHeader5= "Handover";
			strHeader6= "Ready";
			strHeader7= "Install";
			strHeader8= "Spend";
			strHeader9= "Budget";

			strHeader1 = TextTranslator.TheInstance.Translate(strHeader1);
			strHeader2 = TextTranslator.TheInstance.Translate(strHeader2);
			strHeader3 = TextTranslator.TheInstance.Translate(strHeader3);
			strHeader4 = TextTranslator.TheInstance.Translate(strHeader4);
			strHeader5 = TextTranslator.TheInstance.Translate(strHeader5);
			strHeader6 = TextTranslator.TheInstance.Translate(strHeader6);
			strHeader7 = TextTranslator.TheInstance.Translate(strHeader7);
			strHeader8 = TextTranslator.TheInstance.Translate(strHeader8);
			strHeader9 = TextTranslator.TheInstance.Translate(strHeader9);

			int strHeader1_midPoint = 10+60*0+52/2;
			int strHeader2_midPoint = 10+60*1+52/2;
			int strHeader3_midPoint = 10+60*2+52/2;
			int strHeader4_midPoint = 10+60*3+52/2;
			int strHeader5_midPoint = 10+60*4+52/2;
			int strHeader6_midPoint = 10+60*5+52/2;
			int strHeader7_midPoint = 10+60*6+52/2;
			int strHeader8_midPoint = 10+60*7+52/2;
			int strHeader9_midPoint = 10+60*8+52/2;

			StringFormat sf = new StringFormat(StringFormatFlags.MeasureTrailingSpaces);
			SizeF tmpMeasure;

			Bitmap bmp = new Bitmap(300,50);
			Graphics g = Graphics.FromImage(bmp);


			tmpMeasure = g.MeasureString(strHeader1,headerFont,Width,sf);
			strHeader1_offset = strHeader1_midPoint - ((int)(tmpMeasure.Width /2));

			tmpMeasure = g.MeasureString(strHeader2,headerFont,Width,sf);
			strHeader2_offset = strHeader2_midPoint - ((int)(tmpMeasure.Width /2));
			
			tmpMeasure = g.MeasureString(strHeader3,headerFont,Width,sf);
			strHeader3_offset = strHeader3_midPoint - ((int)(tmpMeasure.Width /2));
			

			tmpMeasure = g.MeasureString(strHeader4,headerFont,Width,sf);
			strHeader4_offset = strHeader4_midPoint - ((int)(tmpMeasure.Width /2));
			
			tmpMeasure = g.MeasureString(strHeader5,headerFont,Width,sf);
			strHeader5_offset = strHeader5_midPoint - ((int)(tmpMeasure.Width /2));
			
			tmpMeasure = g.MeasureString(strHeader6,headerFont,Width,sf);
			strHeader6_offset = strHeader6_midPoint - ((int)(tmpMeasure.Width /2));

			tmpMeasure = g.MeasureString(strHeader7,headerFont,Width,sf);
			strHeader7_offset = strHeader7_midPoint - ((int)(tmpMeasure.Width /2));

			tmpMeasure = g.MeasureString(strHeader8,headerFont,Width,sf);
			strHeader8_offset = strHeader8_midPoint - ((int)(tmpMeasure.Width /2));

			tmpMeasure = g.MeasureString(strHeader9,headerFont,Width,sf);
			strHeader9_offset = strHeader9_midPoint - ((int)(tmpMeasure.Width /2));

			g.Dispose();
			bmp.Dispose();
		}


		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			//g.DrawRectangle(Pens.Cyan,0,0,this.Width-1,this.Height-1);
			if (SelfDrawTranslatedTitle)
			{
				string title_text = panelTitle;
				if (auto_translate)
				{
					title_text = TextTranslator.TheInstance.Translate(title_text);
				    TransitionScreen.DrawSectionTitle(this, g, title_text, titleFont, titleBrush, new Point(10, 0));
				}
			}
			if (SelfDrawTranslatedHeaders)
			{
				g.DrawString(strHeader1, headerFont, headerBrush, strHeader1_offset, 28);
				g.DrawString(strHeader2, headerFont, headerBrush, strHeader2_offset, 28);
				g.DrawString(strHeader3, headerFont, headerBrush, strHeader3_offset, 28);
				g.DrawString(strHeader4, headerFont, headerBrush, strHeader4_offset, 28);
				g.DrawString(strHeader5, headerFont, headerBrush, strHeader5_offset, 28);
				g.DrawString(strHeader6, headerFont, headerBrush, strHeader6_offset, 28);
				g.DrawString(strHeader7, headerFont, headerBrush, strHeader7_offset, 28);
				g.DrawString(strHeader8, headerFont, headerBrush, strHeader8_offset, 28);
				g.DrawString(strHeader9, headerFont, headerBrush, strHeader9_offset, 28);
			}
		}

	}
}
