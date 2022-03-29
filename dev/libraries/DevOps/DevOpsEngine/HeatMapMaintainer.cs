using System;
using System.Collections.Generic;
using System.Linq;
using Network;

namespace DevOpsEngine
{
	public class HeatMapMaintainer : IDisposable
	{
		NodeTree model;
		Node businessServiceGroup;

		public HeatMapMaintainer (NodeTree model)
		{
			this.model = model;

			businessServiceGroup = model.GetNamedNode("Business Services Group");
			businessServiceGroup.ChildAdded += businessServiceGroup_ChildAdded;
			businessServiceGroup.ChildRemoved += businessServiceGroup_ChildRemoved;
			foreach (var businessService in businessServiceGroup.GetChildrenAsList())
			{
				WatchBusinessService(businessService);
			}
		}

		public void Dispose ()
		{
			businessServiceGroup.ChildAdded -= businessServiceGroup_ChildAdded;
			businessServiceGroup.ChildRemoved -= businessServiceGroup_ChildRemoved;
			foreach (var businessService in businessServiceGroup.GetChildrenAsList())
			{
				UnWatchBusinessService(businessService);
			}
		}

		void businessServiceGroup_ChildAdded (Node sender, Node child)
		{
			WatchBusinessService(child);
		}

		void businessServiceGroup_ChildRemoved (Node sender, Node child)
		{
			UnWatchBusinessService(child);
		}

		void WatchBusinessService (Node businessService)
		{
			UpdateHeatMapForService(businessService);

			businessService.ChildAdded += businessService_ChildAdded;
			businessService.ChildRemoved += businessService_ChildRemoved;
		}

		void UnWatchBusinessService (Node businessService)
		{
			businessService.ChildAdded -= businessService_ChildAdded;
			businessService.ChildRemoved -= businessService_ChildRemoved;
		}

		void businessService_ChildAdded (Node sender, Node child)
		{
			UpdateHeatMapForService(sender);
		}

		void businessService_ChildRemoved (Node sender, Node child)
		{
			UpdateHeatMapForService(sender);
		}

		void UpdateHeatMapForService (Node service)
		{
			var serviceName = service.GetAttribute("name");
			var initialHeatMap = model.GetNamedNode($"{serviceName}.HeatMap.Initial");

			var currentStateNodeName = $"{serviceName}.HeatMap.Current";
			var currentState = model.GetNamedNode(currentStateNodeName);
			if (currentState == null)
			{
				currentState = new Node(service, "heat_map_state", currentStateNodeName,
					new AttributeValuePair("type", "heat_map_state"));

				foreach (var customerComplaint in AgileComplaints.CustomerComplaintTypes)
				{
					foreach (var customerType in AgileComplaints.CustomerTypes)
					{
						new Node(currentState, "customer_complaint", "",
							new List<AttributeValuePair>
							{
								new AttributeValuePair("complaint", customerComplaint),
								new AttributeValuePair("customer_type", customerType)
							});
					}
				}
			}

			foreach (var customerComplaint in AgileComplaints.CustomerComplaintTypes)
			{
				foreach (var customerType in AgileComplaints.CustomerTypes)
				{
					var attributeValueFilter = new List<AttributeValuePair>
					{
						new AttributeValuePair("complaint", customerComplaint),
						new AttributeValuePair("customer_type", customerType)
					};

					var initialEntry = initialHeatMap.GetChildWithAttributeValues(attributeValueFilter);

					var isOk = initialEntry.GetBooleanAttribute("is_ok", false);
					var isBestInClass = initialEntry.GetBooleanAttribute("is_best_in_class", false);
					var lastChangedBy = initialEntry.GetAttribute("last_changed_by");
					var lastFalsePromisedChangeBy = initialEntry.GetAttribute("last_false_promised_change_by");

					foreach (var modification in service.GetChildrenOfTypeAsList("heat_map_modification"))
					{
						var featureId = modification.GetAttribute("feature_id");

						var modificationEntry = modification.GetChildWithAttributeValues(attributeValueFilter);

						var isOkModification = modificationEntry?.GetBooleanAttribute("is_ok");
						var isBestInClassModification = modificationEntry?.GetBooleanAttribute("is_best_in_class");
						var falsePromisesFix = modificationEntry?.GetBooleanAttribute("false_promises_fix");

						if (isOkModification != null)
						{
							isOk = isOkModification.Value;
							lastChangedBy = featureId;
						}

						if (isBestInClassModification != null)
						{
							isBestInClass = isBestInClassModification.Value;
							lastChangedBy = featureId;
						}

						if (falsePromisesFix == true)
						{
							lastFalsePromisedChangeBy = featureId;
						}
					}

					var entry = currentState.GetChildWithAttributeValues(attributeValueFilter);
					entry.SetAttributeIfNotEqual("is_ok", isOk);
					entry.SetAttributeIfNotEqual("is_best_in_class", isBestInClass);
					entry.SetAttributeIfNotEqual("last_changed_by", lastChangedBy);
					entry.SetAttributeIfNotEqual("last_false_promised_change_by", lastFalsePromisedChangeBy);
				}
			}

			RemoveFixedHeatMapIncidents(service, currentState);
		}

