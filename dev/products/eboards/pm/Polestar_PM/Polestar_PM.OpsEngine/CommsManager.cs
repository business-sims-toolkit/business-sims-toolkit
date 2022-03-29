using System;
using System.Collections;
using System.IO;
using System.Xml;

using Network;
using LibCore;
using CoreUtils;
using Polestar_PM.DataLookup;

namespace Polestar_PM.OpsEngine
{
	/// <summary>
	/// The Comms Manager handles all the Communications and Notifications that need to be displayed 
	/// There are 3 sources for these messages 
	///   External Blocking Days 
	///   Change Card / FSC and Upgrade Events  
	///   Project Events  
	///   Adhoc Events (direct XML) 
	///  
	/// This class monitors these items and creates nodes which represents the neccessary messages 
	/// These message nodes are created under comms_list that is monitored by the GameCommsDisplay
	///  
	///
	/// </summary>
	public class CommsManager
	{
		protected NodeTree MyNodeTree = null;
		protected Node comms_list_node = null;  //Output node for messages
		protected Hashtable monitored_nodes = new Hashtable();
		protected Node currentTimeNode;

		protected Node DailyDepartmentStaffReportMsgNode = null;

		protected Hashtable employees = new Hashtable();
		protected ArrayList runningprojects = new ArrayList();
		protected Hashtable Project_finNodes = new Hashtable();
		protected Hashtable Project_prjNodes = new Hashtable();

		//Nodes that we need to monitor
		protected Node node_ops_worklist = null;
		protected Node node_pm_projects_running = null;
		
		public CommsManager (NodeTree tree)
		{
			//Build the nodes 
			MyNodeTree = tree;

			currentTimeNode = MyNodeTree.GetNamedNode("CurrentTime");
			currentTimeNode.AttributesChanged += new Network.Node.AttributesChangedEventHandler(currentTimeNode_AttributesChanged);

			comms_list_node = MyNodeTree.GetNamedNode("comms_list");
			BuildMonitoring();
			BuildEmployeeMonitoring();
		}

		public void Dispose()
		{
			//leave the Message Nodes in place
			foreach (Node n in monitored_nodes.Keys)
			{
				n.AttributesChanged -=new Network.Node.AttributesChangedEventHandler(mon_node_AttributesChanged);
			}

			if (currentTimeNode != null)
			{
				currentTimeNode.AttributesChanged -= new Network.Node.AttributesChangedEventHandler(currentTimeNode_AttributesChanged);
				currentTimeNode = null;
			}

			DisposeEmployeeMonitoring();
			DisposeMonitoring();

			MyNodeTree = null;
			comms_list_node = null;
		}

		#region Monitoring Methods

		public void BuildMonitoring()
		{
			//=======================================
			//Need to monitor the pending Ops Actions 
			//=======================================
			node_ops_worklist = this.MyNodeTree.GetNamedNode("ops_worklist");
			if (node_ops_worklist != null)
			{
				node_ops_worklist.ChildAdded +=new Network.Node.NodeChildAddedEventHandler(node_ops_worklist_ChildAdded);
				node_ops_worklist.ChildRemoved +=new Network.Node.NodeChildRemovedEventHandler(node_ops_worklist_ChildRemoved);
			}
			//add in any children all ready there 
			foreach (Node n in node_ops_worklist.getChildren())
			{
				ops_child_added(n);
			}

			//=======================================
			//Need to monitor the project as they run 
			//=======================================
			node_pm_projects_running = this.MyNodeTree.GetNamedNode("pm_projects_running");
			if (node_pm_projects_running != null)
			{
				node_pm_projects_running.ChildAdded +=new Network.Node.NodeChildAddedEventHandler(node_pm_projects_running_ChildAdded);
				node_pm_projects_running.ChildRemoved +=new Network.Node.NodeChildRemovedEventHandler(node_pm_projects_running_ChildRemoved);
			}
			//add in any children all ready there 
			foreach (Node n in node_pm_projects_running.getChildren())
			{
				project_child_added(n);
			}
		}

		public void DisposeMonitoring()
		{
			if (node_ops_worklist != null)
			{
				node_ops_worklist.ChildAdded -=new Network.Node.NodeChildAddedEventHandler(node_ops_worklist_ChildAdded);
				node_ops_worklist.ChildRemoved -=new Network.Node.NodeChildRemovedEventHandler(node_ops_worklist_ChildRemoved);
				node_ops_worklist = null;
			}
			//Need to monitor the project as they run 
			if (node_pm_projects_running != null)
			{
				node_pm_projects_running.ChildAdded -=new Network.Node.NodeChildAddedEventHandler(node_pm_projects_running_ChildAdded);
				node_pm_projects_running.ChildRemoved -=new Network.Node.NodeChildRemovedEventHandler(node_pm_projects_running_ChildRemoved);
				node_pm_projects_running = null;
			}
		}

