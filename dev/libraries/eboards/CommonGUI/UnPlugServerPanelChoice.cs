using System;
using System.Windows.Forms;
using System.Collections;
using System.Drawing;

using Network;
using LibCore;
using CoreUtils;

namespace CommonGUI
{
	public class UnPlugServerPanelChoice : FlickerFreePanel
	{
		FocusJumper jumper;
		UnPlugServerPanel parent;
		int zone;

		ImageTextButton cancelButton;

		NodeTree model;

		ArrayList nodesWithEvents = new ArrayList ();

		protected override void Dispose (bool disposing)
		{
			if (disposing)
			{
				foreach (Node server in nodesWithEvents)
				{
					server.AttributesChanged -= server_AttributesChanged;
				}
			}

			base.Dispose(disposing);
		}

		public UnPlugServerPanelChoice (UnPlugServerPanel parent, NodeTree tree, int zone, int Width)
		{
			model= tree;
			this.zone = zone;

			Color upColour = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_text_colour", Color.Black);
			Color downColour = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_down_text_colour", Color.White);
			Color hoverColour = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_hover_text_colour", Color.Green);
			Color disabledColour = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_disabled_text_colour", Color.DarkGray);

			string serverDisplayName = SkinningDefs.TheInstance.GetData("servername");
			if (string.IsNullOrEmpty(serverDisplayName))
			{
				serverDisplayName = "Cabinet";
			}


			this.parent = parent;
			jumper = new FocusJumper ();

			Size = new Size (Width, Height);

			string fontName = SkinningDefs.TheInstance.GetData("fontname");
			Font fontBold12 = ConstantSizeFont.NewFont (fontName, 12, FontStyle.Bold);
			Font fontBold10 = ConstantSizeFont.NewFont (fontName, 10, FontStyle.Bold);
			Font fontBold9 = ConstantSizeFont.NewFont (fontName, 9, FontStyle.Bold);

			BackgroundImage = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location +
				"\\images\\panels\\race_panel_back_normal.png");

			Label label = new Label ();
			label.Text = serverDisplayName +" On/Off";
			label.Font = fontBold12;
			label.Size = new Size (400, 20);
			label.Location = new Point (10, 10);
			label.BackColor = Color.Transparent;
			Controls.Add(label);

			Label legend = new Label ();
			legend.Text = "Select " + serverDisplayName.ToLower();
			legend.Font = fontBold10;
			legend.Size = new Size (400, 20);
			legend.Location = new Point (10, 30);
			legend.BackColor = Color.Transparent;
			Controls.Add(legend);

			cancelButton = new ImageTextButton(0);
			cancelButton.SetVariants(@"/images/buttons/blank_small.png");
			cancelButton.ButtonFont = fontBold9;
			cancelButton.Size = new Size (80, 20);
			cancelButton.Location = new Point (457 + 55, 5 + 180);
			cancelButton.SetButtonText("Close", upColour, upColour, hoverColour, disabledColour);
			cancelButton.Click += cancelButton_Click;
			cancelButton.Visible = true;
			Controls.Add(cancelButton);
			jumper.Add(cancelButton);

			int startX = 50;
			int x = startX;
			int y = 70;
			int width = 120;
			int lightGap = 5;
			int serverGap = 20;
			ArrayList list = tree.GetNodesWithAttributeValue("zone", CONVERT.ToStr(zone));
			ArrayList serverNames = new ArrayList ();
			Hashtable serverNameToNode = new Hashtable ();
			foreach (Node server in list)
			{
				if (server.GetAttribute("type").ToLower() == "server")
				{
					serverNames.Add(server.GetAttribute("name"));
					serverNameToNode.Add(server.GetAttribute("name"), server);
				}
			}

			serverNames.Sort();

