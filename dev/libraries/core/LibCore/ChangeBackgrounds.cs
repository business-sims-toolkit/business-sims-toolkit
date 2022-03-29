using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;

namespace LibCore
{
	public static class ChangeBackgrounds
	{
		static List<Control> GetAllChildren (Control control)
		{
			List<Control> controls = new List<Control> ();

			if (control != null)
			{
				controls.Add(control);

				foreach (Control child in control.Controls)
				{
					controls.AddRange(GetAllChildren(child));
				}
			}

			return controls;
		}

		public static void ChangeBackgroundRecursively (Control parent, Color colour)
		{
			foreach (Control control in GetAllChildren(parent))
			{
				if ((control is ComboBox) || (control is AxHost) || (control is TextBox) || (control is DividerPanel))
				{
					continue;
				}

				try
				{
					control.BackColor = colour;
				}
				catch (ArgumentException)
				{
				}
			}
		}

		public static void ChangeBackgroundRecursively (Control parent, Image image)
		{
			foreach (Control control in GetAllChildren(parent))
			{
				control.BackgroundImage = image;
				control.BackgroundImageLayout = ImageLayout.Stretch;
			}
		}
	}
}