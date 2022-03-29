using System.Drawing;
using ApplicationUi;
using Licensor;

namespace Cloud.Application
{
	class MainApplication : EboardApplication
	{
		protected override SplashScreen CreateSplashScreen ()
		{
			return new SplashScreen(new RectangleF(0, 243, 500, 12), Color.Black,
				new RectangleF(410, 281, 77, 24), Color.Black);
		}

		protected override GameForm CreateMainGameForm (IProductLicensor productLicensor, IProductLicence productLicence)
		{
			return new MainGameForm(productLicensor, productLicence);
		}
	}
}