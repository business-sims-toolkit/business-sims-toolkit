using System;
using GameEngine;

namespace CommonGUI
{
	public interface IPhaseScreen : IDisposable
	{
		void ImportIncidents (string filename);

		bool IsGameFinished ();

		event PhaseFinishedHandler PhaseFinished;
	}
}