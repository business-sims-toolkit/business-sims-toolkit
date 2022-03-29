using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace CommonGUI
{

	public enum emToggleButtonBarAlignment
	{
		HORIZONTAL,
		VERTICAL,
		GRID
	}

	public class ToggleButtonBarItem
	{
		public int displayIndex = 0;
		public string displayName = string.Empty;
		public object DataObject = null;
		public bool Enabled = true;

		public ToggleButtonBarItem(int disp_index, string disp_name, object data_obj)
		{
			displayIndex = disp_index;
			displayName = disp_name;
			DataObject = data_obj;
		}

		public ToggleButtonBarItem(int disp_index, string disp_name, object data_obj, bool enabled)
		{
			displayIndex = disp_index;
			displayName = disp_name;
			DataObject = data_obj;
			Enabled = enabled;
		}
	}

	/// <summary>
	/// This is control for a series of toggle buttons 
	/// There are 2 display modes 
	///   No Label -- We just display the individual buttons 
	///   Show Label -- we show the label and then the buttons (allowing smaller buttons) as common text can be put into the label
	/// </summary>
	public class ToggleButtonBar : FlickerFreePanel
	{
		public delegate void ItemSelectedHandler(object sender, string item_name_selected, object selected_object);
		public event ItemSelectedHandler sendItemSelected;

		protected bool MyTrainingFlag = false;
		protected Hashtable dataNamesByDispIndex = new Hashtable();
		protected Hashtable dataObjectsdByDispName = new Hashtable();
		protected ArrayList disp_orders_list = new ArrayList();
		protected ArrayList btns = new ArrayList();
		protected Label Title;

		Dictionary<ImageToggleButton, ToggleButtonBarItem> buttonToItem;

		protected string selected_name = string.Empty;
		protected object selected_object = null;
		protected bool AllowNoneSelected = true;

		protected emToggleButtonBarAlignment ctrl_alignment = emToggleButtonBarAlignment.HORIZONTAL;

		public ToggleButtonBarItem SelectedBarItem
		{
			get
			{
				foreach (Control control in Controls)
				{
					ImageToggleButton button = control as ImageToggleButton;
					if (button != null)
					{
						if (button.State == 1)
						{
							return buttonToItem[button];
						}
					}
				}
				return null;
			}
		}

		public void EnableItem (ToggleButtonBarItem item, bool enabled)
		{
			foreach (ImageToggleButton button in buttonToItem.Keys)
			{
				if (buttonToItem[button] == item)
				{
					button.Enabled = enabled;
					item.Enabled = enabled;
					break;
				}
			}
		}

		public int SelectedIndex
		{
			get
			{
				foreach (Control control in Controls)
				{
					ImageToggleButton button = control as ImageToggleButton;
					if (button != null)
					{
						if (button.State == 1)
						{
							return buttonToItem[button].displayIndex;
						}
					}
				}
				return -1;
			}

			set 
			{ 
				//set the valid Button if it exists 
				foreach (Control control in Controls)
				{
					ImageToggleButton button = control as ImageToggleButton;
					if (button != null)
					{
						ToggleButtonBarItem tbi = (ToggleButtonBarItem)buttonToItem[button];
						if (tbi.displayIndex == (int)value)
						{
							handle_Selected(button);
						}
					}
				}
			}
		}

		public object SelectedObject
		{
			get
			{
				foreach (Control control in Controls)
				{
					ImageToggleButton button = control as ImageToggleButton;
					if (button != null)
					{
						if (button.State == 1)
						{
							if (buttonToItem[button] != null)
							{
								return buttonToItem[button].DataObject;
							}
							return null;
						}
					}
				}
				return null;
			}		
		}

		public ToggleButtonBar(Boolean IsTraining)
		{
			MyTrainingFlag = IsTraining;
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				Dispose_Ctrls();
			}
		}

		public void SetLabel(string newLabel, Point startPos, Size NewSize, Color TextForeColor, Color TextBackColor, Font newFont)
		{
			Title = new Label();
			Title.Location = startPos;
			Title.Size = NewSize;
			Title.Text = newLabel;
			Title.ForeColor = TextForeColor;
			Title.BackColor = TextBackColor;
			Title.Font = Font;
			if (newFont != null)
			{
				Title.Font = newFont;
			}
			Controls.Add(Title);
		}

		public void SetControlAlignment(emToggleButtonBarAlignment newAlign)
		{
			ctrl_alignment = newAlign;
		}

		public void SetAllowNoneSelected(bool newValue)
		{
			AllowNoneSelected = newValue;
		}

		public void Dispose_Ctrls()
		{
			if (btns.Count > 0)
			{
				foreach (ImageToggleButton itb_temp in btns)
				{
					itb_temp.ButtonPressed -= Btn_ButtonPressed;
					itb_temp.DoubleClick -= Btn_DoubleClick;
				}
				btns.Clear();
			}
		}

		public void SetOptions (ArrayList Required_Items, int width, int height, int gapx, int gapy,
			string bmp_On_filebase, string bmp_Off_filebase, Color textColor, string pre_selected_name)
		{
			SetOptions(Required_Items, width, height, gapx, gapy,
					   bmp_On_filebase, bmp_Off_filebase, "", "", textColor, pre_selected_name);
		}

		public void SetOptions(ArrayList Required_Items, int width, int height, int gapx, int gapy, 
			                   string bmp_On_filebase, string bmp_Off_filebase, string bmp_Disabled_filebase, string bmp_Hover_filebase,
			                   Color textColor, string pre_selected_name)
		{
			//Dispose of current contents
			Dispose_Ctrls();

			int posx = gapx;
			int posy = gapy;

			if (Title != null)
			{
				switch (ctrl_alignment)
				{
					case emToggleButtonBarAlignment.HORIZONTAL:
						posx += Title.Width + gapx;
						posy += 0;
						break;
					case emToggleButtonBarAlignment.VERTICAL:
						posx += 0;
						posy += Title.Height + gapy;
						break;
					case emToggleButtonBarAlignment.GRID:
						posx += 0;
						posy += Title.Height + gapy;
						break;
				}
			}

			Dictionary<int, ToggleButtonBarItem> displayOrderToItem = new Dictionary<int, ToggleButtonBarItem> ();
			buttonToItem = new Dictionary<ImageToggleButton, ToggleButtonBarItem> ();

			if (Required_Items.Count > 0)
			{
				//extract out the data elements 
				foreach (ToggleButtonBarItem tti in Required_Items)
				{
					dataNamesByDispIndex.Add(tti.displayIndex,tti.displayName);
					dataObjectsdByDispName.Add(tti.displayName, tti.DataObject);
					disp_orders_list.Add(tti.displayIndex);
					displayOrderToItem[tti.displayIndex] = tti;
				}
			}
			disp_orders_list.Sort();
			//Build the controls 
			foreach (int disp_order in disp_orders_list)
			{
				string disp_name = (string)dataNamesByDispIndex[disp_order];
				if (dataObjectsdByDispName.ContainsKey(disp_name))
				{
					Object data_obj = dataObjectsdByDispName[disp_name];

					//add button 
					ImageToggleButton itb_temp = new ImageToggleButton (0, bmp_On_filebase, bmp_Off_filebase,
							disp_name, disp_name, bmp_Disabled_filebase, bmp_Hover_filebase);
					itb_temp.Name = disp_name;
					itb_temp.Enabled = displayOrderToItem[disp_order].Enabled;

					buttonToItem.Add(itb_temp, displayOrderToItem[disp_order]);

					if (pre_selected_name.Equals(disp_name, StringComparison.InvariantCultureIgnoreCase) == true)
					{
						itb_temp.State = 1;
					}
					else
					{
						itb_temp.State = 0;
					}
					itb_temp.Size = new Size(width, height);
					itb_temp.Location = new Point(posx, posy);
					itb_temp.Font = Font;
					itb_temp.Tag = data_obj;
					itb_temp.ForeColor = textColor;
					itb_temp.setTextForeColor(textColor);
					itb_temp.ButtonPressed += Btn_ButtonPressed;
					itb_temp.DoubleClick += Btn_DoubleClick;
					Controls.Add(itb_temp);

					btns.Add(itb_temp);

					switch (ctrl_alignment)
					{ 
						case emToggleButtonBarAlignment.HORIZONTAL:
							posx += (width + gapx);
							posy += 0;
							break;
						case emToggleButtonBarAlignment.VERTICAL:
							posx += 0;
							posy += (height + gapy);
							break;
						case emToggleButtonBarAlignment.GRID:
							posx += (width + gapx);
							posy += 0;
							break;
					}
				}
			}
		}

		void handle_Selected(object sender)
		{
			ImageToggleButton itb1 = (ImageToggleButton)sender;
			bool process_required = false;

			if (AllowNoneSelected)
			{
				itb1.State = 1 - itb1.State;
				itb1.Refresh();

				if (itb1.State == 1)
				{
					selected_name = itb1.Name;
					selected_object = itb1.Tag;
				}
				else
				{
					selected_name = string.Empty;
					selected_object = null;
				}
				process_required = true;
			}
			else
			{
				if (itb1.State == 0)
				{
					itb1.State = 1 - itb1.State;
					itb1.Refresh();

					if (itb1.State == 1)
					{
						selected_name = itb1.Name;
						selected_object = itb1.Tag;
					}
					else
					{
						selected_name = string.Empty;
						selected_object = null;
					}
					process_required = true;
				}
			}

			if (process_required)
			{
				foreach (ImageToggleButton itb in btns)
				{
					if (itb != sender)
					{
						itb.State = 0;
					}
				}

				sendItemSelected?.Invoke(this, selected_name, selected_object);
			}
		}
		protected void Btn_DoubleClick(object sender, EventArgs e)
		{
			handle_Selected(sender);
		}
		protected void Btn_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			handle_Selected(sender);
		}


	}
}
