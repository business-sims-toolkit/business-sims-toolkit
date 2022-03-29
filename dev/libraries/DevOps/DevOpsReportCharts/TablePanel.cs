using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

using Charts;
using DevOpsReportBuilders;
using Events;
using ResizingUi;

namespace DevOpsReportCharts
{
	public delegate TableReportBuildResult TableReportBuilderHandler();
	public class TablePanel : SharedMouseEventControl
	{
		public TablePanel (string reportFilename)
		{
			table = new Table
			{
				BorderStyle = BorderStyle.None,
				Size = Size
			};
			table.LoadData(File.ReadAllText(reportFilename));
			Controls.Add(table);

			table.Height = table.TableHeight;
		}
		
		protected override void OnSizeChanged(EventArgs e)
		{
			DoSize();
		}

		void DoSize()
		{
			const int tableXOffset = 30;

			var tableWidth = Width - 2 * tableXOffset;
			table.TextScaleFactor = Math.Min(tableWidth * 1.0f / 940, Height * 1.0f / 600);
			table.Bounds = new Rectangle(tableXOffset, 0, tableWidth, Math.Min(Height, table.TableHeight));

			Invalidate();
		}

		readonly Table table;

		public override List<KeyValuePair<string, Rectangle>> BoundIdsToRectangles =>
			new List<KeyValuePair<string, Rectangle>> { new KeyValuePair<string, Rectangle>("table", RectangleToScreen(table.Bounds)) };

		public override void ReceiveMouseEvent(SharedMouseEventArgs args)
		{

		}
	}
}
