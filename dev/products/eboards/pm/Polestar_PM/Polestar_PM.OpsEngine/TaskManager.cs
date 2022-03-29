using System;
using System.Collections;
using System.IO;
using System.Xml;

using Network;

using LibCore;
using CoreUtils;
using Polestar_PM.DataLookup;

using IncidentManagement;

namespace Polestar_PM.OpsEngine
{
	/// <summary>
	/// The task manager deals with all the Orders from the Facilitator
	///  Creating projects, Altering Resource levels, FSC etc 
	/// </summary>
	public class TaskManager : IDisposable
	{

		//The standard time delays for various activities
		protected NodeTree MyNodeTree = null;
		protected Node queueNode = null;
		protected Node currentTimeNode = null;
		protected Node projectsRunningNode = null;
		protected Node projectsKnownNode = null;
		protected Node RootNode = null;
		protected Node PMO_BudgetNode = null;
		protected Node ops_worklist_node = null;
		protected Node CurrDayNode = null;

		protected CurrentProjectLookup MyCurrProjLookup;

		public event EventHandler TaskProcessed;
		protected PM_OpsEngine opsEngine;

		public TaskManager(PM_OpsEngine opsEngine, NodeTree tree)
		{
			//Build the nodes 
			MyNodeTree = tree;
			RootNode = MyNodeTree.GetNamedNode("root");

			this.opsEngine = opsEngine;

			ops_worklist_node = MyNodeTree.GetNamedNode("ops_worklist");

			CurrDayNode = MyNodeTree.GetNamedNode("CurrentDay");

			MyCurrProjLookup = new CurrentProjectLookup(MyNodeTree);

			projectsRunningNode = MyNodeTree.GetNamedNode("pm_projects_running");
			projectsKnownNode = MyNodeTree.GetNamedNode("pm_projects_known");
			PMO_BudgetNode = MyNodeTree.GetNamedNode("pmo_budget"); 

			queueNode = MyNodeTree.GetNamedNode("TaskManager");
			queueNode.ChildAdded += new Network.Node.NodeChildAddedEventHandler(queueNode_ChildAdded);

			foreach (Node task in queueNode.getChildrenClone())
			{
				HandleTaskRequest(task);
				queueNode.DeleteChildTree(task);
			}

			currentTimeNode = MyNodeTree.GetNamedNode("CurrentTime");
		}

		public void Dispose ()
		{
			if (PMO_BudgetNode != null)
			{
				PMO_BudgetNode = null;
			}
			if (projectsRunningNode != null)
			{
				projectsRunningNode = null;
			}
			if (projectsKnownNode != null)
			{
				projectsKnownNode = null;
			}
			if (RootNode != null)
			{
				RootNode = null;
			}
			if (CurrDayNode != null)
			{
				CurrDayNode = null;
			}

			if (MyCurrProjLookup != null)
			{
				MyCurrProjLookup.Dispose();
				MyCurrProjLookup = null;
			}

			//Disconnect the nodes
			if (queueNode != null)
			{
				queueNode.ChildAdded -= new Network.Node.NodeChildAddedEventHandler(queueNode_ChildAdded);
				queueNode = null;
			}
			if (currentTimeNode != null)
			{
				currentTimeNode = null;
			}
		}

		private int GetNextProjectUID()
		{
			//We need a unique number for each created project 
			//as slot is no longer constant, we need a unique constant
			//this is used to retrive data from the logs for the various reports 
			//we now use sequence based around a node in the network
			Node project_sequence_node = this.MyNodeTree.GetNamedNode("pm_project_unique_no");
			int next_id = project_sequence_node.GetIntAttribute("value",0);
			project_sequence_node.SetAttribute("value",CONVERT.ToStr(next_id+1));
			return next_id;
		}

		private void setExpertsInformation(string project_uid, string expertname_design,
			string expertname_build, string expertname_test)
		{
			bool goodDataProvided = false;
			bool proceed = false;
			Node expertsNode = null;

			goodDataProvided = ((expertname_design.Length > 0) & (expertname_build.Length > 0) & (expertname_test.Length > 0));

			if (goodDataProvided)
			{
				expertsNode = this.MyNodeTree.GetNamedNode("experts");
				if (expertsNode != null)
				{
					bool experts_system_enabled = expertsNode.GetBooleanAttribute("enabled", false);
					if (experts_system_enabled)
					{
						proceed = true;
					}
				}
			}
			if (proceed)
			{
				foreach (Node expert_node in expertsNode.getChildren())
				{
					string expert_name = expert_node.GetAttribute("expert_name");
					string skill_type = expert_node.GetAttribute("skill_type");
					string assigned_project = expert_node.GetAttribute("assigned_project");

					if (expertname_design.ToLower() == expert_name.ToLower())
					{
						expert_node.SetAttribute("assigned_project", project_uid);
					}
					if (expertname_build.ToLower() == expert_name.ToLower())
					{
						expert_node.SetAttribute("assigned_project", project_uid);
					}
					if (expertname_test.ToLower() == expert_name.ToLower())
					{
						expert_node.SetAttribute("assigned_project", project_uid);
					}
				}
			}
		}

		private void clearExpertsInformation(string project_uid)
		{
			Node expertsNode = null;

			expertsNode = this.MyNodeTree.GetNamedNode("experts");
			if (expertsNode != null)
			{
				foreach (Node expert_node in expertsNode.getChildren())
				{
					string expert_name = expert_node.GetAttribute("expert_name");
					string skill_type = expert_node.GetAttribute("skill_type");
					string assigned_project = expert_node.GetAttribute("assigned_project");

					if (assigned_project.ToLower() == project_uid.ToLower())
					{
						expert_node.SetAttribute("assigned_project", "");
					}
				}
			}
		}

