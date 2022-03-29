using System;
using System.Windows.Forms;

using LibCore;

namespace GameDetails
{
	public abstract class GameDetailsSection : ExpandablePanel
	{
		public GameDetailsSection ()
		{
			Title = "Game Details";
		}

		public virtual void LoadData ()
		{
		}

		public virtual bool SaveData ()
		{
			return true;
		}

		public virtual bool ValidateFields (bool reportErrors = true)
		{
			return true;
		}

		public virtual void ShowSpecificParts (bool showGameSpecificParts)
		{
		}

		protected virtual void SetAutoSize ()
		{
			int y = 0;

			foreach (System.Windows.Forms.Control control in panel.Controls)
			{
				y = Math.Max(y, control.Bottom);
			}

			SetSize(500, y + 15);
		}

		public virtual string GetQuickStartGameTitle ()
		{
			DateTime now = DateTime.Now;
			return CONVERT.Format("Test {2:0000}-{3:00}-{4:00} {0:00}{1:00}",
								 now.Hour, now.Minute,
								 now.Year, now.Month, now.Day);
		}

		public virtual void SetIfEmpty (TextBox textBox, string value)
		{
			if (string.IsNullOrEmpty(textBox.Text))
			{
				textBox.Text = value;
			}
		}

		public virtual void SetIfEmpty (ComboBox comboBox, string value)
		{
			if (comboBox.SelectedIndex == -1)
			{
				comboBox.SelectedItem = value;
			}
		}

		public virtual void SetIfEmpty (ComboBox comboBox)
		{
			if (comboBox.SelectedIndex == -1)
			{
				comboBox.SelectedIndex = 0;
			}
		}

		public virtual void SetIfEmpty(RadioButton rb)
		{
			rb.Checked = true;
		}

		public event EventHandler Changed;

		protected void OnChanged ()
		{
			Changed?.Invoke(this, EventArgs.Empty);
		}

		public virtual void SetFocus ()
		{
			Select();
			Focus();
		}
	}
}