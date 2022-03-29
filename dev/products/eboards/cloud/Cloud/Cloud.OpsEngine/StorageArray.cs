using System;
using System.Collections.Generic;
using System.Text;

using Network;
using LibCore;

namespace Cloud.OpsEngine
{
	public class StorageArray : IDisposable
	{
		Node storageArrayNode;
		Node timeNode;

		int lastKnownTime;

		public StorageArray (NodeTree model, Node storageArrayNode)
		{
			this.storageArrayNode = storageArrayNode;

			timeNode = model.GetNamedNode("CurrentTime");
			timeNode.AttributesChanged += new Node.AttributesChangedEventHandler (timeNode_AttributesChanged);
		}

		public void Dispose ()
		{
			timeNode.AttributesChanged -= new Node.AttributesChangedEventHandler (timeNode_AttributesChanged);
		}

		void timeNode_AttributesChanged (Node sender, System.Collections.ArrayList attrs)
		{
			int time = timeNode.GetIntAttribute("seconds", 0);
			int dt = time - lastKnownTime;

			lastKnownTime = time;

			if (dt > 0)
			{
				foreach (Node upgrade in storageArrayNode.GetChildrenOfType("storage_array_upgrade_delay"))
				{
					int countdown = Math.Max(0, upgrade.GetIntAttribute("time_till_ready", 0) - dt);
					if (countdown <= 0)
					{
						storageArrayNode.DeleteChildTree(upgrade);
					}
					else
					{
						if (upgrade.GetIntAttribute("time_till_ready", 0) != countdown)
						{
							upgrade.SetAttribute("time_till_ready", countdown);
						}
					}
				}
			}
		}
	}
}