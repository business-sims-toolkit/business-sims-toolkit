using System;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;
using System.IO;

using LibCore;
using GameManagement;
using CoreUtils;

namespace GameDetails
{
	/// <summary>
	/// Summary description for PSGameDetails.
	/// </summary>
	public class Cloud_Choice_GameDetails : HPGameDetails
	{
		protected Label BookingCodeLabel;
		protected TextBox tbBookingCode;

		protected Label BillableLabel;
		protected ComboBox cbBillable;

		protected Label CostCentreLabel;
		protected TextBox tbCostCentre;

		protected Label PartnerNameLabel;
		protected TextBox tbPartnerName;

		protected Label PartnerNumberLabel;
		protected TextBox tbPartnerNumber;

		public Cloud_Choice_GameDetails (GameManagement.NetworkProgressionGameFile gameFile)
			: base(gameFile)
		{
			BookingCodeLabel = CreateLabel("Class ID", 248, 105, 80, 20);
			tbBookingCode = CreateTextBox("", 315, 105, 140 + 20, 20);

			int costCentreWidth = CoreUtils.SkinningDefs.TheInstance.GetIntData("game_details_cost_centre_width", 81);
			CostCentreLabel = CreateLabel("Cost Center", 81 - costCentreWidth, 131, costCentreWidth, 20);
			tbCostCentre = CreateTextBox("", 85, 131, 140 + 20, 20);

			PartnerNameLabel = CreateLabel("Partner", 248, 131, 80, 20);
			tbPartnerName = CreateTextBox("", 315, 131, 140 + 20, 20);

			PartnerNumberLabel = CreateLabel("Partner No", 248, 156, 80, 20);
			tbPartnerNumber = CreateTextBox("", 315, 156, 140 + 20, 20);

			BillableLabel = CreateLabel("Billable", 0, 131 - 25, 80, 20);
			cbBillable = new ComboBox();
			cbBillable.Font = labelFont;
			cbBillable.Location = new Point(85, 131 - 25);
			cbBillable.Size = new Size(160, 20);
			cbBillable.TabIndex = 2;
			cbBillable.Items.Add("Yes");
			cbBillable.Items.Add("No");
			cbBillable.DropDownStyle = ComboBoxStyle.DropDownList;
			panel.Controls.Add(cbBillable);

			//need to stretch the left column to allow good display of the ca purposes 
			//"Marketing event (Tradeshow)" is quite long 
			if (this.purpose != null)
			{
				this.purpose.Width = this.purpose.Width + 20;
			}
			if (this.tbBookingCode != null)
			{
				this.tbBookingCode.Width = this.tbBookingCode.Width + 20;
			}
			if (this.venue != null)
			{
				this.venue.Width = this.venue.Width + 20;
				if (tbPartnerName != null)
				{
					this.tbPartnerName.Width = this.venue.Width;
				}
				if (tbPartnerNumber != null)
				{
					this.tbPartnerNumber.Width = this.venue.Width;
				}
			}

			if (this.client != null)
			{
				this.client.Width = this.client.Width + 20;
			}
			if (this.notes != null)
			{
				this.notes.Width = this.notes.Width + 20;
			}
			//shuffle the tab indexes so that the tab order is correct
			if (this.tbBookingCode != null & this.notes != null)
			{
				int tb = this.tbBookingCode.TabIndex;
				this.tbBookingCode.TabIndex = this.notes.TabIndex;
				this.cbBillable.TabIndex = 4;//this.tbBookingCode.TabIndex + 1;
				this.tbCostCentre.TabIndex = this.cbBillable.TabIndex + 1;
				this.venue.TabIndex = this.tbCostCentre.TabIndex + 1;
				this.client.TabIndex = this.venue.TabIndex + 1;
				this.players.TabIndex = this.client.TabIndex + 1;
				this.purpose.TabIndex = this.players.TabIndex + 1;



				this.tbPartnerName.TabIndex = 11;//this.tbCostCentre.TabIndex + 1;
				this.tbPartnerNumber.TabIndex = 12;//this.tbPartnerName.TabIndex + 1;
				this.notes.TabIndex = this.tbPartnerNumber.TabIndex + 1;
			}
			//need to shift the 2nd columns left (to allow the error spot to appear)

			Change_Location_Left(this.purposeLabel, 250);
			Change_Location_Left(this.purpose, 330);

			Change_Location_Left(this.BookingCodeLabel, 250);
			Change_Location_Left(this.tbBookingCode, 330);

			Change_Location_Left(this.venueLabel, 250);
			Change_Location_Left(this.venue, 330);

			Change_Location_Left(this.purposeLabel, 250);
			Change_Location_Left(this.purpose, 330);

			Change_Location_Left(this.PartnerNameLabel, 250);
			Change_Location_Left(this.tbPartnerName, 330);

			Change_Location_Left(this.PartnerNumberLabel, 250);
			Change_Location_Left(this.tbPartnerNumber, 330);

			Change_Location_Left(this.clientLabel, 250);
			Change_Location_Left(this.client, 330);

			Change_Location_Left(this.playersLabel, 250);
			Change_Location_Left(this.players, 330);

			notesLabel.Location = new Point(0, 132 + 50);
			notes.Location = new Point(85, 131 + 50);
			notes.Width = 425;
			notes.Height = notes.Height;

			LoadData();

			title.Width = tbCostCentre.Width;
			place.Width = tbCostCentre.Width;
			georegion.Width = tbCostCentre.Width;
			country.Width = tbCostCentre.Width;
			cbBillable.Width = tbCostCentre.Width;

			venue.Width = tbBookingCode.Width;
			client.Width = tbBookingCode.Width;
			purpose.Width = tbBookingCode.Width;
			tbPartnerName.Width = tbBookingCode.Width;
			tbPartnerNumber.Width = tbBookingCode.Width;
		}

