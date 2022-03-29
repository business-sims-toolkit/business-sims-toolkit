using System;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;

using System.Xml;

using LibCore;

namespace Charts
{
	public class Annotation
	{
		public string Text;
		public double X;
		public double Y;
		public StringAlignment Alignment;
		public StringAlignment LineAlignment;
		public bool AutoY;
		public bool ShowOnlyOnMouseover;
	}

	public class Block
	{
		public double X;
		public double Y;
		public double Width;
		public double Height;
		public bool Elliptical;
		public bool MakeSquare;
		public bool OnlyBorder;
		public int BorderThickness;

		public Color Colour;
		public string Legend;
		public Color LegendColour;
		public Color FillColour;
		public StringAlignment LegendAlignment = StringAlignment.Center;
		public StringAlignment LegendLineAlignment = StringAlignment.Center;
	}

	public class DPoint
	{
		public bool Show;
		public double X;
		public double Y;
		public string Label;
		public bool Stepped;

		public DPoint()
		{
			X = 0;
			Y = 0;
			Label = "";
			Show = true;
			Stepped = false;
		}
	}

	public class DataLine
	{
		public Color color = Color.Black;
		public bool dashed = false;
		public bool dotted = false;
		public double thickness = 2;
		public string yaxis = "left";
		public ArrayList data = new ArrayList();
		public string title;
		public bool showInKey = true;
		public bool useLineInKey = false;
		public bool showInHighlight = true;

		public List<Annotation> annotations = new List<Annotation> ();

		public bool Stepped = false;

		public double GetValueAt (double x)
		{
			DPoint previousPoint = null;
			DPoint nextPoint = null;
			foreach (DPoint point in data)
			{
				if ((point.X < x) && ((previousPoint == null) || ((x - point.X) < (x - previousPoint.X))))
				{
					previousPoint = point;
				}

				if ((point.X > x) && ((nextPoint == null) || ((point.X - x) < (nextPoint.X - x))))
				{
					nextPoint = point;
				}
			}

			if ((nextPoint != null) && nextPoint.Stepped)
			{
				if (previousPoint != null)
				{
					return previousPoint.Y;
				}
			}
			else
			{
				if (previousPoint != null)
				{
					if (nextPoint != null)
					{
						return previousPoint.Y + ((x - previousPoint.X) * (nextPoint.Y - previousPoint.Y) / (nextPoint.X - previousPoint.X));
					}
					else
					{
						return previousPoint.Y;
					}
				}
			}

			return 0;
		}
	}

	public class LineGraph : Chart
	{
		class TopKeyElement
		{
			public Color colour;
			public double thickness;
			public string text;

			public TopKeyElement()
			{
				colour = Color.Transparent;
				thickness = 2;
				text = "";
			}
		}

		bool betterAutoScale = false;

		bool enableHighlightBar = false;
		Point? highlightCursorPosition = null;

		protected ArrayList topKeyElements = new ArrayList();

		public PrintLabel mainTitle = new PrintLabel();
		//
		public RightYAxis rightAxis;
		public LeftYAxis leftAxis;
		public XAxis xAxis;
		protected bool xAxisVisible = false;
		protected bool rightAxisVisible = false;

		protected bool have_graduation = false;
		protected Color graduated_colour;

		bool draw_key;
		Point keyOffset;

		List<Block> blocks;

		//
		// An array of AxisDataLines
		//
		protected ArrayList dataLines = new ArrayList();
		//
		int TopOffset;
		//
		public void SetTitles(string mTitle, string xaxis, string lyaxis, string ryaxis)
		{
			mainTitle.Text = mTitle;
			xAxis.axisTitle.Text = xaxis;
			rightAxis.axisTitle.Text = ryaxis;
			leftAxis.axisTitle.Text = lyaxis;
		}
		//
		public void SetFont(string name, int titleSize, FontStyle fs1, int axesSize, FontStyle fs2)
		{
			mainTitle.Font = ConstantSizeFont.NewFont(name, titleSize, fs1);
			mainTitle.TextAlign = ContentAlignment.MiddleCenter;
			Font f = ConstantSizeFont.NewFont(name, axesSize, fs2);
			xAxis.Font = f;
			rightAxis.Font = f;
			leftAxis.Font = f;
		}
		//
		public LineGraph()
		{
			this.SuspendLayout();

			rightAxis = new RightYAxis();
			leftAxis = new LeftYAxis();
			xAxis = new XAxis();
			//
			this.AutoScroll = false;
			mainTitle = new PrintLabel();
			Controls.Add(mainTitle);
			Controls.Add(xAxis);
			Controls.Add(rightAxis);
			Controls.Add(leftAxis);
			Resize += LineGraph_Resize;
			//
			leftAxis.SetRange(0, 10, 10);
			rightAxis.SetRange(0, 10, 10);
			xAxis.SetRange(0, 6, 6);
			//
			this.ResumeLayout(false);

			draw_key = false;
			keyOffset = new Point(0, 0);
			//
			DoSize();

			blocks = new List<Block> ();

			this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			this.SetStyle(ControlStyles.UserPaint, true);
			this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
		}

		public void SetXAxisLastItemAutoShift(bool setshift)
		{
			xAxis.AutoLastLabelSlide = setshift;
		}

