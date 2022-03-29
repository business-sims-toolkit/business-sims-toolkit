using System;
using System.Collections.Generic;

namespace LibCore
{
	public static class TopologicalSortExtensions
	{
		public class CyclicGraphException<T> : Exception
		{
			List<T> cycle;

			public IList<T> Cycle
			{
				get
				{
					return new List<T> (cycle);
				}
			}

			public CyclicGraphException (IList<T> cycle)
				: base ("Cycle detected in graph")
			{
				this.cycle = new List<T> (cycle);
			}
		}

		public static IEnumerable<T> TopologicalSort<T> (this IEnumerable<T> source, Func<T, IEnumerable<T>> getDependencies)
		{
			var sorted = new List<T> ();
			var visited = new HashSet<T> ();

			foreach (var item in source)
			{
				Visit(item, visited, sorted, new List<T> (), getDependencies);
			}

			return sorted;
		}

		static void Visit<T> (T item, HashSet<T> visited, List<T> sorted, List<T> visitedInStack, Func<T, IEnumerable<T>> getDependencies)
		{
			List<T> deeperVisitedInStack = new List<T> (visitedInStack);
			deeperVisitedInStack.Add(item);

			if (visitedInStack.Contains(item))
			{
				throw new CyclicGraphException<T> (deeperVisitedInStack);
			}

			if (! visited.Contains(item))
			{
				visited.Add(item);

				foreach (var dependency in getDependencies(item))
				{
					Visit(dependency, visited, sorted, deeperVisitedInStack, getDependencies);
				}

				sorted.Add(item);
			}
		}
	}
}