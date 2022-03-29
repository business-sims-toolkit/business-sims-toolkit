using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using System.Xml;

using Algorithms;
using CoreUtils;
using LibCore;
using LibCore.Enums;
using ResizingUi;
// ReSharper disable ParameterHidesMember

namespace Charts
{
	public class LineGraphChart : SharedMouseEventControl
	{
		List<Category.Label> labels;
		readonly List<Category> categories;

		public LineGraphChart (XmlNode xmlRoot)
		{
			categories = new List<Category> ();

			foreach (XmlElement child in xmlRoot.ChildNodes)
			{
				switch (child.Name)
				{
					case "category":
						categories.Add(new Category(child));
						break;
				}
			}
		}

		public override List<KeyValuePair<string, Rectangle>> BoundIdsToRectangles { get; }

		protected override void OnSizeChanged (EventArgs e)
		{
			// TODO yAxis width
			var yAxisWidth = 120;
			// TODO xAxis height
			var xAxisHeight = 50;

			const int legendHeight = 50;
			const int padding = 10;
			var width = Width - 2 * padding;

			yAxisBounds = new RectangleF(padding, legendHeight, yAxisWidth, Height - xAxisHeight - legendHeight);

			xAxisBounds = new RectangleF(yAxisBounds.Right, yAxisBounds.Bottom, width - yAxisWidth - 40, xAxisHeight);

			graphBounds = new RectangleFFromBounds
			{
				Left = yAxisBounds.Right,
				Right = xAxisBounds.Right,
				Top = legendHeight + padding,
				Bottom = xAxisBounds.Top
			}.ToRectangleF();

			legendBounds = new RectangleFFromBounds
			{
				Left = 10,
				Right = Width,
				Top = 0,
				Bottom = graphBounds.Top - 5
			}.ToRectangleF();

			foreach (var category in categories)
			{
				category.UpdateBounds(yAxisBounds, xAxisBounds, graphBounds, legendBounds);
			}

			Invalidate();
		}

		protected override void OnMouseLeave (EventArgs e)
		{
			base.OnMouseLeave(e);
			mousePosition = null;
			Invalidate();
		}

		Point? mousePosition;

		protected override void OnMouseMove (MouseEventArgs e)
		{
			base.OnMouseMove(e);
			mousePosition = e.Location;
			Invalidate();
		}

		protected override void OnPaint (PaintEventArgs e)
		{
			e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
			labels = new List<Category.Label> ();

			foreach (var category in categories)
			{
				category.Render(e.Graphics, ClientSize, labels);
			}

			if ((mousePosition != null) && (labels != null) && (labels.Count > 0))
			{
				var thresholdDistance = 20;
				var labelsInRange = labels.Where(l => (((mousePosition.Value.X - l.X) * (mousePosition.Value.X - l.X)) +
				                                       ((mousePosition.Value.Y - l.Y) * (mousePosition.Value.Y - l.Y)))
				                                      <= (thresholdDistance * thresholdDistance)).ToList();

				if (labelsInRange.Count > 0)
				{
					RenderLabels(e.Graphics, labelsInRange);
				}
			}
		}

		void RenderLabels (Graphics graphics, List<Category.Label> labelsInRange)
		{
			using (var font = SkinningDefs.TheInstance.GetFont(20))
			{
				var labelToSize = labelsInRange.ToDictionary(l => l, l => graphics.MeasureString(l.Text, font).ExpandByAmount(10));
				var gap = 20;
				var totalWidth = labelToSize.Values.Sum(l => l.Width) + ((labelsInRange.Count - 1) * gap);

				var startX = mousePosition.Value.X - (totalWidth / 2);
				var x = startX;
				var y = mousePosition.Value.Y - 100;
				foreach (var label in labelsInRange)
				{
					if ((x + labelToSize[label].Width) >= Width)
					{
						x = startX;
						y -= (int) (labelToSize[label].Height * 1.5f);
					}

					var labelBounds = new Rectangle(Math.Max(0, (int) (x - (labelToSize[label].Width / 2))), (int) (y - (labelToSize[label].Height / 2)), (int) labelToSize[label].Width, (int) labelToSize[label].Height);

					graphics.FillRectangle(Brushes.White, labelBounds);
					graphics.DrawRectangle(Pens.Black, labelBounds);
					graphics.DrawString(label.Text, font, Brushes.Black, labelBounds,
						new StringFormat
						{
							Alignment = StringAlignment.Center,
							LineAlignment = StringAlignment.Near,
							FormatFlags = StringFormatFlags.NoClip | StringFormatFlags.NoWrap
						});
					graphics.DrawLine(Pens.Black, (labelBounds.Left + labelBounds.Right) / 2, labelBounds.Bottom, label.X, label.Y);

					x = labelBounds.Right + (labelBounds.Width / 2) + gap;
				}
			}
		}

