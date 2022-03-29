using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.Xml;
using CommonGUI;
using LibCore;

namespace Charts
{
    public class HatchFillProperties
    {
        public int Angle { get; set; }
        public float LineWidth { get; set; }
        public float AltLineWidth { get; set; }
        public Color LineColour { get; set; }
        public Color AltLineColour { get; set; }

        public float PatternWidth => LineWidth + AltLineWidth;

        public HatchFillProperties ()
        {
        }
        public HatchFillProperties (XmlNode node)
        {
            var angle = BasicXmlDocument.GetIntAttribute(node, "line_angle", 0);
            
            Debug.Assert(Math.Abs(angle) % 45 == 0, "Angle needs to be a multiple of 45");

            Angle = angle;
            LineWidth = BasicXmlDocument.GetFloatAttribute(node, "line_width", 2);
            AltLineWidth = BasicXmlDocument.GetFloatAttribute(node, "alt_line_width", LineWidth);
            LineColour = BasicXmlDocument.GetColourAttribute(node, "line_colour", Color.Black);
            AltLineColour = BasicXmlDocument.GetColourAttribute(node, "alt_line_colour", Color.White);
        }
    }

    public class HatchFillImage
    {
        readonly HatchFillProperties hatchProperties;

        public Image HatchedImage { get; }

        public HatchFillImage (XmlNode node)
        {
            hatchProperties = new HatchFillProperties(node);
            
            HatchedImage = CreateHatchedImage();
        }

        public void RenderToBounds (Graphics graphics, RectangleF bounds)
        {
            for (var y = (int) bounds.Top; y <= bounds.Bottom; y += HatchedImage.Height)
            {
                for (var x = (int) bounds.Left; x <= bounds.Right; x += HatchedImage.Width)
                {
                    graphics.DrawImageUnscaledAndClipped(HatchedImage, new Rectangle(x, y, 
                        (int)Math.Min(HatchedImage.Width, bounds.Right - x), 
                        (int)Math.Min(HatchedImage.Height, bounds.Bottom - y)));
                }
            }
        }

        Image CreateHatchedImage ()
        {
            int hatchedImageWidth;
            int hatchedImageHeight;

            if (hatchProperties.Angle == 0 || Math.Abs(hatchProperties.Angle) == 90)
            {
                // Horizontal lines (angle = 0)
                // Vertical lines (angle = 90)

                hatchedImageWidth = hatchedImageHeight = 
                    (int) Math.Round(hatchProperties.PatternWidth * 3, MidpointRounding.AwayFromZero);
            }
            else
            {
                // Diagonal lines (0 > angle < 90)
                // For now this is restricted to being 45/-45

                hatchedImageWidth = (int)Math.Abs(Math.Round(hatchProperties.PatternWidth / Math.Cos(hatchProperties.Angle * 180 / Math.PI),
                    MidpointRounding.AwayFromZero)) - 3;

                hatchedImageHeight = (int)Math.Abs(Math.Round(hatchProperties.PatternWidth / Math.Sin(hatchProperties.Angle * 180 / Math.PI),
                    MidpointRounding.AwayFromZero)) + 2;
            }

            var tempImageWidth = hatchedImageWidth * 16;
            var tempImageHeight = hatchedImageHeight * 16;
            
            using (var finalImage = new Bitmap(hatchedImageWidth, hatchedImageHeight))
            using (var tempImage = new Bitmap(tempImageWidth, tempImageHeight, PixelFormat.Format32bppArgb))
            using (var finalGraphics = Graphics.FromImage(finalImage))
            using (var tempGraphics = Graphics.FromImage(tempImage))
            using (var linePen = new Pen(hatchProperties.LineColour, hatchProperties.LineWidth))
            using (var altLinePen = new Pen(hatchProperties.AltLineColour, hatchProperties.AltLineWidth))
            {
                tempGraphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                finalGraphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

                if (Math.Abs(hatchProperties.Angle) > 0)
                {
                    tempGraphics.RotateTransform(hatchProperties.Angle);
                    tempGraphics.TranslateTransform(-tempImageWidth / 2f, -tempImageHeight/2f);
                }

                for (var y = hatchProperties.LineWidth / 2; y <= tempImageHeight; y += hatchProperties.PatternWidth / 2)
                {
                    tempGraphics.DrawLine(linePen, 0, y, tempImageWidth, y);
                    y += hatchProperties.PatternWidth / 2;
                    tempGraphics.DrawLine(altLinePen, 0, y, tempImageWidth, y);
                }

                finalGraphics.DrawImageUnscaled(tempImage, new Rectangle(0, 0, hatchedImageWidth, hatchedImageHeight));

                return new Bitmap(finalImage);
            }
        }
    }

    public class HatchFillLegendPanel : FlickerFreePanel
    {
        readonly HatchFillImage hatchFillImage;
	    readonly Action<Graphics, RectangleF, Color?> customRenderer;

		public HatchFillLegendPanel (HatchFillImage hatchImage)
        {
            hatchFillImage = hatchImage;
        }

	    public HatchFillLegendPanel (Action<Graphics, RectangleF, Color?> customRenderer)
	    {
		    this.customRenderer = customRenderer;
	    }

        public HatchFillLegendPanel (XmlNode node)
        {
            hatchFillImage = node == null ? null : new HatchFillImage(node);
        }

        protected override void OnSizeChanged (EventArgs e)
        {
            Invalidate();
        }

        protected override void OnPaint (PaintEventArgs e)
        {
            hatchFillImage?.RenderToBounds(e.Graphics, new RectangleF(0, 0, Width, Height));

			customRenderer?.Invoke(e.Graphics, new RectangleF(0, 0, Width, Height), null);
        }
    }
}
