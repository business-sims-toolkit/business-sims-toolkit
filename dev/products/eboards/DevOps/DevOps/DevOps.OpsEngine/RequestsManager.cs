using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using CoreUtils;
using LibCore;
using Network;
// ReSharper disable ObjectCreationAsStatement

namespace DevOps.OpsEngine
{
    public class RequestsManager
    {
        readonly NodeTree model;
        readonly Node newServicesRound;
        readonly Node beginBizServicesHead;
        readonly Node timeNode;

        readonly Node startServicesNode;

        readonly Node beginServicesCommandQueueNode;

        readonly int round;

        

        public RequestsManager(NodeTree nt, int round)
        {
            model = nt;

            this.round = round;

            timeNode = model.GetNamedNode("CurrentTime");
            timeNode.AttributesChanged += TimeNodeAttributesChanged;

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

            PreprocessExistingServices();
        }

	    void beginServicesCommandQueueNode_ChildAdded(Node sender, Node child)
        {
            var commandType = child.GetAttribute("type");
            var beginServiceName = child.GetAttribute("service_name");

            var beginServiceNode = model.GetNamedNode(beginServiceName);

            Debug.Assert(beginServiceNode != null);

            switch(commandType)
            {
                case AppDevelopmentCommandType.DevOneSelection:
                case AppDevelopmentCommandType.DevTwoSelection:
                case AppDevelopmentCommandType.TestEnvironmentSelection:
                case AppDevelopmentCommandType.ReleaseSelection:
                case AppDevelopmentCommandType.EnclosureSelection:
                    var selection = child.GetAttribute("selection");
                    beginServiceNode.SetAttribute(commandType, selection);
                    break;
                case AppDevelopmentCommandType.ResetStage:
                    ResetServiceToStage(beginServiceNode, child.GetAttribute("target_status"));
                    break;
                case AppDevelopmentCommandType.EnqueueDeployment:
                    beginServiceNode.SetAttribute("deployment_enqueued", true);
                    break;
                case AppDevelopmentCommandType.InstallService:
                    beginServiceNode.SetAttribute("install_service", true);
                    break;
                case AppDevelopmentCommandType.CancelService:
                    beginServiceNode.SetAttribute("status", ServiceStatus.Cancelled);
                    break;
                case AppDevelopmentCommandType.UndoService:
                    beginServiceNode.SetAttribute("status", ServiceStatus.Undo);
                    break;
                case AppDevelopmentCommandType.ClearInstallFeedback:
                    beginServiceNode.SetAttribute("install_feedback_message", "");
                    break;
            }
            
            sender.DeleteChildTree(child);
        }

