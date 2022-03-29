using System;
using System.Drawing;
using System.Windows.Forms;

namespace ResizingUi.Interfaces
{
	public interface IDialogOpener
	{
		void OpenDialog (Control dialog, Func<Rectangle> boundsInScreenCoordinatesFunc);

		void CloseDialog ();
	}
}
