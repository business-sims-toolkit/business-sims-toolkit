using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Collections;
using LibCore;
using System.Xml;

namespace Charts
{

	public class PureTable : TableCell
	{
		float textScaleFactor = 1;

		public float TextScaleFactor
		{
			get => textScaleFactor;

			set
			{
				textScaleFactor = value;

				if (rows != null)
				{
					foreach (ArrayList row in rows)
					{
						foreach (TableCell cell in row)
						{
							if (cell is TextTableCell)
							{
								((TextTableCell) cell).TextScaleFactor = textScaleFactor;
							}
							else if (cell is PureTable)
							{
								((PureTable) cell).TextScaleFactor = textScaleFactor;
							}
						}
					}
				}
			}
		}

		protected ArrayList rows;
		protected double[] row_heights;
		double[] col_widths;

		double baseRowHeight;
		protected double rowheight => baseRowHeight * textScaleFactor;

		protected bool have_border_colour = false;
		protected Color border_colour;

		protected Color rowBackground1 = Color.White;
		protected Color rowBackground2 = Color.Transparent;

		protected int rowIndex;
		protected ArrayList tabledata;

		protected int[] Absolute_Row_Heights;

		protected bool auto_translate = true;

		public int TableHeight
		{
			get
			{
				if (rows == null)
				{
					return 0;
				}
				else
				{
					int h = 0;

					// TODO: : This is nasty and should go somewhere else.  Ideally, the table
					// classes should set their own Size fields automatically.
					foreach (ArrayList arow in rows)
					{
						int rowMaxHeight = (int) rowheight;

						foreach (TableCell tc in arow)
						{
							if (tc is PureTable)
							{
								PureTable pt = (PureTable) tc;
								rowMaxHeight = Math.Max(rowMaxHeight, pt.TableHeight);
							}
						}

						h += rowMaxHeight;
					}

					return h;
				}
			}
		}

        public override void Paint(DestinationDependentGraphics ddg)
		{
            WindowsGraphics g = (WindowsGraphics) ddg;

			Matrix m = g.Graphics.Transform.Clone();
			//m.Translate(left,top);
			//
			if (have_border_colour)
			{
				using (Pen pen = new Pen (border_colour))
				{
					g.Graphics.DrawRectangle(pen,0,0,width-1,height-1);
				}
			}
			else
			{			
				g.Graphics.DrawRectangle(Pens.Black,0,0,width-1,height-1);
			}
			//
			foreach(ArrayList arow in rows)
			{
				foreach(TableCell tc in arow)
				{
					g.Graphics.Transform = m.Clone();
					//g.TranslateTransform( left, top );
					g.Graphics.TranslateTransform( tc.Left, tc.Top );
					tc.Paint(ddg);
				}
			}
		}

		public int NumRows
		{
			get
			{
				return rows.Count;
			}
		}

		public int GetNumColumns(int row)
		{
			ArrayList arow = (ArrayList) rows[row];
			return arow.Count;
		}

		public override Size Size
		{
			get { return base.Size; }
			set
			{
				base.Size = value;
				DoLayout();
			}
		}

		public void LoadData(string xmldata)
		{
			BasicXmlDocument xdoc = LibCore.BasicXmlDocument.Create(xmldata);
			XmlNode rootNode = xdoc.DocumentElement;

			ReadTableData(rootNode);
		}
		
