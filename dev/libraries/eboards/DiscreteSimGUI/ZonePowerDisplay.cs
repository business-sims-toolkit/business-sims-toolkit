using System;
using System.Drawing;
using System.Windows.Forms;

using System.Collections;
using System.Runtime.InteropServices;

using LibCore;
using Network;
using CommonGUI;
using CoreUtils;

namespace DiscreteSimGUI
{
	/// <summary>
	/// This Control presents a power use graph covering the different zones with the network. 
	/// This is just the display of values held within the model 
	/// The values are produced by BusinessServiceRules code (SystemPowerMonitor.cs)
	/// The Control can project the Graph in both Horizontel and Vertical Modes
	/// Switch Test Mode to TRUE to use a test 0,20,40,60,80,100,120 test ramp display 
	/// </summary>
	public class ZonePowerDisplay : FlickerFreePanel
	{
		[DllImport("User32.dll", CharSet=CharSet.Auto)]
		protected static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool redraw);

		static System.Globalization.CultureInfo myCI_enGB = new System.Globalization.CultureInfo( "en-GB", false );

		protected Node powerNode;
		protected int ZoneLabelheight = 20;
		protected int ZoneLabelwidth = 75;
		protected	Font dispfont = ConstantSizeFont.NewFont(CoreUtils.SkinningDefs.TheInstance.GetData("fontname", "Trebuchet MS"),9.5f, FontStyle.Bold);
		protected Boolean PaintBackground = true;
		protected int[] ZoneLevels = new int[7];

		public int zoneCount = 7;

		protected Color Values_Color = Color.White;
		protected Color Titles_Color = Color.White;

		protected Brush Brush_PowerLevel_OK;
		protected Brush Brush_PowerLevel_High;
		protected Brush Brush_PowerLevel_Warning;
		protected Brush Brush_PowerLevel_Overload;

	    protected Brush Brush_PowerLevel_Empty_OK;
	    protected Brush Brush_PowerLevel_Empty_High;
	    protected Brush Brush_PowerLevel_Empty_Warning;
	    protected Brush Brush_PowerLevel_Empty_Overload;
        
		protected Brush Brush_Background;
		protected Brush Brush_Titles;
		protected Brush Brush_Values;
		protected Pen Pen_Valuemarks;
		protected Boolean DrawEmptyBoxes = false;
		protected Boolean DrawVertical = true;
		protected Boolean testMode = false;
        Color backColour = SkinningDefs.TheInstance.GetColorDataGivenDefault("power_graph_background_colour", Color.Black);
	    bool showBorders = true;

		public ZonePowerDisplay (NodeTree model)
			: this (model, 7)
		{
		}

		/// <summary>
		/// Show Availability / Revenue
		/// </summary>
		/// <param name="model"></param>
		public ZonePowerDisplay(NodeTree model, int zoneCount)
		{
		    BackColor = backColour;

			this.zoneCount = zoneCount;

			for(int step =0; step < 7; step++)
			{
				ZoneLevels[step] = (step * 20);
			}
			ZoneLevels[0] = 10;
			
			//this.BackColor = Color.DarkKhaki;
			Font f1_Values = CoreUtils.SkinningDefs.TheInstance.GetFont(9.5f, FontStyle.Bold);
			Font f1_Titles = CoreUtils.SkinningDefs.TheInstance.GetFont(9.5f, FontStyle.Bold);
			Color Titles_Color = Color.White;
			Color Values_Color = Color.White;

            Brush_Background = new SolidBrush(backColour);
			Brush_Titles = new SolidBrush(Color.White);
			Brush_Values = new SolidBrush(Color.White);
			Pen_Valuemarks = new Pen(Color.White);

			Brush_PowerLevel_OK = new SolidBrush(SkinningDefs.TheInstance.GetColorDataGivenDefault("power_graph_colour_ok", Color.Green));
			Brush_PowerLevel_High = new SolidBrush(SkinningDefs.TheInstance.GetColorDataGivenDefault("power_graph_colour_high", Color.Yellow));
			Brush_PowerLevel_Warning = new SolidBrush(SkinningDefs.TheInstance.GetColorDataGivenDefault("power_graph_colour_warning", Color.Orange));
			Brush_PowerLevel_Overload = new SolidBrush(SkinningDefs.TheInstance.GetColorDataGivenDefault("power_graph_colour_overload", Color.Red));

            Brush_PowerLevel_Empty_OK = new SolidBrush(SkinningDefs.TheInstance.GetColorDataGivenDefault("power_graph_background_colour_ok", backColour));
            Brush_PowerLevel_Empty_High = new SolidBrush(SkinningDefs.TheInstance.GetColorDataGivenDefault("power_graph_background_colour_high", backColour));
            Brush_PowerLevel_Empty_Warning = new SolidBrush(SkinningDefs.TheInstance.GetColorDataGivenDefault("power_graph_background_colour_warning", backColour));
            Brush_PowerLevel_Empty_Overload = new SolidBrush(SkinningDefs.TheInstance.GetColorDataGivenDefault("power_graph_background_colour_overload", backColour));

			powerNode = model.GetNamedNode("PowerLevel");
			powerNode.AttributesChanged += powerNode_AttributesChanged;

			SetBoundingClipWindow();
			this.Resize += ZonePowerDisplay_Resize;
			UpdatePowerDisplay();
		}

