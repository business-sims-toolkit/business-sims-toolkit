using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using Network;
using LibCore;

namespace Cloud.OpsEngine
{
	public class OrderExecutor : IDisposable
	{
		NodeTree model;
		Node roundVariablesNode;
		Node incomingOrders;
		Node turnover;
		
		OrderPlanner orderPlanner;
		Biller biller;
		VirtualMachineManager vmManager;

		public event EventHandler OrderProcessed;

		public OrderExecutor (NodeTree model, OrderPlanner orderPlanner, Biller biller, VirtualMachineManager vmManager)
		{
			this.model = model;
			this.orderPlanner = orderPlanner;
			this.biller = biller;
			this.vmManager = vmManager;

			roundVariablesNode = model.GetNamedNode("RoundVariables");

			turnover = model.GetNamedNode("Turnover");

			incomingOrders = model.GetNamedNode("IncomingOrders");
			incomingOrders.ChildAdded += new Node.NodeChildAddedEventHandler(incomingOrders_ChildAdded);
		}

		public void Dispose ()
		{
			incomingOrders.ChildAdded -= new Node.NodeChildAddedEventHandler(incomingOrders_ChildAdded);
		}

		void OnOrderProcessed ()
		{
			if (OrderProcessed != null)
			{
				OrderProcessed(this, EventArgs.Empty);
			}
		}

		void incomingOrders_ChildAdded (Node sender, Node child)
		{
			string type = child.GetAttribute("type");
			if (type.Equals("order", StringComparison.InvariantCultureIgnoreCase))
			{
				ProcessOrder(child);
			}
		}

		void ProcessOrder (Node order)
		{
			if (! roundVariablesNode.GetBooleanAttribute("round_started", false))
			{
				roundVariablesNode.SetAttribute("round_started", true);
			}

			switch (order.GetAttribute("order"))
			{
				case "commission_server":
					ProcessCommissionServerOrder(order);
					break;

				case "upgrade_storage":
					ProcessUpgradeStorageOrder(order);
					break;

				case "commission_service":
					ProcessCommissionServiceOrder(order);
					break;

				case "decommission_server":
					ProcessDecommissionServerOrder(order);
					break;

				case "delete_service":
					ProcessDeleteServiceOrder(order);
					break;

				case "delete_server":
					ProcessDeleteServerOrder(order);
					break;

				case "move_server":
					ProcessMoveServerOrder(order);
					break;

				case "set_optional_demands":
					ProcessSetOptionalDemandsOrder(order);
					break;

				default:
					Debug.Assert(order == null);
					break;
			}

			OnOrderProcessed();
		}

		void ProcessSetOptionalDemandsOrder (Node order)
		{
			Node timeNode = model.GetNamedNode("CurrentTime");
			int currentTime = timeNode.GetIntAttribute("seconds", 0);

			foreach (Node subOrder in order.getChildren())
			{
				Node demand = model.GetNamedNode(subOrder.GetAttribute("demand"));
				bool active = subOrder.GetBooleanAttribute("active", false);

				bool past;
				switch (demand.GetAttribute("status"))
				{
					case "inactive":
					case "announcing":
					case "waiting":
						past = false;
						break;

					case "running":
					case "lingering":
					default:
						past = true;
						break;
				}

				if ((demand.GetBooleanAttribute("active", false) != active)
					&& !past)
				{
					demand.SetAttribute("active", active);
					if (active)
					{
						demand.SetAttribute("delay_countdown", demand.GetIntAttribute("delay", 0) - currentTime);
					}
				}
			}
		}

		void DeleteServer (Node server)
		{
			// Remove any VMs instantiated on our CPUs.
			foreach (Node serverLinkToVmInstance in server.GetChildrenOfType("server_link_to_vm_instance"))
			{
				Node vmInstance = model.GetNamedNode(serverLinkToVmInstance.GetAttribute("vm_instance"));
				if (vmInstance != null)
				{
					Node vmInstanceLinkToServer = null;
					foreach (Node tryVmInstanceLinkToServer in vmInstance.GetChildrenOfType("vm_instance_link_to_server"))
					{
						if (tryVmInstanceLinkToServer.GetAttribute("server") == server.GetAttribute("name"))
						{
							vmInstanceLinkToServer = tryVmInstanceLinkToServer;
							break;
						}
					}

					Debug.Assert(vmInstanceLinkToServer != null);

					DeleteVmInstance(vmInstance);
				}
			}

			Node rack = server.Parent;
			string owner = rack.GetAttribute("owner");

			rack.DeleteChildTree(server);

			if (rack.GetChildrenOfType("server").Count == 0)
			{
				rack.SetAttribute("owner", "");
			}
		}

