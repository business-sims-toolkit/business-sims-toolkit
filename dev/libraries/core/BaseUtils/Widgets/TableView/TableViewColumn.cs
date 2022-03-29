using System.ComponentModel;

namespace BaseUtils
{
	/// <summary>
	/// The type of column.
	/// </summary>
	public enum ColumnType
	{
		Text,
		Image
	}

	/// <summary>
	/// The alignment for a column.
	/// </summary>
	public enum ColumnAlign
	{
		Left,
		Middle,
		Right
	}

	/// <summary>
	/// Encapsulates the information required to draw
	/// a column of data in a TableViewControl.
	/// </summary>
	public class TableViewColumn
	{
		string dataMember;
		string title;
		ColumnType colType;
		ColumnAlign colAlign;
		int sequence;
		int width;
		int x;
		int pad;
		PropertyDescriptor pd;
		TableViewTable table;
		DefaultColumnRenderer renderer;

		/// <summary>
		/// Creates an instance of TableViewColumn.
		/// </summary>
		/// <param name="dataMember"></param>
		/// <param name="title"></param>
		/// <param name="colType"></param>
		/// <param name="sequence"></param>
		/// <param name="width"></param>
		public TableViewColumn(string dataMember, string title, ColumnType colType, int sequence, int width)
		{
			Init(dataMember, title, colType, ColumnAlign.Left, sequence, width, new DefaultColumnRenderer(this));
		}

		/// <summary>
		/// Creates an instance of TableViewColumn.
		/// </summary>
		/// <param name="dataMember"></param>
		/// <param name="title"></param>
		/// <param name="colType"></param>
		/// <param name="colAlign"></param>
		/// <param name="sequence"></param>
		/// <param name="width"></param>
		public TableViewColumn(string dataMember, string title, ColumnType colType, ColumnAlign colAlign, int sequence, int width)
		{
			Init(dataMember, title, colType, colAlign, sequence, width, new DefaultColumnRenderer(this));
		}

		/// <summary>
		/// Creates an instance of TableViewColumn.
		/// </summary>
		/// <param name="dataMember"></param>
		/// <param name="title"></param>
		/// <param name="colType"></param>
		/// <param name="colAlign"></param>
		/// <param name="sequence"></param>
		/// <param name="width"></param>
		/// <param name="renderer"></param>
		public TableViewColumn(string dataMember, string title, ColumnType colType, ColumnAlign colAlign, int sequence, int width, DefaultColumnRenderer renderer)
		{
			renderer.SetColumn(this);
			Init(dataMember, title, colType, colAlign, sequence, width, renderer);
		}

		/// <summary>
		/// Initializes an instance of TableViewColumn.
		/// </summary>
		/// <param name="dataMember"></param>
		/// <param name="title"></param>
		/// <param name="colType"></param>
		/// <param name="colAlign"></param>
		/// <param name="sequence"></param>
		/// <param name="width"></param>
		/// <param name="renderer"></param>
		void Init(string dataMember, string title, ColumnType colType, ColumnAlign colAlign, int sequence, int width, DefaultColumnRenderer renderer)
		{
			this.dataMember = dataMember;
			this.title = title;
			this.colType = colType;
			this.colAlign = colAlign;
			this.sequence = sequence;
			this.width = width;
			this.renderer = renderer;
			this.pad = 6;
		}

		/// <summary>
		/// Associates the specified PropertyDescriptor with an instance of TableViewColumn.
		/// </summary>
		/// <param name="pd"></param>
		internal void SetPropertyDescriptor(PropertyDescriptor pd)
		{
			this.pd = pd;
		}

		/// <summary>
		/// Associates the specified TableViewTable with an instance of TableViewColumn.
		/// </summary>
		/// <param name="table"></param>
		internal void SetTable(TableViewTable table)
		{
			this.table = table;
		}

		/// <summary>
		/// Gets or Sets the DataMember used for data-binding.
		/// </summary>
		public string DataMember
		{
			get { return dataMember; }
			set { dataMember = value; }
		}

		/// <summary>
		/// Gets or Sets the title.
		/// </summary>
		public string Title
		{
			get { return title; }
			set { title = value; }
		}

		/// <summary>
		/// Gets or Sets the column type.
		/// </summary>
		public ColumnType ColumnType
		{
			get { return colType; }
			set { colType = value; }
		}

		/// <summary>
		/// Gets or Sets the column alignment.
		/// </summary>
		public ColumnAlign ColumnAlign
		{
			get { return colAlign; }
			set { colAlign = value; }
		}

		/// <summary>
		/// Gets or Sets the column sequence.
		/// </summary>
		public int Sequence
		{
			get { return sequence; }
			set { sequence = value; }
		}

		/// <summary>
		/// Gets or Sets the column width.
		/// </summary>
		public int Width
		{
			get { return width; }
			set { width = value; }
		}

		/// <summary>
		/// Gets or Sets the location of the column.
		/// </summary>
		public int X
		{
			get { return x; }
			set { x = value; }
		}

		/// <summary>
		/// Gets or Sets the padding for the column.
		/// </summary>
		public int Pad
		{
			get { return pad; }
			set { pad = value; }
		}

		/// <summary>
		/// Gets or Sets the default column renderer.
		/// </summary>
		public DefaultColumnRenderer ColumnRenderer
		{
			get { return renderer; }
			set { renderer = value; }
		}

		/// <summary>
		/// Gets the PropertyDescriptor.
		/// </summary>
		public PropertyDescriptor PropertyDescriptor
		{
			get { return pd; }
		}

		/// <summary>
		/// Gets the associated TableViewTable.
		/// </summary>
		public TableViewTable Table
		{
			get { return table; }
		}

		/// <summary>
		/// Gets a value for the specified "Row" (object in a collection)
		/// </summary>
		/// <param name="row"></param>
		/// <returns></returns>
		public object GetValue(object row)
		{
			return pd.GetValue(row);
		}
	}
}
