using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

using Network;
using LibCore;

namespace Cloud.OpsEngine
{
	public class BusinessService : IDisposable
	{
		NodeTree model;
		Node businessServiceNode;
		Node timeNode;
		Node roundVariablesNode;

		BauManager bauManager;
		OrderExecutor orderExecutor;
		Biller biller;

		public BusinessService (NodeTree model, Node businessServiceNode, BauManager bauManager, OrderExecutor orderExecutor, Biller biller)
		{
			this.model = model;
			this.businessServiceNode = businessServiceNode;
			this.bauManager = bauManager;
			this.orderExecutor = orderExecutor;
			this.biller = biller;

			timeNode = model.GetNamedNode("CurrentTime");
			timeNode.AttributesChanged += new Node.AttributesChangedEventHandler (timeNode_AttributesChanged);

			roundVariablesNode = model.GetNamedNode("RoundVariables");

			businessServiceNode.AttributesChanged += new Node.AttributesChangedEventHandler (businessServiceNode_AttributesChanged);

			AdvanceTimers(0);
			AdvanceRevenue(true);
		}

		void businessServiceNode_AttributesChanged (Node sender, System.Collections.ArrayList attrs)
		{
			bool vmChanged = false;

			foreach (AttributeValuePair avp in attrs)
			{
				if (avp.Attribute == "vm_instance")
				{
					vmChanged = true;
				}
			}

			if (vmChanged)
			{
				AdvanceTimers(0);
			}
		}

		public void Dispose ()
		{
			timeNode.AttributesChanged -= new Node.AttributesChangedEventHandler (timeNode_AttributesChanged);
			businessServiceNode.AttributesChanged -= new Node.AttributesChangedEventHandler (businessServiceNode_AttributesChanged);
		}

		void timeNode_AttributesChanged (Node sender, System.Collections.ArrayList attrs)
		{
			bool timeChanged = false;
			foreach (AttributeValuePair avp in attrs)
			{
				if (avp.Attribute == "seconds")
				{
					timeChanged = true;
					break;
				}
			}

			if (timeChanged)
			{
				AdvanceTimers(1);

				int seconds = timeNode.GetIntAttribute("seconds", 0);
				int length = timeNode.GetIntAttribute("round_duration", 0);

				if ((seconds % 60) == 1)
				{
					AdvanceRevenue(false);
				}
			}
		}

