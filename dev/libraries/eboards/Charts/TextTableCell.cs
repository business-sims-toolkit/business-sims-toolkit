using System.Drawing;
using System.Drawing.Drawing2D;
using LibCore;

using CoreUtils;

namespace Charts
{
	public class TextTableCell : TableCell
	{
		float textScaleFactor = 1;
		float fontSize;

		public float TextScaleFactor
		{
			get => textScaleFactor;

			set
			{
				textScaleFactor = value;

				RegenerateFont();
			}
		}

		static Font defaultFont = CoreUtils.SkinningDefs.TheInstance.GetFont(11);

		public bool tabbedBackground = false;
		public bool wrapped = false;
		public bool no_border = false;

		protected string text = "";

		protected Font font = defaultFont;

		protected string cellName = "";

		protected Pen border_pen = Pens.LightGray;

		protected bool auto_translate = true;

		public ContentAlignment TextAlignment
		{
			get
			{
				return contentAlignment;
			}
		}

		public TextTableCell (PureTable table)
			: base (table)
		{
			if(auto_translate)
			{
				string fstr = font.FontFamily.GetName(409);
				font = ConstantSizeFont.NewFont( TextTranslator.TheInstance.GetTranslateFont(fstr), 
					TextTranslator.TheInstance.GetTranslateFontSizeForName("reports", fstr, SkinningDefs.TheInstance.GetFloatData("pdf_report_table_font_size", 11)), font.Style);
			}

			fontSize = font.Size;
			RegenerateFont();
		}

		void RegenerateFont ()
		{
			var baseFont = font ?? defaultFont;
			var family = baseFont.FontFamily;
			var style = baseFont.Style;
			font?.Dispose();

			font = new Font (family, fontSize * textScaleFactor, style);
		}

		public override void SetBorderColour(Color c)
		{
			if(border_pen != Pens.LightGray)
			{
				border_pen.Dispose();
			}
			border_pen= new Pen(c,1);
		}

		public string CellName
		{
			get
			{
				return cellName;
			}

			set
			{
				cellName = value;
			}
		}

		public override void SetFont(Font f)
		{
			if(!auto_translate)
			{
				f = ConstantSizeFont.NewFont(f.FontFamily.GetName(409), f.Size, f.Style);
			}
			else
			{
				string fstr = f.FontFamily.GetName(409);
				f = ConstantSizeFont.NewFont( TextTranslator.TheInstance.GetTranslateFont(fstr), TextTranslator.TheInstance.GetTranslateFontSize(fstr,(int)f.Size), f.Style);
			}
		}

        public override void SetFontStyle(FontStyle fs)
        {
            if (!auto_translate)
            {
                font = ConstantSizeFont.NewFont(font.FontFamily.GetName(409), font.Size, fs);
            }
            else
            {
                string fstr = font.FontFamily.GetName(409);
                font = ConstantSizeFont.NewFont(TextTranslator.TheInstance.GetTranslateFont(fstr), TextTranslator.TheInstance.GetTranslateFontSize(fstr, (int)font.Size), fs);
            }
        }

		public override void SetFontSize(double size)
		{
			fontSize = (float) size;
			size *= textScaleFactor;
			if (!auto_translate)
			{
				font = ConstantSizeFont.NewFont(font.FontFamily.GetName(409), (float) size, font.Style);
			}
			else
			{
				string fstr = font.FontFamily.GetName(409);
				font = ConstantSizeFont.NewFont( TextTranslator.TheInstance.GetTranslateFont(fstr), TextTranslator.TheInstance.GetTranslateFontSize(fstr,(float) size), font.Style);
			}
		}

		public Font GetFont() { return font; }

		public string Text
		{
			set
			{
				if(!auto_translate)
				{
					text = value;
				}
				else
				{
					text= TextTranslator.TheInstance.Translate(value);
				}
			}

			get
			{
				return text;
			}
		}

		public void setNoBorder(bool no_value)
		{
			this.no_border = no_value;
		}

		public override void Paint(DestinationDependentGraphics ddg)
		{
			WindowsGraphics g = (WindowsGraphics)ddg;

			if (tabbedBackground)
			{
				Image leftEdge = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\tabs\\table_tab_left.png");
				Image mid = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\tabs\\table_tab_mid.png");
				Image rightEdge = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\tabs\\table_tab_right.png");

				// Involved workaround for the GDI / .NET bug where horizontally-stretched bitmaps
				// have garbage towards the right-hand end.  Set a clip rectangle and then
				// scale up much more than we need.
				InterpolationMode oldInterpolation = g.Graphics.InterpolationMode;
				g.Graphics.InterpolationMode =  InterpolationMode.NearestNeighbor;

				Region oldClip = g.Graphics.Clip;
				g.Graphics.Clip = new Region (new Rectangle (0, 0, width, height));

				g.Graphics.DrawImage(mid, leftEdge.Width, 0, (width - rightEdge.Width - leftEdge.Width) * 2, height);
				g.Graphics.DrawImage(leftEdge, 0, 0, leftEdge.Width, height);
				g.Graphics.DrawImage(rightEdge, width - rightEdge.Width, 0, rightEdge.Width, height);

				g.Graphics.Clip = oldClip;
				g.Graphics.InterpolationMode = oldInterpolation;
			}
			else
			{
				if(backBrush != null)
				{
					g.Graphics.FillRectangle(backBrush,0,0,width,height);
				}

				if (no_border == false)
				{
					g.Graphics.DrawRectangle(border_pen, 0, 0, width - 1, height - 1);
				}
			}

			StringFormat stringFormat = new StringFormat (StringFormatFlags.MeasureTrailingSpaces);
			SizeF size = g.Graphics.MeasureString(text, font, width, stringFormat);
			RectangleF rect = new RectangleF (0, 0, width, height);

			int x = 0, y = 0;

			if (wrapped)
			{
				StringFormat format = new StringFormat ();
				g.Graphics.DrawString(text, font, this.foreBrush, rect, format);
			}
			else
			{
				// Do the horizontal alignment.
				switch (this.contentAlignment)
				{
					case ContentAlignment.BottomLeft:
					case ContentAlignment.MiddleLeft:
					case ContentAlignment.TopLeft:
						x = 0;
						break;

					case ContentAlignment.BottomCenter:
					case ContentAlignment.MiddleCenter:
					case ContentAlignment.TopCenter:
						x = (width / 2) - (int) (size.Width / 2);
						break;

					case ContentAlignment.BottomRight:
					case ContentAlignment.MiddleRight:
					case ContentAlignment.TopRight:
						x = width - 1 - (int) size.Width;
						break;
				}

				// If too wide, don't do anything clever.
				if (size.Width >= width)
				{
					x = 0;
				}

				// Do the vertical alignment.
				switch (this.contentAlignment)
				{
					case ContentAlignment.TopLeft:
					case ContentAlignment.TopCenter:
					case ContentAlignment.TopRight:
						y = 0;
						break;

					case ContentAlignment.MiddleLeft:
					case ContentAlignment.MiddleCenter:
					case ContentAlignment.MiddleRight:
						y = (height / 2) - (int) (size.Height / 2);
						break;

					case ContentAlignment.BottomLeft:
					case ContentAlignment.BottomRight:
					case ContentAlignment.BottomCenter:
						y = height - 1 - (int) size.Height;
						break;
				}

				g.Graphics.DrawString(text, font, this.foreBrush, x, y);
			}
		}
	}
}
