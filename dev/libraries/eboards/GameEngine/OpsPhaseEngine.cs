using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using LibCore;
using Network;
using IncidentManagement;
using BusinessServiceRules;
using TransitionObjects;

using CoreUtils;
using GameManagement;
using Logging;

namespace GameEngine
{
	public class OpsPhaseEngine : BaseOpsPhaseEngine
	{
		protected NetworkProgressionGameFile gameFile;

		protected IncidentApplier iApplier;
		protected MachineRebooter rebooter;
		protected BusinessServiceFixer bizFixer;

		protected BusinessServiceGroupMonitor businessServiceGroupMonitor;

		protected CostMonitor costMonitor;

		protected NodeTree _Network;
		protected ArrayList serviceMonitors;
		protected BasicIncidentLogger biLog;

		protected SecondsRunner secondsRunner;

		protected ServiceDownCounter _sdc;
		protected ServiceSpaceCalculator _spaceCalculator;

		protected MirrorApplier mirrorApplier;

		protected ISlaManager _slamanager;
		protected ProjectManager MyProjectManager;
		protected MachineUpgrader machineUpgrader;
		protected AppUpgrader appUpgrader;
		protected AWTProvisionMonitor _awtProvisionMonitor;
		protected SystemPowerMonitorBase _systempowermonitor;
		protected SystemAppLinkMonitor _systemapplinkmonitor;
		protected IDisposable _preplaymanager;
		protected ZoneTemperatureMonitor temperatureMonitor;
		protected AutoRestorer autoRestorer;
			
		protected RecurringIncidentDetector recurringIncidentDetector;

		protected IDisposable availabilityMonitor;
		protected Node CurrTimeNode = null;

		protected int round;

		public ZoneTemperatureMonitor TheZoneTemperatureMonitor
		{
			get
			{
				return temperatureMonitor;
			}
		}

		public IncidentApplier TheIncidentApplier
		{
			get
			{
				return iApplier;
			}
		}

		public MirrorApplier TheMirrorApplier
		{
			get
			{
				return mirrorApplier;
			}
		}

		public bool OpenLog()
		{
			if(null != biLog) return this.biLog.OpenLog();
			return false;
		}

		public void CloseLog()
		{
			if(null != biLog) this.biLog.CloseLog();
		}

		protected Node incoming;

		public void ResetLogger()
		{
			if(null != biLog) biLog.Reset();
		}


		public IncidentApplier getIncidentApplier()
		{
			return iApplier;
		}

		public ProjectManager getProjectManager()
		{
			return MyProjectManager;
		}

		public ArrayList GetInstallableProjects ()
		{
			return MyProjectManager.getInstallableProjects();
		}

		protected string _incidentDefsFile;

		protected static Node CreateDefaultSlas (NodeTree model)
		{
			Node slas = new Node (model.Root, "slas", "SLAs", new AttributeValuePair ("type", "slas"));

			AddDefaultSlaEntry(slas, 6, 8, 360);
			AddDefaultSlaEntry(slas, 4, 5, 360);
			AddDefaultSlaEntry(slas, 2, 3, 360);
			AddDefaultSlaEntry(slas, 1, 1, 360);
			AddDefaultSlaEntry(slas, 0, 0, 360);

			return slas;
		}

		static Node AddDefaultSlaEntry (Node slas, int minStreams, int maxStreams, int mtrs)
		{
			List<AttributeValuePair> attributes = new List<AttributeValuePair>();

			attributes.Add(new AttributeValuePair("type", "sla"));
			attributes.Add(new AttributeValuePair("revenue_streams_min", minStreams));
			attributes.Add(new AttributeValuePair("revenue_streams_max", maxStreams));
			attributes.Add(new AttributeValuePair("slalimit", mtrs));

			return new Node(slas, "sla", "", attributes);
		}

		public virtual void BringEverythingUp ()
		{
			BringEverythingUp(_Network, round);
		}

