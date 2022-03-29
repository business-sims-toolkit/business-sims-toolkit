using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using System.Xml;

using Charts;
using CommonGUI;

using CoreUtils;

using LibCore;

namespace ReportBuilder
{
    public class ESM_TimeChart : TimeChart
    {
        public ESM_TimeChart(XmlElement root) : base(root)
        {
        }
        
        protected override void RenderTimedBlock(Graphics graphics, Color colour, bool borderOnly, float startTime, float endTime, RectangleF fullRectangle, string legend, string smallLegend, string bottomLegend, Color textColour, Font font, Color borderColour, bool striped = false)
        {
            float left = MapFromRange(startTime, timeline.Start, timeline.End, fullRectangle.Left, fullRectangle.Right);
            float right = MapFromRange(endTime, timeline.Start, timeline.End, fullRectangle.Left, fullRectangle.Right);

            RectangleF rectangle = new RectangleF(left, (int)fullRectangle.Top, right - left, (int)fullRectangle.Height);

            if (borderOnly)
            {
                float thickness = 4;

                using (Pen pen = new Pen(colour, thickness))
                {
                    graphics.DrawRectangle(pen, (int)(rectangle.Left + (thickness / 2)), (int)(rectangle.Top + (thickness / 2)), (int)(rectangle.Width - thickness), (int)(rectangle.Height - thickness));
                }
            }
            else
            {
                // There's a bug in LinearGradientBrush that can render the edges wrongly: expand the brush rectangle to work around it.
                RectangleF gradientRectangle = new RectangleF(rectangle.Left - 1, rectangle.Top - 1, rectangle.Width + 2, rectangle.Height + 2);

                Color lightColour = Lerp(0.2f, colour, Color.White);
                Color darkColour = Lerp(0.2f, colour, Color.Black);

                using (Brush brush = new LinearGradientBrush(gradientRectangle, lightColour, darkColour, 90))
                {
                    graphics.FillRectangle(brush, rectangle);

                    if (striped)
                    {
                        RectangleF hatchedRectangle = new RectangleF(rectangle.Left,
                                                                     rectangle.Top,
                                                                     rectangle.Width,
                                                                     rectangle.Height);

                        using (Brush hatchedBrush = new HatchBrush(HatchStyle.OutlinedDiamond, Color.Crimson, Color.Black))
                        {
                            graphics.FillRectangle(hatchedBrush, hatchedRectangle);
                        }
                    }
                }
            }
        }
        public override FlickerFreePanel CreateLegendPanel(Rectangle bounds)
        {
            FlickerFreePanel panel = new FlickerFreePanel { Size = bounds.Size };

            Dictionary<Color, string> blockColourToLegend = new Dictionary<Color, string>();

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
                    maxLegendWidth = Math.Max(maxLegendWidth, (int)graphics.MeasureString(legend, font).Width);
                }
            }

            int lineHeight = 3 + (int)font.GetHeight();
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
                string value;
                blockColourToLegend.TryGetValue(colour, out value);

                Panel block = new Panel
                              {
                                  BackColor = colour,
                                  Location = new Point(x, y),
                                  Size = new Size(lineHeight, lineHeight)
                              };

                Label label = new Label
                              {
                                  Font = font,
                                  Text = blockColourToLegend[colour],
                                  Location = new Point(x + lineHeight + blockGap, y),
                                  Size = new Size(legendWidth, lineHeight)
                              };

                if (value == "Fine")
                {
                    block.Paint += fineBlock_Paint;
                }

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

