using System;

using System.Collections.Generic;
using System.Linq;
using System.Xml;

using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;

using CommonGUI;

using LibCore;
using CoreUtils;

namespace Charts
{
	public class TimeChart : FlickerFreePanel
	{
		protected class Block
		{
			public float Start;
			public float End;
			public string Legend;
			public string SmallLegend;
			public string BottomLegend;
			public Color Colour;
			public Color TextColour;
			public Color BorderColour;
			public bool Hollow;
			public bool Striped;

			public Block (XmlElement root)
			{
				Legend = BasicXmlDocument.GetStringAttribute(root, "legend");
				SmallLegend = BasicXmlDocument.GetStringAttribute(root, "small_legend");
				BottomLegend = BasicXmlDocument.GetStringAttribute(root, "bottom_legend");
				Colour = CONVERT.ParseComponentColor(BasicXmlDocument.GetStringAttribute(root, "colour"));
				TextColour = CONVERT.ParseComponentColor(BasicXmlDocument.GetStringAttribute(root, "textcolour", "0,0,0"));
				BorderColour = CONVERT.ParseComponentColor(BasicXmlDocument.GetStringAttribute(root, "border_colour", CONVERT.ToComponentStr(Colour)));
				Start = (float) BasicXmlDocument.GetDoubleAttribute(root, "start", 0);
				End = (float) BasicXmlDocument.GetDoubleAttribute(root, "end", 0);
				Hollow = BasicXmlDocument.GetBoolAttribute(root, "hollow", false);
				Striped = BasicXmlDocument.GetBoolAttribute(root, "striped", false);
			}
		}

		protected class MouseoverBlock
		{
			public float Start;
			public float End;
			public string Legend;

			public MouseoverBlock (XmlElement root)
			{
				Legend = BasicXmlDocument.GetStringAttribute(root, "legend");
				Start = (float) BasicXmlDocument.GetDoubleAttribute(root, "start", 0);
				End = (float) BasicXmlDocument.GetDoubleAttribute(root, "end", 0);
			}

			public bool IsPointInside (Timeline timeline, RectangleF fullRectangle, Point point)
			{
				float left = MapFromRange(Start, timeline.Start, timeline.End, fullRectangle.Left, fullRectangle.Right);
				float right = MapFromRange(End, timeline.Start, timeline.End, fullRectangle.Left, fullRectangle.Right);
				RectangleF rectangle = new RectangleF (left, (int) fullRectangle.Top, right - left, (int) fullRectangle.Height);

				return rectangle.Contains(point);
			}
		}

		protected class Marker
		{
			public readonly float Start;
			public readonly MarkerType Type;
			public readonly Color Colour;
			public readonly Color? BackgroundColour;
			public readonly string Legend;

			public Marker(XmlElement root)
			{
				Start = (float) BasicXmlDocument.GetDoubleAttribute(root, "start", 0);
				Colour = CONVERT.ParseComponentColor(BasicXmlDocument.GetStringAttribute(root, "colour", "0,0,0"));
				Legend = BasicXmlDocument.GetStringAttribute(root, "legend", "");

				var backgroundColour = BasicXmlDocument.GetStringAttribute(root, "background_colour", "");
				if (!string.IsNullOrEmpty(backgroundColour))
				{
					BackgroundColour = CONVERT.ParseComponentColor(backgroundColour);
				}

				if (!Enum.TryParse(BasicXmlDocument.GetStringAttribute(root, "type", "square"), true, out Type))
				{
					Type = MarkerType.Square;
				}
			}
		}

		protected class MouseoverMarker
		{
			public float Start;
			public string Legend;

			public MouseoverMarker(XmlElement root)
			{
				Legend = BasicXmlDocument.GetStringAttribute(root, "legend");
				Start = (float)BasicXmlDocument.GetDoubleAttribute(root, "start", 0);
			}

			public bool IsPointInside(Timeline timeline, RectangleF fullRectangle, Point point)
			{
				float position = MapFromRange(Start, timeline.Start, timeline.End, fullRectangle.Left, fullRectangle.Right);
				var rectangle = new RectangleF(position, (int)fullRectangle.Top, fullRectangle.Height/2, (int)fullRectangle.Height);

				return rectangle.Contains(point);
			}
		}

