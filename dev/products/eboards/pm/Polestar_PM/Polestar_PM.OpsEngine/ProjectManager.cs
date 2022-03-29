using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;

using Network;

using LibCore;
using CoreUtils;
using Polestar_PM.DataLookup;

using GameManagement;
using Logging;

namespace Polestar_PM.OpsEngine
{
	public class day_work_report
	{
		public int dev_int_tasked=0;
		public int dev_int_worked=0;
		public int dev_int_wasted=0;
		public int dev_ext_tasked=0;
		public int dev_ext_worked=0;
		public int dev_ext_wasted=0;
		public int test_int_tasked=0;
		public int test_int_worked=0;
		public int test_int_wasted=0;
		public int test_ext_tasked=0;
		public int test_ext_worked=0;
		public int test_ext_wasted=0;

		public day_work_report()
		{ }

		public void wrt_debug(string prefix)
		{
			string dbg = "";
			dbg += "DIT:" + CONVERT.ToStr(dev_int_tasked)+"";
			dbg += "DIW:" + CONVERT.ToStr(dev_int_worked) + "";
			dbg += "DIA:" + CONVERT.ToStr(dev_int_wasted) + "";
			dbg += "DET:" + CONVERT.ToStr(dev_ext_tasked) + "";
			dbg += "DEW:" + CONVERT.ToStr(dev_ext_worked) + "";
			dbg += "DEA:" + CONVERT.ToStr(dev_ext_wasted) + "";

			dbg += "TIT:" + CONVERT.ToStr(test_int_tasked) + "";
			dbg += "TIW:" + CONVERT.ToStr(test_int_worked) + "";
			dbg += "TIA:" + CONVERT.ToStr(test_int_wasted) + "";
			dbg += "TET:" + CONVERT.ToStr(test_ext_tasked) + "";
			dbg += "TEW:" + CONVERT.ToStr(test_ext_worked) + "";
			dbg += "TEA:" + CONVERT.ToStr(test_ext_wasted) + "";
			System.Diagnostics.Debug.WriteLine(prefix+" "+dbg);
		}
	}

	/// <summary>
	/// The Project Manager Class maintaisn a list of of Project Runner Object which relate to running projects
	/// 
	/// </summary>
	public class ProjectManager
	{
		protected const int numberofProject = 7;

		protected NetworkProgressionGameFile MyGameFile = null;
		protected NodeTree MyNodeTree = null;
		protected Node projectsNode = null;
		protected ProjectRunner[] ProjectRunnerArray = new ProjectRunner[numberofProject];
		protected Node MyChangeListNode = null;

		//the worker section nodes 
		protected Node staff_section_int_dev_node = null;
		protected Node staff_section_ext_dev_node = null;
		protected Node staff_section_int_test_node = null;
		protected Node staff_section_ext_test_node = null;

		protected int money_per_point = 0;
		protected int gain_penalty_per_missed_reg_project = 0;
		protected int fin_penalty_per_missed_reg_project = 0;
		protected int costavoid_benefit_per_completed_reg_project = 0;

		protected int round;

		DayTimeSheet[] past_time_sheets = new DayTimeSheet[GameConstants.MAX_NUMBER_DAYS+1];
		//Dictionary<int, day_work_report> day_culm_reports = new Dictionary<int, day_work_report>();
		Hashtable daily_reps = new Hashtable();

		public ProjectManager(NetworkProgressionGameFile gameFile, NodeTree tree, int tmpround)
		{
			MyGameFile = gameFile;
			MyNodeTree = tree;
			round = tmpround;
			projectsNode = MyNodeTree.GetNamedNode("pm_projects_running");

			//staff links (where the staff come from)
			staff_section_int_dev_node = MyNodeTree.GetNamedNode("dev_staff");
			staff_section_ext_dev_node = MyNodeTree.GetNamedNode("dev_contractor");
			staff_section_int_test_node = MyNodeTree.GetNamedNode("test_staff");
			staff_section_ext_test_node = MyNodeTree.GetNamedNode("test_contractor");

			//Handles Any Changes as well
			MyChangeListNode = MyNodeTree.GetNamedNode("project_duration_change_list");
			MyChangeListNode.ChildAdded += new Network.Node.NodeChildAddedEventHandler(MyChangeListNode_ChildAdded);

			//watch out for any projects created or cancelled 
			projectsNode.ChildAdded += new Node.NodeChildAddedEventHandler(projectsNode_ChildAdded);
			projectsNode.ChildRemoved += new Node.NodeChildRemovedEventHandler(projectsNode_ChildRemoved);
			projectsNode.AttributesChanged += new Node.AttributesChangedEventHandler(projectsNode_AttributesChanged);


			//Pickup any existing project
			foreach (Node pnode in projectsNode.getChildren())
			{
				this.handleNewProjectNode(pnode);
			}
			//skinning values
			money_per_point = SkinningDefs.TheInstance.GetIntData("revenue_money_per_point", 0);
			gain_penalty_per_missed_reg_project = SkinningDefs.TheInstance.GetIntData("gain_penalty_per_missed_reg_project", 0);
			fin_penalty_per_missed_reg_project = SkinningDefs.TheInstance.GetIntData("fin_penalty_per_missed_reg_project", 0);
			costavoid_benefit_per_completed_reg_project = SkinningDefs.TheInstance.GetIntData("costavoid_benefit_per_completed_reg_project", 0);
		}

		public void Dispose()
		{
			MyGameFile = null;
			MyNodeTree = null;

			if (projectsNode != null)
			{
				projectsNode.ChildAdded -= new Network.Node.NodeChildAddedEventHandler(projectsNode_ChildAdded);
				projectsNode.ChildRemoved -= new Network.Node.NodeChildRemovedEventHandler(projectsNode_ChildRemoved);
				projectsNode = null;
			}
			if (MyChangeListNode != null)
			{
				MyChangeListNode.ChildAdded -= new Network.Node.NodeChildAddedEventHandler(MyChangeListNode_ChildAdded);
				MyChangeListNode = null;
			}

			//close all the open project runners (want to call dispose to ensure full cleanup)
			for (int step = 0; step < numberofProject; step++)
			{ 
				if (ProjectRunnerArray[step] != null)
				{
					ProjectRunner pr = ProjectRunnerArray[step];
					pr.Dispose();
				}
			}
		}

		/// <summary>
		/// Handles any Request to Change a Project Duration
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="child"></param>
		private void MyChangeListNode_ChildAdded(Node sender, Node child)
		{
			string type = child.GetAttribute("type");
			int slot = child.GetIntAttribute("slot", 0);
			int extra_days = child.GetIntAttribute("extra_days", 0);
			string reason = child.GetAttribute("reason");

			int project_slot_id = slot - 1;

			if (ProjectRunnerArray[project_slot_id] != null)
			{
				ProjectRunner pr = ProjectRunnerArray[project_slot_id];
				if (pr != null)
				{
					pr.SetNextStageDurationChange(extra_days, reason);
				}
			}
			child.SetAttribute("status", "done");
		}

		void projectsNode_ChildRemoved(Node sender, Node child)
		{
			handleRemoveProjectNode(child);
		}

		void projectsNode_ChildAdded(Node sender, Node child)
		{
			handleNewProjectNode(child);
		}

		private void projectsNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			bool refreshtimesheets = false;

			if (attrs != null)
			{
				if (attrs.Count > 0)
				{
					foreach (AttributeValuePair avp in attrs)
					{
						if (avp.Attribute.ToLower() == "rebuild_timesheets")
						{
							refreshtimesheets = true;
						}
					}
				}
			}
			if (refreshtimesheets)
			{
				RebuildFutureTimesheets();
			}
		}

		public void RebuildFutureTimesheets ()
		{
			Node CurrDayNode = MyNodeTree.GetNamedNode("CurrentDay");
			int current_day = CurrDayNode.GetIntAttribute("day", 0);
			ForceRebuildFutureTimeSheets(this.MyGameFile, current_day);
		}

