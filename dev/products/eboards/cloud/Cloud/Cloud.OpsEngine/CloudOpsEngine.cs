using System;
using System.Collections.Generic;
using System.Text;

using GameManagement;
using Network;
using LibCore;
using CoreUtils;
using GameEngine;
using IncidentManagement;
using Logging;

namespace Cloud.OpsEngine
{
	public class CloudOpsEngine : BaseOpsPhaseEngine, ITimedClass, IDisposable
	{
		NetworkProgressionGameFile gameFile;
		NodeTree model;

		IncidentApplier incidentApplier;

		public IncidentApplier IncidentApplier => incidentApplier;

		BusinessServiceRules.PreplayManager prePlayManager;
		SecondsRunner secondsRunner;
		Node timeNode;
		Node roundVariables;

		Biller biller;
		OrderPlanner orderPlanner;
		OrderExecutor orderExecutor;

		PowerMonitor powerMonitor;
		OpExMonitor opExMonitor;
		BauManager bauManager;
		VirtualMachineManager vmManager;
		DemandManager demandManager;
		PublicVendorManager pvManager;

		Node businessesNode;
		Dictionary<Node, Business> nodeToBusiness;

		Dictionary<Node, Datacenter> nodeToDatacenter;

		BasicIncidentLogger incidentLogger;

		Leaderboard leaderboard;

		public CloudOpsEngine (NetworkProgressionGameFile gameFile)
		{
			int round = gameFile.CurrentRound;

			this.gameFile = gameFile;
			model = gameFile.NetworkModel;

			if (! gameFile.IsSalesGame)
			{
				incidentLogger = new BasicIncidentLogger ();
				incidentLogger.SetIgnoreUnameDeletions(true);
				incidentLogger.LogTreeToFile(model, gameFile.GetRoundFile(round, "NetworkIncidents.log", GameFile.GamePhase.OPERATIONS));
				incidentLogger.KeepLogOpen = true;
			}

			incidentApplier = new IncidentApplier (model);

			bauManager = new BauManager (model);
			vmManager = new VirtualMachineManager (model, bauManager);

			biller = new Biller (model);

			orderPlanner = new OrderPlanner (model, bauManager, vmManager);
			orderExecutor = new OrderExecutor (model, orderPlanner, biller, vmManager);
			orderExecutor.OrderProcessed += new EventHandler (orderExecutor_OrderProcessed);

			prePlayManager = new BusinessServiceRules.PreplayManager (model);
			secondsRunner = new SecondsRunner (model);
			secondsRunner.Tick += secondsRunner_SecondTickover;
			timeNode = model.GetNamedNode("CurrentTime");
			timeNode.SetAttribute("seconds", 0);
			timeNode.AttributesChanged += new Node.AttributesChangedEventHandler (timeNode_AttributesChanged);

			powerMonitor = new PowerMonitor (model, biller);
			opExMonitor = new OpExMonitor (model, biller);
			demandManager = new DemandManager (model, orderExecutor);

			pvManager = new PublicVendorManager (model, round);

			roundVariables = model.GetNamedNode("RoundVariables");

			PrepareNetwork(round);

			nodeToBusiness = new Dictionary<Node, Business> ();
			businessesNode = model.GetNamedNode("Businesses");
			foreach (Node businessNode in businessesNode.GetChildrenOfType("business"))
			{
				nodeToBusiness.Add(businessNode, new Business (model, businessNode, bauManager, orderExecutor, biller));
			}

			nodeToDatacenter = new Dictionary<Node, Datacenter> ();
			Node networkNode = model.GetNamedNode("Network");
			foreach (Node datacenterNode in networkNode.GetChildrenOfType("datacenter"))
			{
				nodeToDatacenter.Add(datacenterNode, new Datacenter (model, datacenterNode));
			}

			GlobalEventDelayer.TheInstance.DestroyEventDelayer();
			GlobalEventDelayer.TheInstance.SetEventDelayer(new EventDelayer ());
			GlobalEventDelayer.TheInstance.Delayer.SetModelCounter(model, "CurrentTime", "seconds");
			OnAttributeHitApplier.TheInstance.Clear();

			leaderboard = new Leaderboard (model, bauManager);

			model.SaveToURL("", gameFile.GetRoundFile(gameFile.CurrentRound, "network_at_start.xml", GameFile.GamePhase.OPERATIONS));

			TimeManager.TheInstance.ManageClass(this);

			timeNode.SetAttribute("seconds", 0);
		}

