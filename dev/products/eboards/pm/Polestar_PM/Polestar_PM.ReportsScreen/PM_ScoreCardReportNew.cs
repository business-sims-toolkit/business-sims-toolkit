using System;
using System.Collections;

using System.Drawing;

using System.IO;
using System.Text;
using System.Xml;

using Network;
using GameManagement;


namespace Polestar_PM.ReportsScreen
{
	public class PM_ScoreCardReportNew
	{
		string headingBackColour = "0,66,109";
		string headingForeColour = "255,255,255";

		ReportBuilder.SupportSpendOverrides spendOverrides;

		public PM_ScoreCardReportNew()
		{
		}

		public string BuildReport(NetworkProgressionGameFile gameFile, NodeTree model, ReportBuilder.SupportSpendOverrides spendOverrides)
		{
			LibCore.BasicXmlDocument doc = LibCore.BasicXmlDocument.Create();

			this.spendOverrides = spendOverrides;

			int rounds = CoreUtils.SkinningDefs.TheInstance.GetIntData("roundcount", 2);
			int LastRoundPlayed = gameFile.LastRoundPlayed;

			int rowHeight = CoreUtils.SkinningDefs.TheInstance.GetIntData("table_row_height", 19);

			int columns = 2 + rounds;
			string columnsString = LibCore.CONVERT.ToStr(columns);

			double titleColumnWidth = 0.3;
			double columnWidth = (1.0 - titleColumnWidth) / (columns - 1);
			string widthsString = LibCore.CONVERT.ToStr(titleColumnWidth);
			for (int i = 1; i < columns; i++)
			{
				widthsString += "," + LibCore.CONVERT.ToStr(columnWidth);
			}

			NodeTree[] roundModels = new NodeTree[rounds];
			Node[] budgetNodes = new Node[rounds];
			Node[] roundResultsNodes = new Node[rounds];
			Node[] operationalResultsNodes = new Node[rounds];
			Node[] projectsResultsNodes = new Node[rounds];
			Node[] financialResultsNodes = new Node[rounds];
			Node[] resourcesResultsNodes = new Node[rounds];
			Node[] marketInfoNodes = new Node[rounds];
			for (int round = 0; round < rounds; round++)
			{
				roundModels[round] = gameFile.GetNetworkModel(1 + round, GameManagement.GameFile.GamePhase.OPERATIONS);
				budgetNodes[round] = roundModels[round].GetNamedNode("pmo_budget");
				roundResultsNodes[round] = roundModels[round].GetNamedNode("round_results");
				operationalResultsNodes[round] = roundModels[round].GetNamedNode("operational_results");
				projectsResultsNodes[round] = roundModels[round].GetNamedNode("projects_results");
				financialResultsNodes[round] = roundModels[round].GetNamedNode("fin_results");
				resourcesResultsNodes[round] = roundModels[round].GetNamedNode("resources_results");
				marketInfoNodes[round] = roundModels[round].GetNamedNode("market_info");
			}

			XmlElement table = CreateChild(doc, "table");
			table.SetAttribute("columns", "1");
			table.SetAttribute("rowheight", LibCore.CONVERT.ToStr(rowHeight));
			table.SetAttribute("border_colour", CoreUtils.SkinningDefs.TheInstance.GetData("table_border_colour", "211,211,211"));
			table.SetAttribute("row_colour_1", CoreUtils.SkinningDefs.TheInstance.GetData("table_row_colour", "255,255,255"));
			table.SetAttribute("row_colour_2", CoreUtils.SkinningDefs.TheInstance.GetData("table_row_colour_alternate", "255,255,255"));

			XmlElement headerTable = CreateChild(table, "table");
			headerTable.SetAttribute("columns", columnsString);
			headerTable.SetAttribute("widths", widthsString);
			headerTable.SetAttribute("rowheight", LibCore.CONVERT.ToStr(rowHeight));
			headerTable.SetAttribute("border_colour", CoreUtils.SkinningDefs.TheInstance.GetData("table_border_colour", "211,211,211"));
			headerTable.SetAttribute("row_colour_1", CoreUtils.SkinningDefs.TheInstance.GetData("table_row_colour", "255,255,255"));
			headerTable.SetAttribute("row_colour_2", CoreUtils.SkinningDefs.TheInstance.GetData("table_row_colour_alternate", "255,255,255"));

			XmlElement headerRow = CreateChild(headerTable, "rowdata");
			headerRow.SetAttribute("colour", "85,183,221");
			CreateCellChild(headerRow, "");
			for (int round = 1; round <= CoreUtils.SkinningDefs.TheInstance.GetIntData("roundcount", 3); round++)
			{
				XmlElement cell = CreateCellChild(headerRow, "Round " + LibCore.CONVERT.ToStr(round), ContentAlignment.MiddleRight, FontStyle.Bold);
				cell.SetAttribute("textcolour", "255,255,255");
			}
			XmlElement totalCell = CreateCellChild(headerRow, "Total", ContentAlignment.MiddleRight, FontStyle.Bold);
			totalCell.SetAttribute("textcolour", "255,255,255");

			XmlElement roundSectionRow = CreateChild(table, "rowdata");
			XmlElement roundSectionCell = CreateCellChild(roundSectionRow, "Business Performance", ContentAlignment.MiddleLeft, FontStyle.Bold);
			roundSectionCell.SetAttribute("colour", headingBackColour);
			roundSectionCell.SetAttribute("textcolour", headingForeColour);

			XmlElement roundPerformanceTable1 = CreateChild(table, "table");
			roundPerformanceTable1.SetAttribute("columns", columnsString);
			roundPerformanceTable1.SetAttribute("widths", widthsString);
			roundPerformanceTable1.SetAttribute("rowheight", LibCore.CONVERT.ToStr(rowHeight));
			roundPerformanceTable1.SetAttribute("border_colour", CoreUtils.SkinningDefs.TheInstance.GetData("table_border_colour", "211,211,211"));
			roundPerformanceTable1.SetAttribute("row_colour_1", CoreUtils.SkinningDefs.TheInstance.GetData("table_row_colour", "255,255,255"));
			roundPerformanceTable1.SetAttribute("row_colour_2", CoreUtils.SkinningDefs.TheInstance.GetData("table_row_colour", "255,255,255"));
			
			XmlElement roundPerformanceRow1 = CreateChild(roundPerformanceTable1, "rowdata");
			CreateCellChild(roundPerformanceRow1, "  Market Position", ContentAlignment.MiddleLeft);
			CreateNumericRow(roundPerformanceRow1, operationalResultsNodes, "market_position", LastRoundPlayed, false, false);

			XmlElement roundPerformanceTable2 = CreateChild(table, "table");
			roundPerformanceTable2.SetAttribute("columns", columnsString);
			roundPerformanceTable2.SetAttribute("widths", widthsString);
			roundPerformanceTable2.SetAttribute("rowheight", LibCore.CONVERT.ToStr(rowHeight));
			roundPerformanceTable2.SetAttribute("border_colour", CoreUtils.SkinningDefs.TheInstance.GetData("table_border_colour", "211,211,211"));
			roundPerformanceTable2.SetAttribute("row_colour_1", CoreUtils.SkinningDefs.TheInstance.GetData("table_row_colour_alternate", "255,255,255"));
			roundPerformanceTable2.SetAttribute("row_colour_2", CoreUtils.SkinningDefs.TheInstance.GetData("table_row_colour_alternate", "255,255,255"));

			XmlElement financialPerformanceRow1 = CreateChild(roundPerformanceTable2, "rowdata");
			CreateCellChild(financialPerformanceRow1, " Transaction Revenue Generated", ContentAlignment.MiddleLeft);
			CreateNumericRow(financialPerformanceRow1, financialResultsNodes, "total_revenue", LastRoundPlayed, true, true);

			XmlElement financialPerformanceRow99 = CreateChild(roundPerformanceTable2, "rowdata");
			CreateCellChild(financialPerformanceRow99, "  Cost Reduction Delivered", ContentAlignment.MiddleLeft);
			CreateNumericRowWithPercentage(financialPerformanceRow99, roundResultsNodes, "cost_reduction_achieved", marketInfoNodes, "cost_reduction", LastRoundPlayed, true, true);

			XmlElement financialPerformanceRow98 = CreateChild(roundPerformanceTable2, "rowdata");
			CreateCellChild(financialPerformanceRow98, "  Total Business Benefit", ContentAlignment.MiddleLeft);
			CreateNumericRow(financialPerformanceRow98, financialResultsNodes, "total_business_benefit", LastRoundPlayed, true, true);

			XmlElement roundPerformanceTable3 = CreateChild(table, "table");
			roundPerformanceTable3.SetAttribute("columns", columnsString);
			roundPerformanceTable3.SetAttribute("widths", widthsString);
			roundPerformanceTable3.SetAttribute("rowheight", LibCore.CONVERT.ToStr(rowHeight));
			roundPerformanceTable3.SetAttribute("border_colour", CoreUtils.SkinningDefs.TheInstance.GetData("table_border_colour", "211,211,211"));
			roundPerformanceTable3.SetAttribute("row_colour_1", CoreUtils.SkinningDefs.TheInstance.GetData("table_row_colour", "255,255,255"));
			roundPerformanceTable3.SetAttribute("row_colour_2", CoreUtils.SkinningDefs.TheInstance.GetData("table_row_colour", "255,255,255"));

			XmlElement financialPerformanceRow2 = CreateChild(roundPerformanceTable3, "rowdata");
			CreateCellChild(financialPerformanceRow2, "  PMO Budget", ContentAlignment.MiddleLeft);
			CreateNumericRow(financialPerformanceRow2, budgetNodes, "budget_allowed", LastRoundPlayed, true, true);

			XmlElement financialPerformanceRow3 = CreateChild(roundPerformanceTable3, "rowdata");
			CreateCellChild(financialPerformanceRow3, "  Fixed Costs", ContentAlignment.MiddleLeft);
			CreateNumericRow(financialPerformanceRow3, financialResultsNodes, "fixed_costs", LastRoundPlayed, true, true);

			XmlElement financialPerformanceRow4 = CreateChild(roundPerformanceTable3, "rowdata");
			CreateCellChild(financialPerformanceRow4, "  Operational Fines", ContentAlignment.MiddleLeft);
			CreateNumericRow(financialPerformanceRow4, operationalResultsNodes, "fines", LastRoundPlayed, true, true);

			XmlElement financialPerformanceRow5 = CreateChild(roundPerformanceTable3, "rowdata");
			CreateCellChild(financialPerformanceRow5, "  Additional Fines", ContentAlignment.MiddleLeft);
			CreateEditableNumericRow(financialPerformanceRow5, operationalResultsNodes, "additional_fines", LastRoundPlayed, true, true);

			XmlElement roundPerformanceTable4 = CreateChild(table, "table");
			roundPerformanceTable4.SetAttribute("columns", columnsString);
			roundPerformanceTable4.SetAttribute("widths", widthsString);
			roundPerformanceTable4.SetAttribute("rowheight", LibCore.CONVERT.ToStr(rowHeight));
			roundPerformanceTable4.SetAttribute("border_colour", CoreUtils.SkinningDefs.TheInstance.GetData("table_border_colour", "211,211,211"));
			roundPerformanceTable4.SetAttribute("row_colour_1", CoreUtils.SkinningDefs.TheInstance.GetData("table_row_colour_alternate", "255,255,255"));
			roundPerformanceTable4.SetAttribute("row_colour_2", CoreUtils.SkinningDefs.TheInstance.GetData("table_row_colour_alternate", "255,255,255"));

			XmlElement financialPerformanceRow6 = CreateChild(roundPerformanceTable4, "rowdata");
			CreateCellChild(financialPerformanceRow6, "  Profit / Loss", ContentAlignment.MiddleLeft);

			// Build a set of nodes that incorporate any fudge factors added outwith the model.
			NodeTree tempModel = new NodeTree ("<network />");
			Node profitsNode = new Node (tempModel, tempModel.Root, "profits");
			Node [] profitNodes = new Node [financialResultsNodes.Length];
			for (int round = 0; round < profitNodes.Length; round++)
			{
				double originalProfit = financialResultsNodes[round].GetDoubleAttribute("profitloss", 0);
				string fineString;
				spendOverrides.GetOverride(1 + round, "additional_fines", out fineString);
				double fine = LibCore.CONVERT.ParseDoubleSafe(fineString, 0);
				profitNodes[round] = new Node (profitsNode, "profit", "", new AttributeValuePair("profitloss", originalProfit - fine));
			}
			CreateNumericRow(financialPerformanceRow6, profitNodes, "profitloss", LastRoundPlayed, true, true);

			XmlElement programSectionRow = CreateChild(table, "rowdata");
			XmlElement programSectionCell = CreateCellChild(programSectionRow, "PMO Performance", ContentAlignment.MiddleLeft, FontStyle.Bold);
			programSectionCell.SetAttribute("colour", headingBackColour);
			programSectionCell.SetAttribute("textcolour", headingForeColour);

			XmlElement programPerformanceTable1 = CreateChild(table, "table");
			programPerformanceTable1.SetAttribute("columns", columnsString);
			programPerformanceTable1.SetAttribute("widths", widthsString);
			programPerformanceTable1.SetAttribute("rowheight", LibCore.CONVERT.ToStr(rowHeight));
			programPerformanceTable1.SetAttribute("border_colour", CoreUtils.SkinningDefs.TheInstance.GetData("table_border_colour", "211,211,211"));
			programPerformanceTable1.SetAttribute("row_colour_1", CoreUtils.SkinningDefs.TheInstance.GetData("table_row_colour", "255,255,255"));
			programPerformanceTable1.SetAttribute("row_colour_2", CoreUtils.SkinningDefs.TheInstance.GetData("table_row_colour", "255,255,255"));

			XmlElement projectPerformanceRow3 = CreateChild(programPerformanceTable1, "rowdata");
			CreateCellChild(projectPerformanceRow3, "  Completed on Time", ContentAlignment.MiddleLeft);
			CreateNumericRow(projectPerformanceRow3, projectsResultsNodes, "project_completed", LastRoundPlayed, true, false);

			XmlElement programPerformanceTable2 = CreateChild(table, "table");
			programPerformanceTable2.SetAttribute("columns", columnsString);
			programPerformanceTable2.SetAttribute("widths", widthsString);
			programPerformanceTable2.SetAttribute("rowheight", LibCore.CONVERT.ToStr(rowHeight));
			programPerformanceTable2.SetAttribute("border_colour", CoreUtils.SkinningDefs.TheInstance.GetData("table_border_colour", "211,211,211"));
			programPerformanceTable2.SetAttribute("row_colour_1", CoreUtils.SkinningDefs.TheInstance.GetData("table_row_colour_alternate", "255,255,255"));
			programPerformanceTable2.SetAttribute("row_colour_2", CoreUtils.SkinningDefs.TheInstance.GetData("table_row_colour_alternate", "255,255,255"));

			XmlElement programPerformanceRow1 = CreateChild(programPerformanceTable2, "rowdata");
			CreateCellChild(programPerformanceRow1, "  PMO Budget", ContentAlignment.MiddleLeft);
			CreateNumericRow(programPerformanceRow1, budgetNodes, "budget_allowed", LastRoundPlayed, true, true);

			XmlElement programPerformanceRow2 = CreateChild(programPerformanceTable2, "rowdata");
			CreateCellChild(programPerformanceRow2, "  PMO Spend", ContentAlignment.MiddleLeft);
			CreateNumericRow(programPerformanceRow2, budgetNodes, "budget_spent", LastRoundPlayed, true, true);

			XmlElement programPerformanceRow3 = CreateChild(programPerformanceTable2, "rowdata");
			CreateCellChild(programPerformanceRow3, "  PMO Underspend", ContentAlignment.MiddleLeft);
			CreateNumericRow(programPerformanceRow3, budgetNodes, "budget_left", LastRoundPlayed, true, true);

			XmlElement programPerformanceTable3 = CreateChild(table, "table");
			programPerformanceTable3.SetAttribute("columns", columnsString);
			programPerformanceTable3.SetAttribute("widths", widthsString);
			programPerformanceTable3.SetAttribute("rowheight", LibCore.CONVERT.ToStr(rowHeight));
			programPerformanceTable3.SetAttribute("border_colour", CoreUtils.SkinningDefs.TheInstance.GetData("table_border_colour", "211,211,211"));
			programPerformanceTable3.SetAttribute("row_colour_1", CoreUtils.SkinningDefs.TheInstance.GetData("table_row_colour", "255,255,255"));
			programPerformanceTable3.SetAttribute("row_colour_2", CoreUtils.SkinningDefs.TheInstance.GetData("table_row_colour", "255,255,255"));

			//XmlElement projectPerformanceRow1 = CreateChild(projectPerformanceTable, "rowdata");
			//CreateCellChild(projectPerformanceRow1, "  Cost Reduction", ContentAlignment.MiddleLeft);
			//CreateNumericRowWithPercentage(projectPerformanceRow1, roundResultsNodes, "cost_reduction_achieved", marketInfoNodes, "cost_reduction", LastRoundPlayed, true, true);

			//XmlElement projectPerformanceRow2 = CreateChild(projectPerformanceTable, "rowdata");
			//CreateCellChild(projectPerformanceRow2, "  Cost Avoidance", ContentAlignment.MiddleLeft);
			//CreateNumericRowWithPercentage(projectPerformanceRow2, roundResultsNodes, "cost_avoidance_achieved", marketInfoNodes, "cost_avoidance", LastRoundPlayed, true, true);

			XmlElement projectPerformanceRow4 = CreateChild(programPerformanceTable3, "rowdata");
			CreateCellChild(projectPerformanceRow4, "  Number of Internal Days Tasked", ContentAlignment.MiddleLeft);
			CreateNumericRow(projectPerformanceRow4, resourcesResultsNodes, "int_tasked_days", LastRoundPlayed, true, false);

			XmlElement projectPerformanceRow5 = CreateChild(programPerformanceTable3, "rowdata");
			CreateCellChild(projectPerformanceRow5, "  Number of Internal Days Wasted", ContentAlignment.MiddleLeft);
			CreateNumericRowWithPercentage(projectPerformanceRow5, resourcesResultsNodes, "int_wasted_days", "int_tasked_days", LastRoundPlayed, true, false);

			XmlElement projectPerformanceRow6 = CreateChild(programPerformanceTable3, "rowdata");
			CreateCellChild(projectPerformanceRow6, "  Cost of Internal Days Wasted", ContentAlignment.MiddleLeft);
			CreateNumericRow(projectPerformanceRow6, resourcesResultsNodes, "int_wasted_cost", LastRoundPlayed, true, true);

			XmlElement projectPerformanceRow7 = CreateChild(programPerformanceTable3, "rowdata");
			CreateCellChild(projectPerformanceRow7, "  Number of Contractor Days Tasked", ContentAlignment.MiddleLeft);
			CreateNumericRow(projectPerformanceRow7, resourcesResultsNodes, "ext_tasked_days", LastRoundPlayed, true, false);

			XmlElement projectPerformanceRow8 = CreateChild(programPerformanceTable3, "rowdata");
			CreateCellChild(projectPerformanceRow8, "  Number of Contractor Days Wasted", ContentAlignment.MiddleLeft);
			CreateNumericRowWithPercentage(projectPerformanceRow8, resourcesResultsNodes, "ext_wasted_days", "ext_tasked_days", LastRoundPlayed, true, false);

			XmlElement projectPerformanceRow9 = CreateChild(programPerformanceTable3, "rowdata");
			CreateCellChild(projectPerformanceRow9, "  Cost of Contractor Days Wasted", ContentAlignment.MiddleLeft);
			CreateNumericRow(projectPerformanceRow9, resourcesResultsNodes, "ext_wasted_cost", LastRoundPlayed, true, true);

			XmlElement projectPerformanceRow10 = CreateChild(programPerformanceTable3, "rowdata");
			CreateCellChild(projectPerformanceRow10, "  Total Cost of Wasted Resource", ContentAlignment.MiddleLeft);
			CreateNumericRow(projectPerformanceRow10, resourcesResultsNodes, "total_wasted_cost", LastRoundPlayed, true, true);

			string filename = gameFile.GetRoundFile(gameFile.LastRoundPlayed, "ScoreCardReport.xml", GameFile.GamePhase.OPERATIONS);
			doc.Save(filename);

			return filename;
		}

