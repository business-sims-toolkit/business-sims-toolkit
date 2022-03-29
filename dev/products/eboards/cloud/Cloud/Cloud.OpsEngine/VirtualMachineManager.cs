using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

using Network;
using GameManagement;

using LibCore;
using CoreUtils;

namespace Cloud.OpsEngine
{
	public class VirtualMachineManager : IDisposable
	{
		BauManager bauManager;

		NodeTree model;
		Node vmmNode;
		Node timeNode;

		List<Node> watchedBusinessServices;

		public VirtualMachineManager (NodeTree model, BauManager bauManager)
		{
			this.model = model;
			this.bauManager = bauManager;

			vmmNode = model.GetNamedNode("VirtualMachineManager");

			timeNode = model.GetNamedNode("CurrentTime");
			timeNode.AttributesChanged += new Node.AttributesChangedEventHandler(timeNode_AttributesChanged);

			foreach (Node rack in model.GetNodesWithAttributeValue("type", "rack"))
			{
				rack.AttributesChanged += new Node.AttributesChangedEventHandler(rack_AttributesChanged);
			}

			watchedBusinessServices = new List<Node>();
			foreach (Node business in model.GetNodesWithAttributeValue("type", "business"))
			{
				business.ChildAdded += new Node.NodeChildAddedEventHandler(business_ChildAdded);
				business.ChildRemoved += new Node.NodeChildRemovedEventHandler(business_ChildRemoved);

				foreach (Node businessService in business.GetChildrenOfType("business_service"))
				{
					WatchBusinessService(businessService);
				}
			}
		}

		public void Dispose ()
		{
			timeNode.AttributesChanged -= new Node.AttributesChangedEventHandler (timeNode_AttributesChanged);

			foreach (Node rack in model.GetNodesWithAttributeValue("type", "rack"))
			{
				rack.AttributesChanged -= new Node.AttributesChangedEventHandler (rack_AttributesChanged);
			}

			foreach (Node business in model.GetNodesWithAttributeValue("type", "business"))
			{
				business.ChildAdded -= new Node.NodeChildAddedEventHandler (business_ChildAdded);
				business.ChildRemoved -= new Node.NodeChildRemovedEventHandler (business_ChildRemoved);
			}

			foreach (Node businessService in new List<Node> (watchedBusinessServices))
			{
				UnWatchBusinessService(businessService);
			}
		}

		void business_ChildAdded (Node sender, Node child)
		{
			WatchBusinessService(sender);
			AssignCpus();
		}

		void business_ChildRemoved (Node sender, Node child)
		{
			UnWatchBusinessService(sender);
		}

		void WatchBusinessService (Node businessService)
		{
			watchedBusinessServices.Add(businessService);
			businessService.AttributesChanged += new Node.AttributesChangedEventHandler (businessService_AttributesChanged);
		}

		void UnWatchBusinessService (Node businessService)
		{
			watchedBusinessServices.Remove(businessService);
			businessService.AttributesChanged -= new Node.AttributesChangedEventHandler (businessService_AttributesChanged);
		}

		void businessService_AttributesChanged (Node sender, System.Collections.ArrayList attrs)
		{
            bool needToReassignCpus = (timeNode.GetIntAttribute("seconds", 0) == 0);

			foreach (AttributeValuePair avp in attrs)
			{
				if ((avp.Attribute == "status")
					|| (avp.Attribute == "cloud"))
				{
					needToReassignCpus = true;
					break;
				}
			}

			if (needToReassignCpus)
			{
				AssignCpus();
			}
		}

		void rack_AttributesChanged (Node sender, System.Collections.ArrayList attrs)
		{
			bool ownerChanged = false;
			string owner = "";

			foreach (AttributeValuePair avp in attrs)
			{
				if (avp.Attribute == "owner")
				{
					ownerChanged = true;
					owner = avp.Value;
					break;
				}
			}

			if (ownerChanged)
			{
				Node oldCloud = GetCloudControllingLocation(sender);
				Node newCloud = GetCloudControllingRack(sender, owner);

				if (newCloud != oldCloud)
				{
					if (oldCloud != null)
					{
						Node locationReference = null;
						foreach (Node tryLocationReference in oldCloud.GetChildrenOfType("cloud_location"))
						{
							if (tryLocationReference.GetAttribute("location") == sender.GetAttribute("name"))
							{
								locationReference = tryLocationReference;
								break;
							}
						}

						if (locationReference != null)
						{
							locationReference.Parent.DeleteChildTree(locationReference);
						}
					}

					if (newCloud != null)
					{
						List<AttributeValuePair> attributes = new List<AttributeValuePair> ();
						attributes.Add(new AttributeValuePair ("type", "cloud_location"));
						attributes.Add(new AttributeValuePair ("location", sender.GetAttribute("name")));
						new Node (newCloud, "cloud_location", "", attributes);
					}
				}
			}
		}

		Node GetCloudControllingRack (Node rack, string newOwner)
		{
			Node rackCloud = model.GetNamedNode(rack.GetAttribute("name") + " Cloud");
			Node newCloud = null;
			Node datacenter = rack.Parent;
			Node business = model.GetNamedNode(datacenter.GetAttribute("business"));
			string region = business.GetAttribute("region");

			if (string.IsNullOrEmpty(newOwner))
			{
				newCloud = rackCloud;
			}
			else
			{
				Node roundVariablesNode = model.GetNamedNode("RoundVariables");
				int round = roundVariablesNode.GetIntAttribute("current_round", 0);
				string cloudName = null;

				if (newOwner == "dev&test")
				{
					if (round == 1)
					{
						cloudName = region + " Dev&Test Cloud";
					}
					else
					{
						cloudName = "Global Dev&Test Cloud";
					}
				}
				else if (newOwner == "traditional")
				{
					cloudName = rackCloud.GetAttribute("name");
				}
				else
				{
					if (round == 1)
					{
						cloudName = rackCloud.GetAttribute("name");
					}
					else
					{
						cloudName = region + " Production Cloud";
					}
				}

				newCloud = model.GetNamedNode(cloudName);
			}

			return newCloud;
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
				if ((timeNode.GetIntAttribute("seconds", 0) % 60) == 0)
				{
					AssignCpus();
				}
			}
		}