		void secondsRunner_SecondTickover (object sender, EventArgs e)
		{
			vmManager.AssignCpus();
		}

		void orderExecutor_OrderProcessed (object sender, EventArgs e)
		{
			SetGameStarted();
		}

		void PrepareNetwork (int round)
		{
			bool sharedDevelopment = false;
			bool onlineAndFloorCanShareRacks = false;

			bool serverDeploymentAllowed = false;
			bool rackDeploymentAllowed = false;
			bool localPrivateCloudDeploymentAllowed = false;
			bool globalPrivateCloudDeploymentAllowed = false;
			bool publicIaasCloudDeploymentAllowed = false;
			bool publicSaasCloudDeploymentAllowed = false;

			bool canDeployLowSecurityToCloud = false;
			bool canDeployMediumSecurityToCloud = false;

			string productionDeployType = "";
			string devTestDeployType = "";

			int extraDevDelay = 0;
			string latestRetirableServerGroup = "";

			int normalDemandAnnouncementDuration = 0;
			int optionalDemandAnnouncementDuration = 0;

			switch (round)
			{
				case 1:
					onlineAndFloorCanShareRacks = false;
					sharedDevelopment = false;

					serverDeploymentAllowed = false;
					rackDeploymentAllowed = true;
					localPrivateCloudDeploymentAllowed = false;
					globalPrivateCloudDeploymentAllowed = false;
					publicIaasCloudDeploymentAllowed = false;
					publicSaasCloudDeploymentAllowed = false;

					productionDeployType = "rack";
					devTestDeployType = "server";

					extraDevDelay = 120;
					latestRetirableServerGroup = "";

					normalDemandAnnouncementDuration = 4;
					optionalDemandAnnouncementDuration = 4;

					break;

				case 2:
					onlineAndFloorCanShareRacks = true;
					sharedDevelopment = true;

					serverDeploymentAllowed = false;
					rackDeploymentAllowed = false;
					localPrivateCloudDeploymentAllowed = true;
					globalPrivateCloudDeploymentAllowed = false;
					publicIaasCloudDeploymentAllowed = false;
					publicSaasCloudDeploymentAllowed = false;

					productionDeployType = "local";
					devTestDeployType = "global";

					extraDevDelay = 60;
					latestRetirableServerGroup = "A";

					normalDemandAnnouncementDuration = 4;
					optionalDemandAnnouncementDuration = 4;

					break;

				case 3:
				case 4:
					onlineAndFloorCanShareRacks = true;
					sharedDevelopment = true;

					serverDeploymentAllowed = false;
					rackDeploymentAllowed = false;
					localPrivateCloudDeploymentAllowed = true;
					globalPrivateCloudDeploymentAllowed = false;
					publicIaasCloudDeploymentAllowed = true;
					publicSaasCloudDeploymentAllowed = true;

					productionDeployType = "local";
					devTestDeployType = "global";

					extraDevDelay = 60;
					latestRetirableServerGroup = ((round == 3) ? "B" : "C");
					canDeployLowSecurityToCloud = true;
					canDeployMediumSecurityToCloud = (round == 4);

					normalDemandAnnouncementDuration = 2;
					optionalDemandAnnouncementDuration = 2;

					break;
			}

			List<AttributeValuePair> attributes = new List<AttributeValuePair> ();
			attributes.Add(new AttributeValuePair ("online_and_floor_can_share_racks", onlineAndFloorCanShareRacks));
			attributes.Add(new AttributeValuePair ("shared_development", sharedDevelopment));
												  
			attributes.Add(new AttributeValuePair ("server_deployment_allowed", serverDeploymentAllowed));
			attributes.Add(new AttributeValuePair ("rack_deployment_allowed", rackDeploymentAllowed));
			attributes.Add(new AttributeValuePair ("local_private_cloud_deployment_allowed", localPrivateCloudDeploymentAllowed));
			attributes.Add(new AttributeValuePair ("global_private_cloud_deployment_allowed", globalPrivateCloudDeploymentAllowed));
			attributes.Add(new AttributeValuePair ("public_iaas_cloud_deployment_allowed", publicIaasCloudDeploymentAllowed));
			attributes.Add(new AttributeValuePair ("public_saas_cloud_deployment_allowed", publicSaasCloudDeploymentAllowed));
												  
			attributes.Add(new AttributeValuePair ("production_deploy_type", productionDeployType));
			attributes.Add(new AttributeValuePair ("dev_deploy_type", devTestDeployType));
												  
			attributes.Add(new AttributeValuePair ("handover_time", extraDevDelay));
			attributes.Add(new AttributeValuePair ("latest_retirable_server_group", latestRetirableServerGroup));
			attributes.Add(new AttributeValuePair ("current_round", round));
			attributes.Add(new AttributeValuePair ("round_started", false));

			attributes.Add(new AttributeValuePair ("can_deploy_low_security_to_cloud", canDeployLowSecurityToCloud));
			attributes.Add(new AttributeValuePair ("can_deploy_medium_security_to_cloud", canDeployMediumSecurityToCloud));

			attributes.Add(new AttributeValuePair ("demand_announcement_duration_trading_periods", normalDemandAnnouncementDuration));
			attributes.Add(new AttributeValuePair ("optional_demand_announcement_duration_trading_periods", optionalDemandAnnouncementDuration));

			roundVariables.SetAttributes(attributes);

			CompleteAllDevelopment();
			ClearOpexFlags();
			ClearDemands();
			ClearFinances();
			ClearConsolidations();
			ClearCpuUses();

			if (round == 2)
			{
				CombineClouds();
			}
		}

