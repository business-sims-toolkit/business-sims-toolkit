using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CommonGUI;
using CoreUtils;

namespace GameDetails
{
	public class Field : Panel
	{
		Label label;
		Control value;
		Label error;

		bool optional;

		static float fontSize = 14;

		public event EventHandler ValueChanged;

		float fieldNameWidthFraction;

		public string Value
		{
			set
			{
				if (this.value is ComboBox)
				{
					((ComboBox) this.value).SelectedText = value;
				}
				else if (this.value is TextBox)
				{
					((TextBox) this.value).Text = value;
				}
				else
				{
					throw new Exception();
				}

				UpdateState();
			}

			get
			{
				if (value is ComboBox)
				{
					return (string) ((ComboBox) value).SelectedItem;
				}
				else if (value is TextBox)
				{
					return ((TextBox) value).Text;
				}
				else
				{
					throw new Exception();
				}
			}
		}

		public bool IsOptional
		{
			get => optional;

			set
			{
				optional = value;
				UpdateState();
			}
		}

		public bool IsPassword
		{
			get => (value as TextBox)?.UseSystemPasswordChar ?? false;

			set => ((TextBox) this.value).UseSystemPasswordChar = value;
		}

		Field (string name, Control value)
		{
			label = SkinningDefs.TheInstance.CreateLabel(name, fontSize);
			label.TextAlign = ContentAlignment.MiddleRight;

			this.value = value;

			error = SkinningDefs.TheInstance.CreateLabel("", fontSize);
			error.ForeColor = Color.Red;
			error.TextAlign = ContentAlignment.MiddleLeft;

			Controls.Add(label);
			Controls.Add(value);
			Controls.Add(error);

			fieldNameWidthFraction = 0.5f;

			DoSize();
			UpdateState();
		}

		public float FieldNameWidthFraction
		{
			get => fieldNameWidthFraction;

			set
			{
				fieldNameWidthFraction = value;
				DoSize();
			}
		}

		public static Field CreateTextField (string name)
		{
			var textBox = new TextBox { Font = SkinningDefs.TheInstance.GetFont(fontSize) };

			var field = new Field(name, textBox);

			textBox.TextChanged += field.value_ValueChanged;

			return field;
		}

		public static Field CreatePasswordField (string name)
		{
			var textBox = new TextBox { Font = SkinningDefs.TheInstance.GetFont(fontSize), UseSystemPasswordChar = true };

			var field = new Field(name, textBox);

			textBox.TextChanged += field.value_ValueChanged;

			return field;
		}

		public static Field CreateFilteredTextField (string name, TextBoxFilterType filterType)
		{
			var textBox = new FilteredTextBox(filterType) { Font = SkinningDefs.TheInstance.GetFont(fontSize) };

			var field = new Field(name, textBox);

			textBox.TextChanged += field.value_ValueChanged;

			return field;
		}

		public override Size GetPreferredSize (Size proposedSize)
		{
			return new Size(proposedSize.Width, value.PreferredSize.Height);
		}

		public static Field CreateComboBoxField (string name, IList<string> values, bool allowCustom = false)
		{
			var comboBox = new ComboBox { Font = SkinningDefs.TheInstance.GetFont(fontSize) };
			comboBox.Items.AddRange(values.ToArray());
			comboBox.DropDownStyle = (allowCustom ? ComboBoxStyle.DropDown : ComboBoxStyle.DropDownList);

			var field = new Field(name, comboBox);

			comboBox.SelectedValueChanged += field.value_ValueChanged;

			return field;
		}

		public void ChangeValues (IList<string> values, bool allowCustom = false)
		{
			var comboBox = value as ComboBox;
			if (comboBox == null)
			{
				throw new Exception();
			}

			comboBox.Items.Clear();
			comboBox.Items.AddRange(values.ToArray());
		}

		protected override void OnEnabledChanged (EventArgs e)
		{
			base.OnEnabledChanged(e);

			value.Enabled = Enabled;
		}

		void value_ValueChanged (object sender, EventArgs args)
		{
			OnValueChanged();
		}

		void OnValueChanged ()
		{
			UpdateState();

			ValueChanged?.Invoke(this, EventArgs.Empty);
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);
			DoSize();
		}

		void DoSize ()
		{
			int gap = 10;
			var errorWidth = Width / 10;
			error.Bounds = new Rectangle (Width - errorWidth, (Height - value.PreferredSize.Height) / 2, errorWidth, value.PreferredSize.Height);
			label.Bounds = new Rectangle (0, error.Top, (int) (Width * fieldNameWidthFraction), error.Height);
			value.Bounds = new Rectangle (label.Right + gap, error.Top, error.Left - label.Right - gap, error.Height);
		}

		void UpdateState ()
		{
			if (! optional)
			{
				if (string.IsNullOrEmpty(Value))
				{
					error.Text = "!";
				}
				else
				{
					error.Text = "";
				}
			}
			else
			{
				error.Text = "";
			}
		}

		public void SetError (string message)
		{
			error.Text = message ?? "";
		}

		protected override void OnGotFocus (EventArgs e)
		{
			base.OnGotFocus(e);

			value.Select();
			value.Focus();
		}
	}
}