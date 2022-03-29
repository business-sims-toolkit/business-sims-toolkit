using System;
using System.Collections.Generic;
using System.Xml;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using Algorithms;
using LibCore;
using CoreUtils;
using Events;
using ResizingUi;

namespace Charts
{
	public class GroupedBarChart : SharedMouseEventControl, ICategoryCollector
	{
		class Group
		{
			public readonly string Name;
			public readonly List<Bar> Bars;
		    public readonly bool Stacked;

			public Group (GroupedBarChart barChart, XmlElement xml)
			{
				Name = BasicXmlDocument.GetStringAttribute(xml, "name");
                Stacked = BasicXmlDocument.GetBoolAttribute(xml, "stacked", false);
				Bars = new List<Bar>();
				foreach (XmlElement child in xml.ChildNodes)
				{
					if (child.Name == "bar")
					{
						Bars.Add(new Bar(barChart, child));
					}
				}
			}
		}
        
		string legend;
		readonly List<Category> categories;
		readonly VerticalAxis yAxis;
		readonly VerticalLabel yAxisLabel;
		readonly string verticalLegend;
		readonly List<Group> groups;
		readonly string horizontalLegend;

		double yAxisWidth;
		double xAxisHeight;
		double legendHeight;
		double legendX;
	    double legendY;

		readonly Font font;

		readonly bool useGradient;
		readonly Color yAxisTickColour;

		readonly bool areBarsStacked = false;
		readonly bool isLegendBold;

		readonly bool shouldDrawLinesForTicks;
		readonly bool showYAxisLine;

		readonly Color rowColourOne;
		readonly Color rowColourTwo;

		readonly int? maxLabelRows;

		public GroupedBarChart (XmlElement xml)
		{
			boundIdsDictionary = new Dictionary<string, Rectangle>();

			legend = BasicXmlDocument.GetStringAttribute(xml, "legend");
		    areBarsStacked = BasicXmlDocument.GetBoolAttribute(xml, "bars_stacked", false);
		    isLegendBold = BasicXmlDocument.GetBoolAttribute(xml, "bold_legend", false);
            shouldDrawLinesForTicks = BasicXmlDocument.GetBoolAttribute(xml, "draw_tick_lines", true);
            maxLabelRows = BasicXmlDocument.GetIntAttribute(xml, "max_label_rows");

            if (!shouldDrawLinesForTicks)
            {
                rowColourOne =
                    CONVERT.ParseComponentColor(BasicXmlDocument.GetStringAttribute(xml, "row_colour_one", "255,255,255"));
                rowColourTwo =
                    CONVERT.ParseComponentColor(BasicXmlDocument.GetStringAttribute(xml, "row_colour_two", "200,200,200"));
            }

            legendY = 0.0;

			categories = new List<Category>();
			groups = new List<Group>();

			foreach (XmlElement child in xml.ChildNodes)
			{
				switch (child.Name)
				{
					case "bar_categories":
						foreach (XmlElement category in child.ChildNodes)
						{
							categories.Add(new Category(category));
						}
						break;

					case "y_axis":
						yAxis = new VerticalAxis(child);
				        verticalLegend = child.GetStringAttribute("legend", "");
				        yAxisTickColour = BasicXmlDocument.GetColourAttribute(child, "tick_colour", Color.Black);
				        showYAxisLine = BasicXmlDocument.GetBoolAttribute(child, "show_y_axis_line", true);
				        yAxisLabel = new VerticalLabel
				                     {
                                         Text = verticalLegend,
                                         ForeColor = Color.Black,
                                         BackColor = Color.Transparent,
                                         Location = new Point(0, 0),
                                         Font = SkinningDefs.TheInstance.GetFont(9.5f),
                                         UseAlternatePaintMethod = SkinningDefs.TheInstance.GetBoolData("vertical_label_use_alternate_paint", false)
				                     };
				        Controls.Add(yAxisLabel);
						break;

					case "groups":
				        horizontalLegend = child.GetStringAttribute("legend", "");
				        useGradient = child.GetBooleanAttribute("use_gradient", true);
						foreach (XmlElement group in child.ChildNodes)
						{
							groups.Add(new Group(this, group));
						}
						break;
				}
			}

			font = ConstantSizeFont.NewFont(SkinningDefs.TheInstance.GetData("fontname"), SkinningDefs.TheInstance.GetFloatData("cpu_report_font_size", 9.5f));
		}

