using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

using Algorithms;
using CommonGUI;
using DevOpsEngine.StringConstants;
using Network;
using ResizingUi.Button;

namespace DevOpsUi.FeatureDevelopment
{
	public class StageGroupPanel : FlickerFreePanel, ILinkedStage
	{
		int rowPadding = 2;
		public int RowPadding
		{
			set
			{
				rowPadding = value;
				DoSize();
			}
		}

		int rowHeight;

		public int RowHeight
		{
			set
			{
				rowHeight = value;
				DoSize();
			}
		}

		readonly bool isAlwaysHidden;

		public StageGroupPanel(Func<StageGroupProperties, Size, Func<bool>, Node, Node, StageOptionsPanel> optionsPanelFunc,
								IEnumerable<StageGroupProperties> optionsProperties, Func<Node, bool> canResetStageFunc,
								Node serviceNode, Node servicesCommandQueueNode, Size panelSize, string resetToStage,
								string stageStatusAttribute, ILinkedStage precedingStage, bool alwaysHidden,
								List<Node> monitorForChangesNodes = null)
		{
			PrecedingStage = precedingStage;
			isAlwaysHidden = alwaysHidden;

			this.panelSize = panelSize;

			if (PrecedingStage != null)
			{
				PrecedingStage.StageStatusChanged += precedingStage_StageStatusChanged;
				Visible = !isAlwaysHidden && PrecedingStage.HasCompletedStage;
			}

			if (monitorForChangesNodes != null)
			{
				foreach (var monitorForChangesNode in monitorForChangesNodes)
				{
					monitorForChangesNode.AttributesChanged += monitorForChangesNode_AttributesChanged;
				}
			}

			this.monitorForChangesNodes = monitorForChangesNodes;

			this.servicesCommandQueueNode = servicesCommandQueueNode;

			this.serviceNode = serviceNode;
			serviceNode.AttributesChanged += serviceNode_AttributesChanged;

			this.stageStatusAttribute = stageStatusAttribute;

			this.canResetStageFunc = canResetStageFunc;

			//this.useResetImage = useResetImage;

			stageResetButton = new StyledImageButton("progression_panel", 0, false)
			{
				Enabled = CanResetStage(),
				Tag = resetToStage,
				BackColor = Color.Transparent,
				CornerRadius = 4
			};
			stageResetButton.SetVariants(@"\images\buttons\reset.png");


			Controls.Add(stageResetButton);
			stageResetButton.Click += stageResetButton_Click;

			buttonGroups = new List<StageOptionsPanel>();

			foreach (var properties in optionsProperties)
			{
				var optionsPanel = optionsPanelFunc.Invoke(properties, panelSize, () => HasCompletedStage || IsStageInProgress, serviceNode,
					servicesCommandQueueNode);

				Controls.Add(optionsPanel);
				buttonGroups.Add(optionsPanel);
			}

			DoSize();
		}

		public bool HasCompletedStage => serviceNode.GetAttribute(stageStatusAttribute) == StageStatus.Completed;
		public bool HasFailedStage => serviceNode.GetAttribute(stageStatusAttribute) == StageStatus.Failed;
		public bool IsStageIncomplete => serviceNode.GetAttribute(stageStatusAttribute) == StageStatus.Incomplete;
		public bool IsStageInProgress => serviceNode.GetAttribute(stageStatusAttribute) == StageStatus.InProgress;
		public ILinkedStage PrecedingStage { get; }

		public event EventHandler StageStatusChanged;

		public ILinkedStage FinalStage
		{
			get => finalStage;
			set
			{
				if (finalStage != null)
				{
					finalStage.StageStatusChanged -= finalStage_StageStatusChanged;
				}

				finalStage = value;

				if (finalStage != null)
				{
					finalStage.StageStatusChanged += finalStage_StageStatusChanged;
				}

				UpdateStage();
			}
		}

		public int NumberOfRows => buttonGroups.Count;
		ILinkedStage finalStage;

