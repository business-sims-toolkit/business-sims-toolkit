using System;
using System.Collections;
using LibCore;
using Network;

namespace DevOps.OpsEngine
{
	/// <summary>
	/// This calculates the Max Applications Processed For this round 
	/// It is refreshed by an attribute request on the ApplicationsProcessed node. ("max_apps_refresh")
	/// It can operate in two modes
	///   A, SIP Bonus in Project Node  
	///     The max revenue is the (transactions * (base value + store Bonus values)) + Project Offsets
	///     The Poject offesets are defined in the creation section of the SIPs
	///   B, In Round Recalculation of Max Revenue when the Stores bonus values are changed.
	///     The older method is (transactions * (base value + store Bonus values))
	///     and we recalculate the future transaction when the store bonus are chnaged. 
	/// Currently the Mode A system is hard coded inside the constructor 
	/// </summary>
	public class MaxAppsProcessedManager 
	{
		NodeTree MyNodeTree;

		Node TransactionProfilesNode;
		Node TransactionValuesNode;
		Node ApplicationsProcessedNode;

		Hashtable TheStoreNodes = new Hashtable();
		//private Hashtable TheStoreBonusAmounts;
		Hashtable TheProfilePoints = new Hashtable();

		Hashtable TransactionAmounts = new Hashtable();
		int MaxTransactionNumber=0;
		bool UseNewSIPOffsetBonusModel = true;  
		
		public MaxAppsProcessedManager(NodeTree nt)
		{
			MyNodeTree = nt;
			string BizEntityName = CoreUtils.SkinningDefs.TheInstance.GetData("biz");			

			//Hard coded to use the SIP Project Calculation Mode
			UseNewSIPOffsetBonusModel = true;  

			//Connect to the Transation Profiles Node (what happens when)
			TransactionProfilesNode = MyNodeTree.GetNamedNode("TransactionProfiles");
			BuildProfilesCache();

			//Connect to the Transation Values Node (The Base Revenues) 
			TransactionValuesNode = MyNodeTree.GetNamedNode("TransactionValues");
			BuildTransactionRevenueCache();

			//Connect up to the stores for the current Bonus Amounts
			ArrayList types = new ArrayList();
			Hashtable ht = new Hashtable();
			types.Clear();
			types.Add(BizEntityName);
			ht = MyNodeTree.GetNodesOfAttribTypes(types);
			foreach(Node storeNode in ht.Keys)
			{
				string namestr = storeNode.GetAttribute("name");
				Boolean ourstore = storeNode.GetBooleanAttribute("playerstore", false);
				//System.Diagnostics.Debug.WriteLine("Router Added: "+namestr);
				if (ourstore)
				{
					TheStoreNodes.Add(namestr, storeNode);
					if (UseNewSIPOffsetBonusModel==false)
					{
						//we attach to see changes 
						storeNode.AttributesChanged +=new Network.Node.AttributesChangedEventHandler(storeNode_AttributesChanged);
					}
				}
			}
//			if (TheStoreNodes.Count>0)
//			{
//				BuildStoreData();
//			}

			//Connect to the Transations Node (What Has Happened)
			ApplicationsProcessedNode = MyNodeTree.GetNamedNode("ApplicationsProcessed");
			ApplicationsProcessedNode.AttributesChanged += new Network.Node.AttributesChangedEventHandler(ApplicationsProcessedNode_AttributesChanged);
			CheckForEmptyMaxAppsProcessed();

			Boolean RefreshRequired = ApplicationsProcessedNode.GetBooleanAttribute("max_apps_refresh", false);
			if (RefreshRequired)
			{
				RefreshMaxAppsProcessed();
				ApplicationsProcessedNode.SetAttribute("max_apps_refresh", "false");
			}
		}

		/// <summary>
		/// Dispose ....
		/// </summary>
		public void Dispose()
		{
			//Disconnect from Store Nodes 
			if (TheStoreNodes.Count>0)
			{
				foreach (Node storeNode in TheStoreNodes.Values)
				{
					if (UseNewSIPOffsetBonusModel==false)
					{
						storeNode.AttributesChanged -= new Network.Node.AttributesChangedEventHandler(storeNode_AttributesChanged);
					}
				}
			}
			TheStoreNodes.Clear();
		}

