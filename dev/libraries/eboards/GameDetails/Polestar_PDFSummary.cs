using System;
using System.Windows.Forms;
using System.Drawing;
using System.Collections;
using System.IO;

using GameManagement;
using ReportBuilder;

namespace GameDetails
{
	/// <summary>
	/// Summary description for Polestar_PDFSummary.
	/// </summary>
	public class Polestar_PDFSummary : PDFSummary
	{
		public Polestar_PDFSummary(NetworkProgressionGameFile gameFile, EditGamePanel gamePanel, bool showCSVButton) :
			base(gameFile, gamePanel, showCSVButton)
		{
		}

		protected override void GeneratePDF(Bitmap teamphoto, Bitmap back, Bitmap facilitatorLogo, string reportfilename)
		{
			//FOR THE REFACTOR - pass all this info in as an object, 
			//no time to change all the pdf code just now
			string[] members = new string[24];
			string[] roles = new string[24];
			this.GetTeamMembersandRoles(members, roles);

			string title = "";
			string venue = "";
			GetTitleandVenue(ref title, ref venue);

			int NumRounds = 5;
			int NumTeams = 5;

			float[] avails = new float[NumRounds];
			float[] profits = new float[NumRounds];
			int[] pointsR1 = new int[NumTeams];
			int[] pointsR2 = new int[NumTeams];
			int[] pointsR3 = new int[NumTeams];
			int[] pointsR4 = new int[NumTeams];
			int[] pointsR5 = new int[NumTeams];
			float[] fixedcosts = new float[NumRounds];
			float[] gains = new float[NumRounds];
			float[] indicators = new float[NumRounds];
			float[] mtrs = new float[NumRounds];
			float[] projcosts = new float[NumRounds];
			float[] revenues = new float[NumRounds];
			float[] supportbudgets = new float[NumRounds];
			float[] supportprofits = new float[NumRounds];
			float[] supportspends = new float[NumRounds];
			float[] transactions_handled = new float[NumRounds];
			float[] transactions_max = new float[NumRounds];
			float[] revenue_max = new float[NumRounds];
            float[] tradesTargets = new float[NumRounds];
            float[] totalFines = new float[NumRounds];

			Hashtable champ = new Hashtable();

			int prevprofit = 0;
			int newservices = 0;
			for (int i = 1; i <= _gameFile.LastOpsRoundPlayed; i++)
			{
				SupportSpendOverrides sso = new SupportSpendOverrides(_gameFile);
				RoundScores score = new RoundScores(_gameFile, i, prevprofit, newservices, sso);

				prevprofit = score.Profit;

				float tmp = (float)score.NumTransactions;
				transactions_handled.SetValue(tmp, i - 1);

				tmp = (float)score.MaxTransactions;
				transactions_max.SetValue(tmp, i - 1);

				tmp = (float)score.MaxRevenue / 1000000;
				revenue_max.SetValue(tmp, i - 1);

				tmp = (float)score.Revenue / 1000000;
				revenues.SetValue(tmp, i - 1);

				tmp = (float)score.FixedCosts / 1000000;
				fixedcosts.SetValue(tmp, i - 1);

				tmp = (float)score.SupportBudget / 1000000;
				supportbudgets.SetValue(tmp, i - 1);

				tmp = (float)score.ProjectSpend / 1000000;
				projcosts.SetValue(tmp, i - 1);

				float profit = (float)score.Profit / 1000000;
				profits.SetValue(profit, i - 1);

				tmp = (float)score.Gain / 1000000;
				gains.SetValue(tmp, i - 1);

				tmp = (float)score.SupportCostsTotal / 1000000;
				supportspends.SetValue(tmp, i - 1);

				tmp = (float)score.SupportProfit / 1000000;
				supportprofits.SetValue(tmp, i - 1);

				tmp = (float)score.Availability;
				avails.SetValue(tmp, i - 1);

				mtrs.SetValue((float)score.MTTR, i - 1);

				tmp = (float)score.IndicatorScore;
				indicators.SetValue(tmp, i - 1);

				tradesTargets[i - 1] = score.TargetTransactions;

                tmp = (float)(score.RegulationFines + score.ComplianceFines) / 1000000.0f;
                totalFines.SetValue(tmp, i - 1);
			}

			if (File.Exists(reportfilename))
			{
				try
				{
					System.Diagnostics.Process.Start(reportfilename);		
				}
				catch (Exception evc)
				{
					if (evc.Message.IndexOf("No Application")>-1)
					{
						MessageBox.Show(_gamePanel.TopLevelControl, "Cannot present PDF Summary Sheet ", "No PDF Reader Application Installed"
							,MessageBoxButtons.OK,MessageBoxIcon.Error);
					}
					else
					{
						MessageBox.Show(_gamePanel.TopLevelControl, "Cannot present PDF Summary Sheet ", "Failed to Start PDF Reader."
							,MessageBoxButtons.OK,MessageBoxIcon.Error);
					}
				}
			}
		}
	}
}