        void fineBlock_Paint (object sender, PaintEventArgs e)
        {
            using (Brush hatchedBrush = new HatchBrush(HatchStyle.OutlinedDiamond, Color.Crimson, Color.Black))
            {
                e.Graphics.FillRectangle(hatchedBrush, 0, 0, 50, 50);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            int rows = 0;
            foreach (Section section in sections)
            {
                rows += section.Rows.Count;
            }

            if (sections.Any()) sectionHeaderWidth = sections.Max(x => x.HeaderWidth ?? sectionHeaderWidth);
            if (sections.Any() && sections.SelectMany(x => x.Rows).Any()) rowHeaderWidth = sections.SelectMany(x => x.Rows).Max(x => x.header_width);

            float sectionLeading = 40;

            float timelineHeight = 50;
            float timelineLeftMargin = 10;
            float timelineRightMargin = 10;
            float timelineLeft = timelineLeftMargin;
            float timelineRight = Width - TimelineRightMargin;
            float timelineWidth = timelineRight - timelineLeft;

            float rowHeight = (int)Math.Max(MinRowHeight, Math.Min(MaxRowHeight, (Height - timelineHeight - (sections.Count * sectionLeading * 2)) / Math.Max(1, rows)));

            float y = 0;

            MouseoverBlock activeMouseover = null;
            MouseoverMarker activeMouseoverMarker = null;

            foreach (Section section in sections)
            {
                if (section.HeaderWidth.HasValue)
                {
                    sectionHeaderWidth = section.HeaderWidth.Value * 2;
                }

                float sectionHeight = (rowHeight * section.Rows.Count) + (sectionLeading * 2);
                RectangleF sectionRectangle = new RectangleF(0, (int)y, Width, (int)sectionHeight);
                RectangleF sectionTitleRectangle = new RectangleF((Width-sectionHeaderWidth)/2, (int)y, sectionHeaderWidth, (int)sectionHeight);
                RectangleF sectionContentsRectangle = new RectangleF(timelineLeft + timelineLeftMargin, (int)(y + sectionLeading), timelineWidth - (timelineLeftMargin + timelineRightMargin), (int)sectionHeight);

                using (Brush sectionBrush = new SolidBrush(section.Colour))
                {
                    e.Graphics.FillRectangle(sectionBrush, sectionRectangle);
                }

                RenderText(e.Graphics, font, section.Legend, section.FontColour, sectionTitleRectangle, StringAlignment.Center, StringAlignment.Near);

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

                if (section != sections[0])
                {
                    RenderTimeLine(e.Graphics, y, timelineLeft, timelineWidth, timelineHeight, timelineLeftMargin, timelineRightMargin);
                }
                
                y += sectionLeading;
                foreach (Row row in section.Rows)
                {
                    if (row.header_width != -1)
                    {
                        rowHeaderWidth = row.header_width;
                    }

                    RectangleF rowRectangle = new RectangleF(timelineLeftMargin, (int)y, Width-timelineRightMargin, (int)rowHeight);
                    RectangleF rowTitleRectangle = new RectangleF(sectionHeaderWidth, (int)y, rowHeaderWidth, (int)rowHeight);
                    RectangleF rowContentsRectangle = new RectangleF(timelineLeft + timelineLeftMargin, (int)y, timelineWidth - (timelineLeftMargin + timelineRightMargin), (int)rowHeight);

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
                        e.Graphics.DrawLine(new Pen(RenderColour, 1), timelineLeftMargin + timelineRightMargin, rowRectangle.Top, Width - timelineRightMargin, rowRectangle.Top);
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

                    y = (int)Math.Ceiling(rowRectangle.Bottom);
                }

                SectionRenderHook(e.Graphics, sectionContentsRectangle, section);

                y = (int)Math.Ceiling(sectionRectangle.Bottom);
            }

            RenderTimeLine(e.Graphics, y, timelineLeft, timelineWidth, timelineHeight, timelineLeftMargin, timelineRightMargin);

            var mouseOverText = (activeMouseover != null) ? activeMouseover.Legend : (activeMouseoverMarker != null) ? activeMouseoverMarker.Legend : "";
            if (!string.IsNullOrEmpty(mouseOverText))
            {
                DrawMouseOver(e.Graphics, mouseoverPoint.Value.X, mouseoverPoint.Value.Y, mouseOverText);
            }
        }

        void RenderTimeLine(Graphics g, float y, float timelineLeft, float timelineWidth, float timelineHeight, float timelineLeftMargin, float timelineRightMargin)
        {
            float timelineTickHeight = 10;
            float timelineLabelHeight = 20;
            float timelineLegendHeight = 20;

            // Render the timeline.
            RectangleF timelineRectangle = new RectangleF(timelineLeft, y-50, timelineWidth, timelineHeight);

            RectangleF legendRectangle = new RectangleF(timelineRectangle.Left, timelineRectangle.Top + timelineLabelHeight, timelineRectangle.Width, timelineRectangle.Height - timelineTickHeight - timelineLabelHeight);
            RenderText(g, font, timeline.Legend, SkinningDefs.TheInstance.GetColorData("table_text_colour", Color.Black), legendRectangle, StringAlignment.Center, StringAlignment.Center);

            using (Pen pen = new Pen(RenderColour))
            {
                g.DrawLine(pen, timelineRectangle.Left + timelineLeftMargin,
                    timelineRectangle.Top + timelineTickHeight, timelineRectangle.Right - timelineRightMargin,
                    timelineRectangle.Top + timelineTickHeight);
                for (float t = timeline.Start; t <= timeline.End; t = AdvancePausingAtEnd(t, timeline.Interval, timeline.End))
                {
                    float x = MapFromRange(t, timeline.Start, timeline.End, timelineRectangle.Left + timelineLeftMargin,
                        timelineRectangle.Right - timelineRightMargin);
                    float width =
                        MapFromRange(t + timeline.Interval, timeline.Start, timeline.End,
                            timelineRectangle.Left + timelineLeftMargin, timelineRectangle.Right - timelineRightMargin) - x;

                    g.DrawLine(pen, x, timelineRectangle.Top, x, timelineRectangle.Top + timelineTickHeight);

                    var timeString = FormatTimeString(t);

                    var xPosition = (x - (width / 2));
                    var labelSize = g.MeasureString(timeString, font, (int)width);
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
                    RenderText(g, font, timeString, RenderColour, labelRectangle, StringAlignment.Center,
                        StringAlignment.Near);
                }
            }
        }
    }
}
