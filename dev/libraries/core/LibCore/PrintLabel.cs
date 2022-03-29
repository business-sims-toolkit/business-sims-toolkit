using System.Windows.Forms;

namespace LibCore
{
	/// <summary>
	/// Summary description for PrintLabel.
	/// </summary>
	public class PrintLabel : DoubleBufferableLabel, IPrint
	{
		public PrintLabel()
		{
		}

		#region IPrint Members

		public void DrawToHDC(PaintEventArgs e)
		{
			OnPaint(e);
		}

		#endregion
	}
}
