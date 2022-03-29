using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Drawing;

using CommonGUI;
using CoreUtils;
using DevOps.OpsEngine;
using DevOps.OpsScreen.ServiceDevelopmentUi;
using LibCore;
using Network;
using ResizingUi.Button;

// ReSharper disable ObjectCreationAsStatement

namespace DevOps.OpsScreen
{
    public class ServiceProgressPanel : FlickerFreePanel
	{
	    readonly NodeTree model;
	    readonly Node service;

	    readonly IDataEntryControlHolderWithShowPanel parentControl;

        Label titleLabel;
        
	    StageButtonGroupPanel devOne;
        StageButtonGroupPanel devTwo;
        
	    StageButtonGroupPanel testStage;
	    
	    StageButtonGroupPanel releaseStage;
	    
        ImageButton devResetButton;
        ImageButton testResetButton;
        ImageButton releaseResetButton;
        
	    InstallServicePanel installPanel;
	    
        readonly RequestsManager requestsManager;
        
	    ImageButton abortServiceButton;
	    ImageButton undoServiceButton;
	    ImageButton closeButton;

	    StyledDynamicButton enclosureButton;
        
	    ServiceProgressFeedbackPanel feedbackPanel;
        
	    Label failedLabel;

	    const int innerWidthPadding = 5;
	    int heightPadding = 5;

        int leftPadding = 15;
        int topPadding = 10;
        int rightPadding = 15;
        int bottomPadding = 15;

	    readonly int round;
        
	    Node testEnvironmentsNode;

	    readonly Node beginServicesCommandQueue;
	    readonly Node beginInstallNode;

	    bool canReset;

        public ServiceProgressPanel(NodeTree model, Node service, RequestsManager requestsManager, IDataEntryControlHolderWithShowPanel parent, int round)
        {
            parentControl = parent;

            this.model = model;
            this.service = service;
            this.requestsManager = requestsManager;
            this.round = round;

            beginServicesCommandQueue = model.GetNamedNode("BeginServicesCommandQueue");

	        beginInstallNode = model.GetNamedNode("BeginNewServicesInstall");
	        beginInstallNode.ChildAdded += beginInstallNode_ChildAdded;
	        beginInstallNode.ChildRemoved += beginInstallNode_ChildRemoved;
	        foreach (Node child in beginInstallNode.getChildren())
	        {
		        child.AttributesChanged += beginInstallNodeChild_AttributesChanged;
	        }
            
            Setup();

            canReset = this.service.GetAttribute("deployment_stage_status") == ServiceStageStatus.Incomplete;
        }

		void beginInstallNode_ChildAdded (Node parent, Node child)
		{
			child.AttributesChanged += beginInstallNodeChild_AttributesChanged;
			UpdateTestUsage();
		}

		void beginInstallNode_ChildRemoved(Node parent, Node child)
		{
			child.AttributesChanged -= beginInstallNodeChild_AttributesChanged;
			UpdateTestUsage();
		}

		void beginInstallNodeChild_AttributesChanged (Node sender, ArrayList attributes)
		{
			UpdateTestUsage();
		}

		void UpdateTestUsage ()
		{
		    if (testStage == null) return;

		    foreach (var button in testStage.Buttons)
		    {
		        if (button == null) continue;

		        var envDesc = (string)button.Tag;
		        var inUse = false;

		        foreach (Node newService in model.GetNamedNode("BeginNewServicesInstall").GetChildrenOfType("BeginNewServicesInstall"))
		        {
		            if (newService != service && 
		                (newService.GetAttribute("status") == ServiceStatus.Test || 
		                 newService.GetAttribute("status") == ServiceStatus.TestDelay) && 
		                newService.GetAttribute("test_environment_selection") == envDesc)
		            {
		                inUse = true;
		                break;
		            }
		        }

		        if (!envDesc.Contains("Virtual"))
		        {
		            button.Text = (inUse ? (envDesc + " IN USE") : envDesc);
		            button.Enabled = !inUse;
		        }
		    }
		}