		public static void BringEverythingUp (NodeTree net, int round)
		{
			var currentRoundNode = net.GetNamedNode("CurrentRound");
			if (currentRoundNode != null)
			{
				currentRoundNode.SetAttribute("round", round);
			}

			if (net.GetNamedNode("slas") == null)
			{
				CreateDefaultSlas(net);
			}

			Node errors = net.GetNamedNode("FacilitatorNotifiedErrors");
			if (errors != null)
			{
				errors.DeleteChildren();
			}

			Node roundVariables = net.GetNamedNode("RoundVariables");
			if (roundVariables == null)
			{
				roundVariables = new Node (net.Root, "round_variables", "RoundVariables",
				                           new AttributeValuePair("type", "round_variables"));
			}
			roundVariables.SetAttribute("current_round", round);

			string baselineTemperature = "72.3";

			Hashtable nodes = net.GetNodesWithAttribute("incident_id");
			foreach (Node n in nodes.Keys)
			{
				n.SetAttribute("incident_id", "");
			}

			nodes = net.GetNodesWithAttribute("turnedoff");
			foreach (Node n in nodes.Keys)
			{
				// Leave aircon nodes turned off between rounds, as being turned off isn't a fault.
				if (!n.GetAttribute("name").ToLower().StartsWith("aircon"))
				{
					n.SetAttribute("turnedoff", "false");
				}
			}

			nodes = net.GetNodesWithAttribute("powering_down");
			foreach (Node n in nodes.Keys)
			{
				n.SetAttribute("powering_down", "false");
			}

			nodes = net.GetNodesWithAttribute("power_overload");
			foreach (Node n in nodes.Keys)
			{
				n.SetAttribute("power_overload", "false");
			}

			nodes = net.GetNodesWithAttribute("power_tripped");
			foreach (Node n in nodes.Keys)
			{
				n.SetAttribute("power_tripped", "false");
			}

			nodes = net.GetNodesWithAttribute("has_some_children_turned_off");
			foreach (Node n in nodes.Keys)
			{
				n.SetAttribute("has_some_children_turned_off", "false");
			}

			nodes = net.GetNodesWithAttribute("nopower");
			foreach (Node n in nodes.Keys)
			{
				n.SetAttribute("nopower", "false");
			}

			nodes = net.GetNodesWithAttribute("goingDownInSecs");
			foreach (Node n in nodes.Keys)
			{
				n.SetAttribute("goingDownInSecs", "");
			}

			ArrayList nodes2 = net.GetNodesWithAttributeValue("up", "false");
			foreach (Node n in nodes2)
			{
				n.SetAttribute("up", "true");
			}

			nodes = net.GetNodesWithAttribute("rebootingForSecs");
			foreach (Node n in nodes.Keys)
			{
				n.SetAttribute("rebootingForSecs", "");
			}

			nodes = net.GetNodesWithAttribute("slabreach");
			foreach (Node n in nodes.Keys)
			{
				n.SetAttribute("slabreach", "false");
			}

			nodes = net.GetNodesWithAttribute("downForSecs");
			foreach (Node n in nodes.Keys)
			{
				n.SetAttribute("downForSecs", "");
			}

			nodes = net.GetNodesWithAttribute("virtualmirrorinuse");
			foreach (Node n in nodes.Keys)
			{
				n.SetAttribute("virtualmirrorinuse", "false");
			}

			nodes = net.GetNodesWithAttribute("mirrorForSecs");
			foreach (Node n in nodes.Keys)
			{
				n.SetAttribute("mirrorForSecs", "");
			}

			nodes = net.GetNodesWithAttribute("users_down");
			foreach (Node n in nodes.Keys)
			{
				n.SetAttribute("users_down", "");
			}

			nodes2 = net.GetNodesWithAttributeValue("canworkaround", "true");
			foreach (Node n in nodes2)
			{
				n.SetAttribute("canworkaround", "");
			}

			nodes2 = net.GetNodesWithAttributeValue("fixable", "true");
			foreach (Node n in nodes2)
			{
				n.SetAttribute("fixable", "");
			}

			nodes2 = net.GetNodesWithAttributeValue("denial_of_service", "true");
			foreach (Node n in nodes2)
			{
				n.SetAttribute("denial_of_service", "false");
			}

			nodes2 = net.GetNodesWithAttributeValue("security_flaw", "true");
			foreach (Node n in nodes2)
			{
				n.SetAttribute("security_flaw", "false");
			}

			nodes2 = net.GetNodesWithAttributeValue("compliance_incident", "true");
			foreach (Node n in nodes2)
			{
				n.SetAttribute("compliance_incident", "false");
			}

			nodes2 = net.GetNodesWithAttributeValue("thermal", "true");
			foreach (Node n in nodes2)
			{
				n.SetAttribute("thermal", "false");
			}

			nodes = net.GetNodesWithAttribute("initial_baseline_temperature");
			foreach (Node n in nodes.Keys)
			{
				double temperature = n.GetDoubleAttribute("initial_baseline_temperature", 0);
				ArrayList attrs = new ArrayList();
				attrs.Add(new AttributeValuePair("baseline_temperature", temperature));
				attrs.Add(new AttributeValuePair("temperature", temperature));
				n.SetAttributes(attrs);
			}

			nodes = net.GetNodesWithAttribute("temperature");
			foreach (Node n in nodes.Keys)
			{
				string zone = n.GetAttribute("zone");
				if (zone == "")
				{
					zone = n.GetAttribute("proczone");
				}

				Node cooling = net.GetNamedNode("C" + zone);
				double temperature = 72;
				if (cooling != null)
				{
					temperature = cooling.GetDoubleAttribute("baseline_temperature", temperature);
				}
				n.SetAttribute("temperature", temperature);
			}

			nodes = net.GetNodesWithAttribute("goal_temperature");
			foreach (Node n in nodes.Keys)
			{
				n.SetAttribute("goal_temperature", baselineTemperature);
			}

			nodes = net.GetNodesWithAttribute("goal_temperature_start");
			foreach (Node n in nodes.Keys)
			{
				n.SetAttribute("goal_temperature_start", baselineTemperature);
			}

			nodes = net.GetNodesWithAttribute("goal_temperature_change_duration");
			foreach (Node n in nodes.Keys)
			{
				n.SetAttribute("goal_temperature_change_duration", "1");
			}

			nodes = net.GetNodesWithAttribute("goal_temperature_start_time");
			foreach (Node n in nodes.Keys)
			{
				n.SetAttribute("goal_temperature_start_time", "0");
			}

			nodes = net.GetNodesWithAttribute("workaround");
			foreach (Node n in nodes.Keys)
			{
				n.SetAttribute("workaround", "");
			}

			nodes = net.GetNodesWithAttribute("workingaround");
			foreach (Node n in nodes.Keys)
			{
				n.SetAttribute("workingaround", "");
			}

			// Wipe any incoming requests (pending)...
			Node projectIncomingRequestQueueHandle = net.GetNamedNode("ProjectsIncomingRequests");
			if (projectIncomingRequestQueueHandle != null)
			{
				ArrayList children = (ArrayList) projectIncomingRequestQueueHandle.getChildren().Clone();
				if (children.Count > 0)
				{
					foreach (Node n in children)
					{
						projectIncomingRequestQueueHandle.DeleteChildTree(n);
					}
				}
			}

			// Wipe any current audits
			Node audits = net.GetNamedNode("Audits");
            if (audits != null)
            {
                foreach (Node audit in audits.getChildren())
                {
                    List<AttributeValuePair> attributes = new List<AttributeValuePair>();
                    attributes.Add(new AttributeValuePair("active", false));
                    attributes.Add(new AttributeValuePair("timer", 0));
                    audit.SetAttributes(attributes);
                }
            }

			// Wipe any incoming app ugrade requests...
			Node appUpgradeQueue = net.GetNamedNode("AppUpgradeQueue");
			if (appUpgradeQueue != null)
			{
				var children = (ArrayList) appUpgradeQueue.getChildren().Clone();
				if (children.Count > 0)
				{
					foreach (Node n in children)
					{
						appUpgradeQueue.DeleteChildTree(n);
					}
				}
			}

			// Wipe any incoming machine ugrade requests...
			Node machineUpgradeQueue = net.GetNamedNode("MachineUpgradeQueue");
			if (machineUpgradeQueue != null)
			{
				var children = (ArrayList) machineUpgradeQueue.getChildren().Clone();
				if (children.Count > 0)
				{
					foreach (Node n in children)
					{
						machineUpgradeQueue.DeleteChildTree(n);
					}
				}
			}

			Node auditsNode = net.GetNamedNode("Audits");
			if (auditsNode != null)
			{
				foreach (Node audit in auditsNode.getChildren())
				{
					audit.SetAttribute("timer", 0);
				}
			}
		}

