using System;
using System.Collections.Generic;
using System.Text;
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
	/// This class builds the Benefit Spend Graph (based on the PM_PortfolioReport)
	/// This code needs a severe tidyup to remove unneeded code and concepts 
	/// 
	/// It builds the following hashtables
	///   programNumberToTotalBenefit (a time series of total benefit values)
	///   programNumberToSpendData (7 time series of project spend changes)
	/// We then combine the project values to find the total spend 
	/// So we ened up with 2 time series (when the benefit was achieved step by step) and the cost incurred
	/// 
	/// So we can produce a graph of Benefit against Cost sampled at each day
	/// In all situations, the projects need a fair bit of investment to achive any benefit. 
	/// Normally, each project only delivers benefit when complete
	/// 
	/// Still needs a good tidy up to remove 
	/// 
	/// </summary>
	public class PM_ProjectsBenefitMultiTrace
	{
		static readonly int totalProgramNumber = -1;
		static readonly int targetProgramNumber = -2;

		public enum Metric
		{
			Transactions,
			CostReduction,
			TotalBenefit
		}

		protected double lastKnownTime;
		protected Hashtable programNumberToTransactionData;
		protected Hashtable programNumberToCostReductionData;
		protected Hashtable programNumberToTotalBenefit;
		protected Hashtable programNumberToSpendData;
		protected Hashtable programNumberToCulminlativeSpendData;

		public PM_ProjectsBenefitMultiTrace()
		{
		}

		public string BuildReport(NetworkProgressionGameFile gameFile, Metric metric, int round)
		{
			//int round = gameFile.CurrentRound;
			string report_name = "ProjectsReport_transactions.xml";

			// Build the graph data.
			LibCore.BasicXmlDocument xdoc = LibCore.BasicXmlDocument.Create();
			XmlElement root = (XmlElement) xdoc.CreateElement("linegraph");
			// Show the key if there's more than one portfolio to show.
			//root.SetAttribute("show_key", CONVERT.ToStr(programNumbers.Count > 1));
			root.SetAttribute("show_key", "false");
			xdoc.AppendChild(root);

			NodeTree model = GetModelForRound(gameFile, round);

			if (model != null)
			{
				ArrayList known_work_nodes = new ArrayList();
				Node projectsKnownNode = model.GetNamedNode("pm_projects_known");
				if (projectsKnownNode != null)
				{
					foreach (Node n in projectsKnownNode.getChildren())
					{
						string node_name = n.GetAttribute("value");
						//need to replace 
						string fin_node_name = node_name.Replace("work_data", "financial_data");
						if (fin_node_name != "")
						{
							if (known_work_nodes.Contains(fin_node_name) == false)
							{
								known_work_nodes.Add(fin_node_name);
							}
						}
					}
				}

				//Node current_time_node = model.GetNamedNode("CurrentTime");
				//lastKnownTime = current_time_node.GetIntAttribute("seconds", 0);

				// Initialise the list of portfolios and their names.
				//ArrayList programNumbers = new ArrayList();
				Hashtable programNumberToName = new Hashtable();

				//Node portfoliosNode = model.GetNamedNode("Portfolios");
				//Node portfolioNode = portfoliosNode.GetFirstChildOfType("Portfolio");
				//ArrayList programs = new ArrayList();
				//programs.AddRange(portfolioNode.GetChildrenOfType("Program"));
				//programs.AddRange(portfoliosNode.GetChildrenOfType("Program"));
				//foreach (Node programNode in programs)
				//{
				//  int number = programNode.GetIntAttribute("shortdesc", 0);
				//  programNumbers.Add(number);
				//  programNumberToName.Add(number, programNode.GetAttribute("desc"));
				//}
				//programNumbers.Add(totalProgramNumber);
				programNumberToName.Add(totalProgramNumber, "Total");

				//programNumbers.Add(targetProgramNumber);
				programNumberToName.Add(targetProgramNumber, "Target");

				programNumberToTransactionData = new Hashtable();
				programNumberToCostReductionData = new Hashtable();
				programNumberToTotalBenefit = new Hashtable();
				programNumberToSpendData = new Hashtable();
				//programNumberToEmployeeData = new Hashtable();
				//programNumberToPredictedBudgetData = new Hashtable();
				programNumberToCulminlativeSpendData = new Hashtable();


				foreach (string nn in known_work_nodes)
				{
					programNumberToSpendData.Add(nn, new Hashtable());
				}

				//programNumberToSpendData.Add(1, new Hashtable());
				//programNumberToSpendData.Add(2, new Hashtable());
				//programNumberToSpendData.Add(3, new Hashtable());
				//programNumberToSpendData.Add(4, new Hashtable());
				//programNumberToSpendData.Add(5, new Hashtable());
				//programNumberToSpendData.Add(6, new Hashtable());
				//programNumberToSpendData.Add(7, new Hashtable());

				programNumberToTransactionData.Add(totalProgramNumber, new Hashtable());
				programNumberToTransactionData.Add(targetProgramNumber, new Hashtable());
				programNumberToCostReductionData.Add(totalProgramNumber, new Hashtable());
				programNumberToCostReductionData.Add(targetProgramNumber, new Hashtable());
				programNumberToTotalBenefit.Add(totalProgramNumber, new Hashtable());
				programNumberToTotalBenefit.Add(targetProgramNumber, new Hashtable());
				programNumberToSpendData.Add(totalProgramNumber, new Hashtable());
				programNumberToSpendData.Add(targetProgramNumber, new Hashtable());

				//foreach (int i in programNumbers)
				//{
				//  programNumberToTransactionData.Add(i, new Hashtable());
				//  //programNumberToCostAvoidanceData.Add(i, new Hashtable ());
				//  programNumberToCostReductionData.Add(i, new Hashtable());
				//  programNumberToEmployeeData.Add(i, new Hashtable());
				//  programNumberToPredictedBudgetData.Add(i, new Hashtable());
				//}

				// Initialise their graph styles.
				Hashtable programNumberToColour = new Hashtable();
				Hashtable programNumberToDashed = new Hashtable();

				Color[] colours = new Color[] { Color.FromArgb(255, 0, 0), Color.FromArgb(0, 255, 0), Color.FromArgb(0, 0, 255),
			                                  Color.FromArgb(255, 255, 0), Color.FromArgb(0, 255, 255), Color.FromArgb(255, 0, 255),
			                                  Color.FromArgb(155, 0, 255), Color.FromArgb(0, 155, 255), Color.FromArgb(255, 0, 155),
			                                  Color.FromArgb(255, 155, 0), Color.FromArgb(0, 255, 155), Color.FromArgb(155, 255, 0),
			                                  Color.FromArgb(155, 0, 155), Color.FromArgb(155, 155, 0) };
				//int index = 0;
				//foreach (int i in programNumbers)
				//{
				//  programNumberToColour.Add(i, colours[index]);
				//  index = (index + 1) % colours.Length;
				//  programNumberToDashed.Add(i, false);
				//}

				programNumberToColour[totalProgramNumber] = Color.FromArgb(0, 155, 155);
				programNumberToDashed[totalProgramNumber] = false;
				programNumberToColour[targetProgramNumber] = Color.FromArgb(0, 0, 0);
				programNumberToDashed[targetProgramNumber] = true;

				// Extract the round events.
				lastKnownTime = 0;

				if (model != null)
				{
					Node current_time_node = model.GetNamedNode("CurrentTime");
					lastKnownTime = current_time_node.GetIntAttribute("seconds", 0);
				}

				string logFile = gameFile.GetRoundFile(round, "NetworkIncidents.log", GameManagement.GameFile.GamePhase.OPERATIONS);
				BasicIncidentLogReader biLogReader = new BasicIncidentLogReader(logFile);

//				System.Diagnostics.Debug.WriteLine("=======================================================");

				//Extract out the data chnages from the log file 
				biLogReader.WatchApplyAttributes("running_benefit", new Logging.LogLineFoundDef.LineFoundHandler(biLogReader_RunningBenefitApply));

				//need to look for financial changes 
				//biLogReader.WatchApplyAttributes("project_0_financial_data", new LogLineFoundDef.LineFoundHandler(biLogReader_RunningSpendApply));
				foreach (string nn in known_work_nodes)
				{
					biLogReader.WatchApplyAttributes(nn, new LogLineFoundDef.LineFoundHandler(biLogReader_RunningSpendApply));
				}

				//biLogReader.WatchApplyAttributes("project_1001_financial_data", new LogLineFoundDef.LineFoundHandler(biLogReader_RunningSpendApply));
				//biLogReader.WatchApplyAttributes("project_1002_financial_data", new LogLineFoundDef.LineFoundHandler(biLogReader_RunningSpendApply));
				//biLogReader.WatchApplyAttributes("project_1003_financial_data", new LogLineFoundDef.LineFoundHandler(biLogReader_RunningSpendApply));
				//biLogReader.WatchApplyAttributes("project_1004_financial_data", new LogLineFoundDef.LineFoundHandler(biLogReader_RunningSpendApply));
				//biLogReader.WatchApplyAttributes("project_1005_financial_data", new LogLineFoundDef.LineFoundHandler(biLogReader_RunningSpendApply));
				//biLogReader.WatchApplyAttributes("project_1006_financial_data", new LogLineFoundDef.LineFoundHandler(biLogReader_RunningSpendApply));
				//biLogReader.WatchApplyAttributes("project_1007_financial_data", new LogLineFoundDef.LineFoundHandler(biLogReader_RunningSpendApply));
				//biLogReader.WatchApplyAttributes("project_1008_financial_data", new LogLineFoundDef.LineFoundHandler(biLogReader_RunningSpendApply));
				//biLogReader.WatchApplyAttributes("project_1009_financial_data", new LogLineFoundDef.LineFoundHandler(biLogReader_RunningSpendApply));

				biLogReader.Run();

//				System.Diagnostics.Debug.WriteLine("=======================================================");

				//// Finish off the graphs by adding end-of-round data points.
				//foreach (int i in programNumbers)
				//{
				//  AddFinalElement(programNumberToTransactionData[i] as Hashtable);
				//  AddFinalElement(programNumberToCostReductionData[i] as Hashtable);
				//  AddFinalElement(programNumberToTotalBenefit[i] as Hashtable);

				//  //AddFinalElement(programNumberToEmployeeData[i] as Hashtable);
				//  //AddFinalElement(programNumberToPredictedBudgetData[i] as Hashtable);
				//}

				AddFinalElement(programNumberToTransactionData[totalProgramNumber] as Hashtable);
				AddFinalElement(programNumberToTransactionData[targetProgramNumber] as Hashtable);
				AddFinalElement(programNumberToCostReductionData[totalProgramNumber] as Hashtable);
				AddFinalElement(programNumberToCostReductionData[targetProgramNumber] as Hashtable);
				AddFinalElement(programNumberToTotalBenefit[totalProgramNumber] as Hashtable);
				AddFinalElement(programNumberToTotalBenefit[targetProgramNumber] as Hashtable);
				AddFinalElement(programNumberToSpendData[totalProgramNumber] as Hashtable);
				AddFinalElement(programNumberToSpendData[targetProgramNumber] as Hashtable);

				foreach (string nn in known_work_nodes)
				{
					AddFinalElement(programNumberToSpendData[nn] as Hashtable);
				}

				//AddFinalElement(programNumberToSpendData[1] as Hashtable);
				//AddFinalElement(programNumberToSpendData[2] as Hashtable);
				//AddFinalElement(programNumberToSpendData[3] as Hashtable);
				//AddFinalElement(programNumberToSpendData[4] as Hashtable);
				//AddFinalElement(programNumberToSpendData[5] as Hashtable);
				//AddFinalElement(programNumberToSpendData[6] as Hashtable);
				//AddFinalElement(programNumberToSpendData[7] as Hashtable);

				//Build the Culminlative Spend 
				Hashtable tmpRawPointspendData = (Hashtable) programNumberToSpendData[totalProgramNumber];

				int c_spend = 0;
				ArrayList dataPointKeysSortedByTime = new ArrayList(tmpRawPointspendData.Keys);
				dataPointKeysSortedByTime.Sort();
				foreach (double time in dataPointKeysSortedByTime)
				{
					int additional = (int) tmpRawPointspendData[time];
					c_spend = c_spend + additional;
					programNumberToCulminlativeSpendData.Add(time, c_spend);
				}


				//Determine Max
				XmlElement xAxis = (XmlElement) xdoc.CreateElement("xAxis");
				xAxis.SetAttribute("minMaxSteps", "0,1750,250");
				xAxis.SetAttribute("autoScale", "false");
				xAxis.SetAttribute("title", "Spend ($K)");
				root.AppendChild(xAxis);

				XmlElement yAxis = (XmlElement) xdoc.CreateElement("yLeftAxis");
				yAxis.SetAttribute("autoScale", "false");
				yAxis.SetAttribute("minMaxSteps", "0,6000,500");
				yAxis.SetAttribute("title", "Total Benefit ($K)");
				//yScale = 1000;
				report_name = "ProjectsBenefitReport_totalbenefit.xml";

				yAxis.SetAttribute("align", "centre_on_tick");
				yAxis.SetAttribute("omit_top", "true");
				yAxis.SetAttribute("width", "60");
				root.AppendChild(yAxis);

				XmlElement yRightAxis = (XmlElement) xdoc.CreateElement("yRightAxis");
				yRightAxis.SetAttribute("visible", "false");
				root.AppendChild(yRightAxis);

				XmlElement data = (XmlElement) xdoc.CreateElement("data");
				data.SetAttribute("axis", "left");
				data.SetAttribute("thickness", "2");
				data.SetAttribute("title", (string) programNumberToName[totalProgramNumber]);
				data.SetAttribute("colour", CONVERT.ToComponentStr((Color) programNumberToColour[totalProgramNumber]));
				data.SetAttribute("dashed", CONVERT.ToStr((bool) programNumberToDashed[totalProgramNumber]));
				root.AppendChild(data);

				for (int step = 0; step < 25; step++)
				{
					int time_point = step * 60 + 1;
					int spend_value = 0;
					int total_benefit_value = 0;

					if (lastKnownTime >= time_point)
					{
						//getDataPoint((Hashtable)programNumberToSpendData[totalProgramNumber], time_point, out spend_value);
						//getDataPoint(programNumberToCulminlativeSpendData, time_point, out spend_value);
						getDataPoint((Hashtable) programNumberToTotalBenefit[totalProgramNumber], time_point, out total_benefit_value);

						int tmpDataValue = 0;
						foreach (string nn in known_work_nodes)
						{
							getDataPoint((Hashtable)programNumberToSpendData[nn], time_point, out tmpDataValue);
							spend_value += tmpDataValue;
						}

						//int spend_value1 = 0;
						//getDataPoint((Hashtable) programNumberToSpendData[1], time_point, out spend_value1);
						//int spend_value2 = 0;
						//getDataPoint((Hashtable) programNumberToSpendData[2], time_point, out spend_value2);
						//int spend_value3 = 0;
						//getDataPoint((Hashtable) programNumberToSpendData[3], time_point, out spend_value3);
						//int spend_value4 = 0;
						//getDataPoint((Hashtable) programNumberToSpendData[4], time_point, out spend_value4);
						//int spend_value5 = 0;
						//getDataPoint((Hashtable) programNumberToSpendData[5], time_point, out spend_value5);
						//int spend_value6 = 0;
						//getDataPoint((Hashtable) programNumberToSpendData[6], time_point, out spend_value6);
						//int spend_value7 = 0;
						//getDataPoint((Hashtable) programNumberToSpendData[7], time_point, out spend_value7);

						//spend_value = spend_value1 + spend_value2 + spend_value3 + spend_value4 + spend_value5 + spend_value6 + spend_value7;

						spend_value = spend_value / 1000;
						total_benefit_value = total_benefit_value / 1000;
						System.Diagnostics.Debug.WriteLine("TP:" + time_point.ToString() + " SV:" + spend_value.ToString() + "BV:" + total_benefit_value.ToString());

						XmlElement point1 = xdoc.CreateElement("p");
						data.AppendChild(point1);
						point1.SetAttribute("x", CONVERT.ToStr(spend_value));
						point1.SetAttribute("y", CONVERT.ToStr(total_benefit_value));
						point1.SetAttribute("dot", "yes");
					}

				}
			}
			string reportFile = gameFile.GetRoundFile(round, report_name, GameFile.GamePhase.OPERATIONS);
			xdoc.SaveToURL("", reportFile);

			return reportFile;
		}

		public void getDataPoint(Hashtable dataPoints, int timePoint, out int value)
		{
			value = 0;
			ArrayList dataPointKeysSortedByTime = new ArrayList(dataPoints.Keys);
			dataPointKeysSortedByTime.Sort();
			foreach (double time in dataPointKeysSortedByTime)
			{
				if (time <=timePoint)
				{
					value = (int)dataPoints[time];
				}
			}
		}

		/// <summary>
		/// This builds the xml data elements for each point as a stepped graph
		/// The data level is horizontal until the next point and then it steps up or down
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
			Hashtable programNumberToName, Hashtable programNumberToColour, Hashtable programNumberToDashed)
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
		///   We draw a straight line between data points
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
		/// This is called each time, we find a benefit change in the log file 
		/// So Record the new benefit details
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="key"></param>
		/// <param name="line"></param>
		/// <param name="time"></param>
		protected void biLogReader_RunningBenefitApply(object sender, string key, string line, double time)
		{
			lastKnownTime = (int)Math.Max(lastKnownTime, time);

			string name = BasicIncidentLogReader.ExtractValue(line, "i_name");

			//string transactions_gained = BasicIncidentLogReader.ExtractValue(line, "transactions_gained");
			//string cost_reduction = BasicIncidentLogReader.ExtractValue(line, "cost_reduction");
			string total_benefit = BasicIncidentLogReader.ExtractValue(line, "total_benefit");



			//if (transactions_gained != "")
			//{
			//  Hashtable data = programNumberToTransactionData[totalProgramNumber] as Hashtable;

			//  if (data.ContainsKey(time))
			//  {
			//    int prev = (int)data[time];
			//    int new_tr_amount = CONVERT.ParseIntSafe(transactions_gained, 0);
			//    int total = prev + new_tr_amount;
			//    data[time] = (total);
			//    System.Diagnostics.Debug.WriteLine("TR name:" + name + " SP " + time.ToString() + " (" + transactions_gained + ") " + total.ToString());
			//  }
			//  else
			//  {
			//    int new_tr_amount = CONVERT.ParseIntSafe(transactions_gained, 0);
			//    data.Add(time, new_tr_amount);
			//    System.Diagnostics.Debug.WriteLine("TR  name:" + name + " SP " + time.ToString() + "  " + transactions_gained);
			//  }
			//  //data.Add(time, CONVERT.ParseIntSafe(transactions_gained, 0));
			//  //System.Diagnostics.Debug.WriteLine("TR " + time.ToString() + "  " + transactions_gained);
			//}
			//if (cost_reduction != "")
			//{
			//  Hashtable data = programNumberToCostReductionData[totalProgramNumber] as Hashtable;

			//  if (data.ContainsKey(time))
			//  {
			//    int prev = (int)data[time];
			//    int new_costreduct_amount = CONVERT.ParseIntSafe(cost_reduction, 0);
			//    int total = prev + new_costreduct_amount;
			//    data[time] = (total);
			//    System.Diagnostics.Debug.WriteLine("TB name:" + name + " SP " + time.ToString() + " (" + cost_reduction + ") " + total.ToString());
			//  }
			//  else
			//  {
			//    int new_costreduct_amount = CONVERT.ParseIntSafe(cost_reduction, 0);
			//    data.Add(time, new_costreduct_amount);
			//    System.Diagnostics.Debug.WriteLine("TB  name:" + name + " SP " + time.ToString() + "  " + cost_reduction);
			//  }
			//  //data.Add(time, CONVERT.ParseIntSafe(cost_reduction, 0));
			//  //System.Diagnostics.Debug.WriteLine("CR " + time.ToString() + "  " + cost_reduction);
			//}

			if (total_benefit != "")
			{
				System.Diagnostics.Debug.WriteLine("Raw " + total_benefit);

				Hashtable data = programNumberToTotalBenefit[totalProgramNumber] as Hashtable;

				if (data.ContainsKey(time))
				{
					int prev = (int)data[time];
					int new_benefit_amount = CONVERT.ParseIntSafe(total_benefit, 0);
					//int total = prev +new_benefit_amount;
					int total = Math.Max(prev, new_benefit_amount);
					data[time] = (total);
					System.Diagnostics.Debug.WriteLine("TB AT name:" + name + " SP " + time.ToString() + " (" + prev.ToString() + ")+(" + new_benefit_amount.ToString()+ ") =" + total.ToString());
				}
				else
				{
					int new_benefit_amount = CONVERT.ParseIntSafe(total_benefit, 0);
					data.Add(time, new_benefit_amount);
					System.Diagnostics.Debug.WriteLine("TB NT name:" + name + " SP " + time.ToString() + "  " + total_benefit);
				}
				//data.Add(time, CONVERT.ParseIntSafe(total_benefit, 0));
				//System.Diagnostics.Debug.WriteLine("TB " + time.ToString() + "  " + total_benefit);
			}
		}

		/// <summary>
		/// This is called each time, we find a spend changes in the log file 
		/// So Record the new benefit details for this project
		/// We will total the projects together later.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="key"></param>
		/// <param name="line"></param>
		/// <param name="time"></param>
		protected void biLogReader_RunningSpendApply(object sender, string key, string line, double time)
		{
			lastKnownTime = (int)Math.Max(lastKnownTime, time);

			string name = BasicIncidentLogReader.ExtractValue(line, "i_name");

			//int which_project = -1;
			//if (name.IndexOf("1000") > -1)
			//{
			//  which_project = 1;
			//}
			//if (name.IndexOf("1001") > -1)
			//{
			//  which_project = 2;
			//}
			//if (name.IndexOf("1002") > -1)
			//{
			//  which_project = 3;
			//}
			//if (name.IndexOf("1003") > -1)
			//{
			//  which_project = 4;
			//}
			//if (name.IndexOf("1004") > -1)
			//{
			//  which_project = 5;
			//}
			//if (name.IndexOf("1005") > -1)
			//{
			//  which_project = 6;
			//}
			//if (name.IndexOf("1006") > -1)
			//{
			//  which_project = 7;
			//}


			string spend = BasicIncidentLogReader.ExtractValue(line, "spend");

			if (spend != "")
			{
				//if (which_project > -1)
				if (programNumberToSpendData.ContainsKey(name))
				{
					//Hashtable tmpdata = programNumberToSpendData[which_project] as Hashtable;
					Hashtable tmpdata = programNumberToSpendData[name] as Hashtable;

					int spend_amount = CONVERT.ParseIntSafe(spend, 0);
					//Just in case, we get multiple items declared at the same time
					//This is the spend (which goes up) so take the higher of the values
					if (tmpdata.ContainsKey(time))
					{
						int prev = (int)tmpdata[time];
						if (prev > spend_amount)
						{
							tmpdata[time] = (prev);
						}
						else
						{
							tmpdata[time] = (spend_amount);
						}
						//int prev = (int)tmpdata[time];
						//int total = prev + spend_amount;
						//tmpdata[time] = (total);
					}
					else
					{
						tmpdata.Add(time, spend_amount);
					}
					//System.Diagnostics.Debug.WriteLine("ST name:" + name + " SP " + time.ToString() + "  " + spend_amount);
				}

				Hashtable data = programNumberToSpendData[totalProgramNumber] as Hashtable;
				int new_spend_amount = CONVERT.ParseIntSafe(spend, 0);
				if (data.ContainsKey(time))
				{
					int prev = (int)data[time];
					int total = prev + new_spend_amount;
					data[time] = (total);
					//System.Diagnostics.Debug.WriteLine("MT name:"+name+" SP " + time.ToString() + " ("+spend+") " + total.ToString());
				}
				else 
				{
					data.Add(time, new_spend_amount);
					//System.Diagnostics.Debug.WriteLine("MT name:" + name + " SP " + time.ToString() + "  " + spend);
				}
			}
		}


		/// <summary>
		/// Helper Method for Adding a final point to the graphs so that the line go all the way to the end.
		/// </summary>
		/// <param name="dataPoints"></param>
		void AddFinalElement(Hashtable dataPoints)
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
					lastValue = (int)dataPoints[time];
					lastTime = time;
				}
			}

			if (!hasAtEnd)
			{
				dataPoints.Add(lastKnownTime, lastValue);
			}
		}

		protected NodeTree GetModelForRound(NetworkProgressionGameFile gameFile, int round)
		{
			return gameFile.GetNetworkModel(round, GameFile.GamePhase.OPERATIONS);
		}
	}
}

