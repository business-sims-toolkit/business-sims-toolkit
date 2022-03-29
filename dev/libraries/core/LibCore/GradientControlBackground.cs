using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace LibCore
{
	public class GradientControlBackground
	{
		Control sourceControl;
		Rectangle sourceRegion;
		Color colour1;
		Color colour2;
		int angle;

		public GradientControlBackground (Control sourceControl, Rectangle sourceRegion, Color colour1, Color colour2, int angle)
		{
			this.sourceControl = sourceControl;
			this.sourceRegion = sourceRegion;
			this.colour1 = colour1;
			this.colour2 = colour2;

			while (angle < 0)
			{
				angle += 360;
			}
			angle = angle % 360;

			this.angle = angle;
		}

		public void Draw (Control destinationControl, Graphics graphics)
		{
			Point topLeft = destinationControl.PointToClient(sourceControl.PointToScreen(new Point (0, 0)));
			Point bottomRight = destinationControl.PointToClient(sourceControl.PointToScreen(new Point (sourceControl.Width - 1, sourceControl.Height - 1)));
			Rectangle destinationRegion = new Rectangle (topLeft.X, topLeft.Y, bottomRight.X - topLeft.X, bottomRight.Y - topLeft.Y);

			using (Brush brush = new LinearGradientBrush (destinationRegion, colour1, colour2, angle))
			{
				graphics.FillRectangle(brush, 0, 0, destinationControl.Width, destinationControl.Height);
			}
		}

		public Color GetColourAtPoint (Control destinationControl, Point point)
		{
			if ((destinationControl == null)
				|| destinationControl.IsDisposed
				|| (sourceControl == null)
				|| sourceControl.IsDisposed)
			{
				return Color.Transparent;
			}

			Point pointInSourceGradient = sourceControl.PointToClient(destinationControl.PointToScreen(point));

			Point startPoint, endPoint;
			if (angle < 90)
			{
				startPoint = new Point (sourceRegion.Left, sourceRegion.Top);
				endPoint = new Point (sourceRegion.Right, sourceRegion.Bottom);
			}
			else if (angle < 180)
			{
				startPoint = new Point (sourceRegion.Right, sourceRegion.Top);
				endPoint = new Point (sourceRegion.Left, sourceRegion.Bottom);
			}
			else if (angle < 270)
			{
				startPoint = new Point (sourceRegion.Right, sourceRegion.Bottom);
				endPoint = new Point (sourceRegion.Left, sourceRegion.Top);
			}
			else
			{
				startPoint = new Point (sourceRegion.Left, sourceRegion.Bottom);
				endPoint = new Point (sourceRegion.Right, sourceRegion.Top);
			}

			PointF axis = new PointF ((float) Math.Cos(angle * 2 * Math.PI / 360),
			                          (float) Math.Sin(angle * 2 * Math.PI / 360));

			PointF displacement = new PointF (pointInSourceGradient.X - startPoint.X,
			                                  pointInSourceGradient.Y - startPoint.Y);

			PointF endDisplacement = new PointF (endPoint.X - startPoint.X,
			                                     endPoint.Y - startPoint.Y);

			double gradientLength = Dot(endDisplacement, axis);
			double t = 0;
			if (gradientLength > 0)
			{
				t = Dot(displacement, axis) / gradientLength;
			}

			return Algorithms.Maths.Lerp(t, colour1, colour2);
		}

		double Dot (PointF a, PointF b)
		{
			return ((a.X * b.X) + (a.Y * b.Y));
		}
	}
}