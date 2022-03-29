using System;
using System.Collections;
using System.Collections.Generic;
using LibCore;
using Network;

using CoreUtils;

namespace DevOps.OpsEngine
{
	/// <summary>
	/// The transaction manager does the following events 
	///   when transaction are created, their status need to updated in line with store instore and online flags 
	///   when store portal values are changed, transactions status may need to be altered  
	///   when the transaction time arrives, each is processed either Completed or "Cancelled" and money to suit
	///   when the transaction time+30 arrives, transactions are removed.
	/// </summary>
	/// 
	/// Was originally the Polestar NPO TransactionManager but now it uses the Polestar Retail.
	/// 
    public class TransactionManager : ITimedClass
    {
	    NodeTree myNodeTree;

	    Node currentTimeNode;
	    Node transactionsNode;
	    Node revenueNode;
	    Node transactionValuesNode;

	    Hashtable theStoreNodes = new Hashtable();
	    Hashtable transactionAmounts = new Hashtable();

	    int round;

        public TransactionManager(NodeTree nt, int round)
        {
            CoreUtils.TimeManager.TheInstance.ManageClass(this);
            myNodeTree = nt;
            this.round = round;

            currentTimeNode = myNodeTree.GetNamedNode("CurrentTime");
            currentTimeNode.AttributesChanged += new Network.Node.AttributesChangedEventHandler(currentTimeNode_AttributesChanged);

            transactionsNode = myNodeTree.GetNamedNode("Transactions");
            ArrayList killList = new ArrayList();
            if (transactionsNode != null)
            {
                foreach (Node n1 in transactionsNode.getChildren())
                {
                    killList.Add(n1);
                }
                foreach (Node n2 in killList)
                {
                    n2.Parent.DeleteChildTree(n2);
                }
            }
            transactionsNode.ChildAdded += new Network.Node.NodeChildAddedEventHandler(TransactionsNode_ChildAdded);


            revenueNode = myNodeTree.GetNamedNode("Revenue");
            transactionValuesNode = myNodeTree.GetNamedNode("TransactionValues");

            BuildStoreNodes();

            BuildTransactionRevenueCache();
        }

        /// <summary>
        /// Dispose ....
        /// </summary>
        public void Dispose()
        {
            //Disconnect from the Transactions Node 
            transactionsNode.ChildAdded -= new Network.Node.NodeChildAddedEventHandler(TransactionsNode_ChildAdded);

            //Disconnect from the Time Node 
            currentTimeNode.AttributesChanged -= new Network.Node.AttributesChangedEventHandler(currentTimeNode_AttributesChanged);
            //Disconnect from Store Nodes 
            if (theStoreNodes.Count > 0)
            {
                foreach (Node storeNode in theStoreNodes.Values)
                {
                    storeNode.AttributesChanged -= new Network.Node.AttributesChangedEventHandler(storeNode_AttributesChanged);
                }
            }
            theStoreNodes.Clear();
        }

        public void Clear()
        {
        }

        public void Reset()
        {
        }

        public void Start()
        {
        }

        public void Stop()
        {

        }

        public void FastForward(double timesRealTime)
        {
        }

	    void BuildStoreNodes()
        {
            string bizEntityName = CoreUtils.SkinningDefs.TheInstance.GetData("biz");
            ArrayList types = new ArrayList();
            Hashtable ht = new Hashtable();
            types.Clear();
            types.Add(bizEntityName);
            ht = myNodeTree.GetNodesOfAttribTypes(types);
            foreach (Node storeNode in ht.Keys)
            {
                string storename = storeNode.GetAttribute("name");
                Boolean ourstore = storeNode.GetBooleanAttribute("playerstore", false);
                //System.Diagnostics.Debug.WriteLine("Router Added: "+namestr);
                if (ourstore)
                {
                    theStoreNodes.Add(storename, storeNode);
                    storeNode.AttributesChanged += new Network.Node.AttributesChangedEventHandler(storeNode_AttributesChanged);
                }
            }
        }

	    void BuildTransactionRevenueCache()
        {
            transactionAmounts.Clear();
            foreach (Node n in transactionValuesNode.getChildren())
            {
                string eventtype = n.GetAttribute("event_type");
                int revenuelevel = n.GetIntAttribute("revenuelevel", 0);
                int revenue = n.GetIntAttribute("revenue", 0);

                string identifier = eventtype + " " + CONVERT.ToStr(revenuelevel);
                transactionAmounts.Add(identifier, revenue);
            }
        }

