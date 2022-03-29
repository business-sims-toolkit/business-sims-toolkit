using System;
using System.Collections.Generic;
using System.Drawing;
using System.Xml;

using Network;
using GameManagement;
using LibCore;
using CoreUtils;

namespace ReportBuilder
{
	public class ESM_BusinessCaseReport
	{
		string BackgroundColour = SkinningDefs.TheInstance.GetData("table_colour", "40, 44, 46");
		string TextColour = SkinningDefs.TheInstance.GetData("table_text_colour", "233, 233, 234");

		NetworkProgressionGameFile gameFile;

		public ESM_BusinessCaseReport (NetworkProgressionGameFile gameFile)
		{
			this.gameFile = gameFile;
		}

		public string BuildReport (int round, EsmRoundScores roundScores)
		{
			string filename = gameFile.GetRoundFile(round, "BusinessCaseReport.xml", GameFile.GamePhase.OPERATIONS);
			NodeTree model = gameFile.GetNetworkModel(round);

			BasicXmlDocument xml = BasicXmlDocument.Create();

			XmlElement root = xml.AppendNewChild("timechart");

			double endTime = model.GetNamedNode("CurrentTime").GetIntAttribute("seconds", 0);
			if (endTime <= 0)
			{
				endTime = 1;
			}

			XmlElement timeline = xml.AppendNewChild(root, "timeline");
			timeline.AppendAttribute("start", 0);
			timeline.AppendAttribute("end", Math.Ceiling(endTime / 60.0f));
			timeline.AppendAttribute("interval", 5);
			timeline.AppendAttribute("legend", "Time");
			timeline.AppendAttribute("font_colour", TextColour);
			timeline.AppendAttribute("colour", BackgroundColour);

			XmlElement sections = root.AppendNewChild("sections");
            foreach (EsmRoundScores.BusinessCase businessCase in roundScores.NameToBusinessCase.Values)
            {
                XmlElement section = sections.AppendNewChild("section");
                section.AppendAttribute("legend", businessCase.Description);
                section.AppendAttribute("font_colour", TextColour);
                section.AppendAttribute("colour", BackgroundColour);
                section.AppendAttribute("header_width", 100);

                Dictionary<string, Color> functionNameToColour = new Dictionary<string, Color>();
                foreach (Node function in model.GetNamedNode("Functions").GetChildrenOfType("function"))
                {
                    functionNameToColour.Add(function.GetAttribute("attribute_name"), CONVERT.ParseComponentColor(function.GetAttribute("color")));
                }

                bool showProfit = SkinningDefs.TheInstance.GetBoolData("show_business_case_profit", true);

                foreach (string functionName in functionNameToColour.Keys)
                {
                    XmlElement row = section.AppendNewChild("row");
                    row.AppendAttribute("font_colour", TextColour);
                    row.AppendAttribute("colour", BackgroundColour);
                    string functionLegend =
                        roundScores.GetFunctionDescription(functionName).Replace(@"&", @"&&").ToUpper();
                    row.AppendAttribute("legend", functionLegend);

                    double functionEndTime = businessCase.InstallTimeEnd ?? businessCase.PenaltyTime ?? businessCase.EndTime ?? endTime;
                    if (businessCase.FunctionAttributeNameToSatisfiedTime.ContainsKey(functionName))
                    {
                        functionEndTime = businessCase.FunctionAttributeNameToSatisfiedTime[functionName];
                    }

                    double installEndTime = businessCase.InstallTimeEnd ?? endTime;

                    if (businessCase.InstallTimeStart != null)
                    {
                        XmlElement installBlock = row.AppendNewChild("block");
                        installBlock.AppendAttribute("start", businessCase.InstallTimeStart.Value / 60.0f);
                        installBlock.AppendAttribute("end", installEndTime / 60.0f);
                        installBlock.AppendAttribute("colour", Color.HotPink);
                        installBlock.AppendAttribute("legend", "TFR");
                        installBlock.AppendAttribute("small_legend", "");
                        installBlock.AppendAttribute("bottom_legend", "");
                        installBlock.AppendAttribute("hollow", false);
                    }

                    if (businessCase.PenaltyTime != null)
                    {
                        XmlElement penaltyBlock = row.AppendNewChild("block");
                        penaltyBlock.AppendAttribute("start", (businessCase.PenaltyTime.Value / 60.0f) - 0.01f);
                        penaltyBlock.AppendAttribute("end", (businessCase.PenaltyTime.Value / 60.0f) + 0.3f);
                        penaltyBlock.AppendAttribute("colour", Color.DarkRed);
                        penaltyBlock.AppendAttribute("legend", "Fine");
                        penaltyBlock.AppendAttribute("small_legend", "");
                        penaltyBlock.AppendAttribute("bottom_legend", "");
                        penaltyBlock.AppendAttribute("hollow", false);
                        penaltyBlock.AppendAttribute("striped", true);
                    }
                    else if (showProfit)
                    {
                        XmlElement ttvBlock = row.AppendNewChild("block");
                        ttvBlock.AppendAttribute("start", (installEndTime / 60.0f) - 0.01f);
                        ttvBlock.AppendAttribute("end", ((installEndTime + businessCase.TimeToValue) / 60.0f) + 0.01f);
                        ttvBlock.AppendAttribute("colour", Color.DarkMagenta);
                        ttvBlock.AppendAttribute("legend", "TTV");
                        ttvBlock.AppendAttribute("small_legend", "");
                        ttvBlock.AppendAttribute("bottom_legend", "");
                        ttvBlock.AppendAttribute("hollow", false);

                        if (endTime > businessCase.TimeToValue)
                        {
                            XmlElement profitBlock = row.AppendNewChild("block");
                            profitBlock.AppendAttribute("start", ((installEndTime + businessCase.TimeToValue) / 60.0f) - 0.01f);
                            profitBlock.AppendAttribute("end", (endTime / 60.0f) + 0.1f);
                            profitBlock.AppendAttribute("colour", Color.DarkGreen);
                            profitBlock.AppendAttribute("legend", "Profit");
                            profitBlock.AppendAttribute("small_legend", "");
                            profitBlock.AppendAttribute("bottom_legend", "");
                            profitBlock.AppendAttribute("hollow", false);
                        }
                    }

                    string newFunctionName = functionLegend;
                    

                    XmlElement block = row.AppendNewChild("block");
                    block.AppendAttribute("start", businessCase.StartTime / 60.0f);
                    block.AppendAttribute("end", functionEndTime / 60.0f);
                    block.AppendAttribute("colour", functionNameToColour[functionName]);
                    block.AppendAttribute("legend", newFunctionName);
                    block.AppendAttribute("small_legend", "");
                    block.AppendAttribute("bottom_legend", "");
                    block.AppendAttribute("hollow", false);
                }
            }

			xml.Save(filename);
			return filename;
		}
	}
}