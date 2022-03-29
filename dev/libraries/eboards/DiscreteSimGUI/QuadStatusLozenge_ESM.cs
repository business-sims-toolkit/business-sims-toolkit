using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using CoreUtils;
using LibCore;
using Network;
using CommonGUI;

namespace DiscreteSimGUI
{
	/// <summary>
	/// Summary description for QuadStatusLozenge.
	/// </summary>
	public class QuadStatusLozenge_ESM : MonitorItem
	{
		[DllImport("User32.dll", CharSet=CharSet.Auto)]
		protected static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool redraw);

		protected enum VisualState
		{
			UpAndGreen,  
			DownAndRed,
			DenialOfService,
			SecurityFlaw,
			ComplianceIncident,
			UpByMirrorAmber,
			RebootingCyan,
			UpDueToWorkAroundBlue,
			GoingDownPurple,
			AutoRestoreTickingDown
		}

		protected VisualState visualState = VisualState.UpAndGreen;

		protected int default_width;
		protected int default_height;
        protected int image_width = SkinningDefs.TheInstance.GetIntData("esm_lozenges_width", 65);
        protected int image_height = SkinningDefs.TheInstance.GetIntData("esm_lozenges_height", 40);

		protected bool showSLAState = true;

		List<Point> maskBoundingPoints;

		public void SetDefaultWidthAndHeight(int w, int h)
		{
			default_width = w;
			default_height = h;

			this.Size = new Size(w,h);
		}

		/// <summary>
		/// Whether this item can be fixed
		/// </summary>
		public bool fixable = false;

		/// <summary>
		/// Whether a workaround can be applied
		/// </summary>
		public bool canWorkAround = false;

	

		protected bool _blank;
		protected Node monitoredItem;
		protected Node monitoredChild;
		protected Node node_workaroundcount; 
		protected Node node_awt;

		protected float fontSize = 10.0f;
		
		protected Image backGraphic;
		protected Font infoFont;

		protected bool slabreach = false;
		protected bool apply_workaround_display_offset = false;

		protected int flashCount = 0;
		protected StopControlledTimer timer;

		protected string info_stores= "";
		protected string info_status= "";
		protected int workaround_count = 0;

		protected Node fixItQueue;

		protected Brush brush;
		protected Boolean PaintBackground = true;
		protected Color LegendTextColor = Color.White;
		protected int ToolTipOffsetY = 60;

        protected Color lozengesLightRed = SkinningDefs.TheInstance.GetColorData("esm_lozenges_red_colour");
        protected Color lozengesDarkRed = SkinningDefs.TheInstance.GetColorData("esm_lozenges_darkred_colour");
        protected Color lozengesLightBlue = SkinningDefs.TheInstance.GetColorData("esm_lozenges_lightblue_colour");
        protected Color lozengesBlue = SkinningDefs.TheInstance.GetColorData("esm_lozenges_blue_colour");
        protected Color lozengesPurple = SkinningDefs.TheInstance.GetColorData("esm_lozenges_purple_colour");
        protected Color lozengesOrange = SkinningDefs.TheInstance.GetColorData("esm_lozenges_orange_colour");
        protected Color lozengesGrey = SkinningDefs.TheInstance.GetColorData("esm_lozenges_grey_colour");
        protected Color lozengesBackgroundColor = SkinningDefs.TheInstance.GetColorData("esm_main_screen_background_colour");
        protected Color lozengesCurrentStateColor;
        protected bool isTimerRunning = false;
		float? maxTimerValue;
        Image iconAlt;
	    
        public virtual void base_work(Boolean UseMaskPath)
		{
			//this.BackColor = Color.FromArgb(198,199,201);
			this.BackColor = Color.FromArgb(27,53,155);
			brush = new SolidBrush( this.BackColor );

			if (PaintBackground == false)
			{
				this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
				this.SetStyle(ControlStyles.DoubleBuffer,true);
				this.SetStyle(ControlStyles.UserPaint, true);
			}
			else
			{
				this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
				this.SetStyle(ControlStyles.DoubleBuffer,true);
				this.SetStyle(ControlStyles.UserPaint, true);
			}
			this.infoFont = CoreUtils.SkinningDefs.TheInstance.GetFont(fontSize, FontStyle.Bold);


			//Preliminarey Code for masking to 2 rounded Rects 
			if (UseMaskPath)
			{
				DoMask();
			}
		}

		protected virtual void DoMask ()
		{
			Point[] pp = getMaskBoundingPolygon();
			
			GraphicsPath mPath = new GraphicsPath(); 
			mPath.AddPolygon(pp);
			//crreate a region from the Path 
			Region region = new Region(mPath);
			//create a graphics object 
			Graphics graphics = this.CreateGraphics();
			//get the handle to the region 
			IntPtr ptr = region.GetHrgn(graphics);
			//Call the Win32 window region code 
			SetWindowRgn((IntPtr)this.Handle, ptr, true);
		}

