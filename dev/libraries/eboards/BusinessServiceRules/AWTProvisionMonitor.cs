using System;
using System.Collections;
using Network;

namespace BusinessServiceRules
{
	/// <summary>
	/// This is a monitor to determine whether the AWT is functional
	/// The AWT depends on App Boutsen and all it's parents being up and working
	/// This monitor switchs the up tag on the AdvancedWarningTechnology node to show system status 
	/// </summary>
	public class AWTProvisionMonitor
	{
		NodeTree _model;
		Node MyNodeToWatch;
		Node AWT_SystemNode;

		Boolean isFunctionallyUP = false;
		ArrayList RequiredNodes = new ArrayList();
		Hashtable NodeStatusCache = new Hashtable();
		
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="nt"></param>
		public AWTProvisionMonitor(NodeTree nt)
		{
			_model = nt;
			string namedNode = CoreUtils.SkinningDefs.TheInstance.GetData("awt_provision_name");
			AWT_SystemNode = _model.GetNamedNode("AdvancedWarningTechnology");
			MyNodeToWatch = _model.GetNamedNode(namedNode);

			BuildNodeMonitoring();
		}

		/// <summary>
		/// Dispose all event handlers and anything else
		/// </summary>
		public void Dispose()
		{
			DisposeNodeMonitoring();
		}

		/// <summary>
		/// Iterate over the required detaching the event handlers 
		/// </summary>
		protected void DisposeNodeMonitoring()
		{
			if (RequiredNodes.Count>0)
			{
				foreach (Node n2 in RequiredNodes)
				{
					n2.AttributesChanged -=Node_AttributesChanged;
					n2.ParentChanged -=Node_ParentChanged;
				}
			}
		}

		/// <summary>
		/// Build the Monitoring for all the required nodes 
		/// </summary>
		void BuildNodeMonitoring()
		{
			if (MyNodeToWatch != null)
			{
				RequiredNodes.Clear();
				NodeStatusCache.Clear();
				//walk up the tree 
				Boolean ReachedEnd = false;
				Node n1 = null;
				n1 = MyNodeToWatch;

				while ((n1 != null)&(ReachedEnd==false))
				{
					//RequiredMonitorCount++;
					string name1 = n1.GetAttribute("name");
					//System.Diagnostics.Debug.WriteLine("Adding "+name1);
					RequiredNodes.Add(n1);
					n1.AttributesChanged +=Node_AttributesChanged;
					n1.ParentChanged +=Node_ParentChanged;
					//Move up 
					n1 = n1.Parent;
					if (n1 != null)
					{
						string name = n1.GetAttribute("name");
						Boolean notin = n1.GetBooleanAttribute("notinnetwork", false);
						if ((name=="Root")||(notin==true))
						{
							ReachedEnd = true;
							//System.Diagnostics.Debug.WriteLine("Reached End with"+name);
						}
					}
				}
				BuildCache();
				ReEvaluateStatus(); 
			}
		}

		/// <summary>
		/// We keep a cache of the starus of all the nodes 
		/// saving us having to recheck every node when a change comes in
		/// </summary>
		protected void BuildCache()
		{
			//Interate over all required nodes and if any are down then we are
			if (RequiredNodes.Count>0)
			{
				foreach (Node n2 in RequiredNodes)
				{
					string Name = n2.GetAttribute("name");
					string Status = n2.GetAttribute("up");
					NodeStatusCache.Add(Name, Status);
				}
			}
		}

		/// <summary>
		/// Helper method for setting the AWT System Status  
		/// </summary>
		/// <param name="status"></param>
		protected void setAWTStatus(Boolean status)
		{
			if (AWT_SystemNode != null)
			{
				if (status)
				{
					AWT_SystemNode.SetAttribute("up","true");
				}
				else
				{
					AWT_SystemNode.SetAttribute("up","false");
				}
			}
		}

		/// <summary>
		/// Assume up and iterate over the cache to see if anything is down
		/// Set the system to the updated status
		/// </summary>
		protected void ReEvaluateStatus()
		{
			isFunctionallyUP = true;
			if (RequiredNodes.Count>0)
			{
				foreach (string status in NodeStatusCache.Values)
				{
					if (status.ToLower() == "false")
					{
						isFunctionallyUP = false;
					}
				}
			}
			setAWTStatus(isFunctionallyUP);
		}

		/// <summary>
		/// Handling a change in status of one of the required nodes 
		/// we are only interested in a change to the up status
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="attrs"></param>
		void Node_AttributesChanged(Node sender, ArrayList attrs)
		{
			//Just to update the cache if the up was involved
			foreach(AttributeValuePair avp in attrs)
			{
				if(avp.Attribute == "up")
				{
					string name = sender.GetAttribute("name");
					string valuestr = sender.GetAttribute("up");
					if (NodeStatusCache.ContainsKey(name))
					{
						NodeStatusCache[name] = valuestr;
						ReEvaluateStatus();
					}
				}
			}
		}

		/// <summary>
		/// If we have a change in Parent
		/// then dispose of current list of required nodes and rebuild
		/// Its much easier and cleaner than maintaining the modified set 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="child"></param>
		void Node_ParentChanged(Node sender, Node child)
		{
			//Detach from everything 
			DisposeNodeMonitoring();
			//Rebuild it all 
			BuildNodeMonitoring();
		}



	}
}
