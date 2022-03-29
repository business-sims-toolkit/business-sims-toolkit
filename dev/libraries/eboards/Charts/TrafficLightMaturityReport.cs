using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;

using CommonGUI;

using LibCore;
using ReportBuilder;

namespace Charts
{
	public class TrafficLightMaturityReport : FlickerFreePanel
	{
		Image backImage;
		List<string> keyLabels;
		Dictionary<string, Image> keyLabelToIcon;
		ITrafficLightRateable [] maturityScores;

		Font font;

		bool scale;
		bool DrawDebugBounds = false;

		public TrafficLightMaturityReport (string imageName, List<string> keyLabels, Dictionary<string, Image> keyLabelToIcon, ITrafficLightRateable [] maturityScores, bool scale)
		{
			backImage = Repository.TheInstance.GetImage(imageName);
			this.keyLabels = keyLabels;
			this.keyLabelToIcon = keyLabelToIcon;
			this.maturityScores = maturityScores;
			this.scale = scale;

			font = ConstantSizeFont.NewFont(CoreUtils.SkinningDefs.TheInstance.GetData("fontname"), 10);
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing)
			{
				font.Dispose();
			}

			base.Dispose(disposing);
		}

		Color BlendColour (Color a, Color b, double t)
		{
			return Color.FromArgb((byte) ((a.A * (1 - t)) + (b.A * t)),
								  (byte) ((a.R * (1 - t)) + (b.R * t)),
								  (byte) ((a.G * (1 - t)) + (b.G * t)),
								  (byte) ((a.B * (1 - t)) + (b.B * t)));
		}

		int MapRange (int x, int x0, int x1, int y0, int y1)
		{
			return (y0 + ((x - x0) * (y1 - y0) / (x1 - x0)));
		}

		Point MapPointBetweenRectangles (Point mapPoint, Rectangle sourceBounds, Rectangle destinationBounds)
		{
			return new Point (MapRange(mapPoint.X, sourceBounds.Left, sourceBounds.Right, destinationBounds.Left, destinationBounds.Right),
							  MapRange(mapPoint.Y, sourceBounds.Top, sourceBounds.Bottom, destinationBounds.Top, destinationBounds.Bottom));
		}

		Rectangle MapRectangleBetweenRectangles (Rectangle mapRectangle, Rectangle sourceBounds, Rectangle destinationBounds)
		{
			Point mappedTopLeft = MapPointBetweenRectangles(new Point (mapRectangle.Left, mapRectangle.Top),
															sourceBounds, destinationBounds);
			Point mappedBottomRight = MapPointBetweenRectangles(new Point (mapRectangle.Right, mapRectangle.Bottom),
																sourceBounds, destinationBounds);

			return new Rectangle (mappedTopLeft, new Size (mappedBottomRight.X - mappedTopLeft.X, mappedBottomRight.Y - mappedTopLeft.Y));
		}

		protected override void OnPaint (PaintEventArgs e)
		{
			base.OnPaint(e);

			e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
			e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

			Rectangle distortedBounds = new Rectangle (0, 0, ClientSize.Width, ClientSize.Height);
			double xFactor = ((double) distortedBounds.Width) / backImage.Width;
			double yFactor = ((double) distortedBounds.Height) / backImage.Height;
			double factor = Math.Min(xFactor, yFactor);

			if (! scale)
			{
				factor = 1;
			}

			Size undistortedSize = new Size ((int) (backImage.Width * factor), (int) (backImage.Height * factor));
			Rectangle bounds = new Rectangle (new Point ((distortedBounds.Width - undistortedSize.Width) / 2,
														 (distortedBounds.Height - undistortedSize.Height) / 2),
											  undistortedSize);

			// Draw background.
			e.Graphics.DrawImage(backImage, bounds);

			// Draw scores.
            if (maturityScores != null)
            {
                foreach (ITrafficLightRateable maturityScore in maturityScores)
                {
					Size size = new Size ((int) (factor * maturityScore.Size.Width),
						                  (int) (factor * maturityScore.Size.Height));
                    DrawIcon(e.Graphics, maturityScore.Location, size, maturityScore.Icon,
                             new Rectangle (0, 0, backImage.Width, backImage.Height),
                             bounds);
                }
            }

			// Draw key.
			if (keyLabels != null)
			{
				int keyWidth = 130;
				int keyOffset = 50;
				int lineGap = 10;

				for (int i = 0; i < keyLabels.Count; i++)
				{
					string label = keyLabels[i];
					Image icon = keyLabelToIcon[label];

					Size size = new Size ((int) (factor * icon.Width),
										  (int) (factor * icon.Height));

					DrawKeyLine(e.Graphics, icon, size, label, bounds.Right - keyWidth, bounds.Top + keyOffset + (i * (size.Height + lineGap)));
				}
			}
		}

		void DrawKeyLine (Graphics graphics, Image icon, Size size, string legend, int x, int y)
		{
			DrawIcon(graphics, new Point (x - (size.Width / 4), y - (size.Height / 2)), icon, size);
			graphics.DrawString(legend, font, Brushes.Black, x + (size.Width / 4), y - (size.Height / 2) - (font.Height / 2));
		}

		void DrawIcon(Graphics graphics, Point location, Size iconScreenSize, Image icon, Rectangle imageBounds, Rectangle screenBounds)
		{
			DrawIcon(graphics, MapPointBetweenRectangles(location, imageBounds, screenBounds),
							 icon, iconScreenSize);

			if (DrawDebugBounds)
			{
				graphics.DrawRectangle(Pens.Red, location.X - (iconScreenSize.Width / 2), location.Y - (iconScreenSize.Height / 2), iconScreenSize.Width, iconScreenSize.Height);
			}
		}

		void DrawIcon (Graphics graphics, Point screenLocation, Image icon)
		{
			graphics.DrawImage(icon, screenLocation);
		}

		void DrawIcon (Graphics graphics, Point screenLocation, Image icon, Size iconScreenSize)
		{
			graphics.DrawImage(icon,
				screenLocation.X - (iconScreenSize.Width / 2), screenLocation.Y - (iconScreenSize.Height / 2),
				iconScreenSize.Width, iconScreenSize.Height);
		}
	}
}