using System;
using System.Collections;
using LibCore;
using Network;

namespace TransitionObjects
{
	/// <summary>
	/// Summary description for ProjectManager.
	/// </summary>
	public class ProjectManager
	{
		//Attributes (marking the project)
		const string AttrName_ReadyForDeployment = "ReadyForDeployment";
		//Node Name Definitions  
		const string Node_ProjectIncomingRequest = "ProjectsIncomingRequests";

		const string Node_ProjectsRunning = "Projects";
		//Nodes that we need to Watch for UI events
		Node MyProjectsIncomingRequestsNode;	//Request by UI to Create a Project
		//Nodes that contain Projects
		Node MyProjectsRunningNode;

		//Node that contains the money
		Node DevSpend;

		//My List of the running projects 
		Hashtable MyRunningProjects;			//Local Array for working across all projects

		NodeTree MyNodeTree;
		int CurrentRound = 1;
		Boolean TransitionMode = true;

		#region Constructor and Disposes

		public ProjectManager(NodeTree tree, int Round, bool inTransitionMode)
		{
			CurrentRound = Round;
			MyRunningProjects = new Hashtable();
			MyNodeTree = tree;
			TransitionMode = inTransitionMode;

			//Connect up to Finance Node
			DevSpend = MyNodeTree.GetNamedNode("DevelopmentSpend");

			//Connect up to incoming Requests
			MyProjectsIncomingRequestsNode = MyNodeTree.GetNamedNode(Node_ProjectIncomingRequest);
			MyProjectsIncomingRequestsNode.ChildAdded += MyProjectsIncomingRequestsNode_ChildAdded;
			//MyProjectsIncomingRequestsNode.ChildRemoved += new Network.Node.NodeChildRemovedEventHandler(MyProjectsIncomingRequestsNode_ChildRemoved);

			//Connect up to Current Running Projects
			MyProjectsRunningNode = MyNodeTree.GetNamedNode(Node_ProjectsRunning);

			// Only need to watch for additions during ops phase: in transition phase, it's redundant and leads to problems.
			if (! inTransitionMode)
			{
				MyProjectsRunningNode.ChildAdded += MyProjectsRunningNode_ChildAdded;
			}

			MyProjectsRunningNode.ChildRemoved +=MyProjectsRunningNode_ChildRemoved;

			//Attach to existing projects 
			ArrayList kids_to_kill = new ArrayList();
			ArrayList existing_projects = MyProjectsRunningNode.getChildren();
			Boolean discard_this_kid = true;

			foreach (Node kid in existing_projects)
			{
				AddExistingProject(kid,TransitionMode,out discard_this_kid);
				if (discard_this_kid)
				{
					kids_to_kill.Add(kid);
				}
			}
			if (kids_to_kill.Count>0)
			{
				foreach (Node n1 in kids_to_kill)
				{
					n1.Parent.DeleteChildTree(n1);
				}
			}
		}

		/// <summary>
		/// Setting all thye handles to null, detachs the Event handlers
		/// </summary>
		public void Dispose()
		{
			//detach from Watched Nodes
			MyProjectsIncomingRequestsNode.ChildAdded -= MyProjectsIncomingRequestsNode_ChildAdded;
			MyProjectsRunningNode.ChildAdded -= MyProjectsRunningNode_ChildAdded;
			MyProjectsRunningNode.ChildRemoved -= MyProjectsRunningNode_ChildRemoved;
		}

		#endregion Constructor and Disposes

		#region Misc

		public ArrayList getInstallableProjects()
		{
			ArrayList al = new ArrayList();

			foreach (ProjectRunner pr in MyRunningProjects.Values)
			{
				if ((pr.isState_Ready())|(pr.isState_InstallFail()))
				{
					al.Add(pr);
				}
			}
			return al;
		}

		public ArrayList getPendingProjects()
		{
			ArrayList al = new ArrayList();

			foreach (ProjectRunner pr in MyRunningProjects.Values)
			{
				if (pr.getWhentoInstall() != -1)
				{
					al.Add(pr);
				}
			}
			return al;
		}

		void OutputError(string errorText)
		{
			Node errorsNode = MyNodeTree.GetNamedNode("FacilitatorNotifiedErrors");
			Node error = new Node(errorsNode, "error", "", new AttributeValuePair( "text", errorText ) );
		}

		#endregion Misc

		#region Calender and Time Methods

		int GetCurrentDay()
		{
			Node todayNode = MyNodeTree.GetNamedNode("CurrentDay");
			int today = todayNode.GetIntAttribute("day",-1);

			if(-1 == today) 
				throw( new Exception("No CurrentDay model node found.") );

			return today;
		}

		/// <summary>
		/// Is "day" free in calendar
		/// </summary>
		/// <param name="day">The Day in Question</param>
		/// <param name="RequestingProduct">Who is making the request</param>
		/// <param name="SelfBlocked">Are we blocking ourself</param>
		/// <returns></returns>
		Boolean IsDayFreeInCalendar(int day, string RequestingProduct, out Boolean SelfBlocked)
		{
			Boolean DayIsFree = true;
			SelfBlocked = false;
			Node CalendarNode = MyNodeTree.GetNamedNode("Calendar");
			//Need to iterate over children 
			foreach(Node calendarEvent in CalendarNode.getChildren())
			{
				int cday = calendarEvent.GetIntAttribute("day",0);
				string block = calendarEvent.GetAttribute("block");
				string product_Booked = calendarEvent.GetAttribute("productid");

				if (day == cday)
				{
					if (block.ToLower() == "true")
					{
						DayIsFree = false;
						if (product_Booked.ToLower() == RequestingProduct)
						{
							SelfBlocked = true;
						}
					}
				}
			}
			return DayIsFree;
		}

