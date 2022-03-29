using System;
using System.Collections;
using System.Xml;

using LibCore;
using Network;

namespace IncidentManagement
{
	/// <summary>
	/// This class watch the Server Upgrade Queue
	/// It operates in Both Operation Phase and Transition Phase
	/// It receives Time Information (either Day Ticks or Second Ticks)
	/// It will dispatch Upgrades as immediate incidents when the time reaches the required point 
	///   It Reads the server hardware definitions from the Application Upgrade XML Definition File.
	/// In transition Pahse, It understands handling the calendar events
	///   so that we can mark the Calender as failed or Successfull
	/// </summary>
	public class MachineUpgrader
	{
		NodeTree _model;
		Node upgradeQueue;
		Node calendarNode;
		Node currentDayNode;
		Node currentTimeNode;

		Hashtable serverToRequirements = new Hashtable();
		Hashtable serverToIncidentDef = new Hashtable();
		Hashtable DayToUpGrade = new Hashtable();

		Boolean MyTransitionMode = false;
		Hashtable PendingMemoryUpgradeTimeByName = new Hashtable();
		Hashtable PendingMemoryUpgradeNodeByName = new Hashtable();
		Hashtable PendingStorageUpgradeTimeByName = new Hashtable();
		Hashtable PendingStorageUpgradeNodeByName = new Hashtable();
		Hashtable PendingHwareUpgradeTimeByName = new Hashtable();
		Hashtable PendingHwareUpgradeNodeByName = new Hashtable();
		Hashtable PendingFirmwareUpgradeTimeByName = new Hashtable();
		Hashtable PendingFirmwareUpgradeNodeByName = new Hashtable();
		Hashtable PendingProcUpgradeTimeByName = new Hashtable();
		Hashtable PendingProcUpgradeNodeByName = new Hashtable();

		public MachineUpgrader(NodeTree model, Boolean TransitionMode)
		{
			_model = model;

			LoadDefs(model);

			upgradeQueue = model.GetNamedNode("MachineUpgradeQueue");
			upgradeQueue.ChildAdded += upgradeQueue_ChildAdded;
			upgradeQueue.ChildRemoved +=upgradeQueue_ChildRemoved;
			
			calendarNode = _model.GetNamedNode("Calendar");
			
			MyTransitionMode = TransitionMode;
			if (MyTransitionMode)
			{
				//In transition we count in Days
				currentDayNode = _model.GetNamedNode("CurrentDay");
				//CurrentDay = currentDayNode.GetIntAttribute("day",0);
				currentDayNode.AttributesChanged += TimeNode_AttributesChanged;
			}
			else
			{
				//In operation we count in Seconds
				currentTimeNode = _model.GetNamedNode("CurrentTime");
				//CurrentSecond = currentTimeNode.GetIntAttribute("seconds",0);
				currentTimeNode.AttributesChanged += TimeNode_AttributesChanged;
			}
		}
	
		/// <summary>
		///	Dispose ...
		/// </summary>
		public void Dispose()
		{
			if (this.MyTransitionMode)
			{
				currentDayNode.AttributesChanged -= TimeNode_AttributesChanged;
			}
			else
			{
				currentTimeNode.AttributesChanged -= TimeNode_AttributesChanged;
			}
			upgradeQueue.ChildAdded -= upgradeQueue_ChildAdded;
			upgradeQueue.ChildRemoved -=upgradeQueue_ChildRemoved;
		}

		/// <summary>
		/// Loading the definitions from the XML files
		/// </summary>
		/// <param name="model"></param>
		protected void LoadDefs(NodeTree model)
		{
			// Load the ServerUpgradeDefs.xml file...
			string upgradeFile = AppInfo.TheInstance.Location + "\\data\\ServerUgradeDefs.xml";
			XmlDocument xdoc = new XmlDocument();
			System.IO.StreamReader file = new System.IO.StreamReader(upgradeFile);
			string xmldata = file.ReadToEnd();
			file.Close();
			file = null;
			//
			xdoc.LoadXml(xmldata);
			//
			foreach(XmlNode xnode in xdoc.DocumentElement.ChildNodes)
			{
				if(xnode.NodeType == XmlNodeType.Element)
				{
					if(xnode.Name == "upgrade")
					{
						XmlAttribute att = xnode.Attributes["server"];

						XmlNode req = xnode.SelectSingleNode("requirements");
						serverToRequirements.Add(att.Value, req.OuterXml);

						XmlNode idef = xnode.SelectSingleNode("i");
						serverToIncidentDef.Add(att.Value, new IncidentDefinition(idef,model));
					}
				}
			}
		}

