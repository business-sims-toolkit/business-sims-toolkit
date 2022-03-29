using System;
using System.Drawing;
using System.Windows.Forms;

using System.Collections;
using LibCore;
using Network;
using CoreUtils;

namespace DiscreteSimGUI
{
	/// <summary>
	/// Summary description for ProjectedRevenueAndAvailabilityView.
	/// </summary>
	public class ProjectedAvailabilityView : BasePanel
	{
		static System.Globalization.CultureInfo myCI_enGB = new System.Globalization.CultureInfo( "en-GB", false );

		Node availabilityNode;
		Label availabilityLabel;
		Label availabilityTitle;

		Node ImpactNode;
		Label impactLabel;
		Label impactTitle;

		Node SLABreachNode;
		Label SLABreachLabel;
		Label SLABreachTitle;

		Node timeNode;
		Label RealTimeLabel;

		Font MyDefaultSkinFontNormal95;
		Font f1_Values;
		Font f1_Titles;
		Color Titles_Color = Color.White;
		Color Values_Color = Color.White;

		bool auto_translate = true;
	
		/// <summary>
		/// Wether to show availability
		/// </summary>
		public bool ShowAvailability
		{
			set
			{
				availabilityLabel.Visible = value;
				if(value)
				{
					UpdateAvailabilityDisplay();
				}
			}
		}

		public void SetLayout(int text_height, int left /* 5 */,
			int availability_title_width /* 80 */, int availability_value_width /* 45 */,
			int impact_space /* 30 */, int impact_title_width /* 40 */, int impact_value_width /* 45 */,
			int sla_space /* 30 */, int sla_title_width /* 120 */, int sla_value_width /* 20 */)
		{
			availabilityTitle.Size = new Size(availability_title_width,text_height); // 80
			availabilityTitle.Location = new Point(left,0);
			availabilityLabel.Size = new Size(availability_value_width,text_height); // 45
			availabilityLabel.Location = new Point(left + availability_title_width,0);
			//
			impactTitle.Size = new Size(impact_title_width,text_height);
			impactTitle.Location = new Point(left + availability_title_width + availability_value_width + impact_space,0);
			impactLabel.Size = new Size(impact_value_width,text_height);
			impactLabel.Location = new Point(left + availability_title_width + availability_value_width + impact_space + impact_title_width,0);
			//
			left = left + availability_title_width + availability_value_width + impact_space + impact_title_width;
			//
			SLABreachTitle.Size = new Size(sla_title_width,text_height);
			SLABreachTitle.Location = new Point(left + impact_value_width + sla_space,0);
			SLABreachLabel.Size = new Size(sla_value_width,text_height);
			SLABreachLabel.Location = new Point(left + impact_value_width + sla_space + sla_title_width,0);
		}

		public ProjectedAvailabilityView (NodeTree model)
		{
			string bold = SkinningDefs.TheInstance.GetData("availability_bold");
			float font_size = SkinningDefs.TheInstance.GetFloatData("availability_size", 9.5f);
			string skin_font_name = SkinningDefs.TheInstance.GetData("fontname");

			//Construct the standard font (from the skin file)
			Font MyNormalFont = ConstantSizeFont.NewFont(skin_font_name, font_size);
			Font MyBoldFont = ConstantSizeFont.NewFont(skin_font_name, font_size, FontStyle.Bold);

			if(auto_translate)
			{
				MyNormalFont.Dispose();
				MyBoldFont.Dispose();
				MyNormalFont = ConstantSizeFont.NewFont(TextTranslator.TheInstance.GetTranslateFont(skin_font_name), font_size);
				MyBoldFont = ConstantSizeFont.NewFont(TextTranslator.TheInstance.GetTranslateFont(skin_font_name), font_size, FontStyle.Bold);
			}

			if("true" != bold)
			{
			    if (SkinningDefs.TheInstance.GetBoolData("esm_sim", false))
			    {
                    SetupESM(model, MyNormalFont);
			    }
			    else
			    {
			        Setup(model, MyNormalFont);
			    }
			}
			else
			{
			    if (SkinningDefs.TheInstance.GetBoolData("esm_sim", false))
			    {
                    SetupESM(model, MyBoldFont);
			    }
			    else
			    {
			        Setup(model, MyBoldFont);
			    }
			}

			if(auto_translate)
			{
				if (TextTranslator.TheInstance.areTranslationsLoaded())
				{
					this.SetLayout(25,4,90,50,5,90,50,5,90,50);
				}
			}
		}