		void ClearCpuUses ()
		{
			foreach (Node vmInstance in model.GetNodesWithAttributeValue("type", "vm_instance"))
			{
				Node businessService = model.GetNamedNode(vmInstance.GetAttribute("business_service"));
				if ((businessService == null)
					|| (businessService.GetAttribute("owner") != "traditional"))
				{
					foreach (Node vmInstanceLinkToServer in vmInstance.GetChildrenOfType("vm_instance_link_to_server"))
					{
						Node server = model.GetNamedNode(vmInstanceLinkToServer.GetAttribute("server"));
						foreach (Node serverLinkToVmInstance in server.GetChildrenOfType("server_link_to_vm_instance"))
						{
							if (serverLinkToVmInstance.GetAttribute("vm_instance") == vmInstance.GetAttribute("name"))
							{
								server.DeleteChildTree(serverLinkToVmInstance);
							}
						}

						vmInstance.DeleteChildTree(vmInstanceLinkToServer);
					}
				}
			}
		}

		void CombineClouds ()
		{
			// First remove all locations from cloud control.
			Dictionary<Node, List<Node>> oldCloudToListOfRacks = new Dictionary<Node, List<Node>> ();
			foreach (Node cloud in model.GetNodesWithAttributeValue("type", "cpu_cloud"))
			{
				if (! cloud.GetBooleanAttribute("is_public_cloud", false))
				{
					List<Node> racks = new List<Node> ();
					bool cloudContainsTraditionalRacks = false;
					foreach (Node locationReference in cloud.GetChildrenOfType("cloud_location"))
					{
						Node rack = model.GetNamedNode(locationReference.GetAttribute("location"));
						racks.Add(rack);

						if (rack.GetAttribute("owner") == "traditional")
						{
							cloudContainsTraditionalRacks = true;
						}
					}

					if (! cloudContainsTraditionalRacks)
					{
						oldCloudToListOfRacks.Add(cloud, new List<Node> (racks));
						cloud.DeleteChildren();
					}
				}
			}

			// Now build new clouds.
			Dictionary<Node, Node> rackToNewCloud = new Dictionary<Node, Node> ();
			Node globalDevTestCloud = model.GetNamedNode("Global Dev&Test Cloud");
			foreach (Node rack in model.GetNodesWithAttributeValue("type", "rack"))
			{
				if (! rack.GetBooleanAttribute("is_cloud_rack", false))
				{
					Node datacenter = rack.Parent;
					Node business = model.GetNamedNode(datacenter.GetAttribute("business"));
					string regionName = business.GetAttribute("region");

					Node datacenterProductionCloud = model.GetNamedNode(regionName + " Production Cloud");

					Node destinationCloud = null;

					switch (rack.GetAttribute("owner"))
					{
						case "online":
						case "floor":
							destinationCloud = datacenterProductionCloud;
							break;

						case "dev&test":
							destinationCloud = globalDevTestCloud;
							break;

						case "":
						case "traditional":
							destinationCloud = null;
							break;

						default:
							System.Diagnostics.Debug.Assert(false);
							break;
					}

					if (destinationCloud != null)
					{
						List<AttributeValuePair> attributes = new List<AttributeValuePair> ();
						attributes.Add(new AttributeValuePair ("type", "cloud_location"));
						attributes.Add(new AttributeValuePair ("location", rack.GetAttribute("name")));

						new Node (destinationCloud, "cloud_location", "", attributes);

						rackToNewCloud.Add(rack, destinationCloud);
					}
				}
			}

			// Finally relocate business services.
			foreach (Node businessService in model.GetNodesWithAttributeValue("type", "business_service"))
			{
				Node oldCloud = model.GetNamedNode(businessService.GetAttribute("cloud"));
				
				if ((oldCloud != null)
					&& oldCloudToListOfRacks.ContainsKey(oldCloud))
				{
					Node newCloud = null;
					foreach (Node rack in oldCloudToListOfRacks[oldCloud])
					{
						if (rackToNewCloud.ContainsKey(rack))
						{
							newCloud = rackToNewCloud[rack];
							break;
						}
					}

					if (newCloud != null)
					{
						businessService.SetAttribute("cloud", newCloud.GetAttribute("name"));
					}
				}
			}
		}