		void ProcessDeleteServerOrder (Node order)
		{
			Node server = model.GetNamedNode(order.GetAttribute("server"));
			Node rack = server.Parent;
			string owner = rack.GetAttribute("owner");
			DeleteServer(server);

			List<AttributeValuePair> attributes = new List<AttributeValuePair> ();
			attributes.Add(new AttributeValuePair ("type", "server_consolidation_record"));
			attributes.Add(new AttributeValuePair ("rack", rack.GetAttribute("name")));
			attributes.Add(new AttributeValuePair ("server_spec", server.GetAttribute("server_spec")));
			attributes.Add(new AttributeValuePair ("server_group", server.GetAttribute("server_group")));
			attributes.Add(new AttributeValuePair ("owner", owner));
			new Node (model.GetNamedNode("Server Consolidations"), "server_consolidation_record", "", attributes);
		}

		void ProcessMoveServerOrder (Node order)
		{
			Node server = model.GetNamedNode(order.GetAttribute("server"));
			Node destinationRack = model.GetNamedNode(order.GetAttribute("destination_rack"));
			Node sourceRack = server.Parent;

			List<string> owners = orderPlanner.GetOwnersOfServer(server);
			string serverOwner = "";
			if (owners.Count > 0)
			{
				serverOwner = owners[0];
				if (serverOwner == "traditional")
				{
					serverOwner = "online";
				}
			}

			model.MoveNode(server, destinationRack);

			if (destinationRack.GetAttribute("owner") == "")
			{
				destinationRack.SetAttribute("owner", serverOwner);
			}

			if (sourceRack.GetChildrenOfType("server").Count == 0)
			{
				sourceRack.SetAttribute("owner", "");
			}
		}

		void ProcessDeleteServiceOrder (Node order)
		{
			Node businessService = model.GetNamedNode(order.GetAttribute("service"));
			Debug.Assert(businessService != null);

			if (businessService.GetBooleanAttribute("is_dev", false))
			{
				Node business = businessService.Parent;

				int cost = businessService.GetIntAttribute("dev_cost", 0);

				List<AttributeValuePair> attributes = new List<AttributeValuePair> ();
				attributes.Add(new AttributeValuePair ("type", "bill"));
				attributes.Add(new AttributeValuePair ("bill_type", "development"));
				attributes.Add(new AttributeValuePair ("business_service", businessService.GetAttribute("name")));
				attributes.Add(new AttributeValuePair ("business", business.GetAttribute("name")));
				attributes.Add(new AttributeValuePair ("value", cost));
				biller.CreateOrUpdateTurnoverItem(attributes);

				business.SetAttribute("dev_budget_left", business.GetIntAttribute("dev_budget_left", 0) + cost);
			}

			ReleaseVmInstance(businessService, true);
			businessService.Parent.DeleteChildTree(businessService);
		}

		void ProcessDecommissionServerOrder (Node order)
		{
			Node server = model.GetNamedNode(order.GetAttribute("server"));
			Debug.Assert(server != null);

			DeleteServer(server);
		}

