using System;
using System.Drawing;
using System.Globalization;

using System.Windows.Forms;

using System.Collections;
using LibCore;
using Network;
using CoreUtils;

namespace DiscreteSimGUI
{
	/// <summary>
	/// Summary description for ProjectedRevenueView.
	/// </summary>
	public class ProjectedRevenueView : BasePanel
	{
		static System.Globalization.CultureInfo myCI_enGB = new System.Globalization.CultureInfo( "en-GB", false );

		Node projectedRevenueNode;

		Label maxrevenueLabel;
		Label maxrevenueTitle;

		Label revenueLabel;
		Label revenueTitle;

		Label lostrevenueLabel;
		Label lostrevenueTitle;

		Node TransactionsNode;
		Label TransactionsLabel;
		Label TransactionsTitle;
		Color Titles_Color;
		Color Values_Color;
		Color LostRevColor;
		Color ActualRevColor;

		Font MyDefaultSkinFontBold95;

        string NameOfBusinessWhoseRevenueIsNeeded = String.Empty;

		public ProjectedRevenueView (NodeTree model, int offset)
			: this(model, ConstantSizeFont.NewFont(SkinningDefs.TheInstance.GetData("fontname"), SkinningDefs.TheInstance.GetFloatData("revenue_font_size", 10.0f), SkinningDefs.TheInstance.GetBoolData("revenue_font_bold", true) ? FontStyle.Bold : FontStyle.Regular), offset)
		{
		}

		public ProjectedRevenueView (NodeTree model)
			: this(model, ConstantSizeFont.NewFont(SkinningDefs.TheInstance.GetData("fontname"), SkinningDefs.TheInstance.GetFloatData("revenue_font_size", 10.0f), SkinningDefs.TheInstance.GetBoolData("revenue_font_bold", true) ? FontStyle.Bold : FontStyle.Regular), 0)
		{
		}

        public ProjectedRevenueView(NodeTree model,int business, int offset)
            : this(model,business, ConstantSizeFont.NewFont(SkinningDefs.TheInstance.GetData("fontname"), SkinningDefs.TheInstance.GetFloatData("revenue_font_size", 10.0f), SkinningDefs.TheInstance.GetBoolData("revenue_font_bold", true) ? FontStyle.Bold : FontStyle.Regular), offset)
        {
        }

		public ProjectedRevenueView (NodeTree model, Font font)
			: this (model, font, 0)
		{
		}

