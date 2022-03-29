using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using System.Drawing;

using LibCore;
using GameManagement;
using Network;
using Logging;
using ReportBuilder;

namespace Cloud.ReportsScreen
{
	public class NewServicesReport
	{
		NetworkProgressionGameFile gameFile;
		int round;
		NodeTree initialModel;
		NodeTree model;

		enum ServiceStatus
		{
			NotCreated,
			WaitingOnDevStorage,
			WaitingOnDevServer,
			WaitingOnDev,
			WaitingOnHandover,
			WaitingOnStorage,
			WaitingOnServer,
			Up
		}

		class ServiceStatement
		{
			public double Start;
			public double End;
			public double DevCost;
			public double Trades;
			public double FinesAvoided;

			public ServiceStatement (double start, double end)
			{
				Start = start;
				End = end;
			}

			public ServiceStatement (double start, double end, ServiceStatement a)
			{
				Start = start;
				End = end;
				DevCost = a.DevCost;
				Trades = a.Trades;
				FinesAvoided = a.FinesAvoided;
			}

			public double Net
			{
				get
				{
					return Trades + FinesAvoided - DevCost;
				}
			}

			public override string ToString ()
			{
				StringBuilder builder = new StringBuilder ();

				int startPeriod = 1 + (int) (Start / 60);
				int endPeriod = 1 + (int) (End / 60);

				if (endPeriod <= (startPeriod + 1))
				{
					builder.AppendLine(CONVERT.Format("Trading period {0}", startPeriod));
				}
				else
				{
					builder.AppendLine(CONVERT.Format("Trading periods {0} - {1}", startPeriod, endPeriod - 1));
				}

				builder.AppendLine(CONVERT.Format("Dev investment: ${0}", CONVERT.ToPaddedStrWithThousands(DevCost, 0)));

				if (Trades > 0)
				{
					builder.AppendLine(CONVERT.Format("Trades: ${0}", CONVERT.ToPaddedStrWithThousands(Trades, 0)));
				}

				if (FinesAvoided > 0)
				{
					builder.AppendLine(CONVERT.Format("Fines avoided: ${0}", CONVERT.ToPaddedStrWithThousands(FinesAvoided, 0)));
				}

				string value;
				if (Net >= 0)
				{
					value = "$" + CONVERT.ToPaddedStrWithThousands(Net, 0);
				}
				else
				{
					value = "($" + CONVERT.ToPaddedStrWithThousands(Math.Abs(Net), 0) + ")";
				}
				builder.AppendLine(CONVERT.Format("Net: {0}", value));

				return builder.ToString();
			}
		}

		Dictionary<string, TimeLog<ServiceStatus>> serviceNameToTimeToStatus;
		Dictionary<string, TimeLog<ServiceStatement>> serviceNameToTimeToStatement;
		Dictionary<string, TimeLog<bool>> serviceNameToDemandActive;
		Dictionary<string, StringBuilder> serviceNameToMoneyMessages;

		public NewServicesReport (NetworkProgressionGameFile gameFile, int round)
		{
			this.gameFile = gameFile;
			this.round = round;

			initialModel = new NodeTree (File.ReadAllText(gameFile.GetRoundFile(round, "network_at_start.xml", GameFile.GamePhase.OPERATIONS)));
			model = gameFile.GetNetworkModel(round);
		}

