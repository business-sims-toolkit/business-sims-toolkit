using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using CoreUtils;
using GameManagement;
using LibCore;
using Logging;
using Network;

namespace ReportBuilder
{
    public class DevOpsRoundScores : RoundScores, IDisposable
    {
        public bool WasUnableToGetData = false;

        class TurnoverItem
        {
            readonly List<AttributeValuePair> attributes;

            double time;
            public double Time => time;

            public TurnoverItem(string logLine)
            {
                logLine = BaseUtils.xml_utils.TranslateFromEscapedXMLChars(logLine);
                attributes = ExtractAttributesFromLogLine(logLine, true);
                time = CONVERT.ParseDouble(BasicIncidentLogReader.ExtractValue(logLine, "i_doAfterSecs"));
            }

            public TurnoverItem(Node node)
            {
                attributes = new List<AttributeValuePair>();
                foreach (AttributeValuePair attributeValuePair in node.GetAttributes())
                {
                    attributes.Add(attributeValuePair);
                }

                time = 0;
            }

            public void Update(string logLine)
            {
                logLine = BaseUtils.xml_utils.TranslateFromEscapedXMLChars(logLine);
                var newAttributes = ExtractAttributesFromLogLine(logLine, false);
                time = CONVERT.ParseDouble(BasicIncidentLogReader.ExtractValue(logLine, "i_doAfterSecs"));

                foreach (var avp in newAttributes)
                {
                    var found = false;

                    foreach (var tryAvp in attributes)
                    {
                        if (tryAvp.Attribute == avp.Attribute)
                        {
                            tryAvp.Value = avp.Value;
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        attributes.Add(avp);
                    }
                }
            }

            static List<AttributeValuePair> ExtractAttributesFromLogLine(string logLine, bool isCreateLine)
            {
                var attributes = new List<AttributeValuePair>();

                string commandName;
                if (isCreateLine)
                {
                    commandName = "createNodes";
                }
                else
                {
                    commandName = "apply";
                }

                var prefix = "<" + commandName + " ";
                var prefixStart = logLine.IndexOf(prefix);
                Debug.Assert(prefixStart != -1);

                var chopped = logLine.Substring(prefixStart);

                if (isCreateLine)
                {
                    chopped = chopped.Substring(chopped.IndexOf(">") + 1);
                }

                var suffix = "/>";
                chopped = chopped.Substring(0, chopped.IndexOf(suffix));

                var parts = new List<string>();
                var currentPart = new StringBuilder();
                var inQuotes = false;
                foreach (var c in chopped)
                {
                    var handled = false;

                    switch (c)
                    {
                        case '"':
                            inQuotes = !inQuotes;
                            handled = true;
                            break;
                        case ' ':
                            if (!inQuotes)
                            {
                                if (currentPart.Length > 0)
                                {
                                    parts.Add(currentPart.ToString());
                                    currentPart = new StringBuilder();
                                    handled = true;
                                }
                            }
                            break;
                    }
                    if (!handled)
                    {
                        currentPart.Append(c);
                    }
                }

                if (currentPart.Length > 0)
                {
                    parts.Add(currentPart.ToString());
                    currentPart = new StringBuilder();
                }

                for (var i = 0; i < parts.Count; i++)
                {
                    var components = parts[i].Split('=');
                    var attributeName = components[0];

                    if (!attributeName.StartsWith("i_"))
                    {
                        var attributeValue = components[1];
                        attributes.Add(new AttributeValuePair(attributeName, attributeValue));
                    }

                }

                return attributes;
            }

            public AttributeValuePair GetAttributeValuePair(string attributeName)
            {
                foreach (var avp in attributes)
                {
                    if (avp.Attribute == attributeName)
                    {
                        return avp;
                    }
                }

                return null;
            }

            public string GetAttribute(string attributeName)
            {
                var avp = GetAttributeValuePair(attributeName);
                return avp?.Value;
            }

            public void SetAttribute(string attributeName, string value)
            {
                var avp = GetAttributeValuePair(attributeName);
                if (avp != null)
                {
                    avp.Value = value;
                }
                else
                {
                    attributes.Add(new AttributeValuePair(attributeName, value));
                }
            }
        }

