using System;
using System.Collections;
using System.Xml;

using LibCore;
using Network;

using CoreUtils;

namespace Polestar_PM.OpsEngine
{
	/// <summary>
	/// </summary>
	public class ServiceStatusManager 
	{
		private NodeTree MyNodeTree;
		
		private Hashtable TheServiceNodes = new Hashtable();
		private Hashtable TheServiceChangeLockNodes = new Hashtable();
		private Hashtable TheServiceSwitchNodes = new Hashtable();
		private Hashtable TheServiceStatusNodes = new Hashtable();
		private Hashtable TheServiceTeamNodes = new Hashtable();
		private Hashtable validTravelPlansByEntryCode = new Hashtable();
		private Hashtable TheOfficeNodes = new Hashtable();
		private Hashtable TheOfficeNodesByName = new Hashtable();

		protected Node alert_IT_Node = null;
		protected Node alert_HR_Node = null;
		
		public ServiceStatusManager (NodeTree nt)
		{
			MyNodeTree = nt;

			alert_IT_Node = MyNodeTree.GetNamedNode("all_it_alerts");
			alert_HR_Node = MyNodeTree.GetNamedNode("all_hr_alerts");

			BuildServiceMonitoring();
		}

		/// <summary>
		/// Dispose ....
		/// </summary>
		public void Dispose()
		{
			ClearServiceMonitoring();
			alert_IT_Node = null;
			alert_HR_Node = null; 
			MyNodeTree = null;
		}		
			
		private void BuildServiceMonitoring()
		{
			Hashtable ht;
			ArrayList types;

			//Connect up to all the services	
			types = new ArrayList();
			types.Clear();
			types.Add("biz_service");
			ht = MyNodeTree.GetNodesOfAttribTypes(types);
			foreach(Node serviceNode in ht.Keys)
			{
				//Extract the Name of the Service 
				string service_name = serviceNode.GetAttribute("name");
				string service_letter = service_name.Replace("Service ","");

				//Add onto the List of service nodes
				if (TheServiceNodes.ContainsKey(service_name)==false)
				{
					TheServiceNodes.Add(service_name, serviceNode);
					serviceNode.AttributesChanged +=new Network.Node.AttributesChangedEventHandler(serviceNode_AttributesChanged);
				}
				//Add onto the List of Change Lock nodes (Is Down due to a timed lock)
				Node locknode = MyNodeTree.GetNamedNode(service_name+" ChangeLock");
				if (locknode != null)
				{
					if (TheServiceChangeLockNodes.ContainsKey(service_name)==false)
					{
						TheServiceChangeLockNodes.Add(service_name, locknode);
						locknode.AttributesChanged +=new Network.Node.AttributesChangedEventHandler(locknode_AttributesChanged);
					}
				}
				//Add onto the List of switch nodes (Used as the Output) 
				Node switchnode = MyNodeTree.GetNamedNode(service_name+" Switch");
				if (switchnode != null)
				{
					if (TheServiceSwitchNodes.ContainsKey(service_name)==false)
					{
						TheServiceSwitchNodes.Add(service_name, switchnode);
					}
				}
				//Add onto the List of switch nodes (Used as the Output) 
				Node statusnode = MyNodeTree.GetNamedNode(service_name+" Status");
				if (statusnode != null)
				{
					if (TheServiceStatusNodes.ContainsKey(service_name)==false)
					{
						TheServiceStatusNodes.Add(service_name, statusnode);
					}
				}

				//Build a lookup for the team nodes
				//Any Change will be Signalled by a Service Update
				// we dont need to monitor the team node 
				Node teamnode = MyNodeTree.GetNamedNode("team_"+service_letter);
				if (teamnode != null)
				{
					if (TheServiceTeamNodes.ContainsKey(service_name)==false)
					{
						TheServiceTeamNodes.Add(service_name, teamnode);
					}
				}
			}

			//Build an Lookup for the Office Nodes 
			types.Clear();
			types.Add("office");
			ht = this.MyNodeTree.GetNodesOfAttribTypes(types);
			foreach (Node n in ht.Keys)
			{
				string office_name = n.GetAttribute("name");
				string office_code = n.GetAttribute("office_code");
				if (TheOfficeNodes.ContainsKey(office_code)==false)
				{
					TheOfficeNodes.Add(office_code, n);
					TheOfficeNodesByName.Add(office_name, n);
				}
			}

			//Build an Lookup for the TravelPlan Nodes 
			types = new ArrayList();
			types.Clear();
			types.Add("travelplan");
			ht = MyNodeTree.GetNodesOfAttribTypes(types);
			foreach(Node tpNode in ht.Keys)
			{
				//Extract the Name of the TravelPlan
				string travelplan_name = tpNode.GetAttribute("name");
				string travelplan_code = tpNode.GetAttribute("entrycode");
				//Add onto the List of TravelPlans
				if (validTravelPlansByEntryCode.ContainsKey(travelplan_name)==false)
				{
					validTravelPlansByEntryCode.Add(travelplan_code,tpNode);
				}
			}

			//Validate the Current status 
			foreach (string sername in TheServiceNodes.Keys)
			{
				CheckStatus(sername);
			}
		}		

