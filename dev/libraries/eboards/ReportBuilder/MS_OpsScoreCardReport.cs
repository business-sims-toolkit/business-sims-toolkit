using System.Xml;
using LibCore;
using GameManagement;
using CoreUtils;

namespace ReportBuilder
{
	/// <summary>
	/// Summary description for MS_OpsScoreCardReport.
	/// </summary>
	public class MS_OpsScoreCardReport
	{
		static int NUMROUNDS = 5;

		string transactionName =  SkinningDefs.TheInstance.GetData("transactionname");

		protected string header_colour = "176,196,222";
		protected string text_colour = "0,0,0";
		protected string table_border_color = "211,211,211";

		public MS_OpsScoreCardReport()
		{
			string hcol = CoreUtils.SkinningDefs.TheInstance.GetData("table_header_color");
			if(hcol != "")
			{
				header_colour = hcol;
			}
			//
			hcol = CoreUtils.SkinningDefs.TheInstance.GetData("table_header_text_color");
			if(hcol != "")
			{
				text_colour = hcol;
			}
			//
			hcol = CoreUtils.SkinningDefs.TheInstance.GetData("table_border_color");
			if(hcol != "")
			{
				table_border_color = hcol;
			}
		}

		public string BuildReport(NetworkProgressionGameFile gameFile, int round, RoundScores scores)
		{

			//Create the interim xml doc
			LibCore.BasicXmlDocument xdoc = LibCore.BasicXmlDocument.Create();
			XmlNode root = (XmlNode) xdoc.CreateElement("scorecard");
			xdoc.AppendChild(root);

			XmlNode cost = (XmlNode) xdoc.CreateElement("cost");
			((XmlElement)cost).SetAttribute( "name","points" );
			((XmlElement)cost).SetAttribute( "val", CONVERT.ToStr(scores.Points));
			((XmlElement)cost).SetAttribute( "sum", "true" );
			root.AppendChild(cost);

			cost = (XmlNode) xdoc.CreateElement("cost");
			((XmlElement)cost).SetAttribute( "name","revenue" );
			double tmp = scores.Revenue / 1000000.0;
			((XmlElement)cost).SetAttribute( "val", CONVERT.ToPaddedStr(tmp,2) );
			((XmlElement)cost).SetAttribute( "sum", "true" );
			((XmlElement)cost).SetAttribute( "numberformat", "currency-nosymbol" );
			root.AppendChild(cost);

			cost = (XmlNode) xdoc.CreateElement("cost");
			((XmlElement)cost).SetAttribute( "name","fixedcosts" );
			tmp = scores.FixedCosts / 1000000.0;
			((XmlElement)cost).SetAttribute( "val", CONVERT.ToPaddedStr(tmp, 2));
			((XmlElement) cost).SetAttribute("numberformat", "currency-nosymbol");
			((XmlElement)cost).SetAttribute( "sum", "true" );
			root.AppendChild(cost);

			cost = (XmlNode) xdoc.CreateElement("cost");
			((XmlElement)cost).SetAttribute( "name","projectcosts" );
			tmp = scores.ProjectSpend / 1000000.0;
			((XmlElement)cost).SetAttribute( "val", CONVERT.ToPaddedStr(tmp, 2));
			((XmlElement) cost).SetAttribute("numberformat", "currency-nosymbol");
			((XmlElement)cost).SetAttribute( "sum", "true" );
			root.AppendChild(cost);

			cost = (XmlNode) xdoc.CreateElement("cost");
			((XmlElement)cost).SetAttribute( "name","profitloss" );
			tmp = scores.Profit / 1000000.0;
			((XmlElement)cost).SetAttribute( "val", CONVERT.ToPaddedStr(tmp, 2));
			((XmlElement) cost).SetAttribute("numberformat", "currency-nosymbol");
			((XmlElement)cost).SetAttribute( "sum", "true" );
			root.AppendChild(cost);

			cost = (XmlNode) xdoc.CreateElement("cost");
			((XmlElement)cost).SetAttribute( "name","gainloss" );
			tmp = scores.Gain / 1000000.0;
			((XmlElement)cost).SetAttribute( "val", CONVERT.ToPaddedStr(tmp, 2));
			((XmlElement) cost).SetAttribute("numberformat", "currency-nosymbol");
			root.AppendChild(cost);

			cost = (XmlNode) xdoc.CreateElement("cost");
			((XmlElement)cost).SetAttribute( "name","availability" );
			((XmlElement)cost).SetAttribute( "val", CONVERT.ToPaddedStr(scores.Availability, 2));
			((XmlElement)cost).SetAttribute( "numberformat", "percentage" );
			((XmlElement)cost).SetAttribute( "average", "true" );
			root.AppendChild(cost);

			cost = (XmlNode) xdoc.CreateElement("cost");
			
			((XmlElement)cost).SetAttribute( "name","mttr" );
			int mins = (int)scores.MTTR / 60;
			int secs = (int)scores.MTTR - (60 * mins);
			string secstr = "";
			if (secs < 10) secstr = "0" + CONVERT.ToStr(secs);
			else secstr = CONVERT.ToStr(secs);
			((XmlElement)cost).SetAttribute( "val", CONVERT.ToStr(mins) + ":" + secstr);
			root.AppendChild(cost);

			cost = (XmlNode) xdoc.CreateElement("cost");
			((XmlElement)cost).SetAttribute( "name","totalincidents" );
			((XmlElement)cost).SetAttribute( "val", CONVERT.ToStr(scores.Incidents));
			root.AppendChild(cost);

			cost = (XmlNode) xdoc.CreateElement("cost");
			((XmlElement)cost).SetAttribute( "name","preventedincidents" );
			((XmlElement)cost).SetAttribute( "val", CONVERT.ToStr(scores.PreventedIncidents));
			root.AppendChild(cost);

            if (SkinningDefs.TheInstance.GetBoolData("allow_first_line_fix", true))
            {
                cost = (XmlNode)xdoc.CreateElement("cost");
                ((XmlElement)cost).SetAttribute("name", "firstLineFixes");
                ((XmlElement)cost).SetAttribute("val", CONVERT.ToStr(scores.FirstLineFixes));
                root.AppendChild(cost);
            }


			cost = (XmlNode) xdoc.CreateElement("cost");
			((XmlElement)cost).SetAttribute( "name","recurringincidents" );
			((XmlElement)cost).SetAttribute( "val", CONVERT.ToStr(scores.RecurringIncidents));
			root.AppendChild(cost);

			cost = (XmlNode) xdoc.CreateElement("cost");
			((XmlElement)cost).SetAttribute( "name","workarounds" );
			((XmlElement)cost).SetAttribute( "val", CONVERT.ToStr(scores.NumWorkarounds));
			root.AppendChild(cost);

			cost = (XmlNode) xdoc.CreateElement("cost");
			((XmlElement)cost).SetAttribute( "name", "supportbudget" );
			tmp = scores.SupportBudget / 1000000.0;
			((XmlElement)cost).SetAttribute( "val", CONVERT.ToPaddedStr(tmp,2));
			((XmlElement) cost).SetAttribute("numberformat", "currency-nosymbol");
			((XmlElement)cost).SetAttribute( "sum", "true" );
			root.AppendChild(cost);

			cost = (XmlNode) xdoc.CreateElement("cost");
			((XmlElement)cost).SetAttribute( "name", "supportspend" );
			tmp = scores.SupportCostsTotal / 1000000.0;
			((XmlElement)cost).SetAttribute( "val", CONVERT.ToPaddedStr(tmp,2));
			((XmlElement) cost).SetAttribute("numberformat", "currency-nosymbol");
			((XmlElement)cost).SetAttribute( "sum", "true" );
			root.AppendChild(cost);

			cost = (XmlNode) xdoc.CreateElement("cost");
			((XmlElement)cost).SetAttribute( "name", "supportprofit" );
			tmp = scores.SupportProfit / 1000000.0;
			((XmlElement)cost).SetAttribute( "val", CONVERT.ToPaddedStr(tmp,2));
			((XmlElement)cost).SetAttribute( "numberformat", "currency-nosymbol" );
			((XmlElement)cost).SetAttribute( "sum", "true" );
			root.AppendChild(cost);

			cost = (XmlNode) xdoc.CreateElement("cost");
			((XmlElement)cost).SetAttribute( "name","slabreaches" );
			((XmlElement)cost).SetAttribute( "val", CONVERT.ToStr(scores.NumSLAbreaches));
			root.AppendChild(cost);

			cost = (XmlNode) xdoc.CreateElement("cost");
			((XmlElement)cost).SetAttribute( "name", "fines" );
			tmp = scores.RegulationFines / 1000000.0;
			((XmlElement)cost).SetAttribute( "val", CONVERT.ToPaddedStr(tmp,2));
			((XmlElement) cost).SetAttribute("numberformat", "currency-nosymbol");
			((XmlElement)cost).SetAttribute( "sum", "true" );
			root.AppendChild(cost);

			cost = (XmlNode) xdoc.CreateElement("cost");
			((XmlElement)cost).SetAttribute( "name", "compliance" );
			tmp = scores.ComplianceFines / 1000000.0;
			((XmlElement)cost).SetAttribute( "val", CONVERT.ToPaddedStr(tmp,2));
			((XmlElement) cost).SetAttribute("numberformat", "currency-nosymbol");
			((XmlElement)cost).SetAttribute( "sum", "true" );
			root.AppendChild(cost);

			cost = (XmlNode) xdoc.CreateElement("cost");
			((XmlElement)cost).SetAttribute( "name","numservicesbeforerace" );
			((XmlElement)cost).SetAttribute( "val", CONVERT.ToStr(scores.NumServicesBeforeRace));
			root.AppendChild(cost);

			cost = (XmlNode) xdoc.CreateElement("cost");
			((XmlElement)cost).SetAttribute( "name","numservices" );
			((XmlElement)cost).SetAttribute( "val", CONVERT.ToStr(scores.NumServices));
			root.AppendChild(cost);
			
			cost = (XmlNode) xdoc.CreateElement("cost");
			((XmlElement)cost).SetAttribute( "name","maxtrans" );
			((XmlElement)cost).SetAttribute( "val", CONVERT.ToStr(scores.MaxTransactions));
			((XmlElement)cost).SetAttribute( "sum", "true" );
			root.AppendChild(cost);

			cost = (XmlNode) xdoc.CreateElement("cost");
			((XmlElement)cost).SetAttribute( "name","transactions" );
			((XmlElement)cost).SetAttribute( "val", CONVERT.ToStr(scores.NumTransactions));
			((XmlElement)cost).SetAttribute( "sum", "true" );
			root.AppendChild(cost);

			cost = (XmlNode) xdoc.CreateElement("cost");
			((XmlElement)cost).SetAttribute( "name","maxrev" );
			tmp = scores.MaxRevenue / 1000000.0;
			((XmlElement)cost).SetAttribute( "val", CONVERT.ToPaddedStr(tmp,2));
			((XmlElement) cost).SetAttribute("numberformat", "currency-nosymbol");
			((XmlElement)cost).SetAttribute( "sum", "true" );
			root.AppendChild(cost);


			return xdoc.InnerXml;
		}

