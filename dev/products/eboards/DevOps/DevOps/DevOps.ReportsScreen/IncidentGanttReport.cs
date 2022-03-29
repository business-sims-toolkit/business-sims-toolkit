using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Drawing;

using Charts;
using LibCore;
using CoreUtils;
using GameManagement;
using Network;
using Logging;
using ReportBuilder;

namespace DevOps.ReportsScreen
{

    /// <summary>
    /// Based off of ReportBuilder.OpsGanttReport but modified to be used
    /// with Charts.CloudTimeChart.
    /// </summary>

    public class IncidentGanttReport
    {
        class AffectedServicesComparer : IComparer<string>
        {
            readonly Dictionary<string, int> serviceNameToNumImpacted;
            readonly NodeTree model;

            public AffectedServicesComparer(Dictionary<string, int> servicesToImpact, NodeTree model)
            {
                serviceNameToNumImpacted = servicesToImpact;
                this.model = model;
            }

            public int Compare(string lhs, string rhs)
            {

                var leftBizServiceName = model.GetNamedNode(lhs).GetAttribute("biz_service_function");
                var rightBizServiceName = model.GetNamedNode(rhs).GetAttribute("biz_service_function");

                if (serviceNameToNumImpacted.ContainsKey(leftBizServiceName) && serviceNameToNumImpacted.ContainsKey(rightBizServiceName))
                {
                    var leftPriority = serviceNameToNumImpacted[leftBizServiceName];
                    var rightPriority = serviceNameToNumImpacted[rightBizServiceName];

                    if (leftPriority.CompareTo(rightPriority) != 0)
                    {
                        return rightPriority.CompareTo(leftPriority);
                    }
                }

                return leftBizServiceName.CompareTo(rightBizServiceName);
            }
        }

        NetworkProgressionGameFile gameFile;
        
        int currentRound;
        
        NodeTree model;

        DevOpsRoundScores roundScores;

        double lastKnownTimeInGame = 0.0;
        readonly int roundMins = 25;

        readonly Hashtable mappings = new Hashtable();
        readonly Dictionary<string, EventStream> bizServiceStatusStreams = new Dictionary<string, EventStream>();
        
        readonly ArrayList bizServices = new ArrayList();


        
        // This will hold all of the biz services regardless
        // whether it's relevant to the current business
        Dictionary<string, Node> bizServiceNameToNode;
        // 
        Dictionary<string, List<Node>> bizServiceNameToBizUsers;

        readonly Hashtable bizToTracker = new Hashtable();
        readonly Hashtable serverNameToTracker = new Hashtable();
        readonly Hashtable appNameToTracker = new Hashtable();

        // Are these still used?? TODO (GC)
        readonly ArrayList lostRevenues = new ArrayList();

        readonly List<Dictionary<int, int>> lostRevenuePerStore = new List<Dictionary<int, int>>();
        // ****

        readonly Dictionary<string, int> bizServiceToNumImpactedStoreChannels;

        readonly string serviceStartsWith;
        readonly bool isBsu;

        readonly bool hideWarningsIfAwtOff = true;

        
        bool awtActive = false;
        readonly bool showOnlyDownedServices;

        public IncidentGanttReport (string serviceStartsWith, bool showOnlyDownServices = false)
        {
            this.serviceStartsWith = serviceStartsWith;
            isBsu = !this.serviceStartsWith.Equals(SkinningDefs.TheInstance.GetData("allbiz"));
            this.showOnlyDownedServices = showOnlyDownServices;


            bizServiceToNumImpactedStoreChannels = new Dictionary<string, int>();
        }