		protected string roundSecs;

		/// <summary>
		/// Accessor for the Round Phase in Seconds
		/// </summary>
		/// <returns>Round length in Secs</returns>
		public int getRoundLengthSecs()
		{
			return CONVERT.ParseInt(roundSecs);
		}

		/// <summary>
		/// Accessor for the Round Phase in Seconds
		/// </summary>
		/// <returns>Round length in Mins</returns>
		public int getRoundLengthMins()
		{
			return (CONVERT.ParseInt(roundSecs))/60;
		}

		/// <summary>
		/// Setter for the Round Phase Length
		/// </summary>
		/// <param name="mins"></param>
		public void SetRoundMinutes(int mins)
		{
			int secs = mins * 60;
			roundSecs = CONVERT.ToStr(secs);

			if (CurrTimeNode.GetIntAttribute("round_duration_secs", 0) != CONVERT.ParseInt(roundSecs))
			{
				CurrTimeNode.SetAttribute("round_duration_secs", roundSecs);
			}
		}

		/// <summary>
		/// Constructors for the Engine
		/// </summary>
		/// <param name="n">The Node Tree</param>
		/// <param name="roundDir">The File Directory for the Round Data Files</param>
		/// <param name="incidentDefsFile">Definition Files for the incidents xml</param>
		/// <param name="round">Which round</param>
		public OpsPhaseEngine (NetworkProgressionGameFile gameFile, string roundDir, string incidentDefsFile, int round, bool logResults)
		{
			this.gameFile = gameFile;
			this.round = round;
            _Network = gameFile.NetworkModel;
			TimeManager.TheInstance.ManageClass(this);

			//Reset the Clock time to 10
			Node timeNode = gameFile.NetworkModel.GetNamedNode("CurrentTime");
			timeNode.SetAttribute("seconds","0");

			BringEverythingUp();

			PreRoundProcessing(gameFile.NetworkModel, this.round);
			
			serviceMonitors = new ArrayList();
			_incidentDefsFile = incidentDefsFile;

			ResetCalendar();

			//
			// Clear the EventDelayers...
			//
			GlobalEventDelayer.TheInstance.DestroyEventDelayer();
			GlobalEventDelayer.TheInstance.SetEventDelayer( new EventDelayer() );
			GlobalEventDelayer.TheInstance.Delayer.SetModelCounter(_Network, "CurrentTime", "seconds");
			OnAttributeHitApplier.TheInstance.Clear();

			if(logResults)
			{
				biLog = new BasicIncidentLogger();
				biLog.LogTreeToFile(_Network, roundDir + "\\NetworkIncidents.log");
				biLog.OpenLog();
			}
			else
			{
				biLog = null;
			}
			//
            recurringIncidentDetector = CreateRecurringIncidentDetector(_Network);
			//
			_sdc = CreateServiceDownCounter(_Network);
			//
			iApplier = CreateIncidentApplier(_Network);

			mirrorApplier = CreateMirrorApplier(_Network);
			rebooter = CreateRebooter(_Network);

			bizFixer = CreateBusinessServiceFixer(_sdc, iApplier);

			machineUpgrader = CreateMachineUpgrader(_Network);
			appUpgrader = CreateAppUpgrader(_Network);

			costMonitor = CreateCostMonitor(_Network);

			SetupSpaceMonitor();

			businessServiceGroupMonitor = CreateBusinessServiceGroupMonitor(_Network);

			_awtProvisionMonitor = new AWTProvisionMonitor(_Network);

			SetupPowerMonitor();
			_systemapplinkmonitor = CreateSystemAppLinkMonitor(_Network);
			_preplaymanager = CreatePrePlayManager(_Network);

			SetupTemperatureMonitor();
			SetupAutoRestorer(_Network);

			_slamanager = CreateSlaManager(_Network);
			MyProjectManager = CreateProjectManager(_Network);

			PreIncidentsInitialisation();

			LoadIncidentDefinitions(incidentDefsFile);

			incoming = _Network.GetNamedNode("enteredIncidents");
			if(null == incoming)
			{
				incoming = new Node(_Network.Root, "enteredIncidents","enteredIncidents", (ArrayList)null);
			}
			iApplier.IncidentEntryQueue = incoming;
			//

			if (rebooter != null)
			{
				rebooter.TargetTree = _Network;
			}

			if (bizFixer != null)
			{
				bizFixer.TargetTree = _Network;
			}

			availabilityMonitor = CreateAvailabilityMonitor(_Network);

			//Clear down the SLA braech Flags on all the Business Service Users 
			if (_slamanager != null)
			{
				_slamanager.ResetBsuSlaBreaches();
			}

			//
			// Create a SecondsRunner and watch the current time being set to 25 minutes which is
			// the end of the race.
			//
			secondsRunner = new SecondsRunner(_Network);

			CurrTimeNode = _Network.GetNamedNode("CurrentTime");

		    int roundDuration = SkinningDefs.TheInstance.GetIntData("round_duration_secs", 1500);
		    roundSecs = CONVERT.ToStr(CurrTimeNode.GetIntAttribute("round_duration_secs", roundDuration));
		    
            SetRoundMinutes(CONVERT.ParseInt(roundSecs) / 60);

			CurrTimeNode.AttributesChanged += OpsPhaseEngine_AttributesChanged;
			//
			if(null != biLog) biLog.CloseLog();
			TimeManager.TheInstance.Stop();

			UpdateEditedCosts(gameFile);

			AttachServiceMonitors();
		}

