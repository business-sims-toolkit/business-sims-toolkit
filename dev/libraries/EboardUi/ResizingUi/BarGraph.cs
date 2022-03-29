using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResizingUi
{
	public class BarGraph 
	{
		// TODO this code is taken from the FeatureDevelopmentModule
		// Either make it a self-contained control with the node it should
		// be tracking or make the below method a static method (probably the latter)
		//void RenderBarChart(Graphics graphics, RectangleF bounds, string title, float minValue, float maxValue,
		//                    float value, Func<float, float, Color> colourFunc, Func<float, string> valueFormatter, bool originateFromTarget = false, float? targetValue = null, bool showTargetLine = false)
		//{
		//	var startValue = originateFromTarget ? targetValue ?? minValue : minValue;

		//	var startValueXPosition = (float)Maths.MapBetweenRanges(startValue, minValue, maxValue, bounds.Left, bounds.Right);
		//	var valueXPosition = (float)Maths.MapBetweenRanges(value, minValue, maxValue, bounds.Left, bounds.Right);

		//	var barRectangleX = Math.Min(startValueXPosition, valueXPosition);
		//	var barWidth = Math.Abs(startValueXPosition - valueXPosition);

		//	const float minBarHeight = 10;
		//	var barHeight = Math.Max(bounds.Height * 0.65f, minBarHeight);

		//	const float targetOffset = 3;
		//	var targetMarkerHeight = barHeight + 2 * targetOffset;


		//	// TODO move to skin file
		//	using (var barBackBrush = new SolidBrush(CONVERT.ParseHtmlColor("#bbbbbb")))
		//	using (var barFillBrush = new SolidBrush(colourFunc(value, targetValue ?? minValue)))
		//	{
		//		var barY = bounds.Y + targetOffset;
		//		graphics.FillRectangle(barBackBrush, new RectangleF(bounds.X, barY, bounds.Width, barHeight));

		//		var barFillBounds = new RectangleF(barRectangleX, barY, barWidth, barHeight);
		//		graphics.FillRectangle(barFillBrush, barFillBounds);

		//		if (showTargetLine && targetValue != null)
		//		{
		//			var targetXPosition = (float)Maths.MapBetweenRanges(targetValue.Value, minValue, maxValue, bounds.Left, bounds.Right);

		//			graphics.FillRectangle(value >= targetValue ? barFillBrush : barBackBrush, new RectangleF(targetXPosition, barY - targetOffset, 2, targetMarkerHeight));
		//		}
		//	}

		//}

	}
}