        public string BuildReport(NetworkProgressionGameFile gameFile, int round, Boolean revenueHoverRequired, DevOpsRoundScores roundScores)
        {
            
            this.gameFile = gameFile;
            currentRound = round;
            this.roundScores = roundScores;

            // Determine which round's file needs to be loaded. If it's a sales game then load up round 5,
            // If the round passed in isn't the same as the game file's current round, then load up the 
            // file for @round, else load up the file for the game file's current round.
            var roundFile = gameFile.IsSalesGame
                ? 3 : gameFile.CurrentRound != round ? round : gameFile.CurrentRound;
            
            model = gameFile.GetNetworkModel(roundFile);

            bizServiceNameToNode = new Dictionary<string, Node>();
            bizServiceNameToBizUsers = new Dictionary<string, List<Node>>();
            var mbuServices = new List<string>();
            // Load all of the biz services and their connected biz service users.
            foreach (Node service in model.GetNodesWithAttributeValue("type", "biz_service"))
            {
                // Incident biz services have -1 info per trans. Don't include the ones
                // that have anything else.
                //if (CONVERT.ParseInt(service.GetAttribute("gain_per_minute")) > 0)
                //{
                //    continue;
                //}

                var serviceName = service.GetAttribute("name");
                if (!bizServiceNameToNode.ContainsKey(serviceName))
                {
                    bizServiceNameToNode.Add(serviceName, service);
                }
                else
                {
                    throw new Exception("Duplicate name??");
                }

                if (!bizServiceNameToBizUsers.ContainsKey(serviceName))
                {
                    bizServiceNameToBizUsers.Add(serviceName, new List<Node>());
                }
                else
                {
                    throw new Exception("Duplicate name??");
                }
                var bsuNames = new List<string>();
                foreach (Node connection in service.GetChildrenOfType("Connection"))
                {
                    var bizUserName = connection.GetAttribute("to");
                    
                    if (bsuNames.Contains(bizUserName))
                    {
                        throw new Exception("BSU already added??");
                    }
                    bsuNames.Add(bizUserName);

                    var bizUser = model.GetNamedNode(bizUserName);
                    bizServiceNameToBizUsers[serviceName].Add(bizUser);
                    if (bizUserName.StartsWith(serviceStartsWith))
                    {
                        bizServiceNameToNode.Add(bizUserName, bizUser);
                    }
                }
                
            }

            foreach(var bizName in bizServiceNameToNode.Keys)
            {
                if (isBsu)
                {
                    if (bizName.StartsWith(serviceStartsWith))
                    {
                        AddBusinessService(bizName, "");
                    }

                }
                else // Looking at all businesses (MBUs)
                {
                    if (!bizName.StartsWith(serviceStartsWith))
                    {
                        AddBusinessService(bizName, "");
                    }
                }
            }
            

            

            // 22-08-2007 : Don't watch businesses that were retired at the end
            // of the last round.
            NodeTree pNetworkModel = null;

            if (round >= 2)
            {
                // Not using transitions so get the network file for the previous round
                var xmlFilename = gameFile.GetNetworkFile(round - 1, GameFile.GamePhase.OPERATIONS/*TRANSITION*/);
                var file = new StreamReader(xmlFilename);
                var xmlData = file.ReadToEnd();
                file.Close();
                file = null;
                pNetworkModel = new NodeTree(xmlData);
            }
            

            // Set lost revenue array to be roundMins usually 25
            for (var i = 0; i <= roundMins; i++)
            {
                lostRevenues.Add("0");
            }
            var numStores = 4;
            for (var storeId = 0; storeId < numStores; storeId++)
            {
                lostRevenuePerStore.Add(new Dictionary<int, int>());

                lostRevenuePerStore[storeId].Add(0, 0);
            }

            // Pull the logfile to get data from.
            var logFile = gameFile.GetRoundFile(round, "NetworkIncidents.log", GameFile.GamePhase.OPERATIONS);

            var biLogReader = new BasicIncidentLogReader(logFile);
            foreach (string service in bizServices)
            {
                biLogReader.WatchApplyAttributes(service, BiLogReaderLineFound);

                var latchEdgeTracker = new LatchEdgeTracker();
                latchEdgeTracker.TrackBooleanAttribute("up", true);
                latchEdgeTracker.TrackBooleanAttribute("denial_of_service", false);
                latchEdgeTracker.TrackBooleanAttribute("security_flaw", false);
                latchEdgeTracker.TrackBooleanAttribute("compliance_incident", false);
                latchEdgeTracker.TrackBooleanAttribute("awaiting_saas_auto_restore", false);

                latchEdgeTracker.TrackBooleanAttribute("retired", false);

                latchEdgeTracker.TrackBooleanAttribute("slabreach", false);
                latchEdgeTracker.TrackBooleanAttribute("upByMirror", false);
                latchEdgeTracker.TrackStringAttribute("incident_id");
                latchEdgeTracker.TrackStringAttribute("users_down");
                latchEdgeTracker.TrackCounterAttribute("rebootingForSecs");
                latchEdgeTracker.TrackCounterAttribute("workingAround");
                bizToTracker.Add(service, latchEdgeTracker);
            }

            foreach (Node serverNode in model.GetNodesWithAttributeValue("type", "Server"))
            {
                var name = serverNode.GetAttribute("name");
                biLogReader.WatchApplyAttributes(name, BiLogReaderServerLineFound);

                var latchEdgeTracker = new LatchEdgeTracker();
                latchEdgeTracker.TrackStringAttribute("danger_level");
                serverNameToTracker.Add(name, latchEdgeTracker);
            }

            var apps = new ArrayList();
            apps.AddRange(model.GetNodesWithAttributeValue("type", "App"));
            apps.AddRange(model.GetNodesWithAttributeValue("type", "Database"));
            foreach (Node appNode in apps)
            {
                var name = appNode.GetAttribute("name");
                biLogReader.WatchApplyAttributes(name, BiLogReaderAppLineFound);

                if (appNameToTracker.ContainsKey(name) == false)
                {
                    var latchEdgeTracker = new LatchEdgeTracker();
                    latchEdgeTracker.TrackStringAttribute("danger_level");
                    appNameToTracker.Add(name, latchEdgeTracker);
                }
            }

            // Watch for costed events.
            biLogReader.WatchCreatedNodes("CostedEvents", BiLogReaderCostedEventFound);
            biLogReader.WatchApplyAttributes("Revenue", BiLogReaderRevenueFound);
            //biLogReader.WatchApplyAttributes("ApplicationsProcessed", BiLogReaderApplicationsProcessedFound);
            // TODO does nothing? (GC)
            WatchAdditionalItems(biLogReader);

            biLogReader.WatchApplyAttributes("AdvancedWarningTechnology", BiLogReaderAwtChanged);

            biLogReader.Run();

            // Before we continue any further we have to Merge Trackers that are upgraded services that should
            // be reported as one track.

            // If roundScores null, then we know this round has not been run, we just want an empty report
            if (roundScores != null)
            {
                if (gameFile.Version == 1)
                {
                    MergeTrackers();
                }

                ConvertTrackersToStreams();
            }


            return GenerateGanttReport();
        }