		private void handleSetupNewProject(Node task)
		{
			ArrayList attrs= new ArrayList();
			Def_Project project_data = null;
			Def_Product product_data = null;
			Def_Platform platform_data = null;

			int request_slot_id =  task.GetIntAttribute("slotid",0);
			int request_project_id = task.GetIntAttribute("prjid",0);
			int request_product_id = task.GetIntAttribute("prdid",0);
			int request_platform_id = task.GetIntAttribute("pltid",0);

			int request_design_level = task.GetIntAttribute("design_res_level",0);
			int request_build_level = task.GetIntAttribute("build_res_level",0);
			int request_test_level = task.GetIntAttribute("test_res_level",0);

			string request_expert_design = task.GetAttribute("design_expert");
			string request_expert_build = task.GetAttribute("build_expert");
			string request_expert_test = task.GetAttribute("test_expert");

			int request_stage_a_internal = task.GetIntAttribute("stage_a_internal",0); 
			int request_stage_a_external = task.GetIntAttribute("stage_a_external",0); 
			int request_stage_b_internal = task.GetIntAttribute("stage_b_internal",0); 
			int request_stage_b_external = task.GetIntAttribute("stage_b_external",0); 
			int request_stage_c_internal = task.GetIntAttribute("stage_c_internal",0); 
			int request_stage_c_external = task.GetIntAttribute("stage_c_external",0); 
			int request_stage_d_internal = task.GetIntAttribute("stage_d_internal",0); 
			int request_stage_d_external = task.GetIntAttribute("stage_d_external",0); 
			int request_stage_e_internal = task.GetIntAttribute("stage_e_internal",0); 
			int request_stage_e_external = task.GetIntAttribute("stage_e_external",0); 
			int request_stage_f_internal = task.GetIntAttribute("stage_f_internal",0); 
			int request_stage_f_external = task.GetIntAttribute("stage_f_external",0); 
			int request_stage_g_internal = task.GetIntAttribute("stage_g_internal",0); 
			int request_stage_g_external = task.GetIntAttribute("stage_g_external",0); 
			int request_stage_h_internal = task.GetIntAttribute("stage_h_internal",0); 
			int request_stage_h_external = task.GetIntAttribute("stage_h_external",0); 

			int request_budget = task.GetIntAttribute("budget",0);
			int delay_day = task.GetIntAttribute("delay_days", 0); 
			bool use_prefered_staff_levels = task.GetBooleanAttribute("use_prefered_staff_levels", false);
			bool use_auto_start = task.GetBooleanAttribute("use_auto_start", false); 

			int predict_stage_days = 0;
			int total_stage_days = 0;
			int request_stage_internal = 0;
			int request_stage_external = 0;

			int total_stage_tasks = 0;
			int total_project_tasks = 0;
			string autostart_install_location = "";
			int autostart_install_day = 0;
			bool autostart_install_day_auto_update = false; //Do we to autochange the install based on GoLiveDay

			Node project_sub_node = null;
			string work_node_name = "";

			bool proceed = true;

			//Just a couple of safety checks 
			//Check that there is no Existing project with that slot 
			//Check that there is no Existing project with that 
			if (MyCurrProjLookup.isSlotUsed(request_slot_id))
			{
				proceed = false;
			}
			if (MyCurrProjLookup.isProjectUsed(request_project_id))
			{
				proceed = false;
			}

			if (proceed)
			{
				if (use_auto_start)
				{
					Def_Project dp = DataLookup.ProjectLookup.TheInstance.getProjectObj(request_project_id);
					if (dp != null)
					{
						request_product_id = dp.autostart_product;
						request_platform_id = dp.autostart_platform;
						autostart_install_location = dp.autostart_install_location;
						autostart_install_day = dp.autostart_install_day;
						autostart_install_day_auto_update = dp.auto_start_install_day_auto_update;
					}
					else
					{
						proceed = false;
					}
				}
			}

			if (proceed)
			{
				//Build the new project node under Root and then transfer in into "pm_running_projects"
				//This ensures that the project is fully constructed before other interested parties see it
				bool good_data = false;

				good_data = DataLookup.ProjectLookup.TheInstance.getProjectData(
					request_project_id,	request_product_id,	request_platform_id, 
					out project_data, out product_data, out platform_data);

				if (good_data)
				{
					int projectuid = GetNextProjectUID();
					int project_defined_budget = platform_data.ne_totalcost;

					//handle the cost reduction 
					//Defing at product level overrides any project level definition
					int cost_reduction = 0;
					if (product_data.cost_reduction > -1)
					{
						cost_reduction = product_data.cost_reduction;
					}
					else
					{
						if (project_data.cost_reduction > -1)
						{
							cost_reduction = project_data.cost_reduction;
						}
					}

					//If the request is wanting the predefined staff values
					// then extract then and ovverride the passed values (which should be 0 anyway)
					if (use_prefered_staff_levels)
					{
						request_budget = project_data.autostart_budget;

						work_stage ws;
						ws = platform_data.getProjectWorkStage(emPHASE_STAGE.STAGE_A);
						request_stage_a_internal = ws.prefered_int_level;
						request_stage_a_external = ws.prefered_ext_level;

						ws = platform_data.getProjectWorkStage(emPHASE_STAGE.STAGE_B);
						request_stage_b_internal = ws.prefered_int_level;
						request_stage_b_external = ws.prefered_ext_level;

						ws = platform_data.getProjectWorkStage(emPHASE_STAGE.STAGE_C);
						request_stage_c_internal = ws.prefered_int_level;
						request_stage_c_external = ws.prefered_ext_level;

						ws = platform_data.getProjectWorkStage(emPHASE_STAGE.STAGE_D);
						request_stage_d_internal = ws.prefered_int_level;
						request_stage_d_external = ws.prefered_ext_level;

						ws = platform_data.getProjectWorkStage(emPHASE_STAGE.STAGE_E);
						request_stage_e_internal = ws.prefered_int_level;
						request_stage_e_external = ws.prefered_ext_level;

						ws = platform_data.getProjectWorkStage(emPHASE_STAGE.STAGE_F);
						request_stage_f_internal = ws.prefered_int_level;
						request_stage_f_external = ws.prefered_ext_level;

						ws = platform_data.getProjectWorkStage(emPHASE_STAGE.STAGE_G);
						request_stage_g_internal = ws.prefered_int_level;
						request_stage_g_external = ws.prefered_ext_level;

						ws = platform_data.getProjectWorkStage(emPHASE_STAGE.STAGE_H);
						request_stage_h_internal = ws.prefered_int_level;
						request_stage_h_external = ws.prefered_ext_level;

						request_design_level = Math.Max(Math.Max(request_stage_a_internal, request_stage_b_internal), request_stage_c_internal);
						request_build_level =	Math.Max(request_stage_d_internal, request_stage_e_internal);
						request_test_level = Math.Max(Math.Max(request_stage_f_internal, request_stage_g_internal), request_stage_h_internal);
					}
					//System.Diagnostics.Debug.WriteLine("Creating New Project "+CONVERT.ToStr(request_project_id));

					//create the main project node 
					attrs.Clear();
					attrs.Add(new AttributeValuePair ("name", "project"+CONVERT.ToStr(projectuid)));
					attrs.Add(new AttributeValuePair ("uid", CONVERT.ToStr(projectuid)));
					attrs.Add(new AttributeValuePair ("type", "project"));
					attrs.Add(new AttributeValuePair ("slot", CONVERT.ToStr(request_slot_id)));
					attrs.Add(new AttributeValuePair ("project_id", CONVERT.ToStr(request_project_id )));
					attrs.Add(new AttributeValuePair ("product_id", CONVERT.ToStr(request_product_id)));
					attrs.Add(new AttributeValuePair ("platform_id", CONVERT.ToStr(request_platform_id)));

					attrs.Add(new AttributeValuePair ("design_reslevel", CONVERT.ToStr(request_design_level)));
					attrs.Add(new AttributeValuePair ("build_reslevel", CONVERT.ToStr(request_build_level)));
					attrs.Add(new AttributeValuePair ("test_reslevel", CONVERT.ToStr(request_test_level)));
					
					attrs.Add(new AttributeValuePair ("state_name", "PROJECT_STATE_PRODUCTSELECTED"));
					attrs.Add(new AttributeValuePair ("status", "running"));
					attrs.Add(new AttributeValuePair ("status_request", ""));

					int currentDay = CurrDayNode.GetIntAttribute("day", 0);
					if (currentDay == 0) //Are you defining project before the start of play
					{
						currentDay++;
						attrs.Add(new AttributeValuePair("product_firstworkday", "1"));
					}
					else
					{
						attrs.Add(new AttributeValuePair("product_firstworkday", CONVERT.ToStr(currentDay+1)));
					}
					attrs.Add(new AttributeValuePair ("product_selection_day", CONVERT.ToStr(currentDay)));

					//The absolute start Day 
					attrs.Add(new AttributeValuePair("project_delayed_start_day", CONVERT.ToStr(delay_day)));
					//attrs.Add(new AttributeValuePair ("project_delayed_start_days", CONVERT.ToStr(delay_days)));
					//attrs.Add(new AttributeValuePair("project_delayed_start_count", CONVERT.ToStr(delay_days)));
					
					Node prj_node = new Node (RootNode, "project", "", attrs);	
					if (prj_node != null)
					{
						//Build the Financiel data Sub Node
						attrs.Clear();
						attrs.Add(new AttributeValuePair ("type", "financial_data"));
						attrs.Add(new AttributeValuePair ("budget_player", CONVERT.ToStr(request_budget)));
						attrs.Add(new AttributeValuePair ("budget_defined", CONVERT.ToStr(project_data.budget)));
						attrs.Add(new AttributeValuePair ("projected_cost", CONVERT.ToStr(project_data.budget)));
						attrs.Add(new AttributeValuePair("cost_reduction", CONVERT.ToStr(cost_reduction)));
						attrs.Add(new AttributeValuePair("solvent", "true"));
						attrs.Add(new AttributeValuePair ("spend", "0"));
						attrs.Add(new AttributeValuePair ("name", "project_" + CONVERT.ToStr(projectuid) + "_financial_data"));
						new Node (prj_node, "financial_data", "", attrs);	

						//Build the workbooked data Sub Node
						attrs.Clear();
						attrs.Add(new AttributeValuePair ("type", "work_data"));
						attrs.Add(new AttributeValuePair ("dev_int_tasked", "0"));
						attrs.Add(new AttributeValuePair ("dev_int_worked", "0"));
						attrs.Add(new AttributeValuePair ("dev_int_wasted", "0"));
						attrs.Add(new AttributeValuePair ("dev_ext_tasked", "0"));
						attrs.Add(new AttributeValuePair ("dev_ext_worked", "0"));
						attrs.Add(new AttributeValuePair ("dev_ext_wasted", "0"));
						attrs.Add(new AttributeValuePair ("test_int_tasked", "0"));
						attrs.Add(new AttributeValuePair ("test_int_worked", "0"));
						attrs.Add(new AttributeValuePair ("test_int_wasted", "0"));
						attrs.Add(new AttributeValuePair ("test_ext_tasked", "0"));
						attrs.Add(new AttributeValuePair ("test_ext_worked", "0"));
						attrs.Add(new AttributeValuePair ("test_ext_wasted", "0"));
						
						//No longer used the slot number as this change (when 7 gets a promotion in round 3)
						//work_node_name = "project_slot_" + CONVERT.ToStr(request_slot_id) + "_work_data"
						//attrs.Add(new AttributeValuePair("name", "project_slot_" + CONVERT.ToStr(request_slot_id) + "_work_data"));
						work_node_name = "project_" + CONVERT.ToStr(projectuid) + "_work_data";
						attrs.Add(new AttributeValuePair("name", work_node_name));
						new Node (prj_node, "work_data", "", attrs);

						//Build the Project Data Sub Node
						attrs.Clear();
						attrs.Add(new AttributeValuePair("name", "project" + CONVERT.ToStr(projectuid) + "_project_data"));
						attrs.Add(new AttributeValuePair("type", "project_data"));
						attrs.Add(new AttributeValuePair ("desc", project_data.appname));
						attrs.Add(new AttributeValuePair ("planned_gain",CONVERT.ToStr(product_data.estimatedgains)));
						attrs.Add(new AttributeValuePair("planned_reduction", CONVERT.ToStr(cost_reduction)));
						attrs.Add(new AttributeValuePair("achieved_gain", "0"));

						if (project_data.isRegulation)
						{
							attrs.Add(new AttributeValuePair ("is_regulation", "true"));
						}
						else
						{
							attrs.Add(new AttributeValuePair ("is_regulation", "false"));
						}
						if (project_data.allowedrecycle)
						{
							attrs.Add(new AttributeValuePair("allowed_recycle", "true"));
						}
						else
						{
							attrs.Add(new AttributeValuePair ("allowed_recycle","false"));
						}
						attrs.Add(new AttributeValuePair("recycle_improve",CONVERT.ToStr(platform_data.ne_recycleimprovement)));
						attrs.Add(new AttributeValuePair("recycle_request_count", "0"));
						attrs.Add(new AttributeValuePair("recycle_processed_count", "0"));
						attrs.Add(new AttributeValuePair("recycle_request_pending", "false"));
						attrs.Add(new AttributeValuePair("recycle_stages_changed", "0"));

						//how good is the project being implementeted 
						attrs.Add(new AttributeValuePair ("implementation_effect",CONVERT.ToStr(platform_data.ne_implementationeffect)));
						//how good is the team of experts
						attrs.Add(new AttributeValuePair("experts_effect", CONVERT.ToStr(100)));
						//how much of thw work are we doing 
						attrs.Add(new AttributeValuePair ("scope", "100")); //affected by the descoping

						if (project_data.RequiredSkills.Count > 0)
						{
							foreach (string skill_name in project_data.RequiredSkills.Keys)
							{
								int skill_value = (int)project_data.RequiredSkills[skill_name];
								string short_skill_name = "expert_sk_" + skill_name.ToLower().Replace(" ", "_");
								attrs.Add(new AttributeValuePair(short_skill_name, CONVERT.ToStr(skill_value)));
							}
						}

						if (use_auto_start)
						{
							attrs.Add(new AttributeValuePair("install_day", CONVERT.ToStr(autostart_install_day)));
							attrs.Add(new AttributeValuePair("install_timefailure", "false"));
							attrs.Add(new AttributeValuePair("target_location", autostart_install_location));
							attrs.Add(new AttributeValuePair("install_predicted_notready", "false"));
						}
						else
						{
							attrs.Add(new AttributeValuePair("install_day", "0"));
							attrs.Add(new AttributeValuePair("target_location", ""));
							attrs.Add(new AttributeValuePair("install_predicted_notready", "false"));
						}

						if (autostart_install_day_auto_update)
						{
							attrs.Add(new AttributeValuePair("install_day_auto_update", "true"));
						}
						else 
						{
							attrs.Add(new AttributeValuePair("install_day_auto_update", "false"));
						}
						attrs.Add(new AttributeValuePair ("target_disk_requirement", CONVERT.ToStr(platform_data.ne_diskrequirements)));
						attrs.Add(new AttributeValuePair ("target_mem_requirement", CONVERT.ToStr(platform_data.ne_memoryrequirements)));

						attrs.Add(new AttributeValuePair ("next_stage_duration_change_total", ""));
						attrs.Add(new AttributeValuePair ("next_stage_duration_change_reason", ""));
						project_sub_node = new Node (prj_node, "project_data", "", attrs);							

						//Now build the Stages and Tasks 
						attrs.Clear();
						attrs.Add(new AttributeValuePair ("type", "stages"));
						Node stages_node = new Node (prj_node, "stages", "", attrs);	

						foreach (int stagecode in platform_data.newWorkStages.Keys)
						{
							work_stage ws = (work_stage) platform_data.newWorkStages[stagecode];

							switch (stagecode)
							{ 
								case (int)emPHASE_STAGE.STAGE_A:
									request_stage_internal = request_stage_a_internal;
									request_stage_external = request_stage_a_external;
									break;
								case (int)emPHASE_STAGE.STAGE_B:
									request_stage_internal = request_stage_b_internal;
									request_stage_external = request_stage_b_external;
									break;
								case (int)emPHASE_STAGE.STAGE_C:
									request_stage_internal = request_stage_c_internal;
									request_stage_external = request_stage_c_external;
									break;
								case (int)emPHASE_STAGE.STAGE_D:
									request_stage_internal = request_stage_d_internal;
									request_stage_external = request_stage_d_external;
									break;
								case (int)emPHASE_STAGE.STAGE_E:
									request_stage_internal = request_stage_e_internal;
									request_stage_external = request_stage_e_external;
									break;
								case (int)emPHASE_STAGE.STAGE_F:
									request_stage_internal = request_stage_f_internal;
									request_stage_external = request_stage_f_external;
									break;
								case (int)emPHASE_STAGE.STAGE_G:
									request_stage_internal = request_stage_g_internal;
									request_stage_external = request_stage_g_external;
									break;
								case (int)emPHASE_STAGE.STAGE_H:
									request_stage_internal = request_stage_h_internal;
									request_stage_external = request_stage_h_external;
									break;
							}
							//get the work state class to create the stage node
							ws.create_work_stage_node(stages_node, request_stage_internal, request_stage_external,
								false, out predict_stage_days, out total_stage_tasks);
							total_stage_days += predict_stage_days;
							total_project_tasks += total_stage_tasks;
						}

						project_sub_node.SetAttribute("descope_total_mandays_defined", CONVERT.ToStr(total_project_tasks));
					}

					int total_days = total_stage_days;
					total_days += 3; //two gap days 
					total_days += CurrDayNode.GetIntAttribute("day",0);

					prj_node.SetAttribute("project_golive_day", CONVERT.ToStr(total_days));
					prj_node.SetAttribute("project_display_golive_day", CONVERT.ToStr(total_days));

					if (autostart_install_day_auto_update)
					{
						project_sub_node.SetAttribute("install_day", CONVERT.ToStr(total_days-1));
					}

					//now move to the Project node 
					this.projectsRunningNode.AddChild(prj_node);
					this.MyNodeTree.FireMovedNode(RootNode, prj_node);

					if (work_node_name != "")
					{
						//we need to record the project name as known, so that we can get the work data for it
						//for the resource system later on
						attrs.Clear();
						attrs.Add(new AttributeValuePair("type", "project_known_name"));
						attrs.Add(new AttributeValuePair("value", work_node_name));
						Node wk_node = new Node(this.projectsKnownNode, "node", "", attrs);
					}

					//Need to handle the experts
					setExpertsInformation(CONVERT.ToStr(projectuid), request_expert_design, 
						request_expert_build, request_expert_test);

					//need to decrease the PMO Budget 
					ReducePMOBudget(request_budget);

					//handling the capture of the gantt plan 
					int currentDay2 = CurrDayNode.GetIntAttribute("day", 0);
					currentDay2++; // the plan starts the day after
					ProjectRunner pr = new ProjectRunner(prj_node);
					pr.CapturePlan(currentDay2);
					pr.Dispose();

					RequestSingleTimeSheetRebuild();

					//if ((projectsRunningNode.getChildren().Count == 7)||
					if (request_slot_id == 6)
					{
						bool flag = projectsRunningNode.GetBooleanAttribute("display_seven_projects",false);
						if (flag == false)
						{
							projectsRunningNode.SetAttribute("display_seven_projects", "true");
						}
					}
					//Need to pulse the Bubble Display
					Node node_bubble_display = MyNodeTree.GetNamedNode("bubble_chart_display");
					int pulse = node_bubble_display.GetIntAttribute("pulse", 0);
					node_bubble_display.SetAttribute("pulse", CONVERT.ToStr(pulse + 1));
				}
			}
			//System.Diagnostics.Debug.WriteLine("Project Creation complete");
		}

