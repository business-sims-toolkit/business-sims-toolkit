using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using CoreUtils;

using LibCore;

namespace CommonGUI
{
	public enum Orientation
	{
		Horizontal,
		Vertical
	}

	public class ComboBoxRow : FlickerFreePanel
	{
		public interface ICheckableButton
		{
			Control Control { get; }
			bool Checked { get; set; }
			bool Enabled { get; set; }
			event EventHandler CheckedChanged;
		}

		public interface ICheckableButtonCreator
		{
			ICheckableButton CreateButton (string text, object tag, Font font, Color backColour, Color foreColour);
		}

	    public class ComboBoxButtonCreator : ICheckableButtonCreator
	    {
	        public ComboBoxButtonCreator (string styleName, int buttonWidth, int buttonHeight, float fontSize, FontStyle style)
	        {
	            this.styleName = styleName;
	            this.buttonWidth = buttonWidth;
	            this.buttonHeight = buttonHeight;

	            buttonFont = SkinningDefs.TheInstance.GetFont(fontSize, style);
	        }

	        public ComboBoxButtonCreator (string styleName, int buttonWidth, int buttonHeight, Font buttonFont)
	        {
	            this.styleName = styleName;
	            this.buttonWidth = buttonWidth;
	            this.buttonHeight = buttonHeight;
	            this.buttonFont = buttonFont;
	        }

	        public ICheckableButton CreateButton (string text, object tag, Font font, Color backColour, Color foreColour)
	        {
	            var button = new StyledDynamicButtonCommon(styleName, text)
	            {
	                Size = new Size(buttonWidth, buttonHeight),
	                Font = buttonFont,
                    Tag = tag
	            };

	            return new ImageButtonWrapper(button);
	        }
            
	        readonly string styleName;
	        readonly int buttonWidth;
	        readonly int buttonHeight;
	        readonly Font buttonFont;
	    }

		public class ImageTextButtonCreator : ICheckableButtonCreator
		{
			Color? normalColour;
			Color? activeColour;
			Color? hoverColour;
			Color? disabledColour;
			string imageStub;

			public ImageTextButtonCreator (string imageStub)
			{
				this.imageStub = imageStub;
			}

			public ImageTextButtonCreator (Color normalColour, Color activeColour, Color hoverColour, Color disabledColour, string imageStub)
			{
				this.normalColour = normalColour;
				this.activeColour = activeColour;
				this.hoverColour = hoverColour;
				this.disabledColour = disabledColour;
				this.imageStub = imageStub;
			}

			public ICheckableButton CreateButton (string text, object tag, Font font, Color backColour, Color foreColour)
			{
				ImageTextButton button = new ImageTextButton (@"\images\buttons\" + imageStub);

				if (normalColour.HasValue && activeColour.HasValue && hoverColour.HasValue && disabledColour.HasValue)
				{
					button.SetButtonText(text, normalColour.Value, activeColour.Value, hoverColour.Value, disabledColour.Value);
				}
				else
				{
					button.SetButtonText(text);
				}

			    button.SetAutoSize();

				button.ButtonFont = font;
				button.Tag = tag;

				return new ImageButtonWrapper (button);
			}
		}

		public class RadioButtonCreator : ICheckableButtonCreator
		{
			public ICheckableButton CreateButton (string text, object tag, Font font, Color backColour, Color foreColour)
			{
				RadioButton button = new RadioButton ();
				button.Text = text;
				button.Tag = tag;
				button.Font = font;
				button.BackColor = backColour;
				button.ForeColor = foreColour;

				return new RadioButtonWrapper (button);
			}
		}

		class CheckBoxWrapper : ICheckableButton, IDisposable
		{
			CheckBox checkBox;

			public CheckBoxWrapper (CheckBox checkBox)
			{
				this.checkBox = checkBox;
				checkBox.CheckedChanged += checkBox_CheckedChanged;
			}

			public void Dispose ()
			{
				checkBox.CheckedChanged -= checkBox_CheckedChanged;
			}

