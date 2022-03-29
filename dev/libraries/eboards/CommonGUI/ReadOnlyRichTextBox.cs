using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CommonGUI
{
	public class ReadOnlyRichTextBox : RichTextBox
	{
		[DllImport("user32.dll")]
		static extern int HideCaret (IntPtr hwnd);

		public ReadOnlyRichTextBox ()
		{
			base.ReadOnly = true;
			base.TabStop = false;
			HideCaret(Handle);
		}

		protected override void OnKeyPress (KeyPressEventArgs e)
		{
			base.OnKeyPress(e);
			HideCaret(Handle);
		}

		protected override void OnSelectionChanged (EventArgs e)
		{
			base.OnSelectionChanged(e);
			HideCaret(Handle);
		}

		protected override void OnMouseDown (MouseEventArgs e)
		{
			base.OnMouseDown(e);
			HideCaret(Handle);
		}

		protected override void OnMouseUp (MouseEventArgs mevent)
		{
			base.OnMouseUp(mevent);
			HideCaret(Handle);
		}

		protected override void OnGotFocus (EventArgs e)
		{
			HideCaret(Handle);
		}

		protected override void OnEnter (EventArgs e)
		{
			HideCaret(Handle);
		}

		[DefaultValue(true)]
		public new bool ReadOnly
		{
			get
			{
				return true;
			}
			set
			{
			}
		}

		[DefaultValue(false)]
		public new bool TabStop
		{
			get
			{
				return false;
			}
			set
			{
			}
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);
			HideCaret(Handle);
		}
	}
}