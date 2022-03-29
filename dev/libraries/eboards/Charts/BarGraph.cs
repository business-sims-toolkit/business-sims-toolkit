using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

using LibCore;

namespace Charts
{
	/// <summary>
	/// Summary description for BarGraph.
	/// </summary>
	public class BarGraph : Chart
	{
		class PowerBarPanel : VisualPanel
		{
			BarGraph graph;
			PowerBar bar;

			public PowerBarPanel (BarGraph graph, PowerBar bar)
			{
				this.graph = graph;
				this.bar = bar;
			}

			protected void DrawLineBy (Graphics graphics, Pen pen, int x, int y, int w, int h)
			{
				graphics.DrawLine(pen, x, y, x + w, y + h);
			}

			protected override void OnPaint (PaintEventArgs e)
			{
				base.OnPaint(e);

				double outlineFraction = 0.8;
				double barFraction = 0.35;

				// Grid (as the main graph's grid is mostly obscured by us).
				for (int y = graph.leftAxis.Min; y <= graph.leftAxis.Max; y += ((graph.leftAxis.Max - graph.leftAxis.Min) / graph.leftAxis.Steps))
				{
					int h = graph.xAxis.Top - graph.TopOffset;
					int ourY = (int) (h - ((y - graph.leftAxis.Min) * (double) h / (graph.leftAxis.Max - graph.leftAxis.Min)));
					e.Graphics.DrawLine(Pens.LightSeaGreen, 0, ourY, Width - 1, ourY);
				}

				// First bar.
				using (Brush brush = new SolidBrush (bar.firstColour))
				{
					int w = (int) (barFraction * Width);
					int h = (int) ((bar.firstHeight - graph.leftAxis.Min) * 1.0 / (graph.leftAxis.Max - graph.leftAxis.Min) * Height);
					e.Graphics.FillRectangle(brush, (int) ((0.5 * Width) - w), Height - h, w, h);
					e.Graphics.DrawRectangle(Pens.Black, (int) ((0.5 * Width) - w), Height - h, w, h);
				}

				// Second bar.
				using (Brush brush = new SolidBrush (bar.secondColour))
				{
					int w = (int) (barFraction * Width);
					int h = (int) ((bar.secondHeight - graph.leftAxis.Min) * 1.0 / (graph.leftAxis.Max - graph.leftAxis.Min) * Height);
					e.Graphics.FillRectangle(brush, (int) (0.5 * Width), Height - h, w, h);
					e.Graphics.DrawRectangle(Pens.Black, (int) (0.5 * Width), Height - h, w, h);
				}

				// Outline.
				using (Pen pen = new Pen (bar.outlineColour, 3))
				{
					int w = (int) (outlineFraction * Width);
					int h = (int) ((bar.outlineHeight - graph.leftAxis.Min) * 1.0 / (graph.leftAxis.Max - graph.leftAxis.Min) * Height);

					if (bar.drawTop)
					{
						DrawLineBy(e.Graphics, pen, (int) (0.5 * Width) - (w / 2), Height - h, w, 0);
					}
					if (bar.drawBottom)
					{
						DrawLineBy(e.Graphics, pen, (int) (0.5 * Width) - (w / 2), Height, w, 0);
					}
					if (bar.drawLeft)
					{
						DrawLineBy(e.Graphics, pen, (int) (0.5 * Width) - (w / 2), Height, 0, h);
					}
					if (bar.drawRight)
					{
						DrawLineBy(e.Graphics, pen, (int) (0.5 * Width) + (w / 2), Height, 0, h);
					}
				}
			}
		}

		class TopKeyElement
		{
			public Color colour;
			public Color outlineColour;

			public bool drawTop = true;
			public bool drawBottom = true;
			public bool drawLeft = true;
			public bool drawRight = true;

			public int rectWidth = 20, rectHeight = 20;

			public string text;

			public TopKeyElement ()
			{
				colour = Color.Transparent;
				outlineColour = Color.Transparent;
				text = "";
			}
		}

