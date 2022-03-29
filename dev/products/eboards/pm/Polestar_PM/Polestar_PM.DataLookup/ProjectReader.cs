using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Text;

using Network;
using BaseUtils;
using CoreUtils;
using LibCore;

namespace Polestar_PM.DataLookup
{
	public class ProjectReader
	{
		protected int worker_internal_daypayrate = 2000; //default level from old PM game 8*250=2000
		protected int worker_external_daypayrate = 6000; //default level from old PM game 8*750=6000

		protected NodeTree MyNodeTree = null;
		protected Node project_node = null;
		protected string project_id_str = "";
		protected ArrayList project_work_stages = new ArrayList();
		protected ArrayList project_work_stage_nodes = new ArrayList();
		protected bool singleProjectInstall = false;

		protected Node project_subnode_workdata = null;
		protected Node project_subnode_findata = null;
		protected Node project_subnode_prjdata = null;
		protected Node project_subnode_wk_stages = null;

		protected emProjectOperationalState currentState = emProjectOperationalState.PROJECT_STATE_UNKNOWN;

		public ProjectReader(Node prjnode)
		{
			project_node = prjnode;
			MyNodeTree = project_node.Tree;

			worker_internal_daypayrate = SkinningDefs.TheInstance.GetIntData("worker_internal_daypayrate", 0);
			worker_external_daypayrate = SkinningDefs.TheInstance.GetIntData("worker_external_daypayrate", 0);

			project_id_str = project_node.GetAttribute("project_id");
			
			foreach (Node subnode in project_node.getChildren())
			{
				string sub_node_type = subnode.GetAttribute("type");
				if (sub_node_type == "financial_data")
				{
					project_subnode_findata = subnode;
				}
				if (sub_node_type == "project_data")
				{
					project_subnode_prjdata = subnode;
				}
				if (sub_node_type == "work_data")
				{
					project_subnode_workdata = subnode;
				}
				if (sub_node_type == "stages")
				{
					project_subnode_wk_stages = subnode;
					project_work_stages.Clear();
					foreach (Node stge in subnode.getChildren())
					{
						work_stage ws = new work_stage();
						ws.loadfromNetworkStageNode(stge);
						project_work_stages.Add(ws);
						project_work_stage_nodes.Add(stge);
					}
				}
			}
			//load the current state 
			determinecurrentstate();
		}

		public void Dispose()
		{
			project_node = null;
			project_subnode_findata = null;
			project_subnode_prjdata = null;
			project_subnode_wk_stages = null;
			project_work_stages.Clear();
			project_work_stage_nodes.Clear();
		}

		public void AddAdditionalStagestoList()
		{ 
			foreach (Node subnode in project_node.getChildren())
			{
				string sub_node_type = subnode.GetAttribute("type");
				if (sub_node_type == "stages")
				{
					foreach (Node stge in subnode.getChildren())
					{
						if (project_work_stage_nodes.Contains(stge) == false)
						{
							work_stage ws = new work_stage();
							ws.loadfromNetworkStageNode(stge);
							project_work_stages.Add(ws);
							project_work_stage_nodes.Add(stge);
						}
					}
				}
			}
		}

		protected void determinecurrentstate()
		{
			string state_name = project_node.GetAttribute("state_name");
			currentState = (emProjectOperationalState)Enum.Parse(typeof(emProjectOperationalState), state_name.ToUpper());
		}


		public Node getProjectSubNode()
		{
			return project_subnode_prjdata;
		}
		public Node getWorkSubNode()
		{
			return project_subnode_workdata;
		}
		public Node getFinSubNode()
		{
			return project_subnode_findata;
		}

		public Node getWorkStagesSubNode()
		{
			return project_subnode_wk_stages;
		}

		public int getWorkerInternal_HourlyPayRate()
		{
			return worker_internal_daypayrate;
		}
		public int getWorkerExternal_HourlyPayRate()
		{
			return worker_external_daypayrate;
		}

		public int getProjectSlot()
		{
			return this.project_node.GetIntAttribute("slot", 0);
		}

		public int getProjectSelectionDay()
		{
			return this.project_node.GetIntAttribute("product_selection_day", 0);
		}

		public int getDelayedStartDay()
		{
		  return this.project_node.GetIntAttribute("project_delayed_start_day", 0);
		}

		public int getProjectFirstWorkDay()
		{
			return this.project_node.GetIntAttribute("product_firstworkday", 0);
		}