		/// <summary>
		/// Helper method for diposing brushes
		/// </summary>
		/// <param name="br"></param>
		protected void DisposeBrush(ref Brush br)
		{
			if (br != null)
			{
				br.Dispose();
				br = null;
			}
		}

		/// <summary>
		/// Dispose of all the Pens and Brushes
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose(bool disposing)
		{
			if(disposing)
			{
				DisposeBrush(ref Brush_Background);
				DisposeBrush(ref Brush_Titles);
				DisposeBrush(ref Brush_Values);

				if (Pen_Valuemarks != null)
				{
					Pen_Valuemarks.Dispose();
				}

				DisposeBrush(ref Brush_PowerLevel_OK);
				DisposeBrush(ref Brush_PowerLevel_High);
				DisposeBrush(ref Brush_PowerLevel_Warning);
				DisposeBrush(ref Brush_PowerLevel_Overload);

				DisposeBrush(ref Brush_PowerLevel_Empty_OK);
			    DisposeBrush(ref Brush_PowerLevel_Empty_High);
			    DisposeBrush(ref Brush_PowerLevel_Empty_Warning);
			    DisposeBrush(ref Brush_PowerLevel_Empty_Overload);
			}

			base.Dispose (disposing);
		}
		
		/// <summary>
		/// Dispose ...
		/// </summary>
		public new void Dispose()
		{
			powerNode.AttributesChanged -= powerNode_AttributesChanged;
		}

		/// <summary>
		/// Helper method for descendants used in PoleStar2  
		/// </summary>
		public virtual void SetBoundingClipWindow()
		{
		}

		/// <summary>
		/// Allow Changing of all the colours 
		/// </summary>
		/// <param name="NewBackColor"></param>
		/// <param name="TextLabelColor"></param>
		/// <param name="TextValueColor"></param>
		/// <param name="power_level_ok_color"></param>
		/// <param name="power_level_high_color"></param>
		/// <param name="power_level_warning_color"></param>
		/// <param name="power_level_overload_color"></param>
		/// <param name="power_level_empty"></param>
		public void SetDisplayColors(Color NewBackColor, Color TextLabelColor, Color TextValueColor, 
			Color power_level_ok_color, Color power_level_high_color, 
			Color power_level_warning_color, Color power_level_overload_color,
			Color power_level_empty_color)
		{
			//Handle the Background
			BackColor = NewBackColor;
			DisposeBrush(ref Brush_Background);
			Brush_Background = new SolidBrush(NewBackColor);

			DisposeBrush(ref Brush_Titles);
			Brush_Titles = new SolidBrush(TextLabelColor);
			DisposeBrush(ref Brush_Values);
			Brush_Values = new SolidBrush(TextValueColor);
			Titles_Color = TextLabelColor;
			Values_Color = TextValueColor;

			if (Pen_Valuemarks != null)
			{
				Pen_Valuemarks.Dispose();
				Pen_Valuemarks = new Pen(TextValueColor);
			}

			DisposeBrush(ref Brush_PowerLevel_OK);
			Brush_PowerLevel_OK = new SolidBrush(power_level_ok_color);

			DisposeBrush(ref Brush_PowerLevel_High);	
			Brush_PowerLevel_High = new SolidBrush(power_level_high_color);

			DisposeBrush(ref Brush_PowerLevel_Warning);	
			Brush_PowerLevel_Warning = new SolidBrush(power_level_warning_color);

			DisposeBrush(ref Brush_PowerLevel_Overload);	
			Brush_PowerLevel_Overload = new SolidBrush(power_level_overload_color);

		    DisposeBrush(ref Brush_PowerLevel_Empty_OK);
		    Brush_PowerLevel_Empty_OK = new SolidBrush(SkinningDefs.TheInstance.GetColorDataGivenDefault("power_graph_background_colour_ok", backColour));

		    DisposeBrush(ref Brush_PowerLevel_Empty_High);
		    Brush_PowerLevel_Empty_High = new SolidBrush(SkinningDefs.TheInstance.GetColorDataGivenDefault("power_graph_background_colour_high", backColour));

		    DisposeBrush(ref Brush_PowerLevel_Empty_Warning);
		    Brush_PowerLevel_Empty_Warning = new SolidBrush(SkinningDefs.TheInstance.GetColorDataGivenDefault("power_graph_background_colour_warning", backColour));

		    DisposeBrush(ref Brush_PowerLevel_Empty_Overload);
		    Brush_PowerLevel_Empty_Overload = new SolidBrush(SkinningDefs.TheInstance.GetColorDataGivenDefault("power_graph_background_colour_overload", backColour));
		}

