using System;
using System.Collections.Generic;
using System.Text;

using Network;
using LibCore;

namespace Cloud.OpsEngine
{
	public class Datacenter : IDisposable
	{
		NodeTree model;
		Node datacenterNode;
		Dictionary<Node, Rack> nodeToRack;

		Node timeNode;

		public Datacenter (NodeTree model, Node datacenter)
		{
			this.model = model;
			datacenterNode = datacenter;

			nodeToRack = new Dictionary<Node, Rack> ();
			foreach (Node rackNode in datacenterNode.GetChildrenOfType("rack"))
			{
				nodeToRack.Add(rackNode, new Rack (model, rackNode));
			}

			timeNode = model.GetNamedNode("CurrentTime");
			timeNode.AttributesChanged += new Node.AttributesChangedEventHandler (timeNode_AttributesChanged);
		}

		void timeNode_AttributesChanged (Node sender, System.Collections.ArrayList attrs)
		{
			RecalculateIdleCpus();
		}

		public void Dispose ()
		{
			foreach (Node rackNode in nodeToRack.Keys)
			{
				nodeToRack[rackNode].Dispose();
			}

			timeNode.AttributesChanged -= new Node.AttributesChangedEventHandler (timeNode_AttributesChanged);
		}

		void RecalculateIdleCpus ()
		{
			if (datacenterNode.GetBooleanAttribute("is_cloud", false))
			{
				foreach (Node rack in datacenterNode.GetChildrenOfType("rack"))
				{
					foreach (Node server in rack.GetChildrenOfType("server"))
					{
					}
				}
			}
		}
	}
}