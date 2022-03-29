using System.Xml;
using System.Collections;

using System.Drawing;

using GameManagement;
using LibCore;
using CoreUtils;

namespace ReportBuilder
{
	/// <summary>
	/// Summary description for OpsComparisonReport.
	/// </summary>
	public class OpsComparisonReport
	{
		protected ArrayList _scores = new ArrayList();

		protected string left_colour = "0,0,255";
		protected string right_colour = "0,80,0";
		protected string transactionName = "Transactions";
		protected string axisTextColour = SkinningDefs.TheInstance.GetData("comparison_report_y_axis_text_colour", "0,0,0");
		protected bool showkey = false;

		public OpsComparisonReport()
		{
			string hcol = CoreUtils.SkinningDefs.TheInstance.GetData("comparison_left_color");
			if(hcol != "")
			{
				left_colour = hcol;
			}
			//
			hcol = CoreUtils.SkinningDefs.TheInstance.GetData("comparison_right_color");
			if(hcol != "")
			{
				right_colour = hcol;
			}
			transactionName = SkinningDefs.TheInstance.GetData("transactionname");
		}

		protected string ConvertToMinsSecs(double dsecs)
		{
			int secs = (int) dsecs;
			int mins = secs/60;
			secs = secs - mins*60;

			string strSecs = CONVERT.ToStr(secs).PadLeft(2,'0');
			string str = CONVERT.ToStr(mins).PadLeft(2,'0') + ":" + strSecs;

			return str;
		}

		protected virtual string GetValue(int round, string series)
		{
			//handling the skin dependant items 
			if ((series == (transactionName + " Handled")) || (series == ("Handled " + transactionName)))
			{
				return CONVERT.ToStr(((RoundScores)_scores[round-1]).NumTransactions);
			}
			if ((series == ("Max "+ transactionName)) || (series == ("Maximum " + transactionName)))
			{
				return CONVERT.ToStr(((RoundScores)_scores[round-1]).MaxTransactions);
			}

			RoundScores scores = (RoundScores) _scores[round - 1];

			string gametype = SkinningDefs.TheInstance.GetData("gametype");

			//Everthing else is common amongst all skins 
			switch(series)
			{
				case "Max Revenue":
				case "Target Revenue":
					return CONVERT.ToStr(((RoundScores) _scores[round - 1]).MaxRevenue / 1000000.0, 2);
				case "Maximum Applications":
					return CONVERT.ToStr(((RoundScores) _scores[round - 1]).ApplicationsMax);
				case "Handled Applications":
					return CONVERT.ToStr(((RoundScores)_scores[round - 1]).ApplicationsHandled);
				case "Delayed Applications":
					return CONVERT.ToStr(((RoundScores)_scores[round - 1]).ApplicationsDelayed);
				case "Points":
					return CONVERT.ToStr(((RoundScores)_scores[round-1]).Points);
				case "Budget":
					return CONVERT.ToStr(((RoundScores) _scores[round - 1]).RoundBudget / 1000000.0, 2);
				case "Average Cost per App":
					return CONVERT.ToStr(((RoundScores) _scores[round - 1]).AverageAppCost, 2);
				case "Revenue":
				case "Actual Revenue":
					return CONVERT.ToStr(((RoundScores)_scores[round-1]).Revenue / 1000000.0, 2);
				case "Fixed Costs":
					return CONVERT.ToStr(((RoundScores)_scores[round-1]).FixedCosts / 1000000.0, 2);
				case "New Service Costs":
				case "Service Cost (new/upgrade)":
					return CONVERT.ToStr(((RoundScores)_scores[round-1]).ProjectSpend / 1000000.0,2);
				case "Services Implemented (new/upgrade)":
					return CONVERT.ToStr(((RoundScores) _scores[round - 1]).NumServices);
				case "Regulation Fines":
				case "Fines":
					return CONVERT.ToStr(((RoundScores)_scores[round-1]).RegulationFines / 1000000.0, 2);
				case "Profit / Loss":
					return CONVERT.ToStr(((RoundScores)_scores[round-1]).Profit / 1000000.0, 2);
				case "Gain / Loss":
				case "Improvement on previous round":
					return CONVERT.ToStr(((RoundScores)_scores[round-1]).Gain / 1000000.0, 2);
				case "Support Spend":
					return CONVERT.ToStr(((RoundScores)_scores[round-1]).SupportCostsTotal / 1000000.0, 2);
				case "Support Profit/Loss":
					return CONVERT.ToStr(((RoundScores)_scores[round-1]).SupportProfit / 1000000.0, 2);
				case "Support Budget":
					return CONVERT.ToStr(((RoundScores) _scores[round - 1]).SupportBudget / 1000000.0, 2);
				case "New Services Implemented":
					return CONVERT.ToStr(((RoundScores)_scores[round-1]).NumServices);
				case "New Services Implemented Before Race":
				case "New Services Implemented Before Round":
					return CONVERT.ToStr(((RoundScores)_scores[round-1]).NumServicesBeforeRace);
				case "Availability":
					return CONVERT.ToStr(((RoundScores)_scores[round-1]).Availability);
				case "MTRS":
				case "Mean Time to Restore Service":
					return CONVERT.ToStr(((RoundScores)_scores[round-1]).MTTR);
				case "Total Failures":
					return CONVERT.ToStr(((RoundScores)_scores[round-1]).Incidents);
				case "Prevented Failures":
					return CONVERT.ToStr(((RoundScores)_scores[round-1]).PreventedIncidents);
				case "Recurring Failures":
					return CONVERT.ToStr(((RoundScores)_scores[round-1]).RecurringIncidents);
				case "Workarounds":
					return CONVERT.ToStr(((RoundScores)_scores[round-1]).NumWorkarounds);
				case "SLA Breaches":
					return CONVERT.ToStr(((RoundScores)_scores[round-1]).NumSLAbreaches);
				case "Maturity Indicator":
					return CONVERT.ToStr(((RoundScores)_scores[round-1]).IndicatorScore);

				case "Customer Complaints":
					return CONVERT.ToStr(((RoundScores) scores).CustomerComplaints);

				case "Customer Satisfaction":
					//use the default 
					return GetMaturityVal(round, series);

				default:
					return GetMaturityVal(round, series);
			}
		}

