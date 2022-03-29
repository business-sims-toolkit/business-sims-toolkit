using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace CommonGUI
{
	/// <summary>
	/// A FocusJumper explicitly manages the focus shift across a range of controls.
	/// This is done as the standard MS Windows Focus seems to have a mind of its own!
	/// </summary>
	public class FocusJumper
	{
		List<Control> controls;
		Control lastFocussedControl;

		public FocusJumper ()
		{
			controls = new List<Control> ();
			lastFocussedControl = null;
		}

		public void Add (Control c)
		{
			if (! controls.Contains(c))
			{
				controls.Add(c);

				c.ParentChanged += c_ParentChanged;
				c.Disposed += c_Disposed;

				iTabPressed itb = c as iTabPressed;
				if (itb != null)
				{
					itb.tabPressed += itb_tabPressed;
				}
				else
				{
					c.PreviewKeyDown += c_PreviewKeyDown;
					c.KeyDown += c_KeyDown;
				}
				c.EnabledChanged += c_EnabledChanged;
				c.GotFocus += c_GotFocus;

				if (c.Focused)
				{
					lastFocussedControl = c;
				}
			}
		}

		void c_GotFocus (object sender, EventArgs e)
		{
			lastFocussedControl = (Control) sender;
		}

		void c_EnabledChanged (object sender, EventArgs e)
		{
			Control control = (Control) sender;

			if ((! control.Enabled) && (control == lastFocussedControl))
			{
				Tab(control, 1);
			}
		}

		void c_PreviewKeyDown (object sender, PreviewKeyDownEventArgs e)
		{
			if (e.KeyCode == Keys.Tab)
			{
				e.IsInputKey = true;
			}
		}

		void c_KeyDown (object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Tab)
			{
				Tab((Control) sender, e.Shift ? -1 : 1);
				e.Handled = true;
			}
		}

		void c_ParentChanged (object sender, EventArgs e)
		{
			Remove((Control) sender);
		}

		void c_Disposed (object sender, EventArgs e)
		{
			Remove((Control) sender);
		}

		void itb_tabPressed (Control sender)
		{
			Tab(sender, 1);
		}

		public void Dispose ()
		{
			RemoveAll();
		}

		public void RemoveAll ()
		{
			foreach (Control c in new List<Control> (controls))
			{
				Remove(c);
			}
		}

		public void Remove (Control c)
		{
			if (controls.Contains(c))
			{
				c.ParentChanged -= c_ParentChanged;
				c.Disposed -= c_Disposed;
				c.PreviewKeyDown -= c_PreviewKeyDown;
				c.KeyDown -= c_KeyDown;
				c.EnabledChanged -= c_EnabledChanged;
				c.GotFocus -= c_GotFocus;

				iTabPressed itb = c as iTabPressed;
				if (itb != null)
				{
					itb.tabPressed -= itb_tabPressed;
				}

				controls.Remove(c);
			}
		}

	    void Tab (Control control, int d)
		{
			int oldIndex = controls.IndexOf(control);
			int index = oldIndex;

			if (index != -1)
			{
				do
				{
					while (index < 0)
					{
						index += controls.Count;
					}

					index = (index + d) % controls.Count;

					Control newControl = controls[index];

					if (newControl.Enabled && newControl.Visible)
					{
						newControl.Select();
						newControl.Focus();
						break;
					}
				}
				while (index != oldIndex);
			}
		}

	    public void Clear ()
	    {
            controls.Clear();
	        lastFocussedControl = null;
	    }
	}
}