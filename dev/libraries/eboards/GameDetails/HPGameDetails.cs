using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Xml;

using LibCore;
using CoreUtils;
using CommonGUI;

using GameManagement;

namespace GameDetails
{
	public class HPGameDetails : GameDeliveryDetailsSection
	{
		ErrorProvider errProvider;
		protected em_GameEvalType gameEvalType = em_GameEvalType.UNDEFINED;
		protected string gameEval_CustomName = "";

		protected Label titleLabel;
		protected new TextBox title;
		protected Label placeLabel;
		protected TextBox place;
		protected Label countryLabel;
		protected ComboBox country;
		protected Label geoRegionLabel;
		protected ComboBox georegion;
		protected Label venueLabel;
		protected TextBox venue;
		protected Label clientLabel;
		protected TextBox client;
		protected Label playersLabel;
		protected CommonGUI.EntryBox players;
		protected Label purposeLabel;
		protected ComboBox purpose;
		protected Label notesLabel;
		protected TextBox notes;
		//
		protected GameManagement.NetworkProgressionGameFile _gameFile;
		protected Hashtable RegionsLookup = new Hashtable();
    protected ArrayList geographicRegions = new ArrayList();
		protected bool IgnoreRegionChange = false;
		//
		protected string originalTitle = "";
		protected string originalPath = "";
		//
		protected Font labelFont;
		protected object ControlInError = null;

		public override string GameTitle => title.Text;

		public override string GameVenue => venue.Text;
		public override string GameLocation => place.Text;
		public override string GameRegion => georegion.Text;
		public override string GameCountry => country.Text;
		public override string GameClient => client.Text;
		public override string GameChargeCompany => null;
		public override string GameNotes => notes.Text;
		public override string GamePurpose => purpose.Text;
		public override int GamePlayers => CONVERT.ParseIntSafe(players.Text, 0);

		protected Label CreateLabel (string name, int x, int y, int w, int h)
		{
			return CreateLabel(name, x, y, w, h, labelFont);
		}

		protected Label CreateLabel (string name, int x, int y, int w, int h, float size)
		{
			return CreateLabel(name, x, y, w, h, ConstantSizeFont.NewFont(labelFont.FontFamily, size, labelFont.Style));
		}

		protected Label CreateLabel(string name, int x, int y, int w, int h, Font font)
		{
			Label l = new Label();
			l.Text = name;
			l.Font = font;
			l.Location = new Point(x,y);
			l.Size = new Size(w,h);
			l.TextAlign = ContentAlignment.MiddleRight;
			panel.Controls.Add(l);
			return l;
		}

		protected TextBox CreateTextBox(string name, int x, int y, int w, int h)
		{
			TextBox l = new TextBox();
			l.Text = name;
			l.Font = labelFont;
			l.Location = new Point(x,y);
			l.Size = new Size(w,h);
			panel.Controls.Add(l);
			l.TextChanged += textBox_TextChanged;
			return l;
		}

		public void BuildGeographicData()
		{
			StreamReader SR;
			string S;

			SR = File.OpenText(LibCore.AppInfo.TheInstance.Location + "\\data\\countries.xml");
			S = SR.ReadToEnd();
			SR.Close();
			SR = null;

			LibCore.BasicXmlDocument geoxml = LibCore.BasicXmlDocument.Create(S);
			//the first is the xml version data, the second is the data 
			System.Xml.XmlNode datanode = geoxml.ChildNodes[1];

			foreach(System.Xml.XmlNode region_node in datanode.ChildNodes)
			{
				System.Xml.XmlNode NameNode_Region = region_node.Attributes.GetNamedItem("name");
				string region_name = NameNode_Region.InnerText; 

				ArrayList al = new ArrayList();
				foreach (System.Xml.XmlNode country_node in region_node.ChildNodes)
				{
					System.Xml.XmlNode NameNode_Country = country_node.Attributes.GetNamedItem("name");
					string country_name = NameNode_Country.InnerText; 
					//string countryname = region.Attributes.GetNamedItem("name");
					//System.Diagnostics.Debug.WriteLine("  Country :"+ country_name);
					al.Add(country_name);
				}
				geographicRegions.Add(region_name);
				RegionsLookup.Add(region_name, al);
			}
		}

