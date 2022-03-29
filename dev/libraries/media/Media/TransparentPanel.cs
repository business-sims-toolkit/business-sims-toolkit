using System.Windows.Forms;
using System.Drawing;

namespace Media
{
	public class TransparentPanel : Control
	{
		public TransparentPanel ()
		{
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			SetStyle(ControlStyles.Opaque, true);
			SetStyle(ControlStyles.ResizeRedraw, true);

			BackColor = Color.Transparent;
		}

		protected override void OnPaintBackground (PaintEventArgs pevent)
		{
		}

		protected override CreateParams CreateParams
		{
			get
			{
				const int WS_EX_TRANSPARENT = 0x20;
				CreateParams createParams = base.CreateParams;
				createParams.ExStyle |= WS_EX_TRANSPARENT;
				return createParams;
			}
		}
	}
}