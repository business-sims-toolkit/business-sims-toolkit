using System;
using System.Collections;
using System.Xml;
using System.Collections.Generic;
using LibCore;
using Network;

using CoreUtils;

namespace Polestar_Retail.OpsEngine
{
	/// <summary>
	/// The transaction manager does the following events 
	///   when transaction are created, thier status need to updated in line with store instore and online flags 
	///   when store portal values are changed, transactions status may need to be altered  
	///   when the transaction time arrives, each is processed either Completed or "Cancelled" and money to suit
	///   when the transaction time+30 arrives, transactions are removed.
	/// </summary>
	public class TransactionManager : ITimedClass
	{
		private NodeTree MyNodeTree;
		
		private Node currentTimeNode;
		private Node TransactionsNode;
		private Node RevenueNode;
		private Node TransactionValuesNode;

		private Hashtable TheStoreNodes = new Hashtable();
		private Hashtable TransactionAmounts = new Hashtable();
		
		public TransactionManager(NodeTree nt, int round)
		{
			CoreUtils.TimeManager.TheInstance.ManageClass(this);
			MyNodeTree = nt;
			
			currentTimeNode = MyNodeTree.GetNamedNode("CurrentTime");
			currentTimeNode.AttributesChanged += currentTimeNode_AttributesChanged;

			TransactionsNode = MyNodeTree.GetNamedNode("Transactions");
			ArrayList KillList = new ArrayList();
			if (TransactionsNode != null)
			{
				foreach (Node n1 in TransactionsNode.getChildren())
				{
					KillList.Add(n1);
				}
				foreach(Node n2 in KillList)
				{
					n2.Parent.DeleteChildTree(n2);
				}
			}
			TransactionsNode.ChildAdded +=TransactionsNode_ChildAdded;


			RevenueNode = MyNodeTree.GetNamedNode("Revenue");
			TransactionValuesNode = MyNodeTree.GetNamedNode("TransactionValues");

			BuildStoreNodes();

			BuildTransactionRevenueCache();
		}