	    void Setup ()
	    {
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
	        
            abortServiceButton = CreateTextButton(100, 30, "Abort");
	        abortServiceButton.Click += cancelServiceButton_Click;
	        Controls.Add(abortServiceButton);

	        undoServiceButton = CreateTextButton(100, 30, "Undo");
	        undoServiceButton.Click += undoServiceButton_Click;
	        Controls.Add(undoServiceButton);

	        closeButton = CreateTextButton(100, 30, "Close");
	        closeButton.Click += closeButton_Click;
	        Controls.Add(closeButton);

            feedbackPanel = new ServiceProgressFeedbackPanel(service);
	        Controls.Add(feedbackPanel);
            
	        failedLabel = new Label
	        {
	            Visible = false,
	            Size = new Size(482, 144),
	            Font = SkinningDefs.TheInstance.GetFont(50f, FontStyle.Bold),
	            TextAlign = ContentAlignment.MiddleCenter,
	            BackColor = Color.Transparent,
	            ForeColor = CONVERT.ParseHtmlColor("#ff2d4e")
	        };

            Controls.Add(failedLabel);
            failedLabel.BringToFront();

	        const int resetButtonWidth = 53;
	        const int resetButtonHeight = 30;
	        devResetButton = CreateTextButton(resetButtonWidth, resetButtonHeight, "Reset");
	        devResetButton.Click += devResetButton_Click;
	        Controls.Add(devResetButton);

	        testResetButton = CreateTextButton(resetButtonWidth, resetButtonHeight, "Reset");
	        testResetButton.Click += testResetButton_Click;
	        Controls.Add(testResetButton);
	        testResetButton.Visible = false;

	        releaseResetButton = CreateTextButton(resetButtonWidth, resetButtonHeight, "Reset");
	        releaseResetButton.Click += releaseResetButton_Click;
	        Controls.Add(releaseResetButton);
	        releaseResetButton.Visible = false;

            service.AttributesChanged += service_AttributesChanged;

	        testEnvironmentsNode = model.GetNamedNode("TestEnvironments");

	        testEnvironmentsNode.AttributesChanged += testEnvironments_AttributesChanged;

	        enclosureButton = CreateTextButton(100, 30, "Enclosures");
	        enclosureButton.Visible = false;
            Controls.Add(enclosureButton);
            enclosureButton.Click += enclosureButton_Click;

	    }

        void enclosureButton_Click(object sender, EventArgs e)
        {
            ShowInstallPanel();
        }

        void closeButton_Click(object sender, EventArgs e)
        {
            parentControl.DisposeEntryPanel();
        }

        void AddResetToStageCommand(string targetStatus)
        {
            new Node(beginServicesCommandQueue, "reset_stage", "",
                new List<AttributeValuePair>
                {
                    new AttributeValuePair("type", "reset_stage"),
                    new AttributeValuePair("target_status", targetStatus),
                    new AttributeValuePair("service_name", service.GetAttribute("name"))
                });

            UpdateStages();
        }
        void devResetButton_Click(object sender, EventArgs e)
        {
            AddResetToStageCommand(ServiceStatus.Dev);
            
        }

        void testResetButton_Click(object sender, EventArgs e)
        {

            AddResetToStageCommand(ServiceStatus.Test);
            
        }

        void releaseResetButton_Click(object sender, EventArgs e)
        {

            AddResetToStageCommand(ServiceStatus.Release);

        }

        void ShowDevStagePanel(bool isEnabled)
        {
            if (devOne == null || devTwo == null)
            {
                CreateDevStagePanel(isEnabled);
            }

            devOne.UpdateOptions();
            devTwo.UpdateOptions();

            devOne.EnableButtons(isEnabled);
            devTwo.EnableButtons(isEnabled);

            devOne.Visible = devTwo.Visible = true;
        }

        void CreateDevStagePanel(bool isEnabled)
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
            

            var panelSize = new Size(420, 30);

