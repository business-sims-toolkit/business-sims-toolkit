using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Linq;

using CoreUtils;

using LibCore;
using GameManagement;
using Network;
using ReportBuilder;
using Logging;

namespace DevOps.ReportsScreen
{
    public class DevOpsRoundScores : RoundScores, IDisposable
    {
        public bool WasUnableToGetData = false;

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

            public TurnoverItem(string logLine)
            {
                logLine = BaseUtils.xml_utils.TranslateFromEscapedXMLChars(logLine);
                attributes = ExtractAttributesFromLogLine(logLine, true);
                time = CONVERT.ParseDouble(BasicIncidentLogReader.ExtractValue(logLine, "i_doAfterSecs"));
            }

            public TurnoverItem (Node node)
            {
                attributes = new List<AttributeValuePair>();
                foreach (AttributeValuePair attributeValuePair in node.GetAttributes())
                {
                    attributes.Add(attributeValuePair);
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

            static List<AttributeValuePair> ExtractAttributesFromLogLine( string logLine, bool isCreateLine)
            {
                List<AttributeValuePair> attributes = new List<AttributeValuePair>();

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

                List<string> parts = new List<string>();
                StringBuilder currentPart = new StringBuilder();
                bool inQuotes = false;
                foreach (char c in chopped)
                {
                    bool handled = false;

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

                if(currentPart.Length > 0)
                {
                    parts.Add(currentPart.ToString());
                    currentPart = new StringBuilder();
                }

                for (int i = 0; i < parts.Count; i++)
                {
                    string [] components = parts[i].Split('=');
                    string attributeName = components[0];

                    if (!attributeName.StartsWith("i_"))
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

            public void SetAttribute(string attributeName, string value)
            {
                AttributeValuePair avp = GetAttributeValuePair(attributeName);
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

        public double TargetRevenue
        {
            // Change to be the same as TargetInfoBatches
            get;
            private set;
        }        

		List<string> newServicesCommissioned;
		List<string> demandsCommissioned;        
        
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

        public double AverageDeployTime
        {
            get
            {
                if (serviceInstallNameToDeploymentTime.Count == 0)
                {
                    return 0.0;
                }

                int counter = 0;
                double sum = 0.0;
                foreach (string key in serviceInstallNameToDeploymentTime.Keys)
                {
                    sum += serviceInstallNameToDeploymentTime[key];
                    counter++;
                }

                return (sum / counter);
            }
        }


        

        
        Dictionary<string, double> serviceInstallNameToStartTime;
        Dictionary<string, double> serviceInstallNameToDeploymentTime;

        double maxBudget;
        public double InvestmentBudget
        {
            get
            {
                return maxBudget;
            }
        }

        public double Expenditure
        {
            get
            {
                double total = 0.0;
                foreach (Node budget in budgetNodes)
                {
                    double max = CONVERT.ParseDouble(budget.GetAttribute("maxBudget"));
                    double current = CONVERT.ParseDouble(budget.GetAttribute("budget"));
                    
                    total += max - current;
                }

                return total;
            }
        }

        int targetInfoBatches = 0;
        public int TargetInfoBatches
        { 
            get
            {
                return targetInfoBatches;
            }
        }

        public int MaxInfoBatches
        {
            get
            {
                return ApplicationsMax + TargetInfoBatches;
            }
        }

        public int NumDeployedServices
        {
            get
            {
                return serviceInstallNameToDeploymentTime.Count(x => ! (model.GetNamedNode(x.Key).GetBooleanAttribute("is_auto_installed", false)));
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
                    return ((double) RevenueFromNewServices / (double) TargetNewServicesSales) * 100;
                }
            }
        }

        public double GainOnPreviousRound
        {
            get;
            private set;
        }

        public double NewServiceProfitLoss
        {
            // actual revenue - (support costs + fixed costs + expenditure (new apps)
            get
            {
                return Revenue - (SupportCostsTotal + FixedCosts + Expenditure);
            }
            
        }


        List<Node> budgetNodes;


        Dictionary<string, int> serviceNamesToMaxPotentials;

        public double MaximumPotential
        {
            get
            {
                double potential = 0.0;

                foreach (int value in serviceNamesToMaxPotentials.Values)
                {
                    potential += value;
                }

                return potential;

                //return serviceNamesToMaxPotentials.Values.Aggregate(0.0, (current, value) => current + value);
            }
        }

        public double OpsProfit
        {   // SupportBudget
            // Budget - (support + fixed + spend)
            get
            {
                return SupportBudget - (SupportCostsTotal + FixedCosts + OpsSpend);
            }
        }

        public double OpsSpend
        {
            get
            {
                return totalBladeCosts;
            }
        }
        
        double totalBladeCosts;
        

        public DevOpsRoundScores (NetworkProgressionGameFile gameFile, int round, int previousProfit, int newServices, int previousRevenue, SupportSpendOverrides spendOverrides)
            : base(gameFile, round, previousProfit, newServices, spendOverrides)
        {
            WasUnableToGetData = false;
            try
            {
                model = gameFile.GetNetworkModel(round);
                duration = model.GetNamedNode("CurrentTime").GetIntAttribute("seconds", 0);
                
                string initialModelFilename = gameFile.GetRoundFile(round, "network_at_start.xml",
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

                foreach(Node budget in budgetNodes)
                {
                    string maxBudgetStr = budget.GetAttribute("maxBudget");
                    if (!string.IsNullOrEmpty(maxBudgetStr))
                    {
                        maxBudget += CONVERT.ParseDouble(maxBudgetStr);
                    }
                }

                Node targetSalesNode = model.GetNamedNode("Targets")
                    .GetChildWithAttributeValue("round", round.ToString());

                TargetNewServicesSales = targetSalesNode.GetIntAttribute("target", 0);
                

                BasicIncidentLogReader logReader =
                    new BasicIncidentLogReader(gameFile.GetRoundFile(round, "NetworkIncidents.log",
                        GameFile.GamePhase.OPERATIONS));
                
                newServicesCommissioned = new List<string>();
                demandsCommissioned = new List<string>();
                newServiceNameToTimeToNetValue = new Dictionary<string, TimeLog<double>>();

                serviceNamesToMaxPotentials = new Dictionary<string, int>();
                

                string bizName = SkinningDefs.TheInstance.GetData("biz");
                List<Node> businessesInOrder = new List<Node>((Node[])
                    gameFile.NetworkModel.GetNodesWithAttributeValue("type", bizName).ToArray(typeof(Node)));
                
                businessesInOrder.Sort((a, b) => a.GetIntAttribute("order", 0).CompareTo(b.GetIntAttribute("order", 0)));
                

                int revMade = 0;
                Node completedServicesForRound =
                    model.GetNamedNode("CompletedNewServices").GetChildWithAttributeValue("round", round.ToString());
                foreach (Node completedService in completedServicesForRound.GetChildrenAsList())
                {
                    revMade += completedService.GetIntAttribute("revenue_made", 0);
                }
                
                RevenueFromNewServices = revMade;
                
                
                serviceInstallNameToStartTime = new Dictionary<string, double>();
                serviceInstallNameToDeploymentTime = new Dictionary<string, double>();
                // Load up the new services so that they can be tracked by the log reader.
                Node newServicesNode = model.GetNamedNode("New Services");
                Node newServicesRoundNode = newServicesNode.GetChildWithAttributeValue("round", round.ToString());

                foreach (Node newService in newServicesRoundNode.GetChildrenWithAttributeValue("type", "New_Service"))
                {
                    // NS MBU # ___
                    string serviceName = newService.GetAttribute("name");
                    // Begin MBU # ___
                    string installName = serviceName.Replace("NS", "Begin");

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
            string name = BasicIncidentLogReader.ExtractValue(line, "i_name");

            if (serviceInstallNameToStartTime.ContainsKey(name))
            {
                string status = BasicIncidentLogReader.ExtractValue(line, "status");

                // Service has been undone or cancelled so remove its start time.
                if (status.Equals("undo") || status.Equals("cancelled"))
                {
                    serviceInstallNameToStartTime.Remove(name);
                }
                
            }
           
        }

        void logReader_BeginInstall_CreateNodes(object sender, string key, string line, double time)
        {
            string name = BasicIncidentLogReader.ExtractValue(line, "name");
            
            serviceInstallNameToStartTime[name] = time;
        }

        void logReader_CompletedServices_CreateNodes(object sender, string key, string line, double time)
        {
            bool? isAutoInstalled = BasicIncidentLogReader.ExtractBoolValue(line, "is_auto_installed");

            if (isAutoInstalled.HasValue && isAutoInstalled.Value)
            {
                // Don't bother with auto installed apps as it messes with the average.
                return;
            }

            string name = BasicIncidentLogReader.ExtractValue(line, "name");

            string installName = name.Replace("Completed", "Begin");

            serviceInstallNameToDeploymentTime[installName] = (time - serviceInstallNameToStartTime[installName]);
        }

        void LogReader_Demand_ApplyAttributes(object sender, string key, string line, double time)
        {
            string demandName = BasicIncidentLogReader.ExtractValue(line, "i_name");
            Node demand = model.GetNamedNode(demandName);
            string businessServiceName = demand.GetAttribute("business_service");
            string startedString = BasicIncidentLogReader.ExtractValue(line, "started");
            if ((! string.IsNullOrEmpty(startedString)) && CONVERT.ParseBool(startedString).Value)
            {
                if (! demandsCommissioned.Contains(businessServiceName))
                {
                    demandsCommissioned.Add(businessServiceName);
                }
            }
        }

        void logReader_CostEvents_CreatedNodes(object sender, string key, string line, double time)
        {
            string type = BasicIncidentLogReader.ExtractValue(line, "type");

            if (type.Equals("service_potential"))
            {
                string name = BasicIncidentLogReader.ExtractValue(line, "service_name");

                string potentialString = BasicIncidentLogReader.ExtractValue(line, "service_potential");

                if (string.IsNullOrEmpty(potentialString))
                {
                    throw new Exception("Potential value missing for " + name);
                }

                int potential = CONVERT.ParseInt(potentialString);

                serviceNamesToMaxPotentials[name] = potential;

            }
            else if (type == "blade_cost")
            {
                int? bladeCost = BasicIncidentLogReader.ExtractIntValue(line, "cost");

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