		private void getPMOBudgetData(out int budgettotal, out int budgetspent, out int budgetleft)
		{
			budgettotal = 0;
			budgetspent = 0; 
			budgetleft =  0; 

			Node pb = this.MyNodeTree.GetNamedNode("pmo_budget");
			if (pb != null)
			{
				budgettotal = pb.GetIntAttribute("budget_allowed",0);
				budgetspent = pb.GetIntAttribute("budget_spent",0); 
				budgetleft =  pb.GetIntAttribute("budget_left",0);
			}
		}

		private bool AllowedtoEditResourcesLevels(bool isinprogress, bool iscompleted)
		{
			//return ((isinprogress==false)&(iscompleted==false));
			return (iscompleted==false);
		}

		public void handleReStaffProject(Node task)
		{
			int budgettotal=0;
			int budgetspent=0;
			int budgetleft=0;
			int currentBudget=0;
			int currentSpend=0;

			string project_node_name = task.GetAttribute("project_node_name"); 

			int newbudget = task.GetIntAttribute("budget_value",0);
			int delay_start_day = task.GetIntAttribute("delayed_start_days", 0);
				
			int request_stage_a_internal = task.GetIntAttribute("stage_a_internal",0); 
			int request_stage_a_external = task.GetIntAttribute("stage_a_external",0); 
			int request_stage_b_internal = task.GetIntAttribute("stage_b_internal",0); 
			int request_stage_b_external = task.GetIntAttribute("stage_b_external",0); 
			int request_stage_c_internal = task.GetIntAttribute("stage_c_internal",0); 
			int request_stage_c_external = task.GetIntAttribute("stage_c_external",0); 
			int request_stage_d_internal = task.GetIntAttribute("stage_d_internal",0); 
			int request_stage_d_external = task.GetIntAttribute("stage_d_external",0); 
			int request_stage_e_internal = task.GetIntAttribute("stage_e_internal",0); 
			int request_stage_e_external = task.GetIntAttribute("stage_e_external",0); 
			int request_stage_f_internal = task.GetIntAttribute("stage_f_internal",0); 
			int request_stage_f_external = task.GetIntAttribute("stage_f_external",0); 
			int request_stage_g_internal = task.GetIntAttribute("stage_g_internal",0); 
			int request_stage_g_external = task.GetIntAttribute("stage_g_external",0); 
			int request_stage_h_internal = task.GetIntAttribute("stage_h_internal",0); 
			int request_stage_h_external = task.GetIntAttribute("stage_h_external",0); 

			Node project_node = this.MyNodeTree.GetNamedNode(project_node_name);
			if (project_node != null)
			{
				ProjectRunner pr = new ProjectRunner(project_node);

				int current_day = CurrDayNode.GetIntAttribute("day", 0);

				//=============================================
				//Update the Delay Start Day
				//=============================================
				//pr.UpdateDelayStartDays(delay_start_days);
				int existing_Delay_day = pr.getDelayedStartDay();
				if (existing_Delay_day != delay_start_day)
				{
					pr.setDelayedStartDay(delay_start_day);
				}

				//=============================================
				//change the Project budget (if Required)
				//=============================================
				getPMOBudgetData(out budgettotal, out budgetspent, out budgetleft);
				//We cannot reduce the budget below what we have already spent
				currentBudget = pr.getPlayerDefinedBudget();
				currentSpend = pr.getSpend();

				if (newbudget!=currentBudget)
				{
					if (newbudget<currentBudget)
					{
						//reducing budget and giving money back to PMO 
						int budget_difference = currentBudget- newbudget;
						pr.setPlayerDefinedBudget(current_day, newbudget);
						IncreasePMOBudget(budget_difference);
					}
					else
					{
						//increasing budget and taking extra money from PMO 
						int budget_difference = newbudget - currentBudget;
						pr.setPlayerDefinedBudget(current_day, newbudget);
						ReducePMOBudget(budget_difference);

						//Need to check if we are insolvent, 
						//Is the payement enough to clear the solvent flag or just reduce it.
						if (pr.GetGoodMoneyFlag()==false)
						{
							int fundinggap = pr.GetFundingGap();
							if (budget_difference>=fundinggap)
							{
								pr.SetGoodMoneyFlag(true, 0); //clears the flag and funding gap 
							}
							else
							{
								//new Gap 
								pr.setFundingGap(fundinggap-budget_difference);
							}
						}
					}
				}
				//==================================================================
				//Go through all the stages and change the staff level (if Required)
				//==================================================================
				bool iscompleted = false;
				bool isinprogress = false;
				int current_request_int = 0;
				int current_request_ext = 0;
				bool changed_resource_level = false;

				//display stage A
				pr.getRequestedResourceLevelsForStage(emProjectOperationalState.PROJECT_STATE_A, 
					out current_request_int, out current_request_ext, out isinprogress, out iscompleted);
				//if ((isinprogress==false)&(iscompleted==false))
				if (AllowedtoEditResourcesLevels(isinprogress, iscompleted))
				{
					if ((request_stage_a_internal!=current_request_int)|(request_stage_a_external!=current_request_ext))
					{
						pr.setStageResourceLevels(emProjectOperationalState.PROJECT_STATE_A,
							request_stage_a_internal, request_stage_a_external);
						changed_resource_level =true;
					}
				}
				//display stage B
				pr.getRequestedResourceLevelsForStage(emProjectOperationalState.PROJECT_STATE_B, 
					out current_request_int, out current_request_ext, out isinprogress, out iscompleted);
				//if ((isinprogress==false)&(iscompleted==false))
				if (AllowedtoEditResourcesLevels(isinprogress, iscompleted))
				{
					if ((request_stage_b_internal!=current_request_int)|(request_stage_b_external!=current_request_ext))
					{
						pr.setStageResourceLevels(emProjectOperationalState.PROJECT_STATE_B,
							request_stage_b_internal, request_stage_b_external);
						changed_resource_level =true;
					}
				}
				//display stage C
				pr.getRequestedResourceLevelsForStage(emProjectOperationalState.PROJECT_STATE_C, 
					out current_request_int, out current_request_ext, out isinprogress, out iscompleted);
				//if ((isinprogress==false)&(iscompleted==false))
				if (AllowedtoEditResourcesLevels(isinprogress, iscompleted))
				{
					if ((request_stage_c_internal!=current_request_int)|(request_stage_c_external!=current_request_ext))
					{
						pr.setStageResourceLevels(emProjectOperationalState.PROJECT_STATE_C,
							request_stage_c_internal, request_stage_c_external);
						changed_resource_level =true;
					}
				}
				//display stage D
				pr.getRequestedResourceLevelsForStage(emProjectOperationalState.PROJECT_STATE_D, 
					out current_request_int, out current_request_ext, out isinprogress, out iscompleted);
				//if ((isinprogress==false)&(iscompleted==false))
				if (AllowedtoEditResourcesLevels(isinprogress, iscompleted))
				{
					if ((request_stage_d_internal!=current_request_int)|(request_stage_d_external!=current_request_ext))
					{
						pr.setStageResourceLevels(emProjectOperationalState.PROJECT_STATE_D,
							request_stage_d_internal, request_stage_d_external);
						changed_resource_level =true;
					}
				}
				//display stage E
				pr.getRequestedResourceLevelsForStage(emProjectOperationalState.PROJECT_STATE_E, 
					out current_request_int, out current_request_ext, out isinprogress, out iscompleted);
				//if ((isinprogress==false)&(iscompleted==false))
				if (AllowedtoEditResourcesLevels(isinprogress, iscompleted))
				{
					if ((request_stage_e_internal!=current_request_int)|(request_stage_e_external!=current_request_ext))
					{
						pr.setStageResourceLevels(emProjectOperationalState.PROJECT_STATE_E,
							request_stage_e_internal, request_stage_e_external);
						changed_resource_level =true;
					}
				}

				//display stage F (maps to testing A)
				pr.getRequestedResourceLevelsForStage(emProjectOperationalState.PROJECT_STATE_F, 
					out current_request_int, out current_request_ext, out isinprogress, out iscompleted);
				//if ((isinprogress==false)&(iscompleted==false))
				if (AllowedtoEditResourcesLevels(isinprogress, iscompleted))
				{
					if ((request_stage_f_internal!=current_request_int)|(request_stage_f_external!=current_request_ext))
					{
						pr.setStageResourceLevels(emProjectOperationalState.PROJECT_STATE_F,
							request_stage_f_internal, request_stage_f_external);
						changed_resource_level =true;
					}
				}
				//display stage G (maps to testing B)
				pr.getRequestedResourceLevelsForStage(emProjectOperationalState.PROJECT_STATE_G, 
					out current_request_int, out current_request_ext, out isinprogress, out iscompleted);
				//if ((isinprogress==false)&(iscompleted==false))
				if (AllowedtoEditResourcesLevels(isinprogress, iscompleted))
				{
					if ((request_stage_g_internal!=current_request_int)|(request_stage_g_external!=current_request_ext))
					{
						pr.setStageResourceLevels(emProjectOperationalState.PROJECT_STATE_G,
							request_stage_g_internal, request_stage_g_external);
						changed_resource_level =true;
					}
				}
				//display stage H (maps to testing C)
				pr.getRequestedResourceLevelsForStage(emProjectOperationalState.PROJECT_STATE_H, 
					out current_request_int, out current_request_ext, out isinprogress, out iscompleted);
				//if ((isinprogress==false)&(iscompleted==false))
				if (AllowedtoEditResourcesLevels(isinprogress, iscompleted))
				{
					if ((request_stage_h_internal!=current_request_int)|(request_stage_h_external!=current_request_ext))
					{
						pr.setStageResourceLevels(emProjectOperationalState.PROJECT_STATE_H,
							request_stage_h_internal, request_stage_h_external);
						changed_resource_level =true;
					}
				}
				
				if (changed_resource_level)
				{
					int currentDay = CurrDayNode.GetIntAttribute("day", 0);
					pr.UpdateDaysToLive(currentDay, false);
					//Signal to the project Manager
					RequestSingleTimeSheetRebuild();
					if (current_day == 0)
					{
						//Before the Round starts, the display GoLive doewsn't need adjustment
						pr.UpdateDisplayGoLiveNumber(0, true);
					}
					else
					{
						//After the Round starts, the display GoLive doesn't need adjustment
						pr.UpdateDisplayGoLiveNumber(1, true);
					}
					pr.handleInstallDayPredictedNotReady();
				}
				pr.Dispose();
			}

			RequestSingleTimeSheetRebuild();
		}

