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
	public class StackBarChartArea : FlickerFreePanel, ICategoryCollector
	{
		List<Category> categories;

		protected HorizontalAxis xAxis;
		protected VerticalAxis yAxis;

		Point? mouseHoverLocation;

		protected Font legendFont;
		protected Font axisFont;

		protected Dictionary<int, List<Bar>> xToBars;
		protected Dictionary<int, MouseoverAnnotation> xToAnnotation;
		protected Dictionary<int, string> xToTopLabel;

		protected int yAxisWidth;
		protected int xAxisHeight;
		/*
		protected int leftMargin;
		protected int rightMargin;
		protected int topMargin;
		protected int bottomMargin;
		*/
		protected int yAxisLegendMargin;

		protected string legend;
		protected bool showMouseAnnotations = true;

		protected List<int> dividerXes;
		
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

		string dataCenterName = "";

		protected bool showNumbersOnBar = true;
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
		public void SetShowNumbersOnBar(bool showNumbersOnBar)
		{
			ShowNumbersOnBar = showNumbersOnBar;
		}

		public StackBarChartArea(XmlElement xml, List<Category> _categories, string _dataCenterName)
		{
			categories = _categories;
			dataCenterName = _dataCenterName;

			xToBars = new Dictionary<int, List<Bar>>();
			xToAnnotation = new Dictionary<int, MouseoverAnnotation>();
			xToTopLabel = new Dictionary<int, string>();

			dividerXes = new List<int>();

			legend = BasicXmlDocument.GetStringAttribute(xml, "legend");

			foreach (XmlElement child in xml.ChildNodes)
			{
				switch (child.Name)
				{
					case "x_axis":
						xAxis = new HorizontalAxis(child);
						break;

					case "y_axis":
						yAxis = new VerticalAxis(child);
						break;

					case "divider":
						dividerXes.Add(BasicXmlDocument.GetIntAttribute(child, "x", 0));
						break;

					case "stacks":
						foreach (XmlElement stack in child.ChildNodes)
						{
							int x = BasicXmlDocument.GetIntAttribute(stack, "x", 0);

							if (!xToBars.ContainsKey(x))
							{
								xToBars.Add(x, new List<Bar>());
							}

							xToTopLabel.Add(x, BasicXmlDocument.GetStringAttribute(stack, "top_label", ""));

							foreach (XmlElement stackable in stack.ChildNodes)
							{
								switch (stackable.Name)
								{
									case "bar":
										xToBars[x].Add(new Bar(this, stackable));
										break;

									case "line":
										xToBars[x].Add(new Line(this, stackable));
										break;

									case "annotation":
										xToAnnotation[x] = new MouseoverAnnotation(stackable);
										break;
								}
							}
						}
						break;
				}
			}

			LegendFontSize = 9f;
			AxisFontSize = 8;

			ForeColor = Color.White;
			BackColor = Color.Black;
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


		protected override void OnPaint(PaintEventArgs e)
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

			var topChartMargin = 30;

			var recX = (float)yAxisWidth;
			var recY = topChartMargin;
			var recWidth = (float)(ClientSize.Width - yAxisWidth);
			var recHeight = (float)(ClientSize.Height - xAxisHeight - topChartMargin);
			RectangleF chartArea = new RectangleF(recX, recY, recWidth, recHeight);

			double stackWidth = chartArea.Width / (double)(xAxis.Max + 1 - xAxis.Min);

			int stringHeight = legendFont.Height;

			// Y axis.
			if (yAxis.Visible)
			{
				using (Pen pen = new Pen(ForeColor))
				{
					e.Graphics.DrawLine(pen, chartArea.Left, chartArea.Bottom, chartArea.Left, chartArea.Top);
				}

				// Y axis ticks (and the X axis).
				using (Pen forePen = new Pen (ForeColor))
				using (Pen pen = new Pen (yAxis.TickColour))
				{
					for (double y = yAxis.Min; y <= yAxis.Max; y = BarChartUtils.CheckedAdvance(y, yAxis.Max, yAxis.TickInterval))
					{
						int screenY = (int) (chartArea.Bottom - ((y - yAxis.Min) * chartArea.Height / (yAxis.Max - yAxis.Min)));

						Pen usePen = pen;
						if (y == yAxis.Min)
						{
							usePen = forePen;
						}

						e.Graphics.DrawLine(forePen, (int) (chartArea.Left - axisTickLength), screenY, chartArea.Left, screenY);
						e.Graphics.DrawLine(usePen, chartArea.Left, screenY, chartArea.Right, screenY);
					}
				}

				// Y axis numbers.
				for (double y = yAxis.Min; y <= yAxis.Max; y = BarChartUtils.CheckedAdvance(y, yAxis.Max, yAxis.NumberInterval))
				{
					int screenY = (int)(chartArea.Bottom - ((y - yAxis.Min) * chartArea.Height / (yAxis.Max - yAxis.Min)));
					double zoneHeight = 100;

					RectangleF labelArea = new RectangleF(0, (float)(screenY - zoneHeight), (float)(chartArea.Left - axisTickTextOffset), (float)(2 * zoneHeight));
					StringFormat format = new StringFormat();
					format.Alignment = StringAlignment.Far;
					format.LineAlignment = StringAlignment.Center;

					using (Brush brush = new SolidBrush(ForeColor))
					{
						e.Graphics.DrawString(CONVERT.ToStr(y), axisFont, brush, labelArea, format);
					}
				}

				// Y axis legend.
				{
					RectangleF legendRectangle = new RectangleF(0, 0, (float)yAxisWidth - yAxisLegendMargin, Height - topChartMargin);
					SizeF legendSize = e.Graphics.MeasureString(yAxis.Legend, legendFont);

					Matrix oldMatrix = e.Graphics.Transform;
					e.Graphics.TranslateTransform((legendRectangle.Left + legendRectangle.Right - legendSize.Height) / 2,
													(legendRectangle.Top + legendRectangle.Bottom + legendSize.Width) / 2);
					e.Graphics.RotateTransform(-90);

					using (Brush brush = new SolidBrush(ForeColor))
					{
						e.Graphics.DrawString(yAxis.Legend, legendFont, brush, 0, 0);
					}

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
			Dictionary<string, Line> categoryNameTolastKnownLine = new Dictionary<string, Line>();
			for (int x = xAxis.Min; x <= xAxis.Max; x++)
			{
				double stackX = chartArea.Left + ((x - xAxis.Min) * stackWidth);

				if (showMouseAnnotations)
				{
					// Annotation?
					if (mouseHoverLocation.HasValue && xToAnnotation.ContainsKey(x))
					{
						if ((mouseHoverLocation.Value.X >= stackX) && (mouseHoverLocation.Value.X < (stackX + stackWidth)))
						{
							int width = Math.Max(250, (int)stackWidth);
							SizeF size = e.Graphics.MeasureString(xToAnnotation[x].Text, axisFont, width);
							RectangleF popup = new RectangleF((float)stackX, chartArea.Top, size.Width, size.Height);

							using (Brush foreBrush = new SolidBrush (ForeColor))
							using (Brush backBrush = new SolidBrush (BackColor))
							using (Pen pen = new Pen(ForeColor))
							{
								e.Graphics.FillRectangle(backBrush, popup);
								e.Graphics.DrawRectangle(pen, new Rectangle((int) popup.Left, (int) popup.Top, (int) popup.Width, (int) popup.Height));
								e.Graphics.DrawString(xToAnnotation[x].Text, axisFont, foreBrush, popup);
							}
						}
					}
				}

				double heightAccumulator = 0;
				if (xToBars.ContainsKey(x))
				{
					List<Bar> stack = xToBars[x];

					//for (int j = 0; j < stack.Count; j++)
					for (int j = stack.Count - 1; j >= 0; j--)
					{
						int barX = (int)stackX;
						Bar bar = stack[j];

						if (bar is Line)
						{
							Line newValue = (Line)bar;

							Line oldValue = null;
							if (categoryNameTolastKnownLine.ContainsKey(newValue.Category.Name))
							{
								oldValue = categoryNameTolastKnownLine[newValue.Category.Name];
							}

							int newY = (int)(chartArea.Bottom - ((newValue.Y - yAxis.Min) * chartArea.Height / (yAxis.Max - yAxis.Min)));
							using (Pen pen = new Pen(newValue.Category.Colour, 2))
							{
								if (oldValue != null)
								{
									int oldY = (int)(chartArea.Bottom - ((oldValue.Y - yAxis.Min) * chartArea.Height / (yAxis.Max - yAxis.Min)));
									e.Graphics.DrawLine(pen, barX, oldY, barX, newY);
								}

								e.Graphics.DrawLine(pen, barX - 1, newY, (int)(barX + stackWidth + 1), newY);

								if (newValue.show_above_line_at_end == true)
								{
									PointF pf = new PointF(barX + (int)stackWidth + 1, newY);
									string datastr = CONVERT.ToStr(newValue.Y);

									SizeF textsize = e.Graphics.MeasureString(datastr, this.Font);
									pf.X = pf.X - (textsize.Width + 10);
									pf.Y = pf.Y - ((textsize.Height * 3) / 2);

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

								using (Brush brush = new SolidBrush (ForeColor))
								{
									e.Graphics.FillRectangle(brush, barOutlineBounds);
								}

								using (Pen pen = new Pen(bar.Category.BorderColour, (float)bar.Category.BorderThickness))
								{
									e.Graphics.DrawRectangle(pen, barOutlineBounds);
								}

								using (Brush brush = new SolidBrush(bar.Category.Colour))
								{
									e.Graphics.FillRectangle(brush, barFillBounds);
								}

								if (ShowNumbersOnBar)
								{
									StringFormat format = new StringFormat();
									format.Alignment = StringAlignment.Center;
									format.LineAlignment = StringAlignment.Center;
									//e.Graphics.DrawString(bar.Legend, legendFont, Brushes.White, barFillBounds, format);
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

							using (Brush brush = new SolidBrush (ForeColor))
							{
								e.Graphics.DrawString(topLabel, legendFont, brush, labelX, labelBottom, format);
							}
						}
					}
				}

				if (xAxis.Visible)
				{
					RectangleF labelArea = new RectangleF((float)stackX, (float)chartArea.Bottom, (float)stackWidth, (float)stringHeight);
					StringFormat format = new StringFormat();
					format.Alignment = StringAlignment.Center;
					format.LineAlignment = StringAlignment.Center;

					using (Brush brush = new SolidBrush(ForeColor))
					{
						e.Graphics.DrawString(CONVERT.ToStr(x), axisFont, brush, labelArea, format);
					}
				}
			}

			// X axis legend.
			if (xAxis.Visible)
			{
				RectangleF legendRectangle = new RectangleF(chartArea.Left, chartArea.Bottom + stringHeight, chartArea.Width, stringHeight);
				StringFormat format = new StringFormat();
				format.Alignment = StringAlignment.Center;
				format.LineAlignment = StringAlignment.Center;
				using (Brush brush = new SolidBrush(ForeColor))
				{
					e.Graphics.DrawString(xAxis.Legend, legendFont, brush, legendRectangle, format);
				}
			}

			// Dividers.
			foreach (int x in dividerXes)
			{
				Rectangle dividerRectangle = new Rectangle((int)(chartArea.Left + ((x - xAxis.Min) * stackWidth)), (int)chartArea.Top, (int)stackWidth, (int)chartArea.Height);

				using (Pen pen = new Pen (ForeColor))
				{
					e.Graphics.DrawRectangle(pen, dividerRectangle);
				}
			}
		}

		public Category GetCategoryByName(string name)
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

		protected override void OnMouseLeave(EventArgs e)
		{
			base.OnMouseEnter(e);
			mouseHoverLocation = null;
			Invalidate();
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			mouseHoverLocation = e.Location;
			Invalidate();
		}
	}
}