		/// <summary>
		/// Show Availability / Revenue
		/// </summary>
		/// <param name="model"></param>
		public ProjectedAvailabilityView (NodeTree model, Font font)
		{
		    if (SkinningDefs.TheInstance.GetBoolData("esm_sim", false))
		    {
		        SetupESM(model, font);
		    }
		    else
		    {
		        Setup(model, font);
		    }
		}

		protected void Setup(NodeTree model, Font font)
		{
		    Color backColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("esm_main_screen_background_colour", Color.Black);

            BackColor = backColor;

			string title_Availability = "Availability";
			string title_Impact = "Impact";
			string title_slabreach = "SLA Breach Count";

            int runningGap = 0;
            int textHeight = SkinningDefs.TheInstance.GetIntData("availability_text_height", 25);
            int paddingAvailTitle = SkinningDefs.TheInstance.GetIntData("availability_padding_avail_title", 5);
            int paddingAvailLabel = SkinningDefs.TheInstance.GetIntData("availability_padding_avail_label", 0);
            int impactTitleWidth = SkinningDefs.TheInstance.GetIntData("availability_padding_impact_title_width", 60);
            int paddingImpactTitle = SkinningDefs.TheInstance.GetIntData("availability_padding_impact_title", 30);
            int paddingImpactLabel = SkinningDefs.TheInstance.GetIntData("availability_padding_impact_label", 0);
            int paddingSLABreachTitle = SkinningDefs.TheInstance.GetIntData("availability_padding_slaBreach_title", 25);
            int paddingSLABreachLabel = SkinningDefs.TheInstance.GetIntData("availability_padding_slaBreach_label", 0);

			if (auto_translate)
			{
				title_Availability = TextTranslator.TheInstance.Translate(title_Availability);
				title_Impact =  TextTranslator.TheInstance.Translate(title_Impact);
				title_slabreach =  TextTranslator.TheInstance.Translate(title_slabreach);
			}

			MyDefaultSkinFontNormal95 = font;
            
			//this.BackColor = Color.DarkKhaki;
			f1_Values = font;
			f1_Titles = font;
			Titles_Color = Color.White;
			Values_Color = Color.White;

			availabilityTitle = new Label();
            availabilityTitle.BackColor = backColor;
			availabilityTitle.ForeColor = Titles_Color;
			availabilityTitle.TextAlign = ContentAlignment.MiddleLeft;
			availabilityTitle.Size = new Size(80,textHeight);
			availabilityTitle.Text = title_Availability;
			availabilityTitle.Font = f1_Titles;
			availabilityTitle.Location = new Point(paddingAvailTitle,0);
			this.Controls.Add(availabilityTitle);
            runningGap = availabilityTitle.Right;

			availabilityLabel = new Label();
		    availabilityLabel.BackColor = backColor;
			availabilityLabel.ForeColor = Values_Color;
            availabilityLabel.TextAlign = ContentAlignment.MiddleRight;
			availabilityLabel.Size = new Size(45,textHeight);
			availabilityLabel.Font = f1_Values;
			availabilityLabel.Location = new Point(runningGap + paddingAvailLabel,0);
			this.Controls.Add(availabilityLabel);
            runningGap = availabilityLabel.Right;

			availabilityNode = model.GetNamedNode("Availability");
			availabilityNode.AttributesChanged += availabilityNode_AttributesChanged;

			impactTitle = new Label();
		    impactTitle.BackColor = backColor;
			impactTitle.ForeColor = Titles_Color;
            impactTitle.TextAlign = ContentAlignment.MiddleLeft;
			impactTitle.Font = f1_Titles;
            impactTitle.Size = new Size(impactTitleWidth, textHeight);
			impactTitle.Location = new Point(runningGap + paddingImpactTitle,0);
			impactTitle.Text = title_Impact;
			this.Controls.Add(impactTitle);
            runningGap = impactTitle.Right;

			impactLabel = new Label();
		    impactLabel.BackColor = backColor;
			impactLabel.ForeColor = Values_Color;
            impactLabel.TextAlign = ContentAlignment.MiddleRight;
			impactLabel.Font = f1_Values;
            impactLabel.Size = new Size(45, textHeight);
			impactLabel.Location = new Point(runningGap + paddingImpactLabel,0);
			impactLabel.Text = "";
			this.Controls.Add(impactLabel);
            runningGap = impactLabel.Right;

			ImpactNode = model.GetNamedNode("Impact");
			ImpactNode.AttributesChanged += ImpactNode_AttributesChanged;

			SLABreachTitle = new Label();
		    SLABreachTitle.BackColor = backColor;
			SLABreachTitle.ForeColor = Titles_Color;
            SLABreachTitle.TextAlign = ContentAlignment.MiddleLeft;
			SLABreachTitle.Font = f1_Titles;
            SLABreachTitle.Size = new Size(120, textHeight);
			SLABreachTitle.Location = new Point(runningGap + paddingSLABreachTitle,0);
			SLABreachTitle.Text = title_slabreach;
			this.Controls.Add(SLABreachTitle);
            runningGap = SLABreachTitle.Right;

			SLABreachLabel = new Label();
		    SLABreachLabel.BackColor = backColor;
			SLABreachLabel.ForeColor = Values_Color;
            SLABreachLabel.TextAlign = ContentAlignment.MiddleRight;
			SLABreachLabel.Font = f1_Values;
            SLABreachLabel.Size = new Size(20, textHeight);
			SLABreachLabel.Location = new Point(runningGap + paddingSLABreachLabel,0);
			SLABreachLabel.Text = "";
			this.Controls.Add(SLABreachLabel);
            runningGap = SLABreachLabel.Right;


            timeNode = model.GetNamedNode("CurrentTime");
            timeNode.AttributesChanged += timeNode_AttributesChanged;

            if (timeNode.GetBooleanAttribute("show_world_time", false)) // If using real time
            {
                availabilityTitle.Location = new Point(5, 0);
                availabilityLabel.Location = new Point(85, 0);
                availabilityTitle.Size = new Size(85, 25);
                availabilityLabel.Size = new Size(40, 25);


                impactTitle.Location = new Point(132, 0);
                impactLabel.Location = new Point(175, 0);
                impactTitle.Size = new Size(50, 25);
                impactLabel.Size = new Size(40, 25);


                SLABreachTitle.Location = new Point(227, 0);
                SLABreachLabel.Location = new Point(315, 0);
                SLABreachTitle.Size = new Size(88, 25);
                SLABreachLabel.Size = new Size(30, 25);

                SLABreachTitle.Text = "SLA Breaches";

                Label RealTimeTitle = new Label();
                RealTimeTitle.BackColor = backColor;
                RealTimeTitle.ForeColor = Titles_Color;
                RealTimeTitle.TextAlign = ContentAlignment.MiddleLeft;
                RealTimeTitle.Font = f1_Titles;
                RealTimeTitle.Size = new Size(50, 25);
                RealTimeTitle.Location = new Point(355, 0);
                RealTimeTitle.Text = "Time";
                this.Controls.Add(RealTimeTitle);
                
          

                RealTimeLabel = new Label();
                RealTimeLabel.BackColor = backColor;
                RealTimeLabel.ForeColor = Values_Color;
                RealTimeLabel.TextAlign = ContentAlignment.MiddleRight;
                RealTimeLabel.Font = f1_Values;
                RealTimeLabel.Size = new Size(30, 25);
                RealTimeLabel.Location = new Point(405, 0);
                double time = timeNode.GetIntAttribute("seconds", 0);
                RealTimeLabel.Text = CONVERT.ToStr(Math.Floor(time/60));
                this.Controls.Add(RealTimeLabel);
            }

			SLABreachNode = model.GetNamedNode("SLA_Breach");
			SLABreachNode.AttributesChanged +=SLABreachNode_AttributesChanged;

			this.Resize += ProjectedAvailabilityView_Resize;
			UpdateImpactDisplay();
			UpdateAvailabilityDisplay();
			UpdateBreachCountDisplay();
		}

