using System.Collections.Generic;
using System.Drawing;
using CommonGUI;
using LibCore;

namespace ChartScreens
{
	/// <summary>
	/// This is the empty base class for all Tabbed ChartScreens
	/// </summary>
	public abstract class PureTabbedChartScreen : BasePanel
	{
		public enum ChartPanel
		{
			ScoreCards = 2,
			GanttChart = 3,
			ComparisionGraph = 5,
			MaturityGraph = 4,
			LeaderBoard = 0,
			RacePoints = 1,
			Network = 7,
			Transition = 6,
			Incidents = 8,
			KB = 9
		}

		public TabBar TabBar { get; protected set; }

		public PureTabbedChartScreen ()
		{
		}

		public virtual void Init(ChartPanel screen)
		{
		}

		public abstract void ReloadDataAndShow(bool reload);

		public abstract IList<ChartScreenTabOption> GetAllAvailableReports ();

		public abstract void ShowReport (ChartScreenTabOption report);
	}
}
