using System.Xml;
using System.Drawing;
using LibCore;

namespace ReportBuilder
{
	public class MaturityIndicatorBarChartReport
	{
		SummaryReportData data;

		public MaturityIndicatorBarChartReport (SummaryReportData data)
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
			BasicXmlDocument.AppendAttribute(yAxis, "minMaxSteps", "0,100,20");
			BasicXmlDocument.AppendAttribute(yAxis, "autoScale", false);

			XmlElement bars = xml.AppendNewChild(root, "bars");

			for (int round = 1; round <= data.RoundsMax; round++)
			{
				XmlElement bar = xml.AppendNewChild(bars, "bar");

				BasicXmlDocument.AppendAttribute(bar, "colour", CONVERT.ToComponentStr(CoreUtils.SkinningDefs.TheInstance.GetColorData("pdf_report_maturity_graph_colour", Color.DarkGreen)));
				BasicXmlDocument.AppendAttribute(bar, "height", (round <= data.RoundsPlayed) ? (int) data.IndicatorScore[round - 1] : 0);
			}

			string filename = data.GameFile.Dir + @"\global\MaturityReport.xml";
			xml.Save(filename);
			return filename;
		}
	}
}
