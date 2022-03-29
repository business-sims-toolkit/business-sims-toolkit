using System;
using System.Windows.Forms;
using System.Drawing;

using Network;

using LibCore;
using CoreUtils;

using IncidentManagement;

namespace CommonGUI
{
	public class ZoneOptionsPanel : FlickerFreePanel
	{
		NodeTree model;
		IDataEntryControlHolder mainPanel;
		Color MyTitleForeColor, MyOperationsBackColor, MyGroupPanelBackColor;
		Font MyDefaultSkinFontNormal10, MyDefaultSkinFontBold10, MyDefaultSkinFontBold12, MyDefaultSkinFontBold24;
		bool IsTrainingMode;
		string fontname;
		Color upColor, downColor, hoverColor, disabledColor;
		int round;

		void SetUpPanel (Panel panel, string title, EventHandler cancel)
		{
			panel.Size = Size;

			if (IsTrainingMode) 
			{
				panel.BackgroundImage = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location +
					"\\images\\panels\\race_panel_back_training.png");
			}
			else
			{
				panel.BackgroundImage = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location +
					"\\images\\panels\\race_panel_back_normal.png");
			}

			panel.BackColor = MyOperationsBackColor;

			Label PanelTitleLabel = new Label();
			PanelTitleLabel.Text = title;
			PanelTitleLabel.Size = new Size(500,20);
			PanelTitleLabel.Location = new Point(10,10);
			PanelTitleLabel.Font = MyDefaultSkinFontBold12;
			PanelTitleLabel.BackColor = MyOperationsBackColor;
			PanelTitleLabel.ForeColor = MyTitleForeColor;
			panel.Controls.Add(PanelTitleLabel);

