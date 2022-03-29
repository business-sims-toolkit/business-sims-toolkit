using System.Xml;
using System.Collections;
using System.Drawing;

using GameManagement;
using LibCore;
using CoreUtils;

namespace ReportBuilder
{
	/// <summary>
	/// Summary description for OpsProcessScoresReport.
	/// </summary>
	public class OpsProcessScoresReport
	{
		static int NUMROUNDS = SkinningDefs.TheInstance.GetIntData("roundcount", 5);

	    Color headerColour;

        protected string header_colour
        {
            get
            {
                return CONVERT.ToComponentStr(headerColour);
            }
        }


	    Color textColour;
        
        protected string text_colour
        {
            get
            {
                return CONVERT.ToComponentStr(textColour);
            }
        }

	    Color tableBorderColour;

        protected string table_border_color
        {
            get
            {
                return CONVERT.ToComponentStr(tableBorderColour);
            }
        }

	    Color tableRowColour;

        protected string table_row_color
        {
            get
            {
                return CONVERT.ToComponentStr(tableRowColour);
            }
        }

	    Color tableRowAltColour;
        protected string table_row_color_alt
        {
            get
            {
                return CONVERT.ToComponentStr(tableRowAltColour);
            }
        }

	    bool tableNoBorder;

        protected string table_no_border
        {
            get
            {
                return CONVERT.ToStr(tableNoBorder);
            }
        }
	    protected Color headerTextColour;
	    protected string headerTextAlignment;
	    protected bool useSectionNumberInHeader;
	    bool showRoundInHeading;
	    bool isHeadingBold;
	    protected string textAlignment;

	    Color columnHeadingBackColour;

	    int rowHeight;

		public OpsProcessScoresReport()
		{
            headerColour = SkinningDefs.TheInstance.GetColorData("table_header_color", SkinningDefs.TheInstance.GetColorData("table_header_colour", Color.FromArgb(176, 196, 222)));
            textColour = SkinningDefs.TheInstance.GetColorData("table_text_colour", Color.Black);
            tableBorderColour = SkinningDefs.TheInstance.GetColorData("table_border_color", Color.FromArgb(211, 211, 211));
            tableRowColour = SkinningDefs.TheInstance.GetColorData("table_row_colour", Color.White);
            tableRowAltColour = SkinningDefs.TheInstance.GetColorData("table_row_colour_alternate", Color.White);
            tableNoBorder = SkinningDefs.TheInstance.GetBoolData("table_no_border", false);
 
		    headerTextColour = SkinningDefs.TheInstance.GetColorData("table_header_text_colour", text_colour);
		    headerTextAlignment = SkinningDefs.TheInstance.GetData("table_header_alignment", "center");

            useSectionNumberInHeader = SkinningDefs.TheInstance.GetIntData("reports_use_numbered_sections", 1) == 1;
		    showRoundInHeading = SkinningDefs.TheInstance.GetBoolData("table_show_round", false);
		    isHeadingBold = SkinningDefs.TheInstance.GetBoolData("table_heading_bold", false);
		    textAlignment = SkinningDefs.TheInstance.GetData("table_text_alignment", "center");

		    columnHeadingBackColour = SkinningDefs.TheInstance.GetColorData("table_column_heading_colour", Color.White);
            
            rowHeight = SkinningDefs.TheInstance.GetIntData("table_row_height", 20);


		}

		public string BuildReport(NetworkProgressionGameFile gameFile, int round, RoundScores scores)
		{
			//Create the interim xml doc
			LibCore.BasicXmlDocument xdoc = LibCore.BasicXmlDocument.Create();
			XmlNode root = (XmlNode) xdoc.CreateElement("scorecard");
			xdoc.AppendChild(root);

            XmlNode cost;

			foreach(string section in scores.outer_sections.Keys)
			{
				ArrayList points = (ArrayList)scores.outer_sections[section];

				// Make sure we never have more than 5 points!
				int count = 0;

				foreach (string pt in points)
				{
					//if(count < 5)
					{
						string[] vals = pt.Split(':');

						string mangled_unique_name = section + "_" + vals[0];
						mangled_unique_name = mangled_unique_name.Replace(" ", "_");

						cost = (XmlNode) xdoc.CreateElement("cost");
						//((XmlElement)cost).SetAttribute( "name", vals[0] );
						((XmlElement)cost).SetAttribute( "name", mangled_unique_name );
						((XmlElement)cost).SetAttribute( "val", vals[1]);
						root.AppendChild(cost);

						++count;
					}
				}
			}
			
			cost = (XmlNode) xdoc.CreateElement("cost");
			((XmlElement)cost).SetAttribute( "name", "indicator" );
			((XmlElement)cost).SetAttribute( "val", CONVERT.ToPaddedStr(scores.IndicatorScore,2));
			root.AppendChild(cost);

			// Should be saved to disk!!!
			string fname = gameFile.Dir + "\\global\\round_scores_" + CONVERT.ToStr(round) + ".xml";
			xdoc.Save(fname);

			return xdoc.InnerXml;
		}


