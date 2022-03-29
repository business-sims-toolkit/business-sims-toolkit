using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Algorithms;
using CommonGUI;
using DevOpsEngine.Interfaces;
using DevOpsEngine.StringConstants;
using DevOpsUi.FeatureDevelopment;
using Network;
using ResizingUi.Button;

namespace DevOpsUi.FacilitatorControls.FeatureDevelopment.Agile
{
	class AgileDeploymentStagePanel : FlickerFreePanel, ILinkedStage
	{
		public AgileDeploymentStagePanel(IRequestsManager requestsManager, ILinkedStage precedingStage, Node feature, Node commandQueueNode, bool isAlwaysHidden)
		{
			this.requestsManager = requestsManager;

			PrecedingStage = precedingStage;
			PrecedingStage.StageStatusChanged += precedingStage_StageStatusChanged;

			this.isAlwaysHidden = isAlwaysHidden;

			Visible = !isAlwaysHidden && PrecedingStage.HasCompletedStage;

			FinalStage = null;

			deploymentStageStatusAttribute = "deployment_stage_status";

			this.feature = feature;
			feature.AttributesChanged += feature_AttributesChanged;

			this.commandQueueNode = commandQueueNode;

			List<ButtonTextTags> GetOptionsFunc()
			{
				var enclosures = requestsManager.GetEnclosures(feature);

				return enclosures.Select(e => new ButtonTextTags
				{
					ButtonText = e.Name,
					ButtonId = e.Name,
					ButtonTag = e.Name,
					IsEnabled = e.IsEnabled
				})
					.ToList();
			}

			queueInstallButton = new StyledDynamicButton("standard", "Enqueue", true)
			{
				Enabled = false,
				CornerRadius = 4
			};

			Controls.Add(queueInstallButton);
			queueInstallButton.Click += queueInstallButton_Click;

			var stageGroupProperties = new StageGroupProperties
			{
				Title = "E:",
				CommandTypes = new List<string> { CommandTypes.EnclosureSelection },
				GetOptions = GetOptionsFunc,
				GetCorrectOption = featureNode => featureNode.GetAttribute("server"),
				GetCurrentSelection = featureNode => featureNode.GetAttribute("enclosure_selection"),
				ButtonFlowDirection = FlowDirection.LeftToRight,
				TitleAlignment = ContentAlignment.MiddleLeft,
				WrapContents = false
			};

			optionsPanel = new StageOptionsDropdownPanel(stageGroupProperties, () => HasCompletedStage, feature, commandQueueNode, false);
			Controls.Add(optionsPanel);

			optionsPanel.OptionSelected += optionsPanel_OptionSelected;

			SetButtonState();

			DoSize();
		}

		public override Size GetPreferredSize(Size size)
		{
			return new Size(size.Width, optionsPanel.Height);
		}

		public bool HasCompletedStage => feature.GetAttribute(deploymentStageStatusAttribute) == StageStatus.Completed;
		public bool HasFailedStage => feature.GetAttribute(deploymentStageStatusAttribute) == StageStatus.Failed;
		public bool IsStageIncomplete => feature.GetAttribute(deploymentStageStatusAttribute) == StageStatus.Incomplete;
		public bool IsStageInProgress => feature.GetAttribute(deploymentStageStatusAttribute) == StageStatus.InProgress;
		public ILinkedStage PrecedingStage { get; }
		public ILinkedStage FinalStage { get; }
		public int NumberOfRows { get; } = 3;

		int rowPadding;
		int rowHeight;

		public int RowPadding
		{
			set
			{
				rowPadding = value;
				DoSize();
			}
		}

		public int RowHeight
		{
			set
			{
				rowHeight = value;
				DoSize();
			}
		}

		public event EventHandler StageStatusChanged;

		public bool AutoSelectCorrectOptions
		{
			get => autoSelectCorrectOptions;
			set
			{
				autoSelectCorrectOptions = value;

				if (Visible && autoSelectCorrectOptions)
				{
					SelectCorrectOptions();
				}
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (PrecedingStage != null)
				{
					PrecedingStage.StageStatusChanged -= precedingStage_StageStatusChanged;
				}

				feature.AttributesChanged -= feature_AttributesChanged;
			}

			base.Dispose(disposing);
		}
		protected override void OnSizeChanged(EventArgs e)
		{
			DoSize();
		}

