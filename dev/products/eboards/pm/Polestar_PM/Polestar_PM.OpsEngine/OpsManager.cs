using System;
using System.Collections;
using System.IO;
using System.Xml;

using Network;

using GameManagement;

using LibCore;
using CoreUtils;
using Polestar_PM.DataLookup;

namespace Polestar_PM.OpsEngine
{
	/// <summary>
	/// The ops Manager handles all the 
	///   blocking days (both round 1 and round 2)
	///   FixedScheduleChnage (round 1)
	///   Servers Upgrades (round 2)
	///   
	/// While processing an Upgrade, the Ops Manager raises a block that prevents project installations
	/// The Blocking days are created by timed node createion steps in the required round 
	/// 
	/// Very simple structure with a tag with the flags and a list of pending children  
	/// 
	/// This used to be triggered of the day node change but it must be called before the projects
	/// so this is now called from the PM_Ops_Engine so that we can force it to happen before the 
	/// projects get called. 
	/// 
	/// </summary>
	public class OpsManager : OpsReader
	{
		NetworkProgressionGameFile gameFile;

		public OpsManager(NodeTree tree, NetworkProgressionGameFile gameFile):base (tree)
		{
			this.gameFile = gameFile;
		}

		private void affectPMOBudget(int amount)
		{
			if (PMO_BudgetNode != null)
			{
				int current_left = PMO_BudgetNode.GetIntAttribute("budget_left",0);
				int current_spend = PMO_BudgetNode.GetIntAttribute("budget_spent",0);
				
				current_left -= amount;
				current_spend += amount;
				
				PMO_BudgetNode.SetAttribute("budget_left",CONVERT.ToStr(current_left));
				PMO_BudgetNode.SetAttribute("budget_spent",CONVERT.ToStr(current_spend));
			}
		}

		/// <summary>
		/// At the end of the Round, we need to examine any operational items that were required. 
		/// Basically we need to aplly fines for any missing operational items 
		/// This could handles both FSC and Change Cards (Just set the xml tag "required") 
		/// </summary>
		public void RunEndOfRoundCalculation(int round)
		{
			// Aaargh! rwriting back into the model again!
			// Round1 fines then exist in the round 2 network and get applied again of course!
			//
			//if (ops_round_results_node != null)
			{
				int fines = 0; // ops_round_results_node.GetIntAttribute("fines", 0);
				int missed_items = ops_round_results_node.GetIntAttribute("missed_items",0);

				//==================================================================================
				//==Check through the different fsc items(have we done the ones that we should have)
				//==================================================================================
				// Only do for round 1 and if IT enabled
				if ((round == 1) && true)
				{
					foreach (Node fsc_node in fsc_list_node.getChildren())
					{
						string name = fsc_node.GetAttribute("name");
						string status = fsc_node.GetAttribute("status");
						bool isRequired = fsc_node.GetBooleanAttribute("required", false);
						int fine_cost = fsc_node.GetIntAttribute("fine_cost", 0);

						if (isRequired)
						{
							if (status.ToLower() != "done")
							{
								if (fine_cost > 0)
								{
									fines += fine_cost;
								}
								missed_items++;
							}
						}
					}
				}

				//==================================================================================
				//==Check through the different Change items(have we done the ones that we should have)
				//==================================================================================
				if ((round == 1) && true)
				{
					foreach (Node chg_node in changecard_list_node.getChildren())
					{
						string name = chg_node.GetAttribute("name");
						string status = chg_node.GetAttribute("status");
						bool isRequired = chg_node.GetBooleanAttribute("required", false);
						int fine_cost = chg_node.GetIntAttribute("fine_cost", 0);

						if (isRequired)
						{
							if (status.ToLower() != "done")
							{
								if (fine_cost > 0)
								{
									fines += fine_cost;
								}
								missed_items++;
							}
						}
					}
				}
				ops_round_results_node.SetAttribute("fines",CONVERT.ToStr(fines));
				ops_round_results_node.SetAttribute("missed_items",CONVERT.ToStr(missed_items));

				System.Diagnostics.Debug.WriteLine("OPSManager ENDEX fines:"+CONVERT.ToStr(fines));
				System.Diagnostics.Debug.WriteLine("OPSManager ENDEX missed_items:"+CONVERT.ToStr(missed_items));
			}
		}

