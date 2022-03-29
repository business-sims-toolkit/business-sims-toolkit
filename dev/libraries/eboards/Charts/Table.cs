using System;
using System.Drawing;
using System.Collections;
using System.Windows.Forms;
using LibCore;

namespace Charts
{
	/// <summary>
	/// A CellInfo class is the base class for any data that is held in a table cell.
	/// This data is graphics device independent (e.g. able to be drawn to a window or
	/// a PDF / Bitmap). Any types of cell that require rendering data have to be
	/// derived from this.
	/// 
	/// ????
	/// </summary>
	public class CellInfo
	{
		protected string _type;
		protected Hashtable _args = new Hashtable();

		public CellInfo(string type)
		{
			_type = type;
		}

		public string Type { get { return _type; } }

		public void SetArg(string arg, string val)
		{
			_args[arg] = val;
		}

		public string GetStringArg(string arg)
		{
			if(_args.ContainsKey(arg))
			{
				return (string) _args[arg];
			}

			return "";
		}

		public bool GetStringArg(string arg, out string val)
		{
			if(_args.ContainsKey(arg))
			{
				val = (string) _args[arg];
				return true;
			}

			val = "";
			return false;
		}

		public bool GetBooleanArg(string arg, out bool val)
		{
			if(_args.ContainsKey(arg))
			{
				val = CONVERT.ParseBool( (string) _args[arg], false);
				return true;
			}

			val = false;
			return false;
		}

		public bool GetBooleanArg (string arg)
		{
			if (_args.ContainsKey(arg))
			{
				return CONVERT.ParseBool((string) _args[arg], false);
			}

			return false;
		}

		public bool GetColorArg(string arg, out Color c)
		{
			if(_args.ContainsKey(arg))
			{
				string val = (string) _args[arg];
				//
				string[] parts = val.Split(',');
				//
				if (parts.Length==3)
				{
					int RedFactor = CONVERT.ParseInt(parts[0]);
					int GreenFactor = CONVERT.ParseInt(parts[1]);
					int BlueFactor = CONVERT.ParseInt(parts[2]);

					c = Color.FromArgb(RedFactor,GreenFactor,BlueFactor);

					return true;
				}
			}

			c = Color.White;
			return false;
		}
	}

	public class RowData
	{
		public Color Colour = Color.White;
		public bool hasColour = false;
		public ArrayList Data = new ArrayList();
		public int absolute_height = -1;
	}

	public class Table : BasePanel
	{
		public float TextScaleFactor
		{
			get => table.TextScaleFactor;

			set => table.TextScaleFactor = value;
		}

		protected PureTable table;

		protected int selOffsetX = 0;
		protected int selOffsetY = 0;
		protected int selWidth = 0;
		protected int selHeight = 0;
		protected TextTableCell selCell = null;

		protected Panel editPanel = null;
		protected TextBox editBox = null;

		public delegate void CellChangedHandler(Table sender, TextTableCell cell, string val);
		public event CellChangedHandler CellTextChanged;

		new protected Bitmap BackgroundImage = null;
		protected bool showBackImage = false;

		protected string currentTip = "";

		public int TableHeight
		{
			get
			{
				if (table == null)
				{
					return 0;
				}
				else
				{
					return table.TableHeight;
				}
			}
		}

		public bool Selectable
		{
			set
			{
				if(value)
				{
					this.Click += Table_Click;
					this.MouseMove += Table_MouseMove;
				}
				else
				{
					this.Click -= Table_Click;
					this.MouseMove -= Table_MouseMove;
				}
			}
		}

		protected virtual PureTable CreatePureTable()
		{
			return new PureTable();
		}

		protected virtual PureTable CreatePureTable(int r, int c)
		{
			return new PureTable(r,c);
		}

		public Table()
		{
			table = CreatePureTable();
			this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			this.SetStyle(ControlStyles.DoubleBuffer,true);
			this.SetStyle(ControlStyles.UserPaint, true);
			this.Resize += Table_Resize;
		}

		public int NumRows
		{
			get
			{
				return table.NumRows;
			}
		}

		public Table(int r, int c)
		{
			table = CreatePureTable(r,c);
			this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			this.SetStyle(ControlStyles.DoubleBuffer,true);
			this.SetStyle(ControlStyles.UserPaint, true);
			this.Resize += Table_Resize;
			this.VisibleChanged += Table_VisibleChanged;
		}

