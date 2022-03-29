using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Text;
using Algorithms;

namespace LibCore
{
	/// <summary>
	/// Summary description for Convert.
	/// </summary>
	public sealed class CONVERT
	{
		CultureInfo ukCulture;
		static readonly CONVERT t = new CONVERT();

		public enum HmsOptions
		{
			Canonical, // eg 27:00:00 becomes 03:00:00
			Canonical12Hour, // eg 27:00:00 becomes 03:00:00 am
			Freeform, // eg 27:00:00 remains as 27:00:00
		}

		CONVERT()
		{
			ukCulture = new CultureInfo("en-GB",false);

            CultureIndependence.SetDefaultCulture(ukCulture);
		}

		/// <summary>
		/// Locale Safe ParseInt
		/// </summary>
		/// <param name="s"></param>
		/// <returns></returns>
		public static int ParseInt(string s)
		{
			return int.Parse(s,t.ukCulture.NumberFormat);
		}

		/// <summary>
		/// ParseInt but return a null if invalid.
		/// </summary>
		public static int? ParseIntSafe (string s)
		{
			if (int.TryParse(s,
							 NumberStyles.AllowLeadingSign | NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite | NumberStyles.Integer,
							 t.ukCulture, out var result))
			{
				return result;
			}

			return null;
		}

		/// <summary>
		/// ParseInt but return a default value if invalid.
		/// </summary>
		public static int ParseIntSafe (string s, int def)
		{
			return int.TryParse(s,
				NumberStyles.AllowLeadingSign | NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite | NumberStyles.Integer,
				t.ukCulture, out var result) ? result : def;
		}

		/// <summary>
		/// Locale Safe ParseLong
		/// </summary>
		/// <param name="s"></param>
		/// <returns></returns>
		public static long ParseLong(string s)
		{
			return long.Parse(s, t.ukCulture.NumberFormat);
		}

		/// <summary>
		/// ParseLong but return a default value if invalid.
		/// Best solution would be long.TryParse() but that's
		/// not available pre-.NET 2.  Alternatively we could
		/// just call long.Parse() and swallow the exception,
		/// but that's slow and will upset the debugger.
		/// Instead, do it manually (yuck).
		/// </summary>
		public static long ParseLongSafe(string s, int def)
		{
			long result = 0;

			if (s == null)
			{
				return def;
			}

			int i = 0;
			while ((i < s.Length) && Char.IsWhiteSpace(s[i]))
			{
				i++;
			}

			if (i >= s.Length)
			{
				return def;
			}

			// Get any sign character.
			int sign = 1;
			if (s[i] == '+')
			{
				i++;
			}
			else if (s[i] == '-')
			{
				i++;
				sign = -1;
			}

			if (i >= s.Length)
			{
				return def;
			}

			// Parse the digits.
			while ((i < s.Length) && Char.IsDigit(s[i]))
			{
				long n = (long)(s[i] - '0');
				result = (result * 10) + n;
				i++;
			}

			// Skip trailing white space.
			while ((i < s.Length) && Char.IsWhiteSpace(s[i]))
			{
				i++;
			}

			// If there's trailing garbage, complain.
			if (i < s.Length)
			{
				return def;
			}

			return result * sign;
		}

		public static double? ParseDoubleSafe (string stringVal)
		{
			double value;
			if (double.TryParse(stringVal, out value))
			{
				return value;
			}

			return null;
		}

		/// <summary>
		/// ParseDouble but return a default value if invalid.
		/// </summary>
		public static double ParseDoubleSafe (string stringVal, double defaultVal)
		{
			try
			{
				if (stringVal == "")
				{
					return defaultVal;
				}
				return ParseDouble(stringVal);
			}
			catch (ArgumentNullException)
			{
				return defaultVal;
			}
			catch (FormatException)
			{
				return defaultVal;
			}
			catch (OverflowException)
			{
				return defaultVal;
			}
		}

        public static float ParseFloatSafe (string stringVal, float defaultVal)
        {
            try
            {
                if (stringVal == "")
                {
                    return defaultVal;
                }
                return ParseFloat(stringVal);
            }
            catch (ArgumentNullException)
            {
                return defaultVal;
            }
            catch (FormatException)
            {
                return defaultVal;
            }
            catch (OverflowException)
            {
                return defaultVal;
            }
        }
		/// <summary>
		/// Localse safe ToString
		/// </summary>
		/// <param name="i"></param>
		/// <returns></returns>
		public static string ToStr(int i)
		{
			return i.ToString(t.ukCulture.NumberFormat);
		}