		protected string GetMaturityVal(int round, string series)
		{
			foreach(string section in ((RoundScores)_scores[round-1]).outer_sections.Keys)
			{
				ArrayList points = (ArrayList)((RoundScores)_scores[round-1]).outer_sections[section];

				foreach (string pt in points)
				{
					string[] vals = pt.Split(':');

					if (vals[0] == series )
					{
						return vals[1];
					}
				}
			}
			return "0";
		}

		protected void SetData(BasicXmlDocument xdoc, XmlNode data, string series, int round)
		{
			for (int i=1; i<=round; i++)
			{
				if (_scores.Count >= i)
				{
					XmlNode pnt = (XmlNode) xdoc.CreateElement("p");
					((XmlElement)pnt).SetAttribute( "x", CONVERT.ToStr(i) );
					((XmlElement)pnt).SetAttribute( "y", GetValue(i, series) );
					data.AppendChild(pnt);
				}
				else
				{
					XmlNode pnt = (XmlNode) xdoc.CreateElement("p");
					((XmlElement)pnt).SetAttribute( "x", CONVERT.ToStr(i) );
					((XmlElement)pnt).SetAttribute( "y", "0" );
					data.AppendChild(pnt);
				}
			}
		}

		public virtual string BuildReport (NetworkProgressionGameFile gameFile, int round, string Series1, string Series2, ArrayList Scores)
		{
			return BuildReport(gameFile, round, Series1, Series2, Scores, null);
		}

