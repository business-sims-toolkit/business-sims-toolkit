using LibCore;

namespace Charts
{
	/// <summary>
	/// Summary description for Chart.
	/// </summary>
	public abstract class Chart : VisualPanel
	{
		public Chart()
		{
			//
			// TODO: Add constructor logic here
			//
		}

		public abstract void LoadData(string xmldata);
	}
}