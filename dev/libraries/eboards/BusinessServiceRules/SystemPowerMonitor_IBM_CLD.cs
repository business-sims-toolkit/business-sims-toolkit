using System;
using System.Collections;
using LibCore;
using Network;

namespace BusinessServiceRules
{
	/// <summary>
	/// This is a monitor to determine the use of system power
	/// It also calculates the following 
	///   Number of active servers (they have atleast 1 software app)
	///   Server Utilisation 
	/// </summary>
	public class SystemPowerMonitor_IBM_CLD : SystemPowerMonitorBase
	{
		protected NodeTree _model;
		protected Node _powerLevelNode;
		protected Node _serverUtilLevelNode;
		
		protected ArrayList KnownRouters = new ArrayList();
		protected ArrayList KnownServers = new ArrayList();
		protected ArrayList KnownSoftwareSlots = new ArrayList();
		
		protected Hashtable ServerNodes_StatusCache = new Hashtable();
		protected Hashtable ServerNodes_CapabilityCache = new Hashtable();
		protected Hashtable AppNodes_StatusCache = new Hashtable();
		protected Hashtable AppNodes_CapabilityCache = new Hashtable();
		protected Hashtable DatabaseNodes_StatusCache = new Hashtable();
		protected Hashtable DatabaseNodes_CapabilityCache = new Hashtable();

		protected Hashtable KnownItemsByZone = new Hashtable();
		protected Hashtable KnownItemsByServer = new Hashtable();
		protected Hashtable KnownItemsNodeToNameLookup = new Hashtable();

		protected ArrayList RequiredNodes = new ArrayList();
		protected Hashtable NodeStatusCache = new Hashtable();
		
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="nt"></param>
		public SystemPowerMonitor_IBM_CLD(NodeTree nt):base(nt)
		{
			Construct(nt);
		}

		protected virtual void Construct(NodeTree nt)
		{
			_model = nt;

			for (int step =0; step <7; step++)
			{
				KnownItemsByZone[step] = new Hashtable();
			}

			_powerLevelNode = _model.GetNamedNode("PowerLevel");
			_serverUtilLevelNode = _model.GetNamedNode("ServerUtil");

			//System.Diagnostics.Debug.WriteLine("SYSTEM POWER MONITOR START");
			BuildMonitoring();
			BuildCache();
			BuildPowerScore();
		}


		/// <summary>
		/// Dispose all event handlers and anything else
		/// </summary>
		public override void Dispose()
		{
			DisposeMonitoring();
			_model = null;
			//System.Diagnostics.Debug.WriteLine("SYSTEM POWER MONITOR DISPOSE");
		}

		#region Helper Methods For KnownItemsByZone Lookup

		public void Rename_Item_By_Zone(string oldname, string newname)
		{
			if (KnownItemsByZone.ContainsKey(oldname))
			{
				int zone = (int)KnownItemsByZone[oldname];
				KnownItemsByZone.Add(newname,zone);
				Remove_Item_By_Zone(oldname);
			}
		}

		public void Add_Item_By_Zone(string name, int zone)
		{
			if (KnownItemsByZone.ContainsKey(name)==false)
			{
				KnownItemsByZone.Add(name,zone);
			}
		}
		
		public void Remove_Item_By_Zone(string name)
		{
			if (KnownItemsByZone.ContainsKey(name))
			{
				KnownItemsByZone.Remove(name);
			}
		}

		#endregion Helper Methods For KnownItemsByZone Lookup

		#region Helper Methods For KnownItemsByServer Lookup

		public void Rename_Item_By_Server(string oldname, string newname)
		{
			if (KnownItemsByServer.ContainsKey(oldname))
			{
				string servername = (string)KnownItemsByServer[oldname];
				KnownItemsByServer.Add(newname,servername);
				Remove_Item_By_Server(oldname);
			}
		}

		public void Add_Item_By_Server(string name, string servername)
		{
			if (KnownItemsByServer.ContainsKey(name)==false)
			{
				KnownItemsByServer.Add(name,servername);
				//System.Diagnostics.Debug.WriteLine(" Add Server Lookup for  "+name+"  on "+ servername);
			}
		}
		
		public void Remove_Item_By_Server(string name)
		{
			if (KnownItemsByServer.ContainsKey(name))
			{
				KnownItemsByServer.Remove(name);
				//System.Diagnostics.Debug.WriteLine(" Remove Server Lookup for  "+name);
			}
		}

