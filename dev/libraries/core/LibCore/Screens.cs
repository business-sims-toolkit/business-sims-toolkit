using System;
using System.Drawing;
using System.Windows.Forms;

namespace LibCore
{
	public static class Screens
	{
		public static Rectangle GetAllScreenBounds ()
		{
			Rectangle? bounds = null;

			foreach (Screen screen in Screen.AllScreens)
			{
				if (bounds.HasValue)
				{
					int left = Math.Min(bounds.Value.Left, screen.Bounds.Left);
					int top = Math.Min(bounds.Value.Top, screen.Bounds.Top);

					int width = Math.Max(bounds.Value.Right, screen.Bounds.Right) - left;
					int height = Math.Max(bounds.Value.Bottom, screen.Bounds.Bottom) - top;

					bounds = new Rectangle(left, top, width, height);

				}
				else
				{
					bounds = screen.Bounds;
				}
			}

			return bounds.Value;
		}
	}
}
