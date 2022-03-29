using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace LibCore
{
	public class ConstantSizeFont
	{
		[DllImport("user32.dll")]
		static extern IntPtr GetDC (IntPtr hwnd);

		[DllImport("user32.dll")]
		static extern Int32 ReleaseDC (IntPtr hwnd, IntPtr hdc);

		static List<Font> allocatedFonts;

		static ConstantSizeFont ()
		{
			allocatedFonts = new List<Font> ();
		}

		public static Font NewFont (string familyName, float size)
		{
			return NewFont(familyName, size, FontStyle.Regular);
		}

		public static Font NewFont (FontFamily family, float size)
		{
			return NewFont(family, size, FontStyle.Regular);
		}

		public static Font NewFont (string familyName, float size, FontStyle style)
		{
			Font font;
			IntPtr dc = GetDC(IntPtr.Zero);
			using (Graphics graphics = Graphics.FromHdc(dc))
			{
				font = NewFont(graphics, familyName, size, style);
			}
			ReleaseDC(IntPtr.Zero, dc);

			return font;
		}

		public static Font NewFontPixelSized (string familyName, float size, FontStyle style)
		{
			return new Font (familyName, Math.Max(1, size), style, GraphicsUnit.Pixel);
		}

		public static Font NewFont (Font baseFont, float size, FontStyle style)
		{
			Font font;
			IntPtr dc = GetDC(IntPtr.Zero);
			using (Graphics graphics = Graphics.FromHdc(dc))
			{
				font = NewFont(graphics, baseFont.FontFamily, size, style);
			}
			ReleaseDC(IntPtr.Zero, dc);

			return font;
		}

		public static Font NewFont (FontFamily family, float size, FontStyle style)
		{
			Font font;
			IntPtr dc = GetDC(IntPtr.Zero);
			using (Graphics graphics = Graphics.FromHdc(dc))
			{
				font = NewFont(graphics, family, size, style);
			}
			ReleaseDC(IntPtr.Zero, dc);

			return font;
		}

		public static Font NewFont (Control control, string familyName, float size, FontStyle style)
		{
			using (Graphics graphics = control.CreateGraphics())
			{
				return NewFont(graphics, familyName, size, style);
			}
		}
		 
		public static Font NewFont (Graphics graphics, string familyName, float size, FontStyle style)
		{
			// Correct the size, given that we designed at 96dpi.
			size /= graphics.DpiX / 96.0f; // Windows assumes square pixels anyway!

			return new Font (familyName, size, style);
		}

		public static Font NewFont (Graphics graphics, FontFamily family, float size, FontStyle style)
		{
			// Correct the size, given that we designed at 96dpi.
			size /= graphics.DpiX / 96.0f; // Windows assumes square pixels anyway!

			return new Font (family, size, style);
		}

		public static Font GetFont (string name, float size, FontStyle style)
		{
			foreach (Font font in allocatedFonts)
			{
				if ((font.Name == name) && (font.SizeInPoints == size) && (font.Style == style))
				{
					return font;
				}
			}

			Font newFont = NewFont(name, size, style);
			allocatedFonts.Add(newFont);

			return newFont;
		}

		public static Font GetFont (string name, float size)
		{
			return GetFont(name, size, FontStyle.Regular);
		}
	}
}