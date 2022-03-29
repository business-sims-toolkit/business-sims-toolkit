using System;
using System.Xml;
using System.Drawing;

using CoreUtils;

using GameManagement;
using LibCore;


namespace DevOps.ReportsScreen
{
	public class NpsSurveyReport
    {
        string surveyFilename;
        NetworkProgressionGameFile gameFile;

        int minScore = 0;
        int maxScore = 10;

        int numQuestions = 0;

        public NpsSurveyReport(NetworkProgressionGameFile gameFile, string surveyFilename = "pathfinder_survey_wizard.xml")
        {
            this.gameFile = gameFile;
            this.surveyFilename = surveyFilename;
        }

        public string BuildReport( )
        {
            BasicXmlDocument xmlReport = BasicXmlDocument.Create();

            XmlElement xmlRoot = xmlReport.AppendNewChild("grouped_bar_chart");

            BasicXmlDocument.AppendAttribute(xmlRoot, "bars_stacked", false);
            BasicXmlDocument.AppendAttribute(xmlRoot, "bold_legend", true);
            BasicXmlDocument.AppendAttribute(xmlRoot, "draw_tick_lines", false);
            BasicXmlDocument.AppendAttribute(xmlRoot, "max_label_rows", 1);
            BasicXmlDocument.AppendAttribute(xmlRoot, "row_colour_one", SkinningDefs.TheInstance.GetColorData("scorecard_row_two_colour", Color.White));
            BasicXmlDocument.AppendAttribute(xmlRoot, "row_colour_two", SkinningDefs.TheInstance.GetColorData("scorecard_row_one_colour", Color.White));

            XmlElement categories = xmlReport.AppendNewChild(xmlRoot, "bar_categories");

            int numRounds = SkinningDefs.TheInstance.GetIntData("roundcount", 4);
            

            XmlElement groupsElement = xmlReport.AppendNewChild(xmlRoot, "groups");
            groupsElement.AppendAttribute("legend", "Round");
            groupsElement.AppendAttribute("use_gradient", false);

            AddCategory(xmlReport, categories, "Actual Score",
                SkinningDefs.TheInstance.GetColorData("nps_report_actual_colour"));
            AddCategory(xmlReport, categories, "Expected Score",
                SkinningDefs.TheInstance.GetColorData("nps_report_expected_colour"));

            for( int round = 1; round <= numRounds; round++)
            {
                AddRoundGroup(xmlReport, groupsElement, round);
            }


            int max = maxScore * numQuestions;
            int interval = (int)Algorithms.Maths.RoundToNiceInterval(max / 10.0);
            AddYAxis(xmlReport, xmlRoot, "Customer Satisfaction", minScore, max, interval,
                Color.FromArgb(125, 238, 238, 238));

            if (gameFile.LastRoundPlayed > 0)
            {
                string reportFile = gameFile.GetRoundFile(gameFile.LastRoundPlayed, "NpsSurveyReport.xml",
                    GameFile.GamePhase.OPERATIONS);

                xmlReport.Save(reportFile);

                return reportFile;
            }
            return "";
        }

        Color GetRoundColourFromSkin(int roundNumber)
        {
            return SkinningDefs.TheInstance.GetColorData(CONVERT.Format("nps_report_round_{0}_colour", roundNumber),
                Color.Black);
        }

        void AddYAxis(BasicXmlDocument xml, XmlElement root, string legend, int min, int max, int interval, Color tickColour)
        {
            XmlElement yAxis = xml.AppendNewChild(root, "y_axis");
            BasicXmlDocument.AppendAttribute(yAxis, "legend", legend);

            BasicXmlDocument.AppendAttribute(yAxis, "min", min);
            BasicXmlDocument.AppendAttribute(yAxis, "max", max);
            BasicXmlDocument.AppendAttribute(yAxis, "interval", interval);
            BasicXmlDocument.AppendAttribute(yAxis, "tick_colour", tickColour);
        }