        protected string GenerateGanttReport()
        {
            var xml = BasicXmlDocument.Create();
            var root = xml.AppendNewChild("timechart");

            // Build the time axis
            var timeline = xml.AppendNewChild(root, "timeline");
            BasicXmlDocument.AppendAttribute(timeline, "start", 0);
            BasicXmlDocument.AppendAttribute(timeline, "end",
                model.GetNamedNode("CurrentTime").GetIntAttribute("round_duration", 0) / 60);
            BasicXmlDocument.AppendAttribute(timeline, "interval", 1);
            var timeLegend = SkinningDefs.TheInstance.GetData("gantt_chart_xaxis", "Minute");
            BasicXmlDocument.AppendAttribute(timeline, "legend", timeLegend);
            BasicXmlDocument.AppendAttribute(timeline, "title_fore_colour", SkinningDefs.TheInstance.GetColorData("gantt_chart_timeline_title_fore_colour"));
            BasicXmlDocument.AppendAttribute(timeline, "title_back_colour", SkinningDefs.TheInstance.GetColorData("report_titles_back_colour"));
            BasicXmlDocument.AppendAttribute(timeline, "minutes_fore_colour", SkinningDefs.TheInstance.GetColorData("gantt_chart_timeline_minute_fore_colour"));
            BasicXmlDocument.AppendAttribute(timeline, "minutes_back_colour", SkinningDefs.TheInstance.GetColorData("report_titles_back_colour"));
            BasicXmlDocument.AppendAttribute(timeline, "use_timeline_font", true);
            BasicXmlDocument.AppendAttribute(timeline, "font_style", "bold");
            BasicXmlDocument.AppendAttribute(timeline, "font_size", 10);
            BasicXmlDocument.AppendAttribute(timeline, "should_draw_markings", false);

            // Order the names alphabetically

            var serviceNames = new List<string>(bizServiceToNumImpactedStoreChannels.Keys);
            serviceNames.Sort(new AffectedServicesComparer(bizServiceToNumImpactedStoreChannels, model));
            
            var nameToDisplayName = new Dictionary<string, string>();
            foreach (var name in serviceNames)
            {
                var displayName = name;
                if (name.StartsWith(serviceStartsWith))
                {
                    displayName = displayName.Substring(serviceStartsWith.Length + 1);
                }

                nameToDisplayName.Add(name, displayName);
            }
            
            
            // Old version 1 game files (Gannt chart is much more complex and error prone)
            // Populate a hashtable with the name as the key and the short name plus its
            // availability as the value to be displayed in the left column of the CloudTimeChart

            var nameToLabel = new Dictionary<string, string>();

            foreach (var name in serviceNames)
            {
                var displayName = nameToDisplayName[name];

                var statusStreamName = isBsu ? serviceStartsWith + " " + name : name;

                if (!(showOnlyDownedServices && bizServiceStatusStreams[statusStreamName].events.Count == 0))
                {

                    if (bizToTracker.ContainsKey(statusStreamName))
                    {
                        var avail = "100";
                        if (roundScores != null)
                        {
                            avail = GetAvailability(statusStreamName);
                        }

                        nameToLabel[name] = Strings.RemoveHiddenText(displayName) + " " + avail;
                    }
                    else
                    {
                        var mappedService = GetActiveTrackerForMappedService(name);
                        if (!string.IsNullOrEmpty(mappedService))
                        {
                            var avail = "100";
                            if (roundScores != null)
                            {
                                avail = GetAvailability(name);
                            }
                            nameToLabel[name] = Strings.RemoveHiddenText(displayName) + " " + avail;
                        }
                        else
                        {
                            nameToLabel[name] = name + " 0%";
                        }
                    }
                }
            }
            
            // Build the chart proper.
            var sections = xml.AppendNewChild(root, "sections");
            BasicXmlDocument.AppendAttribute(sections, "use_gradient", false);

            lastKnownTimeInGame = model.GetNamedNode("CurrentTime").GetIntAttribute("seconds", 0);
            var rowCount = 0;
            var rowForeColour = "0, 0, 0";
           // string [] rowBackColours = { "230, 230, 230", "210, 210, 210" };

            Color [] rowBackColours =
            {
                SkinningDefs.TheInstance.GetColorData("gantt_chart_row_back_colour_one", Color.White),
                SkinningDefs.TheInstance.GetColorData("gantt_chart_row_back_colour_two", Color.GhostWhite)
            };
            

            // Create the individual section blocks.
            foreach (var name in serviceNames)
            {
                var service = isBsu ? serviceStartsWith + " " + name : name;

                if (! (showOnlyDownedServices && bizServiceStatusStreams[service].events.Count == 0))
                {
                    if (bizServiceStatusStreams.ContainsKey(service))
                    {
                        XmlNode section = xml.AppendNewChild(sections, "section");

                        BasicXmlDocument.AppendAttribute(section, "row_forecolour", rowForeColour);
                        BasicXmlDocument.AppendAttribute(section, "row_backcolour", rowBackColours[rowCount % 2]);
                        rowCount++;
                        BasicXmlDocument.AppendAttribute(section, "legend", nameToLabel[name]);
                        BasicXmlDocument.AppendAttribute(section, "header_width", 250);

                        var row = xml.AppendNewChild(section, "row");
                        BasicXmlDocument.AppendAttribute(row, "legend", "-- -- -- --");
                        BasicXmlDocument.AppendAttribute(row, "regions_code", "");
                        BasicXmlDocument.AppendAttribute(row, "header_width", 50);

                        var eventStream = bizServiceStatusStreams[service];
                        
                        foreach (ServiceEvent serviceEvent in eventStream.events)
                        {
                            // Warnings can overlap with the subsequent failure, so truncate them.
                            if (serviceEvent.seType == ServiceEvent.eventType.WARNING)
                            {
                                if (eventStream.events.IndexOf(serviceEvent) < eventStream.events.Count - 1)
                                {
                                    var nextEvent =
                                        (ServiceEvent) eventStream.events[1 + eventStream.events.IndexOf(serviceEvent)];
                                    serviceEvent.secondsIntoGameEnds = Math.Min(serviceEvent.secondsIntoGameEnds,
                                        nextEvent.secondsIntoGameOccured);
                                }
                            }

                            if (serviceEvent.seType == ServiceEvent.eventType.WARNING && hideWarningsIfAwtOff && !awtActive)
                            {
                                continue;
                            }
                            
                            if (serviceEvent.secondsIntoGameOccured == serviceEvent.secondsIntoGameEnds)
                            {
                                continue;
                            }
                            
                            var startTime = (int)serviceEvent.secondsIntoGameOccured;

                            var length = (int)(lastKnownTimeInGame - startTime);
                            var reportedLength = length;
                            if (serviceEvent.secondsIntoGameEnds != 0)
                            {
                                var secondsGameEnd = (int) serviceEvent.secondsIntoGameEnds;
                                length = secondsGameEnd - startTime;
                            }

                            reportedLength = (int) serviceEvent.GetLength(lastKnownTimeInGame);

                            if (length == 0)
                            {
                                length = 1;
                            }
                            if (reportedLength == 0)
                            {
                                reportedLength = 1;
                            }

                            var endTime = startTime + length;
                            
                            var start = startTime / 60.0f;
                            var end = endTime / 60.0f;
                            switch (serviceEvent.seType)
                            {
                                case ServiceEvent.eventType.INCIDENT:
                                case ServiceEvent.eventType.WORKAROUND:
                                case ServiceEvent.eventType.SLABREACH:

                                    XmlBlock.AppendBlockChildToElement(row, new BlockProperties
                                    {
                                        Start = start,
                                        End = end,
                                        Legend = GetEventTypeAsLegend(serviceEvent.seType),
                                        Colour = GetColourForEventType(serviceEvent.seType)
                                    });

                                    break;

                                case ServiceEvent.eventType.WA_SLABREACH:

                                    XmlBlock.AppendBlockChildToElement(row, new BlockProperties
                                    {
                                        Start = start,
                                        End = end,
                                        Legend = GetEventTypeAsLegend(serviceEvent.seType),
                                        HatchFillProperties = GetHatchPropertiesForEventType(serviceEvent.seType)
                                    });

                                    break;
                            }

                            // Get minute from seconds
                            var min = startTime / 60;
                            var finalMin = endTime / 60;

                            while (min <= finalMin)
                            {
                                var minIndex = (min * 60);
                                var rev = 0;
                                if (isBsu)
                                {
                                    var storeId = CONVERT.ParseInt(serviceStartsWith.Substring(serviceStartsWith.Length - 2).Trim()) - 1;
                                    
                                    if (lostRevenuePerStore[storeId].ContainsKey(minIndex))
                                    {
                                        rev += lostRevenuePerStore[storeId][minIndex];
                                    }

                                }
                                else
                                {
                                    // Hardcoded as 4 stores just now
                                    for (var i = 0; i < 4; i++)
                                    {
                                        if (lostRevenuePerStore[i].ContainsKey(minIndex))
                                        {
                                            rev += lostRevenuePerStore[i][minIndex];
                                        }
                                    }

                                }

                                var mouseBlock = xml.AppendNewChild(row, "mouseover_block_bar");

                                BasicXmlDocument.AppendAttribute(mouseBlock, "start", min);
                                BasicXmlDocument.AppendAttribute(mouseBlock, "end", min + 1);
                                BasicXmlDocument.AppendAttribute(mouseBlock, "legend",
                                    CONVERT.Format("Minute: {0}\r\nRevenue Lost: {1}\r\nTime Down: {2}",
                                    min, CONVERT.ToPaddedCurrencyStrWithThousands(rev, 0), CONVERT.FormatTimeFourDigits(reportedLength)));
                                BasicXmlDocument.AppendAttribute(mouseBlock, "value", CONVERT.ToPaddedCurrencyStrWithThousands(rev, 0));
                                BasicXmlDocument.AppendAttribute(mouseBlock, "length", reportedLength);


                                min++;
                            }
                        }
                    }
                }
            }


            var reportFile = gameFile.GetRoundFile(gameFile.LastRoundPlayed,
                "IncidentGanttReport_" + serviceStartsWith + ".xml", GameFile.GamePhase.OPERATIONS);
            xml.Save(reportFile);

            return reportFile;

        }
        static string GetEventTypeAsLegend(ServiceEvent.eventType eventType)
        {
            switch (eventType)
            {
                case ServiceEvent.eventType.INCIDENT:
                    return "Incident";
                case ServiceEvent.eventType.WORKAROUND:
                    return "Workaround";
                case ServiceEvent.eventType.SLABREACH:
                    return "Incident (SLA Breached)";
                case ServiceEvent.eventType.WA_SLABREACH:
                    return "Workaround (SLA Breached)";
                // TODO
                default:
                    return "UNKNOWN";
            }
        }

