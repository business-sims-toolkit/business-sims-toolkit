using System;
using System.Collections.Generic;
using System.Xml;
using System.Drawing;
using System.Windows.Forms;
using System.IO;

using Network;
using GameManagement;
using LibCore;

namespace GameDetails
{
	public class SupportTeamDetails : GameDetailsSection
	{
		NetworkProgressionGameFile gameFile;

		List<string> platformNames;
		Dictionary<string, TextBox> platformNameToTextBox;

		ErrorProvider errorProvider;

		public SupportTeamDetails (NetworkProgressionGameFile gameFile)
		{
			this.gameFile = gameFile;

			Title = "Support Teams";
			Collapsible = true;
			Expanded = false;

			errorProvider = new ErrorProvider (this);

			platformNames = new List<string> ();
			platformNameToTextBox = new Dictionary<string, TextBox> ();

			Dictionary<string, string> platformNameToCustomName = new Dictionary<string, string> ();
			if (File.Exists(Filename))
			{
				BasicXmlDocument xml = BasicXmlDocument.CreateFromFile(Filename);
				foreach (XmlElement child in xml.DocumentElement.ChildNodes)
				{
					platformNameToCustomName.Add(child.GetStringAttribute("platform_name", ""), child.GetStringAttribute("custom_name", ""));
				}
			}

			int y = 15;
			int i = 1;
			foreach (Node platform in gameFile.NetworkModel.GetNamedNode("Platforms").GetChildrenOfType("platform"))
			{
				if (platform.GetAttribute("name") == "Cloud")
				{
					continue;
				}

				string platformName = platform.GetAttribute("name");
				platformNames.Add(platformName);

				Label label = new Label () { Font = CoreUtils.SkinningDefs.TheInstance.GetFont(10), Text = platform.GetAttribute("support_team"), TextAlign = ContentAlignment.MiddleLeft };
				panel.Controls.Add(label);
				i++;

				string customName = platform.GetAttribute("desc", platformName);
				if (platformNameToCustomName.ContainsKey(platformName))
				{
					customName = platformNameToCustomName[platformName];
				}

				int maxLength = 12;
				TextBox textBox = new TextBox () { Font = CoreUtils.SkinningDefs.TheInstance.GetFont(10),
												   Text = customName.Substring(0, Math.Min(maxLength, customName.Length)),
												   MaxLength = maxLength };
				textBox.TextChanged += textBox_TextChanged;
				panel.Controls.Add(textBox);
				platformNameToTextBox.Add(platformName, textBox);

				label.Bounds = new Rectangle (0, y, 20, 25);
				textBox.Bounds = new Rectangle (label.Right + 10, y, 200, label.Height);
				y = textBox.Bottom + 10;
			}

			SetAutoSize();
		}

		public string Filename
		{
			get
			{
				return GetFilename(gameFile);
			}
		}

		public static string GetFilename (NetworkProgressionGameFile gameFile)
		{
			return gameFile.GetGlobalFile("support_team_names.xml");
		}

		void textBox_TextChanged (object sender, EventArgs args)
		{
			OnChanged();
		}

		public override bool ValidateFields (bool reportErrors = true)
		{
			if (reportErrors)
			{
				errorProvider.Clear();
			}

			List<string> usedNames = new List<string>();
			foreach (TextBox textBox in platformNameToTextBox.Values)
			{
				if (string.IsNullOrEmpty(textBox.Text))
				{
					if (reportErrors)
					{
						errorProvider.SetError(textBox, "Blank name");
					}
					return false;
				}
				else if (usedNames.Contains(textBox.Text))
				{
					if (reportErrors)
					{
						errorProvider.SetError(textBox, "Duplicate name");
					}
					return false;
				}
			}

			return true;
		}

		public override bool SaveData ()
		{
			if (! ValidateFields())
			{
				return false;
			}

			BasicXmlDocument xml = BasicXmlDocument.Create();
			XmlElement root = xml.AppendNewChild("custom_names");

			foreach (string platformName in platformNames)
			{
				XmlElement child = root.AppendNewChild("custom_name_mapping");
				child.AppendAttribute("platform_name", platformName);
				child.AppendAttribute("custom_name", platformNameToTextBox[platformName].Text);
			}

			xml.Save(Filename);

			gameFile.Save(true);

			return true;
		}
	}
}