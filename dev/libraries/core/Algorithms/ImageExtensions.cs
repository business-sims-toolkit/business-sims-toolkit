using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Algorithms
{
	public static class BitmapExtensions
	{
		public static Bitmap ConvertColours (this Bitmap src, Dictionary<Color, Color> colourKeyMappings, bool maintainAlpha = true)
		{
			var converted = new Bitmap(src.Width, src.Height);

			for (var c = 0; c < src.Width; c++)
			{
				for (var r = 0; r < src.Height; r++)
				{
					var currentColour = src.GetPixel(c, r);

					if (currentColour.A == 0)
					{
						converted.SetPixel(c, r, Color.Transparent);
						continue;
					}
					
					if (colourKeyMappings.All(kvp => !kvp.Key.EqualsByComponentsWithThreshold(currentColour, 3)))
					{
						converted.SetPixel(c, r, currentColour);
						continue;
					}

					var colourMapping =
						colourKeyMappings.FirstOrDefault(kvp => kvp.Key.EqualsByComponentsWithThreshold(currentColour, 3));
					
					var replacementColour = colourMapping.Value;

					var colourToUse = Color.FromArgb(maintainAlpha ? currentColour.A : replacementColour.A,
						replacementColour);

					converted.SetPixel(c, r, colourToUse);
				}
			}



			return converted;
		}

		public static Bitmap ConvertColours(this Bitmap src, Color foreColourKey, Color replacementForeColour,
		                                    Color backColourKey, Color replacementBackColour,
		                                    bool maintainAlpha = true)
		{
			return ConvertColours(src, new Dictionary<Color, Color>
			{
				{ foreColourKey, replacementForeColour },
				{ backColourKey, replacementBackColour }
			}, maintainAlpha);
		}

		public static Bitmap AlphaBlend (this Bitmap src, Color replacmentColour)
		{
			var bitmap = new Bitmap(src);

			using (var graphics = Graphics.FromImage(bitmap))
			{
				
			}

			return bitmap;
		}
	}
}
