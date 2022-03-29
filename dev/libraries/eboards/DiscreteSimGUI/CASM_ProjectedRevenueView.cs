using System.Drawing;
using System.Globalization;

using System.Windows.Forms;

using System.Collections;
using LibCore;
using Network;
using CommonGUI;
using CoreUtils;

namespace DiscreteSimGUI
{
	/// <summary>
	/// Summary description for CASM_ProjectedRevenueView.
	/// The Revenue numbers and the Incident numbers as well using a background and a paint method
	/// </summary>
	public class CASM_ProjectedRevenueView : FlickerFreePanel
	{
		static System.Globalization.CultureInfo myCI_enGB = new System.Globalization.CultureInfo( "en-GB", false );

		Node TransactionsNode;
		Node projectedRevenueNode;
		Node VulnerabilityStatsNode;

		Color Titles_Color = Color.White;
		Color Values_Color = Color.White;
		Color LostRevColor = Color.Orange;
		Color ActualRevColor = Color.LawnGreen;

		bool isTrainingGame;

		//Font MyDefaultSkinFontBold95;
		Font f1_Values;
		Font f1_Titles;
		Font f1_debug;

		string revname = "Actual";
		string max_revname = "Target";
		string lost_revname = "Lost";
		string prevented_violation_name = "Prevented Violations";
		string fixed_violation_name = "Fixed Violations";
		string transaction_name= "Transactions";

		public CASM_ProjectedRevenueView (NodeTree model, bool IsTrainingFlag, int offset)
		{
			isTrainingGame = IsTrainingFlag;

			BackColor = Color.Transparent;

			revname = TextTranslator.TheInstance.Translate("Actual");
			max_revname = TextTranslator.TheInstance.Translate("Target");
			lost_revname = TextTranslator.TheInstance.Translate("Lost");
			transaction_name =  TextTranslator.TheInstance.Translate(SkinningDefs.TheInstance.GetData("transactionname"));

			Font font = ConstantSizeFont.NewFont(SkinningDefs.TheInstance.GetData("fontname"),9.5f, FontStyle.Bold);

			f1_Values = font;
			f1_Titles = font;
			f1_debug = ConstantSizeFont.NewFont(SkinningDefs.TheInstance.GetData("fontname"), 8f, FontStyle.Bold);

			Titles_Color = Color.White;
			Values_Color = Color.White;
			LostRevColor = Color.Orange;
			ActualRevColor = Color.LawnGreen;

			VulnerabilityStatsNode = model.GetNamedNode("VulnerabilityStats");
			VulnerabilityStatsNode.AttributesChanged +=VulnerabilityStatsNode_AttributesChanged;

			TransactionsNode = model.GetNamedNode("Transactions");
			TransactionsNode.AttributesChanged += TransactionsNode_AttributesChanged;

			projectedRevenueNode = model.GetNamedNode("Revenue");
			projectedRevenueNode.AttributesChanged += projectedRevenueNode_AttributesChanged;
		}

		/// <summary>
		/// Dispose ...
		/// </summary>
		public new void Dispose()
		{
			if (projectedRevenueNode != null)
			{
				projectedRevenueNode.AttributesChanged -= projectedRevenueNode_AttributesChanged;
				projectedRevenueNode = null;
			}
			if (TransactionsNode != null)
			{
				TransactionsNode.AttributesChanged -= TransactionsNode_AttributesChanged;
				TransactionsNode = null;
			}
			if (VulnerabilityStatsNode != null)
			{
				VulnerabilityStatsNode.AttributesChanged -=VulnerabilityStatsNode_AttributesChanged;
				VulnerabilityStatsNode = null;
			}
		}
	
		protected void  VulnerabilityStatsNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			this.Refresh();
		}

