using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

using LibCore;
using CoreUtils;
using Network;

namespace Cloud.OpsEngine
{
	public class BauManager : IDisposable
	{
		NodeTree model;

		List<Node> watchedBauDataPoints;
		Dictionary<Node, TimeLog<bool>> businessServiceToTimeToOpenState;
		Dictionary<Node, TimeLog<int>> businessServiceToTimeToUsedCpus;

		public BauManager (NodeTree model)
		{
			this.model = model;

			watchedBauDataPoints = new List<Node> ();
			foreach (Node node in model.GetNodesWithAttributeValue("type", "business_as_usual_data_point"))
			{
				watchedBauDataPoints.Add(node);
				node.AttributesChanged += new Node.AttributesChangedEventHandler (node_AttributesChanged);
			}

			ReadUsedCpuFiguresFromNetwork();
		}

		void node_AttributesChanged (Node sender, System.Collections.ArrayList attrs)
		{
			ReadUsedCpuFiguresFromNetwork();
		}

		void ReadUsedCpuFiguresFromNetwork ()
		{
			businessServiceToTimeToOpenState = new Dictionary<Node, TimeLog<bool>> ();
			businessServiceToTimeToUsedCpus = new Dictionary<Node, TimeLog<int>> ();

			foreach (Node business in model.GetNodesWithAttributeValue("type", "business"))
			{
				Node bauNode = ((Node []) business.GetChildrenOfType("business_as_usual").ToArray(typeof (Node)))[0];
				foreach (Node businessServiceBauNode in bauNode.GetChildrenOfType("service_business_as_usual"))
				{
					string owner = businessServiceBauNode.GetAttribute("owner");
					Node businessService = model.GetNamedNode(businessServiceBauNode.GetAttribute("business_service"));
					
					businessServiceToTimeToOpenState.Add(businessService, new TimeLog<bool> ());
					businessServiceToTimeToUsedCpus.Add(businessService, new TimeLog<int> ());

					foreach (Node dataPoint in businessServiceBauNode.GetChildrenOfType("business_as_usual_data_point"))
					{
						int dataPointTime = dataPoint.GetIntAttribute("time", 0);

						businessServiceToTimeToOpenState[businessService].Add(dataPointTime, dataPoint.GetBooleanAttribute("open", false));
						businessServiceToTimeToUsedCpus[businessService].Add(dataPointTime, dataPoint.GetIntAttribute("cpus_used", 0));
					}
				}
			}
		}

		public void Dispose ()
		{
			foreach (Node node in watchedBauDataPoints)
			{
				node.AttributesChanged -= new Node.AttributesChangedEventHandler (node_AttributesChanged);
			}
		}

		public bool IsBusinessServiceTrading (Node businessServiceOrDefinition, int time)
		{
			Node business = model.GetNamedNode(businessServiceOrDefinition.GetAttribute("business"));

			// Demands stop using CPUs after they complete.
			bool serviceStillRunning = true;
			Node demand = model.GetNamedNode(businessServiceOrDefinition.GetAttribute("demand_name"));
			if (demand != null)
			{
				if (demand.GetBooleanAttribute("active", false))
				{
					int startTime = demand.GetIntAttribute("delay", 0);
					int endTime = startTime + demand.GetIntAttribute("duration", 0);

					Node roundVariables = model.GetNamedNode("RoundVariables");
					serviceStillRunning = (time >= startTime) && (time < endTime);
				}
				else
				{
					serviceStillRunning = false;
				}
			}

			// New Floor services don't trade at certain times.
			string owner = businessServiceOrDefinition.GetAttribute("owner");
			bool ownerOpen = true;
			foreach (Node tryBusinessService in businessServiceToTimeToOpenState.Keys)
			{
				if ((model.GetNamedNode(tryBusinessService.GetAttribute("business")) == business)
					&& (tryBusinessService.GetAttribute("owner") == owner))
				{
					ownerOpen = businessServiceToTimeToOpenState[tryBusinessService].GetLastValueOnOrBefore(time);
					break;
				}
			}

			// But dev work is ongoing.
			if (owner == "dev&test")
			{
				ownerOpen = true;
			}

			return ownerOpen && serviceStillRunning;
		}

