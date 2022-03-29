using System;
using System.Drawing;

using Algorithms;
using CoreUtils;

namespace CommonGUI
{
    internal class ButtonStyler
    {
		public ButtonStyler(string styleName, IStyledButton button)
		{
			button.HighlightChanged += button_HighlightChanged;

			defaultBackColour = GetColourFromSkin(styleName, nameof(defaultBackColour));
			activeBackColour = GetColourFromSkin(styleName, nameof(activeBackColour));
			highlightBackColour = GetColourFromSkin(styleName, nameof(highlightBackColour), defaultBackColour);
			hoverBackColour = GetColourFromSkin(styleName, nameof(hoverBackColour));

			defaultDisabledBackColour = GetColourFromSkin(styleName, nameof(defaultDisabledBackColour));
			activeDisabledBackColour = GetColourFromSkin(styleName, nameof(activeDisabledBackColour));
			highlightDisabledBackColour = GetColourFromSkin(styleName, nameof(highlightDisabledBackColour), defaultDisabledBackColour);

			defaultBorderColour = GetColourFromSkin(styleName, nameof(defaultBorderColour), defaultBackColour);
			activeBorderColour = GetColourFromSkin(styleName, nameof(activeBorderColour), activeBackColour);
			highlightBorderColour = GetColourFromSkin(styleName, nameof(highlightBorderColour), highlightBackColour);
			hoverBorderColour = GetColourFromSkin(styleName, nameof(hoverBorderColour), hoverBackColour);

			defaultDisabledBorderColour = GetColourFromSkin(styleName, nameof(defaultDisabledBorderColour), defaultDisabledBackColour);
			activeDisabledBorderColour = GetColourFromSkin(styleName, nameof(activeDisabledBorderColour), activeDisabledBackColour);
			highlightDisabledBorderColour = GetColourFromSkin(styleName, nameof(highlightDisabledBorderColour), highlightDisabledBackColour);

			defaultForeColour = GetColourFromSkin(styleName, nameof(defaultForeColour));
			activeForeColour = GetColourFromSkin(styleName, nameof(activeForeColour));
			highlightForeColour = GetColourFromSkin(styleName, nameof(highlightForeColour), defaultForeColour);
			hoverForeColour = GetColourFromSkin(styleName, nameof(hoverForeColour));

			defaultDisabledForeColour = GetColourFromSkin(styleName, nameof(defaultDisabledForeColour));
			activeDisabledForeColour = GetColourFromSkin(styleName, nameof(activeDisabledForeColour), activeForeColour);
			highlightDisabledForeColour = GetColourFromSkin(styleName, nameof(highlightDisabledForeColour), highlightForeColour);

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
				BackColour = isEnabled ? activeBackColour : activeDisabledBackColour;
				BorderColour = isEnabled ? activeBorderColour : activeDisabledBorderColour;
				ForeColour = isEnabled ? activeForeColour : activeDisabledForeColour;
			}
			else if (isMouseOver)
			{
				BackColour = hoverBackColour;
				BorderColour = hoverBorderColour;
				ForeColour = hoverForeColour;
			}
		}

		static Color GetColourFromSkin(string styleName, string propertyName, Color? defaultColour = null)
		{
			return SkinningDefs.TheInstance.GetColorData(GetSkinFileName(styleName, propertyName), defaultColour ?? Color.Fuchsia);
		}

		static string GetSkinFileName(string styleName, string propertyName)
		{
			return $"{styleName}_button_{propertyName.ToSnakeCase()}";
		}

		void button_HighlightChanged(object sender, EventArgs e)
		{
			SetDefaultColours(((IStyledButton)sender).Highlighted || ((System.Windows.Forms.Control)sender).Focused);
		}

		void SetDefaultColours(bool isHighlighted)
		{
			currentDefaultBackColour = isHighlighted ? highlightBackColour : defaultBackColour;
			currentDefaultBorderColour = isHighlighted ? highlightBorderColour : defaultBorderColour;
			currentDefaultForeColour = isHighlighted ? highlightForeColour : defaultForeColour;

			currentDefaultDisabledBackColour = isHighlighted ? highlightDisabledBackColour : defaultDisabledBackColour;
			currentDefaultDisabledBorderColour = isHighlighted ? highlightDisabledBorderColour : defaultDisabledBorderColour;
			currentDefaultDisabledForeColour = isHighlighted ? highlightDisabledForeColour : defaultDisabledForeColour;
		}

		Color currentDefaultBackColour;
		Color currentDefaultBorderColour;
		Color currentDefaultForeColour;

		Color currentDefaultDisabledBackColour;
		Color currentDefaultDisabledBorderColour;
		Color currentDefaultDisabledForeColour;

		readonly Color defaultBackColour;
		readonly Color activeBackColour;
		readonly Color highlightBackColour;
		readonly Color hoverBackColour;

		readonly Color defaultDisabledBackColour;
		readonly Color activeDisabledBackColour;
		readonly Color highlightDisabledBackColour;

		readonly Color defaultBorderColour;
		readonly Color activeBorderColour;
		readonly Color highlightBorderColour;
		readonly Color hoverBorderColour;

		readonly Color defaultDisabledBorderColour;
		readonly Color activeDisabledBorderColour;
		readonly Color highlightDisabledBorderColour;

		readonly Color defaultForeColour;
		readonly Color activeForeColour;
		readonly Color highlightForeColour;
		readonly Color hoverForeColour;

		readonly Color defaultDisabledForeColour;
		readonly Color activeDisabledForeColour;
		readonly Color highlightDisabledForeColour;
	}
}
