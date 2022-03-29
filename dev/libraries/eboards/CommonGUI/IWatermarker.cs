using System.Drawing;
using System.Windows.Forms;

namespace CommonGUI
{
	public interface IWatermarker
	{
		void Draw (Control control, Graphics graphics);
	}
}