using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Drawing;

using Charts;
using LibCore;
using CoreUtils;
using GameManagement;
using Network;
using Logging;

using ServiceStatusName = DevOps.OpsEngine.ServiceStatus;
// ReSharper disable ObjectCreationAsStatement

namespace DevOps.ReportsScreen
{
	internal class NewServicesComparer : IComparer<string>
    {
        NodeTree model;

        readonly Dictionary<string, double> serviceNamesToStartTimes;

        public NewServicesComparer(NodeTree model, Dictionary<string, double> serviceStartTimes)
        {
            this.model = model;
            serviceNamesToStartTimes = serviceStartTimes;
        }

        public int Compare(string lhs, string rhs)
        {

            Debug.Assert(serviceNamesToStartTimes.ContainsKey(lhs) && serviceNamesToStartTimes.ContainsKey(rhs),
                "Shouldn't hit this.");

            var lhsTime = serviceNamesToStartTimes[lhs];
            var rhsTime = serviceNamesToStartTimes[rhs];

            return lhsTime.CompareTo(rhsTime);
            
        }
    }

    public class NewServicesReport
    {
        readonly NetworkProgressionGameFile gameFile;
        readonly int round;
        readonly NodeTree model;

        public enum ServiceStatus
        {
            NotCreated,
            Cancelled,
            Down,
            Dev,
            Test,
            TestCountdown,
            Release,
            FinishedRelease,
            Installing,
            WaitingOnDev,
            WaitingOnInstall,
            Installed,
            Live,
            LiveDebt,
            LiveDebtSub,
            LiveProfit,
            LiveProfitSub,
            Undo,
            StopTracking
        }

        enum ProfitStatus
        {
            None,
            InDebt,
            InProfit
        }



        Dictionary<string, TimeLog<ServiceStatus>> serviceInstallNameToTimeToStatus;
        Dictionary<string, TimeLog<ProfitStatus>> serviceInstallNameToTimeToProfitStatus;
        readonly List<string> subOptimalServices = new List<string>();
        Dictionary<string, string> completedInstallNameToNsName;

        readonly Dictionary<string, List<string>> serviceCommonNameToServices = new Dictionary<string, List<string>>();
        readonly Dictionary<string, string> serviceInstallNameToCommonName = new Dictionary<string, string>();
        readonly Dictionary<string, string> serviceCommonNameToShortName = new Dictionary<string, string>();

        readonly Dictionary<string, Node> existingServicesNameToNode = new Dictionary<string, Node>();
        readonly Dictionary<string, List<Node>> existingServicesNameToConnections = new Dictionary<string, List<Node>>();

        readonly Dictionary<string, string> demandInstallNameToCommonName = new Dictionary<string, string>();
        readonly Dictionary<string, string> demandInstallNameToService = new Dictionary<string, string>();

        readonly List<string> demandsAdded = new List<string>();

        readonly Dictionary<string, TimeLog<ServiceStatus>> demandInstallNameToTimeToStatus =
            new Dictionary<string, TimeLog<ServiceStatus>>();

        readonly Dictionary<string, TimeLog<ServiceStatus>> demandsDeployedToTimeToStatus =
            new Dictionary<string, TimeLog<ServiceStatus>>();

        string businessName;

        readonly List<string> activeOptionalDemands = new List<string>();

        readonly double roundDuration;

