using System;
using System.Drawing;
using ApplicationUi;
using Licensor;

namespace DevOps.Application
{
	class MainApplication : EboardApplication
	{
		protected override SplashScreen CreateSplashScreen ()
		{
			return new SplashScreen(new RectangleF(5, 235, 490, 10), Color.Black,
				new RectangleF(410, 281, 77, 24), Color.Black);
		}

		protected override GameForm CreateMainGameForm (IProductLicensor productLicensor, IProductLicence productLicence)
		{
			return new MainGameForm(productLicensor, productLicence);
		}
	}
}