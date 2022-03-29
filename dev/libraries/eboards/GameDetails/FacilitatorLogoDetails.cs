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
	public class FacilitatorLogoDetails : GameDetailsSection
	{
		protected PictureBox picture;
		protected GameManagement.NetworkProgressionGameFile _gameFile;
		protected Font MyDefaultSkinFontBold9; 

		public FacilitatorLogoDetails(GameManagement.NetworkProgressionGameFile gameFile)
		{
			_gameFile = gameFile;

			string fontname =  SkinningDefs.TheInstance.GetData("fontname");
			MyDefaultSkinFontBold9 = ConstantSizeFont.NewFont(fontname,9,FontStyle.Bold);

			this.Title = "Facilitator Logo";
			picture = new PictureBox();
			picture.Location = new Point(5,5);

			// : This code used to resize the images to be 140x60 (2.33:1).  But in the game
			// proper, they are required to be 140x70 (2:1).  This only became
			// a noticable problem when the game details screen was changed to preload the
			// default logos.  Then, the mismatch in sizes led to aspect ratio distortions
			// as images moved back and forth.  This now fixes 3774 and 
			picture.Size = new Size(140, 70);
			picture.SizeMode = PictureBoxSizeMode.StretchImage;
			panel.Controls.Add(picture);

			picture.BorderStyle = BorderStyle.FixedSingle;

			Button load = new Button();
			load.Font = MyDefaultSkinFontBold9;
			load.Location = new Point(310,5);
			load.Size = new Size(80,25);
		    if (SkinningDefs.TheInstance.GetBoolData("windows_buttons_styled", true))
		    {

		        load.BackColor =
		            SkinningDefs.TheInstance.GetColorDataGivenDefault("windows_button_colour", Color.LightSteelBlue);
		        load.ForeColor =
		            SkinningDefs.TheInstance.GetColorDataGivenDefault("windows_button_foreground_colour", Color.Black);
		    }
		    load.Text = "Load";
            load.Name = "FLD load Button";
			load.Click += load_Click;
			panel.Controls.Add(load);

			Button del = new Button();
			del.Font = MyDefaultSkinFontBold9;
            del.Name = "FLD load Button";
			del.Location = new Point(310,35);
			del.Size = new Size(80,25);
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

			Button ok = SkinningDefs.TheInstance.CreateWindowsButton(FontStyle.Bold);
			ok.Location = new Point(310,80);
			ok.Size = new Size(80,25);
            ok.Name = "FLD ok Button";
		    ok.Text = "OK";
			ok.Visible = false;
			panel.Controls.Add(ok);

			string fname = _gameFile.Dir + "\\global\\facil_logo.png";
			if(File.Exists(fname))
			{
				try
				{
					Bitmap bmp = new Bitmap(fname);
					picture.Image = (Bitmap) BitmapUtils.ConvertToApectRatio((Bitmap)bmp, this.picture.Width * 1.0 / this.picture.Height);
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

		protected void load_Click(object sender, EventArgs e)
		{
			Bitmap bmp = BitmapUtils.LoadBitmap(this, "facil_logo.png", _gameFile);
			if(null != bmp)
			{
				picture.Image = BitmapUtils.ConvertToApectRatio(bmp, this.picture.Width * 1.0 / this.picture.Height);
				bmp.Dispose();
				bmp = null;
				System.GC.Collect();
			}
		}

		protected void del_Click(object sender, EventArgs e)
		{
			string fname = _gameFile.Dir + "\\global\\facil_logo.png";
			if(File.Exists(fname))
			{
				File.Delete(fname);
			}

			ResetToDefault();
		}

		protected void ResetToDefault ()
		{
			Bitmap bmp = BitmapUtils.LoadBitmapGivenFilename(this, "facil_logo.png", _gameFile, AppInfo.TheInstance.Location + "\\images\\DefFacLogo.png");
			picture.Image = BitmapUtils.ConvertToApectRatio(bmp, this.picture.Width * 1.0 / this.picture.Height);
			bmp.Dispose();
		}
	}
}