        public NewServicesReport (NetworkProgressionGameFile gameFile, int round)
        {
            this.gameFile = gameFile;
            this.round = round;

            model = gameFile.GetNetworkModel(round);

            roundDuration = model.GetNamedNode("CurrentTime").GetIntAttribute("round_duration", 0);
        }
        
        
        public string BuildReport (string business, bool includeNewServices, bool includeDemands, bool includeRegionIcons = true)
        {
            businessName = business;
            
            var xml = BasicXmlDocument.Create();
            var root = xml.AppendNewChild("timechart");

            // Build the time axis
            var timeline = xml.AppendNewChild(root, "timeline");
            BasicXmlDocument.AppendAttribute(timeline, "start", 0);
            BasicXmlDocument.AppendAttribute(timeline, "end", roundDuration / 60);
            BasicXmlDocument.AppendAttribute(timeline, "interval", 1);
            var timeLegend = SkinningDefs.TheInstance.GetData("gantt_chart_xaxis", "Minute");
            BasicXmlDocument.AppendAttribute(timeline, "legend", timeLegend);
            BasicXmlDocument.AppendAttribute(timeline, "title_fore_colour", SkinningDefs.TheInstance.GetColorData("gantt_chart_timeline_title_fore_colour"));
            BasicXmlDocument.AppendAttribute(timeline, "title_back_colour", SkinningDefs.TheInstance.GetColorData("report_titles_back_colour"));
            BasicXmlDocument.AppendAttribute(timeline, "minutes_fore_colour", SkinningDefs.TheInstance.GetColorData("gantt_chart_timeline_minute_fore_colour"));
            BasicXmlDocument.AppendAttribute(timeline, "minutes_back_colour", SkinningDefs.TheInstance.GetColorData("report_titles_back_colour"));
            BasicXmlDocument.AppendAttribute(timeline, "use_timeline_font", true);
            BasicXmlDocument.AppendAttribute(timeline, "font_size", 10);
            BasicXmlDocument.AppendAttribute(timeline, "should_draw_markings", false);

            // Get a list of the business names to be referenced to later.
            var businessNames = new List<string>();

            foreach (Node businessNode in model.GetNodesWithAttributeValue("type", "BU"))
            {
                businessNames.Add(businessNode.GetAttribute("name"));
            }

            businessNames.Sort();

            var regionsNode = xml.AppendNewChild(root, "regions");
            foreach (var bizName in businessNames)
            {
                var regionNode = xml.AppendNewChild(regionsNode, "region");
                BasicXmlDocument.AppendAttribute(regionNode, "name", bizName);
                BasicXmlDocument.AppendAttribute(regionNode, "type", "region");
            }

            foreach (Node service in model.GetNamedNode("Business Services Group").GetChildrenOfType("biz_service"))
            {
                if (CONVERT.ParseInt(service.GetAttribute("gain_per_minute")) <= 0)
                {
                    continue;
                }

                if (service.GetBooleanAttribute("is_hidden_in_reports", false))
                {
                    continue;
                }

                var serviceName = service.GetAttribute("name");
                existingServicesNameToNode.Add(serviceName, service);
                var shortName = service.GetAttribute("shortdesc");
                serviceCommonNameToShortName.Add(serviceName, shortName);

                if (!existingServicesNameToConnections.ContainsKey(serviceName))
                {
                    existingServicesNameToConnections.Add(serviceName, new List<Node>());
                }

                foreach (Node connection in service.GetChildrenOfType("Connection"))
                {
                    var connectionName = connection.GetAttribute("to");
                    var connectedNode = model.GetNamedNode(connectionName);

                    existingServicesNameToConnections[serviceName].Add(connectedNode);
                }
            }


            // Build a list of services.
            serviceInstallNameToTimeToStatus = new Dictionary<string, TimeLog<ServiceStatus>>();
            serviceInstallNameToTimeToProfitStatus = new Dictionary<string, TimeLog<ProfitStatus>>();
            completedInstallNameToNsName = new Dictionary<string, string>();

            var logReader =
                new BasicIncidentLogReader(gameFile.GetRoundFile(round, "NetworkIncidents.log",
                    GameFile.GamePhase.OPERATIONS));
            
            logReader.WatchCreatedNodes("BeginNewServicesInstall", logReader_BeginInstallCreateNodes);
            logReader.WatchCreatedNodes(CONVERT.Format("Round {0} Completed New Services", round), logReader_CompletedServicesCreatedNodes);
            

            logReader.Run();

            // The order in which the profit log entries and the begin install entries are 
            // non-deterministic so they're just logged in the logreader, and the time to
            // profit is 'calculated' here.

            foreach (var serviceInstallName in serviceInstallNameToTimeToStatus.Keys)
            {
                if (serviceInstallNameToTimeToProfitStatus.ContainsKey(serviceInstallName))
                {
                    var profitTimes = new List<double>(serviceInstallNameToTimeToProfitStatus[serviceInstallName].Times);

                    foreach (var time in profitTimes)
                    {
                        var profitStatus = serviceInstallNameToTimeToProfitStatus[serviceInstallName][time];
                        
                        if (profitStatus == ProfitStatus.InDebt)
                        {
                            if (subOptimalServices.Contains(serviceInstallName))
                            {
                                var firstLiveDebtTime =
                                    serviceInstallNameToTimeToStatus[serviceInstallName].
                                    GetFirstTimeOfValue(ServiceStatus.LiveDebt);

                                if (firstLiveDebtTime.HasValue)
                                {
                                    serviceInstallNameToTimeToStatus[serviceInstallName].RemoveAfter(firstLiveDebtTime.Value);
                                    serviceInstallNameToTimeToStatus[serviceInstallName].Add(firstLiveDebtTime.Value,
                                    ServiceStatus.LiveDebtSub);
                                }
                                
                            }
                        }
                        else
                        {
                            var status = (subOptimalServices.Contains(serviceInstallName)) ? ServiceStatus.LiveProfitSub : ServiceStatus.LiveProfit;
                            serviceInstallNameToTimeToStatus[serviceInstallName].Add(time, status);
                        }
                        
                    }
                }
                
            }


            if (includeDemands)
            {
                var demandLogReader =
                    new BasicIncidentLogReader(gameFile.GetRoundFile(round, "NetworkIncidents.log",
                        GameFile.GamePhase.OPERATIONS));

                foreach (var demandInstallName in demandInstallNameToCommonName.Keys)
                {
                    demandLogReader.WatchApplyAttributes(demandInstallName, demandLogReader_installDemandApplyAttributes);
                }

                foreach (var demandName in demandsAdded)
                {
                    demandLogReader.WatchApplyAttributes(demandName, demandLogReader_Demands_ApplyAttributes);
                }

                demandLogReader.Run();
            }


            // List of the new services currently being developed for this business
            var activeServices = new List<string>();

            var serviceStartTimes = new Dictionary<string, double>();

            foreach (var newService in serviceInstallNameToTimeToStatus.Keys)
            {
                if (serviceInstallNameToTimeToStatus[newService].Count > 0)
                {
                    activeServices.Add(newService);
                    serviceStartTimes[newService] = serviceInstallNameToTimeToStatus[newService].FirstTime;
                }
            }

            var allServicesToDraw = new List<string>(activeServices);

            if (includeDemands)
            {
                allServicesToDraw.AddRange(demandInstallNameToTimeToStatus.Keys);
            }

            allServicesToDraw.Sort(new NewServicesComparer(model, serviceStartTimes));
            
            // Now build the chart proper
            var sections = xml.AppendNewChild(root, "sections");
            BasicXmlDocument.AppendAttribute(sections, "use_gradient", false);
            

            var rowCount = 0;
            foreach (var serviceName in allServicesToDraw)
            {
                var isNewService = true;

                var displayName = "";

                var commonServiceName = "";
                if (serviceInstallNameToCommonName.ContainsKey(serviceName))
                {
                    commonServiceName = serviceInstallNameToCommonName[serviceName];
                    displayName = serviceCommonNameToShortName.ContainsKey(commonServiceName) ? serviceCommonNameToShortName[commonServiceName] : commonServiceName;
                }
                else if (includeDemands && demandInstallNameToTimeToStatus.ContainsKey(serviceName))
                {
                    isNewService = false;
                    commonServiceName = demandInstallNameToService[serviceName];
                    if (serviceCommonNameToShortName.ContainsKey(commonServiceName))
                    {
                        displayName = "Demand " + serviceCommonNameToShortName[commonServiceName];
                    }
                    else
                    {
                        displayName = commonServiceName;
                    }
                }

                // If there are no statuses to draw (likely due to an Undo)
                // then skip the rest
                if (isNewService && serviceInstallNameToTimeToStatus[serviceName].Count == 0)
                {
                    continue;
                }

                var section = xml.AppendNewChild(sections, "section");
                var rowForeColour = Color.Black;
                // Alternate the background colour for the row.
                Color[] rowBackColours =
                {
                    SkinningDefs.TheInstance.GetColorData("gantt_chart_row_back_colour_one", Color.White),
                    SkinningDefs.TheInstance.GetColorData("gantt_chart_row_back_colour_two", Color.GhostWhite)
                };
                

                BasicXmlDocument.AppendAttribute(section, "row_forecolour", rowForeColour);
                BasicXmlDocument.AppendAttribute(section, "row_backcolour", rowBackColours[rowCount % 2]);
                rowCount++;
                
                BasicXmlDocument.AppendAttribute(section, "legend", displayName);
                BasicXmlDocument.AppendAttribute(section, "header_width", 250);

                var regionStr = "-- -- -- --";
                var regionCodesStr = "";

                if (includeRegionIcons)
                {
                    var regions = new StringBuilder();
                    var regionsCode = new StringBuilder();

                    var businessesUsingService = new List<string>();

                    if (existingServicesNameToConnections.ContainsKey(commonServiceName))
                    {
                        foreach (var connectedNode in existingServicesNameToConnections[commonServiceName])
                        {
                            var parentBusiness = connectedNode.Parent.GetAttribute("name");
                            if (!businessesUsingService.Contains(parentBusiness))
                            {
                                businessesUsingService.Add(parentBusiness);
                            }

                        }

                    }

                    // Iterate through the business names. If it is using the service then
                    // add it to the section legend region.
                    foreach (var bizName in businessNames)
                    {
                        if (businessesUsingService.Contains(bizName))
                        {
                            regions.Append(bizName.Substring(2, 2) + " ");
                            // Horrible necessity to hardcode this.
                            switch (bizName.ToLower())
                            {
                                case "bu 1":
                                    regionsCode.Append("s");
                                    break;
                                case "bu 2":
                                    regionsCode.Append("f");
                                    break;
                                case "bu 3":
                                    regionsCode.Append("o");
                                    break;
                                case "bu 4":
                                    regionsCode.Append("m");
                                    break;
                            }
                        }
                        else
                        {
                            regions.Append("-- ");
                        }
                    }

                    regionStr = regions.ToString();
                    regionCodesStr = regionsCode.ToString();

                   
                }
                

                var row = xml.AppendNewChild(section, "row");
                BasicXmlDocument.AppendAttribute(row, "legend", regionStr);
                BasicXmlDocument.AppendAttribute(row, "regions_code", regionCodesStr);
                BasicXmlDocument.AppendAttribute(row, "header_width", 50);

                var times = isNewService ? new List<double>(serviceInstallNameToTimeToStatus[serviceName].Times) : 
                    new List<double>(demandInstallNameToTimeToStatus[serviceName].Times);

                double lastKnownTime = model.GetNamedNode("CurrentTime").GetIntAttribute("seconds", 0);

                

                // Now emit the blocks
                foreach (var time in times)
                {
                    var end = lastKnownTime;

                    if (times.IndexOf(time) < (times.Count - 1))
                    {
                        end = times[times.IndexOf(time) + 1];
                    }

                    var status = isNewService ? serviceInstallNameToTimeToStatus[serviceName].GetLastValueOnOrBefore(time) : 
                        demandInstallNameToTimeToStatus[serviceName].GetLastValueOnOrBefore(time);

                    var startTime = (float) (time / 60.0f);
                    var endTime = (float) (end / 60.0f);
                    switch (status)
                    {
                        case ServiceStatus.Dev:
                        case ServiceStatus.Test:
                        case ServiceStatus.Release:
                        case ServiceStatus.FinishedRelease:
                        case ServiceStatus.Installing:
                        case ServiceStatus.Live:
                        case ServiceStatus.LiveProfit:
                        case ServiceStatus.LiveDebt:

                            XmlBlock.AppendBlockChildToElement(row, new BlockProperties
                            {
                                Start = startTime,
                                End = endTime,
                                Legend = GetStatusAsLegend(status),
                                Colour = GetColourForStatus(status)
                            });

                            break;

                        case ServiceStatus.LiveDebtSub:
                        case ServiceStatus.LiveProfitSub:
                        case ServiceStatus.Cancelled:
                        case ServiceStatus.TestCountdown:

                            XmlBlock.AppendBlockChildToElement(row, new BlockProperties
                            {
                                Start = startTime,
                                End = endTime,
                                Legend = GetStatusAsLegend(status),
                                HatchFillProperties = GetHatchPropertiesFromStatus(status)
                            });

                            break;
                    }

                    
                }
            }
            
            var reportFile = gameFile.GetRoundFile(gameFile.LastRoundPlayed, "NewServicesReport.xml",
                GameFile.GamePhase.OPERATIONS);
            xml.Save(reportFile);

            return reportFile;
        }
        