		public int getProjectDisplayGoLiveDay()
		{
			int go_live = project_node.GetIntAttribute("project_display_golive_day", 0);
			return go_live;
		}

		public int getProjectGoLiveDay()
		{
			int go_live = project_node.GetIntAttribute("project_golive_day", 0);
			return go_live;
		}

		public int getProjectUniqueID()
		{
			return this.project_node.GetIntAttribute("uid", 0);
		}

		public int getProjectID()
		{
			return this.project_node.GetIntAttribute("project_id", 0);
		}

		public int getProductID()
		{
			return this.project_node.GetIntAttribute("product_id", 0);
		}

		public int getPlatformID()
		{
			return this.project_node.GetIntAttribute("platform_id", 0);
		}

		public string getProjectDesc()
		{
			return this.project_node.GetAttribute("desc");
		}

		public bool isRegulationProject()
		{
			bool reg_prj = project_subnode_prjdata.GetBooleanAttribute("is_regulation", false);
			return reg_prj;
		}

		private string TranslatePlatformToStr(int platform_id)
		{
			string platform_display_str = "X";
			switch (platform_id)
			{
				case 1:
					platform_display_str = "X";
					break;
				case 2:
					platform_display_str = "Y";
					break;
				case 3:
					platform_display_str = "Z";
					break;
			}
			return platform_display_str;
		}

		public string getProjectTextDescription()
		{
			string prj_description = "";
			int plat = this.getPlatformID();
			
			prj_description += CONVERT.ToStr(this.getProductID());
			prj_description += " " + TranslatePlatformToStr(plat);

			return prj_description;
		}


		#region Status Request Methods

		public string getStatusRequest()
		{
			return project_node.GetAttribute("status_request");
		}

		public bool isStatusRequestPreCancel()
		{
			string request = getStatusRequest();
			if (request.ToLower() == "precancel")
			{
				return true;
			}
			return false;
		}

		public bool isStatusRequestPrePause()
		{
			string request = getStatusRequest();
			if (request.ToLower() == "prepause")
			{
				return true;
			}
			return false;
		}
		public bool isStatusRequestPreResume()
		{
			string request = getStatusRequest();
			if (request.ToLower() == "preresume")
			{
				return true;
			}
			return false;
		}

		public bool isPaused()
		{
			string status = this.project_node.GetAttribute("status");
			if (status == "paused")
			{
				return true;
			}
			return false;
		}

		public bool isPausedByNoMoney()
		{
			string status = this.project_node.GetAttribute("status");
			if (status == "paused_no_money")
			{
				return true;
			}
			return false;
		}

		#endregion Status Request Methods

		#region Money (Budget And Expenditure) Methods

		public int GetFundingGap()
		{
			return project_subnode_findata.GetIntAttribute("funding_gap", 0);
		}

		public bool GetGoodMoneyFlag()
		{
			bool isSolvent = project_subnode_findata.GetBooleanAttribute("solvent", false);
			return isSolvent;
		}

		public int getSIPDefinedBudget()
		{
			int budget_defined_by_sip = project_subnode_findata.GetIntAttribute("budget_defined", 0);
			return budget_defined_by_sip;
		}

		public int getPlayerDefinedBudget()
		{
			int budget_defined_by_player = project_subnode_findata.GetIntAttribute("budget_player", 0);
			return budget_defined_by_player;
		}

		public int getSpend()
		{
			int spend = project_subnode_findata.GetIntAttribute("spend", 0);
			return spend;
		}

		public int getBudgetleft()
		{
			int budgetleft = getPlayerDefinedBudget() - getSpend();
			return budgetleft;
		}

		#endregion Money (Budget And Expenditure) Methods

		#region Gain And Scope Methods

		public int getHandoverEffect()
		{
			int handover_effect = project_subnode_prjdata.GetIntAttribute("implementation_effect", 0);
			return handover_effect;
		}

		public int getProjectScope()
		{
			int scope = project_subnode_prjdata.GetIntAttribute("scope", 0);
			return scope;
		}

		public int getDisplayedHandoverEffect()
		{
			int handover_effect = getHandoverEffect();
			int experts_effect = getExpertsEffect();
			int displayed_handover_effect = handover_effect;

			Node expertsNode = this.MyNodeTree.GetNamedNode("experts");
			if (expertsNode != null)
			{
				bool usingExperts = expertsNode.GetBooleanAttribute("enabled",false);
				if (usingExperts)
				{
					displayed_handover_effect = handover_effect - (100 - experts_effect);
				}
			}
			return displayed_handover_effect;
		}

