using System;
using System.Collections;

using System.Text;
using System.Xml;

using Network;

using LibCore;

using GameManagement;

namespace Polestar_PM.OpsEngine
{
	public enum BenefitType
	{
		None,
		Transactions,
		CostAvoidance,
		CostReduction
	}

	public class PM_OpsEngine_Round3 : PM_OpsEngine
	{
		Node businessPerformance;

		public PM_OpsEngine_Round3 (NetworkProgressionGameFile gameFile, NodeTree model, string roundDir, string incidentDefsFile, int round, bool logResults)
			: base (gameFile, model, roundDir, incidentDefsFile, round, logResults)
		{
			this.roundSecs = CONVERT.ToStr(60 * GetCalendarLength(model));

			businessPerformance = model.GetNamedNode("BusinessPerformance");

			// Force an update on everything.
			UpdateDay(0, false);
		}

		static public int GetCalendarLength (NodeTree model)
		{
			return model.GetNamedNode("Calendar").GetIntAttribute("days", 0);
		}

		static public int GetCurrentDay (NodeTree model)
		{
			int day = model.GetNamedNode("CurrentDay").GetIntAttribute("day", 0);

			// If we're asked for the current day at the very start of a new day, then we have a whole extra day
			// ahead of us!
			int second = model.GetNamedNode("CurrentTime").GetIntAttribute("seconds", 0);
			if ((second % 60) <= 1)
			{
				day--;
			}

			day = Math.Max(0, day);

			return day;
		}

		static public int GetNextStartDay (NodeTree model)
		{
			int day = 1 + Math.Max(1, model.GetNamedNode("CurrentDay").GetIntAttribute("day", 0));

			// If we're asked for the current day at the very start of a new day, then we have a whole extra day
			// ahead of us!
			int second = model.GetNamedNode("CurrentTime").GetIntAttribute("seconds", 0);
			if ((second % 60) <= 1)
			{
				day--;
			}

			return day;
		}

		protected override void ResetCalendar ()
		{
			// Leave the number of days at what the network specifies!
		}

		protected override void CurrDayNode_AttributesChanged (Node sender, System.Collections.ArrayList attrs)
		{
			foreach (AttributeValuePair avp in attrs)
			{
				if (avp.Attribute == "day")
				{
					int day = CONVERT.ParseInt(avp.Value);

					UpdateDay(day, true);
				}
			}
		}

		void UpdateDay (int day, bool progressWork)
		{
			UpdateDay(_Network, day, progressWork);

			Node portfolios = _Network.GetNamedNode("Portfolios");
			ArrayList bizAttrs = new ArrayList ();
			bizAttrs.Add(new AttributeValuePair ("transactions", businessPerformance.GetIntAttribute("base_transactions", 0)
				+ GetPortfoliosBenefit(portfolios, "transactions")));
			bizAttrs.Add(new AttributeValuePair ("cost_avoidance", businessPerformance.GetIntAttribute("base_cost_avoidance", 0)
				+ GetPortfoliosBenefit(portfolios, "cost_avoidance")));
			bizAttrs.Add(new AttributeValuePair ("cost_reduction", businessPerformance.GetIntAttribute("base_cost_reduction", 0)
				+ GetPortfoliosBenefit(portfolios, "cost_reduction")));
			businessPerformance.SetAttributes(bizAttrs);
		}

