using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

using LibCore;
using GameManagement;
using Network;
using ReportBuilder;
using Logging;

namespace Cloud.ReportsScreen
{
	public class Cloud_RoundScores : RoundScores, IDisposable
	{
		public bool extract_failure = false;

		class TurnoverItem
		{
			List<AttributeValuePair> attributes;

			double time;
			public double Time
			{
				get
				{
					return time;
				}
			}

			public TurnoverItem (string logLine)
			{
				logLine = BaseUtils.xml_utils.TranslateFromEscapedXMLChars(logLine);
				attributes = ExtractAttributesFromLogLine(logLine, true);
				time = CONVERT.ParseDouble(BasicIncidentLogReader.ExtractValue(logLine, "i_doAfterSecs"));
			}

			public TurnoverItem (Node node)
			{
				attributes = new List<AttributeValuePair> ();
				foreach (AttributeValuePair avp in node.GetAttributes())
				{
					attributes.Add(avp);
				}

				time = 0;
			}

			public void Update (string logLine)
			{
				logLine = BaseUtils.xml_utils.TranslateFromEscapedXMLChars(logLine);
				List<AttributeValuePair> newAttributes = ExtractAttributesFromLogLine(logLine, false);
				time = CONVERT.ParseDouble(BasicIncidentLogReader.ExtractValue(logLine, "i_doAfterSecs"));

				foreach (AttributeValuePair avp in newAttributes)
				{
					bool found = false;

					foreach (AttributeValuePair tryAvp in attributes)
					{
						if (tryAvp.Attribute == avp.Attribute)
						{
							tryAvp.Value = avp.Value;
							found = true;
							break;
						}
					}

					if (! found)
					{
						attributes.Add(avp);
					}
				}
			}

			static List<AttributeValuePair> ExtractAttributesFromLogLine (string logLine, bool isCreateLine)
			{
				List<AttributeValuePair> attributes = new List<AttributeValuePair> ();

				string commandName;
				if (isCreateLine)
				{
					commandName = "createNodes";
				}
				else
				{
					commandName = "apply";
				}
				 
				string prefix = "<" + commandName + " ";
				int prefixStart = logLine.IndexOf(prefix);
				Debug.Assert(prefixStart != -1);

				string chopped = logLine.Substring(prefixStart);

				if (isCreateLine)
				{
					chopped = chopped.Substring(chopped.IndexOf(">") + 1);
				}

				string suffix = "/>";
				chopped = chopped.Substring(0, chopped.IndexOf(suffix));

				List<string> parts = new List<string> ();
				StringBuilder currentPart = new StringBuilder ();
				bool inQuotes = false;
				foreach (char c in chopped)
				{
					bool handled = false;

					switch (c)
					{
						case '"':
							inQuotes = ! inQuotes;
							handled = true;
							break;

						case ' ':
							if (! inQuotes)
							{
								if (currentPart.Length > 0)
								{
									parts.Add(currentPart.ToString());
									currentPart = new StringBuilder ();
									handled = true;
								}
							}
							break;
					}

					if (! handled)
					{
						currentPart.Append(c);
					}
				}
				if (currentPart.Length > 0)
				{
					parts.Add(currentPart.ToString());
					currentPart = new StringBuilder ();							
				}

				for (int i = 1; i < parts.Count; i++)
				{
					string [] components = parts[i].Split('=');

					string attributeName = components[0];

					if (! attributeName.StartsWith("i_"))
					{
						string attributeValue = components[1];
						attributes.Add(new AttributeValuePair(attributeName, attributeValue));
					}
				}

				return attributes;
			}

			public AttributeValuePair GetAttributeValuePair (string attributeName)
			{
				foreach (AttributeValuePair avp in attributes)
				{
					if (avp.Attribute == attributeName)
					{
						return avp;
					}
				}

				return null;
			}

			public string GetAttribute (string attributeName)
			{
				AttributeValuePair avp = GetAttributeValuePair(attributeName);
				if (avp != null)
				{
					return avp.Value;
				}

				return null;
			}

