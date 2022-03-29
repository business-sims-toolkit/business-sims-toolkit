using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using CoreUtils;
using DevOpsEngine.Interfaces;
using DevOpsEngine.ModelProperties;
using DevOpsEngine.StringConstants;
using LibCore;
using Network;

namespace DevOpsEngine.RequestsManagers
{
	public class FeatureAvailability
	{
		public Node FeatureNode { get; set; }
		public bool IsAvailable { get; set; }
		public string FeatureId { get; set; }
	}

	public class FeatureProduct
	{
		public Node ProductNode { get; set; }
		public string ProductId { get; set; }
		public string Platform { get; set; }
		public bool IsOptimal { get; set; }
		public bool IsEnabled { get; set; }
	}

	public class AgileRequestsManager : IRequestsManager
	{
		readonly NodeTree model;
		readonly Node newServicesRound;
		readonly Node beginBizServicesHead;
		readonly Node timeNode;
		readonly HeatMapMaintainer heatMapMaintainer;

		readonly Node startServicesNode;

		readonly Node beginServicesCommandQueueNode;

		readonly int round;

		readonly UniqueServiceIdGenerator serviceIdGenerator;

		public int MaxFeaturesPerService { get; }
		public int MaxProductsPerFeature { get; }

		readonly DevelopingAppTerminator appTerminator;

		readonly Node completedFeaturesRoundNode;

		public AgileRequestsManager(NodeTree nt, int round, HeatMapMaintainer heatMapMaintainer, DevelopingAppTerminator appTerminator)
		{
			this.appTerminator = appTerminator;

			model = nt;
			this.heatMapMaintainer = heatMapMaintainer;

			this.round = round;

			serviceIdGenerator = new UniqueServiceIdGenerator();

			timeNode = model.GetNamedNode("CurrentTime");
			timeNode.AttributesChanged += TimeNodeAttributesChanged;


			foreach (var testEnvironment in model.GetNamedNode("TestEnvironments").GetChildrenAsList()
				.Where(n => n.GetBooleanAttribute("can_be_blocked", false)))
			{
				testEnvironment.SetAttributes(new List<AttributeValuePair>
				{
					new AttributeValuePair("in_use", false),
					new AttributeValuePair("service_id", "")
				});
			}

			newServicesRound = model.GetNamedNode("New Services Round " + round);

			beginBizServicesHead = model.GetNamedNode("BeginNewServicesInstall");
			beginBizServicesHead.DeleteChildren();
			beginBizServicesHead.ChildAdded += MonitorNewServicesForWhenAdded;

			startServicesNode = model.GetNamedNode("StartServices");
			foreach (var service in startServicesNode.GetChildrenAsList())
			{
				ProcessStartedService(service);
			}
			startServicesNode.ChildAdded += startServicesNode_ChildAdded;

			beginServicesCommandQueueNode = model.GetNamedNode("BeginServicesCommandQueue");
			beginServicesCommandQueueNode.ChildAdded += beginServicesCommandQueueNode_ChildAdded;

			var newFeaturesRounds = model.GetNamedNode("New Services").GetChildren();

			MaxProductsPerFeature = newFeaturesRounds.SelectMany(r => r.GetChildren())
				.Select(f => f.GetChildren().Count).Max();


			MaxFeaturesPerService = newFeaturesRounds
				.ToDictionary(n => n.GetIntAttribute("round", 0), n => n.GetChildren())
				.Select(kvp => kvp.Value.GroupBy(n => n.GetAttribute("biz_service_function")).Max(g => g.ToList().Count))
				.Max();

			completedFeaturesRoundNode = model.GetNamedNode("CompletedNewServices")
				.GetSingleChild(n => n.GetIntAttribute("round", 0) == round);
		}

		void beginServicesCommandQueueNode_ChildAdded(Node sender, Node child)
		{
			var commandType = child.GetAttribute("type");
			var beginServiceName = child.GetAttribute("service_name");

			var beginServiceNode = model.GetNamedNode(beginServiceName);

			Debug.Assert(beginServiceNode != null);

			switch (commandType)
			{
				case CommandTypes.DevOneSelection:
				case CommandTypes.DevTwoSelection:
				case CommandTypes.TestEnvironmentSelection:
				case CommandTypes.ReleaseSelection:
				case CommandTypes.EnclosureSelection:
					var selection = child.GetAttribute("selection");
					beginServiceNode.SetAttribute(commandType, selection);
					break;
				case CommandTypes.ResetStage:
					ResetServiceToStage(beginServiceNode, child.GetAttribute("target_status"));
					break;
				case CommandTypes.EnqueueDeployment:
					beginServiceNode.SetAttribute("deployment_enqueued", true);
					break;
				case CommandTypes.InstallService:
					beginServiceNode.SetAttribute("install_service", true);
					break;
				case CommandTypes.CancelService:
					beginServiceNode.SetAttribute("status", FeatureStatus.Cancelled);
					break;
				case CommandTypes.UndoService:
					beginServiceNode.SetAttribute("status", FeatureStatus.Undo);
					break;
				case CommandTypes.ClearInstallFeedback:
					beginServiceNode.SetAttribute("install_feedback_message", "");
					break;
				case CommandTypes.RedevelopService:
					beginServiceNode.SetAttribute("status", FeatureStatus.Redevelop);
					break;
				case CommandTypes.PromoteToStatus:

					if (child.GetAttribute("target_status") == FeatureStatus.Live)
					{
						// TODO This could be refactored to be ProgressToTargetStatus which could be handy
						ProgressServiceToDeployment(beginServiceNode, false, false);
					}
					break;
			}

			sender.DeleteChildTree(child);
		}

		void ResetServiceToStage(Node service, string targetStatus)
		{
			var stages = new List<string>
			{
				FeatureStatus.Dev,
				FeatureStatus.Test,
				FeatureStatus.Release
			};

			var targetStatusIndex = stages.IndexOf(targetStatus);

			Debug.Assert(targetStatusIndex != -1, $"Unknown target status: {targetStatus}");

			if (targetStatusIndex == -1)
			{
				return;
			}

			var currentStatus = service.GetAttribute("status");

			if (targetStatus == FeatureStatus.Release &&
				(currentStatus == FeatureStatus.Test || currentStatus == FeatureStatus.TestDelay))
			{
				targetStatus = currentStatus;
			}

			var resetAttributes = new List<AttributeValuePair>
			{
				new AttributeValuePair("install_feedback_message", ""),
				new AttributeValuePair("feedback_message", ""),
				new AttributeValuePair("feedback_image", "")
			};

			if (targetStatus != currentStatus)
			{
				resetAttributes.Add(new AttributeValuePair("status", targetStatus));
			}

			if (targetStatusIndex == stages.IndexOf(FeatureStatus.Dev))
			{
				resetAttributes.Add(new AttributeValuePair("dev_one_selection", ""));
				resetAttributes.Add(new AttributeValuePair("dev_two_selection", ""));
				resetAttributes.Add(new AttributeValuePair("gain_per_minute",
					service.GetAttribute("optimum_gain_per_minute")));
			}

			if (targetStatusIndex <= stages.IndexOf(FeatureStatus.Test))
			{
				var testEnvironmentSelection = service.GetAttribute("test_environment_selection");

				if (!string.IsNullOrEmpty(testEnvironmentSelection))
				{
					var testEnvironment = model.GetNamedNode("TestEnvironments").GetChildWithAttributeValue("desc", testEnvironmentSelection);
					testEnvironment.SetAttribute("in_use", false);
				}

				resetAttributes.Add(new AttributeValuePair("test_environment_selection", ""));
				resetAttributes.Add(new AttributeValuePair("delayremaining", ""));
				resetAttributes.Add(new AttributeValuePair("gain_per_minute",
					service.GetAttribute("optimum_gain_per_minute")));
				resetAttributes.Add(new AttributeValuePair("test_time", service.GetAttribute("default_test_time")));
			}

			if (targetStatusIndex <= stages.IndexOf(FeatureStatus.Release))
			{
				resetAttributes.Add(new AttributeValuePair("release_selection", ""));
			}

			for (var i = targetStatusIndex; i < stages.Count; i++)
			{
				resetAttributes.Add(new AttributeValuePair($"{stages[i]}_stage_status", StageStatus.Incomplete));
			}

			resetAttributes.Add(new AttributeValuePair("deployment_stage_status", StageStatus.Incomplete));
			resetAttributes.Add(new AttributeValuePair("enclosure_selection", ""));


			service.SetAttributes(resetAttributes);
		}

		void startServicesNode_ChildAdded(Node sender, Node child)
		{
			ProcessStartedService(child);
		}

		void ProcessStartedService(Node startService)
		{
			var serviceName = startService.GetAttribute("service_name");

			var newServiceNode = model.GetNamedNode(serviceName);

			if (!startService.GetBooleanAttribute("is_auto_installed", false))
			{
				LogInfo(newServiceNode, serviceName.Substring(3));
			}

			var serviceId = newServiceNode.GetAttribute("service_id");

			var productId = startService.GetAttribute("product_id", "");

			if (string.IsNullOrEmpty(productId))
			{
				productId = GetOptimalProductForService(newServiceNode);
			}

			Debug.Assert(!string.IsNullOrEmpty(productId), "Product ID shouldn't be empty or null.");

			var targetStatus = startService.GetAttribute("target_status", FeatureStatus.Dev);

			var hideInReports = startService.GetBooleanAttribute("hide_in_reports", false);

			var isAutoInstalled = startService.GetBooleanAttribute("is_auto_installed", false);

			var isServiceIconVisible = startService.GetBooleanAttribute("hide_service_icon", false);

			var bizServiceName = newServiceNode.GetAttribute("biz_service_function");

			var uniqueId = startService.GetIntAttribute("unique_id");
			if (uniqueId.HasValue)
			{
				serviceIdGenerator.UpdateId(uniqueId.Value);
			}

			var beginServiceName = StartService(serviceId, productId, uniqueId ?? serviceIdGenerator.GetNextId(), new List<AttributeValuePair>
				{
					new AttributeValuePair("is_hidden_in_reports", hideInReports),
					new AttributeValuePair("is_auto_installed", isAutoInstalled),
					new AttributeValuePair("is_auto_installed_at_end_of_round", startService.GetBooleanAttribute("is_auto_installed_at_end_of_round", false)),
					new AttributeValuePair("hide_service_icon", isServiceIconVisible),
					new AttributeValuePair("extract_incidents", false),
					new AttributeValuePair("parent_service_number", newServiceNode.GetIntAttribute("parent_service_number", 0)),
					new AttributeValuePair("is_redeveloping", model.GetNamedNode(bizServiceName) != null),
					new AttributeValuePair("service_group", newServiceNode.GetAttribute("service_group"))
				},
				startService.GetBooleanAttribute("force_zero_cost", false),
				startService.GetBooleanAttribute("is_auto_installed", false));

			var beginServiceNode = model.GetNamedNode(beginServiceName);

			var stages = new List<string>
			{
				FeatureStatus.Test,
				FeatureStatus.Release,
				FeatureStatus.Deploy,
				FeatureStatus.Live
			};

			var targetStatusIndex = stages.IndexOf(targetStatus);

			if (targetStatusIndex >= stages.IndexOf(FeatureStatus.Test))
			{
				var attributes = new ArrayList();
				AttributeValuePair.AddIfNotEqual(beginServiceNode, attributes, "analysis_stage_status", StageStatus.Completed);
				AttributeValuePair.AddIfNotEqual(beginServiceNode, attributes, "dev_one_selection", beginServiceNode.GetAttribute("dev_one_environment"));
				AttributeValuePair.AddIfNotEqual(beginServiceNode, attributes, "dev_two_selection", beginServiceNode.GetAttribute("dev_two_environment"));
				beginServiceNode.SetAttributes(attributes);
			}

			if (targetStatusIndex >= stages.IndexOf(FeatureStatus.Release))
			{
				beginServiceNode.SetAttributes(new List<AttributeValuePair>
				{
					new AttributeValuePair("test_environment_selection", beginServiceNode.GetAttribute("test_environment")),
					new AttributeValuePair("skip_test", true)
				});
			}

			if (targetStatusIndex >= stages.IndexOf(FeatureStatus.Deploy))
			{
				beginServiceNode.SetAttribute("release_selection", beginServiceNode.GetAttribute("release_code"));
			}

			if (targetStatusIndex >= stages.IndexOf(FeatureStatus.Live))
			{
				var targetEnclosure = startService.GetAttribute("target_enclosure");

				if (string.IsNullOrEmpty(targetEnclosure))
				{
					targetEnclosure = beginServiceNode.GetAttribute("server");
				}

				beginServiceNode.SetAttribute("force_zero_cpu_usage", startService.GetBooleanAttribute("force_zero_cpu_usage", false));

				beginServiceNode.SetAttribute("enclosure_selection", targetEnclosure);
				InstallNewService(beginServiceNode);
			}


			startServicesNode.DeleteChildTree(startService);
		}



		public void Dispose()
		{
			timeNode.AttributesChanged -= TimeNodeAttributesChanged;
			beginBizServicesHead.ChildAdded -= MonitorNewServicesForWhenAdded;
			beginBizServicesHead.ChildRemoved -= MonitorNewServicesForWhenRemoved;

			startServicesNode.ChildAdded -= startServicesNode_ChildAdded;
			beginServicesCommandQueueNode.ChildAdded -= beginServicesCommandQueueNode_ChildAdded;
		}

		public delegate void NewServiceStatusHandler(string mbu, string serviceName, string status, bool isHidden);
		public event NewServiceStatusHandler NewServiceStatusReceived;

