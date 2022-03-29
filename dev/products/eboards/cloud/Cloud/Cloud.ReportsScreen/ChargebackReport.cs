using System;
using System.Collections.Generic;
using System.Xml;
using LibCore;
using GameManagement;
using Network;

namespace Cloud.ReportsScreen
{
	public class ChargebackReport
	{
		NetworkProgressionGameFile gameFile;
		int round;
		Cloud_RoundScores [] scores;

		string border_colour_str = "255,255,255";
		string row1_backcolor_str = "109,104,117";
		string row2_backcolor_str = "91,85,97";
		string header_backcolor_str = "64,64,64";
		string header_textcolor_str = "255,255,255";
		string row_textcolor_str = "255,255,255";
		string title_align_str = "left";
		string cpu_align_str = "right";

		bool showRegioninSpacerRow = false;

		public ChargebackReport (NetworkProgressionGameFile gameFile, int round, Cloud_RoundScores [] scores)
		{
			this.gameFile = gameFile;
			this.round = round;
			this.scores = scores;
		}

		public string BuildReport (bool showNewServices)
		{
			int closestRoundAvailable = Math.Min(round, scores.Length);
			Cloud_RoundScores roundScores = scores[closestRoundAvailable - 1];

			NodeTree model = gameFile.GetNetworkModel(round);

			BasicXmlDocument xml = BasicXmlDocument.Create();

			XmlElement rootTable = xml.AppendNewChild("table");
			BasicXmlDocument.AppendAttribute(rootTable, "columns", 1);
			BasicXmlDocument.AppendAttribute(rootTable, "rowheight", 17);
			BasicXmlDocument.AppendAttribute(rootTable, "border_colour", border_colour_str);
			BasicXmlDocument.AppendAttribute(rootTable, "row_colour_1", row1_backcolor_str);
			BasicXmlDocument.AppendAttribute(rootTable, "row_colour_2", row2_backcolor_str);

			List<Node> businesses = new List<Node> ((Node []) model.GetNodesWithAttributeValue("type", "business").ToArray(typeof (Node)));
			businesses.Sort(delegate (Node a, Node b)
							{
								return a.GetIntAttribute("order", 0).CompareTo(b.GetIntAttribute("order", 0));
							});

			XmlElement subTable1 = xml.AppendNewChild(rootTable, "table");
			BasicXmlDocument.AppendAttribute(subTable1, "columns", 4);
			BasicXmlDocument.AppendAttribute(subTable1, "widths", "0.40,0.39,0.02,0.19");
			BasicXmlDocument.AppendAttribute(subTable1, "rowheight", 17);
			BasicXmlDocument.AppendAttribute(subTable1, "border_colour", border_colour_str);
			BasicXmlDocument.AppendAttribute(subTable1, "row_colour_1", row1_backcolor_str);
			BasicXmlDocument.AppendAttribute(subTable1, "row_colour_2", row2_backcolor_str);

			XmlElement mainSubHeaderRow1 = xml.AppendNewChild(subTable1, "rowdata");
			//foreach (string header in new string[] { "", "Accounting", "", "CPU Usage" })
			foreach (string header in new string[] { "", "", "", "CPU Usage" })
			{
				XmlElement headerCell = xml.AppendNewChild(mainSubHeaderRow1, "cell");
				//headerCell.SetAttribute("val", header);
				BasicXmlDocument.AppendAttribute(headerCell, "val", header);
				BasicXmlDocument.AppendAttribute(headerCell, "textcolour", header_textcolor_str);
				BasicXmlDocument.AppendAttribute(headerCell, "no_border", "true");
				BasicXmlDocument.AppendAttribute(headerCell, "textstyle", "bold");
			}

			XmlElement subTable2 = xml.AppendNewChild(rootTable, "table");
			BasicXmlDocument.AppendAttribute(subTable2, "columns", 10);
			BasicXmlDocument.AppendAttribute(subTable2, "widths", "0.31,0.07,0.11,0.12,0.15,0.02,0.06,0.06,0.06,0.06");
			BasicXmlDocument.AppendAttribute(subTable2, "rowheight", 17);
			BasicXmlDocument.AppendAttribute(subTable2, "border_colour", border_colour_str);
			BasicXmlDocument.AppendAttribute(subTable2, "row_colour_1", row1_backcolor_str);
			BasicXmlDocument.AppendAttribute(subTable2, "row_colour_2", row2_backcolor_str);

			XmlElement mainSubHeaderRow2 = xml.AppendNewChild(subTable2, "rowdata");
			foreach (string header in new string[] { "Service Name", "Channel", "Total CPU", "Cost Per CPU", "Total Internal Cost", "", "TP1", "TP2", "TP3", "TP4" })
			{
				XmlElement headerCell = xml.AppendNewChild(mainSubHeaderRow2, "cell");
				BasicXmlDocument.AppendAttribute(headerCell, "val", header);
				BasicXmlDocument.AppendAttribute(headerCell, "textcolour", header_textcolor_str);
				BasicXmlDocument.AppendAttribute(headerCell, "no_border", "true");
				BasicXmlDocument.AppendAttribute(headerCell, "textstyle", "bold");

				if (header.IndexOf("Service Name") > -1)
				{
					BasicXmlDocument.AppendAttribute(headerCell, "align", "left");
				}
				if (header.IndexOf("TP")>-1)
				{
					BasicXmlDocument.AppendAttribute(headerCell, "align", "right");
				}
			}

			foreach (Node business in businesses)
			{
				string regionName = business.GetAttribute("region");

				XmlElement regionHeaderRow = xml.AppendNewChild(rootTable, "rowdata");
				XmlElement regionHeaderCell = xml.AppendNewChild(regionHeaderRow, "cell");
				regionHeaderCell.SetAttribute("val", regionName);

				if (showRegioninSpacerRow)
				{
					regionHeaderCell.SetAttribute("val", regionName);
					BasicXmlDocument.AppendAttribute(regionHeaderCell, "align", title_align_str);
					BasicXmlDocument.AppendAttribute(regionHeaderCell, "colour", header_backcolor_str);
					BasicXmlDocument.AppendAttribute(regionHeaderCell, "textcolour", header_textcolor_str);
					BasicXmlDocument.AppendAttribute(regionHeaderCell, "no_border", "true");
				}
				else
				{
					regionHeaderCell.SetAttribute("val", "");
					BasicXmlDocument.AppendAttribute(regionHeaderCell, "colour", header_backcolor_str);
					BasicXmlDocument.AppendAttribute(regionHeaderCell, "textcolour", header_textcolor_str);
					BasicXmlDocument.AppendAttribute(regionHeaderCell, "no_border", "true");
				}
				BasicXmlDocument.AppendAttribute(regionHeaderCell, "textcolour", header_textcolor_str);
				//BasicXmlDocument.AppendAttribute(regionHeaderCell, "no_border", "true");

				XmlElement mainTable = xml.AppendNewChild(rootTable, "table");
				BasicXmlDocument.AppendAttribute(mainTable, "columns", 10);
				//BasicXmlDocument.AppendAttribute(mainTable, "widths", "0.21,0.13,0.15,0.15,0.02,0.11,0.12,0.11");
				BasicXmlDocument.AppendAttribute(mainTable, "widths", "0.31,0.07,0.11,0.12,0.15,0.02,0.06,0.06,0.06,0.06");
				BasicXmlDocument.AppendAttribute(mainTable, "rowheight", 17);
				BasicXmlDocument.AppendAttribute(mainTable, "border_colour", border_colour_str);
				BasicXmlDocument.AppendAttribute(mainTable, "row_colour_1", row1_backcolor_str);
				BasicXmlDocument.AppendAttribute(mainTable, "row_colour_2", row2_backcolor_str);

				int row_counter = 0;
				Node reference = model.GetNamedNode("Chargeback Figures");
				foreach (Node figure in reference.GetChildrenOfType("chargeback"))
				{
					if ((figure.GetIntAttribute("round", 0) == round)
						&& (figure.GetAttribute("region") == regionName)
						&& (figure.GetBooleanAttribute("is_new", false) == showNewServices))
					{
						XmlElement row = xml.AppendNewChild(mainTable, "rowdata");

						bool isPresent = false;
						foreach (Node businessService in business.GetChildrenOfType("business_service"))
						{
							if (businessService.GetAttribute("common_service_name") == figure.GetAttribute("common_service_name"))
							{
								isPresent = true;
								break;
							}
						}

						if (showNewServices)
						{
							isPresent = true;
						}

						XmlElement serviceNameCell = xml.AppendNewChild(row, "cell");
						if (isPresent)
						{
							BasicXmlDocument.AppendAttribute(serviceNameCell, "val", figure.GetAttribute("common_service_name"));
						}
						BasicXmlDocument.AppendAttribute(serviceNameCell, "textcolour", row_textcolor_str);
						BasicXmlDocument.AppendAttribute(serviceNameCell, "align", title_align_str);
						BasicXmlDocument.AppendAttribute(serviceNameCell, "no_border", "true");

						XmlElement serviceChannelNameCell = xml.AppendNewChild(row, "cell");
						if (isPresent)
						{
							BasicXmlDocument.AppendAttribute(serviceChannelNameCell, "val", figure.GetAttribute("owner"));
						}
						BasicXmlDocument.AppendAttribute(serviceChannelNameCell, "textcolour", row_textcolor_str);
						BasicXmlDocument.AppendAttribute(serviceChannelNameCell, "no_border", "true");

						int totalCpuPeriods = figure.GetIntAttribute("total_cpu_periods", 0);

						XmlElement totalCpuPeriodsCell = xml.AppendNewChild(row, "cell");
						if (isPresent)
						{
							BasicXmlDocument.AppendAttribute(totalCpuPeriodsCell, "val", CONVERT.ToStr(totalCpuPeriods));
						}
						BasicXmlDocument.AppendAttribute(totalCpuPeriodsCell, "textcolour", row_textcolor_str);
						BasicXmlDocument.AppendAttribute(totalCpuPeriodsCell, "no_border", "true");

						double costPerCpuPeriod = roundScores.NameToRegion[regionName].OpEx
												  / roundScores.NameToRegion[regionName].TotalBusinessDemandCpuPeriods;

						XmlElement costPerCpuPeriodCell = xml.AppendNewChild(row, "cell");
						if (isPresent)
						{
							if (double.IsNaN(costPerCpuPeriod))
							{
								BasicXmlDocument.AppendAttribute(costPerCpuPeriodCell, "val", "");
							}
							else
							{
								BasicXmlDocument.AppendAttribute(costPerCpuPeriodCell, "val", CONVERT.Format("${0:0.00}", costPerCpuPeriod));
							}
						}
						BasicXmlDocument.AppendAttribute(costPerCpuPeriodCell, "textcolour", row_textcolor_str);
						BasicXmlDocument.AppendAttribute(costPerCpuPeriodCell, "no_border", "true");

						XmlElement totalCostCell = xml.AppendNewChild(row, "cell");
						if (isPresent)
						{
							double tmpTotalCostValue = costPerCpuPeriod * totalCpuPeriods;
							if (double.IsNaN(tmpTotalCostValue))
							{
								BasicXmlDocument.AppendAttribute(totalCostCell, "val", "");
							}
							else
							{
								BasicXmlDocument.AppendAttribute(totalCostCell, "val", CONVERT.Format("${0}", CONVERT.ToPaddedStrWithThousands(tmpTotalCostValue, 0)));
							}
						}
						BasicXmlDocument.AppendAttribute(totalCostCell, "textcolour", row_textcolor_str);
						BasicXmlDocument.AppendAttribute(totalCostCell, "no_border", "true");

						XmlElement blankCell = xml.AppendNewChild(row, "cell");
						BasicXmlDocument.AppendAttribute(blankCell, "textcolour", row_textcolor_str);
						BasicXmlDocument.AppendAttribute(blankCell, "no_border", "true");

						for (int period = 0; period < 4; period++)
						{
							string attributeName = CONVERT.Format("period_{0}_cpus_used", period);
							int cpusUsed = figure.GetIntAttribute(attributeName, 0);

							XmlElement cpuCell = xml.AppendNewChild(row, "cell");
							if (isPresent)
							{
								BasicXmlDocument.AppendAttribute(cpuCell, "val", cpusUsed);
							}
							BasicXmlDocument.AppendAttribute(cpuCell, "textcolour", row_textcolor_str);
							BasicXmlDocument.AppendAttribute(cpuCell, "align", cpu_align_str);
							BasicXmlDocument.AppendAttribute(cpuCell, "no_border", "true");
						}
						
						row_counter++;
					}
				}
				if (row_counter < 7)
				{
					for (int step1 = row_counter; step1 < 7; step1++)
					{
						XmlElement row = xml.AppendNewChild(mainTable, "rowdata");

						for (int step2 = 0; step2 < 10; step2++)
						{
							XmlElement tempCell = xml.AppendNewChild(row, "cell");
							BasicXmlDocument.AppendAttribute(tempCell, "val", "");
							BasicXmlDocument.AppendAttribute(tempCell, "textcolour", row_textcolor_str);
							BasicXmlDocument.AppendAttribute(tempCell, "align", title_align_str);
							BasicXmlDocument.AppendAttribute(tempCell, "no_border", "true");
						}
					}
				}
			}

			string filename = gameFile.GetRoundFile(round, "ChargebackReport.xml", GameFile.GamePhase.OPERATIONS);
			xml.Save(filename);
			return filename;
		}
	}
}