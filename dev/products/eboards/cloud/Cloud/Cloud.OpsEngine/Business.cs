using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

using Network;

using LibCore;

namespace Cloud.OpsEngine
{
	public class Business : IDisposable
	{
		NodeTree model;
		Node businessNode;
		Node timeNode;

		Dictionary<Node, BusinessService> nodeToBusinessService;

		BauManager bauManager;
		OrderExecutor orderExecutor;
		Biller biller;

		public Business (NodeTree model, Node businessNode, BauManager bauManager, OrderExecutor orderExecutor, Biller biller)
		{
			this.model = model;
			this.businessNode = businessNode;
			this.bauManager = bauManager;
			this.orderExecutor = orderExecutor;
			this.biller = biller;

			nodeToBusinessService = new Dictionary<Node, BusinessService> ();

			timeNode = model.GetNamedNode("CurrentTime");
			timeNode.AttributesChanged += new Node.AttributesChangedEventHandler (timeNode_AttributesChanged);

			businessNode.ChildAdded += new Node.NodeChildAddedEventHandler (businessNode_ChildAdded);
			businessNode.ChildRemoved += new Node.NodeChildRemovedEventHandler (businessNode_ChildRemoved);

			foreach (Node businessService in businessNode.GetChildrenOfType("business_service"))
			{
				AddBusinessService(businessService);
			}
		}

		void timeNode_AttributesChanged (Node sender, System.Collections.ArrayList attrs)
		{
			bool newMinute = false;

			foreach (AttributeValuePair avp in attrs)
			{
				if (avp.Attribute == "seconds")
				{
					int seconds = LibCore.CONVERT.ParseInt(avp.Value);

					if ((seconds % 60) < 2)
					{
						newMinute = true;
						break;
					}
				}
			}

			if (newMinute)
			{
				UpdateFinancials();
			}
		}

		public void Dispose ()
		{
			foreach (Node businessService in new List<Node> (nodeToBusinessService.Keys))
			{
				RemoveBusinessService(businessService);
			}

			timeNode.AttributesChanged -= new Node.AttributesChangedEventHandler (timeNode_AttributesChanged);
		}

		void businessNode_ChildAdded (Node sender, Node child)
		{
			AddBusinessService(child);
		}

		void businessNode_ChildRemoved (Node sender, Node child)
		{
			RemoveBusinessService(child);
		}

		void AddBusinessService (Node businessServiceNode)
		{
			nodeToBusinessService.Add(businessServiceNode, new BusinessService (model, businessServiceNode, bauManager, orderExecutor, biller));
			businessServiceNode.AttributesChanged += new Node.AttributesChangedEventHandler (businessServiceNode_AttributesChanged);

			UpdateFinancials();
		}

		void RemoveBusinessService (Node businessServiceNode)
		{
			BusinessService businessService = nodeToBusinessService[businessServiceNode];
			nodeToBusinessService.Remove(businessServiceNode);
			businessServiceNode.AttributesChanged -= new Node.AttributesChangedEventHandler (businessServiceNode_AttributesChanged);
			businessService.Dispose();

			UpdateFinancials();
		}

		void businessServiceNode_AttributesChanged (Node sender, System.Collections.ArrayList attrs)
		{
			UpdateFinancials();
		}

		void UpdateFinancials ()
		{
			// Tot up the revenue from business services...
			double revenueEarned = 0;
			double potentialExtraRevenue = 0;
			foreach (Node businessService in businessNode.GetChildrenOfType("business_service"))
			{
				revenueEarned += businessService.GetDoubleAttribute("revenue_earned", 0);
				potentialExtraRevenue += businessService.GetDoubleAttribute("potential_extra_revenue", 0);
			}

			// ...and add in that from any demands that haven't been commissioned.
			double potentialExtraRevenueIncludingMissedDemands = potentialExtraRevenue;
			Node roundVariables = model.GetNamedNode("RoundVariables");
			int round = roundVariables.GetIntAttribute("current_round", 0);
			int currentTime = model.GetNamedNode("CurrentTime").GetIntAttribute("seconds", 0);
			foreach (Node demand in model.GetNamedNode("Demands").GetChildrenOfType("demand"))
			{
				if (demand.GetBooleanAttribute(LibCore.CONVERT.Format("available_in_round_{0}", round), false)
					&& (demand.GetAttribute("business") == businessNode.GetAttribute("name"))
					&& demand.GetBooleanAttribute("active", false)
					&& (model.GetNamedNode(demand.GetAttribute("business_service")) == null))
				{
					int announceTime = roundVariables.GetIntAttribute("demand_announcement_duration_trading_periods", 0);
					if (demand.GetBooleanAttribute("optional", false))
					{
						announceTime = roundVariables.GetIntAttribute("optional_demand_announcement_duration_trading_periods", 0);
					}

					if ((demand.GetIntAttribute("delay", 0) - currentTime) <= (announceTime * 60))
					{
						string attributeName = LibCore.CONVERT.Format("round_{0}_instances", round);
						int instances = demand.GetIntAttribute(attributeName, 0);

						double demandPotentialExtraRevenue = (demand.GetDoubleAttribute("trades_per_realtime_minute", 0)
													            * demand.GetDoubleAttribute("revenue_per_trade", 0)
													            * instances);

						potentialExtraRevenueIncludingMissedDemands += demandPotentialExtraRevenue;

						if ((demand.GetIntAttribute("delay", 0) + demand.GetIntAttribute("duration", 0)) > currentTime)
						{
							potentialExtraRevenue += demandPotentialExtraRevenue;
						}
					}
				}
			}

			// Tot up the spend too.
			double spend = 0;
			Node turnover = model.GetNamedNode("Turnover");
			foreach (Node trade in turnover.getChildren())
			{
				if ((trade.GetAttribute("business") == businessNode.GetAttribute("name"))
					&& (trade.GetAttribute("type") == "bill")
					&& (trade.GetAttribute("bill_type") == "development"))
				{
					spend += - trade.GetDoubleAttribute("value", 0);
				}
			}

			List<AttributeValuePair> attributes = new List<AttributeValuePair> ();
			if (businessNode.GetDoubleAttribute("potential_extra_revenue_excluding_missed_demands", 0) != potentialExtraRevenue)
			{
				attributes.Add(new AttributeValuePair ("potential_extra_revenue_excluding_missed_demands", potentialExtraRevenue));
			}

			if (businessNode.GetDoubleAttribute("potential_extra_revenue_including_missed_demands", 0) != potentialExtraRevenueIncludingMissedDemands)
			{
				attributes.Add(new AttributeValuePair ("potential_extra_revenue_including_missed_demands", potentialExtraRevenueIncludingMissedDemands));
			}

			if (businessNode.GetDoubleAttribute("spend", 0) != spend)
			{
				attributes.Add(new AttributeValuePair ("spend", spend));
			}

			if (businessNode.GetDoubleAttribute("revenue_earned", 0) != revenueEarned)
			{
				attributes.Add(new AttributeValuePair ("revenue_earned", revenueEarned));
			}

			if (attributes.Count > 0)
			{
				businessNode.SetAttributes(attributes);
			}
		}
	}
}