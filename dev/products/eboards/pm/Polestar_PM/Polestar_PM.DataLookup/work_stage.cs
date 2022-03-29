using System;
using System.Collections;
using System.Text;
using System.Xml;

using LibCore;
using CoreUtils;
using Network;

namespace Polestar_PM.DataLookup
{
	// A work stage defines the work "man days" required for a stage in a project (A,B,C,D,E,  F,G or H).
	//
	// The simplest form is used most of the time....
	// <work_stage stage="S" man_days="N" /> where S = A or B or C... and N is the number of man days required.
	//
	// For round 3 where we have to work out critical paths we use the extended definition that is of the form.
	// <work_stage stage="S" man_days_for_iterate="1">
	//		<sub_work_stage name="i" man_days="N" droppable="true" sequential="true" scope_cost="10"/>
	//		<sub_work_stage name="ii" man_days="N" droppable="true" sequential="true" scope_cost="90"/>
	//		<sub_work_stage name="iii" man_days="N" droppable="true" sequential="true" scope_cost="100"/>
	// </work_stage>
	//
	// In the extended case, we have the following attributes 
	//  name -- This is the display names that is shown to the user when using the descope critical path 
	//  man_days-- Number of man days fo this sub task
	//  droppable--some sub task can be dropped (using the descope critical path system)
	//  sequential--If true, this means that only one person can do the work in the subtask
	//  scope_cost -- This is how much we drop the scope by if this sub task is dropped 
	//
	// In addition the attribute man_days_for_iterate can be added to the work_stage element if we want to
	// override the default of "1".
	//	<work_stage stage="S" man_days_for_iterate="1"/>
	//
	// A work stage should have at least one sub_work_stage which may be implied in the XML by putting the man_days
	// as an attribute at the work_stage element level.
	//   <work_stage stage="A" man_days="6" />
	//
	// Round 3 also has curveballs which are defined in the work stage xml (Usually for B and D)
	//   <work_stage stage="B" curveball="i,3" /> 
	//   When the software moves into the stage, it check for the presence of a curveball
	//
	// When the game is playing in single section mode, everystage is considered a dev stage
	// So when we go to select people, we always select from a single pool of people (the dev people)
	//
	// There is a extra set of optional parameters which allow the prefered initial staffing level to be indicated
	// <work_stage stage="S" man_days="N" initial_int_staff="J" initial_ext_staff="K"/> 
	//   where J is the number of internal staff prefered and K is the number of external staff prefered. 
	// These levels can be overrided by the players during the game but allow the game designers to predefine 
	// staff levels for a very fast selection process where aspects are predefined. 
	// You can only define the staff levels at the work stage level (not for the sub stages)
	// 
	// 
	public class work_stage
	{
		public ArrayList sub_work_stages = new ArrayList();
		private emPHASE_STAGE stage = emPHASE_STAGE.STAGE_A;

		private Node network_stage_node = null;

		public int prefered_int_level = 0;
		public int prefered_ext_level = 0;

		private string curveball = string.Empty;

		private bool use_single_staff_section = false;

		public emPHASE phase
		{
			get
			{
				switch (stage)
				{
					case emPHASE_STAGE.STAGE_A:  //Normal DEV
					case emPHASE_STAGE.STAGE_B:  //Normal DEV
					case emPHASE_STAGE.STAGE_C:  //Normal DEV
					case emPHASE_STAGE.STAGE_D:  //Normal DEV
					case emPHASE_STAGE.STAGE_E:  //Normal DEV
					case emPHASE_STAGE.STAGE_I:  //Recycle DEV
					case emPHASE_STAGE.STAGE_K:  //Recycle DEV
					case emPHASE_STAGE.STAGE_M:  //Recycle DEV
					case emPHASE_STAGE.STAGE_P:  //Recycle DEV
						return emPHASE.DEV;

					case emPHASE_STAGE.STAGE_F:  //Normal Test
					case emPHASE_STAGE.STAGE_G:  //Normal Test
					case emPHASE_STAGE.STAGE_H:  //Normal Test
					case emPHASE_STAGE.STAGE_J:  //Recycle Test 
					case emPHASE_STAGE.STAGE_L:  //Recycle Test 
					case emPHASE_STAGE.STAGE_N:  //Recycle Test 
					case emPHASE_STAGE.STAGE_Q:  //Recycle Test 
						return emPHASE.TEST;
				}
				return emPHASE.DEV;
			}
		}

		public work_stage()
		{
			use_single_staff_section = false;
			string UseSingleStaffSection_str = SkinningDefs.TheInstance.GetData("use_single_staff_section", "false");
			if (UseSingleStaffSection_str.IndexOf("true") > -1)
			{
				use_single_staff_section = true;
			}
		}

		/// <summary>
		/// Dispose
		/// </summary>
		public void Dispose()
		{

		}

		public Node getStageNode()
		{
			return network_stage_node;
		}

		public int getCompletedDaysFromNode()
		{
			int completed_days_count;
			completed_days_count = network_stage_node.GetIntAttribute("completed_days",0);
			return completed_days_count;
		}

		/// <summary>
		/// Only used to determine the number of man-days from the xml definations
		/// </summary>
		/// <returns></returns>
		public int getXMLTaskCountForStage()
		{
			int total_man_days = 0;
			foreach (sub_work_stage sws in sub_work_stages)
			{
				total_man_days += sws.man_days;
			}
			return total_man_days;
		}

		/// <summary>
		/// This extracts the defined task numbers from the XML
		/// We dont load the recycle stage from XML. they have 1 day, task
		/// </summary>
		/// <param name="node"></param>
		public void loadfromXML(XmlElement node)
		{
			string str_stage = node.Attributes["stage"].Value;
			switch (str_stage)
			{
				case "A": { stage = emPHASE_STAGE.STAGE_A; } break;
				case "B": { stage = emPHASE_STAGE.STAGE_B; } break;
				case "C": { stage = emPHASE_STAGE.STAGE_C; } break;
				case "D": { stage = emPHASE_STAGE.STAGE_D; } break;
				case "E": { stage = emPHASE_STAGE.STAGE_E; } break;
				case "F": { stage = emPHASE_STAGE.STAGE_F; } break;
				case "G": { stage = emPHASE_STAGE.STAGE_G; } break;
				case "H": { stage = emPHASE_STAGE.STAGE_H; } break;
			}
			
			//Extracting out the optional internal staff level
			XmlAttribute prefered_int_staff_level_attr = node.Attributes.GetNamedItem("initial_int_staff") as XmlAttribute;
			if (prefered_int_staff_level_attr != null)
			{
				prefered_int_level = LibCore.CONVERT.ParseIntSafe(prefered_int_staff_level_attr.Value,0);
			}
			
			//Extracting out the optional external staff level
			XmlAttribute prefered_ext_staff_level_attr = node.Attributes.GetNamedItem("initial_ext_staff") as XmlAttribute;
			if (prefered_ext_staff_level_attr != null)
			{
				prefered_ext_level = LibCore.CONVERT.ParseIntSafe(prefered_ext_staff_level_attr.Value,0);
			}

			// Do we have a number of man days set insode the main element?
			XmlAttribute man_days = node.Attributes.GetNamedItem("man_days") as XmlAttribute;
			if (man_days != null)
			{
				// Create a sub_work_stage with this many man_days.
				sub_work_stage sub_stage = new sub_work_stage();
				sub_stage.man_days = LibCore.CONVERT.ParseInt(man_days.Value);
				sub_work_stages.Add(sub_stage);
			}

			// Do we have a curveball defined as part of this stage
			XmlAttribute curveball_attr = node.Attributes.GetNamedItem("curveball") as XmlAttribute;
			if (curveball_attr != null)
			{
				this.curveball = curveball_attr.Value;
			}

			//
			// Add any explicitly defined sub_work_stages.
			//
			foreach (XmlNode child in node.ChildNodes)
			{
				int defined_sub_work_days = 0;
				int defined_scope_cost = 0;
				bool defined_sequential = false;
				bool defined_droppable = false;
				string defined_name = "";

				string st = child.InnerXml;
				//string st2 = "";

				//Extract the man days 
				XmlAttribute man_days_attr = child.Attributes.GetNamedItem("man_days") as XmlAttribute;
				if (man_days_attr != null)
				{
					defined_sub_work_days = LibCore.CONVERT.ParseIntSafe(man_days_attr.Value, 0);
				}
				//Extract whether we need to perform in parallel or sequential 
				XmlAttribute allowed_sequential_attr = child.Attributes.GetNamedItem("sequential") as XmlAttribute;
				if (allowed_sequential_attr != null)
				{
					defined_sequential = LibCore.CONVERT.ParseBool(allowed_sequential_attr.Value,false);
				}

				//Extract whether we need to perform in parallel or sequential 
				XmlAttribute defined_droppable_attr = child.Attributes.GetNamedItem("droppable") as XmlAttribute;
				if (defined_droppable_attr != null)
				{
					defined_droppable = LibCore.CONVERT.ParseBool(defined_droppable_attr.Value,false);
				}
				
				//Extract the scope cost in the dropping of this item 
				XmlAttribute defined_scope_cost_attr = child.Attributes.GetNamedItem("scope_cost") as XmlAttribute;
				if (defined_scope_cost_attr != null)
				{
					defined_scope_cost = LibCore.CONVERT.ParseIntSafe(defined_scope_cost_attr.Value,0);
				}
				//Extract the scope cost in the dropping of this item 
				XmlAttribute defined_name_attr = child.Attributes.GetNamedItem("name") as XmlAttribute;
				if (defined_name_attr != null)
				{
					defined_name = defined_name_attr.Value;
				}

				sub_work_stage sub_stage = new sub_work_stage();
				sub_stage.subtask_name = defined_name;
				sub_stage.man_days = defined_sub_work_days;
				sub_stage.droppable = defined_droppable;
				sub_stage.sequential = defined_sequential;
				sub_stage.scope_hit = defined_scope_cost;
				sub_work_stages.Add(sub_stage);
			}
		}

