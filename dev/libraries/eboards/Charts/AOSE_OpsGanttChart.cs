using System;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;
using LibCore;


namespace Charts
{

	/// <summary>
	/// Summary description for GanttChart.
	/// </summary>
	public class AOSE_OpsGanttChart : OpsGanttChart
	{

		protected bool DoOnce = true;
		protected bool InNewForm = false;

		protected Point formOffset;
		new protected Label RevLostLabel2;
			
		const int pitStop1start = 6;
		const int pitStop2start = 12;

		public override void LoadData(string xmldata)
		{
			ReportBuilder.OpsGanttReport.KeyItem ki = new ReportBuilder.OpsGanttReport.SolidColourKeyItem("Pit Stop", Color.LightGray);

			keyItems.Add(ki);

			base.LoadData(xmldata);
		}


		protected void setWarningColor()
		{
			for (int i = 0; i < keyItems.Count; i++)
			{
				string a = keyItems[i].GetType().ToString();
				if (keyItems[i].GetType().ToString() == "ReportBuilder.OpsGanttReport+SolidColourKeyItem")
				{
					keyItems2.Add(keyItems[i]);
				}
			}

			foreach (ReportBuilder.OpsGanttReport.SolidColourKeyItem SCK in keyItems2)
			{
				if (SCK.Legend == "Warning")
				{
					warningColour = SCK.Colour;
				}
			}
		}


		protected override void DefineKey(XmlElement key)
		{

			foreach (XmlElement child in key.ChildNodes)
			{
				keyItems.Add(ReportBuilder.OpsGanttReport.KeyItem.FromXml(child));
			}
			setWarningColor();
		}

		protected override void HandleMouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			int panelPosX = -1;
			int panelPosY = -1;
			if (e != null)
			{
				panelPosX = e.X;
				panelPosY = e.Y;
			}
			InNewForm = false;

			if (sender != this)
			{
				if (sender is GanttPanel)
				{
					GanttPanel gp = (GanttPanel)sender;
					if (gp != null)
					{
						if (e != null)
						{
							panelPosX += gp.Left;
							panelPosY += gp.Top;
						}
						//System.Diagnostics.Debug.WriteLine("Panel X:" + panelPosX + " Y: " + panelPosY);
					}
				}
			}


				//Need to determine which column, our mouse position is over 
				int NewColSelected = -1;
				if ((panelPosX > GridMinX) & (panelPosX < GridMaxX))
				{
					NewColSelected = (int)((((double)panelPosX) - GridMinX) / GridStepX);

					if (NewColSelected != ColSelected)
					{
						ColSelected = NewColSelected;
						if (DisplayLostRev)
						{
							PositionMoneyLostLabel(ColSelected);
						}

						SizeHoverBar(panelPosX, panelPosY);
						
						if (!DoOnce)
						{
							//DrawLabel();
						}
						else
						{
							//SizeP1();
							//SizeP2();
							DoOnce = false;
						}
						//Refresh();
					}
					else
					{
						SizeHoverBar(panelPosX, panelPosY);
						if (DoOnce)
						{
							//SizeP1();
							//SizeP2();
							//DrawLabel();
							DoOnce = false;
						}
					}
				}
				else
				{
					ColSelected = -1;
				}
		}

