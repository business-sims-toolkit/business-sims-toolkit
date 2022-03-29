using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

using LibCore;
using CoreUtils;
using GameManagement;
using Network;

namespace Cloud.ReportsScreen
{
	public class OpExReport
	{
		NetworkProgressionGameFile gameFile;
		Cloud_RoundScores [] scores;

		string border_colour_str = "255,255,255";
		string row1_backcolor_str = "109,104,117";
		string row2_backcolor_str = "91,85,97";
		string header_backcolor_str = "64,64,64";
		string header_textcolor_str = "255,255,255";
		string row_textcolor_str = "255,255,255";
		string title_align_str = "left";

		bool showRegioninSpacerRow = false;


		public OpExReport (NetworkProgressionGameFile gameFile, Cloud_RoundScores [] scores)
		{
			this.gameFile = gameFile;
			this.scores = scores;
		}

		string GetColumnWidthsString (float firstColumnWidth, int columns)
		{
			StringBuilder output = new StringBuilder ();
			output.Append(CONVERT.ToStr(firstColumnWidth));

			float columnWidth = (1 - firstColumnWidth) / columns;

			for (int i = 0; i < columns; i++)
			{
				output.Append(",");
				output.Append(CONVERT.ToStr(columnWidth));
			}

			return output.ToString();
		}

		XmlElement AppendRow (BasicXmlDocument xml, XmlElement table, string heading)
		{
			XmlElement row = xml.AppendNewChild(table, "rowdata");
			XmlElement headerCell = xml.AppendNewChild(row, "cell");
			BasicXmlDocument.AppendAttribute(headerCell, "val", heading);

			return row;
		}

		XmlElement AppendCell (BasicXmlDocument xml, XmlElement row, string text)
		{
			XmlElement cell = xml.AppendNewChild(row, "cell");
			BasicXmlDocument.AppendAttribute(cell, "val", text);

			return cell;
		}

		static string FormatThousands (double a)
		{
			return CONVERT.Format("{0:#,##0}", a);
		}

		static string FormatPercentage (double a)
		{
			return CONVERT.Format("{0:0.0}%", 100 * a);
		}

		static string FormatMoney (double a)
		{
			string value = "$" + FormatThousands(Math.Abs(a));
			if (a < 0)
			{
				value = "(" + value + ")";
			}
			return value;
		}