		public void DoLayout()
		{
			// : Tweak so we don't get upset if we try to resize an empty table.
			if (rows == null)
			{
				return;
			}

			// Check how much left over height we have separate from absolute heights...
			// More efficient to do this earlier of course.
			double remaining_height = Height;
			int rows_to_count = 0;
			if(Absolute_Row_Heights != null)
			{
				for(int i=0; i<Absolute_Row_Heights.Length; ++i)
				{
					if(Absolute_Row_Heights[i] >= 0)
					{
						remaining_height -= Absolute_Row_Heights[i];
					}
					else
					{
						++rows_to_count;
					}
				}
			}
			//
			if(remaining_height < 0) remaining_height = 0;
			//
			double default_height = remaining_height / ((double)rows_to_count);
			//

			//this.SuspendLayout();
			double yoffset = 0.0;
			double point_should_be_at = 0.0;

			for(int r=0; r<rows.Count; ++r)
			{
				ArrayList arow = (ArrayList) rows[r];
				double xoffset = 0.0;
				int h = 0;
				if (rowheight != 0)
				{
					//set absolute value of each row
					h = (int)rowheight;

					if(this.Absolute_Row_Heights[r] >= 0)
					{
						h = Absolute_Row_Heights[r];
					}
					else
					{
						PureTable pt = arow[0] as PureTable;
						if(null != pt)
						{
							// TODO : Hmmm, is this logic correct. Seems a little suspect.
							// I think it is trying to deduce row heights for a table in a table
							// in quite a poor way (single depth assumption).
							h = (int)rowheight * pt.rows.Count;
						}
					}

					point_should_be_at += h;
/* Following code was very bad as it assumed no sub-classing.
					if (((ArrayList)rows[r])[0].GetType().ToString() == "Charts.PureTable")
					{
						h = (int)rowheight * ((PureTable)((ArrayList)rows[r])[0]).rows.Count;
					}*/
				}
				else
				{
					if ((this.Absolute_Row_Heights != null) && (this.Absolute_Row_Heights[r] >= 0))
					//if (this.Absolute_Row_Heights[r] >= 0)
					{
						h = Absolute_Row_Heights[r];

						point_should_be_at += h;
					}
					else
					{
						// 15-01-2008 : We are now ignoring the pre-calculated row heights
						// for now so that we can set some to absolute values...
						//h = (int)(row_heights[r] * remaining_height);

						// If we could assign double point accuracy we would be at...
						point_should_be_at += default_height;

						h = (int) (point_should_be_at - yoffset);

						//if on last row, stretch to fit total height
						if (r == rows.Count - 1)
						{
							h = (int) (remaining_height - yoffset);
						}
					}
				}

				for(int i=0; i<arow.Count; ++i)
				{
					double cw = col_widths[i];

					int w = (int)(cw*Width);
					TableCell c = (TableCell) arow[i];

					//if on last column, stretch to match total width
					if (i == arow.Count - 1)
					{
						w = Width - (int)xoffset;
					}
					
					c.SetLocation(new Point((int)xoffset,(int)yoffset));
					c.SetSize(new Size( w, h));

					xoffset += w;
				}

				yoffset += h;
			}
		}

        protected virtual PureTable CreateTable(int r, int c)
        {
            return new PureTable(r, c);
        }

        protected virtual PureTable CreateTable()
        {
			PureTable pt = new PureTable();
			if(this.have_border_colour)
			{
				pt.border_colour = this.border_colour;
			}
            return pt;
        }

		protected virtual void ProcessElement(XmlNode child)
		{
			if (child.Name == "table")
			{
				//got another table to read in
				PureTable newtable = CreateTable();//this.rowheight);
				newtable.baseRowHeight = baseRowHeight;
				newtable.TextScaleFactor = textScaleFactor;
				newtable.ReadTableData(child);

				tabledata.Add(newtable);
			}
			else if(child.Name == "rowdata")
			{
				RowData rowdata = ReadRowData(child);

				if (! rowdata.hasColour)
				{
					if ((rowIndex % 2) == 0)
					{
						rowdata.Colour = rowBackground1;
					}
					else
					{
						rowdata.Colour = rowBackground2;
					}
				}

				//add to array list of all rows
				tabledata.Add(rowdata);
				rowIndex++;
			}
		}

