using GameDetails;
using GameManagement;
using Licensor;

namespace Cloud.OpsScreen
{
	public class Cloud_GameSelectionScreen : GameSelectionScreen
	{
		public Cloud_GameSelectionScreen (IGameLoader gameLoader, IProductLicence productLicence, IProductLicensor productLicensor)
			: base (gameLoader, productLicence, productLicensor)
		{
		}

		protected override NetworkProgressionGameFile CreateGameFile (string filename, string directory, bool allowSave, bool allowWriteToDisk,
																	IGameLicence licence)
		{
			return GameManagement.NetworkProgressionGameFile.CreateNew_Cloud(filename, directory, allowSave, allowWriteToDisk, licence);
		}

		protected override GameManagement.NetworkProgressionGameFile OpenExistingGameFile(string filename, string roundOneFilesDir, bool allowSave, bool allowWriteToDisk)
		{
			return GameManagement.NetworkProgressionGameFile.OpenExisting_Cloud(filename, roundOneFilesDir, allowSave, allowWriteToDisk);
		}
	}
}