		public delegate void FeatureGoneLiveHandler(AgileRequestsManager sender, Node feature);

		public event FeatureGoneLiveHandler FeatureGoneLive;

		void OnNewServiceStatus(string mbu, string serviceName, string status, bool isHidden)
		{
			NewServiceStatusReceived?.Invoke(mbu, serviceName, status, isHidden);
		}

		void MonitorNewServicesForWhenAdded(Node parent, Node child)
		{
			child.AttributesChanged += MonitorNewServicesForWhenStatusChanges;
			OnNewServiceStatus(child.GetAttribute("mbu"), child.GetAttribute("shortdesc"), child.GetAttribute("status"), child.GetBooleanAttribute("hideMessage", false));
		}

		void MonitorNewServicesForWhenRemoved(Node parent, Node child)
		{
			child.AttributesChanged -= MonitorNewServicesForWhenStatusChanges;
		}

		void MonitorNewServicesForWhenStatusChanges(Node sender, ArrayList attributes)
		{
			var devTeamSelectionsInAttributes = 0;
			foreach (var teamName in new[] { "dev_one_selection", "dev_two_selection" })
			{
				foreach (AttributeValuePair avp in attributes)
				{
					if (avp.Attribute == teamName)
					{
						devTeamSelectionsInAttributes++;
					}
				}
			}
			var devTeamSelectionsProcessed = 0;

			foreach (AttributeValuePair avp in attributes)
			{
				switch (avp.Attribute)
				{
					case "status":
						switch (avp.Value)
						{
							case FeatureStatus.TestDelay:
								var testTime = sender.GetIntAttribute("test_time", 120);
								var testEnvironmentNode = model.GetNamedNode("TestEnvironments")
										.GetChildWithAttributeValue("desc", sender.GetAttribute("test_environment_selection"));

								var modifyTestTime = testEnvironmentNode.GetAttribute("test_time_modifier");
								if (!string.IsNullOrEmpty(modifyTestTime))
								{
									var modifyTestValue = (int)(60 * CONVERT.ParseDouble(modifyTestTime));
									var isModifier = (modifyTestTime.StartsWith("+") || modifyTestTime.StartsWith("-"));
									if (isModifier)
									{
										testTime += modifyTestValue;
									}
									else
									{
										testTime = modifyTestValue;
									}
								}

								var testEnvCanBeBlocked = testEnvironmentNode.GetBooleanAttribute("can_be_blocked", false);

								if (testEnvCanBeBlocked)
								{
									testEnvironmentNode.SetAttributes(new List<AttributeValuePair>
									{
										new AttributeValuePair("in_use", true),
			                            // Adding this so that it can be checked for when 
			                            // setting the environment as not in use anymore,
			                            // so only the service that is using it can change it.
			                            new AttributeValuePair("service_id", sender.GetAttribute("service_id"))
									});
								}

								sender.SetAttributes(new List<AttributeValuePair>
													 {
														 new AttributeValuePair("delayRemaining", testTime),
														 new AttributeValuePair("test_time", testTime)

													 });
								break;

							case FeatureStatus.Cancelled:
								var recoup = sender.GetBooleanAttribute("should_recoup_investment", false);
								RemoveService(sender, recoup);
								break;

							case FeatureStatus.Undo:
								RemoveService(sender, true);
								break;

							case FeatureStatus.Redevelop:
								RedevelopFeature(sender);
								break;

							case FeatureStatus.Release:
								if (IsDeploymentQueuedForService(sender))
								{
									InstallNewService(sender);
								}
								break;
						}
						break;

					case "dev_one_selection":
					case "dev_two_selection":
						var devOneSelection = sender.GetAttribute("dev_one_selection");
						var devTwoSelection = sender.GetAttribute("dev_two_selection");
						devTeamSelectionsProcessed++;

						if (devTeamSelectionsProcessed >= devTeamSelectionsInAttributes)
						{
							// Keep just this if block once the spreadsheet exporter is updated
							if (!string.IsNullOrEmpty(devOneSelection) && !string.IsNullOrEmpty(devTwoSelection))
							{
								CheckDevStageSuccessful(sender);
							}
						}

						var devStageStatus = sender.GetAttribute("dev_stage_status");

						if (devStageStatus == StageStatus.Failed &&
							(!string.IsNullOrEmpty(devOneSelection) || !string.IsNullOrEmpty(devTwoSelection)))
						{
							sender.SetAttribute("dev_stage_status", StageStatus.Incomplete);
						}

						break;

					case "test_environment_selection":

						CheckTestStageSuccessful(sender);
						break;

					case "release_selection":
						CheckReleaseCode(sender);
						break;

					case "enclosure_selection":
						CheckEnclosureSelection(sender);
						break;
					case "install_service":
						InstallNewService(sender);
						break;
					case "deployment_enqueued":

						break;
					case "prerequisite_met":
						if (CONVERT.ParseBool(avp.Value, false))
						{
							if (sender.GetAttribute(StageAttribute.DeploymentStage) == StageStatus.Failed)
							{
								sender.SetAttribute(StageAttribute.DeploymentStage, StageStatus.Incomplete);
								// Try to deploy failed MVPs
								if (sender.GetBooleanAttribute("is_prototype", false))
								{
									InstallNewService(sender);
								}
							}
						}
						break;
				}
			}

			OnNewServiceStatus(sender.GetAttribute("mbu"), sender.GetAttribute("shortdesc"), sender.GetAttribute("status"), sender.GetBooleanAttribute("hideMessage", false));
		}

		void CheckDevStageSuccessful(Node service)
		{
			var devOneSelection = service.GetAttribute("dev_one_selection");
			var devEnvOne = service.GetAttribute("dev_one_environment");
			var isDevOneCorrect = devOneSelection == devEnvOne;

			if (service.GetBooleanAttribute("is_third_party", false)
				|| service.GetBooleanAttribute("is_prototype", false))
			{
				if (isDevOneCorrect)
				{
					ServicePassedDev(service, true, true, devOneSelection, "");
				}
				else
				{
					ServiceFailedDev(service, true, true, devOneSelection, "");
				}

				return;
			}

			var devTwoSelection = service.GetAttribute("dev_two_selection");
			var devEnvTwo = service.GetAttribute("dev_two_environment");
			var isDevTwoCorrect = devTwoSelection == devEnvTwo;

			var serviceSecLevel = service.GetAttribute("data_security_level");

			var devNode = model.GetNamedNode("DevBuildEnvironments")
				.GetChildWithAttributeValue("data_security_level", serviceSecLevel)
				.GetChildWithAttributeValue("desc", devOneSelection);

			Debug.Assert(devNode != null, "DevBuildEnvironments not found.");

			var willIntegrate = devNode.GetChildrenAsList().Any(child => child.GetAttribute("desc") == devTwoSelection);
			if (willIntegrate)
			{
				ServicePassedDev(service, isDevOneCorrect, isDevTwoCorrect, devOneSelection, devTwoSelection);
			}
			else
			{
				ServiceFailedDev(service, isDevOneCorrect, isDevTwoCorrect, devOneSelection, devTwoSelection);
			}
		}

		void ServicePassedDev(Node service, bool team1IsOptimal, bool team2IsOptimal, string team1Choice, string team2Choice)
		{
			var isOptimal = team1IsOptimal && team2IsOptimal;

			var effectivenessPercent = isOptimal ? 100 : model.GetNamedNode("TestEnvironments").GetIntAttribute("run_bad_effectiveness_percent", 0);

			var attributes = new List<AttributeValuePair>
								  {
									  new AttributeValuePair("feedback_message", "Development Integration Passed"),
									  new AttributeValuePair("feedback_image", FeedbackImageName.Tick),
									  new AttributeValuePair("test_environment_selection", ""),
									  new AttributeValuePair("status", "test"),
									  new AttributeValuePair("has_passed_stage", true),
									  new AttributeValuePair("optimal", isOptimal),
									  new AttributeValuePair("integration_gain_percent", effectivenessPercent),
									  new AttributeValuePair("dev_stage_passed", true),
									  new AttributeValuePair("dev_stage_status", StageStatus.Completed),
									  new AttributeValuePair("effectiveness_percent", effectivenessPercent)
								  };

			if (!service.GetBooleanAttribute("is_auto_installed", false))
			{
				if (team1IsOptimal)
				{
					LogSuccess(service, service.GetAttribute("biz_service_function"), "dev1-optimal", team1Choice, null);
				}

				if (team2IsOptimal && !string.IsNullOrEmpty(team2Choice))
				{
					LogSuccess(service, service.GetAttribute("biz_service_function"), "dev2-optimal", team2Choice, null);
				}
			}

			if (!isOptimal)
			{
				attributes.Add(new AttributeValuePair("feedback_message", "Suboptimal Development Environments"));

				if (!service.GetBooleanAttribute("is_auto_installed", false))
				{
					if (!team1IsOptimal)
					{
						LogError(service, service.GetAttribute("biz_service_function"), service.GetAttribute("shortdesc"),
							"dev", Guid.NewGuid().ToString(), "Suboptimal Development Environments Team 1", "dev1-suboptimal", team1Choice, null);
					}

					if (!team2IsOptimal)
					{
						LogError(service, service.GetAttribute("biz_service_function"), service.GetAttribute("shortdesc"),
							"dev", Guid.NewGuid().ToString(), "Suboptimal Development Environments Team 2", "dev2-suboptimal", team2Choice, null);
					}


					LogError(service, service.GetAttribute("biz_service_function"), service.GetAttribute("shortdesc"), "dev", Guid.NewGuid().ToString(), $"Suboptimal development environments. Running at {effectivenessPercent}% effectiveness.", "dev-suboptimal", "", effectivenessPercent);
				}
			}

			if (!service.GetBooleanAttribute("is_auto_installed", false))
			{
				var choices = new StringBuilder();

				if (!string.IsNullOrEmpty(team1Choice))
				{
					choices.Append(team1Choice);
				}

				if (!string.IsNullOrEmpty(team2Choice))
				{
					if (choices.Length > 0)
					{
						choices.Append("/");
					}

					choices.Append(team2Choice);
				}

				LogSuccess(service, service.GetAttribute("biz_service_function"), "dev-integration-succeeded", choices.ToString(), effectivenessPercent);
			}

			service.SetAttributes(attributes);
		}

		void ServiceFailedDev(Node service, bool team1IsOptimal, bool team2IsOptimal, string team1Choice, string team2Choice)
		{
			service.SetAttributes(new List<AttributeValuePair>
								  {
									  new AttributeValuePair("dev_one_selection", ""),
									  new AttributeValuePair("dev_two_selection", ""),
									  new AttributeValuePair("feedback_message", "Development Integration Failed"),
									  new AttributeValuePair("feedback_image", FeedbackImageName.Cross),
									  new AttributeValuePair("has_passed_stage", false),
									  new AttributeValuePair("dev_stage_passed", false),
									  new AttributeValuePair("dev_stage_status", StageStatus.Failed)
								  });

			LogAppStageFailure(service, $"{service.GetAttribute("biz_service_function")} - Development Stage", StageFailureMessage.DevelopmentStage);

			LogError(service, service.GetAttribute("biz_service_function"), service.GetAttribute("shortdesc"),
				"dev", Guid.NewGuid().ToString(), "Development Integration Failed", "dev-integration-failed", CONVERT.Format("{0} / {1}", team1Choice, team2Choice), null);

			LogError(service, service.GetAttribute("biz_service_function"), service.GetAttribute("shortdesc"),
				"dev", Guid.NewGuid().ToString(), "Development Integration Failed", team1IsOptimal ? "dev1-optimal" : "dev1-suboptimal", team1Choice, null);

			LogError(service, service.GetAttribute("biz_service_function"), service.GetAttribute("shortdesc"),
				"dev", Guid.NewGuid().ToString(), "Development Integration Failed", team2IsOptimal ? "dev2-optimal" : "dev2-suboptimal", team2Choice, null);
		}

		void ServiceFailedIntegration (Node service, bool team1IsOptimal, bool team2IsOptimal, string team1Choice, string team2Choice)
		{
			var buildIsOptimal = team1IsOptimal && team2IsOptimal;

			var effectivenessPercent = buildIsOptimal ? 100 : model.GetNamedNode("TestEnvironments").GetIntAttribute("run_bad_effectiveness_percent", 0);
			effectivenessPercent = effectivenessPercent * 80 / 100;

			var attributes = new List<AttributeValuePair>
			{
				new AttributeValuePair("feedback_message", "Development Integration Failed"),
				new AttributeValuePair("feedback_image", FeedbackImageName.Cross),
				new AttributeValuePair("test_environment_selection", ""),
				new AttributeValuePair("status", "test"),
				new AttributeValuePair("has_passed_stage", true),
				new AttributeValuePair("optimal", buildIsOptimal),
				new AttributeValuePair("integration_gain_percent", effectivenessPercent),
				new AttributeValuePair("dev_stage_passed", true),
				new AttributeValuePair("dev_stage_status", StageStatus.Completed),
				new AttributeValuePair("effectiveness_percent", effectivenessPercent)
			};

			if (!service.GetBooleanAttribute("is_auto_installed", false))
			{
				if (team1IsOptimal)
				{
					LogSuccess(service, service.GetAttribute("biz_service_function"), "dev1-optimal", team1Choice, null);
				}

				if (team2IsOptimal && !string.IsNullOrEmpty(team2Choice))
				{
					LogSuccess(service, service.GetAttribute("biz_service_function"), "dev2-optimal", team2Choice, null);
				}
			}

			if (! buildIsOptimal)
			{
				attributes.Add(new AttributeValuePair("feedback_message", "Development Integration Failed; Suboptimal Development Environments"));

				if (!service.GetBooleanAttribute("is_auto_installed", false))
				{
					if (!team1IsOptimal)
					{
						LogError(service, service.GetAttribute("biz_service_function"), service.GetAttribute("shortdesc"),
							"dev", Guid.NewGuid().ToString(), "Suboptimal Development Environments Team 1", "dev1-suboptimal", team1Choice, null);
					}

					if (!team2IsOptimal)
					{
						LogError(service, service.GetAttribute("biz_service_function"), service.GetAttribute("shortdesc"),
							"dev", Guid.NewGuid().ToString(), "Suboptimal Development Environments Team 2", "dev2-suboptimal", team2Choice, null);
					}


					LogError(service, service.GetAttribute("biz_service_function"), service.GetAttribute("shortdesc"), "dev", Guid.NewGuid().ToString(), $"Suboptimal development environments. Running at {effectivenessPercent}% effectiveness.", "dev-suboptimal", "", effectivenessPercent);
				}
			}

			if (!service.GetBooleanAttribute("is_auto_installed", false))
			{
				var choices = new StringBuilder();

				if (!string.IsNullOrEmpty(team1Choice))
				{
					choices.Append(team1Choice);
				}

				if (!string.IsNullOrEmpty(team2Choice))
				{
					if (choices.Length > 0)
					{
						choices.Append("/");
					}

					choices.Append(team2Choice);
				}

				LogSuccess(service, service.GetAttribute("biz_service_function"), "dev-integration-failed", choices.ToString(), effectivenessPercent);
			}

			service.SetAttributes(attributes);
		}