		public void SetMainTitleVisibility(bool vis)
		{
			mainTitle.Visible = vis;
		}

		//
		void LineGraph_Resize(object sender, EventArgs e)
		{
			DoSize();
			this.Invalidate();
		}
		//
		protected void DoSize()
		{
			this.SuspendLayout();
			mainTitle.Size = new Size(Width, 40);

			TopOffset = mainTitle.Bottom;

			int xAxisHeight = 40;
			if (!xAxisVisible)
			{
				xAxisHeight = 1;
			}

			leftAxis.Location = new Point(0, TopOffset);
			leftAxis.Size = new Size(leftAxis.Width, Height - TopOffset - xAxisHeight);
			//
			rightAxis.Height = leftAxis.Height;
			if (!rightAxisVisible)
			{
				rightAxis.Size = new Size(1, leftAxis.Height);
			}
			rightAxis.Location = new Point(Width - rightAxis.Width, TopOffset);

			xAxis.Size = new Size(((Width - rightAxis.Width) - leftAxis.Width) + 4, xAxisHeight);
			xAxis.Location = new Point(leftAxis.Width - 2, Height - xAxis.Height);
			this.ResumeLayout(false);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			double ww = xAxis.Width - 4;
			double hh = leftAxis.Height;

			int xl = xAxis.Left;
			int yl = xAxis.Top;

			int xSteps = xAxis.Steps;
			int ySteps = leftAxis.Steps;

			if ((! xAxis.showGrid) || (xSteps <= 0))
			{
				xSteps = -1;
			}

			if ((! leftAxis.showGrid) || (ySteps <= 0))
			{
				ySteps = -1;
			}

			ChartUtils.DrawGrid(e, this, Color.LightGray, xl, TopOffset, xAxis.Width, yl - TopOffset, xSteps, ySteps);

			e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

			foreach (Block block in blocks)
			{
				double xScale = ww / (xAxis.Max - xAxis.Min);
				double yScale = hh / (leftAxis.Max - leftAxis.Min);

				PointF blockOrigin = new PointF ((float) block.X, (float) block.Y);
				int blockWidth = (int) (block.Width * xScale);
				int blockHeight = (int) (block.Height * yScale);

				if (block.MakeSquare)
				{
					int oldBlockWidth = blockWidth;
					int oldBlockHeight = blockHeight;

					blockWidth = Math.Min(blockWidth, blockHeight);
					blockHeight = Math.Min(blockWidth, blockHeight);

					blockOrigin.X += (float) ((oldBlockWidth - blockWidth) / (2 * xScale));
					blockOrigin.Y += (float) ((oldBlockHeight - blockHeight) / (2 * yScale));
				}

				RectangleF rectangle = new RectangleF (xAxis.Left + 2 + (int) ((blockOrigin.X - xAxis.Min) * xScale),
													   xAxis.Top - (int) (((blockOrigin.Y - leftAxis.Min) * yScale) + (block.Height * yScale)),
													   blockWidth, blockHeight);

				if (block.OnlyBorder)
				{
					if (block.FillColour != null)
					{
						using (Brush brush = new SolidBrush (block.FillColour))
						{
							if (block.Elliptical)
							{
								e.Graphics.FillEllipse(brush, rectangle);
							}
							else
							{
								e.Graphics.FillRectangle(brush, new Rectangle ((int) rectangle.Left, (int) rectangle.Top,
																			   (int) rectangle.Width, (int) rectangle.Height));
							}
						}
					}

					using (Pen pen = new Pen (block.Colour, block.BorderThickness))
					{
						if (block.Elliptical)
						{
							e.Graphics.DrawEllipse(pen, rectangle);
						}
						else
						{
							e.Graphics.DrawRectangle(pen, new Rectangle ((int) rectangle.Left, (int) rectangle.Top,
																		 (int) rectangle.Width, (int) rectangle.Height));
						}
					}
				}
				else
				{
					using (Brush brush = new SolidBrush (block.Colour))
					{
						if (block.Elliptical)
						{
							e.Graphics.FillEllipse(brush, rectangle);
						}
						else
						{
							e.Graphics.FillRectangle(brush, rectangle);
						}
					}
				}

				StringFormat format = new StringFormat ();
				format.Alignment = block.LegendAlignment;
				format.LineAlignment = block.LegendLineAlignment;
				using (Brush brush = new SolidBrush (block.LegendColour))
				{
					e.Graphics.DrawString(block.Legend, xAxis.Font, brush, rectangle, format);
				}
			}

			if (enableHighlightBar && highlightCursorPosition.HasValue)
			{
				int barWidth = 8;
				e.Graphics.FillRectangle(Brushes.LightSteelBlue, highlightCursorPosition.Value.X - (barWidth / 2), TopOffset, barWidth, yl - TopOffset);

				int x = highlightCursorPosition.Value.X + barWidth;
				int y = TopOffset;

				StringFormat format = new StringFormat ();
				format.Alignment = StringAlignment.Near;

				if (x > (Width / 2))
				{
					format.Alignment = StringAlignment.Far;
					x -= (2 * barWidth);
				}

				foreach (DataLine dataLine in dataLines)
				{
					if (dataLine.showInHighlight)
					{
						double xInGraph = xAxis.Min + ((highlightCursorPosition.Value.X - xl) * (xAxis.Max - xAxis.Min) * 1.0 / xAxis.Width);

						e.Graphics.DrawString(String.Format("{0}: {1}", dataLine.title, CONVERT.ToStr(dataLine.GetValueAt(xInGraph), 2)),
												xAxis.Font, Brushes.Black, x, y, format);
						y += 16;
					}
				}
			}

			//
			int ymin = 0;
			int ymax = 10;
			//
			// : temp gradient
			if (have_graduation)
			{
				Rectangle top = new Rectangle(0, 0, xAxis.Width, xAxis.Top);
				System.Drawing.Drawing2D.LinearGradientBrush b = new System.Drawing.Drawing2D.LinearGradientBrush(top, graduated_colour, Color.White, System.Drawing.Drawing2D.LinearGradientMode.Vertical);

				foreach (DataLine dl in dataLines)
				{
					SetStroke((int)dl.thickness, dl.color);

					switch (dl.yaxis)
					{
						case "left":
							{
								ymin = leftAxis.Min;
								ymax = leftAxis.Max;
							}
							break;

						case "right":
							{
								ymin = rightAxis.Min;
								ymax = rightAxis.Max;
							}
							break;
					}
					//
					double xrange = xAxis.Max - xAxis.Min;
					double yrange = ymax - ymin;
					bool first = true;
					foreach (DPoint dp in dl.data)
					{
						int x = xAxis.Left + 2 + (int)(ww * (dp.X - xAxis.Min) / xrange);
						int y = xAxis.Top - (int)(hh * (dp.Y - ymin) / yrange);

						//
						// : Attempt to do a red temperature gradient to highlight it getting hot.
						//
						if (first)
						{
							MoveTo(x, y);
							first = false;
						}
						else
						{
							System.Drawing.Drawing2D.GraphicsPath gp = new System.Drawing.Drawing2D.GraphicsPath();
							gp.FillMode = System.Drawing.Drawing2D.FillMode.Winding;
							gp.AddLine(_px, _py, x + 1, y);
							gp.AddLine(x + 1, y, x + 1, xAxis.Top);
							gp.AddLine(x + 1, xAxis.Top, _px, xAxis.Top);
							gp.AddLine(_px, xAxis.Top, _px, _py);
							e.Graphics.FillPath(b, gp);

							MoveTo(x, y);
						}
					}
				}
			}

			foreach (DataLine dl in dataLines)
			{
				SetStroke((int)dl.thickness, dl.color);
				SetDashedOrDotted(dl.dashed, dl.dotted);

				switch (dl.yaxis)
				{
					case "left":
						{
							ymin = leftAxis.Min;
							ymax = leftAxis.Max;
						}
						break;

					case "right":
						{
							ymin = rightAxis.Min;
							ymax = rightAxis.Max;
						}
						break;
				}
				//
				double xrange = xAxis.Max - xAxis.Min;
				double yrange = ymax - ymin;
				bool first = true;
				int oldY = 0;
				//
				// Now draw the lines.
				//
				if ((xrange != 0) && (yrange != 0))
				{
					foreach (DPoint dp in dl.data)
					{
						int x = xAxis.Left + 2 + (int) (ww * (dp.X - xAxis.Min) / xrange);
						int y = xAxis.Top - (int) (hh * (dp.Y - ymin) / yrange);
						//
						if (first)
						{
							MoveTo(x, y);
							first = false;
						}
						else
						{
							if (dp.Stepped)
							{
								LineTo(e, x, oldY);
							}

							LineTo(e, x, y);
						}
						oldY = y;

						double thickness = dl.thickness / 4;
						if (dp.Show)
						{
							thickness *= dl.thickness;
						}

						//draw a dot where the point is
						int xx = (int) (x - (2 * thickness));
						int yy = (int) (y - (2 * thickness));
						if (dp.Y == 0) yy = (int) (y - (4 * thickness));
						if (dp.X == xAxis.Max) xx = (int) (x - (4 * thickness));

						e.Graphics.FillEllipse(new SolidBrush(dl.color), xx, yy, (float) (4 * thickness), (float) (4 * thickness));
					}
				}
			}

			// Now draw the labels, on top of the lines.
			foreach (DataLine dl in dataLines)
			{
				switch (dl.yaxis)
				{
					case "left":
						{
							ymin = leftAxis.Min;
							ymax = leftAxis.Max;
						}
						break;

					case "right":
						{
							ymin = rightAxis.Min;
							ymax = rightAxis.Max;
						}
						break;
				}
				//
				double xrange = xAxis.Max - xAxis.Min;
				double yrange = ymax - ymin;

				if ((xrange != 0) && (yrange != 0))
				{
					foreach (DPoint dp in dl.data)
					{
						int x = xAxis.Left + 2 + (int) (ww * (dp.X - xAxis.Min) / xrange);
						int y = xAxis.Top - (int) (hh * (dp.Y - ymin) / yrange);

						if (dp.Label != "")
						{
							SizeF size = e.Graphics.MeasureString(dp.Label, xAxis.Font);

							x = Math.Max(xAxis.Left, Math.Min(x - (int) (size.Width / 2), xAxis.Right - (int) size.Width));
							y = Math.Max(leftAxis.Top, Math.Min(y - (int) size.Height, leftAxis.Bottom - (int) size.Height));

							e.Graphics.DrawString(dp.Label, xAxis.Font, Brushes.Black, x, y);
						}
					}
				}

				// Draw any annotations.
				int lastY = xAxis.Top;
				Annotation shownMouseoverAnnotation = null;
				foreach (Annotation annotation in dl.annotations)
				{
					if (annotation.ShowOnlyOnMouseover)
					{
						if (shownMouseoverAnnotation != null)
						{
							continue;
						}
						else if (highlightCursorPosition.HasValue)
						{
							int annotationScreenX = (int) (xAxis.Left + ((annotation.X - xAxis.Min) * xAxis.Width / (xAxis.Max - xAxis.Min)));
							int annotationScreenY = (int) (leftAxis.Bottom - ((annotation.Y - leftAxis.Min) * leftAxis.Height / (leftAxis.Max - leftAxis.Min)));

							int dx = annotationScreenX - highlightCursorPosition.Value.X;
							int dy = annotationScreenY - highlightCursorPosition.Value.Y;

							int distanceToAnnotation = (dx * dx) + (dy * dy);
							if (distanceToAnnotation > (10 * 10))
							{
								continue;
							}

							shownMouseoverAnnotation = annotation;
						}
						else
						{
							continue;
						}
					}

					int x = xAxis.Left + 2 + (int) (ww * (annotation.X - xAxis.Min) / xrange);
					int y = xAxis.Top - (int)(hh * (annotation.Y - ymin) / yrange);

					if (annotation.AutoY)
					{
						y = lastY;
						lastY -= 15;
					}

					Point point = new Point (x, y);

					StringFormat format = new StringFormat ();
					format.Alignment = annotation.Alignment;
					format.LineAlignment = annotation.LineAlignment;

					e.Graphics.DrawString(annotation.Text, xAxis.Font, Brushes.Black, point, format);
				}
			}

			// Draw the top key too.
			int left = leftAxis.Right + 10;
			int width = Width - 10 - left;
			for (int i = 0; i < topKeyElements.Count; i++)
			{
				TopKeyElement key = topKeyElements[i] as TopKeyElement;

				int w = width / topKeyElements.Count;

				Rectangle rect = new Rectangle((left) + (w * i), 0, w, 10);

				int squareSize = 20;

				Rectangle keyRect = new Rectangle(rect.Left, rect.Top + ((rect.Height - squareSize) / 2), squareSize, squareSize);
				if (key.colour != Color.Transparent)
				{
					using (Pen pen = new Pen(key.colour, (float)key.thickness))
					{
						e.Graphics.DrawLine(pen, keyRect.Left, keyRect.Top + (keyRect.Height / 2), keyRect.Right, keyRect.Top + (keyRect.Height / 2));
					}
				}

				SizeF size = e.Graphics.MeasureString(key.text, this.Font);
				e.Graphics.DrawString(key.text, this.Font, Brushes.Black, keyRect.Right + squareSize, rect.Top + ((rect.Height - (int)size.Height) / 2));
			}

			DrawKey(e.Graphics);
		}

