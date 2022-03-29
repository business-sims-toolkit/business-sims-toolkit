using GameManagement;
using Licensor;

namespace GameDetails
{
	public class PMGameSelectionScreen : GameSelectionScreen
	{
		em_GameEvalType defaultGameEvalType;

        public PMGameSelectionScreen(IGameLoader gameLoader, IProductLicence productLicence, IProductLicensor productLicensor)
            : base(gameLoader, productLicence, productLicensor)
        {
            defaultGameEvalType = em_GameEvalType.ITIL;
        }

		protected override GameManagement.NetworkProgressionGameFile CreateGameFile (string filename, string directory, bool allowSave, bool allowWriteToDisk, IGameLicence licence)
		{
			NetworkProgressionGameFile gameFile = GameManagement.PMNetworkProgressionGameFile.CreateNewPM(filename, directory, allowSave, allowWriteToDisk, licence);
			gameFile.Game_Eval_Type = defaultGameEvalType;
			return gameFile;
		}

		protected override GameManagement.NetworkProgressionGameFile OpenExistingGameFile (string filename, string roundOneFilesDir, bool allowSave, bool allowWriteToDisk)
		{
			return GameManagement.PMNetworkProgressionGameFile.OpenExistingPM(filename, roundOneFilesDir, allowSave, allowWriteToDisk);
		}
	}
}