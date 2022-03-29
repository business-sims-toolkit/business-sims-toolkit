using System.Drawing;
using System.Windows.Forms;

namespace Charts
{
    public abstract class DestinationDependentGraphics
    {
    }

    public class WindowsGraphics : DestinationDependentGraphics
    {
        public Graphics Graphics;
		public Control theControl;
    }

	public abstract class TableCell
	{
		protected ContentAlignment contentAlignment = ContentAlignment.MiddleCenter;
		protected Brush backBrush = null;
		protected Brush foreBrush = Brushes.Black;

		protected int top;
		protected int left;
		protected int height;
		protected int width;

		protected string toolTipText = "";

		public string ToolTipText
		{
			get
			{
				return toolTipText;
			}

			set
			{
				toolTipText = value;
			}
		}

		PureTable table;
		public PureTable Table
		{
			get
			{
				return table;
			}
		}

		protected bool editable = false;

		public bool Editable
		{
			get
			{
				return editable;
			}

			set
			{
				editable = value;
			}
		}

		public TableCell (PureTable table)
		{
			this.table = table;
		}

        public abstract void Paint (DestinationDependentGraphics g);

		public void SetAlignment(ContentAlignment ca)
		{
			contentAlignment = ca;
		}

		public virtual void SetLocation(Point p)
		{
			//Location = p;
			left = p.X;
			top = p.Y;
		}

		public Point GetLocation()
		{
			return new Point(left,top);
		}

		public void SetSize(Size s)
		{
			Size = s;
		}

		public Size GetSize()
		{
			return Size;
		}

		public void SetForeColor (Color c)
		{
			foreBrush = new SolidBrush(c);
		}

		public virtual void SetBorderColour (Color c)
		{
		}

		public void SetBackColor(Color c)
		{
			backBrush = new SolidBrush(c);
		}

		public virtual void SetFontStyle(FontStyle fs)
		{
		}

		public virtual void SetFontSize(double size)
		{
		}

        public virtual void SetFont (Font font)
        {
        }

		public Point Location
		{
			set
			{
				left = value.X;
				top = value.Y;
			}

			get
			{
				return new Point(left,top);
			}
		}

		public int Top
		{
			get { return top; }
			set { top = value; }
		}

		public int Height
		{
			get { return height; }
			set { height = value; }
		}

		public int Width
		{
			get { return width; }
			set { width = value; }
		}

		public int Left
		{
			get { return left; }
			set { left = value; }
		}

        public int Right
        {
            get
            {
                return left + width;
            }
        }

        public int Bottom
        {
            get
            {
                return top + height;
            }
        }

		public virtual Size Size
		{
			get { return new Size(width,height); }
			set
			{
				width = value.Width;
				height = value.Height;
			}
		}
	}
}
