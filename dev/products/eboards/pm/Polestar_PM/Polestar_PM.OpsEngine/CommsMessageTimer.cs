using System;
using System.Collections;

using Network;
using LibCore;
using CoreUtils;

namespace Polestar_PM.OpsEngine
{
	public class CommsMessageTimer : IDisposable
	{
		NodeTree model;

		Node timeNode;
		Node commsList;

		int currentTime;

		public CommsMessageTimer (NodeTree model)
		{
			this.model = model;

			timeNode = model.GetNamedNode("CurrentTime");
			timeNode.AttributesChanged += new Network.Node.AttributesChangedEventHandler (timeNode_AttributesChanged);

			commsList = model.GetNamedNode("comms_list");

			currentTime = -1;
		}

		public void Dispose ()
		{
			timeNode.AttributesChanged -= new Network.Node.AttributesChangedEventHandler (timeNode_AttributesChanged);
		}

		private void timeNode_AttributesChanged (Node sender, ArrayList attrs)
		{
			foreach (AttributeValuePair avp in attrs)
			{
				if (avp.Attribute == "seconds")
				{
					int seconds = CONVERT.ParseIntSafe(avp.Value, 0);
					Tick(seconds);
				}
			}
		}

		void Tick (int newTime)
		{
			if (currentTime != -1)
			{
				int dt = newTime - currentTime;

				foreach (Node message in commsList.getChildrenClone())
				{
					if (message.GetAttribute("timeout") != "")
					{
						int timeout = message.GetIntAttribute("timeout", 0);
						timeout -= dt;

						if (timeout > 0)
						{
							message.SetAttribute("timeout", timeout);
						}
						else
						{
							commsList.DeleteChildTree(message);
						}
					}
				}
			}

			currentTime = newTime;
		}
	}
}