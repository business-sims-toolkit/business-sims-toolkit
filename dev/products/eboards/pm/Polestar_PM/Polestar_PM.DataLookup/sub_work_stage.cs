using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml;

using LibCore;
using CoreUtils;
using Network;

namespace Polestar_PM.DataLookup
{
	/// <summary>
	/// 
	/// </summary>
	public class sub_work_stage : IComparable 
	{
		public string subtask_name = "";		//some sub tasks have display names (the subtasks in B and D)
		public bool dropped = false;				//Have we dropped this stage
		public bool droppable = false;			//Whether we can drop / descope this work item
		public bool sequential = false;			//Can we assign more than 1 person (seq=false) or limited to 1 (seq=true)
		public int defined_man_days = 1;		//Number of man days specified in this Task
		public int man_days = 1;						//Number of man days involved in this Task
		public int man_days_for_iterate = 1;//Used when we are iterative development (recycle)
		public int scope_hit;								//How much scope we give up, if we drop it. 
		
		public int wk_count = 0;						//Number of workers assigned (Used for prediction purposes)
		public int wk_int_count = 0;				//Number of internal workers assigned (Used for timesheet purposes)
		public int wk_ext_count = 0;				//Number of external workers assigned (Used for timesheet purposes)

		//Default Constructor
		public sub_work_stage()
		{ 
		}

		//Copy Constructor
		public sub_work_stage(sub_work_stage sws1)
		{
			subtask_name = sws1.subtask_name;
			dropped = sws1.dropped;
			droppable = sws1.droppable;
			sequential = sws1.sequential;
			defined_man_days = sws1.defined_man_days;
			man_days = sws1.man_days;
			man_days_for_iterate = sws1.man_days_for_iterate;
			scope_hit = sws1.scope_hit;
		}

		public void loadFromNode(Node nd)
		{
			if (nd != null)
			{
				subtask_name = nd.GetAttribute("subtask_name");
				dropped = nd.GetBooleanAttribute("dropped", false);
				droppable = nd.GetBooleanAttribute("droppable", false);
				sequential = nd.GetBooleanAttribute("sequential", false);
				defined_man_days = nd.GetIntAttribute("work_total", 0);
				man_days = nd.GetIntAttribute("work_left", 0);
				man_days_for_iterate = nd.GetIntAttribute("work_iterate_total", 0);
				scope_hit = nd.GetIntAttribute("scope_hit", 0);
			}
		}

		public int CompareTo(object obj)
		{
			sub_work_stage sws = obj as sub_work_stage;
			return this.man_days - sws.man_days;
		}

	}
}