		void ProcessCommissionServerOrder (Node order)
		{
			Node rack = model.GetNamedNode(order.GetAttribute("rack"));
			Debug.Assert(rack != null);

			Node serverSpec = model.GetNamedNode(order.GetAttribute("server_type"));
			Debug.Assert(serverSpec != null);

			Node business = model.GetNamedNode(order.GetAttribute("business"));
			Debug.Assert(business != null);

			string owner = order.GetAttribute("owner");
			string serverName = order.GetAttribute("server_name");

			Debug.Assert(orderPlanner.GetFreePhysicalSpace(null, rack) >= serverSpec.GetIntAttribute("height_u", 0));

			// Record the number to be used next time we want to add a server to this rack.
			if (serverName.StartsWith(rack.GetAttribute("name")))
			{
				string serverSuffix = serverName.Substring(1 + rack.GetAttribute("name").Length);
				int nextSuffix = 1 + CONVERT.ParseInt(serverSuffix);

				if (rack.GetIntAttribute("next_created_server_index", 0) != nextSuffix)
				{
					rack.SetAttribute("next_created_server_index", nextSuffix);
				}
			}

			Debug.Assert(orderPlanner.CanRackHostOwner(null, rack, owner));
			if (string.IsNullOrEmpty(rack.GetAttribute("owner")))
			{
				rack.SetAttribute("owner", owner);
			}

			string serverGroup = serverSpec.GetAttribute("server_group");
			string specifiedServerGroup = order.GetAttribute("server_group");
			if (! string.IsNullOrEmpty(specifiedServerGroup))
			{
				serverGroup = specifiedServerGroup;
			}

			// Create the server.
			List<AttributeValuePair> attributes = new List<AttributeValuePair>();
			attributes.Add(new AttributeValuePair ("type", "server"));
			attributes.Add(new AttributeValuePair ("created_in_round", roundVariablesNode.GetIntAttribute("current_round", 0)));
			attributes.Add(new AttributeValuePair ("server_spec", serverSpec.GetAttribute("name")));
			attributes.Add(new AttributeValuePair ("server_group", serverGroup));
			attributes.Add(new AttributeValuePair ("height_u", serverSpec.GetIntAttribute("height_u", 0)));
			attributes.Add(new AttributeValuePair ("power_kw", serverSpec.GetDoubleAttribute("power_kw", 0)));
			attributes.Add(new AttributeValuePair ("opex", serverSpec.GetDoubleAttribute("opex", 0)));
			attributes.Add(new AttributeValuePair ("time_till_ready", serverSpec.GetIntAttribute("purchase_delay", 0)));
			attributes.Add(new AttributeValuePair ("cpus", serverSpec.GetIntAttribute("cpus", 0)));
			Node server = new Node (rack, "server", serverName, attributes);

			// Capex.
			if (! order.GetBooleanAttribute("is_free_replacement", false))
			{
				int time = model.GetNamedNode("CurrentTime").GetIntAttribute("seconds", 0);
				attributes.Clear();
				attributes.Add(new AttributeValuePair ("type", "bill"));
				attributes.Add(new AttributeValuePair ("bill_type", "capex"));
				attributes.Add(new AttributeValuePair ("capex_type", "server_creation"));
				attributes.Add(new AttributeValuePair ("owner", owner));
				attributes.Add(new AttributeValuePair ("server", serverName));
				attributes.Add(new AttributeValuePair ("business_service", order.GetAttribute("business_service")));
				attributes.Add(new AttributeValuePair ("business", business.GetAttribute("name")));
				attributes.Add(new AttributeValuePair ("value", -serverSpec.GetDoubleAttribute("capex", 0)));
				biller.CreateOrUpdateTurnoverItem(attributes);
			}
		}

		void ProcessUpgradeStorageOrder (Node order)
		{
			Node storageArray = model.GetNamedNode(order.GetAttribute("storage_array"));
			Debug.Assert(storageArray != null);

			Node business = model.GetNamedNode(order.GetAttribute("business"));
			Debug.Assert(business != null);

			Node businessService = model.GetNamedNode(order.GetAttribute("business_service"));
			Debug.Assert(businessService != null);

			// Upgrade the storage.
			double amount = order.GetDoubleAttribute("amount_gb", 0);
			storageArray.SetAttribute("total_capacity_gb", storageArray.GetDoubleAttribute("total_capacity_gb", 0) + amount);

			// Create a delay node.
			List<AttributeValuePair> attributes = new List<AttributeValuePair> ();
			attributes.Add(new AttributeValuePair ("type", "storage_array_upgrade_delay"));
			attributes.Add(new AttributeValuePair ("business_service", businessService.GetAttribute("name")));
			attributes.Add(new AttributeValuePair ("time_till_ready", storageArray.GetIntAttribute("upgrade_delay", 0)));
			new Node (storageArray, "storage_array_upgrade_delay", "", attributes);

			// Capex.
			string owner = "production";
			if (order.GetAttribute("stage") == "dev")
			{
				owner = "dev&test";
			}

			int time = model.GetNamedNode("CurrentTime").GetIntAttribute("seconds", 0);
			attributes.Clear();
			attributes.Add(new AttributeValuePair ("type", "bill"));
			attributes.Add(new AttributeValuePair ("bill_type", "capex"));
			attributes.Add(new AttributeValuePair ("capex_type", "storage_upgrade"));
			attributes.Add(new AttributeValuePair ("owner", owner));
			attributes.Add(new AttributeValuePair ("storage_array", storageArray.GetAttribute("name")));
			attributes.Add(new AttributeValuePair ("business_service", businessService.GetAttribute("name")));
			attributes.Add(new AttributeValuePair ("business", business.GetAttribute("name")));
			attributes.Add(new AttributeValuePair ("value", -storageArray.GetDoubleAttribute("upgrade_cost", 0)));
			biller.CreateOrUpdateTurnoverItem(attributes);
		}

