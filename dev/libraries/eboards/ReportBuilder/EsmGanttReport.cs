using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Xml;
using System.Linq;

using CoreUtils;
using GameManagement;
using LibCore;
using Network;

namespace ReportBuilder
{
	public class EsmGanttReport : OpsGanttReport
	{
		string ColourIT = SkinningDefs.TheInstance.GetData("function_colour_it", "62, 143, 205");
		string ColourHR = SkinningDefs.TheInstance.GetData("function_colour_hr", "211, 17, 29");
		string ColourFAC = SkinningDefs.TheInstance.GetData("function_colour_fac", "126, 184, 40");
		string ColourLF = SkinningDefs.TheInstance.GetData("function_colour_leg_fin", "248, 175, 20");
		string ColourDefault = "255, 255, 255";

        class CompareLabelsList : IComparer<string>
        {
            OrderType orderType;
            NetworkProgressionGameFile gameFile;
            int round;

            public CompareLabelsList (NetworkProgressionGameFile gameFile, int round, OrderType orderType)
            {
                this.gameFile = gameFile;
                this.round = round;
                this.orderType = orderType;
            }

            public int Compare(string lhs, string rhs)
            {
                Node lhsService = gameFile.GetNetworkModel(round).GetNamedNode(lhs);
                Node rhsService = gameFile.GetNetworkModel(round).GetNamedNode(rhs);

                Debug.Assert(lhsService != null && rhsService != null);

                switch (orderType)
                {
                    case OrderType.Priority:

                        break;
                    case OrderType.IncidentType:
                        int lhsImpact = lhsService.GetIntAttribute("impact", 0) - lhsService.GetIntAttribute("gain", 0);
                        int rhsImpact = rhsService.GetIntAttribute("impact", 0) - rhsService.GetIntAttribute("gain", 0);

                        if (lhsImpact != rhsImpact)
                        {
                            return lhsImpact.CompareTo(rhsImpact);
                        }
                        break;
                    case OrderType.Function:
                        if (lhsService.GetAttribute("function") != rhsService.GetAttribute("function"))
                        {
                            return lhsService.GetAttribute("function").CompareTo(rhsService.GetAttribute("function"));
                        }
                        break;
                }

                int lhsPriority = lhsService.GetIntAttribute("priority", 0);
                int rhsPriority = rhsService.GetIntAttribute("priority", 0);
                if (lhsPriority != rhsPriority)
                {
                    return lhsPriority.CompareTo(rhsPriority);
                }

                return lhsService.GetAttribute("shortdesc").CompareTo(rhsService.GetAttribute("shortdesc"));
            }
        }

		class CompareLabels : IComparer
		{
			OrderType orderType;
			NetworkProgressionGameFile gameFile;
			int round;

			public CompareLabels (NetworkProgressionGameFile gameFile, int round, OrderType orderType)
			{
				this.gameFile = gameFile;
				this.round = round;
				this.orderType = orderType;
			}

			public int Compare (object x, object y)
			{
				string stringX = (string) x;
				string stringY = (string) y;

				Node serviceX = gameFile.GetNetworkModel(round).GetNamedNode(stringX);
				Node serviceY = gameFile.GetNetworkModel(round).GetNamedNode(stringY);

				switch (orderType)
				{
					case OrderType.Priority:
						// Just fall through to sort by priority.
						break;

					case OrderType.Function:
						if (serviceX.GetAttribute("function") != serviceY.GetAttribute("function"))
						{
							return serviceX.GetAttribute("function").CompareTo(serviceY.GetAttribute("function"));
						}
						break;

					case OrderType.IncidentType:
						int impactX = serviceX.GetIntAttribute("impact", 0) - serviceX.GetIntAttribute("gain", 0);
						int impactY = serviceY.GetIntAttribute("impact", 0) - serviceY.GetIntAttribute("gain", 0);
						if (impactX != impactY)
						{
							return impactX.CompareTo(impactY);
						}
						break;
				}

				int priorityX = serviceX.GetIntAttribute("priority", 0);
				int priorityY = serviceY.GetIntAttribute("priority", 0);
				if (priorityX != priorityY)
				{
					return priorityX.CompareTo(priorityY);
				}

				return serviceX.GetAttribute("shortdesc").CompareTo(serviceY.GetAttribute("shortdesc"));
			}
		}

		public enum OrderType
		{
			Function,
			Priority,
			IncidentType
		}

		OrderType orderType = OrderType.Function;

		public EsmGanttReport(bool showOnlyDownedServices, OrderType orderType)
			: base (showOnlyDownedServices)
		{
			this.orderType = orderType;

            if (orderType == OrderType.IncidentType)
            {
                showOnlyDownedServices = true;
            }
		}

		protected override void OrderServiceNames (ArrayList names)
		{
			names.Sort(new CompareLabels (gameFile, round.Value, orderType));
		}

        //protected override string WriteReport()
        //{

        //    return GenerateGanttReport();
        //}

