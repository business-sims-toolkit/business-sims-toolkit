using System;
using System.Windows.Forms;
using System.Drawing;

namespace BaseUtils
{
	/// <summary>
	/// A customized implementation of Button that
	/// allows images to be used for each button state.
	/// </summary>
	public class ImageButtonControl : Button
	{
		bool mouseDown;
		bool drawFocusRect;
		ImageButtonType buttonType;
		bool toggleButton;
		bool toggled;

		/// <summary>
		/// Creates an instance of ImageButtonControl.
		/// </summary>
		public ImageButtonControl() : base()
		{
			this.SetStyle(ControlStyles.DoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.SupportsTransparentBackColor | ControlStyles.UserPaint, true);
			// This is the crucial style for transparrent buttons!
			this.SetStyle(ControlStyles.Opaque, false);

			this.BackColor = Color.Transparent;
			this.buttonType = ImageButtonType.ArrowLeft;
			this.Size = new Size(28, 28);
			this.drawFocusRect = true;
		}

		/// <summary>
		/// Toggle the mouseDown and toggled flags.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnMouseDown(MouseEventArgs e)
		{
			if (!mouseDown)
			{
				if (toggleButton)
					toggled = !toggled;

				mouseDown = true;
				Invalidate(ClientRectangle);
			}

			base.OnMouseDown (e);
		}

		/// <summary>
		/// Toggle the mouseDown flag.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnMouseUp(MouseEventArgs e)
		{
			if (mouseDown)
			{
				mouseDown = false;
				Invalidate(ClientRectangle);
			}

			base.OnMouseUp (e);
		}

		/// <summary>
		/// Redraw the button.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnMouseEnter(EventArgs e)
		{
			Invalidate(ClientRectangle);
			base.OnMouseEnter (e);
		}

		/// <summary>
		/// Redraw the button.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnMouseLeave(EventArgs e)
		{
			Invalidate(ClientRectangle);
			base.OnMouseLeave (e);
		}

		/// <summary>
		/// Redraw the button.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnEnter(EventArgs e)
		{
			Invalidate(ClientRectangle);
			base.OnEnter (e);
		}

		/// <summary>
		/// Redraw the button.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnLeave(EventArgs e)
		{
			Invalidate(ClientRectangle);
			base.OnLeave (e);
		}

		/// <summary>
		/// Uses the ImageButtonFactory to draw the appropriate image
		/// given the current state of the button.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnPaint(PaintEventArgs e)
		{
			ImageButtonFactory.Instance().DrawButton(e.Graphics, buttonType, mouseDown, base.Enabled, toggled);

			if (drawFocusRect && base.Focused)
				ControlPaint.DrawFocusRectangle(e.Graphics, ClientRectangle, Color.White, Color.Black);
		}

		/// <summary>
		/// Toggle the mouseDown flag.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Space)
				mouseDown = true;

			base.OnKeyDown (e);
		}

		/// <summary>
		/// Toggle the mouseDown flag.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnKeyUp(KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Space)
				mouseDown = false;

			base.OnKeyUp (e);
		}

		/// <summary>
		/// Get or Set the ImageButtonType.
		/// </summary>
		public ImageButtonType ButtonType
		{
			get { return buttonType; }
			set 
			{ 
				buttonType = value; 
				toggleButton = ImageButtonFactory.Instance().IsToggleButton(buttonType);
				toggled = false;
				Invalidate();
			}
		}

		/// <summary>
		/// Get or Set the toggled state.
		/// </summary>
		public bool Toggled
		{
			get { return toggled; }
			set
			{
				toggled = value;
				Invalidate();
			}
		}

		/// <summary>
		/// Get or Set whether to draw a focus
		/// rectangle round the button.
		/// </summary>
		public bool DrawFocusRect
		{
			get { return drawFocusRect; }
			set { drawFocusRect = value; }
		}
	}
}
