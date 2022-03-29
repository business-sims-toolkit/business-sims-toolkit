using System;
using System.IO;
using System.Xml;
using System.Collections;

using BaseUtils;
using CoreUtils;
using LibCore;

namespace Polestar_PM.DataLookup
{
	
	public enum emCritPathDeScope
	{
	  CRITICAL,
	  ACTIVE,
	  DESCOPED
	}

	public enum emSTAGE_STATUS
	{
		NO_ENTRY = 0,
		NOT_STARTED = 1,
		WORKING_FULL = 2,
		WORKING_PARTIAL = 3,
		STOPPED_ON = 4,
		STOPPED_OFF = 5,
		COMPLETED = 6,
		PAUSE = 7,
		BUYIN = 8
	}

	/// <summary>
	/// The Project task block represents the basic tasks that needs to be completed for a project stage
	/// A task can either represent a single track or a collection of parallel tasks 
	/// If multiple then there is a maximum of 20 tracks and costs are held in size order (largest first)
	/// </summary>
	public class Def_ProjectTaskBlock
	{
		public const int MAX_TASKS = 100;
		//Which Project Phase we are in 
		private emPHASE phase = emPHASE.DEV;
		//Which Project Stage we are in 	
		private emPHASE_STAGE stage = emPHASE_STAGE.STAGE_A;
		//Total Tracks
		private int totaltasks = 0;
		//Scope Level (0 is the core level)
		private int scopelevel = 0;
		//The task in this Block
		private Def_ProjectTask[] tasks = new Def_ProjectTask[MAX_TASKS];
		//Is this a single track 
		private Boolean ismultiple;
		//What the current status is 
		private emSTAGE_STATUS status = emSTAGE_STATUS.NO_ENTRY;
		public emProjectRunningState overridepause = emProjectRunningState.NOT_PAUSED;
		// Number of hours per day that those people will put in 
		private emWorkTimeMode requestedmanpowerrate = emWorkTimeMode.NORMAL;
		// Number of people the user has requested INTERNAL
		private int requestedmanpowerlevel_int = 0;
		// Number of people the user has requested EXTERNAL
		private int requestedmanpowerlevel_ext = 0;
		// The Allocated Internal Workers
		private ArrayList AssignedInternalWorkers = new ArrayList();
		//The Allocated External Workers
		private ArrayList AssignedExternalWorkers = new ArrayList();

		private Boolean RetainUntilBlockEnd = true;
		private int recyclecount=0;
		private Boolean ignorewaiting = false;
    
		#region Constructor

		public Def_ProjectTaskBlock()
		{
			for (int step=0; step<MAX_TASKS; step++)
			{
				tasks[step] = new Def_ProjectTask();
				if (tasks[step] != null)
				{
					tasks[step].Sequence = step;
				}
			}
			Clear();
		}

		#endregion Constructor

		#region Accessors

		public emPHASE Phase
		{
			get { return phase; }
			set { phase = value; }
		}

		public emPHASE_STAGE Stage
		{
			get { return stage; }
			set { stage = value; }
		}

		public Boolean IgnoreWaiting
		{
			get { return ignorewaiting; }
			set { ignorewaiting = value; }
		}

		public int TotalTasks
		{
			get { return totaltasks; }
			set { totaltasks = value; }
		}

		public Boolean isMultiple
		{
			get { return ismultiple; }
			set { ismultiple = value; }
		}

		public int RequestedManPowerLevel_Int
		{
			get { return requestedmanpowerlevel_int; }
			set { requestedmanpowerlevel_int = value; }
		}

		public int RequestedManPowerLevel_Ext
		{
			get { return requestedmanpowerlevel_ext; }
			set { requestedmanpowerlevel_ext = value; }
		}

		public emWorkTimeMode RequestedManPowerRate
		{
			get { return requestedmanpowerrate; }
			set { requestedmanpowerrate = value; }
		}

