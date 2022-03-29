using System;
using System.Collections;
using LibCore;
using Network;

namespace IncidentManagement
{
	/// <summary>
	/// This handles the alteration of Zone 1 when we virtualised it in IBM Cloud R3
	/// 
	/// </summary>
	public class Zone_Alter_IBM_CLD : IDisposable
	{
		NodeTree model;
		Node queueNode;

		public Zone_Alter_IBM_CLD (NodeTree model)
		{
			this.model = model;
			queueNode = model.GetNamedNode("AlterZoneQueue");
			queueNode.ChildAdded += queueNode_ChildAdded;
		}

		public void Dispose ()
		{
			if (queueNode != null)
			{
				queueNode.ChildAdded -= queueNode_ChildAdded;
			}
		}

		void applyVirtualMirrorToServer(string ServerName)
		{
			Node SvrNode = this.model.GetNamedNode(ServerName);
			if (SvrNode != null)
			{
				ArrayList attrs = new ArrayList();
				attrs.Add(new AttributeValuePair("virtualmirrored", "true"));
				//attrs.Add(new AttributeValuePair("virtualmirrorinuse", "true"));
				SvrNode.SetAttributes(attrs);
			}
		}

		void changeLocationOfApp(string AppName, string NewLocation)
		{
			ArrayList attrs_app = new ArrayList();
			Node AppNode = this.model.GetNamedNode(AppName);
			string app_old_location = AppNode.GetAttribute("location");

			attrs_app.Clear();
			attrs_app.Add(new AttributeValuePair("location", NewLocation));

			//Apps are not virtual mirrored 
			//attrs_app.Add(new AttributeValuePair("virtualmirrored", "true"));
			attrs_app.Add(new AttributeValuePair("proccap", "1"));
			AppNode.SetAttributes(attrs_app);
		}

		void changeAppParent(string AppName, string newServerName)
		{
			Node AppNode = this.model.GetNamedNode(AppName);
			Node newServerNode = this.model.GetNamedNode(newServerName);

			Node oldParent = AppNode.Parent;
			newServerNode.AddChild(AppNode);
			newServerNode.Tree.FireMovedNode(oldParent, AppNode);
		}

		void moveAppNodeWithinZone(string AppName, string SlotName)
		{
			ArrayList attrs_app = new ArrayList();
			ArrayList attrs_slot = new ArrayList();

			Node AppNode = this.model.GetNamedNode(AppName);
			Node SlotNode = this.model.GetNamedNode(SlotName); 
			string app_old_location = AppNode.GetAttribute("location");
			string slot_old_location = SlotNode.GetAttribute("location");
			Node AppNodeParent = AppNode.Parent;
			Node SlotNodeParent = SlotNode.Parent;

			//Alter the Location
			attrs_app.Clear();
			attrs_app.Add(new AttributeValuePair("location", slot_old_location));
			attrs_app.Add(new AttributeValuePair("virtualmirrored", "true"));
			//attrs_app.Add(new AttributeValuePair("virtualmirrorinuse", "true"));
			AppNode.SetAttributes(attrs_app);
			attrs_slot.Clear();
			attrs_slot.Add(new AttributeValuePair("location", app_old_location));
			attrs_slot.Add(new AttributeValuePair("name", app_old_location));
			SlotNode.SetAttributes(attrs_slot);

			//Move the slot over to the New Parent
			AppNodeParent.AddChild(SlotNode);
			AppNodeParent.Tree.FireMovedNode(SlotNodeParent, SlotNode);
			//Move the App over to the New Parent
			SlotNodeParent.AddChild(AppNode);
			SlotNodeParent.Tree.FireMovedNode(AppNodeParent, AppNode);
		}

		void Clear_AWT_For_NamedNode(string nodename)
		{ 
			Node namedNode = this.model.GetNamedNode(nodename);
			if (namedNode != null)
			{
				namedNode.SetAttribute("danger_level","-1");
			}
		}

		void setAWT_BounceGreen_NamedNode(string nodename)
		{
			Node namedNode = this.model.GetNamedNode(nodename);
			if (namedNode != null)
			{
				namedNode.SetAttribute("danger_level", "22");
			}
		}