		Boolean AddCalendarEvent(int day, string prj_name, string prd_name, string location)
		{
			Node MyCalendarNode = MyNodeTree.GetNamedNode("Calendar");
			Boolean OpSuccess = false;

			ArrayList al = new ArrayList();
			al.Add( new AttributeValuePair("day", day) );
			al.Add( new AttributeValuePair("showName", prj_name) );
			al.Add( new AttributeValuePair("projectid", prj_name) );
			al.Add( new AttributeValuePair("productid", prd_name) );
			al.Add( new AttributeValuePair("type", "Install") ); // Shouldn't we actually get the type from the model?
			al.Add( new AttributeValuePair("status", "active") );
			al.Add( new AttributeValuePair("block", "true") );
			al.Add( new AttributeValuePair("location", location) );
			
			// Add the Node...
			Node incident = new Node(MyCalendarNode, "CalendarEvent","", al);
			OpSuccess = true;

			return OpSuccess;
		}

//		private Boolean RemoveCalendarEvent(int day, string prj_name, string prd_name)
//		{
//			//we only remove the calendaer record if it was still pending
//			Node Node_Found = null;
//			string removeBookingDay = CONVERT.ToStr(day);
//
//			foreach(Node n2 in calendarNode.getChildren())
//			{
//				string CalenderProductID = n2.GetAttribute("productid");
//				string CalenderDay = n2.GetAttribute("day");
//				if (CalenderDay.ToLower() == removeBookingDay.ToLower())
//				{
//					if (CalenderProductID.ToLower() == prd_name.ToLower())
//					{
//						Node_Found = n2;
//					}
//				}
//			}
//			if (Node_Found != null)
//			{
//				Node_Found.Parent.DeleteChildTree(Node_Found);
//			}
//		}

		#endregion Calender and Time Methods

		#region Budget Methods 


		/// <summary>
		/// This get the Remaining Budget from the network tree
		/// </summary>
		/// <returns></returns>
		int GetRemainingBudget()
		{
			int RemainingBudget = DevSpend.GetIntAttribute("RoundBudgetLeft",0);
			return RemainingBudget;
		}

		void ReduceRemainingBudget(int AdditionalCost)
		{
			int RemainingBudget = DevSpend.GetIntAttribute("RoundBudgetLeft",0);
			RemainingBudget = RemainingBudget - AdditionalCost;
			DevSpend.SetAttribute("RoundBudgetLeft", RemainingBudget);
		}

		void IncreaseRemainingBudget(int MoneyBack)
		{
			int RemainingBudget = DevSpend.GetIntAttribute("RoundBudgetLeft",0);
			RemainingBudget = RemainingBudget + MoneyBack;
			DevSpend.SetAttribute("RoundBudgetLeft", RemainingBudget);
		}

		#endregion Budget Methods

		#region Handling Project Requests Methods

		/// <summary>
		/// Handling a Project Created Node
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="child"></param>
		void  MyProjectsIncomingRequestsNode_ChildAdded(Node sender, Node child)
		{
			string strInstallAction = string.Empty;
			Boolean OpSuccess = false;

			if (child != null)
			{
				string type = child.Type;//.GetAttribute("type");

				switch (type.ToLower())
				{
					case "create":
						OpSuccess = HandleCreateProject(child);
						break;
					case "cancel":
						OpSuccess = HandleCancelProject(child);
						break;
					case "cancelbooking":
						OpSuccess = HandleCancelBookingForProject(child);
						break;
					case "install":
						string inner_type = child.GetAttribute("type");
						switch (inner_type.ToLower())
						{ 
							case "install_apphw":
								OpSuccess = HandleInsertProjectWithHW(child);
								break;
							case "install_apponly":
								OpSuccess = HandleInsertProjectWithAppOnly(child);
								break;
							default:
								OpSuccess = HandleInsertProject(child);
								break;
						}
						break;
					default:
						// Unknown Event has occured...
						Node errorsNode = MyNodeTree.GetNamedNode("FacilitatorNotifiedErrors");
						string errorText = "Unknown Event Sent To The ProjectManager " + type;
						Node error = new Node(errorsNode, "error", "", new AttributeValuePair( "text", errorText ) );
						break;
				}
				//removing the request
				child.Parent.DeleteChildTree(child);
			}
		}

		bool HandleCancelBookingForProject(string projectToReBook, int WhenWasBooking)
		{
			// We need to remove any relevant calendar event if it exists.
			if (MyRunningProjects.ContainsKey(projectToReBook))
			{
				ProjectRunner pr = (ProjectRunner) MyRunningProjects[projectToReBook];
				pr.ClearBookingState();

				////Handle the tidy up for IBM Cloud 
				//Node pn = pr.getProjectNode();
				//if (pn != null)
				//{
				//  string install_data_node_name = pn.GetAttribute("install_data_node","");
				//  if (install_data_node_name != "")
				//  {
				//    Node install_data_node = this.MyNodeTree.GetNamedNode(install_data_node_name);
				//    if (install_data_node != null)
				//    {
				//      install_data_node.SetAttribute("status", "open");
				//    }
				//  }
				//}
			}
		
			// We also have to remove any relevant calendar event if it exists.
			ArrayList delNodes = new ArrayList();
			Node calendarNode = MyNodeTree.GetNamedNode("Calendar");
			int today = GetCurrentDay();
			// Run over the calendar events and see if we can find any that relate to this sip project...
			foreach(Node child in calendarNode.getChildren())
			{
				// Only wipe calendar events in the future.
				if(child.GetIntAttribute("day",0) > today)
				{
					//if this is the day of the booking 
					if (WhenWasBooking == child.GetIntAttribute("day",1))
					{
						// Only wipe calendar events in the future.
						if(projectToReBook == child.GetAttribute("projectid"))
						{
							//we only delete active ones
							if("active"==child.GetAttribute("status").ToLower())
							{
								delNodes.Add(child);
							}
						}
					}
				}
			}
			//
			foreach(Node dnode in delNodes)
			{
				calendarNode.DeleteChildTree(dnode);
			}
			return true;
		}

		bool HandleCancelBookingForProject(Node node)
		{
			// Find the project node and delete it from the model.
			string projectToReBook = node.GetAttribute("projectid");
			int WhenWasBooking = node.GetIntAttribute("when",1);
			return HandleCancelBookingForProject(projectToReBook, WhenWasBooking);
		}

