using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Text;
using System.IO;
using System.Xml;

using CoreUtils;
using Logging;

using Polestar_PM.DataLookup;

using GameEngine;
using GameManagement;

using LibCore;
using Network;
using IncidentManagement;
using BusinessServiceRules;

namespace Polestar_PM.OpsEngine
{
	public class PM_OpsEngine : BaseOpsPhaseEngine
	{
		private IncidentApplier iApplier;
		//private MachineRebooter rebooter;
		private BusinessServiceFixer bizFixer;
		//BusinessServiceRules.ModelTimeManager mt_manager = null;
		TaskManager taskManager = null;
		private BusinessServiceGroupMonitor businessServiceGroupMonitor;

		protected NodeTree _Network;
		private ArrayList serviceMonitors;
		protected BasicIncidentLogger biLog;

		private ServiceStatusManager service_status_manager;
		private ServiceSpaceCalculator _spaceCalculator;
		protected SecondsRunner secondsRunner;
		private DayRunner dayRunner;
		private OpsManager opsManager;

		private ProjectManager projectManager;
		private PredictedInfoManager predictInformManager;
		private AlarmHandler myKlaxonHandler;
		
		private ServiceDownCounter _sdc;
		private MirrorApplier mirrorApplier;
		private SLAManager _slamanager;
		private SystemAppLinkMonitor _systemapplinkmonitor;
		private PreplayManager _preplaymanager;
		AutoRestorer autoRestorer;
		
		CommsMessageTimer messageTimer;
			
		protected RecurringIncidentDetector recurringIncidentDetector;
		private AvailabilityMonitor availabilityMonitor;
		private Node CurrTimeNode = null;
		protected Node CurrDayNode = null;

		protected NetworkProgressionGameFile _gameFile;

		protected int round;
		protected string _incidentDefsFile;

		public event EventHandler TaskProcessed;

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

		public void CalculateProjectedBenefits()
		{
			int transactionBenefit = 0;
			int costReductionBenefit = 0;

			this.projectManager.CalculateProjectedBenefitsForAllProjects(
				out transactionBenefit, out costReductionBenefit);

			Node pmiNode = _Network.GetNamedNode("predicted_market_info");

			ArrayList attrs = new ArrayList();
			attrs.Add(new AttributeValuePair("transactions_gain", CONVERT.ToStr(transactionBenefit)));
			attrs.Add(new AttributeValuePair("cost_reduction", CONVERT.ToStr(costReductionBenefit)));
			attrs.Add(new AttributeValuePair("displaytime", CONVERT.ToStr(30)));
			attrs.Add(new AttributeValuePair("displaytext", "true"));
			pmiNode.SetAttributes(attrs);
		}