		void ClearConsolidations ()
		{
			model.GetNamedNode("Server Consolidations").DeleteChildren();
		}

		void ClearFinances ()
		{
			model.GetNamedNode("Turnover").DeleteChildren();

			Node budgets = model.GetNamedNode("Budgets");
			Node roundBudget = null;
			foreach (Node tryRoundBudget in budgets.GetChildrenOfType("round_budget"))
			{
				if (tryRoundBudget.GetIntAttribute("round", 0) == gameFile.CurrentRound)
				{
					roundBudget = tryRoundBudget;
					break;
				}
			}

			Node businesses = model.GetNamedNode("Businesses");
			foreach (Node business in businesses.GetChildrenOfType("business"))
			{
				Node businessBudget = null;
				foreach (Node tryBusinessBudget in roundBudget.GetChildrenOfType("budget"))
				{
					if (tryBusinessBudget.GetAttribute("business") == business.GetAttribute("name"))
					{
						businessBudget = tryBusinessBudget;
						break;
					}
				}

				List<AttributeValuePair> attributes = new List<AttributeValuePair> ();
				attributes.Add(new AttributeValuePair ("dev_budget", businessBudget.GetIntAttribute("dev_budget", 0)));
				attributes.Add(new AttributeValuePair ("dev_budget_left", businessBudget.GetIntAttribute("dev_budget", 0)));
				attributes.Add(new AttributeValuePair ("it_budget", businessBudget.GetIntAttribute("it_budget", 0)));
				attributes.Add(new AttributeValuePair ("it_budget_left", businessBudget.GetIntAttribute("it_budget", 0)));
				attributes.Add(new AttributeValuePair ("target_revenue", businessBudget.GetIntAttribute("target_revenue", 0)));
				attributes.Add(new AttributeValuePair ("spend", 0));
				attributes.Add(new AttributeValuePair ("revenue_earned", 0));
				attributes.Add(new AttributeValuePair ("potential_extra_revenue_including_missed_demands", 0));
				attributes.Add(new AttributeValuePair ("potential_extra_revenue_excluding_missed_demands", 0));
				business.SetAttributes(attributes);

				foreach (Node businessService in business.GetChildrenOfType("business_service"))
				{
					attributes.Clear();
					attributes.Add(new AttributeValuePair ("revenue_earned", 0));
					attributes.Add(new AttributeValuePair ("potential_extra_revenue", 0));
					businessService.SetAttributes(attributes);
				}
			}
		}

