using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;

namespace LibCore
{
	public class PanelLabeller : IDisposable
	{
		Control control;

		Form popup;
		string labelText;

		Timer closeTimer;
		Timer redrawTimer;

		int colour;

		bool manuallyClosed;

		static bool enabled;
		public static bool Enabled
		{
			get
			{
				return enabled;
			}

			set
			{
				enabled = value;
			}
		}

		static Dictionary<Control, PanelLabeller> controlToLabeller;

		static PanelLabeller ()
		{
			Enabled = false;
			controlToLabeller = new Dictionary<Control, PanelLabeller> ();
		}

		public PanelLabeller (Control control)
		{
			this.control = control;

			popup = new Form ();
			popup.FormBorderStyle = FormBorderStyle.None;
			popup.ShowInTaskbar = false;
			popup.Opacity = 0.5f;
			popup.MouseEnter += popup_MouseEnter;
			popup.MouseLeave += popup_MouseLeave;
			popup.Paint += popup_Paint;
			popup.MouseDown += popup_MouseDown;

			closeTimer = new Timer ();
			closeTimer.Interval = 100;
			closeTimer.Tick += closeTimer_Tick;

			redrawTimer = new Timer ();
			redrawTimer.Interval = 500;
			redrawTimer.Tick += redrawTimer_Tick;
			redrawTimer.Start();

			control.MouseEnter += control_MouseEnter;
			control.MouseLeave += control_MouseLeave;
		}

		void popup_MouseDown (object sender, MouseEventArgs e)
		{
			closeTimer.Interval = 100;
			closeTimer.Start();
			manuallyClosed = true;
		}

		void popup_Paint (object sender, PaintEventArgs e)
		{
			Point controlTopLeft = popup.PointToClient(control.PointToScreen(new Point (0, 0)));
			e.Graphics.DrawRectangle(Pens.Black, controlTopLeft.X, controlTopLeft.Y, control.Width, control.Height);

			Color [] colours = new Color [] { Color.Black, Color.Red, Color.Orange, Color.Yellow, Color.Green, Color.Cyan, Color.Blue, Color.Magenta, Color.White };

			using (Brush brush = new SolidBrush (colours[colour % colours.Length]))
			{
				Rectangle rectangle = new Rectangle (0, 0, popup.Width, popup.Height);
				StringFormat format = new StringFormat ();
				format.Alignment = StringAlignment.Center;
				format.LineAlignment = StringAlignment.Center;
				e.Graphics.DrawString(labelText, Control.DefaultFont, brush, rectangle, format);
			}
		}

		void redrawTimer_Tick (object sender, EventArgs e)
		{
			Redraw();
		}

		void popup_MouseLeave (object sender, EventArgs e)
		{
			closeTimer.Interval = 100;
			closeTimer.Start();
		}

		void popup_MouseEnter (object sender, EventArgs e)
		{
			closeTimer.Stop();
		}

		void closeTimer_Tick (object sender, EventArgs e)
		{
			closeTimer.Stop();
			popup.Hide();
		}

		public void Dispose ()
		{
			popup.Dispose();
			if (closeTimer != null)
			{
				closeTimer.Enabled = false;
				closeTimer.Dispose();
			}
			if (redrawTimer != null)
			{
				redrawTimer.Enabled = false;
				redrawTimer.Dispose();
			}
		}

		void Redraw ()
		{
			if (popup.Visible)
			{
				int textWidth;
				using (Graphics graphics = popup.CreateGraphics())
				{
					textWidth = 10 + (int) graphics.MeasureString(labelText, Control.DefaultFont).Width;
				}
				popup.Size = new Size (Math.Max(textWidth, control.Width), control.Height);
				popup.Location = control.PointToScreen(new Point ((control.Width - popup.Width) / 2, 0));

				colour++;

				popup.Invalidate();
			}
		}

		void control_MouseEnter (object sender, EventArgs e)
		{
			if (enabled && ! manuallyClosed)
			{
				popup.Show();
				closeTimer.Stop();

				labelText = control.GetType().ToString();

				Redraw();

				closeTimer.Interval = 2000;
				closeTimer.Start();
			}
		}

		void control_MouseLeave (object sender, EventArgs e)
		{
			closeTimer.Interval = 100;
			closeTimer.Start();
			manuallyClosed = false;
		}

		public static void LabelControl (Control control)
		{
			if (! controlToLabeller.ContainsKey(control))
			{
				PanelLabeller labeller = new PanelLabeller (control);
				control.Disposed += control_Disposed;
				controlToLabeller.Add(control, labeller);
			}
		}

		static void control_Disposed (object sender, EventArgs e)
		{
			Control control = (Control) sender;
			controlToLabeller[control].Dispose();
			controlToLabeller.Remove(control);
		}
	}
}