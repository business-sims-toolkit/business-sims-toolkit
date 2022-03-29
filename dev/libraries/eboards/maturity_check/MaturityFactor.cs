using System;
using System.Xml;

using System.Windows.Forms;
using System.Drawing;

using LibCore;
using CoreUtils;
using CommonGUI;

namespace maturity_check
{
	public class MaturityFactor : BasePanel
	{
		XmlNode factorNode;
		MaturityAspect parent;
		XmlNode dataNode;

		Font font;

		public string Title
		{
			get
			{
				return titleLabel.Text;
			}		
		}

		int weight;
		public int Weight
		{
			get
			{
				return weight;
			}

			set
			{
				weight = value;
				weight_box.Value = weight;
			}
		}

		public string Help
		{
			get
			{
				return helpLabel.Text;
			}
		}

		public bool Ticked
		{
			get
			{
				return checkBox.Checked;
			}

			set
			{
				checkBox.Checked = value;
			}
		}

		// Non-editable controls...
		Label titleLabel;

		CheckBox checkBox;

		Label helpLabel;
		// Editable controls...
		TextBox e_titleLabel;

		TextBox e_helpLabel;

		NumericUpDown weight_box;
		//private Button remove_factor;
		ImageButton remove_factor;

		public void SetEditable(bool e)
		{
			if(e)
			{
				e_titleLabel.Visible = true;
				e_helpLabel.Visible = true;
				weight_box.Visible = true;
				remove_factor.Visible = true;
				checkBox.Visible = false;
				titleLabel.Visible = false;
				helpLabel.Visible = false;
			}
			else
			{
				titleLabel.Visible = true;
				helpLabel.Visible = true;
				checkBox.Visible = true;
				remove_factor.Visible = false;
				e_titleLabel.Visible = false;
				e_helpLabel.Visible = false;
				weight_box.Visible = false;
			}
		}

		public MaturityFactor (MaturityAspect parent, XmlNode factorNode)
		{
			this.factorNode = factorNode;
			this.parent = parent;

			string title = null;
			int weight = 0;
			string help = null;
			foreach (XmlNode child in factorNode.ChildNodes)
			{
				switch (child.Name.ToLower())
				{
					case "aspect":
						title = child.InnerText;
						break;

					case "weight":
						weight = CONVERT.ParseInt(child.InnerText);
						break;

					case "guidance":
						help = child.InnerText;
						break;

					case "factor_data":
						dataNode = child;
						break;
				}
			}

			Setup(title, weight, help);
			Ticked = CONVERT.ParseBool(dataNode.InnerText, false);
		}

		public MaturityFactor (MaturityAspect parent, string title)
		{
			this.factorNode = null;
			this.parent = parent;
			Setup(title, 0, "");
		}

