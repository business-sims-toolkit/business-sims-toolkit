using System;
using System.Collections;
using GameManagement;
using Network;
using LibCore;

namespace ReportBuilder
{
	public class CAPM_SummaryReportData : SummaryReportData
	{
		public float[] round_BP_MarketPositions;
		public float[] round_BP_TransactionRevenueGenerated;
		public float[] round_BP_CostReduction;
		public float[] round_BP_TotalBusinessBenefit;
		public float[] round_BP_PMO_Budget;
		public float[] round_BP_FixedCosts;
		public float[] round_BP_OperationalFines;
		public float[] round_BP_ProfitLoss;
		public float[] round_PP_CompletedOnTime;
		public float[] round_PP_Budget;
		public float[] round_PP_Spend;
		public float[] round_PP_BudgetOverUnder;
		public float[] round_PP_IntDaysTasked;
		public float[] round_PP_IntDaysWasted;
		public float[] round_PP_IntCostWasted;
		public float[] round_PP_ExtDaysTasked;
		public float[] round_PP_ExtDaysWasted;
		public float[] round_PP_ExtCostWasted;
		public float[] round_PP_TotalCostWasted;
		public int currentMode;

		public CAPM_SummaryReportData(PMNetworkProgressionGameFile gameFile, DateTime date)
			:base (gameFile, date)
		{
			GameFile = gameFile;
			Date = date;
			ExtractData();
		}

		protected override void ExtractData()
		{
			SpendOverrides = new SupportSpendOverrides(GameFile);

			Background = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + @"\images\PDFTemplate.png");
			FacilitatorLogo = GameFile.GetFacilitatorLogo();
			TeamPhoto = GameFile.GetPdfTeamPhoto();

			Title = GameFile.GetTitle();
			Venue = GameFile.GetVenue();

			int teamCount = 24;
			TeamMembers = new string[teamCount];
			TeamRoles = new string[teamCount];
			GameFile.GetTeamMembersAndRoles(TeamMembers, TeamRoles);

			RoundsPlayed = GameFile.LastRoundPlayed;
			RoundsMax = CoreUtils.SkinningDefs.TheInstance.GetIntData("roundcount", 5);

			Availability = new float[RoundsMax];
			MTRS = new float[RoundsMax];
			TransactionsAchieved = new float[RoundsMax];
			TransactionsMax = new float[RoundsMax];
			RevenueAchieved = new float[RoundsMax];
			RevenueMax = new float[RoundsMax];
			FixedCosts = new float[RoundsMax];
			ProjectSpend = new float[RoundsMax];
			Profit = new float[RoundsMax];
			ProfitGain = new float[RoundsMax];
			SupportBudget = new float[RoundsMax];
			SupportCosts = new float[RoundsMax];
			SupportProfit = new float[RoundsMax];
			IndicatorScore = new float[RoundsMax];

			//PM data 
			round_BP_MarketPositions = new float[RoundsMax];
			round_BP_TransactionRevenueGenerated = new float[RoundsMax];
			round_BP_CostReduction = new float[RoundsMax];
			round_BP_TotalBusinessBenefit = new float[RoundsMax];
			round_BP_PMO_Budget = new float[RoundsMax];
			round_BP_FixedCosts = new float[RoundsMax];
			round_BP_OperationalFines = new float[RoundsMax];
			round_BP_ProfitLoss = new float[RoundsMax];
			round_PP_CompletedOnTime = new float[RoundsMax];
			round_PP_Budget = new float[RoundsMax];
			round_PP_Spend = new float[RoundsMax];
			round_PP_BudgetOverUnder = new float[RoundsMax];
			round_PP_IntDaysTasked = new float[RoundsMax];
			round_PP_IntDaysWasted = new float[RoundsMax];
			round_PP_IntCostWasted = new float[RoundsMax];
			round_PP_ExtDaysTasked = new float[RoundsMax];
			round_PP_ExtDaysWasted = new float[RoundsMax];
			round_PP_ExtCostWasted = new float[RoundsMax];
			round_PP_TotalCostWasted = new float[RoundsMax];
			currentMode = 0;

			InitialiseArrays();