		public void BringEverythingUp(NodeTree net)
		{
			Node roundResults = net.GetNamedNode("round_results");
			roundResults.SetAttribute("cumulative_cost_reduction_at_start_of_round",
			                          roundResults.GetIntAttribute("cumulative_cost_reduction_at_start_of_round", 0)
									  + roundResults.GetIntAttribute("cost_reduction_achieved", 0));
			roundResults.SetAttribute("cost_reduction_achieved", 0);

			Node financial_results_node = net.GetNamedNode("fin_results");
			int fixedCosts = financial_results_node.GetIntAttribute("fixed_costs", 0);
			fixedCosts -= roundResults.GetIntAttribute("cumulative_cost_reduction_at_start_of_round", 0);
			financial_results_node.SetAttribute("fixed_costs", fixedCosts);

			Hashtable nodes = net.GetNodesWithAttribute("incident_id");
			foreach(Node n in nodes.Keys)
			{
				n.SetAttribute("incident_id", "");
			}

			nodes = net.GetNodesWithAttribute("goingDownInSecs");
			foreach(Node n in nodes.Keys)
			{
				n.SetAttribute("goingDownInSecs", "");
			}

			ArrayList nodes2 = net.GetNodesWithAttributeValue("up","false");
			foreach(Node n in nodes2)
			{
				n.SetAttribute("up","true");
			}

			nodes = net.GetNodesWithAttribute("rebootingForSecs");
			foreach(Node n in nodes.Keys)
			{
				n.SetAttribute("rebootingForSecs", "");
			}

			nodes = net.GetNodesWithAttribute("slabreach");
			foreach(Node n in nodes.Keys)
			{
				n.SetAttribute("slabreach", "false");
			}

			nodes = net.GetNodesWithAttribute("downForSecs");
			foreach(Node n in nodes.Keys)
			{
				n.SetAttribute("downForSecs", "");
			}

			nodes = net.GetNodesWithAttribute("mirrorForSecs");
			foreach(Node n in nodes.Keys)
			{
				n.SetAttribute("mirrorForSecs", "");
			}

			nodes = net.GetNodesWithAttribute("users_down");
			foreach(Node n in nodes.Keys)
			{
				n.SetAttribute("users_down", "");
			}

			nodes2 = net.GetNodesWithAttributeValue("canworkaround","true");
			foreach(Node n in nodes2)
			{
				n.SetAttribute("canworkaround","");
			}

			nodes2 = net.GetNodesWithAttributeValue("fixable","true");
			foreach(Node n in nodes2)
			{
				n.SetAttribute("fixable","");
			}

			nodes2 = net.GetNodesWithAttributeValue("denial_of_service","true");
			foreach(Node n in nodes2)
			{
				n.SetAttribute("denial_of_service","false");
			}

			nodes = net.GetNodesWithAttribute("workaround");
			foreach(Node n in nodes.Keys)
			{
				n.SetAttribute("workaround", "");
			}

			nodes = net.GetNodesWithAttribute("workingaround");
			foreach(Node n in nodes.Keys)
			{
				n.SetAttribute("workingaround", "");
			}

			// Wipe any incoming requests (pending)...
			Node projectIncomingRequestQueueHandle = net.GetNamedNode("ProjectsIncomingRequests");
			ArrayList children = (ArrayList) projectIncomingRequestQueueHandle.getChildren().Clone();
			if (children.Count > 0)
			{
				foreach (Node n in children)
				{
					projectIncomingRequestQueueHandle.DeleteChildTree(n);
				}
			}

			// Wipe any incoming app ugrade requests...
			Node appUpgradeQueue = net.GetNamedNode("AppUpgradeQueue");
			children = (ArrayList) appUpgradeQueue.getChildren().Clone();
			if (children.Count > 0)
			{
				foreach (Node n in children)
				{
					appUpgradeQueue.DeleteChildTree(n);
				}
			}

			// Wipe any incoming machine ugrade requests...
			Node machineUpgradeQueue = net.GetNamedNode("MachineUpgradeQueue");
			children = (ArrayList) machineUpgradeQueue.getChildren().Clone();
			if (children.Count > 0)
			{
				foreach (Node n in children)
				{
					machineUpgradeQueue.DeleteChildTree(n);
				}
			}
		}

		/// <summary>
		/// Default round length is 25 mins * 60 seconds.
		/// </summary>
		protected string roundSecs = "1500";

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
		}

		/// <summary>
		/// Constructors for the Engine
		/// </summary>
		/// <param name="n">The Node Tree</param>
		/// <param name="roundDir">The File Directory for the Round Data Files</param>
		/// <param name="incidentDefsFile">Definition Files for the incidents xml</param>
		/// <param name="round">Which round</param>
		/// 
		public PM_OpsEngine(NetworkProgressionGameFile gameFile, NodeTree n, string roundDir, string incidentDefsFile, int round, bool logResults)
		{
			_gameFile = gameFile;
			this.round = round;
			TimeManager.TheInstance.ManageClass(this);

			ProjectLookup.TheInstance.lookupData(round);

			SetRoundMinutes(25);

			//Reset the Clock time to 10
			Node timeNode = n.GetNamedNode("CurrentTime");
			timeNode.SetAttribute("seconds","0");

			CurrDayNode = n.GetNamedNode("CurrentDay");
			CurrDayNode.AttributesChanged +=new Network.Node.AttributesChangedEventHandler(CurrDayNode_AttributesChanged);

			BringEverythingUp(n);

			_Network = n;
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
				biLog = new BasicIncidentLogger(true);
				biLog.LogTreeToFile(_Network, roundDir + "\\NetworkIncidents.log");
				biLog.OpenLog();
			}
			else
			{
				biLog = null;
			}
			//
			//recurringIncidentDetector = new RecurringIncidentDetector(_Network);
			//
			_sdc = new ServiceDownCounter(_Network);
			//
			iApplier = new IncidentApplier(_Network);

			mirrorApplier = new MirrorApplier(_Network);
			bizFixer = new BusinessServiceFixer(_sdc, iApplier);

