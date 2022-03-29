using System;
using System.Collections;
using Network;

namespace IncidentManagement
{
	/// <summary>
	/// This class handles the business of determining whether a node is functionally up
	/// This is used in the AWT Monitor classes 
	/// We want to know if we can display this node as UP 
	/// So we need to monitor The node and all it's parents for changes 
	/// We build a list of nodes which are required to be up for that node to be up
	/// </summary>
	public class AWT_WatchNode
	{
		NodeTree MyNodeTree;
		Node MyNodeToWatch;
		Boolean isFunctionallyUP = false;
		ArrayList RequiredNodes = new ArrayList();
		Hashtable NodeStatusCache = new Hashtable();
		Boolean IncludeOnlyParents = true;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="NodeToWatch"></param>
		public AWT_WatchNode(Node NodeToWatch)
		{
			MyNodeToWatch = NodeToWatch;
			MyNodeTree = NodeToWatch.Tree;
			BuildNodeMonitoring();

			MyNodeTree.NodeMoved += MyNodeTree_NodeMoved;
		}

		/// <summary>
		/// Dispose all event handlers and anything else
		/// </summary>
		public void Dispose()
		{
			DisposeNodeMonitoring();
		}

		public void SetIncludeOnlyParents (bool newValue)
		{
			IncludeOnlyParents = newValue;
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

			MyNodeTree.NodeMoved -= MyNodeTree_NodeMoved;
		}

		/// <summary>
		/// Public method to access the status 
		/// </summary>
		/// <returns></returns>
		public Boolean isUP()
		{
			return isFunctionallyUP;
		}

		/// <summary>
		/// Public method to access the visibility
		/// </summary>
		/// <returns></returns>
		public bool isVisible()
		{
			return MyNodeToWatch.GetBooleanAttribute("visible", true);
		}

		/// <summary>
		/// Helper method for adding a required node 
		/// </summary>
		/// <param name="n1"></param>
		void AddRequiredNode_IfNew(Node n1)
		{
			if (RequiredNodes.Contains(n1)==false)
			{
				RequiredNodes.Add(n1);
				n1.AttributesChanged +=Node_AttributesChanged;
				n1.ParentChanged +=Node_ParentChanged;
				n1.ChildAdded +=n1_ChildAdded;
				n1.ChildRemoved +=n1_ChildRemoved;
			}
		}

		/// <summary>
		/// Helper method for adding an entire depends tree
		/// </summary>
		/// <param name="DependsOnNode"></param>
		void AddDependsTree(Node DependsOnNode)
		{
			Boolean ReachedEnd = false;

			Node n1 = DependsOnNode;
			if (null != n1)
			{
				while ((n1 != null)&(ReachedEnd==false))
				{
					//RequiredMonitorCount++;
					string name1 = n1.GetAttribute("name");
					//System.Diagnostics.Debug.WriteLine(" D Adding "+name1);

					//only add if we haven't got one already
					AddRequiredNode_IfNew(n1);
					//Move up 
					n1 = n1.Parent;
					if (n1 != null)
					{
						string name = n1.GetAttribute("name");
						Boolean notin = n1.GetBooleanAttribute("notinnetwork", false);
						if ((name=="Root")||(notin==true))
						{
							ReachedEnd = true;
							//System.Diagnostics.Debug.WriteLine(" D Reached End with"+name);
						}
					}
				}
			}
		}

		void CheckForDepends(Node Node1)
		{
			ArrayList kids = Node1.getChildren();
			if (kids.Count>0)
			{
				foreach(Node kid in kids)
				{
					string node_type = kid.GetAttribute("type");
					if (node_type.ToLower()=="dependson")
					{
						string node_name = kid.GetAttribute("item");
						Node n1 = MyNodeTree.GetNamedNode(node_name);
						AddDependsTree(n1);
					}
				}
			}
		}

		public void RefreshNodeMonitoring ()
		{
			BuildNodeMonitoring();
		}

		/// <summary>
		/// Building the required node list for this Node
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

				//System.Diagnostics.Debug.WriteLine("Monitor Node is "+MyNodeToWatch.GetAttribute("name"));

				if (IncludeOnlyParents)
				{
					//System.Diagnostics.Debug.WriteLine(" Check for Depends");
					CheckForDepends(MyNodeToWatch);
					n1 = MyNodeToWatch.Parent;
					//System.Diagnostics.Debug.WriteLine(" Starting with  "+n1.GetAttribute("name"));
				}
				else
				{
					//System.Diagnostics.Debug.WriteLine(" Check for Depends");
					CheckForDepends(MyNodeToWatch);
					n1 = MyNodeToWatch;
					//System.Diagnostics.Debug.WriteLine(" Starting with  "+n1.GetAttribute("name"));
				}

				while ((n1 != null)&(ReachedEnd==false))
				{
					//RequiredMonitorCount++;
					string name1 = n1.GetAttribute("name");
					//System.Diagnostics.Debug.WriteLine(" Adding "+name1);
					//only add if we haven't got one already
					AddRequiredNode_IfNew(n1);
					//check for Depends childs 
					if (n1 != MyNodeToWatch)
					{
						CheckForDepends(n1);
					}

					//Move up 
					n1 = n1.Parent;
					if (n1 != null)
					{
						string name = n1.GetAttribute("name");
						Boolean notin = n1.GetBooleanAttribute("notinnetwork", false);
						if ((name=="Root")||(notin==true))
						{
							ReachedEnd = true;
							//System.Diagnostics.Debug.WriteLine(" Reached End with"+name);
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
			//Interate over all required nodes, building the cache
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
		/// Assume up and iterate of the cache to see if any thing is down
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
		}

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

		void Node_ParentChanged(Node sender, Node child)
		{
			//Detach from everything 
			DisposeNodeMonitoring();
			//Rebuild it all 
			BuildNodeMonitoring();
		}

		void n1_ChildAdded(Node sender, Node child)
		{
			string node_type = child.GetAttribute("type");
			if (node_type.ToLower()=="dependson")
			{
				//Detach from everything 
				DisposeNodeMonitoring();
				//Rebuild it all 
				BuildNodeMonitoring();
			}
		}

		void n1_ChildRemoved(Node sender, Node child)
		{
			string node_type = child.GetAttribute("type");
			if (node_type.ToLower()=="dependson")
			{
				//Detach from everything 
				DisposeNodeMonitoring();
				//Rebuild it all 
				BuildNodeMonitoring();
			}
		}

		void MyNodeTree_NodeMoved(NodeTree sender, Node oldParent, Node movedNode)
		{
			if (movedNode == MyNodeToWatch)
			{
				DisposeNodeMonitoring();
				BuildNodeMonitoring();
			}
		}
	}
}