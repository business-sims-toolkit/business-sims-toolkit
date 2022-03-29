using System;
using System.Collections;
using LibCore;
using Network;

namespace IncidentManagement
{
	public interface IInstallableProjectsEnumerator
	{
		ArrayList GetInstallableProjects ();
	}

	/// <summary>
	/// ZoneActivator maintains the ZoneActivation queue during the ops phase, watching for
	/// the addition of nodes that indicate zones to be turned on.
	/// </summary>
	public class ZoneActivator : IDisposable
	{
		NodeTree model;

		Node queueNode;

		IInstallableProjectsEnumerator projectsEnumerator;

		public ZoneActivator (NodeTree model, IInstallableProjectsEnumerator projectsEnumerator)
		{
			this.model = model;
			this.projectsEnumerator = projectsEnumerator;

			queueNode = model.GetNamedNode("ZoneActivationQueue");
			queueNode.ChildAdded += queueNode_ChildAdded;
		}

		public void Dispose ()
		{
			if (queueNode != null)
			{
				queueNode.ChildAdded -= queueNode_ChildAdded;
			}
		}

		void queueNode_ChildAdded (Node sender, Node child)
		{
			switch (child.GetAttribute("type").ToLower())
			{
				case "activatezone":
				{
					int zone = child.GetIntAttribute("zone", 0);
					string [] services = child.GetAttribute("services").Split(',');
					ActivateZone(zone, services);
					break;
				}

				case "retireservices":
				{
					RetireServices();
					break;
				}

				case "switchzonetoblades":
				{
					SwitchZone4ToBlades();
					break;
				}

				case "raisezonetemperature":
				{
					int zone = child.GetIntAttribute("zone", 0);
					RaiseZoneTemperature(zone);
					break;
				}

				case "consolidatefuel":
				{
					ConsolidateFuel();
					break;
				}

				case "turnserveron":
				{
					string server = child.GetAttribute("server");
					TurnServerOn(server);
					break;
				}

				case "turnserveroff":
				{
					string server = child.GetAttribute("server");
					TurnServerOff(server);
					break;
				}
			}

			queueNode.DeleteChildTree(child);
		}

