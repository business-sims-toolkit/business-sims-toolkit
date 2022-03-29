using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

using CommonGUI;

namespace NeowUi
{
	public class TabBar : FlickerFreePanel
	{
		List<ITabBarItem> items;
		ITabBarItem selectedItem;
		ITabBarItem hoverItem;

		public TabBar ()
		{
			items = new List<ITabBarItem> ();
		}

		public ReadOnlyCollection<ITabBarItem> Tabs
		{
			get
			{
				return items.AsReadOnly();
			}
		}

		public void AddTab (ITabBarItem item)
		{
			if (! items.Contains(item))
			{
				items.Add(item);
				item.Changed += item_Changed;
			}
		}

		void item_Changed (object sender, EventArgs args)
		{
			Invalidate();
		}

		public void RemoveTab (ITabBarItem item)
		{
			item.Changed -= item_Changed;
			items.Remove(item);
		}

		int GetItemWidth (ITabBarItem item)
		{
			return 100;
		}

		Rectangle GetItemBounds (ITabBarItem item)
		{
			Rectangle itemBounds = new Rectangle (0, 0, 0, Height + 2);
			foreach (ITabBarItem otherItem in items)
			{
				itemBounds = new Rectangle (itemBounds.Right, itemBounds.Top, GetItemWidth(otherItem), itemBounds.Height);
				if (otherItem == item)
				{
					break;
				}
			}

			return itemBounds;
		}

		public int PreferredWidth
		{
			get
			{
				return items.Sum(item => GetItemWidth(item));
			}
		}

		protected override void OnMouseMove (MouseEventArgs e)
		{
			base.OnMouseMove(e);

			ITabBarItem previousHoverItem = hoverItem;

			hoverItem = null;
			foreach (ITabBarItem item in items)
			{
				if (item.Enabled
					&& GetItemBounds(item).Contains(new Point (e.X, e.Y)))
				{
					hoverItem = item;
					break;
				}
			}

			if (hoverItem != previousHoverItem)
			{
				Invalidate();
			}
		}

		protected override void OnMouseLeave (EventArgs e)
		{
			base.OnMouseLeave(e);

			if (hoverItem != null)
			{
				hoverItem = null;
				Invalidate();
			}
		}

		protected override void OnMouseClick (MouseEventArgs e)
		{
			base.OnMouseClick(e);

			if (hoverItem != null)
			{
				SelectItem(hoverItem);
			}
		}

		void SelectItem (ITabBarItem item)
		{
			if (selectedItem != item)
			{
				selectedItem = item;
				Invalidate();

				OnItemSelected(item);
			}
		}

		protected override void OnPaint (PaintEventArgs e)
		{
			e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

			base.OnPaint(e);

			int cornerRoundingRadius = 6;

			foreach (ITabBarItem item in items)
			{
				Rectangle bounds = GetItemBounds(item);
				Color fillColour = BackColor;
				Color outlineColour = Color.Transparent;
				Color textColour = ForeColor;

				if (item == selectedItem)
				{
					fillColour = ForeColor;
					textColour = BackColor;
				}
				else if (item == hoverItem)
				{
					fillColour = Algorithms.Maths.Lerp(0.5, ForeColor, BackColor);
				}

				if (! item.Enabled)
				{
					textColour = Algorithms.Maths.Lerp(0.75, ForeColor, BackColor);
				}

				using (Brush brush = new SolidBrush (fillColour))
				{
					LibCore.RoundedRectangle.FillRoundedRectangle(e.Graphics, brush, bounds, cornerRoundingRadius,
																  LibCore.RoundedRectangle.Corner.TopLeft, LibCore.RoundedRectangle.Corner.TopRight);
				}

				StringFormat format = new StringFormat ();
				format.Alignment = StringAlignment.Center;
				format.LineAlignment = StringAlignment.Center;

				using (Brush brush = new SolidBrush (textColour))
				{
					e.Graphics.DrawString(item.Name, CoreUtils.SkinningDefs.TheInstance.GetFont(10), brush, bounds, format);
				}

				if (outlineColour != Color.Transparent)
				{
					using (Pen pen = new Pen (outlineColour, 2))
					{
						e.Graphics.DrawRectangle(pen, bounds);
					}
				}
			}
		}

		void OnItemSelected (ITabBarItem item)
		{
			if ((item != null)
				&& (ItemSelected != null))
			{
				ItemSelected(this, new TabItemSelectedEventArgs (item));
			}
		}

		public event TabItemSelectedEventHandler ItemSelected;

		public ITabBarItem SelectedItem
		{
			get
			{
				return selectedItem;
			}

			set
			{
				if (selectedItem != value)
				{
					SelectItem(value);
				}
			}
		}
	}
}