		private void handleNewProjectNode(Node pnode)
		{
			ProjectRunner pr = new ProjectRunner(pnode);
			int intended_slot = pr.getProjectSlot();
			ProjectRunnerArray[pr.getProjectSlot()] = pr;
			if (intended_slot == 6)
			{
				bool areWeAlreadyDisplayingSeven = this.projectsNode.GetBooleanAttribute("display_seven_projects", false);
				if (areWeAlreadyDisplayingSeven == false)
				{ 
					//The Fisrt add of a seventh project, we give it top priority
					//need to swap the slot order 1,2,3,4,5,6,7 becomes 7,1,2,3,4,5,6
					for (int step = 6; step > 0; step--)
					{
						ProjectRunnerArray[step] = ProjectRunnerArray[step - 1];
						if (ProjectRunnerArray[step] != null)
						{
							ProjectRunnerArray[step].setProjectSlot(step);
						}
					}
					ProjectRunnerArray[0] = pr;
					if (ProjectRunnerArray[0] != null)
					{
						ProjectRunnerArray[0].setProjectSlot(0);
					}
				}
			}
		}

		private void handleRemoveProjectNode(Node pnode)
		{
			//need to create a pr in order to get the slot number 
			ProjectRunner tmp_pr = new ProjectRunner(pnode);
			int project_slot = tmp_pr.getProjectSlot();
			tmp_pr.Dispose();

			if (ProjectRunnerArray[project_slot]!= null)
			{
				ProjectRunnerArray[project_slot].Dispose();
				ProjectRunnerArray[project_slot] = null;
			}
		}

		public void CalculateProjectedBenefitsForAllProjects(
			out int transactionBenefit, out int costReductionBenefitout)
		{
			transactionBenefit =0;
			costReductionBenefitout = 0;

			for (int step = 0; step < numberofProject; step++)
			{
				ProjectRunner pr = this.ProjectRunnerArray[step];
				if (pr != null)
				{
					transactionBenefit += pr.getGainPlanned();
					costReductionBenefitout += pr.getReductionPlanned();
				}
			}
			//Mind to multiply the gain by 1,000
			transactionBenefit = transactionBenefit * 1000;
		}

		public void RecordWorkAchieved(int day, bool applyCostAtEndofDay)
		{
			int NothingHrs = 0;
			int WorkedHrs = 0;
			int WorkerCost = 0;

			for (int step = 0; step < numberofProject; step++)
			{ 
				ProjectRunner pr = this.ProjectRunnerArray[step];
				if (pr != null)
				{
					pr.RecordWorkAchieved(day, applyCostAtEndofDay, out NothingHrs, out WorkedHrs, out WorkerCost);
					//System.Diagnostics.Debug.WriteLine("RECORDWORK- NothingHrs:" + NothingHrs.ToString() + " WorkedHrs:" + WorkedHrs.ToString() + " WorkerCost:" + WorkerCost.ToString());
					if (applyCostAtEndofDay)
					{
						pr.alterSpend(day, WorkerCost);
					}
				}
			}
		}

		private int getNumberOfStaffOnBench(bool isInternal)
		{
			int number_of_staff = 0;
			string staff_area_name = "dev_staff";
			if (isInternal == false)
			{
				staff_area_name = "dev_contractor";
			}
			Node staff_area_node = this.MyNodeTree.GetNamedNode(staff_area_name);
			number_of_staff = staff_area_node.getChildren().Count;
			return number_of_staff;
		}

		private void moveStaffFromBenchToRecall(bool isInternal, int numberToMove)
		{
			string staff_normal_area_name = "dev_staff";
			string staff_recall_area_name = "dev_staff_recall";

			if (isInternal == false)
			{
				staff_normal_area_name = "dev_contractor";
				staff_recall_area_name = "dev_contractor_recall";
			}
			Node staff_area_node = this.MyNodeTree.GetNamedNode(staff_normal_area_name);
			Node staff_recall_node = this.MyNodeTree.GetNamedNode(staff_recall_area_name);

			for (int step = 0; step < numberToMove; step++)
			{
				if (staff_area_node.getChildren().Count > 0)
				{ 
					//move a person over 
					Node person = staff_area_node.GetFirstChild();
					if (person != null)
					{
						staff_recall_node.AddChild(person);
						MyNodeTree.FireMovedNode(staff_area_node, person);
					}
				}
			}
		}

		private void HandleStaffRecall(int day)
		{
			int count_int_staff = 0;
			int count_ext_staff = 0;

			//Extract out whether we have any recall to process 
			Node staff_control_node = this.MyNodeTree.GetNamedNode("department");
			bool internalRecall = staff_control_node.GetBooleanAttribute("int_recall", false);
			int internalRecallCount = staff_control_node.GetIntAttribute("int_recall_count", 0);
			bool contractorRecall = staff_control_node.GetBooleanAttribute("ext_recall", false);
			int contractorRecallCount = staff_control_node.GetIntAttribute("ext_recall_count", 0);

			//=====================================================
			//==handle any Internal Staff recall
			//=====================================================
			if (internalRecall)
			{
				int remainingStaffToRecall = internalRecallCount;
				//Extract staff from the Department first, trying to limit effect on projects 
				int numberOnBench = getNumberOfStaffOnBench(true);
				remainingStaffToRecall = remainingStaffToRecall - numberOnBench;
				//Extract the amount of contractors that we need 
				if (remainingStaffToRecall > 0)
				{
					for (int prj_step = 0; prj_step < numberofProject; prj_step++)
					{
						ProjectRunner pr = this.ProjectRunnerArray[prj_step];
						if (pr != null)
						{
							bool project_changed = false;
							bool isPaused = pr.isPaused();
							bool isStaffStage = pr.isStaffStage();
							if ((pr.isPaused() == false) & (isStaffStage == true))
							{
								//System.Diagnostics.Debug.WriteLine("  LOOKING AT PR" + pr.getProjectSlot().ToString());
								pr.getStaffCountForAllocatedStaff(out count_int_staff, out count_ext_staff);
								if (count_int_staff > 0)
								{
									for (int person_step = 0; person_step < count_int_staff; person_step++)
									{
										if (remainingStaffToRecall > 0)
										{
											pr.DirectFireWorker(day, true);
											remainingStaffToRecall--;
											project_changed = true;
										}
									}
								}
							}
							if (project_changed)
							{
								pr.recalculateGoLiveAfterRecall(day);
							}
						}
					}
					staff_control_node.SetAttribute("int_recall", "false");
					staff_control_node.SetAttribute("int_recall_count", "0");
				}
				//Now move the staff into the recall area from the normall staff area
				moveStaffFromBenchToRecall(true, internalRecallCount);
			}
			//=====================================================
			//==Now handle any External Contractor recall
			//=====================================================
			if (contractorRecall)
			{
				int remainingContractorToRecall = contractorRecallCount;
				//Extract contractors from the Department first, trying to limit effect on projects 
				int numberOnBench = getNumberOfStaffOnBench(false);
				remainingContractorToRecall = remainingContractorToRecall - numberOnBench;
				//Extract the amount of contractors that we need 
				if (remainingContractorToRecall > 0)
				{
					for (int prj_step = 0; prj_step < numberofProject; prj_step++)
					{
						ProjectRunner pr = this.ProjectRunnerArray[prj_step];
						if (pr != null)
						{
							bool project_changed = false; 
							bool isPaused = pr.isPaused();
							bool isStaffStage = pr.isStaffStage();
							if ((pr.isPaused() == false) & (isStaffStage == true))
							{
								//System.Diagnostics.Debug.WriteLine("  LOOKING AT PR" + pr.getProjectSlot().ToString());
								pr.getStaffCountForAllocatedStaff(out count_int_staff, out count_ext_staff);
								if (count_ext_staff > 0)
								{
									for (int person_step = 0; person_step < count_ext_staff; person_step++)
									{
										if (remainingContractorToRecall > 0)
										{
											pr.DirectFireWorker(day, false);
											remainingContractorToRecall--;
											project_changed = true;
										}
									}
								}
							}

							if (project_changed)
							{
								pr.recalculateGoLiveAfterRecall(day);
							}
						}
					}
					staff_control_node.SetAttribute("ext_recall", "false");
					staff_control_node.SetAttribute("ext_recall_count", "0");
				}
				//Now move the staff into the recall area from the normall staff area
				moveStaffFromBenchToRecall(false, contractorRecallCount);
			}

		}