		void ActivateZone (int zone, string [] appsToMove)
		{
			ArrayList attrs = new ArrayList ();
			Node zoneNode = model.GetNamedNode("Zone" + CONVERT.ToStr(zone));
			int powerTotal = 45;
			int appsToDistributePowerOver = appsToMove.Length;

			ArrayList potentialSlots = model.GetNodesWithAttributeValue("type", "Slot");
			ArrayList slots = new ArrayList ();
			foreach (Node slot in potentialSlots)
			{
				if ((slot.GetIntAttribute("proczone", 0) == zone) || (slot.GetIntAttribute("zone", 0) == zone))
				{
					slots.Add(slot);
				}
			}

			foreach (string appName in appsToMove)
			{
				if (appName != "")
				{
					Node app = model.GetNamedNode(appName);

					if (slots.Count > 0)
					{
						Node slot = slots[0] as Node;
						slots.RemoveAt(0);

						// Move the app to the same parent as the slot; copy the slot's location over the app's;
						// move the slot to the app's old parent and set its name and location to the app's old ones.

						Node otherZoneParent = app.Parent;
						Node newZoneParent = slot.Parent;
						string otherZoneLocation = app.GetAttribute("location");
						string newZoneLocation = slot.GetAttribute("location");
						string otherZone = app.GetAttribute("proczone");
						string newZone = slot.GetAttribute("proczone");

						attrs.Clear();
						attrs.Add(new AttributeValuePair ("location", newZoneLocation));
						attrs.Add(new AttributeValuePair ("zone", newZone));
						attrs.Add(new AttributeValuePair ("proczone", newZone));
						attrs.Add(new AttributeValuePair ("virtualmirrored", "true"));

						// The new app uses no extra power: this allows the zone power total to remain as we want.
						int thisPower = powerTotal / appsToDistributePowerOver;
						appsToDistributePowerOver--;
						powerTotal -= thisPower;
						attrs.Add(new AttributeValuePair ("proccap", thisPower));
						app.SetAttributes(attrs);

						attrs.Clear();
						attrs.Add(new AttributeValuePair ("location", otherZoneLocation));
						attrs.Add(new AttributeValuePair ("name", otherZoneLocation));
						attrs.Add(new AttributeValuePair ("zone", otherZone));
						attrs.Add(new AttributeValuePair ("proczone", otherZone));
						slot.SetAttributes(attrs);

						otherZoneParent.AddChild(slot);
						newZoneParent.AddChild(app);

						model.FireMovedNode(newZoneParent, slot);
						model.FireMovedNode(otherZoneParent, app);

						// Also ensure that the child connections are mirrored too.
						foreach (Node child in app)
						{
							child.SetAttribute("virtualmirrored", "true");

							LinkNode link = child as LinkNode;
							if (link != null)
							{
								link.To.SetAttribute("virtualmirrored", "true");
							}
						}
					}
					else
					{
						Node errors = model.GetNamedNode("FacilitatorNotifiedErrors");
						new Node (errors, "error", "", new AttributeValuePair ("text", "Cannot Move App " + app.GetAttribute("name") + " As There Are No Slots Left In Zone " + CONVERT.ToStr(zone)));
					}
				}
			}

			// Now install the two round 3 SIPs.
			Node installerNode = model.GetNamedNode("ProjectsIncomingRequests");

			string when = CONVERT.ToStr(1 + model.GetNamedNode("CurrentTime").GetIntAttribute("seconds", 0));

			if (slots.Count > 0)
			{
				// Install 301 to C339 if possible.
				Node slot = slots[0] as Node;
				foreach (Node trySlot in slots)
				{
					if (trySlot.GetAttribute("name") == "C339")
					{
						slot = trySlot;
					}
				}

				attrs.Clear();
				attrs.Add(new AttributeValuePair ("projectid", "301"));
				attrs.Add(new AttributeValuePair ("installwhen", when));
				attrs.Add(new AttributeValuePair ("sla", "6"));
				attrs.Add(new AttributeValuePair ("location", slot.GetAttribute("name")));
				attrs.Add(new AttributeValuePair ("type", "Install") );
				attrs.Add(new AttributeValuePair ("phase", "operations") );
				new Node (installerNode, "install", "", attrs);

				slots.Remove(slot);
			}

			if (slots.Count > 0)
			{
				// Install 305 to C341 if possible.
				Node slot = slots[0] as Node;
				foreach (Node trySlot in slots)
				{
					if (trySlot.GetAttribute("name") == "C341")
					{
						slot = trySlot;
					}
				}

				attrs.Clear();
				attrs.Add(new AttributeValuePair ("projectid", "305"));
				attrs.Add(new AttributeValuePair ("installwhen", when));
				attrs.Add(new AttributeValuePair ("sla", "6"));
				attrs.Add(new AttributeValuePair ("location", slot.GetAttribute("name")));
				attrs.Add(new AttributeValuePair ("type", "Install") );
				attrs.Add(new AttributeValuePair ("phase", "operations") );
				new Node (installerNode, "install", "", attrs);

				slots.Remove(slot);
			}

			attrs.Clear();
			attrs.Add(new AttributeValuePair ("activated", "true"));
			zoneNode.SetAttributes(attrs);

			// Turn off zones 1 and 6 too...
			zoneNode = model.GetNamedNode("Zone1");
			zoneNode.SetAttribute("activated", false);

			zoneNode = model.GetNamedNode("Zone6");
			zoneNode.SetAttribute("activated", false);
		}

		public static bool IsZone4Bladed (NodeTree model)
		{
			Node zone4 = model.GetNamedNode("Zone4");
			return ((zone4 != null) && (zone4.GetBooleanAttribute("bladed", false)));
		}

