using System;

using System.Windows.Forms;
using System.Drawing;

using System.IO;

using CoreUtils;
using LibCore;

namespace GameDetails
{
	/// <summary>
	/// Summary description for FacilitatorLogoDetails.
	/// </summary>
	public class CoursewareDelivererLogoDetails : GameDetailsSection
	{
		protected PictureBox picture;
		protected GameManagement.NetworkProgressionGameFile _gameFile;
		protected Font MyDefaultSkinFontBold9;

		public CoursewareDelivererLogoDetails (GameManagement.NetworkProgressionGameFile gameFile)
		{
			_gameFile = gameFile;

			string fontName = SkinningDefs.TheInstance.GetData("fontname");
			MyDefaultSkinFontBold9 = ConstantSizeFont.NewFont(fontName, 9, FontStyle.Bold);

			this.Title = "Client Logo";

			picture = new PictureBox();
			picture.Location = new Point(5, 5);

			picture.Size = new Size (140, 70);
			picture.SizeMode = PictureBoxSizeMode.StretchImage;
			panel.Controls.Add(picture);

			picture.BorderStyle = BorderStyle.FixedSingle;

			Button load = new Button();
			load.Font = MyDefaultSkinFontBold9;
			load.Location = new Point(picture.Right + 20, 5);
			load.Size = new Size(80, 25);
		    if (SkinningDefs.TheInstance.GetBoolData("windows_buttons_styled", true))
		    {

		        load.BackColor =
		            SkinningDefs.TheInstance.GetColorDataGivenDefault("windows_button_colour", Color.LightSteelBlue);
		        load.ForeColor =
		            SkinningDefs.TheInstance.GetColorDataGivenDefault("windows_button_foreground_colour", Color.Black);
		    }
		    load.Text = "Load";
			load.Click += load_Click;
			panel.Controls.Add(load);

			Button del = new Button();
			del.Font = MyDefaultSkinFontBold9;
			del.Location = new Point(picture.Right + 20, 35);
			del.Size = new Size(80, 25);
		    if (SkinningDefs.TheInstance.GetBoolData("windows_buttons_styled", true))
		    {
		        del.BackColor =
		            SkinningDefs.TheInstance.GetColorDataGivenDefault("windows_button_colour", Color.LightSteelBlue);
		        del.ForeColor =
		            SkinningDefs.TheInstance.GetColorDataGivenDefault("windows_button_foreground_colour", Color.Black);
		    }
		    del.Text = "Reset";
			del.Click += del_Click;
			panel.Controls.Add(del);

			Button ok = new Button();
			ok.Font = MyDefaultSkinFontBold9;
			ok.Location = new Point(picture.Right + 20, 80);
			ok.Size = new Size(80, 25);
			ok.Name = "FLD ok Button";
		    if (SkinningDefs.TheInstance.GetBoolData("windows_buttons_styled", true))
		    {
		        ok.BackColor =
		            SkinningDefs.TheInstance.GetColorDataGivenDefault("windows_button_colour", Color.LightSteelBlue);
		        ok.ForeColor =
		            SkinningDefs.TheInstance.GetColorDataGivenDefault("windows_button_foreground_colour", Color.Black);
		    }
		    ok.Text = "OK";
			ok.Visible = false;
			panel.Controls.Add(ok);

			string fname = _gameFile.Dir + "\\global\\client_logo.png";
			if (File.Exists(fname))
			{
				try
				{
					Bitmap bmp = new Bitmap(fname);
					picture.Image = (Bitmap) BitmapUtils.ConvertToApectRatio((Bitmap) bmp, this.picture.Width * 1.0 / this.picture.Height);
					bmp.Dispose();
					bmp = null;
					System.GC.Collect();
				}
				catch
				{
					MessageBox.Show(TopLevelControl, "Failed to load image.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				}
			}

			if (picture.Image == null)
			{
				ResetToDefault();
			}
		}

		protected void load_Click (object sender, EventArgs e)
		{
			Bitmap bmp = BitmapUtils.LoadBitmap(this, "client_logo.png", _gameFile);
			if (null != bmp)
			{
				picture.Image = BitmapUtils.ConvertToApectRatio(bmp, this.picture.Width * 1.0 / this.picture.Height);
				bmp.Dispose();
				bmp = null;
				System.GC.Collect();
			}
		}

		protected void del_Click (object sender, EventArgs e)
		{
			string fname = _gameFile.Dir + "\\global\\client_logo.png";
			if (File.Exists(fname))
			{
				File.Delete(fname);
			}

			ResetToDefault();
		}

		protected void ResetToDefault ()
		{
			picture.Image = null;
		}
	}
}