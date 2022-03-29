using System;
using System.Collections;

using Network;
using LibCore;

namespace IncidentManagement
{
	/// <summary>
	/// The SLAManager contains 
	///   Static Access Routines for getting and setting the SLA Information 
	///     We need to manage the propagate the limits down to the BSU for each car as well
	///   Monitor Functionality 
	///     We need to watch the Business Service User Nodes 
	///       (if the "downforsecs" node changes above the slalimit node then the "slabreach" is true)
	///       (if the "downforsecs" node changes below the slalimit node then the "slabreach" is false)
	///       (if we change the "slabreach" node then we increment the sla_breach_round_count)
	/// </summary>
	public class SLAManager : ISlaManager
	{
		Hashtable ht = new Hashtable();
		Node businessServicesGroup;
		NodeTree MyTreeRootHandle = null;

		ArrayList MyBusinessServiceNodes = new ArrayList();			//for propagating sla changes
		ArrayList MyPlayerCarNodes = new ArrayList();						//for handling 
		ArrayList MyBusinessServiceUserNodes = new ArrayList(); //Monitoring Down Time

		/// <summary>
		/// 
		/// </summary>
		/// <param name="model"></param>
		public SLAManager(NodeTree model)
		{
			//Keep a local jhandle 
			MyTreeRootHandle = model;
			
			//=============================================================================
			//==Propagation of the SLA changes=============================================
			//=============================================================================
			//There are 2 ways of changing the SLA value that a BSU needs to operate under 
			// A, Setting of the Business Service Attribute (handled by the Static Methods)
			// B, Movement of the Link nodes under the Business Service (moving the links to the new active version)
      //      This happens as part of the new SIP installation xml 
			// While A is handled, B needs us to monitor the Business Service Nodes for children added
			// so we can propagate the SLA of that Business Service to the required Business Services User nodes

			businessServicesGroup = MyTreeRootHandle.GetNamedNode("Business Services Group");
			
			//connect up to existing Business Services 
			ArrayList existingkids = businessServicesGroup.getChildren();
			foreach (Node kid in existingkids)
			{
				AddBusinessService(kid);
			}
			//handling New and Removed Business Services 
			businessServicesGroup.ChildAdded += businessServicesGroup_ChildAdded;
			businessServicesGroup.ChildRemoved += businessServicesGroup_ChildRemoved;

			//=============================================================================
			//==Monitoring the Business Service User Down Time for SLA Breaches============
			//=============================================================================
			// For each player car 
			//   attach to all children (watching for attribute changes [downforsecs="0"] and [slalimit]
			//   handle any children added
			//   handle any children removed

			string biz_name = CoreUtils.SkinningDefs.TheInstance.GetData("biz");
			string our_biz_name = CoreUtils.SkinningDefs.TheInstance.GetData("ourbiz");

			ArrayList types = new ArrayList();
			types.Add(biz_name);
			Hashtable allCars = MyTreeRootHandle.GetNodesOfAttribTypes(types);

			foreach (Node n1 in allCars.Keys)
			{
				Boolean ourcars = n1.GetBooleanAttribute(our_biz_name,false);

				if (ourcars)
				{
					AddCarNode(n1);
				}
			}
		}

		/// <summary>
		/// removing all the monitoring 
		/// </summary>
		public void Dispose()
		{
			//=======================================================================
			//==Removing the Business Service monitoring and objects
			//=======================================================================
			businessServicesGroup.ChildAdded -= businessServicesGroup_ChildAdded;
			businessServicesGroup.ChildRemoved -= businessServicesGroup_ChildRemoved;

			//Detach from any existing monitored project nodes 
			foreach (object o1 in MyBusinessServiceNodes)
			{
				Node child = (Node) o1;
				child.AttributesChanged -= MyBusinessService_AttributesChanged;
			}
			//Clear my list of monitored nodes
			MyBusinessServiceNodes.Clear();

			//=======================================================================
			//==Detach from any existing monitored player car nodes 
			//=======================================================================
			foreach (object o1 in MyPlayerCarNodes)
			{
				Node child = (Node) o1;
				child.ChildAdded -=MyCarNode_ChildAdded;
				child.ChildRemoved -=MyCarNode_ChildRemoved;
			}
			MyPlayerCarNodes.Clear();
			//=======================================================================
			//==Detach from any existing monitored business service user nodes=======
			//=======================================================================
			foreach (object o1 in MyBusinessServiceUserNodes)
			{
				Node child = (Node) o1;
				child.AttributesChanged -= MyBusinessServiceUser_AttributesChanged;
			}
			MyBusinessServiceUserNodes.Clear();
		}

		#region Business Service Monitoring 

		void  businessServicesGroup_ChildAdded(Node sender, Node child)
		{
			AddBusinessService(child);
		}