		private void ops_child_added(Node child)
		{
			/*
			Node mon_node = child;
			if (monitored_nodes.Contains(mon_node)==false)
			{
				//Extract the required data 
				string node_name = mon_node.GetAttribute("name");
				string node_display = mon_node.GetAttribute("display");
				string node_status_str = mon_node.GetAttribute("status");
				string display_title = "Operations Team: "+node_display;
				string display_content = node_status_str;
				string display_day = mon_node.GetAttribute("day");

				switch (node_status_str.ToLower())
				{
					case "todo":
						display_content = "Day " + display_day;
						break;
					case "inprogress":
						display_content = "Working on during day " + display_day;
						break;
					case "done":
						display_content = "Completed on day " + display_day;
						break;
					default:
						display_content = "status:" + node_status_str + " day:" + display_day;
						break;
				}

				ArrayList attrs = new ArrayList ();
				attrs.Add(new AttributeValuePair ("type", "msg"));
				attrs.Add(new AttributeValuePair ("subtype", "ops_msg"));
				attrs.Add(new AttributeValuePair("display_title", display_title + " - " + display_content));
				attrs.Add(new AttributeValuePair ("display_content", display_content));
				attrs.Add(new AttributeValuePair ("display_icon", "block_msg"));
				//attrs.Add(new AttributeValuePair ("day", day_number));
				Node msg_node = new Node (comms_list_node, "msg", "", attrs);	

				monitored_nodes.Add(mon_node, msg_node);
				mon_node.AttributesChanged +=new Network.Node.AttributesChangedEventHandler(mon_node_AttributesChanged);
			}*/
		}

		private void node_ops_worklist_ChildAdded(Node sender, Node child)
		{
			ops_child_added(child);
		}

		private void ops_child_removed(Node child)
		{
			Node mon_node = child;
			if (monitored_nodes.ContainsKey(mon_node))
			{
				//Extract the required data 
				Node msg_node = (Node) monitored_nodes[mon_node];
				//Remove the msg node 
				msg_node.Parent.DeleteChildTree(msg_node);
				//Remove from monitoring 
				monitored_nodes.Remove(mon_node);
				//No catching the Changes 
				mon_node.AttributesChanged -=new Network.Node.AttributesChangedEventHandler(mon_node_AttributesChanged);
			}
		}

		private void node_ops_worklist_ChildRemoved(Node sender, Node child)
		{
			ops_child_removed(child);
		}

		private void project_child_added(Node child)
		{
			Node project_node = child;
			if (project_node != null)
			{
				runningprojects.Add(project_node);
				ProjectReader pr = new ProjectReader(project_node);
				Node prj_findata_subnode = pr.getFinSubNode();
				Node prj_prjdata_subnode = pr.getProjectSubNode();
				Node prj_workdata_subnode = pr.getWorkSubNode();
				prj_findata_subnode.AttributesChanged +=new Network.Node.AttributesChangedEventHandler(prj_findata_subnode_AttributesChanged);
				prj_prjdata_subnode.AttributesChanged +=new Network.Node.AttributesChangedEventHandler(prj_prjdata_subnode_AttributesChanged);
			}
		}

