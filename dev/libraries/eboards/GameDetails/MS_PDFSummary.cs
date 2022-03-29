using System;
using System.Windows.Forms;
using System.Drawing;
using System.Collections;
using System.IO;

using CoreUtils;
using GameManagement;
using ReportBuilder;

namespace GameDetails
{
	/// <summary>
	/// This handles both PDF and CSV summaries 
	/// </summary>
	public class MS_PDFSummary : PDFSummary
	{
		//We only have long full game reports at this time 
		bool ShortReport = false;

		public MS_PDFSummary(NetworkProgressionGameFile gameFile, EditGamePanel gamePanel, bool showCSVButton) :
			base(gameFile, gamePanel, showCSVButton)
		{
		}

		/// <summary>
		/// Just a helpher for building a string from the different round values 
		/// You can end with a total, an average or nothing
		/// </summary>
		/// <param name="LineName"></param>
		/// <param name="roundvalues"></param>
		/// <param name="UseTotalAsLastItem"></param>
		/// <param name="UseAverageAsLastItem"></param>
		/// <returns></returns>
		protected string GenerateCSV_Line(string LineName, float[] roundvalues, 
			bool UseTotalAsLastItem, bool UseAverageAsLastItem, bool showDecimalPlaces )
		{
			string linedata = "";
			string formatstr = "{0:0}";

			if (showDecimalPlaces)
			{
				formatstr = "{0:0.00}";
			}


			linedata += LineName+",";
			float sum = 0f;
			float count = 0f;
			if (roundvalues != null)
			{
				foreach (float val in roundvalues)
				{
					linedata += LibCore.CONVERT.Format(formatstr, val) + ",";
					sum += val;
					count+=1;
				}
			}
			if (UseTotalAsLastItem)
			{
				linedata += LibCore.CONVERT.Format(formatstr, sum);
			}
			if (UseAverageAsLastItem)
			{
				linedata += LibCore.CONVERT.Format(formatstr, (sum / count));
			}
			return linedata;
		}