			if (cancel != null)
			{
				ImageTextButton cancelButton = new ImageTextButton(0);
				cancelButton.ButtonFont = MyDefaultSkinFontBold10;
				cancelButton.SetVariants("/images/buttons/blank_small.png");
				cancelButton.Size = new Size(80,20);
				cancelButton.Location = new Point(520,185);
				cancelButton.SetButtonText("Close",
					upColor,upColor,
					hoverColor,disabledColor);
				cancelButton.Click += cancel;
				cancelButton.Visible = true;
				panel.Controls.Add(cancelButton);
			}
		}
		
		public ZoneOptionsPanel (IDataEntryControlHolder mainPanel, NodeTree model, Boolean IsTrainingMode,
			Color OperationsBackColor, Color GroupPanelBackColor, int round)
		{
			this.mainPanel = mainPanel;
			this.round = round;

			string racetitlecolour =  SkinningDefs.TheInstance.GetData("race_paneltitleforecolor");
			if (racetitlecolour != "")
			{
				MyTitleForeColor =  SkinningDefs.TheInstance.GetColorData("race_paneltitleforecolor");
			}

			MyOperationsBackColor = OperationsBackColor;
			MyGroupPanelBackColor = GroupPanelBackColor;
			MyTitleForeColor = Color.Black;

			fontname = SkinningDefs.TheInstance.GetData("fontname");
			MyDefaultSkinFontNormal10 = ConstantSizeFont.NewFont(fontname,10);
			MyDefaultSkinFontBold10 = ConstantSizeFont.NewFont(fontname,10,FontStyle.Bold);
			MyDefaultSkinFontBold12 = ConstantSizeFont.NewFont(fontname,12,FontStyle.Bold);
			MyDefaultSkinFontBold24 = ConstantSizeFont.NewFont(fontname,24,FontStyle.Bold);

			this.IsTrainingMode = IsTrainingMode;

			upColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_text_colour", Color.Black);
			downColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_down_text_colour", Color.White);
			hoverColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_hover_text_colour", Color.Green);
			disabledColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_disabled_text_colour", Color.DarkGray);

			SetStyle(ControlStyles.Selectable, true);

			SetUpPanel(this, "Options", cancelButton_Click);

			this.model = model;
			this.mainPanel = mainPanel;

			ImageTextButton reducePowerButton = new ImageTextButton (0);
			reducePowerButton.ButtonFont = MyDefaultSkinFontBold10;
			reducePowerButton.SetVariants("/images/buttons/blank_med.png");
			reducePowerButton.Size = new Size (300, 20);
			reducePowerButton.Location = new Point (50, 50);
			reducePowerButton.SetButtonText("Reduce Power Consumption", upColor, upColor, hoverColor, disabledColor);
			reducePowerButton.Click += reducePowerButton_Click;
			reducePowerButton.Enabled = ! (ZoneActivator.IsZone2TemperatureRaised(model) && ZoneActivator.HaveZonesHadRetirement(model) && ZoneActivator.IsZone4Bladed(model));
			Controls.Add(reducePowerButton);

			ImageTextButton activateZone7Button = new ImageTextButton (0);
			activateZone7Button.ButtonFont = MyDefaultSkinFontBold10;
			activateZone7Button.SetVariants("/images/buttons/blank_med.png");
			activateZone7Button.Size = new Size (300, 20);
			activateZone7Button.Location = new Point (50, 100);
			activateZone7Button.SetButtonText("Commission New DC", upColor, upColor, hoverColor, disabledColor);
			activateZone7Button.Click += activateZone7Button_Click;
			activateZone7Button.Enabled = ! ZoneActivator.IsZone7Activated(model);
			activateZone7Button.Visible = (this.round >= 3);
			Controls.Add(activateZone7Button);
		}

		void cancelButton_Click (object sender, EventArgs args)
		{
			mainPanel.DisposeEntryPanel();
		}

		void reducePowerButton_Click (object sender, EventArgs e)
		{
			FlickerFreePanel reducePowerPanel = new FlickerFreePanel ();
			SetUpPanel(reducePowerPanel, "Reduce Power Consumption", cancelButton_Click);
			Controls.Add(reducePowerPanel);

			ImageTextButton zone2TemperatureButton = new ImageTextButton (0);
			zone2TemperatureButton.ButtonFont = MyDefaultSkinFontBold10;
			zone2TemperatureButton.SetVariants("/images/buttons/blank_med.png");
			zone2TemperatureButton.Size = new Size (350, 20);
			zone2TemperatureButton.Location = new Point (50, 50);
			zone2TemperatureButton.SetButtonText("Adjust Zone 2 Temperature", upColor, upColor, hoverColor, disabledColor);
			zone2TemperatureButton.Click += zone2TemperatureButton_Click;
			zone2TemperatureButton.Enabled = ! ZoneActivator.IsZone2TemperatureRaised(model);
			reducePowerPanel.Controls.Add(zone2TemperatureButton);

			ImageTextButton retireServicesButton = new ImageTextButton (0);
			retireServicesButton.ButtonFont = MyDefaultSkinFontBold10;
			retireServicesButton.SetVariants("/images/buttons/blank_med.png");
			retireServicesButton.Size = new Size (350, 20);
			retireServicesButton.Location = new Point (50, 90);
			retireServicesButton.SetButtonText("Retire Zone 3 Services", upColor, upColor, hoverColor, disabledColor);
			retireServicesButton.Click += retireServicesButton_Click;
			retireServicesButton.Enabled = ! ZoneActivator.HaveZonesHadRetirement(model);
			reducePowerPanel.Controls.Add(retireServicesButton);

			ImageTextButton zone4BladesButton = new ImageTextButton (0);
			zone4BladesButton.ButtonFont = MyDefaultSkinFontBold10;
			zone4BladesButton.SetVariants("/images/buttons/blank_med.png");
			zone4BladesButton.Size = new Size (350, 20);
			zone4BladesButton.Location = new Point (50, 130);
			zone4BladesButton.SetButtonText("Move Zone 4 To Blades", upColor, upColor, hoverColor, disabledColor);
			zone4BladesButton.Click += zone4BladesButton_Click;
			zone4BladesButton.Enabled = ! ZoneActivator.IsZone4Bladed(model);
			reducePowerPanel.Controls.Add(zone4BladesButton);

			ImageTextButton allTheAboveButton = new ImageTextButton (0);
			allTheAboveButton.ButtonFont = MyDefaultSkinFontBold10;
			allTheAboveButton.SetVariants("/images/buttons/blank_med.png");
			allTheAboveButton.Size = new Size(350, 20);
			allTheAboveButton.Location = new Point (50, 170);
			allTheAboveButton.SetButtonText("Perform All Available Improvements", upColor, upColor, hoverColor, disabledColor);
			allTheAboveButton.Click += allTheAboveButton_Click;
			allTheAboveButton.Enabled = zone2TemperatureButton.Enabled || zone4BladesButton.Enabled || retireServicesButton.Enabled;
			reducePowerPanel.Controls.Add(allTheAboveButton);

			reducePowerPanel.BringToFront();
		}

		void activateZone7Button_Click (object sender, EventArgs e)
		{
			Control activationPanel = new ZoneActivationPanel (mainPanel, model, false, MyOperationsBackColor, MyGroupPanelBackColor);
			activationPanel.Location = new Point (0, 0);
			activationPanel.Size = Size;
			SuspendLayout();
			Controls.Add(activationPanel);
			activationPanel.BringToFront();
			ResumeLayout(false);
		}

		void allTheAboveButton_Click (object sender, EventArgs e)
		{
			FlickerFreePanel confirmPanel = new FlickerFreePanel();
			SetUpPanel(confirmPanel, "Confirm Apply Improvements", cancelButton_Click);
			Controls.Add(confirmPanel);

			ImageTextButton okButton = new ImageTextButton(0);
			okButton.ButtonFont = MyDefaultSkinFontBold10;
			okButton.SetVariants("/images/buttons/blank_small.png");
			okButton.Size = new Size(80, 20);
			okButton.Location = new Point(410, 185);
			okButton.SetButtonText("OK", upColor, upColor, hoverColor, disabledColor);
			okButton.Click += okAllTheAboveButton_Click;
			okButton.Visible = true;
			confirmPanel.Controls.Add(okButton);

			confirmPanel.BringToFront();
		}

		void retireServicesButton_Click (object sender, EventArgs e)
		{
			FlickerFreePanel confirmPanel = new FlickerFreePanel ();
			SetUpPanel(confirmPanel, "Confirm Retire Zone 3 Services", cancelButton_Click);
			Controls.Add(confirmPanel);

			ImageTextButton okButton = new ImageTextButton(0);
			okButton.ButtonFont = MyDefaultSkinFontBold10;
			okButton.SetVariants("/images/buttons/blank_small.png");
			okButton.Size = new Size(80,20);
			okButton.Location = new Point(410,185);
			okButton.SetButtonText("OK", upColor,upColor, hoverColor,disabledColor);
			okButton.Click += okRetireButton_Click;
			okButton.Visible = true;
			confirmPanel.Controls.Add(okButton);

			confirmPanel.BringToFront();
		}

		void okRetireButton_Click (object sender, EventArgs e)
		{
			ZoneActivator.RetireServices(model);
			mainPanel.DisposeEntryPanel();
		}

		void okBladesButton_Click (object sender, EventArgs e)
		{
			ZoneActivator.SwitchZone4ToBlades(model);
			mainPanel.DisposeEntryPanel();
		}

		void okTemperatureButton_Click (object sender, EventArgs e)
		{
			ZoneActivator.RaiseZone2Temperature(model);
			mainPanel.DisposeEntryPanel();
		}

		void okConsolidateButton_Click (object sender, EventArgs e)
		{
			ZoneActivator.ConsolidateFuel(model);
			mainPanel.DisposeEntryPanel();
		}

		void okAllTheAboveButton_Click (object sender, EventArgs e)
		{
			if (! ZoneActivator.IsZone4Bladed(model))
			{
				ZoneActivator.SwitchZone4ToBlades(model);
			}

			if (! ZoneActivator.IsZone2TemperatureRaised(model))
			{
				ZoneActivator.RaiseZone2Temperature(model);
			}

			if (! ZoneActivator.HaveZonesHadRetirement(model))
			{
				ZoneActivator.RetireServices(model);
			}

			mainPanel.DisposeEntryPanel();
		}

		void zone2TemperatureButton_Click (object sender, EventArgs e)
		{
			FlickerFreePanel confirmPanel = new FlickerFreePanel ();
			SetUpPanel(confirmPanel, "Confirm Raise Zone 2 Temperature", cancelButton_Click);
			Controls.Add(confirmPanel);

			ImageTextButton okButton = new ImageTextButton(0);
			okButton.ButtonFont = MyDefaultSkinFontBold10;
			okButton.SetVariants("/images/buttons/blank_small.png");
			okButton.Size = new Size(80,20);
			okButton.Location = new Point(410,185);
			okButton.SetButtonText("OK", upColor,upColor, hoverColor,disabledColor);
			okButton.Click += okTemperatureButton_Click;
			okButton.Visible = true;
			confirmPanel.Controls.Add(okButton);

			confirmPanel.BringToFront();
		}

		void consolidateButton_Click (object sender, EventArgs e)
		{
			FlickerFreePanel confirmPanel = new FlickerFreePanel ();
			SetUpPanel(confirmPanel, "Confirm Consolidate Fuel Rig Ops", cancelButton_Click);
			Controls.Add(confirmPanel);

			ImageTextButton okButton = new ImageTextButton(0);
			okButton.ButtonFont = MyDefaultSkinFontBold10;
			okButton.SetVariants("/images/buttons/blank_small.png");
			okButton.Size = new Size(80,20);
			okButton.Location = new Point(410,185);
			okButton.SetButtonText("OK", upColor,upColor, hoverColor,disabledColor);
			okButton.Click += okConsolidateButton_Click;
			okButton.Visible = true;
			confirmPanel.Controls.Add(okButton);

			confirmPanel.BringToFront();
		}

		void zone4BladesButton_Click (object sender, EventArgs e)
		{
			FlickerFreePanel confirmPanel = new FlickerFreePanel ();
			SetUpPanel(confirmPanel, "Confirm Move Zone 4 To Blades", cancelButton_Click);
			Controls.Add(confirmPanel);

			ImageTextButton okButton = new ImageTextButton(0);
			okButton.ButtonFont = MyDefaultSkinFontBold10;
			okButton.SetVariants("/images/buttons/blank_small.png");
			okButton.Size = new Size(80,20);
			okButton.Location = new Point(410,185);
			okButton.SetButtonText("OK", upColor,upColor, hoverColor,disabledColor);
			okButton.Click += okBladesButton_Click;
			okButton.Visible = true;
			confirmPanel.Controls.Add(okButton);

			confirmPanel.BringToFront();
		}
	}
}