		#endregion Helper Methods For KnownItemsByServer Lookup

		#region Handling Software Objects 

		public void Add_Software(Node software_node)
		{
			string servertype = string.Empty;
			string servername = string.Empty;

			string name = software_node.GetAttribute("name");
			int zone_location = software_node.GetIntAttribute("proczone",1);
			//System.Diagnostics.Debug.WriteLine(" Software Node Added "+name + " ("+zone_location.ToString()+")");

			string type = software_node.GetAttribute("type");
			//handling the parent
			if (software_node.Parent != null)
			{
				servertype = software_node.Parent.GetAttribute("type");
				servername = software_node.Parent.GetAttribute("name");
				if (servertype.ToLower()=="server")
				{
					Add_Item_By_Server(name, servername);
				}
			}

			Add_Item_By_Zone(name, zone_location);
			if (KnownItemsNodeToNameLookup.ContainsKey(software_node)==false)
			{
			KnownItemsNodeToNameLookup.Add(software_node, name);
			}

			if (KnownSoftwareSlots.Contains(software_node) == false)
			{
				KnownSoftwareSlots.Add(software_node);
				software_node.AttributesChanged += software_node_AttributesChanged;
				software_node.Deleting += software_node_Deleting;
			}

//			if (type.ToLower()=="app")
//			{
//				KnownApps.Add(software_node);
//				software_node.AttributesChanged +=new Network.Node.AttributesChangedEventHandler(software_node_AttributesChanged);
//				software_node.Deleting +=new Network.Node.NodeDeletingEventHandler(software_node_Deleting);
//			}
//			if (type.ToLower()=="database")
//			{
//				KnownDatabases.Add(software_node);
//				software_node.AttributesChanged +=new Network.Node.AttributesChangedEventHandler(software_node_AttributesChanged);
//				software_node.Deleting +=new Network.Node.NodeDeletingEventHandler(software_node_Deleting);
//			}
		}

		public void Remove_Software(Node software_node)
		{
			string servername = string.Empty;
			string servertype = string.Empty;

			string name = software_node.GetAttribute("name");
			//System.Diagnostics.Debug.WriteLine(" Software Node Removed "+name);

			//handling the parent
			if (software_node.Parent != null)
			{
				servertype = software_node.Parent.GetAttribute("type");
				servername = software_node.Parent.GetAttribute("name");
				if (servertype.ToLower()=="server")
				{
					Remove_Item_By_Server(name);
				}
			}

			Remove_Item_By_Zone(name);
			KnownItemsNodeToNameLookup.Remove(software_node);

			KnownSoftwareSlots.Remove(software_node);
			software_node.AttributesChanged -=software_node_AttributesChanged;
			software_node.Deleting -=software_node_Deleting;
		}

		#endregion Handling Software Objects 

		#region Handling Server Objects 

		protected void AddServer(Node ServerNode)
		{
			string name = ServerNode.GetAttribute("name");
			//System.Diagnostics.Debug.WriteLine("Add Server   "+name);
			int zone_location = ServerNode.GetIntAttribute("proczone",1);
			//System.Diagnostics.Debug.WriteLine(" Server Added "+name + " ("+zone_location.ToString()+")");

			Add_Item_By_Zone(name, zone_location);

			if (KnownServers.Contains(ServerNode) == false)
			{
				KnownServers.Add(ServerNode);
				KnownItemsNodeToNameLookup.Add(ServerNode, name);
			}

			//Handle any Existing Children (ie Apps and databases)
			ArrayList ServerKids = ServerNode.getChildren();
			foreach (Node kid in ServerKids)
			{
				Add_Software(kid);
			}
			//Handle any Additions or Subtractions of Apps and the server attributes
			ServerNode.ChildAdded +=ServerNode_ChildAdded;
			ServerNode.ChildRemoved +=ServerNode_ChildRemoved;
			ServerNode.AttributesChanged +=ServerNode_AttributesChanged;
			ServerNode.Deleting +=ServerNode_Deleting;
		}

