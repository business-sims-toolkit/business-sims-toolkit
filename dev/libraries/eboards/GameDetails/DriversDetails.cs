using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;

using LibCore;
using CoreUtils;
using GameManagement;

namespace GameDetails
{
	/// <summary>
	/// Summary description for DriversDetails.
	/// </summary>
	public class DriversDetails : GameDetailsSection
	{
		NetworkProgressionGameFile gameFile;
        int numberOfPlayers = 4;
        List<Label> labels;

		string Filename
		{
			get
			{
				return gameFile.Dir + @"\global\team.xml";
			}
		}

		protected TextBox[] drivers;

        public void SetDriver(int num, string name)
        {
            drivers[num].Text = name;
        }

        public void SetDriverTitle(int num, string name)
        {
            labels[num].Text = name;
        }



		public string GetDriver(int num)
		{
			return drivers[num].Text;
		}

        public DriversDetails(NetworkProgressionGameFile gameFile) : this(gameFile, 4)
        {      
        }


        public DriversDetails(NetworkProgressionGameFile gameFile, int numPlayers)
        {
			this.gameFile = gameFile;
            numberOfPlayers = numPlayers;
			Title = "Drivers";
            labels = new List<Label>();

			Font f = ConstantSizeFont.NewFont("Arial Unicode MS", 8);
			drivers = new TextBox[numberOfPlayers];

			int top = 20;

			for(int i=1; i<=numberOfPlayers; ++i)
			{
				Label l = new Label();
				l.Text = "Driver " + CONVERT.ToStr(i);
				l.Font = f;
				l.Size = new Size(150, 20);
				l.Location = new Point(0, top + (i-1)*25);
                labels.Add(l);
				panel.Controls.Add(l);
				//
				drivers[i-1] = new TextBox();
				drivers[i-1].Text = "Driver " + CONVERT.ToStr(i);
				drivers[i-1].Location = new Point(160, top + (i-1)*25);
				drivers[i-1].Size = new Size(150,20);
				drivers[i-1].Font = f;
				drivers[i-1].TextChanged += DriversDetails_TextChanged;
				panel.Controls.Add( drivers[i-1] );
			}

			LoadData();
			
			SetSize(460, 150);
		}

		void DriversDetails_TextChanged(object sender, EventArgs e)
		{
			TextBox textBox = sender as TextBox;
			if(null != textBox)
			{
				string str = textBox.Text;
				if(str.Length > 8)
				{
					textBox.Text = str.Substring(0,8);
					textBox.SelectionStart = 8;
				}
			}
		}

		public override void LoadData ()
		{
			BasicXmlDocument xml = BasicXmlDocument.LoadOrCreate(Filename);
			XmlElement root = XMLUtils.GetOrCreateElement(xml, "team");

			for (int i = 0; i < numberOfPlayers; i++)
			{
				bool exists;
				string name = XMLUtils.GetElementStringWithCheck(root, CONVERT.Format("driver{0}", i + 1), out exists);

				if (exists)
				{
					SetDriver(i, name);
				}
			}
		}

		public override bool SaveData ()
		{
			BasicXmlDocument xml = BasicXmlDocument.LoadOrCreate(Filename);
			XmlElement root = XMLUtils.GetOrCreateElement(xml, "team");

			for (int i = 0; i < numberOfPlayers; i++)
			{
				XMLUtils.GetOrCreateElement(root, CONVERT.Format("driver{0}", i + 1)).InnerText = GetDriver(i);
			}

			xml.Save(Filename);

			return true;
		}
	}
}