		void UpdatePowerDisplay()
		{
			//int level = this.powerNode.GetIntAttribute("level",0);
			int power_z1_base = this.powerNode.GetIntAttribute("z1_base",0);
			int power_z1_now = this.powerNode.GetIntAttribute("z1_now",0);
			int power_z2_base = this.powerNode.GetIntAttribute("z2_base",0);
			int power_z2_now = this.powerNode.GetIntAttribute("z2_now",0);
			int power_z3_base = this.powerNode.GetIntAttribute("z3_base",0);
			int power_z3_now = this.powerNode.GetIntAttribute("z3_now",0);
			int power_z4_base = this.powerNode.GetIntAttribute("z4_base",0);
			int power_z4_now = this.powerNode.GetIntAttribute("z4_now",0);
			int power_z5_base = this.powerNode.GetIntAttribute("z5_base",0);
			int power_z5_now = this.powerNode.GetIntAttribute("z5_now",0);
			int power_z6_base = this.powerNode.GetIntAttribute("z6_base",0);
			int power_z6_now = this.powerNode.GetIntAttribute("z6_now",0);
			int power_z7_base = this.powerNode.GetIntAttribute("z7_base",0);
			int power_z7_now = this.powerNode.GetIntAttribute("z7_now",0);

			int power_z1 = 100;
			int power_z2 = 100;
			int power_z3 = 100;
			int power_z4 = 100;
			int power_z5 = 100;
			int power_z6 = 100;
			int power_z7 = 100;
			
			if (power_z1_base !=0)
			{
				power_z1 =(power_z1_now*100 / power_z1_base);
			}
			
			if (power_z2_base !=0)
			{
				power_z2 =(power_z2_now*100 / power_z2_base);
			}

			if (power_z3_base !=0)
			{
				power_z3 =(power_z3_now*100 / power_z3_base);
			}
				
			if (power_z4_base !=0)
			{
				power_z4 =(power_z4_now*100 / power_z4_base);
			}

			if (power_z5_base !=0)
			{
				power_z5 =(power_z5_now*100 / power_z5_base);
			}
				
			if (power_z6_base !=0)
			{
				power_z6 =(power_z6_now*100 / power_z6_base);
			}

			if (power_z7_base !=0)
			{
				power_z7 =(power_z7_now*100 / power_z7_base);
			}

			ZoneLevels[0] = power_z1;
			ZoneLevels[1] = power_z2;
			ZoneLevels[2] = power_z3;
			ZoneLevels[3] = power_z4;
			ZoneLevels[4] = power_z5;
			ZoneLevels[5] = power_z6;
			ZoneLevels[6] = power_z7;

			this.Invalidate();
		}