		public void UpdateEditedCosts (NetworkProgressionGameFile gameFile)
		{
			UpdateBudgetFromDefinedCosts(gameFile.NetworkModel, gameFile, gameFile.CurrentRound);

			var roundCostsFilename = gameFile.GetGlobalFile($"costs_r{gameFile.CurrentRound}.xml");
			if (File.Exists(roundCostsFilename))
			{
				var roundCostsXml = BasicXmlDocument.CreateFromFile(roundCostsFilename);
				foreach (XmlElement cost in roundCostsXml.DocumentElement.ChildNodes)
				{
					var value = cost.GetDoubleAttribute("cost");
					var modelName = cost.GetAttribute("model_value");
					if ((! string.IsNullOrEmpty(modelName))
						&& (value != null))
					{
						modelName = modelName.Replace("{round}", CONVERT.ToStr(round));
						var nodeName = modelName.Substring(0, modelName.IndexOf("."));
						var attributeName = modelName.Substring(modelName.IndexOf(".") + 1);

						var node = gameFile.NetworkModel.GetNamedNode(nodeName);
						node.SetAttribute(attributeName, value.Value);
					}
				}
			}
		}

		protected virtual IDisposable CreatePrePlayManager (NodeTree _Network)
		{
			return new PreplayManager(_Network);
		}