		protected class Row
		{
			public string Legend;
			public int header_width = -1;
			public Color Colour;
			public Color FontColour;
			public List<Block> Blocks;
			public List<MouseoverBlock> MouseoverBlocks;
			public List<MouseoverMarker> MouseoverMarkers;
			public List<Marker> Markers;

			public Row (XmlElement root)
			{
				Legend = BasicXmlDocument.GetStringAttribute(root, "legend");
				header_width = BasicXmlDocument.GetIntAttribute(root, "header_width",-1);
				Colour = CONVERT.ParseComponentColor(BasicXmlDocument.GetStringAttribute(root, "colour"));
				FontColour = CONVERT.ParseComponentColor(BasicXmlDocument.GetStringAttribute(root, "font_colour", "0,0,0"));

				Blocks = new List<Block> ();
				MouseoverBlocks = new List<MouseoverBlock> ();
				Markers = new List<Marker>();
				MouseoverMarkers = new List<MouseoverMarker>();
				foreach (XmlElement child in root.ChildNodes)
				{
					switch (child.Name)
					{
						case "block":
							Blocks.Add(new Block (child));
							break;

						case "mouseover_block":
							MouseoverBlocks.Add(new MouseoverBlock (child));
							break;

						case "marker":
							Markers.Add(new Marker(child));
							break;

						case "mouseover_marker":
							MouseoverMarkers.Add(new MouseoverMarker (child));
							break;
					}
				}
			}
		}

		protected class Section
		{
			public string Legend;
			public Color Colour;
			public Color FontColour;
			public int? HeaderWidth;
			public bool IsHeaderSection;

			public List<Block> Blocks;
			public List<MouseoverBlock> MouseoverBlocks;
			public List<Row> Rows;

			public Section (XmlElement root)
			{
				Legend = BasicXmlDocument.GetStringAttribute(root, "legend");
				HeaderWidth = BasicXmlDocument.GetIntAttribute(root, "header_width");
				Colour = CONVERT.ParseComponentColor(BasicXmlDocument.GetStringAttribute(root, "colour"));
				FontColour = CONVERT.ParseComponentColor(BasicXmlDocument.GetStringAttribute(root, "font_colour", "0,0,0"));
				IsHeaderSection = BasicXmlDocument.GetBoolAttribute(root, "is_header", false);

				Blocks = new List<Block> ();
				MouseoverBlocks = new List<MouseoverBlock> ();
				Rows = new List<Row> ();
				foreach (XmlElement child in root.ChildNodes)
				{
					switch (child.Name)
					{
						case "block":
							Blocks.Add(new Block (child));
							break;

						case "mouseover_block":
							MouseoverBlocks.Add(new MouseoverBlock (child));
							break;

						case "row":
							Rows.Add(new Row (child));
							break;
					}
				}
			}
		}

		protected class Timeline
		{
			public float Start;
			public float End;
			public float Interval;
			public string Legend;
			public Color Colour;

			public Timeline (XmlElement root)
			{
				Start = (float) BasicXmlDocument.GetDoubleAttribute(root, "start", 0);
				End = (float) BasicXmlDocument.GetDoubleAttribute(root, "end", 0);
				Interval = (float) BasicXmlDocument.GetDoubleAttribute(root, "interval", 0);
				Legend = BasicXmlDocument.GetStringAttribute(root, "legend");
				Colour = CONVERT.ParseComponentColor(BasicXmlDocument.GetStringAttribute(root, "colour", "255,255,255"));
			}
		}

		protected enum TimelineLabelAlignment
		{
			Left,
			Center,
			Right
		}

		protected Timeline timeline;
		protected float MinRowHeight = 15;
		protected float MaxRowHeight = 50;
		protected float TimelineRightMargin = 0;
		protected Color RenderColour = CONVERT.ParseComponentColor(SkinningDefs.TheInstance.GetData("table_text_colour", "0,0,0"));
		protected TimelineLabelAlignment TimelineLabelAlingment = TimelineLabelAlignment.Center;

		public int TotalNumberOfRows
		{
			get
			{
				return sections.SelectMany(x => x.Rows).Count();
			}
		}

		public float MaxRequiredHeight
		{
			get
			{
				// 100 minimum height and add 100 for the timeline
				var height = TotalNumberOfRows*MaxRowHeight + 100;
				return height > 100 ? height : 100;
			}
		}