		void CheckTestStageSuccessful (Node service)
		{
			var testSelection = service.GetAttribute("test_environment_selection");

			if (string.IsNullOrEmpty(testSelection))
			{
				return;
			}
			// The first time a new test environment has been selected since
			// they failed so reset the status to "incomplete".
			if (service.GetAttribute("test_stage_status") == StageStatus.Failed)
			{
				service.SetAttribute("test_stage_status", StageStatus.Incomplete);
			}

			var percent = service.GetIntAttribute("integration_gain_percent", 100);
			var nextStatus = FeatureStatus.TestDelay;

			var feedbackMessage = "Test Passed";
			var feedbackImageName = FeedbackImageName.Tick;

			var isOptimal = service.GetBooleanAttribute("optimal", true);
			var testCost = 0;

			var testEnvironments = model.GetNamedNode("TestEnvironments");

			var testStageStatus = StageStatus.InProgress;

			if (testSelection == "Bypass")
			{
				testStageStatus = StageStatus.Completed;
				if (service.GetBooleanAttribute("is_prototype", false))
				{
					feedbackMessage = "Test Not Needed";

					feedbackImageName = FeedbackImageName.Tick;
					nextStatus = FeatureStatus.Release;
					

					isOptimal = true;
				}
				else
				{
					percent = percent * testEnvironments.GetIntAttribute("bypass_test_effectiveness_percent", 100) / 100;

					feedbackMessage = "Test Bypassed";

					feedbackImageName = FeedbackImageName.Cross;
					nextStatus = FeatureStatus.Release;
					

					isOptimal = false;

					var serviceName = service.GetAttribute("biz_service_function");
					var shortName = service.GetAttribute("shortdesc");
					if (!service.GetBooleanAttribute("is_auto_installed", false))
					{
						LogError(service, serviceName, shortName, "test", Guid.NewGuid().ToString(), CONVERT.Format("Test bypassed. Running at {0}% effectiveness.", percent), "test-bypassed", "", percent);
					}
				}

			}
			else
			{
				var testEnvironment = testEnvironments.GetChildWithAttributeValue("desc", testSelection);
				var serviceName = service.GetAttribute("biz_service_function");

				if (!service.GetBooleanAttribute("is_auto_installed", false))
				{
					LogSuccess(service, serviceName, "test-done", "", percent);
				}

				if (!service.GetBooleanAttribute("skip_test", false))
				{
					var serviceSecLevel = service.GetAttribute("data_security_level");

					if (string.IsNullOrEmpty(serviceSecLevel))
					{
						throw new Exception("New Service missing security level");
					}

					var secLevelSupported =
						testEnvironment.GetChildrenWithAttributeValue("type", "SecurityLevel")
							.Any(secNode => secNode.GetAttribute("desc") == serviceSecLevel);

					var isVirtual = (testEnvironment.GetAttribute("desc") == "Virtual Test");

					var optimalEnv = service.GetAttribute("test_environment");
					isOptimal = ((testEnvironment.GetAttribute("desc") == optimalEnv)
								|| isVirtual
								|| string.IsNullOrEmpty(optimalEnv));

					testCost = testEnvironment.GetIntAttribute("extra_cost", 0);

					string errorLogString = null;

					var testEnvironmentDescription = testEnvironment.GetAttribute("desc");
					if (isVirtual)
					{
						testEnvironmentDescription = "Virtual";
					}

					if ((!string.IsNullOrEmpty(optimalEnv))
						&& (!service.GetBooleanAttribute("is_auto_installed", false)))
					{
						switch (testSelection)
						{
							case "2":
								LogSuccess(service, serviceName, "test-extra-time", "2", null);
								if (optimalEnv != "2")
								{
									errorLogString = "Suboptimal test environment selected-Additonal test time added.";
								}

								feedbackImageName = FeedbackImageName.Clock;
								break;

							case "3":
								LogSuccess(service, serviceName, "test-extra-cost", "3", null);
								if (optimalEnv != "3")
								{
									errorLogString = "Suboptimal test environment selected-Additional test cost incurred.";
								}

								feedbackImageName = FeedbackImageName.Cash;

								break;
						}
					}

					if (isOptimal)
					{
						if (!service.GetBooleanAttribute("is_auto_installed", false))
						{
							LogSuccess(service, serviceName, "test-right-environment", testEnvironmentDescription, percent);
						}
					}
					else
					{
						percent = percent * testEnvironments.GetIntAttribute("run_bad_effectiveness_percent", 100) / 100;

						if (!secLevelSupported)
						{
							errorLogString += "Security level not supported.";
						}

						errorLogString += CONVERT.Format("Running at {0}% effectiveness", percent);

						if (!service.GetBooleanAttribute("is_auto_installed", false))
						{
							LogError(service, serviceName, serviceName, "test", Guid.NewGuid().ToString(), errorLogString, "test-wrong-environment", testEnvironmentDescription, percent);
						}
					}
				}
				else
				{
					nextStatus = FeatureStatus.Release;
					testCost = 0;
				}
			}

			service.SetAttributes(new List<AttributeValuePair>
												 {
													 new AttributeValuePair("status", nextStatus),
													 new AttributeValuePair("feedback_message", feedbackMessage),
													 new AttributeValuePair("feedback_image", feedbackImageName),
													 new AttributeValuePair("optimal", isOptimal),
													 new AttributeValuePair("has_passed_stage", true),
													 new AttributeValuePair("test_stage_status", testStageStatus),
													 new AttributeValuePair("effectiveness_percent", percent)
												 });

			if (testCost != 0)
			{
				LogCost(service.GetAttribute("biz_service_function"), "Test", testCost);
				AdjustBudget(-testCost);
			}
		}

		void CheckReleaseCode(Node service)
		{
			var releaseSelection = service.GetAttribute("release_selection");
			var isServicePrototype = service.GetBooleanAttribute("is_prototype", false);

			if (string.IsNullOrEmpty(releaseSelection) && !isServicePrototype)
			{
				service.SetAttribute("deployment_enqueued", false);
				return;
			}
			// The first time a new release code has been selected since
			// they failed so reset the status to "incomplete".
			if (service.GetAttribute("release_stage_status") == StageStatus.Failed)
			{
				service.SetAttribute("release_stage_status", StageStatus.Incomplete);
			}

			var correctReleaseCode = service.GetAttribute("release_code");
			var isCorrectReleaseCode = isServicePrototype || (string.IsNullOrEmpty(correctReleaseCode)
										|| (releaseSelection == correctReleaseCode));

			if (isCorrectReleaseCode)
			{
				service.SetAttributes(new List<AttributeValuePair>
				{
					new AttributeValuePair("has_passed_stage", true),
					new AttributeValuePair("release_stage_passed", true),
					new AttributeValuePair("release_stage_status", StageStatus.Completed)
				});

				if (!service.GetBooleanAttribute("is_auto_installed", false))
				{
					LogSuccess(service, service.GetAttribute("biz_service_function"), "release-right-code", releaseSelection, null);
				}
			}
			else
			{
				service.SetAttributes(new List<AttributeValuePair>
				{
					new AttributeValuePair("release_selection", ""),
					new AttributeValuePair("has_passed_stage", false),
					new AttributeValuePair("release_stage_passed", false),
					new AttributeValuePair("release_stage_status", StageStatus.Failed)
				});

				LogAppStageFailure(service, $"{service.GetAttribute("biz_service_function")} - Release Stage", StageFailureMessage.ReleaseStage);

				if (!service.GetBooleanAttribute("is_auto_installed", false))
				{
					LogError(service, service.GetAttribute("biz_service_function"), service.GetAttribute("shortdesc"), "release", Guid.NewGuid().ToString(), "Incorrect Release Code", "release-wrong-code", releaseSelection, null);
				}
			}
		}

		void CheckEnclosureSelection(Node service)
		{
			var enclosureSelected = service.GetAttribute("enclosure_selection");

			if (string.IsNullOrEmpty(enclosureSelected))
			{
				return;
			}

			var serviceName = service.GetAttribute("biz_service_function");

			var cpuReq = service.GetIntAttribute("cpu_req", 0);

			if (service.GetBooleanAttribute("force_zero_cpu_usage", false))
			{
				cpuReq = 0;
			}


			var server = service.GetAttribute("server");

			string feedbackMessage;
			var canDeploy = false;

			var feedbackImageName = FeedbackImageName.Tick;

			if (server.Contains("Fail"))
			{
				service.SetAttribute("is_install_successful", false);
				feedbackMessage = $"Fail - {service.GetAttribute("failure_remark")}";
				feedbackImageName = FeedbackImageName.Cross;

				if (!service.GetBooleanAttribute("is_auto_installed", false))
				{
					LogError(service, serviceName, service.GetAttribute("shortdesc"), "deploy", Guid.NewGuid().ToString(), feedbackMessage, "deploy-install-failed", enclosureSelected, null);
				}
			}
			else
			{
				var liveService = model.GetNamedNode(serviceName);

				var serviceIsLive = liveService != null;

				if (serviceIsLive)
				{
					var appName = service.GetAttribute("app");

					var liveEnclosure = model.GetNamedNode(appName)?.Parent;
					var liveEnclosureName = liveEnclosure?.GetAttribute("name") ?? "";

					if (liveEnclosureName == enclosureSelected)
					{
						var currentCpuReq = liveService.GetIntAttribute("cpu_req", 0);

						cpuReq = Math.Max(0, cpuReq - currentCpuReq);
					}

				}

				canDeploy = IsSufficientCapacityAvailable(enclosureSelected, out feedbackMessage, cpuReq);


				if (canDeploy)
				{
					if (!service.GetBooleanAttribute("is_auto_installed", false))
					{
						LogSuccess(service, serviceName, "deploy-install-success", enclosureSelected, null);
					}
				}
			}

			service.SetAttributes(new List<AttributeValuePair>
			{
				new AttributeValuePair("install_feedback_message", feedbackMessage),
				new AttributeValuePair("feedback_image", feedbackImageName),
				new AttributeValuePair("can_deploy", canDeploy)
			});
		}

		bool IsSufficientCapacityAvailable(string enclosureName, out string message, int cpuReq)
		{
			var enclosureNode = model.GetNamedNode(enclosureName);

			var freeCpu = enclosureNode.GetIntAttribute("free_cpu", 0);
			var freeHeight = enclosureNode.GetIntAttribute("free_height", 0);


			var numCpuPerBlade = model.GetNamedNode("blade").GetIntAttribute("cpu", 15);
			if (freeCpu >= cpuReq)
			{
				// If there's sufficient space for the service in the enclosure
				// then notify the user.
				message = "Sufficient CPU";
				return true;
			}
			else if (freeHeight > 0)
			{
				if (cpuReq - freeCpu <= freeHeight * numCpuPerBlade)
				{
					message = "Insufficient CPU, New Blade Required";
					return true;
				}
			}

			message = "No Capacity or Blades Remaining.";

			return false;
		}

		static bool IsDeploymentQueuedForService(Node service)
		{
			var enclosureSelection = service.GetAttribute("enclosure_selection");
			var isDeploymentQueued = service.GetBooleanAttribute("deployment_enqueued", false);

			return isDeploymentQueued &&
				   !string.IsNullOrEmpty(enclosureSelection);
		}

		void LogInfo(Node newService, string serviceName) => new Node(model.GetNamedNode("CostedEvents"), "NS_info", "", new List<AttributeValuePair>
		{
			new AttributeValuePair ("type", "NS_info"),
			new AttributeValuePair ("product_id", newService.GetAttribute("product_id")),
			new AttributeValuePair ("service_name", serviceName),
			new AttributeValuePair ("is_prototype", newService.GetBooleanAttribute("is_prototype", false))
		});

