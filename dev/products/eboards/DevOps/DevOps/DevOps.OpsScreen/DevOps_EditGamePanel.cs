using System;
using System.Collections.Generic;

using GameDetails;

using CoreUtils;

using GameManagement;
using ReportBuilder;

using DevOps.ReportsScreen;
using DevOpsRoundScores = ReportBuilder.DevOpsRoundScores;

namespace DevOps.OpsScreen
{
	public class DevOps_EditGamePanel : PS_EditGamePanel
	{
        SupportSpendOverrides spendOverrides;
        ReportSection reportSection;
		HoldScreenTextSection holdScreenTextSection;

		public DevOps_EditGamePanel (NetworkProgressionGameFile game, IGameLoader gamePanel, SupportSpendOverrides spendOverrides)
            : base (game, gamePanel)
		{
            this.spendOverrides = spendOverrides;
		}

        protected override void Add_Team_Members_Section ()
        {
        }

        protected override void  Add_PDF_Section()
        {
        }

		protected override void BuildRightHandPanel ()
		{
			AddLogosSection();
			AddHoldScreenTextSection();
			AddTeamDetailsSection();
			AddCustomReportSection();
			Add_PDF_Section();
		}

		void AddCustomReportSection ()
		{
			var customReports = new CustomReportSection(_gameFile);
			customReports.Changed += customReports_Changed;
			rightExpandHolder.AddExpandablePanel(customReports);
		}

		void customReports_Changed (object sender, EventArgs args)
		{
			OnReportsInvalidated();
		}

		void AddHoldScreenTextSection ()
		{
			holdScreenTextSection = new HoldScreenTextSection (this, _gameFile, "Round Complete");
			rightExpandHolder.AddExpandablePanel(holdScreenTextSection);

			holdScreenTextSection.LoadData();
		}

		protected override void  AddTeamDetailsSection()
        {
            reportSection = new ReportSection (this, _gameFile, false, true, true, false);
            reportSection.GenerateReport += reportSection_GenerateReport;
            rightExpandHolder.AddExpandablePanel(reportSection);
        }

        protected override void AddLogosSection()
        {
            logoDetails = new DevOpsLogoDetails(_gameFile, true, false);
            rightExpandHolder.AddExpandablePanel(logoDetails);
        }

        protected override void AddProcessMaturitySection()
        {
			maturityDetails = new ProcessMaturityDetails (_gameFile, gamePanel);
			leftExpandHolder.AddExpandablePanel(maturityDetails);

			maturityDetails.AddType("DevOps", "ITIL", "ITIL", em_GameEvalType.ITIL);

			if (SkinningDefs.TheInstance.GetBoolData("game_type_allow_custom", true))
			{
				maturityDetails.AddCustomType("Load custom file...", "CUSTOM", "Custom");
			}

			maturityDetails.LoadData();
        }

        void reportSection_GenerateReport (object sender, GenerateReportEventArgs args)
        {
            List<DevOpsRoundScores> scores = new List<DevOpsRoundScores> ();
            int profit = 0;
            int revenue = 0;

            for (int i = 1; i <= _gameFile.LastRoundPlayed; i++)
            {
                DevOpsRoundScores roundScores = new DevOpsRoundScores (_gameFile, i, profit, 0, revenue, spendOverrides);
                scores.Add(roundScores);
                profit = roundScores.Profit;
                revenue = roundScores.Revenue;
            }
        }
	}
}