			public void SetAttribute (string attributeName, string value)
			{
				AttributeValuePair avp = GetAttributeValuePair(attributeName);
				if (avp != null)
				{
					avp.Value = value;
				}
				else
				{
					attributes.Add(new AttributeValuePair (attributeName, value));
				}
			}
		}

		NodeTree model;
		NodeTree initialModel;
		double duration;

		Dictionary<string, TurnoverItem> nameToTurnoverItem;

		double totalNewServiceCost;
		public double TotalNewServiceCost
		{
			get
			{
				return totalNewServiceCost;
			}
		}

		double netValue;
		public double NetValue
		{
			get
			{
				return netValue;
			}
		}

		public int NewServicesDeployed
		{
			get
			{
				return newServicesCommissioned.Count;
			}
		}

		public int DemandsDeployed
		{
			get
			{
				return demandsCommissioned.Count;
			}
		}

		double totalOpportunity;
		public double TotalOpportunity
		{
			get
			{
				return totalOpportunity;
			}
		}

		double totalRealisedOpportunity;
		public double TotalRealisedOpportunity
		{
			get
			{
				return totalRealisedOpportunity;
			}
		}

		double newServiceRealisedOpportunity;
		public double NewServiceRealisedOpportunity
		{
			get
			{
				return newServiceRealisedOpportunity;
			}
		}

		double demandRealisedOpportunity;
		public double DemandRealisedOpportunity
		{
			get
			{
				return demandRealisedOpportunity;
			}
		}

		double totalOperatingCosts;
		public double TotalOperatingCosts
		{
			get
			{
				return totalOperatingCosts;
			}
		}

		double businessAsUsualRevenue;
		public double BusinessAsUsualRevenue
		{
			get
			{
				return businessAsUsualRevenue;
			}
		}

		Dictionary<string, RegionPerformance> nameToRegion;
		public IDictionary<string, RegionPerformance> NameToRegion
		{
			get
			{
				return new Dictionary<string, RegionPerformance> (nameToRegion);
			}
		}

		public double TargetRevenue
		{
			get
			{
				double target = 0;

				foreach (RegionPerformance performance in nameToRegion.Values)
				{
					target += performance.TargetRevenue;
				}

				return target;
			}
		}

		CpuUsageReport cpuUsageReport;
		public CpuUsageReport CpuUsageReport
		{
			get
			{
				return cpuUsageReport;
			}
		}

		List<string> newServicesCommissioned;
		List<string> demandsCommissioned;

		Dictionary<Node, RegionPerformance> businessToPerformance;

		List<string> regions;
		public IList<string> Regions
		{
			get
			{
				return regions.AsReadOnly();
			}
		}

		Dictionary<string, TimeLog<double>> newServiceNameToTimeToNetValue;
		public double TimeToValue
		{
			get
			{
				double totalTimeTillProfit = 0;
				int services = 0;

				foreach (string serviceName in newServiceNameToTimeToNetValue.Keys)
				{
					Node businessService = model.GetNamedNode(serviceName);
					if ((businessService != null) && businessService.GetBooleanAttribute("is_new_service", false))
					{
						double startTime = newServiceNameToTimeToNetValue[serviceName].FirstTime;
						double? profitTime = null;
						foreach (double time in newServiceNameToTimeToNetValue[serviceName].Times)
						{
							if (newServiceNameToTimeToNetValue[serviceName][time] > 0)
							{
								profitTime = time - 1;
								break;
							}
						}

						if (profitTime.HasValue)
						{
							totalTimeTillProfit += (profitTime.Value - startTime);
							services++;
						}
					}
				}

				return (totalTimeTillProfit / 60) / services;
			}
		}

		public class RegionPerformance
		{
			public string Name;
			public int NewServicesDeployed;
			public int DemandsDeployed;
			public int ServicesDeployed;
			public double MaxPossibleServicesDeployed;
			public double TotalNewServiceCost;
			public double NewServiceOpportunityRealised;
			public double MaxPossibleNewServiceOpportunity;
			public double DemandOpportunityRealised;
			public double MaxPossibleDemandOpportunity;
			public double BusinessAsUsualRevenue;

			public double DevBudget;
			public double ITBudget;

			public double OpEx;
			public double CapEx;

			public double DevTestCost;
			public double ProductionCost;

