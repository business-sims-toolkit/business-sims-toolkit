using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Charts
{
	public static class GraphicsExtensions
	{
		public static void DrawRotatedString (this Graphics graphics, float angle, string text, PointF location, Font font, Brush brush)
		{
			var oldState = graphics.Save();

			graphics.ResetTransform();
			graphics.RotateTransform(angle);
			graphics.TranslateTransform(location.X, location.Y, MatrixOrder.Append);
			graphics.DrawString(text, font, brush, 0, 0);

			graphics.Restore(oldState);
		}

		public static void FillStar (this Graphics graphics, Brush brush, Rectangle bounds, float spikeThicknessFraction, int points)
		{
			var centre = new PointF( bounds.Left + (bounds.Width / 2), bounds.Top + (bounds.Height / 2));
			var xRadius = bounds.Width / 2;
			var yRadius = bounds.Height / 2;

			for (var i = 0; i < points; i++)
			{
				var spikeAngle = (Math.PI * 2 * i / points) - (Math.PI);
				var spikeAngularThickness = Math.PI * 2 / points;
				var spikeStartAngle = spikeAngle + (spikeAngularThickness / 2);
				var spikeEndAngle = spikeAngle - (spikeAngularThickness / 2);

				var spikeTip = new PointF ((float) (centre.X + (xRadius * Math.Sin(spikeAngle))), (float) (centre.Y + (yRadius * Math.Cos(spikeAngle))));
				var spikeStart = new PointF((float) (centre.X + (xRadius * spikeThicknessFraction * Math.Sin(spikeStartAngle))), (float) (centre.Y + (yRadius * spikeThicknessFraction * Math.Cos(spikeStartAngle))));
				var spikeEnd = new PointF((float) (centre.X + (xRadius * spikeThicknessFraction * Math.Sin(spikeEndAngle))), (float) (centre.Y + (yRadius * spikeThicknessFraction * Math.Cos(spikeEndAngle))));

				graphics.FillPolygon(brush, new [] { centre, spikeStart, spikeTip, spikeEnd });
			}
		}
	}
}
