using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using CommonGUI;
using CoreUtils;
using DevOps.OpsEngine;
using LibCore;
using Network;
using ResizingUi.Button;

// ReSharper disable ObjectCreationAsStatement

namespace DevOps.OpsScreen
{
	internal class InstallServicePanel : Panel
    {
        readonly Node service;
        readonly RequestsManager requestsManager;

        Label titleLabel;

        EnclosurePanel enclosurePanel;
        Label installFailedLabel;
        
        string enclosureSelected;

        StyledDynamicButton queueInstallButton;
        StyledDynamicButton backButton;

        int widthPadding = 15;
        int heightPadding = 5;

        int panelHeights;

        bool isServiceDeployable;

        readonly NodeTree model;

        enum ButtonState
        {
            Enqueue,
            Install,
            Disabled
        }

        ButtonState buttonState;
        
        public event EventHandler InstallClicked;

        void OnInstallClicked()
        {
            InstallClicked?.Invoke(this, EventArgs.Empty);
        }

        public InstallServicePanel(Node service, RequestsManager requestsManager, NodeTree model, bool isDeployable = true)
        {
            this.service = service;
            this.requestsManager = requestsManager;
            this.model = model;

            isServiceDeployable = isDeployable;
            
            Setup();
        }

        void Setup()
        {
            var serviceStatus = service.GetAttribute("status");

            switch (serviceStatus)
            {
                case ServiceStatus.Dev:
                case ServiceStatus.Test:
                case ServiceStatus.TestDelay:
                    buttonState = ButtonState.Enqueue;
                    break;
                case ServiceStatus.Release:
                    buttonState = ButtonState.Install;
                    break;
                default:
                    buttonState = ButtonState.Disabled;
                    break;
            }

            DoubleBuffered = true;

            titleLabel = new Label
            {
                Text = "Install",
                Font = SkinningDefs.TheInstance.GetFont(12, FontStyle.Bold),
                Location = new Point(widthPadding, 0),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft
            };
            Controls.Add(titleLabel);

            const int installButtonWidth = 100;
            const int installButtonHeight = 30;

            var installButtonText = buttonState == ButtonState.Enqueue ? "Enqueue" : "Install";

            queueInstallButton = new StyledDynamicButton("standard", installButtonText)
            {
                Size = new Size(installButtonWidth, installButtonHeight),

                Font = SkinningDefs.TheInstance.GetFont(12f)
            };

            queueInstallButton.Click += installButton_Click;
            Controls.Add(queueInstallButton);
            queueInstallButton.BringToFront();

            queueInstallButton.Enabled = false;

            installFailedLabel = new Label
            {
                Text = "Failed to Deploy",
                Font = SkinningDefs.TheInstance.GetFont(50f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                ForeColor = CONVERT.ParseHtmlColor("#ff2d4e"),
                Visible = false
            };

            var hasAttemptedInstall = service.GetBooleanAttribute("is_install_successful");

            isServiceDeployable = !hasAttemptedInstall.HasValue;
            
            installFailedLabel.Visible = (hasAttemptedInstall.HasValue && !hasAttemptedInstall.Value);

            Controls.Add(installFailedLabel);

            service.AttributesChanged += service_AttributesChanged;

            backButton = new StyledDynamicButton("standard", "Back")
            {
                Font = SkinningDefs.TheInstance.GetFont(12f),
                Size = new Size(installButtonWidth, installButtonHeight)
            };

            Controls.Add(backButton);
            backButton.BringToFront();

            backButton.Click += backButton_Click;

            backButton.Visible = service.GetAttribute("deployment_stage_status") != ServiceStageStatus.Failed;
        }

        void backButton_Click(object sender, EventArgs e)
        {
            Hide();
        }

        void service_AttributesChanged(Node sender, ArrayList attrs)
        {
            foreach (AttributeValuePair avp in attrs)
            {
                if (avp.Attribute.Equals("is_install_successful"))
                {
                    var installSuccessful = sender.GetBooleanAttribute("is_install_successful", true);

                    if (!installSuccessful)
                    {
                        enclosurePanel.Visible = false;
                        installFailedLabel.Visible = true;
                    }
                    else
                    {
                        enclosurePanel.Enabled = false;
                    }
                }
            }

            var canDeploy = sender.GetBooleanAttribute("can_deploy", false);

            queueInstallButton.Enabled = canDeploy;

            if (!canDeploy && sender.GetAttribute("server").Contains("Fail"))
            {
                enclosurePanel.Visible = false;
            }
        }
        
        void installButton_Click (object sender, EventArgs e)
        {
            var command = (buttonState == ButtonState.Enqueue) ? "enqueue_deployment" : "install_service";

            new Node(model.GetNamedNode("BeginServicesCommandQueue"), command, "",
                new List<AttributeValuePair>
                {
                    new AttributeValuePair("type", command),
                    new AttributeValuePair("service_name", service.GetAttribute("name"))
                });

            if (buttonState == ButtonState.Install)
            {
                OnInstallClicked();
            } 
            else if (buttonState == ButtonState.Enqueue)
            {
                Hide();
            }
        }
        
        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);

            DoLayout();
        }

        void DoLayout()
        {
            titleLabel.Size = new Size(Width - (2 * widthPadding), 30);
            
            panelHeights = Math.Max(144, (queueInstallButton.Top - 60) - (titleLabel.Bottom + heightPadding));

            enclosureSelected = string.Empty;

            installFailedLabel.Size = new Size(Width - (2 * widthPadding), panelHeights);
            installFailedLabel.Location = new Point(widthPadding, titleLabel.Bottom + heightPadding);
            
            RemoveEnclosurePanel();
            CreateEnclosurePanel();

            queueInstallButton.Location = new Point(Width - widthPadding - queueInstallButton.Width, enclosurePanel.Bottom + 15);
            
            backButton.Location = new Point(queueInstallButton.Left - 10 - backButton.Width, queueInstallButton.Top);

        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                service.AttributesChanged -= service_AttributesChanged;
            }
        }

        void CreateEnclosurePanel()
        {
            var enclosureNames = requestsManager.GetEnclosureNames();
            enclosureNames.Sort();

            int enclosureWidth = Width - (2 * widthPadding);

            enclosurePanel = new EnclosurePanel(enclosureNames, service.GetAttribute("server"), service.GetAttribute("enclosure_selection"))
            {
                Size = new Size(enclosureWidth, panelHeights),
                Location = new Point(widthPadding, titleLabel.Bottom + heightPadding),
                Enabled = isServiceDeployable,
                Visible = !installFailedLabel.Visible
            };

            enclosurePanel.EnclosureButtonClicked += enclosurePanel_EnclosureButtonClicked;

            Controls.Add(enclosurePanel);
        }

        void enclosurePanel_EnclosureButtonClicked(object sender, EventArgs e)
        {
            var enclosureButton = (StyledDynamicButton)sender;
            enclosureButton.Tag = true;
            enclosureSelected = enclosureButton.Text;

            new Node(model.GetNamedNode("BeginServicesCommandQueue"), "enclosure_selection", "",
                new List<AttributeValuePair>
                {
                    new AttributeValuePair("type", "enclosure_selection"),
                    new AttributeValuePair("selection", enclosureSelected),
                    new AttributeValuePair("service_name", service.GetAttribute("name"))
                });

        }

        void RemoveEnclosurePanel()
        {
            if (enclosurePanel != null)
            {
                enclosurePanel.EnclosureButtonClicked -= enclosurePanel_EnclosureButtonClicked;
                enclosurePanel.Dispose();
                Controls.Remove(enclosurePanel);
            }
        }


    }
}