		class Band
		{
			public bool fade = false;
			public int bottom = 0;
			public int top = 0;
			public string title = "";
			public Color color = Color.Red;
			public Color title_color = Color.Red;
		}

		protected ArrayList topKeyElements = new ArrayList ();
		protected bool draw_key = true;
		protected bool draw_bar_values = true;

		class Bar
		{
			public Color colour;
			public int height;
			public string title;
			public string logo;
		}

		class PowerBar : Bar
		{
			public Color firstColour, secondColour, outlineColour;
			public int firstHeight, secondHeight, outlineHeight;
			public bool drawTop = true, drawBottom = true, drawLeft = true, drawRight = true;
		}

		public LeftYAxis leftAxis;
		public XAxis xAxis;

		public int TopOffset = 40;

		protected ArrayList BarArray = new ArrayList();
		protected ArrayList BarPanels = new ArrayList();
		protected ArrayList BarLabels = new ArrayList();

		protected ArrayList Bands = new ArrayList();
		//protected ArrayList BandLabels = new ArrayList();

		protected double rowLength = 25;

		public BarGraph()
		{
			this.Font = CoreUtils.SkinningDefs.TheInstance.GetFont(10); //, FontStyle.Bold );
			leftAxis = new LeftYAxis();
			xAxis = new XAxis();
			//
			this.AutoScroll = false;

			this.SuspendLayout();
			Controls.Add(xAxis);
			Controls.Add(leftAxis);
			this.ResumeLayout(false);

			Resize += BarGraph_Resize;
			//
			leftAxis.SetRange(0,10,10);
			xAxis.SetRange(0,6,6);

			DoSize();
		}

		public bool XAxisMarked
		{
			set
			{
				xAxis.Marked = value;
			}
		}

		void BarGraph_Resize(object sender, EventArgs e)
		{
			DoSize();
			this.Invalidate();
		}

