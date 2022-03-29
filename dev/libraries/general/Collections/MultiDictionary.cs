using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Collections
{
	/// <summary>
	///  a dictionary that contains more than one value per key
	///  see http://sachabarber.net/?p=477
	///  this shouldnt really live in this namespace
	/// </summary>
	public class MultiDictionary<T, K> : Dictionary<T, List<K>>
	{
		void EnsureKey(T key)
		{
			if (!ContainsKey(key) || this[key] == null)
			{
				this[key] = new List<K>();
			}
		}

		public void Add(T key, K item)
		{
			EnsureKey(key);

			this[key].Add(item);
		}

		public void AddRange(T key, IEnumerable<K> items)
		{
			EnsureKey(key);

			this[key].AddRange(items);
		}

		public bool Remove(T key, K item)
		{
			if (!ContainsKey(key))
			{
				return false;
			}

			this[key].Remove(item);

			if (this[key].Count == 0)
			{
				Remove(key);
			}

			return true;
		}

		public bool RemoveAllValues(T key, Predicate<K> match)
		{
			if (!ContainsKey(key))
			{
				return false;
			}

			this[key].RemoveAll(match);

			if(this[key].Count == 0)
			{
				Remove(key);
			}

			return true;
		}
	}
}