		protected List<Section> sections;
		protected Font font;
		Font smallFont;
		protected Point? mouseoverPoint;

		protected bool keyEnabled;

		public TimeChart (XmlElement root)
		{
			sections = new List<Section> ();
			foreach (XmlElement child in root.ChildNodes)
			{
				switch (child.Name)
				{
					case "timeline":
						timeline = new Timeline(child);
						break;

					case "sections":
						foreach (XmlElement grandchild in child.ChildNodes)
						{
							switch (grandchild.Name)
							{
								case "section":
									sections.Add(new Section(grandchild));
									break;
							}
						}
						break;
				}
			}

			font = ConstantSizeFont.NewFont(SkinningDefs.TheInstance.GetData("fontname", "Verdana"), 10);
			smallFont = ConstantSizeFont.NewFont(SkinningDefs.TheInstance.GetData("fontname", "Verdana"), 6);

			keyEnabled = false;

			sectionHeaderWidth = 75;
			rowHeaderWidth = 75;
		}

		public static float MapFromRange (float t, float t0, float t1, float y0, float y1)
		{
			return y0 + ((t - t0) * (y1 - y0) / (t1 - t0));
		}

		protected void RenderMarker(Graphics graphics, Marker marker, RectangleF fullRectangle)
		{
			var position = MapFromRange(marker.Start, timeline.Start, timeline.End, fullRectangle.Left, fullRectangle.Right);
			var size = fullRectangle.Height/2;
			var rectangle = new RectangleF(position, fullRectangle.Top + (size/2), size, size);
			var backgroundRectangle = new RectangleF(position, fullRectangle.Top, size, fullRectangle.Height);

			if (marker.BackgroundColour.HasValue)
			{
				graphics.FillRectangle(new SolidBrush(marker.BackgroundColour.Value), backgroundRectangle);
			}

			using (var brush = new SolidBrush(marker.Colour))
			{
				switch (marker.Type)
				{
					case MarkerType.Square:
						graphics.FillRectangle(brush, rectangle);
						break;
					case MarkerType.Circle:
						graphics.FillEllipse(brush, rectangle);
						break;
					case MarkerType.Bar:
						Color lightColour = Lerp(0.2f, marker.Colour, Color.White);
						Color darkColour = Lerp(0.2f, marker.Colour, Color.Black);
						using (Brush gradientBrush = new LinearGradientBrush(backgroundRectangle, lightColour, darkColour, 90))
						{
							graphics.FillRectangle(gradientBrush, backgroundRectangle);
						}
						break;
					case MarkerType.Line:
						using (Pen pen = new Pen (marker.Colour, 3))
						{
							graphics.DrawLine(pen, position, fullRectangle.Top, position, fullRectangle.Bottom);
						}
						break;
				}
			}
		}

		protected void RenderBlock (Graphics graphics, Block block, RectangleF fullRectangle)
		{
			RenderTimedBlock(graphics, block.Colour, block.Hollow, block.Start, block.End, fullRectangle, block.Legend, block.SmallLegend, block.BottomLegend, block.TextColour, font, block.BorderColour, block.Striped);
		}