		void LogSuccess(Node newService, string serviceName, string errorFullType, string errorDetails, int? effectivenessPercent)
		{
			var attributes = new List<AttributeValuePair>
			{
				new AttributeValuePair ("unique_id", GetUniqueBeginServiceTag(newService)),
				new AttributeValuePair ("type", "NS_error"),
				new AttributeValuePair ("product_id", newService.GetAttribute("product_id")),
				new AttributeValuePair ("service_name", serviceName),
				new AttributeValuePair ("error_full_type", errorFullType),
				new AttributeValuePair ("error_details", errorDetails),
				new AttributeValuePair ("is_prototype", newService.GetBooleanAttribute("is_prototype", false))
			};

			if (effectivenessPercent.HasValue)
			{
				attributes.Add(new AttributeValuePair("effectiveness_percent", effectivenessPercent.Value));
			}

			new Node(model.GetNamedNode("CostedEvents"), "NS_error", "", attributes);
		}

		void LogError(Node newService, string serviceName, string shortName, string errorType, string guid, string errorMessage, string errorFullType, string errorDetails, int? effectivenessPercent)
		{
			var attributes = new List<AttributeValuePair>
			{
				new AttributeValuePair ("type", "NS_error"),
				new AttributeValuePair ("unique_id", GetUniqueBeginServiceTag(newService)),
				new AttributeValuePair ("product_id", newService.GetAttribute("product_id")),
				new AttributeValuePair ("service_name", serviceName),
				new AttributeValuePair ("shortdesc", shortName),
				new AttributeValuePair ("error_type", errorType),
				new AttributeValuePair ("error_full_type", errorFullType),
				new AttributeValuePair ("error_message", errorMessage),
				new AttributeValuePair ("error_guid", guid),
				new AttributeValuePair ("error_details", errorDetails),
				new AttributeValuePair ("is_prototype", newService.GetBooleanAttribute("is_prototype", false)),
				new AttributeValuePair ("notes", newService.GetAttribute("failure_remark"))
			};

			if (effectivenessPercent.HasValue)
			{
				attributes.Add(new AttributeValuePair("effectiveness_percent", effectivenessPercent.Value));
			}

			new Node(model.GetNamedNode("CostedEvents"), "NS_error", "", attributes);
		}

		void RedevelopFeature (Node feature)
		{
			var completedFeature = completedFeaturesRoundNode.GetSingleChild(n =>
				n.GetAttribute("name") == feature.GetAttribute("name").Replace("Begin", "Completed"));

			completedFeature?.SetAttributeIfNotEqual("status", "Redeveloping");

		}

		void ReplaceFeatures (string featureId)
		{
			var completedFeatures = completedFeaturesRoundNode
				.GetChildrenWhere(n =>
					n.GetAttribute("service_id") == featureId &&
					(n.GetAttribute("status") == "Live" ||
					 n.GetAttribute("status") == "Redeveloping"));

			foreach (var feature in completedFeatures)
			{
				feature.SetAttributeIfNotEqual("status", "Replaced");
			}
		}

		void RevertReplacements (string featureId)
		{
			var latestPreviousDevelopmentNode = beginBizServicesHead.GetChildrenWhere(n => n.GetAttribute("service_id") == featureId).OrderByDescending(n => n.GetIntAttribute("time_started", 0)).FirstOrDefault();

			if (latestPreviousDevelopmentNode != null)
			{
				Debug.Assert(latestPreviousDevelopmentNode.GetAttribute("status") == FeatureStatus.Redevelop);

				latestPreviousDevelopmentNode.SetAttribute("status", FeatureStatus.Live);
				
				var latestCompletedFeature = model.GetNamedNode(latestPreviousDevelopmentNode.GetAttribute("name")
					.Replace("Begin", "Completed"));

				if (latestCompletedFeature != null)
				{
					Debug.Assert(latestCompletedFeature.GetAttribute("status") == "Redeveloping");

					latestCompletedFeature.SetAttribute("status", "Live");
				}

			}
		}

		void RemoveAllForFeature (string featureId, bool prototypesOnly)
		{
			beginBizServicesHead.DeleteChildrenWhere(n => n.GetAttribute("service_id") == featureId && (!prototypesOnly || n.GetBooleanAttribute("is_prototype", false)));

			completedFeaturesRoundNode.DeleteChildrenWhere(n => n.GetAttribute("service_id") == featureId && (!prototypesOnly || n.GetBooleanAttribute("is_prototype", false)));
		}


		void RemoveService(Node service, bool isUndo = false)
		{
			if (isUndo)
			{
				var investment = service.GetIntAttribute("business_investment", 0);
				LogCost(service.GetAttribute("biz_service_function"), "Undo", -investment);
				//AdjustBudget(investment);
			}

			// If a service is removed during testing (timer etc) then the
			// test environment remains in use
			var testSelection = service.GetAttribute("test_environment_selection");

			// service.GetAttribute("status") == FeatureStatus.TestDelay // TODO can't check for this as the status has been changed to "undo" or "cancel" ... 
			if (!string.IsNullOrEmpty(testSelection))
			{
				var testEnvironment = model.GetNamedNode("TestEnvironments")
					.GetChildWithAttributeValue("desc", testSelection);
				// Check if the test environment selected for this service is in use and by this service
				if (testEnvironment.GetBooleanAttribute("in_use", false) && testEnvironment.GetAttribute("service_id") == service.GetAttribute("service_id"))
				{
					testEnvironment.SetAttributes(new List<AttributeValuePair>
						{
							new AttributeValuePair("in_use", false),
							new AttributeValuePair("service_id", "")
						});
				}
			}


			var isPrototype = service.GetBooleanAttribute("is_prototype", false);
			var isFeatureLive = service.GetAttribute("deployment_stage_status") == StageStatus.Completed;
			
			
			var isRetired = isFeatureLive && ! isUndo;

			var isAborted = ! isFeatureLive && ! isUndo;
			
			
			// Need to know if service being aborted/undone is live (not possible for undo)
			// or mid-development. If it's mid-development then it should get the latest
			// previous completed feature node that is "replaced" and reset to "live"
			// If this is being retired (aborted when live) then all completed nodes
			// for this feature ID should be removed (along with all begin nodes too?)
			
			// TODO current flow is thus:
			// When a development node (begin...) goes live a completed node is created and given the status of "live"
			// If another development node for the same feature is started then the previous is set to "redevelop", as is its completed node
			// Aborting development on the new one will revert the previous one to "live"
			// Retiring a feature removes all references to it (any begin or completed nodes that share its feature ID)


			var featureId = service.GetAttribute("service_id");
			var featureName = service.GetAttribute("name");

			beginBizServicesHead.DeleteChildTree(service);


			if (isRetired)
			{
				RemoveAllForFeature(featureId, false);
				
			}
			else if (isAborted)
			{
				RemoveAllForFeature(featureId, true);

				RevertReplacements(featureId);

			}

			if (isUndo)
			{
				RevertReplacements(featureId);

				completedFeaturesRoundNode.DeleteChildrenWhere(n =>
					n.GetAttribute("name") == featureName.Replace("Begin", "Completed"));


				model.GetNamedNode("CostedEvents").CreateChild("NS_error", "", new List<AttributeValuePair>
				{
					new AttributeValuePair("service_name", service.GetAttribute("biz_service_function")),
					new AttributeValuePair("error_type", "undo"),
				});
			}

			var serviceName = service.GetAttribute("biz_service_function");
			var realService = model.GetNamedNode(serviceName);
			heatMapMaintainer.RemoveFeatureFromService(realService, featureId);

			var level2StageRemoved = service.GetAttribute("level_2_stage_removed");
			if ((!string.IsNullOrEmpty(level2StageRemoved))
			    && (!service.GetBooleanAttribute("is_prototype", false)))
			{
				var level1Stage = model.GetNamedNode("Stages").GetChildWithAttributeValue("desc", serviceName);
				var level2Stage = level1Stage.GetChildWithAttributeValue("level_2_stage", level2StageRemoved);
				level2Stage.SetAttribute("is_disabled", false);
			}

			RemoveMetricAdjustments(realService, service.GetAttribute("service_id"));
		}

		

		
		void TimeNodeAttributesChanged(Node sender, ArrayList attributes)
		{
			foreach (var newServiceNode in beginBizServicesHead.GetChildrenAsList().Where(n => (n.GetAttribute("status") == FeatureStatus.TestDelay)
																								|| (n.GetBooleanAttribute("is_prototype", false) && (n.GetAttribute("status") == FeatureStatus.Dev))))
			{
				var delayTime = newServiceNode.GetIntAttribute("delayRemaining");
				if (delayTime >= 0)
				{
					delayTime--;
					newServiceNode.SetAttribute("delayRemaining", delayTime.Value);
				}

				if (delayTime <= 0)
				{
					switch (newServiceNode.GetAttribute("status"))
					{
						case FeatureStatus.Dev:
						{
							ProgressServiceToDeployment(newServiceNode, false, false);
							break;
						}

						case FeatureStatus.TestDelay:
							// TODO Really this should just be marking the test_stage_status as
							// "Complete" and then that's watched for and sets the status to "Release"
							// rather than what is done in the CheckTest method (GDC)
							newServiceNode.SetAttribute("status", FeatureStatus.Release);
							newServiceNode.SetAttribute("test_stage_status", StageStatus.Completed);

							var testEnvironment = model.GetNamedNode("TestEnvironments")
								.GetChildWithAttributeValue("desc", newServiceNode.GetAttribute("test_environment_selection"));

							if (testEnvironment.GetAttribute("service_id") == newServiceNode.GetAttribute("service_id"))
							{
								testEnvironment.SetAttributes(new List<AttributeValuePair>
								{
									new AttributeValuePair("in_use", false),
									new AttributeValuePair("service_id", "")
								});
							}

							break;
					}
				}
			}
		}

		class NewServicesIdComparer : IComparer<Node>
		{
			public int Compare(Node lhs, Node rhs)
			{
				Debug.Assert(lhs != null && rhs != null, "Neither should be null");

				return string.Compare(lhs.GetAttribute("service_id"), rhs.GetAttribute("service_id"), StringComparison.Ordinal);
			}
		}
		
		public static int GetFeatureBestPerformance (Node feature, bool includeMvp = false)
		{
			var bestProduct = feature.GetChildrenAsList()
				.Where(p => includeMvp || ! p.GetBooleanAttribute("is_prototype", false)).Aggregate(
					(bestSoFar, ns) =>
					{
						var bestSoFarPerformance = (bestSoFar == null) ? null : (int?) GetProductPerformance(bestSoFar);
						var thisProductPerformance = GetProductPerformance(ns);
						return bestSoFarPerformance.HasValue && bestSoFarPerformance.Value < thisProductPerformance
							? ns
							: bestSoFar;
					});
			
			return GetProductPerformance(bestProduct);
		}

		public static int GetProductPerformance(Node product)
		{
			var metrics = product.GetFirstChildOfType("metric_adjustments");
			var metric1Change = metrics.GetChildWithAttributeValue("metric_name", "metric_1");
			return Math.Sign(metric1Change.GetDoubleAttribute("actual", 0) - metric1Change.GetDoubleAttribute("expected", 0));
		}

		public List<string> GetFeatureIdsForService (string serviceName)
		{
			var features = newServicesRound.GetChildrenAsList()
				.Where(ns => ns.GetAttribute("biz_service_function") == serviceName).ToList();

			features.Sort(new NewServicesIdComparer());

			return features.Select(f => f.GetAttribute("service_id")).ToList();
		}

		public List<FeatureAvailability> GetFeaturesByServiceName(string bizServiceName)
		{
			var features = newServicesRound.GetChildrenWhere(ns => ns.GetAttribute("biz_service_function") == bizServiceName);

			features.Sort(new NewServicesIdComparer());

			var featureAvailabilities = new List<FeatureAvailability>();

			foreach (var feature in features)
			{
				var serviceId = feature.GetAttribute("service_id");

				featureAvailabilities.Add(new FeatureAvailability
				{
					FeatureNode = feature,
					IsAvailable = !HasNewServiceDevelopmentAlreadyStarted(serviceId) && string.IsNullOrEmpty(feature.GetAttribute("target_status")),
					FeatureId = serviceId
				});
			}

			return featureAvailabilities;
		}

		public List<KeyValuePair<Node, bool>> GetServices(IDictionary<int, int> parentServiceNumberToRemainingLocations)
		{
			var newServices = newServicesRound.GetChildrenAsList();
			newServices.Sort(new NewServicesIdComparer());

			var serviceIds = new List<KeyValuePair<Node, bool>>();

			foreach (var newService in newServices)
			{
				var serviceId = newService.GetAttribute("service_id");

				serviceIds.Add(new KeyValuePair<Node, bool>(newService,
																(!HasNewServiceDevelopmentAlreadyStarted(serviceId))
																&& string.IsNullOrEmpty(newService.GetAttribute("target_status"))
																&& ((parentServiceNumberToRemainingLocations == null) ||
																	(parentServiceNumberToRemainingLocations[newService.GetIntAttribute("parent_service_number", 0)] > 0))));
			}

			return serviceIds;
		}

		public string GetBusinessServiceNameFromServiceId(string serviceId)
		{
			var newService = newServicesRound.GetChildWithAttributeValue("service_id", serviceId);

			return newService.GetAttribute("biz_service_function") ?? "NOT FOUND";
		}

		public List<FeatureProduct> GetProductsForFeature(string featureId)
		{
			var featureNode = newServicesRound.GetChildWithAttributeValue("service_id", featureId);

			var productNodes = featureNode.GetChildrenAsList();

			return productNodes.Select(pn => new FeatureProduct
			{
				ProductNode = pn,
				IsOptimal = pn.GetBooleanAttribute("is_optimal_product", false),
				Platform = pn.GetAttribute("platform"),
				ProductId = pn.GetAttribute("product_id"),
				IsEnabled = !IsFeatureInDevelopment(featureId) && CanDevelopmentBeStarted(featureId, pn.GetAttribute("product_id"))
			}).ToList();
		}