		public string aggregateResults(string[] roundresults, NetworkProgressionGameFile gameFile, int round, RoundScores scores)
		{
			//Create the xml report
			string reportFile = gameFile.GetRoundFile(gameFile.LastRoundPlayed, "OpsScoreCardReport_Round" + round + ".xml" , gameFile.LastPhasePlayed);

			int i = 0;
			int rounds = roundresults.Length;
				
			LibCore.BasicXmlDocument[] xmldata = new LibCore.BasicXmlDocument[rounds];

			foreach(string s in roundresults)
			{
				if (s != string.Empty && s != null)
				{
					xmldata[i++] = LibCore.BasicXmlDocument.Create(s);
				}
			}	
			
			int NumColumns = NUMROUNDS + 2;
			string RowHeight = "21";

			string colwidths = "0.4,0.1,0.1,0.1,0.1,0.1,0.1";

			LibCore.BasicXmlDocument xdoc = LibCore.BasicXmlDocument.Create();
			XmlNode root = (XmlNode) xdoc.CreateElement("table");
			((XmlElement)root).SetAttribute( "columns","1" );
			((XmlElement)root).SetAttribute( "rowheight",RowHeight );
			((XmlElement)root).SetAttribute( "border_colour", table_border_color );
			string rowColour = SkinningDefs.TheInstance.GetData("table_row_colour", "255,255,255");
			((XmlElement)root).SetAttribute( "row_colour_1", rowColour);
			((XmlElement)root).SetAttribute( "row_colour_2", SkinningDefs.TheInstance.GetData("table_row_colour_alternate", rowColour));
			
			xdoc.AppendChild(root);

			//add the title table
			XmlNode titles = (XmlNode) xdoc.CreateElement("table");
			((XmlElement)titles).SetAttribute( "columns", CONVERT.ToStr(NumColumns) );
			((XmlElement)titles).SetAttribute( "widths",colwidths );
			((XmlElement)titles).SetAttribute( "border_colour", table_border_color );
			rowColour = SkinningDefs.TheInstance.GetData("table_row_colour", "255,255,255");
			((XmlElement)titles).SetAttribute( "row_colour_1", rowColour);
			((XmlElement)titles).SetAttribute( "row_colour_2", SkinningDefs.TheInstance.GetData("table_row_colour_alternate", rowColour));
			root.AppendChild(titles);

			XmlNode titlerow = (XmlNode) xdoc.CreateElement("rowdata");
			titles.AppendChild(titlerow);

			XmlNode cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement)cell).SetAttribute( "val","" );
			titlerow.AppendChild(cell);
			for (int j=1; j<=NUMROUNDS; j++)
			{
				cell = (XmlNode) xdoc.CreateElement("cell");
				((XmlElement)cell).SetAttribute( "val", CONVERT.ToStr(j));
				titlerow.AppendChild(cell);
			}
			cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement)cell).SetAttribute( "val","Total" );
			titlerow.AppendChild(cell);

			
			//add a title row 
			XmlNode row = (XmlNode) xdoc.CreateElement("rowdata");
			root.AppendChild(row);
			cell = (XmlNode) xdoc.CreateElement("cell");
			string name = "Business Expenditure";
			if (SkinningDefs.TheInstance.GetIntData("reports_use_numbered_sections", 1) == 1)
			{
				name = "1 - " + name;
			}
			((XmlElement)cell).SetAttribute( "val", name);
			((XmlElement)cell).SetAttribute( "align","middle" );
			((XmlElement)cell).SetAttribute( "colour",header_colour );
			((XmlElement)cell).SetAttribute( "textcolour",text_colour );
			if (SkinningDefs.TheInstance.GetIntData("table_header_bold", 1) != 0)
			{
				((XmlElement)cell).SetAttribute( "textstyle", "bold" );
			}
			row.AppendChild(cell);

			//add business performance table
			XmlNode bustable = (XmlNode) xdoc.CreateElement("table");
			((XmlElement)bustable).SetAttribute( "columns", CONVERT.ToStr(NumColumns) );
			((XmlElement)bustable).SetAttribute( "widths",colwidths );
			((XmlElement)bustable).SetAttribute( "rowheight", RowHeight );
			((XmlElement)bustable).SetAttribute( "border_colour", table_border_color );
			rowColour = SkinningDefs.TheInstance.GetData("table_row_colour", "255,255,255");
			((XmlElement)bustable).SetAttribute( "row_colour_1", rowColour);
			((XmlElement)bustable).SetAttribute( "row_colour_2", SkinningDefs.TheInstance.GetData("table_row_colour_alternate", rowColour));
			root.AppendChild(bustable);

			row = (XmlNode) xdoc.CreateElement("rowdata");
			bustable.AppendChild(row);
			cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement)cell).SetAttribute( "val", transactionName + " Handled" );
			row.AppendChild(cell);
			setRow(xdoc, row, xmldata, "transactions", round);

			row = (XmlNode) xdoc.CreateElement("rowdata");
			bustable.AppendChild(row);
			cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement)cell).SetAttribute( "val", SkinningDefs.TheInstance.GetData("maxname", "Max") + " " + transactionName );
			row.AppendChild(cell);
			setRow(xdoc, row, xmldata, "maxtrans", round);

			row = (XmlNode) xdoc.CreateElement("rowdata");
			bustable.AppendChild(row);
			cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement)cell).SetAttribute("val", SkinningDefs.TheInstance.GetData("maxname", "Max") + " Revenue ($M)");
			row.AppendChild(cell);
			setRow(xdoc, row, xmldata, "maxrev", round);

			row = (XmlNode) xdoc.CreateElement("rowdata");
			bustable.AppendChild(row);
			cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement)cell).SetAttribute("val", "Actual Revenue ($M)");
			row.AppendChild(cell);
			setRow(xdoc, row, xmldata, "revenue", round);


			row = (XmlNode) xdoc.CreateElement("rowdata");
			bustable.AppendChild(row);
			cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement)cell).SetAttribute("val", "Fixed Costs ($M)");
			row.AppendChild(cell);
			setRow(xdoc, row, xmldata, "fixedcosts", round);

			row = (XmlNode) xdoc.CreateElement("rowdata");
			bustable.AppendChild(row);
			cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement)cell).SetAttribute("val", "Support Budget ($M)");
			row.AppendChild(cell);
			setRow(xdoc, row, xmldata, "supportbudget", round);

			row = (XmlNode) xdoc.CreateElement("rowdata");
			bustable.AppendChild(row);
			cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement)cell).SetAttribute("val", "New Service Costs ($M)");
			row.AppendChild(cell);
			setRow(xdoc, row, xmldata, "projectcosts", round);

			row = (XmlNode) xdoc.CreateElement("rowdata");
			bustable.AppendChild(row);
			cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement)cell).SetAttribute("val", "Regulation Fines ($M)");
			row.AppendChild(cell);
			setRow(xdoc, row, xmldata, "fines", round);

			row = (XmlNode) xdoc.CreateElement("rowdata");
			bustable.AppendChild(row);
			cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement)cell).SetAttribute("val", "Compliance Fines ($M)");
			row.AppendChild(cell);
			setRow(xdoc, row, xmldata, "compliance", round);

			row = (XmlNode) xdoc.CreateElement("rowdata");
			bustable.AppendChild(row);
			cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement)cell).SetAttribute("val", "Profit / Loss ($M)");
			row.AppendChild(cell);
			setRow(xdoc, row, xmldata, "profitloss", round);

			row = (XmlNode) xdoc.CreateElement("rowdata");
			bustable.AppendChild(row);
			cell = (XmlNode) xdoc.CreateElement("cell");
            ((XmlElement)cell).SetAttribute("val", SkinningDefs.TheInstance.GetData("gainlossname", "Improvement On Previous Round ($M)"));
			row.AppendChild(cell);
			setRow(xdoc, row, xmldata, "gainloss", round);

			//add a title row 
			row = (XmlNode) xdoc.CreateElement("rowdata");
			root.AppendChild(row);
			cell = (XmlNode) xdoc.CreateElement("cell");
			name = "IT Expenditure";
			if (SkinningDefs.TheInstance.GetIntData("reports_use_numbered_sections", 1) == 1)
			{
				name = "2 - " + name;
			}
			((XmlElement)cell).SetAttribute( "val", name);
			((XmlElement)cell).SetAttribute( "align","middle" );
			((XmlElement)cell).SetAttribute( "colour",header_colour );
			((XmlElement)cell).SetAttribute( "textcolour",text_colour );
			if (SkinningDefs.TheInstance.GetIntData("table_header_bold", 1) != 0)
			{
				((XmlElement)cell).SetAttribute( "textstyle", "bold" );
			}
			row.AppendChild(cell);

			//add IT performance table
			XmlNode supporttable = (XmlNode) xdoc.CreateElement("table");
			((XmlElement)supporttable).SetAttribute( "columns", CONVERT.ToStr(NumColumns) );
			((XmlElement)supporttable).SetAttribute( "widths",colwidths );
			((XmlElement)supporttable).SetAttribute( "rowheight", RowHeight );
			((XmlElement)supporttable).SetAttribute( "border_colour", table_border_color );
			rowColour = SkinningDefs.TheInstance.GetData("table_row_colour", "255,255,255");
			((XmlElement)supporttable).SetAttribute( "row_colour_1", rowColour);
			((XmlElement)supporttable).SetAttribute( "row_colour_2", SkinningDefs.TheInstance.GetData("table_row_colour_alternate", rowColour));
			root.AppendChild(supporttable);

			row = (XmlNode) xdoc.CreateElement("rowdata");
			supporttable.AppendChild(row);
			cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement)cell).SetAttribute("val", "Support Budget ($M)");
			row.AppendChild(cell);
			setRow(xdoc, row, xmldata, "supportbudget", round);

			row = (XmlNode) xdoc.CreateElement("rowdata");
			supporttable.AppendChild(row);
			cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement)cell).SetAttribute("val", "Support Spend ($M)");
			row.AppendChild(cell);
			setRow(xdoc, row, xmldata, "supportspend", round);

			row = (XmlNode) xdoc.CreateElement("rowdata");
			supporttable.AppendChild(row);
			cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement)cell).SetAttribute("val", "Profit / Loss ($M)");
			row.AppendChild(cell);
			setRow(xdoc, row, xmldata, "supportprofit", round);


			//add a title row 
			row = (XmlNode) xdoc.CreateElement("rowdata");
			root.AppendChild(row);
			cell = (XmlNode) xdoc.CreateElement("cell");
			name = "IT Performance";
			if (SkinningDefs.TheInstance.GetIntData("reports_use_numbered_sections", 1) == 1)
			{
				name = "3 - " + name;
			}
			((XmlElement)cell).SetAttribute( "val", name);
			((XmlElement)cell).SetAttribute( "align","middle" );
			((XmlElement)cell).SetAttribute( "colour",header_colour );
			((XmlElement)cell).SetAttribute( "textcolour",text_colour );
			if (SkinningDefs.TheInstance.GetIntData("table_header_bold", 1) != 0)
			{
				((XmlElement)cell).SetAttribute( "textstyle", "bold" );
			}
			row.AppendChild(cell);

			//add IT performance table
			XmlNode ittable = (XmlNode) xdoc.CreateElement("table");
			((XmlElement)ittable).SetAttribute( "columns", CONVERT.ToStr(NumColumns) );
			((XmlElement)ittable).SetAttribute( "widths",colwidths );
			((XmlElement)ittable).SetAttribute( "rowheight", RowHeight );
			((XmlElement)ittable).SetAttribute( "border_colour", table_border_color );
			rowColour = SkinningDefs.TheInstance.GetData("table_row_colour", "255,255,255");
			((XmlElement)ittable).SetAttribute( "row_colour_1", rowColour);
			((XmlElement)ittable).SetAttribute( "row_colour_2", SkinningDefs.TheInstance.GetData("table_row_colour_alternate", rowColour));
			root.AppendChild(ittable);

			row = (XmlNode) xdoc.CreateElement("rowdata");
			ittable.AppendChild(row);
			cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement)cell).SetAttribute( "val","New Services Implemented" );
			row.AppendChild(cell);
			setRow(xdoc, row, xmldata, "numservices", round);

			row = (XmlNode) xdoc.CreateElement("rowdata");
			ittable.AppendChild(row);
			cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement)cell).SetAttribute( "val","New Services Implemented Before Round" );
			row.AppendChild(cell);
			setRow(xdoc, row, xmldata, "numservicesbeforerace", round);

			row = (XmlNode) xdoc.CreateElement("rowdata");
			ittable.AppendChild(row);
			cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement)cell).SetAttribute( "val","Availability" );
			row.AppendChild(cell);
			setRow(xdoc, row, xmldata, "availability", round);

			row = (XmlNode) xdoc.CreateElement("rowdata");
			ittable.AppendChild(row);
			cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement)cell).SetAttribute("val", SkinningDefs.TheInstance.GetData("mtrsname", "Mean Time to Restore Service"));
			row.AppendChild(cell);
			setRow(xdoc, row, xmldata, "mttr", round);

			row = (XmlNode) xdoc.CreateElement("rowdata");
			ittable.AppendChild(row);
			cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement)cell).SetAttribute( "val","Total Failures" );
			row.AppendChild(cell);
			setRow(xdoc, row, xmldata, "totalincidents", round);

			row = (XmlNode) xdoc.CreateElement("rowdata");
			ittable.AppendChild(row);
			cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement)cell).SetAttribute( "val","Prevented Failures" );
			row.AppendChild(cell);
			setRow(xdoc, row, xmldata, "preventedincidents", round);


            if (SkinningDefs.TheInstance.GetBoolData("allow_first_line_fix", true))
            {
                row = (XmlNode)xdoc.CreateElement("rowdata");
                ittable.AppendChild(row);
                cell = (XmlNode)xdoc.CreateElement("cell");
                ((XmlElement)cell).SetAttribute("val", "First Line Fixes");
                row.AppendChild(cell);
                setRow(xdoc, row, xmldata, "firstLineFixes", round);
            }
			
			row = (XmlNode) xdoc.CreateElement("rowdata");
			ittable.AppendChild(row);
			cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement)cell).SetAttribute( "val","Recurring Failures" );
			row.AppendChild(cell);
			setRow(xdoc, row, xmldata, "recurringincidents", round);

			row = (XmlNode) xdoc.CreateElement("rowdata");
			ittable.AppendChild(row);
			cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement)cell).SetAttribute( "val","Workarounds" );
			row.AppendChild(cell);
			setRow(xdoc, row, xmldata, "workarounds", round);

			row = (XmlNode) xdoc.CreateElement("rowdata");
			ittable.AppendChild(row);
			cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement)cell).SetAttribute( "val","SLA Breaches" );
			row.AppendChild(cell);
			setRow(xdoc, row, xmldata, "slabreaches", round);

			xdoc.SaveToURL("",reportFile);

			return reportFile;
		}

		void setRow(BasicXmlDocument xdoc, XmlNode row, BasicXmlDocument[] xmldata, string costType, int round)
		{
			XmlNode cell;
			double total = 0;
			bool showtotal = false;
			string totalformat = "";
			double average_total = 0;
			int average_num = 0;
			for(int x = 0; x < NUMROUNDS; x++)
			{
				cell = (XmlNode) xdoc.CreateElement("cell");
				if(x < round)
				{
					if (xmldata[x] == null) continue;

					totalformat = "";
					foreach(XmlNode node in xmldata[x].ChildNodes)
					{
						foreach(XmlNode child in node)
						{
							if(child.Attributes["name"].Value == costType)
							{
								cell = (XmlNode) xdoc.CreateElement("cell");

								string val = child.Attributes["val"].Value;

								//add to the total?
								if(child.Attributes["sum"] != null)
								{	
						
									total += CONVERT.ParseDouble(val);
									showtotal = true;
								}
								if (child.Attributes["average"] != null)
								{
									average_total += CONVERT.ParseDouble(val);
									average_num++;
								}

								//work out what type of number this is
								string nf = string.Empty;
								if (child.Attributes["numberformat"] != null)
								{
									nf = child.Attributes["numberformat"].Value;
									totalformat = nf;
									switch (nf)
									{
										case "currency":
										case "currency-nosymbol":
											{
												string symbol = "$";
												if (nf == "currency-nosymbol")
												{
													symbol = "";

												}
												double tmp = CONVERT.ParseDouble(val);
												if (tmp < 0)
												{
													tmp = tmp * (-1);
													val = "(" + symbol + CONVERT.ToPaddedStr(tmp, 2) + ")";
													((XmlElement) cell).SetAttribute("textcolour", "255,0,0");
												}
												else
												{
													val = symbol + val;
												}
											}
											break;

										case "percentage":
											val = val + "%";
											break;
									}
								}

								((XmlElement)cell).SetAttribute( "val", val );
								row.AppendChild(cell);
							}
						}
					}
				}
				else
					((XmlElement)cell).SetAttribute( "val", " " );

				row.AppendChild(cell);
			}
			if (showtotal)
			{
				cell = (XmlNode) xdoc.CreateElement("cell");

				string totalstr = "";
				switch (totalformat)
				{
					case "currency":
					case "currency-nosymbol":
						{
							string symbol = "$";
							if (totalformat == "currency-nosymbol")
							{
								symbol = "";
							}
							if (total < 0)
							{
								total = total * (-1);
								totalstr = "(" + symbol + CONVERT.ToPaddedStr(total, 2) + ")";
                                ((XmlElement)cell).SetAttribute("textcolour", "255,0,0");
							}
							else
							{
								totalstr = symbol + CONVERT.ToPaddedStr(total, 2);
							}
						}
						break;

					case "percentage":
						totalstr = CONVERT.ToPaddedStr(total, 2) + "%";
						break;

					default:
						totalstr = CONVERT.ToStr(total);
						break;
				}
				((XmlElement)cell).SetAttribute( "val", totalstr );

				row.AppendChild(cell);
			}
			if (average_num > 0)
			{
				string avgstr = "";
				double tmp = average_total / average_num;
				if (totalformat == "percentage") avgstr = CONVERT.ToPaddedStr(tmp,2) + "%";
				else avgstr = "$" + CONVERT.ToStr(tmp);

				cell = (XmlNode) xdoc.CreateElement("cell");
				((XmlElement)cell).SetAttribute( "val", avgstr );
				row.AppendChild(cell);

			}
		}
	}
}
