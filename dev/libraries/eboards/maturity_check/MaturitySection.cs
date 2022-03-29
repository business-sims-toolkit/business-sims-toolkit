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
	public class MaturitySection : BasePanel
	{
		public event MaturityCardChangedHandler Changed;

		void OnChanged ()
		{
			if (Changed != null)
			{
				Changed(this);
			}
		}

		XmlNode sectionNode;
		MaturityCard parent;

		Font font;
		bool is_editable = false;

		public string Title
		{
			set
			{
				titleLabel.Text = value;
				e_titleLabel.Text = value;
			}

			get
			{
				return titleLabel.Text;
			}
		}

		Color colour;
		public Color Colour
		{
			get
			{
				return colour;
			}

			set
			{
				colour = value;
				OnChanged();
			}
		}

		ArrayList aspects;

		Label titleLabel;
		TextBox e_titleLabel;
		Panel underline;

		public int order;

		Label notesLabel;
		TextBox notes;

		Button add_new_aspect;
		ImageButton remove_section;

		public bool ShowNotes
		{
			set
			{
				notesLabel.Visible = value;
				notes.Visible = value;
			}
		}

		public void SetEditable(bool e)
		{
			is_editable = e;

			if(e)
			{
				e_titleLabel.Visible = true;
				remove_section.Visible = true;
				titleLabel.Visible = false;
			}
			else
			{
				titleLabel.Visible = true;
				remove_section.Visible = false;
				e_titleLabel.Visible = false;
				add_new_aspect.Visible = false;
			}

			foreach (MaturityAspect aspect in aspects)
			{
				aspect.SetEditable(e);
			}

			UpdateLayout();

			if(e) add_new_aspect.Visible = true;
		}

		public void DisableRemove(bool e)
		{
			remove_section.Enabled = !e;
		}

		public MaturitySection (MaturityCard parent, XmlNode sectionNode)
		{
			this.sectionNode = sectionNode;
			this.parent = parent;
			Setup();

			order = -1;

			colour = BasicXmlDocument.GetColourAttribute(sectionNode, "colour", Color.Transparent);

			foreach (XmlNode child in sectionNode.ChildNodes)
			{
				switch (child.Name.ToLower())
				{
					case "section_name":
					{
						titleLabel.Text = child.InnerText;
						e_titleLabel.Text = child.InnerText;
					}
						break;

					case "section_order":
						order = CONVERT.ParseInt(child.InnerText);
						break;

					case "aspects":
					{
						foreach (XmlNode aspectNode in child.ChildNodes)
						{
							MaturityAspect aspect = new MaturityAspect (this, aspectNode);
							aspects.Add(aspect);
							this.Controls.Add(aspect);
							aspect.Expanded = true;
							aspect.Changed += aspect_Changed;
						}
						//
						if(aspects.Count == 1)
						{
							MaturityAspect ma = (MaturityAspect) aspects[0];
							ma.DisableRemove(true);
						}
					}
						break;

					case "notes":
					{
						notes.Text = child.InnerText;
					}
						break;
				}
			}

			UpdateLayout();
		}

		public MaturitySection (MaturityCard parent, string title)
		{
			this.parent = parent;
			Setup();
			//
			e_titleLabel.Text = title;
			//
			// We must create one aspect within this section.
			//
			MaturityAspect aspect = new MaturityAspect (this, "[Describe Aspect Here]");
			aspects.Add(aspect);
			this.Controls.Add(aspect);
			aspect.Changed += aspect_Changed;
			aspect.SetEditable(true);
			aspect.Expanded = true;
			aspect.DisableRemove(true);
			//
			UpdateLayout();
		}

		protected void Setup()
		{
			font = ConstantSizeFont.NewFont("Verdana", 10);

			this.BackColor = CoreUtils.SkinningDefs.TheInstance.GetColorData("maturity_editor_background_colour");

			titleLabel = new Label ();
			titleLabel.Size = new Size (1000, 20);
			titleLabel.Location = new Point (0, 0);
			titleLabel.Font = font;

			e_titleLabel = new TextBox ();
			e_titleLabel.Size = new Size (1000, 20);
			e_titleLabel.Location = new Point (0, 0);
			e_titleLabel.Font = font;
			e_titleLabel.BackColor = Color.LightSteelBlue;
			e_titleLabel.Visible = false;

			e_titleLabel.TextChanged += e_titleLabel_TextChanged;


			remove_section = new ImageButton(0);
			remove_section.Size = new Size(e_titleLabel.Height, e_titleLabel.Height);
			remove_section.Location = new Point(0,0);
			remove_section.SetToolTipText = "Remove Section";
			remove_section.SetVariants("\\images\\maturity_tool\\edit_delete");
			//remove_section.ButtonPressed += new CommonGUI.ImageButton.ImageButtonEventArgsHandler(idone_ButtonPressed);
			remove_section.ButtonPressed +=remove_section_ButtonPressed;
			//remove_section.Name = "Create/Open Template";
			this.Controls.Add(remove_section);


//			remove_section = new Button();
//			remove_section.Text = "X";
//			remove_section.Size = new Size(e_titleLabel.Height, e_titleLabel.Height);
//			remove_section.Location = new Point(0,0);
//			remove_section.Click += new EventHandler(remove_section_Click);
//			remove_section.Visible = false;
//			remove_section.BackColor = Color.LightGray;
//			this.Controls.Add(remove_section);

			e_titleLabel.Left += 5 + e_titleLabel.Height;
			e_titleLabel.Size = new Size (1000 - 5 - e_titleLabel.Height, e_titleLabel.Height);

			underline = new Panel ();
			underline.BackColor = Color.Gray;
			underline.Size = new Size (1000, 1);
			underline.Location = new Point (0, titleLabel.Bottom);

			notesLabel = new Label();
			notesLabel.Text = "Notes";
			notesLabel.Size = new Size(200,20);
			this.Controls.Add(notesLabel);

			notes = new TextBox();
			notes.Size = new Size(200,200);
			notes.Multiline = true;
			notes.ScrollBars = ScrollBars.Vertical;
			this.Controls.Add(notes);

			this.Controls.Add(e_titleLabel);
			this.Controls.Add(titleLabel);
			this.Controls.Add(underline);

			add_new_aspect = new Button();
			add_new_aspect.Text = "Add Aspect";
			add_new_aspect.Size = new Size(100,20);
			add_new_aspect.Visible = false;
			add_new_aspect.BackColor = Color.LightGray;
			add_new_aspect.Click += add_new_aspect_Click;
			this.Controls.Add(add_new_aspect);

			aspects = new ArrayList ();

			int l_height = titleLabel.Height;
			if(e_titleLabel.Height > l_height)
			{
				l_height = e_titleLabel.Height;
			}

			underline.Location = new Point (0, l_height);//titleLabel.Bottom);
			notesLabel.Location = new Point(800, underline.Bottom+2);
			notes.Location = new Point(800, notesLabel.Bottom+2);
		}

		public void UpdateLayout ()
		{
			int l_height = titleLabel.Height;
			if(e_titleLabel.Height > l_height)
			{
				l_height = e_titleLabel.Height;
			}

			int y0 = l_height + 7;//30;
			int y = y0;

			foreach (MaturityAspect aspect in aspects)
			{
				aspect.Location = new Point (0, y);
				y = aspect.Bottom + 5;
			}

			add_new_aspect.Location = new Point(15, y+5);
			if(is_editable)
			{
				y += add_new_aspect.Height + 10;
			}

			// Always have 200 pixels so that the notes box is big enough.
			if(y < 200) y = 200;
			notes.Size = new Size(200, y - notesLabel.Top - 25);

			underline.Location = new Point (0, y + 2);
			this.Size = new Size (1024, underline.Bottom + 2);

			notes.BringToFront();
			notesLabel.BringToFront();

			parent.UpdateLayout();
		}

		void aspect_Changed (object sender)
		{
			OnChanged();
		}

		public void SaveIgnoreList(XmlElement root)
		{
			foreach (MaturityAspect aspect in aspects)
			{
				aspect.SaveIgnoreList(root);
			}
		}


		public int SaveToXml(int count_it, XmlDocument xdoc, XmlElement root)
		{
			XmlElement section = XMLUtils.CreateElement(root, "section");
			root.AppendChild( section );

			if (Colour != Color.Transparent)
			{
				section.SetAttribute("colour", CONVERT.ToComponentStr(Colour));
			}

			section.AppendChild( XMLUtils.CreateElementString(section, "section_name", titleLabel.Text) );

			section.AppendChild(XMLUtils.CreateElementString(section, "section_order", CONVERT.ToStr(order)));

			XmlElement aspects_node = XMLUtils.CreateElement(section, "aspects");
			section.AppendChild( aspects_node );

			int rcount = count_it;

			foreach (MaturityAspect aspect in aspects)
			{
				aspect.SaveToXml(rcount, titleLabel.Text, xdoc, aspects_node);
				++rcount;
			}

			return rcount;
		}

		public string ExportCSV ()
		{
			string csv = ",,,,,,,,," + MaturityCard.EscapeToCSVFormat(titleLabel.Text) + "," + MaturityCard.EscapeToCSVFormat(notes.Text) + ",,\n";

			foreach (MaturityAspect aspect in aspects)
			{
				csv += ",,,,,,,,,,," + aspect.ExportCSV();
			}

			return csv;
		}

		void e_titleLabel_TextChanged(object sender, EventArgs e)
		{
			titleLabel.Text = e_titleLabel.Text;
			OnChanged();
		}

		void add_new_aspect_Click(object sender, EventArgs e)
		{
			MaturityAspect aspect = new MaturityAspect (this, "[Describe Aspect Here]");
			aspects.Add(aspect);
			this.Controls.Add(aspect);
			aspect.Changed += aspect_Changed;

			aspect.SetEditable(true);
			aspect.Expanded = true;

			UpdateLayout();

			if(aspects.Count == 2)
			{
				MaturityAspect ma = (MaturityAspect) aspects[0];
				ma.DisableRemove(false);
			}

			//MaturityCard mc = (MaturityCard) this.Parent;
			//mc.UpdateLayout();
		}

		public void RemoveAspect(MaturityAspect aspect)
		{
			// We must always have at least one aspect.
			if(aspects.Count > 1)
			{
				aspects.Remove(aspect);
				this.Controls.Remove(aspect);
				UpdateLayout();
			}
			//
			if(aspects.Count == 1)
			{
				MaturityAspect ma = (MaturityAspect) aspects[0];
				ma.DisableRemove(true);
			}
		}

		void remove_section_Click(object sender, EventArgs e)
		{
			parent.RemoveSection(this);
		}

		void remove_section_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			parent.RemoveSection(this);
		}
	}
}