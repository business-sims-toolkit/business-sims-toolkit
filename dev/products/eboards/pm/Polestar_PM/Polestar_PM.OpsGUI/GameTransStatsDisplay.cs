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
	/// Summary description for ProjectedRevenueView.
	/// </summary>
	public class GameTransStatsDisplay : FlickerFreePanel
	{
		static System.Globalization.CultureInfo myCI_enGB = new System.Globalization.CultureInfo( "en-GB", false );

		private Node RevenueNode = null;
		private Node RecoveryProcessNode = null;
		private Node RecoveryCostNode = null;
		private Node RecoveryFinesNode = null;

		private Font MyDefaultSkinFontBold10 = null;
		private Font MyDefaultSkinFontBold12 = null;
		private Font MyDefaultSkinFontBold14 = null;
		protected Image backImage = null;
		private NumberFormatInfo nfi = null;
		
		protected string MaxRevenueValueStr = "";
		protected string GainRevenueValueStr = "";
		protected string LostRevenueValueStr = "";
		protected string RecoveryCostValueStr = "";
		protected string FinesValueStr = "";
		protected bool showRecoveryStats = false;
		protected bool showTitle = true;
		protected Brush tmpTitleRedBrush = new SolidBrush(Color.FromArgb(153,0,0));		

		protected int corneringExtent = 10; //100 no gaps 
		
		/// <summary>
		/// Show Availability / Revenue
		/// </summary>
		/// <param name="model"></param>
		public GameTransStatsDisplay(NodeTree model, bool IsTrainingGame)
		{
			//Just display in GB number format for the time being 
			nfi = new CultureInfo( "en-GB", false ).NumberFormat;

			string fontname =  SkinningDefs.TheInstance.GetData("fontname");
			MyDefaultSkinFontBold10 = ConstantSizeFont.NewFont(fontname,10f,FontStyle.Bold);
			MyDefaultSkinFontBold12 = ConstantSizeFont.NewFont(fontname,12f,FontStyle.Bold);
			MyDefaultSkinFontBold14 = ConstantSizeFont.NewFont(fontname,14f,FontStyle.Bold);
			
			//this.BackColor = Color.DarkKhaki;
			Font f1_Values = MyDefaultSkinFontBold10;
			Font f1_Titles = MyDefaultSkinFontBold10;

			RevenueNode = model.GetNamedNode("Revenue");
			RevenueNode.AttributesChanged += new Network.Node.AttributesChangedEventHandler(RevenueNode_AttributesChanged);

			RecoveryProcessNode = model.GetNamedNode("RecoveryProcess"); 
			RecoveryProcessNode.AttributesChanged +=new Network.Node.AttributesChangedEventHandler(RecoveryProcessNode_AttributesChanged);

			RecoveryCostNode = model.GetNamedNode("RecoveryCost"); 
			RecoveryCostNode.AttributesChanged +=new Network.Node.AttributesChangedEventHandler(RecoveryCostNode_AttributesChanged);

			RecoveryFinesNode = model.GetNamedNode("RecoveryFines"); 
			RecoveryFinesNode.AttributesChanged	+=new Network.Node.AttributesChangedEventHandler(RecoveryFinesNode_AttributesChanged);

			UpdateFigures();
			Refresh();
		}

		/// <summary>
		/// Dispose ...
		/// </summary>
		public new void Dispose()
		{
			//timer.Stop();
			//timer.Dispose();
			if (tmpTitleRedBrush != null)
			{
				tmpTitleRedBrush.Dispose();
				tmpTitleRedBrush = null;
			}

			//get rid of the Font
			if (MyDefaultSkinFontBold10 != null)
			{
				MyDefaultSkinFontBold10.Dispose();
				MyDefaultSkinFontBold10 = null;
			}
			//get rid of the Font
			if (MyDefaultSkinFontBold12 != null)
			{
				MyDefaultSkinFontBold12.Dispose();
				MyDefaultSkinFontBold12 = null;
			}
			//get rid of the Font
			if (MyDefaultSkinFontBold14 != null)
			{
				MyDefaultSkinFontBold14.Dispose();
				MyDefaultSkinFontBold14 = null;
			}
			//Disconnect from the nodes
			if (RevenueNode != null)
			{
				RevenueNode.AttributesChanged -= new Network.Node.AttributesChangedEventHandler(RevenueNode_AttributesChanged);
				RevenueNode = null;
			}
			if (RecoveryProcessNode != null)
			{
				RecoveryProcessNode.AttributesChanged -= new Network.Node.AttributesChangedEventHandler(RecoveryProcessNode_AttributesChanged);
				RecoveryProcessNode = null;
			}

			if (RecoveryCostNode != null)
			{
				RecoveryCostNode.AttributesChanged -= new Network.Node.AttributesChangedEventHandler(RecoveryCostNode_AttributesChanged);
				RecoveryCostNode = null;
			}

			if (RecoveryFinesNode != null)
			{
				RecoveryFinesNode.AttributesChanged	-=new Network.Node.AttributesChangedEventHandler(RecoveryFinesNode_AttributesChanged);
				RecoveryFinesNode = null;
			}
		}

		private void UpdateFigures ()
		{
			int revenue_gain = RevenueNode.GetIntAttribute("revenue",0);
			int revenue_max = RevenueNode.GetIntAttribute("max_revenue",0);
			int revenue_lost = RevenueNode.GetIntAttribute("revenue_lost",0);

			int recovery_cost = RecoveryCostNode.GetIntAttribute("spend",0);
			int fines = RecoveryFinesNode.GetIntAttribute("fines",0);

			string tempstr = "";

			tempstr = revenue_gain.ToString( "N", nfi );
			tempstr = tempstr.Replace(".00","");
			GainRevenueValueStr = "$ " + tempstr;	

			tempstr = revenue_max.ToString( "N", nfi );
			tempstr = tempstr.Replace(".00","");
			MaxRevenueValueStr = "$ " + tempstr;	

			tempstr = revenue_lost.ToString( "N", nfi );
			tempstr = tempstr.Replace(".00","");
			LostRevenueValueStr = "$ " + tempstr;	

			tempstr = recovery_cost.ToString( "N", nfi );
			tempstr = tempstr.Replace(".00","");
			RecoveryCostValueStr = "$ " + tempstr;	

			tempstr = fines.ToString( "N", nfi );
			tempstr = tempstr.Replace(".00","");
			FinesValueStr = "$ " + tempstr;	
			
			this.Refresh();
		}

		private void RevenueNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			UpdateFigures();
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			//Setup the Quality and Rendering Options 
			e.Graphics.SmoothingMode = SmoothingMode.None;
			e.Graphics.CompositingQuality = CompositingQuality.HighQuality;
			e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
			int start_x =0;
			int offset_y = 20;
			SizeF textsize = new SizeF(0,0);
				
			string title = "TRANSACTIONS";

			//draw the background 
			e.Graphics.FillRectangle(Brushes.Black,0,0,this.Width, this.Height);
			//e.Graphics.FillRectangle(Brushes.Lime,0,0,this.Width, this.Height);

			if (showTitle)
			{
				e.Graphics.FillRectangle(tmpTitleRedBrush,2,2,this.Width-4, 18);
				textsize = new SizeF(0,0);
				textsize = e.Graphics.MeasureString(title,MyDefaultSkinFontBold10);
				start_x = (this.Width - (int)textsize.Width) / 2;
				//e.Graphics.DrawString("Daily Financials", MyDefaultSkinFontBold14, Brushes.DimGray,start_x,0);
				e.Graphics.DrawString(title, MyDefaultSkinFontBold10, Brushes.DimGray,start_x,0);
			}
			else
			{
				offset_y = 0;
			}

			//Display the Time 
			e.Graphics.DrawString("TIME",MyDefaultSkinFontBold10,Brushes.DimGray,2,5+offset_y);
			e.Graphics.DrawString("MADE",MyDefaultSkinFontBold10,Brushes.DimGray,52,5+offset_y);
			e.Graphics.DrawString("LOST",MyDefaultSkinFontBold10,Brushes.DimGray,102,5+offset_y);

			offset_y += 18;
			e.Graphics.DrawString("0900",MyDefaultSkinFontBold10,Brushes.DimGray,2,5+offset_y);
			e.Graphics.DrawString("11K",MyDefaultSkinFontBold10,Brushes.DimGray,52,5+offset_y);
			e.Graphics.DrawString(" 3K",MyDefaultSkinFontBold10,Brushes.DimGray,102,5+offset_y);

			offset_y += 18;
			e.Graphics.DrawString("1000",MyDefaultSkinFontBold10,Brushes.DimGray,2,5+offset_y);
			e.Graphics.DrawString("12K",MyDefaultSkinFontBold10,Brushes.DimGray,52,5+offset_y);
			e.Graphics.DrawString(" 5K",MyDefaultSkinFontBold10,Brushes.DimGray,102,5+offset_y);

			offset_y += 18;
			e.Graphics.DrawString("1100",MyDefaultSkinFontBold10,Brushes.DimGray,2,5+offset_y);
			e.Graphics.DrawString("12K",MyDefaultSkinFontBold10,Brushes.DimGray,52,5+offset_y);
			e.Graphics.DrawString(" 5K",MyDefaultSkinFontBold10,Brushes.DimGray,102,5+offset_y);

			offset_y += 18;
			e.Graphics.DrawString("1200",MyDefaultSkinFontBold10,Brushes.DimGray,2,5+offset_y);
			e.Graphics.DrawString("12K",MyDefaultSkinFontBold10,Brushes.DimGray,52,5+offset_y);
			e.Graphics.DrawString(" 5K",MyDefaultSkinFontBold10,Brushes.DimGray,102,5+offset_y);



// 0900  Made Lost Fines

			//Display the revenue Target
			//e.Graphics.DrawString(rev_target_title,MyDefaultSkinFontBold12,Brushes.DimGray,2,5+offset_y);
			//textsize = e.Graphics.MeasureString(MaxRevenueValueStr,MyDefaultSkinFontBold12);
			//start_x = (this.Width - (int)textsize.Width);
			//e.Graphics.DrawString(MaxRevenueValueStr,MyDefaultSkinFontBold12,Brushes.DimGray,start_x,5+offset_y);
			
			//Display the revenue Booked
			//e.Graphics.DrawString(rev_booked_title,MyDefaultSkinFontBold10,Brushes.Green,2,5+offset_y);
			//textsize = e.Graphics.MeasureString(GainRevenueValueStr,MyDefaultSkinFontBold10);
			//start_x = (this.Width - (int)textsize.Width);
			//e.Graphics.DrawString(GainRevenueValueStr,MyDefaultSkinFontBold10,Brushes.Green,start_x,5+offset_y);

			//Display the revenue lost
			//e.Graphics.DrawString(rev_lost_title,MyDefaultSkinFontBold10,Brushes.Red,2,25+offset_y);
			//textsize = e.Graphics.MeasureString(LostRevenueValueStr,MyDefaultSkinFontBold10);
			//start_x = (this.Width - (int)textsize.Width);
			//e.Graphics.DrawString(LostRevenueValueStr,MyDefaultSkinFontBold10,Brushes.Red,start_x,25+offset_y);

			//if (showRecoveryStats)
			//{
			//	//Display the Recovery Cost
			//	e.Graphics.DrawString(recovery_cost_title,MyDefaultSkinFontBold10,Brushes.DimGray,2,45+offset_y);
			//	textsize = e.Graphics.MeasureString(RecoveryCostValueStr,MyDefaultSkinFontBold10);
			//	start_x = (this.Width - (int)textsize.Width);
			//	e.Graphics.DrawString(RecoveryCostValueStr,MyDefaultSkinFontBold10,Brushes.DimGray,start_x,45+offset_y);
      //
			//	//Display the Fines
			//	e.Graphics.DrawString(fines_title,MyDefaultSkinFontBold10,Brushes.DimGray,2,65+offset_y);
			//	textsize = e.Graphics.MeasureString(FinesValueStr,MyDefaultSkinFontBold10);
			//	start_x = (this.Width - (int)textsize.Width);
			//	e.Graphics.DrawString(FinesValueStr,MyDefaultSkinFontBold10,Brushes.DimGray,start_x,65+offset_y);
			//}
		}

		private void RecoveryProcessNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			bool recovery_running = RecoveryProcessNode.GetBooleanAttribute("countdown_running",false);
			this.showRecoveryStats = recovery_running;
			if (recovery_running)
			{
				Refresh();
			}
		}

		private void RecoveryCostNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			UpdateFigures();
		}

		private void RecoveryFinesNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			UpdateFigures();
		}
	}
}