		protected virtual void RenderTimedBlock (Graphics graphics, Color colour, bool borderOnly, float startTime, float endTime, RectangleF fullRectangle, string legend, string smallLegend, string bottomLegend, Color textColour, Font font, Color borderColour, bool striped = false)
		{
			float left = MapFromRange(startTime, timeline.Start, timeline.End, fullRectangle.Left, fullRectangle.Right);
			float right = MapFromRange(endTime, timeline.Start, timeline.End, fullRectangle.Left, fullRectangle.Right);

			RectangleF rectangle = new RectangleF (left, (int) fullRectangle.Top, right - left, (int) fullRectangle.Height);

			if (borderOnly)
			{
				float thickness = 4;

				using (Pen pen = new Pen (colour, thickness))
				{
					graphics.DrawRectangle(pen, (int) (rectangle.Left + (thickness / 2)), (int) (rectangle.Top + (thickness / 2)), (int) (rectangle.Width - thickness), (int) (rectangle.Height - thickness));
				}
			}
			else
			{
				// There's a bug in LinearGradientBrush that can render the edges wrongly: expand the brush rectangle to work around it.
				RectangleF gradientRectangle = new RectangleF (rectangle.Left - 1, rectangle.Top - 1, rectangle.Width + 2, rectangle.Height + 2);

				Color lightColour = Lerp(0.2f, colour, Color.White);
				Color darkColour = Lerp(0.2f, colour, Color.Black);

				using (Brush brush = new LinearGradientBrush(gradientRectangle, lightColour, darkColour, 90))
				{
					graphics.FillRectangle(brush, rectangle);

					if(striped)
					{
						for (var i = 1; i < 9; i++)
						{
							if (i % 2 != 0) continue;

							var rectangleEighthHeight = rectangle.Height/8;
							RectangleF stripeRectangle = new RectangleF(rectangle.Left,
																		rectangle.Top + (rectangleEighthHeight * i - 1),
																		rectangle.Width, rectangleEighthHeight);
							RectangleF stripeGradientRectangle = new RectangleF(stripeRectangle.Left - 1,
																				stripeRectangle.Top - 1,
																				stripeRectangle.Width + 2,
																				stripeRectangle.Height + 2);

							Color stripeColour = Lerp(0.5f, colour, RenderColour);
							Color lightStripeColour = Lerp(0.2f, stripeColour, Color.White);
							Color darkStripeColour = Lerp(0.2f, stripeColour, Color.Black);
							using (Brush stripeBrush = new LinearGradientBrush(stripeGradientRectangle, lightStripeColour, darkStripeColour, 90))
							{
								graphics.FillRectangle(stripeBrush, stripeRectangle);
							}
						}
					}
				}

				float thickness = 1;
				using (Pen pen = new Pen(borderColour, thickness))
				{
					graphics.DrawRectangle(pen, (int) (rectangle.Left + (thickness/2)),
											(int) (rectangle.Top + (thickness/2)),
											(int) (rectangle.Width - thickness), (int) (rectangle.Height - thickness));
				}
			}

			if (! keyEnabled)
			{
				RenderText(graphics, font, legend, textColour, rectangle, StringAlignment.Center, StringAlignment.Center);
			}

			if (smallLegend != "")
			{
				RenderText(graphics, smallFont, smallLegend, textColour, rectangle, StringAlignment.Center, StringAlignment.Near);
			}

			if (bottomLegend != "")
			{
				RenderText(graphics, smallFont, bottomLegend, textColour, rectangle, StringAlignment.Center, StringAlignment.Far);
			}
		}

		protected int Lerp (float t, int a, int b)
		{
			return ((int) (a + (t * (b - a))));
		}

		protected Color Lerp (float t, Color a, Color b)
		{
			return Color.FromArgb(Lerp(t, a.A, b.A), Lerp(t, a.R, b.R), Lerp(t, a.G, b.G), Lerp(t, a.B, b.B));
		}

		protected void RenderText (Graphics graphics, Font font, string text, Color colour, RectangleF rectangle, StringAlignment alignment, StringAlignment lineAlignment)
		{
			StringFormat format = new StringFormat ();
			format.Alignment = alignment;
			format.LineAlignment = lineAlignment;

			using (Brush brush = new SolidBrush (colour))
			{
				graphics.DrawString(text, font, brush, rectangle, format);
			}
		}

		protected int sectionHeaderWidth;

		public int SectionHeaderWidth
		{
			get
			{
				return sectionHeaderWidth;
			}

			set
			{
				sectionHeaderWidth = value;
				Invalidate();
			}
		}

		protected int rowHeaderWidth;

		public int RowHeaderWidth
		{
			get
			{
				return rowHeaderWidth;
			}

			set
			{
				rowHeaderWidth = value;
				Invalidate();
			}
		}

