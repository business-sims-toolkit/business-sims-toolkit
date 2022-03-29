using System;
using System.Collections;
using LibCore;
using Network;
using IncidentManagement;
using TransitionObjects;
using Logging;

using CoreUtils;
using BusinessServiceRules;

namespace GameEngine
{
	/// <summary>
	/// Summary description for TransitionPhaseEngine.
	/// </summary>
	public class TransitionPhaseEngine : BaseClass, IDisposable, ITimedClass
	{
		protected NodeTree _Network;
		BasicIncidentLogger biLog;
		DayRunner MyDayRunner;
		CalendarRunner calendarRunner;
		ProjectManager MyProjectManager;
		ProjectSpendManager MyProjectSpendManager;
		IncidentApplier incidentApplier;
		ServiceSpaceCalculator _spaceCalculator;
		MirrorApplier mirrorApplier;
		SLAManager _slamanager;

		MachineUpgrader machineUpgrader;
		AppUpgrader appUpgrader;
		SecondsRunner secondsRunner;
		MachineRebooter rebooter;
		PreplayManager _preplaymanager;

		Node currentDay;
		Node calendarNode;
		int numDaysInPhase = 100;
		bool TrainingGame = false;

		DayManager dayManager;

		public event PhaseFinishedHandler PhaseFinished;

		public MirrorApplier TheMirrorApplier
		{
			get
			{
				return mirrorApplier;
			}
		}

		public IncidentApplier TheIncidentApplier
		{
			get
			{
				return incidentApplier;
			}
		}

		void ClearCalender(NodeTree n)
		{
			//Find the Calender Node and reset the days to 1
			calendarNode = n.GetNamedNode("Calendar");
			calendarNode.SetAttribute("days","1");
			//Delete calendar events 
			ArrayList delNodes = new ArrayList();
			foreach(Node n2 in calendarNode.getChildren())
			{
				delNodes.Add(n2);
			}
			//
			foreach(Node n2 in delNodes)
			{
				n2.Parent.DeleteChildTree(n2);
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="n"></param>
		/// <param name="roundDir"></param>
		/// <param name="SipsDirs"></param>
		/// <param name="round"></param>
		public TransitionPhaseEngine(NodeTree n, string roundDir, string SipsDirs, int round, bool logResults, bool isTrainingGame)
		{
			TrainingGame = isTrainingGame;
			//Reset the Clock time to 10
			Node timeNode = n.GetNamedNode("CurrentTime");
			timeNode.SetAttribute("seconds","0");

			OpsPhaseEngine.BringEverythingUp(n, round);

			_Network = n;	
			
			ClearCalender(n);

			_Network.GetNamedNode("CurrentDay").SetAttribute("day","1");
			//_Network.GetNamedNode("CurrentTime").SetAttribute("seconds","0");

			GlobalEventDelayer.TheInstance.DestroyEventDelayer();
			GlobalEventDelayer.TheInstance.SetEventDelayer( new EventDelayer() );
			GlobalEventDelayer.TheInstance.Delayer.SetModelCounter(_Network, "CurrentTime", "seconds");
			GlobalEventDelayer.TheInstance.Delayer.SetEventManager( new TransitionEventDelayManager(_Network) );

			if(logResults)
			{
				biLog = new BasicIncidentLogger();
				biLog.LogTreeToFile(_Network, roundDir + "\\NetworkIncidents.log");
			}
			else
			{
				biLog = null;
			}

			n.GetNamedNode("CurrentTime").SetAttribute("seconds",0);

			//Build objects 
			_slamanager = new SLAManager(_Network);
			incidentApplier = new IncidentApplier(n);
			//incidentApplier.TargetTree = n;
			OnAttributeHitApplier.TheInstance.Clear();
			machineUpgrader = new MachineUpgrader(_Network,true);
			appUpgrader = new AppUpgrader(_Network,true);
			secondsRunner = new SecondsRunner(_Network);
			mirrorApplier = new MirrorApplier(_Network);
			rebooter = new MachineRebooter(_Network);

			rebooter.setOverride(2);
			rebooter.TargetTree = _Network;

			System.IO.StreamReader file = new System.IO.StreamReader(AppInfo.TheInstance.Location + "/data/transition_events_r" + CONVERT.ToStr(round) + ".xml");
			string xmldata = file.ReadToEnd();
			incidentApplier.SetIncidentDefinitions(xmldata, n);
			file.Close();
			file = null;

			dayManager = new DayManager();
			dayManager.SetNodeTreeRoot(_Network);
			_spaceCalculator = new ServiceSpaceCalculator(_Network, BusinessServiceRules.ServiceSpaceCalculator.RuleStyle.THROW_ON_NO_RESOURCE);
			
			MyDayRunner = new DayRunner(_Network);
			MyDayRunner.Reset();

			MyProjectManager = new ProjectManager(n,round,true);
			MyProjectSpendManager = new ProjectSpendManager(n,CONVERT.ToStr(round));

			currentDay = n.GetNamedNode("CurrentDay");
			currentDay.AttributesChanged += currentDay_AttributesChanged;

			calendarNode.AttributesChanged += calendarNode_AttributesChanged;
			numDaysInPhase = calendarNode.GetIntAttribute("days",100);
			calendarRunner = new CalendarRunner(n, TrainingGame);
			_preplaymanager = new PreplayManager(_Network);

			TimeManager.TheInstance.ManageClass(this);
		}

		public void SetupIncidents()
		{
			//need to reset the calendar
			ClearCalender(_Network);
			incidentApplier.ResetIncidents();
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
		public virtual void Dispose()
		{
			TimeManager.TheInstance.UnmanageClass(this);

			GlobalEventDelayer.TheInstance.DestroyEventDelayer();

			if(null != _preplaymanager)
			{
				if(null != currentDay)
				{
					currentDay.AttributesChanged -= currentDay_AttributesChanged;
					currentDay = null;
				}

                calendarNode.AttributesChanged -= calendarNode_AttributesChanged;

				_slamanager.Dispose();
				MyProjectManager.Dispose();
				MyProjectSpendManager.Dispose();
				rebooter.Dispose();
				mirrorApplier.Dispose();
				secondsRunner.Dispose();
				machineUpgrader.Dispose();
				appUpgrader.Dispose();
				MyDayRunner.Dispose();
				incidentApplier.Dispose();
				calendarRunner.Dispose();
				_preplaymanager.Dispose();
				dayManager.Dispose();
				CloseLogger();
			}
		}

		void currentDay_AttributesChanged(Node sender, ArrayList attrs)
		{
			foreach(AttributeValuePair avp in attrs)
			{
				if("day" == avp.Attribute)
				{
					// We are finished when we move on to the day after the calendar.
					if(CONVERT.ParseInt(avp.Value) >= numDaysInPhase)
					{
						// The phase has finished!
						TimeManager.TheInstance.Stop();
						if(null != PhaseFinished)
						{
							PhaseFinished(this);
						}
					}
					return;
				}
			}
		}

		void calendarNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			// Maybe the calendar length has changed.
			foreach(AttributeValuePair avp in attrs)
			{
				if("days" == avp.Attribute)
				{
					numDaysInPhase = CONVERT.ParseInt(avp.Value);
					return;
				}
			}
		}

		public virtual void Start ()
		{
		}

		public virtual void Stop ()
		{
		}

		public virtual void Reset ()
		{
		}

		public virtual void FastForward (double timesRealTime)
		{
		}
	}
}