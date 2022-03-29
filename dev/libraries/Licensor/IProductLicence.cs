using System;

namespace Licensor
{
	public interface IProductLicence
	{
		IGameLicence GetGameLicence (Guid guid);
		IGameLicence CreateSalesGameLicence (GameDetails gameDetails);
		bool CanCreateChargeableGame ();
		IGameLicence CreateChargeableGameLicence (GameDetails gameDetails);
		bool CanWeCreateGamesOffTheRecord { get; }
		int GameCreditsRemaining { get; }
		object TimeLastUpdatedUtc { get; }
		bool UpgradeIsAvailable { get; }
		string UpgradeMessage { get; }
		string UpgradeDownloadUrl { get; }
		string UpgradeDownloadUserName { get; }
		string UpgradeDownloadPassword { get; }
		UserDetails UserDetails { get; }
		bool CanBeMadeValidByRenew { get; }
		bool IsValid { get; }
		object ReasonNotValid { get; }
		IGameLicence CreateTrainingGameLicence (object o);
		IGameLicence CreateUnofficialGameLicence (GameDetails gameDetails);
		bool MustBeOnlineToPlay (IGameLicence gameLicence);
		event EventHandler<EventArgs> RefreshStatusComplete;
		bool VerifyPassword (string passwordValue);
		void BeginRefreshStatus ();
		void Deactivate ();
		bool GetPermissionToStartPlay (IGameLicence gameFileLicence, int roundToPhase);
		void Save ();
		void Renew ();
	}
}