		public void BuildPurposesData()
		{
			StreamReader SR;
			string S;

			SR=File.OpenText(LibCore.AppInfo.TheInstance.Location + "\\data\\purposes.txt");
			S=SR.ReadLine();
			while(S!=null)
			{
				purpose.Items.Add(S);
				S=SR.ReadLine();
			}
			SR.Close();
		}		

		public HPGameDetails(GameManagement.NetworkProgressionGameFile gameFile)
		{
			_gameFile = gameFile;

			string firstPart;
			string lastPart;
			originalTitle = GameUtils.FileNameToGameName(_gameFile.Name, out firstPart, out lastPart);

			originalPath = System.IO.Path.GetDirectoryName(firstPart);
			//originalTitle = System.IO.Path.GetFileName(fullName);

			errProvider = new ErrorProvider(this);

			int fontSize = CoreUtils.SkinningDefs.TheInstance.GetIntData("font_size", 10);
			labelFont = CoreUtils.SkinningDefs.TheInstance.GetFont(fontSize);

			BuildNewControls();
			
			//
			titleLabel = CreateLabel("Title", 0,5, 80, 20);
			title = CreateTextBox("", 85, 5, 130+15, 20);
			title.TabIndex = 0;
			title.Text = originalTitle;
			//
			placeLabel = CreateLabel("Town/City", 0,30, 80, 20);
			place = CreateTextBox("", 85, 30, 130+15, 20);
			place.TabIndex = 1;
			//
			geoRegionLabel = CreateLabel("Region", 0, 55, 80, 20);
			georegion = new ComboBox();
			georegion.Font = labelFont;
			georegion.Location = new Point(85,55);
			georegion.Size = new Size(130+15,20);
			georegion.TabIndex = 2;
			georegion.DropDownStyle = ComboBoxStyle.DropDownList;
			georegion.SelectedIndexChanged += this.georegion_SelectedIndexChanged;

			countryLabel = CreateLabel("Country", 0, 80, 80, 20);
			country = new ComboBox();
			country.Font = labelFont;
			country.Location = new Point(85,80);
			country.Size = new Size(130+15,20);
			country.TabIndex = 3;
			country.DropDownStyle = ComboBoxStyle.DropDownList;
			country.SelectedValueChanged += country_SelectedValueChanged;

			BuildGeographicData();
			//
			//
			panel.Controls.Add(georegion);
			panel.Controls.Add(country);
			//
			venueLabel = CreateLabel("Venue", 200+30,5, 80, 20);
			venue = CreateTextBox("", 285+30, 5, 125+20, 20);
			venue.TabIndex = 5;
			//
			clientLabel = CreateLabel("Client", 200+30,30, 80, 20);
			client = CreateTextBox("", 285+30, 30, 125+20, 20);
			client.TabIndex = 6;
			//
			playersLabel = CreateLabel("Players", 200+30,55, 80, 20);
			players = new EntryBox();
			players.DigitsOnly = true;
			players.Location = new Point(285+30,55);
			players.Size = new Size(40,20);
			players.TextAlign = HorizontalAlignment.Center;
			players.Font = labelFont;
			players.TabIndex = 7;
			panel.Controls.Add(players);
			players.TextChanged += players_TextChanged;
			//
			purposeLabel = CreateLabel("Purpose", 200+30,80, 80, 20);
			purpose = new ComboBox();
			purpose.Font = labelFont;
			purpose.Location = new Point(285+30,80);
			purpose.Size = new Size(140+5,20);
			purpose.TabIndex = 8;
			purpose.DropDownStyle = ComboBoxStyle.DropDownList;
			purpose.SelectedValueChanged += purpose_SelectedValueChanged;
			//
			BuildPurposesData();
			//
			panel.Controls.Add(purpose);
			//
			notesLabel = CreateLabel("Notes", 0,107, 80, 20);
			notes = CreateTextBox("Notes In Here."
				, 85, 106, 391, 100);
			notes.Multiline = true;
			notes.TabIndex = 9;
			notes.MaxLength = 1000;

			LoadData();

			Collapsible = false;
			SetAutoSize();

			foreach (var field in new Control [] { title, venue, place, georegion, country, purpose, players, client })
			{
				field.Enabled = false;
			}
		}