		bool HandleCancelProject(Node node)
		{
			Boolean OpSuccess = false;
			// Find the project node and delete it from the model.
			string projectToCancel = node.GetAttribute("projectid");
			Node nodeToRemove = MyNodeTree.GetNamedNode(projectToCancel);
			if(null == nodeToRemove)
			{
				// This will occur if the Facilitator types in a remove project SIP number
				// that does not exist.
				Node errorsNode = MyNodeTree.GetNamedNode("FacilitatorNotifiedErrors");
				string errorText = "Cannot cancel project From SIP " + projectToCancel + " as it has not been created.";
				Node error = new Node(errorsNode, "error", "", new AttributeValuePair( "text", errorText ) );
			}
			else
			{
				//Need to handle the Cost refund
				int Actual_Cost = nodeToRemove.GetIntAttribute("actual_cost",0);
				int CurrentSpend = nodeToRemove.GetIntAttribute("currentspend",0);
				int MoneyBack = Actual_Cost - CurrentSpend;
				
				if (MoneyBack>0)
				{
					this.IncreaseRemainingBudget(MoneyBack);
				}
				
				// We also have to remove any relevant calendar event if it exists.
				ArrayList delNodes = new ArrayList();
				Node calendarNode = MyNodeTree.GetNamedNode("Calendar");
				int today = GetCurrentDay();
				// Run over the calendar events and see if we can find any that relate to this sip project...
				foreach(Node child in calendarNode.getChildren())
				{
					if(child.GetIntAttribute("day",0) > today)
					{
						// Only wipe calendar events in the future.
						if(projectToCancel == child.GetAttribute("projectid"))
						{
							delNodes.Add(child);
						}
					}
				}
				//
				foreach(Node dnode in delNodes)
				{
					calendarNode.DeleteChildTree(dnode);
				}

				//kill of the runner and delete the project node 
				if (MyRunningProjects.ContainsKey(projectToCancel))
				{
					ProjectRunner pr = (ProjectRunner) MyRunningProjects[projectToCancel];
					pr.Dispose();
					this.MyRunningProjects.Remove(projectToCancel);
				}
				nodeToRemove.Parent.DeleteChildTree(nodeToRemove);
				OpSuccess = true;
			}
			return OpSuccess;
		}

		Boolean HandleInsertProject(Node InsertRequestNode)
		{
			Boolean OpSuccess = false;
			string node_projectIDStr;
			string node_productIDStr;
			string node_platformIDStr; 
			string node_stage; 
			int node_projectID = 0;
			int node_productID = 0; 
			int request_installDay = 0;
			int request_installMin = 0;
			Boolean SIPNotFound = true;
			Boolean ProductNotFound = true;
			Boolean	PlatformNotFound = true;
			Boolean SelfBlocked = false;

			//Extract the Request Information 
			string phase = InsertRequestNode.GetAttribute("phase");
			string request_prdIDStr = InsertRequestNode.GetAttribute("productid");
			string request_prjIDStr = InsertRequestNode.GetAttribute("projectid");
			string request_location = InsertRequestNode.GetAttribute("location");
			int request_sla = InsertRequestNode.GetIntAttribute("sla",1);
			int request_when = InsertRequestNode.GetIntAttribute("installwhen",-1);

			// Get the required project node from the model...
			Node n1 = MyNodeTree.GetNamedNode(request_prjIDStr);
			if(null == n1)
			{
				// This will happen if the Facilitator types in a project SIP to install 
				// that doesn't exist (is not running).
				OutputError("Cannot install product " + request_prjIDStr + " as it has not been created.");
				return false;
			}

			//TODO Check that the location is Good (actually on the Board for a start)
			Boolean LocationGood = true;
			if(LocationGood==false)
			{
				//This will happen if the Facilitator enters a bad location 
				OutputError(" No Location [" + request_location + "] Found ");
				return false;
			}

			//Extract data from the Project Node
			node_projectIDStr = n1.GetAttribute("projectid");
			node_productIDStr = n1.GetAttribute("productid");
			node_platformIDStr = n1.GetAttribute("platformid");
			node_stage = n1.GetAttribute("stage");
			node_projectID = CONVERT.ParseInt(node_projectIDStr);
			node_productID = CONVERT.ParseInt(node_productIDStr);

			if (phase.ToLower() == "transition")
			{	
				//=====================================================================
				//==handling an install for the Transition Phase (days)================
				//=====================================================================
				request_installDay = request_when;
				//Check that the day is OK, we aren't booking a passed day 
				int today = GetCurrentDay();
				if(request_installDay <= today)
				{
					OutputError("Cannot install product " + request_prjIDStr + " on Day " + CONVERT.ToStr(request_installDay) +
						" as Today is Day " + CONVERT.ToStr(today));
					return false;
				}
				//Extract out the Project Runner Object
				ProjectRunner runner = (ProjectRunner) MyRunningProjects[request_prjIDStr];
				
				//Check that Has the project already been installed OK
				if (runner.isState_InstallOK())
				{
					OutputError("Cannot install product " + request_prjIDStr + " on Day " + CONVERT.ToStr(request_installDay) +
						" as it's already installed.");
					return false;
				}

				//Have we already got a Booked Day
				int BookedDay = runner.getWhentoInstall();
				if (BookedDay != -1)
				{
					//We have booked an a install day already 
					if (BookedDay == request_when)
					{
						//We are overriding an existing booked day 
						ArrayList al = new ArrayList();
						string InstallActionXML = string.Empty;
						ProjectSIP_Repository.TheInstance.getSIP_Data(node_projectIDStr, node_productIDStr, node_platformIDStr, 
							out al, out InstallActionXML, out SIPNotFound, out ProductNotFound, out PlatformNotFound); 
						//
						if (InstallActionXML != null)
						{
							//We have a Booked Day so we can delete the old booking (if possible)
							HandleCancelBookingForProject(node_projectIDStr, BookedDay);
							//need to create a new Booking for the new Booking Day
							AddCalendarEvent(request_installDay, node_projectIDStr, node_productIDStr, request_location);
							//Update the Project when the location chosen
							runner.ApplyActionOnTime(InstallActionXML, request_installDay, request_location, request_sla);
							//Update the Project when the location chosen
							runner.UpdateNodeData(request_location, request_sla);
							OpSuccess = true;
						}
						else
						{
							OutputError("Cannot install product with no install actions");
							return false;
						}
					}
					else
					{
						//We are trying to shift the day away from our existing booked day
						//Need to checked that the requested Day is free
						if (this.IsDayFreeInCalendar(request_installDay, node_productIDStr, out SelfBlocked))
						{
							//Yes it's free
							ArrayList al = new ArrayList();
							string InstallActionXML = string.Empty;
							ProjectSIP_Repository.TheInstance.getSIP_Data(node_projectIDStr, node_productIDStr, node_platformIDStr, 
								out al, out InstallActionXML, out SIPNotFound, out ProductNotFound, out PlatformNotFound); 
							//
							if (InstallActionXML != null)
							{
								//We have a Booked Day so we can delete the old booking (if possible)
								HandleCancelBookingForProject(node_projectIDStr, BookedDay);
								//Update the Project when the location chosen
								runner.ApplyActionOnTime(InstallActionXML, request_installDay, request_location, request_sla);
								//need to create a new Booking for the new Booking Day
								AddCalendarEvent(request_installDay, node_projectIDStr, node_productIDStr,request_location);
								//Update the Project when the location chosen
								runner.UpdateNodeData(request_location, request_sla);
								OpSuccess = true;
							}
							else
							{
								OutputError("Cannot install product with no install actions");
								return false;
							}
						}
						else
						{
							//No, Day id already Booked
							OutputError("Cannot install product " + request_prjIDStr + " on Day " + 
								CONVERT.ToStr(request_installDay) +	" as day is booked");
							return false;
						}
					}
				}
				else
				{
					//there is no booked day for this Project 
					if (this.IsDayFreeInCalendar(request_installDay, node_productIDStr, out SelfBlocked))
					{
						ArrayList al = new ArrayList();
						string InstallActionXML = string.Empty;
						ProjectSIP_Repository.TheInstance.getSIP_Data(node_projectIDStr, node_productIDStr, node_platformIDStr, 
							out al, out InstallActionXML, out SIPNotFound, out ProductNotFound, out PlatformNotFound); 
						//
						if (InstallActionXML != null)
						{
							//need to create a new Booking for the new Booking Day
							AddCalendarEvent(request_installDay, node_projectIDStr, node_productIDStr,request_location);
							//Update the Project when the location chosen
							runner.ApplyActionOnTime(InstallActionXML, request_installDay, request_location, request_sla);
							//Update the Project when the location chosen
							runner.UpdateNodeData(request_location, request_sla);
							OpSuccess = true;
						}
						else
						{
							OutputError("Cannot install product with no install actions");
							return false;
						}
					}
					else
					{
						//No, Day id already Booked
						OutputError("Cannot install product " + request_prjIDStr + " on Day " + 
							CONVERT.ToStr(request_installDay) +	" as day is booked");
						return false;
					}
				}
			}
			else
			{
				//=====================================================================
				//handling an install for the Operations Phase (mins)
				//=====================================================================
				//Update the Project when the location chosen
				n1.SetAttribute("location",request_location);
				n1.SetAttribute("slalimit",CONVERT.ToStr(request_sla*60));
				request_installMin = request_when;
			
				//Extract the install action 
				ArrayList al = new ArrayList();
				string InstallActionXML = string.Empty;
				ProjectSIP_Repository.TheInstance.getSIP_Data(node_projectIDStr, node_productIDStr, node_platformIDStr, 
					out al, out InstallActionXML, out SIPNotFound, out ProductNotFound, out PlatformNotFound); 
				//
				if (InstallActionXML != null)
				{
					//Create a Project runner to handle the work 	
					ProjectRunner runner = (ProjectRunner) MyRunningProjects[request_prjIDStr];
					runner.ApplyActionOnTime(InstallActionXML, request_installMin, request_location, request_sla);
					//Add a installday tag into the project tag
					//InsertRequestNode.SetAttribute("installMin", CONVERT.ToStr(request_installDay));
					OpSuccess = true;
				}
				else
				{
					OutputError("Cannot install project with no install actions");
					return false;
				}
			}
			return OpSuccess;
		}