		protected void OutputError(string errorText)
		{
			Node errorsNode = _model.GetNamedNode("FacilitatorNotifiedErrors");
			Node error = new Node(errorsNode, "error", "", new AttributeValuePair( "text", errorText ) );
		}

		#region Calender Methods 

		protected void CreateCalendarRecord(string ServerName, string option, int DayValue)
		{
			ArrayList attrs = new ArrayList();
			attrs.Add( new AttributeValuePair("block","true") );
			attrs.Add( new AttributeValuePair("day",DayValue) );
			attrs.Add( new AttributeValuePair("showName",ServerName) );
			attrs.Add( new AttributeValuePair("type","server_upgrade") );
			attrs.Add( new AttributeValuePair("upgrade_option",option) );
			attrs.Add( new AttributeValuePair("status","active") );
			attrs.Add( new AttributeValuePair("target",ServerName));
			Node newEvent = new Node(calendarNode, "server_upgrade", "", attrs);
		}

		protected void AlterCalendarRecord(string AppName, string option, Boolean Success)
		{
			ArrayList delNodes = new ArrayList();
			foreach(Node n2 in calendarNode.getChildren())
			{
				string eventType = n2.GetAttribute("type");
				string eventTarget = n2.GetAttribute("target");
				string eventOption = n2.GetAttribute("upgrade_option");
				if (eventType == "server_upgrade")
				{
					if (eventOption == option)
					{
						if (eventTarget.ToLower() == AppName.ToLower())
						{
							if (Success)
							{
								n2.SetAttribute("status", "completed_ok");
							}
							else
							{
								n2.SetAttribute("status", "completed_fail");
							}
						}
					}
				}
			}
		}

		protected void RemoveCalendarRecord(string AppName, string Option)
		{
			//we only remove the calendaer record if it was still pending
			Node Node_Found = null;
			foreach(Node n2 in calendarNode.getChildren())
			{
				string eventType = n2.GetAttribute("type");
				string eventTarget = n2.GetAttribute("target");
				string eventOption = n2.GetAttribute("upgrade_option");
				string status = n2.GetAttribute("status");
				if (status.ToLower() == "active")
				{
					if (eventType == "server_upgrade")
					{
						if (eventOption == Option)
						{
							if (eventTarget.ToLower() == AppName.ToLower())
							{
								Node_Found = n2;
							}
						}
					}
				}
			}
			if (Node_Found != null)
			{
				Node_Found.Parent.DeleteChildTree(Node_Found);
			}
		}

		#endregion Calendar Methods

