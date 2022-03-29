using System;
using LibCore;

namespace NetworkScreens
{
	/// <summary>
	/// Summary description for ToolsScreenBase.
	/// </summary>
	public abstract class ToolsScreenBase : BasePanel
	{
		public ToolsScreenBase()
		{
		}

		public virtual void ResetView ()
		{
		}

		public virtual void readNetwork()
		{
		}

		public abstract void RemoveTabByName (string tabName);

		public abstract void RemoveTabByCode (int tabCode);

		public abstract void RefreshMaturityScoreSet ();

        public virtual void RefreshSupportCosts()
        {
        }

		public virtual void DisposeEditBox ()
		{
		}

		public virtual void RemoveInfrastructureBoard ()
		{
		}

		public virtual void SelectTab (string tabName)
		{
		}

	    protected override void OnSizeChanged (EventArgs e)
	    {
	        base.OnSizeChanged(e);
	        DoSize();
	    }

	    protected virtual void DoSize ()
	    {
	    }
	}
}