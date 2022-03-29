using System.Drawing;
using System.Windows.Forms;

using LibCore;

namespace Charts
{
	public class RightYAxis : YAxis
	{
		public RightYAxis()
		{
			axisTitle = new DownVerticalLabel();
			this.SuspendLayout();
			Controls.Add(axisTitle);
			this.ResumeLayout(false);
			align = ContentAlignment.MiddleLeft;
		}
		
		protected override void OnPaint( PaintEventArgs e )
		{
			base.OnPaint(e);

			SetStroke(1, Color.Black);
			MoveTo(0,0);
			LineTo(e, 0,Height);

			if (stripeColours == null)
			{
				if (1 <= _steps)
				{
					for (double i = 0; i < _steps; ++i)
					{
						double off = i * Height / _steps;
						MoveTo(0, (int) off);
						LineTo(e, 5, (int) off);
					}
				}
			}
		}
	}
}