		protected void DoUpgrade(string server, string option)
		{
			Node serverNode = _model.GetNamedNode(server);
			string server_name = serverNode.GetAttribute("name");

			Node costedEvents = _model.GetNamedNode("CostedEvents");

			string incident = serverNode.GetAttribute("incident_id");
			if (incident != "")
			{
				ArrayList attributes = new ArrayList ();
				attributes.Add(new AttributeValuePair ("incident_id", incident));
				attributes.Add(new AttributeValuePair ("type", "upgradeclearincident"));
				attributes.Add(new AttributeValuePair ("desc", "upgrade clearing incident"));
				new Node (costedEvents, "node", "", attributes);
			}

			if(option== "hardware")
			{
				//hardware is XML File
				string reason;
				string short_reason;

				if(!RequirementsChecker.AreRequirementsMet((string) serverToRequirements[server], _model, out reason, out short_reason))
				{
					if (MyTransitionMode)
					{
						//Handle the Calender Event going red
						AlterCalendarRecord(server, option, false);
					}
					OutputError("Upgrade "+option+"on server ("+server+") failed because "+ reason);
					return;
				}
				else
				{
					AlterCalendarRecord(server, option, true);
					IncidentDefinition idef = (IncidentDefinition) serverToIncidentDef[server];
					idef.ApplyAction(_model);
				}
			}
			else if (option == "firmware")
			{
				AlterCalendarRecord(server, option, false);
				OutputError("Firmware Upgrade Not Implemented");
			}
			else
			{
				bool upgradeMemory = false;
				bool upgradeStorage = false;
				bool upgradeProcessor = false;
				
				//Building the Server Memory or Storage upgrade by Hand 
				// Standard Storage Upgrade is +100 GB disk AND Standard Memory Upgrade is +2 GB memory
				Boolean ServerCanUpMem = serverNode.GetBooleanAttribute("can_upgrade_mem",false);
				Boolean ServerCanUpDisk = serverNode.GetBooleanAttribute("can_upgrade_disk",false);
				Boolean ServerCanUpProcessor = serverNode.GetBooleanAttribute("can_upgrade_proc", false);
				Boolean proceed = true;

				//check that we can upgrade 
				//Currenlty everthing should check out
				if (option == "memory")
				{
					if (ServerCanUpMem==false)
					{
						proceed = false;
						if (MyTransitionMode)
						{
							AlterCalendarRecord(server, option, false);
						}
					}
				}
				else if (option == "storage")
				{
					if (ServerCanUpDisk==false)
					{
						proceed = false;
						if (MyTransitionMode)
						{
							AlterCalendarRecord(server, option, false);
						}
					}
				}
				else if (option == "processor")
				{
					if (ServerCanUpProcessor == false)
					{
						proceed = false;
						if (MyTransitionMode)
						{
							AlterCalendarRecord(server, option, false);
						}
					}
				}
				else if (option == "hardware")
				{
				}
				else if (option == "firmware")
				{
				}
				//		
				if (proceed)
				{
					//Created New Costed Events 

					if ((option == "memory")|(option == "both"))
					{
						upgradeMemory = true;
						//add the upgrade to the costed events 
						ArrayList cost_attrs = new ArrayList();
						cost_attrs.Add( new AttributeValuePair("ref",server_name) );
						cost_attrs.Add( new AttributeValuePair("type","memoryupgrade") );
						cost_attrs.Add( new AttributeValuePair("desc","storage upgraded") );
						Node newCost = new Node(costedEvents, "node", "", cost_attrs);
					}
					//
					if ((option == "storage")|(option == "both"))
					{
						upgradeStorage = true;
						//add the upgrade to the costed events 
						ArrayList cost_attrs = new ArrayList();
						cost_attrs.Add( new AttributeValuePair("ref",server_name) );
						cost_attrs.Add( new AttributeValuePair("type","storageupgrade") );
						cost_attrs.Add( new AttributeValuePair("desc","storage upgraded") );
						Node newCost = new Node(costedEvents, "node", "", cost_attrs);
					}

					if ((option == "processor"))
					{
						upgradeProcessor = true;
						//add the upgrade to the costed events 
						ArrayList cost_attrs = new ArrayList();
						cost_attrs.Add(new AttributeValuePair("ref", server_name));
						cost_attrs.Add(new AttributeValuePair("type", "processorupgrade"));
						cost_attrs.Add(new AttributeValuePair("desc", "processor upgraded"));
						Node newCost = new Node(costedEvents, "node", "", cost_attrs);
					}

					//Now Upgrade the server
					ArrayList attrs = new ArrayList();
					bool reboot = false;
					//
					if(upgradeStorage)
					{
						//Increase the Disk
						int disk = serverNode.GetIntAttribute("disk",0) + CoreUtils.SkinningDefs.TheInstance.GetIntData("server_disk_upgrade", 100);   // Disk is in GBs
						attrs.Add( new AttributeValuePair("disk", disk ) );
						//Increase the Disk upgrade count
						int upgrade_count = serverNode.GetIntAttribute("count_disk_upgrades",0);
						upgrade_count = upgrade_count +1;
						attrs.Add( new AttributeValuePair("count_disk_upgrades", CONVERT.ToStr(upgrade_count)));
						//Don't stop any future upgrades
						//attrs.Add( new AttributeValuePair("can_upgrade_disk", "false" ) );
						reboot = true;

						//No reboot if it's a router 
						string nodetype = serverNode.GetAttribute("type");
						if (nodetype.ToLower() == "router")
						{
							reboot = false;
						}
					}
					//
					if(upgradeMemory)
					{
						int memory = serverNode.GetIntAttribute("mem", 0) + CoreUtils.SkinningDefs.TheInstance.GetIntData("server_memory_upgrade", 2000); // Memory is in MBs
						attrs.Add( new AttributeValuePair("mem", memory ) );
						//Increase the Disk upgrade count
						int upgrade_count = serverNode.GetIntAttribute("count_mem_upgrades",0);
						upgrade_count = upgrade_count + 1;
						attrs.Add( new AttributeValuePair("count_mem_upgrades", CONVERT.ToStr(upgrade_count)));
						//Don't stop any future upgrades
						//attrs.Add( new AttributeValuePair("can_upgrade_mem", "false" ) );
						reboot = true;
					}

					if (upgradeProcessor)
					{
						int proc_value = serverNode.GetIntAttribute("proccap", 0) + 1; // Processor Steps
						attrs.Add(new AttributeValuePair("proccap", proc_value));
						//Increase the Disk upgrade count
						int upgrade_count = serverNode.GetIntAttribute("count_proc_upgrades", 0);
						upgrade_count = upgrade_count + 1;
						attrs.Add(new AttributeValuePair("count_proc_upgrades", CONVERT.ToStr(upgrade_count)));
						reboot = true;
					}

					//
					if(reboot)
					{
						attrs.Add( new AttributeValuePair("rebootFor", CoreUtils.SkinningDefs.TheInstance.GetData("reboot_time") ) );
					}
					//
					serverNode.SetAttributes(attrs);
					if (MyTransitionMode)
					{
						AlterCalendarRecord(server, option, true);
					}
				}
			}
			//Remove from the Queue
			Node qn = null;
			switch (option)
			{
				case "memory":
					qn = (Node) PendingMemoryUpgradeNodeByName[server];
					break;
				case "storage":
					qn = (Node) PendingStorageUpgradeNodeByName[server];
					break;
				case "hardware":
					qn = (Node) PendingHwareUpgradeNodeByName[server];
					break;
				case "processor":
					qn = (Node)PendingProcUpgradeNodeByName[server];
					break;
				case "firmware":
					qn = (Node) PendingFirmwareUpgradeNodeByName[server];
					break;
			}
			if (qn != null)
			{
				qn.Parent.DeleteChildTree(qn);
			}
			//Remove from the Hashtables
			Remove(server, option);
		}

