using System.Collections.Generic;

using System.Drawing;
using System.Xml;

using Logging;

using GameManagement;
using Network;
using LibCore;

namespace ReportBuilder
{
	public class PM_ProjectSpendReport
	{
		Dictionary<int, string> projectNameBySlotNumber;
		Dictionary<string, string> projectNameByFinancialNodeName;

		Dictionary<string, Dictionary<double, int>> spendByTimeByProjectName;
		Dictionary<string, Dictionary<double, int>> budgetByTimeByProjectName;
		Dictionary<string, Dictionary<double, int>> projectedCostByTimeByProjectName;

		public PM_ProjectSpendReport ()
		{
		}

		public string BuildReport (NetworkProgressionGameFile gameFile, int round, int projectSlot)
		{
			string filename = gameFile.GetRoundFile(round, "NetworkIncidents.log", GameFile.GamePhase.OPERATIONS);
			NodeTree model = gameFile.GetNetworkModel(round, GameFile.GamePhase.OPERATIONS);

			int maxTime = 25 * 60;

			projectNameBySlotNumber = new Dictionary<int, string> ();
			projectNameByFinancialNodeName = new Dictionary<string,string> ();

			spendByTimeByProjectName = new Dictionary<string, Dictionary<double, int>> ();
			budgetByTimeByProjectName = new Dictionary<string, Dictionary<double, int>> ();
			projectedCostByTimeByProjectName = new Dictionary<string, Dictionary<double, int>> ();

			BasicIncidentLogReader reader = new BasicIncidentLogReader (filename);
			Node projects = model.GetNamedNode("pm_projects_running");
			foreach (Node project in projects.GetChildrenOfType("project"))
			{
				Node financialData = project.GetFirstChildOfType("financial_data");

				projectNameBySlotNumber.Add(project.GetIntAttribute("slot", 0), project.GetAttribute("name"));
				projectNameByFinancialNodeName.Add(financialData.GetAttribute("name"), project.GetAttribute("name"));
				reader.WatchApplyAttributes(financialData.GetAttribute("name"), reader_ProjectAttributesChanged);
			}
			reader.WatchCreatedNodes("pm_projects", reader_CreatedProjectNode);
			reader.Run();

			BasicXmlDocument doc = BasicXmlDocument.Create();
			XmlElement root = doc.CreateElement("linegraph");
			root.SetAttribute("show_key", "true");
			doc.AppendChild(root);

			if (projectNameBySlotNumber.ContainsKey(projectSlot))
			{
				string projectName = projectNameBySlotNumber[projectSlot];

				XmlElement xAxis = (XmlElement) doc.CreateElement("xAxis");
				xAxis.SetAttribute("minMaxSteps", "0,25,5");
				xAxis.SetAttribute("autoScale", "false");
				xAxis.SetAttribute("title", "Day");
				root.AppendChild(xAxis);

				XmlElement yAxis = (XmlElement) doc.CreateElement("yLeftAxis");
				yAxis.SetAttribute("autoScale", "true");
				yAxis.SetAttribute("minMaxSteps", "0,1000,100");
				yAxis.SetAttribute("title", "$K");
				yAxis.SetAttribute("omit_top", "true");
				yAxis.SetAttribute("width", "80");
				root.AppendChild(yAxis);

				XmlElement yRightAxis = (XmlElement) doc.CreateElement("yRightAxis");
				yRightAxis.SetAttribute("visible", "false");
				root.AppendChild(yRightAxis);

				// The actual spend.
				XmlElement spendLine = doc.CreateElement("data");
				spendLine.SetAttribute("axis", "left");
				spendLine.SetAttribute("thickness", "2");
				spendLine.SetAttribute("title", "Spend");
				spendLine.SetAttribute("colour", CONVERT.ToComponentStr(Color.Red));
				spendLine.SetAttribute("dashed", "false");
				root.AppendChild(spendLine);

				Dictionary<double, int> spendByTime = spendByTimeByProjectName[projectName];
				List<double> sortedSpendTimes = new List<double> (spendByTime.Keys);
				int spend = 0;
				foreach (double time in sortedSpendTimes)
				{
					spend = spendByTime[time];
					XmlElement point = doc.CreateElement("p");
					point.SetAttribute("x", CONVERT.ToStr(time / 60));
					point.SetAttribute("y", CONVERT.ToStr(spend));
					point.SetAttribute("dot", "yes");
					spendLine.AppendChild(point);
				}
				XmlElement lastSpendPoint = doc.CreateElement("p");
				lastSpendPoint.SetAttribute("x", CONVERT.ToStr(maxTime / 60));
				lastSpendPoint.SetAttribute("y", CONVERT.ToStr(spend));
				lastSpendPoint.SetAttribute("dot", "yes");
				spendLine.AppendChild(lastSpendPoint);

				// The player-assigned budget.
				XmlElement budgetLine = doc.CreateElement("data");
				budgetLine.SetAttribute("axis", "left");
				budgetLine.SetAttribute("thickness", "2");
				budgetLine.SetAttribute("title", "Budget");
				budgetLine.SetAttribute("colour", CONVERT.ToComponentStr(Color.Blue));
				budgetLine.SetAttribute("dashed", "false");
				root.AppendChild(budgetLine);

				Dictionary<double, int> budgetByTime = budgetByTimeByProjectName[projectName];
				List<double> sortedBudgetTimes = new List<double> (budgetByTime.Keys);
				int budget = 0;
				foreach (double time in sortedBudgetTimes)
				{
					XmlElement oldPoint = doc.CreateElement("p");
					oldPoint.SetAttribute("x", CONVERT.ToStr((time - 1) / 60));
					oldPoint.SetAttribute("y", CONVERT.ToStr(budget));
					oldPoint.SetAttribute("dot", "yes");
					budgetLine.AppendChild(oldPoint);

					budget = budgetByTime[time];

					XmlElement point = doc.CreateElement("p");
					point.SetAttribute("x", CONVERT.ToStr(time / 60));
					point.SetAttribute("y", CONVERT.ToStr(budget));
					point.SetAttribute("dot", "yes");
					budgetLine.AppendChild(point);
				}
				XmlElement lastBudgetPoint = doc.CreateElement("p");
				lastBudgetPoint.SetAttribute("x", CONVERT.ToStr(maxTime / 60));
				lastBudgetPoint.SetAttribute("y", CONVERT.ToStr(budget));
				lastBudgetPoint.SetAttribute("dot", "yes");
				budgetLine.AppendChild(lastBudgetPoint);

				// The projected spend.
				XmlElement projectedCostLine = doc.CreateElement("data");
				projectedCostLine.SetAttribute("axis", "left");
				projectedCostLine.SetAttribute("thickness", "2");
				projectedCostLine.SetAttribute("title", "Expected Spend");
				projectedCostLine.SetAttribute("colour", CONVERT.ToComponentStr(Color.Green));
				projectedCostLine.SetAttribute("dashed", "false");
				root.AppendChild(projectedCostLine);

				Dictionary<double, int> projectedCostByTime = projectedCostByTimeByProjectName[projectName];
				List<double> sortedProjectedCostTimes = new List<double> (projectedCostByTime.Keys);
				int projectedCost = 0;
				foreach (double time in sortedProjectedCostTimes)
				{
					XmlElement oldPoint = doc.CreateElement("p");
					oldPoint.SetAttribute("x", CONVERT.ToStr((time - 1) / 60));
					oldPoint.SetAttribute("y", CONVERT.ToStr(projectedCost));
					oldPoint.SetAttribute("dot", "yes");
					projectedCostLine.AppendChild(oldPoint);

					projectedCost = projectedCostByTime[time];

					XmlElement point = doc.CreateElement("p");
					point.SetAttribute("x", CONVERT.ToStr(time / 60));
					point.SetAttribute("y", CONVERT.ToStr(projectedCost));
					point.SetAttribute("dot", "yes");
					projectedCostLine.AppendChild(point);
				}
				XmlElement lastProjectedPoint = doc.CreateElement("p");
				lastProjectedPoint.SetAttribute("x", CONVERT.ToStr(maxTime / 60));
				lastProjectedPoint.SetAttribute("y", CONVERT.ToStr(projectedCost));
				lastProjectedPoint.SetAttribute("dot", "yes");
				projectedCostLine.AppendChild(lastProjectedPoint);
			}

			string report = gameFile.GetRoundFile(round, "ProjectSpendReport.xml", GameFile.GamePhase.OPERATIONS);
			doc.Save(report);

			return report;
		}

