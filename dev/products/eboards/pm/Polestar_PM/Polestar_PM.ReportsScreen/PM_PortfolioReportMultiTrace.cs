using System;
using System.Drawing;
using System.Xml;
using System.Collections;
using System.IO;

using GameManagement;
using LibCore;
using CoreUtils;
using Logging;
using Network;
using ReportBuilder;

namespace Polestar_PM.ReportsScreen
{
	/// <summary>
	/// This is used to provide a set of line graph covering 4 different aspects of Round 3 
	///  (Transactions, CostReduction, People Employed and Budget Employed)
	/// We need to display what the players did and what the target was in each of the aspects  
	/// We generate 4 different xml report files to hold the information 
	/// The display class just creates 4 different line garphs and imports the different data files
	/// 
	/// We can display the actual programs as well in the graphs (if we wanted to)
	/// But this does lead to 8 different traces per graph which is not a clear picture.
	/// </summary>
	public class PM_PortfolioReportMultiTrace
	{
		static readonly int totalProgramNumber = -1;
		static readonly int targetProgramNumber = -2;

		public enum Metric
		{
			Transactions,
			CostReduction,
			PeopleEmployed,
			BudgetEmployed
		}

		double lastKnownTime;
		Hashtable programNumberToTransactionData;
		Hashtable programNumberToCostReductionData;
		Hashtable programNumberToEmployeeData;
		Hashtable programNumberToPredictedBudgetData;

		public PM_PortfolioReportMultiTrace ()
		{
		}

