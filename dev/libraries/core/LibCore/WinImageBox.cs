using System.Drawing;
using System.Windows.Forms;
using System.IO;

namespace LibCore
{
    /// <summary>
    /// Summary description for WinImageBox.
    /// </summary>
    public class ImageBox : PictureBox
    {
        public ImageBox()
        {
            this.BackColor = Color.Transparent;
            this.SizeMode = PictureBoxSizeMode.StretchImage;
        }

        public void PopBack() { }
        public void PopFront() { }

        string imageLocation;

        public new void Load(string url)
        {
            ImageLocation = url;
        }

        public SizeF MeasureString(Font f, string text)
        {
            System.Drawing.Graphics g = this.CreateGraphics();
            SizeF sf = g.MeasureString(text, f);
            g.Dispose();
            return sf;
        }

        public new string ImageLocation
        {
            set
            {
                try
                {
                    Image = Repository.TheInstance.GetImage(value);
                    imageLocation = value;
                }
                catch { }
            }

            get
            {
                return imageLocation;
            }
        }

        public void SetAutoSize()
        {
            Size = Image.Size;
        }

        public Image GetIsolatedImage(string PreferedImageFileName, string BackupImageFileImage)
        {
            Bitmap tmp_img = null;
            Bitmap hack = null;

            if (File.Exists(PreferedImageFileName))
            {
                tmp_img = new Bitmap(PreferedImageFileName);
                tmp_img = (Bitmap)ConvertToApectRatio((Bitmap)tmp_img, 2).Clone();
            }
            else
            {
                if (File.Exists(BackupImageFileImage))
                {
                    tmp_img = new Bitmap(BackupImageFileImage);
                }
            }
            if (tmp_img != null)
            {
                hack = new Bitmap(tmp_img.Width, tmp_img.Height);
                Graphics g = Graphics.FromImage(hack);
                g.DrawImage(tmp_img, 0, 0, (int)hack.Width, (int)hack.Height);
                g.Dispose();
                tmp_img.Dispose();
                tmp_img = null;
                System.GC.Collect();
            }
            return hack;
        }

        public static Bitmap ConvertToApectRatio(Bitmap bmp, double aspectRatio)
        {
            if (bmp == null)
            {
                return null;
            }

            double origAspectRatio = ((double)bmp.Width) / ((double)bmp.Height);
            if (aspectRatio == origAspectRatio) return bmp;

            if (aspectRatio > origAspectRatio)
            {
                // Limit by height...
                double h = bmp.Height;
                double w = h * aspectRatio;
                Bitmap bmp2 = new Bitmap((int)w, (int)h);
                Graphics g = Graphics.FromImage(bmp2);
                g.DrawImage(bmp, (int)((w - bmp.Width) / 2.0), 0, (int)bmp.Width, (int)h);
                g.Dispose();

                return bmp2;
            }
            else
            {
                // Limit by width...
                double w = bmp.Width;
                double h = w / aspectRatio;
                Bitmap bmp2 = new Bitmap((int)w, (int)h);
                Graphics g = Graphics.FromImage(bmp2);
                g.DrawImage(bmp, 0, (int)((h - bmp.Height) / 2.0), (int)w, (int)bmp.Height);
                g.Dispose();

                return bmp2;
            }
        }
    }
}
