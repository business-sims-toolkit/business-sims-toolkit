using System;
using System.Collections.Generic;

using CoreUtils;
using GameManagement;

namespace ReportBuilder
{
    public class DevOpsBusinessScorecardReportBuilder : BaseScorecardReport<DevOpsRoundScores>
    {
        public DevOpsBusinessScorecardReportBuilder (NetworkProgressionGameFile gameFile, List<DevOpsRoundScores> roundScores, bool includeTotalColumn = true)
            : base(gameFile, roundScores, includeTotalColumn)
        {
        }

        [Obsolete("Remove when base method is removed")]
        public override string BuildReport()
        {
            return CreateReportAndFilename();
        }

        protected override void GenerateReport()
        {
            AddColumnHeadings();

            AddBusinessPerformanceSection();

            AddNewAppsSection();
        }

        protected override string GetFilename ()
        {
            return gameFile.GetGlobalFile("businessScorecardReport.xml");
        }

        void AddBusinessPerformanceSection()
        {
            AddSectionHeading("Business Performance");

            var results = CreateResultsTableTemplate();

            var transactionName = SkinningDefs.TheInstance.GetData("transactionname");

            AddMetricRow(results, "Handled " + transactionName, score => score.NumTransactions, TotalStyle.Total, FigureFormatting.Integer);

            AddMetricRow(results, "Maximum " + transactionName, score => score.MaxTransactions, TotalStyle.Total,
                FigureFormatting.Integer);

            AddMetricRow(results, "Maximum Revenue (" + SkinningDefs.TheInstance.GetCurrencySymbol() + "M)", score => score.MaxRevenue, TotalStyle.Total,
                FigureFormatting.MoneyMillions);

            AddMetricRow(results, "Actual Revenue (" + SkinningDefs.TheInstance.GetCurrencySymbol() + "M)", score => score.Revenue, TotalStyle.Total,
                FigureFormatting.MoneyMillions);

            AddMetricRow(results, "Support Costs (" + SkinningDefs.TheInstance.GetCurrencySymbol() + "M)", score => score.SupportCostsTotal, TotalStyle.Total,
                FigureFormatting.MoneyMillions);

            AddMetricRow(results, "Fixed Costs (" + SkinningDefs.TheInstance.GetCurrencySymbol() + "M)", score => score.FixedCosts, TotalStyle.Total,
                FigureFormatting.MoneyMillions);

            AddMetricRow(results, "New App Costs (" + SkinningDefs.TheInstance.GetCurrencySymbol() + "M)", score => score.Expenditure, TotalStyle.Total,
                FigureFormatting.MoneyMillions);

            AddMetricRow(results, "Profit/Loss (" + SkinningDefs.TheInstance.GetCurrencySymbol() + "M)", score => score.NewServiceProfitLoss, TotalStyle.Total,
                FigureFormatting.MoneyMillions);

            AddMetricRow(results, "Gain on the Previous Round (" + SkinningDefs.TheInstance.GetCurrencySymbol() + "M)",
                score => ((score.Round > 1) ? score.GainOnPreviousRound : (double?)null), TotalStyle.None, FigureFormatting.MoneyMillions);

        }

        void AddNewAppsSection()
        {
            AddSectionHeading("New Apps");

            var results = CreateResultsTableTemplate();

            AddMetricRow(results, "Investment Budget (" + SkinningDefs.TheInstance.GetCurrencySymbol() + "M)", score => score.InvestmentBudget,
                TotalStyle.Total, FigureFormatting.MoneyMillions);

            AddMetricRow(results, "Expenditure (" + SkinningDefs.TheInstance.GetCurrencySymbol() + "M)", score => score.Expenditure, TotalStyle.Total,
                FigureFormatting.MoneyMillions);

            AddMetricRow(results, "Target Sales (" + SkinningDefs.TheInstance.GetCurrencySymbol() + "M)", score => score.TargetNewServicesSales,
                TotalStyle.Total, FigureFormatting.MoneyMillions);

            AddMetricRow(results, "Sales Handled From New Apps (" + SkinningDefs.TheInstance.GetCurrencySymbol() + "M)",
                score => score.RevenueFromNewServices, TotalStyle.Total, FigureFormatting.MoneyMillions);

            AddMetricRow(results, "Maximum Potential (" + SkinningDefs.TheInstance.GetCurrencySymbol() + "M)", score => score.MaximumPotential, TotalStyle.Total, FigureFormatting.MoneyMillions);

            AddMetricRow(results, "Percentage of Target Sales", score => score.PercentageOfTargetSales, TotalStyle.Mean,
                FigureFormatting.Percentage);

            AddMetricRow(results, "Number of New Apps Deployed", score => score.NumDeployedServices, TotalStyle.Total,
                FigureFormatting.Integer);

            AddMetricRow(results, "Average Time To Deploy (TTD)", score => score.AverageDeployTime, TotalStyle.Mean,
                FigureFormatting.Time);
        }
    }
}
