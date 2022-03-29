using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DevOpsEngine.StringConstants;
using Network;

namespace DevOpsEngine
{
	public class MilestoneTracker
	{
		readonly int round;
		public MilestoneTracker (NodeTree model, int round)
		{
			this.round = round;

			var milestonesNode = model.GetNamedNode("Milestones");
			milestones = milestonesNode.GetChildrenAsList().Select(n => new Milestone(n)).ToList();

			budgetNode = model.GetNamedNode("Budgets").GetChildWithAttributeValue("round", round.ToString());
			featureMilestonesNode = model.GetNamedNode("FeatureMilestones");

			beginServicesNode = model.GetNamedNode("BeginNewServicesInstall");
			beginServicesNode.ChildAdded += beginServicesNode_ChildAdded;
			beginServicesNode.ChildRemoved += beginServicesNode_ChildRemoved;

			trackedFeatureNodes = new List<Node>();
			featureIdToMilestonesAchieved = new Dictionary<string, List<Node>>();
			featureIdToMilestone = new Dictionary<string, Node>();
		}

		public void Dispose ()
		{
			beginServicesNode.ChildAdded -= beginServicesNode_ChildAdded;
			beginServicesNode.ChildRemoved += beginServicesNode_ChildRemoved;

			foreach (var feature in trackedFeatureNodes)
			{
				feature.AttributesChanged -= feature_AttributesChanged;
			}
		}

		void CheckProgressionForFeature (Node feature)
		{
			var featureId = feature.GetAttribute("service_id");

			if (!featureIdToMilestone.TryGetValue(featureId, out var featureMilestone))
			{
				featureMilestone = featureMilestonesNode.CreateChild("FeatureMilestone", "", new List<AttributeValuePair>
				{
					new AttributeValuePair("feature_id", featureId),
					new AttributeValuePair("round", round)
				});
				featureIdToMilestone[featureId] = featureMilestone;
			}

			var investmentSpent = CalculateInvestmentSpent(featureMilestone);

			var achievedMilestones = featureMilestone.GetChildrenAsList();

			foreach (var milestone in milestones.OrderBy(m => m.Number))
			{
				if (milestone.HasFeaturePassedMilestone(feature) && 
				    !achievedMilestones.Any(am => am.GetIntAttribute("number") == milestone.Number && am.GetAttribute("feature_name") == feature.GetAttribute("name")))
				{
					var milestoneCost = (int)Math.Floor(milestone.InvestmentPercentage * feature.GetIntAttribute("business_investment", 0));

					var costDelta = Math.Max(0, milestoneCost - investmentSpent);

					if (costDelta > 0)
					{
						AdjustBudget(-costDelta);
					}

					featureMilestone.CreateChild("MilestoneAchieved", "", new List<AttributeValuePair>
					{
						new AttributeValuePair("feature_name", feature.GetAttribute("name")),
						new AttributeValuePair("investment_spent", costDelta),
						new AttributeValuePair("number", milestone.Number)
					});
				}
			}

		}

		void AdjustBudget (int amount)
		{
			budgetNode.IncrementIntAttribute("budget", amount, 0);
		}

		void beginServicesNode_ChildAdded(Node sender, Node child)
		{
			TrackFeatureNode(child);

			CheckProgressionForFeature(child);
		}

		void TrackFeatureNode (Node featureNode)
		{
			featureNode.AttributesChanged += feature_AttributesChanged;

			trackedFeatureNodes.Add(featureNode);
		}

		void beginServicesNode_ChildRemoved(Node sender, Node child)
		{
			StopTrackingFeatureNode(child);
		}

		void StopTrackingFeatureNode (Node featureNode)
		{
			featureNode.AttributesChanged -= feature_AttributesChanged;
			trackedFeatureNodes.Remove(featureNode);
		}

		void feature_AttributesChanged(Node sender, ArrayList attrs)
		{
			foreach (AttributeValuePair avp in attrs)
			{
				if (avp.Attribute == "status")
				{
					if (avp.Value == FeatureStatus.Undo)
					{
						RemoveReferencesToFeature(sender, true);
					}
					else
					{
						CheckProgressionForFeature(sender);
					}
				}
			}
		}

		void RemoveReferencesToFeature (Node featureNode, bool recoupInvestment)
		{
			var featureId = featureNode.GetAttribute("service_id");
			
			if (featureIdToMilestone.TryGetValue(featureId, out var featureMilestone))
			{
				if (recoupInvestment)
				{
					var featureProductMilestones = featureMilestone.GetChildrenWithAttributeValue("feature_name", featureNode.GetAttribute("name"));
					foreach (var achievement in featureProductMilestones)
					{
						AdjustBudget(achievement.GetIntAttribute("investment_spent", 0));
					}
				}

				featureMilestone.DeleteChildrenWhere(n => n.GetAttribute("feature_name") == featureNode.GetAttribute("name"));

				if (! featureMilestone.GetChildrenAsList().Any())
				{
					featureMilestone.Parent.DeleteChildTree(featureMilestone);
					featureIdToMilestone.Remove(featureId);
				}
			}
		}

		static int CalculateInvestmentSpent (Node featureMilestone)
		{
			return featureMilestone.GetChildrenAsList().Sum(n => n.GetIntAttribute("investment_spent", 0));
		}

		readonly Node beginServicesNode;
		readonly Node budgetNode;
		readonly Node featureMilestonesNode;
		readonly List<Milestone> milestones;
		readonly List<Node> trackedFeatureNodes;
		readonly Dictionary<string, List<Node>> featureIdToMilestonesAchieved;
		readonly Dictionary<string, Node> featureIdToMilestone;

		class Milestone
		{
			public Milestone (Node milestoneNode)
			{
				Number = milestoneNode.GetIntAttribute("number", 0);
				InvestmentPercentage = milestoneNode.GetIntAttribute("investment_percentage", 0) / 100f;

				targetStatus = milestoneNode.GetAttribute("target_status");
				requiresPrototype = milestoneNode.GetBooleanAttribute("is_prototype", false);
			}

			public bool HasFeaturePassedMilestone (Node feature)
			{
				var status = feature.GetAttribute("status");
				var isPrototype = feature.GetBooleanAttribute("is_prototype", false);
				
				return status == targetStatus && isPrototype == requiresPrototype;
			}

			public int Number { get; }
			public float InvestmentPercentage { get; }

			readonly string targetStatus;
			readonly bool requiresPrototype;

		}

		

	}
}