		private void ClearServiceMonitoring()
		{
			//Get rid of monitoring nodes 
			foreach(Node serviceNode in TheServiceNodes.Values)
			{
				serviceNode.AttributesChanged -=new Network.Node.AttributesChangedEventHandler(serviceNode_AttributesChanged);
			}
			TheServiceNodes.Clear();
			foreach(Node locknode in TheServiceChangeLockNodes.Values)
			{
				locknode.AttributesChanged -=new Network.Node.AttributesChangedEventHandler(locknode_AttributesChanged);
			}
			TheServiceChangeLockNodes.Clear();
			//Get rid of node lists
			TheServiceSwitchNodes.Clear();
			TheServiceStatusNodes.Clear();
			TheServiceTeamNodes.Clear();
			validTravelPlansByEntryCode.Clear();
			TheOfficeNodes.Clear();
			TheOfficeNodesByName.Clear();
		}

		/// <summary>
		/// The lock node makes the service fail if there is a timed event happening 
		/// The lock is engaged by an action (server created, server moved, team moved etc) 
		/// We don't care which item is causing the problem just that it exists
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="attrs"></param>
		private void locknode_AttributesChanged(Node sender, ArrayList attrs)
		{
			bool handle_change = false;

			if (attrs != null)
			{
				if (attrs.Count > 0)
				{
					foreach(AttributeValuePair avp in attrs)
					{
						//used for signalling a timed operation which requires checking
						//Or a UI change of teams and servers 
						if (avp.Attribute == "it_status_flag")
						{
							handle_change = true;
						}
						if (avp.Attribute == "hr_status_flag")
						{
							handle_change = true;
						}

					}
				}
			}
			if ((handle_change))
			{
				string service_status_name = sender.GetAttribute("name");
				string service_name = service_status_name.Replace(" ChangeLock","");
				//System.Diagnostics.Debug.WriteLine("  Service Changed "+service_name+" Check Status");
				CheckStatus(service_name);
			}
		}

		private void serviceNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			bool handle_change = false;

			if (attrs != null)
			{
				if (attrs.Count > 0)
				{
					foreach(AttributeValuePair avp in attrs)
					{
						//Keeping the old method of signalling a check of service status 
						//Perhaps a timer has expired or a ui change 
						//
						if (avp.Attribute == "update")
						{
							handle_change = true;
						}
					}
				}
			}
			if ((handle_change))
			{
				string service_name = sender.GetAttribute("name");
				//System.Diagnostics.Debug.WriteLine("  Service Changed "+service_name+" Check Status");
				CheckStatus(service_name);
			}
		}

		private void SetServiceSwitchStatus(string serviceName, bool status)
		{
			if (TheServiceSwitchNodes.ContainsKey(serviceName))
			{
				Node SwitchNode = (Node)TheServiceSwitchNodes[serviceName];
				if (SwitchNode != null)
				{
					string ssname = SwitchNode.GetAttribute("name");
					//System.Diagnostics.Debug.WriteLine("Service Setting  "+ssname+" "+status.ToString());
					SwitchNode.SetAttribute("up",status.ToString().ToLower());
				}
			}
		}

		private string combineErrMsg(ArrayList errmsgs)
		{
			string errmsg="";
			foreach (string err in errmsgs)
			{
				errmsg+=err+"#";
			}
			return errmsg;
		}



		private void SetServiceStatusStatus(string serviceName, bool it_status, ArrayList it_errmsgs, 
			bool fm_status, ArrayList fm_errmsgs,	bool hr_status, ArrayList hr_errmsgs)
		{
			if (TheServiceStatusNodes.ContainsKey(serviceName))
			{
				Node statusNode = (Node)TheServiceStatusNodes[serviceName];
				if (statusNode != null)
				{
					string ssname = statusNode.GetAttribute("name");

					string it_errmsg_all = combineErrMsg(it_errmsgs);
					string fm_errmsg_all = combineErrMsg(fm_errmsgs);
					string hr_errmsg_all = combineErrMsg(hr_errmsgs);


					ArrayList attrs = new ArrayList();
					attrs.Add(new AttributeValuePair("it_status",it_status.ToString().ToLower()));
					attrs.Add(new AttributeValuePair("it_errmsg",it_errmsg_all));
					attrs.Add(new AttributeValuePair("fm_status",fm_status.ToString().ToLower()));
					attrs.Add(new AttributeValuePair("fm_errmsg",fm_errmsg_all));
					attrs.Add(new AttributeValuePair("hr_status",hr_status.ToString().ToLower()));
					attrs.Add(new AttributeValuePair("hr_errmsg",hr_errmsg_all));
					statusNode.SetAttributes(attrs);

					//System.Diagnostics.Debug.WriteLine("Service Status Update "+serviceName+" "+hr_status.ToString()+":"+fm_status.ToString()+":"+it_status.ToString());
					//System.Diagnostics.Debug.WriteLine("Service Status Update "+serviceName+" "+hr_errmsg_all+":"+fm_errmsg_all+":"+it_errmsg_all);
				}
			}
		}

