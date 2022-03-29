using System;
using System.Collections.Generic;
using System.Text;

using LibCore;
using CoreUtils;
using Network;

namespace Cloud.OpsEngine
{
	public class PowerMonitor : IDisposable
	{
		NodeTree model;
		Node time;
		Node turnoverNode;
		Node roundVariablesNode;
		int billedUpTo;

		Biller biller;

		public PowerMonitor (NodeTree model, Biller biller)
		{
			this.model = model;
			this.biller = biller;

			time = model.GetNamedNode("CurrentTime");
			time.AttributesChanged += new Node.AttributesChangedEventHandler (time_AttributesChanged);

			turnoverNode = model.GetNamedNode("Turnover");
			roundVariablesNode = model.GetNamedNode("RoundVariables");

			billedUpTo = 0;

			UpdatePower();
		}

		public void Dispose ()
		{
			time.AttributesChanged -= new Node.AttributesChangedEventHandler (time_AttributesChanged);
		}

		void time_AttributesChanged (Node sender, System.Collections.ArrayList attrs)
		{
			if (time.GetIntAttribute("seconds", 0) > 0)
			{
				UpdateCosts();
			}
		}

		void UpdateCosts ()
		{
			UpdatePower();

			int seconds = time.GetIntAttribute("seconds", 0);
			while ((seconds - billedUpTo) >= 60)
			{
				int billStart = billedUpTo;
				int billDuration = seconds - billedUpTo;
				billedUpTo = seconds;

				// 6 hours of game time to one minute real time.
				int billDurationInGameTime = billDuration * 6 * 3600 / 60;

				foreach (Node business in model.GetNodesWithAttributeValue("type", "business"))
				{
					Node datacenter = model.GetNamedNode(business.GetAttribute("datacenter"));

					List<AttributeValuePair> attributes = new List<AttributeValuePair> ();
					attributes.Add(new AttributeValuePair ("type", "bill"));
					attributes.Add(new AttributeValuePair ("bill_type", "opex"));
					attributes.Add(new AttributeValuePair ("opex_type", "power"));
					attributes.Add(new AttributeValuePair ("owner", "production"));
					attributes.Add(new AttributeValuePair ("business", business.GetAttribute("name")));
					attributes.Add(new AttributeValuePair ("cost_per_kwh", datacenter.GetDoubleAttribute("kwh_cost", 0)));
					attributes.Add(new AttributeValuePair ("power_level_kw", datacenter.GetDoubleAttribute("power_kw", 0)));
					attributes.Add(new AttributeValuePair ("value", - datacenter.GetDoubleAttribute("kwh_cost", 0)
																	* datacenter.GetDoubleAttribute("production_power_kw", 0)
																	* billDurationInGameTime / 3600.0));
					biller.CreateOrUpdateTurnoverItem(attributes);

					attributes.Clear();
					attributes.Add(new AttributeValuePair ("type", "bill"));
					attributes.Add(new AttributeValuePair ("bill_type", "opex"));
					attributes.Add(new AttributeValuePair ("opex_type", "power"));
					attributes.Add(new AttributeValuePair ("owner", "dev&test"));
					attributes.Add(new AttributeValuePair ("business", business.GetAttribute("name")));
					attributes.Add(new AttributeValuePair ("cost_per_kwh", datacenter.GetDoubleAttribute("kwh_cost", 0)));
					attributes.Add(new AttributeValuePair ("power_level_kw", datacenter.GetDoubleAttribute("power_kw", 0)));
					attributes.Add(new AttributeValuePair ("value", - datacenter.GetDoubleAttribute("kwh_cost", 0)
																	* datacenter.GetDoubleAttribute("dev_power_kw", 0)
																	* billDurationInGameTime / 3600.0));
					biller.CreateOrUpdateTurnoverItem(attributes);
				}
			}
		}

		bool DoubleAttributeDiffers (Node node, string attributeName, double attributeValue, double tolerance)
		{
			return (Math.Abs(node.GetDoubleAttribute(attributeName, 0) - attributeValue) >= tolerance);
		}

		void AddIfDifferent (List<AttributeValuePair> attributes, Node node, string attributeName, double attributeValue)
		{
			if (DoubleAttributeDiffers(node, attributeName, attributeValue, 0.01))
			{
				attributes.Add(new AttributeValuePair (attributeName, attributeValue));
			}
		}

