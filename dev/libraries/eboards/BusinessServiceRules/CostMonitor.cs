using System;
using System.Collections;
using LibCore;
using Network;

namespace BusinessServiceRules
{
	/// <summary>
	/// CostMonitor watches a [CostedEvents] node in the model and costs up any costed events that are children of it.
	/// It then removes (consumes these child nodes). In order to value each costed event it loads a costs.xml
	/// file that defines how much each event costs. This same costs.xml file is used in reports.
	/// </summary>
	public class CostMonitor : IDisposable
	{
		/// <summary>
		/// monitoredEntity stores the Node in the model ("CostedEvents") that the CostMonitor watches for
		/// added children.
		/// </summary>
		protected Node monitoredEntity;

		/// <summary>
		/// The current running total cost.
		/// </summary>
		protected int totalSpent = 0;

		/// <summary>
		/// A hashtable that stores the mapping between a costed event type and its real monetary cost.
		/// </summary>
		protected Hashtable eventTypeToCost;

		/// <summary>
		/// The constructor for a CostMonitor.
		/// </summary>
		/// <param name="namedNode">The model's name of the node that will be watched for added children ["CostedEvents"].</param>
		/// <param name="nt">The NodeTree that the represents the game model.</param>
		/// <param name="costsfile">The costs.xml file that define what each costed event is valued at.</param>
		public CostMonitor (string namedNode, NodeTree nt, string costsfile)
		{
			monitoredEntity = nt.GetNamedNode(namedNode);
			string val = monitoredEntity.GetAttribute("totalSpent");
			if ("" != val)
			{
				totalSpent = CONVERT.ParseInt(val);
			}
			//
			eventTypeToCost = new Hashtable();
			//
			// Load costs definition file.
			//
			//string xmlFileName = GetNetworkFile(round, currentPhase);
			System.IO.StreamReader file = new System.IO.StreamReader(costsfile);
			string xmldata = file.ReadToEnd();
			file.Close();
			file = null;
			NodeTree costsTree = new NodeTree(xmldata);
			// Add costs for each type of costed event!
			foreach (Node n in costsTree.Root)
			{
				string eventCostStr = n.GetAttribute("cost");
				if ("" != eventCostStr)
				{
					int eventCost = CONVERT.ParseInt(eventCostStr);
					eventTypeToCost[n.GetAttribute("type")] = eventCost;
				}
			}
			//
			ArrayList delNodes = new ArrayList();
			foreach (Node n in monitoredEntity)
			{
				val = n.GetAttribute("cost");
				if ("" != val)
				{
					totalSpent += CONVERT.ParseInt(val);
				}
				delNodes.Add(n);
			}
			//
			foreach (Node n in delNodes)
			{
				n.Parent.DeleteChildTree(n);
			}
			//
			monitoredEntity.SetAttribute("totalSpent", totalSpent);
			monitoredEntity.ChildAdded += monitoredEntity_ChildAdded;
		}

		/// <summary>
		/// Event catcher methos that is called when the ["CostedEvents"] watched node has a chile added.
		/// </summary>
		/// <param name="sender">The parent node ["CostedEvents"] that has had a child added.</param>
		/// <param name="child">The new child node that represents the costed event.</param>
		void monitoredEntity_ChildAdded (Node sender, Node child)
		{
			string val = child.GetAttribute("type");
			if ("" != val)
			{
				if (eventTypeToCost.ContainsKey(val))
				{
					totalSpent += (int) eventTypeToCost[val];
					monitoredEntity.SetAttribute("totalSpent", totalSpent);
				}
			}
			sender.DeleteChildTree(child);
		}

		/// <summary>
		/// The Dispose method that unhooks the CostMonitor from the model node that is being watched.
		/// </summary>
		public void Dispose ()
		{
			monitoredEntity.ChildAdded -= monitoredEntity_ChildAdded;
		}

		public double GetCost (string type)
		{
			return (int) eventTypeToCost[type];
		}
	}
}
