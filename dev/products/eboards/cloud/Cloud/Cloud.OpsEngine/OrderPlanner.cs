using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using Network;
using LibCore;

namespace Cloud.OpsEngine
{
	public class OrderPlanner
	{
		NodeTree model;
		Node roundVariablesNode;

		BauManager bauManager;
		VirtualMachineManager vmManager;

		public OrderPlanner (NodeTree model, BauManager bauManager, VirtualMachineManager vmManager)
		{
			this.model = model;
			this.bauManager = bauManager;
			this.vmManager = vmManager;

			roundVariablesNode = model.GetNamedNode("RoundVariables");
		}

		void AddConfirmation (Node plannedOrders, string stage, string messageFormat, params object [] args)
		{
			AddMessage(plannedOrders, stage, "confirmation", messageFormat, args);
		}

		public void AddError (Node plannedOrders, string stage, string messageFormat, params object [] args)
		{
			AddMessage(plannedOrders, stage, "error", messageFormat, args);
		}

		void AddInfo (Node plannedOrders, string stage, string messageFormat, params object [] args)
		{
			AddMessage(plannedOrders, stage, "info", messageFormat, args);
		}

		void AddMessage (Node plannedOrders, string stage, string messageType, string messageFormat, params object [] args)
		{
			List<AttributeValuePair> attributes = new List<AttributeValuePair> ();
			attributes.Add(new AttributeValuePair ("type", messageType));
			attributes.Add(new AttributeValuePair ("stage", stage));
			attributes.Add(new AttributeValuePair ("message", CONVERT.Format(messageFormat, args)));
			new Node (plannedOrders, messageType, "", attributes);
		}

		Node GetDefaultNewServerDefinition ()
		{
			Node serverDefinitions = model.GetNamedNode("ServerSpecs");
			Node defaultDefinition = null;

			foreach (Node serverDefinition in serverDefinitions.GetChildrenOfType("server_spec"))
			{
				if ((defaultDefinition == null) || serverDefinition.GetBooleanAttribute("is_default", false))
				{
					defaultDefinition = serverDefinition;
				}
			}

			return defaultDefinition;		
		}

		public bool IsVmSpecSuitableForBusinessServiceOrDefinition (Node businessServiceOrDefinition, Node vmDefinition, out string error)
		{
			bool ok = true;
			StringBuilder builder = new StringBuilder ();

			if (vmDefinition.GetIntAttribute("cpus", 0) < businessServiceOrDefinition.GetIntAttribute("cpus_required", 0))
			{
				builder.AppendLine(CONVERT.Format("{0} CPU needed, {1} supplied",
											      businessServiceOrDefinition.GetIntAttribute("cpus_required", 0),
							                      vmDefinition.GetIntAttribute("cpus", 0)));
				ok = false;
			}

			if (vmDefinition.GetDoubleAttribute("memory_gb", 0) < businessServiceOrDefinition.GetDoubleAttribute("memory_required_gb", 0))
			{
				builder.AppendLine(CONVERT.Format("{0} GB memory needed, {1} GB supplied",
											      businessServiceOrDefinition.GetDoubleAttribute("memory_required_gb", 0),
											      vmDefinition.GetDoubleAttribute("memory_gb", 0)));
				ok = false;
			}

			if (vmDefinition.GetDoubleAttribute("storage_required_gb", 0) < businessServiceOrDefinition.GetDoubleAttribute("storage_required_gb", 0))
			{
				builder.AppendLine(CONVERT.Format("{0} GB storage needed, {1} GB supplied",
												  businessServiceOrDefinition.GetDoubleAttribute("storage_required_gb", 0),
												  vmDefinition.GetDoubleAttribute("storage_required_gb", 0)));
				ok = false;
			}

			if (! string.IsNullOrEmpty(businessServiceOrDefinition.GetAttribute("db_required")))
			{
				if (vmDefinition.GetAttribute("db") != businessServiceOrDefinition.GetAttribute("db_required"))
				{
					builder.AppendLine(CONVERT.Format("DB '{0}' needed, '{1}' supplied",
												      businessServiceOrDefinition.GetAttribute("db_required"),
												      vmDefinition.GetAttribute("db")));
					ok = false;
				}
			}

			if (vmDefinition.GetIntAttribute("max_trades_per_realtime_minute_per_instance", 0) < businessServiceOrDefinition.GetIntAttribute("trades_per_realtime_minute", 0))
			{
				builder.AppendLine(CONVERT.Format("{0} trades per minute needed, {1} supplied",
											      businessServiceOrDefinition.GetIntAttribute("trades_per_realtime_minute", 0),
											      vmDefinition.GetIntAttribute("max_trades_per_realtime_minute_per_instance", 0)));
				ok = false;
			}

			error = builder.ToString();

			return ok;
		}

		public Node GetAVmSuitableForBusinessServiceOrDefinition (Node businessServiceOrDefinition)
		{
			Node bestVm = null;

			foreach (Node vmSpec in model.GetNamedNode("VmSpecs").GetChildrenOfType("vm_spec"))
			{
				string error;
				if ((! vmSpec.GetBooleanAttribute("hidden", false))
					&& IsVmSpecSuitableForBusinessServiceOrDefinition(businessServiceOrDefinition, vmSpec, out error))
				{
					if ((bestVm == null)
						|| (vmSpec.GetIntAttribute("cpus", 0) < bestVm.GetIntAttribute("cpus", 0)))
					{
						bestVm = vmSpec;
					}
				}
			}

			return bestVm;
		}

		/// <summary>
		/// Given a location, which can be any deployment target that the user can specify
		/// -- ie a server, a rack, a datacenter or a cloud -- return a list of the servers
		/// therein.
		/// </summary>
		List<Node> GetListOfServersFromLocation (Node location)
		{
			List<Node> servers = new List<Node> ();

			switch (location.GetAttribute("type"))
			{
				case "rack":
					foreach (Node server in location.GetChildrenOfType("server"))
					{
						servers.Add(server);
					}
					break;

				case "datacenter":
					foreach (Node rack in location.GetChildrenOfType("rack"))
					{
						foreach (Node server in rack.GetChildrenOfType("server"))
						{
							servers.Add(server);
						}
					}
					break;

				case "network":
					foreach (Node datacenter in location.GetChildrenOfType("datacenter"))
					{
						if (! datacenter.GetBooleanAttribute("is_cloud", false))
						{
							foreach (Node rack in datacenter.GetChildrenOfType("rack"))
							{
								foreach (Node server in rack.GetChildrenOfType("server"))
								{
									servers.Add(server);
								}
							}
						}
					}
					break;

				case "server":
					if (location.GetBooleanAttribute("is_cloud_server", false))
					{
						if (location.GetBooleanAttribute("iaas", false))
						{
							Debug.Assert(roundVariablesNode.GetBooleanAttribute("public_iaas_cloud_deployment_allowed", false));
							servers.Add(location);
						}
						else if (location.GetBooleanAttribute("saas", false))
						{
							Debug.Assert(roundVariablesNode.GetBooleanAttribute("public_saas_cloud_deployment_allowed", false));
							servers.Add(location);
						}
						else
						{
							Debug.Assert(false, "Cloud server isn't marked as IaaS or SaaS!");
						}
					}
					else
					{
						Debug.Assert(roundVariablesNode.GetBooleanAttribute("server_deployment_allowed", false));
						servers.Add(location);
					}
					break;

				default:
					Debug.Assert(false);
					break;
			}

			return servers;
		}

		public int GetUsedCpus (Node server)
		{
			Debug.Assert(server.GetAttribute("type") == "server");

			int usedCpus = 0;
			foreach (Node serverLinkToVmInstance in server.GetChildrenOfType("server_link_to_vm_instance"))
			{
				usedCpus += serverLinkToVmInstance.GetIntAttribute("cpus", 0);
			}

			return usedCpus;
		}