		public string CombineRoundResults(string[] roundresults, NetworkProgressionGameFile gameFile, int round, RoundScores scores, out int TableHeight)
		{
			string reportFile = gameFile.GetRoundFile(gameFile.LastRoundPlayed, "OpsProcessScoresReport_Round" + round + ".xml" , gameFile.LastPhasePlayed);

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

			int numrows = 0;
			
			int NumColumns = NUMROUNDS + 1;

		    string colwidths = SkinningDefs.TheInstance.GetData("table_title_col_width", "0.5");
			double remaining = 1 - CONVERT.ParseDouble(colwidths);
            int numCol = (gameFile.Game_Eval_Type == em_GameEvalType.ISO_20K) ? NUMROUNDS - 1 : NUMROUNDS;

			for (int j = 1; j <= numCol; j++)
			{
				colwidths += "," + CONVERT.ToStr(remaining / numCol);
			}

			LibCore.BasicXmlDocument xdoc = LibCore.BasicXmlDocument.Create();
			XmlNode root = (XmlNode) xdoc.CreateElement("table");
			((XmlElement)root).SetAttribute( "columns","1" );
			((XmlElement)root).SetAttribute( "rowheight",CONVERT.ToStr(rowHeight) );
            if (!tableNoBorder)
            {
                ((XmlElement)root).SetAttribute("border_colour", table_border_color);
            }
			
            ((XmlElement)root).SetAttribute( "no_border", table_no_border);
			((XmlElement)root).SetAttribute( "row_colour_1", table_row_color);
			((XmlElement)root).SetAttribute( "row_colour_2", table_row_color_alt);
			xdoc.AppendChild(root);

			//add the title table
			XmlNode titles = (XmlNode) xdoc.CreateElement("table");
			((XmlElement)titles).SetAttribute( "columns", CONVERT.ToStr(NumColumns) );
			((XmlElement)titles).SetAttribute( "widths",colwidths );
            if (!tableNoBorder)
            {
                ((XmlElement)titles).SetAttribute( "border_colour", table_border_color );
            }
			
            ((XmlElement)titles).SetAttribute( "no_border", table_no_border);
			((XmlElement)titles).SetAttribute( "row_colour_1", table_row_color);
			((XmlElement)titles).SetAttribute( "row_colour_2", table_row_color_alt);
			root.AppendChild(titles);

			XmlNode titlerow = (XmlNode) xdoc.CreateElement("rowdata");
			titles.AppendChild(titlerow);

			XmlNode cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement)cell).SetAttribute( "val","" );
            ((XmlElement)cell).SetAttribute("no_border", table_no_border);
		    ((XmlElement) cell).SetAttribute("colour", CONVERT.ToComponentStr(columnHeadingBackColour));
			titlerow.AppendChild(cell);
			for (int j=1; j<=numCol; j++)
			{
				cell = (XmlNode) xdoc.CreateElement("cell");
                
                ((XmlElement)cell).SetAttribute("textcolour", text_colour);
                ((XmlElement)cell).SetAttribute("no_border", table_no_border);
			    string val = CONVERT.ToStr(j);
                if (showRoundInHeading)
                {
                    val = CONVERT.Format("Round {0}", val);
                }
				((XmlElement)cell).SetAttribute( "val", val);
                if (isHeadingBold)
                {
                    ((XmlElement) cell).SetAttribute("textstyle", "bold");
                }
                ((XmlElement)cell).SetAttribute("colour", CONVERT.ToComponentStr(columnHeadingBackColour));
				titlerow.AppendChild(cell);
			}

			int count = 1;
			XmlNode row;
			bool first = true;

			numrows = 1;

			bool suppressSections = (CoreUtils.SkinningDefs.TheInstance.GetIntData("maturity_suppress_sections", 0) == 1);
			string header = SkinningDefs.TheInstance.GetData("process_scores_table_header");
			XmlNode lastPeopleTable = null;

