using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Xml;

using LibCore;
using CoreUtils;


namespace Charts
{
	public class StackBarChartWithBackground : StackedBarChart
	{
		Bitmap BackImage = null;
		bool UseBackImage = false;
		Hashtable DisplayValues = new Hashtable();
		bool showDisplayValues = true;

		Font Font_Data;

		public StackBarChartWithBackground(XmlElement xml)
			: base(xml)
		{
			string font = SkinningDefs.TheInstance.GetData("fontname");
			Font_Data = FontRepository.GetFont(font, 10, FontStyle.Bold);

			ForeColor = Color.White;
			BackColor = Color.Black;
		}

		public void SetBackImageFromFile(string BackImageName, bool useBack)
		{
			BackImage = (Bitmap)Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\" + BackImageName);
			UseBackImage = true;
		}

		public void SetBackImageFromBitmap(Bitmap newImage, bool useBack)
		{
			BackImage = newImage;
			UseBackImage = true;
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			if (UseBackImage)
			{
				if (BackImage != null)
				{
					e.Graphics.DrawImage(BackImage, 0, 0, this.Width, this.Height);
				}
			}

			double axisTickLength = 5;
			double axisTickTextOffset = 5;
			int barMargin = 2;

			DisplayValues.Clear();

			double maxStackLabelWidth = 0;
			foreach (int x in xToBars.Keys)
			{
				SizeF size = e.Graphics.MeasureString(CONVERT.ToStr(x), axisFont);
				maxStackLabelWidth = Math.Max(maxStackLabelWidth, size.Width);
			}

			RectangleF chartArea = new RectangleF(leftMargin + (float)yAxisWidth, topMargin, (float)(ClientSize.Width - yAxisWidth - rightMargin - leftMargin), (float)(ClientSize.Height - xAxisHeight - topMargin - bottomMargin));

			double stackWidth = chartArea.Width / (double)(xAxis.Max + 1 - xAxis.Min);

			int stringHeight = legendFont.Height;

			// Y axis.
			if (yAxis.Visible)
			{
				using (Pen forePen = new Pen(ForeColor))
				{
					e.Graphics.DrawLine(forePen, chartArea.Left, chartArea.Bottom, chartArea.Left, chartArea.Top);

					// Y axis ticks (and the X axis).
					using (Pen pen = new Pen(yAxis.TickColour))
					{
						for (double y = yAxis.Min; y <= yAxis.Max; y = BarChartUtils.CheckedAdvance(y, yAxis.Max, yAxis.TickInterval))
						{
							int screenY = (int) (chartArea.Bottom - ((y - yAxis.Min) * chartArea.Height / (yAxis.Max - yAxis.Min)));

							Pen usePen = pen;
							if (y == yAxis.Min)
							{
								usePen = forePen;
							}

							e.Graphics.DrawLine(usePen, (int) (chartArea.Left - axisTickLength), screenY, chartArea.Right, screenY);
						}
					}
				}

				using (Brush brush = new SolidBrush(ForeColor))
				{
					// Y axis numbers.
					for (double y = yAxis.Min; y <= yAxis.Max; y = BarChartUtils.CheckedAdvance(y, yAxis.Max, yAxis.NumberInterval))
					{
						int screenY = (int) (chartArea.Bottom - ((y - yAxis.Min) * chartArea.Height / (yAxis.Max - yAxis.Min)));
						double zoneHeight = 100;

						RectangleF labelArea = new RectangleF (0, (float) (screenY - zoneHeight), (float) (chartArea.Left - axisTickTextOffset), (float) (2 * zoneHeight));
						StringFormat format = new StringFormat ();
						format.Alignment = StringAlignment.Far;
						format.LineAlignment = StringAlignment.Center;

						e.Graphics.DrawString(CONVERT.ToStr(y), axisFont, brush, labelArea, format);
					}

					// Y axis legend.
					{
						RectangleF legendRectangle = new RectangleF (0, bottomMargin, (float) yAxisWidth - yAxisLegendMargin, Height - bottomMargin - topMargin);
						SizeF legendSize = e.Graphics.MeasureString(yAxis.Legend, legendFont);

						Matrix oldMatrix = e.Graphics.Transform;
						e.Graphics.TranslateTransform((legendRectangle.Left + legendRectangle.Right - legendSize.Height) / 2,
														(legendRectangle.Top + legendRectangle.Bottom + legendSize.Width) / 2);
						e.Graphics.RotateTransform(-90);
						e.Graphics.DrawString(yAxis.Legend, legendFont, brush, 0, 0);
						e.Graphics.Transform = oldMatrix;
					}
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
							RectangleF popup = new RectangleF ((float) stackX, chartArea.Top, size.Width, size.Height);

							using (Brush backBrush = new SolidBrush (BackColor))
							using (Brush foreBrush = new SolidBrush (ForeColor))
							using (Pen forePen = new Pen (ForeColor))
							{
								e.Graphics.FillRectangle(backBrush, popup);
								e.Graphics.DrawRectangle(forePen, new Rectangle((int) popup.Left, (int) popup.Top, (int) popup.Width, (int) popup.Height));
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
					for (int j = stack.Count -1; j >= 0; j--)
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

									//only draw the last one 
									if (highestBarX == (x+1))
									{
										if (DisplayValues.ContainsKey(newValue.Category.Legend) == false)
										{
											DisplayValues.Add(newValue.Category.Legend, newValue.Y);
										}
										else
										{
											DisplayValues[newValue.Category.Legend] = newValue.Y;
										}
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

								using (Brush brush = new SolidBrush (ForeColor))
								{
									e.Graphics.FillRectangle(brush, barOutlineBounds);
								}

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

				using (Brush brush = new SolidBrush (ForeColor))
				{
					e.Graphics.DrawString(xAxis.Legend, legendFont, brush, legendRectangle, format);
				}
			}

			// Dividers.
			foreach (int x in dividerXes)
			{
				Rectangle dividerRectangle = new Rectangle((int) (chartArea.Left + ((x - xAxis.Min) * stackWidth)), (int) chartArea.Top, (int) stackWidth, (int) chartArea.Height);

				using (Pen pen = new Pen (ForeColor))
				{
					e.Graphics.DrawRectangle(pen, dividerRectangle);
				}
			}

			// Legend.
			using (Brush brush = new SolidBrush(ForeColor))
			{
				e.Graphics.DrawString(legend, legendFont, brush, (float) legendX, (float) legendY);
			}

			// Key.
			double key_height = 0;
			Point last_KeyItem_position = KeyPosition;

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
				SizeF legendBlockSize = new SizeF((float)(rectangleSize + (2 * gap) + maxLegendSize.Width), maxLegendSize.Height);

				foreach (Category category in categories)
				{
					if (category.ShowInKey)
					{
						Rectangle colourOutlineBounds = new Rectangle((int)x, (int)y,
																		 (int)rectangleSize, (int)rectangleSize);
						Rectangle colourFillBounds = new Rectangle((int)(colourOutlineBounds.Left + category.BorderInset),
																	(int)(colourOutlineBounds.Top + category.BorderInset),
																	(int)((int)rectangleSize - (2 * category.BorderInset)) + 1,
																	(int)((int)rectangleSize - (2 * category.BorderInset)) + 1);

						using (Brush brush = new SolidBrush (ForeColor))
						{
							e.Graphics.FillRectangle(brush, colourOutlineBounds);
						}

						using (Pen pen = new Pen(category.BorderColour, (float)category.BorderThickness))
						{
							e.Graphics.DrawRectangle(pen, colourOutlineBounds);
						}

						using (Brush brush = new SolidBrush(category.Colour))
						{
							e.Graphics.FillRectangle(brush, colourFillBounds);
						}

						Rectangle textBounds = new Rectangle((int)(colourOutlineBounds.Right + gap),
																(int)y, (int)(legendBlockSize.Width - (colourOutlineBounds.Right + gap - colourOutlineBounds.Left)), (int)legendBlockSize.Height);
						StringFormat format = new StringFormat();
						format.Alignment = StringAlignment.Near;
						format.LineAlignment = StringAlignment.Center;

						using (Brush brush = new SolidBrush (ForeColor))
						{
							e.Graphics.DrawString(category.Legend, legendFont, brush, textBounds, format);
						}

						x += legendBlockSize.Width;
						if ((x + legendBlockSize.Width) >= Width)
						{
							x = KeyPosition.X;
							y += legendBlockSize.Height + gap;
							key_height = y;
						}
						last_KeyItem_position.X = (int)x;
						last_KeyItem_position.Y = (int)y;
					}
				}
			}

			if (showDisplayValues)			
			{
				double y = key_height + 30;

				foreach (string name in DisplayValues.Keys)
				{
					double data_value = (double) DisplayValues[name];
					double x_value = (double)last_KeyItem_position.X;
					double y_value = ((double)last_KeyItem_position.Y) + 30;

					string display_str = name + "    " + CONVERT.ToStrRounded(data_value, 0);

					using (Brush brush = new SolidBrush (ForeColor))
					{
						e.Graphics.DrawString(display_str, Font_Data, brush, (int) x_value, (int) y_value);
					}

					y += 20;
				}
			}
		}
	}
}