		public void RemoveServer(Node ServerNode)
		{
			string name = ServerNode.GetAttribute("name");
			//System.Diagnostics.Debug.WriteLine("Add Server   "+name);
			Remove_Item_By_Zone(name);
			KnownItemsNodeToNameLookup.Remove(ServerNode);
			KnownServers.Remove(ServerNode);
			//Handle any Existing Children (ie Apps and databases)
			ArrayList ServerKids = ServerNode.getChildren();
			foreach (Node kid in ServerKids)
			{
				Remove_Software(kid);
			}
			//Handle any Additions or Subtractions of Apps and the server attributes
			ServerNode.ChildAdded -=ServerNode_ChildAdded;
			ServerNode.ChildRemoved -=ServerNode_ChildRemoved;
			ServerNode.AttributesChanged -=ServerNode_AttributesChanged;
			ServerNode.Deleting -=ServerNode_Deleting;
		}

		#endregion Handling Server Objects 

		#region Monitoring Methods

		public void BuildMonitoring()
		{
			//System.Diagnostics.Debug.WriteLine("Build Monitor");

			ArrayList types = new ArrayList();
			Hashtable ht = new Hashtable();

			//handling the Router (Watching for any new Servers being added or removed)
			//Currently unlikely but in future, very possible
			types.Clear();
			types.Add("Router");
			ht = _model.GetNodesOfAttribTypes(types);
			foreach(Node RouterNode in ht.Keys)
			{
				string namestr = RouterNode.GetAttribute("name");
				//System.Diagnostics.Debug.WriteLine("Router Added: "+namestr);
				KnownRouters.Add(RouterNode);
				RouterNode.ChildAdded +=RouterNode_ChildAdded;
				RouterNode.ChildRemoved +=RouterNode_ChildRemoved;
			}

			//handling the Servers (Watching for any new Apps being added or removed)
			types.Clear();
			types.Add("Server");
			ht = _model.GetNodesOfAttribTypes(types);
			foreach(Node ServerNode in ht.Keys)
			{
				AddServer(ServerNode);
			}
		}
 
		public void DisposeMonitoring()
		{
			//System.Diagnostics.Debug.WriteLine("Dispose Monitor");

			if (KnownRouters.Count>0)
			{
				foreach (Node RouterNode in KnownRouters)
				{
					RouterNode.ChildAdded -=RouterNode_ChildAdded;
					RouterNode.ChildRemoved -=RouterNode_ChildRemoved;
				}
				KnownRouters.Clear();
			}
			if (KnownServers.Count>0)
			{
				foreach (Node ServerNode in KnownServers)
				{
					ServerNode.ChildAdded -=ServerNode_ChildAdded;
					ServerNode.ChildRemoved -=ServerNode_ChildRemoved;
					ServerNode.AttributesChanged -=ServerNode_AttributesChanged;
					ServerNode.Deleting -=ServerNode_Deleting;
				}
				KnownServers.Clear();
			}

			if (KnownSoftwareSlots.Count>0)
			{
				foreach (Node kid in KnownSoftwareSlots)
				{
					kid.AttributesChanged -=software_node_AttributesChanged;
					kid.Deleting -=software_node_Deleting;
				}
				KnownSoftwareSlots.Clear();
			}

//			if (KnownApps.Count>0)
//			{
//				foreach (Node kid in KnownApps)
//				{
//					kid.AttributesChanged -=new Network.Node.AttributesChangedEventHandler(software_node_AttributesChanged);
//					kid.Deleting -=new Network.Node.NodeDeletingEventHandler(software_node_Deleting);
//				}
//				KnownApps.Clear();
//			}
//			if (KnownDatabases.Count>0)
//			{
//				foreach (Node kid in KnownDatabases)
//				{
//					kid.AttributesChanged -=new Network.Node.AttributesChangedEventHandler(software_node_AttributesChanged);
//					kid.Deleting -=new Network.Node.NodeDeletingEventHandler(software_node_Deleting);
//				}
//				KnownDatabases.Clear();
//			}

			this._model = null;
			this._powerLevelNode = null;
			this.AppNodes_CapabilityCache.Clear();
			this.AppNodes_StatusCache.Clear();
			this.DatabaseNodes_CapabilityCache.Clear();
			this.DatabaseNodes_StatusCache.Clear();
			this.KnownItemsByServer.Clear();
			this.KnownItemsByZone.Clear();
			this.KnownItemsNodeToNameLookup.Clear();
			this.ServerNodes_CapabilityCache.Clear();
			this.ServerNodes_StatusCache.Clear();
		}


		#endregion Monitoring Methods

