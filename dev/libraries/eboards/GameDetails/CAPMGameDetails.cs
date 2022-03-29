using System;
using System.Drawing;
using System.Xml;
using System.IO;

using LibCore;
using GameManagement;
using CoreUtils;

namespace GameDetails
{
	public class CAPMGameDetails : HPGameDetails
	{

		public CAPMGameDetails(GameManagement.NetworkProgressionGameFile gameFile) : base(gameFile)
		{

			//need to stretch the left column to allow good display of the ca purposes 
			//"Marketing event (Tradeshow)" is quite long 
			if (this.purpose != null)
			{
				this.purpose.Width = this.purpose.Width + 20;
			}
			if (this.venue != null)
			{
				this.venue.Width = this.venue.Width + 20;
			}
			if (this.client != null)
			{
				this.client.Width = this.client.Width + 20;
			}
			if (this.notes != null)
			{
				this.notes.Width = this.notes.Width + 20;
			}
			//need to shift the 2nd columns left (to allow the error spot to appear)

			if (this.purpose != null)
			{
				this.purpose.Left = this.purpose.Left - 10;
			}
			//if (this.tbBookingCode != null)
			//{
			//  this.tbBookingCode.Left = this.tbBookingCode.Left - 10;
			//}
			if (this.venue != null)
			{
				this.venue.Left = this.venue.Left - 10;
			}
			if (this.client != null)
			{
				this.client.Left = this.client.Left - 10;
			}
//			if (this.notes != null)
//			{
//				this.notes.Left = this.notes.Left - 10;
//			}
			if (this.players != null)
			{
				this.players.Left = this.players.Left - 10;
			}


			if (this.purposeLabel != null)
			{
				this.purposeLabel.Left = this.purposeLabel.Left - 8;
			}
			if (this.venueLabel != null)
			{
				this.venueLabel.Left = this.venueLabel.Left - 8;
			}
			if (this.clientLabel != null)
			{
				this.clientLabel.Left = this.clientLabel.Left - 8;
			}
			if (this.notesLabel != null)
			{
				this.notesLabel.Left = this.notesLabel.Left - 8;
			}
			if (this.playersLabel != null)
			{
				this.playersLabel.Left = this.playersLabel.Left - 8;
			}

			notesLabel.Location = new Point(0,132);
			notes.Location = new Point(85,131);

			LoadData();
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
					if(xdoc.DocumentElement.SelectSingleNode("GeoRegion") != null)
					{
						tmpRegionName = CoreUtils.XMLUtils.GetElementString(xdoc.DocumentElement, "GeoRegion");
					}

					if(xdoc.DocumentElement.SelectSingleNode("Notes") != null)
					{
						notes.Text = CoreUtils.XMLUtils.GetElementString(xdoc.DocumentElement, "Notes");
					}
				}
				catch
				{
					// Old game with no notes section!
				}
			}
			LoadGeoData(tmpRegionName, tmpCountryName); 
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

			//
			CoreUtils.XMLUtils.CreateElementString(root, "GameType", "ITIL");
			CoreUtils.XMLUtils.CreateElementString(root, "Purpose", (string) purpose.Items[purpose.SelectedIndex]);
			CoreUtils.XMLUtils.CreateElementString(root, "Notes", notes.Text);

			xdoc.Save(_gameFile.Dir + "\\global\\details.xml");

			return true;
		}
	}
}