        static string GetEventTypeAsSkinName(ServiceEvent.eventType eventType)
        {
            switch (eventType)
            {
                case ServiceEvent.eventType.WA_SLABREACH:
                    return "workaround_sla_breached";
                case ServiceEvent.eventType.SLABREACH:
                    return "incident_breached";
                case ServiceEvent.eventType.WORKAROUND:
                    return "workaround";
                case ServiceEvent.eventType.INCIDENT:
                    return "incident";
                default:
                    return "blank";
            }
        }

        static Color GetColourForEventType(ServiceEvent.eventType eventType)
        {
            return SkinningDefs.TheInstance.GetColorData($"{GetEventTypeAsSkinName(eventType)}_colour", Color.Black);
        }

        static HatchFillProperties GetHatchPropertiesForEventType(ServiceEvent.eventType eventType)
        {
            var eventSkinName = GetEventTypeAsSkinName(eventType);
            return new HatchFillProperties
            {
                Angle = SkinningDefs.TheInstance.GetIntData($"hatch_{eventSkinName}_angle", 0),
                LineWidth = SkinningDefs.TheInstance.GetFloatData($"hatch_{eventSkinName}_line_width", 1f),
                LineColour = SkinningDefs.TheInstance.GetColorData($"hatch_{eventSkinName}_line_colour", Color.Black),
                AltLineWidth = SkinningDefs.TheInstance.GetFloatData($"hatch_{eventSkinName}_alt_line_width", 1f),
                AltLineColour = SkinningDefs.TheInstance.GetColorData($"hatch_{eventSkinName}_alt_line_colour", Color.HotPink)
            };
        }


