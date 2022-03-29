using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using CommonGUI;
using Events;

namespace ResizingUi
{
    public class MouseCursorForm : Form
    {
        protected override bool ShowWithoutActivation => true;

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;

                cp.ExStyle |= 0x00000008;
                
                return cp;
            }
        }
    }

    public abstract class SharedMouseEventControl : FlickerFreePanel
    {
	    //public abstract Dictionary<string, Rectangle> BoundIdsToRectangles { get; }

		// Replace dictionary with this, use BoundsIdsToRectangles
		public abstract List<KeyValuePair<string, Rectangle>> BoundIdsToRectangles { get; }

        public event EventHandler<SharedMouseEventArgs> MouseEventFired;

        public virtual void ReceiveMouseEvent (SharedMouseEventArgs args)
        {
            //ShowCursor(args.MouseLocation);
        }

        protected virtual void OnMouseEventFired(SharedMouseEventArgs args)
        {
            MouseEventFired?.Invoke(this, args);
        }		
	}
}