		public static bool IsZone7Activated (NodeTree model)
		{
			Node zone7 = model.GetNamedNode("Zone7");
			return ((zone7 != null) && (zone7.GetBooleanAttribute("activated", false)));
		}

		public static bool IsFuelConsolidated (NodeTree model)
		{
			Node hunt = model.GetNamedNode("Hunt");
			return (hunt == null);
		}

		public static bool HaveZonesHadRetirement (NodeTree model)
		{
			for (int zone = 1; zone <= 7; zone++)
			{
				if (HasZoneHadRetirement(model, zone))
				{
					return true;
				}
			}

			return false;
		}

		public static bool HasZoneHadRetirement (NodeTree model, int zone)
		{
			Node zoneNode = model.GetNamedNode("Zone" + CONVERT.ToStr(zone));
			return zoneNode.GetBooleanAttribute("done_retirement", false);
		}

		public static bool IsZone2TemperatureRaised (NodeTree model)
		{
			Node airconNode = model.GetNamedNode("Aircon2");
			return airconNode.GetBooleanAttribute("turnedoff", false);
		}

		public static void RaiseZone2Temperature (NodeTree model)
		{
			ArrayList attrs = new ArrayList ();
			attrs.Add(new AttributeValuePair ("type", "RaiseZoneTemperature"));
			attrs.Add(new AttributeValuePair ("zone", "2"));
			new Node (model.GetNamedNode("ZoneActivationQueue"), "RaiseZoneTemperature", "", attrs);
		}

		void RaiseZoneTemperature (int zone)
		{
			Node airconNode = model.GetNamedNode("Aircon" + CONVERT.ToStr(zone));
			airconNode.SetAttribute("turnedoff", true);

			Node zoneNode = model.GetNamedNode("Zone" + CONVERT.ToStr(zone));
			zoneNode.SetAttribute("pue", "2");
		}

		public static void SwitchZone4ToBlades (NodeTree model)
		{
			ArrayList attrs = new ArrayList ();
			attrs.Add(new AttributeValuePair ("type", "SwitchZoneToBlades"));
			new Node (model.GetNamedNode("ZoneActivationQueue"), "SwitchZoneToBlades", "", attrs);
		}

		public static int GetZonePower (NodeTree model, int zone)
		{
			ArrayList thingsInZone = model.GetNodesWithAttributeValue("zone", CONVERT.ToStr(zone));
			thingsInZone.AddRange(model.GetNodesWithAttributeValue("proczone", CONVERT.ToStr(zone)));

			// Distribute the power saving amongst all the apps and servers in the zone.
			// First form a list of all apps and DBs in the zone.
			ArrayList apps = new ArrayList ();
			foreach (Node thing in thingsInZone)
			{
				if ((thing.GetAttribute("type") == "App") || (thing.GetAttribute("type") == "Database"))
				{
					apps.Add(thing);
				}
			}

			// Find their total power usage.
			int totalAppPower = 0;
			foreach (Node app in apps)
			{
				totalAppPower += Math.Max(0, app.GetIntAttribute("proccap", 0) - 1);
			}

			return totalAppPower;
		}