		private void removeITAlert(string service_name)
		{
			string alert_name = service_name + "_IT";
			string alert_display_text = service_name + " IT Provisioning";

			//Delete the alert node is it exists 
			Node alert_node = this.MyNodeTree.GetNamedNode(alert_name);
			if (alert_node != null)
			{
				alert_node.Parent.DeleteChildTree(alert_node);
			}
		}

		private void SetITAlert(string service_name, string display_msg)
		{
			string alert_name = service_name + "_IT";
			string alert_display_text = service_name + " " +display_msg;

			removeITAlert(service_name);
			//Delete the alert node is it exists 
//			Node alert_node = this.MyNodeTree.GetNamedNode(alert_name);
//			if (alert_node != null)
//			{
//				alert_node.Parent.DeleteChildTree(alert_node);
//			}
			//Create a new node 
			ArrayList attrs = new ArrayList ();
			attrs.Add(new AttributeValuePair ("sequence", "001"));
			attrs.Add(new AttributeValuePair ("source", "taskmanager"));
			attrs.Add(new AttributeValuePair ("show", "true"));
			attrs.Add(new AttributeValuePair ("displaytext", alert_display_text));
			string required_color = "white";

			if (display_msg.ToLower()=="provisioning")
			{
				required_color = "white";
			}
			attrs.Add(new AttributeValuePair ("color", required_color));
			attrs.Add(new AttributeValuePair ("flash_start", "true"));
			new Node (alert_IT_Node, "alert", alert_name, attrs);
			//alert_IT_Node = MyNodeTree.GetNamedNode("all_it_alerts");
			//alert_HR_Node = MyNodeTree.GetNamedNode("all_hr_alerts");		
		}

		private void removeHRAlert(string service_name)
		{
			string alert_name = service_name + "_HR";
			string alert_display_text = service_name + " HR Travelling";

			//Delete the alert node is it exists 
			Node alert_node = this.MyNodeTree.GetNamedNode(alert_name);
			if (alert_node != null)
			{
				alert_node.Parent.DeleteChildTree(alert_node);
			}
		}

		private void SetHRAlert(string service_name, string display_msg)
		{
			string alert_name = service_name + "_HR";
			string alert_display_text = service_name + " " +display_msg;

			removeHRAlert(service_name);
			//Delete the alert node is it exists 
//			Node alert_node = this.MyNodeTree.GetNamedNode(alert_name);
//			if (alert_node != null)
//			{
//				alert_node.Parent.DeleteChildTree(alert_node);
//			}
			//Create a new node 
			ArrayList attrs = new ArrayList ();
			attrs.Add(new AttributeValuePair ("sequence", "001"));
			attrs.Add(new AttributeValuePair ("source", "taskmanager"));
			attrs.Add(new AttributeValuePair ("show", "true"));
			attrs.Add(new AttributeValuePair ("displaytext", alert_display_text));
			string required_color = "white";

			if (display_msg.ToLower()=="travelling")
			{
				required_color = "white";
			}
			attrs.Add(new AttributeValuePair ("color", required_color));
			attrs.Add(new AttributeValuePair ("flash_start", "true"));
			new Node (alert_HR_Node, "alert", alert_name, attrs);
			//alert_IT_Node = MyNodeTree.GetNamedNode("all_it_alerts");
			//alert_HR_Node = MyNodeTree.GetNamedNode("all_hr_alerts");		
		}