		static void UpdateDay (NodeTree _Network, int day, bool progressWork)
		{
			// Progress the portfolios...
			Node portfolios = _Network.GetNamedNode("Portfolios");
			int portfoliosSpend = 0;
			int portfoliosResources = 0;
			foreach (Node portfolio in portfolios.GetChildrenOfType("Portfolio"))
			{
				int portfolioSpend = 0;
				int portfolioResources = 0;

				// Progress its programs...
				foreach (Node program in portfolio.GetChildrenOfType("Program"))
				{
//					System.Diagnostics.Debug.WriteLine("======================================================");

					int resources = program.GetIntAttribute("resources", 0);
					int manDaysDone = program.GetIntAttribute("man_days_done", 0);
					int totalManDays = GetTotalManDays(program);
					int startDay = program.GetIntAttribute("start_day", 0);
					int bill = 0;
					string desc = program.GetAttribute("desc");
					string debug = "PRG:" + desc + " resources:" + resources.ToString() + " totalManDays:" + totalManDays.ToString() + " manDaysDone:" + manDaysDone.ToString();
//					System.Diagnostics.Debug.WriteLine(debug);

					// Update the projection before we advance the amount of work done (otherwise we'll predict one day's
					// work too much).
					ArrayList newAttrs = new ArrayList();
					newAttrs.Add(new AttributeValuePair("projected_stage_complete", GetProjectedStageComplete(program)));
					program.SetAttributes(newAttrs);

					if (progressWork)
					{
						newAttrs.Clear();

						int currentSpend = program.GetIntAttribute("spend", 0);
						int budget = program.GetIntAttribute("budget", 0);
						int budgetLeft = budget - currentSpend;
						int costPerManDay = GetCostPerManDay(program);
						int manDaysToDoToday = 0;
//						string debug2 = "    budget" + budget.ToString() + " currentSpend:" + currentSpend.ToString() + " budgetLeft:" + budgetLeft.ToString() + " costPerManDay:" + costPerManDay.ToString();
//						System.Diagnostics.Debug.WriteLine(debug2);

						// Credit us for the previous day's work.
						int manDaysGained = program.GetIntAttribute("man_days_working_today", 0);
						if (manDaysGained > 0)
						{
							manDaysDone += manDaysGained;
							newAttrs.Add(new AttributeValuePair ("man_days_done", manDaysDone));
							manDaysToDoToday = 0;
						}
//						string debug3 = " manDaysDone:" + manDaysDone.ToString();
//						System.Diagnostics.Debug.WriteLine(debug3);

						// Are we about to start work?
						if ((! progressWork) && (manDaysDone == 0) && (resources > 0) && (startDay == 0))
						{
							startDay = day + 1;
							newAttrs.Add(new AttributeValuePair("start_day", day + 1));
						}

						// And bill us for today's work.
						if (day >= startDay)
						{
							manDaysToDoToday = Math.Min(totalManDays - manDaysDone, resources);
							if (manDaysToDoToday > 0)
							{
								//Check that Wages bill can be paid for 
								//if ((manDaysToDoToday * costPerManDay) <= budgetLeft)
								if ((resources * costPerManDay) <= budgetLeft)
								{
									if ((manDaysDone == 0) && (resources > 0) && (startDay == 0))
									{
										startDay = day;
										newAttrs.Add(new AttributeValuePair("start_day", startDay));
									}

									newAttrs.Add(new AttributeValuePair("over_budget", "false"));

									bill = resources * costPerManDay;

									currentSpend += bill;
									newAttrs.Add(new AttributeValuePair("spend", currentSpend));
								}
								else
								{
									//Unable to pay the wages bill 
									manDaysToDoToday = 0;
									newAttrs.Add(new AttributeValuePair("over_budget", "true"));
								}
							}
							else
							{
								if ((budgetLeft < 0))
								{
									newAttrs.Add(new AttributeValuePair("over_budget", "true"));
								}
								else
								{
									newAttrs.Add(new AttributeValuePair("over_budget", "false"));
								}
							}
						}

						newAttrs.Add(new AttributeValuePair ("man_days_working_today", manDaysToDoToday));
						newAttrs.Add(new AttributeValuePair ("stage_completed", GetStageCompleteByManDays(program, manDaysDone)));

						string stageWorking = GetStageWorkingByManDays(program, manDaysDone, true);

						// Don't start work until the first day has started, and we have resources.
						if ((day < 1) || ((manDaysDone == 0) && (resources == 0)) || (day < startDay))
						{
							stageWorking = "";
						}
						newAttrs.Add(new AttributeValuePair("stage_working", stageWorking));
						newAttrs.Add(new AttributeValuePair("calendar_days_left_in_stage", GetCalendarDaysLeftInCurrentStageByManDays(program, manDaysDone)));

						program.SetAttributes(newAttrs);
					}

					newAttrs.Clear();
					newAttrs.Add(new AttributeValuePair("transaction_benefit", GetProgramBenefit(program, "transactions")));
					newAttrs.Add(new AttributeValuePair("cost_avoidance_benefit", GetProgramBenefit(program, "cost_avoidance")));
					newAttrs.Add(new AttributeValuePair("cost_reduction_benefit", GetProgramBenefit(program, "cost_reduction")));
					program.SetAttributes(newAttrs);

					portfolioSpend += program.GetIntAttribute("spend", 0);
					portfolioResources += resources;
				}

				ArrayList portfolioAttrs = new ArrayList ();
				portfolioAttrs.Add(new AttributeValuePair ("spend", portfolioSpend));
				portfolioAttrs.Add(new AttributeValuePair ("transaction_benefit", GetPortfolioBenefit(portfolio, "transactions")));
				portfolioAttrs.Add(new AttributeValuePair ("cost_avoidance_benefit", GetPortfolioBenefit(portfolio, "cost_avoidance")));
				portfolioAttrs.Add(new AttributeValuePair ("cost_reduction_benefit", GetPortfolioBenefit(portfolio, "cost_reduction")));
				portfolioAttrs.Add(new AttributeValuePair ("resources", portfolioResources));
				portfolio.SetAttributes(portfolioAttrs);

				portfoliosSpend += portfolioSpend;
				portfoliosResources += portfolioResources;
			}
			// Also update the benefits from any shelved programs (but don't progress them).
			foreach (Node program in portfolios.GetChildrenOfType("Program"))
			{
				ArrayList newAttrs = new ArrayList ();
				newAttrs.Add(new AttributeValuePair ("transaction_benefit", GetProgramBenefit(program, "transactions")));
				newAttrs.Add(new AttributeValuePair ("cost_avoidance_benefit", GetProgramBenefit(program, "cost_avoidance")));
				newAttrs.Add(new AttributeValuePair ("cost_reduction_benefit", GetProgramBenefit(program, "cost_reduction")));
							
				program.SetAttributes(newAttrs);						
			}

			ArrayList portfoliosAttrs = new ArrayList ();
			portfoliosAttrs.Add(new AttributeValuePair ("spend", portfoliosSpend));
			portfolios.SetAttributes(portfoliosAttrs);
		}

