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
	public class ProjectRunner : ProjectReader
	{

		public ProjectRunner(Node prjnode)
			: base(prjnode)
		{
		}

		#region Status Request Methods

		public void AddCancelFlag()
		{
			project_node.SetAttribute("user_cancel","true");
		}

		public void setStatusRequest_Empty()
		{
			this.project_node.SetAttribute("status_request", "");
		}

		public void setStatusRequest_PreCancel(out bool killable)
		{
			killable = false;

			if (this.isCurrentState(emProjectOperationalState.PROJECT_STATE_PRODUCTSELECTED))
			{
				killable = true;
			}
			else
			{
				this.project_node.SetAttribute("status_request", "precancel");
			}
		}

		public void setStatusRequest_PrePause()
		{
			this.project_node.SetAttribute("status_request", "prepause");
		}

		public void setStatusRequest_PreResume()
		{
			this.project_node.SetAttribute("status_request", "preresume");
		}

		public void setPaused(bool setpaused)
		{
			if (setpaused)
			{
				this.project_node.SetAttribute("status", "paused");
			}
			else
			{
				this.project_node.SetAttribute("status", "running");
			}
		}
		#endregion Status Request Methods

		#region Money (Budget And Expenditure) Methods

		public void SetGoodMoneyFlag(bool canPayCosts, int funding_gap)
		{
			project_subnode_findata.SetAttribute("solvent", canPayCosts);
			project_subnode_findata.SetAttribute("funding_gap", CONVERT.ToStr(funding_gap));
		}

		public void setFundingGap(int gap)
		{
			project_subnode_findata.SetAttribute("funding_gap", CONVERT.ToStr(gap));
		}

		public void setPlayerDefinedBudget(int day, int newbudget)
		{
			int old_budget = project_subnode_findata.GetIntAttribute("budget_player", 0);
			int diff = newbudget - old_budget;
			project_subnode_findata.SetAttribute("budget_player", CONVERT.ToStr(newbudget));
		}

		public void alterSpend(int day, int amount)
		{
			int old_spend = project_subnode_findata.GetIntAttribute("spend", 0);
			int new_spend = old_spend + amount;
			project_subnode_findata.SetAttribute("spend", CONVERT.ToStr(new_spend));
		}

		#endregion Money (Budget And Expenditure) Methods

		#region Gain And Scope Methods

		public int CalculateExpertsEffectIfRequired()
		{
			int experts_effect = 100;		//Normally the experts provide a perfect contribution
			int experts_shortfall = 0;	//The number of skill points that we didn't satisfy
			int expert_effect_per_point = 5; //Each dropped expert skill point 
			Hashtable skill_requirements = new Hashtable();

			Node expertsNode = this.MyNodeTree.GetNamedNode("experts");
			if (expertsNode != null)
			{
				bool enabled = expertsNode.GetBooleanAttribute("enabled", false);
				if (enabled)
				{
					//Get the Expert Effect from the skin 
					expert_effect_per_point = SkinningDefs.TheInstance.GetIntData("expert_effect_per_point", 0);

					//Extract the skill requirement from the project sub node
					ArrayList al = this.project_subnode_prjdata.AttributesAsArrayList;
					foreach (AttributeValuePair avp in al)
					{
						string avp_name = avp.Attribute;
						string avp_value = avp.Value;

						if (avp_name.ToLower().IndexOf("expert_sk")>-1)
						{
							avp_name = avp_name.Replace("expert_sk_","");
							avp_name = avp_name.Replace("_"," ");
							skill_requirements.Add(avp_name, avp_value);
						}
					}
					//Now see if we managed to met the requirements 
					if (skill_requirements.Count > 0)
					{
						foreach (string skill_name_required in skill_requirements.Keys)
						{
							string skill_level_required_str = (string)skill_requirements[skill_name_required];
							int skill_level_required = CONVERT.ParseIntSafe(skill_level_required_str, 0);
							//Now interate through the experts, counting those who are assigned to this project 
							string project_uid_str = CONVERT.ToStr(this.getProjectUniqueID());

							//Iterate over all the experts 
							foreach (Node expert_node in expertsNode.getChildren())
							{
								string expert_name = expert_node.GetAttribute("expert_name");
								string skill_type = expert_node.GetAttribute("skill_type");
								int skill_level = expert_node.GetIntAttribute("skill_level",0);
								string assigned_project = expert_node.GetAttribute("assigned_project");

								if (project_uid_str == assigned_project)
								{
									if (skill_type.ToLower() == skill_name_required.ToLower())
									{
										skill_level_required = skill_level_required - skill_level;
										//it can't be -ve as we don't give bonus for overqualified experts 
										if (skill_level_required < 0)
										{
											skill_level_required = 0; 
										}
									}
								}
							}
							//Do we have any requirement left after the contributions from assigned experts
							experts_shortfall = experts_shortfall + skill_level_required;
						}
					}
					//Work out the effect of the 
					experts_effect = 100 - (experts_shortfall * expert_effect_per_point);
					if (experts_effect < 0)
					{
						experts_effect = 0;
					}
					setExpertsEffect(experts_effect);
					//System.Diagnostics.Debug.WriteLine("Experts Effect:" + CONVERT.ToStr(experts_effect));
				}
			}
			return experts_effect;
		}

		public void CalculateBenefitAchieved(out int GainAchieved_value, out int CostReductionAchieved_value)
		{ 
			double gain_achieved = 0;
			double cost_reduction_achieved = 0;

			int gain_planned = 0;						//What we should have
			int cost_reduction_planned = 0;
			int total_scope = 0;
			int handover_effectiveness = 0;

			//What we should have got (as a percentage (2 digit int))
			gain_planned = getGainPlanned();
			cost_reduction_planned = getReductionPlanned();

			//What we actualy in achieved (as a percentage (2 digit int))
			total_scope = getProjectScope(); //Work Covered 
			handover_effectiveness = getHandoverEffect();
			int displayed_handover = this.getDisplayedHandoverEffect();

			//turn the integer based number into double factor for the calculation
			//the factor are between 0.0 and 1.0 to avoid having to divide by 100 in the final calculation 
			double scope_factor = ((double)total_scope) / 100;
			double handover_factor = ((double)handover_effectiveness) / 100;
			//double expert_factor = ((double)(CalculateExpertsEffect())) / 100;
			double expert_factor = ((double)(this.getExpertsEffect())) / 100;

			double display_handover = ((double)(displayed_handover)) / 100;

			//What we planned is affected by both the scope and the handover
			//OLD
			//gain_achieved = ((double)gain_planned) * (scope_factor) * (handover_factor) * (expert_factor);
			//cost_reduction_achieved = ((double)cost_reduction_planned) * (scope_factor) * (handover_factor);

			//NEW
			//Using displayed handover which is a combination of handover and experts
			gain_achieved = ((double)gain_planned) * (scope_factor) * (display_handover);
			cost_reduction_achieved = ((double)cost_reduction_planned) * (scope_factor) * (display_handover);

			//string debugstr = "Project " + this.getProjectID().ToString();
			//debugstr += " AchievedGain:" + CONVERT.ToStr((int)(gain_achieved));
			//debugstr += " AchievedCostReduct: " + CONVERT.ToStr((int)(cost_reduction_achieved));
			//System.Diagnostics.Debug.WriteLine(debugstr);
			//System.Diagnostics.Debug.WriteLine("Project "+this.getProjectID().ToString() + " GP:"+gain_planned.ToString()+ " TS:"+total_scope.ToString()+ " AG:"+gain_achieved.ToString());

			GainAchieved_value = (int) gain_achieved;
			CostReductionAchieved_value = (int)cost_reduction_achieved;
		}

		/// <summary>
		/// 
		/// </summary>
		public void DetermineGainAchieved()
		{
			int GainAchieved_value = 0;
			int CostReductionAchieved_value = 0;

			this.determinecurrentstate();

			bool completed_install = this.InState(emProjectOperationalState.PROJECT_STATE_INSTALLED_OK);
			bool completed_project = this.InState(emProjectOperationalState.PROJECT_STATE_COMPLETED);

			if (completed_install | completed_project)
			{
				CalculateBenefitAchieved(out GainAchieved_value, out CostReductionAchieved_value);
			}
			if (project_subnode_prjdata != null)
			{
				project_subnode_prjdata.SetAttribute("achieved_gain", CONVERT.ToStr(GainAchieved_value));
				project_subnode_prjdata.SetAttribute("achieved_cost_reduction", CONVERT.ToStr(CostReductionAchieved_value));
			}
		}

		#endregion Gain And Scope Methods

		#region Project Installation Methods

		public void setInstallDayPredictedNotReady(bool predictedfail)
		{
			this.project_subnode_prjdata.SetAttribute("install_predicted_notready", CONVERT.ToStr(predictedfail).ToLower());
		}

		public void setInstallDayTimeFailure(bool timefail)
		{
//			this.project_node.SetAttribute("stage", 
			this.project_subnode_prjdata.SetAttribute("install_timefailure", CONVERT.ToStr(timefail).ToLower());
		}

		public void SetInstallTooEarly()
		{
			string install_location = this.getInstallLocation();
			this.project_node.SetAttribute("install_too_early", install_location);
		}

		public void ClearInstallTooEarly()
		{
			this.project_node.SetAttribute("install_too_early", "");
		}

		public void setInstallDay(int Day)
		{
			this.project_subnode_prjdata.SetAttribute("install_day", CONVERT.ToStr(Day));
		}

		public void setExpertsEffect(int ee_level)
		{
			project_subnode_prjdata.SetAttribute("experts_effect", CONVERT.ToStr(ee_level));
		}

		public bool SetInstallLocation(string installocation)
		{
			if (installocation != "")
			{
				if (project_subnode_prjdata != null)
				{
					project_subnode_prjdata.SetAttribute("target_location", installocation);

					//we cant jump to the pre-installing, if we have a pending Recycle 
					if (this.getRecycleRequestPendingStatus() == false)
					{
						if (this.isCurrentState(emProjectOperationalState.PROJECT_STATE_INSTALL_NO_LOCATION) |
							(this.isCurrentState(emProjectOperationalState.PROJECT_STATE_INSTALLED_FAIL)))
						{
							this.changeState(emProjectOperationalState.PROJECT_STATE_PRE_INSTALLING);
						}
					}
					else
					{ 
						//the target location should be handled as the recycle state unwinds 
					}
				}
			}
			return true;
		}

		public void SetInstallError(string err)
		{
			project_node.SetAttribute("install_fail_reason", err);
		}

		#endregion Project Installation Methods

		#region Project State Methods 

		public void UpdateRecycleSystem()
		{
			int request_count = this.getRecycleRequestCount();
			int proc_count = this.getRecycleProcessedCount();
			//if (request_count > proc_count)
			//It's simpler than original, if we have a request then set the flag
			//it's cleared when we complete the cycle
			if (request_count > 0)
			{
				project_subnode_prjdata.SetAttribute("recycle_request_pending", true);
			}
			else
			{
				project_subnode_prjdata.SetAttribute("recycle_request_pending", false);
			}
			//	//TODO handle the GO LIVE Day 
		}

		public void clearAllRecycleRequest()
		{
			if (project_subnode_prjdata != null)
			{
				//we set the request to the number of processed and clear the pending flag
				int processed = this.getRecycleProcessedCount();
				project_subnode_prjdata.SetAttribute("recycle_request_count", CONVERT.ToStr(processed));
				//handle the pending flag 
				UpdateRecycleSystem();
			}
		}

		public void clearOneRecycleRequest()
		{
			if (project_subnode_prjdata != null)
			{
				//we set the request to the number of processed and clear the pending flag
				int request = this.getRecycleRequestCount();
				request = request - 1;
				if (request < 0)
				{
					request = 0;
				}
				project_subnode_prjdata.SetAttribute("recycle_request_count", CONVERT.ToStr(request));
				//handle the pending flag 
				UpdateRecycleSystem();
			}
		}

		public void setOneRecycleRequest()
		{
			if (project_subnode_prjdata != null)
			{
				//we set the request to the number of processed and clear the pending flag
				int request = this.getRecycleRequestCount();
				request = request + 1;
				project_subnode_prjdata.SetAttribute("recycle_request_count", CONVERT.ToStr(request));
				//handle the pending flag 
				UpdateRecycleSystem();
			}
		}

		public void addOneRecycleProcessedCount()
		{
			if (project_subnode_prjdata != null)
			{
				int recycle_processed_count = project_subnode_prjdata.GetIntAttribute("recycle_processed_count", 0);
				recycle_processed_count = recycle_processed_count + 1;
				project_subnode_prjdata.SetAttribute("recycle_processed_count", CONVERT.ToStr(recycle_processed_count));
			}
		}

		public void setProjectSlot(int newslot)
		{
			string d1 = CONVERT.ToStr(this.getProjectID())+ "  " + CONVERT.ToStr(this.getProductID()) + "  "+ CONVERT.ToStr(this.getProjectSlot());
			//System.Diagnostics.Debug.WriteLine("SETSLOT ## "+d1+ "---> New Slot " + CONVERT.ToStr(newslot));
			project_node.SetAttribute("slot", CONVERT.ToStr(newslot));
		}

		public void UpdateScopeByRecycleAmount()
		{
			int recycle_improvement_amount = getRecycleImproveAmount();
			int scope = this.getProjectScope();
			scope = scope + recycle_improvement_amount;
			this.project_subnode_prjdata.SetAttribute("scope", CONVERT.ToStr(scope));
		}

		public void setDelayedStartDay(int delay_day)
		{
		  project_node.SetAttribute("project_delayed_start_day", CONVERT.ToStr(delay_day));
		  UpdateDisplayNumbers();
		}

		/// <summary>
		/// Is this stage dependant on Staff 
		/// </summary>
		/// <returns></returns>
		public bool isStaffStage()
		{
			bool usesStaff = false;

			if (this.InState(emProjectOperationalState.PROJECT_STATE_A))
			{
				usesStaff = true;
			}
			if (this.InState(emProjectOperationalState.PROJECT_STATE_B))
			{
				usesStaff = true;
			}
			if (this.InState(emProjectOperationalState.PROJECT_STATE_C))
			{
				usesStaff = true;
			}
			if (this.InState(emProjectOperationalState.PROJECT_STATE_D))
			{
				usesStaff = true;
			}
			if (this.InState(emProjectOperationalState.PROJECT_STATE_E))
			{
				usesStaff = true;
			}
			if (this.InState(emProjectOperationalState.PROJECT_STATE_F))
			{
				usesStaff = true;
			}
			if (this.InState(emProjectOperationalState.PROJECT_STATE_G))
			{
				usesStaff = true;
			}
			if (this.InState(emProjectOperationalState.PROJECT_STATE_H))
			{
				usesStaff = true;
			}

			if (this.InState(emProjectOperationalState.PROJECT_STATE_I))
			{
				usesStaff = true;
			}
			if (this.InState(emProjectOperationalState.PROJECT_STATE_J))
			{
				usesStaff = true;
			}
			if (this.InState(emProjectOperationalState.PROJECT_STATE_K))
			{
				usesStaff = true;
			}
			if (this.InState(emProjectOperationalState.PROJECT_STATE_L))
			{
				usesStaff = true;
			}
			if (this.InState(emProjectOperationalState.PROJECT_STATE_M))
			{
				usesStaff = true;
			}
			if (this.InState(emProjectOperationalState.PROJECT_STATE_N))
			{
				usesStaff = true;
			}
			if (this.InState(emProjectOperationalState.PROJECT_STATE_P))
			{
				usesStaff = true;
			}
			if (this.InState(emProjectOperationalState.PROJECT_STATE_Q))
			{
				usesStaff = true;
			}
			if (this.InState(emProjectOperationalState.PROJECT_STATE_R))
			{
				usesStaff = true;
			}
			if (this.InState(emProjectOperationalState.PROJECT_STATE_T))
			{
				usesStaff = true;
			}
			return usesStaff;
		}

		/// <summary>
		/// Need to create a new work stage for this new recycle stage 
		/// </summary>
		/// <param name="newState"></param>
		private void BuildExtraWorkStages(emProjectOperationalState newState, int currentDay)
		{
			ArrayList attrs = new ArrayList();
			int predict_stage_days=0;
			int total_stage_tasks = 0;
			work_stage ws1 = null;
			work_stage ws2 = null;
			bool added_stages = false;

			Node ws_node = getWorkStagesSubNode();
			string required_state_name = Enum.GetName(typeof(emProjectOperationalState), newState);

			switch (newState)
			{ 
				case emProjectOperationalState.PROJECT_STATE_I: 
					//Build work stage I
					ws1 = new work_stage();
					ws1.loadfromRecycle("I");
					ws1.create_work_stage_node(ws_node, 1, 0, true, out predict_stage_days, out total_stage_tasks);
					this.project_work_stages.Add(ws1);
					//Build work stage J
					ws2 = new work_stage();
					ws2.loadfromRecycle("J");
					ws2.create_work_stage_node(ws_node, 1, 0, true, out predict_stage_days, out total_stage_tasks);
					this.project_work_stages.Add(ws2);
					added_stages = true;
					break;
				case emProjectOperationalState.PROJECT_STATE_K: 
					//Build work stage K
					ws1 = new work_stage();
					ws1.loadfromRecycle("K");
					ws1.create_work_stage_node(ws_node, 1, 0, true, out predict_stage_days, out total_stage_tasks);
					this.project_work_stages.Add(ws1);
					//Build work stage L
					ws2 = new work_stage();
					ws2.loadfromRecycle("L");
					ws2.create_work_stage_node(ws_node, 1, 0, true, out predict_stage_days, out total_stage_tasks);
					this.project_work_stages.Add(ws2);
					added_stages = true;
					break;
				case emProjectOperationalState.PROJECT_STATE_M: 
					//Build work stage M
					ws1 = new work_stage();
					ws1.loadfromRecycle("M");
					ws1.create_work_stage_node(ws_node, 1, 0, true, out predict_stage_days, out total_stage_tasks);
					this.project_work_stages.Add(ws1);
					//Build work stage N
					ws2 = new work_stage();
					ws2.loadfromRecycle("N");
					ws2.create_work_stage_node(ws_node, 1, 0, true, out predict_stage_days, out total_stage_tasks);
					this.project_work_stages.Add(ws2);
					added_stages = true;
					break;
				case emProjectOperationalState.PROJECT_STATE_P: 
					//Build work stage P
					ws1 = new work_stage();
					ws1.loadfromRecycle("P");
					ws1.create_work_stage_node(ws_node, 1, 0, true, out predict_stage_days, out total_stage_tasks);
					this.project_work_stages.Add(ws1);
					//Build work stage Q
					ws2 = new work_stage();
					ws2.loadfromRecycle("Q");
					ws2.create_work_stage_node(ws_node, 1, 0, true, out predict_stage_days, out total_stage_tasks);
					this.project_work_stages.Add(ws2);
					added_stages = true;
					break;
				case emProjectOperationalState.PROJECT_STATE_R: 
					//Build work stage R
					ws1 = new work_stage();
					ws1.loadfromRecycle("R");
					ws1.create_work_stage_node(ws_node, 1, 0, true, out predict_stage_days, out total_stage_tasks);
					this.project_work_stages.Add(ws1);
					//Build work stage T
					ws2 = new work_stage();
					ws2.loadfromRecycle("T");
					ws2.create_work_stage_node(ws_node, 1, 0, true, out predict_stage_days, out total_stage_tasks);
					this.project_work_stages.Add(ws2);
					added_stages = true;
					break;
			}
			//===============================================================
			//===============================================================
			if (added_stages)
			{
				int psc = project_subnode_prjdata.GetIntAttribute("recycle_stages_changed", 0);
				project_subnode_prjdata.SetAttribute("recycle_stages_changed", CONVERT.ToStr(psc + 1));
			}
			this.UpdateDaysToLive(currentDay, false);
		}

		/// <summary>
		/// We need to record the Change of State in the project node
		/// In some states, we also need to record the install location as this is needed for the OpsGanttChart
		/// </summary>
		/// <param name="newState"></param>
		private void changeState(emProjectOperationalState newState)
		{
			currentState = newState;
			string state_name = Enum.GetName(typeof(emProjectOperationalState), newState);
			//project_node.SetAttribute("state_name", state_name);

			ArrayList attributes = new ArrayList();
			attributes.Add(new AttributeValuePair("state_name", state_name));

			if (
				(newState == emProjectOperationalState.PROJECT_STATE_INSTALLING) |
				(newState == emProjectOperationalState.PROJECT_STATE_INSTALLED_FAIL) |
				(newState == emProjectOperationalState.PROJECT_STATE_INSTALLED_OK) |
				(newState == emProjectOperationalState.PROJECT_STATE_COMPLETED))
			{
				attributes.Add(new AttributeValuePair("target", this.getInstallLocation()));
			} 
			project_node.SetAttributes(attributes);

			//if this state has a work stage then we need to mark it as inprogress
			work_stage ws = getWorkStage(newState);
			if (ws != null)
			{
				ws.changeStateToInProgress();
			}
			if (newState == emProjectOperationalState.PROJECT_STATE_INSTALLED_OK)
			{
				project_node.SetAttribute("status", "done");
			}
			System.Diagnostics.Debug.WriteLine("CHANGE STATE " + project_id_str + " [" + state_name + "]");
		}

		private void changeStateWithCB(emProjectOperationalState newState, int currentDay)
		{
			currentState = newState;
			string state_name = Enum.GetName(typeof(emProjectOperationalState), newState);
			project_node.SetAttribute("state_name", state_name);

			bool curveball_inserted = false;

			//if this state has a work stage then we need to mark it as inprogress
			work_stage ws = getWorkStage(newState);
			if (ws != null)
			{
				if (ws.isCurveBallDefined())
				{
					curveball_inserted = ws.perform_CurveBall_if_Needed();
					if (curveball_inserted)
					{
						this.UpdateDaysToLive(currentDay, false);
					}
				}
				ws.changeStateToInProgress();
			}
			if (newState == emProjectOperationalState.PROJECT_STATE_INSTALLED_OK)
			{
				project_node.SetAttribute("status", "done");
			}
			System.Diagnostics.Debug.WriteLine("CHANGE STATE " + project_id_str + " [" + state_name + "]");
		}

		public bool isStateCompleted(int currentday)
		{
		  bool allowed_to_proceed = false;
		  work_stage tmpWorkStageObj = null;

		  determinecurrentstate();

		  string state_name = Enum.GetName(typeof(emProjectOperationalState), currentState);
		  switch (currentState)
		  {
		    case emProjectOperationalState.PROJECT_STATE_EMPTY:
		    case emProjectOperationalState.PROJECT_STATE_PROJECTSELECTED:
		    case emProjectOperationalState.PROJECT_STATE_PRODUCTSELECTED:

					int start_day_absolute = this.getDelayedStartDay();

					if ((currentday) >= (start_day_absolute))
					{
					  allowed_to_proceed = true;
					}

		      break;
		    case emProjectOperationalState.PROJECT_STATE_A:
		      tmpWorkStageObj = getWorkStageByStageCode("dev_a");
		      allowed_to_proceed = tmpWorkStageObj.isStageComplete();
		      break;
		    case emProjectOperationalState.PROJECT_STATE_B:
		      tmpWorkStageObj = getWorkStageByStageCode("dev_b");
		      allowed_to_proceed = tmpWorkStageObj.isStageComplete();
		      break;
		    case emProjectOperationalState.PROJECT_STATE_C:
		      tmpWorkStageObj = getWorkStageByStageCode("dev_c");
		      allowed_to_proceed = tmpWorkStageObj.isStageComplete();
		      break;
		    case emProjectOperationalState.PROJECT_STATE_D:
		      tmpWorkStageObj = getWorkStageByStageCode("dev_d");
		      allowed_to_proceed = tmpWorkStageObj.isStageComplete();
		      break;
		    case emProjectOperationalState.PROJECT_STATE_E:
		      tmpWorkStageObj = getWorkStageByStageCode("dev_e");
		      allowed_to_proceed = tmpWorkStageObj.isStageComplete();
		      break;
		    case emProjectOperationalState.PROJECT_STATE_F:
		      tmpWorkStageObj = getWorkStageByStageCode("test_f");
		      allowed_to_proceed = tmpWorkStageObj.isStageComplete();
		      break;
		    case emProjectOperationalState.PROJECT_STATE_G:
		      tmpWorkStageObj = getWorkStageByStageCode("test_g");
		      allowed_to_proceed = tmpWorkStageObj.isStageComplete();
		      break;
		    case emProjectOperationalState.PROJECT_STATE_H:
		      tmpWorkStageObj = getWorkStageByStageCode("test_h");
		      allowed_to_proceed = tmpWorkStageObj.isStageComplete();
		      break;
		    case emProjectOperationalState.PROJECT_STATE_IN_HANDOVER:
		      allowed_to_proceed = true;
		      break;

				case emProjectOperationalState.PROJECT_STATE_I:
					tmpWorkStageObj = getWorkStageByStageCode("dev_i");
					allowed_to_proceed = tmpWorkStageObj.isStageComplete();
					break;
				case emProjectOperationalState.PROJECT_STATE_J:
					tmpWorkStageObj = getWorkStageByStageCode("test_j");
					allowed_to_proceed = tmpWorkStageObj.isStageComplete();
					break;
				case emProjectOperationalState.PROJECT_STATE_K:
					tmpWorkStageObj = getWorkStageByStageCode("dev_k");
					allowed_to_proceed = tmpWorkStageObj.isStageComplete();
					break;
				case emProjectOperationalState.PROJECT_STATE_L:
					tmpWorkStageObj = getWorkStageByStageCode("test_l");
					allowed_to_proceed = tmpWorkStageObj.isStageComplete();
					break;
				case emProjectOperationalState.PROJECT_STATE_M:
					tmpWorkStageObj = getWorkStageByStageCode("dev_m");
					allowed_to_proceed = tmpWorkStageObj.isStageComplete();
					break;
				case emProjectOperationalState.PROJECT_STATE_N:
					tmpWorkStageObj = getWorkStageByStageCode("test_n");
					allowed_to_proceed = tmpWorkStageObj.isStageComplete();
					break;
				case emProjectOperationalState.PROJECT_STATE_P:
					tmpWorkStageObj = getWorkStageByStageCode("dev_p");
					allowed_to_proceed = tmpWorkStageObj.isStageComplete();
					break;
				case emProjectOperationalState.PROJECT_STATE_Q:
					tmpWorkStageObj = getWorkStageByStageCode("test_q");
					allowed_to_proceed = tmpWorkStageObj.isStageComplete();
					break;
				case emProjectOperationalState.PROJECT_STATE_R:
					tmpWorkStageObj = getWorkStageByStageCode("dev_r");
					allowed_to_proceed = tmpWorkStageObj.isStageComplete();
					break;
				case emProjectOperationalState.PROJECT_STATE_T:
					tmpWorkStageObj = getWorkStageByStageCode("test_t");
					allowed_to_proceed = tmpWorkStageObj.isStageComplete();
					break;

		    case emProjectOperationalState.PROJECT_STATE_HANDOVER_COMPLETED:
		      //if (this.getInstallLocation()!="")
		      //{
		      //	allowed_to_proceed = true;
		      //}
		      allowed_to_proceed = true;
		      break;
		    case emProjectOperationalState.PROJECT_STATE_INSTALL_NO_LOCATION:
					if (this.getRecycleRequestPendingStatus())
					{
						allowed_to_proceed = true;
					}
					else
					{
						allowed_to_proceed = false;
					}
		      break;
		    case emProjectOperationalState.PROJECT_STATE_PRE_INSTALLING:
		      allowed_to_proceed = true;
		      break;
		    case emProjectOperationalState.PROJECT_STATE_INSTALLING:
		      //we need to block if there is a ops days here or some other project is blocking
		      //we no longer block on any other projects (multiple projects are allowed to install on the same day)
		      Node ops_node = this.MyNodeTree.GetNamedNode("ops_worklist");
		      if (ops_node != null)
		      {
		        ////Explanation 
		        ////In the old code we only needed to check for the existance of a block today 
		        ////But this was effectilvly checking a for block on the day after the installing day
		        ////107-X 5,5,2 handover is day 28, installing is day 29 
		        ////But this check is run on day 30 and there is an external block on day 30
		        ////We actually want to check if there was a block on day 29 (which has passed)
		        ////If there was no block on 29 then the install process was completed correctly 

		        ////==OLD CODE============================================================	
		        //// This considered the day after installing and allowed other projects to get in the way
		        //						bool ops_block = ops_node.GetBooleanAttribute("block",false);
		        //						bool prj_block = ops_node.GetBooleanAttribute("project",false);
		        //						prj_block = false;
		        //						if ((ops_block==false) & (prj_block==false))
		        //						{
		        //							allowed_to_proceed = true;
		        //						}
		        //						else
		        //						{
		        //							System.Diagnostics.Debug.WriteLine("## Project"+this.getProjectID()+ "  Blocked (ops:"+ops_block.ToString()+" prj:"+prj_block.ToString()+")");
		        //						}

		        //New code 
		        //we are interested in whether there was a block yesterday 
		        string blockdaylist = ops_node.GetAttribute("blockdaylist");
		        string previousdaystr = CONVERT.ToStr(currentday - 1);

		        bool previous_day_was_blocked = blockdaylist.IndexOf(previousdaystr) > -1;
		        if (previous_day_was_blocked)
		        {
		          //System.Diagnostics.Debug.WriteLine("## Project" + this.getProjectID() + "  Blocked (List:" + blockdaylist + ")");
		        }
		        else
		        {
		          allowed_to_proceed = true;
		        }
		      }
		      break;
		    case emProjectOperationalState.PROJECT_STATE_INSTALLED_OK:
		      allowed_to_proceed = false;
		      break;
		    case emProjectOperationalState.PROJECT_STATE_INSTALLED_FAIL:
		      allowed_to_proceed = false;
		      break;
		    case emProjectOperationalState.PROJECT_STATE_COMPLETED:
		      allowed_to_proceed = false;
		      break;
		    case emProjectOperationalState.PROJECT_STATE_CANCELLED:
		      allowed_to_proceed = false;
		      break;
		  }
		  //System.Diagnostics.Debug.WriteLine("CHECK STATE COMPLETE [" + state_name + "] is " + allowed_to_proceed.ToString());
		  return allowed_to_proceed;
		}

		private void markStateDone(emProjectOperationalState tmpState)
		{
			work_stage tmpWorkStageObj = this.getWorkStage(tmpState);
			if (tmpWorkStageObj != null)
			{
				tmpWorkStageObj.markStateDone();
			}
		}

		public void UpdateRunningBenefits()
		{
			Node running_benefit_node = this.MyNodeTree.GetNamedNode("running_benefit");
			if (running_benefit_node != null)
			{ 
				int current_transactions_gained = 0;
				int current_cost_reduction = 0;
				int current_total_benefit = 0;
				int new_transactions_gained = 0;
				int new_cost_reduction = 0;
				int new_total_benefit = 0;


				int GainAchieved_value = 0;
				int CostReductionAchieved_value = 0;

				current_transactions_gained = running_benefit_node.GetIntAttribute("transactions_gained", 0);
				current_cost_reduction = running_benefit_node.GetIntAttribute("cost_reduction", 0);
				current_total_benefit = running_benefit_node.GetIntAttribute("total_benefit", 0);

				CalculateBenefitAchieved(out GainAchieved_value, out CostReductionAchieved_value);

				//Its 25,000 per gain point (defined as 25 in the skin)
				int money_per_point = SkinningDefs.TheInstance.GetIntData("revenue_money_per_point", 0);
				GainAchieved_value = GainAchieved_value * 1000 * money_per_point;

				new_cost_reduction = current_cost_reduction + CostReductionAchieved_value;
				new_transactions_gained = current_transactions_gained + GainAchieved_value;
				new_total_benefit = current_total_benefit + CostReductionAchieved_value + GainAchieved_value;

				////current_cost_reduction = current_cost_reduction + GainAchieved_value;
				////current_transactions_gained = current_transactions_gained + CostReductionAchieved_value;
				////current_total_benefit = current_total_benefit + current_cost_reduction + current_transactions_gained;
				//System.Diagnostics.Debug.WriteLine("Alter Running Benefit Project" + CONVERT.ToStr(this.getProjectID()));
				//System.Diagnostics.Debug.WriteLine("CostReduction from " + CONVERT.ToStr(current_cost_reduction) + "  To " + CONVERT.ToStr(new_cost_reduction));
				//System.Diagnostics.Debug.WriteLine("TransactionGained from " + CONVERT.ToStr(current_transactions_gained) + "  To " + CONVERT.ToStr(new_transactions_gained));
				//System.Diagnostics.Debug.WriteLine("TotalBenefit from " + CONVERT.ToStr(current_total_benefit) + "  To " + CONVERT.ToStr(new_total_benefit));

				//set the new values 
				running_benefit_node.SetAttribute("transactions_gained", CONVERT.ToStr(new_cost_reduction));
				running_benefit_node.SetAttribute("cost_reduction", CONVERT.ToStr(new_transactions_gained));
				running_benefit_node.SetAttribute("total_benefit", CONVERT.ToStr(new_total_benefit));
			}
		}

		public bool moveToNextState(int currentday, out bool movedstate, out bool handledinstall)
		{
			movedstate = false;
			determinecurrentstate();
			handledinstall = false;

			//if (this.isPaused()==false)
			{
				switch (currentState)
				{
					case emProjectOperationalState.PROJECT_STATE_EMPTY:
						movedstate = false;
						break;
					case emProjectOperationalState.PROJECT_STATE_PROJECTSELECTED:
						movedstate = false;
						break;
					case emProjectOperationalState.PROJECT_STATE_PRODUCTSELECTED:
						movedstate = true;
						applyDurationChange(currentday, emProjectOperationalState.PROJECT_STATE_A);
						DecreaseNextStageDurationChangeDisplay();
						changeStateWithCB(emProjectOperationalState.PROJECT_STATE_A, currentday);
						break;
					case emProjectOperationalState.PROJECT_STATE_A:
						movedstate = true;
						markStateDone(emProjectOperationalState.PROJECT_STATE_A);
						applyDurationChange(currentday, emProjectOperationalState.PROJECT_STATE_B);
						DecreaseNextStageDurationChangeDisplay();
						changeStateWithCB(emProjectOperationalState.PROJECT_STATE_B, currentday);
						break;
					case emProjectOperationalState.PROJECT_STATE_B:
						movedstate = true;
						markStateDone(emProjectOperationalState.PROJECT_STATE_B);
						applyDurationChange(currentday, emProjectOperationalState.PROJECT_STATE_C);
						DecreaseNextStageDurationChangeDisplay();
						changeStateWithCB(emProjectOperationalState.PROJECT_STATE_C, currentday);
						break;
					case emProjectOperationalState.PROJECT_STATE_C:
						movedstate = true;
						markStateDone(emProjectOperationalState.PROJECT_STATE_C);
						applyDurationChange(currentday, emProjectOperationalState.PROJECT_STATE_D);
						DecreaseNextStageDurationChangeDisplay();
						changeStateWithCB(emProjectOperationalState.PROJECT_STATE_D, currentday);
						break;
					case emProjectOperationalState.PROJECT_STATE_D:
						movedstate = true;
						markStateDone(emProjectOperationalState.PROJECT_STATE_D);
						applyDurationChange(currentday, emProjectOperationalState.PROJECT_STATE_E);
						DecreaseNextStageDurationChangeDisplay();
						changeStateWithCB(emProjectOperationalState.PROJECT_STATE_E, currentday);
						break;
					case emProjectOperationalState.PROJECT_STATE_E:
						//ending the dev stages 
						movedstate = true;
						markStateDone(emProjectOperationalState.PROJECT_STATE_E);
						applyDurationChange(currentday, emProjectOperationalState.PROJECT_STATE_F);
						DecreaseNextStageDurationChangeDisplay();
						changeStateWithCB(emProjectOperationalState.PROJECT_STATE_F, currentday);
						break;

					case emProjectOperationalState.PROJECT_STATE_I: //DEV (next J)
						movedstate = true;
						markStateDone(emProjectOperationalState.PROJECT_STATE_I);
						applyDurationChange(currentday, emProjectOperationalState.PROJECT_STATE_J);
						DecreaseNextStageDurationChangeDisplay();
						changeState(emProjectOperationalState.PROJECT_STATE_J);
						break;
					case emProjectOperationalState.PROJECT_STATE_J: //TEST (next install decision)
						movedstate = true;
						markStateDone(emProjectOperationalState.PROJECT_STATE_J);
						DecreaseNextStageDurationChangeDisplay();
						UpdateScopeByRecycleAmount();

						if (this.getRecycleRequestPendingStatus())
						{
							if (this.getRecycleProcessedCount() < 5)
							{
								//Build the new workstages()
								clearOneRecycleRequest();
								switch (this.getRecycleProcessedCount())
								{
									case 0:
										BuildExtraWorkStages(emProjectOperationalState.PROJECT_STATE_I, currentday);
										addOneRecycleProcessedCount();
										changeState(emProjectOperationalState.PROJECT_STATE_I);
										break;
									case 1:
										BuildExtraWorkStages(emProjectOperationalState.PROJECT_STATE_K, currentday);
										addOneRecycleProcessedCount();
										changeState(emProjectOperationalState.PROJECT_STATE_K);
										break;
									case 2:
										BuildExtraWorkStages(emProjectOperationalState.PROJECT_STATE_M, currentday);
										addOneRecycleProcessedCount();
										changeState(emProjectOperationalState.PROJECT_STATE_M);
										break;
									case 3:
										BuildExtraWorkStages(emProjectOperationalState.PROJECT_STATE_P, currentday);
										addOneRecycleProcessedCount();
										changeState(emProjectOperationalState.PROJECT_STATE_P);
										break;
									case 4:
										BuildExtraWorkStages(emProjectOperationalState.PROJECT_STATE_R, currentday);
										addOneRecycleProcessedCount();
										changeState(emProjectOperationalState.PROJECT_STATE_R);
										break;
								}
							}
						}
						else
						{
							if (this.isTargetLocationDefined())
							{
								//System.Diagnostics.Debug.WriteLine("PREDEFINED MOVE TO PRE_INSTALL");
								changeState(emProjectOperationalState.PROJECT_STATE_PRE_INSTALLING);
							}
							else
							{
								//System.Diagnostics.Debug.WriteLine("PREDEFINED MOVE TO PRE_INSTALL");
								changeState(emProjectOperationalState.PROJECT_STATE_INSTALL_NO_LOCATION);
							}
						}
						break;

					case emProjectOperationalState.PROJECT_STATE_K: //DEV (next L)
						movedstate = true;
						markStateDone(emProjectOperationalState.PROJECT_STATE_K);
						applyDurationChange(currentday, emProjectOperationalState.PROJECT_STATE_L);
						DecreaseNextStageDurationChangeDisplay();
						changeState(emProjectOperationalState.PROJECT_STATE_L);
						break;
					case emProjectOperationalState.PROJECT_STATE_L: //TEST (next install decision)
						movedstate = true;
						markStateDone(emProjectOperationalState.PROJECT_STATE_L);
						DecreaseNextStageDurationChangeDisplay();
						UpdateScopeByRecycleAmount();

						if (this.getRecycleRequestPendingStatus())
						{
							if (this.getRecycleProcessedCount() < 5)
							{
								//Build the new workstages()
								clearOneRecycleRequest();
								switch (this.getRecycleProcessedCount())
								{
									case 0:
										BuildExtraWorkStages(emProjectOperationalState.PROJECT_STATE_I, currentday);
										addOneRecycleProcessedCount();
										changeState(emProjectOperationalState.PROJECT_STATE_I);
										break;
									case 1:
										BuildExtraWorkStages(emProjectOperationalState.PROJECT_STATE_K, currentday);
										addOneRecycleProcessedCount();
										changeState(emProjectOperationalState.PROJECT_STATE_K);
										break;
									case 2:
										BuildExtraWorkStages(emProjectOperationalState.PROJECT_STATE_M, currentday);
										addOneRecycleProcessedCount();
										changeState(emProjectOperationalState.PROJECT_STATE_M);
										break;
									case 3:
										BuildExtraWorkStages(emProjectOperationalState.PROJECT_STATE_P, currentday);
										addOneRecycleProcessedCount();
										changeState(emProjectOperationalState.PROJECT_STATE_P);
										break;
									case 4:
										BuildExtraWorkStages(emProjectOperationalState.PROJECT_STATE_R, currentday);
										addOneRecycleProcessedCount();
										changeState(emProjectOperationalState.PROJECT_STATE_R);
										break;
								}
							}
						}
						else
						{
							if (this.isTargetLocationDefined())
							{
								//System.Diagnostics.Debug.WriteLine("PREDEFINED MOVE TO PRE_INSTALL");
								changeState(emProjectOperationalState.PROJECT_STATE_PRE_INSTALLING);
							}
							else
							{
								//System.Diagnostics.Debug.WriteLine("PREDEFINED MOVE TO PRE_INSTALL");
								changeState(emProjectOperationalState.PROJECT_STATE_INSTALL_NO_LOCATION);
							}
						}

						break;
					case emProjectOperationalState.PROJECT_STATE_M: //DEV (next J)
						break;
					case emProjectOperationalState.PROJECT_STATE_N: //TEST (next install decision)
						break;
					case emProjectOperationalState.PROJECT_STATE_P: //DEV (next J)
						break;
					case emProjectOperationalState.PROJECT_STATE_Q: //TEST (next install decision)
						break;
					case emProjectOperationalState.PROJECT_STATE_R: //DEV (next J)
						break;
					case emProjectOperationalState.PROJECT_STATE_T: //TEST (next install decision)
						break;

					case emProjectOperationalState.PROJECT_STATE_F:
						movedstate = true;
						markStateDone(emProjectOperationalState.PROJECT_STATE_F);
						applyDurationChange(currentday, emProjectOperationalState.PROJECT_STATE_G);
						DecreaseNextStageDurationChangeDisplay();
						changeStateWithCB(emProjectOperationalState.PROJECT_STATE_G, currentday);
						break;
					case emProjectOperationalState.PROJECT_STATE_G:
						movedstate = true;
						markStateDone(emProjectOperationalState.PROJECT_STATE_G);
						applyDurationChange(currentday, emProjectOperationalState.PROJECT_STATE_H);
						DecreaseNextStageDurationChangeDisplay();
						changeStateWithCB(emProjectOperationalState.PROJECT_STATE_H, currentday);
						break;
					case emProjectOperationalState.PROJECT_STATE_H:
						movedstate = true;
						markStateDone(emProjectOperationalState.PROJECT_STATE_H);
						DecreaseNextStageDurationChangeDisplay();
						CalculateExpertsEffectIfRequired();
						
						if (this.getRecycleRequestPendingStatus())
						{
							if (this.getRecycleProcessedCount() < 5)
							{
								//Build the new workstages()
								clearOneRecycleRequest();
								switch (this.getRecycleProcessedCount())
								{
									case 0:
										BuildExtraWorkStages(emProjectOperationalState.PROJECT_STATE_I, currentday);
										addOneRecycleProcessedCount();
										changeState(emProjectOperationalState.PROJECT_STATE_I);
										break;
									case 1:
										BuildExtraWorkStages(emProjectOperationalState.PROJECT_STATE_K, currentday);
										addOneRecycleProcessedCount();
										changeState(emProjectOperationalState.PROJECT_STATE_K);
										break;
									case 2:
										BuildExtraWorkStages(emProjectOperationalState.PROJECT_STATE_M, currentday);
										addOneRecycleProcessedCount();
										changeState(emProjectOperationalState.PROJECT_STATE_M);
										break;
									case 3:
										BuildExtraWorkStages(emProjectOperationalState.PROJECT_STATE_P, currentday);
										addOneRecycleProcessedCount();
										changeState(emProjectOperationalState.PROJECT_STATE_P);
										break;
									case 4:
										BuildExtraWorkStages(emProjectOperationalState.PROJECT_STATE_R, currentday);
										addOneRecycleProcessedCount();
										changeState(emProjectOperationalState.PROJECT_STATE_R);
										break;
								}
							}
						}
						else
						{
							if (this.isTargetLocationDefined())
							{
								//System.Diagnostics.Debug.WriteLine("PREDEFINED MOVE TO PRE_INSTALL");
								changeState(emProjectOperationalState.PROJECT_STATE_PRE_INSTALLING);
							}
							else
							{
								//System.Diagnostics.Debug.WriteLine("PREDEFINED MOVE TO PRE_INSTALL");
								changeState(emProjectOperationalState.PROJECT_STATE_INSTALL_NO_LOCATION);
							}
						}
						break;
					case emProjectOperationalState.PROJECT_STATE_IN_HANDOVER:
						movedstate = true;

						if (this.getRecycleRequestPendingStatus())
						{
							if (this.getRecycleProcessedCount() < DataLookup.GameConstants.MAX_RECYCLE)
							{
								//Build the new workstages()
								clearOneRecycleRequest();
								switch (this.getRecycleProcessedCount())
								{
									case 0:
										BuildExtraWorkStages(emProjectOperationalState.PROJECT_STATE_I, currentday);
										changeState(emProjectOperationalState.PROJECT_STATE_I);
										break;
									case 1:
										BuildExtraWorkStages(emProjectOperationalState.PROJECT_STATE_K, currentday);
										changeState(emProjectOperationalState.PROJECT_STATE_K);
										break;
									case 2:
										BuildExtraWorkStages(emProjectOperationalState.PROJECT_STATE_M, currentday);
										changeState(emProjectOperationalState.PROJECT_STATE_M);
										break;
									case 3:
										BuildExtraWorkStages(emProjectOperationalState.PROJECT_STATE_P, currentday);
										changeState(emProjectOperationalState.PROJECT_STATE_P);
										break;
									case 4:
										BuildExtraWorkStages(emProjectOperationalState.PROJECT_STATE_R, currentday);
										changeState(emProjectOperationalState.PROJECT_STATE_R);
										break;
								}
							}
							else
							{
								changeState(emProjectOperationalState.PROJECT_STATE_HANDOVER_COMPLETED);
							}
						}
						else
						{
							changeState(emProjectOperationalState.PROJECT_STATE_HANDOVER_COMPLETED);
						}
						break;

					case emProjectOperationalState.PROJECT_STATE_HANDOVER_COMPLETED:
						movedstate = true;
						changeState(emProjectOperationalState.PROJECT_STATE_INSTALLING);
						break;
					case emProjectOperationalState.PROJECT_STATE_INSTALL_NO_LOCATION:
						//movedstate = true;
						//changeState(emProjectOperationalState.PROJECT_STATE_INSTALLING);

						if (this.getRecycleRequestPendingStatus())
						{
							movedstate = true;
							markStateDone(emProjectOperationalState.PROJECT_STATE_INSTALL_NO_LOCATION);
							DecreaseNextStageDurationChangeDisplay();
							UpdateScopeByRecycleAmount();

							if (this.getRecycleProcessedCount() < 5)
							{
								//Build the new workstages()
								clearOneRecycleRequest();
								switch (this.getRecycleProcessedCount())
								{
									case 0:
										BuildExtraWorkStages(emProjectOperationalState.PROJECT_STATE_I, currentday);
										addOneRecycleProcessedCount();
										changeState(emProjectOperationalState.PROJECT_STATE_I);
										break;
									case 1:
										BuildExtraWorkStages(emProjectOperationalState.PROJECT_STATE_K, currentday);
										addOneRecycleProcessedCount();
										changeState(emProjectOperationalState.PROJECT_STATE_K);
										break;
									case 2:
										BuildExtraWorkStages(emProjectOperationalState.PROJECT_STATE_M, currentday);
										addOneRecycleProcessedCount();
										changeState(emProjectOperationalState.PROJECT_STATE_M);
										break;
									case 3:
										BuildExtraWorkStages(emProjectOperationalState.PROJECT_STATE_P, currentday);
										addOneRecycleProcessedCount();
										changeState(emProjectOperationalState.PROJECT_STATE_P);
										break;
									case 4:
										BuildExtraWorkStages(emProjectOperationalState.PROJECT_STATE_R, currentday);
										addOneRecycleProcessedCount();
										changeState(emProjectOperationalState.PROJECT_STATE_R);
										break;
								}
							}
						}
						else
						{
							if (this.isTargetLocationDefined())
							{
								//System.Diagnostics.Debug.WriteLine("PREDEFINED MOVE TO PRE_INSTALL");
								changeState(emProjectOperationalState.PROJECT_STATE_PRE_INSTALLING);
							}
							else
							{
								//System.Diagnostics.Debug.WriteLine("PREDEFINED MOVE TO PRE_INSTALL");
								changeState(emProjectOperationalState.PROJECT_STATE_INSTALL_NO_LOCATION);
							}
						}










						break;
					case emProjectOperationalState.PROJECT_STATE_PRE_INSTALLING:
						int requested_day = this.getInstallDay();
						if ((requested_day == currentday) | (this.getInstallDayAutoUpdate()))
						{
							movedstate = true;
							changeState(emProjectOperationalState.PROJECT_STATE_INSTALLING);
							handledinstall = true;
						}
						break;
					case emProjectOperationalState.PROJECT_STATE_INSTALLING:
						//Handle the Installation process 

						int tmpProjectid = this.getProjectID();
						int tmpProductid = this.getProductID();
						int tmpPlatformid = this.getPlatformID();
						string desc = this.getProjectDesc();
						int tmpMemRequirement = this.getMemoryRequirement();
						int tmpDiskRequirement = this.getDiskRequirement();
						string target_location = this.getInstallLocation();
						string errmsg = "";

						AppInstaller pi = new AppInstaller(this.MyNodeTree);
						if (pi.install_project(tmpProjectid, tmpProductid, tmpPlatformid, desc,
							tmpMemRequirement, tmpDiskRequirement, target_location, out errmsg))
						{
							this.changeState(emProjectOperationalState.PROJECT_STATE_COMPLETED);
							UpdateRunningBenefits();
							this.UpdateDaysToLive(currentday, true);
						}
						else
						{
							this.changeState(emProjectOperationalState.PROJECT_STATE_INSTALLED_FAIL);
							this.SetInstallError(errmsg);
							this.UpdateDaysToLive(currentday, false);
						}
						//Mind to block other project from using this day 
						if (singleProjectInstall)
						{
							Node ops_node = this.MyNodeTree.GetNamedNode("ops_worklist");
							if (ops_node != null)
							{
								ops_node.SetAttribute("project", "true");
							}
						}
						break;
					case emProjectOperationalState.PROJECT_STATE_INSTALLED_OK:
						movedstate = false;
						break;
					case emProjectOperationalState.PROJECT_STATE_INSTALLED_FAIL:
						movedstate = false;
						break;
					case emProjectOperationalState.PROJECT_STATE_COMPLETED:
						movedstate = false;
						break;
					case emProjectOperationalState.PROJECT_STATE_CANCELLED:
						movedstate = false;
						break;
				}
			}
			return movedstate;
		}

		public void recalculateGoLiveAfterRecall(int day)
		{
			determinecurrentstate();
			work_stage ws = this.getWorkStage(this.currentState);
			if (ws != null)
			{
				int predicted_days = 0;
				//Recalculate the predicted_days 
				ws.RecalculatePredictedDays(true, true, out predicted_days);
			}
			UpdateDaysToLive(day, false);
		}

		#endregion Project State Methods

		#region Duration Change Methods

		public void handleRequests(out bool cancell_this_project)
		{
			cancell_this_project = false;

			if (this.isStatusRequestPrePause())
			{
				this.setPaused(true);
				this.setStatusRequest_Empty();
			}
			if (this.isStatusRequestPreResume())
			{
				this.setPaused(false);
				this.setStatusRequest_Empty();
			}
			if (this.isStatusRequestPreCancel())
			{
				cancell_this_project = true;
				this.setStatusRequest_Empty();
			}
		}

		//The display of the duration change 
		public bool DecreaseNextStageDurationChangeDisplay()
		{
			int dc = this.project_subnode_prjdata.GetIntAttribute("next_stage_duration_display_count", -1);
			if (dc == 2)
			{
				this.project_subnode_prjdata.SetAttribute("next_stage_duration_display_count", CONVERT.ToStr(dc - 1));
			}
			else
			{
				if (dc == 1)
				{
					this.project_subnode_prjdata.SetAttribute("next_stage_duration_display_count", CONVERT.ToStr(0));
					this.project_subnode_prjdata.SetAttribute("next_stage_duration_display_reason", "");
				}
			}
			return true;
		}

		//This methods sets the intentions to change the next Stage Duration
		//We can't do it now because it should display at the start of the next stage
		//The days are culmulative 
		public bool ClearNextStageDurationChange()
		{
			project_node.SetAttribute("next_stage_duration_change_total", "0");
			project_node.SetAttribute("next_stage_duration_change_reason", "");
			return true;
		}

		//This methods sets the intentions to change the next Stage Duration
		//We can't do it now because it should display at the start of the next stage
		//The days are culmulative 
		public bool SetNextStageDurationChange(int days_change, string reason)
		{
			bool OpSuccess = false;

			//Check that we are in a stage that allows a future duration change 
			bool stage_defined = this.InState(emProjectOperationalState.PROJECT_STATE_PRODUCTSELECTED);
			bool stage_a = this.InState(emProjectOperationalState.PROJECT_STATE_A);
			bool stage_b = this.InState(emProjectOperationalState.PROJECT_STATE_B);
			bool stage_c = this.InState(emProjectOperationalState.PROJECT_STATE_C);
			bool stage_d = this.InState(emProjectOperationalState.PROJECT_STATE_D);
			bool stage_e = this.InState(emProjectOperationalState.PROJECT_STATE_E);
			bool stage_f = this.InState(emProjectOperationalState.PROJECT_STATE_F);
			bool stage_g = this.InState(emProjectOperationalState.PROJECT_STATE_G);
			bool stage_h = false; //No Further Stgaes to alter (can't alter a current Stage)

			if ((stage_defined) | (stage_a) | (stage_b) | (stage_c) | (stage_d) | (stage_e) | (stage_f) | (stage_g) | (stage_h))
			{
				int current_pending_duration_change = project_node.GetIntAttribute("next_stage_duration_change_total", 0);
				string current_pending_duration_reason = project_node.GetAttribute("next_stage_duration_change_reason");

				if (days_change != 0)
				{
					current_pending_duration_change += days_change;
					current_pending_duration_reason = reason;
					project_node.SetAttribute("next_stage_duration_change_total", CONVERT.ToStr(current_pending_duration_change));
					project_node.SetAttribute("next_stage_duration_change_reason", current_pending_duration_reason);

					this.project_subnode_prjdata.SetAttribute("next_stage_duration_display_count", 2);
					this.project_subnode_prjdata.SetAttribute("next_stage_duration_display_reason", current_pending_duration_reason);
					OpSuccess = true;
				}
			}
			return OpSuccess;
		}

		private void extractTaskDetails(Node stage_node, out int highest_sequence, out int work_todo)
		{
			highest_sequence = 1;   //task sequence number
			work_todo = 8;					//Standard hours for each task
			if (stage_node != null)
			{
				foreach (Node task in stage_node.getChildren())
				{
					string node_type = task.GetAttribute("type");
					string node_status = task.GetAttribute("status");
					bool descoped = task.GetBooleanAttribute("descoped", false);
					int sequence = task.GetIntAttribute("sequence", 0);
					if (node_type.ToLower() == "work_task")
					{
						if (sequence > highest_sequence)
						{
							highest_sequence = sequence;
						}
					}
				}
			}
		}

		private void applyDurationChange(int currentDay, emProjectOperationalState tmpState)
		{
			int current_pending_duration_change = project_node.GetIntAttribute("next_stage_duration_change_total", 0);
			string current_pending_duration_reason = project_node.GetAttribute("next_stage_duration_change_reason");
			bool UpdateProjectPredictedDays = false;

			if (current_pending_duration_change != 0)
			{
				//get 
				work_stage ws = this.getWorkStage(tmpState);
				if (ws != null)
				{
			    ArrayList attrs = new ArrayList();
				
			    bool UseIncidentsAsElapsedDays = false;
			    string UseIncidentsAsElapsedDays_str = SkinningDefs.TheInstance.GetData("use_incidents_as_elapsed_days", "false");
			    if (UseIncidentsAsElapsedDays_str.IndexOf("true") > -1)
			    {
			      UseIncidentsAsElapsedDays = true;
			    }

			    if (UseIncidentsAsElapsedDays)
			    {
			      int requested_int_staff = 0;
			      int requested_ext_staff = 0;
			      bool inprogress = false;
			      bool iscompleted = false;
			      //we scale the number of task indicted by the the number of people to have a bigger impact
			      this.getRequestedResourceLevelsForStage(tmpState, out requested_int_staff, out requested_ext_staff, out inprogress, out iscompleted);
			      current_pending_duration_change = current_pending_duration_change * (requested_int_staff + requested_ext_staff);
			    }
			    if (current_pending_duration_change > 0)
			    {
						ws.CreateNewSubTask(current_pending_duration_change);
			      ClearNextStageDurationChange();
			      UpdateProjectPredictedDays = true;
			    }
			    else
			    {
						int number_mandays_to_drop = Math.Abs(current_pending_duration_change);
						for (int stepp = 0; stepp < number_mandays_to_drop; stepp++)
						{
							ws.DescopeByOneManDay();
						}
						ws.RecalculateStageTaskNumbers();
						int tmp_predict_days = 0;
						ws.RecalculatePredictedDays(true, true, out tmp_predict_days);
						UpdateProjectPredictedDays = true;
			      ClearNextStageDurationChange();
			      UpdateProjectPredictedDays = true;
			    }
			  }
			}
			if (UpdateProjectPredictedDays)
			{
			  UpdateDaysToLive(currentDay, false);
			}
		}

		private int getPredictedStageDuration(emProjectOperationalState tmpState)
		{
			int duration = 0;

			work_stage ws = this.getWorkStage(tmpState);
			duration = ws.getPredictedStageDuration();
			return duration;
		}

		#endregion Duration Change Methods

		#region Time Plan Methods

		public void RemovePlan(int currentday, string plan_name)
		{
			int ps = getProjectSlot();
			// Remove the plan node.
			Node plan = MyNodeTree.GetNamedNode(plan_name);
			if (plan != null)
			{
				plan.Parent.DeleteChildTree(plan);
			}
		}

		public void CaptureTimePlan(int currentday, DayTimeSheet[] time_sheets)
		{

			Hashtable timesheets = new Hashtable();

			//Boolean HandledDay = false;
			int DayCounter = 0;
			//bool tmpFlag1 = false; //just empty to catch unneeded parameter
			//bool tmpFlag2 = false; //just empty to catch unneeded parameter

			//In Single section, we consider everything as Dev staff 
			bool SingleSection = false;

			SingleSection = false;
			string UseSingleStaffSection_str = SkinningDefs.TheInstance.GetData("use_single_staff_section", "false");
			if (UseSingleStaffSection_str.IndexOf("true") > -1)
			{
				SingleSection = true;
			}

			//int ps = this.getProjectSlot();
			//System.Diagnostics.Debug.WriteLine("=====================================================================");
			//string debug = this.getProductID().ToString() + "";
			//System.Diagnostics.Debug.WriteLine(debug);

			//int start_day = this.getProjectSelectionDay();
			int first_work_day = getProjectFirstWorkDay();
			int delayed_start_day = this.getDelayedStartDay();

			//If the project is using the delayed start, the plan is based on that day not the FirstWorkDay
			//The First WorkDay is defined when the node is constructed 
			DayCounter = first_work_day;
			if (delayed_start_day != 0)
			{
				DayCounter = delayed_start_day;
			}
			int day_point = DayCounter;

			//look at 8171
			//We now generated the future plan on demand (whenever we open the resource level displays)
			//This can happen through the round so we need to takle account of the current day in the calculation
			if (currentday > day_point)
			{
				//we have already started and need to start the day count from the current day
				day_point = currentday;
			}

			for (int step = 0; step <= GameConstants.MAX_NUMBER_DAYS; step++)
			{
				timesheets.Add(step, new DayTimeSheet());
			}

			//Get the workstage to do the time based calculations 
			work_stage ws = null;
			
			ws = this.getWorkStage(emProjectOperationalState.PROJECT_STATE_A);
			day_point += ws.GenerateTimeSheetsForStage(day_point, currentday, timesheets);
			ws = this.getWorkStage(emProjectOperationalState.PROJECT_STATE_B);
			day_point += ws.GenerateTimeSheetsForStage(day_point, currentday, timesheets);
			ws = this.getWorkStage(emProjectOperationalState.PROJECT_STATE_C);
			day_point += ws.GenerateTimeSheetsForStage(day_point, currentday, timesheets);
			ws = this.getWorkStage(emProjectOperationalState.PROJECT_STATE_D);
			day_point += ws.GenerateTimeSheetsForStage(day_point, currentday, timesheets);
			ws = this.getWorkStage(emProjectOperationalState.PROJECT_STATE_E);
			day_point += ws.GenerateTimeSheetsForStage(day_point, currentday, timesheets);
			ws = this.getWorkStage(emProjectOperationalState.PROJECT_STATE_F);
			day_point += ws.GenerateTimeSheetsForStage(day_point, currentday, timesheets);
			ws = this.getWorkStage(emProjectOperationalState.PROJECT_STATE_G);
			day_point += ws.GenerateTimeSheetsForStage(day_point, currentday, timesheets);
			ws = this.getWorkStage(emProjectOperationalState.PROJECT_STATE_H);
			day_point += ws.GenerateTimeSheetsForStage(day_point, currentday, timesheets);

			//string sst = "";
			for (int step = DayCounter; step <= GameConstants.MAX_NUMBER_DAYS; step++)
			{
				DayTimeSheet dts = (DayTimeSheet)timesheets[step];

				int IntDevEmployed = Math.Max(dts.staff_int_dev_day_employed_count, dts.staff_int_test_day_employed_count);
				int IntDevIdle = Math.Max(dts.staff_int_dev_day_idle_count,  dts.staff_int_test_day_idle_count);
				int ExtDevEmployed = Math.Max(dts.staff_ext_dev_day_employed_count, dts.staff_ext_test_day_employed_count);
				int ExtDevIdle = Math.Max(dts.staff_ext_dev_day_idle_count,  dts.staff_ext_test_day_idle_count);

				if (SingleSection)
				{
					time_sheets[step].staff_int_dev_day_employed_count += IntDevEmployed;
					time_sheets[step].staff_int_dev_day_idle_count += IntDevIdle;
					time_sheets[step].staff_ext_dev_day_employed_count += ExtDevEmployed;
					time_sheets[step].staff_ext_dev_day_idle_count += ExtDevIdle;

					time_sheets[step].staff_int_test_day_employed_count += 0;
					time_sheets[step].staff_int_test_day_idle_count += 0;
					time_sheets[step].staff_ext_test_day_employed_count += 0;
					time_sheets[step].staff_ext_test_day_idle_count += 0;
				}
				else
				{
					time_sheets[step].staff_int_dev_day_employed_count += dts.staff_int_dev_day_employed_count;
					time_sheets[step].staff_int_dev_day_idle_count += dts.staff_int_dev_day_idle_count;
					time_sheets[step].staff_ext_dev_day_employed_count += dts.staff_ext_dev_day_employed_count;
					time_sheets[step].staff_ext_dev_day_idle_count += dts.staff_ext_dev_day_idle_count;

					time_sheets[step].staff_int_test_day_employed_count += dts.staff_int_test_day_employed_count;
					time_sheets[step].staff_int_test_day_idle_count += dts.staff_int_test_day_idle_count;
					time_sheets[step].staff_ext_test_day_employed_count += dts.staff_ext_test_day_employed_count;
					time_sheets[step].staff_ext_test_day_idle_count += dts.staff_ext_test_day_idle_count;
				}
			}
		}

		public void CapturePlan(int currentday)
		{
			int DurationStageA = 0;
			int DurationStageB = 0;
			int DurationStageC = 0;
			int DurationStageD = 0;
			int DurationStageE = 0;
			int DurationStageF = 0;
			int DurationStageG = 0;
			int DurationStageH = 0;
			Boolean HandledDay = false;
			int DayCounter = 0;
			int LastTestDay = -1;

			int ps = this.getProjectUniqueID();

			// Create a freestanding plan in the network.
			Node plansNode = MyNodeTree.GetNamedNode("project_plans");
			string nodeName = "project" + CONVERT.ToStr(ps) + "_plan";
			string planName = nodeName;

			//remove any existing plan 
			RemovePlan(currentday, planName);

			ArrayList planAttrs = new ArrayList ();
			planAttrs.Add(new AttributeValuePair ("type", "project_plan"));
			planAttrs.Add(new AttributeValuePair ("name", nodeName));
			planAttrs.Add(new AttributeValuePair ("project", "project" + CONVERT.ToStr(ps)));
			Node planNode = new Node (plansNode, "project_plan", nodeName, planAttrs);

			int delayed_start_day = this.getDelayedStartDay();

			DayCounter = currentday;
			if (delayed_start_day > 0)
			{
				DayCounter = delayed_start_day;
			}

			Dictionary<string, int> stageNameToDailyCost = new Dictionary<string, int> ();
			foreach (Node stage in project_subnode_wk_stages.GetChildrenOfType("stage"))
			{
				string stageName = stage.GetAttribute("action");
				stageNameToDailyCost.Add(stageName, (stage.GetIntAttribute("staff_int_requested", 0) * worker_internal_daypayrate)
													+ (stage.GetIntAttribute("staff_ext_requested", 0) * worker_external_daypayrate));
			}

			//Determine the predicted Duration 
			DurationStageA = getPredictedStageDuration(emProjectOperationalState.PROJECT_STATE_A);
			DurationStageB = getPredictedStageDuration(emProjectOperationalState.PROJECT_STATE_B);
			DurationStageC = getPredictedStageDuration(emProjectOperationalState.PROJECT_STATE_C);
			DurationStageD = getPredictedStageDuration(emProjectOperationalState.PROJECT_STATE_D);
			DurationStageE = getPredictedStageDuration(emProjectOperationalState.PROJECT_STATE_E);
			DurationStageF = getPredictedStageDuration(emProjectOperationalState.PROJECT_STATE_F);
			DurationStageG = getPredictedStageDuration(emProjectOperationalState.PROJECT_STATE_G);
			DurationStageH = getPredictedStageDuration(emProjectOperationalState.PROJECT_STATE_H);

			//Now build the plans 
			for (int step = DayCounter; step <= GameConstants.MAX_NUMBER_DAYS; step++)
			{
				HandledDay = false;
				if (DurationStageA > 0)
				{
					DurationStageA--;
					//System.Diagnostics.Debug.WriteLine("Day:" + step.ToString() + " Stage Dev A  ");
					AddPlanNode(planNode, step, emProjectOperationalState.PROJECT_STATE_A, stageNameToDailyCost["A"]);
					HandledDay = true;
				}
				if ((DurationStageB > 0) & (HandledDay == false))
				{
					DurationStageB--;
					//System.Diagnostics.Debug.WriteLine("Day:" + step.ToString() + " Stage Dev B  ");
					AddPlanNode(planNode, step, emProjectOperationalState.PROJECT_STATE_B, stageNameToDailyCost["B"]);
					HandledDay = true;
				}
				if ((DurationStageC > 0) & (HandledDay == false))
				{
					DurationStageC--;
					//System.Diagnostics.Debug.WriteLine("Day:" + step.ToString() + " Stage Dev C  ");
					AddPlanNode(planNode, step, emProjectOperationalState.PROJECT_STATE_C, stageNameToDailyCost["C"]);
					HandledDay = true;
				}
				if ((DurationStageD > 0) & (HandledDay == false))
				{
					DurationStageD--;
					//System.Diagnostics.Debug.WriteLine("Day:" + step.ToString() + " Stage Dev D  ");
					AddPlanNode(planNode, step, emProjectOperationalState.PROJECT_STATE_D, stageNameToDailyCost["D"]);
					HandledDay = true;
				}
				if ((DurationStageE > 0) & (HandledDay == false))
				{
					DurationStageE--;
					//System.Diagnostics.Debug.WriteLine("Day:" + step.ToString() + " Stage Dev E  ");
					AddPlanNode(planNode, step, emProjectOperationalState.PROJECT_STATE_E, stageNameToDailyCost["E"]);
					HandledDay = true;
				}
				if ((DurationStageF > 0) & (HandledDay == false))
				{
					DurationStageF--;
					//System.Diagnostics.Debug.WriteLine("Day:" + step.ToString() + " Stage Test A ");
					AddPlanNode(planNode, step, emProjectOperationalState.PROJECT_STATE_F, stageNameToDailyCost["F"]);
					HandledDay = true;
				}
				if ((DurationStageG > 0) & (HandledDay == false))
				{
					DurationStageG--;
					//System.Diagnostics.Debug.WriteLine("Day:" + step.ToString() + " Stage Test B ");
					AddPlanNode(planNode, step, emProjectOperationalState.PROJECT_STATE_G, stageNameToDailyCost["G"]);
					HandledDay = true;
				}
				if ((DurationStageH > 0) & (HandledDay == false))
				{
					DurationStageH--;
					//System.Diagnostics.Debug.WriteLine("Day:" + step.ToString() + " Stage Test C ");
					AddPlanNode(planNode, step, emProjectOperationalState.PROJECT_STATE_H, stageNameToDailyCost["H"]);
					LastTestDay = step;
					HandledDay = true;
				}
			}

			if (LastTestDay != -1)
			{
				if (((LastTestDay + 1) <= (GameConstants.MAX_NUMBER_DAYS)))
				{
					//System.Diagnostics.Debug.WriteLine(this.getProductID().ToString()+"Day:" + (LastTestDay + 1).ToString() + " Stage Hand ");
					AddPlanNode(planNode, LastTestDay + 1, emProjectOperationalState.PROJECT_STATE_IN_HANDOVER, 0);
				}
				if (((LastTestDay + 2) <= (GameConstants.MAX_NUMBER_DAYS)))
				{
					//System.Diagnostics.Debug.WriteLine(this.getProductID().ToString() + "Day:" + (LastTestDay + 2).ToString() + " Stage Installed ");
					AddPlanNode(planNode, LastTestDay + 2, emProjectOperationalState.PROJECT_STATE_INSTALLED_OK, 0);
				}
				if (((LastTestDay + 3) <= (GameConstants.MAX_NUMBER_DAYS)))
				{
					//System.Diagnostics.Debug.WriteLine(this.getProductID().ToString() + "Day:" + (LastTestDay + 3).ToString() + " Stage Completed ");
					AddPlanNode(planNode, LastTestDay + 3, emProjectOperationalState.PROJECT_STATE_COMPLETED, 0);
				}
			}
		}

		Node AddPlanNode (Node planNode, int day, emProjectOperationalState state, int dailyCost)
		{
			ArrayList attrs = new ArrayList ();

			attrs.Add(new AttributeValuePair ("type", "day_plan"));
			attrs.Add(new AttributeValuePair ("day", CONVERT.ToStr(day)));
			attrs.Add(new AttributeValuePair ("stage", state.ToString()));
			attrs.Add(new AttributeValuePair ("spend", dailyCost));

			return new Node (planNode, "day_plan", "", attrs);
		}

		#endregion Time Plan Methods

		#region Stage Methods

		//public int getTotalTasksForStage(emProjectOperationalState dispState)
		//{
		//  int total_tasks = 0;
		//  work_stage ws = this.getWorkStage(dispState);
		//  total_tasks = ws.getTotalTasks();
		//  return total_tasks;
		//}

		#endregion Stage Methods

		#region Descope Methods

		/// <summary>
		/// iterate all the stage to determine how many tasks are left 
		/// Used in Descope to determine the total dropped tasks 
		/// </summary>
		/// <param name="total_task"></param>
		/// <param name="tasks_dropped"></param>
		private void getTotalTaskNumbers(out int total_task, out int tasks_dropped)
		{
			total_task=0;
			tasks_dropped=0;

			int total_stage_task_count=0;
			int total_stage_task_dropped_count = 0;
			int total_stage_remaining_task_count = 0; 

			foreach (work_stage ws in this.project_work_stages)
			{
				ws.getFullTaskCountsForStage(out total_stage_task_count, out total_stage_task_dropped_count, 
					out total_stage_remaining_task_count);

				total_task += total_stage_task_count;
				tasks_dropped += total_stage_task_dropped_count;
			}
		}

		/// <summary>
		/// We descope by increasing the amount of dropped mandays 
		/// At start, we have a scope of 100% and no dropped man days
		/// Whenever the users selects a new Descope level (the scope level)
		/// We need to check how many dropped mandays we have adn adjust them to match what the user intended.
		/// We may not have enough mandays left to satisfy the amount that needs to dropped. 
		///   as stage cannot be reduced bvelow one manday 
		/// 
		/// Once we have establish how many extra dropped days we need to have 
		/// We buld an array of the stages that can be affected (The ones are still "todo")
		/// then we spin through then 
		/// </summary>
		/// <param name="currentDay"></param>
		/// <param name="project_droptasks_percentage"></param>
		public void DescopeTasks(int currentDay, int requested_project_droptasks_percentage)
		{
			work_stage[] active_stages = new work_stage[8]; //ABCDEFGH -- 8 stages 
			bool performRecalculateofGoLive = false;

			int current_total_manday =0;
			int current_total_mandays_dropped =0;

			for (int step=0; step<8; step++)
			{
				active_stages[step]=null;
			}

			//What is the current level of descope
			int current_descope_level = this.project_subnode_prjdata.GetIntAttribute("scope", 0);

			//System.Diagnostics.Debug.WriteLine("current_descope_level :" + CONVERT.ToStr(current_descope_level));
			//System.Diagnostics.Debug.WriteLine("requested_project_droptasks_percentage :" + CONVERT.ToStr(requested_project_droptasks_percentage));

			//Only do work if we have increased the level of descope 
			if ((current_descope_level - requested_project_droptasks_percentage) > 0)
			{
				//Dtermine how dropped mandays, we should have for this percentage
				int total_project_manday_count = this.project_subnode_prjdata.GetIntAttribute("descope_total_mandays_defined", 0);
				int required_dropped_mandays_for_this_level = (total_project_manday_count * (100-requested_project_droptasks_percentage)) / 100;

				//Get the current state of the project 
				this.getTotalTaskNumbers(out current_total_manday, out current_total_mandays_dropped);

				//Determine how many extra mandays we need to drop to match requirements
				int extra_mandays_to_dropped = required_dropped_mandays_for_this_level - current_total_mandays_dropped;

				//System.Diagnostics.Debug.WriteLine("total_project_manday_count:"+ CONVERT.ToStr(total_project_manday_count));
				//System.Diagnostics.Debug.WriteLine("required_dropped_mandays_for_this_level:" + CONVERT.ToStr(required_dropped_mandays_for_this_level));
				//System.Diagnostics.Debug.WriteLine("current_total_mandays_dropped:" + CONVERT.ToStr(current_total_mandays_dropped));
				//System.Diagnostics.Debug.WriteLine("extra_manday_to_dropped:" + CONVERT.ToStr(extra_mandays_to_dropped));

				if (extra_mandays_to_dropped>0)
				{
					//=========================================================================================
					//==build an arraylist of all the stages that will be affected (no effect on current stage)
					//=========================================================================================
					ArrayList affected_stages = new ArrayList();

					this.determinecurrentstate();
					//Determine how much work, we need to do 
					//we only need to do work for the following states 
					//(all other states can't descope or there is nothing to descope)
					int workcode = 0;
					switch (this.currentState)
					{
						case emProjectOperationalState.PROJECT_STATE_PROJECTSELECTED:
						case emProjectOperationalState.PROJECT_STATE_PRODUCTSELECTED:
							workcode = 9;
							break;
						case emProjectOperationalState.PROJECT_STATE_A:
							workcode = 8;
							break;
						case emProjectOperationalState.PROJECT_STATE_B:
							workcode = 7;
							break;
						case emProjectOperationalState.PROJECT_STATE_C:
							workcode = 6;
							break;
						case emProjectOperationalState.PROJECT_STATE_D:
							workcode = 5;
							break;
						case emProjectOperationalState.PROJECT_STATE_E:
							workcode = 4;
							break;
						case emProjectOperationalState.PROJECT_STATE_F:
							workcode = 3;
							break;
						case emProjectOperationalState.PROJECT_STATE_G:
							workcode = 2;
							break;
						case emProjectOperationalState.PROJECT_STATE_H:
							workcode = 1;
							break;
					}

					if (workcode > 8)
					{
						work_stage ws = this.getWorkStage(emProjectOperationalState.PROJECT_STATE_A);
						affected_stages.Add(ws);
						active_stages[0] = ws;
						ws.DebugTaskNumbers();
					}
					if (workcode > 7)
					{
						work_stage ws = this.getWorkStage(emProjectOperationalState.PROJECT_STATE_B);
						affected_stages.Add(ws);
						active_stages[1] = ws;
						ws.DebugTaskNumbers();
					}
					if (workcode > 6)
					{
						work_stage ws = this.getWorkStage(emProjectOperationalState.PROJECT_STATE_C);
						affected_stages.Add(ws);
						active_stages[2] = ws;
						ws.DebugTaskNumbers();
					}
					if (workcode > 5)
					{
						work_stage ws = this.getWorkStage(emProjectOperationalState.PROJECT_STATE_D);
						affected_stages.Add(ws);
						active_stages[3] = ws;
						ws.DebugTaskNumbers();
					}
					if (workcode > 4)
					{
						work_stage ws = this.getWorkStage(emProjectOperationalState.PROJECT_STATE_E);
						affected_stages.Add(ws);
						active_stages[4] = ws;
						ws.DebugTaskNumbers();
					}
					if (workcode > 3)
					{
						work_stage ws = this.getWorkStage(emProjectOperationalState.PROJECT_STATE_F);
						affected_stages.Add(ws);
						active_stages[5] = ws;
						ws.DebugTaskNumbers();
					}
					if (workcode > 2)
					{
						work_stage ws = this.getWorkStage(emProjectOperationalState.PROJECT_STATE_G);
						affected_stages.Add(ws);
						active_stages[6] = ws;
						ws.DebugTaskNumbers();
					}
					if (workcode > 1)
					{
						work_stage ws = this.getWorkStage(emProjectOperationalState.PROJECT_STATE_H);
						affected_stages.Add(ws);
						active_stages[7] = ws;
						ws.DebugTaskNumbers();
					}

					//System.Diagnostics.Debug.WriteLine("STARTDESCOPE");
					//We know the number of mandays that we want to remove and which stages are affected
					//Now we can remove man days for each of the stages in turn 
					//we try to remove the number of tasks one by one 
					int stage_task_count = 0;
					int stage_task_dropped_count = 0;
					int stage_task_remaining_count = 0;
					bool processed = false;

					int starting_stage = 0;
					for (int step = 0; step < extra_mandays_to_dropped; step++)
					{
						processed = false;
						//foreach task to drop, we iterate over the stages, seeing which one can drop a manday 
						for (int stage_step = 0; stage_step < 8; stage_step++)
						{
							if (processed == false)
							{
								int index = (starting_stage + stage_step) % 8;
								work_stage tmp_ws = active_stages[index];
								if (tmp_ws != null)
								{
									tmp_ws.getFullTaskCountsForStage(out stage_task_count, out stage_task_dropped_count, out stage_task_remaining_count);
									if (stage_task_remaining_count > 1)
									{
										int predicted_days = 0;

										tmp_ws.DescopeByOneManDay();
										tmp_ws.RecalculateStageTaskNumbers();
										tmp_ws.RecalculatePredictedDays(true, true, out predicted_days);
										tmp_ws.DebugTaskNumbers();
										processed = true;
										performRecalculateofGoLive = true;
										starting_stage = (index +1) % 8;
									}
								}
							}
						}
						//starting_stage++;
					}
				}
			}
			//System.Diagnostics.Debug.WriteLine("STOPDESCOPE");

			if (performRecalculateofGoLive)
			{
				this.UpdateDaysToLive(currentDay, false);
			}
			this.project_subnode_prjdata.SetAttribute("scope", CONVERT.ToStr(requested_project_droptasks_percentage));
		}

		public void DescopeCritPathTasks(int currentDay, string stage_b_sub_drop_requests,
				  string stage_b_sub_raise_requests, string stage_d_sub_drop_requests, string stage_d_sub_raise_requests)
		{
			int total_scope_change = 0;
			int stage_scope_hit = 0;
			bool total_recalc_required = false;
			bool recalc_required = false;

			string[] subtask_names;
			//bool recalcDays;
			int predicted_days = 0;

			//=============================================================
			//==Handling Stage B sub tasks changes 
			//=============================================================
			if ((stage_b_sub_drop_requests.Length>0)|(stage_b_sub_raise_requests.Length>0))
			{
				total_recalc_required = false;
				work_stage ws = this.getWorkStage(emProjectOperationalState.PROJECT_STATE_B);
				//Split into the parts that need to be changed (dropped)
				subtask_names = stage_b_sub_drop_requests.Split(',');
				foreach (string subtask_name in subtask_names)
				{
					ws.ChangeScopeStatusByNamedCritPathSection(subtask_name, true, out stage_scope_hit, out recalc_required);
					if (recalc_required)
					{
						total_recalc_required =true;
					}
					total_scope_change += stage_scope_hit;
				}
				//Split into the parts that need to be changed (raised)
				subtask_names = stage_b_sub_raise_requests.Split(',');
				foreach (string subtask_name in subtask_names)
				{
					ws.ChangeScopeStatusByNamedCritPathSection(subtask_name, false, out stage_scope_hit, out recalc_required);
					if (recalc_required)
					{
						total_recalc_required =true;
					}
					total_scope_change += stage_scope_hit;
				}

				if (total_recalc_required)
				{
					ws.RecalculateStageTaskNumbers();
					ws.RecalculatePredictedDays(true, true, out predicted_days);
					ws.DebugTaskNumbers();
				}
			}
			//=============================================================
			//==Handling Stage D sub tasks changes 
			//=============================================================
			if ((stage_d_sub_drop_requests.Length>0)|(stage_d_sub_raise_requests.Length>0))
			{
				total_recalc_required = false;
				work_stage ws = this.getWorkStage(emProjectOperationalState.PROJECT_STATE_D);
				//Split into the parts that need to be changed (dropped)
				subtask_names = stage_d_sub_drop_requests.Split(',');
				foreach (string subtask_name in subtask_names)
				{
					ws.ChangeScopeStatusByNamedCritPathSection(subtask_name, true, out stage_scope_hit, out recalc_required);
					if (recalc_required)
					{
						total_recalc_required =true;
					}
					total_scope_change += stage_scope_hit;
				}
				//Split into the parts that need to be changed (raised)
				subtask_names = stage_d_sub_raise_requests.Split(',');
				foreach (string subtask_name in subtask_names)
				{
					ws.ChangeScopeStatusByNamedCritPathSection(subtask_name, false, out stage_scope_hit, out recalc_required);
					if (recalc_required)
					{
						total_recalc_required =true;
					}
					total_scope_change += stage_scope_hit;
				}
				if (total_recalc_required)
				{
					ws.RecalculateStageTaskNumbers();
					ws.RecalculatePredictedDays(true, true, out predicted_days);
					ws.DebugTaskNumbers();
				}
			}
			//=============================================================================
			//==Handle the consequences (Chnage in overal Project scope and GoLive Day
			//=============================================================================
			if (total_scope_change != 0)
			{
				//update the scope (subtasks have absolute scope points, just add the total diff))
				int pjc_scope = this.project_subnode_prjdata.GetIntAttribute("scope", 0);
				pjc_scope = pjc_scope + total_scope_change;
				this.project_subnode_prjdata.SetAttribute("scope", CONVERT.ToStr(pjc_scope));
			}
			if (total_recalc_required)
			{ 
				this.UpdateDaysToLive(currentDay, false);
			}
		}

		#endregion Descope Methods

		#region Staff Methods

		public void setProjectStaffLimits(int currentDay, int DesignLimit, int BuildLimit, int TestLimit)
		{
			this.project_node.SetAttribute("design_reslevel", CONVERT.ToStr(DesignLimit));
			this.project_node.SetAttribute("build_reslevel", CONVERT.ToStr(BuildLimit));
			this.project_node.SetAttribute("test_reslevel", CONVERT.ToStr(TestLimit));

			//we go through all uncompleted stages are redefine only the int levels to the new values
			//the ext are kept as they are (as per the original pm game)
			//==================================================================
			//Go through all the stages and change the staff level (if Required)
			//==================================================================
			bool iscompleted = false;
			bool isinprogress = false;
			int current_request_int = 0;
			int current_request_ext = 0;
			bool changed_resource_level = false;

			int request_stage_a_internal = DesignLimit;
			int request_stage_b_internal = DesignLimit;
			int request_stage_c_internal = DesignLimit;
			int request_stage_d_internal = BuildLimit;
			int request_stage_e_internal = BuildLimit;
			int request_stage_f_internal = TestLimit;
			int request_stage_g_internal = TestLimit;
			int request_stage_h_internal = TestLimit;

			//display stage A
			getRequestedResourceLevelsForStage(emProjectOperationalState.PROJECT_STATE_A,
				out current_request_int, out current_request_ext, out isinprogress, out iscompleted);
			if ((isinprogress == false) & (iscompleted == false))
			{
				if ((request_stage_a_internal != current_request_int))
				{
					setStageResourceLevels(emProjectOperationalState.PROJECT_STATE_A,
						request_stage_a_internal, current_request_ext);
					changed_resource_level = true;
				}
			}

			//display stage B
			getRequestedResourceLevelsForStage(emProjectOperationalState.PROJECT_STATE_B,
				out current_request_int, out current_request_ext, out isinprogress, out iscompleted);
			if ((isinprogress == false) & (iscompleted == false))
			{
				if ((request_stage_b_internal != current_request_int))
				{
					setStageResourceLevels(emProjectOperationalState.PROJECT_STATE_B,
						request_stage_b_internal, current_request_ext);
					changed_resource_level = true;
				}
			}

			//display stage C
			getRequestedResourceLevelsForStage(emProjectOperationalState.PROJECT_STATE_C,
				out current_request_int, out current_request_ext, out isinprogress, out iscompleted);
			if ((isinprogress == false) & (iscompleted == false))
			{
				if ((request_stage_c_internal != current_request_int))
				{
					setStageResourceLevels(emProjectOperationalState.PROJECT_STATE_C,
						request_stage_c_internal, current_request_ext);
					changed_resource_level = true;
				}
			}

			//display stage D
			getRequestedResourceLevelsForStage(emProjectOperationalState.PROJECT_STATE_D,
				out current_request_int, out current_request_ext, out isinprogress, out iscompleted);
			if ((isinprogress == false) & (iscompleted == false))
			{
				if ((request_stage_d_internal != current_request_int))
				{
					setStageResourceLevels(emProjectOperationalState.PROJECT_STATE_D,
						request_stage_d_internal, current_request_ext);
					changed_resource_level = true;
				}
			}

			//display stage E
			getRequestedResourceLevelsForStage(emProjectOperationalState.PROJECT_STATE_E,
				out current_request_int, out current_request_ext, out isinprogress, out iscompleted);
			if ((isinprogress == false) & (iscompleted == false))
			{
				if ((request_stage_e_internal != current_request_int))
				{
					setStageResourceLevels(emProjectOperationalState.PROJECT_STATE_E,
						request_stage_e_internal, current_request_ext);
					changed_resource_level = true;
				}
			}

			//display stage F
			getRequestedResourceLevelsForStage(emProjectOperationalState.PROJECT_STATE_F,
				out current_request_int, out current_request_ext, out isinprogress, out iscompleted);
			if ((isinprogress == false) & (iscompleted == false))
			{
				if ((request_stage_f_internal != current_request_int))
				{
					setStageResourceLevels(emProjectOperationalState.PROJECT_STATE_F,
						request_stage_f_internal, current_request_ext);
					changed_resource_level = true;
				}
			}

			//display stage G
			getRequestedResourceLevelsForStage(emProjectOperationalState.PROJECT_STATE_G,
				out current_request_int, out current_request_ext, out isinprogress, out iscompleted);
			if ((isinprogress == false) & (iscompleted == false))
			{
				if ((request_stage_g_internal != current_request_int))
				{
					setStageResourceLevels(emProjectOperationalState.PROJECT_STATE_G,
						request_stage_g_internal, current_request_ext);
					changed_resource_level = true;
				}
			}

			//display stage H
			getRequestedResourceLevelsForStage(emProjectOperationalState.PROJECT_STATE_H,
				out current_request_int, out current_request_ext, out isinprogress, out iscompleted);
			if ((isinprogress == false) & (iscompleted == false))
			{
				if ((request_stage_h_internal != current_request_int))
				{
					setStageResourceLevels(emProjectOperationalState.PROJECT_STATE_H,
						request_stage_h_internal, current_request_ext);
					changed_resource_level = true;
				}
			}

			if (changed_resource_level)
			{
				if (changed_resource_level)
				{
					UpdateDaysToLive(currentDay, false);
				}
			}
		}

		public void setStageResourceLevels(emProjectOperationalState tmpState,
			int requested_int, int requested_ext)
		{
			work_stage ws = this.getWorkStage(tmpState);
			ws.setRequestedResourceLevels(requested_int, requested_ext);
		}

		public bool StaffChangeNeeded(out bool isDev, out int number_of_int_staff, out int number_of_ext_staff,
			out int number_of_int_staff_requested, out int number_of_ext_staff_requested)
		{
			bool needStaffChange = false;
			number_of_int_staff = 0;
			number_of_ext_staff = 0;
			number_of_int_staff_requested = 0;
			number_of_ext_staff_requested = 0;
			isDev = false;

			//get the workstage that we are currenlty in
			//we may be in handover or install which has no work stage so no object and nothing needed
			this.determinecurrentstate();
			work_stage ws = this.getWorkStage(currentState);
			if (ws != null)
			{
				isDev = ws.isDev();
				needStaffChange = ws.getCountofStaffChangeNeeded(out number_of_int_staff, out number_of_ext_staff,
					out number_of_int_staff_requested, out number_of_ext_staff_requested);
			}
			return needStaffChange;
		}

		public void adjustAssignedStaffCount(Node StageNode, bool isExternal, int count)
		{
			if (isExternal)
			{
				int currentcount = StageNode.GetIntAttribute("staff_ext_assigned", 0);
				currentcount = currentcount + count;
				StageNode.SetAttribute("staff_ext_assigned", CONVERT.ToStr(currentcount));
				//System.Diagnostics.Debug.WriteLine("Added 1 to stage assigned count"+ this.project_id_str+ " EXT "+currentcount.ToString());
			}
			else
			{
				int currentcount = StageNode.GetIntAttribute("staff_int_assigned", 0);
				currentcount = currentcount + count;
				StageNode.SetAttribute("staff_int_assigned", CONVERT.ToStr(currentcount));
				//System.Diagnostics.Debug.WriteLine("Added 1 to stage assigned count"+ this.project_id_str+ " INT "+currentcount.ToString());
			}
		}

		public bool AttachStaffNeeded(Node staffmember_node, out bool RequireUpdateGoLive)
		{
			RequireUpdateGoLive = false;

			this.determinecurrentstate();
			work_stage ws = this.getWorkStage(currentState);
			if (ws != null)
			{
				ws.AttachStaffMember(staffmember_node, out RequireUpdateGoLive);
			}
			return true;
		}

		/// <summary>
		/// Check throufght 
		/// </summary>
		/// <param name="day"></param>
		public void AssignWorkersToWorkIfAvailable(int day)
		{
			bool RequireUpdateGoLive = false;

			determinecurrentstate();

			bool usesStaff = this.isStaffStage();

			if (usesStaff)
			{
				work_stage ws = this.getWorkStage(currentState);
				if (ws != null)
				{
					ws.AssignWorkersToWorkIfAvailable(day, out RequireUpdateGoLive);
				}
				if (RequireUpdateGoLive)
				{
				}
			}
		}

		public void UpdateDisplayGoLiveNumber(int offset, bool UseAutoUpdate)
		{
			int go_live = project_node.GetIntAttribute("project_golive_day", 0);
			go_live = go_live + offset;
			project_node.SetAttribute("project_display_golive_day", CONVERT.ToStr(go_live));

			if (UseAutoUpdate)
			{
				if (getInstallDayAutoUpdate())
				{
					this.setInstallDay(go_live - 1);
					this.setInstallDayTimeFailure(false);
				}
			}
		}

		public void UpdateDisplayNumbers()
		{
			determinecurrentstate();
			bool usesStaff = this.isStaffStage();
			if (usesStaff)
			{
				work_stage ws = this.getWorkStage(currentState);
				if (ws != null)
				{
					ws.UpdateDisplayResourceNumbers();
				}
			}

			UpdateDaysToLive();

			int go_live = project_node.GetIntAttribute("project_golive_day", 0);
			
			int existing_display_day = project_node.GetIntAttribute("project_display_golive_day", 0);
			project_node.SetAttribute("project_display_golive_day", CONVERT.ToStr(go_live));

			if (existing_display_day != go_live)
			{
				handleInstallDayPredictedNotReady();
			}
		}

		/// <summary>
		/// This forces the direct firing of a worker from the current stage back to the employtee section 
		/// It needs to updats the stage employee numbers and predicted days 
		/// </summary>
		public void DirectFireWorker(int day, bool isInternalWorker)
		{
			//System.Diagnostics.Debug.WriteLine("DIRECT FIREWORKERS-->START");
			determinecurrentstate();
			work_stage ws = this.getWorkStage(this.currentState);
			if (ws != null)
			{
				ws.DirectFireWorker(day, isInternalWorker);
			}
			//System.Diagnostics.Debug.WriteLine("DIRECT FIREWORKERS-->STOP");
		}

		/// <summary>
		/// This process moves the workers away from doing nothing back to thier department store
		/// if there are no more tasks to do or if there is a forced reassignment  
		///  Forced reassignment is either Precancel or PrePause
		/// </summary>
		public void FireWorkers(int day)
		{
			FireWorkers(day, false);
		}

		/// <summary>
		/// This process moves the workers away from doing nothing back to thier department store
		/// if there are no more tasks to do or if there is a forced reassignment  
		///  Forced reassignment is either Precancel or PrePause
		/// </summary>
		public void FireWorkers(int day, bool NoMoney)
		{
			bool StatusRequestPreCancel = isStatusRequestPreCancel();
			bool StatusRequestPrePause = isStatusRequestPrePause();

			//System.Diagnostics.Debug.WriteLine("FIREWORKERS-->START");
			determinecurrentstate();
			work_stage ws = this.getWorkStage(this.currentState);
			if (ws != null)
			{
				ws.FireWorkers(day, NoMoney, StatusRequestPreCancel, StatusRequestPrePause);
			}
			//System.Diagnostics.Debug.WriteLine("FIREWORKERS-->STOP");
		}

		/// <summary>
		/// This process moves the workers away from tasks back to doing nothing
		/// If the task is done or there is a forced reassignment 
		///  Forced reassignment is either Precancel or PrePause or No Money
		/// </summary>
		public void ReAssignWorkers(int day)
		{
			ReAssignWorkers(day, false);
		}

		/// <summary>
		/// This process moves the workers away from tasks back to doing nothing
		/// If the task is done or there is a forced reassignment 
		/// Forced reassignment is either Precancel or PrePause or No Money
		/// </summary>
		public void ReAssignWorkers(int day, bool NoMoney)
		{
			bool StatusRequestPreCancel = isStatusRequestPreCancel();
			bool StatusRequestPrePause = isStatusRequestPrePause();
			
			//System.Diagnostics.Debug.WriteLine("REASSIGNWORKERS-->START");
			determinecurrentstate();
			work_stage ws = this.getWorkStage(this.currentState);
			if (ws != null)
			{
				ws.ReAssignWorkers(day,NoMoney, StatusRequestPreCancel, StatusRequestPrePause);
			}
			//System.Diagnostics.Debug.WriteLine("REASSIGNWORKERS-->STOP");
		}

		public void getWorkdataForDev(
			out int dev_int_tasked, out int dev_int_worked, out int dev_int_wasted,
			out int dev_ext_tasked, out int dev_ext_worked, out int dev_ext_wasted)
		{
			dev_int_tasked = this.project_subnode_workdata.GetIntAttribute("dev_int_tasked", 0);
			dev_int_worked = this.project_subnode_workdata.GetIntAttribute("dev_int_worked", 0);
			dev_int_wasted = this.project_subnode_workdata.GetIntAttribute("dev_int_wasted", 0);
			dev_ext_tasked = this.project_subnode_workdata.GetIntAttribute("dev_ext_tasked", 0);
			dev_ext_worked = this.project_subnode_workdata.GetIntAttribute("dev_ext_worked", 0);
			dev_ext_wasted = this.project_subnode_workdata.GetIntAttribute("dev_ext_wasted", 0);
		}

		public void updateWorkdataForDev(
			int dev_int_tasked, int dev_int_worked, int dev_int_wasted,
			int dev_ext_tasked, int dev_ext_worked, int dev_ext_wasted)
		{
			int current_dev_int_tasked = this.project_subnode_workdata.GetIntAttribute("dev_int_tasked", 0);
			int current_dev_int_worked = this.project_subnode_workdata.GetIntAttribute("dev_int_worked", 0);
			int current_dev_int_wasted = this.project_subnode_workdata.GetIntAttribute("dev_int_wasted", 0);
			int current_dev_ext_tasked = this.project_subnode_workdata.GetIntAttribute("dev_ext_tasked", 0);
			int current_dev_ext_worked = this.project_subnode_workdata.GetIntAttribute("dev_ext_worked", 0);
			int current_dev_ext_wasted = this.project_subnode_workdata.GetIntAttribute("dev_ext_wasted", 0);

			current_dev_int_tasked += dev_int_tasked;
			current_dev_int_worked += dev_int_worked;
			current_dev_int_wasted += dev_int_wasted;
			current_dev_ext_tasked += dev_ext_tasked;
			current_dev_ext_worked += dev_ext_worked;
			current_dev_ext_wasted += dev_ext_wasted;

			ArrayList attrs = new ArrayList();
			attrs.Add(new AttributeValuePair("dev_int_tasked", CONVERT.ToStr(current_dev_int_tasked)));
			attrs.Add(new AttributeValuePair("dev_int_worked", CONVERT.ToStr(current_dev_int_worked)));
			attrs.Add(new AttributeValuePair("dev_int_wasted", CONVERT.ToStr(current_dev_int_wasted)));
			attrs.Add(new AttributeValuePair("dev_ext_tasked", CONVERT.ToStr(current_dev_ext_tasked)));
			attrs.Add(new AttributeValuePair("dev_ext_worked", CONVERT.ToStr(current_dev_ext_worked)));
			attrs.Add(new AttributeValuePair("dev_ext_wasted", CONVERT.ToStr(current_dev_ext_wasted)));
			this.project_subnode_workdata.SetAttributes(attrs);
		}

		public void getWorkdataForTest(
			out int test_int_tasked, out int test_int_worked, out int test_int_wasted,
			out int test_ext_tasked, out int test_ext_worked, out int test_ext_wasted)
		{
			test_int_tasked = this.project_subnode_workdata.GetIntAttribute("test_int_tasked", 0);
			test_int_worked = this.project_subnode_workdata.GetIntAttribute("test_int_worked", 0);
			test_int_wasted = this.project_subnode_workdata.GetIntAttribute("test_int_wasted", 0);
			test_ext_tasked = this.project_subnode_workdata.GetIntAttribute("test_ext_tasked", 0);
			test_ext_worked = this.project_subnode_workdata.GetIntAttribute("test_ext_worked", 0);
			test_ext_wasted = this.project_subnode_workdata.GetIntAttribute("test_ext_wasted", 0);
		}

		public void updateWorkdataForTest(
			int test_int_tasked, int test_int_worked, int test_int_wasted,
			int test_ext_tasked, int test_ext_worked, int test_ext_wasted)
		{
			//Update the Local Project Copy of the Work
			int current_test_int_tasked = this.project_subnode_workdata.GetIntAttribute("test_int_tasked", 0);
			int current_test_int_worked = this.project_subnode_workdata.GetIntAttribute("test_int_worked", 0);
			int current_test_int_wasted = this.project_subnode_workdata.GetIntAttribute("test_int_wasted", 0);
			int current_test_ext_tasked = this.project_subnode_workdata.GetIntAttribute("test_ext_tasked", 0);
			int current_test_ext_worked = this.project_subnode_workdata.GetIntAttribute("test_ext_worked", 0);
			int current_test_ext_wasted = this.project_subnode_workdata.GetIntAttribute("test_ext_wasted", 0);

			current_test_int_tasked += test_int_tasked;
			current_test_int_worked += test_int_worked;
			current_test_int_wasted += test_int_wasted;
			current_test_ext_tasked += test_ext_tasked;
			current_test_ext_worked += test_ext_worked;
			current_test_ext_wasted += test_ext_wasted;

			ArrayList attrs = new ArrayList();
			attrs.Add(new AttributeValuePair("test_int_tasked", CONVERT.ToStr(current_test_int_tasked)));
			attrs.Add(new AttributeValuePair("test_int_worked", CONVERT.ToStr(current_test_int_worked)));
			attrs.Add(new AttributeValuePair("test_int_wasted", CONVERT.ToStr(current_test_int_wasted)));
			attrs.Add(new AttributeValuePair("test_ext_tasked", CONVERT.ToStr(current_test_ext_tasked)));
			attrs.Add(new AttributeValuePair("test_ext_worked", CONVERT.ToStr(current_test_ext_worked)));
			attrs.Add(new AttributeValuePair("test_ext_wasted", CONVERT.ToStr(current_test_ext_wasted)));
			this.project_subnode_workdata.SetAttributes(attrs);
		}

		private void getWagesRates(out int int_staff_wagerate, out int ext_staff_wagerate)
		{
			int_staff_wagerate = worker_internal_daypayrate;
			ext_staff_wagerate = worker_external_daypayrate;
		}

		public int getCostForNextDay(int number_int_staff, int number_ext_staff)
		{
			int int_staff_wagerate = 0;
			int ext_staff_wagerate = 0;
			int daycost_int = 0;
			int daycost_ext = 0;
			int daycost_total = 0;

			int_staff_wagerate = worker_internal_daypayrate;
			ext_staff_wagerate = worker_external_daypayrate;

			daycost_int = int_staff_wagerate * number_int_staff;
			daycost_ext = ext_staff_wagerate * number_ext_staff;
			daycost_total = daycost_int + daycost_ext;
			return daycost_total;
		}

		public void RecordWorkAchieved(int day, bool applyCosts, out int NothingHrs, out int WorkedHrs, out int WorkerCost)
		{
			int predicted_days = 0;
			bool isDev = false;

			NothingHrs = 0;
			WorkedHrs = 0;
			WorkerCost = 0;

			//Stats collection for number of days 
			int int_tasked = 0;
			int int_worked = 0;
			int int_wasted = 0;
			int ext_tasked = 0;
			int ext_worked = 0;
			int ext_wasted = 0;

			//System.Diagnostics.Debug.WriteLine("RECORD WORK-->START");
			determinecurrentstate();
			work_stage ws = this.getWorkStage(this.currentState);
			if (ws != null)
			{
				ws.RecordWorkAchieved(out isDev, 
					out int_tasked, out int_worked, out int_wasted,
					out ext_tasked, out ext_worked, out ext_wasted);

				//Calculate the Worker Cost
				if (applyCosts)
				{
					WorkerCost += (worker_internal_daypayrate * int_tasked);
					WorkerCost += (worker_internal_daypayrate * ext_tasked);
				}
				//Recalculate the predicted_days 
				ws.RecalculatePredictedDays(true, true, out predicted_days);
				//ws.DebugTaskNumbers();
				//ws.DebugPredictedDays();

				if (ws.isDev())
				{
					updateWorkdataForDev(int_tasked, int_worked, int_wasted, ext_tasked, ext_worked, ext_wasted);
				}
				else
				{
					updateWorkdataForTest(int_tasked, int_worked, int_wasted, ext_tasked, ext_worked, ext_wasted);
				}
			}
		}

		public void UpdateDaysToLive ()
		{
			UpdateDaysToLive(MyNodeTree.GetNamedNode("CurrentDay").GetIntAttribute("day", 0), false);
		}


		public void UpdateDaysToLive(int currDay, bool handleComplete)
		{
			currDay = Math.Max(1, currDay);
			bool work_completed = false;

			int daysTillWorkStarts = Math.Max(0, getDelayedStartDay() - currDay);

			//System.Diagnostics.Debug.WriteLine("======================================================");
			//System.Diagnostics.Debug.WriteLine("UPDATE GOLIVE START");
			this.determinecurrentstate();

			int days_to_live = daysTillWorkStarts;
			int work_left = 0;
			if (project_node != null)
			{
				//If we are complete, no more updates to go live day 
				if (this.isCurrentState(emProjectOperationalState.PROJECT_STATE_COMPLETED) == false)
				{
					//System.Diagnostics.Debug.WriteLine("======================================================");
					switch (this.currentState)
					{
						case emProjectOperationalState.PROJECT_STATE_PRODUCTSELECTED:
						case emProjectOperationalState.PROJECT_STATE_A:
						case emProjectOperationalState.PROJECT_STATE_B:
						case emProjectOperationalState.PROJECT_STATE_C:
						case emProjectOperationalState.PROJECT_STATE_D:
						case emProjectOperationalState.PROJECT_STATE_E:
						case emProjectOperationalState.PROJECT_STATE_F:
						case emProjectOperationalState.PROJECT_STATE_G:
						case emProjectOperationalState.PROJECT_STATE_H:
						case emProjectOperationalState.PROJECT_STATE_I: //DEVELOPING_I  REBUILD 1
						case emProjectOperationalState.PROJECT_STATE_J: //TESTING_J     RETEST 1
						case emProjectOperationalState.PROJECT_STATE_K: //DEVELOPING_K  REBUILD 2
						case emProjectOperationalState.PROJECT_STATE_L: //TESTING_L     RETEST 2
						case emProjectOperationalState.PROJECT_STATE_M: //DEVELOPING_M  REBUILD 3
						case emProjectOperationalState.PROJECT_STATE_N: //TESTING_N     RETEST 3
						case emProjectOperationalState.PROJECT_STATE_P: //DEVELOPING_P  REBUILD 4
						case emProjectOperationalState.PROJECT_STATE_Q: //TESTING_Q     RETEST 4
						case emProjectOperationalState.PROJECT_STATE_R: //DEVELOPING_R  REBUILD 5
						case emProjectOperationalState.PROJECT_STATE_T: //TESTING_T     RETEST 5
							days_to_live += 2;
							break;
						case emProjectOperationalState.PROJECT_STATE_INSTALL_NO_LOCATION:
						case emProjectOperationalState.PROJECT_STATE_IN_HANDOVER:
							days_to_live += 2;
							work_completed = true;
							break;
						case emProjectOperationalState.PROJECT_STATE_INSTALLING:
							days_to_live += 1;
							work_completed = true;
							break;
						case emProjectOperationalState.PROJECT_STATE_PRE_INSTALLING:
							days_to_live += 2;
							work_completed = true;
							break;
						case emProjectOperationalState.PROJECT_STATE_INSTALLED_OK:
						case emProjectOperationalState.PROJECT_STATE_COMPLETED:
							days_to_live += 0;
							work_completed = true;
							break;
						case emProjectOperationalState.PROJECT_STATE_INSTALLED_FAIL:
							days_to_live += 2;
							work_completed = true;
							break;
					}
					//System.Diagnostics.Debug.WriteLine("Start--->CurrDay: State: " + this.currentState.ToString());
					//System.Diagnostics.Debug.WriteLine("Start--->CurrDay: CurrDay: " + currDay.ToString());
					//System.Diagnostics.Debug.WriteLine("Start--->CurrDay: OFFSET:" + days_to_live.ToString());
					days_to_live += currDay;

					work_left = 0;
					foreach (work_stage ws in this.project_work_stages)
					{
						string name = ws.getCode();
						int pd = ws.getPredictedDays();
						//System.Diagnostics.Debug.WriteLine("STAGE  " + name + " days " + CONVERT.ToStr(pd));
						days_to_live += pd;
						work_left += pd;
					}
					//System.Diagnostics.Debug.WriteLine("Start--->CurrDay: Stge: prevDaysToLive:" + work_left.ToString());
					//System.Diagnostics.Debug.WriteLine("Start--->CurrDay: Stge: prevDaysToLive:" + days_to_live.ToString());

					//take account of requested Interative development
					int recycle_req_count = this.getRecycleRequestCount();
					int recycle_proc_count = this.getRecycleProcessedCount();
					if (recycle_req_count > recycle_proc_count)
					{
						days_to_live += 2 * (recycle_req_count - recycle_proc_count);
						//System.Diagnostics.Debug.WriteLine(" Recycle " + CONVERT.ToStr((recycle_req_count - recycle_proc_count)));
					}

					//System.Diagnostics.Debug.WriteLine(" New DaystoLive:" + CONVERT.ToStr(days_to_live));
					project_node.SetAttribute("project_golive_day", CONVERT.ToStr(days_to_live));

					if (getInstallDayAutoUpdate())
					{
						//Round 3 using Auto install at earliest possible
						int current_install_day = this.getInstallDay();
						if (work_completed == false)
						{
							this.setInstallDay(days_to_live - 1);
							this.setInstallDayTimeFailure(false);
						}
					}
					else
					{
						handleInstallDayPredictedNotReady();
					}
				}
				else
				{
					if (handleComplete)
					{
						days_to_live += currDay;
						//System.Diagnostics.Debug.WriteLine(" New DaystoLive:" + CONVERT.ToStr(days_to_live));
						project_node.SetAttribute("project_golive_day", CONVERT.ToStr(days_to_live));
					}
				}
			}
			//System.Diagnostics.Debug.WriteLine("UPDATE GOLIVE STOP");
		}

		public void handleInstallDayPredictedNotReady()
		{
			int current_install_day = this.getInstallDay();
			int current_GoLiveday = this.getProjectGoLiveDay();
			int current_DisplayGoLiveday = this.getProjectDisplayGoLiveDay();
			bool AnyLaterDayIsOK = false;

			bool notReadyGoLive = false; 
			bool notDisplayReadyGoLive = false; 

			if (current_install_day ==0)
			{
				this.setInstallDayPredictedNotReady(false);
			}
			else
			{
				if (AnyLaterDayIsOK)
				{
					//Provided that the Install day is the later or equal to the day before the GoLive 
					//then we are OK
					//Any day after the day before Go Live is OK

					//Condition One (Check Actual)
					if ((current_install_day < (current_GoLiveday - 1)))
					{
						notReadyGoLive = true;
					}
					//Condition One (Check DisplayGoiLive)
					if ((current_install_day < (current_DisplayGoLiveday - 1)))
					{
						notDisplayReadyGoLive = true;
					}
					//Now set the Flag 
					if ((notReadyGoLive) | (notDisplayReadyGoLive))
					{
						//Show the Warning
						this.setInstallDayPredictedNotReady(true);
					}
					else
					{
						//DO NOT Show the Warning
						this.setInstallDayPredictedNotReady(false);
					}

				}
				else
				{
					if (current_GoLiveday == current_DisplayGoLiveday)
					{
						//Any day after the day before Go Live is OK
						//Condition One (Check Actual)
						if ((current_install_day != (current_GoLiveday - 1)))
						{
							notReadyGoLive = true;
						}
						//Condition One (Check DisplayGoiLive)
						if ((current_install_day != (current_DisplayGoLiveday - 1)))
						{
							notDisplayReadyGoLive = true;
						}
					}
					else
					{ 
						//The days are different (only consider the current)
						if ((current_install_day != (current_DisplayGoLiveday - 1)))
						{
							notReadyGoLive = true;
						}	
					}

					//Now set the flag
					if ((notReadyGoLive) | (notDisplayReadyGoLive))
					{
						this.setInstallDayPredictedNotReady(true);
					}
					else
					{
						this.setInstallDayPredictedNotReady(false);
					}
				}
			}
		}
		#endregion Staff Methods
	}
}