		public string BuildReport (string business, bool includeNewServices, bool includeDemands)
		{
			ReportUtils reportUtils = new ReportUtils (gameFile);

			BasicXmlDocument xml = BasicXmlDocument.Create();
			XmlElement root = xml.AppendNewChild("timechart");

			// Build the time axis.
			XmlElement timeline = xml.AppendNewChild(root, "timeline");
			BasicXmlDocument.AppendAttribute(timeline, "start", 1);
			BasicXmlDocument.AppendAttribute(timeline, "end", 1 + initialModel.GetNamedNode("CurrentTime").GetIntAttribute("round_duration", 0) / 60);
			BasicXmlDocument.AppendAttribute(timeline, "interval", 1);
			BasicXmlDocument.AppendAttribute(timeline, "legend", "Trading Periods");
			BasicXmlDocument.AppendAttribute(timeline, "fore_colour", "255,255,255");
			BasicXmlDocument.AppendAttribute(timeline, "back_colour", "0,0,0");
			BasicXmlDocument.AppendAttribute(timeline, "minutes_back_colour", "0,0,0");

			// Now build the chart proper.
			XmlElement sections = xml.AppendNewChild(root, "sections");

			// Build a list of services.
			Dictionary<string, List<Node>> serviceCommonNameToServices = new Dictionary<string, List<Node>> ();
			Dictionary<string, double> serviceNameToPotentialValue = new Dictionary<string, double> ();
			Dictionary<string, string> serviceNameToCommonName = new Dictionary<string, string> ();

			Dictionary<string, string> serviceNameToShortDisplayName = new Dictionary<string, string> ();

			serviceNameToTimeToStatus = new Dictionary<string, TimeLog<ServiceStatus>> ();
			serviceNameToTimeToStatement = new Dictionary<string, TimeLog<ServiceStatement>> ();
			serviceNameToDemandActive = new Dictionary<string,TimeLog<bool>> ();
			serviceNameToMoneyMessages = new Dictionary<string, StringBuilder> ();

			List<string> serviceNamesToInclude = new List<string> ();

			foreach (Node businessService in model.GetNodesWithAttributeValue("type", "business_service"))
			{
				string serviceCommonName = businessService.GetAttribute("common_service_name");

				if (! serviceCommonNameToServices.ContainsKey(serviceCommonName))
				{
					serviceCommonNameToServices.Add(serviceCommonName, new List<Node>());
				}
				serviceCommonNameToServices[serviceCommonName].Add(businessService);

				string serviceName = businessService.GetAttribute("name");
				serviceNameToCommonName.Add(serviceName, serviceCommonName);

				StringBuilder builder = new StringBuilder ();
				if ((! businessService.GetBooleanAttribute("is_new_service", false))
					&& (! businessService.GetBooleanAttribute("is_preexisting", false))
					&& (! businessService.GetBooleanAttribute("is_placeholder", false)))
				{
					Node demand = model.GetNamedNode(businessService.GetAttribute("demand_name"));
					builder.Append(CONVERT.Format("({0}) {1}", demand.GetAttribute(CONVERT.Format("short_desc_round_{0}", round)), businessService.GetAttribute("common_service_name")));
				}
				else
				{
					builder.Append(businessService.GetAttribute("common_service_name"));
				}

				serviceNameToShortDisplayName.Add(serviceName, builder.ToString());
			}

			BasicIncidentLogReader logReader = new BasicIncidentLogReader (gameFile.GetRoundFile(round, "NetworkIncidents.log", GameFile.GamePhase.OPERATIONS));
			foreach (Node businessService in model.GetNodesWithAttributeValue("type", "business_service"))
			{
				// Skip dev services and those that were there at the start of the round.
				if (businessService.GetBooleanAttribute("is_dev", false)
					|| (initialModel.GetNamedNode(businessService.GetAttribute("name")) != null))
				{
					continue;
				}

				if (businessService.Parent.GetAttribute("name") == business)
				{
					string serviceName = businessService.GetAttribute("name");

					serviceNameToTimeToStatement.Add(serviceName, new TimeLog<ServiceStatement> ());
					serviceNameToTimeToStatement[serviceName].Add(0, new ServiceStatement (0, 0));

					serviceNameToTimeToStatus.Add(serviceName, new TimeLog<ServiceStatus> ());
					serviceNameToTimeToStatus[serviceName].Add(0, ServiceStatus.NotCreated);

					double value = businessService.GetDoubleAttribute("trades_per_realtime_minute", 0) * businessService.GetDoubleAttribute("revenue_per_trade", 0) * model.GetNamedNode("CurrentTime").GetDoubleAttribute("round_duration", 0) / 60;
					if (businessService.GetBooleanAttribute("is_regulation", false))
					{
						double regulationFine = reportUtils.GetCost("fine", round);
						value += regulationFine;
						serviceNameToTimeToStatement[serviceName][0].FinesAvoided += regulationFine;
					}
					serviceNameToPotentialValue.Add(serviceName, value);

					serviceNameToMoneyMessages.Add(serviceName, new StringBuilder ());

					string demandName = businessService.GetAttribute("demand_name");
					bool isDemand = ! string.IsNullOrEmpty(demandName);
					if ((isDemand && includeDemands) || ((! isDemand) && includeNewServices))
					{
						serviceNamesToInclude.Add(serviceName);
					}

					if (isDemand)
					{
						logReader.WatchApplyAttributes(demandName, new LogLineFoundDef.LineFoundHandler (logReader_DemandApplyAttributes));
					}

					logReader.WatchApplyAttributes(serviceName, new LogLineFoundDef.LineFoundHandler (logReader_ServiceApplyAttributes));
				}
			}

			logReader.WatchCreatedNodes("Turnover", new LogLineFoundDef.LineFoundHandler (logReader_TurnoverCreateNodes));

			logReader.Run();

			double lastKnownTime = model.GetNamedNode("CurrentTime").GetIntAttribute("seconds", 0);

			List<string> orderedServiceNames = new List<string> (serviceNamesToInclude);
			orderedServiceNames.Sort(delegate (string a, string b)
									{
										Node serviceA = model.GetNamedNode(a);
										Node serviceB = model.GetNamedNode(b);

										if (serviceA.GetBooleanAttribute("is_new_service", false)
											&& (! serviceB.GetBooleanAttribute("is_new_service", false)))
										{
											return -1;
										}
										else if ((! serviceA.GetBooleanAttribute("is_new_service", false))
												 && serviceB.GetBooleanAttribute("is_new_service", false))
										{
											return 1;
										}

										double? aCreatedTime = null;
										if (serviceNameToTimeToStatus.ContainsKey(a))
										{
											foreach (double time in serviceNameToTimeToStatus[a].Times)
											{
												if (serviceNameToTimeToStatus[a][time] != ServiceStatus.NotCreated)
												{
													aCreatedTime = time;
													break;
												}
											}
										}

										double? bCreatedTime = null;
										if (serviceNameToTimeToStatus.ContainsKey(b))
										{
											foreach (double time in serviceNameToTimeToStatus[b].Times)
											{
												if (serviceNameToTimeToStatus[b][time] != ServiceStatus.NotCreated)
												{
													bCreatedTime = time;
													break;
												}
											}
										}

										if ((! aCreatedTime.HasValue) && (! bCreatedTime.HasValue))
										{
											return a.CompareTo(b);
										}
										if (aCreatedTime.HasValue && ! bCreatedTime.HasValue)
										{
											return -1;
										}
										else if (bCreatedTime.HasValue && ! aCreatedTime.HasValue)
										{
											return 1;
										}
										else if (aCreatedTime.Value != bCreatedTime.Value)
										{
											return aCreatedTime.Value.CompareTo(bCreatedTime.Value);
										}
										else
										{
											return a.CompareTo(b);
										}
									});

			// Build the chart.
			int rowNumber = 0;
			foreach (string serviceName in orderedServiceNames)
			{
				XmlElement section = xml.AppendNewChild(sections, "section");
				string rowForeColour;
				string rowBackColour;

				if ((rowNumber % 2) == 0)
				{
					rowBackColour = "92,86,101";
					rowForeColour = "255,255,255";
				}
				else
				{
					rowBackColour = "102,94,109";
					rowForeColour = "255,255,255";
				}
				rowNumber++;
				BasicXmlDocument.AppendAttribute(section, "row_forecolour", rowForeColour);
				BasicXmlDocument.AppendAttribute(section, "row_backcolour", rowBackColour);
				BasicXmlDocument.AppendAttribute(section, "legend", serviceNameToShortDisplayName[serviceName]);
				BasicXmlDocument.AppendAttribute(section, "header_width", "200");

				List<Node> businesses = new List<Node> ((Node []) model.GetNodesWithAttributeValue("type", "business").ToArray(typeof (Node)));
				businesses.Sort(delegate (Node a, Node b)
								{
									return a.GetIntAttribute("order", 0).CompareTo(b.GetIntAttribute("order", 0));
								});

				StringBuilder regions = new StringBuilder ();
				StringBuilder regions_code = new StringBuilder();
				foreach (Node region in businesses)
				{
					bool hasRegion = false;

					foreach (Node service in serviceCommonNameToServices[serviceNameToCommonName[serviceName]])
					{
						if (service.Parent == region)
						{
							hasRegion = true;
							break;
						}
					}

					if (regions.Length > 0)
					{
						regions.Append(" ");
					}
					if (hasRegion)
					{
						string desc = region.GetAttribute("desc");
						regions.Append(desc.Substring(0, 2));
						switch (desc.ToLower())
						{ 
							case "america":
								regions_code.Append("m");
								break;
							case "europe":
								regions_code.Append("o");
								break;
							case "africa":
								regions_code.Append("f");
								break;
							case "asia":
								regions_code.Append("s");
								break;
						}
					}
					else
					{
						regions.Append("--");
					}
				}

				XmlElement row = xml.AppendNewChild(section, "row");
				BasicXmlDocument.AppendAttribute(row, "legend", regions.ToString());
				BasicXmlDocument.AppendAttribute(row, "regions_code", regions_code.ToString());
				BasicXmlDocument.AppendAttribute(row, "header_width", "100");

				List<double> times = new List<double> (serviceNameToTimeToStatus[serviceName].Times);

				// Add the breakeven point to the list of times, if there is one.
				double? lastTime = null;
				foreach (double time in serviceNameToTimeToStatement[serviceName].Times)
				{
					if (lastTime.HasValue)
					{
						if (((serviceNameToTimeToStatement[serviceName][lastTime.Value].Net < 0)
							&& (serviceNameToTimeToStatement[serviceName][time].Net >= 0))
							|| ((serviceNameToTimeToStatement[serviceName][lastTime.Value].Net == 0)
							&& (serviceNameToTimeToStatement[serviceName][time].Net > 0)))
						{
							if (! times.Contains(time))
							{
								times.Add(time);
								times.Sort();
							}
						}
					}

					lastTime = time;
				}

				foreach (double time in times)
				{
					// This segment ends at either the end of time, or the start of the next segment.
					double end = lastKnownTime;
					if (times.IndexOf(time) < (times.Count - 1))
					{
						end = times[times.IndexOf(time) + 1];
					}

					// If we're a demand, don't emit any segments after the demand has gone inactive again.
					if (serviceNameToDemandActive.ContainsKey(serviceName))
					{
						double? timeGoesInactiveAgain = null;

						bool hasGoneActive = false;
						
						foreach (double tryTime in serviceNameToDemandActive[serviceName].Times)
						{
							if (serviceNameToDemandActive[serviceName][tryTime])
							{
								hasGoneActive = true;
							}
							else if (hasGoneActive)
							{
								timeGoesInactiveAgain = tryTime;
							}
						}

						if (timeGoesInactiveAgain.HasValue)
						{
							end = Math.Min(end, timeGoesInactiveAgain.Value);
						}
					}

					Color blockColour = Color.Gray;
					string legend = "";
					bool emit = true;

					switch (serviceNameToTimeToStatus[serviceName].GetLastValueOnOrBefore(time))
					{
						case ServiceStatus.NotCreated:
							emit = false;
							break;

						case ServiceStatus.WaitingOnDevStorage:
							//blockColour = Color.Crimson;
							blockColour = Color.FromArgb(Color.Crimson.B, Color.Crimson.G, Color.Crimson.R);
							legend = "DevStorage";
							break;

						case ServiceStatus.WaitingOnDevServer:
							//blockColour = Color.DarkRed;
							blockColour = Color.FromArgb(Color.DarkRed.B, Color.DarkRed.G, Color.DarkRed.R);
							legend = "DevServer";
							break;

						case ServiceStatus.WaitingOnDev:
							//blockColour = Color.Red;
							blockColour = Color.FromArgb(Color.Red.B, Color.Red.G, Color.Red.R);
							legend = "Dev";
							break;

						case ServiceStatus.WaitingOnHandover:
							blockColour = Color.Orange;
							legend = "Transition";
							break;

						case ServiceStatus.WaitingOnStorage:
							blockColour = Color.Yellow;
							legend = "Storage";
							break;

						case ServiceStatus.WaitingOnServer:
							blockColour = Color.LightYellow;
							legend = "Server";
							break;

						case ServiceStatus.Up:
							if (serviceNameToTimeToStatement[serviceName].GetLastValueOnOrBefore(time).Net < 0)
							{
								blockColour = Color.LightGreen;
								legend = "Debit";
							}							
							else
							{
								blockColour = Color.Green;
								legend = "Profit";
							}

							if (serviceNameToDemandActive.ContainsKey(serviceName)
								&& ! serviceNameToDemandActive[serviceName].GetLastValueOnOrBefore(time))
							{
								emit = false;
							}

							break;
					}

					if (emit)
					{
						XmlElement block = xml.AppendNewChild(row, "block");
						BasicXmlDocument.AppendAttribute(block, "start", 1 + (time / 60));
						BasicXmlDocument.AppendAttribute(block, "end", 1 + (end / 60));
						BasicXmlDocument.AppendAttribute(block, "colour", CONVERT.ToComponentStr(blockColour));
						BasicXmlDocument.AppendAttribute(block, "legend", legend);
					}
				}

				foreach (double time in serviceNameToTimeToStatement[serviceName].Times)
				{
					serviceNameToTimeToStatement[serviceName][time].End = serviceNameToTimeToStatement[serviceName].TryGetFirstTimeAfter(time, time + 60 - (time % 60));

					XmlElement block = xml.AppendNewChild(row, "mouseover_block");
					BasicXmlDocument.AppendAttribute(block, "start", 1 + (serviceNameToTimeToStatement[serviceName][time].Start / 60));
					BasicXmlDocument.AppendAttribute(block, "end", 1 + (serviceNameToTimeToStatement[serviceName][time].End / 60));
					BasicXmlDocument.AppendAttribute(block, "legend", serviceNameToTimeToStatement[serviceName][time].ToString());
				}
			}

			string reportFile = gameFile.GetRoundFile(gameFile.LastRoundPlayed, "NewServicesReport.xml", GameFile.GamePhase.OPERATIONS);
			xml.Save(reportFile);
			return reportFile;
		}

