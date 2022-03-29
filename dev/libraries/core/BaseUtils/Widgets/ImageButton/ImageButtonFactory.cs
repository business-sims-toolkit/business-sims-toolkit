using System;
using System.Drawing;

//using PerformancePlusUtils;

namespace BaseUtils
{
	/// <summary>
	/// The types of ImageButton supported by the factory.
	/// </summary>
	public enum ImageButtonType
	{
		ArrowLeft,
		ArrowRight,
		NavUp,
		NavDown,
		NavClose,
		NavMin,
		BigNavLeft,
		BigNavRight,
		BigHome,
		BigPlay,
		BigStop,
		BigRewind,
		BigShield,
		BigBomb,
		BigCheck,
		BigNavUp,
		BigNavDown,
		NavPM_Disk,
		NavPM_Folder,
		NavPM_Flag,
		NavPM_Net,
		NavPM_Rep
	}

	/// <summary>
	/// A Factory for drawing button images.
	/// </summary>
	public class ImageButtonFactory
	{
		static ImageButtonFactory fact;
		ImageSet[] imageSets;

		/// <summary>
		/// Returns the one and only instance of ImageButtonFactory.
		/// </summary>
		/// <returns></returns>
		public static ImageButtonFactory Instance()
		{
			if (fact == null)
				fact = new ImageButtonFactory();
			return fact;
		}

		/// <summary>
		/// Creates the one and only instance of ImageButtonFactory.
		/// </summary>
		ImageButtonFactory()
		{
			imageSets = new ImageSet[22];
			imageSets[0] = new ImageSet("arrow_left_blue");
			imageSets[1] = new ImageSet("arrow_right_blue");
			imageSets[2] = new ImageSet("nav_up_blue");
			imageSets[3] = new ImageSet("nav_down_blue");
			imageSets[4] = new ImageSet("nav_close_red");
			imageSets[5] = new ImageSet("nav_min_gray");
			imageSets[6] = new ImageSet("big_nav_left_green");
			imageSets[7] = new ImageSet("big_nav_right_green");
			imageSets[8] = new ImageSet("big_home");
			imageSets[9] = new ImageSet("big_media_play_green");
			imageSets[10] = new ImageSet("big_media_stop_red");
			imageSets[11] = new ImageSet("big_media_beginning");
			imageSets[12] = new ImageSet("big_shield_red", "big_shield_green");
			imageSets[13] = new ImageSet("big_bomb");
			imageSets[14] = new ImageSet("big_check");
			imageSets[15] = new ImageSet("big_nav_up_green");
			imageSets[16] = new ImageSet("big_nav_down_red");
			imageSets[17] = new ImageSet("nav_pm_disk");
			imageSets[18] = new ImageSet("nav_pm_folder");
			imageSets[19] = new ImageSet("nav_pm_flag");
			imageSets[20] = new ImageSet("nav_pm_net");
			imageSets[21] = new ImageSet("nav_pm_rep");
		}

		/// <summary>
		/// Draws the specified ImageButtonType.
		/// </summary>
		/// <param name="g"></param>
		/// <param name="type"></param>
		/// <param name="pushed"></param>
		/// <param name="enabled"></param>
		/// <param name="toggled"></param>
		public void DrawButton(Graphics g, ImageButtonType type, bool pushed, bool enabled, bool toggled)
		{
			int idx = Convert.ToInt32(type);
			if (idx >= 0 && idx < imageSets.Length)
			{
				if (enabled == false)
					g.DrawImage(imageSets[idx].DisabledImage, 2, 2, imageSets[idx].Width, imageSets[idx].Height);
				else if (pushed)
				{
					if (!toggled)
						g.DrawImage(imageSets[idx].DownImage, 2, 2, imageSets[idx].Width, imageSets[idx].Height);
					else
						g.DrawImage(imageSets[idx].DownImage2, 2, 2, imageSets[idx].Width, imageSets[idx].Height);
				}
				else
				{
					if (!toggled)
						g.DrawImage(imageSets[idx].UpImage, 2, 2, imageSets[idx].Width, imageSets[idx].Height);
					else
						g.DrawImage(imageSets[idx].UpImage2, 2, 2, imageSets[idx].Width, imageSets[idx].Height);
				}
			}
		}

