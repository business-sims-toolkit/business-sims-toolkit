using System;
using LibCore;

namespace Cloud.Application
{
	public static class Program
	{
		[STAThread]
		public static void Main (string[] scriptArgsAsArray)
		{
			using (var application = new MainApplication ())
			{
				var useLicensor = true;
				var useXnd = true;

#if NO_XND
				useXnd = false;
#endif

#if ENABLE_LICENCE_FREE_PLAY
				useLicensor = false;
#endif

				Logging.AppLogger.TheInstance.WriteLine("Loading data from " + AppInfo.TheInstance.Location);

				application.Run(scriptArgsAsArray, useLicensor, useXnd);
			}
		}
	}
}