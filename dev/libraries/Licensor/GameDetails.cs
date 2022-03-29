namespace Licensor
{
	public class GameDetails
	{
		public GameDetails (string v1, string v2, string v3, string v4, string v5, string v6, string v7, string v8, string v9, int v10)
		{
		}

		public string Title { get; private set; }
		public string Venue { get; private set; }
		public string Location { get; private set; }
		public string Client { get; private set; }
		public int Players { get; private set; }
		public string Region { get; private set; }
		public string Country { get; private set; }
		public string Purpose { get; private set; }
		public string ChargeCompany { get; private set; }
		public string Notes { get; private set; }
	}
}