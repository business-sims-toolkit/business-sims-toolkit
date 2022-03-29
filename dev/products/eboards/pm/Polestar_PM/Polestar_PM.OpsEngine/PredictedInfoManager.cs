using System;
using System.Collections;

using Network;
using LibCore;
using CoreUtils;

namespace Polestar_PM.OpsEngine
{
	public class PredictedInfoManager
	{
		NodeTree model;
		Node timeNode;
		Node predictedMarketInfoNode;

		public PredictedInfoManager(NodeTree model)
		{
			this.model = model;
			timeNode = model.GetNamedNode("CurrentTime");
			timeNode.AttributesChanged += new Network.Node.AttributesChangedEventHandler (timeNode_AttributesChanged);
			predictedMarketInfoNode = model.GetNamedNode("predicted_market_info");
		}

		public void Dispose()
		{
			timeNode.AttributesChanged -= new Network.Node.AttributesChangedEventHandler(timeNode_AttributesChanged);
		}

		private void timeNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			foreach (AttributeValuePair avp in attrs)
			{
				if (avp.Attribute == "seconds")
				{
					int displaytime = predictedMarketInfoNode.GetIntAttribute("displaytime",0);
					if (displaytime > 0)
					{
						displaytime--;
						predictedMarketInfoNode.SetAttribute("displaytime", CONVERT.ToStr(displaytime));
					}
					else
					{
						bool display = predictedMarketInfoNode.GetBooleanAttribute("displaytext", false);
						if (display)
						{
							predictedMarketInfoNode.SetAttribute("displaytext", "false");
						}
					}
				}
			}
		}

	}
}
