using System.Collections;
using System.Xml;
using System.IO;

using GameManagement;
using LibCore;
using Logging;
using CoreUtils;

namespace ReportBuilder
{
	/// <summary>
	/// Summary description for OpsKBReport.
	/// </summary>
	public class OpsKBReport
	{
		ArrayList uniqueincidents;
		Hashtable answers;
		Hashtable questions;
		Hashtable KB;

		public OpsKBReport()
		{	
			answers = new Hashtable();
	//		questions = new Hashtable();
	//		uniqueincidents = new ArrayList();
			KB = new Hashtable();
		}

		void ReadAnswers()
		{
			string QsFile = LibCore.AppInfo.TheInstance.Location + "\\data\\answers.xml";
			FileStream fs = new FileStream(QsFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			//
			StreamReader sr = new StreamReader(fs);
			string line = sr.ReadLine();

			while(null != line)
			{
				string number = BasicIncidentLogReader.ExtractValue(line, "number");
				string answer = BasicIncidentLogReader.ExtractValue(line, "answer");


				if (! answers.ContainsKey(number) && number != string.Empty)
				{
					answers.Add(number, answer);
				}

				line = sr.ReadLine();
			}
		}

		void ReadQuestions(int round)
		{
			string QsFile = LibCore.AppInfo.TheInstance.Location + "\\data\\questions_r" + CONVERT.ToStr(round) + ".xml";
			FileStream fs = new FileStream(QsFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			//
			StreamReader sr = new StreamReader(fs);
			string line = sr.ReadLine();

			questions = new Hashtable();

			while(null != line)
			{
				string id = BasicIncidentLogReader.ExtractValue(line, "id");
				string question = BasicIncidentLogReader.ExtractValue(line, "question");


				if (! questions.ContainsKey(id) && id != string.Empty)
				{
					questions.Add(id, question);
				}

				line = sr.ReadLine();
			}
		}

		public string BuildReport(NetworkProgressionGameFile gameFile, ArrayList Scores)
		{
			ReadAnswers();

			for(int i=0; i<Scores.Count; i++)
			{
				uniqueincidents = new ArrayList();

				foreach( string incident in ((RoundScores)Scores[i]).FixedIncidents)
				{

					if ( ! uniqueincidents.Contains(incident))
					{
						uniqueincidents.Add(incident);
					}
				}

				ReadQuestions(i+1);

				foreach (string incident in uniqueincidents)
				{
					string question = (string)questions[incident];
					string answer = (string)answers[question];
					
					if ( ! KB.ContainsKey(question))
					{
						KB.Add(question, answer);
					}
				}
			}

			//Create the xml report
			string reportFile = gameFile.GetRoundFile(gameFile.LastRoundPlayed, "OpsKnowledgeBaseReport.xml" , gameFile.LastPhasePlayed);

			int NumColumns = 2;
			string RowHeight = "23";

			string colwidths = "0.4,0.6";

			LibCore.BasicXmlDocument xdoc = LibCore.BasicXmlDocument.Create();
			XmlNode root = (XmlNode) xdoc.CreateElement("table");
			((XmlElement)root).SetAttribute( "columns","1" );
			((XmlElement)root).SetAttribute( "rowheight",RowHeight );
			xdoc.AppendChild(root);

			//add the title table
			XmlNode titles = (XmlNode) xdoc.CreateElement("table");
			((XmlElement)titles).SetAttribute( "columns", CONVERT.ToStr(NumColumns) );
			((XmlElement)titles).SetAttribute( "widths",colwidths );
			root.AppendChild(titles);

			XmlNode titlerow = (XmlNode) xdoc.CreateElement("rowdata");
			titles.AppendChild(titlerow);

			XmlNode cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement)cell).SetAttribute( "val","Question" );
			((XmlElement)cell).SetAttribute( "colour",SkinningDefs.TheInstance.GetData("fis_reports_green_header_colour", "176,196,222") );
			titlerow.AppendChild(cell);
			cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement)cell).SetAttribute( "val", "Answer");
			((XmlElement)cell).SetAttribute( "colour",SkinningDefs.TheInstance.GetData("fis_reports_green_header_colour", "176,196,222"));
			titlerow.AppendChild(cell);

			//add kb table
			XmlNode kbtable = (XmlNode) xdoc.CreateElement("table");
			((XmlElement)kbtable).SetAttribute( "columns", CONVERT.ToStr(NumColumns) );
			((XmlElement)kbtable).SetAttribute( "widths", colwidths);
			root.AppendChild(kbtable);

			ArrayList sorted = new ArrayList(KB.Keys);
			sorted.Sort();

			for (int i=0; i<sorted.Count; i++)
			{
				string q = (string)sorted[i];
				string a = (string)KB[q];

				XmlNode row = (XmlNode) xdoc.CreateElement("rowdata");
				kbtable.AppendChild(row);

				cell = (XmlNode) xdoc.CreateElement("cell");
				((XmlElement)cell).SetAttribute( "val", q );
				((XmlElement)cell).SetAttribute( "textcolour", "255,0,0" );
				row.AppendChild(cell);

				cell = (XmlNode) xdoc.CreateElement("cell");
				((XmlElement)cell).SetAttribute( "val", a );
				((XmlElement)cell).SetAttribute( "textcolour", "255,0,0" );
				row.AppendChild(cell);

			}
			xdoc.SaveToURL("",reportFile);

			return reportFile;

		}
	}
}
