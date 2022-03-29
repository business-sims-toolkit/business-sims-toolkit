using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

using Network;

namespace BusinessServiceRules
{
	public class ServiceDownCounterWithImpactBasedSlas : ServiceDownCounter
	{
		Node slas;

		public ServiceDownCounterWithImpactBasedSlas (NodeTree model)
			: base (model)
		{
			slas = model.GetNamedNode("SLAs");
			foreach (Node sla in slas.GetChildrenOfType("sla"))
			{
				sla.AttributesChanged += sla_AttributesChanged;
			}

			UpdateBsuSlas();
		}

		public override void Dispose ()
		{
			foreach (Node sla in slas.GetChildrenOfType("sla"))
			{
				sla.AttributesChanged -= sla_AttributesChanged;
			}

			base.Dispose();
		}

		public List<Node> GetBsusAffectedByIncident (string incidentId)
		{
			return GetBsusAffectedByIncident(null, incidentId);
		}

		public List<Node> GetBsusAffectedByIncident (Node businessService, string incidentId)
		{
			List<Node> potentialBsus;
			if (businessService != null)
			{
				potentialBsus = GetBsusOwnedByBusinessService(businessService);
			}
			else
			{
				potentialBsus = new List<Node> ((Node[]) model.GetNodesWithAttributeValue("type", "biz_service_user").ToArray(typeof (Node)));
			}

			return potentialBsus.Where(bsu => bsu.GetAttribute("incident_id") == incidentId).ToList();
		}

		public int GetRevenueStreamsForBsu (Node bsu)
		{
			switch (bsu.GetAttribute("transaction_type"))
			{
				case "both":
					return 2;

				case "online":
				case "instore":
					return 1;

				case "none":
					return 0;

				default:
					Debug.Assert(false);
					return 0;
			}
		}

		public int GetRevenueStreamsForBsus (List<Node> bsus)
		{
			int revenueStreams = 0;
			foreach (Node bsu in bsus)
			{
				revenueStreams += GetRevenueStreamsForBsu(bsu);
			}

			return revenueStreams;
		}

		public Node GetSlaForRevenueStreams (int revenueStreams)
		{
			foreach (Node sla in slas.GetChildrenOfType("sla"))
			{
				if ((revenueStreams >= sla.GetIntAttribute("revenue_streams_min", 0))
					&& (revenueStreams <= sla.GetIntAttribute("revenue_streams_max", 0)))
				{
					return sla;
				}
			}

			Debug.Assert(false);
			return null;
		}

		public Node GetBusinessServiceOwningBsu (Node bsu)
		{
			foreach (LinkNode link in bsu.BackLinks)
			{
				if (link.From.GetAttribute("type") == "biz_service")
				{
					return link.From;
				}
			}

			Debug.Assert(false);
			return null;
		}

		public List<Node> GetBsusOwnedByBusinessService (Node businessService)
		{
			List<Node> bsus = new List<Node> ();

			foreach (LinkNode link in businessService.GetChildrenOfType("Connection"))
			{
				if (link.To.GetAttribute("type") == "biz_service_user")
				{
					bsus.Add(link.To);
				}
			}

			return bsus;
		}

		protected override void IncrementAtt (Node node, string attributeName)
		{
			base.IncrementAtt(node, attributeName);

			// At the point we go down, we need to set our SLA limit
			// based on how many other revenue streams are affected by this incident.
			if (node.GetAttribute("type") == "biz_service_user")
			{
				if ((attributeName == "downforsecs")
					&& (node.GetIntAttribute(attributeName, 0) == 1))
				{
					UpdateBsuSla(node);
				}
			}
		}

		void UpdateBsuSla (Node bsu)
		{
			string incidentId = bsu.GetAttribute("incident_id");
			if (! string.IsNullOrEmpty(incidentId))
			{
				Node businessService = GetBusinessServiceOwningBsu(bsu);
				List<Node> affectedBsus = GetBsusAffectedByIncident(businessService, incidentId);
				int revenueStreamsAffected = GetRevenueStreamsForBsus(affectedBsus);
				Node sla = GetSlaForRevenueStreams(revenueStreamsAffected);

				int slaLimit = sla.GetIntAttribute("slalimit", 0);
				if (bsu.GetIntAttribute("slalimit", 0) != slaLimit)
				{
					bsu.SetAttribute("slalimit", slaLimit);
				}
			}
		}

		void UpdateBsuSlas ()
		{
			foreach (Node bsu in model.GetNodesWithAttributeValue("type", "biz_service_user"))
			{
				UpdateBsuSla(bsu);
			}
		}

		void sla_AttributesChanged (Node sender, System.Collections.ArrayList attrs)
		{
			UpdateBsuSlas();
		}
	}
}