		private void prj_prjdata_subnode_AttributesChanged(Node sender, ArrayList attrs)
		{
			foreach(AttributeValuePair avp in attrs)
			{
				if(avp.Attribute == "next_stage_duration_display_count")
				{
					int count = sender.GetIntAttribute("next_stage_duration_display_count",-1);
					if (count==0)
					{
						//Turning Solvent So we need to remove the Note
						//do we have a monitored fin node
						if (Project_prjNodes.ContainsKey(sender))
						{
							//remove it and the 
							Node msg_node = (Node) Project_prjNodes[sender];
							msg_node.Parent.DeleteChildTree(msg_node);
							Project_prjNodes.Remove(sender);
						}
					}
					if (count==1)
					{
						//Turning InSolvent So we need to add the Note
						if (Project_prjNodes.ContainsKey(sender)==false)
						{
							//Extract the details from the Project 
							Node Project_node = sender.Parent;
							ProjectReader pr = new ProjectReader(Project_node);
							
							string reason = pr.getDurationDisplayReason();
							string project_id = CONVERT.ToStr(pr.getProjectID());
							string title_content = "Project Manager: Project Duration Change";
							string body_content = "Project "+project_id+" has changed duration ("+reason+")";

							ArrayList attrs2 = new ArrayList ();
							attrs2.Add(new AttributeValuePair ("type", "msg"));
							attrs2.Add(new AttributeValuePair ("timeout", "120"));
							attrs2.Add(new AttributeValuePair ("subtype", "prj_msg"));
							attrs2.Add(new AttributeValuePair ("display_title", title_content));
							attrs2.Add(new AttributeValuePair ("display_content", body_content));
							attrs2.Add(new AttributeValuePair ("display_icon", "prj_msg"));
							//attrs.Add(new AttributeValuePair ("day", day_number));
							Node msg_node = new Node (comms_list_node, "msg", "", attrs2);	

							Project_prjNodes.Add(sender,msg_node);
							pr.Dispose();
						}
					}
				}
			}
		}

		private void prj_findata_subnode_AttributesChanged(Node sender, ArrayList attrs)
		{
			foreach(AttributeValuePair avp in attrs)
			{
				if(avp.Attribute == "solvent")
				{
					bool isSolvent = sender.GetBooleanAttribute("solvent",false);
					if (isSolvent)
					{
						//Turning Solvent So we need to remove the Note
						//do we have a monitored fin node
						if (Project_finNodes.ContainsKey(sender))
						{
							//remove it and the 
							Node msg_node = (Node) Project_finNodes[sender];
							msg_node.Parent.DeleteChildTree(msg_node);
							Project_finNodes.Remove(sender);
						}
					}
					else
					{
						//Turning InSolvent So we need to add the Note
						if (Project_finNodes.ContainsKey(sender)==false)
						{
							//Extract the details from the Project 
							Node Project_node = sender.Parent;
							ProjectReader pr = new ProjectReader(Project_node);
							
							string project_id = CONVERT.ToStr(pr.getProjectID());
							string title_content = "Project Manager: Project Problem";
							string body_content = "Project "+project_id+" has insufficent funds to continue.";

							ArrayList attrs2 = new ArrayList ();
							attrs2.Add(new AttributeValuePair ("type", "msg"));
							attrs2.Add(new AttributeValuePair ("subtype", "prj_msg"));
							attrs2.Add(new AttributeValuePair ("display_title", title_content));
							attrs2.Add(new AttributeValuePair ("display_content", body_content));
							attrs2.Add(new AttributeValuePair ("display_icon", "prj_msg"));
							attrs2.Add(new AttributeValuePair ("timeout", "120"));
							//attrs.Add(new AttributeValuePair ("day", day_number));
							Node msg_node = new Node (comms_list_node, "msg", "", attrs2);	

							Project_finNodes.Add(sender,msg_node);
							pr.Dispose();
						}
					}
				}
			}
		}

		private void node_pm_projects_running_ChildAdded(Node sender, Node child)
		{
			project_child_added(child);
		}

		private void project_child_removed(Node child)
		{
			Node project_node = child;
			if (project_node != null)
			{
				runningprojects.Remove(project_node);

				ProjectReader pr = new ProjectReader(project_node);
				Node prj_findata_subnode = pr.getFinSubNode();
				Node prj_prjdata_subnode = pr.getProjectSubNode();
				Node prj_workdata_subnode = pr.getWorkSubNode();

				if (null != prj_findata_subnode)
				{
					if (Project_finNodes.ContainsKey(prj_findata_subnode))
					{
						Node msg_node = (Node)Project_finNodes[prj_findata_subnode];
						msg_node.Parent.DeleteChildTree(msg_node);
						Project_finNodes.Remove(prj_findata_subnode);
					}
					prj_findata_subnode.AttributesChanged -= new Network.Node.AttributesChangedEventHandler(prj_findata_subnode_AttributesChanged);
					prj_prjdata_subnode.AttributesChanged -= new Network.Node.AttributesChangedEventHandler(prj_prjdata_subnode_AttributesChanged);
				}
				pr.Dispose();
			}

		}

		private void node_pm_projects_running_ChildRemoved(Node sender, Node child)
		{
			project_child_removed(child);
		}