		public string BuildReport (NetworkProgressionGameFile gameFile, Metric metric)
		{
			int round = 3;
			string report_name = "PortfolioReport_transactions.xml";

			NodeTree model = GetModelForRound(gameFile, round);

			// Initialise the list of portfolios and their names.
			ArrayList programNumbers = new ArrayList ();
			Hashtable programNumberToName = new Hashtable ();

			Node portfoliosNode = model.GetNamedNode("Portfolios");
			Node portfolioNode = portfoliosNode.GetFirstChildOfType("Portfolio");
			ArrayList programs = new ArrayList ();
			programs.AddRange(portfolioNode.GetChildrenOfType("Program"));
			programs.AddRange(portfoliosNode.GetChildrenOfType("Program"));
			foreach (Node programNode in programs)
			{
				int number = programNode.GetIntAttribute("shortdesc", 0);
				programNumbers.Add(number);
				programNumberToName.Add(number, programNode.GetAttribute("desc"));
			}
			programNumbers.Add(totalProgramNumber);
			programNumberToName.Add(totalProgramNumber, "Total");

			programNumbers.Add(targetProgramNumber);
			programNumberToName.Add(targetProgramNumber, "Target");

			programNumberToTransactionData = new Hashtable ();
			programNumberToCostReductionData = new Hashtable ();
			programNumberToEmployeeData = new Hashtable ();
			programNumberToPredictedBudgetData = new Hashtable();

			foreach (int i in programNumbers)
			{
				programNumberToTransactionData.Add(i, new Hashtable ());
				//programNumberToCostAvoidanceData.Add(i, new Hashtable ());
				programNumberToCostReductionData.Add(i, new Hashtable ());
				programNumberToEmployeeData.Add(i, new Hashtable ());
				programNumberToPredictedBudgetData.Add(i, new Hashtable());
			}

			// Initialise their graph styles.
			Hashtable programNumberToColour = new Hashtable ();
			Hashtable programNumberToDashed = new Hashtable ();

			Color [] colours = new Color [] { Color.FromArgb(255, 0, 0), Color.FromArgb(0, 255, 0), Color.FromArgb(0, 0, 255),
			                                  Color.FromArgb(255, 255, 0), Color.FromArgb(0, 255, 255), Color.FromArgb(255, 0, 255),
			                                  Color.FromArgb(155, 0, 255), Color.FromArgb(0, 155, 255), Color.FromArgb(255, 0, 155),
			                                  Color.FromArgb(255, 155, 0), Color.FromArgb(0, 255, 155), Color.FromArgb(155, 255, 0),
			                                  Color.FromArgb(155, 0, 155), Color.FromArgb(155, 155, 0) };
			int index = 0;
			foreach (int i in programNumbers)
			{
				programNumberToColour.Add(i, colours[index]);
				index = (index + 1) % colours.Length;
				programNumberToDashed.Add(i, false);
			}

			programNumberToColour[totalProgramNumber] = Color.FromArgb(0, 155, 155);
			programNumberToDashed[totalProgramNumber] = false;
			programNumberToColour[targetProgramNumber] = Color.FromArgb(0, 0, 0);
			programNumberToDashed[targetProgramNumber] = true;

			// Extract the round events.
			lastKnownTime = 0;

			string logFile = gameFile.GetRoundFile(round, "NetworkIncidents.log", GameManagement.GameFile.GamePhase.OPERATIONS);
			BasicIncidentLogReader biLogReader = new BasicIncidentLogReader(logFile);

			//Extract out the data chnages from the log file 
			biLogReader.WatchApplyAttributes("Portfolio1", new Logging.LogLineFoundDef.LineFoundHandler(biLogReader_DeptStaffApply));
			biLogReader.WatchApplyAttributes("pmo_budget", new Logging.LogLineFoundDef.LineFoundHandler(biLogReader_DeptBudgetApply));
			biLogReader.WatchApplyAttributes("BusinessProjectedPerformance", new Logging.LogLineFoundDef.LineFoundHandler(biLogReader_BusinessProjectedPerformanceApply));

			//Mind to watch for the individual program nodes chanaging 
			foreach (int i in programNumbers)
			{
				biLogReader.WatchApplyAttributes("Program" + CONVERT.ToStr(i), new Logging.LogLineFoundDef.LineFoundHandler(biLogReader_PortfolioApply));
			}
			biLogReader.Run();

			// Finish off the graphs by adding end-of-round data points.
			foreach (int i in programNumbers)
			{
				AddFinalElement(programNumberToTransactionData[i] as Hashtable);
				AddFinalElement(programNumberToCostReductionData[i] as Hashtable);
				AddFinalElement(programNumberToEmployeeData[i] as Hashtable);
				AddFinalElement(programNumberToPredictedBudgetData[i] as Hashtable);
			}

			// Build the graph data.
			LibCore.BasicXmlDocument xdoc = LibCore.BasicXmlDocument.Create();
			XmlElement root = (XmlElement) xdoc.CreateElement("linegraph");
			// Show the key if there's more than one portfolio to show.
			//root.SetAttribute("show_key", CONVERT.ToStr(programNumbers.Count > 1));
			root.SetAttribute("show_key", "false");
			xdoc.AppendChild(root);

			XmlElement xAxis = (XmlElement) xdoc.CreateElement("xAxis");
			xAxis.SetAttribute("minMaxSteps", "0,25,5");
			xAxis.SetAttribute("autoScale", "false");
			xAxis.SetAttribute("title", "Day");
			root.AppendChild(xAxis);

			XmlElement yAxis = (XmlElement) xdoc.CreateElement("yLeftAxis");
			yAxis.SetAttribute("autoScale", "false");

			int yScale = 1;

			switch (metric)
			{
				case Metric.Transactions:
					yAxis.SetAttribute("minMaxSteps", "0,3000,1000");
					yAxis.SetAttribute("title", "Transactions (K)");
					yScale = 1000;
					report_name = "PortfolioReport_transactions.xml";
					break;

				case Metric.CostReduction:
					yAxis.SetAttribute("minMaxSteps", "0,10,1");
					yAxis.SetAttribute("title", "Cost Reduction ($M)");
					yScale = 1000000;
					report_name = "PortfolioReport_costreduction.xml";
					break;

				case Metric.PeopleEmployed:
					yAxis.SetAttribute("minMaxSteps", "0,500,100");
					yAxis.SetAttribute("title", "People Assigned");
					yScale = 1;
					report_name = "PortfolioReport_staffemployed.xml";
					break;

				case Metric.BudgetEmployed:
					yAxis.SetAttribute("minMaxSteps", "8000,15000,2000");
					yAxis.SetAttribute("title", "Budget Allocated ($K)");
					yScale = 1000;
					report_name = "PortfolioReport_budgetemployed.xml";
					break;

			}

			yAxis.SetAttribute("align", "centre_on_tick");
			yAxis.SetAttribute("omit_top", "true");
			yAxis.SetAttribute("width", "60");
			root.AppendChild(yAxis);

			XmlElement yRightAxis = (XmlElement) xdoc.CreateElement("yRightAxis");
			yRightAxis.SetAttribute("visible", "false");
			root.AppendChild(yRightAxis);

			// Build a list of programs worth showing.
			ArrayList programNumbersToShow = new ArrayList ();
			int realProgramsShown = 0;
			foreach (int program in programNumbers)
			{
				// Apart from the total, any program (including the target) is worth showing
				// iff it has any nonzero values.
				if (program != totalProgramNumber)
				{
					Hashtable dataPoints = null;

					switch (metric)
					{
						case Metric.Transactions:
							dataPoints = programNumberToTransactionData[program] as Hashtable;
							break;

						case Metric.CostReduction:
							dataPoints = programNumberToCostReductionData[program] as Hashtable;
							break;

						case Metric.PeopleEmployed:
							dataPoints = this.programNumberToEmployeeData[program] as Hashtable;
							break;

						case Metric.BudgetEmployed:
							dataPoints = this.programNumberToPredictedBudgetData[program] as Hashtable;
							break;
					}

					int maxValue = 0;
					foreach (int val in dataPoints.Values)
					{
						maxValue = Math.Max(maxValue, val);
					}

					if (maxValue > 0)
					{
						programNumbersToShow.Add(program);

						if (program != targetProgramNumber)
						{
							realProgramsShown++;
						}
					}
				}
			}

			// And only bother showing a total if there's more than one.
			//if (realProgramsShown > 1)
			//{
			//	programNumbersToShow.Add(totalProgramNumber);
			//}
				
			//WE want to show the total and the target graphs
			programNumbersToShow.Add(totalProgramNumber);
			programNumbersToShow.Add(targetProgramNumber);

			// Add the data points.
			foreach (int program in programNumbersToShow)
			{
				Hashtable dataPoints = null;

				//We don't want to see the individual traces for the different programs
				//so only allow the negative index which correspond to Total and Target
				if (program < 0)
				{
					switch (metric)
					{
						case Metric.Transactions:
							dataPoints = programNumberToTransactionData[program] as Hashtable;
							BuildDataStepped(program, xdoc, dataPoints, root, yScale, 
								programNumberToName, programNumberToColour, programNumberToDashed);
							break;

						case Metric.CostReduction:
							dataPoints = programNumberToCostReductionData[program] as Hashtable;
							BuildDataStepped(program, xdoc, dataPoints, root, yScale,  
								programNumberToName, programNumberToColour, programNumberToDashed);
							break;

						case Metric.PeopleEmployed:
							dataPoints = this.programNumberToEmployeeData[program] as Hashtable;
							BuildDataStepped(program, xdoc, dataPoints, root, yScale, 
								programNumberToName, programNumberToColour, programNumberToDashed);
							break;

						case Metric.BudgetEmployed:
							dataPoints = this.programNumberToPredictedBudgetData[program] as Hashtable;
							BuildDataStepped(program, xdoc, dataPoints, root, yScale, 
								programNumberToName, programNumberToColour, programNumberToDashed);
							break;
					}

				}
			}

			string reportFile = gameFile.GetRoundFile(round, report_name, GameFile.GamePhase.OPERATIONS);
			xdoc.SaveToURL("", reportFile);

			return reportFile;
		}