			_spaceCalculator = new ServiceSpaceCalculator(_Network, BusinessServiceRules.ServiceSpaceCalculator.RuleStyle.FAIL_APP_ON_NO_RESOURCE);

			businessServiceGroupMonitor = new BusinessServiceGroupMonitor( _Network.GetNamedNode("Business Services Group") );

			_systemapplinkmonitor = new SystemAppLinkMonitor(_Network);
			_preplaymanager = new PreplayManager(_Network);

			SetupAutoRestorer(_Network);
			messageTimer = new CommsMessageTimer (_Network);

			_slamanager = new SLAManager(_Network);
			//
			//
			//iApplier.TargetTree = _Network;
			//
			System.IO.StreamReader file = new System.IO.StreamReader(incidentDefsFile);
			string xmldata = file.ReadToEnd();
			iApplier.SetIncidentDefinitions(xmldata, _Network);
			file.Close();
			file = null;
			//
			//
			incoming = _Network.GetNamedNode("enteredIncidents");
			if(null == incoming)
			{
				incoming = new Node(_Network.Root, "enteredIncidents","enteredIncidents", (ArrayList)null);
			}
			iApplier.IncidentEntryQueue = incoming;
			//
			bizFixer.TargetTree = _Network;

			availabilityMonitor = new AvailabilityMonitor(_Network, "Business Services Group");

			//Clear down the SLA braech Flags on all the Business Service Users 
			_slamanager.ResetBsuSlaBreaches();

			//
			// Create a SecondsRunner and watch the current time being set to 25 minutes which is
			// the end of the race.
			//
			secondsRunner = new SecondsRunner(_Network);
			dayRunner = new DayRunner(_Network);
			CurrTimeNode = _Network.GetNamedNode("CurrentTime");
			CurrTimeNode.AttributesChanged += new Network.Node.AttributesChangedEventHandler(CurrTimeNode_AttributesChanged);

			//
			if(null != biLog) biLog.CloseLog();
			TimeManager.TheInstance.Stop();