		public int GetFreePhysicalSpace (Node plannedOrders, Node rack)
		{
			Debug.Assert(rack.GetAttribute("type") == "rack");

			int space = rack.GetIntAttribute("max_height_u", 0);

			foreach (Node server in rack.GetChildrenOfType("server"))
			{
				space -= server.GetIntAttribute("height_u", 0);
			}

			if (plannedOrders != null)
			{
				foreach (Node order in plannedOrders.GetChildrenOfType("order"))
				{
					switch (order.GetAttribute("order"))
					{
						case "commission_server":
							if (order.GetAttribute("rack") == rack.GetAttribute("name"))
							{
								Node serverSpec = model.GetNamedNode(order.GetAttribute("server_type"));
								space -= serverSpec.GetIntAttribute("height_u", 0);
							}
							break;

						case "move_server":
							{
								Node server = model.GetNamedNode(order.GetAttribute("server"));
								int serverSize = server.GetIntAttribute("height_u", 0);

								if (order.GetAttribute("destination_rack") == rack.GetAttribute("name"))
								{
									space -= serverSize;
								}
								else if (server.Parent == rack)
								{
									space += serverSize;
								}
							}
							break;
					}
				}
			}

			return space;
		}

		public bool CanRackHostOwner (Node plannedOrders, Node rack, string businessServiceOwner, bool devTest)
		{
			if (devTest)
			{
				businessServiceOwner = "dev&test";
			}

			return CanRackHostOwner(plannedOrders, rack, businessServiceOwner);
		}

		public bool CanRackHostOwner (Node plannedOrders, Node rack, string businessServiceOwner)
		{
			Debug.Assert(rack.GetAttribute("type") == "rack");

			string rackOwner = rack.GetAttribute("owner");

			if (string.IsNullOrEmpty(rackOwner))
			{
				if (plannedOrders != null)
				{
					List<string> serverNamesToAddToThisRack = new List<string> ();
					List<Node> serversToRemoveFromThisRack = new List<Node> ();						 

					foreach (Node order in plannedOrders.GetChildrenOfType("order"))
					{
						switch (order.GetAttribute("order"))
						{
							case "commission_service":
								foreach (Node serverAssignment in order.getChildren())
								{
									string serverName = serverAssignment.GetAttribute("server");
									Node server = model.GetNamedNode(serverName);

									if (((server != null) && (server.Parent == rack))
										|| serverNamesToAddToThisRack.Contains(serverName))
									{
										Node serviceDefinition = model.GetNamedNode(order.GetAttribute("service"));

										if (order.GetAttribute("stage") == "dev")
										{
											rackOwner = "dev&test";
										}
										else
										{
											rackOwner = serviceDefinition.GetAttribute("owner");
										}
									}
								}
								break;

							case "commission_server":
								if (order.GetAttribute("rack") == rack.GetAttribute("name"))
								{
									serverNamesToAddToThisRack.Add(order.GetAttribute("server_name"));
								}
								break;

							case "move_server":
								{
									string serverName = order.GetAttribute("server");
									Node server = model.GetNamedNode(serverName);
									Node currentRack = server.Parent;

									if (order.GetAttribute("destination_rack") == rack.GetAttribute("name"))
									{
										serverNamesToAddToThisRack.Add(serverName);

										if (string.IsNullOrEmpty(rackOwner))
										{
											List<string> owners = GetOwnersOfServer(server);
											if (owners.Count > 0)
											{
												rackOwner = owners[0];
											}
										}
									}
									else if (currentRack == rack)
									{
										serversToRemoveFromThisRack.Add(server);
										
										// Are we releasing the rack's owner by moving all servers?
										if (serversToRemoveFromThisRack.Count == rack.GetChildrenOfType("server").Count)
										{
											rackOwner = "";
										}
									}
								}
								break;
						}
					}
				}
			}

			switch (rackOwner)
			{
				case "":
					// Can host anything!
					break;

				case "online":
				case "floor":
					if (businessServiceOwner != rackOwner)
					{
						bool canShare = (businessServiceOwner == rackOwner);

						if (businessServiceOwner == "traditional")
						{
							return true;
						}

						if ((businessServiceOwner == "floor") || (businessServiceOwner == "online"))
						{
							if (roundVariablesNode.GetBooleanAttribute("online_and_floor_can_share_racks", false)
								&& ((rackOwner == "floor") || (rackOwner == "online")))
							{
								canShare = true;
							}
						}

						if (! canShare)
						{
							return false;
						}
					}
					break;

				case "traditional":
				case "dev&test":
					if (businessServiceOwner != rackOwner)
					{
						return false;
					}
					break;

				default:
					Debug.Assert(false);
					break;
			}

			return true;
		}

		/// <summary>
		/// If development work can be shared, and this service has already been developed in another region,
		/// return that sharable development service; otherwise, return the development service for
		/// the specified region if it exists; failing that, null.
		/// </summary>
		public Node FindDevelopmentService (Node businessServiceDefinition)
		{
			Node serviceAvailableSoonest = null;

			if (roundVariablesNode.GetBooleanAttribute("shared_development", false))
			{
				foreach (Node business in model.GetNamedNode("Businesses").GetChildrenOfType("business"))
				{
					foreach (Node businessService in business.GetChildrenOfType("business_service"))
					{
						if (businessService.GetAttribute("common_service_name") == businessServiceDefinition.GetAttribute("common_service_name"))
						{
							if ((serviceAvailableSoonest == null)
								|| ((businessService.GetIntAttribute("dev_countdown", 0) <= serviceAvailableSoonest.GetIntAttribute("dev_countdown", 0))
								    && (businessService.GetIntAttribute("handover_countdown", 0) <= serviceAvailableSoonest.GetIntAttribute("handover_countdown", 0))))

							{
								serviceAvailableSoonest = businessService;
							}
						}
					}
				}
			}

			if (serviceAvailableSoonest != null)
			{
				return serviceAvailableSoonest;
			}

			return model.GetNamedNode(businessServiceDefinition.GetAttribute("dev_service_name"));
		}

		public double GetFreeStorage (Node plannedOrders, Node storageArray)
		{
			Debug.Assert(storageArray.GetAttribute("type") == "storage_array");

			double freeSpace = storageArray.GetDoubleAttribute("total_capacity_gb", 0);

			foreach (Node businessService in model.GetNodesWithAttributeValue("type", "business_service"))
			{
				if (model.GetNamedNode(businessService.GetAttribute("storage_array")) == storageArray)
				{
					Node vmInstance = model.GetNamedNode(businessService.GetAttribute("vm_instance"));
					if (vmInstance != null)
					{
						freeSpace -= vmInstance.GetDoubleAttribute("storage_required_gb", 0);
					}
				}
			}

			if (plannedOrders != null)
			{
				foreach (Node plannedOrder in plannedOrders.GetChildrenOfType("order"))
				{
					switch (plannedOrder.GetAttribute("order"))
					{
						case "commission_service":
							if (plannedOrder.GetAttribute("storage_array") == storageArray.GetAttribute("name"))
							{
								Node businessServiceDefinition = model.GetNamedNode(plannedOrder.GetAttribute("service"));
								freeSpace -= businessServiceDefinition.GetDoubleAttribute("storage_required_gb", 0);
							}
							break;

						case "upgrade_storage":
							if (plannedOrder.GetAttribute("storage_array") == storageArray.GetAttribute("name"))
							{
								freeSpace += plannedOrder.GetDoubleAttribute("amount_gb", 0);
							}
							break;
					}
				}
			}

			return freeSpace;
		}

