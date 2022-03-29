using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;

namespace Algorithms
{
    public static class RectangleExtensions
    {
        public static Rectangle CentreSubRectangle(this Rectangle referenceBounds, int width, int height)
        {
            return AlignRectangle(referenceBounds, width, height, StringAlignment.Center, StringAlignment.Center);
        }

        public static Rectangle CentreSubRectangle (this Rectangle referenceBounds, Size size)
        {
            return CentreSubRectangle(referenceBounds, size.Width, size.Height);
        }

        public static RectangleF CentreSubRectangle(this RectangleF referenceBounds, float width, float height)
        {
            return AlignRectangle(referenceBounds, width, height, StringAlignment.Center, StringAlignment.Center);
        }

        public static Rectangle AlignRectangle (this Rectangle referenceBounds, Size size,
												StringAlignment horizontalAlignment = StringAlignment.Center,
                                                StringAlignment verticalAlignment = StringAlignment.Center,
												int horizontalOffset = 0, int verticalOffset = 0)
        {
            return AlignRectangle(referenceBounds, size.Width, size.Height, horizontalAlignment, verticalAlignment, horizontalOffset, verticalOffset);
        }

        public static Rectangle AlignRectangle (this Rectangle referenceBounds, int width, int height,
												StringAlignment horizontalAlignment = StringAlignment.Center,
                                                StringAlignment verticalAlignment = StringAlignment.Center,
                                                int horizontalOffset = 0, int verticalOffset = 0)
        {
            var horizontalReference = 0;
            var xOffset = 0;

            switch (horizontalAlignment)
            {
                case StringAlignment.Near:
                    xOffset = 0;
                    horizontalReference = referenceBounds.X;
                    break;
                case StringAlignment.Center:
                    horizontalReference = referenceBounds.X;
                    xOffset = (referenceBounds.Width - width) / 2;
                    break;
                case StringAlignment.Far:
                    horizontalReference = referenceBounds.Right;
                    xOffset = -width;
	                //horizontalOffset = -horizontalOffset;
					break;
            }

            var verticalReference = 0;
            var yOffset = 0;

            switch (verticalAlignment)
            {
                case StringAlignment.Near:
                    yOffset = 0;
                    verticalReference = referenceBounds.Y;
                    break;
                case StringAlignment.Center:
                    verticalReference = referenceBounds.Y;
                    yOffset = (referenceBounds.Height - height) / 2;
                    break;
                case StringAlignment.Far:
                    verticalReference = referenceBounds.Bottom;
                    yOffset = -height;
	                //verticalOffset = -verticalOffset;
					break;
            }
            
            return new Rectangle(horizontalReference + xOffset + horizontalOffset, verticalReference + yOffset + verticalOffset, width, height);
        }

        public static RectangleF AlignRectangle (this RectangleF referenceBounds, SizeF size,
												 StringAlignment horizontalAlignment = StringAlignment.Center,
                                                 StringAlignment verticalAlignment = StringAlignment.Center,
                                                 float horizontalOffset = 0f, float verticalOffset = 0f)
        {
            return AlignRectangle(referenceBounds, size.Width, size.Height, horizontalAlignment, verticalAlignment, horizontalOffset, verticalOffset);
        }

        public static RectangleF AlignRectangle (this RectangleF referenceBounds, float width, float height, StringAlignment horizontalAlignment = StringAlignment.Center, 
                                                 StringAlignment verticalAlignment = StringAlignment.Center, float horizontalOffset = 0f, float verticalOffset = 0f)
        {
            float horizontalReference = 0;
            float xOffset = 0;

            switch (horizontalAlignment)
            {
                case StringAlignment.Near:
                    xOffset = 0;
                    horizontalReference = referenceBounds.X;
                    break;
                case StringAlignment.Center:
                    horizontalReference = referenceBounds.X;
                    xOffset = (referenceBounds.Width - width) / 2;
                    break;
                case StringAlignment.Far:
                    horizontalReference = referenceBounds.Right;
                    xOffset = -width;
	                //horizontalOffset = -horizontalOffset;
                    break;
            }

            float verticalReference = 0;
            float yOffset = 0;

            switch (verticalAlignment)
            {
                case StringAlignment.Near:
                    yOffset = 0;
                    verticalReference = referenceBounds.Y;
                    break;
                case StringAlignment.Center:
                    verticalReference = referenceBounds.Y;
                    yOffset = (referenceBounds.Height - height) / 2;
                    break;
                case StringAlignment.Far:
                    verticalReference = referenceBounds.Bottom;
                    yOffset = -height;
	                //verticalOffset = -verticalOffset;
					break;
            }

            return new RectangleF(horizontalReference + xOffset + horizontalOffset, verticalReference + yOffset + verticalOffset, width, height);
        }
        
