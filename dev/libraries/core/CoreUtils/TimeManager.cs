using System;
using System.Collections;
using System.Windows.Forms;

namespace CoreUtils
{
	public sealed class TimeManager : ITimedClass, IPostNotifiedTimedClass
	{
		Hashtable timedClasses = new Hashtable();
		Hashtable timedClassesToAdd = new Hashtable ();
		ArrayList timedClassesToRemove = new ArrayList ();

		int handlerDepth = 0;

		bool running = false;
		double fastForwardSpeed = 1;
		double currentSpeed;

		public static readonly TimeManager TheInstance = new TimeManager();

		TimeManager()
		{
		}

		public int GetNumTimedClasses()
		{
			return timedClasses.Keys.Count;
		}

		public bool TimeIsRunning
		{
			get
			{
				return running;
			}
		}

		public double FastForwardSpeed
		{
			get
			{
				return fastForwardSpeed;
			}
		}

		public void ManageClass(ITimedClass itc)
		{
			if (handlerDepth == 0)
			{
				timedClasses.Add(itc,1);
			}
			else
			{
				timedClassesToAdd.Add(itc, 1);
			}

			if(running) itc.Start();
			else itc.Stop();
		}

		public void UnmanageClass(ITimedClass itc)
		{
			if (handlerDepth == 0)
			{
				if(timedClasses.ContainsKey(itc))
				{
					timedClasses.Remove(itc);
				}
				
				if (timedClassesToAdd.ContainsKey(itc))
				{
					timedClassesToAdd.Remove(itc);
				}
			}
			else
			{
				timedClassesToRemove.Add(itc);
			}
		}

		#region ITimedClass Members

		public void Start()
		{
			BeforeStart();

			running = true;
			currentSpeed = 1;

			try
			{
				handlerDepth++;
				foreach(ITimedClass itc in timedClasses.Keys)
				{
					itc.Start();
				}
			}
			finally
			{
				handlerDepth--;
				UpdateList();
			}
		}

		public void Stop()
		{
			running = false;
			currentSpeed = 0;

			try
			{
				handlerDepth++;
				foreach(ITimedClass itc in timedClasses.Keys)
				{
					itc.Stop();
				}
			}
			finally
			{
				handlerDepth--;
				UpdateList();
			}

			AfterStop();

			GC.Collect();
			GC.WaitForPendingFinalizers();
		}

		public void Reset()
		{
			try
			{
				handlerDepth++;
				foreach(ITimedClass itc in timedClasses.Keys)
				{
					itc.Reset();
					itc.FastForward(1);
				}
			}
			finally
			{
				handlerDepth--;
				UpdateList();
			}
		}

		public void FastForward (double timesRealTime, bool startClock)
		{
			if (startClock && ! running)
			{
				Start();
			}

			fastForwardSpeed = timesRealTime;
			currentSpeed = fastForwardSpeed;

			try
			{
				handlerDepth++;
				foreach (ITimedClass itc in timedClasses.Keys)
				{
					itc.FastForward(timesRealTime);
				}
			}
			finally
			{
				handlerDepth--;
				UpdateList();
			}
		}

		public void FastForward(double timesRealTime)
		{
			FastForward(timesRealTime, true);
		}

		#endregion

		#region IPostNotifiedTimedClass Members

		public void BeforeStart()
		{
			try
			{
				handlerDepth++;
				foreach(ITimedClass itc in timedClasses.Keys)
				{
					IPostNotifiedTimedClass pitc = itc as IPostNotifiedTimedClass;
					if(pitc != null)
					{
						pitc.BeforeStart();
					}
				}
			}
			finally
			{
				handlerDepth--;
				UpdateList();
			}
		}

		public void AfterStop()
		{
			try
			{
				handlerDepth++;
				foreach(ITimedClass itc in timedClasses.Keys)
				{
					IPostNotifiedTimedClass pitc = itc as IPostNotifiedTimedClass;
					if(pitc != null)
					{
						pitc.AfterStop();
					}
				}
			}
			finally
			{
				handlerDepth--;
				UpdateList();
			}
		}

		#endregion

		void UpdateList ()
		{
			if (handlerDepth == 0)
			{
				foreach (ITimedClass itc in timedClassesToAdd.Keys)
				{
					timedClasses.Add(itc, timedClassesToAdd[itc]);
				}

				foreach (ITimedClass itc in timedClassesToRemove)
				{
					timedClasses.Remove(itc);
				}

				timedClassesToAdd.Clear();
				timedClassesToRemove.Clear();
			}
		}

		public double CurrentSpeed
		{
			get
			{
				return currentSpeed;
			}
		}
	}
}