		void AdvanceTimers (int dt)
		{
			using (NodeChangeTransaction transaction = businessServiceNode.CreateTransaction())
			{
				string status = "unknown";

				string vmInstanceName = businessServiceNode.GetAttribute("vm_instance");
				Node vmInstance = model.GetNamedNode(vmInstanceName);

				// Decrement our hardware delay, if any (this is used only by production services waiting on us if we're
				// a dev service).
				if (businessServiceNode.GetIntAttribute("hardware_delay", 0) > 0)
				{
					businessServiceNode.SetAttribute("hardware_delay", businessServiceNode.GetIntAttribute("hardware_delay", 0) - 1);
				}

				if (vmInstance == null)
				{
					status = "no_vm_instance";
				}
				else
				{
					// VM instances can be delayed because they are waiting on storage coming online.
					int timeTillReady = 0;

					bool waitingOnDev = false;

					Node storageArray = model.GetNamedNode(businessServiceNode.GetAttribute("storage_array"));
					if (storageArray != null)
					{
						foreach (Node delay in storageArray.GetChildrenOfType("storage_array_upgrade_delay"))
						{
							if (delay.GetAttribute("business_service") == businessServiceNode.GetAttribute("name"))
							{
								timeTillReady = Math.Max(timeTillReady, delay.GetIntAttribute("time_till_ready", 0));
							}
						}
					}

					if (timeTillReady > 0)
					{
						status = "waiting_on_storage";
					}
					else
					{
						int cpusUp = 0;
						foreach (Node vmInstanceLinkToServer in vmInstance.GetChildrenOfType("vm_instance_link_to_server"))
						{
							Node server = model.GetNamedNode(vmInstanceLinkToServer.GetAttribute("server"));
							int cpus = vmInstanceLinkToServer.GetIntAttribute("cpus", 0);

							// Servers take time to come up.
							if (server.GetIntAttribute("time_till_ready", 0) > 0)
							{
								status = "waiting_on_server";
								break;
							}

							// They can also be down if the relevant cloud provider is down.
							if (server.GetBooleanAttribute("is_cloud_server", false))
							{
								Node cloudProvider = model.GetNamedNode(businessServiceNode.GetAttribute("cloud_provider"));
								if ((cloudProvider == null) || (!cloudProvider.GetBooleanAttribute("available", false)))
								{
									break;
								}
								else
								{
									cpusUp += cpus;
								}
							}
							else
							{
								cpusUp += cpus;
							}
						}

						int cpusNeeded = bauManager.GetCpusNeeded(businessServiceNode, timeNode.GetIntAttribute("seconds", 0), businessServiceNode.GetBooleanAttribute("is_dev", false));
						if (cpusUp >= cpusNeeded)
						{
							status = "up";
						}

						if (businessServiceNode.GetBooleanAttribute("is_dev", false))
						{
							if (status == "up")
							{
								// Dev services have a countdown that ticks down once they are up and running.
								int devCountdown = businessServiceNode.GetIntAttribute("dev_countdown", 0);
								devCountdown = Math.Max(0, devCountdown - dt);

								if (devCountdown != businessServiceNode.GetIntAttribute("dev_countdown", 0))
								{
									businessServiceNode.SetAttribute("dev_countdown", devCountdown);
								}

								if (devCountdown == 0)
								{
									if (businessServiceNode.GetBooleanAttribute("is_dev", false))
									{
										orderExecutor.ReleaseVmInstance(businessServiceNode, true);
										businessServiceNode.SetAttribute("has_been_developed", true);
									}
								}
							}
						}
						else
						{
							string devDependencyName = businessServiceNode.GetAttribute("requires_dev");
							if (devDependencyName != "")
							{
								if (!IsRunningUnderSaaS())
								{
									Node devDependency = model.GetNamedNode(devDependencyName);
									waitingOnDev = true;

									switch (devDependency.GetAttribute("status"))
									{
										case "waiting_on_storage":
											status = "waiting_on_dev_storage";
											break;

										case "waiting_on_server":
											status = "waiting_on_dev_server";
											break;

										case "up":
										default:
											if (devDependency.GetBooleanAttribute("has_been_developed", false))
											{
												waitingOnDev = false;
											}
											else
											{
												status = "waiting_on_dev";
											}
											break;
									}
								}
							}
							else
							{
								if (HasSomeCpus()
									&& (!IsRunningUnderSaaS())
									&& !businessServiceNode.GetBooleanAttribute("has_been_developed", false))
								{
									businessServiceNode.SetAttribute("has_been_developed", true);
								}
							}

							if (!waitingOnDev)
							{
								int handoverCountdown = Math.Max(0, businessServiceNode.GetIntAttribute("handover_countdown", 0) - dt);
								if (handoverCountdown != businessServiceNode.GetIntAttribute("handover_countdown", 0))
								{
									businessServiceNode.SetAttribute("handover_countdown", handoverCountdown);
								}

								int time = timeNode.GetIntAttribute("seconds", 0);
								if (time < businessServiceNode.GetIntAttribute("handover_starts_at", 0))
								{
									status = "waiting_on_start_of_handover";
								}
								else if (!String.IsNullOrEmpty(businessServiceNode.GetAttribute("handover_finishes_at"))
									&& 0 <= businessServiceNode.GetIntAttribute("handover_finishes_at", -1)
									&& time <= businessServiceNode.GetIntAttribute("handover_finishes_at", 0))
								{
									status = "waiting_on_handover";
								}
								else if (status == "unknown" && businessServiceNode.GetAttribute("status") == "waiting_on_handover")
								{
									// Cannot have an unknown state on transition (between period 59 of one trading period and the next)
									status = businessServiceNode.GetAttribute("status");
								}
							}
						}
					}
				}

				if (businessServiceNode.GetAttribute("status") != status)
				{
					businessServiceNode.SetAttribute("status", status);
				}
			}
		}

