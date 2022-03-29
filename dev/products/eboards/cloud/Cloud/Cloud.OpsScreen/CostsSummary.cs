using System.Collections.Generic;
using System.Text;

using LibCore;
using GameManagement;
using Network;
using Logging;

namespace Cloud.OpsScreen
{
	public class CostsSummary
	{
		class Cost
		{
			public int Amount;
			public string Name;

			public Cost (string name)
				: this (name, 0)
			{
			}

			public Cost (string name, int amount)
			{
				Name = name;
				Amount = amount;
			}

			public override string ToString ()
			{
				return CONVERT.Format("{0}: ${1}", Name, Amount);
			}
		}

		class OwnerCosts
		{
			public int TotalCapEx
			{
				get
				{
					int total = 0;
					foreach (Cost capex in capExes)
					{
						total += capex.Amount;
					}

					return total;
				}
			}

			public List<Cost> capExes;

			public int TotalOpEx
			{
				get
				{
					return rackOpex.Amount + serverOpex.Amount + storageOpex.Amount + powerOpex.Amount;
				}
			}

			public Cost rackOpex;
			public Cost serverOpex;
			public Cost storageOpex;
			public Cost powerOpex;

			public OwnerCosts ()
			{
				capExes = new List<Cost> ();

				rackOpex = new Cost ("Racks");
				serverOpex = new Cost ("Servers");
				storageOpex = new Cost ("Storage");
				powerOpex = new Cost ("Power");
			}

			public override string ToString ()
			{
				StringBuilder builder = new StringBuilder ();
				builder.AppendLine(CONVERT.Format("Total CapEx: ${0}", TotalCapEx));
				if (capExes.Count > 0)
				{
					builder.AppendLine("of which:");
					foreach (Cost cost in capExes)
					{
						builder.AppendLine(CONVERT.Format("    {0}", cost.ToString()));
					}
				}

				builder.AppendLine(CONVERT.Format("Total OpEx: ${0}", TotalOpEx));
				builder.AppendLine("of which:");
				foreach (Cost cost in new Cost [] { rackOpex, serverOpex, storageOpex, powerOpex })
				{
					builder.AppendLine(CONVERT.Format("    {0}", cost.ToString()));
				}

				return builder.ToString();
			}
		}

		class RegionCosts
		{
			public int DevBudget;
			public int ServiceDevelopmentSpend;

			public int ITBudget;

			Dictionary<string, OwnerCosts> ownerToCosts;

			public RegionCosts ()
			{
				DevBudget = 0;
				ServiceDevelopmentSpend = 0;
				ITBudget = 0;

				ownerToCosts = new Dictionary<string, OwnerCosts> ();
			}

			public override string ToString ()
			{
				StringBuilder builder = new StringBuilder ();

				builder.AppendLine(CONVERT.Format("Dev budget: ${0}", DevBudget));
				builder.AppendLine(CONVERT.Format("Service dev spend: ${0}", ServiceDevelopmentSpend));
				builder.AppendLine(CONVERT.Format("IT budget: ${0}", ITBudget));

				foreach (string owner in ownerToCosts.Keys)
				{
					builder.AppendLine();
					builder.AppendLine(CONVERT.Format("{0} costs", owner));
					builder.Append(ownerToCosts[owner].ToString());
				}

				return builder.ToString();
			}

			public void AddCapEx (string owner, Cost cost)
			{
				if (! ownerToCosts.ContainsKey(owner))
				{
					ownerToCosts.Add(owner, new OwnerCosts ());
				}
				ownerToCosts[owner].capExes.Add(cost);
			}

			public void AddOpEx (string owner, string type, int amount)
			{
				if (! ownerToCosts.ContainsKey(owner))
				{
					ownerToCosts.Add(owner, new OwnerCosts ());
				}
				OwnerCosts costs = ownerToCosts[owner];

				switch (type)
				{
					case "storage":
						costs.storageOpex.Amount += amount;
						break;

					case "rack":
						costs.rackOpex.Amount += amount;
						break;

					case "server":
						costs.serverOpex.Amount += amount;
						break;

					case "power":
						costs.powerOpex.Amount += amount;
						break;

					default:
						System.Diagnostics.Debug.Assert(false);
						break;
				}
			}
		}

		Dictionary<string, RegionCosts> regionToCosts;

		public CostsSummary (NetworkProgressionGameFile gameFile)
		{
			regionToCosts = new Dictionary<string, RegionCosts> ();

			string logFile = gameFile.GetRoundFile(gameFile.CurrentRound, "NetworkIncidents.log", GameFile.GamePhase.OPERATIONS);
			BasicIncidentLogReader logReader = new BasicIncidentLogReader(logFile);
			logReader.WatchCreatedNodes("Turnover", new LogLineFoundDef.LineFoundHandler (turnover_CreatedNodes));

			NodeTree model = gameFile.NetworkModel;

			foreach (Node business in model.GetNodesWithAttributeValue("type", "business"))
			{
				string region = business.GetAttribute("region");
				RegionCosts costs = new RegionCosts ();
				regionToCosts.Add(region, costs);

				foreach (Node tryRoundBudget in model.GetNamedNode("Budgets").GetChildrenOfType("round_budget"))
				{
					if (tryRoundBudget.GetIntAttribute("round", 0) == gameFile.CurrentRound)
					{
						foreach (Node tryRegionBudget in tryRoundBudget.GetChildrenOfType("budget"))
						{
							if (tryRegionBudget.GetAttribute("business") == business.GetAttribute("name"))
							{
								costs.ITBudget = tryRegionBudget.GetIntAttribute("it_budget", 0);
								costs.DevBudget = tryRegionBudget.GetIntAttribute("dev_budget", 0);
							}
						}
					}
				}
			}

			logReader.Run();
		}

		public void EmitCosts ()
		{
			foreach (string region in regionToCosts.Keys)
			{
				System.Diagnostics.Debug.WriteLine(CONVERT.Format("Region {0}", region));
				System.Diagnostics.Debug.Write(regionToCosts[region].ToString());
				System.Diagnostics.Debug.WriteLine("");
			}
		}

		void turnover_CreatedNodes (object sender, string key, string line, double time)
		{
			string region = BasicIncidentLogReader.ExtractValue(line, "business").Replace(" Business", "");
			RegionCosts regionCosts = regionToCosts[region];

			int amount = (int) (- CONVERT.ParseDouble(BasicIncidentLogReader.ExtractValue(line, "value")));

			string owner = BasicIncidentLogReader.ExtractValue(line, "owner");

			string type = BasicIncidentLogReader.ExtractValue(line, "type");
			if (type == "bill")
			{
				string billType = BasicIncidentLogReader.ExtractValue(line, "bill_type");
				switch (billType)
				{
					case "opex":
						regionCosts.AddOpEx(owner, BasicIncidentLogReader.ExtractValue(line, "opex_type"), amount);
						break;

					case "capex":
						regionCosts.AddCapEx(owner, new Cost (BasicIncidentLogReader.ExtractValue(line, "capex_type"), amount));
						break;

					case "development":
						regionCosts.ServiceDevelopmentSpend += amount;
						break;
				}
			}
		}
	}
}