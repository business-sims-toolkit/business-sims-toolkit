using System.Drawing;

using ResizingUi.Button;

namespace DevOpsUi.FacilitatorControls.FeatureDevelopment
{
	internal class FeatureSelectionButtonFactory
	{
		public static StyledDynamicButton CreateButton(string text, string tag, bool isOptimal, bool isActive, bool isEnabled,
		                                               int width, int height, Color? altForeColour = null, string styleName = "standard", bool useDynamicFont = false)
		{
			var button = new StyledDynamicButton(styleName, text, useDynamicFont)
			{
				Size = new Size(width, height),
				Tag = tag,
				Highlighted = isOptimal,
				Active = isActive,
				Enabled = isEnabled,
				Margin = 4,
				CornerRadius = 4
			};

			if (altForeColour != null)
			{
				button.ForeColor = altForeColour.Value;
			}

			return button;
		}
	}
}