		void Remove(string servername, string option)
		{
			switch (option)
			{
				case "memory":
					PendingMemoryUpgradeTimeByName.Remove(servername);
					PendingMemoryUpgradeNodeByName.Remove(servername);
					break;
				case "storage":
					PendingStorageUpgradeTimeByName.Remove(servername);
					PendingStorageUpgradeNodeByName.Remove(servername);
					break;
				case "hardware":
					PendingHwareUpgradeTimeByName.Remove(servername);
					PendingHwareUpgradeNodeByName.Remove(servername);
					break;
				case "firmware":
					PendingFirmwareUpgradeTimeByName.Remove(servername);
					PendingFirmwareUpgradeNodeByName.Remove(servername);
					break;
				case "processor":
					PendingProcUpgradeTimeByName.Remove(servername);
					PendingProcUpgradeNodeByName.Remove(servername);
					break;
			}
		}

		void HandleNewServerUpgrade(Node sender, Node child, int WhenValue)
		{
			string pendingServerType = child.GetAttribute("type");
			string pendingServerName = child.GetAttribute("target");
			string pendingServerOption = child.GetAttribute("upgrade_option");
			int pendingServerTime = child.GetIntAttribute("when",0);

			switch (pendingServerOption)
			{
				case "memory":
					PendingMemoryUpgradeTimeByName.Add(pendingServerName,pendingServerTime);
					PendingMemoryUpgradeNodeByName.Add(pendingServerName,child);
					break;
				case "storage":
					PendingStorageUpgradeTimeByName.Add(pendingServerName,pendingServerTime);
					PendingStorageUpgradeNodeByName.Add(pendingServerName,child);
					break;
				case "hardware":
					PendingHwareUpgradeTimeByName.Add(pendingServerName,pendingServerTime);
					PendingHwareUpgradeNodeByName.Add(pendingServerName,child);
					break;
				case "firmware":
					PendingFirmwareUpgradeTimeByName.Add(pendingServerName,pendingServerTime);
					PendingFirmwareUpgradeNodeByName.Add(pendingServerName,child);
					break;
				case "processor":
					PendingProcUpgradeTimeByName.Add(pendingServerName, pendingServerTime);
					PendingProcUpgradeNodeByName.Add(pendingServerName, child);
					break;
			}
			if (MyTransitionMode)
			{
				CreateCalendarRecord(pendingServerName,pendingServerOption, pendingServerTime);
			}
			if (pendingServerTime<= WhenValue)
			{
				DoUpgrade(pendingServerName,pendingServerOption);
			}
		}

