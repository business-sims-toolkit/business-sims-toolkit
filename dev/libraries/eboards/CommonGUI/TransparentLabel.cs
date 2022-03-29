using System;
using System.Windows.Forms;
using System.Drawing;

using LibCore;

namespace CommonGUI
{
	/// <summary>
	/// A version of the Label class that derives from VisualPanel, and hence can support graduated background fills.
	/// </summary>
	public class TransparentLabel : VisualPanel
	{
		ContentAlignment textAlign;

		public TransparentLabel ()
		{
			textAlign = ContentAlignment.MiddleCenter;
			BackColor = Color.Transparent;
		}

		protected override void OnPaint (PaintEventArgs e)
		{
			base.OnPaint(e);

			Rectangle bounds = new Rectangle (0, 0, Width, Height);

			if (BackColor != Color.Transparent)
			{
				using (Brush brush = new SolidBrush (BackColor))
				{
					e.Graphics.FillRectangle(brush, bounds);
				}
			}

			using (Brush brush = new SolidBrush (ForeColor))
			{
				e.Graphics.DrawString(Text, Font, brush, bounds,
									  new StringFormat
									  {
										  Alignment = Alignment.GetHorizontalAlignment(TextAlign),
										  LineAlignment = Alignment.GetVerticalAlignment(TextAlign)
									  });
			}
		}

		public ContentAlignment TextAlign
		{
			get
			{
				return textAlign;
			}

			set
			{
				textAlign = value;
				OnTextAlignChanged();
			}
		}

		void OnTextAlignChanged ()
		{
			TextAlignChanged?.Invoke(this, EventArgs.Empty);

			Invalidate();
		}

		public event EventHandler TextAlignChanged;
	}
}