			foreach (string serverName in serverNames)
			{
				Node server = serverNameToNode[serverName] as Node;

				server.AttributesChanged += server_AttributesChanged;
				nodesWithEvents.Add(server);

				ImageTextButton button = new ImageTextButton(0);
				button.SetVariants(@"/images/buttons/blank_small.png");
				button.ButtonFont = fontBold9;
				button.Size = new Size (width, 20);
				button.Location = new Point (x, y);
				x += width + lightGap;

				button.SetButtonText(serverName, upColour, upColour, hoverColour, disabledColour);
				button.Click += button_Click;
				button.Tag = server;
				button.Visible = true;
				Controls.Add(button);
				jumper.Add(button);

				ImageBox image = new ImageBox ();
				image.Tag = server;
				image.Size = new Size (button.Height * 3 / 4, button.Height * 3 / 4);
				image.Location = new Point (button.Right + lightGap, button.Top + ((button.Height - image.Height) / 2));
				Controls.Add(image);

				x += image.Width + serverGap;

				if ((x + width + lightGap + image.Width + serverGap) >= Width)
				{
					x = startX;
					y += 30;
				}

				Node zoneNode = tree.GetNamedNode("Zone" + CONVERT.ToStr(zone));
				button.Visible = ! zoneNode.GetBooleanAttribute("bladed", false);
				image.Visible = button.Visible;
			}

			UpdateState();

			GotFocus += UnPlugServerPanel_GotFocus;
		}

		protected void UpdateState ()
		{
			foreach (Control control in Controls)
			{
				ImageBox image = control as ImageBox;

				if (image != null)
				{
					Node server = image.Tag as Node;

					if (server != null)
					{
						if (server.GetAttribute("type") == "zone")
						{
							if (server.GetAttribute("activated") == "true")
							{
								image.Image = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + @"/images/buttons/on.png");
							}
							else
							{
								image.Image = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + @"/images/buttons/off.png");
							}
						}
						else
						{
							string zone = server.GetAttribute("zone");
							if (zone == "")
							{
								zone = server.GetAttribute("proczone");
							}

							Node zoneNode = model.GetNamedNode("Zone" + zone);
							bool zoneUp = zoneNode.GetBooleanAttribute("activated", false);

							// Disable the related button.
							foreach (Control other in Controls)
							{
								if (other.Tag == image.Tag)
								{
									other.Enabled = zoneUp;
								}
							}

							if (server.GetBooleanAttribute("powering_down", false))
							{
								image.Image = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + @"/images/buttons/changing.png");
							}
							else if (server.GetBooleanAttribute("turnedoff", false) || ! zoneUp)
							{
								image.Image = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + @"/images/buttons/off.png");
							}
							else
							{
								image.Image = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + @"/images/buttons/on.png");
							}
						}
					}
				}
			}
		}

		void cancelButton_Click (object sender, EventArgs e)
		{
			parent.DisposeChoicePanel();
		}

		ArrayList GetAppsDBsOnZone (string zone)
		{
			ArrayList itemsInZone = model.GetNodesWithAttributeValue("proczone", zone);
			ArrayList appsDBs = new ArrayList ();

			foreach (Node node in itemsInZone)
			{
				if ((node.GetAttribute("type") == "Database") || (node.GetAttribute("type") == "App"))
				{
					appsDBs.Add(node);
				}
			}

			return appsDBs;
		}

		void button_Click (object sender, EventArgs e)
		{
			ImageTextButton button = sender as ImageTextButton;
			Node server = button.Tag as Node;

			// Currently turned/ing off?
			if ((server.GetAttribute("turnedoff").ToLower() == "true") || (server.GetAttribute("powering_down") == "true"))
			{
				ArrayList attrs = new ArrayList ();
				attrs.Add(new AttributeValuePair ("type", "TurnServerOn"));
				attrs.Add(new AttributeValuePair ("server", server.GetAttribute("name")));
				new Node (model.GetNamedNode("ZoneActivationQueue"), "TurnServerOn", "", attrs);
			}
			// Must be turned on.
			else
			{
				ArrayList attrs = new ArrayList ();
				attrs.Add(new AttributeValuePair ("type", "TurnServerOff"));
				attrs.Add(new AttributeValuePair ("server", server.GetAttribute("name")));
				new Node (model.GetNamedNode("ZoneActivationQueue"), "TurnServerOff", "", attrs);
			}

			UpdateState();
		}

		void UnPlugServerPanel_GotFocus(object sender, EventArgs e)
		{
			cancelButton.Focus();
		}

		void server_AttributesChanged(Node sender, ArrayList attrs)
		{
			UpdateState();
		}
	}
}