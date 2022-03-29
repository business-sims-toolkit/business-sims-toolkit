using System;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace CommonGUI
{
	public enum TextBoxFilterType
	{
		Unfiltered,
		Nothing,
		Digits,
		DigitsAndSigns,
		Alphanumeric,
		Alphabetic,
		Custom
	}

	public class FilteredTextBox : TextBox
	{
		TextBoxFilterType filterType;
		public TextBoxFilterType FilterType
		{
			get
			{
				return filterType;
			}

			set
			{
				filterType = value;
			}
		}

		public delegate bool ValidateInputHandler (FilteredTextBox sender, KeyPressEventArgs e);
		public event ValidateInputHandler ValidateInput;

		public FilteredTextBox (TextBoxFilterType filterType)
		{
			FilterType = filterType;

			KeyPress += FilteredTextBox_KeyPress;
            TextChanged += OnTextChanged;
		}

		void OnTextChanged(object sender, EventArgs eventArgs)
	    {
	        switch (filterType)
	        {
	            case TextBoxFilterType.Digits:
	                Text = Regex.Replace(Text, @"[^0-9]", "");
	                break;
	        }
	    }

	    protected override void Dispose (bool disposing)
		{
			if (disposing)
			{
				KeyPress -= FilteredTextBox_KeyPress;
			}

			base.Dispose(disposing);
		}

		protected virtual void FilteredTextBox_KeyPress (object sender, KeyPressEventArgs e)
		{
			switch (filterType)
			{
				case TextBoxFilterType.Unfiltered:
					break;

				case TextBoxFilterType.Nothing:
					if (! Char.IsControl(e.KeyChar))
					{
						e.Handled = true;
					}
					break;

				case TextBoxFilterType.Digits:
					if (! (Char.IsControl(e.KeyChar) || Char.IsDigit(e.KeyChar)))
					{
						e.Handled = true;
					}
					break;

				case TextBoxFilterType.DigitsAndSigns:
					if (! (Char.IsControl(e.KeyChar) || Char.IsDigit(e.KeyChar)
					    || (e.KeyChar == '-') || (e.KeyChar == '+')))
					{
						e.Handled = true;
					}
					break;

				case TextBoxFilterType.Alphabetic:
					if (! (Char.IsControl(e.KeyChar) || Char.IsLetter(e.KeyChar)))
					{
						e.Handled = true;
					}
					break;

				case TextBoxFilterType.Alphanumeric:
					if (! (Char.IsControl(e.KeyChar) || Char.IsLetterOrDigit(e.KeyChar)))
					{
						e.Handled = true;
					}
					break;

				case TextBoxFilterType.Custom:
					e.Handled = ! OnValidateInput(e);
					break;
			}
		}

		protected virtual bool OnValidateInput (KeyPressEventArgs e)
		{
			if (ValidateInput != null)
			{
				return ValidateInput(this, e);
			}

			return false;
		}
	}
}