		protected virtual void ProcessCellInfo(CellInfo ci, int i, int j)
		{
			string val, cellname, tooltiptext;
			bool edit, tabbed, wrapped;
			//
			// Assumes cell type is "cell"...
			//
			ci.GetStringArg("val", out val);
			ci.GetBooleanArg("edit", out edit);
			ci.GetStringArg("cellname", out cellname);
			ci.GetStringArg("tooltiptext", out tooltiptext);
			ci.GetBooleanArg("tabbed", out tabbed);
			ci.GetBooleanArg("wrapped", out wrapped);

			TextTableCell ttc = setContent(i, j,
				val,
				edit,
				cellname,
				tooltiptext,
				tabbed,
				wrapped);

			if(have_border_colour)
			{
				ttc.SetBorderColour( border_colour );
			}

			Color c;
			if(ci.GetColorArg("colour", out c))
			{
				this.setBackColor(i, j, c);
			}
			if(ci.GetColorArg("textcolour", out c))
			{
				this.setForeColor(i, j, c);
			}

			if (ci.GetColorArg("border_colour", out c))
			{
				setBorderColor(i, j, c);
			}
			bool no_border_flag = false;
			if (ci.GetBooleanArg("no_border", out no_border_flag))
			{
				ttc.setNoBorder(no_border_flag);
			}

			if("bold" == ci.GetStringArg("textstyle"))
			{
				this.setFontStyle(i,j, FontStyle.Bold);
			}

			if("" != ci.GetStringArg("fontsize"))
			{
				setFontSize(i,j, CONVERT.ParseDouble( ci.GetStringArg("fontsize") ) );
			}

			string align = ci.GetStringArg("align");

			if ("left" == align)        { this.SetAlignment(i,j, System.Drawing.ContentAlignment.MiddleLeft); }
			else if ("center" == align) { this.SetAlignment(i,j, System.Drawing.ContentAlignment.MiddleCenter); }
			else if ("right" == align)  { this.SetAlignment(i,j, System.Drawing.ContentAlignment.MiddleRight); }
		}