		void BuildProfilesCache()
		{
			TheProfilePoints.Clear();
			foreach (Node profileNode in TransactionProfilesNode.getChildren())
			{
				string storename = profileNode.GetAttribute("storename");
				string profile = profileNode.GetAttribute("profile");
				string[] batchvalues = profile.Split(',');

				int counter=0;
				if (batchvalues.Length>0)
				{
					for (int step=0; step < batchvalues.Length; step++)
					{
						string dataname = storename.ToLower() + "_event" + CONVERT.ToStr(counter+1);
						string datavalue = batchvalues[counter];
						TheProfilePoints.Add(dataname, datavalue);
						counter++;
						if (counter>MaxTransactionNumber)
						{
							MaxTransactionNumber = counter;
						}
					}
				}
				//in future we may modify the profile in run-time
				//so we may add an attribute changed event handler here
			}
		}

		void BuildTransactionRevenueCache()
		{
			TransactionAmounts.Clear();
			foreach (Node n in TransactionValuesNode.getChildren())
			{
				string eventtype = n.GetAttribute("event_type");
				int revenuelevel = n.GetIntAttribute("revenuelevel",0);
				int revenue = n.GetIntAttribute("applications",0);

				string identifier = eventtype + "_" +CONVERT.ToStr(revenuelevel);
				TransactionAmounts.Add(identifier, revenue);
				//in future we may modify the revenue base amount in run-time
				//so we may add an attribute changed event handler here
			}
		}

//		private void BuildStoreData()
//		{
//			TheStoreBonusAmounts.Clear();
//			LastTransactionCounts.Clear();
//			foreach (Node storeNode in TheStoreNodes.Values)
//			{
//				string namestr = storeNode.GetAttribute("name");
//				int online_bonus_amount = storeNode.GetIntAttribute("online_bonus",0);
//				int instore_bonus_amount = storeNode.GetIntAttribute("instore_bonus",0);
//				int last_transaction = storeNode.GetIntAttribute("last_transaction",0);
//
//				TheStoreBonusAmounts.Add("online_bonus"+"_"+namestr, online_bonus_amount);
//				TheStoreBonusAmounts.Add("instore_bonus"+"_"+namestr, instore_bonus_amount);
//			}
//		}

		void CheckForEmptyMaxAppsProcessed()
		{
			int currentMaxApps = ApplicationsProcessedNode.GetIntAttribute("max_apps_processed", -1);
			if (currentMaxApps == -1)
			{
				RefreshMaxAppsProcessed();
			}
		}

		void RefreshMaxAppsProcessed()
		{
			int max_apps_processed = CalculateMaxRevenue();
			this.ApplicationsProcessedNode.SetAttribute("max_apps_processed", CONVERT.ToStr(max_apps_processed));
		}

		int DetermineSIPOffsets()
		{
			int total_sip_offset_revenue =0;
			int individual_sip_offset_revenue =0;

			Node ProjectsNode = MyNodeTree.GetNamedNode("Projects");
			if (ProjectsNode != null)
			{
				foreach (Node prjnode in ProjectsNode.getChildren())
				{
					individual_sip_offset_revenue = prjnode.GetIntAttribute("MaxRevOffset",0);
					total_sip_offset_revenue += individual_sip_offset_revenue;
				}
			}
			return total_sip_offset_revenue;
		}

