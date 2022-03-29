using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace NeowUi
{
	public static class WindowDraggingExtensions
	{
		const int WM_NCLBUTTONDOWN = 0xa1;
		const int HTCAPTION = 0x2;

		[DllImport("User32.dll")]
		static extern bool ReleaseCapture ();

		[DllImport("User32.dll")]
		static extern int SendMessage (IntPtr hWnd, int Msg, int wParam, int lParam);

		public static void DragMove (this Form form)
		{
			ReleaseCapture();
			SendMessage(form.Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
		}
	}
}