		RectangleF yAxisBounds;
		RectangleF xAxisBounds;
		RectangleF graphBounds;
		RectangleF legendBounds;

		class Axis
		{
			public Axis (XmlElement node, Side graphSide)
			{
				this.graphSide = graphSide;

				title = BasicXmlDocument.GetStringAttribute(node, "title");
				
				if (Enum.TryParse(BasicXmlDocument.GetStringAttribute(node, "title_orientation"),
					true, out titleOrientation))
				{
					titleOrientation = Orientation.Horizontal;
				}
				

				Minimum = BasicXmlDocument.GetFloatAttribute(node, "min", 0);
				Maximum = BasicXmlDocument.GetFloatAttribute(node, "max", 100);

				interval = BasicXmlDocument.GetFloatAttribute(node, "interval", 1);
				showTicks = BasicXmlDocument.GetBoolAttribute(node, "show_ticks", true);

				numberFormatting = BasicXmlDocument.GetEnumAttribute(node, "number_formatting", NumberFormatting.None);
			}

			public void UpdateLimits (float minimum, float maximum)
			{
				Minimum = Math.Min(minimum, maximum);
				Maximum = (float)Maths.RoundToNiceInterval(Math.Max(minimum, maximum));

				// TODO recalculate the interval ??
				interval = (float) Maths.RoundToNiceInterval(Maximum / 10f);

				CalculateIntervalSpacing();
			}
			
			public float Minimum { get; private set; }
			public float Maximum { get; private set; }

			// TODO if this handles its own drawing then
			// properties below need not be public

			float interval;

			public float IntervalSpacing { get; private set; }

			public float IntervalSpacingInNativeRange => interval;

			readonly bool showTicks;

			readonly NumberFormatting numberFormatting;

			public RectangleF RenderedBounds { get; private set; }

			enum Orientation
			{
				Horizontal,
				Vertical
			}

			public enum Side
			{
				Left,
				Right,
				Top,
				Bottom
			}

			RectangleF bounds;

			public RectangleF Bounds
			{
				get => bounds;

				set
				{
					bounds = value;
					CalculateIntervalSpacing();
				}
			}

			void CalculateIntervalSpacing ()
			{
				var intervalFrequency = (Maximum - Minimum) / interval;
				switch (graphSide)
				{
					case Side.Left:
					case Side.Right:
						IntervalSpacing = bounds.Height / intervalFrequency;
						break;
					case Side.Top:
					case Side.Bottom:
						IntervalSpacing = bounds.Width / intervalFrequency;
						break;
				}
			}
			