			//mt_manager = new BusinessServiceRules.ModelTimeManager(_Network);
			service_status_manager = new ServiceStatusManager(_Network);
			taskManager = new TaskManager (this, _Network);
			taskManager.TaskProcessed += new EventHandler (taskManager_TaskProcessed);
			projectManager = new ProjectManager(gameFile, _Network, round);
			predictInformManager = new PredictedInfoManager(_Network);
			myKlaxonHandler = new AlarmHandler(_Network);
			opsManager = new OpsManager(_Network, gameFile);
		}

		void taskManager_TaskProcessed(object sender, EventArgs e)
		{
			OnTaskProcessed();
		}

		void OnTaskProcessed ()
		{
			if (TaskProcessed != null)
			{
				TaskProcessed(this, new EventArgs ());
			}
		}

		protected virtual void ResetCalendar ()
		{
			//Reset the Calendar days to 1 
			Node calendarNode =_Network.GetNamedNode("Calendar");
			calendarNode.SetAttribute("days","1");
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

//			if (mt_manager != null)
//			{
//				mt_manager.Dispose();
//				mt_manager = null;
//			}
			if (service_status_manager != null)
			{
				service_status_manager.Dispose();
				service_status_manager=null;
			}
			if (taskManager != null)
			{
				taskManager.Dispose();
				taskManager = null;
			}

			if (predictInformManager != null)
			{ 
				predictInformManager.Dispose();
				predictInformManager = null;
			}
			if (myKlaxonHandler != null)
			{
				myKlaxonHandler.Dispose();
				myKlaxonHandler = null;
			}
			
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

			messageTimer.Dispose();

			if(bizFixer != null) { bizFixer.Dispose(); bizFixer = null; }
			if(mirrorApplier != null) { mirrorApplier.Dispose(); mirrorApplier = null; }
			if(_spaceCalculator != null) { _spaceCalculator.Dispose(); _spaceCalculator = null; }
			if(businessServiceGroupMonitor != null) { businessServiceGroupMonitor.Dispose(); businessServiceGroupMonitor = null; }
			if(availabilityMonitor != null) { availabilityMonitor.Dispose(); availabilityMonitor = null; }
			if(_slamanager != null) { _slamanager.Dispose(); _slamanager = null; }
			if(dayRunner != null) { dayRunner.Dispose(); dayRunner = null; }
			if(secondsRunner != null) { secondsRunner.Dispose(); secondsRunner = null; }
			if(_systemapplinkmonitor != null) { _systemapplinkmonitor.Dispose(); _systemapplinkmonitor = null; }
			if(recurringIncidentDetector != null) { recurringIncidentDetector.Dispose(); recurringIncidentDetector = null; }
			if(_preplaymanager != null) { _preplaymanager.Dispose(); _preplaymanager = null; }

			if (projectManager != null)
			{
				projectManager.Dispose();
				projectManager = null;
			}

			if (opsManager != null)
			{
				opsManager.Dispose();
				opsManager = null;
			}

			//missing disposes
			if (CurrTimeNode != null)
			{
				CurrTimeNode.AttributesChanged -= new Network.Node.AttributesChangedEventHandler(CurrTimeNode_AttributesChanged);
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

		new public void Start()
		{
			// TODO:  Add OpsPhaseEngine.Start implementation
		}

		new public void Stop()
		{
			// TODO:  Add OpsPhaseEngine.Stop implementation
		}

		new public void Reset()
		{
			this._Network.GetNamedNode("CurrentTime").SetAttribute("seconds",0);
			GlobalEventDelayer.TheInstance.Delayer.Clear();
			iApplier.ResetIncidents();
		}

		new public void FastForward(double timesRealTime)
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

		private int getcurrentDay()
		{
			int current_day =0;
			if (CurrDayNode != null)
			{
				current_day = CurrDayNode.GetIntAttribute("day",0);
			}
			return current_day;
		}

		private void ForceCaptureofProjectPlans()
		{
			projectManager.ForceCaptureofProjectPlans();
		}

		private void ForceRebuildFutureTimeSheets(NetworkProgressionGameFile gameFile, int current_day)
		{
			projectManager.ForceRebuildFutureTimeSheets(gameFile, current_day);
		}

		private void ForceRebuildPastTimeSheets(NetworkProgressionGameFile gameFile, int current_day)
		{
			projectManager.ForceRebuildPastTimeSheets(gameFile, current_day);
		}

		protected void HandleEndOfRound_Calculations()
		{
			//we need to run some calculations to provide for Fines and Missing Projects 
			
			//Ops end of Round calculations include fines for missing FSC, Changes etc
			this.opsManager.RunEndOfRoundCalculation(_gameFile.CurrentRound);
			//Projects end of Round calculations (Gain Achieved, penalties for missing Reg Projects)
			projectManager.RunEndOfRoundCalculation();
		}

		protected virtual void CurrTimeNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			// The seconds clock has been moved on.
			// Check if we have hit the end of the round (25 minutes).
			foreach(AttributeValuePair avp in attrs)
			{
				if("seconds" == avp.Attribute)
				{
					int attr_value = CONVERT.ParseIntSafe(avp.Value,-1);
					if (attr_value != -1)
					{
						//hack to provide a day transition at the very start
						// this allows the project which are preentered to start work quickly
						if (attr_value==1)
						{
							ForceCaptureofProjectPlans();
							CurrDayNode.SetAttribute("day","1");
							ForceRebuildFutureTimeSheets(_gameFile, 1);
						}

						// 1500 seconds = 25 minutes * 60 seconds per minute.
						if (CONVERT.ParseIntSafe(avp.Value, 0) >= CONVERT.ParseIntSafe(roundSecs, 0))
						{
							//handle the end of the round
							HandleEndOfRound_Calculations();

							Node ep = _Network.GetNamedNode("endpoint");
							int epd = ep.GetIntAttribute("hits",-1);
							epd++;
							ep.SetAttribute("hits",CONVERT.ToStr(epd));

							base.RaiseEvent(this);
						}
					}
				}
			}
		}


		/// <summary>
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="attrs"></param>
		protected virtual void CurrDayNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			bool ApplyCostAtStartofDay = true;
			bool ApplyCostAtEndofDay = false;

			foreach (AttributeValuePair avp in attrs)
			{
				if ("day" == avp.Attribute)
				{
					try
					{
						int newday = sender.GetIntAttribute("day", 0);
//						System.Diagnostics.Debug.WriteLine("================================================================");
//						System.Diagnostics.Debug.WriteLine("================================================================");
//						System.Diagnostics.Debug.WriteLine("================================================================");
//						System.Diagnostics.Debug.WriteLine("ENGINE DAY Handler Day:" + CONVERT.ToStr(newday));

						if (newday < 31)
						{
							int closingday = newday - 1;
							//opening day activities
							this.opsManager.handleOperationalWorkForDay(newday);//Handle any blocking day, FSC or upgrades (must be first)
							this.projectManager.ReassignStaff(newday);					//If people have completed the task, then back to DoingNothing
							this.projectManager.FireStaff(newday);							//If Nothing left, back to Department 
							this.projectManager.UpdateGoliveDay(newday);				//
							this.projectManager.ChangeState(newday);						//Move to the next state, if required 
							this.projectManager.HireStaff(newday, ApplyCostAtStartofDay);	//Hire any extra staff needed (placed in DoingNothing)
							this.projectManager.AssignStaff(newday);						//Assign staff to open tasks
							this.projectManager.UpdateDisplayNumbers();		//Update the display Numbers 
							this.projectManager.RecordWorkAchieved(newday, ApplyCostAtEndofDay); //record all the hard work
							//Extract the past work 
							ForceRebuildPastTimeSheets(_gameFile, newday);
						}
					}
					catch (Exception exx)
					{
						string st = exx.Message;
					}
				}
			}
		}

		///// <summary>
		///// </summary>
		///// <param name="sender"></param>
		///// <param name="attrs"></param>
		//protected virtual void CurrDayNode_AttributesChanged_Old(Node sender, ArrayList attrs)
		//{
		//  bool nostaff = false;
		//  bool ForceRelease = false;

		//  bool ApplyCostAtStartofDay = true;
		//  bool ApplyCostAtEndofDay = false;

		//  foreach(AttributeValuePair avp in attrs)
		//  {
		//    if("day" == avp.Attribute)
		//    {
		//      try
		//      {
		//        int newday = sender.GetIntAttribute("day", 0);
		//        //System.Diagnostics.Debug.WriteLine("================================================================");
		//        //System.Diagnostics.Debug.WriteLine("================================================================");
		//        //System.Diagnostics.Debug.WriteLine("================================================================");
		//        System.Diagnostics.Debug.WriteLine("ENGINE DAY Handler Day:" + CONVERT.ToStr(newday));

		//        if (newday < 31)
		//        {
		//          int closingday = newday - 1;
		//          //closing day activities
		//          if (closingday > 0)
		//          {
		//            this.projectManager.RecordWorkAchieved(closingday, ApplyCostAtEndofDay); //record all the hard work
		//            this.projectManager.ReassignStaff(closingday);			//If people have completed the task, then back to DoingNothing
		//            this.projectManager.FireStaff(closingday);					//If Nothing left, back to Department 
		//          }

		//          //Extract the past work 
		//          ForceRebuildPastTimeSheets(_gameFile, newday);
		//          //opening day activities
		//          this.opsManager.handleOperationalWorkForDay(newday);				//Handle any blocking day, FSC or upgrades (must be first)
		//          this.projectManager.UpdateGoliveDay(newday);
		//          this.projectManager.ChangeState(newday);											//Move to the next state, if required 
		//          this.projectManager.HireStaff(newday, ApplyCostAtStartofDay);	//Hire any extra staff needed (placed in DoingNothing)
		//          this.projectManager.AssignStaff(newday);											//Assign staff to open tasks
		//          this.projectManager.UpdateDisplayNumbers(); 
		//        }
		//      }
		//      catch(Exception exx)
		//      {
		//        string st = exx.Message;
		//      }
		//    }			
		//  }
		//}

		public static void StoreResults (GameManagement.NetworkProgressionGameFile gameFile, NodeTree model, int round)
		{
			string raceTeamName = SkinningDefs.TheInstance.GetData("race_team_name");

			LibCore.BasicXmlDocument raceData = LibCore.BasicXmlDocument.CreateFromFile(LibCore.AppInfo.TheInstance.Location + "data/race.xml");

			int playerGain = 0;
			Node roundResults = model.GetNamedNode("round_results");
			if (roundResults != null)
			{
				playerGain = - roundResults.GetIntAttribute("gain_achieved", 0);
			}

			// Get the list of driver numbers and names.
			Hashtable driverNumberToName = new Hashtable ();
			int playerDriverNumber = -2;
		{
			XmlNode driversNode = raceData.DocumentElement.SelectSingleNode("drivers");
			int driverNumber = -1;
			driverNumberToName.Add(playerDriverNumber, raceTeamName);
			foreach (XmlNode driverNode in driversNode.ChildNodes)
			{
				if (driverNode is XmlText)
				{
					driverNumber = LibCore.CONVERT.ParseIntSafe(driverNode.InnerText, -1);
				}
				else
				{
					switch (driverNode.Name)
					{
						case "d":
							if (driverNumber != -1)
							{
								driverNumberToName.Add(driverNumber, driverNode.InnerText);
							}
							break;
					}
				}
			}
		}

			// Get the scores.
			int winLevel = 0;
			Hashtable driverNumberToScore = new Hashtable ();
			foreach (XmlNode roundNode in raceData.DocumentElement.ChildNodes)
			{
				if (roundNode.Name == "round")
				{
					XmlNode roundNumberNode = roundNode.SelectSingleNode("num");
					if (roundNumberNode.InnerText == LibCore.CONVERT.ToStr(round))
					{
						XmlNode winLevelNode = roundNode.SelectSingleNode("winLevel");
						winLevel = LibCore.CONVERT.ParseIntSafe(winLevelNode.InnerText, 0);

						int driverNumber = 0;
						foreach (XmlNode dataNode in roundNode.ChildNodes)
						{
							switch (dataNode.Name)
							{
								case "d":
									driverNumber = LibCore.CONVERT.ParseIntSafe(dataNode.InnerText, 0);
									break;

								case "l":
									driverNumberToScore.Add(driverNumber, ConvertToTransactions(winLevel, LibCore.CONVERT.ParseIntSafe(dataNode.InnerText, 0)));
									break;

								case "td":
									driverNumberToScore.Add(playerDriverNumber, ConvertToTransactions(winLevel, playerGain + LibCore.CONVERT.ParseIntSafe(dataNode.InnerText, 0)));
									break;
							}
						}
					}
				}
			}

			ArrayList rankedDriverNumbers = new ArrayList (driverNumberToScore.Keys);
			rankedDriverNumbers.Sort(new CompareRankings (driverNumberToScore));

			LibCore.BasicXmlDocument doc = LibCore.BasicXmlDocument.Create();

			XmlElement root = doc.CreateElement("results");
			doc.AppendChild(root);

			int totalScore = 0;
			foreach (int score in driverNumberToScore.Values)
			{
				totalScore += score;
			}

			int rank = 0;
			int lastScore = 0;
			int position = 1;
			foreach (int driverNumber in rankedDriverNumbers)
			{
				string name = (string) driverNumberToName[driverNumber];
				int score = (int) driverNumberToScore[driverNumber];

				// By tracking position as well as rank, we ensure that ties work
				// properly (eg scores of 5, 5, 4, 3 will yield ranks of 1, 1, 3, 4
				// rather than say 1, 1, 2, 3 or 1, 2, 3, 4).
				if ((rank == 0) || (score != lastScore))
				{
					rank = position;
				}
				position++;

				XmlElement row = doc.CreateElement("business");
				root.AppendChild(row);

				double marketShare = 100 * (score * 1.0 / totalScore);

				row.SetAttribute("rank", CONVERT.ToStr(rank));
				row.SetAttribute("name", name);
				row.SetAttribute("transactions", CONVERT.ToStr(score));
				row.SetAttribute("market_share", CONVERT.ToStr(marketShare, 1));
				row.SetAttribute("revenue", CONVERT.ToStr(score * CoreUtils.SkinningDefs.TheInstance.GetIntData("revenue_money_per_point", 25)));

				if (driverNumber == playerDriverNumber)
				{
					row.SetAttribute("is_player", "true");

					Node operationalResults = model.GetNamedNode("operational_results");
					if (operationalResults != null)
					{
						operationalResults.SetAttribute("market_position", rank);
					}
				}

				lastScore = score;
			}

			if (! gameFile.IsSalesGame)
			{
				string filename = gameFile.GetRoundFile(round, "Results.xml", GameManagement.GameFile.GamePhase.OPERATIONS);
				doc.Save(filename);
			}

			gameFile.Save(true);
		}

		static int ConvertToTransactions (int winLevel, int score)
		{
			return (winLevel - score) * 1000;
		}

		class CompareRankings : IComparer
		{
			Hashtable driverNumberToScore;

			public CompareRankings (Hashtable driverNumberToScore)
			{
				this.driverNumberToScore = driverNumberToScore;
			}

			public int Compare (object x, object y)
			{
				int a = (int) x;
				int b = (int) y;

				int scoreA = (int) driverNumberToScore[a];
				int scoreB = (int) driverNumberToScore[b];

				return scoreB.CompareTo(scoreA);
			}
		}

		public ProjectManager GetProjectManager ()
		{
			return projectManager;
		}
	}
}