using System;
using System.Drawing;

namespace Algorithms
{
	public class RectangleFFromBounds
	{
		float? left;
		float? right;
		float? width;

		float? top;
		float? bottom;
		float? height;

		public float Left
		{
			set => left = value;
		}

		public float Right
		{
			set => right = value;
		}

		public float Width
		{
			set => width = value;
		}

		public float Top
		{
			set => top = value;
		}

		public float Bottom
		{
			set => bottom = value;
		}

		public float Height
		{
			set => height = value;
		}

		void CalculateInterval (ref float? min, ref float? max, ref float? size, string label)
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

		public RectangleF ToRectangleF ()
		{
			CalculateInterval(ref left, ref right, ref width, "Width");
			CalculateInterval(ref top, ref bottom, ref height, "Height");

			return new RectangleF (left.Value, top.Value, width.Value, height.Value);
		}
	}
}