        static ServiceStatus GetStatusFromString(string status)
        {
            switch(status)
            {
                case ServiceStatusName.Dev:
                    return ServiceStatus.Dev;

                case ServiceStatusName.Test:
	                return ServiceStatus.Test;
                case ServiceStatusName.TestDelay:
	                return ServiceStatus.TestCountdown;
                
                case ServiceStatusName.Release:	                
                case "finishedrelease":
	                return ServiceStatus.Release;
                case ServiceStatusName.Installing:
	                return ServiceStatus.Installing;

                case ServiceStatusName.Live:
	                return ServiceStatus.Live;

                case ServiceStatusName.Undo:
	                return ServiceStatus.Undo;
                case ServiceStatusName.Cancelled:
	                return ServiceStatus.Cancelled;

                default:
	                return ServiceStatus.Down;
            }
        }

        static string GetStatusAsLegend(ServiceStatus status)
        {
            switch (status)
            {
                case ServiceStatus.Down:
                    return "Down";
                case ServiceStatus.LiveDebt:
                    return "Live (Debt)";
                case ServiceStatus.LiveDebtSub:
                    return "Live (Debt) Suboptimal";
                case ServiceStatus.LiveProfit:
                    return "Live (Profit)";
                case ServiceStatus.LiveProfitSub:
                    return "Live (Profit) Suboptimal";
                case ServiceStatus.NotCreated:
                    return "Not Created";
                case ServiceStatus.Dev:
                case ServiceStatus.WaitingOnDev:
                    return "In Development";
                case ServiceStatus.Installing:
                case ServiceStatus.WaitingOnInstall:
                    return "Transitioning";
                case ServiceStatus.Test:
                    return "In Test";
                case ServiceStatus.TestCountdown:
                    return "Test Time";
                case ServiceStatus.Release:
                case ServiceStatus.FinishedRelease:
                    return "In Release";
                case ServiceStatus.Live:
                    return "Live";
                case ServiceStatus.Cancelled:
                    return "Cancelled";

                default:
                    return "";
            }
        }

        

