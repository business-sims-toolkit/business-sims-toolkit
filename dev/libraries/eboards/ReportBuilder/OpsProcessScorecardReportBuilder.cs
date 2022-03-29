using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using GameManagement;
using LibCore;

namespace ReportBuilder
{
    public class OpsProcessScorecardReportBuilder : BaseScorecardReport<RoundScores>
    {
        readonly List<string> sectionsOrder;
        readonly Dictionary<string, List<string>> sectionsToSubSectionsOrder;

        readonly Dictionary<string, Dictionary<string, List<double>>> sectionsToPointValues;

        readonly Dictionary<string, FigureFormatting> subSectionToFigureFormatting;
        readonly Dictionary<string, TotalStyle> subSectionToTotalStyle;

        public OpsProcessScorecardReportBuilder (NetworkProgressionGameFile gameFile, List<RoundScores> roundScores, bool includeTotalColumn = true)
            : base(gameFile, roundScores, includeTotalColumn)
        {
            sectionsOrder = roundScores.SelectMany(s => s.MaturityHashOrder.Cast<string>()).Distinct().ToList();
            sectionsOrder.Add("Maturity Indicator");

            sectionsToSubSectionsOrder = ReportUtils.TheInstance.Maturity_Names.Cast<DictionaryEntry>()
                .ToDictionary(kvp => (string)kvp.Key, kvp => ((ArrayList)kvp.Value).Cast<string>().ToList());

            subSectionToFigureFormatting = sectionsToSubSectionsOrder.SelectMany(s => s.Value)
                .ToDictionary(s => s, s => FigureFormatting.Integer);

            subSectionToTotalStyle = sectionsToSubSectionsOrder.SelectMany(s => s.Value)
                .ToDictionary(s => s, s => TotalStyle.None);

            sectionsToSubSectionsOrder["Maturity Indicator"] = new List<string>
            {
                "Indicator Score"
            };

            subSectionToFigureFormatting["Indicator Score"] = FigureFormatting.DecimalPoint;
            subSectionToTotalStyle["Indicator Score"] = TotalStyle.None;
            
            sectionsToPointValues = sectionsToSubSectionsOrder.ToDictionary(kvp => kvp.Key,
                kvp => kvp.Value.ToDictionary(s => s, s => new List<double>()));
            
            foreach (var score in roundScores)
            {
                var outerSection = score.outer_sections.Cast<DictionaryEntry>().ToDictionary(kvp => (string)kvp.Key,
                    kvp => ((ArrayList)kvp.Value).Cast<string>().ToList());

                outerSection["Maturity Indicator"] = new List<string>
                {
                    $"Indicator Score:{CONVERT.ToPaddedStr(score.IndicatorScore, 2)}"
                };

                foreach (var section in sectionsOrder)
                {
                    var points = outerSection[section];

                    var pointValues = points.Select(p =>
                    {
                        var split = p.Split(':');

                        return new KeyValuePair<string, double>(split[0], double.Parse(split[1]));
                    });

                    foreach (var p in pointValues)
                    {
                        sectionsToPointValues[section][p.Key].Add(p.Value);
                    }

                }
            }
        }

        [Obsolete("Remove when base method is removed")]
        public override string BuildReport()
        {
            return CreateReportAndFilename();
        }

        protected override void GenerateReport()
        {
            AddColumnHeadings();

            foreach (var section in sectionsOrder)
            {
                AddSection(section);
            }
        }

        protected override string GetFilename()
        {
            return gameFile.GetGlobalFile("OpsProcessScoresReport.xml");
        }

        void AddSection(string section)
        {
            AddSectionHeading(section);

            var resultsTable = CreateResultsTableTemplate();

            foreach (var subSection in sectionsToSubSectionsOrder[section])
            {
                AddMaturityRow(resultsTable, subSection, sectionsToPointValues[section][subSection],
                    subSectionToTotalStyle[subSection], subSectionToFigureFormatting[subSection]);
            }
        }
    }
}
