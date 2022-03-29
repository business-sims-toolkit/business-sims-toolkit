using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CoreUtils;

namespace BaseUtils
{
	public class GradientPanel : Panel
	{
		Color BaseColor = Color.LightGray;
		Color BaseDarkColor = Color.DarkGray;
		Color BaseTintColor = Color.White;

	    int? autoHeight;

		public GradientPanel()
		{
			this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.DoubleBuffer | ControlStyles.SupportsTransparentBackColor | ControlStyles.UserPaint, true);
			this.BackColor = Color.Transparent;

		    var graphic = CoreUtils.SkinningDefs.TheInstance.GetData("gradient_panels_tab_graphic");
            if (! string.IsNullOrEmpty(graphic))
		    {
		        Image middle = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + @"\images\tabs\" + graphic);
		        autoHeight = middle.Height;
		    }
		    else if (CoreUtils.SkinningDefs.TheInstance.GetBoolData("draw_gradient_panels_using_tab_graphics", false))
		    {
		        Image middle = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + @"\images\tabs\tab_central_selected.png");
		        autoHeight = middle.Height;
		    }
		    else
		    {
		        autoHeight = SkinningDefs.TheInstance.GetIntData("graduated_panel_height", 40);
		    }
        }

        public GradientPanel (Color NewBaseColor, Color NewBaseDarkColor, Color NewBaseTintColor)
            : this ()
		{
			BaseColor = NewBaseColor;
			BaseDarkColor = NewBaseDarkColor;
			BaseTintColor = NewBaseTintColor;
		}

		public GradientPanel(Color NewBaseColor, Color NewBaseDarkColor)
			: this(NewBaseColor, NewBaseDarkColor, Color.White)
		{
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			RenderPanel(e.Graphics);
		}

		protected override void OnResize(EventArgs e)
		{
			Invalidate();
			base.OnResize (e);
		}

		void RenderPanel (Graphics g)
		{
		    var graphic = CoreUtils.SkinningDefs.TheInstance.GetData("gradient_panels_tab_graphic");
            if (! string.IsNullOrEmpty(graphic))
			{
				Image left = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + @"\images\tabs\tab_left_end.png");
				Image middle = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + @"\images\tabs\" + graphic);
				Image right = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + @"\images\tabs\tab_right_end.png");

				g.DrawImage(left, 0, 0, left.Width, left.Height);
				for (int x = left.Width; x < (Width - right.Width); x += middle.Width)
				{
				    var widthRemaining = Math.Min(middle.Width, Width - right.Width - 1 - x);
					g.DrawImage(middle, new Rectangle  (x, 0, widthRemaining, middle.Height), new Rectangle (0, 0, widthRemaining, middle.Height), GraphicsUnit.Pixel);
				}
				g.DrawImage(right, Width - right.Width - 1, 0, right.Width, right.Height);
			}
		    else if (CoreUtils.SkinningDefs.TheInstance.GetBoolData("draw_gradient_panels_using_tab_graphics", false))
		    {
		        Image left = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + @"\images\tabs\tab_left_end_selected.png");
		        Image middle = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + @"\images\tabs\tab_central_selected.png");
		        Image right = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + @"\images\tabs\tab_right_end_selected.png");

		        g.DrawImage(left, 0, 0, left.Width, left.Height);
		        for (int x = left.Width; x < (Width - right.Width); x += middle.Width)
		        {
		            var widthRemaining = Math.Min(middle.Width, Width - right.Width - 1 - x);
		            g.DrawImage(middle, new Rectangle(x, 0, widthRemaining, middle.Height), new Rectangle(0, 0, widthRemaining, middle.Height), GraphicsUnit.Pixel);
		        }
                g.DrawImage(right, Width - right.Width - 1, 0, right.Width, right.Height);
            }
            else if (CoreUtils.SkinningDefs.TheInstance.GetBoolData("draw_gradient_panels_using_graduation", false))
			{
				Rectangle topHalf = new Rectangle (0, 0, Width, Height / 2);
				Rectangle bottomHalf = new Rectangle (0, topHalf.Bottom, Width, Height - topHalf.Bottom);
		
				using (LinearGradientBrush brush = new LinearGradientBrush (topHalf,
				                                                            Color.FromArgb(0, 80, 149),
																			Color.FromArgb(38, 147, 255),
																			90))
				{
					g.FillRectangle(brush, topHalf);
				}

				using (LinearGradientBrush brush = new LinearGradientBrush (bottomHalf,
																			Color.FromArgb(38, 147, 255),
																			Color.FromArgb(0, 80, 149),
																			90))
				{
					g.FillRectangle(brush, bottomHalf);
				}
			}
			else
            {
	            int arcSize = SkinningDefs.TheInstance.GetIntData("gradient_panels_arc_size", 75);

				GraphicsPath p1 = new GraphicsPath();
				p1.FillMode = FillMode.Winding;
				p1.AddArc(0, 0, arcSize, arcSize, 180, 90);
				p1.AddArc(this.ClientRectangle.Width - arcSize, 0, arcSize, arcSize, 270, 90);

				GraphicsPath p2 = new GraphicsPath();
				p2.FillMode = FillMode.Winding;
				p2.AddArc(1, 1, arcSize - 2, arcSize - 2, 180, 90);
				p2.AddArc(this.ClientRectangle.Width - arcSize, 1, arcSize - 2, arcSize - 2, 270, 90);

				Rectangle top = new Rectangle(0, 0, this.ClientRectangle.Width, (arcSize / 2) + 1);
				LinearGradientBrush b = new LinearGradientBrush(top, BaseColor, BaseTintColor, LinearGradientMode.Vertical);

				g.SmoothingMode = SmoothingMode.AntiAlias;
				g.FillPath(b, p1);
				g.DrawPath(new Pen(BaseDarkColor, 1f), p1);
				g.DrawPath(new Pen(BaseTintColor, 1f), p2);
			    using (SolidBrush brush = new SolidBrush(BaseTintColor))
			    {
			        g.FillRectangle(brush, 0, arcSize / 2, this.ClientRectangle.Width,
			            this.ClientRectangle.Height - (arcSize / 2));
			    }

			    b.Dispose();
				p1.Dispose();
				p2.Dispose();
			}
		}

	    public void SetAutoSize ()
	    {
	        if (autoHeight.HasValue)
	        {
	            Height = autoHeight.Value;
	        }
	    }
	}
}