using System;
using System.Xml;

using LibCore;
using CoreUtils;
using GameManagement;

namespace Cloud.ReportsScreen
{
	public class BusinessScoreCardReport
	{
		NetworkProgressionGameFile gameFile;
		Cloud_RoundScores [] scores;

		string border_colour_str = "255,255,255";
		string row1_backcolor_str = "109,104,117";
		string row2_backcolor_str = "91,85,97";
		string header_backcolor_str = "64,64,64";
		string header_textcolor_str = "255,255,255";
		string row_textcolor_str = "255,255,255";
		//string header_align_str = "middle";
		string header_align_str = "left";
		string title_align_str = "left";

		bool showBorder = false;
		bool showRegioninSpacerRow = false;
		
		public BusinessScoreCardReport (NetworkProgressionGameFile gameFile, Cloud_RoundScores [] scores)
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
			int rowHeight = 23;
			string columnWidths = GetColumnWidthsString(0.35, rounds);

			BasicXmlDocument xml = BasicXmlDocument.Create();

			// Top-level table.
			XmlElement rootTable = xml.AppendNewChild("table");
			BasicXmlDocument.AppendAttribute(rootTable, "columns", 1);
			BasicXmlDocument.AppendAttribute(rootTable, "rowheight", rowHeight);
			if (showBorder)
			{
				BasicXmlDocument.AppendAttribute(rootTable, "border_colour", border_colour_str);
			}
			BasicXmlDocument.AppendAttribute(rootTable, "row_colour_1", row1_backcolor_str);
			BasicXmlDocument.AppendAttribute(rootTable, "row_colour_2", row2_backcolor_str);

			// Round headings.
			XmlElement columnHeadingTable = xml.AppendNewChild(rootTable, "table");
			BasicXmlDocument.AppendAttribute(columnHeadingTable, "columns", 1 + rounds);
			BasicXmlDocument.AppendAttribute(columnHeadingTable, "widths", columnWidths);
			if (showBorder)
			{
				BasicXmlDocument.AppendAttribute(columnHeadingTable, "border_colour", border_colour_str);
			}
			BasicXmlDocument.AppendAttribute(columnHeadingTable, "row_colour_1", row1_backcolor_str);
			BasicXmlDocument.AppendAttribute(columnHeadingTable, "row_colour_2", row2_backcolor_str);
			XmlElement columnHeadingRow = xml.AppendNewChild(columnHeadingTable, "rowdata");
			XmlElement columnHeadingLegendCell = xml.AppendNewChild(columnHeadingRow, "cell");
			BasicXmlDocument.AppendAttribute(columnHeadingLegendCell, "val", "Business Performance");
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

			// Overall business performance section.
			XmlElement overallBusinessPerformanceHeadingTable = xml.AppendNewChild(rootTable, "table");
			BasicXmlDocument.AppendAttribute(overallBusinessPerformanceHeadingTable, "columns", 1);
			if (showBorder)
			{
				BasicXmlDocument.AppendAttribute(overallBusinessPerformanceHeadingTable, "border_colour", border_colour_str);
			}
			BasicXmlDocument.AppendAttribute(overallBusinessPerformanceHeadingTable, "row_colour_1", row1_backcolor_str);
			BasicXmlDocument.AppendAttribute(overallBusinessPerformanceHeadingTable, "row_colour_2", row2_backcolor_str);
			XmlElement overallBusinessPerformanceHeadingRow = xml.AppendNewChild(overallBusinessPerformanceHeadingTable, "rowdata");
			XmlElement overallBusinessPerformanceHeadingCell = xml.AppendNewChild(overallBusinessPerformanceHeadingRow, "cell");
			BasicXmlDocument.AppendAttribute(overallBusinessPerformanceHeadingCell, "align", header_align_str);
			BasicXmlDocument.AppendAttribute(overallBusinessPerformanceHeadingCell, "colour", header_backcolor_str);
			BasicXmlDocument.AppendAttribute(overallBusinessPerformanceHeadingCell, "textcolour", header_textcolor_str);
			BasicXmlDocument.AppendAttribute(overallBusinessPerformanceHeadingCell, "textstyle", "bold");

