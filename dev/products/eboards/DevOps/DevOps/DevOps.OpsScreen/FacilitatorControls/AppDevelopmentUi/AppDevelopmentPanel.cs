using System;
using System.Collections;
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
    internal class AppDevelopmentPanel : FlickerFreePanel
    {
        public AppDevelopmentPanel (NodeTree model, Node service, RequestsManager requestsManager, 
                                    int round, IDataEntryControlHolderWithShowPanel controlContainer, DevelopingAppTerminator appTerminator)
        {
            this.model = model;
            this.controlContainer = controlContainer;

            this.appTerminator = appTerminator;

            this.service = service;
            service.AttributesChanged += service_AttributesChanged;

            servicesCommandQueueNode = model.GetNamedNode("BeginServicesCommandQueue");
            
            this.round = round;
            
            var serviceName = service.GetAttribute("biz_service_function");
            var serviceId = service.GetAttribute("service_id");
            var productId = service.GetAttribute("product_id");
            titleLabel = new Label
            {
                Text = $"{serviceName.Replace(@"&", @"&&")} - {serviceId} - {productId}",
                Font = SkinningDefs.TheInstance.GetFont(12, FontStyle.Bold),
                Location = new Point(leftPadding, topPadding),
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Size = new Size(Width, 25)
            };
            Controls.Add(titleLabel);

            abortButton = CreateTextButton("Abort");
            Controls.Add(abortButton);
            abortButton.Click += abortButton_Click;


            undoButton = CreateTextButton("Undo");
            Controls.Add(undoButton);
            undoButton.Click += undoButton_Click;

            closeButton = CreateTextButton("Close");
            Controls.Add(closeButton);
            closeButton.Click += closeButton_Click;

            enclosureButton = CreateTextButton("Enclosures");
            Controls.Add(enclosureButton);
            
            enclosureButton.Click += enclosureButton_Click;

            feedbackPanel = new ServiceProgressFeedbackPanel(service);
            Controls.Add(feedbackPanel);

            failureMessagePanel = new StageFailurePanel();
            Controls.Add(failureMessagePanel);
            

            devStageGroupPanel = CreateDevStage();
            Controls.Add(devStageGroupPanel);

            failureMessagePanel.AddStageFailureMessage(devStageGroupPanel, AppDevelopmentStageFailureMessage.DevelopmentStage);

            testStageGroupPanel = CreateTestStage();
            Controls.Add(testStageGroupPanel);

            releaseStageGroupPanel = CreateReleaseStage();
            Controls.Add(releaseStageGroupPanel);
            releaseStageGroupPanel.StageStatusChanged += releaseStagePanel_StageStatusChanged;

            failureMessagePanel.AddStageFailureMessage(releaseStageGroupPanel, AppDevelopmentStageFailureMessage.ReleaseStage);

            deploymentStagePanel = new DeploymentStagePanel(releaseStageGroupPanel, service, servicesCommandQueueNode, requestsManager)
            {
                BackColor = SkinningDefs.TheInstance.GetColorData("facilitator_app_development_back_colour", Color.HotPink)
            };
            Controls.Add(deploymentStagePanel);
            deploymentStagePanel.StageStatusChanged += deploymentStagePanel_StageStatusChanged;
            if (deploymentStagePanel.Visible)
            {
                deploymentStagePanel.BringToFront();
            }

            devStageGroupPanel.FinalStage = testStageGroupPanel.FinalStage =
                releaseStageGroupPanel.FinalStage = deploymentStagePanel;

            failureMessagePanel.AddStageFailureMessage(deploymentStagePanel, AppDevelopmentStageFailureMessage.DeploymentStage);

            enclosureButton.Visible = releaseStageGroupPanel.HasCompletedStage;
        }

        public event EventHandler AppDevelopmentAborted;

        void OnAppDevelopmentAborted ()
        {
            AppDevelopmentAborted?.Invoke(this, EventArgs.Empty);
        }


        void releaseStagePanel_StageStatusChanged (object sender, EventArgs e)
        {
            enclosureButton.Visible = releaseStageGroupPanel.HasCompletedStage;
        }

        void deploymentStagePanel_StageStatusChanged(object sender, EventArgs e)
        {
            abortButton.Enabled = ! deploymentStagePanel.HasCompletedStage;
            undoButton.Enabled = false;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                service.AttributesChanged -= service_AttributesChanged;
            }

            base.Dispose(disposing);
        }

        protected override void OnSizeChanged (EventArgs e)
        {
            DoSize();
        }

        void DoSize ()
        {
            titleLabel.Size = new Size(Width - leftPadding - rightPadding, 25);
            
            closeButton.Location = new Point(Width - rightPadding - closeButton.Width, Height - closeButton.Height - bottomPadding);

            abortButton.Location = new Point(closeButton.Left - abortButton.Width - 20, closeButton.Top);

            undoButton.Location = new Point(abortButton.Left - undoButton.Width - 5, closeButton.Top);

            feedbackPanel.Size = new Size(Width, 40); // TODO
            feedbackPanel.Location = new Point(0, closeButton.Top - 10 - feedbackPanel.Height); //TODO
            feedbackPanel.BackColor = Color.Transparent;

            var y = titleLabel.Bottom + 10;

            foreach (var groupPanel in new []
            {
                devStageGroupPanel,
                testStageGroupPanel,
                releaseStageGroupPanel
            })
            {
                groupPanel.Bounds = new Rectangle(leftPadding, y, Width - 2 * leftPadding, groupPanel.PreferredHeight);

                y = groupPanel.Bottom + 10;
            }

            enclosureButton.Location = new Point(releaseStageGroupPanel.Right - 5 - enclosureButton.Width, releaseStageGroupPanel.Bottom + 10);

            var popupBounds = new Rectangle(0, titleLabel.Bottom + 10, Width, feedbackPanel.Top - 10 - (titleLabel.Bottom + 10));

            failureMessagePanel.Bounds = popupBounds;

            deploymentStagePanel.Bounds = popupBounds;
            
            Invalidate();
        }

        static StyledDynamicButton CreateTextButton (string text)
        {
            return new StyledDynamicButton("standard", text)
            {
                Size = new Size(100, 30),
                Font = SkinningDefs.TheInstance.GetPixelSizedFont(12, FontStyle.Bold)
            };
        }

        

        StageGroupPanel CreateDevStage ()
        {
            var serviceClassification = service.GetAttribute("data_security_classification");
            var isThirdParty = service.GetBooleanAttribute("is_third_party", false);

            Func<List<ButtonTextTags>> getDevOptionsFunc;

            if (isThirdParty)
            {
                getDevOptionsFunc = () =>
                {
                    return model.GetNamedNode("VendorBuildEnvironments").GetChildrenAsList()
                        .Select(e => e.GetAttribute("desc")).Select(e => new ButtonTextTags
                        {
                            ButtonId = e,
                            ButtonText = e,
                            ButtonTag = e,
                            IsEnabled = true
                        }).ToList();
                };
            }
            else
            {
                getDevOptionsFunc = () =>
                {
                    return model.GetNamedNode("DevBuildEnvironments")
                        .GetChildWithAttributeValue("data_security_level", serviceClassification)
                        .GetChildrenAsList()
                        .Select(e => e.GetAttribute("desc")).Select(e => new ButtonTextTags
                        {
                            ButtonId = e,
                            ButtonText = e,
                            ButtonTag = e,
                            IsEnabled = true
                        }).ToList();
                };
            }
            
            var devOneTitle = (isThirdParty) ? "Vendor" : "Dev 1";
            var devOneCommands = new List<string>
            {
                AppDevelopmentCommandType.DevOneSelection
            };

            if (isThirdParty)
            {
                devOneCommands.Add(AppDevelopmentCommandType.DevTwoSelection);
            }
            
            var devPropertiesGroups = new List<StageGroupProperties>
            {
                new StageGroupProperties
                {
                    Title = devOneTitle,
                    CommandTypes = devOneCommands,
                    GetOptions = getDevOptionsFunc,
                    GetCorrectOption = serviceNode => serviceNode.GetAttribute("dev_one_environment"),
                    GetCurrentSelection = serviceNode => serviceNode.GetAttribute("dev_one_selection"),
                    ButtonFlowDirection = FlowDirection.LeftToRight,
                    WrapContents = true
                }
            };

            if (! isThirdParty)
            {
                devPropertiesGroups.Add(new StageGroupProperties
                {
                    Title = "Dev 2",
                    CommandTypes = new List<string> { AppDevelopmentCommandType.DevTwoSelection },
                    GetOptions = getDevOptionsFunc,
                    GetCorrectOption = serviceNode => serviceNode.GetAttribute("dev_two_environment"),
                    GetCurrentSelection = serviceNode => serviceNode.GetAttribute("dev_two_selection"),
                    ButtonFlowDirection = FlowDirection.LeftToRight,
                    WrapContents = true
                });
            }
            
            return new StageGroupPanel(devPropertiesGroups, serviceNode => true, service, servicesCommandQueueNode, new Size(420, 30), ServiceStatus.Dev, "dev_stage_status", null)
            {
                BackColor = SkinningDefs.TheInstance.GetColorData("facilitator_development_stages_back_colour", Color.HotPink)
            };
        }
        
        StageGroupPanel CreateTestStage ()
        {

            var isVirtualTestingEnabled =
                model.GetNamedNode("TestEnvironments").GetBooleanAttribute("virtual_test_enabled", false);


            Func<List<ButtonTextTags>> getOptionsFunc;
            Func<Node, string> getCorrectOption;

            string GetCurrentSelectionFunc(Node serviceNode) => serviceNode.GetAttribute("test_environment_selection");

            if (isVirtualTestingEnabled)
            {
                getCorrectOption = serviceNode => "Virtual Test";

                getOptionsFunc = () => new List<ButtonTextTags>
                {
                    new ButtonTextTags
                    {
                        ButtonId = "Virtual Test",
                        ButtonText = "Test",
                        ButtonTag = "Virtual Test",
                        IsEnabled = true
                    },
                    new ButtonTextTags
                    {
                        ButtonId = "Bypass",
                        ButtonText = "Bypass",
                        ButtonTag = "Bypass",
                        IsEnabled = true
                    }
                };

            }
            else
            {
                getOptionsFunc = () =>
                {
                    return model.GetNamedNode("TestEnvironments").GetChildrenAsList()
                        .Where(e => ! e.GetAttribute("desc").Contains("Virtual")).Select(e =>
                        {
                            var testEnvironmentName = e.GetAttribute("desc");
                            var inUse = e.GetBooleanAttribute("in_use", false);
                            return new ButtonTextTags
                            {
                                ButtonId = testEnvironmentName,
                                ButtonText = $"{testEnvironmentName}{(inUse ? " IN USE" : "")}",
                                ButtonTag = testEnvironmentName,
                                IsEnabled = !inUse
                            };
                        }).ToList();
                };

                getCorrectOption = serviceNode => serviceNode.GetAttribute("test_environment");
                
            }


            return new StageGroupPanel(
                new List<StageGroupProperties>
                {
                    new StageGroupProperties
                    {
                        Title = "Test",
                        CommandTypes = new List<string> { AppDevelopmentCommandType.TestEnvironmentSelection },
                        GetCurrentSelection = GetCurrentSelectionFunc,
                        GetCorrectOption = getCorrectOption,
                        GetOptions = getOptionsFunc,
                        ButtonFlowDirection = FlowDirection.LeftToRight,
                        WrapContents = true
                    }
                },
                serviceNode => true, service, servicesCommandQueueNode, new Size(305, 30),
                ServiceStatus.Test, "test_stage_status", devStageGroupPanel, model.GetNamedNode("TestEnvironments").GetChildrenWithAttributeValue("type", "TestEnvironment"))
            {
                BackColor = SkinningDefs.TheInstance.GetColorData("facilitator_development_stages_back_colour", Color.HotPink)
            };
        }
        
        StageGroupPanel CreateReleaseStage ()
        {

            List<ButtonTextTags> GetOptionsFunc()
            {
                return model.GetNamedNode("ReleaseCodes")
                    .GetChildWithAttributeValue("round", round.ToString())
                    .GetChildrenWithAttributeValue("type", "ReleaseCode")
                    .Select(n => n.GetAttribute("desc"))
                    .Select(r => new ButtonTextTags
                    {
                        ButtonId = r,
                        ButtonText = r,
                        ButtonTag = r,
                        IsEnabled = true
                    })
                    .ToList();
            }

            return new StageGroupPanel(
                new List<StageGroupProperties>
                {
                    new StageGroupProperties
                    {
                        Title = "Release",
                        CommandTypes = new List<string> { AppDevelopmentCommandType.ReleaseSelection },
                        GetCurrentSelection = serviceNode => serviceNode.GetAttribute("release_selection"),
                        GetCorrectOption = serviceNode => serviceNode.GetAttribute("release_code"),
                        GetOptions = GetOptionsFunc,
                        ButtonFlowDirection = FlowDirection.LeftToRight,
                        WrapContents = true
                    }
                }, serviceNode => !serviceNode.GetBooleanAttribute("deployment_enqueued", false),
                service, servicesCommandQueueNode, new Size(245, 30), 
                ServiceStatus.Release, "release_stage_status", devStageGroupPanel)
            {
                BackColor = SkinningDefs.TheInstance.GetColorData("facilitator_development_stages_back_colour", Color.HotPink)
            };
        }
        
        void abortButton_Click (object sender, EventArgs e)
        {
            const string message = "Are you sure you want to cancel the development of this service?";
            const string caption = "Cancel Service";

            ShowConfirmationPopup(caption, message, AppDevelopmentCommandType.CancelService);
            
        }

        void undoButton_Click (object sender, EventArgs e)
        {
            const string message = "Are you sure you want to undo this service?\r\nThis should only be used if the service was started accidentally.";
            const string caption = "Undo Service";

            ShowConfirmationPopup(caption, message, AppDevelopmentCommandType.UndoService);
        }

        void ShowConfirmationPopup (string caption, string message, string command)
        {
            const MessageBoxButtons buttons = MessageBoxButtons.YesNo;

            var result = MessageBox.Show(this, message, caption, buttons);

            if (result == DialogResult.Yes)
            {
                OnAppDevelopmentAborted();
                appTerminator.TerminateApp(service, command);
                controlContainer.DisposeEntryPanel();
            }
        }

        void closeButton_Click (object sender, EventArgs e)
        {
            controlContainer.DisposeEntryPanel();
        }

        void enclosureButton_Click (object sender, EventArgs e)
        {
            deploymentStagePanel.Show();
        }

        void service_AttributesChanged (object sender, ArrayList attrs)
        {
            
        }

        readonly IDataEntryControlHolderWithShowPanel controlContainer;

        readonly NodeTree model;
        readonly Node service;
        readonly Node servicesCommandQueueNode;
        readonly int round;

        readonly DevelopingAppTerminator appTerminator;

        readonly Label titleLabel;
        readonly StyledDynamicButton undoButton;
        readonly StyledDynamicButton abortButton;
        readonly StyledDynamicButton closeButton;

        readonly StyledDynamicButton enclosureButton;

        readonly ServiceProgressFeedbackPanel feedbackPanel;
        //readonly Label failedLabel;

        readonly StageFailurePanel failureMessagePanel;

        readonly StageGroupPanel devStageGroupPanel;
        readonly StageGroupPanel testStageGroupPanel;
        readonly StageGroupPanel releaseStageGroupPanel;

        readonly DeploymentStagePanel deploymentStagePanel;

        const int leftPadding = 15;
        const int topPadding = 10;
        const int rightPadding = 15;
        const int bottomPadding = 15;
        
        
    }
}