		void switchServerON(string server_nodename)
		{
			ArrayList attrs = new ArrayList();
			Node namedNode = this.model.GetNamedNode(server_nodename);
			if (namedNode != null)
			{
				attrs.Clear();
				attrs.Add(new AttributeValuePair("ignore", "false"));
				attrs.Add(new AttributeValuePair("visible", "true"));
				namedNode.SetAttributes(attrs);
			}
		}

		void switchServerOFF(string server_nodename)
		{
			ArrayList attrs = new ArrayList();
			Node namedNode = this.model.GetNamedNode(server_nodename);
			if (namedNode != null)
			{
				attrs.Clear();
				attrs.Add(new AttributeValuePair("ignore", "true"));
				attrs.Add(new AttributeValuePair("visible", "false"));
				namedNode.SetAttributes(attrs);
			}
		}

		void switchRouterOFF(string rtr_nodename)
		{
			ArrayList attrs = new ArrayList();
			Node namedNode = this.model.GetNamedNode(rtr_nodename);
			if (namedNode != null)
			{
				attrs.Clear();
				attrs.Add(new AttributeValuePair("ignore", "true"));
				attrs.Add(new AttributeValuePair("visible", "false"));
				namedNode.SetAttributes(attrs);
			}
		}

		void changeServerLocation(string server_nodename, string newlocation)
		{
			ArrayList attrs = new ArrayList();
			Node namedNode = this.model.GetNamedNode(server_nodename);
			if (namedNode != null)
			{
				attrs.Clear();
				attrs.Add(new AttributeValuePair("location", newlocation));
				namedNode.SetAttributes(attrs);
			}
		}

		Node getServerNode(string server_nodename)
		{
			return this.model.GetNamedNode(server_nodename);
		}

		void setVirtualGroupToServer(string server_nodename, string virtual_group_level)
		{
			ArrayList attrs = new ArrayList();
			Node namedNode = this.model.GetNamedNode(server_nodename);
			if (namedNode != null)
			{
				attrs.Clear();
				attrs.Add(new AttributeValuePair("virtual_group", virtual_group_level));
				namedNode.SetAttributes(attrs);
			}
		}

		void ChangeAppPriority(string app_name, int new_priority_level)
		{
			Node AppNode = this.model.GetNamedNode(app_name);
			if (AppNode != null)
			{
				//System.Diagnostics.Debug.WriteLine("APP:" + app_name + "LEVEL:" + new_priority_level.ToString());
				AppNode.SetAttribute("virtual_priority_level", CONVERT.ToStr(new_priority_level));
			}
		}

		void setUpVirtualCore(int numExtraServers)
		{
			int num_cores =0;

			Node namedNode = null;
			namedNode = this.model.GetNamedNode("Metis");
			if (namedNode != null)
			{
				num_cores += namedNode.GetIntAttribute("cores", 0);
			}
			namedNode = this.model.GetNamedNode("Tarvos");
			if (namedNode != null)
			{
				num_cores += namedNode.GetIntAttribute("cores", 0);
			}
			namedNode = this.model.GetNamedNode("Iris");
			if (namedNode != null)
			{
				num_cores += namedNode.GetIntAttribute("cores", 0);
			}
			if (numExtraServers > 1)
			{
				namedNode = this.model.GetNamedNode("Aurora");
				if (namedNode != null)
				{
					num_cores += namedNode.GetIntAttribute("cores", 0);
				}			
			}
			namedNode = this.model.GetNamedNode("virtual_core_status");
			namedNode.SetAttribute("cores_in_use", CONVERT.ToStr(num_cores));
		}