		public void loadfromRecycle(string tmpstage)
		{
			switch (tmpstage)
			{
				case "I": { stage = emPHASE_STAGE.STAGE_I; } break;
				case "J": { stage = emPHASE_STAGE.STAGE_J; } break;
				case "K": { stage = emPHASE_STAGE.STAGE_K; } break;
				case "L": { stage = emPHASE_STAGE.STAGE_L; } break;
				case "M": { stage = emPHASE_STAGE.STAGE_M; } break;
				case "N": { stage = emPHASE_STAGE.STAGE_N; } break;
				case "P": { stage = emPHASE_STAGE.STAGE_P; } break;
				case "Q": { stage = emPHASE_STAGE.STAGE_Q; } break;
				case "R": { stage = emPHASE_STAGE.STAGE_R; } break;
				case "T": { stage = emPHASE_STAGE.STAGE_T; } break;
			}
			prefered_int_level = 1;  //preference int staff 
			prefered_ext_level = 0;  //preference ext staff 

			// Do we have a number of man days set insode the main element?
			int man_days = 1;
			if (man_days > 0)
			{
				// Create a sub_work_stage with this many man_days.
				sub_work_stage sub_stage = new sub_work_stage();
				sub_stage.man_days = 1;
				sub_work_stages.Add(sub_stage);
			}
		}

		public void loadfromNetworkStageNode(Node stagenode)
		{
			network_stage_node = stagenode;
			//need to extract the stage from the node attributes
			string code_str = network_stage_node.GetAttribute("code");
			switch (code_str)
			{ 
				case "dev_a":			
					stage = emPHASE_STAGE.STAGE_A;
					break;
				case "dev_b":
					stage = emPHASE_STAGE.STAGE_B;
					break;
				case "dev_c":
					stage = emPHASE_STAGE.STAGE_C;
					break;
				case "dev_d":
					stage = emPHASE_STAGE.STAGE_D;
					break;
				case "dev_e":
					stage = emPHASE_STAGE.STAGE_E;
					break;
				case "test_f":
					stage = emPHASE_STAGE.STAGE_F;
					break;
				case "test_g":
					stage = emPHASE_STAGE.STAGE_G;
					break;
				case "test_h":
					stage = emPHASE_STAGE.STAGE_H;
					break;

				case "dev_i":
					stage = emPHASE_STAGE.STAGE_I;
					break;
				case "test_j":
					stage = emPHASE_STAGE.STAGE_J;
					break;
				case "dev_k":
					stage = emPHASE_STAGE.STAGE_K;
					break;
				case "test_l":
					stage = emPHASE_STAGE.STAGE_L;
					break;
				case "dev_m":
					stage = emPHASE_STAGE.STAGE_M;
					break;
				case "test_n":
					stage = emPHASE_STAGE.STAGE_N;
					break;
				case "dev_p":
					stage = emPHASE_STAGE.STAGE_P;
					break;
				case "test_q":
					stage = emPHASE_STAGE.STAGE_Q;
					break;
				case "dev_r":
					stage = emPHASE_STAGE.STAGE_R;
					break;
				case "test_s":
					stage = emPHASE_STAGE.STAGE_T;
					break;
			}
			//extract the sub-workstages as well
		}

		public void loadSubTasks()
		{
			if (network_stage_node != null)
			{
				this.sub_work_stages.Clear();
				foreach (Node nd in network_stage_node.getChildren())
				{
					string task_type = nd.GetAttribute("type");
					switch (task_type)
					{
						case "work_task":
							sub_work_stage sws = new sub_work_stage();
							sws.loadFromNode(nd);
							this.sub_work_stages.Add(sws);
							break;
					}
				}
			}
		}

		public bool isCurveBallDefined() 
		{
			bool cb_exists = false; 
			if (network_stage_node != null)
			{ 
				string cb_text = network_stage_node.GetAttribute("curveball");
				cb_exists = (cb_text != string.Empty);
			}
			return cb_exists;
		}

		public bool perform_CurveBall_if_Needed()
		{
			bool altered = false;
			if (network_stage_node != null)
			{
				string cb_text = network_stage_node.GetAttribute("curveball");
				string[] parts = cb_text.Split(',');
				string target_name_str = parts[0];
				string target_added_days_str = parts[1];

				foreach (Node nd in network_stage_node.getChildren())
				{
					string task_type = nd.GetAttribute("type");
					switch (task_type)
					{
						case "work_task":
							string sws_name = nd.GetAttribute("subtask_name");
							if (sws_name.ToLower() == target_name_str.ToLower())
							{
								string tk_status = nd.GetAttribute("status");
								int tk_tasktotal = nd.GetIntAttribute("work_total", 0);
								int tk_taskleft = nd.GetIntAttribute("work_left", 0);

								int additional_days = CONVERT.ParseIntSafe(target_added_days_str, 0);
								tk_tasktotal += additional_days;
								tk_taskleft += additional_days;
								nd.SetAttribute("work_total", CONVERT.ToStr(tk_tasktotal));
								nd.SetAttribute("work_left", CONVERT.ToStr(tk_taskleft));
								altered = true;
							}
							break;
					}
				}
				if (altered)
				{ 
					//refresh the sws list 
					loadSubTasks();
					int tmp_predicted_days = 0;
					this.RecalculatePredictedDays(true, true, out tmp_predicted_days);
					this.RecalculateStageTaskNumbers();
				}
			}
			return altered;
		}

		public emPHASE_STAGE getStage()
		{
			return stage;
		}

		public string getStageAndLength()
		{
			string debug = "";
			string phase = "";
			string action = "";
			int mandays_left = 0;

			foreach (sub_work_stage sws in sub_work_stages)
			{
				mandays_left += sws.man_days;
			}

			getPhaseAndStage(out phase, out action);
			debug = action + ":" + mandays_left.ToString();
			return debug;
		}