		/// <summary>
		/// This builds the xml data elements for each point as a stepped graph
		/// </summary>
		/// <param name="program"></param>
		/// <param name="xdoc"></param>
		/// <param name="dataPoints"></param>
		/// <param name="root"></param>
		/// <param name="yScale"></param>
		/// <param name="programNumberToName"></param>
		/// <param name="programNumberToColour"></param>
		/// <param name="programNumberToDashed"></param>
		private void BuildDataStepped(int program, BasicXmlDocument xdoc, Hashtable dataPoints,
			XmlElement root, int yScale, 
			Hashtable programNumberToName, Hashtable programNumberToColour,	Hashtable programNumberToDashed)
		{
			XmlElement data = (XmlElement)xdoc.CreateElement("data");
			data.SetAttribute("axis", "left");
			data.SetAttribute("thickness", "2");
			data.SetAttribute("title", (string)programNumberToName[program]);
			data.SetAttribute("colour", CONVERT.ToComponentStr((Color)programNumberToColour[program]));
			data.SetAttribute("dashed", CONVERT.ToStr((bool)programNumberToDashed[program]));
			root.AppendChild(data);

			//origin point 
			XmlElement pointZ = xdoc.CreateElement("p");
			data.AppendChild(pointZ);
			pointZ.SetAttribute("x", CONVERT.ToStr(0));
			pointZ.SetAttribute("y", CONVERT.ToStr(0));
			pointZ.SetAttribute("dot", "no");
			int prev_y = 0;

			bool firstpoint = true;
			ArrayList dataPointKeysSortedByTime = new ArrayList(dataPoints.Keys);
			dataPointKeysSortedByTime.Sort();
			foreach (double time in dataPointKeysSortedByTime)
			{
				if (firstpoint == false)
				{
					XmlElement point1 = xdoc.CreateElement("p");
					data.AppendChild(point1);
					point1.SetAttribute("x", CONVERT.ToStr((int)(time / 60.0)));
					point1.SetAttribute("y", CONVERT.ToStr(prev_y));
					point1.SetAttribute("dot", "no");
				}
				firstpoint = false;

				int y = ((int)dataPoints[time]) / yScale;

				XmlElement point = xdoc.CreateElement("p");
				data.AppendChild(point);
				point.SetAttribute("x", CONVERT.ToStr((int)(time / 60.0)));
				point.SetAttribute("y", CONVERT.ToStr(y));
				point.SetAttribute("dot", "no");

				prev_y = y;
			}
		}

