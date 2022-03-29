using System.Xml;
using System.Collections;

using GameManagement;
using LibCore;
using CoreUtils;


namespace ReportBuilder
{
	/// <summary>
	/// Summary description for OpsSupportCostsReport.
	/// </summary>
	public class OpsSupportCostsReport
	{
		protected static int NUMROUNDS = SkinningDefs.TheInstance.GetIntData("roundcount", 5);
		protected ArrayList Scores;
		protected SupportSpendOverrides supportOverrides;

        protected string header_colour = SkinningDefs.TheInstance.GetData("table_header_colour", SkinningDefs.TheInstance.GetData("table_header_color", "176,196,222"));
        protected string text_colour = SkinningDefs.TheInstance.GetData("table_text_colour", "0,0,0");
        string header_text_colour = SkinningDefs.TheInstance.GetData("table_header_text_colour", "0,0,0");
        protected string table_border_color = SkinningDefs.TheInstance.GetData("table_border_color", "211,211,211");
        protected string table_row_color = SkinningDefs.TheInstance.GetData("table_row_colour", "255,255,255");
        protected string table_row_color_alt = SkinningDefs.TheInstance.GetData("table_row_colour_alternate", "255,255,255");
        protected string table_no_border = SkinningDefs.TheInstance.GetData("table_no_border", "false");

