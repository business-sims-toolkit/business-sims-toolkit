using System.Windows.Forms;
using System.Drawing;
using System.Xml;

using GameManagement;
using LibCore;
using CoreUtils;

namespace GameDetails
{
	internal class TeamNameSection : Panel
	{
		NetworkProgressionGameFile gameFile;

		Label label;
		TextBox box;

		string TeamFile
		{
			get
			{
				return gameFile.Dir + @"\global\team.xml";
			}
		}

		public TeamNameSection (NetworkProgressionGameFile gameFile)
		{
			this.gameFile = gameFile;

			label = new Label();
			label.Font = SkinningDefs.TheInstance.GetFont(10);
			label.Text = "Team Name";
            label.ForeColor = SkinningDefs.TheInstance.GetColorData("game_details_screen_text_colour", Color.Black);
            Controls.Add(label);

			box = new TextBox();
			box.Font = SkinningDefs.TheInstance.GetFont(10);
			Controls.Add(box);

			DoLayout();
			LoadData();
		}

		void DoLayout ()
		{
			label.Location = new Point(0, 0);
			label.Size = new Size(120, 25);

			box.Location = new Point(label.Right, label.Top);
			box.Size = new Size(150, 25);

			Size = new Size(500, box.Bottom + 20);
		}

		public void LoadData ()
		{
			BasicXmlDocument xml = BasicXmlDocument.LoadOrCreate(TeamFile);
			XmlElement root = XMLUtils.GetOrCreateElement(xml, "team");
			XmlElement nameElement = XMLUtils.GetOrCreateElement(root, "team_name");
			box.Text = nameElement.InnerText;
			xml.Save(TeamFile);
		}

		public bool SaveData ()
		{
			BasicXmlDocument xml = BasicXmlDocument.LoadOrCreate(TeamFile);
			XmlElement root = XMLUtils.GetOrCreateElement(xml, "team");
			XmlElement nameElement = XMLUtils.GetOrCreateElement(root, "team_name");
			nameElement.InnerText = box.Text;
			xml.Save(TeamFile);

			return true;
		}
	}
}