		void  businessServicesGroup_ChildRemoved(Node sender, Node child)
		{
			RemoveBusinessService(child);
		}

		/// <summary>
		/// Handling the addition of a Business Service 
		/// </summary>
		/// <param name="child"></param>
		void AddBusinessService(Node child)
		{
			if (MyBusinessServiceNodes.Contains(child)==false)
			{
				//add to my list of Monitored Business Services 
				MyBusinessServiceNodes.Add(child);
				//add a monitor of the attributes changes
				child.AttributesChanged +=MyBusinessService_AttributesChanged;
			}
		}

		/// <summary>
		/// Removing the addition of a Business Service 
		/// </summary>
		/// <param name="child"></param>
		void RemoveBusinessService(Node child)
		{
			if (MyBusinessServiceNodes.Contains(child))
			{
				//add to my list of Monitored Business Services 
				MyBusinessServiceNodes.Remove(child);
				//add a monitor of the attributes changes
				child.AttributesChanged -=MyBusinessService_AttributesChanged;
			}
		}

		/// <summary>
		/// Handling the Business Service changing it's attributes 
		/// It's on the slalimit that we are interested in and we need to pass it down to the BSUs 
		/// </summary>
		/// <param name="sender">Node which has changed</param>
		/// <param name="attrs">What has changed</param>
		void MyBusinessService_AttributesChanged(Node sender, ArrayList attrs)
		{
			if (attrs != null && attrs.Count > 0)
			{
				foreach(AttributeValuePair avp in attrs)
				{
					if (avp.Attribute == "slalimit")
					{
						//connect up to existing Business Services 
						ArrayList existingLinks = sender.getChildren();
						foreach (Node link_kid in existingLinks)
						{
							string bsu_name = link_kid.GetAttribute("to");
							if (bsu_name != string.Empty)
							{
								Node bsu = this.MyTreeRootHandle.GetNamedNode(bsu_name);
								if (bsu != null)
								{
									bsu.SetAttribute("slalimit",avp.Value);
								}
							}
						}
					}
				}
			}
		}

		#endregion Business Service Monitoring 

		#region Business Service User Monitoring 

		/// <summary>
		/// Handling the addition of a Business Service 
		/// </summary>
		/// <param name="child"></param>
		void AddCarNode(Node child)
		{
			if (MyPlayerCarNodes.Contains(child)==false)
			{
				//add to my list of Monitored Cars Services 
				MyPlayerCarNodes.Add(child);
				child.ChildAdded +=MyCarNode_ChildAdded;
				child.ChildRemoved +=MyCarNode_ChildRemoved;
				//add Monitoring for all the Business Servioce Users Nodxes in this car 
				foreach (Node n2 in child.getChildren())
				{
					AddBusinessServiceUser(n2);
				}
			}
		}

		void RemoveCarNode(Node child)
		{
			if (MyPlayerCarNodes.Contains(child))
			{
				//add to my list of Monitored Cars Services 
				MyPlayerCarNodes.Remove(child);
				child.ChildAdded -=MyCarNode_ChildAdded;
				child.ChildRemoved -=MyCarNode_ChildRemoved;
				//add Monitoring for all the Business Servioce Users Nodxes in this car 
				foreach (Node n2 in child.getChildren())
				{
					RemoveBusinessServiceUser(n2);
				}
			}
		}

		void MyCarNode_ChildAdded(Node sender, Node child)
		{
			AddBusinessServiceUser(child);
		}

		void MyCarNode_ChildRemoved(Node sender, Node child)
		{
			RemoveBusinessServiceUser(child);
		}

		void AddBusinessServiceUser(Node child)
		{
			if (MyBusinessServiceUserNodes.Contains(child)==false)
			{
				MyBusinessServiceUserNodes.Add(child);
				child.AttributesChanged += MyBusinessServiceUser_AttributesChanged;
			}
		}

		void RemoveBusinessServiceUser(Node child)
		{
			if (MyBusinessServiceUserNodes.Contains(child))
			{
				MyBusinessServiceUserNodes.Remove(child);
				child.AttributesChanged -= MyBusinessServiceUser_AttributesChanged;
			}
		}

