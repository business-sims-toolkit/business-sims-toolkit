using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace LibCore
{
	public static class ImageUtils
	{
		public static Image ScaleImageToFitSize (Image sourceImage, Size size)
		{
			return ScaleImageToFitSize(sourceImage, size, Color.Transparent);
		}

		public static Image ScaleImageToFitSize (Image sourceImage, Size size, Color background)
		{
			Image destinationImage = new Bitmap (size.Width, size.Height);

			using (Graphics graphics = Graphics.FromImage(destinationImage))
			{
				if (background != Color.Transparent)
				{
					using (Brush brush = new SolidBrush (background))
					{
						graphics.FillRectangle(brush, 0, 0, size.Width, size.Height);
					}
				}

				double xScale = size.Width * 1.0 / sourceImage.Width;
				double yScale = size.Height * 1.0 / sourceImage.Height;
				double scale = Math.Min(xScale, yScale);

				Size drawnSize = new Size ((int) (sourceImage.Width * scale), (int) (sourceImage.Height * scale));

				Rectangle destinationRectangle = new Rectangle((destinationImage.Width - drawnSize.Width) / 2,
																(destinationImage.Height - drawnSize.Height) / 2,
																drawnSize.Width, drawnSize.Height);

				graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
				graphics.DrawImage(sourceImage, destinationRectangle);
			}

			return destinationImage;
		}

		public static Image ScaleImageToAspectRatio (Image source, double desiredAspect)
		{
			return ScaleImageToAspectRatio(source, desiredAspect, Color.Transparent);
		}

		public static Image ScaleImageToAspectRatio (Image source, double desiredAspect, Color background)
		{
			Image destinationImage;

			if ((source.Height * desiredAspect) > source.Width)
			{
				// Need to make the image wider.
				destinationImage = ScaleImageToFitSize(source, new Size ((int) (source.Height * desiredAspect), source.Height), background);
			}
			else if ((source.Height * desiredAspect) < source.Width)
			{
				// Need to make the image taller.
				destinationImage = ScaleImageToFitSize(source, new Size (source.Width, (int) (source.Width / desiredAspect)), background);
			}
			else
			{
				destinationImage = CloneImageOntoColour(source, background);
			}

			return destinationImage;
		}

		public static Image CloneImage (Image source)
		{
			Image destinationImage = new Bitmap (source.Width, source.Height);
			using (Graphics graphics = Graphics.FromImage(destinationImage))
			{
				graphics.DrawImage(source, 0, 0, source.Width, source.Height);
			}

			return destinationImage;
		}

		public static Image CloneImageOntoColour (Image source, Color background)
		{
			Bitmap copy = new Bitmap (source.Width, source.Height, PixelFormat.Format32bppArgb);

			using (Graphics graphics = Graphics.FromImage(copy))
			{
				graphics.CompositingMode = CompositingMode.SourceOver;

				using (Brush brush = new SolidBrush (background))
				{
					graphics.FillRectangle(brush, 0, 0, copy.Width, copy.Height);
				}

				graphics.DrawImage(source, new Rectangle (0, 0, copy.Width, copy.Height), new Rectangle (0, 0, source.Width, source.Height), GraphicsUnit.Pixel);
			}

			copy.SetResolution(96, 96);

			return copy;
		}

		public static Image LoadImage (string filename)
		{
			try
			{
				using (Image source = Image.FromFile(filename))
				{
					return CloneImage(source);
				}
			}
			catch
			{
				return null;
			}
		}
	}
}