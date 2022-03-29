using System.Xml;
using System.Drawing;
using LibCore;

namespace ReportBuilder
{
	public class ProfitLossBarChartReport
	{
		SummaryReportData data;

		public ProfitLossBarChartReport (SummaryReportData data)
		{
			this.data = data;
		}

		public string BuildReport ()
		{			
			BasicXmlDocument xml = BasicXmlDocument.Create();
			XmlElement root = xml.AppendNewChild("bargraph");
			BasicXmlDocument.AppendAttribute(root, "show_key", false);
			BasicXmlDocument.AppendAttribute(root, "show_bar_values", false);

			XmlElement xAxis = xml.AppendNewChild(root, "xAxis");
			BasicXmlDocument.AppendAttribute(xAxis, "minMaxSteps", CONVERT.Format("1,{0},1", data.RoundsMax));
			BasicXmlDocument.AppendAttribute(xAxis, "autoScale", false);

			XmlElement yAxis = xml.AppendNewChild(root, "yAxis");
			BasicXmlDocument.AppendAttribute(yAxis, "minMaxSteps", "-15,15,5");
			BasicXmlDocument.AppendAttribute(yAxis, "autoScale", false);

			XmlElement bars = xml.AppendNewChild(root, "bars");

			for (int round = 1; round <= data.RoundsMax; round++)
			{
				XmlElement bar = xml.AppendNewChild(bars, "bar");

				BasicXmlDocument.AppendAttribute(bar, "colour", CONVERT.ToComponentStr(CoreUtils.SkinningDefs.TheInstance.GetColorData("pdf_report_profit_loss_graph_colour", Color.DarkGreen)));
				BasicXmlDocument.AppendAttribute(bar, "height", (round <= data.RoundsPlayed) ? (int) data.ProfitGain[round - 1] : 0);
			}

			string filename = data.GameFile.Dir + @"\global\ProfitLossReport.xml";
			xml.Save(filename);
			return filename;
		}
	}
}
