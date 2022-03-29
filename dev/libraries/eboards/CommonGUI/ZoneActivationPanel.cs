using System;
using System.Collections;

using System.Windows.Forms;
using System.Drawing;

using Network;

using LibCore;
using CoreUtils;

using IncidentManagement;

namespace CommonGUI
{
	public class ZoneActivationPanel : FlickerFreePanel
	{
		NodeTree model;
		IDataEntryControlHolder mainPanel;

		ArrayList slots;
		
		Label errorDisplayText;
		
		public ZoneActivationPanel (IDataEntryControlHolder mainPanel, NodeTree model, Boolean IsTrainingMode,
		                            Color OperationsBackColor, Color GroupPanelBackColor)
		{
			Color MyTitleForeColor, MyOperationsBackColor, MyGroupPanelBackColor;

			SetStyle(ControlStyles.Selectable, true);

			string racetitlecolour =  SkinningDefs.TheInstance.GetData("race_paneltitleforecolor");
			if (racetitlecolour != "")
			{
				MyTitleForeColor =  SkinningDefs.TheInstance.GetColorData("race_paneltitleforecolor");
			}

			MyOperationsBackColor = OperationsBackColor;
			MyGroupPanelBackColor = GroupPanelBackColor;
			MyTitleForeColor = Color.Black;

			string fontname =  SkinningDefs.TheInstance.GetData("fontname");
			Font MyDefaultSkinFontNormal10 = ConstantSizeFont.NewFont(fontname,10);
			Font MyDefaultSkinFontBold10 = ConstantSizeFont.NewFont(fontname,10,FontStyle.Bold);
			Font MyDefaultSkinFontBold12 = ConstantSizeFont.NewFont(fontname,12,FontStyle.Bold);
			Font MyDefaultSkinFontBold24 = ConstantSizeFont.NewFont(fontname,24,FontStyle.Bold);

			this.model = model;
			this.mainPanel = mainPanel;

			//Determine the training Mode and hence the Background Image
			if (IsTrainingMode) 
			{
				BackgroundImage = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location +
					"\\images\\panels\\race_panel_back_training.png");
			}
			else
			{
				BackgroundImage = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location +
					"\\images\\panels\\race_panel_back_normal.png");
			}

			//Start building this control
			BackColor = OperationsBackColor;

			Label PanelTitleLabel = new Label();
			PanelTitleLabel.Text = "Choose Services To Migrate To Zone 7";
			PanelTitleLabel.Size = new Size(500,20);
			PanelTitleLabel.Location = new Point(10,10);
			PanelTitleLabel.Font = MyDefaultSkinFontBold12;
			PanelTitleLabel.BackColor = OperationsBackColor;
			PanelTitleLabel.ForeColor = MyTitleForeColor;
			Controls.Add(PanelTitleLabel);

			Color upColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_text_colour", Color.Black);
			Color downColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_down_text_colour", Color.White);
			Color hoverColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_hover_text_colour", Color.Green);
			Color disabledColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_disabled_text_colour", Color.DarkGray);

			ImageTextButton okButton = new ImageTextButton(0);
			okButton.ButtonFont = MyDefaultSkinFontBold10;
			okButton.SetVariants("/images/buttons/blank_small.png");
			okButton.Size = new Size(80,20);
			okButton.Location = new Point(265,180);
			okButton.SetButtonText("OK",
				upColor,upColor,
				hoverColor,disabledColor);
			okButton.Click += okButton_Click;
			okButton.Visible = true;
			Controls.Add(okButton);

			ImageTextButton cancelButton = new ImageTextButton(0);
			cancelButton.ButtonFont = MyDefaultSkinFontBold10;
			cancelButton.SetVariants("/images/buttons/blank_small.png");
			cancelButton.Size = new Size(80,20);
			cancelButton.Location = new Point(370,180);
			cancelButton.SetButtonText("Close",
				upColor,upColor,
				hoverColor,disabledColor);
			cancelButton.Click += cancelButton_Click;
			cancelButton.Visible = true;
			Controls.Add(cancelButton);

			errorDisplayText = new Label();
			errorDisplayText.Text = "";
			errorDisplayText.Size = new Size(470,25);
			errorDisplayText.Font = MyDefaultSkinFontBold10;
			errorDisplayText.TextAlign = ContentAlignment.MiddleCenter;
			errorDisplayText.ForeColor = Color.Red;
			errorDisplayText.Location = new Point(30,130);
			errorDisplayText.Visible = false;
			Controls.Add(errorDisplayText);

			slots = ZoneActivator.GetZone7Slots(model);
			ArrayList potentialApps = ZoneActivator.GetPotentialAppsToMoveToZone7(model);

			int startY = PanelTitleLabel.Bottom + 20;
			int x = 10;
			int y = startY;
			for (int i = 0; i < potentialApps.Count; i += 2)
			{
				ImageTextButton button = new ImageTextButton (0);
				button.SetVariants("/images/buttons/blank_big.png");
				Node [] apps = new Node [2] { potentialApps[i] as Node, potentialApps[i + 1] as Node };
				button.Tag = apps;
				button.SetButtonText(apps[0].GetAttribute("desc") + " & " + apps[1].GetAttribute("desc"), upColor, upColor, hoverColor, disabledColor);
				button.Location = new Point (x, y);
				button.Size = new Size (390, 20);
				button.Click += button_Click;
				Controls.Add(button);
				y += 25;

				if ((y + 20) >= 250)
				{
					y = startY;
					x += 400;
				}

			}
		}

		void okButton_Click (object sender, EventArgs args)
		{
			ArrayList appsToMove = new ArrayList ();

			foreach (Control control in Controls)
			{
				ImageTextButton button = control as ImageTextButton;
				if (button != null)
				{
					Node [] apps = button.Tag as Node [];

					if (apps != null)
					{
						foreach (Node app in apps)
						{
							if ((app != null) && button.Active)
							{
								appsToMove.Add(app.GetAttribute("name"));
							}
						}
					}
				}
			}

			ArrayList attrs = new ArrayList ();
			attrs.Add(new AttributeValuePair ("type", "ActivateZone"));
			attrs.Add(new AttributeValuePair ("zone", "7"));
			attrs.Add(new AttributeValuePair ("services", String.Join(",", (string []) appsToMove.ToArray(typeof (string)))));
			new Node (model.GetNamedNode("ZoneActivationQueue"), "ActivateZone", "", attrs);

			mainPanel.DisposeEntryPanel();
		}

		void cancelButton_Click (object sender, EventArgs args)
		{
			mainPanel.DisposeEntryPanel();
		}

		void button_Click (object sender, EventArgs e)
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

					if ((app != null) && ((app.Tag as Node []) != null) && app.Active)
					{
						Node [] apps = app.Tag as Node [];
						active += apps.Length;
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