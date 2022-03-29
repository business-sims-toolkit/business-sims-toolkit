using System;
using System.Collections.Generic;
using System.Drawing;

namespace Algorithms
{
	public static class Maths
	{
		public static double Lerp (double t, double a, double b)
		{
			return (t * b) + ((1 - t) * a);
		}

		public static RectangleF Lerp (double t, RectangleF a, RectangleF b)
		{
			return new RectangleF ((float) Lerp(t, a.Left, b.Left), (float) Lerp(t, a.Top, b.Top), (float) Lerp(t, a.Width, b.Width), (float) Lerp(t, a.Height, b.Height));
		}

		public static Rectangle Lerp (double t, Rectangle a, Rectangle b)
		{
			return new Rectangle ((int) Lerp(t, a.Left, b.Left), (int) Lerp(t, a.Top, b.Top), (int) Lerp(t, a.Width, b.Width), (int) Lerp(t, a.Height, b.Height));
		}

	    public static PointF Lerp (double t, PointF a, PointF b)
	    {
            return new PointF((float)Lerp(t, a.X, b.X), (float)Lerp(t, a.Y, b.Y));
	    }

	    public static SizeF Lerp (double t, SizeF a, SizeF b)
	    {
            return new SizeF((float)Lerp(t, a.Width, b.Width), (float)Lerp(t, a.Height, b.Height));
	    }

		public static Color Lerp (double t, Color a, Color b)
		{
			return Color.FromArgb((int) Lerp(t, a.A, b.A), (int) Lerp(t, a.R, b.R), (int) Lerp(t, a.G, b.G), (int) Lerp(t, a.B, b.B));
		}

		public static double MapBetweenRanges (double x, double x0, double x1, double y0, double y1)
		{
			return y0 + ((y1 - y0) * (x - x0) / (x1 - x0));
		}

		public static float MapBetweenRanges (float value, float startRange1, float endRange1, float startRange2, float endRange2) 
		{
			return startRange2 + (endRange2 - startRange2) * (value - startRange1) / (endRange1 - startRange1);
		}
        
		public static int Clamp (int t, int min, int max)
		{
			return Math.Max(min, Math.Min(t, max));
		}

		public static double Clamp (double t, double min, double max)
		{
			return Math.Max(min, Math.Min(t, max));
		}
		
		// TODO make this a Math Extension method instead??
		public static T Clamp<T> (T value, T min, T max) where T : IComparable<T>
		{
			if (value.CompareTo(min) < 0) return min;
			if (value.CompareTo(max) > 0) return max;

			return value;
		}

		public static double SmoothStep (double t)
		{
			return t * t * (3 - (2 * t));
		}

		/// <summary>
		/// Return the greatest common divisor of a set of integers.
		/// </summary>
		public static int Gcd (int a, int b, params int [] others)
		{
			if (others.Length > 0)
			{
				foreach (int other in others)
				{
					b = Gcd(b, other);
				}
			}

			while (b != 0)
			{
				int temp = b;
				b = a % b;
				a = temp;
			}

			return a;
		}

		public static bool IsPointInPolygon (Point p, IList<Point> polygon)
		{
			if (polygon.Count < 3)
			{
				return false;
			}

			Point p1, p2;
			bool inside = false;
			Point oldPoint = new Point (polygon[polygon.Count - 1].X, polygon[polygon.Count - 1].Y);
			for (int i = 0; i < polygon.Count; i++)
			{
				Point newPoint = new Point (polygon[i].X, polygon[i].Y);
				if (newPoint.X > oldPoint.X)
				{
					p1 = oldPoint;
					p2 = newPoint;
				}
				else
				{
					p1 = newPoint;
					p2 = oldPoint;
				}

				if (((newPoint.X < p.X) == (p.X <= oldPoint.X))
					&& (((p.Y - p1.Y) * (p2.X - p1.X)) < ((p2.Y - p1.Y) * (p.X - p1.X))))
				{
					inside = ! inside;
				}
				oldPoint = newPoint;
			}

			return inside;
		}

