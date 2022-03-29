using System.Drawing;

namespace BaseUtils
{
	/// <summary>
	/// Adapts the abstract ChartContainer implementation to 
	/// draw using GDI functions.
	/// </summary>
	public class WinChartAdapter : ChartContainer
	{
		Graphics target;

		public WinChartAdapter()
		{
		}

		public void Draw(Graphics g)
		{
			target = g;
			base.Draw();
		}

		protected override void DrawAxes()
		{
			CurrentPen = new Pen(Color.FromArgb(24, 54, 126));
			CurrentFont = CoreUtils.SkinningDefs.TheInstance.GetFont(8f, FontStyle.Bold);
			base.DrawAxes ();
		}

		protected override void DrawTitle()
		{
			CurrentPen = new Pen(Color.Black);
			CurrentFont = CoreUtils.SkinningDefs.TheInstance.GetFont(8f, FontStyle.Bold);
			base.DrawTitle ();
		}

		protected override void DrawCharts()
		{
			target.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
			CurrentFont = CoreUtils.SkinningDefs.TheInstance.GetFont(8f, FontStyle.Regular);
			base.DrawCharts ();
		}

		public override void DrawLine(float x1, float y1, float x2, float y2)
		{
			target.DrawLine(new Pen(base.CurrentPen.Brush, base.CurrentPen.Width), x1, y1, x2, y2);
		}

		public override void DrawRectangle(float x, float y, float width, float height)
		{
			target.DrawRectangle(new Pen(base.CurrentPen.Brush, base.CurrentPen.Width), x, y, width, height);
		}

		public override void FillCircle(float cx, float cy, float radius)
		{
			target.FillEllipse(CurrentBrush, cx - radius, cy - radius, radius*2, radius*2);
		}

		public override void FillRectangle(float x, float y, float width, float height)
		{
			target.FillRectangle(CurrentBrush, x, y, width, height);
		}

		public override void DrawText(float x, float y, string text, float maxWidth, TextAlignment alignment)
		{
			StringFormat sf = new StringFormat();
			sf.Alignment = (StringAlignment)alignment;
			target.DrawString(text, CurrentFont, CurrentBrush, new RectangleF(x, y, maxWidth, 30), sf);
		}

		public override void DrawVerticalText(float x, float y, string text)
		{
			StringFormat sf = new StringFormat(StringFormatFlags.DirectionVertical);
			target.DrawString(text, CurrentFont, CurrentBrush, x, y, sf);
		}

		public override void DrawImage(Image img, float x, float y, float width, float height)
		{
			target.DrawImage(img, x, y, width, height);
		}

		public override SizeF MeasureString(string text, Font font)
		{
			return target.MeasureString(text, font);
		}
	}
}