		void upgradeQueue_ChildAdded(Node sender, Node child)
		{
			int CurrentWhen =0;
			//Handling an new Upgrade 
			if (MyTransitionMode)
			{
				CurrentWhen = currentDayNode.GetIntAttribute("day",0);
			}
			else
			{
				CurrentWhen = currentTimeNode.GetIntAttribute("seconds",0);
			}
			HandleNewServerUpgrade(sender, child, CurrentWhen);
		}

		void upgradeQueue_ChildRemoved(Node sender, Node child)
		{
			string ServerNameToBeCancelled = child.GetAttribute("target");
			string pendingServerOption = child.GetAttribute("upgrade_option");

			Remove(ServerNameToBeCancelled, pendingServerOption);
			RemoveCalendarRecord(ServerNameToBeCancelled,pendingServerOption);
		}

		void HandleTimeChanged(Node sender, ArrayList attrs, int CurrentWhen)
		{
			//Scan throughtb the different collections to build a list of what needs to be done
			//since doing the upgrades affects the hashtable that you are stepping through
			Hashtable appsToUpgrade = new Hashtable();
			Hashtable appsToUpgradeMemory = new Hashtable();
			Hashtable appsToUpgradeStorage = new Hashtable();
			Hashtable appsToUpgradeHardware = new Hashtable();
			Hashtable appsToUpgradeFirmware = new Hashtable();
			Hashtable appsToUpgradeProcessor = new Hashtable();

			foreach (string servername in PendingMemoryUpgradeTimeByName.Keys)
			{
				int when_value = (int)PendingMemoryUpgradeTimeByName[servername];
				if (when_value <= CurrentWhen)
				{
					appsToUpgradeMemory.Add(servername,"memory");
				}
			}
			foreach (string servername in PendingStorageUpgradeTimeByName.Keys)
			{
				int when_value = (int)PendingStorageUpgradeTimeByName[servername];
				if (when_value <= CurrentWhen)
				{
					appsToUpgradeStorage.Add(servername,"storage");
				}
			}
			foreach (string servername in PendingHwareUpgradeTimeByName.Keys)
			{
				int when_value = (int)PendingHwareUpgradeTimeByName[servername];
				if (when_value <= CurrentWhen)
				{
					appsToUpgradeHardware.Add(servername,"hardware");
				}
			}
			foreach (string servername in PendingFirmwareUpgradeTimeByName.Keys)
			{
				int when_value = (int)PendingFirmwareUpgradeTimeByName[servername];
				if (when_value <= CurrentWhen)
				{
					appsToUpgradeHardware.Add(servername,"firmware");
				}
			}
			foreach (string servername in PendingProcUpgradeTimeByName.Keys)
			{
				int when_value = (int)PendingProcUpgradeTimeByName[servername];
				if (when_value <= CurrentWhen)
				{
					appsToUpgradeHardware.Add(servername, "processor");
				}
			}

			if (appsToUpgradeMemory.Count>0)
			{
				foreach (string servername in appsToUpgradeMemory.Keys)
				{
					string option = (string)appsToUpgradeMemory[servername];
					DoUpgrade(servername,option);
				}
			}

			if (appsToUpgradeStorage.Count>0)
			{
				foreach (string servername in appsToUpgradeStorage.Keys)
				{
					string option = (string)appsToUpgradeStorage[servername];
					DoUpgrade(servername,option);
				}
			}

			if (appsToUpgradeHardware.Count>0)
			{
				foreach (string servername in appsToUpgradeHardware.Keys)
				{
					string option = (string)appsToUpgradeHardware[servername];
					DoUpgrade(servername,option);
				}
			}
			if (appsToUpgradeFirmware.Count>0)
			{
				foreach (string servername in appsToUpgradeFirmware.Keys)
				{
					string option = (string)appsToUpgradeFirmware[servername];
					DoUpgrade(servername,option);
				}
			}
			if (appsToUpgradeProcessor.Count > 0)
			{
				foreach (string servername in appsToUpgradeProcessor.Keys)
				{
					string option = (string)appsToUpgradeProcessor[servername];
					DoUpgrade(servername, option);
				}
			}
		}

		void TimeNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			int CurrentWhen = 0;
			if (MyTransitionMode)
			{
				//working in Days 
				CurrentWhen = currentDayNode.GetIntAttribute("day",0);
			}
			else
			{
				//working in Seconds
				CurrentWhen = currentTimeNode.GetIntAttribute("seconds",0);
			}
			HandleTimeChanged(sender, attrs, CurrentWhen);
		}

	}
}