		/// <summary>
		/// This builds the xml data elements for each point as a straight line graph
		/// </summary>
		/// <param name="program"></param>
		/// <param name="xdoc"></param>
		/// <param name="dataPoints"></param>
		/// <param name="root"></param>
		/// <param name="yScale"></param>
		/// <param name="programNumberToName"></param>
		/// <param name="programNumberToColour"></param>
		/// <param name="programNumberToDashed"></param>
		private void BuildData(int program, BasicXmlDocument xdoc, Hashtable dataPoints,
			XmlElement root, int yScale,
			Hashtable programNumberToName, Hashtable programNumberToColour, Hashtable programNumberToDashed)
		{
			XmlElement data = (XmlElement)xdoc.CreateElement("data");
			data.SetAttribute("axis", "left");
			data.SetAttribute("thickness", "2");
			data.SetAttribute("title", (string)programNumberToName[program]);
			data.SetAttribute("colour", CONVERT.ToComponentStr((Color)programNumberToColour[program]));
			data.SetAttribute("dashed", CONVERT.ToStr((bool)programNumberToDashed[program]));
			root.AppendChild(data);

			ArrayList dataPointKeysSortedByTime = new ArrayList(dataPoints.Keys);
			dataPointKeysSortedByTime.Sort();
			foreach (double time in dataPointKeysSortedByTime)
			{
				int y = ((int)dataPoints[time]) / yScale;

				XmlElement point = xdoc.CreateElement("p");
				data.AppendChild(point);
				point.SetAttribute("x", CONVERT.ToStr((int)(time / 60.0)));
				point.SetAttribute("y", CONVERT.ToStr(y));
				point.SetAttribute("dot", "no");
			}
		}