		private void RequestSingleTimeSheetRebuild()
		{
			Node pmnode = this.MyNodeTree.GetNamedNode("pm_projects_running");
			int rebuild_timesheets_count = pmnode.GetIntAttribute("rebuild_timesheets",0);
			pmnode.SetAttribute("rebuild_timesheets",CONVERT.ToStr(rebuild_timesheets_count++));
		}

		private void ReducePMOBudget(int added_amount)
		{
			if (PMO_BudgetNode != null)
			{
				int current_left = PMO_BudgetNode.GetIntAttribute("budget_left",0);
				int current_spend = PMO_BudgetNode.GetIntAttribute("budget_spent",0);
				
				current_left -= added_amount;
				current_spend += added_amount;
				
				PMO_BudgetNode.SetAttribute("budget_left",CONVERT.ToStr(current_left));
				PMO_BudgetNode.SetAttribute("budget_spent",CONVERT.ToStr(current_spend));
			}
		}

		private void IncreasePMOBudget(int added_amount)
		{
			if (PMO_BudgetNode != null)
			{
				int current_left = PMO_BudgetNode.GetIntAttribute("budget_left",0);
				int current_spend = PMO_BudgetNode.GetIntAttribute("budget_spent",0);
				
				current_left += added_amount;
				current_spend -= added_amount;
				
				PMO_BudgetNode.SetAttribute("budget_left",CONVERT.ToStr(current_left));
				PMO_BudgetNode.SetAttribute("budget_spent",CONVERT.ToStr(current_spend));
			}
		}