		public void HireStaff(int day, bool ApplyCostAtStartofDay)
		{
			//System.Diagnostics.Debug.WriteLine("HIRE STAFF START");
			int total_int_staff_asked_For_Dev = 0;
			int total_ext_staff_asked_For_Dev = 0;
			int total_int_staff_asked_For_Test = 0;
			int total_ext_staff_asked_For_Test = 0;
			int project_int_staff_as_ordered = 0; 
			int project_ext_staff_as_ordered = 0; 

			HandleStaffRecall(day);
			for (int prj_step = 0; prj_step < numberofProject; prj_step++)
			{
				project_int_staff_as_ordered = 0; 
				project_ext_staff_as_ordered = 0; 

				ProjectRunner pr = this.ProjectRunnerArray[prj_step];
				if (pr != null)
				{
					bool isPaused = pr.isPaused();
					bool isStaffStage = pr.isStaffStage();

					if ((pr.isPaused() == false) & (isStaffStage == true))
					{
						//System.Diagnostics.Debug.WriteLine("  LOOKING AT PR"+pr.getProjectSlot().ToString());
						Node staff_int_source_node = null;
						Node staff_ext_source_node = null;
						bool isDev = false;
						int number_of_extra_int_staff = 0; //how many extra int staff do we need 
						int number_of_extra_ext_staff = 0; //how many extra ext staff do we need 

						bool updategolive = false;

						//Are we up to requested Strength (if not then recruit more if possible)
						if (pr.StaffChangeNeeded(out isDev, out number_of_extra_int_staff, out number_of_extra_ext_staff,
										out project_int_staff_as_ordered, out project_ext_staff_as_ordered))
						{
							//Determine which section we need to take the worker nodes from 
							if (isDev)
							{
								staff_int_source_node = staff_section_int_dev_node;
								staff_ext_source_node = staff_section_ext_dev_node;
							}
							else
							{
								staff_int_source_node = staff_section_int_test_node;
								staff_ext_source_node = staff_section_ext_test_node;
							}

							if (number_of_extra_int_staff > 0)
							{
								//try and get the additional staff members needed (internal)
								int number_of_free_staff = staff_int_source_node.getChildren().Count;
								if (number_of_free_staff > 0)
								{
									int number_of_staff_to_move = 0;
									//how many can we supply
									if (number_of_free_staff > number_of_extra_int_staff)
									{
										//we can fill the need 
										number_of_staff_to_move = number_of_extra_int_staff;
									}
									else
									{
										//we can only partially fill the need 
										number_of_staff_to_move = number_of_free_staff;
									}
									//Now transfer the number of staff required
									for (int step = 0; step < number_of_staff_to_move; step++)
									{
										ArrayList currentstaff = staff_int_source_node.getChildren();
										if (currentstaff.Count > 0)
										{
											Node staff_member = (Node)currentstaff[0];
											//this adds the new staff memmber to the project team (Doing Nothing)
											//this updates the stage predicted days as well
											pr.AttachStaffNeeded(staff_member, out updategolive);
										}
									}
								}
							}
							else
							{ 
								//handle the direct firing of people, currently assigned in this stage
								int numberToLetGo = Math.Abs(number_of_extra_int_staff);
								if (numberToLetGo > 0)
								{
									for (int step = 0; step < Math.Abs(numberToLetGo); step++)
									{
										pr.DirectFireWorker(day, true);
									}
									updategolive = true;
								}
							}
							if (number_of_extra_ext_staff > 0)
							{
								//try and get the additional staff memebers needed (external)
								int number_of_free_staff = staff_ext_source_node.getChildren().Count;
								if (number_of_free_staff > 0)
								{
									int number_of_staff_to_move = 0;
									//how many can we supply
									if (number_of_free_staff > number_of_extra_ext_staff)
									{
										//we can fill the need 
										number_of_staff_to_move = number_of_extra_ext_staff;
									}
									else
									{
										//we can only partially fill the need 
										number_of_staff_to_move = number_of_free_staff;
									}
									//Now transfer the number of staff required
									for (int step = 0; step < number_of_staff_to_move; step++)
									{
										ArrayList currentstaff = staff_ext_source_node.getChildren();
										if (currentstaff.Count > 0)
										{
											Node staff_member = (Node)currentstaff[0];
											//this adds the new staff memmber to the project team (Doing Nothing)
											//this updates the stage predicted days as well
											pr.AttachStaffNeeded(staff_member, out updategolive);
										}
									}
								}
							}
							else
							{
								//handle the direct firing of people, currently assigned in this stage
								int numderToLetGo = Math.Abs(number_of_extra_ext_staff);
								if (numderToLetGo > 0)
								{
									for (int step = 0; step < numderToLetGo; step++)
									{
										pr.DirectFireWorker(day, false);
									}
									updategolive = true;
								}
							}
						}
						
						//Accounting Over time 
						if (isDev)
						{
							total_int_staff_asked_For_Dev += project_int_staff_as_ordered;
							total_ext_staff_asked_For_Dev += project_ext_staff_as_ordered;
						}
						else
						{
							total_int_staff_asked_For_Test += project_int_staff_as_ordered;
							total_ext_staff_asked_For_Test += project_ext_staff_as_ordered;
						}
						//System.Diagnostics.Debug.WriteLine("   PR "+pr.getProjectSlot().ToString() + "Dev:" +CONVERT.ToStr(isDev)+ 
						//  "Int:" + CONVERT.ToStr(project_int_staff_as_ordered) + "  " +
						//  "Ext:" + CONVERT.ToStr(project_ext_staff_as_ordered));


						//we have changed the staff and need to update the project golive
						if (updategolive)
						{
							pr.UpdateDaysToLive(day, false);
						}

						//need to check that we can afford the staff 
						int number_of_int_staff = 0;
						int number_of_ext_staff = 0;
						int nextdaycost = 0;
						int budget_left = 0;

						pr.getStaffCountForAllocatedStaff(out number_of_int_staff, out number_of_ext_staff);
						nextdaycost = pr.getCostForNextDay(number_of_int_staff, number_of_ext_staff);
						budget_left = pr.getBudgetleft();

						int gap = nextdaycost - budget_left;
						if (nextdaycost > budget_left)
						{
							//Not enough money to pay staff, so release them all  	
							pr.ReAssignWorkers(day, true);
							pr.FireWorkers(day, true);
							pr.SetGoodMoneyFlag(false, gap);
						}
						else
						{
							if (ApplyCostAtStartofDay)
							{
								pr.alterSpend(day, nextdaycost);
							}
							pr.SetGoodMoneyFlag(true, 0);
						}
					}
				}
			}

			//System.Diagnostics.Debug.WriteLine(
			//  " DAY " + CONVERT.ToStr(day) + 
			//  "DevInt:" + CONVERT.ToStr(total_int_staff_asked_For_Dev) + "  " +
			//  "DevExt:" + CONVERT.ToStr(total_ext_staff_asked_For_Dev)+ "  "+
			//  "TestInt:" + CONVERT.ToStr(total_int_staff_asked_For_Test)+ "  "+
			//  "TestExt:" + CONVERT.ToStr(total_ext_staff_asked_For_Test)+ "  ");
			//System.Diagnostics.Debug.WriteLine("HIRE STAFF STOP");

			UpdateDemandData(day, total_int_staff_asked_For_Dev, total_ext_staff_asked_For_Dev, 
				total_int_staff_asked_For_Test, total_ext_staff_asked_For_Test);
		}