        string GenerateGanttReport()
        {
            BasicXmlDocument xml = BasicXmlDocument.Create();
            XmlElement root = xml.AppendNewChild("timechart");

            XmlElement timeline = xml.AppendNewChild(root, "timegrid");
            BasicXmlDocument.AppendAttribute(timeline, "start", 0);
            BasicXmlDocument.AppendAttribute(timeline, "end", roundMins);
            BasicXmlDocument.AppendAttribute(timeline, "interval", 5);
            BasicXmlDocument.AppendAttribute(timeline, "title", "Time");
            BasicXmlDocument.AppendAttribute(timeline, "fore_colour", Color.White);


            List<string> serviceNames = new ArrayList(BizServiceStatusStreams.Keys).Cast<string>().ToList();

            Dictionary<string, string> newNamesToOriginalNames = new Dictionary<string, string>();
            
            List<string> newNames = new List<string>();
            foreach (string name in serviceNames)
            {
                string newName = name.StartsWith(serviceStartsWith) ? name.Substring(serviceStartsWith.Length).Trim() : name;

                newNamesToOriginalNames[newName] = name;
                newNames.Add(newName);
            }

            newNames.Sort(new CompareLabelsList(gameFile, round.Value, orderType));

            bool useStriped = SkinningDefs.TheInstance.GetBoolData("gantt_use_striped_colours", false);

            Dictionary<string, string> nameToLabel = new Dictionary<string, string>();

            List<string> colours = new List<string>();


            foreach (string name in newNames)
            {
                string serviceName = newNamesToOriginalNames[name];

                if (!(showOnlyDownedServices && (((EventStream)BizServiceStatusStreams[serviceName]).events.Count == 0)))
                {
                    string mtfr = "0";
                    string displayName = Strings.RemoveHiddenText((string)LongNamesToShortDescs[serviceName]);
                    if (bizToTracker.ContainsKey(serviceName))
                    {
                        

                        if (roundScores != null)
                        {
                            mtfr = GetMTFR(serviceName);
                        }
                        
                    }
                    else
                    {
                        string mappedService = GetActiveTrackerForMappedService(serviceName);
                        if (mappedService != "")
                        {
                            if (roundScores != null)
                            {
                                mtfr = GetAvailability(mappedService);
                            }
                        }
                        else
                        {
                            displayName = name;
                            mtfr = "0";
                        }
                    }

                    nameToLabel[name] = displayName + "\t" + mtfr;

                    if (useStriped)
                    {
                        List<Node> services =
                            model.GetNamedNode("Business Services Group")
                                .GetChildrenWithAttributeValue("type", "biz_service");

                        string func = "";

                        foreach (Node service in services.Where(service => service.GetAttribute("name") == serviceName))
                        {
                            func = service.GetAttribute("function");
                        }

                        switch(func)
                        {
                            case "IT":
                                colours.Add(ColourIT);
                                break;
                            case "HR":
                                colours.Add(ColourHR);
                                break;
                            case "FM":
                                colours.Add(ColourFAC);
                                break;
                            case "Other":
                                colours.Add(ColourLF);
                                break;
                            default:
                                colours.Add(ColourDefault);
                                break;
                        }
                    }
                }
            }

            if (useStriped)
            {
                colours.Reverse();
            }

            XmlElement sections = xml.AppendNewChild(root, "sections");
            BasicXmlDocument.AppendAttribute(sections, "use_gradient", true);
            
            if (orderType == OrderType.IncidentType)
            { 
                foreach(ServiceEvent.eventType [] types in new [] 
                        {
                            new [] { ServiceEvent.eventType.INFORMATION_REQUEST, ServiceEvent.eventType.INFORMATION_REQUEST_SLA_BREACH },
                            new [] { ServiceEvent.eventType.REQUEST, ServiceEvent.eventType.REQUEST_SLA_BREACH },
                            new [] { ServiceEvent.eventType.INCIDENT, ServiceEvent.eventType.SLABREACH }
                        })
                {
                    foreach (string name in newNames)
                    {
                        string serviceName = newNamesToOriginalNames[name];

                        List<ServiceEvent> events = new List<ServiceEvent>
                                                            (
                                                            (ServiceEvent[])((EventStream)BizServiceStatusStreams[serviceName])
                                                                                            .events.ToArray(typeof(ServiceEvent))
                                                            );

                        if (events.Any(e => types.Contains(e.seType)))
                        {
                            OutputServiceBlocks(xml, sections, serviceName, nameToLabel[name], types);
                        }
                        
                    }
                }
            }
            else
            {
                foreach (string name in newNames)
                {
                    string serviceName = newNamesToOriginalNames[name];

                    if (!(showOnlyDownedServices && (((EventStream)BizServiceStatusStreams[serviceName]).events.Count == 0)))
                    {
                        OutputServiceBlocks(xml, sections, serviceName, nameToLabel[name]);
                    }
                }
                    

            }

           
            
           


            // **************

            string reportFile = gameFile.GetRoundFile(gameFile.LastRoundPlayed,
                "OpsGanttReport_" + serviceStartsWith + ".xml",
                gameFile.LastPhasePlayed);
            xml.Save(reportFile);

            return reportFile;
        }