		/// <summary>
		/// Given a zone number and an amount to increase its total power consumption by,
		/// distribute that increase amongst its apps and servers in proportion to their
		/// current usages.
		/// </summary>
		public static void ReduceZonePower (NodeTree model, int zone, int reduction)
		{
			ArrayList thingsInZone = model.GetNodesWithAttributeValue("zone", CONVERT.ToStr(zone));
			thingsInZone.AddRange(model.GetNodesWithAttributeValue("proczone", CONVERT.ToStr(zone)));

			// Distribute the power saving amongst all the apps and servers in the zone.
			// First form a list of all apps and DBs in the zone.
			ArrayList apps = new ArrayList ();
			foreach (Node thing in thingsInZone)
			{
				if ((thing.GetAttribute("type") == "App") || (thing.GetAttribute("type") == "Database"))
				{
					apps.Add(thing);
				}
			}

			// Find their total power usage.
			int totalReducibleAppPower = 0;
			foreach (Node app in apps)
			{
				totalReducibleAppPower += Math.Max(0, app.GetIntAttribute("proccap", 0) - 1);
			}

			// Now for each one, reduce its power proportionate to its usage relative to the zone total.
			int remainingReduction = reduction;
			int remainingReducibleAppPower = totalReducibleAppPower;

			while ((apps.Count > 0) && (remainingReduction > 0) && (remainingReducibleAppPower > 0))
			{
				Node app = (Node) apps[0];
				Node server = app.Parent;

				int appPower = app.GetIntAttribute("proccap", 0);
				int serverPower = server.GetIntAttribute("proccap", 0);

				int reducibleAppPower = Math.Max(0, appPower - 1);

				int share = (remainingReduction * reducibleAppPower) / remainingReducibleAppPower;
				share = Math.Max(0, Math.Min(share, appPower - 1));

				appPower -= share;
				serverPower -= share;

				remainingReduction -= share;
				remainingReducibleAppPower -= reducibleAppPower;

				app.SetAttribute("proccap", appPower);
				server.SetAttribute("proccap", serverPower);

				apps.Remove(app);
			}
		}

		/// <summary>
		/// Given a zone number and an amount to increase its total power consumption by,
		/// distribute that increase amongst its apps and servers in proportion to their
		/// current usages.
		/// </summary>
		static void IncreaseZonePower (NodeTree model, int zone, int increase)
		{
			ArrayList thingsInZone = model.GetNodesWithAttributeValue("zone", CONVERT.ToStr(zone));
			thingsInZone.AddRange(model.GetNodesWithAttributeValue("proczone", CONVERT.ToStr(zone)));

			// Distribute the power change amongst all the apps and servers in the zone.
			// First form a list of all apps and DBs in the zone.
			ArrayList apps = new ArrayList ();
			foreach (Node thing in thingsInZone)
			{
				if ((thing.GetAttribute("type") == "App") || (thing.GetAttribute("type") == "Database"))
				{
					apps.Add(thing);
				}
			}

			// Find their total power usage.
			int totalAppPower = 0;
			foreach (Node app in apps)
			{
				totalAppPower += app.GetIntAttribute("proccap", 0);
			}

			// Now for each one, increase its power proportionate to its usage relative to the zone total.
			int remainingIncrease = increase;
			int remainingAppPower = totalAppPower;

			while (apps.Count > 0)
			{
				Node app = (Node) apps[0];
				Node server = app.Parent;

				int appPower = app.GetIntAttribute("proccap", 0);
				int serverPower = server.GetIntAttribute("proccap", 0);

				int share = (remainingIncrease * appPower) / remainingAppPower;

				remainingIncrease -= share;
				remainingAppPower -= appPower;

				appPower += share;
				serverPower += share;

				app.SetAttribute("proccap", appPower);
				server.SetAttribute("proccap", serverPower);

				apps.Remove(app);
			}
		}

		public static ArrayList GetZone7Slots (NodeTree model)
		{
			ArrayList potentialSlots = model.GetNodesWithAttributeValue("type", "Slot");
			ArrayList slots = new ArrayList ();
			foreach (Node slot in potentialSlots)
			{
				// Only count slots in the right zone...
				if (((slot.GetIntAttribute("proczone", 0) == 7) || (slot.GetIntAttribute("zone", 0) == 7))
				// and only allow up to four anyway.
					&& (slots.Count < 4))
				{
					slots.Add(slot);
				}
			}

			return slots;
		}

		public static ArrayList GetPotentialAppsToMoveToZone7 (NodeTree model)
		{
			ArrayList apps = new ArrayList ();

			apps.Add(model.GetNamedNode("Stewart"));
			apps.Add(model.GetNamedNode("Brabham"));

			apps.Add(model.GetNamedNode("Fangio"));
			apps.Add(model.GetNamedNode("Piquet"));

			apps.Add(model.GetNamedNode("Hulme"));
			apps.Add(model.GetNamedNode("Clark"));

			return apps;
		}