		/// <summary>
		/// We keep a cache of the starus of all the nodes 
		/// saving us having to recheck every node when a change comes in
		/// </summary>
		protected virtual void BuildCache()
		{
			//System.Diagnostics.Debug.WriteLine(" Build Cache");
			
			//Clear out all the previous cached values 
			ServerNodes_StatusCache.Clear();
			ServerNodes_CapabilityCache.Clear();
			AppNodes_StatusCache.Clear();
			AppNodes_CapabilityCache.Clear();
			DatabaseNodes_StatusCache.Clear();
			DatabaseNodes_CapabilityCache.Clear();

			if (KnownServers.Count>0)
			{
				foreach (Node ServerNode in KnownServers)
				{
					string Name = ServerNode.GetAttribute("name");
					string Status = ServerNode.GetAttribute("up");
					int procAbility = ServerNode.GetIntAttribute("proccap",0);
					Boolean procdown = ServerNode.GetBooleanAttribute("procdown",false);
					string full_status = determineStatus(Status, procdown);

					ServerNodes_StatusCache.Add(Name, full_status);
					ServerNodes_CapabilityCache.Add(Name, procAbility);
					//System.Diagnostics.Debug.WriteLine("BC    Server "+Name+" is "+Status+"  with "+procAbility.ToString());
				}
			}

			if (KnownSoftwareSlots.Count>0)
			{
				ArrayList all = new ArrayList();

				foreach (Node swNode in KnownSoftwareSlots)
				{
					string Name = swNode.GetAttribute("name");
					string Status = swNode.GetAttribute("up");
					string type = swNode.GetAttribute("type");
					int procAbility = swNode.GetIntAttribute("proccap",0);
					Boolean procdown = swNode.GetBooleanAttribute("procdown",false);
					string full_status = determineStatus(Status, procdown);
					string st = "BC  "+Name+" "+type+" is "+Status+"  with "+procAbility.ToString();
					
					all.Add(Name);

					if (type.ToLower()=="app")
					{
						//st += " =====";
						AppNodes_StatusCache.Add(Name, full_status);
						AppNodes_CapabilityCache.Add(Name, procAbility);					
					}

					if (type.ToLower()=="database")
					{
						//st += " ######";
						DatabaseNodes_StatusCache.Add(Name, full_status);
						DatabaseNodes_CapabilityCache.Add(Name, procAbility);
					}
					//System.Diagnostics.Debug.WriteLine(st);
				}
			}
		}

		protected virtual Boolean checkServerUp(string software_name)
		{
			Boolean serverup = false; //assuming it's down
			string servername = string.Empty;

			if (KnownItemsByServer.ContainsKey(software_name))
			{
				servername = (string) KnownItemsByServer[software_name];
				//check through existing servers 
				if (KnownServers.Count>0)
				{
					foreach (Node ServerNode in KnownServers)
					{
						string Name = ServerNode.GetAttribute("name");
						string status = (string) ServerNodes_StatusCache[Name];
						if (Name == servername)
						{
							if ((status.ToLower()== "true")|(status.ToLower()== ""))
							{
								serverup = true;
							}
						}
					}
				}
			}
			return serverup;
		}

