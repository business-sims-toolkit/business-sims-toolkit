using System;
using System.Collections;
using System.Xml;

using LibCore;
using Network;
using CoreUtils;

namespace IncidentManagement
{
	/// <summary>
	/// This class watch the Application Upgrade Queue
	/// It operates in both Operation Phase and Transition Phase
	/// It receives Time Information (either Day Ticks or Second Ticks)
	/// It will dispatch Upgrades as immediate incident when the time reaches the required point 
	///   It Reads the incident definitions from the Application Upgrade XML Definition File.
	/// In transition Phase, It understands handling the calendar events
	///   so that we can mark the Calender as failed or Successfull
	/// </summary>
	public class AppUpgrader
	{
		NodeTree _model;
		Node upgradeQueue;
		Node calendarNode;
		Node currentDayNode;
		Node currentTimeNode;

		int CurrentDay =0;
		int CurrentSecond =0;

		Hashtable appToRequirements = new Hashtable();
		Hashtable appToIncidentDef = new Hashtable();
		Hashtable DayToUpGrade = new Hashtable();

		Boolean MyTransitionMode = false;

		Hashtable PendingUpgradeNodesByType = new Hashtable ();
		Hashtable PendingUpgradeTimesByType = new Hashtable ();

		bool FirmwareDangerLevelReset = false;

		public AppUpgrader(NodeTree model, Boolean TransitionMode)
		{
			_model = model;
			upgradeQueue = model.GetNamedNode("AppUpgradeQueue");
			calendarNode = _model.GetNamedNode("Calendar");

			//Some Apps want to reset the danger level when resetting the firmware 
			FirmwareDangerLevelReset  = SkinningDefs.TheInstance.GetBoolData("firmware_upgrade_reset_danger_level", false);
			//
			LoadDefs(model);
			//
			upgradeQueue.ChildAdded += upgradeQueue_ChildAdded;
			upgradeQueue.ChildRemoved +=upgradeQueue_ChildRemoved;

			MyTransitionMode = TransitionMode;
			if (MyTransitionMode)
			{
				//In transition we count in Days
				currentDayNode = _model.GetNamedNode("CurrentDay");
				CurrentDay = currentDayNode.GetIntAttribute("day",0);
				//
				currentDayNode.AttributesChanged += TimeNode_AttributesChanged;
			}
			else
			{
				//In operation we count in Seconds
				currentTimeNode = _model.GetNamedNode("CurrentTime");
				CurrentSecond = currentTimeNode.GetIntAttribute("seconds",0);
				currentTimeNode.AttributesChanged += TimeNode_AttributesChanged;
			}
		}

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

			appToIncidentDef.Clear();
		}