			public void Render (Graphics graphics)
			{
				var intervalFrequency = (Maximum - Minimum) / interval;

				var axisLineStartX = 0f;
				var axisLineStartY = 0f;
				var axisLineEndX = 0f;
				var axisLineEndY = 0f;

				var lineWidth = 1;
				// What side of the graph is the axis on?
				// If left then draw the axis line down the
				// right side of the given bounds, for example.
				switch (graphSide)
				{
					case Side.Left:
						axisLineStartX = axisLineEndX = bounds.Right - lineWidth /2f;
						axisLineStartY = bounds.Top;
						axisLineEndY = bounds.Bottom + lineWidth;
						break;

					case Side.Right:
						axisLineStartX = axisLineEndX = bounds.Left + lineWidth / 2f;
						axisLineStartY = bounds.Top;
						axisLineEndY = bounds.Bottom;
						break;

					case Side.Bottom:
						axisLineStartX = bounds.Left - lineWidth * 0.5f;
						axisLineEndX = bounds.Right;
						axisLineStartY = axisLineEndY = bounds.Top + lineWidth / 2f;
						break;

					case Side.Top:
						axisLineStartX = bounds.Left;
						axisLineEndX = bounds.Right;
						axisLineStartY = axisLineEndY = bounds.Bottom - lineWidth / 2f;
						break;
				}

				// TODO border colour and line width
				using (var markingPen = new Pen(Color.Black, lineWidth))
				using (var font = SkinningDefs.TheInstance.GetFont(12f))
				{
					RenderedBounds = new RectangleF (axisLineStartX, axisLineStartY, axisLineEndX - axisLineStartX, axisLineEndY - axisLineStartY);
					graphics.DrawLine(markingPen, axisLineStartX, axisLineStartY, axisLineEndX, axisLineEndY);

					var tickValue = Minimum;
					for (var i = 0; i <= intervalFrequency; i++)
					{
						float tickX = 0;
						float tickY = 0;

						float tickStartX = 0;
						float tickStartY = 0;
						float tickEndX = 0;
						float tickEndY = 0;

						var alignment = ContentAlignment.MiddleCenter;

						switch (graphSide)
						{
							case Side.Left:
								tickY = bounds.Bottom - IntervalSpacing * i;

								tickStartX = bounds.Left + bounds.Width * 0.66f;
								tickEndX = bounds.Right;

								tickStartY = tickEndY = tickY;

								tickX = tickStartX;
								alignment = ContentAlignment.MiddleRight;
								break;

							case Side.Right:
								alignment = ContentAlignment.MiddleLeft;
								break;

							case Side.Bottom:
								tickX = bounds.Left + IntervalSpacing * i;
								tickY = bounds.Top + bounds.Height * 0.5f;

								tickStartX = tickEndX = tickX;

								tickStartY = bounds.Top;
								tickEndY = bounds.Top + bounds.Height * 0.33f;
								alignment = ContentAlignment.TopCenter;
								break;

							case Side.Top:
								alignment = ContentAlignment.BottomCenter;
								break;
						}

						var text = FormatValue(tickValue);
						graphics.DrawString(text, font, Brushes.Black, 
							new PointF(tickX, tickY), new StringFormat
						{
								LineAlignment = Alignment.GetVerticalAlignment(alignment),
								Alignment = Alignment.GetHorizontalAlignment(alignment)
						});

						var size = graphics.MeasureString(text, font);

						RenderedBounds = RenderedBounds.ExtendToInclude(new RectangleF (tickX, tickY, size.Width, size.Height));

						tickValue += interval;

						graphics.DrawLine(markingPen, new PointF(tickStartX, tickStartY), new PointF(tickEndX, tickEndY) );
					}
				}
			}

			string FormatValue (float value)
			{
				switch (numberFormatting)
				{
					case NumberFormatting.PaddedThousands:
						return CONVERT.ToPaddedStrWithThousands(value, 0);
					default:
						return $"{value}";
				}
			}

			readonly string title;
			readonly Side graphSide;
			readonly Orientation titleOrientation;
		}

		struct Entry
		{
			public string Title { get; set; }
			public float HorizontalValue { get; set; }
			public float VerticalValue { get; set; }
			public bool ShowTitle { get; set; }
			public bool ShowControlPoint { get; set; }
			public bool IgnoreFiltering { get; set; }
			public string FilterName { get; set; }
			public bool LineEndPoint { get; set; }

			public string DeltaImageFilename { get; set; }
			public bool ShowDeltaImage { get; set; }

			public override string ToString () => $"({HorizontalValue}, {VerticalValue})";
		}

