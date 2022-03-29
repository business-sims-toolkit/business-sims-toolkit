using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace ResizingUi
{
	public class HatchFillImage
	{
		readonly HatchFillProperties hatchProperties;

		readonly Image hatchedImage;

		public HatchFillImage (HatchFillProperties properties)
		{
			hatchProperties = properties;

			hatchedImage = CreateHatchedImage(properties);
		}

		public void RenderToBounds (Graphics graphics, RectangleF bounds)
		{
			for (var y = (int) bounds.Top; y <= bounds.Bottom; y += hatchedImage.Height)
			{
				for (var x = (int) bounds.Left; x <= bounds.Right + 1; x += hatchedImage.Width)
				{
					graphics.DrawImageUnscaledAndClipped(hatchedImage, new Rectangle(x, y, 
						(int)Math.Min(hatchedImage.Width, (bounds.Right + 1) - x),
						(int)Math.Min(hatchedImage.Height, bounds.Bottom - y)));
				}
			}
		}

		public static Image CreateHatchedImage (HatchFillProperties properties, int scaleFactor = 3)
		{
			int hatchedImageWidth;
			int hatchedImageHeight;

			if (properties.Angle == 0 || Math.Abs(properties.Angle) == 90)
			{
				hatchedImageWidth = hatchedImageHeight =
					(int) Math.Round(properties.PatternWidth * scaleFactor, MidpointRounding.AwayFromZero);
			}
			else
			{
				hatchedImageWidth = (int)Math.Abs(Math.Round(properties.PatternWidth / Math.Cos(properties.AngleInRadians),
					MidpointRounding.AwayFromZero));

				hatchedImageHeight = (int)Math.Abs(Math.Round(properties.PatternWidth / Math.Sin(properties.AngleInRadians),
					                                              MidpointRounding.AwayFromZero));
			}

			var intermediateImageWidth = hatchedImageWidth * 16;
			var intermediateImageHeight = hatchedImageHeight * 16;

			using (var finalImage = new Bitmap(hatchedImageWidth, hatchedImageHeight))
			using (var intermediateImage = new Bitmap(intermediateImageWidth, intermediateImageHeight, PixelFormat.Format32bppArgb))
			using (var finalGraphics = Graphics.FromImage(finalImage))
			using (var intermediateGraphics = Graphics.FromImage(intermediateImage))
			using (var linePen = new Pen(properties.LineColour, properties.LineWidth))
			using (var altLinePen = new Pen(properties.AltLineColour, properties.AltLineWidth))
			{
				intermediateGraphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
				finalGraphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

				if (Math.Abs(properties.Angle) > 0)
				{
					intermediateGraphics.RotateTransform(properties.Angle);
					intermediateGraphics.TranslateTransform(-intermediateImageWidth / 2f,-intermediateImageHeight / 2f);
				}

				for (var y = properties.LineWidth / 2; y <= intermediateImageHeight; y += properties.PatternWidth / 2)
				{
					intermediateGraphics.DrawLine(linePen, 0, y, intermediateImageWidth, y);
					y += properties.PatternWidth / 2;
					intermediateGraphics.DrawLine(altLinePen, 0, y, intermediateImageWidth, y);
				}
				
				finalGraphics.DrawImageUnscaled(intermediateImage, new Rectangle(0, 0, hatchedImageWidth, hatchedImageHeight));
				
				return new Bitmap(finalImage);
			}


			
		}
	}
}