		private void BuildEmployeeMonitoring()
		{
			Hashtable ht = new Hashtable();
			ArrayList types = new ArrayList();
			
			//Build an Lookup for the TravelPlan Nodes 
			types = new ArrayList();
			types.Clear();
			types.Add("person");
			ht = MyNodeTree.GetNodesOfAttribTypes(types);
			foreach(Node empNode in ht.Keys)
			{
				string name = empNode.GetAttribute("name");
				employees.Add(name, empNode);
			}		
		}

		private void DisposeEmployeeMonitoring()
		{
			employees.Clear();
		}

		#endregion Monitoring Methods

		/// <summary>
		/// The monitored node might change and we might need to alter the displayed message
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="attrs"></param>
		private void mon_node_AttributesChanged(Node sender, ArrayList attrs)
		{
			Node mon_node = sender;

			if (monitored_nodes.ContainsKey(mon_node))
			{
				//Extract the required data 
				Node msg_node = (Node) monitored_nodes[mon_node];
				if (msg_node != null)
				{
					string type = msg_node.GetAttribute("type");
					string sub_type = msg_node.GetAttribute("subtype");

					switch (sub_type.ToLower())
					{
						case "ops_msg":
							string node_name = mon_node.GetAttribute("name");
							string node_display = mon_node.GetAttribute("display");
							string node_status_str = mon_node.GetAttribute("status");
							string display_title = "Operations Team: "+node_display;
							string display_content = node_status_str;			
							string display_day = mon_node.GetAttribute("day");

							switch (node_status_str.ToLower())
							{
								case "todo":
									display_content = "Planned for day " + display_day;
									break;
								case "inprogress":
									display_content = "Working on during day " + display_day;
									break;
								case "done":
									display_content = "Completed on day " + display_day;
									break;
								default:
									display_content = "status:" + node_status_str + " day:" + display_day;
									break;
							}

							ArrayList delta_attrs = new ArrayList ();
							delta_attrs.Add(new AttributeValuePair ("display_title", display_title));
							delta_attrs.Add(new AttributeValuePair ("display_content", display_content));
							delta_attrs.Add(new AttributeValuePair ("display_icon", "block_msg"));
							msg_node.SetAttributes(delta_attrs);
							break;
					}
				
				}
			}
		}

