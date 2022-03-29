using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

using CommonGUI;
using Network;

using Cloud.OpsEngine;

namespace Cloud.OpsScreen
{
	public class CloudProviderSelectionPanel : FlickerFreePanel
	{
		NodeTree model;

		Node business;
		List<Node> businessServices;
		Dictionary<Node, ImageTextButton> cloudProviderToButton;

		OrderPlanner orderPlanner;

		Node cloudDeploymentLocation;

		bool isSaaS;

		public CloudProviderSelectionPanel (OrderPlanner orderPlanner, NodeTree model, List<Node> businessServices, bool isSaaS)
		{
			this.model = model;
			this.businessServices = new List<Node> (businessServices);
			this.orderPlanner = orderPlanner;
			this.isSaaS = isSaaS;

			if (businessServices.Count > 0)
			{
				business = businessServices[0].Parent;
			}

			string cloudDeploymentLocationName = isSaaS ? "Public SaaS Cloud Server" : "Public IaaS Cloud Server";
			cloudDeploymentLocation = model.GetNamedNode(cloudDeploymentLocationName);

			List<Node> cloudProviders = new List<Node> ((Node []) model.GetNodesWithAttributeValue("type", "cloud_provider").ToArray(typeof (Node)));
			cloudProviders.Sort(delegate (Node a, Node b)
								{
									return a.GetAttribute("desc").CompareTo(b.GetAttribute("desc"));
								});

			cloudProviderToButton = new Dictionary<Node, ImageTextButton> ();
			int x = 0;
			int gap = 15;
			foreach (Node cloudProvider in cloudProviders)
			{
				ImageTextButton button = new ImageTextButton (@"images\buttons\button_85x25.png");
				Controls.Add(button);
				button.Location = new Point (x, 0);
				button.SetAutoSize();
				x = button.Right + gap;
				button.Tag = cloudProvider;
				button.SetButtonText(cloudProvider.GetAttribute("desc"), Color.White, Color.White, Color.White, Color.DimGray);
				button.ButtonPressed += new ImageButton.ImageButtonEventArgsHandler (button_ButtonPressed);
				cloudProviderToButton.Add(cloudProvider, button);
			}

			UpdateButtons();
		}

		void UpdateButtons ()
		{
			foreach (Node cloudProvider in cloudProviderToButton.Keys)
			{
				bool selected = false;
				bool enabled = false;

				foreach (Node businessService in businessServices)
				{
					if (orderPlanner.GetSelectedCloudProvider(businessService) == cloudProvider)
					{
						selected = true;
					}
		
					if (orderPlanner.IsCloudProviderActive(cloudProvider)
						&& orderPlanner.CanCloudProviderHandleService(cloudProvider, businessService, isSaaS))
					{
						enabled = true;
					}
				}

				cloudProviderToButton[cloudProvider].Active = selected;
				cloudProviderToButton[cloudProvider].Enabled = enabled;
			}
		}

		void button_ButtonPressed (object sender, ImageButtonEventArgs args)
		{
			Node cloudProvider = (Node) ((ImageTextButton) sender).Tag;
			Node plannedOrders = model.GetNamedNode("PlannedOrders");
			plannedOrders.DeleteChildren();

			bool errors = false;

			if (! orderPlanner.AddMassCloudVendorChangePlanToQueue(plannedOrders, businessServices, cloudProvider))
			{
				errors = true;
			}

			StringBuilder builder = new StringBuilder ();
			foreach (Node error in plannedOrders.GetChildrenOfType("error"))
			{
				builder.AppendLine(error.GetAttribute("message"));
				errors = true;
			}

			if (errors)
			{
				MessageBox.Show(builder.ToString());
			}
			else
			{
				foreach (Node command in plannedOrders.GetChildrenOfType("order"))
				{
					model.MoveNode(command, model.GetNamedNode("IncomingOrders"));
				}
			}

			plannedOrders.DeleteChildren();

			UpdateButtons();
		}
	}
}