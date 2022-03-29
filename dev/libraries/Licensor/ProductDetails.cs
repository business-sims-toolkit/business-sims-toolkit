namespace Licensor
{
	public class ProductDetails
	{
		private string productName;
		private string productVersion;
		private string location;
		private string v;

		public ProductDetails(string productName, string productVersion, string location, string v)
		{
			this.productName = productName;
			this.productVersion = productVersion;
			this.location = location;
			this.v = v;
		}
	}
}