		public int getExpertsEffect()
		{
			int experts_effect = project_subnode_prjdata.GetIntAttribute("experts_effect", 0);
			return experts_effect;
		}

		public int getGainAchieved()
		{
			int gain_achieved = project_subnode_prjdata.GetIntAttribute("achieved_gain", 0);
			return gain_achieved;
		}

		public int getCostReductionAchieved ()
		{
			return project_subnode_prjdata.GetIntAttribute("achieved_cost_reduction", 0);
		}

		public int getGainPlanned()
		{
			int gain_planned = project_subnode_prjdata.GetIntAttribute("planned_gain", 0);
			return gain_planned;
		}

		public int getReductionPlanned ()
		{
			return project_subnode_prjdata.GetIntAttribute("planned_reduction", 0);
		}

		#endregion Gain And Scope Methods

		#region Project Installation Methods

		public bool getInstallDayPredictedNotReady()
		{
			bool install_predicted_not_ready = this.project_subnode_prjdata.GetBooleanAttribute("install_predicted_notready", false);
			return install_predicted_not_ready;
		}

		public bool getInstallDayTimeFailure()
		{
			bool install_timefailure = this.project_subnode_prjdata.GetBooleanAttribute("install_timefailure",false);
			return install_timefailure;
		}

		public string getInstallLocation()
		{
			string install_location = this.project_subnode_prjdata.GetAttribute("target_location");
			return install_location;
		}

		public int getInstallDay()
		{
			int install_day = this.project_subnode_prjdata.GetIntAttribute("install_day",0);
			return install_day;
		}

		public bool getInstallDayAutoUpdate()
		{
			bool autoUpdate = this.project_subnode_prjdata.GetBooleanAttribute("install_day_auto_update", false);
			return autoUpdate;
		}

		public string getInstallError()
		{
			string install_failure_reason = project_node.GetAttribute("install_fail_reason");
			return install_failure_reason;
		}

		public bool isTargetLocationDefined()
		{
			bool location_defined = false;
			string install_location = this.project_subnode_prjdata.GetAttribute("target_location");
			if (install_location != "")
			{
				location_defined = true;
			}
			return location_defined;
		}

		#endregion Project Installation Methods

		#region Project State Methods

		/// <summary>
		/// This currently used to disable the Delays days once the project is running
		/// Should be removed infavour of the better and more general 
		/// isCurrentStateOrLater(emProjectOperationalState tmpState)
		/// 
		/// minor refactor 
		/// </summary>
		/// <returns></returns>
		public bool isStateBeforeStateA()
		{
			bool beforeA_flag = false;
			determinecurrentstate();
			switch (currentState)
			{
				case emProjectOperationalState.PROJECT_STATE_EMPTY:
				case emProjectOperationalState.PROJECT_STATE_PROJECTSELECTED:
				case emProjectOperationalState.PROJECT_STATE_PRODUCTSELECTED:
					beforeA_flag = true;
					break;
				default:
					break;
			}
			return beforeA_flag;
		}

		public bool isCurrentStateRecycle()
		{
			bool isRecycle = false;

			determinecurrentstate();

			switch (currentState)
			{
				case emProjectOperationalState.PROJECT_STATE_I:
				case emProjectOperationalState.PROJECT_STATE_J:
				case emProjectOperationalState.PROJECT_STATE_K:
				case emProjectOperationalState.PROJECT_STATE_L:
				case emProjectOperationalState.PROJECT_STATE_M:
				case emProjectOperationalState.PROJECT_STATE_N:
				case emProjectOperationalState.PROJECT_STATE_P:
				case emProjectOperationalState.PROJECT_STATE_Q:
				case emProjectOperationalState.PROJECT_STATE_R:
				case emProjectOperationalState.PROJECT_STATE_T:
					isRecycle = true;
					break;
			}
			return isRecycle;
		}


		public bool isCurrentStateOrLater(emProjectOperationalState tmpState)
		{
			bool iscurrentstate = false;

			determinecurrentstate();
			if (currentState >= tmpState)
			{
				iscurrentstate = true;
			}
			return iscurrentstate;
		}