		private void IncreaseCancellationMoney(int added_amount)
		{
			if (PMO_BudgetNode != null)
			{
				int cancellation_money = PMO_BudgetNode.GetIntAttribute("cancellation_money",0);
				cancellation_money += added_amount;
				PMO_BudgetNode.SetAttribute("cancellation_money",CONVERT.ToStr(cancellation_money));
			}
		}

		private void handleGlobalTeamSizeRequest(Node task)
		{
			int[] request_projects = new int[6];
			int[] request_design_team_size = new int[6];
			int[] request_build_team_size = new int[6];
			int[] request_test_team_size = new int[6];
			
			request_projects[0] = task.GetIntAttribute("slot1_prjnumber",0);
			request_design_team_size[0] = task.GetIntAttribute("slot1_design_team_size",0);
			request_build_team_size[0] = task.GetIntAttribute("slot1_build_team_size",0);
			request_test_team_size[0] = task.GetIntAttribute("slot1_test_team_size",0);

			request_projects[1] = task.GetIntAttribute("slot2_prjnumber",0);
			request_design_team_size[1] = task.GetIntAttribute("slot2_design_team_size",0);
			request_build_team_size[1] = task.GetIntAttribute("slot2_build_team_size",0);
			request_test_team_size[1] = task.GetIntAttribute("slot2_test_team_size",0);

			request_projects[2] = task.GetIntAttribute("slot3_prjnumber",0);
			request_design_team_size[2] = task.GetIntAttribute("slot3_design_team_size",0);
			request_build_team_size[2] = task.GetIntAttribute("slot3_build_team_size",0);
			request_test_team_size[2] = task.GetIntAttribute("slot3_test_team_size",0);

			request_projects[3] = task.GetIntAttribute("slot4_prjnumber",0);
			request_design_team_size[3] = task.GetIntAttribute("slot4_design_team_size",0);
			request_build_team_size[3] = task.GetIntAttribute("slot4_build_team_size",0);
			request_test_team_size[3] = task.GetIntAttribute("slot4_test_team_size",0);

			request_projects[4] = task.GetIntAttribute("slot5_prjnumber",0);
			request_design_team_size[4] = task.GetIntAttribute("slot5_design_team_size",0);
			request_build_team_size[4] = task.GetIntAttribute("slot5_build_team_size",0);
			request_test_team_size[4] = task.GetIntAttribute("slot5_test_team_size",0);

			request_projects[5] = task.GetIntAttribute("slot6_prjnumber",0);
			request_design_team_size[5] = task.GetIntAttribute("slot6_design_team_size",0);
			request_build_team_size[5] = task.GetIntAttribute("slot6_build_team_size",0);
			request_test_team_size[5] = task.GetIntAttribute("slot6_test_team_size",0);

			for (int step =0; step < 6; step++)
			{
				int prjnumber = request_projects[step];
				int request_design_size = request_design_team_size[step];
				int request_build_size = request_build_team_size[step];
				int request_test_size = request_test_team_size[step];

				if (prjnumber != -1)
				{
					Node project_node = this.MyCurrProjLookup.getProjectNode(prjnumber);
					if (project_node != null)
					{
						ProjectRunner pr = new ProjectRunner(project_node);
						int currentDay = CurrDayNode.GetIntAttribute("day", 0);
						pr.setProjectStaffLimits(currentDay, request_design_size, request_build_size, request_test_size);

						if (currentDay == 0)
						{
							//Before the Round starts, the display GoLive doewsn't need adjustment
							pr.UpdateDisplayGoLiveNumber(0, true);
						}
						else
						{
							//After the Round starts, the display GoLive doesn't need adjustment
							pr.UpdateDisplayGoLiveNumber(1, true);
						}
						pr.handleInstallDayPredictedNotReady();
						pr.Dispose();
					}
				}
			}
		}

