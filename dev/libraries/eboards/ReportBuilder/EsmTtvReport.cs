using System;
using System.Drawing;
using System.Xml;

using GameManagement;
using LibCore;
using CoreUtils;
using Logging;
using BusinessServiceRules;

namespace ReportBuilder
{
	public class EsmTtvReport
	{
		NetworkProgressionGameFile gameFile;
		TimeLog<double> timeToRevenue;
		TimeLog<double> timeToMaxRevenue;

        protected string header_colour = SkinningDefs.TheInstance.GetData("table_header_color", "40,44,46");
        protected string text_colour = SkinningDefs.TheInstance.GetData("table_text_colour", "233,233,234");
        protected string table_border_color = SkinningDefs.TheInstance.GetData("table_border_color", "40,44,46");
        protected string table_row_color = SkinningDefs.TheInstance.GetData("table_row_colour", "0,0,0");
        protected string table_row_color_alt = SkinningDefs.TheInstance.GetData("table_row_colour_alternate", "30,33,35");
        protected string table_no_border = SkinningDefs.TheInstance.GetData("table_no_border", "true");

		public EsmTtvReport (NetworkProgressionGameFile gameFile)
		{
			this.gameFile = gameFile;
		}

		void reader_RevenueApplyAttributes (object sender, string key, string line, double time)
		{
			string revenueString = BasicIncidentLogReader.ExtractValue(line, "revenue");
			if (! string.IsNullOrEmpty(revenueString))
			{
				timeToRevenue.Add(time, CONVERT.ParseDouble(revenueString));
			}

			string maxRevenueString = BasicIncidentLogReader.ExtractValue(line, "max_revenue");
			if (! string.IsNullOrEmpty(maxRevenueString))
			{
				timeToMaxRevenue.Add(time, CONVERT.ParseDouble(maxRevenueString));
			}
		}

		public string BuildReport (int round, Color backgroundColour)
		{
			using (CostMonitor costs = new CostMonitor ("CostedEvents", gameFile.GetNetworkModel(round), AppInfo.TheInstance.Location + "\\data\\costs.xml"))
			{
				timeToRevenue = new TimeLog<double> ();
				timeToMaxRevenue = new TimeLog<double> ();
				using (BasicIncidentLogReader reader = new BasicIncidentLogReader(gameFile.GetRoundFile(round, "NetworkIncidents.log", GameFile.GamePhase.OPERATIONS)))
				{
					reader.WatchApplyAttributes("Revenue", reader_RevenueApplyAttributes);
					reader.Run();
				}

				BasicXmlDocument xml = BasicXmlDocument.Create();

				XmlElement root = xml.AppendNewChild("graph");
				root.AppendAttribute("show_key", true);
				root.AppendAttribute("better_auto_scale", true);

				XmlElement xAxis = root.AppendNewChild("xAxis");
				xAxis.AppendAttribute("minMaxSteps", "0,25,1");
				xAxis.AppendAttribute("autoScale", false);
				xAxis.AppendAttribute("grid", false);
				xAxis.AppendAttribute("title", "Time");
				xAxis.AppendAttribute("colour", text_colour);
				xAxis.AppendAttribute("textcolour", text_colour);

				XmlElement yAxis = root.AppendNewChild("yLeftAxis");
				yAxis.AppendAttribute("autoScale", true);
				yAxis.AppendAttribute("grid", true);
				yAxis.AppendAttribute("title", "Value");
				yAxis.AppendAttribute("colour", text_colour);
				yAxis.AppendAttribute("textcolour", text_colour);

				foreach (var series in new Tuple<string, TimeLog<double>, Color> [] { new Tuple<string, TimeLog<double>, Color> ("Potential", timeToMaxRevenue, Color.Red),
				                                                                      new Tuple<string, TimeLog<double>, Color> ("Revenue", timeToRevenue, Color.White)})
				{
					XmlElement line = root.AppendNewChild("data");
					line.AppendAttribute("yscale", "left");
					line.AppendAttribute("title", series.Item1);
					line.AppendAttribute("colour", series.Item3);

					foreach (double time in series.Item2.Times)
					{
						XmlElement point = line.AppendNewChild("p");
						point.AppendAttribute("x", time / 60);
						point.AppendAttribute("y", series.Item2[time] / 1000.0);
					}
				}

				string filename = gameFile.GetRoundFile(round, "TtvReport.xml", GameFile.GamePhase.OPERATIONS);
				xml.Save(filename);
				return filename;
			}
		}
	}
}