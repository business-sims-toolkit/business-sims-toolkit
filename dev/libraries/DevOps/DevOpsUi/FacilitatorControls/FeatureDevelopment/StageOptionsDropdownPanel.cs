using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Algorithms;
using CoreUtils;
using DevOpsUi.FeatureDevelopment;
using Network;
using ResizingUi.Button;

namespace DevOpsUi.FacilitatorControls.FeatureDevelopment
{
	public class StageOptionsDropdownPanel : StageOptionsPanel
	{
		internal class ItemProperties
		{
			public string Text { get; set; }
			public string Tag { get; set; }
			public bool IsCorrectOption { get; set; }
			public bool IsEnabled { get; set; }
			public int Index { get; set; }
		}

		readonly bool includeSubmitButton;

		public StageOptionsDropdownPanel(StageGroupProperties properties, Func<bool> hasPassedStage, Node featureNode, Node commandQueueNode, bool includeSubmitButton)
			: base(properties, hasPassedStage, featureNode, commandQueueNode)
		{
			this.includeSubmitButton = includeSubmitButton;

			titleLabel = new Label
			{
				Text = properties.Title,
				Location = new Point(0, 0),
				Font = SkinningDefs.TheInstance.GetFont(10, FontStyle.Bold),
				TextAlign = ContentAlignment.MiddleRight
			};
			Controls.Add(titleLabel);

			optionsDropdown = new ComboBox
			{
				DrawMode = DrawMode.OwnerDrawVariable,
				Enabled = !hasPassedStage(),
				FlatStyle = FlatStyle.Flat
			};
			optionsDropdown.SelectedIndexChanged += optionsDropdown_SelectedIndexChanged;
			optionsDropdown.KeyPress += optionsDropdown_KeyPress;
			optionsDropdown.DrawItem += optionsDropdown_DrawItem;

			Controls.Add(optionsDropdown);

			if (includeSubmitButton)
			{
				submitButton = new StyledImageButton("progression_panel_tick", 0, false)
				{
					Enabled = !hasPassedStage(),
					CornerRadius = 4,
					BackColor = Color.Transparent,
					Active = hasPassedStage()
				};
				submitButton.Click += submitButton_Click;
				submitButton.SetVariants(@"\images\buttons\tick.png");
				Controls.Add(submitButton);
			}

		}

		void optionsDropdown_KeyPress(object sender, KeyPressEventArgs e)
		{
			e.Handled = true;
		}

		public override void UpdateOptions()
		{
			if (!Visible)
			{
				return;
			}

			var options = getOptions().GroupBy(b => b.ButtonId).Select(g => g.First()).ToList();
			var correctOption = getCorrectOption(serviceNode);
			var currentSelection = getCurrentSelection(serviceNode);

			if (currentSelection == "")
			{
				currentSelection = null;
			}

			optionsDropdown.Items.Clear();

			itemProperties = options.Select((o, index) =>
			{
				var text = $"{o.ButtonText}{(o.ButtonTag == correctOption ? " *" : "")}";
				optionsDropdown.Items.Add(text);
				return new ItemProperties
				{
					Text = text,
					Tag = o.ButtonTag,
					IsCorrectOption = o.ButtonTag == correctOption,
					IsEnabled = o.IsEnabled,
					Index = index
				};
			}).ToList();

			Debug.Assert(itemProperties.Count(i => i.IsCorrectOption) == 1);

			var startingValue = selectedValue ?? currentSelection ?? correctOption;

			optionsDropdown.SelectedIndex = Math.Max(itemProperties.FindIndex(p => p.Tag == startingValue), 0);
			var selectedValueIndex = itemProperties.FindIndex(p => p.Tag == selectedValue);

			selectedItem = selectedValueIndex >= 0 ? itemProperties[selectedValueIndex] : null;

			if (submitButton != null)
			{
				SetSubmitButtonState();
				submitButton.Active = hasPassedStage() || !string.IsNullOrEmpty(currentSelection);
			}

			optionsDropdown.Enabled = !hasPassedStage();
		}

		public override void SelectCorrectOption()
		{

			var correctOption = getCorrectOption(serviceNode);

			if (string.IsNullOrEmpty(correctOption))
			{
				var correctItem = itemProperties.FirstOrDefault(i => i.IsCorrectOption);

				Debug.Assert(correctItem != null);
				correctOption = correctItem.Tag;
			}

			SelectedOption = correctOption;
		}

