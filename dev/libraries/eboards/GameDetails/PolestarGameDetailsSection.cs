using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.Xml;
using System.IO;

using CommonGUI;
using LibCore;
using CoreUtils;

using GameManagement;

namespace GameDetails
{
	public class PolestarGameDetailsSection : GameDeliveryDetailsSection
	{
		NetworkProgressionGameFile gameFile;

		string DetailsFile
		{
			get
			{
				return gameFile.Dir + @"\global\details.xml";
			}
		}

		ErrorProvider errorProvider;
		Control controlInError;

		Label titleLabel;
		TextBox titleBox;

		Label venueLabel;
		TextBox venueBox;

		Label townLabel;
		TextBox townBox;

		Label clientLabel;
		TextBox clientBox;

		Label regionLabel;
		ComboBox regionBox;

		Label playersLabel;
		FilteredTextBox playersBox;

		Label countryLabel;
		ComboBox countryBox;

		protected Label purposeLabel;
        protected ComboBox purposeBox;

		List<string> regions;
		Dictionary<string, List<string>> regionToCountries;

		public PolestarGameDetailsSection (NetworkProgressionGameFile gameFile)
		{
			this.gameFile = gameFile;

			errorProvider = new ErrorProvider (this);

			LoadRegionsAndCountries();
			BuildControls();

			LoadData();
		}

		protected void LoadRegionsAndCountries ()
		{
			regions = new List<string> ();
			regionToCountries = new Dictionary<string, List<string>> ();

			BasicXmlDocument xml = BasicXmlDocument.CreateFromFile(AppInfo.TheInstance.Location + @"\data\countries.xml");
			foreach (XmlElement regionElement in xml.DocumentElement.ChildNodes)
			{
				string region = BasicXmlDocument.GetStringAttribute(regionElement, "name");
				regions.Add(region);
				regionToCountries.Add(region, new List<string>());

				foreach (XmlElement countryElement in regionElement.ChildNodes)
				{
					string country = BasicXmlDocument.GetStringAttribute(countryElement, "name");
					regionToCountries[region].Add(country);
				}
			}
		}

