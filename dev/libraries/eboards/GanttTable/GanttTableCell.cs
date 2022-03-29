using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using LibCore;

using System.Xml;

using Charts;

namespace GanttTable
{
	/// <summary>
	/// Summary description for ImageTableCell.
	/// </summary>
	public class GanttTableCell : TextTableCell
	{
		protected OpsGanttChart _gantt;
		//
		// Store the last known position and size in case we have changed.
		//
		protected Point last_pos;
		protected Size last_size;

		public GanttTableCell()
			: base (null)
		{
			_gantt = new OpsGanttChart();
		}

		internal OpsGanttChart TheGanttChart
		{
			get
			{
				return _gantt;
			}
		}

		public bool LoadGanttChart(string data)
		{
			try
			{
				_gantt.LoadData( data );
			}
			catch { }
			//
			return false;
		}

		public override void Paint(DestinationDependentGraphics ddg)
		{
			if(_gantt != null)
			{
				WindowsGraphics g = (WindowsGraphics)ddg;

				if(_gantt.Parent == null)
				{
					// The Gantt Chart control has not been added to the table control.
					if(g.theControl != null)
					{
						// Work out where we are on the table control.
						// We can do that by using the Transform matrix.
						Point[] points = new Point[2];
						points[0].X = 0;
						points[0].Y = 0;
						g.Graphics.Transform.TransformPoints(points);
						_gantt.Location = points[0];
						_gantt.Size = new Size(this.width, this.height);
						//
						last_pos = points[0];
						last_size = _gantt.Size;
						//
						g.theControl.SuspendLayout();
						g.theControl.Controls.Add(_gantt);
						g.theControl.ResumeLayout(false);
					}
				}
				else
				{
					// We have to check to see if we have been resized...
					// Just re-position anyway!
					Point[] points = new Point[2];
					points[0].X = 0;
					points[0].Y = 0;
					g.Graphics.Transform.TransformPoints(points);
					_gantt.Location = points[0];
					//
					last_pos = points[0];
					//
					if( (this.width != last_size.Width) || (this.height != last_size.Height) )
					{
						_gantt.Size = new Size(this.width, this.height);
						last_size = _gantt.Size;
					}
				}

				//Rectangle dest_rect = new Rectangle(0,0,this.width, this.height);
				//g.Graphics.DrawImage(_bitmap, dest_rect, 0,0, _bitmap.Width, _bitmap.Height, GraphicsUnit.Pixel);
			}
		}
	}
}