        public ProjectedRevenueView(NodeTree model, int business, Font font, int offset)
        {
            this.BackColor = Color.White;
            //this.BackColor = Color.LawnGreen;
            string revname = TextTranslator.TheInstance.Translate("Revenue");
            string lost_revname = TextTranslator.TheInstance.Translate("Lost Revenue");

            NameOfBusinessWhoseRevenueIsNeeded = "Revenue" + " " + business;

            MyDefaultSkinFontBold95 = font;

            string transactionName = TextTranslator.TheInstance.Translate(SkinningDefs.TheInstance.GetData("transactionname"));

            //this.BackColor = Color.DarkKhaki;
            string font_name = TextTranslator.TheInstance.GetTranslateFont(font.FontFamily.GetName(0));
            float font_size = TextTranslator.TheInstance.GetTranslateFontSize(font_name, font.Size);
            Font f1_Values = ConstantSizeFont.NewFont(font_name, font_size, font.Style);
            Font f1_Titles = f1_Values;

            Titles_Color = SkinningDefs.TheInstance.GetColorDataGivenDefault("revenue_titles_colour", Color.White);
            Values_Color = SkinningDefs.TheInstance.GetColorDataGivenDefault("revenue_values_colour", Color.White);
            LostRevColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("revenue_lost_revenue_colour", Color.Orange);
            ActualRevColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("revenue_gained_revenue_colour", Color.LawnGreen);

            //instantiated but not used so previous code can continue to work
            TransactionsTitle = new Label();
            TransactionsLabel = new Label();
            maxrevenueLabel = new Label();
            maxrevenueTitle = new Label();

            int height = SkinningDefs.TheInstance.GetIntData("height_revenue_perstore",20);
            int revenueTitleLength = SkinningDefs.TheInstance.GetIntData("length_revenue_label_perstore",65);
            int revenueLabelLength = SkinningDefs.TheInstance.GetIntData("length_revenue_title_perstore",100);
            int smallGap = SkinningDefs.TheInstance.GetIntData("padding_perstore",5);

            int lostRevenueTitleLength = SkinningDefs.TheInstance.GetIntData("length_lostrevenue_label_perstore",80);
            int lostRevenueLabelLength = SkinningDefs.TheInstance.GetIntData("length_lostrevenue_title_perstore", 100);

            revenueTitle = new Label();
            revenueTitle.BackColor = Color.Black;
            //revenueTitle.BackColor = Color.RosyBrown;
            revenueTitle.ForeColor = ActualRevColor;
            revenueTitle.TextAlign = ContentAlignment.MiddleLeft;
            revenueTitle.Font = f1_Titles;
            revenueTitle.Size = new Size(revenueTitleLength, height);
            revenueTitle.Location = new Point(smallGap,smallGap);
            revenueTitle.Text = revname;
            //revenueTitle.Text = "Actual ";
            this.Controls.Add(revenueTitle);

            revenueLabel = new Label();
            revenueLabel.BackColor = Color.Black;
            //revenueLabel.BackColor = Color.LightCoral;
            revenueLabel.ForeColor = ActualRevColor;
            revenueLabel.TextAlign = ContentAlignment.MiddleRight;
            revenueLabel.Font = f1_Values;
            revenueLabel.Size = new Size(revenueLabelLength, height);
            revenueLabel.Location = new Point(revenueTitle.Right+smallGap, smallGap);
            this.Controls.Add(revenueLabel);

            projectedRevenueNode = model.GetNamedNode(NameOfBusinessWhoseRevenueIsNeeded);
            projectedRevenueNode.AttributesChanged += ChangedAttributeOnProjectedRevenueNode;

            lostrevenueTitle = new Label();
            lostrevenueTitle.BackColor = Color.Black;
            //lostrevenueTitle.BackColor = Color.Maroon;
            lostrevenueTitle.ForeColor = LostRevColor;
            lostrevenueTitle.TextAlign = ContentAlignment.MiddleLeft;
            lostrevenueTitle.Font = f1_Titles;
            lostrevenueTitle.Size = new Size(lostRevenueTitleLength, height);
            lostrevenueTitle.Location = new Point(revenueTitle.Left, revenueTitle.Bottom);
            lostrevenueTitle.Text = lost_revname;
            //lostrevenueTitle.Text = "Lost ";
            this.Controls.Add(lostrevenueTitle);

            lostrevenueLabel = new Label();
            lostrevenueLabel.BackColor = Color.Black;
            //lostrevenueLabel.BackColor = Color.Teal;
            lostrevenueLabel.ForeColor = LostRevColor;
            lostrevenueLabel.TextAlign = ContentAlignment.MiddleRight;
            lostrevenueLabel.Font = f1_Values;
            lostrevenueLabel.Size = new Size(lostRevenueLabelLength, height);
            lostrevenueLabel.Location = new Point(lostrevenueTitle.Right+smallGap, revenueLabel.Bottom);
            lostrevenueLabel.Text = "";
            this.Controls.Add(lostrevenueLabel);

            Color backColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("esm_main_screen_background_colour", Color.Black);

            ChangeDisplayColours(backColor, Values_Color, LostRevColor, ActualRevColor);

            //this.Resize += new EventHandler(ProjectedRevenueView_Resize);
            RefreshRevenueDisplay();
        }

