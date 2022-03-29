using System;
using LibCore;
using CoreUtils;

namespace GameEngine
{
	/// <summary>
	/// This just a empty base class to allow different implentmentation of the operations phase engine 
	/// </summary>
	public class BaseOpsPhaseEngine : BaseClass, ITimedClass, IDisposable
	{
		public event PhaseFinishedHandler PhaseFinished;

		bool started;
		public virtual bool IsStarted
		{
			get
			{
				return started;
			}
		}

		public BaseOpsPhaseEngine()
		{
			started = false;
		}

		public virtual void Dispose ()
		{
		}

		public virtual void RaiseEvent(BaseOpsPhaseEngine MyHandle)
		{
			if(null != PhaseFinished)
			{
				OnPhaseFinished();
			}		
		}

		protected virtual void OnPhaseFinished ()
		{
			if (PhaseFinished != null)
			{
				PhaseFinished(this);
			}
		}

		#region ITimedClass Members

		public virtual void Start()
		{
			started = true;
		}

		public virtual void Stop()
		{
		}

		public virtual void Reset()
		{
			started = false;
		}

		public virtual void FastForward(double timesRealTime)
		{
		}

		#endregion

		public virtual void SetGameStarted ()
		{
			if (! started)
			{
				OnGameStarted();
			}

			started = true;
		}

		public event EventHandler GameStarted;

		protected virtual void OnGameStarted ()
		{
			if (GameStarted != null)
			{
				GameStarted(this, new EventArgs ());
			}
		}
	}
}