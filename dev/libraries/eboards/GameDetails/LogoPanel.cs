using System;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using GameManagement;
using CoreUtils;
using LibCore;

namespace GameDetails
{

    public class LogoPanel : Panel
    {
        Label title;
        PictureBox image;
        Button loadButton;
        Button resetButton;

        NetworkProgressionGameFile gameFile;
        string destinationFilename;
        string defaultFilename;
        Size thumbnailSize; 
        
        public LogoPanel(NetworkProgressionGameFile gameFile, string title, string destinationFilename, string defaultFilename, Size size)
        {
            this.gameFile = gameFile;
            this.destinationFilename = destinationFilename;
            this.defaultFilename = defaultFilename;
            thumbnailSize = size;

            this.title = new Label();
            this.title.Font = SkinningDefs.TheInstance.GetFont(10);
            this.title.Text = title;
            this.title.ForeColor = SkinningDefs.TheInstance.GetColorData("game_details_screen_text_colour", Color.Black);
            Controls.Add(this.title);

            image = new PictureBox();
            image.BorderStyle = BorderStyle.FixedSingle;
            image.SizeMode = PictureBoxSizeMode.StretchImage;
            image.Image = null;
            Controls.Add(image);

	        loadButton = SkinningDefs.TheInstance.CreateWindowsButton();
            loadButton.Text = "Load...";
            loadButton.Click += loadButton_Click;
            Controls.Add(loadButton);

            resetButton = SkinningDefs.TheInstance.CreateWindowsButton();
            resetButton.Text = "Reset";
            resetButton.Click += resetButton_Click;
            Controls.Add(resetButton);

            DoLayout();
        }

        void DoLayout()
        {
            title.Location = new Point(0, 25);
            title.Size = new Size(120, 20);

            int maxWidth = 200;
            int actualWidth = Math.Min(maxWidth, thumbnailSize.Width);
            Size scaledSize = new Size(actualWidth, actualWidth * thumbnailSize.Height / thumbnailSize.Width);
            image.Location = new Point(title.Right, 0);
            image.Size = scaledSize;

            Size buttonSize = new Size(75, 25);
            int gap = 20;

            loadButton.Location = new Point(image.Right + 50, ((image.Top + image.Bottom - gap) / 2) - buttonSize.Height);
            loadButton.Size = buttonSize;

            resetButton.Location = new Point(loadButton.Left, loadButton.Bottom + gap);
            resetButton.Size = buttonSize;
        }

        void loadButton_Click(object sender, EventArgs e)
        {
            string filename = RequestImage();
            if (!string.IsNullOrEmpty(filename))
            {
                using (Image unscaledImage = Image.FromFile(filename))
                {
                    Image newImage = ImageUtils.ScaleImageToFitSize(unscaledImage, thumbnailSize);
                    SaveThumbnailImageIntoGame(newImage);
                }
            }
        }
        
        void resetButton_Click(object sender, EventArgs e)
        {
            SaveDefaultImageIntoGame();
        }

        void SaveDefaultImageIntoGame()
        {
            if (!string.IsNullOrEmpty(defaultFilename))
            {
                SaveImageIntoGame(AppInfo.TheInstance.Location + @"\images\" + defaultFilename);
            }
            else
            {
                File.Delete(gameFile.Dir + @"\global\" + destinationFilename);
            }
        }

        public void LoadData()
        {
            string specificFile = gameFile.Dir + @"\global\" + destinationFilename;
            if (File.Exists(specificFile))
            {
                ShowImage(specificFile);
            }
            else
            {
                SaveDefaultImageIntoGame();
            }
        }

        public bool SaveData() 
        {           
            return true;
        }

        void ShowImage(string filename)
        {
            using (Image loadedImage = Image.FromFile(filename))
            {
                Image scaledImage = ImageUtils.ScaleImageToFitSize(loadedImage, thumbnailSize);
                ShowImage(scaledImage);
            }
        }

        void ShowImage(Image image)
        {
            this.image.Image = image;
        }

        void SaveImageIntoGame(string filename)
        {
            using (Image unscaledImage = Image.FromFile(filename))
            {
                SaveThumbnailImageIntoGame(unscaledImage);
            }
        }

        void SaveThumbnailImageIntoGame(Image unscaledImage)
        {
            Image scaledImage = ImageUtils.ScaleImageToFitSize(unscaledImage, thumbnailSize);
            scaledImage.Save(gameFile.Dir + @"\global\" + destinationFilename);
            ShowImage(scaledImage);
        }

        string RequestImage()
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Image files (*.BMP;*.JPG;*.GIF;*.PNG;*.TIF)|*.BMP;*.JPG;*.GIF;*.PNG;*.TIF";
            DialogResult result = dialog.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                return dialog.FileName;
            }

            return null;
        }

        public Image GetIsolatedImage(string PreferedImageFileName, string BackupImageFileImage)
        {
            Bitmap tmp_img = null;
            Bitmap hack = null;

            if (File.Exists(PreferedImageFileName))
            {
                tmp_img = new Bitmap(PreferedImageFileName);
                tmp_img = (Bitmap)CommonGUI.BitmapUtils.ConvertToApectRatio((Bitmap)tmp_img, 2).Clone();
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

	    public Size ThumbnailSize => thumbnailSize;

	    public Size ShownImageSize => image.Size;
    }
}