        protected void SetupESM(NodeTree model, Font font)
        {
            Color backColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("esm_main_screen_background_colour", Color.Black);
            //stnadrda colours
            this.BackColor = backColor;
            string title_Availability = "Availability";
            string title_Impact = "Impact";
            string title_slabreach = "SLA Breach Count";

            int gap = 0;
            int textHeight = SkinningDefs.TheInstance.GetIntData("availability_text_height", 25);
            int paddingAvailTitle = SkinningDefs.TheInstance.GetIntData("availability_padding_avail_title", 5);
            int paddingAvailLabel = SkinningDefs.TheInstance.GetIntData("availability_padding_avail_label", 0);
            int impactTitleWidth = SkinningDefs.TheInstance.GetIntData("availability_padding_impact_title_width", 60);
            int paddingImpactTitle = SkinningDefs.TheInstance.GetIntData("availability_padding_impact_title", 30);
            int paddingImpactLabel = SkinningDefs.TheInstance.GetIntData("availability_padding_impact_label", 0);
            int paddingSLABreachTitle = SkinningDefs.TheInstance.GetIntData("availability_padding_slaBreach_title", 25);
            int paddingSLABreachLabel = SkinningDefs.TheInstance.GetIntData("availability_padding_slaBreach_label", 0);

            if (auto_translate)
            {
                title_Availability = TextTranslator.TheInstance.Translate(title_Availability);
                title_Impact = TextTranslator.TheInstance.Translate(title_Impact);
                title_slabreach = TextTranslator.TheInstance.Translate(title_slabreach);
            }

            MyDefaultSkinFontNormal95 = font;

            //this.BackColor = Color.DarkKhaki;
            f1_Values = font;
            f1_Titles = font;
            Titles_Color = Color.White;
            Values_Color = Color.White;

            availabilityTitle = new Label();
            availabilityTitle.BackColor = BackColor;
            //availabilityTitle.BackColor = Color.Cyan;
            availabilityTitle.ForeColor = Titles_Color;
            availabilityTitle.TextAlign = ContentAlignment.MiddleLeft;
            availabilityTitle.Size = new Size(120, textHeight);
            availabilityTitle.Text = title_Availability;
            availabilityTitle.Font = f1_Titles;
            availabilityTitle.Location = new Point(paddingAvailTitle, paddingAvailTitle);
            this.Controls.Add(availabilityTitle);
            

            availabilityLabel = new Label();
            availabilityLabel.BackColor = BackColor;
            //availabilityLabel.BackColor = Color.LightPink;
            availabilityLabel.ForeColor = Values_Color;
            availabilityLabel.TextAlign = ContentAlignment.MiddleRight;
            availabilityLabel.Size = new Size(45, textHeight);
            availabilityLabel.Font = f1_Values;
            availabilityLabel.Location = new Point(availabilityTitle.Right + gap, availabilityTitle.Top);
            this.Controls.Add(availabilityLabel);
            
            availabilityNode = model.GetNamedNode("Availability");
            availabilityNode.AttributesChanged += availabilityNode_AttributesChanged;

            impactTitle = new Label();
            impactTitle.BackColor = BackColor;
            //impactTitle.BackColor = Color.SlateBlue;
            impactTitle.ForeColor = Titles_Color;
            impactTitle.TextAlign = ContentAlignment.MiddleLeft;
            impactTitle.Font = f1_Titles;
            impactTitle.Size = new Size(120, textHeight);
            impactTitle.Location = new Point(availabilityTitle.Left, availabilityLabel.Bottom + gap);
            impactTitle.Text = title_Impact;
            this.Controls.Add(impactTitle);
            
            impactLabel = new Label();
            impactLabel.BackColor = BackColor;
            //impactLabel.BackColor = Color.Magenta;
            impactLabel.ForeColor = Values_Color;
            impactLabel.TextAlign = ContentAlignment.MiddleRight;
            impactLabel.Font = f1_Values;
            impactLabel.Size = new Size(45, textHeight);
            impactLabel.Location = new Point(impactTitle.Right + gap, impactTitle.Top);
            impactLabel.Text = "";
            this.Controls.Add(impactLabel);
            
            ImpactNode = model.GetNamedNode("Impact");
            ImpactNode.AttributesChanged += ImpactNode_AttributesChanged;

            SLABreachTitle = new Label();
            SLABreachTitle.BackColor = BackColor;
            //SLABreachTitle.BackColor = Color.Lime;
            SLABreachTitle.ForeColor = Titles_Color;
            SLABreachTitle.TextAlign = ContentAlignment.MiddleLeft;
            SLABreachTitle.Font = f1_Titles;
            SLABreachTitle.Size = new Size(120, textHeight);
            SLABreachTitle.Location = new Point(impactTitle.Left, impactLabel.Bottom + gap);
            SLABreachTitle.Text = title_slabreach;
            this.Controls.Add(SLABreachTitle);
            
            SLABreachLabel = new Label();
            SLABreachLabel.BackColor = BackColor;
            //SLABreachLabel.BackColor = Color.LightGreen;
            SLABreachLabel.ForeColor = Values_Color;
            SLABreachLabel.TextAlign = ContentAlignment.MiddleRight;
            SLABreachLabel.Font = f1_Values;
            SLABreachLabel.Size = new Size(45, textHeight);
            SLABreachLabel.Location = new Point(SLABreachTitle.Right + gap, SLABreachTitle.Top);
            SLABreachLabel.Text = "";
            this.Controls.Add(SLABreachLabel);
            

            timeNode = model.GetNamedNode("CurrentTime");
            timeNode.AttributesChanged += timeNode_AttributesChanged;

            if (timeNode.GetBooleanAttribute("show_world_time", false)) // If using real time
            {
                availabilityTitle.Location = new Point(5, 0);
                availabilityLabel.Location = new Point(85, 0);
                availabilityTitle.Size = new Size(85, 25);
                availabilityLabel.Size = new Size(40, 25);


                impactTitle.Location = new Point(132, 0);
                impactLabel.Location = new Point(175, 0);
                impactTitle.Size = new Size(50, 25);
                impactLabel.Size = new Size(40, 25);


                SLABreachTitle.Location = new Point(227, 0);
                SLABreachLabel.Location = new Point(315, 0);
                SLABreachTitle.Size = new Size(88, 25);
                SLABreachLabel.Size = new Size(30, 25);

                SLABreachTitle.Text = "SLA Breaches";

                Label RealTimeTitle = new Label();
                RealTimeTitle.BackColor = BackColor;
                //SLABreachTitle.BackColor = Color.Lime;
                RealTimeTitle.ForeColor = Titles_Color;
                RealTimeTitle.TextAlign = ContentAlignment.MiddleLeft;
                RealTimeTitle.Font = f1_Titles;
                RealTimeTitle.Size = new Size(50, 25);
                RealTimeTitle.Location = new Point(355, 0);
                RealTimeTitle.Text = "Time";
                this.Controls.Add(RealTimeTitle);


                RealTimeLabel = new Label();
                RealTimeLabel.BackColor = BackColor;
                //SLABreachLabel.BackColor = Color.LightGreen;
                RealTimeLabel.ForeColor = Values_Color;
                RealTimeLabel.TextAlign = ContentAlignment.MiddleRight;
                RealTimeLabel.Font = f1_Values;
                RealTimeLabel.Size = new Size(30, 25);
                RealTimeLabel.Location = new Point(405, 0);
                double time = timeNode.GetIntAttribute("seconds", 0);
                RealTimeLabel.Text = CONVERT.ToStr(Math.Floor(time / 60));
                this.Controls.Add(RealTimeLabel);
            }

            SLABreachNode = model.GetNamedNode("SLA_Breach");
            SLABreachNode.AttributesChanged += SLABreachNode_AttributesChanged;

            this.Resize += ProjectedAvailabilityView_Resize;
            UpdateImpactDisplay();
            UpdateAvailabilityDisplay();
            UpdateBreachCountDisplay();
        }

