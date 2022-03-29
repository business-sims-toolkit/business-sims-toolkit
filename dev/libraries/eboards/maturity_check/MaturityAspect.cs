using System;
using System.Collections;
using System.Xml;

using System.Windows.Forms;
using System.Drawing;

using LibCore;
using CoreUtils;
using CommonGUI;


namespace maturity_check
{
	public class MaturityAspect : BasePanel
	{
		public event MaturityCardChangedHandler Changed;

		void OnChanged ()
		{
			if (Changed != null)
			{
				Changed(this);
			}
		}

		XmlNode aspectNode;
		MaturitySection parent;
		XmlNode dataNode;

		Font font;

		public string Title
		{
			get
			{
				return titleLabel.Text;
			}
		}

		public ArrayList factors;

		string tagName;

		protected string dest_tag_name = "";

		public int Score
		{
			get
			{
				return CONVERT.ParseIntSafe(scoreBox.Text, 0);
			}

			set
			{
				scoreBox.Text = CONVERT.ToStr(value);
			}
		}

		bool expanded;
		public bool Expanded
		{
			get
			{
				return expanded;
			}

			set
			{
				expanded = value;
				expandButton.State = (expanded ? 1 : 0);

				foreach (MaturityFactor factor in factors)
				{
					factor.EnableTabStop(expanded);
					factor.Visible = expanded;
				}

				UpdateLayout();
			}
		}

		// Non-editable fields
		CheckBox checkBox;

		ImageToggleButton expandButton;

		Label titleLabel;
		//private TextBox scoreBox;
		NumericUpDown scoreBox;

		Label helpLabel;
		// Editable fields
		TextBox e_titleLabel;

		TextBox e_helpLabel;

		Button add_factor;
		//private Button remove_aspect;
		ImageButton remove_aspect;

		bool is_editable = false;

		public void DisableRemove(bool e)
		{
			remove_aspect.Enabled = !e;
		}

		public void SetEditable(bool e)
		{
			is_editable = e;

			if(e)
			{
				e_titleLabel.Visible = true;
				e_helpLabel.Visible = true;
				remove_aspect.Visible = true;
				titleLabel.Visible = false;
				helpLabel.Visible = false;
			}
			else
			{
				add_factor.Visible = false;
				titleLabel.Visible = true;
				helpLabel.Visible = true;
				remove_aspect.Visible = false;
				e_titleLabel.Visible = false;
				e_helpLabel.Visible = false;
			}

			foreach(MaturityFactor factor in factors)
			{
				factor.SetEditable(e);
			}

			if(e)
			{
				add_factor.Visible = true;
				// Always expand if we have factor and are editable.
				if(factors.Count > 0)
				{
					Expanded = true;
				}
			}

			UpdateLayout();
		}

		public MaturityAspect (MaturitySection parent, XmlNode aspectNode)
		{
			this.parent = parent;
			this.aspectNode = aspectNode;
			Setup();

			int score = 0;

			foreach (XmlNode child in aspectNode.ChildNodes)
			{
				switch (child.Name.ToLower())
				{
					case "aspect_name":
						titleLabel.Text = child.InnerText;
						e_titleLabel.Text = child.InnerText;
						break;

					case "aspect_guidance":
						helpLabel.Text = child.InnerText;
						e_helpLabel.Text = child.InnerText;
						break;

					case "dest_tag_name":
						tagName = child.InnerText;
						break;

					case "dest_tag_data":
						dataNode = child;
						score = CONVERT.ParseIntSafe(dataNode.InnerText, 0);
						break;

					case "factors":
						foreach (XmlNode factorNode in child.ChildNodes)
						{
							MaturityFactor factor = new MaturityFactor (this, factorNode);
							factor.Visible = false;
							factors.Add(factor);
							this.Controls.Add(factor);
						}
						break;
				}
			}

			if(factors.Count >= 10)
			{
				add_factor.Enabled = false;
			}

			// The score will have been updated from the checkboxes during loading;
			// overwrite it with the saved one (which may differ).
			Score = score;

			UpdateLayout();
		}

		public MaturityAspect (MaturitySection parent, string title)
		{
			this.parent = parent;
			this.aspectNode = null;
			Setup();

			titleLabel.Text = title;
			e_titleLabel.Text = title;

			Score = 0;

			UpdateLayout();
		}