		protected override void GenerateGanttReport(BasicXmlDocument xdoc, XmlNode root)
		{
			XmlNode xtitle = xdoc.CreateElement("xAxis");

			string minMaxSteps_str = "0," + CONVERT.ToStr(roundMins) + ",5";

			((XmlElement)xtitle).SetAttribute("title", "Time");
			((XmlElement)xtitle).SetAttribute("minMaxSteps", minMaxSteps_str);
			((XmlElement)xtitle).SetAttribute("autoScale", "false");
		    BasicXmlDocument.AppendAttribute(xtitle, "colour",
		        SkinningDefs.TheInstance.GetColorData("gantt_text_colour", Color.Black));

			root.AppendChild(xtitle);

			XmlNode yaxis = xdoc.CreateElement("yAxis");

			if (-1 != yaxis_width)
			{
				((XmlElement)yaxis).SetAttribute("width", CONVERT.ToStr(yaxis_width));
			}

			((XmlElement)yaxis).SetAttribute("align", "right");

			string labels = "";
			List<string> colours = new List<string>();
			bool addComma = false;

			// Order names alphabetically
			ArrayList names = new ArrayList(BizServiceStatusStreams.Keys);

			// Map all names to labels that do not have store/car x at the begining...
			Hashtable nameToLabel = new Hashtable();
			ArrayList newNames = new ArrayList();

			foreach (string str in names)
			{
				string s = str.StartsWith(serviceStartsWith) ? str.Substring(serviceStartsWith.Length).Trim() : str;

				nameToLabel[s] = str;
				newNames.Add(s);
			}

			OrderServiceNames(newNames);
			string use_striped = SkinningDefs.TheInstance.GetData("gantt_use_striped_colours", "false");

			// Old version 1 game files... (Gannt chart is much more complex and error prone...
			foreach (string str2 in newNames)
			{
				string str = (string)nameToLabel[str2];

				if (!(showOnlyDownedServices && (((EventStream)BizServiceStatusStreams[str]).events.Count == 0)))
				{
					if (bizToTracker.ContainsKey(str))
					{
						string mtfr = "0";

						if (roundScores != null)
						{
							mtfr = GetMTFR(str);
						}

						// Get the availability and add here
						if (addComma) labels += ",";

						// : Fix for 3762 (names in Gantt were coming up emtpy except for all stores).
						labels += Strings.RemoveHiddenText((string)LongNamesToShortDescs[str]) + "    " + mtfr;

						addComma = true;
					}
					else
					{
						// This must be a mapped service.
						// Find a valid service that maps to this...

						string mappedService = GetActiveTrackerForMappedService(str);
						if ("" != mappedService)
						{
							string mtfr = "0";
							if (roundScores != null)
							{
								mtfr = GetAvailability(mappedService);
							}
							if (addComma) labels += ",";

							labels += Strings.RemoveHiddenText((string)LongNamesToShortDescs[str]) + "    " + mtfr;

							addComma = true;
						}
						else
						{
							if (addComma) labels += ",";
							labels += str2 + " " + "0";
							addComma = true;
						}
					}
					if (use_striped == "true")
					{
						ArrayList services = model.GetNamedNode("Business Services Group").GetChildrenOfType("biz_service");
						String func = "";
						foreach (Node n in services)
						{
							if (n.GetAttribute("name") == str)
							{
								func = n.GetAttribute("function");
							}
						}

						switch (func)
						{
							case "IT":
								colours.Add(ColourIT);
								break;
							case "HR":
								colours.Add(ColourHR);
								break;
							case "FM":
								colours.Add(ColourFAC);
								break;
							case "Other":
								colours.Add(ColourLF);
								break;
							default:
								colours.Add(ColourDefault);
								break;
						}
					}
				}
			}

			if (use_striped == "true")
			{
				colours.Reverse();
				StringBuilder builder = new StringBuilder();

				foreach (string c in colours)
				{
					builder.Append(c).Append(", ");
				}
				((XmlElement)yaxis).SetAttribute("colours", builder.ToString());
			}

			root.AppendChild(yaxis);

			XmlNode rows = xdoc.CreateElement("rows");
			int width = 60 * roundMins;
			((XmlElement)rows).SetAttribute("length", CONVERT.ToStr(width));
			root.AppendChild(rows);

            if (orderType == OrderType.IncidentType)
            {
                StringBuilder labelsBuilder = new StringBuilder ();

                foreach (ServiceEvent.eventType [] types in new[] { new [] { ServiceEvent.eventType.INFORMATION_REQUEST, ServiceEvent.eventType.INFORMATION_REQUEST_SLA_BREACH },
                                                                    new [] { ServiceEvent.eventType.REQUEST, ServiceEvent.eventType.REQUEST_SLA_BREACH },
                                                                    new [] { ServiceEvent.eventType.INCIDENT, ServiceEvent.eventType.SLABREACH },
                                                                    })
                {
                    foreach (string serviceName in newNames)
                    {
                        string service = (string) nameToLabel[serviceName];
                        ServiceEvent [] events = (ServiceEvent []) ((EventStream) BizServiceStatusStreams[service]).events.ToArray(typeof (ServiceEvent));

                        if (events.Any(e => types.Contains(e.seType)))
                        {
                            OutputServiceData(xdoc, rows, service, types);

                            if (labelsBuilder.Length > 0)
                            {
                                labelsBuilder.Append(",");
                            }
                            labelsBuilder.Append((string) LongNamesToShortDescs[service]);
                            labelsBuilder.Append("    ");
                            labelsBuilder.Append((roundScores != null) ? GetMTFR(service) : "0");
                        }
                    }
                }

                labels = labelsBuilder.ToString();
            }
            else
            {
                foreach (string str2 in newNames)
                {
                    string service = (string)nameToLabel[str2];

                    if (!(showOnlyDownedServices && (((EventStream)BizServiceStatusStreams[service]).events.Count == 0)))
                    {
                        OutputServiceData(xdoc, rows, service);
                    }
                }
            }

			if ((RevenueHoverRequired))
			{
                
				if (store_level)
				{
					// Save the lost revenues for each minute
					revenues = xdoc.CreateElement("revenue_lost");

					prev_rev = 0;
					for (int i = 0; i < LostRevenues.Count; i++)
					{
						string tmp = (string)LostRevenues[i];

						if (tmp == string.Empty) tmp = "0";
						int rev = CONVERT.ParseInt(tmp);

						int lost_rev;
						if (rev == 0)
						{
							lost_rev = 0;
						}
						else
						{
							lost_rev = rev - prev_rev;
							prev_rev = rev;
						}

						AddToNode(i, lost_rev, xdoc);

					}

					root.AppendChild(revenues);
				}
				else if (LostRevenuePerStore.Count != 0)
				{

					int store = CONVERT.ParseInt(serviceStartsWith.Remove(0, serviceStartsWith.Length - 2));
					revenues = xdoc.CreateElement("revenue_lost");

					int[] LocalLostRevenues = new int[roundMins + 1];

					foreach (KeyValuePair<int, int> avp in LostRevenuePerStore[store - 1])
					{
						int minute = (int)Math.Ceiling((double)(avp.Key / 60));
						LocalLostRevenues[minute] = avp.Value;
					}

					for (int i = 0; i < LocalLostRevenues.Length; i++)
					{
						AddToNode(i, LocalLostRevenues[i], xdoc);
					}

					root.AppendChild(revenues);
				}
			}

			XmlElement key = xdoc.AppendNewChild(root, "key");
			foreach (KeyItem keyItem in legendToKeyItem.Values)
			{
				keyItem.AddToXml(xdoc, key);
			}

            ((XmlElement)yaxis).SetAttribute("minMaxSteps", "0," + CONVERT.ToStr(xdoc.DocumentElement.SelectSingleNode("rows").ChildNodes.Count) + ",1");
            ((XmlElement)yaxis).SetAttribute("labels", labels);
        }