	    Boolean DetermineStoreStatus(string destinationStore, string eventType)
        {
            Boolean storeUpForEvent = false;
            foreach (Node n1 in this.theStoreNodes.Values)
            {
                string name = n1.GetAttribute("name");
                string instorePortalStatus = n1.GetAttribute("up_instore");
                string onlinePortalStatus = n1.GetAttribute("up_online");
                if (name.ToLower() == destinationStore.ToLower())
                {
                    if (eventType.ToLower() == "instore")
                    {
                        if (instorePortalStatus.ToLower() == "true")
                        {
                            storeUpForEvent = true;
                        }
                    }
                    if (eventType.ToLower() == "online")
                    {
                        if (onlinePortalStatus.ToLower() == "true")
                        {
                            storeUpForEvent = true;
                        }
                    }

                }
            }
            return storeUpForEvent;
        }

	    List<AttributeValuePair> CalculateLostRevenuePerStore(int globalTime)
        {
            List<AttributeValuePair> avps = new List<AttributeValuePair>();
            foreach (Node n in transactionsNode.getChildren())
            {
                int nodeTime = n.GetIntAttribute("time", 0);
                int revLevel = n.GetIntAttribute("revenuelevel", 0);
                string eventtype = n.GetAttribute("event_type");
                string destinationStore = n.GetAttribute("store");
                string status = n.GetAttribute("status");

                if (nodeTime == globalTime)
                {
                    int revenueLost = 0;
                    if (status == "Canceled")
                    {
                        //revenueLost = (int)transactionAmounts[eventtype + " " + revLevel];
                        int batchValue = DetermineBatchRevenue(destinationStore, eventtype, revLevel);
                        revenueLost += batchValue;
                    }
                    AttributeValuePair avp = new AttributeValuePair("lost" + destinationStore.Replace(" ", ""), revenueLost);
                    avps.Add(avp);
                }


            }
            AttributeValuePair flag = new AttributeValuePair("all_lost_revenues", true);
            avps.Add(flag);

            return avps;
        }

	    int DetermineBatchRevenue(string destinationStore, string eventtype, int revLevel)
        {
            int batchRevenue = 0;
            int baseRevenue = 0;
            int onlineBonus = 0;
            int instoreBonus = 0;

            Node storeNode = myNodeTree.GetNamedNode(destinationStore);
            if (storeNode != null)
            {
                //Get the Base Amount
                string identifier = eventtype + " " + CONVERT.ToStr(revLevel);
                if (transactionAmounts.Contains(identifier))
                {
                    baseRevenue = (int)transactionAmounts[identifier];
                }
                //Get the Store Bonus level 
                onlineBonus = storeNode.GetIntAttribute("online_bonus", 0);
                instoreBonus = storeNode.GetIntAttribute("instore_bonus", 0);

                if (eventtype.ToLower() == "online")
                {
                    batchRevenue = baseRevenue + onlineBonus;
                }
                if (eventtype.ToLower() == "instore")
                {
                    batchRevenue = baseRevenue + instoreBonus;
                }
            }
            return batchRevenue;
        }

	    void UpdateStoreLastTransaction(string destinationStore)
        {
            Node storeNode = myNodeTree.GetNamedNode(destinationStore);
            if (storeNode != null)
            {
                int lt = storeNode.GetIntAttribute("last_transaction", -1);
                lt = lt + 1;
                storeNode.SetAttribute("last_transaction", CONVERT.ToStr(lt));
            }
        }

