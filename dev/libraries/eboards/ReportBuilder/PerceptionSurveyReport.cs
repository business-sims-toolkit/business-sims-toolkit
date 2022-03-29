using System.Collections.Generic;
using System.Xml;

using System.Drawing;

using GameManagement;

using LibCore;

namespace ReportBuilder
{
	public class PerceptionSurveyReport
	{
		public PerceptionSurveyReport ()
		{
		}
		
		public string BuildReport (NetworkProgressionGameFile gameFile, int round)
		{
			string surveyFile = gameFile.GetRoundFile(round, "pathfinder_survey_wizard.xml", GameManagement.GameFile.GamePhase.OPERATIONS);
			BasicXmlDocument surveyXml = BasicXmlDocument.CreateFromFile(surveyFile);
			XmlElement surveyElement = surveyXml.DocumentElement;

			BasicXmlDocument xml = BasicXmlDocument.Create();
			XmlElement barChartElement = xml.AppendNewChild("grouped_bar_chart");

			XmlElement categoriesElement = xml.AppendNewChild(barChartElement, "bar_categories");
			XmlElement yAxisElement = xml.AppendNewChild(barChartElement, "y_axis");
			XmlElement groupsElement = xml.AppendNewChild(barChartElement, "groups");

			Dictionary<string, XmlElement> groupNameToElement = new Dictionary<string, XmlElement> ();

			Color [] colours = new Color [] { Color.Red, Color.Green, Color.Blue,
			                                  Color.Magenta, Color.Yellow, Color.Cyan };
			int colourIndex = 0;

			int minScore = 0;
			int maxScore = 5;

			foreach (XmlElement surveyCategory in surveyElement.ChildNodes)
			{
				Color colour = colours[colourIndex];
				colourIndex = (colourIndex + 1) % colours.Length;

				string categoryName = surveyCategory.SelectSingleNode("section_desc").InnerText;

				XmlElement categoryElement = xml.AppendNewChild(categoriesElement, "bar_category");

				BasicXmlDocument.AppendAttribute(categoryElement, "name", categoryName);
				BasicXmlDocument.AppendAttribute(categoryElement, "colour", CONVERT.ToComponentStr(colour));
				BasicXmlDocument.AppendAttribute(categoryElement, "border_colour", CONVERT.ToComponentStr(Color.Black));
				BasicXmlDocument.AppendAttribute(categoryElement, "border_thickness", 1);
				BasicXmlDocument.AppendAttribute(categoryElement, "border_inset", 2);

				XmlElement surveyAspects = (XmlElement) surveyCategory.SelectSingleNode("aspects");

				foreach (XmlElement surveyAspect in surveyAspects.ChildNodes)
				{
					string groupName = surveyAspect.SelectSingleNode("aspect_name").InnerText;
					int score = CONVERT.ParseInt(surveyAspect.SelectSingleNode("dest_tag_data").InnerText);

					XmlElement groupElement;
					if (groupNameToElement.ContainsKey(groupName))
					{
						groupElement = groupNameToElement[groupName];
					}
					else
					{
						groupElement = xml.AppendNewChild(groupsElement, "group");
						groupNameToElement.Add(groupName, groupElement);
						BasicXmlDocument.AppendAttribute(groupElement, "name", groupName);
					}

					//Option A the score can move the min or max values 
					//minScore = Math.Min(minScore, score);
					//maxScore = Math.Max(maxScore, score);

					//Option B limit the score to the range Min to Max
					if (score < minScore)
					{
						score = minScore;
					}
					if (score > maxScore)
					{
						score = maxScore;
					}

					XmlElement barElement = xml.AppendNewChild(groupElement, "bar");
					BasicXmlDocument.AppendAttribute(barElement, "category", categoryName);
					BasicXmlDocument.AppendAttribute(barElement, "height", score);
				}
			}
			BasicXmlDocument.AppendAttribute(yAxisElement, "min", minScore);
			BasicXmlDocument.AppendAttribute(yAxisElement, "max", maxScore);
			BasicXmlDocument.AppendAttribute(yAxisElement, "interval", 1);

			string reportFile = gameFile.GetRoundFile(round, "Perception.xml", GameFile.GamePhase.OPERATIONS);
			xml.Save(reportFile);
			return reportFile;
		}
	}
}