		protected virtual void BuildControls ()
		{
			string fontName = SkinningDefs.TheInstance.GetData("fontname", "Arial Unicode MS");
			Font font = ConstantSizeFont.NewFont(fontName, 10);
		    Color labelTextColour = SkinningDefs.TheInstance.GetColorData("game_details_text_colour", Color.Black);
			titleLabel = new Label ();
			titleLabel.Font = font;
			titleLabel.Text = "Title";
            titleLabel.ForeColor = labelTextColour;
			titleLabel.TextAlign = ContentAlignment.MiddleRight;
			panel.Controls.Add(titleLabel);

			titleBox = new TextBox ();
			titleBox.Font = font;
			titleBox.MaxLength = 30;
			panel.Controls.Add(titleBox);

			venueLabel = new Label ();
			venueLabel.Font = font;
			venueLabel.Text = "Venue";
			venueLabel.TextAlign = ContentAlignment.MiddleRight;
            venueLabel.ForeColor = labelTextColour;
            panel.Controls.Add(venueLabel);

			venueBox = new TextBox ();
			venueBox.Font = font;
			venueBox.MaxLength = 30;
			panel.Controls.Add(venueBox);

			townLabel = new Label ();
			townLabel.Font = font;
			townLabel.Text = "Town / City";
			townLabel.TextAlign = ContentAlignment.MiddleRight;
            townLabel.ForeColor = labelTextColour;
            panel.Controls.Add(townLabel);

			townBox = new TextBox();
			townBox.MaxLength = 30;
			townBox.Font = font;
			panel.Controls.Add(townBox);

			clientLabel = new Label ();
			clientLabel.Font = font;
			clientLabel.Text = "Client";
			clientLabel.TextAlign = ContentAlignment.MiddleRight;
            clientLabel.ForeColor = labelTextColour;
            panel.Controls.Add(clientLabel);

			clientBox = new TextBox ();
			clientBox.Font = font;
			clientBox.MaxLength = 30;
			panel.Controls.Add(clientBox);

			regionLabel = new Label ();
			regionLabel.Font = font;
			regionLabel.Text = "Region";
			regionLabel.TextAlign = ContentAlignment.MiddleRight;
            regionLabel.ForeColor = labelTextColour;
            panel.Controls.Add(regionLabel);

			regionBox = new ComboBox ();
			regionBox.DropDownStyle = ComboBoxStyle.DropDownList;
			regionBox.Font = font;
			regionBox.SelectedValueChanged += regionBox_SelectedValueChanged;
			panel.Controls.Add(regionBox);

			playersLabel = new Label ();
			playersLabel.Font = font;
			playersLabel.Text = "Players";
			playersLabel.TextAlign = ContentAlignment.MiddleRight;
            playersLabel.ForeColor = labelTextColour;
            panel.Controls.Add(playersLabel);

			playersBox = new FilteredTextBox (TextBoxFilterType.Digits);
			playersBox.Font = font;
			panel.Controls.Add(playersBox);

			countryLabel = new Label ();
			countryLabel.Font = font;
			countryLabel.Text = "Country";
			countryLabel.TextAlign = ContentAlignment.MiddleRight;
            countryLabel.ForeColor = labelTextColour;
            panel.Controls.Add(countryLabel);

			countryBox = new ComboBox ();
			countryBox.DropDownStyle = ComboBoxStyle.DropDownList;
			countryBox.Font = font;
			panel.Controls.Add(countryBox);

			purposeLabel = new Label ();
			purposeLabel.Font = font;
			purposeLabel.Text = "Purpose";
			purposeLabel.TextAlign = ContentAlignment.MiddleRight;
            purposeLabel.ForeColor = labelTextColour;
            panel.Controls.Add(purposeLabel);

			purposeBox = new ComboBox ();
			purposeBox.DropDownStyle = ComboBoxStyle.DropDownList;
			purposeBox.Font = font;
			purposeBox.Items.AddRange(File.ReadAllLines(AppInfo.TheInstance.Location + @"\data\purposes.txt"));
			panel.Controls.Add(purposeBox);

			regionBox.Items.AddRange(regions.ToArray());

			foreach (var field in new Control [] { titleBox, venueBox, townBox, regionBox, countryBox, purposeBox, playersBox, clientBox })
			{
				field.Enabled = false;
			}

			DoLayout();
		}

		void regionBox_SelectedValueChanged (object sender, EventArgs e)
		{
			countryBox.Items.Clear();

			if (regionBox.SelectedIndex != -1)
			{
				countryBox.Items.AddRange(regionToCountries[regions[regionBox.SelectedIndex]].ToArray());
			}
		}