			public double EnergyKWs;
			public double Dcie;
			public double Pue;
			public double RegulationFines;

			public int DevCpus;
			public double DevCpuUtilisation;

			public int ProductionCpus;
			public double ProductionCpuUtilisation;

			public int TotalCpus;
			public double CpuUtilisation;

			public double TargetRevenue;

			public Dictionary<double, double> timeToItPowerUsed;
			public Dictionary<double, double> timeToNonItPowerUsed;

			public double TotalBusinessDemandCpuPeriods;
			public double TotalCapacityCpuPeriods;

			public double RealisedCpuDemandFraction;
			public double InternalCpuProvisionFraction;
			public double ExternalCpuProvisionFraction;

			public double NetValue
			{
				get
				{
					return NewServiceOpportunityRealised + DemandOpportunityRealised - TotalNewServiceCost;
				}
			}

			public double ITOverspend
			{
				get
				{
					return ITBudget  - (CapEx + OpEx);
				}
			}

			public double DevOverspend
			{
				get
				{
					return TotalNewServiceCost - DevBudget;
				}
			}

			public double TotalOpportunityRealised
			{
				get
				{
					return NewServiceOpportunityRealised + DemandOpportunityRealised;
				}
			}

			public RegionPerformance (string name)
			{
				Name = name;

				timeToItPowerUsed = new Dictionary<double, double> ();
				timeToNonItPowerUsed = new Dictionary<double, double> ();
			}
		}