            var devOneTitle = (isThirdParty) ? "Vendor" : "Dev 1";
            var devOneCommands = new List<string>
            {
                AppDevelopmentCommandType.DevOneSelection
            };

            if (isThirdParty)
            {
                devOneCommands.Add(AppDevelopmentCommandType.DevTwoSelection);
            }
            
            bool HasPassedStageFunc () => service.GetAttribute("dev_stage_status") == ServiceStageStatus.Completed;

            devOne = new StageButtonGroupPanel(
                new StageGroupProperties
                {
                    Title = devOneTitle,
                    CommandTypes = devOneCommands,
                    GetOptions = getDevOptionsFunc,
                    GetCorrectOption = serviceNode => serviceNode.GetAttribute("dev_one_environment"),
                    GetCurrentSelection = serviceNode => serviceNode.GetAttribute("dev_one_selection"),
                    ButtonFlowDirection = FlowDirection.LeftToRight,
                    WrapContents = true

                }, HasPassedStageFunc, service, beginServicesCommandQueue)
            {
                Size = panelSize,
                Location = new Point(leftPadding, titleLabel.Bottom + 40)
            };
            
            
            devOne.EnableButtons(isEnabled);
            
            Controls.Add(devOne);
            devOne.Visible = true;
            // If it's not a third party service then create DevTwo
            if (!isThirdParty)
            {
                devTwo = new StageButtonGroupPanel(
                    new StageGroupProperties
                    {
                        Title = "Dev 2",
                        CommandTypes = new List<string>{ AppDevelopmentCommandType.DevTwoSelection },
                        GetOptions = getDevOptionsFunc,
                        GetCorrectOption = serviceNode => serviceNode.GetAttribute("dev_two_environment"),
                        GetCurrentSelection = serviceNode => serviceNode.GetAttribute("dev_two_selection"),
                        ButtonFlowDirection = FlowDirection.LeftToRight,
                        WrapContents = true
                    }, HasPassedStageFunc, service, beginServicesCommandQueue)
                {
                    Size = panelSize,
                    Location = new Point(leftPadding, devOne.Bottom + innerWidthPadding)
                };
                
                devTwo.EnableButtons(isEnabled);

                Controls.Add(devTwo);
                devTwo.Visible = true;
            }
            
            
            var resetTop = (isThirdParty) ? devOne.Top : devTwo.Top;
            devResetButton.Location = new Point(Width - rightPadding - devResetButton.Width, resetTop);
            devResetButton.Visible = true;
            devResetButton.Enabled = canReset && service.GetAttribute("dev_stage_status") != ServiceStageStatus.Incomplete;
        }
        
        

        
        void ShowTestStagePanel(bool isEnabled)
        {

            if (testStage == null)
            {
                CreateTestStagePanel(isEnabled);
            }

            testStage.UpdateOptions();
            testStage.Show();
        }
        
