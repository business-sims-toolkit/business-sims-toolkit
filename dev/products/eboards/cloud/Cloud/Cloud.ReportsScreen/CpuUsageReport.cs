using System;
using System.Collections.Generic;
using System.Xml;
using System.Drawing;
using System.IO;

using LibCore;
using GameManagement;
using Network;
using Logging;

using Cloud.OpsEngine;

namespace Cloud.ReportsScreen
{
	public class CpuUsageReport : IDisposable
	{
		NetworkProgressionGameFile gameFile;
		NodeTree initialModel;
		NodeTree lastModel;
		int round;
		bool plannerMode;
		double lastKnownTime;

		Dictionary<string, string> serverNameToRackName;
		Dictionary<string, string> rackNameToDatacenterName;
		Dictionary<string, string> vmInstanceNameToBusinessName;
		Dictionary<string, TimeLog<string>> rackNameToTimeToOwner;
		Dictionary<string, BusinessServiceInfo> businessServiceNameToInfo;
		Dictionary<string, TimeLog<Dictionary<string, int>>> serverNameToTimeToVmInstanceNameToCpus;
		Dictionary<string, ServerLinkToVmInstance> serverLinkToVmInstanceNameToInfo;
		Dictionary<string, TimeLog<int>> serverNameToTimeToCpus;

		class ServerLinkToVmInstance
		{
			public string ServerName;
			public string VmInstanceName;

			public ServerLinkToVmInstance (string serverName, string vmInstanceName)
			{
				ServerName = serverName;
				VmInstanceName = vmInstanceName;
			}
		}

		class Server
		{
			public string Name;
			public int Cpus;
			public int TimeTillReady;

			public Server (Node node)
			{
				Name = node.GetAttribute("name");
				Cpus = node.GetIntAttribute("cpus", 0);
				TimeTillReady = node.GetIntAttribute("time_till_ready", 0);
			}
		}

		class BusinessService
		{
			public string Name;

			public bool IsDev;
			public string DevDependencyName;

			public int HandoverCountdown;
			public int StorageUpgradeCountdown;
			public int DevCountdown;

			public BusinessService (Node node)
			{
				Name = node.GetAttribute("name");

				IsDev = node.GetBooleanAttribute("is_dev", false);
				DevDependencyName = node.GetAttribute("requires_dev");

				HandoverCountdown = node.GetIntAttribute("handover_countdown", 0);

				StorageUpgradeCountdown = 0;
				Node storageArray = node.Tree.GetNamedNode(node.GetAttribute("storage_array"));
				if (storageArray != null)
				{
					foreach (Node delay in storageArray.GetChildrenOfType("storage_array_upgrade_delay"))
					{
						if (delay.GetAttribute("business_service") == Name)
						{
							StorageUpgradeCountdown = delay.GetIntAttribute("time_till_ready", 0);
							break;
						}
					}
				}

				DevCountdown = node.GetIntAttribute("dev_countdown", 0);
			}
		}

		class BusinessServiceInfo
		{
			public string Name;
			public string BusinessName;
			public string VmInstanceName;
			public string Owner;
			public double? TimeWentLive;

			public BusinessServiceInfo (string name, string businessName, string owner, string vmInstanceName)
			{
				Name = name;
				BusinessName = businessName;
				Owner = owner;
				VmInstanceName = vmInstanceName;

				TimeWentLive = null;
			}
		}