		public static int GetCostPerManDay (Node program)
		{
			string cost = program.GetAttribute("cost_per_man_day");
			if (cost == "")
			{
				cost = program.Parent.GetAttribute("cost_per_man_day");
				if (cost == "")
				{
					cost = program.Parent.Parent.GetAttribute("cost_per_man_day");
				}
			}

			return CONVERT.ParseIntSafe(cost, 0);
		}

		public static int GetTotalManDays (Node program)
		{
			int manDays = 0;

			foreach (Node stage in program.GetChildrenOfType("Stage"))
			{
				manDays += stage.GetIntAttribute("man_days", 0);
			}

			return manDays;
		}

		public static int GetResourcesNeededToHitTargetSpend (Node program, int spend)
		{
			int currentSpend = program.GetIntAttribute("spend", 0);
			int remainingSpend = spend - currentSpend;
			int daysLeft = GetCalendarLength(program.Tree) - GetCurrentDay(program.Tree);

			if (daysLeft <= 0)
			{
				return int.MinValue;
			}

			return (int) Math.Floor(remainingSpend * 1.0 / (daysLeft * GetCostPerManDay(program)));
		}

		public static int GetResourcesNeededToHitTargetDay (Node program, int targetDay, int currentDay)
		{
			int manDaysDone = program.GetIntAttribute("man_days_done", 0);
			int totalManDays = GetTotalManDays(program);
			int daysLeft = targetDay - currentDay;

			if (daysLeft <= 0)
			{
				return int.MinValue;
			}

			return (int) Math.Ceiling((totalManDays - manDaysDone) * 1.0 / daysLeft);
		}

		public static int GetResourcesNeededToHitTargetStageComplete (Node program, string stage, int currentDay)
		{
			return GetResourcesNeededToHitTargetStageComplete(program, stage, currentDay, GetCalendarLength(program.Tree));
		}

		public static int GetResourcesNeededToHitTargetStageComplete (Node program, string stage, int currentDay, int endDay)
		{
			int manDaysDone = program.GetIntAttribute("man_days_done", 0);
			int daysLeft = endDay - currentDay;
			int targetManDays = GetManDaysGivenStageComplete(program, stage);

			return (int) Math.Ceiling((targetManDays - manDaysDone) / (double) daysLeft);
		}

		public static string GetProjectedStageComplete (Node program)
		{
			return GetProjectedStageComplete(program, GetCalendarLength(program.Tree));
		}

		public static string GetProjectedStageComplete (Node program, int endDay)
		{
			return GetProjectedStageComplete(program, GetCurrentDay(program.Tree), endDay);
		}

		public static string GetProjectedStageComplete (Node program, int currentDay, int endDay)
		{
			int resources = program.GetIntAttribute("resources", 0);

			return GetProjectedStageCompleteGivenResources(program, currentDay, endDay, resources);
		}

		public static string GetProjectedStageCompleteGivenResources (Node program, int currentDay, int resources)
		{
			return GetProjectedStageCompleteGivenResources(program, currentDay, GetCalendarLength(program.Tree), resources);
		}

		public static string GetProjectedStageCompleteGivenResources (Node program, int resources)
		{
			return GetProjectedStageCompleteGivenResources(program, GetCurrentDay(program.Tree), GetCalendarLength(program.Tree), resources);
		}

		public static string GetProjectedStageCompleteGivenResources (Node program, int currentDay, int endDay, int resources)
		{
			int manDaysDone = program.GetIntAttribute("man_days_done", 0);
			int totalManDays = GetTotalManDays(program);
			int daysLeft = Math.Max(0, 1 + endDay - currentDay);

			int projectedManDays = Math.Min(totalManDays, manDaysDone + (daysLeft * resources));

			return GetStageCompleteByManDays(program, projectedManDays);
		}

		public static string GetProjectedStageWorking (Node program)
		{
			return GetProjectedStageWorking(program, GetCalendarLength(program.Tree));
		}

		public static string GetProjectedStageWorking (Node program, int endDay)
		{
			return GetProjectedStageWorking(program, GetCurrentDay(program.Tree), endDay);
		}