		void AdvanceRevenue (bool calculatePotentialRevenueOnly)
		{
			using (NodeChangeTransaction transaction = businessServiceNode.CreateTransaction())
			{
				int minutes = 1;
				int time = timeNode.GetIntAttribute("seconds", 0);
				int revenuePerTrade = businessServiceNode.GetIntAttribute("revenue_per_trade", 0);
				int tradesPerRealMinute = businessServiceNode.GetIntAttribute("trades_per_realtime_minute", 0);

				Node turnoverNode = model.GetNamedNode("Turnover");
				int round = roundVariablesNode.GetIntAttribute("current_round", 0);
				string groupWith = businessServiceNode.GetAttribute("group_with_service", businessServiceNode.GetAttribute("name"));
				string commonServiceName = businessServiceNode.GetAttribute("common_service_name");

				string vmInstanceName = businessServiceNode.GetAttribute("vm_instance");
				Node vmInstance = model.GetNamedNode(vmInstanceName);

				if (vmInstance != null)
				{
					bool allServersUp = (businessServiceNode.GetAttribute("status") == "up");

					bool devDependencyUp = true;
					string devDependencyName = businessServiceNode.GetAttribute("requires_dev");
					if (devDependencyName != "")
					{
						if (IsRunningUnderSaaS())
						{
							devDependencyUp = true;
						}
						else
						{
							Node devDependency = model.GetNamedNode(devDependencyName);

							if (devDependency.GetIntAttribute("dev_countdown", 0) > 0)
							{
								devDependencyUp = false;
							}
							else
							{
								businessServiceNode.SetAttribute("requires_dev", "");
							}
						}
					}

					if (time <= businessServiceNode.GetIntAttribute("handover_finishes_at", 0))
					{
						devDependencyUp = false;
					}

					bool openForBusiness = bauManager.IsBusinessServiceTrading(businessServiceNode, time);

					bool generatingIncome;
					if (businessServiceNode.GetBooleanAttribute("is_new_service", false) || businessServiceNode.GetBooleanAttribute("is_preexisting", false))
					{
						generatingIncome = true;
					}
					else if (businessServiceNode.GetBooleanAttribute("is_dev", false))
					{
						generatingIncome = true;
					}
					else if (businessServiceNode.GetBooleanAttribute("is_placeholder", false))
					{
						generatingIncome = false;
					}
					else
					{
						Node demandNode = model.GetNamedNode(businessServiceNode.GetAttribute("demand_name"));
						generatingIncome = demandNode.GetBooleanAttribute("active", false)
										   && (demandNode.GetIntAttribute("delay_countdown", 0) == 0);

						tradesPerRealMinute = demandNode.GetIntAttribute("trades_per_realtime_minute", 0);

						string attributeName = CONVERT.Format("round_{0}_instances", roundVariablesNode.GetIntAttribute("current_round", 0));
						int instances = demandNode.GetIntAttribute(attributeName, 0);
						tradesPerRealMinute *= instances;
					}

					int smi = 0;

					// We generate opex fees if in the public cloud.
					if (IsRunningUnderIaaS())
					{
						string chargeModel = businessServiceNode.GetAttribute("cloud_charge_model");
						Debug.Assert(!string.IsNullOrEmpty(chargeModel));

						Node cloudProvider = model.GetNamedNode(businessServiceNode.GetAttribute("cloud_provider"));
						Debug.Assert(cloudProvider != null);

						string security = businessServiceNode.GetAttribute("security");
						Debug.Assert(!string.IsNullOrEmpty(security));

						Node cloudChargeOption = GetMatchingCloudChargeOption(cloudProvider, security, chargeModel);
						Debug.Assert(cloudChargeOption != null);

						smi = cloudChargeOption.GetIntAttribute("smi", 0);

						if (!calculatePotentialRevenueOnly)
						{
							if (chargeModel == "on_demand")
							{
								if (openForBusiness || (businessServiceNode.GetAttribute("owner") == "traditional"))
								{
									List<AttributeValuePair> attributes = new List<AttributeValuePair>();
									attributes.Add(new AttributeValuePair("type", "bill"));
									attributes.Add(new AttributeValuePair("business_service", groupWith));
									attributes.Add(new AttributeValuePair("business_service_ungrouped", businessServiceNode.GetAttribute("name")));
									attributes.Add(new AttributeValuePair("business", businessServiceNode.Parent.GetAttribute("name")));
									attributes.Add(new AttributeValuePair("owner", "online"));
									attributes.Add(new AttributeValuePair("bill_type", "opex"));
									attributes.Add(new AttributeValuePair("opex_type", "iaas_on_demand_fee"));
									attributes.Add(new AttributeValuePair("value", -cloudChargeOption.GetIntAttribute("cost_per_realtime_minute", 0)));

									biller.CreateOrUpdateTurnoverItem(attributes);
								}
							}
							else
							{
								if (!businessServiceNode.GetBooleanAttribute("iaas_reservation_opex_charged", false))
								{
									List<AttributeValuePair> attributes = new List<AttributeValuePair>();
									attributes.Add(new AttributeValuePair("type", "bill"));
									attributes.Add(new AttributeValuePair("business_service", groupWith));
									attributes.Add(new AttributeValuePair("business_service_ungrouped", businessServiceNode.GetAttribute("name")));
									attributes.Add(new AttributeValuePair("business", businessServiceNode.Parent.GetAttribute("name")));
									attributes.Add(new AttributeValuePair("owner", "online"));
									attributes.Add(new AttributeValuePair("bill_type", "opex"));
									attributes.Add(new AttributeValuePair("opex_type", "iaas_reservation_fee"));
									attributes.Add(new AttributeValuePair("value", -cloudChargeOption.GetIntAttribute("cost_per_round", 0)));

									biller.CreateOrUpdateTurnoverItem(attributes);

									businessServiceNode.SetAttribute("iaas_reservation_opex_charged", true);
								}
							}
						}
					}
					else if (IsRunningUnderSaaS())
					{
						string chargeModel = businessServiceNode.GetAttribute("cloud_charge_model");
						Node cloudProvider = model.GetNamedNode(businessServiceNode.GetAttribute("cloud_provider"));
						Node cloudChargeOption = null;
						foreach (Node tryCloudChargeOption in cloudProvider.GetChildrenOfType("saas_cloud_option"))
						{
							if (tryCloudChargeOption.GetAttribute("common_service_name") == commonServiceName)
							{
								cloudChargeOption = tryCloudChargeOption;
								break;
							}
						}

						Debug.Assert(cloudChargeOption != null);

						smi = cloudChargeOption.GetIntAttribute("smi", 0);

						double fee = 0;
						int users = businessServiceNode.GetIntAttribute("users", 0);
						if (users > 0)
						{
							fee = users * cloudChargeOption.GetDoubleAttribute("cost_per_user_per_round", 0) * minutes / timeNode.GetDoubleAttribute("round_duration", 0);
						}
						else
						{
							int trades = (openForBusiness ? businessServiceNode.GetIntAttribute("trades_per_realtime_minute", 0) : 0);
							fee = trades * cloudChargeOption.GetDoubleAttribute("cost_per_trade", 0);
						}

						if (!calculatePotentialRevenueOnly)
						{
							if (fee > 0)
							{
								List<AttributeValuePair> attributes = new List<AttributeValuePair>();
								attributes.Add(new AttributeValuePair("type", "bill"));
								attributes.Add(new AttributeValuePair("business_service", groupWith));
								attributes.Add(new AttributeValuePair("business_service_ungrouped", businessServiceNode.GetAttribute("name")));
								attributes.Add(new AttributeValuePair("business", businessServiceNode.Parent.GetAttribute("name")));
								attributes.Add(new AttributeValuePair("owner", "online"));
								attributes.Add(new AttributeValuePair("bill_type", "opex"));
								attributes.Add(new AttributeValuePair("opex_type", "saas_fee"));
								attributes.Add(new AttributeValuePair("value", -fee));

								biller.CreateOrUpdateTurnoverItem(attributes);
							}
						}
					}
					else
					{
						smi = businessServiceNode.GetIntAttribute("private_smi", 0);
					}

					if (smi < 56)
					{
						tradesPerRealMinute -= 20;
					}
					else if (smi < 61)
					{
						tradesPerRealMinute -= 10;
					}
					tradesPerRealMinute = Math.Max(0, tradesPerRealMinute);

					if (!calculatePotentialRevenueOnly)
					{
						if (allServersUp && devDependencyUp && generatingIncome && openForBusiness)
						{
							int trades = tradesPerRealMinute * minutes;
							int revenue = revenuePerTrade * trades;

							if (revenue != 0)
							{
								List<AttributeValuePair> attributes = new List<AttributeValuePair>();
								attributes.Add(new AttributeValuePair("type", "trade"));

								string tradeType;
								if (businessServiceNode.GetBooleanAttribute("is_new_service", false))
								{
									if (businessServiceNode.GetIntAttribute("created_in_round", 0) == round)
									{
										tradeType = "new_service";
									}
									else
									{
										tradeType = "old_service";
									}
								}
								else
								{
									tradeType = "demand";
								}

								attributes.Add(new AttributeValuePair("business_service", groupWith));
								attributes.Add(new AttributeValuePair("business_service_ungrouped", businessServiceNode.GetAttribute("name")));
								attributes.Add(new AttributeValuePair("business", businessServiceNode.Parent.GetAttribute("name")));
								attributes.Add(new AttributeValuePair("time", time));
								attributes.Add(new AttributeValuePair("trades", trades));
								attributes.Add(new AttributeValuePair("trade_type", tradeType));
								attributes.Add(new AttributeValuePair("value", revenue));

								new Node(turnoverNode, "trade", CONVERT.Format("turnover_item_{0}", 1 + turnoverNode.getChildren().Count), attributes);
							}
						}

						businessServiceNode.SetAttribute("revenue_updated_to", time);
					}
				}

				// Work out how much we've earned already...
				double revenueEarned = 0;
				foreach (Node trade in turnoverNode.getChildren())
				{
					if ((trade.GetAttribute("business_service") == businessServiceNode.GetAttribute("name"))
						&& (businessServiceNode.GetIntAttribute("created_in_round", 0) == round))
					{
						double value = trade.GetDoubleAttribute("value", 0);
						if (value > 0)
						{
							revenueEarned += value;
						}
					}
				}
				if (businessServiceNode.GetDoubleAttribute("revenue_earned", 0) != revenueEarned)
				{
					businessServiceNode.SetAttribute("revenue_earned", revenueEarned);
				}

				// ...and how much we will earn.
				double potentialExtraRevenue = 0;

				if (businessServiceNode.GetIntAttribute("created_in_round", 0) == round)
				{
					int nextMinute = businessServiceNode.GetIntAttribute("revenue_updated_to", time) + 59;
					nextMinute = nextMinute - (nextMinute % 60);

					for (int t = nextMinute; t < timeNode.GetIntAttribute("round_duration", 0); t += 60)
					{
						if (bauManager.IsBusinessServiceTrading(businessServiceNode, t))
						{
							potentialExtraRevenue += (revenuePerTrade * tradesPerRealMinute);
						}
					}
				}
				if (businessServiceNode.GetDoubleAttribute("potential_extra_revenue", 0) != potentialExtraRevenue)
				{
					businessServiceNode.SetAttribute("potential_extra_revenue", potentialExtraRevenue);
				}
			}
		}