		public CpuUsageReport (NetworkProgressionGameFile gameFile, int round, bool plannerMode)
		{
			this.gameFile = gameFile;
			this.round = round;
			this.plannerMode = plannerMode;

			string initialModelFilename = gameFile.GetRoundFile(round, "network_at_start.xml", GameFile.GamePhase.OPERATIONS);
			if (! File.Exists(initialModelFilename))
			{
				initialModelFilename = gameFile.GetRoundFile(Math.Max(1, round - 1), "network.xml", GameFile.GamePhase.OPERATIONS);
			}
			initialModel = new NodeTree (File.ReadAllText(initialModelFilename));

			lastModel = gameFile.GetNetworkModel(round);

			lastKnownTime = lastModel.GetNamedNode("CurrentTime").GetDoubleAttribute("seconds", 0);
			if (lastModel.GetNamedNode("RoundVariables").GetIntAttribute("current_round", 0) != round)
			{
				lastKnownTime = 0;
			}

			serverNameToRackName = new Dictionary<string, string> ();
			rackNameToDatacenterName = new Dictionary<string, string> ();
			rackNameToTimeToOwner = new Dictionary<string, TimeLog<string>> ();
			vmInstanceNameToBusinessName = new Dictionary<string, string> ();
			serverNameToTimeToVmInstanceNameToCpus = new Dictionary<string, TimeLog<Dictionary<string, int>>> ();
			serverLinkToVmInstanceNameToInfo = new Dictionary<string, ServerLinkToVmInstance> ();
			serverNameToTimeToCpus = new Dictionary<string, TimeLog<int>> ();
			businessServiceNameToInfo = new Dictionary<string, BusinessServiceInfo> ();

			BasicIncidentLogReader logReader = new BasicIncidentLogReader (gameFile.GetRoundFile(round, "NetworkIncidents.log", GameFile.GamePhase.OPERATIONS));

			foreach (Node datacenter in initialModel.GetNodesWithAttributeValue("type", "datacenter"))
			{
				foreach (Node rack in datacenter.GetChildrenOfType("rack"))
				{
					string rackName = rack.GetAttribute("name");

					rackNameToDatacenterName.Add(rackName, datacenter.GetAttribute("name"));
					rackNameToTimeToOwner.Add(rackName, new TimeLog<string> ());
					rackNameToTimeToOwner[rackName].Add(0, rack.GetAttribute("owner"));

					logReader.WatchApplyAttributes(rackName, new LogLineFoundDef.LineFoundHandler (logReader_RackApplyAttributes));
					logReader.WatchCreatedNodes(rackName, new LogLineFoundDef.LineFoundHandler (logReader_ServerCreatedNodes));

					foreach (Node server in rack.GetChildrenOfType("server"))
					{
						string serverName = server.GetAttribute("name");

						AddServer(logReader, serverName, rackName, 0, server.GetIntAttribute("cpus", 0));

						foreach (Node serverLinkToVmInstance in server.GetChildrenOfType("server_link_to_vm_instance"))
						{
							string vmInstanceName = serverLinkToVmInstance.GetAttribute("vm_instance");
							int cpus = serverLinkToVmInstance.GetIntAttribute("cpus", 0);

							LogServerLinkToVmInstance(logReader, serverLinkToVmInstance.GetAttribute("name"), 0, serverName, vmInstanceName, cpus);
						}
					}
				}
			}

			foreach (Node vmInstance in initialModel.GetNodesWithAttributeValue("type", "vm_instance"))
			{
				vmInstanceNameToBusinessName.Add(vmInstance.GetAttribute("name"), vmInstance.GetAttribute("business"));
			}

			foreach (Node business in initialModel.GetNodesWithAttributeValue("type", "business"))
			{
				foreach (Node businessService in business.GetChildrenOfType("business_service"))
				{
					string businessServiceName = businessService.GetAttribute("name");
					businessServiceNameToInfo.Add(businessServiceName, new BusinessServiceInfo (businessServiceName,
																								business.GetAttribute("name"),
																								businessService.GetAttribute("owner"),
																								businessService.GetAttribute("vm_instance")));
					if (businessService.GetAttribute("status") == "up")
					{
						businessServiceNameToInfo[businessServiceName].TimeWentLive = 0;
					}

					logReader.WatchApplyAttributes(businessServiceName, new LogLineFoundDef.LineFoundHandler (logReader_BusinessServiceApplyAttributes));
				}

				logReader.WatchCreatedNodes(business.GetAttribute("name"), new LogLineFoundDef.LineFoundHandler (logReader_BusinessServiceCreatedNodes));
			}

			logReader.WatchCreatedNodes("VmInstances", new LogLineFoundDef.LineFoundHandler (logReader_VmInstanceCreatedNodes));

			logReader.Run();
			logReader.Dispose();

			SimulateFuture();
		}

