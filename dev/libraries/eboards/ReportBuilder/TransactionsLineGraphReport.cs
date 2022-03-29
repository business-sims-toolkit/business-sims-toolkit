using System.Xml;
using System.Drawing;
using LibCore;

namespace ReportBuilder
{
	public class TransactionsLineGraphReport
	{
		SummaryReportData data;

		public TransactionsLineGraphReport (SummaryReportData data)
		{
			this.data = data;
		}

		public string BuildReport ()
		{
			BasicXmlDocument xml = BasicXmlDocument.Create();
			XmlElement root = xml.AppendNewChild("linegraph");
			BasicXmlDocument.AppendAttribute(root, "show_key", false);

			XmlElement xAxis = xml.AppendNewChild(root, "xAxis");
			BasicXmlDocument.AppendAttribute(xAxis, "minMaxSteps", CONVERT.Format("1,{0},1", data.RoundsMax));
			BasicXmlDocument.AppendAttribute(xAxis, "autoScale", false);
			BasicXmlDocument.AppendAttribute(xAxis, "title", "Round");

			XmlElement yAxis = xml.AppendNewChild(root, "yLeftAxis");
			BasicXmlDocument.AppendAttribute(yAxis, "minMaxSteps", "0,100,20");
			BasicXmlDocument.AppendAttribute(yAxis, "autoScale", false);
			BasicXmlDocument.AppendAttribute(yAxis, "title", CoreUtils.SkinningDefs.TheInstance.GetData("transactionname"));

			XmlElement line = xml.AppendNewChild(root, "data");
			BasicXmlDocument.AppendAttribute(line, "yscale", "left");
			BasicXmlDocument.AppendAttribute(line, "thickness", "4");
			BasicXmlDocument.AppendAttribute(line, "colour", CONVERT.ToComponentStr(CoreUtils.SkinningDefs.TheInstance.GetColorData("pdf_report_transactions_graph_colour", Color.DarkGreen)));

			for (int round = 1; round <= data.RoundsPlayed; round++)
			{
				XmlElement point = xml.AppendNewChild(line, "p");

				BasicXmlDocument.AppendAttribute(point, "x", round);
				BasicXmlDocument.AppendAttribute(point, "y", data.TransactionsAchieved[round - 1]);
				BasicXmlDocument.AppendAttribute(point, "dot", true);
			}

			string filename = data.GameFile.Dir + @"\global\TransactionsReport.xml";
			xml.Save(filename);
			return filename;
		}
	}
}
