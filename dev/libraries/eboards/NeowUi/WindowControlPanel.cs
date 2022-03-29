using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

using LibCore;
using CommonGUI;

namespace NeowUi
{
	public class WindowControlPanel : Panel
	{
		ImageButton minimise;
		ImageButton toggleSize;
		ImageButton close;

		public WindowControlPanel ()
		{
			minimise = new ImageButton (0);
			minimise.SetVariants(@"images\buttons\minimise.png");
			minimise.SetAutoSize();
			Controls.Add(minimise);
			minimise.ButtonPressed += minimise_ButtonPressed;

			toggleSize = new ImageButton (0);
			toggleSize.SetVariants(@"images\buttons\toggle_size.png");
			toggleSize.SetAutoSize();
			Controls.Add(toggleSize);
			toggleSize.ButtonPressed += toggleSize_ButtonPressed;

			close = new ImageButton(0);
			close.SetVariants(@"images\buttons\close.png");
			close.SetAutoSize();
			Controls.Add(close);
			close.ButtonPressed += close_ButtonPressed;

			DoSize();
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);
			DoSize();
		}

		void DoSize ()
		{
			int gap = (Width - (close.Width + toggleSize.Width + minimise.Width)) / 2;

			minimise.Location = new Point (0, (Height - minimise.Height) / 2);
			toggleSize.Location = new Point (minimise.Right + gap, (Height - toggleSize.Height) / 2);
			close.Location = new Point (toggleSize.Right + gap, (Height - close.Height) / 2);
		}

		public void SetAutoSize ()
		{
			Size = new Size (close.Width + toggleSize.Width + minimise.Width, Math.Max(close.Height, Math.Max(toggleSize.Height, minimise.Height)));
		}

		public event EventHandler MinimisePressed;
		public event EventHandler ToggleSizePressed;
		public event EventHandler ClosePressed;

		void minimise_ButtonPressed (object sender, ImageButtonEventArgs args)
		{
			if (MinimisePressed != null)
			{
				MinimisePressed(this, EventArgs.Empty);
			}
		}

		void toggleSize_ButtonPressed (object sender, ImageButtonEventArgs args)
		{
			if (ToggleSizePressed != null)
			{
				ToggleSizePressed(this, EventArgs.Empty);
			}
		}

		void close_ButtonPressed (object sender, ImageButtonEventArgs args)
		{
			if (ClosePressed != null)
			{
				ClosePressed(this, EventArgs.Empty);
			}
		}

		public Control ToggleSizeButton
		{
			get
			{
				return toggleSize;
			}
		}
	}
}