		public Cloud_RoundScores (NetworkProgressionGameFile gameFile, int round, int previousProfit, int newServices, SupportSpendOverrides spendOverrides)
			: base (gameFile, round, previousProfit, newServices, spendOverrides)
		{
			extract_failure = false;
			try
			{
				model = gameFile.GetNetworkModel(round);
				duration = model.GetNamedNode("CurrentTime").GetIntAttribute("seconds", 0);

				ReportUtils reportUtils = new ReportUtils(gameFile);

				string initialModelFilename = gameFile.GetRoundFile(round, "network_at_start.xml", GameFile.GamePhase.OPERATIONS);
				if (!System.IO.File.Exists(initialModelFilename))
				{
					initialModelFilename = gameFile.GetRoundFile(round, "network.xml", GameFile.GamePhase.OPERATIONS);
				}
				initialModel = new NodeTree (System.IO.File.ReadAllText(initialModelFilename));

				BasicIncidentLogReader logReader = new BasicIncidentLogReader(gameFile.GetRoundFile(round, "NetworkIncidents.log", GameFile.GamePhase.OPERATIONS));

				regions = new List<string>();
				nameToRegion = new Dictionary<string, RegionPerformance>();
				businessToPerformance = new Dictionary<Node, RegionPerformance>();
				newServicesCommissioned = new List<string>();
				demandsCommissioned = new List<string>();
				newServiceNameToTimeToNetValue = new Dictionary<string, TimeLog<double>>();

				nameToTurnoverItem = new Dictionary<string, TurnoverItem>();
				foreach (Node turnoverItem in initialModel.GetNamedNode("Turnover").getChildren())
				{
					AddTurnoverItem(new TurnoverItem(turnoverItem), logReader);
				}

				List<Node> businessesInOrder = new List<Node>();
				foreach (Node business in model.GetNamedNode("Businesses").GetChildrenOfType("business"))
				{
					businessesInOrder.Add(business);
				}
				businessesInOrder.Sort(delegate(Node a, Node b)
				{
					return a.GetIntAttribute("order", 0).CompareTo(b.GetIntAttribute("order", 0));
				});

				//foreach (Node business in model.GetNamedNode("Businesses").GetChildrenOfType("business"))
				foreach (Node business in businessesInOrder)
				{
					string name = business.GetAttribute("desc");

					RegionPerformance regionPerformance = new RegionPerformance(name);

					regions.Add(name);
					nameToRegion.Add(name, regionPerformance);
					businessToPerformance.Add(business, regionPerformance);

					regionPerformance.ITBudget = business.GetIntAttribute("it_budget", 0);
					regionPerformance.DevBudget = business.GetIntAttribute("dev_budget", 0);

					Node datacenter = model.GetNamedNode(business.GetAttribute("datacenter"));

					regionPerformance.TargetRevenue = business.GetDoubleAttribute("target_revenue", 0);

					regionPerformance.timeToItPowerUsed.Add(0, initialModel.GetNamedNode(datacenter.GetAttribute("name")).GetDoubleAttribute("it_power_kw", 0));
					regionPerformance.timeToNonItPowerUsed.Add(0, initialModel.GetNamedNode(datacenter.GetAttribute("name")).GetDoubleAttribute("non_it_power_kw", 0));

					regionPerformance.TotalCpus = 0;
					foreach (Node rack in datacenter.GetChildrenOfType("rack"))
					{
						int cpus = 0;
						foreach (Node server in rack.GetChildrenOfType("server"))
						{
							cpus += server.GetIntAttribute("cpus", 0);
						}

						regionPerformance.TotalCpus += cpus;
						switch (rack.GetAttribute("owner"))
						{
							case "dev&test":
								regionPerformance.DevCpus += cpus;
								break;

							case "online":
							case "floor":
								regionPerformance.ProductionCpus += cpus;
								break;
						}
					}

					logReader.WatchApplyAttributes(datacenter.GetAttribute("name"), new LogLineFoundDef.LineFoundHandler(logReader_DatacenterApplyAttributes));
					logReader.WatchCreatedNodes(business.GetAttribute("name"), new LogLineFoundDef.LineFoundHandler(logReader_BusinessCreatedNodes));
				}

				foreach (Node demand in model.GetNodesWithAttributeValue("type", "demand"))
				{
					logReader.WatchApplyAttributes(demand.GetAttribute("name"), new LogLineFoundDef.LineFoundHandler(logReader_DemandApplyAttributes));
				}

				logReader.WatchCreatedNodes("Turnover", new LogLineFoundDef.LineFoundHandler(logReader_TurnoverCreatedNodes));
				logReader.Run();

				ProcessTurnoverItems();

				foreach (Node newService in model.GetNodesWithAttributeValue("type", "new_service"))
				{
					bool availableInThisRound = newService.GetBooleanAttribute(CONVERT.Format("available_in_round_{0}", round), false);
					if (availableInThisRound)
					{
						Node business = model.GetNamedNode(newService.GetAttribute("business"));
						businessToPerformance[business].MaxPossibleServicesDeployed++;

						if (newService.GetBooleanAttribute("is_new_service", false))
						{
							if (newServicesCommissioned.Contains(newService.GetAttribute("service_name")))
							{
								double serviceValue = newService.GetDoubleAttribute("trades_per_realtime_minute", 0)
														* newService.GetDoubleAttribute("revenue_per_trade", 0)
														* model.GetNamedNode("CurrentTime").GetDoubleAttribute("round_duration", 0) / 60;
								if (newService.GetAttribute("owner") == "floor")
								{
									serviceValue /= 2;
								}

								businessToPerformance[business].MaxPossibleNewServiceOpportunity += serviceValue;
							}

							if (newService.GetBooleanAttribute("is_regulation", false))
							{
								if (model.GetNamedNode(newService.GetAttribute("service_name")) == null)
								{
									businessToPerformance[business].RegulationFines += reportUtils.GetCost("fine", round);
								}
							}
						}
						else
						{
							if (demandsCommissioned.Contains(newService.GetAttribute("service_name")))
							{
								Node demand = model.GetNamedNode(newService.GetAttribute("demand_name"));
								double demandDuration = demand.GetDoubleAttribute("duration", 0) / 60;

								double serviceValue = newService.GetDoubleAttribute("trades_per_realtime_minute", 0)
														* newService.GetDoubleAttribute("revenue_per_trade", 0)
														* demandDuration;
								if ((newService.GetAttribute("owner") == "floor") && (demandDuration > 2))
								{
									serviceValue /= 2;
								}

								businessToPerformance[business].MaxPossibleDemandOpportunity += serviceValue;
							}
						}
					}
				}

				// Accumulate power figures.
				foreach (Node business in businessToPerformance.Keys)
				{
					RegionPerformance performance = businessToPerformance[business];
					double itEnergyKWs = IntegratePowerFigures(performance.timeToItPowerUsed);
					double nonItEnergyKWs = IntegratePowerFigures(performance.timeToNonItPowerUsed);

					performance.EnergyKWs = itEnergyKWs + nonItEnergyKWs;
					performance.Dcie = itEnergyKWs / nonItEnergyKWs;
					performance.Pue = 1 / performance.Dcie;
				}

				// Tot up BAU figures.
				foreach (Node business in businessToPerformance.Keys)
				{
					RegionPerformance performance = businessToPerformance[business];

					Node businessAsUsual = (Node)business.GetChildrenOfType("business_as_usual")[0];
					foreach (Node serviceBau in businessAsUsual.GetChildrenOfType("service_business_as_usual"))
					{
						foreach (Node dataPoint in serviceBau.GetChildrenOfType("business_as_usual_data_point"))
						{
							if (dataPoint.GetIntAttribute("time", 0) <= model.GetNamedNode("CurrentTime").GetIntAttribute("seconds", 0))
							{
								performance.BusinessAsUsualRevenue += dataPoint.GetDoubleAttribute("revenue", 0);
							}
						}
					}
				}

				// Accumulate opportunity figures.
				totalOpportunity = 0;
				totalRealisedOpportunity = 0;
				totalOperatingCosts = 0;
				businessAsUsualRevenue = 0;
				newServiceRealisedOpportunity = 0;
				demandRealisedOpportunity = 0;
				totalNewServiceCost = 0;
				netValue = 0;
				foreach (string regionName in NameToRegion.Keys)
				{
					RegionPerformance region = nameToRegion[regionName];

					totalOpportunity += region.MaxPossibleDemandOpportunity + region.MaxPossibleNewServiceOpportunity;
					totalRealisedOpportunity += region.DemandOpportunityRealised + region.NewServiceOpportunityRealised;
					totalOperatingCosts += region.OpEx;
					businessAsUsualRevenue += region.BusinessAsUsualRevenue;
					newServiceRealisedOpportunity += region.NewServiceOpportunityRealised;
					demandRealisedOpportunity += region.DemandOpportunityRealised;
					totalNewServiceCost += region.TotalNewServiceCost;
					netValue += region.NetValue;
				}

				// Let the CPU usage report calculate the utilisation.
				cpuUsageReport = new CpuUsageReport (gameFile, round, false);
				foreach (Node business in model.GetNodesWithAttributeValue("type", "business"))
				{
					RegionPerformance region = businessToPerformance[business];

					List<string> datacenters = new List<string>();
					datacenters.Add(business.GetAttribute("datacenter"));

					region.CpuUtilisation = cpuUsageReport.CalculateUtilisation(datacenters, null,
																				out region.TotalBusinessDemandCpuPeriods,
																				out region.TotalCapacityCpuPeriods,
																				out region.InternalCpuProvisionFraction,
																				out region.ExternalCpuProvisionFraction);
					region.DevCpuUtilisation = cpuUsageReport.CalculateUtilisation(datacenters, new List<string>(new string[] { "dev&test" }));
					region.ProductionCpuUtilisation = cpuUsageReport.CalculateUtilisation(datacenters, new List<string>(new string[] { "online", "floor" }));

                    OpsEngine.OrderPlanner orderPlanner = new OpsEngine.OrderPlanner(model, null, null);

					// Tot up the total CPU-periods needed by all demands available for this region this round.
					double totalDemandCpu = 0;
					double realisedDemandCpu = 0;
					foreach (Node businessServiceDefinition in model.GetNodesWithAttributeValue("type", "new_service"))
					{
						if ((!businessServiceDefinition.GetBooleanAttribute("new_service", false))
							&& (businessServiceDefinition.GetAttribute("business") == business.GetAttribute("name"))
							&& businessServiceDefinition.GetBooleanAttribute(CONVERT.Format("available_in_round_{0}", round), false))
						{
							Node demand = model.GetNamedNode(businessServiceDefinition.GetAttribute("demand_name"));

							if (demand != null)
							{
								Node vm = orderPlanner.GetAVmSuitableForBusinessServiceOrDefinition(businessServiceDefinition);

								double cpuPeriodsNeeded = vm.GetIntAttribute("cpus", 0)
															* demand.GetIntAttribute("duration", 0) / 60;

								totalDemandCpu += cpuPeriodsNeeded;

								if (demandsCommissioned.Contains(businessServiceDefinition.GetAttribute("service_name")))
								{
									realisedDemandCpu += cpuPeriodsNeeded;
								}
							}
						}
					}

					region.RealisedCpuDemandFraction = realisedDemandCpu / totalDemandCpu;
				}

				extract_failure = false;
			}
			catch (Exception)
			{
				extract_failure = true;
			}
		}

