using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using CoreUtils;
using GameManagement;
using LibCore;
using ResizingUi;

namespace GameDetails
{
	public class CustomReportRoundImagesPanel : Panel
	{
		NetworkProgressionGameFile gameFile;
		int round;

		Label label;
		ChoosablePicturePanel mainImage;
		ChoosablePicturePanel revealedImage;
		ErrorProvider errorProvider;

		string MainImageTag => CONVERT.Format("report_round_{0}", round);
		string RevealedImageTag => CONVERT.Format("report_round_{0}_revealed", round);

		public CustomReportRoundImagesPanel (NetworkProgressionGameFile gameFile, int round, ErrorProvider errorProvider)
		{
			this.gameFile = gameFile;
			this.round = round;

			this.errorProvider = errorProvider;

			label = new Label { Font = SkinningDefs.TheInstance.GetFont(14, FontStyle.Bold), Text = CONVERT.Format("Round {0}", round), TextAlign = ContentAlignment.MiddleLeft };
			Controls.Add(label);

			mainImage = new ChoosablePicturePanel ();
			mainImage.Click += mainImage_Click;
			Controls.Add(mainImage);

			revealedImage = new ChoosablePicturePanel ();
			revealedImage.Click += revealedImage_Click;
			Controls.Add(revealedImage);

			LoadData();

			DoSize();
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);

			DoSize();
		}

		void DoSize ()
		{
			label.Bounds = new Rectangle (0, 0, 150, Height);

			mainImage.Bounds = new Rectangle (label.Right + 10, 0, (mainImage.Image?.Width * Height / mainImage.Image?.Height) ?? Height, Height);
			revealedImage.Bounds = new Rectangle (mainImage.Right + 10, 0, (revealedImage.Image?.Width * Height / revealedImage.Image?.Height) ?? Height, Height);
		}

		public void LoadData ()
		{
			errorProvider.Clear();

			switch (gameFile.CustomContentSource)
			{
				case CustomContentSource.GameFile:
				{
					var main = gameFile.GetCustomContentFilename(MainImageTag);
					mainImage.ZoomWithLetterboxing((main != null) ? Repository.TheInstance.GetImage(main) : null);

					var revealed = gameFile.GetCustomContentFilename(RevealedImageTag);
					revealedImage.ZoomWithLetterboxing((revealed != null) ? Repository.TheInstance.GetImage(revealed) : null);

					break;
				}

				case CustomContentSource.None:
				default:
					mainImage.RemoveImage();
					revealedImage.RemoveImage();
					break;
			}

			DoSize();
		}

		void mainImage_Click (object sender, EventArgs args)
		{
			SelectAndLoadImage(MainImageTag);
		}

		void revealedImage_Click (object sender, EventArgs args)
		{
			SelectAndLoadImage(RevealedImageTag);
		}

		void SelectAndLoadImage (string tag)
		{
			using (var dialog = new OpenFileDialog
			{
				Filter = "Image files (*.jpg, *.jpeg, *.png)|*.jpg;*.jpeg;*.png",
				Title = "Select custom report image file"
			})
			{
				if (dialog.ShowDialog(TopLevelControl) == DialogResult.OK)
				{
					gameFile.ImportCustomContent(tag, dialog.FileName);
					LoadData();
				}
				else
				{
					gameFile.DeleteCustomContentByTag(tag);
					LoadData();
				}
			}
		}

		public bool Validate ()
		{
			return true;
		}
	}
}