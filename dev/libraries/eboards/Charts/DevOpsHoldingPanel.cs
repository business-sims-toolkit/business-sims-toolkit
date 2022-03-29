using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

using Algorithms;
using CoreUtils;
using GameManagement;
using LibCore;
using ResizingUi;

namespace Charts
{
    public class DevOpsHoldingPanel : SharedMouseEventControl
    {
        readonly NetworkProgressionGameFile gameFile;

        public DevOpsHoldingPanel (NetworkProgressionGameFile gameFile)
        {
            this.gameFile = gameFile;
	        boundsIdToRectanglesDictionary = new Dictionary<string, Rectangle>();
		}

        protected override void OnSizeChanged (EventArgs e)
        {
            Invalidate();
        }

        protected override void OnPaint (PaintEventArgs e)
        {
            float topHeightFraction = SkinningDefs.TheInstance.GetFloatData("hold_panel_top_size_fraction", 0.75f);
            float midHeightFraction = SkinningDefs.TheInstance.GetFloatData("hold_panel_mid_size_fraction", 0.135f);

            var topHeight = Height * topHeightFraction;
            var midHeight = Height * midHeightFraction;
            var bottomHeight = Height - topHeight - midHeight;

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

            RenderTopSection(e.Graphics, new RectangleF(0, 0, Width, topHeight));
            RenderMidSection(e.Graphics, new RectangleF(0, topHeight, Width, midHeight));
            RenderBottomSection(e.Graphics, new RectangleF(0, topHeight + midHeight, Width, bottomHeight));
        }

        void RenderTopSection(Graphics graphics, RectangleF sectionBounds)
        {
            using (var sectionBackgroundBrush = new SolidBrush(SkinningDefs.TheInstance.GetColorData("hold_panel_top_back_colour", Color.White)))
            {
                graphics.FillRectangle(sectionBackgroundBrush, sectionBounds);

                var clientImage = Repository.TheInstance.GetImage($@"{AppInfo.TheInstance.Location}\images\panels\devops_logo.png");
				const int padding = 5;

                var imageHeight = Math.Min(sectionBounds.Height - padding * 2, clientImage.Height);

	            var imageBounds = sectionBounds.CentreSubRectangle(imageHeight * clientImage.Width / clientImage.Height, imageHeight);

	            boundsIdToRectanglesDictionary["holding_top_image"] = imageBounds.ToRectangle();

	            graphics.DrawImage(clientImage, imageBounds);
			}
        }

        void RenderMidSection(Graphics graphics, RectangleF sectionBounds)
        {
	        boundsIdToRectanglesDictionary["holding_mid_section"] = sectionBounds.ToRectangle();

            using (var sectionBackgroundBrush = new SolidBrush(SkinningDefs.TheInstance.GetColorData("hold_panel_mid_back_colour", Color.Black)))
            using (var textBrush = new SolidBrush(SkinningDefs.TheInstance.GetColorData("hold_panel_text_colour", Color.White)))
            {
                graphics.FillRectangle(sectionBackgroundBrush, sectionBounds);

                var text = gameFile.GetOption("hold_screen_text", "DevOps Simulation");

                var font = this.GetFontToFit(FontStyle.Bold, text, new SizeF(Width, sectionBounds.Height));
                graphics.DrawString(text, font, textBrush, sectionBounds, new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                });
            }
        }

        void RenderBottomSection(Graphics graphics, RectangleF sectionBounds)
        {
            using (var sectionBackgroundBrush = new SolidBrush(SkinningDefs.TheInstance.GetColorData("hold_panel_bottom_back_colour", Color.FromArgb(70, 69, 83))))
            {
                graphics.FillRectangle(sectionBackgroundBrush, sectionBounds);
            }

            if (SkinningDefs.TheInstance.GetBoolData("show_watermark_text", false))
            {
                var client = gameFile.GetClient();
                client = client.Substring(0, Math.Min(client.Length, 30));

                var watermarkText = SkinningDefs.TheInstance.GetData("watermark_text_format")
                    .Replace("{clientName}", client)
                    .Replace("{licenseDate}", gameFile.Licence.ValidFromUtc?.ToString(SkinningDefs.TheInstance.GetData("watermark_text_date_format", "MMMM dd yyyy")) ?? "");

                var watermarkBounds = sectionBounds.AlignRectangle(sectionBounds.Width * 0.3f,
                    sectionBounds.Height * 0.3f, StringAlignment.Center, StringAlignment.Far);

                using (var font = SkinningDefs.TheInstance.GetPixelSizedFont(
                    this.GetFontSizeInPixelsToFit(FontStyle.Regular, watermarkText, watermarkBounds.Size)))
                {
                    graphics.DrawString(watermarkText, font, Brushes.White, watermarkBounds, new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    });
                }
            }
        }

	    readonly Dictionary<string, Rectangle> boundsIdToRectanglesDictionary;

	    public override List<KeyValuePair<string, Rectangle>> BoundIdsToRectangles => 
		    boundsIdToRectanglesDictionary.Select(kvp => new KeyValuePair<string, Rectangle>(kvp.Key, RectangleToScreen(kvp.Value))).ToList();

    }
}