		void SimulateFuture ()
		{
			BauManager bauManager = new BauManager (lastModel);
			VirtualMachineManager vmManager = new VirtualMachineManager (lastModel, bauManager);
			OrderPlanner orderPlanner = new OrderPlanner (lastModel, bauManager, vmManager);

			int roundEnd = lastModel.GetNamedNode("CurrentTime").GetIntAttribute("round_duration", 0);
			int nextMinute = 1 + (int) (lastKnownTime / 60);

			// Build a list of server statuses.
			Dictionary<string, Server> serverNameToState = new Dictionary<string, Server> ();
			foreach (Node server in lastModel.GetNodesWithAttributeValue("type", "server"))
			{
				serverNameToState.Add(server.GetAttribute("name"), new Server (server));
			}

			// And of business services.
			Dictionary<string, BusinessService> businessServiceNameToState = new Dictionary<string, BusinessService> ();
			foreach (Node service in lastModel.GetNodesWithAttributeValue("type", "business_service"))
			{
				businessServiceNameToState.Add(service.GetAttribute("name"), new BusinessService (service));
			}

			double updatedTo = lastKnownTime;
			for (int minute = nextMinute; minute <= Math.Ceiling((double) roundEnd / 60); minute++)
			{
				double time = minute * 60;
				double nextMinuteStart = (minute + 1) * 60;
				double dt = time - updatedTo;

				// Update server delays.
				foreach (Server server in serverNameToState.Values)
				{
					server.TimeTillReady = Math.Max(0, (int) (server.TimeTillReady - dt));
				}

				// Build a list of current server allocations.
				Dictionary<string, Dictionary<Node, int>> serverNameToVmInstanceToCpus = new Dictionary<string, Dictionary<Node, int>> ();
				foreach (string serverName in serverNameToTimeToVmInstanceNameToCpus.Keys)
				{
					Dictionary<string, int> vmInstanceNameToCpus = serverNameToTimeToVmInstanceNameToCpus[serverName].GetLastValueBefore(nextMinuteStart);

					if (vmInstanceNameToCpus != null)
					{
						foreach (string vmInstanceName in vmInstanceNameToCpus.Keys)
						{
							int cpus = vmInstanceNameToCpus[vmInstanceName];

							if (! serverNameToVmInstanceToCpus.ContainsKey(serverName))
							{
								serverNameToVmInstanceToCpus.Add(serverName, new Dictionary<Node, int> ());
							}

							serverNameToVmInstanceToCpus[serverName].Add(lastModel.GetNamedNode(vmInstanceName), cpus);
						}
					}
				}

				// Now update the allocations according to the VMM.
				foreach (Node cloud in lastModel.GetNodesWithAttributeValue("type", "cpu_cloud"))
				{
					Dictionary<Node, Dictionary<string, int>> cloudBusinessServiceToServerNameToCpus = vmManager.GetCpuAssignments(null, cloud, serverNameToVmInstanceToCpus, (int) time);
					foreach (Node businessService in cloudBusinessServiceToServerNameToCpus.Keys)
					{
						string vmInstanceName = businessService.GetAttribute("vm_instance");

						// Find all the servers that this service is currently using.
						List<string> serverNamesOccupiedByBusinessService = new List<string> ();
						foreach (string serverName in serverNameToVmInstanceToCpus.Keys)
						{
							foreach (Node vmInstance in serverNameToVmInstanceToCpus[serverName].Keys)
							{
								if (vmInstance.GetAttribute("name") == vmInstanceName)
								{
									serverNamesOccupiedByBusinessService.Add(serverName);
								}
							}
						}

						// For all servers we occupy or should occupy, change the occupation count.
						foreach (string serverName in CollectionUtils.Union(cloudBusinessServiceToServerNameToCpus[businessService].Keys,
																			serverNamesOccupiedByBusinessService))
						{
							int cpusWanted = 0;
							if (cloudBusinessServiceToServerNameToCpus[businessService].ContainsKey(serverName))
							{
								cpusWanted = cloudBusinessServiceToServerNameToCpus[businessService][serverName];
							}

							if (! serverNameToTimeToVmInstanceNameToCpus.ContainsKey(serverName))
							{
								serverNameToTimeToVmInstanceNameToCpus.Add(serverName, new TimeLog<Dictionary<string, int>> ());
							}

							if (! serverNameToTimeToVmInstanceNameToCpus[serverName].ContainsTime(time))
							{
								serverNameToTimeToVmInstanceNameToCpus[serverName].Add(time, new Dictionary<string, int> ());
							}

							if (serverNameToTimeToVmInstanceNameToCpus[serverName][time].ContainsKey(vmInstanceName))
							{
								serverNameToTimeToVmInstanceNameToCpus[serverName][time][vmInstanceName] += cpusWanted;
							}
							else
							{
								serverNameToTimeToVmInstanceNameToCpus[serverName][time].Add(vmInstanceName, cpusWanted);
							}
						}
					}
				}

				// Update business services.
				foreach (BusinessService businessService in businessServiceNameToState.Values)
				{
					bool up = true;
					bool waitingOnHardware = false;

					// Advance the storage delay timer.
					if (businessService.StorageUpgradeCountdown > 0)
					{
						up = false;
						waitingOnHardware = true;
						businessService.StorageUpgradeCountdown = Math.Max(0, (int) (businessService.StorageUpgradeCountdown - dt));
					}

					bool waitingOnDev = false;

					// We might be waiting on dev.
					if (! string.IsNullOrEmpty(businessService.DevDependencyName))
					{
						BusinessService devService = businessServiceNameToState[businessService.DevDependencyName];

						if (devService.DevCountdown > 0)
						{
							up = false;
							waitingOnDev = true;
						}
					}

					if (! (waitingOnDev || waitingOnHardware))
					{
						businessService.HandoverCountdown = Math.Max(0, (int) (businessService.HandoverCountdown - dt));

						if (businessService.HandoverCountdown > 0)
						{
							up = false;
						}
					}

					// Dev work can only proceed once everything else is ready.
					if (up)
					{
						if (businessService.IsDev)
						{
							if (businessService.DevCountdown > 0)
							{
								businessService.DevCountdown = Math.Max(0, (int) (businessService.DevCountdown - dt));
							}
							else
							{
								up = false;
							}
						}
					}
				}

				updatedTo = time;
			}
		}

		void AddServer (BasicIncidentLogReader logReader, string serverName, string rackName, double time, int cpus)
		{
			if (! serverNameToTimeToVmInstanceNameToCpus.ContainsKey(serverName))
			{
				serverNameToTimeToVmInstanceNameToCpus.Add(serverName, new TimeLog<Dictionary<string, int>>());
			}

			serverNameToRackName[serverName] = rackName;

			if (! serverNameToTimeToCpus.ContainsKey(serverName))
			{
				serverNameToTimeToCpus.Add(serverName, new TimeLog<int>());
			}

			serverNameToTimeToCpus[serverName][time] = cpus;

			logReader.WatchCreatedNodes(serverName, new LogLineFoundDef.LineFoundHandler (logReader_ServerLinkToVmInstanceCreatedNodes));
			logReader.WatchDeletedNodes(serverName, new LogLineFoundDef.LineFoundHandler (logReader_ServerDeletedNodes));
			logReader.WatchApplyAttributes(serverName, new LogLineFoundDef.LineFoundHandler (logReader_ServerApplyAttributes));
		}

		void LogServerLinkToVmInstance (BasicIncidentLogReader logReader, string linkName, double time, string serverName, string vmInstanceName, int cpus)
		{
			double nextMinuteStart = (time + 60) - ((time + 60) % 60);

			if (! serverNameToTimeToVmInstanceNameToCpus.ContainsKey(serverName))
			{
				serverNameToTimeToVmInstanceNameToCpus.Add(serverName, new TimeLog<Dictionary<string, int>> ());
			}

			if (serverNameToTimeToVmInstanceNameToCpus[serverName].IsEmpty)
			{
				serverNameToTimeToVmInstanceNameToCpus[serverName][time] = new Dictionary<string, int> ();
			}
			else
			{
				serverNameToTimeToVmInstanceNameToCpus[serverName][time] = new Dictionary<string, int>(serverNameToTimeToVmInstanceNameToCpus[serverName].GetLastValueBefore(nextMinuteStart));
			}

			if (cpus > 0)
			{
				serverNameToTimeToVmInstanceNameToCpus[serverName][time][vmInstanceName] = cpus;
			}
			else
			{
				if (serverNameToTimeToVmInstanceNameToCpus[serverName][time].ContainsKey(vmInstanceName))
				{
					serverNameToTimeToVmInstanceNameToCpus[serverName][time].Remove(vmInstanceName);
				}
			}

			if ((! string.IsNullOrEmpty(linkName))
				&& (! serverLinkToVmInstanceNameToInfo.ContainsKey(linkName)))
			{
				serverLinkToVmInstanceNameToInfo.Add(linkName, new ServerLinkToVmInstance (serverName, vmInstanceName));
			}

			logReader.WatchApplyAttributes(linkName, new LogLineFoundDef.LineFoundHandler (logReader_ServerLinkToVmInstanceApplyAttributes));
			logReader.WatchDeletedNodes(linkName, new LogLineFoundDef.LineFoundHandler (logReader_ServerLinkToVmInstanceDeletedNodes));
		}

