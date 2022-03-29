using System;
using System.Drawing;
using System.Windows.Forms;

namespace ResizingUi
{
	public class CascadedBackgroundPanel : Panel
	{
		Control parent;
		bool useCascadedBackground;
		bool customBackground;

		protected Control referenceControl;
		protected PointF referenceControlPoint;
		protected Image referenceImage;
		protected PointF referenceImagePoint;
		protected ZoomMode zoomMode;

		public CascadedBackgroundPanel ()
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.DoubleBuffer, true);
			SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
			SetStyle(ControlStyles.ResizeRedraw, true);

			ResizeRedraw = true;

			useCascadedBackground = true;
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing)
			{
				if (parent != null)
				{
					parent.Paint -= parent_Paint;
				}
			}

			base.Dispose(disposing);
		}

		public bool UseCascadedBackground
		{
			get => useCascadedBackground;

			set
			{
				useCascadedBackground = value;
				customBackground = false;

				Invalidate(new Rectangle(0, 0, Width, Height), true);
			}
		}

		public void UseCustomBackground (ZoomMode zoomMode, Control referenceControl, Image referenceImage, PointF referenceControlPoint, PointF referenceImagePoint)
		{
			this.zoomMode = zoomMode;
			this.referenceControl = referenceControl;
			this.referenceControlPoint = referenceControlPoint;
			this.referenceImage = referenceImage;
			this.referenceImagePoint = referenceImagePoint;

			useCascadedBackground = true;
			customBackground = true;

			Invalidate();
		}

		public void UseCustomBackground (PicturePanel picturePanel)
		{
			if (picturePanel != null)
			{
				zoomMode = picturePanel.ZoomMode;
				referenceControl = picturePanel;
				referenceControlPoint = picturePanel.WindowReferencePoint;
				referenceImage = picturePanel.Image;
				referenceImagePoint = picturePanel.ImageReferencePoint;

				useCascadedBackground = true;
				customBackground = true;
			}
			else
			{
				useCascadedBackground = false;
				customBackground = false;
			}

			Invalidate();
		}

		protected override void OnParentBackgroundImageChanged (EventArgs e)
		{
			base.OnParentBackgroundImageChanged(e);

			Invalidate(new Rectangle (0, 0, Width, Height), true);
		}

		protected override void OnParentChanged (EventArgs e)
		{
			base.OnParentChanged(e);

			if (parent != null)
			{
				parent.Paint -= parent_Paint;
			}

			parent = Parent;

			if (parent != null)
			{
				parent.Paint += parent_Paint;
			}

			Invalidate(new Rectangle(0, 0, Width, Height), true);
		}

		void parent_Paint (object sender, EventArgs args)
		{
			Invalidate(new Rectangle(0, 0, Width, Height), true);
		}

		protected override void OnResize (EventArgs eventargs)
		{
			base.OnResize(eventargs);

			Invalidate(new Rectangle (0, 0, Width, Height), true);
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);

			Invalidate(new Rectangle(0, 0, Width, Height), true);
		}

		protected override void OnLocationChanged (EventArgs e)
		{
			base.OnLocationChanged(e);

			Invalidate(new Rectangle(0, 0, Width, Height), true);
		}

		protected override void OnPaintBackground (PaintEventArgs e)
		{
			if (useCascadedBackground)
			{
				if (customBackground)
				{
					BackgroundPainter.Paint(this, e.Graphics,
						referenceControl, referenceImage, zoomMode,
						referenceImagePoint,
						new PointF(referenceControlPoint.X * referenceControl.ClientSize.Width,
							referenceControlPoint.Y * referenceControl.ClientSize.Height));
				}
				else
				{
					BackgroundPainter.Paint(this, e.Graphics);
				}
			}
			else
			{
				base.OnPaintBackground(e);
			}
		}

		protected override void OnPaint (PaintEventArgs e)
		{
			base.OnPaint(e);

			using (var brush = new SolidBrush (ForeColor))
			{
				e.Graphics.DrawString(Text, Font, brush, new RectangleF (0, 0, Width, Height), new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center });
			}
		}
	}
}