		public ProjectedRevenueView (NodeTree model, Font font, int offset)
		{
			this.BackColor = Color.White;
			//this.BackColor = Color.LawnGreen;
			string revname = TextTranslator.TheInstance.Translate("Revenue");
			string max_revname = TextTranslator.TheInstance.Translate("Max Revenue");
			string lost_revname = TextTranslator.TheInstance.Translate("Lost Revenue");

            bool isESM = SkinningDefs.TheInstance.GetBoolData("esm_sim", false);
		    int padding = 0;
		    
			MyDefaultSkinFontBold95 = font;

			string transactionName =  TextTranslator.TheInstance.Translate(SkinningDefs.TheInstance.GetData("transactionname"));

			//this.BackColor = Color.DarkKhaki;
			string font_name = TextTranslator.TheInstance.GetTranslateFont(font.FontFamily.GetName(0));
			float font_size = TextTranslator.TheInstance.GetTranslateFontSize(font_name, font.Size);
			Font f1_Values = ConstantSizeFont.NewFont(font_name, font_size, font.Style);
			Font f1_Titles = f1_Values;

		    Titles_Color = SkinningDefs.TheInstance.GetColorDataGivenDefault("revenue_titles_colour", Color.White);
		    Values_Color = SkinningDefs.TheInstance.GetColorDataGivenDefault("revenue_values_colour", Color.White);
		    LostRevColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("revenue_lost_revenue_colour", Color.Orange);
		    ActualRevColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("revenue_gained_revenue_colour", Color.LawnGreen);

			TransactionsTitle = new Label();
			TransactionsTitle.BackColor = Color.Black;
			//TransactionsTitle.BackColor = Color.LightBlue;
			TransactionsTitle.ForeColor = Titles_Color;
			TransactionsTitle.TextAlign = ContentAlignment.MiddleLeft;
			TransactionsTitle.Font =  f1_Titles;
			TransactionsTitle.Size = new Size(90+10,25);
		    if(isESM)
		    {
                TransactionsTitle.Location = new Point(padding, padding);
		    }
		    else
		    {
		        TransactionsTitle.Location = new Point(0, offset);
		    }
		    TransactionsTitle.Text = transactionName;
			this.Controls.Add(TransactionsTitle);

			TransactionsLabel = new Label();
			TransactionsLabel.BackColor = Color.Black;
			//TransactionsLabel.BackColor = Color.SkyBlue;
			TransactionsLabel.ForeColor = Values_Color;
			TransactionsLabel.TextAlign = ContentAlignment.MiddleRight;
			TransactionsLabel.Font = f1_Values;
			
		    if (isESM)
            {
                TransactionsLabel.Size = new Size(100, 25);
		        TransactionsLabel.Location = new Point(TransactionsTitle.Right, TransactionsTitle.Top );
		    }
		    else
		    {
                TransactionsLabel.Size = new Size(70, 25);
                TransactionsLabel.Location = new Point(120, offset);
		    }
		    TransactionsLabel.Text = "";
			this.Controls.Add(TransactionsLabel);


            string TransactionNodeName;
            ArrayList TransactionNode =  model.GetNodesWithAttributeValue("use_for_transactions", "true");
            if (TransactionNode.Count > 1)
            {
                throw new Exception("Multiple Nodes being used as transaction node");
            }
            else if (TransactionNode.Count == 1)
            {
                Node transactionNode = (Node)TransactionNode[0];
                TransactionNodeName = transactionNode.GetAttribute("name");
            }
            else
            {
                TransactionNodeName = "Transactions";
            }

            
			TransactionsNode = model.GetNamedNode(TransactionNodeName);
			TransactionsNode.AttributesChanged += TransactionsNode_AttributesChanged;

			maxrevenueTitle = new Label();
			maxrevenueTitle.BackColor = Color.Black;
			//maxrevenueTitle.BackColor = Color.Teal;
			maxrevenueTitle.ForeColor = Titles_Color;
			maxrevenueTitle.TextAlign = ContentAlignment.MiddleLeft;
			maxrevenueTitle.Font =  f1_Titles;
			maxrevenueTitle.Size = new Size(90+10,25);
		    if (isESM)
		    {
                maxrevenueTitle.Location = new Point(TransactionsTitle.Left, TransactionsTitle.Bottom + padding);    
		    }
		    else
		    {
                maxrevenueTitle.Location = new Point(0, 25 + offset);
		    }
			maxrevenueTitle.Text = max_revname;
			this.Controls.Add(maxrevenueTitle);

            maxrevenueLabel = new Label();
            maxrevenueLabel.BackColor = Color.Black;
            //maxrevenueLabel.BackColor = Color.DeepSkyBlue;
            maxrevenueLabel.ForeColor = Values_Color;
            maxrevenueLabel.TextAlign = ContentAlignment.MiddleRight;
            maxrevenueLabel.Font = f1_Values;
            maxrevenueLabel.Size = new Size(100, 25);
            if (isESM)
            {
                maxrevenueLabel.Location = new Point(maxrevenueTitle.Right, maxrevenueTitle.Top);
            }
            else
            {
                maxrevenueLabel.Location = new Point(90, 25 + offset);
            }

            maxrevenueLabel.Text = max_revname;
            this.Controls.Add(maxrevenueLabel);

			revenueTitle = new Label();
			revenueTitle.BackColor = Color.Black;
			//revenueTitle.BackColor = Color.RosyBrown;
			revenueTitle.ForeColor = ActualRevColor;
			revenueTitle.TextAlign = ContentAlignment.MiddleLeft;
			revenueTitle.Font =  f1_Titles;
			revenueTitle.Size = new Size(100,25);
		    if (isESM)
		    {
                revenueTitle.Location = new Point(maxrevenueTitle.Left,maxrevenueTitle.Bottom + padding);    
		    }
		    else
		    {
                revenueTitle.Location = new Point(200, offset);
		    }
			revenueTitle.Text = revname;
			//revenueTitle.Text = "Actual ";
			this.Controls.Add(revenueTitle);

			revenueLabel = new Label();
			revenueLabel.BackColor = Color.Black;
			//revenueLabel.BackColor = Color.LightCoral;
			revenueLabel.ForeColor = ActualRevColor;
			revenueLabel.TextAlign = ContentAlignment.MiddleRight;
			revenueLabel.Font =f1_Values;
			revenueLabel.Size = new Size(100,25);
		    if (isESM)
		    {
                revenueLabel.Location = new Point(revenueTitle.Right, revenueTitle.Top);    
		    }
		    else
		    {
                revenueLabel.Location = new Point(300, offset);
		    }
			this.Controls.Add(revenueLabel);

			projectedRevenueNode = model.GetNamedNode("Revenue");
			projectedRevenueNode.AttributesChanged += projectedRevenueNode_AttributesChanged;

			lostrevenueTitle = new Label();
			lostrevenueTitle.BackColor = Color.Black;
			//lostrevenueTitle.BackColor = Color.Maroon;
			lostrevenueTitle.ForeColor = LostRevColor;
			lostrevenueTitle.TextAlign = ContentAlignment.MiddleLeft;
			lostrevenueTitle.Font =  f1_Titles;
			lostrevenueTitle.Size = new Size(100,25);
		    if (isESM)
		    {
		        lostrevenueTitle.Location = new Point(revenueTitle.Left, revenueTitle.Bottom + padding);
		    }
		    else
		    {
                lostrevenueTitle.Location = new Point(200, 25 + offset);
		    }

		    lostrevenueTitle.Text = lost_revname;
			//lostrevenueTitle.Text = "Lost ";
			this.Controls.Add(lostrevenueTitle);

			lostrevenueLabel = new Label();
			lostrevenueLabel.BackColor = Color.Black;
			//lostrevenueLabel.BackColor = Color.Teal;
			lostrevenueLabel.ForeColor = LostRevColor;
			lostrevenueLabel.TextAlign = ContentAlignment.MiddleRight;
			lostrevenueLabel.Font = f1_Values;
			lostrevenueLabel.Size = new Size(100,25);
		    if (isESM)
		    {
		        lostrevenueLabel.Location = new Point(lostrevenueTitle.Right, lostrevenueTitle.Top);
		    }
		    else
		    {
		        lostrevenueLabel.Location = new Point(300, 25 + offset);
		    }
		    lostrevenueLabel.Text = "";
			this.Controls.Add(lostrevenueLabel);

            Color backColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("esm_main_screen_background_colour", Color.Black);

		    ChangeDisplayColours(backColor, Values_Color, ActualRevColor, LostRevColor);

			this.Resize += ProjectedRevenueView_Resize;
			UpdateTransactionsDisplay();
			UpdateRevenueDisplay();
		}