		public Category GetCategoryByName (string name)
		{
			foreach (var category in categories)
			{
				if (category.Name == name)
				{
					return category;
				}
			}

			return null;
		}

	    public bool ShowLegend;

		public double XAxisHeight
		{
			get
			{
				return xAxisHeight;
			}

			set
			{
				xAxisHeight = value;
				Invalidate();
			}
		}

		public double YAxisWidth
		{
			get
			{
				return yAxisWidth;
			}

			set
			{
				yAxisWidth = value;
			    yAxisLabel.Size = new Size((int)yAxisWidth, Height);
				Invalidate();
			}
		}

		public double LegendHeight
		{
			get
			{
				return legendHeight;
			}

			set
			{
				legendHeight = value;
				Invalidate();
			}
		}

		public double LegendX
		{
			get
			{
				return legendX;
			}

			set
			{
				legendX = value;
				Invalidate();
			}
		}

        public double LegendY
        {
            get
            {
                return legendY;
            }
            set
            {
                legendY = value;
                Invalidate();
            }
        }

	    double barPadding = 0;

        public double BarPadding
        {
            get
            {
                return barPadding;
            }
            set
            {
                if (value < 0)
                {
                    throw new Exception("Bar padding shouldn't be negative.");
                }

                barPadding = value;
            }
        }

	    double groupMargin = 10;

        public double GroupMargin
        {
            get
            {
                return groupMargin;
            }
            set
            {
                if (value < 0)
                {
                    throw new Exception("Group margin shouldn't be negative.");
                }

                groupMargin = value;
            }
        }

		readonly Dictionary<string, Rectangle> boundIdsDictionary;