		private void UpdateDemandData(int day, int devint, int devext, int testint, int testext)
		{
			string demand_nodename = "demand_sheet" + CONVERT.ToStr(day);
			Node demand_node = this.MyNodeTree.GetNamedNode(demand_nodename);
			if (demand_node != null)
			{
				ArrayList attrs = new ArrayList();
				attrs.Add(new AttributeValuePair("IntDevPeopleRequired", CONVERT.ToStr(devint)));
				attrs.Add(new AttributeValuePair("ExtDevPeopleRequired", CONVERT.ToStr(devext)));
				attrs.Add(new AttributeValuePair("IntTestDevPeopleRequired", CONVERT.ToStr(testint)));
				attrs.Add(new AttributeValuePair("ExtTestPeopleRequired", CONVERT.ToStr(testext)));
				demand_node.SetAttributes(attrs);
			}
		}

		public void FireStaff(int day)
		{
			for (int step = 0; step < numberofProject; step++)
			{
				ProjectRunner pr = this.ProjectRunnerArray[step];
				if (pr != null)
				{
					pr.FireWorkers(day);
				}
			}
		}

		public void AssignStaff(int day)
		{
			for (int step = 0; step < numberofProject; step++)
			{
				ProjectRunner pr = this.ProjectRunnerArray[step];
				if (pr != null)
				{
					pr.AssignWorkersToWorkIfAvailable(day);
				}
			}
		}

		public void UpdateDisplayNumbers()
		{
			for (int step = 0; step < numberofProject; step++)
			{
				ProjectRunner pr = this.ProjectRunnerArray[step];
				if (pr != null)
				{
					pr.UpdateDisplayNumbers();
				}
			}		
		}


		public void ReassignStaff(int day)
		{
			for (int step = 0; step < numberofProject; step++)
			{
				ProjectRunner pr = this.ProjectRunnerArray[step];
				if (pr != null)
				{
					pr.ReAssignWorkers(day); 
				}
			}
		}

		public void UpdateGoliveDay(int day)
		{
			for (int step = 0; step < numberofProject; step++)
			{
				ProjectRunner pr = this.ProjectRunnerArray[step];
				if (pr != null)
				{
					pr.UpdateDaysToLive(day, false);
				}
			}
		}

		/// <summary>
		/// Move to the next state, if required 
		/// </summary>
		/// <param name="day"></param>
		public void ChangeState(int day)
		{
			bool movedState = false;
			ArrayList al = new ArrayList();
			ArrayList killlist = new ArrayList();
			bool killflag = false;
			bool handledinstall = false;

			for (int step = 0; step < numberofProject; step++)
			{
				ProjectRunner pr = this.ProjectRunnerArray[step];
				if (pr != null)
				{
					pr.handleRequests(out killflag);
					if (killflag)
					{
						killlist.Add(pr);
					}
					else
					{
						if (pr.isStateCompleted(day))
						{
							pr.moveToNextState(day, out movedState, out handledinstall);
						}
						int defined_install_day = pr.getInstallDay();
						if ((defined_install_day == day) & (handledinstall == false))
						{
							pr.setInstallDayTimeFailure(true);
							pr.SetInstallTooEarly();
						}
					}
				}
			}
			//remove any canceled Projects 
			if (killlist.Count > 0)
			{
				foreach (Node n2 in killlist)
				{
					n2.Parent.DeleteChildTree(n2);
				}
			}
		}

		public void ForceCaptureofProjectPlans()
		{
			for (int step = 0; step < numberofProject; step++)
			{
				ProjectRunner pr = this.ProjectRunnerArray[step];
				if (pr != null)
				{
					//When we capture the plan, we delete any previous copy
					pr.CapturePlan(1); 
				}
			}
		}

		public void ForceRebuildFutureTimeSheets(NetworkProgressionGameFile gameFile, int current_day)
		{
			DayTimeSheet[] time_sheets = new DayTimeSheet[GameConstants.MAX_NUMBER_DAYS+1];
			for (int i = 0; i <= GameConstants.MAX_NUMBER_DAYS; ++i)
			{
				time_sheets[i] = new DayTimeSheet();
			}

			for (int step = 0; step < numberofProject; step++)
			{
				ProjectRunner pr = this.ProjectRunnerArray[step];
				if (pr != null)
				{
					pr.CaptureTimePlan(current_day, time_sheets);
				}
			}
			
			//Extract out the maximum staff levels as we need to write them into the timesheet xml
			Node staff_dev_int_limit_node = this.MyNodeTree.GetNamedNode("dev_staff");
			int DevIntMax = staff_dev_int_limit_node.GetIntAttribute("total", 0);
			Node staff_dev_ext_limit_node = this.MyNodeTree.GetNamedNode("dev_contractor");
			int DevExtMax = staff_dev_ext_limit_node.GetIntAttribute("total", 0);

			Node staff_test_int_limit_node = this.MyNodeTree.GetNamedNode("test_staff");
			int TestIntMax = staff_test_int_limit_node.GetIntAttribute("total", 0);
			Node staff_test_ext_limit_node = this.MyNodeTree.GetNamedNode("test_contractor");
			int TestExtMax = staff_test_ext_limit_node.GetIntAttribute("total", 0);

			//
			// Write the report out to a file...
			//
			/*
			 * <time_sheet maximum_int_dev_staff_count="" maximum_ext_dev_staff_count=""
			 *		maximum_int_test_staff_count="" maximum_ext_test_staff_count="" >
			 *	<day value="1">
			 *		<workers type="staff" stage="dev/test" working="true/false" number="x" />
			 *		<workers type="contracters" ... />
			 *  </day>
			 * </time_sheet>
			 */
			XmlDocument doc = new XmlDocument();
			XmlElement ts = doc.CreateElement("time_sheet");
			ts.SetAttribute("maximum_int_dev_staff_count", CONVERT.ToStr(DevIntMax));
			ts.SetAttribute("maximum_ext_dev_staff_count", CONVERT.ToStr(DevExtMax));
			ts.SetAttribute("maximum_int_test_staff_count", CONVERT.ToStr(TestIntMax));
			ts.SetAttribute("maximum_ext_test_staff_count", CONVERT.ToStr(TestExtMax));
			doc.AppendChild(ts);

			for (int day = 0; day < time_sheets.Length; day++)
			{
				if (day > (current_day - 1))
				{
					DayTimeSheet sheet = time_sheets[day];

					XmlElement nday = doc.CreateElement("day");
					nday.SetAttribute("value", CONVERT.ToStr(day));
					doc.DocumentElement.AppendChild(nday);
					//
					XmlElement worker = null;

					worker = doc.CreateElement("staff_int_dev_day_employed");
					nday.AppendChild(worker);
					worker.SetAttribute("num", CONVERT.ToStr(sheet.staff_int_dev_day_employed_count));

					worker = doc.CreateElement("staff_int_dev_day_idle");
					nday.AppendChild(worker);
					worker.SetAttribute("num", CONVERT.ToStr(sheet.staff_int_dev_day_idle_count));

					worker = doc.CreateElement("staff_int_test_day_employed");
					nday.AppendChild(worker);
					worker.SetAttribute("num", CONVERT.ToStr(sheet.staff_int_test_day_employed_count));

					worker = doc.CreateElement("staff_int_test_day_idle");
					nday.AppendChild(worker);
					worker.SetAttribute("num", CONVERT.ToStr(sheet.staff_int_test_day_idle_count));

					worker = doc.CreateElement("staff_ext_dev_day_employed");
					nday.AppendChild(worker);
					worker.SetAttribute("num", CONVERT.ToStr(sheet.staff_ext_dev_day_employed_count));

					worker = doc.CreateElement("staff_ext_dev_day_idle");
					nday.AppendChild(worker);
					worker.SetAttribute("num", CONVERT.ToStr(sheet.staff_ext_dev_day_idle_count));

					worker = doc.CreateElement("staff_ext_test_day_employed");
					nday.AppendChild(worker);
					worker.SetAttribute("num", CONVERT.ToStr(sheet.staff_ext_test_day_employed_count));

					worker = doc.CreateElement("staff_ext_test_day_idle");
					nday.AppendChild(worker);
					worker.SetAttribute("num", CONVERT.ToStr(sheet.staff_ext_test_day_idle_count));
				}
			}
			// Write the xml file out...

			string filename = gameFile.CurrentRoundDir;
			filename += "\\future_timesheet.xml";
			doc.Save(filename);
		}