		/// <summary>
		/// We are upgrading a machine based on a FSC order.
		/// This is pretty much guarateed to work as the locations are predefined 
		/// </summary>
		/// <param name="job_location">which node to update</param>
		/// <param name="job_diskchange">How Much disk to alter</param>
		/// <param name="job_memchange">How Much memory to alter</param>
		/// <param name="job_moneycost">how much to change the PMO</param>
		/// <param name="job_rx_nodename">which item to mark as done</param>
		/// <returns></returns>
		private bool doWorkFor_FSC_Upgrade(string job_location, int job_diskchange, int job_memchange, 
			int job_moneycost, string job_rx_nodename)
		{
			bool Opsuccess = false;
			
			Node target_node = this.MyNodeTree.GetNamedNode(job_location);
			if (target_node != null)
			{
				int disk = target_node.GetIntAttribute("disk",0);
				int mem = target_node.GetIntAttribute("mem",0);
				int disk_upgraded = target_node.GetIntAttribute("count_disk_upgrades",0);
				int mem_upgraded= target_node.GetIntAttribute("count_mem_upgrades",0);

				if (job_diskchange != 0)
				{
					disk = disk + job_diskchange;
					target_node.SetAttribute("disk",CONVERT.ToStr(disk));
					target_node.SetAttribute("count_disk_upgrades",CONVERT.ToStr(disk_upgraded+1));
				}
				if (job_memchange != 0)
				{
					mem = mem + job_memchange;
					target_node.SetAttribute("mem",CONVERT.ToStr(mem));
					target_node.SetAttribute("count_mem_upgrades",CONVERT.ToStr(mem_upgraded+1));
				}
				if (this.PMO_BudgetNode != null)
				{
					if (job_moneycost>0)
					{
						affectPMOBudget(job_moneycost);
					}
				}
				if (job_rx_nodename != "")
				{
					Node fsc_node = this.MyNodeTree.GetNamedNode(job_rx_nodename);
					if (fsc_node != null)
					{
						fsc_node.SetAttribute("status","done");
					}
				}
				Opsuccess = true;
			}
			return Opsuccess;
		}

		/// <summary>
		/// This is a normal Round 2 defined machine Upgrade 
		/// This is pretty much guarateed to work as all servers allow upgrades 
		/// </summary>
		/// <param name="job_location"></param>
		/// <param name="job_diskchange"></param>
		/// <param name="job_memchange"></param>
		/// <param name="job_moneycost"></param>
		/// <param name="job_rx_nodename"></param>
		/// <returns></returns>
		private bool doWorkFor_Normal_Upgrade(string job_location, int job_diskchange, int job_memchange, 
			int job_moneycost, string job_rx_nodename)
		{
			bool Opsuccess = false;

			Node target_node = this.MyNodeTree.GetNamedNode(job_location);
			if (target_node != null)
			{
				int disk = target_node.GetIntAttribute("disk",0);
				int mem = target_node.GetIntAttribute("mem",0);
				int disk_upgraded = target_node.GetIntAttribute("count_disk_upgrades",0);
				int mem_upgraded= target_node.GetIntAttribute("count_mem_upgrades",0);

				if (job_diskchange != 0)
				{
					disk = disk + job_diskchange;
					target_node.SetAttribute("disk",CONVERT.ToStr(disk));
					target_node.SetAttribute("count_disk_upgrades",CONVERT.ToStr(disk_upgraded+1));
				}
				if (job_memchange != 0)
				{
					mem = mem + job_memchange;
					target_node.SetAttribute("mem",CONVERT.ToStr(mem));
					target_node.SetAttribute("count_mem_upgrades",CONVERT.ToStr(mem_upgraded+1));
				}
				if (this.PMO_BudgetNode != null)
				{
					if (job_moneycost>0)
					{
						affectPMOBudget(job_moneycost);
					}
				}
				if (job_rx_nodename != "")
				{
					Node req_node = this.MyNodeTree.GetNamedNode(job_rx_nodename);
					if (req_node != null)
					{
						req_node.SetAttribute("status","done");
					}
				}
				Opsuccess = true;
			}
			return Opsuccess;
		}