		public static string ToStr (long i)
		{
			return ToStr((double) i);
		}

		public static string ToStr (Point point)
		{
			return Format("{0},{1}", point.X, point.Y);
		}

		public static string ToStr (PointF point)
		{
			return Format("{0},{1}", point.X, point.Y);
		}

        public static string ToStr (Size size)
        {
            return Format("{0},{1}", size.Width, size.Height);
        }

        public static string ToStr(SizeF size)
        {
            return Format("{0},{1}", size.Width, size.Height);
        }
		/// <summary>
		/// Locale safe ToString()
		/// </summary>
		/// <param name="i"></param>
		/// <returns></returns>
		public static string ToStr(bool i)
		{
			return i.ToString(t.ukCulture.NumberFormat);
		}

		/// <summary>
		/// Locale safe bool.Parse
		/// </summary>
		/// <param name="s"></param>
		/// <returns></returns>
		public static bool? ParseBool(string s)
		{
			if (! string.IsNullOrEmpty(s))
			{
				return bool.Parse(s);
			}

			return null;
		}

		/// <summary>
		/// Locale-safe bool.Parse taking a default value to be returned if s is empty.
		/// </summary>
		public static bool ParseBool (string s, bool defaultValue)
		{
			return ParseBool(s) ?? defaultValue;
		}

		/// <summary>
		/// Locale safe ulong.Parse
		/// </summary>
		/// <param name="s"></param>
		/// <returns></returns>
		public static ulong ParseULong(string s)
		{
			return ulong.Parse(s,t.ukCulture.NumberFormat);
		}
		
		/// <summary>
		/// Locale safe ulong.ToString()
		/// </summary>
		/// <param name="i"></param>
		/// <returns></returns>
		public static string ToStr(ulong i)
		{
			return i.ToString(t.ukCulture.NumberFormat);
		}

		/// <summary>
		/// Locale safe ToString()
		/// </summary>
		/// <param name="i"></param>
		/// <returns></returns>
		public static string ToStr(DateTime i, string format)
		{
			return i.ToString(format, t.ukCulture);
		}

		/// <summary>
		/// Locale Safe time.ToString
		/// </summary>
		/// <param name="time"></param>
		/// <returns></returns>
		public static string ToStr(DateTime time)
		{
			return Format("{0}-{1:00}-{2:00} {3:00}:{4:00}:{5:00}",
						  time.Year, time.Month, time.Day,
						  time.Hour, time.Minute, time.Second);
		}

		/// <summary>
		/// DateTime.ToString() for only the date part.
		/// </summary>
		public static string ToDateStr (DateTime time)
		{
			return Format("{0}-{1:00}-{2:00}",
						  time.Year, time.Month, time.Day);
		}

		public static int ParseHmsToSeconds (string s)
		{
			s = s.Trim();
			string [] parts = s.Split(':');

			int time = (ParseInt(parts[0]) * 3600)
					   + (ParseInt(parts[1]) * 60);

			if (parts.Length > 2)
			{
				time += ParseInt(parts[2]);
			}

			return time;
		}

		public static string ToHmsFromSeconds (int seconds, HmsOptions options = HmsOptions.Freeform)
		{
			int hours = seconds / 3600;
			seconds -= (hours * 3600);

			string suffix = "";

			if ((options == HmsOptions.Canonical)
				|| (options == HmsOptions.Canonical12Hour))
			{
				while (hours < 0)
				{
					hours += 24;
				}

				hours = hours % 24;
			}

			if (options == HmsOptions.Canonical12Hour)
			{
				if (hours >= 12)
				{
					hours -= 12;
					suffix = " pm";
				}
				else
				{
					suffix = " am";
				}

				if (hours == 0)
				{
					hours = 12;
				}
			}

			int minutes = seconds / 60;
			seconds -= (minutes * 60);

			return Format("{0:00}:{1:00}:{2:00}{3}", hours, minutes, seconds, suffix);
		}

