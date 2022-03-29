using System.Collections;
using System.Collections.Generic;

namespace Licensor
{
	public interface IProductLicensor
	{
		IList<string> UserNamePrefices { get; }
		IProductLicence ActivateProductAndGetLicence (ProductDetails productDetails, UserDetails userDetails);
		IProductLicence GetProductLicence (ProductDetails productDetails);
	}
}