		Node GetMatchingCloudChargeOption (Node cloudProvider, string security, string chargeModel)
		{
			foreach (Node tryCloudChargeOption in cloudProvider.GetChildrenOfType("iaas_cloud_option"))
			{
				if ((tryCloudChargeOption.GetAttribute("charge_model") == chargeModel)
					&& (tryCloudChargeOption.GetAttribute("security") == security))
				{
					return tryCloudChargeOption;
				}
			}

			// If we didn't find one, maybe we can find one at a higher security level.
			if (security == "low")
			{
				foreach (Node tryCloudChargeOption in cloudProvider.GetChildrenOfType("iaas_cloud_option"))
				{
					if ((tryCloudChargeOption.GetAttribute("charge_model") == chargeModel)
						&& (tryCloudChargeOption.GetAttribute("security") == "medium"))
					{
						return tryCloudChargeOption;
					}
				}
			}

			return null;
		}

		bool HasSomeCpus ()
		{
			string vmInstanceName = businessServiceNode.GetAttribute("vm_instance");
			Node vmInstance = model.GetNamedNode(vmInstanceName);

			int cpus = 0;
			if (vmInstance != null)
			{
				foreach (Node vmInstanceLinkToServer in vmInstance.GetChildrenOfType("vm_instance_link_to_server"))
				{
					cpus += vmInstanceLinkToServer.GetIntAttribute("cpus", 0);
				}
			}

			return (cpus > 0);
		}

