using System.Collections.Generic;

namespace LibCore
{
	public static class CollectionUtils
	{
		public static T [] Union<T> (params ICollection<T> [] args)
		{
			List<T> results = new List<T> ();

			foreach (ICollection<T> collection in args)
			{
				if (collection != null)
				{
					foreach (T value in collection)
					{
						if (!results.Contains(value))
						{
							results.Add(value);
						}
					}
				}
			}

			return results.ToArray();
		}

		public static void AddRange<T, U> (Dictionary<T, U> original, Dictionary<T, U> extra)
		{
			foreach (T key in extra.Keys)
			{
				original.Add(key, extra[key]);
			}
		}
	}
}