		Node GetServiceByName (Dictionary<string, List<Node>> serviceCommonNameToServices, Dictionary<string, string> serviceNameToCommonName, string serviceName)
		{
			foreach (Node service in serviceCommonNameToServices[serviceNameToCommonName[serviceName]])
			{
				if (service.GetAttribute("name") == serviceName)
				{
					return service;
				}
			}

			return null;
		}

		void logReader_TurnoverCreateNodes (object sender, string key, string line, double time)
		{
			string serviceName = BasicIncidentLogReader.ExtractValue(line, "business_service");
			Node service = model.GetNamedNode(serviceName);

			if ((service != null) && service.GetBooleanAttribute("is_dev", false))
			{
				serviceName = service.GetAttribute("production_service_name");
			}

			double amount = CONVERT.ParseDouble(BasicIncidentLogReader.ExtractValue(line, "value"));

			if (serviceNameToTimeToStatement.ContainsKey(serviceName))
			{
				serviceNameToTimeToStatement[serviceName][time] = new ServiceStatement (time, 0, serviceNameToTimeToStatement[serviceName].LastValue);

				switch (BasicIncidentLogReader.ExtractValue(line, "type"))
				{
					case "bill":
						switch (BasicIncidentLogReader.ExtractValue(line, "bill_type"))
						{
							case "development":
								serviceNameToTimeToStatement[serviceName][time].DevCost += Math.Abs(amount);
								break;
						}
						break;

					case "trade":
						serviceNameToTimeToStatement[serviceName][time].Trades += amount;
						break;
				}
			}
		}

