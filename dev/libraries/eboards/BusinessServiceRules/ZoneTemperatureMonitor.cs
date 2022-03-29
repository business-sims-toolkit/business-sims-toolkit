using System;
using System.Collections;
using LibCore;
using Network;

namespace BusinessServiceRules
{
	public class ZoneTemperatureMonitor : IDisposable
	{
		protected NodeTree model;
		protected ArrayList coolingNodes;
		protected ArrayList airConNodes;
		protected ArrayList serverNodes;
		protected Node timeNode;

		public ZoneTemperatureMonitor (NodeTree model)
		{
			this.model = model;

			serverNodes = model.GetNodesWithAttributeValue("type", "Server");
			foreach (Node serverNode in serverNodes)
			{
				serverNode.AttributesChanged += serverNode_AttributesChanged;
			}

			coolingNodes = model.GetNodesWithAttributeValue("type", "Cooling");
			foreach (Node coolingNode in coolingNodes)
			{
				coolingNode.PreAttributesChanged += coolingNode_AttributesChanged;
			}

			airConNodes = model.GetNodesWithAttributeValue("type", "Aircon");
			foreach (Node airConNode in airConNodes)
			{
				airConNode.PreAttributesChanged += airConNode_AttributesChanged;
			}

			timeNode = model.GetNamedNode("CurrentTime");
			timeNode.AttributesChanged += timeNode_AttributesChanged;

			model.NodeAdded += model_NodeAdded;
		}

		void model_NodeAdded (NodeTree sender, Node newNode)
		{
			switch (newNode.Type.ToLower())
			{
				case "server":
					newNode.AttributesChanged += serverNode_AttributesChanged;
					break;

				case "cooling":
					newNode.PreAttributesChanged += coolingNode_AttributesChanged;
					break;

				case "aircon":
					newNode.PreAttributesChanged += airConNode_AttributesChanged;
					break;
			}
		}

		void serverNode_AttributesChanged (Node sender, ArrayList attrs)
		{
			foreach (AttributeValuePair avp in attrs)
			{
				if (avp.Attribute.ToLower() == "turnedoff")
				{
					UpdateZonePlugStatus(sender);
				}
			}
		}

		void coolingNode_AttributesChanged (Node sender, ref ArrayList attrs)
		{
			foreach (AttributeValuePair avp in attrs)
			{
				switch (avp.Attribute.ToLower())
				{
					case "goal_temperature":
						// If we're changing goal temperature, set the start time to be now, and
						// start temperature to be current temperature.
						sender.SetAttribute("goal_temperature_start_time", timeNode.GetAttribute("seconds"));
						sender.SetAttribute("goal_temperature_start", sender.GetAttribute("temperature"));
						break;

					case "baseline_temperature":
						// If we're changing baseline temperature, then if we are currently
						// at, or tending toward, baseline temperature, then aim toward
						// the new baseline temperature.
						string currentBaseline = sender.GetAttribute("baseline_temperature");
						string newBaseline = avp.Value;
						string goal = sender.GetAttribute("goal_temperature");
						if ((goal == "") || (goal == currentBaseline))
						{
							int changeStartTime = sender.GetIntAttribute("goal_temperature_start_time", 0);
							int changeEndTime = changeStartTime + sender.GetIntAttribute("goal_temperature_change_duration", 0);
							int currentTime = timeNode.GetIntAttribute("seconds", 0);

							sender.SetAttribute("goal_temperature", newBaseline);
							if (currentTime >= changeEndTime)
							{
								// We're already at the old baseline, so start a new tend to the new baseline.
								sender.SetAttribute("goal_temperature_change_duration", 5);
							}
							else
							{
								// We're still moving toward the old baseline, so just correct the destination.
								sender.SetAttribute("goal_temperature_change_duration", changeEndTime - currentTime);
							}
						}

						break;
				}
			}
		}

		void airConNode_AttributesChanged (Node sender, ref ArrayList attrs)
		{
			foreach (AttributeValuePair avp in attrs)
			{
				switch (avp.Attribute.ToLower())
				{
					case "turnedoff":
						Node coolingNode = sender.Parent;
						if (coolingNode != null)
						{
							Node coolerNode = model.GetNamedNode("CoolerEffect");
							string rate = "z" + sender.GetAttribute("zone") + "_rate";

							bool off = CONVERT.ParseBool(avp.Value, false);
							double oldBase = coolingNode.GetDoubleAttribute("initial_baseline_temperature", 72.3);
							double newBase;
							double newRate;
							if (off)
							{
								newBase = oldBase + 2.5;
								newRate = 10;
							}
							else
							{
								newBase = oldBase;
								newRate = 0;
							}

							if (oldBase != newBase)
							{
								coolingNode.SetAttribute("baseline_temperature", newBase);
							}

							if (coolerNode != null)
							{
								coolerNode.SetAttribute(rate, newRate);
							}
						}
						break;
				}
			}
		}

