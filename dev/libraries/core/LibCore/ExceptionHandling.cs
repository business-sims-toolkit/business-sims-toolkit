using System;
using System.Threading;
using System.Windows.Forms;

namespace LibCore
{
	public static class ExceptionHandling
	{
		public static void ShowExceptionDialog (Exception exception)
		{
			Thread thread = new Thread(ShowExceptionThreadFunction);
			thread.Start(exception);
			thread.Join();
		}

		static void ShowExceptionThreadFunction (object exceptionAsObject)
		{
			Exception exception = exceptionAsObject as Exception;

			using (ThreadExceptionDialog dialog = new ThreadExceptionDialog (exception))
			{
				DialogResult result = dialog.ShowDialog();

				// The result value changes between different .NET versions, so cover several bases.
				if ((result != DialogResult.Ignore)
					&& (result != DialogResult.Retry)
					&& (result != DialogResult.OK)
					&& (result != DialogResult.Cancel))
				{
					System.Windows.Forms.Application.Exit();
				}
			}
		}
	}
}