			if (showRegioninSpacerRow)
			{
				BasicXmlDocument.AppendAttribute(overallBusinessPerformanceHeadingCell, "val", "Overall");
			}
			else
			{
				BasicXmlDocument.AppendAttribute(overallBusinessPerformanceHeadingCell, "val", "");
			}
			BasicXmlDocument.AppendAttribute(overallBusinessPerformanceHeadingCell, "no_border", "true");

			XmlElement overallBusinessPerformanceResultsTable = xml.AppendNewChild(rootTable, "table");
			BasicXmlDocument.AppendAttribute(overallBusinessPerformanceResultsTable, "columns", 1 + rounds);
			BasicXmlDocument.AppendAttribute(overallBusinessPerformanceResultsTable, "widths", columnWidths);
			if (showBorder)
			{
				BasicXmlDocument.AppendAttribute(overallBusinessPerformanceResultsTable, "border_colour", border_colour_str);
			}
			BasicXmlDocument.AppendAttribute(overallBusinessPerformanceResultsTable, "row_colour_1", row1_backcolor_str);
			BasicXmlDocument.AppendAttribute(overallBusinessPerformanceResultsTable, "row_colour_2", row2_backcolor_str);

			//XmlElement overallBusinessResultsLeaderboardPositionRow = xml.AppendNewChild(overallBusinessPerformanceResultsTable, "rowdata");
			//XmlElement overallBusinessResultsLeaderboardPositionLegend = xml.AppendNewChild(overallBusinessResultsLeaderboardPositionRow, "cell");

			//BasicXmlDocument.AppendAttribute(overallBusinessResultsLeaderboardPositionLegend, "val", "Market Position");
			//BasicXmlDocument.AppendAttribute(overallBusinessResultsLeaderboardPositionLegend, "textcolour", row_textcolor_str);
			//BasicXmlDocument.AppendAttribute(overallBusinessResultsLeaderboardPositionLegend, "no_border", "true");
			//BasicXmlDocument.AppendAttribute(overallBusinessResultsLeaderboardPositionLegend, "align", title_align_str);

			//for (int round = 1; round <= rounds; round++)
			//{
			//  XmlElement overalBusinessResultsLeaderboardPositionCell = xml.AppendNewChild(overallBusinessResultsLeaderboardPositionRow, "cell");
			//  if (round <= scores.Length)
			//  {
			//    BasicXmlDocument.AppendAttribute(overalBusinessResultsLeaderboardPositionCell, "val", "");
			//    BasicXmlDocument.AppendAttribute(overalBusinessResultsLeaderboardPositionCell, "no_border", "true");
			//  }
			//  else
			//  {
			//    BasicXmlDocument.AppendAttribute(overalBusinessResultsLeaderboardPositionCell, "val", "");
			//    BasicXmlDocument.AppendAttribute(overalBusinessResultsLeaderboardPositionCell, "no_border", "true");
			//  }
			//}

			XmlElement overallBusinessResultsBenefitDeliveredRow = xml.AppendNewChild(overallBusinessPerformanceResultsTable, "rowdata");
			XmlElement overallBusinessResultsBenefitDeliveredLegend = xml.AppendNewChild(overallBusinessResultsBenefitDeliveredRow, "cell");
			//BasicXmlDocument.AppendAttribute(overallBusinessResultsBenefitDeliveredLegend, "val", "Benefit delivered [services / demands] ($M)");
			BasicXmlDocument.AppendAttribute(overallBusinessResultsBenefitDeliveredLegend, "val", "Benefit (Services / Demands) ($M)");
			BasicXmlDocument.AppendAttribute(overallBusinessResultsBenefitDeliveredLegend, "textcolour", row_textcolor_str);
			BasicXmlDocument.AppendAttribute(overallBusinessResultsBenefitDeliveredLegend, "no_border", "true");
			BasicXmlDocument.AppendAttribute(overallBusinessResultsBenefitDeliveredLegend, "align", title_align_str);

