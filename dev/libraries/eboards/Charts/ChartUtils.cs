using System.Drawing;
using System.Windows.Forms;

using LibCore;

namespace Charts
{
	/// <summary>
	/// Summary description for ChartUntils.
	/// </summary>
	public class ChartUtils
	{
		public ChartUtils()
		{
		}
        
		public static Color DarkerColor(Color c)
		{
			byte r, g, b;
			r = (byte)(c.R * 0.55);
			g = (byte)(c.G * 0.55);
			b = (byte)(c.B * 0.55);

			return Color.FromArgb(255, r, g, b);
		}

		public static void DrawGrid(PaintEventArgs e, VisualPanel vp, Color c, int x, int y, int width, int height, 
			double xsegs, double ysegs)
		{
			DrawGrid(e,vp,c,x,y,width,height,xsegs,ysegs,1,1);
		}

		public static void DrawGrid(PaintEventArgs e, VisualPanel vp, Color c, int x, int y, int width, int height, 
				double xsegs, double ysegs, int hwidth, int vwidth)
		{
			vp.SetStroke(vwidth, c);
			//
			double xstep = width / xsegs;
			//
			if(xstep > 0)
			{
				for(double ix = x; (int)ix <= x+width; ix+=xstep)
				{
					vp.MoveTo((int)ix, y);
					vp.LineTo(e,(int) ix, y+height);
				}
			}
			//
			vp.SetStroke(hwidth, c);
			//
			double ystep = height / ysegs;
			//
			if(ystep > 0)
			{
				for(double iy = y; iy <= y+height; iy+=ystep)
				{
					vp.MoveTo(x,(int) iy);
					vp.LineTo(e, x+width, (int)iy);
				}
			}
		}
	}
}
