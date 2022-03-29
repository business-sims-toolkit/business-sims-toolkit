using System.Drawing;

namespace Algorithms
{
    public static class SizeFExtensions
    {
        public static SizeF ExpandByFraction (this SizeF source, float fraction)
        {
            return new SizeF(source.Width + 2 * (source.Width * fraction), source.Height + 2 * (source.Height * fraction));
        }
    }
}
