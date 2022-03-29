using LibCore;

namespace GameDetails
{
	public interface IGameLoader
	{
		void LoadGame (GameManagement.NetworkProgressionGameFile _gameFile, bool trainingGame);

		void RefreshMaturityScoreSet ();

		bool ResetActivation (string password);

		bool RefreshActivation ();
	}
}