using System;
using System.Drawing;
using System.Windows.Forms;

using Algorithms;
namespace Events
{
    public class SharedMouseEventArgs : EventArgs
    {
        public bool MousePresent => MouseLocation != null;
        public Point? MouseLocation { get; }
        public MouseButtons Button { get; }
		// TODO no longer need to be public
        public float XRatio => (MouseLocation?.X ?? 0) / (float)sourceSize.Width;
        public float YRatio => (MouseLocation?.Y ?? 0) / (float) sourceSize.Height;

        readonly Size sourceSize;

		public string BoundsId { get; }

        public SharedMouseEventArgs(Point? mouseLocation, MouseButtons button, Size size, string boundsId = null)
        {
            MouseLocation = mouseLocation;
            Button = button;
            sourceSize = size;

	        BoundsId = boundsId;
        }

        public Point? CalculateMouseLocation (Size destinationSize)
        {
            if (MouseLocation == null)
            {
                return null;
            }
            
            return new Point((int)(destinationSize.Width * XRatio), (int)(destinationSize.Height * YRatio));
        }

	    public Point? CalculateMouseLocation (Rectangle destinationBounds)
	    {
		    if (MouseLocation == null)
		    {
			    return null;
		    }

		    return destinationBounds.FindPointByRatio(XRatio, YRatio);
	    }
    }
}