		/// <summary>
		/// Build the Power Score for All the zones using the cached values 
		/// </summary>
		protected virtual void BuildPowerScore()
		{
			int[] TotalServerPower = new int[7];
			int[] TotalAppPower = new int[7];
			int[] TotalDatabasePower = new int[7];
			string[] DebugServerStr = new string[7];
			string[] DebugAppStr = new string[7];
			int zone = 1;
			int number_of_total_servers = 0;
			int number_of_active_servers = 0;
			int server_total_util = 0;

			for (int step=0; step < 7; step++)
			{
				TotalServerPower[step]=0;
				TotalAppPower[step]=0;
				TotalDatabasePower[step]=0;
				DebugServerStr = new string[7];
				DebugAppStr = new string[7];
			}

			if (KnownServers.Count>0)
			{
				foreach (Node ServerNode in KnownServers)
				{
					string Name = ServerNode.GetAttribute("name");
					string status = (string) ServerNodes_StatusCache[Name];
					int proc = (int) ServerNodes_CapabilityCache[Name];

					//handling the New Server Util values 
					number_of_total_servers++;
					server_total_util = server_total_util + ServerNode.GetIntAttribute("util",0);

					bool has_valid_software = false;
					foreach(Node child in ServerNode.getChildren())
					{
						string child_type = child.GetAttribute("type");
						if ((child_type.ToLower() == "app") | (child_type.ToLower() == "database"))
						{
							has_valid_software = true;
						}
					}
					if (has_valid_software == true)
					{
						number_of_active_servers++;
					}

					zone = 1;
					if (KnownItemsByZone.ContainsKey(Name))
					{
						zone = (int)KnownItemsByZone[Name];
						zone = zone-1;

						if ((status.ToLower()== "true")||(status.ToLower()== ""))
						{
							TotalServerPower[zone] += proc;
							DebugServerStr[zone] += " " + Name + "["+ proc.ToString() + "]";
						}
					}
					else
				  {
						//System.Diagnostics.Debug.WriteLine("=SYS POWER======="+Name);
				  } 
				}
			}

			//Set the Number of Active Servers 
			this._serverUtilLevelNode.SetAttribute("active_server_count", number_of_active_servers);
			
			//Set the Server Utilisation Value 
			//THIS IS NOW HARDCODED AT ROUND START AT REQUEST
			//double avg_server_util = 0;
			//if (number_of_total_servers>0)
			//{
			//	avg_server_util = (server_total_util) / number_of_total_servers;
			//}
			//this._serverUtilLevelNode.SetAttribute("overall_server_util_level", CONVERT.ToStrRounded(avg_server_util, 2));

			if (KnownSoftwareSlots.Count>0)
			{
				foreach (Node swNode in KnownSoftwareSlots)
				{
					string Name = swNode.GetAttribute("name");
					string type = swNode.GetAttribute("type");
					string status = string.Empty;
					int proc = 0;
					Boolean ConsiderThisNode = false;

					if (type.ToLower()=="app")
					{
						proc = (int) AppNodes_CapabilityCache[Name];
						status = (string) AppNodes_StatusCache[Name];
						ConsiderThisNode = true;
					}
					if (type.ToLower()=="database")
					{
						proc = (int) DatabaseNodes_CapabilityCache[Name];
						status = (string) DatabaseNodes_StatusCache[Name];
						ConsiderThisNode = true;
					}

					if (ConsiderThisNode)
					{
						zone = 1;
						if (KnownItemsByZone.ContainsKey(Name))
						{
							zone = (int)KnownItemsByZone[Name];
						}
						zone = zone-1;

						if (checkServerUp(Name)==true)
						{
							if ((status.ToLower()== "true")||(status.ToLower()== ""))
							{
								if (type.ToLower()=="app")
								{
									TotalAppPower[zone] += proc;
									DebugAppStr[zone] += " " + Name + "["+ proc.ToString() + "]";
								}
								if (type.ToLower()=="database")
								{
									TotalDatabasePower[zone] += proc;
									DebugAppStr[zone] += " " + Name + "["+ proc.ToString() + "]";
								}
							}
						}
					}
				}
			}

			for (int step = 0; step <7; step++)
			{
				//System.Diagnostics.Debug.WriteLine("Zone "+step.ToString()+" : "+ (TotalServerPower[step]).ToString()+"   " + DebugServerStr[step]);

				//int currentpower = TotalServerPower[step] + TotalAppPower[step] + TotalDatabasePower[step];
				int currentpowerNeeds = TotalAppPower[step] + TotalDatabasePower[step];
				//System.Diagnostics.Debug.WriteLine(" Zone "+step.ToString()+" : " + currentpowerNeeds.ToString() + "   "+ DebugAppStr[step]);
				UpdatePowerFactorForAppDBs(step, currentpowerNeeds);

				int currentpowerSupply = TotalServerPower[step];
				UpdatePowerFactorForServers(step, currentpowerSupply);
			}
		}

		protected virtual void UpdatePowerFactorForAppDBs(int zone, int powerFactor)
		{
			if (_powerLevelNode != null)
			{
				string tag_name = "z"+CONVERT.ToStr(zone+1)+"_now";
				string tag_power = CONVERT.ToStr(powerFactor);
				//System.Diagnostics.Debug.WriteLine(" ## "+tag_name + "  "+ tag_power);
				_powerLevelNode.SetAttribute(tag_name,tag_power);
			}
		}

