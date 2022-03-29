using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Algorithms
{
	public static class StringExtensions
	{
		public static string [] SplitOnWhitespace (this string source, bool allowQuotes = false)
		{
			List<string> strings = new List<string> ();

			StringBuilder stringBuilder = null;
			bool inQuotes = false;

			foreach (char c in source)
			{
				if ((! inQuotes)
					&& Char.IsWhiteSpace(c))
				{
					if (stringBuilder != null)
					{
						strings.Add(stringBuilder.ToString());
						stringBuilder = null;
					}
				}
				else if (allowQuotes
					&& (c == '"'))
				{
					inQuotes = ! inQuotes;
				}
				else
				{
					if (stringBuilder == null)
					{
						stringBuilder = new StringBuilder ();
					}

					stringBuilder.Append(c);
				}
			}

			if ((stringBuilder != null)
				&& (stringBuilder.Length > 0))
			{
				strings.Add(stringBuilder.ToString());
			}

			return strings.ToArray();
		}

        /// <summary>
        /// Takes a string assumed to be in UpperCamelCase or lowerCamelCase
        /// and converts it to snake_case. Useful for converting the name of
        /// a property to snake case for skin file look ups.
        /// </summary>
	    public static string ToSnakeCase(this string source)
	    {
	        var regex = new Regex(@"
                (?<=[A-Z])(?=[A-Z][a-z]) |
                 (?<=[^A-Z])(?=[A-Z]) |
                 (?<=[A-Za-z])(?=[^A-Za-z])", RegexOptions.IgnorePatternWhitespace);

	        return regex.Replace(source, "_").ToLower();
	    }

        /// <summary>
        /// Checks if the supplied string is a valid hex code
        /// i.e. it starts with # and it made up of either 3/4/6/8
        /// characters in the range of 0-9, a-f, or A-F (hexadecimal digits)
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
	    public static bool IsValidHexCode (this string source)
	    {
            var regex = new Regex(@"^#([0-9A-Fa-f]{3}|[0-9A-Fa-f]{4}|[0-9A-Fa-f]{6}|[0-9A-Fa-f]{8})$");
            
	        return regex.IsMatch(source);
	    }
    }
}