using System;
using System.Windows.Forms;
using System.Drawing;

using LibCore;
using CoreUtils;
using GameManagement;
using Network;

namespace CommonGUI
{
	public class FacilitatorOptionsScreen : Panel
	{
		class ScreenChoice
		{
			Screen screen;
			public Screen Screen
			{
				get
				{
					return screen;
				}
			}

			string name;

			public ScreenChoice (Screen screen)
			{
				this.screen = screen;
				name = screen.DeviceName;
			}

			public override string ToString ()
			{
				return name;
			}
		}

		Form gameForm;
		NetworkProgressionGameFile gameFile;

		public FacilitatorOptionsScreen (Control control, NetworkProgressionGameFile gameFile)
		{
			gameForm = control.Parent as Form;
			this.gameFile = gameFile;

			int y = 0;

			if (SkinningDefs.TheInstance.GetIntData("options_show_display", 0) == 1)
			{
				Label label = new Label ();
				label.Location = new Point (10, y + 20);
				label.Text = "Display:";
				label.Size = new Size (100, label.Height);
				label.TextAlign = ContentAlignment.MiddleRight;

				ListBox box = new ListBox ();
				box.Location = new Point (label.Right + 20, label.Top);
				box.Size = new Size (300, 100);
				foreach (Screen screen in Screen.AllScreens)
				{
					if ((screen.Bounds.Width >= gameForm.Width) && (screen.Bounds.Height >= gameForm.Height))
					{
						ScreenChoice choice = new ScreenChoice (screen);
						box.Items.Add(choice);

						if (screen.Bounds.IntersectsWith(gameForm.Bounds))
						{
							box.SelectedIndex = box.Items.IndexOf(choice);
						}
					}
				}
				box.SelectedIndexChanged += box_SelectedIndexChanged;
				if (box.Items.Count <= 1)
				{
					box.Enabled = false;
				}

				Controls.Add(label);
				Controls.Add(box);

				y = box.Bottom;
			}

			if (SkinningDefs.TheInstance.GetIntData("options_show_celsius", 0) == 1)
			{
				bool showCelsius = 	gameFile.NetworkModel.GetNamedNode("TemperatureOptions").GetBooleanAttribute("show_in_celsius", false);

				GroupBox box = new GroupBox ();
				box.Location = new Point (130, y + 20);
				box.Text = "Temperatures:";
				box.Size = new Size (300, 100);

				RadioButton fahrenheit = new RadioButton ();
				fahrenheit.Location = new Point (10, 20);
				fahrenheit.Text = "Fahrenheit";
				fahrenheit.Size = new Size (150, fahrenheit.Height);
				fahrenheit.Checked = ! showCelsius;

				RadioButton celsius = new RadioButton ();
				celsius.Location = new Point (10, 40);
				celsius.Text = "Celsius";
				celsius.Size = new Size (150, celsius.Height);
				celsius.Checked = showCelsius;

				fahrenheit.CheckedChanged += fahrenheit_CheckedChanged;
				celsius.CheckedChanged += celsius_CheckedChanged;

				box.Controls.Add(fahrenheit);
				box.Controls.Add(celsius);
				Controls.Add(box);

				y = box.Bottom;
			}

			Height = y + 20;
			AutoScroll = true;
		}

		void box_SelectedIndexChanged (object sender, EventArgs e)
		{
			ListBox box = sender as ListBox;
			ScreenChoice choice = box.SelectedItem as ScreenChoice;

			SetScreen(choice.Screen);
		}

		void SetScreen (Screen screen)
		{
			gameForm.Location = new Point (screen.Bounds.Left, screen.Bounds.Top);
		}

		void fahrenheit_CheckedChanged (object sender, EventArgs e)
		{
			RadioButton button = sender as RadioButton;

			if (button.Checked)
			{
				Node node = gameFile.NetworkModel.GetNamedNode("TemperatureOptions");
				node.SetAttribute("show_in_celsius", CONVERT.ToStr(false));
			}
		}

		void celsius_CheckedChanged (object sender, EventArgs e)
		{
			RadioButton button = sender as RadioButton;

			if (button.Checked)
			{
				Node node = gameFile.NetworkModel.GetNamedNode("TemperatureOptions");
				node.SetAttribute("show_in_celsius", CONVERT.ToStr(true));
			}
		}
	}
}