		public void handleSetInstallDataForProject(Node task)
		{
			ArrayList attrs= new ArrayList();
			string project_nodename = task.GetAttribute("nodename");
			string project_requestlocation = task.GetAttribute("install_location");
			int install_day = task.GetIntAttribute("install_day",0);

			int current_day = CurrDayNode.GetIntAttribute("day", 0);

			Node project_node = this.MyNodeTree.GetNamedNode(project_nodename);
			if (project_node != null)
			{
				ProjectRunner pr = new ProjectRunner(project_node);
				pr.SetInstallLocation(project_requestlocation);
				pr.setInstallDay(install_day);
				pr.setInstallDayTimeFailure(false);

				if (current_day == 0)
				{
					pr.UpdateDisplayGoLiveNumber(0, true);
				}
				else
				{
					pr.UpdateDisplayGoLiveNumber(0, true);
				}
				pr.handleInstallDayPredictedNotReady();

				pr.Dispose();
			}
		}

		private void handleProjectDropTasks(Node task)
		{
			string project_nodename = task.GetAttribute("nodename");
			int project_droptasks_percentage = task.GetIntAttribute("droppercent",0);

			Node project_node = this.MyNodeTree.GetNamedNode(project_nodename);
			if (project_node != null)
			{
				ProjectRunner pr = new ProjectRunner(project_node);
				int current_day = CurrDayNode.GetIntAttribute("day", 0);
				pr.DescopeTasks(current_day, project_droptasks_percentage);

				if (current_day == 0)
				{
					pr.UpdateDisplayGoLiveNumber(0, true);
				}
				else
				{
					pr.UpdateDisplayGoLiveNumber(1, true);
				}
				pr.handleInstallDayPredictedNotReady();
				pr.Dispose();
			}
		}

		private void handleProjectDropCritPathTasks(Node task)
		{
			string project_nodename = task.GetAttribute("projectnodename");
			Node project_node = this.MyNodeTree.GetNamedNode(project_nodename);

			if (project_node != null)
			{
				string stage_b_sub_drop_requests = task.GetAttribute("stage_b_sub_drop_requests");
				string stage_b_sub_raise_requests = task.GetAttribute("stage_b_sub_raise_requests");
				string stage_d_sub_drop_requests = task.GetAttribute("stage_d_sub_drop_requests");
				string stage_d_sub_raise_requests = task.GetAttribute("stage_d_sub_raise_requests");
				int currentDay = CurrDayNode.GetIntAttribute("day", 0);

				ProjectRunner pr = new ProjectRunner(project_node);

				pr.DescopeCritPathTasks(currentDay, stage_b_sub_drop_requests,
					stage_b_sub_raise_requests, stage_d_sub_drop_requests, stage_d_sub_raise_requests);

				if (currentDay == 0)
				{
					pr.UpdateDisplayGoLiveNumber(0, true);
				}
				else
				{
					pr.UpdateDisplayGoLiveNumber(1, true);
				}
				pr.handleInstallDayPredictedNotReady();

				pr.Dispose();
			}
		}

		private void handleProjectClearInstall(Node task)
		{
			string project_nodename = task.GetAttribute("projectnodename");
			Node project_node = this.MyNodeTree.GetNamedNode(project_nodename);

			if (project_node != null)
			{
				ProjectRunner pr = new ProjectRunner(project_node);

				bool installed = pr.InState(emProjectOperationalState.PROJECT_STATE_INSTALLED_OK);
				bool completed = pr.InState(emProjectOperationalState.PROJECT_STATE_COMPLETED);

				if ((installed == false) | (completed == false))
				{
					pr.setInstallDay(0);
					pr.setInstallDayTimeFailure(false);
				}
				pr.Dispose();
			}		
		}

		private void handleProjectCancel(Node task)
		{
			int returningMoney =0;
			int canMoneyUsed = 0;

			string project_nodename = task.GetAttribute("projectnodename");
			bool killable= false;
			Node project_node = this.MyNodeTree.GetNamedNode(project_nodename);
			string project_uid_str = "";
			string project_id_str = "";

			//if the project hasn't started using resources then we can kill without waiting
			//Used primarily before the game starts (mind to hand the money back)
			if (project_node != null)
			{
				ProjectRunner pr = new ProjectRunner(project_node);

				project_uid_str = CONVERT.ToStr(pr.getProjectUniqueID());
				project_id_str = CONVERT.ToStr(pr.getProjectID());

				killable = true;
				//pa.setStatusRequest_PreCancel(out killable);

				if (killable)
				{
					pr.AddCancelFlag();
					returningMoney = pr.getBudgetleft();
					canMoneyUsed = pr.getSpend();
					pr.ReAssignWorkers(0, true);
					pr.FireWorkers(0,true); // Day seems to be ignored!
				}
				pr.Dispose();

				if (killable)
				{
					this.IncreasePMOBudget(returningMoney);
					this.IncreaseCancellationMoney(canMoneyUsed);
					project_node.Parent.DeleteChildTree(project_node);
				}
				NodeTree nt = this.MyNodeTree;

				//Add it to the list of cancelled projects 
				Node projectsCancelledNode = MyNodeTree.GetNamedNode("pm_projects_cancelled");
				ArrayList attrs = new ArrayList();
				attrs.Clear();
				attrs.Add(new AttributeValuePair("type", "project_cancelled_name"));
				attrs.Add(new AttributeValuePair("project_unique_id", project_uid_str));
				attrs.Add(new AttributeValuePair("project_id", project_id_str));
				Node wk_node = new Node(projectsCancelledNode, "node", "", attrs);
				//Need to pulse the Bubble Display
				Node node_bubble_display = MyNodeTree.GetNamedNode("bubble_chart_display");
				int pulse = node_bubble_display.GetIntAttribute("pulse", 0);
				node_bubble_display.SetAttribute("pulse", CONVERT.ToStr(pulse+1));

				clearExpertsInformation(project_uid_str);

				RequestSingleTimeSheetRebuild();
			}
		}