		double IntegratePowerFigures(Dictionary<double, double> timeToPower)
		{
			double lastTime = 0;
			double energy = 0;
			
			// One second of world time corresponds to six minutes of game time.
			double timeScale = model.GetNamedNode("CurrentTime").GetDoubleAttribute("real_world_second_corresponds_to_simulated_seconds", 0);

			List<double> sortedTimes = new List<double> (timeToPower.Keys);
			sortedTimes.Sort();

			if (sortedTimes.Count > 0)
			{
				if (sortedTimes[sortedTimes.Count - 1] < duration)
				{
					timeToPower.Add(duration, timeToPower[sortedTimes[sortedTimes.Count - 1]]);
					sortedTimes.Add(duration);
				}

				foreach (double time in sortedTimes)
				{
					energy += ((time - lastTime) * timeScale * timeToPower[time]);
					lastTime = time;
				}
			}

			return energy;
		}

		void logReader_TurnoverCreatedNodes (object sender, string key, string line, double time)
		{
			TurnoverItem item = new TurnoverItem (line);
			BasicIncidentLogReader logReader = (BasicIncidentLogReader) sender;

			AddTurnoverItem(item, logReader);
		}

		void AddTurnoverItem (TurnoverItem item, BasicIncidentLogReader logReader)
		{
			string turnoverItemName = item.GetAttribute("name");
			nameToTurnoverItem.Add(turnoverItemName, item);
			logReader.WatchApplyAttributes(turnoverItemName, new LogLineFoundDef.LineFoundHandler (logReader_TurnoverApplyAttributes));
		}

