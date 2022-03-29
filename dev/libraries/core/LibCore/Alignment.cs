using System.Drawing;
using System.Windows.Forms;

namespace LibCore
{
	public static class Alignment
	{
		public static ContentAlignment GetContentAlignment (StringAlignment horizontal, StringAlignment vertical)
		{
			switch (vertical)
			{
				case StringAlignment.Near:
					switch (horizontal)
					{
						case StringAlignment.Near:
							return ContentAlignment.TopLeft;

						case StringAlignment.Center:
							return ContentAlignment.TopCenter;

						case StringAlignment.Far:
							return ContentAlignment.TopRight;
					}
					break;

				case StringAlignment.Center:
					switch (horizontal)
					{
						case StringAlignment.Near:
							return ContentAlignment.MiddleLeft;

						case StringAlignment.Center:
							return ContentAlignment.MiddleCenter;

						case StringAlignment.Far:
							return ContentAlignment.MiddleRight;
					}
					break;

				case StringAlignment.Far:
					switch (horizontal)
					{
						case StringAlignment.Near:
							return ContentAlignment.BottomLeft;

						case StringAlignment.Center:
							return ContentAlignment.BottomCenter;

						case StringAlignment.Far:
							return ContentAlignment.BottomRight;
					}
					break;
			}

			return default (ContentAlignment);
		}

		public static StringAlignment GetHorizontalAlignment (ContentAlignment alignment)
		{
			switch (alignment)
			{
				case ContentAlignment.TopLeft:
				case ContentAlignment.MiddleLeft:
				case ContentAlignment.BottomLeft:
					return StringAlignment.Near;

				case ContentAlignment.TopCenter:
				case ContentAlignment.MiddleCenter:
				case ContentAlignment.BottomCenter:
					return StringAlignment.Center;

				case ContentAlignment.TopRight:
				case ContentAlignment.MiddleRight:
				case ContentAlignment.BottomRight:
					return StringAlignment.Far;
			}

			return default (StringAlignment);
		}

		public static StringAlignment GetVerticalAlignment (ContentAlignment alignment)
		{
			switch (alignment)
			{
				case ContentAlignment.TopLeft:
				case ContentAlignment.TopCenter:
				case ContentAlignment.TopRight:
					return StringAlignment.Near;

				case ContentAlignment.MiddleLeft:
				case ContentAlignment.MiddleCenter:
				case ContentAlignment.MiddleRight:
					return StringAlignment.Center;

				case ContentAlignment.BottomLeft:
				case ContentAlignment.BottomCenter:
				case ContentAlignment.BottomRight:
					return StringAlignment.Far;
			}

			return default (StringAlignment);
		}

		public static HorizontalAlignment GetHorizontalAlignmentFromStringAlignment (StringAlignment stringAlignment)
		{
			switch (stringAlignment)
			{
				case StringAlignment.Near:
					return HorizontalAlignment.Left;

				case StringAlignment.Center:
					return HorizontalAlignment.Center;

				case StringAlignment.Far:
					return HorizontalAlignment.Right;
			}

			return default (HorizontalAlignment);
		}

		public static StringAlignment GetStringAlignmentFromHorizontalAlignment (HorizontalAlignment horizontalAlignment)
		{
			switch (horizontalAlignment)
			{
				case HorizontalAlignment.Left:
					return StringAlignment.Near;

				case HorizontalAlignment.Center:
					return StringAlignment.Center;

				case HorizontalAlignment.Right:
					return StringAlignment.Far;
			}

			return default (StringAlignment);
		}

		public static VerticalAlignment GetVerticalAlignmentFromStringAlignment (StringAlignment stringAlignment)
		{
			switch (stringAlignment)
			{
				case StringAlignment.Near:
					return VerticalAlignment.Top;

				case StringAlignment.Center:
					return VerticalAlignment.Middle;

				case StringAlignment.Far:
					return VerticalAlignment.Bottom;
			}

			return default (VerticalAlignment);
		}

		public static StringAlignment GetStringAlignmentFromVerticalAlignment (VerticalAlignment verticalAlignment)
		{
			switch (verticalAlignment)
			{
				case VerticalAlignment.Top:
					return StringAlignment.Near;

				case VerticalAlignment.Middle:
					return StringAlignment.Center;

				case VerticalAlignment.Bottom:
					return StringAlignment.Far;
			}

			return default (StringAlignment);
		}

		public static StringAlignment ReverseAlignment (StringAlignment alignment)
		{
			switch (alignment)
			{
				case StringAlignment.Center:
					return StringAlignment.Center;

				case StringAlignment.Far:
					return StringAlignment.Near;

				case StringAlignment.Near:
					return StringAlignment.Far;
			}

			return default(StringAlignment);
		}
	}
}