using System.Xml;
using System.Collections;
using GameManagement;
using LibCore;

namespace ReportBuilder
{
	/// <summary>
	/// Summary description for OpsRacePointsReport.
	/// </summary>
	public class OpsRacePointsReport
	{
		string[] Colours = new string[5];

		public OpsRacePointsReport()
		{
			Colours[0] = "210, 129, 120";
			Colours[1] = "242, 175, 201";
			Colours[2] = "24, 54, 126";
			Colours[3] = "96, 149, 193";
			Colours[4] = "166, 217, 106";
		}

		public string BuildReport(NetworkProgressionGameFile gameFile, int round, bool Championship, ArrayList Scores)
		{
			Hashtable teams = new Hashtable();
			if (Championship == true)
			{
				ReportUtils rep = new ReportUtils(gameFile);

				for (int i=1; i<=round; i++)
				{
					if (i > Scores.Count) continue;
					RoundScores scores = (RoundScores)Scores[i-1];

					//now work out how many points each team got
					foreach( CarInfo car in scores.cars.Values)
					{
						string team = car.team;

						if (teams.ContainsKey(team))
						{
							int pos = CONVERT.ParseInt(car.pos);
							int pts = (int)teams[team] + rep.GetCost("pos " + car.pos,round);
							teams[team] = pts;
						}
						else
						{
							int pos = CONVERT.ParseInt(car.pos);
							int pts = rep.GetCost("pos " + car.pos,round);
							teams.Add(team,pts);
						}
					}
				}
			}
			else
			{
				if (round <= Scores.Count)
				{
					teams = ((RoundScores)Scores[round-1]).GetRoundPoints();
				}	
			}

			//Create the xml file
			string reportFile = gameFile.GetRoundFile(gameFile.LastRoundPlayed, "OpsRacePointsReport_Championship.xml" , gameFile.LastPhasePlayed);

			LibCore.BasicXmlDocument xdoc = LibCore.BasicXmlDocument.Create();
			XmlNode root = (XmlNode) xdoc.CreateElement("bargraph");
			xdoc.AppendChild(root);
			//
			//
			XmlNode xtitle = (XmlNode) xdoc.CreateElement("xAxis");
			//
			int xmax = 0;
			if (teams == null) xmax = 0;
			else xmax = teams.Count;
			if (xmax == 0) xmax = 5;
			((XmlElement)xtitle).SetAttribute( "minMaxSteps","0," + xmax + ",1" );
			((XmlElement)xtitle).SetAttribute( "autoScale","false" );
			root.AppendChild(xtitle);
			//
			XmlNode yaxis = (XmlNode) xdoc.CreateElement("yAxis");
			if (Championship == true)
			{
				((XmlElement)yaxis).SetAttribute( "minMaxSteps","0,250,25");
			}
			else
			{
				((XmlElement)yaxis).SetAttribute( "minMaxSteps","0,60,5");
			}
//			((XmlElement)yaxis).SetAttribute( "colour","255,0,0" );
			root.AppendChild(yaxis);

			XmlNode bars = (XmlNode) xdoc.CreateElement("bars");
			root.AppendChild(bars);

			//sort alphabetically
			if (teams != null)
			{
				ArrayList teamnames = new ArrayList(teams.Keys);
				teamnames.Sort();

				int i=0;
				foreach (string team in teamnames)
				{
					XmlNode bar = (XmlNode) xdoc.CreateElement("bar");
					((XmlElement)bar).SetAttribute( "colour", Colours[i] );
					((XmlElement)bar).SetAttribute( "title", team);
					((XmlElement)bar).SetAttribute( "height", CONVERT.ToStr((int)teams[team]));
					bars.AppendChild(bar);

					i++;
				}
			}
			
			xdoc.SaveToURL("",reportFile);
			return reportFile;
		}
	}
}