		private void determinediff(day_work_report yesterday, day_work_report today, 
			out int dev_int_tasked,	 out int dev_int_worked,	out int dev_int_wasted,
			out int dev_ext_tasked,	 out int dev_ext_worked,	out int dev_ext_wasted,
			out int test_int_tasked, out int test_int_worked, out int test_int_wasted,
			out int test_ext_tasked, out int test_ext_worked, out int test_ext_wasted)
		{ 
			dev_int_tasked=0;
			dev_int_worked=0;
			dev_int_wasted=0;
			dev_ext_tasked=0;
			dev_ext_worked=0;
			dev_ext_wasted=0;
			test_int_tasked=0;
			test_int_worked=0;
			test_int_wasted=0;
			test_ext_tasked=0;
			test_ext_worked=0;
			test_ext_wasted = 0;

			if ((yesterday != null) & (today != null))
			{
				dev_int_tasked = Math.Max((today.dev_int_tasked - yesterday.dev_int_tasked), 0);
				dev_int_worked = Math.Max((today.dev_int_worked - yesterday.dev_int_worked), 0);
				dev_int_wasted = Math.Max((today.dev_int_wasted - yesterday.dev_int_wasted), 0);
				dev_ext_tasked = Math.Max((today.dev_ext_tasked - yesterday.dev_ext_tasked), 0);
				dev_ext_worked = Math.Max((today.dev_ext_worked - yesterday.dev_ext_worked), 0);
				dev_ext_wasted = Math.Max((today.dev_ext_wasted - yesterday.dev_ext_wasted), 0);
				test_int_tasked = Math.Max((today.test_int_tasked - yesterday.test_int_tasked), 0);
				test_int_worked = Math.Max((today.test_int_worked - yesterday.test_int_worked), 0);
				test_int_wasted = Math.Max((today.test_int_wasted - yesterday.test_int_wasted), 0);
				test_ext_tasked = Math.Max((today.test_ext_tasked - yesterday.test_ext_tasked), 0);
				test_ext_worked = Math.Max((today.test_ext_worked - yesterday.test_ext_worked), 0);
				test_ext_wasted = Math.Max((today.test_ext_wasted - yesterday.test_ext_wasted), 0);
			}
			else
			{
				if ((yesterday == null) & (today != null))
				{
					dev_int_tasked = Math.Max(today.dev_int_tasked, 0);
					dev_int_worked = Math.Max(today.dev_int_worked, 0);
					dev_int_wasted = Math.Max(today.dev_int_wasted, 0);
					dev_ext_tasked = Math.Max(today.dev_ext_tasked, 0);
					dev_ext_worked = Math.Max(today.dev_ext_worked, 0);
					dev_ext_wasted = Math.Max(today.dev_ext_wasted, 0);
					test_int_tasked = Math.Max(today.test_int_tasked, 0);
					test_int_worked = Math.Max(today.test_int_worked, 0);
					test_int_wasted = Math.Max(today.test_int_wasted, 0);
					test_ext_tasked = Math.Max(today.test_ext_tasked, 0);
					test_ext_worked = Math.Max(today.test_ext_worked, 0);
					test_ext_wasted = Math.Max(today.test_ext_wasted, 0);
				}
			}

		}

