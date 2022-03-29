using System.Collections.Generic;
using System.Linq;
using System.Xml;

using GameManagement;
using LibCore;
using Logging;
using Network;

namespace DevOpsReportBuilders
{
	public class ProductQualityReportBuilder
	{
		readonly NetworkProgressionGameFile gameFile;
		Dictionary<string, List<Error>> serviceNameToErrors;
		Dictionary<string, List<int>> newServiceIdToEffectivenesses;
		List<string> orderedServiceNames;

		public ProductQualityReportBuilder(NetworkProgressionGameFile gameFile)
		{
			this.gameFile = gameFile;
		}

		public string BuildReport(int round)
		{
			var xml = BasicXmlDocument.Create();

			var root = xml.AppendNewChild("ProductQualityReport");

			var orderedCategories = new[] { "Planning", "Development", "Test", "Deployment", "Notes" };
			var categoryToSubcategories = new Dictionary<string, string[]>
			{
				{ "Planning", new [] { "Product Choice" } },
				{ "Development", new [] { "Environment - Dev Team 1", "Environment - Dev Team 2", "Integration" } },
				{ "Test", new [] { "" } },
				{ "Deployment", new [] { "Environment" } },
				{ "Notes", new [] { "" }  }
			};
			var subCategoryNameToDescription = new Dictionary<string, string>
			{
				{ "Development - Environment - Dev Team 1", "Team 1" },
				{ "Development - Environment - Dev Team 2", "Team 2" },
			};

			var categories = root.AppendNewChild("Categories");
			AddCategories(orderedCategories, categoryToSubcategories, categories, subCategoryNameToDescription);

			var logFile = gameFile.GetRoundFile(round, "NetworkIncidents.log", GameFile.GamePhase.OPERATIONS);
			var logReader = new BasicIncidentLogReader(logFile);
			logReader.WatchCreatedNodes("CostedEvents", logReader_CostedEventFound);
			serviceNameToErrors = new Dictionary<string, List<Error>>();
			newServiceIdToEffectivenesses = new Dictionary<string, List<int>> ();
			orderedServiceNames = new List<string>();
			logReader.Run();

			var model = gameFile.GetNetworkModel(round);

			foreach (Node service in model.GetNamedNode("BeginNewServicesInstall").GetChildrenOfType("BeginNewServicesInstall"))
			{
				var serviceName = service.GetAttribute("biz_service_function");
				if (!serviceNameToErrors.ContainsKey(serviceName))
				{
					serviceNameToErrors.Add(serviceName, new List<Error>());
				}
			}

			var services = root.AppendNewChild("Services");

			var beginServicesNodes = model.GetNamedNode("BeginNewServicesInstall")
				.GetChildrenWithAttributeValue("round", round.ToString()).ToList();

			var testImpactExtras = new Dictionary<string, string>
			{
				{ "test-extra-time", "time" },
				{ "test-extra-cost", "cost" }
			};

			foreach (var serviceName in orderedServiceNames)
			{
				// There could now be multiple nodes with the same "Begin {serviceName}" name but each 
				// have a 'unique' ID to differentiate them. Hopefully this workaround doesn't bork 
				// the report in the short term. 
				
				if (beginServicesNodes.Where(n => n.GetAttribute("name").Contains(serviceName))
					.Any(n => n.GetBooleanAttribute("is_auto_installed_at_end_of_round", false)))
				{
					continue;
				}

				if (! serviceNameToErrors.ContainsKey(serviceName))
				{
					continue;
				}

				var serviceNode = model.GetNamedNode(serviceName);
				if (serviceNode == null)
				{
					serviceNode = model.GetNamedNode("NS " + serviceName);
				}
				if (serviceNode == null)
				{
					serviceNode = model.GetNamedNode("New Services").GetChildWithAttributeValue("round", CONVERT.ToStr(round)).GetChildrenAsList().FirstOrDefault(n => n.GetAttribute("name").StartsWith("NS " + serviceName));
				}
				if (serviceNode == null)
				{
					serviceNode = beginServicesNodes.First(sn => sn.GetAttribute("biz_service_function") == serviceName);
				}

				var errors = serviceNameToErrors[serviceName];
				var newServiceIds = errors.Select(e => e.NewServiceUniqueId).Distinct().ToList();
				newServiceIds.Sort();

				foreach (var newServiceId in newServiceIds)
				{
					var service = services.AppendNewChild("Service");
					service.AppendAttribute("name", serviceName);
					service.AppendAttribute("desc", serviceName);
					service.AppendAttribute("shortdesc", serviceNode.GetAttribute("shortdesc"));

					AddStage(service, newServiceId, "Planning", "Product Choice", errors, "product-optimal", "product-suboptimal");
					AddStage(service, newServiceId, "Development", "Environment - Dev Team 1", errors, "dev1-optimal", "dev1-suboptimal");
					AddStage(service, newServiceId, "Development", "Environment - Dev Team 2", errors, "dev2-optimal", "dev2-suboptimal");
					AddStage(service, newServiceId, "Development", "Integration", errors, "dev-integration-succeeded", "dev-integration-failed");
					AddStage(service, newServiceId, "Notes", "", errors, "notes", "notes");

					if (newServiceIdToEffectivenesses.ContainsKey(newServiceId))
					{
						foreach (var effectiveness in newServiceIdToEffectivenesses[newServiceId])
						{
							var stage = service.AppendNewChild("ServiceStage");
							stage.AppendAttribute("stage", "Effectiveness");
							stage.AppendAttribute("details", effectiveness);
							stage.AppendAttribute("new_service_id", newServiceId);
						}
					}
					
					AddStage(service, newServiceId, "Test", "", errors, new [] { "test-done", "test-right-environment" }, new [] { "test-bypassed", "test-wrong-environment" }, testImpactExtras);
					AddStage(service, newServiceId, "Deployment", "Environment", errors, "deploy-optimal-enclosure", "deploy-suboptimal-enclosure");
				}
			}

			var filename = gameFile.GetRoundFile(round, "product_quality_report.xml", GameFile.GamePhase.OPERATIONS);
			xml.Save(filename);
			return filename;
		}