		bool IsFeatureInDevelopment (string featureId)
		{
			var developmentNodes = beginBizServicesHead.GetChildrenWhere(n => n.GetAttribute("service_id") == featureId);


			return developmentNodes.Select(d => FeatureStatus.All.IndexOf(d.GetAttribute("status"))).Where(i => i != -1)
				.Any(i => i < FeatureStatus.IndexOfLastDevelopmentStatus);
		}

		bool IsDevelopmentInProgressForProduct (string featureId, string productId)
		{
			var developmentNodes = beginBizServicesHead.GetChildrenWhere(n =>
				n.GetAttribute("service_id") == featureId && n.GetAttribute("product_id") == productId);
			
			return developmentNodes.Any(n => FeatureStatus.All.IndexOf(n.GetAttribute("status")) != -1 && FeatureStatus.All.IndexOf(n.GetAttribute("status")) < FeatureStatus.IndexOfLastDevelopmentStatus);
		}

		bool CanDevelopmentBeStarted (string featureId, string productId)
		{
			var completedServices = model.GetNamedNode("CompletedNewServices")
				.GetFirstChild(n => n.GetIntAttribute("round", 0) == round)
				.GetChildrenWhere(n => n.GetAttribute("service_id") == featureId);

			var productNode = newServicesRound.GetFirstChild(n => n.GetAttribute("service_id") == featureId).GetFirstChild(n => n.GetAttribute("product_id") == productId);

			var isPrototype = productNode.GetBooleanAttribute("is_prototype", false);

			if (productId == "MVP C01")
			{

			}

			if (completedServices.Any() && isPrototype)
			{
				return false;
			}
			
			return !IsDevelopmentInProgressForProduct(featureId, productId);
		}

		public List<string> GetProductsForService(string serviceId)
		{
			var newService = newServicesRound.GetChildWithAttributeValue("service_id", serviceId);

			var productIds = new List<string>();

			foreach (Node product in newService)
			{
				productIds.Add(product.GetAttribute("product_id"));
			}

			return productIds;
		}


		public bool HasNewServiceDevelopmentAlreadyStarted(string serviceId)
		{
			foreach (Node installService in beginBizServicesHead)
			{
				if (installService.GetAttribute("service_id").Equals(serviceId))
				{
					return true;
				}
			}
			return false;
		}

		public List<string> GetEnclosureNames()
		{
			var routersList = model.GetNamedNode("Hub").GetChildrenAsList();
			var enclosureList = new List<string>();

			foreach (var router in routersList)
			{
				var enclosureNodeList = router.GetChildrenAsList();

				foreach (var enclosure in enclosureNodeList)
				{
					if (!enclosure.GetBooleanAttribute("hidden", false) && enclosure.GetAttribute("type").Equals("Server"))
					{
						enclosureList.Add(enclosure.GetAttribute("name"));
					}
				}
			}

			return enclosureList;
		}

		public bool CanDeployFeature (string developmentName)
		{
			// If feature is already completed then it can't be deployed.
			// TODO test that the entry in completed services is removed for aborts etc
			// Might also be best using the node's name rather than just the feature and product IDs
			// as there might be more than one due to redevelopment
			if (model.GetNamedNode("CompletedNewServices").GetChildrenAsList().Any(c =>
				//c.GetAttribute("service_id") == featureId && c.GetAttribute("product_id") == productId
				c.GetAttribute("name") == developmentName.Replace("Begin", "Completed")))
			{
				return false;
			}

			

			return true;
		}

		public List<EnclosureProperties> GetEnclosures(Node featureNode)
		{
			var cpuReq = featureNode.GetIntAttribute("cpu_req", 0);

			return model.GetNamedNode("Hub").GetChildrenAsList().SelectMany(r => r.GetChildrenAsList()).Where(e =>
				!e.GetBooleanAttribute("hidden", false) && e.GetAttribute("type") == "Server").Select(e =>
			   new EnclosureProperties
			   {
				   Name = e.GetAttribute("name"),
				   IsEnabled = IsSufficientCapacityAvailable(e.GetAttribute("name"), out var message, cpuReq)
			   }).ToList();
		}

		string TrimRoundSuffixFromFeatureId (string featureId)
		{
			var spaceParenIndex = featureId.IndexOf(" (");
			if (spaceParenIndex != -1)
			{
				return featureId.Substring(0, spaceParenIndex);
			}

			var parenIndex = featureId.IndexOf("(");
			if (parenIndex != -1)
			{
				return featureId.Substring(0, parenIndex);
			}

			return featureId;
		}

		public Node GetFeatureById (string featureId)
		{
			var tryFeature = newServicesRound.GetChildrenAsList().SingleOrDefault(feature => feature.GetAttribute("service_id") == featureId);
			if (tryFeature != null)
			{
				return tryFeature;
			}

			// Allow, eg, "S13" to match "S13 (2)".
			tryFeature = newServicesRound.GetChildrenAsList().SingleOrDefault(feature => TrimRoundSuffixFromFeatureId(feature.GetAttribute("service_id")) == featureId);
			if (tryFeature != null)
			{
				return tryFeature;
			}

			return null;
		}

		public Node GetServiceByFeature (Node feature)
		{
			return model.GetNamedNode(feature.GetAttribute("biz_service_function"));
		}

		public Node GetProductById (string productId)
		{
			foreach (var feature in newServicesRound.GetChildrenAsList())
			{
				var tryProduct = feature.GetChildrenAsList().SingleOrDefault(p => p.GetAttribute("product_id") == productId);
				if (tryProduct != null)
				{
					return tryProduct;
				}

				var featureId = TrimRoundSuffixFromFeatureId(feature.GetAttribute("service_id"));
				if (productId.StartsWith(featureId))
				{
					var productSuffix = productId.Substring(featureId.Length).ToUpper();
					tryProduct = feature.GetChildrenAsList().SingleOrDefault(p => ((p.GetAttribute("platform") == productSuffix) && ! p.GetBooleanAttribute("is_prototype", false))
																				  || (productSuffix.StartsWith("M") && p.GetBooleanAttribute("is_prototype", false)));
					if (tryProduct != null)
					{
						return tryProduct;
					}
				}
			}

			return null;
		}

		public void StartServiceDevelopment (string productId)
		{
			var product = GetProductById(productId);
			var feature = product.Parent;

			StartServiceDevelopment(feature.GetAttribute("service_id"), product.GetAttribute("product_id"));
		}

		public void StartServiceDevelopment(string serviceId, string productId)
		{
			var developmentNodes =
				beginBizServicesHead.GetChildrenWhere(n => n.GetAttribute("service_id") == serviceId && n.GetAttribute("status") == FeatureStatus.Live);

			// Force any live developments with the same service ID to be redeveloped
			foreach (var liveDev in developmentNodes)
			{
				appTerminator.TerminateApp(liveDev, CommandTypes.RedevelopService);
			}


			CreateStartService(serviceId, productId,
				new List<AttributeValuePair> { new AttributeValuePair("target_status", FeatureStatus.Dev), new AttributeValuePair("extract_incidents", true) });
		}

		void CreateStartService(string serviceId, string productId, List<AttributeValuePair> additionalAttrs)
		{
			var serviceName =
				model.GetNamedNode("New Services")
					.GetChildWithAttributeValue("round", round.ToString())
					.GetChildWithAttributeValue("service_id", serviceId)
					.GetAttribute("name");

			var attrs = new List<AttributeValuePair>
											 {
												 new AttributeValuePair("type", "StartService"),
												 new AttributeValuePair("service_name", serviceName),
												 new AttributeValuePair("product_id", productId),
												 new AttributeValuePair("unique_id", serviceIdGenerator.GetNextId())
											 };
			if (additionalAttrs != null)
			{
				attrs.AddRange(additionalAttrs);
			}

			new Node(startServicesNode, "StartService", "", attrs);
		}

		string StartService(string serviceId, string productId, int uniqueId, List<AttributeValuePair> attrs = null, bool forceZeroCost = false, bool isAutoInstalled = false)
		{
			var serviceNode = newServicesRound.GetChildWithAttributeValue("service_id", serviceId);
			var productNode = serviceNode.GetChildWithAttributeValue("product_id", productId);
			var metricAdjustments = productNode.GetFirstChildOfType("metric_adjustments");

			var metricNodes = metricAdjustments.GetChildrenAsList();

			var metricAttrs = new List<AttributeValuePair>();

			foreach (var metricAdjustment in metricNodes)
			{
				var metricName = metricAdjustment.GetAttribute("metric_name");
				metricAttrs.Add(new AttributeValuePair($"{metricName}_change_actual", metricAdjustment.GetIntAttribute("actual", -1)));
				metricAttrs.Add(new AttributeValuePair($"{metricName}_change_expected", metricAdjustment.GetIntAttribute("expected", -1)));
			}

			var customerComplaintFixes = productNode.GetFirstChildOfType("customer_complaint_adjustments");
			var customerComplaintPredictedFixes = productNode.GetFirstChildOfType("customer_complaint_predicted_adjustments");
			var complaintAttrs = new List<AttributeValuePair> ();
			if ((customerComplaintFixes != null)
				&& ! productNode.GetBooleanAttribute("is_prototype", false))
			{
				foreach (Node customerComplaintFix in customerComplaintFixes.getChildren())
				{
					var type = customerComplaintFix.GetAttribute("complaint_type");
					var customerType = customerComplaintFix.GetIntAttribute("customer_type", 0);

					var effect = customerComplaintFix.GetBooleanAttribute("is_best_in_class", false) ? "best" : (customerComplaintFix.GetBooleanAttribute("is_fix", true) ? "fixed" : "broken");
					complaintAttrs.Add(new AttributeValuePair($"customer_complaint_{type}{customerType}_{effect}", true));
				}

				foreach (Node customerComplaintPredictedFix in customerComplaintPredictedFixes.getChildren())
				{
					var type = customerComplaintPredictedFix.GetAttribute("complaint_type");
					var customerType = customerComplaintPredictedFix.GetIntAttribute("customer_type", 0);

					bool reallyDoesFix = false;
					var realEffect = complaintAttrs.SingleOrDefault(c => c.Attribute.StartsWith($"customer_complaint_{type}{customerType}"));
					// TODO should the || not be "fixed" or "best" ?? 
					if ((realEffect != null) && (realEffect.Attribute.EndsWith("fixed") || realEffect.Attribute.EndsWith("broken")))
					{
						reallyDoesFix = true;
					}

					if (! reallyDoesFix)
					{
						complaintAttrs.Add(new AttributeValuePair ($"customer_complaint_{type}{customerType}_false_predicted_fix", true));
					}
				}
			}

			var defaultAttrs = new List<AttributeValuePair>
													{
                                                        // Attributes from Service node
                                                        new AttributeValuePair("type", "BeginNewServicesInstall"),
														new AttributeValuePair("up", true),
														new AttributeValuePair("service_id", serviceId),
														new AttributeValuePair("product_id", productId),
														new AttributeValuePair("status", FeatureStatus.Dev),
														new AttributeValuePair("bladeCost",
															serviceNode.GetIntAttribute("bladeCost", 0)),
														new AttributeValuePair("round", round),
														new AttributeValuePair("data_security_level",
															productNode.GetAttribute("data_security_level")),
														new AttributeValuePair("is_prototype",
															productNode.GetBooleanAttribute("is_prototype", false)),
														new AttributeValuePair("has_impact",
															serviceNode.GetAttribute("has_impact")),
														new AttributeValuePair("notinnetwork",
															serviceNode.GetAttribute("notinnetwork")),
														new AttributeValuePair("transaction_type",
															serviceNode.GetAttribute("transaction_type")),
														new AttributeValuePair("biz_service_function",
															serviceNode.GetAttribute("biz_service_function")),
														new AttributeValuePair("slalimit",
															serviceNode.GetAttribute("slalimit")),
														new AttributeValuePair("gantt_order",
															serviceNode.GetAttribute("gantt_order")),
														new AttributeValuePair("desc", serviceNode.GetAttribute("desc")),
														new AttributeValuePair("shortdesc",
															serviceNode.GetAttribute("shortdesc")),
														new AttributeValuePair("icon", serviceNode.GetAttribute("icon")),
														new AttributeValuePair("stores",
															serviceNode.GetAttribute("stores")),

														new AttributeValuePair("is_third_party",
															serviceNode.GetAttribute("is_third_party")),
														new AttributeValuePair("version", serviceNode.GetAttribute("version")),
														new AttributeValuePair ("problem_statement", serviceNode.GetIntAttribute("problem_statement", 0)),
														new AttributeValuePair ("problem_statement_column", serviceNode.GetIntAttribute("problem_statement_column", 0)),

                                                        // Time
                                                        new AttributeValuePair("time_started",
															model.GetNamedNode("CurrentTime")
															.GetIntAttribute("seconds", 0)),

                                                        // Attributes from Product node
                                                        new AttributeValuePair("business_investment",
															productNode.GetAttribute("business_investment")),
														new AttributeValuePair("test_environment",
															productNode.GetAttribute("test_environment")),

														new AttributeValuePair("test_time",
															productNode.GetAttribute("test_time")),
														new AttributeValuePair("default_test_time",
															productNode.GetAttribute("test_time")),
														new AttributeValuePair("server",
															productNode.GetAttribute("server_name")),
														new AttributeValuePair("platform",
															productNode.GetAttribute("platform")),
														new AttributeValuePair("release_code",
															productNode.GetAttribute("release_code")),
														new AttributeValuePair("cpu_req",
															productNode.GetAttribute("cpu_req")),
														new AttributeValuePair("failure_remark",
															productNode.GetAttribute("failure_remark")),
														new AttributeValuePair("app",
															productNode.GetAttribute("app_name")),
														new AttributeValuePair("make_unbreakable",
															productNode.GetAttribute("make_unbreakable")),

                                                        // Product node children
                                                        new AttributeValuePair("dev_one_environment",
															productNode.GetChildWithAttributeValue("dev_team", "1")
															.GetAttribute("build_environment")),
														new AttributeValuePair("dev_two_environment",
															productNode.GetChildWithAttributeValue("dev_team", "2")
															.GetAttribute("build_environment")),
                                                        // Additional attributes
                                                        new AttributeValuePair("dev_one_selection", ""),
														new AttributeValuePair("dev_two_selection", ""),
														new AttributeValuePair("test_environment_selection", ""),
														new AttributeValuePair("release_selection", ""),
														new AttributeValuePair("enclosure_selection", ""),
														new AttributeValuePair("optimal", true),
														new AttributeValuePair("has_passed_stage", true),
														new AttributeValuePair("dev_stage_status", StageStatus.Incomplete),
														new AttributeValuePair("test_stage_status", StageStatus.Incomplete),
														new AttributeValuePair("release_stage_status", StageStatus.Incomplete),
														new AttributeValuePair("deployment_stage_status", StageStatus.Incomplete),

														new AttributeValuePair("uac_colour", productNode.GetAttribute("uac_colour")),
														new AttributeValuePair("uac_shape", productNode.GetAttribute("uac_shape"))
													};

			if (productNode.GetBooleanAttribute("is_prototype", false))
			{
				defaultAttrs.Add(new AttributeValuePair ("delayRemaining", 5));
			}

			var prerequisiteFeatureId = serviceNode.GetAttribute("prerequisite_feature_id");
			if (! string.IsNullOrEmpty(prerequisiteFeatureId))
			{
				defaultAttrs.Add(new AttributeValuePair("prerequisite_feature_id", prerequisiteFeatureId));
			}

			defaultAttrs.AddRange(metricAttrs);
			defaultAttrs.AddRange(complaintAttrs);

			var level2StageRemoved = serviceNode.GetAttribute("level_2_stage_removed");
			if (! string.IsNullOrEmpty(level2StageRemoved))
			{
				defaultAttrs.Add(new AttributeValuePair ("level_2_stage_removed", level2StageRemoved));
			}

			if (attrs != null)
			{
				defaultAttrs.AddRange(attrs);
			}

			var beginServiceName = BuildUniqueBeginServiceName(serviceNode, round, uniqueId);

			var newService = new Node(beginBizServicesHead, "BeginNewServicesInstall", beginServiceName, defaultAttrs);

			if (!forceZeroCost)
			{
				var serviceCost = productNode.GetIntAttribute("business_investment", 0);

				LogCost(serviceNode.GetAttribute("biz_service_function"), "Development", serviceCost);

				//AdjustBudget(-serviceCost);
			}

			if (! isAutoInstalled)
			{
				var platform = productNode.GetAttribute("platform");

				if (productNode.GetBooleanAttribute("is_optimal_product", false))
				{
					LogSuccess(newService, serviceNode.GetAttribute("biz_service_function"), "product-optimal",
						serviceId + " " + platform, null);
				}
				else
				{
					LogError(newService, serviceNode.GetAttribute("biz_service_function"), serviceNode.GetAttribute("shortdesc"),
						"product", Guid.NewGuid().ToString(), "Suboptimal financial choice", "product-suboptimal",
						serviceId + " " + platform, null);
				}

				var remarks = newService.GetAttribute("failure_remark");
				if (! string.IsNullOrEmpty(remarks))
				{
					LogSuccess(newService, serviceNode.GetAttribute("biz_service_function"), "notes", remarks, null);
				}
			}

			return beginServiceName;
		}

