using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Xml;

using CoreUtils;
using GameManagement;
using LibCore;

namespace ReportBuilder
{
    public abstract class BaseScorecardReport<T> where T : RoundScores
    {
        protected NetworkProgressionGameFile gameFile;

        readonly int numRounds;
        readonly int numColumns;
        readonly string columnWidths;

        readonly bool noBorder;

        readonly Color borderColour;
        readonly Color rowColour1;
        readonly Color rowColour2;
        readonly Color textColour;
        readonly Color columnHeadingBackColour;
        readonly Color columnHeadingTextColour;
        readonly Color sectionTitleBackColour;
        readonly Color sectionTitleTextColour;

        protected int rowHeight;

        protected int numRows;

        protected BasicXmlDocument xml;
        protected XmlElement root;

        protected readonly List<T> roundScores;

        readonly int numFigureColumns;
        readonly int totalColumn;
        readonly bool includeTotalColumn;
        
        public int Height
        {
            get;
            protected set;
        }
        

        protected BaseScorecardReport(NetworkProgressionGameFile gameFile, List<T> roundScores, bool includeTotalColumn = true)
        {
            this.gameFile = gameFile;
            this.roundScores = roundScores;
            this.includeTotalColumn = includeTotalColumn;

            numRounds = SkinningDefs.TheInstance.GetIntData("roundcount", 5);

            const double legendWidth = 0.4;
            
            numFigureColumns = (includeTotalColumn) ? numRounds + 1 : numRounds;
            totalColumn = (includeTotalColumn) ? numRounds + 1 : -1;
            
            numColumns = numFigureColumns + 1;
            var figureWidth = (1 - legendWidth) / numFigureColumns;
            columnWidths = CONVERT.ToStr(legendWidth) +
                           string.Join("," + CONVERT.ToStr(figureWidth), new string[numColumns]);

            borderColour = SkinningDefs.TheInstance.GetColorData("scorecard_border_colour", Color.FromArgb(211, 211, 211));
            noBorder = SkinningDefs.TheInstance.GetBoolData("scorecard_no_border", true);

            rowColour1 = SkinningDefs.TheInstance.GetColorData("scorecard_row_one_colour", Color.White);
            rowColour2 = SkinningDefs.TheInstance.GetColorData("scorecard_row_two_colour", Color.White);
            textColour = SkinningDefs.TheInstance.GetColorData("scorecard_text_colour", Color.Orange);

            columnHeadingBackColour = SkinningDefs.TheInstance.GetColorData("scorecard_column_heading_back_colour", Color.White);
            columnHeadingTextColour = SkinningDefs.TheInstance.GetColorData("scorecard_column_heading_text_colour", Color.Black);

            sectionTitleBackColour = SkinningDefs.TheInstance.GetColorData("report_titles_back_colour", Color.Black);
            sectionTitleTextColour = SkinningDefs.TheInstance.GetColorData("scorecard_section_title_text_colour", Color.White);

	        rowHeight = GetRowHeight();

            xml = BasicXmlDocument.Create();

            root = xml.AppendNewChild("scorecard");
            root.AppendAttribute("columns", 1);
            root.AppendAttribute("rowheight", rowHeight);
            root.AppendAttribute("no_border", noBorder);
            root.AppendAttribute("row_colour_1", rowColour1);
            root.AppendAttribute("row_colour_2", rowColour2);
        }

	    protected virtual int GetRowHeight ()
	    {
			return SkinningDefs.TheInstance.GetIntData("table_row_height", 25);
		}

        public string CreateReportAndFilename ()
        {
            GenerateReport();

            CalculatePreferredHeight();

            var filename = GetFilename();
            xml.Save(filename);
            
            return GetFilename();
        }

        [Obsolete("BuildReport is deprecated and has been replaced by CreateReportAndFilename. Derived classes are also required to implement the GenerateReport and GetFilename methods. These are effectively what their BuildReport methods should be now.")]
        public abstract string BuildReport ();

        protected abstract void GenerateReport();

        protected abstract string GetFilename ();
        
        protected void CalculatePreferredHeight ()
        {
            Height = numRows * rowHeight;
        }