		public List<Node> GetAllServersInCloud (Node cloud)
		{
			List<Node> servers = new List<Node> ();

			foreach (Node cloudLocation in cloud.GetChildrenOfType("cloud_location"))
			{
				Node rack = model.GetNamedNode(cloudLocation.GetAttribute("location"));
				foreach (Node server in rack.GetChildrenOfType("server"))
				{
					servers.Add(server);
				}
			}

			return servers;
		}

		void AddRange (IDictionary<string, int> list1, IDictionary<string, int> list2)
		{
			foreach (string serverName in list2.Keys)
			{
				if (list1.ContainsKey(serverName))
				{
					list1[serverName] += list2[serverName];
				}
				else
				{
					list1.Add(serverName, list2[serverName]);
				}
			}
		}

		public Dictionary<string, int> GetCloudServerNamesToCpuCounts (Node plannedOrders, Node cloud, int time, out Dictionary<string, int> unreadyServerNameToCpus)
		{
			Dictionary<string, int> serverNameToCpus = new Dictionary<string, int> ();
			unreadyServerNameToCpus = new Dictionary<string, int> ();
			foreach (Node location in cloud.GetChildrenOfType("cloud_location"))
			{
				AddRange(serverNameToCpus, GetServerNamesToCpuCountsInLocation(plannedOrders, model.GetNamedNode(location.GetAttribute("location")), time, ref unreadyServerNameToCpus));
			}

			if (plannedOrders != null)
			{
				int currentNetworkTime = timeNode.GetIntAttribute("seconds", 0);

				foreach (Node order in plannedOrders.GetChildrenOfType("order"))
				{
					if (order.GetAttribute("order") == "commission_server")
					{
						Node rack = model.GetNamedNode(order.GetAttribute("rack"));
						string owner = order.GetAttribute("owner");

						Node newOwningCloud = GetCloudControllingRack(rack, owner);

						if (newOwningCloud == cloud)
						{
							string serverName = order.GetAttribute("server_name");
							Node serverType = model.GetNamedNode(order.GetAttribute("server_type"));
							if (time >= (currentNetworkTime + serverType.GetIntAttribute("purchase_delay", 0)))
							{
								serverNameToCpus[serverName] = serverType.GetIntAttribute("cpus", 0);
							}
							else
							{
								unreadyServerNameToCpus[serverName] = serverType.GetIntAttribute("cpus", 0);
							}
						}
					}
				}
			}

			return serverNameToCpus;
		}

		List<Node> GetAllBusinessServicesManagedByCloud (Node cloud)
		{
			List<Node> businessServices = new List<Node>();
			foreach (Node businessService in model.GetNodesWithAttributeValue("type", "business_service"))
			{
				if (businessService.GetAttribute("cloud") == cloud.GetAttribute("name"))
				{
					businessServices.Add(businessService);
				}
			}

			return businessServices;
		}

		static int GetNextMinuteOnOrAfter (int time)
		{
			return time + ((60 - time) % 60);
		}

		public int GetMaxCpusNeeded (Node businessServiceOrDefinition)
		{
			int cpusNeeded = 0;

			for (int time = GetNextMinuteOnOrAfter(timeNode.GetIntAttribute("seconds", 0));
				 time <= timeNode.GetIntAttribute("round_duration", 0);
				 time += 60)
			{
				cpusNeeded = Math.Max(cpusNeeded, bauManager.GetCpusNeeded(businessServiceOrDefinition, time, false));
			}

			return cpusNeeded;
		}

		int GetTotalCpus (IDictionary<string, int> serverNameToCpus)
		{
			int total = 0;
			foreach (string serverName in serverNameToCpus.Keys)
			{
				total += serverNameToCpus[serverName];
			}

			return total;
		}