		class Series
		{
			public Series (XmlElement node)
			{
				Title = BasicXmlDocument.GetStringAttribute(node, "title");
				Colour = BasicXmlDocument.GetColourAttribute(node, "colour", Color.HotPink);
				FillColour = BasicXmlDocument.GetColourAttribute(node, "fill_colour", Color.FromArgb(125, Colour.Tint(0.45f)));

				entries = node.ChildNodes.Cast<XmlElement>().Where(e => e.Name == "entry").Select(e => new Entry
				{
					Title = BasicXmlDocument.GetStringAttribute(e, "title"),
					HorizontalValue = BasicXmlDocument.GetFloatAttribute(e, "horizontal_value", 0),
					VerticalValue = BasicXmlDocument.GetFloatAttribute(e, "vertical_value", 0),
					ShowTitle = BasicXmlDocument.GetBoolAttribute(e, "show_title", false),
					ShowControlPoint = BasicXmlDocument.GetBoolAttribute(e, "show_control_point", false),
					IgnoreFiltering = BasicXmlDocument.GetBoolAttribute(e, "ignore_filtering", false),
					FilterName = BasicXmlDocument.GetStringAttribute(e, "filter_name", BasicXmlDocument.GetStringAttribute(e, "title")),
					LineEndPoint = BasicXmlDocument.GetBoolAttribute(e, "line_end_point", false),
					DeltaImageFilename = BasicXmlDocument.GetStringAttribute(e, "delta_image"),
					ShowDeltaImage = BasicXmlDocument.GetBoolAttribute(e, "show_delta_image", true),
				}).ToList();

				if (!Enum.TryParse(BasicXmlDocument.GetStringAttribute(node, "line_style"), out lineStyle))
				{
					lineStyle = DashStyle.Solid;
				}

				Order = BasicXmlDocument.GetIntAttribute(node, "order", 0);
				LineWidth = BasicXmlDocument.GetFloatAttribute(node, "line_width", 4);
				FillArea = BasicXmlDocument.GetBoolAttribute(node, "fill_area", false);

				IncludeStepEntries = BasicXmlDocument.GetBoolAttribute(node, "include_steps", false);

				if (! Enum.TryParse(
					BasicXmlDocument.GetStringAttribute(node, "step_coordinate_order"), true, out stepCoordinateOrder))
				{
					stepCoordinateOrder = StepCoordinateOrder.YThenX;
				}

				sumVerticalEntries = BasicXmlDocument.GetBoolAttribute(node, "sum_vertical_values", false);
			}
			
			public string Title { get; }
			public Color Colour { get; }
			public Color FillColour { get; }
			
			public DashStyle LineStyle => lineStyle;

			readonly List<Entry> entries;
			public int Order { get; }
			public float LineWidth { get; }
			public bool FillArea { get; }

			public bool IncludeStepEntries { get; }
			
			public StepCoordinateOrder CoordinateOrder => stepCoordinateOrder;

			public List<Entry> GetEntries ()
			{
				var useEntries = entries;

				if (sumVerticalEntries)
				{
					var runningEntries = new List<Entry>();

					var runningValue = 0.0f;

					foreach (var entry in entries)
					{
						runningValue += entry.VerticalValue;

						var runningEntry = entry;
						runningEntry.VerticalValue = runningValue;

						runningEntries.Add(runningEntry);
					}

					useEntries = runningEntries;
				}

				return useEntries;
			}
			
			public enum StepCoordinateOrder
			{
				// Takes the Y of the previous point
				// and the X of the next point 
				//      |
				//      |
				// _____|
				// 
				YThenX,
				// Takes the X of the previous point
				// and the Y of the next point
				//  ______
				// |
				// |
				// |
				XThenY
			}

			readonly StepCoordinateOrder stepCoordinateOrder;
			readonly DashStyle lineStyle;
			readonly bool sumVerticalEntries;
		}

		class Category
		{
			public string Title { get; }

			readonly Axis horizontalAxis;
			readonly Axis verticalAxis;
			readonly float defaultVerticalAxisMax;

			readonly List<Series> series;

			readonly bool isEmpty;

			RectangleF graphBounds;
			RectangleF legendBounds;

			public Category (XmlNode node)
			{
				Title = BasicXmlDocument.GetStringAttribute(node, "title", "*MISSING*");

				series = new List<Series>();

				foreach (XmlElement child in node.ChildNodes)
				{
					switch (child.Name)
					{
						case "x_axis":
							horizontalAxis = new Axis(child, Axis.Side.Bottom);
							break;
						case "y_axis":
							verticalAxis = new Axis(child, Axis.Side.Left);
							defaultVerticalAxisMax = verticalAxis.Maximum;
							break;
						case "content":
							foreach (XmlElement grandchild in child.ChildNodes)
							{
								switch (grandchild.Name)
								{
									case "series":
										series.Add(new Series(grandchild));
										break;
								}
							}
							break;
					}
				}

				isEmpty = horizontalAxis == null || verticalAxis == null || ! series.Any();
			}