		public static string ToHmFromSeconds (int seconds, HmsOptions options = HmsOptions.Freeform, bool padMinutes = false)
		{
			int hours = seconds / 3600;
			seconds -= (hours * 3600);

			string suffix = "";

			if ((options == HmsOptions.Canonical)
				|| (options == HmsOptions.Canonical12Hour))
			{
				while (hours < 0)
				{
					hours += 24;
				}

				hours = hours % 24;
			}

			if (options == HmsOptions.Canonical12Hour)
			{
				if (hours >= 12)
				{
					hours -= 12;
					suffix = " pm";
				}
				else
				{
					suffix = " am";
				}

				if (hours == 0)
				{
					hours = 12;
				}
			}

			int minutes = seconds / 60;
			seconds -= (minutes * 60);

			if (padMinutes)
			{
				return Format("{0:00}:{1:00}{2}", hours, minutes, suffix);
			}
			else
			{
				return Format("{0}:{1:00}{2}", hours, minutes, suffix);
			}
		}

		public static DateTime ParseDate (string s)
		{
			string [] dateStrs = s.Split('-');

			if (dateStrs.Length == 1)
			{
				return DateTime.ParseExact(s, "yyyyMMdd", t.ukCulture.DateTimeFormat);
			}
			else
			{
				int year = ParseInt(dateStrs[0]);
				int month = ParseInt(dateStrs[1]);
				int day = ParseInt(dateStrs[2]);

				return new DateTime(year, month, day);
			}
		}

		/// <summary>
		/// Locale safe DateTime.Parse
		/// </summary>
		/// <param name="s"></param>
		/// <returns></returns>
		public static DateTime ParseDateTime(string s)
		{
			//return DateTime.Parse(s,t.ukCulture.NumberFormat);
			char[] space = { ' ' };
			string[] dateTimeStrs = s.Trim().Split(space);
			char[] dash = { '-' };
			string[] dateStrs = dateTimeStrs[0].Split(dash);
			char[] colon = { ':' };
			string[] timeStrs = dateTimeStrs[1].Split(colon);
			//
			int year = ParseInt(dateStrs[0]);
			int month = ParseInt(dateStrs[1]);
			int day = ParseInt(dateStrs[2]);
			//
			int hour = ParseInt(timeStrs[0]);
			int minute = ParseInt(timeStrs[1]);
			int second = ParseInt(timeStrs[2]);
			//
			DateTime dt = new DateTime(year,month,day,hour,minute,second);
			return dt;
		}
		/// <summary>
		/// Locale safe double.Parse
		/// </summary>
		/// <param name="s"></param>
		/// <returns></returns>
		public static double ParseDouble(string s)
		{
			return double.Parse(s.Replace(",", "").Replace(" ", "").Replace("$", ""),t.ukCulture.NumberFormat);
		}

        public static float ParseFloat(string s)
        {
            return float.Parse(s.Replace(",", "").Replace(" ", "").Replace("$", ""), t.ukCulture.NumberFormat);
        }

		public static Color ParseColor (string s, Color defaultColour)
		{
			if (s.Contains(","))
			{
				return ParseComponentColor(s);
			}
			else if (s.StartsWith("#"))
			{
				return ParseHtmlColor(s);
			}
			else
			{
				return defaultColour;
			}
		}

		/// <summary>
		/// Locale-safe parsing of r,g,b -> Color.
		/// </summary>
		public static Color ParseComponentColor (string s)
		{
			string [] components = s.Replace(" ", "").Split(',');

			if(components.Length == 3)
			{
				return Color.FromArgb(ParseInt(components[0]),
					ParseInt(components[1]),
					ParseInt(components[2]));
			}
			else if(components.Length == 4)
			{
				return Color.FromArgb(
					ParseInt(components[0]),
					ParseInt(components[1]),
					ParseInt(components[2]),
					ParseInt(components[3]));
			}

			return Color.White;
		}

		/// <summary>
		/// Locale-safe #rrggbb string -> Color
		/// </summary>
		public static Color ParseHtmlColor(string hexCode)
		{
		    if (!hexCode.IsValidHexCode())
		    {
                throw new ArgumentException($"{hexCode} is not a valid hex colour code");
		    }

		    hexCode = hexCode.Substring(1);

		    var increment = hexCode.Length <= 4 ? 1 : 2;

		    var components = new List<byte>();

		    for (var index = 0; index < hexCode.Length; index += increment)
		    {
                components.Add(byte.Parse(hexCode.Substring(index, increment), NumberStyles.HexNumber));
		    }
            
		    switch (components.Count)
		    {
		        case 3:
		            return Color.FromArgb(components[0], components[1], components[2]);
		        case 4:
		            return Color.FromArgb(components[0], components[1], components[2], components[3]);
                default:
                    return Color.White;
		    }
		}

