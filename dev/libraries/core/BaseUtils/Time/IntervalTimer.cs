using System;
using System.Runtime.InteropServices;

namespace BaseUtils
{
	/// <summary>
	/// Summary description for IntervalTimer.
	/// </summary>
	public class IntervalTimer
	{

		[DllImport("kernel32.dll")]
		static extern int QueryPerformanceCounter(out long count);

		[DllImport("kernel32.dll")]
		static extern int QueryPerformanceFrequency(out long count);

		public enum TimerState {NotStarted, Stopped, Started}

		TimerState state;
		long ticksAtStart;
		long intervalTicks;
		static long frequency;
		static int decimalPlaces;
		static string formatString;
		static bool initialized = false;


		public IntervalTimer()
		{
			if (!initialized)
			{
				QueryPerformanceFrequency(out frequency);
				decimalPlaces = (int)Math.Log10(frequency);
				formatString = String.Format("Interval: {{0:F{0}}} seconds ({{1}} ticks)", decimalPlaces);
				initialized = true;
			}
			state = TimerState.NotStarted;
		}

		public void Start()
		{
			state = TimerState.Started;
			ticksAtStart = CurrentTicks;
		}

		public void Stop()
		{
			intervalTicks = CurrentTicks - ticksAtStart;
			state = TimerState.Stopped;
		}

		public float GetSeconds()
		{
			if (state != TimerState.Stopped)
				throw new TimerNotStoppedException();
			return (float)intervalTicks/(float)frequency;
		}

		public long GetTicks()
		{
			if (state != TimerState.Stopped)
				throw new TimerNotStoppedException();
			return intervalTicks;
		}

		long CurrentTicks
		{
			get
			{
				long ticks;
				QueryPerformanceCounter(out ticks);
				return ticks;
			}
		}

		public override string ToString()
		{
			if (state != TimerState.Stopped)
				return "Interval timer, state: " + state.ToString();
			return String.Format(formatString, GetSeconds(), intervalTicks);
		}
	}


	public class TimerNotStoppedException : ApplicationException
	{
		public TimerNotStoppedException()
			: base("Timer is either still running or has not been started")
		{
		}
	}
}