		protected virtual void SetupDefaultMaskBounds ()
		{
			Point[] pp = new Point[8];
			pp[0] = new Point(0, 7);
			pp[1] = new Point(1 + 38, 7);
			pp[2] = new Point(1 + 38 + 4, 2);
			pp[3] = new Point(110, 2);
			pp[4] = new Point(110, 42);
			pp[5] = new Point(1 + 38 + 5, 42);
			pp[6] = new Point(1 + 38, 38);
			pp[7] = new Point(0, 38);

			SetMaskBoundingPolygon(pp);
		}

		public virtual Point [] getMaskBoundingPolygon ()
		{
			return maskBoundingPoints.ToArray();
		}

		public void SetMaskBoundingPolygon (Point [] points)
		{
			maskBoundingPoints = new List<Point> (points);
			DoMask();
		}

		public QuadStatusLozenge_ESM(Boolean UseMaskPath)
		{
			base_work(UseMaskPath);
            lozengesCurrentStateColor = lozengesBackgroundColor; //default value of currentstate

			string loc = AppInfo.TheInstance.Location + "\\";
			//backGraphic = Repository.TheInstance.GetImage(loc + "\\images\\lozenges\\lozenge_grey.png");
		    icon = Repository.TheInstance.GetImage(loc + "\\images\\icons\\Icon_Blank.png");
			icon.Tag = null; // Throws if it doesn't exist!
		    iconAlt = GetAltIcon();
			this._blank = true;
			padTop = 2;

            lozengesCurrentStateColor = lozengesGrey;
			SetupDefaultMaskBounds();

			// Check when we mouse over so that we can display fix and workaround buttons as required...
//			this.MouseEnter += new EventHandler(MouseEnter);
//			this.MouseLeave += new EventHandler(MouseLeave);
//			this.MouseDown  += new MouseEventHandler(MouseDown);

			timer = new StopControlledTimer();
			timer.Interval = 1000;
			timer.Tick += _timer_Tick;
		}
		/// <summary>
		/// Creates a status lozenge for a service as seen on the racing
		/// screen
		/// </summary>
		/// <param name="n"></param>
		/// <param name="r"></param>
		public QuadStatusLozenge_ESM(Node n, Random r, Boolean UseMaskPath)
		{
			base_work(UseMaskPath);

            lozengesCurrentStateColor = lozengesBackgroundColor; //default value of currentstate;

			this._blank = false;
			monitoredItem = n;

			int randTime = r.Next(200) - 100;

			timer = new StopControlledTimer();
			timer.Interval = 900 + randTime;
			timer.Tick += _timer_Tick;

			desc = monitoredItem.GetAttribute("desc");
			shortdesc = monitoredItem.GetAttribute("shortdesc");
			iconname = monitoredItem.GetAttribute("icon");
			GetIcon();

		    iconAlt = GetAltIcon();


			padTop = 2;

            // Get the FixItQueue
			fixItQueue = monitoredItem.Tree.GetNamedNode("FixItQueue");

			// Watch for changes on the monitored item 
			monitoredItem.AttributesChanged += monitoredItem_AttributesChanged;
			//monitoredItem.ChildRemoved +=new Network.Node.NodeChildRemovedEventHandler(monitoredItem_ChildRemoved);
			//monitoredItem.ChildAdded +=new Network.Node.NodeChildAddedEventHandler(monitoredItem_ChildAdded);

			ArrayList al = monitoredItem.getChildren();
			if (al.Count>0)
			{
				monitoredChild = (Node)al[0];
				monitoredChild.ParentChanged +=monitoredChild_ParentChanged;
			}
			else
			{
				monitoredChild = null;
			}

			//need to connect to golbal workaround count 
			node_workaroundcount = monitoredItem.Tree.GetNamedNode("AppliedWorkArounds");
			node_workaroundcount.AttributesChanged +=node_workaroundcount_AttributesChanged;
			workaround_count = node_workaroundcount.GetIntAttribute("num",0);

			//Need to watch the Advanced Warning Technology node to switch on the display of the down time
			node_awt = monitoredItem.Tree.GetNamedNode("AdvancedWarningTechnology");
			//node_awt.AttributesChanged +=new Network.Node.AttributesChangedEventHandler(node_awt_AttributesChanged);
			//awt_system_on = node_awt.GetBooleanAttribute("enabled",false);

			// Check when we mouse over so that we can display fix and workaround buttons as required...
			this.MouseEnter += MouseEnterHandler;
			this.MouseLeave += MouseLeaveHandler;
			this.MouseDown  += MouseDownHandler;

			CalculateState(true);
            
		}