		public void CheckStatus(string serviceName)
		{
			ArrayList it_errmsgs = new ArrayList();
			ArrayList hr_errmsgs = new ArrayList();
			ArrayList fm_errmsgs = new ArrayList();

			bool fm_status = true;
			bool hr_status = true;
			bool it_status = false;
			bool isIT_LockEngaged = false;
			bool isHR_LockEngaged = false;
			bool min_HR_Provision = false;
			bool min_IT_ReplaceProvision = false;

			if (TheServiceNodes.ContainsKey(serviceName))
			{
				Node SNode = (Node)TheServiceNodes[serviceName];
				if (SNode != null)
				{
					//Extract out the data elements
					string service_name = SNode.GetAttribute("name");
					string serviceletter = serviceName.Replace("Service ","");

					//System.Diagnostics.Debug.WriteLine("=======================================================================");
					//System.Diagnostics.Debug.WriteLine("Service Checking "+serviceName);
					hr_status = Determine_HR_Status(SNode, service_name, serviceletter, hr_errmsgs, out isHR_LockEngaged, out min_HR_Provision);
					it_status = Determine_IT_Status(SNode, service_name, it_errmsgs, out isIT_LockEngaged, out min_IT_ReplaceProvision);

					if ((hr_status)&(it_status))
					{
						removeHRAlert(service_name);
						removeITAlert(service_name);
					}
					else
					{
						if (min_IT_ReplaceProvision==true)
						{
							if (isIT_LockEngaged)
							{
								SetITAlert(service_name, "provisioning");
							}
							else
							{
								SetITAlert(service_name, "provisioned");
							}
						}
						else
						{
							removeITAlert(service_name);
						}

						if (min_HR_Provision==true)
						{
							if (isHR_LockEngaged)
							{
								SetHRAlert(service_name, " staff travelling");
							}
							else
							{
								SetHRAlert(service_name, "staff on site");
							}
						}
						else
						{
							removeHRAlert(service_name);
						}

					}

//					if (it_status)
//					{
//						removeITAlert(service_name);
//					}
//					else
//					{
//						if (isIT_LockEngaged)
//						{
//							SetITAlert(service_name, "provisioning");
//						}
//					}
//
//					if (hr_status)
//					{
//						removeHRAlert(service_name);
//					}
//					else
//					{
//						if (isHR_LockEngaged)
//						{
//							SetHRAlert(service_name, "travelling");
//						}
//					}


					SetServiceStatusStatus(service_name, it_status, it_errmsgs, fm_status, fm_errmsgs, hr_status, hr_errmsgs);

					if (hr_status & fm_status & it_status)
					{
						SetServiceSwitchStatus(serviceName,true);
					}
					else
					{

						SetServiceSwitchStatus(serviceName,false);
					}
				}
			}
		}

		public void Determine_Permitted_Styles(string serviceletter, string team_option, out ArrayList travelStyles)
		{	
			travelStyles = new ArrayList();
			//
			string def_name = "team_"+serviceletter+"_def";
			Node team_def_node = this.MyNodeTree.GetNamedNode(def_name);
			if (team_def_node != null)
			{
				string local_defs = team_def_node.GetAttribute("teams_local");
				string commute_defs = team_def_node.GetAttribute("teams_commute");
				string overnight_defs = team_def_node.GetAttribute("teams_overnight");

				if (local_defs.LastIndexOf(team_option)>-1)
				{
					travelStyles.Add("local");
				}
				if (commute_defs.LastIndexOf(team_option)>-1)
				{
					travelStyles.Add("commute");
					travelStyles.Add("local");
				}
				if (overnight_defs.LastIndexOf(team_option)>-1)
				{
					travelStyles.Add("overnight");
					travelStyles.Add("commute");
					travelStyles.Add("local");
				}
			}
		}
		
