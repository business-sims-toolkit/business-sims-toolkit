using System.Drawing;
using System.Drawing.Drawing2D;

using Algorithms;
using ResizingUi.Extensions;

namespace DevOpsUi.FacilitatorControls.ServiceLozenge
{
	internal static class LozengeIconRenderer
	{
		public static void RenderIconReticuleAndBackground (Graphics graphics, RectangleF bounds, StatusColours statusColours)
		{
			using (var reticulePen = new Pen(statusColours.ReticuleColour, 1))
			{
				graphics.DrawRectangleFReticule(bounds, reticulePen, 10);
				if (statusColours.HatchFillProperties != null)
				{

					using (var graphicsPath = new GraphicsPath())
					{
						graphicsPath.AddEllipse(bounds);
						graphics.DrawHatchedArea(bounds, statusColours.HatchFillProperties, graphicsPath, 8);
					}
				}
				else if (statusColours.CircleFillColour != null)
				{
					using (var backgroundBrush = new SolidBrush(statusColours.CircleFillColour.Value))
					{
						graphics.FillEllipse(backgroundBrush, bounds);
					}
				}

				if (statusColours.CircleOutlineColour != null)
				{
					var outlineThickness = bounds.Width * 0.065f;
					using (var outlinePen = new Pen(statusColours.CircleOutlineColour.Value, outlineThickness))
					{
						graphics.DrawEllipse(outlinePen, bounds.AlignRectangle(bounds.Width - outlineThickness, bounds.Height - outlineThickness));
					}
				}

			}
		}
	}
}
