using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using Algorithms;
using ResizingUi.Enums;

namespace ResizingUi.Extensions
{
	public static class GraphicsExtensions
	{
		public static void DrawRectangleReticule(this Graphics graphics, Rectangle bounds, Pen pen, int tickLength)
		{
			DrawRectangleFReticule(graphics, new RectangleF(bounds.Left, bounds.Top, bounds.Width, bounds.Height), pen, tickLength);
		}

		public static void DrawRectangleFReticule(this Graphics graphics, RectangleF bounds, Pen pen, float tickLength, params RectangleCorners[] cornersAsArray) 
		{
			var corners = new List<RectangleCorners>(cornersAsArray);

			// TODO offset by half the pen width to ensure the lines are within the bounds??
			
			var drawAllCorners = corners.Count == 0 || corners.Contains(RectangleCorners.All);

			// Top left
			if (drawAllCorners || corners.Contains(RectangleCorners.TopLeft))
			{
				DrawCorner(graphics, new PointF(bounds.Left, bounds.Top), pen, tickLength, tickLength);
			}

			// Top right
			if (drawAllCorners || corners.Contains(RectangleCorners.TopRight))
			{
				DrawCorner(graphics, new PointF(bounds.Right, bounds.Top), pen, -tickLength, tickLength);
			}

			// Bottom left
			if (drawAllCorners || corners.Contains(RectangleCorners.BottomLeft))
			{
				DrawCorner(graphics, new PointF(bounds.Left, bounds.Bottom), pen, tickLength, -tickLength);
			}

			// Bottom right
			if (drawAllCorners || corners.Contains(RectangleCorners.BottomRight))
			{
				DrawCorner(graphics, new PointF(bounds.Right, bounds.Bottom), pen, -tickLength, -tickLength);
			}

		}

		static void DrawCorner (Graphics graphics, PointF origin, Pen pen, float xOffset, float yOffset)
		{
			graphics.DrawLine(pen, origin, new PointF(origin.X + xOffset, origin.Y));
			graphics.DrawLine(pen, origin, new PointF(origin.X, origin.Y + yOffset));
		}

		public static void DrawHatchedArea (this Graphics graphics, RectangleF bounds, HatchFillProperties hatchFillProperties, GraphicsPath regionPath = null, int scaleFactor = 3)
		{
			// TODO potentially move the drawing 'logic' from CreateHatchedImage and just do it here
			// instead of creating an image. It's probably 6 and half a dozen either way
			using (var hatchedImage = HatchFillImage.CreateHatchedImage(hatchFillProperties))
			{
				Region region = null;

				var previousRegion = graphics.Clip;
				if (regionPath != null)
				{
					graphics.Clip = region = new Region(regionPath);
				}

				for (var y = bounds.Top; y < bounds.Bottom; y += hatchedImage.Height / scaleFactor)
				{
					for (var x = bounds.Left; x < bounds.Right; x += hatchedImage.Width / scaleFactor)
					{
						var renderBounds = new Rectangle ((int) x, (int) y, hatchedImage.Width / scaleFactor, hatchedImage.Height / scaleFactor);
						graphics.DrawImage(hatchedImage, renderBounds);

					}
				}

				graphics.Clip = previousRegion;

				region?.Dispose();
			}
		}

		public static void DrawOutlinedString(this Graphics graphics, string text, FontFamily fontFamily, FontStyle fontStyle, float fontInPixelSize, PointF position, 
		                                     Color outlineColour, Color fillColour, StringFormat stringFormat, float outlineWidth = 4)
		{
			var graphicsPath = new GraphicsPath();

			graphicsPath.AddString(text, fontFamily, (int)fontStyle, fontInPixelSize, position, stringFormat);

			using (var outlinePen = new Pen(outlineColour, outlineWidth))
			using (var fillBrush = new SolidBrush(fillColour))
			{
				graphics.DrawPath(outlinePen, graphicsPath);
				graphics.FillPath(fillBrush, graphicsPath);
			}
		}

		public static void DrawOutlinedString(this Graphics graphics, string text, FontFamily fontFamily, FontStyle fontStyle, float fontInPixelSize, RectangleF layoutBounds,
		                                     Color outlineColour, Color fillColour, StringFormat stringFormat, float outlineWidth = 4)
		{
			graphics.SmoothingMode = SmoothingMode.AntiAlias;
			graphics.CompositingQuality = CompositingQuality.HighQuality;
			graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
			graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
			
			var graphicsPath = new GraphicsPath();

			graphicsPath.AddString(text, fontFamily, (int)fontStyle, fontInPixelSize, layoutBounds, stringFormat);

			using (var outlinePen = new Pen(outlineColour, outlineWidth))
			using (var fillBrush = new SolidBrush(fillColour))
			{
				graphics.DrawPath(outlinePen, graphicsPath);
				graphics.FillPath(fillBrush, graphicsPath);
			}
		}


