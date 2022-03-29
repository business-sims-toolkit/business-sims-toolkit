using System.Collections;

namespace BaseUtils
{
	/// <summary>
	/// A collection of Chart instances.
	/// </summary>
	public class ChartCollection : CollectionBase
	{
		/// <summary>
		/// Creates an instance of ChartCollection.
		/// </summary>
		public ChartCollection()
		{
		}

		public Chart this[int index]
		{
			get { return base.List[index] as Chart; }
			set { base.List[index] = value; }
		}

		public int Add(Chart chart)
		{
			return base.List.Add(chart);
		}

		public void Remove(Chart chart)
		{
			base.List.Remove(chart);
		}
	}
}