        void CreateTestStagePanel(bool isEnabled)
        {
            // There are three test environments, labelled as 
            // 1, 2, or 3
            
            var isVirtualTestingEnabled =
                model.GetNamedNode("TestEnvironments").GetBooleanAttribute("virtual_test_enabled", false);


            Func<List<ButtonTextTags>> getOptionsFunc;
            Func<Node, string> getCorrectOption;

            string GetCurrentSelectionFunc (Node serviceNode) => serviceNode.GetAttribute("test_environment_selection");

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
                        .Where(e => !e.GetAttribute("desc").Contains("Virtual")).Select(e =>
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

            var testTop = devTwo?.Bottom + 20 ?? devOne.Bottom + devOne.Height + innerWidthPadding;

            testStage = new StageButtonGroupPanel(
                new StageGroupProperties
                {
                    Title = "Test",
                    CommandTypes = new List<string>{AppDevelopmentCommandType.TestEnvironmentSelection},
                    GetCurrentSelection = GetCurrentSelectionFunc,
                    GetCorrectOption = getCorrectOption,
                    GetOptions = getOptionsFunc,
                    ButtonFlowDirection = FlowDirection.LeftToRight,
                    WrapContents = true
                }, 
                () => service.GetAttribute("test_stage_status") == ServiceStageStatus.Completed, service, beginServicesCommandQueue)
            {
                Size = new Size(305, 30),
                Location = new Point(leftPadding, testTop + 20)
            };

            
            testStage.EnableButtons(isEnabled);
            Controls.Add(testStage);
            testStage.Visible = testResetButton.Visible = true;
            testResetButton.Location = new Point(devResetButton.Left, testStage.Top);
            testResetButton.Enabled = canReset && service.GetAttribute("test_stage_status") != ServiceStageStatus.Incomplete;
        }
        
        void ShowReleaseStagePanel(bool isEnabled)
        {
            if (releaseStage == null)
            {
                CreateReleaseStagePanel(isEnabled);
            }

            releaseStage.UpdateOptions();
            releaseStage.Show();
        }

	    void CreateReleaseStagePanel (bool isEnabled)
	    {
	        List<ButtonTextTags> GetOptionsFunc ()
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
            
	        releaseStage = new StageButtonGroupPanel(
	                new StageGroupProperties
	                {
	                    Title = "Release",
	                    CommandTypes = new List<string> { AppDevelopmentCommandType.ReleaseSelection },
                        GetCurrentSelection = serviceNode => serviceNode.GetAttribute("release_selection"),
	                    GetCorrectOption = serviceNode => serviceNode.GetAttribute("release_code"),
	                    GetOptions = GetOptionsFunc,
	                    ButtonFlowDirection = FlowDirection.LeftToRight,
	                    WrapContents = true
                    }, 
	                () => service.GetAttribute("release_stage_status") == ServiceStageStatus.Completed, service, beginServicesCommandQueue)
	        {
	            Size = new Size(245, 30),
	            Location = new Point(leftPadding, testStage.Bottom + 20)
	        };

	        releaseStage.OptionSelected += releaseStage_OptionSelected;
            releaseStage.EnableButtons(isEnabled);
	        Controls.Add(releaseStage);
            
            releaseResetButton.Location = new Point(devResetButton.Left, releaseStage.Top);
            releaseStage.Visible = releaseResetButton.Visible = true;

            enclosureButton.Location = new Point(releaseResetButton.Left - 20 - enclosureButton.Width, releaseResetButton.Top);
	        enclosureButton.Visible = service.GetAttribute("release_stage_status") == "completed";

	        releaseResetButton.Enabled = canReset && !service.GetBooleanAttribute("deployment_enqueued", false) && service.GetAttribute("release_stage_status") != ServiceStageStatus.Incomplete;
        }

        void releaseStage_OptionSelected(object sender, EventArgs e)
        {
            //releaseOption = releaseStage.SelectedOption;

            //AddSelectionCommand("release_selection", releaseOption);
            
            //if (service.GetAttribute("release_stage_status") == ServiceStageStatus.Completed)
            //{
            //    ShowInstallPanel();
            //}
        }
        
        void undoServiceButton_Click(object sender, EventArgs e)
        {
            const string message = "Are you sure you want to undo this service?\r\nThis should only be used if the service was started accidentally.";
            const string caption = "Undo Service";
            const MessageBoxButtons buttons = MessageBoxButtons.YesNo;

            var result = MessageBox.Show(this, message, caption, buttons);

            if (result == DialogResult.Yes)
            {
                AddCancelledOrUndoCommand("undo_service");
                parentControl.DisposeEntryPanel();
            }
        }

        void cancelServiceButton_Click(object sender, EventArgs e)
        {
            const string message = "Are you sure you want to cancel the development of this service?";
            const string caption = "Cancel Service Development";
            const MessageBoxButtons buttons = MessageBoxButtons.YesNo;

            var result = MessageBox.Show(this, message, caption, buttons);

            if (result == DialogResult.Yes)
            {
                AddCancelledOrUndoCommand("cancel_service");
                parentControl.DisposeEntryPanel();
            }
            
        }

        void AddCancelledOrUndoCommand(string type)
        {
            new Node(beginServicesCommandQueue, type, "", 
                new List<AttributeValuePair>
                {
                    new AttributeValuePair("type", type),
                    new AttributeValuePair("service_name", service.GetAttribute("name"))
                });
        }

	    static StyledDynamicButton CreateTextButton(int buttonWidth, int buttonHeight, string text)
        {
            var button = new StyledDynamicButton("standard", text)
            {
                Size = new Size(buttonWidth, buttonHeight),
                Font = SkinningDefs.TheInstance.GetPixelSizedFont(12, FontStyle.Bold)
            };

            return button;
        }

        void testEnvironments_AttributesChanged(Node sender, ArrayList attrs)
        {
            foreach(AttributeValuePair attr in attrs)
            {
                if (attr.Attribute.Equals("virtual_test_enabled"))
                {
                    UpdateStages();
                }
            }
        }

	    string previousStatus = "";

        void service_AttributesChanged(Node sender, ArrayList attrs)
        {

            foreach (AttributeValuePair avp in attrs)
            {
                // ReSharper disable once SwitchStatementMissingSomeCases
                switch (avp.Attribute)
                {
                    case "status":
                    {
                        if (avp.Value == ServiceStatus.Cancelled || avp.Value == ServiceStatus.Undo)
                        {
                            return;
                        }

                        var status = sender.GetAttribute("status");
                        UpdateStages();
                        if (!status.Equals(previousStatus))
                        {
                            previousStatus = status;
                        }
                    }
                        break;

                    case "feedback_message":
                        UpdateStages();
                        break;

                    case "dev_stage_status":
                        if (avp.Value == ServiceStageStatus.Failed)
                        {
                            ShowIntegrationFailureMessage();
                        }

                        devResetButton.Enabled =
                            service.GetAttribute("dev_stage_status") != ServiceStageStatus.Incomplete;

                        break;

                    case "test_stage_status":

                        testResetButton.Enabled =
                            service.GetAttribute("test_stage_status") != ServiceStageStatus.Incomplete;
                        break;

                    case "release_stage_status":
                        if (avp.Value == ServiceStageStatus.Failed)
                        {
                            ShowReleaseFailureMessage();
                        }

                        releaseResetButton.Enabled =
                            service.GetAttribute("release_stage_status") != ServiceStageStatus.Incomplete;

                        break;
                    case "deployment_stage_status":
                        canReset = service.GetAttribute("deployment_stage_status") == ServiceStageStatus.Incomplete;
                        if (avp.Value == ServiceStageStatus.Failed || avp.Value == ServiceStageStatus.Completed)
                        {
                            ShowInstallPanel();
                        }
                        break;
                    case "deployment_enqueued":
                        releaseResetButton.Enabled = !service.GetBooleanAttribute("deployment_enqueued", false);
                        break;
                }
                
            }
        }

        void ShowIntegrationFailureMessage()
        {
            SetVisibilityForStages(false);

            failedLabel.Text = "Failed to Integrate";
            failedLabel.Visible = true;
        }

        void ShowReleaseFailureMessage()
        {
            SetVisibilityForStages(false);

            failedLabel.Text = "Failed to Release";
            failedLabel.Visible = true;
        }
        
        void SetVisibilityForStages (bool isVisible)
	    {
	        foreach (var stage in Controls.OfType<StageButtonGroupPanel>())
	        {
	            stage.Visible = isVisible;
	        }

	        devResetButton.Visible = isVisible;
            testResetButton.Visible = isVisible;
	        releaseResetButton.Visible = isVisible;
        }
        
        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);

