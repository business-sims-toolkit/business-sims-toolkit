using System;

using System.Windows.Forms;
using System.Drawing;

namespace GameDetails
{
	public class OptionsDetails : GameDetailsSection
	{
		CheckBox checkBox;
		CheckBox flashBox;

		public OptionsDetails ()
		{
			Title = "Options";

			checkBox = new CheckBox ();
			checkBox.Text = "Animate main flash";
			checkBox.Checked = CoreUtils.NonPersistentGlobalOptions.AnimateMainGameFlash;
			checkBox.CheckedChanged += checkBox_CheckedChanged;
			checkBox.Location = new Point (10, 5);
			checkBox.Size = new Size (200, 30);
			panel.Controls.Add(checkBox);

			flashBox = new CheckBox ();
			flashBox.Text = "Show debug flash names";
			flashBox.Checked = CoreUtils.NonPersistentGlobalOptions.ShowDebugFlashNames;
			flashBox.CheckedChanged += flashBox_CheckedChanged;
			flashBox.Location = new Point (10, 40);
			flashBox.Size = new Size (200, 30);
			panel.Controls.Add(flashBox);
		}

		void checkBox_CheckedChanged (object sender, EventArgs e)
		{
			CoreUtils.NonPersistentGlobalOptions.AnimateMainGameFlash = checkBox.Checked;
		}

		void flashBox_CheckedChanged (object sender, EventArgs e)
		{
			CoreUtils.NonPersistentGlobalOptions.ShowDebugFlashNames = flashBox.Checked;
		}
	}
}