		public virtual string BuildReport(NetworkProgressionGameFile gameFile, int round, string Series1, string Series2, ArrayList Scores, Color[] stripeColours)
		{
			_scores = Scores;

			//Create the xml report
			string reportFile = gameFile.GetRoundFile(gameFile.LastRoundPlayed/*.CurrentRound*/,"OpsComparisonReport_Round" + round + ".xml" , gameFile.LastPhasePlayed);//.CurrentPhase);

			LibCore.BasicXmlDocument xdoc = LibCore.BasicXmlDocument.Create();
			XmlNode root = (XmlNode) xdoc.CreateElement("graph");
			if (showkey)
			{
				((XmlElement)root).SetAttribute("show_key", "true");
				//root.SetAttribute("show_key", "true");
			}
			xdoc.AppendChild(root);

			XmlNode xtitle = (XmlNode) xdoc.CreateElement("xAxis");
			((XmlElement)xtitle).SetAttribute( "minMaxSteps","1," + SkinningDefs.TheInstance.GetIntData("roundcount", 5) + ",1" );
			((XmlElement)xtitle).SetAttribute( "autoScale","false" );
			((XmlElement)xtitle).SetAttribute( "title","Round" );
			root.AppendChild(xtitle);
			//
			XmlNode yaxis = CreateLeftAxis(xdoc, left_colour, axisTextColour, Series1);
			if (stripeColours != null)
			{
				((XmlElement) yaxis).SetAttribute("stripes", CollapseColourArray(stripeColours));
			}
			root.AppendChild(yaxis);

			if (IsValidSeries(Series2))
			{
				yaxis = CreateRightAxis(xdoc, right_colour, axisTextColour, Series2);
				if (stripeColours != null)
				{
					((XmlElement) yaxis).SetAttribute("stripes", CollapseColourArray(stripeColours));
				}
				root.AppendChild(yaxis);
			}

			string series1_title = Series1.Replace("Global.", "");
			
			XmlNode data = (XmlNode) xdoc.CreateElement("data");
		    ((XmlElement) data).SetAttribute("yscale", "left");
			((XmlElement)data).SetAttribute( "thickness", "3");
			if (series1_title != "")
			{
				((XmlElement)data).SetAttribute("title", series1_title);
			}
			((XmlElement)data).SetAttribute( "colour",left_colour );
			root.AppendChild(data);

			SetData(xdoc, data, Series1, round);

			if (IsValidSeries(Series2))
			{
				string series2_title = Series2.Replace("Global.", "");

				data = (XmlNode) xdoc.CreateElement("data");
				((XmlElement)data).SetAttribute( "yscale","right");
				((XmlElement)data).SetAttribute("title", series2_title);
				((XmlElement)data).SetAttribute( "colour", right_colour );
			    ((XmlElement) data).SetAttribute("thickness", "3");
				root.AppendChild(data);

				SetData(xdoc, data, Series2, round);
			}
			
			xdoc.SaveToURL("",reportFile);
			return reportFile;
		}

		protected virtual XmlElement CreateLeftAxis (XmlDocument xml, string colour, string textColour, string metric)
		{
			XmlElement axis = xml.CreateElement("yLeftAxis");
			axis.SetAttribute("colour", colour);
			axis.SetAttribute("textcolour", textColour);
			axis.SetAttribute("title", GetAxisTitle(metric));
			axis.SetAttribute("autoScale", "true");

			return axis;
		}

		protected virtual XmlElement CreateRightAxis (XmlDocument xml, string colour, string textColour, string metric)
		{
			XmlElement axis = xml.CreateElement("yRightAxis");
			axis.SetAttribute("colour", colour);
			axis.SetAttribute("textcolour", textColour);
			axis.SetAttribute("title", GetAxisTitle(metric));
			axis.SetAttribute("autoScale", "true");

			return axis;
		}

		protected virtual bool IsValidSeries (string metric)
		{
			return (metric.ToUpper() != "NONE");
		}

		protected virtual string GetAxisTitle (string metric)
		{
			return metric;
		}

		protected string CollapseColourArray (Color [] colours)
		{
			string output = "";

			foreach (Color colour in colours)
			{
				if (output.Length > 0)
				{
					output += ",";
				}

				output += CONVERT.Format("{0},{1},{2}", colour.R, colour.G, colour.B);
			}

			return output;
		}
	}
}