using System;
using System.Collections;

using System.IO;
using System.Xml;

using System.Windows.Forms;
using System.Drawing;

using LibCore;
using CoreUtils;

namespace maturity_check
{
	public delegate void MaturityCardChangedHandler (object sender);

	public class MaturityCard : BasePanel
	{
		public event MaturityCardChangedHandler Changed;

		void OnChanged ()
		{
			if (Changed != null)
			{
				Changed(this);
			}
		}

		XmlDocument xml;
		public XmlDocument Xml
		{
			get
			{
				return xml;
			}
		}

		Font font;

		ArrayList sections;
		Button add_section;
		bool is_editable = false;

		public ArrayList DefaultColours;

		MaturityEditSessionPanel sessionPanel;

		public MaturityCard (string filename, MaturityEditSessionPanel sessionPanel)
		{
			AddDefaultColours();
			this.sessionPanel = sessionPanel;
			ConstructFromFile(filename);
		}

		bool showNotes = true;
		public bool ShowNotes
		{
			set
			{
				showNotes = value;

				foreach (MaturitySection section in sections)
				{
					section.ShowNotes = value;
				}
			}
		}

		public void SetEditable(bool e)
		{
			is_editable = e;

			foreach (MaturitySection section in sections)
			{
				section.SetEditable(e);
			}

			add_section.Visible = e;

			UpdateLayout();
		}

		void ConstructFromFile (string filename)
		{
			if (! File.Exists(filename))
			{
				filename = AppInfo.TheInstance.Location + "data/eval_wizard.xml";
			}

			using (TextReader reader = new StreamReader (filename))
			{
				BasicXmlDocument xml = BasicXmlDocument.Create(reader.ReadToEnd());
				ConstructFromXml(xml);
			}
		}
		
		public MaturityCard (XmlDocument xml, MaturityEditSessionPanel sessionPanel)
		{
			AddDefaultColours();
			ConstructFromXml(xml);
			this.sessionPanel = sessionPanel;
		}

		void AddDefaultColours ()
		{
			DefaultColours = new ArrayList ();
			DefaultColours.Add(SkinningDefs.TheInstance.GetColorDataGivenDefault("pie_colour_0", Color.FromArgb(204, 160, 202)));
			DefaultColours.Add(SkinningDefs.TheInstance.GetColorDataGivenDefault("pie_colour_1", Color.FromArgb(242, 175, 201)));
			DefaultColours.Add(SkinningDefs.TheInstance.GetColorDataGivenDefault("pie_colour_2", Color.FromArgb(244, 241, 158)));
			DefaultColours.Add(SkinningDefs.TheInstance.GetColorDataGivenDefault("pie_colour_3", Color.FromArgb(114, 198, 219)));
			DefaultColours.Add(SkinningDefs.TheInstance.GetColorDataGivenDefault("pie_colour_4", Color.FromArgb(166, 217, 106)));
			DefaultColours.Add(SkinningDefs.TheInstance.GetColorDataGivenDefault("pie_colour_5", Color.FromArgb(189, 153, 121)));
			DefaultColours.Add(SkinningDefs.TheInstance.GetColorDataGivenDefault("pie_colour_6", Color.FromArgb(210, 129, 120)));
			DefaultColours.Add(SkinningDefs.TheInstance.GetColorDataGivenDefault("pie_colour_7", Color.FromArgb(102, 152, 121)));
			DefaultColours.Add(SkinningDefs.TheInstance.GetColorDataGivenDefault("pie_colour_8", Color.FromArgb(162, 153, 140)));
			DefaultColours.Add(SkinningDefs.TheInstance.GetColorDataGivenDefault("pie_colour_9", Color.FromArgb( 96, 149, 193)));
		}

		void ConstructFromXml (XmlDocument xml)
		{
			this.xml = xml;
			XmlNode cardNode = xml.DocumentElement;

			font = ConstantSizeFont.NewFont("Verdana", 10);

			sections = new ArrayList ();

			foreach (XmlNode child in cardNode.ChildNodes)
			{
				switch (child.Name.ToLower())
				{
					case "section":
						MaturitySection section = new MaturitySection (this, child);
						sections.Add(section);
						this.Controls.Add(section);
						section.ShowNotes = showNotes;

						if (section.Colour == Color.Transparent)
						{
							section.Colour = GetUnusedSectionColour();
						}
						
						section.Changed += section_Changed;
						break;
				}
			}

			UpdateSectionOrders();

			if(sections.Count == 1)
			{
				MaturitySection s = (MaturitySection) sections[0];
				s.DisableRemove(true);
			}

			add_section = new Button();
			add_section.Text = "Add Section";
			add_section.Size = new Size(100,20);
			add_section.BackColor = Color.LightGray;
			add_section.Visible = false;
			add_section.Click += add_section_Click;
			this.Controls.Add(add_section);

			UpdateLayout();
		}

