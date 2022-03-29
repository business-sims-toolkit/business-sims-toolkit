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
	/// The costs and task required to implement functionality in a particular technology 
	/// </summary>
	public class Def_Platform
	{
		static System.Globalization.CultureInfo myCI_enGB = new System.Globalization.CultureInfo( "en-GB", false );

    //Overall Attributes
		public int platformid=0;
		public string platformref; 	

    //Normal Execution Attributes (Custom In House Development)
		public int ne_totalcost = 0;							//Total In House Development Cost
		public int ne_implementationeffect = 0;		//How Effective the implementation is
		public int ne_memoryrequirements = 0;			//How much memory the software takes
		public int ne_diskrequirements = 0;				//How disk space the software takes
		public int ne_recycleimprovement = 0;			//How much we can improve this software by a design recycle

		public Hashtable newWorkStages = new Hashtable();

		#region Constructor

		public Def_Platform()
		{
		}

		#endregion Constructor

		#region Utils

		public void Clear()
		{
			platformid = 0; 	
			platformref = string.Empty;
			ne_totalcost = 0;
		  ne_implementationeffect = 0; 
			ne_memoryrequirements = 0;
			ne_diskrequirements = 0;
			ne_recycleimprovement = 0;
		}

		#endregion Utils

		#region XML Methods

		public Boolean LoadFromXMLNode(XmlNode xn)
		{
			Boolean OpSuccess = false;
			int ErrCount =0;
			string ErrMsg = string.Empty;
			
			try
			{
				this.platformid = xml_utils.extractInt("PlatformID", ref xn, 0, ref ErrCount, out ErrMsg);
        this.platformref = xml_utils.extractStr("PlatformRef", ref xn, string.Empty, ref ErrCount, out ErrMsg);
				this.ne_totalcost = xml_utils.extractInt("NE_TotalCost", ref xn, 0, ref ErrCount, out ErrMsg);				 
				this.ne_implementationeffect = xml_utils.extractInt("NE_ImplementationEffect", ref xn, 0, ref ErrCount, out ErrMsg);
				this.ne_diskrequirements = xml_utils.extractInt("NE_AppStoreRequirements", ref xn, 0, ref ErrCount, out ErrMsg);
				this.ne_memoryrequirements = xml_utils.extractInt("NE_AppMemRequirements", ref xn, 0, ref ErrCount, out ErrMsg);
				this.ne_recycleimprovement = xml_utils.extractInt("NE_RecycleImprovement", ref xn, 0, ref ErrCount, out ErrMsg);

				//System.Diagnostics.Debug.WriteLine("    PLATFORM "+CONVERT.ToStr(platformid));
				string debug = "" + "    PLATFORM " + CONVERT.ToStr(platformid);
				//====================================================================
				//==Tasks Length Definitions==========================================
				//====================================================================
				XmlNode xn2 = xn.SelectSingleNode("project_tasks");
				if (xn2 != null)
				{
					foreach (XmlNode xcn2 in xn2.ChildNodes)
					{
						if (xcn2.Name == "work_stage")
						{
							work_stage ws = new work_stage();
							ws.loadfromXML((XmlElement)xcn2);
							debug = debug + " " + ws.getStageAndLength() + " ";
							newWorkStages.Add((int)ws.getStage(), ws);
						}
					}
				}
				System.Diagnostics.Debug.WriteLine(debug);
			}
			catch (Exception evc)
			{
				string st = "Platform LoadFromXMLNode Exc " + evc.Message + "##" + evc.StackTrace;
				//LoggerSimple.TheInstance.Error(st);
			}

			return OpSuccess;
		}

		#endregion XML Methods

		public work_stage getProjectWorkStage(emPHASE_STAGE state_def)
		{
			return (work_stage)newWorkStages[(int)state_def];
		}

	}
}