		string GetMTFR(string service)
		{
			bool up = true;
			double lastTime = 0.0;
			double downFor = 0.0;

			bool isBSU = (serviceStartsWith != SkinningDefs.TheInstance.GetData("allbiz"));

			int numUsers = 4;

			if(serviceToUserCount.ContainsKey(service))
			{
				numUsers = (int) serviceToUserCount[service];
			}

			// Prevent divide by zero just in case.
			if(numUsers == 0) numUsers = 4;

			// : Fix for 4064 (availability values differ for the same service between
			// the all and single store modes).
			if (isBSU)
			{
				numUsers = 1;
			}

			double numDown = 0.0;

			if(bizToTracker.ContainsKey(service))
			{
				LatchEdgeTracker let = (LatchEdgeTracker) bizToTracker[service];

				if (let.Count == 0) return "0";

				for(int i = 0; i < let.Count; ++i)
				{
					LatchedEvent le = let.GetLatchedEvent(i);
					double lengthOfLastEvent = le.time - lastTime;

					string nd_str = le.GetStringEvent("users_down");

					if(!up)
					{
						downFor += lengthOfLastEvent * numDown;
					}

					up = le.GetBoolEventActive("up");

					// : Wrongly calculated numDown as 1 when users_down was blank.
					if (isBSU)
					{
						numDown = up ? 0 : 1;
					}
					else
					{
						numDown = nd_str.Length == 0 ? 0 : nd_str.Split(',').Length;
					}

					lastTime = le.time;
				}
			}

			double lengthOfFinalEvent = roundScores.FinalTime - lastTime;

			if(!up)
			{
				downFor += lengthOfFinalEvent * numDown;
			}

			double mtfr = downFor / numUsers;

			return CONVERT.FormatTimeFourDigits(mtfr);
		}