		public void RemoveSection(MaturitySection section)
		{
			// We must always have at least one section.
			if(sections.Count > 1)
			{
				sections.Remove(section);
				this.Controls.Remove(section);
				UpdateLayout();
			}
			//
			if(sections.Count == 1)
			{
				MaturitySection s = (MaturitySection) sections[0];
				s.DisableRemove(true);
			}
		}

		public void UpdateLayout ()
		{
			int y0 = 0;
			int y = y0;
			int left_buffer = 10;

			foreach (MaturitySection section in sections)
			{
				section.Location = new Point (0+left_buffer, y);
				y = section.Bottom + 5;
			}

			if(is_editable)
			{
				add_section.Location = new Point(5+left_buffer, y+5);
				y += add_section.Height + 10;
			}

			this.Size = new Size (1024, y);
		}

		void section_Changed (object sender)
		{
			MaturitySection section = sender as MaturitySection;
			OnChanged();

			EnsureSectionNamesUnique(section);
		}

		void EnsureSectionNamesUnique (MaturitySection changedSection)
		{
			foreach (MaturitySection section in sections)
			{
				if ((section != changedSection) && (section.Title == changedSection.Title))
				{
					section.Title = GetNewSectionName(section.Title);
				}
			}
		}

		public static string EscapeToCSVFormat (string input)
		{
			string output = "";

			// After some heavy Googling, it seems that most versions of Excel have a problem when
			// importing CSVs with newlines within fields.  Try creating a cell with an embedded newline
			// (using alt-enter), export to CSV, then re-import... You'll get mangled input.

			// To work around this, remove any newlines.

			input = input.Replace('\r', ' ').Replace('\n', ' ');

			// Don't quote the string unless...
			// ...it contains a double-quote or newline...
			if ((input.IndexOf('"') != -1)
				|| (input.IndexOf(',') != -1) || (input.IndexOf('\r') != -1) || (input.IndexOf('\n') != -1)
				// ...or it starts or ends with whitespace.
				|| ((input.Length > 0) && (Char.IsWhiteSpace(input[0]) || Char.IsWhiteSpace(input[input.Length - 1]))))
			{
				output = "\"";

				foreach (char c in input)
				{
					if (c == '"')
					{
						output += "\"\"";
					}
					else
					{
						output += c;
					}
				}

				output += "\"";
			}
			else
			{
				output = input;
			}

			return output;
		}

		public XmlDocument SaveToXML(string filename)
		{
			XmlDocument xdoc = new XmlDocument();
			XmlElement root = (XmlElement) xdoc.CreateElement("root");
			xdoc.AppendChild(root);
			//
			int count = 0;
			//
			foreach (MaturitySection section in sections)
			{
				count = section.SaveToXml(count,xdoc, root);
			}
			//
			xdoc.Save(filename);
			return xdoc;
		}

		public void SaveIgnoreList(string filename)
		{
			XmlDocument xdoc = new XmlDocument();
			XmlElement root = (XmlElement) xdoc.CreateElement("ignore_list");
			xdoc.AppendChild(root);
			//
			foreach (MaturitySection section in sections)
			{
				section.SaveIgnoreList(root);
			}
			//
			xdoc.Save(filename);
		}

		public string ExportCSV ()
		{
			string csv = "Customer Name,Date,Region,Country,Email 1,Email 2,Phone,Purpose,Address,Section,Notes,Maturity Aspect,Maturity Score,Maturity Factors...\n";
			
			csv += sessionPanel.ExportCustomerInfoCSV();

			foreach (MaturitySection section in sections)
			{
				csv += section.ExportCSV();
			}

			return csv;
		}

		public void ExportCSVFile (string filename)
		{
			using (StreamWriter writer = new StreamWriter (filename))
			{
				writer.Write(ExportCSV());
			}
		}

