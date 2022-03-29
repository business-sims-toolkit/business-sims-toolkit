using System.Drawing;

namespace LibCore
{
	public interface IGraphics
	{
		void DrawRectangle (Color colour, float thickness, RectangleF bounds);
		void FillRectangle (Color colour, RectangleF bounds);

		void DrawLine (Color colour, float thickness, float x0, float y0, float x1, float y1);

		void DrawString (string text, Font font, Color colour, RectangleF bounds, StringFormat format);
		SizeF MeasureString (string text, Font font);

		void DrawImage (Image image, RectangleF bounds);
	}
}