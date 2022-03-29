using System;
using System.Drawing;

using GameManagement;
using LibCore;

namespace ReportBuilder
{
	public class SummaryReportData
	{
		public NetworkProgressionGameFile GameFile;
		public SupportSpendOverrides SpendOverrides;

		public int RoundsPlayed;
		public int RoundsMax;

		public Image Background;
		public Image FacilitatorLogo;
		public Image TeamPhoto;
		public bool TeamPhotoHasReplacedDefaultImage;

		public DateTime Date;

		public string Title;
		public string Venue;

		public string [] TeamMembers;
		public string [] TeamRoles;

		public float [] Availability;
		public float [] MTRS;

		public float [] TransactionsAchieved;
		public float [] TransactionsMax;

		public float [] RevenueAchieved;
		public float [] RevenueMax;

		public float [] FixedCosts;
		public float [] ProjectSpend;

		public float [] Profit;
		public float [] ProfitGain;

		public float [] SupportBudget;
		public float [] SupportCosts;
		public float [] SupportProfit;
        public float[] totalFines;

		public float [] IndicatorScore;

		public SummaryReportData (NetworkProgressionGameFile gameFile, DateTime date)
		{
			GameFile = gameFile;
			Date = date;

			ExtractData();
		}

		protected virtual void ExtractData ()
		{
			SpendOverrides = new SupportSpendOverrides(GameFile);

			Background = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + @"\images\PDFTemplate.png");
			FacilitatorLogo = GameFile.GetFacilitatorLogo();
			TeamPhoto = GameFile.GetPdfTeamPhoto();
			TeamPhotoHasReplacedDefaultImage = GameFile.IsPdfTeamPhotoOverriddenFromDefault();

			Title = GameFile.GetTitle();
			Venue = GameFile.GetVenue();

			int teamCount = 30;
			TeamMembers = new string [teamCount];
			TeamRoles = new string [teamCount];
			GameFile.GetTeamMembersAndRoles(TeamMembers, TeamRoles);

			RoundsPlayed = GameFile.LastOpsRoundPlayed;
			RoundsMax = CoreUtils.SkinningDefs.TheInstance.GetIntData("roundcount", 5);

			Availability = new float [RoundsMax];
			MTRS = new float [RoundsMax];
			TransactionsAchieved = new float [RoundsMax];
			TransactionsMax = new float [RoundsMax];
			RevenueAchieved = new float [RoundsMax];
			RevenueMax = new float [RoundsMax];
			FixedCosts = new float [RoundsMax];
			ProjectSpend = new float [RoundsMax];
			Profit = new float [RoundsMax];
			ProfitGain = new float [RoundsMax];
			SupportBudget = new float [RoundsMax];
			SupportCosts = new float [RoundsMax];
			SupportProfit = new float [RoundsMax];
            IndicatorScore = new float[RoundsMax];
            totalFines = new float[RoundsMax];

			InitialiseArrays();

			int previousProfit = 0;
			int newServices = 0;
			for (int round = 0; round < RoundsPlayed; round++)
			{
				RoundScores scores = ExtractRoundScores(round, previousProfit, newServices);

				Availability[round] = (float) scores.Availability;
				MTRS[round] = (float) scores.MTTR;

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
                totalFines[round] = (scores.RegulationFines + scores.ComplianceFines) / 1000000.0f;

				IndicatorScore[round] = scores.IndicatorScore;
			}
		}

		protected virtual RoundScores ExtractRoundScores (int round, int previousProfit, int newServices)
		{
			return new RoundScores (GameFile, round + 1, previousProfit, newServices, SpendOverrides);
		}

		protected virtual void InitialiseArrays ()
		{
		}
	}
}