		void ChangeZoneToPrivateCloud(int command_target_zone, Node request_node)
		{
			//=========================================================================
			//==Extract Information from the request node============================== 
			//=========================================================================
			int extra_server_count = request_node.GetIntAttribute("extra_server_count", 1);
			Hashtable ServiceRequestDefinitions = new Hashtable();

			//extract the data values 
			foreach (AttributeValuePair avp in request_node.AttributesAsArrayList)
			{
				string attr_name = avp.Attribute;
				string attr_value = avp.Value;

				if (attr_name.IndexOf("data")>-1)
				{
					string service_data = attr_value;
					string[] parts = service_data.Split('#');
					string service_name = parts[0];
					int service_level = CONVERT.ParseIntSafe(parts[1], 1);
					ServiceRequestDefinitions.Add(service_name, service_level);
				}
			}

			//=========================================================================
			//==Marking that we have deployed the Virtual Zone
			//=========================================================================
			Node tmpNode = null;
			tmpNode = this.model.GetNamedNode("virtual_deployment");
			if (tmpNode != null)
			{
				tmpNode.SetAttribute("deployed","true");
			}
			//=========================================================================
			//==Alter the Flash Board that we need to use
			//=========================================================================
			tmpNode = this.model.GetNamedNode("flashboard_override");
			if (tmpNode != null)
			{
				tmpNode.SetAttribute("board_name","board_r3.swf");
			}
			//=========================================================================
			//==Alter the Location, Power and Virtual Mirror of the Apps 
			//=========================================================================
			changeLocationOfApp("Kastra", "E446");
			changeLocationOfApp("Kaus", "E440");
			changeLocationOfApp("Navi", "E433");
			changeLocationOfApp("Castor", "E426");

			changeLocationOfApp("Haldus", "E445");
			changeLocationOfApp("Chertan", "E439");
			changeLocationOfApp("Bellatrix", "E425");

			changeLocationOfApp("Atlas", "E438");
			changeLocationOfApp("Electra", "E424");

			changeLocationOfApp("Lucida", "E437");
			changeLocationOfApp("Canopus", "E430");
			changeLocationOfApp("Okul", "E423");

			//=========================================================================
			//==Apply Virtual Mirror Tech to servers 
			//=========================================================================
			applyVirtualMirrorToServer("Metis");
			applyVirtualMirrorToServer("Tarvos");
			changeServerLocation("Metis", "E410");
			changeServerLocation("Tarvos", "E411");

			switchServerON("Metis");
			switchServerON("Tarvos");
			setAWT_BounceGreen_NamedNode("Metis");
			setAWT_BounceGreen_NamedNode("Tarvos");
			//=========================================================================
			//==Switch the Extra Servers ON
			//=========================================================================
			if ((extra_server_count == 1)|(extra_server_count == 2))
			{
				switchServerON("Iris");
				applyVirtualMirrorToServer("Iris");
				changeServerLocation("Iris", "E412");
				setAWT_BounceGreen_NamedNode("Iris");
			}
			if (extra_server_count == 2)
			{
				switchServerON("Aurora");
				applyVirtualMirrorToServer("Aurora");
				changeServerLocation("Aurora", "E413");
				setAWT_BounceGreen_NamedNode("Aurora");
			}

			setUpVirtualCore(extra_server_count);
			//=========================================================================
			//==Hide the old Router 
			//=========================================================================
			switchRouterOFF("M2403");
			//=========================================================================
			//==Hide the old servers 
			//=========================================================================
			switchServerOFF("Cybele");
			switchServerOFF("Europa");
			switchServerOFF("Juno");
			switchServerOFF("Pallas");
			switchServerOFF("Pluto");
			switchServerOFF("Saturn");
			//Affecting the Servers which are now Empty
			Clear_AWT_For_NamedNode("Cybele");
			Clear_AWT_For_NamedNode("Europa");
			Clear_AWT_For_NamedNode("Juno");
			Clear_AWT_For_NamedNode("Pallas");
			Clear_AWT_For_NamedNode("Pluto");
			Clear_AWT_For_NamedNode("Saturn");

			//=========================================================================
			//==Apply High, Medium, Low
			//=========================================================================
			ArrayList lowServers = new ArrayList();
			ArrayList mediumServers = new ArrayList();
			ArrayList highServers = new ArrayList();

			setVirtualGroupToServer("Tarvos", "low");
			lowServers.Add(getServerNode("Tarvos"));
			setVirtualGroupToServer("Metis", "medium");
			mediumServers.Add(getServerNode("Metis"));

			setVirtualGroupToServer("Iris", "high");
			highServers.Add(getServerNode("Iris"));

			if (extra_server_count == 2)
			{
				setVirtualGroupToServer("Aurora", "high");
				highServers.Add(getServerNode("Aurora"));
			}


			//=========================================================================
			//==Get the requested priority of the service names 
			//=========================================================================
			Hashtable servicesByName = new Hashtable();

			Node srvice_pls = this.model.GetNamedNode("ServicePriorityLevels");
			foreach (Node n1 in srvice_pls.getChildren())
			{
				bool visible = n1.GetBooleanAttribute("visible", false);
				string desc = n1.GetAttribute("desc");
				int level = 1;
				
				if (ServiceRequestDefinitions.ContainsKey(desc))
				{
					level = (int)ServiceRequestDefinitions[desc];
				}
				n1.SetAttribute("level", CONVERT.ToStr(level));
				if (visible)
				{
					servicesByName.Add(desc, level);
				}
			}
			//=========================================================================
			//==Reassign the Parent of the App depending on HML =======================
			//=========================================================================
			//Need to move all apps to the server that has thier General Priority Level 
			//These are just defined in the xml since the effects are applied by a seperate engine componenet
			//The actual parents is not a concern 
			Hashtable ht = new Hashtable();
			ArrayList types = new ArrayList();
			
			types.Clear();
			types.Add("App");

			ht = this.model.GetNodesOfAttribTypes(types);
			foreach (Node appnode in ht.Keys)
			{
				bool new_level_defined = false;
				int new_priority_level =0;

				//Extract the data for each app
				string app_name = appnode.GetAttribute("name");
				int app_zone = appnode.GetIntAttribute("proczone", 0);
				string app_vgroup_name = appnode.GetAttribute("virtual_group_name");
				string app_vgroup_level = appnode.GetAttribute("virtual_group_level");
				if (app_vgroup_name != "")
				{
					if (servicesByName.ContainsKey(app_vgroup_name))
					{
						new_priority_level = (int)servicesByName[app_vgroup_name];
						new_level_defined = true;
					}

					if (app_zone == command_target_zone)
					{
						//using the virtual group level to swap the app to a server
						Node target_server_node = null;
						switch (app_vgroup_level)
						{
							case "low":
								target_server_node = (Node) lowServers[0];
								break;
							case "medium":
								target_server_node = (Node) mediumServers[0];
								break;
							case "high":
								target_server_node = (Node) highServers[0];
								break;
						}
						if (target_server_node != null)
						{
							if (target_server_node != appnode.Parent)
							{
								string new_server_parent = target_server_node.GetAttribute("name");
								changeAppParent(app_name, new_server_parent);
							}
							if (new_level_defined)
							{
								ChangeAppPriority(app_name, new_priority_level);
							}
						}
					}
				}
			}
			//=========================================================================
			//==Refresh the AWT Display system =========================================
			//=========================================================================
			refreshAWTSystem();
		}

