using System.Drawing;
using System.Drawing.Drawing2D;
using Algorithms;

namespace ResizingUi
{
	public class GradientBackgroundProperties
	{
		public Rectangle Bounds
		{
			get => bounds;
			set
			{
				bounds = value;
				UpdateImage();
			}
		}
		
		public Color StartColour
		{
			get => startColour;
			set
			{
				startColour = value;
				UpdateImage();
			}
		}
		public Color EndColour
		{
			get => endColour;
			set
			{
				endColour = value;
				UpdateImage();
			}
		}

		public LinearGradientMode GradientMode
		{
			get => gradientMode;
			set
			{
				gradientMode = value;
				UpdateImage();
			}
		} 

		public Image Image {get; private set; }
		
		void UpdateImage()
		{
			if (Bounds.Width == 0 || Bounds.Height == 0)
			{
				return;
			}

			var img = new Bitmap(Bounds.Width, Bounds.Height);

			var brushBounds = new Rectangle(0, 0, Bounds.Width, Bounds.Height);
			
			using (var graphics = Graphics.FromImage(img))
			using (var brush = new LinearGradientBrush(brushBounds, StartColour, EndColour, GradientMode))
			{
				graphics.FillRectangle(brush, brushBounds);
			}
			
			Image = img;
		}

		Rectangle bounds;
		Color startColour;
		Color endColour;
		LinearGradientMode gradientMode = LinearGradientMode.Horizontal;

	}
}