			foreach(string section in scores.MaturityHashOrder)
			{
				ArrayList points = (ArrayList)ReportUtils.TheInstance.Maturity_Names[section];

				if (points != null)
				{
					XmlNode table;

					if (! suppressSections)
					{
						row = (XmlNode) xdoc.CreateElement("rowdata");
						root.AppendChild(row);
						cell = (XmlNode) xdoc.CreateElement("cell");

						string name = section;
                        if (useSectionNumberInHeader)
						{
							name = CONVERT.ToStr(count) + " - " + name;
						}
						((XmlElement)cell).SetAttribute( "val", name);
						((XmlElement)cell).SetAttribute( "align",headerTextAlignment );
						((XmlElement)cell).SetAttribute( "colour",header_colour );
						((XmlElement)cell).SetAttribute( "textcolour",CONVERT.ToComponentStr(headerTextColour) );
                        ((XmlElement)cell).SetAttribute("no_border", table_no_border);
						if (SkinningDefs.TheInstance.GetIntData("table_header_bold", 1) != 0)
						{
							((XmlElement)cell).SetAttribute( "textstyle", "bold" );
						}

                        row.AppendChild(cell);

						XmlNode peopletable = (XmlNode) xdoc.CreateElement("table");
						((XmlElement)peopletable).SetAttribute( "columns", CONVERT.ToStr(NumColumns) );
						((XmlElement)peopletable).SetAttribute( "widths",colwidths );
                        ((XmlElement)peopletable).SetAttribute("rowheight", CONVERT.ToStr(rowHeight));
					    if (!tableNoBorder)
					    {
					        ((XmlElement) peopletable).SetAttribute("border_colour", table_border_color);
					    }
					    ((XmlElement)peopletable).SetAttribute( "no_border", table_no_border);
						((XmlElement)peopletable).SetAttribute( "row_colour_1", table_row_color);
						((XmlElement)peopletable).SetAttribute( "row_colour_2", table_row_color_alt);
						root.AppendChild(peopletable);

						count++;
						numrows++;

						table = peopletable;
					}
					else if (! string.IsNullOrEmpty(header))
					{
						if (first)
						{
							row = (XmlNode) xdoc.CreateElement("rowdata");
							root.AppendChild(row);
							cell = (XmlNode) xdoc.CreateElement("cell");

							string name = header;
							((XmlElement) cell).SetAttribute("val", name);
							((XmlElement) cell).SetAttribute("align", "middle");
							((XmlElement) cell).SetAttribute("colour", SkinningDefs.TheInstance.GetData("score_card_highlight_colour"));
							((XmlElement) cell).SetAttribute("textcolour", text_colour);
                            ((XmlElement)cell).SetAttribute("no_border", table_no_border);
							if (SkinningDefs.TheInstance.GetIntData("table_header_bold", 1) != 0)
							{
								((XmlElement) cell).SetAttribute("textstyle", "bold");
							}
							row.AppendChild(cell);

							XmlNode peopletable = (XmlNode) xdoc.CreateElement("table");
							((XmlElement) peopletable).SetAttribute("columns", CONVERT.ToStr(NumColumns));
							((XmlElement) peopletable).SetAttribute("widths", colwidths);
                            ((XmlElement)peopletable).SetAttribute("rowheight", CONVERT.ToStr(rowHeight));
						    if (!tableNoBorder)
						    {
						        ((XmlElement) peopletable).SetAttribute("border_colour", table_border_color);
						    }
						    ((XmlElement) peopletable).SetAttribute("no_border", table_no_border);
							((XmlElement) peopletable).SetAttribute("row_colour_1", table_row_color);
							((XmlElement) peopletable).SetAttribute("row_colour_2", table_row_color_alt);
							root.AppendChild(peopletable);

							count++;
							numrows++;

							table = peopletable;
							lastPeopleTable = peopletable;
						}
						else
						{
							table = lastPeopleTable;
						}
					}
					else
					{
						table = titles;
					}

					first = false;

					foreach (string pt in points)
					{
						string[] vals = pt.Split(':');

						string unique_mangled_name = section + "_" + vals[0];
						unique_mangled_name = unique_mangled_name.Replace(" ","_");

						row = (XmlNode) xdoc.CreateElement("rowdata");
						table.AppendChild(row);

						cell = (XmlNode) xdoc.CreateElement("cell");
						((XmlElement)cell).SetAttribute( "val",vals[0] );
                        ((XmlElement)cell).SetAttribute("textcolour", text_colour);
					    ((XmlElement) cell).SetAttribute("align", textAlignment);
                        ((XmlElement)cell).SetAttribute("no_border", table_no_border);
						row.AppendChild(cell);
						setRow(xdoc, row, xmldata, unique_mangled_name/*vals[0]*/, round);

						numrows++;
					}
				}
			}
			
