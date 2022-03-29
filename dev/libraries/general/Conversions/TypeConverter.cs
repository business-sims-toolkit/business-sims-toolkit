using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace Conversions
{
	public static class TypeConverter
	{
		public static T ConvertFromString<T>(string value)
		{
			try
			{
				return (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
			}
			catch
			{
				return default(T);
			}
		}

		public static T ConvertFromObject<T>(object obj)
		{
			try
			{
				return (T)Convert.ChangeType(obj, typeof(T), CultureInfo.InvariantCulture);
			}
			catch
			{
				return default(T);
			}
		}

	}
}