        static string GetStatusAsSkinName(ServiceStatus status)
        {
            // TODO: Have only done this for a subset 
            // as they were the ones needed atm
            switch (status)
            {
                case ServiceStatus.Dev:
                    return "dev";
                case ServiceStatus.Test:
                    return "test";
                case ServiceStatus.Release:
                    return "release";
                case ServiceStatus.LiveDebt:
                    return "live_debt";
                case ServiceStatus.LiveProfit:
                    return "live_profit";
                case ServiceStatus.Live:
                    return "live";
                case ServiceStatus.Cancelled:
                    return "cancelled";
                case ServiceStatus.TestCountdown:
                    return "test_countdown";
                case ServiceStatus.LiveDebtSub:
                    return "live_debt_sub";
                case ServiceStatus.LiveProfitSub:
                    return "live_profit_sub";
                default:
                    return "blank";
            }
        }

        static Color GetColourForStatus(ServiceStatus status)
        {
            var skinName = GetStatusAsSkinName(status);

            return SkinningDefs.TheInstance.GetColorData($"{skinName}_colour", Color.Black);
        }

        static HatchFillProperties GetHatchPropertiesFromStatus(ServiceStatus status)
        {
            var statusName = GetStatusAsSkinName(status);
            return new HatchFillProperties
            {
                Angle = SkinningDefs.TheInstance.GetIntData($"hatch_{statusName}_angle", 0),
                LineWidth = SkinningDefs.TheInstance.GetFloatData($"hatch_{statusName}_line_width", 1f),
                LineColour = SkinningDefs.TheInstance.GetColorData($"hatch_{statusName}_line_colour", Color.Black),
                AltLineWidth = SkinningDefs.TheInstance.GetFloatData($"hatch_{statusName}_alt_line_width", 1f),
                AltLineColour = SkinningDefs.TheInstance.GetColorData($"hatch_{statusName}_alt_line_colour", Color.HotPink)
            };

        }

