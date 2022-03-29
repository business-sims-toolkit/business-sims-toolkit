using System;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using Algorithms;
using CoreUtils;
using LibCore;

namespace Charts
{
	public class BarData
	{
		public Color color = Color.Black;
		public Color borderColor = Color.Transparent;
		public string fill = "";
		public double X = 0;
		public double Y = 0;
		public double length = 0;
		public string description;
		public Color legendColour;
		public string legend;
	}

	/// <summary>
	/// Summary description for GanttChart.
	/// </summary>
	public class OpsGanttChart : Chart
	{
		protected class Strip
		{
			public Color? colour;
			public Color? borderColour;
			public string legend;
			public double start;
			public double end;

			public Strip (string legend, double start, double end, Color? colour, Color? borderColour)
			{
				this.colour = colour;
				this.borderColour = borderColour;
				this.legend = legend;
				this.start = start;
				this.end = end;
			}
		}

		protected bool ShowPitStops = false;

	    bool stripeAlternateRows;
	    List<Color> rowStripeColours;

		public LeftYAxis leftAxis;
		public XAxis xAxis;

		public int TopOffset = 40;

		protected ArrayList BarArray = new ArrayList();
		protected ArrayList BarPanels = new ArrayList();

		protected double rowLength = 25;

		protected PrintLabel mainTitle = new PrintLabel();

		//protected ToolTip toolTips;

		protected Hashtable controlToTitle = new Hashtable();

		protected ArrayList LostRevenues = new ArrayList();

		protected int HighlightColumn = -1;
		protected double GridMinX = 0;
		protected double GridMaxX = 0;
		protected double GridStepX = 1;
		protected int ColSelected = -1;

		protected Color warningColour;

		protected List<Strip> strips = new List<Strip> ();

		protected Label RevLostLabel;
		protected Label RevLostLabel2; 
		protected Boolean DisplayLostRev = false;

		protected Form highlightBar;
		protected TipWindow tipWindow;

		protected int left_axis_width = 220;

		protected int left_border = 10;
		protected int right_border = 10;

		protected bool _GraduateBars = true;
		protected bool auto_translate = true;

		protected List<ReportBuilder.OpsGanttReport.KeyItem> keyItems;
		protected List<ReportBuilder.OpsGanttReport.KeyItem> keyItems2;

		public bool GraduateBars
		{
			get { return _GraduateBars; }

			set
			{
				_GraduateBars = value;

				foreach(Control c in this.Controls)
				{
					GanttPanel gp = c as GanttPanel;
					if(null != gp)
					{
						gp.GraduateBars = value;
					}
				}
			}
		}

		public LeftYAxis TheLeftAxis
		{
			get { return leftAxis; }
		}

		public XAxis TheXAxis
		{
			get { return xAxis; }
		}

		public void SetLeftRightBorders(int left, int right)
		{
			left_border = left;
			right_border = right;
			DoSize();
			this.Invalidate();
		}

		public OpsGanttChart()
		{
			this.SuspendLayout();

			keyItems = new List<ReportBuilder.OpsGanttReport.KeyItem>();
			keyItems2 = new List<ReportBuilder.OpsGanttReport.KeyItem>();

			// We are creating a floating form owned by the main form so we need to watch for when
			// we get added to a form...
			this.ParentChanged += OpsGanttChart_ParentChanged;

			RevLostLabel = new Label();
			RevLostLabel.Font = CoreUtils.SkinningDefs.TheInstance.GetFont(9, FontStyle.Bold);
			RevLostLabel.ForeColor = Color.Black;
			RevLostLabel.BackColor = Color.White;
			RevLostLabel.Location = new Point(10, 10);
			RevLostLabel.TextAlign = ContentAlignment.MiddleCenter;
			RevLostLabel.Visible = false;
			RevLostLabel.Size = new Size(100, 20);
			this.Controls.Add(RevLostLabel);
            
			this.VisibleChanged += OpsGanttChart_VisibleChanged;
            
			leftAxis = new LeftYAxis();
		    leftAxis.ShowTicks = SkinningDefs.TheInstance.GetBoolData("gantt_show_y_axis_ticks", true);

			xAxis = new XAxis();
			//
			this.AutoScroll = false;
			mainTitle = new PrintLabel();
			mainTitle.TextAlign = ContentAlignment.MiddleCenter;
			mainTitle.Font = CoreUtils.SkinningDefs.TheInstance.GetFont(14);
			Controls.Add(mainTitle);
			Controls.Add(xAxis);
			Controls.Add(leftAxis);
			Resize += GanttChart_Resize;
			//
			leftAxis.SetRange(0,10,10);
			xAxis.SetRange(0,6,6);

			this.ResumeLayout(false);
			this.MouseMove += this.GanttChart_MouseMove;
			//
			//DoSize();
		}