		protected virtual IDisposable CreateAvailabilityMonitor (NodeTree _Network)
		{
			return new AvailabilityMonitor(_Network, "Business Services Group");
		}

		protected virtual ProjectManager CreateProjectManager (NodeTree _Network)
		{
			return new ProjectManager(_Network, round, false);
		}

		protected virtual SystemAppLinkMonitor CreateSystemAppLinkMonitor (NodeTree _Network)
		{
			return new SystemAppLinkMonitor(_Network);
		}

		protected virtual BusinessServiceGroupMonitor CreateBusinessServiceGroupMonitor (NodeTree _Network)
		{
			return new BusinessServiceGroupMonitor(_Network.GetNamedNode("Business Services Group"));
		}

		protected virtual CostMonitor CreateCostMonitor (NodeTree _Network)
		{
			return new CostMonitor ("CostedEvents", _Network, AppInfo.TheInstance.Location + "\\data\\costs.xml");
		}

        protected virtual RecurringIncidentDetector CreateRecurringIncidentDetector(NodeTree network)
        {
            return new RecurringIncidentDetector(network);
        }

		protected virtual void ResetCalendar ()
		{
			_Network.GetNamedNode("Calendar")?.SetAttribute("days", "1");
		}

		protected virtual void LoadIncidentDefinitions (string incidentsDefFile) 
		{
			iApplier.SetIncidentDefinitions(System.IO.File.ReadAllText(incidentsDefFile), _Network);
		}