		void Change_Location_Left (Control ct, int left)
		{
			if (ct != null)
			{
				ct.Left = left;
			}
		}

		public override void LoadData ()
		{
			string tmpRegionName = string.Empty;
			string tmpCountryName = string.Empty;

			string fileName = _gameFile.Dir + "\\global\\details.xml";
			if (File.Exists(fileName))
			{
				XmlDocument xdoc = new XmlDocument();
				xdoc.Load(fileName);
				title.Text = CoreUtils.XMLUtils.GetElementString(xdoc.DocumentElement, "Title");
				place.Text = CoreUtils.XMLUtils.GetElementString(xdoc.DocumentElement, "Location");
				venue.Text = CoreUtils.XMLUtils.GetElementString(xdoc.DocumentElement, "Venue");
				client.Text = CoreUtils.XMLUtils.GetElementString(xdoc.DocumentElement, "Client");
				players.Text = CoreUtils.XMLUtils.GetElementString(xdoc.DocumentElement, "Players");
				tmpCountryName = CoreUtils.XMLUtils.GetElementString(xdoc.DocumentElement, "Country");

				try
				{
					if (xdoc.DocumentElement.SelectSingleNode("GeoRegion") != null)
					{
						tmpRegionName = CoreUtils.XMLUtils.GetElementString(xdoc.DocumentElement, "GeoRegion");
					}

					if ((xdoc.DocumentElement.SelectSingleNode("CostCentre") != null) && (tbCostCentre != null))
					{
						tbCostCentre.Text = CoreUtils.XMLUtils.GetElementString(xdoc.DocumentElement, "CostCentre");
					}

					if ((xdoc.DocumentElement.SelectSingleNode("PartnerName") != null) && (tbPartnerName != null))
					{
						tbPartnerName.Text = CoreUtils.XMLUtils.GetElementString(xdoc.DocumentElement, "PartnerName");
					}

					if ((xdoc.DocumentElement.SelectSingleNode("PartnerNumber") != null) && (tbPartnerName != null))
					{
						tbPartnerNumber.Text = CoreUtils.XMLUtils.GetElementString(xdoc.DocumentElement, "PartnerNumber");
					}

					//handling the purpose (which is a combobox)
					string PurposeValue = "";
					if (xdoc.DocumentElement.SelectSingleNode("Purpose") != null)
					{
						PurposeValue = CoreUtils.XMLUtils.GetElementString(xdoc.DocumentElement, "Purpose");
					}
					if (this.purpose != null)
					{
						for (int step = 0; step < this.purpose.Items.Count; step++)
						{
							string item_name = (string) this.purpose.Items[step];
							if (item_name.Equals(PurposeValue, StringComparison.InvariantCultureIgnoreCase))
							{
								this.purpose.SelectedIndex = step;
							}
						}
					}

					//handling the Billable (which is a combobox)
					string BillableValue = "";
					if (xdoc.DocumentElement.SelectSingleNode("Billing") != null)
					{
						BillableValue = CoreUtils.XMLUtils.GetElementString(xdoc.DocumentElement, "Billing");
					}

					if (cbBillable != null)
					{
						for (int step = 0; step < this.cbBillable.Items.Count; step++)
						{
							string item_name = (string) this.cbBillable.Items[step];
							if (item_name.Equals(BillableValue, StringComparison.InvariantCultureIgnoreCase))
							{
								this.cbBillable.SelectedIndex = step;
							}
						}
					}

					if (xdoc.DocumentElement.SelectSingleNode("Notes") != null)
					{
						notes.Text = CoreUtils.XMLUtils.GetElementString(xdoc.DocumentElement, "Notes");
					}

					if (xdoc.DocumentElement.SelectSingleNode("ClassIdent") != null)
					{
						if (tbBookingCode != null)
						{
							tbBookingCode.Text = CoreUtils.XMLUtils.GetElementString(xdoc.DocumentElement, "ClassIdent");
						}
					}
					//gameType.Text = CoreUtils.XMLUtils.GetElementString(xdoc.DocumentElement, "GameType");
				}
				catch
				{
					// Old game with no notes section!
				}
			}
			LoadGeoData(tmpRegionName, tmpCountryName);
		}