		public virtual void BuildNewControls()
		{
		}

		protected void LoadGeoData(string gameRegionName, string gameCountryName)
		{
			IgnoreRegionChange = true;
			if ((gameRegionName == string.Empty)&&(gameCountryName == string.Empty))
			{
				this.georegion.Items.Clear();
				foreach (string reg in geographicRegions)
				{
					this.georegion.Items.Add(reg);
				}
				this.georegion.SelectedIndex = -1;
				this.georegion.SelectedText = "";
				this.country.SelectedIndex = -1;
				this.country.SelectedText = "";
			}
			else
			{
				//Build the Regions control
				//if (RegionsLookup.Contains(gameRegionName))
				{
					//Clear
					this.georegion.Items.Clear();
					int selectedRegionIndex = -1;
					int selectedCountryIndex = -1;
					foreach (string reg in geographicRegions)
					{
						int region_index = this.georegion.Items.Add(reg);
						if (gameRegionName.ToLower() == reg.ToLower())
						{ 
							selectedRegionIndex = region_index;
							//fill the country control 
							this.country.Items.Clear();
							ArrayList cl = (ArrayList) RegionsLookup[reg];
							foreach(string countryname in cl)
							{
								int ctry_index = this.country.Items.Add(countryname);
								if (countryname.ToLower() == gameCountryName.ToLower())
								{
									selectedCountryIndex = ctry_index;
								}
							}
						}
					}
					this.georegion.SelectedIndex = selectedRegionIndex;
					this.country.SelectedIndex = selectedCountryIndex;
				}
				/*
				else
				{
					//geographicRegions.Clear();
					//geographicRegions.Add("World");
					this.georegion.Items.Clear();
					this.georegion.Items.Add("World");
					this.georegion.SelectedIndex = 0;
					if (gameCountryName != null)
					{
						this.country.Items.Clear();
						this.country.Items.Add(gameCountryName);
						this.country.SelectedIndex = 0;
					}
					else
					{
						this.country.Items.Clear();
						this.country.Items.Add("UNITED KINGDOM");
						this.country.SelectedIndex = 0;
					}
				}*/
			}
			IgnoreRegionChange = false;
		}

		void georegion_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			OnChanged();

			if (IgnoreRegionChange == false)
			{
				ComboBox cb = (System.Windows.Forms.ComboBox)(sender);
				if (cb != null)
				{
					string newRegionName = (string) cb.SelectedItem;
					if (newRegionName != "World")
					{
						this.country.Items.Clear();
						if (RegionsLookup.ContainsKey(newRegionName))
						{
							ArrayList cl = (ArrayList) RegionsLookup[newRegionName];
							foreach(string countryname in cl)
							{
								this.country.Items.Add(countryname);
							}
							//this.country.SelectedIndex =0;
						}
					}
				}
			}
		}

		public override void LoadData()
		{
			string tmpRegionName = string.Empty;
			string tmpCountryName = string.Empty;

			string fileName = _gameFile.Dir + "\\global\\details.xml";
			if(File.Exists(fileName))
			{
				XmlDocument xdoc = new XmlDocument();
				xdoc.Load(fileName);
				title.Text   = CoreUtils.XMLUtils.GetElementString(xdoc.DocumentElement, "Title");
				place.Text   = CoreUtils.XMLUtils.GetElementString(xdoc.DocumentElement, "Location");
				venue.Text   = CoreUtils.XMLUtils.GetElementString(xdoc.DocumentElement, "Venue");
				client.Text  = CoreUtils.XMLUtils.GetElementString(xdoc.DocumentElement, "Client");
				players.Text = CoreUtils.XMLUtils.GetElementString(xdoc.DocumentElement, "Players");
				tmpCountryName = CoreUtils.XMLUtils.GetElementString(xdoc.DocumentElement, "Country");
				purpose.Text = CoreUtils.XMLUtils.GetElementString(xdoc.DocumentElement, "Purpose");

				try
				{
					tmpRegionName = CoreUtils.XMLUtils.GetElementString(xdoc.DocumentElement, "GeoRegion");
					notes.Text = CoreUtils.XMLUtils.GetElementString(xdoc.DocumentElement, "Notes");
				}
				catch
				{
					// Old game with no notes section!
				}
			}
			LoadGeoData(tmpRegionName, tmpCountryName); 
		}

