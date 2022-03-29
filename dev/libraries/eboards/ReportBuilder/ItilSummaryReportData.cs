using System;
using GameManagement;

namespace ReportBuilder
{
	public class ItilSummaryReportData : SummaryReportData
	{
		public float [] TradesTargets;
		public float [] FirstLineFixes;

		public ItilSummaryReportData (NetworkProgressionGameFile gameFile, DateTime date)
			: base (gameFile, date)
		{
			TradesTargets = new float [RoundsMax];
			FirstLineFixes = new float [RoundsMax];

			int previousProfit = 0;
			int newServices = 0;
			for (int round = 0; round < RoundsPlayed; round++)
			{
				RoundScores scores = ExtractRoundScores(round, previousProfit, newServices);

				TradesTargets[round] = scores.TargetTransactions;
				FirstLineFixes[round] = scores.FirstLineFixes;
			}
		}
	}
}