		public Dictionary<Node, Dictionary<string, int>> GetCpuAssignments (Node plannedOrders, Node cloud, Dictionary<string, Dictionary<Node, int>> serverNameToVmInstanceToPriorCpuCount, int time)
		{
			Dictionary<Node, Dictionary<string, int>> businessServiceToServerNameToCpus = new Dictionary<Node, Dictionary<string, int>> ();

			Dictionary<string, int> unreadyServerNameToCpus;
			Dictionary<string, int> serverNameToCpuCount = GetCloudServerNamesToCpuCounts(plannedOrders, cloud, time, out unreadyServerNameToCpus);
			List<Node> businessServices = GetAllBusinessServicesManagedByCloud(cloud);

			Dictionary<Node, int> businessServiceToCpusNeeded = new Dictionary<Node, int> ();
			Dictionary<Node, Dictionary<string, int>> businessServiceToServerNameToDynamicCpus = new Dictionary<Node, Dictionary<string, int>> ();
			Dictionary<Node, Dictionary<string, int>> businessServiceToServerNameToStaticCpus = new Dictionary<Node, Dictionary<string, int>> ();

			// Order the business services by creation order, so older services get given CPUs first, and
			// only later-added services can run out.
			businessServices.Sort(delegate (Node a, Node b)
								{
									int creationComparison = a.GetIntAttribute("created_in_round", 0).CompareTo(b.GetIntAttribute("created_in_round", 0));
									if (creationComparison != 0)
									{
										return creationComparison;
									}

									return a.GetAttribute("name").CompareTo(b.GetAttribute("name"));
								});

			// First see what CPUs each service currently has, and what it needs.
			foreach (Node businessService in businessServices)
			{
				Dictionary<string, int> serverNameToStaticCpuCountForThisService = new Dictionary<string, int>();
				Dictionary<string, int> serverNameToDynamicCpuCountForThisService = new Dictionary<string, int> ();
				int serviceCpusNeeded = 0;

				// Find what CPUs we currently have.
				Node vmInstance = model.GetNamedNode(businessService.GetAttribute("vm_instance"));
				if (vmInstance != null)
				{
					foreach (string serverName in serverNameToVmInstanceToPriorCpuCount.Keys)
					{
						if (serverNameToVmInstanceToPriorCpuCount[serverName].ContainsKey(vmInstance))
						{
							if (serverNameToCpuCount.ContainsKey(serverName))
							{
								if (! serverNameToDynamicCpuCountForThisService.ContainsKey(serverName))
								{
									serverNameToDynamicCpuCountForThisService.Add(serverName, 0);
								}

								serverNameToDynamicCpuCountForThisService[serverName] += serverNameToVmInstanceToPriorCpuCount[serverName][vmInstance];
							}
							else
							{
								if (! serverNameToStaticCpuCountForThisService.ContainsKey(serverName))
								{
									serverNameToStaticCpuCountForThisService.Add(serverName, 0);
								}

								serverNameToStaticCpuCountForThisService[serverName] += serverNameToVmInstanceToPriorCpuCount[serverName][vmInstance];
							}
						}
					}
				}

				serviceCpusNeeded = bauManager.GetCpusNeeded(businessService, time, businessService.GetBooleanAttribute("is_dev", false));

				// Release any unwanted CPUs.
				if ((GetTotalCpus(serverNameToStaticCpuCountForThisService) + GetTotalCpus(serverNameToDynamicCpuCountForThisService)) > serviceCpusNeeded)
				{
					int excessCpus = GetTotalCpus(serverNameToStaticCpuCountForThisService) + GetTotalCpus(serverNameToDynamicCpuCountForThisService) - serviceCpusNeeded;
					int dynamicCpusToRelease = Math.Min(excessCpus, GetTotalCpus(serverNameToDynamicCpuCountForThisService));
					
					foreach (string serverName in new List<string> (serverNameToDynamicCpuCountForThisService.Keys))
					{
						int dynamicCpusToReleaseFromThisServer = Math.Min(dynamicCpusToRelease, serverNameToDynamicCpuCountForThisService[serverName]);
						dynamicCpusToRelease -= dynamicCpusToReleaseFromThisServer;
						serverNameToDynamicCpuCountForThisService[serverName] -= dynamicCpusToReleaseFromThisServer;
					}
				}

				businessServiceToCpusNeeded.Add(businessService, serviceCpusNeeded);

				if (! businessServiceToServerNameToDynamicCpus.ContainsKey(businessService))
				{
					businessServiceToServerNameToDynamicCpus.Add(businessService, new Dictionary<string, int> ());
				}
				if (! businessServiceToServerNameToStaticCpus.ContainsKey(businessService))
				{
					businessServiceToServerNameToStaticCpus.Add(businessService, new Dictionary<string, int> ());
				}

				AddRange(businessServiceToServerNameToDynamicCpus[businessService], serverNameToDynamicCpuCountForThisService);
				AddRange(businessServiceToServerNameToStaticCpus[businessService], serverNameToStaticCpuCountForThisService);
			}

			// Now build a list of all free CPUs.
			Dictionary<string, int> serverNameToFreeCpus = new Dictionary<string, int> ();
			foreach (string serverName in serverNameToCpuCount.Keys)
			{
				serverNameToFreeCpus.Add(serverName, serverNameToCpuCount[serverName]);

				foreach (Node businessService in businessServiceToServerNameToDynamicCpus.Keys)
				{
					if (businessServiceToServerNameToDynamicCpus[businessService].ContainsKey(serverName))
					{
						serverNameToFreeCpus[serverName] -= businessServiceToServerNameToDynamicCpus[businessService][serverName];
					}
				}
			}

			// Now claim more CPUs for each service, as necessary.
			foreach (Node businessService in businessServices)
			{
				// Do we need more CPUs?
				if ((GetTotalCpus(businessServiceToServerNameToDynamicCpus[businessService]) + GetTotalCpus(businessServiceToServerNameToStaticCpus[businessService]))
					< businessServiceToCpusNeeded[businessService])
				{
					int extraCpus = businessServiceToCpusNeeded[businessService]
									- GetTotalCpus(businessServiceToServerNameToDynamicCpus[businessService])
									- GetTotalCpus(businessServiceToServerNameToStaticCpus[businessService]);									

					// Are there not enough CPUs free?
					if (extraCpus > GetTotalCpus(serverNameToFreeCpus))
					{
						// If we're a public cloud provider, more can be created as needed.
						if (cloud.GetBooleanAttribute("is_public_cloud", false))
						{
							Node server = null;
							foreach (Node locationReference in cloud.GetChildrenOfType("cloud_location"))
							{
								Node rack = model.GetNamedNode(locationReference.GetAttribute("location"));
								foreach (Node tryServer in rack.GetChildrenOfType("server"))
								{
									server = tryServer;
									break;
								}
							}

							Debug.Assert(server != null);
							Debug.Assert(server.GetBooleanAttribute("is_cloud_server", false));

							while (GetTotalCpus(serverNameToFreeCpus) < extraCpus)
							{
								server.SetAttribute("cpus", server.GetIntAttribute("cpus", 0) + 1);
								serverNameToFreeCpus[server.GetAttribute("name")]++;
							}
						}
					}

					int dynamicCpusToClaim = Math.Min(extraCpus, GetTotalCpus(serverNameToFreeCpus));

					Dictionary<string, int> serverNameToCpusToClaim = new Dictionary<string, int> ();
					foreach (string serverName in new List<string> (serverNameToFreeCpus.Keys))
					{
						int cpusToTakeFromThisServer = Math.Min(dynamicCpusToClaim, serverNameToFreeCpus[serverName]);
						dynamicCpusToClaim -= cpusToTakeFromThisServer;
						serverNameToFreeCpus[serverName] -= cpusToTakeFromThisServer;

						if (! businessServiceToServerNameToDynamicCpus[businessService].ContainsKey(serverName))
						{
							businessServiceToServerNameToDynamicCpus[businessService].Add(serverName, 0);
						}

						businessServiceToServerNameToDynamicCpus[businessService][serverName] += cpusToTakeFromThisServer;
					}
				}

				businessServiceToServerNameToCpus.Add(businessService, new Dictionary<string, int> ());
				AddRange(businessServiceToServerNameToCpus[businessService], businessServiceToServerNameToDynamicCpus[businessService]);
			}

			return businessServiceToServerNameToCpus;
		}

