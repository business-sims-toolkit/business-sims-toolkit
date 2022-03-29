using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using CommonGUI;
using CoreUtils;
using Network;
using ResizingUi.Button;

namespace DevOpsUi.FeatureDevelopment
{
	public class StageOptionsButtonGroupPanel : StageOptionsPanel
	{
		public StageOptionsButtonGroupPanel(StageGroupProperties properties, Func<bool> hasPassedStage, Node serviceNode, Node servicesCommandQueueNode)
			: base(properties, hasPassedStage, serviceNode, servicesCommandQueueNode)
		{

			buttonSize = SkinningDefs.TheInstance.GetSizeData("stage_button_size", new Size(53, 30));
			buttonFlowDirection = properties.ButtonFlowDirection;

			titleLabel = new Label
			{
				Text = properties.Title,
				Location = new Point(widthPadding, heightPadding),
				TextAlign = properties.TitleAlignment,
				Font = SkinningDefs.TheInstance.GetFont(10f, FontStyle.Bold),
				ForeColor = SkinningDefs.TheInstance.GetColorData("service_progress_panel_text_colour", Color.White),
				BackColor = Color.Transparent
			};

			Controls.Add(titleLabel);

			buttonPanel = new FlowPanel
			{
				WrapContents = properties.WrapContents,
				FlowDirection = buttonFlowDirection,
				OuterMargin = 0,
				InnerPadding = heightPadding
			};

			buttons = new List<StyledDynamicButton>();

			Controls.Add(buttonPanel);
		}


		public override void UpdateOptions()
		{
			var buttonOptions = getOptions().GroupBy(b => b.ButtonId).Select(g => g.First()).ToList();
			var correctOption = getCorrectOption(serviceNode);
			var currentSelection = getCurrentSelection(serviceNode);

			var buttonIdToProperties = buttonOptions.ToDictionary(b => b.ButtonId, b => new StageButtonProperties
			{
				IsAlreadySelected = b.ButtonTag == currentSelection,
				IsCorrectOption = b.ButtonTag == correctOption,
				Text = b.ButtonText,
				Tag = b.ButtonTag,
				IsEnabled = b.IsEnabled
			});


			Debug.Assert(buttonIdToProperties.Count(o => o.Value.IsAlreadySelected) <= 1, "More than one button is set to be active.");


			if (buttons.Any())
			{
				// Update the properties for the existing buttons

				Debug.Assert(buttons.Count == buttonIdToProperties.Count);

				foreach (var button in buttons)
				{
					var id = (string)button.Tag;

					var properties = buttonIdToProperties[id];

					button.Active = properties.IsAlreadySelected;
					button.Highlighted = properties.IsCorrectOption;
					button.Enabled = properties.IsEnabled;
					button.Text = properties.Text;
				}
			}
			else
			{
				// Otherwise make wid dee buttons
				foreach (var option in buttonOptions.Select(o => o.ButtonId))
				{
					var isAlreadySelected = buttonIdToProperties[option].IsAlreadySelected;

					var button = new StyledDynamicButton("standard", buttonIdToProperties[option].Text)
					{
						Size = buttonSize,
						Font = SkinningDefs.TheInstance.GetFont(10),
						Active = isAlreadySelected,
						Tag = buttonIdToProperties[option].Tag,
						Enabled = buttonIdToProperties[option].IsEnabled,
						Highlighted = buttonIdToProperties[option].IsCorrectOption
					};

					button.Click += button_Click;

					buttons.Add(button);
					buttonPanel.Controls.Add(button);
					button.BringToFront();

					if (isAlreadySelected)
					{
						selectedOption = option;
					}
				}
			}

			EnableButtons(!hasPassedStage());
		}

		

		public void EnableButtons(bool enable)
		{
			buttonPanel.Enabled = enable;
		}


		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);

			DoSize();
		}

		protected override void OnEnabledChanged(EventArgs e)
		{
			titleLabel.Enabled = true;
			foreach (var button in buttons)
			{
				button.Enabled = Enabled;
			}
		}
		
		protected override void DoSize()
		{
			titleLabel.Size = new Size(75, Height);

			switch (buttonFlowDirection)
			{
				case FlowDirection.LeftToRight:
					titleLabel.Location = new Point(0, 0);

					buttonPanel.Location = new Point(titleLabel.Right, titleLabel.Top);
					buttonPanel.Size = new Size(Width - titleLabel.Width, Height);
					break;
				case FlowDirection.TopDown:
					buttonPanel.Location = new Point(widthPadding, titleLabel.Bottom + heightPadding);
					buttonPanel.Size = new Size(Width - (2 * widthPadding), Height - titleLabel.Height - heightPadding);
					break;
			}

			UpdateOptions();
		}
		
		void button_Click(object sender, EventArgs e)
		{
			var selectedButton = (StyledDynamicButton)sender;

			foreach (var button in buttons)
			{
				button.Active = false;
			}

			selectedButton.Active = true;

			SelectedOption = (string)selectedButton.Tag;
		}

		readonly Label titleLabel;
		readonly FlowPanel buttonPanel;
		readonly List<StyledDynamicButton> buttons;
		
		readonly FlowDirection buttonFlowDirection;
		readonly Size buttonSize;

		const int widthPadding = 0;
		const int heightPadding = 5;

		public override void SelectCorrectOption()
		{
			var correctOption = getCorrectOption(serviceNode);
			if (string.IsNullOrEmpty(correctOption))
			{
				correctOption = (string)buttons[0].Tag;
			}

			SelectedOption = correctOption;
		}
	}
}