		private void getPhaseAndStage(out string phase, out string action)
		{
			phase = "Dev";
			action = "A";

			switch (stage)
			{
				case emPHASE_STAGE.STAGE_A:
					phase = "Dev";
					action = "A";
					break;
				case emPHASE_STAGE.STAGE_B:
					phase = "Dev";
					action = "B";
					break;
				case emPHASE_STAGE.STAGE_C:
					phase = "Dev";
					action = "C";
					break;
				case emPHASE_STAGE.STAGE_D:
					phase = "Dev";
					action = "D";
					break;
				case emPHASE_STAGE.STAGE_E:
					phase = "Dev";
					action = "E";
					break;
				case emPHASE_STAGE.STAGE_F:
					phase = "Test";
					action = "F";
					break;
				case emPHASE_STAGE.STAGE_G:
					phase = "Test";
					action = "G";
					break;
				case emPHASE_STAGE.STAGE_H:
					phase = "Test";
					action = "H";
					break;
				case emPHASE_STAGE.STAGE_I:
					phase = "Dev";
					action = "I";
					break;
				case emPHASE_STAGE.STAGE_J:
					phase = "Test";
					action = "J";
					break;
				case emPHASE_STAGE.STAGE_K:
					phase = "Dev";
					action = "K";
					break;
				case emPHASE_STAGE.STAGE_L:
					phase = "Test";
					action = "L";
					break;
				case emPHASE_STAGE.STAGE_M:
					phase = "Dev";
					action = "M";
					break;
				case emPHASE_STAGE.STAGE_N:
					phase = "Test";
					action = "N";
					break;
				case emPHASE_STAGE.STAGE_P:
					phase = "Dev";
					action = "P";
					break;
				case emPHASE_STAGE.STAGE_Q:
					phase = "Test";
					action = "Q";
					break;
				case emPHASE_STAGE.STAGE_R:
					phase = "Dev";
					action = "R";
					break;
				case emPHASE_STAGE.STAGE_T:
					phase = "Test";
					action = "T";
					break;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="parentnode"></param>
		/// <param name="request_int_staff"></param>
		/// <param name="request_ext_staff"></param>
		/// <param name="number_of_days"></param>
		public void create_work_stage_node(Node parentnode, int request_int_staff, int request_ext_staff, 
			bool connect_node_ref, out int predicted_number_of_days, out int total_stage_task_count)
		{
			//work out how day in total 
			int total_man_days = 0;
			int totalTasks = 0;
			int predicted_days = 0;
			bool hasSequential = false; 
			ArrayList workList = new ArrayList();

			foreach (sub_work_stage sws in sub_work_stages)
			{
				//need to to take account of Sequential stages (used in R3)
				total_man_days += sws.man_days;
				if (sws.sequential)
				{
					hasSequential = true;
				}
				sub_work_stage sws2 = new sub_work_stage(sws);
				workList.Add(sws2);
			}
			totalTasks = total_man_days;

			if (hasSequential)
			{
				int current_staff = request_int_staff + request_ext_staff;
				predicted_days = GeneratePredictedDaysForSequentialTasksFromList(current_staff, workList);
			}
			else
			{
				totalTasks = total_man_days;
				predicted_days = totalTasks / 1;
				if ((request_int_staff + request_ext_staff) > 0)
				{
					predicted_days = totalTasks / (request_int_staff + request_ext_staff);
					if ((totalTasks % (request_int_staff + request_ext_staff)) > 0)
					{
						predicted_days += 1;
					}
				}
			}
			
			//Assign the output information 
			predicted_number_of_days = predicted_days;
			total_stage_task_count = totalTasks;

			string desc = "Dev";
			string action = "A";
			getPhaseAndStage(out desc, out action);

			ArrayList attrs = new ArrayList(); 
			attrs.Clear();
			attrs.Add(new AttributeValuePair("type", "stage"));
			attrs.Add(new AttributeValuePair("desc", desc));
			attrs.Add(new AttributeValuePair("action", action));
			attrs.Add(new AttributeValuePair("curveball", curveball));
			attrs.Add(new AttributeValuePair("scope_level", "100"));
			attrs.Add(new AttributeValuePair("code", desc.ToLower() + "_" + action.ToLower()));
			attrs.Add(new AttributeValuePair("tasks_total", CONVERT.ToStr(totalTasks)));
			attrs.Add(new AttributeValuePair("tasks_left", CONVERT.ToStr(totalTasks)));
			attrs.Add(new AttributeValuePair("tasks_dropped", "0"));
			attrs.Add(new AttributeValuePair("staff_int_requested", CONVERT.ToStr(request_int_staff)));
			attrs.Add(new AttributeValuePair("staff_int_assigned", "0"));
			attrs.Add(new AttributeValuePair("staff_ext_requested", CONVERT.ToStr(request_ext_staff)));
			attrs.Add(new AttributeValuePair("staff_ext_assigned", "0"));
			attrs.Add(new AttributeValuePair("predicted_days", CONVERT.ToStr(predicted_days)));
			attrs.Add(new AttributeValuePair("completed_days", "0"));
			attrs.Add(new AttributeValuePair("status", "todo"));
			Node stage_node = new Node(parentnode, "stage", "", attrs);

			//System.Diagnostics.Debug.WriteLine("Stage " + desc.ToLower() + "_" + action.ToLower() 
			//	+ "  tasks:" + CONVERT.ToStr(totalTasks) + " pdays:" + CONVERT.ToStr(predicted_days));

			if (connect_node_ref)
			{
				this.network_stage_node = stage_node;
			}

			//A Holder for workers who are Doing Nothing 
			attrs.Clear();
			attrs.Add(new AttributeValuePair("type", "doing_nothing"));
			new Node(stage_node, "tasks", "", attrs);

			//Iterate through the sub_work_stage creating the work sub_tasks
			foreach (sub_work_stage sub_stage in sub_work_stages)
			{
				attrs.Clear();
				attrs.Add(new AttributeValuePair("type", "work_task"));
				attrs.Add(new AttributeValuePair("work_iterate_total", CONVERT.ToStr(sub_stage.man_days_for_iterate)));
				attrs.Add(new AttributeValuePair("work_total", CONVERT.ToStr(sub_stage.man_days)));
				attrs.Add(new AttributeValuePair("work_left", CONVERT.ToStr(sub_stage.man_days)));
				attrs.Add(new AttributeValuePair("work_dropped", "0"));
				attrs.Add(new AttributeValuePair("status", "todo"));
				attrs.Add(new AttributeValuePair("subtask_name", sub_stage.subtask_name.ToLower()));
				attrs.Add(new AttributeValuePair("scope_hit", CONVERT.ToStr(sub_stage.scope_hit).ToLower()));
				attrs.Add(new AttributeValuePair("sequential", sub_stage.sequential.ToString().ToLower()));
				attrs.Add(new AttributeValuePair("droppable", sub_stage.droppable.ToString().ToLower()));
				attrs.Add(new AttributeValuePair("dropped", "false"));
				new Node(stage_node, "work_task", "", attrs);

				//System.Diagnostics.Debug.WriteLine("SubTask " + sub_stage.subtask_name + "wk:" + CONVERT.ToStr(sub_stage.man_days) + " seq:" + sub_stage.sequential.ToString());
			}
			//System.Diagnostics.Debug.WriteLine("Create Work Stage Complete");
		}

		public void CreateNewSubTask(int numberManDays) 
		{
			ArrayList attrs = new ArrayList(); 
			attrs.Clear();
			attrs.Add(new AttributeValuePair("type", "work_task"));
			attrs.Add(new AttributeValuePair("work_iterate_total", CONVERT.ToStr(1)));
			attrs.Add(new AttributeValuePair("work_total", CONVERT.ToStr(numberManDays)));
			attrs.Add(new AttributeValuePair("work_left", CONVERT.ToStr(numberManDays)));
			attrs.Add(new AttributeValuePair("work_dropped", "0"));
			attrs.Add(new AttributeValuePair("status", "todo"));
			attrs.Add(new AttributeValuePair("sequential", "false"));
			attrs.Add(new AttributeValuePair("droppable", "true"));
			attrs.Add(new AttributeValuePair("dropped", "false"));
			attrs.Add(new AttributeValuePair("scope_hit", "0"));
			new Node(network_stage_node, "work_task", "", attrs);
			this.RecalculateStageTaskNumbers();

			int new_predicted_days = 0;
			this.RecalculatePredictedDays(true, true, out new_predicted_days);
		}

		public string getCode()
		{
			return network_stage_node.GetAttribute("action");
		}

		public string getDesc()
		{
			return network_stage_node.GetAttribute("desc");
		}

		/// <summary>
		/// Whether this stage is a Development Stage
		/// </summary>
		/// <returns></returns>
		public bool isDev()
		{
			bool isdevelopment = (phase == emPHASE.DEV);

			if (use_single_staff_section)
			{
				isdevelopment = true;
			}
			return isdevelopment;
		}

		private int getTasksLeftCount()
		{
			return network_stage_node.GetIntAttribute("tasks_left", -1);
		}

		private int getTotalTasks()
		{
			return network_stage_node.GetIntAttribute("tasks_total", 0);
		}

		public void changeStateToInProgress()
		{
			network_stage_node.SetAttribute("status", "inprogress");
		}

		public int getPredictedDays()
		{
			return network_stage_node.GetIntAttribute("predicted_days", 0);
		}

		public int getPredictedStageDuration()
		{
			return network_stage_node.GetIntAttribute("predicted_days",0);
		}

		public void markStateDone()
		{
			network_stage_node.SetAttribute("status", "done");
		}

		public bool isStateDone()
		{
			bool status_done = false;
			if (network_stage_node != null)
			{
				string status_str = network_stage_node.GetAttribute("status", "done");
				if (status_str.ToLower() == "done")
				{
					status_done = true;
				}
			}
			return status_done;
		}

		public bool isStageComplete()
		{
			bool stage_completed = false;
			int tasks_left = getTasksLeftCount();
			if (tasks_left == 0)
			{
				stage_completed = true;
			}
			return stage_completed;
		}

		private Node getDoingNothingNode()
		{
			Node tmpNode = null;
			foreach (Node tn in network_stage_node.getChildren())
			{
				string node_type = tn.GetAttribute("type");
				if (node_type.ToLower() == "doing_nothing")
				{
					tmpNode = tn;
				}
			}
			return tmpNode;
		}

		public void getFullTaskCountsForStage(out int total_task_count, 
			out int dropped_task_count, out int remaining_task_count)
		{
			total_task_count = network_stage_node.GetIntAttribute("tasks_total", 0);
			dropped_task_count = network_stage_node.GetIntAttribute("tasks_dropped", 0);
			remaining_task_count = network_stage_node.GetIntAttribute("tasks_left", 0);
		}

		public void getRequestedResourceLevels(out int requested_int, 
			out int requested_ext, out bool isInProgress, out bool isCompleted)
		{
			isInProgress = false;
			isCompleted = false;

			string status = network_stage_node.GetAttribute("status");
			if (status.ToLower() == "done")
			{
				isCompleted = true;
			}
			if (status.ToLower() == "inprogress")
			{
				isInProgress = true;
			}
			requested_int = network_stage_node.GetIntAttribute("staff_int_requested", 0);
			requested_ext = network_stage_node.GetIntAttribute("staff_ext_requested", 0); 
		}

		public bool setRequestedResourceLevels(int requested_int, int requested_ext)
		{
			bool OpSuccess = false;
			int predicted_days = 0;

			string status = network_stage_node.GetAttribute("status");
			bool stage_in_progress = status.ToLower() == "inprogress";
			bool stage_is_done = status.ToLower() == "done";

			if (stage_is_done)
			{
			}
			else
			{
				network_stage_node.SetAttribute("staff_int_requested", CONVERT.ToStr(requested_int));
				network_stage_node.SetAttribute("staff_ext_requested", CONVERT.ToStr(requested_ext));
				this.RecalculatePredictedDays(true, true, out predicted_days);
				OpSuccess = true;
			}
			return OpSuccess;
		}

		private void getWorkProfileFromSWSList(ArrayList sws_list, out int concurrent_task_count, out int seq_task_count, out int max_seq_task)
		{
			concurrent_task_count = 0;
			max_seq_task = 0;
			seq_task_count = 0;

			//System.Diagnostics.Debug.WriteLine("getWorkProfileFromSWS");
			foreach (sub_work_stage sws in sws_list)
			{
				//System.Diagnostics.Debug.WriteLine(sws.subtask_name + " m:" + CONVERT.ToStr(sws.man_days)
				//	+ " seq:" + CONVERT.ToStr(sws.sequential) + " drp: " + CONVERT.ToStr(sws.dropped));
				if (sws.dropped == false)
				{
					if (sws.sequential)
					{
						seq_task_count += 1;
						if (sws.man_days > max_seq_task)
						{
							max_seq_task = sws.man_days;
						}
					}
					else
					{
						concurrent_task_count += sws.man_days;
					}
				}
			}
			//System.Diagnostics.Debug.WriteLine("Work ProfileFromSWS  "
			//	+ " C:" + CONVERT.ToStr(concurrent_task_count) + " max_seq:" + CONVERT.ToStr(max_seq_task));			
		}

		private void getWorkProfile(out int concurrent_task_count, out int seq_task_count, out int max_seq_task)
		{ 
			concurrent_task_count = 0;
			max_seq_task = 0;
			seq_task_count = 0;

			//System.Diagnostics.Debug.WriteLine("getWorkProfile");
			//this.sub_work_stages.Count

			foreach (Node nd in network_stage_node.getChildren())
			{
				string task_type = nd.GetAttribute("type");
				switch (task_type)
				{
					case "work_task":
						sub_work_stage sws = new sub_work_stage();
						sws.loadFromNode(nd);

						//System.Diagnostics.Debug.WriteLine(sws.subtask_name+" m:"+CONVERT.ToStr(sws.man_days)
						//	+ " seq:"+CONVERT.ToStr(sws.sequential) + " drp: "+CONVERT.ToStr(sws.dropped));
						if (sws.dropped == false)
						{
							if (sws.sequential)
							{
								seq_task_count += 1;
								if (sws.man_days > max_seq_task)
								{
									max_seq_task = sws.man_days;
								}
							}
							else
							{
								concurrent_task_count += sws.man_days;
							}
						}
						break;
				}
			}
			//System.Diagnostics.Debug.WriteLine("Work Profile  " 
			//	+ " C:" + CONVERT.ToStr(concurrent_task_count) + " max_seq:" + CONVERT.ToStr(max_seq_task));
		}

		public int GenerateTimeSheetsForStage(int day_start, int currentDay, Hashtable ht)
		{
			if (day_start == -1)
			{ 
				//start from the day after the max day in the hashtable keys 
				foreach (int day_number in ht.Keys)
				{
					if (day_number > day_start)
					{
						day_start = day_number;
					}
				}
			}
			//=================================================================
			//extract the staff levels 
			//=================================================================
			int requested_int = network_stage_node.GetIntAttribute("staff_int_requested", 0);
			int requested_ext = network_stage_node.GetIntAttribute("staff_ext_requested", 0);
			//=================================================================
			//extract the sws list
			//=================================================================
			ArrayList sws_list = new ArrayList();
			foreach (Node nd in network_stage_node.getChildren())
			{
				string task_type = nd.GetAttribute("type");
				switch (task_type)
				{
					case "work_task":
						sub_work_stage sws = new sub_work_stage();
						sws.loadFromNode(nd);
						if ((sws.dropped == false) & (sws.man_days > 0))
						{
							sws_list.Add(sws);
						}
						break;
				}
			}
			//=================================================================
			//extract the sws list
			//=================================================================
			bool isDevStage = this.isDev();
			int duration = 0;
			duration = GenerateWorksheetsFromListTasks(requested_int, requested_ext, day_start, currentDay, isDevStage, sws_list, ht);
			return duration;
		}

		private static int GenerateWorksheetsFromListTasks(int staff_int, int staff_ext, int day_start, int current_day, bool isDev,
			ArrayList swsWorkList, Hashtable worksheets)
		{
			int default_duration = 0;
			int number_of_days_left = 0;
			int number_of_predicted_days = 0;

			// Bug 7789: if we've started work, then swsWorkList will already have today's work deducted from it,
			// which will shorten the first task's duration by a day.  Fake a replacement task
			// with the extra day added back in.
			if (current_day >= day_start)
			{
				if (swsWorkList.Count > 0)
				{
					((sub_work_stage) swsWorkList[0]).man_days += (staff_int + staff_ext);
				}
			}

			//System.Diagnostics.Debug.WriteLine("====================================================");
			//System.Diagnostics.Debug.WriteLine("====================================================");
			//System.Diagnostics.Debug.WriteLine("GenerateWorksheetsFromListTasks");
			int staff_int_current = staff_int;
			int staff_ext_current = staff_ext;
			//=============================================================
			//determine total days left (it's the max possible duration)
			//=============================================================
			default_duration = 0;
			foreach (sub_work_stage sws in swsWorkList)
			{
				default_duration += sws.man_days;
			}
			number_of_days_left = default_duration;
			//System.Diagnostics.Debug.WriteLine("Scanning " + CONVERT.ToStr(default_duration));
			//=============================================================
			//Interate through the days, processing the worklist 
			//=============================================================
			//if ((staff_int_current + staff_ext_current) != 1)
			if ((staff_int_current + staff_ext_current) > 0)
			{
				//System.Diagnostics.Debug.WriteLine("====================================================");
				int day_counter = 0;
				int required_staff_level = 0;
				while ((number_of_days_left > 0) && (day_counter < default_duration))
				{
					swsWorkList.Sort(); //sort into ascending 
					swsWorkList.Reverse(); //reverse as we want work with biggest first
					//System.Diagnostics.Debug.WriteLine("Day Counter " + CONVERT.ToStr(day_counter));
					//System.Diagnostics.Debug.WriteLine(" number_of_days_left " + CONVERT.ToStr(number_of_days_left));
					//System.Diagnostics.Debug.WriteLine(" default_duration " + CONVERT.ToStr(default_duration));

					foreach (sub_work_stage sws in swsWorkList)
					{
						sws.wk_count = sws.wk_ext_count + sws.wk_int_count;

						required_staff_level = sws.man_days - sws.wk_count;
						if (required_staff_level > 0) //we want more people 
						{
							if (sws.sequential) //but if we are sequential (limited to one person)
							{
								if (sws.wk_count == 0) //if we don't already have the person
								{
									required_staff_level = 1; //ask for him
								}
								else
								{
									required_staff_level = 0; //already filled the vacancy
								}
							}
							//extract workers from pool if needed (use int pool before the ext pool)
							if (staff_int_current >= required_staff_level)
							{
								//More available internal staff than the current demand
								sws.wk_int_count = sws.wk_int_count + required_staff_level;
								staff_int_current = staff_int_current - required_staff_level;
								required_staff_level = 0;
								//System.Diagnostics.Debug.WriteLine("Adding more int staff (P) " + sws.subtask_name + " int:" + sws.wk_int_count.ToString() + "  StaffIntCurr" + staff_int_current.ToString());
							}
							else
							{
								//less than the demand
								sws.wk_int_count = sws.wk_int_count + staff_int_current;
								staff_int_current = 0;
								required_staff_level = required_staff_level - staff_int_current;
								//System.Diagnostics.Debug.WriteLine("Adding more int staff (F) " + sws.subtask_name + " int:" + sws.wk_int_count.ToString() + "  StaffIntCurr" + staff_int_current.ToString());
							}
							//extract workers from pool if needed (use int pool before the ext pool)
							if (required_staff_level > 0)
							{
								//extract workers from pool if needed (using ext pool)
								if (staff_ext_current >= required_staff_level)
								{
									//More available internal staff than the current demand
									sws.wk_ext_count = sws.wk_ext_count + required_staff_level;
									staff_ext_current = staff_ext_current - required_staff_level;
									required_staff_level = 0;
									//System.Diagnostics.Debug.WriteLine("Adding more ext staff (P) " + sws.subtask_name + " ext:" + sws.wk_ext_count.ToString() + "  StaffExtCurr" + staff_ext_current.ToString());
								}
								else
								{
									//less than the demand
									sws.wk_ext_count = sws.wk_ext_count + staff_ext_current;
									staff_ext_current = 0;
									required_staff_level = required_staff_level - staff_ext_current;
									//System.Diagnostics.Debug.WriteLine("Adding more ext staff (F) " + sws.subtask_name + " ext:" + sws.wk_ext_count.ToString() + "  StaffExtCurr" + staff_ext_current.ToString());
								}
							}
						}
						else
						{
							if (required_staff_level < 0)
							{
								//We want less people (release people from the sws, ext staff first)
								if (sws.wk_ext_count > 0)
								{
									if (sws.wk_ext_count > required_staff_level)
									{
										//We can release enough staff by purely releasing ing ext staff
										sws.wk_ext_count = sws.wk_ext_count - Math.Abs(required_staff_level);
										staff_ext_current = staff_ext_current + Math.Abs(required_staff_level);
										required_staff_level = 0;
										//System.Diagnostics.Debug.WriteLine("Release Ext Staff A " + sws.subtask_name + " wkc" + sws.wk_ext_count.ToString() + "  StaffExtCurr" + staff_ext_current.ToString());
									}
									else
									{
										//We can release enough staff by purely releasing ing ext staff
										staff_ext_current = staff_ext_current + Math.Abs(sws.wk_ext_count);
										required_staff_level = required_staff_level - Math.Abs(sws.wk_ext_count);
										sws.wk_ext_count = 0;
										//System.Diagnostics.Debug.WriteLine("Release Ext Staff B " + sws.subtask_name + " wkc" + sws.wk_ext_count.ToString() + "  StaffExtCurr" + staff_ext_current.ToString());
									}
								}
								//now release any int staff
								if ((sws.wk_int_count > 0) && (required_staff_level > 0))
								{
									if (sws.wk_int_count > required_staff_level)
									{
										//We can release enough staff by purely releasing ing ext staff
										sws.wk_int_count = sws.wk_int_count - Math.Abs(required_staff_level);
										staff_int_current = staff_int_current + Math.Abs(required_staff_level);
										required_staff_level = 0;
										//System.Diagnostics.Debug.WriteLine("Release Staff C " + sws.subtask_name + " wkc" + sws.wk_int_count.ToString() + "  StaffCurr" + staff_int_current.ToString());
									}
									else
									{
										//We can release enough staff by purely releasing ing ext staff
										staff_int_current = staff_int_current + Math.Abs(sws.wk_int_count);
										required_staff_level = required_staff_level - Math.Abs(sws.wk_int_count);
										sws.wk_int_count = 0;
										//System.Diagnostics.Debug.WriteLine("Release Staff D " + sws.subtask_name + " wkc" + sws.wk_int_count.ToString() + "  StaffCurr" + staff_int_current.ToString());
									}
								}
							}
						}
					}
					//Apply Staff to the jobs in hand
					foreach (sub_work_stage sws in swsWorkList)
					{
						//System.Diagnostics.Debug.WriteLine("##  Apply Before " + sws.subtask_name + " md" + sws.man_days.ToString());
						//System.Diagnostics.Debug.WriteLine("##  Apply workers int:" + CONVERT.ToStr(sws.wk_int_count) + "  ext:" + CONVERT.ToStr(sws.wk_ext_count));
						sws.man_days = sws.man_days - (sws.wk_int_count + sws.wk_ext_count);
						//System.Diagnostics.Debug.WriteLine("##  Apply After " + sws.subtask_name + " md" + sws.man_days.ToString());
						//System.Diagnostics.Debug.WriteLine("##  Idle Workers int:" + CONVERT.ToStr(staff_int_current) + "  ext:" + CONVERT.ToStr(staff_ext_current));

						if (worksheets.ContainsKey(day_counter + day_start))
						{
							DayTimeSheet dts = (DayTimeSheet) worksheets[day_counter + day_start];
							if (isDev)
							{
								dts.staff_int_dev_day_employed_count += sws.wk_int_count;
								dts.staff_int_dev_day_idle_count = staff_int_current;
								dts.staff_ext_dev_day_employed_count += sws.wk_ext_count;
								dts.staff_ext_dev_day_idle_count = staff_ext_current;
							}
							else
							{
								dts.staff_int_test_day_employed_count += sws.wk_int_count;
								dts.staff_int_test_day_idle_count = staff_int_current;
								dts.staff_ext_test_day_employed_count += sws.wk_ext_count;
								dts.staff_ext_test_day_idle_count = staff_ext_current;
							}
						}
					}
					//Release Staff if not needed
					foreach (sub_work_stage sws in swsWorkList)
					{
						required_staff_level = sws.man_days - (sws.wk_int_count + sws.wk_ext_count);
						//System.Diagnostics.Debug.WriteLine("Post work required_staff_level " + CONVERT.ToStr(required_staff_level));

						if (required_staff_level < 0) //we need to release people 
						{
							int release_staff_level = Math.Abs(required_staff_level);
							//We want less people (release people from the sws, ext staff first)
							if (sws.wk_ext_count > 0)
							{
								if (sws.wk_ext_count > release_staff_level)
								{
									//We can release enough staff by purely releasing ing ext staff
									sws.wk_ext_count = sws.wk_ext_count - Math.Abs(release_staff_level);
									staff_ext_current = staff_ext_current + Math.Abs(release_staff_level);
									release_staff_level = 0;
									//System.Diagnostics.Debug.WriteLine("Release Ext Staff A2 " + sws.subtask_name + " wkc" + sws.wk_ext_count.ToString() + "  StaffExtCurr" + staff_ext_current.ToString());
								}
								else
								{
									//We can release enough staff by purely releasing ing ext staff
									staff_ext_current = staff_ext_current + Math.Abs(sws.wk_ext_count);
									release_staff_level = release_staff_level - Math.Abs(sws.wk_ext_count);
									sws.wk_ext_count = 0;
									//System.Diagnostics.Debug.WriteLine("Release Ext Staff B2 " + sws.subtask_name + " wkc" + sws.wk_ext_count.ToString() + "  StaffExtCurr" + staff_ext_current.ToString());
								}
							}
							//now release any int staff
							if ((sws.wk_int_count > 0) && (release_staff_level > 0))
							{
								if (sws.wk_int_count > release_staff_level)
								{
									//We can release enough staff by purely releasing ing ext staff
									sws.wk_int_count = sws.wk_int_count - Math.Abs(release_staff_level);
									staff_int_current = staff_int_current + Math.Abs(release_staff_level);
									release_staff_level = 0;
									//System.Diagnostics.Debug.WriteLine("Release Staff C2 " + sws.subtask_name + " wkc" + sws.wk_int_count.ToString() + "  StaffCurr" + staff_int_current.ToString());
								}
								else
								{
									//We can release enough staff by purely releasing ing ext staff
									staff_int_current = staff_int_current + Math.Abs(sws.wk_int_count);
									release_staff_level = release_staff_level - Math.Abs(sws.wk_int_count);
									sws.wk_int_count = 0;
									//System.Diagnostics.Debug.WriteLine("Release Staff D2 " + sws.subtask_name + " wkc" + sws.wk_int_count.ToString() + "  StaffCurr" + staff_int_current.ToString());
								}
							}
						}
					}
					//Recalculate the Number of days left
					number_of_days_left = 0;
					foreach (sub_work_stage sws in swsWorkList)
					{
						number_of_days_left += sws.man_days;
						//System.Diagnostics.Debug.WriteLine("Checking " + sws.subtask_name + " md" + sws.man_days.ToString() + number_of_days_left.ToString());
					}
					//System.Diagnostics.Debug.WriteLine("Checking number_of_days_left  " + number_of_days_left.ToString());
					day_counter = day_counter + 1;	//count the Days Used
					//System.Diagnostics.Debug.WriteLine("end  " + day_counter.ToString());
				}
				number_of_predicted_days = day_counter;
			}
			else
			{
				number_of_predicted_days = number_of_days_left;
			}

			return number_of_predicted_days;
		}

		private static int GeneratePredictedDaysForSequentialTasksFromList(int staff_current, ArrayList swsWorkList)
		{
			int default_duration = 0;
			int number_of_days_left = 0;
			int number_of_predicted_days = 0;
			//=============================================================
			//determine total days left (it's the max possible duration)
			//=============================================================
			default_duration = 0;
			foreach (sub_work_stage sws in swsWorkList)
			{
				default_duration += sws.man_days;
				//System.Diagnostics.Debug.WriteLine("Scan " + sws.subtask_name + " md:" + sws.man_days.ToString() + " sum:" + default_duration.ToString());
			}
			//default_duration = number_of_days_left;
			number_of_days_left = default_duration;
			//System.Diagnostics.Debug.WriteLine("Scanning " + CONVERT.ToStr(default_duration));
			//=============================================================
			//Interate through the days, processing the worklist 
			//=============================================================
			if (staff_current != 1)
			{
				int day_counter = 0;
				int required_staff_level = 0;
				while ((number_of_days_left > 0) && (day_counter < default_duration))
				{
					swsWorkList.Sort(); //sort into ascending 
					swsWorkList.Reverse(); //reverse as we want work with biggest first

					foreach (sub_work_stage sws in swsWorkList)
					{
						//Hire more staff
						required_staff_level = sws.man_days - (sws.wk_count);
						if (required_staff_level > 0) //we want more people 
						{
							if (sws.sequential) //but we are sequential (limited to one person)
							{
								if (sws.wk_count == 0) //if we don't already have the person
								{
									required_staff_level = 1; //ask for him
								}
								else
								{
									required_staff_level = 0; //already filled the vacancy
								}
							}
							//check how many staff we have 
							if (staff_current >= required_staff_level)
							{
								//More than or equal to the demand
								sws.wk_count = sws.wk_count + required_staff_level;
								staff_current = staff_current - required_staff_level;
								//System.Diagnostics.Debug.WriteLine("More Demand " + sws.subtask_name + " wkc" + sws.wk_count.ToString() + "  StaffCurr" + staff_current.ToString());
							}
							else
							{
								//less than the demand
								sws.wk_count = sws.wk_count + staff_current;
								staff_current = 0;
								//System.Diagnostics.Debug.WriteLine("Less Demand " + sws.subtask_name + " wkc" + sws.wk_count.ToString() + "  StaffCurr" + staff_current.ToString());
							}
						}
						else
						{
							if (sws.wk_count > 0)
							{
								//release staff
								sws.wk_count = sws.wk_count - Math.Abs(required_staff_level);
								staff_current = staff_current + Math.Abs(required_staff_level);
								//System.Diagnostics.Debug.WriteLine("Release Staff A " + sws.subtask_name + " wkc" + sws.wk_count.ToString() + "  StaffCurr" + staff_current.ToString());
							}
						}
					}
					//Apply Staff to the jobs in hand
					foreach (sub_work_stage sws in swsWorkList)
					{
						sws.man_days = sws.man_days - sws.wk_count;
						//System.Diagnostics.Debug.WriteLine("Apply " + sws.subtask_name + " md" + sws.man_days.ToString());
					}
					//Release Staff if not needed
					foreach (sub_work_stage sws in swsWorkList)
					{
						required_staff_level = sws.man_days - (sws.wk_count);
						if (required_staff_level < 0) //we need to release people 
						{
							sws.wk_count = sws.wk_count - Math.Abs(required_staff_level);
							staff_current = staff_current + Math.Abs(required_staff_level);
							//System.Diagnostics.Debug.WriteLine("Release Staff B " + sws.subtask_name + " wkc" + sws.wk_count.ToString() + "  StaffCurr" + staff_current.ToString());
						}
					}
					//Recalculate the Number of days left
					number_of_days_left = 0;
					foreach (sub_work_stage sws in swsWorkList)
					{
						number_of_days_left += sws.man_days;
						//System.Diagnostics.Debug.WriteLine("Checking " + sws.subtask_name + " md" + sws.man_days.ToString() + number_of_days_left.ToString());
					}
					//System.Diagnostics.Debug.WriteLine("Checking number_of_days_left  " + number_of_days_left.ToString());
					day_counter = day_counter + 1;	//count the Days Used
					//System.Diagnostics.Debug.WriteLine("end  " + day_counter.ToString());
				}
				number_of_predicted_days = day_counter;
			}
			else
			{
				number_of_predicted_days = number_of_days_left;
			}
			//System.Diagnostics.Debug.WriteLine("GenPredictDaysForSeqTasksFromList	number_of_predicted_days" +CONVERT.ToStr(number_of_predicted_days));
			return number_of_predicted_days;
		}

		public void RecalculatePredictedDaysWithDefinedStaff(bool UseExtractFromNode, 
			int staff_int, int staff_ext, out int predicted_days)
		{
			int concurrent_task_count = 0;
			int seq_task_count = 0;
			int max_seq_task_mandays = 0;
			predicted_days = 0;
			ArrayList sws_list = new ArrayList();

			//Are we getting the data from the Network Node or the sub_work list
			if (UseExtractFromNode)
			{
				foreach (Node nd in network_stage_node.getChildren())
				{
					string task_type = nd.GetAttribute("type");
					switch (task_type)
					{
						case "work_task":
							sub_work_stage sws = new sub_work_stage();
							sws.loadFromNode(nd);
							if ((sws.dropped == false) & (sws.man_days > 0))
							{
								sws_list.Add(sws);
							}
							break;
					}
				}
			}
			else
			{
				//clone the existing list
				foreach (sub_work_stage existing_sws in this.sub_work_stages)
				{
					sub_work_stage sws = new sub_work_stage(existing_sws);
					if ((sws.dropped==false)&(sws.man_days>0))
					{
						sws_list.Add(sws);
					}
				}
			}
			getWorkProfileFromSWSList(sws_list, out concurrent_task_count, out seq_task_count, out max_seq_task_mandays);

			if (seq_task_count > 0)
			{
				int staff_current = staff_int + staff_ext;
				predicted_days = work_stage.GeneratePredictedDaysForSequentialTasksFromList(staff_current, sws_list);
				//GeneratePredictedDaysForSequentialTasks(sws_list, staff_int, staff_ext, out predicted_days);
			}
			else
			{ 
				//no Sequential task-- standard calculations 
				predicted_days = concurrent_task_count / 1;
				if ((staff_int + staff_ext) > 0)
				{
					predicted_days = concurrent_task_count / (staff_int + staff_ext);
					if ((concurrent_task_count % (staff_int + staff_ext)) > 0)
					{
						predicted_days += 1;
					}
				}
			}
		}

		public void RecalculatePredictedDays(bool UseExtractFromNode,  bool recordtoNode,  out int predicted_days)
		{
			string status = network_stage_node.GetAttribute("status");
			int totalTasks_left = network_stage_node.GetIntAttribute("tasks_left", 0);
			int staff_int = 0;
			int staff_ext = 0;
			predicted_days = totalTasks_left / 1;

			if (status.ToLower() == "todo")
			{
				staff_int = network_stage_node.GetIntAttribute("staff_int_requested", 0);
				staff_ext = network_stage_node.GetIntAttribute("staff_ext_requested", 0);
			}
			else
			{
				staff_int = network_stage_node.GetIntAttribute("staff_int_assigned", 0);
				staff_ext = network_stage_node.GetIntAttribute("staff_ext_assigned", 0);
			}

			RecalculatePredictedDaysWithDefinedStaff(UseExtractFromNode, staff_int, staff_ext, out predicted_days);

			if (recordtoNode)
			{
				network_stage_node.SetAttribute("predicted_days", CONVERT.ToStr(predicted_days));
			}
		}

		public void getRequestedAndAllocatedResourceLevels(out int requested, out int allocated, out int days)
		{
			int requested_int = network_stage_node.GetIntAttribute("staff_int_requested",0); 
			int assigned_int = network_stage_node.GetIntAttribute("staff_int_assigned",0); 
			int requested_ext = network_stage_node.GetIntAttribute("staff_ext_requested",0); 
			int assigned_ext = network_stage_node.GetIntAttribute("staff_ext_assigned",0); 
			int predicted_days  = network_stage_node.GetIntAttribute("predicted_days",0); 

			requested = requested_int + requested_ext;
			allocated = assigned_int + assigned_ext;
			days = predicted_days;
		}

		public void getDisplayRequestedAndAllocatedResourceLevels(out int requested, out int allocated, out int days)
		{
			requested = network_stage_node.GetIntAttribute("staff_requested_displayed",0); 
			allocated = network_stage_node.GetIntAttribute("staff_allocated_displayed",0); 
			days = network_stage_node.GetIntAttribute("predicted_days_displayed",0); 
		}

		public void UpdateDisplayResourceNumbers()
		{
			int requested_int = network_stage_node.GetIntAttribute("staff_int_requested", 0);
			int assigned_int = network_stage_node.GetIntAttribute("staff_int_assigned", 0);
			int requested_ext = network_stage_node.GetIntAttribute("staff_ext_requested", 0);
			int assigned_ext = network_stage_node.GetIntAttribute("staff_ext_assigned", 0);
			int predicted_days = network_stage_node.GetIntAttribute("predicted_days", 0);

			int requested = requested_int + requested_ext;
			int allocated = assigned_int + assigned_ext;
			int days = predicted_days;

			network_stage_node.SetAttribute("staff_requested_displayed", CONVERT.ToStr(requested));
			network_stage_node.SetAttribute("staff_allocated_displayed", CONVERT.ToStr(allocated));
			network_stage_node.SetAttribute("predicted_days_displayed", CONVERT.ToStr(days));
		}


		/// <summary>
		/// check the the DoingNothing node and Tasks nodes to determine 
		/// how many people we have allocated (seperate int and contractors counts)
		/// </summary>
		/// <param name="count_int_staff"></param>
		/// <param name="count_ext_staff"></param>
		public void getStaffCountForAllocatedStaff(out int count_int_staff, out int count_ext_staff)
		{
			count_int_staff = 0;
			count_ext_staff = 0;

			foreach (Node nd in network_stage_node.getChildren())
			{
				string task_type = nd.GetAttribute("type");
				switch (task_type)
				{
					case "doing_nothing":
						foreach (Node emp in nd.getChildren())
						{
							bool isExternal = emp.GetBooleanAttribute("is_contractor", false);
							if (isExternal == false)
							{
								count_int_staff++;
							}
							else
							{
								count_ext_staff++;
							}
						}
						break;
					case "work_task":
						//we need how many people have been assigned to this task
						if (nd.getChildren().Count > 0)
						{
							foreach (Node person in nd.getChildren())
							{
								bool isExternal = person.GetBooleanAttribute("is_contractor", false);
								if (isExternal == false)
								{
									count_int_staff++;
								}
								else
								{
									count_ext_staff++;
								}								
							}
						}
						break;
				}
			}
		}

		public void getDescopeCritPathStatus(string subtaskname, 
			out bool exists, out bool droppable, out bool dropped)
		{
			exists = false;
			droppable = false;
			dropped = false;

			foreach (Node nd in network_stage_node.getChildren())
			{
				string task_type = nd.GetAttribute("type");
				switch (task_type)
				{
					case "work_task":
						string subnode_subtaskname = nd.GetAttribute("subtask_name");
						if (subnode_subtaskname.ToLower() == subtaskname.ToLower())
						{
							exists = true;
							droppable = nd.GetBooleanAttribute("droppable",false);
							dropped = nd.GetBooleanAttribute("dropped",false);

							string debug = "CPStatus ";
							debug += "  subtaskname [" + subtaskname + "]";
							debug += "  droppable ["+droppable.ToString()+"]";
							debug += "  dropped ["+dropped.ToString()+"]";
							//System.Diagnostics.Debug.WriteLine(debug);
						}
						break;
				}
			}
		}

		public bool DoWeConsistOnlyOfSequentialSubtasks (out int numberOfSubtasks)
		{
			bool hasNonSequentialSubTasks = false;
			numberOfSubtasks = 0;

			foreach (sub_work_stage subTask in sub_work_stages)
			{
				numberOfSubtasks++;

				if (! subTask.sequential)
				{
					hasNonSequentialSubTasks = true;
				}
			}

			return ((numberOfSubtasks > 0) && ! hasNonSequentialSubTasks);
		}

		public void ChangeScopeStatusByNamedCritPathSection(string subtaskname, bool drop_task,
			out int scope_hit, out bool recalc_required)
		{
			recalc_required = false;
			scope_hit = 0;

			foreach (Node nd in network_stage_node.getChildren())
			{
				string task_type = nd.GetAttribute("type");
				switch (task_type)
				{
					case "work_task":
						string subnode_subtaskname = nd.GetAttribute("subtask_name");
						int work_total = nd.GetIntAttribute("work_total", 0);

						if (subnode_subtaskname.ToLower() == subtaskname.ToLower())
						{
							if (drop_task)
							{
								nd.SetAttribute("dropped", "true");
								nd.SetAttribute("man_days", 0);
								scope_hit = -1 * nd.GetIntAttribute("scope_hit", 0);
								recalc_required = true;
								nd.SetAttribute("work_left", 0);
								nd.SetAttribute("work_dropped", CONVERT.ToStr(work_total));
							}
							else
							{
								nd.SetAttribute("dropped", "false");
								nd.SetAttribute("man_days", CONVERT.ToStr(work_total));
								scope_hit = nd.GetIntAttribute("scope_hit", 0);
								recalc_required = true;
								nd.SetAttribute("work_left", CONVERT.ToStr(work_total));
								nd.SetAttribute("work_dropped", CONVERT.ToStr(0));
							}
						}
						break;
				}
			}
		}

		public bool DescopeByOneManDay()
		{
			int currentTaskTotalCount = 0;
			int currentTaskDescopedCount = 0;
			int currentTaskActiveCount = 0;
			bool dropped = false;

			getFullTaskCountsForStage(out currentTaskTotalCount, out currentTaskDescopedCount, out currentTaskActiveCount);
			if (currentTaskActiveCount > 0)
			{
				foreach (Node nd in network_stage_node.getChildren())
				{
					if (dropped == false)
					{
						string task_type = nd.GetAttribute("type");
						switch (task_type)
						{
							case "work_task":
								int work_total = nd.GetIntAttribute("work_total", 0);
								int work_left = nd.GetIntAttribute("work_left", 0);
								int work_dropped = nd.GetIntAttribute("work_dropped", 0);

								if (work_left > 1)
								{
									nd.SetAttribute("work_left", CONVERT.ToStr(work_left - 1));
									nd.SetAttribute("work_dropped", CONVERT.ToStr(work_dropped + 1));
									dropped = true;
								}
								break;
						}
					}
				}
			}
			return dropped;
		}

		public void DebugTaskNumbers()
		{
			int stage_tasks_total = network_stage_node.GetIntAttribute("tasks_total",0);
			int stage_tasks_left = network_stage_node.GetIntAttribute("tasks_left",0);
			int stage_tasks_dropped = network_stage_node.GetIntAttribute("tasks_dropped",0);
			string code_str = network_stage_node.GetAttribute("code");
			//System.Diagnostics.Debug.WriteLine("##DTN  " + code_str + " TT:" + CONVERT.ToStr(stage_tasks_total) + " TL:" + CONVERT.ToStr(stage_tasks_left) + " TD:" + CONVERT.ToStr(stage_tasks_dropped));
		}
		
		public void DebugPredictedDays()
		{
			int prd_days = getPredictedDays();
			string code_str = network_stage_node.GetAttribute("code");
			//System.Diagnostics.Debug.WriteLine("##DPD  " + code_str + " predict:" + CONVERT.ToStr(prd_days));
		}

		/// <summary>
		/// Do we need to make staff changes
		///   Do we need extra staff (requested more than assigned) 
		///   Do we need less staff (requested less than assigned) 
		/// </summary>
		/// <param name="number_of_int_staff">The number of Additional Staff needed</param>
		/// <param name="number_of_ext_staff">The number of Additional Staff needed</param>
		/// <param name="number_of_int_staff_requested">What the Players wanted</param>
		/// <param name="number_of_ext_staff_requested">What the Players wanted</param>
		/// <returns></returns>
		public bool getCountofStaffChangeNeeded(out int number_of_int_staff, out int number_of_ext_staff,
			out int number_of_int_staff_requested, out int number_of_ext_staff_requested)
		{
			bool needStaffChange = false;
			number_of_int_staff = 0;
			number_of_ext_staff = 0;
			number_of_int_staff_requested = 0;
			number_of_ext_staff_requested = 0;

			int staff_int_requested = network_stage_node.GetIntAttribute("staff_int_requested", 0);
			int staff_int_assigned = network_stage_node.GetIntAttribute("staff_int_assigned", 0);
			int staff_ext_requested = network_stage_node.GetIntAttribute("staff_ext_requested", 0);
			int staff_ext_assigned = network_stage_node.GetIntAttribute("staff_ext_assigned", 0);

			number_of_int_staff_requested = staff_int_requested;
			number_of_ext_staff_requested = staff_ext_requested;

			if (staff_int_requested > staff_int_assigned)
			{
				number_of_int_staff = staff_int_requested - staff_int_assigned;
				needStaffChange = true;
			}
			else
			{
				if (staff_int_requested < staff_int_assigned)
				{
					number_of_int_staff = staff_int_requested - staff_int_assigned;
					needStaffChange = true;
				}
			}

			if (staff_ext_requested > staff_ext_assigned)
			{
				number_of_ext_staff = staff_ext_requested - staff_ext_assigned;
				needStaffChange = true;
			}
			else
			{
				if (staff_ext_requested < staff_ext_assigned)
				{
					number_of_ext_staff = staff_ext_requested - staff_ext_assigned;
					needStaffChange = true;
				}
			}
			return needStaffChange;
		}

		private void adjustAssignedStaffCount(bool isExternal, int count)
		{
			if (isExternal)
			{
				int currentcount = network_stage_node.GetIntAttribute("staff_ext_assigned", 0);
				currentcount = currentcount + count;
				network_stage_node.SetAttribute("staff_ext_assigned", CONVERT.ToStr(currentcount));
				//System.Diagnostics.Debug.WriteLine("Added 1 to stage assigned count"+ this.project_id_str+ " EXT "+currentcount.ToString());
			}
			else
			{
				int currentcount = network_stage_node.GetIntAttribute("staff_int_assigned", 0);
				currentcount = currentcount + count;
				network_stage_node.SetAttribute("staff_int_assigned", CONVERT.ToStr(currentcount));
				//System.Diagnostics.Debug.WriteLine("Added 1 to stage assigned count"+ this.project_id_str+ " INT "+currentcount.ToString());
			}
		}

		public bool AttachStaffMember(Node staffmember_node, out bool RequireUpdateGoLive)
		{
			RequireUpdateGoLive = false;

			string stage_code = network_stage_node.GetAttribute("code");
			string staff_name = staffmember_node.GetAttribute("name");
			////add the new staff member into the doing nothing area 
			////they will be reassign to an pending job later (if any exist)
			Node tmpDoNowt_Node = this.getDoingNothingNode();
			Node staff_parent = staffmember_node.Parent;
			///move the staff member
			tmpDoNowt_Node.AddChild(staffmember_node);
			network_stage_node.Tree.FireMovedNode(staff_parent, staffmember_node);

			bool staff_member_external = staffmember_node.GetBooleanAttribute("is_contractor", false);

			adjustAssignedStaffCount(staff_member_external, 1);

			int predicted_days = 0;
			RecalculatePredictedDays(true, true, out predicted_days);
			RequireUpdateGoLive = true;
			//System.Diagnostics.Debug.WriteLine("MOVE STAFF ["+staff_name+"] moved to "+stage_code);
			return true;
		}

		/// <summary>
		/// This assigns any idle worker assigned to to stage node to an incomplete task(if exists)
		/// We do assign any internal staff first before any external staff are assigned
		/// (if we didn't then the cost of wasted time report for this project might be different for different games)
		/// (even when the project staff requirements were the same)
		/// (due to other projects affecting when internal and external staff became available to us)
		/// (we want consistant results for this project despite the activities of other projects)
		/// </summary>
		/// <param name="day"></param>
		/// <param name="RequireUpdateGoLive"></param>
		public void AssignWorkersToWorkIfAvailable(int day, out bool RequireUpdateGoLive)
		{
			ArrayList int_workers_doingnothing = new ArrayList();
			ArrayList ext_workers_doingnothing = new ArrayList();
			ArrayList tasksWithWorkLeft = new ArrayList();

			RequireUpdateGoLive = false;

		  //Need to scan through and get 
		  // 1, all workers who are "DoingNothing"
		  // 2, all tasks which are todo and have no worker attached
			foreach (Node nd in network_stage_node.getChildren())
			{
				string task_type = nd.GetAttribute("type");
				switch (task_type)
				{
					case "doing_nothing":
						foreach (Node emp in nd.getChildren())
						{
		          bool isExternal = emp.GetBooleanAttribute("is_contractor", false);
		          if (isExternal==false)	
		          {
		            int_workers_doingnothing.Add(emp);
		          }
		          else
		          {
								ext_workers_doingnothing.Add(emp);
		          }
						}
						break;
					case "work_task":
						string tk_status = nd.GetAttribute("status");
						int tk_mandays_left = nd.GetIntAttribute("work_left",0);
						//a task must be todo and have no people 
						if ((tk_status.ToLower() == "todo") |(tk_status.ToLower() == "inprogress"))
						{
							if (nd.getChildren().Count != tk_mandays_left)
							{
								tasksWithWorkLeft.Add(nd);
							}
						}
						break;
				}
			}

			bool haveIdleWorkers = (int_workers_doingnothing.Count >0) | (ext_workers_doingnothing.Count >0);
			bool haveWorkUndone = tasksWithWorkLeft.Count > 0;

			//to make any changes, we need both tasks to do and worker to do them 
			if ((haveIdleWorkers) & (haveWorkUndone))
			{
				foreach (Node wt in tasksWithWorkLeft)
				{ 
					//handle the internal workers
					if (int_workers_doingnothing.Count>0)
					{
						bool sequential = wt.GetBooleanAttribute("sequential",false);

						int tk_mandays_left = wt.GetIntAttribute("work_left",0);
						int tk_staff_assigned = wt.getChildren().Count;
						int staff_shortfall = tk_mandays_left - tk_staff_assigned;

						if ((sequential))
						{
							staff_shortfall =0;
							if (tk_staff_assigned == 0)
							{
								if (tk_mandays_left > 0)
								{
									staff_shortfall = 1;
								}
							}
						}

						if (staff_shortfall > 0)
						{
							//pull the employees out of doing nothing and move them to the task
							for (int step=0; step<staff_shortfall; step++)
							{
								if (int_workers_doingnothing.Count>0)
								{
									Node worker = (Node) int_workers_doingnothing[0];
									NodeTree tree = worker.Tree;
									Node next_worker_parent = worker.Parent;
									//==perform the move 
									string staff_name = worker.GetAttribute("name");
									wt.AddChild(worker);
									tree.FireMovedNode(next_worker_parent, worker);
									//System.Diagnostics.Debug.WriteLine("ordered STAFF ["+staff_name+"] moved to task");
									//==remove the worker from the 
									int_workers_doingnothing.Remove(worker);
								}
							}
						}
					}
					//handle the external workers
					if (ext_workers_doingnothing.Count>0)
					{
						bool sequential = wt.GetBooleanAttribute("sequential", false);

						int tk_mandays_left = wt.GetIntAttribute("work_left",0);
						int tk_staff_assigned = wt.getChildren().Count;
						int staff_shortfall = tk_mandays_left - tk_staff_assigned;

						if ((sequential))
						{
							staff_shortfall = 0;
							if (tk_staff_assigned == 0)
							{
								if (tk_mandays_left > 0)
								{
									staff_shortfall = 1;
								}
							}
						}
						if (staff_shortfall > 0)
						{
							//pull the employees out of doing nothing and move them to the task
							for (int step=0; step<staff_shortfall; step++)
							{
								if (ext_workers_doingnothing.Count > 0)
								{
									Node worker = (Node)ext_workers_doingnothing[0];
									NodeTree tree = worker.Tree;
									Node next_worker_parent = worker.Parent;
									//==perform the move 
									string staff_name = worker.GetAttribute("name");
									wt.AddChild(worker);
									tree.FireMovedNode(next_worker_parent, worker);
									//System.Diagnostics.Debug.WriteLine("ordered STAFF ["+staff_name+"] moved to task");
									//==remove the worker from the 
									ext_workers_doingnothing.Remove(worker);
								}
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// In ReassignWorkers, we iterate over the tasks and see if the we can move people back to DoNothing
		/// --We check how many tasks we have left, how many people are in DoingWork
		/// --If the mandays left are less than the people Assigned 
		/// --then we move the over supply of people back to doing nothing 
		/// The forced reassign moves everybody back to DoingNothing
		/// --This happends for PreCancel and PrePause and No Money
		/// </summary>
		/// <param name="day"></param>
		/// <param name="NoMoney"></param>
		/// <param name="StatusRequestPreCancel"></param>
		/// <param name="StatusRequestPrePause"></param>
		public void ReAssignWorkers(int day, bool NoMoney, bool StatusRequestPreCancel, bool StatusRequestPrePause)
		{
			bool forced_reassign = ((StatusRequestPreCancel) | (StatusRequestPrePause));
			forced_reassign = ((forced_reassign) | (NoMoney));

			Node tmpDoNowt_Node = getDoingNothingNode();
			ArrayList tasksWithOverSupplyofWorkers = new ArrayList();
			ArrayList moveList = new ArrayList();

			//Get all tasks with an Over supply of workers (everthing with a forced_reassign)
			foreach (Node nd in network_stage_node.getChildren())
			{
				string task_type = nd.GetAttribute("type");
				switch (task_type)
				{
					case "work_task":
						string tk_status = nd.GetAttribute("status");
						int tk_mandays_left = nd.GetIntAttribute("work_left", 0);
						bool no_work_left = (tk_mandays_left == 0);
						//a task must be todo and have no people 
						if ((forced_reassign)|(no_work_left))
						{
							tasksWithOverSupplyofWorkers.Add(nd);
						}
						else
						{
							if (nd.getChildren().Count > tk_mandays_left)
							{
								tasksWithOverSupplyofWorkers.Add(nd);
							}
						}
						break;
				}
			}

			//Now go through each of the tasks and reassign excess back to Doing Nothing node
			foreach (Node wt in tasksWithOverSupplyofWorkers)
			{
				if (forced_reassign)
				{
					//we need to move all people due to a forced reassign
					foreach (Node emp in wt.getChildren())
					{
						moveList.Add(emp);
					}
				}
				else
				{
					//we only need to move the over supply
					int tk_mandays_left = wt.GetIntAttribute("work_left",0);
					int tk_staff_assigned = wt.getChildren().Count;
					int staff_oversupply = tk_staff_assigned - tk_mandays_left;
					if (staff_oversupply > 0)
					{
						ArrayList assigned_staff = wt.getChildren();
						for (int step = 0; step < staff_oversupply; step++)
						{
							moveList.Add(assigned_staff[step]);
						}
					}
				}
			}
			//Now move the people into the DoingNothing node
			foreach (Node person in moveList)
			{
				string staff_name = person.GetAttribute("name");
				Node task = person.Parent;
				NodeTree tree = person.Tree;
				tmpDoNowt_Node.AddChild(person);
				tree.FireMovedNode(task, person);
				//System.Diagnostics.Debug.WriteLine("ordered STAFF ["+staff_name+"] moved to [Do Nothing]");
			}
		}

		/// <summary>
		/// This is used to direct remove a worker from the current stage 
		/// It will try to remove a single worker (firstly from the do Nothin and then from the work_stages
		/// </summary>
		/// <param name="day"></param>
		/// <param name="isInternal">Are we to remove a internal worker</param>
		public void DirectFireWorker(int day, bool isInternal)
		{ 
			//
			Node tmpDoNowt_Node = null;
			Node worker = null;
			int workercount_int_returned = 0;
			int workercount_ext_returned = 0;

			//find the worker to free (pref from the people do nothing)
			tmpDoNowt_Node = getDoingNothingNode();
			if (tmpDoNowt_Node.getChildren().Count>0)
			{
				foreach (Node employee in tmpDoNowt_Node.getChildren())
				{
					bool isExternal = employee.GetBooleanAttribute("is_contractor", false);

					if (worker == null)
					{
						if ((isInternal) & (isExternal == false))
						{
							worker = employee;
						}
						if ((isInternal==false) & (isExternal == true))
						{
							worker = employee;
						}
					}
				}
			}
			//If we haven't found one in the doNothing, then check the people attached to the work_stages
			if (worker ==null)
			{
				//Scan through the work stages 
				foreach (Node nd in network_stage_node.getChildren())
				{
					string task_type = nd.GetAttribute("type");
					switch (task_type)
					{
						case "work_task":
							//need to scan through any employees attached 
							if (nd.getChildren().Count > 0)
							{
								foreach (Node employee in nd.getChildren())
								{
									bool isExternal = employee.GetBooleanAttribute("is_contractor", false);
									if (worker == null)
									{
										if ((isInternal) & (isExternal == false))
										{
											worker = employee;
										}
										if ((isInternal == false) & (isExternal == true))
										{
											worker = employee;
										}
									}
								}
							}
							break;
					}
				}
			}
			//If we have a worker which can remove
			if (worker != null)
			{
				//Return the worker to the department
				NodeTree tree = worker.Tree;
				Node parentnode = worker.Parent;
				bool isWorkerInternal = (worker.GetBooleanAttribute("is_contractor", false) == false);
				string destnodename = worker.GetAttribute("section");
				Node sectionNode = tree.GetNamedNode(destnodename);

				if (sectionNode != null)
				{
					string staff_name = worker.GetAttribute("name");
					sectionNode.AddChild(worker);
					tree.FireMovedNode(parentnode, worker);
					//System.Diagnostics.Debug.WriteLine("returing worker to section ["+staff_name+"] to ["+destnodename+"]");
					if (isWorkerInternal)
					{
						workercount_int_returned++;
					}
					else
					{
						workercount_ext_returned++;
					}
				}

				int assigned_int = network_stage_node.GetIntAttribute("staff_int_assigned", 0);
				int assigned_ext = network_stage_node.GetIntAttribute("staff_ext_assigned", 0);

				//System.Diagnostics.Debug.WriteLine("before Assigned Int :"+assigned_int.ToString()+" Ext:"+assigned_ext.ToString());
				assigned_int = assigned_int - workercount_int_returned;
				assigned_ext = assigned_ext - workercount_ext_returned;

				network_stage_node.SetAttribute("staff_int_assigned", CONVERT.ToStr(assigned_int));
				network_stage_node.SetAttribute("staff_ext_assigned", CONVERT.ToStr(assigned_ext));

				int predicted_days = 0;
				this.RecalculatePredictedDays(true, true, out predicted_days);
			}
		}

		public void FireWorkers(int day, bool NoMoney, bool StatusRequestPreCancel, bool StatusRequestPrePause)
		{
			Node tmpDoNowt_Node = getDoingNothingNode();
			bool forced_reassign = ((StatusRequestPreCancel) | (StatusRequestPrePause));
			forced_reassign = ((forced_reassign) | (NoMoney));

			NodeTree tree = network_stage_node.Tree;

			int workercount_int_exists = 0;
			int workercount_ext_exists = 0;
			int workercount_int_returned = 0;
			int workercount_ext_returned = 0;

			int tasks_left = network_stage_node.GetIntAttribute("tasks_left", 0);
			if ((tasks_left == 0) | (forced_reassign))
			{
				ArrayList workerKillList = new ArrayList();

				//get all workers into the kill list (They should all be doing nothing at this point)
				foreach (Node nd in network_stage_node.getChildren())
				{
					string task_type = nd.GetAttribute("type");
					switch (task_type)
					{
						case "doing_nothing":
							foreach (Node emp in nd.getChildren())
							{
								workerKillList.Add(emp);
								if (emp.GetBooleanAttribute("is_contractor", false) == false)
								{
									workercount_int_exists++;
								}
								else
								{
									workercount_ext_exists++;
								}
							}
							break;
					}
				}

				//Now kick them back to thier sections 
				foreach (Node worker in workerKillList)
				{
					bool isInternal = (worker.GetBooleanAttribute("is_contractor", false) == false);
					string destnodename = worker.GetAttribute("section");
					Node sectionNode = tree.GetNamedNode(destnodename);

					if (sectionNode != null)
					{
						string staff_name = worker.GetAttribute("name");
						Node parentnode = worker.Parent;
						sectionNode.AddChild(worker);
						tree.FireMovedNode(parentnode, worker);
						//System.Diagnostics.Debug.WriteLine("returing worker to section ["+staff_name+"] to ["+destnodename+"]");
						if (isInternal)
						{
							workercount_int_returned++;
						}
						else
						{
							workercount_ext_returned++;
						}
					}
				}
				workerKillList.Clear();
				//Update 
				int assigned_int = network_stage_node.GetIntAttribute("staff_int_assigned", 0);
				int assigned_ext = network_stage_node.GetIntAttribute("staff_ext_assigned", 0);

				//System.Diagnostics.Debug.WriteLine("before Assigned Int :"+assigned_int.ToString()+" Ext:"+assigned_ext.ToString());
				assigned_int = assigned_int - workercount_int_returned;
				assigned_ext = assigned_ext - workercount_ext_returned;

				network_stage_node.SetAttribute("staff_int_assigned", CONVERT.ToStr(assigned_int));
				network_stage_node.SetAttribute("staff_ext_assigned", CONVERT.ToStr(assigned_ext));
				//System.Diagnostics.Debug.WriteLine("after Assigned Int :"+assigned_int.ToString()+" Ext:"+assigned_ext.ToString());
			}
		}

		public void RecalculateStageTaskNumbers()
		{
			int stage_tasks_total = network_stage_node.GetIntAttribute("tasks_total",0);
			int stage_tasks_left = network_stage_node.GetIntAttribute("tasks_left",0);
			int stage_tasks_dropped = network_stage_node.GetIntAttribute("tasks_dropped",0);

			int subtask_mandays_total = 0;
			int subtask_mandays_left = 0;
			int subtask_mandays_dropped = 0;

			stage_tasks_total = 0;
			stage_tasks_left = 0;
			stage_tasks_dropped = 0;
			//System.Diagnostics.Debug.WriteLine("WS RECALC");

			foreach (Node nd in network_stage_node.getChildren())
			{
				string task_type = nd.GetAttribute("type");
				switch (task_type)
				{
					case "work_task":
						subtask_mandays_total = nd.GetIntAttribute("work_total",0);
						subtask_mandays_left = nd.GetIntAttribute("work_left", 0);
						subtask_mandays_dropped = nd.GetIntAttribute("work_dropped", 0);

						stage_tasks_total = stage_tasks_total + subtask_mandays_total;
						stage_tasks_left = stage_tasks_left + subtask_mandays_left;
						stage_tasks_dropped = stage_tasks_dropped + subtask_mandays_dropped;

						//System.Diagnostics.Debug.WriteLine("WS SUB STT:" + CONVERT.ToStr(subtask_mandays_total)
						//	+ " TKS" + CONVERT.ToStr(subtask_mandays_left) + "  TDP" + CONVERT.ToStr(subtask_mandays_dropped));
						break;
				}
			}
			//System.Diagnostics.Debug.WriteLine("WS  STT:" + CONVERT.ToStr(stage_tasks_total)
			//	+ " TKS" + CONVERT.ToStr(stage_tasks_left) + "  TDP" + CONVERT.ToStr(stage_tasks_dropped));

			network_stage_node.SetAttribute("tasks_total", CONVERT.ToStr(stage_tasks_total));
			network_stage_node.SetAttribute("tasks_left", CONVERT.ToStr(stage_tasks_left));
			network_stage_node.SetAttribute("tasks_dropped", CONVERT.ToStr(stage_tasks_dropped)); 
		}

		public void RecordWorkAchieved(out bool isDev,
			out int overall_int_tasked, out int overall_int_worked, out int overall_int_wasted,
			out int overall_ext_tasked, out int overall_ext_worked, out int overall_ext_wasted)
		{

			overall_int_tasked = 0; //How many internal people tasked 
			overall_int_worked = 0; //How many internal man day worked 
			overall_int_wasted = 0; //How many internal man day wasted
			overall_ext_tasked = 0; //How many external people tasked 
			overall_ext_worked = 0; //How many external man day worked 
			overall_ext_wasted = 0; //How many external man day wasted

			int number_of_tasks_completed = 0;
			int NothingHrs = 0;
			isDev = this.isDev();

			int this_task_int_worked=0;
			int this_task_ext_worked=0;

			foreach (Node nd in network_stage_node.getChildren())
			{
				string task_type = nd.GetAttribute("type");
				switch (task_type)
				{
					case "doing_nothing":
						int count_people_doing_nothing = nd.getChildren().Count;
						foreach (Node person in nd.getChildren())
						{
							string staff_name1 = person.GetAttribute("name");
							bool isConsultant = person.GetBooleanAttribute("is_contractor", false);
							//Take Record of Wasted Hours 
							NothingHrs = NothingHrs + 8;
							//Recording the activity (and the cost if applicable)
							if (isConsultant == false)
							{
								overall_int_tasked++;
								overall_int_wasted++;
								//System.Diagnostics.Debug.WriteLine(" " + staff_name1 + " int "+"DoingNowt");
							}
							else
							{
								overall_ext_tasked++;
								overall_ext_wasted++;
								//System.Diagnostics.Debug.WriteLine(" " + staff_name1 + " ext " + "DoingNowt");
							}
						}
						break;
					case "work_task":
						this_task_int_worked = 0;
						this_task_ext_worked = 0;
						//a task must have some people assigned to have any activity that needs recorded
						if (nd.getChildren().Count > 0)
						{
							string tk_status = nd.GetAttribute("status");
							int tk_tasktotal = nd.GetIntAttribute("work_total", 0);
							int tk_taskleft = nd.GetIntAttribute("work_left", 0);

							foreach (Node person in nd.getChildren())
							{
								string staff_name2 = person.GetAttribute("name");
								bool isConsultant = person.GetBooleanAttribute("is_contractor", false);

								//Recording the activity
								if (isConsultant == false)
								{
									overall_int_tasked++;
									overall_int_worked++;
									this_task_int_worked++;
									//System.Diagnostics.Debug.WriteLine(" " + staff_name2 + " int " + "DoingWork");
								}
								else
								{
									overall_ext_tasked++;
									overall_ext_worked++;
									this_task_ext_worked++;
									//System.Diagnostics.Debug.WriteLine(" " + staff_name2 + " ext " + "DoingWork");
								}							
							}
							tk_taskleft = tk_taskleft - (this_task_int_worked + this_task_ext_worked);
							nd.SetAttribute("work_left", CONVERT.ToStr(tk_taskleft));
							if (tk_taskleft > 0)
							{
								nd.SetAttribute("status", "inprogress");
							}
							else
							{
								nd.SetAttribute("status", "done");
							}
							number_of_tasks_completed = number_of_tasks_completed + (this_task_int_worked + this_task_ext_worked);
						}
						break;
				}
			}
			//rebuild the stage task numbers 
			RecalculateStageTaskNumbers();

			//Update the number of completed days 
			if (network_stage_node != null)
			{
				int completed_days_count = network_stage_node.GetIntAttribute("completed_days",0);
				completed_days_count += 1;
				network_stage_node.SetAttribute("completed_days",CONVERT.ToStr(completed_days_count));
			}
		}

	}
}