		XmlElement CreateChild(XmlDocument doc, string name)
		{
			XmlElement element = doc.CreateElement(name);
			doc.AppendChild(element);

			return element;
		}

		XmlElement CreateChild(XmlNode parent, string name)
		{
			XmlElement element = parent.OwnerDocument.CreateElement(name);
			parent.AppendChild(element);

			return element;
		}

		XmlElement CreateCellChild (XmlNode parent, string content)
		{
			return CreateCellChild(parent, content, "");
		}

		XmlElement CreateCellChild(XmlNode parent, string content, string costName)
		{
			return CreateCellChild(parent, content, ContentAlignment.MiddleCenter, FontStyle.Regular, costName);
		}

		XmlElement CreateCellChild(XmlNode parent, string content, ContentAlignment alignment, string costName)
		{
			return CreateCellChild(parent, content, alignment, FontStyle.Regular, costName);
		}

		XmlElement CreateCellChild (XmlNode parent, string content, ContentAlignment alignment)
		{
			return CreateCellChild(parent, content, alignment, "");
		}

		XmlElement CreateCellChild (XmlNode parent, string content, ContentAlignment alignment, FontStyle style)
		{
			return CreateCellChild(parent, content, alignment, style, "");
		}

		XmlElement CreateCellChild(XmlNode parent, string content, ContentAlignment alignment, FontStyle style, string costName)
		{
			XmlElement element = parent.OwnerDocument.CreateElement("cell");
			parent.AppendChild(element);
			element.SetAttribute("val", content);

			string alignString = "";
			switch (alignment)
			{
				case ContentAlignment.BottomCenter:
				case ContentAlignment.MiddleCenter:
				case ContentAlignment.TopCenter:
					alignString = "";
					break;

				case ContentAlignment.BottomLeft:
				case ContentAlignment.MiddleLeft:
				case ContentAlignment.TopLeft:
					alignString = "left";
					break;

				case ContentAlignment.BottomRight:
				case ContentAlignment.MiddleRight:
				case ContentAlignment.TopRight:
					alignString = "right";
					break;
			}

			if (alignString != "")
			{
				element.SetAttribute("align", alignString);
			}

			if (style == FontStyle.Bold)
			{
				element.SetAttribute("textstyle", "bold");
			}

			if (costName != "")
			{
				element.SetAttribute("edit", "true");
				element.SetAttribute("cellname", costName);
			}

			return element;
		}

