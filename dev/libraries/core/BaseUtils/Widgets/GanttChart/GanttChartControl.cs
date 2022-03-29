using System;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using LibCore;

namespace BaseUtils
{
	/// <summary>
	/// 
	/// </summary>
	public enum Interval
	{
		Mins,
		FiveMins,
		FifteenMins,
		HalfHour,
		Hour
	}

	/// <summary>
	/// A UserContol for displaying interactive
	/// Gantt Charts.
	/// </summary>
	public class GanttChartControl : UserControl
	{
		InfoBox ibox;
		Graphics g;
		Font font = ConstantSizeFont.NewFont("Tahoma", 10f);
		Font fontB = ConstantSizeFont.NewFont("Tahoma", 10f, FontStyle.Bold);

		int padRight;
		int padTop;
		int padBottom;
		int startPeriod;
		int endPeriod;
		Interval majorInt;
		Interval minorInt = Interval.Mins;
		int[] secsPerInterval = (new int[] {60, 300, 900, 1800, 3600});
		GanttBar mouseBar = null;
		GanttRowCollection ganttData;

		int padL;
		int availableWidth;
		int availableHeight;
		int visibleSeconds;
		int tableHeight;
		float pixPerSec;
		float barHeight = 26;

		RectangleF currColumn = new RectangleF();

		void InitializeComponent()
		{
			// 
			// GanttUserControl
			// 

			this.BackColor = System.Drawing.Color.White;
			this.Name = "GanttChartControl";
			this.Size = new System.Drawing.Size(272, 280);
		}
	
		/// <summary>
		/// Creates an instance of GanttChartControl.
		/// </summary>
		public GanttChartControl()
		{
			InitializeComponent();

			ganttData = new GanttRowCollection();

			RecalcBounds();
			g = Graphics.FromHwnd(this.Handle);
			this.SetStyle(ControlStyles.UserPaint | ControlStyles.DoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
			ibox = new InfoBox(g);
			this.Invalidate();
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				g.Dispose();
				font.Dispose();
				fontB.Dispose();
			}
			base.Dispose (disposing);
		}

		/// <summary>
		/// Returns a collection of GanttRows.
		/// </summary>
		public GanttRowCollection GanttRows
		{
			get { return ganttData; }
		}

		/// <summary>
		/// The right boundry of the plot area.
		/// </summary>
		public int PadRight
		{
			get { return padRight; }
			set 
			{ 
				padRight = value;
				RecalcBounds();
			}
		}

		/// <summary>
		/// The top boundry of the plot area.
		/// </summary>
		public int PadTop
		{
			get { return padTop; }
			set 
			{
				padTop = value; 
				RecalcBounds();
			}
		}

		/// <summary>
		/// The bottom boundry of the plot area.
		/// </summary>
		public int PadBottom
		{
			get { return padBottom; }
			set 
			{
				padBottom = value; 
				RecalcBounds();
			}
		}

		/// <summary>
		/// The major interval for the time-series.
		/// </summary>
		public Interval TimeMajorInterval
		{
			get { return majorInt; }
			set 
			{
				majorInt = value; 
			}
		}

		/// <summary>
		/// The minor interval for the time-series.
		/// </summary>
		public Interval TimeMinorInterval
		{
			get { return minorInt; }
			set 
			{
				minorInt = value; 
			}
		}

		/// <summary>
		/// The start of the period in time we're interested in. In seconds
		/// since midnight.
		/// </summary>
		public int StartPeriod
		{
			get { return startPeriod; }
			set 
			{ 
				startPeriod = value; 
				RecalcBounds();
			}
		}

		/// <summary>
		/// The end of the period in time we're interested in. In seconds
		/// since midnight.
		/// </summary>
		public int EndPeriod
		{
			get { return endPeriod; }
			set 
			{ 
				endPeriod = value; 
				RecalcBounds();
			}
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			DrawBars(e.Graphics);
			if (mouseBar != null)
				ibox.Paint(e.Graphics);
			base.OnPaint (e);
		}