		void UpdateBreachCount(int impact, string neam, int dt)
		{
			//extract the various values 


//			Node node_breach = MyTreeRootHandle.GetNamedNode("SLA_Breach");
//			int breach_bs_count = node_breach.GetIntAttribute("biz_serv_count",0);
//			int breach_bsu_count = node_breach.GetIntAttribute("biz_serv_user_count",0);
//			int breach_impact_count = node_breach.GetIntAttribute("impact_count",0);
//
//			//Update the various values 
//			breach_impact_count++;										//A straight of how many bsu breaches we have had
//			breach_bsu_count += impact;								//A weighted sum of all breaches
//			breach_bs_count = breach_bsu_count / 12;	//The number of breachs in terms of Busines Services 
//
//			node_breach.SetAttribute("impact_count",CONVERT.ToStr(breach_impact_count));
//			node_breach.SetAttribute("biz_serv_user_count",CONVERT.ToStr(breach_bsu_count));
//			node_breach.SetAttribute("biz_serv_count",CONVERT.ToStr(breach_bs_count));



			//System.Diagnostics.Debug.WriteLine("###  "+neam+ "IM  CountPost:"+breach_impact_count.ToString()+ "     with dt"+dt.ToString());
			//System.Diagnostics.Debug.WriteLine("###  "+neam+ "BSU CountPost:"+breach_bsu_count.ToString()+ "     with dt"+dt.ToString());
			//System.Diagnostics.Debug.WriteLine("###  "+neam+ "BS  CountPost:"+breach_bs_count.ToString()+ "     with dt"+dt.ToString());
		}

		/// <summary>
		/// Main Operational Method which Handles the Business Service User attribute changes
		///
		///   "slalimit" -- The user has changed the SLA limit, we need to ensure that SLA system is updated  
		///      -- if "downforsecs" exceeds new limit and slabreach = false 
		///      -- then slabreach = true and sla_breach_round_count incremented
		///      -- if "downforsecs" inside new limit and slabreach = true 
		///      -- then slabreach = false
		///
		///   "downforsecs" -- The down time has changed 
		///      -- if "downforsecs" exceeds slaimit and slabreach = false 
		///      -- then slabreach = true and sla_breach_round_count incremented
		///      -- if "downforsecs" inside slaimit and slabreach = true
		///      -- then slabreach = false
		///   
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="attrs"></param>
		void MyBusinessServiceUser_AttributesChanged(Node sender, ArrayList attrs)
		{
			if (attrs != null && attrs.Count > 0)
			{
				foreach(AttributeValuePair avp in attrs)
				{
					//handling the change to the slalimit attribute
					if (((string)avp.Attribute).ToLower() == "slalimit")
					{
						int sla_limit = sender.GetIntAttribute("slalimit",0);
						bool sla_breachlimit = sender.GetBooleanAttribute("slabreach",false);
						bool hasImpact = sender.GetBooleanAttribute("has_impact",false);
						int down_time = sender.GetIntAttribute("downforsecs",0);
						int sla_impact = sender.GetIntAttribute("slaimpact",0);
						string sender_name = sender.GetAttribute("name");

						if (down_time > sla_limit)
						{
							if (sla_breachlimit==false)
							{
								//OLD IMPACT CODE
								//only if there is a impact in either speed or pit
								//if ((impact!=0)|(impactsecsinpit!=0))
								//NEW IMPACT CODE
								if (hasImpact)
								{
									sender.SetAttribute("slabreach","true");
									//UpdateBreachCount(sla_impact, sender_name, 0);
								}
							}
						}
						else
						{
							if (sla_breachlimit==true)
							{
								sender.SetAttribute("slabreach","false");
							}
						}
					}
					//handling the change to the downforsecs attribute

					if ((string)avp.Attribute == "downforsecs")
					{
						int sla_limit = sender.GetIntAttribute("slalimit",0);
						bool sla_breachlimit = sender.GetBooleanAttribute("slabreach",false);
						bool hasImpact = sender.GetBooleanAttribute("has_impact",false);
						int down_time = sender.GetIntAttribute("downforsecs",0);
						int sla_impact = sender.GetIntAttribute("slaimpact",0);
						string sender_name = sender.GetAttribute("name");

						if (down_time > sla_limit)
						{
							if (sla_breachlimit==false)
							{
								//OLD IMPACT CODE
								//only if there is a impact in either speed or pit
								//if ((impact!=0)|(impactsecsinpit!=0))
								//NEW IMPACT CODE
								if (hasImpact)
								{
									sender.SetAttribute("slabreach","true");
									string neam = sender.GetAttribute("name");
								}
							}
						}
						else
						{
							if (sla_breachlimit==true)
							{
								sender.SetAttribute("slabreach","false");
							}
						}
					}
				}
			}
		}

		#endregion Business Service User Monitoring 

		#region SLA Data Access Methods


		/// <summary>
		/// Returns sla as a string, used in Project Manager
		/// Used in replacing the YYY tag
		/// </summary>
		/// <param name="n"></param>
		/// <returns></returns>
		public static string get_SLA_string(Node n)
		{
			return n.GetAttribute("slalimit");
		}

		/// <summary>
		/// Returns sla for services not related to sips
		/// </summary>
		/// <param name="n"></param>
		/// <returns></returns>
		public static int get_SLA(Node n)
		{
			return n.GetIntAttribute("slalimit",0);
		}

