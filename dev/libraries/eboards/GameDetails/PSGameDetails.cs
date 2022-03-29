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
	public class PSGameDetails : HPGameDetails
	{
		class custom_info
		{
			public string short_name;
			public string file_path;

			override public string ToString ()
			{
				return short_name;
			}
		};

		IGameLoader gamePanel;

		protected Label TypeLabel;
		protected ComboBox gameType;
		protected Label CustomLabel;
		protected ComboBox gameCustomList;
		protected Button btn_LoadCustom;

		protected Label chargeCompanyLabel;
		protected TextBox chargeCompanyBox;

		protected ComboBox FileSelector;
		protected string lastfivenames_filename = "";

		protected bool ignoreListSelect = false;
		protected bool ignoreTypeSelect = false;

		protected bool allowLean;
		protected bool allowCustom;

		public override string GameChargeCompany => chargeCompanyBox.Text;


		public PSGameDetails (GameManagement.NetworkProgressionGameFile gameFile, IGameLoader gamePanel)
			: base(gameFile)
		{
			this.gamePanel = gamePanel;

			//KnownFile Names 
			lastfivenames_filename = LibCore.AppInfo.TheInstance.Location + "\\data\\eval_file_list.xml";

			BuildNewControls();

			ExtractLastFiveEvalFileNames(lastfivenames_filename);

			//Shift the first column over by 10 pixels
			titleLabel.Width = titleLabel.Width + 10;
			placeLabel.Width = placeLabel.Width + 10;
			countryLabel.Width = countryLabel.Width + 10;
			geoRegionLabel.Width = geoRegionLabel.Width + 10;
			title.Left = title.Left + 10;
			place.Left = place.Left + 10;
			country.Left = country.Left + 10;
			georegion.Left = georegion.Left + 10;

			notes.Left = notes.Left + 10;
			notes.Width = notes.Width - 10;
			notes.Top = chargeCompanyBox.Bottom + (chargeCompanyBox.Top - purpose.Bottom);

			notesLabel.Left = notesLabel.Left + 10;
			notesLabel.Top = notes.Top + 1;

			TypeLabel.Top = notes.Bottom + (chargeCompanyBox.Top - purposeLabel.Bottom);
			gameType.Top = notes.Bottom + (chargeCompanyLabel.Top - purposeLabel.Bottom);

			CustomLabel.Top = TypeLabel.Bottom + (countryLabel.Top - geoRegionLabel.Bottom);
			gameCustomList.Top = gameType.Bottom + (country.Top - georegion.Bottom);
			btn_LoadCustom.Top = gameCustomList.Top;

			notes.Text = "Charge company: This field should be used by freelance facilitators to confirm the company which is sponsoring the simulation delivery.";

			title.TabIndex = 0;
			place.TabIndex = 1;
			georegion.TabIndex = 2;
			country.TabIndex = 3;
			venue.TabIndex = 4;
			client.TabIndex = 5;
			players.TabIndex = 6;
			purpose.TabIndex = 7;
			chargeCompanyBox.TabIndex = 8;
			notes.TabIndex = 9;
			gameType.TabIndex = 10;
			gameCustomList.TabIndex = 11;
			btn_LoadCustom.TabIndex = 12;

			LoadData();

			//Now load the actual data 
			//LoadData();
			ConfigureNewControls();
		}

		public new virtual void BuildNewControls ()
		{
			TypeLabel = CreateLabel("Game Type", 0,105, 88, 20);
			//TypeLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			TypeLabel.Visible = true;
			
			gameType = new ComboBox();
			gameType.Font = labelFont;
			gameType.Location = new Point(85+10,105);
			gameType.Size = new Size(140+20,20);
			gameType.DropDownStyle = ComboBoxStyle.DropDownList;
			gameType.Items.Add("ITIL");
			gameType.Items.Add("ISO");

			allowLean = SkinningDefs.TheInstance.GetBoolData("game_type_allow_lean", true);
			if (allowLean)
			{
				gameType.Items.Add("Lean");
			}

			allowCustom = SkinningDefs.TheInstance.GetBoolData("game_type_allow_custom", true);

			if (allowCustom)
			{
				gameType.Items.Add("Custom");
			}

			gameType.SelectedIndex = 0;
			gameType.TabIndex = 4;
			panel.Controls.Add(gameType);
			gameType.SelectedIndexChanged +=gameType_SelectedIndexChanged;
			gameType.Visible = true;

			CustomLabel = CreateLabel("Eval. File", 0, 105 + 25, 88, 20);
			CustomLabel.Visible = true;

			gameCustomList = new ComboBox();
			gameCustomList.Font = labelFont;
			gameCustomList.Location = new Point(85 + 10, 105 + 25);
			gameCustomList.Size = new Size(140 + 20, 25);
			gameCustomList.DropDownStyle = ComboBoxStyle.DropDownList;
			gameCustomList.Visible = true;
			gameCustomList.Enabled = false;
			gameCustomList.SelectedIndexChanged += gameCustomList_SelectedIndexChanged;
			panel.Controls.Add(gameCustomList);

			gameCustomList.Visible = allowCustom;

			chargeCompanyBox = new TextBox ();
			chargeCompanyBox.Font = venue.Font;
			chargeCompanyBox.Location = new Point(purpose.Left, purpose.Bottom + (purpose.Top - players.Bottom));
			chargeCompanyBox.Size = venue.Size;
			panel.Controls.Add(chargeCompanyBox);

			chargeCompanyLabel = CreateLabel("Charge Company", purposeLabel.Right - 250, chargeCompanyBox.Top + (geoRegionLabel.Top - georegion.Top), 250, purposeLabel.Height);
		
			btn_LoadCustom = new Button();
			btn_LoadCustom.Text = "Load Custom Evaluation";
		    if (SkinningDefs.TheInstance.GetBoolData("windows_buttons_styled", true))
		    {
		        btn_LoadCustom.BackColor =
		            SkinningDefs.TheInstance.GetColorDataGivenDefault("windows_button_colour", Color.LightSteelBlue);
		        btn_LoadCustom.ForeColor =
		            SkinningDefs.TheInstance.GetColorDataGivenDefault("windows_button_foreground_colour", Color.White);
		    }
		    btn_LoadCustom.Size = new Size(180,25);
			btn_LoadCustom.Location = new Point(260+12, 130);
			btn_LoadCustom.Click += btn_LoadCustom_Click;
			btn_LoadCustom.Visible = allowCustom;
			btn_LoadCustom.Enabled = true;
			panel.Controls.Add(btn_LoadCustom);

			//ExtractLastFiveEvalFileNames(lastfivenames_filename);
		}

		protected void ConfigureNewControls()
		{
			ConfigureNewControls(true);
		}

		protected virtual void ConfigureNewControls(bool recurse)
		{
			// Now update the Controls 
			switch (gameEvalType)
			{
				case em_GameEvalType.ITIL:
					gameType.SelectedIndex = gameType.Items.IndexOf("ITIL");
					break;

				case em_GameEvalType.ISO_20K:
					gameType.SelectedIndex = gameType.Items.IndexOf("ISO");
					break;

				case em_GameEvalType.LEAN:

					gameType.SelectedIndex = gameType.Items.IndexOf("Lean");
					break;

				default:
					gameType.SelectedIndex = gameType.Items.IndexOf("Custom");
					//
					// Make sure we select the correct option in our drop down combo box.
					//
					RefreshList(stripToShortName(gameEval_CustomName), gameEval_CustomName);
					ignoreListSelect = true;
					gameCustomList.SelectedIndex = 0;
					ignoreListSelect = false;
					break;
			}

			if (recurse)
			{
				UpdateCustomEvaluationControls();
			}
		}

		#region Custom File List Methods

		string stripToShortName(string fullfilename)
		{
			return (Path.GetFileNameWithoutExtension(fullfilename));
		}

		void ExtractLastFiveEvalFileNames(string storage_name)
		{
			if (File.Exists(storage_name))
			{
				gameCustomList.Items.Clear();

				//Extract the data from the file into a xml doc
				System.IO.StreamReader file = new System.IO.StreamReader(storage_name);
				string data = file.ReadToEnd();
				BasicXmlDocument doc = LibCore.BasicXmlDocument.Create(data);
				file.Close();
				file = null;

				XmlNode rootNode = doc.DocumentElement;

				// custom_info
				// gameCustomList

				if (rootNode != null)
				{
					foreach (XmlNode xm in (rootNode.ChildNodes))
					{
						if(xm.Name == "file")
						{
							string fullname = xm.InnerText;
							string shortname = stripToShortName(fullname);

							if(File.Exists(fullname))
							{
								custom_info ci = new custom_info();
								ci.short_name = shortname;
								ci.file_path = fullname;
								gameCustomList.Items.Add(ci);
							}
						}
					}
				}
			}
		}

		public void SaveLast5FileData()
		{
			try
			{
				XmlDocument xdoc = new XmlDocument();
				XmlNode root = (XmlNode) xdoc.CreateElement("root");
				xdoc.AppendChild(root);

				int count = 0;

				foreach(custom_info ci in gameCustomList.Items)
				{
					// Use an XML Document class so that XML is escaped.
					if(count < 5)
					{
						root.AppendChild( XMLUtils.CreateElementString(root, "file", ci.file_path) );
					}
					++count;
				}

				xdoc.Save(lastfivenames_filename);
			}
			catch(Exception)
			{
				// TODO : log that we failed to write the file.
			}
		}

		/// <summary>
		/// Changing the current file 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		protected void gameCustomList_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (ignoreListSelect == false)
			{
				if(gameCustomList.SelectedItem != null)
				{
					custom_info ci = (custom_info) gameCustomList.SelectedItem;
					if(!handleNewFile(ci.file_path, true))
					{
						MessageBox.Show(TopLevelControl, "File " + ci.file_path + " is not a valid custom evaluation file.", "File Error");
						gameCustomList.Items.Remove(gameCustomList.SelectedItem);
						UpdateCustomEvaluationControls();
					}
				}
			}
		}

		protected void UpdateCustomEvaluationControls()
		{
			foreach (custom_info item in new System.Collections.ArrayList (gameCustomList.Items))
			{
				if (! File.Exists(item.file_path))
				{
					gameCustomList.Items.Remove(item);
				}
			}

			if ((gameCustomList.Items.Count > 0) || (gameEvalType == em_GameEvalType.CUSTOM))
			{
				if (! gameType.Items.Contains("Custom"))
				{
					gameType.Items.Add("Custom");
				}

				if (! DoesCustomListContainCurrentOption())
				{
					ConfigureNewControls(false);
				}

				if ((gameType.SelectedItem != null) && (gameType.SelectedItem.ToString() == "Custom"))
				{
					gameCustomList.Enabled = true;
					if ((gameCustomList.Items.Count) > 0 && (gameCustomList.SelectedIndex == -1))
					{
						gameCustomList.SelectedIndex = 0;
					}
				}
				else
				{
					gameCustomList.Enabled = false;
					gameCustomList.SelectedIndex = -1;
				}
			}
			else
			{
				if (gameType.Items.Contains("Custom"))
				{
					gameType.Items.Remove("Custom");
				}

				gameCustomList.Enabled = false;
				gameCustomList.SelectedIndex = -1;

				if ((gameType.SelectedItem != null) && (gameType.SelectedItem.ToString() == "Custom"))
				{
					gameType.SelectedIndex = 0;
				}
			}
		}

		bool DoesCustomListContainCurrentOption ()
		{
			foreach (custom_info ci in gameCustomList.Items)
			{
				if (ci.short_name == stripToShortName(gameEval_CustomName))
				{
					return true;
				}
			}

			return false;
		}

		void RefreshList(string shortname, string newfilename)
		{
			int index_to_remove = -1;
			int count = 0;

			foreach(custom_info ci in gameCustomList.Items)
			{
				if(shortname == ci.short_name)
				{
					index_to_remove = count;
				}
				++count;
			}
			if(-1 != index_to_remove)
			{
				gameCustomList.Items.RemoveAt( index_to_remove );
			}
			//
			custom_info ci2 = new custom_info();
			ci2.short_name = shortname;
			ci2.file_path = newfilename;
			gameCustomList.Items.Insert( 0, ci2 );

			// Save the list!
			this.SaveLast5FileData();
		}

		bool handleNewFile(string newfilename, bool refreshList)
		{
			if (File.Exists(newfilename))
			{
				MaturityInfoFile report_file;
				string eval_file;
				string ignore_file;

				// Do a quick check to see if the file is at least valid XML.
				try
				{
					report_file = new MaturityInfoFile(newfilename);
					eval_file = report_file.GetFile("eval_wizard_custom.xml");
					//
					XmlDocument xdoc = new XmlDocument();
					xdoc.Load(eval_file);
					//
					ignore_file = report_file.GetFile("Eval_States.xml");
					//
					XmlDocument xdoc2 = new XmlDocument();
					xdoc2.Load(ignore_file);
				}
				catch(Exception)
				{
					// TODO - Log.
					return false;
				}

				gameEval_CustomName = newfilename;

				gameEvalType = em_GameEvalType.CUSTOM;
				gameEval_CustomName = newfilename;
				this.ignoreTypeSelect = true;
				ConfigureNewControls();
				this.ignoreTypeSelect = false;

				string shortname = stripToShortName(newfilename);
				//copy the file into the base directory 
				string destfileName_base = _gameFile.Dir + "\\global\\eval_custom.xml";
				File.Copy(eval_file,destfileName_base, true);

				string destfileName_base2 = _gameFile.Dir + "\\global\\Eval_States.xml";
				File.Copy(ignore_file,destfileName_base2, true);

				// copy the file into _all_ dirs. If a dir doesn't exist then just move on.
				for(int i=1; i<=5; ++i)
				{
					string dest_file = _gameFile.GetRoundFile(i, "eval_custom.xml", GameManagement.GameFile.GamePhase.OPERATIONS);
					//
					try
					{
						if(Directory.Exists( Path.GetDirectoryName(dest_file)))
						{
							File.Copy(eval_file,dest_file, true);
						}
					}
					catch(Exception)
					{
						// NOP. TODO - Should log!
					}
				}

				gameType.SelectedIndex = gameType.Items.IndexOf("Custom");

				RefreshList(shortname, newfilename);
			}
			else
			{
				MessageBox.Show(TopLevelControl, "File Not Found, please select another file","Evaluation File Not Found");
			}

			//if (enableList)
			{
				gameCustomList.Enabled = true;
				ignoreListSelect = true;
				gameCustomList.SelectedIndex = 0;
				ignoreListSelect = false;
			}

			return true;
		}

		#endregion Custom File List Methods

		protected void gameType_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (ignoreTypeSelect == false)
			{
				switch (gameType.SelectedItem.ToString().ToUpper())
				{
					case "ITIL":
						gameCustomList.Enabled = false;
						gameEvalType = em_GameEvalType.ITIL;
						break;

					case "ISO":
						gameCustomList.Enabled = false;
						gameEvalType = em_GameEvalType.ISO_20K;
						break;

					case "LEAN":
						gameCustomList.Enabled = false;
						gameEvalType = em_GameEvalType.LEAN;
						break;

					default:
						gameEvalType = em_GameEvalType.CUSTOM;

						ignoreListSelect = true;
						gameCustomList.SelectedIndex = -1;
						ignoreListSelect = false;
						gameCustomList.Enabled = true;
						break;
				}
			}

			UpdateCustomEvaluationControls();
		}

		protected void btn_LoadCustom_Click(object sender, EventArgs e)
		{
			LoadNewCustomFile();
		}

		protected bool LoadNewCustomFile()
		{
			OpenFileDialog fdlg = new OpenFileDialog(); 
			fdlg.Title = "Load Maturity Template"; 
			fdlg.Filter = "Maturity Template (*.mrt)|*.mrt";
			fdlg.FilterIndex = 1 ; 
			fdlg.RestoreDirectory = true ; 
			if(fdlg.ShowDialog(TopLevelControl) == DialogResult.OK) 
			{
				if(!handleNewFile(fdlg.FileName, true))
				{
					MessageBox.Show(TopLevelControl, "File " + fdlg.FileName + " is not a valid maturity template file.", "File Error");
					return false;
				}
				// Make sure our game type selector is set to custom.
				// need to protect againt cyclic calls.
				// gameType.SelectedIndex = 1;
				return true;
			}

			return false;
		}

		/// <summary>
		/// This extracts the game details from the file
		/// When opeing a new game, there is no file so the attributes just have thier defaults
		/// </summary>
		public override void LoadData ()
		{
			string tmpRegionName = string.Empty;
			string tmpCountryName = string.Empty;

			//Default 
			gameEvalType = em_GameEvalType.ITIL;
			gameEval_CustomName = "";

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

				if (chargeCompanyBox != null)
				{
					bool exists;
					chargeCompanyBox.Text = CoreUtils.XMLUtils.GetElementStringWithCheck(xdoc.DocumentElement, "ChargeCompany", out exists);
				}
				
				//Handling the GameEval type 
				//Default to Undefined (which may not exist)
				bool existance = false;
				gameEvalType = em_GameEvalType.ITIL;
				string evaltypestr = CoreUtils.XMLUtils.GetElementStringWithCheck(xdoc.DocumentElement, "evaltype", out existance);
				if (existance == true)
				{
					if (evaltypestr != "")
					{
						gameEvalType = (em_GameEvalType) Enum.Parse(typeof(em_GameEvalType),evaltypestr.ToUpper());
					}
				}
				//IF UNDEFINED THEN remap to ITIL
				if ((int)gameEvalType == (int)em_GameEvalType.UNDEFINED)
				{
					gameEvalType = em_GameEvalType.ITIL;
				}

				//Handling the default custom name 
				gameEval_CustomName = "";
				gameEval_CustomName = CoreUtils.XMLUtils.GetElementStringWithCheck(xdoc.DocumentElement, "evalname_custom", out existance);

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

					if(gameType != null)
					{
						XmlNode nn = xdoc.DocumentElement.SelectSingleNode("GameType");
						if(nn != null)
						{
							gameType.Text = CoreUtils.XMLUtils.GetElementString(xdoc.DocumentElement, "GameType");
						}
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

			if((int)gameEvalType == (int)em_GameEvalType.CUSTOM) 
			{
				if (gameEval_CustomName == "")
				{
					return Error(gameCustomList, "No Custom File Defined.");
				}
			}

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

			//Standard
			CoreUtils.XMLUtils.CreateElementString(root, "Title", title.Text);
			CoreUtils.XMLUtils.CreateElementString(root, "Location", place.Text);
			CoreUtils.XMLUtils.CreateElementString(root, "Venue", venue.Text);
			CoreUtils.XMLUtils.CreateElementString(root, "Client", client.Text);
			CoreUtils.XMLUtils.CreateElementString(root, "Players", players.Text);
			CoreUtils.XMLUtils.CreateElementString(root, "ChargeCompany", chargeCompanyBox.Text);

			//New GameType Structures
			CoreUtils.XMLUtils.CreateElementString(root, "evalname_custom", gameEval_CustomName);
			if ((int)gameEvalType == (int)em_GameEvalType.UNDEFINED)
			{
				gameEvalType = em_GameEvalType.ITIL;
			}
			string type_full_ename = Enum.GetName(typeof(em_GameEvalType),gameEvalType);
			CoreUtils.XMLUtils.CreateElementString(root, "evaltype", type_full_ename);

			SaveLast5FileData();
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
			if (gameType.SelectedIndex == -1)
			{
				CoreUtils.XMLUtils.CreateElementString(root, "GameType", "");
			}
			else
			{
				CoreUtils.XMLUtils.CreateElementString(root, "GameType", (string) gameType.Items[gameType.SelectedIndex]);
			}
			CoreUtils.XMLUtils.CreateElementString(root, "Purpose", (string) purpose.Items[purpose.SelectedIndex]);
			CoreUtils.XMLUtils.CreateElementString(root, "Notes", notes.Text);

			xdoc.Save(_gameFile.Dir + "\\global\\details.xml");

			_gameFile.Game_Eval_Type = gameEvalType;

			gamePanel.RefreshMaturityScoreSet();

			return true;
		}

		protected override void Dispose(bool disposing)
		{
			if(disposing)
			{

				TypeLabel.Dispose();
				gameType.Dispose();
			}

			base.Dispose (disposing);
		}

		public override void ShowSpecificParts (bool showGameSpecificParts)
		{
			base.ShowSpecificParts(showGameSpecificParts);

			if (! showGameSpecificParts)
			{
				gameType.SelectedIndex = 0;
			}

			gameType.Visible = showGameSpecificParts;
			TypeLabel.Visible = showGameSpecificParts;

			gameCustomList.Visible = showGameSpecificParts && allowCustom;
			CustomLabel.Visible = showGameSpecificParts && allowCustom;

			btn_LoadCustom.Visible = showGameSpecificParts && allowCustom;
		}
	}
}