        void OutputServiceBlocks(BasicXmlDocument xml, XmlNode root, string service, string displayName, ServiceEvent.eventType [] eventTypes = null)
        {
            Node serviceNode = model.GetNamedNode(service);

             lastKnownTimeInGame = 0;
            if (roundScores != null)
            {
                lastKnownTimeInGame = roundScores.FinalTime;
            }

            if (BizServiceStatusStreams.ContainsKey(service))
            {
                

                XmlNode section = xml.AppendNewChild(root, "section");

                // TODO need to check colours used
                BasicXmlDocument.AppendAttribute(section, "row_forecolour", Color.White);
                BasicXmlDocument.AppendAttribute(section, "row_backcolour", Color.Black);

                BasicXmlDocument.AppendAttribute(section, "legend", displayName);
                BasicXmlDocument.AppendAttribute(section, "header_width", 250);
                BasicXmlDocument.AppendAttribute(section, "align", "right");


                XmlElement row = xml.AppendNewChild(section, "row");
                BasicXmlDocument.AppendAttribute(row, "legend", "-- -- -- --");
                BasicXmlDocument.AppendAttribute(row, "regions_code", "");
                BasicXmlDocument.AppendAttribute(row, "header_width", 50);

                EventStream eventStream = (EventStream) BizServiceStatusStreams[service];

                string function = serviceNode.GetAttribute("function");

                foreach (ServiceEvent se in eventStream.events)
                {
                    if (se.secondsIntoGameOccured == se.secondsIntoGameEnds)
                    {
                        continue;
                    }

                    if (eventTypes != null
                        && !eventTypes.Contains(se.seType))
                    {
                        continue;
                    }

                    int startTime = (int) (se.secondsIntoGameOccured);

                    int length = (int) (lastKnownTimeInGame - startTime);
                    
                    if (se.secondsIntoGameEnds != 0)
                    {
                        int secondsGameEnd = (int) (se.secondsIntoGameEnds);
                        length = secondsGameEnd - startTime;
                    }

                    int reportedLength = (int)se.GetLength(lastKnownTimeInGame);

                    if (length == 0)
                    {
                        length = 1;
                    }

                    if (reportedLength == 0)
                    {
                        reportedLength = 1;
                    }

                    int endTime = startTime + length;

                    XmlElement block = xml.AppendNewChild(row, "block");

                    float start = (startTime / 60f);
                    float end = (endTime / 60f);

                    bool isSlaBreach = (se.seType == ServiceEvent.eventType.SLABREACH) ||
                                          (se.seType == ServiceEvent.eventType.REQUEST_SLA_BREACH) ||
                                          (se.seType == ServiceEvent.eventType.INFORMATION_REQUEST_SLA_BREACH);

                    string type = "MISSING";
                    string patternName = "MISSING";
                    string legend = "MISSING";

                    if (orderType == OrderType.IncidentType)
                    {
                        switch (se.seType)
                        {
                            case ServiceEvent.eventType.INCIDENT:
                                type = "incident";
                                legend = "Incident";
                                break;
                            case ServiceEvent.eventType.REQUEST:
                                type = "request";
                                legend = "Request";
                                break;
                            case ServiceEvent.eventType.INFORMATION_REQUEST:
                                type = "info";
                                legend = "Info";
                                break;
                            case ServiceEvent.eventType.SLABREACH:
                                type = "slabreach";
                                legend = "Incident";
                                patternName = "orange_hatch";
                                break;
                            case ServiceEvent.eventType.REQUEST_SLA_BREACH:
                                type = "request_slabreach";
                                legend = "Request";
                                patternName = "yellow_hatch";
                                break;
                            case ServiceEvent.eventType.INFORMATION_REQUEST_SLA_BREACH:
                                type = "info_slabreach";
                                legend = "Info";
                                patternName = "magenta_hatch";
                                break;
                        }
                    }
                    else
                    {
                        switch (function)
                        {
                            case "IT":
                            case "HR":
                            case "FM":
                                type = legend = function;
                                break;
                            default:
                                type = "FIN";
                                legend = "FIN & LEG";
                                break;
                        }

                        if (isSlaBreach)
                        {
                            patternName = type.ToLower() + "_hatch";
                        }
                    }

                    Debug.Assert(type != "MISSING" && legend != "MISSING");

                    if (isSlaBreach)
                    {
                        legend += " (SLA Breach)";
                        Debug.Assert(patternName != "MISSING");
                        EmitPatternedBlock(block, start, end, patternName, legend);
                    }
                    else
                    {
                        EmitColouredBlock(block, start, end, type, legend);
                    }


                    string desc = "";

                    if (se.users_affected != "")
                    {
                        desc = se.users_affected;

                        if (serviceToUserCount.ContainsKey(service))
                        {
                            int numUsers = (int) serviceToUserCount[service];
                            desc += " of " + CONVERT.ToStr(numUsers);
                        }

                        desc += ": ";
                    }

                    int mins = reportedLength / 60;
                    int secs = reportedLength % 60;

                    if (mins < 1)
                    {
                        desc += "Request lasts " + CONVERT.ToStr(secs) + " seconds.";
                    }
                    else
                    {
                        desc += CONVERT.Format("Request lasts {0} {1} seconds.", Plurals.Format(mins, "minute", "minutes"), secs);
                    }

                    desc = CONVERT.Format("Domains Affected({0}): ",
                        serviceNode.GetIntAttribute("num_domains_impacted", 0));

                    string [] domains =
                    {
                        "fm",
                        "hr",
                        "it",
                        "other"
                    };
                    string domainsAffected = domains.Where(domain => serviceNode.GetBooleanAttribute(domain + "_impacted", false))
                        .Aggregate("", (current, domain) => current + (DomainDisplayName(domain) + "#"));

                    domainsAffected = domainsAffected.Substring(0, domainsAffected.Length - 1).Replace("#", ", ");

                    desc += domainsAffected;

                    if (RevenueHoverRequired)
                    {
                        int min = startTime / 60;
                        int endMin = endTime / 60;

                        while (min <= endMin)
                        {
                            int minIndex = min * 60;
                            int lostRev = 0;
                            
                            // All stores.
                            if (store_level)
                            {
                                foreach (Dictionary<int, int> storeMinRev in LostRevenuePerStore)
                                {
                                    if (storeMinRev.ContainsKey(minIndex))
                                    {
                                        lostRev += storeMinRev[minIndex];
                                    }
                                }
                                
                            }
                            else if (LostRevenuePerStore.Count != 0)
                            {

                                int store = CONVERT.ParseInt(serviceStartsWith.Remove(0, serviceStartsWith.Length - 2));

                                if (LostRevenuePerStore[store - 1].ContainsKey(minIndex))
                                {
                                    lostRev += LostRevenuePerStore[store -1][minIndex];
                                }
                            }

                            XmlElement mouseBlock = xml.AppendNewChild(row, "mouseover_block_bar");

                            BasicXmlDocument.AppendAttribute(mouseBlock, "start", min);
                            BasicXmlDocument.AppendAttribute(mouseBlock, "end", min + 1);
                            BasicXmlDocument.AppendAttribute(mouseBlock, "legend", desc);
                            BasicXmlDocument.AppendAttribute(mouseBlock, "value", lostRev);
                            BasicXmlDocument.AppendAttribute(mouseBlock, "length", reportedLength);

                            min++;
                        }
                    }
                    

                }



            }
        }

