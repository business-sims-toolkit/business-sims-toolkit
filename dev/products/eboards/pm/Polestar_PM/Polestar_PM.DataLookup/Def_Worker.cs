using System;
using System.IO;
using System.Xml;
using System.Collections;

using BaseUtils;
using CoreUtils;
using LibCore;

namespace Polestar_PM.DataLookup
{

	public enum emResourcePeopleType
	{
		NONE = 0,
		INTERNAL = 1,
		EXTERNAL = 2,
		TOTAL = 3
	}

	public enum emResourceSkillType
	{
		NONE = 0,
		DEV = 1,
		TEST = 2,
		OPERATIONS = 3
	}

	/// <summary>
	/// Summary description for Worker.
	/// </summary>
	public class Worker
	{
		emResourcePeopleType workerjobtype = emResourcePeopleType.INTERNAL;
		emResourceSkillType workerskilltype = emResourceSkillType.DEV;
		int currentprojectslot = -1;
		//ProjectRunner mycurrproject = null;
		Def_ProjectTask mycurrtask = null;
		int hours = 0;
		int debugid=0;
		//int normalwagerate = 0;		//Normal wage rate per hour
		//int overtimewagerate = 0;	//Overtime wage rate per hour

		int[] WorkedHoursByDay = new int[GameConstants.MAX_NUMBER_DAYS];	//What hours did i work in which days 
		int[] WaitedHoursByDay = new int[GameConstants.MAX_NUMBER_DAYS];	//What hours did i wait in which days 

		#region Constructor 
		public Worker()
		{
			Clear();
		}
		#endregion Constructor 

		#region Accessors 

		public emResourcePeopleType WorkerJobType
		{
			get { return workerjobtype; }
			set { workerjobtype = value; }
		}

		public emResourceSkillType WorkerSkillType
		{
			get { return workerskilltype; }
			set { workerskilltype = value; }
		}

		public int CurrentProjectSlot
		{
			get { return currentprojectslot; }
			set { currentprojectslot = value; }
		}

//		public ProjectRunner MyCurrProject
//		{
//			get { return mycurrproject; }
//			set { mycurrproject = value; }
//		}

		public Def_ProjectTask MyCurrTask
		{
			get { return  mycurrtask; }
			set { mycurrtask = value; }
		}

		public int DebugID
		{
			get { return debugid; }
			set { debugid = value; }
		}

//		public int NormalWageRate
//		{
//			get { return normalwagerate; }
//			set { normalwagerate = value; }
//		}
//
//		public int OvertimeWageRate
//		{
//			get { return overtimewagerate; }
//			set { overtimewagerate = value; }
//		}

		#endregion Accessors 
		
		#region Utils 

		public void ResetHoursRecorded()
		{
			for (int step =0; step<GameConstants.MAX_NUMBER_DAYS; step++)
			{
				WorkedHoursByDay[step]=0;
				WaitedHoursByDay[step]=0;
			}
		}

		public void Clear()
		{
//			//System.Diagnostics.Debug.WriteLine("Worker Clear");
//			currentprojectslot = -1;
//			mycurrproject = null;
//			mycurrtask = null;
//			ClearHours();
//			ResetHoursRecorded();
		}

		public void Reset()
		{
//			//System.Diagnostics.Debug.WriteLine("Worker Reset");
//			currentprojectslot = -1;
//			mycurrproject = null;
//			mycurrtask = null;
//			ClearHours();
//			ResetHoursRecorded();
		}

		public Boolean isUnEmployed()
		{
//			if (MyCurrProject != null)
//			{
//				return false;
//			}
//			else
//			{
//				return true;
//			}
			return false;
		}

		public string getDebugStr()
		{
//			string str = string.Empty;
//			str += "WKR "+ debugid.ToString();
//			str += "[ResPpleType "+((int)workerjobtype).ToString()+"]";
//			str += "[PrjSlt "+(currentprojectslot).ToString()+"]";
//			if (mycurrproject != null)
//			{str += "[CurrProjObj "+(mycurrproject).ToString()+"]";}
//			else
//			{str += "[CurrProjObj NULL]";}
//			if (mycurrtask != null)
//			{str += "[CurrTaskObj "+(mycurrtask).ToString()+"]";}
//			else
//			{str += "[CurrTaskObj NULL]";}
//			str += "[hours "+(hours).ToString()+"]";
//			//str += "[NormalWageRate "+(NormalWageRate).ToString()+"]";
//			//str += "[OvertimeWageRate "+(OvertimeWageRate).ToString()+"]";
//			return str;
			return "";
		}