		/// <summary>
		/// Extracting information for the Setting of Node "BusinessProjectedPerformance"
		/// This is the node where we build the main metrics for the Graph
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="key"></param>
		/// <param name="line"></param>
		/// <param name="time"></param>
		protected void biLogReader_BusinessProjectedPerformanceApply(object sender, string key, string line, double time)
		{
			lastKnownTime = (int)Math.Max(lastKnownTime, time);

			string name = BasicIncidentLogReader.ExtractValue(line, "i_name");

			string transactionsPlanned = BasicIncidentLogReader.ExtractValue(line, "transactionBenefitPlanned");
			string costReductionPlanned = BasicIncidentLogReader.ExtractValue(line, "costReductionBenefitPlanned");
			string transactionsTarget = BasicIncidentLogReader.ExtractValue(line, "transactionBenefitTarget");
			string costReductionTarget = BasicIncidentLogReader.ExtractValue(line, "costReductionBenefitTarget");

			string budgetTarget = BasicIncidentLogReader.ExtractValue(line, "pmo_budgetTarget");
			string peopleTarget = BasicIncidentLogReader.ExtractValue(line, "peopleTarget");

			if (transactionsPlanned != "")
			{
				Hashtable data = programNumberToTransactionData[totalProgramNumber] as Hashtable;
				data.Add(time, CONVERT.ParseIntSafe(transactionsPlanned, 0));
			}
			if (costReductionPlanned != "")
			{
				Hashtable data = programNumberToCostReductionData[totalProgramNumber] as Hashtable;
				data.Add(time, CONVERT.ParseIntSafe(costReductionPlanned, 0));
			}

			if (transactionsTarget != "")
			{
				Hashtable data = programNumberToTransactionData[targetProgramNumber] as Hashtable;
				data.Add(time, CONVERT.ParseIntSafe(transactionsTarget, 0));
			}
			if (costReductionTarget != "")
			{
				Hashtable data = programNumberToCostReductionData[targetProgramNumber] as Hashtable;
				data.Add(time, CONVERT.ParseIntSafe(costReductionTarget, 0));
			}

			if (budgetTarget != "")
			{
				Hashtable data = this.programNumberToPredictedBudgetData[targetProgramNumber] as Hashtable;
				data.Add(time, CONVERT.ParseIntSafe(budgetTarget, 0));
			}
			if (peopleTarget != "")
			{
				Hashtable data = this.programNumberToEmployeeData[targetProgramNumber] as Hashtable;
				data.Add(time, CONVERT.ParseIntSafe(peopleTarget, 0));
			}
		}