        protected string GetAvailability(string service)
        {
            var availability = 0.0;
            var up = true;
            var lastTime = 0.0;
            var downFor = 0.0;

            //bool isBsu = (serviceStartsWith != SkinningDefs.TheInstance.GetData("allbiz"));

            var numUsers = 4; /*double*/
            if (bizServiceNameToBizUsers.ContainsKey(service))
            {
                numUsers = bizServiceNameToBizUsers[service].Count;

                if (numUsers == 0)
                {
                    throw new Exception("Count should not be zero.");
                }
            }
           
            // Prevent divide by zero.
            if (numUsers == 0)
                numUsers = 4;

            // : Fix for 4064 (availability values differ for the same service
            // between the all and single store modes).
            if (isBsu)
            {
                numUsers = 1;
            }

            var numDown = 0.0;
            char [] comma = { ',' };

            if (bizToTracker.ContainsKey(service))
            {
                var let = (LatchEdgeTracker) bizToTracker[service];

                if (let.Count == 0)
                    return "100%";

                for (var i = 0; i < let.Count; i++)
                {
                    var latchedEvent = let.GetLatchedEvent(i);
                    var lengthOfLastEvent = latchedEvent.time - lastTime;

                    var eventStr = latchedEvent.GetStringEvent("users_down");

                    if (!up)
                    {
                        downFor += lengthOfLastEvent * numDown;
                    }

                    up = latchedEvent.GetBoolEventActive("up");

                    // : Wrongly calculated numDown as 1 when users_down was blank.
                    if (isBsu)
                    {
                        numDown = up ? 0 : 1;
                    }
                    else
                    {
                        numDown = eventStr.Length == 0 ? 0 : eventStr.Split(',').Length;
                    }

                    lastTime = latchedEvent.time;
                }
            }
            else
            { // ??? TODO (GC) never used
                var s = service;
            }

            var lengthOfFinalEvent = roundScores.FinalTime - lastTime;

            if (!up)
            {
                downFor += lengthOfFinalEvent * numDown;
            }

            availability = (roundScores.FinalTime * numUsers - downFor) / numUsers;

            // : Fix so it doesn't flash up NaN before playing any data are available.
            if (roundScores.FinalTime <= 0.0)
            {
                availability = 1;
            }
            else
            {
                availability = availability / roundScores.FinalTime;
            }

            availability = Math.Round(availability * 100.0, 0);

            return CONVERT.ToStr(availability) + "%";
        }

        protected void AddBusinessService (string service, string desc)
        {
            bizServices.Add(service);

            // If we are a version 2 or later game file then just add the stream tracker here.
            if (gameFile.Version > 1)
            {
                if (!bizServiceStatusStreams.ContainsKey(service))
                {
                    bizServiceStatusStreams.Add(service, new EventStream());
                }
            }    
        
        }