		protected override void OnPaint (PaintEventArgs e)
		{
			base.OnPaint(e);

			int rows = 0;
			foreach (Section section in sections)
			{
				rows += section.Rows.Count;
			}

			if(sections.Any()) sectionHeaderWidth = sections.Max(x => x.HeaderWidth ?? sectionHeaderWidth);
			if(sections.Any() && sections.SelectMany(x => x.Rows).Any()) rowHeaderWidth = sections.SelectMany(x => x.Rows).Max(x => x.header_width);

			float sectionLeading = 0;

			float timelineHeight = 50;
			float timelineLeftMargin = 10;
			float timelineRightMargin = 10;
			float timelineLeft = sectionHeaderWidth + rowHeaderWidth;
			float timelineRight = Width - TimelineRightMargin;
			float timelineWidth = timelineRight - timelineLeft;
			float timelineTickHeight = 10;
			float timelineLabelHeight = 20;
			float timelineLegendHeight = 20;

			float rowHeight = (int) Math.Max(MinRowHeight, Math.Min(MaxRowHeight, (Height - timelineHeight - (sections.Count * sectionLeading * 2)) / Math.Max(1, rows)));

			float y = 0;

			MouseoverBlock activeMouseover = null;
			MouseoverMarker activeMouseoverMarker = null;

			foreach (Section section in sections)
			{
				if (section.HeaderWidth.HasValue)
				{
					sectionHeaderWidth = section.HeaderWidth.Value;
				}

				float sectionHeight = (rowHeight * section.Rows.Count) + (sectionLeading * 2);
				RectangleF sectionRectangle = new RectangleF (0, (int) y, Width - timelineRightMargin, (int) sectionHeight);
				RectangleF sectionTitleRectangle = new RectangleF (0, (int) y, sectionHeaderWidth, (int) sectionHeight);
				RectangleF sectionContentsRectangle = new RectangleF (timelineLeft + timelineLeftMargin, (int) y, timelineWidth - (timelineLeftMargin + timelineRightMargin), (int) sectionHeight);

				using (Brush sectionBrush = new SolidBrush (section.Colour))
				{
					e.Graphics.FillRectangle(sectionBrush, sectionRectangle);
				}

				RenderText(e.Graphics, font, section.Legend, section.FontColour, sectionTitleRectangle, StringAlignment.Near, StringAlignment.Center);

				foreach (Block block in section.Blocks)
				{
					RenderBlock(e.Graphics, block, sectionContentsRectangle);
				}

				if (mouseoverPoint.HasValue)
				{
					foreach (MouseoverBlock block in section.MouseoverBlocks)
					{
						if (block.IsPointInside(timeline, sectionContentsRectangle, mouseoverPoint.Value))
						{
							activeMouseover = block;
						}
					}
				}

				y += sectionLeading;
				foreach (Row row in section.Rows)
				{
					if (row.header_width != -1)
					{
						rowHeaderWidth = row.header_width;
					}

					RectangleF rowRectangle = new RectangleF (sectionHeaderWidth, (int) y, Width - sectionHeaderWidth, (int) rowHeight);
					RectangleF rowTitleRectangle = new RectangleF (sectionHeaderWidth, (int) y, rowHeaderWidth, (int) rowHeight);
					RectangleF rowContentsRectangle = new RectangleF (timelineLeft + timelineLeftMargin, (int) y, timelineWidth - (timelineLeftMargin + timelineRightMargin), (int) rowHeight);

					using (Brush rowBrush = new SolidBrush(row.Colour))
					{
						e.Graphics.FillRectangle(rowBrush, rowRectangle);
					}

					RenderText(e.Graphics, font, row.Legend, row.FontColour, rowTitleRectangle, StringAlignment.Near, StringAlignment.Center);

					foreach (Block block in row.Blocks)
					{
						RenderBlock(e.Graphics, block, rowContentsRectangle);
					}

					foreach (Marker marker in row.Markers)
					{
						RenderMarker(e.Graphics, marker, rowContentsRectangle);
					}

					if (row != section.Rows[0])
					{
						e.Graphics.DrawLine(new Pen(RenderColour, 1), rowRectangle.Left, rowRectangle.Top, rowRectangle.Right, rowRectangle.Top);
					}

					if (mouseoverPoint.HasValue)
					{
						foreach (MouseoverBlock block in row.MouseoverBlocks)
						{
							if (block.IsPointInside(timeline, rowContentsRectangle, mouseoverPoint.Value))
							{
								activeMouseover = block;
							}
						}

						foreach (MouseoverMarker mouseoverMarker in row.MouseoverMarkers)
						{
							if (mouseoverMarker.IsPointInside(timeline, rowContentsRectangle, mouseoverPoint.Value))
							{
								activeMouseoverMarker = mouseoverMarker;
							}
						}
					}

					y = (int) Math.Ceiling(rowRectangle.Bottom);
				}

				if (section != sections[0])
				{
					e.Graphics.DrawLine(new Pen(RenderColour, 1), sectionRectangle.Left, sectionRectangle.Top, sectionRectangle.Right, sectionRectangle.Top);
				}

				SectionRenderHook(e.Graphics, sectionContentsRectangle, section);

				y = (int) Math.Ceiling(sectionRectangle.Bottom);
			}

			// Render the timeline.
			RectangleF timelineRectangle = new RectangleF (timelineLeft, y, timelineWidth, timelineHeight);
			RectangleF bottomRectangle = new RectangleF (0, y, Width, timelineHeight);
			using (Brush brush = new SolidBrush (timeline.Colour))
			{
				e.Graphics.FillRectangle(brush, bottomRectangle);
			}

			RectangleF legendRectangle = new RectangleF (timelineRectangle.Left, timelineRectangle.Top + timelineTickHeight + timelineLabelHeight, timelineRectangle.Width, timelineRectangle.Height - timelineTickHeight - timelineLabelHeight);
			RenderText(e.Graphics, font, timeline.Legend, Color.White, legendRectangle, StringAlignment.Center, StringAlignment.Center);

			using (Pen pen = new Pen(RenderColour))
			{
				e.Graphics.DrawLine(pen, timelineRectangle.Left + timelineLeftMargin,
					timelineRectangle.Top + timelineTickHeight, timelineRectangle.Right - timelineRightMargin,
					timelineRectangle.Top + timelineTickHeight);
				for (float t = timeline.Start; t <= timeline.End; t = AdvancePausingAtEnd(t, timeline.Interval, timeline.End))
				{
					float x = MapFromRange(t, timeline.Start, timeline.End, timelineRectangle.Left + timelineLeftMargin,
						timelineRectangle.Right - timelineRightMargin);
					float width =
						MapFromRange(t + timeline.Interval, timeline.Start, timeline.End,
							timelineRectangle.Left + timelineLeftMargin, timelineRectangle.Right - timelineRightMargin) - x;

					e.Graphics.DrawLine(pen, x, timelineRectangle.Top, x, timelineRectangle.Top + timelineTickHeight);

					var timeString = FormatTimeString(t);

					var xPosition = (x - (width / 2));
					var labelSize = e.Graphics.MeasureString(timeString, font, (int) width);
					switch (TimelineLabelAlingment)
					{
						case TimelineLabelAlignment.Left:
							xPosition = xPosition - (labelSize.Width / 2);
							break;
						case TimelineLabelAlignment.Right:
							xPosition = xPosition + (labelSize.Width / 2);
							break;
					}

					RectangleF labelRectangle = new RectangleF(xPosition, timelineRectangle.Top + timelineTickHeight, width,
						timelineLegendHeight);
					RenderText(e.Graphics, font, timeString, RenderColour, labelRectangle, StringAlignment.Center,
						StringAlignment.Near);
				}
			}

			var mouseOverText = (activeMouseover != null) ? activeMouseover.Legend : (activeMouseoverMarker != null) ? activeMouseoverMarker.Legend : "";
			if (!string.IsNullOrEmpty(mouseOverText))
			{
				DrawMouseOver(e.Graphics, mouseoverPoint.Value.X, mouseoverPoint.Value.Y, mouseOverText);
			}
		}

