using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Parsing
{
    public class Conversion
    {
	    static CultureInfo ukCulture;

		static Conversion ()
	    {
		    ukCulture = new CultureInfo ("en-GB", false);
		}

	    public static int? ParseInt (string s)
	    {
		    if (int.TryParse(s, NumberStyles.Integer, ukCulture, out var result))
		    {
			    return result;
		    }
		    else
		    {
			    return null;
		    }
	    }

	    public static int ParseInt (string s, int defaultValue)
	    {
		    return ParseInt(s) ?? defaultValue;
	    }

	    public static bool? ParseBool (string s)
	    {
		    if (bool.TryParse(s, out var result))
		    {
			    return result;
		    }
		    else
		    {
			    return null;
		    }
	    }

	    public static bool ParseBool (string s, bool defaultValue)
	    {
		    return ParseBool(s) ?? defaultValue;
	    }

	    public static DateTime? ParseDateTime (string s)
	    {
		    if (DateTime.TryParse(s, out var result))
		    {
			    return result;
		    }
		    else
		    {
			    return null;
		    }
	    }

	    public static DateTime ParseDateTime (string s, DateTime defaultValue)
	    {
		    return ParseDateTime(s) ?? defaultValue;
	    }

		public static string ToString (int i)
	    {
		    return i.ToString(ukCulture);
	    }

	    public static string ToString (bool b)
	    {
		    return b.ToString(ukCulture);
	    }

		public static string ToString (DateTime d)
		{
			return d.ToString(ukCulture);
		}
	}
}