		public bool isCurrentState(emProjectOperationalState tmpState)
		{
			bool iscurrentstate = false;

			determinecurrentstate();
			if (currentState == tmpState)
			{
				iscurrentstate = true;
			}
			return iscurrentstate;
		}

		public bool inHandover()
		{
			determinecurrentstate();

			if (this.currentState == emProjectOperationalState.PROJECT_STATE_IN_HANDOVER)
			{
				return true;
			}
			return false;
		}

		public void getFullTaskCountsForStage(emProjectOperationalState dispState,
			out int total_task_count, out int dropped_task_count, out int remaining_task_count)
		{
			total_task_count = 0;
			dropped_task_count = 0;
			remaining_task_count = 0;

			work_stage ws = this.getWorkStage(dispState);
			ws.getFullTaskCountsForStage(out total_task_count, out dropped_task_count, out remaining_task_count);
		}

		public bool InState(emProjectOperationalState testState)
		{
			determinecurrentstate();
			int current_state_value = (int)currentState;
			int test_state_value = (int)testState;
			if ((current_state_value) == (test_state_value))
			{
				return true;
			}
			return false;
		}

		public void shouldDisplayState(emProjectOperationalState dispState, out bool display, out bool inhand)
		{
			determinecurrentstate();
			int current_state_value = (int)currentState;
			int request_state_value = (int)dispState;
			display = false;
			inhand = false;

			if (current_state_value >= request_state_value)
			{
				display = true;
				if (current_state_value == request_state_value)
				{
					inhand = true;
				}
			}
		}

		#endregion Project State Methods

		public void shouldDisplayStateForTaskStage(emProjectOperationalState dispState,
			out bool display, out bool paused, out bool inhand, out int allocated, out int requested, out int days)
		{
			determinecurrentstate();
			int current_state_value = (int)currentState;
			int request_state_value = (int)dispState;
			paused = false;
			display = false;
			inhand = false;
			allocated = 0;
			requested = 0;
			days = 0;

			paused = this.isPaused() | this.isPausedByNoMoney();

			if (current_state_value >= request_state_value)
			{
				display = true;
				if (current_state_value == request_state_value)
				{
					inhand = true;
					work_stage ws = this.getWorkStage(currentState);
					//ws.getRequestedAndAllocatedResourceLevels(out requested, out allocated, out days);
					ws.getDisplayRequestedAndAllocatedResourceLevels(out requested, out allocated, out days);
				}
			}
		}

		protected work_stage getWorkStageByStageCode(string requested_stage_code)
		{
			work_stage tmpWorkStageObj = null;
			if (project_node != null)
			{
				foreach (work_stage ws in project_work_stages)
				{
					string code = ws.getCode();
					string desc = ws.getDesc();

					string stage_code = desc.ToLower() + "_" + code.ToLower();
					//string action = tn.GetAttribute("action");
					//string desc = tn.GetAttribute("desc");
					if (requested_stage_code.ToLower() == stage_code.ToLower())
					{
						tmpWorkStageObj = ws;
					}
				}
			}
			return tmpWorkStageObj;
		}

		public work_stage getWorkStageCloneForStage(emProjectOperationalState requestedState)
		{
			//TODO need to handle the recycle as well 
			work_stage ws2 = null;
			work_stage ws = getWorkStage(requestedState);
			if (ws != null)
			{
				ws2 = new work_stage();
				ws2.loadfromNetworkStageNode(ws.getStageNode());
				ws2.loadSubTasks();
			}
			return ws2;
		}