		public int RecycleCount
		{
			get { return recyclecount; }
			set { recyclecount = value; }
		}

		public Def_ProjectTask this [int index]  
		{
			get 
			{
				// Check the index limits.
				if (index < 0 || index >= MAX_TASKS)
					return null;
				else
					return tasks[index];
			}
			set 
			{
				if (!(index < 0 || index >= MAX_TASKS))
					tasks[index] = value;
			}
		}

		public emProjectRunningState OverridePause
		{
			get { return overridepause; }
			set { overridepause = value; }
		}

		public emSTAGE_STATUS Status
		{
			get 
			{
				if (this.OverridePause != emProjectRunningState.NOT_PAUSED)
				{
					return emSTAGE_STATUS.PAUSE;
				}
				return status; 
			}
			set { status = value; }
		}

		#endregion Accessors

		#region Utils

		public void SetRetainMode(Boolean Retain)
		{
			RetainUntilBlockEnd  = Retain;
		}

		/// <summary>
		/// Used in setting the task information from the XML editor
		/// </summary>
		/// <param name="count"></param>
		public void validateTasks(int count)
		{
			for (int step=0; step< this.TotalTasks; step++)
			{
				if (step<count)
				{
					tasks[step].Valid = true;
				}
			}
		}

		/// <summary>
		/// This used to define this TaskBlock as being to the TEST phase
		/// </summary>
		public void setTestPhase()
		{
			phase =emPHASE.TEST;
		}

		/// <summary>
		/// This clears all the runtime information 
		/// </summary>
		public void Clear()
		{
			totaltasks = 0;	
			//recyclecount = 0;	not needed
			isMultiple = false;									
			RequestedManPowerLevel_Int = 0;
			RequestedManPowerLevel_Ext = 0;

			AssignedInternalWorkers.Clear();
			AssignedExternalWorkers.Clear();

			RequestedManPowerRate = emWorkTimeMode.NORMAL;
			status = emSTAGE_STATUS.NO_ENTRY;
			overridepause = emProjectRunningState.NOT_PAUSED;
			IgnoreWaiting = false;

			for (int step=0; step < MAX_TASKS; step++)
			{
				this[step].Time_Required = -1;
				this[step].Time_Remaining = 0;
				this[step].Completed = false;
				this[step].AssignedWorker = false;
				this[step].DeScoped = false;
				scopelevel = step;
			}
		}

		/// <summary>
		/// This clears all the runtime information
		/// Usually used at the start of the round 
		/// </summary>
		public void Reset()
		{
			//RetainUntilBlockEnd = true;
			requestedmanpowerrate = emWorkTimeMode.NORMAL;
			RequestedManPowerLevel_Int = 1;
			RequestedManPowerLevel_Ext = 0;
			status = emSTAGE_STATUS.NO_ENTRY;
			overridepause = emProjectRunningState.NOT_PAUSED;
			IgnoreWaiting = false;
			totaltasks = 0;
			this.ismultiple = false;
			//recyclecount = 0;	not needed 
			//Drop the Workers
			if (AssignedInternalWorkers.Count>0)
			{
				foreach(Object o1 in AssignedInternalWorkers)
				{
					Worker w1 = o1 as Worker;
					w1.Fired();
				}
				AssignedInternalWorkers.Clear();
			}
			if (AssignedExternalWorkers.Count>0)
			{
				foreach(Object o1 in AssignedExternalWorkers)
				{
					Worker w1 = o1 as Worker;
					w1.Fired();
				}
				AssignedExternalWorkers.Clear();
			}
			//Reset the tasks
			for (int step=0; step < MAX_TASKS; step++)
			{
				this[step].Reset();
			}
		}

