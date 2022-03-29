using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace UI
{
	public class ImageTextButton : ImageButton
	{
		ContentAlignment textAlign;
		public ContentAlignment TextAlign
		{
			get
			{
				return textAlign;
			}

			set
			{
				textAlign = value;
				Invalidate();
			}
		}

		Color focusColour;
		public Color FocusColor
		{
			get
			{
				return focusColour;
			}

			set
			{
				focusColour = value;
				Invalidate();
			}
		}

		Color hoverColour;
		public Color HoverColor
		{
			get
			{
				return hoverColour;
			}

			set
			{
				hoverColour = value;
				Invalidate();
			}
		}

		Color activeColour;
		public Color ActiveColor
		{
			get
			{
				return activeColour;
			}

			set
			{
				activeColour = value;
				Invalidate();
			}
		}

		Color disabledColour;
		public Color DisabledColor
		{
			get
			{
				return disabledColour;
			}

			set
			{
				disabledColour = value;
				Invalidate();
			}
		}

		public ImageTextButton ()
		{
		}

		public void SetColours (Color normalColour, Color activeColour, Color focusColour, Color hoverColour, Color disabledColour)
		{
			ForeColor = normalColour;
			ActiveColor = activeColour;
			FocusColor = focusColour;
			HoverColor = hoverColour;
			DisabledColor = disabledColour;
		}

		protected override void OnPaint (PaintEventArgs e)
		{
			base.OnPaint(e);

			DrawText(e);
		}

		public virtual void DrawText (PaintEventArgs e)
		{
			Color colour;
			switch (State)
			{
				case SkinnedButtonState.Active:
					colour = activeColour;
					break;

				case SkinnedButtonState.Disabled:
					colour = disabledColour;
					break;

				case SkinnedButtonState.Focussed:
					colour = focusColour;
					break;

				case SkinnedButtonState.Hover:
					colour = hoverColour;
					break;

				case SkinnedButtonState.Normal:
				default:
					colour = ForeColor;
					break;
			}

			using (Brush brush = new SolidBrush (colour))
			{
				StringFormat format = new StringFormat ();
				format.Alignment = LibCore.Alignment.GetHorizontalAlignment(TextAlign);
				format.LineAlignment = LibCore.Alignment.GetVerticalAlignment(TextAlign);

				e.Graphics.DrawString(Text, Font, brush,
									  new RectangleF (0, 0, Width, Height),
									  format);
			}
		}
	}
}