		protected virtual void UpdatePowerFactorForServers(int zone, int powerFactor)
		{
			if (_powerLevelNode != null)
			{
				string tag_name = "z"+CONVERT.ToStr(zone+1)+"_base";
				string tag_power = CONVERT.ToStr(powerFactor);
				//System.Diagnostics.Debug.WriteLine(" ## "+tag_name + "  "+ tag_power);
				_powerLevelNode.SetAttribute(tag_name,tag_power);
			}
		}

		protected void ServerNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			Boolean rescore = false;

			foreach(AttributeValuePair avp in attrs)
			{
				if((avp.Attribute == "up")|(avp.Attribute == "procdown"))
				{
					string Name = sender.GetAttribute("name");
					string Status = sender.GetAttribute("up");
					Boolean procdown = sender.GetBooleanAttribute("procdown",false);
					string full_status = determineStatus(Status, procdown);
					
					if (ServerNodes_StatusCache.ContainsKey(Name))
					{
						ServerNodes_StatusCache.Remove(Name);
						ServerNodes_StatusCache.Add(Name, full_status);
						rescore = true;
					}
					//System.Diagnostics.Debug.WriteLine("  Server Up Changed ("+ Name+")  status "+full_status);
				}
				if(avp.Attribute == "proccap")
				{
					string Name = sender.GetAttribute("name");
					int procAbility = sender.GetIntAttribute("proccap",0);
					if (ServerNodes_StatusCache.ContainsKey(Name))
					{
						ServerNodes_CapabilityCache.Remove(Name);
						ServerNodes_CapabilityCache.Add(Name, procAbility);
						rescore = true;
					}
					//System.Diagnostics.Debug.WriteLine("  Server proc Changed ("+ Name+")");
				}
			}
			if (rescore)
			{
				BuildPowerScore();
			}
		}

		protected string determineStatus(string status, Boolean procdown)
		{
			string outcome="false";
			if (procdown==false)
			{
				if ((status.ToLower()=="true")|((status=="")))
				{
					outcome="true";
				}
			}	
			return outcome;
		}

		protected void software_node_AttributesChanged(Node sender, ArrayList attrs)
		{
			Boolean rescore = false;
			
			foreach(AttributeValuePair avp in attrs)
			{
				//=====================================================
				//==Handle the Node Going up or Down===================
				//=====================================================
				if((avp.Attribute == "up")|(avp.Attribute == "procdown"))
				{
					string type = sender.GetAttribute("type");
					if (type.ToLower()=="app")
					{
						string Name = sender.GetAttribute("name");
						string Status = sender.GetAttribute("up");
						Boolean procdown = sender.GetBooleanAttribute("procdown",false);
						string full_status = determineStatus(Status, procdown);

						if (AppNodes_StatusCache.ContainsKey(Name))
						{
							AppNodes_StatusCache.Remove(Name);
							AppNodes_StatusCache.Add(Name, full_status);
							rescore = true;
						}
						//System.Diagnostics.Debug.WriteLine("  SW Attrs Changed");
					}
					if (type.ToLower()=="database")
					{
						string Name = sender.GetAttribute("name");
						string Status = sender.GetAttribute("up");
						Boolean procdown = sender.GetBooleanAttribute("procdown",false);
						string full_status = determineStatus(Status, procdown);
					
						if (DatabaseNodes_StatusCache.ContainsKey(Name))
						{
							DatabaseNodes_StatusCache.Remove(Name);
							DatabaseNodes_StatusCache.Add(Name, full_status);
							rescore = true;
						}
						//System.Diagnostics.Debug.WriteLine("  SW Attrs Changed");
					}
				}
				//=====================================================
				//==Handle the Node Changing it's Processing Requirements
				//=====================================================
				if(avp.Attribute == "proccap")
				{
					string type = sender.GetAttribute("type");
					if (type.ToLower()=="app")
					{
						string Name = sender.GetAttribute("name");
						int procAbility = sender.GetIntAttribute("proccap",0);
						if (AppNodes_StatusCache.ContainsKey(Name))
						{
							AppNodes_CapabilityCache.Remove(Name);
							AppNodes_CapabilityCache.Add(Name, procAbility);
							rescore = true;
						}
						//System.Diagnostics.Debug.WriteLine("  SW Attrs Changed");
					}
					if (type.ToLower()=="database")
					{
						string Name = sender.GetAttribute("name");
						int procAbility = sender.GetIntAttribute("proccap",0);
						if (DatabaseNodes_StatusCache.ContainsKey(Name))
						{
							DatabaseNodes_CapabilityCache.Remove(Name);
							DatabaseNodes_CapabilityCache.Add(Name, procAbility);
							rescore = true;
						}
						//System.Diagnostics.Debug.WriteLine("  SW Attrs Changed");
					}
				}
				//=====================================================
				//==Handle the Node Changing it's Type (used when we install new software)
				//=====================================================
				if(avp.Attribute == "type")
				{
					string NameStr = sender.GetAttribute("name");
					string LocatStr = sender.GetAttribute("location");
					//System.Diagnostics.Debug.WriteLine("### Node Type Changed "+NameStr + "  "+LocatStr);
					string type_str = sender.GetAttribute("type");
					BuildCache();
					rescore = true;
				}
				//=====================================================
				//==Handle the Node Changing it's Name 
				//=====================================================
				if(avp.Attribute == "name")
				{
					string newname = sender.GetAttribute("name");
					string LocatStr = sender.GetAttribute("location");
					//System.Diagnostics.Debug.WriteLine("### Node Name Changed to "+newname + "  "+LocatStr);
					if (KnownItemsNodeToNameLookup.ContainsKey(sender))
					{
						string oldname = (string)KnownItemsNodeToNameLookup[sender];
						//System.Diagnostics.Debug.WriteLine("### Node Name Changed from "+oldname);
						Rename_Item_By_Zone(oldname, newname);
						Rename_Item_By_Server(oldname, newname);
						KnownItemsNodeToNameLookup.Remove(sender);
						KnownItemsNodeToNameLookup.Add(sender, newname);
					}
					BuildCache();
					rescore = true;
				}

			}
			if (rescore)
			{
				BuildPowerScore();
			}
		}