		public Node getMonitoredNode()
		{
			return monitoredItem;
		}

		/// <summary>
		/// Need to be able to detach from 
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			if(disposing)
			{
				//detach from all monitered nodes 
				//if(!_blank)
				//{
				if (monitoredItem != null)
				{
					monitoredItem.AttributesChanged -= monitoredItem_AttributesChanged;
				}
				if (monitoredChild != null)
				{
					monitoredChild.ParentChanged -=monitoredChild_ParentChanged;
				}
				//}

				if (node_workaroundcount != null)
				{
					node_workaroundcount.AttributesChanged -=node_workaroundcount_AttributesChanged;
				}
				if (node_awt != null)
				{
					node_awt = null;
				}
				timer.Stop();
			}
			base.Dispose (disposing);
		}

		/// <summary>
		/// Set node to be monitored
		/// </summary>
		/// <param name="NewMonitoredItem"></param>
		public void setMonitoredNode(Node NewMonitoredItem)
		{
			node_awt = NewMonitoredItem.Tree.GetNamedNode("AdvancedWarningTechnology");

			//detach the current one 
			if(monitoredItem != null)
			{
				monitoredItem.AttributesChanged -= monitoredItem_AttributesChanged;
			}

			if(node_workaroundcount != null)
			{
				node_workaroundcount.AttributesChanged -= node_workaroundcount_AttributesChanged;
				node_workaroundcount = null;
			}
			//
			if (NewMonitoredItem != null)
			{
				//Connect to the new one
				monitoredItem = NewMonitoredItem;
				this._blank = false;
				desc = monitoredItem.GetAttribute("desc");
				shortdesc = monitoredItem.GetAttribute("shortdesc");
				iconname = monitoredItem.GetAttribute("icon");
				NewMonitoredItem.AttributesChanged += monitoredItem_AttributesChanged;

				// Get the FixItQueue
				fixItQueue = monitoredItem.Tree.GetNamedNode("FixItQueue");

				//need to connect to golbal workaround count 
				node_workaroundcount = monitoredItem.Tree.GetNamedNode("AppliedWorkArounds");
				node_workaroundcount.AttributesChanged +=node_workaroundcount_AttributesChanged;
				workaround_count = node_workaroundcount.GetIntAttribute("num",0);

				CalculateState(true);
				GetIcon();
                iconAlt = GetAltIcon();
				Invalidate(); //Refresh();
			}
			else
			{
				monitoredItem = null;
				//backGraphic = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\lozenges\\lozenge_grey.png");
				icon = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\icons\\Icon_Blank.png");
                iconAlt = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\icons\\Icon_Blank_alt.png");
				this._blank = true;
				padTop = 2;
                lozengesCurrentStateColor = lozengesGrey;
                Invalidate(); //Refresh();
			}
		}

		void _timer_Tick(object sender, EventArgs e)
		{
			if(this.flashCount <= -2)
			{
				timer.Stop();
			}
			else
			{
				--flashCount;
				SetState();
				this.Invalidate();
			}
		}


		public void SetBackgroundColor(Color c1)
		{
			this.BackColor = c1;
			brush = new SolidBrush( this.BackColor );
		}


		/// <summary>
		/// Override on Paint
		/// </summary>
		/// <param name="e"></param>
		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			Render(g);
		}

		Rectangle ChangeRect(RectangleF rf)	
		{
			Rectangle r = new Rectangle(0,0,0,0);

			r.Location = new Point((int)rf.Left, (int)rf.Top);
			r.Size = new Size((int)rf.Width, (int)rf.Height);

			return r;
		}


		/// <summary>
		/// Render the status monitor item.
		/// </summary>
		/// <param name="g"></param>
		public virtual void Render(Graphics g)
		{
		    if (PaintBackground == true)
			{
                brush = new SolidBrush(lozengesBackgroundColor);   
				g.FillRectangle(brush,0,0,this.Width, this.Height);
			}
			g.SmoothingMode = SmoothingMode.AntiAlias;
			g.CompositingQuality = CompositingQuality.HighQuality;
			g.PixelOffsetMode = PixelOffsetMode.HighQuality;

			RectangleF textRect_stores;

            int borderAdjustment = SkinningDefs.TheInstance.GetIntData("esm_lozenges_border_width");
            int x = SkinningDefs.TheInstance.GetIntData("esm_lozenges_pie_fill_X_adjustment");
            int y = SkinningDefs.TheInstance.GetIntData("esm_lozenges_pie_fill_Y_adjustment");
            float startPoint = SkinningDefs.TheInstance.GetFloatData("esm_lozenges_pie_fill_startpoint", 0);

			float? currentTimeSecs = GetTimeInSecondsFromStatus(info_status);

		    if (_blank)
		    {
			    using (Brush brush = new SolidBrush(lozengesCurrentStateColor))
			    {
				    g.FillEllipse(brush, x, y, Size.Width - borderAdjustment, Size.Height - borderAdjustment);
			    }
			    g.DrawImage(icon, 0, 0, Size.Width, Size.Height);
			    currentTimeSecs = null;
		    }
            else if (visualState == VisualState.UpAndGreen)
            {
                g.DrawImage(icon, 0, 0, Size.Width, Size.Height);
				currentTimeSecs = null;
			}
            else
            {
                textRect_stores = CalculateStoresTextLocation();

	            if (this.info_status != null && this.info_status.Length > 0)
	            {
		            using (Brush brush = new SolidBrush(lozengesCurrentStateColor))
		            {
			            g.FillEllipse(brush, x, y, Size.Width - borderAdjustment, Size.Height - borderAdjustment);
		            }

		            if ((maxTimerValue == null) || (currentTimeSecs > maxTimerValue))
		            {
			            maxTimerValue = currentTimeSecs;
		            }

		            float sweepAngle = 360 * ((maxTimerValue.Value - currentTimeSecs.Value) / maxTimerValue.Value);

		            if (visualState == VisualState.UpDueToWorkAroundBlue)
		            {
			            Color colour = lozengesLightRed;
			            if (showSLAState && slabreach)
			            {
				            colour = lozengesOrange;
			            }

			            using (Brush brush = new SolidBrush(colour))
			            {
				            g.FillPie(brush, x, y, Size.Width - borderAdjustment, Size.Height - borderAdjustment, startPoint,
					            sweepAngle);
			            }
		            }
		            else
		            {
			            using (Brush brush = new SolidBrush(lozengesBackgroundColor))
			            {
				            g.FillPie(brush, x, y, Size.Width - borderAdjustment,
					            Size.Height - borderAdjustment, startPoint, sweepAngle);
			            }
		            }
		            g.DrawImage(iconAlt, 0, 0, Size.Width, Size.Height);
	            }
	            else
	            {
		            using (Brush brush = new SolidBrush(lozengesCurrentStateColor))
					{
			            g.FillEllipse(brush, x, y, Size.Width - borderAdjustment, Size.Height - borderAdjustment);
		            }

		            g.DrawImage(iconAlt, 0, 0, Size.Width, Size.Height);
	                maxTimerValue = null;
                }

                if ((int) visualState != (int) VisualState.UpAndGreen)
                {
                    if (this.info_stores != null && this.info_stores.Length > 0)
                    {
                        StringFormat sf = CalculateStoresTextFormatting();

	                    using (Brush brush = new SolidBrush(LegendTextColor))
	                    {
							g.DrawString(info_stores, infoFont, brush, textRect_stores, sf);
	                    }
                    }
                }
            }
		}

		/// <summary>
		/// Converts the specified number of seconds
		/// to mins:secs format.
		/// </summary>
		/// <param name="ticks"></param>
		/// <returns></returns>
		protected string ConvertToTime(int ticks)
		{
			if(ticks == 0) return "";

			int mins = ticks / 60;
			int secs = ticks- (mins * 60);

			return CONVERT.ToStr(mins) + ":" + CONVERT.ToStr(secs).PadLeft(2,'0');
		}

		/// <summary>
		/// Now setting text color as well 
		/// </summary>
		public virtual void SetState()
		{
			string loc = AppInfo.TheInstance.Location + "\\";
			switch(visualState)
			{
				case VisualState.UpAndGreen:
					//backGraphic = Repository.TheInstance.GetImage(loc + "\\images\\lozenges\\lozenge_green.png");
                    this.LegendTextColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("lozenge_text_green", Color.White);
			        lozengesCurrentStateColor = lozengesBackgroundColor; //we keep default color as background not green.
				break;

				case VisualState.AutoRestoreTickingDown:
					if(this.flashCount <= 0)
					{
						//backGraphic = Repository.TheInstance.GetImage(loc + "\\images\\lozenges\\lozenge_saas_light.png");
						this.LegendTextColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("lozenge_text_saas_light", Color.Black);
					}
					else
					{
						if(flashCount%2 == 1)
						{
							//backGraphic = Repository.TheInstance.GetImage(loc + "\\images\\lozenges\\lozenge_saas.png");
							this.LegendTextColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("lozenge_text_saas", Color.Black);
						}
						else
						{
							//backGraphic = Repository.TheInstance.GetImage(loc + "\\images\\lozenges\\lozenge_saas_light.png");
							this.LegendTextColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("lozenge_text_saas_light", Color.Black);
						}
					}
					break;

				case VisualState.ComplianceIncident:
				case VisualState.SecurityFlaw:
				case VisualState.DenialOfService:
				case VisualState.DownAndRed:
					if(this.flashCount <= 0)
					{
						if(this.slabreach && showSLAState)
						{
							//backGraphic = Repository.TheInstance.GetImage(loc + "\\images\\lozenges\\lozenge_hatch.png");
                            this.LegendTextColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("lozenge_text_hatch", Color.Black);
						    lozengesCurrentStateColor = lozengesOrange;
						}
						else
						{
							//backGraphic = Repository.TheInstance.GetImage(loc + "\\images\\lozenges\\lozenge_lightred.png");
							this.LegendTextColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("lozenge_text_lightred", Color.White);
                            lozengesCurrentStateColor = lozengesLightRed; 
						}
					}
					else
					{
						if(flashCount%2 == 1)
						{
							if(this.slabreach && showSLAState)
							{
								//backGraphic = Repository.TheInstance.GetImage(loc + "\\images\\lozenges\\lozenge_hatch.png");
								this.LegendTextColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("lozenge_text_hatch", Color.Black);
                                lozengesCurrentStateColor = lozengesOrange; 
							}
							else
							{
								//backGraphic = Repository.TheInstance.GetImage(loc + "\\images\\lozenges\\lozenge_darkred.png");
								this.LegendTextColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("lozenge_text_darkred", Color.White);
                                lozengesCurrentStateColor = lozengesDarkRed; 
							}
						}
						else
						{
							//backGraphic = Repository.TheInstance.GetImage(loc + "\\images\\lozenges\\lozenge_lightred.png");
							this.LegendTextColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("lozenge_text_lightred", Color.White);
                            lozengesCurrentStateColor = lozengesLightRed; 
						}
					}
					break;

				case VisualState.GoingDownPurple:
					//backGraphic = Repository.TheInstance.GetImage(loc + "\\images\\lozenges\\lozenge_purple.png");
					this.LegendTextColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("lozenge_text_purple", Color.White);
                    lozengesCurrentStateColor = lozengesPurple; 
					break;

				case VisualState.RebootingCyan:
					//backGraphic = Repository.TheInstance.GetImage(loc + "\\images\\lozenges\\lozenge_lilac.png");
					this.LegendTextColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("lozenge_text_lilac", Color.White);
                    lozengesCurrentStateColor = lozengesPurple; 
					break;

				case VisualState.UpByMirrorAmber:
					//backGraphic = Repository.TheInstance.GetImage(loc + "\\images\\lozenges\\lozenge_amber.png");
					this.LegendTextColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("lozenge_text_amber", Color.White);
                    lozengesCurrentStateColor = lozengesOrange; 
					break;

				case VisualState.UpDueToWorkAroundBlue:
					//backGraphic = Repository.TheInstance.GetImage(loc + "\\images\\lozenges\\lozenge_cyan.png");
					this.LegendTextColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("lozenge_text_cyan", Color.White);
                    lozengesCurrentStateColor = lozengesBlue; 
					break;
			}
		}

		public virtual void CalculateState(bool do_klaxon)
		{
			bool awt_system_on = node_awt.GetBooleanAttribute("enabled",false);
			LegendTextColor = Color.White;

			VisualState oldVisualState = visualState;

			apply_workaround_display_offset = false;
			//handle the case where we are not connected.
			if (monitoredItem == null)
			{
				return; 
			}

			visualState = VisualState.UpAndGreen;
			this.info_status = "";
			this.info_stores = monitoredItem.GetAttribute("users_down");

			canWorkAround = monitoredItem.GetBooleanAttribute("canworkaround", false);

			if("true" == monitoredItem.GetAttribute("slabreach"))
			{
				this.slabreach = true;
				LegendTextColor = Color.Black;
			}
			else
			{
				this.slabreach = false;
				LegendTextColor = Color.White;
			}

			//
			if("true" == monitoredItem.GetAttribute("fixable"))
			{
				this.fixable = true;
			}
			else
			{
				this.fixable = false;
			}
			// If we are down we are red...
			if(! monitoredItem.GetBooleanAttribute("up", false))
			{
				// If we are rebooting then we are lilac...
				// Check our rebooting status...
				string rebooting = monitoredItem.GetAttribute("rebootingForSecs");
				if( ("" == rebooting) || ("0" == rebooting) )
				{
					// We are not rebooting...
					// Therefore we should be red but if there is no impact on our service we lie and say
					// that the service is up.
					//string impactkmh = monitoredItem.GetAttribute("impactkmh");
					//string impactsecsinpit = monitoredItem.GetAttribute("impactsecsinpit");
					Boolean hasImpact = monitoredItem.GetBooleanAttribute("has_impact",false);
						
					//OLD CODE 
					//if( ( (impactkmh != "") && (impactkmh != "0") ) ||
					//	( (impactsecsinpit != "") && (impactsecsinpit != "0") ) )
					if (hasImpact)
					{
						if(monitoredItem.GetBooleanAttribute("denial_of_service",false))
						{
							visualState = VisualState.DenialOfService;
						}
						else if (monitoredItem.GetBooleanAttribute("security_flaw", false))
						{
							visualState = VisualState.SecurityFlaw;
						}
						else if (monitoredItem.GetBooleanAttribute("compliance_incident", false))
						{
							visualState = VisualState.ComplianceIncident;
						}
						else if (monitoredItem.GetBooleanAttribute("is_saas", false))
						{
							visualState = VisualState.AutoRestoreTickingDown;
						}
						else
						{
							// We must be red.
							visualState = VisualState.DownAndRed;
						}

						// How long have we been down for?...
						int downFor = monitoredItem.GetIntAttribute("downforsecs",-1);
						if(-1 != downFor)
						{
							this.info_status = ConvertToTime(downFor);
						}
						//we only show the down for time, if awt is switched on
						if (awt_system_on==false)
						{
							this.info_status = "";
						}

						if (visualState == VisualState.AutoRestoreTickingDown)
						{
							info_status = CONVERT.FormatTime(monitoredItem.GetIntAttribute("auto_restore_time", 0) - monitoredItem.GetIntAttribute("downforsecs", 0));
						}
					}
					else
					{
						// We are down but have no impact so lie and say that we are up...
						visualState = VisualState.UpAndGreen;
						// Since we are lying don't say that we can fix anything....
						this.fixable = false;
						this.canWorkAround = false;
					}
				}
				else
				{
					// We are rebooting so show for how long...
					this.info_status = ConvertToTime( CONVERT.ParseInt( rebooting ) );
					visualState = VisualState.RebootingCyan;
				}
			}
			else
			{
				// : 9-4-2007 :  says that we don't show going down in the lozenge any more
				// as this is the AWT monitor tool's job.
				// : 18-04-2007 : has now decided that we do show going down but we
				// show it as red.
				//
				string gd = monitoredItem.GetAttribute("goingDownInSecs");
				//
				if( (gd != "") && (gd != "0") )
				{
					// We must show purple.
					//visualState = VisualState.GoingDownPurple;
					if(monitoredItem.GetBooleanAttribute("denial_of_service",false))
					{
						visualState = VisualState.DenialOfService;
					}
					else if (monitoredItem.GetBooleanAttribute("security_flaw", false))
					{
						visualState = VisualState.SecurityFlaw;
					}
					else if (monitoredItem.GetBooleanAttribute("compliance_incident", false))
					{
						visualState = VisualState.ComplianceIncident;
					}
					else
					{
						visualState = VisualState.DownAndRed;
					}
				}
				else
				{
					int wa = monitoredItem.GetIntAttribute("workingAround",0);
					//string cwa = monitoredItem.GetAttribute("canworkaround");

					if( /*(cwa != "true") ||*/ (wa == 0) )
					{
						// We are not up due to a workaround.
						if(monitoredItem.GetBooleanAttribute("show_as_upgrading", false))
						{
							visualState = VisualState.RebootingCyan;

							CheckIfRebooting(awt_system_on);
						}
						else if( monitoredItem.GetAttribute("upByMirror") == "true")
						{
							List<Node> apps = new List<Node> ();
							foreach (LinkNode link in monitoredItem.GetChildrenOfType("Connection"))
							{
								Node bsu = link.To;

								if (bsu != null)
								{
									foreach (LinkNode backLink in bsu.BackLinks)
									{
										if (!backLink.GetBooleanAttribute("ismirror", false))
										{
											Node app = backLink.From;
											if (app.GetAttribute("type") == "App")
											{
												if (!apps.Contains(app))
												{
													apps.Add(app);
												}
											}
										}
									}
								}
							}

							bool allAppsAreVirtual = true;
							bool allAppsAreRebooting = true;
							foreach (Node app in apps)
							{
								Node server = app.Parent;

								if (! server.GetBooleanAttribute("is_virtual", false))
								{
									allAppsAreVirtual = false;
								}

								if (server.GetIntAttribute("rebootingforsecs", 0) == 0)
								{
									allAppsAreRebooting = false;
								}
							}

							if ((apps.Count > 0) && allAppsAreVirtual && allAppsAreRebooting)
							{
								visualState = VisualState.RebootingCyan;
							}
							else
							{
								// We should show Amber
								visualState = VisualState.UpByMirrorAmber;
							}

							// Nasty bodge for CASE2 where if you virtualise while an incident is active, the
							// canworkaround flag doesn't get cleared.
							if (allAppsAreVirtual)
							{
								canWorkAround = false;
							}

							CheckIfRebooting(awt_system_on);
						}

						// If we're only partially up, use the amber status too.
						switch (monitoredItem.GetAttribute("team_status").ToLower())
						{
							case "none":
								visualState = VisualState.DownAndRed;
								break;

							case "coreonly":
								visualState = VisualState.UpByMirrorAmber;
								break;

							case "full":
								visualState = VisualState.UpAndGreen;
								break;
						}
					}
					else
					{
						// We are only up due to a workaround.
						visualState = VisualState.UpDueToWorkAroundBlue;
						this.info_status = ConvertToTime( wa );
						//add on the workaround count 
						this.info_status = this.info_status + "  " + CONVERT.ToStr(workaround_count);
						//mark for x shift to allow display 
						apply_workaround_display_offset = true;
					}
				}
			}
			//
			SetState();
			//

			if(do_klaxon)
			{
				// : fix for 4181 (don't play klaxon at start of round).
				Node timeNode = monitoredItem.Tree.GetNamedNode("CurrentTime");
				bool atStart = false;
				if ((timeNode != null) && (timeNode.GetAttribute("seconds") == "0"))
				{
					atStart = true;
				}

				if ((! atStart)
				    && (visualState != oldVisualState)
					&& ((visualState == VisualState.DownAndRed) ||
				       (visualState == VisualState.UpByMirrorAmber) ||
				       (visualState == VisualState.DenialOfService) ||
					   (visualState == VisualState.SecurityFlaw) ||
					   (visualState == VisualState.ComplianceIncident) ||
					   (visualState == VisualState.AutoRestoreTickingDown)))
				{
					// Keep klaxon.
					string file = AppInfo.TheInstance.Location + "\\audio\\alarm.wav";
					KlaxonSingleton.TheInstance.PlayAudio( file, false );
					flashCount = 60;
					timer.Start();
				}
			}
				/*
			else if(do_klaxon && (visualState == VisualState.UpByMirrorAmber))
			{
				string file = AppInfo.TheInstance.Location + "\\audio\\alarm.wav";
				KlaxonSingleton.TheInstance.PlayAudio( file, false );
				// Keep klaxon.
			}*/
		}

		void CheckIfRebooting(bool awt_system_on)
		{
			// We may have one of our servers rebooting so we should show that state...
			string rebooting = monitoredItem.GetAttribute("rebootingForSecs");
			if (("" == rebooting) || ("0" == rebooting))
			{
				// Nope, no reboot.
				//int downFor = monitoredItem.GetIntAttribute("downforsecs",-1);
				int downFor = monitoredItem.GetIntAttribute("mirrorforsecs", -1);
				if (-1 != downFor)
				{
					this.info_status = ConvertToTime(downFor);
				}
				//we only show the down for time, if awt is switched on
				if (awt_system_on == false)
				{
					this.info_status = "";
				}
			}
			else
			{
				// Yes, something is rebooting...
				this.info_status = ConvertToTime(CONVERT.ParseInt(rebooting));
			}
		}

		protected virtual void Handle_monitoredItem_AttributesChanged(Node sender, ArrayList attrs)
		{
			bool do_klaxon = false;

			foreach (AttributeValuePair avp in attrs)
			{
				//handling changed description for upgrade applied mid race
				if (avp.Attribute == "desc")
				{
					desc = monitoredItem.GetAttribute("desc");
				}
				else if (avp.Attribute == "shortdesc")
				{
					shortdesc = monitoredItem.GetAttribute("shortdesc");
				}
				else if (avp.Attribute == "retired")
				{
				}
				else if ((avp.Attribute == "upByMirror") && (avp.Value == "true"))
				{
					//now doing klaxon
					do_klaxon = true;
				}
				else if ((avp.Attribute == "up") && (avp.Value == "false"))
				{
					do_klaxon = true;
				}
			}
			CalculateState(do_klaxon);
			this.Invalidate();
		}


		protected void monitoredItem_AttributesChanged(Node sender, ArrayList attrs)
		{
			Handle_monitoredItem_AttributesChanged(sender, attrs);
		}

		public virtual void MouseEnterHandler(object sender, EventArgs e)
		{
			if(this.monitoredItem != null)
			{
				if(!ItilToolTip_Quad.TheInstance.ShowFullPanel)
				{
					int x = (this.Parent.Location.X > 900) ? 900 : this.Parent.Location.X;

					ItilToolTip_Quad.TheInstance.ShowToolTip_Quad(this, this.fixItQueue, LibCore.Strings.RemoveHiddenText(desc.Replace("\r\n", " ")),
						monitoredItem.GetAttribute("incident_id"),
						(this.Location.X + x), (this.Location.Y + this.Parent.Location.Y) + ToolTipOffsetY);

					ItilToolTip_Quad.TheInstance.BringToFront();
				}

				//CalculateState(false);
			}
		}

		public virtual void MouseLeaveHandler(object sender, EventArgs e)
		{
			if(this.monitoredItem != null)
			{
				if(!ItilToolTip_Quad.TheInstance.ShowFullPanel)
				{
					ItilToolTip_Quad.TheInstance.HideToolTip();
					//this.Refresh();
				}
			}
		}

		public virtual void MouseDownHandler(object sender, MouseEventArgs e)
		{
			if(this.monitoredItem != null)
			{
				ItilToolTip_Quad.TheInstance.toggleShow();
			}
		}

		/// <summary>
		/// Catching the increase of the Global Work Around count
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="attrs"></param>
		protected void node_workaroundcount_AttributesChanged(Node sender, ArrayList attrs)
		{
			if (attrs != null && attrs.Count > 0)
			{
				foreach(AttributeValuePair avp in attrs)
				{
					if (avp.Attribute == "num")
					{
						workaround_count = node_workaroundcount.GetIntAttribute("num",0);
					}
				}
			}
		}