		/// <summary>
		/// Load this TaskBlock from another TaskBlock
		/// Used to initialise the engine items from the project items 
		/// </summary>
		/// <param name="StandardPTB">The Reference TaskBlock</param>
		/// <param name="noRuntime">Should we copy the runtime dependant data</param>
		public void CopyData(Def_ProjectTaskBlock StandardPTB, Boolean noRuntime)
		{
			Clear();
			this.Phase = StandardPTB.Phase;
			this.Stage = StandardPTB.Stage;
			this.totaltasks = StandardPTB.TotalTasks;
			this.isMultiple = StandardPTB.isMultiple;
			this.IgnoreWaiting =  StandardPTB.IgnoreWaiting;
			if (noRuntime == false)
			{
				this.RequestedManPowerLevel_Int = StandardPTB.RequestedManPowerLevel_Int;
				this.RequestedManPowerLevel_Ext = StandardPTB.RequestedManPowerLevel_Ext;
				this.RequestedManPowerRate = StandardPTB.RequestedManPowerRate;
			}
			for (int step=0; step < MAX_TASKS; step++)
			{
				this[step].Critical = StandardPTB[step].Critical;
				this[step].ScopeCost = StandardPTB[step].ScopeCost;
				this[step].ScopeGroup = StandardPTB[step].ScopeGroup;
				this[step].Valid =  StandardPTB[step].Valid;
				scopelevel = step;
				this[step].Time_Required = StandardPTB[step].Time_Required;
				this[step].Time_Remaining = this[step].Time_Required;
				if (noRuntime == false)
				{
					this[step].Time_Remaining = this[step].Time_Remaining;
					this[step].Completed = false;
				}
			}
		}

		public void ReBuildTotalsFromTasks()
		{
			//Build up the calculated information 
			//this.Total_Timecost_Required=0;
			totaltasks =0;
			for (int step=0; step<MAX_TASKS; step++)
			{
				if (this[step].Time_Required >0)
				{
					//this.Total_Timecost_Required += this[step].Time_Required;
					totaltasks++;
				}
			}
			if (this.totaltasks == 1)
			{
				isMultiple = false;		
			}
			else
			{
				isMultiple = true;		
			}
		}

		#endregion Utils

		#region Critical Path Descope Methods

		public int getMaxGroupNumber()
		{
			int GrpNo = -1;

			for (int step=0; step < this.TotalTasks; step++)
			{
				if (tasks[step].ScopeGroup > GrpNo)
				{
					GrpNo = tasks[step].ScopeGroup;
				}
			}
			//System.Diagnostics.Debug.WriteLine("GrpNo"+GrpNo.ToString());
			return GrpNo;
		}

		public Boolean CritPathDeScopeByGroup(emCritPathDeScope[] RequestPath, 
			out int TotalScopeHit, out int TotalManHoursSaved)
		{
			int NumberofGroups = RequestPath.GetLength(0);
			TotalManHoursSaved = 0;
			TotalScopeHit = 0;
			Boolean OpSuccess = false;

			try
			{
				for (int GrpNo =0; GrpNo < NumberofGroups; GrpNo++)
				{
					if (RequestPath[GrpNo]==emCritPathDeScope.DESCOPED)
					{
						for (int step=0; step< this.TotalTasks; step++)
						{
							if (tasks[step].Critical == false)
							{
								if (tasks[step].DeScoped == false)
								{
									if (tasks[step].ScopeGroup == (GrpNo+1))
									{
										tasks[step].DeScoped = true;
										TotalManHoursSaved += tasks[step].Time_Required;
										TotalScopeHit += tasks[step].ScopeCost;
									}
								}
							}	
						}
					}
				}
				OpSuccess = true;
			}
			catch (Exception)
			{
				OpSuccess = false;
			}
			return OpSuccess;
		}

		#endregion Critical Path Descope Methods

		#region Descope Methods

		public double MyNumericRounder(double x, int numerator, int denominator)
		{
			// returns the number nearest x, with a precision of numerator/denominator
			// example: Round(12.1436, 5, 100) will round x to 12.15 (precision = 5/100 = 0.05)
			long y = (long)Math.Floor(x * denominator + (double)numerator / 2.0);
			return (double)(y - y % numerator)/(double)denominator;
		}