		void AddCategories (IList<string> orderedCategories, Dictionary<string, string[]> categoryToSubcategories, XmlElement node, Dictionary<string, string> subCategoryNameToDescription, string baseId = "")
		{
			foreach (var categoryName in orderedCategories)
			{
				var category = node.AppendNewChild("Category");
				category.AppendAttribute("id", baseId + categoryName);

				var description = categoryName;
				var fullName = baseId + categoryName;
				if (subCategoryNameToDescription.ContainsKey(fullName))
				{
					description = subCategoryNameToDescription[fullName];
				}
				category.AppendAttribute("desc", description);

				if (categoryToSubcategories.ContainsKey(fullName))
				{
					AddCategories(categoryToSubcategories[fullName], categoryToSubcategories, category, subCategoryNameToDescription, fullName + " - ");
				}
			}
		}

		void AddStage (XmlElement service, string newServiceId, string categoryId, string subCategoryId, IList<Error> errors, string successString, string failureString, IReadOnlyDictionary<string, string> extrasTypeToImpact = null)
		{
			AddStage(service, newServiceId, categoryId, subCategoryId, errors, new List<string> { successString }, new List<string> { failureString }, extrasTypeToImpact);
		}

		void AddStage (XmlElement service, string newServiceId, string categoryId, string subCategoryId, IList<Error> errors, IList<string> successStrings, IList<string> failureStrings, IReadOnlyDictionary<string, string> extrasTypeToImpact = null)
		{
			var prunedErrors = new List<Error> ();
			Error? errorToAdd = null;
			int? mostRecentSubStage = null;

			foreach (var error in errors.Where(e => e.NewServiceUniqueId == newServiceId))
			{
				int? thisSubStage = null;
				if (failureStrings.Contains(error.Type))
				{
					thisSubStage = failureStrings.IndexOf(error.Type);
				}
				else if (successStrings.Contains(error.Type))
				{
					thisSubStage = successStrings.IndexOf(error.Type);
				}

				if (thisSubStage.HasValue)
				{
					if ((mostRecentSubStage != null)
						&& (thisSubStage.Value <= mostRecentSubStage.Value))
					{
						prunedErrors.Add(errorToAdd.Value);
					}

					mostRecentSubStage = thisSubStage;
					errorToAdd = error;
				}
			}

			if (errorToAdd != null)
			{
				prunedErrors.Add(errorToAdd.Value);
			}

			foreach (var error in prunedErrors)
			{
				bool? ok = null;
				string details = null;
				bool isNeutral = false;

				if (failureStrings.Contains(error.Type)
					&& successStrings.Contains(error.Type))
				{
					ok = true;
					isNeutral = true;
					details = error.Details;
				}
				else if (failureStrings.Contains(error.Type))
				{
					ok = false;
					details = error.Details;
				}
				else if (successStrings.Contains(error.Type))
				{
					ok = true;
					details = error.Details;
				}

				if (ok.HasValue)
				{
					var stage = service.AppendNewChild("ServiceStage");
					stage.AppendAttribute("stage", categoryId + " - " + subCategoryId);

					if (! isNeutral)
					{
						stage.AppendAttribute("ok", ok.Value);
					}

					stage.AppendAttribute("details", details);
					stage.AppendAttribute("new_service_id", newServiceId);

					if (extrasTypeToImpact != null)
					{
						foreach (var errorExtra in prunedErrors.Where(e => ((e.Details == error.Details)
																			&& (e.NewServiceUniqueId == error.NewServiceUniqueId)
																			&& extrasTypeToImpact.ContainsKey(e.Type))))
						{
							var extra = extrasTypeToImpact[errorExtra.Type];

							var impact = stage.AppendNewChild("impact");
							impact.AppendAttribute("type", extra);
						}
					}
				}
			}
		}