//		private void monitoredItem_ChildAdded(Node sender, Node child)
//		{
//			if (this.monitoredChild == null)
//			{
//				this.monitoredChild = child;
//				this.monitoredChild.ParentChanged +=new Network.Node.NodeChildAddedEventHandler(monitoredChild_ParentChanged);
//			}
//		}

		protected void monitoredChild_ParentChanged(Node sender, Node child)
		{
			//The Parent of our monitored child is Changed 
			//So we need to changed our monitored item to the new Parent.
			Node NewParent = child.Parent;
			string namestr = NewParent.GetAttribute("name");
			this.setMonitoredNode(NewParent);
		}

		protected virtual RectangleF CalculateStoresTextLocation ()
		{
			RectangleF textRect_stores;
            int startXCoord = SkinningDefs.TheInstance.GetIntData("esm_lozenges_text_X");
            int startYCoord = SkinningDefs.TheInstance.GetIntData("esm_lozenges_text_Y");
            int widthPadding = SkinningDefs.TheInstance.GetIntData("esm_lozenges_text_width_padding");
            int heightPadding = SkinningDefs.TheInstance.GetIntData("esm_lozenges_text_height_padding");

			if (apply_workaround_display_offset)
			{
				//textRect_stores = new RectangleF(padLeft + 23, padTop + 3, Size.Width - padLeft - padRight - 25, Size.Height - padTop - padBottom);
                textRect_stores = new RectangleF(startXCoord, startYCoord, Size.Width - widthPadding, Size.Height - heightPadding);
				//textRect_stores.Offset(0, +1.0f);
			}
			else
			{
				textRect_stores = new RectangleF(startXCoord, startYCoord, Size.Width - widthPadding, Size.Height - heightPadding);
		    }

			return textRect_stores;
		}

	    protected float? GetTimeInSecondsFromStatus (string status)
	    {
			if (status.Contains(":"))
			{
				string timePart = status.Split(" ".ToCharArray())[0];
				float min = Convert.ToSingle(timePart.Split(":".ToCharArray())[0]);
				float sec = Convert.ToSingle(timePart.Split(":".ToCharArray())[1]);

				return min * 60 + sec;
			}
			else
			{
				return null;
			}
	    }

	    protected virtual RectangleF CalculateStatusTextLocation ()
		{
			RectangleF textRect_status;
            int startXCoord = SkinningDefs.TheInstance.GetIntData("esm_lozenges_status_X");
            int startYCoord = SkinningDefs.TheInstance.GetIntData("esm_lozenges_status_Y");
            int widthPadding = SkinningDefs.TheInstance.GetIntData("esm_lozenges_status_width_padding");
            int heightPadding = SkinningDefs.TheInstance.GetIntData("esm_lozenges_status_height_padding");

			if (apply_workaround_display_offset)
			{
                textRect_status = new RectangleF(startXCoord, startYCoord, Size.Width - widthPadding, Size.Height - heightPadding);
				
			}
			else
			{
                textRect_status = new RectangleF(startXCoord, startYCoord, Size.Width - widthPadding, Size.Height - heightPadding);
			}

			return textRect_status;
		}

		protected virtual StringFormat CalculateStatusTextFormatting ()
		{
			StringFormat sf = new StringFormat(StringFormatFlags.NoClip);
			sf.Trimming = StringTrimming.EllipsisCharacter;
			sf.LineAlignment = StringAlignment.Center;
			sf.Alignment = StringAlignment.Center;

			return sf;
		}

		protected virtual StringFormat CalculateStoresTextFormatting ()
		{
			StringFormat sf = new StringFormat(StringFormatFlags.NoClip);
			sf.Trimming = StringTrimming
                .EllipsisCharacter;
			sf.LineAlignment = StringAlignment.Center;
			sf.Alignment = StringAlignment.Center;

			return sf;
		}
	}
}