		protected override void OnPaint (PaintEventArgs e)
		{
			base.OnPaint(e);

			double textPadding = 10;
			double axisTickLength = 5;
			double axisTickTextOffset = 5;

			var maxBarsPergroup = 0;
			double maxGroupLabelWidth = 0;
			foreach (var group in groups)
			{
				maxBarsPergroup = Math.Max(maxBarsPergroup, group.Bars.Count);

				var size = e.Graphics.MeasureString(group.Name, font);

				maxGroupLabelWidth = Math.Max(maxGroupLabelWidth, size.Width);
			}

			float chartAreaYOffset = 0;
			if (ShowLegend)
			{
				chartAreaYOffset = (float) legendHeight;
			}
			else
			{
				chartAreaYOffset = 10;
			}

			var chartArea = new RectangleF((float) yAxisLabel.Right + 5, chartAreaYOffset,
				(float) (ClientSize.Width - yAxisLabel.Width - 5),
				(float) (ClientSize.Height - xAxisHeight - chartAreaYOffset));
			yAxisLabel.Size = new Size((int) yAxisWidth, (int) chartArea.Height);

			
			boundIdsDictionary["yAxis"] = RectangleToScreen(yAxisLabel.Bounds);


			var groupWidth = chartArea.Width / (double) groups.Count;
			var barWidth = (groupWidth - (2 * groupMargin) - ((maxBarsPergroup - 1) * barPadding)) /
			                  (double) maxBarsPergroup;

			if (areBarsStacked)
			{
				barWidth = groupWidth * 0.6;
			}

			var textRows = maxLabelRows ?? (int) Math.Ceiling(groupWidth / (maxGroupLabelWidth + textPadding));

			// Y axis.
			if (yAxis.Visible)
			{
				// Y axis ticks (and the X axis).
				using (Brush penBrush = new SolidBrush(yAxisTickColour))
				{
					using (var pen = new Pen(penBrush))
					{
						if (showYAxisLine)
						{
							e.Graphics.DrawLine(pen, chartArea.Left, chartArea.Bottom, chartArea.Left, chartArea.Top);
						}

						if (shouldDrawLinesForTicks)
						{
							for (var y = yAxis.Min;
								y <= yAxis.Max;
								y = BarChartUtils.CheckedAdvance(y, yAxis.Max, yAxis.TickInterval))
							{
								var screenY = (int) (chartArea.Bottom -
								                     ((y - yAxis.Min) * chartArea.Height / (yAxis.Max - yAxis.Min)));

								e.Graphics.DrawLine(pen, (int) (chartArea.Left - axisTickLength), screenY,
									chartArea.Right, screenY);
							}
						}
						else
						{
							// Just the x-axis.
							e.Graphics.DrawLine(pen, (int) chartArea.Left, chartArea.Bottom, chartArea.Right,
								chartArea.Bottom);
						}
					}
				}

				if (! shouldDrawLinesForTicks)
				{
					var rowHeight = yAxis.TickInterval * chartArea.Height / (yAxis.Max - yAxis.Min);
					var rowCount = 0;

					Brush [] brushes =
					{
						new SolidBrush(rowColourOne),
						new SolidBrush(rowColourTwo)
					};

					var isFirst = true;
					for (var y = BarChartUtils.CheckedAdvance(yAxis.Min, yAxis.Max, yAxis.TickInterval);
						y <= yAxis.Max;
						y = BarChartUtils.CheckedAdvance(y, yAxis.Max, yAxis.TickInterval))
					{
						var screenY = (int) (chartArea.Bottom -
						                     ((y - yAxis.Min) * chartArea.Height / (yAxis.Max - yAxis.Min)));
						var screenX = (int) (chartArea.Left + 1);

						var adjust = isFirst ? -1 : 0;

						var rect = new Rectangle(screenX, screenY,
							(int) (chartArea.Right - screenX), (int) rowHeight + adjust);

						e.Graphics.FillRectangle(brushes[rowCount % 2], rect);
						rowCount++;

						isFirst = false;
					}
				}

				// Y axis numbers.
				for (var y = yAxis.Min;
					y <= yAxis.Max;
					y = BarChartUtils.CheckedAdvance(y, yAxis.Max, yAxis.NumberInterval))
				{
					var screenY =
						(int) (chartArea.Bottom - ((y - yAxis.Min) * chartArea.Height / (yAxis.Max - yAxis.Min)));
					double zoneHeight = 100;

					var labelArea = new RectangleF(0, (float) (screenY - zoneHeight),
						(float) (chartArea.Left - axisTickTextOffset), (float) (2 * zoneHeight));
					var format = new StringFormat();
					format.Alignment = StringAlignment.Far;
					format.LineAlignment = StringAlignment.Center;

					e.Graphics.DrawString(CONVERT.ToStr(y), font, Brushes.Black, labelArea, format);
				}
			}
			

		// Bars and X axis ticks.
			var labelRow = 0;
			for (var i = 0; i < groups.Count; i++)
			{
				var group = groups[i];

				var groupBaseX = yAxisWidth + (i * groupWidth);
                
			    var previousHeight = 0;
				for (var j = 0; j < group.Bars.Count; j++)
				{
					var bar = group.Bars[j];

                    var barX = (int)(groupBaseX + groupMargin + (j * (barWidth + barPadding)));
                    var barTop = (int)(chartArea.Bottom - ((bar.Height - yAxis.Min) * chartArea.Height / (yAxis.Max - yAxis.Min)));

                    var barHeight = (int)(chartArea.Bottom - barTop);

                    if (group.Stacked)
                    {
                        var commonX = (int)((groupWidth - barWidth) / 2.0);
                        barX = (int)(groupBaseX) + commonX;

                        barTop = (int)chartArea.Bottom - previousHeight - barHeight;
                        previousHeight = barHeight;
                    }

                    var barOutlineBounds = new Rectangle((int)barX, (int)barTop, (int)barWidth, barHeight);

					//boundIdsDictionary[$"group_{group.Name}_bar_{j + 1}"] = RectangleToScreen(barOutlineBounds);
                    
                    if (useGradient)
                    {
                        var barFillBounds = new Rectangle(
                        (int)(barOutlineBounds.Left + bar.Category.BorderInset), //x
                        (int)(barOutlineBounds.Top + bar.Category.BorderInset), //y
                        (int)(barOutlineBounds.Width - (2 * bar.Category.BorderInset)) + 1, //w
                        (int)(barOutlineBounds.Height - (2 * bar.Category.BorderInset)) + 1); //h

                        var tint = Color.FromArgb((bar.Category.Colour.R + 255) / 2, (bar.Category.Colour.G + 255) / 2, (bar.Category.Colour.B + 255) / 2);

                        e.Graphics.FillRectangle(Brushes.White, barOutlineBounds);

                        using (var pen = new Pen(bar.Category.BorderColour, (float)bar.Category.BorderThickness))
                        {
                            e.Graphics.DrawRectangle(pen, barOutlineBounds);
                        }

                        using (Brush brush = new LinearGradientBrush(barFillBounds, bar.Category.Colour, tint, -90))
                        {
                            e.Graphics.FillRectangle(brush, barFillBounds);
                        }
                    }
                    else
                    {
                        using (Brush brush = new SolidBrush(bar.Category.Colour))
                        {
                            e.Graphics.FillRectangle(brush, barOutlineBounds);
                        }
                    }                    

                    if (bar.ShouldDisplayText)
                    {
                        var text = "";

                        if (!string.IsNullOrEmpty(bar.DisplayText))
                        {
                            text = bar.DisplayText;
                        }
                        else
                        {
                            text = ((int)bar.Height).ToString();
                        }

                        var textSize = e.Graphics.MeasureString(text, font);
                        var y = 0;
                        if (barHeight == 0)
                        {
                            y = barTop - (int) textSize.Height;
                        }
                        else
                        {
                            y = barTop + (barHeight / 2) - (int) (textSize.Height / 2);
                        }
                        var textRect = new RectangleF(barOutlineBounds.Left, y,
                            barOutlineBounds.Width, textSize.Height);

                        using (Brush brush = new SolidBrush(Color.Black))
                        {
                            var textFormat = new StringFormat();
                            textFormat.Alignment = StringAlignment.Center;
                            textFormat.LineAlignment = StringAlignment.Center;
                            
                            e.Graphics.DrawString(text, font, brush, textRect, textFormat);
                        }
                    }
				}

				

				if (shouldDrawLinesForTicks)
                {
                    e.Graphics.DrawLine(Pens.Black, (int)groupBaseX, (int)(chartArea.Bottom - axisTickLength), (int)groupBaseX, chartArea.Bottom);
                }

				var rowHeight = xAxisHeight / 2;
				var labelArea = new RectangleF((float) groupBaseX, (float) (chartArea.Bottom), (float) groupWidth, (float) rowHeight);
				var format = new StringFormat();
				format.Alignment = StringAlignment.Center;
				format.LineAlignment = StringAlignment.Center;

				boundIdsDictionary[$"group_{group.Name}"] = RectangleToScreen(labelArea.ToRectangle());
				
				e.Graphics.DrawString(group.Name, font, Brushes.Black, labelArea, format);

				labelRow++;
				if (labelRow >= textRows)
				{
					labelRow = 0;
				}
			}

			boundIdsDictionary["chart_area"] = RectangleToScreen(chartArea.ToRectangle());

			if (!string.IsNullOrEmpty(horizontalLegend))
            {
                var textSize = e.Graphics.MeasureString(horizontalLegend, font);
                var horizontalLegendRect = new RectangleF (new Point ((int) chartArea.Width / 2, (int) (chartArea.Bottom + (xAxisHeight * textRows / (1 + textRows)))), textSize);

	            boundIdsDictionary["horizontal_legend"] = RectangleToScreen(horizontalLegendRect.ToRectangle());
	            var format = new StringFormat();
                format.Alignment = StringAlignment.Center;
                format.LineAlignment = StringAlignment.Center;
                e.Graphics.DrawString(horizontalLegend, font, Brushes.Black, horizontalLegendRect, format);
            }

			// Legend.
            if (ShowLegend)
			{
				var x = legendX;
			    var y = LegendY;
				var maxLegendSize = new SizeF(0, 0);
				foreach (var category in categories)
				{
					var legendSize = e.Graphics.MeasureString(category.Name, font);
					maxLegendSize = new SizeF(Math.Max(maxLegendSize.Width, legendSize.Width),
											   Math.Max(maxLegendSize.Height, legendSize.Height));
				}

				// TODO (GDC) possible LINQ replacement for the above
				//var categorySizes = categories.Select(c => e.Graphics.MeasureString(c.Name, font)).ToList();

				//maxLegendSize = new SizeF(
				//	categorySizes.Max(s => s.Width),
				//	categorySizes.Max(s => s.Height)
				//);

                // MeasureString always seems to ignore drooping characters.
			    maxLegendSize.Height += 5;
				double rectangleSize = maxLegendSize.Height;
				double gap = 5;
				var legendBlockSize = new SizeF((float) (rectangleSize + (2 * gap) + maxLegendSize.Width), maxLegendSize.Height);

				var legendRight = legendX;

				foreach (var category in categories)
				{
					var colourOutlineBounds = new RectangleF((float) x, (float) y, (float) rectangleSize, (float) rectangleSize);
					
                    if (useGradient)
                    {
                        var colourFillBounds = new Rectangle((int)(colourOutlineBounds.Left + category.BorderInset), (int)(colourOutlineBounds.Top + category.BorderInset), (int)((int)rectangleSize - (2 * category.BorderInset)) + 1, (int)((int)rectangleSize - (2 * category.BorderInset)) + 1);

                        e.Graphics.FillRectangle(Brushes.White, colourOutlineBounds);

                        using (var pen = new Pen(category.BorderColour, (float)category.BorderThickness))
                        {
                            e.Graphics.DrawRectangle(pen, colourOutlineBounds.Left, colourOutlineBounds.Top, 
                                colourOutlineBounds.Width, colourOutlineBounds.Height);
                            
                        }

                        using (Brush brush = new SolidBrush(category.Colour))
                        {
                            e.Graphics.FillRectangle(brush, colourFillBounds);
                        }
                    }
                    else
                    {
                        using (Brush brush = new SolidBrush(category.Colour))
                        {
                            e.Graphics.FillRectangle(brush, colourOutlineBounds);
                        }
                        
                    }
                    

					var textBounds = new Rectangle((int) (colourOutlineBounds.Right + gap), (int) y, (int) (legendBlockSize.Width - (colourOutlineBounds.Right + gap - colourOutlineBounds.Left)), (int) legendBlockSize.Height);
					var format = new StringFormat();
					format.Alignment = StringAlignment.Near;
					format.LineAlignment = StringAlignment.Center;

                    if (isLegendBold)
                    {
                        var boldFont = SkinningDefs.TheInstance.GetFont(9.5f, FontStyle.Bold);

                        e.Graphics.DrawString(category.Name, boldFont, Brushes.Black, textBounds, format);
                    }
                    else
                    {
                        e.Graphics.DrawString(category.Name, font, Brushes.Black, textBounds, format);
                    }					

					x += legendBlockSize.Width;
					legendRight = x;
					if ((x + legendBlockSize.Width) >= Width)
					{
						x = legendX;
						legendRight = Width;
						y += legendBlockSize.Height;
					}
				}

				boundIdsDictionary["legend"] = new Rectangle((int)legendX, (int)LegendY, (int)(legendRight - legendX), (int)((y + legendBlockSize.Height) - LegendY));
			}
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);
			Invalidate();
		}

		public override List<KeyValuePair<string, Rectangle>> BoundIdsToRectangles => boundIdsDictionary.ToList();
		public override void ReceiveMouseEvent (SharedMouseEventArgs args)
	    {
	        throw new NotImplementedException();
	    }
	}
}