        void logReader_ServiceApplyAttributes(object sender, string key, string line, double time)
        {
            var name = BasicIncidentLogReader.ExtractValue(line, "i_name");

            var isAutoInstalled = BasicIncidentLogReader.ExtractBoolValue(line, "is_auto_installed");

            if (isAutoInstalled.HasValue && isAutoInstalled.Value)
            {
                // If this value is set then it means an already started
                // service is being auto installed. All statuses (statusi?)
                // after this time should be removed and no further ones logged.
                // (May add a function to BasicIncidentLogReader to have it stop
                // tracking keys)
                serviceInstallNameToTimeToStatus[name].RemoveAfter(time);
                serviceInstallNameToTimeToStatus[name].Add(time, ServiceStatus.StopTracking);
            }

            // If the last value before the current time is StopTracking 
            // then don't log anything new.
            if (serviceInstallNameToTimeToStatus[name].GetLastValueOnOrBefore(time) == ServiceStatus.StopTracking)
            {
                return;
            }

            var isOptimal = BasicIncidentLogReader.ExtractBoolValue(line, "optimal");

            var statusStr = BasicIncidentLogReader.ExtractValue(line, "status");

            // Sanity checks

            if (!serviceInstallNameToTimeToStatus.ContainsKey(name))
            {
                // Service isn't being tracked.
                return;
            }

            if (!string.IsNullOrEmpty(statusStr))
            {
                switch(statusStr)
                {
                    case ServiceStatusName.Undo:
                        // Service has been undone by the facilitator.
                        // Remove any states before this time.
                        serviceInstallNameToTimeToStatus[name].RemoveUntil(time);
                        break;
                    case ServiceStatusName.Cancelled:
                        var cancelledStatus = ServiceStatus.Cancelled;

                        if (serviceInstallNameToTimeToStatus[name].GetLastValueOnOrBefore(time) != cancelledStatus)
                        {
                            serviceInstallNameToTimeToStatus[name].Add(time, cancelledStatus);
                            serviceInstallNameToTimeToStatus[name].Add(time + 60, ServiceStatus.Down);
                        }

                        if (subOptimalServices.Contains(name))
                        {
                            subOptimalServices.Remove(name);
                        }
                        break;
                    case ServiceStatusName.Live:
                        // This is only triggered once, so it's likely in debt. 
                        var liveStatus = ServiceStatus.LiveDebt;
                        
                        if (isOptimal.HasValue)
                        {
                            if (!isOptimal.Value)
                            {
                                liveStatus = ServiceStatus.LiveDebtSub;
                            }
                        }

                        if (serviceInstallNameToTimeToStatus[name].GetLastValueOnOrBefore(time) != liveStatus)
                        {
                            // To handle aborted services that are auto deployed at the end of the round
                            // This prevents the 5 seconds of release time being added when they go live.
                            if (time >= roundDuration)
                            {
                                serviceInstallNameToTimeToStatus[name].Add(time, liveStatus);
                                break;
                            }
                            serviceInstallNameToTimeToStatus[name].Add(time, ServiceStatus.Release);
                            const int releaseDelay = 5;
                            serviceInstallNameToTimeToStatus[name].Add(time + releaseDelay, liveStatus);
                        }

                        break;
                    default :
                        
                        var status = GetStatusFromString(statusStr);
                        
                        if (serviceInstallNameToTimeToStatus[name].GetLastValueOnOrBefore(time) != status)
                        {
                            serviceInstallNameToTimeToStatus[name].Add(time, status);
                        }

                        break;
                }
            }

            
            if (isOptimal.HasValue)
            {
                if (!isOptimal.Value)
                {
                    if (!subOptimalServices.Contains(name))
                    {
                        subOptimalServices.Add(name);
                    }
                }
            }
            
        }

