using System;
using System.Collections.Generic;
using System.Drawing;

using System.Windows.Forms;

namespace LibCore
{
	public class TextBoxFlasher
	{
		TextBox textBox;
		Timer timer;

		Color oldBackColour;
		Color flashColour;
		bool flashOn;
		int flashes;

		int maxFlashes;

		public TextBoxFlasher (TextBox textBox)
		{
			timer = new Timer ();
			timer.Interval = 500;
			timer.Tick += timer_Tick;

			this.textBox = textBox;
			textBox.TextChanged += textBox_TextChanged;

			flashColour = Color.Red;

			maxFlashes = 5;

			flashes = 0;
			flashOn = false;
			timer.Start();
		}

		void timer_Tick (object sender, EventArgs e)
		{
			if (flashOn)
			{
				flashOn = false;
				textBox.BackColor = oldBackColour;
				flashes++;
			}
			else
			{
				flashOn = true;
				oldBackColour = textBox.BackColor;
				textBox.BackColor = flashColour;
			}

			if (flashes >= maxFlashes)
			{
				StopFlashing();
			}
		}

		void textBox_TextChanged (object sender, EventArgs e)
		{
			StopFlashing();
		}

		void StopFlashing ()
		{
			timer.Stop();
			textBox.BackColor = oldBackColour;
			textBox.TextChanged -= textBox_TextChanged;
			textBox = null;
		}

		public static void FlashBoxes (ICollection<TextBox> boxes)
		{
			foreach (TextBox box in boxes)
			{
				new TextBoxFlasher (box);
			}
		}
	}
}