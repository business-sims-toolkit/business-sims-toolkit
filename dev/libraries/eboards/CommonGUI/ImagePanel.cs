using System.Drawing;
using LibCore;

namespace CommonGUI
{
	public class ImagePanel : BasePanel
	{
		public ImagePanel (Color colour, int width, int height)
		{
			Size = new Size (width, height);
			BackColor = colour;
		}

        public ImagePanel(string ImageName)
        {
            Bitmap BackImage = Repository.TheInstance.GetImage(ImageName);
            if (BackImage != null)
            {
                BackgroundImage = BackImage;
                Size = new Size(BackImage.Width, BackImage.Height);
            }
        }

		public ImagePanel(string ImageName, int width, int height)
		{
			Size = new Size(width,height);
			Bitmap BackImage = (Bitmap) Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\"+ImageName);
			if (BackImage != null)
			{
				BackgroundImage = BackImage;
			}
		}

		public ImagePanel(Bitmap Image, int width, int height)
		{
			Size = new Size(width, height);
			Bitmap BackImage = Image;
			if (BackImage != null)
			{
				BackgroundImage = BackImage;
			}
		}
	}
}