		static string BuildUniqueBeginServiceName(Node serviceNode, int round, int uniqueId)
		{
			return $"Begin {serviceNode.GetAttribute("biz_service_function")} {round}_{uniqueId:0000}";
		}

		string GetUniqueBeginServiceTag(Node newService)
		{
			var parts = newService.GetAttribute("name").Split(' ');
			return parts[parts.Length - 1];
		}

		void LogCost(string serviceName, string description, int cost)
		{
			new Node(model.GetNamedNode("CostedEvents"), "cost", "", new ArrayList(new[]
			{
				new AttributeValuePair ("type", "cost"),
				new AttributeValuePair ("cost", cost),
				new AttributeValuePair ("desc", description),
				new AttributeValuePair ("service_name", serviceName)
			}));
		}

		public bool IsPlatformSupportedForProduct(string productId)
		{
			var project = ((Node[])(model.GetNodesWithAttributeValue("product_id", productId)).ToArray(typeof(Node)))
				.First(n => ((n.GetAttribute("type") == "new_service") && (n.Parent.GetIntAttribute("round", 0) == round)));

			return IsPlatformSupported(project.GetAttribute("platform"));
		}

		public bool IsPlatformSupported(string platform)
		{
			return model.GetNamedNode("SupportedPlatforms").GetChildrenAsList().
				Any(supportedPlatform => supportedPlatform.GetAttribute("desc") == platform);
		}

		// TODO ignore the commented out code for now. Trying to extract some of the logic from the InstallNewService method so it's less cluttered
		//  bool CanDeployService (Node service, bool isAutoDeploying)
		//  {
		//   var serviceName = service.GetAttribute("biz_service_function");
		//var servicePlatform = service.GetAttribute("platform");
		//var isServicePlatformSupported =
		//    model.GetNamedNode("SupportedPlatforms").GetChildrenAsList().
		//	    Any(supportedPlatform => supportedPlatform.GetAttribute("desc") == servicePlatform);

		//   var hasFailed = !isServicePlatformSupported;

		//   if (hasFailed)
		//   {
		//    var failureMessage = $"Fail- {service.GetAttribute("failure_remark")}";

		//    service.SetAttributes(new List<AttributeValuePair>
		//    {
		//	    new AttributeValuePair("install_feedback_message", failureMessage),
		//	    new AttributeValuePair("feedback_image", FeedbackImageName.Cross),
		//	    new AttributeValuePair("is_install_successful", false),
		//	    new AttributeValuePair("deployment_stage_status", StageStatus.Failed),
		//	    new AttributeValuePair("can_deploy", false)
		//    });

		//	LogError(service, serviceName, serviceName, "deploy", Guid.NewGuid().ToString(), failureMessage, "deploy-failure", servicePlatform, null);

		//    if (!isAutoDeploying)
		//    {
		//	    LogAppStageFailure(service, $"{serviceName} - Deployment Stage", StageFailureMessage.DeploymentStage);
		//    }
		//	return false;
		//   }


		//   var enclosureSelected = service.GetAttribute("enclosure_selection");
		//   var enclosureNode = model.GetNamedNode(enclosureSelected);

		//   var isOptimal = (service.GetAttribute("enclosure_selection") == service.GetAttribute("server"))
		//                   || enclosureNode.GetBooleanAttribute("always_optimal", false);

		//   var feedbackMessage = "Successful Installation- Optimum Enclosure";
		//   var gain = service.GetIntAttribute("gain_per_minute", 0);

		//   var serviceEffectiveness = (float)service.GetIntAttribute("effectiveness_percent", 100);

		//if (isOptimal)
		//{
		//	if (!service.GetBooleanAttribute("is_auto_installed", false))
		//	{
		//		LogSuccess(service, serviceName, "deploy-optimal-enclosure", service.GetAttribute("enclosure_selection"), null);
		//	}
		//}
		//else
		//{
		//	serviceEffectiveness = model.GetNamedNode("TestEnvironments").GetIntAttribute("run_bad_effectiveness_percent", 0) / 100f * serviceEffectiveness;
		//	feedbackMessage = "Successful Installation - Suboptimal Enclosure";

		//	gain = (int)(gain * (serviceEffectiveness / 100f));

		//	var optimalGain = service.GetIntAttribute("optimum_gain_per_minute", 0);
		//	var effectiveness = (optimalGain == 0) ? 0f : (gain / (float)optimalGain) * 100;

		//	if (!service.GetBooleanAttribute("is_auto_installed", false))
		//	{
		//		LogError(service, serviceName, serviceName, "deploy", Guid.NewGuid().ToString(), $"Running at {(int)effectiveness}% effectiveness.", "deploy-suboptimal-enclosure", service.GetAttribute("enclosure_selection"), (int)serviceEffectiveness);
		//	}
		//}

		//var cpuReq = service.GetBooleanAttribute("force_zero_cpu_usage", false) ? 0 : service.GetIntAttribute("cpu_req", 0);

		//   var isPrototypeService = service.GetBooleanAttribute("is_prototype", false);




		//// Scenario 1, the service being installed is an MVP therefore it's not using a 'live' enclosure
		//// and it's not replacing the existing service yet, so there's no need to adjust the CPU usage

		//// Scenario 2, the service being installed is an upgrade for the existing service, so the previous
		//// CPU usage can be reclaimed from the current enclosure

		//var bizServiceNode = model.GetNamedNode(serviceName);
		//var appName = service.GetAttribute("app") + (isPrototypeService ? " MVP" : "");

		//   var appNode = model.GetNamedNode(appName);

		//   if (! isPrototypeService)
		//   {
		//	// Need to reclaim previous CPU usage before checking
		//	// there's sufficient CPU in the new enclosure 

		//   }


		//return true;
		//  }
		void LogPrerequisiteError (string dependentFeatureId, string prerequisiteFeatureId)
		{
			model.GetNamedNode("AppDevelopmentStageFailures").CreateChild("prerequisite_error", "",
				new List<AttributeValuePair>
				{
					new AttributeValuePair("dependent_feature", dependentFeatureId),
					new AttributeValuePair("prerequisite_feature", prerequisiteFeatureId)
				});
		}

		void LogHeatMapPrerequisiteError (Node service)
		{
			var featureId = service.GetAttribute("service_id");
			var serviceName = service.GetAttribute("biz_service_function");
			var attributes = new List<AttributeValuePair>
			{
				new AttributeValuePair("type", "NS_error"),
				new AttributeValuePair("unique_id", GetUniqueBeginServiceTag(service)),
				new AttributeValuePair("product_id", service.GetAttribute("product_id")),
				new AttributeValuePair("service_name", service.GetAttribute("name")),
				new AttributeValuePair("shortdesc", service.GetAttribute("name")),
				new AttributeValuePair("error_type", "heat_map_prerequisite"),
				new AttributeValuePair("error_full_type", "heat_map_prerequisite"),
				new AttributeValuePair("error_message",
					$"Feature {featureId} can't be deployed because the heatmap prerequisites are not met"),
				new AttributeValuePair("error_guid", Guid.NewGuid().ToString()),
				new AttributeValuePair("error_details", ""),
				new AttributeValuePair("is_prototype", service.GetBooleanAttribute("is_prototype", false)),
				new AttributeValuePair("notes", service.GetAttribute("failure_remark"))
			};

			model.GetNamedNode("AppDevelopmentStageFailures").CreateChild("NS_error", "", attributes);
			new Node(model.GetNamedNode("CostedEvents"), "NS_error", "", attributes);
		}