		protected virtual void SectionRenderHook(Graphics graphics, RectangleF rectangle, Section section)
		{

		}

		protected void DrawMouseOver(Graphics graphics, int x, int y, string text)
		{
			SizeF size = graphics.MeasureString(text, font, 200);
			int margin = 6;
			size = new SizeF(size.Width + (margin * 2), size.Height + (margin * 2));
			Rectangle rectangle = new Rectangle(x - (int)(size.Width / 2), y + 20, (int)(0.5f + size.Width), (int)(0.5f + size.Height));
			rectangle = PushRectangleWithinBounds(rectangle, new Rectangle(0, 0, Width, Height));
			graphics.FillRectangle(Brushes.White, rectangle);
			graphics.DrawRectangle(Pens.Black, rectangle);
			graphics.DrawString(text, font, Brushes.Black, new Rectangle(rectangle.Left + margin, rectangle.Top + margin, rectangle.Width - margin, rectangle.Height - margin));
		}

		protected virtual string FormatTimeString(float t)
		{
			return CONVERT.ToStr(t);
		}

		protected bool IsFirstNonHeaderSection(Section section)
		{
			if (sections.Count == 0) return false;

			var firstSection = sections[0];
			if (firstSection.IsHeaderSection) firstSection = sections[1];

			return section == firstSection;
		}