		protected override void OnPaint( PaintEventArgs e )
		{
			base.OnPaint(e);

			//int xl = leftAxis.Width;
			int xl = xAxis.Left;
			int yl = xAxis.Top;

			//
			ChartUtils.DrawGrid(e,this,Color.LightSeaGreen,xl,TopOffset,xAxis.Width,yl-TopOffset, xAxis.Max, leftAxis.Steps);

			foreach(Control c in Controls)
			{
				if (c.GetType().ToString() == "LibCore.VisualPanel")
				{
					VisualPanel vp = (VisualPanel)c;
					RectangleF gradRect = vp.Bounds;
					gradRect.Inflate(0, 10);
					LinearGradientBrush lBrush = new LinearGradientBrush(gradRect, vp.BackColor, ChartUtils.DarkerColor(vp.BackColor), LinearGradientMode.Horizontal); 
					e.Graphics.FillRectangle(lBrush, vp.Bounds);

					if (vp.BackgroundImage != null)
					{
						int x = vp.Left + ((vp.Width/2)-(vp.Width/4));
						int y = vp.Top + ((vp.Height/2)-(35));
						e.Graphics.DrawImage(vp.BackgroundImage, x, y, vp.Width/2,70);
					}
				}
			}

			// Draw any bands that we have.
			Color bottom_colour = Color.FromArgb(0,255,255,255);
			double yrange = leftAxis.Max-leftAxis.Min;
			//
			foreach(Band band in Bands)
			{
				int tt = xAxis.Top - (int)( leftAxis.Height*((band.top)-leftAxis.Min)/yrange );
				int bb = xAxis.Top - (int)( leftAxis.Height*((band.bottom)-leftAxis.Min)/yrange );

				System.Drawing.Rectangle gradRect = new Rectangle(xl, tt, xAxis.Width, bb-tt);
				LinearGradientBrush lBrush;

				if ((gradRect.Height>0)&(gradRect.Width>0))
				{
					if(band.fade)
					{
						lBrush = new LinearGradientBrush(gradRect, band.color, bottom_colour, LinearGradientMode.Vertical); 
					}
					else
					{
						lBrush = new LinearGradientBrush(gradRect, band.color, band.color, LinearGradientMode.Vertical); 
					}
					e.Graphics.FillRectangle(lBrush, gradRect);
				}

				// Draw the heading for the band...
				//double yrange = leftAxis.Max-leftAxis.Min;
				int y = xAxis.Top - (int)( leftAxis.Height*((band.top)-leftAxis.Min)/yrange ) - 20;
				//label.Location = new Point(xAxis.Left+20, y-20);
				System.Drawing.SizeF ssize = e.Graphics.MeasureString(band.title, this.Font);
				e.Graphics.FillRectangle(new SolidBrush( Color.FromArgb(130, 255,255,255) ), xAxis.Left+20,y, ssize.Width, ssize.Height);
				e.Graphics.DrawString( band.title, this.Font, new SolidBrush(band.title_color), xAxis.Left+20, y);
			}
			
			DrawKey(e.Graphics);

			// Draw the top key too.
			for (int i = 0; i < topKeyElements.Count; i++)
			{
				TopKeyElement key = topKeyElements[i] as TopKeyElement;

				int w = Width / topKeyElements.Count;

				Rectangle rect = new Rectangle (w * i, 0, w, TopOffset);
				e.Graphics.FillRectangle (Brushes.White, rect);

				Rectangle keyRect = new Rectangle (rect.Left, rect.Top + ((rect.Height - key.rectHeight) / 2), key.rectWidth, key.rectHeight);
				if (key.colour != Color.Transparent)
				{
					using (Brush brush = new SolidBrush (key.colour))
					{
						e.Graphics.FillRectangle(brush, keyRect);
					}
				}
				if (key.outlineColour != Color.Transparent)
				{
					using (Pen pen = new Pen (key.outlineColour, 2))
					{
						if (key.drawTop)
						{
							e.Graphics.DrawLine(pen, keyRect.Left, keyRect.Top, keyRect.Right, keyRect.Top);
						}

						if (key.drawBottom)
						{
							e.Graphics.DrawLine(pen, keyRect.Left, keyRect.Bottom, keyRect.Right, keyRect.Bottom);
						}

						if (key.drawLeft)
						{
							e.Graphics.DrawLine(pen, keyRect.Left, keyRect.Top, keyRect.Left, keyRect.Bottom);
						}

						if (key.drawRight)
						{
							e.Graphics.DrawLine(pen, keyRect.Right, keyRect.Top, keyRect.Right, keyRect.Bottom);
						}
					}
				}

				SizeF size = e.Graphics.MeasureString(key.text, this.Font);
				e.Graphics.DrawString(key.text, this.Font, Brushes.Black, keyRect.Right + key.rectWidth, rect.Top + ((rect.Height - (int) size.Height) / 2));
			}
		}

		protected void DoSize()
		{
			this.SuspendLayout();

			//set size and location of axes
			leftAxis.Location = new Point(0,TopOffset);
			leftAxis.Size = new Size(100,Height-TopOffset-40);
			//
			xAxis.Size = new Size(Width-leftAxis.Width-80+4,40);
			xAxis.Location = new Point(leftAxis.Width-2,Height-xAxis.Height);

			// Position band labels
			/*
			for(int i=0; i<Bands.Count; ++i)
			{
				Band band = (Band) Bands[i];
				PrintLabel label = (PrintLabel) BandLabels[i];
				double yrange = leftAxis.Max-leftAxis.Min;
				int y = xAxis.Top - (int)( leftAxis.Height*((band.top)-leftAxis.Min)/yrange );

				label.Location = new Point(xAxis.Left+20, y-20);
				label.Size = new Size(120,15);
			}*/

			//set size of any bar controls
			for (int i=0; i<BarPanels.Count; i++)
			{
				VisualPanel vp = (VisualPanel)BarPanels[i];
				Bar bar = (Bar)BarArray[i];

				double BarWidth = ((double)xAxis.Width) / ((double)BarPanels.Count);
				
				//work out top left corner of panel
				double yrange = leftAxis.Max-leftAxis.Min;

				int x = xAxis.Left + (int)( ((double)i) * BarWidth);// + 2;
				int y = xAxis.Top - (int)( leftAxis.Height*((bar.height)-leftAxis.Min)/yrange );

				int barHeight = (xAxis.Top - y) + 5;

				if (BarLabels.Count > i)
				{
					if (BarLabels[i] != null)
					{
						PrintLabel label = (PrintLabel) BarLabels[i];
						if(draw_bar_values)
						{
							label.Text = CONVERT.ToStr(bar.height);
						}
						else
						{
							label.Visible = false;
						}

						//set label showing num points
                        label.TextAlign = ContentAlignment.MiddleCenter;
                        //
						label.Location = new Point(x+(int)(BarWidth/2-60/2), y-20);
						label.Size = new Size(60,15);
						this.Controls.Add(label);
					}
				}

				//set size and location of bar
				vp.Location = new Point(x,y);
				vp.Size = new Size((int)BarWidth - 4, barHeight);


				//show the company logo
				if (bar.logo != string.Empty)
				{
					vp.BackgroundImage = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + 
						"\\images\\" + bar.logo);
				}
			}