		void ClearDemands ()
		{
			foreach (Node demand in model.GetNodesWithAttributeValue("type", "demand"))
			{
				List<AttributeValuePair> attributes = new List<AttributeValuePair> ();

				int round = roundVariables.GetIntAttribute("current_round", 0);

				bool active = demand.GetBooleanAttribute(CONVERT.Format("available_in_round_{0}", round), false)
							  && ! demand.GetBooleanAttribute("optional", false);

				int delay = demand.GetIntAttribute(CONVERT.Format("round_{0}_delay", round), 0);

				attributes.Add(new AttributeValuePair ("active", active));
				attributes.Add(new AttributeValuePair ("status", "waiting"));
				attributes.Add(new AttributeValuePair ("delay", delay));
				attributes.Add(new AttributeValuePair ("delay_countdown", delay));
				attributes.Add(new AttributeValuePair ("duration_countdown", demand.GetIntAttribute("duration", 0)));
				                                       
				demand.SetAttributes(attributes);
			}

			foreach (Node businessService in model.GetNodesWithAttributeValue("type", "business_service"))
			{
				if (! string.IsNullOrEmpty(businessService.GetAttribute("demand_name")))
				{
					Node vmInstance = model.GetNamedNode(businessService.GetAttribute("vm_instance"));
					if (vmInstance != null)
					{
						orderExecutor.ReleaseVmInstance(businessService, true);
						orderExecutor.DeleteVmInstance(vmInstance);
					}

					businessService.Parent.DeleteChildTree(businessService);
				}
			}

			foreach (Node vmInstance in model.GetNodesWithAttributeValue("type", "vm_instance"))
			{
				if (! string.IsNullOrEmpty(vmInstance.GetAttribute("demand_name")))
				{
					orderExecutor.DeleteVmInstance(vmInstance);
				}
			}
		}

		void ClearOpexFlags ()
		{
			foreach (Node item in model.GetNodesWithAttributeValue("type", "storage_array"))
			{
				item.SetAttribute("opex_charged", false);
			}

			foreach (Node item in model.GetNodesWithAttributeValue("type", "rack"))
			{
				item.SetAttribute("opex_charged", false);
			}

			foreach (Node item in model.GetNodesWithAttributeValue("type", "server"))
			{
				item.SetAttribute("opex_charged", false);
			}

			foreach (Node businessService in model.GetNodesWithAttributeValue("type", "business_service"))
			{
				businessService.SetAttribute("iaas_reservation_opex_charged", false);
			}
		}

