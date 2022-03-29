using System.Drawing;
using System.Windows.Forms;

namespace LibCore
{
	public static class RectangleExtensions
	{
		public static Rectangle Map (this Rectangle sourceRectangle, Control source, Control destination)
		{
			var topLeftSource = sourceRectangle.Location;
			var topLeftScreen = (source != null) ? source.PointToScreen(topLeftSource) : topLeftSource;
			var topLeftDestination = (destination != null) ? destination.PointToClient(topLeftScreen) : topLeftScreen;

			return new Rectangle (topLeftDestination, sourceRectangle.Size);
		}
	}
}