		protected bool IsLastSection(Section section)
		{
			if (sections.Count == 0) return false;

			var lastSection = sections[sections.Count - 1];
			return section == lastSection;
		}

		static Rectangle PushRectangleWithinBounds (Rectangle a, Rectangle bounds)
		{
			int rightOverstep = a.Right - bounds.Right;
			if (rightOverstep > 0)
			{
				a = new Rectangle (a.Left - rightOverstep, a.Top, a.Width, a.Height);
			}

			int leftOverstep = bounds.Left - a.Left;
			if (leftOverstep > 0)
			{
				a = new Rectangle (a.Left + leftOverstep, a.Top, a.Width, a.Height);
			}

			int bottomOverstep = a.Bottom - bounds.Bottom;
			if (bottomOverstep > 0)
			{
				a = new Rectangle (a.Left, a.Top - bottomOverstep, a.Width, a.Height);
			}

			int topOverstep = bounds.Top - a.Top;
			if (topOverstep > 0)
			{
				a = new Rectangle (a.Left, a.Top + topOverstep, a.Width, a.Height);
			}

			return a;
		}

		protected float AdvancePausingAtEnd (float current, float increment, float end)
		{
			if (current < end)
			{
				return Math.Min(end, current + increment);
			}

			return end + increment;
		}

		public virtual FlickerFreePanel CreateLegendPanel (Rectangle bounds)
		{
			FlickerFreePanel panel = new FlickerFreePanel ();
			panel.Size = bounds.Size;

			Dictionary<Color, string> blockColourToLegend = new Dictionary<Color, string> ();

			Font font = ConstantSizeFont.NewFont(this.font.FontFamily.Name, 9);

			foreach (Section section in sections)
			{
				foreach (Block block in section.Blocks)
				{
					blockColourToLegend[block.Colour] = block.Legend;
				}

				foreach (Row row in section.Rows)
				{
					foreach (Block block in row.Blocks)
					{
						blockColourToLegend[block.Colour] = block.Legend;
					}

					foreach (Marker marker in row.Markers)
					{
						blockColourToLegend[marker.Colour] = marker.Legend;
					}
				}
			}

			int maxLegendWidth = 0;
			using (Graphics graphics = panel.CreateGraphics())
			{
				foreach (string legend in blockColourToLegend.Values)
				{
					maxLegendWidth = Math.Max(maxLegendWidth, (int) graphics.MeasureString(legend, font).Width);
				}
			}

			int lineHeight = 3 + (int) font.GetHeight();
			int blockGap = 10;
			int columnGap = 25;
			int rowGap = 5;
			int rowHeight = rowGap + lineHeight;

			int legendWidth = blockGap + maxLegendWidth;
			int columnWidth = lineHeight + legendWidth + columnGap;

			int x = 0;
			int y = 0;
			foreach (Color colour in blockColourToLegend.Keys)
			{
				Panel block = new Panel ();
				block.BackColor = colour;
				block.Location = new Point (x, y);
				block.Size = new Size (lineHeight, lineHeight);

				Label label = new Label ();
				label.Font = font;
				label.Text = blockColourToLegend[colour];
				label.Location = new Point (x + lineHeight + blockGap, y);
				label.Size = new Size (legendWidth, lineHeight);

				panel.Controls.Add(block);
				panel.Controls.Add(label);

				x += columnWidth;
				if ((x + columnWidth) > panel.Width)
				{
					x = 0;
					y += rowHeight;
				}
			}

			keyEnabled = true;
			Invalidate();

			return panel;
		}

		protected override void OnMouseMove (MouseEventArgs e)
		{
			base.OnMouseMove(e);

			mouseoverPoint = e.Location;
			Invalidate();
		}

		protected override void OnMouseLeave (EventArgs e)
		{
			base.OnMouseLeave(e);

			mouseoverPoint = null;
			Invalidate();
		}
	}

	public enum MarkerType
	{
		Square,
		Circle,
		Bar,
		Line
	}
}