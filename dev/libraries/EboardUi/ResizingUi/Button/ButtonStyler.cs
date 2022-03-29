using System;
using System.Drawing;

namespace ResizingUi.Button
{
	internal class ButtonStyler
	{
		public ButtonStyler(string styleName, IStyledButton button):
			this(new ButtonStyleSheet(styleName), button)
		{
		}

		public ButtonStyler (ButtonStyleSheet styleSheet, IStyledButton button)
		{
			this.styleSheet = styleSheet;
			button.HighlightChanged += button_HighlightChanged;

			SetDefaultColours(false);
		}


		public Color BackColour { get; private set; }
		public Color BorderColour { get; private set; }
		public Color ForeColour { get; private set; }

		public void SetColoursForState(bool isDefault, bool isActive, bool isMouseOver, bool isMouseDown, bool isEnabled)
		{
			if (isDefault)
			{
				BackColour = isEnabled ? currentDefaultBackColour : currentDefaultDisabledBackColour;
				BorderColour = isEnabled ? currentDefaultBorderColour : currentDefaultDisabledBorderColour;
				ForeColour = isEnabled ? currentDefaultForeColour : currentDefaultDisabledForeColour;
			}
			else if (isActive || isMouseDown)
			{
				BackColour = isEnabled ? styleSheet.ActiveBackColour : styleSheet.ActiveDisabledBackColour;
				BorderColour = isEnabled ? styleSheet.ActiveBorderColour : styleSheet.ActiveDisabledBorderColour;
				ForeColour = isEnabled ? styleSheet.ActiveForeColour : styleSheet.ActiveDisabledForeColour;
			}
			else if (isMouseOver)
			{
				BackColour = styleSheet.HoverBackColour;
				BorderColour = styleSheet.HoverBorderColour;
				ForeColour = styleSheet.HoverForeColour;
			}
		}
		
		void button_HighlightChanged(object sender, EventArgs e)
		{
			SetDefaultColours(((IStyledButton)sender).Highlighted || ((System.Windows.Forms.Control)sender).Focused);
		}

		void SetDefaultColours(bool isHighlighted)
		{
			currentDefaultBackColour = isHighlighted ? styleSheet.HighlightBackColour : styleSheet.DefaultBackColour;
			currentDefaultBorderColour = isHighlighted ? styleSheet.HighlightBorderColour : styleSheet.DefaultBorderColour;
			currentDefaultForeColour = isHighlighted ? styleSheet.HighlightForeColour : styleSheet.DefaultForeColour;

			currentDefaultDisabledBackColour = isHighlighted ? styleSheet.HighlightDisabledBackColour : styleSheet.DefaultDisabledBackColour;
			currentDefaultDisabledBorderColour = isHighlighted ? styleSheet.HighlightDisabledBorderColour : styleSheet.DefaultDisabledBorderColour;
			currentDefaultDisabledForeColour = isHighlighted ? styleSheet.HighlightDisabledForeColour : styleSheet.DefaultDisabledForeColour;
		}

		readonly ButtonStyleSheet styleSheet;

		Color currentDefaultBackColour;
		Color currentDefaultBorderColour;
		Color currentDefaultForeColour;

		Color currentDefaultDisabledBackColour;
		Color currentDefaultDisabledBorderColour;
		Color currentDefaultDisabledForeColour;

		//readonly Color defaultBackColour;
		//readonly Color activeBackColour;
		//readonly Color highlightBackColour;
		//readonly Color hoverBackColour;

		//readonly Color defaultDisabledBackColour;
		//readonly Color activeDisabledBackColour;
		//readonly Color highlightDisabledBackColour;

		//readonly Color defaultBorderColour;
		//readonly Color activeBorderColour;
		//readonly Color highlightBorderColour;
		//readonly Color hoverBorderColour;

		//readonly Color defaultDisabledBorderColour;
		//readonly Color activeDisabledBorderColour;
		//readonly Color highlightDisabledBorderColour;

		//readonly Color defaultForeColour;
		//readonly Color activeForeColour;
		//readonly Color highlightForeColour;
		//readonly Color hoverForeColour;

		//readonly Color defaultDisabledForeColour;
		//readonly Color activeDisabledForeColour;
		//readonly Color highlightDisabledForeColour;
	}
}
