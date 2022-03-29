using System.Drawing;

using CommonGUI;
using ResizingUi.Button;

namespace DevOps.OpsScreen.ServiceDevelopmentUi
{
    internal class AppDevelopmentButtonFactory
    {
        public static StyledDynamicButton CreateButton (string text, string tag, bool isOptimal, int width, int height,
                                                        string styleName = "standard")
        {
            return new StyledDynamicButton(styleName, text)
            {
                Size = new Size(width, height),
                Tag = tag,
                Highlighted = isOptimal
            };
        }

        public static StyledDynamicButton CreateButton (string text, string tag, bool isOptimal, bool isActive, bool isEnabled,
                                                        int width, int height, Color? altForeColour = null, string styleName = "standard")
        {
            var button = new StyledDynamicButton(styleName, text)
            {
                Size = new Size(width, height),
                Tag = tag,
                Highlighted = isOptimal,
                Active = isActive,
                Enabled = isEnabled
            };

            if (altForeColour != null)
            {
                button.ForeColor = altForeColour.Value;
            }

            return button;
        }

    }
}