		public static string GetProjectedStageWorking (Node program, int currentDay, int endDay)
		{
			int resources = program.GetIntAttribute("resources", 0);

			return GetProjectedStageWorkingGivenResources(program, currentDay, endDay, resources);
		}

		public static string GetProjectedStageWorkingGivenResources (Node program, int currentDay, int resources)
		{
			return GetProjectedStageWorkingGivenResources(program, currentDay, GetCalendarLength(program.Tree), resources);
		}

		public static string GetProjectedStageWorkingGivenResources (Node program, int resources)
		{
			return GetProjectedStageWorkingGivenResources(program, GetCurrentDay(program.Tree), GetCalendarLength(program.Tree), resources);
		}

		public static string GetProjectedStageWorkingGivenResources (Node program, int currentDay, int endDay, int resources)
		{
			int manDaysDone = program.GetIntAttribute("man_days_done", 0);
			int totalManDays = GetTotalManDays(program);
			int daysLeft = Math.Max(0, 1 + endDay - currentDay);

			// Pick a point halfway through the given working day.
			int projectedManDays = Math.Min(totalManDays, manDaysDone + (daysLeft * resources) - (resources / 2));

			return GetStageWorkingByManDays(program, projectedManDays, false);
		}

		static Node GetStageNodeGivenManDays (Node program, int manDays)
		{
			Node lastStageDone = null;

			int accumulatedManDays = 0;
			foreach (Node stageNode in program.GetChildrenOfType("Stage"))
			{
				accumulatedManDays += stageNode.GetIntAttribute("man_days", 0);

				if (accumulatedManDays <= manDays)
				{
					lastStageDone = stageNode;
				}
			}

			return lastStageDone;
		}

		static Node GetStageWorkingNodeGivenManDays (Node program, int manDays, bool onEdgeGiveLaterStage)
		{
			Node lastStageWorking = null;

			int accumulatedManDays = 0;
			ArrayList stages = program.GetChildrenOfType("Stage");
			for (int i = 0; i < stages.Count; i++)
			{
				Node stageNode = stages[i] as Node;

				if (onEdgeGiveLaterStage)
				{
					if (accumulatedManDays <= manDays)
					{
						lastStageWorking = stageNode;
					}
				}
				else
				{
					if ((accumulatedManDays < manDays) || (i == 0))
					{
						lastStageWorking = stageNode;
					}
				}

				accumulatedManDays += stageNode.GetIntAttribute("man_days", 0);
			}

			if (lastStageWorking == stages[stages.Count - 1])
			{
				if (manDays >= accumulatedManDays)
				{
					lastStageWorking = null;
				}
			}

			return lastStageWorking;
		}

		static int GetCalendarDaysLeftInCurrentStageByManDays (Node program, int manDays)
		{
			int completedStagesTotalManDays = 0;
			int nextStageTotalManDays = 0;
			foreach (Node stageNode in program.GetChildrenOfType("Stage"))
			{
				int stageLength = stageNode.GetIntAttribute("man_days", 0);

				nextStageTotalManDays += stageLength;
				if (nextStageTotalManDays <= manDays)
				{
					completedStagesTotalManDays = nextStageTotalManDays;
				}
				else
				{
					break;
				}
			}

			int resources = program.GetIntAttribute("resources", 0);
			if (resources > 0)
			{
				return (int) Math.Ceiling((nextStageTotalManDays - manDays) / (double) resources);
			}

			return 0;
		}

		static string GetStageCompleteByManDays (Node program, int manDays)
		{
			Node stage = GetStageNodeGivenManDays(program, manDays);

			if (stage != null)
			{
				return stage.GetAttribute("stage");
			}

			return "";
		}

		static string GetStageWorkingByManDays (Node program, int manDays, bool onEdgeGiveLaterStage)
		{
			Node stage = GetStageWorkingNodeGivenManDays(program, manDays, onEdgeGiveLaterStage);

			if (stage != null)
			{
				return stage.GetAttribute("stage");
			}

			return "";
		}

		static int GetManDaysGivenStageComplete (Node program, string stage)
		{
			int manDays = 0;

			if (stage == "")
			{
				return 0;
			}

			foreach (Node stageNode in program.GetChildrenOfType("Stage"))
			{
				manDays += stageNode.GetIntAttribute("man_days", 0);
				if (stageNode.GetAttribute("stage") == stage)
				{
					break;
				}
			}

			return manDays;
		}

		public static int GetProjectedFinishDay (Node program)
		{
			return GetProjectedFinishDay(program, GetCurrentDay(program.Tree));
		}

		public static int GetProjectedFinishDay (Node program, int currentDay)
		{
			int resources = program.GetIntAttribute("resources", 0);

			return GetProjectedFinishDayGivenResource(program, currentDay, resources);
		}

		public static int GetProjectedFinishDayGivenResource (Node program, int resources)
		{
			return GetProjectedFinishDayGivenResource(program, GetCurrentDay(program.Tree), resources);
		}

