using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

using Algorithms;
using CommonGUI;
using CoreUtils;
using ResizingUi.Enums;
using ResizingUi.Extensions;

namespace ResizingUi.Button
{
	public class StyledImageButton : ImageButton, IStyledButton
	{
		public StyledImageButton(int code)
			: base(code)
		{
			useImageBackground = true;
		}

		public StyledImageButton(int code, bool flickerFree)
			: base(code, flickerFree)
		{
			useImageBackground = true;
		}

		readonly bool useImageBackground;
		public StyledImageButton(string baseStyle, int code = 0, bool useImageBackground = true)
			: base(code, true)
		{
			foreColourKey = SkinningDefs.TheInstance.GetColorData($"{baseStyle}_button_fore_colour_key");
			backColourKey = SkinningDefs.TheInstance.GetColourData($"{baseStyle}_button_back_colour_key");

			this.useImageBackground = useImageBackground;
			
			buttonStyler = new ButtonStyler(baseStyle, this);
		}
		
		readonly ButtonStyler buttonStyler;

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

		protected override Image GetImageForState ()
		{
			return useImageBackground ? base.GetImageForState() : up;
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
			e.Graphics.CompositingQuality = CompositingQuality.HighQuality;
			e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
			e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;


			var inDefaultState = ! Active && ! mouseDown && ! mouseHover;
			buttonStyler.SetColoursForState(inDefaultState, active, mouseHover, mouseDown, Enabled);

			var image = GetImageForState();

			if (image == null)
			{
				e.Graphics.FillRectangle(Brushes.HotPink, new Rectangle(0, 0, Width, Height));
				return;
			}
			
			var colourMappings = new Dictionary<Color, Color>
			{
				{ foreColourKey, buttonStyler.ForeColour }
			};

			if (useImageBackground && backColourKey != null)
			{
				colourMappings.Add(backColourKey.Value, buttonStyler.BackColour);
			}

			if (colourMappings.Count > 0)
			{
				image = new Bitmap(image).ConvertColours(colourMappings);
			}
			

			if (! useImageBackground)
			{
				BackColor = Color.Transparent;

				using (var backBrush = new SolidBrush(buttonStyler.BackColour))
				using (var borderPen = new Pen(buttonStyler.BorderColour, SkinningDefs.TheInstance.GetFloatData("styled_button_border_width", 2f))) // TODO
				{
					if (useCircularBackground)
					{
						cornerRadius = Math.Min(Width * 0.5f, Height * 0.5f);
					}

					e.Graphics.DrawAndFillRoundedRectangle(backBrush, borderPen, ClientRectangle, cornerRadius ?? 0, RectangleCorners.All);

				}


			}

			

			var imageBounds = new Rectangle(0, 0, image.Width, image.Height);

			e.Graphics.DrawImage(image, ClientRectangle.AlignRectangle(Width - Margin.Left - Margin.Right, Height - Margin.Top - Margin.Bottom, StringAlignment.Near, StringAlignment.Near, Margin.Left, Margin.Top), imageBounds, GraphicsUnit.Pixel);

		}


		//readonly Dictionary<string, Color> stateToColours;
		readonly Color foreColourKey;
		readonly Color? backColourKey;

		bool highlighted;

		public event EventHandler HighlightChanged;

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

		void OnHighlightChanged ()
		{
			HighlightChanged?.Invoke(this, EventArgs.Empty);
		}
	}
}
