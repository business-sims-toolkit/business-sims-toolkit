using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using LibCore;
using CoreUtils;

namespace GameDetails
{
	/// <summary>
	/// Summary description for TeamPhotoDetails.
	/// </summary>
	public class TeamPhotoDetails : GameDetailsSection
	{
		protected PictureBox picture;
		protected GameManagement.NetworkProgressionGameFile _gameFile;
		protected Font MyDefaultSkinFontBold9;

		public TeamPhotoDetails(GameManagement.NetworkProgressionGameFile gameFile)
		{
			_gameFile = gameFile;

			string fontname =  SkinningDefs.TheInstance.GetData("fontname");
			MyDefaultSkinFontBold9 = ConstantSizeFont.NewFont(fontname,9,FontStyle.Bold);

			this.Title = "Team Photo";
			picture = new PictureBox();
			picture.Location = new Point(5,5);
			picture.Size = new Size(280,200);
			picture.SizeMode = PictureBoxSizeMode.StretchImage;
			panel.Controls.Add(picture);

			picture.BorderStyle = BorderStyle.FixedSingle;

			Button load = SkinningDefs.TheInstance.CreateWindowsButton(FontStyle.Bold);
            load.Name = "load Button";
			load.Location = new Point(290,5);
			load.Size = new Size(80,25);
		    load.Text = "Load";
			load.Click += load_Click;
			panel.Controls.Add(load);

			Button del = SkinningDefs.TheInstance.CreateWindowsButton(FontStyle.Bold);
            del.Name = "del Button";
			del.Location = new Point(290,35);
			del.Size = new Size(80,25);
		    del.Text = "Remove";
			del.Click += del_Click;
			panel.Controls.Add(del);

			string fname = _gameFile.Dir + "\\global\\team_photo.png";
			if(File.Exists(fname))
			{
				try
				{
					Bitmap bmp = new Bitmap(fname);
					if(bmp != null)
					{
						Bitmap bmp2 = BitmapUtils.ConvertToApectRatio(bmp, 1.4);
						bmp.Dispose();
						picture.Image = (Bitmap)bmp2.Clone();
					}
				}
				catch
				{
				}
			}
		}

		protected void load_Click(object sender, EventArgs e)
		{
			Bitmap bmp = BitmapUtils.LoadBitmap(this, "team_photo.png", _gameFile);

			if(null != bmp)
			{
				Bitmap bmp2 = BitmapUtils.ConvertToApectRatio(bmp,1.4);
				
				bmp.Dispose();
				bmp = null;
				System.GC.Collect();

				picture.Image = (Bitmap) bmp2.Clone();
			}
		}

		protected void del_Click(object sender, EventArgs e)
		{
			picture.Image = null;
			//
			string fname = _gameFile.Dir + "\\global\\team_photo.png";
			if(File.Exists(fname))
			{
				File.Delete(fname);
			}
			//
		}
	}
}