		bool IsRunningUnderSaaS ()
		{
			string vmInstanceName = businessServiceNode.GetAttribute("vm_instance");
			Node vmInstance = model.GetNamedNode(vmInstanceName);
			bool haveSomeCpus = false;

			foreach (Node vmInstanceLinkToServer in vmInstance.GetChildrenOfType("vm_instance_link_to_server"))
			{
				Node server = model.GetNamedNode(vmInstanceLinkToServer.GetAttribute("server"));
				haveSomeCpus = (vmInstanceLinkToServer.GetIntAttribute("cpus", 0) > 0);

				if (! server.GetBooleanAttribute("saas", false))
				{
					return false;
				}
			}

			return haveSomeCpus;
		}

		bool IsRunningUnderIaaS ()
		{
			string vmInstanceName = businessServiceNode.GetAttribute("vm_instance");
			Node vmInstance = model.GetNamedNode(vmInstanceName);
			bool haveSomeCpus = false;

			foreach (Node vmInstanceLinkToServer in vmInstance.GetChildrenOfType("vm_instance_link_to_server"))
			{
				Node server = model.GetNamedNode(vmInstanceLinkToServer.GetAttribute("server"));
				haveSomeCpus = (vmInstanceLinkToServer.GetIntAttribute("cpus", 0) > 0);

				if (! server.GetBooleanAttribute("iaas", false))
				{
					return false;
				}
			}

			return haveSomeCpus;
		}
	}
}