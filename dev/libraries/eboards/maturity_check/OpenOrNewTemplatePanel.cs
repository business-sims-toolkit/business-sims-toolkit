using LibCore;

namespace maturity_check
{
	public abstract class OpenOrNewTemplatePanel : BasePanel, ICustomerInfo
	{
		public abstract void Cancelled ();

		public abstract void Accepted ();

		public abstract void SaveCustomerDetails ();

		public abstract void SaveToXML ();
	}
}