			for (int round = 1; round <= rounds; round++)
			{
				XmlElement overalBusinessResultsRealizedOpportunityCell = xml.AppendNewChild(overallBusinessResultsBenefitDeliveredRow, "cell");
				if (round <= scores.Length)
				{
					BasicXmlDocument.AppendAttribute(overalBusinessResultsRealizedOpportunityCell, "val",
					                                 CONVERT.Format("{0:0.0} [{1:0.0} / {2:0.0}]",
													                CONVERT.ToPaddedStrWithThousands(scores[round - 1].TotalRealisedOpportunity / 1000000.0, 3),
													                CONVERT.ToPaddedStrWithThousands(scores[round - 1].NewServiceRealisedOpportunity / 1000000.0, 3),
													                CONVERT.ToPaddedStrWithThousands(scores[round - 1].DemandRealisedOpportunity / 1000000.0, 3)));
					BasicXmlDocument.AppendAttribute(overalBusinessResultsRealizedOpportunityCell, "textcolour", row_textcolor_str);
					BasicXmlDocument.AppendAttribute(overalBusinessResultsRealizedOpportunityCell, "no_border", "true");
				}
				else
				{
					BasicXmlDocument.AppendAttribute(overalBusinessResultsRealizedOpportunityCell, "val", "");
					BasicXmlDocument.AppendAttribute(overalBusinessResultsRealizedOpportunityCell, "textcolour", row_textcolor_str);
					BasicXmlDocument.AppendAttribute(overalBusinessResultsRealizedOpportunityCell, "no_border", "true");
				}
			}

			XmlElement overallBusinessResultsNewServiceSpendRow = xml.AppendNewChild(overallBusinessPerformanceResultsTable, "rowdata");
			XmlElement overallBusinessResultsNewServiceSpendLegend = xml.AppendNewChild(overallBusinessResultsNewServiceSpendRow, "cell");
			BasicXmlDocument.AppendAttribute(overallBusinessResultsNewServiceSpendLegend, "val", "New Service Spend ($M)");
			BasicXmlDocument.AppendAttribute(overallBusinessResultsNewServiceSpendLegend, "textcolour", row_textcolor_str);
			BasicXmlDocument.AppendAttribute(overallBusinessResultsNewServiceSpendLegend, "no_border", "true");
			BasicXmlDocument.AppendAttribute(overallBusinessResultsNewServiceSpendLegend, "align", title_align_str);

			for (int round = 1; round <= rounds; round++)
			{
				XmlElement overalBusinessResultsRealizedOpportunityCell = xml.AppendNewChild(overallBusinessResultsNewServiceSpendRow, "cell");
				if (round <= scores.Length)
				{
					BasicXmlDocument.AppendAttribute(overalBusinessResultsRealizedOpportunityCell, "val",
					                                 CONVERT.ToPaddedStrWithThousands(scores[round - 1].TotalNewServiceCost / 1000000.0, 3));
					BasicXmlDocument.AppendAttribute(overalBusinessResultsRealizedOpportunityCell, "textcolour", row_textcolor_str);
					BasicXmlDocument.AppendAttribute(overalBusinessResultsRealizedOpportunityCell, "no_border", "true");
				}
				else
				{
					BasicXmlDocument.AppendAttribute(overalBusinessResultsRealizedOpportunityCell, "val", "");
					BasicXmlDocument.AppendAttribute(overalBusinessResultsRealizedOpportunityCell, "textcolour", row_textcolor_str);
					BasicXmlDocument.AppendAttribute(overalBusinessResultsRealizedOpportunityCell, "no_border", "true");
				}
			}