		public int HowManyTasksToKill(int NumberOfTasks, int ScopeDropPercent)
		{ 
			System.Diagnostics.Debug.WriteLine("Calc Kill Number");
			double ScopeDropFlt = (double) ScopeDropPercent;
			double HowManyTasks = (double) NumberOfTasks;
			double KillCount = (ScopeDropPercent * HowManyTasks) / 100;
			double KillCount2 = MyNumericRounder(KillCount, 1, 1);
			int kc = (int)(KillCount2);
			return kc;
		}


		/// <summary>
		/// Get amount of man hours that we could save idf wer drop that p[erecentage of the remaining tasks 
		/// Level 1 is all the tasks 
		/// </summary>
		/// <param name="TakeAccountofWorkDone"></param>
		/// <param name="Level"></param>
		/// <returns></returns>
		public void GetDeScopedManHours(Boolean TakeAccountofWorkDone, int ScopeDrop, 
			out int SavedHours, out int SavedTasks)
		{
			SavedHours = 0;
			SavedTasks = 0;
		}

		public void ApplyDeScope(int ScopeDrop, out int DroppedHours, out int DroppedTasks)
		{
			DroppedHours = 0;
			DroppedTasks = 0;
		}

		#endregion Descope Methods

		#region XML Methods

		public Boolean LoadFromDefinationXMLNode(XmlNode xn)
		{
			Boolean OpSuccess = true;
			string ErrMsg = string.Empty;
			string tmpstr = string.Empty;
			string stxx= string.Empty; 
			
			try
			{
				//A number of the fields are either runtime only or calculated from stored data 
				//We only need to extract the following 

				tmpstr = LibCore.BasicXmlDocument.GetStringAttribute(xn, "stage");
				this.Stage = (emPHASE_STAGE) Enum.Parse(typeof(emPHASE_STAGE), "STAGE_" + tmpstr.ToUpper());

				switch (this.Stage)
				{
					case emPHASE_STAGE.STAGE_A:
					case emPHASE_STAGE.STAGE_B:
					case emPHASE_STAGE.STAGE_C:
					case emPHASE_STAGE.STAGE_D:
					case emPHASE_STAGE.STAGE_E:
						this.Phase = emPHASE.DEV;
						break;


					case emPHASE_STAGE.STAGE_F:
					case emPHASE_STAGE.STAGE_G:
					case emPHASE_STAGE.STAGE_H:
						this.Phase = emPHASE.TEST;
						break;
				}

				//totaltasks = int.Parse(xml_utils.extractStr("Tracks", ref xn, string.Empty, ref ErrCount, out ErrMsg));

				///now need to iterate throught the Tasks 
				///Clear the full set of tasks (for ones we don't set)
				for (int step = 0; step < MAX_TASKS; step++)
				{
					this[step].Clear();
				}

				// Extract the Man Days for the project.
				{
					int man_days = CONVERT.ParseInt(LibCore.BasicXmlDocument.GetStringAttribute(xn, "man_days"));
					int step = 0;
					for (int i = 0; i < man_days; ++i)
					{
						Def_ProjectTask pt = new Def_ProjectTask();
						pt.SetToDefault();

						if (step < MAX_TASKS)
						{
							// Is this a special way just to obfusticate the code?!
							this[step].CopyData(pt);
						}
						step++;
					}
				}
				ReBuildTotalsFromTasks();
				//Build up the calculated information 
				//ReBuildTotalsFromTasks();
				OpSuccess = true;
			}
			catch (Exception evc)
			{
				string st = "PTB LoadFromDefXMLString Exc " + evc.Message + "##" + evc.StackTrace;
				//LoggerSimple.TheInstance.Error(st);
			}
			return OpSuccess;
		}

		#endregion XML Methods
	}
}