            DoLayout();
        }

        void DoLayout()
        {
            titleLabel.Size = new Size(Width - leftPadding - rightPadding, 25);

            closeButton.Location = new Point(Width - rightPadding - closeButton.Width, Height - closeButton.Height - bottomPadding);

            abortServiceButton.Location = new Point(closeButton.Left - abortServiceButton.Width - 20, closeButton.Top);

            undoServiceButton.Location = new Point(abortServiceButton.Left - undoServiceButton.Width - innerWidthPadding, closeButton.Top);

            abortServiceButton.Enabled = undoServiceButton.Enabled = (service.GetAttribute("status") != ServiceStatus.Live);

            feedbackPanel.Size = new Size(Width, 60); // TODO
            feedbackPanel.Location = new Point(0, closeButton.Top - 10 - feedbackPanel.Height); //TODO
            
            UpdateStages();
            
            var failedLabelWidth = Width - 2 * leftPadding;
            //TODO
            var failedLabelHeight = Math.Max((closeButton.Top - 100) - (titleLabel.Bottom + 40), 140);

            failedLabel.Size = new Size(failedLabelWidth, failedLabelHeight);
            failedLabel.Location = new Point(leftPadding, titleLabel.Bottom + 40);

            if (installPanel != null && installPanel.Visible)
            {
                ShowInstallPanel();
            }
        }
        
	    void UpdateStages ()
	    {
	        var devStageStatus = service.GetAttribute("dev_stage_status");

            ShowDevStagePanel(devStageStatus != ServiceStageStatus.Completed);

	        if (devStageStatus == ServiceStageStatus.Incomplete || devStageStatus == ServiceStageStatus.Failed)
	        {
	            testStage?.Hide();
	            releaseStage?.Hide();

	            return;
	        }

	        var testStageStatus = service.GetAttribute("test_stage_status");
            ShowTestStagePanel(testStageStatus != ServiceStageStatus.Completed);

	        var releaseStageStatus = service.GetAttribute("release_stage_status");
            ShowReleaseStagePanel(releaseStageStatus != ServiceStageStatus.Completed);

            //TODO
            if (service.GetAttribute("release_stage_status") == ServiceStageStatus.Completed || service.GetAttribute("deployment_stage_status") == ServiceStageStatus.Failed)
	        {
                ShowInstallPanel();
	        }
	    }

        void ShowInstallPanel()
        {
            if (installPanel != null)
            {
                installPanel.InstallClicked -= installPanel_InstallClicked;
                installPanel.Dispose();
                Controls.Remove(installPanel);
            }

            var installPanelWidth = Width;
            var installPanelHeight = feedbackPanel.Top - (titleLabel.Bottom + heightPadding);
            
            var serviceStatus = service.GetAttribute("status");
            var hasAttemptedInstall = service.GetBooleanAttribute("is_install_successful");
            var isDeployable = !(serviceStatus == ServiceStatus.Installing || serviceStatus == ServiceStatus.Live) || !hasAttemptedInstall.HasValue;
            

            installPanel = new InstallServicePanel(service, requestsManager, model, isDeployable)
            {
                Location = new Point(0, titleLabel.Bottom + heightPadding),
                Size = new Size(installPanelWidth, installPanelHeight)
            };

            installPanel.Show();

            Controls.Add(installPanel);
            installPanel.BringToFront();


            installPanel.InstallClicked += installPanel_InstallClicked;
            
        }

        void installPanel_InstallClicked(object sender, EventArgs e)
        {
            abortServiceButton.Enabled = !service.GetBooleanAttribute("is_install_successful", true);

            undoServiceButton.Enabled = false;
        }
        
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                service.AttributesChanged -= service_AttributesChanged;

                service.SetAttribute("has_passed_stage", true);

	            beginInstallNode.ChildAdded -= beginInstallNode_ChildAdded;
	            beginInstallNode.ChildRemoved -= beginInstallNode_ChildRemoved;
	            foreach (Node child in beginInstallNode.getChildren())
	            {
		            child.AttributesChanged -= beginInstallNodeChild_AttributesChanged;
	            }
                

                if (releaseStage != null)
                {
                    releaseStage.OptionSelected -= releaseStage_OptionSelected;
                    releaseStage.Dispose();
                }



            }

            base.Dispose(disposing);
        }
	}
}