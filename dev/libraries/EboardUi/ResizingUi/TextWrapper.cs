using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.VisualStyles;

namespace ResizingUi
{
	public static class TextWrapper
	{
		static string NormaliseNewlines (string text)
		{
			return text.Replace("\r\n", "\n").Replace("\n\r", "\n").Replace("\r", "\n");
		}

		public static string NormaliseLines (params string [] lines)
		{
			var builder = new StringBuilder ();

			foreach (var line in lines)
			{
				foreach (var subLine in SplitIncludingMultiples(NormaliseNewlines(line), c => (c == '\n')))
				{
					if (builder.Length > 0)
					{
						builder.Append("\n");
					}

					builder.Append(subLine);
				}
			}

			return builder.ToString();
		}

		static string BuildPartialLine (string [] words, int firstWord, int lastWord)
		{
			string line = "";
			for (int word = firstWord; word <= lastWord; word++)
			{
				if (word > firstWord)
				{
					line += " ";
				}

				line += words[word];
			}

			return line;
		}

		delegate bool CharacterPredicate (char c);

		/// <summary>
		/// Split the source string, including null-length strings in the output,
		/// so that concatenating the output array with a split character
		/// between each pair of strings, reproduces the input string exactly.
		/// In contrast, string.Split() effectively trims leading and trailing
		/// split characters, and collapses runs of them.
		/// </summary>
		static string [] SplitIncludingMultiples (string source, CharacterPredicate isSpacePredicate)
		{
			var parts = new List<string>();
			string part = "";

			for (int index = 0; index < source.Length; index++)
			{
				char character = source[index];

				if (isSpacePredicate(character))
				{
					parts.Add(part);
					part = "";
				}
				else
				{
					part += character;
				}
			}
			parts.Add(part);

			return parts.ToArray();
		}

		public static string [] WordWrapText (Graphics graphics, Font font, float layoutWidth, string input)
		{
			var outputLines = new List<string> ();
			var inputLines = SplitIncludingMultiples(NormaliseNewlines(input), c => (c == '\n'));

			foreach (var line in inputLines)
			{
				string [] words = SplitIncludingMultiples(line, c => Char.IsWhiteSpace(c));

				int lineStartWord = 0;
				do
				{
					if (words.Length > 0)
					{
						int lineEndWord = 0;

						// Find how many words we can render on this line (at least one, even if
						// that is too wide).
						int tryLineEndWord = lineStartWord;
						bool tryAnother;

						do
						{
							bool fits = (graphics.MeasureString(BuildPartialLine(words, lineStartWord, tryLineEndWord), font).Width <= layoutWidth);
							tryAnother = false;

							if (fits || (tryLineEndWord == lineStartWord))
							{
								lineEndWord = tryLineEndWord;

								if (lineEndWord < (words.Length - 1))
								{
									tryLineEndWord++;
									tryAnother = true;
								}
							}
						}
						while (tryAnother);

						outputLines.Add(BuildPartialLine(words, lineStartWord, lineEndWord));

						lineStartWord = lineEndWord + 1;
					}
				}
				while (lineStartWord < words.Length);
			}

			return outputLines.ToArray();
		}

		public static SizeF MeasureText (Graphics graphics, Font font, string [] lines)
		{
			float width = 0;
			float height = 0;

			for (int i = 0; i < lines.Length; i++)
			{
				var lineSize = graphics.MeasureString(lines[i], font);

				width = Math.Max(width, lineSize.Width);

				if (i == (lines.Length - 1))
				{
					height += lineSize.Height;
				}
				else
				{
					height += font.Height;
				}
			}

			return new SizeF (width, height);
		}

		public static void DrawText (Graphics graphics, Font font, Brush brush, string [] lines, RectangleF rectangle, StringAlignment horizontalAlignment, StringAlignment verticalAlignment)
		{
			var lineSizes = lines.Select(line => graphics.MeasureString(line, font)).ToList();
			lineSizes[lineSizes.Count - 1] = new SizeF (lineSizes[lineSizes.Count - 1].Width, font.Height);

			float totalHeight = lineSizes.Sum(s => s.Height);

			float y;
			switch (verticalAlignment)
			{
				case StringAlignment.Center:
					y = rectangle.Top + ((rectangle.Height - totalHeight) / 2);
					break;

				case StringAlignment.Far:
					y = rectangle.Bottom - totalHeight;
					break;

				case StringAlignment.Near:
				default:
					y = rectangle.Top;
					break;
			}

			for (int i = 0; i < lines.Length; i++)
			{
				float x;
				switch (horizontalAlignment)
				{
					case StringAlignment.Center:
						x = rectangle.Left + ((rectangle.Width - lineSizes[i].Width) / 2);
						break;

					case StringAlignment.Far:
						x = rectangle.Right - lineSizes[i].Width;
						break;

					case StringAlignment.Near:
					default:
						x = rectangle.Left;
						break;
				}

				graphics.DrawString(lines[i], font, brush, x, y);
				y += lineSizes[i].Height;
			}
		}
	}
}