		void logReader_CostedEventFound(object sender, string key, string line, double time)
		{
			var type = BasicIncidentLogReader.ExtractValue(line, "type");

			if (BasicIncidentLogReader.ExtractBoolValue(line, "is_prototype") ?? false)
			{
				return;
			}

			switch (type)
			{
				case "NS_error":
					{
						var serviceName = BasicIncidentLogReader.ExtractValue(line, "service_name");
						if (BasicIncidentLogReader.ExtractValue(line, "error_type") == "undo")
						{
							serviceNameToErrors.Remove(serviceName);
							orderedServiceNames.Remove(serviceName);
							return;
						}

						if (!orderedServiceNames.Contains(serviceName))
						{
							orderedServiceNames.Add(serviceName);
						}

						if (!serviceNameToErrors.ContainsKey(serviceName))
						{
							serviceNameToErrors.Add(serviceName, new List<Error>());
						}

						var newServiceId = BasicIncidentLogReader.ExtractValue(line, "unique_id");
						var productId = BasicIncidentLogReader.ExtractValue(line, "product_id");
						var errorType = BasicIncidentLogReader.ExtractValue(line, "error_full_type");
						var errorDetails = BasicIncidentLogReader.ExtractValue(line, "error_details");

						if (string.IsNullOrEmpty(errorDetails))
						{
							errorDetails = BasicIncidentLogReader.ExtractValue(line, "notes");
						}
						serviceNameToErrors[serviceName].Add(new Error { NewServiceUniqueId = newServiceId, ProductId = productId, Type = errorType, Details = errorDetails });

						var effectiveness = BasicIncidentLogReader.ExtractIntValue(line, "effectiveness_percent");
						if (effectiveness.HasValue)
						{
							if (line.Contains("deploy-final"))
							{
								if (! newServiceIdToEffectivenesses.ContainsKey(newServiceId))
								{
									newServiceIdToEffectivenesses.Add(newServiceId, new List<int> ());
								}

								newServiceIdToEffectivenesses[newServiceId].Add(effectiveness.Value);
							}
						}
						break;
					}

				case "NS_info":
					{
						var serviceName = BasicIncidentLogReader.ExtractValue(line, "service_name");
						if (BasicIncidentLogReader.ExtractValue(line, "error_type") == "undo")
						{
							serviceNameToErrors.Remove(serviceName);
							orderedServiceNames.Remove(serviceName);
							return;
						}

						if (!orderedServiceNames.Contains(serviceName))
						{
							orderedServiceNames.Add(serviceName);
						}
						break;
					}
			}
		}

		struct Error
		{
			public string NewServiceUniqueId { get; set; }
			public string ProductId { get; set; }
			public string Type { get; set; }
			public string Details { get; set; }
		}
	}
}