		bool AddPlanToQueueForServiceDeployment (Node plannedOrders, Node businessServiceOrDefinition, Node vmDefinition,
		                                         bool devTest, string devServiceName, bool canSplitVmAcrossServers,
		                                         Node specifiedDeploymentLocation)
		{
			string stage = (devTest ? "dev" : "production");
			Node business = model.GetNamedNode(businessServiceOrDefinition.GetAttribute("business"));
			Node homeDatacenter = model.GetNamedNode(business.GetAttribute("datacenter"));
			string owner = businessServiceOrDefinition.GetAttribute("owner");
			if (devTest)
			{
				owner = "dev&test";
			}

			if (specifiedDeploymentLocation != null)
			{
				Debug.Assert(specifiedDeploymentLocation.GetAttribute("type") == "cpu_cloud");

				if (! vmManager.ValidateCloudIsSuitableDeploymentTarget(plannedOrders, this,
					                                                    specifiedDeploymentLocation,
																		businessServiceOrDefinition,
																		devTest))
				{
					return false;
				}
			}

			string serviceName = "";
			bool redeploying = false;
			switch (businessServiceOrDefinition.GetAttribute("type"))
			{
				case "business_service":
					serviceName = businessServiceOrDefinition.GetAttribute("name");
					redeploying = true;
					break;

				case "new_service":
					serviceName = businessServiceOrDefinition.GetAttribute("service_name");
					break;

				default:
					Debug.Assert(false);
					break;
			}

			if (devTest)
			{
				Debug.Assert(string.IsNullOrEmpty(devServiceName));
			}

			// What storage array do we use?
			Node storageArray = null;
			if ((specifiedDeploymentLocation != null)
				&& specifiedDeploymentLocation.GetBooleanAttribute("is_public_cloud", false))
			{
				storageArray = model.GetNamedNode("Public Cloud Storage");
			}
			else
			{
				List<Node> datacentersToUse = vmManager.GetDataCentersControllingCloud(specifiedDeploymentLocation);
				string ownerToUse = owner;

				if (devTest)
				{
					ownerToUse = "dev&test";
				}
				else if (owner == "traditional")
				{
					ownerToUse = "online";
				}

				storageArray = null;
				// Check if service's business has storage array used - if so use
				foreach (Node tryStorageArray in homeDatacenter.GetChildrenOfType("storage_array"))
				{
					if (tryStorageArray.GetAttribute("owner") == ownerToUse)
					{
						foreach (Node businessService in model.GetNodesWithAttributeValue("type", "business_service"))
						{
							if ((businessService.GetAttribute("storage_array") == tryStorageArray.GetAttribute("name")
								&& ((!businessService.GetBooleanAttribute("is_dev", false))
									|| (businessService.GetIntAttribute("dev_countdown", 0) > 0))))
							{
								storageArray = tryStorageArray;
								break;
							}
						}
					}
				}

				// if not used find one that is in use from other business
				if (null == storageArray)
				{
					foreach (Node datacenterToUse in datacentersToUse)
					{
						foreach (Node tryStorageArray in datacenterToUse.GetChildrenOfType("storage_array"))
						{
							if (tryStorageArray.GetAttribute("owner") == ownerToUse)
							{
								foreach (Node businessService in model.GetNodesWithAttributeValue("type", "business_service"))
								{
									if ((businessService.GetAttribute("storage_array") == tryStorageArray.GetAttribute("name")
										&& ((!businessService.GetBooleanAttribute("is_dev", false))
											|| (businessService.GetIntAttribute("dev_countdown", 0) > 0))))
									{
										storageArray = tryStorageArray;
										break;
									}
								}
							}
						}
					}
				}

				// else use the service's business's storage array
				if (null == storageArray)
				{
					foreach (Node tryStorageArray in homeDatacenter.GetChildrenOfType("storage_array"))
					{
						if (tryStorageArray.GetAttribute("owner") == ownerToUse)
						{
							storageArray = tryStorageArray;
							break;
						}
					}
				}
			}
			
			Debug.Assert(storageArray != null);

			double storageAvailable = GetFreeStorage(plannedOrders, storageArray);
			double storageNeeded = vmDefinition.GetDoubleAttribute("storage_required_gb", 0);
			if (storageArray.GetBooleanAttribute("unlimited_storage", false))
			{
				storageAvailable = storageNeeded;
			}

			// Do we need a storage upgrade?
			if (storageAvailable < storageNeeded)
			{
				double upgradeAmount = roundVariablesNode.GetDoubleAttribute("storage_upgrade_gb", 0);

				List<AttributeValuePair> storageOrderAttributes = new List<AttributeValuePair> ();
				storageOrderAttributes.Add(new AttributeValuePair ("type", "order"));
				storageOrderAttributes.Add(new AttributeValuePair ("order", "upgrade_storage"));
				storageOrderAttributes.Add(new AttributeValuePair ("business_service", serviceName));
				storageOrderAttributes.Add(new AttributeValuePair ("stage", stage));
				storageOrderAttributes.Add(new AttributeValuePair ("storage_array", storageArray.GetAttribute("name")));
				storageOrderAttributes.Add(new AttributeValuePair ("amount_gb", upgradeAmount));
				new Node (plannedOrders, "order", "", storageOrderAttributes);

				AddConfirmation(plannedOrders, stage, "Upgrade {0}", storageArray.GetAttribute("name"));
			}

			Dictionary<string, Node> newlyCommissionedServerNameToContainingRack = new Dictionary<string, Node> ();
			Node newServerDefinition = GetDefaultNewServerDefinition();

			// Do we need to commission a new server?
			VirtualMachineManager.CloudCapacityState state;
			int delay;
			while ((state = vmManager.DoesCloudHaveCapacityForService(plannedOrders, businessServiceOrDefinition, specifiedDeploymentLocation, out delay, devTest))
				   == VirtualMachineManager.CloudCapacityState.Insufficient)
			{
				List<Node> usableRacks = vmManager.GetRacksSuitableForNewServer(plannedOrders, this, businessServiceOrDefinition, devTest, newServerDefinition, specifiedDeploymentLocation);

				// Sort the racks according to preference.
				usableRacks.Sort(delegate (Node a, Node b)
								{
									// Count servers currently, or planned to be, in each rack.
									int serversInA = a.GetChildrenOfType("server").Count;
									int serversInB = b.GetChildrenOfType("server").Count;
									foreach (string serverName in newlyCommissionedServerNameToContainingRack.Keys)
									{
										Node rack = newlyCommissionedServerNameToContainingRack[serverName];

										if (rack == a)
										{
											serversInA++;
										}

										if (rack == b)
										{
											serversInB++;
										}
									}

									// Given an empty rack and a nonempty rack...
									if ((serversInA == 0)
										&& (serversInB > 0))
									{
										// ...prefer the nonempty one.
										return 1;
									}
									else if ((serversInA > 0)
										&& (serversInB == 0))
									{
										return -1;
									}

									// Given a choice of two empty racks to reopen...
									if ((serversInA == 0)
										&& (serversInB == 0))
									{
										// ...choose the bigger...
										int aSize = GetFreePhysicalSpace(plannedOrders, a);
										int bSize = GetFreePhysicalSpace(plannedOrders, b);
										if (aSize != bSize)
										{
											return bSize - aSize;
										}

										// ...or the cheaper.
										double aOpex = a.GetDoubleAttribute("opex", 0);
										double bOpex = b.GetDoubleAttribute("opex", 0);
										if (aOpex != bOpex)
										{
											return aOpex.CompareTo(bOpex);
										}
									}

									// Failing that, prefer fuller racks.
									if (GetFreePhysicalSpace(plannedOrders, a) < GetFreePhysicalSpace(plannedOrders, b))
									{
										return -1;
									}
									else if (GetFreePhysicalSpace(plannedOrders, a) > GetFreePhysicalSpace(plannedOrders, b))
									{
										return 1;
									}

									// Failing that, prefer racks in our home datacenter.
									Node aDatacenter = a.Parent;
									Node bDatacenter = b.Parent;

									if ((aDatacenter == homeDatacenter) && (bDatacenter != homeDatacenter))
									{
										return -1;
									}
									else if ((aDatacenter != homeDatacenter) && (bDatacenter == homeDatacenter))
									{
										return 1;
									}

									// Failing that, just order by name.
									return a.GetAttribute("name").CompareTo(b.GetAttribute("name"));
								});

				if (usableRacks.Count > 0)
				{
					// Make an order, and a message, to create a server.  Add its name to the front of the list.
					Node rack = usableRacks[0];
					string serverName = GetNewServerName(plannedOrders, rack);

                    AddCommissionServerPlanToQueue(plannedOrders, stage, owner, newServerDefinition, business, serviceName, rack, serverName);

					newlyCommissionedServerNameToContainingRack.Add(serverName, rack);
				}
				else
				{
					AddError(plannedOrders, stage, "No capacity");

					return false;
				}
			}

			switch (state)
			{
				case VirtualMachineManager.CloudCapacityState.Insufficient:
					AddError(plannedOrders, stage, "No capacity");
					break;

				case VirtualMachineManager.CloudCapacityState.Sufficient:
					break;

				case VirtualMachineManager.CloudCapacityState.SufficientButNotReadyInTime:
					// Allow this to go ahead anyway, even though it'll fail -- we want to let the user waste the resources.
					break;
			}

			// Commission the service.
			List<AttributeValuePair> serviceOrderAttributes = new List<AttributeValuePair> ();
			serviceOrderAttributes.Add(new AttributeValuePair ("type", "order"));
			serviceOrderAttributes.Add(new AttributeValuePair ("order", "commission_service"));
			serviceOrderAttributes.Add(new AttributeValuePair ("service", businessServiceOrDefinition.GetAttribute("name")));
			serviceOrderAttributes.Add(new AttributeValuePair ("stage", stage));
			serviceOrderAttributes.Add(new AttributeValuePair ("cloud", specifiedDeploymentLocation.GetAttribute("name")));

			if (newlyCommissionedServerNameToContainingRack.Count > 0)
			{
				delay = Math.Max(delay, newServerDefinition.GetIntAttribute("purchase_delay", 0));
			}

			if (delay > 0)
			{
				// We only need to add a hardware delay if there's either no dev service we're waiting on,
				// or if that dev service exists and still has work to do.
				bool useHardwareDelays = false;
				if (string.IsNullOrEmpty(devServiceName))
				{
					useHardwareDelays = true;
				}
				else
				{
					Node devService = model.GetNamedNode(devServiceName);
					if (devService != null)
					{
						if (devService.GetIntAttribute("dev_countdown", 0) <= 0)
						{
							useHardwareDelays = true;
						}
					}
				}

				if (useHardwareDelays)
				{
					serviceOrderAttributes.Add(new AttributeValuePair ("hardware_delay", delay));
				}
			}

			if (! redeploying)
			{
				serviceOrderAttributes.Add(new AttributeValuePair ("vm_type", vmDefinition.GetAttribute("name")));
			}
			serviceOrderAttributes.Add(new AttributeValuePair ("storage_array", storageArray.GetAttribute("name")));
			if (! string.IsNullOrEmpty(devServiceName))
			{
				serviceOrderAttributes.Add(new AttributeValuePair ("dev_service", devServiceName));
			}
			Node serviceOrder = new Node (plannedOrders, "order", "", serviceOrderAttributes);

			AddInfo(plannedOrders, stage, "Use {0} CPU from {1}", vmManager.GetMaxCpusNeeded(businessServiceOrDefinition), specifiedDeploymentLocation.GetAttribute("desc"));

			return true;
		}