		/// <summary>
		///  This is the work for a Round 1 Change Card 
		///  This could fail as the players define the location and the day
		/// </summary>
		/// <param name="job_location"></param>
		/// <param name="card_change_id"></param>
		/// <param name="job_moneycost"></param>
		/// <param name="job_diskchange"></param>
		/// <param name="job_memchange"></param>
		/// <param name="job_platform_required"></param>
		/// <param name="job_rx_nodename"></param>
		/// <param name="new_cc_appname"></param>
		/// <param name="errmsg"></param>
		/// <returns></returns>
		private bool doWorkForChangeCard(string job_location, int card_change_id, int job_moneycost, 
			int job_diskchange, int job_memchange, string job_platform_required, 
			string job_rx_nodename, string new_cc_appname, out string errmsg)
		{
			bool OpSuccess = false;
			errmsg = "";

			Node target_node = this.MyNodeTree.GetNamedNode(job_location);

			AppInstaller ai = new AppInstaller(this.MyNodeTree);

			OpSuccess = ai.install_change_app(card_change_id, job_memchange, job_diskchange, 
				job_platform_required, new_cc_appname, job_location, out errmsg);

			//Handle the Money (if needed)
			if (this.PMO_BudgetNode != null)
			{
				if (job_moneycost>0)
				{
					affectPMOBudget(job_moneycost);
				}
			}

			if (job_rx_nodename != "")
			{
				Node req_node = this.MyNodeTree.GetNamedNode(job_rx_nodename);
				if (req_node != null)
				{
					if  (OpSuccess==false)
					{
						req_node.SetAttribute("status","failled");
					}
					else
					{
						req_node.SetAttribute("status","done");
					}
				}
			}
			ai.Dispose();
			return OpSuccess;
		}

		private void doWork(int currentday, Node jobNode, 
			out string jobname, out bool installing, out string errmsg)
		{
			string job_type; 
			string job_action; 
			string job_location; 
			string job_display;
			int job_memchange; 
			int job_diskchange; 
			int job_moneycost;
			int job_duration;
			string job_rx_nodename; 
			string job_app_name;
			int job_cardChangeID=0;
			string job_platformRequired = "";

			jobname="";
			installing = false;
			errmsg = "";
			bool job_success = false;

			if (jobNode != null)
			{
				job_type = jobNode.GetAttribute("type");
				job_action = jobNode.GetAttribute("action");
				job_location = jobNode.GetAttribute("location");
				job_display = jobNode.GetAttribute("display");
				job_memchange = jobNode.GetIntAttribute("memory_change",0);
				job_diskchange = jobNode.GetIntAttribute("disk_change",0);
				job_moneycost = jobNode.GetIntAttribute("money_cost",0);
				job_rx_nodename = jobNode.GetAttribute("rx_nodename");
				job_duration = jobNode.GetIntAttribute("duration",0);

				jobname = job_display;

				switch (job_action.ToLower())
				{
					case "upgrade_fsc_app":
					case "rebuild_fsc_app":
						job_success = doWorkFor_FSC_Upgrade(job_location, job_diskchange, job_memchange, 
              job_moneycost, job_rx_nodename);
						AddOpsActivityNote(currentday,job_action.ToLower(),job_location,job_success);
						installing = true;
						break;
					case "upgrade_memory":
					case "upgrade_disk":
					case "upgrade_both":
						job_success = doWorkFor_Normal_Upgrade(job_location, job_diskchange, job_memchange, 
							job_moneycost, job_rx_nodename);
						AddOpsActivityNote(currentday,job_action.ToLower(),job_location,job_success);
						installing = true;
						break;
					case "install_cc_app":
						//Extract the extra parameters 
						job_app_name = jobNode.GetAttribute("cc_appname");
						job_cardChangeID = jobNode.GetIntAttribute("cc_card_change_id",0);
						job_platformRequired = jobNode.GetAttribute("cc_platform");

						//perform the work 
						job_success = doWorkForChangeCard(job_location, job_cardChangeID, job_moneycost, 
							job_diskchange, job_memchange, job_platformRequired, job_rx_nodename, job_app_name,
							out errmsg);
						AddOpsActivityNote(currentday,job_action.ToLower(),job_location,job_success);
						installing = true;
						break;
					case "blockday": //no work need required 
						break;
				}
				//now set the current job to inprogress
				jobNode.SetAttribute("status","inprogress");
			}
		}