		void powerNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			UpdatePowerDisplay();// Update our display...
		}

		void ZonePowerDisplay_Resize(object sender, EventArgs e)
		{
		}

		public void setDrawEmptyBoxStatus(Boolean newStatus, bool showBorders = true)
		{
			DrawEmptyBoxes = newStatus;
		    this.showBorders = showBorders;
		}

		/// <summary>
		/// Allow other code to draw the graph oin a different alignment
		/// </summary>
		/// <param name="vertmode"></param>
		public void setDrawVertical(bool vertmode)
		{
			DrawVertical = vertmode;
		}

		public virtual void Render_Vertical(Graphics g)
		{
			//g.SmoothingMode = SmoothingMode.AntiAlias;
			//g.CompositingQuality = CompositingQuality.HighQuality;
			//g.PixelOffsetMode = PixelOffsetMode.HighQuality;
			Brush BoxBrush = Brushes.White;

			if (testMode)
			{
				ZoneLevels[0] = 0;
				ZoneLevels[1] = 20;
				ZoneLevels[2] = 40;
				ZoneLevels[3] = 60;
				ZoneLevels[4] = 80;
				ZoneLevels[5] = 100;
				ZoneLevels[6] = 120;
			}

			if (PaintBackground == true)
			{
				//g.FillRectangle(Brushes.Crimson,0,0,this.Width, this.Height);
				g.FillRectangle(this.Brush_Background,0,0,this.Width, this.Height);
			}
			g.DrawString("Power", dispfont, this.Brush_Titles, 45, 17);

			int col_offset = 28;
			int col_width = (this.Width - col_offset) / zoneCount;
			g.DrawString("1", dispfont, this.Brush_Values, col_offset+col_width*0, 154);
			g.DrawString("2", dispfont, this.Brush_Values, col_offset+col_width*1, 154);
			g.DrawString("3", dispfont, this.Brush_Values, col_offset+col_width*2, 154);
			g.DrawString("4", dispfont, this.Brush_Values, col_offset+col_width*3, 154);
			g.DrawString("5", dispfont, this.Brush_Values, col_offset+col_width*4, 154);
			g.DrawString("6", dispfont, this.Brush_Values, col_offset+col_width*5, 154);
			g.DrawString("7", dispfont, this.Brush_Values, col_offset+col_width*6, 154);

			g.DrawString("120", dispfont, this.Brush_Values, 0,(134-(20*5))-8);
			g.DrawString("100", dispfont, this.Brush_Values, 0, (134-(20*4))-8);
			g.DrawString("80", dispfont, this.Brush_Values, 0, (134-(20*3))-8);
			g.DrawString("60", dispfont, this.Brush_Values, 0, (134-(20*2))-8);
			g.DrawString("40", dispfont, this.Brush_Values, 0, (134-(20*1))-8);
			g.DrawString("20", dispfont, this.Brush_Values, 0, (134-(20*0))-8);
			g.DrawString("0", dispfont, this.Brush_Values, 9, (154-(20*0))-8);

			g.DrawLine(Pen_Valuemarks, 26, 134-(20*0), 28, 134-(20*0));
			g.DrawLine(Pen_Valuemarks, 26, 134-(20*1), 28, 134-(20*1));
			g.DrawLine(Pen_Valuemarks, 26, 134-(20*2), 28, 134-(20*2));
			g.DrawLine(Pen_Valuemarks, 26, 134-(20*3), 28, 134-(20*3));
			g.DrawLine(Pen_Valuemarks, 26, 134-(20*4), 28, 134-(20*4));
			g.DrawLine(Pen_Valuemarks, 26, 134-(20*5), 28, 134-(20*5));
			g.DrawLine(Pen_Valuemarks, 26, 154-(20*0), 28, 154-(20*0));
			
			int xoffset = 10;
			int yoffset = 170;


			for (int colstep = 0; colstep < zoneCount; colstep++)
			{
				xoffset = 28;
				yoffset = 150;
				//step throught the 24 boxes of the vertical display
				for (int step =0; step < 24; step++)
				{
					//if the value is higher then display a colored box 
					if ((step*5)<ZoneLevels[colstep])
					{
						BoxBrush = this.Brush_PowerLevel_OK;
						if (step>10)
						{
							BoxBrush = this.Brush_PowerLevel_Overload;
							if ((step>=10)&&(step<16))
							{
								BoxBrush = this.Brush_PowerLevel_High;
							}
							if ((step>=16)&&(step<20))
							{
								BoxBrush = this.Brush_PowerLevel_Warning;
							}
						}
						g.FillRectangle(BoxBrush, xoffset+(colstep*col_width), yoffset-(step*5), col_width-2, 4 + (showBorders ? 0 : 1));
					}
					else
					{
						if (DrawEmptyBoxes)
						{
						    var brush = Brush_PowerLevel_Empty_OK;
						    if (step > 10)
						    {
                                brush = Brush_PowerLevel_Empty_Overload;
						        if ((step >= 10) && (step < 16))
						        {
                                    brush = Brush_PowerLevel_Empty_High;
						        }
						        if ((step >= 16) && (step < 20))
						        {
                                    brush = Brush_PowerLevel_Empty_Warning;
						        }
						    }

                            g.FillRectangle(brush, xoffset + (colstep * col_width), yoffset - (step * 5), col_width - 2, 4 + (showBorders ? 0 : 1));
						}
					}
				}
			}
		}

		public virtual void Render_Horizontal(Graphics g)
		{
			if (testMode)
			{
				ZoneLevels[0] = 0;
				ZoneLevels[1] = 20;
				ZoneLevels[2] = 40;
				ZoneLevels[3] = 60;
				ZoneLevels[4] = 80;
				ZoneLevels[5] = 100;
				ZoneLevels[6] = 120;
			}

			Brush BoxBrush = Brushes.White;
			if (PaintBackground == true)
			{
				g.FillRectangle(Brushes.Crimson,0,0,this.Width, this.Height);
				//g.FillRectangle(this.Brush_Background,0,0,this.Width, this.Height);
			}
			StringFormat sf = new StringFormat(StringFormatFlags.MeasureTrailingSpaces);
			SizeF title_measure = g.MeasureString("Power",dispfont,this.Width,sf);
			SizeF zonenumber_measure = g.MeasureString("6",dispfont,this.Width,sf);


			int TitleHeight = (int)title_measure.Height;
			int TitleWidth = (int)title_measure.Width;
			int ZoneNoWidth = (int)zonenumber_measure.Width;

			g.DrawString("Power", dispfont, this.Brush_Titles, (this.Width/2 - TitleWidth /2) , this.Height-TitleHeight);

			int rowHeight = (this.Height -  (TitleHeight * 2)) / this.zoneCount;
			int powerstepwidth = (this.Width - (((int)(zonenumber_measure.Width)) * 2)) / 24;
			int xoffset =12;
			int yoffset =17;

			g.DrawString("0", dispfont, this.Brush_Titles, xoffset-4, 0);
			g.DrawString("25", dispfont, this.Brush_Titles,  xoffset+(5*(powerstepwidth))-((ZoneNoWidth*2)/2), 0);
			g.DrawString("50", dispfont, this.Brush_Titles,  xoffset+(10*(powerstepwidth))-((ZoneNoWidth*2)/2), 0);
			g.DrawString("75", dispfont, this.Brush_Titles,  xoffset+(15*(powerstepwidth))-((ZoneNoWidth*2)/2), 0);
			g.DrawString("100", dispfont, this.Brush_Titles, xoffset+(20*(powerstepwidth))-((ZoneNoWidth*3)/2), 0);

			for (int zonestep=0; zonestep< this.zoneCount; zonestep++)
			{
				g.DrawRectangle(Pens.Teal, 10,TitleHeight+(rowHeight*zonestep), this.Width-20,rowHeight);
				g.DrawString((zonestep+1).ToString(), dispfont, this.Brush_Titles, 0, TitleHeight+(rowHeight*zonestep)-2);

				for (int powerstep =0; powerstep < 24; powerstep++)
				{
					//if the value is higher then display a colored box 
					if ((powerstep*5)<ZoneLevels[zonestep])
					{
						//Determine which color of box to draw
						BoxBrush = this.Brush_PowerLevel_OK;
						if (powerstep>10)
						{
							BoxBrush = this.Brush_PowerLevel_Overload;
							if ((powerstep>=10)&&(powerstep<16))
							{
								BoxBrush = this.Brush_PowerLevel_High;
							}
							if ((powerstep>=16)&&(powerstep<20))
							{
								BoxBrush = this.Brush_PowerLevel_Warning;
							}
						}
						g.FillRectangle(BoxBrush, xoffset+(powerstep*powerstepwidth), yoffset+(zonestep*rowHeight), (powerstepwidth-1), rowHeight-2);
					}
					else
					{
						//Are we drawing empty boxes 
						if (DrawEmptyBoxes)
						{
							//If yes, then use the supplied empty color
							g.FillRectangle(Brush_PowerLevel_Empty_OK, xoffset+(powerstep*powerstepwidth), yoffset+(zonestep*rowHeight), (powerstepwidth-1), rowHeight-2);
						}
					}
				}
			}
		}

		/// <summary>
		/// Override on Paint
		/// </summary>
		/// <param name="e"></param>
		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			if (DrawVertical)
			{
				Render_Vertical(g);
			}
			else
			{
				Render_Horizontal(g);
			}
		}

	}
}
