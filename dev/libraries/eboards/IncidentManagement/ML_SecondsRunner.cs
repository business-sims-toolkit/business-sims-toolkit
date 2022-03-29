using System;
using LibCore;
using Network;
using CoreUtils;

using Logging;

namespace IncidentManagement
{
	/// <summary>
	/// Summary description for ML_SecondsRunner.
	/// </summary>
	public class ML_SecondsRunner : ITimedClass
	{
		StopControlledTimer _timer = new StopControlledTimer();
		NodeTree _nodeTree = null;
		Node _currentTimeNode = null;
		Node _elapsedTimeNode = null;
		int freezeAt = 0;
		//private int sixtiethsPerTick = 1;
		//
		public ML_SecondsRunner(NodeTree nt)
		{
			_nodeTree = nt;
			_currentTimeNode = nt.GetNamedNode("CurrentTime");
			_elapsedTimeNode = nt.GetNamedNode("ElapsedTime");
			TimeManager.TheInstance.ManageClass(this);
			_timer = new StopControlledTimer();
			_timer.Interval = 1000;
			_timer.Tick += _timer_Tick;
		}

		public void Dispose()
		{
			TimeManager.TheInstance.UnmanageClass(this);
			_timer.Stop();
			_timer.Dispose();
			_timer = null;
			_currentTimeNode = null;
			_elapsedTimeNode = null;
		}

		#region ITimedClass Members

		public void Start()
		{
			_timer.Start();
		}

		public void Stop()
		{
			_timer.Stop();
		}

		public void Reset()
		{
			_currentTimeNode.SetAttribute("seconds", "0");
			_elapsedTimeNode.SetAttribute("seconds", "0"); 
		}

		public void FastForward(double timesRealTime)
		{
			// Don't try to run faster than 60 Hz.
			//Old Code
			//_timer.Interval = Math.Max(1000 / 60, (1000.0 / timesRealTime));
			//sixtiethsPerTick = Math.Max(1, timesRealTime / (1000.0 / _timer.Interval));
			//Old Code
			_timer.Interval = (int)(1000.0/ timesRealTime);
			//sixtiethsPerTick = Math.Max(1, timesRealTime / (1000 / _timer.Interval));
		}

		#endregion

		void _timer_Tick(object sender, EventArgs e)
		{
#if !PASSEXCEPTIONS
			try
			{
#endif

			int seconds = _currentTimeNode.GetIntAttribute("seconds",0);
			int e_seconds = _elapsedTimeNode.GetIntAttribute("seconds",0);

			if ((freezeAt == 0) || (seconds < freezeAt))
			{
				seconds++;
				e_seconds++;
				_currentTimeNode.SetAttribute("seconds",seconds);
				_elapsedTimeNode.SetAttribute("seconds", e_seconds);
			}
#if !PASSEXCEPTIONS
			}
			catch(Exception ex)
			{
				AppLogger.TheInstance.WriteException("Timer Level Exception", ex);
			}
#endif
		}

		public void FreezeAt (int freezeAt)
		{
			this.freezeAt = freezeAt;
		}
	}
}