		// **** Rounded Rectangle


		public static void FillRoundedRectangle (this Graphics graphics, Brush brush, RectangleF bounds, float radius, params RectangleCorners [] cornersAsArray)
		{
			var corners = new List<RectangleCorners>(cornersAsArray);

			using (var pen = new Pen(brush, 1))
			{
				
				graphics.SmoothingMode = SmoothingMode.None;

				var widthMinusRadius = bounds.Width - 2 * radius;
				var heightMinusRadius = bounds.Height - 2 * radius;

				// Fill in the background
				{
					// Centre
					graphics.FillRectangle(brush, bounds.AlignRectangle(widthMinusRadius, heightMinusRadius));
					// Top
					graphics.FillRectangle(brush, bounds.AlignRectangle(widthMinusRadius, radius, StringAlignment.Center, StringAlignment.Near));
					// Bottom
					graphics.FillRectangle(brush, bounds.AlignRectangle(widthMinusRadius, radius, StringAlignment.Center, StringAlignment.Far));
					// Left
					graphics.FillRectangle(brush, bounds.AlignRectangle(radius, heightMinusRadius, StringAlignment.Near));
					// Right
					graphics.FillRectangle(brush, bounds.AlignRectangle(radius, heightMinusRadius, StringAlignment.Far));
				}

				var topLeftBounds = bounds.AlignRectangle(2 * radius + 1, 2 * radius + 1, StringAlignment.Near, StringAlignment.Near);

				if (corners.Contains(RectangleCorners.TopLeft) || corners.Contains(RectangleCorners.All))
				{
					FillRoundedCorner(graphics, radius, topLeftBounds, brush, pen, 180);
				}
				else
				{
					graphics.FillRectangle(brush, topLeftBounds);
				}

				var topRightBounds = bounds.AlignRectangle(2 * radius, 2 * radius + 1, StringAlignment.Far, StringAlignment.Near);

				if (corners.Contains(RectangleCorners.TopRight) || corners.Contains(RectangleCorners.All))
				{
					FillRoundedCorner(graphics, radius, topRightBounds, brush, pen, 270);
				}
				else
				{
					graphics.FillRectangle(brush, topRightBounds);
				}


				var bottomLeftBounds = bounds.AlignRectangle(2 * radius + 1, 2 * radius, StringAlignment.Near, StringAlignment.Far);

				if (corners.Contains(RectangleCorners.BottomLeft) || corners.Contains(RectangleCorners.All))
				{
					FillRoundedCorner(graphics, radius, bottomLeftBounds, brush, pen, 90);
				}
				else
				{
					graphics.FillRectangle(brush, bottomLeftBounds);
				}

				var bottomRightBounds = bounds.AlignRectangle(2 * radius, 2 * radius, StringAlignment.Far, StringAlignment.Far);

				if (corners.Contains(RectangleCorners.BottomRight) || corners.Contains(RectangleCorners.All))
				{
					FillRoundedCorner(graphics, radius, bottomRightBounds, brush, pen, 0);
				}
				else
				{
					graphics.FillRectangle(brush, bottomRightBounds);
				}

			}
		}

		static void FillRoundedCorner (Graphics graphics, float radius, RectangleF bounds, Brush brush, Pen pen, float startAngle)
		{
			var smoothingMode = graphics.SmoothingMode;
			var interpolationMode = graphics.InterpolationMode;
			var pixelOffsetMode = graphics.PixelOffsetMode;
			var compositingQuality = graphics.CompositingQuality;

			graphics.SmoothingMode = SmoothingMode.AntiAlias;
			graphics.CompositingQuality = CompositingQuality.HighQuality;
			graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
			graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
			
			graphics.FillPie(brush, bounds.X, bounds.Y, bounds.Width, bounds.Height, startAngle, 90);

			graphics.SmoothingMode = smoothingMode;
			graphics.CompositingQuality = compositingQuality;
			graphics.PixelOffsetMode = pixelOffsetMode;
			graphics.InterpolationMode = interpolationMode;
		}