		private void handleRebuildProject(Node task)
		{
			string project_nodename = task.GetAttribute("nodename");
			string cmd_action = task.GetAttribute("cmd_action");
			bool action_performed= false;
			Node project_node = this.MyNodeTree.GetNamedNode(project_nodename);
			int current_day = CurrDayNode.GetIntAttribute("day", 0);

			if (project_node != null)
			{
				ProjectRunner pr = new ProjectRunner(project_node);

				if (cmd_action.ToLower() == "add_cycle")
				{
					pr.setOneRecycleRequest();
					pr.UpdateDaysToLive(current_day, false);
					action_performed = true;
				}
				if (cmd_action.ToLower()== "clear_cycle")
				{
					pr.clearAllRecycleRequest();
					pr.UpdateDaysToLive(current_day, false);
					action_performed = true;
				}
				if (action_performed)
				{
					if (current_day == 0)
					{
						pr.UpdateDisplayGoLiveNumber(0, true);
					}
					else
					{
						pr.UpdateDisplayGoLiveNumber(1, true);
					}
					pr.handleInstallDayPredictedNotReady();
				}
				pr.Dispose();
			}
		}

		private void handleProjectPause(Node task)
		{
			string project_nodename = task.GetAttribute("projectnodename");

			Node project_node = this.MyNodeTree.GetNamedNode(project_nodename);
			if (project_node != null)
			{
				ProjectRunner pr = new ProjectRunner(project_node);
				pr.setStatusRequest_PrePause();
				pr.Dispose();
			}
		}

		private void handleProjectResume(Node task)
		{
			string project_nodename = task.GetAttribute("projectnodename");

			Node project_node = this.MyNodeTree.GetNamedNode(project_nodename);
			if (project_node != null)
			{
				ProjectRunner pr = new ProjectRunner(project_node);
				pr.setStatusRequest_PreResume();
				pr.Dispose();
			}
		}

		private void handleRequestOpsUpgrade(Node task)
		{
			//This directly injects a ops request into the ops queue
			//there is no rx_nodeName since there is no FSC to mark done
			string rx_nodename = "";
			string action = task.GetAttribute("action");
			string location = task.GetAttribute("location");
			string requested_day = task.GetAttribute("day");
			string memory_change = task.GetAttribute("memory_change");
			string disk_change = task.GetAttribute("disk_change");
			string money_cost = task.GetAttribute("money_cost");

			//data matches the required entry data 
			ArrayList attrs = new ArrayList ();
			attrs.Add(new AttributeValuePair ("type" ,"ops_item"));
			attrs.Add(new AttributeValuePair ("action" ,action));
			attrs.Add(new AttributeValuePair ("day", requested_day));  //no preffered day
			attrs.Add(new AttributeValuePair ("duration", "1"));
			attrs.Add(new AttributeValuePair ("status" ,"todo"));
			attrs.Add(new AttributeValuePair ("location" ,location));
			attrs.Add(new AttributeValuePair ("display" ,location));
			attrs.Add(new AttributeValuePair ("memory_change" ,memory_change));
			attrs.Add(new AttributeValuePair ("disk_change" ,disk_change));
			attrs.Add(new AttributeValuePair ("money_cost" ,money_cost));
			attrs.Add(new AttributeValuePair ("rx_nodename" ,rx_nodename));
			//Determine which icon to use 
			string icon = "memory.png";
			switch (action)
			{
				case "upgrade_memory":
					icon = "memory.png";
					break;
				case "upgrade_disk":
					icon = "storage.png";
					break;
				case "upgrade_both":
					icon = "memory_and_storage.png";
					break;
			}
			attrs.Add(new AttributeValuePair("icon", icon));

			//attrs.Add(new AttributeValuePair ("day", day_number));
			new Node (ops_worklist_node, "task", "", attrs);				
		}

		private void handleRemoveOpsUpgrade(Node task)
		{
			string requested_remove_display = task.GetAttribute("upgrade_displayname");
			string requested_remove_day = task.GetAttribute("upgrade_day");

			Node kill_node = null;
			foreach (Node ops_job in ops_worklist_node.getChildren())
			{
				string display = ops_job.GetAttribute("display");
				string day = ops_job.GetAttribute("day");

				bool correctname = requested_remove_display.ToLower() == display.ToLower();
				bool correctday = requested_remove_day.ToLower() == day.ToLower();
				if ((correctname)&(correctday))
				{
					kill_node = ops_job;
				}
			}
			if (kill_node != null)
			{
				kill_node.Parent.DeleteChildTree(kill_node);
			}
		}

		private void handleRequestFSC(Node task)
		{
			string request_fsc_id = task.GetAttribute("fsc_id");
			string fsc_name = "fsc"+request_fsc_id;

			//This creates a new job in the ops_worklist queue
			//so that the op manager does the required upgrade work and blocks any project installs 
			//very simple method for handling both FSC and Upgrades
			//with one Manager that performs the work and blocks the Projects
			//mostly just transshipping the information
			Node fsc_node = this.MyNodeTree.GetNamedNode(fsc_name);
			if (fsc_node != null)
			{
				string status_str = fsc_node.GetAttribute("status");
				if (status_str.ToLower()=="todo")
				{
					string rx_nodename = fsc_node.GetAttribute("name");
					string action = fsc_node.GetAttribute("action");
					string dest = fsc_node.GetAttribute("dest");
					string location = fsc_node.GetAttribute("location");
					string memory_change = fsc_node.GetAttribute("memory");
					string disk_change = fsc_node.GetAttribute("disk");
					string money_cost = fsc_node.GetAttribute("money_cost");
					int time_cost = fsc_node.GetIntAttribute("time_cost",-1);

					//data matches the required entry data 
					ArrayList attrs = new ArrayList ();
					attrs.Add(new AttributeValuePair ("type" ,"ops_item"));
					attrs.Add(new AttributeValuePair ("action" ,action));
					attrs.Add(new AttributeValuePair ("day" ,"-1"));  //no preffered day
					attrs.Add(new AttributeValuePair ("days_left",CONVERT.ToStr(time_cost)));
					attrs.Add(new AttributeValuePair ("duration", CONVERT.ToStr(time_cost)));
					attrs.Add(new AttributeValuePair ("status" ,"todo"));
					attrs.Add(new AttributeValuePair ("location" ,dest));
					attrs.Add(new AttributeValuePair ("display" ,dest));
					attrs.Add(new AttributeValuePair ("memory_change" ,memory_change));
					attrs.Add(new AttributeValuePair ("disk_change" ,disk_change));
					attrs.Add(new AttributeValuePair ("money_cost" ,money_cost));
					attrs.Add(new AttributeValuePair ("rx_nodename" ,rx_nodename));
					attrs.Add(new AttributeValuePair("icon", "projectinstall.png"));

					//attrs.Add(new AttributeValuePair ("day", day_number));
					new Node (ops_worklist_node, "task", "", attrs);	

					//Mind to set the FSC node to pending to prevent a second Click 
					fsc_node.SetAttribute("status","pending");
				}
			}
		}

		private void handleRemoveFSC(Node task)
		{
		}

