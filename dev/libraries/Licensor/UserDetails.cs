namespace Licensor
{
	public class UserDetails
	{
		public UserDetails(string v, string value1, string value2, string value3, string value4, string value5, string value6)
		{
		}

		public string UserName { get; private set; }
		public string Password { get; private set; }
		public string GivenName { get; private set; }
		public string FamilyName { get; private set; }
		public string CompanyName { get; private set; }
		public string Email { get; private set; }
		public string MasterTrainer { get; private set; }
	}
}