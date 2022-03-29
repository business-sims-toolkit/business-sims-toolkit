using System.Drawing;
using System.Windows.Forms;

namespace LibCore
{
	public class CustomMessageBoxItem
	{
		string text;
		DialogResult dialogResult;
		StringAlignment alignment;

		public CustomMessageBoxItem (string text)
			: this (text, DialogResult.None, StringAlignment.Center)
		{
		}

		public CustomMessageBoxItem (string text, DialogResult dialogResult)
			: this (text, dialogResult, StringAlignment.Center)
		{
		}

		public CustomMessageBoxItem (string text, StringAlignment alignment)
			: this (text, DialogResult.None, alignment)
		{
		}

		public CustomMessageBoxItem (string text, DialogResult dialogResult, StringAlignment alignment)
		{
			this.text = text;
			this.dialogResult = dialogResult;
			this.alignment = alignment;
		}

		public string Text
		{
			get
			{
				return text;
			}
		}

		public DialogResult DialogResult
		{
			get
			{
				return dialogResult;
			}
		}

		public StringAlignment Alignment
		{
			get
			{
				return alignment;
			}
		}
	}
}
