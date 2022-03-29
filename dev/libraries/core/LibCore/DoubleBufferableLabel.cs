using System.Windows.Forms;

namespace LibCore
{
	public class DoubleBufferableLabel : Label, IDoubleBufferable
	{
		public DoubleBufferableLabel ()
		{
		}

		public void SetFlickerFree (bool flickerFree)
		{
			this.SetStyle(ControlStyles.AllPaintingInWmPaint, flickerFree);
			this.SetStyle(ControlStyles.UserPaint, flickerFree);
			this.SetStyle(ControlStyles.DoubleBuffer, flickerFree);
		}
	}
}