		void logReader_VmInstanceCreatedNodes (object sender, string key, string line, double time)
		{
			BasicIncidentLogReader logReader = (BasicIncidentLogReader)sender;
			string vmInstanceName = BasicXmlDocument.GetStringAttribute(logReader.currentCommand.ChildNodes[0], "name");
			string businessName = BasicXmlDocument.GetStringAttribute(logReader.currentCommand.ChildNodes[0], "business");

			vmInstanceNameToBusinessName.Add(vmInstanceName, businessName);
		}

		void logReader_RackApplyAttributes (object sender, string key, string line, double time)
		{
			BasicIncidentLogReader logReader = (BasicIncidentLogReader)sender;
			string rackName = BasicXmlDocument.GetStringAttribute(logReader.currentCommand, "i_name");
			string owner = BasicXmlDocument.GetStringAttribute(logReader.currentCommand, "owner");

			if (! string.IsNullOrEmpty(owner))
			{
				rackNameToTimeToOwner[rackName].Add(time, owner);
			}
		}

		void logReader_ServerCreatedNodes (object sender, string key, string line, double time)
		{
			BasicIncidentLogReader logReader = (BasicIncidentLogReader)sender;
			string rackName = BasicXmlDocument.GetStringAttribute(logReader.currentCommand, "i_to");
			string serverName = BasicXmlDocument.GetStringAttribute(logReader.currentCommand.ChildNodes[0], "name");
			int cpus = CONVERT.ParseIntSafe(BasicXmlDocument.GetStringAttribute(logReader.currentCommand.ChildNodes[0], "cpus"), 0);

			AddServer(logReader, serverName, rackName, time, cpus);
		}

		void logReader_ServerDeletedNodes (object sender, string key, string line, double time)
		{
			BasicIncidentLogReader logReader = (BasicIncidentLogReader)sender;
			string serverName = BasicXmlDocument.GetStringAttribute(logReader.currentCommand, "i_name");
			serverNameToTimeToCpus[serverName][time] = 0;
		}

		void logReader_ServerLinkToVmInstanceApplyAttributes (object sender, string key, string line, double time)
		{
			BasicIncidentLogReader logReader = (BasicIncidentLogReader)sender;
			string linkName = BasicXmlDocument.GetStringAttribute(logReader.currentCommand, "i_name");

			ServerLinkToVmInstance linkInfo = serverLinkToVmInstanceNameToInfo[linkName];
			int cpus = CONVERT.ParseIntSafe(BasicXmlDocument.GetStringAttribute(logReader.currentCommand, "cpus"), 0);

			LogServerLinkToVmInstance(logReader, linkName, time, linkInfo.ServerName, linkInfo.VmInstanceName, cpus);
		}

		void logReader_ServerLinkToVmInstanceCreatedNodes (object sender, string key, string line, double time)
		{
			BasicIncidentLogReader logReader = (BasicIncidentLogReader)sender;
			string linkName = BasicXmlDocument.GetStringAttribute(logReader.currentCommand.ChildNodes[0], "name");
			string serverName = BasicXmlDocument.GetStringAttribute(logReader.currentCommand, "i_to");
			string vmInstanceName = BasicXmlDocument.GetStringAttribute(logReader.currentCommand.ChildNodes[0], "vm_instance");
			int cpus = CONVERT.ParseIntSafe(BasicXmlDocument.GetStringAttribute(logReader.currentCommand.ChildNodes[0], "cpus"), 0);

			LogServerLinkToVmInstance(logReader, linkName, time, serverName, vmInstanceName, cpus);
		}

		void logReader_ServerLinkToVmInstanceDeletedNodes (object sender, string key, string line, double time)
		{
			BasicIncidentLogReader logReader = (BasicIncidentLogReader) sender;
			string linkName = BasicXmlDocument.GetStringAttribute(logReader.currentCommand, "i_name");
			ServerLinkToVmInstance linkInfo = serverLinkToVmInstanceNameToInfo[linkName];

			LogServerLinkToVmInstance(logReader, linkName, time, linkInfo.ServerName, linkInfo.VmInstanceName, 0);
		}

		void logReader_ServerApplyAttributes (object sender, string key, string line, double time)
		{
			BasicIncidentLogReader logReader = (BasicIncidentLogReader) sender;
			string serverName = BasicXmlDocument.GetStringAttribute(logReader.currentCommand, "i_name");
			string cpuString = BasicXmlDocument.GetStringAttribute(logReader.currentCommand, "cpus");

			if (!string.IsNullOrEmpty(cpuString))
			{
				int cpus = CONVERT.ParseInt(cpuString);
				serverNameToTimeToCpus[serverName][time] = cpus;
			}
		}

		void logReader_BusinessServiceApplyAttributes (object sender, string key, string line, double time)
		{
			BasicIncidentLogReader logReader = (BasicIncidentLogReader) sender;
			string businessServiceName = BasicXmlDocument.GetStringAttribute(logReader.currentCommand, "i_name");
			string status = BasicXmlDocument.GetStringAttribute(logReader.currentCommand, "status");

			if (status == "up")
			{
				businessServiceNameToInfo[businessServiceName].TimeWentLive = time;
			}
		}