			this.ResumeLayout(false);
		}

		protected void DefineAxis (Axis axis, XmlNode n, out bool autoScale)
		{
			autoScale = false;
			char[] sep = { ',' };

			foreach(XmlAttribute att in n.Attributes)
			{
				if("title" == att.Name)
				{
					axis.axisTitle.Text = att.Value;
				}
				else if("minMaxSteps" == att.Name)
				{
					string[] data = att.Value.Split(sep);
					axis.SetRange(CONVERT.ParseInt(data[0]), CONVERT.ParseInt(data[1]), CONVERT.ParseInt(data[2]));
				}
				else if("colour" == att.Name)
				{

					string[] parts = att.Value.Split(',');
					if (parts.Length==3)
					{
						int RedFactor = CONVERT.ParseInt(parts[0]);
						int GreenFactor = CONVERT.ParseInt(parts[1]);
						int BlueFactor = CONVERT.ParseInt(parts[2]);
						axis.SetColour( Color.FromArgb(RedFactor,GreenFactor,BlueFactor) );
					}					
				}
				else if(att.Name == "labels")
				{
					string[] labels = att.Value.Split(',');
					//			axis.SetLabels(labels);
				}
				else if (att.Name == "offset")
				{
					XAxis xaxis = axis as XAxis;

					if (xaxis != null)
					{
						xaxis.ShiftLabels = (att.Value == "true");
					}
				}
				else if (att.Name == "autoScale")
				{
					autoScale = CONVERT.ParseBool(att.Value, false);
				}
			}
		}

		void AddBarControls()
		{
			this.SuspendLayout();

			foreach (Bar bar in BarArray)
			{
				if (bar is PowerBar)
				{
					PowerBar pb = bar as PowerBar;

					PowerBarPanel panel = new PowerBarPanel (this, pb);

					panel.BorderStyle = BorderStyle.None;
					panel.BackColor = Color.White;

					BarPanels.Add(panel);
					Controls.Add(panel);
				}
				else
				{
					VisualPanel vp = new VisualPanel();

					vp.BorderStyle = BorderStyle.FixedSingle;
					vp.BackColor = bar.colour;
					vp.Visible = false;

					PrintLabel label = new PrintLabel();
					if(draw_bar_values)
					{
						label.Text = CONVERT.ToStr(bar.height);
					}
					else
					{
						label.Text = "";
					}
					BarLabels.Add(label);
					
					BarPanels.Add(vp);
					Controls.Add(vp);
				}
			}

			this.ResumeLayout(false);

			DoSize();
		}

