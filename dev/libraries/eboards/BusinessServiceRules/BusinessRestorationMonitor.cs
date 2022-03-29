using System;

using System.Collections.Generic;
using System.Collections;
using Network;

namespace BusinessServiceRules
{
	public class BusinessRestorationMonitor : IDisposable
	{
		NodeTree model;
		Node business;
		List<Node> bsus;

		public BusinessRestorationMonitor (NodeTree model, Node business)
		{
			this.model = model;

			this.business = business;
			business.ChildAdded += business_ChildAdded;
			business.ChildRemoved += business_ChildRemoved;

			bsus = new List<Node> ((Node []) business.GetChildrenOfType("biz_service_user").ToArray(typeof (Node)));
			foreach (Node bsu in bsus)
			{
				bsu.AttributesChanged += bsu_AttributesChanged;
			}

			CalculateRestoration();
		}

		public void Dispose ()
		{
			business.ChildAdded -= business_ChildAdded;
			business.ChildRemoved -= business_ChildRemoved;

			foreach (Node bsu in bsus)
			{
				bsu.AttributesChanged -= bsu_AttributesChanged;
			}
		}

		void business_ChildAdded (Node sender, Node child)
		{
			bsus.Add(child);
			child.AttributesChanged += bsu_AttributesChanged;
		}

		void business_ChildRemoved (Node sender, Node child)
		{
			bsus.Remove(child);
			child.AttributesChanged -= bsu_AttributesChanged;
		}

		void bsu_AttributesChanged (Node sender, ArrayList attrs)
		{
			CalculateRestoration();
		}

		void CalculateRestoration ()
		{
			bool calculateByImpact = true;

			int up = 0;
			int total = 0;

			int impact = 0;
			int totalImpact = 0;

			foreach (Node bsu in bsus)
			{
				if (bsu.GetBooleanAttribute("has_impact", false))
				{
					total++;

					int bsuImpact = bsu.GetIntAttribute("rev_impact_full", 0);
					totalImpact += bsuImpact;

					Node serviceNode = null;
					foreach (LinkNode link in bsu.BackLinks)
					{
						if (link.From.GetAttribute("type") == "biz_service")
						{
							serviceNode = link.From;
							break;
						}
					}

					Node switchNode = null;

					if (serviceNode != null)
					{
						switchNode = model.GetNamedNode(serviceNode.GetAttribute("name") + " Switch");
					}

					if ((switchNode != null) && switchNode.GetBooleanAttribute("up", false))
					{
						up++;

						int rev_impact = 0;

						string status = bsu.GetAttribute("team_status");
						switch (status.ToLower())
						{
							case "coreonly":
								rev_impact = bsu.GetIntAttribute("rev_impact_core", 0);
								break;

							case "full":
							default:
								rev_impact = bsu.GetIntAttribute("rev_impact_full", 0);
								break;
						}

						impact += rev_impact;
					}
				}
			}

			int servicesUp = up;

			if (calculateByImpact)
			{
				total = totalImpact;
				up = impact;
			}

			int percent = (int) (up * 100.0 / Math.Max(1, total));

			ArrayList attributes = new ArrayList ();
			AttributeValuePair.AddIfNotEqual(business, attributes, "percent_up", percent);
			AttributeValuePair.AddIfNotEqual(business, attributes, "services_up", servicesUp);
			business.SetAttributes(attributes);
		}
	}
}