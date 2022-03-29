using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Xml;
using Algorithms;
using CommonGUI;
using CoreUtils;
using LibCore;

namespace Charts
{
	public class HeatMap : FlickerFreePanel
	{
		class RenderParameters
		{
			public XAxis Axis;

			public int RowHeight;
			public int BlockSize;
			public int LeftColumnWidth;
			public int LeftColumnGutter;

			public Font KeyFont;
			public Font AxisFont;
			public Font AxisTitleFont;
			public Font TitleFont;
			public Font RowTitleFont;
			public Font SummaryTitleFont;
			public Font SummaryMainFont;

			public RenderParameters (int rowHeight, int blockSize, int leftColumnWidth, int leftColumnGutter,
									 Font keyFont, Font axisFont, Font axisTitleFont, Font titleFont,
									 Font rowTitleFont, Font summaryTitleFont, Font summaryMainFont,
			                         XAxis axis)
			{
				RowHeight = rowHeight;
				BlockSize = blockSize;
				LeftColumnWidth = leftColumnWidth;
				LeftColumnGutter = leftColumnGutter;

				KeyFont = keyFont;
				AxisFont = axisFont;
				AxisTitleFont = axisTitleFont;
				TitleFont = titleFont;
				RowTitleFont = rowTitleFont;
				SummaryTitleFont = summaryTitleFont;
				SummaryMainFont = summaryMainFont;

				Axis = axis;
			}
		}

		class Category
		{
			string id;
			public string Id
			{
				get
				{
					return id;
				}
			}

			Color colour;
			public Color Colour
			{
				get
				{
					return colour;
				}
			}

			string legend;
			public string Legend
			{
				get
				{
					return legend;
				}
			}

			int blockMargin;
			int keyRowMargin;

			public Category (XmlElement root)
			{
				id = BasicXmlDocument.GetStringAttribute(root, "id");
				colour = BasicXmlDocument.GetColourAttribute(root, "colour", Color.Gray);
				legend = BasicXmlDocument.GetStringAttribute(root, "legend");

				blockMargin = 2;
				keyRowMargin = 5;
			}

			public void RenderKey (PaintEventArgs e, Rectangle rectangle, RenderParameters renderParameters)
			{
				Rectangle outerBlock = new Rectangle (rectangle.Left, rectangle.Top, renderParameters.BlockSize, renderParameters.BlockSize);
				Rectangle innerBlock = new Rectangle (outerBlock.Left + blockMargin, outerBlock.Top + blockMargin, outerBlock.Width - (2 * blockMargin), outerBlock.Height - (2 * blockMargin));

				using (Brush brush = new SolidBrush (colour))
				{
					e.Graphics.FillRectangle(brush, innerBlock);
					e.Graphics.DrawRectangle(Pens.Black, innerBlock);
				}

				Rectangle text = new Rectangle (outerBlock.Right + keyRowMargin, rectangle.Top, rectangle.Right - (outerBlock.Right + keyRowMargin), rectangle.Height);
				StringFormat format = new StringFormat ();
				format.Alignment = StringAlignment.Near;
				format.LineAlignment = StringAlignment.Center;
				e.Graphics.DrawString(legend, renderParameters.KeyFont, Brushes.Black, text, format);
			}

			public void Render (PaintEventArgs e, Rectangle blockRectangle, RenderParameters renderParameters)
			{
				int blockSize = renderParameters.BlockSize - (2 * blockMargin);
				Rectangle block = new Rectangle (blockRectangle.Left + ((blockRectangle.Width - blockSize) / 2), blockRectangle.Top + ((blockRectangle.Height - blockSize) / 2), blockSize, blockSize);

				using (Brush brush = new SolidBrush(colour))
				{
					e.Graphics.FillRectangle(brush, block);
					e.Graphics.DrawRectangle(Pens.Black, block);
				}
			}
		}

		class CategorySet
		{
			Dictionary<string, Category> idToCategory;

