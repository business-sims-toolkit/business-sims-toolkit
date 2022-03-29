using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Text;
using System.Windows.Forms;

namespace BaseUtils
{
	/// <summary>
	/// Displays data in tabular format.
	/// </summary>
	public class TableViewControl : System.Windows.Forms.UserControl
	{
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		System.ComponentModel.Container components = null;

		TableViewTable table = null;
		IList list;
		bool drawBorder;

		/// <summary>
		/// Creates an instance of TableViewControl.
		/// </summary>
		public TableViewControl()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			// Defaults
			table = new TableViewTable();

			// Double buffered
			SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.DoubleBuffer, true);
		}

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		/// <summary>
		/// Render the table.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnPaint(PaintEventArgs e)
		{
			e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
			//e.Graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;

			if (list != null)
				table.TableRenderer.RenderList(e.Graphics, list);

			if (table != null)
				RenderColumns(e.Graphics);

		}

		/// <summary>
		/// Render the columns.
		/// </summary>
		/// <param name="g"></param>
		void RenderColumns(Graphics g)
		{
			Pen p;

			if (drawBorder)
			{
				p = new Pen(this.ForeColor, 1f);
				g.DrawRectangle(p, 0, 0, this.Width - 1, this.Height - 1);
				g.DrawLine(p, 0, table.TableRenderer.TitleHeight - 1, this.Width - 1, table.TableRenderer.TitleHeight - 1);
			}
			else
			{
				p = new Pen(this.ForeColor, 2f);
			}

			for (int c = 0; c < table.Count; c++)
			{
				if (c > 0)
					g.DrawLine(p, table[c].X - 3, 0, table[c].X - 3, this.Height);
			}

			p.Dispose();
		}

		/// <summary>
		/// Update the default table colours.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnForeColorChanged(EventArgs e)
		{
			table.TableRenderer.TitleColor = this.ForeColor;
			table.TableRenderer.TableColor = this.ForeColor;
			Invalidate();
			base.OnForeColorChanged (e);
		}

		/// <summary>
		/// Update the default font.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnFontChanged(EventArgs e)
		{
			table.TableRenderer.TableFont = this.Font;
			Invalidate();
			base.OnFontChanged (e);
		}

		protected override void OnResize(EventArgs e)
		{
			Invalidate();
			base.OnResize (e);
		}

		/// <summary>
		/// Gets or Sets the default Title Font.
		/// </summary>
		public Font TitleFont
		{
			get { return table.TableRenderer.TitleFont; }
			set 
			{ 
				table.TableRenderer.TitleFont = value; 
				Invalidate();
			}
		}

		/// <summary>
		/// Gets or Sets the default Title Colour.
		/// </summary>
		public Color TitleColor
		{
			get { return table.TableRenderer.TitleColor; }
			set 
			{ 
				table.TableRenderer.TitleColor = value; 
				Invalidate();
			}
		}

		/// <summary>
		/// Gets the associated TableViewTable.
		/// </summary>
		public TableViewTable Table
		{
			get { return table; }
		}

		/// <summary>
		/// Determines whether or not to draw a border for the table.
		/// </summary>
		public bool DrawBorder
		{
			get { return drawBorder; }
			set { drawBorder = value; }
		}

		/// <summary>
		/// Gets or Sets the data source for the table.
		/// </summary>
		public IList DataSource
		{
			get { return list; }
			set 
			{ 
				list = value; 
				Invalidate();
			}
		}

		#region Component Designer generated code
		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			components = new System.ComponentModel.Container();
		}
		#endregion
	}
}