		public void AddCommissionServerPlanToQueue (Node plannedOrders,
		                                            string stage, string owner,
		                                            Node serverSpec, Node business, string serviceName,
													Node rack, string serverName)
		{
			AddConfirmation(plannedOrders, stage, "Commission new server {0}", serverName);

			List<AttributeValuePair> serverOrderAttributes = new List<AttributeValuePair> ();
			serverOrderAttributes.Add(new AttributeValuePair ("type", "order"));
			serverOrderAttributes.Add(new AttributeValuePair ("order", "commission_server"));
			serverOrderAttributes.Add(new AttributeValuePair ("owner", owner));
			serverOrderAttributes.Add(new AttributeValuePair ("stage", stage));
			serverOrderAttributes.Add(new AttributeValuePair ("rack", rack.GetAttribute("name")));
			serverOrderAttributes.Add(new AttributeValuePair ("server_type", serverSpec.GetAttribute("name")));
			serverOrderAttributes.Add(new AttributeValuePair ("server_name", serverName));
			serverOrderAttributes.Add(new AttributeValuePair ("business", business.GetAttribute("name")));
			serverOrderAttributes.Add(new AttributeValuePair ("business_service", serviceName));
			new Node (plannedOrders, "order", "", serverOrderAttributes);
		}

		public bool AddCommissionFreeReplacementServerPlanToQueue (Node plannedOrders,
			                                                       Node serverSpec, Node business,
													               Node rack, string serverName, string serverGroup, string owner)
		{
			if (GetFreePhysicalSpace(plannedOrders, rack) >= serverSpec.GetIntAttribute("height_u", 0))
			{
				if (CanRackHostOwner(plannedOrders, rack, owner))
				{
					List<AttributeValuePair> serverOrderAttributes = new List<AttributeValuePair> ();
					serverOrderAttributes.Add(new AttributeValuePair ("type", "order"));
					serverOrderAttributes.Add(new AttributeValuePair ("order", "commission_server"));
					serverOrderAttributes.Add(new AttributeValuePair ("rack", rack.GetAttribute("name")));
					serverOrderAttributes.Add(new AttributeValuePair ("server_type", serverSpec.GetAttribute("name")));
					serverOrderAttributes.Add(new AttributeValuePair ("server_name", serverName));
					serverOrderAttributes.Add(new AttributeValuePair ("server_group", serverGroup));
					serverOrderAttributes.Add(new AttributeValuePair ("business", business.GetAttribute("name")));
					serverOrderAttributes.Add(new AttributeValuePair ("is_free_replacement", true));
					serverOrderAttributes.Add(new AttributeValuePair ("owner", owner));
					new Node (plannedOrders, "order", "", serverOrderAttributes);

					return true;
				}
				else
				{
					AddError(plannedOrders, "", "Rack {0} has been reclaimed by '{1}' and can't fit the replacement server(s).", rack.GetAttribute("name"), rack.GetAttribute("owner"));

					return false;
				}
			}
			else
			{
				AddError(plannedOrders, "", "Insufficient free space on rack {0}", rack.GetAttribute("name"));

				return false;
			}
		}

		public string GetNewServerName (Node plannedOrders, Node rack)
		{
			int serversInRack = rack.GetChildrenOfType("server").Count;
			int newServerIndex = rack.GetIntAttribute("next_created_server_index", serversInRack + 1);

			List<string> plannedNewNames = new List<string> ();

			if (plannedOrders != null)
			{
				foreach (Node order in plannedOrders)
				{
					if (order.GetAttribute("order") == "commission_server")
					{
						if (order.GetAttribute("rack") == rack.GetAttribute("name"))
						{
							plannedNewNames.Add(order.GetAttribute("server_name"));
						}
					}
				}
			}

			string serverName;
			bool exists;
			do
			{
				serverName = CONVERT.Format("{0}.{1}", rack.GetAttribute("name"), newServerIndex);

				exists = ((model.GetNamedNode(serverName) != null)
						  || plannedNewNames.Contains(serverName));

				if (exists)
				{
					newServerIndex++;
				}
			}
			while (exists);

			return serverName;
		}

		public Node GetSingleCloudServer (Node cloud)
		{
			List<Node> servers = new List<Node> ();
			foreach (Node locationReference in cloud.GetChildrenOfType("cloud_location"))
			{
				Node rack = model.GetNamedNode(locationReference.GetAttribute("location"));

				servers.AddRange((Node []) rack.GetChildrenOfType("server").ToArray(typeof (Node)));
			}

			if (cloud.GetAttribute("type") == "server")
			{
				servers.Add(cloud);
			}

			if (servers.Count == 1)
			{
				return servers[0];
			}

			return null;
		}

