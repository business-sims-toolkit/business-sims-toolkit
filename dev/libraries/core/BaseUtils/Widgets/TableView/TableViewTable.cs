using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;

namespace BaseUtils
{
	/// <summary>
	/// Defines how data is rendered in the table.
	/// </summary>
	public class TableViewTable : CollectionBase
	{
		Type containedType;
		PropertyDescriptorCollection props;
		DefaultTableRenderer renderer;
		DefaultRowRenderer rowRenderer;

		/// <summary>
		/// Creates an instance of TableViewTable.
		/// </summary>
		public TableViewTable()
		{
			renderer = new DefaultTableRenderer(this);
			rowRenderer = new DefaultRowRenderer();
			rowRenderer.SetTable(this);
		}

		/// <summary>
		/// Gets or Sets an instance of TableViewColumn.
		/// </summary>
		public TableViewColumn this[int index]
		{
			get { return (TableViewColumn)base.List[index]; }
			set { base.List[index] = value; }
		}

		/// <summary>
		/// Adds a TableViewColumn to the collection. 
		/// </summary>
		/// <param name="col"></param>
		public void Add(TableViewColumn col)
		{
			if (props != null)
			{
				PropertyDescriptor pd = props.Find(col.DataMember, false);

				if (pd != null)
				{
					col.SetPropertyDescriptor(pd);
					col.SetTable(this);

					if (col.Sequence == 0)
						col.Sequence = base.List.Count;

					base.List.Add(col);
					base.InnerList.Sort(new TableViewColumnComparer());

					int x = 0;

					foreach (TableViewColumn c in base.List)
					{
						c.X = x;
						x += c.Width + c.Pad;
					}
				}
				else
				{
					Debug.Assert(false, String.Format("Property {0} not found in class {1}", col.DataMember, containedType.ToString()));
				}
			}
		}

		/// <summary>
		/// Removes the specified TableViewColumn from the collection.
		/// </summary>
		/// <param name="col"></param>
		public void Remove(TableViewColumn col)
		{
			base.List.Remove(col);
		}

		/// <summary>
		/// Gets or Sets the type of the item contained within the table
		/// data source.
		/// </summary>
		public Type ContainedType
		{
			get { return containedType; }
			set
			{
				this.containedType = value;
				this.props = TypeDescriptor.GetProperties(value);
			}
		}

		/// <summary>
		/// Gets or Sets the default table renderer.
		/// </summary>
		public DefaultTableRenderer TableRenderer
		{
			get { return renderer; }
			set { renderer = value; }
		}

		/// <summary>
		/// Gets or Sets the default row renderer.
		/// </summary>
		public DefaultRowRenderer RowRenderer
		{
			get { return rowRenderer; }
			set 
			{ 
				rowRenderer = value; 
				rowRenderer.SetTable(this);
			}
		}
	}

	/// <summary>
	/// Helper class implementing IComparer to sort
	/// TableViewColumn instances by ascending sequence.
	/// </summary>
	public class TableViewColumnComparer : IComparer
	{
		public TableViewColumnComparer() : base()
		{
		}

		public int Compare(object x, object y)
		{
			TableViewColumn xs = x as TableViewColumn;
			TableViewColumn ys = y as TableViewColumn;
			return xs.Sequence.CompareTo(ys.Sequence);
		}
	}
}