		protected void Setup (string title, int weight, string help)
		{
			this.BackColor = CoreUtils.SkinningDefs.TheInstance.GetColorData("maturity_editor_background_colour");

			font = ConstantSizeFont.NewFont("Verdana", 8);


			int textHeight = 0;

			titleLabel = new Label ();
			titleLabel.Size = new Size (400, 20);
			titleLabel.Location = new Point (40, 0);
			titleLabel.Font = font;
			titleLabel.Text = title;
			textHeight = Math.Max(textHeight, titleLabel.GetPreferredSize(new Size(400, 0)).Height);

			e_titleLabel = new TextBox ();
			e_titleLabel.Size = new Size (400, 20);
			e_titleLabel.Location = new Point (40, 0);
			e_titleLabel.Font = font;
			e_titleLabel.BackColor = Color.LightGray;
			e_titleLabel.Visible = false;
			e_titleLabel.Text = title;
			textHeight = Math.Max(textHeight, e_titleLabel.GetPreferredSize(new Size (400, 0)).Height);

			remove_factor = new ImageButton(0);
			remove_factor.Size = new Size(e_titleLabel.Height, e_titleLabel.Height);
			remove_factor.Location = new Point(40,0);
			remove_factor.SetToolTipText = "Remove Factor";
			remove_factor.SetVariants("\\images\\maturity_tool\\edit_delete");
			remove_factor.ButtonPressed +=remove_factor_ButtonPressed;
			this.Controls.Add(remove_factor);

			e_titleLabel.Size = new Size(400 - 5 - e_titleLabel.Height, e_titleLabel.Height);
			e_titleLabel.Left += (5 + e_titleLabel.Height);

			e_titleLabel.TextChanged += e_titleLabel_TextChanged;

			checkBox = new CheckBox ();
			checkBox.Size = new Size (12, 12);
			checkBox.Location = new Point (titleLabel.Right + 10 + 10, 2);
			checkBox.CheckedChanged += checkBox_CheckedChanged;

			weight_box = new NumericUpDown();
			weight_box.Visible = false;
			weight_box.Size = new Size (40, 20);
			weight_box.TextAlign = HorizontalAlignment.Center;
			weight_box.Location = new Point (titleLabel.Right + 10, 0);
			weight_box.Value = weight;
			weight_box.TextChanged += weight_box_TextChanged;
			weight_box.Minimum = 0;
			weight_box.Maximum = 10;

			helpLabel = new Label ();
			helpLabel.Location = new Point (490+10 /*checkBox.Right + 10*/, 0);
			helpLabel.Size = new Size (300/*800 - helpLabel.Left*/, 30);
			helpLabel.Font = font;
			helpLabel.Text = help;
			textHeight = Math.Max(textHeight, helpLabel.GetPreferredSize(new Size(300, 0)).Height);

			e_helpLabel = new TextBox ();
			e_helpLabel.Location = new Point (490+10 /*checkBox.Right + 10*/, 0);
			e_helpLabel.Size = new Size (290/*800 - helpLabel.Left*/, 20);
			e_helpLabel.Font = font;
			e_helpLabel.Visible = false;
			e_helpLabel.Text = help;
			e_helpLabel.TextChanged += e_helpLabel_TextChanged;
			textHeight = Math.Max(textHeight, e_helpLabel.GetPreferredSize(new Size(290, 0)).Height);

			titleLabel.Height = textHeight;
			e_titleLabel.Height = textHeight;
			helpLabel.Height = textHeight;
			e_helpLabel.Height = textHeight;

			this.Controls.Add(remove_factor);
			this.Controls.Add(weight_box);
			this.Controls.Add(e_titleLabel);
			this.Controls.Add(e_helpLabel);

			this.Controls.Add(titleLabel);
			this.Controls.Add(checkBox);
			this.Controls.Add(helpLabel);

			int l_height = titleLabel.Height;
			if(e_titleLabel.Height > l_height)
			{
				l_height = e_titleLabel.Height;
			}

			this.Size = new Size (1000, l_height+7);
		}

		public void SaveToXml(XmlDocument xdoc, XmlElement root)
		{
			XmlElement factor = XMLUtils.CreateElement(root, "factor");
			root.AppendChild( factor );
			//
			factor.AppendChild( XMLUtils.CreateElementString(factor, "aspect", titleLabel.Text) );
			factor.AppendChild( XMLUtils.CreateElementString(factor, "weight", CONVERT.ToStr(weight) ) );
			factor.AppendChild( XMLUtils.CreateElementString(factor, "guidance", helpLabel.Text) );
			if(Ticked)
			{
				factor.AppendChild( XMLUtils.CreateElementString(factor, "factor_data", "true") );
			}
			else
			{
				factor.AppendChild( XMLUtils.CreateElementString(factor, "factor_data", "false") );
			}
		}

		void checkBox_CheckedChanged (object sender, EventArgs e)
		{
			if (dataNode != null)
			{
				dataNode.InnerText = CONVERT.ToStr(checkBox.Checked);
			}
			parent.UpdateScoreFromChecks();
		}

		public void Enable (bool enable)
		{
			titleLabel.Enabled = enable;
			checkBox.Enabled = enable;
			helpLabel.Enabled = enable;

			checkBox.TabStop = enable && parent.Expanded;
		}

		public void EnableTabStop (bool enable)
		{
			checkBox.TabStop = enable;
		}

		void e_titleLabel_TextChanged(object sender, EventArgs e)
		{
			titleLabel.Text = e_titleLabel.Text;
		}

		void e_helpLabel_TextChanged(object sender, EventArgs e)
		{
			helpLabel.Text = e_helpLabel.Text;
		}

		void weight_box_TextChanged(object sender, EventArgs e)
		{
			weight = (int) weight_box.Value;
		}

		void remove_factor_Click(object sender, EventArgs e)
		{
			parent.RemoveFactor(this);
		}

		void remove_factor_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			parent.RemoveFactor(this);
		}


	}
}