		public void handleOperationalWorkForDay(int currentday)
		{
			ArrayList jobs_in_progress = new ArrayList();
			ArrayList jobs_todo_unscheduled = new ArrayList(); //FSC and Upgrades
			ArrayList jobs_todo_scheduled = new ArrayList();   //Blocking Days
			string job_displayname = "";
			bool setBlockFlag = false;
			bool installing = false;
			string err_msg="";

			//clear the project block 
			ops_worklist_node.SetAttribute("project",false);
			ops_worklist_node.SetAttribute("err_msg",""); 

			//check if we are doing a job at the moment
			foreach (Node ops_job in ops_worklist_node.getChildren())
			{
				string status = ops_job.GetAttribute("status");
				if (status=="inprogress")
				{
					jobs_in_progress.Add(ops_job);
				}
			}
			//Now clear out the job in hand 
			if (jobs_in_progress.Count>0)
			{
				//there should only one job in progress at any one time 
				Node jn = (Node)jobs_in_progress[0];
				if (jn != null)
				{
					int days_left= jn.GetIntAttribute("days_left",0);
					if (days_left<=1)
					{
						jn.SetAttribute("days_left","0");
						jn.SetAttribute("status","done");
						setBlockFlag = false;
					}
					else
					{
						days_left -=1;
						jn.SetAttribute("days_left",CONVERT.ToStr(days_left));

						string job_action = jn.GetAttribute("action");
						string job_location = jn.GetAttribute("location");
						AddOpsActivityNote(currentday, job_action.ToLower(), job_location, true);
					}
				}
			}
			if (setBlockFlag==false)
			{

				//Build list of 
				foreach (Node ops_job in ops_worklist_node.getChildren())
				{
					string status = ops_job.GetAttribute("status");
					int required_day = ops_job.GetIntAttribute("day",0);

					//only look at jobs which are not yet done.
					if (status=="todo")
					{
						//is this job scheduled (has a defined day) 
						if (required_day != -1)
						{
							//is this job scheduled for today or previous day
							if (required_day <= currentday)
							{
								jobs_todo_scheduled.Add(ops_job);
							}
						}
						else
						{
							//This is unscheduled job
							jobs_todo_unscheduled.Add(ops_job);
						}
					}
				}
				//determine which job to do next
				//Pick a scheduled that is for today, or a unscheduled job
				Node nextJob = null;			
				if (jobs_todo_scheduled.Count>0)
				{
					//we have scheduled job for today 
					nextJob = (Node) jobs_todo_scheduled[0];
				}
				else
				{
					if (jobs_todo_unscheduled.Count>0)
					{
						//we have scheduled job for today 
						nextJob = (Node) jobs_todo_unscheduled[0];
					}
				}
				//now do the work
				if (nextJob!= null)
				{
					//Do the work and charge the money
					doWork(currentday, nextJob, out job_displayname, out installing, out err_msg);
					//need to block this for a day
					setBlockFlag = true;
				}
				//Need to set the block Flags and 
				ops_worklist_node.SetAttribute("block",setBlockFlag);
				ops_worklist_node.SetAttribute("install",installing);
				ops_worklist_node.SetAttribute("jobname",job_displayname);

				if (setBlockFlag)
				{
					string blockdaylist = ops_worklist_node.GetAttribute("blockdaylist");	
					blockdaylist += ","+CONVERT.ToStr(currentday);
					ops_worklist_node.SetAttribute("blockdaylist",blockdaylist);	
				}

				if (err_msg != "")
				{
					ops_worklist_node.SetAttribute("err_msg",err_msg); 
				}
			}
		}

		public void AddOpsActivityNote (int day, string OpsActivity, string location, bool success)
		{
			Node opsNode = MyNodeTree.GetNamedNode("ops_activity");

			//The old controls understand a string based indications system 
			ArrayList attrs = new ArrayList();
			attrs.Clear();
			attrs.Add(new AttributeValuePair("type", "op_activity"));
			attrs.Add(new AttributeValuePair("sub_type", OpsActivity));
			attrs.Add(new AttributeValuePair("day", CONVERT.ToStr(day)));
			attrs.Add(new AttributeValuePair("location", location));
			attrs.Add(new AttributeValuePair("success", CONVERT.ToStr(success)));
			new Node(opsNode, "note", "", attrs);
		}
	}
}