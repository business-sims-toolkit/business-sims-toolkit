using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Xml;

using LibCore;
using CoreUtils;

namespace GameDetails
{
	/// <summary>
	/// Summary description for TeamdLogoDetails.
	/// </summary>
	public class TeamNameAndLogoDetails : GameDetailsSection
	{
		protected PictureBox picture;
		protected GameManagement.NetworkProgressionGameFile _gameFile;
		protected Font MyDefaultSkinFontBold9; 

		protected TextBox team_name;

		string TeamFile
		{
			get
			{
				return _gameFile.Dir + @"\global\team.xml";
			}
		}

		public TeamNameAndLogoDetails(GameManagement.NetworkProgressionGameFile gameFile)
		{
			_gameFile = gameFile;

			string fontname =  SkinningDefs.TheInstance.GetData("fontname");
			MyDefaultSkinFontBold9 = ConstantSizeFont.NewFont(fontname,9,FontStyle.Bold);

			this.Title = "Team Name And Logo";
			picture = new PictureBox();
			picture.Location = new Point(5,5);

			// : Changed from 140x60 to 140x70.  See FacilitatorLogoDetails for more info.
			picture.Size = new Size(140, 70);
			picture.SizeMode = PictureBoxSizeMode.StretchImage;
			panel.Controls.Add(picture);

			picture.BorderStyle = BorderStyle.FixedSingle;

			Button load = SkinningDefs.TheInstance.CreateWindowsButton(FontStyle.Bold);
            load.Name = "load Button";
			load.Location = new Point(310,5);
			load.Size = new Size(80,25);
		    load.Text = "Load";
			load.Click += load_Click;
			panel.Controls.Add(load);

			Button del = SkinningDefs.TheInstance.CreateWindowsButton(FontStyle.Bold);
            del.Name = "del Button";
			del.Location = new Point(310,35);
			del.Size = new Size(80,25);
		    del.Text = "Reset";
			del.Click += del_Click;
			panel.Controls.Add(del);

			team_name = new TextBox();
			team_name.Text = "";
			team_name.Location = new Point(0, 80);
			team_name.Font = this.MyDefaultSkinFontBold12;
			team_name.Size = new Size(300,20);
			panel.Controls.Add(team_name);

			Button ok = SkinningDefs.TheInstance.CreateWindowsButton(FontStyle.Bold);
			ok.Name = "TLD ok Button";
			ok.Location = new Point(310,80);
			ok.Size = new Size(80,25);
		    ok.Text = "OK";
			ok.Visible = false;
			panel.Controls.Add(ok);

			LoadData();

			SetSize(460, 100);
		}

		public override void LoadData()
		{
			string fname = _gameFile.Dir + "\\global\\logo.png";
			if(File.Exists(fname))
			{
				try
				{
					Bitmap bmp = new Bitmap(fname);
					picture.Image = (Bitmap) BitmapUtils.ConvertToApectRatio((Bitmap)bmp, this.picture.Width * 1.0 / this.picture.Height).Clone();
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

			BasicXmlDocument xml = BasicXmlDocument.LoadOrCreate(TeamFile);
			XmlElement root = XMLUtils.GetOrCreateElement(xml, "team");
			XmlElement teamElement = XMLUtils.GetOrCreateElement(xml, "team_name");
			team_name.Text = teamElement.InnerText;
		}

		public void ChangeTeamNameVisibility(Boolean show)
		{
			this.team_name.Visible = show;
			if (show)
			{
				this.Title = "Team Name And Logo";
			}
			else
			{
				this.Title = "Team Logo";
			}
		}

		protected void load_Click(object sender, EventArgs e)
		{
			Bitmap bmp = BitmapUtils.LoadBitmap(this, "logo.png", _gameFile);
			if(null != bmp)
			{
				picture.Image = BitmapUtils.ConvertToApectRatio(bmp,this.picture.Width * 1.0 / this.picture.Height);				
				bmp.Dispose();
				bmp = null;
				System.GC.Collect();
			}
		}

		protected void del_Click(object sender, EventArgs e)
		{
			string fname = _gameFile.Dir + "\\global\\logo.png";
			if(File.Exists(fname))
			{
				File.Delete(fname);
			}
			//
			ResetToDefault();
		}

		protected void ResetToDefault ()
		{
			Bitmap bmp = BitmapUtils.LoadBitmapGivenFilename(this, "logo.png", _gameFile, LibCore.AppInfo.TheInstance.Location + "\\images\\DefCustLogo.png");
			picture.Image = BitmapUtils.ConvertToApectRatio(bmp, this.picture.Width * 1.0 / this.picture.Height);
			bmp.Dispose();
		}

		public override bool SaveData ()
		{
			BasicXmlDocument xml = BasicXmlDocument.LoadOrCreate(TeamFile);
			XmlElement root = XMLUtils.GetOrCreateElement(xml, "team");
			XmlElement nameElement = XMLUtils.GetOrCreateElement(root, "team_name");
			nameElement.InnerText = team_name.Text;
			xml.Save(TeamFile);

			return true;
		}
	}
}