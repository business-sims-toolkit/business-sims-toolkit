using System;
using System.Threading;

namespace LibCore
{
	internal sealed class timerController
	{
		public static readonly timerController TheInstance = new timerController();

		public Mutex mutex = new Mutex();
		public bool insideTickCall;

		timerController()
		{
			insideTickCall = false;
		}
	}
	/// <summary>
	/// The StopControlledTimer class wraps a timer object but using thread locking to prevent multiple
	/// executions on a timer's callback function and to also disallow any subesequent calls to a timer's
	/// callback if the timer has been stopped. This is not the case for the normal Windows timer as if
	/// the timer is stopped while inside the timer's own callback function then another call to the
	/// function can still occur ending in the result of unexpected behaviour and ugly special case code
	/// to check against it happening.
	/// </summary>
	public class StopControlledTimer
	{
		Mutex mutex = new Mutex();
		System.Windows.Forms.Timer _timer;
		bool stopped;

		//protected System.Threading.Timer
		public EventHandler Tick;

		public StopControlledTimer()
		{
			stopped = true;
			_timer = new System.Windows.Forms.Timer();
			_timer.Tick += _timer_Tick;
		}

		public bool Enabled
		{
			get
			{
				lock(this)
				{
					return !stopped;
				}
			}

			set
			{
				lock(this)
				{
					if(value)
					{
						Start();
					}
					else
					{
						Stop();
					}
				}
			}
		}

		public int Interval
		{
			get
			{
				lock(this)
				{
					return _timer.Interval;
				}
			}

			set
			{
				lock(this)
				{
					_timer.Interval = value;
				}
			}
		}

		public void Start()
		{
			lock(this)
			{
				stopped = false;
				_timer.Start();
			}
		}

		public void Stop()
		{
			lock(this)
			{
				stopped = true;
				_timer.Stop();
			}
		}

		public void Dispose()
		{
			lock(this)
			{
				stopped = true;
				_timer.Dispose();
			}
		}

		void _timer_Tick(object sender, EventArgs e)
		{
			timerController.TheInstance.mutex.WaitOne();

			try
			{
				if(timerController.TheInstance.insideTickCall)
				{
					return;
				}
				// Bomb out and don't pass then event on if we have actually been stopped.
				if(stopped)
				{
					return;
				}
				//
				try
				{
					timerController.TheInstance.insideTickCall = true;
					//
					if(Tick != null)
					{
						Tick(this,e);
					}
				}
				finally
				{
					timerController.TheInstance.insideTickCall = false;
				}
			}
			finally
			{
				timerController.TheInstance.mutex.ReleaseMutex();
			}
		}
	}
}
