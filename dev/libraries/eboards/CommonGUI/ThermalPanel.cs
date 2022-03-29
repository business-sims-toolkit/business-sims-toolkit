using System;
using System.Windows.Forms;
using System.Collections;
using System.Drawing;

using Network;
using LibCore;
using CoreUtils;
using IncidentManagement;

namespace CommonGUI
{
	public class ThermalPanel : FlickerFreePanel
	{
		FocusJumper jumper;
		NodeTree tree;
		OpsControlPanel parent;

		ImageTextButton cancelButton;

		Hashtable incidentToListOfNodes = new Hashtable ();

		public ThermalPanel (OpsControlPanel_DC tcp, NodeTree tree)
		{
			Hashtable nodeToIncident = tree.GetNodesWithAttribute("incident_id");
			foreach (Node node in nodeToIncident.Keys)
			{
				string incident = (string) nodeToIncident[node];
				int incidentNumber = CONVERT.ParseIntSafe(incident, -1);

				// Skip incidents on retired services.
				if (node.GetAttribute("type").ToLower().StartsWith("retired"))
				{
					continue;
				}

				// Skip incidents with text in the name (eg suzuka_turn_off, jerez_overload).
				if (CONVERT.ToStr(incidentNumber) == incident)
				{
					if (! incidentToListOfNodes.ContainsKey(incidentNumber))
					{
						incidentToListOfNodes.Add(incidentNumber, new ArrayList ());
					}

					((ArrayList) (incidentToListOfNodes[incidentNumber])).Add(node);
				}
			}

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

			Label label = new Label ();
			label.Text = "Fix Incident By Consultancy";
			label.Font = fontBold12;
			label.Size = new Size (500, 20);
			label.Location = new Point (10, 10);
			label.BackColor = Color.Transparent;
			Controls.Add(label);

			cancelButton = new ImageTextButton(0);
			cancelButton.SetVariants(@"/images/buttons/blank_small.png");
			cancelButton.ButtonFont = fontBold10;
			cancelButton.Size = new Size (80, 20);
            cancelButton.Location = new Point(520, 185);
          

			cancelButton.SetButtonText("Close", upColour, upColour, hoverColour, disabledColour);
			cancelButton.Click += cancelButton_Click;
			cancelButton.Visible = true;
			Controls.Add(cancelButton);
			jumper.Add(cancelButton);

			int x = 30;
			int y = 50;
			ArrayList sortedIncidents = new ArrayList (incidentToListOfNodes.Keys);
			sortedIncidents.Sort();
			foreach (int incidentNumber in sortedIncidents)
			{
				string incident = CONVERT.ToStr(incidentNumber);

				IncidentDefinition incidentDefinition = tcp.IncidentApplier.GetIncident(incident);

				if ((incidentDefinition != null) && incidentDefinition.IsPenalty)
				{
					continue;
				}

				ImageTextButton button = new ImageTextButton(0);
				button.SetVariants(@"/images/buttons/blank_small.png");
				button.ButtonFont = fontBold9;
				button.Size = new Size (60, 20);
				button.Location = new Point (x, y);
				button.SetButtonText(incident, upColour, upColour, hoverColour, disabledColour);
				button.Click += incidentButton_Click;
				button.Tag = incidentNumber;

				x += button.Width + 30;
				if ((x + button.Width) >= (550 - 30))
				{
					x = 30;
					y += 30;
				}

				Controls.Add(button);
				jumper.Add(button);
			}

			GotFocus += ThermalPanel_GotFocus;
		}

		void cancelButton_Click (object sender, EventArgs e)
		{
			Close();
		}

		public void Close ()
		{
			parent.DisposeEntryPanel();
		}

		void incidentButton_Click (object sender, EventArgs e)
		{
			ImageTextButton button = sender as ImageTextButton;

			int incidentId = (int) button.Tag;
			Node node = (Node) (((ArrayList) (incidentToListOfNodes[incidentId]))[0]);

			AttributeValuePair avp = new AttributeValuePair ("target", node.GetAttribute("name"));
			new Node (tree.GetNamedNode("FixItQueue"), "fix_by_consultancy", "", avp);

			ArrayList attributes = new ArrayList ();
			attributes.Add(new AttributeValuePair ("type", "fix_thermal_by_consultancy"));
			attributes.Add(new AttributeValuePair ("incident_id", incidentId));
			new Node(tree.GetNamedNode("CostedEvents"), "fix_thermal_by_consultancy", "", attributes);

			Close();

			(parent as OpsControlPanel_DC).CloseToolTips();
		}

		void ThermalPanel_GotFocus (object sender, EventArgs e)
		{
			cancelButton.Focus();
		}
	}
}