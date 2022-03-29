using System;
using System.Collections.Generic;
using System.Text;

using Network;

namespace Cloud.OpsEngine
{
	public class DemandManager : IDisposable
	{
		NodeTree model;
		Node demandsNode;
		Node timeNode;
		Node roundVariablesNode;

		OrderExecutor orderExecutor;

		int lastTimeKnown;

		public DemandManager (NodeTree model, OrderExecutor orderExecutor)
		{
			this.model = model;
			this.orderExecutor = orderExecutor;

			demandsNode = model.GetNamedNode("Demands");

			roundVariablesNode = model.GetNamedNode("RoundVariables");

			timeNode = model.GetNamedNode("CurrentTime");
			timeNode.AttributesChanged += new Node.AttributesChangedEventHandler (timeNode_AttributesChanged);
			lastTimeKnown = timeNode.GetIntAttribute("seconds", 0);

			foreach (Node business in model.GetNodesWithAttributeValue("type", "business"))
			{
				business.ChildAdded += new Node.NodeChildAddedEventHandler (business_ChildAdded);
				business.ChildRemoved += new Node.NodeChildRemovedEventHandler (business_ChildRemoved);

				foreach (Node businessService in business.GetChildrenOfType("business_service"))
				{
					businessService.AttributesChanged += new Node.AttributesChangedEventHandler (businessService_AttributesChanged);					 
				}
			}
		}

		public void Dispose ()
		{
			timeNode.AttributesChanged -= new Node.AttributesChangedEventHandler(timeNode_AttributesChanged);

			foreach (Node business in model.GetNodesWithAttributeValue("type", "business"))
			{
				business.ChildAdded -= new Node.NodeChildAddedEventHandler (business_ChildAdded);
				business.ChildRemoved -= new Node.NodeChildRemovedEventHandler (business_ChildRemoved);

				foreach (Node businessService in business.GetChildrenOfType("business_service"))
				{
					businessService.AttributesChanged -= new Node.AttributesChangedEventHandler(businessService_AttributesChanged);
				}
			}
		}

		void businessService_AttributesChanged (Node sender, System.Collections.ArrayList attrs)
		{
			foreach (AttributeValuePair pair in attrs)
			{
				if (pair.Attribute == "status")
				{
					AdvanceTime(0, sender);
					break;
				}
			}
		}

		void business_ChildRemoved (Node sender, Node child)
		{
			child.AttributesChanged -= new Node.AttributesChangedEventHandler (businessService_AttributesChanged);
		}

		void business_ChildAdded (Node sender, Node child)
		{
			AdvanceTime(0);
			child.AttributesChanged += new Node.AttributesChangedEventHandler(businessService_AttributesChanged);
		}

		void timeNode_AttributesChanged (Node sender, System.Collections.ArrayList attrs)
		{
			int time = timeNode.GetIntAttribute("seconds", 0);
			int dt = time - lastTimeKnown;
			lastTimeKnown = time;
			if (dt > 0)
			{
				AdvanceTime(dt);
			}
		}

		void AdvanceTime(int dt)
		{
			AdvanceTime(dt, null);
		}

		void AdvanceTime(int dt, Node businessServiceRequested)
		{
			foreach (Node demand in demandsNode.GetChildrenOfType("demand"))
			{
				List<AttributeValuePair> attributes = new List<AttributeValuePair> ();

				string status = "inactive";

				Node businessService = model.GetNamedNode(demand.GetAttribute("business_service"));

				if (demand.GetBooleanAttribute("active", false))
				{
					if (demand.GetIntAttribute("delay_countdown", 0) > 0)
					{
						int announcementTime = 60 * roundVariablesNode.GetIntAttribute("demand_announcement_duration_trading_periods", 0);
						if (demand.GetBooleanAttribute("optional", false))
						{
							announcementTime = 60 * roundVariablesNode.GetIntAttribute("optional_demand_announcement_duration_trading_periods", 0);
						}

						if (demand.GetIntAttribute("delay_countdown", 0) <= announcementTime)
						{
							status = "announcing";
						}
						else
						{
							status = "waiting";
						}

						attributes.Add(new AttributeValuePair ("delay_countdown", Math.Max(0, demand.GetIntAttribute("delay_countdown", 0) - dt)));
					}
					else
					{
						if (demand.GetIntAttribute("duration_countdown", 0) > 0)
						{
							string metStatus = "";

							status = "running";

							attributes.Add(new AttributeValuePair ("duration_countdown", Math.Max(0, demand.GetIntAttribute("duration_countdown", 0) - dt)));

							int secondsIntoMinute = timeNode.GetIntAttribute("seconds", 0) % 60;

							// It's always OK to bring us up.
							if ((businessService != null) && (businessService.GetAttribute("status") == "up"))
							{
								metStatus = "met";
							}
							// We should only go down if triggered directly by an attribute change on a business service...
							else if (((null != businessServiceRequested) && (demand.GetAttribute("business_service") == businessServiceRequested.GetAttribute("name")))
							// ...or if we don't have a business service for this demand...
							         || (businessService == null)
							// ...or if it's past the minute (in which case, even if we go up later, we've missed the demand).
							         || ((secondsIntoMinute > 0) && (secondsIntoMinute < 59)))
							{
								metStatus = "unmet";
							}

							if ((! string.IsNullOrEmpty(metStatus))
								&& (demand.GetAttribute("met_status") != metStatus))
							{
								attributes.Add(new AttributeValuePair ("met_status", metStatus));
							}

							attributes.Add(new AttributeValuePair ("duration_countdown", Math.Max(0, demand.GetIntAttribute("duration_countdown", 0) - dt)));

							if (demand.GetIntAttribute("duration_countdown", 0) == 0)
							{
								attributes.Add(new AttributeValuePair ("linger_countdown", 60));

								if (businessService != null)
								{
									orderExecutor.ReleaseVmInstance(businessService, true);
								}
							}
						}
						else if (demand.GetIntAttribute("linger_countdown", 0) > 0)
						{
							status = "lingering";
							attributes.Add(new AttributeValuePair ("linger_countdown", demand.GetIntAttribute("linger_countdown", 0) - 1));
						}
						else
						{
							status = "";
						}
					}
				}

				if (demand.GetAttribute("status") != status)
				{
					attributes.Add(new AttributeValuePair ("status", status));
				}

				if (attributes.Count > 0)
				{
					demand.SetAttributes(attributes);
				}
			}
		}
	}
}