        void logReader_BeginInstallCreateNodes(object sender, string key, string line, double time)
        {
            var isAutoInstalled = BasicIncidentLogReader.ExtractBoolValue(line, "is_auto_installed");

            if (isAutoInstalled.HasValue && isAutoInstalled.Value)
            {
                // Services that are started for auto installation
                // don't need to have their progress tracked.
                return;
            }

            var stores = BasicIncidentLogReader.ExtractValue(line, "stores");

            var storeId = businessName.Substring(businessName.Length - 1);

            // Either this service isn't for the current store or we're not
            // currently looking at all businesses so don't do anything else
            // with this service
            if (!stores.Contains(storeId) && !businessName.Equals(SkinningDefs.TheInstance.GetData("allbiz")))
            {
                return;
            }
            
            var commonName = BasicIncidentLogReader.ExtractValue(line, "biz_service_function");
            if (!serviceCommonNameToServices.ContainsKey(commonName))
            {
                serviceCommonNameToServices.Add(commonName, new List<string>());
            }

            var name = BasicIncidentLogReader.ExtractValue(line, "name");

            // Services can be cancelled and restarted, so to prevent the
            // same service being added twice (or more) do this check
            if (!serviceCommonNameToServices[commonName].Contains(name))
            {
                serviceCommonNameToServices[commonName].Add(name);
            }
            serviceInstallNameToCommonName[name] = commonName;
            var shortName = BasicIncidentLogReader.ExtractValue(line, "shortdesc");

            Debug.Assert(!string.IsNullOrEmpty(shortName), "Missing short name");

            serviceCommonNameToShortName[commonName] = shortName;
            // A service could be added again due to it being cancelled then
            // started again.
            if (!serviceInstallNameToTimeToStatus.ContainsKey(name))
            {
                serviceInstallNameToTimeToStatus.Add(name, new TimeLog<ServiceStatus>());
            }

            var statusStr = BasicIncidentLogReader.ExtractValue(line, "status");

            Debug.Assert(!string.IsNullOrEmpty(statusStr), "Status shouldn't be empty");

            var status = GetStatusFromString(statusStr);
            // Should always be dev as it's just started
            Debug.Assert(status == ServiceStatus.Dev);

            // Cancelled services put in a minute block from when they were cancelled.
            // On the chance this cancelled service was restarted within that minute
            // then the new status should overwrite the cancelled block.
            if (serviceInstallNameToTimeToStatus[name].TryGetFirstTimeOnOrAfter(time).HasValue)
            {
                serviceInstallNameToTimeToStatus[name].RemoveAfter(time);
            }

            serviceInstallNameToTimeToStatus[name].Add(time, status);

            var logReader = (BasicIncidentLogReader)sender;

            logReader.WatchApplyAttributes(name, logReader_ServiceApplyAttributes);

        }