		protected void Setup()
		{
			this.BackColor = CoreUtils.SkinningDefs.TheInstance.GetColorData("maturity_editor_background_colour");

			font = ConstantSizeFont.NewFont("Verdana", 10);

			checkBox = new CheckBox ();
			checkBox.Size = new Size (12, 12);
			checkBox.Location = new Point (0, 2);
			checkBox.CheckedChanged += checkBox_CheckedChanged;
			checkBox.TabStop = false;

			expandButton = new ImageToggleButton (0, "/images/wizard/arrow_collapsed.png", "/images/wizard/arrow_expanded.png");
			expandButton.Size = new Size (12, 12);
			expandButton.Location = new Point (20, 2);
			expandButton.Click += expandButton_Click;

			titleLabel = new Label ();
			titleLabel.Size = new Size (400, 20);
			titleLabel.Location = new Point (40, 0);
			titleLabel.Font = font;

			e_titleLabel = new TextBox ();
			e_titleLabel.Location = new Point (40 + 20 + 5, 0);
			e_titleLabel.Size = new Size (400 - 20 - 5, 20);
			e_titleLabel.Font = font;
			e_titleLabel.BackColor = Color.LightSkyBlue;
			e_titleLabel.Visible = false;

			e_titleLabel.TextChanged += e_titleLabel_TextChanged;

			remove_aspect = new ImageButton(0);
			remove_aspect.Size = new Size(e_titleLabel.Height, e_titleLabel.Height);
			remove_aspect.Location = new Point(40,0);
			remove_aspect.SetToolTipText = "Remove Aspect";
			remove_aspect.SetVariants("\\images\\maturity_tool\\edit_delete");
			remove_aspect.ButtonPressed +=remove_aspect_ButtonPressed;
			//remove_section.Name = "Create/Open Template";
			this.Controls.Add(remove_aspect);




//			scoreBox = new TextBox ();
//			scoreBox.Size = new Size (30, 20);
//			scoreBox.TextAlign = HorizontalAlignment.Center;
//			scoreBox.Location = new Point (titleLabel.Right + 10, 0);
//			scoreBox.TextChanged += new EventHandler (scoreBox_TextChanged);

			scoreBox = new NumericUpDown();
			scoreBox.Size = new Size (40, 20);
			scoreBox.TextAlign = HorizontalAlignment.Center;
			scoreBox.Location = new Point (titleLabel.Right + 10, 0);
			scoreBox.ValueChanged += scoreBox_ValueChanged;
			scoreBox.Minimum = 0;
			
			helpLabel = new Label ();
			helpLabel.Size = new Size (300, 20);
			helpLabel.Location = new Point (490 /*scoreBox.Right + 10*/, 0);
			helpLabel.Font = font;

			e_helpLabel = new TextBox ();
			e_helpLabel.Size = new Size (290, 20);
			e_helpLabel.Location = new Point (490 + 10 /*scoreBox.Right + 10*/, 0);
			e_helpLabel.Font = font;
			e_helpLabel.Visible = false;

			e_helpLabel.TextChanged += e_helpLabel_TextChanged;

			add_factor = new Button();
			add_factor.Text = "Add Factor";
			add_factor.Size = new Size(100,20);
			add_factor.Visible = false;
			add_factor.BackColor = Color.LightGray;
			add_factor.Click += add_factor_Click;
			Controls.Add(add_factor);

			this.Controls.Add(remove_aspect);
			this.Controls.Add(e_titleLabel);
			this.Controls.Add(e_helpLabel);

			this.Controls.Add(checkBox);
			this.Controls.Add(expandButton);
			this.Controls.Add(titleLabel);
			this.Controls.Add(scoreBox);
			this.Controls.Add(helpLabel);
			checkBox.SendToBack();

			factors = new ArrayList ();

			checkBox.Checked = true;
		}

		public void SaveIgnoreList(XmlElement root)
		{
			if(!checkBox.Checked)
			{
				XmlElement ignore = XMLUtils.CreateElement(root, "ignore");
				XMLUtils.SetAttribute(ignore, "question", titleLabel.Text);
				if("" != dest_tag_name)
				{
					XMLUtils.SetAttribute(ignore, "dest_tag_name", dest_tag_name);
				}
			}
		}

