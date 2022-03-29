using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace LibCore
{
	public class CustomMessageBox : Form
	{
		List<CustomMessageBoxItem> items;
		Dictionary<CustomMessageBoxItem, Button> itemToButton;

		Label bodyText;

		CustomMessageBoxItem selectedItem;

		public CustomMessageBox (string text, string title, params CustomMessageBoxItem [] items)
		{
			ShowInTaskbar = false;
			FormBorderStyle = FormBorderStyle.FixedDialog;
			ControlBox = false;

			Text = title;

			bodyText = new Label ();
			bodyText.Text = text;
			Controls.Add(bodyText);

			this.items = new List<CustomMessageBoxItem> ();
			itemToButton = new Dictionary<CustomMessageBoxItem, Button>();

			List<CustomMessageBoxItem> leftItems = new List<CustomMessageBoxItem> ();
			List<CustomMessageBoxItem> centreItems = new List<CustomMessageBoxItem> ();
			List<CustomMessageBoxItem> rightItems = new List<CustomMessageBoxItem> ();

			int verticalSeparation = 20;
			int horizontalSeparation = 10;
			int buttonWidth = 100;
			int maxButtonHeight = 0;
			foreach (CustomMessageBoxItem item in items)
			{
				Button button = new Button ();
				button.Text = item.Text;
				button.Tag = item;
				button.Click += button_Click;
				Controls.Add(button);
				itemToButton.Add(item, button);
				this.items.Add(item);

				List<CustomMessageBoxItem> list;
				switch (item.Alignment)
				{
					case StringAlignment.Near:
						list = leftItems;
						break;

					case StringAlignment.Far:
						list = rightItems;
						break;

					case StringAlignment.Center:
					default:
						list = centreItems;
						break;
				}

				list.Add(item);

				maxButtonHeight = Math.Max(maxButtonHeight, button.GetPreferredSize(new Size (buttonWidth, 0)).Height);
			}

			int buttonHeight = maxButtonHeight;

			int widthForButtons = (this.items.Count * buttonWidth) + ((this.items.Count + 1) * horizontalSeparation);
			int widthForBodyText = Math.Max(100, Math.Min(300, bodyText.GetPreferredSize(new Size (0, 50)).Width));
			int width = Math.Max(widthForBodyText, widthForButtons);
			int bodyTextWidth = width - (2 * horizontalSeparation);
			int bodyTextHeight = bodyText.GetPreferredSize(new Size (bodyTextWidth, 0)).Height;
			int height = verticalSeparation + bodyTextHeight + verticalSeparation + buttonHeight + verticalSeparation;

			int extraWidth = Width - ClientSize.Width;
			int extraHeight = Height - ClientSize.Height;
			Size = new Size (width + extraWidth, height + extraHeight);

			bodyText.Location = new Point (horizontalSeparation, verticalSeparation);
			bodyText.Size = new Size (bodyTextWidth, bodyTextHeight);

			int leftX = horizontalSeparation;
			foreach (CustomMessageBoxItem item in leftItems)
			{
				Button button = itemToButton[item];
				button.Location = new Point (leftX, bodyText.Bottom + verticalSeparation);
				button.Size = new Size (buttonWidth, buttonHeight);
				leftX = button.Right + horizontalSeparation;
			}

			int rightX = ClientSize.Width - horizontalSeparation;
			for (int i = rightItems.Count - 1; i >= 0; i--)
			{
				CustomMessageBoxItem item = rightItems[i];
				Button button = itemToButton[item];
				button.Size = new Size (buttonWidth, buttonHeight);
				button.Location = new Point (rightX - buttonWidth, bodyText.Bottom + verticalSeparation);
				rightX = button.Left - horizontalSeparation;
			}

			int x = (leftX + rightX - (centreItems.Count * buttonWidth) - ((centreItems.Count - 1) * horizontalSeparation)) / 2;
			foreach (CustomMessageBoxItem item in centreItems)
			{
				Button button = itemToButton[item];
				button.Location = new Point (x, bodyText.Bottom + verticalSeparation);
				button.Size = new Size(buttonWidth, buttonHeight);
				leftX = button.Right + horizontalSeparation;
			}
		}

		public CustomMessageBoxItem HighlightedItem
		{
			get
			{
				foreach (CustomMessageBoxItem item in itemToButton.Keys)
				{
					if (itemToButton[item].Focused)
					{
						return item;
					}
				}

				return null;
			}

			set
			{
				itemToButton[value].Select();
			}
		}

		public CustomMessageBoxItem SelectedItem
		{
			get
			{
				return selectedItem;
			}
		}

		void button_Click (object sender, EventArgs e)
		{
			Button button = (Button) sender;
			selectedItem = (CustomMessageBoxItem) (button.Tag);
			DialogResult = selectedItem.DialogResult;
			Close();
		}
	}
}