			public Control Control
			{
				get
				{
					return checkBox;
				}
			}

			public bool Checked
			{
				get
				{
					return checkBox.Checked;
				}

				set
				{
					checkBox.Checked = value;
				}
			}

			public event EventHandler CheckedChanged;

			void checkBox_CheckedChanged (object sender, EventArgs args)
			{
				OnCheckedChanged(args);
			}

			void OnCheckedChanged (EventArgs args)
			{
				CheckedChanged?.Invoke(this, args);
			}

			public bool Enabled
			{
				get
				{
					return checkBox.Enabled;
				}

				set
				{
					checkBox.Enabled = value;
				}
			}
		}

		class RadioButtonWrapper : ICheckableButton, IDisposable
		{
			RadioButton radioButton;

			public RadioButtonWrapper (RadioButton radioButton)
			{
				this.radioButton = radioButton;
				radioButton.CheckedChanged += radioButton_CheckedChanged;
			}

			public void Dispose ()
			{
				radioButton.CheckedChanged -= radioButton_CheckedChanged;
			}

			public Control Control
			{
				get
				{
					return radioButton;
				}
			}

			public bool Checked
			{
				get
				{
					return radioButton.Checked;
				}

				set
				{
					radioButton.Checked = value;
				}
			}

			public event EventHandler CheckedChanged;

			void radioButton_CheckedChanged (object sender, EventArgs args)
			{
				OnCheckedChanged(args);
			}

			void OnCheckedChanged (EventArgs args)
			{
				CheckedChanged?.Invoke(this, args);
			}

			public bool Enabled
			{
				get
				{
					return radioButton.Enabled;
				}

				set
				{
					radioButton.Enabled = value;
				}
			}
		}

		class ImageButtonWrapper : ICheckableButton, IDisposable
		{
			ImageButton button;

			public ImageButtonWrapper (ImageButton button)
			{
				this.button = button;
				button.ButtonPressed += button_ButtonPressed;
			}

			public void Dispose ()
			{
				button.ButtonPressed -= button_ButtonPressed;
			}

			public Control Control
			{
				get
				{
					return button;
				}
			}

			public bool Checked
			{
				get
				{
					return button.Active;
				}

				set
				{
					if (button.Active != value)
					{
						button.Active = value;

						if (value && (button.Parent != null))
						{
							foreach (Control control in button.Parent.Controls)
							{
								ImageButton imageButton = control as ImageButton;
								if ((imageButton != null) && (imageButton != button))
								{
									imageButton.Active = false;
								}
							}
						}

						OnCheckedChanged(EventArgs.Empty);
					}
				}
			}

			public bool Enabled
			{
				get
				{
					return button.Enabled;
				}

				set
				{
					button.Enabled = value;
				}
			}

			public event EventHandler CheckedChanged;

			void button_ButtonPressed (object sender, ImageButtonEventArgs args)
			{
				Checked = true;
			}

			void OnCheckedChanged (EventArgs args)
			{
				CheckedChanged?.Invoke(this, args);
			}
		}

		WatchableList<ComboBoxOption> items;
		public WatchableList<ComboBoxOption> Items
		{
			get
			{
				return items;
			}
		}

		ICheckableButtonCreator factory;

		List<ICheckableButton> buttons;
		Dictionary<ComboBoxOption, ICheckableButton> itemToButton;
		Dictionary<ICheckableButton, ComboBoxOption> buttonToItem;
		Dictionary<ComboBoxOption, bool> itemToEnabled;

		public event EventHandler SelectedItemChanged;
		public event EventHandler SelectedIndexChanged;

		Orientation orientation;
		int itemGap;

		public void SelectItemByText (string text)
		{
			SelectedItem = items.FirstOrDefault(i => i.Text == text);
		}

