using System;
using System.Collections;
using System.IO;
using System.Xml;

using Network;

using LibCore;
using CoreUtils;

using IncidentManagement;


namespace Polestar_PM.OpsEngine
{
	/// <summary>
	/// Summary description for CurrentProjectLookup.
	/// </summary>
	public class CurrentProjectLookup
	{
		protected NodeTree MyNodeTree = null;
		protected Node running_project_node = null;
		protected Hashtable ProjectsBySlot = new Hashtable();
		protected Hashtable ProjectsByID = new Hashtable();

		public CurrentProjectLookup(NodeTree tree)
		{
			//Build the nodes 
			MyNodeTree = tree;
			running_project_node = MyNodeTree.GetNamedNode("pm_projects_running");

			foreach (Node prj_node in  running_project_node.getChildren())
			{
				int slot_id = prj_node.GetIntAttribute("slot",0);
				int project_id = prj_node.GetIntAttribute("project_id",0);
				
				if (ProjectsBySlot.ContainsKey(slot_id)==false)
				{
					ProjectsBySlot.Add(slot_id,prj_node);
				}
				if (ProjectsByID.ContainsKey(project_id)==false)
				{
					ProjectsByID.Add(project_id,prj_node);
				}
			}
			running_project_node.ChildAdded +=new Network.Node.NodeChildAddedEventHandler(running_project_node_ChildAdded);
			running_project_node.ChildRemoved +=new Network.Node.NodeChildRemovedEventHandler(running_project_node_ChildRemoved);
		}

		public void Dispose()
		{
			if (running_project_node != null)
			{
				running_project_node.ChildAdded -=new Network.Node.NodeChildAddedEventHandler(running_project_node_ChildAdded);
				running_project_node.ChildRemoved -=new Network.Node.NodeChildRemovedEventHandler(running_project_node_ChildRemoved);
			}
		}

		private void running_project_node_ChildAdded(Node sender, Node child)
		{
			int slot_id = child.GetIntAttribute("slot",-1);
			int project_id = child.GetIntAttribute("project_id",0);
				
			if (ProjectsBySlot.ContainsKey(slot_id)==false)
			{
				ProjectsBySlot.Add(slot_id,child);
			}
			if (ProjectsByID.ContainsKey(project_id)==false)
			{
				ProjectsByID.Add(project_id,child);
			}
		}

		private void running_project_node_ChildRemoved(Node sender, Node child)
		{
			int slot_id = child.GetIntAttribute("slot",0);
			int project_id = child.GetIntAttribute("project_id",0);
				
			if (ProjectsBySlot.ContainsKey(slot_id))
			{
				ProjectsBySlot.Remove(slot_id);
			}
			if (ProjectsByID.ContainsKey(project_id))
			{
				ProjectsByID.Remove(project_id);
			}
		}

		public bool isSlotUsed(int slot_number)
		{
			bool isused = false;
			if (ProjectsBySlot.ContainsKey(slot_number))
			{
				isused = true;
			}
			return isused;
		}

		public bool isProjectUsed(int prj_id_number)
		{
			bool isused = false;
			if (ProjectsByID.ContainsKey(prj_id_number))
			{
				isused = true;
			}
			return isused;
		}

		public Node getProjectNode(int prj_id_number)
		{
			Node prjnode = null;
			if (ProjectsByID.ContainsKey(prj_id_number))
			{
				prjnode = (Node) ProjectsByID[prj_id_number];
			}
			return prjnode;
		}


	}
}