		protected string GenerateCSV_TimeLine (string LineName, float[] roundvalues,
			bool UseTotalAsLastItem, bool UseAverageAsLastItem)
		{
			string linedata = "";

			linedata += LineName + ",";
			float sum = 0f;
			float count = 0f;
			if (roundvalues != null)
			{
				foreach (float val in roundvalues)
				{
					linedata += LibCore.CONVERT.FormatTimeHms(val) + ",";
					sum += val;
					count += 1;
				}
			}
			if (UseTotalAsLastItem)
			{
				linedata += LibCore.CONVERT.FormatTime(sum);
			}
			if (UseAverageAsLastItem)
			{
				linedata += LibCore.CONVERT.FormatTime(sum / count);
			}
			return linedata;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="reportfilename"></param>
		protected override void GenerateCSV(string reportfilename)
		{
			string transactionName =  SkinningDefs.TheInstance.GetData("transactionname");

			//Build up all the data
			string[] members = new string[20];
			string[] roles = new string[20];
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
			}

			//Build the CSV file 
			try
			{
				//Build up the substring of different data 
				FileInfo fi = new FileInfo(reportfilename);
				string fn2 = fi.Name;
				string fn3 = fi.FullName;
			
				_gameGameCreated = getDateCreated(fn2);
				DateTime cd = getDateCreated(fn2);
				string game_Created_Datestr = "";
				game_Created_Datestr += cd.Year.ToString()+"-";
				if (cd.Month<10) game_Created_Datestr+="0";
				game_Created_Datestr += ""+cd.Month.ToString()+"-";
				if (cd.Day<10) game_Created_Datestr+="0";
				game_Created_Datestr += ""+cd.Day.ToString()+"";

				float total_revenue = sumArray(revenues);
				float total_supportspend = sumArray(supportspends);
				float total_profitloss = sumArray(profits);
				float total_indicators = sumArray(indicators);

				//Create the file
				StreamWriter writer = new StreamWriter(reportfilename,false);
				if (writer != null)
				{
					if (ShortReport)
					{
						//Basic Game Version (a single line of csv text with just the totals)
						//GameTitle, GameVenue, GameDate, ActualRevenue, SupportSpend, ProfitLoss, MTRS, IndicatorScore 
						string line = "";
						line += stripcomma(title) + ",";
						line += stripcomma(venue) + ",";
						line += game_Created_Datestr + ",";
						line += total_revenue.ToString() + ",";
						line += total_supportspend.ToString() + ",";
						line += total_profitloss.ToString() + ",";
						line += total_indicators.ToString() + ",";
						writer.WriteLine(line);
						writer.Close();
					}
					else
					{
						string line = "";
						//Full Report 
						//Line 001  GameTitle, GameVenue, GameDate 
						line = "";
						line += stripcomma(title) + ",";
						line += stripcomma(venue) + ",";
						line += game_Created_Datestr + ",";
						writer.WriteLine(line);
						//Line 002  Empty Line 
						line = "";
						writer.WriteLine(line);
						//Line 003  Empty Line 
						line = "";
						writer.WriteLine(line);
						//Line 004  Table Headers 
						line += ",1,2,3,4,5,Total";
						writer.WriteLine(line);

						//Line 005  The Max Revenue per round
						line = GenerateCSV_Line("Maximum "+transactionName, transactions_max, true, false,false);
						writer.WriteLine(line);
						//Line 006  The Max Revenue per round
						line = GenerateCSV_Line("Actual "+transactionName, transactions_handled, true, false,false);
						writer.WriteLine(line);
						//Line 007  The Max Revenue per round
						line = GenerateCSV_Line("Maximum Revenue", revenue_max, true, false,true);
						writer.WriteLine(line);
						//Line 008  The Actual Revenue per round
						line = GenerateCSV_Line("Actual Revenue", revenues, true, false,true);
						writer.WriteLine(line);
						//Line 009  Fixed Costs
						line = GenerateCSV_Line("Fixed Costs", fixedcosts, true, false,true);
						writer.WriteLine(line);
						//Line 010  Support Budget 
						line = GenerateCSV_Line("Support Budgets", supportbudgets, true, false,true);
						writer.WriteLine(line);
						//Line 011  Project Costs
						line = GenerateCSV_Line("Project Costs", projcosts, true, false,true);
						writer.WriteLine(line);
						//Line 012  Profit / Loss
						line = GenerateCSV_Line("Profit / Loss ", profits, true, false,true);
						writer.WriteLine(line);
						//Line 013  Gain Loss
                        line = GenerateCSV_Line("Improvement On Previous Round", gains, true, false, true);
						writer.WriteLine(line);
						//Line 014  Empty Line 
						line = "";
						writer.WriteLine(line);
						//Line 015  Empty Line 
						line = GenerateCSV_Line("Support Budgets", supportbudgets, true, false,true);
						writer.WriteLine(line);
						//Line 016  Empty Line 
						line = GenerateCSV_Line("Support Spend", supportspends, true, false,true);
						writer.WriteLine(line);
						//Line 017  Empty Line 
						line = GenerateCSV_Line("Support Profit Loss", supportprofits, true, false,true);
						writer.WriteLine(line);
						//Line 018  Empty Line 
						line = "";
						writer.WriteLine(line);
						//Line 019  Availiabity
						line = GenerateCSV_Line("Availabilty", avails, false, true,true);
						writer.WriteLine(line);
						//Line 020  MTRS
						line = GenerateCSV_TimeLine("MTRS", mtrs, false, false);
						writer.WriteLine(line);
						//Line 021  Indicator Score 
						line = GenerateCSV_Line("Indicator Score", indicators, false, false,true);
						writer.WriteLine(line);
						//Line 022  Empty Line 
						line = "";
						writer.WriteLine(line);
						//Line 023  Empty Line 
						line = "";
						writer.WriteLine(line);
						//Line 024 The Headers of the players table
						line = "Players,Roles";
						writer.WriteLine(line);
						//Line 025 The Headers of the players table  
						for (int step=0; step < 20; step++)
						{
							string name = members[step];
							string role = roles[step];
							if ((name != null)&(role != null))
							{
								if ((name != "")&(role != ""))
								{
									line = name+","+role;
									writer.WriteLine(line);
								}
							}
						}
						writer.Close();
					}
				}
			}
			catch (Exception)
			{
				MessageBox.Show(_gamePanel.TopLevelControl, "Cannot present CSV Summary Sheet", "CSV Generation Failled"
					,MessageBoxButtons.OK,MessageBoxIcon.Error);
			}

			//Try to open the CSV with the registered editor (usually Excel)

			if (File.Exists(reportfilename))
			{
				try
				{
					System.Diagnostics.Process.Start(reportfilename);		
				}
				catch (Exception)
				{
					MessageBox.Show(_gamePanel.TopLevelControl, "Cannot present CSV Summary Sheet ", "CSV Summary"
						,MessageBoxButtons.OK,MessageBoxIcon.Error);
				}
			}

		}

		protected override void GeneratePDF (Bitmap teamphoto, Bitmap back, Bitmap facilitatorLogo, string reportfilename)
		{
			//FOR THE REFACTOR - pass all this info in as an object, 
			//no time to change all the pdf code just now
			string[] members = new string[20];
			string[] roles = new string[20];
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

			Hashtable champ = new Hashtable();

			int prevprofit = 0;
			int newservices = 0;
			for (int i = 1; i <= _gameFile.LastOpsRoundPlayed; i++)
			{
				SupportSpendOverrides sso = new SupportSpendOverrides(_gameFile);
				RoundScores score = new RoundScores(_gameFile, i, prevprofit, newservices, sso);

				prevprofit = score.Profit;
				newservices = score.NumNewServices;

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