		ComboBoxOption selectedItem;
		public ComboBoxOption SelectedItem
		{
			get
			{
				return selectedItem;
			}

			set
			{
				selectedItem = value;

				if (selectedItem == null)
				{
					foreach (ICheckableButton button in buttons)
					{
						button.Checked = false;
					}
				}
				else
				{
					itemToButton[selectedItem].Checked = true;
				}

				OnSelectedItemChanged();
			}
		}

		public int SelectedIndex
		{
			get
			{
				if (items == null)
				{
					return -1;
				}
				else
				{
					return items.IndexOf(SelectedItem);
				}
			}

			set
			{
				if (value == -1)
				{
					SelectedItem = null;
				}
				else
				{
					SelectedItem = items[Math.Min(items.Count - 1, value)];
				}
			}
		}

	    Label titleLabel = new Label();

        public string TitleText
        {
            get
            {
                return titleLabel.Text;
            }
            set
            {
                titleLabel.Text = value;
            }
        }

	    public Font TitleFont
        {
            get
            {
                return titleLabel.Font;
            }
            set
            {
                titleLabel.Font = value;
            }
        }

	    public Color TitleFontColour
        {
            get
            {
                return titleLabel.ForeColor;
            }
            set
            {
                titleLabel.ForeColor = value;
            }
        }

	    public Color TitleBackColour
        {
            get
            {
                return titleLabel.BackColor;
            }
            set
            {
                titleLabel.BackColor = value;
            }
        }

        public ContentAlignment TitleAlignment
        {
            get
            {
                return titleLabel.TextAlign;
            }
            set
            {
                titleLabel.TextAlign = value;
            }
        }
        
		public ComboBoxRow (ICheckableButtonCreator factory, string title = "")
		{
			this.factory = factory;

			items = new WatchableList<ComboBoxOption> ();
			itemToEnabled = new Dictionary<ComboBoxOption, bool> ();
			buttons = new List<ICheckableButton>();
			itemToButton = new Dictionary<ComboBoxOption, ICheckableButton> ();
			buttonToItem = new Dictionary<ICheckableButton, ComboBoxOption> ();
			items.ItemsChanged += items_ItemsChanged;
			SelectedItem = null;

			orientation = Orientation.Horizontal;
			itemGap = 10;

			Height = 22;

            TitleText = title;
		    TitleFont = SkinningDefs.TheInstance.GetFont(12, FontStyle.Bold);
            TitleFontColour = Color.Black;
		    TitleBackColour = Color.Transparent;
		    TitleAlignment = ContentAlignment.MiddleLeft;

		    titleLabel.Location = new Point(0, 0);

		    Controls.Add(titleLabel);
		}

		void items_ItemsChanged (object sender, EventArgs e)
		{
			Rebuild();
		}

		void OnSelectedItemChanged ()
		{
			SelectedItemChanged?.Invoke(this, EventArgs.Empty);
			SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
		}

		void Rebuild ()
		{
			foreach (ICheckableButton button in buttons)
			{
				Controls.Remove(button.Control);
			}

			buttons.Clear();
			itemToButton.Clear();
			buttonToItem.Clear();

			if (items.Count == 1)
			{
				selectedItem = items[0];
			}

			foreach (var item in items)
			{
				if (! itemToEnabled.ContainsKey(item))
				{
					itemToEnabled.Add(item, true);
				}

				ICheckableButton button = factory.CreateButton(item.Text, item, Font, BackColor, ForeColor);
				buttons.Add(button);
				itemToButton.Add(item, button);
				buttonToItem.Add(button, item);
				Controls.Add(button.Control);
				button.Checked = (item == selectedItem);
				button.CheckedChanged += button_CheckedChanged;
			}

			UpdateState();
			DoSize();
		}

		void UpdateState ()
		{
			foreach (var item in items)
			{
				itemToButton[item].Enabled = itemToEnabled[item];
			}
		}

		public void EnableItem (ComboBoxOption item, bool enabled)
		{
			itemToEnabled[item] = enabled;
			UpdateState();
		}