		public void ExportPDFFile (string filename)
		{
			string xml = sessionPanel.GenerateMaturityPieXml();
			sessionPanel.BuildMaturityPDF(filename, xml);

			if (File.Exists(filename))
			{
				try
				{
					System.Diagnostics.Process.Start(filename);
				}
				catch (Exception evc)
				{
					if (evc.Message.IndexOf("No Application") > -1)
					{
						MessageBox.Show(TopLevelControl, "Cannot present PDF Summary Sheet ", "No PDF Reader Application Installed"
							, MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
					else
					{
						MessageBox.Show(TopLevelControl, "Cannot present PDF Summary Sheet ", "Failed to Start PDF Reader."
							, MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			}
		}

		/// <summary>
		/// Return the specified name X, unless there are already sections named X, X (2), X (3) etc,
		/// in which case return X (n) for some currently-unused n.
		/// </summary>
		string GetNewSectionName (string baseName)
		{
			string name;
			int suffix = 1;
			bool clash;

			do
			{
				name = baseName;
				if (suffix > 1)
				{
					name += " (" + CONVERT.ToStr(suffix) + ")";
				}

				clash = false;
				foreach (MaturitySection section in sections)
				{
					if (section.Title == name)
					{
						clash = true;
						break;
					}
				}

				suffix++;
			}
			while (clash);

			return name;
		}

		void add_section_Click(object sender, EventArgs e)
		{
			MaturitySection section = new MaturitySection(this, GetNewSectionName("[Describe Section Here]"));
			sections.Add(section);
			section.ShowNotes = showNotes;
			this.Controls.Add(section);

			section.Colour = GetUnusedSectionColour();

			section.Changed += section_Changed;

			section.SetEditable(true);

			UpdateSectionOrders();
			UpdateLayout();

			if(sections.Count == 2)
			{
				MaturitySection s = (MaturitySection) sections[0];
				s.DisableRemove(false);
			}
		}

		Color GetUnusedSectionColour ()
		{
			Color unusedColour = Color.Transparent;
			foreach (Color colour in DefaultColours)
			{
				bool used = false;

				foreach (MaturitySection trySection in sections)
				{
					if ((trySection.Colour != Color.Transparent) && (trySection.Colour == colour))
					{
						used = true;
						break;
					}
				}

				if (!used)
				{
					unusedColour = colour;
					break;
				}
			}

			return unusedColour;
		}

		public void SwapSectionsGivenPieChartOrder (string sourceSectionName, string destSectionName)
		{
			int sourceSectionIndex = -1;
			int destSectionIndex = -1;

			for (int i = 0; i < sections.Count; i++)				
			{
				MaturitySection section = sections[i] as MaturitySection;

				if (section.Title == sourceSectionName)
				{
					sourceSectionIndex = i;
				}
				if (section.Title == destSectionName)
				{
					destSectionIndex = i;
				}
			}

			SwapSections(sourceSectionIndex, destSectionIndex);
		}

		public void SwapSections (int source, int dest)
		{
			MaturitySection a = sections[source] as MaturitySection;
			MaturitySection b = sections[dest] as MaturitySection;

			int oldA = a.order;
			a.order = b.order;
			b.order = oldA;
		}

		/// <summary>
		/// Ensure that the section orders make sense, filling in any unspecified ones,
		/// resolving duplicates, and compressing them to a logical range.
		/// eg sections with orders { 7, 3, unspecified, 7, unspecified }
		/// -> { 2, 1, 4, 3, 5 }
		/// </summary>
		void UpdateSectionOrders ()
		{
			ArrayList correctedOrder = new ArrayList ();
			ArrayList sectionsToProcess = new ArrayList (sections);

			while (sectionsToProcess.Count > 0)
			{
				// Find the section with the lowest assigned order.
				MaturitySection lowestOrderedSection = null;
				foreach (MaturitySection compareSection in sectionsToProcess)
				{
					if ((lowestOrderedSection == null)
					    || ((compareSection.order != -1) &&
					        (compareSection.order < lowestOrderedSection.order)))
					{
						lowestOrderedSection = compareSection;
					}
				}

				// Add it into the corrected orders.
				correctedOrder.Add(lowestOrderedSection);
				sectionsToProcess.Remove(lowestOrderedSection);
			}

			// Now we have an array of sections ordered by their original order.  Now assign them
			// a new order just based on this, to reduce the ordering to a nice sequential list.
			int i = 1;
			foreach (MaturitySection section in correctedOrder)
			{
				section.order = i;
				i++;
			}
		}

		public void ChangeSectionColour (string sectorName, Color colour)
		{
			foreach (MaturitySection section in sections)
			{
				if (section.Title == sectorName)
				{
					section.Colour = colour;
				}
			}
		}

		public Hashtable GetSectionColours ()
		{
			Hashtable colours = new Hashtable ();

			foreach (MaturitySection section in sections)
			{
				colours.Add(section.Title, section.Colour);
			}

			return colours;
		}
	}
}