		protected virtual void PositionMoneyLostLabel(int column)
		{
			string revamount = string.Empty;
			if ((column>-1)&(column<LostRevenues.Count))
			{
				revamount = (string)LostRevenues[column];
				RevLostLabel.Text = revamount;
				RevLostLabel.Width = 100;
				RevLostLabel.Visible = true;
				if (column>22)
				{
					RevLostLabel.Left = ((int)GridMinX)+ ((int)GridStepX * 22) + ((int)(GridStepX)) / 2;
				}
				else
				{
					RevLostLabel.Left = ((int)GridMinX)+ ((int)GridStepX * column) + ((int)(GridStepX)) / 2;
				}
				RevLostLabel.Invalidate(); //Refresh();
			}
			else
			{
				RevLostLabel.Visible = false;
			}
		}

		protected virtual void HandleMouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			int panelPosX = e.X;
			int panelPosY = e.Y;

			//Check that we need to display the lost revenue
			//if (DisplayLostRev)
			{
				//if we are handling a mouse passed on from the bar control
				//then we need to adjust the relative position to an overall control position
				if (sender != this)
				{
					if (sender is GanttPanel)
					{
						GanttPanel gp = (GanttPanel) sender;
						if (gp != null)
						{
							panelPosX += gp.Left;
							panelPosY += gp.Top;
						}
					}
				}
				//System.Diagnostics.Debug.WriteLine("X:"+panelPosX.ToString()+" Y:"+panelPosY.ToString() + "   "+stt);
				//Need to determine which column, our mouse position is over 
				int NewColSelected = -1;
				if ((panelPosX > GridMinX)&(panelPosX < GridMaxX))
				{
					//NewColSelected = (panelPosX - (int)GridMinX) / ((int)GridStepX);

					NewColSelected = (int)(( ((double)panelPosX) - GridMinX) / GridStepX);

					//System.Diagnostics.Debug.WriteLine("ColSelected"+ColSelected.ToString());
					//Only need to do work if the selected column is changed
					if (NewColSelected != ColSelected)
					{
						ColSelected = NewColSelected;
						if (DisplayLostRev)
						{
							PositionMoneyLostLabel(ColSelected);
						}
						SizeHoverBar(panelPosX,panelPosY);
						//Refresh();
					}
					else
					{
						SizeHoverBar(panelPosX,panelPosY);
					}
				}
				else
				{
					ColSelected = -1;
				}
			}
		}

