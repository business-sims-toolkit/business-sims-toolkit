using System;
using System.Collections;
using System.Xml;

using LibCore;
using Network;

using CoreUtils;

namespace Polestar_Retail.OpsEngine
{
	/// <summary>
	///  This handles the monitoring of system impact
	//   Monitors the Stores for wether the different transaction types are down
  //   if all 8 ( 4 stores * instore/online) are up then it's 0%
	/// </summary>
	public class SystemImpactMonitor
	{
		private NodeTree MyNodeTree;
		
		private Node ImpactNode;
		private Hashtable TheStoreNodes = new Hashtable();
		private Hashtable TheStoreBSUNodes = new Hashtable();
		
		//private Hashtable TheStoreBonusAmounts;
		
		public SystemImpactMonitor(NodeTree nt)
		{
			MyNodeTree = nt;

			BuildStoreMonitoring();

			//Connect to the Impact Node for setting the output value  
			ImpactNode = MyNodeTree.GetNamedNode("Impact");
			RebuildImpactValue(); 
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
					storeNode.ChildAdded -=storeNode_ChildAdded;
					storeNode.ChildRemoved -=storeNode_ChildRemoved;
				}
			}
			TheStoreNodes.Clear();
			//Disconnect from Store BSU Nodes 		
			if (TheStoreBSUNodes.Count>0)
			{
				foreach (Node storeBSUNode in TheStoreBSUNodes.Values)
				{
					storeBSUNode.AttributesChanged -=bsunode_AttributesChanged;
				}
			}
			TheStoreBSUNodes.Clear();
		}
		
		private void BuildStoreMonitoring()
		{
			string BizEntityName = CoreUtils.SkinningDefs.TheInstance.GetData("biz");
			//Connect up to the stores for the current Bonus Amounts
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
					storeNode.ChildAdded +=storeNode_ChildAdded;
					storeNode.ChildRemoved +=storeNode_ChildRemoved;
					foreach (Node bsunode in storeNode.getChildren())
					{
						string bsuname = bsunode.GetAttribute("name");
						TheStoreBSUNodes.Add(bsuname,bsunode);
						bsunode.AttributesChanged +=bsunode_AttributesChanged;
					}
				}
			}
		}

		private void storeNode_ChildAdded(Node sender, Node child)
		{
			//if child not known then add it and it's event handler
			string name = child.GetAttribute("name");
			if (TheStoreBSUNodes.Contains(name)==false)
			{
				TheStoreBSUNodes.Add(name,child);
				child.AttributesChanged +=bsunode_AttributesChanged;
			}
		}

		private void storeNode_ChildRemoved(Node sender, Node child)
		{
			//if child known then remove it and it's event handler
			string name = child.GetAttribute("name");
			if (TheStoreBSUNodes.Contains(name))
			{
				TheStoreBSUNodes.Remove(name);
				child.AttributesChanged -=bsunode_AttributesChanged;
			}
		}

		private void bsunode_AttributesChanged(Node sender, ArrayList attrs)
		{
			//if up flag has changed then perform refresh of the Impact Value 
			if (attrs != null)
			{
				if (attrs.Count > 0)
				{
					foreach(AttributeValuePair avp in attrs)
					{
						string attribute = avp.Attribute;
						string newValue = avp.Value;
						if ((attribute.ToLower()=="up"))
						{
							RebuildImpactValue(); 
						}
						if ((attribute.ToLower()=="transaction_type"))
						{
							RebuildImpactValue(); 
						}
					}
				}
			}
		}

		private void SetImpactValue(int percent)
		{
			ImpactNode.SetAttribute("impact",CONVERT.ToStr(percent));
		}

		private void CheckStore(Node storeNode, out Boolean instore_up, out Boolean online_up) 
		{
			int portal_instore_requirecount = 0;
			int portal_online_requirecount = 0;
			int portal_instore_upcount = 0;
			int portal_online_upcount = 0;
			string st = string.Empty;

			instore_up = true;
			online_up = true;
			//System.Diagnostics.Debug.WriteLine("===========================================================");
			//System.Diagnostics.Debug.WriteLine("===========================================================");
			string storename = storeNode.GetAttribute("name");
			//System.Diagnostics.Debug.WriteLine("Checking for Store "+storename);

			foreach (Node n1 in storeNode.getChildren())
			{
				string name = n1.GetAttribute("name");
				string transtype = n1.GetAttribute("transaction_type");
				string up_status = n1.GetAttribute("up");
				//int impact_status = n1.GetIntAttribute("impactkmh",0);
				Boolean hasImpact = n1.GetBooleanAttribute("has_impact",false);

				transtype = transtype.ToLower();
				up_status = up_status.ToLower();
				st = name;
			
				//if (impact_status != 0)
				if (hasImpact)
				{
					switch (transtype)
					{
						case "both":
							portal_instore_requirecount++;
							portal_online_requirecount++;
							if (up_status=="true")
							{
								portal_instore_upcount++;
								portal_online_upcount++;
								st += "BOTH OK";
							}
							else
							{
								st += "BOTH FAIL";
							}
						

							break;
						case "instore":
							portal_instore_requirecount++;
							if (up_status=="true")
							{
								portal_instore_upcount++;
								st += "INSTORE ONLY OK";
							}
							else
							{
								st += "INSTORE ONLY FAIL";
							}
							break;
						case "online":
							portal_online_requirecount++;
							if (up_status=="true")
							{
								portal_online_upcount++;
								st += "ONLINE ONLY OK";
							}
							else
							{
								st += "ONLINE ONLY FAIL";
							}
							break;
						case "none":
							st += "NONE";
							break;
					}
				}
				//System.Diagnostics.Debug.WriteLine("  "+ st);
				//string st = "Name: "+name+ " TT:"+transtype+ " up_status:"+up_status.ToString();
				//st+= " PIR:"+portal_instore_requirecount.ToString();
				//st+= " POR:"+portal_instore_requirecount.ToString();
				//st+= " PIU:"+portal_instore_upcount.ToString();
				//st+= " POU:"+portal_online_upcount.ToString();
				//System.Diagnostics.Debug.WriteLine(st);
			}
			if (portal_instore_requirecount != portal_instore_upcount)
			{
				instore_up = false;
				//System.Diagnostics.Debug.WriteLine(" INSTORE FAIL");
			}
			else
			{
				instore_up = true;
				//System.Diagnostics.Debug.WriteLine(" INSTORE OK");
			}
			if (portal_online_requirecount != portal_online_upcount)
			{
				online_up = false;
				//System.Diagnostics.Debug.WriteLine(" ONLINE FAIL");
			}
			else
			{
				online_up = true;
				//System.Diagnostics.Debug.WriteLine(" ONLINE OK");
			}
		}

		private void RebuildImpactValue()
		{
			int store_count=0;
			int portal_instore_upcount=0;
			int portal_online_upcount=0;
			int percent_up = 0;
			int percent_down = 0;
			Boolean instore_up = false;
			Boolean	online_up = false;
			string instore_flag_str = "false";
			string online_flag_str = "false";
			//
			//System.Diagnostics.Debug.WriteLine("#####################################################################");

			foreach (Node storenode in TheStoreNodes.Values)
			{
				string storename = storenode.GetAttribute("name");
				instore_flag_str = "false";
				online_flag_str = "false";
				CheckStore(storenode, out instore_up, out online_up);
				store_count++;
				if (instore_up)
				{
					portal_instore_upcount++;
					instore_flag_str = "true";
				}
				if (online_up)
				{
					portal_online_upcount++;
					online_flag_str = "true";
				}
				//Update the Portal Tags
				storenode.SetAttribute("up_instore",instore_flag_str);
				storenode.SetAttribute("up_online",online_flag_str);
				//System.Diagnostics.Debug.WriteLine("PORTAL "+storename+" IN:"+instore_flag_str+ " ON:"+online_flag_str);
			}
			//System.Diagnostics.Debug.WriteLine("#####################################################################");

			//Update the Percentage Impact, we show the percentage of the portal which are down
			percent_up = ((portal_instore_upcount + portal_online_upcount)*100) / (store_count * 2);
			percent_down = 100 - percent_up;

			SetImpactValue(percent_down);
		}

	}
}
