using System.Collections;

namespace BaseUtils
{
	/// <summary>
	/// A collection of Series instances.
	/// </summary>
	public class SeriesCollection : CollectionBase
	{
		/// <summary>
		/// Creates an instance of SeriesCollection.
		/// </summary>
		public SeriesCollection()
		{
		}

		public Series this[int index]
		{
			get { return base.List[index] as Series; }
			set { base.List[index] = value; }
		}

		public int Add(Series series)
		{
			return base.List.Add(series);
		}

		public void Remove(Series series)
		{
			base.List.Remove(series);
		}
	}
}
