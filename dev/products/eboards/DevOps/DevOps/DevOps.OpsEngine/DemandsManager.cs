using System;
using System.Collections;
using System.Collections.Generic;
using CommonGUI;

using LibCore;

using Network;

namespace DevOps.OpsEngine
{
    public class DemandsManager:IDisposable
    {
	    NodeTree model;
	    Node demandsNode;
	    Node timeNode;
	    Node beginDemandsInstallHead;
	    Node demandsCompleted;
	    Node demandsRemoved;

	    int maxInstallTime = 60;
	    int messageRemoveTime = 60;
	    List<Node> optionalDemandsSelected; 

        public List<Node> OptionalDemandsList
        {
            get
            {
                return optionalDemandsSelected;
            }
        }

        public DemandsManager(NodeTree nt)
        {
            this.model = nt;

            timeNode = model.GetNamedNode("CurrentTime");
            timeNode.AttributesChanged += TimeNodeAttributesChangedForHandlingDemands;
            timeNode.AttributesChanged += TimeNodeAttributesChangedForNewDemandsInstall;

            demandsNode = model.GetNamedNode("Demands");
            demandsNode.ChildAdded += NewDemandsNodeAdded;
            demandsNode.ChildRemoved += NewDemandsNodeRemoved;
            foreach (Node demandsAlreadyExisting in demandsNode)
            {
                demandsAlreadyExisting.AttributesChanged += DemandsUpdated;
            }
            
            demandsCompleted = model.GetNamedNode("DemandsCompleted");
            demandsRemoved = model.GetNamedNode("DemandsRemoved");
            demandsRemoved.DeleteChildren();


            beginDemandsInstallHead = model.GetNamedNode("BeginDemandsInstall");
            beginDemandsInstallHead.DeleteChildren();
            beginDemandsInstallHead.ChildAdded += MonitorDemandsWhenAdded;

            optionalDemandsSelected = new List<Node>();
        }

        public void Dispose()
        {
            timeNode.AttributesChanged -= TimeNodeAttributesChangedForNewDemandsInstall;
            timeNode.AttributesChanged -= TimeNodeAttributesChangedForHandlingDemands;

            demandsNode.ChildAdded -= NewDemandsNodeAdded;
            demandsNode.ChildRemoved -= NewDemandsNodeRemoved;

            List<Node> demandsList = demandsNode.GetChildrenAsList();
            foreach (Node child in demandsList)
            {
                child.AttributesChanged -= DemandsUpdated;
            }

            beginDemandsInstallHead.ChildAdded -= MonitorDemandsWhenAdded;
            beginDemandsInstallHead.ChildRemoved -= MonitorDemandsWhenRemoved;

            optionalDemandsSelected.Clear();
            demandsNode.DeleteChildren();
        }

        public delegate void DemandStatusDisplayHandler(string mbu, string serviceName,string demandId, string status, bool isHidden);
        public event DemandStatusDisplayHandler DemandStatusReceived;


        void OnDemandStatus(string mbu, string serviceName, string demandId, string status, bool isHidden)
        {
            if (DemandStatusReceived != null)
            {
                DemandStatusReceived(mbu, serviceName, demandId, status, isHidden);
            }
        }

	    void MonitorDemandsWhenAdded(Node parent, Node child)
        {
            child.AttributesChanged += MonitorDemandsWhenStatusChanges;
            OnDemandStatus(child.GetAttribute("mbu"), child.GetAttribute("serviceImpacted"), child.GetAttribute("demandId"), child.GetAttribute("status"), child.GetBooleanAttribute("hideMessage", false));
        }

	    void MonitorDemandsWhenRemoved(Node parent, Node child)
        {
            child.AttributesChanged -= MonitorDemandsWhenStatusChanges;
        }

	    void MonitorDemandsWhenStatusChanges(Node sender, ArrayList attributes)
        {
            OnDemandStatus(sender.GetAttribute("mbu"), sender.GetAttribute("serviceImpacted"), sender.GetAttribute("demandId"), sender.GetAttribute("status"), sender.GetBooleanAttribute("hideMessage", false));
        }

        void NewDemandsNodeRemoved(Node sender, Node child)
        {
            int infoPerTrans = child.GetIntAttribute("information_per_trans", 0);
            int instances = child.GetIntAttribute("instances", 1);

            OnDemandStatusUpdated("Demand " + child.GetAttribute("demandId"), child.GetAttribute("service_id"), Convert.ToString(infoPerTrans * instances), child.GetAttribute("startTime"),
                    child.GetAttribute("status"), child.GetAttribute("mbu"), true);
            child.AttributesChanged -= DemandsUpdated;
        }

        public delegate void DemandsStatusHandler(string demandId,string serviceId, string infoPerTrans, string startTime, string status, string mbu, bool hideMessage);
        public event DemandsStatusHandler DemandsStatusUpdated;

        void OnDemandStatusUpdated(string demandId, string serviceId, string infoPerTrans, string startTime, string status, string mbu, bool hideMessage)
        {
            if (DemandsStatusUpdated != null)
            {
                DemandsStatusUpdated(demandId, serviceId, infoPerTrans, startTime, status, mbu, hideMessage);
            }
        }

        void NewDemandsNodeAdded(Node sender, Node child)
        {
            child.AttributesChanged += DemandsUpdated;
            int currentTime = GetCurrentTime();
            
            if (child.GetAttribute("optional").Equals(Boolean.FalseString) || IsOptionalDemandSelected(child.GetAttribute("name")))
            {
                if (!child.GetAttribute("instances").Equals("0"))
                {
                    string file = AppInfo.TheInstance.Location + "\\audio\\woopwoop.wav";
                    KlaxonSingleton.TheInstance.PlayAudio(file, false);
                }
            }
        }

        void DemandsUpdated(Node sender, ArrayList attributes)
        {
            int infoPerTrans = sender.GetIntAttribute("information_per_trans",0);
            int instances = sender.GetIntAttribute("instances", 1);

            OnDemandStatusUpdated("Demand " + sender.GetAttribute("demandId"), sender.GetAttribute("service_id"), Convert.ToString(infoPerTrans * instances), sender.GetAttribute("startTime"),
                    sender.GetAttribute("status"), sender.GetAttribute("mbu"), sender.GetBooleanAttribute("hideMessage", false));
        }

