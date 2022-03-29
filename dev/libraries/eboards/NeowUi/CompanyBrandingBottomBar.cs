using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;

using CommonGUI;

namespace NeowUi
{
	public class CompanyBrandingBottomBar : FlickerFreePanel
	{
		int logoRightEdge;

		public CompanyBrandingBottomBar ()
		{
			logoRightEdge = 20;
		}
		
		protected override void OnPaint (PaintEventArgs e)
		{
			base.OnPaint(e);

			using (Brush brush = new LinearGradientBrush (FillRegion,
														  FillColour1,
														  FillColour2,
														  FillAngle))
			{
				e.Graphics.FillRectangle(brush, 0, 0, Width - 1, Height - 1);
			}

			Image image = LibCore.Repository.TheInstance.GetImage(LibCore.AppInfo.TheInstance.Location + @"\images\panels\poweredbyCompany.png");

			e.Graphics.DrawImage(image, new Rectangle (Width - image.Width - logoRightEdge, (Height - image.Height) / 2, image.Width, image.Height));
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);
			Invalidate();
		}

		public int LogoRightEdge
		{
			get
			{
				return logoRightEdge;
			}

			set
			{
				logoRightEdge = value;
				Invalidate();
			}
		}

		public Color FillColour1
		{
			get
			{
				return Color.FromArgb(221, 221, 221);
			}
		}

		public Color FillColour2
		{
			get
			{
				return Color.FromArgb(89, 89, 89);
			}
		}

		public Rectangle FillRegion
		{
			get
			{
				return new Rectangle (0, 0, Width, Height);
			}
		}

		public int FillAngle
		{
			get
			{
				return 90;
			}
		}
	}
}