		public void SetTabStops (int x1, int x2, int x3)
		{
			Graphics graphics;
			int surround = 2;
			int gap = 0;
			
			graphics = availabilityTitle.CreateGraphics();
			availabilityTitle.Width = (int) (graphics.MeasureString(availabilityTitle.Text, availabilityTitle.Font).Width) + (surround * 2);
			graphics.Dispose();
			availabilityTitle.Left = x1;
			availabilityLabel.Left = availabilityTitle.Right + gap;
			graphics = availabilityLabel.CreateGraphics();
			availabilityLabel.Width = (int) (graphics.MeasureString("99.9%", availabilityLabel.Font).Width) + (surround * 2);
			graphics.Dispose();

			graphics = impactTitle.CreateGraphics();
			impactTitle.Width = (int) (graphics.MeasureString(impactTitle.Text, impactTitle.Font).Width) + (surround * 2);
			graphics.Dispose();
			impactTitle.Left = x2 - impactTitle.Width - (gap / 2);
			graphics = impactLabel.CreateGraphics();
			impactLabel.Width = (int) (graphics.MeasureString("99.9%", impactLabel.Font).Width) + (surround * 2);
			graphics.Dispose();
			impactLabel.Left = x2 + (gap / 2);

			graphics = SLABreachLabel.CreateGraphics();
			SLABreachLabel.Width = (int) (graphics.MeasureString("999", SLABreachLabel.Font).Width) + (surround * 2);
			graphics.Dispose();
			SLABreachLabel.Left = x3 - SLABreachLabel.Width;		
			graphics = SLABreachTitle.CreateGraphics();
			SLABreachTitle.Width = (int) (graphics.MeasureString(SLABreachTitle.Text, SLABreachTitle.Font).Width) + (surround * 2);
			graphics.Dispose();
			SLABreachTitle.Left = SLABreachLabel.Left - gap - SLABreachTitle.Width;
		}

