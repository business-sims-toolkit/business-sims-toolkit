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
	public class RevenueAvail_Display : FlickerFreePanel
	{
		static System.Globalization.CultureInfo myCI_enGB = new System.Globalization.CultureInfo( "en-GB", false );

		private Node projectedRevenueNode = null;
		
		private string MaxRevenueTitle = "Max Revenue";
		private string MaxRevenueValue = "0";
		private Point[] MaxRevenuePolygon = null;

		private string GainRevenueTitle = "Gain Revenue";
		private string GainRevenueValue = "0";
		private Point[] GainRevenuePolygon = null;

		private string LostRevenueTitle = "Lost Revenue";
		private string LostRevenueValue = "0";
		private Point[] LostRevenuePolygon = null;
		
		private string AvailibilityTitle = "Availibility";
		private string AvailibilityValue = "0";
		private Point[] AvailibilityPolygon = null;

		private string ImpactTitle = "Impact";
		private string ImpactValue = "0";
		private Point[] ImpactPolygon = null;

		private string SLABreachesTitle = "SLA Breaches";
		private string SLABreachesValue = "0";
		private Point[] SLABreachesPolygon = null;

		private Node availabilityNode = null;
		private Node ImpactNode = null;
		private Node SLABreachNode = null;

		private Color PanelBackColor = Color.FromArgb(152,185,185);
		private Color TextDarkBackColor = Color.FromArgb(36,73,79);
		private Color TextLightBackColor = Color.FromArgb(179,199,199);
		private Color TextDarkForeColor = Color.FromArgb(42,75,80);
		private Color TextLightForeColor = Color.FromArgb(250,251,251);

		private Brush PanelBackBrush = new SolidBrush(Color.FromArgb(152,185,185));
		//private Brush TextDarkBackBrush =  new SolidBrush(Color.FromArgb(36,73,79));
		private Brush TextDarkBackBrush =  new SolidBrush(Color.FromArgb(61,81,88));
		private Brush TextLightBackBrush =  new SolidBrush(Color.FromArgb(179,199,199));
		private Brush TextDarkForeBrush =  new SolidBrush(Color.FromArgb(42,75,80));
		private Brush TextLightForeBrush =  new SolidBrush(Color.FromArgb(250,251,251));

		private int GeneralOffsetY = 5;
		private int StepHeight = 25-2; 

		private int TitleWidth = 100+17; 
		private int TitleHeight = 16+2;
		private int TitleOffsetX = 5+65-20-15+30; 
		private int ValueWidth = 90+15;		
		private int ValueHeight = 16+2;
		private int ValueOffsetX = 135+40-15+30; //135
		private Boolean DrawRectangles = false;
		private Font MyDefaultSkinFontBold10 = null;
		protected Image backImage = null;
		
		/// <summary>
		/// Show Availability / Revenue
		/// </summary>
		/// <param name="model"></param>
		public RevenueAvail_Display(NodeTree model)
		{
			string fontname =  SkinningDefs.TheInstance.GetData("fontname");
			//MyDefaultSkinFontBold10 = ConstantSizeFont.NewFont(fontname,10f,FontStyle.Bold);
			MyDefaultSkinFontBold10 = ConstantSizeFont.NewFont(fontname,12f,FontStyle.Bold);

			string back_image_str = "images\\panels\\RevAvail_Back.png";
			backImage = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + back_image_str);
			
			//this.BackColor = Color.DarkKhaki;
			Font f1_Values = MyDefaultSkinFontBold10;
			Font f1_Titles = MyDefaultSkinFontBold10;

			projectedRevenueNode = model.GetNamedNode("Revenue");
			projectedRevenueNode.AttributesChanged += new Network.Node.AttributesChangedEventHandler(projectedRevenueNode_AttributesChanged);

			availabilityNode = model.GetNamedNode("Availability");
			availabilityNode.AttributesChanged += new Network.Node.AttributesChangedEventHandler(availabilityNode_AttributesChanged);

			ImpactNode = model.GetNamedNode("Impact");
			ImpactNode.AttributesChanged += new Network.Node.AttributesChangedEventHandler(ImpactNode_AttributesChanged);

			SLABreachNode = model.GetNamedNode("SLA_Breach");
			SLABreachNode.AttributesChanged +=new Network.Node.AttributesChangedEventHandler(SLABreachNode_AttributesChanged);

			BuildTrailingRectangles();

			UpdateRevenueDisplay(false);
			UpdateBreachCountDisplay(false);
			UpdateImpactDisplay(false);
			UpdateAvailabilityDisplay(false);
			Refresh();
		}

		/// <summary>
		/// Dispose ...
		/// </summary>
		public new void Dispose()
		{
			if (MyDefaultSkinFontBold10 != null)
			{
				MyDefaultSkinFontBold10.Dispose();
				MyDefaultSkinFontBold10 = null;
			}

			if (projectedRevenueNode != null)
			{
				projectedRevenueNode.AttributesChanged -= new Network.Node.AttributesChangedEventHandler(projectedRevenueNode_AttributesChanged);
				projectedRevenueNode = null;
			}
			if (availabilityNode != null)
			{
				availabilityNode.AttributesChanged -= new Network.Node.AttributesChangedEventHandler(availabilityNode_AttributesChanged);
				availabilityNode = null;
			}
			if (ImpactNode != null)
			{
				ImpactNode.AttributesChanged -= new Network.Node.AttributesChangedEventHandler(projectedRevenueNode_AttributesChanged);
				ImpactNode = null;
			}
			if (SLABreachNode != null)
			{
				SLABreachNode.AttributesChanged -= new Network.Node.AttributesChangedEventHandler(SLABreachNode_AttributesChanged);
				SLABreachNode = null;
			}
		}

		private void UpdateRevenueDisplay(Boolean RefreshRequired)
		{
			//Just display in GB number format for the time being 
			NumberFormatInfo nfi = new CultureInfo( "en-GB", false ).NumberFormat;

			//int revenue = projectedRevenueNode.GetIntAttribute("revenue",0);
			//double revenueMillions = ((double)revenue)/1000000.0;
			//revenueLabel.Text = "Actual Revenue $" + CONVERT.ToStr(revenueMillions) + "M";
			int revenue = projectedRevenueNode.GetIntAttribute("revenue",0);
			string numberstr1 = revenue.ToString( "N", nfi );
			numberstr1 = numberstr1.Replace(".00","");
			GainRevenueValue = "$ " + numberstr1;

			int max_revenue = projectedRevenueNode.GetIntAttribute("max_revenue",0);
			string numberstr2 = max_revenue.ToString( "N", nfi );
			numberstr2 = numberstr2.Replace(".00","");
			MaxRevenueValue = "$ " + numberstr2;

			int lost_revenue = projectedRevenueNode.GetIntAttribute("revenue_lost",0);
			string numberstr3 = lost_revenue.ToString( "N", nfi );
			numberstr3 = numberstr3.Replace(".00","");
			LostRevenueValue = "$ " + numberstr3;
			if (RefreshRequired)
			{
				Refresh();
			}
		}

		private void projectedRevenueNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			UpdateRevenueDisplay(true);
		}

		private void UpdateAvailabilityDisplay(Boolean RefreshRequired)
		{
			double availability = availabilityNode.GetDoubleAttribute("availability",0.0);
			AvailibilityValue = CONVERT.ToStr(availability,1) + "%";
			if (RefreshRequired)
			{
				Refresh();
			}
		}

		private void availabilityNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			UpdateAvailabilityDisplay(true);
		}

		private void UpdateImpactDisplay(Boolean RefreshRequired)
		{
			double impact_level = this.ImpactNode.GetDoubleAttribute("impact",0.0);
			ImpactValue = CONVERT.ToStr(impact_level,0) + "%";
			if (RefreshRequired)
			{
				Refresh();
			}
		}

		private void ImpactNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			UpdateImpactDisplay(true);
		}

		private void UpdateBreachCountDisplay(Boolean RefreshRequired)
		{
			int sla_count = this.SLABreachNode.GetIntAttribute("biz_serv_count",0);
			SLABreachesValue = CONVERT.ToStr(sla_count,0);
			if (RefreshRequired)
			{
				Refresh();
			}
		}

		private void SLABreachNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			UpdateBreachCountDisplay(true);
		}

		private void BuildTrailingRectangles()
		{
			MaxRevenuePolygon = new Point[4];
			MaxRevenuePolygon[0] = new Point(TitleOffsetX-20,GeneralOffsetY+StepHeight*0);
			MaxRevenuePolygon[1] = new Point(TitleOffsetX+TitleWidth,GeneralOffsetY+StepHeight*0);
			MaxRevenuePolygon[2] = new Point(TitleOffsetX+TitleWidth,GeneralOffsetY+StepHeight*0+TitleHeight);
			MaxRevenuePolygon[3] = new Point(TitleOffsetX-15,GeneralOffsetY+StepHeight*0+TitleHeight);

			GainRevenuePolygon = new Point[4];
			GainRevenuePolygon[0] = new Point(TitleOffsetX-20,GeneralOffsetY+StepHeight*1);
			GainRevenuePolygon[1] = new Point(TitleOffsetX+TitleWidth,GeneralOffsetY+StepHeight*1);
			GainRevenuePolygon[2] = new Point(TitleOffsetX+TitleWidth,GeneralOffsetY+StepHeight*1+TitleHeight);
			GainRevenuePolygon[3] = new Point(TitleOffsetX-15,GeneralOffsetY+StepHeight*1+TitleHeight);

			LostRevenuePolygon = new Point[4];
			LostRevenuePolygon[0] = new Point(TitleOffsetX-20,GeneralOffsetY+StepHeight*2);
			LostRevenuePolygon[1] = new Point(TitleOffsetX+TitleWidth,GeneralOffsetY+StepHeight*2);
			LostRevenuePolygon[2] = new Point(TitleOffsetX+TitleWidth,GeneralOffsetY+StepHeight*2+TitleHeight);
			LostRevenuePolygon[3] = new Point(TitleOffsetX-15,GeneralOffsetY+StepHeight*2+TitleHeight);

			AvailibilityPolygon = new Point[4];
			AvailibilityPolygon[0] = new Point(TitleOffsetX-20,GeneralOffsetY+StepHeight*3);
			AvailibilityPolygon[1] = new Point(TitleOffsetX+TitleWidth,GeneralOffsetY+StepHeight*3);
			AvailibilityPolygon[2] = new Point(TitleOffsetX+TitleWidth,GeneralOffsetY+StepHeight*3+TitleHeight);
			AvailibilityPolygon[3] = new Point(TitleOffsetX-15,GeneralOffsetY+StepHeight*3+TitleHeight);

			ImpactPolygon = new Point[4];
			ImpactPolygon[0] = new Point(TitleOffsetX-20,GeneralOffsetY+StepHeight*4);
			ImpactPolygon[1] = new Point(TitleOffsetX+TitleWidth,GeneralOffsetY+StepHeight*4);
			ImpactPolygon[2] = new Point(TitleOffsetX+TitleWidth,GeneralOffsetY+StepHeight*4+TitleHeight);
			ImpactPolygon[3] = new Point(TitleOffsetX-15,GeneralOffsetY+StepHeight*4+TitleHeight);

			SLABreachesPolygon = new Point[4]; 
			SLABreachesPolygon[0] = new Point(TitleOffsetX-20,GeneralOffsetY+StepHeight*5);
			SLABreachesPolygon[1] = new Point(TitleOffsetX+TitleWidth,GeneralOffsetY+StepHeight*5);
			SLABreachesPolygon[2] = new Point(TitleOffsetX+TitleWidth,GeneralOffsetY+StepHeight*5+TitleHeight);
			SLABreachesPolygon[3] = new Point(TitleOffsetX-15,GeneralOffsetY+StepHeight*5+TitleHeight);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			//Setup the Quality and Rendering Options 
			e.Graphics.SmoothingMode = SmoothingMode.None;

//			if (DrawBackground)
//			{
//				e.Graphics.FillRectangle(PanelBackBrush, 0,0, this.Width, this.Height);
//				//e.Graphics.FillRectangle(, 0,0, this.Width, this.Height);
//				e.Graphics.DrawRectangle(Pens.Thistle, 0,0, this.Width-1, this.Height-1);
//			}
			
			if (backImage != null)
			{
				e.Graphics.DrawImage(backImage,0,0,300,155);
			}

			e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
			e.Graphics.TextRenderingHint = TextRenderingHint.AntiAlias;
			StringFormat format2 = new StringFormat();
			format2.Alignment = StringAlignment.Center;
			format2.LineAlignment = StringAlignment.Center;

			DrawRectangles = true;

			if (DrawRectangles)
			{
				//Draw the 	
				e.Graphics.FillRectangle(TextDarkBackBrush, TitleOffsetX, GeneralOffsetY+StepHeight*0, TitleWidth, TitleHeight);
				e.Graphics.DrawString(this.MaxRevenueTitle,MyDefaultSkinFontBold10,TextLightForeBrush,TitleOffsetX+1,GeneralOffsetY+StepHeight*0);

				e.Graphics.FillRectangle(TextDarkBackBrush, TitleOffsetX, GeneralOffsetY+StepHeight*1, TitleWidth, TitleHeight);
				e.Graphics.DrawString(this.GainRevenueTitle,MyDefaultSkinFontBold10,TextLightForeBrush,TitleOffsetX+1,GeneralOffsetY+StepHeight*1);

				e.Graphics.FillRectangle(TextDarkBackBrush, TitleOffsetX, GeneralOffsetY+StepHeight*2, TitleWidth, TitleHeight);
				e.Graphics.DrawString(this.LostRevenueTitle,MyDefaultSkinFontBold10,TextLightForeBrush,TitleOffsetX+1,GeneralOffsetY+StepHeight*2);

				e.Graphics.FillRectangle(TextDarkBackBrush, TitleOffsetX, GeneralOffsetY+StepHeight*3, TitleWidth, TitleHeight);
				e.Graphics.DrawString(this.AvailibilityTitle,MyDefaultSkinFontBold10,TextLightForeBrush,TitleOffsetX+1,GeneralOffsetY+StepHeight*3);

				e.Graphics.FillRectangle(TextDarkBackBrush, TitleOffsetX, GeneralOffsetY+StepHeight*4, TitleWidth, TitleHeight);
				e.Graphics.DrawString(this.ImpactTitle,MyDefaultSkinFontBold10,TextLightForeBrush,TitleOffsetX+1,GeneralOffsetY+StepHeight*4);

				e.Graphics.FillRectangle(TextDarkBackBrush, TitleOffsetX, GeneralOffsetY+StepHeight*5, TitleWidth, TitleHeight);
				e.Graphics.DrawString(this.SLABreachesTitle,MyDefaultSkinFontBold10,TextLightForeBrush,TitleOffsetX+1,GeneralOffsetY+StepHeight*5);

				//Values
				e.Graphics.FillRectangle(TextDarkBackBrush, ValueOffsetX, GeneralOffsetY+StepHeight*0, ValueWidth, ValueHeight);
				e.Graphics.DrawString(this.MaxRevenueValue,MyDefaultSkinFontBold10,TextLightForeBrush,ValueOffsetX+1,GeneralOffsetY+StepHeight*0);

				e.Graphics.FillRectangle(TextDarkBackBrush, ValueOffsetX, GeneralOffsetY+StepHeight*1, ValueWidth, ValueHeight);
				e.Graphics.DrawString(this.GainRevenueValue,MyDefaultSkinFontBold10,TextLightForeBrush,ValueOffsetX+1,GeneralOffsetY+StepHeight*1);

				e.Graphics.FillRectangle(TextDarkBackBrush, ValueOffsetX, GeneralOffsetY+StepHeight*2, ValueWidth, ValueHeight);
				e.Graphics.DrawString(this.LostRevenueValue,MyDefaultSkinFontBold10,TextLightForeBrush,ValueOffsetX+1,GeneralOffsetY+StepHeight*2);

				e.Graphics.FillRectangle(TextLightBackBrush, ValueOffsetX, GeneralOffsetY+StepHeight*3, ValueWidth, ValueHeight);
				e.Graphics.DrawString(this.AvailibilityValue,MyDefaultSkinFontBold10,TextDarkForeBrush,ValueOffsetX+1,GeneralOffsetY+StepHeight*3);

				e.Graphics.FillRectangle(TextLightBackBrush, ValueOffsetX, GeneralOffsetY+StepHeight*4, ValueWidth, ValueHeight);
				e.Graphics.DrawString(this.ImpactValue,MyDefaultSkinFontBold10,TextDarkForeBrush,ValueOffsetX+1,GeneralOffsetY+StepHeight*4);

				e.Graphics.FillRectangle(TextLightBackBrush, ValueOffsetX, GeneralOffsetY+StepHeight*5, ValueWidth, ValueHeight);
				e.Graphics.DrawString(this.SLABreachesValue,MyDefaultSkinFontBold10,TextDarkForeBrush,ValueOffsetX+1,GeneralOffsetY+StepHeight*5);



			}
//			else
//			{
//				e.Graphics.FillRectangle(TextDarkBackBrush, TitleOffsetX, GeneralOffsetY+StepHeight*0, TitleWidth, TitleHeight);
//				
//				if (MaxRevenuePolygon != null)
//				{
//					e.Graphics.FillRectangle(TextDarkBackBrush, TitleOffsetX, GeneralOffsetY+StepHeight*0, TitleWidth, TitleHeight);
//					//e.Graphics.FillPolygon(TextDarkBackBrush, MaxRevenuePolygon);
//					e.Graphics.FillPolygon(Brushes.Violet, MaxRevenuePolygon);
//					e.Graphics.DrawString(this.MaxRevenueTitle,MyDefaultSkinFontBold10,TextLightForeBrush,TitleOffsetX+1,GeneralOffsetY+StepHeight*0);
//				}
//
//				if (GainRevenuePolygon != null)
//				{
//					e.Graphics.FillRectangle(TextDarkBackBrush, TitleOffsetX, GeneralOffsetY+StepHeight*1, TitleWidth, TitleHeight);
//					//e.Graphics.FillPolygon(TextDarkBackBrush, MaxRevenuePolygon);
//					e.Graphics.FillPolygon(Brushes.Violet, GainRevenuePolygon);
//					e.Graphics.DrawString(GainRevenueTitle,MyDefaultSkinFontBold10,TextLightForeBrush,TitleOffsetX+1,GeneralOffsetY+StepHeight*1);
//				}
//
//				if (LostRevenuePolygon != null)
//				{
//					e.Graphics.FillRectangle(TextDarkBackBrush, TitleOffsetX, GeneralOffsetY+StepHeight*2, TitleWidth, TitleHeight);
//					//e.Graphics.FillPolygon(TextDarkBackBrush, MaxRevenuePolygon);
//					e.Graphics.FillPolygon(Brushes.Violet, LostRevenuePolygon);
//					e.Graphics.DrawString(LostRevenueTitle,MyDefaultSkinFontBold10,TextLightForeBrush,TitleOffsetX+1,GeneralOffsetY+StepHeight*2);
//				}
//
//				if (AvailibilityPolygon != null)
//				{
//					e.Graphics.FillRectangle(TextDarkBackBrush, TitleOffsetX, GeneralOffsetY+StepHeight*3, TitleWidth, TitleHeight);
//					//e.Graphics.FillPolygon(TextDarkBackBrush, MaxRevenuePolygon);
//					e.Graphics.FillPolygon(Brushes.Violet, AvailibilityPolygon);
//					e.Graphics.DrawString(AvailibilityTitle,MyDefaultSkinFontBold10,TextLightForeBrush,TitleOffsetX+1,GeneralOffsetY+StepHeight*3);
//
//				}
//				if (ImpactPolygon != null)
//				{
//					e.Graphics.FillRectangle(TextDarkBackBrush, TitleOffsetX, GeneralOffsetY+StepHeight*4, TitleWidth, TitleHeight);
//					//e.Graphics.FillPolygon(TextDarkBackBrush, MaxRevenuePolygon);
//					e.Graphics.FillPolygon(Brushes.Violet, ImpactPolygon);
//					e.Graphics.DrawString(ImpactTitle,MyDefaultSkinFontBold10,TextLightForeBrush,TitleOffsetX+1,GeneralOffsetY+StepHeight*4);
//				}
//				if (SLABreachesPolygon != null)
//				{
//					e.Graphics.FillRectangle(TextDarkBackBrush, TitleOffsetX, GeneralOffsetY+StepHeight*5, TitleWidth, TitleHeight);
//					//e.Graphics.FillPolygon(TextDarkBackBrush, MaxRevenuePolygon);
//					e.Graphics.FillPolygon(Brushes.Violet, SLABreachesPolygon);
//					e.Graphics.DrawString(SLABreachesTitle,MyDefaultSkinFontBold10,TextLightForeBrush,TitleOffsetX+1,GeneralOffsetY+StepHeight*5);
//				}
//				
//				//Values
//				e.Graphics.FillRectangle(TextDarkBackBrush, ValueOffsetX, GeneralOffsetY+StepHeight*0, ValueWidth, ValueHeight);
//				e.Graphics.DrawString(this.MaxRevenueValue,MyDefaultSkinFontBold10,TextLightForeBrush,ValueOffsetX+1,GeneralOffsetY+StepHeight*0);
//
//				e.Graphics.FillRectangle(TextDarkBackBrush, ValueOffsetX, GeneralOffsetY+StepHeight*1, ValueWidth, ValueHeight);
//				e.Graphics.DrawString(this.GainRevenueValue,MyDefaultSkinFontBold10,TextLightForeBrush,ValueOffsetX+1,GeneralOffsetY+StepHeight*1);
//
//				e.Graphics.FillRectangle(TextDarkBackBrush, ValueOffsetX, GeneralOffsetY+StepHeight*2, ValueWidth, ValueHeight);
//				e.Graphics.DrawString(this.LostRevenueValue,MyDefaultSkinFontBold10,TextLightForeBrush,ValueOffsetX+1,GeneralOffsetY+StepHeight*2);
//
//				e.Graphics.FillRectangle(TextLightBackBrush, ValueOffsetX, GeneralOffsetY+StepHeight*3, ValueWidth, ValueHeight);
//				e.Graphics.DrawString(this.AvailibilityValue,MyDefaultSkinFontBold10,TextDarkForeBrush,ValueOffsetX+1,GeneralOffsetY+StepHeight*3);
//
//				e.Graphics.FillRectangle(TextLightBackBrush, ValueOffsetX, GeneralOffsetY+StepHeight*4, ValueWidth, ValueHeight);
//				e.Graphics.DrawString(this.ImpactValue,MyDefaultSkinFontBold10,TextDarkForeBrush,ValueOffsetX+1,GeneralOffsetY+StepHeight*4);
//
//				e.Graphics.FillRectangle(TextLightBackBrush, ValueOffsetX, GeneralOffsetY+StepHeight*5, ValueWidth, ValueHeight);
//				e.Graphics.DrawString(this.SLABreachesValue,MyDefaultSkinFontBold10,TextDarkForeBrush,ValueOffsetX+1,GeneralOffsetY+StepHeight*5);
//			}
		}
	}
}