		void refreshAWTSystem()
		{
			Node MyAWT_SystemNode = this.model.GetNamedNode("AdvancedWarningTechnology");
			MyAWT_SystemNode.SetAttribute("refresh", "0");
			MyAWT_SystemNode = null;
		}

		void ChangeZonePriorityLevels(int command_target_zone, Node request_node)
		{
			//=========================================================================
			//==Extract Information from the request node============================== 
			//=========================================================================
			Hashtable ServiceRequestDefinitions = new Hashtable();
			//extract the data values 
			foreach (AttributeValuePair avp in request_node.AttributesAsArrayList)
			{
				string attr_name = avp.Attribute;
				string attr_value = avp.Value;

				if (attr_name.IndexOf("data")>-1)
				{
					string service_data = attr_value;
					string[] parts = service_data.Split('#');
					string service_name = parts[0];
					int service_level = CONVERT.ParseIntSafe(parts[1], 1);
					ServiceRequestDefinitions.Add(service_name, service_level);
				}
			}
			//=========================================================================
			//==Get the requested priority of the service names 
			//=========================================================================
			Hashtable servicesByName = new Hashtable();

			Node srvice_pls = this.model.GetNamedNode("ServicePriorityLevels");
			foreach (Node n1 in srvice_pls.getChildren())
			{
				bool visible = n1.GetBooleanAttribute("visible", false);
				string desc = n1.GetAttribute("desc");
				int level = 1;
				
				if (ServiceRequestDefinitions.ContainsKey(desc))
				{
					level = (int)ServiceRequestDefinitions[desc];
				}
				n1.SetAttribute("level", CONVERT.ToStr(level));
				if (visible)
				{
					servicesByName.Add(desc, level);
				}
			}
			//=========================================================================
			//==Reassign the Priority levels of the App================================ 
			//=========================================================================
			Hashtable ht = new Hashtable();
			ArrayList types = new ArrayList();
			
			types.Clear();
			types.Add("App");

			ht = this.model.GetNodesOfAttribTypes(types);
			foreach (Node appnode in ht.Keys)
			{
				bool new_level_defined = false;
				int new_priority_level =0;

				//Extract the data for each app
				string app_name = appnode.GetAttribute("name");
				int app_zone = appnode.GetIntAttribute("proczone", 0);
				string app_vgroup_name = appnode.GetAttribute("virtual_group_name");
				string app_vgroup_level = appnode.GetAttribute("virtual_group_level");
				if (app_vgroup_name != "")
				{
					if (servicesByName.ContainsKey(app_vgroup_name))
					{
						new_priority_level = (int)servicesByName[app_vgroup_name];
						new_level_defined = true;

						if (app_zone == command_target_zone)
						{
							//using the virtual group level to swap the app to a server
							if (new_level_defined)
							{
								ChangeAppPriority(app_name, new_priority_level);
							}
						}
					}
				}
			}


			if (srvice_pls != null)
			{
				int change_count = srvice_pls.GetIntAttribute("change_count", 0);
				change_count = change_count + 1;
				srvice_pls.SetAttribute("change_count", CONVERT.ToStr(change_count));
			}
		}

