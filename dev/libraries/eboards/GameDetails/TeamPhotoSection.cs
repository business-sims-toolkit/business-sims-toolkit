using System;
using System.Windows.Forms;
using System.Drawing;
using System.IO;

using GameManagement;
using CoreUtils;

namespace GameDetails
{
	public class TeamPhotoSection : Panel
	{
		NetworkProgressionGameFile gameFile;

		Label label;
		PictureBox box;
		Button loadButton;
		Button removeButton;

		string PhotoFile
		{
			get
			{
				return gameFile.GetGlobalFile("team_photo.png");
			}
		}

		public TeamPhotoSection (NetworkProgressionGameFile gameFile)
		{
			this.gameFile = gameFile;

			BuildControls();
		}

		void BuildControls ()
		{
			label = new Label ();
			label.Font = SkinningDefs.TheInstance.GetFont(10);
			label.Text = "Team Photo";
            label.ForeColor = SkinningDefs.TheInstance.GetColorData("game_details_screen_text_colour", Color.Black);
            Controls.Add(label);

			box = new PictureBox ();
			box.SizeMode = PictureBoxSizeMode.StretchImage;
			box.BorderStyle = BorderStyle.FixedSingle;
			Controls.Add(box);

			loadButton = SkinningDefs.TheInstance.CreateWindowsButton(FontStyle.Bold);
			loadButton.Text = "Load...";
		    loadButton.Click += loadButton_Click;
			Controls.Add(loadButton);

			removeButton = SkinningDefs.TheInstance.CreateWindowsButton(FontStyle.Bold);
			removeButton.Text = "Remove";
		    removeButton.Click += removeButton_Click;
			Controls.Add(removeButton);

			DoLayout();
			LoadData();
		}

		void DoLayout ()
		{
			label.Location = new Point (0, 0);
			label.Size = new Size (120, 25);

			box.Location = new Point (label.Right, 0);
			box.Size = new Size (280, 200);

			Size buttonSize = new Size (75, 25);
			int gap = 10;

			loadButton.Location = new Point (label.Right, box.Bottom + gap);
			loadButton.Size = buttonSize;

			removeButton.Location = new Point (loadButton.Right + gap, box.Bottom + gap);
			removeButton.Size = buttonSize;

			Size = new Size (500, loadButton.Bottom + gap);
		}

		public void LoadData ()
		{
			removeButton.Enabled = false;

			if (File.Exists(PhotoFile))
			{
                LoadTeamPhoto(PhotoFile);
				removeButton.Enabled = true;
			}
		}

		public void SaveData ()
		{
		}

		void loadButton_Click (object sender, EventArgs args)
		{
			using (Bitmap image = BitmapUtils.LoadBitmap(this, "team_photo.png", gameFile))
			{
				if (image != null)
				{
					box.Image = (Image) (BitmapUtils.ConvertToApectRatio(image, box.Width * 1.0f / box.Height).Clone());
					removeButton.Enabled = true;
				}
			}
		}

		void removeButton_Click (object sender, EventArgs args)
		{
			if (box.Image != null)
			{
				box.Image.Dispose();
			}

		    if (File.Exists(PhotoFile))
		    {
		        File.Delete(PhotoFile);
		    }

		    box.Image = null;
			removeButton.Enabled = false;
		}

	    void LoadTeamPhoto(string fileName)
	    {
	        var ms = new MemoryStream();

            var imageBytes = File.ReadAllBytes(fileName);
            ms.Write(imageBytes, 0, imageBytes.Length);
            ms.Position = 0;

            var image = Image.FromStream(ms);
	        box.Image = image;
	    }
	}
}