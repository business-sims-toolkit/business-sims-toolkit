using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;

using System.IO;
using Charts;

namespace GanttTable
{
	/// <summary>
	/// ImageTable extends Table to create a form based table that supports images inside cells.
	/// </summary>
	public class GanttTable : Table
	{
		// Store all tables that can contain Gantt charts so we can extract them if
		// necessary.
		protected ArrayList pureGanttTables = new ArrayList();
		//
		public GanttTable()
		{
		}

		public ArrayList GetEmbeddedGanttCharts()
		{
			ArrayList array = new ArrayList();

			foreach(PureGanttTable pgt in pureGanttTables)
			{
				array.AddRange(pgt.GetEmbeddedGanttCharts());
				/*
				foreach(OpsGanttChart ganttChart in pgt.ganttCharts)
				{
					pureGanttTables.AddRange(pgt.ganttCharts);
				}*/
			}

			return array;
		}

		protected override PureTable CreatePureTable()
		{
			PureGanttTable pgt = new PureGanttTable();
			pureGanttTables.Add(pgt);
			return pgt;
		}

		protected override PureTable CreatePureTable(int r, int c)
		{
			PureGanttTable pgt = new PureGanttTable();
			pureGanttTables.Add(pgt);
			return pgt;
		}
	}
}