		protected virtual void DoLayout ()
		{
			int labelToBoxGap = 6;
			Size leftColumnLabelSize = new Size (85, 25);
			Size rightColumnLabelSize = new Size (65, leftColumnLabelSize.Height);
			Size boxSize = new Size (150, leftColumnLabelSize.Height);

			titleLabel.Location = new Point (0, 0);
			titleLabel.Size = leftColumnLabelSize;
			titleBox.Location = new Point (titleLabel.Right + labelToBoxGap, titleLabel.Top);
			titleBox.AutoSize = false;
			titleBox.Size = boxSize;
			titleBox.TabIndex = 0;

			venueLabel.Location = new Point (titleBox.Right, titleLabel.Top);
			venueLabel.Size = rightColumnLabelSize;
			venueBox.Location = new Point (venueLabel.Right + labelToBoxGap, venueLabel.Top);
			venueBox.AutoSize = false;
			venueBox.Size = boxSize;
			venueBox.TabIndex = 4;

			townLabel.Location = new Point (titleLabel.Left, titleLabel.Bottom + 2);
			townLabel.Size = leftColumnLabelSize;
			townBox.Location = new Point (townLabel.Right + labelToBoxGap, townLabel.Top);
			townBox.AutoSize = false;
			townBox.Size = boxSize;
			townBox.TabIndex = 1;

			clientLabel.Location = new Point (townBox.Right, townLabel.Top);
			clientLabel.Size = rightColumnLabelSize;
			clientBox.Location = new Point (clientLabel.Right + labelToBoxGap, clientLabel.Top);
			clientBox.AutoSize = false;
			clientBox.Size = boxSize;
			clientBox.TabIndex = 5;

			regionLabel.Location = new Point (townLabel.Left, townLabel.Bottom + 2);
			regionLabel.Size = leftColumnLabelSize;
			regionBox.Location = new Point (regionLabel.Right + labelToBoxGap, regionLabel.Top);
			regionBox.Size = boxSize;
			regionBox.TabIndex = 2;

			playersLabel.Location = new Point (regionBox.Right, regionLabel.Top);
			playersLabel.Size = rightColumnLabelSize;
			playersBox.Location = new Point (playersLabel.Right + labelToBoxGap, playersLabel.Top);
			playersBox.AutoSize = false;
			playersBox.Size = new Size (50, regionBox.Height);
			playersBox.TabIndex = 6;

			countryLabel.Location = new Point (regionLabel.Left, regionLabel.Bottom + 2);
			countryLabel.Size = leftColumnLabelSize;
			countryBox.Location = new Point (countryLabel.Right + labelToBoxGap, countryLabel.Top);
			countryBox.Size = boxSize;
			countryBox.TabIndex = 3;

			purposeLabel.Location = new Point (countryBox.Right, countryLabel.Top);
			purposeLabel.Size = rightColumnLabelSize;
			purposeBox.Location = new Point (purposeLabel.Right + labelToBoxGap, purposeLabel.Top);
			purposeBox.Size = boxSize;
			purposeBox.TabIndex = 7;

			SetSize(500, purposeBox.Bottom + 15);
		}

		public override void LoadData ()
		{
			if (File.Exists(DetailsFile))
			{
				BasicXmlDocument xml = BasicXmlDocument.CreateFromFile(DetailsFile);

				titleBox.Text = XMLUtils.GetElementString(xml.DocumentElement, "Title");
				venueBox.Text = XMLUtils.GetElementString(xml.DocumentElement, "Venue");
				townBox.Text = XMLUtils.GetElementString(xml.DocumentElement, "Location");
				clientBox.Text = XMLUtils.GetElementString(xml.DocumentElement, "Client");
				playersBox.Text = XMLUtils.GetElementString(xml.DocumentElement, "Players");
				regionBox.SelectedItem = XMLUtils.GetElementString(xml.DocumentElement, "GeoRegion");
				countryBox.SelectedItem = XMLUtils.GetElementString(xml.DocumentElement, "Country");
				purposeBox.SelectedItem = XMLUtils.GetElementString(xml.DocumentElement, "Purpose");
			}
		}

		protected void ClearError ()
		{
			if (controlInError != null)
			{
				errorProvider.SetError(controlInError, "");
				controlInError = null;
			}

			errorProvider.Clear();
		}

		protected void SetError (Control control, string error)
		{
			ClearError();

			errorProvider.SetError(control, error);
			controlInError = control;

			Expanded = true;
			ScrollControlIntoView(control);
			control.Select();
		}

		protected bool ValidateField (TextBox control, string error)
		{
			if (! control.Enabled)
			{
				return true;
			}

			if (string.IsNullOrEmpty(control.Text.Trim()))
			{
				SetError(control, error);
				return false;
			}

			return true;
		}

		protected bool ValidateField (ComboBox control, string error)
		{
			if (!control.Enabled)
			{
				return true;
			}

			if (control.SelectedIndex == -1)
			{
				SetError(control, error);
				return false;
			}

			return true;
		}

		protected bool ValidateField (Control control, bool condition, string error)
		{
			if (!control.Enabled)
			{
				return true;
			}

			if (! condition)
			{
				SetError(control, error);
				return false;
			}

			return true;
		}