		/// <summary>
		/// Project a point onto a line and return the corresponding fraction along the line.
		/// </summary>
		public static float ProjectPointToLine (Point point, Point endA, Point endB)
		{
			return ProjectPointToLine(new PointF (point.X, point.Y),
									  new PointF (endA.X, endA.Y),
									  new PointF (endB.X, endB.Y));
		}

		/// <summary>
		/// Project a point onto a line and return the corresponding fraction along the line.
		/// </summary>
		public static float ProjectPointToLine (PointF point, PointF endA, PointF endB)
		{
			PointF unitLine = new PointF (endB.X - endA.X, endB.Y - endA.Y);
			float length = (float) Math.Sqrt((unitLine.X * unitLine.X) + (unitLine.Y * unitLine.Y));
			unitLine.X /= length;
			unitLine.Y /= length;

			return (((point.X - endA.X) * unitLine.X) + ((point.Y - endA.Y) * unitLine.Y)) / length;
		}

		public static Rectangle GetPolygonBounds (IList<Point> points)
		{
			Rectangle? bounds = null;

			foreach (Point point in points)
			{
				if (bounds == null)
				{
					bounds = new Rectangle (point, new Size (0, 0));
				}
				else
				{
					bounds = (new RectangleFromBounds
					{
						Left = Math.Min(bounds.Value.Left, point.X),
						Top = Math.Min(bounds.Value.Top, point.Y),
						Right = Math.Max(bounds.Value.Right, point.X),
						Bottom = Math.Max(bounds.Value.Bottom, point.Y)
					}).ToRectangle();
				}
			}

			if (bounds.HasValue)
			{
				return bounds.Value;
			}
			else
			{
				return Rectangle.Empty;
			}
		}

		public static RectangleF GetPolygonBounds (IList<PointF> points)
		{
			RectangleF? bounds = null;

			foreach (PointF point in points)
			{
				if (bounds == null)
				{
					bounds = new RectangleF (point, new SizeF (0, 0));
				}
				else
				{
					bounds = (new RectangleFFromBounds
					{
						Left = Math.Min(bounds.Value.Left, point.X),
						Top = Math.Min(bounds.Value.Top, point.Y),
						Right = Math.Max(bounds.Value.Right, point.X),
						Bottom = Math.Max(bounds.Value.Bottom, point.Y)
					}).ToRectangleF();
				}
			}

			if (bounds.HasValue)
			{
				return bounds.Value;
			}
			else
			{
				return RectangleF.Empty;
			}
		}

		/// <summary>
		/// Given an interval for a graph, rounds it up to the next "nice" figure (ie 10^n * {1, 2, 2.5 or 5}).
		/// </summary>
		public static double RoundToNiceInterval (double start)
		{
			int sign = Math.Sign(start);
			start = Math.Abs(start);
			int exponent = (int) Math.Floor(Math.Log10(start));

			double radix = start / Math.Pow(10, exponent);

			if (radix <= 1)
			{
				radix = 1;
			}
			else if (radix <= 2)
			{
				radix = 2;
			}
			else if (radix <= 2.5)
			{
				radix = 2.5;
			}
			else
			{
				radix = Math.Ceiling(radix);
			}

			return sign * radix * Math.Pow(10, exponent);
		}

		public static RectangleF MapRectangle (RectangleF source, RectangleF from, RectangleF to)
		{
			return new RectangleFFromBounds
			{
				Left = (float) MapBetweenRanges(source.Left, from.Left, from.Right, to.Left, to.Right),
				Top = (float) MapBetweenRanges(source.Top, from.Top, from.Bottom, to.Top, to.Bottom),
				Right = (float) MapBetweenRanges(source.Right, from.Left, from.Right, to.Left, to.Right),
				Bottom = (float) MapBetweenRanges(source.Bottom, from.Top, from.Bottom, to.Top, to.Bottom)
			}.ToRectangleF();
		}
	}
}