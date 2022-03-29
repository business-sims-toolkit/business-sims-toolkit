using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using CommonGUI;
using CoreUtils;
using DevOps.OpsEngine;
using DevOps.OpsScreen.ServiceDevelopmentUi;
using Network;
using ResizingUi.Button;

// ReSharper disable ObjectCreationAsStatement

namespace DevOps.OpsScreen.FacilitatorControls.AppDevelopmentUi
{
    
    internal class DeploymentStagePanel : FlickerFreePanel, ILinkedStage
    {
        enum ButtonState
        {
            Enqueue,
            Install,
            Disabled
        }

        ButtonState buttonState;
        
        public DeploymentStagePanel (ILinkedStage precedingStage, Node service, Node servicesCommandQueueNode, RequestsManager requestsManager)
        {
            PrecedingStage = precedingStage;
            PrecedingStage.StageStatusChanged += precedingStage_StageCompletionStateChanged;
            
            Visible = PrecedingStage.HasCompletedStage;

            FinalStage = null;

            this.service = service;
            service.AttributesChanged += service_AttributesChanged;

            this.servicesCommandQueueNode = servicesCommandQueueNode;
            
            titleLabel = new Label
            {
                Text = "Install",
                Font = SkinningDefs.TheInstance.GetFont(12, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft
            };
            Controls.Add(titleLabel);

            deploymentStageStatusAttribute = "deployment_stage_status";

            enclosurePanel = new EnclosurePanel(service, servicesCommandQueueNode, requestsManager)
            {
                BackColor = SkinningDefs.TheInstance.GetColorData("facilitator_development_stages_back_colour", Color.HotPink)
            };
            Controls.Add(enclosurePanel);
            enclosurePanel.Enabled = ! HasCompletedStage;
            
            var controlButtonSize = new Size(100, 30); // TODO skin file
            
            queueInstallButton = new StyledDynamicButton("standard", "Install")
            {
                Size = controlButtonSize,
                Font = SkinningDefs.TheInstance.GetFont(12),
                Enabled = false
            };
            Controls.Add(queueInstallButton);
            queueInstallButton.Click += installButton_Click;

            SetButtonState();

            backButton = new StyledDynamicButton("standard", "Back")
            {
                Size = controlButtonSize,
                Font = SkinningDefs.TheInstance.GetFont(12)
            };
            Controls.Add(backButton);
            backButton.Click += backButton_Click;

        }


        public bool HasCompletedStage => service.GetAttribute(deploymentStageStatusAttribute) == ServiceStageStatus.Completed;
        public bool HasFailedStage => service.GetAttribute(deploymentStageStatusAttribute) == ServiceStageStatus.Failed;
        public bool IsStageIncomplete => service.GetAttribute(deploymentStageStatusAttribute) == ServiceStageStatus.Incomplete;
        public ILinkedStage PrecedingStage { get; }
        public ILinkedStage FinalStage { get; }
        
        public event EventHandler StageStatusChanged;
        
        void SetButtonState ()
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

            var queueInstallText = buttonState == ButtonState.Enqueue ? "Enqueue" : "Install";
            queueInstallButton.Text = queueInstallText;
        }

        protected override void OnSizeChanged (EventArgs e)
        {
            DoSize();
        }

        protected override void Dispose (bool disposing)
        {
            if (disposing)
            {
                if (PrecedingStage != null)
                {
                    PrecedingStage.StageStatusChanged -= precedingStage_StageCompletionStateChanged;
                }

                if (FinalStage != null)
                {
                    FinalStage.StageStatusChanged -= finalStage_StageCompletionStateChanged;
                }

                service.AttributesChanged -= service_AttributesChanged;
            }

            base.Dispose(disposing);
        }

        void DoSize ()
        {

            var widthPadding = SkinningDefs.TheInstance.GetIntData("app_development_width_padding", 15);
            var heightPadding = SkinningDefs.TheInstance.GetIntData("app_development_height_padding", 5);

            titleLabel.Bounds = new Rectangle(widthPadding, 0, Width - 2 * widthPadding, 30);
            

            queueInstallButton.Location = new Point(Width - widthPadding - queueInstallButton.Width, Height - heightPadding - queueInstallButton.Height);
            backButton.Location = new Point(queueInstallButton.Left - 10 - backButton.Width, queueInstallButton.Top);

            var enclosurePanelHeight = Math.Max(130, queueInstallButton.Top - 20 - (titleLabel.Bottom + 5));
            
            
            enclosurePanel.Bounds = new Rectangle(widthPadding, titleLabel.Bottom + heightPadding,
                Width - 2 * widthPadding, enclosurePanelHeight);
            

            Invalidate();
        }

        void UpdateEnclosures ()
        {
            enclosurePanel.UpdateEnclosures();
            
            enclosurePanel.Enabled = !HasCompletedStage;
        }

        protected override void OnVisibleChanged (EventArgs e)
        {
            if (Visible)
            {
                BringToFront();
                UpdateEnclosures();
            }
        }

        void precedingStage_StageCompletionStateChanged(object sender, EventArgs e)
        {
            Visible = PrecedingStage.HasCompletedStage;
        }

        void finalStage_StageCompletionStateChanged(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        void service_AttributesChanged(Node sender, System.Collections.ArrayList attrs)
        {
            UpdateEnclosures();
            
            var attributes = attrs.Cast<AttributeValuePair>().ToList();

            if (attributes.Any(avp => avp.Attribute == "status"))
            {
                SetButtonState();
            }

            if (attributes.Any(avp => avp.Attribute == deploymentStageStatusAttribute))
            {
                OnStageStatusChanged();
            }

            var canDeploy = sender.GetBooleanAttribute("can_deploy", false);

            queueInstallButton.Enabled = canDeploy;

        }



        void installButton_Click (object sender, EventArgs e)
        {
            var command = (buttonState == ButtonState.Enqueue) ? AppDevelopmentCommandType.EnqueueDeployment : AppDevelopmentCommandType.InstallService;

            new Node(servicesCommandQueueNode, command, "",
                new List<AttributeValuePair>
                {
                    new AttributeValuePair("type", command),
                    new AttributeValuePair("service_name", service.GetAttribute("name"))
                });

            if (buttonState == ButtonState.Enqueue)
            {
                Hide();
            }

            // TODO if Install ??? 
        }
        
        void backButton_Click (object sender, EventArgs e)
        {
            Hide();
        }


        void OnStageStatusChanged ()
        {
            StageStatusChanged?.Invoke(this, EventArgs.Empty);
        }
        
        readonly string deploymentStageStatusAttribute;

        readonly Node service;
        readonly Node servicesCommandQueueNode;
        
        readonly Label titleLabel;

        readonly EnclosurePanel enclosurePanel;

        readonly StyledDynamicButton queueInstallButton;
        readonly StyledDynamicButton backButton;

    }
}