		public void SetLayout(
			int left_gutter, int left_title_width, int left_value_width,
			int right_gutter, int right_title_width, int right_value_width)
		{
			TransactionsTitle.Left = left_gutter;
			TransactionsTitle.Width = left_title_width;
			TransactionsLabel.Left = TransactionsTitle.Right;
			TransactionsLabel.Width = left_value_width;

			maxrevenueTitle.Left = TransactionsTitle.Left;
			maxrevenueTitle.Width = TransactionsTitle.Width;
			maxrevenueLabel.Left = TransactionsLabel.Left;
			maxrevenueLabel.Width = TransactionsLabel.Width;

			revenueLabel.Width = right_value_width;
			revenueLabel.Left = this.Width - right_gutter - revenueLabel.Width;
			revenueTitle.Width = right_title_width;
			revenueTitle.Left = revenueLabel.Left - revenueTitle.Width;

			lostrevenueLabel.Width = revenueLabel.Width;
			lostrevenueLabel.Left = revenueLabel.Left;
			lostrevenueTitle.Width = revenueTitle.Width;
			lostrevenueTitle.Left = revenueTitle.Left;
		}

		public void ChangeDisplayColours(Color PanelBack, Color NormalForeColor, 
			Color RevenueGainedForeColor, Color RevenueLostForeColor)
		{
			this.BackColor = PanelBack;

			Titles_Color = NormalForeColor;
			Values_Color = NormalForeColor;
			LostRevColor = RevenueLostForeColor;
			ActualRevColor = RevenueGainedForeColor;

			TransactionsTitle.BackColor = this.BackColor;
			//TransactionsTitle.BackColor = Color.LightBlue;
			TransactionsTitle.ForeColor = Titles_Color;

			TransactionsLabel.BackColor = this.BackColor;
			//TransactionsLabel.BackColor = Color.SkyBlue;
			TransactionsLabel.ForeColor = Values_Color;

			maxrevenueTitle.BackColor = this.BackColor;
			//maxrevenueTitle.BackColor = Color.Teal;
			maxrevenueTitle.ForeColor = Titles_Color;

			maxrevenueLabel.BackColor = this.BackColor;
			//maxrevenueLabel.BackColor = Color.DeepSkyBlue;
			maxrevenueLabel.ForeColor = Values_Color;

			revenueTitle.BackColor = this.BackColor;
			//revenueTitle.BackColor = Color.RosyBrown;
			revenueTitle.ForeColor = ActualRevColor;

			revenueLabel.BackColor = this.BackColor;
			//revenueLabel.BackColor = Color.LightCoral;
			revenueLabel.ForeColor = ActualRevColor;

			lostrevenueTitle.BackColor = this.BackColor;
			//lostrevenueTitle.BackColor = Color.Maroon;
			lostrevenueTitle.ForeColor = LostRevColor;

			lostrevenueLabel.BackColor = this.BackColor;
			//lostrevenueLabel.BackColor = Color.Teal;
			lostrevenueLabel.ForeColor = LostRevColor;

		}

