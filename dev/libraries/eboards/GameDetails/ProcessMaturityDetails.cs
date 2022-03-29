using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.Xml;
using System.IO;

using LibCore;
using CoreUtils;
using GameManagement;

namespace GameDetails
{
	public class ProcessMaturityDetails : GameDetailsSection
	{
		class GameTypeEntry
		{
			public string ShownName;
			public string EvalType;
			public string GameType;
			public em_GameEvalType GameEvalType;

			public GameTypeEntry (string shownName, string evalType, string gameType, em_GameEvalType gameEvalType)
			{
				ShownName = shownName;
				EvalType = evalType;
				GameType = gameType;
				GameEvalType = gameEvalType;
			}

			public override string ToString ()
			{
				return ShownName;
			}
		}

		NetworkProgressionGameFile gameFile;
		IGameLoader gamePanel;

		List<RadioButton> buttons;
		Dictionary<GameTypeEntry, RadioButton> gameTypeToButton;
		bool updatingButtons;
		RadioButton lastCheckedButton;

		List<GameTypeEntry> fixedTypes;
		GameTypeEntry typeForCurrentCustom;
		GameTypeEntry typeForLoadNew;

		string DetailsFilename
		{
			get
			{
				return gameFile.Dir + @"\global\details.xml";
			}
		}

		public ProcessMaturityDetails (NetworkProgressionGameFile gameFile, IGameLoader gamePanel)
		{
			this.gameFile = gameFile;
			this.gamePanel = gamePanel;

			Title = "Process Selection";

			fixedTypes = new List<GameTypeEntry> ();
			typeForLoadNew = null;
			typeForCurrentCustom = null;

			BuildControls();
		}

		void BuildControls ()
		{
			buttons = new List<RadioButton> ();
			gameTypeToButton = new Dictionary<GameTypeEntry, RadioButton> ();

			RebuildChoices();
		}

		void button_CheckedChanged (object sender, EventArgs e)
		{
			RadioButton button = (RadioButton) sender;

			if (button.Checked && ! updatingButtons)
			{
				GameTypeEntry type = (GameTypeEntry) button.Tag;

				SaveData(type);
				RebuildChoices();
			}
		}

		public override bool SaveData ()
		{
			SaveData(GetSelectedEntry());

			return true;
		}

		void SaveData (GameTypeEntry type)
		{
			if (type == typeForLoadNew)
			{
				string file = ChooseNewFile();

				if (! string.IsNullOrEmpty(file))
				{
					LoadNewCustomFile(file);
				}
				else
				{
					if (lastCheckedButton != null)
					{
						lastCheckedButton.Checked = true;
					}
				}
			}
			else if (type == typeForCurrentCustom)
			{
				// Nothing doing!
			}
			else
			{
				ClearCustomFileFromList();
				WriteGameDetailsFile(type.GameEvalType, type.EvalType, type.GameType, null);
			}
		}

		string ChooseNewFile ()
		{
			OpenFileDialog dialog = new OpenFileDialog ();
			dialog.Title = "Load Maturity Template";
			dialog.Filter = "Maturity Template (*.mrt)|*.mrt";

			dialog.FilterIndex = 1;
			dialog.RestoreDirectory = true;

			if (dialog.ShowDialog(TopLevelControl) == DialogResult.OK)
			{
				return dialog.FileName;
			}

			return null;
		}

		void DoLayout ()
		{
			int y = 0;

			foreach (RadioButton button in buttons)
			{
				button.Location = new Point (30, y);
				button.Size = new Size (150, 25);
				y = button.Bottom;
			}

			SetSize(500, y + 15);
		}

		public override void LoadData ()
		{
			BasicXmlDocument xml = BasicXmlDocument.LoadOrCreate(DetailsFilename);
			XmlElement root = XMLUtils.GetOrCreateElement(xml, "details");
			XmlElement evalTypeElement = XMLUtils.GetOrCreateElement(root, "evaltype");
			XmlElement gameTypeElement = XMLUtils.GetOrCreateElement(root, "GameType");
			XmlElement customEvalFilenameElement = XMLUtils.GetOrCreateElement(root, "evalname_custom");

			string evalType = evalTypeElement.InnerText;
			if ((typeForLoadNew != null) && (evalType == typeForLoadNew.EvalType))
			{
				string customFilename = customEvalFilenameElement.InnerText;
				AddCustomFileIntoList(customFilename);
			}
			else
			{
				GameTypeEntry type = GetTypeByEvalType(evalTypeElement.InnerText);

				if (type == null)
				{
					type = fixedTypes[0];
				}

				RebuildChoices();

				updatingButtons = true;
				gameTypeToButton[type].Checked = true;
				updatingButtons = false;
			}

			SaveData();
		}