		public static int GetProjectedFinishDayGivenResource (Node program, int currentDay, int resources)
		{
			int manDaysDone = program.GetIntAttribute("man_days_done", 0);
			int totalManDays = GetTotalManDays(program);

			int manDaysLeft = totalManDays - manDaysDone;

			if ((manDaysLeft <= 0) || (resources > 0))
			{
				return Math.Max(1, currentDay) + (int) Math.Ceiling(manDaysLeft * 1.0 / resources);
			}
			else
			{
				return int.MaxValue;
			}
		}

		public void FreezeOnThisMinute ()
		{
			int seconds = this._Network.GetNamedNode("CurrentTime").GetIntAttribute("seconds", 0);
			FreezeAt((60 * (1 + (seconds / 60))) - 1);
		}

		public void FreezeAt (int freezeAt)
		{
			secondsRunner.FreezeAt(freezeAt);
		}

		public static int GetProgramBenefit (Node program, string benefitType)
		{
			return GetProgramBenefitGivenStage(program, program.GetAttribute("stage_completed"), benefitType);
		}

		public static int GetProgramBenefitGivenStage (Node program, string stageComplete, string benefitType)
		{
			foreach (Node stageNode in program.GetChildrenOfType("Stage"))
			{
				if (stageNode.GetAttribute("stage") == stageComplete)
				{
					return stageNode.GetIntAttribute(benefitType, 0);
				}
			}

			return 0;
		}

		public static int GetPortfolioBenefit (Node portfolio, string benefitType)
		{
			int benefit = 0;

			foreach (Node program in portfolio.GetChildrenOfType("Program"))
			{
				benefit += GetProgramBenefit(program, benefitType);
			}

			return benefit;
		}

		public static int GetPortfoliosBenefit (Node portfolios, string benefitType)
		{
			int benefit = 0;

			foreach (Node portfolio in portfolios.GetChildrenOfType("Portfolio"))
			{
				benefit += GetPortfolioBenefit(portfolio, benefitType);
			}
			foreach (Node program in portfolios.GetChildrenOfType("Program"))
			{
				benefit += GetProgramBenefit(program, benefitType);
			}

			return benefit;
		}

		public static int GetProgramSpend (Node program)
		{
			return program.GetIntAttribute("spend", 0);
		}

		public static int GetPortfolioSpend (Node portfolio)
		{
			int spend = 0;

			foreach (Node program in portfolio.GetChildrenOfType("Program"))
			{
				spend += GetProgramSpend(program);
			}

			return spend;
		}

		public static int GetPortfoliosSpend (Node portfolios)
		{
			int spend = 0;

			foreach (Node portfolio in portfolios.GetChildrenOfType("Portfolio"))
			{
				spend += GetPortfolioSpend(portfolio);
			}

			return spend;
		}

		public static int GetProjectedProgramSpend (Node program)
		{
			int calendarEnd = GetCalendarLength(program.Tree);

			return GetProjectedProgramSpendByDay(program, calendarEnd);
		}

		public static int GetProjectedProgramSpendByDay (Node program, int day)
		{
			int currentDay = GetCurrentDay(program.Tree);
			int programFinish = GetProjectedFinishDay(program);

			//int daysLeft = Math.Max(0, Math.Min(day, programFinish) - currentDay - 1);
			int daysLeft = Math.Max(0, Math.Min(day, programFinish) - currentDay);

			return program.GetIntAttribute("spend", 0) + (daysLeft * program.GetIntAttribute("resources", 0) * GetCostPerManDay(program));
		}

		public static int GetProjectedProgramSpendGivenResource (Node program, int resources)
		{
			int currentDay = GetCurrentDay(program.Tree);
			int calendarEnd = GetCalendarLength(program.Tree);
			int programFinish = GetProjectedFinishDayGivenResource(program, currentDay, resources);
			int daysLeft = Math.Min(calendarEnd, programFinish) - currentDay;

			return program.GetIntAttribute("spend", 0) + (daysLeft * resources * GetCostPerManDay(program));
		}

		public enum BudgetChangeResult
		{
			Success,
			AlreadySpentMore,
			BudgetNotAvailable
		}