		/// <summary>
		/// Handles the Mouse moves on the Gantt Chart Panel
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		protected void GanttChart_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			//pass on the move to the general handler 
			 HandleMouseMove(sender, e);
		}

		protected void GanttChart_Resize (object sender, EventArgs e)
		{
			if (highlightBar != null)
			{
				highlightBar.Hide();
			}

			DoSize();
			this.Invalidate();
		}

		public void SetLeftAxisWidth (int width)
		{
			left_axis_width = width;
			DoSize();
		}
		protected virtual void DoSize()
		{
			SuspendLayout();
			mainTitle.Size = new Size(Width, 40);

            leftAxis.Location = new Point (left_border,TopOffset);
			leftAxis.Size = new Size (left_axis_width, Height - TopOffset - mainTitle.Height);

			xAxis.Size = new Size (Width - leftAxis.Right - right_border, 40);

            xAxis.Location = new Point (leftAxis.Right, Height - xAxis.Height);

			double hh = leftAxis.Height;
			double ww = xAxis.Width-4;

			//set size of any bar controls
			for (int i = 0; i < BarPanels.Count; i++)
			{
				VisualPanel vp = (VisualPanel)BarPanels[i];
				BarData bar = (BarData)BarArray[i];

				//work out top left corner of panel
				int x = xAxis.Left + 2 + (int)(ww * (bar.X) / rowLength);

				double steps = (leftAxis.Max - leftAxis.Min);
				double ystep = hh / steps;
				double bary = (double)leftAxis.Max - (bar.Y - 1);
				double yoff = hh - (bary * ystep);

			    var inset = (SkinningDefs.TheInstance.GetBoolData("gantt_bars_have_vertical_inset", true) ? 2 : 0);

                int y = (int)yoff + leftAxis.Top + inset;

				//now the height of the panel
				double yoff2 = hh - ((bary - 1) * ystep) - inset;
				int barHeight = (int)(yoff2 - yoff);

				//now the width based on the length
				int rr = xAxis.Left + 2 + (int)(ww * (bar.length + bar.X) / rowLength);
				int barw = rr - x;

				//set size and location of bar
				vp.Location = new Point(x, y);
				vp.Size = new Size(barw, barHeight);
			}

			GridMinX = xAxis.Left;
			GridMaxX = xAxis.Width/*-4*/ + xAxis.Left;
			GridStepX = ((double)xAxis.Width) / ((double)xAxis.Max);

			this.ResumeLayout(false);
		}

		public void SetFont(string name, int titleSize, FontStyle fs1, int axesSize, FontStyle fs2)
		{
			//mainTitle.Font = ConstantSizeFont.NewFont(name, titleSize, fs1);
			//mainTitle.TextAlign = ContentAlignment.MiddleCenter;
			Font f = ConstantSizeFont.NewFont(name, axesSize, fs2);
			xAxis.Font = f;
			leftAxis.Font = f;
			mainTitle.Font = f;
		}

		protected bool fillColour = false;
		protected SolidBrush fill;

		public void SetFillColour(Color c)
		{
			fill = new SolidBrush(c);
			fillColour = true;
		}

		protected int vwidth = 1;
		protected int hwidth = 1;

		public void SetGridWidth(int h, int v)
		{
			hwidth = h;
			vwidth = v;
		}

		protected Color gridColour = Color.LightGray;

		public void SetGridColour(Color c)
		{
			gridColour = c;
		}
		
		protected override void OnPaint( PaintEventArgs e )
		{
			base.OnPaint(e);

			if(fillColour)
			{
				e.Graphics.FillRectangle(fill, xAxis.Left, TopOffset, xAxis.Width, xAxis.Top - TopOffset);
			}

		    if (stripeAlternateRows)
		    {
		        int i = 0;
		        for (int y = 0; y <= leftAxis.Max; y++)
		        {
		            var row = new Rectangle (xAxis.Left, (int) Maths.MapBetweenRanges(y, 0, leftAxis.Max, leftAxis.Bottom, leftAxis.Top), xAxis.Width, (int) (leftAxis.Height / (double) (leftAxis.Max - leftAxis.Min)));

		            using (var brush = new SolidBrush (rowStripeColours[(leftAxis.Max - i) % rowStripeColours.Count]))
		            {
		                e.Graphics.FillRectangle(brush, row);
		            }

		            i++;
		        }

		        for (int x = xAxis.Min; x <= xAxis.Max; x++)
		        {
			        i = 0;

		            for (int y = 0; y <= leftAxis.Max; y++)
		            {
						using (var pen = new Pen (rowStripeColours[(leftAxis.Max - i) % rowStripeColours.Count]))
						{
							var screenX = (int) Maths.MapBetweenRanges(x, xAxis.Min, xAxis.Max, xAxis.Left, xAxis.Right);
							var bottomScreenY = (int) Maths.MapBetweenRanges(y, leftAxis.Min, leftAxis.Max, leftAxis.Bottom, leftAxis.Top);
							var topScreenY = (int) Maths.MapBetweenRanges(y + 1, leftAxis.Min, leftAxis.Max, leftAxis.Bottom, leftAxis.Top);
							e.Graphics.DrawLine(pen, screenX, bottomScreenY, screenX, topScreenY);
						}

			            i++;
		            }
				}
			}
            else
		    {
		        ChartUtils.DrawGrid(e, this, gridColour,
		            xAxis.Left, TopOffset, xAxis.Width, xAxis.Top - TopOffset,
		            xAxis.Max, leftAxis.Steps, hwidth, vwidth);
		    }

		    foreach (Strip strip in strips)
			{
				double startX = (strip.start - xAxis.Min) * (xAxis.Width * 1.0 / (xAxis.Max - xAxis.Min));
				double endX = (strip.end - xAxis.Min) * (xAxis.Width * 1.0 / (xAxis.Max - xAxis.Min));

                RectangleF rectangle = new RectangleF ((float) startX, TopOffset, (float) (endX - startX), xAxis.Top - TopOffset);

				StringFormat format = new StringFormat();
				format.Alignment = StringAlignment.Far;
				format.LineAlignment = StringAlignment.Center;

				if (strip.colour.HasValue)
				{
					using (Brush brush = new SolidBrush (strip.colour.Value))
					{
						e.Graphics.FillRectangle(brush, rectangle);
					}
				}

				if (strip.borderColour.HasValue)
				{
					using (Pen pen = new Pen (strip.borderColour.Value))
					{
						e.Graphics.DrawRectangle(pen, new Rectangle ((int) rectangle.Left, (int) rectangle.Top, (int) rectangle.Width, (int) rectangle.Height));
					}
				}

				if (! String.IsNullOrEmpty(strip.legend))
				{
					using (Font font = ConstantSizeFont.NewFont(SkinningDefs.TheInstance.GetData("fontname"), 12))
					{
						e.Graphics.DrawString(strip.legend.Replace(" ", "\n\n"), font, Brushes.Black, rectangle, format);
					}
				}
			}
		}

		public void SetTitles(string mTitle, string xaxis, string lyaxis)
		{
			mainTitle.Text = mTitle;
			xAxis.axisTitle.Text = xaxis;
			leftAxis.axisTitle.Text = lyaxis;
		}

		protected void DefineAxis(Axis axis, XmlNode n)
		{
			char [] sep = { ',' };
			//
			foreach(XmlAttribute att in n.Attributes)
			{
				if("title" == att.Name)
				{
					axis.axisTitle.Text = att.Value;
					axis.TitleFont = CoreUtils.SkinningDefs.TheInstance.GetFont(11, FontStyle.Bold);
				}
				else if("width" == att.Name)
				{
					if(leftAxis == axis)
					{
						this.left_axis_width = CONVERT.ParseInt( att.Value );
					}
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
					    axis.SetTextColour(Color.FromArgb(RedFactor, GreenFactor, BlueFactor));
					}					
				}
				else if(att.Name == "labels")
				{
					string[] labels = att.Value.Split(',');
				}
				else if(att.Name == "align")
				{
					axis.SetLabelAlignment(att.Value);
				}
			}
		}

		protected virtual string GetFilenameFromPatternName (string patternName)
		{
			if (patternName == "orange_hatch")
			{
				return AppInfo.TheInstance.Location + "\\images\\chart\\hatch.png";
			}
			else if (patternName == "blue_hatch")
			{
				return AppInfo.TheInstance.Location + "\\images\\chart\\blue_hatch.png";
			}
			else if (patternName == "dos_hatch")
			{
				return AppInfo.TheInstance.Location + "\\images\\chart\\hatch_dos.png";
			}
			else if (patternName == "thermal_sla")
			{
				return AppInfo.TheInstance.Location + "\\images\\chart\\thermal_sla.png";
			}
			else if (patternName == "no_power")
			{
				return AppInfo.TheInstance.Location + "\\images\\chart\\no_power.png";
			}
			else if (patternName == "no_power_sla")
			{
				return AppInfo.TheInstance.Location + "\\images\\chart\\no_power_sla.png";
			}
			else if (patternName == "yellow_hatch")
			{
				return AppInfo.TheInstance.Location + "\\images\\chart\\yellow_hatch.png";
			}
            else if (patternName == "magenta_hatch")
            {
                return AppInfo.TheInstance.Location + "\\images\\chart\\magenta_hatch.png";
            }
            else if (patternName.EndsWith(".png"))
			{
				return AppInfo.TheInstance.Location + "\\images\\chart\\" + patternName;
			}
			else
			{
                return "";
			}
		}

		protected virtual void AddBarControls()
		{
			this.SuspendLayout();

			foreach (BarData bar in BarArray)
			{
				GanttPanel vp = new GanttPanel();
				//vp.Visible = false;

				vp.GraduateBars = _GraduateBars;

				vp.BorderStyle = BorderStyle.None;
				vp.BorderColor = bar.borderColor;

				vp.LegendColor = bar.legendColour;
				vp.Legend = bar.legend;

				string filename = GetFilenameFromPatternName(bar.fill);
				if (filename != "")
				{
					vp.BackgroundImage = Repository.TheInstance.GetImage(filename);
				}
				else
				{
					vp.BackColor = bar.color;
				}

				if(bar.description != "")
				{
					string desc = bar.description;
					desc = desc.Replace("\\r\\n","\r\n");
					//toolTips.SetToolTip(vp, desc );
					controlToTitle[vp] = desc;
				}

				BarPanels.Add(vp);
				vp.MouseMove +=vp_MouseMove;
				
				Controls.Add(vp);
            }

			this.ResumeLayout(false);
		}

		public override void LoadData(string xmldata)
		{
			//if(toolTips != null) toolTips.Dispose();
			//toolTips = new ToolTip();
			controlToTitle.Clear();

			//reset the Lost Rev to Zero	
			for (int i=0; i<26; i++)
			{
				LostRevenues.Add("0");
			}
			//Assume no display 
			DisplayLostRev = false;
			//
			BasicXmlDocument xdoc = LibCore.BasicXmlDocument.Create(xmldata);
			var rootNode = xdoc.DocumentElement;

		    stripeAlternateRows = rootNode.GetBooleanAttribute("stripe_alternate_rows", false);
            rowStripeColours = new List<Color> ();
		    rowStripeColours.Add(Color.FromArgb(238, 238, 238));
            rowStripeColours.Add(Color.White);

			string YLeftAxisColour = string.Empty;
            string StripedColours = string.Empty;

			int yoff = 1;

			foreach(XmlNode child in rootNode.ChildNodes)
			{
				if(child.NodeType == XmlNodeType.Element)
				{
					if(child.Name == "title")
					{
						mainTitle.Text = child.InnerXml;
					}
					else if (child.Name == "key")
					{
						DefineKey((XmlElement) child);
					}
					else if (child.Name == "xAxis")
					{
						DefineAxis((Axis) xAxis, child);
					}
					else if (child.Name == "yAxis")
					{
						string labels = "";
						DefineAxis((Axis) leftAxis, child);

						//store the colour incase the yaxis data has no colour attribute
						foreach (XmlAttribute att in child.Attributes)
						{
							if (att.Name == "colour")
							{
								YLeftAxisColour = att.Value;
							}
							else if (att.Name == "labels")
							{
								labels = att.Value;
							}
                            else if (att.Name == "colours")
                            {
                                StripedColours = att.Value;
                            }
						}

						if (labels != "")
						{
							char[] seps = { ',' };
							string[] theLabels = labels.Split(seps);
							int count = leftAxis.NumLabels - 1; //-1;
							foreach (string l in theLabels)
							{
								if (auto_translate)
								{
									leftAxis.SetLabel(count, TextTranslator.TheInstance.Translate(l));
								}
								else
								{
									leftAxis.SetLabel(count, l);
								}
								--count;
							}
						    if (StripedColours != string.Empty)
						    {
                                List<Color> labelColours = new List<Color>();
                                string[] theColours = StripedColours.Split(seps);
                                for (int i = 0; i < theColours.Length-1; i=i+3)
                                {
                                    labelColours.Add(CONVERT.ParseComponentColor(theColours[i] + "," + theColours[i+1] + "," + theColours[i+2]));
                                }
						        leftAxis.SetStriped(labelColours.ToArray());
						    }
						}
					}
					else if (child.Name == "rows")
					{
						foreach (XmlAttribute att in child.Attributes)
						{
							if (att.Name == "length")
							{
								rowLength = CONVERT.ParseDouble(att.Value);
							}
						}

						foreach (XmlNode rowNode in child.ChildNodes)
						{
							if ((rowNode.NodeType == XmlNodeType.Element) && (rowNode.Name == "row"))
							{
								string BarColour = string.Empty;
								string BorderColour = string.Empty;
								string LegendColour = string.Empty;
								string Legend = string.Empty;

								// Load the gantt row attributes
								foreach (XmlAttribute att in rowNode.Attributes)
								{
                                    switch(att.Name)
                                    {
                                        case "colour":
                                            BarColour = att.Value;
                                            break;
                                    }
								}

								//load each bar on the chart
								foreach (XmlNode n in rowNode.ChildNodes)
								{
									if (n.NodeType == XmlNodeType.Element)
									{
										if (n.Name == "bar")
										{
											BarData bar = new BarData();
											
											bar.description = n.InnerXml.Trim();
                                            //bar.legend = Legend;
											
											bar.Y = yoff;
											bar.color = Color.Red;

											foreach (XmlAttribute att in n.Attributes)
											{
												if ("x" == att.Name)
												{
													bar.X = CONVERT.ParseDouble(att.Value);
												}
												else if (att.Name == "length")
												{
													bar.length = CONVERT.ParseDouble(att.Value);
												}
												else if (att.Name == "colour")
												{
													BarColour = att.Value;

													string[] parts = BarColour.Split(',');
													if (parts.Length == 3)
													{
														int RedFactor = CONVERT.ParseInt(parts[0]);
														int GreenFactor = CONVERT.ParseInt(parts[1]);
														int BlueFactor = CONVERT.ParseInt(parts[2]);

														bar.color = Color.FromArgb(RedFactor, GreenFactor, BlueFactor);
													}
												}
												else if (att.Name == "bordercolour")
												{
													BorderColour = att.Value;

													string[] parts = BorderColour.Split(',');
													if (parts.Length == 3)
													{
														int RedFactor = CONVERT.ParseInt(parts[0]);
														int GreenFactor = CONVERT.ParseInt(parts[1]);
														int BlueFactor = CONVERT.ParseInt(parts[2]);

														bar.borderColor = Color.FromArgb(RedFactor, GreenFactor, BlueFactor);
													}
												}
												else if (att.Name == "legendcolour")
												{
													LegendColour = att.Value;
													bar.legendColour = CONVERT.ParseComponentColor(LegendColour);
												}
												else if (att.Name == "legend")
												{
													Legend = att.Value;
													bar.legend = Legend;
												}
												else if (att.Name == "fill")
												{
													bar.fill = att.Value;
												}
												else if (att.Name == "description")
												{
													bar.description = att.Value;
												}
											}

											BarArray.Add(bar);
										}
									}
								}

								++yoff;
							}
						}
					}
					else if (child.Name == "revenue_lost")
					{
						//we have revenue data so we need to display it 
						DisplayLostRev = true;
						foreach (XmlNode minuteNode in child.ChildNodes)
						{
							if ((minuteNode.NodeType == XmlNodeType.Element) && (minuteNode.Name == "revenue"))
							{
								string min = "";
								string lost_rev = "";
								foreach (XmlAttribute att in minuteNode.Attributes)
								{
									if (att.Name == "minute")
									{
										min = att.Value;
									}
									if (att.Name == "lost")
									{
										lost_rev = att.Value;
									}
								}
								int inx = CONVERT.ParseInt(min);
								LostRevenues[inx] = lost_rev;
							}
						}
					}
					else if (child.Name == "strip")
					{
						string legend = "";
						if (child.Attributes["legend"] != null)
						{
							legend = child.Attributes["legend"].Value;
						}
						double start = CONVERT.ParseDouble(child.Attributes["start"].Value);
						double end = CONVERT.ParseDouble(child.Attributes["end"].Value);

						Color? colour = null;
						if (child.Attributes["colour"] != null)
						{
							colour = CONVERT.ParseComponentColor(child.Attributes["colour"].Value);
						}

						Color? borderColour = null;
						if (child.Attributes["border_colour"] != null)
						{
							borderColour = CONVERT.ParseComponentColor(child.Attributes["border_colour"].Value);
						}
						strips.Add(new Strip(legend, start, end, colour, borderColour));
					}
				}
			}

			AddBarControls();

			leftAxis.Font = CoreUtils.SkinningDefs.TheInstance.GetFont(8);
		}

		/// <summary>
		/// Handles the mouse moves of the bar controls 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		protected void vp_MouseMove(object sender, MouseEventArgs e)
		{
			//passing on the mouse from this bar control
			HandleMouseMove(sender, e);
		}

		protected virtual void SizeHoverBar(int px, int py)
		{
			if(null != highlightBar)
			{
				if(ColSelected == -1)
				{
					highlightBar.Visible = false;
				}
				else
				{
					int xl = xAxis.Left;
					int yl = xAxis.Top;
					double ww = xAxis.Width;//-4;
					double hh = leftAxis.Height;

					double daycol_width = ((double)xAxis.Width) / ((double)xAxis.Max);
					double daycol_height = hh;
					double daycol_top = TopOffset;

					double startpos = daycol_width * (ColSelected) + xl;

					Point p = new Point((int)startpos, (int)daycol_top);
					p = this.PointToScreen(p);
					highlightBar.Location = p;
					highlightBar.Size = new Size((int)daycol_width,(int)daycol_height);

					highlightBar.Visible = true;

					if( (null != tipWindow) && tipWindow.Visible)
					{
						p = new Point(px,py);
						p = this.PointToScreen( p );
						tipWindow.Location = new Point(p.X - tipWindow.Width/2, p.Y - tipWindow.Height/2) ;
					}

					// Show tooltips if required...
					//toolTips.ShowAlways = true;
				}
			}
		}
		protected virtual void OpsGanttChart_ParentChanged(object sender, EventArgs e)
		{
			Form main = this.FindForm();

			if(null != main)
			{
				highlightBar = new Form();
				highlightBar.FormBorderStyle = FormBorderStyle.None;
				highlightBar.ShowInTaskbar = false;
				highlightBar.BackColor = Color.LightSkyBlue;
				highlightBar.Opacity = 0.5;
				highlightBar.MouseMove += highlightBar_MouseMove;
				main.AddOwnedForm(highlightBar);
				SizeHoverBar(0,0);

				tipWindow = new TipWindow();
				tipWindow.Size = new Size(300,25);
				tipWindow.Visible = false;
				tipWindow.Opacity = 0.75;
				tipWindow.label.MouseMove += tipWindow_MouseMove;
				main.AddOwnedForm(tipWindow);
			}
			else
			{
				if(highlightBar != null)
				{
					highlightBar.Dispose();
					highlightBar = null;
				}
			}
		}

		protected void highlightBar_MouseMove(object sender, MouseEventArgs e)
		{
			// Pass on to Gannt chart so tool tips work...
			Point p = new Point(e.X, e.Y);
			p = highlightBar.PointToScreen( p );
			Point p2 = this.PointToClient( p );

			Control control = this.GetChildAtPoint(p2);

			if( (null != control) && (this.controlToTitle.ContainsKey(control)) )
			{
				string desc = (string) controlToTitle[control];
				tipWindow.SetText(desc);
				tipWindow.Location = new Point(p.X - tipWindow.Width/2, p.Y - tipWindow.Height/2) ;
				if (control.BackColor != warningColour)
				{
					tipWindow.Visible = true;
				}
			}
			else
			{
				tipWindow.Visible = false;
			}

			/*
			MouseEventArgs newEvent = new MouseEventArgs(e.Button, e.Clicks, p.X, p.Y, e.Delta);
			this.OnMouseMove( newEvent );
			*/
		}

		protected void tipWindow_MouseMove(object sender, MouseEventArgs e)
		{
			Point p = new Point(e.X, e.Y);
			p = tipWindow.PointToScreen( p );
			p = this.PointToClient( p );

			Control control = this.GetChildAtPoint(p);

			if( (null != control) && (this.controlToTitle.ContainsKey(control)) )
			{
				if (control.BackColor == warningColour)
				{
					tipWindow.Visible = false;
				}
				string desc = (string) controlToTitle[control];
				tipWindow.SetText(desc);
				//tipWindow.Location = new Point(p.X - tipWindow.Width/2, p.Y - tipWindow.Height/2) ;
			}
			else
			{
				tipWindow.Visible = false;
			}

			MouseEventArgs newEvent = new MouseEventArgs(e.Button, e.Clicks, p.X, p.Y, e.Delta);
			this.HandleMouseMove(sender, newEvent );
		}

		public virtual void HideBars()
		{
			if(null != tipWindow) tipWindow.Visible = false;
			if(null != highlightBar) highlightBar.Visible = false;
		}

		protected virtual void OpsGanttChart_VisibleChanged(object sender, EventArgs e)
		{
			if(!this.Visible)
			{
				if(null != tipWindow) tipWindow.Visible = false;
				if(null != highlightBar) highlightBar.Visible = false;
			}
		}
	
		protected override void Dispose(bool disposing)
		{
			// TODO:  Add OpsGanttChart.Dispose implementation
			if(disposing)
			{
				if(null != tipWindow) tipWindow.Visible = false;
				if(null != highlightBar) highlightBar.Visible = false;
				tipWindow = null;
				highlightBar = null;
			}
			base.Dispose (disposing);
		}

		protected virtual void DefineKey (XmlElement key)
		{
			
			foreach (XmlElement child in key.ChildNodes)
			{
				keyItems.Add(ReportBuilder.OpsGanttReport.KeyItem.FromXml(child));
			}
		}

		public void AddKey (Control parent)
		{
			Font font = ConstantSizeFont.NewFont(CoreUtils.SkinningDefs.TheInstance.GetData("fontname"), 9);
			
			using (Graphics graphics = parent.CreateGraphics())
			{
				int blockSize = 20;
				int blockGap = 10;
				int columnGap = 50;
				int rowGap = 4;
				int borderThickness = 1;

				int maxWidth = 0;
				int items = 0;
				foreach (ReportBuilder.OpsGanttReport.KeyItem keyItem in keyItems)
				{
					maxWidth = Math.Max(maxWidth, blockSize + blockGap + (int) graphics.MeasureString(keyItem.Legend, font).Width);
					items++;
				}

				int columnWidth = blockSize + blockGap + maxWidth;
				int columnsPerRow = (int) Math.Floor((double) (parent.Width + columnGap) / (columnWidth + columnGap));
				int columns = Math.Min(columnsPerRow, items);
				int rows = (int) Math.Ceiling((double) items / columnsPerRow);

				int startX = (parent.Width - ((columns * columnWidth) + ((columns - 1) * columnGap))) / 2;
				int x = startX;
				int y = (parent.Height - ((rows * blockSize) + ((rows - 1) * rowGap))) / 2;
				foreach (ReportBuilder.OpsGanttReport.KeyItem keyItem in keyItems)
				{
					Panel panel = new Panel ();
					panel.Location = new Point (x, y);
					panel.Size = new Size (blockSize, blockSize);
					parent.Controls.Add(panel);

					Control interior = null;
					if (keyItem.BorderColour.HasValue)
					{
						panel.BackColor = keyItem.BorderColour.Value;

						interior = new Panel ();
						interior.Location = new Point (borderThickness, borderThickness);
						interior.Size = new Size (panel.Width - (2 * borderThickness), panel.Height - (2 * borderThickness));
						panel.Controls.Add(interior);
					}
					else
					{
						interior = panel;
					}

					ReportBuilder.OpsGanttReport.SolidColourKeyItem colouredItem = keyItem as ReportBuilder.OpsGanttReport.SolidColourKeyItem;
					ReportBuilder.OpsGanttReport.PatternedKeyItem patternedItem = keyItem as ReportBuilder.OpsGanttReport.PatternedKeyItem;

					if (colouredItem != null)
					{
						interior.BackColor = colouredItem.Colour;
					}
					else if (patternedItem != null)
					{
						ImageBox imageBox = new ImageBox ();
						imageBox.ImageLocation = GetFilenameFromPatternName(patternedItem.Pattern);
						imageBox.Size = interior.Size;
						interior.Controls.Add(imageBox);
					}

					Label label = new Label ();
					label.Font = font;
					label.Text = keyItem.Legend;
					label.Location = new Point (x + blockSize + blockGap, y);
					label.Size = new Size (maxWidth, blockSize);
					label.TextAlign = ContentAlignment.MiddleLeft;
					parent.Controls.Add(label);

					x = label.Right + columnGap;
					if ((x + blockSize + blockGap + maxWidth) > parent.Width)
					{
						x = startX;
						y += (blockSize + rowGap);
					}
				}				
			}
		}
	}
}