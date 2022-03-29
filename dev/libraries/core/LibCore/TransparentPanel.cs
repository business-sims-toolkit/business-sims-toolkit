using System.Windows.Forms;
using System.Drawing;

namespace LibCore
{
	public class TransparentPanel : Panel
	{
		protected override CreateParams CreateParams
		{
			get
			{
				CreateParams createParams = base.CreateParams;
				createParams.ExStyle |= 0x20; // WS_EX_TRANSPARENT

				return createParams;
			}
		}

		protected override void OnPaintBackground (PaintEventArgs e)
		{
			// Don't call the base method, which overwrites the background.
		}

		/// <summary>
		/// If we call Invalidate(), our parent's background gets overwritten.  Instead we should use InvalidateEx(),
		/// which preserves the background.
		/// </summary>
		public void InvalidateEx ()
		{
			if (Parent != null)
			{
				Rectangle rectangle = new Rectangle (Location, Size);
				Parent.Invalidate(rectangle, true);
			}
		}
	}
}