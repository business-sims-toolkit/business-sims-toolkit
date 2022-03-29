using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Xml;

using LibCore;
using CommonGUI;

namespace Charts
{
	public class StackedBarChart : FlickerFreePanel, ICategoryCollector
	{
		protected List<Category> categories;
		protected HorizontalAxis xAxis;
		protected VerticalAxis yAxis;
		protected bool showKey;
		protected bool useGradient;

		protected Point? mouseHoverLocation;

		protected Dictionary<int, List<Bar>> xToBars;
		protected Dictionary<int, MouseoverAnnotation> xToAnnotation;
		protected Dictionary<int, string> xToTopLabel;

		protected Font legendFont;
		protected Font axisFont;

		protected int yAxisWidth;
		protected int xAxisHeight;
		protected int legendX;
		protected int legendY;
		protected int leftMargin;
		protected int rightMargin;
		protected int topMargin;
		protected int bottomMargin;
		protected int yAxisLegendMargin;

		protected string legend;
		protected bool showMouseAnnotations = true;
		protected bool showNumbersOnBar = false;

		protected List<int> dividerXes;
		protected Dictionary<int, string> xToLabel;

		public int LeftMargin
		{
			get
			{
				return leftMargin;
			}

			set
			{
				leftMargin = value;
				Invalidate();
			}
		}

		public int RightMargin
		{
			get
			{
				return rightMargin;
			}

			set
			{
				rightMargin = value;
				Invalidate();
			}
		}

		public int TopMargin
		{
			get
			{
				return topMargin;
			}

			set
			{
				topMargin = value;
				Invalidate();
			}
		}

		public int BottomMargin
		{
			get
			{
				return bottomMargin;
			}

			set
			{
				bottomMargin = value;
				Invalidate();
			}
		}

		public int XAxisHeight
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

		public int YAxisWidth
		{
			get
			{
				return yAxisWidth;
			}

			set
			{
				yAxisWidth = value;
				Invalidate();
			}
		}

		public int YAxisLegendMargin
		{
			get
			{
				return yAxisLegendMargin;
			}

			set
			{
				yAxisLegendMargin = value;
				Invalidate();
			}
		}

		public int LegendX
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

		public int LegendY
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

		Point keyPosition;
		public Point KeyPosition
		{
			get
			{
				return keyPosition;
			}

			set
			{
				keyPosition = value;
				Invalidate();
			}
		}

		public bool ShowNumbersOnBar
		{
			get
			{
				return showNumbersOnBar;
			}

			set
			{
				showNumbersOnBar = value;
				Invalidate();
			}
		}

		public StackedBarChart (XmlElement xml)
		{
			categories = new List<Category> ();
			xToBars = new Dictionary<int, List<Bar>> ();
			xToAnnotation = new Dictionary<int, MouseoverAnnotation> ();
			xToTopLabel = new Dictionary<int, string> ();

			dividerXes = new List<int> ();
			xToLabel = new Dictionary<int, string> ();

			showKey = BasicXmlDocument.GetBoolAttribute(xml, "show_key", true);
			useGradient = BasicXmlDocument.GetBoolAttribute(xml, "use_gradient", true);
			legend = BasicXmlDocument.GetStringAttribute(xml, "legend");

			foreach (XmlElement child in xml.ChildNodes)
			{
				switch (child.Name)
				{
					case "bar_categories":
						foreach (XmlElement category in child.ChildNodes)
						{
							categories.Add(new Category (category));
						}
						break;

					case "x_axis":
						xAxis = new HorizontalAxis (child);
						break;

					case "y_axis":
						yAxis = new VerticalAxis (child);
						break;

					case "divider":
						dividerXes.Add(BasicXmlDocument.GetIntAttribute(child, "x", 0));
						break;

					case "stacks":
						foreach (XmlElement stack in child.ChildNodes)
						{
							int x = BasicXmlDocument.GetIntAttribute(stack, "x", 0);

							if (! xToBars.ContainsKey(x))
							{
								xToBars.Add(x, new List<Bar> ());
							}

							xToTopLabel.Add(x, BasicXmlDocument.GetStringAttribute(stack, "top_label", ""));

							foreach (XmlElement stackable in stack.ChildNodes)
							{
								switch (stackable.Name)
								{
									case "bar":
										xToBars[x].Add(new Bar (this, stackable));
										break;

									case "line":
										xToBars[x].Add(new Line (this, stackable));
										break;

									case "annotation":
										xToAnnotation[x] = new MouseoverAnnotation (stackable);
										break;
								}
							}
						}
						break;
				}
			}

			LegendFontSize = 9.5f;
			AxisFontSize = 8;
		}