		/*ArrayList*/ void ReadTableData(XmlNode Node)
		{
			tabledata = new ArrayList();

			//get table attributes
			string cols = "0";
			string alignment = string.Empty;
			string[] heights = null;
			string[] widths = null;

			rowIndex = 0;

			foreach(System.Xml.XmlAttribute att in Node.Attributes)
			{
				if (att.Name == "columns")
				{
					cols = att.Value;
				}
				else if (att.Name == "heights")
				{
					heights = att.Value.Split(',');
				}
				else if (att.Name == "widths")
				{
					widths = att.Value.Split(',');
				}
				else if (att.Name == "align")
				{
					alignment = att.Value;
				}
				else if (att.Name.ToLower() == "rowheight")
				{
					baseRowHeight = CONVERT.ParseDouble(att.Value);
				}
				else if(att.Name == "border_colour")
				{
					have_border_colour = true;

					string[] parts = att.Value.Split(',');
					if (parts.Length == 3)
					{
						int RedFactor = CONVERT.ParseInt(parts[0]);
						int GreenFactor = CONVERT.ParseInt(parts[1]);
						int BlueFactor = CONVERT.ParseInt(parts[2]);
						border_colour = Color.FromArgb(RedFactor, GreenFactor, BlueFactor);
					}
					else
					{
						border_colour = Color.Transparent;
					}
				}
				else if (att.Name == "row_colour_1")
				{
					string[] parts = att.Value.Split(',');
					if (parts.Length==3)
					{
						int RedFactor = CONVERT.ParseInt(parts[0]);
						int GreenFactor = CONVERT.ParseInt(parts[1]);
						int BlueFactor = CONVERT.ParseInt(parts[2]);
						rowBackground1 = Color.FromArgb(RedFactor,GreenFactor,BlueFactor);

						if (rowBackground2 == Color.Transparent)
						{
							rowBackground2 = rowBackground1;
						}
						//rowBackground1 = Color.Transparent;
					}
				}
				else if (att.Name == "row_colour_2")
				{
					string[] parts = att.Value.Split(',');
					if (parts.Length==3)
					{
						int RedFactor = CONVERT.ParseInt(parts[0]);
						int GreenFactor = CONVERT.ParseInt(parts[1]);
						int BlueFactor = CONVERT.ParseInt(parts[2]);
						rowBackground2 = Color.FromArgb(RedFactor,GreenFactor,BlueFactor);

						//rowBackground2 = Color.Transparent;
					}
				}
			}

			foreach(XmlNode child in Node.ChildNodes)
			{
				if(child.NodeType == XmlNodeType.Element)
				{
					ProcessElement(child);
				}
			}

			//need to wait until we have all the data before we know how many rows
			int numcols = CONVERT.ParseInt(cols);
			int numrows = tabledata.Count;
			this.SetTableSize(numrows, numcols);

			//set row heights
			if (heights != null)
			{
				int num = heights.Length;

				for (int i=0; i<num; i++)
				{
					if (i< numrows)
					{
						this.setRowHeight(i, CONVERT.ParseDouble(heights[i]));
					}
				}
			}

			//set column widths
			if (widths != null)
			{
				int num = widths.Length;

				for (int i=0; i<num; i++)
				{
					if (i< numcols)
					{
						this.setColWidth(i, CONVERT.ParseDouble(widths[i]));
					}
				}
			}

			//set table alignment
			if (alignment != string.Empty)
			{
				//only support left, center and right at the moment
				if (alignment == "left") this.SetAlignment(System.Drawing.ContentAlignment.MiddleLeft);
				if (alignment == "center") this.SetAlignment(System.Drawing.ContentAlignment.MiddleCenter);
				if (alignment == "right") this.SetAlignment(System.Drawing.ContentAlignment.MiddleRight);
			}

			if(tabledata.Count > 0)
			{
				Absolute_Row_Heights = new int[tabledata.Count];
			}

			for (int i=0; i< tabledata.Count; i++)
			{
				//string tmp = tabledata[i].ToString();

				RowData row = tabledata[i] as RowData;

				if(row != null)
				{
					Absolute_Row_Heights[i] = row.absolute_height;

					this.setRowColor(i, row.Colour);

					for (int j=0; j< row.Data.Count; j++)
					{
						CellInfo ci = row.Data[j] as CellInfo;
						if(ci != null)
						{
							ProcessCellInfo( (CellInfo) row.Data[j], i,j );
						}
						else
						{
							// Do we have a table instead of a normal cell?
							PureTable pt = row.Data[j] as PureTable;
							if(pt != null)
							{
								this.setContent(i,j, pt);
							}
						}
					}
				}
				else
				{
					PureTable rowtable = (PureTable)tabledata[i];
					this.setContent(i,0,rowtable);
					Absolute_Row_Heights[i] = -1;
				}
			}

			// The RowData is not required anymore.
			tabledata.Clear();
			//return tabledata;
		}

        protected virtual TextTableCell CreateTextTableCell()
        {
			TextTableCell ttc = new TextTableCell (this);
			if(have_border_colour)
			{
				ttc.SetBorderColour( border_colour );
			}

            return ttc;
        }

		protected void SetTableSize(int r, int c)
		{
			//BorderStyle = BorderStyle.None;
			rows = new ArrayList();
			row_heights = new double[r];
			col_widths = new double[c];

			Absolute_Row_Heights = new int[r];

			double h = 1.0/r;
			double w = 1.0/c;

			for(int i = 0; i < r ; i++)
			{
				row_heights[i] = h;
				Absolute_Row_Heights[i] = -1;
				//
				ArrayList row = new ArrayList();		
		
				for(int x = 0; x < c ; x++)
				{
					TextTableCell cell = CreateTextTableCell();
					cell.TextScaleFactor = textScaleFactor;
					row.Add(cell);
				}
				
				rows.Add(row);
			}

			for(int i=0; i<c; ++i)
			{
				col_widths[i] = w;
			}
		}

		public PureTable(int r, int c)
			: base (null)
		{
			SetTableSize(r, c);
		}

		public PureTable()
			: base (null)
		{
		}