		private void handleRequestCC(Node task)
		{
			int request_cc_id = task.GetIntAttribute("cc_id",-1);
			string request_cc_nodename = task.GetAttribute("cc_nodename");
			int request_cc_day = task.GetIntAttribute("cc_day",0);
			string request_cc_location = task.GetAttribute("cc_location");

			string dest = "Change " + request_cc_id;
			//This creates a new job in the ops_worklist queue
			//so that the op manager does the required upgrade work and blocks any project installs 

			//very simple method for handling both FSC and Upgrades
			//with one Manager that performs the work and blocks the Projects
 
			//mostly just transshipping the information
			Node cc_node = this.MyNodeTree.GetNamedNode(request_cc_nodename);
			if (cc_node != null)
			{
				string status_str = cc_node.GetAttribute("status");
				if (status_str.ToLower()=="todo")
				{
					string rx_nodename = cc_node.GetAttribute("name");
					string action = cc_node.GetAttribute("action");
					//string dest = cc_node.GetAttribute("dest");
					string location = cc_node.GetAttribute("location");
					string memory_change = cc_node.GetAttribute("memory");
					string disk_change = cc_node.GetAttribute("disk");
					string platform_required = cc_node.GetAttribute("platform");
					string money_cost = cc_node.GetAttribute("money_cost");
					string cc_appname = cc_node.GetAttribute("appname");
					int time_cost = cc_node.GetIntAttribute("time_cost",-1);

					//data matches the required entry data 
					ArrayList attrs = new ArrayList ();
					attrs.Add(new AttributeValuePair ("type" ,"ops_item"));
					attrs.Add(new AttributeValuePair ("action" ,action));
					attrs.Add(new AttributeValuePair ("day", request_cc_day)); //Scheduled Card Change 
					attrs.Add(new AttributeValuePair ("days_left",time_cost));
					attrs.Add(new AttributeValuePair ("duration", CONVERT.ToStr(time_cost)));
					attrs.Add(new AttributeValuePair ("status", "todo"));
					attrs.Add(new AttributeValuePair ("location" ,request_cc_location));
					attrs.Add(new AttributeValuePair ("display" ,dest));
					attrs.Add(new AttributeValuePair ("memory_change" ,memory_change));
					attrs.Add(new AttributeValuePair ("disk_change" ,disk_change));
					attrs.Add(new AttributeValuePair ("money_cost" ,money_cost));
					attrs.Add(new AttributeValuePair ("rx_nodename" ,rx_nodename));
					attrs.Add(new AttributeValuePair ("cc_appname" ,cc_appname));
					attrs.Add(new AttributeValuePair ("cc_platform" ,platform_required));
					attrs.Add(new AttributeValuePair ("cc_card_change_id",CONVERT.ToStr(request_cc_id)));
					attrs.Add(new AttributeValuePair("icon", "change" + CONVERT.ToStr(request_cc_id) + ".png"));

					new Node(ops_worklist_node, "task", "", attrs);	

					//Mind to set the FSC node to pending to prevent a second Click 
					cc_node.SetAttribute("status","pending");
				}
			}
		}

		private void handleRemoveCC(Node task)
		{
			string requested_remove = task.GetAttribute("changedisplayname");

			Node kill_node = null;
			foreach (Node ops_job in ops_worklist_node.getChildren())
			{
				string display = ops_job.GetAttribute("display");
				string status = ops_job.GetAttribute("status");

				if (status.ToLower() == "todo")
				{
					if (requested_remove.ToLower() == display.ToLower())
					{
						kill_node = ops_job;
					}
				}
			}
			if (kill_node != null)
			{
				//Need to update the chnage card node to allow further selection
				string status = kill_node.GetAttribute("status");
				string card_node_name = kill_node.GetAttribute("rx_nodename");
				Node cc_node = this.MyNodeTree.GetNamedNode(card_node_name);
				if (cc_node != null)
				{
					cc_node.SetAttribute("status", "todo");
				}
				kill_node.Parent.DeleteChildTree(kill_node);
			}
		}

		private void handleChangePMOBudget(Node task)
		{
			int requested_newbudget = task.GetIntAttribute("pmo_newvalue",0);

			Node budget_node = this.MyNodeTree.GetNamedNode("pmo_budget");
			int budget_spent = budget_node.GetIntAttribute("budget_spent", 0);
			//int cancel_spend = budget_node.GetIntAttribute("cancellation_money", 0);

			budget_node.SetAttribute("budget_allowed", CONVERT.ToStr(requested_newbudget));
			int new_left = requested_newbudget - (budget_spent);
			budget_node.SetAttribute("budget_left", CONVERT.ToStr(new_left));
		}

		private void handleChangeMarketPos(Node task)
		{
			int requested_new_market_pos = task.GetIntAttribute("predictpos_newvalue", 0);

			Node op_results_node = this.MyNodeTree.GetNamedNode("operational_results");
			int predict_pos = op_results_node.GetIntAttribute("predicted_market_position", 0);

			op_results_node.SetAttribute("predicted_market_position", CONVERT.ToStr(requested_new_market_pos));
		}

		private void handleEnableExperts(Node task)
		{
			bool requested_status = task.GetBooleanAttribute("status", false);
			Node experts_node = this.MyNodeTree.GetNamedNode("experts");
			experts_node.SetAttribute("enabled", CONVERT.ToStr(requested_status));
		}

		private void handleChangeExperts(Node task)
		{
			Hashtable expertstatus = new Hashtable();
			ArrayList attrs = task.AttributesAsArrayList;

			foreach (AttributeValuePair avp in attrs)
			{
				string attr_name = avp.Attribute;
				string attr_value = avp.Value;
				bool valid = false;

				//Strip the debug prefixs
				if (attr_name.IndexOf("_da_")>-1)
				{
					attr_name = attr_name.Replace("exp_da_","");
					valid = true;
				}
				if (attr_name.IndexOf("_bm_")>-1)
				{
					attr_name = attr_name.Replace("exp_bm_", "");
					valid = true;
				}
				if (attr_name.IndexOf("_tm_")>-1)
				{
					attr_name = attr_name.Replace("exp_tm_", "");
					valid = true;
				}
				if (valid)
				{
					expertstatus.Add(attr_name, attr_value);
				}
			}

			Node expertsNode = this.MyNodeTree.GetNamedNode("experts");

			if (expertsNode != null)
			{
				foreach (Node expert_node in expertsNode.getChildren())
				{
					string expert_name = expert_node.GetAttribute("expert_name");
					string skill_type = expert_node.GetAttribute("skill_type");
					string assigned_project = expert_node.GetAttribute("assigned_project");

					if (expertstatus.ContainsKey(expert_name.ToLower())==true)
					{
						string targetProject = (string)expertstatus[expert_name.ToLower()];
						expert_node.SetAttribute("assigned_project",targetProject);
					}
					else
					{
						expert_node.SetAttribute("assigned_project","");
					}
				}
			}
		}

		/// <summary>
		/// Catch the new request, process it and remove it
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="task"></param>
		private void queueNode_ChildAdded (Node sender, Node task)
		{
			opsEngine.SetGameStarted();
			HandleTaskRequest(task);					//process the request
			queueNode.DeleteChildTree(task);	//delete the command node
		}

		void HandleTaskRequest (Node task)
		{
			//Identify what to do 
			string cmd_type = task.GetAttribute("cmd_type");
			switch (cmd_type)
			{
				case "request_opsupgrade":
					handleRequestOpsUpgrade(task);
					break;
				case "remove_opsupgrade":
					handleRemoveOpsUpgrade(task);
					break;
				case "request_fsc":
					handleRequestFSC(task);
					break;
				case "remove_fsc":
					handleRemoveFSC(task);
					break;
				case "request_cc":
					handleRequestCC(task);
					break;
				case "remove_cc":
					handleRemoveCC(task);
					break;
				case "droptasks_project":
					handleProjectDropTasks(task);
					break;
				case "dropcritpath_project":
					handleProjectDropCritPathTasks(task);
					break;
				case "cancel_project":
					handleProjectCancel(task);
					break;
				case "clear_project_install":
					handleProjectClearInstall(task);
					break;
				case "pause_project":
					handleProjectPause(task);
					break;
				case "resume_project":
					handleProjectResume(task);
					break;
				case "request_new_project":
					handleSetupNewProject(task);
					break;
				case "restaff_project":
					handleReStaffProject(task);
					break;
				case "request_teamsize_change":
					handleGlobalTeamSizeRequest(task);
					break;
				case "install_project":
					handleSetInstallDataForProject(task);
					break;
				case "rebuild_project":
					handleRebuildProject(task);
					break;
				case "change_pmo":
					handleChangePMOBudget(task);
					break;
				case "change_predict_pos":
					handleChangeMarketPos(task);
					break;
				case "change_all_experts":
					handleChangeExperts(task);
					break;
				case "enable_all_experts":
					handleEnableExperts(task);
					break;
			}

			OnTaskProcessed();
		}

		void OnTaskProcessed ()
		{
			if (TaskProcessed != null)
			{
				TaskProcessed(this, new EventArgs ());
			}
		}
	}
}