		XmlElement CreateCellChild (XmlNode parent, int content)
		{
			return CreateCellChild(parent, content, "");
		}

		XmlElement CreateCellChild(XmlNode parent, int content, string costName)
		{
			XmlElement child = CreateCellChild(parent, LibCore.CONVERT.ToStr(content) + " ");
			child.SetAttribute("align", "right");

			if (costName != "")
			{
				child.SetAttribute("edit", "true");
				child.SetAttribute("cellname", costName);
			}

			return child;
		}

		string FormatThousands(int a)
		{
			if (a < 0)
			{
				return "(" + FormatThousands(Math.Abs(a)) + ")";
			}

			string raw = LibCore.CONVERT.ToStr(a);

			StringBuilder builder = new StringBuilder("");
			int digits = 0;
			for (int character = raw.Length - 1; character >= 0; character--)
			{
				builder.Insert(0, raw[character]);
				digits++;

				if (((digits % 3) == 0) && (character > 0))
				{
					builder.Insert(0, ",");
				}
			}

			return builder.ToString();
		}

		string FormatMoney(int a)
		{
			return "$" + FormatThousands(a);
		}

		XmlElement CreateMoneyCellChild (XmlNode parent, int content)
		{
			return CreateMoneyCellChild(parent, content, "");
		}