		protected virtual MachineUpgrader CreateMachineUpgrader (NodeTree model)
		{
			return new MachineUpgrader (model, false);
		}

		protected virtual AppUpgrader CreateAppUpgrader (NodeTree model)
		{
			return new AppUpgrader (model, false);
		}

		protected virtual MirrorApplier CreateMirrorApplier (NodeTree model)
		{
			return new MirrorApplier (model);
		}

		protected virtual MachineRebooter CreateRebooter (NodeTree model)
		{
			return new MachineRebooter (model);
		}

		protected virtual ISlaManager CreateSlaManager (NodeTree model)
		{
			return new SLAManager (model);
		}

		protected virtual IncidentApplier CreateIncidentApplier (NodeTree _Network)
		{
			return new IncidentApplier (_Network);
		}

		protected virtual void PreIncidentsInitialisation ()
		{
		}

		protected virtual BusinessServiceFixer CreateBusinessServiceFixer (ServiceDownCounter _sdc, IncidentApplier iApplier)
		{
			return new BusinessServiceFixer(_sdc, iApplier);
		}

		/// <summary>
		/// This is used to perform actions on the network before we connect components to it and start logging activities
		/// 
		/// </summary>
		public virtual void PreRoundProcessing(NodeTree n, int round)
		{
			
		}

		public virtual void SetupSpaceMonitor()
		{
			_spaceCalculator = new ServiceSpaceCalculator(_Network, BusinessServiceRules.ServiceSpaceCalculator.RuleStyle.LOG_ON_NO_RESOURCE);
		}


		public virtual void SetupPowerMonitor()
		{
			_systempowermonitor = new SystemPowerMonitor(_Network);			
		}

		public virtual void SetupTemperatureMonitor ()
		{
			temperatureMonitor = new ZoneTemperatureMonitor (_Network);
		}

		public virtual void SetupAutoRestorer (NodeTree model)
		{
			autoRestorer = new AutoRestorer (model);
		}

		public void SetupIncidents()
		{
			iApplier.ResetIncidents();
		}