		float legendFontSize;
		public float LegendFontSize
		{
			get
			{
				return legendFontSize;
			}

			set
			{
				legendFontSize = value;
				legendFont = CoreUtils.SkinningDefs.TheInstance.GetFont(legendFontSize);
			}
		}

		float axisFontSize;
		public float AxisFontSize
		{
			get
			{
				return axisFontSize;
			}

			set
			{
				axisFontSize = value;
				axisFont = CoreUtils.SkinningDefs.TheInstance.GetFont(axisFontSize);
			}
		}

		public void setShowMouseNotes(bool show_mouse_notes)
		{
			showMouseAnnotations = show_mouse_notes;
		}

		public void setShowNumbersOnBar(bool showNumbersOnBar)
		{
			ShowNumbersOnBar = showNumbersOnBar;
		}

		public Category GetCategoryByName (string name)
		{
			foreach (Category category in categories)
			{
				if (category.Name == name)
				{
					return category;
				}
			}

			return null;
		}

		protected override void OnPaint (PaintEventArgs e)
		{
			base.OnPaint(e);

			double axisTickLength = 5;
			double axisTickTextOffset = 5;

			int barMargin = 2;

			double maxStackLabelWidth = 0;
			foreach (int x in xToBars.Keys)
			{
				SizeF size = e.Graphics.MeasureString(CONVERT.ToStr(x), axisFont);
				maxStackLabelWidth = Math.Max(maxStackLabelWidth, size.Width);
			}

			RectangleF chartArea = new RectangleF (leftMargin + (float) yAxisWidth, topMargin, (float) (ClientSize.Width - yAxisWidth - rightMargin - leftMargin), (float) (ClientSize.Height - xAxisHeight - topMargin - bottomMargin));

			double stackWidth = chartArea.Width / (double) (xAxis.Max + 1 - xAxis.Min);

			int stringHeight = legendFont.Height;

			// Y axis.
			if (yAxis.Visible)
			{
				e.Graphics.DrawLine(Pens.Black, chartArea.Left, chartArea.Bottom, chartArea.Left, chartArea.Top);

				// Y axis ticks (and the X axis).
				using (Pen pen = new Pen(yAxis.TickColour))
				{
					for (double y = yAxis.Min; y <= yAxis.Max; y = BarChartUtils.CheckedAdvance(y, yAxis.Max, yAxis.TickInterval))
					{
						int screenY = (int) (chartArea.Bottom - ((y - yAxis.Min) * chartArea.Height / (yAxis.Max - yAxis.Min)));

						Pen usePen = pen;
						if (y == yAxis.Min)
						{
							usePen = Pens.Black;
						}

						e.Graphics.DrawLine(usePen, (int) (chartArea.Left - axisTickLength), screenY, chartArea.Right, screenY);
					}
				}

				// Y axis numbers.
				for (double y = yAxis.Min; y <= yAxis.Max; y = BarChartUtils.CheckedAdvance(y, yAxis.Max, yAxis.NumberInterval))
				{
					int screenY = (int) (chartArea.Bottom - ((y - yAxis.Min) * chartArea.Height / (yAxis.Max - yAxis.Min)));
					double zoneHeight = 100;

					RectangleF labelArea = new RectangleF(0, (float) (screenY - zoneHeight), (float) (chartArea.Left - axisTickTextOffset), (float) (2 * zoneHeight));
					StringFormat format = new StringFormat();
					format.Alignment = StringAlignment.Far;
					format.LineAlignment = StringAlignment.Center;

					e.Graphics.DrawString(CONVERT.ToStr(y), axisFont, Brushes.Black, labelArea, format);
				}

				// Y axis legend.
				{
					RectangleF legendRectangle = new RectangleF (0, bottomMargin, (float) yAxisWidth - yAxisLegendMargin, Height - bottomMargin - topMargin);
					SizeF legendSize = e.Graphics.MeasureString(yAxis.Legend, legendFont);

					Matrix oldMatrix = e.Graphics.Transform;
					e.Graphics.TranslateTransform((legendRectangle.Left + legendRectangle.Right - legendSize.Height) / 2,
												  (legendRectangle.Top + legendRectangle.Bottom + legendSize.Width) / 2);
					e.Graphics.RotateTransform(-90);
					e.Graphics.DrawString(yAxis.Legend, legendFont, Brushes.Black, 0, 0);
					e.Graphics.Transform = oldMatrix;
				}
			}

			//used in the displaying the line value for the last known
			int highestBarX = -1;
			{
				foreach (int x_value in xToBars.Keys)
				{
					if (x_value > highestBarX)
					{
						highestBarX = x_value;
					}
				}
			}

			// Bars and X axis labels.
			Dictionary<string, Line> categoryNameTolastKnownLine = new Dictionary<string, Line> ();
			for (int x = xAxis.Min; x <= xAxis.Max; x++)
			{
				double stackX = chartArea.Left + ((x - xAxis.Min) * stackWidth);

				// Annotation?
				if (showMouseAnnotations)
				{
					if (mouseHoverLocation.HasValue && xToAnnotation.ContainsKey(x))
					{
						if ((mouseHoverLocation.Value.X >= stackX) && (mouseHoverLocation.Value.X < (stackX + stackWidth)))
						{
							int width = Math.Max(250, (int)stackWidth);
							SizeF size = e.Graphics.MeasureString(xToAnnotation[x].Text, axisFont, width);
							RectangleF popup = new RectangleF((float)stackX, chartArea.Top, size.Width, size.Height);
							e.Graphics.FillRectangle(Brushes.White, popup);
							e.Graphics.DrawRectangle(Pens.Black, new Rectangle ((int) popup.Left, (int) popup.Top, (int) popup.Width, (int) popup.Height));

							e.Graphics.DrawString(xToAnnotation[x].Text, axisFont, Brushes.Black, popup);
						}
					}
				}

				double heightAccumulator = 0;
				if (xToBars.ContainsKey(x))
				{
					List<Bar> stack = xToBars[x];

					for (int j = 0; j < stack.Count; j++)
					{
						int barX = (int) stackX;
						Bar bar = stack[j];

						if (bar is Line)
						{
							Line newValue = (Line) bar;

							Line oldValue = null;
							if (categoryNameTolastKnownLine.ContainsKey(newValue.Category.Name))
							{
								oldValue = categoryNameTolastKnownLine[newValue.Category.Name];
							}

							int newY = (int) (chartArea.Bottom - ((newValue.Y - yAxis.Min) * chartArea.Height / (yAxis.Max - yAxis.Min)));
							using (Pen pen = new Pen (newValue.Category.Colour, 2))
							{
								if (oldValue != null)
								{
									int oldY = (int) (chartArea.Bottom - ((oldValue.Y - yAxis.Min) * chartArea.Height / (yAxis.Max - yAxis.Min)));
									e.Graphics.DrawLine(pen, barX, oldY, barX, newY);
								}

								e.Graphics.DrawLine(pen, barX - 1, newY, (int) (barX + stackWidth + 1), newY);

								if (newValue.show_above_line_at_end == true)
								{
									PointF pf = new PointF(barX + (int)stackWidth + 1 , newY);
									string datastr = CONVERT.ToStr(newValue.Y);

									SizeF textsize = e.Graphics.MeasureString(datastr, this.Font);
									pf.X = pf.X - (textsize.Width + 10);
									pf.Y = pf.Y - ((textsize.Height * 3) / 2);

									// only draw the last one 
									if (highestBarX == x)
									{
										e.Graphics.DrawString(CONVERT.ToStr(newValue.Y), this.Font, Brushes.Black, pf);
									}
								}
							}

							categoryNameTolastKnownLine[newValue.Category.Name] = newValue;
						}
						else
						{
							if (bar.Height > 0)
							{
								int barBottom = (int)(chartArea.Bottom - ((heightAccumulator - yAxis.Min) * chartArea.Height / (yAxis.Max - yAxis.Min)));
								int barTop = (int)(chartArea.Bottom - ((heightAccumulator + bar.Height - yAxis.Min) * chartArea.Height / (yAxis.Max - yAxis.Min)));
								heightAccumulator += bar.Height;

								Rectangle barOutlineBounds = new Rectangle((int)barX + barMargin, (int)barTop, (int)stackWidth - (2 * barMargin), (int)(barBottom - barTop));
								Rectangle barFillBounds = new Rectangle((int)(barOutlineBounds.Left + bar.Category.BorderInset), (int)(barOutlineBounds.Top + bar.Category.BorderInset), (int)(barOutlineBounds.Width - (2 * bar.Category.BorderInset)) + 1, (int)(barOutlineBounds.Height - (2 * bar.Category.BorderInset)) + 1);

								Color tint = Color.FromArgb((bar.Category.Colour.R + 255) / 2, (bar.Category.Colour.G + 255) / 2, (bar.Category.Colour.B + 255) / 2);

								e.Graphics.FillRectangle(Brushes.White, barOutlineBounds);

								using (Pen pen = new Pen(bar.Category.BorderColour, (float)bar.Category.BorderThickness))
								{
									e.Graphics.DrawRectangle(pen, barOutlineBounds);
								}

								if (useGradient)
								{
									using (Brush brush = new LinearGradientBrush(barFillBounds, bar.Category.Colour, tint, -90))
									{
										e.Graphics.FillRectangle(brush, barFillBounds);
									}
								}
								else
								{
									using (Brush brush = new SolidBrush(bar.Category.Colour))
									{
										e.Graphics.FillRectangle(brush, barFillBounds);
									}
								}

								if (ShowNumbersOnBar)
								{
									StringFormat format = new StringFormat();
									format.Alignment = StringAlignment.Center;
									format.LineAlignment = StringAlignment.Center;
									//e.Graphics.DrawString(bar.Legend, legendFont, Brushes.Black, barFillBounds, format);
									using (Brush textbrush = new SolidBrush(bar.Category.TextColour))
									{
										e.Graphics.DrawString(bar.Legend, legendFont, textbrush, barFillBounds, format);
									}
								}
							}
						}
					}
				}

				if (ShowNumbersOnBar)
				{
					if (xToTopLabel.ContainsKey(x))
					{
						string topLabel = xToTopLabel[x];
						if (!string.IsNullOrEmpty(topLabel))
						{
							int labelX = (int)(stackX + (stackWidth / 2));
							int labelBottom = (int)(chartArea.Bottom - ((heightAccumulator - yAxis.Min) * chartArea.Height / (yAxis.Max - yAxis.Min)));

							StringFormat format = new StringFormat();
							format.Alignment = StringAlignment.Center;
							format.LineAlignment = StringAlignment.Far;
							e.Graphics.DrawString(topLabel, legendFont, Brushes.Black, labelX, labelBottom, format);
						}
					}
				}

				if (xAxis.Visible)
				{
					RectangleF labelArea = new RectangleF ((float) stackX, (float) chartArea.Bottom, (float) stackWidth, (float) stringHeight);
					StringFormat format = new StringFormat ();
					format.Alignment = xAxis.LabelAlignment;
					format.LineAlignment = StringAlignment.Center;
					e.Graphics.DrawString(CONVERT.ToStr(x), axisFont, Brushes.Black, labelArea, format);
				}
			}

			// X axis legend.
			if (xAxis.Visible)
			{
				RectangleF legendRectangle = new RectangleF (chartArea.Left, chartArea.Bottom + stringHeight, chartArea.Width, stringHeight);
				StringFormat format = new StringFormat ();
				format.Alignment = StringAlignment.Center;
				format.LineAlignment = StringAlignment.Center;
				e.Graphics.DrawString(xAxis.Legend, legendFont, Brushes.Black, legendRectangle, format);
			}

			// Dividers.
			foreach (int x in dividerXes)
			{
				Rectangle dividerRectangle = new Rectangle ((int) (chartArea.Left + ((x - xAxis.Min) * stackWidth)), (int) chartArea.Top, (int) stackWidth, (int) chartArea.Height);
				e.Graphics.DrawRectangle(Pens.Black, dividerRectangle);
			}

			// Legend.
			e.Graphics.DrawString(legend, legendFont, Brushes.Black, (float) legendX, (float) legendY);

			// Key.
			if (showKey)
			{
				double x = KeyPosition.X;
				double y = KeyPosition.Y;
				SizeF maxLegendSize = new SizeF(0, 0);
				foreach (Category category in categories)
				{
					if (category.ShowInKey)
					{
						SizeF legendSize = e.Graphics.MeasureString(category.Legend, legendFont);
						maxLegendSize = new SizeF (Math.Max(maxLegendSize.Width, legendSize.Width),
												   Math.Max(maxLegendSize.Height, legendSize.Height));
					}
				}

				double rectangleSize = maxLegendSize.Height;
				double gap = 5;
				SizeF legendBlockSize = new SizeF ((float) (rectangleSize + (2 * gap) + maxLegendSize.Width), maxLegendSize.Height);

				foreach (Category category in categories)
				{
					if (category.ShowInKey)
					{
						Rectangle colourOutlineBounds = new Rectangle ((int) x, (int) y,
																	   (int) rectangleSize, (int) rectangleSize);
						Rectangle colourFillBounds = new Rectangle ((int) (colourOutlineBounds.Left + category.BorderInset),
																	(int) (colourOutlineBounds.Top + category.BorderInset),
																	(int) ((int) rectangleSize - (2 * category.BorderInset)) + 1,
																	(int) ((int) rectangleSize - (2 * category.BorderInset)) + 1);

						e.Graphics.FillRectangle(Brushes.White, colourOutlineBounds);

						using (Pen pen = new Pen (category.BorderColour, (float) category.BorderThickness))
						{
							e.Graphics.DrawRectangle(pen, colourOutlineBounds);
						}

						using (Brush brush = new SolidBrush (category.Colour))
						{
							e.Graphics.FillRectangle(brush, colourFillBounds);
						}

						Rectangle textBounds = new Rectangle ((int) (colourOutlineBounds.Right + gap),
															  (int) y, (int) (legendBlockSize.Width - (colourOutlineBounds.Right + gap - colourOutlineBounds.Left)), (int) legendBlockSize.Height);
						StringFormat format = new StringFormat ();
						format.Alignment = StringAlignment.Near;
						format.LineAlignment = StringAlignment.Center;

						using (Brush textbrush = new SolidBrush(category.TextColour))
						{
							e.Graphics.DrawString(category.Legend, legendFont, textbrush, textBounds, format);
						}

						x += legendBlockSize.Width;
						if ((x + legendBlockSize.Width) >= Width)
						{
							x = keyPosition.X;
							y += legendBlockSize.Height + gap;
						}
					}
				}
			}
		}

		protected override void OnMouseLeave (EventArgs e)
		{
			base.OnMouseEnter(e);
			mouseHoverLocation = null;
			Invalidate();
		}

		protected override void OnMouseMove (MouseEventArgs e)
		{
			base.OnMouseMove(e);
			mouseHoverLocation = e.Location;
			Invalidate();
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);
			Invalidate();
		}
	}
}