			int previousProfit = 0;
			int newServices = 0;
			for (int round = 0; round < RoundsPlayed; round++)
			{
				RoundScores scores = ExtractRoundScores(round, previousProfit, newServices);

				Availability[round] = (float)scores.Availability;
				MTRS[round] = (float)scores.MTTR;

				TransactionsAchieved[round] = scores.NumTransactions;
				TransactionsMax[round] = scores.MaxTransactions;

				RevenueAchieved[round] = scores.Revenue / 1000000.0f;
				RevenueMax[round] = scores.MaxRevenue / 1000000.0f;

				previousProfit = scores.Profit;
				newServices = scores.NumNewServices;

				FixedCosts[round] = scores.FixedCosts / 1000000.0f;
				ProjectSpend[round] = scores.ProjectSpend / 1000000.0f;

				Profit[round] = scores.Profit / 1000000.0f;
				ProfitGain[round] = scores.Gain / 1000000.0f;
				SupportBudget[round] = scores.SupportBudget / 1000000.0f;
				SupportCosts[round] = scores.SupportCostsTotal / 1000000.0f;
				SupportProfit[round] = scores.SupportProfit / 1000000.0f;

				IndicatorScore[round] = scores.IndicatorScore;
			}

			Hashtable champ = new Hashtable();
			Node tmpNode = null;

			for (int i = 0; i < GameFile.LastRoundPlayed; i++)
			{
				NodeTree nt = this.GameFile.GetNetworkModel(1 + i, GameManagement.GameFile.GamePhase.OPERATIONS);

				//Getting the Business Performance 
				//Market Position 
				tmpNode = nt.GetNamedNode("operational_results");
				round_BP_MarketPositions[i] = tmpNode.GetIntAttribute("market_position", 0);
				//Transaction Rev Generated
				tmpNode = nt.GetNamedNode("fin_results");
				round_BP_TransactionRevenueGenerated[i] = tmpNode.GetIntAttribute("total_revenue", 0);
				//CostReduction Delivered
				tmpNode = nt.GetNamedNode("round_results");
				round_BP_CostReduction[i] = tmpNode.GetIntAttribute("cost_reduction_achieved", 0);
				//Total BusinessBenefit
				tmpNode = nt.GetNamedNode("fin_results");
				round_BP_TotalBusinessBenefit[i] = tmpNode.GetIntAttribute("total_business_benefit", 0);
				//PMO Budget
				tmpNode = nt.GetNamedNode("pmo_budget");
				round_BP_PMO_Budget[i] = tmpNode.GetIntAttribute("budget_allowed", 0);
				//Fixed Costs 
				tmpNode = nt.GetNamedNode("fin_results");
				round_BP_FixedCosts[i] = tmpNode.GetIntAttribute("fixed_costs", 0);
				//Operational Fines
				tmpNode = nt.GetNamedNode("operational_results");
				round_BP_OperationalFines[i] = tmpNode.GetIntAttribute("fines", 0);
				//Profit Loss
				tmpNode = nt.GetNamedNode("fin_results");
				round_BP_ProfitLoss[i] = tmpNode.GetIntAttribute("profitloss", 0);

				//Getting the PMO Performance 
				//Number of Project completed on Time
				tmpNode = nt.GetNamedNode("projects_results");
				round_PP_CompletedOnTime[i] = tmpNode.GetIntAttribute("project_completed", 0);
				//Budget Allowed, Spent and OverUnder
				tmpNode = nt.GetNamedNode("pmo_budget");
				round_PP_Budget[i] = tmpNode.GetIntAttribute("budget_allowed", 0);
				round_PP_Spend[i] = tmpNode.GetIntAttribute("budget_spent", 0);
				round_PP_BudgetOverUnder[i] = tmpNode.GetIntAttribute("budget_left", 0);

				//Resources
				tmpNode = nt.GetNamedNode("resources_results");
				round_PP_IntDaysTasked[i] = tmpNode.GetIntAttribute("int_tasked_days", 0);
				round_PP_IntDaysWasted[i] = tmpNode.GetIntAttribute("int_wasted_days", 0);
				round_PP_IntCostWasted[i] = tmpNode.GetIntAttribute("int_wasted_cost", 0);
				round_PP_ExtDaysTasked[i] = tmpNode.GetIntAttribute("ext_tasked_days", 0);
				round_PP_ExtDaysWasted[i] = tmpNode.GetIntAttribute("ext_wasted_days", 0);
				round_PP_ExtCostWasted[i] = tmpNode.GetIntAttribute("ext_wasted_cost", 0);
				round_PP_TotalCostWasted[i] = tmpNode.GetIntAttribute("total_wasted_cost", 0);
			}





		}
	}
}