		public bool AddCloudDeploymentPlanToQueue (Node plannedOrders, Node businessServiceOrDefinition, Node deployLocation,
		                                           string selectedBuildType, string selectedChargeModel, Node selectedCloudProvider)
		{
			bool canDoSomeWork = false;

			AddInfo(plannedOrders, "production", "Deploy to production");

			Node server = GetSingleCloudServer(deployLocation);

			// If it's a cloud deployment, check it's possible!
			if ((server != null)
				&& server.GetBooleanAttribute("is_cloud_server", false))
			{
				if (server.GetBooleanAttribute("saas", false))
				{
					if (selectedCloudProvider != null)
					{
						if (! CanCloudProviderHandleService(selectedCloudProvider, businessServiceOrDefinition, true))
						{
							AddError(plannedOrders, "production", "Service '{0}' not available under SaaS from provider '{1}'",
									 businessServiceOrDefinition.GetAttribute("name"), selectedCloudProvider.GetAttribute("desc"));

							return false;
						}
					}
					else
					{
						AddError(plannedOrders, "production", "No cloud provider specified");
						return false;
					}
				}

				switch (selectedCloudProvider.GetAttribute("status"))
				{
					case "closed":
						AddError(plannedOrders, "production", "Cloud provider is not available");
						return false;

					case "closing":
						AddError(plannedOrders, "production", "Cloud provider is closing down");
						return false;
				}
			}

			Node businessService;
			Node businessServiceDefinition;
			string developmentServiceName = null;
			Node vmDefinition = null;
			switch (businessServiceOrDefinition.GetAttribute("type"))
			{
				case "business_service":
					businessService = businessServiceOrDefinition;
					businessServiceDefinition = null;
					developmentServiceName = businessService.GetAttribute("requires_dev");
					Node vmInstance = model.GetNamedNode(businessService.GetAttribute("vm_instance"));
					vmDefinition = model.GetNamedNode(vmInstance.GetAttribute("vm_spec"));
					break;

				case "new_service":
					businessService = null;
					businessServiceDefinition = businessServiceOrDefinition;
					developmentServiceName = businessServiceDefinition.GetAttribute("dev_service_name");
					vmDefinition = GetAVmSuitableForBusinessServiceOrDefinition(businessServiceOrDefinition);
					break;

				default:
					Debug.Assert(false);
					break;
			}

			bool toPrivate = false;
			bool toIaaS = false;
			bool toSaaS = false;
			bool devIgnoreCloud = false;

			switch (deployLocation.GetAttribute("type"))
			{
				case "server":
					Debug.Assert(deployLocation.GetBooleanAttribute("is_cloud_server", false));
					toIaaS = deployLocation.GetBooleanAttribute("iaas", false);
					toSaaS = deployLocation.GetBooleanAttribute("saas", false);
					break;

				case "datacenter":
					toPrivate = true;
					break;

				case "network":
					// Just used for redeploying dev projects.
					toPrivate = true;
					devIgnoreCloud = true;
					break;

				case "cpu_cloud":
					toPrivate = true;
					break;

				default:
					Debug.Assert(false);
					break;
			}

			if (toPrivate)
			{
				if (! devIgnoreCloud)
				{
					Debug.Assert(string.IsNullOrEmpty(selectedBuildType));
					Debug.Assert(string.IsNullOrEmpty(selectedChargeModel));
					Debug.Assert(selectedCloudProvider == null);
				}
			}
			else
			{
				Debug.Assert(selectedCloudProvider != null);

				if (toIaaS)
				{
					Node demand = model.GetNamedNode(businessServiceOrDefinition.GetAttribute("demand_name"));
					if (demand == null)
					{
						Debug.Assert(! string.IsNullOrEmpty(selectedBuildType));
					}
					Debug.Assert(! string.IsNullOrEmpty(selectedChargeModel));
				}
				else if (toSaaS)
				{
					Debug.Assert(string.IsNullOrEmpty(selectedBuildType));
					Debug.Assert(string.IsNullOrEmpty(selectedChargeModel));
				}
				else
				{
					Debug.Assert(false);
				}
			}

			Node cloud = vmManager.GetCloudControllingLocation(deployLocation);
			if (AddPlanToQueueForServiceDeployment(plannedOrders, businessServiceOrDefinition, vmDefinition, false, developmentServiceName, true, cloud))
			{
				canDoSomeWork = true;

				foreach (Node order in plannedOrders.GetChildrenOfType("order"))
				{
					if (order.GetAttribute("order") == "commission_service")
					{
						List<AttributeValuePair> attributes = new List<AttributeValuePair> ();
						if (selectedCloudProvider != null)
						{
							attributes.Add(new AttributeValuePair ("cloud_provider", selectedCloudProvider.GetAttribute("name")));
						}
						if (! string.IsNullOrEmpty(selectedChargeModel))
						{
							attributes.Add(new AttributeValuePair ("cloud_charge_model", selectedChargeModel));
						}
						if (! string.IsNullOrEmpty(selectedBuildType))
						{
							attributes.Add(new AttributeValuePair ("cloud_build_type", selectedBuildType));
						}
						order.SetAttributes(attributes);
					}
				}
			}

			bool hasAnError = false;
			foreach (Node order in plannedOrders)
			{
				if (order.GetAttribute("type") == "error")
				{
					hasAnError = true;
					break;
				}
			}

			return canDoSomeWork && ! hasAnError;
		}

		public bool AddServiceCommissionPlanToQueue (Node plannedOrders, Node businessServiceDefinition, string vmDefinitionName, Node deployLocation)
		{
			Node business = model.GetNamedNode(businessServiceDefinition.GetAttribute("business"));
			bool canDoSomeWork = false;
			string developmentServiceName = "";

			Node demand = model.GetNamedNode(businessServiceDefinition.GetAttribute("demand_name"));
			if (demand != null)
			{
				Node timeNode = model.GetNamedNode("CurrentTime");
				if (timeNode.GetIntAttribute("seconds", 0) >= demand.GetIntAttribute("delay", 0))
				{
					AddError(plannedOrders, "", "Insufficient time to deploy VM");
					return false;
				}
			}

			// Validate VM type.
			Node vmDefinition = null;
			if (! string.IsNullOrEmpty(vmDefinitionName))
			{
				vmDefinition = model.GetNamedNode(vmDefinitionName);
				if ((vmDefinition == null) || (vmDefinition.GetAttribute("type") != "vm_spec"))
				{
					AddError(plannedOrders, "production", "Unknown VM type {0}", vmDefinitionName);
					vmDefinition = null;
				}
			}

			// Do we need to do any dev work?
			Node developmentService = FindDevelopmentService(businessServiceDefinition);
			if (! businessServiceDefinition.GetBooleanAttribute("is_new_service", false))
			{
				developmentService = null;
			}
			if (developmentService != null)
			{
				developmentServiceName = developmentService.GetAttribute("name");

				Node developmentServiceBusiness = model.GetNamedNode(developmentService.GetAttribute("business"));

				if (developmentService.GetIntAttribute("dev_countdown", 0) > 0)
				{
					if (developmentServiceBusiness == business)
					{
						AddInfo(plannedOrders, "dev", "Already in development");
					}
					else
					{
						AddInfo(plannedOrders, "dev", "Already in development for {0}", developmentServiceBusiness.GetAttribute("desc"));
					}
				}
				else
				{
					if (developmentServiceBusiness == business)
					{
						AddInfo(plannedOrders, "dev", "Already developed");
					}
					else
					{
						AddInfo(plannedOrders, "dev", "Already developed for {0}", developmentServiceBusiness.GetAttribute("desc"));
					}
				}
			}
			else
			{
				if (businessServiceDefinition.GetIntAttribute("dev_test_time", 0) > 0)
				{
					if (business.GetIntAttribute("dev_budget_left", 0) >= businessServiceDefinition.GetIntAttribute("dev_cost", 0))
					{
						AddInfo(plannedOrders, "dev", "Start development");

						Node devVmDefinition = vmDefinition;
						if (devVmDefinition == null)
						{
							devVmDefinition = GetAVmSuitableForBusinessServiceOrDefinition(businessServiceDefinition);
							Debug.Assert(devVmDefinition != null, "No VM suitable for service");
						}

						List<Node> devLocations = new List<Node> ();
						switch (roundVariablesNode.GetAttribute("dev_deploy_type"))
						{
							case "server":
								{
									Node datacenter = model.GetNamedNode(business.GetAttribute("datacenter"));
									foreach (Node rack in datacenter.GetChildrenOfType("rack"))
									{
										Node cloud = vmManager.GetCloudControllingLocation(rack);
										devLocations.Add(cloud);
									}
								}
								break;

							case "rack":
								Debug.Assert(false, "Dev can't deploy to a rack!");
								break;

							case "local":
								Debug.Assert(false, "Dev can't deploy to a local cloud!");
								break;

							case "global":
								devLocations.Add(model.GetNamedNode("Global Dev&Test Cloud"));
								break;

							case "public":
								Debug.Assert(false, "Dev can't deploy to a public cloud!");
								break;

							default:
								Debug.Assert(false);
								break;
						}

						developmentServiceName = businessServiceDefinition.GetAttribute("dev_service_name");

						List<Node> existingOrders = new List<Node> ((Node []) plannedOrders.getChildren().ToArray(typeof(Node)));

						Dictionary<Node, int> successfulDevLocationToServerPurchasesNeeded = new Dictionary<Node, int> ();
						foreach (Node devLocation in devLocations)
						{
							if (AddPlanToQueueForServiceDeployment(plannedOrders, businessServiceDefinition, devVmDefinition, true, "", true, devLocation))
							{
								int serversBought = 0;
								foreach (Node order in plannedOrders.GetChildrenOfType("order"))
								{
									if (order.GetAttribute("order") == "commission_server")
									{
										serversBought++;
									}
								}

								successfulDevLocationToServerPurchasesNeeded.Add(devLocation, serversBought);
							}

							List<Node> ordersToDelete = new List<Node> ();
							foreach (Node order in plannedOrders.getChildren())
							{
								if (! existingOrders.Contains(order))
								{
									ordersToDelete.Add(order);
								}
							}
							foreach (Node order in ordersToDelete)
							{
								plannedOrders.DeleteChildTree(order);
							}
						}

						List<Node> successfulDevLocations = new List<Node> (successfulDevLocationToServerPurchasesNeeded.Keys);
						successfulDevLocations.Sort(delegate (Node a, Node b)
													{
                                                        if (successfulDevLocationToServerPurchasesNeeded[a] == successfulDevLocationToServerPurchasesNeeded[b])
                                                        {
                                                            return a.GetAttribute("name").CompareTo(b.GetAttribute("name"));
                                                        }
														return successfulDevLocationToServerPurchasesNeeded[a].CompareTo(successfulDevLocationToServerPurchasesNeeded[b]);
													});

						if (successfulDevLocations.Count > 0)
						{
							AddPlanToQueueForServiceDeployment(plannedOrders, businessServiceDefinition, devVmDefinition, true, "", true, successfulDevLocations[0]);
							canDoSomeWork = true;
						}
					}
					else
					{
						AddError(plannedOrders, "dev", "Insufficient development budget");
					}
				}
			}

			// We only need to do deploy into production if we've not done so already.
			if (model.GetNamedNode(businessServiceDefinition.GetAttribute("service_name")) != null)
			{
				if ((developmentService != null) && (developmentService.GetIntAttribute("dev_countdown", 0) > 0))
				{
					AddInfo(plannedOrders, "production", "In Service Catalog, under development");
				}
				else
				{
					AddInfo(plannedOrders, "production", "In Service Catalog");
				}
			}
			else
			{
				if ((vmDefinition != null) || (deployLocation != null))
				{
					AddInfo(plannedOrders, "production", "Deploy to production");

					// We might not be supplied a VM spec, if we're only doing dev work.
					if (vmDefinition != null)
					{
						string error = null;
						if (! IsVmSpecSuitableForBusinessServiceOrDefinition(businessServiceDefinition, vmDefinition, out error))
						{
							AddError(plannedOrders, "production", "Incorrect VM type");
						}

						// We only need to do production work if a location is specified.
						if (deployLocation != null)
						{
							bool canSplitProductionVmOverServers = true;
							switch (roundVariablesNode.GetAttribute("production_deploy_type"))
							{
								case "server":
									canSplitProductionVmOverServers = false;
									break;

								case "rack":
								case "local":
								case "global":
								case "public":
									break;

								default:
									Debug.Assert(false);
									break;
							}

							if (! HasAnError(plannedOrders))
							{
								if ((vmDefinition != null) && (deployLocation != null))
								{
									bool rejectRack = false;

									if (deployLocation.GetAttribute("type") == "rack")
									{
										if (! CanRackHostOwner(plannedOrders, deployLocation, businessServiceDefinition.GetAttribute("owner"), false))
										{
											AddError(plannedOrders, "production", "Rack {0} owned by {1}", deployLocation.GetAttribute("name"), Strings.SentenceCase(deployLocation.GetAttribute("owner")));
											rejectRack = true;
										}
									}

									if (! rejectRack)
									{
										if (AddPlanToQueueForServiceDeployment(plannedOrders, businessServiceDefinition, vmDefinition, false, developmentServiceName, canSplitProductionVmOverServers, deployLocation))
										{
											canDoSomeWork = true;
										}
									}
								}
							}
						}
						else
						{
							AddError(plannedOrders, "production", "No location specified");
						}
					}
					else
					{
						AddError(plannedOrders, "production", "No VM specified");
					}
				}
			}

			return canDoSomeWork && ! HasAnError(plannedOrders);
		}

