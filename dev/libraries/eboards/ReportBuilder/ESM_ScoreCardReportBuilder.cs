using System;
using System.Collections.Generic;
using System.Xml;

using CoreUtils;
using GameManagement;
using LibCore;
using Network;

namespace ReportBuilder
{
    public class ESM_ScoreCardReportBuilder : BaseScorecardReport<EsmRoundScores>
    {
        readonly Dictionary<string, string> functionNamesToDisplayNames;

        readonly string requestsDisplayName;
        readonly string functionDisplayName;
        readonly string mtfrDisplayName;

        public ESM_ScoreCardReportBuilder(NetworkProgressionGameFile gameFile, List<EsmRoundScores> scores):
            base (gameFile, scores)
        {
            requestsDisplayName = SkinningDefs.TheInstance.GetData("requests_display_name", "Requests");
            functionDisplayName = SkinningDefs.TheInstance.GetData("function_display_name", "Function");
            mtfrDisplayName = SkinningDefs.TheInstance.GetData("mtfr_display_name", "MTFR");

            functionNamesToDisplayNames = new Dictionary<string, string>();

            NodeTree model = gameFile.GetNetworkModel(gameFile.CurrentRound);
            foreach (var function in model.GetNamedNode("Functions").GetChildrenAsList())
            {
                functionNamesToDisplayNames[function.GetAttribute("name")] = function.GetAttribute("desc");
            }

        }

        protected override void GenerateReport ()
        {
            AddColumnHeadings();

            AddResults();
        }

        protected override string GetFilename ()
        {
            return gameFile.GetGlobalFile("ScoreCardReport.xml");
        }

        [Obsolete("Remove when base method is removed")]
        public override string BuildReport()
        {
            return CreateReportAndFilename();
        }

        void AddResults()
        {
            AddBusinessSection();

            AddRequestsSection();

            AddMtfrSection();

            AddServiceStatisticsSection();
        }

        void AddBusinessSection()
        {
            AddSectionHeading("Business Performance");

            XmlElement resultsTable = CreateResultsTableTemplate();

            AddMetricRow(resultsTable, "Revenue ($M)", score => score.Revenue, TotalStyle.Total,
                FigureFormatting.MoneyMillions);
            AddMetricRow(resultsTable, "Costs ($M)", score => score.Costs, TotalStyle.Total, FigureFormatting.MoneyMillions);
            AddMetricRow(resultsTable, "Profit ($M)", score => score.Profit, TotalStyle.Total, FigureFormatting.MoneyMillions);
            AddMetricRow(resultsTable, "Target ($M)", score => score.ProfitTarget, TotalStyle.Total, FigureFormatting.MoneyMillions);
            AddMetricRow(resultsTable, "Target Profit Achieved (%)", score => score.PercentTargetProfit, TotalStyle.Mean, FigureFormatting.Percentage);
            AddMetricRow(resultsTable, "Position", score => score.Position, TotalStyle.Mean, FigureFormatting.Integer);

        }

        void AddRequestsSection()
        {
            AddSectionHeading(CONVERT.Format("{0} (By {1})", requestsDisplayName, functionDisplayName));
            
            XmlElement resultsTable = CreateResultsTableTemplate();

            AddMetricRow(resultsTable, functionNamesToDisplayNames["HR"], score => score.HrRequests, TotalStyle.Total, FigureFormatting.Integer);
            AddMetricRow(resultsTable, functionNamesToDisplayNames["FM"], score => score.FacRequests, TotalStyle.Total, FigureFormatting.Integer);
            AddMetricRow(resultsTable, functionNamesToDisplayNames["IT"], score => score.ItRequests, TotalStyle.Total, FigureFormatting.Integer);
            AddMetricRow(resultsTable, functionNamesToDisplayNames["Other"], score => score.FinLegRequests, TotalStyle.Total, FigureFormatting.Integer);
        }

        void AddMtfrSection()
        {
            AddSectionHeading(CONVERT.Format("{0} (By {1})", mtfrDisplayName, functionDisplayName));
            
            XmlElement resultsTable = CreateResultsTableTemplate();

            AddMetricRow(resultsTable, functionNamesToDisplayNames["HR"], score => score.HrMtrs, TotalStyle.Mean, FigureFormatting.Time);
            AddMetricRow(resultsTable, functionNamesToDisplayNames["FM"], score => score.FacMtrs, TotalStyle.Mean, FigureFormatting.Time);
            AddMetricRow(resultsTable, functionNamesToDisplayNames["IT"], score => score.ItMtrs, TotalStyle.Mean, FigureFormatting.Time);
            AddMetricRow(resultsTable, functionNamesToDisplayNames["Other"], score => score.FinLegMtrs, TotalStyle.Mean, FigureFormatting.Time);

        }

        void AddServiceStatisticsSection()
        {
            AddSectionHeading("Service Statistics");

            XmlElement resultsTable = CreateResultsTableTemplate();

            AddMetricRow(resultsTable, "Total " + requestsDisplayName, score => score.Requests + score.Incidents, TotalStyle.Total, FigureFormatting.Integer);
            AddMetricRow(resultsTable, mtfrDisplayName, score => score.Mtrs, TotalStyle.Mean, FigureFormatting.Time);
            AddMetricRow(resultsTable, "Recurring " + requestsDisplayName, score => score.RecurringRequests + score.RecurringIncidents, TotalStyle.Total, FigureFormatting.Integer);
            AddMetricRow(resultsTable, "Self-Fulfilled " + requestsDisplayName, score => score.SelfFulfilledRequests, TotalStyle.Total, FigureFormatting.Integer);
            AddMetricRow(resultsTable, "Automated " + requestsDisplayName, score => score.AutomatedRequests, TotalStyle.Total, FigureFormatting.Integer);
            
           
        }
    }

}