		protected bool Error(Control c, string error)
		{
			this.Expanded = true;
			errProvider.SetError(c, error);
			ControlInError = c;
			return false;
		}

		protected void ErrorClear()
		{
			if (ControlInError != null)
			{
				errProvider.SetError((Control)ControlInError, "");
				ControlInError = null;
			}	
		}

		public override bool SaveData()
		{
			ErrorClear();
			
			if(title.Text   == "") return Error(title,   "Title Must Be Entered.");
			if(place.Text   == "") return Error(place,   "Place Must Be Entered.");
			if(venue.Text   == "") return Error(venue,   "Venue Must Be Entered.");
			if(client.Text  == "") return Error(client,  "Client Must Be Entered.");
			if(players.Text == "") return Error(players, "Players Must Be Entered.");

			int numPlayers = CONVERT.ParseInt(players.Text);
			if(numPlayers <= 0) return Error(players, "Players Must Be Non-Zero.");

			if(georegion.SelectedIndex == -1) return Error(georegion, "Region Must Be Entered.");
			if(country.SelectedIndex == -1) return Error(country, "Country Must Be Entered.");
			if(purpose.SelectedIndex == -1) return Error(purpose, "Purpose Must Be Entered.");

			if(originalTitle != title.Text)
			{
				// TODO :  Tell the game file to rename the game!
				string newName;
				string err;

				// Get the creation date out of the existing filename, if present
				// (if it fails, it'll get the current date, which is existing behaviour).
				DateTime creationDate = GameUtils.FileNameToCreationDate(Path.GetFileName(_gameFile.FileName));
				GameUtils.EstablishNewFileName(out newName, title.Text, out err, creationDate.Year, creationDate.Month, creationDate.Day);

				string newFullName = this.originalPath + "\\" + newName;
				_gameFile.Rename(newFullName);

				// Make sure we are stopped....
				TimeManager.TheInstance.Stop();

				throw new Exception();
//				_gameFile.License.ChangeFileName( Path.GetFileName(newFullName) );
			}

			// TODO : write the data into the game file! (GameDetails.xml)
			XmlDocument xdoc = new XmlDocument();
			XmlElement root = xdoc.CreateElement("details");
			LibCore.BasicXmlDocument.AppendAttribute(root, "type", CoreUtils.SkinningDefs.TheInstance.GetData("gametype"));
			xdoc.AppendChild(root);

			CoreUtils.XMLUtils.CreateElementString(root, "Title", title.Text);
			CoreUtils.XMLUtils.CreateElementString(root, "Location", place.Text);
			CoreUtils.XMLUtils.CreateElementString(root, "Venue", venue.Text);
			CoreUtils.XMLUtils.CreateElementString(root, "Client", client.Text);
			CoreUtils.XMLUtils.CreateElementString(root, "Players", players.Text);

			//the user should select the country 
			if (country.SelectedIndex == -1)
			{
				CoreUtils.XMLUtils.CreateElementString(root, "Country", "");
			}
			else
			{
				CoreUtils.XMLUtils.CreateElementString(root, "Country", (string) country.Items[country.SelectedIndex]);
			}
			if (georegion.SelectedIndex == -1)
			{
				CoreUtils.XMLUtils.CreateElementString(root, "GeoRegion", "");
			}
			else
			{
				CoreUtils.XMLUtils.CreateElementString(root, "GeoRegion", (string) georegion.Items[georegion.SelectedIndex]);
			}
			CoreUtils.XMLUtils.CreateElementString(root, "Purpose", (string) purpose.Items[purpose.SelectedIndex]);
			CoreUtils.XMLUtils.CreateElementString(root, "Notes", notes.Text);

			xdoc.Save(_gameFile.Dir + "\\global\\details.xml");
			return true;
		}
	
