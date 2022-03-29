using System;

namespace Licensor
{
	public class PhasePlayabilityState
	{
		public bool IsPlayable { get; }
		public bool IsPermanentlyUnplayable { get; }
		public string ReasonForUnplayability { get; private set; }
		public bool HasBeenPlayedTooManyTimes { get; private set; }
	}

	public interface IGameLicence
	{
		PhasePlayabilityState GetPhasePlayability (int roundToPhase);

		void PlayPhase (int phase);
		Guid Id { get; }
		DateTime? ValidFromUtc { get; }
		GameDetails GameDetails { get; }
		bool IsPlayable { get; }
		int? PhasePlayLimit { get; }
		void MakeUnplayable ();
		void UpdateDetails (GameDetails gameDetails);
		void Upload ();
		int GetTimesPhasePlayed (int gameFileCurrentPhaseNumber);
		void FullyUnlock ();
	}
}