			if (! suppressSections)
			{
				//add a title row 
				row = (XmlNode) xdoc.CreateElement("rowdata");
				root.AppendChild(row);
				cell = (XmlNode) xdoc.CreateElement("cell");
				string newName = "Maturity Indicator";
				if (useSectionNumberInHeader)
				{
					newName = CONVERT.ToStr(count) + " - " + newName;
				}
				((XmlElement)cell).SetAttribute( "val", newName );
				((XmlElement)cell).SetAttribute( "align", headerTextAlignment );
				((XmlElement)cell).SetAttribute( "colour",header_colour );
				((XmlElement)cell).SetAttribute( "textcolour",CONVERT.ToComponentStr(headerTextColour));
                ((XmlElement)cell).SetAttribute("no_border", table_no_border);
				if (SkinningDefs.TheInstance.GetIntData("table_header_bold", 1) != 0)
				{
					((XmlElement)cell).SetAttribute( "textstyle", "bold" );
				}
				row.AppendChild(cell);
			}

			XmlNode indictable = (XmlNode) xdoc.CreateElement("table");
			((XmlElement)indictable).SetAttribute( "columns", CONVERT.ToStr(NumColumns) );
			((XmlElement)indictable).SetAttribute( "widths",colwidths );
            ((XmlElement)indictable).SetAttribute("rowheight", CONVERT.ToStr(rowHeight));
		    if (!tableNoBorder)
		    {
		        ((XmlElement) indictable).SetAttribute("border_colour", table_border_color);
		    }
		    ((XmlElement)indictable).SetAttribute( "no_border", table_no_border);
			((XmlElement)indictable).SetAttribute( "row_colour_1", table_row_color);
			((XmlElement)indictable).SetAttribute( "row_colour_2", table_row_color_alt);
			root.AppendChild(indictable);

			row = (XmlNode) xdoc.CreateElement("rowdata");
			indictable.AppendChild(row);

			cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement)cell).SetAttribute( "val","Indicator Score" );
		    ((XmlElement) cell).SetAttribute("align", textAlignment);
            ((XmlElement)cell).SetAttribute("no_border", table_no_border);
			row.AppendChild(cell);
			setRow(xdoc, row, xmldata, "indicator", round);

			numrows += 2;

			TableHeight = numrows * rowHeight;
			
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

			int count = 0;

			for (int x = 0; x < NUMROUNDS; x++)
			{
				if (x < round)
				{
					if (xmldata[x] == null) continue;

					totalformat = "";

					XmlNode root = xmldata[x].DocumentElement;

					foreach (XmlNode child in root)
					{
						if (child.Attributes["name"].Value == costType)
						{
							++count;

							cell = (XmlNode) xdoc.CreateElement("cell");
                            ((XmlElement)cell).SetAttribute("textcolour", text_colour);
                            ((XmlElement)cell).SetAttribute("no_border", table_no_border);

							string val = child.Attributes["val"].Value;

							//add to the total?
							if (child.Attributes["sum"] != null)
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
								if (nf == "currency")
								{
									double tmp = CONVERT.ParseDouble(val);
									if (tmp < 0)
									{
										tmp = tmp * (-1);
										val = "($" + CONVERT.ToStr(tmp) + ")";
										((XmlElement) cell).SetAttribute("textcolour", "255,0,0");
									}
									else
									{
										val = "$" + val;
									}
								}
								if (nf == "percentage") val = val + "%";
							}

							((XmlElement) cell).SetAttribute("val", val);
                            ((XmlElement)cell).SetAttribute("no_border", table_no_border);
							row.AppendChild(cell);
						}
					}
				}
				else
				{
					++count;
					cell = (XmlNode) xdoc.CreateElement("cell");
                    ((XmlElement)cell).SetAttribute("textcolour", text_colour);
                    ((XmlElement)cell).SetAttribute("no_border", table_no_border);
					((XmlElement) cell).SetAttribute("val", " ");
					row.AppendChild(cell);
				}
			}
			if (showtotal)
			{
				cell = (XmlNode) xdoc.CreateElement("cell");
                ((XmlElement)cell).SetAttribute("textcolour", text_colour);
                ((XmlElement)cell).SetAttribute("no_border", table_no_border);

				string totalstr = "";
				if (totalformat == "currency")
				{
					if (total < 0)
					{
						total = total * (-1);
						totalstr = "($" + CONVERT.ToPaddedStr(total,2) + ")";
					}
					else
					{
						totalstr = "$" + CONVERT.ToPaddedStr(total,2);
					}
				}
				else if (totalformat == "percentage") totalstr = CONVERT.ToPaddedStr(total,2) + "%";
				else totalstr = CONVERT.ToStr(total);
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
                ((XmlElement)cell).SetAttribute("textcolour", text_colour);
                ((XmlElement)cell).SetAttribute("no_border", table_no_border);
				((XmlElement)cell).SetAttribute( "val", avgstr );
				row.AppendChild(cell);
			}
		}
	}
}
