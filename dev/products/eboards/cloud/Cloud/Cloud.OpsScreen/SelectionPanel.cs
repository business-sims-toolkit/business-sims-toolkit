using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;

using CommonGUI;
using LibCore;

namespace Cloud.OpsScreen
{
	public class SelectionPanel<T> : FlickerFreePanel
	{
		List<T> items;
		Dictionary<T, ImageTextButton> itemToButton;
		Label title;
		float fontSize;
		public bool useBack = false;

		protected Bitmap FullBackgroundImage;

		public SelectionPanel (string title, float fontSize) : this (title, fontSize, false)
		{

		}

		public SelectionPanel(string title, float fontSize, bool useBack)
		{
			this.fontSize = fontSize;
			items = new List<T>();
			itemToButton = new Dictionary<T, ImageTextButton>();

			if (useBack)
			{
				FullBackgroundImage = (Bitmap)Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "images\\panels\\PopupBack_Requests.png");
				BackgroundImage = FullBackgroundImage;
			}

			this.title = new Label();
			this.title.Text = title;
			this.title.Font = CoreUtils.SkinningDefs.TheInstance.GetFont(16);
			if (useBack)
			{
				this.title.ForeColor = Color.White;
				this.title.BackColor = Color.Black;
			}
			Controls.Add(this.title);

			DoSize();
		}


		protected override void Dispose (bool disposing)
		{
			base.Dispose(disposing);
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);
			DoSize();
		}

		void DoSize ()
		{
			OnRearrangeItems();
		}

		public ImageTextButton AddItem (T item, string buttonText)
		{
			return AddItem(item, buttonText, @"images\buttons\button_medium.png");
		}

		public ImageTextButton AddItem (T item, string buttonText, string buttonImage)
		{
			ImageTextButton button = new ImageTextButton (buttonImage);
			button.Tag = item;
			button.ButtonPressed += new ImageButton.ImageButtonEventArgsHandler (button_ButtonPressed);
			button.ButtonFont = CoreUtils.SkinningDefs.TheInstance.GetFont(fontSize);
			button.SetButtonText(buttonText);
			Controls.Add(button);

			items.Add(item);
			itemToButton.Add(item, button);

			DoSize();

			return button;
		}

		public void RemoveItem (T item)
		{
			Controls.Remove(itemToButton[item]);

			items.Remove(item);
			itemToButton.Remove(item);

			DoSize();
		}

		void button_ButtonPressed (object sender, ImageButtonEventArgs args)
		{
			OnButtonPressed((ImageTextButton) sender);
		}

		public delegate void ItemSelectedHandler (SelectionPanel<T> sender, T item);

		public event ItemSelectedHandler ItemSelected;

		void OnButtonPressed (ImageTextButton button)
		{
			if (ItemSelected != null)
			{
				ItemSelected(this, (T) (button.Tag));
			}
		}

		public delegate void RearrangeItemsHandler (SelectionPanel<T> sender, Control title, IList<Control> items);

		public event RearrangeItemsHandler RearrangeItems;

		void OnRearrangeItems ()
		{
			title.Location = new Point (0, 0);
			title.Size = new Size (Width - title.Left, 20);

			if (RearrangeItems != null)
			{
				List<Control> controls = new List<Control> ();
				foreach (T item in items)
				{
					controls.Add(itemToButton[item]);
				}
				RearrangeItems(this, title, controls);
			}
		}

		public ImageTextButton GetButtonForItem (T item)
		{
			return itemToButton[item];
		}

		public List<ImageTextButton> AllButtons
		{
			get
			{
				return new List<ImageTextButton> (itemToButton.Values);
			}
		}
	}
}