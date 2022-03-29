using System;
using System.Collections;
using System.Xml;

using LibCore;
using Network;

using CoreUtils;

namespace Polestar_PM.OpsEngine
{
	/// <summary>
	/// This Handles the Revenue for the system 
	/// Mode 1 On the Hour (If the bsu is up on the hour then we book the revenue)
	/// Mode 2 Continous (If the bsu is up on the real sec then we book the revenue /60)
	/// </summary>
	public class RevenueManager 
	{
		protected NodeTree MyNodeTreeHandle;
		private Node currentTimeNode;
		private Node RevenueNode;
		protected Hashtable ExchangeNodes = new Hashtable();
		protected Boolean BookingOnTheHour = false;
		
		public RevenueManager(NodeTree model, bool tmpBookingOnTheHour)
		{
			MyNodeTreeHandle = model;
			BookingOnTheHour = tmpBookingOnTheHour;

			RevenueNode = MyNodeTreeHandle.GetNamedNode("Revenue");
			BuildMonitoringSystem();
			
			currentTimeNode = MyNodeTreeHandle.GetNamedNode("CurrentTime");
			currentTimeNode.AttributesChanged += new Network.Node.AttributesChangedEventHandler(currentTimeNode_AttributesChanged);
		}

		public void Dispose()
		{
			ClearMonitoring();
			if (RevenueNode != null)
			{
				RevenueNode = null;
			}
			if (currentTimeNode != null)
			{
				currentTimeNode.AttributesChanged -= new Network.Node.AttributesChangedEventHandler(currentTimeNode_AttributesChanged);
				currentTimeNode = null;
			}
		}
			
		protected void BuildMonitoringSystem()
		{
			ClearMonitoring();
			//System.Diagnostics.Debug.WriteLine("Building the Monitoring System"); 

			ArrayList slots = MyNodeTreeHandle.GetNodesWithAttributeValue("type","Exchange");
			foreach(Node ExchangeNode in slots)
			{
				string name_str = ExchangeNode.GetAttribute("name");
				string desc_str = ExchangeNode.GetAttribute("desc");
				//System.Diagnostics.Debug.WriteLine("  Exchge "+name_str ); 
				ExchangeNodes.Add(name_str,ExchangeNode);
			}
		}

		protected void ClearMonitoring()
		{
			//disconnect the Exchange Nodes
			foreach (string exchge_name_str in ExchangeNodes.Keys)
			{
				Node ExchangeNode = (Node) ExchangeNodes[exchge_name_str];
				//disconnect and handlers
			}
			ExchangeNodes.Clear();
		}

		private void UpdateRevenueNode(int AdditionalBookedRev, int AdditionalLostRev) 
		{
			int currentGoodRevenue = RevenueNode.GetIntAttribute("revenue",-1);
			int currentLostRevenue = RevenueNode.GetIntAttribute("revenue_lost",-1);
			int newGoodRevenue = currentGoodRevenue + AdditionalBookedRev;
			int newLostRevenue = currentLostRevenue + AdditionalLostRev;
			RevenueNode.SetAttribute("revenue", CONVERT.ToStr(newGoodRevenue));
			RevenueNode.SetAttribute("revenue_lost", CONVERT.ToStr(newLostRevenue));
		}

		private void handleRevenueUpdate(bool onHour) 
		{
			//need to scan through the BSUs  
			foreach (string exchge_name_str in ExchangeNodes.Keys)
			{
				Node ExchangeNode = (Node) ExchangeNodes[exchge_name_str];
				string enode_name = ExchangeNode.GetAttribute("name");
				int additional_revenue_to_book=0; 
				int additional_revenue_to_lose=0; 
				if (ExchangeNode != null)
				{
					foreach (Node bsu_node in ExchangeNode.getChildren())
					{	
						string bsunode_name = bsu_node.GetAttribute("name");
						bool status = bsu_node.GetBooleanAttribute("up", true);
						int rev_impact = bsu_node.GetIntAttribute("rev_impact",0);
						int rev_hour = bsu_node.GetIntAttribute("rev_hour",0);
						int rev_amount= rev_hour;
						if (onHour==false)
						{
							rev_amount = rev_amount / 60;
						}
						if (status)
						{
							additional_revenue_to_book+=rev_amount;
						}
						else
						{
							additional_revenue_to_lose+=rev_amount;
						}
						//System.Diagnostics.Debug.WriteLine("Exchge "+enode_name + " "+ bsunode_name+ " " + rev_hour.ToString() + "["+status.ToString()+"]");
					}
					//System.Diagnostics.Debug.WriteLine("Exchge "+enode_name + "["+additional_revenue_to_book.ToString()+"]"+ "["+additional_revenue_to_lose.ToString()+"]");
					UpdateRevenueNode(additional_revenue_to_book, additional_revenue_to_lose);
				}
			}
		}

		private void currentTimeNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			foreach(AttributeValuePair avp in attrs)
			{
				if(avp.Attribute == "seconds")
				{
					int second_count = int.Parse(avp.Value);
					if (BookingOnTheHour)
					{
						if (second_count % 60 == 0)
						{
							handleRevenueUpdate(true);
						}
					}
					else
					{
						handleRevenueUpdate(false);
					}
				}
			}
		}

	}
}
