using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace UI
{
	public enum SkinnedButtonState
	{
		Normal,
		Active,
		Hover,
		Disabled,
		Focussed
	}

	public class SkinnedButton : DoubleBufferedPanel
	{
		protected bool active = false;
		public bool Active
		{
			get
			{
				return active;
			}

			set
			{
				active = value;
				Invalidate();
			}
		}

		public override string Text
		{
			get
			{
				return base.Text;
			}

			set
			{
				base.Text = value;
				Invalidate();
			}
		}

		protected bool mouseHovering = false;
		protected bool mouseDown = false;

		public SkinnedButtonState State
		{
			get
			{
				if (! Enabled)
				{
					return SkinnedButtonState.Disabled;
				}
				else if (mouseHovering && mouseDown)
				{
					return SkinnedButtonState.Active;
				}
				else if (mouseHovering)
				{
					return SkinnedButtonState.Hover;
				}
				else if (Active)
				{
					return SkinnedButtonState.Active;
				}
				else if (Focused)
				{
					return SkinnedButtonState.Focussed;
				}
				else
				{
					return SkinnedButtonState.Normal;
				}
			}
		}

		public SkinnedButton ()
		{
			SetStyle(ControlStyles.Selectable, true);
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);

			BackColor = Color.Transparent;

			MouseEnter += new EventHandler (SkinnedButton_MouseEnter);
			MouseLeave += new EventHandler (SkinnedButton_MouseLeave);
			MouseDown += new MouseEventHandler (SkinnedButton_MouseDown);
			MouseUp += new MouseEventHandler (SkinnedButton_MouseUp);
			GotFocus += new EventHandler (SkinnedButton_GotFocus);
			LostFocus += new EventHandler (SkinnedButton_LostFocus);
			EnabledChanged += new EventHandler (SkinnedButton_EnabledChanged);
		}

		protected void SkinnedButton_MouseEnter (object sender, EventArgs e)
		{
			mouseHovering = true;
			Invalidate();
		}

		protected void SkinnedButton_MouseLeave (object sender, EventArgs e)
		{
			mouseHovering = false;
			Invalidate();
		}

		protected void SkinnedButton_MouseDown (object sender, MouseEventArgs e)
		{
			mouseDown = true;
			Invalidate();
		}

		protected void SkinnedButton_MouseUp (object sender, MouseEventArgs e)
		{
			mouseDown = false;
			Invalidate();
		}

		protected override void OnKeyPress (KeyPressEventArgs e)
		{
			if (Enabled)
			{
				if (e.KeyChar == 13)
				{
					OnClick(new EventArgs());
				}

				base.OnKeyPress(e);
			}
		}

		private void SkinnedButton_GotFocus (object sender, EventArgs e)
		{
			Invalidate();
		}

		private void SkinnedButton_LostFocus (object sender, EventArgs e)
		{
			Invalidate();
		}

		private void SkinnedButton_EnabledChanged (object sender, EventArgs e)
		{
			Invalidate();
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);
			Invalidate();
		}
	}
}