		public static void AddMiscServices (NodeTree network, int zone, int numServices, int totalPower, int nonITPower)
		{
			Node zoneNode = network.GetNamedNode("Zone" + CONVERT.ToStr(zone));

			zoneNode.SetAttribute("dummy_app_count", numServices + zoneNode.GetIntAttribute("dummy_app_count", 0));
			IncreaseZonePower(network, zone, totalPower);
		}

		public static void ConsolidateFuel (NodeTree network)
		{
			ArrayList attrs = new ArrayList ();
			attrs.Add(new AttributeValuePair ("type", "ConsolidateFuel"));
			new Node (network.GetNamedNode("ZoneActivationQueue"), "ConsolidateFuel", "", attrs);
		}

		public static void RetireServices (NodeTree network)
		{
			ArrayList attrs = new ArrayList ();
			attrs.Add(new AttributeValuePair ("type", "RetireServices"));
			new Node (network.GetNamedNode("ZoneActivationQueue"), "RetireServices", "", attrs);
		}

		void RetireServices ()
		{
			int zone = 3;

			int dummyAppReduction = 79;

			Node zoneNode = model.GetNamedNode("Zone" + CONVERT.ToStr(zone));

			int dummyApps = zoneNode.GetIntAttribute("dummy_app_count", 0);
			int totalPower = GetZonePower(model, zone);

			dummyApps -= dummyAppReduction;

			ArrayList attributes = new ArrayList ();
			attributes.Add(new AttributeValuePair ("done_retirement", "true"));
			attributes.Add(new AttributeValuePair ("dummy_app_count", dummyApps));
			attributes.Add(new AttributeValuePair ("pue", 2));
			zoneNode.SetAttributes(attributes);

			ReduceZonePower(model, zone, totalPower / 20);
		}

		void SwitchZone4ToBlades ()
		{
			// Make it impossible to install anything to servers in the zone (apart from Indianapolis).
			ArrayList thingsInZone4 = model.GetNodesWithAttributeValue("zone", "4");
			thingsInZone4.AddRange(model.GetNodesWithAttributeValue("proczone", "4"));
			ArrayList slots = new ArrayList ();
			foreach (Node node in thingsInZone4)
			{
				string server = node.Parent.GetAttribute("name");
				if ((slots.IndexOf(node) == -1) && (node.GetAttribute("type") == "Slot") && (server != "Indianapolis"))
				{
					slots.Add(node);
				}
			}
			foreach (Node slot in slots)
			{
				slot.SetAttribute("bladed", "true");
			}

			// Mark the zone as bladed.
			Node zone4 = model.GetNamedNode("Zone4");
			zone4.SetAttribute("bladed", "true");

			// Improve PUE...
			zone4.SetAttribute("pue", "2");

			// ...and total power consumption by (25 + 12) kW.
			ReduceZonePower(model, 4, 25 + 12);

			// Create blade icons in the flash board.
/*			ArrayList attrs = new ArrayList ();
			attrs.Add(new AttributeValuePair ("name", "IndianapolisBlade"));
			attrs.Add(new AttributeValuePair ("location", "IndianapolisBlade"));
			attrs.Add(new AttributeValuePair ("type", "Blade"));
			attrs.Add(new AttributeValuePair ("new", "true"));
			new Node (zone4, "Blade", "", attrs);

			attrs.Clear();
			attrs.Add(new AttributeValuePair ("name", "DetroitBlade"));
			attrs.Add(new AttributeValuePair ("location", "DetroitBlade"));
			attrs.Add(new AttributeValuePair ("type", "Blade"));
			attrs.Add(new AttributeValuePair ("new", "true"));
			new Node (zone4, "Blade", "", attrs);*/
		}

