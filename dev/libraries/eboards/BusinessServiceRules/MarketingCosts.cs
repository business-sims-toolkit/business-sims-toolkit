using System;
using System.Collections;
using System.Xml;

using LibCore;
using Network;

using CoreUtils;

namespace BusinessServiceRules
{
	public class MarketingCosts : IDisposable
	{
		private NodeTree model;

		private Node marketingCostsNode;
		private Node timeNode;

		public MarketingCosts (NodeTree model)
		{
			this.model = model;

			timeNode = model.GetNamedNode("CurrentModelTime");
			marketingCostsNode = model.GetNamedNode("MarketingCosts");

			timeNode.AttributesChanged += new Network.Node.AttributesChangedEventHandler(timeNode_AttributesChanged);
		}

		public void Dispose ()
		{
			if (timeNode != null)
			{
				timeNode.AttributesChanged -= new Network.Node.AttributesChangedEventHandler(timeNode_AttributesChanged);
				timeNode = null;
			}
		}

		private void timeNode_AttributesChanged (Node sender, ArrayList attrs)
		{
			foreach (AttributeValuePair avp in attrs)
			{
				if (avp.Attribute == "time")
				{
					string [] components = avp.Value.Split(':');

					// Is this the start of a new hour?
					if (components[1] == "00")
					{
						FindAndChargeForDownedBSUs();
					}
				}
			}
		}

		private void FindAndChargeForDownedBSUs ()
		{
			ArrayList bsus = model.GetNodesWithAttributeValue("type", "biz_service_user");

			foreach (Node bsu in bsus)
			{
				if (bsu.GetAttribute("up") != "true")
				{
					string costBand = bsu.GetAttribute("extramarketcost");					
					int cost = SkinningDefs.TheInstance.GetIntData("extramarketcost" + costBand, 0);
					int currentTotal = marketingCostsNode.GetIntAttribute("costs", 0);
					currentTotal += cost;
					marketingCostsNode.SetAttribute("costs", currentTotal);
				}
			}
		}
	}
}