		void DrawKey(Graphics g)
		{
			if (!draw_key) return;

			if (dataLines.Count > 0)
			{
				Font font = ConstantSizeFont.NewFont("Tahoma", 8f, FontStyle.Bold);
				int rowHeight = 16;
				int maxWidth = 0;
				int linesInKey = 0;

				foreach (DataLine dataLine in dataLines)
				{
					if (dataLine.showInKey)
					{
						SizeF size = g.MeasureString(dataLine.title, font);
						if (size.Width > maxWidth)
							maxWidth = (int) size.Width;
						linesInKey++;
					}
				}

				float width = 18 + maxWidth + 5;
				float height = 5 + (linesInKey * rowHeight);

				int x = this.xAxis.Left + 30 + keyOffset.X;
				int y = 50 + keyOffset.Y;

				Rectangle borderRect = new Rectangle(x, y, (int)width + 8, (int)height + 1);
				g.FillRectangle(new SolidBrush(Color.FromArgb(200, 255, 255, 255)), borderRect);
				g.DrawRectangle(new Pen(Color.Black, 1f), borderRect);

				foreach (DataLine dataLine in dataLines)
				{
					if (dataLine.showInKey)
					{
						Rectangle rect = new Rectangle(x + 5, y + 5, 12, 12);
						Brush b = new SolidBrush(dataLine.color);

						if (dataLine.dashed || dataLine.dotted || dataLine.useLineInKey)
						{
							Pen pen = new Pen (dataLine.color, (float) dataLine.thickness);
							if (dataLine.dashed)
							{
								pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
							}
							else if (dataLine.dotted)
							{
								pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
							}

							g.DrawLine(pen, x + 5, y + 5 + 6, x + 5 + 12, y + 5 + 6);
						}
						else
						{
							g.FillEllipse(b, rect);
						}

						g.DrawString(dataLine.title, font, Brushes.Black, x + 23, y + 5);
						y += rowHeight;
					}
				}
			}
		}