		/// <summary>
		/// We need to generate the profile of worker over the entire department per day
		/// We have a culminlative report of activity for each project recorded in the model
		/// and we can extract information from the log of events 
		/// The steps are 
		/// A, Extract all the projects event log information into a culminlative record per day
		/// B, Extract the difference between sucessive days int a sequence of time sheets 
		/// C, Build the past_days xml from that sequence of time sheets
		/// D, Save the xml into the required file
		/// </summary>
		/// <param name="gameFile"></param>
		/// <param name="current_day"></param>
		public void ForceRebuildPastTimeSheets(NetworkProgressionGameFile gameFile, int current_day)
		{
			//System.Diagnostics.Debug.WriteLine("FORCE REBUILD PAST TIMESHEETS");
			//clear the culmulative reports of how time we  
			daily_reps.Clear();

			int DisplayRound = gameFile.CurrentRound;
			ArrayList known_work_nodes = new ArrayList();
			//==================================================================
			//A, Identify all the projects that have been used in the round=====
			//==================================================================
			//NodeTree mynodetree = gameFile.GetNetworkModel(DisplayRound, GameFile.GamePhase.OPERATIONS);
			NodeTree mynodetree = this.MyNodeTree;

			Node projectsKnownNode = mynodetree.GetNamedNode("pm_projects_known");
			if (projectsKnownNode != null)
			{ 
				foreach (Node n in projectsKnownNode.getChildren())
				{
					string node_name= n.GetAttribute("value");
					if (node_name != "")
					{
						if (known_work_nodes.Contains(node_name) == false)
						{
							known_work_nodes.Add(node_name);
						}
					}
				}
			}

			//==================================================================
			//B, Extract the past execution information from all the projects 
			//==================================================================
			string filename_past = gameFile.GetRoundFile(DisplayRound, "NetworkIncidents.log", GameFile.GamePhase.OPERATIONS);
			BasicIncidentLogReader reader = new BasicIncidentLogReader(filename_past);
			foreach (string nn in known_work_nodes)
			{
				reader.WatchApplyAttributes(nn, new LogLineFoundDef.LineFoundHandler(reader_alteredNodeFound));
			}
			reader.Run();

			//==================================================================
			//C,Extract the difference between sucessive days int a sequence of time sheets
			//==================================================================
			//Clean the old past time sheets 
			for (int i = 0; i <= GameConstants.MAX_NUMBER_DAYS; ++i)
			{
				if (this.past_time_sheets[i] == null)
				{
					this.past_time_sheets[i] = new DayTimeSheet();
				}
			}

			for (int i = 1; i <= GameConstants.MAX_NUMBER_DAYS; ++i)
			{
				int dev_int_tasked = 0;
				int dev_int_worked = 0;
				int dev_int_wasted = 0;
				int dev_ext_tasked = 0;
				int dev_ext_worked = 0;
				int dev_ext_wasted = 0;
				int test_int_tasked = 0;
				int test_int_worked = 0;
				int test_int_wasted = 0;
				int test_ext_tasked = 0;
				int test_ext_worked = 0;
				int test_ext_wasted = 0;

				int daily_total_dev_int_tasked = 0;
				int daily_total_dev_int_worked = 0;
				int daily_total_dev_int_wasted = 0;
				int daily_total_dev_ext_tasked = 0;
				int daily_total_dev_ext_worked = 0;
				int daily_total_dev_ext_wasted = 0;
				int daily_total_test_int_tasked = 0;
				int daily_total_test_int_worked = 0;
				int daily_total_test_int_wasted = 0;
				int daily_total_test_ext_tasked = 0;
				int daily_total_test_ext_worked = 0;
				int daily_total_test_ext_wasted = 0;

				foreach (string prjname in daily_reps.Keys)
				{
					Hashtable tmp = (Hashtable)daily_reps[prjname];
					if (tmp != null)
					{
						day_work_report dwr_yesterday = (day_work_report)tmp[i - 1];
						day_work_report dwr_today = (day_work_report)tmp[i];

						determinediff(dwr_yesterday, dwr_today,
							out dev_int_tasked, out dev_int_worked, out dev_int_wasted,
							out dev_ext_tasked, out dev_ext_worked, out dev_ext_wasted,
							out test_int_tasked, out test_int_worked, out test_int_wasted,
							out test_ext_tasked, out test_ext_worked, out test_ext_wasted);

						daily_total_dev_int_tasked += dev_int_tasked;
						daily_total_dev_int_worked += dev_int_worked;
						daily_total_dev_int_wasted += dev_int_wasted;
						daily_total_dev_ext_tasked += dev_ext_tasked;
						daily_total_dev_ext_worked += dev_ext_worked;
						daily_total_dev_ext_wasted += dev_ext_wasted;
						daily_total_test_int_tasked += test_int_tasked;
						daily_total_test_int_worked += test_int_worked;
						daily_total_test_int_wasted += test_int_wasted;
						daily_total_test_ext_tasked += test_ext_tasked;
						daily_total_test_ext_worked += test_ext_worked;
						daily_total_test_ext_wasted += test_ext_wasted;
					}
				}

				if (this.past_time_sheets[i] != null)
				{
					this.past_time_sheets[i].staff_int_dev_day_employed_count = daily_total_dev_int_worked;
					this.past_time_sheets[i].staff_int_dev_day_idle_count = daily_total_dev_int_wasted;
					this.past_time_sheets[i].staff_int_test_day_employed_count = daily_total_test_int_worked;
					this.past_time_sheets[i].staff_int_test_day_idle_count = daily_total_test_int_wasted;
					this.past_time_sheets[i].staff_ext_dev_day_employed_count = daily_total_dev_ext_worked;
					this.past_time_sheets[i].staff_ext_dev_day_idle_count = daily_total_dev_ext_wasted;
					this.past_time_sheets[i].staff_ext_test_day_employed_count = daily_total_test_ext_worked;
					this.past_time_sheets[i].staff_ext_test_day_idle_count = daily_total_test_ext_wasted;

					//string dbg1 = "day:" + CONVERT.ToStr(i) + " ";
					//dbg1 += "TS_SDE:"+ CONVERT.ToStr(this.past_time_sheets[i].staff_int_dev_day_employed_count
					//dbg1 += "TS_SDI:" + CONVERT.ToStr(this.past_time_sheets[i].staff_int_dev_day_idle_count
					//dbg1 += "TS_STE:" + CONVERT.ToStr(this.past_time_sheets[i].staff_int_test_day_employed_count
					//dbg1 += "TS_STI:" + CONVERT.ToStr(this.past_time_sheets[i].staff_int_test_day_idle_count 
					//dbg1 += "TS_CDE:" + CONVERT.ToStr(this.past_time_sheets[i].staff_ext_dev_day_employed_count 
					//dbg1 += "TS_CDI:" + CONVERT.ToStr(this.past_time_sheets[i].staff_ext_dev_day_idle_count
					//dbg1 += "TS_CTE:" + CONVERT.ToStr(this.past_time_sheets[i].staff_ext_test_day_employed_count
					//dbg1 += "TS_CTI:" + CONVERT.ToStr(this.past_time_sheets[i].staff_ext_test_day_idle_count
					//System.Diagnostics.Debug.WriteLine(dbg1);
				}
			}

			//==================================================================
			//C,Build the past_days xml from that sequence of time sheets
			//==================================================================
			//Extract out the maximum staff levels as we need to write them into the timesheet xml
			Node staff_dev_int_limit_node = this.MyNodeTree.GetNamedNode("dev_staff");
			int DevIntMax = staff_dev_int_limit_node.GetIntAttribute("total", 0);
			Node staff_dev_ext_limit_node = this.MyNodeTree.GetNamedNode("dev_contractor");
			int DevExtMax = staff_dev_ext_limit_node.GetIntAttribute("total", 0);

			Node staff_test_int_limit_node = this.MyNodeTree.GetNamedNode("test_staff");
			int TestIntMax = staff_test_int_limit_node.GetIntAttribute("total", 0);
			Node staff_test_ext_limit_node = this.MyNodeTree.GetNamedNode("test_contractor");
			int TestExtMax = staff_test_ext_limit_node.GetIntAttribute("total", 0);

			//
			// Write the report out to a file...
			//
			/*
			 * <time_sheet maximum_int_dev_staff_count="" maximum_ext_dev_staff_count=""
			 *		maximum_int_test_staff_count="" maximum_ext_test_staff_count="" >
			 *	<day value="1">
			 *		<workers type="staff" stage="dev/test" working="true/false" number="x" />
			 *		<workers type="contracters" ... />
			 *  </day>
			 * </time_sheet>
			 */
			int day = current_day;
			XmlDocument doc = new XmlDocument();
			XmlElement ts = doc.CreateElement("time_sheet");
			ts.SetAttribute("maximum_int_dev_staff_count", CONVERT.ToStr(DevIntMax));
			ts.SetAttribute("maximum_ext_dev_staff_count", CONVERT.ToStr(DevExtMax));
			ts.SetAttribute("maximum_int_test_staff_count", CONVERT.ToStr(TestIntMax));
			ts.SetAttribute("maximum_ext_test_staff_count", CONVERT.ToStr(TestExtMax));
			doc.AppendChild(ts);

			for (int i = 1; i <= GameConstants.MAX_NUMBER_DAYS; ++i)
			{
				DayTimeSheet sheet = past_time_sheets[i];
				if (i <= current_day)
				{
					XmlElement nday = doc.CreateElement("day");
					nday.SetAttribute("value", CONVERT.ToStr(i));
					doc.DocumentElement.AppendChild(nday);
					//

					XmlElement worker = null;

					worker = doc.CreateElement("staff_int_dev_day_employed");
					nday.AppendChild(worker);
					worker.SetAttribute("num", CONVERT.ToStr(sheet.staff_int_dev_day_employed_count));

					worker = doc.CreateElement("staff_int_dev_day_idle");
					nday.AppendChild(worker);
					worker.SetAttribute("num", CONVERT.ToStr(sheet.staff_int_dev_day_idle_count));

					worker = doc.CreateElement("staff_int_test_day_employed");
					nday.AppendChild(worker);
					worker.SetAttribute("num", CONVERT.ToStr(sheet.staff_int_test_day_employed_count));

					worker = doc.CreateElement("staff_int_test_day_idle");
					nday.AppendChild(worker);
					worker.SetAttribute("num", CONVERT.ToStr(sheet.staff_int_test_day_idle_count));

					worker = doc.CreateElement("staff_ext_dev_day_employed");
					nday.AppendChild(worker);
					worker.SetAttribute("num", CONVERT.ToStr(sheet.staff_ext_dev_day_employed_count));

					worker = doc.CreateElement("staff_ext_dev_day_idle");
					nday.AppendChild(worker);
					worker.SetAttribute("num", CONVERT.ToStr(sheet.staff_ext_dev_day_idle_count));

					worker = doc.CreateElement("staff_ext_test_day_employed");
					nday.AppendChild(worker);
					worker.SetAttribute("num", CONVERT.ToStr(sheet.staff_ext_test_day_employed_count));

					worker = doc.CreateElement("staff_ext_test_day_idle");
					nday.AppendChild(worker);
					worker.SetAttribute("num", CONVERT.ToStr(sheet.staff_ext_test_day_idle_count));

				}
			}

			string filename = gameFile.CurrentRoundDir;
			filename += "\\past_timesheet.xml";
			doc.Save(filename);
		}

