using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Algorithms;
using CommonGUI;
using CoreUtils;
using DevOpsEngine.Interfaces;
using DevOpsEngine.StringConstants;
using DevOpsUi.FeatureDevelopment;
using LibCore;
using Network;
using ResizingUi.Button;
using ResizingUi.Interfaces;

namespace DevOpsUi.FacilitatorControls.FeatureDevelopment.Agile
{
	public class FeatureProgressionPanel : FlickerFreePanel
	{
		public FeatureProgressionPanel(NodeTree model, Node feature, IRequestsManager requestsManager, IDialogOpener dialogOpener)
		{
			this.model = model;
			this.feature = feature;
			this.dialogOpener = dialogOpener;

			isPrototypeFeature = feature.GetBooleanAttribute("is_prototype", false);

			commandQueueNode = model.GetNamedNode("BeginServicesCommandQueue");

			var featureId = feature.GetAttribute("service_id");
			var productId = feature.GetAttribute("product_id");
			var platform = feature.GetAttribute("platform");

			titleLabel = new Label
			{
				Text = (isPrototypeFeature ? $"{featureId} - {productId}" : $"{featureId} - {productId} - {platform}"),
				Font = SkinningDefs.TheInstance.GetFont(12, FontStyle.Bold),
				Location = new Point(5, 5),
				ForeColor = Color.Black,
				TextAlign = ContentAlignment.MiddleLeft

			};
			Controls.Add(titleLabel);

			closeButton = new StyledImageButton("progression_panel", 0, false)
			{
				BackColor = Color.Transparent,
				UseCircularBackground = true,
				Margin = new Padding(4)
			};
			closeButton.SetVariants(@"\images\buttons\cross.png");
			closeButton.Click += closeButton_Click;
			Controls.Add(closeButton);

			linkedStageToCheckForClosure = new Dictionary<ILinkedStage, Func<ILinkedStage, bool>>();

			devStageGroupPanel = CreateDevStage();
			Controls.Add(devStageGroupPanel);

			linkedStageToCheckForClosure[devStageGroupPanel] = ls => ls.HasCompletedStage;

			devStageGroupPanel.StageStatusChanged += linkedStage_StageStatusChanged;

			testStageGroupPanel = CreateTestStage();
			Controls.Add(testStageGroupPanel);

			linkedStageToCheckForClosure[testStageGroupPanel] = ls => ls.HasCompletedStage || ls.IsStageInProgress;

			testStageGroupPanel.AutoSelectCorrectOptions = isPrototypeFeature;
			testStageGroupPanel.StageStatusChanged += linkedStage_StageStatusChanged;

			deploymentStagePanel = new AgileDeploymentStagePanel(requestsManager, testStageGroupPanel, feature, commandQueueNode, isPrototypeFeature)
			{
				AutoSelectCorrectOptions = isPrototypeFeature
			};
			Controls.Add(deploymentStagePanel);
			deploymentStagePanel.StageStatusChanged += linkedStage_StageStatusChanged;
			deploymentStagePanel.DeploymentStaged += deploymentStagePanel_DeploymentStaged;

			devStageGroupPanel.FinalStage = deploymentStagePanel;
			testStageGroupPanel.FinalStage = deploymentStagePanel;

			linkedStages = new List<ILinkedStage>
			{
				devStageGroupPanel,
				testStageGroupPanel,
				deploymentStagePanel
			};

			failureMessagePanel = new StageFailurePanel
			{
				ForeColor = Color.Black,
				TextOutlineColour = CONVERT.ParseHtmlColor("#ff2d4e")
			};
			Controls.Add(failureMessagePanel);
			failureMessagePanel.Click += failureMessagePanel_Click;

			failureMessagePanel.AddStageFailureMessage(devStageGroupPanel, StageFailureMessage.DevelopmentStage.Replace(" ", "\r\n"));
			failureMessagePanel.AddStageFailureMessage(deploymentStagePanel, StageFailureMessage.DeploymentStage.Replace(" ", "\r\n"));

			interval = 1000;
			duration = 5 * interval;
			closingTimer = new Timer
			{
				Interval = interval
			};
			closingTimer.Tick += closingTimer_Tick;
		}