		void logReader_TurnoverApplyAttributes (object sender, string key, string line, double time)
		{
			string turnoverItemName = BasicIncidentLogReader.ExtractValue(line, "i_name");
			TurnoverItem item = nameToTurnoverItem[turnoverItemName];
			item.Update(line);
		}

		void ProcessTurnoverItems ()
		{
			foreach (TurnoverItem item in nameToTurnoverItem.Values)
			{
				string turnoverType = item.GetAttribute("type");
				string businessName = item.GetAttribute("business");
				Node business = model.GetNamedNode(businessName);
				double value = CONVERT.ParseDouble(item.GetAttribute("value"));
				double time = item.Time;

				switch (turnoverType)
				{
					case "bill":
						bool isDev = (item.GetAttribute("owner") == "dev&test");

						switch (item.GetAttribute("bill_type"))
						{
							case "opex":
								businessToPerformance[business].OpEx += - value;

								if (isDev)
								{
									businessToPerformance[business].DevTestCost += - value;
								}
								else
								{
									businessToPerformance[business].ProductionCost += - value;
								}
								break;

							case "capex":
								businessToPerformance[business].CapEx += -value;
								if (isDev)
								{
									businessToPerformance[business].DevTestCost += - value;
								}
								else
								{
									businessToPerformance[business].ProductionCost += - value;
								}
								break;

							case "development":
								businessToPerformance[business].TotalNewServiceCost += - value;

								string businessServiceName = item.GetAttribute("business_service");
								if (! newServiceNameToTimeToNetValue.ContainsKey(businessServiceName))
								{
									newServiceNameToTimeToNetValue.Add(businessServiceName, new TimeLog<double> ());
								}
								newServiceNameToTimeToNetValue[businessServiceName][time] = value;

								break;
						}
						break;

					case "trade":
						{
							switch (item.GetAttribute("trade_type"))
							{
								case "new_service":
									businessToPerformance[business].NewServiceOpportunityRealised += value;
									break;

								case "old_service":
									businessToPerformance[business].BusinessAsUsualRevenue += value;
									break;

								case "demand":
									businessToPerformance[business].DemandOpportunityRealised += value;
									break;
							}

							string businessServiceName = item.GetAttribute("business_service");
							if (newServiceNameToTimeToNetValue.ContainsKey(businessServiceName))
							{
								newServiceNameToTimeToNetValue[businessServiceName][time] = value + newServiceNameToTimeToNetValue[businessServiceName].GetLastValueOnOrBefore(time);
							}
						}

						break;
				}
			}
		}

