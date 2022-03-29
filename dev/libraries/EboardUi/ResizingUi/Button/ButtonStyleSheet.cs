using System.Drawing;

using Algorithms;
using CoreUtils;

namespace ResizingUi.Button
{
	public class ButtonStyleSheet
	{
		public ButtonStyleSheet (string styleName)
		{
			DefaultBackColour = GetColourFromSkin(styleName, nameof(DefaultBackColour));
			ActiveBackColour = GetColourFromSkin(styleName, nameof(ActiveBackColour));
			HighlightBackColour = GetColourFromSkin(styleName, nameof(HighlightBackColour), DefaultBackColour);
			HoverBackColour = GetColourFromSkin(styleName, nameof(HoverBackColour));

			DefaultDisabledBackColour = GetColourFromSkin(styleName, nameof(DefaultDisabledBackColour));
			ActiveDisabledBackColour = GetColourFromSkin(styleName, nameof(ActiveDisabledBackColour));
			HighlightDisabledBackColour = GetColourFromSkin(styleName, nameof(HighlightDisabledBackColour), DefaultDisabledBackColour);

			DefaultBorderColour = GetColourFromSkin(styleName, nameof(DefaultBorderColour), DefaultBackColour);
			ActiveBorderColour = GetColourFromSkin(styleName, nameof(ActiveBorderColour), ActiveBackColour);
			HighlightBorderColour = GetColourFromSkin(styleName, nameof(HighlightBorderColour), HighlightBackColour);
			HoverBorderColour = GetColourFromSkin(styleName, nameof(HoverBorderColour), HoverBackColour);

			DefaultDisabledBorderColour = GetColourFromSkin(styleName, nameof(DefaultDisabledBorderColour), DefaultDisabledBackColour);
			ActiveDisabledBorderColour = GetColourFromSkin(styleName, nameof(ActiveDisabledBorderColour), ActiveDisabledBackColour);
			HighlightDisabledBorderColour = GetColourFromSkin(styleName, nameof(HighlightDisabledBorderColour), HighlightDisabledBackColour);

			DefaultForeColour = GetColourFromSkin(styleName, nameof(DefaultForeColour));
			ActiveForeColour = GetColourFromSkin(styleName, nameof(ActiveForeColour));
			HighlightForeColour = GetColourFromSkin(styleName, nameof(HighlightForeColour), DefaultForeColour);
			HoverForeColour = GetColourFromSkin(styleName, nameof(HoverForeColour));

			DefaultDisabledForeColour = GetColourFromSkin(styleName, nameof(DefaultDisabledForeColour));
			ActiveDisabledForeColour = GetColourFromSkin(styleName, nameof(ActiveDisabledForeColour), ActiveForeColour);
			HighlightDisabledForeColour = GetColourFromSkin(styleName, nameof(HighlightDisabledForeColour), HighlightForeColour);

		}

		static Color GetColourFromSkin(string styleName, string propertyName, Color? defaultColour = null)
		{
			return SkinningDefs.TheInstance.GetColorData(GetSkinFileName(styleName, propertyName), defaultColour ?? Color.Fuchsia);
		}

		static string GetSkinFileName(string styleName, string propertyName)
		{
			return $"{styleName}_button_{propertyName.ToSnakeCase()}";
		}


		public Color DefaultBackColour { get; }
		public Color ActiveBackColour { get; }
		public Color HighlightBackColour { get; }
		public Color HoverBackColour { get; }

		public Color DefaultDisabledBackColour { get; }
		public Color ActiveDisabledBackColour { get; }
		public Color HighlightDisabledBackColour { get; }

		public Color DefaultBorderColour { get; }
		public Color ActiveBorderColour { get; }
		public Color HighlightBorderColour { get; }
		public Color HoverBorderColour { get; }

		public Color DefaultDisabledBorderColour { get; }
		public Color ActiveDisabledBorderColour { get; }
		public Color HighlightDisabledBorderColour { get; }

		public Color DefaultForeColour { get; }
		public Color ActiveForeColour { get; }
		public Color HighlightForeColour { get; }
		public Color HoverForeColour { get; }

		public Color DefaultDisabledForeColour { get; }
		public Color ActiveDisabledForeColour { get; }
		public Color HighlightDisabledForeColour { get; }
	}
}
