using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using LibCore;

namespace GameDetails
{
	/// <summary>
	/// Summary description for ScrollableExpandHolder.
	/// </summary>
	public class ScrollableExpandHolder : BasePanel
	{
		protected List<Control> panels;
		protected Panel panel;

		int topGap;
        int leftGap;

		public ScrollableExpandHolder()
		{
			panels = new List<Control> ();
			panel = new Panel();
			this.Controls.Add(panel);
			this.AutoScroll = true;

			topGap = 0;
		    leftGap = 0;

			this.VisibleChanged += ScrollableExpandHolder_VisibleChanged;
		}

		void ScrollableExpandHolder_VisibleChanged (object sender, EventArgs e)
		{
			if (Visible)
			{
				DoSize();
			}
		}

		public void AddExpandablePanel (Control _panel)
		{
			_panel.SizeChanged += panel_SizeChanged;
			_panel.VisibleChanged += _panel_VisibleChanged;
				 
			panels.Add(_panel);
			panel.Controls.Add(_panel);
			DoSize();
		}

		public void RemoveExpandablePanel (ExpandablePanel panel)
		{
			panel.SizeChanged -= panel_SizeChanged;
			panel.VisibleChanged -= _panel_VisibleChanged;

			panels.Remove(panel);
			panel.Controls.Remove(panel);
			DoSize();
		}

	    protected override void OnSizeChanged (EventArgs e)
	    {
	        base.OnSizeChanged(e);
	        DoSize();
	    }

	    void _panel_VisibleChanged (object sender, EventArgs e)
		{
			DoSize();
		}

		public void DoSize()
		{
			int yoffset = topGap;
		    int xoffset = leftGap;

			foreach (Control _panel in panels)
			{
				_panel.Location = new Point (xoffset, yoffset);
				_panel.Width = Width - 20;

				if (_panel.Visible)
				{
					yoffset += _panel.Height + 5;
				}
			}

			AutoScrollPosition = new Point (0, 0);
			AutoScroll = false;
			panel.Size = new Size(this.Width - 20, yoffset);
			AutoScroll = true;
		}

		void panel_SizeChanged (object sender, EventArgs e)
		{
			DoSize();
		}
	
		public IList<Control> Panels
		{
			get
			{
				return new List<Control> (panels);
			}
		}

		public int TopGap
		{
			get
			{
				return topGap;
			}

			set
			{
				topGap = value;
				DoSize();
			}
		}

	    public int LeftGap
	    {
	        get
	        {
	            return leftGap;
	        }

	        set
	        {
	            leftGap = value;
                DoSize();
	        }
	    }
	}
}