		protected override void Dispose(bool disposing)
		{
			if(disposing)
			{
				errProvider.Dispose();
				titleLabel.Dispose();
				title.Dispose();
				placeLabel.Dispose();
				place.Dispose();
				countryLabel.Dispose();
				country.Dispose();
				geoRegionLabel.Dispose();
				georegion.Dispose();
				venueLabel.Dispose();
				venue.Dispose();
				clientLabel.Dispose();
				client.Dispose();
				playersLabel.Dispose();
				players.Dispose();
				purposeLabel.Dispose();
				purpose.Dispose();
				notesLabel.Dispose();
				notes.Dispose();
			}

			base.Dispose (disposing);
		}

		/// <summary>
		/// Reorder the tab indices assigned to all the editable controls within the specified control,
		/// so that the tab key cycles through them top-to-bottom and then left-to-right.
		/// </summary>
		public void ReassignTabOrder (Control parent)
		{
			List<Control> controls = new List<Control>();
			foreach (Control control in parent.Controls)
			{
				if ((control is ComboBox) || (control is TextBox))
				{
					controls.Add(control);
				}
			}
			controls.Sort(CompareControlsByPosition);

			for (int i = 0; i < controls.Count; i++)
			{
				controls[i].TabIndex = i;
			}
		}

		/// <summary>
		/// Given two controls, return a signed integer suitable for sorting them in a to-to-bottom-then-left-to-right
		/// order.
		/// Multiline text boxes sort after other controls though.
		/// </summary>
		int CompareControlsByPosition (Control a, Control b)
		{
			if ((a is TextBox) && ((a as TextBox).Multiline) && ! ((b is TextBox) && ((b as TextBox).Multiline)))
			{
				return 1;
			}
			else if ((b is TextBox) && ((b as TextBox).Multiline) && !((a is TextBox) && ((a as TextBox).Multiline)))
			{
				return -1;
			}

			if (a.Left < b.Left)
			{
				return -1;
			}
			else if (b.Left < a.Left)
			{
				return 1;
			}
			else
			{
				if (a.Top < b.Top)
				{
					return -1;
				}
				else if (b.Top < a.Top)
				{
					return 1;
				}
				else
				{
					if (a.Right < b.Right)
					{
						return -1;
					}
					else if (b.Right < a.Right)
					{
						return 1;
					}
					else
					{
						return b.Parent.Controls.IndexOf(b) - a.Parent.Controls.IndexOf(a);
					}
				}
			}
		}

		public override void ShowSpecificParts (bool showGameSpecificParts)
		{
			if (! showGameSpecificParts)
			{
				purpose.SelectedIndex = 0;
				for (int i = 0; i < purpose.Items.Count; i++)
				{
					string purposeString = (string) purpose.Items[i];
					if (purposeString.ToLower().Contains("foundation"))
					{
						purpose.SelectedIndex = i;
						break;
					}
				}
			}

			purpose.Visible = showGameSpecificParts;
			purposeLabel.Visible = showGameSpecificParts;
		}

		protected bool ValidateField (TextBox control, string error, bool reportErrors)
		{
			if (string.IsNullOrEmpty(control.Text.Trim()))
			{
				if (reportErrors)
				{
					Error(control, error);
				}
				return false;
			}

			return true;
		}

		protected bool ValidateField (ComboBox control, string error, bool reportErrors)
		{
			if (control.SelectedIndex == -1)
			{
				if (reportErrors)
				{
					Error(control, error);
				}
				return false;
			}

			return true;
		}

		protected bool ValidateField (Control control, bool condition, string error, bool reportErrors)
		{
			if (! condition)
			{
				if (reportErrors)
				{
					Error(control, error);
				}
				return false;
			}

			return true;
		}

		void textBox_TextChanged (object sender, EventArgs args)
		{
			OnChanged();
		}

		void country_SelectedValueChanged (object sender, EventArgs args)
		{
			OnChanged();
		}

		void purpose_SelectedValueChanged (object sender, EventArgs args)
		{
			OnChanged();
		}

		void players_TextChanged (object sender, EventArgs args)
		{
			OnChanged();
		}

		public string Notes
		{
			get
			{
				return notes.Text;
			}

			set
			{
				notes.Text = value;
			}
		}

		public override void SetFocus ()
		{
			base.SetFocus();

			title.Focus();
			title.Select();
		}
	}
}