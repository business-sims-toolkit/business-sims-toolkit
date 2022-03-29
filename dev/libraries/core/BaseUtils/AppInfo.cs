using System.Reflection;

namespace BaseUtils
{
	/// <summary>
	/// Summary description for AppInfo.
	/// </summary>
	public sealed class AppInfo
	{
		string location;
		string name = System.Windows.Forms.Application.ProductName;
		string pinstr = string.Empty;
		string fullsuppliedpinstr = string.Empty;

		public static readonly AppInfo TheInstance = new AppInfo();

		AppInfo()
		{
			Assembly a = Assembly.GetExecutingAssembly();
			location = a.Location.Substring(0,a.Location.LastIndexOf("\\"));
		}

		public string Location
		{
			get { return location; }
		}

		public string Name
		{
			get { return name; }
			set { name = value; }
		}

		public string FullSuppliedPinStr
		{
			get { return fullsuppliedpinstr; }
			set { fullsuppliedpinstr = value; }
		}

		public string PinStr
		{
			get { return pinstr; }
			set { pinstr = value; }
		}

	}
}
