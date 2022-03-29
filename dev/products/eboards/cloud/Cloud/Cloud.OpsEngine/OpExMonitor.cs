using System;
using System.Collections.Generic;
using System.Text;

using LibCore;
using CoreUtils;
using Network;

namespace Cloud.OpsEngine
{
	public class OpExMonitor : IDisposable
	{
		NodeTree model;
		Node time;
		Node turnoverNode;
		Node roundVariablesNode;

		Biller biller;

		public OpExMonitor (NodeTree model, Biller biller)
		{
			this.model = model;
			this.biller = biller;

			time = model.GetNamedNode("CurrentTime");
			time.AttributesChanged += new Node.AttributesChangedEventHandler (time_AttributesChanged);

			turnoverNode = model.GetNamedNode("Turnover");
			roundVariablesNode = model.GetNamedNode("RoundVariables");
		}

		public void Dispose ()
		{
			time.AttributesChanged -= new Node.AttributesChangedEventHandler (time_AttributesChanged);
		}

		void time_AttributesChanged (Node sender, System.Collections.ArrayList attrs)
		{
			int currentTime = time.GetIntAttribute("seconds", 0);
			if (currentTime > 0)
			{
				int roundLength = time.GetIntAttribute("round_duration", 0);

				List<AttributeValuePair> attributes = new List<AttributeValuePair> ();

				// Run through the CMDB charging for all the CIs.
				foreach (Node business in model.GetNodesWithAttributeValue("type", "business"))
				{
					Node datacenter = model.GetNamedNode(business.GetAttribute("datacenter"));

					// Storage arrays.
					foreach (Node storage in datacenter.GetChildrenOfType("storage_array"))
					{
						if (! storage.GetBooleanAttribute("opex_charged", false))
						{
							if (PowerMonitor.IsStorageArrayInUse(storage))
							{
								string owner = "production";
								if (storage.GetAttribute("owner") == "dev&test")
								{
									owner = "dev&test";
								}

								attributes.Clear();
								attributes.Add(new AttributeValuePair ("type", "bill"));
								attributes.Add(new AttributeValuePair ("bill_type", "opex"));
								attributes.Add(new AttributeValuePair ("opex_type", "storage"));
								attributes.Add(new AttributeValuePair ("owner", owner));
								attributes.Add(new AttributeValuePair ("datacenter", datacenter.GetAttribute("name")));
								attributes.Add(new AttributeValuePair ("business", business.GetAttribute("name")));
								attributes.Add(new AttributeValuePair ("value", - storage.GetDoubleAttribute("opex", 0)));
								biller.CreateOrUpdateTurnoverItem(attributes);

								storage.SetAttribute("opex_charged", true);
							}
						}
					}

					// Racks.
					foreach (Node rack in datacenter.GetChildrenOfType("rack"))
					{
						string owner = "production";
						if (rack.GetAttribute("owner") == "dev&test")
						{
							owner = "dev&test";
						}

						if (rack.GetChildrenOfType("server").Count > 0)
						{
							if (! rack.GetBooleanAttribute("opex_charged", false))
							{
								attributes.Clear();
								attributes.Add(new AttributeValuePair ("type", "bill"));
								attributes.Add(new AttributeValuePair ("bill_type", "opex"));
								attributes.Add(new AttributeValuePair ("opex_type", "rack"));
								attributes.Add(new AttributeValuePair ("owner", owner));
								attributes.Add(new AttributeValuePair ("datacenter", datacenter.GetAttribute("name")));
								attributes.Add(new AttributeValuePair ("business", business.GetAttribute("name")));
								attributes.Add(new AttributeValuePair ("value", -rack.GetDoubleAttribute("opex", 0)));
								biller.CreateOrUpdateTurnoverItem(attributes);

								rack.SetAttribute("opex_charged", true);
							}

							// Servers.
							foreach (Node server in rack.GetChildrenOfType("server"))
							{
								if (! server.GetBooleanAttribute("opex_charged", false))
								{
									attributes.Clear();
									attributes.Add(new AttributeValuePair ("type", "bill"));
									attributes.Add(new AttributeValuePair ("bill_type", "opex"));
									attributes.Add(new AttributeValuePair ("opex_type", "server"));
									attributes.Add(new AttributeValuePair ("owner", owner));
									attributes.Add(new AttributeValuePair ("datacenter", datacenter.GetAttribute("name")));
									attributes.Add(new AttributeValuePair ("business", business.GetAttribute("name")));
									attributes.Add(new AttributeValuePair ("value", - server.GetDoubleAttribute("opex", 0)));
									biller.CreateOrUpdateTurnoverItem(attributes);

									server.SetAttribute("opex_charged", true);
								}
							}
						}
					}
				}
			}
		}
	}
}