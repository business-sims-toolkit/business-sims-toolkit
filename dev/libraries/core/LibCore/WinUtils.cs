using System.Windows.Forms;

namespace LibCore
{
	/// <summary>
	/// Summary description for WinUtils.
	/// </summary>
	public class WinUtils
	{
		public static void Show(Control c)
		{
			if(c != null)
			{
				c.Show();
			}
		}
		public static void Hide(Control c)
		{
			if(c != null)
			{
				if(c.Visible) c.Hide();
			}
		}

		public static void Dispose(Control c)
		{
			if(c != null)
			{
				if(c.Visible) c.Hide();
				c.Dispose();
				//c = null;
			}
		}
	}

	public sealed class Win32Error
	{
		public const int ERROR_NO_ASSOCIATION = 1155;
	}
}