			public void UpdateBounds (RectangleF yAxisBounds, RectangleF xAxisBounds, RectangleF graphBounds,
			                          RectangleF legendBounds)
			{
				if (isEmpty)
				{
					return;
				}

				this.graphBounds = graphBounds;
				this.legendBounds = legendBounds;
				verticalAxis.Bounds = yAxisBounds;
				horizontalAxis.Bounds = xAxisBounds;
			}

			void RenderLegend (Graphics graphics)
			{
				using (var legendFont = SkinningDefs.TheInstance.GetFont(12f))
				{
					const float legendLineLength = 25f;
					const float legendLineWidth = 1f;

					var legends = series.OrderBy(s => s.Order).Reverse().Select(s => new
					{
						s.Title,
						s.Colour,
						s.LineStyle
					}).ToList();

					var maxTextWidth = legends.Select(l => graphics.MeasureString(l.Title, legendFont).Width).Max();

					var legendWidth = maxTextWidth + 20 + legendLineLength;
					var legendHeight = Math.Max(legendBounds.Height * 0.5f, 20);

					var x = legendBounds.X;
					foreach (var legend in legends)
					{
						var labelBounds = new RectangleF(x, legendBounds.Top, maxTextWidth, legendHeight);
						var lineY = (labelBounds.Top + labelBounds.Bottom) / 2;

						graphics.DrawString(legend.Title, legendFont, Brushes.Black, labelBounds, new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center });

						var lineStartX = x + maxTextWidth + 10;
						using (var legendPen = new Pen (legend.Colour, legendLineWidth))
						{
							legendPen.DashStyle = legend.LineStyle;
							graphics.DrawLine(legendPen, lineStartX, lineY, lineStartX + legendLineLength, lineY);

							var circleRadius = legendHeight / 4;
							var circleBounds = new Rectangle ((int) (lineStartX + legendLineLength), (int) (lineY - circleRadius), (int) (2 * circleRadius), (int) (2 * circleRadius));

							if (legendPen.DashStyle == DashStyle.Solid)
							{
								using (var brush = new SolidBrush (legendPen.Color))
								{
									graphics.FillEllipse(brush, circleBounds);
								}
							}
							else
							{
								graphics.DrawEllipse(legendPen, circleBounds);
							}
						}

						x += (legendWidth * 6 / 4);
					}
				}
			}

			void RenderBackground (Graphics graphics)
			{
				using (var pen = new Pen(Color.LightGray, 1) { DashStyle = DashStyle.Dash })
				{
					for (var y = verticalAxis.Maximum; y >= verticalAxis.Minimum; y -= verticalAxis.IntervalSpacingInNativeRange)
					{
						var screenY = (int) Maths.MapBetweenRanges(y,
							verticalAxis.Minimum, verticalAxis.Maximum,
							verticalAxis.Bounds.Bottom, verticalAxis.Bounds.Top);

						graphics.DrawLine(pen, graphBounds.Left, screenY, graphBounds.Right, screenY);
					}
				}
			}

			PointF MapEntryToGraph (Entry entry, Axis xAxis, Axis yAxis, RectangleF graphBounds)
			{
				return new PointF(
					(float) Maths.MapBetweenRanges(entry.HorizontalValue, horizontalAxis.Minimum, horizontalAxis.Maximum,
						graphBounds.Left, graphBounds.Right),
					(float) Maths.MapBetweenRanges(entry.VerticalValue, verticalAxis.Minimum, verticalAxis.Maximum, graphBounds.Bottom,
						graphBounds.Top));
			}

			internal class Label
			{
				public string Text;
				public int X;
				public int Y;

				public SizeF CalculateSize (Graphics graphics, Font font)
				{
					return graphics.MeasureString(Text, font);
				}

				public float CalculateLeft (Graphics graphics, Font font)
				{
					return X - (CalculateSize(graphics, font).Width / 2);
				}

				public override string ToString ()
				{
					return "\"" + Text + "\"" + $" ({X}, {Y})";
				}
			}

