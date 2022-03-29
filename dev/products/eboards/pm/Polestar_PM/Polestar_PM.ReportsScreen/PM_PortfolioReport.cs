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
	public class PM_PortfolioReport
	{
		static readonly int totalPortfolioNumber = -1;
		static readonly int targetPortfolioNumber = -2;

		public enum Metric
		{
			Transactions,
			CostReduction
			//CostAvoidance,
		}

		double lastKnownTime;
		Hashtable portfolioNumberToTransactionData;
		//Hashtable portfolioNumberToCostAvoidanceData;
		Hashtable portfolioNumberToCostReductionData;

		public PM_PortfolioReport ()
		{
		}

		public string BuildReport (NetworkProgressionGameFile gameFile, Metric metric)
		{
			// Initialise the list of portfolios and their names.
			ArrayList portfolioNumbers = new ArrayList ();
			Hashtable portfolioNumberToName = new Hashtable ();
			for (int i = 1; i <= 3; i++)
			{
				portfolioNumbers.Add(i);
				portfolioNumberToName.Add(i, "Portfolio " + CONVERT.ToStr(i));
			}
			portfolioNumbers.Add(totalPortfolioNumber);
			portfolioNumberToName.Add(totalPortfolioNumber, "Total");

			portfolioNumbers.Add(targetPortfolioNumber);
			portfolioNumberToName.Add(targetPortfolioNumber, "Target");

			portfolioNumberToTransactionData = new Hashtable ();
			//portfolioNumberToCostAvoidanceData = new Hashtable ();
			portfolioNumberToCostReductionData = new Hashtable ();			

			foreach (int i in portfolioNumbers)
			{
				portfolioNumberToTransactionData.Add(i, new Hashtable ());
				//portfolioNumberToCostAvoidanceData.Add(i, new Hashtable ());
				portfolioNumberToCostReductionData.Add(i, new Hashtable ());
			}

			// Initialise their graph styles.
			Hashtable portfolioNumberToColour = new Hashtable ();
			Hashtable portfolioNumberToDashed = new Hashtable ();

			portfolioNumberToColour.Add(1, Color.FromArgb(255, 0, 0));
			portfolioNumberToDashed.Add(1, false);
			portfolioNumberToColour.Add(2, Color.FromArgb(0, 255, 0));
			portfolioNumberToDashed.Add(2, false);
			portfolioNumberToColour.Add(3, Color.FromArgb(0, 0, 255));
			portfolioNumberToDashed.Add(3, false);
			portfolioNumberToColour.Add(totalPortfolioNumber, Color.FromArgb(0, 155, 155));
			portfolioNumberToDashed.Add(totalPortfolioNumber, false);
			portfolioNumberToColour.Add(targetPortfolioNumber, Color.FromArgb(0, 0, 0));
			portfolioNumberToDashed.Add(targetPortfolioNumber, true);

			// Extract the round events.
			int round = 3;

			lastKnownTime = 0;

			string logFile = gameFile.GetRoundFile(round, "NetworkIncidents.log", GameManagement.GameFile.GamePhase.OPERATIONS);
			BasicIncidentLogReader biLogReader = new BasicIncidentLogReader(logFile);
			biLogReader.WatchApplyAttributes("BusinessTargetTransactions", new Logging.LogLineFoundDef.LineFoundHandler(biLogReader_TransactionsTargetApply));
			//biLogReader.WatchApplyAttributes("BusinessTargetCostAvoidance", new Logging.LogLineFoundDef.LineFoundHandler(biLogReader_CostAvoidanceTargetApply));
			biLogReader.WatchApplyAttributes("BusinessTargetCostReduction", new Logging.LogLineFoundDef.LineFoundHandler(biLogReader_CostReductionTargetApply));
			biLogReader.WatchApplyAttributes("BusinessPerformance", new Logging.LogLineFoundDef.LineFoundHandler(biLogReader_BusinessPerformanceApply));
			for (int i = 1; i <= 3; i++)
			{
				biLogReader.WatchApplyAttributes("Portfolio" + CONVERT.ToStr(i), new Logging.LogLineFoundDef.LineFoundHandler(biLogReader_PortfolioApply));
			}
			biLogReader.Run();

			// Finish off the graphs by adding end-of-round data points.
			foreach (int i in portfolioNumbers)
			{
				AddFinalElement(portfolioNumberToTransactionData[i] as Hashtable);
				//AddFinalElement(portfolioNumberToCostAvoidanceData[i] as Hashtable);
				AddFinalElement(portfolioNumberToCostReductionData[i] as Hashtable);
			}

			// Build the graph data.
			LibCore.BasicXmlDocument xdoc = LibCore.BasicXmlDocument.Create();
			XmlElement root = (XmlElement) xdoc.CreateElement("linegraph");
			// Show the key if there's more than one portfolio to show.
			root.SetAttribute("show_key", CONVERT.ToStr(portfolioNumbers.Count > 1));
			xdoc.AppendChild(root);

			XmlElement xAxis = (XmlElement) xdoc.CreateElement("xAxis");
			xAxis.SetAttribute("minMaxSteps", "0,30,5");
			xAxis.SetAttribute("autoScale", "false");
			xAxis.SetAttribute("title", "Day");
			root.AppendChild(xAxis);

			XmlElement yAxis = (XmlElement) xdoc.CreateElement("yLeftAxis");
			yAxis.SetAttribute("autoScale", "false");

			int yScale = 1;

			switch (metric)
			{
				case Metric.Transactions:
					yAxis.SetAttribute("minMaxSteps", "0,3000,500");
					yAxis.SetAttribute("title", "Transactions (K)");
					yScale = 1000;
					break;

				//case Metric.CostAvoidance:
				//  yAxis.SetAttribute("minMaxSteps", "0,100,10");
				//  yAxis.SetAttribute("title", "Cost Avoidance ($M)");
				//  yScale = 1000000;
				//  break;

				case Metric.CostReduction:
					yAxis.SetAttribute("minMaxSteps", "0,100,10");
					yAxis.SetAttribute("title", "Cost Reduction ($M)");
					yScale = 1000000;
					break;
			}

			yAxis.SetAttribute("align", "centre_on_tick");
			yAxis.SetAttribute("omit_top", "true");
			yAxis.SetAttribute("width", "60");
			root.AppendChild(yAxis);

			XmlElement yRightAxis = (XmlElement) xdoc.CreateElement("yRightAxis");
			yRightAxis.SetAttribute("visible", "false");
			root.AppendChild(yRightAxis);

			// Add the data points.
			foreach (int portfolio in portfolioNumbers)
			{
				Hashtable dataPoints = null;

				switch (metric)
				{
					case Metric.Transactions:
						dataPoints = portfolioNumberToTransactionData[portfolio] as Hashtable;
						break;

					//case Metric.CostAvoidance:
					//  dataPoints = portfolioNumberToCostAvoidanceData[portfolio] as Hashtable;
					//  break;

					case Metric.CostReduction:
						dataPoints = portfolioNumberToCostReductionData[portfolio] as Hashtable;
						break;
				}

				XmlElement data = (XmlElement) xdoc.CreateElement("data");
				data.SetAttribute("axis", "left");
				data.SetAttribute("thickness", "2");
				data.SetAttribute("title", (string) portfolioNumberToName[portfolio]);
				data.SetAttribute("colour", CONVERT.ToComponentStr((Color) portfolioNumberToColour[portfolio]));
				data.SetAttribute("dashed", CONVERT.ToStr((bool) portfolioNumberToDashed[portfolio]));
				root.AppendChild(data);

				ArrayList dataPointKeysSortedByTime = new ArrayList (dataPoints.Keys);
				dataPointKeysSortedByTime.Sort();
				foreach (double time in dataPointKeysSortedByTime)
				{
					int y = ((int) dataPoints[time]) / yScale;

					XmlElement point = xdoc.CreateElement("p");
					data.AppendChild(point);
					point.SetAttribute("x", CONVERT.ToStr((int) (time / 60.0)));
					point.SetAttribute("y", CONVERT.ToStr(y));
					point.SetAttribute("dot", "no");
				}
			}

			string reportFile = gameFile.GetRoundFile(round, "PortfolioReport.xml" , GameFile.GamePhase.OPERATIONS);
			xdoc.SaveToURL("", reportFile);

			return reportFile;
		}

		protected void biLogReader_BusinessPerformanceApply (object sender, string key, string line, double time)
		{
			lastKnownTime = (int) Math.Max(lastKnownTime, time);

			string name = BasicIncidentLogReader.ExtractValue(line, "i_name");

			string transactions = BasicIncidentLogReader.ExtractValue(line, "transactions");
			string costAvoidance = BasicIncidentLogReader.ExtractValue(line, "cost_avoidance");
			string costReduction = BasicIncidentLogReader.ExtractValue(line, "cost_reduction");

			if (transactions != "")
			{
				Hashtable data = portfolioNumberToTransactionData[totalPortfolioNumber] as Hashtable;
				data.Add(time, CONVERT.ParseIntSafe(transactions, 0));
			}
			//if (costAvoidance != "")
			//{
			//  Hashtable data = portfolioNumberToCostAvoidanceData[totalPortfolioNumber] as Hashtable;
			//  data.Add(time, CONVERT.ParseIntSafe(costAvoidance, 0));
			//}
			if (costReduction != "")
			{
				Hashtable data = portfolioNumberToCostReductionData[totalPortfolioNumber] as Hashtable;
				data.Add(time, CONVERT.ParseIntSafe(costReduction, 0));
			}
		}

		protected void biLogReader_PortfolioApply (object sender, string key, string line, double time)
		{
			lastKnownTime = (int) Math.Max(lastKnownTime, time);

			string name = BasicIncidentLogReader.ExtractValue(line, "i_name");
			int portfolio = CONVERT.ParseIntSafe(name.Substring(name.Length - 1), 0);

			string transactions = BasicIncidentLogReader.ExtractValue(line, "transaction_benefit");
			string costAvoidance = BasicIncidentLogReader.ExtractValue(line, "cost_avoidance_benefit");
			string costReduction = BasicIncidentLogReader.ExtractValue(line, "cost_reduction_benefit");

			if (transactions != "")
			{
				Hashtable data = portfolioNumberToTransactionData[portfolio] as Hashtable;
				data.Add(time, CONVERT.ParseIntSafe(transactions, 0));
			}
			//if (costAvoidance != "")
			//{
			//  Hashtable data = portfolioNumberToCostAvoidanceData[portfolio] as Hashtable;
			//  data.Add(time, CONVERT.ParseIntSafe(costAvoidance, 0));
			//}
			if (costReduction != "")
			{
				Hashtable data = portfolioNumberToCostReductionData[portfolio] as Hashtable;
				data.Add(time, CONVERT.ParseIntSafe(costReduction, 0));
			}
		}

		protected void biLogReader_TransactionsTargetApply (object sender, string key, string line, double time)
		{
			lastKnownTime = (int) Math.Max(lastKnownTime, time);

			string name = BasicIncidentLogReader.ExtractValue(line, "i_name");
			string attribute = BasicIncidentLogReader.ExtractValue(line, "value");
			int val = CONVERT.ParseIntSafe(attribute, 0);

			Hashtable targets = portfolioNumberToTransactionData[targetPortfolioNumber] as Hashtable;
			targets.Add(time, val);
		}

		//protected void biLogReader_CostAvoidanceTargetApply (object sender, string key, string line, double time)
		//{
		//  lastKnownTime = (int) Math.Max(lastKnownTime, time);

		//  string name = BasicIncidentLogReader.ExtractValue(line, "i_name");
		//  string attribute = BasicIncidentLogReader.ExtractValue(line, "value");
		//  int val = CONVERT.ParseIntSafe(attribute, 0);

		//  Hashtable targets = portfolioNumberToCostAvoidanceData[targetPortfolioNumber] as Hashtable;
		//  targets.Add(time, val);
		//}

		protected void biLogReader_CostReductionTargetApply (object sender, string key, string line, double time)
		{
			lastKnownTime = (int) Math.Max(lastKnownTime, time);

			string name = BasicIncidentLogReader.ExtractValue(line, "i_name");
			string attribute = BasicIncidentLogReader.ExtractValue(line, "value");
			int val = CONVERT.ParseIntSafe(attribute, 0);

			Hashtable targets = portfolioNumberToCostReductionData[targetPortfolioNumber] as Hashtable;
			targets.Add(time, val);
		}

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
	}
}