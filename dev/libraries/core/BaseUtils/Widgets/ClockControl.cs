using System;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace BaseUtils
{
	/// <summary>
	/// User Control for displaying the time.
	/// </summary>
	public class ClockControl : Control
	{
		protected string time;
		protected Brush brush;

		/// <summary>
		/// Creates an instance of ClockControl.
		/// </summary>
		public ClockControl()
		{
			this.time = "00:00";
			this.brush = new SolidBrush(this.ForeColor);
			this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.DoubleBuffer | ControlStyles.SupportsTransparentBackColor | ControlStyles.UserPaint, true);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			RenderClock(e.Graphics);
		}

		protected override void OnForeColorChanged(EventArgs e)
		{
			this.brush = new SolidBrush(this.ForeColor);
			base.OnForeColorChanged (e);
		}

		protected void RenderClock(Graphics g)
		{
			g.SmoothingMode = SmoothingMode.AntiAlias;
			g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
			StringFormat sf = new StringFormat();
			sf.LineAlignment = StringAlignment.Center;
			sf.Alignment = StringAlignment.Center;
			g.DrawString(time, base.Font, brush, this.ClientRectangle, sf);

			/*int curPos = 0;
			for (int c = 0; c < time.Length; c++)
			{
				MatrixLetter l = MatrixLetterFactory.Instance().GetLetter(time[c]);
				l.Render(g, pixBrush, curPos, pixSize);
				curPos += l.MaxX * pixSize;
			}*/
		}

		/// <summary>
		/// Renders the clock by converting the specified
		/// number of seconds to hrs:mins:secs format.
		/// </summary>
		/// <param name="tick"></param>
		public void SetClock(int tick)
		{
			int hours = Math.Abs(tick) / 3600;
			int temp = Math.Abs(tick) - (hours * 60 * 60);
			int mins = temp / 60;
			int secs = temp - (mins * 60);
			
			brush = Brushes.Black;
			time = "00:" + mins.ToString().PadLeft(2, '0') + ":" + secs.ToString().PadLeft(2, '0');
			Invalidate();
		}
	}
}
