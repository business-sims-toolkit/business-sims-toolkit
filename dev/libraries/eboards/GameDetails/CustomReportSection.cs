using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using CoreUtils;
using GameManagement;
using LibCore;

namespace GameDetails
{
	public class CustomReportSection : GameDetailsSection
	{
		NetworkProgressionGameFile gameFile;

		RadioButton noneButton;
		RadioButton customButton;
		List<RadioButton> radioButtons;
		Dictionary<int, CustomReportRoundImagesPanel> roundToImages;

		ErrorProvider errorProvider;

		public CustomReportSection (NetworkProgressionGameFile gameFile)
		{
			this.gameFile = gameFile;

			Title = "Custom Report Graphics";

			radioButtons = new List<RadioButton>();

			noneButton = CreateRadioButton("No Report", CustomContentSource.None);
			customButton = CreateRadioButton("Custom Report", CustomContentSource.GameFile);
			int y = customButton.Bottom + 10;

			errorProvider = new ErrorProvider (this);

			roundToImages = new Dictionary<int, CustomReportRoundImagesPanel> ();
			for (int round = 1; round <= gameFile.GetTotalRounds(); round++)
			{
				var roundPanel = new CustomReportRoundImagesPanel (gameFile, round, errorProvider);
				roundPanel.Bounds = new Rectangle(0, y, 400, 50);
				panel.Controls.Add(roundPanel);
				y = roundPanel.Bottom + 5;

				roundToImages.Add(round, roundPanel);
			}

			LoadData();
		}

		void button_CheckedChanged (object sender, EventArgs args)
		{
			var button = (RadioButton) sender;

			if (button.Checked)
			{
				switch ((CustomContentSource) button.Tag)
				{
					case CustomContentSource.None:
						SetNoCustomReportImages();
						break;

					case CustomContentSource.GameFile:
						SetCustomReportImages();
						break;
				}
			}

			OnChanged();
		}

		RadioButton CreateRadioButton (string text, CustomContentSource source)
		{
			int y = 0;

			if (radioButtons.Count > 0)
			{
				y = radioButtons[radioButtons.Count - 1].Bottom + 20;
			}

			var button = new RadioButton
			{
				Text = text,
				Tag = source,
				Font = SkinningDefs.TheInstance.GetFont(10),
				Bounds = new Rectangle(0, y, 500, 30)
			};

			button.CheckedChanged += button_CheckedChanged;
			panel.Controls.Add(button);
			radioButtons.Add(button);

			return button;
		}

		public override void LoadData ()
		{
			base.LoadData();

			foreach (var button in radioButtons)
			{
				button.Checked = (gameFile.CustomContentSource == (CustomContentSource) button.Tag);
			}

			foreach (var panel in roundToImages.Values)
			{
				panel.LoadData();
			}
		}

		void SetNoCustomReportImages ()
		{
			gameFile.DeleteAllCustomContent();
			SetSize(400, customButton.Bottom + 10);

			LoadData();
		}

		void SetCustomReportImages ()
		{
			SetSize(400, roundToImages.Values.Max(panel => panel.Bottom) + 10);

			foreach (var panel in roundToImages.Values)
			{
				panel.LoadData();
			}
		}

		public override bool ValidateFields (bool reportErrors = true)
		{
			if (customButton.Checked)
			{
				foreach (var panel in roundToImages.Values)
				{
					if (! panel.Validate())
					{
						return false;
					}
				}
			}

			return true;
		}
	}
}