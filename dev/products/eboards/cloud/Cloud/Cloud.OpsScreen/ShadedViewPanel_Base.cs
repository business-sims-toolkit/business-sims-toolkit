using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using CoreUtils;
using LibCore;
using Network;
using CommonGUI;

namespace Cloud.OpsScreen
{
	public enum emShadedViewPanelDisplayMode
	{
		NORMAL,
		FOLDER_HALFTITLE,
		ROUNDED_CORNERS,
	}

	public class ShadedViewPanel_Base : FlickerFreePanel
	{
		[DllImport("User32.dll", CharSet = CharSet.Auto)]
		private static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool redraw);

		protected List<Point> maskBoundingPoints;
		protected Bitmap FullBackgroundImage;
		protected Bitmap ControlBackgroundImage;

		protected Rectangle rectTitleText = new Rectangle(0, 0, 100, 20);
		protected Rectangle rectMainText = new Rectangle(0, 20, 100, 80);
		protected Color color_ShadeTitle = Color.FromArgb(128, 0, 128, 128);
		protected Color color_ShadeMain = Color.FromArgb(128, 32, 32, 32);

		protected Color color_ShadeTitle_Training = Color.FromArgb(128, 188, 75, 244);
		protected Color color_ShadeMain_Training = Color.FromArgb(128, 188, 75, 0);

		protected Color color_OrangeRed = Color.FromArgb(0, 255, 51, 0);
		
		protected Color color_TitleFore = Color.White;
		protected Color color_BodyFore = Color.AliceBlue;

		protected Brush br_hiWhite = new SolidBrush(Color.FromArgb(224, 224, 224));
		protected Brush br_hiRed = new SolidBrush(Color.FromArgb(255, 0, 0));
		protected Brush br_hiGreen = new SolidBrush(Color.FromArgb(102, 204, 0));
		protected Brush br_hiAmber = new SolidBrush(Color.FromArgb(255, 204, 0));
		protected Brush br_hiOrangeRed = new SolidBrush(Color.FromArgb(255, 51, 0));

		protected SolidBrush brush_Title = null;
		protected SolidBrush brush_Body = null;

		protected Font Font_Title;
		protected Font Font_Body;

		protected string TitleText = "TitleXXX";
		protected bool showTitleBack = true;
		protected bool showConstruction = false;

		protected NodeTree _model = null;
		protected Node roundVariables = null; 
		protected int CurrentRound = 1;
		protected bool isTraining = false;

		protected int Shade_Level = 240;
		protected int chamfer_level = 3;
		protected int title_height = 20;
		protected emShadedViewPanelDisplayMode currentDisplayMode = emShadedViewPanelDisplayMode.ROUNDED_CORNERS;

		public ShadedViewPanel_Base(NodeTree nt, bool isTraining)
		{
			_model = nt;
			this.isTraining = isTraining;

			Node roundVariables = _model.GetNamedNode("RoundVariables");
			CurrentRound = roundVariables.GetIntAttribute("current_round", 0);

			// wanted the traing colours switched off for the SVP panels  

			//if (this.isTraining == false)
			//{
				color_ShadeTitle = Color.FromArgb(Shade_Level, color_ShadeTitle.R, color_ShadeTitle.G, color_ShadeTitle.B);
				color_ShadeMain = Color.FromArgb(Shade_Level, color_ShadeMain.R, color_ShadeMain.G, color_ShadeMain.B);
			//}
			//else
			//{
			//  color_ShadeTitle = Color.FromArgb(Shade_Level, color_ShadeTitle_Training.R, color_ShadeTitle_Training.G, color_ShadeTitle_Training.B);
			//  color_ShadeMain = Color.FromArgb(Shade_Level, color_ShadeMain_Training.R, color_ShadeMain_Training.G, color_ShadeMain_Training.B);
			//}

			FullBackgroundImage = (Bitmap)Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\panels\\map.png");

			string font = SkinningDefs.TheInstance.GetData("fontname");
			Font_Title = FontRepository.GetFont(font, 12, FontStyle.Bold);
			Font_Body = FontRepository.GetFont(font, 10, FontStyle.Regular);

			//brush_Title = new SolidBrush(color_TitleFore);
			//brush_Body = new SolidBrush(color_BodyFore);
			brush_Title = new SolidBrush(Color.FromArgb(224, 224, 224));
			brush_Body = new SolidBrush(Color.FromArgb(224, 224, 224));
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{

				if (br_hiWhite != null)
				{
					br_hiWhite.Dispose();
				}
				if (br_hiRed != null)
				{
					br_hiRed.Dispose();
				}
				if (br_hiGreen != null)
				{
					br_hiGreen.Dispose();
				}
				if (br_hiAmber != null)
				{
					br_hiAmber.Dispose();
				}
				if (br_hiOrangeRed != null)
				{
					br_hiOrangeRed.Dispose();
				}

				if (brush_Title != null)
				{
					brush_Title.Dispose(); 
				}
				if (brush_Body != null)
				{
					brush_Body.Dispose();
				}
			}
		}

		protected void setDisplayMode(emShadedViewPanelDisplayMode newMode)
		{
			currentDisplayMode = newMode;
			SetupDefaultMaskBounds();
		}

		public void SetTitle(string NewTitle)
		{
			TitleText = NewTitle;
		}