		/// <summary>
		/// Determines whether the specified ImageButtonType is a
		/// toggle button.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public bool IsToggleButton(ImageButtonType type)
		{
			bool result = false;

			int idx = Convert.ToInt32(type);
			if (idx >= 0 && idx < imageSets.Length)
				result = imageSets[idx].Toggle;

			return result;
		}

		/// <summary>
		/// Represents all of the images required
		/// to depict a button's various states.
		/// </summary>
		internal class ImageSet
		{
			int width;
			int height;
			Bitmap upImage1;
			Bitmap downImage1;
			Bitmap upImage2;
			Bitmap downImage2;
			Bitmap disabledImage;
			static string basePath = "BaseUtils.Widgets.ImageButton.Resources";
			bool toggle;

			/// <summary>
			/// Creates an instance of ImageSet.
			/// </summary>
			/// <param name="res1"></param>
			public ImageSet(string res1)
			{
				string up1 = String.Format("{0}.{1}{2}", basePath, res1, "_s.png");
				string down1 = String.Format("{0}.{1}{2}", basePath, res1, ".png");
				upImage1 = new Bitmap(UtilFunctions.GetEmbeddedResource(up1));
				downImage1 = new Bitmap(UtilFunctions.GetEmbeddedResource(down1));
				disabledImage = GrayScale(upImage1);
				width = upImage1.Width;
				height = upImage1.Height;
			}

			/// <summary>
			/// Creates an instance of ImageSet for a three-state button.
			/// </summary>
			/// <param name="res1"></param>
			/// <param name="res2"></param>
			public ImageSet(string res1, string res2)
			{
				string up1 = String.Format("{0}.{1}{2}", basePath, res1, "_s.png");
				string down1 = String.Format("{0}.{1}{2}", basePath, res1, ".png");
				string up2 = String.Format("{0}.{1}{2}", basePath, res2, "_s.png");
				string down2 = String.Format("{0}.{1}{2}", basePath, res2, ".png");
				upImage1 = new Bitmap(UtilFunctions.GetEmbeddedResource(up1));
				downImage1 = new Bitmap(UtilFunctions.GetEmbeddedResource(down1));
				upImage2 = new Bitmap(UtilFunctions.GetEmbeddedResource(up2));
				downImage2 = new Bitmap(UtilFunctions.GetEmbeddedResource(down2));
				disabledImage = GrayScale(upImage1);
				width = upImage1.Width;
				height = upImage1.Height;
				toggle = true;
			}

			/// <summary>
			/// Converts the specified button to a
			/// gray scale image.
			/// </summary>
			/// <param name="source"></param>
			/// <returns></returns>
			public Bitmap GrayScale(Bitmap source)
			{
				Bitmap bmp = new Bitmap(source.Width, source.Height);

				for(int y = 0; y < bmp.Height; y++)
				{
					for(int x = 0; x < bmp.Width; x++)
					{
						Color c = source.GetPixel(x, y);
						int luma = (int)(c.R * 0.3 + c.G * 0.59 + c.B * 0.11);
						bmp.SetPixel(x, y, Color.FromArgb(c.A, luma, luma, luma));
					}
				}
				return bmp;
			}

			public Bitmap UpImage
			{
				get { return upImage1; }
			}

			public Bitmap DownImage
			{
				get { return downImage1; }
			}

			public Bitmap DisabledImage
			{
				get { return disabledImage; }
			}

			public Bitmap UpImage2
			{
				get { return upImage2; }
			}

			public Bitmap DownImage2
			{
				get { return downImage2; }
			}

			public int Width
			{
				get { return width; }
			}
			
			public int Height
			{
				get { return height; }
			}

			public bool Toggle
			{
				get { return toggle; }
			}
		}
	}
}