		public static Size ParseSize (string s)
		{
			string [] components = s.Replace(" ", "").Split(',');

			return new Size (ParseInt(components[0]), ParseInt(components[1]));
		}

		public static SizeF ParseSizeF (string s)
		{
			string [] components = s.Replace(" ", "").Split(',');

			return new SizeF ((float) ParseDouble(components[0]), (float) ParseDouble(components[1]));
		}

		public static Point ParsePoint (string s)
		{
			string [] components = s.Replace(" ", "").Split(',');

			return new Point (ParseInt(components[0]), ParseInt(components[1]));
		}

		public static PointF ParsePointF (string s)
		{
			string [] components = s.Replace(" ", "").Split(',');

			return new PointF ((float) ParseDouble(components[0]), (float) ParseDouble(components[1]));
		}

		public static decimal ParseDecimal (string s)
		{
			return decimal.Parse(s, t.ukCulture.NumberFormat);
		}

		/// <summary>
		/// Locale safe double.ToString
		/// </summary>
		/// <param name="d"></param>
		/// <returns></returns>
		public static string ToStr(double d)
		{
			return d.ToString(t.ukCulture.NumberFormat);
		}

		public static string ToStr (float d)
		{
			return d.ToString(t.ukCulture.NumberFormat);
		}

		/// <summary>
		/// Locale safe toString padded
		/// </summary>
		/// <param name="d"></param>
		/// <param name="numPlaces"></param>
		/// <returns></returns>
		public static string ToPaddedStr(double d, int numPlaces)
		{
			string format = "0." + "".PadRight(numPlaces,'0');
			return d.ToString(format,t.ukCulture);
		}

		/// <summary>
		/// Locale safe toString padded
		/// </summary>
		/// <param name="d"></param>
		/// <param name="numPlaces"></param>
		/// <returns></returns>
		public static string ToPaddedStrWithThousands(double d, int numPlaces)
		{
			string format = "#,##0." + "".PadRight(numPlaces, '0');
			return d.ToString(format, t.ukCulture);
		}

		public static string ToPaddedCurrencyStrWithThousands (double d, int numPlaces, string currencySymbol = "$")
	    {
            
	        return $"{(d < 0 ? "-" : "")}{currencySymbol}{ToPaddedStrWithThousands(Math.Abs(d), numPlaces)}";
	    }

		public static string ToPaddedPercentageString (double d, int numPlaces, bool includePositiveSign = false)
		{
			var paddedStr = ToPaddedStr(d, numPlaces);

			return $"{(includePositiveSign && d >= 0 ? "+" : "")}{paddedStr}%";
		}

		/// <summary>
		/// Bouble rounded to numPlaces
		/// </summary>
		/// <param name="d"></param>
		/// <param name="numPlaces"></param>
		/// <returns></returns>
		public static string ToStr(double d, int numPlaces)
		{
			string ret = d.ToString(t.ukCulture);
			int point = ret.IndexOf(".");
			if(point != -1)
			{
				int end = point + numPlaces + 1;
				if(end >= ret.Length-1)
				{
					return ret;
				}

				return ret.Substring(0,end);
			}

			return ret;
		}

		/// <summary>
		/// Double correctly rounded to numPlaces, as plain ToStr() can return too many
		/// or too few places depending on God knows what.
		/// </summary>
		public static string ToStrRounded (double d, int numPlaces)
		{
			return d.ToString("F" + ToStr(numPlaces), t.ukCulture);
		}

		/// <summary>
		/// Locale-safe Color -> r,g,b string
		/// </summary>
		public static string ToComponentStr (Color c)
		{
			string alpha = "";
			if (c.A < 255)
			{
				alpha = ToStr(c.A) + ",";
			}
			return alpha + ToStr(c.R) + "," + ToStr(c.G) + "," + ToStr(c.B);
		}

		/// <summary>
		/// Locale-safe Color -> #rrggbb string
		/// </summary>
		public static string ToHtmlStr (Color c)
		{
			return "#" + c.R.ToString("x2") + c.G.ToString("x2") + c.B.ToString("x2");
		}

