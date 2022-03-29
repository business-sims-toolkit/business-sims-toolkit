using System;
using System.Windows.Forms;
using System.Drawing;

using Network;
using LibCore;
using CoreUtils;

namespace CommonGUI
{
	public class UnPlugServerPanel : FlickerFreePanel
	{
		FocusJumper jumper;
		NodeTree tree;
		OpsControlPanel parent;
		UnPlugServerPanelChoice choicePanel;

		ImageTextButton cancelButton;

		public UnPlugServerPanel (OpsControlPanel tcp, NodeTree tree)
		{
			Color upColour = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_text_colour", Color.Black);
			Color downColour = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_down_text_colour", Color.White);
			Color hoverColour = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_hover_text_colour", Color.Green);
			Color disabledColour = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_disabled_text_colour", Color.DarkGray);

			string serverDisplayName = SkinningDefs.TheInstance.GetData("servername");
			if (string.IsNullOrEmpty(serverDisplayName))
			{
				serverDisplayName = "Cabinet";
			}
				
			parent = tcp;
			this.tree = tree;
			jumper = new FocusJumper ();

			string fontName = SkinningDefs.TheInstance.GetData("fontname");
			Font fontBold12 = ConstantSizeFont.NewFont (fontName, 12, FontStyle.Bold);
			Font fontBold10 = ConstantSizeFont.NewFont (fontName, 10, FontStyle.Bold);
			Font fontBold9 = ConstantSizeFont.NewFont (fontName, 9, FontStyle.Bold);

			BackgroundImage = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location +
				"\\images\\panels\\race_panel_back_normal.png");

			Label label = new Label ();
			label.Text = serverDisplayName + " On/Off";
			label.Font = fontBold12;
			label.Size = new Size (400, 20);
			label.Location = new Point (10, 10);
			label.BackColor = Color.Transparent;
			Controls.Add(label);

			Label legend = new Label ();
			legend.Text = "Select zone";
			legend.Font = fontBold10;
			legend.Size = new Size (400, 20);
			legend.Location = new Point (10, 30);
			legend.BackColor = Color.Transparent;
			Controls.Add(legend);

			cancelButton = new ImageTextButton(0);
			cancelButton.SetVariants(@"/images/buttons/blank_small.png");
			cancelButton.ButtonFont = fontBold10;
			cancelButton.Size = new Size (80, 20);
			cancelButton.Location = new Point (520, 185);
			cancelButton.SetButtonText("Close", upColour, upColour, hoverColour, disabledColour);
			cancelButton.Click += cancelButton_Click;
			cancelButton.Visible = true;
			Controls.Add(cancelButton);
			jumper.Add(cancelButton);

			// Not allowed to unplug servers in zone 7.
			for (int zone = 1; zone <= 6; zone++)
			{
				ImageTextButton button = new ImageTextButton(0);
				button.SetVariants(@"/images/buttons/blank_small.png");
				button.ButtonFont = fontBold9;
				button.Size = new Size (60, 20);
				button.Location = new Point (30 + ((zone - 1) * 80), 70);
				button.SetButtonText(CONVERT.ToStr(zone), upColour, upColour, hoverColour, disabledColour);
				button.Click += button_Click;

				Node zoneNode = tree.GetNamedNode("Zone" + CONVERT.ToStr(zone));
				button.Tag = zone;
				Controls.Add(button);
				jumper.Add(button);
			}

			GotFocus += UnPlugServerPanel_GotFocus;
		}

		void cancelButton_Click (object sender, EventArgs e)
		{
			Close();
		}

		public void Close ()
		{
			parent.DisposeEntryPanel();
		}

		void button_Click (object sender, EventArgs e)
		{
			ImageTextButton button = sender as ImageTextButton;

			choicePanel = new UnPlugServerPanelChoice (this, tree, (int) (button.Tag), Width);
			choicePanel.Size = Size;
			choicePanel.Location = new Point (0, 0);
			Controls.Add(choicePanel);
			choicePanel.BringToFront();
		}

		void UnPlugServerPanel_GotFocus(object sender, EventArgs e)
		{
			cancelButton.Focus();
		}

		public void DisposeChoicePanel ()
		{
			if (choicePanel != null)
			{
				Controls.Remove(choicePanel);
				choicePanel = null;
			}

			parent.DisposeEntryPanel();
		}
	}
}