		protected override void DoSize()
		{
			titleLabel.Size = new Size(35, Height);

			var submitButtonSize = optionsDropdown.Height;

			if (submitButton != null)
			{
				submitButton.Bounds = new Rectangle(0, 0, Width, Height).AlignRectangle(submitButtonSize, Height - 4, StringAlignment.Far);
			}

			const int padding = 10;

			var dropdownLeft = titleLabel.Right + padding / 2;

			var dropdownWidth = (submitButton?.Left - padding ?? Width) - dropdownLeft;

			optionsDropdown.Bounds = new Rectangle(0, 0, Width, Height).AlignRectangle(dropdownWidth, optionsDropdown.Height, StringAlignment.Near, StringAlignment.Center, dropdownLeft);
			optionsDropdown.FlatStyle = FlatStyle.Flat;

			UpdateOptions();
		}

		int selectedIndex = -1;
		int SelectedIndex
		{
			set
			{
				var item = itemProperties.FirstOrDefault(i => i.Index == value);

				selectedIndex = value;

				selectedItem = item;
				SelectedValue = item?.Tag;
			}
		}

		ItemProperties selectedItem;

		string SelectedValue
		{
			set
			{
				selectedValue = value;

				if (submitButton != null && includeSubmitButton)
				{
					SetSubmitButtonState();
				}
				else if (selectedItem.IsEnabled)
				{
					SetSelectedOption();
				}
			}
		}

		void SetSubmitButtonState()
		{
			if (submitButton == null || !includeSubmitButton)
			{
				return;
			}

			submitButton.Enabled = !string.IsNullOrEmpty(selectedValue) && !hasPassedStage() && (selectedItem?.IsEnabled ?? false);
		}

		void SetSelectedOption()
		{
			if (!string.IsNullOrEmpty(selectedValue) && !hasPassedStage())
			{
				SelectedOption = selectedValue;
			}
		}

		void optionsDropdown_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (optionsDropdown.SelectedIndex != selectedIndex)
			{
				SelectedIndex = optionsDropdown.SelectedIndex;
			}

			if (submitButton != null)
			{
				submitButton.Active = false;
			}
		}

		void submitButton_Click(object sender, EventArgs e)
		{
			submitButton.Active = true;
			SetSelectedOption();
		}

		void optionsDropdown_DrawItem(object sender, DrawItemEventArgs e)
		{
			var item = itemProperties?.FirstOrDefault(p => p.Index == e.Index);

			if (item == null)
			{
				return;
			}

			var isItemEnabled = item.IsEnabled && !hasPassedStage();
			var isItemSelected = e.Index == selectedIndex;

			// Is the mouse currently hovering over this item?
			var isHighlighted = (e.State & DrawItemState.Selected) == DrawItemState.Selected;

			var backColour = isHighlighted ? Color.Navy.Tint(0.3f) : isItemSelected ? Color.Navy : Color.White;
			var foreColour = isItemSelected ? Color.White : Color.Black;

			if (!isItemEnabled)
			{
				backColour = backColour.Shade(0.2f);
				foreColour = foreColour.Shade(0.1f);
			}

			using (var backBrush = new SolidBrush(backColour))
			using (var foreBrush = new SolidBrush(foreColour))
			{
				e.Graphics.FillRectangle(backBrush, e.Bounds);

				e.Graphics.DrawString(item.Text, e.Font, foreBrush, e.Bounds, new StringFormat
				{
					LineAlignment = StringAlignment.Center,
					Alignment = StringAlignment.Near
				});
			}

			var drawBorder = item.IsCorrectOption;

			if (drawBorder)
			{
				var borderColour = Color.ForestGreen;

				if (!isItemEnabled)
				{
					borderColour = borderColour.Shade(0.3f);
				}

				using (var borderPen = new Pen(borderColour, 3f))
				{
					e.Graphics.DrawRectangle(borderPen, e.Bounds);
				}
			}
		}

		string selectedValue;

		readonly Label titleLabel;
		readonly ComboBox optionsDropdown;
		readonly StyledImageButton submitButton;

		List<ItemProperties> itemProperties;
	}
}