		protected bool DefineAxis(Axis axis, XmlNode n, out bool autoscale)
		{
			axis.Width = 50;

			autoscale = false;
			bool visible = true;

			char[] sep = { ',' };
			//
			foreach (XmlAttribute att in n.Attributes)
			{
				if ("title" == att.Name)
				{
					axis.axisTitle.Text = att.Value;
				}
				else if ("minMaxSteps" == att.Name)
				{
					string[] data = att.Value.Split(sep);
					axis.SetRange(CONVERT.ParseInt(data[0]), CONVERT.ParseInt(data[1]), CONVERT.ParseInt(data[2]));
				}
				else if ("visible" == att.Name)
				{
					visible = CONVERT.ParseBool(att.Value, false);
				}
				else if ("autoScale" == att.Name)
				{
					autoscale = CONVERT.ParseBool(att.Value, false);
				}
				else if ("UseNewLabelCreation" == att.Name)
				{
					if (axis is XAxis)
					{
						((XAxis)axis).UseNewLabelCreation = CONVERT.ParseBool(att.Value, false);
						((XAxis)axis).adjustAlignment();
					}
				}
				else if ("colour" == att.Name)
				{

					string[] parts = att.Value.Split(',');
					if (parts.Length == 3)
					{
						int RedFactor = CONVERT.ParseInt(parts[0]);
						int GreenFactor = CONVERT.ParseInt(parts[1]);
						int BlueFactor = CONVERT.ParseInt(parts[2]);
						axis.SetColour(Color.FromArgb(RedFactor, GreenFactor, BlueFactor));
					}
				}
				else if ("textcolour" == att.Name)
				{

					string[] parts = att.Value.Split(',');
					if (parts.Length == 3)
					{
						int RedFactor = CONVERT.ParseInt(parts[0]);
						int GreenFactor = CONVERT.ParseInt(parts[1]);
						int BlueFactor = CONVERT.ParseInt(parts[2]);
						axis.SetTextColour(Color.FromArgb(RedFactor, GreenFactor, BlueFactor));
					}
				}
				else if ("stripes" == att.Name)
				{
					string stripped = att.Value.Replace(" ", "");
					string [] parts = stripped.Split(',');

					if ((parts.Length % 3) == 0)
					{
						int colourCount = parts.Length / 3;

						Color [] colours = new Color [colourCount];
						for (int i = 0; i < colourCount; i++)
						{
							colours[i] = Color.FromArgb(CONVERT.ParseInt(parts[(i * 3)]),
							                            CONVERT.ParseInt(parts[(i * 3) + 1]),
														CONVERT.ParseInt(parts[(i * 3) + 2]));
						}

						axis.SetStriped(colours);
					}
				}
				else if ("align" == att.Name)
				{
					axis.SetLabelAlignment(att.Value);
				}
				else if ("omit_top" == att.Name)
				{
					axis.OmitTop(CONVERT.ParseBool(att.Value, false));
				}
				else if ("width" == att.Name)
				{
					axis.Size = new Size(CONVERT.ParseInt(att.Value), axis.Height);
				}
				else if ("height" == att.Name)
				{
					axis.Size = new Size(axis.Width, CONVERT.ParseInt(att.Value));
				}
				else if ("grid" == att.Name)
				{
					axis.showGrid = CONVERT.ParseBool(att.Value, false);
				}
			}

			return visible;
		}