		public static bool IsStorageArrayInUse (Node storageArray)
		{
			NodeTree model = storageArray.Tree;

			foreach (Node businessService in model.GetNodesWithAttributeValue("type", "business_service"))
			{
				// Does the business service use this storage array?  And, if it's a dev service, is it still active?
				if ((businessService.GetAttribute("storage_array") == storageArray.GetAttribute("name")
					&& ((! businessService.GetBooleanAttribute("is_dev", false))
						|| (businessService.GetIntAttribute("dev_countdown", 0) > 0))))
				{
					return true;
				}

				// Is the business service a preexisting one?  And the right owner for this storage array?
				// And still doing work?
				if (businessService.GetBooleanAttribute("is_preexisting", false))
				{
					if (businessService.GetAttribute("owner") == storageArray.GetAttribute("owner"))
					{
						Node business = model.GetNamedNode(businessService.GetAttribute("business"));
						Node datacenter = model.GetNamedNode(business.GetAttribute("datacenter"));

						if (storageArray.Parent == datacenter)
						{
							Node servicesUsage = ((Node []) business.GetChildrenOfType("business_as_usual").ToArray(typeof (Node)))[0];

							foreach (Node serviceUsage in servicesUsage.GetChildrenOfType("service_business_as_usual"))
							{
								if (serviceUsage.GetAttribute("business_service") == businessService.GetAttribute("name"))
								{
									foreach (Node bauPoint in serviceUsage.GetChildrenOfType("business_as_usual_data_point"))
									{
										if (bauPoint.GetIntAttribute("cpus_used", 0) > 0)
										{
											return true;
										}
									}
								}
							}
						}
					}
				}
			}

			return false;
		}

		void UpdatePower ()
		{
			foreach (Node datacenter in model.GetNodesWithAttributeValue("type", "datacenter"))
			{
				if (! datacenter.GetBooleanAttribute("hidden", false))
				{
					Dictionary<string, double> ownerToPower = new Dictionary<string, double> ();
					int devRacksActive = 0;
					int productionRacksActive = 0;

					foreach (Node rack in datacenter.GetChildrenOfType("rack"))
					{
						string owner = "production";
						if (rack.GetAttribute("owner") == "dev&test")
						{
							owner = "dev&test";
						}

						int servers = 0;
						double rackPower = rack.GetDoubleAttribute("power_kw", 0);

						foreach (Node server in rack.GetChildrenOfType("server"))
						{
							servers++;
							rackPower += server.GetDoubleAttribute("power_kw", 0);
						}

						if (servers == 0)
						{
							rackPower = 0;
						}

						if (servers > 0)
						{
							if (owner == "dev&test")
							{
								devRacksActive++;
							}
							else
							{
								productionRacksActive++;
							}
						}

						if (ownerToPower.ContainsKey(owner))
						{
							ownerToPower[owner] += rackPower;
						}
						else
						{
							ownerToPower.Add(owner, rackPower);
						}
					}

					foreach (Node storageArray in datacenter.GetChildrenOfType("storage_array"))
					{
						if (IsStorageArrayInUse(storageArray))
						{
							double storagePower = storageArray.GetDoubleAttribute("power_kw", 0);

							string owner = "production";
							if (storageArray.GetAttribute("owner") == "dev&test")
							{
								owner = "dev&test";
							}

							if (ownerToPower.ContainsKey(owner))
							{
								ownerToPower[owner] += storagePower;
							}
							else
							{
								ownerToPower.Add(owner, storagePower);
							}
						}
					}

					List<AttributeValuePair> attributes = new List<AttributeValuePair> ();

					if (ownerToPower.ContainsKey("production"))
					{
						AddIfDifferent(attributes, datacenter, "production_it_power_kw", ownerToPower["production"]);
					}

					if (ownerToPower.ContainsKey("dev&test"))
					{
						AddIfDifferent(attributes, datacenter, "dev_it_power_kw", ownerToPower["dev&test"]);
					}

					double devNonItPower = 0;
					double productionNonItPower = 0;

					double kWperRack = 100 * 6 / (24.0 * 6);

					devNonItPower += (devRacksActive * kWperRack);
					productionNonItPower += (productionRacksActive * kWperRack);

					double nonItPower = devNonItPower + productionNonItPower;

					double devItPower = 0;
					if (ownerToPower.ContainsKey("dev&test"))
					{
						devItPower = ownerToPower["dev&test"];
					}

					double productionItPower = 0;
					if (ownerToPower.ContainsKey("production"))
					{
						productionItPower = ownerToPower["production"];
					}
					double itPower = devItPower + productionItPower;

					double totalPower = itPower + nonItPower;
					AddIfDifferent(attributes, datacenter, "total_power_kw", totalPower);
					AddIfDifferent(attributes, datacenter, "it_power_kw", itPower);
					AddIfDifferent(attributes, datacenter, "non_it_power_kw", nonItPower);
					AddIfDifferent(attributes, datacenter, "dev_non_it_power_kw", devNonItPower);
					AddIfDifferent(attributes, datacenter, "production_non_it_power_kw", productionNonItPower);

					double totalDevPower = devNonItPower + devItPower;
					AddIfDifferent(attributes, datacenter, "dev_power_kw", totalDevPower);

					double totalProductionPower = productionNonItPower + productionItPower;
					AddIfDifferent(attributes, datacenter, "production_power_kw", totalProductionPower);

					double dcie = 100.0 * itPower / totalPower;
					AddIfDifferent(attributes, datacenter, "dcie", dcie);

					double pue = totalPower / itPower;
					AddIfDifferent(attributes, datacenter, "pue", pue);

					if (attributes.Count > 0)
					{
						datacenter.SetAttributes(attributes);
					}
				}
			}
		}
	}
}