		public string BuildReport ()
		{
			int rounds = SkinningDefs.TheInstance.GetIntData("roundcount", 5);

			BasicXmlDocument xml = BasicXmlDocument.Create();

			XmlElement rootTable = xml.AppendNewChild("table");
			BasicXmlDocument.AppendAttribute(rootTable, "columns", 1);
			BasicXmlDocument.AppendAttribute(rootTable, "rowheight", 20);
			BasicXmlDocument.AppendAttribute(rootTable, "border_colour", border_colour_str);
			BasicXmlDocument.AppendAttribute(rootTable, "row_colour_1", row1_backcolor_str);
			BasicXmlDocument.AppendAttribute(rootTable, "row_colour_2", row2_backcolor_str);

			float leftColumnWidth = 0.4f;

			XmlElement preambleTable = xml.AppendNewChild(rootTable, "table");
			BasicXmlDocument.AppendAttribute(preambleTable, "columns", 1 + rounds);
			BasicXmlDocument.AppendAttribute(preambleTable, "widths", GetColumnWidthsString(leftColumnWidth, rounds));
			BasicXmlDocument.AppendAttribute(preambleTable, "border_colour", border_colour_str);
			BasicXmlDocument.AppendAttribute(preambleTable, "row_colour_1", row1_backcolor_str);
			BasicXmlDocument.AppendAttribute(preambleTable, "row_colour_2", row2_backcolor_str);

			XmlElement headerRow = xml.AppendNewChild(preambleTable, "rowdata");
			XmlElement blankHeaderCell = xml.AppendNewChild(headerRow, "cell");
			BasicXmlDocument.AppendAttribute(blankHeaderCell, "no_border", "true");

			for (int round = 1; round <= rounds; round++)
			{
				XmlElement roundHeaderCell = xml.AppendNewChild(headerRow, "cell");
				BasicXmlDocument.AppendAttribute(roundHeaderCell, "align", "middle");
				BasicXmlDocument.AppendAttribute(roundHeaderCell, "val", CONVERT.Format("Round {0}", round));
				BasicXmlDocument.AppendAttribute(roundHeaderCell, "textcolour", row_textcolor_str);
				BasicXmlDocument.AppendAttribute(roundHeaderCell, "textstyle", "bold");
				BasicXmlDocument.AppendAttribute(roundHeaderCell, "no_border", "true");
			}

			List<Node> businesses = new List<Node> ();
			foreach (Node business in gameFile.GetNetworkModel(1).GetNodesWithAttributeValue("type", "business"))
			{
				businesses.Add(business);
			}
			businesses.Sort(delegate (Node a, Node b)
							{
								return a.GetIntAttribute("order", 0).CompareTo(b.GetIntAttribute("order", 0));
							});

			foreach (Node businessNode in businesses)
			{
				string business = businessNode.GetAttribute("region");

				List<Cloud_RoundScores.RegionPerformance> roundPerformances = new List<Cloud_RoundScores.RegionPerformance> ();
				for (int round = 1; round <= scores.Length; round++)
				{
					roundPerformances.Add(scores[round - 1].NameToRegion[business]);
				}

				XmlElement businessHeaderRow = xml.AppendNewChild(rootTable, "rowdata");
				XmlElement businessHeaderCell = xml.AppendNewChild(businessHeaderRow, "cell");
				BasicXmlDocument.AppendAttribute(businessHeaderCell, "colour", header_backcolor_str);
				BasicXmlDocument.AppendAttribute(businessHeaderCell, "textcolour", header_textcolor_str);
				BasicXmlDocument.AppendAttribute(businessHeaderCell, "textstyle", "bold");
				if (showRegioninSpacerRow)
				{
					BasicXmlDocument.AppendAttribute(businessHeaderCell, "val", business);
				}
				else
				{
					BasicXmlDocument.AppendAttribute(businessHeaderCell, "val", "");
				}
				BasicXmlDocument.AppendAttribute(businessHeaderCell, "align", title_align_str);
				BasicXmlDocument.AppendAttribute(businessHeaderCell, "no_border", "true");

				XmlElement businessTable = xml.AppendNewChild(rootTable, "table");
				BasicXmlDocument.AppendAttribute(businessTable, "columns", 1 + rounds);
				BasicXmlDocument.AppendAttribute(businessTable, "widths", GetColumnWidthsString(leftColumnWidth, rounds));
				BasicXmlDocument.AppendAttribute(businessTable, "border_colour", border_colour_str);
				BasicXmlDocument.AppendAttribute(businessTable, "row_colour_1", row1_backcolor_str);
				BasicXmlDocument.AppendAttribute(businessTable, "row_colour_2", row2_backcolor_str);

				XmlElement totalbusinessDemandRow = xml.AppendNewChild(businessTable, "rowdata");
				XmlElement totalBusinessDemandHeaderCell = xml.AppendNewChild(totalbusinessDemandRow, "cell");
				BasicXmlDocument.AppendAttribute(totalBusinessDemandHeaderCell, "val", "Total Business Demand (CPU-Periods)");
				BasicXmlDocument.AppendAttribute(totalBusinessDemandHeaderCell, "textcolour", header_textcolor_str);
				BasicXmlDocument.AppendAttribute(totalBusinessDemandHeaderCell, "no_border", "true");
				BasicXmlDocument.AppendAttribute(totalBusinessDemandHeaderCell, "align", title_align_str);

				for (int round = 1; round <= rounds; round++)
				{
					XmlElement totalBusinessDemandCell = xml.AppendNewChild(totalbusinessDemandRow, "cell");

					if ((round - 1) < roundPerformances.Count)
					{
						BasicXmlDocument.AppendAttribute(totalBusinessDemandCell, "val", CONVERT.ToPaddedStrWithThousands(roundPerformances[round - 1].TotalBusinessDemandCpuPeriods, 0));
						BasicXmlDocument.AppendAttribute(totalBusinessDemandCell, "textcolour", row_textcolor_str);
						BasicXmlDocument.AppendAttribute(totalBusinessDemandCell, "no_border", "true");
					}
					else
					{
						BasicXmlDocument.AppendAttribute(totalBusinessDemandCell, "val", "");
						BasicXmlDocument.AppendAttribute(totalBusinessDemandCell, "textcolour", row_textcolor_str);
						BasicXmlDocument.AppendAttribute(totalBusinessDemandCell, "no_border", "true");
					}
				}

				XmlElement totalCapacityRow = xml.AppendNewChild(businessTable, "rowdata");
				XmlElement totalCapacityHeaderCell = xml.AppendNewChild(totalCapacityRow, "cell");
				BasicXmlDocument.AppendAttribute(totalCapacityHeaderCell, "val", "Total Capacity (CPU-Periods)");
				BasicXmlDocument.AppendAttribute(totalCapacityHeaderCell, "textcolour", header_textcolor_str);
				BasicXmlDocument.AppendAttribute(totalCapacityHeaderCell, "no_border", "true");
				BasicXmlDocument.AppendAttribute(totalCapacityHeaderCell, "align", title_align_str);

				for (int round = 1; round <= rounds; round++)
				{
					XmlElement totalCapacityCell = xml.AppendNewChild(totalCapacityRow, "cell");

					if ((round - 1) < roundPerformances.Count)
					{
						BasicXmlDocument.AppendAttribute(totalCapacityCell, "val", CONVERT.ToPaddedStrWithThousands(roundPerformances[round - 1].TotalCapacityCpuPeriods, 0));
						BasicXmlDocument.AppendAttribute(totalCapacityCell, "textcolour", row_textcolor_str);
						BasicXmlDocument.AppendAttribute(totalCapacityCell, "no_border", "true");
					}
					else
					{
						BasicXmlDocument.AppendAttribute(totalCapacityCell, "val", "");
						BasicXmlDocument.AppendAttribute(totalCapacityCell, "textcolour", row_textcolor_str);
						BasicXmlDocument.AppendAttribute(totalCapacityCell, "no_border", "true");
					}
				}

				XmlElement localProvisionRow = xml.AppendNewChild(businessTable, "rowdata");
				XmlElement localProvisionHeaderCell = xml.AppendNewChild(localProvisionRow, "cell");
				BasicXmlDocument.AppendAttribute(localProvisionHeaderCell, "val", "Internal Provision");
				BasicXmlDocument.AppendAttribute(localProvisionHeaderCell, "textcolour", header_textcolor_str);
				BasicXmlDocument.AppendAttribute(localProvisionHeaderCell, "no_border", "true");
				BasicXmlDocument.AppendAttribute(localProvisionHeaderCell, "align", title_align_str);

				for (int round = 1; round <= rounds; round++)
				{
					XmlElement localProvisionCell = xml.AppendNewChild(localProvisionRow, "cell");

					if ((round - 1) < roundPerformances.Count)
					{
						BasicXmlDocument.AppendAttribute(localProvisionCell, "val", CONVERT.Format("{0:0}%", (100 * roundPerformances[round - 1].InternalCpuProvisionFraction)));
						BasicXmlDocument.AppendAttribute(localProvisionCell, "textcolour", row_textcolor_str);
						BasicXmlDocument.AppendAttribute(localProvisionCell, "no_border", "true");
					}
					else
					{
						BasicXmlDocument.AppendAttribute(localProvisionCell, "val", "");
						BasicXmlDocument.AppendAttribute(localProvisionCell, "textcolour", row_textcolor_str);
						BasicXmlDocument.AppendAttribute(localProvisionCell, "no_border", "true");
					}
				}

				XmlElement publicProvisionRow = xml.AppendNewChild(businessTable, "rowdata");
				XmlElement publicProvisionHeaderCell = xml.AppendNewChild(publicProvisionRow, "cell");
				BasicXmlDocument.AppendAttribute(publicProvisionHeaderCell, "val", "External Provision");
				BasicXmlDocument.AppendAttribute(publicProvisionHeaderCell, "textcolour", header_textcolor_str);
				BasicXmlDocument.AppendAttribute(publicProvisionHeaderCell, "no_border", "true");
				BasicXmlDocument.AppendAttribute(publicProvisionHeaderCell, "align", title_align_str);

				for (int round = 1; round <= rounds; round++)
				{
					XmlElement publicProvisionCell = xml.AppendNewChild(publicProvisionRow, "cell");

					if ((round - 1) < roundPerformances.Count)
					{
						BasicXmlDocument.AppendAttribute(publicProvisionCell, "val", CONVERT.Format("{0:0}%", (100 * roundPerformances[round - 1].ExternalCpuProvisionFraction)));
						BasicXmlDocument.AppendAttribute(publicProvisionCell, "textcolour", row_textcolor_str);
						BasicXmlDocument.AppendAttribute(publicProvisionCell, "no_border", "true");
					}
					else
					{
						BasicXmlDocument.AppendAttribute(publicProvisionCell, "val", "");
						BasicXmlDocument.AppendAttribute(publicProvisionCell, "textcolour", row_textcolor_str);
						BasicXmlDocument.AppendAttribute(publicProvisionCell, "no_border", "true");
					}
				}

				XmlElement opexRow = xml.AppendNewChild(businessTable, "rowdata");
				XmlElement opexHeaderCell = xml.AppendNewChild(opexRow, "cell");
				BasicXmlDocument.AppendAttribute(opexHeaderCell, "val", "Opex");
				BasicXmlDocument.AppendAttribute(opexHeaderCell, "textcolour", header_textcolor_str);
				BasicXmlDocument.AppendAttribute(opexHeaderCell, "no_border", "true");
				BasicXmlDocument.AppendAttribute(opexHeaderCell, "align", title_align_str);

				for (int round = 1; round <= rounds; round++)
				{
					XmlElement opexCell = xml.AppendNewChild(opexRow, "cell");

					if ((round - 1) < roundPerformances.Count)
					{
						BasicXmlDocument.AppendAttribute(opexCell, "val", FormatMoney(roundPerformances[round - 1].OpEx));
						BasicXmlDocument.AppendAttribute(opexCell, "textcolour", row_textcolor_str);
						BasicXmlDocument.AppendAttribute(opexCell, "no_border", "true");
					}
					else
					{
						BasicXmlDocument.AppendAttribute(opexCell, "val", "");
						BasicXmlDocument.AppendAttribute(opexCell, "textcolour", row_textcolor_str);
						BasicXmlDocument.AppendAttribute(opexCell, "no_border", "true");
					}
				}

				XmlElement businessCostPerCuRow = xml.AppendNewChild(businessTable, "rowdata");
				XmlElement businessCostPerCuHeaderCell = xml.AppendNewChild(businessCostPerCuRow, "cell");
				BasicXmlDocument.AppendAttribute(businessCostPerCuHeaderCell, "val", "Business Cost Per CPU-Period");
				BasicXmlDocument.AppendAttribute(businessCostPerCuHeaderCell, "textcolour", header_textcolor_str);
				BasicXmlDocument.AppendAttribute(businessCostPerCuHeaderCell, "no_border", "true");
				BasicXmlDocument.AppendAttribute(businessCostPerCuHeaderCell, "align", title_align_str);

				for (int round = 1; round <= rounds; round++)
				{
					XmlElement businessCostPerCuCell = xml.AppendNewChild(businessCostPerCuRow, "cell");

					if ((round - 1) < roundPerformances.Count)
					{
						BasicXmlDocument.AppendAttribute(businessCostPerCuCell, "val", FormatMoney(roundPerformances[round - 1].OpEx
					     / roundPerformances[round - 1].TotalBusinessDemandCpuPeriods));
						BasicXmlDocument.AppendAttribute(businessCostPerCuCell, "textcolour", row_textcolor_str);
						BasicXmlDocument.AppendAttribute(businessCostPerCuCell, "no_border", "true");
					}
					else
					{
						BasicXmlDocument.AppendAttribute(businessCostPerCuCell, "val", "");
						BasicXmlDocument.AppendAttribute(businessCostPerCuCell, "textcolour", row_textcolor_str);
						BasicXmlDocument.AppendAttribute(businessCostPerCuCell, "no_border", "true");
					}
				}

				XmlElement utilisationRow = xml.AppendNewChild(businessTable, "rowdata");
				XmlElement utilisationHeaderCell = xml.AppendNewChild(utilisationRow, "cell");
				BasicXmlDocument.AppendAttribute(utilisationHeaderCell, "val", "Utilization");
				BasicXmlDocument.AppendAttribute(utilisationHeaderCell, "textcolour", header_textcolor_str);
				BasicXmlDocument.AppendAttribute(utilisationHeaderCell, "no_border", "true");
				BasicXmlDocument.AppendAttribute(utilisationHeaderCell, "align", title_align_str);

				for (int round = 1; round <= rounds; round++)
				{
					XmlElement utilisationCell = xml.AppendNewChild(utilisationRow, "cell");

					if ((round - 1) < roundPerformances.Count)
					{
						BasicXmlDocument.AppendAttribute(utilisationCell, "val", FormatPercentage(roundPerformances[round - 1].CpuUtilisation));
						BasicXmlDocument.AppendAttribute(utilisationCell, "textcolour", row_textcolor_str);
						BasicXmlDocument.AppendAttribute(utilisationCell, "no_border", "true");
					}
					else
					{
						BasicXmlDocument.AppendAttribute(utilisationCell, "val", "");
						BasicXmlDocument.AppendAttribute(utilisationCell, "textcolour", row_textcolor_str);
						BasicXmlDocument.AppendAttribute(utilisationCell, "no_border", "true");
					}
				}
			}

			string reportFile = gameFile.GetRoundFile(gameFile.LastRoundPlayed, "OpExReport.xml", GameFile.GamePhase.OPERATIONS);
			xml.Save(reportFile);
			return reportFile;
		}
	}
}