		void UpdateTransactionsDisplay()
		{
			int trans_count_good = this.TransactionsNode.GetIntAttribute("count_good",0);
			int trans_count_max = this.TransactionsNode.GetIntAttribute("count_max",0);
			this.TransactionsLabel.Text = CONVERT.ToStr(trans_count_good) + @"/" + CONVERT.ToStr(trans_count_max);
		}

		void RefreshRevenueDisplay()
        {
            //Just display in single Dollars for the time being 
            NumberFormatInfo nfi = new CultureInfo("en-GB", false).NumberFormat;

            int revenue = projectedRevenueNode.GetIntAttribute("revenue", 0);
            string numberstr1 = revenue.ToString("N", nfi);
            numberstr1 = numberstr1.Replace(".00", "");
            revenueLabel.Text = "$" + numberstr1;

            int lost_revenue = projectedRevenueNode.GetIntAttribute("revenue_lost", 0);
            string numberstr3 = lost_revenue.ToString("N", nfi);
            numberstr3 = numberstr3.Replace(".00", "");
            this.lostrevenueLabel.Text = "$" + numberstr3;
        }

		void UpdateRevenueDisplay()
		{
			//Just display in single Dollars for the time being 
			NumberFormatInfo nfi = new CultureInfo( "en-GB", false ).NumberFormat;

			//int revenue = projectedRevenueNode.GetIntAttribute("revenue",0);
			//double revenueMillions = ((double)revenue)/1000000.0;
			//revenueLabel.Text = "Actual Revenue $" + CONVERT.ToStr(revenueMillions) + "M";
			int revenue = projectedRevenueNode.GetIntAttribute("revenue",0);
			string numberstr1 = revenue.ToString( "N", nfi );
			numberstr1 = numberstr1.Replace(".00","");
			revenueLabel.Text = "$" + numberstr1;

			int max_revenue = projectedRevenueNode.GetIntAttribute("max_revenue",0);
			string numberstr2 = max_revenue.ToString( "N", nfi );
			numberstr2 = numberstr2.Replace(".00","");
			this.maxrevenueLabel.Text = "$" + numberstr2;

			int lost_revenue = projectedRevenueNode.GetIntAttribute("revenue_lost",0);
			string numberstr3 = lost_revenue.ToString( "N", nfi );
			numberstr3 = numberstr3.Replace(".00","");
			this.lostrevenueLabel.Text = "$" + numberstr3;
		}

		void ChangedAttributeOnProjectedRevenueNode (Node sender, ArrayList attrs)
	    {
            // Update our display...
            RefreshRevenueDisplay();
	    }

		void projectedRevenueNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			// Update our display...
			UpdateRevenueDisplay();
		}

		/// <summary>
		/// Dispose ...
		/// </summary>
		public new void Dispose()
		{
			projectedRevenueNode.AttributesChanged -= projectedRevenueNode_AttributesChanged;
			TransactionsNode.AttributesChanged -= TransactionsNode_AttributesChanged;
		}

		void ProjectedRevenueView_Resize(object sender, EventArgs e)
		{
		}

		void TransactionsNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			UpdateTransactionsDisplay();
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