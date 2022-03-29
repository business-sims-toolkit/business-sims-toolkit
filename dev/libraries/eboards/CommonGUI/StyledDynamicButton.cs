using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

using Algorithms;
using CoreUtils;
using LibCore;
using Brushes = System.Drawing.Brushes;

namespace CommonGUI
{
	
	public class StyledImageButtonCommon : ImageButton
	{
		public StyledImageButtonCommon (int code)
			: base(code)
		{
		}

		public StyledImageButtonCommon (int code, bool flickerFree)
			: base(code, flickerFree)
		{
		}

		public StyledImageButtonCommon (string baseSkinStyle, int code = 0)
			: base(code, true)
		{
			//baseImage = Repository.TheInstance.GetImage(imageFilepath);
			stateToColours = new Dictionary<string, Color>();

			foreColourKey = SkinningDefs.TheInstance.GetColorData($"{baseSkinStyle}_fore_colour_key");
			backColourKey = SkinningDefs.TheInstance.GetColourData($"{baseSkinStyle}_back_colour_key");

			var states = new []
			{
				"default",
				"active",
				"hover",
				"disabled",
				"active_disabled"
			};

			var fields = new []
			{
				"fore",
				"back"
			};

			foreach (var state in states)
			{
				foreach (var field in fields)
				{
					var replacementColour = SkinningDefs.TheInstance.GetColourData($"{baseSkinStyle}_{state}_{field}_colour");

					if (replacementColour != null)
					{
						stateToColours[$"{state}_{field}"] = replacementColour.Value;
					}
				}
			}

		}

		protected override void OnPaint (PaintEventArgs e)
		{
			var state = "default";

			
			if (! Enabled)
			{
				state = active ? "active_disabled" : "disabled";
			}
			else if ((mouseHover && mouseDown) || Active)
			{
				state = "active";
			}
			else if (mouseHover)
			{
				state = "hover";
			}
			
			if (up == null)
			{
				e.Graphics.FillRectangle(Brushes.HotPink, new Rectangle(0, 0, Width, Height));
				return;
			}

			var image = up;

			if (! string.IsNullOrEmpty(state))
			{
				var colourKeys = new Dictionary<string, Color>
				{
					{ "fore", foreColourKey }
				};

				if (backColourKey != null)
				{
					colourKeys["back"] = backColourKey.Value;
				}

				var colourMappings = new Dictionary<Color, Color>();


				foreach (var field in new []
				{
					"fore", "back"
				})
				{
					var key = $"{state}_{field}";

					if (stateToColours.ContainsKey(key))
					{
						colourMappings[colourKeys[field]] = stateToColours[key];
					}
				}

				if (colourMappings.Count > 0)
				{
					image = new Bitmap(image).ConvertColours(colourMappings);
				}

			}

			var imageBounds = new Rectangle(0, 0, image.Width, image.Height);

			e.Graphics.DrawImage(image, ClientRectangle, imageBounds, GraphicsUnit.Pixel);

		}
		

		readonly Dictionary<string, Color> stateToColours;
		readonly Color foreColourKey;
		readonly Color? backColourKey;

	}
	
	public class StyledDynamicButtonCommon : ImageTextButton, IStyledButton
    {
        public StyledDynamicButtonCommon(int code)
            : this(code, true)
        {
        }

        public StyledDynamicButtonCommon(int code, bool flickerFree)
            : base(code, flickerFree)
        {
        }

        public StyledDynamicButtonCommon(string fileBase, bool flickerFree)
            : base(fileBase, flickerFree)
        {
        }


        public StyledDynamicButtonCommon(string styleName, string buttonText)
        : base(0, true)
        {
            this.buttonText = buttonText;

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

        void OnHighlightChanged()
        {
            HighlightChanged?.Invoke(this, EventArgs.Empty);

			Invalidate();
        }

	    protected override void OnGotFocus (EventArgs e)
	    {
		    base.OnGotFocus(e);

			OnHighlightChanged();
	    }

	    protected override void OnLostFocus (EventArgs e)
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

        public override Size GetPreferredSize (Size proposedSize)
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

		        return new Size((int) textSize.Width + (2 * Margin), (int) textSize.Height + (2 * Margin));
	        }
        }
        
        protected override void OnForeColorChanged (EventArgs e)
        {
            foreColourOverriden = true;
            Invalidate();
        }

        bool foreColourOverriden;


        protected override void OnSizeChanged(EventArgs e)
        {
            Invalidate();
        }

	    protected override void OnFontChanged (EventArgs e)
	    {
		    base.OnFontChanged(e);

			Invalidate();
	    }

        protected override void OnPaint(PaintEventArgs e)
        {
            var graphics = e.Graphics;

            var stringFormat = new StringFormat(StringFormatFlags.MeasureTrailingSpaces)
            {
                LineAlignment = Alignment.GetVerticalAlignment(TextAlign),
                Alignment = Alignment.GetHorizontalAlignment(TextAlign),
				Trimming = StringTrimming.None,
				FormatFlags = StringFormatFlags.NoClip | StringFormatFlags.NoWrap
            };

            var textSize = graphics.MeasureString(buttonText, Font, Width - 2 * Margin, stringFormat);
            var inDefaultState = !Active && !mouseDown && !mouseHover;
            buttonStyler.SetColoursForState(inDefaultState, active, mouseHover, mouseDown, Enabled);

            using (var backBrush = new SolidBrush(buttonStyler.BackColour))
            using (var borderBrush = new SolidBrush(buttonStyler.BorderColour))
            using (var textBrush = new SolidBrush(foreColourOverriden ? ForeColor : buttonStyler.ForeColour))
            {
                var bounds = new RectangleF(0, 0, Width, Height);

                graphics.FillRectangle(borderBrush, bounds);
				graphics.SmoothingMode = SmoothingMode.None;
				graphics.FillRectangle(backBrush, bounds.CentreSubRectangle(Width - 2 * borderWidth, Height - 2 * borderWidth));

                var textBounds = new RectangleF (0, 0, Width - 2 * Margin, Height).AlignRectangle(textSize.Width, textSize.Height,
                    stringFormat.Alignment, stringFormat.LineAlignment);

	            graphics.SmoothingMode = SmoothingMode.AntiAlias;

				graphics.DrawString(buttonText, Font, textBrush, textBounds, stringFormat);
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
    }

}