		public Dictionary<string, Dictionary<Node, int>> GetCurrentCpuAssignments (IList<string> cloudServerNames)
		{
			Dictionary<string, Dictionary<Node, int>> serverNameToVmInstanceToCpus = new Dictionary<string, Dictionary<Node, int>> ();
			foreach (string serverName in cloudServerNames)
			{
				Node server = model.GetNamedNode(serverName);
				if (server != null)
				{
					serverNameToVmInstanceToCpus.Add(serverName, new Dictionary<Node, int> ());

					foreach (Node serverLinkToVmInstance in server.GetChildrenOfType("server_link_to_vm_instance"))
					{
						Node vmInstance = model.GetNamedNode(serverLinkToVmInstance.GetAttribute("vm_instance"));
						int cpus = serverLinkToVmInstance.GetIntAttribute("cpus", 0);

						serverNameToVmInstanceToCpus[serverName].Add(vmInstance, cpus);
					}
				}
			}

			return serverNameToVmInstanceToCpus;
		}

		public void AssignCpus ()
		{
            int time = timeNode.GetIntAttribute("seconds", 0);

            foreach (Node cloud in vmmNode.GetChildrenOfType("cpu_cloud"))
            {
                // Build a list of current CPU assignments.
                Dictionary<string, int> unreadyServerNameToCpus;
				Dictionary<string, int> serverNameToCpus = GetCloudServerNamesToCpuCounts(null, cloud, time, out unreadyServerNameToCpus);
                Dictionary<string, Dictionary<Node, int>> serverNameToVmInstanceToPriorCpus = GetCurrentCpuAssignments(new List<string> (serverNameToCpus.Keys));

                Dictionary<Node, Dictionary<string, int>> businessServiceToServerNameToCpus = GetCpuAssignments(null, cloud, serverNameToVmInstanceToPriorCpus, time);

                // Reassign CPUs as necessary.
                foreach (Node businessService in businessServiceToServerNameToCpus.Keys)
                {
                    Node vmInstance = model.GetNamedNode(businessService.GetAttribute("vm_instance"));
                    if (vmInstance != null)
                    {
						Dictionary<string, int> serverNameToCpusOccupied = new Dictionary<string, int> ();

                        // Now release CPUs as needed.
						foreach (Node vmInstanceLinkToServer in vmInstance.GetChildrenOfType("vm_instance_link_to_server"))
						{
							string serverName = vmInstanceLinkToServer.GetAttribute("server");
							int cpusHeld = vmInstanceLinkToServer.GetIntAttribute("cpus", 0);

							Node serverLinkToVmInstance = GetServerLinkToVmInstance(vmInstanceLinkToServer);

							// Is this a cloud-managed server?
							if (serverNameToCpus.ContainsKey(serverName))
							{
								int cpusWanted = 0;
								if (businessServiceToServerNameToCpus[businessService].ContainsKey(serverName))
								{
									cpusWanted = businessServiceToServerNameToCpus[businessService][serverName];
								}

								// Do we want to release all the CPUs?
								if (cpusWanted == 0)
								{
									serverLinkToVmInstance.Parent.DeleteChildTree(serverLinkToVmInstance);
									vmInstanceLinkToServer.Parent.DeleteChildTree(vmInstanceLinkToServer);
								}
								// Or some?
								else if (cpusWanted < cpusHeld)
								{
									Debug.Assert(vmInstanceLinkToServer.GetIntAttribute("cpus", 0) == serverLinkToVmInstance.GetIntAttribute("cpus", 0));
									if (vmInstanceLinkToServer.GetIntAttribute("cpus", 0) != cpusWanted)
									{
										vmInstanceLinkToServer.SetAttribute("cpus", cpusWanted);
										serverLinkToVmInstance.SetAttribute("cpus", cpusWanted);
									}
	                                serverNameToCpusOccupied.Add(serverName, cpusWanted);
								}
							}
							else
							{
                                serverNameToCpusOccupied.Add(serverName, cpusHeld);
							}
                        }

						// Now claim any extra.
						foreach (string serverName in businessServiceToServerNameToCpus[businessService].Keys)
						{
							int cpusHeld = 0;
							if (serverNameToCpusOccupied.ContainsKey(serverName))
							{
								cpusHeld = serverNameToCpusOccupied[serverName];
							}

							int cpusWanted = businessServiceToServerNameToCpus[businessService][serverName];

							Node server = model.GetNamedNode(serverName);
							Node vmInstanceLinkToServer = null;
							foreach (Node tryVmInstanceLinkToServer in vmInstance.GetChildrenOfType("vm_instance_link_to_server"))
							{
								if (tryVmInstanceLinkToServer.GetAttribute("server") == serverName)
								{
									vmInstanceLinkToServer = tryVmInstanceLinkToServer;
									break;
								}
							}
							Node serverLinkToVmInstance = GetServerLinkToVmInstance(vmInstanceLinkToServer);

							if (cpusWanted > cpusHeld)
							{
								// Do we need to create a new link?
								if (vmInstanceLinkToServer == null)
								{
									List<AttributeValuePair> attributes = new List<AttributeValuePair> ();

									attributes.Clear();
									attributes.Add(new AttributeValuePair ("type", "vm_instance_link_to_server"));
									attributes.Add(new AttributeValuePair ("server", serverName));
									attributes.Add(new AttributeValuePair ("vm_instance", vmInstance.GetAttribute("name")));
									attributes.Add(new AttributeValuePair ("cpus", cpusWanted));
									new Node (vmInstance, "vm_instance_link_to_server",
									         CONVERT.Format("{0} link to {1}", vmInstance.GetAttribute("name"), serverName),
									         attributes);

									attributes.Clear();
									attributes.Add(new AttributeValuePair ("type", "server_link_to_vm_instance"));
									attributes.Add(new AttributeValuePair ("server", serverName));
									attributes.Add(new AttributeValuePair ("vm_instance", vmInstance.GetAttribute("name")));
									attributes.Add(new AttributeValuePair ("cpus", cpusWanted));
									new Node (server, "server_link_to_vm_instance",
									          CONVERT.Format("{0} link to {1}", serverName, vmInstance.GetAttribute("name")),
									          attributes);
								}
								else
								{
									Debug.Assert(vmInstanceLinkToServer.GetIntAttribute("cpus", 0) == serverLinkToVmInstance.GetIntAttribute("cpus", 0));

									if (vmInstanceLinkToServer.GetIntAttribute("cpus", 0) != cpusWanted)
									{
										vmInstanceLinkToServer.SetAttribute("cpus", cpusWanted);
										serverLinkToVmInstance.SetAttribute("cpus", cpusWanted);
									}
								}

								serverNameToCpusOccupied[serverName] = cpusWanted;
							}
						}

						int cpusNeeded = bauManager.GetCpusNeeded(businessService, time, businessService.GetBooleanAttribute("is_dev", false));
                        if (businessService.GetAttribute("status").StartsWith("waiting"))
                        {
                            bool waitingOnHandover = (businessService.GetAttribute("status") == "waiting_on_handover");
                            if ((businessService.GetIntAttribute("handover_countdown", 0) > 0)
                                || (! waitingOnHandover))
                            {
                                cpusNeeded = 0;
                            }
                        }

                        int idleCount = GetTotalCpus(serverNameToCpusOccupied) - cpusNeeded;

                        bool isPlaceholder = (businessService.GetBooleanAttribute("is_placeholder", false)
                                              && (GetTotalCpus(serverNameToCpusOccupied) == 0));

                        if (isPlaceholder)
                        {
                            idleCount = 0;
                        }

						idleCount = Math.Max(0, idleCount);

                        // How many CPUs are idle?
                        Debug.Assert(idleCount >= 0);

                        // And set the idle CPU count.
                        if (vmInstance.GetIntAttribute("idle_cpus", 0) != idleCount)
                        {
                            vmInstance.SetAttribute("idle_cpus", idleCount);
                        }
                    }
                }
            }
		}