		/// <summary>
		/// This is a modified routine for handling an install that adds an app only
		/// The app location is automatically determined from it's priority 
		/// This is used By IBM Cloud, where we install a app dynamically based on it's priority within the cloud
		/// </summary>
		/// <param name="InsertRequestNode"></param>
		/// <returns></returns>
		Boolean HandleInsertProjectWithAppOnly(Node InsertRequestNode)
		{
			Boolean OpSuccess = false;
			string node_projectIDStr;
			string node_productIDStr;
			string node_platformIDStr;
			string node_stage;
			int node_projectID = 0;
			int node_productID = 0;
			int request_installDay = 0;
			int request_installMin = 0;
			Boolean SIPNotFound = true;
			Boolean ProductNotFound = true;
			Boolean PlatformNotFound = true;
			Boolean SelfBlocked = false;

			//Extract the Request Information 
			string phase = InsertRequestNode.GetAttribute("phase");
			string request_prdIDStr = InsertRequestNode.GetAttribute("productid");
			string request_prjIDStr = InsertRequestNode.GetAttribute("projectid");
			string request_location = InsertRequestNode.GetAttribute("location");
			int request_sla = InsertRequestNode.GetIntAttribute("sla", 1);
			int request_when = InsertRequestNode.GetIntAttribute("installwhen", -1);
			string install_data_name = InsertRequestNode.GetAttribute("install_data_node", "");
			string install_data_servernode = InsertRequestNode.GetAttribute("install_data_server", "");
			int required_priority_level = InsertRequestNode.GetIntAttribute("required_priority_level", 1);
			//check that the virtual system has been deployed 

			// Get the required project node from the model...
			Node node_virtual_deploy = MyNodeTree.GetNamedNode("virtual_deployment");
			if (null != node_virtual_deploy)
			{
				bool virtual_installed = node_virtual_deploy.GetBooleanAttribute("deployed", false);
				if (virtual_installed == false)
				{
					OutputError("Cannot install product " + request_prjIDStr + " as no Virtual Zone exists");
					return false;
				}
			}

			//Get the Server node that we are installing to 
			Node server_node = this.MyNodeTree.GetNamedNode(install_data_servernode);
			//create the empty slot that will be used in the install process 
			ArrayList attrs = new ArrayList();
			attrs.Add(new AttributeValuePair("type", "Slot"));
			attrs.Add(new AttributeValuePair("proczone", "1"));
			attrs.Add(new AttributeValuePair("dangerlevel", "0"));
			attrs.Add(new AttributeValuePair("name", request_location.ToUpper()));
			attrs.Add(new AttributeValuePair("location", request_location.ToUpper()));
			attrs.Add(new AttributeValuePair("desc", ""));
			attrs.Add(new AttributeValuePair("virtual_priority_level", CONVERT.ToStr(required_priority_level)));
			attrs.Add(new AttributeValuePair("cores_required", "1"));

			new Node(server_node, "node", "", attrs);

			// Get the required project node from the model...
			Node n1 = MyNodeTree.GetNamedNode(request_prjIDStr);
			if (null == n1)
			{
				// This will happen if the Facilitator types in a project SIP to install 
				// that doesn't exist (is not running).
				OutputError("Cannot install product " + request_prjIDStr + " as it has not been created.");
				return false;
			}

			//n1.SetAttribute("install_data_node", install_data_name);

			//TODO Check that the location is Good (actually on the Board for a start)
			Boolean LocationGood = true;
			if (LocationGood == false)
			{
				//This will happen if the Facilitator enters a bad location 
				OutputError(" No Location [" + request_location + "] Found ");
				return false;
			}

			//Extract data from the Project Node
			node_projectIDStr = n1.GetAttribute("projectid");
			node_productIDStr = n1.GetAttribute("productid");
			node_platformIDStr = n1.GetAttribute("platformid");
			node_stage = n1.GetAttribute("stage");
			node_projectID = CONVERT.ParseInt(node_projectIDStr);
			node_productID = CONVERT.ParseInt(node_productIDStr);

			if (phase.ToLower() == "transition")
			{
				//=====================================================================
				//==handling an install for the Transition Phase (days)================
				//=====================================================================
				request_installDay = request_when;
				//Check that the day is OK, we aren't booking a passed day 
				int today = GetCurrentDay();
				if (request_installDay <= today)
				{
					OutputError("Cannot install product " + request_prjIDStr + " on Day " + CONVERT.ToStr(request_installDay) +
						" as today is Day " + CONVERT.ToStr(today));
					return false;
				}
				//Extract out the Project Runner Object
				ProjectRunner runner = (ProjectRunner)MyRunningProjects[request_prjIDStr];

				//Check that Has the project already been installed OK
				if (runner.isState_InstallOK())
				{
					OutputError("Cannot install product " + request_prjIDStr + " on Day " + CONVERT.ToStr(request_installDay) +
						" as it's already installed.");
					return false;
				}

				//Have we already got a Booked Day
				int BookedDay = runner.getWhentoInstall();
				if (BookedDay != -1)
				{
					//We have booked an a install day already 
					if (BookedDay == request_when)
					{
						//We are overriding an existing booked day 
						ArrayList al = new ArrayList();
						string InstallActionXML = string.Empty;
						ProjectSIP_Repository.TheInstance.getSIP_Data(node_projectIDStr, node_productIDStr, node_platformIDStr,
							out al, out InstallActionXML, out SIPNotFound, out ProductNotFound, out PlatformNotFound);
						//
						if (InstallActionXML != null)
						{
							//We have a Booked Day so we can delete the old booking (if possible)
							HandleCancelBookingForProject(node_projectIDStr, BookedDay);
							//need to create a new Booking for the new Booking Day
							AddCalendarEvent(request_installDay, node_projectIDStr, node_productIDStr, request_location);
							//Update the Project when the location chosen
							runner.ApplyActionOnTimeWithInstallData(InstallActionXML, request_installDay, request_location, request_sla, install_data_name);
							//Update the Project when the location chosen
							runner.UpdateNodeData(request_location, request_sla);
							OpSuccess = true;
						}
						else
						{
							OutputError("Cannot install product with no install actions");
							return false;
						}
					}
					else
					{
						//We are trying to shift the day away from our existing booked day
						//Need to checked that the requested Day is free
						if (this.IsDayFreeInCalendar(request_installDay, node_productIDStr, out SelfBlocked))
						{
							//Yes it's free
							ArrayList al = new ArrayList();
							string InstallActionXML = string.Empty;
							ProjectSIP_Repository.TheInstance.getSIP_Data(node_projectIDStr, node_productIDStr, node_platformIDStr,
								out al, out InstallActionXML, out SIPNotFound, out ProductNotFound, out PlatformNotFound);
							//
							if (InstallActionXML != null)
							{
								//We have a Booked Day so we can delete the old booking (if possible)
								HandleCancelBookingForProject(node_projectIDStr, BookedDay);
								//Update the Project when the location chosen
								runner.ApplyActionOnTimeWithInstallData(InstallActionXML, request_installDay, request_location, request_sla, install_data_name);
								//need to create a new Booking for the new Booking Day
								AddCalendarEvent(request_installDay, node_projectIDStr, node_productIDStr, request_location);
								//Update the Project when the location chosen
								runner.UpdateNodeData(request_location, request_sla);
								OpSuccess = true;
							}
							else
							{
								OutputError("Cannot install product with no install actions");
								return false;
							}
						}
						else
						{
							//No, Day id already Booked
							OutputError("Cannot install product " + request_prjIDStr + " on Day " +
								CONVERT.ToStr(request_installDay) + " as day is booked");
							return false;
						}
					}
				}
				else
				{
					//there is no booked day for this Project 
					if (this.IsDayFreeInCalendar(request_installDay, node_productIDStr, out SelfBlocked))
					{
						ArrayList al = new ArrayList();
						string InstallActionXML = string.Empty;
						ProjectSIP_Repository.TheInstance.getSIP_Data(node_projectIDStr, node_productIDStr, node_platformIDStr,
							out al, out InstallActionXML, out SIPNotFound, out ProductNotFound, out PlatformNotFound);
						//
						if (InstallActionXML != null)
						{
							//need to create a new Booking for the new Booking Day
							AddCalendarEvent(request_installDay, node_projectIDStr, node_productIDStr, request_location);
							//Update the Project when the location chosen
							runner.ApplyActionOnTimeWithInstallData(InstallActionXML, request_installDay, request_location, request_sla, install_data_name);
							//Update the Project when the location chosen
							runner.UpdateNodeData(request_location, request_sla);
							OpSuccess = true;
						}
						else
						{
							OutputError("Cannot install product with no install actions");
							return false;
						}
					}
					else
					{
						//No, Day id already Booked
						OutputError("Cannot install product " + request_prjIDStr + " on Day " +
							CONVERT.ToStr(request_installDay) + " as day is booked");
						return false;
					}
				}
			}
			else
			{
				//=====================================================================
				//handling an install for the Operations Phase (mins)
				//=====================================================================
				//Update the Project when the location chosen
				n1.SetAttribute("location", request_location);
				n1.SetAttribute("slalimit", CONVERT.ToStr(request_sla * 60));
				request_installMin = request_when;

				//Extract the install action 
				ArrayList al = new ArrayList();
				string InstallActionXML = string.Empty;
				ProjectSIP_Repository.TheInstance.getSIP_Data(node_projectIDStr, node_productIDStr, node_platformIDStr,
					out al, out InstallActionXML, out SIPNotFound, out ProductNotFound, out PlatformNotFound);
				//
				if (InstallActionXML != null)
				{
					//Create a Project runner to handle the work 	
					ProjectRunner runner = (ProjectRunner)MyRunningProjects[request_prjIDStr];
					runner.ApplyActionOnTimeWithInstallData(InstallActionXML, request_installMin, request_location, request_sla, install_data_name);
					//Add a installday tag into the project tag
					//InsertRequestNode.SetAttribute("installMin", CONVERT.ToStr(request_installDay));
					OpSuccess = true;
				}
				else
				{
					OutputError("Cannot install project with no install actions");
					return false;
				}
			}
			return OpSuccess;
		}

