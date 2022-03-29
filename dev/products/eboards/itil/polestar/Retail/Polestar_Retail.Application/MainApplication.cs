using System.Drawing;
using ApplicationUi;
using Licensor;

namespace Polestar_Retail.Application
{
	class MainApplication : EboardApplication
	{
		protected override SplashScreen CreateSplashScreen ()
		{
			return new SplashScreen(new RectangleF(0, 243, 500, 12), Color.FromArgb(34, 40, 49),
				new RectangleF(410, 281, 77, 24), Color.Black);
		}

		protected override GameForm CreateMainGameForm (IProductLicensor productLicensor, IProductLicence productLicence)
		{
			return new MainGameForm(productLicensor, productLicence);
		}
	}
}