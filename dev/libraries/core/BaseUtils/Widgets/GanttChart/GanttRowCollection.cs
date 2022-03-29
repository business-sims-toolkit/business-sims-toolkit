using System;
using System.Collections;
using System.Drawing;

namespace BaseUtils
{
	/// <summary>
	/// A collection of one or more GanttBarCollection objects.
	/// </summary>
	public class GanttRowCollection : DictionaryBase, ICollection
	{
		/// <summary>
		/// Creates an instance of GanttRowCollection. Used for deserialization.
		/// </summary>
		public GanttRowCollection()
		{
		}

		/// <summary>
		/// Returns the GanttBarCollection at the specified index.
		/// </summary>
		public GanttBarCollection this[string configId]
		{
			get 
			{ 
				GanttBarCollection bars = null;

				if (Dictionary.Contains(configId))
					bars = (GanttBarCollection)Dictionary[configId];

				return bars;
			}
		}

		/// <summary>
		/// Add the specified GanttBarCollection to the collection.
		/// </summary>
		/// <param name="bars"></param>
		/// <returns></returns>
		public int Add(GanttBarCollection bars)  
		{
			int result = 0;

			if (!Dictionary.Contains(bars.ConfigId))
				Dictionary.Add(bars.ConfigId, bars);
			else
				Dictionary[bars.ConfigId] = bars;

			return result;
		}

		/// <summary>
		/// Remove the specified GanttBarCollection from the collection.
		/// </summary>
		/// <param name="bars"></param>
		public void Remove(GanttBarCollection bars)  
		{
			Dictionary.Remove(bars.ConfigId);
		}

		/// <summary>
		/// Clear the collection.
		/// </summary>
		public new void Clear()
		{
			foreach (GanttBarCollection bars in Dictionary.Values)
				bars.Clear();
		}

		/// <summary>
		/// Determines whether the specified GanttBarCollection exists
		/// in the collection.
		/// </summary>
		/// <param name="bars"></param>
		/// <returns></returns>
		public bool Contains(GanttBarCollection bars)  
		{
			// If value is not of type GanttBar, this will return false.
			return(Dictionary.Contains(bars.ConfigId));
		}

		/// <summary>
		/// Calculate the left-padding required to fully display
		/// the GanttRowCollection.Title properties.
		/// </summary>
		/// <param name="g"></param>
		/// <param name="font"></param>
		/// <returns></returns>
		public int CalcMaxPadLeft(Graphics g, Font font)
		{
			int temp = 0;
			SizeF size;

			foreach (GanttBarCollection bars in Dictionary.Values)
			{
				size = g.MeasureString(bars.Title, font);
				if ((int)size.Width > temp)
				{
					temp = (int)size.Width;
				}
			}
			temp += 40;

			return temp;
		}

		public ICollection Values
		{
			get 
			{ 
				ArrayList items = new ArrayList(Dictionary.Values);
				items.Sort(new GanttRowComparer());
				return items; 
			}
		}
	}

	/// <summary>
	/// Helper class implementing IComparer to sort
	/// GantRowCollection by ascending Title.
	/// </summary>
	public class GanttRowComparer : IComparer
	{
		public int Compare(Object x, Object y)
		{
			GanttBarCollection xs = x as GanttBarCollection;
			GanttBarCollection ys = y as GanttBarCollection;
			xs.Title.CompareTo(ys.Title);
			return xs.Title.CompareTo(ys.Title);
		}
	}
}