        protected void AddColumnHeadings()
        {
            var columnHeadingTable = root.AppendNewChild("table");
            columnHeadingTable.AppendAttribute("columns", numColumns);
            columnHeadingTable.AppendAttribute("widths", columnWidths);

            if (! noBorder)
            {
                columnHeadingTable.AppendAttribute("border_colour", borderColour);
            }

            columnHeadingTable.AppendAttribute("row_colour_1", rowColour1);
            columnHeadingTable.AppendAttribute("row_colour_2", rowColour2);

            // Create the Column headers
            // (Blank) | Round 1 | Round 2 | ... Round N (| Total)


            var columnHeadingRow = columnHeadingTable.AppendNewChild("rowdata");

            AddHeadingCell(columnHeadingRow, string.Empty, columnHeadingBackColour, columnHeadingTextColour, "center");
            
            for (var round = 1; round <= numRounds; round++)
            {
	            var title = $"Round {round}";
	            if (SkinningDefs.TheInstance.GetBoolData("scorecard_headings_uppercase", false))
	            {
		            title = title.ToUpper();
	            }

				AddHeadingCell(columnHeadingRow, title, columnHeadingBackColour, columnHeadingTextColour, "center");
            }

            if (includeTotalColumn)
            {
	            var title = "Total";
	            if (SkinningDefs.TheInstance.GetBoolData("scorecard_headings_uppercase", false))
	            {
		            title = title.ToUpper();
	            }

                AddHeadingCell(columnHeadingRow, title, columnHeadingBackColour, columnHeadingTextColour, "center");
            }

            numRows++;
        }

        void AddHeadingCell (XmlElement row, string value, Color backColour, Color textColour, string alignment)
        {
            var headingCell = row.AppendNewChild("cell");
            headingCell.AppendAttribute("val", value);
            headingCell.AppendAttribute("textstyle", "bold");
            headingCell.AppendAttribute("no_border", noBorder);
            headingCell.AppendAttribute("colour", backColour);
            headingCell.AppendAttribute("textcolour", textColour);
            headingCell.AppendAttribute("align", alignment);
        }

        protected XmlElement AddSectionHeading(string title, string alignment = "left")
        {
            var alignToLower = alignment.ToLower();
            Debug.Assert(alignToLower == "left" || alignToLower == "center" || alignToLower == "right");

            var table = root.AppendNewChild("table");

            table.AppendAttribute("columns", 1);
            if (! noBorder)
            {
                table.AppendAttribute("border_colour", borderColour);
            }
            table.AppendAttribute("row_colour_1", rowColour1);
            table.AppendAttribute("row_colour_2", rowColour2);

            var headingRow = table.AppendNewChild("rowdata");

            AddHeadingCell(headingRow, title, sectionTitleBackColour, sectionTitleTextColour, alignment);
            
            numRows++;

            return table;
        }

        protected XmlElement CreateResultsTableTemplate()
        {
            var table = root.AppendNewChild("table");
            table.AppendAttribute("columns", numColumns);
            table.AppendAttribute("widths", columnWidths);
            if (! noBorder)
            {
                table.AppendAttribute("border_colour", borderColour);
            }
            table.AppendAttribute("row_colour_1", rowColour1);
            table.AppendAttribute("row_colour_2", rowColour2);
            table.AppendAttribute("textcolour", textColour);
            return table;
        }

        XmlElement CreateValueCell (XmlElement row)
        {
            var valueCell = row.AppendNewChild("cell");
            valueCell.AppendAttribute("no_border", noBorder);
            valueCell.AppendAttribute("textcolour", textColour);
            valueCell.AppendAttribute("align", "center");

            return valueCell;
        }

        void AddTitleCell (XmlElement row, string title)
        {
            var titleCell = row.AppendNewChild("cell");
            titleCell.AppendAttribute("val", title);
            titleCell.AppendAttribute("align", "left");
            titleCell.AppendAttribute("no_border", noBorder);
            titleCell.AppendAttribute("textcolour", textColour);
        }

        protected void AddEmptyRow(XmlElement table)
        {
            var spacerRow = table.AppendNewChild("rowdata");

            for (var round = 0; round <= (numRounds + 1); round++)
            {
                var cell = spacerRow.AppendNewChild("cell");
                cell.AppendAttribute("no_border", noBorder);
            }

            numRows++;
        }

	    public void AddBlankRow (XmlElement table)
	    {
			var row = table.AppendNewChild("rowdata");

		    for (var round = 0; round <= numFigureColumns; round++)
		    {
			    var element = CreateValueCell(row);
			}
		}

	    public class MetricWithTarget
	    {
		    public double? Actual;
		    public double? Target;
	    }

		protected void AddMetricRow (XmlElement table, string title, Func<T, double?> getMetric, 
                                     TotalStyle totalStyle, FigureFormatting figureFormat)
        {
            var scores = roundScores.Select(t => new MetricWithTarget { Actual = getMetric(t), Target = null}).ToList();

            AddRow(table, title, scores, totalStyle, figureFormat);
        }

	    protected void AddMetricRow (XmlElement table, string title, Func<T, MetricWithTarget> getMetric,
	                                 TotalStyle totalStyle, FigureFormatting figureFormat)
	    {
		    var scores = roundScores.Select(getMetric).ToList();

		    AddRow(table, title, scores, totalStyle, figureFormat);
	    }

