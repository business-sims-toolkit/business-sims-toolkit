using System.Reflection;
using System.IO;

namespace LibCore
{
	public interface ILogger
	{
		void WriteLine(string line, params object [] args);
		void Write (string line, params object[] args);
	}

	public sealed class AppInfo : ILogger
	{
		string install_location;
		string data_location;

		ILogger logger;

		public static readonly AppInfo TheInstance = new AppInfo();

		string appName;

		AppInfo()
		{
			Assembly a = Assembly.GetExecutingAssembly();
			install_location = Path.GetDirectoryName(a.Location);
			logger = null;

			SetApplicationName(System.Windows.Forms.Application.ProductName);
		}

		public void SetApplicationName(string name)
		{
			appName = name;

#if DEBUG
			data_location = InstallLocation;
#else
			string folder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
			LibCore.GlobalAccess.EnsurePathExists(folder);

			data_location = folder + @"\" + name;
			LibCore.GlobalAccess.EnsurePathExists(data_location);
#endif
		}

		public string GetApplicationName ()
		{
			return appName;
		}

		public void SetLocationToAppPath ()
		{
			data_location = InstallLocation;
		}

		public string Location
		{
			get { return data_location; }
		}

		public string InstallLocation
		{
			get { return install_location; }
		}

		public void SetLogger(ILogger l)
		{
			logger = l;
		}

		#region ILogger Members

		public void WriteLine(string line, params object [] args)
		{
			if (null != logger)
			{
				logger.WriteLine(line, args);
			}
		}

		public void Write(string line, params object [] args)
		{
			if (null != logger)
			{
				logger.Write(line, args);
			}
		}

		#endregion
	}
}