		public static void DrawRoundedRectangle (this Graphics graphics, Pen pen, Rectangle bounds, float radius,
		                                         params RectangleCorners [] cornersAsArray)
		{
			DrawRoundedRectangle(graphics, pen, new RectangleF(bounds.Location, bounds.Size), radius, cornersAsArray);
		}

		public static void DrawRoundedRectangle (this Graphics graphics, Pen pen, RectangleF bounds, float radius,
		                                         params RectangleCorners [] cornersAsArray)
		{
			var corners = new List<RectangleCorners>(cornersAsArray);

			graphics.SmoothingMode = SmoothingMode.AntiAlias;

			var allCorners = new []
			{
				RectangleCorners.TopLeft,
				RectangleCorners.TopRight,
				RectangleCorners.BottomLeft,
				RectangleCorners.BottomRight
			};

			var allCornersRounded =  corners.Contains(RectangleCorners.All) || allCorners.Any(c => !corners.Contains(c));

			var missingCorners = allCorners.Where(c => ! corners.Contains(c) && !allCornersRounded).ToList();
			
			if (missingCorners.Any())
			{
				DrawRectangleFReticule(graphics, bounds, pen, radius, missingCorners.ToArray());
			}

			// Top line
			graphics.DrawLine(pen, bounds.Left + radius -1, bounds.Top, bounds.Right - radius, bounds.Top);
			// Left line
			graphics.DrawLine(pen, bounds.Left, bounds.Top + radius - 1, bounds.Left, bounds.Bottom - radius);
			// Bottom line
			graphics.DrawLine(pen, bounds.Left + radius -1, bounds.Bottom - 1, bounds.Right - radius, bounds.Bottom - 1);
			// Right line
			graphics.DrawLine(pen, bounds.Right - 1, bounds.Top + radius -1, bounds.Right - 1, bounds.Bottom - radius);


			if (corners.Contains(RectangleCorners.TopLeft) || allCornersRounded)
			{
				var topLeftBounds = bounds.AlignRectangle(2 * radius, 2 * radius, StringAlignment.Near, StringAlignment.Near);

				DrawRoundedCorner(graphics, pen, topLeftBounds, 180);
			}

			if (corners.Contains(RectangleCorners.TopRight) || allCornersRounded)
			{
				var topRightBounds = bounds.AlignRectangle(2 * radius, 2 * radius, StringAlignment.Far, StringAlignment.Near, -1);

				DrawRoundedCorner(graphics, pen, topRightBounds, 270);
			}

			if (corners.Contains(RectangleCorners.BottomLeft) || allCornersRounded)
			{
				var bottomLeftBounds = bounds.AlignRectangle(2 * radius, 2 * radius, StringAlignment.Near, StringAlignment.Far, 0, -1);

				DrawRoundedCorner(graphics, pen, bottomLeftBounds, 90);
			}

			if (corners.Contains(RectangleCorners.BottomRight) || allCornersRounded)
			{
				var bottomRightBounds = bounds.AlignRectangle(2 * radius, 2 * radius, StringAlignment.Far, StringAlignment.Far, -1, -1);

				DrawRoundedCorner(graphics, pen, bottomRightBounds, 0);
			}
		}

		static void DrawRoundedCorner (Graphics graphics, Pen pen, RectangleF bounds, float startAngle)
		{
			graphics.DrawArc(pen, bounds.X, bounds.Y, bounds.Width, bounds.Height, startAngle, 90);
		}

		public static void DrawAndFillRoundedRectangle (this Graphics graphics, Brush fillBrush, Pen outlinePen, RectangleF bounds, float radius, params RectangleCorners [] cornersAsArray)
		{
			
			FillRoundedRectangle(graphics, fillBrush, bounds, radius, cornersAsArray);
			DrawRoundedRectangle(graphics, outlinePen, bounds, radius, cornersAsArray);
		}

		public static void DrawOutlinedLines (this Graphics graphics, PointF [] points, Color fillColour,
		                                     Color outlineColour, float pathThickness, float outlineThickness)
		{
			var graphicsPath = new GraphicsPath();

			graphicsPath.AddLines(points);

			using (var fillBrush = new SolidBrush(fillColour))
			using (var outlinePen = new Pen(outlineColour, outlineThickness))
			{
				graphics.FillPath(fillBrush, graphicsPath);
				graphics.DrawPath(outlinePen, graphicsPath);
			}
		}
	}
}
