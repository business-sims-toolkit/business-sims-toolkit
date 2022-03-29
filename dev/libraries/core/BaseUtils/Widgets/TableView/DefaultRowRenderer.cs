using System.Drawing;

namespace BaseUtils
{
	/// <summary>
	/// Base class for rendering a row of data in the TableViewControl.
	/// </summary>
	public class DefaultRowRenderer
	{
		protected TableViewTable table;

		/// <summary>
		/// Creates an instance of DefaultRowRenderer.
		/// </summary>
		public DefaultRowRenderer()
		{
		}

		/// <summary>
		/// Associates the specified TableViewTable with an instance
		/// of DefaultRowRenderer.
		/// </summary>
		/// <param name="table"></param>
		internal void SetTable(TableViewTable table)
		{
			this.table = table;
		}

		/// <summary>
		/// Renders a row of data.
		/// </summary>
		/// <param name="g"></param>
		/// <param name="row"></param>
		/// <param name="y"></param>
		public virtual void RenderRow(Graphics g, object row, int y)
		{
			BeforeRender(row);
			foreach (TableViewColumn c in table)
				c.ColumnRenderer.RenderColumn(g, row, y);
		}

		/// <summary>
		/// Virtual method giving sub-classes the opertunity
		/// to customize the way that rows are rendered.
		/// </summary>
		/// <param name="row"></param>
		public virtual void BeforeRender(object row)
		{
		}

		/// <summary>
		/// Gets the row Font.
		/// </summary>
		public virtual Font RowFont
		{
			get { return table.TableRenderer.TableFont; }
		}

		/// <summary>
		/// Gets the row Colour.
		/// </summary>
		public virtual Color RowColor
		{
			get { return table.TableRenderer.TableColor; }
		}
	}
}
