using System.Drawing;
using System.Xml;
using System.Text;

using LibCore;
using GameManagement;
using CoreUtils;

namespace ReportBuilder
{
	/// <summary>
	/// Summary description for PS_OpsScoreCardReport.
	/// </summary>
	public class PS_OpsScoreCardReport
	{
		static int NUMROUNDS = SkinningDefs.TheInstance.GetIntData("roundcount", 5);

		string transactionName =  SkinningDefs.TheInstance.GetData("transactionname");
		bool use_transaction_targets = SkinningDefs.TheInstance.GetBoolData("use_target_transactions_system", false);
		bool use_transaction_targets_data_exists = false;

        protected string header_colour = SkinningDefs.TheInstance.GetData("table_header_colour", SkinningDefs.TheInstance.GetData("fis_reports_green_header_colour", "176,196,222"));
		protected string text_colour = "0,0,0";
	    Color table_border_color = Color.FromArgb(211, 211, 211);

	    public PS_OpsScoreCardReport ()
	    {
	        string hcol = CoreUtils.SkinningDefs.TheInstance.GetData("table_header_color");
	        if (hcol != "")
	        {
	            header_colour = hcol;
	        }
	        //
	        hcol = CoreUtils.SkinningDefs.TheInstance.GetData("table_header_text_color");
	        if (hcol != "")
	        {
	            text_colour = hcol;
	        }
	        //
	        hcol = CoreUtils.SkinningDefs.TheInstance.GetData("table_border_color", null);
	        if (hcol != null)
	        {
	            if (hcol == "")
	            {
	                table_border_color = Color.Transparent;
	            }
	            else
	            {
	                table_border_color = CONVERT.ParseComponentColor(hcol);
	            }
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
			((XmlElement)cost).SetAttribute( "val", CONVERT.ToPaddedStr(tmp, 2));
			((XmlElement)cost).SetAttribute( "sum", "true" );
			((XmlElement)cost).SetAttribute( "numberformat", "currency" );
			root.AppendChild(cost);

			cost = (XmlNode) xdoc.CreateElement("cost");
			((XmlElement)cost).SetAttribute( "name","fixedcosts" );
			tmp = scores.FixedCosts / 1000000.0;
			((XmlElement)cost).SetAttribute( "val", CONVERT.ToPaddedStr(tmp, 2));
			((XmlElement)cost).SetAttribute( "numberformat", "currency" );
			((XmlElement)cost).SetAttribute( "sum", "true" );
			root.AppendChild(cost);

			cost = (XmlNode) xdoc.CreateElement("cost");
			((XmlElement)cost).SetAttribute( "name","projectcosts" );
			tmp = scores.ProjectSpend / 1000000.0;
			((XmlElement)cost).SetAttribute( "val", CONVERT.ToPaddedStr(tmp, 2));
			((XmlElement)cost).SetAttribute( "numberformat", "currency" );
			((XmlElement)cost).SetAttribute( "sum", "true" );
			root.AppendChild(cost);

			cost = (XmlNode) xdoc.CreateElement("cost");
			((XmlElement)cost).SetAttribute( "name","profitloss" );
			tmp = scores.Profit / 1000000.0;
			((XmlElement)cost).SetAttribute( "val", CONVERT.ToPaddedStr(tmp, 2));
			((XmlElement)cost).SetAttribute( "numberformat", "currency" );
			((XmlElement)cost).SetAttribute( "sum", "true" );
			root.AppendChild(cost);

			cost = (XmlNode) xdoc.CreateElement("cost");
			((XmlElement)cost).SetAttribute( "name","gainloss" );
			tmp = scores.Gain / 1000000.0;
			((XmlElement)cost).SetAttribute( "val", CONVERT.ToPaddedStr(tmp, 2));
			((XmlElement)cost).SetAttribute( "numberformat", "currency" );
			root.AppendChild(cost);

			cost = (XmlNode) xdoc.CreateElement("cost");
			((XmlElement)cost).SetAttribute( "name","availability" );
			((XmlElement)cost).SetAttribute( "val", CONVERT.ToStrRounded(scores.Availability, 0));
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
			((XmlElement) cost).SetAttribute("average", "true");
			((XmlElement) cost).SetAttribute("numberformat", "time");
			root.AppendChild(cost);

			cost = (XmlNode) xdoc.CreateElement("cost");
			((XmlElement)cost).SetAttribute( "name","totalincidents" );
			((XmlElement)cost).SetAttribute( "val", CONVERT.ToStr(scores.Incidents));
			((XmlElement) cost).SetAttribute("sum", "true");
			root.AppendChild(cost);

            cost = (XmlNode)xdoc.CreateElement("cost");
            ((XmlElement)cost).SetAttribute("name", "preventedincidents");
            ((XmlElement)cost).SetAttribute("val", CONVERT.ToStr(scores.PreventedIncidents));
			((XmlElement) cost).SetAttribute("sum", "true");
            root.AppendChild(cost);
           
            if(SkinningDefs.TheInstance.GetBoolData("allow_first_line_fix", true))
            {
                  cost = (XmlNode)xdoc.CreateElement("cost");
                  ((XmlElement)cost).SetAttribute("name", "firstLineFixes");
                  ((XmlElement)cost).SetAttribute("val", CONVERT.ToStr(scores.FirstLineFixes));
	            ((XmlElement) cost).SetAttribute("sum", "true");
                  root.AppendChild(cost);
            }

			cost = (XmlNode) xdoc.CreateElement("cost");
			((XmlElement)cost).SetAttribute( "name","recurringincidents" );
			((XmlElement)cost).SetAttribute( "val", CONVERT.ToStr(scores.RecurringIncidents));
			((XmlElement) cost).SetAttribute("sum", "true");
			root.AppendChild(cost);

			cost = (XmlNode) xdoc.CreateElement("cost");
			((XmlElement)cost).SetAttribute( "name","workarounds" );
			((XmlElement)cost).SetAttribute( "val", CONVERT.ToStr(scores.NumWorkarounds));
			((XmlElement) cost).SetAttribute("sum", "true");
			root.AppendChild(cost);

			cost = (XmlNode) xdoc.CreateElement("cost");
			((XmlElement)cost).SetAttribute( "name", "supportbudget" );
			tmp = scores.SupportBudget / 1000000.0;
			((XmlElement)cost).SetAttribute( "val", CONVERT.ToPaddedStr(tmp, 2));
			((XmlElement)cost).SetAttribute( "numberformat", "currency" );
			((XmlElement)cost).SetAttribute( "sum", "true" );
			root.AppendChild(cost);

			cost = (XmlNode) xdoc.CreateElement("cost");
			((XmlElement)cost).SetAttribute( "name", "supportspend" );
			tmp = scores.SupportCostsTotal / 1000000.0;
			((XmlElement)cost).SetAttribute( "val", CONVERT.ToPaddedStr(tmp, 2));
			((XmlElement)cost).SetAttribute( "numberformat", "currency" );
			((XmlElement)cost).SetAttribute( "sum", "true" );
			root.AppendChild(cost);

			cost = (XmlNode) xdoc.CreateElement("cost");
			((XmlElement) cost).SetAttribute("name", "supportfines");
			tmp = scores.SupportFinesTotal / 1000000.0;
			((XmlElement) cost).SetAttribute("val", CONVERT.ToPaddedStr(tmp, 2));
			((XmlElement) cost).SetAttribute("numberformat", "currency");
			((XmlElement) cost).SetAttribute("sum", "true");
			root.AppendChild(cost);

			cost = (XmlNode) xdoc.CreateElement("cost");
			((XmlElement)cost).SetAttribute( "name", "supportprofit" );
			tmp = scores.SupportProfit / 1000000.0;
			((XmlElement)cost).SetAttribute( "val", CONVERT.ToPaddedStr(tmp, 2));
			((XmlElement)cost).SetAttribute( "numberformat", "currency" );
			((XmlElement)cost).SetAttribute( "sum", "true" );
			root.AppendChild(cost);

			cost = (XmlNode) xdoc.CreateElement("cost");
			((XmlElement)cost).SetAttribute( "name","slabreaches" );
			((XmlElement)cost).SetAttribute( "val", CONVERT.ToStr(scores.NumSLAbreaches));
			((XmlElement) cost).SetAttribute("sum", "true");
			root.AppendChild(cost);

			cost = (XmlNode) xdoc.CreateElement("cost");
			((XmlElement)cost).SetAttribute( "name", "fines" );
			tmp = scores.RegulationFines / 1000000.0;
			((XmlElement)cost).SetAttribute( "val", CONVERT.ToPaddedStr(tmp, 2));
			((XmlElement)cost).SetAttribute( "numberformat", "currency" );
			((XmlElement)cost).SetAttribute( "sum", "true" );
			root.AppendChild(cost);

			cost = (XmlNode) xdoc.CreateElement("cost");
			((XmlElement)cost).SetAttribute( "name", "compliance" );
			tmp = (scores.ComplianceFines + scores.StockLevelFines) / 1000000.0;
			((XmlElement)cost).SetAttribute( "val", CONVERT.ToPaddedStr(tmp, 2));
			((XmlElement)cost).SetAttribute( "numberformat", "currency" );
			((XmlElement)cost).SetAttribute( "sum", "true" );
			root.AppendChild(cost);

			cost = (XmlNode) xdoc.CreateElement("cost");
			((XmlElement)cost).SetAttribute( "name","numservicesbeforerace" );
			((XmlElement)cost).SetAttribute( "val", CONVERT.ToStr(scores.NumServicesBeforeRace));
			((XmlElement) cost).SetAttribute("sum", "true");
			root.AppendChild(cost);

			cost = (XmlNode) xdoc.CreateElement("cost");
			((XmlElement)cost).SetAttribute( "name","numservices" );
			((XmlElement)cost).SetAttribute( "val", CONVERT.ToStr(scores.NumServices));
			((XmlElement) cost).SetAttribute("sum", "true");
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

			cost = (XmlNode)xdoc.CreateElement("cost");
			((XmlElement)cost).SetAttribute("name", "transtarget");
			((XmlElement) cost).SetAttribute("sum", "true");
			if (scores.TargetTransactions > 0)
			{
				((XmlElement) cost).SetAttribute("val", CONVERT.ToStr(scores.TargetTransactions));
				use_transaction_targets_data_exists = true;
			}
			else
			{
				((XmlElement) cost).SetAttribute("val", "");
			}
			root.AppendChild(cost);

			cost = (XmlNode) xdoc.CreateElement("cost");
			((XmlElement)cost).SetAttribute( "name","maxrev" );
			tmp = scores.MaxRevenue / 1000000.0;
			((XmlElement)cost).SetAttribute( "val", CONVERT.ToPaddedStr(tmp, 2));
			((XmlElement)cost).SetAttribute( "numberformat", "currency" );
			((XmlElement)cost).SetAttribute( "sum", "true" );
			root.AppendChild(cost);

			return xdoc.InnerXml;
		}

		string GetColumnWidthsString (float firstColumnWidth, int columns)
		{
			StringBuilder output = new StringBuilder();
			output.Append(CONVERT.ToStr(firstColumnWidth));

			float columnWidth = (1 - firstColumnWidth) / columns;

			for (int i = 0; i < columns; i++)
			{
				output.Append(",");
				output.Append(CONVERT.ToStr(columnWidth));
			}

			return output.ToString();
		}

		public string aggregateResults(string[] roundresults, NetworkProgressionGameFile gameFile, int round, RoundScores scores)
		{
			//Create the xml report
			string reportFile = gameFile.GetRoundFile(gameFile.LastRoundPlayed, "OpsScoreCardReport_Round" + round + ".xml", gameFile.LastPhasePlayed);

			int i = 0;
			int rounds = roundresults.Length;

			LibCore.BasicXmlDocument[] xmldata = new LibCore.BasicXmlDocument[rounds];

			foreach (string s in roundresults)
			{
				if (!string.IsNullOrEmpty(s))
				{
					xmldata[i++] = LibCore.BasicXmlDocument.Create(s);
				}
			}

			int NumColumns = NUMROUNDS + 2;
			string RowHeight = "20";

			string colwidths = GetColumnWidthsString(0.4f, NumColumns - 1);

			LibCore.BasicXmlDocument xdoc = LibCore.BasicXmlDocument.Create();
			XmlNode root = (XmlNode) xdoc.CreateElement("table");
			((XmlElement) root).SetAttribute("columns", "1");
			((XmlElement) root).SetAttribute("rowheight", RowHeight);
			((XmlElement) root).SetAttribute("border_colour", table_border_color == Color.Transparent ? "" : CONVERT.ToComponentStr(table_border_color));
			string rowColour = SkinningDefs.TheInstance.GetData("table_row_colour", "255,255,255");
			((XmlElement) root).SetAttribute("row_colour_1", rowColour);
			((XmlElement) root).SetAttribute("row_colour_2", SkinningDefs.TheInstance.GetData("table_row_colour_alternate", rowColour));

			xdoc.AppendChild(root);

			//add the title table
			XmlNode titles = (XmlNode) xdoc.CreateElement("table");
			((XmlElement) titles).SetAttribute("columns", CONVERT.ToStr(NumColumns));
			((XmlElement) titles).SetAttribute("widths", colwidths);
		    ((XmlElement) titles).SetAttribute("border_colour", table_border_color == Color.Transparent ? "" : CONVERT.ToComponentStr(table_border_color));
			rowColour = SkinningDefs.TheInstance.GetData("table_row_colour", "255,255,255");
			((XmlElement) titles).SetAttribute("row_colour_1", rowColour);
			((XmlElement) titles).SetAttribute("row_colour_2", SkinningDefs.TheInstance.GetData("table_row_colour_alternate", rowColour));
			root.AppendChild(titles);

			XmlNode titlerow = (XmlNode) xdoc.CreateElement("rowdata");
			titles.AppendChild(titlerow);

			XmlNode cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement) cell).SetAttribute("val", "");
			titlerow.AppendChild(cell);
			for (int j = 1; j <= NUMROUNDS; j++)
			{
				cell = (XmlNode) xdoc.CreateElement("cell");
				((XmlElement) cell).SetAttribute("val", CONVERT.ToStr(j));
				titlerow.AppendChild(cell);
			}
			cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement) cell).SetAttribute("val", "Total");
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
			((XmlElement) cell).SetAttribute("val", name);
			((XmlElement) cell).SetAttribute("align", SkinningDefs.TheInstance.GetBoolData("table_centre_headers", true) ? "middle" : "left");
			((XmlElement) cell).SetAttribute("colour", header_colour);
			((XmlElement) cell).SetAttribute("textcolour", CONVERT.ToComponentStr(SkinningDefs.TheInstance.GetColorDataGivenDefault("table_header_text_colour", text_colour)));
			if (SkinningDefs.TheInstance.GetIntData("table_header_bold", 1) != 0)
			{
				((XmlElement) cell).SetAttribute("textstyle", "bold");
			}
			row.AppendChild(cell);

            //add business performance table
            XmlNode bustable = (XmlNode) xdoc.CreateElement("table");
			((XmlElement) bustable).SetAttribute("columns", CONVERT.ToStr(NumColumns));
			((XmlElement) bustable).SetAttribute("widths", colwidths);
			((XmlElement) bustable).SetAttribute("rowheight", RowHeight);
		    ((XmlElement) bustable).SetAttribute("border_colour", table_border_color == Color.Transparent ? "" : CONVERT.ToComponentStr(table_border_color));
			rowColour = SkinningDefs.TheInstance.GetData("table_row_colour", "255,255,255");
			((XmlElement) bustable).SetAttribute("row_colour_1", rowColour);
			((XmlElement) bustable).SetAttribute("row_colour_2", SkinningDefs.TheInstance.GetData("table_row_colour_alternate", rowColour));
			root.AppendChild(bustable);


			string handlingDefinition = " " + SkinningDefs.TheInstance.GetData("handlingDefinition", "Handled");

			row = (XmlNode) xdoc.CreateElement("rowdata");
			bustable.AppendChild(row);
			cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement) cell).SetAttribute("val", transactionName + handlingDefinition);
		    ((XmlElement) cell).SetAttribute("align", SkinningDefs.TheInstance.GetBoolData("table_centre_first_column", true) ? "middle" : "left");
			row.AppendChild(cell);
			setRow(xdoc, row, xmldata, "transactions", round);

			row = (XmlNode) xdoc.CreateElement("rowdata");
			bustable.AppendChild(row);
			cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement) cell).SetAttribute("val", SkinningDefs.TheInstance.GetData("maxname", "Max") + " " + transactionName);
		    ((XmlElement) cell).SetAttribute("align", SkinningDefs.TheInstance.GetBoolData("table_centre_first_column", true) ? "middle" : "left");
			row.AppendChild(cell);
			setRow(xdoc, row, xmldata, "maxtrans", round);

			row = (XmlNode) xdoc.CreateElement("rowdata");
			bustable.AppendChild(row);
			cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement) cell).SetAttribute("val", SkinningDefs.TheInstance.GetData("maxname", "Max") + " Revenue" + " ($M)");
		    ((XmlElement) cell).SetAttribute("align", SkinningDefs.TheInstance.GetBoolData("table_centre_first_column", true) ? "middle" : "left");
			row.AppendChild(cell);
			setRow(xdoc, row, xmldata, "maxrev", round);

			row = (XmlNode) xdoc.CreateElement("rowdata");
			bustable.AppendChild(row);
			cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement) cell).SetAttribute("val", "Actual Revenue ($M)");
		    ((XmlElement) cell).SetAttribute("align", SkinningDefs.TheInstance.GetBoolData("table_centre_first_column", true) ? "middle" : "left");
			row.AppendChild(cell);
			setRow(xdoc, row, xmldata, "revenue", round);


			row = (XmlNode) xdoc.CreateElement("rowdata");
			bustable.AppendChild(row);
			cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement) cell).SetAttribute("val", "Fixed Costs ($M)");
		    ((XmlElement) cell).SetAttribute("align", SkinningDefs.TheInstance.GetBoolData("table_centre_first_column", true) ? "middle" : "left");
			row.AppendChild(cell);
			setRow(xdoc, row, xmldata, "fixedcosts", round);

			row = (XmlNode) xdoc.CreateElement("rowdata");
			bustable.AppendChild(row);
			cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement) cell).SetAttribute("val", "Support Budget ($M)");
		    ((XmlElement) cell).SetAttribute("align", SkinningDefs.TheInstance.GetBoolData("table_centre_first_column", true) ? "middle" : "left");
			row.AppendChild(cell);
			setRow(xdoc, row, xmldata, "supportbudget", round);

			row = (XmlNode) xdoc.CreateElement("rowdata");
			bustable.AppendChild(row);
			cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement) cell).SetAttribute("val", "Service Cost (new/upgrade) ($M)");
		    ((XmlElement) cell).SetAttribute("align", SkinningDefs.TheInstance.GetBoolData("table_centre_first_column", true) ? "middle" : "left");
			row.AppendChild(cell);
			setRow(xdoc, row, xmldata, "projectcosts", round);

			row = (XmlNode) xdoc.CreateElement("rowdata");
			bustable.AppendChild(row);
			cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement) cell).SetAttribute("val", "Regulation Fines ($M)");
		    ((XmlElement) cell).SetAttribute("align", SkinningDefs.TheInstance.GetBoolData("table_centre_first_column", true) ? "middle" : "left");
			row.AppendChild(cell);
			setRow(xdoc, row, xmldata, "fines", round);

			row = (XmlNode) xdoc.CreateElement("rowdata");
			bustable.AppendChild(row);
			cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement) cell).SetAttribute("val", "Compliance Fines ($M)");
		    ((XmlElement) cell).SetAttribute("align", SkinningDefs.TheInstance.GetBoolData("table_centre_first_column", true) ? "middle" : "left");
			row.AppendChild(cell);
			setRow(xdoc, row, xmldata, "compliance", round);

			row = (XmlNode) xdoc.CreateElement("rowdata");
			bustable.AppendChild(row);
			cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement) cell).SetAttribute("val", "Profit / Loss ($M)");
		    ((XmlElement) cell).SetAttribute("align", SkinningDefs.TheInstance.GetBoolData("table_centre_first_column", true) ? "middle" : "left");
			row.AppendChild(cell);
			setRow(xdoc, row, xmldata, "profitloss", round);

			row = (XmlNode) xdoc.CreateElement("rowdata");
			bustable.AppendChild(row);
			cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement) cell).SetAttribute("val", SkinningDefs.TheInstance.GetData("gainlossname", "Improvement On Previous Round") + " ($M)");
		    ((XmlElement) cell).SetAttribute("align", SkinningDefs.TheInstance.GetBoolData("table_centre_first_column", true) ? "middle" : "left");
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
			((XmlElement) cell).SetAttribute("val", name);
		    ((XmlElement) cell).SetAttribute("align", SkinningDefs.TheInstance.GetBoolData("table_centre_headers", true) ? "middle" : "left");
			((XmlElement) cell).SetAttribute("colour", header_colour);
		    ((XmlElement) cell).SetAttribute("textcolour", CONVERT.ToComponentStr(SkinningDefs.TheInstance.GetColorDataGivenDefault("table_header_text_colour", text_colour)));
			if (SkinningDefs.TheInstance.GetIntData("table_header_bold", 1) != 0)
			{
				((XmlElement) cell).SetAttribute("textstyle", "bold");
			}
			row.AppendChild(cell);

			//add IT performance table
			XmlNode supporttable = (XmlNode) xdoc.CreateElement("table");
			((XmlElement) supporttable).SetAttribute("columns", CONVERT.ToStr(NumColumns));
			((XmlElement) supporttable).SetAttribute("widths", colwidths);
			((XmlElement) supporttable).SetAttribute("rowheight", RowHeight);
		    ((XmlElement) supporttable).SetAttribute("border_colour", table_border_color == Color.Transparent ? "" : CONVERT.ToComponentStr(table_border_color));
			rowColour = SkinningDefs.TheInstance.GetData("table_row_colour", "255,255,255");
			((XmlElement) supporttable).SetAttribute("row_colour_1", rowColour);
			((XmlElement) supporttable).SetAttribute("row_colour_2", SkinningDefs.TheInstance.GetData("table_row_colour_alternate", rowColour));
			root.AppendChild(supporttable);

			row = (XmlNode) xdoc.CreateElement("rowdata");
			supporttable.AppendChild(row);
			cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement) cell).SetAttribute("val", "Support Budget ($M)");
		    ((XmlElement) cell).SetAttribute("align", SkinningDefs.TheInstance.GetBoolData("table_centre_first_column", true) ? "middle" : "left");
			row.AppendChild(cell);
			setRow(xdoc, row, xmldata, "supportbudget", round);

			row = (XmlNode) xdoc.CreateElement("rowdata");
			supporttable.AppendChild(row);
			cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement) cell).SetAttribute("val", "Support Spend ($M)");
		    ((XmlElement) cell).SetAttribute("align", SkinningDefs.TheInstance.GetBoolData("table_centre_first_column", true) ? "middle" : "left");
			row.AppendChild(cell);
			setRow(xdoc, row, xmldata, "supportspend", round);

			row = (XmlNode) xdoc.CreateElement("rowdata");
			supporttable.AppendChild(row);
			cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement) cell).SetAttribute("val", "Support Fines ($M)");
		    ((XmlElement) cell).SetAttribute("align", SkinningDefs.TheInstance.GetBoolData("table_centre_first_column", true) ? "middle" : "left");
			row.AppendChild(cell);
			setRow(xdoc, row, xmldata, "supportfines", round);

			row = (XmlNode) xdoc.CreateElement("rowdata");
			supporttable.AppendChild(row);
			cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement) cell).SetAttribute("val", "Profit / Loss ($M)");
		    ((XmlElement) cell).SetAttribute("align", SkinningDefs.TheInstance.GetBoolData("table_centre_first_column", true) ? "middle" : "left");
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
			((XmlElement) cell).SetAttribute("val", name);
		    ((XmlElement) cell).SetAttribute("align", SkinningDefs.TheInstance.GetBoolData("table_centre_headers", true) ? "middle" : "left");
			((XmlElement) cell).SetAttribute("colour", header_colour);
		    ((XmlElement) cell).SetAttribute("textcolour", CONVERT.ToComponentStr(SkinningDefs.TheInstance.GetColorDataGivenDefault("table_header_text_colour", text_colour)));
			if (SkinningDefs.TheInstance.GetIntData("table_header_bold", 1) != 0)
			{
				((XmlElement) cell).SetAttribute("textstyle", "bold");
			}
			row.AppendChild(cell);

			//add IT performance table
			XmlNode ittable = (XmlNode) xdoc.CreateElement("table");
			((XmlElement) ittable).SetAttribute("columns", CONVERT.ToStr(NumColumns));
			((XmlElement) ittable).SetAttribute("widths", colwidths);
			((XmlElement) ittable).SetAttribute("rowheight", RowHeight);
		    ((XmlElement) ittable).SetAttribute("border_colour", table_border_color == Color.Transparent ? "" : CONVERT.ToComponentStr(table_border_color));
			rowColour = SkinningDefs.TheInstance.GetData("table_row_colour", "255,255,255");
			((XmlElement) ittable).SetAttribute("row_colour_1", rowColour);
			((XmlElement) ittable).SetAttribute("row_colour_2", SkinningDefs.TheInstance.GetData("table_row_colour_alternate", rowColour));
			root.AppendChild(ittable);

			row = (XmlNode) xdoc.CreateElement("rowdata");
			ittable.AppendChild(row);
			cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement) cell).SetAttribute("val", "Services Implemented (new/upgrade)");
		    ((XmlElement) cell).SetAttribute("align", SkinningDefs.TheInstance.GetBoolData("table_centre_first_column", true) ? "middle" : "left");
			row.AppendChild(cell);
			setRow(xdoc, row, xmldata, "numservices", round);

			if (SkinningDefs.TheInstance.GetBoolData("uses_transition_rounds", true))
			{
				row = (XmlNode) xdoc.CreateElement("rowdata");
				ittable.AppendChild(row);
				cell = (XmlNode) xdoc.CreateElement("cell");
				((XmlElement) cell).SetAttribute("val", "New Services Implemented Before Round");
			    ((XmlElement) cell).SetAttribute("align", SkinningDefs.TheInstance.GetBoolData("table_centre_first_column", true) ? "middle" : "left");
				row.AppendChild(cell);
				setRow(xdoc, row, xmldata, "numservicesbeforerace", round);
			}

			row = (XmlNode) xdoc.CreateElement("rowdata");
			ittable.AppendChild(row);
			cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement) cell).SetAttribute("val", "Availability");
		    ((XmlElement) cell).SetAttribute("align", SkinningDefs.TheInstance.GetBoolData("table_centre_first_column", true) ? "middle" : "left");
			row.AppendChild(cell);
			setRow(xdoc, row, xmldata, "availability", round);

			if (use_transaction_targets)
			{
				if (use_transaction_targets_data_exists)
				{
					row = (XmlNode) xdoc.CreateElement("rowdata");
					ittable.AppendChild(row);
					cell = (XmlNode) xdoc.CreateElement("cell");
					((XmlElement) cell).SetAttribute("val", transactionName + " Target");
				    ((XmlElement) cell).SetAttribute("align", SkinningDefs.TheInstance.GetBoolData("table_centre_first_column", true) ? "middle" : "left");
                    row.AppendChild(cell);
					setRow(xdoc, row, xmldata, "transtarget", round);
				}
			}

			row = (XmlNode) xdoc.CreateElement("rowdata");
			ittable.AppendChild(row);
			cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement) cell).SetAttribute("val", SkinningDefs.TheInstance.GetData("mtrsname", "Mean Time to Restore Service"));
		    ((XmlElement) cell).SetAttribute("align", SkinningDefs.TheInstance.GetBoolData("table_centre_first_column", true) ? "middle" : "left");
			row.AppendChild(cell);
			setRow(xdoc, row, xmldata, "mttr", round);

			row = (XmlNode) xdoc.CreateElement("rowdata");
			ittable.AppendChild(row);
			cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement) cell).SetAttribute("val", "Total Failures");
		    ((XmlElement) cell).SetAttribute("align", SkinningDefs.TheInstance.GetBoolData("table_centre_first_column", true) ? "middle" : "left");
			row.AppendChild(cell);
			setRow(xdoc, row, xmldata, "totalincidents", round);

			row = (XmlNode) xdoc.CreateElement("rowdata");
			ittable.AppendChild(row);
			cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement) cell).SetAttribute("val", "Prevented Failures");
		    ((XmlElement) cell).SetAttribute("align", SkinningDefs.TheInstance.GetBoolData("table_centre_first_column", true) ? "middle" : "left");
			row.AppendChild(cell);
			setRow(xdoc, row, xmldata, "preventedincidents", round);

			if (SkinningDefs.TheInstance.GetBoolData("allow_first_line_fix", true))
			{
				row = (XmlNode) xdoc.CreateElement("rowdata");
				ittable.AppendChild(row);
				cell = (XmlNode) xdoc.CreateElement("cell");
				((XmlElement) cell).SetAttribute("val", "First Line Fixes");
			    ((XmlElement) cell).SetAttribute("align", SkinningDefs.TheInstance.GetBoolData("table_centre_first_column", true) ? "middle" : "left");
				row.AppendChild(cell);
				setRow(xdoc, row, xmldata, "firstLineFixes", round);
			}

			row = (XmlNode) xdoc.CreateElement("rowdata");
			ittable.AppendChild(row);
			cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement) cell).SetAttribute("val", "Recurring Failures");
		    ((XmlElement) cell).SetAttribute("align", SkinningDefs.TheInstance.GetBoolData("table_centre_first_column", true) ? "middle" : "left");
			row.AppendChild(cell);
			setRow(xdoc, row, xmldata, "recurringincidents", round);

			row = (XmlNode) xdoc.CreateElement("rowdata");
			ittable.AppendChild(row);
			cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement) cell).SetAttribute("val", "Workarounds");
		    ((XmlElement) cell).SetAttribute("align", SkinningDefs.TheInstance.GetBoolData("table_centre_first_column", true) ? "middle" : "left");
			row.AppendChild(cell);
			setRow(xdoc, row, xmldata, "workarounds", round);

			row = (XmlNode) xdoc.CreateElement("rowdata");
			ittable.AppendChild(row);
			cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement) cell).SetAttribute("val", "SLA Breaches");
		    ((XmlElement) cell).SetAttribute("align", SkinningDefs.TheInstance.GetBoolData("table_centre_first_column", true) ? "middle" : "left");
			row.AppendChild(cell);
			setRow(xdoc, row, xmldata, "slabreaches", round);

			xdoc.SaveToURL("", reportFile);

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
			bool makeRed = false;

			int rightSpaces = 4;

			for(int x = 0; x < NUMROUNDS; x++)
			{
				cell = (XmlNode) xdoc.CreateElement("cell");
				if (x < round)
				{
					if (xmldata[x] == null) continue;

					totalformat = "";
					foreach (XmlNode node in xmldata[x].ChildNodes)
					{
						foreach (XmlNode child in node)
						{
							if (child.Attributes["name"].Value == costType)
							{
								cell = (XmlNode) xdoc.CreateElement("cell");

								int spaces = rightSpaces;
								string val = child.Attributes["val"].Value;

								//add to the total?
								if (child.Attributes["sum"] != null)
								{
									if (val.Contains(":"))
									{
										total += CONVERT.ParseHmsToSeconds(val);
									}
									else
									{
										total += CONVERT.ParseDoubleSafe(val, 0);
									}
									showtotal = true;
								}
								if (child.Attributes["average"] != null)
								{
									if (val.Contains(":"))
									{
										average_total += CONVERT.ParseHmsToSeconds(val);
									}
									else
									{
										average_total += CONVERT.ParseDoubleSafe(val, 0);
									}

									average_num++;
								}

								//work out what type of number this is
								string nf = string.Empty;
								if (child.Attributes["numberformat"] != null)
								{
									nf = child.Attributes["numberformat"].Value;
									totalformat = nf;
									if (nf == "currency")
									{
										double tmp = CONVERT.ParseDouble(val);
										if (tmp < 0)
										{
											tmp = tmp * (-1);
											val = "(" + CONVERT.ToPaddedStr(tmp, 2) + ")";
											((XmlElement) cell).SetAttribute("textcolour", "255,0,0");
											spaces--;
										}
									}

									if (nf == "percentage")
									{
										val = val + "%";
										spaces -= 2;
									}
								}

								((XmlElement) cell).SetAttribute("val", val + new string(' ', spaces));
								((XmlElement) cell).SetAttribute("align", "right");
								row.AppendChild(cell);
							}
						}
					}
				}
				else
				{
					((XmlElement) cell).SetAttribute("val", " ");
				}

				row.AppendChild(cell);
			}

			if (showtotal)
			{
				cell = (XmlNode) xdoc.CreateElement("cell");
				int spaces = rightSpaces;

				string totalstr = "";
				if (totalformat == "currency")
				{
					if (total < 0)
					{
						total = total * (-1);
						totalstr = "(" + CONVERT.ToPaddedStr(total, 2) + ")";
						spaces--;
						makeRed = true;
					}
					else
					{
						totalstr = CONVERT.ToPaddedStr(total, 2);
					}
				}
				else if (totalformat == "percentage")
				{
					totalstr = CONVERT.ToPaddedStr(total, 2) + "%";
					spaces -= 2;
				}
				else if (totalformat == "time")
				{
					totalstr = CONVERT.ToHmFromSeconds((int) total);
				}
				else
				{
					totalstr = CONVERT.ToStr(total);
				}

				((XmlElement)cell).SetAttribute( "val", totalstr + new string (' ', spaces));

				if (makeRed)
				{
					((XmlElement) cell).SetAttribute("textcolour", "255,0,0");
				}

				((XmlElement) cell).SetAttribute("align", "right");

				row.AppendChild(cell);
			}

			if (average_num > 0)
			{
				int spaces = rightSpaces;
				string avgstr = "";
				double tmp = average_total / average_num;
				if (totalformat == "percentage")
				{
					avgstr = CONVERT.ToStrRounded(tmp, 0) + "%";
					spaces -= 2;
				}
				else if (totalformat == "currency")
				{
					if (tmp < 0)
					{
						avgstr = "(" + CONVERT.ToPaddedStr(tmp, 1) + ")";
						spaces--;
						makeRed = true;
					}
					else
					{
						avgstr = CONVERT.ToPaddedStr(tmp, 1);
					}
				}
				else if (totalformat == "time")
				{
					avgstr = CONVERT.ToHmFromSeconds((int) tmp);
				}
				else
				{
					avgstr = CONVERT.ToStr(tmp);
				}

				cell = (XmlNode) xdoc.CreateElement("cell");
				((XmlElement) cell).SetAttribute("val", avgstr + new string(' ', spaces));
				((XmlElement) cell).SetAttribute("align", "right");

				row.AppendChild(cell);

				if (makeRed)
				{
					((XmlElement) cell).SetAttribute("textcolour", "255,0,0");
				}
			}
		}
	}
}