		protected work_stage getWorkStage(emProjectOperationalState requestedState)
		{
			work_stage tmpWorkStageObj = null;

			switch (requestedState)
			{
				case emProjectOperationalState.PROJECT_STATE_EMPTY:
				case emProjectOperationalState.PROJECT_STATE_PROJECTSELECTED:
				case emProjectOperationalState.PROJECT_STATE_PRODUCTSELECTED:
					break;
				case emProjectOperationalState.PROJECT_STATE_A:
					tmpWorkStageObj = getWorkStageByStageCode("dev_a");
					break;
				case emProjectOperationalState.PROJECT_STATE_B:
					tmpWorkStageObj = getWorkStageByStageCode("dev_b");
					break;
				case emProjectOperationalState.PROJECT_STATE_C:
					tmpWorkStageObj = getWorkStageByStageCode("dev_c");
					break;
				case emProjectOperationalState.PROJECT_STATE_D:
					tmpWorkStageObj = getWorkStageByStageCode("dev_d");
					break;
				case emProjectOperationalState.PROJECT_STATE_E:
					tmpWorkStageObj = getWorkStageByStageCode("dev_e");
					break;
				case emProjectOperationalState.PROJECT_STATE_F:
					tmpWorkStageObj = getWorkStageByStageCode("test_f");
					break;
				case emProjectOperationalState.PROJECT_STATE_G:
					tmpWorkStageObj = getWorkStageByStageCode("test_g");
					break;
				case emProjectOperationalState.PROJECT_STATE_H:
					tmpWorkStageObj = getWorkStageByStageCode("test_h");
					break;

				case emProjectOperationalState.PROJECT_STATE_I:
					tmpWorkStageObj = getWorkStageByStageCode("dev_i");
					break;
				case emProjectOperationalState.PROJECT_STATE_J:
					tmpWorkStageObj = getWorkStageByStageCode("test_j");
					break;
				case emProjectOperationalState.PROJECT_STATE_K:
					tmpWorkStageObj = getWorkStageByStageCode("dev_k");
					break;
				case emProjectOperationalState.PROJECT_STATE_L:
					tmpWorkStageObj = getWorkStageByStageCode("test_l");
					break;
				case emProjectOperationalState.PROJECT_STATE_M:
					tmpWorkStageObj = getWorkStageByStageCode("dev_m");
					break;
				case emProjectOperationalState.PROJECT_STATE_N:
					tmpWorkStageObj = getWorkStageByStageCode("test_n");
					break;
				case emProjectOperationalState.PROJECT_STATE_P:
					tmpWorkStageObj = getWorkStageByStageCode("dev_p");
					break;
				case emProjectOperationalState.PROJECT_STATE_Q:
					tmpWorkStageObj = getWorkStageByStageCode("test_q");
					break;
				case emProjectOperationalState.PROJECT_STATE_R:
					tmpWorkStageObj = getWorkStageByStageCode("dev_r");
					break;
				case emProjectOperationalState.PROJECT_STATE_T:
					tmpWorkStageObj = getWorkStageByStageCode("test_t");
					break;

				case emProjectOperationalState.PROJECT_STATE_IN_HANDOVER:
				case emProjectOperationalState.PROJECT_STATE_HANDOVER_COMPLETED:
				case emProjectOperationalState.PROJECT_STATE_INSTALL_NO_LOCATION:
				case emProjectOperationalState.PROJECT_STATE_PRE_INSTALLING:
				case emProjectOperationalState.PROJECT_STATE_INSTALLING:
				case emProjectOperationalState.PROJECT_STATE_INSTALLED_OK:
				case emProjectOperationalState.PROJECT_STATE_INSTALLED_FAIL:
				case emProjectOperationalState.PROJECT_STATE_COMPLETED:
				case emProjectOperationalState.PROJECT_STATE_CANCELLED:
					break;
			}
			return tmpWorkStageObj;
		}

		#region Project Footprint and Recycle Methods

		public int getMemoryRequirement()
		{
			int memory_require = project_subnode_prjdata.GetIntAttribute("target_mem_requirement", 0);
			return memory_require;
		}

		public int getDiskRequirement()
		{
			int disk_require = project_subnode_prjdata.GetIntAttribute("target_disk_requirement", 0);
			return disk_require;
		}

		public int getRecycleRequestCount()
		{
			int recycle_request_count = 0;
			if (project_subnode_prjdata != null)
			{
				recycle_request_count = project_subnode_prjdata.GetIntAttribute("recycle_request_count", 0);
			}
			return recycle_request_count;
		}

		public int getRecycleImproveAmount()
		{
			int recycle_improve_amount = 0;
			if (project_subnode_prjdata != null)
			{
				recycle_improve_amount = project_subnode_prjdata.GetIntAttribute("recycle_improve", 0);
			}
			return recycle_improve_amount;
		}

		public int getRecycleProcessedCount()
		{
			int recycle_processed_count = 0;

			if (project_subnode_prjdata != null)
			{
				recycle_processed_count = project_subnode_prjdata.GetIntAttribute("recycle_processed_count", 0);
			}
			return recycle_processed_count;
		}

		public bool getRecycleRequestPendingStatus()
		{
			bool recycle_request = project_subnode_prjdata.GetBooleanAttribute("recycle_request_pending", false);
			return recycle_request;
		}