		protected void biLogReader_PortfolioApply (object sender, string key, string line, double time)
		{
			lastKnownTime = (int) Math.Max(lastKnownTime, time);

			string name = BasicIncidentLogReader.ExtractValue(line, "i_name");
			int program = CONVERT.ParseIntSafe(name.Substring("Program".Length), 0);

			string transactions = BasicIncidentLogReader.ExtractValue(line, "transaction_benefit_compelte");
			//string costAvoidance = BasicIncidentLogReader.ExtractValue(line, "cost_avoidance_benefit");
			string costReduction = BasicIncidentLogReader.ExtractValue(line, "cost_reduction_benefit");
			string employeeUsed = BasicIncidentLogReader.ExtractValue(line, "resources");
			string predictBudget = BasicIncidentLogReader.ExtractValue(line, "predicted_cost");

			//if (transactions != "")
			//{
			//  Hashtable data = programNumberToTransactionData[program] as Hashtable;
			//  data[time] = CONVERT.ParseIntSafe(transactions, 0);
			//}
			//if (costAvoidance != "")
			//{
			//  Hashtable data = programNumberToCostAvoidanceData[program] as Hashtable;
			//  data[time] = CONVERT.ParseIntSafe(costAvoidance, 0);
			//}
			//if (costReduction != "")
			//{
			//  Hashtable data = programNumberToCostReductionData[program] as Hashtable;
			//  data[time] = CONVERT.ParseIntSafe(costReduction, 0);
			//}

			if (employeeUsed != "")
			{
				Hashtable data = this.programNumberToEmployeeData[program] as Hashtable;
				data[time] = CONVERT.ParseIntSafe(employeeUsed, 0);
			}
			if (predictBudget != "")
			{
				Hashtable data = this.programNumberToPredictedBudgetData[program] as Hashtable;
				data[time] = CONVERT.ParseIntSafe(predictBudget, 0);
			}
		}


		protected void biLogReader_DeptStaffApply(object sender, string key, string line, double time)
		{
			lastKnownTime = (int)Math.Max(lastKnownTime, time);

			string name = BasicIncidentLogReader.ExtractValue(line, "i_name");
			string employeeUsed = BasicIncidentLogReader.ExtractValue(line, "resources");
			string employeeTarget = BasicIncidentLogReader.ExtractValue(line, "resources_target");

			if (employeeUsed != "")
			{
				Hashtable data1 = this.programNumberToEmployeeData[totalProgramNumber] as Hashtable;
				data1[time] = CONVERT.ParseIntSafe(employeeUsed, 0);
			}
			//if (employeeTarget != "")
			//{
			//  Hashtable data2 = this.programNumberToEmployeeData[targetProgramNumber] as Hashtable;
			//  data2[time] = CONVERT.ParseIntSafe(employeeTarget, 0);
			//}
		}

		protected void biLogReader_DeptBudgetApply(object sender, string key, string line, double time)
		{
			lastKnownTime = (int)Math.Max(lastKnownTime, time);

			string name = BasicIncidentLogReader.ExtractValue(line, "i_name");
			string budgetUsed = BasicIncidentLogReader.ExtractValue(line, "budget_spent");
			string budgetTarget = BasicIncidentLogReader.ExtractValue(line, "budget_target");

			if (budgetUsed != "")
			{
				Hashtable data1 = this.programNumberToPredictedBudgetData[totalProgramNumber] as Hashtable;
				data1[time] = CONVERT.ParseIntSafe(budgetUsed, 0);
			}
			//if (budgetTarget != "")
			//{
			//  Hashtable data2 = this.programNumberToPredictedBudgetData[targetProgramNumber] as Hashtable;
			//  data2[time] = CONVERT.ParseIntSafe(budgetTarget, 0);
			//}
		}

		/// <summary>
		/// Helper Method for Adding a final point to the graphs so that the line go all the way to the end.
		/// </summary>
		/// <param name="dataPoints"></param>
		void AddFinalElement (Hashtable dataPoints)
		{
			bool hasAtEnd = false;
			double lastTime = 0;
			int lastValue = 0;

			foreach (double time in dataPoints.Keys)
			{
				if (time >= lastKnownTime)
				{
					hasAtEnd = true;
				}

				if ((lastTime <= 0) || (time >= lastTime))
				{
					lastValue = (int) dataPoints[time];
					lastTime = time;
				}
			}

			if (! hasAtEnd)
			{
				dataPoints.Add(lastKnownTime, lastValue);
			}
		}

		protected NodeTree GetModelForRound (NetworkProgressionGameFile gameFile, int round)
		{
			return gameFile.GetNetworkModel(round, GameFile.GamePhase.OPERATIONS);
		}
	}
}