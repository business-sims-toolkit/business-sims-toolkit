using System;
using System.Drawing;
using System.Windows.Forms;
using Network;
using CoreUtils;
using TransitionObjects;

using IncidentManagement;

namespace CommonGUI
{
	public interface IOpsScreen
	{
		void CloseToolTips ();
	}

		/// </summary>
	public class OpsControlPanelBase : FlickerFreePanel, ITimedClass, IDataEntryControlHolder
	{
		public delegate void PanelClosedHandler();
		public event PanelClosedHandler PanelClosed;
		
		public IncidentApplier _iApplier;
		public MirrorApplier _mirrorApplier;
		public ProjectManager _prjmanager;

		protected Control shownControl;
		protected NodeTree _network;
		protected bool playing = false;

		protected int popup_xposition = 9;
		protected int popup_yposition = 440;
		protected int popup_width = 607;
		protected int popup_height = 185;
		protected int buttonwidth = 60;
		protected int buttonheight = 27;
		protected int buttonseperation = 0;

		protected int _round;
		protected int _round_maxmins;
		protected Boolean MyIsTrainingFlag;
		protected Color MyGroupPanelBackColor;
		protected Color MyOperationsBackColor;

		protected GameManagement.NetworkProgressionGameFile gameFile;


		public virtual void Start()
		{
		}

		public virtual void FastForward(double timesRealTime)
		{
		}

		public virtual void Reset()
		{
		}

		public virtual void Stop()
		{
		}

		public virtual void DisposeEntryPanel_indirect(int which)
		{ 
		}

		public virtual void DisposeEntryPanel()
		{ 
		}

		public virtual void ShowEntryPanel (Control control)
		{
			shownControl = control;
		}

		//This used to ask the control to open a second popup based on a passed choice
		public virtual void SwapToOtherPanel(int which_operation)
		{ 
		}

		public IncidentApplier IncidentApplier
		{
			get
			{
				return _iApplier;
			}
		}

		public void RaisePanelClosed()
		{
			PanelClosed?.Invoke();
		}

	}
}