	    void currentTimeNode_AttributesChanged(Node sender, ArrayList attrs)
        {
            lock (this)
            {
                foreach (AttributeValuePair avp in attrs)
                {

                    bool addIndividualCosts = false;
                    if (avp.Attribute == "seconds")
                    {
                        int countGood = transactionsNode.GetIntAttribute("count_good", 0);
                        int countProcessed = transactionsNode.GetIntAttribute("count_processed", 0);
                        int countGoodOld = countGood;
                        int countProcessedOld = countProcessed;
                        int globalTime = currentTimeNode.GetIntAttribute("seconds", 0);
                        int revenueGood = revenueNode.GetIntAttribute("revenue", 0);
                        int revenueLost = revenueNode.GetIntAttribute("revenue_lost", 0);


                        ArrayList killList = new ArrayList();

                        int revenueMadeFromNS = 0;

                        foreach (Node n in transactionsNode.getChildren())
                        {
                            int nodeTime = n.GetIntAttribute("time", 0);
                            int revLevel = n.GetIntAttribute("revenuelevel", 0);
                            string eventtype = n.GetAttribute("event_type");
                            string destinationStore = n.GetAttribute("store");
                            string status = n.GetAttribute("status");

                            if ((status.ToLower() == "queued") || (status.ToLower() == "at risk"))
                            {
                                Boolean StoreUp = true;
                                int batch_value = 0;

                                if (nodeTime == globalTime)
                                {
                                    //Work out wether the Store is Up 
                                    StoreUp = DetermineStoreStatus(destinationStore, eventtype);
                                    //Work out how much the Batch is Worth
                                    batch_value = DetermineBatchRevenue(destinationStore, eventtype, revLevel);

                                    //need to update the Store node last transaction
                                    UpdateStoreLastTransaction(destinationStore);

                                    countProcessed++;
                                    if (StoreUp)
                                    {
                                        //set the transaction to completed
                                        n.SetAttribute("status", "Handled");
                                        countGood++;
                                        //process the money as revenue gained
                                        revenueGood = revenueGood + batch_value;
                                        revenueNode.SetAttribute("revenue", CONVERT.ToStr(revenueGood));
                                        revenueMadeFromNS += batch_value;
                                    }
                                    else
                                    {
                                        n.SetAttribute("status", "Canceled");
                                        //process the money as revenue lost
                                        revenueLost = revenueLost + batch_value;
                                        //revenueNode.SetAttribute("revenue_lost", CONVERT.ToStr(revenueLost));
                                        revenueNode.SetAttributes(new List<AttributeValuePair>()
                                                                  {
                                                                      new AttributeValuePair("revenue_lost", revenueLost),
                                                                      new AttributeValuePair("all_lost_revenues", true)
                                                                  });
                                        addIndividualCosts = true;
                                    }
                                }
                                else
                                {
                                    //not processing, need to check for Store Up Status 
                                    //Work out wether the Store is Up 
                                    //StoreUp = DetermineStoreStatus(destination_store, eventtype);
                                }
                            }
                            else
                            {
                                //Not scheduled, must be completed waiting for kill time 
                                if ((nodeTime + 30) == globalTime)
                                {
                                    killList.Add(n);
                                }
                            }
                            //
                        }

                        if (revenueMadeFromNS > 0)
                        {

                            Node revenueMadeNode =
                                myNodeTree.GetNamedNode("NewServicesGainMade")
                                    .GetChildWithAttributeValue("round", round.ToString());

                            int currentRev = revenueMadeNode.GetIntAttribute("total_rev_made", 0);
                            currentRev += revenueMadeFromNS;
                            revenueMadeNode.SetAttribute("total_rev_made", currentRev);
                        }

                        if (addIndividualCosts)
                        {
                            List<AttributeValuePair> lostRevenuePerStore = CalculateLostRevenuePerStore(globalTime);
                            revenueNode.SetAttributes(lostRevenuePerStore);
                        }

                        foreach (Node n in killList)
                        {
                            n.Parent.DeleteChildTree(n);
                        }
                        if (countGoodOld != countGood)
                        {
                            transactionsNode.SetAttribute("count_good", CONVERT.ToStr(countGood));
                        }
                        if (countProcessedOld != countProcessed)
                        {
                            transactionsNode.SetAttribute("count_processed", CONVERT.ToStr(countProcessed));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Handle the Change of Store Status 
        /// Need to alter the status of Pending Transactions 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="attrs"></param>
        void storeNode_AttributesChanged(Node sender, ArrayList attrs)
        {
            lock (this)
            {
                Boolean inStorePortalWorking = false;
                Boolean onLinePortalWorking = false;

                //System.Diagnostics.Debug.WriteLine("##############################################################");

                foreach (AttributeValuePair avp in attrs)
                {
                    if (avp.Attribute == "up_instore")
                    {
                        //get name data from the sender 
                        string storename = sender.GetAttribute("name");
                        string attrName = avp.Attribute;
                        string attrValue = avp.Value;
                        //System.Diagnostics.Debug.WriteLine("EVENT "+storename+" "+AttrName+ " "+ AttrValue);

                        //what status are we now at 
                        inStorePortalWorking = false;
                        if (avp.Value.ToLower() == "true")
                        {
                            inStorePortalWorking = true;
                        }
                        //System.Diagnostics.Debug.WriteLine(" Portal Change ["+storename +"]("+InStorePortalWorking+")");

                        //Process all the transations
                        foreach (Node n in transactionsNode.getChildren())
                        {
                            int nodeTime = n.GetIntAttribute("time", 0);
                            int revLevel = n.GetIntAttribute("revenuelevel", 0);
                            string eventtype = n.GetAttribute("event_type");
                            string destinationStore = n.GetAttribute("store");
                            string status = n.GetAttribute("status");
                            string seq = n.GetAttribute("sequence");
                            string db = "  TR" + seq + " " + destinationStore + " " + eventtype + "  " + nodeTime + "  " + revLevel + "  " + status;
                            //System.Diagnostics.Debug.WriteLine(db);

                            if (destinationStore.ToLower() == storename.ToLower())
                            {
                                if (eventtype == "instore")
                                {
                                    //portal good and transaction showing "Delayed" --> back to "Queued"	
                                    if ((inStorePortalWorking) && (status.ToLower() == "at risk"))
                                    {
                                        n.SetAttribute("status", "Queued");
                                        //System.Diagnostics.Debug.WriteLine("  Changed");
                                    }
                                    //portal good and transaction showing "Queued" --> back to "Delayed"	
                                    if ((inStorePortalWorking == false) && (status.ToLower() == "queued"))
                                    {
                                        n.SetAttribute("status", "At Risk");
                                        //System.Diagnostics.Debug.WriteLine("  Changed");
                                    }
                                }
                            }
                        }
                    }
                    if (avp.Attribute == "up_online")
                    {
                        //get name data from the sender 
                        string storename = sender.GetAttribute("name");
                        //what status are we now at 
                        onLinePortalWorking = false;
                        if (avp.Value.ToLower() == "true")
                        {
                            onLinePortalWorking = true;
                        }
                        //Process all the transations
                        foreach (Node n in transactionsNode.getChildren())
                        {
                            int nodeTime = n.GetIntAttribute("time", 0);
                            int revLevel = n.GetIntAttribute("revenuelevel", 0);
                            string eventtype = n.GetAttribute("event_type");
                            string destinationStore = n.GetAttribute("store");
                            string status = n.GetAttribute("status");
                            string seq = n.GetAttribute("sequence");
                            string db = "  TR" + seq + " " + destinationStore + " " + eventtype + "  " + nodeTime + "  " + revLevel + "  " + status;
                            //System.Diagnostics.Debug.WriteLine(db);

                            if (destinationStore.ToLower() == storename.ToLower())
                            {

                                if (eventtype == "online")
                                {
                                    //portal good and transaction showing "Delayed" --> back to "Queued"	
                                    if ((onLinePortalWorking) && (status.ToLower() == "at risk"))
                                    {
                                        n.SetAttribute("status", "Queued");
                                        //System.Diagnostics.Debug.WriteLine("  Changed");
                                    }
                                    //portal good and transaction showing "Queued" --> back to "Delayed"	

                                    if ((onLinePortalWorking == false) && (status.ToLower() == "queued"))
                                    {
                                        n.SetAttribute("status", "At Risk");
                                        //System.Diagnostics.Debug.WriteLine("  Changed");
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Processing the Transaction as they are added into the network as timed events 
        /// We need to establish thier Status (could thier store process them now)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="child"></param>
        void TransactionsNode_ChildAdded(Node sender, Node child)
        {
            //when adding new children, we need to check the destination store's status 
            if (child != null)
            {
                int nodeTime = child.GetIntAttribute("time", 0);
                int revLevel = child.GetIntAttribute("revenuelevel", 0);
                string eventtype = child.GetAttribute("event_type");
                string destinationStore = child.GetAttribute("store");
                string status = child.GetAttribute("status");
                string seq = child.GetAttribute("sequence");
                string db = "  TR" + seq + " " + destinationStore + " " + eventtype + "  " + nodeTime + "  " + revLevel + "  " + status;

                Boolean storeStatus = false;

                storeStatus = DetermineStoreStatus(destinationStore, eventtype);
                if (storeStatus == false)
                {
                    child.SetAttribute("status", "At Risk");
                }
            }

        }
    }
}
