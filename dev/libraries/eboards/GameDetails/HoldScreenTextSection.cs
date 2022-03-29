using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CoreUtils;
using GameManagement;
using LibCore;

namespace GameDetails
{
	public class HoldScreenTextSection : GameDetailsSection
	{
		NetworkProgressionGameFile gameFile;

		Font font;
		TextBox textBox;

	    readonly string defaultText;

		public HoldScreenTextSection (EditGamePanel parent, NetworkProgressionGameFile gameFile, string defaultText)
		{
			this.gameFile = gameFile;

			font = SkinningDefs.TheInstance.GetFont(9);

			Title = "Hold Screen Text";

			textBox = new TextBox { Font = font, Text = defaultText, MaxLength = 30 };
			panel.Controls.Add(textBox);

		    this.defaultText = defaultText;

			DoLayout();
		}

		void DoLayout ()
		{
			textBox.Bounds = new Rectangle (0, 20, 400, 30);
		}
	
		public override void LoadData ()
		{
			base.LoadData();

			var text = gameFile.GetOption("hold_screen_text");
			if (text != null)
			{
				textBox.Text = text;
			}
		}

		public override bool SaveData ()
		{
			gameFile.SetOption("hold_screen_text", (string.IsNullOrEmpty(textBox.Text) ? defaultText : textBox.Text));

			return base.SaveData();
		}
	}
}