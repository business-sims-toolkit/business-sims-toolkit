using System;
using System.Windows.Forms;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using CoreUtils;
using DevOps.OpsEngine;
using LibCore;
using Network;

namespace DevOps.OpsScreen
{
    internal class ServiceProgressFeedbackPanel : Panel
    {

        public ServiceProgressFeedbackPanel (Node service)
        {
            this.service = service;

            this.service.AttributesChanged += service_AttributesChanged;

            Setup();
        }
        
        protected override void OnSizeChanged (EventArgs e)
        {
            DoLayout();
        }
        

        void DoLayout ()
        {
            var widthPadding = (int)(Width * 0.03f); //TODO

            imageLabel.Location = new Point(Width - widthPadding - imageLabel.Width, (Height - imageLabel.Height) / 2);

            // TODO resize height and font of text label?? 
            textLabel.Size = new Size(Width - widthPadding * 2 - imageLabel.Width - 10, 30); //TODO
            textLabel.Location = new Point(widthPadding, (Height - textLabel.Height) / 2);

        }

        void Setup ()
        {
            textLabel = new Label
            {
                TextAlign = ContentAlignment.MiddleRight,
                BackColor = Color.Transparent,
                ForeColor = Color.White,
                Font = SkinningDefs.TheInstance.GetFont(14f, FontStyle.Bold),
                Padding = new Padding(5, 0, 10, 0)
            };

            Controls.Add(textLabel);
            
            images = new Dictionary<string, Image>();
            foreach (var imageName in FeedbackImageName.All)
            {
                images[imageName] = Repository.TheInstance.GetImage($@"\images\chart\{imageName}.png");
            }

            imageLabel = new Label
            {
                BackColor = Color.Transparent,
                Image = null,
                Size = new Size(30,30) // TODO responsive?? 
            };

            Controls.Add(imageLabel);

            var hasFailedInstallation = service.GetAttribute("deployment_stage_status") == ServiceStageStatus.Failed;

            if (hasFailedInstallation)
            {
                textLabel.Text = service.GetAttribute("install_feedback_message");
                imageLabel.Image = images[service.GetAttribute("feedback_image")];
            }

        }

        
        void service_AttributesChanged(Node sender, ArrayList attrs)
        {
            foreach (AttributeValuePair avp in attrs)
            {
                switch (avp.Attribute)
                {
                    case "feedback_message":
                    case "install_feedback_message":
                        textLabel.Text = avp.Value;
                        break;
                    case "feedback_image":
                        if (string.IsNullOrEmpty(avp.Value))
                        {
                            imageLabel.Visible = false;
                        }
                        else
                        {
                            imageLabel.Visible = true;
                            imageLabel.Image = images[avp.Value];
                        }
                        
                        break;
                }
            }
                
        }

        readonly Node service;

        Label textLabel;
        Label imageLabel;
        Dictionary<string, Image> images;
    }
}