		#endregion Utils  

		#region Operational Methods 

		public int GetWorkedHoursByDay(int DayStep)
		{
			return WorkedHoursByDay[DayStep];
		}

		public int GetWaitedHoursByDay(int DayStep)
		{
			return WaitedHoursByDay[DayStep];
		}

		public void Fired()
		{
//			System.Diagnostics.Debug.WriteLine("Worker "+this.debugid.ToString()+" Fired "+currentprojectslot.ToString());
//			currentprojectslot = -1;
//			MyCurrProject = null;
//			MyCurrTask = null;
		}

//    public void QuitEmployment()
//    {
//			System.Diagnostics.Debug.WriteLine("Worker "+this.debugid.ToString()+" Quit Employment "+currentprojectslot.ToString());
//			currentprojectslot = -1;
//      MyCurrProject = null;
//      MyCurrTask = null;
//    }

//		public void joinEmployment(int ProjectSlot, ProjectRunner PrjObj)
//		{
//			CurrentProjectSlot = ProjectSlot;
//			MyCurrProject = PrjObj;
//			MyCurrTask = null;
//			System.Diagnostics.Debug.WriteLine("Worker "+this.debugid.ToString()+" Join Employment "+ProjectSlot.ToString());
//		}

    public void setTask(Def_ProjectTask TaskObj)
    {
			MyCurrTask = TaskObj;
      System.Diagnostics.Debug.WriteLine("Worker "+this.debugid.ToString()+" SetTask ");
    }

		public void ClearHours()
		{
			hours = 0; 
		}

		public void setHours(int tmpHours)
		{
			hours = tmpHours; 
		}

		public int getHours()
		{
			return hours; 
		}

		public void BookHours(int tmpHours, int DayNo, Boolean isWait)
		{
			hours = hours - tmpHours;
			if (isWait)
			{
				if(DayNo < GameConstants.MAX_NUMBER_DAYS)
				{
					WaitedHoursByDay[DayNo-1] = WaitedHoursByDay[DayNo-1] + tmpHours;
				}
			}
			else
			{
				if(DayNo < GameConstants.MAX_NUMBER_DAYS)
				{
					WorkedHoursByDay[DayNo-1] = WorkedHoursByDay[DayNo-1] + tmpHours;
				}
			}
		}

		public Boolean hasHoursLeft()
		{
			if (hours>0)
			{ 
				return true;
			}
			else
			{ 
				return false;
			}
		}

		#endregion Operational Methods 

		public string SaveToRunningXMLString()
		{
			string xmldata = string.Empty;
			
			try
			{
				xmldata  += "<Worker>\r\n";
				xmldata  += "<WorkerJobType>" + Enum.GetName(typeof(emResourcePeopleType),this.workerjobtype) + "</WorkerJobType>\r\n";
				xmldata  += "<WorkerSkillType>" + Enum.GetName(typeof(emResourceSkillType),this.workerskilltype) + "</WorkerSkillType>\r\n";
				xmldata  += "<CurrentProjectSlot" + this.currentprojectslot.ToString() + "</CurrentProjectSlot>\r\n";
				xmldata  += "<Hours>" + this.hours.ToString() + "</Hours>\r\n";
				xmldata  += "<debugid>" + this.debugid.ToString() + "</debugid>\r\n";
				//xmldata  += "<NormalWageRate>" + this.normalwagerate.ToString() + "</NormalWageRate>\r\n";
				//xmldata  += "<OverTimeWageRate>" + this.overtimewagerate.ToString() + "</OverTimeWageRate>\r\n";

				xmldata  += "<WorkedHoursByDay >";
				for (int step =0; step < GameConstants.MAX_NUMBER_DAYS; step++)
				{
					if (step!=0)
					{xmldata  +=",";}
					xmldata += WorkedHoursByDay[step].ToString();
				}
				xmldata  += "</WorkedHoursByDay >\r\n";

				xmldata  += "<WaitedHoursByDay >";
				for (int step =0; step < GameConstants.MAX_NUMBER_DAYS; step++)
				{
					if (step!=0)
					{xmldata  +=",";}
					xmldata += WaitedHoursByDay[step].ToString();
				}
				xmldata  += "</WaitedHoursByDay >\r\n";

				xmldata  += "</Worker>\r\n";
			}
			catch (Exception)
			{
			}
			return xmldata;
		}

	}
}
