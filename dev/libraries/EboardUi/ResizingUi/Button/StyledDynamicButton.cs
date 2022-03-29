using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

using Algorithms;
using CommonGUI;
using CoreUtils;
using LibCore;
using ResizingUi.Enums;
using ResizingUi.Extensions;
using ResizingUi.Interfaces;

namespace ResizingUi.Button
{
	public class StyledDynamicButton : ImageTextButton, IStyledButton, IDynamicSharedFontSize
	{
		public StyledDynamicButton(int code)
			: this(code, true)
		{
		}

		public StyledDynamicButton(int code, bool flickerFree)
			: base(code, flickerFree)
		{
		}

		public StyledDynamicButton(string fileBase, bool flickerFree)
			: base(fileBase, flickerFree)
		{
		}


		public StyledDynamicButton(string styleName, string buttonText, bool useDynamicFont = false)
		: base(0, true)
		{
			this.buttonText = buttonText;
			this.useDynamicFont = useDynamicFont;

			buttonStyler = new ButtonStyler(styleName, this);
			borderWidth = SkinningDefs.TheInstance.GetFloatData($"{styleName}_button_border_width", SkinningDefs.TheInstance.GetFloatData("styled_button_border_width", 2f));
		}

		public bool Highlighted
		{
			get => highlighted;
			set
			{
				highlighted = value;
				OnHighlightChanged();
				Invalidate();
			}
		}

		bool highlighted;

		public event EventHandler HighlightChanged;

		float? cornerRadius;

		public float? CornerRadius
		{
			get => cornerRadius;
			set
			{
				cornerRadius = value;
				Invalidate();
			}
		}

		bool useCircularBackground;

		public bool UseCircularBackground
		{
			set
			{
				useCircularBackground = value;
				Invalidate();
			}
		}

		void OnHighlightChanged()
		{
			HighlightChanged?.Invoke(this, EventArgs.Empty);

			Invalidate();
		}

		protected override void OnGotFocus(EventArgs e)
		{
			base.OnGotFocus(e);

			OnHighlightChanged();
		}

		protected override void OnLostFocus(EventArgs e)
		{
			base.OnLostFocus(e);

			OnHighlightChanged();
		}

		public string ButtonText
		{
			get => buttonText;
			set
			{
				buttonText = value;
				Invalidate();
			}
		}

		public override Size GetPreferredSize(Size proposedSize)
		{
			using (var graphics = CreateGraphics())
			{
				var stringFormat = new StringFormat(StringFormatFlags.MeasureTrailingSpaces)
				{
					LineAlignment = Alignment.GetVerticalAlignment(TextAlign),
					Alignment = Alignment.GetHorizontalAlignment(TextAlign),
					Trimming = StringTrimming.None,
					FormatFlags = StringFormatFlags.NoClip | StringFormatFlags.NoWrap
				};

				var textSize = graphics.MeasureString(buttonText, Font, Width - 2 * Margin, stringFormat);

				return new Size((int)textSize.Width + (2 * Margin), (int)textSize.Height + (2 * Margin));
			}
		}

		protected override void OnForeColorChanged(EventArgs e)
		{
			foreColourOverriden = true;
			Invalidate();
		}

		bool foreColourOverriden;


		protected override void OnSizeChanged(EventArgs e)
		{
			if (useDynamicFont)
			{
				var textSize = new RectangleF(0, 0, Width, Height).AlignRectangle(Width - 2 * Margin, Height - 2 * Margin).Size;

				FontSize = FontSizeToFit = this.GetFontSizeInPixelsToFit(Font.Style, ButtonText, textSize);
			}

			Invalidate();
		}

		protected override void OnFontChanged(EventArgs e)
		{
			base.OnFontChanged(e);

			Invalidate();
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			var graphics = e.Graphics;

			var renderFormat = new StringFormat
			{
				LineAlignment = Alignment.GetVerticalAlignment(TextAlign),
				Alignment = Alignment.GetHorizontalAlignment(TextAlign),
				Trimming = StringTrimming.None
			};

			var inDefaultState = !Active && !mouseDown && !mouseHover;
			buttonStyler.SetColoursForState(inDefaultState, active, mouseHover, mouseDown, Enabled);

			using (var backBrush = new SolidBrush(buttonStyler.BackColour))
			using (var borderBrush = new SolidBrush(buttonStyler.BorderColour))
			using (var textBrush = new SolidBrush(foreColourOverriden ? ForeColor : buttonStyler.ForeColour))
			{
				var bounds = new RectangleF(0, 0, Width, Height);

				if (useCircularBackground)
				{
					cornerRadius = Math.Min(Width * 0.5f, Height * 0.5f);
				}

				if (cornerRadius != null)
				{
					using (var borderPen = new Pen(borderBrush, borderWidth))
					{
						graphics.DrawAndFillRoundedRectangle(backBrush, borderPen, bounds, cornerRadius.Value, RectangleCorners.All);
					}
				}
				else
				{
					graphics.FillRectangle(borderBrush, bounds);
					graphics.SmoothingMode = SmoothingMode.None;
					graphics.FillRectangle(backBrush, bounds.CentreSubRectangle(Width - 2 * borderWidth, Height - 2 * borderWidth));
				}
				
				graphics.SmoothingMode = SmoothingMode.AntiAlias;
				graphics.DrawString(buttonText, Font, textBrush, new Rectangle (0, 0, Width, Height), renderFormat);
			}
		}


		float borderWidth;

		readonly ButtonStyler buttonStyler;

		public float BorderWidth
		{
			get => borderWidth;

			set
			{
				borderWidth = value;
				Invalidate();
			}
		}

		readonly bool useDynamicFont;

		void UpdateFontSize ()
		{
			if (useDynamicFont)
			{
				Font = SkinningDefs.TheInstance.GetPixelSizedFont(fontSize);
				Invalidate();
			}
		}

		public float FontSize
		{
			get => fontSize;
			set
			{
				fontSize = value;
				UpdateFontSize();
			}
		}

		public float FontSizeToFit
		{
			get => fontSizeToFit;
			set
			{
				if (Math.Abs(fontSizeToFit - value) > float.Epsilon)
				{
					fontSizeToFit = value;
					OnFontSizeToFitChanged();
				}
			}
		}

		float fontSizeToFit;
		float fontSize;

		public event EventHandler FontSizeToFitChanged;

		void OnFontSizeToFitChanged ()
		{
			FontSizeToFitChanged?.Invoke(this, EventArgs.Empty);
		}


	}
}
