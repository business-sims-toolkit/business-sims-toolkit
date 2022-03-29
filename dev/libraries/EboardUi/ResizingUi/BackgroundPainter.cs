using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Algorithms;

namespace ResizingUi
{
    public static class BackgroundPainter
    {
        static Point TransformBetweenControls(Point fromPoint, Control fromControl, Control toControl)
        {
            return toControl.PointToClient(fromControl.PointToScreen(fromPoint));
        }

        public static void Paint(Control control, Graphics graphics)
        {
            ZoomMode zoomMode;
            switch (control.TopLevelControl.BackgroundImageLayout)
            {
                case ImageLayout.None:
                    return;

                case ImageLayout.Center:
                    zoomMode = ZoomMode.Centre;
                    break;

                case ImageLayout.Tile:
                    zoomMode = ZoomMode.Tile;
                    break;

                case ImageLayout.Stretch:
                    zoomMode = ZoomMode.StretchToFill;
                    break;

                case ImageLayout.Zoom:
                default:
                    zoomMode = ZoomMode.PreserveAspectRatioWithLetterboxing;
                    break;
            }

            Paint(control, graphics, control.TopLevelControl, control.TopLevelControl.BackgroundImage, zoomMode);
        }

        public static void Paint (Control control, Graphics graphics, CascadedBackgroundProperties properties)
        {
            Paint(control, graphics, properties.CascadedReferenceControl, properties.CascadedBackgroundImage, properties.CascadedBackgroundImageZoomMode);
        }

        public static void Paint(Control control, Graphics graphics, Control referenceControl, Image image, ZoomMode zoomMode)
        {
            Paint(control, graphics, referenceControl, image, zoomMode, new PointF(0.5f, 0.5f), new PointF(referenceControl.ClientSize.Width / 2, referenceControl.ClientSize.Height / 2));
        }

        public static void Paint (Control control, Graphics graphics, Control referenceControl, Image image, ZoomMode zoomMode, PointF referencePointInImage, PointF referencePointInReferenceControl)
        {
            using (var brush = new SolidBrush (referenceControl.BackColor))
            {
                graphics.FillRectangle(brush, new Rectangle (0, 0, control.ClientSize.Width, control.ClientSize.Height));
            }

            if (image == null)
            {
                return;
            }

            switch (zoomMode)
            {
                case ZoomMode.Centre:
                    PaintImage(image, graphics,
                        1, 1,
                        TransformBetweenControls(new Point((int)referencePointInReferenceControl.X, (int)referencePointInReferenceControl.Y), referenceControl, control));
                    break;

                case ZoomMode.StretchToFill:
                    PaintImage(image, graphics,
                        (float)referenceControl.ClientSize.Width / image.Width, (float)referenceControl.ClientSize.Height / image.Height,
                        TransformBetweenControls(new Point((int)referencePointInReferenceControl.X, (int)referencePointInReferenceControl.Y), referenceControl, control));
                    break;

                case ZoomMode.PreserveAspectRatioWithLetterboxing:
                    {
                        var scale = Math.Min((float)referenceControl.ClientSize.Width / image.Width, (float)referenceControl.ClientSize.Height / image.Height);
                        PaintImage(image, graphics,
                            scale, scale,
                            TransformBetweenControls(new Point((int)referencePointInReferenceControl.X, (int)referencePointInReferenceControl.Y), referenceControl, control));
                        break;
                    }

                case ZoomMode.PreserveAspectRatioWithCropping:
                    {
                        var scale = Math.Max((float)referenceControl.ClientSize.Width / image.Width, (float)referenceControl.ClientSize.Height / image.Height);
                        PaintSubImage(control, image, graphics,
                            scale, scale,
                            TransformBetweenControls(new Point ((int) referencePointInReferenceControl.X, (int) referencePointInReferenceControl.Y), referenceControl, control));
                        break;
                    }

                case ZoomMode.Tile:
                    {
                        var topLeft = TransformBetweenControls(new Point(0, 0), control, referenceControl);
                        var bottomRight = TransformBetweenControls(new Point(control.ClientSize.Width, control.ClientSize.Height), control, referenceControl);

                        for (var row = (int)(topLeft.Y / image.Height); row <= (1 + (bottomRight.Y / image.Height)); row++)
                        {
                            for (var column = (int)(topLeft.X / image.Width); column <= (1 + (bottomRight.X / image.Width)); column++)
                            {
                                graphics.DrawImageUnscaled(image, TransformBetweenControls(new Point(column * image.Width, row * image.Height), referenceControl, control));
                            }
                        }
                        break;
                    }
            }
        }

	    static Rectangle ClipRectangle (Rectangle source, Rectangle clippingRectangle)
	    {
		    var left = Math.Max(source.Left, clippingRectangle.Left);
		    var top = Math.Max(source.Top, clippingRectangle.Top);
		    var right = Math.Min(source.Right, clippingRectangle.Right);
		    var bottom = Math.Min(source.Bottom, clippingRectangle.Bottom);

		    return new Rectangle (left, top, right - left, bottom - top);
	    }

	    static void PaintSubImage (Control control, Image image, Graphics graphics, float scaleX, float scaleY, PointF centre)
	    {
		    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

		    var unclippedDestination = new Rectangle ((int) (centre.X - (image.Width * scaleX / 2)), (int) (centre.Y - (image.Height * scaleY / 2)), (int) (image.Width * scaleX), (int) (image.Height * scaleY));
			var clippedDestination = ClipRectangle(unclippedDestination,
													new Rectangle (0, 0, control.ClientSize.Width, control.ClientSize.Height));
		    var clippedImageSource = Maths.MapRectangle(clippedDestination, unclippedDestination, new RectangleF (0, 0, image.Width, image.Height));

			graphics.DrawImage(image, clippedDestination, clippedImageSource, GraphicsUnit.Pixel);
	    }

		static void PaintImage(Image image, Graphics graphics, float scaleX, float scaleY, PointF centre)
        {
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.DrawImage(image,
                new Rectangle((int)(centre.X - (image.Width * scaleX / 2)), (int)(centre.Y - (image.Height * scaleY / 2)), (int)(image.Width * scaleX), (int)(image.Height * scaleY)),
                new Rectangle(0, 0, image.Width, image.Height),
                GraphicsUnit.Pixel);
        }

	    public static void PaintGradientBackground (Control control, Graphics graphics,
	                                                GradientBackgroundProperties gradientBackgroundProperties)
	    {
		    var offsetPosition = new Point(gradientBackgroundProperties.Bounds.X - control.Left,
			    gradientBackgroundProperties.Bounds.Y - control.Top);

		    var gradientImage = gradientBackgroundProperties.Image;

		    if (gradientImage != null)
		    {
				graphics.DrawImage(gradientImage, offsetPosition);
		    }
		    else
		    {
			    graphics.FillRectangle(Brushes.HotPink, control.ClientRectangle);
		    }
	    }
    }
}