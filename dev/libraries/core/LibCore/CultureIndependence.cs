using System;
using System.Reflection;
using System.Globalization;

namespace LibCore
{
	public static class CultureIndependence
	{
		static bool SetPrivateStaticFields (Type type, string [] names, object value)
		{
			bool success = false;

			foreach (string name in names)
			{
				MemberInfo [] members = type.GetMember(name, BindingFlags.NonPublic | BindingFlags.Static);
				if (members.Length > 0)
				{
					type.InvokeMember(name,
									  BindingFlags.SetField | BindingFlags.NonPublic | BindingFlags.Static,
									  null,
									  value,
									  new [] { value });

					success = true;
				}
			}

			return success;
		}

		static bool SetPublicStaticProperties (Type type, string [] names, object value)
		{
			bool success = false;

			foreach (string name in names)
			{
				MemberInfo [] members = type.GetMember(name, BindingFlags.Static);
				if (members.Length > 0)
				{
					type.InvokeMember(name,
									  BindingFlags.SetProperty | BindingFlags.Static,
									  null,
									  value,
									  new [] { value });

					success = true;
				}
			}

			return success;
		}

		// See http://blog.intninety.co.uk/2012/09/setting-default-currentculture-in-all-versions-of-net/
		public static void SetDefaultCulture (CultureInfo culture)
		{
			Type type = typeof (CultureInfo);

			// First try the legal .NET 4.5+ approach...
			if (SetPublicStaticProperties(type, new [] { "DefaultThreadCurrentCulture" }, culture)
			// ...failing that, the hacky 3.0+ one...
				|| SetPrivateStaticFields(type, new [] { "s_userDefaultCulture", "s_userDefaultUICulture" }, culture)
			// ...or the hacky 2.0+ one!
				|| SetPrivateStaticFields(type, new [] { "m_userDefaultCulture", "m_userDefaultUICulture" }, culture))
			{
			}
			else
			{
                // We can't do this in MacOS so don't bother!
                if (LibCore.Environment.Platform != PlatformID.MacOSX)
                {
                    throw new ApplicationException("Can't find a way to set global culture!");
                }
			}
		}
	}
}