		/// <summary>
		/// This is a modified routine for handling an install that requires new hardware support 
		/// This is used By IBM Cloud, where we install a server along with the app in the early rounds 
		/// </summary>
		/// <param name="InsertRequestNode"></param>
		/// <returns></returns>
		Boolean HandleInsertProjectWithHW(Node InsertRequestNode)
		{
			Boolean OpSuccess = false;
			string node_projectIDStr;
			string node_productIDStr;
			string node_platformIDStr; 
			string node_stage; 
			int node_projectID = 0;
			int node_productID = 0; 
			int request_installDay = 0;
			int request_installMin = 0;
			Boolean SIPNotFound = true;
			Boolean ProductNotFound = true;
			Boolean	PlatformNotFound = true;
			Boolean SelfBlocked = false;

			//Extract the Request Information 
			string phase = InsertRequestNode.GetAttribute("phase");
			string request_prjIDStr = InsertRequestNode.GetAttribute("projectid");
			string request_prdIDStr = InsertRequestNode.GetAttribute("productid");
			string request_location = InsertRequestNode.GetAttribute("location");
			int request_sla = InsertRequestNode.GetIntAttribute("sla",1);
			int request_when = InsertRequestNode.GetIntAttribute("installwhen",-1);
			string install_data_name = InsertRequestNode.GetAttribute("install_data_node","");

			if (install_data_name != "")
			{
				Node install_data_node = this.MyNodeTree.GetNamedNode(install_data_name);
				request_location = install_data_node.GetAttribute("app_location");

				//
				string preferred_sip = install_data_node.GetAttribute("prefered_sip");
				if ((preferred_sip.ToLower()) != (request_prjIDStr.ToLower()))
				{
					bool swap_completed = install_data_node.Parent.GetBooleanAttribute("swap_completed", false);
					if (swap_completed==false)
					{
						//we need to swap the names of the servers as the players have chosen the wrong locations 
						string swap_name_data = install_data_node.GetAttribute("swap_data"); 
						string[] parts = swap_name_data.Split(',');
						string swap_name1 = parts[0];
						string swap_name2 = parts[1];
							
						Node tmpNode1 = this.MyNodeTree.GetNamedNode(swap_name1);
						Node tmpNode2 = this.MyNodeTree.GetNamedNode(swap_name2);

						tmpNode1.SetAttribute("name", "##" + swap_name2);
						tmpNode2.SetAttribute("name", swap_name1);
						tmpNode1.SetAttribute("name", swap_name2);
						install_data_node.Parent.SetAttribute("swap_completed", "true");
					}
				}
			}

			// Get the required project node from the model...
			Node n1 = MyNodeTree.GetNamedNode(request_prjIDStr);
			if(null == n1)
			{
				// This will happen if the Facilitator types in a project SIP to install 
				// that doesn't exist (is not running).
				OutputError("Cannot install product " + request_prjIDStr + " as it has not been created.");
				return false;
			}

			//n1.SetAttribute("install_data_node", install_data_name);

			//TODO Check that the location is Good (actually on the Board for a start)
			Boolean LocationGood = true;
			if(LocationGood==false)
			{
				//This will happen if the Facilitator enters a bad location 
				OutputError(" No Location [" + request_location + "] Found ");
				return false;
			}

			//Extract data from the Project Node
			node_projectIDStr = n1.GetAttribute("projectid");
			node_productIDStr = n1.GetAttribute("productid");
			node_platformIDStr = n1.GetAttribute("platformid");
			node_stage = n1.GetAttribute("stage");
			node_projectID = CONVERT.ParseInt(node_projectIDStr);
			node_productID = CONVERT.ParseInt(node_productIDStr);

			if (phase.ToLower() == "transition")
			{	
				//=====================================================================
				//==handling an install for the Transition Phase (days)================
				//=====================================================================
				request_installDay = request_when;
				//Check that the day is OK, we aren't booking a passed day 
				int today = GetCurrentDay();
				if(request_installDay <= today)
				{
					OutputError("Cannot install product " + request_prjIDStr + " on Day " + CONVERT.ToStr(request_installDay) +
						" as today is Day " + CONVERT.ToStr(today));
					return false;
				}
				//Extract out the Project Runner Object
				ProjectRunner runner = (ProjectRunner) MyRunningProjects[request_prjIDStr];
				
				//Check that Has the project already been installed OK
				if (runner.isState_InstallOK())
				{
					OutputError("Cannot install product " + request_prjIDStr + " on Day " + CONVERT.ToStr(request_installDay) +
						" as it's already installed.");
					return false;
				}

				//Have we already got a Booked Day
				int BookedDay = runner.getWhentoInstall();
				if (BookedDay != -1)
				{
					//We have booked an a install day already 
					if (BookedDay == request_when)
					{
						//We are overriding an existing booked day 
						ArrayList al = new ArrayList();
						string InstallActionXML = string.Empty;
						ProjectSIP_Repository.TheInstance.getSIP_Data(node_projectIDStr, node_productIDStr, node_platformIDStr, 
							out al, out InstallActionXML, out SIPNotFound, out ProductNotFound, out PlatformNotFound); 
						//
						if (InstallActionXML != null)
						{
							//We have a Booked Day so we can delete the old booking (if possible)
							HandleCancelBookingForProject(node_projectIDStr, BookedDay);
							//need to create a new Booking for the new Booking Day
							AddCalendarEvent(request_installDay, node_projectIDStr, node_productIDStr, request_location);
							//Update the Project when the location chosen
							runner.ApplyActionOnTimeWithInstallData(InstallActionXML, request_installDay, request_location, request_sla, install_data_name);
							//Update the Project when the location chosen
							runner.UpdateNodeData(request_location, request_sla);
							OpSuccess = true;
						}
						else
						{
							OutputError("Cannot install product with no install actions");
							return false;
						}
					}
					else
					{
						//We are trying to shift the day away from our existing booked day
						//Need to checked that the requested Day is free
						if (this.IsDayFreeInCalendar(request_installDay, node_productIDStr, out SelfBlocked))
						{
							//Yes it's free
							ArrayList al = new ArrayList();
							string InstallActionXML = string.Empty;
							ProjectSIP_Repository.TheInstance.getSIP_Data(node_projectIDStr, node_productIDStr, node_platformIDStr, 
								out al, out InstallActionXML, out SIPNotFound, out ProductNotFound, out PlatformNotFound); 
							//
							if (InstallActionXML != null)
							{
								//We have a Booked Day so we can delete the old booking (if possible)
								HandleCancelBookingForProject(node_projectIDStr, BookedDay);
								//Update the Project when the location chosen
								runner.ApplyActionOnTimeWithInstallData(InstallActionXML, request_installDay, request_location, request_sla, install_data_name);
								//need to create a new Booking for the new Booking Day
								AddCalendarEvent(request_installDay, node_projectIDStr, node_productIDStr,request_location);
								//Update the Project when the location chosen
								runner.UpdateNodeData(request_location, request_sla);
								OpSuccess = true;
							}
							else
							{
								OutputError("Cannot install product with no install actions");
								return false;
							}
						}
						else
						{
							//No, Day id already Booked
							OutputError("Cannot install product " + request_prjIDStr + " on Day " + 
								CONVERT.ToStr(request_installDay) +	" as day is booked");
							return false;
						}
					}
				}
				else
				{
					//there is no booked day for this Project 
					if (this.IsDayFreeInCalendar(request_installDay, node_productIDStr, out SelfBlocked))
					{
						ArrayList al = new ArrayList();
						string InstallActionXML = string.Empty;
						ProjectSIP_Repository.TheInstance.getSIP_Data(node_projectIDStr, node_productIDStr, node_platformIDStr, 
							out al, out InstallActionXML, out SIPNotFound, out ProductNotFound, out PlatformNotFound); 
						//
						if (InstallActionXML != null)
						{
							//need to create a new Booking for the new Booking Day
							AddCalendarEvent(request_installDay, node_projectIDStr, node_productIDStr,request_location);
							//Update the Project when the location chosen
							runner.ApplyActionOnTimeWithInstallData(InstallActionXML, request_installDay, request_location, request_sla, install_data_name);
							//Update the Project when the location chosen
							runner.UpdateNodeData(request_location, request_sla);
							OpSuccess = true;
						}
						else
						{
							OutputError("Cannot install product with no install actions");
							return false;
						}
					}
					else
					{
						//No, Day id already Booked
						OutputError("Cannot install product " + request_prjIDStr + " on Day " + 
							CONVERT.ToStr(request_installDay) +	" as day is booked");
						return false;
					}
				}
			}
			else
			{
				//=====================================================================
				//handling an install for the Operations Phase (mins)
				//=====================================================================
				//Update the Project when the location chosen
				n1.SetAttribute("location",request_location);
				n1.SetAttribute("slalimit",CONVERT.ToStr(request_sla*60));
				request_installMin = request_when;
			
				//Extract the install action 
				ArrayList al = new ArrayList();
				string InstallActionXML = string.Empty;
				ProjectSIP_Repository.TheInstance.getSIP_Data(node_projectIDStr, node_productIDStr, node_platformIDStr, 
					out al, out InstallActionXML, out SIPNotFound, out ProductNotFound, out PlatformNotFound); 
				//
				if (InstallActionXML != null)
				{
					//Create a Project runner to handle the work 	
					ProjectRunner runner = (ProjectRunner) MyRunningProjects[request_prjIDStr];
					runner.ApplyActionOnTimeWithInstallData(InstallActionXML, request_installMin, request_location, request_sla, install_data_name);
					//Add a installday tag into the project tag
					//InsertRequestNode.SetAttribute("installMin", CONVERT.ToStr(request_installDay));
					OpSuccess = true;
				}
				else
				{
					OutputError("Cannot install project with no install actions");
					return false;
				}
			}
			return OpSuccess;
		}

