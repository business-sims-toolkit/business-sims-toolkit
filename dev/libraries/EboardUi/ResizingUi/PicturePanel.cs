using System;
using System.Drawing;
using System.Windows.Forms;
using CommonGUI;

namespace ResizingUi
{
	public class PicturePanel : Panel
	{
		ZoomMode zoomMode;
		protected Image image;

		IWatermarker watermarker;
		bool watermarkOnTop;

		PointF imageReferencePoint;
		PointF windowReferencePoint;

		int instep;

		public IWatermarker Watermarker
		{
			get => watermarker;

			set
			{
				watermarker = value;
				Invalidate();
			}
		}

		public bool WatermarkOnTop
		{
			get => watermarkOnTop;

			set
			{
				watermarkOnTop = value;
				Invalidate();
			}
		}

		public PicturePanel ()
		{
			DoubleBuffered = true;
		}

		public ZoomMode ZoomMode => zoomMode;

		public PointF WindowReferencePoint => windowReferencePoint;

		public PointF ImageReferencePoint => imageReferencePoint;

		public void RemoveImage ()
		{
			image = null;
			Invalidate();
		}

		public void ZoomWithLetterboxing (Image image)
		{
			ZoomWithLetterboxing(image, new PointF (0.5f, 0.5f), new PointF (0.5f, 0.5f));
		}

		public void ZoomWithLetterboxing (Image image, PointF windowReferencePoint, PointF imageReferencePoint)
		{
			this.image = image;
			zoomMode = ZoomMode.PreserveAspectRatioWithLetterboxing;
			this.windowReferencePoint = windowReferencePoint;
			this.imageReferencePoint = imageReferencePoint;
			Invalidate();
		}

		public void ZoomWithCropping (Image image)
		{
			ZoomWithCropping(image, new PointF (0.5f, 0.5f), new PointF (0.5f, 0.5f));
		}

		public void ZoomWithCropping (Image image, PointF windowReferencePoint, PointF imageReferencePoint)
		{
			this.image = image;
			zoomMode = ZoomMode.PreserveAspectRatioWithCropping;
			this.windowReferencePoint = windowReferencePoint;
			this.imageReferencePoint = imageReferencePoint;
			Invalidate();
		}

		public Image Image => image;

		public int Instep
		{
			get => instep;

			set
			{
				instep = value;
				Invalidate();
			}
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);
			Invalidate();
		}

		protected override void OnPaint (PaintEventArgs e)
		{
			base.OnPaint(e);

			if (! watermarkOnTop)
			{
				watermarker?.Draw(this, e.Graphics);
			}

			if (image != null)
			{
				if (ZoomMode == ZoomMode.Tile)
				{
					for (var x = instep; (x + image.Width) < (Width - instep); x += image.Width)
					{
						for (var y = instep; (y + image.Height) < (Height - instep); x += image.Height)
						{
							e.Graphics.DrawImage(image, x, y, image.Width, image.Height);
						}
					}
				}
				else
				{
					float scale;

					var xScale = (Width - (2 * instep)) / (float) image.Width;
					var yScale = (Height - (2 * instep)) / (float) image.Height;

					switch (ZoomMode)
					{
						case ZoomMode.PreserveAspectRatioWithLetterboxing:
							scale = Math.Min(xScale, yScale);
							xScale = scale;
							yScale = scale;
							break;

						case ZoomMode.PreserveAspectRatioWithCropping:
							scale = Math.Max(xScale, yScale);
							xScale = scale;
							yScale = scale;
							break;

						case ZoomMode.StretchToFill:
							break;

						case ZoomMode.Centre:
							xScale = 1;
							yScale = 1;
							break;

						default:
							throw new Exception ("Unhandled ZoomMode");
					}

					Point pixelWindowReferencePoint = new Point((int) (Width * windowReferencePoint.X),
						(int) (Height * windowReferencePoint.Y));
					Point pixelImageReferencePoint = new Point((int) (image.Width * imageReferencePoint.X),
						(int) (image.Height * imageReferencePoint.Y));

					e.Graphics.DrawImage(image,
						new Rectangle((int) (pixelWindowReferencePoint.X + instep - (pixelImageReferencePoint.X * xScale)),
							(int) (pixelWindowReferencePoint.Y + instep - (pixelImageReferencePoint.Y * yScale)),
							(int) (image.Width * xScale),
							(int) (image.Height * yScale)),
						new Rectangle(0, 0, image.Width, image.Height),
						GraphicsUnit.Pixel);
				}
			}

			if (watermarkOnTop)
			{
				watermarker?.Draw(this, e.Graphics);
			}
		}
	}
}