		void Table_Resize(object sender, EventArgs e)
		{
			if(null != table)
			{
				table.SetSize(this.Size);
				table.DoLayout();
				this.Invalidate();
			}
		}

		public void LoadData(string xmldata)
		{
			table.LoadData(xmldata);
		}

		public void SetBackImage(Bitmap newBack, bool showBack)
		{
		  BackgroundImage = newBack;
		  showBackImage = showBack;
		}

		//public void SetContentsTransparent()
		//{
		//  table.SetContentsTrans();
		//  Refresh();
		//}


		protected override void OnPaint(PaintEventArgs e)
		{
			WindowsGraphics wg = new WindowsGraphics();
			wg.Graphics = e.Graphics;
			wg.theControl = this;

			//table.Paint(wg);
			//base.OnPaint (e);

			//if (showBackImage)
			//{
			//  if (this.BackgroundImage != null)
			//  {
			//    wg.Graphics.DrawImage(this.BackgroundImage, 0, 0);
			//  }
			//}

			base.OnPaint (e);
			if (showBackImage)
			{
				if (this.BackgroundImage != null)
				{
					wg.Graphics.DrawImage(this.BackgroundImage, 0, 0);
				}
			} 
			table.Paint(wg);
			



			if(selWidth != 0)
			{
				wg.Graphics.ResetTransform();
				e.Graphics.DrawRectangle(Pens.Red, selOffsetX, selOffsetY, selWidth-1, selHeight-1);

				if("" != currentTip)
				{
					SizeF sf = e.Graphics.MeasureString(currentTip, this.Font);

					if(selOffsetX+selHeight >= this.Height)
					{
						e.Graphics.FillRectangle(Brushes.White, selOffsetX, (int)(selOffsetY-sf.Height), (int)sf.Width, (int) sf.Height);
						e.Graphics.DrawString(currentTip, this.Font, Brushes.Black, selOffsetX, (int)(selOffsetY-sf.Height));
						e.Graphics.DrawRectangle(Pens.Blue, selOffsetX, (int)(selOffsetY-sf.Height), (int)sf.Width, (int) sf.Height);
					}
					else
					{
						e.Graphics.FillRectangle(Brushes.White, selOffsetX, selOffsetY+selHeight, (int)sf.Width, (int) sf.Height);
						e.Graphics.DrawString(currentTip, this.Font, Brushes.Black, selOffsetX, selOffsetY+selHeight);
						e.Graphics.DrawRectangle(Pens.Blue, selOffsetX, selOffsetY+selHeight, (int)sf.Width, (int) sf.Height);
					}
				}
			}
		}

		void Table_Click(object sender, EventArgs e)
		{
			// User has tried to click on a cell...
			DisposeEditBox();

			if(null != selCell)
			{
				this.SuspendLayout();
				// Create a text entry box that sits over the cell and allows the user to type in there...
				editPanel = new Panel();
				editPanel.Location = new Point(this.selOffsetX+1, this.selOffsetY+1);
				editPanel.Size = new Size(this.selWidth - 2, this.selHeight - 2);

				editBox = new TextBox();
				editBox.Text = selCell.Text;
				editBox.Tag = selCell;
				editBox.TextAlign = LibCore.Alignment.GetHorizontalAlignmentFromStringAlignment(LibCore.Alignment.GetHorizontalAlignment(selCell.TextAlignment));
				editBox.BorderStyle = BorderStyle.None;
				editBox.Font = selCell.GetFont();
				editBox.KeyPress += editBox_KeyPress;
				// Calc the required height for the text box.
				int diffHeight = editBox.PreferredHeight - selHeight;
				editBox.Location = new Point(0, -diffHeight/2);
				editBox.Size = new Size(this.selWidth-2, diffHeight);
				//
				editBox.LostFocus += editBox_LostFocus;
				editPanel.SuspendLayout();
				editPanel.Controls.Add(editBox);
				editPanel.ResumeLayout(false);
				this.Controls.Add(editPanel);
				editBox.SelectAll();
				editBox.Focus();

				this.ResumeLayout(false);
			}
		}