        protected void ConvertTrackersToStreams()
        {
            // Before the incidents, do the AWT warnings for the servers. 
            // Warnings need to come first because they will typically be
            // followed, and superceded, by incidents.
            if (SkinningDefs.TheInstance.GetIntData("gantt_show_warnings", 0) == 1
                && currentRound >= SkinningDefs.TheInstance.GetIntData("gantt_warnings_start_round", 0))
            {
                foreach (string serverName in serverNameToTracker.Keys)
                {
                    var server = gameFile.NetworkModel.GetNamedNode(serverName);

                    if (server == null)
                    {
                        continue;
                    }

                    var let = (LatchEdgeTracker)serverNameToTracker[serverName];

                    for (var i = 0; i < let.Count; i++)
                    {
                        var latchedEvent = let.GetLatchedEvent(i);
                        var dangerLevel = latchedEvent.GetStringEvent("danger_level");
                        if (dangerLevel != "")
                        {
                            var level = CONVERT.ParseInt(dangerLevel);

                            // This server contains apps.
                            foreach (Node app in server.getChildren())
                            {
                                DoAppWarningEvent(app, latchedEvent, level >= 33);
                            }
                        }
                    }
                }

                // And for the apps.
                foreach (string appName in appNameToTracker.Keys)
                {
                    var app = gameFile.NetworkModel.GetNamedNode(appName);
                    var latchedEdgeTracker = (LatchEdgeTracker) appNameToTracker[appName];

                    for (var i = 0; i < latchedEdgeTracker.Count; i++)
                    {
                        var latchedEvent = latchedEdgeTracker.GetLatchedEvent(i);
                        var dangerLevel = latchedEvent.GetStringEvent("danger_level");
                        if (dangerLevel != "")
                        {
                            var level = CONVERT.ParseInt(dangerLevel);

                            DoAppWarningEvent(app, latchedEvent, level >= 33);
                        }
                    }

                }

            }

            // Then do the proper incidents
            foreach (string biz in bizToTracker.Keys)
            {
                var latchEdgeTracker = (LatchEdgeTracker) bizToTracker[biz];

                var count = latchEdgeTracker.Count;
                for (var i = 0; i < count; i++)
                {
                    var latchedEvent = latchEdgeTracker.GetLatchedEvent(i);
                    // Pull the incident ID
                    var incidentId = latchedEvent.GetStringEvent("incident_id");

                    
                    // Always show rebooting first
                    if (latchedEvent.GetCounterEventActive("rebootingForSecs"))
                    {
                        AddEvent(biz, latchedEvent.time, false, /*reason*/string.Empty, ServiceEvent.eventType.INSTALL);

                        var usersDown = latchedEvent.GetStringEvent("users_down");
                        if (!string.IsNullOrEmpty(usersDown))
                        {
                            SetUsersAffected(usersDown, biz);
                        }

                    }
                    else if (latchedEvent.GetCounterEventActive("workingAround"))
                    {
                        // New mode that shows SLA breaches in workaround.
                        if (latchedEvent.GetBoolEventActive("slabreach"))
                        {
                            AddEvent(biz, latchedEvent.time, false, /*reason*/string.Empty,
                                ServiceEvent.eventType.WA_SLABREACH);

                            var usersDown = latchedEvent.GetStringEvent("users_down");
                            if (!string.IsNullOrEmpty(usersDown))
                            {
                                SetUsersAffected(usersDown, biz);
                            }
                        }
                        else
                        {
                            AddEvent(biz, latchedEvent.time, false, string.Empty, ServiceEvent.eventType.WORKAROUND);
                        }
                    }
                    else if (latchedEvent.GetBoolEventActive("slabreach"))
                    {
                        if (latchedEvent.GetBoolEventActive("denial_of_service") == true)
                        {
                            AddEvent(biz, latchedEvent.time, false, "", ServiceEvent.eventType.DOS_SLABREACH);
                        }
                        else if (latchedEvent.GetBoolEventActive("security_flaw") == true)
                        {
                            AddEvent(biz, latchedEvent.time, false, "", ServiceEvent.eventType.SECURITY_FLAW_SLABREACH);
                        }
                        else if (latchedEvent.GetBoolEventActive("compliance_incident") == true)
                        {
                            AddEvent(biz, latchedEvent.time, false, "", ServiceEvent.eventType.COMPLIANCE_INCIDENT_SLA_BREACH);
                        }
                        else if (model.GetNamedNode(biz).GetIntAttribute("gain", 0) > 0)
                        {
                            AddEvent(biz, latchedEvent.time, false, "", ServiceEvent.eventType.REQUEST_SLA_BREACH);
                        }
                        else
                        {
                            AddEvent(biz, latchedEvent.time, false, "", ServiceEvent.eventType.SLABREACH);
                        }
                        //
                        var usersDown = latchedEvent.GetStringEvent("users_down");
                        if ("" != usersDown)
                        {
                            this.SetUsersAffected(usersDown, biz);
                        }
                    }
                    else if (latchedEvent.GetBoolEventActive("upByMirror") == true)
                    {
                        AddEvent(biz, latchedEvent.time, false, "", ServiceEvent.eventType.MIRROR);
                        //
                        var usersDown = latchedEvent.GetStringEvent("users_down");
                        if ("" != usersDown)
                        {
                            this.SetUsersAffected(usersDown, biz);
                        }
                    }
                    else if (!latchedEvent.GetBoolEventActive("up"))
                    {
                        if (incidentId != "")
                        {
                            if (latchedEvent.GetBoolEventActive("denial_of_service") == true)
                            {
                                AddEvent(biz, latchedEvent.time, false, "", ServiceEvent.eventType.DENIAL_OF_SERVICE);
                            }
                            else if (latchedEvent.GetBoolEventActive("security_flaw") == true)
                            {
                                AddEvent(biz, latchedEvent.time, false, "", ServiceEvent.eventType.SECURITY_FLAW);
                            }
                            else if (latchedEvent.GetBoolEventActive("compliance_incident") == true)
                            {
                                AddEvent(biz, latchedEvent.time, false, "", ServiceEvent.eventType.COMPLIANCE_INCIDENT);
                            }
                            else if (latchedEvent.GetBoolEventActive("awaiting_saas_auto_restore"))
                            {
                                AddEvent(biz, latchedEvent.time, false, "", ServiceEvent.eventType.INCIDENT_ON_SAAS);
                            }
                            else if (model.GetNamedNode(biz).GetIntAttribute("gain", 0) > 0)
                            {
                                AddEvent(biz, latchedEvent.time, false, "", ServiceEvent.eventType.REQUEST);
                            }
                            else
                            {
                                AddEvent(biz, latchedEvent.time, false, "", ServiceEvent.eventType.INCIDENT);
                            }

                            var usersDown = latchedEvent.GetStringEvent("users_down");
                            if (!string.IsNullOrEmpty(usersDown))
                            {
                                SetUsersAffected(usersDown, biz);
                            }
                        }
                    }
                    else if (latchedEvent.GetBoolEventActive("up"))
                    {
                        AddEvent(biz, latchedEvent.time, true, "", ServiceEvent.eventType.INCIDENT);
                    }
                }
            }


        }

