using System.Drawing;

using LibCore;

namespace BaseUtils
{
	/// <summary>
	/// Defines how to aligin text.
	/// </summary>
	public enum TextAlignment
	{
		Left,
		Center,
		Right,
		Justify
	}

	/// <summary>
	/// Abstract base class which acts as a container for Chart implementations.
	/// Charts are drawn using primitives, the details of which are handled by sub-class
	/// implementations. In this way, the same chart can be drawn to a Window or to a PDF
	/// document, or to any other medium.
	/// </summary>
	public abstract class ChartContainer
	{
		protected Point location;
		protected Size size;
		protected Axis leftAxis;
		protected Axis rightAxis;
		protected Axis bottomAxis;
		protected ChartCollection charts;
		protected int padTop;
		protected int padBottom;
		protected int padLeft;
		protected int padRight;
		protected Brush currentBrush;
		protected Pen currentPen;
		protected Font currentFont;
		protected string title;

		/// <summary>
		/// Creates an instance of ChartContainer.
		/// </summary>
		public ChartContainer()
		{
			leftAxis = new LeftAxis(this);
			rightAxis = new RightAxis(this);
			rightAxis.DrawAxis = false;
			bottomAxis = new BottomAxis(this);
			charts = new ChartCollection();

			currentBrush = Brushes.Black;
			currentPen = new Pen(currentBrush, 1f);
			currentFont = ConstantSizeFont.NewFont("Tahoma", 8f);

			location = new Point(0, 0);
			size = new Size(200, 150);

			padTop = 20;
			padBottom = 20;
			padLeft = 20;
			padRight = 20;
		}

		/// <summary>
		/// Draws everything contained within the container.
		/// </summary>
		public virtual void Draw()
		{
			DrawTitle();
			DrawAxes();
			DrawCharts();
		}

		/// <summary>
		/// Uses primitives to draw the title of the chart.
		/// </summary>
		protected virtual void DrawTitle()
		{
			DrawText(padLeft, location.Y, title, PlotWidth, TextAlignment.Left);
		}

		/// <summary>
		/// Uses primitives to draw the Axes for the chart.
		/// </summary>
		protected virtual void DrawAxes()
		{
			DrawRectangle(location.X + padLeft, location.Y + padTop, PlotWidth, PlotHeight);

			if (leftAxis != null)
				leftAxis.Draw();

			if (bottomAxis != null)
				bottomAxis.Draw();

			if (rightAxis != null)
				rightAxis.Draw();
		}

		/// <summary>
		/// Draws all of the charts.
		/// </summary>
		protected virtual void DrawCharts()
		{
			foreach (Chart chart in charts)
			{
				if (chart.Axis.DrawAxis)
					chart.Draw(this);
			}
		}

		/// <summary>
		/// Converts X and Y values into chart coordinates.
		/// </summary>
		/// <param name="axis">The Axis used to convert the Y value.</param>
		/// <param name="x">The X value.</param>
		/// <param name="y">The Y value.</param>
		/// <returns>RectangleF</returns>
		public RectangleF ValueToBounds(Axis axis, float x, float y)
		{
			float xp = bottomAxis.ChartUnits(x);
			float yp = axis.ChartUnits(y);
			float w = bottomAxis.ChartUnits(1);
			return new RectangleF(xp, yp, w, 1f);
		}

		/// <summary>
		/// Primitive for drawing a line.
		/// </summary>
		/// <param name="x1"></param>
		/// <param name="y1"></param>
		/// <param name="x2"></param>
		/// <param name="y2"></param>
		public virtual void DrawLine(float x1, float y1, float x2, float y2)
		{
		}

		/// <summary>
		/// Primitive for drawing a rectangle.
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		public virtual void DrawRectangle(float x, float y, float width, float height)
		{
		}

		/// <summary>
		/// Primitive for filling a rectangle.
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		public virtual void FillRectangle(float x, float y, float width, float height)
		{
		}

		/// <summary>
		/// Primitive for filling a circle.
		/// </summary>
		/// <param name="cx"></param>
		/// <param name="cy"></param>
		/// <param name="radius"></param>
		public virtual void FillCircle(float cx, float cy, float radius)
		{
		}

		/// <summary>
		/// Primitive for drawing text.
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="text"></param>
		public void DrawText(float x, float y, string text)
		{
			DrawText(x, y, text, 0);
		}

