using System;
using System.Collections.Generic;
using Network;

namespace BusinessServiceRules
{
	public class AllBusinessesRestorationMonitor : IDisposable
	{
		NodeTree model;
		Node businesses;
		Dictionary<Node, BusinessRestorationMonitor> businessToMonitor;

		public AllBusinessesRestorationMonitor (NodeTree model, Node businesses)
		{
			this.model = model;
			this.businesses = businesses;

			businesses.ChildAdded += businesses_ChildAdded;
			businesses.ChildRemoved += businesses_ChildRemoved;

			businessToMonitor = new Dictionary<Node, BusinessRestorationMonitor> ();
			foreach (Node business in businesses.getChildren())
			{
				businessToMonitor.Add(business, new BusinessRestorationMonitor (model, business));
			}
		}

		public void Dispose ()
		{
			foreach (Node business in businessToMonitor.Keys)
			{
				businessToMonitor[business].Dispose();
			}

			businesses.ChildAdded -= businesses_ChildAdded;
			businesses.ChildRemoved -= businesses_ChildRemoved;
		}

		void businesses_ChildAdded (Node sender, Node child)
		{
			businessToMonitor.Add(child, new BusinessRestorationMonitor (model, child));
		}

		void businesses_ChildRemoved (Node sender, Node child)
		{
			businessToMonitor.Remove(child);
		}
	}
}