		public static string ToPointStr (Point point)
		{
			return ToStr(point.X) + "," + ToStr(point.Y);
		}

		public static string ToTimeStr (double seconds)
		{
			int minutes = (int) (seconds / 60);
			seconds -= (minutes * 60);

			return Format("{0:00}:{1:00}", minutes, seconds);
		}

		/// <summary>
		/// Like String.Format() but locale-safe.
		/// </summary>
		public static string Format (string format, params object [] args)
		{
			return String.Format(t.ukCulture.NumberFormat, format, args);
		}

		public static string FormatMoney (double amount, string currencySymbol)
		{
			StringBuilder builder = new StringBuilder ();
			if (amount < 0)
			{
				builder.Append("-");
				amount = -amount;
			}

			builder.Append(currencySymbol);
			builder.Append(ToPaddedStrWithThousands(amount, 0));

			return builder.ToString();
		}

		public static string FormatMoney (double amount)
		{
			return FormatMoney(amount, "$");
		}

        public enum MoneyFormatting
        {
            Units,
            Thousands,
            Millions
        }

        public static string FormatMoney (double amount, int numPlaces, MoneyFormatting format, string currencySymbol = "$", bool includeSuffix = true)
        {
            StringBuilder builder = new StringBuilder();

            if (amount < 0)
            {
                builder.Append("-");
                amount = -amount;
            }

            builder.Append(currencySymbol);

            double divisor = 1;
            string suffix = "";
            
            switch(format)
            {
                case MoneyFormatting.Millions:
                    divisor = 1000000.0;
                    suffix = "M";
                    break;
                case MoneyFormatting.Thousands:
                    divisor = 1000.0;
                    suffix = "K";
                    break;
                case MoneyFormatting.Units:
                    divisor = 1.0;
                    suffix = "";
                    break;
            }
            
            double scaledAmount = amount / divisor;

            if (scaledAmount < (1.0 / Math.Pow(10, numPlaces)))
            {
                switch(format)
                {
                    case MoneyFormatting.Millions:
                        suffix = "K";
                        break;
                    case MoneyFormatting.Thousands:
                        suffix = "";
                        break;
                }
                
                scaledAmount *= Math.Pow(10, numPlaces +1);
                numPlaces = 0;
            }

            builder.Append(scaledAmount >= 1000
                ? ToPaddedStrWithThousands(scaledAmount, numPlaces)
                : ToPaddedStr(scaledAmount, numPlaces));
            
            if (includeSuffix)
            {
                builder.Append(suffix);
            }
            

            return builder.ToString();
        }
       
		public static string FormatTimeHms (double amount)
		{
			int hours = ((int) amount) / (60 * 60);
			amount -= (hours * 60 * 60);
			int minutes = ((int) amount) / 60;
			amount -= (minutes * 60);
			int seconds = (int) amount;
			return Format("{0:00}:{1:00}:{2:00}", hours, minutes, seconds);
		}

		public static string FormatTimeHm (double amount)
		{
			int hours = ((int) amount) / (60 * 60);
			amount -= (hours * 60 * 60);
			int minutes = ((int) amount) / 60;
			amount -= (minutes * 60);
			return Format("{0:00}:{1:00}", hours, minutes);
		}

		public static string FormatTime (double amount)
		{
			int minutes = ((int) amount) / 60;
			int seconds = ((int) amount) - (60 * minutes);

			return Format("{0}:{1:00}", minutes, seconds);
		}

		public static string FormatTimeFourDigits (double amount)
		{
			int minutes = ((int) amount) / 60;
			int seconds = ((int) amount) - (60 * minutes);

			return Format("{0:00}:{1:00}", minutes, seconds);
		}

		public static string ToLower (string s)
		{
			StringBuilder builder = new StringBuilder ();

			foreach (char c in s)
			{
				builder.Append(char.ToLower(c));
			}

			return builder.ToString();
		}

		public static string ToStr (decimal v)
		{
			return v.ToString(t.ukCulture.NumberFormat);
		}

		public static bool? ParseBoolSafe (string s)
		{
			if (bool.TryParse(s, out bool value))
			{
				return value;
			}

			return null;
		}

		public static string ToStrWithCommas (int i)
		{
			return i.ToString("N0", t.ukCulture);
		}
	}
}