		protected override void OnResize (EventArgs e)
		{
			if (this.Width < 500) this.Width = 500;
			if (this.Height < 380) this.Height = 390;
			RecalcBounds();
			Invalidate();
			base.OnResize (e);
		}

		/// <summary>
		/// Recalculates the bounds of the GanttChartControl.
		/// </summary>
		public void RecalcBounds()
		{
			if (ganttData != null)
			{
				padL = ganttData.CalcMaxPadLeft(g, font);
				//barHeight = (this.Height - padTop - padBottom) / (ganttData.Count + 1);
				tableHeight = (int)(ganttData.Count * barHeight);
			}
			else
			{
				padL = 0;
				barHeight = 26;
				tableHeight = (int)barHeight * 10;
			}

			availableWidth = this.Width - (padL + padRight);
			availableHeight = this.Height - (padTop + padBottom);
			visibleSeconds = endPeriod - startPeriod;
			pixPerSec = (float)availableWidth / visibleSeconds;
			BackgroundImage = new Bitmap (this.Width, this.Height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
			DrawBackground(Graphics.FromImage(BackgroundImage));
		}

		/// <summary>
		/// Draws the background of the GanttChartControl.
		/// </summary>
		/// <param name="g"></param>
		void DrawBackground(Graphics g)
		{
			Pen pen = new Pen(Brushes.Black, 1);
			Pen lgPen = new Pen(Brushes.LightGray);
			int secs = secsPerInterval[(int)majorInt];

			if (visibleSeconds <= 0)
				return;

			try
			{
				g.SmoothingMode = SmoothingMode.AntiAlias;
				g.Clear(Color.White);

				SizeF size;
				string text;
				int ypos = 0;
				int count = 0;
				int x;

				// draw the background of the chart
				ypos = padTop;
				foreach (GanttBarCollection bars in ganttData.Values)
				{
					SolidBrush clipBrush = new SolidBrush(Color.FromArgb(242,242,242));
					g.FillRectangle(clipBrush, new Rectangle(padL, ypos, availableWidth, (int)barHeight - 2));
					text = bars.Title;
					size = g.MeasureString(text, font);

					StringFormat sf = new StringFormat();
					sf.Alignment = StringAlignment.Far;
					sf.LineAlignment = StringAlignment.Center;
					Rectangle rect = new Rectangle(0, ypos + 4, padL - 10, (int)size.Height);

					g.DrawString(text, font, Brushes.Black, rect, sf);
					g.DrawLine(pen, padL - 5, ypos, padL, ypos);
					ypos += (int)barHeight;
				}

				// draw the minor ticks
				x = 0;
				count = 1;
				int max = visibleSeconds / secsPerInterval[(int)minorInt];

				while (count < max)
				{
					x = padL + (int)(pixPerSec * count * secsPerInterval[(int)minorInt]);
					g.DrawLine(lgPen, x, padTop, x, padTop + tableHeight - 1);
					count++;
				}

				// draw the major ticks and text
				x = 0;
				count = 0;
				while (x <= (availableWidth + padL))
				{
					x = padL + (int)(pixPerSec * count * secsPerInterval[(int)majorInt]);
					g.DrawLine(pen, x, ypos, x, ypos + 5);
					text = SecsToTime((secsPerInterval[(int)majorInt] * count) + StartPeriod);
					size = g.MeasureString(text, font);
					g.DrawString(text, font, Brushes.Black, x - (size.Width / 2), ypos + 8);
					count++;
				}

				// draw border round the table
				g.DrawLine(pen, padL, padTop, padL, padTop + tableHeight);
				g.DrawLine(pen, padL, padTop + tableHeight, this.Width - padRight, padTop + tableHeight);
				g.DrawLine(pen, padL + availableWidth, padTop, padL + availableWidth, padTop + tableHeight);
				g.DrawLine(pen, padL, padTop, this.Width - padRight, padTop);

				text = "Service";

				size = g.MeasureString(text, fontB);
				StringFormat drawFormat = new StringFormat(StringFormatFlags.DirectionVertical);
				g.DrawString(text, fontB, Brushes.Black, 10, (availableHeight / 2) - (size.Width / 2), drawFormat);

				text = "Time";
				size = g.MeasureString(text, font);
				g.DrawString(text, fontB, Brushes.Black, padL + (availableWidth / 2) - (size.Width / 2), this.Height - padBottom);
			}
			finally
			{
				pen.Dispose();
				lgPen.Dispose();
			}
		}

		/// <summary>
		/// Draw the bars on the GanttChartControl.
		/// </summary>
		/// <param name="g"></param>
		void DrawBars(Graphics g)
		{
			Pen pen = new Pen(Brushes.Black, 1);
			Pen lgPen = new Pen(Brushes.LightGray);
			int secs = secsPerInterval[(int)majorInt];

			if (visibleSeconds <= 0)
				return;

			try
			{
				// draw the current column
				if (currColumn.Width > 0)
				{
					g.FillRectangle(new SolidBrush(Color.FromArgb(20, 0, 0, 0)), currColumn);
				}

				// draw the bars
				int ypos = padTop;
				int count = 0;

				foreach (GanttBarCollection bars in ganttData.Values)
				{
					foreach (GanttBar bar in bars)
					{
						int barStart = bar.Start;
						int barEnd = bar.Start + bar.Duration;

						if (barStart < StartPeriod)
							barStart = StartPeriod;

						if (barEnd > EndPeriod)
							barEnd = barEnd - (barEnd - EndPeriod);

						barStart -= StartPeriod;
						barEnd -= StartPeriod;

						float barW = (pixPerSec * (barEnd - barStart));
						float barX = (pixPerSec * barStart) + padL;
						float barY = ypos + 2;
						bar.Bounds = new RectangleF(barX, barY, barW, barHeight - 4);

						if ((barEnd - barStart) > 0)
						{						
							// Now the bar knows it's own
							// size and location we can use it to
							// draw the bar

							RectangleF gradRect = bar.Bounds;
							gradRect.Inflate(0, 10);
							LinearGradientBrush lBrush = new LinearGradientBrush(gradRect, bar.BarColorStart, bar.BarColorEnd, LinearGradientMode.Vertical); 
							g.FillRectangle(lBrush, bar.Bounds);
						}
					}

					ypos += (int)barHeight;
					count++;
				}
			}
			finally
			{
				pen.Dispose();
				lgPen.Dispose();
			}
		}

		/// <summary>
		/// Converts the specified number of seconds to
		/// hours:mins representation.
		/// </summary>
		/// <param name="secs"></param>
		/// <returns></returns>
		string SecsToTime(int secs)
		{
			int hours = secs / 3600;
			int temp = secs - (hours * 3600);
			int mins = temp / 60;

			return hours.ToString() + ":" + mins.ToString().PadLeft(2, '0');
		}

		/// <summary>
		/// Converts the specified number of seconds to
		/// hours:mins representation with 0-padding.
		/// </summary>
		/// <param name="secs"></param>
		/// <returns></returns>
		string SecsToTime2(int secs)
		{
			int hours = secs / 3600;
			int temp = secs - (hours * 3600);
			int mins = temp / 60;
			int seconds = temp - ( mins * 60 );

			return mins.ToString().PadLeft(2, '0') + ":" + seconds.ToString().PadLeft(2, '0');
		}

		/// <summary>
		/// Display the tool-tip and the selected column 
		/// for the GanttBar that the mouse is positioned over.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnMouseMove(MouseEventArgs e)
		{
			mouseBar = null;

			foreach (GanttBarCollection bars in ganttData.Values)
			{
				if (bars.Count > 0)
				{
					GanttBar testBar = bars[0];
					RectangleF testRect = new RectangleF(0, testBar.Bounds.Y, this.Width, barHeight);
					if (!testRect.Contains(e.X, e.Y))
						continue;
				}
				
				foreach (GanttBar bar in bars)
				{
					if (bar.Bounds.Contains(e.X, e.Y))
					{
						mouseBar = bar;
						ibox.Update(bar.Desc, this.Bounds, e.X, e.Y);
					}
				}

				if (mouseBar != null)
					break;
			}

			if (e.X >= padL && e.X <= padL + availableWidth - 1 && e.Y >= padTop && e.Y <= padTop + tableHeight - 1 && !currColumn.Contains(e.X, e.Y))
			{
				int startPixel = e.X - this.padL;
				int temp = startPeriod + (int)(startPixel / this.pixPerSec);
				int minors = (int)temp / secsPerInterval[(int)minorInt];
				int period = minors * secsPerInterval[(int)minorInt];
				int costPeriod = period + (secsPerInterval[(int)minorInt]) - 60;

				currColumn.X = this.padL + (int)((period - startPeriod) * this.pixPerSec) + 1;
				currColumn.Y = padTop + 1;
				currColumn.Width = (int)(secsPerInterval[(int)minorInt] * this.pixPerSec);
				currColumn.Height = tableHeight - 1;

				Invalidate();
			}

			Invalidate(ibox.LastRect);

			base.OnMouseMove (e);
		}

		/// <summary>
		/// Clear the current column indicator.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnMouseLeave(EventArgs e)
		{
			currColumn = new RectangleF();
			Invalidate();
			base.OnMouseLeave (e);
		}

		/// <summary>
		/// Add a Gantt Bar to the collection of bars.
		/// </summary>
		/// <param name="round">The current round.</param>
		/// <param name="configId">The current ConfigId.</param>
		/// <param name="desc">The description of the ConfigItem</param>
		/// <param name="startPeriod">The start period in seconds.</param>
		/// <param name="duration">The duration in seconds.</param>
		/// <param name="cost">The cost.</param>
		/// <param name="barColor">The colour of the bar.</param>
		public void AddBar(int round, string configId, string desc, int startPeriod, int duration, int cost, Color barColor)
		{
			GanttBarCollection bars = ganttData[configId];

			if (bars != null)
			{
				bars.Add(new GanttBar(desc, startPeriod, duration, cost, barColor));
			}
		}
	}