        /// <summary>
        /// Creates a new RectangleF that is widthFraction wider and heightFraction taller than the source RectangleF.
        /// A negative fraction will give a smaller RectangleF and a positive fraction will give a larger one- because maths!
        /// </summary>
        /// <param name="source"></param>
        /// <param name="widthFraction"></param>
        /// <param name="heightFraction"></param>
        /// <returns></returns>
        public static RectangleF ExpandByFraction (this RectangleF source, float widthFraction, float heightFraction)
        {
            widthFraction = widthFraction + 1;
            heightFraction = heightFraction + 1;

            var width = widthFraction * source.Width;
            var height = heightFraction * source.Height;

            Debug.Assert(Math.Abs(width) > float.Epsilon && Math.Abs(height) > float.Epsilon, "New dimensions can't be zero.");

            if (Math.Abs(width) < float.Epsilon)
            {
                width = source.Width;
            }

            if (Math.Abs(height) < float.Epsilon)
            {
                height = source.Height;
            }

            return source.AlignRectangle(width, height, StringAlignment.Center, StringAlignment.Center);
        }

        public static RectangleF ExpandByFraction(this RectangleF source, float fraction)
        {
            return ExpandByFraction(source, fraction, fraction);
        }

		public static RectangleF ExpandByAmount (this RectangleF source, float amount)
	    {
		    return ExpandByAmount(source, amount, amount);
	    }

	    public static SizeF ExpandByAmount (this SizeF source, float amount)
	    {
		    return new SizeF (source.Width + amount, source.Height + amount);
	    }

		public static RectangleF ExpandByAmount (this RectangleF source, float widthAmount, float heightAmount)
	    {
		    return AlignRectangle(source, source.Width + widthAmount, source.Height + heightAmount);
	    }

	    public static Point FindPointByRatio (this Rectangle destination, float xRatio, float yRatio)
	    {
			return new Point(destination.X + (int)(xRatio * destination.Width), destination.Y + (int)(yRatio * destination.Height));
	    }

	    public static Rectangle ToRectangle (this RectangleF original)
	    {
			return new Rectangle((int)original.X, (int)original.Y, (int)original.Width, (int)original.Height);
	    }
		
	    public static List<RectangleF> SubDivideRectangleHorizontally (this RectangleF original, float subRectangleHeight, int numSubRectangles, float innerGap, float outerGap, StringAlignment verticalAlignment = StringAlignment.Center)
	    {
			Debug.Assert(numSubRectangles > 0);
		    var width = original.Width - 2 * outerGap;

		    width -= (numSubRectangles - 1) * innerGap;

		    var subWidth = width / numSubRectangles;
			
		    var subY = MapRangeToRange(original.Y, original.Height, subRectangleHeight, verticalAlignment);
			
		    return Enumerable.Range(0, numSubRectangles).Select(i => original.Left + i * (subWidth + innerGap)).Select(x => new RectangleF(x, subY, subWidth, subRectangleHeight)).ToList();
	    }

		// TODO name the method more accurately
	    public static float MapRangeToRange (float referenceValue, float sizeOfOriginalRange, float sizeOfMappedRange, StringAlignment alignment)
	    {
		    switch (alignment)
		    {
				case StringAlignment.Near:
					return referenceValue;
				case StringAlignment.Center:
					return referenceValue + (sizeOfOriginalRange - sizeOfMappedRange) / 2;
				case StringAlignment.Far:
					return (referenceValue + sizeOfOriginalRange) - sizeOfMappedRange;
		    }

		    return 0;
	    }

	    public static RectangleF ExtendToInclude (this RectangleF start, RectangleF extra)
	    {
		    var left = Math.Min(start.Left, extra.Left);
		    var top = Math.Min(start.Top, extra.Top);
		    var right = Math.Max(start.Right, extra.Right);
		    var bottom = Math.Max(start.Bottom, extra.Bottom);

		    return new RectangleF (left, top, right - left, bottom - top);
		}

	    public static RectangleF ExtendToInclude (this RectangleF start, PointF extra)
	    {
		    var left = Math.Min(start.Left, extra.X);
		    var top = Math.Min(start.Top, extra.Y);
		    var right = Math.Max(start.Right, extra.X);
		    var bottom = Math.Max(start.Bottom, extra.Y);

		    return new RectangleF(left, top, right - left, bottom - top);
		}
		
	}
}