			public void Render (Graphics graphics, Size size, List<Label> labels)
			{
				if (isEmpty)
				{
					return;
				}

				RenderLegend(graphics);

				var seriesOrder = series.OrderBy(s => s.Order)
					.ThenBy(s => s.Title).ToList();

				var seriesToFilteredEntries = series.ToDictionary(s => s.Title, s => s.GetEntries());

				var filteredMax = seriesToFilteredEntries
					.SelectMany(kvp => kvp.Value.Select(e => e.VerticalValue)).Max();

				verticalAxis.UpdateLimits(0, Math.Max(defaultVerticalAxisMax, filteredMax));

				RenderBackground(graphics);

				foreach (var s in seriesOrder)
				{
					var filteredEntries = seriesToFilteredEntries[s.Title];

					using (var seriesBrush = new SolidBrush(s.Colour))
					using (var linePen = new Pen(seriesBrush, s.LineWidth)
					{
						DashStyle = s.LineStyle
					})
					{
						var linePoints = new List<PointF>();
						if (s.IncludeStepEntries)
						{
							for (var i = 0; i < filteredEntries.Count; i++)
							{
								var entry = filteredEntries[i];
								var point = MapEntryToGraph(entry, horizontalAxis, verticalAxis, graphBounds);

								linePoints.Add(point);

								var nextEntry = i < filteredEntries.Count - 1 ? filteredEntries[i + 1] : (Entry?) null;

								if (nextEntry.HasValue)
								{
									var nextPoint = MapEntryToGraph(nextEntry.Value, horizontalAxis, verticalAxis, graphBounds);

									var stepX = point.X;
									var stepY = point.Y;

									switch (s.CoordinateOrder)
									{
										case Series.StepCoordinateOrder.YThenX:
											stepX = nextPoint.X;
											stepY = point.Y;
											break;
										case Series.StepCoordinateOrder.XThenY:
											stepX = point.X;
											stepY = nextPoint.Y;
											break;
									}

									linePoints.Add(new PointF(stepX, stepY));
								}

								if (nextEntry?.LineEndPoint ?? false)
								{
									break;
								}
							}
						}
						else
						{
							linePoints = filteredEntries.Select(e => MapEntryToGraph(e, horizontalAxis, verticalAxis, graphBounds)).ToList();
						}


						if (s.FillArea)
						{
							var areaPoints = new List<PointF>(linePoints);

							var maxX = areaPoints.Max(p => p.X);
							var minX = areaPoints.Min(p => p.X);

							areaPoints.Add(new PointF(maxX, graphBounds.Bottom));
							areaPoints.Add(new PointF(minX, graphBounds.Bottom));

							using (var fillBrush = new SolidBrush(s.FillColour))
							{
								graphics.FillPolygon(fillBrush, areaPoints.ToArray());
							}
						}

						if (linePoints.Count > 0)
						{
							graphics.DrawLines(linePen, linePoints.ToArray());
						}

						foreach (var entry in filteredEntries)
						{
							var point = MapEntryToGraph(entry, horizontalAxis, verticalAxis, graphBounds);

							if (entry.ShowControlPoint)
							{
								const float circleWidth = 8;
								var ellipseBounds = new RectangleF (point.X - circleWidth / 2, point.Y - circleWidth / 2, circleWidth, circleWidth);

								if (linePen.DashStyle == DashStyle.Solid)
								{
									graphics.FillEllipse(seriesBrush, ellipseBounds);
								}
								else
								{
									using (var circlePen = new Pen(linePen.Color, linePen.Width))
									{
										graphics.DrawEllipse(linePen, ellipseBounds);
									}
								}
							}

							if (entry.ShowTitle)
							{
								labels.Add(new Label { X = (int) point.X, Y = (int) point.Y, Text = entry.Title });
							}

							if (! string.IsNullOrEmpty(entry.DeltaImageFilename) && entry.ShowDeltaImage)
							{
								var image = Repository.TheInstance.GetImage(
									AppInfo.TheInstance.Location + $@"\images\chart\{entry.DeltaImageFilename}.png");

								const float imageSize = 20;

								var imageX = point.X - imageSize - 10;
								var imageY = point.Y - imageSize / 2f;

								graphics.DrawImage(image, new RectangleF(imageX, imageY, imageSize, imageSize));
							}
						}
					}
				}

				verticalAxis.Render(graphics);
				horizontalAxis.Render(graphics);
			}
		}
	}
}