		void closingTimer_Tick(object sender, EventArgs e)
		{
			elapsedTime += interval;

			if (elapsedTime >= duration)
			{
				closingTimer.Stop();
				dialogOpener.CloseDialog();
			}
		}

		void linkedStage_StageStatusChanged(object sender, EventArgs e)
		{
			var linkedStage = (ILinkedStage) sender;

			if (linkedStageToCheckForClosure.TryGetValue(linkedStage, out var checkFunc))
			{
				if (checkFunc?.Invoke(linkedStage) ?? false)
				{
					elapsedTime = 0;
					closingTimer.Start();

					return;
				}
			}

			if (linkedStages.All(ls => ls.HasCompletedStage))
			{
				elapsedTime = 0;
				closingTimer.Start();
			}
			else
			{
				closingTimer.Stop();
			}
		}

		readonly Dictionary<ILinkedStage, Func<ILinkedStage, bool>> linkedStageToCheckForClosure;

		readonly int interval;
		readonly int duration;
		int elapsedTime;

		readonly Timer closingTimer;

		readonly List<ILinkedStage> linkedStages;

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				closingTimer.Stop();

			}

			base.Dispose(disposing);
		}


		protected override void OnBackColorChanged(EventArgs e)
		{
			failureMessagePanel.BackColor = BackColor;
		}

		void failureMessagePanel_Click(object sender, EventArgs e)
		{
			failureMessagePanel.Hide();
		}

		void ClosePanel()
		{
			dialogOpener.CloseDialog();
		}

		void closeButton_Click(object sender, EventArgs e)
		{
			ClosePanel();
		}
		void deploymentStagePanel_DeploymentStaged(object sender, EventArgs e)
		{
			ClosePanel();
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			var topStageY = linkedStages.Min(s => s.Bounds.Top);

			var lineY = titleLabel.Bottom + (topStageY - titleLabel.Bottom) / 2f;

			using (var pen = new Pen(CONVERT.ParseHtmlColor("#c1c1c1"), 1))
			{
				e.Graphics.DrawLine(pen, titleLabel.Left, lineY, closeButton.Right, lineY);
			}
		}


		protected override void OnSizeChanged(EventArgs e)
		{
			DoSize();
		}

		void DoSize()
		{
			const int topPadding = 0;
			const int closeButtonSize = 20;

			const int horizontalOffset = 10;

			titleLabel.Bounds = new RectangleFromBounds
			{
				Top = topPadding,
				Left = horizontalOffset,
				Right = Width - closeButtonSize - horizontalOffset,
				Height = 30
			}.ToRectangle();


			closeButton.Bounds = new Rectangle(0, titleLabel.Top, Width, titleLabel.Height).AlignRectangle(closeButtonSize, closeButtonSize, StringAlignment.Far, StringAlignment.Center, -horizontalOffset);
			closeButton.BringToFront();

			const int titlePadding = 10;

			var remainingHeight = Height - (titleLabel.Bottom + titlePadding);

			var totalRows = linkedStages.Sum(s => s.NumberOfRows);

			const int rowHeight = 25;

			const int internalPadding = 5;

			var padding = Maths.Clamp((remainingHeight - rowHeight * totalRows) / (linkedStages.Count - 1), 3, 20);

			var y = titleLabel.Bottom + titlePadding;

			foreach (var stage in linkedStages)
			{
				var stageHeight = rowHeight * stage.NumberOfRows + internalPadding * (stage.NumberOfRows - 1);

				stage.RowHeight = rowHeight;
				stage.RowPadding = internalPadding;

				stage.Bounds = new Rectangle(0, y, closeButton.Right, stageHeight);

				y += stageHeight + padding;
			}

			failureMessagePanel.Bounds = new RectangleFromBounds
			{
				Left = 0,
				Top = titleLabel.Bottom,
				Bottom = y,
				Width = Width
			}.ToRectangle();


			deploymentStagePanel.BringToFront();

			Invalidate();
		}

		static StageOptionsPanel DropdownPanelCreator(StageGroupProperties properties, Size panelSize,
													Func<bool> hasPassedStage, Node serviceNode, Node commandQueue)
		{
			return new StageOptionsDropdownPanel(properties, hasPassedStage, serviceNode, commandQueue, true)
			{
				Size = panelSize
			};
		}

		StageGroupPanel CreateDevStage()
		{
			var securityClassification = feature.GetAttribute("data_security_level");
			var isThirdParty = feature.GetBooleanAttribute("is_third_party", false);
			var copyDevTeam2FromTeam1 = isThirdParty || isPrototypeFeature;

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
						.GetChildWithAttributeValue("data_security_level", securityClassification)
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

			var devOneCommands = new List<string>
			{
				CommandTypes.DevOneSelection
			};

			if (copyDevTeam2FromTeam1)
			{
				devOneCommands.Add(CommandTypes.DevTwoSelection);
			}

			var devStageOptionProperties = new List<StageGroupProperties>
			{
				new StageGroupProperties
				{
					Title = "D1:",
					CommandTypes = devOneCommands,
					GetOptions = getDevOptionsFunc,
					GetCorrectOption = featureNode => featureNode.GetAttribute("dev_one_environment"),
					GetCurrentSelection = featureNode => featureNode.GetAttribute("dev_one_selection", null, null),
					ButtonFlowDirection = FlowDirection.LeftToRight,
					WrapContents = true
				}
			};

			if (!copyDevTeam2FromTeam1)
			{
				devStageOptionProperties.Add(new StageGroupProperties
				{
					Title = "D2:",
					CommandTypes = new List<string> { CommandTypes.DevTwoSelection },
					GetOptions = getDevOptionsFunc,
					GetCorrectOption = featureNode => featureNode.GetAttribute("dev_two_environment"),
					GetCurrentSelection = featureNode => featureNode.GetAttribute("dev_two_selection", null, null),
					ButtonFlowDirection = FlowDirection.LeftToRight,
					WrapContents = true
				});
			}

			var panelSize = new Size(150, 30);

			return new StageGroupPanel(DropdownPanelCreator, devStageOptionProperties, featureNode => true, feature, commandQueueNode, panelSize, FeatureStatus.Dev, "dev_stage_status", null, false)
			{
				RowPadding = 5
			};
		}

		StageGroupPanel CreateTestStage()
		{
			var testEnvironmentsNode = model.GetNamedNode("TestEnvironments");
			var testEnvironments = testEnvironmentsNode.GetChildrenWithAttributeValue("type", "TestEnvironment");

			var isVirtualTestingEnabled = testEnvironmentsNode.GetBooleanAttribute("virtual_test_enabled", false);

			Func<List<ButtonTextTags>> getOptionsFunc;
			Func<Node, string> getCorrectOptionFunc;

			string GetCurrentSelectionFunc(Node featureNode) => featureNode.GetAttribute("test_environment_selection");

			if (isVirtualTestingEnabled)
			{
				getCorrectOptionFunc = featureNode => isPrototypeFeature ? "Bypass" : "Virtual Test";

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
					return testEnvironments.Where(e => !e.GetAttribute("desc").Contains("Virtual"))
						.Select(e =>
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

				if (isPrototypeFeature)
				{
					getCorrectOptionFunc = featureNode => "Bypass";
				}
				else
				{
					getCorrectOptionFunc = featureNode => featureNode.GetAttribute("test_environment");
				}
			}

			return new StageGroupPanel(DropdownPanelCreator, new List<StageGroupProperties>
			{
				new StageGroupProperties
				{
					Title = "T:",
					CommandTypes = new List<string> { CommandTypes.TestEnvironmentSelection },
					GetCurrentSelection = GetCurrentSelectionFunc,
					GetCorrectOption = getCorrectOptionFunc,
					GetOptions = getOptionsFunc,
					ButtonFlowDirection = FlowDirection.LeftToRight,
					WrapContents = true
				}
			}, featureNode => true, feature, commandQueueNode, new Size(150, 30), FeatureStatus.Test,
				"test_stage_status", devStageGroupPanel, isPrototypeFeature, testEnvironments);
		}

		readonly NodeTree model;
		readonly Node feature;
		readonly Node commandQueueNode;

		readonly IDialogOpener dialogOpener;


		readonly bool isPrototypeFeature;

		readonly Label titleLabel;

		readonly StyledImageButton closeButton;

		readonly StageGroupPanel devStageGroupPanel;
		readonly StageGroupPanel testStageGroupPanel;

		readonly AgileDeploymentStagePanel deploymentStagePanel;

		readonly StageFailurePanel failureMessagePanel;

	}
}
