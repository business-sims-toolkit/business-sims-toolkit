using System.Drawing;

namespace BaseUtils
{
	/// <summary>
	/// Base class for rendering column titles and values
	/// within a TableViewControl.
	/// </summary>
	public class DefaultColumnRenderer
	{
		protected TableViewColumn col;
		protected object val;

		/// <summary>
		/// Constructs an instance of DefaultColumnRenderer.
		/// </summary>
		public DefaultColumnRenderer()
		{
		}

		/// <summary>
		/// Constructs an instance of DefaultColumnRenderer.
		/// </summary>
		/// <param name="col"></param>
		public DefaultColumnRenderer(TableViewColumn col)
		{
			this.col = col;
		}

		/// <summary>
		/// Associates a TableViewColumn with a DefaultColumnRenderer instance.
		/// </summary>
		/// <param name="col"></param>
		internal void SetColumn(TableViewColumn col)
		{
			this.col = col;
		}

		/// <summary>
		/// Render a column title.
		/// </summary>
		/// <param name="g"></param>
		/// <param name="y"></param>
		public virtual void RenderTitle(Graphics g, int y)
		{
			RectangleF rect = new Rectangle(col.X, y, col.Width, col.Table.TableRenderer.RowHeight);
			StringFormat sf = new StringFormat(StringFormatFlags.NoWrap);
			sf.Trimming = StringTrimming.EllipsisCharacter;
			sf.Alignment = (StringAlignment)col.ColumnAlign;
			g.DrawString(col.Title, col.Table.TableRenderer.TitleFont, new SolidBrush(col.Table.TableRenderer.TitleColor), rect, sf);
		}

		/// <summary>
		/// Render a column value.
		/// </summary>
		/// <param name="g"></param>
		/// <param name="row"></param>
		/// <param name="y"></param>
		public virtual void RenderColumn(Graphics g, object row, int y)
		{
			val = FormatValue(col.GetValue(row));

			if (col.ColumnType == ColumnType.Text)
			{
				RectangleF rect = new Rectangle(col.X, y, col.Width, col.Table.TableRenderer.RowHeight);
				StringFormat sf = new StringFormat(StringFormatFlags.NoWrap);
				sf.Trimming = StringTrimming.EllipsisCharacter;
				sf.Alignment = (StringAlignment)col.ColumnAlign;
				g.DrawString(val.ToString(), this.ColFont, new SolidBrush(this.ColColor), rect, sf);
			}
		}

		/// <summary>
		/// Virtual method giving sub-classes the opertunity
		/// to customize the way that a value is formatted.
		/// </summary>
		/// <param name="val"></param>
		/// <returns></returns>
		public virtual object FormatValue(object val)
		{
			return val;
		}

		/// <summary>
		/// Gets the associated TableViewColumn.
		/// </summary>
		public TableViewColumn Column
		{
			get { return col; }
		}

		/// <summary>
		/// Gets the associated Value.
		/// </summary>
		public object Value
		{
			get { return val; }
		}

		/// <summary>
		/// Gets the associated Font.
		/// </summary>
		public virtual Font ColFont
		{
			get { return col.Table.RowRenderer.RowFont; }
		}

		/// <summary>
		/// Gets the associated Colour.
		/// </summary>
		public virtual Color ColColor
		{
			get { return col.Table.RowRenderer.RowColor; }
		}
	}
}
