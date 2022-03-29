using System.Drawing;
using System.Windows.Forms;
using System.IO;

namespace CommonGUI
{
	/// <summary>
	/// Summary description for BitmapUtils.
	/// </summary>
	public class BitmapUtils
	{
		public static Bitmap ConvertToApectRatio(Bitmap bmp, double aspectRatio)
		{
			if (bmp == null)
			{
				return null;
			}

			double origAspectRatio = ((double)bmp.Width)/((double)bmp.Height);
			if(aspectRatio == origAspectRatio) return bmp;

			if(aspectRatio > origAspectRatio)
			{
				// Limit by height...
				double h = bmp.Height;
				double w = h*aspectRatio;
				Bitmap bmp2 = new Bitmap((int)w,(int)h);
				Graphics g = Graphics.FromImage(bmp2);
				g.DrawImage(bmp,(int)((w-bmp.Width)/2.0),0,(int)bmp.Width,(int)h);
				g.Dispose();

				return bmp2;
			}
			else
			{
				// Limit by width...
				double w = bmp.Width;
				double h = w/aspectRatio;
				Bitmap bmp2 = new Bitmap((int)w,(int)h);
				Graphics g = Graphics.FromImage(bmp2);
				g.DrawImage(bmp,0,(int)((h-bmp.Height)/2.0),(int)w,(int)bmp.Height);
				g.Dispose();

				return bmp2;
			}
		}

		public static Bitmap LoadBitmap(Control c, string fileName, GameManagement.NetworkProgressionGameFile gameFile)
		{
			OpenFileDialog openFileDialog1 = new OpenFileDialog();
			DialogResult res = openFileDialog1.ShowDialog(c.TopLevelControl);
			if(res != DialogResult.OK) return null;

			try
			{
				FileInfo finfo = new FileInfo(openFileDialog1.FileName);

				Bitmap bmp = new Bitmap(openFileDialog1.FileName);
				double aspectRatio = ((double)bmp.Width)/((double)bmp.Height);
				//
				if(bmp.Width > 500)
				{
					double w = 500;
					double h = w/aspectRatio;
					Bitmap bmp2 = new Bitmap((int)w,(int)h);
					Graphics g = Graphics.FromImage(bmp2);
					g.DrawImage(bmp,0,0,(int)w,(int)h);
					g.Dispose();
					//
					bmp = bmp2;
				}
				//
				if(bmp.Height > 500)
				{
					double h = 500;
					double w = h*aspectRatio;
					Bitmap bmp2 = new Bitmap((int)w,(int)h);
					Graphics g = Graphics.FromImage(bmp2);
					g.DrawImage(bmp,0,0,(int)w,(int)h);
					g.Dispose();
					//
					bmp = bmp2;
				}
				//
				string fname = gameFile.Dir + "\\global\\" + fileName;
				bmp.Save(fname, System.Drawing.Imaging.ImageFormat.Png);
				return bmp;
			}
			catch
			{
				MessageBox.Show(c.TopLevelControl, "Failed to load image.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}

			return null;
		}
	}
}
