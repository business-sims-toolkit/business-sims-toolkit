using System;
using System.Collections;

using System.Drawing;
using System.Windows.Forms;

using LibCore;
using CoreUtils;

using Network;

using CommonGUI;

using IncidentManagement;

namespace TransitionScreens
{
	public class ActivateZonePanel : FlickerFreePanel
	{
		IDataEntryControlHolder mainPanel;
		NodeTree model;

		ArrayList slots;

		Label errorDisplayText;

		public ActivateZonePanel (IDataEntryControlHolder mainPanel, NodeTree model, Boolean IsTrainingMode,
		                          Color OperationsBackColor, Color GroupPanelBackColor)
		{
			this.mainPanel = mainPanel;
			this.model = model;

			string fontname =  SkinningDefs.TheInstance.GetData("fontname");
			Font MyDefaultSkinFontNormal10 = ConstantSizeFont.NewFont(fontname,10);
			Font MyDefaultSkinFontNormal12 = ConstantSizeFont.NewFont(fontname,12);
			Font MyDefaultSkinFontBold8 = ConstantSizeFont.NewFont(fontname,8,FontStyle.Bold);
			Font MyDefaultSkinFontBold9 = ConstantSizeFont.NewFont(fontname,9,FontStyle.Bold);
			Font MyDefaultSkinFontBold10 = ConstantSizeFont.NewFont(fontname,10,FontStyle.Bold);
			Font MyDefaultSkinFontBold11 = ConstantSizeFont.NewFont(fontname,11,FontStyle.Bold);
			Font MyDefaultSkinFontBold12 = ConstantSizeFont.NewFont(fontname,12,FontStyle.Bold);

			Color upColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_text_colour", Color.Black);
			Color downColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_down_text_colour", Color.White);
			Color hoverColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_hover_text_colour", Color.Green);
			Color disabledColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_disabled_text_colour", Color.DarkGray);

			errorDisplayText = new Label();
			errorDisplayText.Text = "";
			errorDisplayText.Size = new Size(350,25);
			errorDisplayText.Font = MyDefaultSkinFontBold10;
			errorDisplayText.TextAlign = ContentAlignment.MiddleCenter;
			errorDisplayText.ForeColor = Color.Red;
			errorDisplayText.Location = new Point(0,160);
			errorDisplayText.Visible = false;
			Controls.Add(errorDisplayText);

			Label header = new Label();
			header.Text = "Select Apps To Move To Zone 7";
			header.Size = new Size(700,25);
			header.Font = MyDefaultSkinFontBold12;
			header.Location = new Point(10,10);
			Controls.Add(header);

			ImageTextButton okButton = new ImageTextButton(0);
			okButton.ButtonFont = MyDefaultSkinFontBold9;
			okButton.SetVariants("/images/buttons/blank_small.png");
			okButton.Size = new Size(80,20);
			okButton.Location = new Point(350,160);
			okButton.SetButtonText("OK",
				upColor,upColor,
				hoverColor,disabledColor);
			okButton.Click += okButton_Click;
			okButton.Visible = true;
			Controls.Add(okButton);

			ImageTextButton cancelButton = new ImageTextButton(0);
			cancelButton.ButtonFont = MyDefaultSkinFontBold9;			
			cancelButton.SetVariants("/images/buttons/blank_small.png");
			cancelButton.Size = new Size(80,20);
			cancelButton.Location = new Point(445,160);
			cancelButton.SetButtonText("Cancel",
				upColor,upColor,
				hoverColor,disabledColor);
			cancelButton.Click += cancelButton_Click;
			cancelButton.Visible = true;
			Controls.Add(cancelButton);

			slots = ZoneActivator.GetZone7Slots(model);
			ArrayList potentialApps = ZoneActivator.GetPotentialAppsToMoveToZone7(model);

			int startY = header.Bottom + 20;
			int x = 10;
			int y = startY;
			foreach (Node app in potentialApps)
			{
				ImageTextButton button = new ImageTextButton (0);
				button.SetVariants("/images/buttons/blank_small.png");
				button.Tag = app;
				button.SetButtonText(app.GetAttribute("name"), upColor, upColor, hoverColor, disabledColor);
				button.Location = new Point (x, y);
				button.Size = new Size (100, 20);
				button.Click += button_Click;
				Controls.Add(button);
				y += 25;

				if ((y + 20) >= 150)
				{
					y = startY;
					x += 120;
				}
			}
		}

		void okButton_Click (object sender, EventArgs args)
		{
			mainPanel.DisposeEntryPanel();
		}

		void cancelButton_Click (object sender, EventArgs args)
		{
			mainPanel.DisposeEntryPanel();
		}

		void button_Click (object sender, EventArgs args)
		{
			ImageTextButton button = sender as ImageTextButton;

			errorDisplayText.Hide();

			if (button.Active)
			{
				button.Active = false;
			}
			else
			{
				int active = 0;
				foreach (Control control in Controls)
				{
					ImageTextButton app = control as ImageTextButton;

					if ((app != null) && ((app.Tag as Node) != null) && app.Active)
					{
						active++;
					}
				}

				if (active < slots.Count)
				{
					button.Active = true;
				}
				else
				{
					errorDisplayText.Text = "Not Enough Slots";
					errorDisplayText.Show();
				}
			}
		}
	}
}