		void reader_alteredNodeFound(object sender, string key, string line, double time)
		{
			string st = key;
			//System.Diagnostics.Debug.WriteLine("Found ("+((int)time).ToString()+") --> "+line);

			string prj_name = BasicIncidentLogReader.ExtractValue(line,"i_name");

			int report_day = (((int)time) / 60)+1;

			int dev_int_tasked = CONVERT.ParseIntSafe(BasicIncidentLogReader.ExtractValue(line,"dev_int_tasked"),0);
			int dev_int_worked = CONVERT.ParseIntSafe(BasicIncidentLogReader.ExtractValue(line,"dev_int_worked"),0);
			int dev_int_wasted= CONVERT.ParseIntSafe(BasicIncidentLogReader.ExtractValue(line,"dev_int_wasted"),0);
			int dev_ext_tasked= CONVERT.ParseIntSafe(BasicIncidentLogReader.ExtractValue(line,"dev_ext_tasked"),0);
			int dev_ext_worked= CONVERT.ParseIntSafe(BasicIncidentLogReader.ExtractValue(line,"dev_ext_worked"),0);
			int dev_ext_wasted= CONVERT.ParseIntSafe(BasicIncidentLogReader.ExtractValue(line,"dev_ext_wasted"),0);
			int test_int_tasked= CONVERT.ParseIntSafe(BasicIncidentLogReader.ExtractValue(line,"test_int_tasked"),0);
			int test_int_worked= CONVERT.ParseIntSafe(BasicIncidentLogReader.ExtractValue(line,"test_int_worked"),0);
			int test_int_wasted= CONVERT.ParseIntSafe(BasicIncidentLogReader.ExtractValue(line,"test_int_wasted"),0);
			int test_ext_tasked= CONVERT.ParseIntSafe(BasicIncidentLogReader.ExtractValue(line,"test_ext_tasked"),0);
			int test_ext_worked= CONVERT.ParseIntSafe(BasicIncidentLogReader.ExtractValue(line,"test_ext_worked"),0);
			int test_ext_wasted= CONVERT.ParseIntSafe(BasicIncidentLogReader.ExtractValue(line,"test_ext_wasted"),0);
		
			if (daily_reps.Contains(prj_name)== false)
			{
				daily_reps.Add(prj_name, new Hashtable());
			}
			Hashtable reps = (Hashtable) daily_reps[prj_name];
			if (reps.Contains(report_day))
			{
				//already have a report for that day
				day_work_report dwr = (day_work_report)reps[report_day];
				if (dwr != null)
				{
					dwr.dev_int_tasked += dev_int_tasked;
					dwr.dev_int_worked += dev_int_worked;
					dwr.dev_int_wasted += dev_int_wasted;
					dwr.dev_ext_tasked += dev_ext_tasked;
					dwr.dev_ext_worked += dev_ext_worked;
					dwr.dev_ext_wasted += dev_ext_wasted;
					dwr.test_int_tasked += test_int_tasked;
					dwr.test_int_worked += test_int_worked;
					dwr.test_int_wasted += test_int_wasted;
					dwr.test_ext_tasked += test_ext_tasked;
					dwr.test_ext_worked += test_ext_worked;
					dwr.test_ext_wasted += test_ext_wasted;
				}
			}
			else
			{
				//Dont have any day for that day
				day_work_report dwr = new day_work_report();
				dwr.dev_int_tasked = dev_int_tasked;
				dwr.dev_int_worked = dev_int_worked;
				dwr.dev_int_wasted = dev_int_wasted;
				dwr.dev_ext_tasked = dev_ext_tasked;
				dwr.dev_ext_worked = dev_ext_worked;
				dwr.dev_ext_wasted = dev_ext_wasted;
				dwr.test_int_tasked = test_int_tasked;
				dwr.test_int_worked = test_int_worked;
				dwr.test_int_wasted = test_int_wasted;
				dwr.test_ext_tasked = test_ext_tasked;
				dwr.test_ext_worked = test_ext_worked;
				dwr.test_ext_wasted = test_ext_wasted;
				reps.Add(report_day, dwr);
			}
		}

			//if (day_culm_reports.ContainsKey(report_day))
			//{
			//  day_work_report dwr = day_culm_reports[report_day];
			//  if (dwr != null)
			//  {
			//    dwr.dev_int_tasked += dev_int_tasked;
			//    dwr.dev_int_worked += dev_int_worked;
			//    dwr.dev_int_wasted += dev_int_wasted;
			//    dwr.dev_ext_tasked += dev_ext_tasked;
			//    dwr.dev_ext_worked += dev_ext_worked;
			//    dwr.dev_ext_wasted += dev_ext_wasted;
			//    dwr.test_int_tasked += test_int_tasked;
			//    dwr.test_int_worked += test_int_worked;
			//    dwr.test_int_wasted += test_int_wasted;
			//    dwr.test_ext_tasked += test_ext_tasked;
			//    dwr.test_ext_worked += test_ext_worked;
			//    dwr.test_ext_wasted += test_ext_wasted;
			//  }
			//}
			//else
			//{
			//  day_work_report dwr = new day_work_report();
			//  dwr.dev_int_tasked = dev_int_tasked;
			//  dwr.dev_int_worked = dev_int_worked;
			//  dwr.dev_int_wasted = dev_int_wasted;
			//  dwr.dev_ext_tasked = dev_ext_tasked;
			//  dwr.dev_ext_worked = dev_ext_worked;
			//  dwr.dev_ext_wasted = dev_ext_wasted;
			//  dwr.test_int_tasked = test_int_tasked;
			//  dwr.test_int_worked = test_int_worked;
			//  dwr.test_int_wasted = test_int_wasted;
			//  dwr.test_ext_tasked = test_ext_tasked;
			//  dwr.test_ext_worked = test_ext_worked;
			//  dwr.test_ext_wasted = test_ext_wasted;
			//  day_culm_reports.Add(report_day, dwr);
			//}
		//}

