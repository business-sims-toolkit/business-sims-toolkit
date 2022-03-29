using System.Collections;
using System.Drawing;

namespace BaseUtils
{
	/// <summary>
	/// A collection of Category instances.
	/// </summary>
	public class CategoryCollection : CollectionBase
	{
		ChartContainer container;
		bool drawKey;

		/// <summary>
		/// Creates an instance of CategoryCollection.
		/// </summary>
		public CategoryCollection(ChartContainer container)
		{
			this.container = container;
			this.drawKey = false;
		}

		public Category this[int index]
		{
			get { return base.List[index] as Category; }
			set { base.List[index] = value; }
		}

		public int Add(Category cat)
		{
			cat.Index = this.Count;
			return base.List.Add(cat);
		}

		public void Remove(Category cat)
		{
			base.List.Remove(cat);
		}

		public void Draw()
		{
			if (!drawKey)
				return;

			float maxKeyWidth = 0;
			float maxKeyHeight = 0;

			foreach (Category cat in base.List)
			{
				SizeF s = container.MeasureString(cat.Label, container.CurrentFont);
				if (s.Width > maxKeyWidth)
				{
					maxKeyWidth = s.Width;
					maxKeyHeight = s.Height;
				}
			}

			if (maxKeyWidth > 0)
			{
				float width = 18 + maxKeyWidth;
				float height = base.List.Count * (maxKeyHeight + 2);
				int x = (int)(container.PlotBounds.X + container.PlotBounds.Width - width - 20);
				int y = (int)(container.PlotBounds.Y + 10);

				Rectangle borderRect = new Rectangle(x - 5, y - 5, (int)width + 10, (int)height + 5);
				container.CurrentPen = new Pen(Color.Black, 1f);
				container.CurrentBrush = Brushes.White;
				container.FillRectangle(borderRect.X, borderRect.Y, borderRect.Width, borderRect.Height);
				container.DrawRectangle(borderRect.X, borderRect.Y, borderRect.Width, borderRect.Height);

				foreach (Category cat in base.List)
				{
					Rectangle rect = new Rectangle(x, (int)y, 12, 12);
					container.CurrentBrush = new SolidBrush(cat.Color);
					container.FillRectangle(rect.X, rect.Y, rect.Width, rect.Height);
					container.DrawRectangle(rect.X, rect.Y, rect.Width, rect.Height);
					container.CurrentBrush = Brushes.Black;
					container.DrawText(x + 18, y, cat.Label);
					y += (int)(maxKeyHeight + 2);
				}
			}
		}

		/// <summary>
		/// Determines whether or not to draw the key.
		/// </summary>
		public bool DrawKey
		{
			get { return drawKey; }
			set { drawKey = value; }
		}
	}
}
