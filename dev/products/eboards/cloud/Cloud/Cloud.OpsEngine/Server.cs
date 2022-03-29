using System;
using System.Collections.Generic;
using System.Text;

using Network;
using LibCore;

namespace Cloud.OpsEngine
{
	public class Server : IDisposable
	{
		Node serverNode;
		Node timeNode;

		int lastKnownTime;

		public Server (NodeTree model, Node serverNode)
		{
			this.serverNode = serverNode;

			timeNode = model.GetNamedNode("CurrentTime");
			timeNode.AttributesChanged += new Node.AttributesChangedEventHandler (timeNode_AttributesChanged);

			lastKnownTime = timeNode.GetIntAttribute("seconds", 0);
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
				if (serverNode.GetIntAttribute("time_till_ready", 0) > 0)
				{
					serverNode.SetAttribute("time_till_ready", Math.Max(0, serverNode.GetIntAttribute("time_till_ready", 0) - dt));
				}
			}
		}
	}
}