		void DoSize()
		{
			var optionsWidth = Width;

			var optionsHeight = rowHeight;

			optionsPanel.Bounds = new Rectangle(0, 0, optionsWidth, optionsHeight);

			var installButtonHeight = Maths.Clamp(Height - rowHeight - rowPadding, 20, 30);

			var installButtonY = optionsPanel.Bottom + rowPadding;

			if (optionsHeight + rowPadding + installButtonHeight > Height)
			{
				installButtonY = optionsPanel.Bottom + (Height - installButtonY - optionsHeight);
			}

			queueInstallButton.Bounds = new RectangleFromBounds
			{
				Right = Width,
				Top = installButtonY,
				Width = 60,
				Height = installButtonHeight
			}.ToRectangle();
			queueInstallButton.BringToFront();

		}

		void SelectCorrectOptions()
		{
			optionsPanel.SelectCorrectOption();
			if (IsStageIncomplete)
			{
				queueInstallButton.PressButton();
			}
		}

		void SetButtonState()
		{
			var status = feature.GetAttribute("status");

			switch (status)
			{
				case FeatureStatus.Dev:
				case FeatureStatus.Test:
				case FeatureStatus.TestDelay:
					buttonState = ButtonState.Enqueue;
					break;
				case FeatureStatus.Release:
					buttonState = ButtonState.Install;
					break;
				default:
					buttonState = ButtonState.Disabled;
					break;
			}

			var queueInstallText = buttonState == ButtonState.Enqueue ? "Enqueue" : "Install";
			queueInstallButton.Text = queueInstallText;
		}

		void OnStageStatusChanged()
		{
			StageStatusChanged?.Invoke(this, EventArgs.Empty);
		}

		void queueInstallButton_Click(object sender, EventArgs e)
		{
			optionsPanel.AddSelectionCommand();
			var isPrototypeFeature = feature.GetBooleanAttribute("is_prototype", false);

			var command = isPrototypeFeature || buttonState == ButtonState.Install
				? CommandTypes.InstallService
				: CommandTypes.EnqueueDeployment;

			commandQueueNode.CreateChild(command, "", new AttributeValuePair("service_name", feature.GetAttribute("name")));

			OnDeploymentStaged();
		}

		public event EventHandler DeploymentStaged;

		void OnDeploymentStaged()
		{
			DeploymentStaged?.Invoke(this, EventArgs.Empty);
		}

		void feature_AttributesChanged(Node sender, ArrayList attrs)
		{
			var attributes = attrs.Cast<AttributeValuePair>().ToList();

			if (attributes.Any(avp => avp.Attribute == "status"))
			{
				SetButtonState();
			}

			if (attributes.Any(avp => avp.Attribute == deploymentStageStatusAttribute) && HasCompletedStage)
			{
				OnStageStatusChanged();
			}

			SetButtonAvailability();

			
		}

		void SetButtonAvailability ()
		{
			var canDeploy = feature.GetBooleanAttribute("can_deploy", false);
			queueInstallButton.Enabled = !string.IsNullOrEmpty(optionsPanel.SelectedOption) && canDeploy && IsStageIncomplete;
		}

		void precedingStage_StageStatusChanged(object sender, EventArgs e)
		{
			Visible = !isAlwaysHidden && PrecedingStage.HasCompletedStage;

			if (PrecedingStage.HasCompletedStage && IsStageIncomplete && autoSelectCorrectOptions)
			{
				SelectCorrectOptions();
			}
		}

		void optionsPanel_OptionSelected(object sender, EventArgs e)
		{
			SetButtonAvailability();
		}

		enum ButtonState
		{
			Enqueue,
			Install,
			Disabled
		}


		ButtonState buttonState;

		readonly IRequestsManager requestsManager;

		readonly StageOptionsPanel optionsPanel;
		readonly StyledDynamicButton queueInstallButton;

		readonly bool isAlwaysHidden;
		readonly Node feature;
		readonly Node commandQueueNode;
		readonly string deploymentStageStatusAttribute;

		bool autoSelectCorrectOptions;

	}
}
