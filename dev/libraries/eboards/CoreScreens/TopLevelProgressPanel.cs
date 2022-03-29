using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Algorithms;

namespace CoreScreens
{
	public class TopLevelProgressPanel : Panel
	{
		int maxValue;
		int value;

		public TopLevelProgressPanel ()
		{
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);
			Invalidate();
		}

		public int ProgressMax
		{
			get => maxValue;

			set
			{
				maxValue = value;
				Invalidate();
			}
		}

		public int ProgressCount
		{
			get => value;

			set
			{
				this.value = value;
				Invalidate();
			}
		}

		protected override void OnPaint (PaintEventArgs e)
		{
			base.OnPaint(e);

			var text = $"Exporting report {value} / {maxValue}";
			var margin = Width / 5;
			var leading = 50;
			var textBounds = new RectangleF (margin, leading, Width - (2 * margin), (Height / 2) - leading);
			using (var font = ResizingUi.FontScalerExtensions.GetFontToFit(this, FontStyle.Bold, text, textBounds.Size))
			{
				e.Graphics.DrawString(text, font, Brushes.Black, textBounds, new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
			}

			var outerRectangle = new Rectangle ((int) textBounds.Left, (int) (textBounds.Bottom + leading), (int) (textBounds.Width), (int) (Height - leading - (textBounds.Bottom + leading)));
			var thickness = 10;
			var innerRectangle = new Rectangle (outerRectangle.Left + thickness, outerRectangle.Top + thickness, outerRectangle.Width - (2 * thickness), outerRectangle.Height - (2 * thickness));
			var barRectangle = new Rectangle(innerRectangle.Left, innerRectangle.Top, (int) Maths.MapBetweenRanges(value, 0, maxValue, 0, innerRectangle.Width) , innerRectangle.Height);

			e.Graphics.DrawRectangle(Pens.Black, outerRectangle);
			e.Graphics.FillRectangle(Brushes.Green, barRectangle);
		}
	}
}