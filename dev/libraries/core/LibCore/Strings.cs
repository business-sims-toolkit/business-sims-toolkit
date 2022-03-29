using System;
using System.Collections.Generic;
using System.Text;

namespace LibCore
{
	public static class Strings
	{
		public static string SentenceCase (string a)
		{
			var builder = new StringBuilder ();
			var capitaliseNextLetter = true;

			foreach (var c in a)
			{
				if (capitaliseNextLetter)
				{
					builder.Append(char.ToUpper(c));
					capitaliseNextLetter = false;
				}
				else
				{
					builder.Append(c);
					if (! char.IsLetter(c))
					{
						capitaliseNextLetter = true;
					}
				}
			}

			return builder.ToString();
		}

		public static string RemoveHiddenText (string a)
		{
			var builder = new StringBuilder ();
			var braceDepth = 0;

			foreach (var c in a)
			{
				if (c == '{')
				{
					braceDepth++;
				}
				else if (c == '}')
				{
					braceDepth--;
				}
				else if (braceDepth == 0)
				{
					builder.Append(c);
				}
			}

			return builder.ToString();
		}

		public static int CompareVersions (string versionA, string versionB, bool compareMajorOnly = false)
		{
			var aParts = versionA.Split('.');
			var bParts = versionB.Split('.');
			
			for (var partIndex = 0; partIndex < Math.Max(aParts.Length, bParts.Length); partIndex++)
			{
				if (partIndex >= aParts.Length)
				{
					return -1;
				}
				else if (partIndex >= bParts.Length)
				{
					return 1;
				}
				else
				{
					var difference = CONVERT.ParseIntSafe(aParts[partIndex], 0).CompareTo(CONVERT.ParseIntSafe(bParts[partIndex], 0));

					if (compareMajorOnly)
					{
						return difference;
					}

					if (difference != 0)
					{
						return difference;
					}
				}
			}

			return 0;
		}

		public static string CollapseList (params string [] items)
		{
			return CollapseList(new List<string> (items));
		}

		public static string CollapseList (List<string> itemsOriginal)
		{
			var items = new List<string> (itemsOriginal);

			var builder = new StringBuilder ();

			while (items.Count > 0)
			{
				if (builder.Length > 0)
				{
					if (items.Count > 1)
					{
						builder.Append(", ");
					}
					else
					{
						builder.Append(" and ");
					}
				}

				builder.Append(items[0]);
				items.RemoveAt(0);
			}

			return builder.ToString();
		}
	}
}