		Boolean HandleCreateProject(Node child)
		{
			Boolean OpSuccess = false;
			Boolean SIPNotFound = true;
			Boolean ProductNotFound = true;
			Boolean PlatformNotFound = true;

			ArrayList PrjAttrs = new ArrayList();
			string FullInstallActionsXML = string.Empty;
			//now we need to extract the project and data and construct a project node in the running project section 
			//Step 1 Extract the attributes (which represents the choices)
			string projectid = child.GetAttribute("projectid");
			string productid = child.GetAttribute("productid");
			string platformid = child.GetAttribute("platformid");
			//Step 2 Extract the SIP information from the Definition File
			if(!ProjectSIP_Repository.TheInstance.getSIP_Data(projectid, productid, platformid, 
				out PrjAttrs, out FullInstallActionsXML, out SIPNotFound, out ProductNotFound, out PlatformNotFound))
			{
				// SIP does not seem to exist.
				Node errorsNode = MyNodeTree.GetNamedNode("FacilitatorNotifiedErrors");
				string errorText = "Cannot create product " + productid + " , " + platformid + ".";
				Node error = new Node(errorsNode, "error", "", new AttributeValuePair( "text", errorText ) );
				return false;
			}

			//Step 3 Calculation of total days 
			int scheduled_days=0;
			foreach (AttributeValuePair avp in PrjAttrs)
			{
				if (avp.Attribute.ToLower() == "designdays")
				{
					scheduled_days += CONVERT.ParseInt(avp.Value);
				}
				if (avp.Attribute.ToLower() == "builddays")
				{
					scheduled_days += CONVERT.ParseInt(avp.Value);
				}
				if (avp.Attribute.ToLower() == "testdays")
				{
					scheduled_days += CONVERT.ParseInt(avp.Value);
				}
			}
			PrjAttrs.Add( new AttributeValuePair("scheduled_days",CONVERT.ToStr(scheduled_days)) );
			PrjAttrs.Add( new AttributeValuePair("completed_days",0) );

			//Step 3 Add in the Execution attributes
			PrjAttrs.Add( new AttributeValuePair(AttrName_ReadyForDeployment,"0") );
			PrjAttrs.Add( new AttributeValuePair("wrequest","1") );
			PrjAttrs.Add( new AttributeValuePair("wcount","1") );
			PrjAttrs.Add( new AttributeValuePair("slalimit","360") ); //Default SLA Limit
			PrjAttrs.Add( new AttributeValuePair("createdinround",CurrentRound) );
			PrjAttrs.Add( new AttributeValuePair("firstday", 1 + GetCurrentDay()) );

			//PrjAttrs.Add( new AttributeValuePair("actual_cost","0") );
			PrjAttrs.Add( new AttributeValuePair("currentspend","0") );

			// You are only ever allowed to have one project running from any particular SIP at a time.
			// Therefore the projectid (SIP number) can be used as a unique name in the model.
			if(null != this.MyNodeTree.GetNamedNode(projectid))
			{
				// This project exists already so fire an error instead.
				Node errorsNode = MyNodeTree.GetNamedNode("FacilitatorNotifiedErrors");
				string errorText = "Specified product is already being developed.";
				Node error = new Node(errorsNode, "error", "", new AttributeValuePair( "text", errorText ) );
				return false;
			}
			
			int CostRequired = 0;	
			int TotalDaysRequired = 0;	
			//extract out the actual_cost
			foreach (AttributeValuePair avp in PrjAttrs)
			{
				if ((avp.Attribute).ToLower() == "actual_cost")
				{
					CostRequired = CONVERT.ParseInt(avp.Value);
				}
				if ((avp.Attribute).ToLower() == "designdays")
				{
					TotalDaysRequired += CONVERT.ParseInt(avp.Value);
				}
				if ((avp.Attribute).ToLower() == "builddays")
				{
					TotalDaysRequired += CONVERT.ParseInt(avp.Value);
				}
				if ((avp.Attribute).ToLower() == "testdays")
				{
					TotalDaysRequired += CONVERT.ParseInt(avp.Value);
				}
			}

			// Check for sufficient budget 
			if(CostRequired > GetRemainingBudget())
			{
				// This project exists already so fire an error instead.
				Node errorsNode = MyNodeTree.GetNamedNode("FacilitatorNotifiedErrors");
				string errorText = "Cannot create product " + productid + " as it costs too much.";
				Node error = new Node(errorsNode, "error", "", new AttributeValuePair( "text", errorText ) );
				return false;
			}

			//Charge the Project 
			this.ReduceRemainingBudget(CostRequired);

			//Create the project 
			Node projectNode = new Node(MyProjectsRunningNode, "project", projectid, PrjAttrs);
			ProjectRunner runner = new ProjectRunner(projectNode,this.TransitionMode);
			//set the create script from the extracted install script
			runner.Set_CreateScriptActionXML(FullInstallActionsXML);
			MyRunningProjects.Add(projectid,runner);
			//
			return OpSuccess;
		}