        protected void MergeTrackers()
        {
            // : This code should only run on old-format files.
            if (gameFile.Version > 1)
            {
                return;
            }

            // Hashtable to ArrayList of mergable streams...
            var mergesToDo = new Hashtable();

            foreach (string str in this.mappings.Values)
            {
                var trackersToMerge = new ArrayList();

                foreach (string str2 in mappings.Keys)
                {
                    if ((string)mappings[str2] == str)
                    {
                        trackersToMerge.Add(str2);
                    }
                }

                if (trackersToMerge.Count > 1)
                {
                    // Trackers need to be merged.
                    mergesToDo[str] = trackersToMerge;
                }
            }
            //
            // Do we have any redundant streams?
            // A redundant stream is one that has another mergable non-retired stream but it has no non-zero time events on it.
            //
            //
            foreach (string str in mergesToDo.Keys)
            {
                var trackersToMerge = (ArrayList)mergesToDo[str];

                if (trackersToMerge.Count >= 2)
                {
                    var nonRetired = new ArrayList();

                    // Count non-retired streams on this merge...
                    foreach (string str_let in trackersToMerge)
                    {
                        var let = (LatchEdgeTracker)bizToTracker[str_let];

                        if (let != null)
                        {
                            if (false == let.GetLatchedEvent(let.Count - 1).GetBoolEventActive("retired"))
                            {
                                nonRetired.Add(str_let);
                            }
                        }
                    }

                    if (nonRetired.Count > 1)
                    {
                        var RemoveStreams = new ArrayList();

                        for (var i = 0; i < nonRetired.Count; i++)
                        {
                            var str_let = (string)nonRetired[i];
                            var let = (LatchEdgeTracker)bizToTracker[str_let];

                            var count = 0;

                            for (var ii = 0; ii < let.Count; ++ii)
                            {
                                var le = let.GetLatchedEvent(ii);
                                if (le.time != 0.0)
                                {
                                    ++count;
                                }
                            }

                            if (count == 0)
                            {
                                // This stream has nothing on it.
                                // Therefore, add it to streams to remove unless it is the last one and
                                // we would be removing all the streams.
                                if (i < @let.Count - 1)
                                {
                                    // Safe to remove...
                                    RemoveStreams.Add(str_let);
                                }
                                else
                                {
                                    // This is the last stream so check if we will accidentally remove all of them!
                                    if (RemoveStreams.Count != @let.Count - 1)
                                    {
                                        RemoveStreams.Add(str_let);
                                    }
                                }
                            }
                        }

                        foreach (string str_let in RemoveStreams)
                        {
                            trackersToMerge.Remove(str_let);
                            bizToTracker.Remove(str_let);
                        }
                    }
                }
            }
            //
            // Merge trackers...
            //
            foreach (string str in mergesToDo.Keys)
            {
                var trackersToMerge = (ArrayList)mergesToDo[str];
                //
                // TODO : Should in theory be able to support more than one upgrade
                // in a particular round.
                //
                // Assume for now that we only have two to merge! (Dangerous I know : LP).
                //
                if (trackersToMerge.Count == 2)
                {
                    var first = (string)trackersToMerge[0];
                    var upgrade = (string)trackersToMerge[1];

                    var letFirst = (LatchEdgeTracker)bizToTracker[first];
                    var letUpgrade = (LatchEdgeTracker)bizToTracker[upgrade];

                    //
                    // If the first version isn't marked as retired then the upgrades are the other way around.
                    //
                    if (letFirst.Count == 0 || false == letFirst.GetLatchedEvent(letFirst.Count - 1).GetBoolEventActive("retired"))
                    {
                        //
                        // Hack alert! If you produce a new service in transition (e.g. through skip) then don't install
                        // we'll have two versions neither of which is retired. In this case we jump through a hoop of
                        // hack and pick the service that has the most things happening on it to be the first service.
                        //
                        // First check if the other service isn't marked as retired either.
                        //
                        // Actually, only swap if the second is marked as retired and has enough data...
                        //
                        if (letUpgrade.Count > 0 && true == letUpgrade.GetLatchedEvent(letUpgrade.Count - 1).GetBoolEventActive("retired"))
                        {
                            first = (string)trackersToMerge[1];
                            upgrade = (string)trackersToMerge[0];

                            letFirst = (LatchEdgeTracker)bizToTracker[first];
                            letUpgrade = (LatchEdgeTracker)bizToTracker[upgrade];
                        }
                    }
                    //
                    // The first version must now be marked as retired for us to proceed.
                    //
                    if (letFirst.Count > 0)
                    {
                        if (true == letFirst.GetLatchedEvent(letFirst.Count - 1).GetBoolEventActive("retired"))
                        {
                            letFirst.StoreInFile("letFirst.latch");
                            letUpgrade.StoreInFile("letUpgrade.latch");
                            // Removed the retired events for the initial stream...
                            var remove = new ArrayList();
                            //
                            for (var i = 0; i < letFirst.Count; ++i)
                            {
                                var le = letFirst.GetLatchedEvent(i);
                                if (true == le.GetBoolEventActive("retired"))
                                {
                                    // remove this event...
                                    remove.Add(le);
                                }
                            }

                            foreach (LatchedEvent le in remove)
                            {
                                letFirst.RemoveLatchedEvent(le);
                            }

                            for (var i = 0; i < letUpgrade.Count; ++i)
                            {
                                var le = letUpgrade.GetLatchedEvent(i);
                                // TODO : Do this better???
                                // Don't add the first event on the upgrade stream.
                                if (le.time != 0.0)
                                {
                                    letFirst.AddLatchedEvent((LatchedEvent)le.Clone());
                                }
                            }
                        }
                    }

                    bizToTracker.Remove(upgrade);
                }
            }
        }

        protected void DoAppWarningEvent(Node app, LatchedEvent latchedEvent, bool inWarning)
        {
            var affectedServices = new ArrayList();

            //Issue 8575 the warning system will have a problem if there is an support tech node inbetween the app and 
            //the links to the BSUs. We fixed it in AOSE R2 incident 3 by removing the unneeded support tech node 
            //It was much quicker than upgrading this code to walk down through support tech nodes to get to the links 
            //A job for the future when someone has more than 5 mins 

            // The app contains links to various BSUs...
            foreach (Node child in app.getChildren())
            {
                var linkNode = child as LinkNode;
                if (linkNode != null)
                {
                    var bsu = linkNode.To;

                    // The BSU will also be linked to by services that use it.
                    foreach (LinkNode serviceLink in bsu.BackLinks)
                    {
                        var service = serviceLink.From;
                        if (service.GetAttribute("type") == "biz_service" && !affectedServices.Contains(service))
                        {
                            affectedServices.Add(service);
                        }
                    }
                }
            }

            foreach (Node service in affectedServices)
            {
                var eventType = ServiceEvent.eventType.OK;
                if (inWarning)
                {
                    eventType = ServiceEvent.eventType.WARNING;
                }

                AddEvent(service.GetAttribute("name"), latchedEvent.time, !inWarning, "", eventType);
            }

        }

        protected void SetUsersAffected(string users, string service)
        {
            if (mappings.ContainsKey(service)) service = (string)mappings[service];

            var eventStream = bizServiceStatusStreams[service];

            if (eventStream != null && eventStream.lastEvent != null)
            {
                eventStream.lastEvent.SetUsersAffected(users);
            }
        }

        protected string GetActiveTrackerForMappedService(string service)
        {
            foreach (string mappedService in mappings.Keys)
            {
                var mappedTo = (string) mappings[mappedService];
                if (mappedTo == service)
                {
                    if (bizToTracker.ContainsKey(mappedService))
                    {
                        return mappedService;
                    }
                }
            }

            return "";
        }

        protected void BiLogReaderLineFound(object sender, string key, string line, double time)
        {
            if (bizToTracker.ContainsKey(key))
            {
                var let = (LatchEdgeTracker) bizToTracker[key];
                let.CheckLine(line, time);
            }
        }

        protected void BiLogReaderServerLineFound(object sender, string key, string line, double time)
        {
            if (serverNameToTracker.ContainsKey(key))
            {
                var let = (LatchEdgeTracker) serverNameToTracker[key];
                let.CheckLine(line, time);
            }
        }