		void DrawKey(Graphics g)
		{
			if(!draw_key) return;

			if (BarArray.Count > 0)
			{
				Font font = ConstantSizeFont.NewFont("Tahoma", 8f, FontStyle.Bold);
				int rowHeight = 16;
				int maxWidth = 0;

				for (int i = 0; i < BarArray.Count; i++)
				{
					SizeF size = g.MeasureString( ((Bar)BarArray[i]).title, font);
					if (size.Width > maxWidth)
						maxWidth = (int)size.Width;
				}

				float width = 18 + maxWidth + 5;
				float height = 5 + (BarArray.Count * rowHeight);
				
				int x = (this.xAxis.Left + this.xAxis.Width) - (maxWidth + 50);
				int y = 25;
				y = this.leftAxis.Top + 10;

				Rectangle borderRect = new Rectangle(x, y, (int)width + 8, (int)height + 1);
				g.FillRectangle(new SolidBrush(Color.FromArgb(200, 255, 255, 255)), borderRect);
				g.DrawRectangle(new Pen(Color.Black, 1f), borderRect);

				for (int i = 0; i < BarArray.Count; i++)
				{
					Rectangle rect = new Rectangle(x + 5, y + 5, 12, 12);
					Brush b = new SolidBrush(((Bar)BarArray[i]).colour);

					g.FillRectangle(b, rect);
					g.DrawRectangle(new Pen(Color.Black, 1f), rect);
					g.DrawString( ((Bar)BarArray[i]).title, font, Brushes.Black, x + 23, y + 5);
					y += rowHeight;
				}
			}
		}

