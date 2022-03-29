//using CommonGUI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using ResizingUi.Button;

namespace ResizingUi
{
	public static class FontScalerExtensions
    {
	    public static float GetFontSizeInPixelsToFit (this Control control, string fontName, IList<FontStyle> fontStyles, IList<string> strings, IList<SizeF> bounds)
	    {
		    float? fontSize = null;

			using (var graphics = control.CreateGraphics())
			{
				for (int i = 0; i < strings.Count; i++)
				{
					using (var font = new Font (fontName, 100, fontStyles[i], GraphicsUnit.Pixel))
					{
						var stringSize = graphics.MeasureString(strings[i], font);
						var fontSizeToFit = Math.Min(bounds[i].Width * font.Size / (float) Math.Ceiling(stringSize.Width), bounds[i].Height * font.Size / (float) Math.Ceiling(stringSize.Height));

						fontSize = (fontSize == null) ? fontSizeToFit : Math.Min(fontSize.Value, fontSizeToFit);
					}
				}
			}

		    return Math.Max(0.01f, fontSize ?? 0);
	    }

		public static float GetFontSizeInPixelsToFit (this Control control, string fontName, FontStyle fontStyle, IList<string> strings, IList<SizeF> bounds)
	    {
		    float? fontSize = null;

		    using (var graphics = control.CreateGraphics())
		    using (var font = new Font (fontName, 100, fontStyle, GraphicsUnit.Pixel))
		    {
			    for (var i = 0; i < strings.Count; i++)
			    {
			        if (string.IsNullOrEmpty(strings[i]))
			        {
			            continue;
			        }

				    var stringSize = graphics.MeasureString(strings[i], font);
				    var fontSizeToFit = Math.Min(bounds[i].Width * font.Size / (float) Math.Ceiling(stringSize.Width), bounds[i].Height * font.Size / (float) Math.Ceiling(stringSize.Height));

				    fontSize = (fontSize == null) ? fontSizeToFit : Math.Min(fontSize.Value, fontSizeToFit);
			    }
		    }

		    return Math.Max(0.01f, fontSize ?? 0);
		}

		public static float GetFontSizeInPixelsToFit (this Control control, FontStyle fontStyle, IList<string> strings, IList<SizeF> bounds)
	    {
		    return GetFontSizeInPixelsToFit(control, CoreUtils.SkinningDefs.TheInstance.GetFontName(), fontStyle, strings, bounds);
	    }

		public static float GetFontSizeInPixelsToFit (this Control control, string fontName, FontStyle fontStyle, string text, SizeF bounds)
	    {
		    return GetFontSizeInPixelsToFit(control, fontName, fontStyle, new [] { text }, new [] { bounds });
		}

	    public static float GetFontSizeInPixelsToFit (this Control control, FontStyle fontStyle, string text, SizeF bounds)
	    {
		    return GetFontSizeInPixelsToFit(control, CoreUtils.SkinningDefs.TheInstance.GetFontName(), fontStyle, new [] { text }, new [] { bounds });
	    }

	    public static float GetFontSizeInPixelsToFit (this Control control, FontStyle fontStyle, IList<string> strings, SizeF bounds)
	    {
			return GetFontSizeInPixelsToFit(control, CoreUtils.SkinningDefs.TheInstance.GetFontName(), fontStyle, strings, Enumerable.Repeat(bounds, strings.Count).ToList());
	    }

		public static Font GetFontToFit (this Control control, FontStyle fontStyle, string text, SizeF bounds)
	    {
		    return CoreUtils.SkinningDefs.TheInstance.GetPixelSizedFont(GetFontSizeInPixelsToFit(control, CoreUtils.SkinningDefs.TheInstance.GetFontName(), fontStyle, text, bounds), fontStyle);
	    }

	    public static Font GetFontToFit (this Control control, FontStyle fontStyle, IList<string> strings, SizeF bounds)
	    {
		    return CoreUtils.SkinningDefs.TheInstance.GetPixelSizedFont(GetFontSizeInPixelsToFit(control, fontStyle, strings, bounds), fontStyle);
		}

	    public static Font GetFontToFit (this Control control, FontStyle fontStyle, IList<string> strings, IList<SizeF> bounds)
	    {
		    return CoreUtils.SkinningDefs.TheInstance.GetPixelSizedFont(GetFontSizeInPixelsToFit(control, fontStyle, strings, bounds), fontStyle);
	    }

		public static void SetFontToFit (this Control control, FontStyle fontStyle)
	    {
		    float borderWidth = 0;
		    string text = null;

		    var styledButton = control as StyledDynamicButton;
		    var panel = control as Panel;
		    if (styledButton != null)
		    {
			    borderWidth = styledButton.BorderWidth + 2;
			    text = styledButton.ButtonText;
		    }
		    else if (panel != null)
		    {
			    if (panel.BorderStyle != BorderStyle.None)
			    {
				    borderWidth = 8;
			    }

			    text = panel.Text;
		    }
		    else
		    {
			    text = control.Text;
		    }

			control.Font = control.GetFontToFit(fontStyle, text, new SizeF (control.Width - (2 * borderWidth), control.Height - (2 * borderWidth)));
	    }
    }
}