		void CompleteAllDevelopment ()
		{
			// Advance all service development so production services go live.
			foreach (Node businessService in model.GetNodesWithAttributeValue("type", "business_service"))
			{
				List<AttributeValuePair> attributes = new List<AttributeValuePair> ();

				if (businessService.GetIntAttribute("dev_countdown", 0) > 0)
				{
					attributes.Add(new AttributeValuePair ("dev_countdown", 0));
					if (!businessService.GetBooleanAttribute("has_been_developed", false))
					{
						attributes.Add(new AttributeValuePair("has_been_developed", true));
					}
				}

				if (businessService.GetIntAttribute("handover_countdown", 0) > 0)
				{
					attributes.Add(new AttributeValuePair ("handover_countdown", 0));
				}

				if (businessService.GetIntAttribute("handover_starts_at", 0) > 0)
				{
					attributes.Add(new AttributeValuePair ("handover_starts_at", 0));
				}

				if (businessService.GetIntAttribute("handover_finishes_at", 0) > 0)
				{
					attributes.Add(new AttributeValuePair ("handover_finishes_at", -1));
				}

				if (businessService.GetIntAttribute("time_till_ready", 0) > 0)
				{
					attributes.Add(new AttributeValuePair ("time_till_ready", 0));
				}

				if (attributes.Count > 0)
				{
					businessService.SetAttributes(attributes);
				}
			}

			// Advance all hardware upgrades.
			foreach (Node server in model.GetNodesWithAttributeValue("type", "server"))
			{
				if (server.GetIntAttribute("time_till_ready", 0) > 0)
				{
					server.SetAttribute("time_till_ready", 0);
				}
			}
			foreach (Node storageArrayDelay in model.GetNodesWithAttributeValue("type", "storage_array_upgrade_delay"))
			{
				if (storageArrayDelay.GetIntAttribute("time_till_ready", 0) > 0)
				{
					storageArrayDelay.SetAttribute("time_till_ready", 0);
				}
			}

			// Release all dev hardware.
			if (roundVariables.GetIntAttribute("current_round", 0) > 1)
			{
				foreach (Node business in model.GetNodesWithAttributeValue("type", "business"))
				{
					foreach (Node businessService in business.GetChildrenOfType("business_service"))
					{
						if (businessService.GetAttribute("owner") == "dev&test")
						{
							orderExecutor.ReleaseVmInstance(businessService, false);
						}
					}

					Node businessAsUsual = ((Node []) business.GetChildrenOfType("business_as_usual").ToArray(typeof (Node)))[0];
					foreach (Node serviceBusinessAsUsual in businessAsUsual.GetChildrenOfType("service_business_as_usual"))
					{
						Node businessService = model.GetNamedNode(serviceBusinessAsUsual.GetAttribute("business_service"));
						if (businessService.GetAttribute("owner") == "dev&test")
						{
							foreach (Node dataPoint in serviceBusinessAsUsual.GetChildrenOfType("business_as_usual_data_point"))
							{
								dataPoint.SetAttribute("cpus_used", 0);
							}
						}
					}
				}
			}
		}

		public override void Dispose ()
		{
			timeNode.AttributesChanged -= new Node.AttributesChangedEventHandler (timeNode_AttributesChanged);
			incidentApplier.Dispose();
			secondsRunner.Dispose();
			orderExecutor.Dispose();

			leaderboard.Dispose();

			vmManager.Dispose();
			powerMonitor.Dispose();
			opExMonitor.Dispose();
			bauManager.Dispose();
			demandManager.Dispose();
			pvManager.Dispose();

			if (incidentLogger != null)
			{
				incidentLogger.CloseLog();
				incidentLogger.Dispose();
			}

			foreach (Node businessNode in new List<Node> (nodeToBusiness.Keys))
			{
				nodeToBusiness[businessNode].Dispose();
			}

			foreach (Node datcenterNode in nodeToDatacenter.Keys)
			{
				nodeToDatacenter[datcenterNode].Dispose();
			}

			TimeManager.TheInstance.UnmanageClass(this);
		}

		public void SetIncidentDefinitions (string xml, NodeTree model)
		{
			incidentApplier.SetIncidentDefinitions(xml, model);
		}

		void timeNode_AttributesChanged (Node sender, System.Collections.ArrayList attrs)
		{
			if (timeNode.GetIntAttribute("seconds", 0) >= timeNode.GetIntAttribute("round_duration", 0))
			{
				RaiseEvent(this);
			}
		}

		public OrderPlanner OrderPlanner
		{
			get
			{
				return orderPlanner;
			}
		}

		public BauManager BauManager
		{
			get
			{
				return bauManager;
			}
		}

		public PublicVendorManager CloudVendorManager
		{
			get
			{
				return pvManager;
			}
		}

		public override void Start ()
		{
			base.Start();

			if (incidentLogger != null)
			{
				incidentLogger.OpenLog();
			}
		}

		public override void Reset ()
		{
			base.Reset();

			PrepareNetwork(gameFile.CurrentRound);
			GlobalEventDelayer.TheInstance.Delayer.Clear();
			incidentApplier.ResetIncidents();
		}

		public VirtualMachineManager VirtualMachineManager
		{
			get
			{
				return vmManager;
			}
		}
	}
}