		protected void ExtractControlBack(int x, int y, int w, int h)
		{
			if (ControlBackgroundImage != null)
			{
				ControlBackgroundImage.Dispose();
			}

			rectTitleText.Width = w;
			rectMainText.Width = w;
			rectMainText.Height = h - title_height;

			ControlBackgroundImage = new Bitmap(w, h);
			Graphics g = Graphics.FromImage(ControlBackgroundImage);

			g.SmoothingMode = SmoothingMode.AntiAlias;
			g.CompositingQuality = CompositingQuality.HighQuality;
			g.PixelOffsetMode = PixelOffsetMode.HighQuality;
			//Redraw
			Rectangle destRect = new Rectangle(0,0,w,h);
			Rectangle srcRect = new Rectangle(x, y, w, h);

			g.DrawImage(FullBackgroundImage, destRect, srcRect, GraphicsUnit.Pixel);
			
			//SolidBrush sb = new SolidBrush(Color.FromArgb(128, 0, 0, 0));
			if (showConstruction)
			{
				Pen sp = new Pen(Color.FromArgb(Shade_Level, 255, 0, 0));
				g.DrawRectangle(sp, 1, 1, w - 2, h - 2);
				sp.Dispose();
			}

			if (showTitleBack)
			{
				SolidBrush sb1 = new SolidBrush(color_ShadeTitle);
				g.FillRectangle(sb1, rectTitleText);
				sb1.Dispose();
			}
			else
			{
				SolidBrush sb1 = new SolidBrush(color_ShadeMain);
				g.FillRectangle(sb1, rectTitleText);
				sb1.Dispose();
			}
			SolidBrush sb2 = new SolidBrush(color_ShadeMain);
			g.FillRectangle(sb2, rectMainText);
			sb2.Dispose();

			//dispose the Graphics 
			g.Dispose();

			BackgroundImage = ControlBackgroundImage;
		}

		public void SetDisplayPositionAndExtent(int x, int y, int w, int h)
		{
			ExtractControlBack(x, y, w, h);
			Location = new Point(x, y);
			Size = new Size(w, h);
		}

		protected virtual void SetupDefaultMaskBounds()
		{
			Point[] pp;

			switch (currentDisplayMode)
			{ 
				case emShadedViewPanelDisplayMode.NORMAL:
					pp = new Point[4];
					pp[0] = new Point(0, 0);
					pp[1] = new Point(Width, 0);
					pp[2] = new Point(Width, Height);
					pp[3] = new Point(0, Height);
					SetMaskBoundingPolygon(pp);
					break;
				case emShadedViewPanelDisplayMode.FOLDER_HALFTITLE:
					pp = new Point[11];
					pp[0] = new Point(chamfer_level, 0);
					pp[1] = new Point(Width / 2, 0);
					pp[2] = new Point((Width / 2) + chamfer_level, chamfer_level);
					pp[3] = new Point((Width / 2) + chamfer_level, title_height);
					pp[4] = new Point((Width - chamfer_level), title_height);
					pp[5] = new Point((Width), title_height + chamfer_level);
					pp[6] = new Point((Width), Height - chamfer_level);
					pp[7] = new Point((Width - chamfer_level), Height);
					pp[8] = new Point(chamfer_level, Height);
					pp[9] = new Point(0, Height - chamfer_level);
					pp[10] = new Point(0, chamfer_level);

					SetMaskBoundingPolygon(pp);
					break;
				case emShadedViewPanelDisplayMode.ROUNDED_CORNERS:
					pp = new Point[8];
					pp[0] = new Point(chamfer_level, 0);
					pp[1] = new Point((Width - chamfer_level), 0);
					pp[2] = new Point((Width), 0 + chamfer_level);
					pp[3] = new Point((Width), Height - chamfer_level);
					pp[4] = new Point((Width - chamfer_level), Height);
					pp[5] = new Point(chamfer_level, Height);
					pp[6] = new Point(0, Height - chamfer_level);
					pp[7] = new Point(0, chamfer_level);
					SetMaskBoundingPolygon(pp);
					break;
			}
		}

		protected virtual Point[] getMaskBoundingPolygon()
		{
			return maskBoundingPoints.ToArray();
		}

		public void SetMaskBoundingPolygon(Point[] points)
		{
			maskBoundingPoints = new List<Point>(points);
			DoMask();
		}

		protected void DoMask()
		{
			Point[] pp = getMaskBoundingPolygon();

			if (pp != null)
			{
				if (pp.Length > 0)
				{
					GraphicsPath mPath = new GraphicsPath();
					mPath.AddPolygon(pp);
					//crreate a region from the Path 
					Region region = new Region(mPath);
					//create a graphics object 
					Graphics graphics = CreateGraphics();
					//get the handle to the region 
					IntPtr ptr = region.GetHrgn(graphics);
					//Call the Win32 window region code 
					SetWindowRgn((IntPtr)Handle, ptr, true);
				}
			}
		}

		protected void DoSize()
		{
			SetupDefaultMaskBounds();
		}

		protected override void OnSizeChanged(EventArgs e)
		{
			DoSize();
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			e.Graphics.FillRectangle(Brushes.Black, 0, 0, Width, Height);
			if (ControlBackgroundImage != null)
			{
				e.Graphics.DrawImageUnscaled(BackgroundImage, 0, 0, ControlBackgroundImage.Width, ControlBackgroundImage.Height);
			}
			if (string.IsNullOrEmpty(TitleText)==false)
			{
				e.Graphics.DrawString(TitleText, Font_Title, brush_Title, 0 , 0);
			}

		}

	}
}
