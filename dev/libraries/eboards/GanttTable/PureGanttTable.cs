using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;

using Charts;

using System.Xml;

namespace GanttTable
{
	/// <summary>
	/// ImageTable extends PureTable to create a "pure" class that supports images inside cells.
	/// </summary>
	public class PureGanttTable : PureTable
	{
		/// <summary>
		/// Store the Gantt charts in te table so that they can be extracted externally to
		/// the table.
		/// </summary>
		protected ArrayList ganttCharts = new ArrayList();
		protected ArrayList pureGanttTables = new ArrayList();

		public ArrayList GetEmbeddedGanttCharts()
		{
			ArrayList array = new ArrayList();

			array.AddRange(ganttCharts);

			foreach(PureGanttTable pgt in pureGanttTables)
			{
				array.AddRange(pgt.GetEmbeddedGanttCharts());
			}

			return array;
		}

		public PureGanttTable()
		{
		}

		public PureGanttTable(int r, int c) : base(r,c)
		{
		}

		protected override void ProcessCellInfo(CellInfo ci, int r, int c)
		{
			if(ci.Type == "opsgantt")
			{
				GanttTableCell gtc = new GanttTableCell();
				gtc.LoadGanttChart( ci.GetStringArg("data") );

				ganttCharts.Add(gtc.TheGanttChart);

				TableCell cell = getCell(r,c);
				Size s = cell.GetSize();
				Point p = cell.GetLocation();

				//ttc.CellName = cellname;
				//ttc.ToolTipText = tooltiptext;
				ArrayList row = (ArrayList) rows[r];
				row[c] = gtc;
				gtc.Location = p;
			}
			else if(ci.Type == "image")
			{
				ImageTableCell itc = new ImageTableCell();
				itc.SetImageFile( ci.GetStringArg("file") );
				itc.SetImageAlign(ci.GetStringArg("image_align"), ci.GetStringArg("image_vertical_align"));

				TableCell cell = getCell(r,c);
				Size s = cell.GetSize();
				Point p = cell.GetLocation();

				itc.Text = ci.GetStringArg("val");
				itc.Editable = ci.GetBooleanArg("edit");
				itc.CellName = ci.GetStringArg("cellname");
				itc.ToolTipText = ci.GetStringArg("tooltiptext");
				itc.tabbedBackground = ci.GetBooleanArg("tabbed");
				itc.wrapped = ci.GetBooleanArg("wrapped");

				if (have_border_colour)
				{
					itc.SetBorderColour(border_colour);
				}

				Color colour;
				if (ci.GetColorArg("colour", out colour))
				{
					itc.SetBackColor(colour);
				}
				if (ci.GetColorArg("textcolour", out colour))
				{
					itc.SetForeColor(colour);
				}

				if (ci.GetColorArg("border_colour", out colour))
				{
					itc.SetBorderColour(colour);
				}


				ArrayList row = (ArrayList) rows[r];
				row[c] = itc;
				itc.Location = p;
			}
			else
			{
				base.ProcessCellInfo(ci,r,c);
			}
		}

		protected override void ProcessRowCellElement(RowData rowdata, XmlNode n)
		{
			if(n.Name == "opsgantt")
			{
				CellInfo ci = new CellInfo(n.Name);
				// Spew XML tree from this point down into a "data" field.
				ci.SetArg("data", n.OuterXml);
				//add to arraylist
				rowdata.Data.Add(ci);
			}
			else base.ProcessRowCellElement(rowdata, n);
		}

		protected override PureTable CreateTable(int r, int c)
		{
			PureGanttTable ptg = new PureGanttTable(r, c);
			pureGanttTables.Add(ptg);
			return ptg;
		}

		protected override PureTable CreateTable()
		{
			PureGanttTable pt = new PureGanttTable();
			pureGanttTables.Add(pt);
			//
			if(this.have_border_colour)
			{
				pt.border_colour = this.border_colour;
			}
			return pt;
		}
	}
}
