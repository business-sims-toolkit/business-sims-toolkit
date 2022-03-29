using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace LibCore
{
	/// <summary>
	/// Summary description for GDI.
	/// </summary>
	public class GDI
	{
		[DllImport("gdi32")] public static extern int CreateRoundRectRgn(int X1, int Y1, int X2, int Y2, int X3, int Y3);

		[DllImport("user32")] public static extern int SetWindowRgn(IntPtr hwnd, int hRgn, int bRedraw);

		public static void MakeControlRounded(Control c, int roundness)
		{
			int orgn = CreateRoundRectRgn(0,0,c.Width,c.Height,roundness,roundness);
			SetWindowRgn(c.Handle, orgn, 1);
		}
	}
}