		public void SaveToXml(int count_id, string section_name, XmlDocument xdoc, XmlElement root)
		{
			XmlElement aspect = XMLUtils.CreateElement(root, "aspect");
			root.AppendChild( aspect );
			aspect.AppendChild( XMLUtils.CreateElementString(aspect, "aspect_name", titleLabel.Text) );
			// aspect_guidance 		helpLabel.Text
			aspect.AppendChild( XMLUtils.CreateElementString(aspect, "aspect_guidance", helpLabel.Text) );
			// dest_tag_name
			dest_tag_name = section_name + "_" + titleLabel.Text + "_" + CONVERT.ToStr(count_id); 
			dest_tag_name = dest_tag_name.Replace(" ", "_");
			aspect.AppendChild( XMLUtils.CreateElementString(aspect, "dest_tag_name", dest_tag_name) );
			// dest_tag_data score
			aspect.AppendChild( XMLUtils.CreateElementString(aspect, "dest_tag_data", CONVERT.ToStr(Score) ) );
			//
			XmlElement xfactors = XMLUtils.CreateElement(aspect, "factors");
			aspect.AppendChild( xfactors );
			//
			foreach(MaturityFactor factor in factors)
			{
				factor.SaveToXml(xdoc,xfactors);
			}
		}

		void expandButton_Click (object sender, EventArgs e)
		{
			expandButton.State = 1 - expandButton.State;
			Expanded = (expandButton.State == 1);
		}

//		private void scoreBox_TextChanged (object sender, EventArgs e)
//		{
//			//dataNode.InnerText = scoreBox.Text;
//			OnChanged();
//		}

		void scoreBox_ValueChanged (object sender, EventArgs e)
		{
			//dataNode.InnerText = scoreBox.Text;
			OnChanged();
		}

		void checkBox_CheckedChanged (object sender, EventArgs e)
		{
			scoreBox.Enabled = checkBox.Checked;
			titleLabel.Enabled = checkBox.Checked;
			helpLabel.Enabled = checkBox.Checked;

			scoreBox.TabStop = checkBox.Checked;

			foreach (MaturityFactor factor in factors)
			{
				factor.Enable(checkBox.Checked);
			}
		}

		public void UpdateScoreFromChecks ()
		{
			int score = 0;
			foreach (MaturityFactor factor in factors)
			{
				if (factor.Ticked)
				{
					score += factor.Weight;
				}
			}

			Score = score;
		}

		public void UpdateLayout ()
		{
			int l_height = titleLabel.Height;
			if(e_titleLabel.Height > l_height)
			{
				l_height = e_titleLabel.Height;
			}

			expandButton.Visible = (factors.Count > 0);

			int y0 = l_height + 7;//24;
			int y = y0;

			foreach (MaturityFactor factor in factors)
			{
				factor.Location = new Point (0, y);
				y = factor.Bottom + 5;
			}

			if (Expanded)
			{
				add_factor.Location = new Point(338, y+5);
				if(is_editable) y = add_factor.Bottom + 5;
			}
			else
			{
				add_factor.Location = new Point(338, y0+5);
				if(is_editable) y0 = add_factor.Bottom + 5;
			}

			if (Expanded)
			{
				this.Size = new Size (1000, y);
			}
			else
			{
				this.Size = new Size (1000, y0);
			}

			parent.UpdateLayout();
		}

		public string ExportCSV () 
		{
			string csv = MaturityCard.EscapeToCSVFormat(Title);
			csv += "," + MaturityCard.EscapeToCSVFormat(CONVERT.ToStr(Score));

			foreach (MaturityFactor factor in factors)
			{
				csv += "," + MaturityCard.EscapeToCSVFormat(factor.Title) + "," + MaturityCard.EscapeToCSVFormat(CONVERT.ToStr(factor.Ticked));
			}

			csv += "\n";

			return csv;
		}

		void e_titleLabel_TextChanged(object sender, EventArgs e)
		{
			titleLabel.Text = e_titleLabel.Text;
		}

		void e_helpLabel_TextChanged(object sender, EventArgs e)
		{
			helpLabel.Text = e_helpLabel.Text;
		}

		public void RemoveFactor(MaturityFactor factor)
		{
			factors.Remove(factor);
			this.Controls.Remove(factor);
			UpdateLayout();

			if(factors.Count < 10)
			{
				add_factor.Enabled = true;
			}
		}

		void add_factor_Click(object sender, EventArgs e)
		{
			MaturityFactor factor = new MaturityFactor (this, "New Factor");
			factor.Visible = true;
			factors.Add(factor);
			this.Controls.Add(factor);

			if(factors.Count >= 10)
			{
				add_factor.Enabled = false;
			}

			factor.SetEditable(true);
			UpdateLayout();
		}

		void remove_aspect_Click(object sender, EventArgs e)
		{
			parent.RemoveAspect(this);
		}

		void remove_aspect_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			parent.RemoveAspect(this);
		}


	}
}