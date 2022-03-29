using System.Windows.Forms;

namespace LibCore
{
	/// <summary>
	/// Summary description for IPrint.
	/// </summary>
	public interface IPrint
	{
		void DrawToHDC(PaintEventArgs e);
	}
}
