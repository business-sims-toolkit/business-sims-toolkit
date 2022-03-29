using LibCore;

namespace maturity_check
{
	public class MaturityCheckPanel : BasePanel
	{
		protected OpenOrNewReportPanel first_panel;

		public MaturityCheckPanel()
		{
			this.SuspendLayout();
			first_panel = new OpenOrNewReportPanel();
			this.ResumeLayout(false);
		}
	}
}