		protected virtual void ProcessRowCellElement(RowData rowdata, XmlNode n)
		{
			if(n.Name == "table")
			{
				PureTable pt = CreateTable();
				pt.ReadTableData(n);
				rowdata.Data.Add(pt);
			}
			else
			{
				CellInfo ci = new CellInfo(n.Name);
				foreach(XmlAttribute att in n.Attributes)
				{
					ci.SetArg(att.Name, att.Value);
				}
				//add to arraylist
				rowdata.Data.Add(ci);
			}
		}

		RowData ReadRowData(XmlNode child)
		{
			RowData rowdata = new RowData();
			//ArrayList rowdata = new ArrayList();

			foreach(XmlAttribute att in child.Attributes)
			{
				if(att.Name == "colour")
				{
					//set colour for the row
					string[] parts = att.Value.Split(',');
					if (parts.Length==3)
					{
						int RedFactor = CONVERT.ParseInt(parts[0]);
						int GreenFactor = CONVERT.ParseInt(parts[1]);
						int BlueFactor = CONVERT.ParseInt(parts[2]);
						rowdata.Colour = Color.FromArgb(RedFactor,GreenFactor,BlueFactor);
						rowdata.hasColour = true;
					}
				}
				else if(att.Name == "absolute_height")
				{
					rowdata.absolute_height = CONVERT.ParseInt( att.Value );
				}
			}

			foreach(XmlNode n in child.ChildNodes)
			{
				if(n.NodeType == XmlNodeType.Element)
				{
					ProcessRowCellElement(rowdata, n);
				}
			}
			return rowdata;
		}
	
		public TableCell getCell(int r, int c)
		{
			ArrayList row = (ArrayList) rows[r];
			return (TableCell) row[c];
		}

		public void setBackColor(int r, int c, Color color)
		{
			TableCell cell = (TableCell) getCell(r,c);
			cell.SetBackColor(color);
		}

		public void setForeColor(int r, int c, Color color)
		{
			TableCell cell = (TableCell) getCell(r,c);
			cell.SetForeColor(color);
		}

		public void setBorderColor (int r, int c, Color color)
		{
			TableCell cell = (TableCell) getCell(r, c);
			cell.SetBorderColour(color);
		}

		public void setFontStyle(int r, int c, FontStyle fs)
		{
			TableCell cell = (TableCell) getCell(r,c);
			cell.SetFontStyle(fs);
		}

        public void setFont(int r, int c, Font f)
        {
            TableCell cell = (TableCell)getCell(r, c);
            cell.SetFont(f);
        }

		public void setFontSize(int r, int c, double size)
		{
			TableCell cell = (TableCell) getCell(r,c);
			cell.SetFontSize(size);
		}

		public void setColumnColor(int c, Color color)
		{
			foreach(ArrayList row in rows)
			{
				TableCell control = (TableCell) row[c];
				control.SetBackColor(color);
			}
		}

		public void setRowColor(int r, Color color)
		{
			ArrayList row = (ArrayList)rows[r];
			foreach(TableCell c in row)
			{
				c.SetBackColor(color);
			}
		}
		
		public void setContent(int r, int c, PureTable table)
		{
			TableCell cell = getCell(r,c);
			Size s = cell.GetSize();
			Point p = cell.GetLocation();

			ArrayList row = (ArrayList) rows[r];
			row[c] = table;
			table.Location = p;
			table.Size = s;
		}

		public new void SetAlignment(ContentAlignment ca)
		{
			foreach(ArrayList row in rows)
			{
				foreach(TableCell tc in row)
				{
					tc.SetAlignment(ca);
				}
			}
		}

		public void SetAlignment(int r, int c, ContentAlignment ca)
		{
			TableCell tc = getCell(r,c);
			tc.SetAlignment(ca);
		}
		
		public void setRowHeight(int r, double height)
		{
			row_heights[r] = height;
		}

