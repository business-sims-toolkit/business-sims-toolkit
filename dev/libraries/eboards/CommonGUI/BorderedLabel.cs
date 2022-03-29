using System.Windows.Forms;
using System.Drawing;

namespace CommonGUI
{
	public class BorderedLabel : Label
	{
		public Color BorderColor = Color.Transparent;

		public bool LeftBorder = true;
		public bool RightBorder = true;
		public bool TopBorder = true;
		public bool BottomBorder = true;

		public BorderedLabel ()
		{
		}

		protected override void OnPaint (PaintEventArgs e)
		{
			base.OnPaint(e);

			Pen BorderPen = new Pen (BorderColor, 1);

			if (LeftBorder)
			{
				e.Graphics.DrawLine(BorderPen, 0, 0, 0, Height - 1);
			}
			if (RightBorder)
			{
				e.Graphics.DrawLine(BorderPen, Width - 1, 0, Width - 1, Height - 1);
			}
			if (TopBorder)
			{
				e.Graphics.DrawLine(BorderPen, 0, 0, Width - 1, 0);
			}
			if (BottomBorder)
			{
				e.Graphics.DrawLine(BorderPen, 0, Height - 1, Width - 1, Height - 1);
			}

			BorderPen.Dispose();
		}
	}
}