using System;
using System.Collections;
using System.Xml;

using LibCore;
using Network;

using CoreUtils;

namespace Polestar_Retail.OpsEngine
{
	/// <summary>
	/// This calculates the Max Revenue For this round 
	/// It is refreshed by an attribute request on the reveneue node. 
	/// It can operate in two modes
	///   A, SIP Bonus in Project Node  
	///     The max revenue is the (transactions * (base value + store Bonus values)) + Project Offsets
	///     The Poject offesets are defined in the creation section of the SIPs
	///   B, In Round Recalculation of Max Revenue when the Stores bonus values are chnaged.
	///     The older method is (transactions * (base value + store Bonus values))
	///     and we recalculate the future transaction when the store bonus are chnaged. 
	/// Currently the Mode A system is hard coded inside the constructor 
	/// </summary>
	public class MaxRevenueManager 
	{
		private NodeTree MyNodeTree;
		
		private Node TransactionProfilesNode;
		private Node TransactionValuesNode;
		private Node RevenueNode;
		private Hashtable TheStoreNodes = new Hashtable();
		//private Hashtable TheStoreBonusAmounts;
		private Hashtable TheProfilePoints = new Hashtable();
		private Hashtable TransactionAmounts = new Hashtable();
		private int MaxTransactionNumber=0;
		private bool UseNewSIPOffsetBonusModel = true;  
		
		public MaxRevenueManager(NodeTree nt)
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
						storeNode.AttributesChanged +=storeNode_AttributesChanged;
					}
				}
			}
//			if (TheStoreNodes.Count>0)
//			{
//				BuildStoreData();
//			}

			//Connect to the Transations Node (What Has Happened)
			RevenueNode = MyNodeTree.GetNamedNode("Revenue");
			RevenueNode.AttributesChanged +=RevenueNode_AttributesChanged;
			CheckForEmptyMaxRevenue();

			Boolean RefreshRequired = RevenueNode.GetBooleanAttribute("revenue_refresh", false);
			if (RefreshRequired)
			{
				RefreshMaxRevenue();
				RevenueNode.SetAttribute("revenue_refresh","false");
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
						storeNode.AttributesChanged -= storeNode_AttributesChanged;
					}
				}
			}
			TheStoreNodes.Clear();
		}

		private void BuildProfilesCache()
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

		private void BuildTransactionRevenueCache()
		{
			TransactionAmounts.Clear();
			foreach (Node n in TransactionValuesNode.getChildren())
			{
				string eventtype = n.GetAttribute("event_type");
				int revenuelevel = n.GetIntAttribute("revenuelevel",0);
				int revenue = n.GetIntAttribute("revenue",0);

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

		private void CheckForEmptyMaxRevenue()
		{
			int currentRevenue = RevenueNode.GetIntAttribute("max_revenue",-1);
			if (currentRevenue==-1)
			{
				RefreshMaxRevenue();
			}
		}

		private void RefreshMaxRevenue()
		{
			int max_revenue = CalculateMaxRevenue();
			this.RevenueNode.SetAttribute("max_revenue", CONVERT.ToStr(max_revenue));
		}

		private int DetermineSIPOffsets()
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

		private int CalculateMaxRevenue()
		{
			//get the current spend money 
			int currentGoodRevenue = RevenueNode.GetIntAttribute("revenue",-1);
			int currentLostRevenue = RevenueNode.GetIntAttribute("revenue_lost",-1);
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
								}
							}
						}
					}
				}
			}

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
		private void storeNode_AttributesChanged(Node sender, ArrayList attrs)
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
				RefreshMaxRevenue();
			}
		}

		/// <summary>
		/// Checking for a refresh request which is triggered by attributes on the revenue Node
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="attrs"></param>
		private void RevenueNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			if (attrs != null)
			{
				if (attrs.Count > 0)
				{
					foreach(AttributeValuePair avp in attrs)
					{
						string attribute = avp.Attribute;
						string newValue = avp.Value;
						if ((attribute.ToLower()=="revenue_refresh")&&(newValue.ToLower() == "true"))
						{
							RefreshMaxRevenue();
							RevenueNode.SetAttribute("revenue_refresh","false");
						}
					}
				}
			}
		}
	}
}
