using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace LibCore
{
	public static class WindowRedraw
	{
		const int WM_SETREDRAW = 0x000b;

		static Dictionary<Control, int> controlToSuspensions = new Dictionary<Control, int> ();

		public static void Suspend (Control control)
		{
			if (! controlToSuspensions.ContainsKey(control))
			{
				controlToSuspensions.Add(control, 0);
			}

			controlToSuspensions[control]++;

			Message msgSuspendUpdate = Message.Create(control.Handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
			NativeWindow window = NativeWindow.FromHandle(control.Handle);

			window.DefWndProc(ref msgSuspendUpdate);
		}

		public static void Resume (Control control)
		{
			if (controlToSuspensions.ContainsKey(control))
			{
				controlToSuspensions[control]--;
				if (controlToSuspensions[control] <= 0)
				{
					controlToSuspensions.Remove(control);

					Message msgResumeUpdate = Message.Create(control.Handle, WM_SETREDRAW, new IntPtr (1), IntPtr.Zero);
					NativeWindow window = NativeWindow.FromHandle(control.Handle);

					window.DefWndProc(ref msgResumeUpdate);
					control.Invalidate();
				}
			}
		}
	}
}