        string DomainDisplayName(string domain)
        {
            switch(domain.ToLower())
            {
                case "fm":
                    return "Fac";
                case "hr":
                    return "HR";
                case "it":
                    return "IT";
                case "other":
                    return "Fin && Leg";
                default :
                    return "UNKNOWN";
            }
        }

        void EmitColouredBlock(XmlElement block, float start, float end, string type, string legend)
        {
            BasicXmlDocument.AppendAttribute(block, "start", start);
            BasicXmlDocument.AppendAttribute(block, "end", end);
            BasicXmlDocument.AppendAttribute(block, "legend", legend);

            Color blockColour = SkinningDefs.TheInstance.GetColorData("gantt_" + 
                type.ToLower() + "_colour", Color.Brown);

            BasicXmlDocument.AppendAttribute(block, "colour", blockColour);
        }

        void EmitPatternedBlock(XmlElement block, float start, float end, string patternName, string legend)
        {
            BasicXmlDocument.AppendAttribute(block, "start", start);
            BasicXmlDocument.AppendAttribute(block, "end", end);
            BasicXmlDocument.AppendAttribute(block, "legend", legend);
            BasicXmlDocument.AppendAttribute(block, "fill", patternName);
        }

		protected override void OutputServiceData(BasicXmlDocument xdoc, XmlNode root, string service)
        {
            OutputServiceData(xdoc, root, service, null);
        }



