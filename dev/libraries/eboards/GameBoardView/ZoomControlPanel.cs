using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using CoreUtils;

namespace GameBoardView
{
	public class ZoomControlPanel : Panel
	{
		public class ZoomSelectedArgs : EventArgs
		{
			public ZoomZone Zone;

			public ZoomSelectedArgs (ZoomZone zone)
			{
				Zone = zone;
			}
		}

		public delegate void ZoomSelectedHandler (ZoomControlPanel sender, ZoomSelectedArgs args);
		public event ZoomSelectedHandler ZoomSelected;

		List<ZoomZone> zones;

		public ZoomControlPanel (IList<ZoomZone> zones)
		{
			this.zones = new List<ZoomZone> ();

			foreach (ZoomZone zone in zones)
			{
				this.zones.Add(zone);

				Button button = SkinningDefs.TheInstance.CreateWindowsButton(FontStyle.Regular, 10);
				button.Text = zone.Name;
				button.Tag = zone;
				button.Click += button_Click;
			    Controls.Add(button);
			}

			DoSize();
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);

			DoSize();
		}

		void DoSize ()
		{
			Size buttonSize = new Size (70, 30);
			int gap = (Width - (zones.Count * buttonSize.Width)) / (zones.Count + 1);

			int x = gap;
			foreach (Button button in Controls)
			{
				button.Size = buttonSize;
				button.Location = new Point (x, (Height - button.Height) / 2);
				x = button.Right + gap;
			}
		}

		void button_Click (object sender, EventArgs e)
		{
			Button button = (Button) sender;
			ZoomZone zone = (ZoomZone) button.Tag;

			OnZoomSelected(zone);
		}

		void OnZoomSelected (ZoomZone zone)
		{
			if (ZoomSelected != null)
			{
				ZoomSelected(this, new ZoomSelectedArgs (zone));
			}
		}
	}
}