		public void ReleaseVmInstance (Node businessService, bool deleteInstanceToo)
		{
			Debug.Assert(businessService.GetAttribute("type") == "business_service");

			string vmInstanceName = businessService.GetAttribute("vm_instance");
			if (! string.IsNullOrEmpty(vmInstanceName))
			{
				Node vmInstance = model.GetNamedNode(vmInstanceName);

				string owner = businessService.GetAttribute("owner");

				if (businessService.GetBooleanAttribute("is_placeholder", false)
					&& string.IsNullOrEmpty(businessService.GetAttribute("cloud")))
				{
					// Update the BAU CPU usage figures to reflect that they no longer include us.
					Node business = model.GetNamedNode(businessService.GetAttribute("business"));

					List<Node> bauFiguresToReduce = new List<Node> ();
					Node datacenter = model.GetNamedNode(business.GetAttribute("datacenter"));

					foreach (Node rack in datacenter.GetChildrenOfType("rack"))
					{
						Node rackBusinessService = model.GetNamedNode(CONVERT.Format("{0} Preexisting Services", rack.GetAttribute("name")));
						if ((rackBusinessService != null)
							&& (rackBusinessService.GetAttribute("owner") == owner))
						{
							Node bauFigures = model.GetNamedNode(CONVERT.Format("{0} Usage", rackBusinessService.GetAttribute("name")));
							bauFiguresToReduce.Add(bauFigures);
						}
					}

					foreach (Node cpuUsagePoint in businessService.GetChildrenOfType("service_cpu_usage_data_point"))
					{
						int time = cpuUsagePoint.GetIntAttribute("minute", 0) * 60;
						int cpusToRemove = cpuUsagePoint.GetIntAttribute("cpus_used", 0);

						foreach (Node bauGroup in bauFiguresToReduce)
						{
							Node dataPoint = null;
							foreach (Node bauDataPoint in bauGroup.GetChildrenOfType("business_as_usual_data_point"))
							{
								if (bauDataPoint.GetIntAttribute("time", 0) == time)
								{
									dataPoint = bauDataPoint;
									break;
								}
							}

							Debug.Assert(dataPoint != null);

							int cpusToRemoveFromThisDataPoint = Math.Min(dataPoint.GetIntAttribute("cpus_used", 0), cpusToRemove);
							if (cpusToRemoveFromThisDataPoint > 0)
							{
								dataPoint.SetAttribute("cpus_used", dataPoint.GetIntAttribute("cpus_used", 0) - cpusToRemoveFromThisDataPoint);
								cpusToRemove -= cpusToRemoveFromThisDataPoint;
							}

							if (cpusToRemove == 0)
							{
								break;
							}
						}

						Debug.Assert(cpusToRemove == 0);
					}
				}
				else
				{
					if (vmInstance != null)
					{
						foreach (Node vmInstanceLinkToServer in vmInstance.GetChildrenOfType("vm_instance_link_to_server"))
						{
							Node server = model.GetNamedNode(vmInstanceLinkToServer.GetAttribute("server"));
							int cpus = vmInstanceLinkToServer.GetIntAttribute("cpus", 0);

							foreach (Node serverLinkToVmInstance in server.GetChildrenOfType("server_link_to_vm_instance"))
							{
								if (serverLinkToVmInstance.GetAttribute("vm_instance") == vmInstanceName)
								{
									server.DeleteChildTree(serverLinkToVmInstance);
								}
							}

							vmInstance.DeleteChildTree(vmInstanceLinkToServer);
						}
					}
				}

				if (deleteInstanceToo)
				{
					businessService.SetAttribute("vm_instance", "");

					if (vmInstance != null)
					{
						vmInstance.SetAttribute("business_service", "");
					}
				}
			}

			businessService.SetAttribute("cloud", "");
		}

		bool IsCloudPublic (Node cloud)
		{
			List<Node> servers = vmManager.GetAllServersInCloud(cloud);

			foreach (Node server in servers)
			{
				if (! server.GetBooleanAttribute("is_cloud_server", false))
				{
					return false;
				}
			}

			if (servers.Count == 0)
			{
				return false;
			}

			return true;
		}

