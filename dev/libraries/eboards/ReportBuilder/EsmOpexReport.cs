using System;
using System.Xml;
using System.Drawing;
using Algorithms;
using LibCore;
using GameManagement;
using BusinessServiceRules;

namespace ReportBuilder
{
	public class EsmOpexReport
	{
		NetworkProgressionGameFile gameFile;

		public EsmOpexReport (NetworkProgressionGameFile gameFile)
		{
			this.gameFile = gameFile;
		}

		string GetFunctionTagFromName (string functionName)
		{
			string attributeName = functionName;
			if (functionName == "FIN/LEG")
			{
				attributeName = "Other";
			}

			return attributeName.ToLower();
		}

		public string BuildReport (int round)
		{
			using (CostMonitor costs = new CostMonitor ("CostedEvents", gameFile.GetNetworkModel(round), gameFile.GetGlobalFile(CONVERT.Format("costs_r{0}.xml", round))))
			{
				BasicXmlDocument xml = BasicXmlDocument.Create();

				XmlElement root = xml.AppendNewChild("grouped_bar_chart");

				XmlElement categories = root.AppendNewChild("bar_categories");

				double maxY = 0;

				XmlElement yAxis = root.AppendNewChild("y_axis");
				yAxis.AppendAttribute("min", 0);
				yAxis.AppendAttribute("interval", 1);

				XmlElement groups = root.AppendNewChild("groups");

				foreach (string function in new [] { "HR", "IT", "FM", "Other" })
				{
                    string functionName = function;
                    switch (function)
                    {
                        case "Other":
                            functionName = "leg_fin";
                            break;

                        case "FM":
                            functionName = "fac";
                            break;
                    }

                    XmlElement category = categories.AppendNewChild("bar_category");
                    category.AppendAttribute("name", function);
                    category.AppendAttribute("colour", CoreUtils.SkinningDefs.TheInstance.GetColorDataGivenDefault("function_colour_" + functionName.ToLower(), Color.White));
                    category.AppendAttribute("border_colour", Color.Black);
                    category.AppendAttribute("border_thickness", 1);
                    category.AppendAttribute("show_in_key", false);
                    category.AppendAttribute("border_inset", 2);

                    XmlElement group = groups.AppendNewChild("group");
					group.AppendAttribute("name", function);

					XmlElement bar = group.AppendNewChild("bar");
					bar.AppendAttribute("category", function);

					double y = costs.GetCost(CONVERT.Format("opex_{0}", GetFunctionTagFromName(function))) / 1000000.0;
					maxY = Math.Max(maxY, y);

					bar.AppendAttribute("height", y);
				}

				yAxis.AppendAttribute("max", Maths.RoundToNiceInterval(Math.Max(1, maxY)));

				string xmlFilename = gameFile.GetRoundFile(round, "OpexReport.xml", GameFile.GamePhase.OPERATIONS);
				xml.Save(xmlFilename);

				return xmlFilename;
			}
		}
	}
}