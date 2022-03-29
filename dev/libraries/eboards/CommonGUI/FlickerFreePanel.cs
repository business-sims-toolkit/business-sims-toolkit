using System.Windows.Forms;

using LibCore;
using CoreUtils;

namespace CommonGUI
{
	public class FlickerFreePanel : VisualPanel
	{
		public FlickerFreePanel ()
			: this (true)
		{
		}

		public FlickerFreePanel (bool flickerFree)
		{
			if (flickerFree)
			{
				SetStyle(ControlStyles.AllPaintingInWmPaint, true);
				SetStyle(ControlStyles.UserPaint, true);
				SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
			}
		}
		
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;

	            if (SkinningDefs.TheInstance.GetBoolData("use_composited_windows", false))
	            {
		            cp.ExStyle |= 0x02000000; // Turn on WS_EX_COMPOSITED
	            }

	            return cp;
            }
        }
	}
}