using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

using CommonGUI;

using CoreUtils;
using DevOps.OpsEngine;
using LibCore;
using Network;

namespace DevOps.OpsScreen
{
	internal class NewServiceIcon : FlickerFreePanel
    {
        Node serviceNode;

        public string ImageLocation;
        public int BorderThickness;

        ToolTip toolTip;

        string finalStatus = null;

        public NewServiceIcon (Node serviceNode)
        {
            this.serviceNode = serviceNode;

            if (serviceNode != null)
            {
                serviceNode.AttributesChanged += service_AttributesChanged;
            }

            toolTip = new ToolTip();
            
            if (serviceNode != null)
            {
                toolTip.SetToolTip(this, serviceNode.GetAttribute("service_id") + " " + serviceNode.GetAttribute("product_id") + " " + serviceNode.GetAttribute("biz_service_function"));
                toolTip.BackColor = Color.FromArgb(251, 176, 59);
                toolTip.OwnerDraw = true;
                toolTip.Popup += toolTip_Popup;
                toolTip.Draw += toolTip_Draw;
            }
        }

        void toolTip_Popup(object sender, PopupEventArgs e)
        {
            using (Font f = SkinningDefs.TheInstance.GetFont(11, FontStyle.Bold))
            {
                e.ToolTipSize = TextRenderer.MeasureText(toolTip.GetToolTip(e.AssociatedControl), f);
                e.ToolTipSize = new Size(e.ToolTipSize.Width + 5, e.ToolTipSize.Height + 5);
            }
        }

        void toolTip_Draw (object sender, DrawToolTipEventArgs e)
        {
            e.DrawBackground();
            e.DrawBorder();
            
            using (StringFormat sf = new StringFormat())
            {
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;

                using (Font f = SkinningDefs.TheInstance.GetFont(11, FontStyle.Bold))
                {
                    e.Graphics.DrawString(e.ToolTipText, f, SystemBrushes.ActiveCaptionText, e.Bounds, sf);
                }
            }
        }

        bool installFailed;

        void service_AttributesChanged (Node sender, ArrayList attrs)
        {
            foreach(AttributeValuePair avp in attrs)
            {
                switch (avp.Attribute)
                {
                    case "is_auto_installed":
                        finalStatus = sender.GetAttribute("status");
                        break;
                    case "deployment_stage_status":
                        installFailed = avp.Value == ServiceStageStatus.Failed;
                        break;
                }
            }

            Invalidate();
        }

        protected override void OnSizeChanged (EventArgs e)
        {
            BackColor = Color.Transparent;
            Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (serviceNode != null)
                {
                    serviceNode.AttributesChanged -= service_AttributesChanged;
                }

                toolTip.Dispose();
            }

            base.Dispose(disposing);
        }

        protected override void OnPaint (PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
			g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            Image image = Repository.TheInstance.GetImage(ImageLocation + "blank_icon.png");
            
            if (serviceNode != null)
            {
                string status = finalStatus ?? serviceNode.GetAttribute("status");

                string imagePath = ImageLocation;
	            string imageName;
	            Color textColour;

                switch(status)
                {
                    case ServiceStatus.Test:
                    case ServiceStatus.TestDelay:
	                    imageName = Name + "_test.png";
	                    textColour = Color.Gray;
                        break;

                    case ServiceStatus.Release:
                    case "finishedRelease":
	                    imageName = Name + "_release.png";
	                    textColour = Color.White;
                        break;

                    case ServiceStatus.Live:
	                    imageName = Name + "_deployed.png";
	                    textColour = Color.White;
                        break;

                    default:
	                    imageName = Name + "_default.png";
	                    textColour = Color.DimGray;
                        break;
                }

                image = Repository.TheInstance.GetImage(imagePath + imageName);

				e.Graphics.DrawImage(image, new Rectangle(0, 0, Width, Height));

                if (status == ServiceStatus.TestDelay)
                {
                    using (Brush myBrush = new SolidBrush(Color.FromArgb(166, 94, 100, 104)))
                    {
                        float delayRemaining = serviceNode.GetIntAttribute("delayremaining", -2);
                        float testDelay = serviceNode.GetIntAttribute("test_time", 1);

                        e.Graphics.FillPie(myBrush,
                            new Rectangle(BorderThickness, BorderThickness, Size.Width - (2 * BorderThickness),
                                Size.Height - (2 * BorderThickness)), -90, 360 * (delayRemaining / testDelay));
                    }
                }
			}
			else
            {
                e.Graphics.DrawImage(image, new Rectangle(0, 0, Width, Height));
            }

	        if (installFailed)
	        {
		        int size = Width / 10;
		        using (var pen = new Pen(Color.Red, size))
		        {
			        e.Graphics.DrawEllipse(pen, size / 2, size / 2, Width - size, Height - size);
		        }
	        }
        }
	}
}
