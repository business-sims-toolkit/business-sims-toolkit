using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

using LibCore;
using Network;
using CoreUtils;

namespace DevOps.OpsScreen
{
	internal class DevMetricView : BasePanel
	{
		NodeTree network;
		Node newServicesInstallNode;
		Node transNode;
		Node budgetNode;
		Node timeNode;
	    Node completedServices;

		Dictionary<string, Node> trackedNewServices;
		Dictionary<string, Node> trackedTransactions;

        Dictionary<string, Dictionary<string, Node>> trackedTransactionsPerStore;

		Color textColour = Color.White;
	    Color profitColour = Color.White;
	    Color panelBackColour = Color.Transparent;
		Color lossColour = Color.White;
        Color lossBackColour = Color.Red;


		int potentialValue = 0;
		int investmentValue = 0;
		int runningBudget;
        
	    Dictionary<string, Dictionary<string, int>> storeToCurrentBonuses;
        
	    Dictionary<string, Dictionary<string, int>> storeToPotentialDeltas;

		int remainingInstoreTransactions;
		int remainingOnlineTransactions;

		
		int revenueMade = 0;

		AttributeValuePairPanel investmentPanel;
		AttributeValuePairPanel potentialPanel;
		AttributeValuePairPanel profitLossPanel;

		string currencyString = SkinningDefs.TheInstance.GetData("currency_symbol");

	    int round;

		public DevMetricView(NodeTree model, int round)
		{
			network = model;
            this.round = round;

			trackedNewServices = new Dictionary<string, Node>();
			trackedTransactions = new Dictionary<string, Node>();

            trackedTransactionsPerStore = new Dictionary<string, Dictionary<string, Node>>();
            
		    storeToCurrentBonuses = new Dictionary<string, Dictionary<string, int>>();
		    storeToPotentialDeltas = new Dictionary<string, Dictionary<string, int>>();
			
		    Setup();
		}
        
		void Setup()
		{
			BackColor = Color.FromArgb(37, 37, 37);

		    string bizName = SkinningDefs.TheInstance.GetData("biz") + "s";
            foreach (Node store in network.GetNamedNode(bizName))
            {
                string name = store.GetAttribute("name");

                storeToCurrentBonuses.Add(name, new Dictionary<string, int>());

                storeToCurrentBonuses[name].Add("instore", 0);
                storeToCurrentBonuses[name].Add("online", 0);


                storeToPotentialDeltas.Add(name, new Dictionary<string, int>());

                storeToPotentialDeltas[name].Add("instore", 0);
                storeToPotentialDeltas[name].Add("online", 0);
            }

            newServicesInstallNode = network.GetNamedNode("BeginNewServicesInstall");

            newServicesInstallNode.ChildAdded += newServicesInstallNode_ChildAdded;
            newServicesInstallNode.ChildRemoved += newServicesInstallNode_ChildRemoved;

            timeNode = network.GetNamedNode("CurrentTime");

            transNode = network.GetNamedNode("Transactions");
            transNode.ChildAdded += transNode_ChildAdded;
            transNode.ChildRemoved += transNode_ChildRemoved;

            string budgetName = CONVERT.Format("Budgets");
            budgetNode = network.GetNamedNode(budgetName).GetChildWithAttributeValue("round", round.ToString());
            runningBudget = budgetNode.GetIntAttribute("budget", 0);
            budgetNode.AttributesChanged += budgetNode_AttributesChanged;

            Node transactionStatus = network.GetNamedNode("TransactionStatus");
            remainingInstoreTransactions = transactionStatus.GetIntAttribute("instoreMaxPerStore", 12);
            remainingOnlineTransactions = transactionStatus.GetIntAttribute("onlineMaxPerStore", 12);

            completedServices = network.GetNamedNode("CompletedNewServices")
                .GetChildWithAttributeValue("round", round.ToString());

			var attributeSizingReference = "TRANSACTIONS";
			var valueSizingReference = "$99,999,999";

			investmentPanel = CreateAvpPanel("Investment", attributeSizingReference, valueSizingReference);

            Controls.Add(investmentPanel);

		    potentialPanel = CreateAvpPanel("Potential", attributeSizingReference, valueSizingReference);

            Controls.Add(potentialPanel);

		    profitLossPanel = CreateAvpPanel("Profit/Loss", attributeSizingReference, valueSizingReference);

            Controls.Add(profitLossPanel);
        }