		protected void LoadDefs(NodeTree model)
		{
			// Load the AppUpgradeDefs.xml file...
			string upgradeFile = AppInfo.TheInstance.Location + "\\data\\AppUgradeDefs.xml";
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
						XmlAttribute att = xnode.Attributes["app"];

						XmlNode req = xnode.SelectSingleNode("requirements");
						appToRequirements.Add(att.Value, req.OuterXml);

						XmlNode idef = xnode.SelectSingleNode("i");
						appToIncidentDef.Add(att.Value, new IncidentDefinition(idef,model));
					}
				}
			}
		}

		protected void OutputError(string errorText)
		{
			Node errorsNode = _model.GetNamedNode("FacilitatorNotifiedErrors");
			Node error = new Node(errorsNode, "error", "", new AttributeValuePair( "text", errorText ) );
		}

		protected void CreateCalendarRecord(string AppName, int DayValue)
		{
			ArrayList attrs = new ArrayList();
			attrs.Add( new AttributeValuePair("block","true") );
			attrs.Add( new AttributeValuePair("day",DayValue) );
			attrs.Add( new AttributeValuePair("showName",AppName) );
			attrs.Add( new AttributeValuePair("type","app_upgrade") );
			attrs.Add( new AttributeValuePair("status","active") );
			attrs.Add( new AttributeValuePair("target",AppName));
			Node newEvent = new Node(calendarNode, "app_upgrade", "", attrs);
		}

		protected void AlterCalendarRecord(string AppName, Boolean Success)
		{
			ArrayList delNodes = new ArrayList();
			foreach(Node n2 in calendarNode.getChildren())
			{
				string eventType = n2.GetAttribute("type");
				string eventTarget = n2.GetAttribute("target");
				if (eventType == "app_upgrade")
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

		void RemoveCalendarRecord(string AppName)
		{
			//we only remove the calendaer record if it was still pending
			Node Node_Found = null;
			foreach(Node n2 in calendarNode.getChildren())
			{
				string eventType = n2.GetAttribute("type");
				string eventTarget = n2.GetAttribute("target");
				string status = n2.GetAttribute("status");
				if (status.ToLower() == "active")
				{
					if (eventType == "app_upgrade")
					{
						if (eventTarget.ToLower() == AppName.ToLower())
						{
							Node_Found = n2;
						}
					}
				}
			}
			if (Node_Found != null)
			{
				Node_Found.Parent.DeleteChildTree(Node_Found);
			}
		}

		public static bool IsVersionGreaterEqual (string a, string b)
		{
			return (CONVERT.ParseDouble(a) >= CONVERT.ParseDouble(b));
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="app"></param>
		protected void DoUpgrade(string app, string type)
		{
			string reason;
			string short_reason;

			bool cancelled = false;

			Node appNode = _model.GetNamedNode(app);

			if (type == "firmware")
			{
				Node serverNode = appNode.Parent;

				string serverVersion = serverNode.GetAttribute("firmware");
				string appVersion = appNode.GetAttribute("firmware");

				if (IsVersionGreaterEqual(appVersion, serverVersion))
				{
					OutputError("Upgrade Firmware ("+app+") failed because it is already updated");
					cancelled = true;
				}
				else
				{
					Node costedEvents = _model.GetNamedNode("CostedEvents");
					ArrayList attributes = new ArrayList ();

					string incident = appNode.GetAttribute("incident_id");
					if (incident != "")
					{
						attributes.Clear();
						attributes.Add(new AttributeValuePair ("incident_id", incident));
						attributes.Add(new AttributeValuePair ("type", "upgradeclearincident"));
						attributes.Add(new AttributeValuePair ("desc", "upgrade clearing incident"));
						new Node (costedEvents, "node", "", attributes);
					}

					attributes.Clear();
					attributes.Add(new AttributeValuePair("firmware", serverVersion));
					attributes.Add(new AttributeValuePair("can_upgrade_firmware", "false"));
					attributes.Add(new AttributeValuePair("rebootingForSecs", "60"));
					attributes.Add(new AttributeValuePair ("rebootFor", "60"));

					if (FirmwareDangerLevelReset)
					{
						int danger_level = appNode.GetIntAttribute("danger_level",0);
						if (danger_level != 20)
						{
							attributes.Add(new AttributeValuePair ("danger_level",CONVERT.ToStr(20)));
						}
					}
					appNode.SetAttributes(attributes);

					attributes.Clear();
					attributes.Add(new AttributeValuePair ("ref", app));
					attributes.Add(new AttributeValuePair ("type", "firmwareupgrade"));
					attributes.Add(new AttributeValuePair ("desc", "firmware upgraded"));
					Node newCost = new Node (costedEvents, "node", "", attributes);
				}
			}
			else
			{
				if(!RequirementsChecker.AreRequirementsMet((string) appToRequirements[app], _model, out reason, out short_reason))
				{
					if (MyTransitionMode)
					{
						//Handle the Calender Event going red
						AlterCalendarRecord(app, false);
					}
					//Handle the error Message

					string errorMessage = "Upgrade app " + app + " failed because " + reason;
					if (reason.Contains("[COMPLETE]"))
					{
						errorMessage = reason.Replace("[COMPLETE]", "").Replace("[APP]", app);
					}

					OutputError(errorMessage);
					cancelled = true;
				}
				else
				{
					Node costedEvents = _model.GetNamedNode("CostedEvents");
					ArrayList attributes = new ArrayList ();

					// If there's an incident that this upgrade will clear, then make sure that
					// gets recorded so that MTRS etc are reported correctly (bugs 6090, 6208).
					if (appNode != null)
					{
						string incident = appNode.GetAttribute("incident_id");
						if (incident != "")
						{
							attributes.Clear();
							attributes.Add(new AttributeValuePair ("incident_id", incident));
							attributes.Add(new AttributeValuePair ("type", "upgradeclearincident"));
							attributes.Add(new AttributeValuePair ("desc", "upgrade clearing incident"));
							new Node (costedEvents, "node", "", attributes);
						}
					}

					if (MyTransitionMode)
					{
						//Handle the Calender Event going Green
						AlterCalendarRecord(app, true);
					}
					//Handle the immediate application of the upgrade 
					IncidentDefinition idef = (IncidentDefinition) appToIncidentDef[app];
					idef.ApplyAction(_model);
				}
				//

				if (! cancelled)
				{
					// Case 4385:   ALL: install from workaround doesn't clear incident 
					// wipe workarounds and any incident number on the node at the same time.
					// Also refactored so that all attribute changes are sent at the same time.
					// Find the app node and wipe bad atributes...
					Node app_node = this._model.GetNamedNode(app);
					if(app_node != null)
					{
						ArrayList att = new ArrayList();
						att.Add( new AttributeValuePair("goingDown","") );
						att.Add( new AttributeValuePair("workingaround","") );
						att.Add( new AttributeValuePair("incident_id","") );

						if (CoreUtils.SkinningDefs.TheInstance.GetIntData("upgrade_app_dont_clear_reboot", 0) != 1)
						{
							att.Add(new AttributeValuePair("rebootingForSecs", ""));
							att.Add(new AttributeValuePair("up", "true"));
						}

						att.Add( new AttributeValuePair("reasondown","") );
						app_node.SetAttributes(att);
					}
				}
			}

			//Remove from the Queue
			Hashtable nodesTable = (Hashtable) PendingUpgradeNodesByType[type];
			Hashtable timesTable = (Hashtable) PendingUpgradeTimesByType[type];
			Node qn = (Node) nodesTable[app];
			qn.Parent.DeleteChildTree(qn);

			nodesTable.Remove(app);
			timesTable.Remove(app);
		}

		void HandleNewUpgradeOnDay(Node sender, Node child)
		{
			// Get the current day and current time in case we have a delayed upgrade.
			int CurrentDay = currentDayNode.GetIntAttribute("day",0);
			string pendingAppName = child.GetAttribute("appname");
			int pendingAppTime = child.GetIntAttribute("when",0);
			string type = child.GetAttribute("upgrade_option");

			//Add into Pending app list 

			if (! PendingUpgradeNodesByType.ContainsKey(type))
			{
				PendingUpgradeNodesByType.Add(type, new Hashtable ());
			}
			Hashtable PendingUpgradeNodeByName = (Hashtable) PendingUpgradeNodesByType[type];

			if (! PendingUpgradeTimesByType.ContainsKey(type))
			{
				PendingUpgradeTimesByType.Add(type, new Hashtable ());
			}
			Hashtable PendingUpgradeTimeByName = (Hashtable) PendingUpgradeTimesByType[type];
			
			PendingUpgradeTimeByName.Add(pendingAppName, pendingAppTime);
			PendingUpgradeNodeByName.Add(pendingAppName, child);

			CreateCalendarRecord(pendingAppName,pendingAppTime);
			if (pendingAppTime<=CurrentDay)
			{
				DoUpgrade(pendingAppName, type);
			}
		}

		void HandleNewUpgradeOnSecond(Node sender, Node child)
		{
			// Get the current day and current time in case we have a delayed upgrade.
			int CurrentSecond = currentTimeNode.GetIntAttribute("seconds",0);
			string pendingAppName = child.GetAttribute("appname");
			int pendingAppTime = child.GetIntAttribute("when",0);
			string type = child.GetAttribute("upgrade_option");

			//Add into Pending app list 
			if (! PendingUpgradeNodesByType.ContainsKey(type))
			{
				PendingUpgradeNodesByType.Add(type, new Hashtable ());
			}
			Hashtable PendingUpgradeNodeByName = (Hashtable) PendingUpgradeNodesByType[type];

			if (! PendingUpgradeTimesByType.ContainsKey(type))
			{
				PendingUpgradeTimesByType.Add(type, new Hashtable ());
			}
			Hashtable PendingUpgradeTimeByName = (Hashtable) PendingUpgradeTimesByType[type];
			
			PendingUpgradeTimeByName.Add(pendingAppName, pendingAppTime);
			PendingUpgradeNodeByName.Add(pendingAppName, child);

			if (pendingAppTime<=CurrentSecond)
			{
				DoUpgrade(pendingAppName, type);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="child"></param>
		void upgradeQueue_ChildAdded(Node sender, Node child)
		{
			//Handling an new Upgrade 
			if (MyTransitionMode)
			{
				HandleNewUpgradeOnDay(sender, child);//working in Days 
			}
			else
			{
				HandleNewUpgradeOnSecond(sender, child);//working in Seconds
			}
		}

		void upgradeQueue_ChildRemoved(Node sender, Node child)
		{
			string AppNameToBeCancelled = child.GetAttribute("appname");
			string type = child.GetAttribute("upgrade_option");

			if (PendingUpgradeNodesByType.ContainsKey(type))
			{
				Hashtable PendingUpgradeNodeByName = PendingUpgradeNodesByType[type] as Hashtable;
				if (PendingUpgradeNodeByName.ContainsKey(AppNameToBeCancelled))
				{
					PendingUpgradeNodeByName.Remove(AppNameToBeCancelled);
				}
			}

			if (PendingUpgradeTimesByType.ContainsKey(type))
			{
				Hashtable PendingUpgradeTimeByName = PendingUpgradeTimesByType[type] as Hashtable;
				if (PendingUpgradeTimeByName.ContainsKey(AppNameToBeCancelled))
				{
					PendingUpgradeTimeByName.Remove(AppNameToBeCancelled);
				}
			}

			//remove from my lists
			//remove from Calender
			RemoveCalendarRecord(AppNameToBeCancelled);
		}

		struct AppToUpgrade
		{
			public string name;
			public string type;

			public AppToUpgrade (string name, string type)
			{
				this.name = name;
				this.type = type;
			}
		}

		void HandleTimeChanged(Node sender, ArrayList attrs)
		{
			//check if we have a upgrade for this time
			CurrentSecond = currentTimeNode.GetIntAttribute("seconds",0);
			//build up a seperate struture of app to do this day 
			//since doing the upgrade affects the hashtable that you are stepping through
			ArrayList appsToUpgrade = new ArrayList();
			foreach (string type in PendingUpgradeTimesByType.Keys)
			{
				Hashtable PendingUpgradeTimeByName = PendingUpgradeTimesByType[type] as Hashtable;

				foreach (string app_name in PendingUpgradeTimeByName.Keys)
				{
					int when_value = (int)PendingUpgradeTimeByName[app_name];
					if (when_value <= CurrentSecond)
					{
						appsToUpgrade.Add(new AppToUpgrade (app_name, type));
					}
				}
			}
			if (appsToUpgrade.Count>0)
			{
				foreach (AppToUpgrade upgrade in appsToUpgrade)
				{
					DoUpgrade(upgrade.name, upgrade.type);
				}
			}
		}

		void HandleDayChanged(Node sender, ArrayList attrs)
		{
			//check if we have a upgrade for this day 
			CurrentDay = currentDayNode.GetIntAttribute("day",0);
			//build up a seperate struture of app to do this day 
			//since doing the upgrade affects the hashtable that you are stepping through
			ArrayList appsToUpgrade = new ArrayList();

			foreach (string type in PendingUpgradeTimesByType.Keys)
			{
				Hashtable PendingUpgradeTimeByName = PendingUpgradeTimesByType[type] as Hashtable;
				foreach (string app_name in PendingUpgradeTimeByName.Keys)
				{
					int when_value = (int)PendingUpgradeTimeByName[app_name];
					if (when_value <= CurrentDay)
					{
						appsToUpgrade.Add(new AppToUpgrade (app_name, type));
					}
				}
			}
			if (appsToUpgrade.Count>0)
			{
				foreach (AppToUpgrade upgrade in appsToUpgrade)
				{
					DoUpgrade(upgrade.name, upgrade.type);
				}
			}
		}

		void TimeNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			if (MyTransitionMode)
			{
				HandleDayChanged(sender, attrs);	//working in Days 
			}
			else
			{
				HandleTimeChanged(sender, attrs);//working in Seconds
			}
		}
	}
}