		void ProcessCommissionServiceOrder (Node order)
		{
			string businessServiceName;
			Node business;
			string owner;
			Node vmDefinition;
			Node vmInstance;
			Node businessService;
			Node serviceDefinition;

			Node service = model.GetNamedNode(order.GetAttribute("service"));
			bool redeploying = ((service != null) && (service.GetAttribute("type") == "business_service"));
			if (redeploying)
			{
				businessService = model.GetNamedNode(order.GetAttribute("service"));
				Debug.Assert(businessService != null);

				serviceDefinition = null;

				business = model.GetNamedNode(businessService.GetAttribute("business"));
				owner = businessService.GetAttribute("owner");

				if (owner == "traditional")
				{
					owner = "online";
					businessService.SetAttribute("owner", owner);
				}

				vmInstance = model.GetNamedNode(businessService.GetAttribute("vm_instance"));
				Debug.Assert(vmInstance != null);

				vmDefinition = model.GetNamedNode(vmInstance.GetAttribute("vm_spec"));
			}
			else
			{
				serviceDefinition = model.GetNamedNode(order.GetAttribute("service"));
				Debug.Assert(serviceDefinition != null);

				businessService = null;

				business = model.GetNamedNode(serviceDefinition.GetAttribute("business"));
				owner = serviceDefinition.GetAttribute("owner");
				vmDefinition = model.GetNamedNode(order.GetAttribute("vm_type"));

				vmInstance = null;

				string error;
				Debug.Assert(orderPlanner.IsVmSpecSuitableForBusinessServiceOrDefinition(serviceDefinition, vmDefinition, out error));
			}

			Debug.Assert(business != null);
			Debug.Assert(! string.IsNullOrEmpty(owner));
			Debug.Assert(vmDefinition != null);

			string devServiceName = order.GetAttribute("dev_service");
			Node devService = model.GetNamedNode(devServiceName);

			bool isDevTest = (order.GetAttribute("stage") == "dev");
			if (isDevTest)
			{
				Debug.Assert(devService == null);
				owner = "dev&test";
			}

			Node storageArray = model.GetNamedNode(order.GetAttribute("storage_array"));
			Debug.Assert(storageArray != null);

			if (! storageArray.GetBooleanAttribute("unlimited_storage", false))
			{
				Debug.Assert(orderPlanner.GetFreeStorage(null, storageArray) >= vmDefinition.GetDoubleAttribute("storage_required_gb", 0));
			}

			bool deployingToSaaS = false;
			Node deploymentCloud = model.GetNamedNode(order.GetAttribute("cloud"));
			if ((deploymentCloud != null)
				&& deploymentCloud.GetBooleanAttribute("is_public_cloud", false))
			{
				foreach (Node locationReference in deploymentCloud.GetChildrenOfType("cloud_location"))
				{
					Node rack = model.GetNamedNode(locationReference.GetAttribute("location"));
					foreach (Node server in rack.GetChildrenOfType("server"))
					{
						if (server.GetBooleanAttribute("saas", false))
						{
							deployingToSaaS = true;
							break;
						}
					}
				}
			}

			if (redeploying)
			{
				Node cloud = model.GetNamedNode(order.GetAttribute("cloud"));
				Debug.Assert(cloud != null);
				bool toBeInPublicCloud = IsCloudPublic(cloud);

				Node currentCloud = model.GetNamedNode(businessService.GetAttribute("cloud"));
				bool currentlyInPublicCloud = ((currentCloud != null)
											   && IsCloudPublic(currentCloud));

				// Generally we release all our CPUs, unless we're just switching from one public cloud
				// vendor to another (since that should be seamless, but releasing CPUs would bring us down briefly).
				if (! (currentlyInPublicCloud && toBeInPublicCloud))
				{
					ReleaseVmInstance(businessService, false);
				}

				List<AttributeValuePair> attributes = new List<AttributeValuePair> ();
				attributes.Add(new AttributeValuePair ("cloud", cloud.GetAttribute("name")));
				attributes.Add(new AttributeValuePair ("storage_array", storageArray.GetAttribute("name")));

				// HACK HACK HACK TODO REMOVEME
				if (businessService.GetAttribute("owner") == "traditional")
				{
					string newOwner;
					if (businessService.GetAttribute("common_service_name") == "Financial")
					{
						newOwner = "online";
					}
					else
					{
						newOwner = "floor";
					}

					attributes.Add(new AttributeValuePair ("owner", newOwner));
				}

				string cloudProvider = order.GetAttribute("cloud_provider");
				string buildType = order.GetAttribute("cloud_build_type");
				string cloudChargeModel = order.GetAttribute("cloud_charge_model");
				if (! string.IsNullOrEmpty(cloudProvider))
				{
					attributes.Add(new AttributeValuePair("cloud_provider", cloudProvider));
				}
				if (! string.IsNullOrEmpty(buildType))
				{
					attributes.Add(new AttributeValuePair("cloud_build_type", buildType));
				}
				if (! string.IsNullOrEmpty(cloudChargeModel))
				{
					attributes.Add(new AttributeValuePair("cloud_charge_model", cloudChargeModel));
				}

				businessService.SetAttributes(attributes);

				businessServiceName = businessService.GetAttribute("name");
			}
			else
			{
				if (isDevTest)
				{
					businessServiceName = serviceDefinition.GetAttribute("dev_service_name");
				}
				else
				{
					businessServiceName = serviceDefinition.GetAttribute("service_name");
				}

				// Instantiate VM.
				string vmInstanceName = businessServiceName + " VM Instance";

				Node vmInstances = model.GetNamedNode("VmInstances");
				List<AttributeValuePair> attributes = new List<AttributeValuePair> ();
				attributes.Add(new AttributeValuePair ("type", "vm_instance"));
				attributes.Add(new AttributeValuePair ("vm_spec", vmDefinition.GetAttribute("name")));
				attributes.Add(new AttributeValuePair ("opex", vmDefinition.GetIntAttribute("opex", 0)));
				attributes.Add(new AttributeValuePair ("business", business.GetAttribute("name")));
				attributes.Add(new AttributeValuePair ("business_service", businessServiceName));
				attributes.Add(new AttributeValuePair ("idle_cpus", vmDefinition.GetIntAttribute("cpus", 0)));

				if (! string.IsNullOrEmpty(serviceDefinition.GetAttribute("demand_name")))
				{
					attributes.Add(new AttributeValuePair ("demand_name", serviceDefinition.GetAttribute("demand_name")));
				}

				vmInstance = model.GetNamedNode(vmInstanceName);
				if (vmInstance != null)
				{
					vmInstance.SetAttributes(attributes);
				}
				else
				{
					vmInstance = new Node (vmInstances, "vm_instance", vmInstanceName, attributes);
				}

				// Create the business service.
				attributes.Clear();

				// Has this service already been implemented (in this business)?
				if (model.GetNamedNode(businessServiceName) != null)
				{
					// If so, change the name.
					attributes.Add(new AttributeValuePair("group_with_service", businessServiceName));

					string uniqueName = "";
					int count = 0;
					do
					{
						count++;
						uniqueName = CONVERT.Format("{0} Additional Demand {1}", businessServiceName, count);
					}
					while (model.GetNamedNode(uniqueName) != null);

					businessServiceName = uniqueName;
				}

				int instances = 1;
				if (! serviceDefinition.GetBooleanAttribute("is_new_service", false))
				{
					Node demand = model.GetNamedNode(serviceDefinition.GetAttribute("demand_name"));
					string attributeName = CONVERT.Format("round_{0}_instances", roundVariablesNode.GetIntAttribute("current_round", 0));
					instances = demand.GetIntAttribute(attributeName, 0);
				}

				if (isDevTest)
				{
					attributes.Add(new AttributeValuePair ("is_dev", true));
					attributes.Add(new AttributeValuePair ("production_service_name", serviceDefinition.GetAttribute("service_name")));
				}
				else
				{
					attributes.Add(new AttributeValuePair ("revenue_per_trade", serviceDefinition.GetDoubleAttribute("revenue_per_trade", 0)));
					attributes.Add(new AttributeValuePair ("trades_per_realtime_minute", instances * serviceDefinition.GetIntAttribute("trades_per_realtime_minute", 0)));
				}

				attributes.Add(new AttributeValuePair ("type", "business_service"));
				attributes.Add(new AttributeValuePair ("private_smi", serviceDefinition.GetIntAttribute("private_smi", 0)));
				attributes.Add(new AttributeValuePair ("short_name", serviceDefinition.GetAttribute("short_name")));
				attributes.Add(new AttributeValuePair ("desc", serviceDefinition.GetAttribute("desc")));
				attributes.Add(new AttributeValuePair ("common_service_name", serviceDefinition.GetAttribute("common_service_name")));
				attributes.Add(new AttributeValuePair ("business", business.GetAttribute("name")));
				attributes.Add(new AttributeValuePair ("owner", owner));
				attributes.Add(new AttributeValuePair ("vm_instance", vmInstanceName));
				attributes.Add(new AttributeValuePair ("dev_cost", serviceDefinition.GetIntAttribute("dev_cost", 0)));
				attributes.Add(new AttributeValuePair ("security", serviceDefinition.GetAttribute("security")));
				attributes.Add(new AttributeValuePair ("service_code", serviceDefinition.GetAttribute("service_code")));
				attributes.Add(new AttributeValuePair ("storage_array", storageArray.GetAttribute("name")));
				attributes.Add(new AttributeValuePair ("cpus_required", serviceDefinition.GetIntAttribute("cpus_required", 0)));
				attributes.Add(new AttributeValuePair ("memory_required_gb", serviceDefinition.GetDoubleAttribute("memory_required_gb", 0)));
				attributes.Add(new AttributeValuePair ("db_required", serviceDefinition.GetAttribute("db_required")));
				attributes.Add(new AttributeValuePair ("storage_required_gb", serviceDefinition.GetDoubleAttribute("storage_required_gb", 0)));
				attributes.Add(new AttributeValuePair ("group_with_service", serviceDefinition.GetAttribute("service_name")));
				attributes.Add(new AttributeValuePair ("is_regulation", serviceDefinition.GetBooleanAttribute("is_regulation", false)));
				attributes.Add(new AttributeValuePair ("is_new_service", serviceDefinition.GetBooleanAttribute("is_new_service", false)));
				attributes.Add(new AttributeValuePair ("can_deploy_private", serviceDefinition.GetBooleanAttribute("can_deploy_private", false)));
				attributes.Add(new AttributeValuePair ("can_deploy_iaas", serviceDefinition.GetBooleanAttribute("can_deploy_iaas", false)));
				attributes.Add(new AttributeValuePair ("can_deploy_saas", serviceDefinition.GetBooleanAttribute("can_deploy_saas", false)));
				attributes.Add(new AttributeValuePair ("created_in_round", roundVariablesNode.GetIntAttribute("current_round", 0)));
				attributes.Add(new AttributeValuePair ("available_in_cloud_panel_round_3", serviceDefinition.GetBooleanAttribute("available_in_cloud_panel_round_3", false)));
				attributes.Add(new AttributeValuePair ("available_in_cloud_panel_round_4", serviceDefinition.GetBooleanAttribute("available_in_cloud_panel_round_4", false)));
				if (! string.IsNullOrEmpty(serviceDefinition.GetAttribute("correct_build_code")))
				{
					attributes.Add(new AttributeValuePair ("correct_build_code", serviceDefinition.GetAttribute("correct_build_code")));
				}

				Node cloud = model.GetNamedNode(order.GetAttribute("cloud"));
				Debug.Assert(cloud != null);
				attributes.Add(new AttributeValuePair ("cloud", cloud.GetAttribute("name")));

				if (! serviceDefinition.GetBooleanAttribute("is_new_service", false))
				{
					attributes.Add(new AttributeValuePair ("demand_name", serviceDefinition.GetAttribute("demand_name")));
				}

				// We backdate development to start at the beginning of the current minute.
				int currentTime = model.GetNamedNode("CurrentTime").GetIntAttribute("seconds", 0);
				int secondsIntoCurrentMinute = currentTime % 60;
				int extraRoundingDelay = - secondsIntoCurrentMinute;
				if (isDevTest)
				{
					attributes.Add(new AttributeValuePair ("dev_countdown", extraRoundingDelay + serviceDefinition.GetIntAttribute("dev_test_time", 0)));

					int hardwareDelay = order.GetIntAttribute("hardware_delay", 0);
					if (hardwareDelay > 0)
					{
						attributes.Add(new AttributeValuePair ("hardware_delay", hardwareDelay));
					}
				}
				else
				{
					// Don't add the extra delay if we're relying on a dev service, as it will have the delay already!
					if ((devService != null)
						&& (devService.GetIntAttribute("dev_countdown", 0) > 0))
					{
						extraRoundingDelay = -1;
					}
					else
					{
						extraRoundingDelay += -1;
					}

					int handoverTime = roundVariablesNode.GetIntAttribute("handover_time", 0);

					int totalHandoverTime = handoverTime + extraRoundingDelay;

					// If we have a dev service that is delayed by hardware...
					if ((devService != null)
						&& (devService.GetIntAttribute("hardware_delay", 0) > 0))
					{
						// ...then we should be delayed too.
						totalHandoverTime += devService.GetIntAttribute("hardware_delay", 0) - 1;
					}

					int handoverFinishTime = currentTime + totalHandoverTime;

					int hardwareDelay = order.GetIntAttribute("hardware_delay", 0);
					if (hardwareDelay > 0)
					{
						totalHandoverTime += hardwareDelay;
						handoverFinishTime += hardwareDelay;

						attributes.Add(new AttributeValuePair ("hardware_delay", hardwareDelay));
					}

					// Don't have a handover delay if deploying to SaaS either!
					if (deployingToSaaS)
					{
						totalHandoverTime = 0;
                        handoverFinishTime = 0;
					}

					if (devService != null)
					{
						handoverFinishTime += devService.GetIntAttribute("dev_countdown", 0);
					}

					int handoverStartTime = handoverFinishTime - roundVariablesNode.GetIntAttribute("handover_time", 0);

					// Round handover start time to the nearest minute....
					handoverStartTime -= ((handoverStartTime + 30) % 60) - 30;

					// ...and finish time to the nearest minute, minuse one.
					handoverFinishTime -= (((handoverFinishTime + 30) % 60) - 30 + 1);

					attributes.Add(new AttributeValuePair ("handover_countdown", totalHandoverTime));
					attributes.Add(new AttributeValuePair ("handover_starts_at", handoverStartTime));
					if (handoverFinishTime > 0)
					{
						attributes.Add(new AttributeValuePair ("handover_finishes_at", handoverFinishTime));
					}
				}

				if ((devService != null) && (devService.GetBooleanAttribute("is_dev", false)))
				{
					attributes.Add(new AttributeValuePair ("requires_dev", devService.GetAttribute("name")));
				}

				string cloudProvider = order.GetAttribute("cloud_provider");
				string buildType = order.GetAttribute("cloud_build_type");
				string cloudChargeModel = order.GetAttribute("cloud_charge_model");
				if (! string.IsNullOrEmpty(cloudProvider))
				{
					attributes.Add(new AttributeValuePair ("cloud_provider", cloudProvider));
				}
				if (! string.IsNullOrEmpty(buildType))
				{
					attributes.Add(new AttributeValuePair ("cloud_build_type", buildType));
				}
				if (! string.IsNullOrEmpty(cloudChargeModel))
				{
					attributes.Add(new AttributeValuePair ("cloud_charge_model", cloudChargeModel));
				}

				businessService = new Node (business, "business_service", businessServiceName, attributes);

				// Trigger an attribute change on the business service node, to ensure we re-update any revenues.
				businessService.SetAttribute("desc", businessService.GetAttribute("desc"));

				// Development cost.
				if (isDevTest)
				{
					int cost = serviceDefinition.GetIntAttribute("dev_cost", 0);

					Debug.Assert(business.GetIntAttribute("dev_budget_left", 0) >= cost);

					int time = model.GetNamedNode("CurrentTime").GetIntAttribute("seconds", 0);

					attributes.Clear();
					attributes.Add(new AttributeValuePair ("type", "bill"));
					attributes.Add(new AttributeValuePair ("bill_type", "development"));
					attributes.Add(new AttributeValuePair ("business_service", serviceDefinition.GetAttribute("service_name")));
					attributes.Add(new AttributeValuePair ("business", business.GetAttribute("name")));
					attributes.Add(new AttributeValuePair ("value", -cost));
					biller.CreateOrUpdateTurnoverItem(attributes);

					business.SetAttribute("dev_budget_left", business.GetIntAttribute("dev_budget_left", 0) - cost);
				}
			}
		}

		Node GetDefinitionGivenServiceName (string serviceName)
		{
			foreach (Node serviceDefinition in model.GetNamedNode("New Services").GetChildrenOfType("new_service"))
			{
				if (serviceDefinition.GetAttribute("service_name") == serviceName)
				{
					return serviceDefinition;
				}
			}

			return null;
		}

		public void DeleteVmInstance (Node vmInstance)
		{
			foreach (Node vmInstanceLinkToServer in vmInstance.GetChildrenOfType("vm_instance_link_to_server"))
			{
				Node server = vmInstance.Tree.GetNamedNode(vmInstanceLinkToServer.GetAttribute("server"));
				foreach (Node serverLinkToVmInstance in server.GetChildrenOfType("server_link_to_vm_instance"))
				{
					if (serverLinkToVmInstance.GetAttribute("vm_instance") == vmInstance.GetAttribute("name"))
					{
						server.DeleteChildTree(serverLinkToVmInstance);
					}
				}

				vmInstance.DeleteChildTree(vmInstanceLinkToServer);
			}
		}
	}
}