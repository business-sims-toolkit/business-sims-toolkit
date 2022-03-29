using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace UI
{
	public class DoubleBufferedPanel : Panel
	{
		public DoubleBufferedPanel ()
		{
			this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			this.SetStyle(ControlStyles.UserPaint, true);
			this.SetStyle(ControlStyles.DoubleBuffer, true);
		}
	}
}