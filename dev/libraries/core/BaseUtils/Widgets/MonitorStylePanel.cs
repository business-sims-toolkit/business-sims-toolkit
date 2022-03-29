using System;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Text;

using LibCore;

namespace BaseUtils
{
	/// <summary>
	/// A simple panel with a title in Blue.
	/// </summary>
	public class MonitorStylePanel : Panel
	{
		string title;
		int titleHeight = 28;

		public MonitorStylePanel()
		{
			this.SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.DoubleBuffer, true);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
			RenderBorder(e.Graphics);
			base.OnPaint (e);
		}

		protected override void OnResize(EventArgs e)
		{
			Invalidate();
			base.OnResize (e);
		}

		void RenderBorder(Graphics g)
		{
			Color hpBlue = Color.FromArgb(10, 53, 126);
			Font f = ConstantSizeFont.NewFont("Tahoma", 12f, FontStyle.Bold);
			g.FillRectangle(new SolidBrush(hpBlue), 0, 0, this.Width, this.Height);
			g.FillRectangle(Brushes.Black, 2, titleHeight, this.Width - 4, this.Height - titleHeight - 2);
			g.DrawString(title, f, Brushes.White, 4, 4);
			f.Dispose();
		}

		public string Title
		{
			get { return title; }
			set { title = value; }
		}
	}
}