		public override void LoadData(string xmldata)
		{
			bool xAutoScale = false;
			bool yAutoScale = false;

			List<float> xValues = new List<float> ();
			List<float> yValues = new List<float> ();

			BasicXmlDocument xdoc = LibCore.BasicXmlDocument.Create(xmldata);
			XmlNode rootNode = xdoc.DocumentElement;

			foreach(XmlAttribute att in rootNode.Attributes)
			{
				if (att.Name == "show_key")
				{
					draw_key = CONVERT.ParseBool(att.Value, false);
				}

				if (att.Name == "show_bar_values")
				{
					draw_bar_values = CONVERT.ParseBool(att.Value, false);
				}
			}

			string YLeftAxisColour = string.Empty;

			foreach(XmlNode child in rootNode.ChildNodes)
			{
				if(child.NodeType == XmlNodeType.Element)
				{
					if(child.Name == "xAxis")
					{
						DefineAxis((Axis) xAxis, child, out xAutoScale);
					}
					else if(child.Name == "yAxis")
					{
						DefineAxis((Axis) leftAxis, child, out yAutoScale);

						//store the colour incase the yaxis data has no colour attribute
						foreach(XmlAttribute att in child.Attributes)
						{
							if (att.Name == "colour")
							{
								YLeftAxisColour = att.Value;
							}
						}
					}
					else if(child.Name == "band")
					{
						Band band = new Band();

						foreach(XmlAttribute att in child.Attributes)
						{
							if (att.Name == "fade")
							{
								band.fade = CONVERT.ParseBool(att.Value, false);
							}
							else if (att.Name == "colour")
							{
								band.color = CONVERT.ParseComponentColor(att.Value);
							}
							else if (att.Name == "title_colour")
							{
								band.title_color = CONVERT.ParseComponentColor(att.Value);
							}
							else if (att.Name == "bottom")
							{
								band.bottom = CONVERT.ParseInt(att.Value);
							}
							else if (att.Name == "top")
							{
								band.top = CONVERT.ParseInt(att.Value);
							}
							else if (att.Name == "title")
							{
								band.title = att.Value;
							}
						}

						Bands.Add( band );
					}
					else if (child.Name == "topkey")
					{
						TopKeyElement key = new TopKeyElement ();
						foreach (XmlAttribute att in child.Attributes)
						{
							switch (att.Name.ToLower())
							{
								case "colour":
									key.colour = CONVERT.ParseComponentColor(att.Value);
									break;

								case "outlinecolour":
									key.outlineColour = CONVERT.ParseComponentColor(att.Value);
									break;

								case "width":
									key.rectWidth = CONVERT.ParseInt(att.Value);
									break;

								case "height":
									key.rectHeight = CONVERT.ParseInt(att.Value);
									break;

								case "outlinesides":
									key.drawTop = false;
									key.drawBottom = false;
									key.drawLeft = false;
									key.drawRight = false;

									string [] sides = att.Value.Split(',');
									foreach (string side in sides)
									{
										switch (side)
										{
											case "top":
												key.drawTop = true;
												break;

											case "bottom":
												key.drawBottom = true;
												break;

											case "left":
												key.drawLeft = true;
												break;

											case "right":
												key.drawRight = true;
												break;
										}
									}
									break;
							}
						}

						key.text = child.InnerText;

						topKeyElements.Add(key);
					}
					else if(child.Name == "bars")
					{
						foreach (XmlNode n in child.ChildNodes)
						{
							if (n.NodeType == XmlNodeType.Element)
							{
								if (n.Name == "powerbar")
								{
									PowerBar bar = new PowerBar ();

									// By setting the bar's height to the maximum, we ensure that
									// when it gets rescaled, it scales to the full height of the
									// graph, allowing us to control the height of its contents
									// separately.
									bar.height = leftAxis.Max;

									foreach (XmlAttribute att in n.Attributes)
									{
										switch (att.Name)
										{
											case "firstcolour":
												bar.firstColour = CONVERT.ParseComponentColor(att.Value);
												break;

											case "secondcolour":
												bar.secondColour = CONVERT.ParseComponentColor(att.Value);
												break;

											case "outlinecolour":
												bar.outlineColour = CONVERT.ParseComponentColor(att.Value);
												break;

											case "outlinesides":
												bar.drawTop = false;
												bar.drawBottom = false;
												bar.drawLeft = false;
												bar.drawRight = false;

												string [] sides = att.Value.Split(',');
												foreach (string side in sides)
												{
													switch (side)
													{
														case "top":
															bar.drawTop = true;
															break;

														case "bottom":
															bar.drawBottom = true;
															break;

														case "left":
															bar.drawLeft = true;
															break;

														case "right":
															bar.drawRight = true;
															break;
													}
												}
												break;

											case "title":
												bar.title = att.Value;
												break;

											case "firstheight":
												bar.firstHeight = CONVERT.ParseInt(att.Value);
												break;

											case "secondheight":
												bar.secondHeight = CONVERT.ParseInt(att.Value);
												break;

											case "outlineheight":
												bar.outlineHeight = CONVERT.ParseInt(att.Value);
												break;
										
											case "logo":
												bar.logo = att.Value;
												break;
										}
									}

									BarArray.Add(bar);
								}
								else
								{
									Bar bar = new Bar();
									xValues.Add(xValues.Count + 1);

									foreach(XmlAttribute att in n.Attributes)
									{
										if (att.Name == "colour")
										{
											string[] parts = att.Value.Split(',');
											if (parts.Length==3)
											{
												int RedFactor = CONVERT.ParseInt(parts[0]);
												int GreenFactor = CONVERT.ParseInt(parts[1]);
												int BlueFactor = CONVERT.ParseInt(parts[2]);
												bar.colour = Color.FromArgb(RedFactor,GreenFactor,BlueFactor) ;
											}	
										}
										if (att.Name == "title")
										{
											bar.title = att.Value;
										}
										if (att.Name == "height")
										{
											bar.height = CONVERT.ParseInt(att.Value);
											yValues.Add(bar.height);
										}
										if (att.Name == "logo")
										{
											bar.logo = att.Value;
										}
									}

									BarArray.Add(bar);
								}
							}
						}
					}
				}
			}

			if (xAutoScale)
			{
				AutoScaleAxis(xAxis, xValues.ToArray());
			}

			if (yAutoScale)
			{
				AutoScaleAxis(leftAxis, yValues.ToArray());
			}

			AddBarControls();
		}

		void AutoScaleAxis (Axis axis, float [] values)
		{
			float? min = null;
			float? max = null;

			foreach (float value in values)
			{
				min = ((! min.HasValue) ? value : Math.Min(min.Value, value));
				max = ((! max.HasValue) ? value : Math.Max(max.Value, value));
			}

			axis.SetRange((int) min.Value, (int) max.Value, 10);
		}
	}
}