		void InstallNewService (Node service, bool isAutoDeploying = false)
		{
			var serviceName = service.GetAttribute("biz_service_function");
			var enclosureSelected = service.GetAttribute("enclosure_selection");
			var enclosureNode = model.GetNamedNode(enclosureSelected);
			var servicePlatform = service.GetAttribute("platform");
			var isPrototypeService = service.GetBooleanAttribute("is_prototype", false);

			var isServicePlatformSupported =
				model.GetNamedNode("SupportedPlatforms").GetChildrenAsList().
					Any(supportedPlatform => supportedPlatform.GetAttribute("desc") == servicePlatform);

			var hasFailed = !isServicePlatformSupported;
			if (hasFailed)
			{
				var failureMessage = $"Fail- {service.GetAttribute("failure_remark")}";

				service.SetAttributes(new List<AttributeValuePair>
												  {
													  new AttributeValuePair("install_feedback_message", failureMessage),
													  new AttributeValuePair("feedback_image", FeedbackImageName.Cross),
													  new AttributeValuePair("is_install_successful", false),
													  new AttributeValuePair("deployment_stage_status", StageStatus.Failed),
													  new AttributeValuePair("can_deploy", false)
												  });

				LogError(service, serviceName, serviceName, "deploy", Guid.NewGuid().ToString(), failureMessage, "deploy-failure", servicePlatform, null);

				if (!isAutoDeploying)
				{
					LogAppStageFailure(service, $"{serviceName} - Deployment Stage", StageFailureMessage.DeploymentStage);
				}

				return;
			}

			if (! isPrototypeService)
			{
				var prerequisiteFeatureId = service.GetAttribute("prerequisite_feature_id");
				if (!string.IsNullOrEmpty(prerequisiteFeatureId))
				{
					var roundCompletedServices = model.GetNamedNode("CompletedNewServices")
						.GetChildWithAttributeValue("round", round.ToString());
					
					var fullProduct = roundCompletedServices.GetChildrenAsList().FirstOrDefault(cs =>
						cs.GetAttribute("service_id") == prerequisiteFeatureId &&
						!cs.GetBooleanAttribute("is_prototype", false));

					if (fullProduct == null)
					{
						var failureMessage = $"Fail- Prerequisite feature {prerequisiteFeatureId} not deployed";

						service.SetAttributes(new List<AttributeValuePair>
						{
							new AttributeValuePair("install_feedback_message", failureMessage),
							new AttributeValuePair("feedback_image", FeedbackImageName.Cross),
							new AttributeValuePair("is_install_successful", false),
							new AttributeValuePair("deployment_stage_status", StageStatus.Failed),
							new AttributeValuePair("can_deploy", false)
						});

						LogError(service, serviceName, serviceName, "deploy", Guid.NewGuid().ToString(), failureMessage, "deploy-failure", servicePlatform, null);
						LogPrerequisiteError(service.GetAttribute("service_id"), prerequisiteFeatureId);

						if (!isAutoDeploying)
						{
							LogAppStageFailure(service, $"{serviceName} - Deployment Stage", StageFailureMessage.DeploymentStage);

							var featureId = service.GetAttribute("service_id");

							new Node(model.GetNamedNode("FacilitatorNotifiedErrors"), "error", "", new List<AttributeValuePair>
							{
								new AttributeValuePair ("title", "Deployment Error"),
								new AttributeValuePair ("text", $"Feature {featureId} requires {prerequisiteFeatureId}"),
								new AttributeValuePair ("sound", "ding.wav")
							});
						}

						return;
					}
				}

				if (! heatMapMaintainer.AreFeaturePrerequisitesMet(service))
				{
					var failureMessage = $"Fail- Heatmap prerequisites not met";

					service.SetAttributes(new List<AttributeValuePair>
					{
						new AttributeValuePair("install_feedback_message", failureMessage),
						new AttributeValuePair("feedback_image", FeedbackImageName.Cross),
						new AttributeValuePair("is_install_successful", false),
						new AttributeValuePair("deployment_stage_status", StageStatus.Failed),
						new AttributeValuePair("can_deploy", false)
					});

					LogError(service, serviceName, serviceName, "deploy", Guid.NewGuid().ToString(), failureMessage, "deploy-failure", servicePlatform, null);
					LogHeatMapPrerequisiteError(service);

					if (!isAutoDeploying)
					{
						LogAppStageFailure(service, $"{serviceName} - Deployment Stage", StageFailureMessage.DeploymentStage);

						var featureId = service.GetAttribute("service_id");

						new Node(model.GetNamedNode("FacilitatorNotifiedErrors"), "error", "", new List<AttributeValuePair>
						{
							new AttributeValuePair ("title", "Deployment Error"),
							new AttributeValuePair ("text", $"The heatmap is not ready for feature {featureId}"),
							new AttributeValuePair ("sound", "ding.wav")
						});
					}

					return;
				}
			}

			var isOptimal = (service.GetAttribute("enclosure_selection") == service.GetAttribute("server"))
							 || enclosureNode.GetBooleanAttribute("always_optimal", false);

			var feedbackMessage = "Successful Installation- Optimum Enclosure";

			var serviceEffectiveness = (float)service.GetIntAttribute("effectiveness_percent", 100);

			if (isOptimal)
			{
				if (!service.GetBooleanAttribute("is_auto_installed", false))
				{
					LogSuccess(service, serviceName, "deploy-optimal-enclosure", service.GetAttribute("enclosure_selection"), null);
				}
			}
			else
			{
				serviceEffectiveness = model.GetNamedNode("TestEnvironments").GetIntAttribute("run_bad_effectiveness_percent", 0) / 100f * serviceEffectiveness;
				feedbackMessage = "Successful Installation - Suboptimal Enclosure";

				if (!service.GetBooleanAttribute("is_auto_installed", false))
				{
					LogError(service, serviceName, serviceName, "deploy", Guid.NewGuid().ToString(), $"Running at {(int)serviceEffectiveness}% effectiveness.", "deploy-suboptimal-enclosure", service.GetAttribute("enclosure_selection"), (int)serviceEffectiveness);
				}
			}



			var cpuReq = service.GetIntAttribute("cpu_req", 0);
			if (service.GetBooleanAttribute("force_zero_cpu_usage", false))
			{
				cpuReq = 0;
			}

			// Create the biz service
			var bizServiceName = service.GetAttribute("biz_service_function");

			var transactionType = service.GetAttribute("transaction_type");



			var bizServiceNode = model.GetNamedNode(bizServiceName);



			// Scenario 1, the service being installed is an MVP therefore it's not using a 'live' enclosure
			// and it's not replacing the existing service yet, so there's no need to adjust the CPU usage

			// Scenario 2, the service being installed is an upgrade for the existing service, so the previous
			// CPU usage can be reclaimed from the current enclosure


			var appName = service.GetAttribute("app") + (isPrototypeService ? " MVP" : "");

			var appNode = model.GetNamedNode(appName);

			if (!isPrototypeService && bizServiceNode != null)
			{
				var previousCpuReq = bizServiceNode.GetIntAttribute("cpu_req", 0);

				if (appNode != null)
				{
					var previousInstallEnclosure = appNode.Parent;
					previousInstallEnclosure.IncrementIntAttribute("free_cpu", previousCpuReq, 0);
					previousInstallEnclosure.IncrementIntAttribute("used_cpu", -previousCpuReq, 0);
				}
			}


			// Modify capacity of enclosure
			var freeCpu = enclosureNode.GetIntAttribute("free_cpu");
			var usedCpu = enclosureNode.GetIntAttribute("used_cpu");

			Debug.Assert(freeCpu != null && usedCpu != null, "Either free or used CPU is missing from enclosure");

			if (freeCpu < cpuReq)
			{
				var bladeNode = model.GetNamedNode("blade");

				var numCpuPerBlade = bladeNode.GetIntAttribute("cpu", 15);

				var numBladesRequired = (int)Math.Ceiling((cpuReq - freeCpu.Value) / (double)numCpuPerBlade);

				var freeHeight = enclosureNode.GetIntAttribute("free_height", 0);

				if (freeHeight < numBladesRequired)
				{
					// Capacity is no longer available due to 'race conditions'
					// deriving from enqueueing services to be installed.
					// Need to fail the installation and display feedback
					// TODO

					const string failureMessage = "Fail- Insufficient capacity available";

					service.SetAttributes(new List<AttributeValuePair>
					{
						new AttributeValuePair("install_feedback_message", failureMessage),
						new AttributeValuePair("feedback_image", FeedbackImageName.Cross),
						new AttributeValuePair("is_install_successful", false),
						new AttributeValuePair("deployment_stage_status", StageStatus.Failed),
						new AttributeValuePair("can_deploy", false)
					});

					LogError(service, serviceName, serviceName, "deploy", Guid.NewGuid().ToString(), failureMessage, "deploy-failure", servicePlatform, null);

					if (!isAutoDeploying)
					{
						LogAppStageFailure(service, $"{serviceName} - Deployment Stage", StageFailureMessage.DeploymentStage);
					}


					return;
				}

				freeHeight -= numBladesRequired;

				enclosureNode.SetAttribute("free_height", freeHeight);

				freeCpu += numBladesRequired * numCpuPerBlade;

				var bladeCost = bladeNode.GetIntAttribute("cost", 0);

				bladeCost *= numBladesRequired;

				LogCost(serviceName, "Blade", bladeCost);
				AdjustBudget(-bladeCost);
			}

			enclosureNode.SetAttributes(new List<AttributeValuePair>
			{
				new AttributeValuePair("free_cpu", freeCpu.Value - cpuReq),
				new AttributeValuePair("used_cpu", usedCpu.Value + cpuReq)
			});

			Debug.Assert(bizServiceNode != null);

			ApplyMetricAdjustments(service, bizServiceNode, serviceEffectiveness);

			var level2StageRemoved = service.GetAttribute("level_2_stage_removed");
			if ((! string.IsNullOrEmpty(level2StageRemoved))
				&& (! service.GetBooleanAttribute("is_prototype", false)))
			{
				var level1Stage = model.GetNamedNode("Stages").GetChildWithAttributeValue("desc", bizServiceName);
				var level2Stage = level1Stage.GetChildWithAttributeValue("level_2_stage", level2StageRemoved);
				level2Stage.SetAttribute("is_disabled", true);
			}

			UpdateServiceCustomerComplaints(bizServiceNode, service);



			service.SetAttributes(new List<AttributeValuePair>
			{
				new AttributeValuePair("status", FeatureStatus.Live),
				new AttributeValuePair("enclosure", enclosureSelected),
				new AttributeValuePair("install_feedback_message", feedbackMessage),
				new AttributeValuePair("feedback_image", FeedbackImageName.Tick),

				new AttributeValuePair("optimal", isOptimal),
				new AttributeValuePair("is_install_successful", true),
				//new AttributeValuePair("can_deploy", false),
				new AttributeValuePair("deployment_stage_status", StageStatus.Completed)
			});

			OnFeatureGoneLive(service);

			LogSuccess(service, serviceName, "deploy-final-effectiveness", "", (int)serviceEffectiveness);

			var upgradeableAttributes = new List<AttributeValuePair>
			{
		        //new AttributeValuePair("product_id", service.GetAttribute("product_id")),
		        //new AttributeValuePair("version", service.GetAttribute("version")),
		        //new AttributeValuePair("effectiveness_percentage", (int) serviceEffectiveness),
		        new AttributeValuePair("cpu_req", cpuReq),
		        //new AttributeValuePair("is_prototype", service.GetBooleanAttribute("is_prototype", false)),
				//new AttributeValuePair("uac_shape", service.GetAttribute("uac_shape")),
				//new AttributeValuePair("uac_colour", service.GetAttribute("uac_colour")),
				//new AttributeValuePair("platform", service.GetAttribute("platform")),
		        //new AttributeValuePair("service_id", service.GetAttribute("service_id"))
			};


			bizServiceNode.SetAttributes(upgradeableAttributes);


			if (appNode == null)
			{
				Debug.Assert(isPrototypeService);
				appNode = new Node(enclosureNode, "App", appName,
					new List<AttributeValuePair>
					{
						new AttributeValuePair("type", "App"),
						new AttributeValuePair("desc", appName),
						new AttributeValuePair("fixable", true),
						new AttributeValuePair("canupgrade", true),
						new AttributeValuePair("userupgrade", false),
						new AttributeValuePair("up", true),
						new AttributeValuePair("proczone", 1),
						new AttributeValuePair("zone", 1),
						new AttributeValuePair("usernode", false),
						new AttributeValuePair("propagate", true),
						new AttributeValuePair("version", service.GetAttribute("version")),
						new AttributeValuePair("round", round)
					});
			}
			else
			{
				if (appNode.Parent != enclosureNode)
				{
					enclosureNode.AddChild(appNode);
				}
			}

			appNode.SetAttribute("platform", service.GetAttribute("platform"));

			if (!isPrototypeService)
			{
				// Create the biz service users
				var bizName = SkinningDefs.TheInstance.GetData("biz");


				foreach (var storeName in service.GetAttribute("stores").Split(',').Select(s => $"{bizName} {s}"))
				{
					var bsuName = $"{storeName} {bizServiceName}";

					var storeNode = model.GetNamedNode(storeName);

					if (model.GetNamedNode(bsuName) == null)
					{
						new Node(storeNode, "biz_service_user", bsuName,
							new List<AttributeValuePair>
							{
							new AttributeValuePair("type", "biz_service_user"),
							new AttributeValuePair("has_impact",true),
							new AttributeValuePair("up",true),
							new AttributeValuePair("slalimit", 360),
							new AttributeValuePair("slabreach", false),
							new AttributeValuePair("slaimpact", 0),
							new AttributeValuePair("canworkaround", false),
							new AttributeValuePair("biz_service_function", bizServiceName),
							new AttributeValuePair("transaction_type",transactionType),
							new AttributeValuePair("desc",bsuName),
							new AttributeValuePair("shortdesc",service.GetAttribute("shortDesc")),
							new AttributeValuePair("version",service.GetAttribute("version")),
							new AttributeValuePair("icon",service.GetAttribute("icon")),
							new AttributeValuePair("is_hidden_in_reports", service.GetBooleanAttribute("is_hidden_in_reports", false))
						});
					}
					// Adjust the store's bonuses 
					// Subtract the previous gain (non-zero if the service is being redeveloped)

					// Create the App to BSU links
					var appLinkName = $"{appName} {bsuName} Connection";

					if (model.GetNamedNode(appLinkName) == null)
					{
						new LinkNode(appNode, "Connection", appLinkName,
							new List<AttributeValuePair>
							{
							new AttributeValuePair("type", "Connection"),
							new AttributeValuePair("to", bsuName)
							});
					}

					// Create the App to BSU links
					var bsuLinkName = $"BS {bsuName} Connection";

					var bsuLink = model.GetNamedNode(bsuLinkName);
					if (bsuLink == null)
					// Link with biz service
					{
						new LinkNode(bizServiceNode, "Connection", bsuLinkName,
							new List<AttributeValuePair>
							{
							new AttributeValuePair("type", "Connection"),
							new AttributeValuePair("contype", bizServiceName),
							new AttributeValuePair("to", bsuName)
							});
					}
					else
					{
						if (bsuLink.Parent != bizServiceNode)
						{
							model.MoveNode(bsuLink, bizServiceNode);
						}
					}
				}

			}

			
			ReplaceFeatures(service.GetAttribute("service_id"));

			// Add installed service to Completed New Services node
			var completedName = service.GetAttribute("name").Replace("Begin", "Completed");

			new Node(completedFeaturesRoundNode, "CompletedService", completedName,
				new List<AttributeValuePair>
				{
				   new AttributeValuePair("biz_service_function", bizServiceName),
				   new AttributeValuePair("service_id", service.GetAttribute("service_id")),
				   new AttributeValuePair("revenue_made", 0),
					new AttributeValuePair("investment_cost", service.GetAttribute("business_investment")),
				   new AttributeValuePair("transaction_type", service.GetAttribute("transaction_type")),
				   new AttributeValuePair("profit", -CONVERT.ParseInt(service.GetAttribute("business_investment"))),
				   new AttributeValuePair("is_hidden_in_reports", service.GetBooleanAttribute("is_hidden_in_reports", false)),
				   new AttributeValuePair("is_auto_installed", service.GetBooleanAttribute("is_auto_installed", false)),
				   // TODO need to check where these are used to replace them
                   new AttributeValuePair("gain_per_minute", -2),
				   new AttributeValuePair("target_gain", -2),
				   new AttributeValuePair("effectiveness", serviceEffectiveness),
				   new AttributeValuePair("parent_service_number", service.GetAttribute("parent_service_number")),
				   new AttributeValuePair("service_group", service.GetAttribute("service_group")),
				   new AttributeValuePair("is_prototype", service.GetBooleanAttribute("is_prototype", false)),
				   new AttributeValuePair("product_id", service.GetAttribute("product_id")),
				   new AttributeValuePair("shortdesc",service.GetAttribute("shortDesc")),
				   new AttributeValuePair("version",service.GetAttribute("version")),
					new AttributeValuePair("status", "Live"),
					new AttributeValuePair("time_completed", model.GetNamedNode("CurrentTime").GetIntAttribute("seconds", 0))
				});



			// You seem to be unbreakable Mr Bond.
			if (!string.IsNullOrEmpty(service.GetAttribute("make_unbreakable")))
			{
				var app = model.GetNamedNode(service.GetAttribute("make_unbreakable"));
				if (app != null)
				{
					string incidentId = null;
					foreach (LinkNode link in app.GetChildrenOfType("Connection"))
					{
						var linkIncident = link.GetAttribute("incident_id");
						if (!string.IsNullOrEmpty(linkIncident))
						{
							incidentId = linkIncident;
							break;
						}
					}

					if (!string.IsNullOrEmpty(incidentId))
					{
						new Node(model.GetNamedNode("enteredIncidents"), "IncidentNumber", "", new AttributeValuePair("id", incidentId + "_fix_confirmed"));
					}

					app.SetAttribute("unbreakable", true);
				}
			}

			if (! isPrototypeService)
			{
				SetPrerequisiteMet(service.GetAttribute("service_id"));

				var featureId = service.GetAttribute("service_id");

				var failedBeginNodes = beginBizServicesHead.GetChildrenAsList().Where(bs =>
					bs.GetAttribute("biz_service_function") == serviceName &&
					(bs.GetAttribute("service_id") != featureId || bs != service) &&
					bs.GetAttribute(StageAttribute.DeploymentStage) == StageStatus.Failed);

				foreach (var failedBeginService in failedBeginNodes)
				{
					if (heatMapMaintainer.AreFeaturePrerequisitesMet(failedBeginService))
					{
						failedBeginService.SetAttribute(StageAttribute.DeploymentStage, StageStatus.Incomplete);
					}
				}

			}
		}