		Node GetServerLinkToVmInstance (Node vmInstanceLinkToServer)
		{
			if (vmInstanceLinkToServer != null)
			{
				Node server = vmInstanceLinkToServer.Tree.GetNamedNode(vmInstanceLinkToServer.GetAttribute("server"));
				foreach (Node serverLinkToVmInstance in server.GetChildrenOfType("server_link_to_vm_instance"))
				{
					if (serverLinkToVmInstance.GetAttribute("vm_instance") == vmInstanceLinkToServer.Parent.GetAttribute("name"))
					{
						return serverLinkToVmInstance;
					}
				}

				Debug.Assert(false);
			}

			return null;
		}

		IDictionary<string, int> GetServerNamesToCpuCountsInLocation (Node plannedOrders, Node location, int time, ref Dictionary<string, int> unreadyServerNameToCpuCount)
		{
			int currentNetworkTime = timeNode.GetIntAttribute("seconds", 0);

			Dictionary<string, int> serverNameToCpus = new Dictionary<string, int> ();

			switch (location.GetAttribute("type"))
			{
				case "server":
					int timeTillReady = location.GetIntAttribute("time_till_ready", 0);
					if ((timeTillReady == 0)
						|| (time >= (currentNetworkTime + timeTillReady)))
					{
						serverNameToCpus.Add(location.GetAttribute("name"), location.GetIntAttribute("cpus", 0));
					}
					else
					{
						unreadyServerNameToCpuCount.Add(location.GetAttribute("name"), location.GetIntAttribute("cpus", 0));
					}
					break;

				case "rack":
					foreach (Node server in location.GetChildrenOfType("server"))
					{
						AddRange(serverNameToCpus, GetServerNamesToCpuCountsInLocation(plannedOrders, server, time, ref unreadyServerNameToCpuCount));
					}

					if (plannedOrders != null)
					{
						foreach (Node order in plannedOrders.GetChildrenOfType("order"))
						{
							switch (order.GetAttribute("order"))
							{
								case "commission_server":
									{
										Node orderRack = model.GetNamedNode(order.GetAttribute("rack"));
										if (orderRack == location)
										{
											string serverName = order.GetAttribute("server_name");
											Node serverType = model.GetNamedNode(order.GetAttribute("server_type"));

											int delay = serverType.GetIntAttribute("purchase_delay", 0);

											if (time >= (currentNetworkTime + delay))
											{
												serverNameToCpus[serverName] = serverType.GetIntAttribute("cpus", 0);
											}
											else
											{
												unreadyServerNameToCpuCount.Add(serverName, serverType.GetIntAttribute("cpus", 0));
											}
										}
									}
									break;

								case "delete_server":
									{
										string serverName = order.GetAttribute("server_name");
										serverNameToCpus.Remove(serverName);
									}
									break;
							}
						}
					}

					break;

				case "datacenter":
					foreach (Node rack in location.GetChildrenOfType("rack"))
					{
						AddRange(serverNameToCpus, GetServerNamesToCpuCountsInLocation(plannedOrders, rack, time, ref unreadyServerNameToCpuCount));
					}
					break;

				case "network":
					foreach (Node datacenter in location.GetChildrenOfType("datacenter"))
					{
						AddRange(serverNameToCpus, GetServerNamesToCpuCountsInLocation(plannedOrders, datacenter, time, ref unreadyServerNameToCpuCount));
					}
					break;

				default:
					Debug.Assert(false);
					break;
			}

			return serverNameToCpus;
		}

		public enum CloudCapacityState
		{
			Insufficient,
			SufficientButNotReadyInTime,
			Sufficient
		}