        void OutputServiceData (BasicXmlDocument xdoc, XmlNode root, string service, ServiceEvent.eventType [] eventTypes)
		{
			Node serviceNode = model.GetNamedNode(service);

			lastKnownTimeInGame = 0;
			if (roundScores != null) lastKnownTimeInGame = roundScores.FinalTime;

			if (BizServiceStatusStreams.ContainsKey(service))
			{
				string function = serviceNode.GetAttribute("function");
			    string functionLegend = model.GetNamedNode(function).GetAttribute("shortdesc").ToUpper().Replace(@"&", @"&&");

				XmlNode row = xdoc.CreateElement("row");
				((XmlElement)row).SetAttribute("colour", "255,0,0");
				((XmlElement)row).SetAttribute("data", Strings.RemoveHiddenText(service));

				EventStream eventStream = (EventStream)BizServiceStatusStreams[service];

				foreach (ServiceEvent se in eventStream.events)
				{
					if (se.secondsIntoGameOccured == se.secondsIntoGameEnds)
					{
						continue;
					}

                    if ((eventTypes != null)
                        && ! eventTypes.Contains(se.seType))
                    {
                        continue;
                    }

					int isecs = (int)(se.secondsIntoGameOccured);

					XmlNode bar = xdoc.CreateElement("bar");
					((XmlElement)bar).SetAttribute("x", CONVERT.ToStr(isecs));

					int ilength = (int)(lastKnownTimeInGame - isecs);
					int reportedLength = ilength;
					if (se.secondsIntoGameEnds != 0)
					{
						int isecsEnd = (int)(se.secondsIntoGameEnds);
						ilength = isecsEnd - isecs;
					}

					reportedLength = (int)se.GetLength(lastKnownTimeInGame);

					if (0 == ilength) ilength = 1;
					if (0 == reportedLength) reportedLength = 1;
					((XmlElement)bar).SetAttribute("length", CONVERT.ToStr(ilength));

					if (orderType != OrderType.IncidentType)
					{
						if (! (se.seType == ServiceEvent.eventType.SLABREACH || se.seType == ServiceEvent.eventType.REQUEST_SLA_BREACH || se.seType == ServiceEvent.eventType.INFORMATION_REQUEST_SLA_BREACH))
						{
							if (function == "IT")
							{
								SetBarAttributes((XmlElement)bar, "IT", ColourIT, "", "", functionLegend);
							}
							else if (function == "HR")
							{
								SetBarAttributes((XmlElement)bar, "HR", ColourHR, "", "", functionLegend);
							}
							else if (function == "FM")
							{
								SetBarAttributes((XmlElement)bar, "FM", ColourFAC, "", "", functionLegend);
							}
							else if (function == "Other")
							{
								SetBarAttributes((XmlElement)bar, "FIN", ColourLF, "", "", functionLegend);
							}
						}
						else
						{
						    string breachedLegend = functionLegend + " (SLA Breach)";

                            if (function == "IT")
							{
								SetBarAttributes((XmlElement)bar, "IT", "", "", "hatch_it", breachedLegend);
							}
							else if (function == "HR")
							{
								SetBarAttributes((XmlElement)bar, "HR", "", "", "hatch_hr", breachedLegend);
							}
							else if (function == "FM")
							{
								SetBarAttributes((XmlElement)bar, "FM", "", "", "hatch_fm", breachedLegend);
							}
							else if (function == "Other")
							{
								SetBarAttributes((XmlElement)bar, "FIN", "", "", "hatch_fin", breachedLegend);
							}
						}
					}
					else
					{
						if (se.seType == ServiceEvent.eventType.INCIDENT)
						{
							SetBarAttributes((XmlElement)bar, "incident", "181,90,129", "", "", "Incident");
						}
						else if (se.seType == ServiceEvent.eventType.REQUEST)
						{
							SetBarAttributes((XmlElement)bar, "request", "212,168,106", "", "", "Request");
						}
                        else if (se.seType == ServiceEvent.eventType.INFORMATION_REQUEST)
                        {
                            SetBarAttributes((XmlElement)bar, "info", "170,169,57", "", "", "Info");
                        }
                        else if (se.seType == ServiceEvent.eventType.SLABREACH)
						{
							SetBarAttributes((XmlElement)bar, "slabreach", "", "", "orange_hatch", "Incident (SLA Breached)");
						}
						else if (se.seType == ServiceEvent.eventType.REQUEST_SLA_BREACH)
						{
							SetBarAttributes((XmlElement)bar, "request_slabreach", "", "", "yellow_hatch", "Request (SLA Breached)");
						}
                        else if (se.seType == ServiceEvent.eventType.INFORMATION_REQUEST_SLA_BREACH)
                        {
                            SetBarAttributes((XmlElement)bar, "info_slabreach", "", "", "magenta_hatch", "Info (SLA Breached)");
                        }
                    }

					// 23-05-2007 - We set the bar's description to be "1,2,3 of 4 : time down".
					string desc = "";

					if (se.users_affected != "")
					{
						desc = se.users_affected;

						if (serviceToUserCount.ContainsKey(service))
						{
							int numUsers = (int)serviceToUserCount[service];
							desc += " of " + CONVERT.ToStr(numUsers);
						}

						desc += ": ";
					}

					int mins = reportedLength / 60;
					int secs = reportedLength % 60;
					if (mins < 1)
					{
						desc += "Requests lasts " + CONVERT.ToStr(secs) + " seconds.";
					}
					else if (mins == 1)
					{
						desc += "Requests lasts 1 Minute " + CONVERT.ToStr(secs) + " seconds.";
					}
					else
					{
						desc += "Requests lasts " + CONVERT.ToStr(mins) + " Minutes " + CONVERT.ToStr(secs) + " seconds.";
					}

                    desc = CONVERT.Format("Domains Affected({0}): ",
                       serviceNode.GetIntAttribute("num_domains_impacted", 0));

                    string[] domains =
                    {
                        "fm",
                        "hr",
                        "it",
                        "other"
                    };
                    string domainsAffected = domains.Where(domain => serviceNode.GetBooleanAttribute(domain + "_impacted", false))
                        .Aggregate("", (current, domain) => current + (DomainDisplayName(domain) + "#"));

                    domainsAffected = domainsAffected.Substring(0, domainsAffected.Length - 1).Replace("#", ", ");

                    desc += domainsAffected;

					((XmlElement)bar).SetAttribute("description", desc);

					row.AppendChild(bar);
				}

				root.AppendChild(row);
			}
		}

