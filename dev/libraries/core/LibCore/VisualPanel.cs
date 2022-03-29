using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;

using System.IO;

namespace LibCore
{
	public class BaseClass
	{
		protected static TextWriter tw =null;
		public static TextWriter GetTextWriter() { return tw; }
		
		public static void StartClassLog()
		{
		}

		public static void StopClassLog()
		{
		}

		public BaseClass()
		{
		}
	}

	public class BasePanel : Panel, IDoubleBufferable
	{
		protected static TextWriter tw = null;
		public static TextWriter GetTextWriter() { return tw; }

		public BasePanel()
		{
		}
	

		public static void WipeControl(Control main)
		{
		}

		static List<Control> GetAllChildren (Control control)
		{
			List<Control> controls = new List<Control>();

			if (control != null)
			{
				foreach (Control child in control.Controls)
				{
					controls.Add(child);
					controls.AddRange(GetAllChildren(child));
				}
			}

			return controls;
		}

		public virtual void SetFlickerFree (bool flickerFree)
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint, flickerFree);
			SetStyle(ControlStyles.UserPaint, flickerFree);
			SetStyle(ControlStyles.DoubleBuffer, flickerFree);

			foreach (Control control in GetAllChildren(this))
			{
				IDoubleBufferable panel = control as IDoubleBufferable;

				if (panel != null)
				{
					panel.SetFlickerFree(flickerFree);
				}
			}
		}
	}

	public class VisualPanel : BasePanel, IPrint
	{
		protected GradientControlBackground backgroundFill;

		Pen pen = new Pen(Color.Black, 1);
		protected int _px = 0;
		protected int _py = 0;

		public VisualPanel()
		{
		}

		public void ClearStroke()
		{
		}

		public void CurveTo(int cx, int cy, int x, int y)
		{
		}

		public void SetStroke(int width, Color c)
		{
		    if (pen != null)
		    {
		        pen.Dispose();
		    }

			pen = new Pen(c,width);
		}

		public void MoveTo(int x, int y)
		{
			_px = x;
			_py = y;
		}

		public SizeF MeasureString(Font f, string text)
		{
			System.Drawing.Graphics g = this.CreateGraphics();
			SizeF sf =  g.MeasureString(text,f);
			g.Dispose();
			return sf;
		}

		public void SetDashedOrDotted (bool dashed, bool dotted)
		{
			if (dashed)
			{
				pen.DashStyle = DashStyle.Dash;
			}
			else if (dotted)
			{
				pen.DashStyle = DashStyle.Dot;
			}
			else
			{
				pen.DashStyle = DashStyle.Solid;
			}
		}

		public void LineTo(System.Windows.Forms.PaintEventArgs e, int x, int y)
		{
			e.Graphics.DrawLine(pen,_px,_py,x,y);
			_px = x;
			_py = y;
		}

		public void DrawRectangle(System.Windows.Forms.PaintEventArgs e, int x, int y, int width, int height)
		{
			e.Graphics.DrawRectangle(pen,x,y,width,height);
		}

		public void PrintAllComponents(PaintEventArgs e)
		{
			PrintAllComponents(this, e);
		}

		protected void PrintAllComponents(Control c, PaintEventArgs e)
		{
			IPrint p = c as IPrint;
			Rectangle r = e.ClipRectangle; // Should we adjust the clip rectangle?
			Matrix transform = e.Graphics.Transform.Clone(); 

			if(null != p)
			{
				p.DrawToHDC(e);
			}

			foreach(Control cc in c.Controls)
			{
				e.Graphics.Transform = transform.Clone();
				e.Graphics.TranslateTransform(cc.Left,cc.Top);
				PrintAllComponents(cc,e);
			}
		}

		public Bitmap DrawToBitmap()
		{
			Bitmap bmp = new Bitmap(Width,Height);
			Graphics g = Graphics.FromImage(bmp);
			g.CompositingQuality = CompositingQuality.HighQuality;
			g.SmoothingMode = SmoothingMode.HighQuality;
			System.Windows.Forms.PaintEventArgs e = new PaintEventArgs(g,new Rectangle(0,0,Width,Height));
			PrintAllComponents(this,e);
			g.Dispose();
			return bmp;
		}

		public void DrawToHDC(PaintEventArgs e)
		{
			OnPaint(e);
		}

		protected override void OnPaint (PaintEventArgs e)
		{
			base.OnPaint(e);

			if (backgroundFill != null)
			{
				backgroundFill.Draw(this, e.Graphics);
			}
		}

		public void SetBackgroundFill (GradientControlBackground background)
		{
			this.backgroundFill = background;

			foreach (Control control in Controls)
			{
				VisualPanel panel = control as VisualPanel;
				if (panel != null)
				{
					panel.SetBackgroundFill(backgroundFill);
				}
			}

			Invalidate();
		}

		public GradientControlBackground BackgroundFill
		{
			get
			{
				return backgroundFill;
			}

			set
			{
				SetBackgroundFill(value);
			}
		}

		protected override void OnControlAdded (ControlEventArgs e)
		{
			base.OnControlAdded(e);
			
			VisualPanel panel = e.Control as VisualPanel;
			if (panel != null)
			{
				panel.SetBackgroundFill(backgroundFill);
			}
		}
	}
}