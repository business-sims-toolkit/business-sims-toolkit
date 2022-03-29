using System;
using System.Collections;

namespace BaseUtils
{
	/// <summary>
	/// GanttBarCollection reprasents a single row within the chart. Each row
	/// consists of a row title and a collection of bars.
	/// </summary>
	public class GanttBarCollection : CollectionBase
	{
		string configId;
		string title;

		/// <summary>
		/// Creates an instance of GanttBarCollection. Used for
		/// deserializing.
		/// </summary>
		public GanttBarCollection()
		{
		}

		/// <summary>
		/// Creates an instance of GanttBarCollection.
		/// </summary>
		/// <param name="configId"></param>
		/// <param name="title"></param>
		public GanttBarCollection(string configId, string title)
		{
			this.configId = configId;
			this.title = title;
		}

		/// <summary>
		/// Returns an instance of GanttBar at the
		/// specified location.
		/// </summary>
		public GanttBar this[int index]
		{
			get { return (GanttBar)List[index]; }
			set { List[index] = value; }
		}

		/// <summary>
		/// Adds the specified GanttBar to the collection.
		/// </summary>
		/// <param name="bar"></param>
		/// <returns></returns>
		public int Add(GanttBar bar)  
		{
			int result = List.Add(bar);

			// Sort the bars in descending duration
			// order. This way, overlapping bars
			// will be visible
			Sort();

			return result;
		}

		/// <summary>
		/// Returns the index of the specified GanttBar.
		/// </summary>
		/// <param name="bar"></param>
		/// <returns>int</returns>
		public int IndexOf(GanttBar bar)  
		{
			return(List.IndexOf(bar));
		}

		/// <summary>
		/// Inserts the specified GanttBar at the index specified.
		/// </summary>
		/// <param name="index"></param>
		/// <param name="bar"></param>
		public void Insert(int index, GanttBar bar)  
		{
			List.Insert(index, bar);
		}

		/// <summary>
		/// Removes the specified GanttBar from the collection.
		/// </summary>
		/// <param name="bar"></param>
		public void Remove(GanttBar bar)  
		{
			List.Remove(bar);
		}

		/// <summary>
		/// Sort the collection of GanttBars.
		/// </summary>
		public void Sort()
		{
			base.InnerList.Sort(new GanttBarComparer());
		}

		/// <summary>
		/// Determines whether the specified GanttBar exists
		/// in the collection.
		/// </summary>
		/// <param name="bar"></param>
		/// <returns></returns>
		public bool Contains(GanttBar bar)  
		{
			// If value is not of type GanttBar, this will return false.
			return List.Contains(bar);
		}

		/// <summary>
		/// The title of the GanttBar.
		/// </summary>
		public string Title
		{
			get { return title; }
			set { title = value; }
		}

		/// <summary>
		/// The ConfigId of the GanttBar.
		/// </summary>
		public string ConfigId
		{
			get { return configId; }
			set { configId = value; }
		}
	}

	/// <summary>
	/// Helper class implementing IComparer which
	/// sorts GanttBar instances by descending duration.
	/// </summary>
	public class GanttBarComparer : IComparer
	{
		public GanttBarComparer() : base()
		{
		}

		public int Compare(Object x, Object y)
		{
			GanttBar xg = x as GanttBar;
			GanttBar yg = y as GanttBar;

			return yg.Duration.CompareTo(xg.Duration);
		}
	}

}