		void logReader_DemandApplyAttributes (object sender, string key, string line, double time)
		{
			string demandName = BasicIncidentLogReader.ExtractValue(line, "i_name");
			Node demand = model.GetNamedNode(demandName);
			string businessServiceName = demand.GetAttribute("business_service");
			string startedString = BasicIncidentLogReader.ExtractValue(line, "started");
			if (CONVERT.ParseBool(startedString) ?? false)
			{
				if (! demandsCommissioned.Contains(businessServiceName))
				{
					demandsCommissioned.Add(businessServiceName);
				}
			}
		}

		void logReader_DatacenterApplyAttributes (object sender, string key, string line, double time)
		{
			string datacenterName = BasicIncidentLogReader.ExtractValue(line, "i_name");
			Node datacenter = model.GetNamedNode(datacenterName);
			string businessName = datacenter.GetAttribute("business");
			Node business = model.GetNamedNode(businessName);

			string itPowerString = BasicIncidentLogReader.ExtractValue(line, "it_power_kw");
			if (! string.IsNullOrEmpty(itPowerString))
			{
				double itPower = CONVERT.ParseDouble(itPowerString);
				businessToPerformance[business].timeToItPowerUsed[time] = itPower;
			}

			string nonItPowerString = BasicIncidentLogReader.ExtractValue(line, "non_it_power_kw");
			if (! string.IsNullOrEmpty(nonItPowerString))
			{
				double nonItPower = CONVERT.ParseDouble(nonItPowerString);
				businessToPerformance[business].timeToNonItPowerUsed[time] = nonItPower;
			}
		}

		void logReader_BusinessCreatedNodes (object sender, string key, string line, double time)
		{
			string businessName = BasicIncidentLogReader.ExtractValue(line, "i_to");
			Node business = model.GetNamedNode(businessName);

			string businessServiceName = BasicIncidentLogReader.ExtractValue(line, "name");

			string isDevString = BasicIncidentLogReader.ExtractValue(line, "is_dev");
			bool isDev = CONVERT.ParseBool(isDevString, false);

			if (! isDev)
			{
				businessToPerformance[business].ServicesDeployed++;

				string isNewString = BasicIncidentLogReader.ExtractValue(line, "is_new_service");
				if (CONVERT.ParseBool(isNewString, false))
				{
					businessToPerformance[business].NewServicesDeployed++;

					if (! newServicesCommissioned.Contains(businessServiceName))
					{
						newServicesCommissioned.Add(businessServiceName);
					}
				}
				else
				{
					businessToPerformance[business].DemandsDeployed++;

					if (! demandsCommissioned.Contains(businessServiceName))
					{
						demandsCommissioned.Add(businessServiceName);
					}
				}
			}

			string productionServiceName = BasicIncidentLogReader.ExtractValue(line, "production_service_name");
			if (!string.IsNullOrEmpty(productionServiceName))
			{
				businessServiceName = productionServiceName;
			}

			if (! newServiceNameToTimeToNetValue.ContainsKey(businessServiceName))
			{
				newServiceNameToTimeToNetValue.Add(businessServiceName, new TimeLog<double>());
				newServiceNameToTimeToNetValue[businessServiceName].Add(time, 0);
			}
		}

		public void Dispose ()
		{
			model = null;
			initialModel = null;
			nameToTurnoverItem.Clear();
            if (cpuUsageReport != null)
            {
                cpuUsageReport.Dispose();
            }
			newServicesCommissioned.Clear();
			demandsCommissioned.Clear();
			businessToPerformance.Clear();
			regions.Clear();
			newServiceNameToTimeToNetValue.Clear();
		}
	}
}