		public CloudCapacityState DoesCloudHaveCapacityForService (Node plannedOrders, Node businessServiceOrDefinition, Node cloud, out int hardwareDelay, bool isDev)
		{
			Dictionary<string, Dictionary<Node, int>> serverNameToVmInstanceToCpus = null;
			CloudCapacityState worstState = CloudCapacityState.Sufficient;
			hardwareDelay = 0;

			for (int time = GetNextMinuteOnOrAfter(timeNode.GetIntAttribute("seconds", 0));
				 time <= timeNode.GetIntAttribute("round_duration", 0);
				 time += 60)
			{
				Dictionary<string, int> unreadyServerNameToCpus;
				Dictionary<string, int> cloudServerNameToCpus = GetCloudServerNamesToCpuCounts(plannedOrders, cloud, time, out unreadyServerNameToCpus);
				if (serverNameToVmInstanceToCpus == null)
				{
					serverNameToVmInstanceToCpus = GetCurrentCpuAssignments(new List<string> (cloudServerNameToCpus.Keys));
				}

				int cpusNeeded = bauManager.GetCpusNeeded(businessServiceOrDefinition, time, isDev);

				Dictionary<Node, Dictionary<string, int>> businessServiceToServerNameToCpus = GetCpuAssignments(plannedOrders, cloud, serverNameToVmInstanceToCpus, time);

				serverNameToVmInstanceToCpus = new Dictionary<string, Dictionary<Node, int>> ();
				foreach (Node businessService in businessServiceToServerNameToCpus.Keys)
				{
					Node vmInstance = model.GetNamedNode(businessService.GetAttribute("vm_instance"));
					foreach (string serverName in businessServiceToServerNameToCpus[businessService].Keys)
					{
						if (! serverNameToVmInstanceToCpus.ContainsKey(serverName))
						{
							serverNameToVmInstanceToCpus.Add(serverName, new Dictionary<Node, int> ());
						}

						if (! serverNameToVmInstanceToCpus[serverName].ContainsKey(vmInstance))
						{
							serverNameToVmInstanceToCpus[serverName].Add(vmInstance, 0);
						}

						serverNameToVmInstanceToCpus[serverName][vmInstance] += businessServiceToServerNameToCpus[businessService][serverName];
					}
				}

				Dictionary<string, int> serverNameToFreeCpus = new Dictionary<string, int> (cloudServerNameToCpus);
				foreach (Node businessService in businessServiceToServerNameToCpus.Keys)
				{
					foreach (string serverName in businessServiceToServerNameToCpus[businessService].Keys)
					{
						if (serverNameToFreeCpus.ContainsKey(serverName))
						{
							serverNameToFreeCpus[serverName] -= businessServiceToServerNameToCpus[businessService][serverName];
						}
					}
				}

				if (GetTotalCpus(serverNameToFreeCpus) < cpusNeeded)
				{
					List<Node> cloudLocations = new List<Node> ();
					foreach (Node cloudLocation in cloud.GetChildrenOfType("cloud_location"))
					{
						Node location = model.GetNamedNode(cloudLocation.GetAttribute("location"));
						cloudLocations.Add(location);
					}

					Node cloudServer = null;
					if (cloudLocations.Count == 1)
					{
						Node [] servers = (Node []) cloudLocations[0].GetChildrenOfType("server").ToArray(typeof (Node));
						if (servers.Length == 1)
						{
							cloudServer = servers[0];
						}
					}

					// If this cloud is a public cloud with infinite capacity, then spawn more CPUs.
					if ((cloudServer != null)
						&& cloudServer.GetBooleanAttribute("unlimited_cpus", false))
					{
						int freeCpus = GetTotalCpus(serverNameToFreeCpus);
						if (freeCpus < cpusNeeded)
						{
							int extra = cpusNeeded - freeCpus;
							cloudServer.SetAttribute("cpus", extra + cloudServer.GetIntAttribute("cpus", 0));
							serverNameToFreeCpus[cloudServer.GetAttribute("name")] += extra;
						}
					}
					else
					{
						int extraCpusNeeded = cpusNeeded - GetTotalCpus(serverNameToFreeCpus);

						foreach (Node otherBusinessService in model.GetNodesWithAttributeValue("type", "business_service"))
						{
							if (otherBusinessService.GetAttribute("cloud") == cloud.GetAttribute("name"))
							{
								int otherBusinessServiceCpusNeeded = bauManager.GetCpusNeeded(otherBusinessService, time, true, isDev);

								int cpusUsedByOtherBusiness = (businessServiceToServerNameToCpus.ContainsKey(otherBusinessService) ? GetTotalCpus(businessServiceToServerNameToCpus[otherBusinessService]) : 0);
								int unreadyCpusWanted = otherBusinessServiceCpusNeeded - cpusUsedByOtherBusiness;

								foreach (string serverName in new List<string> (unreadyServerNameToCpus.Keys))
								{
									int cpusToTakeFromThisServer = Math.Min(unreadyCpusWanted, unreadyServerNameToCpus[serverName]);
									unreadyServerNameToCpus[serverName] -= cpusToTakeFromThisServer;
									unreadyCpusWanted -= cpusToTakeFromThisServer;
								}
							}
						}

						// If there'll be enough CPUs eventually, but not right now...
						if (GetTotalCpus(unreadyServerNameToCpus) >= extraCpusNeeded)
						{
							// ...if we're a new service, we'll be delayed...
							if (string.IsNullOrEmpty(businessServiceOrDefinition.GetAttribute("demand_name")))
							{
								worstState = CloudCapacityState.Sufficient;

								Dictionary<string, int> serverNameToDelay = new Dictionary<string, int> ();
								foreach (string serverName in unreadyServerNameToCpus.Keys)
								{
									int serverDelay = 0;

									Node server = model.GetNamedNode(serverName);
									if (server != null)
									{
										serverDelay = server.GetIntAttribute("time_till_ready", 0);
									}
									else
									{
										foreach (Node serverType in model.GetNodesWithAttributeValue("type", "server_spec"))
										{
											if (serverType.GetBooleanAttribute("is_default", false))
											{
												serverDelay = serverType.GetIntAttribute("purchase_delay", 0);
												break;
											}
										}
									}

									serverDelay -= timeNode.GetIntAttribute("seconds", 0);

									serverNameToDelay.Add(serverName, serverDelay);
								}

								List<string> unreadyServerNamesSortedByReadyTime = new List<string> (serverNameToDelay.Keys);
								unreadyServerNamesSortedByReadyTime.Sort(delegate (string a, string b) { return serverNameToDelay[a].CompareTo(serverNameToDelay[b]); });

								while ((extraCpusNeeded > 0)
									   && (unreadyServerNamesSortedByReadyTime.Count > 0))
								{
									string serverNameToTake = unreadyServerNamesSortedByReadyTime[0];
									unreadyServerNamesSortedByReadyTime.Remove(serverNameToTake);
									hardwareDelay = Math.Max(hardwareDelay, serverNameToDelay[serverNameToTake]);

									int cpusToTake = Math.Min(unreadyServerNameToCpus[serverNameToTake], extraCpusNeeded);
									extraCpusNeeded -= cpusToTake;
									unreadyServerNameToCpus.Remove(serverNameToTake);
								}
							}
							// ...or if we're a demand, we have a problem.
							else
							{
								worstState = CloudCapacityState.SufficientButNotReadyInTime;
							}
						}
						else
						{
							// Not a chance.
							return CloudCapacityState.Insufficient;
						}
					}
				}
			}

			return worstState;
		}

