using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using Algorithms;
using CommonGUI;
using CoreUtils;
using ResizingUi;
using ResizingUi.Button;

namespace DevOpsUi.FacilitatorControls.FeatureDevelopment
{
	internal class FeatureSelectionButtonsPanel : FlickerFreePanel
	{
		public int PreferredHeight { get; private set; }
		public int PreferredWidth { get; private set; }
		public string SelectedId { get; private set; }

		public int MaximumColumns
		{
			get => maximumColumns;
			set
			{
				maximumColumns = value;

				UpdateButtons();
			}
		}

		int maximumColumns = 8;

		public event EventHandler ButtonClicked;
		public event EventHandler ButtonDoubleClicked;

		DynamicGridLayoutPanel gridPanel;

		List<StyledDynamicButton> buttons;

		readonly Label titleLabel;

		readonly Func<List<StyledDynamicButton>> createButtonsFunc;
		readonly Size minimumButtonSize;
		readonly int? maximumButtonWidth;
		readonly int? maximumButtonHeight;

		string preSelectedId;

		public string PreSelectedId
		{
			set
			{
				preSelectedId = value;

				if (string.IsNullOrEmpty(preSelectedId))
				{
					return;
				}

				var preselectedButton = buttons.FirstOrDefault(b => (string)b.Tag == preSelectedId);

				preselectedButton?.PressButton();
			}
		}

		public FeatureSelectionButtonsPanel(Func<List<StyledDynamicButton>> createButtonsFunc, Size minimumButtonSize,
												int? maximumButtonWidth, int? maximumButtonHeight, string title, bool includeTitle = false)
		{
			buttons = new List<StyledDynamicButton>();

			this.createButtonsFunc = createButtonsFunc;
			this.minimumButtonSize = minimumButtonSize;
			this.maximumButtonHeight = maximumButtonHeight;
			this.maximumButtonWidth = maximumButtonWidth;

			UpdateButtons();

			if (includeTitle)
			{
				titleLabel = new Label
				{
					Text = title,
					TextAlign = ContentAlignment.MiddleLeft,
					Font = SkinningDefs.TheInstance.GetFont(
						SkinningDefs.TheInstance.GetFloatData("request_panel_sub_title_size", 15.0f),
						FontStyle.Regular),
					ForeColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("request_panel_text_colour",
						Color.Black),
					BackColor = Color.Transparent
				};
				Controls.Add(titleLabel);
			}

		}

		public void UpdateButtons()
		{
			foreach (var button in buttons)
			{
				button.Dispose();
			}

			gridPanel?.Dispose();

			buttons = createButtonsFunc();

			foreach (var button in buttons)
			{
				button.Click += button_Click;
				button.DoubleClick += button_DoubleClick;
				button.FontSizeToFitChanged += button_FontSizeToFitChanged;
			}

			gridPanel = new DynamicGridLayoutPanel(buttons, minimumButtonSize.Width, minimumButtonSize.Height)
			{
				HorizontalOuterMargin = 5,
				HorizontalInnerPadding = 5,
				VerticalOuterMargin = 5,
				VerticalInnerPadding = 5,
				AreItemsFixedSize = false,
				IsSpacingFixedSize = true,
				MaximumItemHeight = maximumButtonHeight,
				MaximumItemWidth = maximumButtonWidth,
				MaximumColumns = maximumColumns
			};
			Controls.Add(gridPanel);
		}



		void button_FontSizeToFitChanged(object sender, EventArgs e)
		{
			UpdateButtonFontSize();
		}

		void UpdateButtonFontSize()
		{
			if (!buttons.Any())
			{
				return;
			}

			var size = buttons.Min(b => b.FontSizeToFit);
			foreach (var button in buttons)
			{
				button.FontSize = size;
			}
		}

		protected override void OnSizeChanged(EventArgs e)
		{
			DoSize();
		}

		void DoSize()
		{
			if (titleLabel != null)
			{
				titleLabel.Size = new Size(Width, 25);
			}

			gridPanel.Bounds = new RectangleFromBounds
			{
				Left = 0,
				Top = titleLabel?.Bottom ?? 0,
				Right = Width,
				Bottom = Height
			}.ToRectangle();// (0, titleHeight, Width, Height - titleHeight);

			PreferredHeight = (gridPanel.Height = gridPanel.PreferredGridHeight) + (titleLabel?.Height ?? 0);
			gridPanel.Width = gridPanel.PreferredGridWidth;

			if (titleLabel != null)
			{
				using (var graphics = titleLabel.CreateGraphics())
				{
					var titleSize = graphics.MeasureString(titleLabel.Text, titleLabel.Font);

					PreferredWidth = (int)Math.Max(titleSize.Width, gridPanel.PreferredGridWidth) + 10;
				}
			}
			else
			{
				PreferredWidth = gridPanel.PreferredGridWidth;
			}
		}

		void ProcessButtonClick(StyledDynamicButton button)
		{
			foreach (var b in buttons)
			{
				b.Active = false;
			}

			button.Active = true;

			SelectedId = (string)button.Tag;
		}

		void button_Click(object sender, EventArgs e)
		{
			ProcessButtonClick((StyledDynamicButton)sender);
			OnButtonClicked();
		}

		void OnButtonClicked()
		{
			ButtonClicked?.Invoke(this, EventArgs.Empty);
		}

		void button_DoubleClick(object sender, EventArgs e)
		{
			ProcessButtonClick((StyledDynamicButton)sender);
			OnButtonDoubleClicked();
		}

		void OnButtonDoubleClicked()
		{
			ButtonDoubleClicked?.Invoke(this, EventArgs.Empty);
		}
	}

}