		GameTypeEntry GetTypeByEvalType (string evalType)
		{
			foreach (GameTypeEntry type in fixedTypes)
			{
				if (type.EvalType == evalType)
				{
					return type;
				}
			}

			return null;
		}

		GameTypeEntry GetSelectedEntry ()
		{
			foreach (RadioButton button in buttons)
			{
				if (button.Checked)
				{
					return (GameTypeEntry) button.Tag;
				}
			}

			return null;
		}

		void RebuildChoices ()
		{
			updatingButtons = true;

			GameTypeEntry selected = GetSelectedEntry();

			foreach (RadioButton button in buttons)
			{
				panel.Controls.Remove(button);
			}

			buttons.Clear();
			gameTypeToButton.Clear();

			lastCheckedButton = null;

			List<GameTypeEntry> types = new List<GameTypeEntry> ();
			types.AddRange(fixedTypes);

			if (typeForCurrentCustom != null)
			{
				types.Add(typeForCurrentCustom);
			}

			if (typeForLoadNew != null)
			{
				types.Add(typeForLoadNew);
			}

			foreach (GameTypeEntry type in types)
			{
				RadioButton button = new RadioButton ();
				button.Tag = type;
				button.Text = type.ToString();
				button.AutoSize = true;
				button.Font = SkinningDefs.TheInstance.GetFont(10);
                button.ForeColor = SkinningDefs.TheInstance.GetColorData("game_details_screen_text_colour", Color.Black);
                button.CheckedChanged += button_CheckedChanged;
				panel.Controls.Add(button);
				buttons.Add(button);
				gameTypeToButton.Add(type, button);
			}

			if (selected != null)
			{
				gameTypeToButton[selected].Checked = true;
				lastCheckedButton = gameTypeToButton[selected];
			}

			updatingButtons = false;

			DoLayout();
		}

		public void AddType (string shownName, string evalType, string gameType, em_GameEvalType type)
		{
			fixedTypes.Add(new GameTypeEntry (shownName, evalType, gameType, type));
			RebuildChoices();
		}

		public void AddCustomType (string shownName, string evalType, string gameType)
		{
			typeForLoadNew = new GameTypeEntry (shownName, evalType, gameType, em_GameEvalType.CUSTOM);
			RebuildChoices();
		}

		void LoadNewCustomFile (string file)
		{
			gameFile.CopyNewCustomMaturityFiles(file);

			WriteGameDetailsFile(typeForLoadNew.GameEvalType, typeForLoadNew.EvalType, typeForLoadNew.GameType, file);

			AddCustomFileIntoList(file);
		}

		void AddCustomFileIntoList (string file)
		{
			typeForCurrentCustom = new GameTypeEntry ("Custom ('" + Path.GetFileNameWithoutExtension(file) + "')",
												 typeForLoadNew.EvalType, typeForLoadNew.GameType, typeForLoadNew.GameEvalType);

			RebuildChoices();

			updatingButtons = true;
			gameTypeToButton[typeForCurrentCustom].Checked = true;
			updatingButtons = false;
		}

		void ClearCustomFileFromList ()
		{
			typeForCurrentCustom = null;
			RebuildChoices();
		}

		void WriteGameDetailsFile (em_GameEvalType evalType, string evalTypeString, string gameTypeString, string customFilename)
		{
			gameFile.Game_Eval_Type = evalType;

			BasicXmlDocument xml = BasicXmlDocument.LoadOrCreate(DetailsFilename);
			XmlElement root = XMLUtils.GetOrCreateElement(xml, "details");
			XmlElement evalTypeElement = XMLUtils.GetOrCreateElement(root, "evaltype");
			XmlElement gameTypeElement = XMLUtils.GetOrCreateElement(root, "GameType");
			XmlElement customEvalFilenameElement = XMLUtils.GetOrCreateElement(root, "evalname_custom");

			evalTypeElement.InnerText = evalTypeString;
			gameTypeElement.InnerText = gameTypeString;

			if (! string.IsNullOrEmpty(customFilename))
			{
				customEvalFilenameElement.InnerText = customFilename;
			}
			else
			{
				root.RemoveChild(customEvalFilenameElement);
			}

			xml.Save(DetailsFilename);

			gamePanel.RefreshMaturityScoreSet();
		}
	}
}