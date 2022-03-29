using System;
using System.Collections.Generic;

namespace LibCore
{
	public class WatchableList<T> : IList<T>
	{
		List<T> items;

		public WatchableList ()
		{
			items = new List<T> ();
		}

		public int Add (T item)
		{
			items.Add(item);
			OnItemsChanged();
			return items.Count - 1;
		}

		public int Count
		{
			get
			{
				return items.Count;
			}
		}

		public int IndexOf (T item)
		{
			return items.IndexOf(item);
		}

		public T this [int i]
		{
			get
			{
				return items[i];
			}

			set
			{
				items[i] = value;
				OnItemsChanged();
			}
		}

		public void Clear ()
		{
			items.Clear();
			OnItemsChanged();
		}

		public void Insert (int index, T item)
		{
			items.Insert(index, item);
			OnItemsChanged();
		}

		public void RemoveAt (int index)
		{
			items.RemoveAt(index);
			OnItemsChanged();
		}

		void ICollection<T>.Add (T item)
		{
			Add(item);
		}

		public bool Contains (T item)
		{
			return items.Contains(item);
		}

		public void CopyTo (T [] array, int arrayIndex)
		{
			items.CopyTo(array, arrayIndex);
		}

		public bool IsReadOnly
		{
			get
			{
				return false;
			}
		}

		public bool Remove (T item)
		{
			bool removed = items.Remove(item);
			OnItemsChanged();
			return removed;
		}

		public IEnumerator<T> GetEnumerator ()
		{
			return items.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
		{
			return items.GetEnumerator();
		}

		public event EventHandler ItemsChanged;

		void OnItemsChanged ()
		{
			if (ItemsChanged != null)
			{
				ItemsChanged(this, EventArgs.Empty);
			}
		}
	}
}