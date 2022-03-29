using System.Drawing;
using System.Drawing.Drawing2D;

namespace BaseUtils
{
	/// <summary>
	/// Extends Chart to provide an implementation that
	/// draws a Bar Chart.
	/// </summary>
	public class BarChart : Chart
	{
		bool gradientBars;

		/// <summary>
		/// Creates an instance of BarChart.
		/// </summary>
		/// <param name="axis">The Axis to use for the Bar Chart.</param>
		/// <param name="series">The Series to use for the Bar Chart.</param>
		public BarChart(Axis axis, Series series) : base(axis, series)
		{
		}

		/// <summary>
		/// Draws the Bar Chart.
		/// </summary>
		/// <param name="container">The parent ChartContainer.</param>
		public override void Draw(ChartContainer container)
		{
			if (series.Values.Count == 0)
				return;

			Brush serBrush = new SolidBrush(series.Color);

			for (int i = 0; i < container.BottomAxis.Categories.Count; i++)
			{
				Category cat = container.BottomAxis.Categories[i];

				float val = 0;

				if (cat.Index < series.Values.Count)
				{
					val = series.Values[cat.Index].Value;
				}

				if (val != 0)
				{
					float y = axis.ChartUnits(val);
					float x1 = container.BottomAxis.ChartUnits(cat.Index);
					float x2 = container.BottomAxis.ChartUnits(cat.Index + 1);
					float width = x2 - x1 - 4f;
					float height = axis.ChartUnits(0f) - y;
					RectangleF rect = new RectangleF(x1 + 2f, y, width, height);

					if (gradientBars)
					{
						// Create a gradient brush
						RectangleF brushRect = new RectangleF(rect.X, rect.Y, rect.Width, rect.Height);
						brushRect.Inflate(10, 10);
						container.CurrentBrush = new LinearGradientBrush(brushRect, cat.Color, Darker(cat.Color), LinearGradientMode.Horizontal);
					}
					else
					{
						// Create a solid brush
						container.CurrentBrush = new SolidBrush(cat.Color);
					}

					// Draw the bar
					container.FillRectangle(rect.X, rect.Y, rect.Width, rect.Height);

					// Display the value above the bar
					if (series.ShowValues)
					{
						container.CurrentBrush = serBrush;
						container.DrawText(rect.X, rect.Y - 22, val.ToString(), rect.Width, TextAlignment.Center);
					}

					// Display the image in the center of the bar
					if (cat.Image != null)
					{
						if (rect.Height > (cat.Image.Height + 20))
						{
							container.DrawImage(cat.Image, rect.X + (rect.Width - cat.Image.Width) / 2, rect.Y + (rect.Height - cat.Image.Height) / 2, cat.Image.Width, cat.Image.Height);
						}
					}
				}
			}
		}

		Color Darker(Color c)
		{
			byte r, g, b;
			r = (byte)(c.R * 0.75);
			g = (byte)(c.G * 0.75);
			b = (byte)(c.B * 0.75);
			return Color.FromArgb(255, r, g, b);
		}

		/// <summary>
		/// Whether or not to draw bars using a gradient.
		/// </summary>
		public bool GradientBars
		{
			get { return gradientBars; }
			set { gradientBars = value; }
		}
	}
}