		int CalculateMaxRevenue()
		{
			//get the current spend money 
			int currentGoodRevenue = ApplicationsProcessedNode.GetIntAttribute("apps_processed", -1);
			int currentLostRevenue = ApplicationsProcessedNode.GetIntAttribute("apps_lost", -1);
			int future_revenue = 0;
			int max_revenue = 0;

			//System.Diagnostics.Debug.WriteLine("===================================================================");
			//System.Diagnostics.Debug.WriteLine("===================================================================");
			//System.Diagnostics.Debug.WriteLine("===================================================================");
			//System.Diagnostics.Debug.WriteLine("currentGoodRevenue" +currentGoodRevenue.ToString());
			//System.Diagnostics.Debug.WriteLine("currentLostRevenue" +currentLostRevenue.ToString());

			//Step threough the stores and calculate thier future revenue 
			foreach (Node storeNode in TheStoreNodes.Values)
			{
				string namestr = storeNode.GetAttribute("name");
				int last_transaction = storeNode.GetIntAttribute("last_transaction",-1);
				int online_bonus_amount = storeNode.GetIntAttribute("online_bonus",0);
				int instore_bonus_amount = storeNode.GetIntAttribute("instore_bonus",0);
				int applicable_bonus = 0;

				//string debugstr = namestr + " ltr:"+last_transaction.ToString()+ "  oba:"+online_bonus_amount.ToString() + "  iba:" + instore_bonus_amount.ToString();
				//System.Diagnostics.Debug.WriteLine(debugstr);
		
				for (int step=0; step < MaxTransactionNumber; step++)
				{
					//System.Diagnostics.Debug.WriteLine(" Starting the Transactions ");  
					if ((step+1)>last_transaction)
					{
						string dataname = namestr.ToLower() + "_event" + CONVERT.ToStr(step+1);
						if (TheProfilePoints.Contains(dataname))
						{
							string datavalue = (string)TheProfilePoints[dataname];
							if (datavalue != string.Empty)
							{
								string revstr = "";
								if (datavalue.IndexOf("I")>-1)
								{
									revstr =  datavalue.Replace("I","instore_");
									applicable_bonus = instore_bonus_amount;
								}
								if (datavalue.IndexOf("O")>-1)
								{
									revstr =  datavalue.Replace("O","online_");
									applicable_bonus = online_bonus_amount;
								}
								if (TransactionAmounts.Contains(revstr))
								{
									int amount = (int)TransactionAmounts[revstr];
									amount += applicable_bonus;
									future_revenue += amount;
									//string debugstr2 = dataname +"   datavalue:"+datavalue+"  amount:"+amount.ToString()+ "  future_revenue:"+future_revenue.ToString();
									//debugstr2 += "   revstr:"+revstr+"  amount:"+amount.ToString();
									//System.Diagnostics.Debug.WriteLine(debugstr2);
								}
							}
						}
						//System.Diagnostics.Debug.WriteLine(" future_revenue :" + future_revenue.ToString());
					}
				}
			}
			//System.Diagnostics.Debug.WriteLine(" future_revenue :" + future_revenue.ToString());
			//System.Diagnostics.Debug.WriteLine(" currentGoodRevenue :" + currentGoodRevenue.ToString());
			//System.Diagnostics.Debug.WriteLine(" currentLostRevenue :" + currentLostRevenue.ToString());

			max_revenue = currentGoodRevenue + currentLostRevenue + future_revenue;

			if (UseNewSIPOffsetBonusModel)
			{
				max_revenue = max_revenue + DetermineSIPOffsets();
			}
			return max_revenue;
		}

		/// <summary>
		/// The older system used to recalculate the Maxrevenue when the stores bonus levels changed. 
		/// We recalculate the max revenue level when we installed new stuff which affected the bonus level
		/// we reconsidered the future transaction values as they would be greater with the new bonus
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="attrs"></param>
		void storeNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			//we are only interested in bonus amount changes 
			//we get the last transaction directly when we need it 
			Boolean RefreshNeeded = false;

			if (attrs != null)
			{
				if (attrs.Count > 0)
				{
					foreach(AttributeValuePair avp in attrs)
					{
						//Extraction of the data attribute
						string attribute = avp.Attribute;
						string newValue = avp.Value;
						if ((attribute.ToLower() == "instore_bonus")||(attribute.ToLower() == "online_bonus"))
						{
							//System.Diagnostics.Debug.WriteLine("----------------------------------------");
							//System.Diagnostics.Debug.WriteLine("----------------------------------------");
							//System.Diagnostics.Debug.WriteLine("Attr:"+attribute + "  newValue:"+newValue);
							RefreshNeeded = true;
						}
					}
				}
			}
			if (RefreshNeeded)
			{
				this.RefreshMaxAppsProcessed();
			}
		}

		/// <summary>
		/// Checking for a refresh request which is triggered by attributes on the revenue Node
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="attrs"></param>
		void ApplicationsProcessedNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			if (attrs != null)
			{
				if (attrs.Count > 0)
				{
					foreach(AttributeValuePair avp in attrs)
					{
						string attribute = avp.Attribute;
						string newValue = avp.Value;
						if ((attribute.ToLower() == "max_apps_refresh") && (newValue.ToLower() == "true"))
						{
							this.RefreshMaxAppsProcessed();
							ApplicationsProcessedNode.SetAttribute("max_apps_refresh", "false");
						}
					}
				}
			}
		}
	}
}