        protected override void ConvertTrackersToStreams()
        {
            // Before the incidents, do the AWT warnings for the servers.
            // Warnings need to come first because they will typically be followed,
            // and superceded, by incidents.
            if ((CoreUtils.SkinningDefs.TheInstance.GetIntData("gantt_show_warnings", 0) == 1)
                && (currentRound >= CoreUtils.SkinningDefs.TheInstance.GetIntData("gantt_warnings_start_round", 0)))
            {
                foreach (string serverName in serverNameToTracker.Keys)
                {
                    Node server = gameFile.NetworkModel.GetNamedNode(serverName);

                    if (server == null)
                    {
                        continue;
                    }

                    LatchEdgeTracker let = serverNameToTracker[serverName] as LatchEdgeTracker;

                    for (int i = 0; i < let.Count; i++)
                    {
                        LatchedEvent le = let.GetLatchedEvent(i);
                        string dangerLevel = le.GetStringEvent("danger_level");
                        if (dangerLevel != "")
                        {
                            int level = CONVERT.ParseInt(dangerLevel);

                            // This server contains apps...
                            foreach (Node app in server.getChildren())
                            {
                                DoAppWarningEvent(app, le, (level >= 33));
                            }
                        }
                    }
                }
                // And for the apps.
                foreach (string appName in appNameToTracker.Keys)
                {
                    Node app = gameFile.NetworkModel.GetNamedNode(appName);
                    LatchEdgeTracker let = appNameToTracker[appName] as LatchEdgeTracker;

                    for (int i = 0; i < let.Count; i++)
                    {
                        LatchedEvent le = let.GetLatchedEvent(i);
                        string dangerLevel = le.GetStringEvent("danger_level");
                        if (dangerLevel != "")
                        {
                            int level = CONVERT.ParseInt(dangerLevel);

                            DoAppWarningEvent(app, le, (level >= 33));
                        }
                    }
                }
            }

            // Then do the proper incidents.
            foreach (string biz in bizToTracker.Keys)
            {
                LatchEdgeTracker let = (LatchEdgeTracker)bizToTracker[biz];
                //
                int count = let.Count;
                for (int i = 0; i < count; ++i)
                {
                    LatchedEvent le = let.GetLatchedEvent(i);
                    // Pull the incident_id
                    string incident_id = le.GetStringEvent("incident_id");

                    // Always show rebooting first!
                    if (le.GetCounterEventActive("rebootingForSecs"))
                    {
                        AddEvent(biz, le.time, false,/*reason*/"", ServiceEvent.eventType.INSTALL);
                        //
                        string users_down = le.GetStringEvent("users_down");
                        if ("" != users_down)
                        {
                            this.SetUsersAffected(users_down, biz);
                        }
                    }
                    else if (le.GetCounterEventActive("workingAround"))
                    {
                        // New mode that shows SLA breaches in workaround...
                        if (le.GetBoolEventActive("slabreach"))
                        {
                            AddEvent(biz, le.time, false,/*reason*/"", ServiceEvent.eventType.WA_SLABREACH);
                            //
                            string users_down = le.GetStringEvent("users_down");
                            if ("" != users_down)
                            {
                                this.SetUsersAffected(users_down, biz);
                            }
                        }
                        else
                        {
                            AddEvent(biz, le.time, false, "", ServiceEvent.eventType.WORKAROUND);
                            //
                            string users_down = le.GetStringEvent("users_down");
                            if ("" != users_down)
                            {
                                this.SetUsersAffected(users_down, biz);
                            }
                        }
                    }
                    else if (le.GetBoolEventActive("slabreach"))
                    {
                        if (le.GetBoolEventActive("denial_of_service") == true)
                        {
                            AddEvent(biz, le.time, false, "", ServiceEvent.eventType.DOS_SLABREACH);
                        }
                        else if (le.GetBoolEventActive("security_flaw") == true)
                        {
                            AddEvent(biz, le.time, false, "", ServiceEvent.eventType.SECURITY_FLAW_SLABREACH);
                        }
                        else if (le.GetBoolEventActive("compliance_incident") == true)
                        {
                            AddEvent(biz, le.time, false, "", ServiceEvent.eventType.COMPLIANCE_INCIDENT_SLA_BREACH);
                        }
                        else
                        {
                            AddEvent(biz, le.time, false, "", MapErrorCodeToIncidentType(CONVERT.ParseInt((string)le.states["error_code"]), true));
                        }
                        //
                        string users_down = le.GetStringEvent("users_down");
                        if ("" != users_down)
                        {
                            this.SetUsersAffected(users_down, biz);
                        }
                    }
                    else if (le.GetBoolEventActive("upByMirror") == true)
                    {
                        AddEvent(biz, le.time, false, "", ServiceEvent.eventType.MIRROR);
                        //
                        string users_down = le.GetStringEvent("users_down");
                        if ("" != users_down)
                        {
                            this.SetUsersAffected(users_down, biz);
                        }
                    }
                    else if (!le.GetBoolEventActive("up"))
                    {
                        if (incident_id != "")
                        {
                            if (le.GetBoolEventActive("denial_of_service") == true)
                            {
                                AddEvent(biz, le.time, false, "", ServiceEvent.eventType.DENIAL_OF_SERVICE);
                            }
                            else if (le.GetBoolEventActive("security_flaw") == true)
                            {
                                AddEvent(biz, le.time, false, "", ServiceEvent.eventType.SECURITY_FLAW);
                            }
                            else if (le.GetBoolEventActive("compliance_incident") == true)
                            {
                                AddEvent(biz, le.time, false, "", ServiceEvent.eventType.COMPLIANCE_INCIDENT);
                            }
                            else if (le.GetBoolEventActive("awaiting_saas_auto_restore"))
                            {
                                AddEvent(biz, le.time, false, "", ServiceEvent.eventType.INCIDENT_ON_SAAS);
                            }
                            else
                            {
                                AddEvent(biz, le.time, false, "", MapErrorCodeToIncidentType(CONVERT.ParseInt((string) le.states["error_code"]), false));
                            }

                            string users_down = le.GetStringEvent("users_down");
                            if (!string.IsNullOrEmpty(users_down))
                            {
                                SetUsersAffected(users_down, biz);
                            }
                        }
                    }
                    else if (le.GetBoolEventActive("up"))
                    {
                        AddEvent(biz, le.time, true, "", ServiceEvent.eventType.INCIDENT);
                    }
                }
            }
        }

        ServiceEvent.eventType MapErrorCodeToIncidentType (int type, bool slaBreach)
        {
            switch (type)
            {
                case 3:
                    return slaBreach ? ServiceEvent.eventType.SLABREACH : ServiceEvent.eventType.INCIDENT;

                case 2:
                    return slaBreach ? ServiceEvent.eventType.REQUEST_SLA_BREACH : ServiceEvent.eventType.REQUEST;

                case 1:
                    return slaBreach ? ServiceEvent.eventType.INFORMATION_REQUEST_SLA_BREACH : ServiceEvent.eventType.INFORMATION_REQUEST;
            }

            throw new Exception (CONVERT.Format("Unknown error code {0}!", type));
        }    
    }
}