			int keyLeading;
			int keyGutter;

			public CategorySet (XmlElement root)
			{
				idToCategory = new Dictionary<string, Category> ();

				foreach (XmlElement child in root.ChildNodes)
				{
					Category category = new Category (child);

					idToCategory.Add(category.Id, category);
				}

				keyLeading = 5;
				keyGutter = 25;
			}

			public Category GetCategoryById (string id)
			{
				if (idToCategory.ContainsKey(id))
				{
					return idToCategory[id];
				}

				return null;
			}

			public void RenderKey (PaintEventArgs e, Rectangle bounds, RenderParameters renderParameters)
			{
				int y = 0;
				foreach (Category category in idToCategory.Values)
				{
					category.RenderKey(e, new Rectangle (bounds.Left + keyGutter, y, bounds.Width - keyGutter, renderParameters.BlockSize + keyLeading), renderParameters);
					y += renderParameters.BlockSize + keyLeading;
				}
			}
		}

		class XAxis
		{
			int min;
			public int Min
			{
				get
				{
					return min;
				}
			}

			int max;
			public int Max
			{
				get
				{
					return max;
				}
			}

			public XAxis (XmlElement root)
			{
				min = BasicXmlDocument.GetIntAttribute(root, "min", 0);
				max = BasicXmlDocument.GetIntAttribute(root, "max", 1);
			}
		}

		class Block
		{
			List<Row> rows;
			public ReadOnlyCollection<Row> Rows
			{
				get
				{
					return new ReadOnlyCollection<Row> (rows);
				}
			}

			public Block (XmlElement root, CategorySet categories)
			{
				rows = new List<Row> ();
				rows.Add(new TitleRow (root));

				foreach (XmlElement child in root.ChildNodes)
				{
					switch (child.Name)
					{
						case "axis_row":
							rows.Add(new AxisRow (child));
							break;

						case "map_row":
							rows.Add(new MapRow (child, categories));
							break;

						case "summary_row":
							rows.Add(new SummaryRow (child));
							break;
					}
				}
			}

			public void Render (PaintEventArgs e, Rectangle rectangle, RenderParameters renderParameters)
			{
				int y = rectangle.Top;
				foreach (Row row in rows)
				{
					Rectangle rowRectangle = new Rectangle (rectangle.Left, y, rectangle.Width, renderParameters.RowHeight);
					row.Render(e, rowRectangle, renderParameters);
					y += renderParameters.RowHeight;
				}
			}
		}

		abstract class Row
		{
			string leftLegend;
			public string LeftLegend
			{
				get
				{
					return leftLegend;
				}
			}

			public Row (XmlElement root)
			{
				leftLegend = BasicXmlDocument.GetStringAttribute(root, "left_legend");
			}

			public abstract void Render (PaintEventArgs e, Rectangle rowRectangle, RenderParameters renderParameters);
		}

		class TitleRow : Row
		{
			string title;
			public string Title
			{
				get
				{
					return title;
				}
			}

			public TitleRow (XmlElement root)
				: base (root)
			{
				title = BasicXmlDocument.GetStringAttribute(root, "title");
			}

			public override void Render (PaintEventArgs e, Rectangle rowRectangle, RenderParameters renderParameters)
			{
				e.Graphics.DrawRectangle(Pens.Black, rowRectangle);

				if (! string.IsNullOrEmpty(title))
				{
					StringFormat format = new StringFormat ();
					format.Alignment = StringAlignment.Center;
					format.LineAlignment = StringAlignment.Center;

					e.Graphics.DrawString(title, renderParameters.TitleFont, Brushes.Black, rowRectangle, format);
				}
			}
		}

		class MapRow : Row
		{
			List<Category> values;

			public ReadOnlyCollection<Category> Values
			{
				get
				{
					return new ReadOnlyCollection<Category>(values);
				}
			}

