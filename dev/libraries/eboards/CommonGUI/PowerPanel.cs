using System;
using System.Windows.Forms;
using System.Collections;
using System.Drawing;

using Network;
using LibCore;
using CoreUtils;

namespace CommonGUI
{
	public class PowerPanel : FlickerFreePanel
	{
		FocusJumper jumper;
		NodeTree tree;
		OpsControlPanel parent;

		ImageTextButton closeButton;

		ArrayList fieldsByZone = new ArrayList ();

		Node limitsNode;

		public PowerPanel (OpsControlPanel tcp, NodeTree tree)
		{
			Color upColour = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_text_colour", Color.Black);
			Color downColour = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_down_text_colour", Color.White);
			Color hoverColour = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_hover_text_colour", Color.Green);
			Color disabledColour = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_disabled_text_colour", Color.DarkGray);

			parent = tcp;
			this.tree = tree;
			jumper = new FocusJumper ();

			string fontName = SkinningDefs.TheInstance.GetData("fontname");
			Font fontBold12 = ConstantSizeFont.NewFont (fontName, 12, FontStyle.Bold);
			Font fontBold10 = ConstantSizeFont.NewFont (fontName, 10, FontStyle.Bold);
			Font fontBold9 = ConstantSizeFont.NewFont (fontName, 9, FontStyle.Bold);

			BackgroundImage = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location +
				"\\images\\panels\\race_panel_back_normal.png");

			Label mainLabel = new Label ();
			mainLabel.Text = "Set Power Limit For Zone";
			mainLabel.Font = fontBold12;
			mainLabel.Size = new Size (500, 20);
			mainLabel.Location = new Point (10, 10);
			mainLabel.BackColor = Color.Transparent;
			Controls.Add(mainLabel);

			closeButton = new ImageTextButton(0);
			closeButton.SetVariants(@"/images/buttons/blank_small.png");
			closeButton.ButtonFont = fontBold9;
			closeButton.Size = new Size (80, 20);
			closeButton.Location = new Point (457 + 55, 5 + 180);
			closeButton.SetButtonText("Close", upColour, upColour, hoverColour, disabledColour);
			closeButton.Click += closeButton_Click;
			closeButton.Visible = true;
			Controls.Add(closeButton);
			jumper.Add(closeButton);

			limitsNode = tree.GetNamedNode("DemandContractLimit");

			// Zone buttons for thermal.
			for (int zone = 1; zone <= 7; zone++)
			{
				Label label = new Label ();
				label.Text = "Zone " + CONVERT.ToStr(zone);
				label.Font = fontBold10;
				label.BackColor = Color.Transparent;
				label.TextAlign = ContentAlignment.MiddleCenter;
				label.Size = new Size (60, 20);
				label.Location = new Point (30 + ((zone - 1) * 80), 50);

				Controls.Add(label);

				TextBox limit = new TextBox ();
				limit.KeyPress += limit_KeyPress;
				limit.Text = limitsNode.GetAttribute("z" + CONVERT.ToStr(zone) + "_limit");
				limit.Size = new Size (60, 20);
				limit.Location = new Point (30 + ((zone - 1) * 80), 80);

				Controls.Add(limit);

				fieldsByZone.Add(limit);

				jumper.Add(limit);
			}

			GotFocus += PowerPanel_GotFocus;
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing)
			{
				jumper.Dispose();
			}

			base.Dispose(disposing);
		}

		void UpdateValues ()
		{
			for (int zone = 1; zone <= 7; zone++)
			{
				TextBox box = fieldsByZone[zone - 1] as TextBox;

//				string incident = "<i id=\"AtStart\"> <apply i_name=\"DemandContractLimit\" z" + CONVERT.ToStr(zone) + "_limit=\"" + (string) (box.Text) + "\" /> </i>";
//				IncidentDefinition effect = new IncidentDefinition (incident, tree);
//				effect.ApplyAction(tree);

				limitsNode.SetAttribute("z" + CONVERT.ToStr(zone) + "_limit", (string) (box.Text));
			}
		}

		void closeButton_Click (object sender, EventArgs e)
		{
			UpdateValues();
			Close();
		}

		public void Close ()
		{
			parent.DisposeEntryPanel();
		}

		void PowerPanel_GotFocus (object sender, EventArgs e)
		{
			closeButton.Focus();
		}

		void limit_KeyPress (object sender, KeyPressEventArgs e)
		{
			if (! (char.IsDigit(e.KeyChar) || char.IsControl(e.KeyChar)))
			{
				e.Handled = true;
			}
		}
	}
}