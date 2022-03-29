using System;
using System.Collections.Generic;
using GameManagement;
using ReportBuilder;

namespace DevOps.ReportsScreen
{
    public class DevOpsSummaryReportData : SummaryReportData
    {
        IList<DevOpsRoundScores> roundScores;

        public DevOpsSummaryReportData(NetworkProgressionGameFile gameFile, DateTime date, IList<DevOpsRoundScores> roundScores)
            : base(gameFile, date)
        {
            this.roundScores = roundScores;
        }

        public IList<DevOpsRoundScores> RoundScores
        {
            get
            {
                return roundScores;
            }
        }
    }
}