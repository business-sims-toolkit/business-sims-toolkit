using System;
using System.Windows.Forms;
using System.Drawing;

using Network;

using LibCore;
using CoreUtils;

namespace CommonGUI
{
	public class AOSE_ZoneOptionsPanel : FlickerFreePanel
	{
		NodeTree model;
		Node firmware_upgrade_control_node = null;
		Node firmware_upgrade_status_node = null;

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
				cancelButton.Location = new Point(510,185);
				cancelButton.SetButtonText("Close", upColor, upColor, hoverColor, disabledColor);
				cancelButton.Click += cancel;
				cancelButton.Visible = true;
				panel.Controls.Add(cancelButton);
			}
		}

		public AOSE_ZoneOptionsPanel(IDataEntryControlHolder mainPanel, NodeTree model, Boolean IsTrainingMode,
			Color OperationsBackColor, Color GroupPanelBackColor, int round)
		{
			this.mainPanel = mainPanel;
			this.round = round;

			string racetitlecolour =  SkinningDefs.TheInstance.GetData("race_paneltitleforecolor");
			if (racetitlecolour != "")
			{
				MyTitleForeColor =  SkinningDefs.TheInstance.GetColorData("race_paneltitleforecolor");
			}

			firmware_upgrade_control_node = model.GetNamedNode("fm_control");
			firmware_upgrade_status_node = model.GetNamedNode("fm_status");
			bool firmware_applied = firmware_upgrade_status_node.GetBooleanAttribute("applied", false);

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

			if (round > 1)
			{
				ImageTextButton ApplyFirmwareButton = new ImageTextButton(0);
				ApplyFirmwareButton.ButtonFont = MyDefaultSkinFontBold10;
				ApplyFirmwareButton.SetVariants("/images/buttons/blank_huge.png");
				ApplyFirmwareButton.Size = new Size(300, 20);
				ApplyFirmwareButton.Location = new Point(50, 50);
				ApplyFirmwareButton.SetButtonText("Apply Firmware Upgrade", upColor, upColor, hoverColor, disabledColor);
				ApplyFirmwareButton.Click += ApplyFirmwareButton_Click;
				ApplyFirmwareButton.Enabled = !(firmware_applied);
				Controls.Add(ApplyFirmwareButton);
			}
		}

		void cancelButton_Click (object sender, EventArgs args)
		{
			mainPanel.DisposeEntryPanel();
		}

		void ApplyFirmwareButton_Click(object sender, EventArgs e)
		{
			if (firmware_upgrade_control_node != null)
			{
				firmware_upgrade_control_node.SetAttribute("request", "true");
			}
			mainPanel.DisposeEntryPanel();
		}

	}
}