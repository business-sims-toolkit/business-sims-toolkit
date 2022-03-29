using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;

namespace CommonGUI
{
	public class Watermarker : IWatermarker
	{
		bool visible;

		Point originInTopControl;
		double angle;

		int stripeThickness;
		int stripeStride;
		int horizontalRepeatLength;
		string [] textLines;
		Color backColour;
		Color foreColour;

		public Watermarker ()
		{
			visible = false;
		}

		public Watermarker (Color backColour, Color foreColour,
		                    Point originInTopControl, double angle,
							int stripeThickness, int stripeStride, int horizontalRepeatLength,
		                    params string [] textLines)
		{
			visible = true;
			this.backColour = backColour;
			this.foreColour = foreColour;

			this.originInTopControl = originInTopControl;
			this.angle = angle;

			this.stripeThickness = stripeThickness;
			this.stripeStride = stripeStride;
			this.horizontalRepeatLength = horizontalRepeatLength;

			this.textLines = textLines;
		}

		PointF TransformControlPointToMotifSpace (Control control, Point controlSpace)
		{
			Point topLevelSpace = control.TopLevelControl.PointToClient(control.PointToScreen(controlSpace));

			PointF unrotatedMotifSpace = new PointF (topLevelSpace.X - originInTopControl.X, topLevelSpace.Y - originInTopControl.Y);
			PointF unitStripeAxis = new PointF ((float) Math.Cos(angle), - (float) Math.Sin(angle));
			PointF unitStrideAxis = new PointF ((float) Math.Sin(angle), (float) Math.Cos(angle));

			return new PointF ((unrotatedMotifSpace.X * unitStripeAxis.X) + (unrotatedMotifSpace.Y * unitStripeAxis.Y),
							   (unrotatedMotifSpace.X * unitStrideAxis.X) + (unrotatedMotifSpace.Y * unitStrideAxis.Y));
		}

		public void Draw (Control control, Graphics graphics)
		{
			if (! visible)
			{
				return;
			}

			Point origin = control.PointToClient(control.TopLevelControl.PointToScreen(originInTopControl));

			PointF unitStripeAxis = new PointF ((float) Math.Cos(angle), - (float) Math.Sin(angle));
			PointF unitStrideAxis = new PointF ((float) Math.Sin(angle), (float) Math.Cos(angle));

			List<PointF> stripeSpaceControlCorners = new List<PointF> ();
			stripeSpaceControlCorners.Add(TransformControlPointToMotifSpace(control, new Point (0, 0)));
			stripeSpaceControlCorners.Add(TransformControlPointToMotifSpace(control, new Point (control.Width - 1, 0)));
			stripeSpaceControlCorners.Add(TransformControlPointToMotifSpace(control, new Point (control.Width - 1, control.Height - 1)));
			stripeSpaceControlCorners.Add(TransformControlPointToMotifSpace(control, new Point (0, control.Height - 1)));

			float x0 = stripeSpaceControlCorners.Min(p => p.X);
			float y0 = stripeSpaceControlCorners.Min(p => p.Y);
			float x1 = stripeSpaceControlCorners.Max(p => p.X);
			float y1 = stripeSpaceControlCorners.Max(p => p.Y);

			using (Brush backBrush = new SolidBrush (backColour))
			using (Brush foreBrush = new SolidBrush (foreColour))
			{
				for (int row = (int) Math.Floor(y0 / stripeStride); row <= (int) Math.Floor(y1 / stripeStride); row++)
				{
					for (int column = (int) Math.Floor(x0 / horizontalRepeatLength); column <= (int) Math.Floor(x1 / horizontalRepeatLength); column++)
					{
						Point motifOriginInControlSpace = control.PointToClient(control.TopLevelControl.PointToScreen(originInTopControl));

						List<PointF> points = new List<PointF> ();
						PointF start = new PointF (motifOriginInControlSpace.X + (column * horizontalRepeatLength * unitStripeAxis.X) + (row * stripeStride * unitStrideAxis.X),
												   motifOriginInControlSpace.Y + (column * horizontalRepeatLength * unitStripeAxis.Y) + (row * stripeStride * unitStrideAxis.Y));

						points.Add(new PointF (start.X + (unitStripeAxis.X * horizontalRepeatLength),
						                       start.Y + (unitStripeAxis.Y * horizontalRepeatLength)));

						points.Add(new PointF (start.X + (unitStripeAxis.X * horizontalRepeatLength) + (unitStrideAxis.X * stripeThickness),
											   start.Y + (unitStripeAxis.Y * horizontalRepeatLength) + (unitStrideAxis.Y * stripeThickness)));

						points.Add(new PointF (start.X + (unitStrideAxis.X * stripeThickness),
						                       start.Y + (unitStrideAxis.Y * stripeThickness)));
						points.Add(start);

						graphics.FillPolygon(backBrush, points.ToArray());

						System.Drawing.Drawing2D.Matrix oldMatrix = graphics.Transform;

						PointF textPoint = new PointF (start.X, start.Y);
						Font font = CoreUtils.SkinningDefs.TheInstance.GetFont(20, FontStyle.Bold);
						for (int k = 0; k < textLines.Length; k++)
						{
							float lineHeight = font.GetHeight(graphics);
							graphics.TranslateTransform(textPoint.X + (lineHeight * unitStrideAxis.X * k), textPoint.Y + (lineHeight * unitStrideAxis.Y * k));
							graphics.RotateTransform((float) (- angle * 360 / (2 * Math.PI)));
							graphics.DrawString(textLines[k], font, foreBrush, new Point (0, 0));
							graphics.Transform = oldMatrix;
						}
					}
				}
			}
		}
	}
}