			public MapRow (XmlElement root, CategorySet categories)
				: base(root)
			{
				values = new List<Category>();

				foreach (string id in root.InnerText.SplitOnWhitespace())
				{
					values.Add(categories.GetCategoryById(id));
				}
			}

			public override void Render (PaintEventArgs e, Rectangle rowRectangle, RenderParameters renderParameters)
			{
				Rectangle leftColumn = new Rectangle(rowRectangle.Left, rowRectangle.Top, renderParameters.LeftColumnWidth,
					renderParameters.RowHeight);

				e.Graphics.DrawRectangle(Pens.Black, leftColumn);

				if (! string.IsNullOrEmpty(LeftLegend))
				{
					Rectangle leftColumnText = new Rectangle(leftColumn.Left + renderParameters.LeftColumnGutter, leftColumn.Top,
						leftColumn.Width - renderParameters.LeftColumnGutter, leftColumn.Height);

					StringFormat format = new StringFormat();
					format.Alignment = StringAlignment.Near;
					format.LineAlignment = StringAlignment.Center;

					e.Graphics.DrawString(LeftLegend, renderParameters.RowTitleFont, Brushes.Black, leftColumnText, format);
				}

				Rectangle rightColumn = new Rectangle(rowRectangle.Left + renderParameters.LeftColumnWidth, rowRectangle.Top,
					rowRectangle.Width - renderParameters.LeftColumnWidth, renderParameters.RowHeight);

				e.Graphics.DrawRectangle(Pens.Black, rightColumn);

				for (int i = 0; i < values.Count; i++)
				{
					Rectangle blockRectangle = new Rectangle(rightColumn.Left + (i * rightColumn.Width / values.Count),
						rightColumn.Top, rightColumn.Width / values.Count, rightColumn.Height);
					values[i].Render(e, blockRectangle, renderParameters);
				}
			}
		}

		class AxisRow : Row
		{
			public AxisRow (XmlElement root)
				: base (root)
			{
			}

			public override void Render (PaintEventArgs e, Rectangle rowRectangle, RenderParameters renderParameters)
			{
				Rectangle leftColumn = new Rectangle (rowRectangle.Left, rowRectangle.Top, renderParameters.LeftColumnWidth, renderParameters.RowHeight);

				e.Graphics.DrawRectangle(Pens.Black, leftColumn);

				if (! string.IsNullOrEmpty(LeftLegend))
				{
					Rectangle leftColumnText = new Rectangle(leftColumn.Left + renderParameters.LeftColumnGutter, leftColumn.Top, leftColumn.Width - renderParameters.LeftColumnGutter, leftColumn.Height);

					StringFormat format = new StringFormat();
					format.Alignment = StringAlignment.Near;
					format.LineAlignment = StringAlignment.Center;

					e.Graphics.DrawString(LeftLegend, renderParameters.AxisTitleFont, Brushes.Black, leftColumnText, format);
				}

				Rectangle rightColumn = new Rectangle (rowRectangle.Left + renderParameters.LeftColumnWidth, rowRectangle.Top, rowRectangle.Width - renderParameters.LeftColumnWidth, renderParameters.RowHeight);

				e.Graphics.DrawRectangle(Pens.Black, rightColumn);

				int count = renderParameters.Axis.Max + 1 - renderParameters.Axis.Min;
				for (int i = renderParameters.Axis.Min; i <= renderParameters.Axis.Max; i++)
				{
					Rectangle blockRectangle = new Rectangle (rightColumn.Left + (i * rightColumn.Width / count), rightColumn.Top, rightColumn.Width / count, rightColumn.Height);

					StringFormat format = new StringFormat ();
					format.Alignment = StringAlignment.Center;
					format.LineAlignment = StringAlignment.Center;

					int digits = (int) Math.Ceiling(Math.Log10(renderParameters.Axis.Max));
					string timeString = new string ('0', digits) + CONVERT.ToStr(i);
					timeString = timeString.Substring(timeString.Length - digits);

					e.Graphics.DrawString(timeString, renderParameters.AxisFont, Brushes.Black, blockRectangle, format);
				}
			}
		}

