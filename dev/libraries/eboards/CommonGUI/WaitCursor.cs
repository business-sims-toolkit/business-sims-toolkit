using System;
using System.Windows.Forms;

namespace CommonGUI
{
	public class WaitCursor : IDisposable
	{
		Cursor oldCursor;

		public WaitCursor(Control control)
		{
			oldCursor = Cursor.Current;
			Cursor.Current = Cursors.WaitCursor;
		}

		public void Dispose()
		{
			Cursor.Current = oldCursor;
		}
	}
}