        void AddCategory(BasicXmlDocument xml, XmlElement parent, string name, Color colour)
        {
            XmlElement category = xml.AppendNewChild(parent, "bar_category");

            BasicXmlDocument.AppendAttribute(category, "name", name);
            BasicXmlDocument.AppendAttribute(category, "colour", colour);
            BasicXmlDocument.AppendAttribute(category, "border_colour", Color.Black);
            BasicXmlDocument.AppendAttribute(category, "border_thickness", 1);
            BasicXmlDocument.AppendAttribute(category, "border_inset", 2);
        }

        void AddRoundGroup(BasicXmlDocument xml, XmlElement groupsParent, int roundNumber)
        {
            int score = (roundNumber <= gameFile.LastRoundPlayed) ? CalculateCombinedScoreForRound(roundNumber) : 0;

            XmlElement group = xml.AppendNewChild(groupsParent, "group");
            BasicXmlDocument.AppendAttribute(group, "name", roundNumber.ToString());
            BasicXmlDocument.AppendAttribute(group, "stacked", false);

            AddBarToGroup(xml, group, score, "Actual Score");

            int expectedScore = (roundNumber <= gameFile.LastRoundPlayed) ? GetExpectedScoreForRound(roundNumber) : 0;

            AddBarToGroup(xml, group, expectedScore, "Expected Score");


        }

        void AddBarToGroup(BasicXmlDocument xml, XmlElement groupParent, int score, string category)
        {
            XmlElement bar = xml.AppendNewChild(groupParent, "bar");
            BasicXmlDocument.AppendAttribute(bar, "category", category);
            BasicXmlDocument.AppendAttribute(bar, "height", score);
            BasicXmlDocument.AppendAttribute(bar, "display_height", false);
            BasicXmlDocument.AppendAttribute(bar, "display_text", score.ToString());
        }

        

        int CalculateCombinedScoreForRound(int round)
        {
            string surveyFile = gameFile.GetRoundFile(round, surveyFilename, GameFile.GamePhase.OPERATIONS);
            BasicXmlDocument surveyXml = BasicXmlDocument.CreateFromFile(surveyFile);

            XmlElement surveyRoot = surveyXml.DocumentElement;

            int totalScore = 0;
            int questionCount = 0;
            foreach (XmlElement surveySection in surveyRoot.ChildNodes)
            {
                if (surveySection.SelectSingleNode("section_name").InnerText == "Expected Value")
                {
                    continue;
                }

                foreach (XmlElement surveyAspect in surveySection.SelectSingleNode("aspects").ChildNodes)
                {

                    XmlNode scoreElement = surveyAspect.SelectSingleNode("dest_tag_data");
                    int score = CONVERT.ParseInt(scoreElement.InnerText);

                    score = Algorithms.Maths.Clamp(score, minScore, maxScore);
                    totalScore += score;

                    questionCount++;
                }
            }

            numQuestions = Math.Max(questionCount, numQuestions);

            return totalScore;
        }

        int GetExpectedScoreForRound(int round)
        {
            string surveyFile = gameFile.GetRoundFile(round, surveyFilename, GameFile.GamePhase.OPERATIONS);

            BasicXmlDocument surveyXml = BasicXmlDocument.CreateFromFile(surveyFile);

            XmlElement surveyRoot = surveyXml.DocumentElement;

            int score = 0;
            foreach(XmlElement survey in surveyRoot.ChildNodes)
            {
                if (survey.SelectSingleNode("section_name").InnerText != "Expected Value")
                {
                    continue;
                }

                foreach (XmlElement aspect in survey.SelectSingleNode("aspects").ChildNodes)
                {
                    XmlNode scoreNode = aspect.SelectSingleNode("dest_tag_data");

                    if (scoreNode != null)
                    {
                        score += CONVERT.ParseInt(scoreNode.InnerText);
                    }
                }
            }

            return score;
        }
    }
}
