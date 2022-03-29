using System;
using System.Drawing;
using System.Windows.Forms;
using Algorithms;
using CommonGUI;
using CoreUtils;
using GameManagement;

namespace ResizingUi
{
    public class LogoStrip : FlickerFreePanel
    {
        readonly Image facilitatorLogo;
        readonly Image clientLogo;

        readonly IWatermarker watermarker;

        readonly ReadoutPanel.ReadoutPanel serviceMetrics;

        public LogoStrip (NetworkProgressionGameFile gameFile, IWatermarker watermarker)
        {
            this.watermarker = watermarker;

            facilitatorLogo = gameFile.GetFacilitatorLogo();
            clientLogo = gameFile.GetClientLogo();

            serviceMetrics = new ReadoutPanel.ReadoutPanel
            {
                BackColor = SkinningDefs.TheInstance.GetColorData("ops_logo_strip_background_colour", Color.White),
                Watermarker = watermarker,
                EntryLayout = ResizingUi.ReadoutPanel.Layout.Horizontal,
                SeriesLayout = ResizingUi.ReadoutPanel.Layout.Horizontal,
                LegendAlignment = StringAlignment.Center,
                ValueAlignment = StringAlignment.Center,
                HorizontalMarginFraction = 0.1f,
                HorizontalBoundsDivideFraction = 0.65f
            };

            var networkModel = gameFile.NetworkModel;

            var metricsTextColour = SkinningDefs.TheInstance.GetColorData("ops_logo_strip_foreground_colour", Color.FromArgb(62, 62, 62));

            serviceMetrics.AddPercentageEntry("Availability", metricsTextColour, networkModel.GetNamedNode("Availability"), "availability");
            serviceMetrics.AddPercentageEntry("Impact", metricsTextColour, networkModel.GetNamedNode("Impact"), "impact");
            serviceMetrics.AddIntegerEntry("SLA Breaches", 99, metricsTextColour, networkModel.GetNamedNode("sla_breach"), "biz_serv_count");
            Controls.Add(serviceMetrics);
        }

        protected override void OnSizeChanged (EventArgs e)
        {
            DoLayout();
        }

        protected override void OnPaint (PaintEventArgs e)
        {
	        using (var brush = new SolidBrush (SkinningDefs.TheInstance.GetColorData("ops_logo_strip_background_colour", Color.White)))
	        {
		        e.Graphics.FillRectangle(brush, new Rectangle (0, 0, Width, Height));
	        }

	        var subSectionWidth = Width / 3;
            
            var facilitatorLogoBounds = new RectangleF(0, 0, subSectionWidth, Height);
            RenderCentredImage(e.Graphics, facilitatorLogo, facilitatorLogoBounds, Height);

            var clientLogoBounds = new RectangleF(Width - subSectionWidth, 0, subSectionWidth, Height);
            RenderCentredImage(e.Graphics, clientLogo, clientLogoBounds, Height);
            
            watermarker?.Draw(this, e.Graphics);
        }

        void RenderCentredImage (Graphics graphics, Image image, RectangleF renderBounds, float desiredImageHeight)
        {
            var aspectRatio = image.Width / (float) image.Height;

            var imageWidth = aspectRatio * desiredImageHeight;

            graphics.DrawImage(image, renderBounds.CentreSubRectangle(imageWidth, desiredImageHeight));

        }

        void DoLayout ()
        {
            serviceMetrics.Bounds = new Rectangle(Width / 4, 0, Width / 2, Height);
            // TODO metric panel changes?

            Invalidate();
        }
        
    }
}