		void logReader_BusinessServiceCreatedNodes(object sender, string key, string line, double time)
		{
			BasicIncidentLogReader logReader = (BasicIncidentLogReader)sender;
			string businessServiceName = BasicXmlDocument.GetStringAttribute(logReader.currentCommand.ChildNodes[0], "name");
			string owner = BasicXmlDocument.GetStringAttribute(logReader.currentCommand.ChildNodes[0], "owner");
			string business = BasicXmlDocument.GetStringAttribute(logReader.currentCommand.ChildNodes[0], "business");
			string vmInstanceName = BasicXmlDocument.GetStringAttribute(logReader.currentCommand.ChildNodes[0], "vm_instance");
			string status = BasicXmlDocument.GetStringAttribute(logReader.currentCommand.ChildNodes[0], "status");

			businessServiceNameToInfo[businessServiceName] = new BusinessServiceInfo (businessServiceName, business, owner, vmInstanceName);

			if (status == "up")
			{
				businessServiceNameToInfo[businessServiceName].TimeWentLive = time;
			}

			logReader.WatchApplyAttributes(businessServiceName, new LogLineFoundDef.LineFoundHandler (logReader_BusinessServiceApplyAttributes));
		}

		public double CalculateUtilisation (IList<string> datacenters,
											IList<string> owners)
		{
			double totalBusinessDemandCpuPeriods;
			double totalCapacityCpuPeriods;
			double internalCpuProvisionFraction;
			double externalCpuProvisionFraction;

			return CalculateUtilisation(datacenters, owners,
			                            out totalBusinessDemandCpuPeriods,
										out totalCapacityCpuPeriods,
										out internalCpuProvisionFraction,
										out externalCpuProvisionFraction);
		}

		public double CalculateUtilisation (IList<string> datacenters,
		                                    IList<string> owners,
		                                    out double totalBusinessDemandCpuPeriods,
			                                out double totalCapacityCpuPeriods,
			                                out double internalCpuProvisionFraction,
			                                out double externalCpuProvisionFraction)
		{
			totalBusinessDemandCpuPeriods = 0;
			totalCapacityCpuPeriods = 0;
			double internalCpuProvisionPeriods = 0;

			for (double time = 0; time < lastKnownTime; time += 60)
			{
				double nextMinuteStart = time + 60;

				foreach (string serverName in serverNameToTimeToVmInstanceNameToCpus.Keys)
				{
					string rackName = serverNameToRackName[serverName];
					string datacenterName = rackNameToDatacenterName[rackName];
					bool isCloudServer = (datacenterName == "Cloud Datacenter");

					if ((datacenters == null)
						|| datacenters.Contains(datacenterName)
						|| isCloudServer)
					{
						if ((! isCloudServer)
							&& ((owners == null)
							    || owners.Contains(rackNameToTimeToOwner[rackName].GetLastValueBefore(nextMinuteStart))))
						{
							totalCapacityCpuPeriods += serverNameToTimeToCpus[serverName].GetLastValueBefore(nextMinuteStart);
						}

						Dictionary<string, int> vmInstanceNameToCpus = serverNameToTimeToVmInstanceNameToCpus[serverName].GetLastValueBefore(nextMinuteStart);
						if (vmInstanceNameToCpus != null)
						{
							foreach (string vmInstanceName in vmInstanceNameToCpus.Keys)
							{
								if (isCloudServer)
								{
									// Don't include the figures for these CPUs if they're doing work for another datacenter!
									string businessName = vmInstanceNameToBusinessName[vmInstanceName];

									bool isCloudWorkForUs = false;
									if (datacenters == null)
									{
										isCloudWorkForUs = true;
									}
									else
									{
										foreach (string tryDatacenterName in datacenters)
										{
											Node datacenterNode = lastModel.GetNamedNode(tryDatacenterName);
											if (datacenterNode.GetAttribute("business") == businessName)
											{
												isCloudWorkForUs = true;
												break;
											}
										}
									}

									if (! isCloudWorkForUs)
									{
										continue;
									}
								}

								int cpus = vmInstanceNameToCpus[vmInstanceName];
								string businessServiceName = GetBusinessServiceNameByVmInstanceName(vmInstanceName);
								string owner = businessServiceNameToInfo[businessServiceName].Owner;

								// Don't count dev BAU!
								if (vmInstanceName.Contains("Preexisting")
									&& (owner == "dev&test"))
								{
									cpus = 0;
								}

								if ((owners == null)
									|| owners.Contains(owner))
								{
									totalBusinessDemandCpuPeriods += cpus;

									if (! rackName.StartsWith("Public"))
									{
										internalCpuProvisionPeriods += cpus;
									}
								}
							}
						}
					}
				}
			}

			internalCpuProvisionFraction = internalCpuProvisionPeriods / Math.Max(1, totalBusinessDemandCpuPeriods);
			externalCpuProvisionFraction = 1 - internalCpuProvisionFraction;

			return internalCpuProvisionPeriods / Math.Max(totalCapacityCpuPeriods, 1);
		}