        protected void BiLogReaderAppLineFound(object sender, string key, string line, double time)
        {
            if (appNameToTracker.ContainsKey(key))
            {
                var let = (LatchEdgeTracker) appNameToTracker[key];
                let.CheckLine(line, time);
            }
        }

        protected void BiLogReaderCostedEventFound(object sender, string key, string line, double time)
        {
            var type = BasicIncidentLogReader.ExtractValue(line, "type");
            var target = BasicIncidentLogReader.ExtractValue(line, "target");
            if (type == "workaround fixed")
            {
                // Set workaround as ended
                AddEvent(target, time, true, "", ServiceEvent.eventType.INCIDENT);
            }
            else if (type.Equals("incident"))
            {
                var services = BasicIncidentLogReader.ExtractValue(line, "service_name");

                //string serviceName = BasicIncidentLogReader.ExtractValue(line, "service_name");
                var numImpacted = BasicIncidentLogReader.ExtractIntValue(line, "impacted_store_channels");

                Debug.Assert(numImpacted.HasValue, "Number of transactions affected is missing.");
                
                foreach (var serviceName in services.Split(','))
                {
                    var fullServiceName = isBsu
                        ? CONVERT.Format("{0} {1}", serviceStartsWith, serviceName)
                        : serviceName;

                    if (bizServiceStatusStreams.ContainsKey(fullServiceName))
                    {
                        bizServiceToNumImpactedStoreChannels[serviceName] = numImpacted.Value;
                    }
                }
                
                
                
            }


        }

        protected void BiLogReaderRevenueFound(object sender, string key, string line, double time)
        {
            var lostRev = BasicIncidentLogReader.ExtractValue(line, "revenue_lost");
            var allLostRev = BasicIncidentLogReader.ExtractValue(line, "all_lost_revenues");

            if (string.IsNullOrEmpty(lostRev))
            {
                var inX = (int) (time / 60);
                lostRevenues[inX] = lostRev;
            }
            var allLost = CONVERT.ParseBool(allLostRev, false);

            if (allLost)
            {
                var allStoresTaken = true;
                var i = 1;
                while (allStoresTaken)
                {
                    var businessType = SkinningDefs.TheInstance.GetData("biz");

                    var storeLost = BasicIncidentLogReader.ExtractValue(line, "lost" + businessType + i);
                    var revLostTime = BasicIncidentLogReader.ExtractValue(line, "i_doAfterSecs");
                    var avp = new AttributeValuePair(revLostTime, storeLost);

                    if (string.IsNullOrEmpty(storeLost))
                    {
                        allStoresTaken = false;
                    }
                    else
                    {
                        while (lostRevenuePerStore.Count < i)
                        {
                            var blankList = new Dictionary<int, int>();
                            lostRevenuePerStore.Add(blankList);
                        }
                        lostRevenuePerStore[i - 1].Add(CONVERT.ParseInt(revLostTime), CONVERT.ParseInt(storeLost));
                    }

                    i++;
                }
            }
        }

        protected void BiLogReaderApplicationsProcessedFound(object sender, string key, string line, double time)
        {
            var lostRev = BasicIncidentLogReader.ExtractValue(line, "apps_lost");

            if (string.IsNullOrEmpty(lostRev))
            {
                var inX = (int) (time / 60);
                lostRevenues[inX] = lostRev;
            }
        }

        protected void BiLogReaderAwtChanged(object sender, string key, string line, double time)
        {
            var enabledStr = BasicIncidentLogReader.ExtractValue(line, "enabled");

            if (string.IsNullOrEmpty(enabledStr))
            {
                awtActive = enabledStr.ToLower().Equals("true");
            }
        }

        protected void ConnectEvents(ServiceEvent lastEvent, ServiceEvent newEvent)
        {
            if (lastEvent != null)
            {
                if (!lastEvent.up || lastEvent.seType == ServiceEvent.eventType.WORKAROUND)
                {
                    lastEvent.next = newEvent;
                    newEvent.last = lastEvent;
                }
            }
        }

        protected void AddEvent (string service, double seconds, bool up, string reason, ServiceEvent.eventType eType)
        {
            var serviceEvent = new ServiceEvent(seconds, up, reason, eType);

            if (mappings.ContainsKey(service))
            {
                service = (string) mappings[service];
            }

            if (bizServiceStatusStreams.ContainsKey(service))
            {
                var eventStream = bizServiceStatusStreams[service];

                if (eventStream == null)
                    return;
                if (eventStream.lastEvent != null)
                {
                    // If the current event is the same type as the last event 
                    // and they're of type WorkAround, WA_SLABREACH, or DOS_SLABREACH
                    // then ignore it and return.
                    if (eType == eventStream.lastEvent.seType && 
                        (eType == ServiceEvent.eventType.WORKAROUND || 
                        eType == ServiceEvent.eventType.WA_SLABREACH || 
                        eType == ServiceEvent.eventType.DOS_SLABREACH))
                    {
                        return;
                    }
                   
                    // Set this event's end point...
                    eventStream.lastEvent.secondsIntoGameEnds = seconds;
                }

                // Only Add "down" events.
                if (!up)
                {
                    eventStream.events.Add(serviceEvent);
                    ConnectEvents(eventStream.lastEvent, serviceEvent);
                    eventStream.lastEvent = serviceEvent;
                }
                else
                {
                    eventStream.lastEvent = null;
                }

            }
            else
            {
                if (!up)
                {
                    var eventStream = new EventStream();
                    eventStream.lastEvent = serviceEvent;
                    eventStream.events.Add(serviceEvent);
                    bizServiceStatusStreams[service] = eventStream;
                }
               
            }

        }
    


        protected void WatchAdditionalItems (BasicIncidentLogReader logReader)
        {
            // Does nothing apparently (GC) TODO
        }

    } // end class IncidentGanttReport
}