		void projectedRevenueNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			this.Refresh();
		}

		void TransactionsNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			this.Refresh();
		}

		string getTransactionsDisplay()
		{
			int trans_count_good = this.TransactionsNode.GetIntAttribute("count_good", 0);
			int trans_count_max = this.TransactionsNode.GetIntAttribute("count_max", 0);
			return CONVERT.ToStr(trans_count_good) + @"/" + CONVERT.ToStr(trans_count_max);
		}

		void getRevenue(out string max_revenue_str, out string good_revenue_str, out string lost_revenue_str)
		{
			//Just display in single Dollars for the time being 
			NumberFormatInfo nfi = new CultureInfo( "en-GB", false ).NumberFormat;
	
			int revenue = projectedRevenueNode.GetIntAttribute("revenue",0);
			string numberstr1 = revenue.ToString( "N", nfi );
			numberstr1 = numberstr1.Replace(".00","");
			good_revenue_str = "$" + numberstr1;

			int max_revenue = projectedRevenueNode.GetIntAttribute("max_revenue",0);
			string numberstr2 = max_revenue.ToString( "N", nfi );
			numberstr2 = numberstr2.Replace(".00","");
			max_revenue_str = "$" + numberstr2;

			int lost_revenue = projectedRevenueNode.GetIntAttribute("revenue_lost",0);
			string numberstr3 = lost_revenue.ToString( "N", nfi );
			numberstr3 = numberstr3.Replace(".00","");
			lost_revenue_str = "$" + numberstr3;
		}

		void getViolationsDisplay(out string prevented_count_str, out string fixed_count_str)
		{
			prevented_count_str = "";
			fixed_count_str = "";
			if (VulnerabilityStatsNode != null)
			{
				prevented_count_str = CONVERT.ToStr(VulnerabilityStatsNode.GetIntAttribute("prevented_nodes", 0));
				fixed_count_str = CONVERT.ToStr(VulnerabilityStatsNode.GetIntAttribute("fixed_nodes", 0));
			}
		}

		string formatDB(int numvalue)
		{
			NumberFormatInfo nfi = new CultureInfo("en-GB", false).NumberFormat;
			string numberstr2 = numvalue.ToString("N", nfi);
			numberstr2 = numberstr2.Replace(".00", "");
			numberstr2 = "$" + numberstr2;
			return numberstr2;
		}

		void Render_Transactions(Graphics g)
		{
			Brush textBrush = Brushes.Black;

			StringFormat sf = new StringFormat(StringFormatFlags.MeasureTrailingSpaces);

			int col1_titles_offset_x = 4;
			int col1_values_offset_x = 200;
			int col2_titles_offset_x = 210;
			int col2_values_offset_x = 408;

			int row1_offset_y = 22*0;
			int row2_offset_y = 22*1;
			int row3_offset_y = 22*2;
			int row4_offset_y = 22*3;

			string max_revenue_str="";
			string good_revenue_str="";
			string lost_revenue_str = "";
			string prevented_count_str = "";
			string fixed_count_str = "";

			getRevenue(out max_revenue_str, out good_revenue_str, out lost_revenue_str);
			getViolationsDisplay(out prevented_count_str, out fixed_count_str);

			SizeF text_size;
			int offset_x;

			using (Brush brush = new SolidBrush (Color.FromArgb(53, 123, 133)))
			{
				g.FillRectangle(brush, 0, row1_offset_y, Width, 20);
			}

			g.DrawString("Metrics", f1_Values, Brushes.White, col1_titles_offset_x, row1_offset_y);
			g.DrawString("Revenue", f1_Values, Brushes.White, col2_titles_offset_x, row1_offset_y);

			//Draw the Transactions 
			g.DrawString(transaction_name, f1_Values, textBrush, col1_titles_offset_x, row2_offset_y);
			string trans_value = getTransactionsDisplay();
			text_size = g.MeasureString(trans_value, f1_Values, this.Width, sf);
			offset_x = col1_values_offset_x - ((int)text_size.Width);
			g.DrawString(trans_value, f1_Values, textBrush, offset_x, row2_offset_y);

			//Draw the Prevented Violations 
			g.DrawString(prevented_violation_name, f1_Values, textBrush, col1_titles_offset_x, row3_offset_y);
			text_size = g.MeasureString(prevented_count_str, f1_Values, this.Width, sf);
			offset_x = col1_values_offset_x - ((int)text_size.Width);
			g.DrawString(prevented_count_str, f1_Values, textBrush, offset_x, row3_offset_y);

			//Draw the Fixed Violations 
			g.DrawString(fixed_violation_name, f1_Values, textBrush, col1_titles_offset_x, row4_offset_y);
			text_size = g.MeasureString(fixed_count_str, f1_Values, this.Width, sf);
			offset_x = col1_values_offset_x - ((int)text_size.Width);
			g.DrawString(fixed_count_str, f1_Values, textBrush, offset_x, row4_offset_y);

			//Draw the Max Value
			g.DrawString(max_revname, f1_Values, textBrush, col2_titles_offset_x, row2_offset_y);
			text_size = g.MeasureString(max_revenue_str, f1_Values, this.Width, sf);
			offset_x = (int) (col2_values_offset_x - text_size.Width);
			g.DrawString(max_revenue_str, f1_Values, textBrush, offset_x, row2_offset_y);

			//Draw the Good Revenue 
			g.DrawString(revname, f1_Values, Brushes.Green, col2_titles_offset_x, row3_offset_y);
			text_size = g.MeasureString(good_revenue_str, f1_Values, this.Width, sf);
			offset_x = (int) (col2_values_offset_x - text_size.Width);
			g.DrawString(good_revenue_str, f1_Values, Brushes.Green, offset_x, row3_offset_y);

			//Draw the Lost Revenue 
			g.DrawString(lost_revname, f1_Values, Brushes.Red, col2_titles_offset_x, row4_offset_y);
			text_size = g.MeasureString(lost_revenue_str, f1_Values, this.Width, sf);
			offset_x = (int) (col2_values_offset_x - text_size.Width);
			g.DrawString(lost_revenue_str, f1_Values, Brushes.Red, offset_x, row4_offset_y);
		}

		/// <summary>
		/// Override on Paint
		/// </summary>
		/// <param name="e"></param>
		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			Render_Transactions(g);
		}
	}
}