		public void RunEndOfRoundCalculation()
		{
			ArrayList al = new ArrayList();
			ArrayList killlist = new ArrayList();

			//Get the list of Required Projects 
			ArrayList al_required_names = ProjectLookup.TheInstance.getRegulationProjectList();

			int NumberofProjects_Completed = 0;
			int NumberofRegulationProjects_Required = al_required_names.Count;
			int NumberofRegulationProjects_Achieved = 0;
			int NumberofMissingRegulationProjects = 0;

			int Total_FinFineForMissingRegulationProjects = 0;  //Finiancial penalty for missied Regulation Projects
			int Total_GainFineForMissingRegulationProjects = 0; //Gain penalty for missied Regulation Projects

			int TotalProjectGainAchieved = 0;   // What we made on out projects 
			int TotalNetProjectGain = 0;				// The overall total (total project gain - gain fines)

			int TotalBudgetAllowed = 0;
			int TotalProjectSpend = 0;
			int TotalRevenueAchieved = 0;
			int TotalProfitLoss = 0;

			int dev_int_tasked = 0;
			int dev_int_worked = 0;
			int dev_int_wasted = 0;
			int dev_ext_tasked = 0;
			int dev_ext_worked = 0;
			int dev_ext_wasted = 0;

			int test_int_tasked = 0;
			int test_int_worked = 0;
			int test_int_wasted = 0;
			int test_ext_tasked = 0;
			int test_ext_worked = 0;
			int test_ext_wasted = 0;

			int total_dev_int_tasked = 0;
			int total_dev_int_worked = 0;
			int total_dev_int_wasted = 0;
			int total_dev_ext_tasked = 0;
			int total_dev_ext_worked = 0;
			int total_dev_ext_wasted = 0;

			int total_test_int_tasked = 0;
			int total_test_int_worked = 0;
			int total_test_int_wasted = 0;
			int total_test_ext_tasked = 0;
			int total_test_ext_worked = 0;
			int total_test_ext_wasted = 0;

			int worker_internal_hourpayrate = 250;
			int worker_external_hourpayrate = 750;

			int totalCostReduction = 0;

			for (int step = 0; step < numberofProject; step++)
			{
				ProjectRunner pr = this.ProjectRunnerArray[step];
				if (pr != null)
				{
					int tmpProjectID = pr.getProjectID();
					bool isRegulationProject = al_required_names.Contains(tmpProjectID);
					bool isInstalled = pr.InState(emProjectOperationalState.PROJECT_STATE_INSTALLED_OK);
					bool isCompleted = pr.InState(emProjectOperationalState.PROJECT_STATE_COMPLETED);

					if ((isRegulationProject))
					{
						if (isInstalled | isCompleted)
						{
							NumberofRegulationProjects_Achieved++;
						}
					}
					if (isInstalled | isCompleted)
					{
						NumberofProjects_Completed++;
					}
					//Take account of the Spend for this project
					TotalProjectSpend += pr.getSpend();
					//Workout the gain achieved 
					pr.DetermineGainAchieved();
					int this_projectGainAchieved = pr.getGainAchieved();

					TotalProjectGainAchieved += this_projectGainAchieved;
					totalCostReduction += pr.getCostReductionAchieved();

					//Extract the Work Days 
					pr.getWorkdataForDev(out dev_int_tasked, out dev_int_worked, out dev_int_wasted,
						out dev_ext_tasked, out dev_ext_worked, out dev_ext_wasted);

					pr.getWorkdataForTest(out test_int_tasked, out test_int_worked, out test_int_wasted,
						out test_ext_tasked, out test_ext_worked, out test_ext_wasted);

					total_dev_int_tasked += dev_int_tasked;
					total_dev_int_worked += dev_int_worked;
					total_dev_int_wasted += dev_int_wasted;
					total_dev_ext_tasked += dev_ext_tasked;
					total_dev_ext_worked += dev_ext_worked;
					total_dev_ext_wasted += dev_ext_wasted;

					total_test_int_tasked += test_int_tasked;
					total_test_int_worked += test_int_worked;
					total_test_int_wasted += test_int_wasted;
					total_test_ext_tasked += test_ext_tasked;
					total_test_ext_worked += test_ext_worked;
					total_test_ext_wasted += test_ext_wasted;

					//all the projects have the same payrate but it defined inside the pa
					//and we need it up here, refactor to external modifiable class
					worker_internal_hourpayrate = pr.getWorkerInternal_HourlyPayRate();
					worker_external_hourpayrate = pr.getWorkerExternal_HourlyPayRate();
				}
			}
			//Need to take account of any cancelled project spend 
			Node pmo_budget_node = this.MyNodeTree.GetNamedNode("pmo_budget");
			if (pmo_budget_node != null)
			{
				int cancellation_money = pmo_budget_node.GetIntAttribute("cancellation_money",0);
				TotalProjectSpend += cancellation_money;
				TotalBudgetAllowed = pmo_budget_node.GetIntAttribute("budget_allowed", 0);
			}

			//Need to Update the project results 
			Node resources_results_node = this.MyNodeTree.GetNamedNode("resources_results");
			if (resources_results_node != null)
			{
				int int_tasked_days = total_dev_int_tasked + total_test_int_tasked;
				int ext_tasked_days = total_dev_ext_tasked + total_test_ext_tasked;

				int int_wasted_days = total_dev_int_wasted + total_test_int_wasted;
				int ext_wasted_days = total_dev_ext_wasted + total_test_ext_wasted;

				int int_wasted_cost = int_wasted_days * worker_internal_hourpayrate;
				int ext_wasted_cost = ext_wasted_days * worker_external_hourpayrate;

				resources_results_node.SetAttribute("int_tasked_days", CONVERT.ToStr(int_tasked_days));
				resources_results_node.SetAttribute("int_wasted_days", CONVERT.ToStr(int_wasted_days));
				resources_results_node.SetAttribute("int_wasted_cost", CONVERT.ToStr(int_wasted_cost));

				resources_results_node.SetAttribute("ext_tasked_days", CONVERT.ToStr(ext_tasked_days));
				resources_results_node.SetAttribute("ext_wasted_days", CONVERT.ToStr(ext_wasted_days));
				resources_results_node.SetAttribute("ext_wasted_cost", CONVERT.ToStr(ext_wasted_cost));

				resources_results_node.SetAttribute("total_wasted_cost", CONVERT.ToStr(int_wasted_cost + ext_wasted_cost));
			}

			//Work out how many Regulation projects are missing 
			NumberofMissingRegulationProjects = NumberofRegulationProjects_Required - NumberofRegulationProjects_Achieved;

			//Determine Fines for what is Missing
			Total_FinFineForMissingRegulationProjects = NumberofMissingRegulationProjects * fin_penalty_per_missed_reg_project;
			Total_GainFineForMissingRegulationProjects = NumberofMissingRegulationProjects * gain_penalty_per_missed_reg_project;
			Node operationalResults = MyNodeTree.GetNamedNode("operational_results");
			Total_FinFineForMissingRegulationProjects += operationalResults.GetIntAttribute("fines", 0);

			// Cost avoidance.
			int totalCostAvoidedBenefit = NumberofRegulationProjects_Achieved * this.costavoid_benefit_per_completed_reg_project;

			//Determine Overall NET Gain 
			TotalNetProjectGain = TotalProjectGainAchieved - Total_GainFineForMissingRegulationProjects;

			//Need to Update the project results 
			Node projects_results_node = this.MyNodeTree.GetNamedNode("projects_results");
			projects_results_node.SetAttribute("missed_reg_projects_total", CONVERT.ToStr(NumberofMissingRegulationProjects));
			projects_results_node.SetAttribute("missed_reg_projects_fines", CONVERT.ToStr(Total_FinFineForMissingRegulationProjects));
			projects_results_node.SetAttribute("project_completed", CONVERT.ToStr(NumberofProjects_Completed));

			Node operational_results_node = this.MyNodeTree.GetNamedNode("operational_results");
			operational_results_node.SetAttribute("total_gain", CONVERT.ToStr(TotalNetProjectGain));

			Node roundResults = MyNodeTree.GetNamedNode("round_results");
			roundResults.SetAttribute("gain_achieved", TotalNetProjectGain);
			roundResults.SetAttribute("cost_reduction_achieved", totalCostReduction);
			roundResults.SetAttribute("cost_avoidance_achieved", totalCostAvoidedBenefit);

			int winLevel = 230;
			int playerDrop = 122;
			LibCore.BasicXmlDocument raceDoc = LibCore.BasicXmlDocument.CreateFromFile(LibCore.AppInfo.TheInstance.Location + "data/race.xml");
			foreach (XmlElement element in raceDoc.DocumentElement.ChildNodes)
			{
				if (element.Name == "round")
				{
					XmlElement numberNode = element.SelectSingleNode("num") as XmlElement;
					if ((numberNode != null) && (CONVERT.ParseIntSafe(numberNode.InnerText, 0) == round))
					{
						winLevel = CONVERT.ParseIntSafe(element.SelectSingleNode("winLevel").InnerText, 0);
						playerDrop = CONVERT.ParseIntSafe(element.SelectSingleNode("td").InnerText, 0);
						break;
					}
				}
			}

			Node financial_results_node = this.MyNodeTree.GetNamedNode("fin_results");
			{
				//Extract the fixed cost from the stored data 
				int fixed_costs = financial_results_node.GetIntAttribute("fixed_costs", 0);

				TotalRevenueAchieved = money_per_point * 1000 * (winLevel - playerDrop + TotalNetProjectGain);
				
				//store the Total Revenue Achieved 
				financial_results_node.SetAttribute("total_revenue", CONVERT.ToStr(TotalRevenueAchieved));
				//store the Total Project Spend 
				financial_results_node.SetAttribute("total_project_spend", CONVERT.ToStr(TotalProjectSpend));

				int TotalBusinessBenefit = TotalRevenueAchieved;
				Node tmp_round_results_node = this.MyNodeTree.GetNamedNode("round_results");
				if (tmp_round_results_node != null)
				{
					int tmp_cost_reduction_achieved = tmp_round_results_node.GetIntAttribute("cost_reduction_achieved", 0);
					TotalBusinessBenefit = TotalBusinessBenefit + tmp_cost_reduction_achieved;
				}
				//store the Total Business Benefit
				financial_results_node.SetAttribute("total_business_benefit", CONVERT.ToStr(TotalBusinessBenefit));

				TotalProfitLoss = TotalBusinessBenefit - fixed_costs - TotalBudgetAllowed - Total_FinFineForMissingRegulationProjects;
				//store the Total Profit Loss 
				financial_results_node.SetAttribute("profitloss", CONVERT.ToStr(TotalProfitLoss));
			}
		}

	}
}