		public string BuildReport (IList<string> datacenters, IList<string> owners)
		{
			// Build the XML structure.
			BasicXmlDocument xml = BasicXmlDocument.Create();

			XmlElement root = xml.AppendNewChild("stacked_bar_chart");
			root.SetAttribute("show_key", "true");
			root.SetAttribute("legend", "");
			root.SetAttribute("use_gradient", "true");

			int roundEnd = lastModel.GetNamedNode("CurrentTime").GetIntAttribute("round_duration", 0);
			int lastTime = lastModel.GetNamedNode("CurrentTime").GetIntAttribute("seconds", 0);

			XmlElement xAxis = xml.AppendNewChild(root, "x_axis");
			xAxis.SetAttribute("min", "1");
			xAxis.SetAttribute("max", CONVERT.ToStr(1 + Math.Max(1, (int) Math.Ceiling(roundEnd / 60.0) - 1)));
			xAxis.SetAttribute("visible", "true");
			xAxis.SetAttribute("legend", "Trading period");
			xAxis.SetAttribute("tick_colour", "224,224,224");

			XmlElement yAxis = xml.AppendNewChild(root, "y_axis");
			yAxis.SetAttribute("visible", "true");
			yAxis.SetAttribute("legend", "CPUs");
			yAxis.SetAttribute("tick_colour", "164,164,164");

			XmlElement categories = xml.AppendNewChild(root, "bar_categories");

			bool showTotalLine = true;
			if (datacenters.Count == 1)
			{
				Node datacenter = initialModel.GetNamedNode(datacenters[0]);
				if ((datacenter != null)
				   && datacenter.GetBooleanAttribute("is_cloud", false))
				{
					showTotalLine = false;
				}
			}

			XmlElement totalCategory = null;
			if (showTotalLine)
			{
				totalCategory = xml.AppendNewChild(categories, "category");
				totalCategory.SetAttribute("name", "total");
				totalCategory.SetAttribute("legend", "Total");
				totalCategory.SetAttribute("colour", CONVERT.ToComponentStr(Color.White));
			}

			Dictionary<string, Color> categoryToColour = new Dictionary<string, Color>();
			categoryToColour.Add("dev&test", Color.FromArgb(200, 200, 0));
			categoryToColour.Add("traditional", Color.FromArgb(100, 75, 0));

			categoryToColour.Add("bau-floor", Color.FromArgb(0, 200, 200));
			categoryToColour.Add("bau-online", Color.FromArgb(255, 150, 150));
			categoryToColour.Add("foreign-floor", Color.FromArgb(150, 200, 200));
			categoryToColour.Add("foreign-online", Color.FromArgb(200, 150, 150));
			categoryToColour.Add("new-service-floor", Color.FromArgb(0, 0, 255));
			categoryToColour.Add("new-service-online", Color.FromArgb(255, 150, 0));

			categoryToColour.Add("new-demand", Color.FromArgb(0, 200, 0));
			categoryToColour.Add("unmet-demand", Color.FromArgb(255, 0, 0));

			Dictionary<string, Color> categoryToTextColour = new Dictionary<string, Color>();
			categoryToTextColour.Add("dev&test", Color.FromArgb(0, 0, 0));
			categoryToTextColour.Add("traditional", Color.FromArgb(0, 0, 0));
			categoryToTextColour.Add("bau-floor", Color.FromArgb(0, 0, 0));
			categoryToTextColour.Add("bau-online", Color.FromArgb(0, 0, 0));
			categoryToTextColour.Add("foreign-floor", Color.FromArgb(0, 0, 0));
			categoryToTextColour.Add("foreign-online", Color.FromArgb(0, 0, 0));
			categoryToTextColour.Add("new-service-floor", Color.FromArgb(255, 255, 255));
			categoryToTextColour.Add("new-service-online", Color.FromArgb(0, 0, 0));
			categoryToTextColour.Add("new-demand", Color.FromArgb(0, 0, 0));
			categoryToTextColour.Add("unmet-demand", Color.FromArgb(0, 0, 0));

			Dictionary<string, string> categoryToDescription = new Dictionary<string, string>();
			categoryToDescription.Add("dev&test", "Dev & Test");
			categoryToDescription.Add("traditional", "Legacy");
			categoryToDescription.Add("bau-floor", "BAU (Floor)");
			categoryToDescription.Add("bau-online", "BAU (Online)");
			categoryToDescription.Add("foreign-floor", "Foreign Services (Floor)");
			categoryToDescription.Add("foreign-online", "Foreign Services (Online)");
			categoryToDescription.Add("new-service-floor", "New Services (Floor)");
			categoryToDescription.Add("new-service-online", "New Services (Online)");
			categoryToDescription.Add("new-demand", "New Demands");
			categoryToDescription.Add("unmet-demand", "Unmet Demands");

			List<string> orderedCategoryNames = new List<string>();
			orderedCategoryNames.Add("traditional");
			orderedCategoryNames.Add("bau-floor");
			orderedCategoryNames.Add("bau-online");
			orderedCategoryNames.Add("dev&test");
			orderedCategoryNames.Add("foreign-floor");
			orderedCategoryNames.Add("foreign-online");
			orderedCategoryNames.Add("new-service-floor");
			orderedCategoryNames.Add("new-service-online");
			orderedCategoryNames.Add("new-demand");
			orderedCategoryNames.Add("unmet-demand");

			Dictionary<string, XmlElement> categoryNameToNode = new Dictionary<string, XmlElement>();
			foreach (string categoryName in categoryToColour.Keys)
			{
				XmlElement category = xml.AppendNewChild(categories, "category");
				category.SetAttribute("name", categoryName);
				category.SetAttribute("legend", Strings.SentenceCase(categoryToDescription[categoryName]));
				category.SetAttribute("colour", CONVERT.ToComponentStr(categoryToColour[categoryName]));
				category.SetAttribute("text_colour", CONVERT.ToComponentStr(categoryToTextColour[categoryName]));

				bool showInKey = true;

				if (plannerMode)
				{
					showInKey = ((categoryName != "traditional") && (categoryName != "unmet-demand"));
				}

				category.SetAttribute("show_in_key", CONVERT.ToStr(showInKey));
				categoryNameToNode.Add(categoryName, category);
			}

			XmlElement stacks = xml.AppendNewChild(root, "stacks");

			// Work out how many CPUs we would have needed to dedicate to unhandled demands.
			OrderPlanner orderPlanner = new OrderPlanner(lastModel, null, null);
			Dictionary<string, TimeLog<int>> businessNameToTimeToCpusNeededForUnhandledDemand = new Dictionary<string, TimeLog<int>>();
			foreach (Node business in lastModel.GetNodesWithAttributeValue("type", "business"))
			{
				string businessName = business.GetAttribute("name");
				businessNameToTimeToCpusNeededForUnhandledDemand.Add(businessName, new TimeLog<int>());

				foreach (Node demand in lastModel.GetNodesWithAttributeValue("type", "demand"))
				{
					if (demand.GetAttribute("business") == businessName)
					{
						string attributeName = CONVERT.Format("round_{0}_instances", round);
						int instances = demand.GetIntAttribute(attributeName, 0);

						bool serviceExists = businessServiceNameToInfo.ContainsKey(demand.GetAttribute("business_service"));
						double? timeWentLive = null;

						if (serviceExists)
						{
							timeWentLive = businessServiceNameToInfo[demand.GetAttribute("business_service")].TimeWentLive;
						}

						bool wentUpInTime = (serviceExists
											&& timeWentLive.HasValue
											&& timeWentLive.Value <= (demand.GetIntAttribute("delay", 0) + 1));

						bool wasActive = demand.GetBooleanAttribute("active", false);

						if (wasActive && !wentUpInTime)
						{
							double start = demand.GetIntAttribute("delay", 0);
							double end = start + demand.GetIntAttribute("duration", 0);

							string demandNewServiceDefinitionName = demand.GetAttribute("business_service") + " Definition";
							Node businessServiceDefinition = lastModel.GetNamedNode(demandNewServiceDefinitionName);

							if ((owners == null) || owners.Contains(businessServiceDefinition.GetAttribute("owner")))
							{
								Node vm = orderPlanner.GetAVmSuitableForBusinessServiceOrDefinition(businessServiceDefinition);
								int cpus = vm.GetIntAttribute("cpus", 0) * instances;

								int priorValue = businessNameToTimeToCpusNeededForUnhandledDemand[businessName].GetLastValueBefore(start + 60);

								businessNameToTimeToCpusNeededForUnhandledDemand[businessName].Add(start, priorValue + cpus);
								businessNameToTimeToCpusNeededForUnhandledDemand[businessName].Add(end, priorValue);
							}
						}
					}
				}
			}

			// Emit the chart.
			int yMax = 0;
			for (int minute = 0; minute <= Math.Ceiling(roundEnd / 60.0); minute++)
			{
				double time = minute * 60;
				double nextMinuteStart = (minute + 1) * 60;

				XmlElement stack = xml.AppendNewChild(stacks, "stack");
				stack.SetAttribute("x", CONVERT.ToStr(1 + minute));

				Dictionary<string, int> categoryNameToCpus = new Dictionary<string, int>();

				// Work out whether and how to count each server.
				int totalCpus = 0;
				foreach (string serverName in serverNameToTimeToVmInstanceNameToCpus.Keys)
				{
					string rackName = serverNameToRackName[serverName];

					string datacenterName = rackNameToDatacenterName[rackName];
					if ((datacenters == null)
						|| datacenters.Contains(datacenterName))
					{
						Dictionary<string, int> vmInstanceNameToCpus = serverNameToTimeToVmInstanceNameToCpus[serverName].GetLastValueBefore(nextMinuteStart);
						if (vmInstanceNameToCpus != null)
						{
							foreach (string vmInstanceName in vmInstanceNameToCpus.Keys)
							{
								string businessServiceName = GetBusinessServiceNameByVmInstanceName(vmInstanceName);
								string vmInstanceOwner = businessServiceNameToInfo[businessServiceName].Owner;

								// Are we a traditional service...
								if (vmInstanceOwner == "traditional")
								{
									// ...offloaded to a non-traditional rack?
									if (rackNameToTimeToOwner[rackName].GetLastValueBefore(nextMinuteStart) != "traditional")
									{
										// HACK TODO
										// If so, we should count under our true owner!

										if (businessServiceName.EndsWith("Financial"))
										{
											vmInstanceOwner = "online";
										}
										else
										{
											vmInstanceOwner = "floor";
										}
									}
								}

								int cpus = vmInstanceNameToCpus[vmInstanceName];
								string categoryName = GetCpuUsageCategoryByVmInstanceName(datacenters, vmInstanceOwner, vmInstanceName);

								if ((owners == null)
									|| owners.Contains(vmInstanceOwner))
								{
									if (categoryNameToCpus.ContainsKey(categoryName))
									{
										categoryNameToCpus[categoryName] += cpus;
									}
									else
									{
										categoryNameToCpus.Add(categoryName, cpus);
									}
								}
							}
						}

						bool isCloudRack = rackName.StartsWith("Public");
						if ((owners == null)
							|| owners.Contains(rackNameToTimeToOwner[rackName].GetLastValueBefore(nextMinuteStart))
							|| (isCloudRack
								&& (owners.Contains("online") || owners.Contains("floor"))))
						{
							totalCpus += serverNameToTimeToCpus[serverName].GetLastValueBefore(nextMinuteStart);
						}
					}
				}

				// Add in the unhandled demands.
				if (!plannerMode)
				{
					foreach (Node business in initialModel.GetNodesWithAttributeValue("type", "business"))
					{
						string datacenterName = business.GetAttribute("datacenter");

						if ((datacenters == null)
							|| datacenters.Contains(datacenterName))
						{
							int cpusNeeded = businessNameToTimeToCpusNeededForUnhandledDemand[business.GetAttribute("name")].GetLastValueBefore(nextMinuteStart);

							string unmetDemandCategoryName = "unmet-demand";
							if (categoryNameToCpus.ContainsKey(unmetDemandCategoryName))
							{
								categoryNameToCpus[unmetDemandCategoryName] += cpusNeeded;
							}
							else
							{
								categoryNameToCpus.Add(unmetDemandCategoryName, cpusNeeded);
							}
						}
					}
				}

				int stackHeight = 0;
				foreach (string categoryName in orderedCategoryNames)
				{
					XmlElement usedPoint = xml.AppendNewChild(stack, "bar");
					usedPoint.SetAttribute("category", categoryNameToNode[categoryName].GetAttribute("name"));

					int usedCpusInThisCategory = 0;
					if (categoryNameToCpus.ContainsKey(categoryName))
					{
						usedCpusInThisCategory = categoryNameToCpus[categoryName];
					}
					usedPoint.SetAttribute("height", CONVERT.ToStr(usedCpusInThisCategory));
					usedPoint.SetAttribute("legend", CONVERT.ToStr(usedCpusInThisCategory));

					if (categoryName != "unmet-demand")
					{
						stackHeight += usedCpusInThisCategory;
					}
				}
				int freeCpus = totalCpus - stackHeight;
				stack.SetAttribute("top_label", CONVERT.Format("{0}\n({1})", stackHeight, freeCpus));

				if (totalCategory != null)
				{
					XmlElement totalPoint = xml.AppendNewChild(stack, "line");
					totalPoint.SetAttribute("category", "total");
					totalPoint.SetAttribute("y", CONVERT.ToStr(totalCpus));
					totalPoint.SetAttribute("show_above_line", "true");
				}

				yMax = Math.Max(yMax, totalCpus);

				XmlElement freeAnnotation = xml.AppendNewChild(root, "label");
				BasicXmlDocument.AppendAttribute(freeAnnotation, "x", 1 + minute);
				BasicXmlDocument.AppendAttribute(freeAnnotation, "text", freeCpus);
			}

			yMax = Math.Max(1, (int) Charts.LineGraph.RoundToNiceInterval(yMax));
			yAxis.SetAttribute("min", "0");
			yAxis.SetAttribute("max", CONVERT.ToStr(yMax));

			int numberInterval = Math.Max(1, yMax / 10);
			int tickInterval = numberInterval;
			if ((numberInterval % 2) == 0)
			{
				tickInterval = numberInterval / 2;
			}

			yAxis.SetAttribute("tick_interval", CONVERT.ToStr(tickInterval));
			yAxis.SetAttribute("number_interval", CONVERT.ToStr(numberInterval));

			if ((lastTime > 0) && (lastTime < roundEnd))
			{
				XmlElement timeDivider = xml.AppendNewChild(root, "divider");
				BasicXmlDocument.AppendAttribute(timeDivider, "x", 1 + (int) (lastTime / 60));
			}

			string xmlFilename = gameFile.GetRoundFile(round, "CpuUsageReport.xml", GameFile.GamePhase.OPERATIONS);
			xml.Save(xmlFilename);

			return xmlFilename;
		}