        void ResetServiceToStage(Node service, string targetStatus)
        {
            var stages = new List<string>
            {
                ServiceStatus.Dev,
                ServiceStatus.Test,
                ServiceStatus.Release
            };

            var targetStatusIndex = stages.IndexOf(targetStatus);

            Debug.Assert(targetStatusIndex != -1, $"Unknown target status: {targetStatus}");

            if (targetStatusIndex == -1)
            {
                return;
            }

            var currentStatus = service.GetAttribute("status");

            if (targetStatus == ServiceStatus.Release &&
                (currentStatus == ServiceStatus.Test || currentStatus == ServiceStatus.TestDelay))
            {
                targetStatus = currentStatus;
            }

            var resetAttributes = new List<AttributeValuePair>();

            if (targetStatus != currentStatus)
            {
                resetAttributes.Add(new AttributeValuePair("status", targetStatus));
            }

            if (targetStatusIndex == stages.IndexOf(ServiceStatus.Dev))
            {
                resetAttributes.Add(new AttributeValuePair("dev_one_selection", ""));
                resetAttributes.Add(new AttributeValuePair("dev_two_selection", ""));
                resetAttributes.Add(new AttributeValuePair("gain_per_minute",
                    service.GetAttribute("optimum_gain_per_minute")));
            }

            if (targetStatusIndex <= stages.IndexOf(ServiceStatus.Test))
            {
                var testEnvironmentSelection = service.GetAttribute("test_environment_selection");

                if (! string.IsNullOrEmpty(testEnvironmentSelection))
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

            if (targetStatusIndex <= stages.IndexOf(ServiceStatus.Release))
            {
                resetAttributes.Add(new AttributeValuePair("release_selection", ""));
            }

            for (var i = targetStatusIndex; i < stages.Count; i++)
            {
                resetAttributes.Add(new AttributeValuePair($"{stages[i]}_stage_status", ServiceStageStatus.Incomplete));
            }
            
            resetAttributes.Add(new AttributeValuePair("deployment_stage_status", ServiceStageStatus.Incomplete));
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

	        if (! startService.GetBooleanAttribute("is_auto_installed", false))
	        {
		        LogInfo(serviceName.Substring(3));
	        }

	        var newServiceNode = model.GetNamedNode(serviceName);

            var serviceId = newServiceNode.GetAttribute("service_id");

            var productId = startService.GetAttribute("product_id", "");

            if (string.IsNullOrEmpty(productId))
            {
                productId = GetOptimalProductForService(newServiceNode);
            }

            Debug.Assert(!string.IsNullOrEmpty(productId), "Product ID shouldn't be empty or null.");
            
            var targetStatus = startService.GetAttribute("target_status", ServiceStatus.Dev);


            var hideInReports = startService.GetBooleanAttribute("hide_in_reports", false);

            var isAutoInstalled = startService.GetBooleanAttribute("is_auto_installed", false);

            var isServiceIconVisible = startService.GetBooleanAttribute("hide_service_icon", false);
            
			StartService(serviceId, productId, new List<AttributeValuePair> 
                {
                    new AttributeValuePair("is_hidden_in_reports", hideInReports),
	                new AttributeValuePair("is_auto_installed", isAutoInstalled),
					new AttributeValuePair("is_auto_installed_at_end_of_round", startService.GetBooleanAttribute("is_auto_installed_at_end_of_round", false)),
                    new AttributeValuePair("hide_service_icon", isServiceIconVisible),
                    new AttributeValuePair("extract_incidents", false)
                }, 
                startService.GetBooleanAttribute("force_zero_cost", false),
				startService.GetBooleanAttribute("is_auto_installed", false));

            var beginServiceNode = model.GetNamedNode(serviceName.Replace("NS", "Begin"));

            var stages = new List<string>
            {
                ServiceStatus.Test,
                ServiceStatus.Release,
                ServiceStatus.Deploy,
                ServiceStatus.Live
            };

            var targetStatusIndex = stages.IndexOf(targetStatus);

            if (targetStatusIndex >= stages.IndexOf(ServiceStatus.Test))
            {
				var attributes = new ArrayList ();
				AttributeValuePair.AddIfNotEqual(beginServiceNode, attributes, "dev_one_selection", beginServiceNode.GetAttribute("dev_one_environment"));
				AttributeValuePair.AddIfNotEqual(beginServiceNode, attributes, "dev_two_selection", beginServiceNode.GetAttribute("dev_two_environment"));
				beginServiceNode.SetAttributes(attributes);
            }

            if (targetStatusIndex >= stages.IndexOf(ServiceStatus.Release))
            {
                beginServiceNode.SetAttributes(new List<AttributeValuePair>
                {
                    new AttributeValuePair("test_environment_selection", beginServiceNode.GetAttribute("test_environment")),
                    new AttributeValuePair("skip_test", true)
                });
            }

            if (targetStatusIndex >= stages.IndexOf(ServiceStatus.Deploy))
            {
                beginServiceNode.SetAttribute("release_selection", beginServiceNode.GetAttribute("release_code"));
            }

            if (targetStatusIndex >= stages.IndexOf(ServiceStatus.Live))
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

	    void PreprocessExistingServices()
        {
            // Reset the MBU info bonuses to 0
            // to prevent them from accumulating
            // from round to round (WEST-218)
            var mbus = model.GetNamedNode("BUs");
            var mbuList = mbus.GetChildrenAsList();

            foreach (var mbu in mbuList)
            {
                mbu.SetAttribute("instore_bonus", 0);
                mbu.SetAttribute("online_bonus", 0);
            }

            var services = model.GetNamedNode("Business Services Group");

            foreach (Node service in services)
            {
                var infoPerTransaction = service.GetIntAttribute("gain_per_minute", -1);

                if (infoPerTransaction == -1) continue;

                foreach (Node connection in service)
                {
                    AdjustMetrics(infoPerTransaction,
                        "BU " + connection.GetAttribute("to").Split(" ".ToCharArray())[1],
                        service.GetAttribute("transaction_type"));
                }
            }
        }
        
        public delegate void NewServiceStatusHandler(string mbu,string serviceName, string status, bool isHidden);
        public event NewServiceStatusHandler NewServiceStatusReceived;
        
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
			foreach (var teamName in new [] { "dev_one_selection", "dev_two_selection" })
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
                switch(avp.Attribute)
                {
                    case "status":
                        switch(avp.Value)
                        {
                            case ServiceStatus.TestDelay:
                                var testTime = sender.GetIntAttribute("test_time", 120);
                                var testEnvironmentNode = model.GetNamedNode("TestEnvironments")
                                        .GetChildWithAttributeValue("desc", sender.GetAttribute("test_environment_selection"));
                                
                                var additionalTestTime = testEnvironmentNode.GetIntAttribute("extra_test_time", 0);

                                testTime += additionalTestTime;

                                var testEnvCanBeBlocked =
                                    testEnvironmentNode.GetBooleanAttribute("can_be_blocked", false);

                                testEnvironmentNode.SetAttribute("in_use", testEnvCanBeBlocked);

                                sender.SetAttributes(new List<AttributeValuePair>
                                                     {
                                                         new AttributeValuePair("delayRemaining", testTime),
                                                         new AttributeValuePair("test_time", testTime)
                                                         
                                                     });
                                break;

                            case ServiceStatus.Cancelled:
                                var recoup = sender.GetBooleanAttribute("should_recoup_investment", false);
                                RemoveService(sender, recoup);
                                break;

                            case ServiceStatus.Undo:
                                RemoveService(sender, true);
                                break;
                                
                            case ServiceStatus.Release:
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

                        if (devStageStatus == ServiceStageStatus.Failed && 
                            (!string.IsNullOrEmpty(devOneSelection) || !string.IsNullOrEmpty(devTwoSelection)))
                        {
                            sender.SetAttribute("dev_stage_status", ServiceStageStatus.Incomplete);
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
                }
            }

            OnNewServiceStatus(sender.GetAttribute("mbu"), sender.GetAttribute("shortdesc"), sender.GetAttribute("status"), sender.GetBooleanAttribute("hideMessage", false));
        }

        void CheckDevStageSuccessful(Node service)
        {
			var devOneSelection = service.GetAttribute("dev_one_selection");
            var devEnvOne = service.GetAttribute("dev_one_environment");
            var isDevOneCorrect = devOneSelection == devEnvOne;

            if (service.GetBooleanAttribute("is_third_party", false))
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

            var serviceSecLevel = service.GetAttribute("data_security_classification");
            
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

        void ServicePassedDev (Node service, bool team1IsOptimal, bool team2IsOptimal, string team1Choice, string team2Choice)
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
                                      new AttributeValuePair("dev_stage_status", ServiceStageStatus.Completed),
                                      new AttributeValuePair("effectiveness_percent", effectivenessPercent)
                                  };

			if (! service.GetBooleanAttribute("is_auto_installed", false))
			{
				if (team1IsOptimal)
				{
					LogSuccess(service.GetAttribute("biz_service_function"), "dev1-optimal", team1Choice, null);
				}

				if (team2IsOptimal)
				{
					LogSuccess(service.GetAttribute("biz_service_function"), "dev2-optimal", team2Choice, null);
				}
			}

			if (! isOptimal)
            {
                attributes.Add(new AttributeValuePair ("feedback_message", "Suboptimal Development Environments"));

				if (! service.GetBooleanAttribute("is_auto_installed", false))
				{
					if (! team1IsOptimal)
					{
						LogError(service.GetAttribute("biz_service_function"), service.GetAttribute("shortdesc"),
							"dev", Guid.NewGuid().ToString(), "Suboptimal Development Environments Team 1", "dev1-suboptimal", team1Choice, null);
					}

					if (! team2IsOptimal)
					{
						LogError(service.GetAttribute("biz_service_function"), service.GetAttribute("shortdesc"),
							"dev", Guid.NewGuid().ToString(), "Suboptimal Development Environments Team 2", "dev2-suboptimal", team2Choice, null);
					}

					
					LogError(service.GetAttribute("biz_service_function"), service.GetAttribute("shortdesc"), "dev", Guid.NewGuid().ToString(), $"Suboptimal development environments. Running at {effectivenessPercent}% effectiveness.", "dev-suboptimal", "", effectivenessPercent);
				}
			}

			if (! service.GetBooleanAttribute("is_auto_installed", false))
			{
				LogSuccess(service.GetAttribute("biz_service_function"), "dev-integration-succeeded", $"{team1Choice}/{team2Choice}", effectivenessPercent);
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
                                      new AttributeValuePair("dev_stage_status", ServiceStageStatus.Failed)
                                  });

            LogAppStageFailure($"{service.GetAttribute("biz_service_function")} - Development Stage", AppDevelopmentStageFailureMessage.DevelopmentStage);

            LogError(service.GetAttribute("biz_service_function"), service.GetAttribute("shortdesc"), 
                "dev", Guid.NewGuid().ToString(), "Development Integration Failed", "dev-integration-failed", CONVERT.Format("{0} / {1}", team1Choice, team2Choice), null);

	        LogError(service.GetAttribute("biz_service_function"), service.GetAttribute("shortdesc"),
		        "dev", Guid.NewGuid().ToString(), "Development Integration Failed", team1IsOptimal ? "dev1-optimal" : "dev1-suboptimal", team1Choice, null);

	        LogError(service.GetAttribute("biz_service_function"), service.GetAttribute("shortdesc"),
		        "dev", Guid.NewGuid().ToString(), "Development Integration Failed", team2IsOptimal ? "dev2-optimal" : "dev2-suboptimal", team2Choice, null);
        }
        
