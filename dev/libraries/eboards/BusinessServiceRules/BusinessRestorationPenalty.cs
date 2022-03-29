using System;
using System.Collections.Generic;
using System.Collections;
using Network;

using LibCore;

namespace BusinessServiceRules
{
	public class BusinessRestorationPenalty : IDisposable
	{
		struct RestorationTarget
		{
			public Node Business;
			public Node Service;
			public int Time;
			public int Penalty;

			public RestorationTarget (Node targetNode)
			{
				Business = targetNode.Tree.GetNamedNode(targetNode.GetAttribute("business"));
				Service = targetNode.Tree.GetNamedNode(targetNode.GetAttribute("service"));
				Penalty = targetNode.GetIntAttribute("penalty", 0);

				Time = 0;
				Time += (60 * 60 * targetNode.GetIntAttribute("hours", 0));
				Time += (60 * targetNode.GetIntAttribute("minutes", 0));
				Time += (targetNode.GetIntAttribute("seconds", 0));
			}
		}

		NodeTree model;

		Node targets;
		Dictionary<Node, Dictionary<Node, List<RestorationTarget>>> businessToServiceToTargets;
		List<Node> targetNodes;

		Node timeSinceIncident;

		public BusinessRestorationPenalty (NodeTree model, Node businesses)
		{
			this.model = model;

			targetNodes = new List<Node> ();
			businessToServiceToTargets = new Dictionary<Node,Dictionary<Node,List<RestorationTarget>>> ();

			targets = model.GetNamedNode("RestorationTargets");
			foreach (Node targetNode in targets.getChildren())
			{
				AddTarget(targetNode);
			}
			targets.ChildAdded += targets_ChildAdded;
			targets.ChildRemoved += targets_ChildRemoved;

			timeSinceIncident = model.GetNamedNode("TimeSinceIncident");
			timeSinceIncident.PreAttributesChanged += timeSinceIncident_PreAttributesChanged;
		}

		public void Dispose ()
		{
			targets.ChildAdded -= targets_ChildAdded;
			targets.ChildRemoved -= targets_ChildRemoved;

			foreach (Node target in new List<Node> (targetNodes))
			{
				RemoveTarget(target);
			}

			timeSinceIncident.PreAttributesChanged -= timeSinceIncident_PreAttributesChanged;
		}

		void SortTargetList (List<RestorationTarget> list)
		{
			list.Sort(delegate (RestorationTarget a, RestorationTarget b) { return a.Time.CompareTo(b.Time); });
		}

		void AddTarget (Node targetNode)
		{
			RestorationTarget target = new RestorationTarget (targetNode);
			Node business = target.Business;
			Node service = target.Service;

			if (! targetNodes.Contains(targetNode))
			{
				targetNodes.Add(targetNode);
			}

			if (! businessToServiceToTargets.ContainsKey(business))
			{
				businessToServiceToTargets.Add(business, new Dictionary<Node, List<RestorationTarget>> ());
			}

			if (! businessToServiceToTargets[business].ContainsKey(service))
			{
				businessToServiceToTargets[business].Add(service, new List<RestorationTarget> ());
			}

			businessToServiceToTargets[business][service].Add(target);

			targetNode.AttributesChanged += targetNode_AttributesChanged;
		}

		void RemoveTarget (Node targetNode)
		{
			RestorationTarget target = new RestorationTarget (targetNode);
			Node business = target.Business;
			Node service = target.Service;

			if (businessToServiceToTargets.ContainsKey(business))
			{
				if (businessToServiceToTargets[business].ContainsKey(service))
				{
					businessToServiceToTargets[business][service].Remove(target);
				}

				if (businessToServiceToTargets[business][service].Count == 0)
				{
					businessToServiceToTargets[business].Remove(service);

					if (businessToServiceToTargets[business].Count == 0)
					{
						businessToServiceToTargets.Remove(business);
					}
				}
				else
				{
					SortTargetList(businessToServiceToTargets[business][service]);
				}
			}

			targetNodes.Remove(targetNode);

			targetNode.AttributesChanged -= targetNode_AttributesChanged;
		}

		void targets_ChildRemoved (Node sender, Node child)
		{
			RemoveTarget(child);
		}

		void targets_ChildAdded (Node sender, Node child)
		{
			AddTarget(child);
		}

		void targetNode_AttributesChanged (Node sender, ArrayList attrs)
		{
			RestorationTarget target = new RestorationTarget (sender);
			SortTargetList(businessToServiceToTargets[target.Business][target.Service]);
		}

		void timeSinceIncident_PreAttributesChanged (Node sender, ref ArrayList attrs)
		{
			int oldTime = timeSinceIncident.GetIntAttribute("time", 0);

			foreach (AttributeValuePair avp in attrs)
			{
				if (avp.Attribute == "time")
				{
					int newTime = CONVERT.ParseIntSafe(avp.Value, 0);

					foreach (Node business in businessToServiceToTargets.Keys)
					{
						foreach (Node service in businessToServiceToTargets[business].Keys)
						{
							// Find the most recent target we should have hit.
							RestorationTarget? mostRecentTarget = GetMostRecentTarget(business, service, newTime);

							if (mostRecentTarget != null)
							{
								// Only fine us as we pass the nominated time, not every tick thereafter.
								if (mostRecentTarget.Value.Time > oldTime)
								{
									Node businessServiceSwitch = model.GetNamedNode(service.GetAttribute("name") + " Switch");

									if (! (businessServiceSwitch.GetBooleanAttribute("up", false) && (service.GetAttribute("team_status").ToLower() == "full")))
									{
										business.SetAttribute("fines", mostRecentTarget.Value.Penalty + business.GetIntAttribute("fines", 0));

										Node fines = model.GetNamedNode("RecoveryFines");
										fines.SetAttribute("fines", mostRecentTarget.Value.Penalty + fines.GetIntAttribute("fines", 0));
									}
								}
							}
						}
					}
				}
			}
		}

		RestorationTarget? GetMostRecentTarget (Node business, Node service, int time)
		{
			RestorationTarget? mostRecentTarget = null;

			if (businessToServiceToTargets.ContainsKey(business))
			{
				if (businessToServiceToTargets[business].ContainsKey(service))
				{
					foreach (RestorationTarget target in businessToServiceToTargets[business][service])
					{
						if (target.Time <= time)
						{
							mostRecentTarget = target;
						}
						else
						{
							break;
						}
					}
				}
			}

			return mostRecentTarget;
		}

		RestorationTarget? GetNextTarget (Node business, Node service, int time)
		{
			RestorationTarget? nextTarget = null;

			if (businessToServiceToTargets.ContainsKey(business))
			{
				if (businessToServiceToTargets[business].ContainsKey(service))
				{
					foreach (RestorationTarget target in businessToServiceToTargets[business][service])
					{
						nextTarget = target;

						if (target.Time > time)
						{
							break;
						}
					}
				}
			}

			return nextTarget;
		}
	}
}