		public static BudgetChangeResult SetBudget (Node programNode, int budget)
		{
			int oldBudget = programNode.GetIntAttribute("budget", 0);
			int increase = budget - oldBudget;
			int oldProgramSpend = programNode.GetIntAttribute("spend", 0);

			Node pmoBudgetNode = programNode.Tree.GetNamedNode("pmo_budget");
			int oldSpend = pmoBudgetNode.GetIntAttribute("budget_spent", 0);
			int oldLeft = pmoBudgetNode.GetIntAttribute("budget_left", 0);

			//Fail Situations when we are asking for more 
			//(Increase>0) and (Increase>OldLeft)  
			//we need more money and there is not enough left
			if (increase > 0)
			{
				if (increase > oldLeft)
				{
					//More needed than we have left
					return BudgetChangeResult.BudgetNotAvailable;
				}
				if (oldLeft <= 0)
				{
					//Any request when we are empty or in deficit
					return BudgetChangeResult.BudgetNotAvailable;
				}
			}

			//if (increase < 0)
			//Then we are giving monoey back and thats allways good

			//OLD CODE
			//if (increase > oldLeft)
			//{
			//  return BudgetChangeResult.BudgetNotAvailable;
			//}

			if (budget < oldProgramSpend)
			{
				return BudgetChangeResult.AlreadySpentMore;
			}

			programNode.SetAttribute("budget", budget);

			ArrayList attributes = new ArrayList ();
			attributes.Add(new AttributeValuePair("budget_spent", oldSpend + increase));
			attributes.Add(new AttributeValuePair("budget_left", oldLeft - increase));
			pmoBudgetNode.SetAttributes(attributes);

			return BudgetChangeResult.Success;
		}

		public static void SetResources (Node portfoliosNode, Node portfolioNode, Node programNode, int resources)
		{
			int oldResources = programNode.GetIntAttribute("resources", 0);

			resources = Math.Max(resources, 0);

			int resourcesToFind = resources - oldResources;
			int resourcesToSet;

			if (resourcesToFind > 0)
			{
				resourcesToSet = oldResources;

				// Get them from the program's own portfolio first.
				int freeResources = portfolioNode.GetIntAttribute("free_resources", 0);
				int resourcesTaken = Math.Min(freeResources, resourcesToFind);

				portfolioNode.SetAttribute("free_resources", freeResources - resourcesTaken);
				resourcesToFind -= resourcesTaken;
				resourcesToSet += resourcesTaken;

				// Then from other portfolios.
				foreach (Node otherPortfolio in portfoliosNode.GetChildrenOfType("Portfolio"))
				{
					freeResources = otherPortfolio.GetIntAttribute("free_resources", 0);
					resourcesTaken = Math.Min(freeResources, resourcesToFind);

					otherPortfolio.SetAttribute("free_resources", freeResources - resourcesTaken);
					resourcesToFind -= resourcesTaken;
					resourcesToSet += resourcesTaken;
				}
			}
			else
			{
				// Return the unused resources to the pool.
				resourcesToSet = resources;
				portfolioNode.SetAttribute("free_resources", portfolioNode.GetIntAttribute("free_resources", 0) - resourcesToFind);
			}

			programNode.SetAttribute("resources", resourcesToSet);

			int portfolioResources = 0;
			foreach (Node otherProgram in portfolioNode.GetChildrenOfType("Program"))
			{
				portfolioResources += otherProgram.GetIntAttribute("resources", 0);
			}
			portfolioNode.SetAttribute("resources", portfolioResources);

			UpdateDay(portfoliosNode.Tree, GetCurrentDay(portfoliosNode.Tree), false);

			UpdateProjectPlan(programNode);
		}

		static void UpdateProjectPlan (Node programNode)
		{
			Node projectPlans = programNode.Tree.GetNamedNode("project_plans");
			string name = programNode.GetAttribute("name");

			Node projectPlan = null;
			foreach (Node node in projectPlans.GetChildrenOfType("project_plan"))
			{
				if (node.GetAttribute("project") == name)
				{
					projectPlan = node;
					break;
				}
			}

			if (projectPlan == null)
			{
				ArrayList attributes = new ArrayList ();
				attributes.Add(new AttributeValuePair ("project", name));
				attributes.Add(new AttributeValuePair ("type", "project_plan"));
				projectPlan = new Node(projectPlans, "project_plan", "", attributes);
			}

			foreach (Node child in projectPlan.getChildrenClone())
			{
				projectPlan.DeleteChildTree(child);
			}

			int startDay = programNode.GetIntAttribute("start_day", -1);
			for (int day = 1; day <= GetCalendarLength(programNode.Tree); day++)
			{
				string stageWorkingLetter = GetProjectedStageWorking(programNode, day - 1);
				if (stageWorkingLetter == "")
				{
					stageWorkingLetter = "UNKNOWN";
				}
				DataLookup.emProjectOperationalState stageWorking = (DataLookup.emProjectOperationalState) Enum.Parse(typeof (DataLookup.emProjectOperationalState), "PROJECT_STATE_" + stageWorkingLetter);

				ArrayList attributes = new ArrayList ();
				attributes.Add(new AttributeValuePair ("day", day));
				attributes.Add(new AttributeValuePair ("type", "day_plan"));
				attributes.Add(new AttributeValuePair ("stage", stageWorking.ToString()));

				new Node (projectPlan, "day_plan", "", attributes);
			}
		}

