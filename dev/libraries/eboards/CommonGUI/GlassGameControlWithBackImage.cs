using System;
using System.Windows.Forms;
using System.Drawing;
using LibCore;

namespace CommonGUI
{
	/// <summary>
	/// Summary description for GlassGameControlWithBackImage
	/// </summary>
	public class GlassGameControlWithBackImage : GlassGameControl
	{
		Bitmap background = null;

		public GlassGameControlWithBackImage()
			: base()
		{
		}

		public void setBackImage(string back_image_file_name)
		{
			Bitmap BackImage = (Bitmap)Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\" + back_image_file_name);
			if (BackImage != null)
			{
				background = BackImage;
			}
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			if (background != null)
			{
				Graphics g = e.Graphics;

				Rectangle destRect = new Rectangle(0, 0, Width, Height);

				g.DrawImage(background, destRect);
			}
		}
	}
}