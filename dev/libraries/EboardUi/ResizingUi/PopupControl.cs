using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ResizingUi
{
	public class PopupControl : Form
	{
		public PopupControl ()
		{
			ShowInTaskbar = false;
			FormBorderStyle = FormBorderStyle.None;
			StartPosition = FormStartPosition.Manual;
		}
		
		public void Show (Control parentContainer, Control controlToDisplay, Rectangle screenBounds)
		{
			if (managedControl != null)
			{
				Controls.Remove(managedControl);
			}

			managedControl = controlToDisplay;

			if (managedControl != null)
			{
				Controls.Add(managedControl);
			}


			Location = screenBounds.Location;
			Size = screenBounds.Size;

			Show(parentContainer);
		}

		protected override void OnLostFocus(EventArgs e)
		{
			base.OnLostFocus(e);
			Close();
		}

		protected override void OnDeactivate (EventArgs e)
		{
			base.OnDeactivate(e);
			Close();
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			if (managedControl != null)
			{
				managedControl.Bounds = new Rectangle(0, 0, Width, Height);
			}
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing)
			{
				managedControl?.Dispose();
			}

			base.Dispose(disposing);
		}

		Control managedControl;


		[DllImport("user32.dll", EntryPoint = "SetWindowPos", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
		static extern int SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

		[DllImport("user32.dll", EntryPoint = "SetWindowRgn", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
		static extern IntPtr SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);

		const int SWP_DRAWFRAME = 0x20;
		const int SWP_NOMOVE = 0x2;
		const int SWP_NOSIZE = 0x1;
		const int SWP_NOZORDER = 0x4;
		const int HWND_TOPMOST = -1;
		const int HWND_NOTOPMOST = -2;
		const int TOPMOST_FLAGS = SWP_NOMOVE | SWP_NOSIZE;

	}
}
