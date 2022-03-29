using Microsoft.Win32;


namespace CoreUtils
{
	/// <summary>
	/// Summary description for Registry.
	/// </summary>
	public class TheRegistry
	{

		/// <summary>
		/// Used to get the list of string values (the names) of a particular key
		/// </summary>
		/// <param name="KeyName"></param>
		/// <param name="kns"></param>
		/// <returns></returns>
		public static bool GetRegistryLocalMachineSubKeyNames(string KeyName, out string[] kns)
		{
			bool exists = false;
			kns = null;
			try
			{
				RegistryKey regKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\"+KeyName);
				if (regKey != null)
				{
					exists = true;
					kns = regKey.GetValueNames();
				}
				else
				{
					kns = null;
				}
			}
			catch
			{
				kns = null;
			}
			return exists;
		}

		/// <summary>
		/// Used to extract out a particular value of name value item in a hive key
		/// </summary>
		/// <param name="KeyName"></param>
		/// <param name="valuename"></param>
		/// <param name="keyvalue"></param>
		/// 

		/// <summary>
		/// Used to determine whether a particault name value exists 
		/// </summary>
		/// <param name="KeyName"></param>
		public static bool DoesRegistryCurrentUser_KeyExist(string KeyName)
		{
			bool exists = false;
			RegistryKey regKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\"+KeyName);
			if (regKey != null)
			{
				exists = true;
			}
			return exists;
		}

		/// <summary>
		/// Used to extract out a particular value of name value item in a hive key
		/// </summary>
		/// <param name="KeyName"></param>
		/// <param name="valuename"></param>
		/// <param name="keyvalue"></param>
		public static void GetRegistryCurrentUser_KeyValue(string KeyName, string valuename, out string keyvalue)
		{
			keyvalue="";
			RegistryKey regKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\"+KeyName);
			if (regKey != null)
			{
				keyvalue = (string) regKey.GetValue(valuename);
			}
		}
		
		/// <summary>
		/// Used to set a particular value of name value item in a hive key
		/// </summary>
		/// <param name="KeyName"></param>
		/// <param name="ValueName"></param>
		/// <param name="ValueData"></param>
		public static void SetRegistryCurrentUser_KeyValue(string KeyName, string ValueName, string ValueData)
		{
			RegistryKey regKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\"+KeyName,true);
			if (regKey != null)
			{
				regKey.SetValue(ValueName,ValueData);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public static string GetStringValue(string key)
		{
			RegistryKey rk;
            
			rk = Registry.ClassesRoot.OpenSubKey(key, false);
            
			if( rk != null )
			{
				string val = (string) rk.GetValue("");
				rk.Close();
				return val;
			}

			return "";
		}


		internal static void GetRegistryLocalMachine_KeyValue (string KeyName, string valuename, out string keyvalue)
		{
			keyvalue = "";

			RegistryKey regKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\" + KeyName);
			if (regKey != null)
			{
				keyvalue = (string) regKey.GetValue(valuename);
			}
		}
	}
}
