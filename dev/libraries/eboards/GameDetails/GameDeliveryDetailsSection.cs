namespace GameDetails
{
	public abstract class GameDeliveryDetailsSection : GameDetailsSection
	{
		public abstract string GameTitle { get; }
		public abstract string GameVenue { get; }
		public abstract string GameLocation { get; }
		public abstract string GameRegion { get; }
		public abstract string GameCountry { get; }
		public abstract string GameClient { get; }
		public abstract string GameChargeCompany { get; }
		public abstract string GameNotes { get; }
		public abstract string GamePurpose { get; }
		public abstract int GamePlayers { get; }
	}
}