		/// <summary>
		/// Primitive for drawing text.
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="text"></param>
		/// <param name="maxWidth"></param>
		public void DrawText(float x, float y, string text, float maxWidth)
		{
			DrawText(x, y, text, maxWidth, TextAlignment.Left);
		}

		/// <summary>
		/// Primitive for drawing text.
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="text"></param>
		/// <param name="maxWidth"></param>
		/// <param name="alignment"></param>
		public virtual void DrawText(float x, float y, string text, float maxWidth, TextAlignment alignment)
		{
		}

		/// <summary>
		/// Primitive for drawing vertical text.
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="text"></param>
		public virtual void DrawVerticalText(float x, float y, string text)
		{
		}

		/// <summary>
		/// Primitive for drawing images.
		/// </summary>
		/// <param name="img"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		public virtual void DrawImage(Image img, float x, float y, float width, float height)
		{
		}

		/// <summary>
		/// Primitive for measuring the size of the specified string in pixels.
		/// </summary>
		/// <param name="text"></param>
		/// <param name="font"></param>
		/// <returns></returns>
		public virtual SizeF MeasureString(string text, Font font)
		{
			return new SizeF(0, 0);
		}

		/// <summary>
		/// The location of the ChartContainer.
		/// </summary>
		public Point Location
		{
			get { return location; }
			set { location = value; }
		}

		/// <summary>
		/// The size of the ChartContainer.
		/// </summary>
		public Size Size
		{
			get { return size; }
			set { size = value; }
		}

		/// <summary>
		/// The width of the plot area in the container.
		/// </summary>
		public int PlotWidth
		{
			get { return size.Width - padLeft - padRight; }
		}

		/// <summary>
		/// The height of the plot area in the container.
		/// </summary>
		public int PlotHeight
		{
			get { return size.Height - padTop - padBottom; }
		}

		/// <summary>
		/// The bounds of the plot area in the container.
		/// </summary>
		public Rectangle PlotBounds
		{
			get { return new Rectangle(location.X + padLeft, location.Y + padTop, PlotWidth, PlotHeight); }
		}

		/// <summary>
		/// The padding between the top of the container and the top of the plot area.
		/// </summary>
		public int PadTop
		{
			get { return padTop; }
			set { padTop = value; }
		}

		/// <summary>
		/// The padding between the bottom of the container and the bottom of the plot area.
		/// </summary>
		public int PadBottom
		{
			get { return padBottom; }
			set { padBottom = value; }
		}

		/// <summary>
		/// The padding between the left of the container and the left of the plot area.
		/// </summary>
		public int PadLeft
		{
			get { return padLeft; }
			set { padLeft = value; }
		}

		/// <summary>
		/// The padding between the right of the container and the right of the plot area.
		/// </summary>
		public int PadRight
		{
			get { return padRight; }
			set { padRight = value; }
		}

		/// <summary>
		/// The title of the container.
		/// </summary>
		public string Title
		{
			get { return title; }
			set { title = value; }
		}

		/// <summary>
		/// The brush used to draw primitives.
		/// </summary>
		public Brush CurrentBrush
		{
			get { return currentBrush; }
			set { currentBrush = value; }
		}

		/// <summary>
		/// The pen used to draw primitives.
		/// </summary>
		public Pen CurrentPen
		{
			get { return currentPen; }
			set { currentPen = value; }
		}

		/// <summary>
		/// The font used to draw primitives.
		/// </summary>
		public Font CurrentFont
		{
			get { return currentFont; }
			set { currentFont = value; }
		}

		/// <summary>
		/// The left axis of the container.
		/// </summary>
		public Axis LeftAxis
		{
			get { return leftAxis; }
			set { leftAxis = value; }
		}

		/// <summary>
		/// The right axis of the container.
		/// </summary>
		public Axis RightAxis
		{
			get { return rightAxis; }
			set { rightAxis = value; }
		}

		/// <summary>
		/// The bottom axis of the container.
		/// </summary>
		public Axis BottomAxis
		{
			get { return bottomAxis; }
			set { bottomAxis = value; }
		}

		/// <summary>
		/// The collection of charts in the container.
		/// </summary>
		public ChartCollection Charts
		{
			get { return charts; }
		}
	}
}