		/// <summary>
		/// Staff are never destroyed , merely moved from department to project and back 
		/// So we can see what they are doing by who thier parent node is 
		/// </summary>
		protected void getDepartmentStaffReport(out string TitleText, out string BodyText)
		{
			TitleText = "Program Manager: Department Staff Roll Call";
			BodyText = "";

			//the dev categories
			int staff_count_int_dev_unemployed =0;
			int staff_count_ext_dev_unemployed =0;
			int staff_count_int_dev_employed =0;
			int staff_count_ext_dev_employed =0;
			int staff_count_int_dev_DoNothing =0;
			int staff_count_ext_dev_DoNothing =0;

			//the test categories
			int staff_count_int_test_unemployed =0;
			int staff_count_ext_test_unemployed =0;
			int staff_count_int_test_employed =0;
			int staff_count_ext_test_employed =0;
			int staff_count_int_test_DoNothing =0;
			int staff_count_ext_test_DoNothing =0;			


			foreach (Node emp in this.employees.Values)
			{
				string emp_name = emp.GetAttribute("name");
				string emp_skill = emp.GetAttribute("skill");
				bool emp_isExt = emp.GetBooleanAttribute("is_contractor", false);

				Node Parent = emp.Parent;
				if (Parent != null)
				{
					string parent_type = Parent.GetAttribute("type");
					switch (parent_type.ToLower())
					{
						case "bench":
							if (emp_skill=="dev")
							{
								if (emp_isExt==false)
								{
									staff_count_int_dev_unemployed++;
								}
								else
								{
									staff_count_ext_dev_unemployed++;
								}
							}
							else
							{
								if (emp_isExt==false)
								{
									staff_count_int_test_unemployed++;
								}
								else
								{
									staff_count_ext_test_unemployed++;
								}
							}
							break;
						case "doing_nothing":
							if (emp_skill=="dev")
							{
								if (emp_isExt==false)
								{
									staff_count_int_dev_DoNothing++;
								}
								else
								{
									staff_count_ext_dev_DoNothing++;
								}
							}
							else
							{
								if (emp_isExt==false)
								{
									staff_count_int_test_DoNothing++;
								}
								else
								{
									staff_count_ext_test_DoNothing++;
								}
							}
							break;
						case "work_task":
							if (emp_skill=="dev")
							{
								if (emp_isExt==false)
								{
									staff_count_int_dev_employed++;
								}
								else
								{
									staff_count_ext_dev_employed++;
								}
							}
							else
							{
								if (emp_isExt==false)
								{
									staff_count_int_test_employed++;
								}
								else
								{
									staff_count_ext_test_employed++;
								}
							}
							break;
					}
				}
			}

			//Not assigned to Any Project
			int staff_count_int_unemployed = staff_count_int_dev_unemployed + staff_count_int_test_unemployed;
			int staff_count_ext_unemployed = staff_count_ext_dev_unemployed + staff_count_ext_test_unemployed;
			//Assigned to defined Project and working
			int staff_count_int_employed = staff_count_int_dev_employed + staff_count_int_test_employed;
			int staff_count_ext_employed = staff_count_ext_dev_employed + staff_count_ext_test_employed;
			//Assigned to defined Project and Doing Nothing
			int staff_count_int_DoNothing = staff_count_int_dev_DoNothing + staff_count_int_test_DoNothing;
			int staff_count_ext_DoNothing = staff_count_ext_dev_DoNothing + staff_count_ext_test_DoNothing;

			int staff_count_int_assigned = staff_count_int_employed + staff_count_int_DoNothing;
			int staff_count_ext_assigned = staff_count_ext_employed + staff_count_ext_DoNothing; 

			//Attempt 1 test message 
//			BodyText = "Staff (";
//			BodyText += CONVERT.ToStr(staff_count_int_employed)+" Working  ";
//			BodyText += CONVERT.ToStr(staff_count_int_DoNothing)+" Idle";
//			BodyText += ") ";
//			BodyText += "Consultants (";
//			BodyText += CONVERT.ToStr(staff_count_ext_employed)+" Working  ";
//			BodyText += CONVERT.ToStr(staff_count_ext_DoNothing)+" Idle";
//			BodyText += ")";

			if ((staff_count_int_assigned==0)&(staff_count_ext_assigned==0))
			{
				BodyText = "No Staff or Contractors are assigned.";
			}
			else
			{
				//Explain how many are wasted 
				BodyText = "Staff (";
				if (staff_count_int_DoNothing==0)
				{	//None are wasted 
					BodyText += "All " + CONVERT.ToStr(staff_count_int_assigned)+" Working";
				}
				else
				{	//Explain how many are wasted 
					BodyText += CONVERT.ToStr(staff_count_int_DoNothing)+" of ";
					BodyText += CONVERT.ToStr(staff_count_int_assigned)+" Idle";
				}
				BodyText += ") ";
				BodyText += "Contractors (";
				if (staff_count_ext_DoNothing==0)
				{//None are wasted 
					BodyText += "All " + CONVERT.ToStr(staff_count_ext_assigned)+" Working";
				}
				else
				{ //Explain how many are wasted 
					BodyText += CONVERT.ToStr(staff_count_ext_DoNothing)+" of ";
					BodyText += CONVERT.ToStr(staff_count_ext_assigned)+" Idle";
				}
				BodyText += ")";
			}
		}

		/// <summary>
		/// This is
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="attrs"></param>
		private void currentTimeNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			foreach(AttributeValuePair avp in attrs)
			{
				if(avp.Attribute == "seconds")
				{
					/*
					int second_count = int.Parse(avp.Value);
					if (second_count % 60 == 2)
					{
						if (DailyDepartmentStaffReportMsgNode ==null)
						{
							//get the data 
							getDepartmentStaffReport(out TitleText, out BodyText);
							//create a new message node 
							ArrayList attrs1 = new ArrayList ();
							attrs1.Add(new AttributeValuePair ("type", "msg"));
							attrs1.Add(new AttributeValuePair ("subtype", "rollcall"));
							attrs1.Add(new AttributeValuePair ("display_title", TitleText));
							attrs1.Add(new AttributeValuePair ("display_content", BodyText));
							attrs1.Add(new AttributeValuePair ("display_icon", "prg_msg"));
							//attrs.Add(new AttributeValuePair ("day", day_number));
							DailyDepartmentStaffReportMsgNode = new Node (comms_list_node, "msg", "", attrs1);	
						}
						else
						{
							//get the data 
							getDepartmentStaffReport(out TitleText, out BodyText);
							//Update the node
							ArrayList attrs2 = new ArrayList();
							attrs2.Add(new AttributeValuePair ("display_title", TitleText));
							attrs2.Add(new AttributeValuePair ("display_content", BodyText));
							DailyDepartmentStaffReportMsgNode.SetAttributes(attrs2);
						}
					}*/
				}
			}
		}

	}
}