		bool HasAnError (Node plannedOrders)
		{
			foreach (Node order in plannedOrders)
			{
				if (order.GetAttribute("type") == "error")
				{
					return true;
				}
			}

			return false;
		}

		public bool CanServerBeMoved (Node server)
		{
			if (server.Parent.GetAttribute("owner") == "traditional")
			{
				return false;
			}

			return true;
		}

		public List<string> GetOwnersOfServer (Node server)
		{
			List<Node> vmInstancesOnServer = new List<Node> ();
			foreach (Node serverLinkToVmInstance in server.GetChildrenOfType("server_link_to_vm_instance"))
			{
				Node vmInstance = model.GetNamedNode(serverLinkToVmInstance.GetAttribute("vm_instance"));
				if ((vmInstance != null) && ! vmInstancesOnServer.Contains(vmInstance))
				{
					vmInstancesOnServer.Add(vmInstance);
				}
			}

			List<string> owners = new List<string> ();
			foreach (Node vmInstance in vmInstancesOnServer)
			{
				Node businessService = model.GetNamedNode(vmInstance.GetAttribute("business_service"));
				string owner = businessService.GetAttribute("owner");

				if (! owners.Contains(owner))
				{
					owners.Add(owner);
				}
			}

			string rackOwner = server.Parent.GetAttribute("owner");
			if (! string.IsNullOrEmpty(rackOwner))
			{
				if (! owners.Contains(rackOwner))
				{
					owners.Add(rackOwner);
				}
			}

			return owners;
		}

		public bool AddServerMovePlanToQueue (Node plannedOrders, Node server, Node destinationRack)
		{
			if (server.Parent == destinationRack)
			{
				return true;
			}

			if (server.Parent.GetAttribute("owner") == "traditional")
			{
				AddError(plannedOrders, "", "Can't relocate legacy servers");
				return false;
			}

			if (destinationRack.GetAttribute("owner") == "traditional")
			{
				AddError(plannedOrders, "", "Can't relocate servers to a legacy rack");
				return false;
			}

			if (GetFreePhysicalSpace(plannedOrders, destinationRack) < server.GetIntAttribute("height_u", 0))
			{
				AddError(plannedOrders, "", "Insufficient space in rack {0}", destinationRack.GetAttribute("name"));
				return false;
			}

			foreach (string owner in GetOwnersOfServer(server))
			{
				if (! CanRackHostOwner(plannedOrders, destinationRack, owner))
				{
					if (string.IsNullOrEmpty(destinationRack.GetAttribute("owner")))
					{
						AddError(plannedOrders, "", "Cannot move servers with different owners into the same rack");
					}
					else
					{
						AddError(plannedOrders, "", "Destination rack not suitable for services running on server");
					}
					return false;
				}
			}

			List<AttributeValuePair> attributes = new List<AttributeValuePair> ();
			attributes.Add(new AttributeValuePair ("type", "order"));
			attributes.Add(new AttributeValuePair ("order", "move_server"));
			attributes.Add(new AttributeValuePair ("server", server.GetAttribute("name")));
			attributes.Add(new AttributeValuePair ("destination_rack", destinationRack.GetAttribute("name")));
			new Node (plannedOrders, "order", "", attributes);

			return true;
		}

		public bool CanServerBeRetired (Node server)
		{
			// Can't retire a server if it still hosts any legacy services.
			if ((server.Parent.GetAttribute("owner") == "traditional")
				&& (GetUsedCpus(server) > 0))
			{
				return false;
			}

			string latestRetirableGroup = roundVariablesNode.GetAttribute("latest_retirable_server_group");
			bool groupRetirable = ((! string.IsNullOrEmpty(latestRetirableGroup))
			                      && (server.GetAttribute("server_group").CompareTo(latestRetirableGroup) <= 0));

			return groupRetirable;
		}

