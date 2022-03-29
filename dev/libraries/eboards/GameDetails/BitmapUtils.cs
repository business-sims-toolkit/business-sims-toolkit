using System.Drawing;
using System.Windows.Forms;
using System.IO;

namespace GameDetails
{
	/// <summary>
	/// Summary description for BitmapUtils.
	/// </summary>
	public class BitmapUtils
	{
		public static Bitmap ConvertToApectRatio(Bitmap bmp, double aspectRatio)
		{
			double origAspectRatio = ((double)bmp.Width)/((double)bmp.Height);
			if(aspectRatio == origAspectRatio)
			{
				double h = bmp.Height;
				double w = bmp.Width;
				Bitmap bmp2 = new Bitmap((int)w,(int)h);
				Graphics g = Graphics.FromImage(bmp2);
				g.DrawImage(bmp,0,0,(int)w,(int)h);
				g.Dispose();

				return bmp2;
			}

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

		public static Bitmap LoadBitmapGivenFilename (Control c, string generatedCopyFilename, GameManagement.NetworkProgressionGameFile gameFile, string inputFilename)
		{
			try
			{
				FileInfo finfo = new FileInfo(inputFilename);
				Bitmap bmp = new Bitmap(inputFilename);

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
				string fname = gameFile.Dir + "\\global\\" + generatedCopyFilename;
				bmp.Save(fname, System.Drawing.Imaging.ImageFormat.Png);
				return bmp;
			}
			catch
			{
				MessageBox.Show(c.TopLevelControl, "Failed to load image.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}

			return null;
		}

		public static Bitmap LoadBitmap(Control c, string fileName, GameManagement.NetworkProgressionGameFile gameFile)
		{
			System.Windows.Forms.OpenFileDialog openFileDialog1 = new OpenFileDialog();

			// : Fix for 3531 (all load image dialogs should have a filter).
			openFileDialog1.Filter = "Image files (*.BMP;*.JPG;*.GIF;*.PNG;*.TIF)|*.BMP;*.JPG;*.GIF;*.PNG;*.TIF";

			DialogResult res = openFileDialog1.ShowDialog(c.TopLevelControl);
			if(res != DialogResult.OK) return null;

			return LoadBitmapGivenFilename(c, fileName, gameFile, openFileDialog1.FileName);
		}
	}
}