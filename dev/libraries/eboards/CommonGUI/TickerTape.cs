using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;

namespace CommonGUI
{
	public class TickerTape : FlickerFreePanel
	{
		public interface ITickerTapeItem
		{
			Size GetSize ();
			void Paint (Point location, RenderItemStyle style, float timer, PaintEventArgs e);
		}

		public class TickerTapeTextItem : ITickerTapeItem
		{
			public string Message;
			public Color Colour;
			Font font;
			Size size;

			public TickerTapeTextItem (string message, Color colour, TickerTape control)
			{
				Message = message;
				Colour = colour;

				font = LibCore.ConstantSizeFont.NewFont(CoreUtils.SkinningDefs.TheInstance.GetData("fontname"), 15);

				using (Graphics graphics = control.CreateGraphics())
				{
					int margin = 25;
					SizeF fontSize = graphics.MeasureString(Message, font);
					size = new Size ((2 * margin) + (int) fontSize.Width, control.Height);
				}
			}

			public Size GetSize ()
			{
				return size;
			}

			float SquareWave (float t, float tHighStart, float timeOn, float timeOff, float onValue, float offValue)
			{

				float period = timeOn + timeOff;
				t -= tHighStart;

				while (t >= period)
				{
					t -= period;
				}
				while (t < 0)
				{
					t += period;
				}

				if (t <= timeOn)
				{
					return onValue;
				}
				return offValue;
			}

			Color MultiplyColour (Color a, float f)
			{
				return Color.FromArgb((int) (a.R * f), (int) (a.G * f), (int) (a.B * f));
			}

			public override string ToString ()
			{
				return LibCore.CONVERT.Format("\"{0}\"", Message);
			}

			public void Paint (Point location, RenderItemStyle style, float timer, PaintEventArgs e)
			{
				Color colourToUse;

				float bright = 1.0f;
				float mid = 0.7f;
				float dark = 0.5f;
				
				switch (style)
				{
					case RenderItemStyle.New:
						{
							colourToUse = MultiplyColour(Colour, SquareWave(timer, 0, 0.75f, 0.5f, bright, mid));
							break;
						}

					case RenderItemStyle.Recent:
						{
							colourToUse = MultiplyColour(Colour, mid);
							break;
						}

					case RenderItemStyle.Normal:
					default:
						colourToUse = MultiplyColour(Colour, dark);
						break;
				}

				using (Brush brush = new SolidBrush (colourToUse))
				{
					RectangleF rectangle = new RectangleF (location.X, location.Y, size.Width, size.Height);

					StringFormat format = new StringFormat ();
					format.Alignment = StringAlignment.Center;
					format.LineAlignment = StringAlignment.Center;

					e.Graphics.DrawString(Message, font, brush, rectangle, format);
				}
			}
		}

		class ItemHistory
		{
			public int TimesShown;
			public float TimeSinceFirstAppeared;
			public float TimeSinceLastAppeared;

			public ItemHistory ()
			{
				TimesShown = 0;
				TimeSinceFirstAppeared = 0;
				TimeSinceLastAppeared = 0;
			}

			public override string ToString ()
			{
				return LibCore.CONVERT.Format("t={0}", TimeSinceFirstAppeared);
			}
		}

		public enum RenderItemStyle
		{
			New,
			Recent,
			Normal
		}

		class RenderItem
		{
			public ITickerTapeItem Item;
			public Point Location;
			public RenderItemStyle Style;

			public RenderItem (ITickerTapeItem item)
			{
				Item = item;

				Location = new Point (0, 0);
				Style = RenderItemStyle.Normal;
			}
		}

		List<ITickerTapeItem> items;
		Dictionary<ITickerTapeItem, ItemHistory> itemToHistory;
		List<RenderItem> renderingItems;

		Timer timer;
		float scrollSpeed;
		int itemIndexToShowNext;

		public float ScrollSpeed
		{
			get
			{
				return scrollSpeed;
			}

			set
			{
				scrollSpeed = value;

				// Don't update too frequently!
				int minInterval = 20;
				timer.Interval = Math.Max(minInterval, (int) (1000 / scrollSpeed));
			}
		}

		public TickerTape ()
		{
			items = new List<ITickerTapeItem> ();
			itemToHistory = new Dictionary<ITickerTapeItem, ItemHistory> ();
			renderingItems = new List<RenderItem> ();

			itemIndexToShowNext = 0;

			timer = new Timer ();
			timer.Tick += timer_Tick;
			timer.Start();
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing)
			{
				timer.Dispose();
			}

			base.Dispose(disposing);
		}

		public void AddItem (ITickerTapeItem item)
		{
			items.Add(item);
			itemToHistory[item] = new ItemHistory ();
		}

		public void RemoveItem (ITickerTapeItem item)
		{
			items.Remove(item);
		}

		void timer_Tick (object sender, EventArgs e)
		{
			float deltaTime = timer.Interval / 1000.0f;
			int scrollAmount = (int) (scrollSpeed * deltaTime);

			int rightmostEdge = 0;

			foreach (ITickerTapeItem item in items)
			{
				itemToHistory[item].TimeSinceFirstAppeared += deltaTime;
				itemToHistory[item].TimeSinceLastAppeared += deltaTime;
			}

			// Scroll every shown item leftwards.
			List<RenderItem> renderItemsToRemove = new List<RenderItem> ();
			foreach (RenderItem renderItem in renderingItems)
			{
				renderItem.Location = new Point (renderItem.Location.X - scrollAmount, renderItem.Location.Y);

				int itemRightEdge = renderItem.Location.X + renderItem.Item.GetSize().Width;

				rightmostEdge = Math.Max(rightmostEdge, itemRightEdge);

				if (itemRightEdge <= 0)
				{
					renderItemsToRemove.Add(renderItem);
				}
			}
			foreach (RenderItem renderItem in renderItemsToRemove)
			{
				renderingItems.Remove(renderItem);
			}

			// Is it time to bring a new item on at the right?
			if ((rightmostEdge <= Width) && (items.Count > 0))
			{
				itemIndexToShowNext = itemIndexToShowNext % items.Count;

				ITickerTapeItem item = items[itemIndexToShowNext];
				itemIndexToShowNext = (itemIndexToShowNext + 1) % items.Count;

				RenderItem renderItem = new RenderItem (item);
				renderItem.Location = new Point (Width, (Height - item.GetSize().Height) / 2);

				itemToHistory[item].TimeSinceLastAppeared = 0;
				if (itemToHistory[item].TimesShown == 0)
				{
					itemToHistory[item].TimeSinceFirstAppeared = 0;
					renderItem.Style = RenderItemStyle.New;
				}
				else if (itemToHistory[item].TimesShown < 5)
				{
					renderItem.Style = RenderItemStyle.Recent;
				}
				else
				{
					renderItem.Style = RenderItemStyle.Normal;
				}

				itemToHistory[renderItem.Item].TimesShown++;

				renderingItems.Add(renderItem);
			}

			Invalidate();
		}

		protected override void OnPaint (PaintEventArgs e)
		{
			base.OnPaint(e);

			foreach (RenderItem renderItem in renderingItems)
			{
				renderItem.Item.Paint(renderItem.Location, renderItem.Style, itemToHistory[renderItem.Item].TimeSinceFirstAppeared, e);
			}
		}
	}
}