		public bool AddServerRetirementPlanToQueue (Node plannedOrders, List<Node> servers)
		{
			Dictionary<Node, int> cloudToCpusToLose = new Dictionary<Node, int> ();
			foreach (Node server in servers)
			{
				if (! CanServerBeRetired(server))
				{
					AddError(plannedOrders, "", "Server too young to retire");
					return false;
				}

				List<AttributeValuePair> attributes = new List<AttributeValuePair> ();
				attributes.Add(new AttributeValuePair ("type", "order"));
				attributes.Add(new AttributeValuePair ("order", "delete_server"));
				attributes.Add(new AttributeValuePair ("server", server.GetAttribute("name")));
				new Node (plannedOrders, "order", "", attributes);

				Node cloud = vmManager.GetCloudControllingLocation(server);
				if (cloud != null)
				{
					int cpus = server.GetIntAttribute("cpus", 0);
					if (cloudToCpusToLose.ContainsKey(cloud))
					{
						cloudToCpusToLose[cloud] += cpus;
					}
					else
					{
						cloudToCpusToLose.Add(cloud, cpus);
					}
				}
			}

			// Is there enough capacity to do this?
			foreach (Node cloud in cloudToCpusToLose.Keys)
			{
				int freeCpus = vmManager.GetMinFreeCpusOverTimeForCloud(cloud);
				if (freeCpus < cloudToCpusToLose[cloud])
				{
					AddError(plannedOrders, "", "Insufficient free capacity in {0} to consolidate", cloud.GetAttribute("desc"));
					return false;
				}
			}

			return true;
		}

		public bool CanCloudProviderHandleService (Node provider, Node businessServiceOrDefinition, bool saas)
		{
			if (saas)
			{
				foreach (Node option in provider.GetChildrenOfType("saas_cloud_option"))
				{
					if (option.GetAttribute("common_service_name") == businessServiceOrDefinition.GetAttribute("common_service_name"))
					{
						return true;
					}
				}
			}
			else
			{
				return true;
			}

			return false;
		}

		public List<Node> GetDeploymentTargets (Node businessServiceOrDefinition)
		{
			Node business = model.GetNamedNode(businessServiceOrDefinition.GetAttribute("business"));
			Debug.Assert(business != null);

			List<Node> targets = new List<Node> ();

			switch (roundVariablesNode.GetAttribute("production_deploy_type"))
			{
				case "rack":
					{
						Node datacenter = model.GetNamedNode(business.GetAttribute("datacenter"));
						Debug.Assert(datacenter != null);
						foreach (Node rack in datacenter.GetChildrenOfType("rack"))
						{
							if (rack.GetAttribute("owner") != "traditional")
							{
								targets.Add(rack);
							}
						}

						targets.Sort(delegate (Node a, Node b)
									{
										return a.GetIntAttribute("monitor_index", 0).CompareTo(b.GetIntAttribute("monitor_index", 0));
									});
					}
					break;

				case "local":
					{
						foreach (Node datacenter in model.GetNodesWithAttributeValue("type", "datacenter"))
						{
							if (! datacenter.GetBooleanAttribute("hidden", false))
							{
								targets.Add(datacenter);
							}
						}

						targets.Sort(delegate (Node a, Node b)
									{
										Node businessA = model.GetNamedNode(a.GetAttribute("business"));
										Node businessB = model.GetNamedNode(b.GetAttribute("business"));
										return businessA.GetIntAttribute("order", 0).CompareTo(businessB.GetIntAttribute("order", 0));
									});

						if (roundVariablesNode.GetBooleanAttribute("global_private_cloud_deployment_allowed", false))
						{
							targets.Add(model.GetNamedNode("Network"));
						}

						if (roundVariablesNode.GetBooleanAttribute("public_iaas_cloud_deployment_allowed", false)
							&& (! businessServiceOrDefinition.GetBooleanAttribute("is_new_service", false)
							&& CanServiceBeDeployedToIaaS(businessServiceOrDefinition)))
						{
							targets.Add(model.GetNamedNode("Public IaaS Cloud Server"));
						}

						if (roundVariablesNode.GetBooleanAttribute("public_saas_cloud_deployment_allowed", false)
							&& (! businessServiceOrDefinition.GetBooleanAttribute("is_new_service", false))
							&& (GetSuitableCloudProviders(businessServiceOrDefinition, true).Count > 0))
						{
							targets.Add(model.GetNamedNode("Public SaaS Cloud Server"));
						}
					}
					break;

				default:
					Debug.Assert(false);
					break;
			}

			return targets;
		}

		Node GetIaasCloudChargeOption (Node businessServiceOrDefinition, Node cloudProvider)
		{
			string chargeModel = businessServiceOrDefinition.GetAttribute("cloud_charge_model");
			if (! string.IsNullOrEmpty(businessServiceOrDefinition.GetAttribute("demand_name")))
			{
				chargeModel = "on_demand";
			}

			foreach (Node cloudChargeOption in cloudProvider.GetChildrenOfType("iaas_cloud_option"))
			{
				if ((cloudChargeOption.GetAttribute("charge_model") == chargeModel)
					&& (cloudChargeOption.GetAttribute("security") == businessServiceOrDefinition.GetAttribute("security")))
				{
					return cloudChargeOption;
				}
			}

			return null;
		}

		public List<Node> GetBusinesses ()
		{
			List<Node> businesses = new List<Node> ();

			foreach (Node business in model.GetNodesWithAttributeValue("type", "business"))
			{
				businesses.Add(business);
			}

			businesses.Sort(delegate (Node a, Node b)
							{
								return a.GetIntAttribute("order", 0).CompareTo(b.GetIntAttribute("order", 0));
							});

			return businesses;
		}

		public List<Node> GetBusinessServicesOrDefinitionsSuitableForCloudDeployment (Node business)
		{
			List<Node> services = new List<Node> ();
			List<string> existingServiceNamesPresented = new List<string> ();

			string visibleAttrributeName = CONVERT.Format("available_in_cloud_panel_round_{0}", roundVariablesNode.GetIntAttribute("current_round", 0));

			// Existing services.
			foreach (Node existingService in business.GetChildrenOfType("business_service"))
			{
				bool alreadyDeployedToCloud = false;
				Node vmInstance = model.GetNamedNode(existingService.GetAttribute("vm_instance"));
				if (vmInstance != null)
				{
					foreach (Node vmInstanceLinkToServer in vmInstance.GetChildrenOfType("vm_instance_link_to_server"))
					{
						Node server = model.GetNamedNode(vmInstanceLinkToServer.GetAttribute("server"));
						if (server.GetBooleanAttribute("iaas", false) || server.GetBooleanAttribute("saas", false))
						{
							alreadyDeployedToCloud = true;
							break;
						}
					}
				}

				bool hackForceAvailability = false;
				if (existingService.GetAttribute("common_service_name") == "Automated Trading System")
				{
					hackForceAvailability = true;
				}

				if ((! existingService.GetBooleanAttribute("is_dev", false))
					&& (existingService.GetBooleanAttribute(visibleAttrributeName, false)
					    || alreadyDeployedToCloud || hackForceAvailability)
					&& (existingService.GetBooleanAttribute("can_deploy_private", false)
					    || existingService.GetBooleanAttribute("can_deploy_iaas", false)
					    || existingService.GetBooleanAttribute("can_deploy_saas", false)))
				{
					services.Add(existingService);
					existingServiceNamesPresented.Add(existingService.GetAttribute("name"));
				}
			}

			foreach (Node newService in model.GetNamedNode("NewServiceDefinitions").GetChildrenOfType("new_service"))
			{
				if (newService.GetAttribute("business") == business.GetAttribute("name"))
				{
					if (! existingServiceNamesPresented.Contains(newService.GetAttribute("service_name")))
					{
						// TODO Awful awful hack: the "specs" (ha!) say we should only be allowed to cloud-deploy
						// those services that are in the chargeback report, but that excludes a couple that
						// we almost certainly want.
						bool hackForceAvailability = false;
						if ((newService.GetAttribute("common_service_name") == "Automated Trading System")
							|| (newService.GetAttribute("common_service_name") == "Trade Portfolio Filter")
							|| (newService.GetAttribute("common_service_name") == "Automated Search and Match"))
						{
							hackForceAvailability = true;
						}

						bool hasBeenDeveloped = newService.GetBooleanAttribute("is_traditional", false)
						                        || newService.GetBooleanAttribute("has_been_developed", false);

						bool canDeployPrivate = newService.GetBooleanAttribute("can_deploy_private", false)
												&& hasBeenDeveloped;

						bool canDeployIaaS = newService.GetBooleanAttribute("can_deploy_iaas", false)
											 && hasBeenDeveloped;

						bool canDeploySaaS = newService.GetBooleanAttribute("can_deploy_saas", false);

						int round = roundVariablesNode.GetIntAttribute("current_round", 0);
						string cloudAvailabilityAttribute = CONVERT.Format("available_in_cloud_panel_round_{0}", round);
						string sipAvailabilityAttribute = CONVERT.Format("available_in_round_{0}", round);
						bool availableInThisRound = newService.GetBooleanAttribute(cloudAvailabilityAttribute, false)
													&& newService.GetBooleanAttribute(sipAvailabilityAttribute, false);

						if ((newService.GetBooleanAttribute(visibleAttrributeName, false)
							 || hackForceAvailability)
						   && (canDeployPrivate || canDeployIaaS || canDeploySaaS)
						   && availableInThisRound)
						{
							services.Add(newService);
						}
					}
				}
			}

			services.Sort(delegate (Node a, Node b)
						{
							bool aCanDeployIaaS = a.GetBooleanAttribute("can_deploy_iaas", false) && a.GetBooleanAttribute("has_been_developed", false);
							bool aCanDeploySaaS = a.GetBooleanAttribute("can_deploy_saas", false);
							int aSection = (aCanDeployIaaS ? 2 : (aCanDeploySaaS ? 3 : 1));

							bool bCanDeployIaaS = b.GetBooleanAttribute("can_deploy_iaas", false) && b.GetBooleanAttribute("has_been_developed", false);
							bool bCanDeploySaaS = b.GetBooleanAttribute("can_deploy_saas", false);
							int bSection = (bCanDeployIaaS ? 2 : (bCanDeploySaaS ? 3 : 1));

							if (aSection != bSection)
							{
								return aSection - bSection;
							}

							return a.GetAttribute("desc").CompareTo(b.GetAttribute("desc"));
						});

			return services;
		}

