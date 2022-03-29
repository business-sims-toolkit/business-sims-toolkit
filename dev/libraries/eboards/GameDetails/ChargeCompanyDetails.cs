using System.Xml;
using System.Windows.Forms;
using System.Drawing;

using GameManagement;
using LibCore;
using CoreUtils;

namespace GameDetails
{
	public class ChargeCompanyDetails : GameDetailsSection
	{
		NetworkProgressionGameFile gameFile;

		Label preamble;
		Label label;
		TextBox box;

		string Filename
		{
			get
			{
				return gameFile.Dir + @"\global\details.xml";
			}
		}

		public ChargeCompanyDetails (NetworkProgressionGameFile gameFile)		
		{
			this.gameFile = gameFile;

			Title = "Charge Company";

			BuildControls();
			LoadData();

			SetSize(500, 85);
		}

		protected virtual void BuildControls ()
		{
			preamble = new Label ();
			preamble.Font = SkinningDefs.TheInstance.GetFont(10);
			preamble.Text = "If a Partner, other than the company associated with this TAC, is to be charged for this event, then please confirm the company's name here.";
            preamble.ForeColor = SkinningDefs.TheInstance.GetColorData("game_details_screen_text_colour", Color.Black);
            panel.Controls.Add(preamble);

			label = new Label ();
			label.Font = SkinningDefs.TheInstance.GetFont(10);
			label.Text = "Charge company";
            label.ForeColor = SkinningDefs.TheInstance.GetColorData("game_details_screen_text_colour", Color.Black);
            label.TextAlign = ContentAlignment.MiddleRight;
			panel.Controls.Add(label);

			box = new TextBox ();
			box.Font = SkinningDefs.TheInstance.GetFont(10);
			box.Enabled = false;
			panel.Controls.Add(box);

			DoLayout();
		}

		protected virtual void DoLayout ()
		{
			int labelToBoxGap = 6;
			Size labelSize = new Size (89, 35);
			Size boxSize = new Size (150, labelSize.Height);

			preamble.Location = new Point (20, 0);
			preamble.Size = new Size (490 - preamble.Left, 50);

			label.Location = new Point (0, preamble.Bottom);
			label.Size = new Size (labelSize.Width, labelSize.Height);

			box.Location = new Point (label.Right + labelToBoxGap, preamble.Bottom + 4);
			box.Size = boxSize;
		}

		public override void LoadData ()
		{
			BasicXmlDocument xml = BasicXmlDocument.LoadOrCreate(Filename);
			XmlElement root = XMLUtils.GetOrCreateElement(xml, "details");
			XmlElement chargeCompanyElement = XMLUtils.GetOrCreateElement(root, "ChargeCompany");

			box.Text = chargeCompanyElement.InnerText;
		}

		public override bool SaveData ()
		{
			BasicXmlDocument xml = BasicXmlDocument.LoadOrCreate(Filename);
			XmlElement root = XMLUtils.GetOrCreateElement(xml, "details");
			XmlElement chargeCompanyElement = XMLUtils.GetOrCreateElement(root, "ChargeCompany");

			chargeCompanyElement.InnerText = box.Text;

			xml.Save(Filename);

			return true;
		}
	}
}