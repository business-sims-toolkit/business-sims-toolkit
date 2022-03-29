using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using CommonGUI;

using LibCore;
using Network;

using Cloud.OpsEngine;

namespace Cloud.OpsScreen
{
	public class CloudProviderSelectionSubPanel : FlickerFreePanel
	{
		NodeTree model;
		Node businessServiceOrDefinition;

		SelectionPanel<Node> providerPanel;
		OrderPlanner orderPlanner;

		bool saas;

		public delegate void ProviderChosenHandler (CloudProviderSelectionSubPanel sender, Node provider);
		public event ProviderChosenHandler ProviderChosen;

		protected Bitmap FullBackgroundImage;

		public CloudProviderSelectionSubPanel (OrderPlanner orderPlanner, NodeTree model, Node businessServiceOrDefinition, bool saas)
		{
			this.orderPlanner = orderPlanner;
			this.model = model;
			this.businessServiceOrDefinition = businessServiceOrDefinition;
			this.saas = saas;

			FullBackgroundImage = (Bitmap)Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "images\\panels\\PopupBack_Requests.png");
			BackgroundImage = FullBackgroundImage;

			providerPanel = new SelectionPanel<Node> ("Select Cloud Provider", 12, true);
			providerPanel.RearrangeItems += new SelectionPanel<Node>.RearrangeItemsHandler (providerPanel_RearrangeItems);
			providerPanel.ItemSelected += new SelectionPanel<Node>.ItemSelectedHandler (providerPanel_ItemSelected);

			Node roundVariablesNode = model.GetNamedNode("RoundVariables");
			int round = roundVariablesNode.GetIntAttribute("current_round", 0);

			Dictionary<Node, string> cloudProviderToDescription = new Dictionary<Node, string> ();
			foreach (Node cloudProvider in orderPlanner.GetSuitableCloudProviders(businessServiceOrDefinition, saas))
			{
				ImageTextButton button = providerPanel.AddItem(cloudProvider, cloudProvider.GetAttribute("desc"));
				button.Active = (businessServiceOrDefinition.GetAttribute("cloud_provider") == cloudProvider.GetAttribute("name"));
			}
			Controls.Add(providerPanel);

			DoSize();
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);
			DoSize();
		}

		void DoSize ()
		{
			providerPanel.Size = Size;
		}

		void providerPanel_ItemSelected (SelectionPanel<Node> sender, Node item)
		{
			OnProviderChosen(item);
		}

		void OnProviderChosen (Node provider)
		{
			if (ProviderChosen != null)
			{
				ProviderChosen(this, provider);
			}
		}

		void providerPanel_RearrangeItems (SelectionPanel<Node> sender, Control title, IList<Control> items)
		{
			int gap = 10;
			int y = title.Bottom + gap;

			foreach (Control control in items)
			{
				ImageTextButton button = control as ImageTextButton;
				if (button != null)
				{
					button.SetAutoSize();
					button.Location = new Point (0, y);
					y = button.Bottom + gap;
				}
			}
		}
	}
}