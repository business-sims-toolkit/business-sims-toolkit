using System.Collections.Generic;
using System.IO;
using System.Drawing;
using System.Drawing.Text;

namespace LibCore
{
	public static class FontRepository
	{
		static Dictionary<string, List<Font>> nameToFontsAssigned;

		static PrivateFontCollection privateFontCollection;

		static FontRepository ()
		{
			nameToFontsAssigned = new Dictionary<string, List<Font>> ();
			privateFontCollection = new PrivateFontCollection ();
		}

		public static Font GetFont (string name, float size)
		{
			return GetFont(name, size, FontStyle.Regular);
		}

		public static Font GetFont (string name, float size, FontStyle style)
		{
			if (nameToFontsAssigned.ContainsKey(name))
			{
				foreach (Font compare in nameToFontsAssigned[name])
				{
					if ((compare.Size == size) && (compare.Style == style))
					{
						return compare;
					}
				}
			}
			else
			{
				nameToFontsAssigned.Add(name, new List<Font> ());
			}

			Font font = null;

			foreach (FontFamily family in privateFontCollection.Families)
			{
				if (family.Name == name)
				{
					font = ConstantSizeFont.NewFont(family, size, style);
					break;
				}
			}

			if (font == null)
			{
				font = ConstantSizeFont.NewFont(name, size, style);
			}

			nameToFontsAssigned[name].Add(font);

			return font;
		}

		public static void AddFonts (string folder)
		{
			foreach (string font in Directory.GetFiles(folder, "*.ttf"))
			{
				privateFontCollection.AddFontFile(font);
			}
		}
	}
}