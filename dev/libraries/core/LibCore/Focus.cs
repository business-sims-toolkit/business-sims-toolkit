using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace LibCore
{
	public static class Focus
	{
		[DllImport("user32.dll")]
		static extern IntPtr GetForegroundWindow ();

		[DllImport("user32.dll")]
		static extern Int32 GetWindowThreadProcessId (IntPtr hWnd, out uint lpdwProcessId);

		public static string GetForegroundProcessName ()
		{
			IntPtr hwnd = GetForegroundWindow();
			if (hwnd == IntPtr.Zero)
			{
				return "Unknown";
			}

			uint pid;
			GetWindowThreadProcessId(hwnd, out pid);

			foreach (System.Diagnostics.Process p in System.Diagnostics.Process.GetProcesses())
			{
				if (p.Id == pid)
				{
					return p.ProcessName;
				}
			}
			
			return "Unknown";
		}

		public static Control GetForegroundControl ()
		{
			return Control.FromHandle(GetForegroundWindow());
		}
	}
}