using System.Drawing;
using LibCore;

namespace CommonGUI
{
	/// <summary>
	/// Summary description for MonitorItem.
	/// </summary>
	public class MonitorItem : FlickerFreePanel
	{
		/// <summary>
		/// The Icon represenging this item
		/// </summary>
		protected Image icon;
		/// <summary>
		/// Long Description
		/// </summary>
		protected string desc = "";
		/// <summary>
		/// Short Description
		/// </summary>
		protected string shortdesc = "";
		/// <summary>
		/// Name of the icon
		/// </summary>
		protected string iconname = "";

		/// <summary>
		/// Padding used to draw
		/// </summary>
		protected int padTop = 10;
		/// <summary>
		/// Padding used to draw
		/// </summary>
		protected int padBottom = 2;
		/// <summary>
		/// Padding used to draw
		/// </summary>
		protected int padLeft = 2;
		/// <summary>
		/// Padding used to draw
		/// </summary>
		protected int padRight = 2;

		public MonitorItem()
		{
		}

		new protected virtual void Dispose()
		{
			base.Dispose();
		}

		/// <summary>
		/// Get the icon used to represenent the item based on the description
		/// </summary>
		protected virtual void GetIcon()
		{
			string loc = AppInfo.TheInstance.Location + "\\";

			//replace character to reduce the string to the essentials 
			string stripped_desc = desc.Replace("\r\n","");
			stripped_desc = stripped_desc.Replace("2","");
			stripped_desc = stripped_desc.Replace("1","");
			stripped_desc = stripped_desc.Replace("0","");
			stripped_desc = stripped_desc.Replace(".","");
			stripped_desc = stripped_desc.Replace(" ","");

			//need to do this a better way perhaps with id numbers 
			//as this many string compares must be slow

			icon = Repository.TheInstance.GetImage(loc + @"/images/icons/"+iconname+".png");
			//icon.Tag = null; // Throws if it doesn't exist!
			//System.Diagnostics.Debug.WriteLine(" setting icon to " + iconname + ".png");
		}

		/// <summary>
		/// This is used in McKinley 
		/// as we have both black line icons and white line icons 
		/// This copes with ensuring visibility against different backgrounds 
		/// </summary>
		protected Image GetInvertedIcon()
		{
			string loc = AppInfo.TheInstance.Location + "\\";

			//replace character to reduce the string to the essentials 
			string stripped_desc = desc.Replace("\r\n","");
			stripped_desc = stripped_desc.Replace("2","");
			stripped_desc = stripped_desc.Replace("1","");
			stripped_desc = stripped_desc.Replace("0","");
			stripped_desc = stripped_desc.Replace(".","");
			stripped_desc = stripped_desc.Replace(" ","");

			//need to do this a better way perhaps with id numbers 
			//as this many string compares must be slow

			return Repository.TheInstance.GetImage(loc + @"/images/icons/"+iconname+"_invert.png");
			//System.Diagnostics.Debug.WriteLine(" setting icon to " + iconname + ".png");
		}


		/// <summary>
		/// This is used in PoleStar2 as we have different icon for game and transition
		/// </summary>
		protected Image GetAltIcon()
		{
			string loc = AppInfo.TheInstance.Location + "\\";

			//replace character to reduce the string to the essentials 
			string stripped_desc = desc.Replace("\r\n","");
			stripped_desc = stripped_desc.Replace("2","");
			stripped_desc = stripped_desc.Replace("1","");
			stripped_desc = stripped_desc.Replace("0","");
			stripped_desc = stripped_desc.Replace(".","");
			stripped_desc = stripped_desc.Replace(" ","");

			//need to do this a better way perhaps with id numbers 
			//as this many string compares must be slow

			return Repository.TheInstance.GetImage(loc + @"/images/icons/"+iconname+"_alt.png");
			//System.Diagnostics.Debug.WriteLine(" setting icon to " + iconname + ".png");
		}

	}
}