        AttributeValuePairPanel CreateAvpPanel (string text, string attributeSizingReference, string valueSizingReference)
        {
            AttributeValuePairPanel avpPanel = new AttributeValuePairPanel(text, AttributeValuePairPanel.PanelLayout.TopToBottom, 0.5f, attributeSizingReference, valueSizingReference)
                                               {
                                                   AttributeColour = textColour,
                                                   AttributeFontStyle = FontStyle.Regular,
                                                   ValueColour = textColour,
                                                   ValueFontStyle = FontStyle.Bold,
                                                   Padding = new Padding(5),
                                                   ValueText = "$0",
                                                   BackColor = panelBackColour
                                               };

            return avpPanel;
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);

            DoLayout();
        }

        void DoLayout()
        {
	        int margin = Height / 8;
	        var subPanelSize = new Size ((Width - (5 * margin)) / 3, Height - (2 * margin));

	        investmentPanel.Bounds = new Rectangle (margin, margin, subPanelSize.Width, subPanelSize.Height);
	        potentialPanel.Bounds = new Rectangle (investmentPanel.Right + margin, margin, subPanelSize.Width, subPanelSize.Height);
	        profitLossPanel.Bounds = new Rectangle (potentialPanel.Right + margin, margin, subPanelSize.Width, subPanelSize.Height);
        }

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
                foreach (Node newService in trackedNewServices.Values)
                {
                    newService.AttributesChanged -= service_AttributesChanged;
                }

                trackedNewServices.Clear();

                newServicesInstallNode.ChildAdded -= newServicesInstallNode_ChildAdded;
                newServicesInstallNode.ChildRemoved -= newServicesInstallNode_ChildRemoved;

                transNode.ChildAdded -= transNode_ChildAdded;
                transNode.ChildRemoved -= transNode_ChildRemoved;

                foreach (Node trans in trackedTransactions.Values)
                {
                    trans.AttributesChanged -= transaction_AttributesChanged;
                }

                trackedTransactions.Clear();

