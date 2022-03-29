using System;
using System.Drawing;

namespace Algorithms
{
    public static class ColorExtensions
    {
        public static Color Shade(this Color original, float shadeFraction)
        {
            return Maths.Lerp(shadeFraction, original, Color.Black);
        }

        public static Color Tint(this Color original, float tintFraction)
        {
            return Maths.Lerp(tintFraction, original, Color.White);
        }

        public static Color Lerp(this Color original, Color target, float fraction)
        {
            return Maths.Lerp(fraction, original, target);
        }

        public static bool EqualsByComponents(this Color left, Color right)
        {
            return left.R == right.R && left.G == right.G && left.B == right.B;
        }

	    public static bool EqualsByComponentsWithThreshold (this Color left, Color right, int threshold)
	    {
		    return Math.Abs(left.R - right.R) <= threshold && 
		           Math.Abs(left.G - right.G) <= threshold &&
		           Math.Abs(left.B - right.B) <= threshold;

	    }
    }
}