		public static void AddProgram (Node portfoliosNode, Node portfolioNode, Node programNode)
		{
			SetResources(portfoliosNode, portfolioNode, programNode, 0);

			portfolioNode.AddChild(programNode);
			portfolioNode.Tree.FireMovedNode(portfoliosNode, programNode);

			programNode.SetAttribute("start_day", GetNextStartDay(programNode.Tree));

			SetResources(portfoliosNode, portfolioNode, programNode, programNode.GetIntAttribute("default_resources", 0));
			SetBudget(programNode, programNode.GetIntAttribute("predicted_cost", 0));
		}

		public static void ShelveProgram (Node portfoliosNode, Node portfolioNode, Node programNode)
		{
			SetResources(portfoliosNode, portfolioNode, programNode, 0);
			SetBudget(programNode, programNode.GetIntAttribute("spend", 0));

			portfoliosNode.AddChild(programNode);
			portfoliosNode.Tree.FireMovedNode(portfolioNode, programNode);
		}

		public static System.Drawing.Color GetBenefitColour (BenefitType benefit)
		{
			switch (benefit)
			{
				case BenefitType.Transactions:
					return CoreUtils.SkinningDefs.TheInstance.GetColorData("round3_transaction_colour");

				case BenefitType.CostAvoidance:
					return CoreUtils.SkinningDefs.TheInstance.GetColorData("round3_cost_avoidance_colour");

				case BenefitType.CostReduction:
					return CoreUtils.SkinningDefs.TheInstance.GetColorData("round3_cost_reduction_colour");

				case BenefitType.None:
				default:
					return System.Drawing.Color.Black;
			}
		}

		public static BenefitType GetProgramMainBenefitType (Node program)
		{
			if (program.GetChildrenOfType("TransactionBenefits").Count > 0)
			{
				return BenefitType.Transactions;
			}
			else if (program.GetChildrenOfType("CostAvoidanceBenefits").Count > 0)
			{
				return BenefitType.CostAvoidance;
			}
			else if (program.GetChildrenOfType("CostReductionBenefits").Count > 0)
			{
				return BenefitType.CostReduction;
			}
			else
			{
				return BenefitType.None;
			}
		}

		public static System.Drawing.Color GetProgramColor (Node program)
		{
			return GetBenefitColour(GetProgramMainBenefitType(program));
		}

		public void OutputTimeSheet ()
		{
			LibCore.BasicXmlDocument doc = LibCore.BasicXmlDocument.Create();
			XmlElement root = doc.CreateElement("time_sheets");
			doc.AppendChild(root);

			Node portfolios = _Network.GetNamedNode("Portfolios");
			ArrayList programs = new ArrayList ();
			programs.AddRange(portfolios.GetChildrenOfType("Program"));
			foreach (Node portfolio in portfolios.GetChildrenOfType("Portfolio"))
			{
				programs.AddRange(portfolio.GetChildrenOfType("Program"));
			}

			foreach (Node program in programs)
			{
				XmlElement timeSheet = doc.CreateElement("time_sheet");
				timeSheet.SetAttribute("program", program.GetAttribute("name"));
				timeSheet.SetAttribute("desc", program.GetAttribute("desc"));
				timeSheet.SetAttribute("shortdesc", program.GetAttribute("shortdesc"));
				root.AppendChild(timeSheet);

				for (int day = 1; day <= GetCalendarLength(_Network); day++)
				{
					int resources = 0;
					int freeResources = 0;

					XmlElement dayNode = doc.CreateElement("day");
					dayNode.SetAttribute("value", LibCore.CONVERT.ToStr(day));
					timeSheet.AppendChild(dayNode);

					XmlElement working = doc.CreateElement("workers");
					working.SetAttribute("type", "staff");
					working.SetAttribute("stage", "dev");
					working.SetAttribute("working", "true");
					working.SetAttribute("num", LibCore.CONVERT.ToStr(resources));
					dayNode.AppendChild(working);

					XmlElement free = doc.CreateElement("workers");
					free.SetAttribute("type", "staff");
					free.SetAttribute("stage", "dev");
					free.SetAttribute("working", "false");
					free.SetAttribute("num", LibCore.CONVERT.ToStr(freeResources));
					dayNode.AppendChild(free);
				}
			}

			if (_gameFile.IsNormalGame)
			{
				string filename = _gameFile.GetRoundFile(round, "end_timesheet.xml", GameManagement.GameFile.GamePhase.OPERATIONS);
				doc.Save(filename);
			}
		}

		protected override void CurrTimeNode_AttributesChanged (Node sender, ArrayList attrs)
		{
			foreach (AttributeValuePair avp in attrs)
			{
				if ("seconds" == avp.Attribute)
				{
					int seconds = CONVERT.ParseIntSafe(avp.Value, 0);

					if (seconds % 60 == 1)
					{
						RecordProjectedBenefits();
					}


					if (seconds == 1)
					{
						CurrDayNode.SetAttribute("day", "1");
					}
					else if (avp.Value == roundSecs)
					{
						Node ep = _Network.GetNamedNode("endpoint");
						int epd = ep.GetIntAttribute("hits", -1);
						epd++;
						ep.SetAttribute("hits", CONVERT.ToStr(epd));

						base.RaiseEvent(this);

						HandleEndOfRound_Calculations();
					}
				}
			}
		}