		void ConsolidateFuel ()
		{
			Node farina = model.GetNamedNode("Farina");
			Node hunt = model.GetNamedNode("Hunt");
			Node car4 = hunt.getChildren()[0] as Node;

			// Move Car 4 Fuel Rig Operations from Hunt to Farina.
			farina.AddChild(car4);
			model.FireMovedNode(hunt, car4);

			// Now kill Hunt.
			string location = hunt.GetAttribute("location");
			ArrayList attributes = new ArrayList ();
			attributes.Add(new AttributeValuePair ("name", location));
			attributes.Add(new AttributeValuePair ("type", "Slot"));
			attributes.Add(new AttributeValuePair ("proccap", "0"));
			attributes.Add(new AttributeValuePair ("diskrequired", "0"));
			attributes.Add(new AttributeValuePair ("memrequired", "0"));
			attributes.Add(new AttributeValuePair ("desc", ""));
			hunt.SetAttributes(attributes);
		}

		void TurnServerOn (string serverName)
		{
			Node server = model.GetNamedNode(serverName);

			ArrayList attrs = new ArrayList ();
			attrs.Add(new AttributeValuePair ("turnedoff", "false"));
			attrs.Add(new AttributeValuePair ("nopower", "false"));
			attrs.Add(new AttributeValuePair ("power_tripped", "false"));
			attrs.Add(new AttributeValuePair ("up", "true"));
			attrs.Add(new AttributeValuePair ("incident_id", ""));
			attrs.Add(new AttributeValuePair ("powering_down", "false"));
			server.SetAttributes(attrs);

			ArrayList findAttrs = new ArrayList ();
			findAttrs.Add(new AttributeValuePair ("incident_id", server.GetAttribute("name") + "_turn_off"));
			Hashtable scheduledPowerDown = GlobalEventDelayer.TheInstance.Delayer.GetFutureEventsWithAttributeValues(findAttrs);
			foreach (IEvent scheduledEvent in scheduledPowerDown.Keys)
			{
				GlobalEventDelayer.TheInstance.Delayer.RemoveEvent(scheduledEvent);
			}
		}

		void TurnServerOff (string serverName)
		{
			Node server = model.GetNamedNode(serverName);

			// Fix for 4917: don't allow power-cycling on broken servers.
			if (server.GetAttribute("incident_id") != "")
			{
				Node errors = server.Tree.GetNamedNode("FacilitatorNotifiedErrors");
				new Node (errors, "error", "", new AttributeValuePair ("text", "Cannot Turn " + server.GetAttribute("name") + " Off While It Has An Incident"));
			}
			else
			{
				server.SetAttribute("powering_down", "true");

				// Rather than affect the server immediately, create a delayed event.
				string xml = "<i id=\"AtStart\" incident_id=\"" + server.GetAttribute("name") + "_turn_off\">";

				// Register an incident so the incident report can track it.
				xml += "<createNodes i_to=\"enteredIncidents\"> <IncidentNumber id=\"" + server.GetAttribute("name") + "_turn_off\" /> </createNodes>";

				// Register a costed event likewise.
				xml += "<createNodes i_to=\"CostedEvents\"> <CostedEvent type=\"turn_off_incident\" desc=\"Server " + server.GetAttribute("name") + " Turned Off\" incident_id=\"" + server.GetAttribute("name") + "_turn_off\" zoneof=\"" + server.GetAttribute("name") + "\" facilities=\"true\" power=\"true\" /> </createNodes>";

				// Bring down the server.
				xml += "<apply i_name=\"" + server.GetAttribute("name") + "\" turnedoff=\"true\" nopower=\"true\" up=\"false\" incident_id=\"" + server.GetAttribute("name") + "_turn_off\" penalised=\"false\" powering_down=\"false\" />";

				xml += "</i>";

				// Fire it off.
				GlobalEventDelayer.TheInstance.Delayer.AddEvent(xml, 15, server.Tree);
			}
		}
	}
}