		public void SetLeftAligned ()
		{
			this.availabilityLabel.TextAlign = ContentAlignment.MiddleLeft;
			this.impactLabel.TextAlign = ContentAlignment.MiddleLeft;
			this.SLABreachLabel.TextAlign = ContentAlignment.MiddleLeft;
		}

		public void ChangeDisplayColors(Color newBackColor, Color newTitleColor,  Color newValueColor)
		{
			this.BackColor = newBackColor;
			Titles_Color = newTitleColor;
			Values_Color = newValueColor;

			availabilityTitle.BackColor = this.BackColor;
			availabilityTitle.ForeColor = Titles_Color;
			availabilityLabel.BackColor = this.BackColor;
			availabilityLabel.ForeColor = Values_Color;

			impactTitle.BackColor = this.BackColor;
			impactTitle.ForeColor = Titles_Color;
			impactLabel.BackColor = this.BackColor;
			impactLabel.ForeColor = Values_Color;

			SLABreachTitle.BackColor = this.BackColor;
			SLABreachTitle.ForeColor = Titles_Color;
			SLABreachLabel.BackColor = this.BackColor;
			SLABreachLabel.ForeColor = Values_Color;
		}

		void UpdateAvailabilityDisplay()
		{
			double availability = availabilityNode.GetDoubleAttribute("availability",0.0);
			this.availabilityLabel.Text = CONVERT.ToStr((int) availability) + "%";
		}

