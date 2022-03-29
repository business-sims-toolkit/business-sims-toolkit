using System.Windows.Forms;
using ApplicationUi;
using CoreScreens;
using Cloud.OpsScreen;
using GameManagement;
using Licensor;
using Environment = System.Environment;

namespace Cloud.Application
{
	public class MainGameForm : GameForm
	{
		DebugWindow roundPlayForm;

		public MainGameForm (IProductLicensor productLicensor, IProductLicence productLicence)
			: base(productLicence)
		{
			MainPanel = new CompleteGamePanel (productLicence, productLicensor);
			MainPanel.Size = ClientSize;
			Controls.Add(MainPanel);

			MainPanel.GameSetup += mainPanel_GameSetup;
			MainPanel.PasswordCheck += mainPanel_PasswordCheck;

#if DEVTOOLS
			roundPlayForm = new DebugWindow(this);
			roundPlayForm.Show();
			AddOwnedForm(roundPlayForm);
#endif

			DoSize();
		}

		BaseCompleteGamePanel.GameStartType mainPanel_GameSetup (GameFile game)
		{
			try
			{
				var gameDetails = new Licensor.GameDetails(game.GetTitle(), game.GetClient(), game.GetVenue(), game.GetLocation(), game.GetRegion(), game.GetCountry(), game.GetChargeCompany(), game.GetNotes(), game.GetPurpose(), game.GetPlayers());
				game.Licence?.UpdateDetails(gameDetails);

				return BaseCompleteGamePanel.GameStartType.MintedNewCoupon;
			}
			catch
			{
				game.Delete();
				game.Dispose();

				MessageBox.Show("There was a problem creating a Licensed Game. The Application will now exit. Please restart and try again. If problems persist, please contact support", "Licensing Error");
				System.Windows.Forms.Application.Exit();

				return BaseCompleteGamePanel.GameStartType.Failed;
			}
		}

		public void ScreenGrabAllReports ()
		{
			MainPanel.ScreenGrabAllReports(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));
		}
	}
}