		public override bool ValidateFields (bool reportErrors = true)
		{
			ClearError();

			return ValidateField(titleBox, "Title must be entered") && ValidateField(venueBox, "Venue must be entered")
				   && ValidateField(townBox, "Town must be entered") && ValidateField(clientBox, "Client must be entered")
				   && ValidateField(playersBox, "Players must be entered") && ValidateField(playersBox, CONVERT.ParseInt(playersBox.Text) > 0, "Players must be 1 or more")
				   && ValidateField(regionBox, "Region must be entered") && ValidateField(countryBox, "Country must be entered") && ValidateField(purposeBox, "Purpose must be entered");
		}

		public override bool SaveData ()
		{
			if (! ValidateFields())
			{
				return false;
			}

			BasicXmlDocument xml;
			if (File.Exists(DetailsFile))
			{
				xml = BasicXmlDocument.CreateFromFile(DetailsFile);
			}
			else
			{
				xml = BasicXmlDocument.Create();
			}
			
			XmlElement root = XMLUtils.GetOrCreateElement(xml, "details");
			root.RemoveAllAttributes();
			BasicXmlDocument.AppendAttribute(root, "type", CoreUtils.SkinningDefs.TheInstance.GetData("gametype"));

			// Special processing for the title: if it changes, we need to do a rename.
			string firstPart, lastPart;
			if (GameUtils.FileNameToGameName(Path.GetFileName(gameFile.FileName), out firstPart, out lastPart) != titleBox.Text)
			{
				DateTime creationDate = GameUtils.FileNameToCreationDate(Path.GetFileName(gameFile.FileName));

				string newFilename;
				string error;
				GameUtils.EstablishNewFileName(out newFilename, titleBox.Text, out error, creationDate.Year, creationDate.Month, creationDate.Day);

				string newPath = Path.GetDirectoryName(gameFile.FileName) + @"\" + newFilename;

				TimeManager.TheInstance.Stop();

				gameFile.Rename(newPath);
			}

			XMLUtils.GetOrCreateElement(xml.DocumentElement, "Title").InnerText = titleBox.Text;
			XMLUtils.GetOrCreateElement(xml.DocumentElement, "Venue").InnerText = venueBox.Text;
			XMLUtils.GetOrCreateElement(xml.DocumentElement, "Location").InnerText = townBox.Text;
			XMLUtils.GetOrCreateElement(xml.DocumentElement, "Client").InnerText = clientBox.Text;
			XMLUtils.GetOrCreateElement(xml.DocumentElement, "Players").InnerText = playersBox.Text;
			XMLUtils.GetOrCreateElement(xml.DocumentElement, "GeoRegion").InnerText = (string) regionBox.SelectedItem;
			XMLUtils.GetOrCreateElement(xml.DocumentElement, "Country").InnerText = (string) countryBox.SelectedItem;
			XMLUtils.GetOrCreateElement(xml.DocumentElement, "Purpose").InnerText = (string) purposeBox.SelectedItem;
			XMLUtils.GetOrCreateElement(xml.DocumentElement, "Notes").InnerText = "";

			xml.Save(DetailsFile);

			return true;
		}

		public override void ShowSpecificParts (bool showGameSpecificParts)
		{
			if (! showGameSpecificParts)
			{
				purposeBox.SelectedIndex = 0;
				for (int i = 0; i < purposeBox.Items.Count; i++)
				{
					string purposeString = (string) purposeBox.Items[i];
					if (purposeString.ToLower().Contains("foundation"))
					{
						purposeBox.SelectedIndex = i;
						break;
					}
				}
			}

			purposeBox.Visible = showGameSpecificParts;
			purposeLabel.Visible = showGameSpecificParts;
		}

		public override string GameTitle => titleBox.Text;
		public override string GameVenue => venueBox.Text;
		public override string GameLocation => townBox.Text;
		public override string GameRegion => regionBox.Text;
		public override string GameCountry => countryBox.Text;
		public override string GameClient => clientBox.Text;
		public override string GameChargeCompany => null;
		public override string GameNotes => null;
		public override string GamePurpose => purposeBox.Text;
		public override int GamePlayers => CONVERT.ParseIntSafe(playersBox.Text, 0);
	}
}