		public bool Determine_HR_Status(Node SNode, string checkservicename, string serviceletter, ArrayList hr_errmsgs,
			out bool lockEngaged, out bool minProvisionInPlace)
		{	
			//HR Status 
			//Each team has a set of children sub nodes which represent sub-teams allocated different travel options
			//Need to check each sub-team for a number of different factors 
			//Also need to check "ChangeLock" as some one could be moving 
			bool service_status = false;
			bool overall_status = false;							//Overall status for the HR system (not counting the lock)
			bool all_People_Assigned = false;					//There are no missing people 
			bool all_TravelPlans_Good = false;				//The office related to them is Accessible (Open or Provisioning)
			bool all_TravelPlans_Consistent = false;	//All the travel plans point to the same office 
			bool all_People_WillingToGo = true;			//All the travel plans are allowed by the team option 
			bool HaveNoLockEngaged = false;
			bool override_status = false;
			minProvisionInPlace = false;

			string debug_str="";
			
			hr_errmsgs.Clear();
			lockEngaged = false;
			//==================================================================
			//all core team members need to be in a valid travel plan=========== 
			//==================================================================
			Node TeamNode = (Node) TheServiceTeamNodes[checkservicename];
			if (TeamNode != null)
			{
				int core_team_assigned_size = 0;
				int core_team_total_size = TeamNode.GetIntAttribute("core_size",0);
				string core_team_option = TeamNode.GetAttribute("team_option");
				string status = TeamNode.GetAttribute("status");

				//we have a override that applies to teams located in RP1,RP2
				if (status.ToLower()!="override")
				{
					//==================================================================
					//Iterate around the sub-teams =====================================
					//==================================================================
					Hashtable subteamByTravelplan = new Hashtable();
					foreach (Node subteam in TeamNode.getChildren())
					{
						string travelPlan = subteam.GetAttribute("travelplan");
						int numberStaff = subteam.GetIntAttribute("number",0);

						//Check that this is valid travel plan 
						//we do store Z which represents the carpark (not a travel plan)
						if (validTravelPlansByEntryCode.ContainsKey(travelPlan))
						{
							core_team_assigned_size += numberStaff;
							//add to hasttable to count up the people at each place 
							//There should only be one entry per plan but we dont assume that 
							if (subteamByTravelplan.ContainsKey(travelPlan))
							{
								int prev_number = (int) subteamByTravelplan[travelPlan];
								subteamByTravelplan[travelPlan] = (prev_number+numberStaff);
							}
							else
							{
								subteamByTravelplan.Add(travelPlan,numberStaff);
							}
						}
					}
					//==================================================================
					//Determine if travelplans are allowed by team option===============  
					//==================================================================
					ArrayList travelStyles = null;
					Determine_Permitted_Styles(serviceletter, core_team_option.ToString(), out travelStyles);
					//==================================================================
					//Now we can check the "All people need to be assigned" requirement  
					//==================================================================
					if (core_team_assigned_size>=core_team_total_size)
					{
						all_People_Assigned = true;
					}
					else
					{
						hr_errmsgs.Add("Not all core team assigned");
					}

					//==================================================================
					//Now we can check the "The office related to the travel must not be closed" requirement  
					//==================================================================
					int number_of_different_travel_options_used = subteamByTravelplan.Count;
					int number_of_good_travel_options_used = 0;
					Hashtable Offices_required = new Hashtable();

					foreach (string travelplan_code in subteamByTravelplan.Keys)
					{
						Node travelplanNode = (Node)validTravelPlansByEntryCode[travelplan_code];
						if (travelplanNode != null)
						{
							//extract the style 
							string style_code = travelplanNode.GetAttribute("style");
							if (travelStyles.Contains(style_code)==false)
							{
								all_People_WillingToGo = false;
								hr_errmsgs.Add("Some Staff not wlling to travel ("+travelplan_code+")");
							}
							//
							string office_code = travelplanNode.GetAttribute("office");
							if (TheOfficeNodesByName.ContainsKey(office_code))
							{
								Node office_node = (Node) TheOfficeNodesByName[office_code];
								if (office_node != null)
								{
									string office_name = office_node.GetAttribute("name");
									string office_status = office_node.GetAttribute("status");
									if ((office_status.ToLower()=="open")|(office_status.ToLower()=="provisioning"))
									{
										number_of_good_travel_options_used++;
									}
									else
									{
										string disp = "Travel option "+travelplan_code+" requires office ";
										disp += office_name+" to be open (or provisioning).";
										hr_errmsgs.Add(disp);
									}
									if (Offices_required.ContainsKey(office_name)==false)
									{
										Offices_required.Add(office_name,"");
									}
								}
							}
						}
					}
					if (number_of_different_travel_options_used==number_of_good_travel_options_used)
					{
						all_TravelPlans_Good= true;
					}
					if (Offices_required.Count==1)
					{
						all_TravelPlans_Consistent = true;
					}


					if ((all_People_Assigned)&(all_TravelPlans_Good )&(all_TravelPlans_Consistent)&(all_People_WillingToGo))
					{
						overall_status = true;
					}

					if (TheServiceChangeLockNodes.ContainsKey(checkservicename))
					{
						Node serviceLockNode = (Node) TheServiceChangeLockNodes[checkservicename];
						if (serviceLockNode != null)
						{
							bool lock_flag = serviceLockNode.GetBooleanAttribute("hr_lock_flag",false);
							if (lock_flag==false)
							{
								HaveNoLockEngaged = true; 
								debug_str = "[LOCK:FALSE] "+debug_str;
							}
							else
							{
								hr_errmsgs.Add("Waiting for a Lock");
								debug_str = "[LOCK:TRUE] "+debug_str;
								lockEngaged = true;
							}
						}
					}
				}
				else
				{
					override_status = true;
				}
			}
			else 
			{
				hr_errmsgs.Add("Team Unknown");
			}

			if ((overall_status)&(override_status==false))
			{
				minProvisionInPlace = true;
			}

			if (((overall_status)&(HaveNoLockEngaged))|(override_status))
			{
				service_status = true;
			}
			debug_str = "["+checkservicename+"]"+service_status.ToString()+" "+debug_str;
			//System.Diagnostics.Debug.WriteLine(debug_str);
			return service_status;
		}

		public bool ExtractOfficeStatus(Node ServerNode)
		{
			bool office_status = false;

			if (ServerNode != null)
			{
				Node parent = ServerNode.Parent;
				if (parent != null)
				{
					string parent_name = parent.GetAttribute("name");
					string parent_type = parent.GetAttribute("type");
					if (parent_type.ToLower()=="rack")
					{
						Node grandparent = parent.Parent;
						if (grandparent != null)
						{
							string grandparent_name = grandparent.GetAttribute("name");
							string grandparent_type = grandparent.GetAttribute("type");
							if (grandparent_type.ToLower()=="data_center")
							{
								Node officenode = grandparent.Parent;
								if (officenode != null)
								{
									string office_name = officenode.GetAttribute("name");
									string office_status_str = officenode.GetAttribute("status");
									if ((office_status_str.ToLower()=="open"))
									{
										office_status = true;
									}
								}
							}
						}
					}	
				}	 
			}
			return office_status;
		}

