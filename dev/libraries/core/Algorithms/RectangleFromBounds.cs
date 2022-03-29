using System;
using System.Drawing;

namespace Algorithms
{
	public class RectangleFromBounds
	{
		int? left;
		int? right;
		int? width;

		int? top;
		int? bottom;
		int? height;

		public int Left
		{
			set => left = value;
		}

		public int Right
		{
			set => right = value;
		}

		public int Width
		{
			set => width = value;
		}

		public int Top
		{
			set => top = value;
		}

		public int Bottom
		{
			set => bottom = value;
		}

		public int Height
		{
			set => height = value;
		}

		void CalculateInterval (ref int? min, ref int? max, ref int? size, string label)
		{
			if (! min.HasValue)
			{
				if (! (max.HasValue && size.HasValue))
				{
					throw new Exception ($"{label} underspecified");
				}

				min = max.Value - size.Value;
			}
			else if (! size.HasValue)
			{
				if (! (min.HasValue && max.HasValue))
				{
					throw new Exception($"{label} underspecified");
				}

				size = max.Value - min.Value;
			}
			else if (! max.HasValue)
			{
			}
			else
			{
				if ((max.Value - min.Value) != size.Value)
				{
					throw new Exception($"{label} overspecified");
				}
			}
		}

		public Rectangle ToRectangle ()
		{
			CalculateInterval(ref left, ref right, ref width, "Width");
			CalculateInterval(ref top, ref bottom, ref height, "Height");

			return new Rectangle (left.Value, top.Value, width.Value, height.Value);
		}
	}
}