		protected void RouterNode_ChildAdded(Node sender, Node child)
		{
			if (child != null)
			{	
				string routername = sender.GetAttribute("name");
				string childname = child.GetAttribute("name");
				//only add if server
				string type = child.GetAttribute("type");
				if (type.ToLower()=="server")
				{
					//System.Diagnostics.Debug.WriteLine("Adding Router child  Router: "+ routername + " Child "+ childname);
					AddServer(child);
					BuildCache();
					BuildPowerScore();
				}
			}
		}

		protected void RouterNode_ChildRemoved(Node sender, Node child)
		{
			if (sender != null)
			{	
				string routername = sender.GetAttribute("name");
				string childname = child.GetAttribute("name");
				//System.Diagnostics.Debug.WriteLine("Deleting Router child  Router: "+ routername + " Child "+ childname);
				//only add if server
				string type = child.GetAttribute("type");
				if (type.ToLower()=="server")
				{
					//System.Diagnostics.Debug.WriteLine("Deleting Router child  Router: "+ routername + " Child "+ childname);
					RemoveServer(child);
					BuildCache();
					BuildPowerScore();
				}
			}
		}

		protected void software_node_Deleting(Node sender)
		{
			if (sender != null)
			{	
				string name = sender.GetAttribute("name");
				//System.Diagnostics.Debug.WriteLine("Deleting Software Item : "+name);

				Remove_Software(sender);
				BuildCache();
				BuildPowerScore();
			}
		}

		protected void ServerNode_Deleting(Node sender)
		{
			if (sender != null)
			{	
				string name = sender.GetAttribute("name");
				//System.Diagnostics.Debug.WriteLine("Deleting Server Item : "+name);

				this.RemoveServer(sender);
				BuildCache();
				BuildPowerScore();
			}
		}

		protected void ServerNode_ChildAdded(Node sender, Node child)
		{
			if (child != null)
			{	
				string servername = sender.GetAttribute("name");
				string childname = child.GetAttribute("name");
				//System.Diagnostics.Debug.WriteLine("Adding Server child  Server: "+ servername + " Child "+ childname);

				Add_Software(child);
				BuildCache();
				BuildPowerScore();
			}
		}

		protected void ServerNode_ChildRemoved(Node sender, Node child)
		{
			if (child != null)
			{	
				string routername = sender.GetAttribute("name");
				string childname = child.GetAttribute("name");
				//System.Diagnostics.Debug.WriteLine("Deleting Server child  Router: "+ routername + " Child "+ childname);

				Remove_Software(child);
				BuildCache();
				BuildPowerScore();
			}
		}

	}
}
