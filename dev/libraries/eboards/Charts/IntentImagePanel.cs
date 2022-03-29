using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using Events;
using LibCore;
using ResizingUi;

namespace Charts
{
    public class IntentImagePanel : SharedMouseEventControl
    {
        Bitmap bmp_ControlContent_Issues = null;
        Bitmap bmp_ControlContent_Actions = null;
        bool showFull = false;

        public IntentImagePanel(string ContentImageName_Issues, string ContentImageName_Actions)
        {
            bmp_ControlContent_Issues = (Bitmap)Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + ContentImageName_Issues);
            bmp_ControlContent_Actions = (Bitmap)Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + ContentImageName_Actions);
        }

        public IntentImagePanel(Bitmap bmp_ContentImageName_Issues, Bitmap bmp_ContentImageName_Actions)
        {
            bmp_ControlContent_Issues = bmp_ContentImageName_Issues;
            bmp_ControlContent_Actions = bmp_ContentImageName_Actions;
        }

        public void GetNewContent(string ContentImageName_Issues, string ContentImageName_Actions)
        {
            bmp_ControlContent_Issues = (Bitmap)Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + ContentImageName_Issues);
            bmp_ControlContent_Actions = (Bitmap)Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + ContentImageName_Actions);
            Refresh();
        }

        protected override void OnClick(EventArgs e)
        {
            ToggleView();

            OnMouseEventFired(new SharedMouseEventArgs(null, MouseButtons.Left, Size));
        }

        void ToggleView ()
        {
            showFull = !showFull;
            Refresh();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
	        using (var brush = new SolidBrush (BackColor))
	        {
		        e.Graphics.FillRectangle(brush, 0, 0, Width, Height);
	        }

	        var outerBounds = new Rectangle (0, 0, Width, Height);

	        if (showFull)
            {
                if (bmp_ControlContent_Actions != null)
                {
	                DrawScaledImage(e.Graphics, outerBounds, bmp_ControlContent_Actions);
                }
            }
            else
            {
                if (bmp_ControlContent_Issues != null)
                {
	                DrawScaledImage(e.Graphics, outerBounds, bmp_ControlContent_Issues);
                }
            }
        }

	    void DrawScaledImage (Graphics graphics, Rectangle bounds, Image image)
	    {
		    var scale = Math.Min(bounds.Width * 1.0f / image.Width, bounds.Height * 1.0f / image.Height);
		    var shownSize = new Size ((int) (image.Width * scale), (int) (image.Height * scale));
		    var shownBounds = new Rectangle (bounds.Left + ((bounds.Width - shownSize.Width) / 2), bounds.Top + ((bounds.Height - shownSize.Height) / 2), shownSize.Width, shownSize.Height);

		    graphics.DrawImage(image, shownBounds);
		}

		protected override void OnSizeChanged (EventArgs e)
	    {
		    base.OnSizeChanged(e);
		    Invalidate();
	    }

	    public override List<KeyValuePair<string, Rectangle>> BoundIdsToRectangles =>
		    new List<KeyValuePair<string, Rectangle>> { new KeyValuePair<string, Rectangle>("intent_all", RectangleToScreen(ClientRectangle)) };

	    public override void ReceiveMouseEvent (SharedMouseEventArgs args)
        {
            if (args.Button != MouseButtons.None)
            {
                ToggleView();
            }
        }
    }
}
