using System.Drawing;
using System.Collections;

using LibCore;

namespace BaseUtils
{
	/// <summary>
	/// Base class for rendering a table of data in the TableViewControl.
	/// </summary>
	public class DefaultTableRenderer
	{
		protected TableViewTable table;
		protected Font titleFont;
		protected Color titleColor;
		protected Font tableFont;
		protected Color tableColor;
		protected int titleHeight;
		protected int rowHeight;

		/// <summary>
		/// Creates an instance of DefaultTableRenderer.
		/// </summary>
		/// <param name="table"></param>
		public DefaultTableRenderer(TableViewTable table)
		{
			this.table = table;
			this.titleFont = ConstantSizeFont.NewFont("Tahoma", 10f, FontStyle.Bold);
			this.titleColor = Color.Black;
			this.tableFont = ConstantSizeFont.NewFont("Tahoma", 10f);
			this.tableColor = Color.Black;
			this.titleHeight = 0;
			this.rowHeight = 0;
		}

		/// <summary>
		/// Renders the specified list using this DefaultTableRenderer.
		/// </summary>
		/// <param name="g"></param>
		/// <param name="list"></param>
		public virtual void RenderList(Graphics g, IList list)
		{
			int y = 0;

			CalcRowHeights(g);
			RenderTitle(g, y);
			y += titleHeight;

			foreach (object row in list)
			{
				RenderRow(g, row, y);
				y += rowHeight;
			}
		}

		/// <summary>
		/// Calculate the height of title and data rows in the
		/// table given the current fonts.
		/// </summary>
		/// <param name="g"></param>
		protected void CalcRowHeights(Graphics g)
		{
			if (titleHeight == 0 || rowHeight == 0)
			{
				SizeF titleSize = g.MeasureString("ABC", titleFont);
				SizeF rowSize = g.MeasureString("ABC", tableFont);
				titleHeight = (int)titleSize.Height + 2;
				rowHeight = (int)rowSize.Height;
			}
		}

		/// <summary>
		/// Render the title row.
		/// </summary>
		/// <param name="g"></param>
		/// <param name="y"></param>
		protected virtual void RenderTitle(Graphics g, int y)
		{
			foreach (TableViewColumn c in table)
				c.ColumnRenderer.RenderTitle(g, y);
		}

		/// <summary>
		/// Render a row of data in the table.
		/// </summary>
		/// <param name="g"></param>
		/// <param name="row"></param>
		/// <param name="y"></param>
		protected virtual void RenderRow(Graphics g, object row, int y)
		{
			table.RowRenderer.RenderRow(g, row, y);
		}

		/// <summary>
		/// Gets the height of a row of data.
		/// </summary>
		public int RowHeight
		{
			get { return rowHeight; }
		}

		/// <summary>
		/// Gets the height of the title row.
		/// </summary>
		public int TitleHeight
		{
			get { return titleHeight; }
		}

		/// <summary>
		/// Gets or Sets the Font used to display the title row.
		/// </summary>
		public virtual Font TitleFont
		{
			get { return titleFont; }
			set 
			{ 
				titleFont = value; 
				titleHeight = 0;
			}
		}

		/// <summary>
		/// Gets or Sets the colour used to display the title row.
		/// </summary>
		public virtual Color TitleColor
		{
			get { return titleColor; }
			set { titleColor = value; }
		}

		/// <summary>
		/// Gets or Sets the default Font used to display the table.
		/// </summary>
		public virtual Font TableFont
		{
			get { return tableFont; }
			set 
			{ 
				tableFont = value; 
				rowHeight = 0;
			}
		}

		/// <summary>
		/// Gets or Sets the default colour used to display the table.
		/// </summary>
		public virtual Color TableColor
		{
			get { return tableColor; }
			set { tableColor = value; }
		}
	}
}
