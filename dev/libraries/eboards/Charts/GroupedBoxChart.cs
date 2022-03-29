using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Xml;

using LibCore;
using ResizingUi;

namespace Charts
{
	public class GridGroupedBoxChart : SharedMouseEventControl
	{
		public GridGroupedBoxChart(XmlNode xml, Func<XmlElement, BoxChart> derivedCtor, string boxType)
		{
			foreach (XmlElement child in xml.ChildNodes)
			{
				if (child.Name == boxType)
				{
					var panelBackColour = CONVERT.ParseComponentColor(child.GetStringAttribute("back_colour", "255,255,255"));
					boxPanels = child.ChildNodes.Cast<XmlElement>().Select(e =>
					{
						var box = derivedCtor(e);
						box.BackColor = panelBackColour;
						box.FontSizeToFitChanged += box_FontSizeToFitChanged;
						Controls.Add(box);
						return box;
					}).ToList();
				}
			}
		}

		public int ColumnCount
		{
			get => columnCount;
			set
			{
				if (value < 1)
				{
					throw new Exception("Column count can't be less than one");
				}

				columnCount = value;
				DoSize();
			}
		}
		int columnCount = 5;

		public int RowCount
		{
			get => rowCount;
			set
			{
				if (value < 1)
				{
					throw new Exception("Row count can't be less than one");
				}

				rowCount = value;
				DoSize();
			}
		}
		int rowCount = 3;

		public override List<KeyValuePair<string, Rectangle>> BoundIdsToRectangles =>
			new List<KeyValuePair<string, Rectangle>>
			{
				new KeyValuePair<string, Rectangle>("box_chart_all", RectangleToScreen(ClientRectangle))
			};

		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);

			DoSize();
		}

		void DoSize()
		{
			DoubleBuffered = true;

			const float heightPadding = 5;
			const float widthPadding = 5;

			const float innerPadding = 5;
			
			var width = Width - 2 * widthPadding - (ColumnCount - 1) * innerPadding;
			var panelWidth = width / ColumnCount;
			var height = Height - 2 * heightPadding - (RowCount - 1) * innerPadding;
			var panelHeight = height / RowCount;

			for (var i = 0; i < boxPanels.Count; i++)
			{
				var x = widthPadding + i % ColumnCount * (panelWidth + innerPadding);
				var y = heightPadding + (int)(i / ColumnCount) * (panelHeight + innerPadding);

				boxPanels[i].Bounds = new Rectangle((int)x, (int)y, (int)panelWidth, (int)panelHeight);
			}
		}

		void box_FontSizeToFitChanged (object sender, EventArgs e)
		{
			var fontSize = boxPanels.Where(b => Math.Abs(b.FontSizeToFit) > float.Epsilon).Min(b => b.FontSizeToFit);

			foreach (var box in boxPanels)
			{
				box.FontSize = fontSize;
			}
		}

		readonly List<BoxChart> boxPanels;
		
	}
}
