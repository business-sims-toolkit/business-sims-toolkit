using System;
using System.Collections;
using LibCore;
using Network;

namespace BusinessServiceRules
{
	/// <summary>
	/// The Preplay Manager handles the optional 5 minute countdown at the start of round
	/// </summary>
	public class PreplayManager : IDisposable
	{
		protected NodeTree _model;
		protected Node _PrePlayControlNode;
		protected Node _PrePlayStatusNode;

		StopControlledTimer _timer = new StopControlledTimer();

		/// <summary>
		/// 
		/// </summary>
		/// <param name="nt"></param>
		public PreplayManager(NodeTree nt)
		{
			_model = nt;
			_PrePlayControlNode	= _model.GetNamedNode("preplay_control");
			_PrePlayControlNode.AttributesChanged +=_PrePlayControlNode_AttributesChanged;	
			_PrePlayStatusNode	= _model.GetNamedNode("preplay_status");

			_timer = new StopControlledTimer();
			_timer.Interval = 1000;
			_timer.Tick += _timer_Tick;

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
			//_currentTimeNode.SetAttribute("seconds", "0");
		}

		public void FastForward(double timesRealTime)
		{
			_timer.Interval = (int) (1000/timesRealTime);
		}

		#endregion

		void _timer_Tick(object sender, EventArgs e)
		{
			int time_left = _PrePlayStatusNode.GetIntAttribute("time_left",0);
			Boolean preplay_running = _PrePlayStatusNode.GetBooleanAttribute("preplay_running",false);

			if (preplay_running)
			{
				if (time_left>0)
				{
					time_left--;
					_PrePlayStatusNode.SetAttribute("time_left",CONVERT.ToStr(time_left));
				}
				else
				{
					_PrePlayStatusNode.SetAttribute("time_left",CONVERT.ToStr(0));
					_PrePlayStatusNode.SetAttribute("preplay_running","false");
					_timer.Stop();
					OnPrePlayEnded();
				}
			}
		}

		/// <summary>
		/// Dispose all event handlers and anything else
		/// </summary>
		public void Dispose()
		{
			if (_timer != null)
			{
				_timer.Stop();
				_timer.Dispose();
				_timer = null;
			}

			if (_PrePlayControlNode != null)
			{
				_PrePlayControlNode.AttributesChanged -=_PrePlayControlNode_AttributesChanged;	
			}
		}

		void _PrePlayControlNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			if (attrs != null && attrs.Count > 0)
			{
				foreach(AttributeValuePair avp in attrs)
				{
					if (avp.Attribute == "start")
					{
						int time = sender.GetIntAttribute("time_ref",300);
						//reset to start of preplay mode 
						_PrePlayStatusNode.SetAttribute("time_left",CONVERT.ToStr(time));
						_PrePlayStatusNode.SetAttribute("preplay_running","true");
						//start my timer 
						this.Start();
					}
					else if (avp.Attribute == "escape")
					{
						this.Stop();
						_PrePlayStatusNode.SetAttribute("preplay_running", "false");
						_PrePlayStatusNode.SetAttribute("time_left", "0");
						OnPrePlayEnded();
					}
				}
			}
		}

		public event EventHandler PrePlayEnded;

		void OnPrePlayEnded ()
		{
			if (PrePlayEnded != null)
			{
				PrePlayEnded(this, EventArgs.Empty);
			}
		}
	}
}