                budgetNode.AttributesChanged -= budgetNode_AttributesChanged;
			}

			base.Dispose(disposing);
		}

		void TrackNewService(Node service)
		{
			service.AttributesChanged += service_AttributesChanged;

			string name = service.GetAttribute("name");
			trackedNewServices.Add(name, service);

			// Get the bonus for this new service.
            int bonus = CONVERT.ParseInt(service.GetAttribute("optimum_gain_per_minute")); 
			
			// Find out if the service will affect instore or online transactions, or both.
			string channel = service.GetAttribute("transaction_type"); 
            
            int currentTime = CONVERT.ParseInt(timeNode.GetAttribute("seconds"));
            int endTime = CONVERT.ParseInt(timeNode.GetAttribute("round_duration"));
            
		    string stores = service.GetAttribute("stores");

		    int potential = CalculatePotential(currentTime, endTime, channel, stores, bonus);

		    potentialValue += potential;

		    LogServicePotential(name, potential);

			UpdatePotentialDisplay();
		}

        void LogServicePotential(string name, int potential)
        {
            new Node(network.GetNamedNode("CostedEvents"), "service_potential", 
                "", new List<AttributeValuePair>
                {
                    new AttributeValuePair("type", "service_potential"),
                    new AttributeValuePair("service_name", name),
                    new AttributeValuePair("service_potential", potential)
                });
        }
        
        int CalculatePotential(int startTime, int endTime, string channel, string stores, int bonus)
        {
            int potential = 0;

            string bizName = SkinningDefs.TheInstance.GetData("biz");
            foreach (string store in stores.Split(','))
            {
                string storeName = bizName + " " + store;

                // Apply the bonus to the potential deltas for this store.
                // Bonus may be positive or negative.
                if (channel.Equals("both"))
                {
                    storeToPotentialDeltas[storeName]["instore"] += bonus;
                    storeToPotentialDeltas[storeName]["online"] += bonus;
                }
                else
                {
                    storeToPotentialDeltas[storeName][channel] += bonus;
                }

                int numTrans = GetNumPotentialTransactions(startTime, endTime, channel, storeName);

                potential += numTrans * bonus;

            }

            return potential;
        }

        int GetNumPotentialTransactions(int startTime, int endTime, string channel, string store)
        {
            int transCount = 0;

            if (!trackedTransactionsPerStore.ContainsKey(store) || 
                (trackedTransactionsPerStore.ContainsKey(store) && 
                trackedTransactionsPerStore[store].Count == 0) && 
                startTime <= 1)
            {
                // Round hasn't started yet so use hardcoded values.
                switch (channel)
                {
                    case "both":
                        transCount = remainingInstoreTransactions + remainingOnlineTransactions;
                        break;
                    case "instore":
                        transCount = remainingInstoreTransactions;
                        break;
                    case "online":
                        transCount = remainingOnlineTransactions;
                        break;
                }
            }
            else
            {
                foreach (Node transaction in trackedTransactionsPerStore[store].Values)
                {
                    int transTime = CONVERT.ParseInt(transaction.GetAttribute("time"));

                    if (transTime >= startTime && transTime < endTime)
                    {
                        string eventType = transaction.GetAttribute("event_type");

                        if (channel.Equals("both") ||
                            channel.Equals(eventType))
                        {
                            transCount++;
                        }

                    }
                }
            }


            return transCount;
        }

		int GetNumPotentialTransactions(int startTime, int endTime, string channel)
		{
			int transCount = 0;


			if (trackedTransactions.Count == 0)
			{
				// Round hasn't started yet so use hardcoded values.
				switch (channel)
				{
					case "both":
						transCount = remainingInstoreTransactions + remainingOnlineTransactions;
						break;
					case "instore":
						transCount = remainingInstoreTransactions;
						break;
					case "online":
						transCount = remainingOnlineTransactions;
						break;
				}

				return transCount;
			}

			foreach (Node transaction in trackedTransactions.Values)
			{
				int transTime = CONVERT.ParseInt(transaction.GetAttribute("time"));

				if (transTime >= startTime && transTime < endTime)
				{
					string eventType = transaction.GetAttribute("event_type");

					if (channel.Equals("both"))
					{
						transCount++;
					}
					else if (channel.Equals(eventType))
					{
						transCount++;
					}
				}
			}


			return transCount;
		}

		void StopTrackingNewService(Node service)
		{
			string name = service.GetAttribute("name");

			if (trackedNewServices.ContainsKey(name))
			{
				service.AttributesChanged -= service_AttributesChanged;
				trackedNewServices.Remove(name);
			}
		}

		void UpdateInvestmentDisplay()
		{
			string investmentString = currencyString + CONVERT.ToPaddedStrWithThousands(investmentValue, 0);
			investmentPanel.ValueText = investmentString;
		    investmentPanel.Invalidate();
            UpdateProfitLossDisplay();
		}

		void UpdatePotentialDisplay()
		{
			string potentialString = currencyString + CONVERT.ToPaddedStrWithThousands(potentialValue, 0);
			potentialPanel.ValueText = potentialString;
		    potentialPanel.Invalidate();
		}

		void UpdateProfitLossDisplay()
		{
			int profitLoss = revenueMade - investmentValue;
		    string profitLossString = "";

            if (profitLoss < 0)
            {
                profitLossPanel.ValueColour = lossColour;
                profitLossPanel.ValueBackColour = lossBackColour;
                profitLossString = "-" + currencyString + CONVERT.ToPaddedStrWithThousands(Math.Abs(profitLoss), 0);

            }
            else
            {
                profitLossPanel.ValueColour = profitColour;
                profitLossPanel.ValueBackColour = panelBackColour;
                profitLossString = currencyString + CONVERT.ToPaddedStrWithThousands(profitLoss, 0);
            }
            
			profitLossPanel.ValueText = profitLossString;
            
		    profitLossPanel.Invalidate();

		}

		void budgetNode_AttributesChanged(Node sender, ArrayList attrs)
		{
            // Add the change in budget to the investment total.
            int currentBudget = CONVERT.ParseInt(sender.GetAttribute("budget"));

            investmentValue += (runningBudget - currentBudget);
            runningBudget = currentBudget;

            UpdateInvestmentDisplay();
		}

		void newServicesInstallNode_ChildRemoved(Node sender, Node child)
		{
			StopTrackingNewService(child);
		}

		void newServicesInstallNode_ChildAdded(Node sender, Node child)
		{
			TrackNewService(child);
		}

		void service_AttributesChanged(Node sender, ArrayList attrs)
		{
            foreach(AttributeValuePair avp in attrs)
            {
                if (avp.Attribute.Equals("status"))
                {
                    // New service has been deployed.
                    if (avp.Value.Equals("live"))
                    {
                        // Remove
                        int optimumBonus = sender.GetIntAttribute("optimum_gain_per_minute",0); 
                        int gainBonus = sender.GetIntAttribute("gain_per_minute", 0);

                        string channel = sender.GetAttribute("transaction_type"); // TODO transaction_type needs to be changed to channel
                        
                        string stores = sender.GetAttribute("stores");

                        int servicePotential = 0;

                        string bizName = SkinningDefs.TheInstance.GetData("biz");
                        foreach (string storeNum in stores.Split(','))
                        {
                            string storeName = bizName + " " + storeNum;
                            
                            if (channel.Equals("both"))
                            {
                                storeToCurrentBonuses[storeName]["instore"] += gainBonus;
                                storeToCurrentBonuses[storeName]["online"] += gainBonus;

                                storeToPotentialDeltas[storeName]["instore"] -= optimumBonus;
                                storeToPotentialDeltas[storeName]["online"] -= optimumBonus;
                            }
                            else
                            {
                                storeToCurrentBonuses[storeName][channel] += gainBonus;

                                storeToPotentialDeltas[storeName][channel] -= optimumBonus;
                            }


                            int numTrans = GetNumPotentialTransactions(timeNode.GetIntAttribute("seconds", 0),
                                            timeNode.GetIntAttribute("round_duration", 0), channel, storeName);

                            servicePotential += numTrans * optimumBonus;
                        }
                        
                        // Stop listening to this Node now.
                        StopTrackingNewService(sender);

                        
                        if (servicePotential < 0)
                        {
                            throw new Exception("Potential shouldn't be negative.");
                        }

                        int maxRevenue = network.GetNamedNode("Revenue").GetIntAttribute("max_revenue", 0);

                        maxRevenue += servicePotential;
                        network.GetNamedNode("Revenue").SetAttribute("max_revenue", maxRevenue);

                        break;
                    }
                    else if (avp.Value.Equals("cancelled") || avp.Value.Equals("undo"))
                    {
                        int currentTime = timeNode.GetIntAttribute("seconds", 0);
                        int endTime = timeNode.GetIntAttribute("round_duration", 1500);

                        int bonus = CONVERT.ParseInt(sender.GetAttribute("optimum_gain_per_minute")); //TODO
                        string stores = sender.GetAttribute("stores");

                        int potential = CalculatePotential(currentTime, endTime, sender.GetAttribute("transaction_type"),
                            stores, -bonus);

                        potentialValue += potential;

                        UpdatePotentialDisplay();

                        Debug.Assert(potentialValue >= 0);

                        StopTrackingNewService(sender);
                        break;
                    }
                }
            }
		}

		void transNode_ChildAdded(Node sender, Node child)
		{
            TrackAllTransactions(child);

            string store = child.GetAttribute("store");

            // TODO will need to change to store or something later.
            if (store.Equals("BU 1"))
            {
                TrackTransaction(child);
            }
        }

        void TrackAllTransactions (Node transaction)
        {
            string store = transaction.GetAttribute("store");

            if (!trackedTransactionsPerStore.ContainsKey(store))
            {
                trackedTransactionsPerStore.Add(store, new Dictionary<string, Node>());
            }

            string name = transaction.GetAttribute("name");
            trackedTransactionsPerStore[store].Add(name, transaction);

            transaction.AttributesChanged += transaction_AttributesChanged;

		}

		void TrackTransaction(Node transaction)
		{
            string name = transaction.GetAttribute("name");

            trackedTransactions.Add(name, transaction);
            //transaction.AttributesChanged += transaction_AttributesChanged;

		}

        void transaction_AttributesChanged(Node sender, ArrayList attrs)
		{
            string status = sender.GetAttribute("status");

            if (status.Equals("Handled") || status.Equals("Canceled"))
            {
                string eventType = sender.GetAttribute("event_type");
                string store = sender.GetAttribute("store");

                // Remove value potential delta from current accumulated potential.
                potentialValue -= storeToPotentialDeltas[store][eventType];

                // If it was handled then increment revenue made.
                if (status.Equals("Handled"))
                {
                    
                    if (storeToCurrentBonuses.ContainsKey(store))
                    {
                        int rev = storeToCurrentBonuses[store][eventType];
                        revenueMade += rev;
                    }
                    
                    // Iterate through all of the completed services and if
                    // the service links to the store for this transaction 
                    // then add on the bonus to its accumulating total.
                    foreach (Node service in completedServices)
                    {
                        string channel = service.GetAttribute("transaction_type");
                        // Only check if the service was for the correct transaction type,
                        // i.e. instore/online, or if it's both then always check it.
                        if (channel.Equals("both") || channel.Equals(eventType))
                        {
                            Node bizService = network.GetNamedNode(service.GetAttribute("biz_service_function"));

                            foreach (Node bsu in bizService)
                            {
                                if (bsu.GetAttribute("name").Contains(store))
                                {
                                    int revenue = service.GetIntAttribute("revenue_made", 0);
                                    int bonus = service.GetIntAttribute("gain_per_minute", 0);
                                    int profit = service.GetIntAttribute("profit", 0);
                                    revenue += bonus;
                                    profit += bonus;

                                    service.SetAttributes( new List<AttributeValuePair>
                                                            {
                                                                new AttributeValuePair("revenue_made", revenue),
                                                                new AttributeValuePair("profit", profit)
                                                            });

                                    break;
                                }
                            }
                        }
                        
                    }

                    
                }

                StopTrackingTransaction(sender);
                UpdateProfitLossDisplay();
                UpdatePotentialDisplay();
            }
        }

        void transNode_ChildRemoved(Node sender, Node child)
        {
            StopTrackingTransaction(child);
        }

        void StopTrackingTransaction(Node transaction)
        {
            string store = transaction.GetAttribute("store");

            if (trackedTransactionsPerStore.ContainsKey(store))
            {
                string transName = transaction.GetAttribute("name");
                if (trackedTransactions.ContainsKey(transName))
                {
                    trackedTransactions.Remove(transName);
                    
                }

                if (trackedTransactionsPerStore[store].ContainsKey(transName))
                {
                    trackedTransactionsPerStore[store].Remove(transName);
                    transaction.AttributesChanged -= transaction_AttributesChanged;
                }

            }

            
        }

	}
}
