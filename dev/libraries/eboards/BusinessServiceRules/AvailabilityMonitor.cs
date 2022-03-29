using System;
using System.Collections;
using LibCore;
using Network;

using CoreUtils;

namespace BusinessServiceRules
{
	/// <summary>
	/// The AvailabilityMonitor connects to all business services and watches them
	/// for going down. It also watches the clock. It then calculates the current
	/// running availability.
	/// </summary>
	public class AvailabilityMonitor : ITimedClass, IDisposable
	{
		Node currentTimeNode;
		Node _groupNode;
		ArrayList nodesToMonitor;

		Node availabilityNode;

		ArrayList nodesDown;
		double availability;
		double criticalAvailability;

		int DetermineConnectionChildCount(Node BusinessServiceNode)
		{
			int numberOfConnectionChildren = 0;
			if (BusinessServiceNode != null)
			{
				foreach (Node n in BusinessServiceNode.getChildren())
				{
					string kidtype = n.GetAttribute("type");
					if (kidtype.ToLower() == "connection")
					{
						numberOfConnectionChildren++;
					}
				}
			}
			return numberOfConnectionChildren;
		}

		public AvailabilityMonitor(NodeTree model, string groupNodeName)
		{
			_groupNode = model.GetNamedNode(groupNodeName);
			availabilityNode = model.GetNamedNode("Availability");

			availabilityNode.AttributesChanged += availabilityNode_AttributesChanged;



			currentTimeNode = model.GetNamedNode("CurrentTime");
			nodesToMonitor = new ArrayList();
			nodesDown = new ArrayList();

			foreach(Node n in _groupNode)
			{
				if(n.GetAttribute("type") == "biz_service")
				{
					nodesToMonitor.Add(n);
					n.AttributesChanged += n_AttributesChanged;
					n.ChildAdded += n_ChildAdded;
					n.ChildRemoved += n_ChildRemoved;

					if(! n.GetBooleanAttribute("up", false))
					{
						if(DetermineConnectionChildCount(n) > 0)
						{
							nodesDown.Add(n);
						}
					}
				}
			}

			availability = CONVERT.ParseDouble(availabilityNode.GetAttribute("availability"))/100.0;
			criticalAvailability = availabilityNode.GetDoubleAttribute("critical_service_availability", 100) / 100.0;

			currentTimeNode.AttributesChanged += currentTimeNode_AttributesChanged;
			_groupNode.ChildAdded += groupNode_ChildAdded;
			_groupNode.ChildRemoved += groupNode_ChildRemoved;
		}

		void availabilityNode_AttributesChanged (Node sender, ArrayList attrs)
		{
			bool external = false;
			double externalAvailability = 0;

			foreach (AttributeValuePair avp in attrs)
			{
				switch (avp.Attribute.ToLower())
				{
					case "external":
						external = CONVERT.ParseBool(avp.Value, false);
						break;

					case "availability":
						externalAvailability = CONVERT.ParseDouble(avp.Value) / 100;
						break;
				}
			}

			if (external)
			{
				int time = currentTimeNode.GetIntAttribute("seconds", 0);
				availability = externalAvailability * time;
				CalcAvailability(time);
			}
		}

		/// <summary>
		/// Dispose ...
		/// </summary>
		public void Dispose()
		{
			foreach(Node n in nodesToMonitor)
			{
				n.AttributesChanged -= n_AttributesChanged;
				n.ChildAdded -= n_ChildAdded;
				n.ChildRemoved -= n_ChildRemoved;
			}
			nodesToMonitor.Clear();
			nodesDown.Clear();
			//
			currentTimeNode.AttributesChanged -= currentTimeNode_AttributesChanged;
			_groupNode.ChildAdded -= groupNode_ChildAdded;
			_groupNode.ChildRemoved -= groupNode_ChildRemoved;
			availabilityNode.AttributesChanged -= availabilityNode_AttributesChanged;
		}