		public bool CanServersBeRetired (List<Node> servers)
		{
			return false;
		}

		public Node GetCloudControllingLocation (Node location)
		{
			foreach (Node cloud in vmmNode.GetChildrenOfType("cpu_cloud"))
			{
				foreach (Node cloudLocation in cloud.GetChildrenOfType("cloud_location"))
				{
					if (cloudLocation.GetAttribute("location") == location.GetAttribute("name"))
					{
						return cloud;
					}
				}
			}

			switch (location.GetAttribute("type"))
			{
				case "server":
					return GetCloudControllingLocation(location.Parent);

				case "cpu_cloud":
					return location;
			}

			return null;
		}

		bool IsRackControlledOrSuitableForControlByCloud (Node rack, Node cloud)
		{
			Node rackCloud = GetCloudControllingLocation(rack);

			if (rackCloud == cloud)
			{
				return true;
			}
			else if (rackCloud == null)
			{
				if (cloud.GetAttribute("name").EndsWith("Production Cloud"))
				{
					string regionName = cloud.GetAttribute("name").Replace(" Production Cloud", "");

					if (model.GetNamedNode(rack.Parent.GetAttribute("business")).GetAttribute("region") == regionName)
					{
						return true;
					}
				}
				else if (cloud.GetAttribute("name").EndsWith("Dev&Test Cloud"))
				{
					string regionName = cloud.GetAttribute("name").Replace(" Dev&Test Cloud", "");

					if (regionName == "Global")
					{
						return true;
					}
					else
					{
						if (model.GetNamedNode(rack.Parent.GetAttribute("business")).GetAttribute("region") == regionName)
						{
							return true;
						}
					}
				}
			}

			return false;
		}

		public List<Node> GetRacksSuitableForNewServer (Node plannedOrders, OrderPlanner orderPlanner, Node businessServiceOrDefinition, bool devTest, Node newServerDefinition, Node cloud)
		{
			List<Node> racks = new List<Node> ();

			foreach (Node datacenter in model.GetNodesWithAttributeValue("type", "datacenter"))
			{
				if (! datacenter.GetBooleanAttribute("is_cloud", false))
				{
					foreach (Node rack in datacenter.GetChildrenOfType("rack"))
					{
						if ((orderPlanner.GetFreePhysicalSpace(plannedOrders, rack) >= newServerDefinition.GetIntAttribute("height_u", 0))
							&& orderPlanner.CanRackHostOwner(plannedOrders, rack, businessServiceOrDefinition.GetAttribute("owner"), devTest))
						{
							if (IsRackControlledOrSuitableForControlByCloud(rack, cloud))
							{
								racks.Add(rack);
							}
						}
					}
				}
			}

			return racks;
		}

		public bool ValidateCloudIsSuitableDeploymentTarget (Node plannedOrders,
															 OrderPlanner orderPlanner,
															 Node cloud,
															 Node businessServiceOrDefinition,
															 bool devTest)
		{
			string owner = (devTest ? "dev&test" : businessServiceOrDefinition.GetAttribute("owner"));

			foreach (Node rackReference in cloud.GetChildrenOfType("cloud_location"))
			{
				Node rack = model.GetNamedNode(rackReference.GetAttribute("location"));

				if (! orderPlanner.CanRackHostOwner(plannedOrders, rack, owner))
				{
					orderPlanner.AddError(plannedOrders, (devTest ? "dev" : "production"),
										  "{0} owned by {1}",
										  rack.GetAttribute("desc"),
										  Strings.SentenceCase(rack.GetAttribute("owner")));

					return false;
				}
			}

			return true;
		}