		#endregion Project Footprint and Recycle Methods

		#region Staff Methods

		public void getProjectStaffLimits(out int DesignLimit, out int BuildLimit, out int TestLimit)
		{
			DesignLimit = this.project_node.GetIntAttribute("design_reslevel", 0);
			BuildLimit = this.project_node.GetIntAttribute("build_reslevel", 0);
			TestLimit = this.project_node.GetIntAttribute("test_reslevel", 0);
		}

		public void getRequestedResourceLevelsForStage(emProjectOperationalState dispState,
			out int requested_int, out int requested_ext, out bool isInProgress, out bool isCompleted)
		{
			requested_int = 0;
			requested_ext = 0;
			isCompleted = false;
			isInProgress = false;

			work_stage ws = this.getWorkStage(dispState);
			ws.getRequestedResourceLevels(out requested_int, out requested_ext, out isInProgress, out isCompleted);
		}

		public void getStaffCountForAllocatedStaff(out int count_int_staff, out int count_ext_staff)
		{
			count_int_staff = 0;
			count_ext_staff = 0;

			determinecurrentstate();
			work_stage ws = this.getWorkStage(currentState);
			
			ws.getStaffCountForAllocatedStaff(out count_int_staff, out count_ext_staff);
		}

		#endregion Staff Methods

		#region Descope Methods

		public bool CanProjectBeDeScoped_ByPercent(int current_round,  out string ErrMsg)
		{
			Boolean DeScopeAllowed = true;
			ErrMsg = string.Empty;

			determinecurrentstate();

			//Check if we have a defined project
			if (((int)this.currentState == (int)emProjectOperationalState.PROJECT_STATE_EMPTY) & (DeScopeAllowed))
			{
				DeScopeAllowed = false;
				ErrMsg = "Project Not Defined";
			}
			//Are we a regulation project
			if ((isRegulationProject()) & (DeScopeAllowed))
			{
				DeScopeAllowed = false;
				ErrMsg = "Regulation Projects do not allow dropping of tasks";
			}

			//Round 3 Not implemented
			if ((current_round > 2) & (DeScopeAllowed))
			{
				DeScopeAllowed = false;
				ErrMsg = "Percentage based Drop Tasks not Allowed in Race " + current_round;
			}

			if (DeScopeAllowed)
			{
				// We dont support iterative work (Recycle aka Rebuild
				// whenever we put it back, we need to update this check 
				// Basically while in recycle there is no descope
			}

			if (DeScopeAllowed)
			{
				//Are we too late in the game for descope
				//				if ((State == emGameOperationalState.GAME_STATE_TESTING_B)|
				//					(State == emGameOperationalState.GAME_STATE_TESTING_C)|
				//					(State == emGameOperationalState.GAME_STATE_TESTED)|

				if (((int)this.currentState == (int)emProjectOperationalState.PROJECT_STATE_IN_HANDOVER) |
					((int)this.currentState == (int)emProjectOperationalState.PROJECT_STATE_HANDOVER_COMPLETED) |
					((int)this.currentState == (int)emProjectOperationalState.PROJECT_STATE_INSTALL_NO_LOCATION) |
					((int)this.currentState == (int)emProjectOperationalState.PROJECT_STATE_PRE_INSTALLING) |
					((int)this.currentState == (int)emProjectOperationalState.PROJECT_STATE_INSTALLING) |
					((int)this.currentState == (int)emProjectOperationalState.PROJECT_STATE_INSTALLED_OK) |
					((int)this.currentState == (int)emProjectOperationalState.PROJECT_STATE_INSTALLED_FAIL) |
					((int)this.currentState == (int)emProjectOperationalState.PROJECT_STATE_COMPLETED) |
					((int)this.currentState == (int)emProjectOperationalState.PROJECT_STATE_CANCELLED))
				{
					DeScopeAllowed = false;
					ErrMsg = "Too late to drop tasks (Only Core Activities Remain)";
				}
			}

			//Check how much descope has been applied allready
			//is there a future stage with more than 1 active task left
			if ((this.getProjectScope() == 10) & (DeScopeAllowed))
			{
				DeScopeAllowed = false;
				ErrMsg = "No More Drop Tasks Possible (Only Core Activities Remain)";
			}
			//Entire Project Single Track Core Activities Only,  No Descope Allowed
			//			if ((getScopeMaxLevel() == 1)&(DeScopeAllowed))
			//			{
			//				DeScopeAllowed = false;
			//				ErrMsg = "No DeScoping Allowed (Only Core Activities Defined)";
			//			}
			return DeScopeAllowed;
		}

