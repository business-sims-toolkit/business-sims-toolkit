using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using LibCore;

namespace CommonGUI
{
	public class EntryPanelStage : FlickerFreePanel
	{
		Label title;
		List<Label> labels;
		List<Control> fields;
		Dictionary<Control, int> fieldToLines;

		int labelWidth;

		public EntryPanelStage (string title)
		{
			this.title = new Label ();
			this.title.Text = title;
			Controls.Add(this.title);

			labels = new List<Label> ();
			fields = new List<Control> ();
			fieldToLines = new Dictionary<Control, int> ();

			labelWidth = 100;

			DoSize();
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);

			DoSize();
		}

		public int LabelWidth
		{
			get
			{
				return labelWidth;
			}

			set
			{
				labelWidth = value;
				DoSize();
			}
		}

		void DoSize ()
		{
			title.Location = new Point (0, 0);
			title.Size = new Size (Width, 25);

			int margin = 20;
			int gap = 20;
			int gutter = 10;
			int itemHeight = 25;
			int y = title.Bottom + gutter;

			for (int i = 0; i < Math.Min(labels.Count, fields.Count); i++)
			{
				Label label = labels[i];
				Control field = fields[i];

				int height = itemHeight;
				if (fieldToLines.ContainsKey(field))
				{
					height = (itemHeight * fieldToLines[field]) + (gutter * (fieldToLines[field] - 1));
				}

				label.Location = new Point (margin, y);
				label.Size = new Size (labelWidth - label.Left, height);

				field.Location = new Point (label.Right + gap, label.Top);
				field.Size = new Size (Width - margin - field.Left, height);

				y = label.Bottom + gutter;
			}
		}

		public void AddTextField (string attributeName, string labelText)
		{
			AddTextField(attributeName, labelText, 1);
		}

		public void AddTextField (string attributeName, string labelText, int rows)
		{
			Label label = new Label ();
			label.Text = labelText;
			labels.Add(label);
			Controls.Add(label);

			TextBox textBox = new TextBox ();
			textBox.Tag = attributeName;
			textBox.Multiline = (rows > 1);
			fields.Add(textBox);
			Controls.Add(textBox);

			fieldToLines.Add(textBox, rows);

			DoSize();
		}

		public void AddRadioButtonField (string attributeName, string labelText, KeyValuePair<string, string> [] radioLabelToAttributeValue)
		{
			Label label = new Label ();
			label.Text = labelText;
			labels.Add(label);
			Controls.Add(label);

			ComboBoxRow.ImageTextButtonCreator creator = new ComboBoxRow.ImageTextButtonCreator (Color.White, Color.Red, Color.Green, Color.Gray, "blank_med.png");
			ComboBoxRow row = new ComboBoxRow (creator);
			row.Tag = attributeName;
			fields.Add(row);
			Controls.Add(row);

			foreach (KeyValuePair<string, string> labelAndAttributeValue in radioLabelToAttributeValue)
			{
				row.Items.Add(new ComboBoxOption (labelAndAttributeValue.Key, labelAndAttributeValue.Value));
			}

			DoSize();
		}

		public Dictionary<string, string> GetData ()
		{
			Dictionary<string, string> attributeNameToValue = new Dictionary<string, string> ();

			foreach (Control field in fields)
			{
				string value = "";
				if (field is TextBox)
				{
					value = field.Text;
				}
				else if (field is ComboBoxRow)
				{
					value = (string) (((ComboBoxRow) field).SelectedItem).Tag;
				}

				attributeNameToValue.Add((string) field.Tag, value);
			}

			return attributeNameToValue;
		}
	}
}