		ServiceStatus GetServiceStatusFromString (string status)
		{
			switch (status)
			{
				case "waiting_on_dev_storage":
					return ServiceStatus.WaitingOnDevStorage;

				case "waiting_on_dev_server":
					return ServiceStatus.WaitingOnDevServer;

				case "waiting_on_dev":
					return ServiceStatus.WaitingOnDev;

				case "waiting_on_handover":
					return ServiceStatus.WaitingOnHandover;

				case "waiting_on_storage":
					return ServiceStatus.WaitingOnStorage;

				case "waiting_on_server":
					return ServiceStatus.WaitingOnServer;

				case "up":
					return ServiceStatus.Up;
			}

			return ServiceStatus.NotCreated;
		}

		void logReader_ServiceApplyAttributes (object sender, string key, string line, double time)
		{
			string serviceName = BasicIncidentLogReader.ExtractValue(line, "i_name");
			string status = BasicIncidentLogReader.ExtractValue(line, "status");
			if (! string.IsNullOrEmpty(status))
			{
				ServiceStatus serviceStatus = GetServiceStatusFromString(status);

				// Backdate dev and handover to the start of the minute.
				if ((serviceStatus != ServiceStatus.Up) && (serviceStatus != ServiceStatus.NotCreated))
				{
					time -= (time % 60);
				}

				serviceNameToTimeToStatus[serviceName][time] = serviceStatus;
			}
		}

		void logReader_DemandApplyAttributes (object sender, string key, string line, double time)
		{
			string demandName = BasicIncidentLogReader.ExtractValue(line, "i_name");
			string serviceName = model.GetNamedNode(demandName).GetAttribute("business_service");

			string status = BasicIncidentLogReader.ExtractValueGivenDefault(line, "status", null);
			if (status != null)
			{
				if (! serviceNameToDemandActive.ContainsKey(serviceName))
				{
					serviceNameToDemandActive.Add(serviceName, new TimeLog<bool> ());
				}

				serviceNameToDemandActive[serviceName][time] = (status == "running");
			}
		}
	}
}