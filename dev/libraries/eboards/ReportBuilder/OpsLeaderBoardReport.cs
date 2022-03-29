using System.Xml;

using GameManagement;
using LibCore;
using CoreUtils;


namespace ReportBuilder
{
	/// <summary>
	/// Summary description for OpsLeaderBoardReport.
	/// </summary>
	public class OpsLeaderBoardReport
	{

		public OpsLeaderBoardReport()
		{
		}

		public string BuildReport(NetworkProgressionGameFile gameFile, int round, RoundScores Scores)
		{
			//Create the xml report
			string reportFile = gameFile.GetRoundFile(gameFile.LastRoundPlayed, "OpsLeaderBoardReport_Round" + round + ".xml" , gameFile.LastPhasePlayed);
			
			int NumColumns = 7;
			string colwidths = "0.1,0.2,0.2,0.1,0.15,0.1,0.15";

			LibCore.BasicXmlDocument xdoc = LibCore.BasicXmlDocument.Create();
			XmlNode root = (XmlNode) xdoc.CreateElement("table");
			((XmlElement)root).SetAttribute( "columns","1" );
			((XmlElement)root).SetAttribute( "rowheight", "39");
			xdoc.AppendChild(root);

			//add the title table
			XmlNode titles = (XmlNode) xdoc.CreateElement("table");
			((XmlElement)titles).SetAttribute( "columns", CONVERT.ToStr(NumColumns) );
			((XmlElement)titles).SetAttribute( "widths", colwidths);
			root.AppendChild(titles);

			XmlNode titlerow = (XmlNode) xdoc.CreateElement("rowdata");
			titles.AppendChild(titlerow);

			XmlNode cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement)cell).SetAttribute( "val","Pos" );
			((XmlElement)cell).SetAttribute( "colour",SkinningDefs.TheInstance.GetData("fis_reports_green_header_colour", "176,196,222"));
			titlerow.AppendChild(cell);
			cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement)cell).SetAttribute( "val", "Driver");
            ((XmlElement)cell).SetAttribute("colour", SkinningDefs.TheInstance.GetData("fis_reports_green_header_colour", "176,196,222"));
			titlerow.AppendChild(cell);
			cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement)cell).SetAttribute( "val", "Team");
            ((XmlElement)cell).SetAttribute("colour", SkinningDefs.TheInstance.GetData("fis_reports_green_header_colour", "176,196,222"));
			titlerow.AppendChild(cell);
			cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement)cell).SetAttribute( "val", "LagD");
            ((XmlElement)cell).SetAttribute("colour", SkinningDefs.TheInstance.GetData("fis_reports_green_header_colour", "176,196,222"));
			titlerow.AppendChild(cell);
			cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement)cell).SetAttribute( "val", "Catch");
            ((XmlElement)cell).SetAttribute("colour", SkinningDefs.TheInstance.GetData("fis_reports_green_header_colour", "176,196,222"));
			titlerow.AppendChild(cell);
			cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement)cell).SetAttribute( "val", "Pts");
            ((XmlElement)cell).SetAttribute("colour", SkinningDefs.TheInstance.GetData("fis_reports_green_header_colour", "176,196,222"));
			titlerow.AppendChild(cell);
			cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement)cell).SetAttribute( "val","Rev" );
			((XmlElement)cell).SetAttribute( "colour",SkinningDefs.TheInstance.GetData("fis_reports_green_header_colour", "176,196,222"));
			titlerow.AppendChild(cell);

			//add leaderboard table
			XmlNode boardtable = (XmlNode) xdoc.CreateElement("table");
			((XmlElement)boardtable).SetAttribute( "columns", CONVERT.ToStr(NumColumns) );
			((XmlElement)boardtable).SetAttribute( "widths", colwidths);
			((XmlElement)boardtable).SetAttribute( "rowheight", "23");
			root.AppendChild(boardtable);

			ReportUtils rep = new ReportUtils(gameFile);
			//need to add a row for every position
			if (Scores != null)
			{
				for (int i=1; i<=20; i++)
				{
					string key = CONVERT.ToStr(i);
					if (Scores.cars.ContainsKey(key))
					{
						int pts = rep.GetCost("pos " + key,round);
						int rev = pts * rep.GetCost("point",round);

						XmlNode row = (XmlNode) xdoc.CreateElement("rowdata");
						boardtable.AppendChild(row);

						string team = ((CarInfo)Scores.cars[key]).team;

						cell = (XmlNode) xdoc.CreateElement("cell");
						((XmlElement)cell).SetAttribute( "val", key );
						row.AppendChild(cell);

						cell = (XmlNode) xdoc.CreateElement("cell");
						((XmlElement)cell).SetAttribute( "val", ((CarInfo)Scores.cars[key]).driver );
						row.AppendChild(cell);

						cell = (XmlNode) xdoc.CreateElement("cell");
						((XmlElement)cell).SetAttribute( "val", team );
						row.AppendChild(cell);

						cell = (XmlNode) xdoc.CreateElement("cell");
						((XmlElement)cell).SetAttribute( "val", ((CarInfo)Scores.cars[key]).lagd );
						row.AppendChild(cell);

						cell = (XmlNode) xdoc.CreateElement("cell");
						string Catch = ((CarInfo)Scores.cars[key]).Catch;

						if (Catch != "-" && Catch != string.Empty)
						{
							int tmp = CONVERT.ParseInt(Catch);

							int mins = (int)tmp / 60;
							int secs = (int)tmp - (60 * mins);
							string secstr = "";
							if (secs < 10) secstr = "0" + CONVERT.ToStr(secs);
							else secstr = CONVERT.ToStr(secs);
							Catch = CONVERT.ToStr(mins) + ":" + secstr;
							((XmlElement)cell).SetAttribute( "textcolour", "165,42,42" );
						}
						((XmlElement)cell).SetAttribute( "val", Catch );
						row.AppendChild(cell);

						cell = (XmlNode) xdoc.CreateElement("cell");
						((XmlElement)cell).SetAttribute( "val", CONVERT.ToStr(pts) );
                        if (team == SkinningDefs.TheInstance.GetData("team_name", "HP")) ((XmlElement)cell).SetAttribute("textcolour", "255,0,0");
						row.AppendChild(cell);

						cell = (XmlNode) xdoc.CreateElement("cell");
						((XmlElement)cell).SetAttribute( "val", "$" + CONVERT.ToPaddedStr(rev / 1000000.0, 1) + "M" );
                        if (team == SkinningDefs.TheInstance.GetData("team_name", "HP")) ((XmlElement)cell).SetAttribute("textcolour", "255,0,0");
						row.AppendChild(cell);

                        if (team == SkinningDefs.TheInstance.GetData("team_name", "HP"))
						{
							foreach (XmlElement child in row.ChildNodes)
							{
                                child.SetAttribute("colour", SkinningDefs.TheInstance.GetData("fis_reports_green_header_colour", "176,196,222"));
							}
						}
					}
				}
			}

			xdoc.SaveToURL("",reportFile);

			return reportFile;
		}
	}
}