		public bool CanProjectBeDeScoped_ByCritPath(out string ErrMsg)
		{
			Boolean DeScopeAllowed = true;
			ErrMsg = string.Empty;

			determinecurrentstate();

			//=======================================================
			//==Check if we have a defined project
			//=======================================================
			if (((int)this.currentState == (int)emProjectOperationalState.PROJECT_STATE_EMPTY) & (DeScopeAllowed))
			{
				DeScopeAllowed = false;
				ErrMsg = "Project Not Defined";
			}

			//=======================================================
			//==Have we passed the stages that can be descoped
			//=======================================================
			if (DeScopeAllowed)
			{
				if (((int)this.currentState == (int)emProjectOperationalState.PROJECT_STATE_IN_HANDOVER) |
					((int)this.currentState == (int)emProjectOperationalState.PROJECT_STATE_HANDOVER_COMPLETED) |
					((int)this.currentState == (int)emProjectOperationalState.PROJECT_STATE_INSTALL_NO_LOCATION) |
					((int)this.currentState == (int)emProjectOperationalState.PROJECT_STATE_PRE_INSTALLING) |
					((int)this.currentState == (int)emProjectOperationalState.PROJECT_STATE_INSTALLING) |
					((int)this.currentState == (int)emProjectOperationalState.PROJECT_STATE_INSTALLED_OK) |
					((int)this.currentState == (int)emProjectOperationalState.PROJECT_STATE_INSTALLED_FAIL) |
					((int)this.currentState == (int)emProjectOperationalState.PROJECT_STATE_COMPLETED) |
					((int)this.currentState == (int)emProjectOperationalState.PROJECT_STATE_CANCELLED))
				{
					DeScopeAllowed = false;
					ErrMsg = "Too late to drop tasks (Only Core Activities Remain)";
				}
			}

			//=================================================================
			//==Are we in a recycle stage (I,J,K,L,M,N,P,Q,R,T) and (H) as well 
			//=================================================================
			if (DeScopeAllowed)
			{
				if (this.isCurrentStateOrLater(emProjectOperationalState.PROJECT_STATE_H))
				{
					DeScopeAllowed = false;
					ErrMsg = "Too late to drop tasks";
				}
			}
			return DeScopeAllowed;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="state"></param>
		/// <param name="sub_task_name"></param>
		/// <param name="exists"></param>
		/// <param name="allowed"></param>
		/// <param name="dropped"></param>
		public void getSubTaskStateForCritPath(emProjectOperationalState state, string sub_task_name,
			int current_round, out bool completed, out bool exists, out bool droppable, out bool dropped)
		{
			string ErrMsg = "";

			completed = false;
			exists = false;
			droppable = false;
			dropped = false;

			//run a generic check on whether we can descope
			if (this.CanProjectBeDeScoped_ByCritPath(out ErrMsg))
			{
				if (state == emProjectOperationalState.PROJECT_STATE_B)
				{
					if (this.isCurrentStateOrLater(emProjectOperationalState.PROJECT_STATE_B))
					{
						completed = true;
					}
					work_stage ws = this.getWorkStage(emProjectOperationalState.PROJECT_STATE_B);
					if (ws != null)
					{
						ws.getDescopeCritPathStatus(sub_task_name, out exists, out droppable, out dropped);
					}
		    }
		    else
		    {
		      if (state == emProjectOperationalState.PROJECT_STATE_D)
		      {
						if (this.isCurrentStateOrLater(emProjectOperationalState.PROJECT_STATE_D))
						{
							completed = true;
						}
						work_stage ws = this.getWorkStage(emProjectOperationalState.PROJECT_STATE_D);
						if (ws != null)
						{
							ws.getDescopeCritPathStatus(sub_task_name, out exists, out droppable, out dropped);
						}
		      }				
		    }
		  }
		}

		#endregion Descope Methods

		#region Duration Change Methods

		public string getDurationDisplayReason()
		{
			string str = this.project_subnode_prjdata.GetAttribute("next_stage_duration_display_reason");
			return str;
		}

		#endregion Duration Change Methods

	}
}







