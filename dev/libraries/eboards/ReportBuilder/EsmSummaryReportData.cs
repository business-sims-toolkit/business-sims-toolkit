using System;
using GameManagement;

namespace ReportBuilder
{
	public class EsmSummaryReportData : ItilSummaryReportData
	{
		//public float [] ProfitTarget;
		//public float [] Spend;
		//public float [] Benefit;
		//public float [] TimeToValue;
		//public float [] Incidents;

        public float[] revenue;
        public float[] costs;
        public float[] profit;
        public float[] profitTarget;
        public float[] percentTargetAchieved;
        public float[] benefit;
        public float[] position;
        public float[] totalRequests;
        public float[] mtfr;
        public float[] recurrentRequests;
        public float[] selfFulfilledRequests;
        public float[] automatedRequests;
        public float[] highestProductivity;
        public float[] lowestProductivity;
        public float[] averageProductivity;
        public float[] productivityDrain;

        public EsmSummaryReportData(NetworkProgressionGameFile gameFile, DateTime date)
            : base(gameFile, date)
        {
            //ProfitTarget = new float [RoundsMax];
            //Spend = new float [RoundsMax];
            //Benefit = new float [RoundsMax];
            //TimeToValue = new float [RoundsMax];
            //Incidents = new float [RoundsMax];


            revenue = new float[RoundsMax];
            costs = new float[RoundsMax];
            profit = new float[RoundsMax];
            profitTarget = new float[RoundsMax];
            percentTargetAchieved = new float[RoundsMax];
            benefit = new float[RoundsMax];
            position = new float[RoundsMax];
            totalRequests = new float[RoundsMax];
            mtfr = new float[RoundsMax];
            recurrentRequests = new float[RoundsMax];
            selfFulfilledRequests = new float[RoundsMax];
            automatedRequests = new float[RoundsMax];
            highestProductivity = new float[RoundsMax];
            lowestProductivity = new float[RoundsMax];
            averageProductivity = new float[RoundsMax];
            productivityDrain = new float[RoundsMax];
            
            int previousProfit = 0;
            int newServices = 0;
            for (int round = 0; round < RoundsPlayed; round++)
            {
                EsmRoundScores scores = (EsmRoundScores)ExtractRoundScores(round, previousProfit, newServices);

                //ProfitTarget[round] = (float)scores.ProfitTarget;
                //Spend[round] = (float)scores.Spend;
                //Benefit[round] = (float)scores.Benefit;
                //TimeToValue[round] = (float)scores.TimeToValue;
                //Incidents[round] = (float)scores.Incidents;

                revenue[round] = (float)scores.Revenue;
                costs[round] = (float)scores.Costs;
                profit[round] = (float)scores.Profit;
                profitTarget[round] = (float)scores.ProfitTarget;
                percentTargetAchieved[round] = (float)scores.PercentTargetProfit;
                benefit[round] = (float)scores.Benefit;
                position[round] = (float)scores.Position;
                totalRequests[round] = (float)scores.Requests + (float) scores.Incidents;
                mtfr[round] = (float)scores.Mtrs;
                recurrentRequests[round] = (float)scores.RecurringRequests + (float)scores.RecurringIncidents;
                selfFulfilledRequests[round] = (float)scores.SelfFulfilledRequests;
                automatedRequests[round] = (float)scores.AutomatedRequests;
                highestProductivity[round] = scores.MaxProductivity.HasValue ? (float)scores.MaxProductivity : 0;
                lowestProductivity[round] = scores.MinProductivity.HasValue ? (float)scores.MinProductivity : 0;
                averageProductivity[round] = scores.MeanProductivity.HasValue ? (float)scores.MeanProductivity : 0;
                productivityDrain[round] = (float)scores.ProductivityDrain;
            }
        }

		protected override RoundScores ExtractRoundScores (int round, int previousProfit, int newServices)
		{
			return new EsmRoundScores (GameFile, round + 1, previousProfit, newServices, SpendOverrides);
		}
	}
}