using System.Collections;
using Network;

namespace BusinessServiceRules
{
	/// <summary>
	/// A BusinessServiceGroupMonitor attaches a OverallBusinessServiceMonitor to each business service.
	/// </summary>
	public class BusinessServiceGroupMonitor
	{
		protected Node _groupNode;
		protected Hashtable nodeToMonitor;

		public BusinessServiceGroupMonitor(Node groupNode)
		{
			_groupNode = groupNode;
			nodeToMonitor = new Hashtable();

			foreach(Node n in groupNode)
			{
				if(n.GetAttribute("type") == "biz_service")
				{
					OverallBusinessServiceMonitor monitor = new OverallBusinessServiceMonitor(n);
					nodeToMonitor.Add(n,monitor);
				}
			}

			groupNode.ChildAdded += groupNode_ChildAdded;
			groupNode.ChildRemoved += groupNode_ChildRemoved;
		}

		/// <summary>
		/// Dispose
		/// </summary>
		public void Dispose()
		{
			_groupNode.ChildAdded -= groupNode_ChildAdded;
			_groupNode.ChildRemoved -= groupNode_ChildRemoved;

			foreach(OverallBusinessServiceMonitor monitor in nodeToMonitor.Values)
			{
				monitor.Dispose();
			}

			nodeToMonitor.Clear();
		}

		void groupNode_ChildAdded(Node sender, Node child)
		{
			if(child.GetAttribute("type") == "biz_service")
			{
				OverallBusinessServiceMonitor monitor = new OverallBusinessServiceMonitor(child);
				nodeToMonitor.Add(child,monitor);
			}
		}

		void groupNode_ChildRemoved(Node sender, Node child)
		{
			OverallBusinessServiceMonitor monitor = (OverallBusinessServiceMonitor) nodeToMonitor[child];
			nodeToMonitor.Remove(child);
			monitor.Dispose();
		}
	}
}
