using System;
using System.Drawing;
using CommonGUI;

namespace Cloud.OpsScreen
{
	public abstract class PopupPanel : FlickerFreePanel 
	{
		public event EventHandler Closed;

		public virtual void OnClosed ()
		{
			if (Closed != null)
			{
				Closed(this, EventArgs.Empty);
			}
		}


		/// <summary>
		/// This is overriden by the individual Popup panels
		/// They can ask the question whether to ditch any open data 
		/// </summary>
		/// <returns></returns>
		public virtual bool CanClose()
		{
			return true;
		}

		public virtual Size getPreferredSize()
		{
			//the default old size
			return new Size(750, 350);
		}

		public abstract bool IsFullScreen { get; }
	}
}