		public int GetCpusNeeded (Node businessServiceOrDefinition, int time, bool devTest)
		{
			return GetCpusNeeded(businessServiceOrDefinition, time, false, devTest);
		}

		public int GetCpusNeeded (Node businessServiceOrDefinition, int time, bool ignoreHandoverIfDemand, bool devTest)
		{
			Node business = model.GetNamedNode(businessServiceOrDefinition.GetAttribute("business"));
			string owner = businessServiceOrDefinition.GetAttribute("owner");

			// We know how many it needs.
			int cpusNeeded = businessServiceOrDefinition.GetIntAttribute("cpus_required", 0);

			// If it's been deployed, then we know how many CPUs it's actually been assigned.
			Node vmInstance = model.GetNamedNode(businessServiceOrDefinition.GetAttribute("vm_instance"));
			if (vmInstance != null)
			{
				Node vmDefinition = model.GetNamedNode(vmInstance.GetAttribute("vm_spec"));
				cpusNeeded = vmDefinition.GetIntAttribute("cpus", 0);
			}

			// Is it a demand?  If so, it might need extra CPUs.
			Node demand = model.GetNamedNode(businessServiceOrDefinition.GetAttribute("demand_name"));
			if (demand != null)
			{
				Node roundVariablesNode = model.GetNamedNode("RoundVariables");

				string attributeName = CONVERT.Format("round_{0}_instances", roundVariablesNode.GetIntAttribute("current_round", 0));
				cpusNeeded *= demand.GetIntAttribute(attributeName, 0);
			}

			// Preexisting (BAU) services have shaped profiles...
			if (businessServiceOrDefinition.GetBooleanAttribute("is_preexisting", false)
				&& businessServiceToTimeToUsedCpus.ContainsKey(businessServiceOrDefinition))
			{
				cpusNeeded = businessServiceToTimeToUsedCpus[businessServiceOrDefinition].GetLastValueOnOrBefore(time);
			}
			// ...and other services may cut out when the floor is closed or their demands end.
			else if (! devTest)
			{				
				foreach (Node usagePoint in businessServiceOrDefinition.GetChildrenOfType("service_cpu_usage_data_point"))
				{
					if ((time >= (usagePoint.GetIntAttribute("minute", 0) * 60))
						&& (time < ((1 + usagePoint.GetIntAttribute("minute", 0)) * 60)))
					{
						cpusNeeded = usagePoint.GetIntAttribute("cpus_used", 0);
					}
				}

				if (! IsBusinessServiceTrading(businessServiceOrDefinition, time))
				{
					cpusNeeded = 0;
				}

				Node currentTime = model.GetNamedNode("CurrentTime");

				// We don't need CPUs if we're waiting on dev...
				Node devService = model.GetNamedNode(businessServiceOrDefinition.GetAttribute("requires_dev"));
                int devFinishesAt = 0;
                if (devService != null)
                {
                    devFinishesAt = devService.GetIntAttribute("dev_countdown", 0) + currentTime.GetIntAttribute("seconds", 0);
                    if (devFinishesAt > time)
                    {
                        cpusNeeded = 0;
                    }
                }

                // ...or on handover.
				if ((! ignoreHandoverIfDemand)
					|| string.IsNullOrEmpty(businessServiceOrDefinition.GetAttribute("demand_name")))
				{
					string handoverFinishesAtString = businessServiceOrDefinition.GetAttribute("handover_finishes_at");

					if (! string.IsNullOrEmpty(handoverFinishesAtString))
					{
						int handoverFinishesAt = CONVERT.ParseInt(handoverFinishesAtString);

						if ((handoverFinishesAt % 60) > 0)
						{
							handoverFinishesAt += (60 - (handoverFinishesAt % 60));
						}

						if (time < handoverFinishesAt)
						{
							cpusNeeded = 0;
						}
					}
				}
            }
			
			return cpusNeeded;
		}
	}
}