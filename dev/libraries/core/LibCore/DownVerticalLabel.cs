using System.Drawing;
using System.Windows.Forms;

namespace LibCore
{
	public class DownVerticalLabel : VerticalLabel
	{	
		public DownVerticalLabel()
		{
		}

		protected override void OnPaint( PaintEventArgs e )
		{
			base.OnPaint(e);

			if(null != font)
			{
				e.Graphics.RotateTransform(90);
				e.Graphics.DrawString(this.text, font, Brushes.Black, Height/2-sf.Width/2, -Width/2-sf.Height/2);
			}
		}
	}
}