		string FormatThousands (int a)
		{
			if (a < 0)
			{
				return "-" + FormatThousands(Math.Abs(a));
			}

			string raw = LibCore.CONVERT.ToStr(a);

			StringBuilder builder = new StringBuilder ("");
			int digits = 0;
			for (int character = raw.Length - 1; character >= 0; character--)
			{
				builder.Insert(0, raw[character]);
				digits++;

				if (((digits % 3) == 0) && (character > 0))
				{
					builder.Insert(0, ",");
				}				
			}

			return builder.ToString();
		}

		string FormatMoney (int a)
		{
			return "$" + FormatThousands(a);
		}

		new public void CalculateProjectedBenefits ()
		{
			ArrayList programs = new ArrayList ();

			// Get the list of all programs.
			Node portfolios = _Network.GetNamedNode("Portfolios");
			foreach (Node portfolio in portfolios.GetChildrenOfType("Portfolio"))
			{
				foreach (Node program in portfolio.GetChildrenOfType("Program"))
				{
					programs.Add(program);
				}
			}
			foreach (Node program in portfolios.GetChildrenOfType("Program"))
			{
				programs.Add(program);
			}

			// Calculate the benefits.
			int transactionBenefit = 0;
			int costReductionBenefit = 0;
			//int costAvoidanceBenefit = 0;
			foreach (Node program in programs)
			{
				string stage = GetProjectedStageComplete(program);

				transactionBenefit += GetProgramBenefitGivenStage(program, stage, "transactions");
				costReductionBenefit += GetProgramBenefitGivenStage(program, stage, "cost_reduction");
				//costAvoidanceBenefit += GetProgramBenefitGivenStage(program, stage, "cost_avoidance");
			}

			Node messageNode = _Network.GetNamedNode("day_activity_messages");
			string message = "";

			message += "Projected Transactions Gain: " + FormatThousands(transactionBenefit) + "\r\n";
			message += "Projected Cost Reduction: " + FormatMoney(costReductionBenefit) + "\r\n";
			//message += "Projected Cost Avoidance: " + FormatMoney(costAvoidanceBenefit) + "\r\n";
			messageNode.SetAttribute("message", message);
		}


		public void RecordProjectedBenefits()
		{
			ArrayList programs = new ArrayList();

			// Get the list of all programs.
			Node portfolios = _Network.GetNamedNode("Portfolios");
			foreach (Node portfolio in portfolios.GetChildrenOfType("Portfolio"))
			{
				foreach (Node program in portfolio.GetChildrenOfType("Program"))
				{
					programs.Add(program);
				}
			}
			foreach (Node program in portfolios.GetChildrenOfType("Program"))
			{
				programs.Add(program);
			}

			// Calculate the benefits.
			int transactionBenefit = 0;
			int costReductionBenefit = 0;
			//int costAvoidanceBenefit = 0;
			foreach (Node program in programs)
			{
				string stage = GetProjectedStageComplete(program);

				if (stage != "")
				{
					transactionBenefit += GetProgramBenefitGivenStage(program, stage, "transactions");
					costReductionBenefit += GetProgramBenefitGivenStage(program, stage, "cost_reduction");
					//costAvoidanceBenefit += GetProgramBenefitGivenStage(program, stage, "cost_avoidance");
				}
			}

			string debug = "RecordProjectedBenefits TR:"+CONVERT.ToStr(transactionBenefit) + "  CR:"+CONVERT.ToStr(costReductionBenefit);
			System.Diagnostics.Debug.WriteLine(debug);

			Node Market_Info_Node = _Network.GetNamedNode("market_info");
			int BusinessTargetTransactionsValue = Market_Info_Node.GetIntAttribute("transactions_gain", 0);
			int BusinessTargetCostReductionValue = Market_Info_Node.GetIntAttribute("cost_reduction", 0);

			Node BusinesProjectedPerformanceNode = _Network.GetNamedNode("BusinessProjectedPerformance");
			BusinesProjectedPerformanceNode.SetAttribute("transactionBenefitPlanned", CONVERT.ToStr(transactionBenefit));
			BusinesProjectedPerformanceNode.SetAttribute("costReductionBenefitPlanned", CONVERT.ToStr(costReductionBenefit));

			BusinesProjectedPerformanceNode.SetAttribute("transactionBenefitTarget", CONVERT.ToStr(BusinessTargetTransactionsValue));
			BusinesProjectedPerformanceNode.SetAttribute("costReductionBenefitTarget", CONVERT.ToStr(BusinessTargetCostReductionValue));
		}

	}
}