		void AutoScaleDataWithRounding (Axis axis, ArrayList dataLines, string axisName)
		{
			double? min = null;
			double? max = null;
			foreach (DataLine dataLine in dataLines)
			{
				foreach (DPoint point in dataLine.data)
				{
					double value = (axisName == "xaxis") ? point.X : point.Y;

					min = (min == null) ? value : Math.Min(min.Value, value);
					max = (max == null) ? value : Math.Max(max.Value, value);
				}
			}

			double interval;

			if (min.HasValue && max.HasValue)
			{
				max = RoundToNiceInterval(max.Value);

				if (min < 0)
				{
					min = RoundToNiceInterval(min.Value);
				}
				else
				{
					min = 0;
				}

				double range = RoundToNiceInterval(max.Value - min.Value);
				interval = range / 10;
				int intervals = (int) Math.Ceiling((max.Value - min.Value) / interval);
				max = min.Value + (intervals * interval);
			}
			else
			{
				min = 0;
				max = 10;
				interval = 1;
			}

			axis.SetRange((int) min.Value, (int) max.Value, (int) interval);
		}

		public void AutoScaleData(Axis axis, ArrayList dataLines, string axisname)
		{
			if (betterAutoScale)
			{
				AutoScaleDataWithRounding(axis, dataLines, axisname);
				return;
			}

			float minScore = float.MaxValue;
			float maxScore = float.MinValue;
			int majInterval = 0;
			int minInterval = 0;
			int minScale = 0;
			int maxScale = 0;

			foreach (DataLine dataLine in dataLines)
			{
				foreach (DPoint point in dataLine.data)
				{
					int val = 0;
					if (axisname == "xaxis")
					{
						val = (int) point.X;
					}
					else if (axisname == "yaxis")
					{
						val = (int) point.Y;
					}
					else
					{
						//error
						return;
					}

					if (val > maxScore) maxScore = val;
					if (val < minScore) minScore = val;
				}
			}

			if (minScore == float.MaxValue) minScore = 0;
			if (maxScore == float.MinValue) maxScore = 0;

			if (minScore < 0)
			{
				minScale = (int)Math.Floor(minScore / 10) * 10;
			}
			else //if (minScore >= 0)
			{
				minScale = 0;
			}

			if (maxScore < 0)
			{
				maxScale = 0;//(int)Math.Floor(maxScore/10) * 10;
			}
			else //if (maxScore >= 0)
			{
				maxScale = (int)Math.Ceiling(maxScore / 10) * 10;
			}

			int span = maxScale - minScale;

			if (span > 0)
			{
				//int logSpan = (int)Math.Log10(span);
				//majInterval = (int)Math.Pow(10, logSpan) / 2;
				majInterval = span / 10;
				minInterval = majInterval / 2;// / 10;
				//this seemed to cut off my top score
				//maxScale = (int)Math.Round((float)maxScale / (float)majInterval, 0) * majInterval;
				maxScale += majInterval;
				if (minScore < 0) minScale -= majInterval;
				/*
								if (maxScale == 10)
								{
									majInterval = 2;
									minInterval = 1;
								}
								else if (maxScale >= 100 && maxScale <= 500)
								{
									majInterval = 10;
									minInterval = 5;
								}*/
			}
			else
			{
				minScale = 0;
				maxScale = 10;
				majInterval = 1;
				minInterval = 1;
			}

			axis.SetRange(minScale, maxScale, majInterval);
		}


