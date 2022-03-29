using System.Drawing;
using System.Windows.Forms;

using LibCore;

namespace Charts
{
	public class LeftYAxis : YAxis
	{
		public LeftYAxis()
		{
			this.SuspendLayout();
			axisTitle = new UpVerticalLabel();
			Controls.Add(axisTitle);
			align = ContentAlignment.MiddleRight;
		    showTicks = true;
			this.ResumeLayout(false);
		}

	    bool showTicks;
	    public bool ShowTicks
	    {
	        get
	        {
	            return showTicks;
	        }

	        set
	        {
	            showTicks = value;
	            Invalidate();
	        }
	    }

	    protected override void OnPaint( PaintEventArgs e )
		{
			base.OnPaint(e);

			SetStroke(1, textColour);
			MoveTo(Width - 1, 0);
			LineTo(e, Width - 1, Height);

			if ((stripeColours == null) && showTicks)
			{
				double ystep = Height / (double) _steps;
				int y = 0;
				//
				if ((ystep > 0) && (_steps > 0))
				{
					for (int i = 0; i <= _steps; ++i)
					{
						double off = Height - i * ystep;
						y = (int) off;
						MoveTo(Width - 1, y);
						LineTo(e, Width - 6, y);
					}
				}
			}
		}
	}	
}
