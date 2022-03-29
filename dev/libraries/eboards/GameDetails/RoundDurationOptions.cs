using System;
using System.Drawing;
using System.Windows.Forms;
using GameManagement;

namespace GameDetails
{
	/// <summary>
	/// Summary description for DriversDetails.
	/// </summary>
	public class RoundDurationOptions : GameDetailsSection
	{
		NetworkProgressionGameFile gameFile;
        Font font = CoreUtils.SkinningDefs.TheInstance.GetFont(10);

        public RoundDurationOptions(NetworkProgressionGameFile gameFile)
        {
			this.gameFile = gameFile;
			Title = "Game Options";

            CreateGameTimeRadioButtons();
			
			SetSize(460, 150);
		}

        public void CreateGameTimeRadioButtons()
        {
            int spacing = 5;
            RadioButton twentyMinuteGame = new RadioButton();
            twentyMinuteGame.Font = font;
            twentyMinuteGame.Location = new Point(15, spacing + 10);
            twentyMinuteGame.Size = new Size(150, 20);
            twentyMinuteGame.TextAlign = ContentAlignment.MiddleLeft;
            twentyMinuteGame.Text = "20 Minute Game";
            twentyMinuteGame.Checked = ! gameFile.GetBoolGlobalOption("isShortGame", false);
            twentyMinuteGame.CheckedChanged += CheckedChangedHandler;
            twentyMinuteGame.Tag = 20;
            panel.Controls.Add(twentyMinuteGame);

            RadioButton tenMinuteGame = new RadioButton();
            tenMinuteGame.Font = font;
            tenMinuteGame.Location = new Point(15, twentyMinuteGame.Bottom + spacing);
            tenMinuteGame.Size = new Size(150, 20);
            tenMinuteGame.TextAlign = ContentAlignment.MiddleLeft;
            tenMinuteGame.Text = "10 Minute Game";
            tenMinuteGame.Checked = gameFile.GetBoolGlobalOption("isShortGame", false);
            tenMinuteGame.CheckedChanged += CheckedChangedHandler;
            tenMinuteGame.Tag = 10;
            panel.Controls.Add(tenMinuteGame);
        }


        void CheckedChangedHandler(object sender, EventArgs e)
        {
            if (((RadioButton)sender).Checked)
            {
                RadioButton rb = (RadioButton)sender;
                if ((int)rb.Tag == 10)
                {
                   gameFile.SetGlobalOption("isShortGame", true);
                }
                else if ((int)rb.Tag == 20)
                {
                    gameFile.SetGlobalOption("isShortGame", false);
                }
            }
        }
	}
}