		public void setColWidth(int c, double width)
		{
			//int[] tmp = {c,width};
			//this.col_widths.Add( tmp );
			col_widths[c] = width;

		    DoLayout();
		}

		public void setContent(int r, int c, string text, ContentAlignment alignment = ContentAlignment.MiddleLeft)
		{
			TableCell cell = getCell(r, c);

			TextTableCell ttc = cell as TextTableCell;
			if (null != ttc)
			{
				ttc.Text = text;
				ttc.Editable = false;
				ttc.CellName = "";
				ttc.ToolTipText = "";
                ttc.SetAlignment(alignment);
				return;
			}

			Size s = cell.GetSize();
			Point p = cell.GetLocation();

			ttc = new TextTableCell (this);
			ttc.Text = text;
			ttc.Editable = false;
			ttc.CellName = "";
			ttc.ToolTipText = "";
		    ttc.SetAlignment(alignment);
			ArrayList row = (ArrayList)rows[r];
			row[c] = ttc;
			ttc.Location = p;
			ttc.Size = s;
		}

		public TextTableCell setContent(int r, int c, string text, bool editable, string cellname, string tooltiptext, bool tabbedBackground)
		{
			TableCell cell = getCell(r,c);

			TextTableCell ttc = cell as TextTableCell;
			if(null != ttc)
			{
				ttc.Text = text;
				ttc.Editable = editable;
				ttc.CellName = cellname;
				ttc.ToolTipText = tooltiptext;
				ttc.tabbedBackground = tabbedBackground;
				return ttc;
			}

			Size s = cell.GetSize();
			Point p = cell.GetLocation();

			ttc = new TextTableCell (this);
			ttc.Text = text;
			ttc.Editable = editable;
			ttc.CellName = cellname;
			ttc.ToolTipText = tooltiptext;
			ttc.tabbedBackground = tabbedBackground;
			ArrayList row = (ArrayList) rows[r];
			row[c] = ttc;
			ttc.Location = p;
			ttc.Size = s;

			return ttc;
		}

		public TextTableCell setContent(int r, int c, string text, bool editable, string cellname, string tooltiptext, bool tabbedBackground, bool wrapped)
		{
			TableCell cell = getCell(r,c);

			TextTableCell ttc = cell as TextTableCell;
			if(null != ttc)
			{
				ttc.Text = text;
				ttc.Editable = editable;
				ttc.CellName = cellname;
				ttc.ToolTipText = tooltiptext;
				ttc.tabbedBackground = tabbedBackground;
				ttc.wrapped = wrapped;
				return ttc;
			}

			Size s = cell.GetSize();
			Point p = cell.GetLocation();

			ttc = new TextTableCell (this);
			ttc.Text = text;
			ttc.Editable = editable;
			ttc.CellName = cellname;
			ttc.ToolTipText = tooltiptext;
			ttc.tabbedBackground = tabbedBackground;
			ArrayList row = (ArrayList) rows[r];
			row[c] = ttc;
			ttc.Location = p;
			ttc.Size = s;
			ttc.wrapped = wrapped;

			return ttc;
		}

		public int RowCountRecursively
		{
			get
			{
				if (this.rows == null)
				{
					return 0;
				}
				else
				{
					int rows = 0;

					foreach (ArrayList arow in this.rows)
					{
						int thisRowHeightInRows = 1;

						foreach (TableCell tc in arow)
						{
							if (tc is PureTable)
							{
								PureTable pt = (PureTable) tc;
								thisRowHeightInRows = pt.RowCountRecursively;
							}
						}

						rows += thisRowHeightInRows;
					}

					return rows;
				}
			}
		}

		public void SetAllRowSizesRecursively (int height)
		{
			baseRowHeight = height;

			if (rows != null)
			{
				foreach (ArrayList arow in this.rows)
				{
					foreach (TableCell tc in arow)
					{
						if (tc is PureTable)
						{
							PureTable pt = (PureTable) tc;
							pt.SetAllRowSizesRecursively(height);
						}
					}
				}
			}
		}
	}
}