using System.Collections.Generic;
using System.Linq;
using System.Xml;
using GameManagement;
using LibCore;
using Logging;
using Network;

namespace DevOps.ReportsScreen
{
	public class ProductQualityReportBuilder
	{
	    readonly NetworkProgressionGameFile gameFile;
		Dictionary<string, List<Error>> serviceNameToErrors;
		Dictionary<string, int> serviceNameToEffectiveness;
		List<string> orderedServiceNames;

		public ProductQualityReportBuilder (NetworkProgressionGameFile gameFile)
		{
			this.gameFile = gameFile;
		}

		public string BuildReport (int round)
		{
			var xml = BasicXmlDocument.Create();

			var root = xml.AppendNewChild("ProductQualityReport");

			var orderedCategories = new [] { "Planning", "Development", "Test", "Deployment" };
			var categoryToSubcategories = new Dictionary<string, string []>
			{
				{ "Planning", new [] { "Product Choice" } },
				{ "Development", new [] { "Environment - Dev Team 1", "Environment - Dev Team 2", "Integration" } },
				{ "Test", new [] { "" } },
				{ "Deployment", new [] { "Environment" } }
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
			serviceNameToErrors = new Dictionary<string, List<Error>> ();
			serviceNameToEffectiveness = new Dictionary<string, int> ();
			orderedServiceNames = new List<string> ();
			logReader.Run();

			var model = gameFile.GetNetworkModel(round);

			foreach (Node service in model.GetNamedNode("BeginNewServicesInstall").GetChildrenOfType("BeginNewServicesInstall"))
			{
				var serviceName = service.GetAttribute("biz_service_function");
				if (! serviceNameToErrors.ContainsKey(serviceName))
				{
					serviceNameToErrors.Add(serviceName, new List<Error> ());
				}
			}

			var services = root.AppendNewChild("Services");
			foreach (var serviceName in orderedServiceNames)
			{
				var service = services.AppendNewChild("Service");
				service.AppendAttribute("name", serviceName);
				service.AppendAttribute("desc", serviceName);
				service.AppendAttribute("shortdesc", model.GetNamedNode("NS " + serviceName).GetAttribute("shortdesc"));

				int? effectiveness = null;
				var beginNode = model.GetNamedNode("Begin " + serviceName);
				if ((beginNode != null)
					&& (beginNode.GetAttribute("status") == "live")
					&& ! beginNode.GetBooleanAttribute("is_auto_installed_at_end_of_round", false))
				{
					if (serviceNameToEffectiveness.ContainsKey(serviceName))
					{
						effectiveness = serviceNameToEffectiveness[serviceName];
					}
					else
					{
						effectiveness = 100;
					}
				}
				if (effectiveness != null)
				{
					service.AppendAttribute("effectiveness", effectiveness.Value);
				}

				if (serviceNameToErrors.ContainsKey(serviceName))
				{
					var errors = serviceNameToErrors[serviceName];

					AddStage(service, "Planning", "Product Choice", errors, "product-optimal", "product-suboptimal");
					AddStage(service, "Development", "Environment - Dev Team 1", errors, "dev1-optimal", "dev1-suboptimal");
					AddStage(service, "Development", "Environment - Dev Team 2", errors, "dev2-optimal", "dev2-suboptimal");
					AddStage(service, "Development", "Integration", errors, "dev-integration-succeeded", "dev-integration-failed");

					var testImpacts = new List<string> ();
					if (serviceNameToErrors[serviceName].Any(error => error.Type == "test-extra-time"))
					{
						testImpacts.Add("time");
					}
					if (serviceNameToErrors[serviceName].Any(error => error.Type == "test-extra-cost"))
					{
						testImpacts.Add("cost");
					}

					AddStage(service, "Test", "", errors, new [] { "test-done", "test-right-environment" }, new [] { "test-bypassed", "test-wrong-environment"}, testImpacts);
					AddStage(service, "Deployment", "Environment", errors, "deploy-optimal-enclosure", "deploy-suboptimal-enclosure");
				}
			}

			var filename = gameFile.GetRoundFile(round, "product_quality_report.xml", GameFile.GamePhase.OPERATIONS);
			xml.Save(filename);
			return filename;
		}

		void AddCategories (IList<string> orderedCategories, Dictionary<string, string []> categoryToSubcategories, XmlElement node, Dictionary<string, string> subCategoryNameToDescription, string baseId = "")
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

		void AddStage (XmlElement service, string categoryId, string subCategoryId, IList<Error> errors, string successString, string failureString, IList<string> extras = null)
		{
			AddStage(service, categoryId, subCategoryId, errors, new List<string> { successString }, new List<string> { failureString }, extras);
		}

		void AddStage (XmlElement service, string categoryId, string subCategoryId, IList<Error> errors, IList<string> successStrings, IList<string> failureStrings, IList<string> extras = null)
		{
			var prunedErrors = new List<Error> ();
			Error? errorToAdd = null;
			int? mostRecentSubStage = null;

			foreach (var error in errors)
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

				if (failureStrings.Contains(error.Type))
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
					stage.AppendAttribute("ok", ok.Value);
					stage.AppendAttribute("details", details);

					if (extras != null)
					{
						foreach (var extra in extras)
						{
							var impact = stage.AppendNewChild("impact");
							impact.AppendAttribute("type", extra);
						}
					}
				}
			}
		}

		void logReader_CostedEventFound (object sender, string key, string line, double time)
		{
			var type = BasicIncidentLogReader.ExtractValue(line, "type");

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

					if (! orderedServiceNames.Contains(serviceName))
					{
						orderedServiceNames.Add(serviceName);
					}

					if (! serviceNameToErrors.ContainsKey(serviceName))
					{
						serviceNameToErrors.Add(serviceName, new List<Error> ());
					}

					var errorType = BasicIncidentLogReader.ExtractValue(line, "error_full_type");
					var errorDetails = BasicIncidentLogReader.ExtractValue(line, "error_details");
					serviceNameToErrors[serviceName].Add(new Error { Type = errorType, Details = errorDetails });

					var effectiveness = BasicIncidentLogReader.ExtractIntValue(line, "effectiveness_percent");
					if (effectiveness.HasValue)
					{
						serviceNameToEffectiveness[serviceName] = effectiveness.Value;
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

					if (! orderedServiceNames.Contains(serviceName))
					{
						orderedServiceNames.Add(serviceName);
					}
					break;
				}
			}
		}

		struct Error
		{
			public string Type;
			public string Details;
		}
	}
}