		public Dictionary<Node, int> GetMinFreeCpusOverTimeForRacks ()
		{
			int startTime = timeNode.GetIntAttribute("seconds", 0);

			// Work out which clouds we're dealing with, and which racks each controls.
			Dictionary<Node, List<Node>> cloudToRacks = new Dictionary<Node, List<Node>> ();
			Dictionary<Node, Dictionary<string, int>> cloudToServerNameToCpus = new Dictionary<Node, Dictionary<string, int>> ();

			foreach (Node rack in model.GetNodesWithAttributeValue("type", "rack"))
			{
				Node cloud = GetCloudControllingLocation(rack);

				if (cloud != null)
				{
					if (! cloudToRacks.ContainsKey(cloud))
					{
						cloudToRacks.Add(cloud, new List<Node> ());
					}
					cloudToRacks[cloud].Add(rack);
				}
			}

			// Now run forward in time, assigning CPUs as needed.
			Dictionary<string, Dictionary<Node, int>> serverNameToVmInstanceToCpus = null;
			Dictionary<Node, int> cloudToMinFreeCpus = new Dictionary<Node, int> ();
			for (int minute = (int) Math.Ceiling(startTime / 60.0);
					minute <= Math.Ceiling(timeNode.GetIntAttribute("round_duration", 0) / 60.0);
					minute++)
			{
				int time = 60 * minute;

				// Get all the CPUs in this datacenter.
				Dictionary<string, int> serverNameToCpus = new Dictionary<string, int> ();
				cloudToServerNameToCpus.Clear();
				foreach (Node cloud in cloudToRacks.Keys)
				{
					Dictionary<string, int> unreadyServerNameToCpus;
					cloudToServerNameToCpus.Add(cloud, GetCloudServerNamesToCpuCounts(null, cloud, time, out unreadyServerNameToCpus));
					AddRange(serverNameToCpus, cloudToServerNameToCpus[cloud]);
				}

				if (serverNameToVmInstanceToCpus == null)
				{
					serverNameToVmInstanceToCpus = GetCurrentCpuAssignments(new List<string> (serverNameToCpus.Keys));
				}

				Dictionary<Node, Dictionary<string, int>> businessServiceToServerNameToCpus = new Dictionary<Node,Dictionary<string, int>> ();
				foreach (Node cloud in cloudToRacks.Keys)
				{
					Dictionary<Node, Dictionary<string, int>> cloudBusinessServiceToServerNameToCpus = GetCpuAssignments(null, cloud, serverNameToVmInstanceToCpus, time);

					foreach (Node businessService in cloudBusinessServiceToServerNameToCpus.Keys)
					{
						if (! businessServiceToServerNameToCpus.ContainsKey(businessService))
						{
							businessServiceToServerNameToCpus.Add(businessService, new Dictionary<string, int> ());
						}

						AddRange(businessServiceToServerNameToCpus[businessService], cloudBusinessServiceToServerNameToCpus[businessService]);
					}
				}

				serverNameToVmInstanceToCpus.Clear();
				foreach (Node cloud in cloudToRacks.Keys)
				{
					Dictionary<string, int> serverNameToAssignedCpus = new Dictionary<string, int> ();
					foreach (Node businessService in businessServiceToServerNameToCpus.Keys)
					{
						Node vmInstance = model.GetNamedNode(businessService.GetAttribute("vm_instance"));
						foreach (string serverName in businessServiceToServerNameToCpus[businessService].Keys)
						{
							if (cloudToServerNameToCpus[cloud].ContainsKey(serverName))
							{
								int cpus = businessServiceToServerNameToCpus[businessService][serverName];
								if (serverNameToAssignedCpus.ContainsKey(serverName))
								{
									serverNameToAssignedCpus[serverName] += cpus;
								}
								else
								{
									serverNameToAssignedCpus.Add(serverName, cpus);
								}

								if (! serverNameToVmInstanceToCpus.ContainsKey(serverName))
								{
									serverNameToVmInstanceToCpus.Add(serverName, new Dictionary<Node, int>());
								}

								serverNameToVmInstanceToCpus[serverName][vmInstance] = businessServiceToServerNameToCpus[businessService][serverName];
							}
						}
					}

					int cpusFree = GetTotalCpus(cloudToServerNameToCpus[cloud]) - GetTotalCpus(serverNameToAssignedCpus);
					if (cloudToMinFreeCpus.ContainsKey(cloud))
					{
						cloudToMinFreeCpus[cloud] = Math.Min(cloudToMinFreeCpus[cloud], cpusFree);
					}
					else
					{
						cloudToMinFreeCpus.Add(cloud, cpusFree);
					}
				}
			}

			Dictionary<Node, int> rackToMinFreeCpus = new Dictionary<Node, int> ();
			foreach (Node cloud in cloudToRacks.Keys)
			{
				int freeCpusToDistribute = cloudToMinFreeCpus[cloud];

				foreach (Node rack in cloudToRacks[cloud])
				{
					Dictionary<string, int> unreadyServerNameToCpus = new Dictionary<string, int> ();
					int rackCpus = GetTotalCpus(GetServerNamesToCpuCountsInLocation(null, rack, startTime, ref unreadyServerNameToCpus));
					int rackFree = Math.Min(rackCpus, freeCpusToDistribute);
					freeCpusToDistribute -= rackFree;
					rackToMinFreeCpus.Add(rack, rackFree);
				}
			}

			foreach (Node rack in model.GetNodesWithAttributeValue("type", "rack"))
			{
				if (! rackToMinFreeCpus.ContainsKey(rack))
				{
					int freeCpus = 0;
					foreach (Node server in rack.GetChildrenOfType("server"))
					{
						int serverFreeCpus = server.GetIntAttribute("cpus", 0);

						foreach (Node serverLinkToVmInstance in server.GetChildrenOfType("server_link_to_vm_instance"))
						{
							serverFreeCpus -= serverLinkToVmInstance.GetIntAttribute("cpus", 0);
						}

						freeCpus += serverFreeCpus;
					}

					rackToMinFreeCpus.Add(rack, freeCpus);
				}
			}

			return rackToMinFreeCpus;
		}

		public int GetMinFreeCpusOverTimeForCloud (Node cloud)
		{
			Dictionary<Node, int> rackToFreeCpus = GetMinFreeCpusOverTimeForRacks();

			int freeCpus = 0;
			foreach (Node rack in rackToFreeCpus.Keys)
			{
				if (GetCloudControllingLocation(rack) == cloud)
				{
					freeCpus += rackToFreeCpus[rack];
				}
			}

			return freeCpus;
		}

		public List<Node> GetDataCentersControllingCloud (Node cloud)
		{
			List<Node> datacenters = new List<Node> ();

			foreach (Node locationReference in cloud.GetChildrenOfType("cloud_location"))
			{
				Node rack = model.GetNamedNode(locationReference.GetAttribute("location"));
				Node datacenter = rack.Parent;
				if (! datacenters.Contains(datacenter))
				{
					datacenters.Add(datacenter);
				}
			}

			return datacenters;
		}
	}
}