		protected void UpdateZonePlugStatus (Node changedServer)
		{
			string zone = changedServer.GetAttribute("zone");

			// Does this server's zone now have any servers turned off?
			ArrayList array = model.GetNodesWithAttributeValue("type", "Server");
			bool hasSomeOff = false;
			foreach (Node serverNode in array)
			{
				string thisZone = serverNode.GetAttribute("zone");

				if (thisZone == zone)
				{
					hasSomeOff = hasSomeOff || serverNode.GetBooleanAttribute("turnedoff", false);
					if (hasSomeOff)
					{
						break;
					}
				}
			}

			// Now set the power node's attributes accordingly.
			Node powerNode = model.GetNamedNode("P" + zone);
			powerNode.SetAttribute("has_some_children_turned_off", CONVERT.ToStr(hasSomeOff));

			// Now update the temperature if the zone is overheating.
			Node coolingNode = model.GetNamedNode("C" + zone);

			bool overheat = coolingNode.GetBooleanAttribute("thermal", false);
			if (overheat)
			{
				ArrayList attrs = new ArrayList ();
				if (hasSomeOff)
				{
					attrs.Add(new AttributeValuePair ("goal_temperature", coolingNode.GetAttribute("baseline_temperature")));
					attrs.Add(new AttributeValuePair ("goal_temperature_change_duration", 5));
				}
				else
				{
					attrs.Add(new AttributeValuePair ("goal_temperature", "86.4"));
					attrs.Add(new AttributeValuePair ("goal_temperature_change_duration", 5));
				}
				coolingNode.SetAttributes(attrs);
			}
		}

		public void Dispose ()
		{
			foreach (Node serverNode in serverNodes)
			{
				serverNode.AttributesChanged -= serverNode_AttributesChanged;
			}
			foreach (Node coolingNode in coolingNodes)
			{
				coolingNode.PreAttributesChanged -= coolingNode_AttributesChanged;
			}
			foreach (Node airConNode in airConNodes)
			{
				airConNode.PreAttributesChanged -= airConNode_AttributesChanged;
			}

			timeNode.AttributesChanged -= timeNode_AttributesChanged;

			model.NodeAdded -= model_NodeAdded;
		}

		void timeNode_AttributesChanged (Node sender, ArrayList attrs)
		{
			int now = sender.GetIntAttribute("seconds", -1);

			if (now != -1)
			{
				// Update the temperature of each cooling node.
				foreach (Node coolingNode in coolingNodes)
				{
					double goalTemperatureStart = coolingNode.GetDoubleAttribute("goal_temperature_start", -1);
					double goalTemperatureEnd = coolingNode.GetDoubleAttribute("goal_temperature", -1);
					int goalTemperatureStartTime = coolingNode.GetIntAttribute("goal_temperature_start_time", -1);
					int goalTemperatureDuration = coolingNode.GetIntAttribute("goal_temperature_change_duration", -1);

					double currentTemperature = coolingNode.GetDoubleAttribute("temperature", -1);
					double newTemperature = currentTemperature;

					if ((goalTemperatureStart != -1) && (goalTemperatureEnd != -1)
						&& (goalTemperatureStartTime != -1) && (goalTemperatureDuration != -1))
					{
						int goalTemperatureEndTime = goalTemperatureStartTime + goalTemperatureDuration;
						double t = Math.Min(1, Math.Max(0, (now - goalTemperatureStartTime)  / ((double) (goalTemperatureEndTime - goalTemperatureStartTime))));
						newTemperature = goalTemperatureStart + (t * (goalTemperatureEnd - goalTemperatureStart));
					}

					Node powerNode = coolingNode.Tree.GetNamedNode("P" + coolingNode.GetAttribute("zone"));                    
					// Fix for 4914 (delayed temperature increases still take effect even if a server is unplugged).
					if ((powerNode != null) && (powerNode.GetBooleanAttribute("has_some_children_turned_off", false)))
					{
						newTemperature = coolingNode.GetDoubleAttribute("baseline_temperature", 72.3);
					}

					if (newTemperature != currentTemperature)
					{
						coolingNode.SetAttribute("temperature", newTemperature);
					}
				}
			}
		}
	}
}