		/// <summary>
		/// Sets sla for services not related to sips
		/// </summary>
		/// <param name="n"></param>
		/// <param name="sla"></param>
		public static void set_SLA(Node n, int sla)
		{
			n.SetAttribute("slalimit",sla);
			//we need to update the Business Service User Nodes of this Business Service
			ArrayList existingLinks = n.getChildren();
			foreach (Node link_kid in existingLinks)
			{
				string bsu_name = link_kid.GetAttribute("to");
				if (bsu_name != string.Empty)
				{
					Node bsu = n.Tree.GetNamedNode(bsu_name);
					if (bsu != null)
					{
						bsu.SetAttribute("slalimit",CONVERT.ToStr(sla));
					}
				}
			}
		}

		/// <summary>
		///  Returns the sla from the business service if it is created or the project
		///  if it isnt, burns and dies if neither relating to sip are found
		/// </summary>
		/// <param name="_nt"></param>
		/// <param name="sip"></param>
		/// <returns></returns>
		public static int get_SLA(NodeTree _nt, string sip)
		{
			ArrayList project = _nt.GetNodesWithAttributeValue("created_by_sip",sip);
			// biz service has been created
			if(project.Count != 0)
			{
				//extract the biz_service_function attribute so that we can extract the sla of the 
				string bfn = ((Node)project[0]).GetAttribute("biz_service_function");
				if (bfn != "")
				{
					ArrayList nodes = _nt.GetNodesWithAttributeValue("biz_service_function",bfn);
					int slalimit = 360;
					if (nodes.Count>0)
					{
						foreach (Node n in nodes)
						{
							if (n.GetAttribute("type")=="biz_service")
							{
								slalimit = n.GetIntAttribute("slalimit",-1);
								if (slalimit==-1)
								{
									slalimit=360;
								}
							}
						}
						return slalimit;
					}
					else
					{
						return ((Node)project[0]).GetIntAttribute("slalimit",0);
					}
				}
				else
				{
					return ((Node)project[0]).GetIntAttribute("slalimit",0);			
				}
			}
				// biz service has not been created, find project name="sip" and get that
			else
			{
				project = _nt.GetNodesWithAttributeValue("name",sip);

				// : fix for 4410 (install panel doesn't preserve SLA of previous version).
				Node projectNode = (Node) project[0];
				string upgrade = projectNode.GetAttribute("upgradename");
				if (upgrade != "")
				{
					Node existingNode = _nt.GetNamedNode(upgrade);
					if (existingNode != null)
					{
						string desc = existingNode.GetAttribute("desc");
						if (desc != "")
						{
							Node serviceNode = _nt.GetNamedNode(desc);
							if (serviceNode != null)
							{
								string sla = serviceNode.GetAttribute("slalimit");
								if (sla != "")
								{
									return CONVERT.ParseInt(sla);
								}
							}
						}
					}
				}

				return projectNode.GetIntAttribute("slalimit", 0);
			}
				
		}

		/// <summary>
		///  Sets the sla from the business service if it is created or the project
		///  if it isnt, burns and dies if neither relating to sip are found
		/// </summary>
		/// <param name="_nt"></param>
		/// <param name="sip"></param>
		/// <param name="sla"></param>
		public static void set_SLA(NodeTree _nt, string sip, int sla)
		{
			ArrayList project = _nt.GetNodesWithAttributeValue("created_by_sip",sip);

			if(project.Count != 0)
			{
				((Node)project[0]).SetAttribute("slalimit",sla);
			}
			else
			{
				// biz service has not been created, find project name="sip" and set that
				project = _nt.GetNodesWithAttributeValue("name",sip);
				((Node)project[0]).GetIntAttribute("slalimit",0);
			}
		}

		/// <summary>
		/// Checks if a sip related to a valid project or business service
		/// </summary>
		/// <param name="_nt"></param>
		/// <param name="sip"></param>
		/// <returns></returns>
		public static bool is_SIP(NodeTree _nt, string sip)
		{
			return (_nt.GetNodesWithAttributeValue("created_by_sip",sip).Count != 0 ||
					_nt.GetNodesWithAttributeValue("name",sip).Count != 0);		
		}

		#endregion SLA Data Access Methods

		/// <summary>
		/// This resets the BSUs SLA breach flags
		/// Needs to be a method sice we don't know (especially in the later rounds) what BSUs we have
		/// </summary>
		/// <param name="model"></param>
		public void ResetBsuSlaBreaches ()
		{
			ArrayList types = new ArrayList();
			types.Add("biz_service_user");

			foreach (Node n1 in MyTreeRootHandle.GetNodesOfAttribTypes(types).Keys)
			{
				n1.SetAttribute("slabreach","false");
			}
		}
	}
}