		class SummaryRow : Row
		{
			string mainLegend;
			public string MainLegend
			{
				get
				{
					return mainLegend;
				}
			}

			public SummaryRow (XmlElement root)
				 : base (root)
			{
				mainLegend = BasicXmlDocument.GetStringAttribute(root, "main_legend");
			}

			public override void Render (PaintEventArgs e, Rectangle rowRectangle, RenderParameters renderParameters)
			{
				Rectangle leftColumn = new Rectangle (rowRectangle.Left, rowRectangle.Top, renderParameters.LeftColumnWidth, renderParameters.RowHeight);

				e.Graphics.DrawRectangle(Pens.Black, leftColumn);

				if (! string.IsNullOrEmpty(LeftLegend))
				{
					Rectangle leftColumnText = new Rectangle (leftColumn.Left + renderParameters.LeftColumnGutter, leftColumn.Top, leftColumn.Width - renderParameters.LeftColumnGutter, leftColumn.Height);

					StringFormat format = new StringFormat();
					format.Alignment = StringAlignment.Near;
					format.LineAlignment = StringAlignment.Center;

					e.Graphics.DrawString(LeftLegend, renderParameters.SummaryTitleFont, Brushes.Black, leftColumnText, format);
				}

				Rectangle rightColumn = new Rectangle (rowRectangle.Left + renderParameters.LeftColumnWidth, rowRectangle.Top, rowRectangle.Width - renderParameters.LeftColumnWidth, renderParameters.RowHeight);

				e.Graphics.DrawRectangle(Pens.Black, rightColumn);

				if (! string.IsNullOrEmpty(mainLegend))
				{
					StringFormat format = new StringFormat ();
					format.Alignment = StringAlignment.Center;
					format.LineAlignment = StringAlignment.Center;

					e.Graphics.DrawString(mainLegend, renderParameters.SummaryMainFont, Brushes.Black, rightColumn, format);
				}
			}
		}

		CategorySet categories;
		XAxis axis;
		List<Block> blocks;
		bool showKey;

		int leftColumnWidth;
		public int LeftColumnWidth
		{
			get
			{
				return leftColumnWidth;
			}

			set
			{
				leftColumnWidth = value;
				Invalidate();
			}
		}

		int gap;
		public int Gap
		{
			get
			{
				return gap;
			}

			set
			{
				gap = value;
				Invalidate();
			}
		}

		int keyWidth;
		public int KeyWidth
		{
			get
			{
				return keyWidth;
			}

			set
			{
				keyWidth = value;
				Invalidate();
			}
		}

		Font keyFont;
		public Font KeyFont
		{
			get
			{
				return keyFont;
			}

			set
			{
				keyFont = value;
				Invalidate();
			}
		}

		Font axisFont;
		public Font AxisFont
		{
			get
			{
				return axisFont;
			}

			set
			{
				axisFont = value;
				Invalidate();
			}
		}

		Font titleFont;
		public Font TitleFont
		{
			get
			{
				return titleFont;
			}

			set
			{
				titleFont = value;
				Invalidate();
			}
		}

		Font axisTitleFont;
		public Font AxisTitleFont
		{
			get
			{
				return axisTitleFont;
			}

			set
			{
				axisTitleFont = value;
				Invalidate();
			}
		}

		Font rowTitleFont;
		public Font RowTitleFont
		{
			get
			{
				return rowTitleFont;
			}

			set
			{
				rowTitleFont = value;
				Invalidate();
			}
		}

		Font summaryTitleFont;
		public Font SummaryTitleFont
		{
			get
			{
				return summaryTitleFont;
			}

			set
			{
				summaryTitleFont = value;
				Invalidate();
			}
		}

		Font summaryMainFont;
		public Font SummaryMainFont
		{
			get
			{
				return summaryMainFont;
			}

			set
			{
				summaryMainFont = value;
				Invalidate();
			}
		}