		XmlElement CreateMoneyCellChild(XmlNode parent, int content, string costName)
		{
			XmlElement child = CreateCellChild(parent, FormatMoney(content) + " ");
			child.SetAttribute("align", "right");

			if (costName != "")
			{
				child.SetAttribute("edit", "true");
				child.SetAttribute("cellname", costName);
			}

			return child;
		}

		void CreateNumericRow (XmlElement row, Node[] results, string attribute,
				int lastRoundPlayed, bool showTotal, bool isMoney)
		{
			double total = 0;

			for (int round = 0; round < results.Length; round++)
			{
				if ((round < lastRoundPlayed) && (results[round] != null))
				{
					double val = results[round].GetDoubleAttribute(attribute, 0);
					total += val;

					if (isMoney)
					{
						CreateMoneyCellChild(row, (int) val);
					}
					else
					{
						CreateCellChild(row, (int)val);
					}
				}
				else
				{
					CreateCellChild(row, "");
				}
			}

			if (showTotal)
			{
				if (isMoney)
				{
					CreateMoneyCellChild(row, (int)total);
				}
				else
				{
					CreateCellChild(row, (int)total);
				}
			}
			else
			{
				CreateCellChild(row, "");
			}
		}

		void CreateEditableNumericRow (XmlElement row, Node[] results, string attribute,
				int lastRoundPlayed, bool showTotal, bool isMoney)
		{
			double total = 0;

			for (int round = 0; round < results.Length; round++)
			{
				if ((round < lastRoundPlayed) && (results[round] != null))
				{
					string stringValue;
					spendOverrides.GetOverride(1 + round, attribute, out stringValue);

					double val = LibCore.CONVERT.ParseDoubleSafe(stringValue, 0);
					total += val;

					if (isMoney)
					{
						CreateMoneyCellChild(row, (int) val, LibCore.CONVERT.Format("{0}|{1}", attribute, 1 + round));
					}
					else
					{
						CreateCellChild(row, (int) val, LibCore.CONVERT.Format("{0}|{1}", attribute, 1 + round));
					}
				}
				else
				{
					CreateCellChild(row, "");
				}
			}

			if (showTotal)
			{
				if (isMoney)
				{
					CreateMoneyCellChild(row, (int) total);
				}
				else
				{
					CreateCellChild(row, (int) total);
				}
			}
			else
			{
				CreateCellChild(row, "", attribute);
			}
		}