		void Table_MouseMove(object sender, MouseEventArgs e)
		{
			int oldW = selWidth;
			selOffsetX = 0;
			selOffsetY = 0;
			selWidth = 0;
			CheckHover(table, e.X, e.Y);
			//
			if( (selWidth != 0) || (oldW != selWidth) )
			{
				this.Invalidate();
			}
		}

		void CheckHover(PureTable ptable, int x, int y)
		{
			// User has rolled the mouse over the table window...
			for(int i=0; i<ptable.NumRows; ++i)
			{
				TableCell tc = ptable.getCell(i,0);
				//
				if( (y >= tc.Location.Y) && (y <= tc.Location.Y + tc.Height) )
				{
					// Mouse hover is on this row so search along it...
					int cols = ptable.GetNumColumns(i);

					for(int j=0; j<cols; ++j)
					{
						TableCell tc2 = ptable.getCell(i,j);
						if( (x >= tc2.Location.X) && (x <= tc2.Location.X + tc2.Width) )
						{
							// Mouse is inside this cell.
							// If this cell is in fact a further table then more searching is required...
							PureTable pt = tc2 as PureTable;
							if(null != pt)
							{
								selOffsetX += pt.Left;
								selOffsetY += pt.Top;
								CheckHover(pt, x-pt.Left, y-pt.Top);
								return;
							}
							else
							{
								// We are hovered over this cell...
								// If it is editable then we highlight it...
								if(tc2.Editable)
								{
									selOffsetX += tc2.Left;
									selOffsetY += tc2.Top;
									selHeight = tc2.Height;
									selWidth = tc2.Width;
									selCell = tc2 as TextTableCell;

									// Check to see if there is a tool ti for this cell....
									currentTip = tc2.ToolTipText;
								}
								return;
							}
						}
					}

					return;
				}
			}
		}
	
		protected override void Dispose(bool disposing)
		{
			if(disposing)
			{
				DisposeEditBox();

				if(this.table != null)
				{
					//table.Dispose();
					table = null;
				}
			}

			base.Dispose (disposing);
		}

		void EnterChangedText()
		{
			if(editBox == null) return;

			TextTableCell ttc = editBox.Tag as TextTableCell;
			if(ttc != null)
			{
				if(editBox.Text != "")
				{
					if(ttc.Text != editBox.Text)
					{
						if(ttc.ToolTipText == "")
						{
							ttc.ToolTipText = "Original Value: " + ttc.Text;
						}

						ttc.Text = editBox.Text;

						// Mark this cell with the "edited" colour...
						ttc.SetBackColor( Color.FromArgb(246,255,183) );

						if(CellTextChanged != null)
						{
							CellTextChanged(this, ttc, editBox.Text);
						}
					}
				}
			}
		}

		void editBox_KeyPress(object sender, KeyPressEventArgs e)
		{
			// We only allow digits for just now!
			TextTableCell ttc = editBox.Tag as TextTableCell;
			if(ttc != null)
			{
				if(e.KeyChar == 13)
				{
					// Enter pressed so change the value of the text box.
					DisposeEditBox();
				}
				else if (char.IsDigit(e.KeyChar) || e.KeyChar == 8)
				{
					//if its a digit, allow
					e.Handled = false;
				}
				else
				{
					//if its not, tell system we are dealing with it but just ignore it
					//this ensures only digits are allowed
					e.Handled = true;
					return;
				}
			}
		}

		void editBox_LostFocus(object sender, EventArgs e)
		{
			DisposeEditBox();
		}

		protected bool dirtyLock = false;

		public void DisposeEditBox()
		{
			EnterChangedText();

			if (dirtyLock) return;

			if(editBox != null)
			{
				dirtyLock = true;

				editPanel.Controls.Remove(editBox);
				editBox.Dispose();
				editBox = null;

				this.Controls.Remove(editPanel);
				editPanel.Dispose();
				editPanel = null;

				dirtyLock = false;
			}
		}

		public void Flush()
		{
			DisposeEditBox();
		}

		void Table_VisibleChanged(object sender, EventArgs e)
		{
			if(!this.Visible)
			{
				DisposeEditBox();
			}
		}

		public int RowCountRecursively
		{
			get
			{
				if (table != null)
				{
					return table.RowCountRecursively;
				}
				else
				{
					return 0;
				}
			}
		}

		public void SetAllRowSizesRecursively (int height)
		{
			if (table != null)
			{
				table.SetAllRowSizesRecursively(height);
			}
		}
	}
}