	/// <summary>
	/// Represents a tool-tip style window.
	/// </summary>
	public class InfoBox : IDisposable
	{
		Graphics mg;
		Rectangle rect;
		Rectangle lastRect;
		string text = "";
		Font font = ConstantSizeFont.NewFont("Tahoma", 8, FontStyle.Regular);

		/// <summary>
		/// Creates an instance of InfoBox.
		/// </summary>
		/// <param name="g"></param>
		public InfoBox(Graphics g)
		{
			this.rect = new Rectangle();
			this.mg = g;
		}

		/// <summary>
		/// Updates the InfoBox.
		/// </summary>
		/// <param name="text"></param>
		/// <param name="bounds"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		public void Update(string text, Rectangle bounds, int x, int y)
		{
			this.text = text;
			SizeF temp = mg.MeasureString(text, font);
			lastRect = rect;
			lastRect.Inflate(50, 50);
			rect.Width = (int)temp.Width + 4;
			rect.Height = (int)temp.Height + 4;
			rect.Y = y - rect.Height - 10;

			if (x + (rect.Width/2) >= bounds.Width - 2)
				rect.X = (bounds.Width - rect.Width - 2);
			else
				rect.X = x - (rect.Width/2);
		}

		public void Reset()
		{
			lastRect.X = 0;
			lastRect.Y = 0;
			lastRect.Width = 0;
			lastRect.Height = 0;
		}

		public void Paint(Graphics g)
		{
			g.SmoothingMode = SmoothingMode.None;
			g.FillRectangle(new SolidBrush(Color.FromArgb(200, 255, 255, 255)), rect);
			g.DrawRectangle(new Pen(Brushes.Black, 1f), rect);
			g.DrawString(text, font, Brushes.Black, rect.X + 2, rect.Y + 2);
		}

		public Rectangle LastRect
		{
			get { return lastRect; }
		}

		public void Dispose()
		{
			mg.Dispose();
			font.Dispose();
		}
	}
}