		bool autoWidth;
		public bool AutoWidth
		{
			get
			{
				return autoWidth;
			}

			set
			{
				autoWidth = value;
				Invalidate();
			}
		}

		public HeatMap ()
		{
			keyWidth = 150;
			leftColumnWidth = 150;
			gap = 15;

			autoWidth = false;

			FontFamily font = new FontFamily (SkinningDefs.TheInstance.GetData("fontname", "Verdana"));
			float fontSize = 8.5f;

			keyFont = ConstantSizeFont.NewFont(font, fontSize);
			axisFont = ConstantSizeFont.NewFont(font, fontSize * 0.75f);
			titleFont = ConstantSizeFont.NewFont(font, fontSize, FontStyle.Bold);
			axisTitleFont = ConstantSizeFont.NewFont(font, fontSize, FontStyle.Bold);
			rowTitleFont = ConstantSizeFont.NewFont(font, fontSize);
			summaryTitleFont = ConstantSizeFont.NewFont(font, fontSize, FontStyle.Bold);
			summaryMainFont = ConstantSizeFont.NewFont(font, fontSize);
		}

		public void LoadData (string xmlString)
		{
			BasicXmlDocument xml = BasicXmlDocument.Create(xmlString);

			XmlElement root = xml.DocumentElement;

			showKey = BasicXmlDocument.GetBoolAttribute(root, "show_key", true);

			blocks = new List<Block> ();

			foreach (XmlElement child in root.ChildNodes)
			{
				switch (child.Name)
				{
					case "categories":
						categories = new CategorySet (child);
						break;

					case "xaxis":
						axis = new XAxis (child);
						break;

					case "block":
						blocks.Add(new Block (child, categories));
						break;
				}
			}
		}

		protected override void OnPaint (PaintEventArgs e)
		{
			base.OnPaint(e);

			int totalRows = 0;
			int totalBlocks = 0;
			foreach (Block block in blocks)
			{
				int blockRows = block.Rows.Count;

				totalRows += blockRows;
				totalBlocks++;
			}

			if (totalRows < 1)
			{
				return;
			}

			Rectangle keyRectangle = new Rectangle (Width - keyWidth, 0, keyWidth, Height);
			Rectangle mainRectangle = new Rectangle (0, 0, keyRectangle.Left - gap, Height);

			int rowHeight = (mainRectangle.Height - ((totalBlocks - 1) * gap)) / totalRows;
			int blockSize = Math.Min(rowHeight, (mainRectangle.Width - leftColumnWidth) / (axis.Max - axis.Min));
			rowHeight = blockSize;

			if (autoWidth)
			{
				int blockColumns = axis.Max + 1 - axis.Min;
				int rightColumnWidth = blockColumns * rowHeight;
				int leftAndRightColumnsWidth = leftColumnWidth + rightColumnWidth;
				int totalWidth = leftAndRightColumnsWidth + keyWidth;
				mainRectangle = new Rectangle ((Width - totalWidth) / 2, 0, leftAndRightColumnsWidth, Height);
				keyRectangle = new Rectangle (mainRectangle.Right, 0, keyWidth, Height);				
			}

			RenderParameters renderParameters = new RenderParameters (rowHeight, blockSize, leftColumnWidth, 5,
																	  keyFont, axisFont, axisTitleFont,
																	  titleFont, rowTitleFont, summaryTitleFont, summaryMainFont,
																	  axis);
			if (showKey)
			{
				categories.RenderKey(e, keyRectangle, renderParameters);
			}

			int y = 0;
			foreach (Block block in blocks)
			{
				Rectangle rectangle = new Rectangle (mainRectangle.Left, y, mainRectangle.Width, (block.Rows.Count * rowHeight));
				block.Render(e, rectangle, renderParameters);
				y = rectangle.Bottom + gap;
			}
		}
	}
}