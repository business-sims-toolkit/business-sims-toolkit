using System.Drawing;

namespace BaseUtils
{
	/// <summary>
	/// Summary description for BaseResManager.
	/// </summary>
	public class BaseResManager
	{
		public Bitmap BackImage = null;

		public BaseResManager()
		{
			System.Diagnostics.Debug.WriteLine("Base Res Manager Constructor");
			Init();
		}

		public virtual void Init()
		{
			System.Diagnostics.Debug.WriteLine("Base Res Manager Constructor");
			System.Diagnostics.Debug.WriteLine("Base Init");
		}
		
		public virtual System.Drawing.Bitmap getBackgroundImage()
		{
			return null;
		}

	}
}