		public OpsSupportCostsReport(ArrayList _Scores, SupportSpendOverrides _supportOverrides)
		{
			Scores = _Scores;
			supportOverrides = _supportOverrides;

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

		public virtual string BuildReport(NetworkProgressionGameFile gameFile, int round, RoundScores scores)
		{
			//Create the interim xml doc
			LibCore.BasicXmlDocument xdoc = LibCore.BasicXmlDocument.Create();
			XmlNode root = (XmlNode) xdoc.CreateElement("supportcosts");
			xdoc.AppendChild(root);

			XmlNode cost = (XmlNode) xdoc.CreateElement("cost");
			((XmlElement)cost).SetAttribute( "name","mirror_monaco" );
			((XmlElement)cost).SetAttribute( "val", CONVERT.ToStr(scores.Mirror2));
			root.AppendChild(cost);

			cost = (XmlNode) xdoc.CreateElement("cost");
			((XmlElement)cost).SetAttribute( "name","mirror_suzuka" );
			((XmlElement)cost).SetAttribute( "val", CONVERT.ToStr(scores.Mirror1));
			root.AppendChild(cost);

			cost = (XmlNode) xdoc.CreateElement("cost");
			((XmlElement)cost).SetAttribute( "name","eventmonitoring" );
			int tmp = 0;
			if (scores.AdvancedWarningEnabled == true) tmp = 1;
			((XmlElement)cost).SetAttribute( "val", CONVERT.ToStr(tmp));
			root.AppendChild(cost);

			cost = (XmlNode) xdoc.CreateElement("cost");
			((XmlElement)cost).SetAttribute( "name","workaround" );
			((XmlElement)cost).SetAttribute( "val", CONVERT.ToStr(scores.NumWorkarounds));
			root.AppendChild(cost);

			cost = (XmlNode) xdoc.CreateElement("cost");
			((XmlElement)cost).SetAttribute( "name","fixedcosts" );
			((XmlElement)cost).SetAttribute( "val", CONVERT.ToStr(scores.NumFixedCosts));
			root.AppendChild(cost);

			cost = (XmlNode) xdoc.CreateElement("cost");
			((XmlElement) cost).SetAttribute("name", "fixedcosts_saas");
			((XmlElement) cost).SetAttribute("val", CONVERT.ToStr(scores.NumFixedCostsSaaS));
			root.AppendChild(cost);

			cost = (XmlNode) xdoc.CreateElement("cost");
			((XmlElement)cost).SetAttribute( "name","appupgrade" );
			((XmlElement)cost).SetAttribute( "val", CONVERT.ToStr(scores.NumAppUpgrades));
			root.AppendChild(cost);

			cost = (XmlNode) xdoc.CreateElement("cost");
			((XmlElement)cost).SetAttribute( "name","serverupgrade" );
			((XmlElement)cost).SetAttribute( "val", CONVERT.ToStr(scores.NumServerUpgrades));
			root.AppendChild(cost);

			cost = (XmlNode) xdoc.CreateElement("cost");
			((XmlElement)cost).SetAttribute( "name","appconsultancy" );
			((XmlElement)cost).SetAttribute( "val", CONVERT.ToStr(scores.NumAppConsultancyFixes));
			root.AppendChild(cost);

			cost = (XmlNode) xdoc.CreateElement("cost");
			((XmlElement)cost).SetAttribute( "name","serverconsultancy" );
			((XmlElement)cost).SetAttribute( "val", CONVERT.ToStr(scores.NumServerConsultancyFixes));
			root.AppendChild(cost);

			cost = (XmlNode) xdoc.CreateElement("cost");
			((XmlElement)cost).SetAttribute( "name","servermem" );
			((XmlElement)cost).SetAttribute( "val", CONVERT.ToStr(scores.NumMemUpgrades));
			root.AppendChild(cost);

			cost = (XmlNode) xdoc.CreateElement("cost");
			((XmlElement)cost).SetAttribute( "name","serverstorage" );
			((XmlElement)cost).SetAttribute( "val", CONVERT.ToStr(scores.NumStorageUpgrades));
			root.AppendChild(cost);

			cost = (XmlNode) xdoc.CreateElement("cost");
			((XmlElement)cost).SetAttribute( "name","newservices" );
			((XmlElement)cost).SetAttribute( "val", CONVERT.ToStr(scores.NumNewServices));
			root.AppendChild(cost);


			return xdoc.InnerXml;
		}

		protected string left_column_header = "";

		public void SetLeftColumnHeader(string title)
		{
			left_column_header = title;
		}

		public virtual string CombineRoundResults(string[] roundresults, NetworkProgressionGameFile gameFile, int round)
		{
			int rightPaddingSpaces = 4;
			string rightPadding = new string (' ', rightPaddingSpaces);

			int NumColumns = NUMROUNDS + 3;
			//Create the xml report
			string reportFile = gameFile.GetRoundFile(gameFile.LastRoundPlayed, "OpsSupportCostsReport_Round" + round + ".xml" , gameFile.LastPhasePlayed);

			ReportUtils rep = new ReportUtils(gameFile);

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
			
			string colwidths = "0.25,0.15";
			double remaining = 1 - (0.25 + 0.15);
			for (int j = 1; j <= (1 + NUMROUNDS); j++)
			{
				colwidths += "," + CONVERT.ToStr(remaining / (1 + NUMROUNDS));
			}

			LibCore.BasicXmlDocument xdoc = LibCore.BasicXmlDocument.Create();
			XmlNode root = (XmlNode) xdoc.CreateElement("table");
			((XmlElement)root).SetAttribute( "columns","1" );
			((XmlElement)root).SetAttribute( "rowheight","30" );
			((XmlElement)root).SetAttribute( "border_colour", table_border_color );
			((XmlElement)root).SetAttribute( "row_colour_1", table_row_color);
			((XmlElement)root).SetAttribute( "row_colour_2", table_row_color_alt);
			xdoc.AppendChild(root);

			//add the title table
			XmlNode titles = (XmlNode) xdoc.CreateElement("table");
			((XmlElement)titles).SetAttribute( "columns", CONVERT.ToStr(NumColumns) );
			((XmlElement)titles).SetAttribute( "widths", colwidths);
			((XmlElement)titles).SetAttribute( "rowheight","30" );
			((XmlElement)titles).SetAttribute( "border_colour", table_border_color );
			((XmlElement)titles).SetAttribute( "row_colour_1", table_row_color);
			((XmlElement)titles).SetAttribute( "row_colour_2", table_row_color_alt);
			root.AppendChild(titles);

			XmlNode titlerow = (XmlNode) xdoc.CreateElement("rowdata");
			titles.AppendChild(titlerow);

			XmlNode cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement)cell).SetAttribute( "val",left_column_header );
            ((XmlElement)cell).SetAttribute("textcolour", text_colour);
            ((XmlElement)cell).SetAttribute("no_border", table_no_border);
			titlerow.AppendChild(cell);
			cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement)cell).SetAttribute( "val","Ref" );
            ((XmlElement)cell).SetAttribute("textcolour", text_colour);
            ((XmlElement)cell).SetAttribute("no_border", table_no_border);
			titlerow.AppendChild(cell);
			cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement)cell).SetAttribute( "val","Cost ($K)" );
            ((XmlElement)cell).SetAttribute("textcolour", text_colour);
            ((XmlElement)cell).SetAttribute("no_border", table_no_border);
			titlerow.AppendChild(cell);

			for (int j = 1; j <= NUMROUNDS; j++)
			{
				cell = (XmlNode) xdoc.CreateElement("cell");
				((XmlElement)cell).SetAttribute( "val",CONVERT.ToStr(j));
                ((XmlElement)cell).SetAttribute("textcolour", text_colour);
                ((XmlElement)cell).SetAttribute("no_border", table_no_border);
				titlerow.AppendChild(cell);
			}

		    var headerAlignment = "middle";
		    var skinHeaderAlignment = SkinningDefs.TheInstance.GetData("table_header_alignment");
		    if (! string.IsNullOrEmpty(skinHeaderAlignment))
		    {
		        headerAlignment = skinHeaderAlignment;
		    }

            //add a title row 
            XmlNode row = (XmlNode)xdoc.CreateElement("rowdata");
		    string name;
            double tmp = 0;

            if (SkinningDefs.TheInstance.GetData("reports_show_misc_table_only", "false") == "false")
            {
                root.AppendChild(row);
                cell = (XmlNode)xdoc.CreateElement("cell");

		        if (SkinningDefs.TheInstance.GetBoolData("mirrors_are_virtual", false))
		        {
		            name = "Virtualization";
		        }
		        else
		        {
		            name = "Mirror";
		        }

		        if (SkinningDefs.TheInstance.GetIntData("reports_use_numbered_sections", 1) == 1)
		        {
		            name = "1 - " + name;
		        }
		        ((XmlElement) cell).SetAttribute("val", name);
		        ((XmlElement) cell).SetAttribute("align", headerAlignment);
		        ((XmlElement) cell).SetAttribute("colour", header_colour);
		        ((XmlElement) cell).SetAttribute("textcolour", header_text_colour);
		        ((XmlElement) cell).SetAttribute("no_border", table_no_border);
		        if (SkinningDefs.TheInstance.GetIntData("table_header_bold", 1) != 0)
		        {
		            ((XmlElement) cell).SetAttribute("textstyle", "bold");
		        }
		        row.AppendChild(cell);
                
		        if (SkinningDefs.TheInstance.GetBoolData("mirrors_are_virtual", false))
		        {
		            // add vitualisation table
		            XmlNode virtualisationTable = (XmlNode) xdoc.CreateElement("table");
		            ((XmlElement) virtualisationTable).SetAttribute("columns", CONVERT.ToStr(NumColumns));
		            ((XmlElement) virtualisationTable).SetAttribute("widths", colwidths);
		            ((XmlElement) virtualisationTable).SetAttribute("rowheight", "30");
		            ((XmlElement) virtualisationTable).SetAttribute("border_colour", table_border_color);
		            ((XmlElement) virtualisationTable).SetAttribute("row_colour_1", table_row_color);
		            ((XmlElement) virtualisationTable).SetAttribute("row_colour_2", table_row_color_alt);
		            root.AppendChild(virtualisationTable);

		            row = (XmlNode) xdoc.CreateElement("rowdata");
		            virtualisationTable.AppendChild(row);

		            string vitualZone = "Zone 6"; //todo get from skin

		            cell = (XmlNode) xdoc.CreateElement("cell");
		            ((XmlElement) cell).SetAttribute("val", vitualZone);
		            ((XmlElement) cell).SetAttribute("textcolour", text_colour);
		            ((XmlElement) cell).SetAttribute("no_border", table_no_border);
		            row.AppendChild(cell);
		            cell = (XmlNode) xdoc.CreateElement("cell");
		            ((XmlElement) cell).SetAttribute("textcolour", text_colour);
		            ((XmlElement) cell).SetAttribute("no_border", table_no_border);
		            row.AppendChild(cell);
		            cell = (XmlNode) xdoc.CreateElement("cell");
                    tmp = rep.GetCost("mirror_" + ((RoundScores)Scores[0]).MirrorableServerNames[1], round) /
		                  1000.0;
		            ((XmlElement) cell).SetAttribute("val", CONVERT.ToPaddedStr(tmp, 1) + rightPadding);
		            ((XmlElement) cell).SetAttribute("align", "right");
		            ((XmlElement) cell).SetAttribute("textcolour", text_colour);
		            ((XmlElement) cell).SetAttribute("no_border", table_no_border);
		            row.AppendChild(cell);

		            SetRowData(xdoc, row, xmldata, "mirror_monaco", round, vitualZone);
		        }
		        else
		        {
		            //add Mirror table
		            XmlNode mirrortable = (XmlNode) xdoc.CreateElement("table");
		            ((XmlElement) mirrortable).SetAttribute("columns", CONVERT.ToStr(NumColumns));
		            ((XmlElement) mirrortable).SetAttribute("widths", colwidths);
		            ((XmlElement) mirrortable).SetAttribute("rowheight", "30");
		            ((XmlElement) mirrortable).SetAttribute("border_colour", table_border_color);
		            ((XmlElement) mirrortable).SetAttribute("row_colour_1", table_row_color);
		            ((XmlElement) mirrortable).SetAttribute("row_colour_2", table_row_color_alt);
		            root.AppendChild(mirrortable);

		            row = (XmlNode) xdoc.CreateElement("rowdata");
		            mirrortable.AppendChild(row);

			        string mirrorableServerName = "";
			        if (((RoundScores) Scores[0]).MirrorableServerNames.Count > 1)
			        {
						mirrorableServerName = ((RoundScores)Scores[0]).MirrorableServerNames[1];
			        }
			        string mirror2 = "Server " + mirrorableServerName;

		            cell = (XmlNode) xdoc.CreateElement("cell");
		            ((XmlElement) cell).SetAttribute("val", mirror2);
		            ((XmlElement) cell).SetAttribute("textcolour", text_colour);
		            ((XmlElement) cell).SetAttribute("no_border", table_no_border);
		            ((XmlElement) cell).SetAttribute("align", SkinningDefs.TheInstance.GetData("table_header_alignment"));
                    row.AppendChild(cell);
		            cell = (XmlNode) xdoc.CreateElement("cell");
					((XmlElement)cell).SetAttribute("val", mirrorableServerName);
		            ((XmlElement) cell).SetAttribute("textcolour", text_colour);
		            ((XmlElement) cell).SetAttribute("no_border", table_no_border);
		            row.AppendChild(cell);
		            cell = (XmlNode) xdoc.CreateElement("cell");
                    tmp = rep.GetCost("mirror_" + mirrorableServerName, round) / 1000.0;
		            ((XmlElement) cell).SetAttribute("val", "" + CONVERT.ToPaddedStr(tmp, 1) + rightPadding);
		            ((XmlElement) cell).SetAttribute("align", "right");
		            ((XmlElement) cell).SetAttribute("textcolour", text_colour);
		            ((XmlElement) cell).SetAttribute("no_border", table_no_border);
		            row.AppendChild(cell);

		            SetRowData(xdoc, row, xmldata, "mirror_monaco", round, mirror2);

		            row = (XmlNode) xdoc.CreateElement("rowdata");
		            mirrortable.AppendChild(row);

			        mirrorableServerName = "";
			        if (((RoundScores)Scores[0]).MirrorableServerNames.Count > 0)
			        {
				        mirrorableServerName = ((RoundScores)Scores[0]).MirrorableServerNames[0];
			        }
			        string mirror1 = "Server " + mirrorableServerName;

		            cell = (XmlNode) xdoc.CreateElement("cell");
		            ((XmlElement) cell).SetAttribute("val", mirror1);
		            ((XmlElement) cell).SetAttribute("textcolour", text_colour);
		            ((XmlElement) cell).SetAttribute("no_border", table_no_border);
		            ((XmlElement) cell).SetAttribute("align", SkinningDefs.TheInstance.GetData("table_header_alignment"));
		            row.AppendChild(cell);
		            cell = (XmlNode) xdoc.CreateElement("cell");
					((XmlElement)cell).SetAttribute("val", mirrorableServerName);
		            ((XmlElement) cell).SetAttribute("textcolour", text_colour);
		            ((XmlElement) cell).SetAttribute("no_border", table_no_border);
		            row.AppendChild(cell);
		            cell = (XmlNode) xdoc.CreateElement("cell");
					tmp = rep.GetCost("mirror_" + mirrorableServerName, round) / 1000.0;
		            ((XmlElement) cell).SetAttribute("val", "" + CONVERT.ToPaddedStr(tmp, 1) + rightPadding);
		            ((XmlElement) cell).SetAttribute("align", "right");
		            ((XmlElement) cell).SetAttribute("textcolour", text_colour);
		            ((XmlElement) cell).SetAttribute("no_border", table_no_border);
		            row.AppendChild(cell);

		            SetRowData(xdoc, row, xmldata, "mirror_suzuka", round, mirror1);
		        }

		        //add a title row 
		        row = (XmlNode) xdoc.CreateElement("rowdata");
		        root.AppendChild(row);
		        cell = (XmlNode) xdoc.CreateElement("cell");
		        name = "Upgrade";
		        if (SkinningDefs.TheInstance.GetIntData("reports_use_numbered_sections", 1) == 1)
		        {
		            name = "2 - " + name;
		        }
		        ((XmlElement) cell).SetAttribute("val", name);
		        ((XmlElement) cell).SetAttribute("align", headerAlignment);
		        ((XmlElement) cell).SetAttribute("colour", header_colour);
		        ((XmlElement) cell).SetAttribute("textcolour", header_text_colour);
		        ((XmlElement) cell).SetAttribute("no_border", table_no_border);
		        if (SkinningDefs.TheInstance.GetIntData("table_header_bold", 1) != 0)
		        {
		            ((XmlElement) cell).SetAttribute("textstyle", "bold");
		        }
		        row.AppendChild(cell);

		        //add upgrade table
		        XmlNode upgradetable = (XmlNode) xdoc.CreateElement("table");
		        ((XmlElement) upgradetable).SetAttribute("columns", CONVERT.ToStr(NumColumns));
		        ((XmlElement) upgradetable).SetAttribute("widths", colwidths);
		        ((XmlElement) upgradetable).SetAttribute("rowheight", "30");
		        ((XmlElement) upgradetable).SetAttribute("border_colour", table_border_color);
		        ((XmlElement) upgradetable).SetAttribute("row_colour_1", table_row_color);
		        ((XmlElement) upgradetable).SetAttribute("row_colour_2", table_row_color_alt);
		        root.AppendChild(upgradetable);

		        row = (XmlNode) xdoc.CreateElement("rowdata");
		        upgradetable.AppendChild(row);

		        string app = SkinningDefs.TheInstance.GetData("appname", "Application");
		        string application_upgrades = app + " Upgrades";

		        cell = (XmlNode) xdoc.CreateElement("cell");
		        ((XmlElement) cell).SetAttribute("val", application_upgrades);
		        ((XmlElement) cell).SetAttribute("textcolour", text_colour);
		        ((XmlElement) cell).SetAttribute("no_border", table_no_border);
                ((XmlElement) cell).SetAttribute("align", SkinningDefs.TheInstance.GetData("table_header_alignment"));
		        row.AppendChild(cell);
		        cell = (XmlNode) xdoc.CreateElement("cell");
		        ((XmlElement) cell).SetAttribute("val", app);
		        ((XmlElement) cell).SetAttribute("textcolour", text_colour);
		        ((XmlElement) cell).SetAttribute("no_border", table_no_border);
		        row.AppendChild(cell);
		        cell = (XmlNode) xdoc.CreateElement("cell");
		        tmp = rep.GetCost("appupgrade", round) / 1000.0;
		        ((XmlElement) cell).SetAttribute("val", "" + CONVERT.ToPaddedStr(tmp, 1) + rightPadding);
		        ((XmlElement) cell).SetAttribute("align", "right");
		        ((XmlElement) cell).SetAttribute("textcolour", text_colour);
		        ((XmlElement) cell).SetAttribute("no_border", table_no_border);
		        row.AppendChild(cell);

		        SetRowData(xdoc, row, xmldata, "appupgrade", round, application_upgrades);

		        if (SkinningDefs.TheInstance.GetBoolData("saas_upgrades", false))
		        {
		            row = (XmlNode) xdoc.CreateElement("rowdata");
		            upgradetable.AppendChild(row);

		            string saasUpgrades = SkinningDefs.TheInstance.GetData("saasname", "SaaS");

		            cell = (XmlNode) xdoc.CreateElement("cell");
		            ((XmlElement) cell).SetAttribute("val", saasUpgrades);
		            ((XmlElement) cell).SetAttribute("textcolour", text_colour);
		            ((XmlElement) cell).SetAttribute("no_border", table_no_border);
		            ((XmlElement) cell).SetAttribute("align", SkinningDefs.TheInstance.GetData("table_header_alignment"));
		            row.AppendChild(cell);
		            cell = (XmlNode) xdoc.CreateElement("cell");
		            ((XmlElement) cell).SetAttribute("val", app);
		            ((XmlElement) cell).SetAttribute("textcolour", text_colour);
		            ((XmlElement) cell).SetAttribute("no_border", table_no_border);
		            row.AppendChild(cell);
		            cell = (XmlNode) xdoc.CreateElement("cell");
		            tmp = rep.GetCost("saas_fee", round) / 1000.0;
		            ((XmlElement) cell).SetAttribute("val", "" + CONVERT.ToPaddedStr(tmp, 1) + rightPadding);
		            ((XmlElement) cell).SetAttribute("align", "right");
		            ((XmlElement) cell).SetAttribute("textcolour", text_colour);
		            ((XmlElement) cell).SetAttribute("no_border", table_no_border);
		            row.AppendChild(cell);

		            SetRowData(xdoc, row, xmldata, "saas_fee", round, saasUpgrades);
		        }

		        row = (XmlNode) xdoc.CreateElement("rowdata");
		        upgradetable.AppendChild(row);

		        string server = SkinningDefs.TheInstance.GetData("servername", "Server");
		        string server_upgrades = server + " Upgrades";

		        cell = (XmlNode) xdoc.CreateElement("cell");
		        ((XmlElement) cell).SetAttribute("val", server_upgrades);
		        ((XmlElement) cell).SetAttribute("textcolour", text_colour);
		        ((XmlElement) cell).SetAttribute("no_border", table_no_border);
                ((XmlElement) cell).SetAttribute("align", SkinningDefs.TheInstance.GetData("table_header_alignment"));
		        row.AppendChild(cell);
		        cell = (XmlNode) xdoc.CreateElement("cell");
		        ((XmlElement) cell).SetAttribute("val", server);
		        ((XmlElement) cell).SetAttribute("textcolour", text_colour);
		        ((XmlElement) cell).SetAttribute("no_border", table_no_border);
		        row.AppendChild(cell);
		        cell = (XmlNode) xdoc.CreateElement("cell");
		        tmp = rep.GetCost("upgradeserver", round) / 1000.0;
		        ((XmlElement) cell).SetAttribute("val", "" + CONVERT.ToPaddedStr(tmp, 1) + rightPadding);
		        ((XmlElement) cell).SetAttribute("align", "right");
		        ((XmlElement) cell).SetAttribute("textcolour", text_colour);
		        ((XmlElement) cell).SetAttribute("no_border", table_no_border);
		        row.AppendChild(cell);

		        SetRowData(xdoc, row, xmldata, "serverupgrade", round, server_upgrades);

		        row = (XmlNode) xdoc.CreateElement("rowdata");
		        upgradetable.AppendChild(row);

		        string server_memory_upgrades = server + " Memory Upgrades";
		        cell = (XmlNode) xdoc.CreateElement("cell");
		        ((XmlElement) cell).SetAttribute("val", server_memory_upgrades);
		        ((XmlElement) cell).SetAttribute("textcolour", text_colour);
		        ((XmlElement) cell).SetAttribute("no_border", table_no_border);
                ((XmlElement) cell).SetAttribute("align", SkinningDefs.TheInstance.GetData("table_header_alignment"));
		        row.AppendChild(cell);
		        cell = (XmlNode) xdoc.CreateElement("cell");
		        ((XmlElement) cell).SetAttribute("val", "Memory");
		        ((XmlElement) cell).SetAttribute("textcolour", text_colour);
		        ((XmlElement) cell).SetAttribute("no_border", table_no_border);
		        row.AppendChild(cell);
		        cell = (XmlNode) xdoc.CreateElement("cell");
		        tmp = rep.GetCost("upgrade_mem", round) / 1000.0;
		        ((XmlElement) cell).SetAttribute("val", "" + CONVERT.ToPaddedStr(tmp, 1) + rightPadding);
		        ((XmlElement) cell).SetAttribute("align", "right");
		        ((XmlElement) cell).SetAttribute("textcolour", text_colour);
		        ((XmlElement) cell).SetAttribute("no_border", table_no_border);
		        row.AppendChild(cell);

		        SetRowData(xdoc, row, xmldata, "servermem", round, server_memory_upgrades);

		        row = (XmlNode) xdoc.CreateElement("rowdata");
		        upgradetable.AppendChild(row);

		        string server_storage_upgrades = server + " Storage Upgrades";

		        cell = (XmlNode) xdoc.CreateElement("cell");
		        ((XmlElement) cell).SetAttribute("val", server_storage_upgrades);
		        ((XmlElement) cell).SetAttribute("textcolour", text_colour);
		        ((XmlElement) cell).SetAttribute("no_border", table_no_border);
                ((XmlElement) cell).SetAttribute("align", SkinningDefs.TheInstance.GetData("table_header_alignment"));
		        row.AppendChild(cell);
		        cell = (XmlNode) xdoc.CreateElement("cell");
		        ((XmlElement) cell).SetAttribute("val", "Storage");
		        ((XmlElement) cell).SetAttribute("textcolour", text_colour);
		        ((XmlElement) cell).SetAttribute("no_border", table_no_border);
		        row.AppendChild(cell);
		        cell = (XmlNode) xdoc.CreateElement("cell");
		        tmp = rep.GetCost("upgrade_storage", round) / 1000.0;
		        ((XmlElement) cell).SetAttribute("val", "" + CONVERT.ToPaddedStr(tmp, 1) + rightPadding);
		        ((XmlElement) cell).SetAttribute("align", "right");
		        ((XmlElement) cell).SetAttribute("textcolour", text_colour);
		        ((XmlElement) cell).SetAttribute("no_border", table_no_border);
		        row.AppendChild(cell);

		        SetRowData(xdoc, row, xmldata, "serverstorage", round, server_storage_upgrades);
		    }

		    //add a title row 
			row = (XmlNode) xdoc.CreateElement("rowdata");
			root.AppendChild(row);
			cell = (XmlNode) xdoc.CreateElement("cell");
		    if (SkinningDefs.TheInstance.GetData("reports_show_misc_table_only", "false") == "false")
		    {
		        name = "Misc";
                if (SkinningDefs.TheInstance.GetIntData("reports_use_numbered_sections", 1) == 1)
                {
                    name = "3 - " + name;
                }
		    }
		    else
		    {
		        name = "Support Costs";
		    }
		    
			((XmlElement)cell).SetAttribute( "val", name );
			((XmlElement)cell).SetAttribute( "align", headerAlignment);
			((XmlElement)cell).SetAttribute( "colour",header_colour );
            ((XmlElement)cell).SetAttribute("textcolour", header_text_colour);
            ((XmlElement)cell).SetAttribute("no_border", table_no_border);
			if (SkinningDefs.TheInstance.GetIntData("table_header_bold", 1) != 0)
			{
				((XmlElement)cell).SetAttribute( "textstyle", "bold" );
			}
			row.AppendChild(cell);

			//add Misc table
			XmlNode misctable = (XmlNode) xdoc.CreateElement("table");
			((XmlElement)misctable).SetAttribute( "columns", CONVERT.ToStr(NumColumns));
			((XmlElement)misctable).SetAttribute( "widths", colwidths);
			((XmlElement)misctable).SetAttribute( "rowheight","30" );
			((XmlElement)misctable).SetAttribute( "border_colour", table_border_color );
			((XmlElement)misctable).SetAttribute( "row_colour_1", table_row_color);
			((XmlElement)misctable).SetAttribute( "row_colour_2", table_row_color_alt);
			root.AppendChild(misctable);

			row = (XmlNode) xdoc.CreateElement("rowdata");
			misctable.AppendChild(row);

			string consultancy_hardware = "Consultancy - Hardware";
            if (SkinningDefs.TheInstance.GetData("reports_show_misc_table_only", "false") == "true")
		    {
                consultancy_hardware = "Consultancy";
		    }

			cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement)cell).SetAttribute( "val", consultancy_hardware );
            ((XmlElement)cell).SetAttribute("textcolour", text_colour);
            ((XmlElement)cell).SetAttribute("no_border", table_no_border);
		    ((XmlElement) cell).SetAttribute("align", SkinningDefs.TheInstance.GetData("table_header_alignment"));
			row.AppendChild(cell);
			cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement)cell).SetAttribute( "val","Consulting 1" );
            ((XmlElement)cell).SetAttribute("textcolour", text_colour);
            ((XmlElement)cell).SetAttribute("no_border", table_no_border);
			row.AppendChild(cell);
			cell = (XmlNode) xdoc.CreateElement("cell");
			tmp = rep.GetCost("consultancy_hardware",round) / 1000.0;
			((XmlElement) cell).SetAttribute("val", "" + CONVERT.ToPaddedStr(tmp, 1) + rightPadding);
			((XmlElement) cell).SetAttribute("align", "right");
            ((XmlElement)cell).SetAttribute("textcolour", text_colour);
            ((XmlElement)cell).SetAttribute("no_border", table_no_border);
			row.AppendChild(cell);

			SetRowData(xdoc, row, xmldata, "serverconsultancy", round, consultancy_hardware);

			row = (XmlNode) xdoc.CreateElement("rowdata");
			misctable.AppendChild(row);

			string consultancy_software = "Consultancy - Software";
            if (SkinningDefs.TheInstance.GetData("reports_show_misc_table_only", "false") == "true")
            {
                consultancy_software = "Consultancy";
            }

			cell = (XmlNode) xdoc.CreateElement("cell");
            ((XmlElement)cell).SetAttribute("textcolour", text_colour);
            ((XmlElement)cell).SetAttribute("no_border", table_no_border);
			((XmlElement)cell).SetAttribute( "val", consultancy_software );
		    ((XmlElement) cell).SetAttribute("align", SkinningDefs.TheInstance.GetData("table_header_alignment"));
			row.AppendChild(cell);
			cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement)cell).SetAttribute( "val","Consulting 2" );
            ((XmlElement)cell).SetAttribute("textcolour", text_colour);
            ((XmlElement)cell).SetAttribute("no_border", table_no_border);
			row.AppendChild(cell);
			cell = (XmlNode) xdoc.CreateElement("cell");
			tmp =rep.GetCost("consultancy_software",round) / 1000.0;
			((XmlElement) cell).SetAttribute("val", "" + CONVERT.ToPaddedStr(tmp, 1) + rightPadding);
			((XmlElement) cell).SetAttribute("align", "right");
            ((XmlElement)cell).SetAttribute("textcolour", text_colour);
            ((XmlElement)cell).SetAttribute("no_border", table_no_border);
			row.AppendChild(cell);

			SetRowData(xdoc, row, xmldata, "appconsultancy", round, consultancy_software);

			row = (XmlNode) xdoc.CreateElement("rowdata");
			misctable.AppendChild(row);
			string infrastructure_and_manpower = "Infrastructure & Manpower";
			cell = (XmlNode) xdoc.CreateElement("cell");
            ((XmlElement)cell).SetAttribute("textcolour", text_colour);
            ((XmlElement)cell).SetAttribute("no_border", table_no_border);
			((XmlElement)cell).SetAttribute( "val", infrastructure_and_manpower );
		    ((XmlElement) cell).SetAttribute("align", SkinningDefs.TheInstance.GetData("table_header_alignment"));
			row.AppendChild(cell);
			cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement)cell).SetAttribute( "val","Infrastructure" );
            ((XmlElement)cell).SetAttribute("textcolour", text_colour);
            ((XmlElement)cell).SetAttribute("no_border", table_no_border);
			row.AppendChild(cell);
			cell = (XmlNode) xdoc.CreateElement("cell");
			tmp = rep.GetCost("infrastructure",round) / 1000.0;
			((XmlElement) cell).SetAttribute("val", "" + CONVERT.ToPaddedStr(tmp, 1) + rightPadding);
			((XmlElement) cell).SetAttribute("align", "right");
            ((XmlElement)cell).SetAttribute("textcolour", text_colour);
            ((XmlElement)cell).SetAttribute("no_border", table_no_border);
			row.AppendChild(cell);
			SetRowData(xdoc, row, xmldata, "fixedcosts", round, infrastructure_and_manpower);

			row = (XmlNode) xdoc.CreateElement("rowdata");
			misctable.AppendChild(row);

			string new_service_and_support_costs = "New Service Support Costs";

			cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement)cell).SetAttribute( "val", new_service_and_support_costs );
            ((XmlElement)cell).SetAttribute("textcolour", text_colour);
            ((XmlElement)cell).SetAttribute("no_border", table_no_border);
		    ((XmlElement) cell).SetAttribute("align", SkinningDefs.TheInstance.GetData("table_header_alignment"));
			row.AppendChild(cell);
			cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement)cell).SetAttribute( "val","Application" );
            ((XmlElement)cell).SetAttribute("textcolour", text_colour);
            ((XmlElement)cell).SetAttribute("no_border", table_no_border);
			row.AppendChild(cell);
			cell = (XmlNode) xdoc.CreateElement("cell");
			tmp = rep.GetCost("newservice",round) / 1000.0;
			((XmlElement) cell).SetAttribute("val", "" + CONVERT.ToPaddedStr(tmp, 1) + rightPadding);
			((XmlElement) cell).SetAttribute("align", "right");
            ((XmlElement)cell).SetAttribute("textcolour", text_colour);
            ((XmlElement)cell).SetAttribute("no_border", table_no_border);
			row.AppendChild(cell);

			SetRowData(xdoc, row, xmldata, "newservices", round, new_service_and_support_costs);

			row = (XmlNode) xdoc.CreateElement("rowdata");
			misctable.AppendChild(row);

			string workaround = "Workaround";

			cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement)cell).SetAttribute( "val", workaround );
            ((XmlElement)cell).SetAttribute("textcolour", text_colour);
            ((XmlElement)cell).SetAttribute("no_border", table_no_border);
		    ((XmlElement) cell).SetAttribute("align", SkinningDefs.TheInstance.GetData("table_header_alignment"));
			row.AppendChild(cell);
			cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement)cell).SetAttribute( "val","Workaround" );
            ((XmlElement)cell).SetAttribute("textcolour", text_colour);
            ((XmlElement)cell).SetAttribute("no_border", table_no_border);
			row.AppendChild(cell);
			cell = (XmlNode) xdoc.CreateElement("cell");
			tmp = rep.GetCost("workaround",round) / 1000.0;
			((XmlElement) cell).SetAttribute("val", "" + CONVERT.ToPaddedStr(tmp, 1) + rightPadding);
			((XmlElement) cell).SetAttribute("align", "right");
            ((XmlElement)cell).SetAttribute("textcolour", text_colour);
            ((XmlElement)cell).SetAttribute("no_border", table_no_border);
			row.AppendChild(cell);

			SetRowData(xdoc, row, xmldata, "workaround", round, workaround);

			row = (XmlNode) xdoc.CreateElement("rowdata");
			misctable.AppendChild(row);

			string event_monitoring = "Event Monitoring";

			cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement)cell).SetAttribute( "val", event_monitoring );
            ((XmlElement)cell).SetAttribute("textcolour", text_colour);
            ((XmlElement)cell).SetAttribute("no_border", table_no_border);
		    ((XmlElement) cell).SetAttribute("align", SkinningDefs.TheInstance.GetData("table_header_alignment"));
			row.AppendChild(cell);
			cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement)cell).SetAttribute( "val","Monitoring" );
            ((XmlElement)cell).SetAttribute("textcolour", text_colour);
            ((XmlElement)cell).SetAttribute("no_border", table_no_border);
			row.AppendChild(cell);
			cell = (XmlNode) xdoc.CreateElement("cell");
			tmp = rep.GetCost("eventmonitoring",round) / 1000.0;
			((XmlElement) cell).SetAttribute("val", "" + CONVERT.ToPaddedStr(tmp, 1) + rightPadding);
			((XmlElement) cell).SetAttribute("align", "right");
            ((XmlElement)cell).SetAttribute("textcolour", text_colour);
            ((XmlElement)cell).SetAttribute("no_border", table_no_border);
			row.AppendChild(cell);

			SetRowData(xdoc, row, xmldata, "eventmonitoring", round, event_monitoring);

			xdoc.SaveToURL("",reportFile);

			return reportFile;
		}

		protected void SetRowData(BasicXmlDocument xdoc, XmlNode row, BasicXmlDocument[] xmldata, string costType, int round, string row_name)
		{
			XmlNode cell;
			for(int x = 0; x < NUMROUNDS; x++)
			{
			    if (x >= round || xmldata[x] == null)
			    {
                    cell = (XmlNode)xdoc.CreateElement("cell");
                    ((XmlElement)cell).SetAttribute("textcolour", text_colour);
                    ((XmlElement)cell).SetAttribute("no_border", table_no_border);
                    row.AppendChild(cell);
                    continue;
			    }
			    
                foreach(XmlNode node in xmldata[x].ChildNodes)
				{
					foreach(XmlNode child in node)
					{
						if(child.Attributes["name"].Value == costType)
						{
							cell = (XmlNode) xdoc.CreateElement("cell");

							string cellName = SupportSpendOverrides.CreateCellName(x+1, row_name);

							string val = child.Attributes["val"].Value;
							((XmlElement)cell).SetAttribute( "val", val );
							((XmlElement)cell).SetAttribute( "edit", "true" );
							((XmlElement)cell).SetAttribute( "cellname", cellName );
                            ((XmlElement)cell).SetAttribute("textcolour", text_colour);
                            ((XmlElement)cell).SetAttribute("no_border", table_no_border);
							//
							// Mark the cell as having been overriden by changing its colour...
							//
							string override_val;
							if(supportOverrides.GetOverride(cellName, out override_val))
							{
								((XmlElement)cell).SetAttribute( "colour", "246,255,183" );

								if(supportOverrides.GetOriginalValue(cellName, out override_val))
								{
									((XmlElement)cell).SetAttribute( "tooltiptext", "Original Value: " + override_val );
								}
							}
							//
							row.AppendChild(cell);
						}
					}
				}
			}
		}
	}
}
