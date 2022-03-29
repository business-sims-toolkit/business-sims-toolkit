using System;
using System.Collections;
using LibCore;
using Network;
using CoreUtils;

using Logging;

namespace IncidentManagement
{
	/// <summary>
	/// Summary description for SecondsRunner.
	/// </summary>
	public class SecondsRunner : ITimedClass
	{
		StopControlledTimer _timer = new StopControlledTimer();
		NodeTree _nodeTree = null;
		Node _currentTimeNode = null;
		int freezeAt = 0;
		int secondDurationInMs;
		float timesRealTime;
		int? maximumTicksPerSecond;
		int gameSecondsPerTimerTick;

		public SecondsRunner(NodeTree nt)
		{
			_nodeTree = nt;
			_currentTimeNode = nt.GetNamedNode("CurrentTime");
			_currentTimeNode.AttributesChanged += _currentTimeNode_AttributesChanged;
			TimeManager.TheInstance.ManageClass(this);
			_timer = new StopControlledTimer();
			secondDurationInMs = _currentTimeNode.GetIntAttribute("game_second_duration_in_real_world_ms", 1000);
			_timer.Interval = secondDurationInMs;
			timesRealTime = 1;
			gameSecondsPerTimerTick = 1;
			_timer.Tick += _timer_Tick;

			maximumTicksPerSecond = null;
		}

		void _currentTimeNode_AttributesChanged (Node sender, ArrayList attrs)
		{
			foreach (AttributeValuePair avp in attrs)
			{
				switch (avp.Attribute)
				{
					case "game_second_duration_in_real_world_ms":
						_timer.Stop();
						_timer.Interval = (int) (CONVERT.ParseInt(avp.Value) * 1.0f / timesRealTime);
						_timer.Start();
						break;
				}
			}
		}

		public void Dispose()
		{
			_currentTimeNode.AttributesChanged -= _currentTimeNode_AttributesChanged;
			TimeManager.TheInstance.UnmanageClass(this);

			if (_timer != null)
			{
				_timer.Stop();
				_timer.Dispose();
				_timer = null;
			}
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
		}

		public void FastForward(double timesRealTime)
		{
			this.timesRealTime = (float) timesRealTime;

			int interval;
			if (maximumTicksPerSecond.HasValue)
			{
				float ticksPerSecond = (float) (timesRealTime * 1000.0f / secondDurationInMs);
				ticksPerSecond = Math.Min(ticksPerSecond, maximumTicksPerSecond.Value);
				gameSecondsPerTimerTick = (int) (timesRealTime / ticksPerSecond);
				interval = (int) Math.Max(1, 1000 / ticksPerSecond);
			}
			else
			{
				gameSecondsPerTimerTick = 1;
				interval = Math.Max(1, (int) (secondDurationInMs * 1.0 / timesRealTime));
			}

			_timer.Interval = interval;
		}

		#endregion

		void _timer_Tick(object sender, EventArgs e)
		{
#if !PASSEXCEPTIONS
			try
			{
#endif
				int seconds = _currentTimeNode.GetIntAttribute("seconds", 0);

				int advance = gameSecondsPerTimerTick;
				int endTime = _currentTimeNode.GetIntAttribute("round_duration", 0);

				if (endTime > 0)
				{
					advance = Math.Min(advance, endTime - seconds);
				}

				if ((freezeAt == 0) || (seconds < freezeAt))
				{
					seconds += advance;
					_currentTimeNode.SetAttribute("seconds",seconds);
				}

				OnTick();
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

		public int? MaximumTicksPerSecond
		{
			get
			{
				return maximumTicksPerSecond;
			}

			set
			{
				maximumTicksPerSecond = value;

				if (_timer.Enabled)
				{
					FastForward(timesRealTime);
				}
			}
		}

		public event EventHandler<EventArgs> Tick;

		void OnTick ()
		{
			Tick?.Invoke(this, EventArgs.Empty);
		}
	}
}