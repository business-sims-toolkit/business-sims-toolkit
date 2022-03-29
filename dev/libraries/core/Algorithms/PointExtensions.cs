using System.Drawing;

namespace Algorithms
{
	public static class PointExtensions
	{
		public static Point ToPoint (this PointF value)
		{
			return new Point((int)value.X, (int)value.Y);
		}
	}
}
