using System.Drawing;
using System.Windows.Forms;
using ResizingUi;

namespace GameDetails
{
	public class ChoosablePicturePanel : PicturePanel
	{
		public ChoosablePicturePanel ()
		{
		}

		protected override void OnPaint (PaintEventArgs e)
		{
			if (image == null)
			{
				e.Graphics.FillRectangle(Brushes.Gray, 0, 0, Width, Height);
			}
			else
			{
				base.OnPaint(e);
			}
		}
	}
}
