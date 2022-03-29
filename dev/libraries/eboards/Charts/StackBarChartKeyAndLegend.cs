using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using CommonGUI;

namespace Charts
{
	public class StackBarChartKeyAndLegend : FlickerFreePanel, ICategoryCollector
	{
		List<Category> categories;

		Point? mouseHoverLocation;

		protected Font legendFont;
		float legendFontSize;
		public float LegendFontSize
		{
			get
			{
				return legendFontSize;
			}

			set
			{
				legendFontSize = value;
				legendFont = CoreUtils.SkinningDefs.TheInstance.GetFont(legendFontSize);
			}
		}

		protected string legend;
		protected bool showMouseAnnotations = true;

		protected int keyX;
		public int KeyX
		{
			get
			{
				return keyX;
			}

			set
			{
				keyX = value;
				Invalidate();
			}
		}

		public StackBarChartKeyAndLegend(List<Category> _categories)
		{
			categories = _categories;
			LegendFontSize = 9f;

			BackColor = Color.Black;
			ForeColor = Color.White;
		}

		public void setShowMouseNotes(bool show_mouse_notes)
		{
			showMouseAnnotations = show_mouse_notes;
		}

		public Category GetCategoryByName(string name)
		{
			foreach (Category category in categories)
			{
				if (category.Name == name)
				{
					return category;
				}
			}

			return null;
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			// Legend.
			//e.Graphics.DrawString(legend, legendFont, Brushes.Black, (float)legendX, (float)legendY);

			double key_height = 0;
			Point last_KeyItem_position = new Point(0, 0);

			double x = keyX;
			double y = 0;
			SizeF maxLegendSize = new SizeF(0, 0);
			foreach (Category category in categories)
			{
				if (category.ShowInKey)
				{
					SizeF legendSize = e.Graphics.MeasureString(category.Legend, legendFont);
					maxLegendSize = new SizeF(Math.Max(maxLegendSize.Width, legendSize.Width),
													Math.Max(maxLegendSize.Height, legendSize.Height));
				}
			}

			double rectangleSize = maxLegendSize.Height;
			double gap = 5;
			SizeF legendBlockSize = new SizeF((float)(rectangleSize + (2 * gap) + maxLegendSize.Width), maxLegendSize.Height);

			foreach (Category category in categories)
			{
				if (category.ShowInKey)
				{
					Rectangle colourOutlineBounds = new Rectangle((int)x, (int)y,
																		(int)rectangleSize, (int)rectangleSize);
					Rectangle colourFillBounds = new Rectangle((int)(colourOutlineBounds.Left + category.BorderInset),
																(int)(colourOutlineBounds.Top + category.BorderInset),
																(int)((int)rectangleSize - (2 * category.BorderInset)) + 1,
																(int)((int)rectangleSize - (2 * category.BorderInset)) + 1);

					e.Graphics.FillRectangle(Brushes.White, colourOutlineBounds);

					using (Pen pen = new Pen(category.BorderColour, (float)category.BorderThickness))
					{
						e.Graphics.DrawRectangle(pen, colourOutlineBounds);
					}

					using (Brush brush = new SolidBrush(category.Colour))
					{
						e.Graphics.FillRectangle(brush, colourFillBounds);
					}

					Rectangle textBounds = new Rectangle((int)(colourOutlineBounds.Right + gap),
															(int)y, (int)(legendBlockSize.Width - (colourOutlineBounds.Right + gap - colourOutlineBounds.Left)), (int)legendBlockSize.Height);
					StringFormat format = new StringFormat();
					format.Alignment = StringAlignment.Near;
					format.LineAlignment = StringAlignment.Center;

					using (Brush textBrush = new SolidBrush (ForeColor))
					{
						e.Graphics.DrawString(category.Legend, legendFont, textBrush, textBounds, format);
					}

					x += legendBlockSize.Width;
					if ((x + legendBlockSize.Width) >= Width)
					{
						x = keyX;
						y += legendBlockSize.Height + gap;
						key_height = y;
					}
					last_KeyItem_position.X = (int)x;
					last_KeyItem_position.Y = (int)y;
				}
			}
		}
	

		protected override void OnMouseLeave(EventArgs e)
		{
			base.OnMouseEnter(e);
			mouseHoverLocation = null;
			Invalidate();
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			mouseHoverLocation = e.Location;
			Invalidate();
		}
	}
}