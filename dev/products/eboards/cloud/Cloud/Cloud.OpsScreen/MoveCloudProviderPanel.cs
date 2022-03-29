using System.Collections.Generic;
using System.Drawing;
using CommonGUI;
using Network;

using Cloud.OpsEngine;

namespace Cloud.OpsScreen
{
	public class MoveCloudProviderPanel : FlickerFreePanel
	{
		OrderPlanner orderPlanner;
		NodeTree model;

		List<Node> businesses;
		Dictionary<Node, ImageTextButton> businessToButton;
		BusinessMoveCloudProviderPanel panel;

		PublicVendorManager cloudVendorManager;

		public MoveCloudProviderPanel (OrderPlanner orderPlanner, NodeTree model, PublicVendorManager cloudVendorManager)
		{
			BackColor = Color.Transparent;

			this.orderPlanner = orderPlanner;
			this.model = model;
			this.cloudVendorManager = cloudVendorManager;

			businesses = new List<Node> ((Node []) model.GetNodesWithAttributeValue("type", "business").ToArray(typeof (Node)));
			businesses.Sort(delegate(Node a, Node b)
			{
				return a.GetIntAttribute("order", 0).CompareTo(b.GetIntAttribute("order", 0));
			});

			businessToButton = new Dictionary<Node, ImageTextButton> ();

			int x = 15;
			int gap = 15;
			foreach (Node business in businesses)
			{
				ImageTextButton button = new ImageTextButton (@"images\buttons\button_85x25.png");
				button.SetButtonText(business.GetAttribute("desc"), Color.White, Color.White, Color.White, Color.DimGray);
				button.ButtonPressed += new ImageButton.ImageButtonEventArgsHandler (button_ButtonPressed);
				button.Tag = business;
				button.SetAutoSize();
				button.Location = new Point (x, 0);
				businessToButton.Add(business, button);
				x = button.Right + gap;
				Controls.Add(button);
			}
		}

		void button_ButtonPressed (object sender, ImageButtonEventArgs args)
		{
			foreach (ImageTextButton button in businessToButton.Values)
			{
				button.Active = (button == sender);
			}

			if (panel != null)
			{
				panel.Dispose();
				panel = null;
			}

			Node business = (Node) ((ImageTextButton) sender).Tag;

			panel = new BusinessMoveCloudProviderPanel (orderPlanner, model, business, cloudVendorManager);
			Controls.Add(panel);
			panel.Location = new Point (10, 50);
			panel.Size = new Size (ClientSize.Width - panel.Left - 20, ClientSize.Height - panel.Top);
		}
	}
}