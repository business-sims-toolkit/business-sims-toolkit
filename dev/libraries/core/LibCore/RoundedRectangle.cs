using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace LibCore
{
	public static class RoundedRectangle
	{
		public enum Corner
		{
			TopLeft,
			TopRight,
			BottomLeft,
			BottomRight
		}

		public static void DrawRoundedRectangle (Graphics graphics, Pen pen, Rectangle rectangle, int radius)
		{
			DrawRoundedRectangle(graphics, pen, rectangle, radius,
			                     Corner.TopLeft, Corner.TopRight, Corner.BottomLeft, Corner.BottomRight);
		}

		public static void DrawRoundedRectangle (Graphics graphics, Pen pen, Rectangle rectangle, int radius, params Corner [] corners)
		{
			DrawRoundedRectangle(graphics, pen, new RectangleF (rectangle.Left, rectangle.Top, rectangle.Width, rectangle.Height), radius, corners);
		}

		public static void FillRoundedRectangle (Graphics graphics, Brush brush, Rectangle rectangle, int radius)
		{
			FillRoundedRectangle(graphics, brush, rectangle, radius,
								 Corner.TopLeft, Corner.TopRight, Corner.BottomLeft, Corner.BottomRight);
		}

		public static void FillRoundedRectangle (Graphics graphics, Brush brush, Rectangle rectangle, int radius, params Corner [] corners)
		{
			FillRoundedRectangle(graphics, brush, new RectangleF (rectangle.Left, rectangle.Top, rectangle.Width, rectangle.Height), radius, corners);
		}

		public static void DrawRoundedRectangle (Graphics graphics, Pen pen, RectangleF rectangle, float radius)
		{
			DrawRoundedRectangle(graphics, pen, rectangle, radius,
								 Corner.TopLeft, Corner.TopRight, Corner.BottomLeft, Corner.BottomRight);
		}

		public static void DrawRoundedRectangle (Graphics graphics, Pen pen, RectangleF rectangle, float radius, params Corner [] cornersAsArray)
		{
			List<Corner> corners = new List<Corner> (cornersAsArray);

			graphics.SmoothingMode = SmoothingMode.AntiAlias;

			graphics.DrawLine(pen, rectangle.Left + radius, rectangle.Top, rectangle.Right - radius - 1, rectangle.Top);

			if (corners.Contains(Corner.TopLeft))
			{
				graphics.DrawArc(pen, rectangle.Left, rectangle.Top, 2 * radius, 2 * radius, 180, 90);
			}
			else
			{
				graphics.DrawLine(pen, rectangle.Left, rectangle.Top + radius, rectangle.Left, rectangle.Top);
				graphics.DrawLine(pen, rectangle.Left, rectangle.Top, rectangle.Left + radius, rectangle.Top);
			}

			graphics.DrawLine(pen, rectangle.Right - 1, rectangle.Top + radius, rectangle.Right - 1, rectangle.Bottom - radius - 1);

			if (corners.Contains(Corner.TopRight))
			{
				graphics.DrawArc(pen, rectangle.Right - (2 * radius) - 1, rectangle.Top, 2 * radius, 2 * radius, 270, 90);
			}
			else
			{
				graphics.DrawLine(pen, rectangle.Right - radius - 1, rectangle.Top, rectangle.Right - 1, rectangle.Top);
				graphics.DrawLine(pen, rectangle.Right - 1, rectangle.Top, rectangle.Right - 1, rectangle.Top + radius);
			}

			graphics.DrawLine(pen, rectangle.Right - radius - 1, rectangle.Bottom - 1, rectangle.Left + radius, rectangle.Bottom - 1);

			if (corners.Contains(Corner.BottomRight))
			{
				graphics.DrawArc(pen, rectangle.Right - (2 * radius) - 1, rectangle.Bottom - (2 * radius) - 1, 2 * radius, 2 * radius, 0, 90);
			}
			else
			{
				graphics.DrawLine(pen, rectangle.Right - 1, rectangle.Bottom - radius - 1, rectangle.Right - 1, rectangle.Bottom - 1);
				graphics.DrawLine(pen, rectangle.Right - 1, rectangle.Bottom - 1, rectangle.Right - radius - 1, rectangle.Bottom - 1);
			}

			graphics.DrawLine(pen, rectangle.Left, rectangle.Bottom - radius - 1, rectangle.Left, rectangle.Top + radius);

			if (corners.Contains(Corner.BottomLeft))
			{
				graphics.DrawArc(pen, rectangle.Left, rectangle.Bottom - (2 * radius) - 1, 2 * radius, 2 * radius, 90, 90);
			}
			else
			{
				graphics.DrawLine(pen, rectangle.Left + radius, rectangle.Bottom - 1, rectangle.Left, rectangle.Bottom - 1);
				graphics.DrawLine(pen, rectangle.Left, rectangle.Bottom - 1, rectangle.Left, rectangle.Bottom - radius - 1);
			}
		}

		public static void FillRoundedRectangle (Graphics graphics, Brush brush, RectangleF rectangle, float radius)
		{
			FillRoundedRectangle(graphics, brush, rectangle, radius,
								 Corner.TopLeft, Corner.TopRight, Corner.BottomLeft, Corner.BottomRight);
		}

		public static void FillRoundedRectangle (Graphics graphics, Brush brush, RectangleF rectangle, float radius, params Corner [] cornersAsArray)
		{
			List<Corner> corners = new List<Corner> (cornersAsArray);

			using (var pen = new Pen (brush, 1))
			{
				int overlap = 0;
				if (radius >= 1)
				{
					overlap = 0;
				}

				graphics.SmoothingMode = SmoothingMode.None;

				graphics.FillRectangle(brush, rectangle.Left + radius - overlap, rectangle.Top + radius - overlap,
					rectangle.Width - (2 * radius) + (2 * overlap), rectangle.Height - (2 * radius) + (2 * overlap));

				graphics.FillRectangle(brush, rectangle.Left + radius - overlap, rectangle.Top,
					rectangle.Width - (2 * radius) + (2 * overlap), radius);

				if (corners.Contains(Corner.TopLeft))
				{
					graphics.SmoothingMode = SmoothingMode.AntiAlias;
					graphics.FillPie(brush, rectangle.Left, rectangle.Top, 2 * radius, 2 * radius, 180, 90);
					graphics.SmoothingMode = SmoothingMode.None;
					graphics.DrawLine(pen, rectangle.Left, rectangle.Top + radius, rectangle.Left + radius, rectangle.Top + radius);
					graphics.DrawLine(pen, rectangle.Left + radius, rectangle.Top, rectangle.Left + radius, rectangle.Top + radius);
				}
				else
				{
					graphics.FillRectangle(brush, rectangle.Left, rectangle.Top, 2 * radius, 2 * radius);
				}

				graphics.FillRectangle(brush, rectangle.Right - radius, rectangle.Top + radius - overlap, radius,
					rectangle.Height - (2 * radius) + (2 * overlap));

				if (corners.Contains(Corner.TopRight))
				{
					graphics.SmoothingMode = SmoothingMode.AntiAlias;
					graphics.FillPie(brush, rectangle.Right - (2 * radius), rectangle.Top, 2 * radius, 2 * radius, 270, 90);
					graphics.SmoothingMode = SmoothingMode.None;
					graphics.DrawLine(pen, rectangle.Right, rectangle.Top + radius, rectangle.Right - radius, rectangle.Top + radius);
					graphics.DrawLine(pen, rectangle.Right - radius, rectangle.Top, rectangle.Right - radius, rectangle.Top + radius);
				}
				else
				{
					graphics.FillRectangle(brush, rectangle.Right - (2 * radius), rectangle.Top, 2 * radius, 2 * radius);
				}

				graphics.FillRectangle(brush, rectangle.Left + radius - overlap, rectangle.Bottom - radius,
					rectangle.Width - (2 * radius) + (2 * overlap), radius);

				if (corners.Contains(Corner.BottomRight))
				{
					graphics.SmoothingMode = SmoothingMode.AntiAlias;
					graphics.FillPie(brush, rectangle.Right - (2 * radius), rectangle.Bottom - (2 * radius), 2 * radius, 2 * radius, 0, 90);
					graphics.SmoothingMode = SmoothingMode.None;
					graphics.DrawLine(pen, rectangle.Right, rectangle.Bottom - radius, rectangle.Right - radius, rectangle.Bottom - radius);
					graphics.DrawLine(pen, rectangle.Right - radius, rectangle.Bottom, rectangle.Right - radius, rectangle.Bottom - radius);
				}
				else
				{
					graphics.FillRectangle(brush, rectangle.Right - (2 * radius), rectangle.Bottom - (2 * radius), 2 * radius, 2 * radius);
				}

				graphics.FillRectangle(brush, rectangle.Left, rectangle.Top + radius - overlap, radius,
					rectangle.Height - (2 * radius) + (2 * overlap));

				if (corners.Contains(Corner.BottomLeft))
				{
					graphics.SmoothingMode = SmoothingMode.AntiAlias;
					graphics.FillPie(brush, rectangle.Left, rectangle.Bottom - (2 * radius), 2 * radius, 2 * radius, 90, 90);
					graphics.SmoothingMode = SmoothingMode.None;
					graphics.DrawLine(pen, rectangle.Left, rectangle.Bottom - radius, rectangle.Left + radius, rectangle.Bottom - radius);
					graphics.DrawLine(pen, rectangle.Left + radius, rectangle.Bottom, rectangle.Left + radius, rectangle.Bottom - radius);
				}
				else
				{
					graphics.FillRectangle(brush, rectangle.Left, rectangle.Bottom - (2 * radius), 2 * radius, 2 * radius);
				}
			}
		}

		public static GraphicsPath GetRoundedRectanglePath (Rectangle rectangle, int cornerRounding)
		{
			GraphicsPath path = new GraphicsPath ();

			path.AddLine(rectangle.Left + cornerRounding, rectangle.Top, rectangle.Right - 1 - cornerRounding, rectangle.Top);

			if (cornerRounding > 0)
			{
				path.AddArc(rectangle.Right - 1 - (2 * cornerRounding), rectangle.Top, (2 * cornerRounding) - 1, (2 * cornerRounding) - 1, 270, 90);
			}

			path.AddLine(rectangle.Right - 1, rectangle.Top + cornerRounding, rectangle.Right - 1, rectangle.Bottom - 1 - cornerRounding);

			if (cornerRounding > 0)
			{
				path.AddArc(rectangle.Right - 1 - (2 * cornerRounding), rectangle.Bottom - 1 - (2 * cornerRounding), (2 * cornerRounding) - 1, (2 * cornerRounding) - 1, 0, 90);
			}

			path.AddLine(rectangle.Right - 1 - cornerRounding, rectangle.Bottom - 1, rectangle.Left + cornerRounding, rectangle.Bottom - 1);

			if (cornerRounding > 0)
			{
				path.AddArc(rectangle.Left, rectangle.Bottom - 1 - (2 * cornerRounding), (2 * cornerRounding) - 1, (2 * cornerRounding) - 1, 90, 90);
			}

			path.AddLine(rectangle.Left, rectangle.Bottom - 1 - cornerRounding, rectangle.Left, rectangle.Top + cornerRounding);

			if (cornerRounding > 0)
			{
				path.AddArc(rectangle.Left, rectangle.Top, (2 * cornerRounding) - 1, (2 * cornerRounding) - 1, 180, 90);
			}

			return path;
		}
	}
}