		void ApplyMetricAdjustments (Node service, Node bizServiceNode, float serviceEffectiveness)
		{
			var featureId = service.GetAttribute("service_id");
			RemoveMetricAdjustments(bizServiceNode, featureId);

			var metricNodes = bizServiceNode.GetFirstChildOfType("metrics").GetChildrenAsList();
			var metricNames = metricNodes.Select(n => n.GetAttribute("metric_name"));

			var metricAdjustmentsDictionary = metricNames.ToDictionary(m => m, m =>
				new
				{
					Expected = service.GetIntAttribute($"{m}_change_expected", 0),
					Actual = (int) Math.Ceiling(service.GetIntAttribute($"{m}_change_actual", 0) * serviceEffectiveness / 100f)
				});

			foreach (var metricNode in metricNodes)
			{
				var metricName = metricNode.GetAttribute("metric_name");

				var adjustment = metricAdjustmentsDictionary[metricName].Actual;

				metricNode.IncrementIntAttribute("value", adjustment, 0);
				service.SetAttributeIfNotEqual($"{metricName}_change_actual", adjustment);

				new Node (bizServiceNode, "metric_adjustment", $"MetricAdjustment_{featureId}_{metricName}", new List<AttributeValuePair>
				{
					new AttributeValuePair ("type", "metric_adjustment"),
					new AttributeValuePair ("feature", featureId),
					new AttributeValuePair ("metric", metricName),
					new AttributeValuePair ("change", adjustment)
				});
			}
		}

		void RemoveMetricAdjustments (Node service, string featureId)
		{
			var adjustmentsToRemove = new List<Node> ();
			var metricNodes = service.GetFirstChildOfType("metrics").GetChildrenAsList();

			foreach (var metric in service.GetChildrenOfTypeAsList("metric_adjustment"))
			{
				if (metric.GetAttribute("feature") == featureId)
				{
					var metricName = metric.GetAttribute("metric");
					var adjustment = metric.GetIntAttribute("change", 0);

					var metricNode = metricNodes.Single(n => n.GetAttribute("metric_name") == metricName);
					metricNode.IncrementIntAttribute("value", - adjustment, 0);

					adjustmentsToRemove.Add(metric);
				}
			}

			foreach (var metric in adjustmentsToRemove)
			{
				metric.Parent.DeleteChildTree(metric);
			}
		}

		void SetPrerequisiteMet (string prerequisiteFeatureId)
		{
			var completedFeatures = model.GetNamedNode("CompletedNewServices").GetChildrenAsList();

			var developingFeatures = beginBizServicesHead.GetChildrenAsList()
				.Where(n => n.GetAttribute("prerequisite_feature_id") == prerequisiteFeatureId).ToList();

			foreach (var feature in developingFeatures)
			{
				if (completedFeatures.Any(cf => cf.GetAttribute("name") == feature.GetAttribute("name").Replace("Begin", "Completed")))
				{
					continue;
				}

				feature.SetAttribute("prerequisite_met", true);
			}
		}

		void UpdateServiceCustomerComplaints (Node service, Node addedFeature)
		{
			if (! service.GetBooleanAttribute("is_prototype", false))
			{
				heatMapMaintainer.AddFeatureToService(service, addedFeature);
			}
		}

		void OnFeatureGoneLive(Node service)
		{
			FeatureGoneLive?.Invoke(this, service);
		}

		// Adds the amount passed in to the current budget. 
		// If the budget is to be decreased then
		// a negative amount should be passed in.
		void AdjustBudget(int amount)
		{
			var budgetNode = model.GetNamedNode("Budgets").GetChildWithAttributeValue("round", round.ToString());

			budgetNode.IncrementIntAttribute("budget", amount, 0);
		}

		public void AutoInstallServicesAtRoundEnd()
		{
			var autoInstallServices = new List<string>();

			// Get the names of all the new services that need to be auto-installed at the end of the round
			foreach (var newService in model.GetNamedNode("New Services").GetChildWithAttributeValue("round", round.ToString()).GetChildrenAsList())
			{
				if (newService.GetBooleanAttribute("is_installed_at_round_end", false))
				{
					autoInstallServices.Add(newService.GetAttribute("name"));
				}
			}

			foreach (var newServiceName in autoInstallServices)
			{
				AutoInstallService(newServiceName, true);
			}
		}

		void AutoInstallService(string newServiceName, bool isAtRoundEnd)
		{
			var newServiceNode = model.GetNamedNode(newServiceName);
			var bizServiceName = newServiceNode.GetAttribute("biz_service_function");

			// Check if there's a biz service node with this name,
			// if there is then it means the service has already been installed
			var bizServiceNode = model.GetNamedNode(bizServiceName);

			if (bizServiceNode == null)
			{
				// It hasn't been installed yet, so check if 
				// it's currently under development

				var beginServiceNode = model.GetNamedNode(newServiceName.Replace("NS", "Begin"));

				if (beginServiceNode != null)
				{
					// If the service has already been started then try to deploy it
					if (ProgressServiceToDeployment(beginServiceNode, isAtRoundEnd))
					{
						// Has installed correctly so return
						return;
					}

					// If it reaches here then the service failed to install 
					// (they've picked a platform W product) so it needs to be cancelled
					beginServiceNode.SetAttributes(new List<AttributeValuePair>
													   {
														   new AttributeValuePair("status", FeatureStatus.Cancelled)
													   });
				}

				// If it reaches here then the service needs to be started (or restarted)
				var serviceId = newServiceNode.GetAttribute("service_id");
				var productId = GetOptimalProductForService(newServiceNode);

				CreateStartService(serviceId, productId, new List<AttributeValuePair>
												   {
													   new AttributeValuePair("target_status", FeatureStatus.Live),
													   new AttributeValuePair("extract_incidents", false),
													   new AttributeValuePair("force_zero_cpu_usage", true),
													   new AttributeValuePair("force_zero_cost", true),
													   new AttributeValuePair("hide_in_reports", true),
													   new AttributeValuePair("is_auto_installed", true),
													   new AttributeValuePair("is_auto_installed_at_end_of_round", true),
													   new AttributeValuePair("hide_service_icon", true)
												   });
			}
		}

		static string GetOptimalProductForService(Node newService)
		{
			var productId = "";
			foreach (var product in newService.GetChildrenAsList().Where(product => product.GetBooleanAttribute("is_optimal_product", false)))
			{
				productId = product.GetAttribute("product_id");
				break;
			}

			Debug.Assert(!string.IsNullOrEmpty(productId), "Product ID not set.");

			return productId;
		}

		bool ProgressServiceToDeployment(Node beginServiceNode, bool isAtRoundEnd, bool setHideAttributes = true)
		{
			var serviceStatus = beginServiceNode.GetAttribute("status");

			if (serviceStatus == "install" || serviceStatus == FeatureStatus.Live || serviceStatus == FeatureStatus.Redevelop)
			{
				return true;
			}

			if (setHideAttributes)
			{
				beginServiceNode.SetAttribute("is_hidden_in_reports", true);
				beginServiceNode.SetAttribute("hide_service_icon", true);
				beginServiceNode.SetAttribute("is_auto_installed", true);
			}


			if (isAtRoundEnd)
			{
				beginServiceNode.SetAttribute("is_auto_installed_at_end_of_round", true);
			}

			// Service has been started so
			// progress it to being deployed.
			var statuses = new List<string>
							{
								FeatureStatus.Dev,
								FeatureStatus.Test,
								FeatureStatus.TestDelay,
								FeatureStatus.Release,
								"finishedRelease",
								"install",
								FeatureStatus.Live
							};


			var statusIndex = statuses.IndexOf(serviceStatus);

			Debug.Assert(statusIndex >= 0, "Status is in an unrecognised stage.");

			if (statusIndex < statuses.IndexOf(FeatureStatus.Test))
			{
				var devOneBuild = beginServiceNode.GetAttribute("dev_one_environment");
				var devTwoBuild = beginServiceNode.GetAttribute("dev_two_environment");

				beginServiceNode.SetAttributes(new List<AttributeValuePair>
												{
													new AttributeValuePair("dev_one_selection", devOneBuild),
													new AttributeValuePair("dev_two_selection", devTwoBuild),
												});
			}

			if (statusIndex < statuses.IndexOf(FeatureStatus.TestDelay))
			{
				var testEnv = beginServiceNode.GetAttribute("test_environment");

				beginServiceNode.SetAttributes(new List<AttributeValuePair>
												{
													new AttributeValuePair("test_environment_selection", testEnv),
													new AttributeValuePair("skip_test", true)
												});
			}

			if (statusIndex == statuses.IndexOf(FeatureStatus.TestDelay))
			{
				beginServiceNode.SetAttribute("delayRemaining", 0);
			}

			if (statusIndex < statuses.IndexOf(FeatureStatus.Release))
			{
				beginServiceNode.SetAttribute("release_selection", beginServiceNode.GetAttribute("release_code"));
			}

			beginServiceNode.SetAttribute("enclosure_selection", beginServiceNode.GetAttribute("server"));
			InstallNewService(beginServiceNode, true);

			// If a product that can fail has been picked (platform W or DB5)
			// then the service will fail when it's installed
			var hasInstalledSuccessfully = beginServiceNode.GetBooleanAttribute("is_install_successful", true);

			return (hasInstalledSuccessfully);
		}

		public Dictionary<string, bool> GetProductOptimalitiesForService(string serviceId)
		{
			var newService = newServicesRound.GetChildWithAttributeValue("service_id", serviceId);

			var productIdToOptimality = new Dictionary<string, bool>();

			foreach (Node product in newService)
			{
				productIdToOptimality.Add(product.GetAttribute("product_id"), product.GetBooleanAttribute("is_optimal_product", false));
			}

			return productIdToOptimality;
		}

		public Dictionary<string, string> GetProductPlatformsForService(string serviceId)
		{
			var newService = newServicesRound.GetChildWithAttributeValue("service_id", serviceId);

			var productIdToPlatform = new Dictionary<string, string>();

			foreach (Node product in newService)
			{
				productIdToPlatform.Add(product.GetAttribute("product_id"), product.GetAttribute("platform"));
			}

			return productIdToPlatform;
		}

		void LogAppStageFailure(Node newService, string title, string failureMessage)
		{
			new Node(model.GetNamedNode("AppDevelopmentStageFailures"), "app_stage_failure", "",
				new List<AttributeValuePair>
				{
					new AttributeValuePair("type", "app_stage_failure"),
					new AttributeValuePair("error_title",title),
					new AttributeValuePair("error_text", failureMessage),
					new AttributeValuePair("display_to_participants", true),
					new AttributeValuePair("new_service_node", newService.GetAttribute("name"))
				});
		}

		public int GetUniqueServiceId ()
		{
			return serviceIdGenerator.GetNextId();
		}
	}
}