		protected void CalcAvailability(double seconds)
		{
			if (seconds > 0)
			{
				seconds += 1.0;
				double currentAvailability = (1.0 - ((double) nodesDown.Count) / ((double) Math.Max(1, nodesToMonitor.Count)));
				availability += currentAvailability;
				double totalAvailability = (availability / Math.Max(1, seconds)) * 100.0;

				int criticalNodes = 0;
				int criticalNodesUp = 0;
				foreach (Node node in nodesToMonitor)
				{
					if (node.GetBooleanAttribute("critical", false))
					{
						criticalNodes++;

						if (! nodesDown.Contains(node))
						{
							criticalNodesUp++;
						}
					}
				}

				if (criticalNodes == 0)
				{
					criticalAvailability += 1;
				}
				else
				{
					criticalAvailability += (((double) criticalNodesUp) / criticalNodes);
				}
				double totalCriticalAvailability = criticalAvailability * 100 / seconds;

				ArrayList attrs = new ArrayList ();
				AttributeValuePair.AddIfNotEqual(availabilityNode, attrs, "availability", totalAvailability);
				AttributeValuePair.AddIfNotEqual(availabilityNode, attrs, "critical_service_availability", totalCriticalAvailability);
				AttributeValuePair.AddIfNotEqual(availabilityNode, attrs, "current_services_percent", (int) (100 * currentAvailability));
				availabilityNode.SetAttributes(attrs);
			}
		}

		void groupNode_ChildAdded(Node sender, Node child)
		{
			string node_type = child.GetAttribute("type");
			if (node_type.ToLower()=="biz_service")
			{
				if(!nodesToMonitor.Contains(child))
				{
					nodesToMonitor.Add(child);
					child.AttributesChanged += n_AttributesChanged;
					//
					if(! child.GetBooleanAttribute("up", true))
					{
						nodesDown.Add(child);
					}
				}
			}
		}

		void groupNode_ChildRemoved(Node sender, Node child)
		{
			string node_type = child.GetAttribute("type");
			if (node_type.ToLower()=="biz_service")
			{
				if(nodesToMonitor.Contains(child))
				{
					nodesToMonitor.Remove(child);
				}
				//
				if(nodesDown.Contains(child))
				{
					nodesDown.Remove(child);
				}
				//
				child.AttributesChanged -= n_AttributesChanged;
				child.ChildAdded -= n_ChildAdded;
				child.ChildRemoved -= n_ChildRemoved;
			}
		}

		#region ITimedClass Members

		public void Start()
		{
		}

		public void FastForward(double timesRealTime)
		{
		}

		public void Reset()
		{
			nodesDown.Clear();
			availabilityNode.SetAttribute("availability","100.0");
		}

		public void Stop()
		{
		}

		#endregion

		void n_AttributesChanged(Node sender, ArrayList attrs)
		{
			// Check if we are going down or coming back up.
			foreach(AttributeValuePair avp in attrs)
			{
				if(avp.Attribute == "up")
				{
					if(avp.Value.ToLower() == "false")
					{
						// This is down.
						if(!nodesDown.Contains(sender))
						{
							if(sender.getChildren().Count > 0)
							{
								nodesDown.Add(sender);
							}
						}
					}
					else
					{
						// This is up.
						if(nodesDown.Contains(sender))
						{
							nodesDown.Remove(sender);
						}
					}
				}
			}
		}

		void currentTimeNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			bool timeChanged = false;
			string time = "";
			bool suppressAvailabilityUpdate = currentTimeNode.GetBooleanAttribute("suppress_availability_update", false);

			foreach(AttributeValuePair avp in attrs)
			{
				if(avp.Attribute == "seconds")
				{
					timeChanged = true;
					time = avp.Value;
				}
				else if (avp.Attribute == "suppress_availability_update")
				{
					suppressAvailabilityUpdate = true;
				}
			}

			if (timeChanged && ! suppressAvailabilityUpdate)
			{
				CalcAvailability(CONVERT.ParseInt(time));
			}
		}

		void n_ChildAdded(Node sender, Node child)
		{
			if(! sender.GetBooleanAttribute("up", true))
			{
				if (DetermineConnectionChildCount(sender)>0)
				{
					if(!nodesDown.Contains(sender))
					{
						nodesDown.Add(sender);
					}
				}
			}
		}

		void n_ChildRemoved(Node sender, Node child)
		{
			if(nodesDown.Contains(sender))
			{
				if(DetermineConnectionChildCount(sender) == 0)
				{
					nodesDown.Remove(sender);
				}
			}
		}
	}
}