		void reader_CreatedProjectNode (object sender, string key, string line, double time)
		{
		}

		void reader_ProjectAttributesChanged (object sender, string key, string line, double time)
		{
			string financialNodeName = BasicIncidentLogReader.ExtractValue(line, "i_name");
			string projectName = projectNameByFinancialNodeName[financialNodeName];

			string spendString = BasicIncidentLogReader.ExtractValue(line, "spend");

			if (spendString != string.Empty)
			{
				int spend = CONVERT.ParseInt(spendString);

				if (! spendByTimeByProjectName.ContainsKey(projectName))
				{
					spendByTimeByProjectName.Add(projectName, new Dictionary<double,int> ());
				}

				spendByTimeByProjectName[projectName][time] = spend;
			}

			string budgetString = BasicIncidentLogReader.ExtractValue(line, "budget_player");
			if (budgetString != string.Empty)
			{
				int budget = CONVERT.ParseInt(budgetString);

				if (! budgetByTimeByProjectName.ContainsKey(projectName))
				{
					budgetByTimeByProjectName.Add(projectName, new Dictionary<double, int> ());
				}

				budgetByTimeByProjectName[projectName][time] = budget;
			}

			string projectedSpendString = BasicIncidentLogReader.ExtractValue(line, "budget_defined");
			if (projectedSpendString != string.Empty)
			{
				int projectedSpend = CONVERT.ParseInt(projectedSpendString);

				if (! projectedCostByTimeByProjectName.ContainsKey(projectName))
				{
					projectedCostByTimeByProjectName.Add(projectName, new Dictionary<double, int> ());
				}

				projectedCostByTimeByProjectName[projectName][time] = projectedSpend;
			}
		}
	}
}