		void UpdateImpactDisplay()
		{
			double impact_level = this.ImpactNode.GetDoubleAttribute("impact",0.0);
			this.impactLabel.Text = CONVERT.ToStr((int) impact_level) + "%";
		}

		void UpdateBreachCountDisplay()
        {
            int sla_count = this.SLABreachNode.GetIntAttribute("biz_serv_count", 0);
            this.SLABreachLabel.Text = CONVERT.ToStr((int)sla_count);
        }

		void UpdateTimeAttributeDisplay()
        {
            double time = this.timeNode.GetIntAttribute("seconds", 0);
            if (this.RealTimeLabel != null)
            {
                this.RealTimeLabel.Text = CONVERT.ToStr(Math.Floor(time / 60));
            }
        }


		/// <summary>
		/// Dispose ...
		/// </summary>
		public new void Dispose()
		{
			availabilityNode.AttributesChanged -= availabilityNode_AttributesChanged;
			ImpactNode.AttributesChanged -= ImpactNode_AttributesChanged;
			SLABreachNode.AttributesChanged -=SLABreachNode_AttributesChanged;
		}

		void ProjectedAvailabilityView_Resize(object sender, EventArgs e)
		{
		}

		void ImpactNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			UpdateImpactDisplay();
		}

		void availabilityNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			if(availabilityLabel.Visible)
			{
				UpdateAvailabilityDisplay();
			}
		}

		void SLABreachNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			UpdateBreachCountDisplay();
		}

        void timeNode_AttributesChanged(Node sender, ArrayList attrs)
        {
            UpdateTimeAttributeDisplay();
        }

	    protected override void OnBackColorChanged (EventArgs e)
	    {
	        base.OnBackColorChanged(e);

	        foreach (Control control in Controls)
	        {
	            control.BackColor = BackColor;
	        }
	    }
	}
}