		public override void LoadData(string xmldata)
		{
			try
			{

				BasicXmlDocument xdoc = LibCore.BasicXmlDocument.Create(xmldata);
				XmlNode rootNode = xdoc.DocumentElement;

				string YLeftAxisColour = string.Empty;
				string YRightAxisColour = string.Empty;
				bool xAutoScale = false;
				bool yLeftAutoScale = false;
				bool yRightAutoScale = false;

				ArrayList leftAxisData = new ArrayList ();
				ArrayList rightAxisData = new ArrayList ();

				foreach (XmlAttribute att in rootNode.Attributes)
				{
					if (att.Name == "show_key")
					{
						draw_key = CONVERT.ParseBool(att.Value, false);
					}
					else if (att.Name == "key_offset")
					{
						keyOffset = CONVERT.ParsePoint(att.Value);
					}
					else if (att.Name == "better_auto_scale")
					{
						betterAutoScale = CONVERT.ParseBool(att.Value, false);
					}
				}

				foreach (XmlNode child in rootNode.ChildNodes)
				{
					if (child.NodeType == XmlNodeType.Element)
					{
						if (child.Name == "title")
						{
							mainTitle.Text = child.InnerXml;

							StringAlignment horizontalAlignment = StringAlignment.Near;
							StringAlignment verticalAlignment = StringAlignment.Near;

							foreach (XmlAttribute attribute in child.Attributes)
							{
								switch (attribute.Name)
								{
									case "align":
										switch (attribute.Value)
										{
											case "left":
												horizontalAlignment = StringAlignment.Near;
												break;

											case "centre":
											case "center":
											case "middle":
												horizontalAlignment = StringAlignment.Center;
												break;

											case "right":
												horizontalAlignment = StringAlignment.Far;
												break;
										}
										break;

									case "line_align":
										switch (attribute.Value)
										{
											case "top":
												verticalAlignment = StringAlignment.Near;
												break;

											case "centre":
											case "center":
											case "middle":
												verticalAlignment = StringAlignment.Center;
												break;

											case "bottom":
												verticalAlignment = StringAlignment.Far;
												break;
										}
										break;
								}
							}

							switch (horizontalAlignment)
							{
								case StringAlignment.Near:
									switch (verticalAlignment)
									{
										case StringAlignment.Near:
											mainTitle.TextAlign = ContentAlignment.TopLeft;
											break;

										case StringAlignment.Center:
											mainTitle.TextAlign = ContentAlignment.MiddleLeft;
											break;

										case StringAlignment.Far:
											mainTitle.TextAlign = ContentAlignment.BottomLeft;
											break;
									}
									break;

								case StringAlignment.Center:
									switch (verticalAlignment)
									{
										case StringAlignment.Near:
											mainTitle.TextAlign = ContentAlignment.TopCenter;
											break;

										case StringAlignment.Center:
											mainTitle.TextAlign = ContentAlignment.MiddleCenter;
											break;

										case StringAlignment.Far:
											mainTitle.TextAlign = ContentAlignment.BottomCenter;
											break;
									}
									break;

								case StringAlignment.Far:
									switch (verticalAlignment)
									{
										case StringAlignment.Near:
											mainTitle.TextAlign = ContentAlignment.TopRight;
											break;

										case StringAlignment.Center:
											mainTitle.TextAlign = ContentAlignment.MiddleRight;
											break;

										case StringAlignment.Far:
											mainTitle.TextAlign = ContentAlignment.BottomRight;
											break;
									}
									break;
							}
						}
						else if (child.Name == "xAxis")
						{
							xAxisVisible = DefineAxis((Axis)xAxis, child, out xAutoScale);
						}
						else if (child.Name == "gradcolour")
						{
							string[] parts = child.InnerText.Split(',');
							if (parts.Length == 3)
							{
								int RedFactor = CONVERT.ParseInt(parts[0]);
								int GreenFactor = CONVERT.ParseInt(parts[1]);
								int BlueFactor = CONVERT.ParseInt(parts[2]);
								this.graduated_colour = Color.FromArgb(RedFactor, GreenFactor, BlueFactor);
								this.have_graduation = true;
							}
							else
							{
								this.have_graduation = false;
							}
						}
						else if (child.Name == "yLeftAxis")
						{
							DefineAxis((Axis)leftAxis, child, out yLeftAutoScale);

							//store the colour incase the yaxis data has no colour attribute
							foreach (XmlAttribute att in child.Attributes)
							{
								if (att.Name == "colour")
								{
									YLeftAxisColour = att.Value;
								}
							}
						}
						else if (child.Name == "yRightAxis")
						{
							rightAxisVisible = DefineAxis((Axis)rightAxis, child, out yRightAutoScale);

							//store the colour incase the yaxis data has no colour attribute
							foreach (XmlAttribute att in child.Attributes)
							{
								if (att.Name == "colour")
								{
									YRightAxisColour = att.Value;
								}
							}
						}
						else if (child.Name == "topkey")
						{
							TopKeyElement key = new TopKeyElement();
							foreach (XmlAttribute att in child.Attributes)
							{
								switch (att.Name.ToLower())
								{
									case "colour":
										key.colour = CONVERT.ParseComponentColor(att.Value);
										break;
								}
							}

							key.text = child.InnerText;

							topKeyElements.Add(key);
						}
						else if (child.Name == "block")
						{
							Block block = new Block ();
							block.BorderThickness = 1;

							foreach (XmlAttribute att in child.Attributes)
							{
								switch (att.Name.ToLower())
								{
									case "colour":
										block.Colour = CONVERT.ParseComponentColor(att.Value);

										if (block.FillColour == null)
										{
											block.FillColour = CONVERT.ParseComponentColor(att.Value);
										}
										break;

									case "legend_colour":
										block.LegendColour = CONVERT.ParseComponentColor(att.Value);
										break;

									case "fill_colour":
										block.FillColour = CONVERT.ParseComponentColor(att.Value);
										break;

									case "x":
										block.X = CONVERT.ParseDouble(att.Value);
										break;

									case "y":
										block.Y = CONVERT.ParseDouble(att.Value);
										break;

									case "width":
										block.Width = CONVERT.ParseDouble(att.Value);
										break;

									case "height":
										block.Height = CONVERT.ParseDouble(att.Value);
										break;

									case "elliptical":
										block.Elliptical = CONVERT.ParseBool(att.Value, false);
										break;

									case "border_only":
										block.OnlyBorder = CONVERT.ParseBool(att.Value, false);
										break;

									case "border_thickness":
										block.BorderThickness = CONVERT.ParseInt(att.Value);
										break;

									case "make_square":
										block.MakeSquare = CONVERT.ParseBool(att.Value, false);
										break;

									case "align":
										switch (att.Value.ToLower())
										{
											case "left":
												block.LegendAlignment = StringAlignment.Near;
												break;

											case "centre":
												block.LegendAlignment = StringAlignment.Center;
												break;

											case "right":
												block.LegendAlignment = StringAlignment.Far;
												break;
										}
										break;

									case "line_align":
										switch (att.Value.ToLower())
										{
											case "top":
												block.LegendLineAlignment = StringAlignment.Near;
												break;

											case "middle":
												block.LegendLineAlignment = StringAlignment.Center;
												break;

											case "bottom":
												block.LegendLineAlignment = StringAlignment.Far;
												break;
										}
										break;
								}
							}

							block.Legend = child.InnerText;

							blocks.Add(block);
						}
						else if (child.Name == "data")
						{
							DataLine data = new DataLine();
							string DataColour = string.Empty;
							string ColourToUse = string.Empty;

							// Load the data points against the corect axis.
							foreach (XmlAttribute att in child.Attributes)
							{
								if ("yscale" == att.Name)
								{
									data.yaxis = att.Value;
								}
								else if (att.Name == "colour")
								{
									DataColour = att.Value;
								}
								else if (att.Name == "thickness")
								{
									data.thickness = CONVERT.ParseDouble(att.Value);
								}
								else if (att.Name == "dashed")
								{
									data.dashed = CONVERT.ParseBool(att.Value, false);
								}
								else if (att.Name == "dotted")
								{
									data.dotted = CONVERT.ParseBool(att.Value, false);
								}
								else if (att.Name == "title")
								{
									data.title = att.Value;
								}
								else if (att.Name == "show_in_key")
								{
									data.showInKey = CONVERT.ParseBool(att.Value, false);
								}
								else if (att.Name == "use_line_in_key")
								{
									data.useLineInKey = CONVERT.ParseBool(att.Value, false);
								}
								else if (att.Name == "stepped")
								{
									data.Stepped = CONVERT.ParseBool(att.Value, false);
								}
								else if (att.Name == "show_in_highlight_bar")
								{
									data.showInHighlight = CONVERT.ParseBool(att.Value, false);
								}
							}

							//set the colour of the line
							//use the line colour if provided, else use the axis colour
							if (data.yaxis == "left" && YLeftAxisColour != string.Empty)
							{
								ColourToUse = YLeftAxisColour;
							}
							if (data.yaxis == "right" && YRightAxisColour != string.Empty)
							{
								ColourToUse = YRightAxisColour;
							}
							if (DataColour != string.Empty)
							{
								ColourToUse = DataColour;
							}

							string[] parts = ColourToUse.Split(',');
							if (parts.Length == 3)
							{
								int RedFactor = CONVERT.ParseInt(parts[0]);
								int GreenFactor = CONVERT.ParseInt(parts[1]);
								int BlueFactor = CONVERT.ParseInt(parts[2]);
								data.color = Color.FromArgb(RedFactor, GreenFactor, BlueFactor);
							}
							else
							{
								data.color = Color.Black;
							}

							//
							foreach (XmlNode n in child.ChildNodes)
							{
								if (n.NodeType == XmlNodeType.Element)
								{
									if (n.Name == "p")
									{
										DPoint p = new DPoint();
										p.Stepped = data.Stepped;
										//
										foreach (XmlAttribute att in n.Attributes)
										{
											if ("x" == att.Name)
											{
												p.X = CONVERT.ParseDouble(att.Value);
											}
											else if ("y" == att.Name)
											{
												p.Y = CONVERT.ParseDouble(att.Value);
											}
											else if ("dot" == att.Name)
											{
												p.Show = (att.Value == "yes") || CONVERT.ParseBool(att.Value, false);
											}
											else if ("label" == att.Name)
											{
												p.Label = att.Value;
											}
											else if ("stepped" == att.Name)
											{
												p.Stepped = CONVERT.ParseBool(att.Value, false);
											}
										}
										data.data.Add(p);
									}
									else if (n.Name == "annotation")
									{
										Annotation annotation = new Annotation();
										annotation.Alignment = StringAlignment.Center;
										annotation.LineAlignment = StringAlignment.Center;
										annotation.AutoY = false;

										foreach (XmlAttribute attribute in n.Attributes)
										{
											switch (attribute.Name)
											{
												case "x":
													annotation.X = CONVERT.ParseDouble(attribute.Value);
													break;

												case "y":
													if (attribute.Value.ToLower() == "auto")
													{
														annotation.AutoY = true;
													}
													else
													{
														annotation.AutoY = false;
														annotation.Y = CONVERT.ParseDouble(attribute.Value);
													}
													break;

												case "text":
													annotation.Text = attribute.Value;
													break;

												case "align":
													switch (attribute.Value.ToLower())
													{
														case "left":
															annotation.Alignment = StringAlignment.Near;
															break;

														case "center":
															annotation.Alignment = StringAlignment.Center;
															break;

														case "right":
															annotation.Alignment = StringAlignment.Far;
															break;
													}
													break;

												case "line_align":
													switch (attribute.Value.ToLower())
													{
														case "top":
															annotation.LineAlignment = StringAlignment.Near;
															break;

														case "center":
															annotation.LineAlignment = StringAlignment.Center;
															break;

														case "bottom":
															annotation.LineAlignment = StringAlignment.Far;
															break;
													}
													break;

												case "show_only_on_mouseover":
													annotation.ShowOnlyOnMouseover = CONVERT.ParseBool(attribute.Value, false);
													break;
											}
										}

										data.annotations.Add(annotation);
									}
								}
							}

							dataLines.Add(data);

							if (data.yaxis == "left")
							{
								leftAxisData.Add(data);
							}
							else
							{
								rightAxisData.Add(data);
							}
						}
					}

					//now we have the data, auto scale the axes if necessary
					if (xAutoScale)
					{
						AutoScaleData(xAxis, dataLines, "xaxis");
					}

					if (yLeftAutoScale)
					{
						AutoScaleData(leftAxis, leftAxisData, "yaxis");
					}

					if (yRightAutoScale)
					{
						AutoScaleData(rightAxis, rightAxisData, "yaxis");
					}
				}
			}
			catch (Exception evc)
			{
				string sst = evc.Message;
			}
		}

