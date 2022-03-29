using System;
using System.Collections;
using System.Text;
using System.Threading;

using System.Windows.Forms;

using System.Xml;

using Polestar_PM.OpsScreen;
using GameDetails;

using LibCore;

using GameManagement;

using Logging;

namespace Polestar_PM.Application
{
    class AppLoader
    {
        private static SplashForm splash;
        public static ApplicationContext context;
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            using (Mutex mutex = new Mutex(false, "Global\\MainApp_POLESTARPM"))
            {
                // Try to acquire the mutex.
                if (!mutex.WaitOne(0, false))
                {
                    // We have failed to acquire with a non-blocking request so exit. 
                    // We should probably display a dialog box explaining why to the user.
                    MessageBox.Show(null, "Another copy of " + System.Windows.Forms.Application.ProductName + " is already running.",
                        System.Windows.Forms.Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
#if !PASSEXCEPTIONS
                try
                {
#endif
                    AppLogger appLogger = AppLogger.TheInstance;
                    context = new ApplicationContext();
					System.Windows.Forms.Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

					splash = new SplashForm();
                    splash.Show();
                    splash.Start();

					AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
					System.Windows.Forms.Application.ThreadException += new ThreadExceptionEventHandler(Application_ThreadException);
					System.Windows.Forms.Application.Run(context);
					System.Windows.Forms.Application.ThreadException -= new ThreadExceptionEventHandler(Application_ThreadException);
					AppDomain.CurrentDomain.UnhandledException -= new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
#if !PASSEXCEPTIONS
				}
				catch (Exception exception)
				{
					AppLogger.TheInstance.WriteException("App Level Exception", exception);
					ExceptionHandling.ShowExceptionDialog(exception);
				}
#endif
			}
		}

		static void CurrentDomain_UnhandledException (object sender, UnhandledExceptionEventArgs e)
		{
			AppLogger.TheInstance.WriteException("App Level Exception", e.ExceptionObject as Exception);
			ExceptionHandling.ShowExceptionDialog(e.ExceptionObject as Exception);
		}

		static void Application_ThreadException (object sender, ThreadExceptionEventArgs e)
		{
			AppLogger.TheInstance.WriteException("App Level Exception", e.Exception);
			ExceptionHandling.ShowExceptionDialog(e.Exception);
		}
	}
}