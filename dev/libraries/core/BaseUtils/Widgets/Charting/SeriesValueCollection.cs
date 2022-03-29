using System.Collections;

namespace BaseUtils
{
	/// <summary>
	/// A collection of SeriesValues.
	/// </summary>
	public class SeriesValueCollection : CollectionBase
	{
		/// <summary>
		/// Creates an instance of SeriesValueCollection.
		/// </summary>
		public SeriesValueCollection()
		{
		}

		public SeriesValue this[int index]
		{
			get { return base.List[index] as SeriesValue; }
			set { base.List[index] = value; }
		}

		public int Add(float val)
		{
			return base.List.Add(new SeriesValue(val));
		}

		public void Remove(SeriesValue sv)
		{
			base.List.Remove(sv);
		}
	}
}