			XmlElement overallBusinessResultsProfitRow = xml.AppendNewChild(overallBusinessPerformanceResultsTable, "rowdata");
			XmlElement overallBusinessResultsProfitLegend = xml.AppendNewChild(overallBusinessResultsProfitRow, "cell");
			BasicXmlDocument.AppendAttribute(overallBusinessResultsProfitLegend, "val", "Profit / Loss ($M)");
			BasicXmlDocument.AppendAttribute(overallBusinessResultsProfitLegend, "textcolour", row_textcolor_str);
			BasicXmlDocument.AppendAttribute(overallBusinessResultsProfitLegend, "no_border", "true");
			BasicXmlDocument.AppendAttribute(overallBusinessResultsProfitLegend, "align", title_align_str);

			for (int round = 1; round <= rounds; round++)
			{
				XmlElement overalBusinessResultsRealizedOpportunityCell = xml.AppendNewChild(overallBusinessResultsProfitRow, "cell");
				if (round <= scores.Length)
				{
					BasicXmlDocument.AppendAttribute(overalBusinessResultsRealizedOpportunityCell, "val",
																					 CONVERT.ToPaddedStrWithThousands(scores[round - 1].NetValue / 1000000.0, 3));
					BasicXmlDocument.AppendAttribute(overalBusinessResultsRealizedOpportunityCell, "textcolour", row_textcolor_str);
					BasicXmlDocument.AppendAttribute(overalBusinessResultsRealizedOpportunityCell, "no_border", "true");
				}
				else
				{
					BasicXmlDocument.AppendAttribute(overalBusinessResultsRealizedOpportunityCell, "val", "");
					BasicXmlDocument.AppendAttribute(overalBusinessResultsRealizedOpportunityCell, "textcolour", row_textcolor_str);
					BasicXmlDocument.AppendAttribute(overalBusinessResultsRealizedOpportunityCell, "no_border", "true");				
				}
			}

			XmlElement overallBusinessResultsTargetBenefitRow = xml.AppendNewChild(overallBusinessPerformanceResultsTable, "rowdata");
			XmlElement overallBusinessResultsTargetBenefitLegend = xml.AppendNewChild(overallBusinessResultsTargetBenefitRow, "cell");
			BasicXmlDocument.AppendAttribute(overallBusinessResultsTargetBenefitLegend, "val", "Profit Target ($M)");
			BasicXmlDocument.AppendAttribute(overallBusinessResultsTargetBenefitLegend, "textcolour", row_textcolor_str);
			BasicXmlDocument.AppendAttribute(overallBusinessResultsTargetBenefitLegend, "no_border", "true");
			BasicXmlDocument.AppendAttribute(overallBusinessResultsTargetBenefitLegend, "align", title_align_str);

			for (int round = 1; round <= rounds; round++)
			{
				XmlElement overalBusinessResultsTargetBenefitCell = xml.AppendNewChild(overallBusinessResultsTargetBenefitRow, "cell");
				if (round <= scores.Length)
				{
					BasicXmlDocument.AppendAttribute(overalBusinessResultsTargetBenefitCell, "val",
																					 CONVERT.ToPaddedStrWithThousands(scores[round - 1].TargetRevenue / 1000000.0, 3));
					BasicXmlDocument.AppendAttribute(overalBusinessResultsTargetBenefitCell, "textcolour", row_textcolor_str);
					BasicXmlDocument.AppendAttribute(overalBusinessResultsTargetBenefitCell, "no_border", "true");
				}
				else
				{
					BasicXmlDocument.AppendAttribute(overalBusinessResultsTargetBenefitCell, "val", "");
					BasicXmlDocument.AppendAttribute(overalBusinessResultsTargetBenefitCell, "textcolour", row_textcolor_str);
					BasicXmlDocument.AppendAttribute(overalBusinessResultsTargetBenefitCell, "no_border", "true");
				}
			}

			XmlElement overallBusinessResultsTimeToValueRow = xml.AppendNewChild(overallBusinessPerformanceResultsTable, "rowdata");
			XmlElement overallBusinessResultTimeToValueLegend = xml.AppendNewChild(overallBusinessResultsTimeToValueRow, "cell");
			BasicXmlDocument.AppendAttribute(overallBusinessResultTimeToValueLegend, "val", "Time To Value (Trading Periods)");
			BasicXmlDocument.AppendAttribute(overallBusinessResultTimeToValueLegend, "textcolour", row_textcolor_str);
			BasicXmlDocument.AppendAttribute(overallBusinessResultTimeToValueLegend, "no_border", "true");
			BasicXmlDocument.AppendAttribute(overallBusinessResultTimeToValueLegend, "align", title_align_str);

