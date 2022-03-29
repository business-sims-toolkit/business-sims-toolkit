using System;
using System.Drawing;
using System.Windows.Forms;

using Algorithms;
using LibCore;
using Path = System.IO.Path;

namespace ResizingUi
{
	public class ImageRadioButton : Panel
	{
		public ImageRadioButton (string imageFilePath, bool isCheckedByDefault)
		{
			IsDefaultSelected = isCheckedByDefault;

			OptionDetails = Path.GetFileName(imageFilePath);

			imagePanel = new PicturePanel();
			Controls.Add(imagePanel);

			imagePanel.ZoomWithCropping(Repository.TheInstance.GetImage(imageFilePath));

			radioButton = new RadioButton
			{
				Checked = isCheckedByDefault
			};
			Controls.Add(radioButton);
			radioButton.CheckedChanged += radioButton_CheckedChanged;

			LayoutOrientation = Orientation.LeftRight;
		}

		public bool IsDefaultSelected { get; }
		
		public event EventHandler CheckedChanged;
		

		void radioButton_CheckedChanged(object sender, EventArgs e)
		{
			if (radioButton.Checked)
			{
				CheckedChanged?.Invoke(this, e);
			}
		}

		public enum Orientation
		{
			LeftRight,
			TopBottom,
			RightLeft,
			BottomTop
		}

		Orientation layoutOrientation;

		public Orientation LayoutOrientation
		{
			set
			{
				layoutOrientation = value;
				DoLayout();
			}
		}

		public string OptionDetails { get; }

		public void SetToFalse ()
		{
			radioButton.Checked = false;
		}


		protected override void OnSizeChanged (EventArgs e)
		{
			DoLayout();
		}

		void DoLayout ()
		{
			if (layoutOrientation == Orientation.LeftRight || layoutOrientation == Orientation.RightLeft)
			{
				imagePanel.Height = Height;
			}
			else
			{
				imagePanel.Width = Width;
			}

			var radioButtonHorizontalAlignment = StringAlignment.Center;
			var radioButtonVerticalAlignment = StringAlignment.Center;

			var imagePanelHorizontalAlignment = StringAlignment.Center;
			var imagePanelVerticalAlignment = StringAlignment.Center;

			switch (layoutOrientation)
			{
				case Orientation.LeftRight:
					radioButtonHorizontalAlignment = StringAlignment.Near;
					radioButtonVerticalAlignment = StringAlignment.Center;

					imagePanelHorizontalAlignment = StringAlignment.Far;
					imagePanelVerticalAlignment = StringAlignment.Center;
					break;
				case Orientation.TopBottom:

					radioButtonHorizontalAlignment = StringAlignment.Center;
					radioButtonVerticalAlignment = StringAlignment.Near;

					imagePanelHorizontalAlignment = StringAlignment.Center;
					imagePanelVerticalAlignment = StringAlignment.Far;

					break;
				case Orientation.RightLeft:
					radioButtonHorizontalAlignment = StringAlignment.Far;
					radioButtonVerticalAlignment = StringAlignment.Center;

					imagePanelHorizontalAlignment = StringAlignment.Near;
					imagePanelVerticalAlignment = StringAlignment.Center;

					break;
				case Orientation.BottomTop:

					radioButtonHorizontalAlignment = StringAlignment.Center;
					radioButtonVerticalAlignment = StringAlignment.Far;

					imagePanelHorizontalAlignment = StringAlignment.Center;
					imagePanelVerticalAlignment = StringAlignment.Near;

					break;
			}

			var bounds = new Rectangle(0, 0, Width, Height);

			radioButton.Bounds = bounds.AlignRectangle(radioButton.GetPreferredSize(Size.Empty), radioButtonHorizontalAlignment, radioButtonVerticalAlignment);
			imagePanel.Bounds = bounds.AlignRectangle(imagePanel.Size, imagePanelHorizontalAlignment, imagePanelVerticalAlignment);


		}

		readonly RadioButton radioButton;
		readonly PicturePanel imagePanel;
	}
}