		void CreateNumericRowWithPercentage(XmlElement row,
			Node[] results, string attribute,
			string referenceAttribute,
			int lastRoundPlayed,
			bool showTotal, bool isMoney)
		{
			CreateNumericRowWithPercentage(row, results, attribute, results, referenceAttribute, lastRoundPlayed, showTotal, isMoney);
		}

		void CreateNumericRowWithPercentage(XmlElement row,
			Node[] results, string attribute,
			Node[] referenceResults, string referenceAttribute,
			int lastRoundPlayed,
			bool showTotal, bool isMoney)
		{
			double total = 0;
			double referenceTotal = 0;
			XmlElement cell = null;

			for (int round = 0; round < results.Length; round++)
			{
				if ((round < lastRoundPlayed) && (results[round] != null))
				{
					double val = results[round].GetDoubleAttribute(attribute, 0);
					double reference = referenceResults[round].GetDoubleAttribute(referenceAttribute, 0);
					total += val;
					referenceTotal += reference;

					if (isMoney)
					{
						cell = CreateMoneyCellChild(row, (int)val);
					}
					else
					{
						cell = CreateCellChild(row, (int)val);
					}

					if (reference != 0)
					{
						cell.Attributes["val"].Value += " (" + LibCore.CONVERT.ToStr((int)(100 * val / reference)) + "%)";
					}
				}
				else
				{
					CreateCellChild(row, "");
				}
			}

			if (showTotal)
			{
				if (isMoney)
				{
					cell = CreateMoneyCellChild(row, (int)total);
				}
				else
				{
					cell = CreateCellChild(row, (int)total);
				}

				if (referenceTotal != 0)
				{
					cell.Attributes["val"].Value += " (" + LibCore.CONVERT.ToStr((int)(100 * total / referenceTotal)) + "%)";
				}
			}
			else
			{
				CreateCellChild(row, "");
			}
		}
	}
}
