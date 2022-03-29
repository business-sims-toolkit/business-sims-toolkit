using System.Drawing;

namespace LibCore
{
	public class GdiGraphicsWrapper : IGraphics
	{
		readonly Graphics graphics;

		public GdiGraphicsWrapper (Graphics graphics)
		{
			this.graphics = graphics;
		}

		public void DrawRectangle (Color colour, float thickness, RectangleF bounds)
		{
			using (var pen = new Pen (colour, thickness))
			{
				graphics.DrawRectangle(pen, bounds.Left, bounds.Top, bounds.Width, bounds.Height);
			}
		}

		public void FillRectangle (Color colour, RectangleF bounds)
		{
			using (var brush = new SolidBrush (colour))
			{
				graphics.FillRectangle(brush, bounds);
			}
		}

		public void DrawLine (Color colour, float thickness, float x0, float y0, float x1, float y1)
		{
			using (var pen = new Pen(colour, thickness))
			{
				graphics.DrawLine(pen, x0, y0, x1, y1);
			}
		}

		public void DrawString (string text, Font font, Color colour, RectangleF bounds, StringFormat format)
		{
			using (var brush = new SolidBrush (colour))
			{
				graphics.DrawString(text, font, brush, bounds, format);
			}
		}

		public SizeF MeasureString (string text, Font font)
		{
			return graphics.MeasureString(text, font);
		}

		public void DrawImage (Image image, RectangleF bounds)
		{
			graphics.DrawImage(image, bounds);
		}
	}
}