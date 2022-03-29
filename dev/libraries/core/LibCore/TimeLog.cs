using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace LibCore
{
	public class TimeLog<T> : IEnumerable<T>
	{
		public struct TimeValuePair<U>
		{
			public double Time;
			public U Value;

			public TimeValuePair (double time, U value)
			{
				this.Time = time;
				this.Value = value;
			}
		}
		Dictionary<double, T> timeToValue;
		List<TimeValuePair<T>> sortedTimes;

		public class TimeOutOfRangeException : Exception
		{
		}

		public ReadOnlyCollection<double> Times
		{
			get
			{
				List<double> collection = new List<double> ();
				foreach (TimeValuePair<T> t in sortedTimes)
				{
					collection.Add(t.Time);
				}
				return new ReadOnlyCollection<double> (collection);
			}
		}

		public ReadOnlyCollection<T> Values
		{
			get
			{
				List<T> values = new List<T> ();
				foreach (TimeValuePair<T> t in sortedTimes)
				{
					values.Add(t.Value);
				}
				return new ReadOnlyCollection<T> (values);
			}
		}

		public TimeLog ()
		{
			timeToValue = new Dictionary<double, T> ();
			sortedTimes = new List<TimeValuePair<T>> ();
		}

		public int Count
		{
			get
			{
				return timeToValue.Count;
			}
		}

		public bool ContainsTime (double time)
		{
			return timeToValue.ContainsKey(time);
		}

		public bool IsEmpty
		{
			get
			{
				return timeToValue.Count == 0;
			}
		}

		public T this [double time]
		{
			get
			{
				return timeToValue[time];
			}

			set
			{
				Add(time, value);
			}
		}

		public void Add (double time, T value)
		{
			if (timeToValue.ContainsKey(time))
			{
				timeToValue[time] = value;
				for (int i = 0; i < sortedTimes.Count; i++ )
				{
					if (sortedTimes[i].Time == time)
					{
						sortedTimes[i] = new TimeValuePair<T> (time, value);
					}
				}
			}
			else
			{
				timeToValue.Add(time, value);
				sortedTimes.Add(new TimeValuePair<T>(time, value));
				sortedTimes.Sort(delegate (TimeValuePair<T> a, TimeValuePair<T> b) { return a.Time.CompareTo(b.Time); });
			}
		}

		public void Remove (double time)
		{
			if (timeToValue.ContainsKey(time))
			{
				timeToValue.Remove(time);
				for (int i = 0; i < sortedTimes.Count; i++)
				{
					if (sortedTimes[i].Time == time)
					{
						sortedTimes.RemoveAt(i);
						break;
					}
				}
			}
		}

        public void RemoveUntil(double time)
        {
            
            while (TryGetLastTimeBefore(time).HasValue)
            {
                double t = GetLastTimeBefore(time);
                Remove(t);
            }
            
        }

        public void RemoveAfter(double time)
        {
            
            while (TryGetFirstTimeAfter(time).HasValue)
            {
                double t = GetFirstTimeAfter(time);
                Remove(t);
            }
        }

        int lastSearchedIndex = 0;
		public T GetLastValueBefore (double time)
		{
			int count = sortedTimes.Count;
			if (lastSearchedIndex >= count
				|| sortedTimes[lastSearchedIndex].Time >= time)
            {
                lastSearchedIndex = 0;
            }

			TimeValuePair<T>? lastTime = null;
			for (int i = lastSearchedIndex; i < count; i++)
			{
				lastSearchedIndex = i;
				TimeValuePair<T> t = sortedTimes[lastSearchedIndex];

                if (t.Time < time)
				{
					lastTime = t;
				}
				else
				{
					break;
				}
			}

			if (null != lastTime)
			{
				return lastTime.Value.Value;
			}
			else
			{
				return default(T);
			}
		}

		public T GetLastValueOnOrBefore (double time)
		{
			TimeValuePair<T>? lastTime = null;

			foreach (TimeValuePair<T> t in sortedTimes)
			{
				if (t.Time <= time)
				{
					lastTime = t;
				}
				else
				{
					break;
				}
			}

			if (null != lastTime)
			{
				return lastTime.Value.Value;
			}

			return default(T);
		}

		public T GetFirstValueAfter (double time)
		{
			foreach (TimeValuePair<T> t in sortedTimes)
			{
				if (t.Time > time)
				{
					return t.Value;
				}
			}

			return default(T);
		}

		public T GetFirstValueOnOrAfter (double time)
		{
			foreach (TimeValuePair<T> t in sortedTimes)
			{
				if (t.Time >= time)
				{
					return t.Value;
				}
			}

			return default(T);
		}

		public IEnumerator<T> GetEnumerator ()
		{
			foreach (TimeValuePair<T> time in sortedTimes)
			{
				yield return time.Value;
			}
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
		{
			foreach (TimeValuePair<T> time in sortedTimes)
			{
				yield return time.Value;
			}
		}

		public double FirstTime
		{
			get
			{
				if (sortedTimes.Count < 1)
				{
					throw new TimeOutOfRangeException ();
				}

				return sortedTimes[0].Time;
			}
		}

		public double LastTime
		{
			get
			{
				if (sortedTimes.Count < 1)
				{
					throw new TimeOutOfRangeException();
				}

				return sortedTimes[sortedTimes.Count - 1].Time;
			}
		}

		public double? TryGetLastTimeBefore (double time)
		{
			TimeValuePair<T>? lastTime = null;

			foreach (TimeValuePair<T> t in sortedTimes)
			{
				if (t.Time < time)
				{
					lastTime = t;
				}
				else
				{
					break;
				}
			}

			if (lastTime.HasValue)
			{
				return lastTime.Value.Time;
			}

			return null;
		}

		public double TryGetLastTimeBefore (double time, double defaultTime)
		{
			double? lastTime = TryGetLastTimeBefore(time);

			if (lastTime.HasValue)
			{
				return lastTime.Value;
			}

			return defaultTime;
		}

		public double GetLastTimeBefore (double time)
		{
			TimeValuePair<T>? lastTime = null;

			foreach (TimeValuePair<T> t in sortedTimes)
			{
				if (t.Time < time)
				{
					lastTime = t;
				}
				else
				{
					break;
				}
			}

			if (lastTime.HasValue)
			{
				return lastTime.Value.Time;
			}

			throw new TimeOutOfRangeException ();
		}

		public double? TryGetLastTimeOnOrBefore (double time)
		{
			TimeValuePair<T>? lastTime = null;

			foreach (TimeValuePair<T> t in sortedTimes)
			{
				if (t.Time <= time)
				{
					lastTime = t;
				}
				else
				{
					break;
				}
			}

			if (lastTime.HasValue)
			{
				return lastTime.Value.Time;
			}

			return null;
		}

		public double TryGetLastTimeOnOrBefore (double time, double defaultTime)
		{
			double? lastTime = TryGetLastTimeOnOrBefore(time);

			if (lastTime.HasValue)
			{
				return lastTime.Value;
			}

			return defaultTime;
		}

		public double GetLastTimeOnOrBefore (double time)
		{
			TimeValuePair<T>? lastTime = null;

			foreach (TimeValuePair<T> t in sortedTimes)
			{
				if (t.Time <= time)
				{
					lastTime = t;
				}
				else
				{
					break;
				}
			}

			if (lastTime.HasValue)
			{
				return lastTime.Value.Time;
			}

			throw new TimeOutOfRangeException ();
		}

		public double? TryGetFirstTimeAfter (double time)
		{
			foreach (TimeValuePair<T> t in sortedTimes)
			{
				if (t.Time > time)
				{
					return t.Time;
				}
			}

			return null;
		}

		public double TryGetFirstTimeAfter (double time, double defaultTime)
		{
			double? firstTime = TryGetFirstTimeAfter(time);

			if (firstTime.HasValue)
			{
				return firstTime.Value;
			}

			return defaultTime;
		}

		public double GetFirstTimeAfter (double time)
		{
			foreach (TimeValuePair<T> t in sortedTimes)
			{
				if (t.Time > time)
				{
					return t.Time;
				}
			}

			throw new TimeOutOfRangeException();
		}

		public double? TryGetFirstTimeOnOrAfter (double time)
		{
			foreach (TimeValuePair<T> t in sortedTimes)
			{
				if (t.Time >= time)
				{
					return t.Time;
				}
			}

			return null;
		}

		public double TryGetFirstTimeOnOrAfter (double time, double defaultTime)
		{
			double? firstTime = TryGetFirstTimeOnOrAfter(time);

			if (firstTime.HasValue)
			{
				return firstTime.Value;
			}

			return defaultTime;
		}

		public double GetFirstTimeOnOrAfter (double time)
		{
			foreach (TimeValuePair<T> t in sortedTimes)
			{
				if (t.Time >= time)
				{
					return t.Time;
				}
			}

			throw new TimeOutOfRangeException();
		}

		public T FirstValue
		{
			get
			{
				return timeToValue[FirstTime];
			}
		}

		public T LastValue
		{
			get
			{
				return timeToValue[LastTime];
			}
		}

		public void Clear ()
		{
			timeToValue.Clear();
			sortedTimes.Clear();
		}

        public double? GetFirstTimeOfValue(T value)
        {
            foreach (TimeValuePair<T> t in sortedTimes)
            {
                if (t.Value.Equals(value))
                {
                    return t.Time;
                }
            }
            return null;
        }

        public double? GetLastTimeOfValue(T value)
        {
            for (int i = sortedTimes.Count - 1; i >= 0; i--)
            {
                if (sortedTimes[i].Value.Equals(value))
                {
                    return sortedTimes[i].Time;
                }
            }
            return null;
        }
	}
}