		/// <summary>
		/// 
		/// </summary>
		protected void CloseLogger()
		{
			if(null != biLog)
			{
				biLog.Dispose();
				biLog = null;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public override void Dispose()
		{
			if(null != biLog)
			{
				CloseLogger();
			}

			TimeManager.TheInstance.UnmanageClass(this);
			//GlobalEventDelayer.TheInstance.Delayer.Clear();
			OnAttributeHitApplier.TheInstance.Clear();
			GlobalEventDelayer.TheInstance.DestroyEventDelayer();
			//GlobalEventDelayer.TheInstance.Delayer.Detach();

			if(_sdc != null)
			{
				_sdc.Clear();
				_sdc.Dispose();
				_sdc = null;
			}

			if (iApplier != null)
			{
				iApplier.Dispose(); iApplier = null;
			}

			if(bizFixer != null) { bizFixer.Dispose(); bizFixer = null; }
			if(rebooter != null) { rebooter.Dispose(); rebooter = null; }
			if(mirrorApplier != null) { mirrorApplier.Dispose(); mirrorApplier = null; }
			if(machineUpgrader != null) { machineUpgrader.Dispose(); machineUpgrader = null; }
			if(appUpgrader != null) { appUpgrader.Dispose(); appUpgrader = null; }
			if(costMonitor != null) { costMonitor.Dispose(); costMonitor = null; }
			if(_spaceCalculator != null) { _spaceCalculator.Dispose(); _spaceCalculator = null; }
			if(businessServiceGroupMonitor != null) { businessServiceGroupMonitor.Dispose(); businessServiceGroupMonitor = null; }
			if(_awtProvisionMonitor != null) { _awtProvisionMonitor.Dispose(); _awtProvisionMonitor = null; }
			if(availabilityMonitor != null) { availabilityMonitor.Dispose(); availabilityMonitor = null; }
			if(_slamanager != null) { _slamanager.Dispose(); _slamanager = null; }
			if(MyProjectManager != null) { MyProjectManager.Dispose(); MyProjectManager = null; }
			if(secondsRunner != null) { secondsRunner.Dispose(); secondsRunner = null; }
			if(_systempowermonitor != null) { _systempowermonitor.Dispose(); _systempowermonitor = null; }
			if(_systemapplinkmonitor != null) { _systemapplinkmonitor.Dispose(); _systemapplinkmonitor = null; }
			if(recurringIncidentDetector != null) { recurringIncidentDetector.Dispose(); recurringIncidentDetector = null; }
			if(_preplaymanager != null) { _preplaymanager.Dispose(); _preplaymanager = null; }
			if (temperatureMonitor != null) { temperatureMonitor.Dispose(); temperatureMonitor = null; }

			//missing disposes
			if (CurrTimeNode != null)
			{
				CurrTimeNode.AttributesChanged -= OpsPhaseEngine_AttributesChanged;
				CurrTimeNode = null;
			}

			if (this.iApplier != null) {this.iApplier.Dispose(); this.iApplier = null;}
			if (this.incoming != null) {this.incoming = null;}
			if (this._Network != null) {this._Network = null;}
			if (this.serviceMonitors.Count >0)
			{
				ArrayList kill_list = new ArrayList();
				foreach(MultipleServiceMonitor msm in serviceMonitors)
				{
					kill_list.Add(msm);
				}
				foreach(MultipleServiceMonitor msm in kill_list)
				{
					serviceMonitors.Remove(msm);
					msm.Dispose();
				}
				serviceMonitors.Clear();
			}

			CloseLogger();
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="namedNode"></param>
		public void AddMultipleServiceMonitor(string namedNode)
		{
			bool close_again = false;
			if(null != biLog) close_again = this.biLog.OpenLog();
			MultipleServiceMonitor msm = new MultipleServiceMonitor(namedNode, _Network,_sdc);
			serviceMonitors.Add(msm);
			if(close_again)
			{
				if(null != biLog) this.biLog.CloseLog();
			}
		}

		#region ITimedClass Members

		public override void Start()
		{
			// TODO:  Add OpsPhaseEngine.Start implementation
			base.Start();
		}

		public override void Stop()
		{
			// TODO:  Add OpsPhaseEngine.Stop implementation
			base.Stop();
		}

		public override void Reset()
		{
			base.Reset();
			this._Network.GetNamedNode("CurrentTime").SetAttribute("seconds",0);
			GlobalEventDelayer.TheInstance.Delayer.Clear();
			iApplier.ResetIncidents();
		}

		public override void FastForward(double timesRealTime)
		{
		}

		#endregion

		public static void FixAllServices(Network.NodeTree model)
		{
			Node FixItQueue = model.GetNamedNode("FixItQueue");
			ArrayList list = new ArrayList();
			list.Add( "biz_service_user" );
			Hashtable typeToNodeArray = model.GetNodesOfAttribTypes(list);

			foreach(Node bizNode in typeToNodeArray.Keys)
			{
				AttributeValuePair avp = new AttributeValuePair("target", bizNode.GetAttribute("name") );
				Node n = new Node(FixItQueue,"fix","",avp);
			}
		}

		protected virtual void OpsPhaseEngine_AttributesChanged(Node sender, ArrayList attrs)
		{
			// The seconds clock has been moved on.
			// Check if we have hit the end of the round (25 minutes).
			foreach(AttributeValuePair avp in attrs)
			{
				if("seconds" == avp.Attribute)
				{
					if(roundSecs == avp.Value)
					{
						RaiseEvent(this);
					}
				}
			}
		}

		protected virtual ServiceDownCounter CreateServiceDownCounter (NodeTree model)
		{
			if (SkinningDefs.TheInstance.GetBoolData("use_impact_based_slas", false))
			{
				return new ServiceDownCounterWithImpactBasedSlas(model);
			}
			else
			{
				return new ServiceDownCounter (model);
			}
		}

		public ServiceDownCounter ServiceDownCounter
		{
			get
			{
				return _sdc;
			}
		}

		/// <summary>
		/// This is used to extract the support budget from the costs file
		/// The facilitator is allowed to alter the support prior to the round
		/// </summary>
		/// <param name="tmpNetwork"></param>
		/// <param name="tmpGameFile"></param>
		/// <param name="tmpRound"></param>
		void UpdateBudgetFromDefinedCosts (NodeTree tmpNetwork, GameFile tmpGameFile, int tmpRound)
		{
			int support_budget = 0;
			bool processed = false;
			//Extract the information
			string NetworkFile = tmpGameFile.Dir + "\\global\\costs_r" + ((tmpRound).ToString()) + ".xml";
			var xml = LibCore.BasicXmlDocument.CreateFromFile(NetworkFile);
			//Iterate through to see if there is a support cost
			foreach (System.Xml.XmlNode cost in xml.DocumentElement.ChildNodes)
			{
				if (cost.Attributes["type"] != null)
				{
					if (cost.Attributes["type"].Value == "supportbudget")
					{
						if (cost.Attributes["cost"] != null)
						{
							support_budget = CONVERT.ParseIntSafe(cost.Attributes["cost"].Value, 0);
							processed = true;
						}
					}
				}
			}

			if (processed)
			{
				Node bnode = tmpNetwork.GetNamedNode("Budget");
				if (bnode != null)
				{
					bnode.SetAttribute("overall_budget", CONVERT.ToStr(support_budget + 5000000));
				}
			}
		}

		void AttachServiceMonitors ()
		{
			string BizEntityName = CoreUtils.SkinningDefs.TheInstance.GetData("biz");
			AddMultipleServiceMonitor(BizEntityName + " 1");
			AddMultipleServiceMonitor(BizEntityName + " 2");
			AddMultipleServiceMonitor(BizEntityName + " 3");
			AddMultipleServiceMonitor(BizEntityName + " 4");
		}
	}
}