        void logReader_CompletedServices_ApplyAttributes (object sender, string key, string line, double time)
        {
            var name = BasicIncidentLogReader.ExtractValue(line, "i_name");

            var revString = BasicIncidentLogReader.ExtractValue(line, "profit");

            var revenue = CONVERT.ParseInt(revString);
            var installName = completedInstallNameToNsName[name];
            if (!serviceInstallNameToTimeToProfitStatus.ContainsKey(installName))
            {
                serviceInstallNameToTimeToProfitStatus[installName] = new TimeLog<ProfitStatus>();
            }

            var profitStatus = (revenue < 0) ? ProfitStatus.InDebt : ProfitStatus.InProfit;

            serviceInstallNameToTimeToProfitStatus[installName].Add(time, profitStatus);
            
        }

	    void logReader_CompletedServicesCreatedNodes(object sender, string key, string line, double time)
        {
            var isAutoInstalled = BasicIncidentLogReader.ExtractBoolValue(line, "is_auto_installed");

            if (isAutoInstalled.HasValue && isAutoInstalled.Value)
            {
                // Ignore auto installed services
                return;
            }

            var completedName = BasicIncidentLogReader.ExtractValue(line, "name");

            var installName = completedName.Replace("Completed", "Begin");
            completedInstallNameToNsName[completedName] = installName;

            var reader = (BasicIncidentLogReader)sender;
            reader.WatchApplyAttributes(completedName, logReader_CompletedServices_ApplyAttributes);

        }

        void logReader_BeginDemandInstall_CreateNodes(object sender, string key, string line, double time)
        {
            var business = BasicIncidentLogReader.ExtractValue(line, "bu");
            if (!business.Equals(businessName) && !businessName.Equals(SkinningDefs.TheInstance.GetData("allbiz")))
            {
                return;
            }

            var installName = BasicIncidentLogReader.ExtractValue(line, "name");
            var commonName = installName.Replace("Begin ", "");

            demandInstallNameToCommonName.Add(installName, commonName);
            
            if (!demandInstallNameToService.ContainsKey(installName))
            {
                var serviceName = BasicIncidentLogReader.ExtractValue(line, "serviceimpacted");
                demandInstallNameToService.Add(installName, serviceName);
            }
            
        }
        
        string GetDemandInstallName(string demandName)
        {
            var mbuOnwards = demandName.Substring(demandName.IndexOf("BU"));
            var demandId = demandName.Substring(0, 9);

            return "Begin " + demandId + mbuOnwards;
        }
        
        void logReader_Demands_CreateNodes(object sender, string key, string line, double time)
        {
            var business = BasicIncidentLogReader.ExtractValue(line, "bu");
            if (!business.Equals(businessName))
            {
                return;
            }

            var optionalStr = BasicIncidentLogReader.ExtractValue(line, "optional");
            var instanceStr = BasicIncidentLogReader.ExtractValue(line, "instances");

            var name = BasicIncidentLogReader.ExtractValue(line, "name");

            if (optionalStr.Equals(Boolean.TrueString) || instanceStr.Equals("0"))
            {
                if (!activeOptionalDemands.Contains(name))
                {
                    return;
                }
            }
            
            var installName = GetDemandInstallName(name);

            demandsAdded.Add(name);

            var serviceName = BasicIncidentLogReader.ExtractValue(line, "serviceimpacted");
            demandInstallNameToService.Add(installName, serviceName);

            demandInstallNameToTimeToStatus.Add(installName, new TimeLog<ServiceStatus>());
            demandsDeployedToTimeToStatus.Add(installName, new TimeLog<ServiceStatus>());
            
        }
        

