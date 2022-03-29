using System;
using GameManagement;
using LibCore;
using CoreUtils;
using ReportBuilder;

namespace Cloud.ReportsScreen
{
	public class Cloud_SummaryReportData : SummaryReportData
	{
		public float[] Overall_Benefits;
		public float[] Overall_NewServiceSpends;
		public float[] Overall_ProfitLossValues;
		public float[] Overall_ProfitTargetValues;
		public float[] Overall_TimeToValues;

		public float[] Opex_OpexCosts;
		public float[] Opex_CostPerCPU;
		public float[] Opex_UtilValues;

		public Cloud_SummaryReportData (NetworkProgressionGameFile gameFile, DateTime date)
			: base(gameFile, date)
		{

		}

		protected override void InitialiseArrays ()
		{
			Overall_Benefits = new float[5];
			Overall_NewServiceSpends = new float[5];
			Overall_ProfitLossValues = new float[5];
			Overall_ProfitTargetValues = new float[5];
			Overall_TimeToValues = new float[5];

			Opex_OpexCosts = new float[5];
			Opex_CostPerCPU = new float[5];
			Opex_UtilValues = new float[5];

			for (int step = 0; step < 5; step++)
			{
				Overall_Benefits[step] = 0.0f;
				Overall_NewServiceSpends[step] = 0.0f;
				Overall_ProfitLossValues[step] = 0.0f;
				Overall_ProfitTargetValues[step] = 0.0f;
				Overall_TimeToValues[step] = 0.0f;

				Opex_OpexCosts[step] = 0.0f;
				Opex_CostPerCPU[step] = 0.0f;
				Opex_UtilValues[step] = 0.0f;
			}
		}

		protected override void ExtractData ()
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
			RoundsMax = SkinningDefs.TheInstance.GetIntData("roundcount", 5);

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

			InitialiseArrays();

			int previousProfit = 0;
			int newServices = 0;
			int prevprofit = 0;
			for (int round = 0; round < RoundsPlayed; round++)
			{
				Cloud_RoundScores score = (Cloud_RoundScores) ExtractRoundScores(round, previousProfit, newServices);

				if (score.extract_failure == false)
				{

					float Total_Opex = 0;
					float Total_CpuPeriods = 0;
					float Total_Utilisation = 0;
					foreach (string regionName in score.NameToRegion.Keys)
					{
						Cloud_RoundScores.RegionPerformance region = score.NameToRegion[regionName];
						Total_Opex += (float) region.OpEx;
						Total_CpuPeriods += (float) region.TotalBusinessDemandCpuPeriods;
						Total_Utilisation += (float) region.CpuUtilisation;
					}
					Opex_OpexCosts.SetValue(Total_Opex, round);
					Opex_CostPerCPU.SetValue(Total_Opex / Math.Max(1, Total_CpuPeriods), round);
					Opex_UtilValues.SetValue(100 * Total_Utilisation / Math.Max(1, score.NameToRegion.Count), round);

					float tmp2 = (float) score.TotalRealisedOpportunity;
					Overall_Benefits.SetValue(tmp2, round);

					tmp2 = (float) score.TotalNewServiceCost;
					Overall_NewServiceSpends.SetValue(tmp2, round);

					tmp2 = (float) score.NetValue;
					Overall_ProfitLossValues.SetValue(tmp2, round);

					tmp2 = (float) score.TargetRevenue;
					Overall_ProfitTargetValues.SetValue(tmp2, round);

					tmp2 = (float) score.TimeToValue;
					Overall_TimeToValues.SetValue(tmp2, round);

					prevprofit = score.Profit;

					float tmp = (float) score.NumTransactions;
					TransactionsAchieved.SetValue(tmp, round);

					tmp = (float) score.MaxTransactions;
					TransactionsMax.SetValue(tmp, round);

					tmp = (float) score.MaxRevenue / 1000000;
					RevenueMax.SetValue(tmp, round);

					tmp = (float) score.Revenue / 1000000;
					RevenueAchieved.SetValue(tmp, round);

					tmp = (float) score.FixedCosts / 1000000;
					FixedCosts.SetValue(tmp, round);

					tmp = (float) score.SupportBudget / 1000000;
					SupportBudget.SetValue(tmp, round);

					tmp = (float) score.ProjectSpend / 1000000;
					ProjectSpend.SetValue(tmp, round);

					float profit = (float) score.Profit / 1000000;
					Profit.SetValue(profit, round);

					tmp = (float) score.Gain / 1000000;
					ProfitGain.SetValue(tmp, round);

					tmp = (float) score.SupportCostsTotal / 1000000;
					SupportCosts.SetValue(tmp, round);

					tmp = (float) score.SupportProfit / 1000000;
					SupportProfit.SetValue(tmp, round);

					tmp = (float) score.Availability;
					Availability.SetValue(tmp, round);

					MTRS.SetValue((float) score.MTTR, round);

					tmp = (float) score.IndicatorScore;
					IndicatorScore.SetValue(tmp, round);

				}
			}
		}

		protected override RoundScores ExtractRoundScores (int round, int previousProfit, int newServices)
		{
			return new Cloud_RoundScores (GameFile, round + 1, previousProfit, newServices, SpendOverrides);
		}
	}
}