using System.Windows.Forms;

namespace LibCore
{
	public class UpVerticalLabel : VerticalLabel
	{	
		public UpVerticalLabel()
		{
		}

		/// <summary>
		/// Override Paint ...
		/// </summary>
		/// <param name="e"></param>
		protected override void OnPaint( PaintEventArgs e )
		{
			base.OnPaint(e);

			if(null != font)
			{
				e.Graphics.RotateTransform(-90);
				e.Graphics.DrawString(this.text, font, drawingBrush, -Height/2-sf.Width/2, Width/2-sf.Height/2);
			}
		}
	}
}
