using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

using CommonGUI;

namespace NeowUi
{
	public abstract class EboardTopBar : FlickerFreePanel
	{
		protected NavBar navBar;

		public EboardTopBar ()
		{
			SetNavBar(CreateNavBar());
		}

		void SetNavBar (NavBar navBar)
		{
			if (this.navBar != navBar)
			{
				if (this.navBar != null)
				{
					Controls.Remove(this.navBar);
					this.navBar = null;
				}

				if (navBar != null)
				{
					this.navBar = navBar;
					Controls.Add(navBar);
				}
			}

			DoSize();
		}

		protected abstract NavBar CreateNavBar ();

		protected override void OnPaint (PaintEventArgs e)
		{
			base.OnPaint(e);
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);
			DoSize();
			Invalidate();
		}

		protected virtual void DoSize ()
		{
			if (navBar != null)
			{
				int topStrip = 8;
				navBar.Bounds = new Rectangle (0, topStrip, navBar.PreferredWidth, Height - topStrip);
			}
		}
	}
}