		public void EnableHighlightBar (bool enable)
		{
			enableHighlightBar = enable;

			Invalidate();
		}

		protected override void OnMouseMove (MouseEventArgs e)
		{
			base.OnMouseMove(e);

			if (enableHighlightBar)
			{
				highlightCursorPosition = new Point(e.X, e.Y);
				Invalidate();
			}
		}

		protected override void OnMouseLeave (EventArgs e)
		{
			base.OnMouseLeave(e);

			if (enableHighlightBar)
			{
				highlightCursorPosition = null;
				Invalidate();
			}
		}
		/// <summary>
		/// Given an interval for a graph, rounds it up to the next "nice" figure (ie 10^n * {1, 2, 2.5 or 5}).
		/// </summary>
		public static double RoundToNiceInterval (double start)
		{
			int sign = Math.Sign(start);
			start = Math.Abs(start);
			int exponent = (int) Math.Floor(Math.Log10(start));

			double radix = start / Math.Pow(10, exponent);

			if (radix <= 1)
			{
				radix = 1;
			}
			else if (radix <= 2)
			{
				radix = 2;
			}
			else if (radix <= 2.5)
			{
				radix = 2.5;
			}
			else if (radix <= 5)
			{
				radix = 5;
			}
			else
			{
				radix = 10;
			}

			return sign * radix * Math.Pow(10, exponent);
		}
	}
}