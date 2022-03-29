using System;
using System.Collections;
using System.Windows.Forms;

namespace CommonGUI
{
	/// <summary>
	/// Summary description for EntryBox.
	/// </summary>
	public class EntryBox : TextBox, iTabPressed
	{
		public bool DigitsOnly = false;
		public Control NextControl;
		public Control PrevControl;
		public int numChars = -1;

		protected Hashtable charToAlternate = new Hashtable();
		
		protected Hashtable charToIgnore = new Hashtable();

		public void CharIsShortFor(char character, char equivalentCharacter)
		{
			charToAlternate[character] = equivalentCharacter;
		}

		public void CharToIgnore(char character)
		{
			charToIgnore[character] = "";
		}

		public void CharNotToIgnore (char character)
		{
			charToIgnore.Remove(character);
		}

		public EntryBox()
		{
			KeyPress += EntryBox_KeyPress;
			KeyDown += EntryBox_KeyDown;
			KeyUp += EntryBox_KeyUp;
			GotFocus += EntryBox_GotFocus;
		}

		void EntryBox_KeyPress(object sender, KeyPressEventArgs e)
		{
			if(charToIgnore.ContainsKey(e.KeyChar))
			{
				//if its not, tell system we are dealing with it but just ignore it
				//this ensures only digits are allowed
				e.Handled = true;
				return;
			}
			
			// Do we have any character mappings?
			if(charToAlternate.ContainsKey(e.KeyChar))
			{
				e.Handled = true;
				string text = Text;
				string newText = text.Substring(0,SelectionStart);
				newText += (char) charToAlternate[e.KeyChar];
				newText += text.Substring(SelectionStart+SelectionLength);
				Text = newText;
				//
				SelectionLength = 0;
				SelectionStart = SelectionStart+1;
			}

			if(DigitsOnly)
			{
				if (char.IsDigit(e.KeyChar) || e.KeyChar == 8)
				{
					//if its a digit, allow
					e.Handled = false;
				}
				else
				{
					//if its not, tell system we are dealing with it but just ignore it
					//this ensures only digits are allowed
					e.Handled = true;
					return;
				}
			}

			// Do we have the number of chars we want?
			if(e.KeyChar == 8)
			{
				// Let this through as it means backspace (delete).
				if(SelectionStart == 0)
				{
					// Our cursor is at the start so jump back to the previous control
					// and do a delete there...
					if(null != PrevControl)
					{
						TextBox textBox = PrevControl as TextBox;

						if(textBox != null)
						{
							string text = textBox.Text;
							if(text.Length > 0)
							{
								textBox.Text = text.Substring(0,text.Length-1);
								textBox.SelectionLength = 0;
								textBox.SelectionStart = textBox.Text.Length;
							}
						}
						//
						PrevControl.Focus();
					}
				}
			}
			else if(numChars >= 0)
			{
				if(Text.Length == numChars && SelectionLength == 0)
				{
					// Don't allow them to enter the char
					e.Handled = true;
				}
			}
		}

		void EntryBox_KeyDown(object sender, KeyEventArgs e)
		{
			if(e.KeyCode == Keys.Left)
			{
				if(SelectionStart == 0)
				{
					if(PrevControl != null)
					{
						TextBox textBox = PrevControl as TextBox;
						if(textBox != null)
						{
							textBox.SelectionLength = 0;
							textBox.SelectionStart = textBox.Text.Length;
						}
						//
						PrevControl.Focus();
					}
				}
			}
			else if(e.KeyCode == Keys.Right)
			{
				if(SelectionStart == Text.Length)
				{
					if(NextControl != null)
					{
						TextBox textBox = NextControl as TextBox;
						if(textBox != null)
						{
							textBox.SelectionLength = 0;
							textBox.SelectionStart = 0;
						}

						NextControl.Focus();
					}
				}
			}
		}

		public void SelectAllText()
		{
			SelectionStart = 0;
			SelectionLength = Text.Length;
		}

		protected override bool ProcessDialogKey(Keys keyData)
		{
			// If we are a tab....
		if(keyData == Keys.Tab)
			{
				if(null != tabPressed)
				{
					tabPressed(this);
					return true;
				}
			    if(NextControl != null)
				{
					NextControl.Focus();
					return true;
				}

			}


		return base.ProcessDialogKey(keyData);
		}

		void EntryBox_KeyUp(object sender, KeyEventArgs e)
		{
			if(Text.Length == numChars && 
				((e.KeyData != Keys.Delete) || (e.KeyData != Keys.Back)) &&
				SelectionLength == 0)
			{
				if(NextControl != null)
					NextControl.Focus();
			}
		}
		#region iTabPressed Members

		public event cTabPressed.TabEventArgsHandler tabPressed;

		#endregion

		void EntryBox_GotFocus(object sender, EventArgs e)
		{
			SelectAll();
		}
	}
}
