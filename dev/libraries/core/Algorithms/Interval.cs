using System;

namespace Algorithms
{
	public struct Interval : IEquatable<Interval>
	{
		public int Min;
		public int Size;

		public int Max
		{
			get => Min + Size;

			set => Size = value - Min;
		}

		public Interval (int min, int size)
		{
			Min = min;
			Size = size;
		}

		public bool Contains (int value)
		{
			return (value >= Min) && (value < Max);
		}

		public bool Equals (Interval other)
		{
			return (Min == other.Min) && (Size == other.Size);
		}

		public override bool Equals (object obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}

			return (obj is Interval) && Equals((Interval) obj);
		}

		public override int GetHashCode ()
		{
			return (Min * 397) ^ Size;
		}

		public static bool operator == (Interval a, Interval b)
		{
			return a.Equals(b);
		}

		public static bool operator != (Interval a, Interval b)
		{
			return ! (a.Equals(b));
		}
	}
}