			for (int round = 1; round <= rounds; round++)
			{
				XmlElement overalBusinessResultsTimeToValueCell = xml.AppendNewChild(overallBusinessResultsTimeToValueRow, "cell");
				if ((round <= scores.Length) && ! double.IsNaN(scores[round - 1].TimeToValue))
				{
					BasicXmlDocument.AppendAttribute(overalBusinessResultsTimeToValueCell, "val",
																					 CONVERT.ToPaddedStrWithThousands(scores[round - 1].TimeToValue, 1));
					BasicXmlDocument.AppendAttribute(overalBusinessResultsTimeToValueCell, "textcolour", row_textcolor_str);
					BasicXmlDocument.AppendAttribute(overalBusinessResultsTimeToValueCell, "no_border", "true");
				}
				else
				{
					BasicXmlDocument.AppendAttribute(overalBusinessResultsTimeToValueCell, "val", "");
					BasicXmlDocument.AppendAttribute(overalBusinessResultsTimeToValueCell, "textcolour", row_textcolor_str);
					BasicXmlDocument.AppendAttribute(overalBusinessResultsTimeToValueCell, "no_border", "true");
				}
			}

			// Per-business performance sections.
			foreach (string region in scores[0].Regions)
			{
				// Heading.
				XmlElement businessPerformanceHeadingTable = xml.AppendNewChild(rootTable, "table");
				BasicXmlDocument.AppendAttribute(businessPerformanceHeadingTable, "columns", 1);
				if (showBorder)
				{
					BasicXmlDocument.AppendAttribute(businessPerformanceHeadingTable, "border_colour", border_colour_str);
				}
				BasicXmlDocument.AppendAttribute(businessPerformanceHeadingTable, "row_colour_1", row1_backcolor_str);
				BasicXmlDocument.AppendAttribute(businessPerformanceHeadingTable, "row_colour_2", row2_backcolor_str);
				XmlElement businessPerformanceHeadingRow = xml.AppendNewChild(businessPerformanceHeadingTable, "rowdata");
				XmlElement businessPerformanceHeadingCell = xml.AppendNewChild(businessPerformanceHeadingRow, "cell");
				BasicXmlDocument.AppendAttribute(businessPerformanceHeadingCell, "align", header_align_str);
				BasicXmlDocument.AppendAttribute(businessPerformanceHeadingCell, "colour", header_backcolor_str);
				BasicXmlDocument.AppendAttribute(businessPerformanceHeadingCell, "textcolour", header_textcolor_str);
				BasicXmlDocument.AppendAttribute(businessPerformanceHeadingCell, "textstyle", "bold");
				//BasicXmlDocument.AppendAttribute(businessPerformanceHeadingCell, "val", region +  " business performance");

				if (showRegioninSpacerRow)
				{
					BasicXmlDocument.AppendAttribute(businessPerformanceHeadingCell, "val", region);
				}
				else
				{
					BasicXmlDocument.AppendAttribute(businessPerformanceHeadingCell, "val", "");
				}
				BasicXmlDocument.AppendAttribute(businessPerformanceHeadingCell, "no_border", "true");

				// Results.
				XmlElement businessPerformanceResultsTable = xml.AppendNewChild(rootTable, "table");
				BasicXmlDocument.AppendAttribute(businessPerformanceResultsTable, "columns", 1 + rounds);
				BasicXmlDocument.AppendAttribute(businessPerformanceResultsTable, "widths", columnWidths);
				if (showBorder)
				{
					BasicXmlDocument.AppendAttribute(businessPerformanceResultsTable, "border_colour", border_colour_str);
				}
				BasicXmlDocument.AppendAttribute(businessPerformanceResultsTable, "row_colour_1", row1_backcolor_str);
				BasicXmlDocument.AppendAttribute(businessPerformanceResultsTable, "row_colour_2", row2_backcolor_str);

				XmlElement businessResultsBenefitDeliveredRow = xml.AppendNewChild(businessPerformanceResultsTable, "rowdata");
				XmlElement businessResultsBenefitDeliveredLegend = xml.AppendNewChild(businessResultsBenefitDeliveredRow, "cell");
				//BasicXmlDocument.AppendAttribute(businessResultsBenefitDeliveredLegend, "val", "Benefit delivered [services / demands] ($M)");
				BasicXmlDocument.AppendAttribute(businessResultsBenefitDeliveredLegend, "val", "Benefit (Services / Demands) ($M)");
				BasicXmlDocument.AppendAttribute(businessResultsBenefitDeliveredLegend, "textcolour", row_textcolor_str);
				BasicXmlDocument.AppendAttribute(businessResultsBenefitDeliveredLegend, "no_border", "true");
				BasicXmlDocument.AppendAttribute(businessResultsBenefitDeliveredLegend, "align", title_align_str);

				for (int round = 1; round <= rounds; round++)
				{
					XmlElement businessResultsBenefitDeliveredCell = xml.AppendNewChild(businessResultsBenefitDeliveredRow, "cell");
					if (round <= scores.Length)
					{
						BasicXmlDocument.AppendAttribute(businessResultsBenefitDeliveredCell, "val",
														 CONVERT.Format("{0:0.0} [{1:0.0} / {2:0.0}]",
																		CONVERT.ToPaddedStrWithThousands(scores[round - 1].NameToRegion[region].TotalOpportunityRealised / 1000000.0, 3),
																		CONVERT.ToPaddedStrWithThousands(scores[round - 1].NameToRegion[region].NewServiceOpportunityRealised / 1000000.0, 3),
																		CONVERT.ToPaddedStrWithThousands(scores[round - 1].NameToRegion[region].DemandOpportunityRealised / 1000000.0, 3)));
						BasicXmlDocument.AppendAttribute(businessResultsBenefitDeliveredCell, "textcolour", row_textcolor_str);
						BasicXmlDocument.AppendAttribute(businessResultsBenefitDeliveredCell, "no_border", "true");
					}
					else
					{
						BasicXmlDocument.AppendAttribute(businessResultsBenefitDeliveredCell, "val", "");
						BasicXmlDocument.AppendAttribute(businessResultsBenefitDeliveredCell, "textcolour", row_textcolor_str);
						BasicXmlDocument.AppendAttribute(businessResultsBenefitDeliveredCell, "no_border", "true");
					}
				}

				XmlElement businessResultsNewServiceSpendRow = xml.AppendNewChild(businessPerformanceResultsTable, "rowdata");
				XmlElement businessResultsNewServiceSpendLegend = xml.AppendNewChild(businessResultsNewServiceSpendRow, "cell");
				BasicXmlDocument.AppendAttribute(businessResultsNewServiceSpendLegend, "val", "New Service Spend ($M)");
				BasicXmlDocument.AppendAttribute(businessResultsNewServiceSpendLegend, "textcolour", row_textcolor_str);
				BasicXmlDocument.AppendAttribute(businessResultsNewServiceSpendLegend, "no_border", "true");
				BasicXmlDocument.AppendAttribute(businessResultsNewServiceSpendLegend, "align", title_align_str);

				for (int round = 1; round <= rounds; round++)
				{
					XmlElement businessResultsNewServiceSpendCell = xml.AppendNewChild(businessResultsNewServiceSpendRow, "cell");
					if (round <= scores.Length)
					{
						BasicXmlDocument.AppendAttribute(businessResultsNewServiceSpendCell, "val",
														 CONVERT.ToPaddedStrWithThousands(scores[round - 1].NameToRegion[region].TotalNewServiceCost / 1000000.0, 3));
						BasicXmlDocument.AppendAttribute(businessResultsNewServiceSpendCell, "textcolour", row_textcolor_str);
						BasicXmlDocument.AppendAttribute(businessResultsNewServiceSpendCell, "no_border", "true");
					}
					else
					{
						BasicXmlDocument.AppendAttribute(businessResultsNewServiceSpendCell, "val", "");
						BasicXmlDocument.AppendAttribute(businessResultsNewServiceSpendCell, "textcolour", row_textcolor_str);
						BasicXmlDocument.AppendAttribute(businessResultsNewServiceSpendCell, "no_border", "true");
					}

				}

				XmlElement businessResultsProfitSpendRow = xml.AppendNewChild(businessPerformanceResultsTable, "rowdata");
				XmlElement businessResultsProfitSpendLegend = xml.AppendNewChild(businessResultsProfitSpendRow, "cell");
				BasicXmlDocument.AppendAttribute(businessResultsProfitSpendLegend, "val", "Profit / Loss ($M)");
				BasicXmlDocument.AppendAttribute(businessResultsProfitSpendLegend, "textcolour", row_textcolor_str);
				BasicXmlDocument.AppendAttribute(businessResultsProfitSpendLegend, "no_border", "true");
				BasicXmlDocument.AppendAttribute(businessResultsProfitSpendLegend, "align", title_align_str);

				for (int round = 1; round <= rounds; round++)
				{
					XmlElement businessResultsProfitSpendCell = xml.AppendNewChild(businessResultsProfitSpendRow, "cell");
					if (round <= scores.Length)
					{
						BasicXmlDocument.AppendAttribute(businessResultsProfitSpendCell, "val",
														 CONVERT.ToPaddedStrWithThousands(scores[round - 1].NameToRegion[region].NetValue / 1000000.0, 3));
						BasicXmlDocument.AppendAttribute(businessResultsProfitSpendCell, "textcolour", row_textcolor_str);
						BasicXmlDocument.AppendAttribute(businessResultsProfitSpendCell, "no_border", "true");
					}
					else
					{
						BasicXmlDocument.AppendAttribute(businessResultsProfitSpendCell, "val","");
						BasicXmlDocument.AppendAttribute(businessResultsProfitSpendCell, "textcolour", row_textcolor_str);
						BasicXmlDocument.AppendAttribute(businessResultsProfitSpendCell, "no_border", "true");
					}
				}

				XmlElement businessResultsTargetBenefitRow = xml.AppendNewChild(businessPerformanceResultsTable, "rowdata");
				XmlElement businessResultsTargetBenefitLegend = xml.AppendNewChild(businessResultsTargetBenefitRow, "cell");
				BasicXmlDocument.AppendAttribute(businessResultsTargetBenefitLegend, "val", "Profit Target ($M)");
				BasicXmlDocument.AppendAttribute(businessResultsTargetBenefitLegend, "textcolour", row_textcolor_str);
				BasicXmlDocument.AppendAttribute(businessResultsTargetBenefitLegend, "no_border", "true");
				BasicXmlDocument.AppendAttribute(businessResultsTargetBenefitLegend, "align", title_align_str);

				for (int round = 1; round <= rounds; round++)
				{
					XmlElement businessResultsTargetBenefitCell = xml.AppendNewChild(businessResultsTargetBenefitRow, "cell");
					if (round <= scores.Length)
					{
						BasicXmlDocument.AppendAttribute(businessResultsTargetBenefitCell, "val", CONVERT.ToPaddedStrWithThousands(scores[round - 1].NameToRegion[region].TargetRevenue / 1000000.0, 3));
						BasicXmlDocument.AppendAttribute(businessResultsTargetBenefitCell, "textcolour", row_textcolor_str);
						BasicXmlDocument.AppendAttribute(businessResultsTargetBenefitCell, "no_border", "true");
					}
					else
					{
						BasicXmlDocument.AppendAttribute(businessResultsTargetBenefitCell, "val", "");
						BasicXmlDocument.AppendAttribute(businessResultsTargetBenefitCell, "textcolour", row_textcolor_str);
						BasicXmlDocument.AppendAttribute(businessResultsTargetBenefitCell, "no_border", "true");
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