		string GetBusinessServiceNameByVmInstanceName (string vmInstanceName)
		{
			foreach (string businessServiceName in businessServiceNameToInfo.Keys)
			{
				BusinessServiceInfo businessServiceInfo = businessServiceNameToInfo[businessServiceName];
				if (businessServiceInfo.VmInstanceName == vmInstanceName)
				{
					return businessServiceName;
				}
			}

			return null;
		}

		string GetCpuUsageCategoryByVmInstanceName (IList<string> datacenters, string owner, string vmInstanceName)
		{
			// Is the CPU in use by a foreign business?  Ignore dev though -- we treat that as always native.
			Node business = lastModel.GetNamedNode(vmInstanceNameToBusinessName[vmInstanceName]);
			if ((datacenters != null)
				&& (! datacenters.Contains(business.GetAttribute("datacenter")))
				&& (owner != "dev&test"))
			{
				return "foreign-" + owner;
			}
			else
			{
				if (owner == "traditional")
				{
					return "traditional";
				}
				else
				{
					Node vmInstance = lastModel.GetNamedNode(vmInstanceName);
					Node businessService = lastModel.GetNamedNode(vmInstance.GetAttribute("business_service"));

					if ((businessService != null)
						&& businessService.GetBooleanAttribute("is_preexisting", false))
					{
						return "bau-" + owner;
					}
					else if ((businessService != null)
						&& (! string.IsNullOrEmpty(businessService.GetAttribute("demand_name"))))
					{
						return "new-demand";
					}
					else
					{
						if ((businessService != null)
							&& (businessService.GetIntAttribute("created_in_round", 0) == round))
						{
							if (owner != "dev&test")
							{
								return "new-service-" + owner;
							}
							else
							{
								return owner;
							}
						}
						else
						{
							if (owner != "dev&test")
							{
								return "bau-" + owner;
							}
							else
							{
								return owner;
							}
						}
					}
				}
			}
		}

		public void Dispose ()
		{
			gameFile = null;
			initialModel = null;
			lastModel = null;

			serverNameToRackName = null;
			rackNameToDatacenterName = null;
			vmInstanceNameToBusinessName = null;
			rackNameToTimeToOwner = null;
			businessServiceNameToInfo = null;
			serverNameToTimeToVmInstanceNameToCpus = null;
			serverLinkToVmInstanceNameToInfo = null;
			serverNameToTimeToCpus = null;
		}
	}
}