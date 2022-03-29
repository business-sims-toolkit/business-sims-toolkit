using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using LibCore;
using Logging;

namespace ApplicationUi
{
	public class AppLoader : IDisposable
	{
		Mutex mutex;
		bool canRun;

		ApplicationContext context;

		Form splash;
		List<Form> windows;

		public AppLoader ()
		{
			mutex = new Mutex (false, "Global\\" + Application.ProductName);

			if (mutex.WaitOne(TimeSpan.FromSeconds(5)))
			{
				canRun = true;

				context = new ApplicationContext ();

#if ! DEBUG
				Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
				AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
				Application.ThreadException += Application_ThreadException;
#endif
			}
			else
			{
				MessageBox.Show(null, Application.ProductName + " is already running.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
				canRun = false;
			}

			windows = new List<Form> ();
		}

		public void Dispose ()
		{
			AppDomain.CurrentDomain.UnhandledException -= CurrentDomain_UnhandledException;
			Application.ThreadException -= Application_ThreadException;

			foreach (var window in windows)
			{
				window.Dispose();
			}
			splash?.Dispose();

			context?.Dispose();

			mutex?.Dispose();
		}

		public bool CanRun => canRun;

		void CurrentDomain_UnhandledException (object sender, UnhandledExceptionEventArgs e)
		{
			AppLogger.TheInstance.WriteException("App Level Exception", e.ExceptionObject as Exception);
			ExceptionHandling.ShowExceptionDialog(e.ExceptionObject as Exception);
		}

		void Application_ThreadException (object sender, ThreadExceptionEventArgs e)
		{
			AppLogger.TheInstance.WriteException("App Level Exception", e.Exception);
			ExceptionHandling.ShowExceptionDialog(e.Exception);
		}

		public void ShowSplash (Form splash)
		{
			this.splash = splash;
			context.MainForm = splash;

			splash.Show();
		}

		public void ShowWindow (Form window)
		{
			ShowWindows(window);
		}

		public void ShowWindows (params Form [] windows)
		{
			ShowWindows((IList<Form>) windows);
		}

		public void ShowWindows (IList<Form> windows)
		{
			splash.Hide();

			this.windows.AddRange(windows);
			context.MainForm = windows[0];

			foreach (var window in windows)
			{
				window.Show();
			}
		}

		public void Run ()
		{
			Application.Run(context);
		}

		public void RunWindow (Form window)
		{
			ShowWindow(window);
			Run();
		}
	}
}