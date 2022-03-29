using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.IO;

namespace UI
{
	public class ImageButton : SkinnedButton
	{
		Image upImage = null;
		Image activeImage = null;
		Image hoverImage = null;
		Image disabledImage = null;
		Image focusImage = null;

		public Image UpImage
		{
			get
			{
				return upImage;
			}
		}

		public void LoadImages (string fileBase)
		{
			string folder = Path.GetDirectoryName(fileBase);
			string stem = Path.GetFileNameWithoutExtension(fileBase);
			string extension = Path.GetExtension(fileBase);

			string baseName = folder + @"\" + stem;

			string upName = baseName + ".png";
			string activeName = baseName + "_active.png";
			string hoverName = baseName + "_hover.png";
			string focusName = baseName + "_focus.png";
			string disabledName = baseName + "_disabled.png";

			LoadImages(upName, activeName, hoverName, focusName, disabledName);
		}

		public void LoadImages (string upName, string activeName, string hoverName, string focusName, string disabledName)
		{
			upImage = Image.FromFile(upName);

			if (File.Exists(activeName))
			{
				activeImage = Image.FromFile(activeName);
			}
			else
			{
				activeImage = upImage;
			}

			if (File.Exists(hoverName))
			{
				hoverImage = Image.FromFile(hoverName);
			}
			else
			{
				hoverImage = upImage;
			}

			if (File.Exists(focusName))
			{
				focusImage = Image.FromFile(focusName);
			}
			else
			{
				focusImage = upImage;
			}

			if (File.Exists(disabledName))
			{
				disabledImage = Image.FromFile(disabledName);
			}
			else
			{
				disabledImage = upImage;
			}
		}

		protected override void OnPaint (PaintEventArgs e)
		{
			base.OnPaint(e);

			Image image;

			if ((State == SkinnedButtonState.Disabled) && (disabledImage != null))
			{
				image = disabledImage;
			}
			else if ((State == SkinnedButtonState.Hover) && (hoverImage != null))
			{
				image = hoverImage;
			}
			else if ((State == SkinnedButtonState.Active) && (activeImage != null))
			{
				image = activeImage;
			}
			else if ((State == SkinnedButtonState.Focussed) && (focusImage != null))
			{
				image = focusImage;
			}
			else
			{
				image = upImage;
			}

			if (image != null)
			{
				e.Graphics.DrawImage(image, ClientRectangle, new Rectangle (0, 0, image.Width, image.Height), GraphicsUnit.Pixel);
			}
		}
	}
}