        void demandLogReader_installDemandApplyAttributes(object sender, string key, string line, double time)
        {
            var installName = BasicIncidentLogReader.ExtractValue(line, "i_name");
            
            if (!demandInstallNameToTimeToStatus.ContainsKey(installName))
            {
                demandInstallNameToTimeToStatus.Add(installName, new TimeLog<ServiceStatus>());
                demandsDeployedToTimeToStatus.Add(installName, new TimeLog<ServiceStatus>());
            }
            var installTime = BasicIncidentLogReader.ExtractValue(line, "installTimeLeft");
            if (!string.IsNullOrEmpty(installTime))
            {
                var timeRemaining = CONVERT.ParseInt(installTime);
                // Transition period has started, log it.
                if (timeRemaining == 60)
                {
                    demandInstallNameToTimeToStatus[installName].Add(time, ServiceStatus.WaitingOnInstall);
                }
                // Transition period has ended and demand is deployed.
                else if (timeRemaining <= -1)
                {
                    // If a time on or before the current time has been logged for the demand window starting
                    if (demandsDeployedToTimeToStatus[installName].TryGetLastTimeOnOrBefore(time) != null)
                    {
                        // Then log the last time logged for going live
                        time = demandsDeployedToTimeToStatus[installName].GetLastTimeOnOrBefore(time);
                        demandInstallNameToTimeToStatus[installName].Add(time, ServiceStatus.Live);
                        
                    }
                    else
                    {
                        // Otherwise, log the time as Installed.
                        demandInstallNameToTimeToStatus[installName].Add(time, ServiceStatus.Installed);
                    }
                }
            }
            
        }
        
        

        void demandLogReader_Demands_ApplyAttributes(object sender, string key, string line, double time)
        {
            var demandName = BasicIncidentLogReader.ExtractValue(line, "i_name");
            
            var installName = GetDemandInstallName(demandName);
            // Demand install has been started, so check to see if it has 
            // been met and that the demand window has started.
            if (demandInstallNameToTimeToStatus.ContainsKey(installName))
            {
                
                DemandsApplyAttributes(line, time, installName);
            }
        }
        

        void DemandsApplyAttributes(string line, double time, string installName)
        {
            var duration = BasicIncidentLogReader.ExtractValue(line, "duration");

            if (!string.IsNullOrEmpty(duration))
            {
                var timeRemaining = CONVERT.ParseInt(duration);

                // Demand window has started.
                if (timeRemaining >= 0)
                {
                    // If the last time logged on or before the current time's status
                    // is installed then log the current time as live
                    if (demandInstallNameToTimeToStatus[installName].GetLastValueOnOrBefore(time) ==
                        ServiceStatus.Installed)
                    {
                        demandInstallNameToTimeToStatus[installName].Add(time, ServiceStatus.Live);
                    }
                    // Otherwise, keep a track of the time.
                    else
                    {
                        demandsDeployedToTimeToStatus[installName].Add(time, ServiceStatus.Live);
                    }

                }
                // Demand window has ended.
                else if (timeRemaining == -1)
                {
                    demandInstallNameToTimeToStatus[installName].Add(time, ServiceStatus.Down);
                }

            }
        }

        string GetDemandNameFromOptionalDemand(string optionalDemand)
        {
            // Optional demands are in the format
            // "Optional Demand 5 MBU 2 Service Name"
            // and demands are in the format,
            // "Demand 5 Round 3 MBU 2 Service Name"
            // So, in order to get the matching Demand name
            // "Optional" needs to be removed, the round number
            // needs to be inserted before the MBU,
            // and the number after "MBU" needs to be the same
            // as the current business' number.
            
            var demandSB = new StringBuilder(optionalDemand.Replace("Optional ", ""));
            // Business name is "MBU #", so we need the 5th element, 4th index
            var mbuNumber = businessName[4];
            // The MBU number in the demand name is at position 13.
            demandSB[13] = mbuNumber;
            demandSB.Insert(demandSB.ToString().IndexOf("BU"), CONVERT.Format("Round {0} ", round));

            return demandSB.ToString();
        }
        
    }
}

