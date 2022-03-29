using System;
using System.IO;
using System.Xml;
using System.Collections;

using BaseUtils;
using CoreUtils;
using LibCore;

namespace Polestar_PM.DataLookup
{
	/// <summary>
	/// The Project task block represents the basic tasks that needs to be completed 
	/// </summary>
	public class Def_ProjectTask
	{
		private int sequence = 0;						//Sequence number used to ensure correct design sequence is upheld (internal)
		private int time_required = 0;			//How much time is requirred to do the job
		private int time_remaining = 0;			//How much time is left to do 
		private Boolean completed = false;	//Whether each task is completed 
		private Boolean assigned = false; 	//Whether each task is assigned to a worker
		private Boolean valid = false;  		//Is this task defined and valid
		private Boolean descoped = false;		//Is this task defined and valid
		private Boolean critical = false;		//Is this task critical 
		private int scopecost = 0;					//how much scope does this task contribute
		private int group = 0;							//Used for critical path 

		#region Accessors

		public Boolean Critical
		{
			get { return critical; }
			set { critical = value; }
		}

		public int ScopeGroup
		{
			get { return group; }
			set { group = value; }
		}

		public int ScopeCost
		{
			get { return scopecost; }
			set { scopecost = value; }
		}

		public int Sequence
		{
			get { return sequence; }
			set { sequence = value; }
		}

		public int Time_Required
		{
			get { return time_required; }
			set { time_required = value; }
		}

		public int Time_Remaining
		{
			get { return time_remaining; }
			set { time_remaining = value; }
		}

		public Boolean Completed
		{
			get { return completed; }
			set { completed = value; }
		}

		public Boolean AssignedWorker
		{
			get { return assigned; }
			set { assigned = value; }
		}

		public Boolean Valid
		{
			get { return valid; }
			set { valid = value; }
		}

		public Boolean DeScoped
		{
			get { return descoped; }
			set { descoped = value; }
		}

		#endregion Accessors

		#region Utils

		public void Clear()
		{
			time_required = 0;
			time_remaining = 0;
			completed = false;
			assigned = false;
			valid = false;
			descoped = false;
			critical = false;
			scopecost = 0;
		}

		public Boolean CopyData(Def_ProjectTask pt)	
		{
			Clear();
			this.Time_Required  = pt.Time_Required;
			this.Critical = pt.Critical;
			this.ScopeCost = pt.ScopeCost;
			this.ScopeGroup = pt.ScopeGroup;
			this.Completed = false;
			this.Valid = true;
			this.DeScoped = false;
			return true;
		}

		#endregion Utils

		#region XML Methods

		public void SetToDefault()
		{
			Time_Required = 8;
			Critical = true;
			ScopeCost = 0;
			ScopeGroup = 1;
			Time_Remaining = 0;
			Completed = false;
			Valid = true;
			DeScoped = false;
		}

		public Boolean LoadFromXMLNode(XmlNode xn)
		{
			Boolean OpSuccess = false;
			string ErrMsg = string.Empty;
			int ErrCount =0;
			
			try
			{
				//We only need to get the Time Requiring, 
				//the others are pure runtime values only used in game execution
				//Sequence = xml_utils.extractInt("Sequence", ref xn, 0, ref ErrCount, out ErrMsg);
				Time_Required = xml_utils.extractInt("Hours", ref xn, 0, ref ErrCount, out ErrMsg);
				Critical = xml_utils.extractBoolean("Critical", ref xn, false, ref ErrCount, out ErrMsg);
				ScopeCost = xml_utils.extractInt("ScopeCost", ref xn, 0, ref ErrCount, out ErrMsg);
				ScopeGroup = xml_utils.extractInt("Group", ref xn, 0, ref ErrCount, out ErrMsg);
				Time_Remaining = 0;
				Completed = false;
				Valid = true;
				OpSuccess = true;
				DeScoped = false;
			}
			catch (Exception evc)
			{
				string st = "ProjectTask LoadFromXml Exc " + evc.Message + "##" + evc.StackTrace;
				//LoggerSimple.TheInstance.Error(st);
			}

			return OpSuccess;
		}

		public string SaveToXMLString()
		{
			string xmldata = string.Empty;
			try
			{
				//We only get the Time Requiring, 
				//the others are pure runtime values only used in game execution
				xmldata  += "<Task>";
				//xmldata  += "<Sequence>"+ Sequence.ToString() + "</Sequence>";
				xmldata  += "<Hours>"+ Time_Required.ToString() + "</Hours>";
				xmldata  += "<Critical>"+ critical.ToString() + "</Critical>";
				xmldata  += "<ScopeCost>"+ scopecost.ToString() + "</ScopeCost>";
				xmldata  += "<Group>"+ ScopeGroup.ToString() + "</Group>";
				xmldata  += "</Task>\r\n";
			}
			catch (Exception evc)
			{
				string st = "ProjectTask SaveToXml Exc " + evc.Message + "##" + evc.StackTrace;
				//LoggerSimple.TheInstance.Error(st);
			}
			return xmldata;
		}

		public string SaveToRunningXMLString()
		{
			string xmldata = string.Empty;
			try
			{
				//We only get the Time Requiring, 
				//the others are pure runtime values only used in game execution
				xmldata  += "<projecttask>";
				xmldata  += "<Sequence>"+ Sequence.ToString() + "</Sequence>";
				xmldata  += "<TimeRequired>"+ Time_Required.ToString() + "</TimeRequired>";
				xmldata  += "<TimeRemaining>"+ Time_Remaining.ToString() + "</TimeRemaining>";
				xmldata  += "<Completed>"+ completed.ToString() + "</Completed>";
				xmldata  += "<Assigned>"+ assigned.ToString() + "</Assigned>";
				xmldata  += "<Valid>"+ valid.ToString() + "</Valid>";
				xmldata  += "<Descoped>"+ assigned.ToString() + "</Descoped>";
				xmldata  += "</projecttask>\r\n";
			}
			catch (Exception evc)
			{
				string st = "PTB SaveToXMLString Exc " + evc.Message + "##" + evc.StackTrace;
				//LoggerSimple.TheInstance.Error(st);
			}
			return xmldata;
		}

		#endregion XML Methods

		#region Operational Methods

		public void OverrideForBuyin(int hours)
		{
			time_required = hours;
			time_remaining = hours;
			completed = false;	
			assigned = false;
			descoped = false;
			valid = true;
		}

		public void Reset()
		{
			time_required = -1;
			time_remaining = 0;
			completed = false;	
			assigned = false;
			descoped = false;
			valid = false;
		}

		public void ReduceHours(int hours)
		{
			Time_Remaining = Time_Remaining - hours;
			if (Time_Remaining<=0)
			{
				this.Completed = true;
			}
		}

		public Boolean hasHoursLeft()
		{
			if (Time_Remaining >0)
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		#endregion Operational Methods

		#region debug

		/// <summary>
		/// Useful for printing out the state of the object
		/// </summary>
		/// <returns></returns>
		public string getDebugStr()
		{
			string str = string.Empty;
			str += "[PrjTsk]";
			str += "[Seq:"+sequence.ToString()+"]";
			str += "[TR:"+time_required.ToString()+"]";
			str += "[TM:"+time_remaining.ToString()+"]";
			str += "[CM:"+completed.ToString()+"]";
			str += "[AS:"+assigned.ToString()+"]";
			str += "[VD:"+valid.ToString()+"]";
			str += "[DS:"+descoped.ToString()+"]";
			return str;
		}

		#endregion debug

	}
}
