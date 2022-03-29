using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using GameDetails;
using LibCore;
using Licensor;

namespace ApplicationUi
{
	public abstract class EboardApplication : IDisposable
	{
		ProductActivationWindow activationWindow;

		ProductDetails productDetails;
		UserDetails userDetails;
		Licensor.GameDetails gameDetails;

		IProductLicensor productLicensor;
		IProductLicence productLicence;
		IGameLicence gameLicence;
		bool cancelActivation;

		List<string> extractedFiles;

		public EboardApplication ()
		{
		}

		public void Dispose ()
		{
		}

		protected abstract SplashScreen CreateSplashScreen ();

		protected abstract GameForm CreateMainGameForm (IProductLicensor productLicensor, IProductLicence productLicence);

		public void Run (string [] scriptArgsAsArray, bool useLicensor, bool useXnd)
		{
			var scriptArgs = new List<string> ();
			if (scriptArgsAsArray != null)
			{
				scriptArgs.AddRange(scriptArgsAsArray);
			}

			using (var loader = new AppLoader())
			{
				if (loader.CanRun)
				{
					AppDomain.CurrentDomain.ProcessExit += currentDomain_ProcessExit;

					if (scriptArgs.Contains("-unittests"))
					{
						scriptArgs.Remove("-unittests");
					}

					bool windowless = false;
					if (scriptArgs.Contains("-windowless"))
					{
						scriptArgs.Remove("-windowless");
						windowless = true;
					}

					SplashScreen splashScreen = null;

					if (! windowless)
					{
						splashScreen = CreateSplashScreen();
						loader.ShowSplash(splashScreen);
					}

					if (splashScreen != null)
					{
						splashScreen.Progress = new ProgressProperties { Max = 1, Value = 0 };
					}

					extractedFiles = new List<string> ();

					if (useXnd)
					{
						AppXnd.UnZipAppXnd(extractedFiles, (sender, args) =>
						{
							if (splashScreen != null)
							{
								splashScreen.Progress = new ProgressProperties { Max = args.TotalItems, Value = args.ItemsHandled };
							}
						});
					}

					if (useLicensor)
					{
						productLicensor = ProductLicensor.GetLicensor();
					}
					else
					{
						productLicensor = ProductLicensor.GetUnrestrictedLicensor();
					}

					if (splashScreen != null)
					{
						splashScreen.Progress = new ProgressProperties { Max = 1, Value = 1 };
					}

					productDetails = new ProductDetails(Application.ProductName, Application.ProductVersion,
						AppInfo.TheInstance.Location, "");
					do
					{
						productLicence = productLicensor.GetProductLicence(productDetails);

						if ((productLicence != null) && productLicence.CanBeMadeValidByRenew)
						{
							productLicence.Renew();
						}
						else if (((productLicence == null) || ! productLicence.IsValid)
						         && ! windowless)
						{
							activationWindow = new ProductActivationWindow (productLicensor, userDetails);
							activationWindow.OkClicked += activationWindow_OkClicked;
							activationWindow.CancelClicked += activationWindow_CancelClicked;
							activationWindow.FormClosed += activationWindow_FormClosed;

							if (! IsEulaSectionNeeded)
							{
								activationWindow.SkipEulaSection();
							}

							if (! IsGdprSectionNeeded)
							{
								activationWindow.SkipGdprSection();
							}

							loader.RunWindow(activationWindow);
						}
					}
					while (((productLicence == null) || !productLicence.IsValid)
					       && (!windowless)
					       && (! cancelActivation));

					if (productLicence != null)
					{
						productLicence.Save();
						productLicence.RefreshStatusComplete += productLicence_RefreshStatusComplete;
						productLicence.BeginRefreshStatus();

						if (!windowless)
						{
							GameForm mainWindow = CreateMainGameForm(productLicensor, productLicence);
							if (gameLicence != null)
							{
								mainWindow.CreateAndLoadGame(gameLicence);
							}
							loader.ShowWindow(mainWindow);

							if (scriptArgs.Count > 0)
							{
								mainWindow.RunScript(scriptArgs);
							}
							else
							{
								loader.Run();
							}
						}
					}
				}
			}
		}

		void activationWindow_OkClicked (object sender, EventArgs args)
		{
			userDetails = activationWindow.UserDetails;
			gameDetails = activationWindow.GameDetails;

			if (false)
			{
			}
			else
			{
				try
				{
					productLicence = productLicensor.ActivateProductAndGetLicence(productDetails, userDetails);
					if (productLicence.IsValid)
					{
						activationWindow.Close();
					}
					else
					{
						throw new ActivationException (productLicence.ReasonNotValid);
					}
				}
				catch (ActivationException e)
				{
					MessageBox.Show(null, e.Message, "Product License Activation Error");
				}
			}
		}

		void activationWindow_CancelClicked (object sender, EventArgs args)
		{
			cancelActivation = true;
			activationWindow.Close();
		}

		void activationWindow_FormClosed (object sender, EventArgs args)
		{
			if (! activationWindow.ChoiceMade)
			{
				cancelActivation = true;
			}
		}

		void productLicence_RefreshStatusComplete (object sender, EventArgs e)
		{
			var licence = (IProductLicence) sender;
			licence.Save();
		}

		void currentDomain_ProcessExit (object sender, EventArgs e)
		{
			BaseUtils.AppTidyUp.DeleteUnzippedFiles(extractedFiles);
		}

		protected virtual bool IsEulaSectionNeeded => true;

		protected virtual bool IsGdprSectionNeeded => true;
	}
}