		// TODO Want to rename this to AddMetricRow but that would break GDO until updated (GDC)
		[Obsolete("Derived classes should use AddMetricRow and/or AddMaturityRow")]
        protected void AddRow(XmlElement table, string title, Func<T, double?> getMetric, TotalStyle totalStyle, FigureFormatting figureFormat)
        {
            var scores = roundScores.Select(t => new MetricWithTarget { Actual = getMetric(t), Target =  null }).ToList();

            AddRow(table, title, scores, totalStyle, figureFormat);
        }

        protected void AddMaturityRow(XmlElement table, string title, IReadOnlyList<double> values, TotalStyle totalStyle, FigureFormatting figureFormat)
        {
            AddRow(table, title, values.Select(v => new MetricWithTarget { Actual =(double?)v, Target = null}).ToList(), totalStyle, figureFormat);
        }

        
        void AddRow (XmlElement table, string title, IReadOnlyList<MetricWithTarget> values, 
                        TotalStyle totalStyle, FigureFormatting figureFormat)
        {
            var row = table.AppendNewChild("rowdata");

            AddTitleCell(row, title);

            double? accumulator = null;
            double max = 0;
            var min = double.MaxValue;
            var count = 0;

	        double? targetAccumulator = null;
	        double targetMax = 0;
	        var targetMin = double.MaxValue;
	        var targetCount = 0;

            for (var round = 1; round <= numFigureColumns; round++)
            {
                var valueCell = CreateValueCell(row);

                double? score = null;
	            double? target = null;

                if (round <= values.Count)
                {
                    score = values[round - 1]?.Actual;
                    if (score.HasValue)
                    {
	                    if (accumulator.HasValue)
	                    {
		                    accumulator += score.Value;
	                    }
	                    else
	                    {
		                    accumulator = score.Value;
	                    }
	                    count++;

                        if (max < score.Value)
                        {
                            max = score.Value;
                        }

                        if (min > score.Value)
                        {
                            min = score.Value;
                        }
                    }

	                target = values[round - 1]?.Target;
	                if (target.HasValue)
	                {
		                if (targetAccumulator.HasValue)
		                {
			                targetAccumulator += target.Value;
		                }
		                else
		                {
			                targetAccumulator = target.Value;
		                }
		                targetCount++;

		                if (targetMax < target.Value)
		                {
			                targetMax = target.Value;
		                }

		                if (targetMin > target.Value)
		                {
			                targetMin = target.Value;
		                }
	                }
				}
				else if (round == totalColumn)
                {
                    switch (totalStyle)
                    {
                        case TotalStyle.Mean:
                            if (count > 0)
                            {
                                score = accumulator / count;
                            }

	                        if (targetCount > 0)
	                        {
		                        target = targetAccumulator / targetCount;
	                        }
                            break;

                        case TotalStyle.Total:
                            score = accumulator;
	                        target = targetAccumulator;
                            break;

                        case TotalStyle.Max:
                            if (max > 0)
                            {
                                score = max;
                            }

	                        if (targetMax > 0)
	                        {
		                        target = targetMax;
	                        }
                            break;

                        case TotalStyle.Min:
                            if (min < double.MaxValue)
                            {
                                score = min;
                            }
	                        if (targetMin < double.MaxValue)
	                        {
		                        target = targetMin;
	                        }
                            break;

                        case TotalStyle.None:
                            break;
                    }
                }

                if (score != null)
                {
	                string text = null;
                    switch (figureFormat)
                    {
                        case FigureFormatting.Integer:
							text = CONVERT.ToStr((int)score.Value);
                            break;

	                    case FigureFormatting.CommaedInteger:
							text = CONVERT.ToStrWithCommas((int) score.Value);
		                    break;

	                    case FigureFormatting.Millions:
							text = CONVERT.ToPaddedStr(score.Value / 1000000.0, 2);
		                    break;

                        case FigureFormatting.Money:
							text = SkinningDefs.TheInstance.GetCurrencyString(score.Value);
                            break;

                        case FigureFormatting.MoneyMillions:
							text = SkinningDefs.TheInstance.GetCurrencyString(score.Value / 1000000.0, 2);
                            break;

                        case FigureFormatting.Time:
							text = CONVERT.FormatTimeFourDigits(score.Value);
                            break;

                        case FigureFormatting.Percentage:
							text = CONVERT.ToPaddedStr(score.Value, 0) + "%";
                            break;

                        case FigureFormatting.DecimalPoint:
							text = CONVERT.ToPaddedStr(score.Value, 2);
                            break;
                    }

	                if (target.HasValue && (target.Value != 0))
	                {
		                valueCell.AppendAttribute("val", text + " (" + CONVERT.ToStr((int) (100 * score.Value / target.Value)) + "%)");
	                }
					else
	                {
		                valueCell.AppendAttribute("val", text);
	                }
                }
            }

            numRows++;
        }
    }
}