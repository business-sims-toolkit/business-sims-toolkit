using System;

using System.Security.Principal;

namespace LibCore
{
	public static class AdminPrivileges
	{
		public static bool DoWeHaveAdminPrivileges ()
		{
			return new WindowsPrincipal (WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
		}

		public static bool ErrorIfNotAdminPrivileges ()
		{
			bool doWeHaveAdminPrivileges = DoWeHaveAdminPrivileges();

			if (! doWeHaveAdminPrivileges)
			{
				System.Windows.Forms.MessageBox.Show("To run " + AppInfo.TheInstance.GetApplicationName() + ", you must be logged in as an administrator.");
			}

			return doWeHaveAdminPrivileges;
		}

		public static bool CheckAdminPrivilegesIfNecessary ()
		{
			bool quit = false;

			System.OperatingSystem osInfo = System.Environment.OSVersion;

			if (osInfo.Platform == PlatformID.Win32NT)
			{
				if (osInfo.Version.Major == 5)
				{
					// We're running Win2K or XP.
//					quit = ! ErrorIfNotAdminPrivileges();
				}
			}

			return ! quit;
		}
	}
}