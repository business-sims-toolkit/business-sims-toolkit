using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CommonGUI
{
	public class ReadOnlyTextBoxNoCursor : TextBox
	{
		[DllImport("user32.dll")]
		static extern bool HideCaret (IntPtr hWnd);
		public ReadOnlyTextBoxNoCursor ()
		{
			ReadOnly = true;
			Cursor = Cursors.Arrow;
		}

		protected override void OnGotFocus (EventArgs e)
		{
			base.OnGotFocus(e);

			HideCaret(Handle);
		}
	}
}