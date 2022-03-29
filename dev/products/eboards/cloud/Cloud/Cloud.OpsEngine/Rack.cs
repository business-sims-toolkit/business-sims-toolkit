using System;
using System.Collections.Generic;
using System.Text;

using Network;
using LibCore;

namespace Cloud.OpsEngine
{
	public class Rack : IDisposable
	{
		NodeTree model;
		Node rackNode;
		Dictionary<Node, Server> nodeToServer;

		Node timeNode;

		public Rack (NodeTree model, Node rackNode)
		{
			this.model = model;
			this.rackNode = rackNode;

			rackNode.ChildAdded += new Node.NodeChildAddedEventHandler (rackNode_ChildAdded);
			rackNode.ChildRemoved += new Node.NodeChildRemovedEventHandler (rackNode_ChildRemoved);

			nodeToServer = new Dictionary<Node, Server> ();
			foreach (Node serverNode in rackNode.GetChildrenOfType("server"))
			{
				nodeToServer.Add(serverNode, new Server (model, serverNode));
			}

			timeNode = model.GetNamedNode("CurrentTime");
			timeNode.AttributesChanged += new Node.AttributesChangedEventHandler (timeNode_AttributesChanged);
		}

		void timeNode_AttributesChanged (Node sender, System.Collections.ArrayList attrs)
		{
			RecalculateUsage();
		}

		public void Dispose ()
		{
			rackNode.ChildAdded -= new Node.NodeChildAddedEventHandler (rackNode_ChildAdded);
			rackNode.ChildRemoved -= new Node.NodeChildRemovedEventHandler (rackNode_ChildRemoved);

			foreach (Node serverNode in nodeToServer.Keys)
			{
				nodeToServer[serverNode].Dispose();
			}

			timeNode.AttributesChanged -= new Node.AttributesChangedEventHandler (timeNode_AttributesChanged);
		}

		void rackNode_ChildRemoved (Node sender, Node child)
		{
			nodeToServer[child].Dispose();
			nodeToServer.Remove(child);
		}

		void rackNode_ChildAdded (Node sender, Node child)
		{
			nodeToServer.Add(child, new Server (model, child));
		}

		void RecalculateUsage ()
		{
			int usedCpus = 0;
			int totalCpus = 0;
			foreach (Node serverNode in nodeToServer.Keys)
			{
				totalCpus += serverNode.GetIntAttribute("cpus", 0);

				foreach (Node serverLinkToVmInstance in serverNode.GetChildrenOfType("server_link_to_vm_instance"))
				{
					usedCpus += serverLinkToVmInstance.GetIntAttribute("cpus", 0);
				}
			}

			int usagePercent = (int) (100 * usedCpus * 1.0 / Math.Max(1, totalCpus));

			if (rackNode.GetIntAttribute("cpu_usage_percent", 0) != usagePercent)
			{
				rackNode.SetAttribute("cpu_usage_percent", usagePercent);
			}
		}
	}
}