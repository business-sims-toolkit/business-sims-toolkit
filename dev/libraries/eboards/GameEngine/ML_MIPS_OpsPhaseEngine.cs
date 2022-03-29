using System.Collections;
using LibCore;
using Network;
using IncidentManagement;
using BusinessServiceRules;
using TransitionObjects;

using CoreUtils;
using Logging;

namespace GameEngine
{
	public class ML_MIPS_OpsPhaseEngine : BaseOpsPhaseEngine
	{
		protected IncidentApplier iApplier;
		protected ML_MIE_IncidentRemover iRemover;

		protected CostMonitor costMonitor;

		protected NodeTree _Network;
		protected ArrayList serviceMonitors;
		protected BasicIncidentLogger biLog;
		protected ML_SecondsRunner secondsRunner;
		protected PreplayManager _preplaymanager;
		protected RecurringIncidentDetector recurringIncidentDetector;
		protected Node CurrTimeNode = null;

		protected int round;

		public ZoneTemperatureMonitor TheZoneTemperatureMonitor
		{
			get
			{
				//return temperatureMonitor;
				return null;
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
				//return mirrorApplier;
				return null;
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
			//return MyProjectManager;
			return null;
		}

		public ArrayList GetInstallableProjects ()
		{
			//return MyProjectManager.getInstallableProjects();
			return null;
		}

		protected string _incidentDefsFile;

		public static void BringEverythingUp(NodeTree net)
		{
			net.GetNamedNode("FacilitatorNotifiedErrors").DeleteChildren();

			string baselineTemperature = "72.3";

			Hashtable nodes = net.GetNodesWithAttribute("incident_id");
			foreach(Node n in nodes.Keys)
			{
				n.SetAttribute("incident_id", "");
			}

			nodes = net.GetNodesWithAttribute("turnedoff");
			foreach (Node n in nodes.Keys)
			{
				// Leave aircon nodes turned off between rounds, as being turned off isn't a fault.
				if (! n.GetAttribute("name").ToLower().StartsWith("aircon"))
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
			foreach(Node n in nodes.Keys)
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

			nodes = net.GetNodesWithAttribute("virtualmirrorinuse");
			foreach(Node n in nodes.Keys)
			{
				n.SetAttribute("virtualmirrorinuse", "false");
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

			nodes2 = net.GetNodesWithAttributeValue("thermal","true");
			foreach(Node n in nodes2)
			{
				n.SetAttribute("thermal","false");
			}

			nodes = net.GetNodesWithAttribute("initial_baseline_temperature");
			foreach(Node n in nodes.Keys)
			{
				double temperature = n.GetDoubleAttribute("initial_baseline_temperature", 0);
				ArrayList attrs = new ArrayList ();
				attrs.Add(new AttributeValuePair ("baseline_temperature", temperature));
				attrs.Add(new AttributeValuePair ("temperature", temperature));
				n.SetAttributes(attrs);
			}

			nodes = net.GetNodesWithAttribute("temperature");
			foreach(Node n in nodes.Keys)
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
			foreach(Node n in nodes.Keys)
			{
				n.SetAttribute("goal_temperature", baselineTemperature);
			}

			nodes = net.GetNodesWithAttribute("goal_temperature_start");
			foreach(Node n in nodes.Keys)
			{
				n.SetAttribute("goal_temperature_start", baselineTemperature);
			}

			nodes = net.GetNodesWithAttribute("goal_temperature_change_duration");
			foreach(Node n in nodes.Keys)
			{
				n.SetAttribute("goal_temperature_change_duration", "1");
			}

			nodes = net.GetNodesWithAttribute("goal_temperature_start_time");
			foreach(Node n in nodes.Keys)
			{
				n.SetAttribute("goal_temperature_start_time", "0");
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

		}

		/// <summary>
		/// Default round length is 25 mins * 60 seconds.
		/// </summary>
		protected string roundSecs = "3600";

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
		public ML_MIPS_OpsPhaseEngine(NodeTree n, string roundDir, string incidentDefsFile, int round, bool logResults)
		{
			this.round = round;
			TimeManager.TheInstance.ManageClass(this);

			//Reset the Clock time to 10
			Node timeNode = n.GetNamedNode("CurrentTime");
			timeNode.SetAttribute("seconds","0");

			Node elapsedNode = n.GetNamedNode("ElapsedTime");
			elapsedNode.SetAttribute("seconds", "0");

			BringEverythingUp(n);

			_Network = n;
			serviceMonitors = new ArrayList();
			_incidentDefsFile = incidentDefsFile;

			//Reset the Calendar days to 1 
			Node calendarNode = n.GetNamedNode("Calendar");
			calendarNode.SetAttribute("days","1");																																																				

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
			recurringIncidentDetector = new RecurringIncidentDetector(_Network);
			iApplier = new IncidentApplier(_Network);

			//mirrorApplier = new MirrorApplier(_Network);
			//rebooter = new MachineRebooter(_Network);

			iRemover = new ML_MIE_IncidentRemover(_Network);

			//bool AutoUpdateOnFix = SkinningDefs.TheInstance.GetBoolData("auto_update_on_fix", false);
			//if (AutoUpdateOnFix == true)
			//{
			//  bizFixer = (BusinessServiceFixer)(new BusinessServiceFixerAutoUpgrade(_sdc, iApplier));
			//}
			//else
			//{
			//  bizFixer = new BusinessServiceFixer(_sdc, iApplier);
			//}


			//machineUpgrader = new MachineUpgrader(_Network,false);
			//appUpgrader = new AppUpgrader(_Network,false);

			costMonitor = new CostMonitor("CostedEvents", n, AppInfo.TheInstance.Location + "\\data\\costs.xml");

			SetupSpaceMonitor();

			SetupPowerMonitor();
			_preplaymanager = new PreplayManager(_Network);

			SetupTemperatureMonitor();
			SetupAutoRestorer(_Network);

			System.IO.StreamReader file = new System.IO.StreamReader(incidentDefsFile);
			string xmldata = file.ReadToEnd();
			iApplier.SetIncidentDefinitions(xmldata, _Network);
			file.Close();
			file = null;

			incoming = _Network.GetNamedNode("enteredIncidents");
			if(null == incoming)
			{
				incoming = new Node(_Network.Root, "enteredIncidents","enteredIncidents", (ArrayList)null);
			}
			iApplier.IncidentEntryQueue = incoming;
	
			// Create a SecondsRunner and watch the current time being set to 25 minutes which is
			// the end of the race.
			secondsRunner = new ML_SecondsRunner(_Network);
			CurrTimeNode = _Network.GetNamedNode("CurrentTime");
			CurrTimeNode.AttributesChanged += OpsPhaseEngine_AttributesChanged;

			if(null != biLog) biLog.CloseLog();
			TimeManager.TheInstance.Stop();
		}

		public virtual void SetupSpaceMonitor()
		{
		}


		public virtual void SetupPowerMonitor()
		{
		}

		public virtual void SetupTemperatureMonitor ()
		{
		}

		public virtual void SetupAutoRestorer (NodeTree model)
		{
		}

		public void SetupIncidents()
		{
			iApplier.ResetIncidents();
		}

		protected void CloseLogger()
		{
			if(null != biLog)
			{
				biLog.Dispose();
				biLog = null;
			}
		}

		public override void Dispose()
		{
			if(null != biLog)
			{
				CloseLogger();
			}

			TimeManager.TheInstance.UnmanageClass(this);
			OnAttributeHitApplier.TheInstance.Clear();
			GlobalEventDelayer.TheInstance.DestroyEventDelayer();

			if (iApplier != null)
			{
				iApplier.Dispose(); 
				iApplier = null;
			}

			if (iRemover != null)
			{
				iRemover.Dispose();
				iRemover = null;
			}

			if(costMonitor != null) { costMonitor.Dispose(); costMonitor = null; }
			if(secondsRunner != null) { secondsRunner.Dispose(); secondsRunner = null; }
			if(recurringIncidentDetector != null) { recurringIncidentDetector.Dispose(); recurringIncidentDetector = null; }
			if(_preplaymanager != null) { _preplaymanager.Dispose(); _preplaymanager = null; }

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
		}

		#region ITimedClass Members

		public override void Start()
		{
		}

		public override void Stop()
		{
		}

		public override void Reset()
		{
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
					//In ML, the game end when we get to 30 mins of elapsed Time
					//Not the game seconds which can be affected by incidents 
					Node elapsed_time_node = this._Network.GetNamedNode("ElapsedTime");
					int elapsed_seconds = elapsed_time_node.GetIntAttribute("seconds",0);
					int current_round_limit = CONVERT.ParseIntSafe(roundSecs, -1);

					bool time_complete = (elapsed_seconds == current_round_limit);
					bool fac_override = false;

					Node fro = this._Network.GetNamedNode("facilitor_round_override");
					if (fro != null)
					{
						fac_override = fro.GetBooleanAttribute("end_now",false);
					}

					if ((time_complete)|(fac_override))
					{
						RaiseEvent(this);
					}
				}
			}
		}
	}
}