		void ImplementPublicCloud(int command_target_zone, Node request_node)
		{
			//=========================================================================
			//==Marking that we have upgraded the Virtual Zone to use a public cloud
			//=========================================================================
			Node tmpNode = null;
			tmpNode = this.model.GetNamedNode("virtual_deployment");
			if (tmpNode != null)
			{
				//Marked the option taken as public cloud 
				tmpNode.SetAttribute("upgrade", "public_cloud");

				int extra_cpu = request_node.GetIntAttribute("number_of_extra_cpu", 0);
				//record the number of extra cpu units booked 
				tmpNode.SetAttribute("number_of_extra_cpu", CONVERT.ToStr(extra_cpu));
			}
		}

		void ImplementExtraServers(int command_target_zone, Node request_node)
		{
			//=========================================================================
			//==Marking that we have upgraded the Virtual Zone to use a extra 5 servers 
			//=========================================================================
			Node tmpNode = null;
			tmpNode = this.model.GetNamedNode("virtual_deployment");
			if (tmpNode != null)
			{
				tmpNode.SetAttribute("upgrade", "extra_five");
			}

			tmpNode = this.model.GetNamedNode("Expenses");
			if (tmpNode != null)
			{
				//Add 30,000 to the capex
				int capex_new_services = tmpNode.GetIntAttribute("capex_new_services",0);
				capex_new_services = capex_new_services + 30000;
				tmpNode.SetAttribute("capex_new_services",CONVERT.ToStr(capex_new_services));
				
				//Add 10,000 to the opex
				int opex = tmpNode.GetIntAttribute("opex",0);
				opex = opex + 10000;
				tmpNode.SetAttribute("opex",CONVERT.ToStr(opex));
			}
		}

		void queueNode_ChildAdded (Node sender, Node child)
		{
			string command_type = child.GetAttribute("type").ToLower();
			int command_target_zone = child.GetIntAttribute("target_zone", 0);
			string command_alter_option = child.GetAttribute("alter_option");

			switch (command_type)
			{
				case "alter_zone":
					{
						switch (command_alter_option)
						{
							case "private_cloud":
								//
								ChangeZoneToPrivateCloud(command_target_zone, child);
								break;
							case "prioritylevels":
								//
								ChangeZonePriorityLevels(command_target_zone, child);
								break;
							case "deploy_public_cloud":
								//
								ImplementPublicCloud(command_target_zone, child);
								break;
							case "deploy_five_servers":
								//
								ImplementExtraServers(command_target_zone, child);
								break;
						}
						break;
					}
			}
			queueNode.DeleteChildTree(child);
		}
	}
}