		public List<Node> GetDatacenters ()
		{
			List<Node> datacenters = new List<Node> ();
			foreach (Node business in GetBusinesses())
			{
				datacenters.Add(model.GetNamedNode(business.GetAttribute("datacenter")));
			}

			return datacenters;
		}

		public List<Node> GetAllCloudProviders ()
		{
			List<Node> providers = new List<Node> ();
			foreach (Node provider in model.GetNodesWithAttributeValue("type", "cloud_provider"))
			{
				providers.Add(provider);
			}

			providers.Sort(delegate(Node a, Node b)
			{
				return a.GetAttribute("desc").CompareTo(b.GetAttribute("desc"));
			});

			return providers;
		}

		public bool IsCloudProviderActive (Node provider)
		{
			return provider.GetBooleanAttribute("available", false);
		}

		public List<Node> GetAvailableCloudProviders ()
		{
			List<Node> providers = new List<Node> ();
			foreach (Node provider in model.GetNodesWithAttributeValue("type", "cloud_provider"))
			{
				if (IsCloudProviderActive(provider))
				{
					providers.Add(provider);
				}
			}

			providers.Sort(delegate(Node a, Node b)
			{
				return a.GetAttribute("desc").CompareTo(b.GetAttribute("desc"));
			});

			return providers;
		}

		bool CanServiceBeDeployedToIaaS (Node businessServiceOrDefinition)
		{
			foreach (Node provider in GetSuitableCloudProviders(businessServiceOrDefinition, false))
			{
				if (GetIaasCloudChargeOption(businessServiceOrDefinition, provider) != null)
				{
					return true;
				}
			}

			return false;
		}

		public List<Node> GetSuitableCloudProviders (Node businessServiceOrDefinition, bool saas)
		{
			List<Node> providers = new List<Node> ();
			foreach (Node provider in model.GetNodesWithAttributeValue("type", "cloud_provider"))
			{
				if (CanCloudProviderHandleService(provider, businessServiceOrDefinition, saas)
					&& IsCloudProviderActive(provider))
				{
					providers.Add(provider);
				}
			}

			providers.Sort(delegate (Node a, Node b)
							{
								return a.GetAttribute("desc").CompareTo(b.GetAttribute("desc"));
							});

			return providers;
		}

		public bool AddCloudDeploymentInfoToServiceCommission (Node plannedOrders, Node businessServiceOrDefinition, Node provider, string chargeModel)
		{
			Node commissionServiceOrder = null;
			foreach (Node order in plannedOrders.GetChildrenOfType("order"))
			{
				if ((order.GetAttribute("order") == "commission_service")
					&& (order.GetAttribute("service") == businessServiceOrDefinition.GetAttribute("name")))
				{
					commissionServiceOrder = order;
					break;
				}
			}

			Debug.Assert(commissionServiceOrder != null);

			if (provider == null)
			{
				AddError(plannedOrders, "production", "No cloud provider specified");
				return false;
			}
			else if (! provider.GetBooleanAttribute("available", false))
			{
				AddError(plannedOrders, "production", "Cloud provider '{0}' not available", provider.GetAttribute("desc"));
				return false;
			}
			else if (string.IsNullOrEmpty(chargeModel))
			{
				AddError(plannedOrders, "production", "No charge model specified");
				return false;
			}
			else
			{
				List<AttributeValuePair> attributes = new List<AttributeValuePair> ();
				attributes.Add(new AttributeValuePair ("cloud_charge_model", chargeModel));
				attributes.Add(new AttributeValuePair ("cloud_provider", provider.GetAttribute("name")));
				commissionServiceOrder.SetAttributes(attributes);

				return true;
			}
		}

		public Node GetCurrentCloudDeploymentLocationIfAny (Node businessService)
		{
			Node cloud = model.GetNamedNode(businessService.GetAttribute("cloud"));
			if ((cloud != null)
				&& cloud.GetBooleanAttribute("is_public_cloud", false))
			{
				foreach (Node locationReference in cloud.GetChildrenOfType("cloud_location"))
				{
					Node rack = model.GetNamedNode(locationReference.GetAttribute("location"));

					foreach (Node server in rack.GetChildrenOfType("server"))
					{
						return server;
					}
				}
			}

			return null;
		}

		public bool AddMassCloudVendorChangePlanToQueue (Node plannedOrders, List<Node> businessServices, Node newProvider)
		{
			foreach (Node businessService in businessServices)
			{
				Node cloudLocation = GetCurrentCloudDeploymentLocationIfAny(businessService);
				if (cloudLocation != null)
				{
					Node currentProvider = model.GetNamedNode(businessService.GetAttribute("cloud_provider"));
					if (currentProvider != newProvider)
					{
						string chargeModel = businessService.GetAttribute("cloud_charge_model");
						string buildType = businessService.GetAttribute("cloud_build_type");

						if (! AddCloudDeploymentPlanToQueue(plannedOrders, businessService, cloudLocation, buildType, chargeModel, newProvider))
						{
							return false;
						}
					}
				}
			}

			return true;
		}

		public Node GetSelectedCloudProvider (Node businessService)
		{
			string cloudProviderName = businessService.GetAttribute("cloud_provider");
			if (cloudProviderName != null)
			{
				return model.GetNamedNode(cloudProviderName);
			}

			return null;
		}

		public void AddOptionalDemandsPlanToQueue (Node plannedOrders, Dictionary<Node, bool> demandToActive)
		{
			List<AttributeValuePair> attributes = new List<AttributeValuePair> ();
			attributes.Add(new AttributeValuePair ("type", "order"));
			attributes.Add(new AttributeValuePair ("order", "set_optional_demands"));
			Node order = new Node (plannedOrders, "order", "", attributes);

			foreach (Node demand in demandToActive.Keys)
			{
				attributes.Clear();
				attributes.Add(new AttributeValuePair ("demand", demand.GetAttribute("name")));
				attributes.Add(new AttributeValuePair ("active", demandToActive[demand]));
				new Node (order, "", "", attributes);
			}
		}

		public void AddDiscardServicePlanToQueue (Node plannedOrders, Node businessService)
		{
			List<AttributeValuePair> attributes = new List<AttributeValuePair> ();
			attributes.Add(new AttributeValuePair ("type", "order"));
			attributes.Add(new AttributeValuePair ("order", "delete_service"));
			attributes.Add(new AttributeValuePair ("service", businessService.GetAttribute("name")));
			new Node (plannedOrders, "order", "", attributes);			
		}
	}
}