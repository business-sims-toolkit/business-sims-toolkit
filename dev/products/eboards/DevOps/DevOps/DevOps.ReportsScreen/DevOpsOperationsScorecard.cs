using System;
using System.Collections.Generic;

using GameManagement;
using CoreUtils;
using ReportBuilder;

namespace DevOps.ReportsScreen
{
	public class DevOpsOperationsScorecard : BaseScorecardReport<DevOpsRoundScores>
    {

        public DevOpsOperationsScorecard(NetworkProgressionGameFile gameFile, List<DevOpsRoundScores> scores):
            base (gameFile, scores)
        {

        }

        [Obsolete("Remove when base method is removed")]
        public override string BuildReport()
        {
            return CreateReportAndFilename();
        }

        protected override void GenerateReport ()
        {
            AddColumnHeadings();

            AddFinancialPerformanceSection();

            AddItPerformanceSection();
        }

        protected override string GetFilename()
        {
            return gameFile.GetGlobalFile("operationsScorecardReport.xml");
        }

        void AddFinancialPerformanceSection()
        {
            AddSectionHeading("Financial Performance");

            var results = CreateResultsTableTemplate();

            AddMetricRow(results, "Budget (" + SkinningDefs.TheInstance.GetCurrencySymbol() + "M)", score => score.SupportBudget, TotalStyle.Total, FigureFormatting.MoneyMillions);

            AddMetricRow(results, "Support Costs (" + SkinningDefs.TheInstance.GetCurrencySymbol() + "M)", score => score.SupportCostsTotal, TotalStyle.Total,
                FigureFormatting.MoneyMillions);

            AddMetricRow(results, "Fixed Costs (" + SkinningDefs.TheInstance.GetCurrencySymbol() + "M)", score => score.FixedCosts, TotalStyle.Total, FigureFormatting.MoneyMillions);

            AddMetricRow(results, "Spend (" + SkinningDefs.TheInstance.GetCurrencySymbol() + "M)", score => score.OpsSpend, TotalStyle.Total, FigureFormatting.MoneyMillions);

            AddMetricRow(results, "Profit/Loss (" + SkinningDefs.TheInstance.GetCurrencySymbol() + "M)", score => score.OpsProfit, TotalStyle.Total, FigureFormatting.MoneyMillions);
        }
        
        void AddItPerformanceSection()
        {
            AddSectionHeading("IT Performance");

            var results = CreateResultsTableTemplate();

            AddMetricRow(results, "Availability", score => score.Availability, TotalStyle.Mean, FigureFormatting.Percentage);

            AddMetricRow(results, "MTTR", score => score.MTTR, TotalStyle.Mean, FigureFormatting.Time);

            AddMetricRow(results, "Total Failures", score => score.Incidents, TotalStyle.Total, FigureFormatting.Integer);

            AddMetricRow(results, "First Line Fixes", score => score.FirstLineFixes, TotalStyle.Total, FigureFormatting.Integer);

	        AddMetricRow(results, "Recurring Failures", score => score.RecurringIncidents, TotalStyle.Total, FigureFormatting.Integer);

			AddMetricRow(results, "Prevented Incidents", score => score.PreventedIncidents, TotalStyle.Total, FigureFormatting.Integer);

            AddMetricRow(results, "Workarounds", score => score.NumWorkarounds, TotalStyle.Total, FigureFormatting.Integer);

            AddMetricRow(results, "SLA Breaches", score => score.NumSLAbreaches, TotalStyle.Total, FigureFormatting.Integer);
        }
    }
}