		void OnStageCompletionStateChanged()
		{
			StageStatusChanged?.Invoke(this, EventArgs.Empty);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				foreach (var buttonGroup in buttonGroups)
				{
					buttonGroup.Dispose();
				}

				if (PrecedingStage != null)
				{
					PrecedingStage.StageStatusChanged -= precedingStage_StageStatusChanged;
				}

				if (FinalStage != null)
				{
					FinalStage.StageStatusChanged -= finalStage_StageStatusChanged;
				}

				serviceNode.AttributesChanged -= serviceNode_AttributesChanged;

				if (monitorForChangesNodes != null)
				{
					foreach (var monitorForChangesNode in monitorForChangesNodes)
					{
						monitorForChangesNode.AttributesChanged -= monitorForChangesNode_AttributesChanged;
					}
				}
			}

			base.Dispose(disposing);
		}

		void precedingStage_StageStatusChanged(object sender, EventArgs e)
		{
			Visible = !isAlwaysHidden && PrecedingStage.HasCompletedStage;

			if (Visible)
			{
				foreach (var buttonGroup in buttonGroups)
				{
					buttonGroup.UpdateOptions();
				}
			}

			if (autoSelectCorrectOptions && PrecedingStage.HasCompletedStage && IsStageIncomplete)
			{
				SelectCorrectOptions();
			}
		}

		void finalStage_StageStatusChanged(object sender, EventArgs e)
		{
			UpdateStage();
		}

		void serviceNode_AttributesChanged(Node sender, System.Collections.ArrayList attrs)
		{
			var attributes = attrs.Cast<AttributeValuePair>();
			// Moved this out of the if statement below
			// as it meant the release reset button was remaining
			// enabled after deployment was enqueued. 
			// TODO pass in a list of additional attributes to check for
			// that should trigger an update. GDC
			UpdateStage();
			if (attributes.Any(avp => avp.Attribute == stageStatusAttribute))
			{

				OnStageCompletionStateChanged();
			}
		}

		void monitorForChangesNode_AttributesChanged(Node sender, System.Collections.ArrayList attrs)
		{
			UpdateStage();
		}

		bool CanResetStage()
		{
			return !(finalStage?.HasCompletedStage ?? false) && (canResetStageFunc(serviceNode) && !IsStageIncomplete);
		}

		public void UpdateStage()
		{
			foreach (var buttonGroup in buttonGroups)
			{
				buttonGroup.UpdateOptions();
			}

			stageResetButton.Enabled = CanResetStage();
		}

		protected override void OnSizeChanged(EventArgs e)
		{
			DoSize();
		}

		void DoSize()
		{
			var resetSize = rowHeight - 4;

			var dynamicPanelSize = new Size(Width - resetSize - 10, rowHeight);

			var y = 0;
			foreach (var buttonGroup in buttonGroups)
			{
				buttonGroup.Bounds = new Rectangle(new Point(0, y), dynamicPanelSize);

				y += buttonGroup.Height + rowPadding;
			}


			stageResetButton.Bounds = new Rectangle(0, 0, Width, Height).AlignRectangle(resetSize, resetSize, StringAlignment.Far, StringAlignment.Far, 0, -2);
		}

		public override Size GetPreferredSize(Size size)
		{
			return new Size(size.Width, buttonGroups.Max(g => g.Bottom));
		}

		void stageResetButton_Click(object sender, EventArgs e)
		{
			ResetStage();
		}

		void ResetStage()
		{
			// ReSharper disable once ObjectCreationAsStatement
			new Node(servicesCommandQueueNode, "reset_stage", "",
				new List<AttributeValuePair>
				{
					new AttributeValuePair("type", "reset_stage"),
					new AttributeValuePair("target_status", (string) stageResetButton.Tag),
					new AttributeValuePair("service_name", serviceNode.GetAttribute("name"))
				});
		}

		public bool AutoSelectCorrectOptions
		{
			get => autoSelectCorrectOptions;

			set
			{
				autoSelectCorrectOptions = value;

				if (autoSelectCorrectOptions && Visible)
				{
					SelectCorrectOptions();
				}
			}
		}

		void SelectCorrectOptions()
		{
			foreach (var groupPanel in buttonGroups)
			{
				groupPanel.SelectCorrectOption();
			}
		}

		readonly List<StageOptionsPanel> buttonGroups;
		readonly ImageButton stageResetButton;

		readonly string stageStatusAttribute;

		readonly Node servicesCommandQueueNode;
		readonly Node serviceNode;
		readonly List<Node> monitorForChangesNodes;

		readonly Func<Node, bool> canResetStageFunc;
		readonly Size panelSize;

		bool autoSelectCorrectOptions;
	}
}
