using System;
using System.Xml;

using LibCore;
using CoreUtils;
using GameManagement;

namespace Cloud.ReportsScreen
{
	public class OperationsScoreCardReport
	{
		NetworkProgressionGameFile gameFile;
		Cloud_RoundScores [] scores;

		string border_colour_str = "255,255,255";
		string row1_backcolor_str = "109,104,117";
		string row2_backcolor_str = "91,85,97";
		string header_backcolor_str = "64,64,64";
		string header_textcolor_str = "255,255,255";
		string row_textcolor_str = "255,255,255";
		string header_align_str = "left";
		string title_align_str = "left";

		bool showRegioninSpacerRow = false;

		public OperationsScoreCardReport (NetworkProgressionGameFile gameFile, Cloud_RoundScores [] scores)
		{
			this.gameFile = gameFile;
			this.scores = scores;
		}

		string GetColumnWidthsString (double leftColumnWidth, int rounds)
		{
			return CONVERT.ToStr(leftColumnWidth) + string.Join("," + CONVERT.ToStr((1 - leftColumnWidth) / rounds), new string [1 + rounds]);
		}

		public string BuildReport ()
		{
			int rounds = SkinningDefs.TheInstance.GetIntData("roundcount", 5);
			int rowHeight = 20;
			string columnWidths = GetColumnWidthsString(0.3, rounds);

			BasicXmlDocument xml = BasicXmlDocument.Create();

			// Top-level table.
			XmlElement rootTable = xml.AppendNewChild("table");
			BasicXmlDocument.AppendAttribute(rootTable, "columns", 1);
			BasicXmlDocument.AppendAttribute(rootTable, "rowheight", rowHeight);
			BasicXmlDocument.AppendAttribute(rootTable, "border_colour", border_colour_str);
			BasicXmlDocument.AppendAttribute(rootTable, "row_colour_1", row1_backcolor_str);
			BasicXmlDocument.AppendAttribute(rootTable, "row_colour_2", row2_backcolor_str);

			// Round headings.
			XmlElement columnHeadingTable = xml.AppendNewChild(rootTable, "table");
			BasicXmlDocument.AppendAttribute(columnHeadingTable, "columns", 1 + rounds);
			BasicXmlDocument.AppendAttribute(columnHeadingTable, "widths", columnWidths);
			BasicXmlDocument.AppendAttribute(columnHeadingTable, "border_colour", border_colour_str);
			BasicXmlDocument.AppendAttribute(columnHeadingTable, "row_colour_1", row1_backcolor_str);
			BasicXmlDocument.AppendAttribute(columnHeadingTable, "row_colour_2", row2_backcolor_str);
			XmlElement columnHeadingRow = xml.AppendNewChild(columnHeadingTable, "rowdata");
			XmlElement columnHeadingLegendCell = xml.AppendNewChild(columnHeadingRow, "cell");
			BasicXmlDocument.AppendAttribute(columnHeadingLegendCell, "val", "Operating Performance");
			BasicXmlDocument.AppendAttribute(columnHeadingLegendCell, "textcolour", row_textcolor_str);
			BasicXmlDocument.AppendAttribute(columnHeadingLegendCell, "textstyle", "bold");
			BasicXmlDocument.AppendAttribute(columnHeadingLegendCell, "no_border", "true");
			BasicXmlDocument.AppendAttribute(columnHeadingLegendCell, "align", header_align_str);

			for (int round = 1; round <= rounds; round++)
			{
				XmlElement columnHeadingCell = xml.AppendNewChild(columnHeadingRow, "cell");
				BasicXmlDocument.AppendAttribute(columnHeadingCell, "val", CONVERT.Format("Round {0}", round));
				BasicXmlDocument.AppendAttribute(columnHeadingCell, "textcolour", row_textcolor_str);
				BasicXmlDocument.AppendAttribute(columnHeadingCell, "textstyle", "bold");
				BasicXmlDocument.AppendAttribute(columnHeadingCell, "no_border", "true");
			}

			foreach (string region in scores[0].Regions)
			{
				// Heading.
				XmlElement operatingPerformanceHeadingTable = xml.AppendNewChild(rootTable, "table");
				BasicXmlDocument.AppendAttribute(operatingPerformanceHeadingTable, "columns", 1);
				BasicXmlDocument.AppendAttribute(operatingPerformanceHeadingTable, "border_colour", border_colour_str);
				BasicXmlDocument.AppendAttribute(operatingPerformanceHeadingTable, "row_colour_1", row1_backcolor_str);
				BasicXmlDocument.AppendAttribute(operatingPerformanceHeadingTable, "row_colour_2", row2_backcolor_str);
				XmlElement operatingPerformanceHeadingRow = xml.AppendNewChild(operatingPerformanceHeadingTable, "rowdata");
				XmlElement operatingPerformanceHeadingCell = xml.AppendNewChild(operatingPerformanceHeadingRow, "cell");
				BasicXmlDocument.AppendAttribute(operatingPerformanceHeadingCell, "colour", header_backcolor_str);
				BasicXmlDocument.AppendAttribute(operatingPerformanceHeadingCell, "textcolour", header_textcolor_str);
				BasicXmlDocument.AppendAttribute(operatingPerformanceHeadingCell, "textstyle", "bold");
				BasicXmlDocument.AppendAttribute(operatingPerformanceHeadingCell, "align", title_align_str);

				if (showRegioninSpacerRow)
				{
					BasicXmlDocument.AppendAttribute(operatingPerformanceHeadingCell, "val", region);
					BasicXmlDocument.AppendAttribute(operatingPerformanceHeadingCell, "textcolour", header_textcolor_str);
					BasicXmlDocument.AppendAttribute(operatingPerformanceHeadingCell, "no_border", "true");
				}
				else
				{
					BasicXmlDocument.AppendAttribute(operatingPerformanceHeadingCell, "val", "");
					BasicXmlDocument.AppendAttribute(operatingPerformanceHeadingCell, "textcolour", header_textcolor_str);
					BasicXmlDocument.AppendAttribute(operatingPerformanceHeadingCell, "no_border", "true");
				}

				// Results.
				XmlElement operatingPerformanceResultsTable = xml.AppendNewChild(rootTable, "table");
				BasicXmlDocument.AppendAttribute(operatingPerformanceResultsTable, "columns", 1 + rounds);
				BasicXmlDocument.AppendAttribute(operatingPerformanceResultsTable, "widths", columnWidths);
				BasicXmlDocument.AppendAttribute(operatingPerformanceResultsTable, "border_colour", border_colour_str);
				BasicXmlDocument.AppendAttribute(operatingPerformanceResultsTable, "row_colour_1", row1_backcolor_str);
				BasicXmlDocument.AppendAttribute(operatingPerformanceResultsTable, "row_colour_2", row2_backcolor_str);

				XmlElement operatingPerformanceItBudgetRow = xml.AppendNewChild(operatingPerformanceResultsTable, "rowdata");
				XmlElement operatingPerformanceItBudgetLegend = xml.AppendNewChild(operatingPerformanceItBudgetRow, "cell");
				BasicXmlDocument.AppendAttribute(operatingPerformanceItBudgetLegend, "val", "IT Budget ($M)");
				BasicXmlDocument.AppendAttribute(operatingPerformanceItBudgetLegend, "textcolour", row_textcolor_str);
				BasicXmlDocument.AppendAttribute(operatingPerformanceItBudgetLegend, "no_border", "true");
				BasicXmlDocument.AppendAttribute(operatingPerformanceItBudgetLegend, "align", title_align_str);

				for (int round = 1; round <= rounds; round++)
				{
					XmlElement operatingPerformanceItBudgetCell = xml.AppendNewChild(operatingPerformanceItBudgetRow, "cell");
					if (round <= scores.Length)
					{
						BasicXmlDocument.AppendAttribute(operatingPerformanceItBudgetCell, "val",
							CONVERT.ToPaddedStrWithThousands(scores[round - 1].NameToRegion[region].ITBudget / 1000000.0, 3));
						BasicXmlDocument.AppendAttribute(operatingPerformanceItBudgetCell, "textcolour", row_textcolor_str);
						BasicXmlDocument.AppendAttribute(operatingPerformanceItBudgetCell, "no_border", "true");
					}
					else
					{
						BasicXmlDocument.AppendAttribute(operatingPerformanceItBudgetCell, "val", "");
						BasicXmlDocument.AppendAttribute(operatingPerformanceItBudgetCell, "textcolour", row_textcolor_str);
						BasicXmlDocument.AppendAttribute(operatingPerformanceItBudgetCell, "no_border", "true");
					}
				}

				XmlElement operatingPerformanceDevTestCostRow = xml.AppendNewChild(operatingPerformanceResultsTable, "rowdata");
				XmlElement operatingPerformanceDevTestCostLegend = xml.AppendNewChild(operatingPerformanceDevTestCostRow, "cell");
				BasicXmlDocument.AppendAttribute(operatingPerformanceDevTestCostLegend, "val", "Dev & Test Costs ($M)");
				BasicXmlDocument.AppendAttribute(operatingPerformanceDevTestCostLegend, "textcolour", row_textcolor_str);
				BasicXmlDocument.AppendAttribute(operatingPerformanceDevTestCostLegend, "no_border", "true");
				BasicXmlDocument.AppendAttribute(operatingPerformanceDevTestCostLegend, "align", title_align_str);

				for (int round = 1; round <= rounds; round++)
				{
					XmlElement operatingPerformanceDevTestCostCell = xml.AppendNewChild(operatingPerformanceDevTestCostRow, "cell");
					if (round <= scores.Length)
					{
						BasicXmlDocument.AppendAttribute(operatingPerformanceDevTestCostCell, "val",
														 CONVERT.ToPaddedStrWithThousands(scores[round - 1].NameToRegion[region].DevTestCost / 1000000.0, 3));
						BasicXmlDocument.AppendAttribute(operatingPerformanceDevTestCostCell, "textcolour", row_textcolor_str);
						BasicXmlDocument.AppendAttribute(operatingPerformanceDevTestCostCell, "no_border", "true");
					}
					else
					{
						BasicXmlDocument.AppendAttribute(operatingPerformanceDevTestCostCell, "val", "");
						BasicXmlDocument.AppendAttribute(operatingPerformanceDevTestCostCell, "textcolour", row_textcolor_str);
						BasicXmlDocument.AppendAttribute(operatingPerformanceDevTestCostCell, "no_border", "true");
					}
				}

				XmlElement operatingPerformanceProductionCostRow = xml.AppendNewChild(operatingPerformanceResultsTable, "rowdata");
				XmlElement operatingPerformanceProductionCostLegend = xml.AppendNewChild(operatingPerformanceProductionCostRow, "cell");
				BasicXmlDocument.AppendAttribute(operatingPerformanceProductionCostLegend, "val", "Production Costs ($M)");
				BasicXmlDocument.AppendAttribute(operatingPerformanceProductionCostLegend, "textcolour", row_textcolor_str);
				BasicXmlDocument.AppendAttribute(operatingPerformanceProductionCostLegend, "no_border", "true");
				BasicXmlDocument.AppendAttribute(operatingPerformanceProductionCostLegend, "align", title_align_str);

				for (int round = 1; round <= rounds; round++)
				{
					XmlElement operatingPerformanceProductionCostCell = xml.AppendNewChild(operatingPerformanceProductionCostRow, "cell");
					if (round <= scores.Length)
					{
						BasicXmlDocument.AppendAttribute(operatingPerformanceProductionCostCell, "val",
														 CONVERT.ToPaddedStrWithThousands(scores[round - 1].NameToRegion[region].ProductionCost / 1000000.0, 3));
						BasicXmlDocument.AppendAttribute(operatingPerformanceProductionCostCell, "textcolour", row_textcolor_str);
						BasicXmlDocument.AppendAttribute(operatingPerformanceProductionCostCell, "no_border", "true");
					}
					else
					{
						BasicXmlDocument.AppendAttribute(operatingPerformanceProductionCostCell, "val", "");
						BasicXmlDocument.AppendAttribute(operatingPerformanceProductionCostCell, "textcolour", row_textcolor_str);
						BasicXmlDocument.AppendAttribute(operatingPerformanceProductionCostCell, "no_border", "true");
					}
				}

				XmlElement operatingPerformanceOverspendRow = xml.AppendNewChild(operatingPerformanceResultsTable, "rowdata");
				XmlElement operatingPerformanceOverspendLegend = xml.AppendNewChild(operatingPerformanceOverspendRow, "cell");
				BasicXmlDocument.AppendAttribute(operatingPerformanceOverspendLegend, "val", "Profit / Loss ($M)");
				BasicXmlDocument.AppendAttribute(operatingPerformanceOverspendLegend, "textcolour", row_textcolor_str);
				BasicXmlDocument.AppendAttribute(operatingPerformanceOverspendLegend, "no_border", "true");
				BasicXmlDocument.AppendAttribute(operatingPerformanceOverspendLegend, "align", title_align_str);

				for (int round = 1; round <= rounds; round++)
				{
					XmlElement operatingPerformanceOverspendCell = xml.AppendNewChild(operatingPerformanceOverspendRow, "cell");
					if (round <= scores.Length)
					{
						string data_display = string.Empty;
						double data_value = scores[round - 1].NameToRegion[region].ITOverspend;

						if (data_value>0)
						{
							data_display = CONVERT.ToPaddedStrWithThousands(data_value / 1000000.0, 3);
						}
						else
						{
							data_value = Math.Abs(data_value);
							data_display = "(" + CONVERT.ToPaddedStrWithThousands(data_value / 1000000.0, 3) + ")";
						}
						BasicXmlDocument.AppendAttribute(operatingPerformanceOverspendCell, "val", data_display);
						BasicXmlDocument.AppendAttribute(operatingPerformanceOverspendCell, "textcolour", row_textcolor_str);
						BasicXmlDocument.AppendAttribute(operatingPerformanceOverspendCell, "no_border", "true");
					}
					else
					{
						BasicXmlDocument.AppendAttribute(operatingPerformanceOverspendCell, "val", "");
						BasicXmlDocument.AppendAttribute(operatingPerformanceOverspendCell, "textcolour", row_textcolor_str);
						BasicXmlDocument.AppendAttribute(operatingPerformanceOverspendCell, "no_border", "true");
					}
				}

				XmlElement operatingPerformanceDevCapacityRow = xml.AppendNewChild(operatingPerformanceResultsTable, "rowdata");
				XmlElement operatingPerformanceDevCapacityLegend = xml.AppendNewChild(operatingPerformanceDevCapacityRow, "cell");
				BasicXmlDocument.AppendAttribute(operatingPerformanceDevCapacityLegend, "val", "Dev & Test Capacity (CPUs)");
				BasicXmlDocument.AppendAttribute(operatingPerformanceDevCapacityLegend, "textcolour", row_textcolor_str);
				BasicXmlDocument.AppendAttribute(operatingPerformanceDevCapacityLegend, "no_border", "true");
				BasicXmlDocument.AppendAttribute(operatingPerformanceDevCapacityLegend, "align", title_align_str);

				for (int round = 1; round <= rounds; round++)
				{
					XmlElement operatingPerformanceDevCapacityCell = xml.AppendNewChild(operatingPerformanceDevCapacityRow, "cell");
					if (round <= scores.Length)
					{
						if (! double.IsNaN(scores[round - 1].NameToRegion[region].DevCpuUtilisation))
						{
							BasicXmlDocument.AppendAttribute(operatingPerformanceDevCapacityCell, "val",
															 CONVERT.ToPaddedStrWithThousands(scores[round - 1].NameToRegion[region].DevCpus, 0)
															 + CONVERT.Format(" ({0}% used)", (int) (100 * scores[round - 1].NameToRegion[region].DevCpuUtilisation)));
						}
						else
						{
							BasicXmlDocument.AppendAttribute(operatingPerformanceDevCapacityCell, "val",
															 CONVERT.ToPaddedStrWithThousands(scores[round - 1].NameToRegion[region].DevCpus, 0));
						}
						BasicXmlDocument.AppendAttribute(operatingPerformanceDevCapacityCell, "textcolour", row_textcolor_str);
						BasicXmlDocument.AppendAttribute(operatingPerformanceDevCapacityCell, "no_border", "true");
					}
					else
					{
						BasicXmlDocument.AppendAttribute(operatingPerformanceDevCapacityCell, "val", "");
						BasicXmlDocument.AppendAttribute(operatingPerformanceDevCapacityCell, "textcolour", row_textcolor_str);
						BasicXmlDocument.AppendAttribute(operatingPerformanceDevCapacityCell, "no_border", "true");
					}
				}

				XmlElement operatingPerformanceProductionCapacityRow = xml.AppendNewChild(operatingPerformanceResultsTable, "rowdata");
				XmlElement operatingPerformanceProductionCapacityLegend = xml.AppendNewChild(operatingPerformanceProductionCapacityRow, "cell");
				BasicXmlDocument.AppendAttribute(operatingPerformanceProductionCapacityLegend, "val", "Production Capacity (CPUs)");
				BasicXmlDocument.AppendAttribute(operatingPerformanceProductionCapacityLegend, "textcolour", row_textcolor_str);
				BasicXmlDocument.AppendAttribute(operatingPerformanceProductionCapacityLegend, "no_border", "true");
				BasicXmlDocument.AppendAttribute(operatingPerformanceProductionCapacityLegend, "align", title_align_str);

				for (int round = 1; round <= rounds; round++)
				{
					XmlElement operatingPerformanceProductionCapacityCell = xml.AppendNewChild(operatingPerformanceProductionCapacityRow, "cell");
					if (round <= scores.Length)
					{
						if (! double.IsNaN(scores[round - 1].NameToRegion[region].ProductionCpuUtilisation))
						{
							BasicXmlDocument.AppendAttribute(operatingPerformanceProductionCapacityCell, "val",
															 CONVERT.ToPaddedStrWithThousands(scores[round - 1].NameToRegion[region].ProductionCpus, 0)
															 + CONVERT.Format(" ({0}% used)", (int)(100 * scores[round - 1].NameToRegion[region].ProductionCpuUtilisation)));
						}
						else
						{
							BasicXmlDocument.AppendAttribute(operatingPerformanceProductionCapacityCell, "val",
																CONVERT.ToPaddedStrWithThousands(scores[round - 1].NameToRegion[region].ProductionCpus, 0));
						}
						BasicXmlDocument.AppendAttribute(operatingPerformanceProductionCapacityCell, "textcolour", row_textcolor_str);
						BasicXmlDocument.AppendAttribute(operatingPerformanceProductionCapacityCell, "no_border", "true");
					}
					else
					{
						BasicXmlDocument.AppendAttribute(operatingPerformanceProductionCapacityCell, "val", "");
						BasicXmlDocument.AppendAttribute(operatingPerformanceProductionCapacityCell, "textcolour", row_textcolor_str);
						BasicXmlDocument.AppendAttribute(operatingPerformanceProductionCapacityCell, "no_border", "true");
					}
				}

				XmlElement operatingPerformanceEnergyUsageRow = xml.AppendNewChild(operatingPerformanceResultsTable, "rowdata");
				XmlElement operatingPerformanceEnergyUsageLegend = xml.AppendNewChild(operatingPerformanceEnergyUsageRow, "cell");
				BasicXmlDocument.AppendAttribute(operatingPerformanceEnergyUsageLegend, "val", "Energy Usage (kWh)");
				BasicXmlDocument.AppendAttribute(operatingPerformanceEnergyUsageLegend, "textcolour", row_textcolor_str);
				BasicXmlDocument.AppendAttribute(operatingPerformanceEnergyUsageLegend, "no_border", "true");
				BasicXmlDocument.AppendAttribute(operatingPerformanceEnergyUsageLegend, "align", title_align_str);

				for (int round = 1; round <= rounds; round++)
				{
					XmlElement operatingPerformanceEnergyUsageCell = xml.AppendNewChild(operatingPerformanceEnergyUsageRow, "cell");
					if (round <= scores.Length)
					{
						string pue = "";
						if (!double.IsNaN(scores[round - 1].NameToRegion[region].Pue))
						{
							pue = CONVERT.Format("(PUE {0:0.0})", scores[round - 1].NameToRegion[region].Pue);
						}

						BasicXmlDocument.AppendAttribute(operatingPerformanceEnergyUsageCell, "val",
							CONVERT.Format("{0} {1}", CONVERT.ToPaddedStrWithThousands(scores[round - 1].NameToRegion[region].EnergyKWs / 3600.0, 0), pue));
						BasicXmlDocument.AppendAttribute(operatingPerformanceEnergyUsageCell, "textcolour", row_textcolor_str);
						BasicXmlDocument.AppendAttribute(operatingPerformanceEnergyUsageCell, "no_border", "true");
					}
					else
					{
						BasicXmlDocument.AppendAttribute(operatingPerformanceEnergyUsageCell, "val","");
						BasicXmlDocument.AppendAttribute(operatingPerformanceEnergyUsageCell, "textcolour", row_textcolor_str);
						BasicXmlDocument.AppendAttribute(operatingPerformanceEnergyUsageCell, "no_border", "true");
					}
				}
			}

			// Save.
			string reportFile = gameFile.GetRoundFile(gameFile.LastRoundPlayed, "ScoreCardReport.xml", GameFile.GamePhase.OPERATIONS);
			xml.Save(reportFile);
			return reportFile;
		}

		static string FormatThousands (long a)
		{
			return CONVERT.Format("{0:#,##0}", a);
		}

		static string FormatMoney (long a)
		{
			string value = "$" + FormatThousands(Math.Abs(a));
			if (a < 0)
			{
				value = "(" + value + ")";
			}

			return value;
		}
	}
}