		public void EnableIndex (int index, bool enabled)
		{
			EnableItem(items[index], enabled);
		}

		public bool IsItemEnabled (ComboBoxOption item)
		{
			return itemToEnabled[item];
		}

		public bool IsIndexEnabled (int index)
		{
			return IsItemEnabled(items[index]);
		}

		void button_CheckedChanged (object sender, EventArgs e)
		{
			ICheckableButton button = (ICheckableButton) sender;

			if (button.Checked)
			{
				selectedItem = buttonToItem[button];

				OnSelectedItemChanged();
			}
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);
			DoSize();
		}

		Size GetItemSize ()
		{
			Size size = new Size (50, 0);
			foreach (ICheckableButton button in buttons)
			{
				Size itemSize = button.Control.GetPreferredSize(Size.Empty);
				size = new Size (Math.Max(size.Width, itemSize.Width),
				                 Math.Max(size.Height, itemSize.Height));
			}

			return size;
		}

		public int ItemGap
		{
			get => itemGap;

			set
			{
				itemGap = value;
				DoSize();
			}
		}

		void DoSize ()
		{
			switch (orientation)
			{
				case Orientation.Horizontal:
					{
						int width = GetItemSize().Width;
                        int buttonWidths = buttons.Count * width;
					    int labelWidth = Width - buttonWidths - (ItemGap * buttons.Count);

					    titleLabel.Size = new Size(labelWidth, Height);
                        
                        int x = titleLabel.Right + ItemGap;

						foreach (ICheckableButton button in buttons)
						{
							button.Control.Location = new Point (x, 0);
							button.Control.Size = new Size (width, Height);
							x = button.Control.Right + ItemGap;
						}
					}
					break;

				case Orientation.Vertical:
					{
						int height = GetItemSize().Height;

                        int buttonHeights = buttons.Count * height;
					    int labelHeight = Height - buttonHeights - (ItemGap * buttons.Count);

					    titleLabel.Size = new Size(Width, labelHeight);

						int y = titleLabel.Bottom + ItemGap;
						foreach (ICheckableButton button in buttons)
						{
							button.Control.Location = new Point (0, y);
							button.Control.Size = new Size (Width, height);
							y = button.Control.Bottom + ItemGap;
						}
					}
					break;
			}
		}

		public override Size GetPreferredSize (Size proposedSize)
		{
			switch (Orientation)
			{
				case Orientation.Horizontal:
					{
						if (buttons.Count > 0)
						{
							proposedSize.Height = buttons[0].Control.GetPreferredSize(Size.Empty).Height;
						}
						return new Size (((int) (1.25f * titleLabel.GetPreferredSize(Size.Empty).Width)) + (items.Count * GetItemSize().Width) + (items.Count * ItemGap),
						                 proposedSize.Height);
					}

				case Orientation.Vertical:
					{
						if (buttons.Count > 0)
						{
							proposedSize.Width = buttons[0].Control.GetPreferredSize(Size.Empty).Width;
						}
						return new Size (proposedSize.Width,
						                 (items.Count * GetItemSize().Height) + ((items.Count - 1) * ItemGap));
					}
			}

			return Size.Empty;
		}

		public void SetAutoSize ()
		{
			Size = GetPreferredSize(Size);
		}

		public override Font Font
		{
			get => base.Font;

			set
			{
				if (Font != value)
				{
					base.Font = value;

					Rebuild();
				}
			}
		}

		public override Color ForeColor
		{
			get
			{
				return base.ForeColor;
			}

			set
			{
				if (ForeColor != value)
				{
					base.ForeColor = value;

					Rebuild();
				}
			}
		}

		public override Color BackColor
		{
			get
			{
				return base.BackColor;
			}

			set
			{
				if (BackColor != value)
				{
					base.BackColor = value;

					Rebuild();
				}
			}
		}

		public Orientation Orientation
		{
			get
			{
				return orientation;
			}

			set
			{
				orientation = value;
				DoSize();
			}
		}
	}
}