        NodeTree model;
        NodeTree initialModel;
        double duration;

        public int NewServicesDeployed => newServicesCommissioned.Count;

        public int DemandsDeployed => demandsCommissioned.Count;

        public double TargetRevenue
        {
            // Change to be the same as TargetInfoBatches
            get;
            private set;
        }

        readonly List<string> newServicesCommissioned;
        readonly List<string> demandsCommissioned;

        readonly Dictionary<string, TimeLog<double>> newServiceNameToTimeToNetValue;
        public double TimeToValue
        {
            get
            {
                double totalTimeTillProfit = 0;
                var services = 0;

                foreach (var serviceName in newServiceNameToTimeToNetValue.Keys)
                {
                    var businessService = model.GetNamedNode(serviceName);
                    if ((businessService != null) && businessService.GetBooleanAttribute("is_new_service", false))
                    {
                        var startTime = newServiceNameToTimeToNetValue[serviceName].FirstTime;
                        double? profitTime = null;
                        foreach (var time in newServiceNameToTimeToNetValue[serviceName].Times)
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

        public double AverageDeployTime
        {
            get
            {
                if (serviceInstallNameToDeploymentTime.Count == 0)
                {
                    return 0.0;
                }

                var counter = 0;
                var sum = 0.0;
                foreach (var key in serviceInstallNameToDeploymentTime.Keys)
                {
                    sum += serviceInstallNameToDeploymentTime[key];
                    counter++;
                }

                return (sum / counter);
            }
        }

        readonly Dictionary<string, double> serviceInstallNameToStartTime;
        readonly Dictionary<string, double> serviceInstallNameToDeploymentTime;

        readonly double maxBudget;
        public double InvestmentBudget => maxBudget;

        public double Expenditure
        {
            get
            {
                var total = 0.0;
                foreach (var budget in budgetNodes)
                {
                    var max = CONVERT.ParseDouble(budget.GetAttribute("maxBudget"));
                    var current = CONVERT.ParseDouble(budget.GetAttribute("budget"));

                    total += max - current;
                }

                return total;
            }
        }

        readonly int targetInfoBatches = 0;
        public int TargetInfoBatches => targetInfoBatches;

        public int MaxInfoBatches => ApplicationsMax + TargetInfoBatches;

        public int NumDeployedServices
        {
            get
            {
                return serviceInstallNameToDeploymentTime.Count(x => !(model.GetNamedNode(x.Key).GetBooleanAttribute("is_auto_installed", false)));
            }
        }

        public int NewServicesHandledBatches
        {
            get;
            private set;
        }


        public int RevenueFromNewServices
        {
            get;
            private set;
        }

        public int TargetNewServicesSales
        {
            get;
            private set;
        }

        public double PercentageOfTargetSales
        {
            get
            {
                if (TargetNewServicesSales <= 0)
                {
                    return 0.0;
                }
                else
                {
                    return ((double)RevenueFromNewServices / (double)TargetNewServicesSales) * 100;
                }
            }
        }

        public double GainOnPreviousRound
        {
            get;
            private set;
        }

        public double NewServiceProfitLoss => Revenue - (SupportCostsTotal + FixedCosts + Expenditure);

        readonly List<Node> budgetNodes;

        readonly Dictionary<string, int> serviceNamesToMaxPotentials;

        public double MaximumPotential
        {
            get
            {
                var potential = 0.0;

                foreach (var value in serviceNamesToMaxPotentials.Values)
                {
                    potential += value;
                }

                return potential;

                //return serviceNamesToMaxPotentials.Values.Aggregate(0.0, (current, value) => current + value);
            }
        }

        public double OpsProfit => SupportBudget - (SupportCostsTotal + FixedCosts + OpsSpend);

        public double OpsSpend => totalBladeCosts;

        double totalBladeCosts;


        public DevOpsRoundScores(NetworkProgressionGameFile gameFile, int round, int previousProfit, int newServices, int previousRevenue, SupportSpendOverrides spendOverrides)
            : base(gameFile, round, previousProfit, newServices, spendOverrides)
        {
            WasUnableToGetData = false;
            try
            {
                model = gameFile.GetNetworkModel(round);
                duration = model.GetNamedNode("CurrentTime").GetIntAttribute("seconds", 0);

                var initialModelFilename = gameFile.GetRoundFile(round, "network_at_start.xml",
                    GameFile.GamePhase.OPERATIONS);

                if (!System.IO.File.Exists(initialModelFilename))
                {
                    initialModelFilename = gameFile.GetRoundFile(round, "network.xml", GameFile.GamePhase.OPERATIONS);
                }

                initialModel = new NodeTree(System.IO.File.ReadAllText(initialModelFilename));

                if (round == 1)
                {
                    GainOnPreviousRound = 0;
                }
                else
                {
                    GainOnPreviousRound = Revenue - previousRevenue;
                }

                // budgetNode = model.GetNamedNode("Budget");
                budgetNodes = model.GetNamedNode("Budgets").GetChildrenWithAttributeValue("round", round.ToString());

                foreach (var budget in budgetNodes)
                {
                    var maxBudgetStr = budget.GetAttribute("maxBudget");
                    if (!string.IsNullOrEmpty(maxBudgetStr))
                    {
                        maxBudget += CONVERT.ParseDouble(maxBudgetStr);
                    }
                }

                var targetSalesNode = model.GetNamedNode("Targets")
                    .GetChildWithAttributeValue("round", round.ToString());

                TargetNewServicesSales = targetSalesNode.GetIntAttribute("target", 0);


                var logReader =
                    new BasicIncidentLogReader(gameFile.GetRoundFile(round, "NetworkIncidents.log",
                        GameFile.GamePhase.OPERATIONS));

                newServicesCommissioned = new List<string>();
                demandsCommissioned = new List<string>();
                newServiceNameToTimeToNetValue = new Dictionary<string, TimeLog<double>>();

                serviceNamesToMaxPotentials = new Dictionary<string, int>();


                var bizName = SkinningDefs.TheInstance.GetData("biz");
                var businessesInOrder = new List<Node>((Node[])
                    gameFile.NetworkModel.GetNodesWithAttributeValue("type", bizName).ToArray(typeof(Node)));

                businessesInOrder.Sort((a, b) => a.GetIntAttribute("order", 0).CompareTo(b.GetIntAttribute("order", 0)));


                var revMade = 0;
                var completedServicesForRound =
                    model.GetNamedNode("CompletedNewServices").GetChildWithAttributeValue("round", round.ToString());
                foreach (var completedService in completedServicesForRound.GetChildrenAsList())
                {
                    revMade += completedService.GetIntAttribute("revenue_made", 0);
                }

                RevenueFromNewServices = revMade;


                serviceInstallNameToStartTime = new Dictionary<string, double>();
                serviceInstallNameToDeploymentTime = new Dictionary<string, double>();
                // Load up the new services so that they can be tracked by the log reader.
                var newServicesNode = model.GetNamedNode("New Services");
                var newServicesRoundNode = newServicesNode.GetChildWithAttributeValue("round", round.ToString());

                foreach (var newService in newServicesRoundNode.GetChildrenWithAttributeValue("type", "New_Service"))
                {
                    // NS MBU # ___
                    var serviceName = newService.GetAttribute("name");
                    // Begin MBU # ___
                    var installName = serviceName.Replace("NS", "Begin");

                    logReader.WatchApplyAttributes(installName, logReader_ServiceApplyAttributes);

                }

                logReader.WatchCreatedNodes("BeginNewServicesInstall", logReader_BeginInstall_CreateNodes);

                logReader.WatchCreatedNodes(CONVERT.Format("Round {0} Completed New Services", round), logReader_CompletedServices_CreateNodes);


                logReader.WatchCreatedNodes("CostedEvents", logReader_CostEvents_CreatedNodes);

                logReader.Run();


                NewServicesHandledBatches = 0;//infoBatchesFromNs.GetIntAttribute("total_info_batches", 0);


                WasUnableToGetData = false;


            }
            catch
            {
                WasUnableToGetData = true;
            }
        }



        void logReader_ServiceApplyAttributes(object sender, string key, string line, double time)
        {
            var name = BasicIncidentLogReader.ExtractValue(line, "i_name");

            if (serviceInstallNameToStartTime.ContainsKey(name))
            {
                var status = BasicIncidentLogReader.ExtractValue(line, "status");

                // Service has been undone or cancelled so remove its start time.
                if (status.Equals("undo") || status.Equals("cancelled"))
                {
                    serviceInstallNameToStartTime.Remove(name);
                }

            }

        }

        void logReader_BeginInstall_CreateNodes(object sender, string key, string line, double time)
        {
            var name = BasicIncidentLogReader.ExtractValue(line, "name");

            serviceInstallNameToStartTime[name] = time;
        }

        void logReader_CompletedServices_CreateNodes(object sender, string key, string line, double time)
        {
            var isAutoInstalled = BasicIncidentLogReader.ExtractBoolValue(line, "is_auto_installed");

            if (isAutoInstalled.HasValue && isAutoInstalled.Value)
            {
                // Don't bother with auto installed apps as it messes with the average.
                return;
            }

            var name = BasicIncidentLogReader.ExtractValue(line, "name");

            var installName = name.Replace("Completed", "Begin");

            serviceInstallNameToDeploymentTime[installName] = (time - serviceInstallNameToStartTime[installName]);
        }

        void LogReader_Demand_ApplyAttributes(object sender, string key, string line, double time)
        {
            var demandName = BasicIncidentLogReader.ExtractValue(line, "i_name");
            var demand = model.GetNamedNode(demandName);
            var businessServiceName = demand.GetAttribute("business_service");
            var startedString = BasicIncidentLogReader.ExtractValue(line, "started");
            if ((!string.IsNullOrEmpty(startedString)) && CONVERT.ParseBool(startedString, false))
            {
                if (!demandsCommissioned.Contains(businessServiceName))
                {
                    demandsCommissioned.Add(businessServiceName);
                }
            }
        }

        void logReader_CostEvents_CreatedNodes(object sender, string key, string line, double time)
        {
            var type = BasicIncidentLogReader.ExtractValue(line, "type");

            if (type.Equals("service_potential"))
            {
                var name = BasicIncidentLogReader.ExtractValue(line, "service_name");

                var potentialString = BasicIncidentLogReader.ExtractValue(line, "service_potential");

                if (string.IsNullOrEmpty(potentialString))
                {
                    throw new Exception("Potential value missing for " + name);
                }

                var potential = CONVERT.ParseInt(potentialString);

                serviceNamesToMaxPotentials[name] = potential;

            }
            else if (type == "blade_cost")
            {
                var bladeCost = BasicIncidentLogReader.ExtractIntValue(line, "cost");

                if (!bladeCost.HasValue)
                {
                    throw new Exception("Blade cost CostedEvent is missing its cost");
                }

                totalBladeCosts += bladeCost.Value;
            }
        }

        public void Dispose()
        {
            model = null;
            initialModel = null;
            //nameToTurnoverItem.Clear();

            newServicesCommissioned.Clear();
            demandsCommissioned.Clear();
            newServiceNameToTimeToNetValue.Clear();

        }

    }
}