		public override bool SaveData ()
		{
			ErrorClear();

			if (title.Text == "") return Error(title, "Title Must Be Entered.");
			if (place.Text == "") return Error(place, "Place Must Be Entered.");
			if (venue.Text == "") return Error(venue, "Venue Must Be Entered.");
			if (client.Text == "") return Error(client, "Client Must Be Entered.");
			if (players.Text == "") return Error(players, "Players Must Be Entered.");
			if (tbCostCentre.Text == "") return Error(tbCostCentre, "Cost Center Must Be Entered.");
			if (tbBookingCode.Text == "") return Error(tbBookingCode, "Class ID Must Be Entered.");
			if (this.tbPartnerName.Text == "") return Error(this.tbPartnerName, "Player Name Must Be Entered.");
			if (this.tbPartnerNumber.Text == "") return Error(this.tbPartnerNumber, "Player Number Must Be Entered.");

			int numPlayers = CONVERT.ParseInt(players.Text);
			if (numPlayers <= 0) return Error(players, "Players Must Be Non-Zero.");

			if (georegion.SelectedIndex == -1) return Error(georegion, "Region Must Be Entered.");
			if (country.SelectedIndex == -1) return Error(country, "Country Must Be Entered.");
			if (purpose.SelectedIndex == -1) return Error(purpose, "Purpose Must Be Entered.");
			if (this.cbBillable.SelectedIndex == -1) return Error(this.cbBillable, "Billable Must Be Entered.");

			if (originalTitle != title.Text)
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
//				_gameFile.License.ChangeFileName(Path.GetFileName(newFullName));
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

			if (this.cbBillable.SelectedIndex == -1)
			{
				CoreUtils.XMLUtils.CreateElementString(root, "Billing", "");
			}
			else
			{
				CoreUtils.XMLUtils.CreateElementString(root, "Billing", (string) this.cbBillable.Items[this.cbBillable.SelectedIndex]);
			}

			CoreUtils.XMLUtils.CreateElementString(root, "PartnerName", this.tbPartnerName.Text);
			CoreUtils.XMLUtils.CreateElementString(root, "PartnerNumber", this.tbPartnerNumber.Text);
			CoreUtils.XMLUtils.CreateElementString(root, "CostCentre", this.tbCostCentre.Text);
			//
			CoreUtils.XMLUtils.CreateElementString(root, "GameType", "ITIL");
			CoreUtils.XMLUtils.CreateElementString(root, "Purpose", (string) purpose.Items[purpose.SelectedIndex]);
			CoreUtils.XMLUtils.CreateElementString(root, "Notes", notes.Text);
			CoreUtils.XMLUtils.CreateElementString(root, "ClassIdent", this.tbBookingCode.Text);

			xdoc.Save(_gameFile.Dir + "\\global\\details.xml");

			return true;
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing)
			{
				BookingCodeLabel.Dispose();
				tbBookingCode.Dispose();
			}

			base.Dispose(disposing);
		}


		public override bool ValidateFields (bool reportErrors = true)
		{
			ErrorClear();

			if (title.Text == "") return Error(title, "Title Must Be Entered.");
			if (place.Text == "") return Error(place, "Place Must Be Entered.");
			if (venue.Text == "") return Error(venue, "Venue Must Be Entered.");
			if (client.Text == "") return Error(client, "Client Must Be Entered.");
			if (players.Text == "") return Error(players, "Players Must Be Entered.");
			if (tbBookingCode.Text == "") return Error(tbBookingCode, "Class ID Must Be Entered.");
			if (this.tbPartnerName.Text == "") return Error(this.tbPartnerName, "Player Name Must Be Entered.");
			if (this.tbPartnerNumber.Text == "") return Error(this.tbPartnerNumber, "Player Number Must Be Entered.");

			int numPlayers = CONVERT.ParseInt(players.Text);
			if (numPlayers <= 0) return Error(players, "Players Must Be Non-Zero.");

			if (georegion.SelectedIndex == -1) return Error(georegion, "Region Must Be Entered.");
			if (country.SelectedIndex == -1) return Error(country, "Country Must Be Entered.");
			if (purpose.SelectedIndex == -1) return Error(purpose, "Purpose Must Be Entered.");
			if (this.cbBillable.SelectedIndex == -1) return Error(this.cbBillable, "Billable Must Be Entered.");

			if (SaveData())
			{
				return true;
			}
			return false;
		}

	}
}