        void CheckTestStageSuccessful(Node service)
        {
            var testSelection = service.GetAttribute("test_environment_selection");

            if (string.IsNullOrEmpty(testSelection))
            {
                return;
            }
            // The first time a new test environment has been selected since
            // they failed so reset the status to "incomplete".
            if (service.GetAttribute("test_stage_status") == ServiceStageStatus.Failed)
            {
                service.SetAttribute("test_stage_status", ServiceStageStatus.Incomplete);
            }

            var percent = service.GetIntAttribute("integration_gain_percent", 100);
            var nextStatus = ServiceStatus.TestDelay;

            var feedbackMessage = "Test Passed";
            var feedbackImageName = FeedbackImageName.Tick;

            var isOptimal = service.GetBooleanAttribute("optimal", true);
            var testCost = 0;

            var testEnvironments = model.GetNamedNode("TestEnvironments");
            
            if (testSelection == "Bypass")
            {
                percent = percent * testEnvironments.GetIntAttribute("bypass_test_effectiveness_percent", 100) / 100;

                feedbackMessage = "Test Bypassed";
                
                feedbackImageName = FeedbackImageName.Cross;
                nextStatus = ServiceStatus.Release;

                isOptimal = false;

                var serviceName = service.GetAttribute("biz_service_function");
                var shortName = service.GetAttribute("shortdesc");

				if (! service.GetBooleanAttribute("is_auto_installed", false))
				{
					LogError(serviceName, shortName, "test", Guid.NewGuid().ToString(), CONVERT.Format("Test bypassed. Running at {0}% effectiveness.", percent), "test-bypassed", "", percent);
				}
            }
            else
            {
                var testEnvironment = testEnvironments.GetChildWithAttributeValue("desc", testSelection);
                var serviceName = service.GetAttribute("biz_service_function");

				if (! service.GetBooleanAttribute("is_auto_installed", false))
				{
					LogSuccess(serviceName, "test-done", "", percent);
				}

                if(! service.GetBooleanAttribute("skip_test", false))
                {
                    var serviceSecLevel = service.GetAttribute("data_security_classification");

                    if (string.IsNullOrEmpty(serviceSecLevel))
                    {
                        throw new Exception("New Service missing security level");
                    }

                    var secLevelSupported =
                        testEnvironment.GetChildrenWithAttributeValue("type", "SecurityLevel")
                            .Any(secNode => secNode.GetAttribute("desc") == serviceSecLevel);

                    var isVirtual = (testEnvironment.GetAttribute("desc") == "Virtual Test");

	                isOptimal = ((service.GetAttribute("test_environment") == testEnvironment.GetAttribute("desc"))
								|| isVirtual);

                    testCost = testEnvironment.GetIntAttribute("extra_cost", 0);

	                string errorLogString = null;

	                var testEnvironmentDescription = testEnvironment.GetAttribute("desc");
					if (isVirtual)
					{
						testEnvironmentDescription = "Virtual";
					}

                    var optimalEnv = service.GetAttribute("test_environment");

	                if (! service.GetBooleanAttribute("is_auto_installed", false))
	                {
		                switch (testSelection)
		                {
			                case "2":
				                LogSuccess(serviceName, "test-extra-time", "", null);
				                if (optimalEnv != "2")
				                {
					                errorLogString = "Suboptimal test environment selected-Additonal test time added.";
				                }

			                    feedbackImageName = FeedbackImageName.Clock;
				                break;

			                case "3":
				                LogSuccess(serviceName, "test-extra-cost", "", null);
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
		                if (! service.GetBooleanAttribute("is_auto_installed", false))
		                {
			                LogSuccess(serviceName, "test-right-environment", testEnvironmentDescription, percent);
		                }
	                }
	                else
	                {
		                percent = percent * testEnvironments.GetIntAttribute("run_bad_effectiveness_percent", 100) / 100;

		                if (! secLevelSupported)
		                {
			                errorLogString += "Security level not supported.";
		                }

		                errorLogString += CONVERT.Format("Running at {0}% effectiveness", percent);

		                if (! service.GetBooleanAttribute("is_auto_installed", false))
		                {
			                LogError(serviceName, serviceName, "test", Guid.NewGuid().ToString(), errorLogString, "test-wrong-environment", testEnvironmentDescription, percent);
		                }
	                }
                }
                else
                {
                    nextStatus = ServiceStatus.Release;
                    testCost = 0;
                }
            }

            var gain = service.GetIntAttribute("gain_per_minute", 0);

            service.SetAttributes(new List<AttributeValuePair>
                                                 {
                                                     new AttributeValuePair("status", nextStatus),
                                                     //new AttributeValuePair("release_selection", ""),
                                                     new AttributeValuePair("feedback_message", feedbackMessage),
                                                     new AttributeValuePair("feedback_image", feedbackImageName),
                                                     new AttributeValuePair("optimal", isOptimal),
                                                     new AttributeValuePair("gain_per_minute", (int)(gain * (percent / 100f))),
                                                     new AttributeValuePair("has_passed_stage", true),
                                                     new AttributeValuePair("test_stage_status", ServiceStageStatus.Completed),
                                                     new AttributeValuePair("effectiveness_percent", percent)
                                                 });

            if (testCost != 0)
            {
                AdjustBudget(-testCost);
            }
        }

        void CheckReleaseCode (Node service)
        {
            var releaseSelection = service.GetAttribute("release_selection");

            if (string.IsNullOrEmpty(releaseSelection))
            {
                service.SetAttribute("deployment_enqueued", false);
                return;
            }
            // The first time a new release code has been selected since
            // they failed so reset the status to "incomplete".
            if (service.GetAttribute("release_stage_status") == ServiceStageStatus.Failed)
            {
                service.SetAttribute("release_stage_status", ServiceStageStatus.Incomplete);
            }

            var isCorrectReleaseCode = releaseSelection.Equals(service.GetAttribute("release_code"));

            if (isCorrectReleaseCode)
            {
                service.SetAttributes(new List<AttributeValuePair>
                {
                    new AttributeValuePair("feedback_message", "Correct Release Code"),
                    new AttributeValuePair("feedback_image", FeedbackImageName.Tick),
                    new AttributeValuePair("has_passed_stage", true),
                    new AttributeValuePair("release_stage_passed", true),
                    new AttributeValuePair("release_stage_status", ServiceStageStatus.Completed)
                });

                if (!service.GetBooleanAttribute("is_auto_installed", false))
                {
                    LogSuccess(service.GetAttribute("biz_service_function"), "release-right-code", releaseSelection, null);
                }
            }
            else
            {
                service.SetAttributes(new List<AttributeValuePair>
                {
                    new AttributeValuePair("release_selection", ""),
                    new AttributeValuePair("feedback_message", "Incorrect Release Code"),
                    new AttributeValuePair("feedback_image", FeedbackImageName.Cross),
                    new AttributeValuePair("has_passed_stage", false),
                    new AttributeValuePair("release_stage_passed", false),
                    new AttributeValuePair("release_stage_status", ServiceStageStatus.Failed)
                });

                LogAppStageFailure($"{service.GetAttribute("biz_service_function")} - Release Stage", AppDevelopmentStageFailureMessage.ReleaseStage);

                if (!service.GetBooleanAttribute("is_auto_installed", false))
                {
                    LogError(service.GetAttribute("biz_service_function"), service.GetAttribute("shortdesc"), "release", Guid.NewGuid().ToString(), "Incorrect Release Code", "release-wrong-code", releaseSelection, null);
                }
            }
        }

        void CheckEnclosureSelection (Node service)
        {
            var enclosureSelected = service.GetAttribute("enclosure_selection");

            if (string.IsNullOrEmpty(enclosureSelected))
            {
                return;
            }

            var serviceName = service.GetAttribute("biz_service_function");

            var cpuReq = service.GetIntAttribute("cpu_req", 0);

            
            Debug.Assert(cpuReq > 0, $"CPU requirement for service {serviceName} is missing");

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
                    LogError(serviceName, service.GetAttribute("shortdesc"), "deploy", Guid.NewGuid().ToString(), feedbackMessage, "deploy-install-failed", enclosureSelected, null);
                }
            }
            else
            {
                canDeploy = IsSufficientCapacityAvailable(enclosureSelected, out feedbackMessage, cpuReq);

                if (canDeploy)
                {
                    if (! service.GetBooleanAttribute("is_auto_installed", false))
                    {
                        LogSuccess(serviceName, "deploy-install-success", enclosureSelected, null);
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

        static bool IsDeploymentQueuedForService (Node service)
        {
            var releaseSelection = service.GetAttribute("release_selection");
            var enclosureSelection = service.GetAttribute("enclosure_selection");
            var isDeploymentQueued = service.GetBooleanAttribute("deployment_enqueued", false);

            return isDeploymentQueued && 
                   !string.IsNullOrEmpty(releaseSelection) &&
                   !string.IsNullOrEmpty(enclosureSelection);
        }

        void LogInfo(string serviceName) => new Node(model.GetNamedNode("CostedEvents"), "NS_info", "", new List<AttributeValuePair>
        {
            new AttributeValuePair ("type", "NS_info"),
            new AttributeValuePair ("service_name", serviceName)
        });

        void LogSuccess (string serviceName, string errorFullType, string errorDetails, int? effectivenessPercent)
	    {
		    var attributes = new List<AttributeValuePair>
		    {
			    new AttributeValuePair ("type", "NS_error"),
			    new AttributeValuePair ("service_name", serviceName),
			    new AttributeValuePair ("error_full_type", errorFullType),
			    new AttributeValuePair ("error_details", errorDetails)
		    };

		    if (effectivenessPercent.HasValue)
		    {
			    attributes.Add(new AttributeValuePair ("effectiveness_percent", effectivenessPercent.Value));
		    }

		    new Node(model.GetNamedNode("CostedEvents"), "NS_error", "", attributes);
	    }

        void LogError(string serviceName, string shortName, string errorType, string guid, string errorMessage, string errorFullType, string errorDetails, int? effectivenessPercent)
        {
	        var attributes = new List<AttributeValuePair>
	        {
		        new AttributeValuePair ("type", "NS_error"),
		        new AttributeValuePair ("service_name", serviceName),
		        new AttributeValuePair ("shortdesc", shortName),
		        new AttributeValuePair ("error_type", errorType),
		        new AttributeValuePair ("error_full_type", errorFullType),
		        new AttributeValuePair ("error_message", errorMessage),
		        new AttributeValuePair ("error_guid", guid),
		        new AttributeValuePair ("error_details", errorDetails)
	        };

	        if (effectivenessPercent.HasValue)
	        {
		        attributes.Add(new AttributeValuePair ("effectiveness_percent", effectivenessPercent.Value));
	        }

	        new Node(model.GetNamedNode("CostedEvents"), "NS_error", "", attributes);
        }

        void RemoveService (Node service, bool isUndo = false)
        {
            if (isUndo)
            {
                var investment = service.GetIntAttribute("business_investment", 0);
                AdjustBudget(investment);
            }

            beginBizServicesHead.DeleteChildTree(service);

	        if (isUndo)
	        {
		        var attributes = new List<AttributeValuePair>
		        {
			        new AttributeValuePair ("type", "NS_error"),
			        new AttributeValuePair ("service_name", service.GetAttribute("biz_service_function")),
			        new AttributeValuePair ("error_type", "undo"),
		        };

		        new Node (model.GetNamedNode("CostedEvents"), "NS_error", "", attributes);
	        }
        }

		void TimeNodeAttributesChanged(Node sender, ArrayList attributes)
        {
            var newServicesWaitingToBeInstalled = beginBizServicesHead.GetChildrenAsList();

            foreach (var newServiceNode in newServicesWaitingToBeInstalled)
            {

                var testDelayTime = newServiceNode.GetIntAttribute("delayRemaining");

                if (testDelayTime >= 0)
                {
                    if (testDelayTime == 0)
                    {
                        newServiceNode.SetAttribute("status", "release");

                        model.GetNamedNode("TestEnvironments")
                            .GetChildWithAttributeValue("desc", newServiceNode.GetAttribute("test_environment_selection"))
                            .SetAttribute("in_use", false);
                        
                    }

                    testDelayTime--;
                    newServiceNode.SetAttribute("delayRemaining", testDelayTime.Value);
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

        public List<KeyValuePair<string, bool>> GetServices()
        {
            
            var newServices = newServicesRound.GetChildrenAsList();
            newServices.Sort(new NewServicesIdComparer());

            var serviceIds = new List<KeyValuePair<string, bool>>();

            foreach (var newService in newServices)
            {
                var serviceId = newService.GetAttribute("service_id");

                serviceIds.Add(new KeyValuePair<string, bool>(serviceId,
                    (!HasNewServiceDevelopmentAlreadyStarted(serviceId)) &&
                    string.IsNullOrEmpty(newService.GetAttribute("target_status"))));
                
            }

            return serviceIds;
        }

        public string GetBusinessServiceNameFromServiceId (string serviceId)
        {
            var newService = newServicesRound.GetChildWithAttributeValue("service_id", serviceId);

            return newService.GetAttribute("biz_service_function") ?? "NOT FOUND";
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
            foreach(Node installService in beginBizServicesHead)
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

        public void StartServiceDevelopment(string serviceId, string productId)
        {
            CreateStartService(serviceId, productId,
                new List<AttributeValuePair> { new AttributeValuePair("target_status", "dev"), new AttributeValuePair("extract_incidents", true) });
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
                                                 new AttributeValuePair("product_id", productId)
                                             };
            if (additionalAttrs != null)
            {
                attrs.AddRange(additionalAttrs);
            }

            new Node(startServicesNode, "StartService", "", attrs);
        }
        
        // DevOps

        void StartService(string serviceId, string productId, List<AttributeValuePair> attrs = null, bool forceZeroCost = false, bool isAutoInstalled = false)
        {
            var serviceNode = newServicesRound.GetChildWithAttributeValue("service_id", serviceId);
            var productNode = serviceNode.GetChildWithAttributeValue("product_id", productId);

            var defaultAttrs = new List<AttributeValuePair>
                                                    {
                                                        // Attributes from Service node
                                                        new AttributeValuePair("type", "BeginNewServicesInstall"),
                                                        new AttributeValuePair("up", true),
                                                        new AttributeValuePair("service_id", serviceId),
                                                        new AttributeValuePair("product_id", productId),
                                                        new AttributeValuePair("status", "dev"),
                                                        new AttributeValuePair("bladeCost",
                                                            serviceNode.GetIntAttribute("bladeCost", 0)),
                                                        new AttributeValuePair("round", round),
                                                        new AttributeValuePair("data_security_classification",
                                                            serviceNode.GetAttribute("data_security_classification")),
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
                                                        new AttributeValuePair("target_gain",
                                                            serviceNode.GetAttribute("target_gain")),
                                                        new AttributeValuePair("is_third_party",
                                                            serviceNode.GetAttribute("is_third_party")),

                                                        // Time
                                                        new AttributeValuePair("time_started",
                                                            model.GetNamedNode("CurrentTime")
                                                            .GetIntAttribute("seconds", 0)),

                                                        // Attributes from Product node
                                                        new AttributeValuePair("business_investment",
                                                            productNode.GetAttribute("business_investment")),
                                                        new AttributeValuePair("test_environment",
                                                            productNode.GetAttribute("test_environment")),
                                                        new AttributeValuePair("gain_per_minute",
                                                            productNode.GetAttribute("gain_per_minute")),
                                                        new AttributeValuePair("optimum_gain_per_minute",
                                                            productNode.GetAttribute("gain_per_minute")),
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
                                                        new AttributeValuePair("dev_stage_status", ServiceStageStatus.Incomplete),
                                                        new AttributeValuePair("test_stage_status", ServiceStageStatus.Incomplete),
                                                        new AttributeValuePair("release_stage_status", ServiceStageStatus.Incomplete),
                                                        new AttributeValuePair("deployment_stage_status", ServiceStageStatus.Incomplete),

                                                    };

            if (attrs != null)
            {
                defaultAttrs.AddRange(attrs);
            }

            new Node(beginBizServicesHead, "BeginNewServicesInstall",
                "Begin " + serviceNode.GetAttribute("biz_service_function"), defaultAttrs);

            if (!forceZeroCost)
            {
                var serviceCost = productNode.GetIntAttribute("business_investment", 0);

                AdjustBudget(-serviceCost);
            }

	        if (! isAutoInstalled)
	        {
		        if (productNode.GetBooleanAttribute("is_optimal_product", false))
		        {
			        LogSuccess(serviceNode.GetAttribute("biz_service_function"), "product-optimal", productId, null);
		        }
		        else
		        {
			        LogError(serviceNode.GetAttribute("biz_service_function"), serviceNode.GetAttribute("shortdesc"),
				        "product", Guid.NewGuid().ToString(), "Suboptimal financial choice", "product-suboptimal", productId, null);
		        }
	        }
        }
        
        public bool IsNewServiceInCatalog(string newServiceSelected)
        {
            var bizServices = model.GetNamedNode("Business Services Group");

            foreach (Node bizService in bizServices)
            {
                if (bizService.GetAttribute("service_id").Equals(newServiceSelected))
                {
                    return true;
                }
            }
            return false;
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

        void InstallNewService(Node service, bool isAutoDeploying = false)
        {
            var serviceName = service.GetAttribute("biz_service_function");
            var enclosureSelected = service.GetAttribute("enclosure_selection");
            var enclosureNode = model.GetNamedNode(enclosureSelected);
	        var servicePlatform = service.GetAttribute("platform");

            var isServicePlatformSupported =
                model.GetNamedNode("SupportedPlatforms").GetChildrenAsList().
                    Any(supportedPlatform => supportedPlatform.GetAttribute("desc") == servicePlatform);

            var hasFailed = ! isServicePlatformSupported;
            if (hasFailed)
            {
                var failureMessage = $"Fail- {service.GetAttribute("failure_remark")}";

                service.SetAttributes(new List<AttributeValuePair>
                                                  {
                                                      new AttributeValuePair("install_feedback_message", failureMessage),
                                                      new AttributeValuePair("feedback_image", FeedbackImageName.Cross),
                                                      new AttributeValuePair("is_install_successful", false),
                                                      new AttributeValuePair("deployment_stage_status", ServiceStageStatus.Failed),
                                                      new AttributeValuePair("can_deploy", false)
                                                  });
                
                LogError(serviceName, serviceName, "deploy", Guid.NewGuid().ToString(), failureMessage, "deploy-failure", servicePlatform, null);

                // Sorry, not sorry, Chris
                if (! isAutoDeploying)
                {
                    LogAppStageFailure($"{serviceName} - Deployment Stage", AppDevelopmentStageFailureMessage.DeploymentStage);
                }

                return;
            }

			var isOptimal = (service.GetAttribute("enclosure_selection") == service.GetAttribute("server"))
							 || enclosureNode.GetBooleanAttribute("always_optimal", false);

            var feedbackMessage = "Successful Installation- Optimum Enclosure";
            var gain = service.GetIntAttribute("gain_per_minute", 0);

	        if (isOptimal)
	        {
				if (! service.GetBooleanAttribute("is_auto_installed", false))
				{
					LogSuccess(serviceName, "deploy-optimal-enclosure", service.GetAttribute("enclosure_selection"), null);
				}
	        }
			else
	        {
	            var serviceEffectiveness = service.GetIntAttribute("effectiveness_percent", 100);
                var percent = model.GetNamedNode("TestEnvironments").GetIntAttribute("run_bad_effectiveness_percent", 0) / 100f * serviceEffectiveness;
                feedbackMessage = "Successful Installation - Suboptimal Enclosure";

                gain = (int) (gain * (percent / 100f));

                var optimalGain = service.GetIntAttribute("optimum_gain_per_minute", 0);
                var effectiveness = (optimalGain == 0) ? 0f : (gain / (float)optimalGain) * 100;

				if (! service.GetBooleanAttribute("is_auto_installed", false))
				{
					LogError(serviceName, serviceName, "deploy", Guid.NewGuid().ToString(), $"Running at {(int)effectiveness}% effectiveness.", "deploy-suboptimal-enclosure", service.GetAttribute("enclosure_selection"), (int)percent);
				}
            }
            
            service.SetAttributes(new List<AttributeValuePair>
                                  {
                                      new AttributeValuePair("status", ServiceStatus.Live),
                                      new AttributeValuePair("enclosure", enclosureSelected),
                                      new AttributeValuePair("install_feedback_message", feedbackMessage),
                                      new AttributeValuePair("feedback_image", FeedbackImageName.Tick),
                                      new AttributeValuePair("gain_per_minute", gain),
                                      new AttributeValuePair("optimal", isOptimal),
                                      new AttributeValuePair("is_install_successful", true),
                                      new AttributeValuePair("can_deploy", false),
                                      new AttributeValuePair("deployment_stage_status", ServiceStageStatus.Completed)
                                  });

            // Modify capacity of enclosure
			var cpuReq = service.GetIntAttribute("cpu_req");
            if (service.GetBooleanAttribute("force_zero_cpu_usage", false))
            {
                cpuReq = 0;
            }

            var freeCpu = enclosureNode.GetIntAttribute("free_cpu");
            var usedCpu = enclosureNode.GetIntAttribute("used_cpu");

            if (cpuReq.HasValue && freeCpu.HasValue && usedCpu.HasValue)
            {
                if (freeCpu < cpuReq)
                {
                    var numCpuPerBlade = model.GetNamedNode("blade").GetIntAttribute("cpu", 15);

                    var numBladesRequired = (int) Math.Ceiling((cpuReq.Value - freeCpu.Value) / (double) numCpuPerBlade);

                    // Need to commission a new blade
                    var freeHeight = enclosureNode.GetIntAttribute("free_height",0);

                    Debug.Assert(freeHeight > 0 || freeHeight >= numBladesRequired,
                        "Enclosure doesn't have enough CPU or height space remaining. Shouldn't have reached here.");
                    
                    freeHeight -= numBladesRequired;

                    enclosureNode.SetAttribute("free_height", freeHeight);

                    freeCpu += numBladesRequired * numCpuPerBlade;

                    var bladeCost = model.GetNamedNode("blade").GetIntAttribute("cost", 0);

                    bladeCost *= numBladesRequired;

                    AdjustBudget(-bladeCost);

                    new Node(model.GetNamedNode("CostedEvents"), "blade_cost", "", new List<AttributeValuePair>
                                                                                   {
                                                                                       new AttributeValuePair("type", "blade_cost"),
                                                                                       new AttributeValuePair("cost", bladeCost)
                                                                                   });
                }
                
                enclosureNode.SetAttributes(new List<AttributeValuePair>
                                            {
                                                new AttributeValuePair("free_cpu", freeCpu.Value - cpuReq.Value),
                                                new AttributeValuePair("used_cpu", usedCpu.Value + cpuReq.Value)
                                            });
                
            }
            else
            {
                throw new Exception("Either CPU req., free CPU, or used CPU is missing.");
            }
            

            // Create the biz service
            var bizServiceName = service.GetAttribute("biz_service_function");

            var bizServiceNode = new Node(model.GetNamedNode("Business Services Group"), "biz_service", bizServiceName,
                new List<AttributeValuePair>
                {
                    new AttributeValuePair("type", "biz_service"),
                    new AttributeValuePair("desc", bizServiceName),
                    new AttributeValuePair("shortDesc", service.GetAttribute("shortDesc")),
                    new AttributeValuePair("icon", service.GetAttribute("icon")),
                    new AttributeValuePair("notinnetwork", true),
                    new AttributeValuePair("biz_service_function", service.GetAttribute("biz_service_function")),
                    new AttributeValuePair("has_impact", true),
                    new AttributeValuePair("slalimit", 360),
                    new AttributeValuePair("service_id", service.GetAttribute("service_id")),
                    new AttributeValuePair("data_security_classification", service.GetAttribute("data_security_classification")),
                    new AttributeValuePair("business_investment", service.GetAttribute("business_investment")),
                    new AttributeValuePair("transaction_type", service.GetAttribute("transaction_type")),
                    new AttributeValuePair("version", 1),
                    new AttributeValuePair("gantt_order", service.GetAttribute("gantt_order")),
                    new AttributeValuePair("gain_per_minute", service.GetAttribute("gain_per_minute")),
                    new AttributeValuePair("product_id", service.GetAttribute("product_id")),
                    new AttributeValuePair("up", true),
                    new AttributeValuePair("target_gain", service.GetAttribute("target_gain")),
                    new AttributeValuePair("round", round),
                    new AttributeValuePair("is_hidden_in_reports", service.GetBooleanAttribute("is_hidden_in_reports", false))

                });

            // Create the biz service users
            var bizName = SkinningDefs.TheInstance.GetData("biz");

            var gainPerMinute = service.GetIntAttribute("gain_per_minute", 0);

            foreach (var storeId in service.GetAttribute("stores").Split(','))
            {
                var storeName = $"{bizName} {storeId}";

                var storeBizName = $"{storeName} {bizServiceName}";

                // Create BSU
                var storeNode = model.GetNamedNode(storeName);

                new Node(storeNode, "biz_service_user", storeBizName,
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
                        new AttributeValuePair("transaction_type",service.GetAttribute("transaction_type")),
                        new AttributeValuePair("desc",storeBizName),
                        new AttributeValuePair("shortdesc",service.GetAttribute("shortDesc")),
                        new AttributeValuePair("icon",service.GetAttribute("icon")),
                        new AttributeValuePair("is_hidden_in_reports", service.GetBooleanAttribute("is_hidden_in_reports", false))
                    });

                // Adjust the stores bonuses
                AdjustMetrics(gainPerMinute, storeName, service.GetAttribute("transaction_type"));
            }

            var appName = service.GetAttribute("app");
            var appNode = model.GetNamedNode(appName) ?? new Node(enclosureNode, "App", appName,
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
                    new AttributeValuePair("platform", service.GetAttribute("platform")),
                    new AttributeValuePair("version", 1),
                    new AttributeValuePair("round", round)
                });

            // Create the App to BSU links

            foreach (var storeId in service.GetAttribute("stores").Split(','))
            {
                var storeBizName = CONVERT.Format("{0} {1} {2}", bizName, storeId, bizServiceName);

                // Link with app
                new LinkNode(appNode, "Connection", CONVERT.Format("{0} {1} {2}",appName, storeBizName, "Connection"),
                    new List<AttributeValuePair>
                    {
                        new AttributeValuePair("type", "Connection"),
                        new AttributeValuePair("to", storeBizName)
                    });
            }

            // Create BS to BSU links
            foreach (var storeId in service.GetAttribute("stores").Split(','))
            {
                var storeBizName = CONVERT.Format("{0} {1} {2}", bizName, storeId, bizServiceName);

                // Link with biz service
                new LinkNode(bizServiceNode, "Connection", "BS " + storeBizName + " Connection",
                    new List<AttributeValuePair>
                    {
                        new AttributeValuePair("type", "Connection"),
                        new AttributeValuePair("contype", bizServiceName),
                        new AttributeValuePair("to", storeBizName)
                    });
            }

            // Add installed service to Completed New Services node
            var completedServicesNode = model.GetNamedNode("CompletedNewServices")
                .GetChildWithAttributeValue("round", round.ToString());

            var completedName = service.GetAttribute("name").Replace("Begin", "Completed");

            new Node(completedServicesNode, "CompletedService", completedName,
                new List<AttributeValuePair>
                {
                    new AttributeValuePair("biz_service_function", bizServiceName),
                    new AttributeValuePair("revenue_made", 0),
                    new AttributeValuePair("investment_cost", service.GetAttribute("business_investment")),
                    new AttributeValuePair("gain_per_minute", gainPerMinute),
                    new AttributeValuePair("transaction_type", service.GetAttribute("transaction_type")),
                    new AttributeValuePair("profit", -CONVERT.ParseInt(service.GetAttribute("business_investment"))),
                    new AttributeValuePair("is_hidden_in_reports", service.GetBooleanAttribute("is_hidden_in_reports", false)),
                    new AttributeValuePair("is_auto_installed", service.GetBooleanAttribute("is_auto_installed", false))
                });
            
			// You seem to be unbreakable Mr Bond.
	        if (! string.IsNullOrEmpty(service.GetAttribute("make_unbreakable")))
	        {
		        var app = model.GetNamedNode(service.GetAttribute("make_unbreakable"));
				if (app != null)
				{
					string incidentId = null;
					foreach (LinkNode link in app.GetChildrenOfType("Connection"))
					{
						var linkIncident = link.GetAttribute("incident_id");
						if (! string.IsNullOrEmpty(linkIncident))
						{
							incidentId = linkIncident;
							break;
						}
					}
					
					if (! string.IsNullOrEmpty(incidentId))
					{
						new Node (model.GetNamedNode("enteredIncidents"), "IncidentNumber", "", new AttributeValuePair ("id", incidentId + "_fix_confirmed"));
					}

					app.SetAttribute("unbreakable", true);
				}
	        }
        }

        // Adds the amount passed in to the current budget. 
        // If the budget is to be decreased then
        // a negative amount should be passed in.
        void AdjustBudget(int amount)
        {
            var budgetNode = model.GetNamedNode("Budgets").GetChildWithAttributeValue("round", round.ToString());
            
            budgetNode.IncrementIntAttribute("budget", amount, 0);
        }

	    void AdjustMetrics(int infoPerTrans,string mbuSelected, string transactionType)
        {

            var mbus = model.GetNamedNode("BUs");
            var mbuList = mbus.GetChildrenAsList();

            foreach (var mbu in mbuList)
            {
                if (mbu.GetAttribute("name").Equals(mbuSelected))
                {
                    int onlineBonus;
                    int instoreBonus;

                    switch (transactionType)
                    {
                        case "online":
                            onlineBonus = mbu.GetIntAttribute("online_bonus",0);
                            onlineBonus += infoPerTrans;
                            mbu.SetAttribute("online_bonus",onlineBonus);
                            break;
                        case "instore":
                            instoreBonus = mbu.GetIntAttribute("instore_bonus",0);
                            instoreBonus += infoPerTrans;
                            mbu.SetAttribute("instore_bonus",instoreBonus);
                            break;
                        case "both":
                            onlineBonus = mbu.GetIntAttribute("online_bonus",0);
                            instoreBonus = mbu.GetIntAttribute("instore_bonus", 0);
                            onlineBonus += infoPerTrans;
                            instoreBonus += infoPerTrans;
                            mbu.SetAttribute("online_bonus",onlineBonus);
                            mbu.SetAttribute("instore_bonus",instoreBonus);
                            break;
                    }
                }
            }
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
                                                           new AttributeValuePair("status", ServiceStatus.Cancelled)
                                                       });
                }

                // If it reaches here then the service needs to be started (or restarted)
                var serviceId = newServiceNode.GetAttribute("service_id");
                var productId = GetOptimalProductForService(newServiceNode);
                
                CreateStartService(serviceId, productId, new List<AttributeValuePair>
                                                   {
                                                       new AttributeValuePair("target_status", ServiceStatus.Live),
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

        bool ProgressServiceToDeployment(Node beginServiceNode, bool isAtRoundEnd)
        {
            var serviceStatus = beginServiceNode.GetAttribute("status");

            if (serviceStatus == "install" || serviceStatus == ServiceStatus.Live)
            {
                return true;
            }

            beginServiceNode.SetAttribute("is_hidden_in_reports", true);
            beginServiceNode.SetAttribute("hide_service_icon", true);
	        beginServiceNode.SetAttribute("is_auto_installed", true);

	        if (isAtRoundEnd)
	        {
		        beginServiceNode.SetAttribute("is_auto_installed_at_end_of_round", true);
	        }

	        // Service has been started so
			// progress it to being deployed.
			var statuses = new List<string>
                            {
                                ServiceStatus.Dev,
                                ServiceStatus.Test,
                                ServiceStatus.TestDelay,
                                ServiceStatus.Release,
                                "finishedRelease",
                                "install",
                                ServiceStatus.Live
                            };


            var statusIndex = statuses.IndexOf(serviceStatus);

            Debug.Assert(statusIndex >= 0, "Status is in an unrecognised stage.");
            
            if (statusIndex < statuses.IndexOf(ServiceStatus.Test))
            {
                var devOneBuild = beginServiceNode.GetAttribute("dev_one_environment");
                var devTwoBuild = beginServiceNode.GetAttribute("dev_two_environment");

                beginServiceNode.SetAttributes(new List<AttributeValuePair>
                                                {
                                                    new AttributeValuePair("dev_one_selection", devOneBuild),
                                                    new AttributeValuePair("dev_two_selection", devTwoBuild),
                                                });
            }

            if (statusIndex < statuses.IndexOf(ServiceStatus.TestDelay))
            {
                var testEnv = beginServiceNode.GetAttribute("test_environment");

                beginServiceNode.SetAttributes(new List<AttributeValuePair>
                                                {
                                                    new AttributeValuePair("test_environment_selection", testEnv),
                                                    new AttributeValuePair("skip_test", true)
                                                });
            }

            if (statusIndex == statuses.IndexOf(ServiceStatus.TestDelay))
            {
                beginServiceNode.SetAttribute("delayRemaining", 0);
            }

            if (statusIndex < statuses.IndexOf(ServiceStatus.Release))
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

	    public Dictionary<string, bool> GetProductOptimalitiesForService (string serviceId)
	    {
		    var newService = newServicesRound.GetChildWithAttributeValue("service_id", serviceId);

		    var productIdToOptimality = new Dictionary<string, bool> ();

		    foreach (Node product in newService)
		    {
			    productIdToOptimality.Add(product.GetAttribute("product_id"), product.GetBooleanAttribute("is_optimal_product", false));
		    }

		    return productIdToOptimality;
	    }

	    public Dictionary<string, string> GetProductPlatformsForService (string serviceId)
	    {
		    var newService = newServicesRound.GetChildWithAttributeValue("service_id", serviceId);

		    var productIdToPlatform = new Dictionary<string, string> ();

		    foreach (Node product in newService)
		    {
			    productIdToPlatform.Add(product.GetAttribute("product_id"), product.GetAttribute("platform"));
		    }

		    return productIdToPlatform;
	    }

        void LogAppStageFailure (string title, string failureMessage)
        {
            new Node(model.GetNamedNode("AppDevelopmentStageFailures"), "app_stage_failure", "",
                new List<AttributeValuePair>
                {
                    new AttributeValuePair("type", "app_stage_failure"),
                    new AttributeValuePair("error_title",title),
                    new AttributeValuePair("error_text", failureMessage),
                    new AttributeValuePair("display_to_participants", true)
                });
        }
	}
}