	    bool IsDemandCodeActive(string mbu , string code, string demandId)
        {
            Node demands = model.GetNamedNode("Demands");
            List<Node> demandList = demands.GetChildrenAsList();

            foreach (Node demand in demandList)
            {
                //if (demand.GetAttribute("mbu").Equals(mbu) && demand.GetAttribute("service_id").Equals(code) &&
                //    !demand.GetAttribute("instances").Equals("0") && demand.GetBooleanAttribute("optional", false).Equals(false) && demand.GetBooleanAttribute("active", true).Equals(true))
                if (demand.GetAttribute("mbu").Equals(mbu) && demand.GetAttribute("service_id").Equals(code) && demand.GetAttribute("demandId").Equals(demandId) && 
                    !demand.GetAttribute("instances").Equals("0") && demand.GetBooleanAttribute("active", true).Equals(true) &&
                    !demand.GetAttribute("status").Equals("missed"))
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsDemandAlreadyDeployed(string mbu,string code, string demandId)
        {
            List<Node> beginDemandsInstallList = beginDemandsInstallHead.GetChildrenAsList();

            foreach (Node demand in beginDemandsInstallList)
            {
                if (demand.GetAttribute("service_id").Equals(code) && demand.GetAttribute("mbu").Equals(mbu) && demand.GetAttribute("demandId").Equals(demandId))
                {
                    if (demand.GetIntAttribute("installTimeLeft").HasValue)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public bool IsDemandAlreadyExist(string mbuSelected, string code, string demandIdSelected)
        {
            List<Node> beginDemandsInstallList = beginDemandsInstallHead.GetChildrenAsList();

            foreach (Node demand in beginDemandsInstallList)
            {
                if (demand.GetAttribute("mbu").Equals(mbuSelected) && demand.GetAttribute("service_id").Equals(code) && demand.GetAttribute("demandId").Equals(demandIdSelected) && !demand.GetAttribute("status").Equals("missed"))
                {
                    return true;
                }
            }
            return false;
        }
        public Dictionary<string,string> GetDemandsForMbu(string mbu, int round)
        {
            Node demandCodes = model.GetNamedNode("DemandCodes");
            string status = string.Empty;

            Dictionary<string, string> demandsDictionary = new Dictionary<string, string>();
            
            foreach (Node demandCode in demandCodes)
            {
                if (demandCode.GetIntAttribute("round", 1) == round)
                {
                    string code = demandCode.GetAttribute("name").Split(" ".ToCharArray())[4];
                    string demandId = demandCode.GetAttribute("name").Split(" ".ToCharArray())[1];

                    if (IsDemandCodeActive(mbu, code, demandId) && CanDemandInstallInThisRound(mbu, code, round))
                    {
                        if (IsDemandAlreadyDeployed(mbu, code, demandId))
                        {
                            status = "deployed";
                        }
                        else if (IsDemandAlreadyExist(mbu, code, demandId))
                        {
                            status = "waiting";
                        }
                        else
                        {
                            status = "active";
                        }
                    }
                    else
                    {
                        status = "inactive";
                    }

                    demandsDictionary.Add(demandId + " " + code, status);
                }
            }
            return demandsDictionary;
        }

	    bool CanDemandInstallInThisRound(string mbuSelected , string code, int round)
        {
            //checking to see if the demand can be installed in this round. For this we need to check if BSU needs to be checked. 
            string bizName = GetServiceImpactedFromServiceCode(mbuSelected,code,round);
            Node bizServiceUser = model.GetNamedNode(mbuSelected + " " + bizName);

            if (bizServiceUser == null)
            {
                return false;
            }
            return true;
        }

        public void AddToDemandCodes(string optionalDemandName, int round)
        {
            Node optionalDemand = model.GetNamedNode(optionalDemandName);
            Node demandCodes = model.GetNamedNode("DemandCodes");
            string name = "Demand " + optionalDemand.GetAttribute("demandId") + " " +  "Round " + round.ToString() + " " + optionalDemand.GetAttribute("service_id");

            if (!IsDemandCodeExist(name))
            {
                new Node(demandCodes, "demandCode", name, new List<AttributeValuePair>
                {
                    new AttributeValuePair("type","demandCode"),
                    new AttributeValuePair("round",round),
                    new AttributeValuePair("demandId",optionalDemand.GetAttribute("demandId"))
                });
            }
        }

        public void RemoveFromDemandCodes(string optionalDemandName, int round)
        {
            Node optionalDemand = model.GetNamedNode(optionalDemandName);
            Node demandCodes = model.GetNamedNode("DemandCodes");
            string name = "Demand " + optionalDemand.GetAttribute("demandId") + " " + "Round " + round.ToString() + " " + optionalDemand.GetAttribute("service_id");

            if (IsDemandCodeExist(name))
            {
                demandCodes.DeleteChildTree(model.GetNamedNode(name));
            }
        }

        public bool IsDemandCodeExist(string name)
        {
            Node demandCodeNode = model.GetNamedNode(name);

            if (demandCodeNode != null)
            {
                return true;
            }

            return false;
        }

        public void StartTimerForSelectedDemand(string mbuSelected, string demandSelected,string demandIdSelected, int round)
        {
            List<Node> demandList = demandsNode.GetChildrenAsList();

            int serviceCost = 0;
            foreach (Node demand in demandList)
            {
                if (demand.GetAttribute("mbu").Equals(mbuSelected) && demand.GetAttribute("service_id").Equals(demandSelected) && demand.GetAttribute("demandId").Equals(demandIdSelected) && !demand.GetAttribute("status").Equals("missed") && demand.GetIntAttribute("round", 1).Equals(round)) 
                {
                    Node test = new Node(beginDemandsInstallHead, "BeginDemandsInstall", "Begin Demand " + demand.GetAttribute("demandId") + " " + mbuSelected + " " + demand.GetAttribute("serviceImpacted"), new List<AttributeValuePair>()
                    {
                        new AttributeValuePair("type","BeginDemandsInstall"),
                        new AttributeValuePair("service_id", demand.GetAttribute("service_id")),
                        new AttributeValuePair("mbu", mbuSelected),
                        new AttributeValuePair("enclosure", string.Empty),
                        new AttributeValuePair("bladeCost", demand.GetIntAttribute("bladeCost",0)),
                        new AttributeValuePair("vm_id",demand.GetAttribute("vm_id")),
                        new AttributeValuePair("round", round),
                        new AttributeValuePair("status", "waiting"),
                        new AttributeValuePair("demandId",demand.GetAttribute("demandId")),
                        new AttributeValuePair("serviceImpacted",demand.GetAttribute("serviceImpacted")),
                        new AttributeValuePair("instances",demand.GetAttribute("instances")),
                        new AttributeValuePair("optional",demand.GetAttribute("optional")),
                        new AttributeValuePair("information_per_trans",demand.GetAttribute("information_per_trans")),
                        new AttributeValuePair("duration",demand.GetAttribute("duration")),
                        new AttributeValuePair("cpu",GetNumberOfCpuFromVm(demand.GetAttribute("vm_id"))),
                        new AttributeValuePair("gantt_order", GetGanttOrderFromServiceCode(demandSelected,round)),
                        new AttributeValuePair("investment",demand.GetAttribute("investment")),
                        new AttributeValuePair("startTime",demand.GetAttribute("startTime")),
                        new AttributeValuePair("prepDuration",demand.GetAttribute("prepDuration")),
                        new AttributeValuePair("channel",demand.GetAttribute("channel")),
                        new AttributeValuePair("fixable",true),
                        new AttributeValuePair("canupgrade",true),
                        new AttributeValuePair("userupgrade",false),
                        new AttributeValuePair("up",true),
                        new AttributeValuePair("proczone",1),
                        new AttributeValuePair("zone",1),
                        new AttributeValuePair("usernode",false),
                        new AttributeValuePair("propagate",true),
                        new AttributeValuePair("platform","X"),
                        new AttributeValuePair("version",1)
                    });
                    serviceCost = demand.GetIntAttribute("investment", 0);
                }
            }
            //adjust Budget but bladecost needs to be done at the next step
            AdjustBudget(serviceCost,0,mbuSelected,round);
        }

        public int GetVmInstances(string mbuSelected, string demandSelected, string demandIdSelected)
        {
            List<Node> demandList = demandsNode.GetChildrenAsList();

            foreach (Node demand in demandList)
            {
                if (demand.GetAttribute("mbu").Equals(mbuSelected) &&
                    demand.GetAttribute("service_id").Equals(demandSelected) &&
                    demand.GetAttribute("demandId").Equals(demandIdSelected))
                {
                    return Convert.ToInt32(demand.GetAttribute("instances"));
                }
            }

            return 0;
        }

        public bool IsThereCapacityInEnclosure(string vmSelected, string enclosureSelected, int instances)
        {
            Node vm = model.GetNamedNode(vmSelected);

            Node enclosureNode = model.GetNamedNode(enclosureSelected);
            
            int cpuRequired = Convert.ToInt32(vm.GetAttribute("cpu"));
            int freeCpu = Convert.ToInt32(enclosureNode.GetAttribute("free_cpu"));

            if (freeCpu >= cpuRequired*instances)
            {
                return true;
            }

            return false;
        }

        public void PrepareDemandForInstall(string mbuSelected, string demandSelected, string demandIdSelected, string vmSelected, string enclosureSelected, int bladeCost, int round)
        {
            List<Node> beginDemandChildren = beginDemandsInstallHead.GetChildrenAsList();

            int vminstances = 0;
            foreach (Node beginDemandChild in beginDemandChildren)
            {
                if (beginDemandChild.GetAttribute("mbu").Equals(mbuSelected) && beginDemandChild.GetAttribute("service_id").Equals(demandSelected) && 
                    beginDemandChild.GetAttribute("demandId").Equals(demandIdSelected) && 
                    beginDemandChild.GetAttribute("vm_id").Equals(vmSelected))
                {
                    beginDemandChild.SetAttribute("enclosure", enclosureSelected);
                    beginDemandChild.SetAttribute("installTimeLeft", maxInstallTime); //set timer for countdown
                    beginDemandChild.SetAttribute("bladeCost", bladeCost);
                    beginDemandChild.SetAttribute("status","transitioning");
                    vminstances = beginDemandChild.GetIntAttribute("instances", 1);
                }
            }

            //modify capacity of enclosures
            Node enclosureNode = model.GetNamedNode(enclosureSelected);
            Node vmNode = model.GetNamedNode(vmSelected);

            int? cpuRequired = vmNode.GetIntAttribute("cpu");
            int? freeCpu = enclosureNode.GetIntAttribute("free_cpu");
            int? usedCpu = enclosureNode.GetIntAttribute("used_cpu");

            if (cpuRequired.HasValue && freeCpu.HasValue && usedCpu.HasValue)
            {
                enclosureNode.SetAttribute("free_cpu", freeCpu.Value - cpuRequired.Value*vminstances);
                enclosureNode.SetAttribute("used_cpu", usedCpu.Value + cpuRequired.Value*vminstances);
            }

            //adjusting only blade cost if necessary - serviceCost was handled in previous step
            AdjustBudget(0, bladeCost,mbuSelected, round);
        }

        public bool CanAddBladeToServer(string enclosureSelected,string vmSelected, int vmInstances)
        {
            Node enclosureNode = model.GetNamedNode(enclosureSelected);
            Node blade = model.GetNamedNode("blade");
            double vmCpu = Convert.ToDouble(model.GetNamedNode(vmSelected).GetAttribute("cpu"));
            int freeCpu = enclosureNode.GetIntAttribute("free_cpu",0);
            int freeHeight = Convert.ToInt32(enclosureNode.GetAttribute("free_height").Trim());

            int bladesRequired = Convert.ToInt32(Math.Ceiling((vmCpu*vmInstances)/Convert.ToDouble(blade.GetAttribute("cpu"))));
           
            if (freeHeight >= bladesRequired)
            {
                return true;
            }

            return false;
        }

	    string GetServiceImpactedFromServiceCode(string mbuSelected, string demandSelected, int round)
        {
            List<Node> demandList = demandsNode.GetChildrenAsList();

            foreach (Node demand in demandList)
            {
                if (demand.GetAttribute("service_id").Equals(demandSelected)&& demand.GetAttribute("round").Equals(round.ToString()))
                {
                    if (demand.GetAttribute("name").Contains(mbuSelected))
                    {
                        return demand.GetAttribute("serviceImpacted");
                    }
                }
            }

            return "";
        }

        public bool IsVmCompatible(string mbuSelected, string demandSelected, string demandIdSelected,string vmSelected,int round)
        {
            List<Node> demandList = beginDemandsInstallHead.GetChildrenAsList();

            foreach (Node demand in demandList)
            {
                if (demand.GetAttribute("mbu").Equals(mbuSelected) && demand.GetAttribute("service_id").Equals(demandSelected) &&
                    demand.GetAttribute("demandId").Equals(demandIdSelected) && demand.GetAttribute("vm_id").Equals(vmSelected) && 
                    demand.GetAttribute("round").Equals(round.ToString()))
                {
                    return true;
                }
            }
            
            return false;
        }

        public int AddBladeToServer(string enclosureSelected,string vmSelected, int vmInstances)
        {
            Node blade = model.GetNamedNode("blade");
            Node enclosureNode = model.GetNamedNode(enclosureSelected);

            int totalCpu = Convert.ToInt32(enclosureNode.GetAttribute("total_cpu"));
            int usedCpu = Convert.ToInt32(enclosureNode.GetAttribute("used_cpu"));
            int freeHeight = Convert.ToInt32(enclosureNode.GetAttribute("free_height"));
            int freeCpu = enclosureNode.GetIntAttribute("free_cpu", 0);

            int bladeCpu = Convert.ToInt32(blade.GetAttribute("cpu"));
            int bladeCost = blade.GetIntAttribute("cost", 0);
            
            double vmCpu = Convert.ToDouble(model.GetNamedNode(vmSelected).GetAttribute("cpu"));

            int bladesRequired = Convert.ToInt32(Math.Ceiling((vmCpu * vmInstances) / Convert.ToDouble(bladeCpu)));
            
            totalCpu += (bladeCpu*bladesRequired);
            freeCpu = totalCpu - usedCpu;
            freeHeight -= bladesRequired;

            //apply new values to nodes
            enclosureNode.SetAttribute("total_cpu", totalCpu);
            enclosureNode.SetAttribute("free_cpu", freeCpu);
            enclosureNode.SetAttribute("free_height", freeHeight);

            return bladeCost*bladesRequired;
        }

        void TimeNodeAttributesChangedForNewDemandsInstall(Node sender, ArrayList attributes)
        {
            List<Node> demandsWaitingToBeInstalled = beginDemandsInstallHead.GetChildrenAsList();

            foreach (Node demandToBeInstalled in demandsWaitingToBeInstalled)
            {
                //first decrement install time
                int? timeLeftToInstall = demandToBeInstalled.GetIntAttribute("installTimeLeft");

                if (timeLeftToInstall.HasValue)
                {
                    if (timeLeftToInstall == 0)
                    {
                        //this condition is if demand is installed in 59th second then transitioning will finish on the 59th second therefore we can have a situation
                        //where the demand window has already been closed. If that is the case then don't bother installing.
                        Node demandNode = GetDemandNodeFromBeginDemandInstallNode(demandToBeInstalled.GetAttribute("mbu"),demandToBeInstalled.GetAttribute("service_id"),demandToBeInstalled.GetAttribute("vm_id"),demandToBeInstalled.GetAttribute("demandId"));
                        if (!demandNode.GetAttribute("active").Equals(Boolean.FalseString))
                        {
                            InstallDemands(demandToBeInstalled.GetAttribute("mbu"),
                                demandToBeInstalled.GetAttribute("service_id"),
                                demandToBeInstalled.GetAttribute("vm_id"), demandToBeInstalled.GetAttribute("enclosure"),
                                demandToBeInstalled.GetAttribute("demandId"),
                                demandToBeInstalled.GetIntAttribute("round", 1));

                            timeLeftToInstall--;
                            demandToBeInstalled.SetAttribute("installTimeLeft", timeLeftToInstall.Value);
                        }
                        else if (demandNode.GetAttribute("active").Equals(Boolean.FalseString))
                        {
                            //if it were transitioning then do some housekeeping
                            demandToBeInstalled.SetAttribute("status", "deployed"); 
                            demandToBeInstalled.SetAttribute("installTimeLeft",-2);
                        }
                    }
                    else if (timeLeftToInstall > 0)
                    {
                        timeLeftToInstall--;
                        demandToBeInstalled.SetAttribute("installTimeLeft", timeLeftToInstall.Value);
                    }
                }
            }
        }

        void TimeNodeAttributesChangedForHandlingDemands(Node sender, ArrayList attributes)
        {
            if (demandsNode != null )
            {
                List<Node> demandsList = demandsNode.GetChildrenAsList();

                foreach (Node demandNode in demandsList)
                {
                    if(demandNode.GetBooleanAttribute("optional", false).Equals(false) || IsOptionalDemandSelected(demandNode.GetAttribute("name")))
                    {
                        if (demandNode.GetIntAttribute("instances").Value != 0)
                        {
                            int? prepDuration = demandNode.GetIntAttribute("prepDuration");

                            //start the duration countdown;
                            if (prepDuration.HasValue)
                            {
                                if (prepDuration == 0)
                                {
                                    if (!IsDemandDeployed(demandNode.GetAttribute("mbu"), demandNode.GetAttribute("service_id"), demandNode.GetAttribute("demandId"), demandNode.GetAttribute("round")))
                                    {
                                        demandNode.SetAttribute("status", "missed");
                                        demandNode.SetAttribute("active", false);
                                        prepDuration--;
                                        demandNode.SetAttribute("prepDuration", prepDuration.Value);

                                        Node beginInstallDemandNode = GetBeginDemandInstallNode(demandNode.GetAttribute("mbu"), demandNode.GetAttribute("service_id"), demandNode.GetAttribute("demandId"));
                                        if (beginInstallDemandNode != null)
                                        {
                                            beginInstallDemandNode.SetAttribute("InstallTimeLeft", -2); //Disable any left over time as this demand has now ended.
                                        }
                                    }
                                    else
                                    {
                                        demandNode.SetAttribute("status", "met");

                                        Node beginInstallDemandNode = GetBeginDemandInstallNode(demandNode.GetAttribute("mbu"), demandNode.GetAttribute("service_id"), demandNode.GetAttribute("demandId"));
                                        if (beginInstallDemandNode.GetAttribute("status").Equals("deployed") &&
                                            beginInstallDemandNode.GetAttribute("installTimeLeft").Equals("-1"))
                                        {
                                            AdjustInfoPerTransactions(beginInstallDemandNode.GetIntAttribute("information_per_trans", 0), beginInstallDemandNode.GetAttribute("mbu"), beginInstallDemandNode.GetAttribute("channel"), beginInstallDemandNode.GetIntAttribute("instances", 1));
                                            beginInstallDemandNode.SetAttribute("installTimeLeft", -2);
                                        }
                                        if (HandleDemandsWindow(demandNode))
                                        {
                                            //when demand is handled stop processing this node this far
                                            prepDuration = -messageRemoveTime;
                                            demandNode.SetAttribute("prepDuration", prepDuration.Value);
                                        }
                                    }
                                }
                                else if (prepDuration > 0)
                                {
                                    demandNode.SetAttribute("active", true); //This will allow UI to show demand message

                                    if (IsDemandDeployed(demandNode.GetAttribute("mbu"), demandNode.GetAttribute("service_id"), demandNode.GetAttribute("demandId"), demandNode.GetAttribute("round")))
                                    {
                                        //success
                                        demandNode.SetAttribute("status", "met");
                                    }

                                    prepDuration--;
                                    demandNode.SetAttribute("prepDuration", prepDuration.Value);
                                }
                                else if ((prepDuration < 0) && (prepDuration >= (-messageRemoveTime)))
                                {
                                    if (prepDuration == -messageRemoveTime)
                                    {
                                        demandNode.SetAttribute("hideMessage", Boolean.TrueString);
                                        model.MoveNode(demandNode, demandsCompleted);

                                        Node beginInstallDemandNode = GetBeginDemandInstallNode(demandNode.GetAttribute("mbu"), demandNode.GetAttribute("service_id"), demandNode.GetAttribute("demandId"));
                                        if (beginInstallDemandNode != null)
                                        {
                                            if (beginInstallDemandNode.GetAttribute("status").Equals("deployed") &&
                                                beginInstallDemandNode.GetAttribute("installTimeLeft").Equals("-2"))
                                            {
                                                AdjustInfoPerTransactionsBack(beginInstallDemandNode);
                                            }
                                            beginInstallDemandNode.SetAttribute("hideMessage", true);
                                            beginInstallDemandNode.SetAttribute("installTimeLeft", -2);//Disable any left over time as this demand has now ended.
                                        }
                                    }
                                    prepDuration--;
                                    demandNode.SetAttribute("prepDuration", prepDuration.Value);
                                }
                            }
                        }
                    }
                }
            }
        }

	    bool HandleDemandsWindow(Node demandNode)
        {
            int? demandsWindow = demandNode.GetIntAttribute("duration");
            if (demandsWindow.HasValue)
            {
                if (demandsWindow == 0)
                {                   
                    demandNode.SetAttribute("active", false);

                    Node beginInstallDemandNode = GetBeginDemandInstallNode(demandNode.GetAttribute("mbu"), demandNode.GetAttribute("service_id"), demandNode.GetAttribute("demandId"));
                    
                    //this condition is if demand is installed in 59th second then transitioning will finish on the 59th second therefore we can have a situation
                    //where beginInstallDemandNode has not been deployed yet. If that is the case then don't bother adjusting the metrics back and uninstalling.
                    if (beginInstallDemandNode.GetAttribute("status").Equals("deployed"))
                    {
                        //success
                        //Adjust back all info per transactions because demand is finishing
                        UnInstallDemands(beginInstallDemandNode.GetAttribute("mbu"),
                            beginInstallDemandNode.GetAttribute("service_id"),
                            beginInstallDemandNode.GetAttribute("vm_id"),
                            beginInstallDemandNode.GetAttribute("enclosure"),
                            beginInstallDemandNode.GetAttribute("demandId"),
                            beginInstallDemandNode.GetIntAttribute("round", 1));
                    }
                    else
                    {
                        //if it were transitioning then do some housekeeping - you can never have waiting status here because that is handled in calling method 
                        //before getting here
                        beginInstallDemandNode.SetAttribute("status", "deployed");
                    }
                    beginInstallDemandNode.SetAttribute("installTimeLeft", -2);

                    FreeEnclosureCapacity(beginInstallDemandNode); //always free capacity because this is done everytime a demand is serviced.

                    demandNode.SetAttribute("hideMessage", Boolean.TrueString);
                    demandsWindow--;
                    demandNode.SetAttribute("duration", demandsWindow.Value);
                    return true;
                }
                
                if (demandsWindow > 0)
                {
                    if (IsDemandDeployed(demandNode.GetAttribute("mbu"), demandNode.GetAttribute("service_id"), demandNode.GetAttribute("demandId"), demandNode.GetAttribute("round")))
                    {
                        demandNode.SetAttribute("status", "met");
                    }

                    demandsWindow--;
                    demandNode.SetAttribute("duration", demandsWindow.Value);
                }
            }

            return false;
        }

	    bool IsDemandDeployed(string mbuSelected, string serviceId, string demandId, string round)
        {
            List<Node> beginDemandsInstallChildren = beginDemandsInstallHead.GetChildrenAsList();

            foreach (Node beginDemandsInstallChild in beginDemandsInstallChildren)
            {
                if (beginDemandsInstallChild.GetAttribute("service_id").Equals(serviceId) &&
                    beginDemandsInstallChild.GetAttribute("mbu").Equals(mbuSelected) &&
                    beginDemandsInstallChild.GetAttribute("demandId").Equals(demandId) &&
                    beginDemandsInstallChild.GetAttribute("round").Equals(round))
                {
                    if (beginDemandsInstallChild.GetAttribute("status").Equals("transitioning") ||
                        beginDemandsInstallChild.GetAttribute("status").Equals("deployed"))
                    {
                        return true;
                    }
                    return false;
                }
            }
            return false;
        }

	    Node GetBeginDemandInstallNode(string mbuSelected, string serviceId, string demandId)
        {
            foreach (Node beginDemandsInstallNode in beginDemandsInstallHead)
            {
                if (beginDemandsInstallNode.GetAttribute("service_id").Equals(serviceId) &&
                    beginDemandsInstallNode.GetAttribute("demandId").Equals(demandId) &&
                    beginDemandsInstallNode.GetAttribute("mbu").Equals(mbuSelected))
                {
                    return beginDemandsInstallNode;
                }
            }
            return null;
        }

	    Node GetBeginDemandInstallNode(string mbuSelected, string serviceId, string vmSelected, string enclosureSelected, string demandId)
        {
            foreach (Node beginDemandInstallNode in beginDemandsInstallHead)
            {
                if (beginDemandInstallNode.GetAttribute("mbu").Equals(mbuSelected) && beginDemandInstallNode.GetAttribute("service_id").Equals(serviceId) &&
                    beginDemandInstallNode.GetAttribute("vm_id").Equals(vmSelected) && beginDemandInstallNode.GetAttribute("enclosure").Equals(enclosureSelected) &&
                    beginDemandInstallNode.GetAttribute("demandId").Equals(demandId))
                {
                    return beginDemandInstallNode;
                }
            }
            return null;
        }

	    Node GetDemandNodeFromBeginDemandInstallNode(string mbuSelected, string serviceId, string vmSelected, string demandId)
        {
            foreach (Node demandNode in demandsNode)
            {
                if (demandNode.GetAttribute("mbu").Equals(mbuSelected) && demandNode.GetAttribute("service_id").Equals(serviceId) &&
                    demandNode.GetAttribute("vm_id").Equals(vmSelected) && demandNode.GetAttribute("demandId").Equals(demandId))
                {
                    return demandNode;
                }
            }
            return null;
        }

        public Dictionary<string,string> GetActiveDemandCodes()
        {
            List<Node> activeDemands = demandsNode.GetChildrenAsList();
            Dictionary<string, string> activeDemandsList = new Dictionary<string, string>();

            foreach (Node activeDemand in activeDemands)
            {
                if (activeDemand.GetBooleanAttribute("active",false))
                {
                    activeDemandsList.Add(activeDemand.GetAttribute("service_id"),"active");
                }
                else
                {
                    activeDemandsList.Add(activeDemand.GetAttribute("service_id"), "inactive");
                }
            }
            return activeDemandsList;
        }



        public void InstallDemands(string mbuSelected, string demandSelected, string vmSelected, string enclosureSelected, string demandId, int round)
        {
            Node enclosureNode = model.GetNamedNode(enclosureSelected);
            string serviceName = GetServiceImpactedFromServiceCode(mbuSelected, demandSelected, round);
            Node beginDemandInstallNode = GetBeginDemandInstallNode(mbuSelected, demandSelected,vmSelected, enclosureSelected,demandId);

            //create app - use "biz service name" + "app" for its name
            string appName = demandId + " " + mbuSelected.Replace("BU ", "").Trim() + " "  +  round + " " + serviceName + " App"; //create new app name

            beginDemandInstallNode.SetAttribute("status","deployed");

            Node appNode = new Node(enclosureNode, "App", appName, new List<AttributeValuePair>
            {
                new AttributeValuePair("type", "App"),
                new AttributeValuePair("desc", appName),
                new AttributeValuePair("fixable", beginDemandInstallNode.GetAttribute("fixable")),
                new AttributeValuePair("canupgrade", beginDemandInstallNode.GetAttribute("canupgrade")),
                new AttributeValuePair("userupgrade", beginDemandInstallNode.GetAttribute("userupgrade")),
                new AttributeValuePair("up", beginDemandInstallNode.GetAttribute("up")),
                new AttributeValuePair("proczone", beginDemandInstallNode.GetAttribute("proczone")),
                new AttributeValuePair("zone", beginDemandInstallNode.GetAttribute("zone")),
                new AttributeValuePair("usernode", beginDemandInstallNode.GetAttribute("usernode")),
                new AttributeValuePair("propagate", beginDemandInstallNode.GetAttribute("propagate")),
                new AttributeValuePair("platform", beginDemandInstallNode.GetAttribute("platform")),
                new AttributeValuePair("version", beginDemandInstallNode.GetAttribute("version")),
            });
            
            //Create link node with apps
            Node linkNode = new LinkNode(appNode, "Connection", appName + " " + mbuSelected + " " + serviceName + " Connection",
            new List<AttributeValuePair>
            {
                new AttributeValuePair("type", "Connection"),
                new AttributeValuePair("to", mbuSelected + " " + serviceName)
            });
        }

        public void UnInstallDemands(string mbuSelected, string demandSelected, string vmSelected, string enclosureSelected, string demandId, int round)
        {
            Node enclosureNode = model.GetNamedNode(enclosureSelected);
            string serviceName = GetServiceImpactedFromServiceCode(mbuSelected, demandSelected, round);
            Node beginDemandInstallNode = GetBeginDemandInstallNode(mbuSelected, demandSelected,vmSelected, enclosureSelected, demandId);

            //Remove app and associated linkNode
            string appName = demandId + " " + mbuSelected.Replace("BU ", "").Trim() + " "  +  round + " " + serviceName + " App"; 

            beginDemandInstallNode.SetAttribute("status","deployed");
            Node appNode = model.GetNamedNode(appName);
            Node linkNode = model.GetNamedNode(appName + " " + mbuSelected + " " + serviceName + " Connection");

            appNode.DeleteChildTree(linkNode);
            enclosureNode.DeleteChildTree(appNode);
        }

        /// <summary>
        /// This method will remove capacity from enclosure and info batches from revenue to free them up
        /// </summary>
        /// <param name="beginInstallDemandNode"></param>
        void AdjustInfoPerTransactionsBack(Node beginInstallDemandNode )
        {
            string mbuSelected = beginInstallDemandNode.GetAttribute("mbu");
            string transactionType = beginInstallDemandNode.GetAttribute("channel");
            int infoPerTrans = beginInstallDemandNode.GetIntAttribute("information_per_trans", 0);
            int instances = beginInstallDemandNode.GetIntAttribute("instances", 1);

            Node mbus = model.GetNamedNode("BUs");
            List<Node> mbuList = mbus.GetChildrenAsList();

            foreach (Node mbu in mbuList)
            {
                if (mbu.GetAttribute("name").Equals(mbuSelected))
                {
                    int onlineBonus = 0;
                    int instoreBonus = 0;

                    switch (transactionType)
                    {
                        case "online":
                            onlineBonus = mbu.GetIntAttribute("online_bonus", 0);
                            onlineBonus -= (infoPerTrans * instances);
                            mbu.SetAttribute("online_bonus", onlineBonus);
                            break;
                        case "instore":
                            instoreBonus = mbu.GetIntAttribute("instore_bonus", 0);
                            instoreBonus -= (infoPerTrans * instances);
                            mbu.SetAttribute("instore_bonus", instoreBonus);
                            break;
                        case "both":
                            onlineBonus = mbu.GetIntAttribute("online_bonus", 0);
                            instoreBonus = mbu.GetIntAttribute("instore_bonus", 0);
                            onlineBonus -= (infoPerTrans * instances);
                            instoreBonus -= (infoPerTrans * instances);
                            mbu.SetAttribute("online_bonus", onlineBonus);
                            mbu.SetAttribute("instore_bonus", instoreBonus);
                            break;
                    }
                }
            }
        }
        public int BladeCostsToIncur(string vmSelected,string enclosureSelected, int vmInstances)
        {
            Node blade = model.GetNamedNode("blade");
            int bladeCpu = Convert.ToInt32(blade.GetAttribute("cpu"));
  
            double vmCpu = Convert.ToDouble(model.GetNamedNode(vmSelected).GetAttribute("cpu"));
            int bladesRequired = Convert.ToInt32(Math.Ceiling((vmCpu * vmInstances) / Convert.ToDouble(bladeCpu)));

   
            int bladeCost = blade.GetIntAttribute("cost", 0);

            return bladeCost*bladesRequired;
        }

        public bool IsThereEnoughInvestmentBudget(string mbuSelected, string demandSelected, string demandIdSelected, int round)
        {
            List<Node> beginDemandChildren = beginDemandsInstallHead.GetChildrenAsList();

            Node budgetNode = model.GetNamedNode("Round " + round + " " + mbuSelected + " Budget");
            foreach (Node beginDemandChild in beginDemandChildren)
            {
                if (beginDemandChild.GetAttribute("mbu").Equals(mbuSelected) &&
                    beginDemandChild.GetAttribute("service_id").Equals(demandSelected) &&
                    beginDemandChild.GetAttribute("demandId").Equals(demandIdSelected))
                {
                    int serviceCost = beginDemandChild.GetIntAttribute("investment", 0) * beginDemandChild.GetIntAttribute("instances", 0);
                    int budget = budgetNode.GetIntAttribute("budget", 0);

                    if ((budget - serviceCost) < 0)
                    {
                        return false;
                    }
                    return true;
                }
            }
            return false;
        }

        public bool IsThereEnoughBladeBudget(string mbuSelected, int bladeCost, int round)
        {
            Node budgetNode = model.GetNamedNode("Round " + round + " " + mbuSelected + " Budget");

            int budget = budgetNode.GetIntAttribute("budget", 0);

            if ((budget - bladeCost) < 0)
            {
                return false;
            }
            return true;
        }

	    string GetDemandNameFromServiceCode(string mbuSelected, string demandSelected,string demandIdSelected, int round)
        {
            Node demandsNode = model.GetNamedNode("Demands");
            List<Node> demandList = demandsNode.GetChildrenAsList();

            foreach (Node demand in demandList)
            {
                if (demand.GetAttribute("service_id").Equals(demandSelected) && demand.GetAttribute("demandId").Equals(demandIdSelected) && demand.GetAttribute("mbu").Equals(mbuSelected) && demand.GetIntAttribute("round", 1).Equals(round))
                {
                    if (demand.GetAttribute("name").Contains(mbuSelected))
                    {
                        return demand.GetAttribute("name");
                    }
                }
            }

            return string.Empty;
        }

        public string GetDemandStatus(string mbuSelected, string demandSelected, string demandIdSelected)
        {
            foreach (Node demandNode in demandsNode)
            {
                if (demandNode.GetAttribute("mbu").Equals(mbuSelected) &&
                    demandNode.GetAttribute("service_id").Equals(demandSelected) &&
                    demandNode.GetAttribute("demandId").Equals(demandIdSelected))
                {
                    return demandNode.GetAttribute("status");
                }
            }
            return string.Empty;
        }

	    string GetGanttOrderFromServiceCode(string demandSelected,int round)
        {
            //Use bizServices and new services for this.
            Node newServices = model.GetNamedNode("New Services Round " + round.ToString());
            Node bizServices = model.GetNamedNode("Business Services Group");

            foreach (Node bizService in bizServices)
            {
                if (bizService.GetAttribute("service_id").Equals(demandSelected))
                {
                    return bizService.GetAttribute("gantt_order");
                }
            }

            //if not in running biz services then try new services
            foreach (Node newService in newServices)
            {
                if (newService.GetAttribute("service_id").Equals(demandSelected))
                {
                    return newService.GetAttribute("gantt_order");
                }
            }

            return string.Empty;
        }
        public string GetVm(string mbuSelected, string demandSelected,string demandIdSelected,int round)
        {
            string demandName = GetDemandNameFromServiceCode(mbuSelected, demandSelected, demandIdSelected, round);
            Node demand = model.GetNamedNode(demandName);
            return demand.GetAttribute("vm_id");
        }

	    string GetNumberOfCpuFromVm(string vmSelected)
        {
            Node vm = model.GetNamedNode(vmSelected);

            return vm.GetAttribute("cpu");
        }

        public List<string> GetVmNames()
        {
            Node vms = model.GetNamedNode("VMs");
            List<Node> vmNodeList = vms.GetChildrenAsList();
            List<string> vmList = new List<string>();

            foreach (Node child in vmNodeList)
            {
                vmList.Add(child.GetAttribute("name"));
            }
            return vmList;
        }

	    void FreeEnclosureCapacity(Node beginInstallDemandNode )
        {
            //Replenish capacity
            //modify capacity of enclosures
            string enclosureSelected = beginInstallDemandNode.GetAttribute("enclosure");

            if (!enclosureSelected.Equals(string.Empty))
            {
                string vmSelected = beginInstallDemandNode.GetAttribute("vm_id");
                Node enclosureNode = model.GetNamedNode(enclosureSelected);
                Node vmNode = model.GetNamedNode(vmSelected);
                Node bladeNode = model.GetNamedNode("blade");

                int bladeCostIncurred = beginInstallDemandNode.GetIntAttribute("bladeCost", 0);
                int bladeCpu = Convert.ToInt32(bladeNode.GetAttribute("cpu"));
                int totalCpu = Convert.ToInt32(enclosureNode.GetAttribute("total_cpu"));
                int usedCpu = Convert.ToInt32(enclosureNode.GetAttribute("used_cpu"));
                int freeCpu = Convert.ToInt32(enclosureNode.GetAttribute("free_cpu"));
                int freeHeight = Convert.ToInt32(enclosureNode.GetAttribute("free_height"));
                int vmInstances = Convert.ToInt32(beginInstallDemandNode.GetAttribute("instances"));

                if (bladeCostIncurred > 0)
                {
                    double vmCpu = Convert.ToDouble(model.GetNamedNode(vmSelected).GetAttribute("cpu"));
                    int bladesUsed = Convert.ToInt32(Math.Ceiling((vmCpu * vmInstances) / Convert.ToDouble(bladeCpu)));

                    totalCpu -= bladesUsed*bladeCpu;

                    freeHeight += bladesUsed;

                    //apply new values to nodes
                    enclosureNode.SetAttribute("total_cpu", totalCpu);
                    enclosureNode.SetAttribute("free_height", freeHeight);
                }
                
                int cpuRequired = Convert.ToInt32(vmNode.GetIntAttribute("cpu"));
                usedCpu -= cpuRequired*vmInstances;
                freeCpu = totalCpu - usedCpu;
                enclosureNode.SetAttribute("used_cpu", usedCpu);
                enclosureNode.SetAttribute("free_cpu", freeCpu);
            }
        }

	    void AdjustBudget(int serviceCost, int bladeCost, string mbuSelected, int round )
        {
            Node budgetNode = model.GetNamedNode("Round " + round + " " + mbuSelected + " Budget");
            int budget = budgetNode.GetIntAttribute("budget", 0);
            
            budget = budget - serviceCost - bladeCost;
            if (budget < 0) //this should only happen at the end of the round when there is not enough budget for adding blades
            {
                budget = 0;
            }

            budgetNode.SetAttribute("budget", budget);
        }

	    void AdjustInfoPerTransactions(int infoPerTrans, string mbuSelected, string transactionType, int instances)
        {
            Node mbus = model.GetNamedNode("BUs");
            List<Node> mbuList = mbus.GetChildrenAsList();
            foreach (Node mbu in mbuList)
            {
                if (mbu.GetAttribute("name").Equals(mbuSelected))
                {
                    int onlineBonus = 0;
                    int instoreBonus = 0;

                    switch (transactionType)
                    {
                        case "online":
                            onlineBonus = mbu.GetIntAttribute("online_bonus", 0);
                            onlineBonus += (infoPerTrans * instances);
                            mbu.SetAttribute("online_bonus", onlineBonus);
                            break;
                        case "instore":
                            instoreBonus = mbu.GetIntAttribute("instore_bonus", 0);
                            instoreBonus += (infoPerTrans * instances);
                            mbu.SetAttribute("instore_bonus", instoreBonus);
                            break;
                        case "both":
                            onlineBonus = mbu.GetIntAttribute("online_bonus", 0);
                            instoreBonus = mbu.GetIntAttribute("instore_bonus", 0);
                            onlineBonus += (infoPerTrans * instances);
                            instoreBonus += (infoPerTrans * instances);
                            mbu.SetAttribute("online_bonus", onlineBonus);
                            mbu.SetAttribute("instore_bonus", instoreBonus);
                            break;
                    }
                }
            }
        }
        
        public int GetFreeCpuInEnclosure(string enclosureSelected)
        {
            Node enclosureNode = model.GetNamedNode(enclosureSelected);
            int freeCpu = Convert.ToInt32(enclosureNode.GetAttribute("free_cpu"));

            return freeCpu;
        }

        public int GetCurrentTime()
        {
            int currentTime = model.GetNamedNode("CurrentTime").GetIntAttribute("seconds",0);
            return currentTime;
        }

        public Dictionary <KeyValuePair<string,int>, bool> GetOptionalDemands()
        {
            Dictionary<KeyValuePair<string, int>, bool> optionalDemandsList = new Dictionary<KeyValuePair<string, int>, bool>();
            Node optionalDemandCodes = model.GetNamedNode("OptionalDemandCodes");

            
            foreach (Node optionalDemandCode in optionalDemandCodes)
            {
                int startTime = optionalDemandCode.GetIntAttribute("startTime", 0);
                int prepDuration = optionalDemandCode.GetIntAttribute("prepDuration", 0);
                KeyValuePair<string, int> item = new KeyValuePair<string, int>(optionalDemandCode.GetAttribute("name"),startTime-prepDuration);
                bool status = IsOptionalDemandClicked(optionalDemandCode.GetAttribute("name"));

                optionalDemandsList.Add(item,status);
            }

            return optionalDemandsList;
        }

        public void SetOptionalDemand(string optionalDemandSelected, int round)
        {
            Node optionalDemand = model.GetNamedNode(optionalDemandSelected);
            string demandName = "Demand " + optionalDemand.GetAttribute("demandId") + " Round " + round + " " + optionalDemand.GetAttribute("mbu") + " " + optionalDemand.GetAttribute("serviceImpacted");
            Node demand = model.GetNamedNode(demandName);
            
            if (demand != null)
            {
                if (demand.Parent.GetAttribute("name").Equals("DemandsRemoved"))
                {
                    MoveFromDemandsRemoved(demandName);
                }

                int currentTime = GetCurrentTime();
                int adjustedPrepTime = demand.GetIntAttribute("prepDuration", 0);
                adjustedPrepTime -= currentTime;
                demand.SetAttribute("prepDuration", adjustedPrepTime);
            }
            optionalDemandsSelected.Add(optionalDemand);
            optionalDemand.SetAttribute("active",true);
        }

        public void ResetOptionalDemand(string optionalDemandSelected, int round)
        {
            for(int i = 0; i< optionalDemandsSelected.Count;i++)
            {
                if (optionalDemandsSelected[i].GetAttribute("name").Equals(optionalDemandSelected))
                {
                    Node optionalDemand = optionalDemandsSelected[i];
                    string demandName = "Demand " + optionalDemand.GetAttribute("demandId") + " Round " + round + " " + optionalDemand.GetAttribute("mbu") + " " + optionalDemand.GetAttribute("serviceImpacted");
                    Node demand = model.GetNamedNode(demandName);

                    if (demand != null)
                    {
                        if (demand.Parent.GetAttribute("name").Equals("Demands"))
                        {
                            MoveToDemandsRemoved(demandName);
                        }
                        //adjust preptime back to max
                        int currentTime = GetCurrentTime();
                        int adjustedPrepTime = demand.GetIntAttribute("prepDuration", 0);
                        adjustedPrepTime += currentTime;
                        demand.SetAttribute("prepDuration", adjustedPrepTime);
                    }
                    optionalDemandsSelected.RemoveAt(i);
                    optionalDemand.SetAttribute("active", false);
                }
            }
        }

        public KeyValuePair<string, int> GetOptionalDemandClicked(string optionalCodeName)
        {
            foreach (Node optionalDemandNode in optionalDemandsSelected)
            {
                if (optionalDemandNode.GetAttribute("name").Equals(optionalCodeName))
                {
                    return new KeyValuePair<string, int>(optionalDemandNode.GetAttribute("name"), optionalDemandNode.GetIntAttribute("startTime", 0));        
                }
            }
            return new KeyValuePair<string, int> (string.Empty,-1);
        }

        public int GetOptionalDemandStartTime(string optionalCodeName)
        {
            Node optionalDemandCode = model.GetNamedNode(optionalCodeName);
            
            if (optionalCodeName != null)
            {
                int startTime = optionalDemandCode.GetIntAttribute("startTime", 0);
                int prepDuration = optionalDemandCode.GetIntAttribute("prepDuration", 0);
                return startTime - prepDuration;
            }
            return -1;
        }

        public bool IsOptionalDemandClicked(string optionalCodeName)
        {
            foreach (Node optionalDemandNode in optionalDemandsSelected)
            {
                if (optionalDemandNode.GetAttribute("name").Equals(optionalCodeName)) 
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsOptionalDemandSelected(string demandName)
        {
            Node demandNode = model.GetNamedNode(demandName);

            foreach (Node optionalDemandNode in optionalDemandsSelected)
            {
                if (optionalDemandNode.GetAttribute("demandId").Equals(demandNode.GetAttribute("demandId")) &&
                    optionalDemandNode.GetAttribute("service_id").Equals(demandNode.GetAttribute("service_id")))
                {
                    return true;
                }
            }
            return false;
        }

        public void MoveToDemandsRemoved(string optionalDemandName)
        {
            Node optionalDemand = model.GetNamedNode(optionalDemandName);
            
            if (optionalDemand != null)
            {
                List<Node> demandsList = demandsNode.GetChildrenAsList();
                foreach (Node demand in demandsList)
                {
                    if(demand.GetAttribute("service_id").Equals(optionalDemand.GetAttribute("service_id")) &&
                        demand.GetAttribute("demandId").Equals(optionalDemand.GetAttribute("demandId")))
                    {
                        model.MoveNode(demand, demandsRemoved);
                    }
                }
            }
        }
        public void MoveFromDemandsRemoved(string optionalDemandName)
        {
            Node optionalDemand = model.GetNamedNode(optionalDemandName);
            
            if (optionalDemand != null)
            {
                List<Node> demandsList = demandsRemoved.GetChildrenAsList();
                foreach (Node demand in demandsList)
                {
                    if (demand.GetAttribute("service_id").Equals(optionalDemand.GetAttribute("service_id")) &&
                        demand.GetAttribute("demandId").Equals(optionalDemand.GetAttribute("demandId")))
                    {
                        model.MoveNode(demand, demandsNode);
                    }
                }
            }
        }

        public bool IsOptionalDemandInProgress(string optionalDemandNameSelected, int round)
        {
            Node optionalDemand = model.GetNamedNode(optionalDemandNameSelected);
            string beginDemandInstallName = "Begin " + optionalDemand.GetAttribute("mbu") + " " + optionalDemand.GetAttribute("serviceImpacted");

            foreach (Node beginDemandInstall in beginDemandsInstallHead)
            {
                if (beginDemandInstall.GetAttribute("service_id").Equals(optionalDemand.GetAttribute("service_id")) &&
                    beginDemandInstall.GetAttribute("demandId").Equals(optionalDemand.GetAttribute("demandId")))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
