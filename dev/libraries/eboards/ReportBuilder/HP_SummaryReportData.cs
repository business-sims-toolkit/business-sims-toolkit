using System;
using System.Collections.Generic;
using System.Collections;
using GameManagement;

namespace ReportBuilder
{
	public class HP_SummaryReportData : SummaryReportData
	{
		public int Teams;

		public int [][] TeamPoints;
		public int [] TeamTotalPoints;
		public float [] PlayerPoints;

		public HP_SummaryReportData (NetworkProgressionGameFile gameFile, DateTime date)
			: base (gameFile, date)
		{
			for (int team = 0; team < Teams; team++)
			{
				TeamTotalPoints[team] = 0;

				for (int round = 0; round < RoundsPlayed; round++)
				{
					TeamTotalPoints[team] += TeamPoints[round][team];
				}
			}
		}

		protected override void InitialiseArrays ()
		{
			Teams = 5;

			TeamPoints = new int [RoundsMax] [];
			TeamTotalPoints = new int [Teams];
			PlayerPoints = new float [RoundsMax];
		}

		protected override RoundScores ExtractRoundScores (int round, int previousProfit, int newServices)
		{
			RoundScores scores = base.ExtractRoundScores(round, previousProfit, newServices);

			PlayerPoints[round] = scores.Points;

			TeamPoints[round] = new int [Teams];

			Hashtable teamNameToPointsHashtable = scores.GetRoundPoints();
			Dictionary<string, int> teamNameToPoints = new Dictionary<string, int> ();
			List<string> sortedTeamNames = new List<string> ();
			foreach (string teamName in teamNameToPointsHashtable.Keys)
			{
				sortedTeamNames.Add(teamName);
				teamNameToPoints.Add(teamName, (int) teamNameToPointsHashtable[teamName]);
			}
			sortedTeamNames.Sort();

            if (teamNameToPoints.Count != 0)
            {
                for (int i = 0; i < Teams; i++)
                {
                    TeamPoints[round][i] = teamNameToPoints[sortedTeamNames[i]];
                }
            }

			TeamTotalPoints = new int [Teams];

			return scores;
		}
	}
}