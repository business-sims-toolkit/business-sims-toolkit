using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using CoreUtils;
using GameDetails;
using GameManagement;
using LibCore;
using Licensor;

namespace DevOps.OpsScreen
{
	public class DevOpsGameSelectionScreen : GameSelectionScreen
	{
		public DevOpsGameSelectionScreen (IGameLoader gameLoader, IProductLicence productLicence, IProductLicensor productLicensor)
			: base (gameLoader, productLicence, productLicensor)
		{
#if DEVTOOLS
			ShowDevTools();
#endif
		}

		protected override void AddLogoSection ()
		{
			base.AddLogoSection();

			logoBox.Height = 300;
			logoSection.Size = new Size (logoBox.Right, logoBox.Bottom + 50);
		}
	}
}