		void AddExistingProject(Node node, bool inTransitionMode, out Boolean DiscardExisting)
		{
			ArrayList PrjAttrs = null;
			string FullInstallActionsXML = string.Empty;
			DiscardExisting = false;

			Boolean SIPNotFound = true;
			Boolean ProductNotFound = true;
			Boolean PlatformNotFound = true;

			//Need to create a project Runner for the existing pro
			ProjectRunner runner = new ProjectRunner(node, TransitionMode);

			Boolean AddtoCurrentProjects = true;
			Boolean runCreationScript = false;
			if (inTransitionMode == false)
			{
				//We are in The Operations Mode 
				//we need to remove any project which are completed
				if (runner.isState_InstallOK())
				{
					//need to remove the Completed Project
					AddtoCurrentProjects = false;
					//Need to remove the Project from the tree 
					DiscardExisting = true;
					//System.Diagnostics.Debug.WriteLine("Removing completed Project "+runner.getInstallName());
				}
				//we need to remove any project which are completed
				if (runner.isState_NotCompleted())
				{
					//need to mark this project as completed (including the node)
					runner.setStateReady();
					//need to run the creation script for this project
					runCreationScript= true;
				}
			}

			if (AddtoCurrentProjects)
			{
				//extract the project identifiers 
				string projectid = node.GetAttribute("projectid");
				string productid = node.GetAttribute("productid");
				string platformid = node.GetAttribute("platformid");
				//extract the xml for the project 
				if(ProjectSIP_Repository.TheInstance.getSIP_Data(projectid, productid, platformid, 
					out PrjAttrs, out FullInstallActionsXML, out SIPNotFound, out ProductNotFound, out PlatformNotFound))
				{
					runner.Set_CreateScriptActionXML(FullInstallActionsXML);
					MyRunningProjects.Add(projectid,runner);
				}
			}
			if (runCreationScript)
			{
				runner.RunCreateScript();
			}

			//set the create script from the extracted install script
		}

		#endregion Handling Project Requests Methods

		void MyProjectsRunningNode_ChildRemoved(Node sender, Node child)
        {
            // We have a project being removed
            string projectid = child.GetAttribute("projectid");
            MyRunningProjects.Remove(projectid);
        }

		void MyProjectsRunningNode_ChildAdded(Node sender, Node child)
        {
            // We have a project being added
            string projectid = child.GetAttribute("projectid");
            ProjectRunner runner = new ProjectRunner(child, this.TransitionMode);
            MyRunningProjects.Add(projectid, runner);
        }

	}
}