		protected override void SizeHoverBar(int px, int py)
		{
			if (null != highlightBar)
			{
				if (ColSelected == -1)
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
					highlightBar.Size = new Size((int)daycol_width, (int)daycol_height);

					highlightBar.Visible = true;

					if ((null != tipWindow) && tipWindow.Visible)
					{
						p = new Point(px, py);
						p = this.PointToScreen(p);
						//if (InNewForm)
						{
							tipWindow.Location = new Point(p.X - tipWindow.Width / 2, p.Y - tipWindow.Height / 2);
						}
					}

					// Show tooltips if required...
					//toolTips.ShowAlways = true;
				}
			}
		}


		public override void HideBars()
		{
			if (null != tipWindow) tipWindow.Visible = false;
			if (null != highlightBar) highlightBar.Visible = false;
			DoOnce = true;
		}

		protected override void OpsGanttChart_VisibleChanged(object sender, EventArgs e)
		{
			if (!this.Visible)
			{
				if (null != tipWindow) tipWindow.Visible = false;
				if (null != highlightBar) highlightBar.Visible = false;
				DoOnce = true;
			}
		}

		protected override void Dispose(bool disposing)
		{
			// TODO:  Add OpsGanttChart.Dispose implementation
			if (disposing)
			{
				if (null != tipWindow) tipWindow.Visible = false;
				if (null != highlightBar) highlightBar.Visible = false;
				DoOnce = true;
				tipWindow = null;
				highlightBar = null;
			}
			base.Dispose(disposing);
		}

		protected override void AddBarControls ()
		{
			this.SuspendLayout();

			foreach (BarData bar in BarArray)
			{
				GanttPanel vp = new GanttPanel();

				vp.GraduateBars = _GraduateBars;

				vp.BorderStyle = BorderStyle.None; //.FixedSingle;
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

				if (bar.description != "")
				{
					string desc = bar.description;
					desc = desc.Replace("\\r\\n", "\r\n");
					//toolTips.SetToolTip(vp, desc );
					controlToTitle[vp] = desc;
				}

				BarPanels.Add(vp);
				vp.MouseMove += vp_MouseMove;

				Controls.Add(vp);

				if (vp.BackColor != warningColour)
				{
					vp.BringToFront();
				}
				else
				{
					vp.SendToBack();
				}
			}

			if (xAxis.Max == 10)
			{
				GanttPanel vp1 = new GanttPanel();
				vp1.Location = new Point(150 + 90 + 226 + (226 * 3 / 3) - 12, 10);
				vp1.Size = new System.Drawing.Size(47 * 2, 300 + 208);
				vp1.Visible = true;
				vp1.BackColor = Color.LightGray;
				vp1.GraduateBars = false;
				vp1.BorderStyle = BorderStyle.None; //.FixedSingle;
				vp1.BorderColor = Color.LightGray;
				Controls.Add(vp1);
				BarPanels.Add(vp1);
				vp1.MouseMove += vp_MouseMove;
			}
			else
			{
				GanttPanel vp1 = new GanttPanel();
				vp1.Location = new Point(150 + 90 + 226 - 12, 10);
				vp1.Size = new System.Drawing.Size(47, 300 + 208);
				vp1.Visible = true;
				vp1.BackColor = Color.LightGray;
				vp1.GraduateBars = false;
				vp1.BorderStyle = BorderStyle.None; //.FixedSingle;
				vp1.BorderColor = Color.LightGray;
				Controls.Add(vp1);
				BarPanels.Add(vp1);
				vp1.MouseMove += vp_MouseMove;

				GanttPanel vp2 = new GanttPanel();
				vp2.Location = new Point(150 + 90 + 226 + 226 - 12 - 2, 10);
				vp2.Size = new System.Drawing.Size(47, 300 + 208);
				vp2.Visible = true;
				vp2.BackColor = Color.LightGray;
				vp2.GraduateBars = false;
				vp2.BorderStyle = BorderStyle.None; //.FixedSingle;
				vp2.BorderColor = Color.LightGray;
				Controls.Add(vp2);
				BarPanels.Add(vp2);
				vp2.MouseMove += vp_MouseMove;
			}

			DisplayLostRev = false;

			this.ResumeLayout(false);
		}

		protected override void OpsGanttChart_ParentChanged(object sender, EventArgs e)
		{
			Form main = this.FindForm();

			if (null != main)
			{
				highlightBar = new Form();
				highlightBar.FormBorderStyle = FormBorderStyle.None;
				highlightBar.ShowInTaskbar = false;
				highlightBar.BackColor = Color.LightSkyBlue;
				highlightBar.Opacity = 0.5;
				highlightBar.MouseMove += highlightBar_MouseMove;
				main.AddOwnedForm(highlightBar);
				SizeHoverBar(0, 0);

				tipWindow = new TipWindow();
				tipWindow.Size = new Size(300, 25);
				tipWindow.Visible = false;
				tipWindow.Opacity = 0.75;
				tipWindow.label.MouseMove += tipWindow_MouseMove;
				main.AddOwnedForm(tipWindow);
			}
			else
			{
				if (highlightBar != null)
				{
					highlightBar.Dispose();
					highlightBar = null;
				}
			}
		}

		protected override void DoSize()
		{
			SuspendLayout();
			mainTitle.Size = new Size(Width, 40);

            leftAxis.Location = new Point(left_border, TopOffset);
			leftAxis.Size = new Size(left_axis_width, Height - TopOffset - mainTitle.Height);

			xAxis.Size = new Size(Width - leftAxis.Right - right_border, 40);
			xAxis.Location = new Point(leftAxis.Right, Height - xAxis.Height);

			double hh = leftAxis.Height;
			double ww = xAxis.Width - 4;

			//set size of any bar controls
			for (int i = 0; i < BarPanels.Count; i++)
			{
				VisualPanel vp = (VisualPanel)BarPanels[i];
				if (i < BarArray.Count)
				{
					BarData bar = (BarData)BarArray[i];

					//work out top left corner of panel
					int x = xAxis.Left + 2 + (int)(ww * (bar.X) / rowLength);

					double steps = (leftAxis.Max - leftAxis.Min);
					double ystep = hh / steps;
					double bary = (double)leftAxis.Max - (bar.Y - 1);
					double yoff = hh - (bary * ystep);
					int y = (int)yoff + leftAxis.Top + 2;

					//now the height of the panel
					double yoff2 = hh - ((bary - 1) * ystep) - 2;
					int barHeight = (int)(yoff2 - yoff);

					//now the width based on the length
					int rr = xAxis.Left + 2 + (int)(ww * (bar.length + bar.X) / rowLength);
					int barw = rr - x;

					//set size and location of bar
					vp.Location = new Point(x, y);
					vp.Size = new Size(barw, barHeight);
				}
			}

			GridMinX = xAxis.Left;
			GridMaxX = xAxis.Width/*-4*/ + xAxis.Left;
			GridStepX = ((double)xAxis.Width) / ((double)xAxis.Max);

			this.ResumeLayout(false);
		}

	}
}