		private bool isServerInStorage(Node serverNode)
		{
			bool stored = false;
			if (serverNode != null)
			{
				Node parent = serverNode.Parent;
				if (parent != null)
				{
					string rackcode = parent.GetAttribute("rackcode");
					if (rackcode=="0")
					{
						 stored = true;
					}
				}
			}
			return stored;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="ServerNode"></param>
		/// <param name="checkservicename"></param>
		/// <param name="it_errmsg"></param>
		/// <returns></returns>
		public bool Determine_IT_Status(Node ServerNode, string checkservicename, ArrayList it_errmsg, 
			out bool lockEngaged, out bool minReplaceProvisionInPlace)
		{
			bool service_status = false;
			int servers_total = 0;
			int servers_in_good_offices =0;
			int requiredServerCount = 0;
			int replacement_count = 0;
			bool HaveEnoughGoodServers  = false; //Servers which are deployed to data centre whose office is open
			bool HaveNoLockEngaged = false; //Timer Lock from last change operation 
			string debug_str= "";
			minReplaceProvisionInPlace = false;

			lockEngaged = false;
			it_errmsg.Clear();
			//IT Status Checks Section 
			//Is Number of Deployed Server >= Min
			//Are All Data Centres mentioned OPEN 
			//Is ChangeDelay ON then Failure 
			//System.Diagnostics.Debug.WriteLine("DET IT STATUS "+checkservicename);

			//Determine the numbers of required servers
			requiredServerCount = ServerNode.GetIntAttribute("require_it_servers",0);

			//Determine the numbers of active servers
			Hashtable ht;
			ArrayList types = new ArrayList();
			types.Clear();
			types.Add("server");
			ht = MyNodeTree.GetNodesOfAttribTypes(types);
			foreach(Node serverNode in ht.Keys)
			{
				string serverName = serverNode.GetAttribute("name");
				string serviceName = serverNode.GetAttribute("service");
				string built = serverNode.GetAttribute("built");
				bool stored = isServerInStorage(serverNode);

				if (serviceName.ToLower()==checkservicename.ToLower())
				{
					if (stored == false)
					{
						if (built.ToLower()=="replace")
						{
							if (stored==false)
							{
								replacement_count++;
							}
						}
						servers_total++;
						bool office_status = ExtractOfficeStatus(serverNode);
						if (office_status)
						{
							servers_in_good_offices++;
							//System.Diagnostics.Debug.WriteLine(" "+serverName + " in Good Office");
						}
					}
					//else
					//{
					//	System.Diagnostics.Debug.WriteLine(" "+serverName + " in bad Office");
					//}
				}			
			}

			if (servers_total>=requiredServerCount)
			{
				if (replacement_count>0)
				{
					minReplaceProvisionInPlace = true;
				}
			}

			//Check the required angainst the actual
			if (servers_in_good_offices>=requiredServerCount)
			{
				HaveEnoughGoodServers = true;
				//System.Diagnostics.Debug.WriteLine("DET IT STATUS ENOUGH SERVERS");
			}
			else
			{
				if (servers_total>servers_in_good_offices)
				{
					it_errmsg.Add("Not Enough Servers");
				}	
			}
			debug_str = " [RSN:"+requiredServerCount.ToString()+"][GSN"+servers_in_good_offices.ToString()+"][TSN"+servers_total.ToString()+"]";
			
			if (TheServiceChangeLockNodes.ContainsKey(checkservicename))
			{
				Node serviceLockNode = (Node) TheServiceChangeLockNodes[checkservicename];
				if (serviceLockNode != null)
				{
					bool lock_flag = serviceLockNode.GetBooleanAttribute("it_lock_flag",false);
					if (lock_flag==false)
					{
						HaveNoLockEngaged = true; 
						debug_str = "[LOCK:FALSE] "+debug_str;
					}
					else
					{
						lockEngaged = true;
						it_errmsg.Add("Waiting for a Lock");
						debug_str = "[LOCK:TRUE] "+debug_str;
					}
				}
			}

			if ((HaveEnoughGoodServers)&(HaveNoLockEngaged))
			{
				service_status = true;
			}

			debug_str = "["+checkservicename+"]"+service_status.ToString()+" "+debug_str;
			//System.Diagnostics.Debug.WriteLine(debug_str);
			
			return service_status;
		}




//		public bool DetermineTeamStatus(Node SNode, string serviceletter, string team_code, string loc_code, out string errmsg)
//		{
//			bool status = false;
//			errmsg = "";
//			
//			if (CheckDefaultCodes(team_code, out status)==false)
//			{
//				string hr_team_code = serviceletter;
//
//				if (this.HR_TeamNodes.Contains(hr_team_code))
//				{
//					//Extract the Details of the team
//					Node teamNode = (Node)HR_TeamNodes[hr_team_code];
//					
//					//NEW just based on hotel 
//					string team_status = teamNode.GetAttribute("status");
//					if ((team_status.ToLower() == "working")|(team_status.ToLower() == "deployed"))
//					{
//						status = true;
//					}
//					else
//					{
//						errmsg = "HR1:"+team_status+"";
//					}
//				}
//				else
//				{
//					errmsg = "HR2:"+team_code+"";
//				}
//			}
//			else
//			{
//				errmsg = "HR DEF "+team_code+"";
//			}
//			return status;
//		}

//		public bool DetermineLocStatus(Node SNode, string loc_code, out string errmsg)
//		{
//			bool status = false;
//			errmsg = "";
//			
//			if (CheckDefaultCodes(loc_code, out status)==false)
//			{
//				string office_loc = ""+loc_code[0];
//				office_loc = office_loc.ToUpper();
//
//				ArrayList al = this.MyNodeTree.GetNodesWithAttributeValue("office_code",office_loc);
//				if (al.Count>0)
//				{
//					Node officeNode = (Node) al[0];
//					if (officeNode != null)
//					{
//						string loc_status = officeNode.GetAttribute("status");
//						if (loc_status.ToLower() == "open")
//						{
//							status = true;
//						}
//						else
//						{
//							errmsg = "FM1:"+loc_status+"";
//						}
//					}
//					else
//					{
//						errmsg = "FM2:"+office_loc+"";
//					}
//				}
//				else
//				{
//					errmsg = "FM3:"+office_loc+"";
//				}
//			}
//			else
//			{
//				errmsg = "FM:DEF "+loc_code+"";
//			}
//			return status;
//		}
	
//		private string extractShortStatusCode(string it_status)
//		{
//			string shortcode="XX";
//			switch (it_status)
//			{
//				case "building":shortcode="BD";break;
//				case "moving":shortcode="MV";break;
//				case "storing":shortcode="ST";break;
//				case "stored":shortcode="SD";break;
//				case "unpowered":shortcode="NP";break;
//				case "unconnected":shortcode="NC";break;
//				case "no access":shortcode="NA";break;
//				case "connecting":shortcode="CG";break;
//				case "starting":shortcode="SG";break;
//				case "running":shortcode="RU";break;
//				case "deployed":shortcode="DE";break;
//			}
//			return shortcode;
//		}

//		public bool DetermineITStatus_New(Node SNode, out string errmsg)
//		{
//			bool status = false;
//			bool hardware_ok = false;
//			bool deployed_ok = false;
//			string service_name = "";
//			string hw_errmsg = "";
//			string dp_errmsg = "";
//
//			errmsg="";
//			service_name = SNode.GetAttribute("name");
//			//we need to extract what the requirements are from the Service Node 
//			int requirement_number_servers = SNode.GetIntAttribute("require_it_servers",0);
//			int requirement_server_min_disk = SNode.GetIntAttribute("require_it_disk",0);
//			int requirement_server_min_mem = SNode.GetIntAttribute("require_it_mem",0);
//
//			string req_str = "Service name ["+service_name+"] ";
//			req_str += "[NoSvr:"+requirement_number_servers.ToString()+"]";
//			req_str += "[MinDisk:"+requirement_server_min_disk.ToString()+"]";
//			req_str += "[MinMem:"+requirement_server_min_mem.ToString()+"]";
//			System.Diagnostics.Debug.WriteLine(req_str);
//
//			if (ServersByServiceNodes.Contains(service_name))
//			{
//				Hashtable server_nodes = (Hashtable) ServersByServiceNodes[service_name];
//				if (server_nodes != null)
//				{
//					int working_number_Servers = 0;
//					int underspec_number_Servers = 0;
//					int noStatus_number_Servers = 0;
//					int unknown_number_Servers = 0;
//					foreach (Node server_node in server_nodes.Values)
//					{
//						string server_name = server_node.GetAttribute("name");
//						string server_status = server_node.GetAttribute("status");
//						string hw_code = server_node.GetAttribute("hw_code");
//						string debugstr = "  " + server_name;
//						
//						if (server_status.ToLower()=="deployed")
//						{
//							if (IT_ServerDefNodes.Contains(hw_code))
//							{
//								Node server_def_node = (Node) IT_ServerDefNodes[hw_code];
//								int mem_provided = server_def_node.GetIntAttribute("mem_provided",0);
//								int disk_provided = server_def_node.GetIntAttribute("disk_provided",0);
//								bool mem_good = (requirement_server_min_mem<=mem_provided);
//								bool disk_good = (requirement_server_min_disk<=disk_provided);
//
//								if (mem_good & disk_good)
//								{
//									working_number_Servers++;
//									System.Diagnostics.Debug.WriteLine(debugstr + "GOOD");
//								}
//								else
//								{
//									underspec_number_Servers++;
//									System.Diagnostics.Debug.WriteLine(debugstr + " BAD UNDERSPEC");
//								}
//							}
//							else
//							{
//								System.Diagnostics.Debug.WriteLine(debugstr + " NO HW");
//								unknown_number_Servers++;
//							}
//						}
//						else
//						{
//							System.Diagnostics.Debug.WriteLine(debugstr + " NOT DEPLOYED");
//							noStatus_number_Servers++;
//						}
//					}
//					//
//					if (working_number_Servers>=requirement_number_servers)
//					{
//						status = true;
//						System.Diagnostics.Debug.WriteLine(service_name + "GOOD");
//					}
//					else
//					{
//						//error message
//						dp_errmsg=" ";
//						System.Diagnostics.Debug.WriteLine(service_name + "BAD");
//					}				
//				}
//			}
//			System.Diagnostics.Debug.WriteLine(req_str);
//			return status;
//		}

			
//			if (CheckDefaultCodes(it_code, out status)==false)
//			{
//				//Find the box instance and check it is running 
//				string current_Box_name = "Box"+it_code+"_"+service_name;
//				Node currentBox = this.MyNodeTree.GetNamedNode(current_Box_name);
//				if (currentBox != null)
//				{
//					string box_status = currentBox.GetAttribute("status");
//					if ((box_status.ToLower()=="running")|(box_status.ToLower()=="deployed"))
//					{
//						deployed_ok = true;
//						dp_errmsg=" ";
//					}
//					else
//					{
//						dp_errmsg= extractShortStatusCode(box_status.ToLower());
//					}
//				}
//				//Does it satisfy the requirements 
//				int mem_provided = currentBox.GetIntAttribute("mem_provided",0);
//				int disk_provided = currentBox.GetIntAttribute("disk_provided",0);
//				//Extract the requirements of the service
//				int mem_required = SNode.GetIntAttribute("require_it_mem",0);
//				int disk_required = SNode.GetIntAttribute("require_it_disk",0);
//				bool good_disk = (disk_provided>=disk_required);
//				bool good_mem = (mem_provided>=mem_required);
//
//				if ((good_disk)&(good_mem))
//				{
//					hardware_ok = true;
//					hw_errmsg = " ";
//				}
//				else
//				{
//					hw_errmsg+="Mem Pro:"+mem_provided.ToString()+" Req:"+mem_required.ToString()+"";
//					hw_errmsg+="Disk Pro:"+disk_provided.ToString()+" Req:"+disk_required.ToString()+"";
//
//					hw_errmsg = "X";
//					if ((good_disk==false)&(good_mem==false))
//					{
//						hw_errmsg = "B";
//					}
//					else
//					{
//						if (good_disk==false)
//						{
//							hw_errmsg = "D";
//						}
//						else
//						{
//							hw_errmsg = "M";
//						}
//					}
//				}
//				hardware_ok = true;
//
//				//We must have satisfied hardware which is deployed
//				if (hardware_ok & deployed_ok)
//				{
//					status = true;
//				}
//				else
//				{
//					errmsg = "IT:"+hw_errmsg+dp_errmsg+"-"+it_code;
//				}
//			}		
//			else
//			{
//				errmsg = "IT:DEF "+it_code+"";
//			}


//		private void rackNode_ChildAdded(Node sender, Node child)
//		{
//			//Identify which service this created server refers to  
//			string server_name = child.GetAttribute("name");
//			string server_service_name = child.GetAttribute("service");
//			//need to add to correct Monitoring system and then check status 
//
//			if (ServersByServiceNodes.Contains(server_service_name))
//			{
//				Hashtable serverNodes = (Hashtable) ServersByServiceNodes[server_service_name];
//				if (serverNodes.Contains(server_name)==false)
//				{
//					serverNodes.Add(server_name,child);
//					//attach to mointor server status
//					child.AttributesChanged +=new Network.Node.AttributesChangedEventHandler(serverNode_AttributesChanged);
//					System.Diagnostics.Debug.WriteLine(" Server Monitoring ["+server_name+" "+server_service_name+"]");
//				}
//			}
//			CheckStatus(server_service_name);
//		}

//		private void rackNode_ChildRemoved(Node sender, Node child)
//		{
//			//Identify which service this dying server refers to  
//			string server_name = child.GetAttribute("name");
//			string server_service_name = child.GetAttribute("service");
//
//			if (ServersByServiceNodes.Contains(server_service_name))
//			{
//				Hashtable serverNodes = (Hashtable) ServersByServiceNodes[server_service_name];
//				if (serverNodes.Contains(server_name))
//				{
//					serverNodes.Remove(server_name);
//					//attach to mointor server status
//					child.AttributesChanged -=new Network.Node.AttributesChangedEventHandler(serverNode_AttributesChanged);
//					System.Diagnostics.Debug.WriteLine(" Server Monitoring ["+server_name+" "+server_service_name+"]");
//				}
//			}
//			CheckStatus(server_service_name);
//		}

//		private void serverNode_AttributesChanged(Node sender, ArrayList attrs)
//		{
//			bool update = false;
//
//			if (attrs != null && attrs.Count > 0)
//			{
//				foreach(AttributeValuePair avp in attrs)
//				{
//					if (avp.Attribute == "status")
//					{
//						update = true;
//					}
//				}
//			}
//			//only check if the status has changed
//			if (update)
//			{
//				//Identify which service this created server refers to  
//				string server_name = sender.GetAttribute("name");
//				string server_service = sender.GetAttribute("service");
//				CheckStatus(server_service);
//			}
		//		}

	}
}