		/// <summary>
		/// Dispose ....
		/// </summary>
		public void Dispose()
		{
			//Disconnect from the Transactions Node 
			TransactionsNode.ChildAdded -=TransactionsNode_ChildAdded;

			//Disconnect from the Time Node 
			currentTimeNode.AttributesChanged -= currentTimeNode_AttributesChanged;
			//Disconnect from Store Nodes 
			if (TheStoreNodes.Count>0)
			{
				foreach (Node storeNode in TheStoreNodes.Values)
				{
					storeNode.AttributesChanged  -= storeNode_AttributesChanged;
				}
			}
			TheStoreNodes.Clear();
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

		private void BuildStoreNodes()
		{
			string BizEntityName = CoreUtils.SkinningDefs.TheInstance.GetData("biz");
			ArrayList types = new ArrayList();
			Hashtable ht = new Hashtable();
			types.Clear();
			types.Add(BizEntityName);
			ht = MyNodeTree.GetNodesOfAttribTypes(types);
			foreach(Node storeNode in ht.Keys)
			{
				string storename = storeNode.GetAttribute("name");
				Boolean ourstore = storeNode.GetBooleanAttribute("playerstore", false);
				//System.Diagnostics.Debug.WriteLine("Router Added: "+namestr);
				if (ourstore)
				{
					TheStoreNodes.Add(storename, storeNode);
					storeNode.AttributesChanged  +=storeNode_AttributesChanged;
				}
			}
		}

		private void BuildTransactionRevenueCache()
		{
			TransactionAmounts.Clear();
			foreach (Node n in TransactionValuesNode.getChildren())
			{
				string eventtype = n.GetAttribute("event_type");
				int revenuelevel = n.GetIntAttribute("revenuelevel",0);
				int revenue = n.GetIntAttribute("revenue",0);

				string identifier = eventtype + " " +CONVERT.ToStr(revenuelevel);
				TransactionAmounts.Add(identifier, revenue);
			}
		}


		private Boolean DetermineStoreStatus(string destination_store, string eventtype)
		{
			Boolean storeUpForEvent = false;
			foreach (Node n1 in this.TheStoreNodes.Values)
			{
				string name = n1.GetAttribute("name");
				string instore_portal_status = n1.GetAttribute("up_instore");
				string online_portal_status = n1.GetAttribute("up_online");
				if (name.ToLower() == destination_store.ToLower())
				{
					if (eventtype.ToLower() == "instore")
					{
						if (instore_portal_status.ToLower() == "true")
						{
							storeUpForEvent = true;
						}
					}
					if (eventtype.ToLower() == "online")
					{
						if (online_portal_status.ToLower() == "true")
						{
							storeUpForEvent = true;
						}
					}

				}
			}
			return storeUpForEvent;
		}


        private List<AttributeValuePair> CalculateLostRevenuePerStore(int globalTime)
        {
            List<AttributeValuePair> avps = new List<AttributeValuePair>();
            foreach (Node n in TransactionsNode.getChildren())
            {
                int node_time = n.GetIntAttribute("time",0);
				int rev_level = n.GetIntAttribute("revenuelevel",0);
			    string eventtype = n.GetAttribute("event_type");
				string destination_store = n.GetAttribute("store");
                string status = n.GetAttribute("status");

                if (node_time == globalTime)
                {
                    int revenueLost = 0;
                    if (status == "Canceled")
                    {
                        revenueLost = (int)TransactionAmounts[eventtype + " " + rev_level];
                    }
                    AttributeValuePair avp = new AttributeValuePair("lost" + destination_store.Replace(" ", ""), revenueLost);
                    avps.Add(avp);
                }


            }
            AttributeValuePair flag = new AttributeValuePair("all_lost_revenues", true);
            avps.Add(flag);

            return avps;
        }


		private int DetermineBatchRevenue(string destination_store, string eventtype, int rev_level)
		{
			int BatchRevenue = 0;
			int BaseRevenue = 0;
			int online_bonus = 0; 
			int instore_bonus = 0;  

			Node StoreNode = MyNodeTree.GetNamedNode(destination_store);
			if (StoreNode != null)
			{
				//Get the Base Amount
				string identifier = eventtype + " " +CONVERT.ToStr(rev_level);
				if (TransactionAmounts.Contains(identifier))
				{
					BaseRevenue = (int)TransactionAmounts[identifier];
				}
				//Get the Store Bonus level 
				online_bonus = StoreNode.GetIntAttribute("online_bonus",0);
				instore_bonus = StoreNode.GetIntAttribute("instore_bonus",0); 

				if (eventtype.ToLower()=="online")
				{
					BatchRevenue = BaseRevenue + online_bonus;
				}
				if (eventtype.ToLower()=="instore")
				{
					BatchRevenue = BaseRevenue + instore_bonus;
				}
			}
			return BatchRevenue;
		}

		private void UpdateStoreLastTransaction(string destination_store)
		{
			Node StoreNode = MyNodeTree.GetNamedNode(destination_store);
			if (StoreNode != null)
			{
				int lt = StoreNode.GetIntAttribute("last_transaction",-1);
				lt=lt+1;
				StoreNode.SetAttribute("last_transaction",CONVERT.ToStr(lt));
			}
		}

		private void currentTimeNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			lock(this)
			{
				foreach(AttributeValuePair avp in attrs)
				{

                    bool addIndividualCosts = false;
					if(avp.Attribute == "seconds")
					{
						int count_good = TransactionsNode.GetIntAttribute("count_good",0);
						int count_processed = TransactionsNode.GetIntAttribute("count_processed",0);
						int count_good_old = count_good;
						int count_processed_old = count_processed;
						int global_time =  currentTimeNode.GetIntAttribute("seconds",0);
						int revenue_good = RevenueNode.GetIntAttribute("revenue",0);
						int revenue_lost = RevenueNode.GetIntAttribute("revenue_lost",0);
						

						ArrayList KillList = new ArrayList();

						foreach(Node n in TransactionsNode.getChildren())
						{
							int node_time = n.GetIntAttribute("time",0);
							int rev_level = n.GetIntAttribute("revenuelevel",0);
							string eventtype = n.GetAttribute("event_type");
							string destination_store = n.GetAttribute("store"); 
							string status = n.GetAttribute("status"); 

							if ((status.ToLower() == "queued")||(status.ToLower() == "at risk"))
							{
								Boolean StoreUp = true;
								int batch_value = 0;

								if (node_time == global_time)
								{
									//Work out wether the Store is Up 
									StoreUp = DetermineStoreStatus(destination_store, eventtype);
									//Work out how much the Batch is Worth
									batch_value = DetermineBatchRevenue(destination_store, eventtype, rev_level);

									//need to update the Store node last transaction
									UpdateStoreLastTransaction(destination_store);

									count_processed ++;
									if (StoreUp)
									{
										//set the transaction to completed
										n.SetAttribute("status","Handled");
										count_good ++;
										//process the money as revenue gained
										revenue_good = revenue_good + batch_value;
										RevenueNode.SetAttribute("revenue", CONVERT.ToStr(revenue_good));
									}
									else
									{
										n.SetAttribute("status","Canceled");
										//process the money as revenue lost
										revenue_lost = revenue_lost + batch_value;
										RevenueNode.SetAttribute("revenue_lost", CONVERT.ToStr(revenue_lost));
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
								if ((node_time+30) == global_time)
								{
									KillList.Add(n);
								}
							}
							//
						}
                        if (addIndividualCosts)
                        {
                            List<AttributeValuePair> lostRevenuePerStore = CalculateLostRevenuePerStore(global_time);
                            RevenueNode.SetAttributes(lostRevenuePerStore);
                        }

						foreach (Node n in KillList)
						{
							n.Parent.DeleteChildTree(n);
						}
						if (count_good_old != count_good)
						{
							TransactionsNode.SetAttribute("count_good",CONVERT.ToStr(count_good));
						}
						if (count_processed_old != count_processed)
						{
							TransactionsNode.SetAttribute("count_processed",CONVERT.ToStr(count_processed));
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
		private void storeNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			lock(this)
			{
				Boolean InStorePortalWorking = false;
				Boolean OnLinePortalWorking = false;

				//System.Diagnostics.Debug.WriteLine("##############################################################");

				foreach(AttributeValuePair avp in attrs)
				{
					if(avp.Attribute == "up_instore")
					{
						//get name data from the sender 
						string storename = sender.GetAttribute("name");
						string AttrName = avp.Attribute;
						string AttrValue = avp.Value;
						//System.Diagnostics.Debug.WriteLine("EVENT "+storename+" "+AttrName+ " "+ AttrValue);

						//what status are we now at 
						InStorePortalWorking = false;
						if (avp.Value.ToLower() == "true")
						{
							InStorePortalWorking = true;
						}
						//System.Diagnostics.Debug.WriteLine(" Portal Change ["+storename +"]("+InStorePortalWorking+")");

						//Process all the transations
						foreach(Node n in TransactionsNode.getChildren())
						{
							int node_time = n.GetIntAttribute("time",0);
							int rev_level = n.GetIntAttribute("revenuelevel",0);
							string eventtype = n.GetAttribute("event_type");
							string destination_store = n.GetAttribute("store"); 
							string status = n.GetAttribute("status"); 
							string seq = n.GetAttribute("sequence"); 
							string db = "  TR"+seq+" "+destination_store+ " "+eventtype+"  "+ node_time +"  "+rev_level + "  "+ status;
							//System.Diagnostics.Debug.WriteLine(db);

							if (destination_store.ToLower() == storename.ToLower())
							{
								if (eventtype == "instore")
								{
									//portal good and transaction showing "Delayed" --> back to "Queued"	
									if ((InStorePortalWorking) && (status.ToLower() == "at risk"))
									{
										n.SetAttribute("status","Queued");
										//System.Diagnostics.Debug.WriteLine("  Changed");
									}
									//portal good and transaction showing "Queued" --> back to "Delayed"	
									if ((InStorePortalWorking==false)&&(status.ToLower() == "queued"))
									{
										n.SetAttribute("status","At Risk");
										//System.Diagnostics.Debug.WriteLine("  Changed");
									}
								}
							}
						}
					}
					if(avp.Attribute == "up_online")
					{
						//get name data from the sender 
						string storename = sender.GetAttribute("name");
						//what status are we now at 
						OnLinePortalWorking = false;
						if (avp.Value.ToLower() == "true")
						{
							OnLinePortalWorking = true;
						}
						//Process all the transations
						foreach(Node n in TransactionsNode.getChildren())
						{
							int node_time = n.GetIntAttribute("time",0);
							int rev_level = n.GetIntAttribute("revenuelevel",0);
							string eventtype = n.GetAttribute("event_type");
							string destination_store = n.GetAttribute("store"); 
							string status = n.GetAttribute("status"); 
							string seq = n.GetAttribute("sequence"); 
							string db = "  TR"+seq+" "+destination_store+ " "+eventtype+"  "+ node_time +"  "+rev_level + "  "+ status;
							//System.Diagnostics.Debug.WriteLine(db);

							if (destination_store.ToLower() == storename.ToLower())
							{

								if (eventtype == "online")
								{
									//portal good and transaction showing "Delayed" --> back to "Queued"	
									if ((OnLinePortalWorking) && (status.ToLower() == "at risk"))
									{
										n.SetAttribute("status","Queued");
										//System.Diagnostics.Debug.WriteLine("  Changed");
									}
									//portal good and transaction showing "Queued" --> back to "Delayed"	

									if ((OnLinePortalWorking==false)&&(status.ToLower() == "queued"))
									{
										n.SetAttribute("status","At Risk");
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
		private void TransactionsNode_ChildAdded(Node sender, Node child)
		{
			//when adding new children, we need to check the destination store's status 
			if (child != null)
			{
				int node_time = child.GetIntAttribute("time",0);
				int rev_level = child.GetIntAttribute("revenuelevel",0);
				string eventtype = child.GetAttribute("event_type");
				string destination_store = child.GetAttribute("store"); 
				string status = child.GetAttribute("status"); 
				string seq = child.GetAttribute("sequence"); 
				string db = "  TR"+seq+" "+destination_store+ " "+eventtype+"  "+ node_time +"  "+rev_level + "  "+ status;

				Boolean StoreStatus = false;

				StoreStatus = DetermineStoreStatus(destination_store, eventtype);
				if (StoreStatus==false)
				{
					child.SetAttribute("status", "At Risk");
				}
			}

		}
	}
}