		void RemoveFixedHeatMapIncidents (Node service, Node currentState)
		{
			var incidentTag = "Incident";

			var incidentModifications = service.GetChildrenOfTypeAsList("heat_map_modification")
				.Where(s => s.GetAttribute("feature_id").StartsWith(incidentTag));
			foreach (var incidentModification in incidentModifications)
			{
				// Are all the circles that the incident broke now green?
				if (incidentModification.GetChildrenAsList().All(
					impact => currentState
						.GetChildWithAttributeValues(new AttributeValuePair("customer_type", impact.GetAttribute("customer_type")),
							new AttributeValuePair("complaint", impact.GetAttribute("complaint")))
						.GetBooleanAttribute("is_ok", false)))
				{
					var incidentId = incidentModification.GetAttribute("feature_id").Substring(incidentTag.Length);
					var fixItQueue = model.GetNamedNode("FixItQueue");
					new Node(fixItQueue, "fix", "", new AttributeValuePair("incident_id", incidentId));
				}
			}
		}

		enum ComplaintEffect
		{
			BestInClass,
			Fixed,
			Broken,
			FalsePredictedFix
		}

		public void AddFeatureToService (Node service, Node addedFeature)
		{
			var serviceName = service.GetAttribute("name");
			var featureId = addedFeature.GetAttribute("service_id");

			var oldModification = model.GetNamedNode($"{serviceName}.HeatMap.{featureId}");
			if (oldModification != null)
			{
				oldModification.Parent.DeleteChildTree(oldModification);
			}

			var modification = new Node(model.Root, "heat_map_modification", $"{serviceName}.HeatMap.{featureId}",
				new List<AttributeValuePair>
				{
					new AttributeValuePair("type", "heat_map_modification"),
					new AttributeValuePair("feature_id", featureId)
				});

			foreach (var complaintType in AgileComplaints.CustomerComplaintTypes)
			{
				foreach (var customerType in AgileComplaints.CustomerTypes)
				{
					ComplaintEffect? effect = null;
					if (addedFeature.GetBooleanAttribute($"customer_complaint_{complaintType}{customerType}_best", false))
					{
						effect = ComplaintEffect.BestInClass;
					}
					else if (addedFeature.GetBooleanAttribute($"customer_complaint_{complaintType}{customerType}_fixed", false))
					{
						effect = ComplaintEffect.Fixed;
					}
					else if (addedFeature.GetBooleanAttribute($"customer_complaint_{complaintType}{customerType}_broken", false))
					{
						effect = ComplaintEffect.Broken;
					}
					else if (addedFeature.GetBooleanAttribute($"customer_complaint_{complaintType}{customerType}_false_predicted_fix",
						false))
					{
						effect = ComplaintEffect.FalsePredictedFix;
					}

					if (effect.HasValue)
					{
						var attributes = new List<AttributeValuePair>
						{
							new AttributeValuePair("complaint", complaintType),
							new AttributeValuePair("customer_type", customerType),
						};

						switch (effect.Value)
						{
							case ComplaintEffect.Fixed:
								attributes.Add(new AttributeValuePair("is_ok", true));
								break;

							case ComplaintEffect.BestInClass:
								attributes.Add(new AttributeValuePair("is_best_in_class", true));
								break;

							case ComplaintEffect.FalsePredictedFix:
								attributes.Add(new AttributeValuePair("false_promises_fix", true));
								break;

							case ComplaintEffect.Broken:
								attributes.Add(new AttributeValuePair("is_ok", false));
								break;
						}

						new Node(modification, "", "", attributes);
					}
				}
			}

			model.MoveNode(modification, service);
		}

		public void RemoveFeatureFromService (Node service, string featureId)
		{
			var serviceName = service.GetAttribute("name");
			var modification = model.GetNamedNode($"{serviceName}.HeatMap.{featureId}");
			if (modification != null)
			{
				modification.Parent.DeleteChildTree(modification);
			}
		}

		public bool AreFeaturePrerequisitesMet (Node beginNode)
		{
			var serviceId = beginNode.GetAttribute("service_id");
			var productId = beginNode.GetAttribute("product_id");

			var round = model.GetNamedNode("CurrentRound").GetIntAttribute("round", 0);
			var newServicesForRound = model.GetNamedNode($"New Services Round {round}");
			var serviceNode = newServicesForRound.GetChildWithAttributeValue("service_id", serviceId);
			var productNode = serviceNode.GetChildWithAttributeValue("product_id", productId);

			var serviceName = serviceNode.GetAttribute("biz_service_function");
			var currentStateNodeName = $"{serviceName}.HeatMap.Current";
			var currentState = model.GetNamedNode(currentStateNodeName);

			foreach (var modification in productNode.GetFirstChildOfType("customer_complaint_adjustments").GetChildrenAsList())
			{
				var isOk = currentState.GetChildrenAsList().All(n => n.GetBooleanAttribute("is_ok", false));

				if (modification.GetBooleanAttribute("is_best_in_class", false)
				    && ! isOk)
				{
					return false;
				}
			}

			return true;
		}
	}
}
