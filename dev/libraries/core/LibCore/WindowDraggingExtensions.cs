using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace LibCore
{
    public static class WindowDraggingExtensions
    {
        const int WM_NCLBUTTONDOWN = 0xa1;
        const int HTCAPTION = 0x2;

        [DllImport("User32.dll")]
        static extern bool ReleaseCapture ();

        [DllImport("User32.dll")]
        static extern int SendMessage (IntPtr hWnd, int Msg, int wParam, int lParam);

	    static bool dragEnabled;

	    public static void EnableDragging ()
	    {
		    dragEnabled = true;
	    }

        public static void DragMove (this Form form)
        {
	        if (dragEnabled)
	        {
		        ReleaseCapture();
		        SendMessage(form.Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
	        }
        }
    }
}