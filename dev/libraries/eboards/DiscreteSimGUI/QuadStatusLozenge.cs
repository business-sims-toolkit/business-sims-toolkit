using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

using CommonGUI;
using CoreUtils;
using LibCore;
using Network;

namespace DiscreteSimGUI
{
	public enum VisualState
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

	/// <summary>
	/// Summary description for QuadStatusLozenge.
	/// </summary>
	public class QuadStatusLozenge : MonitorItem
	{
		[DllImport("User32.dll", CharSet=CharSet.Auto)]
		protected static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool redraw);

		

		protected VisualState visualState = VisualState.UpAndGreen;

		protected int default_width;
		protected int default_height;
		protected int image_width = 65;
		protected int image_height = 40;

		protected bool showSLAState = true;

		List<Point> maskBoundingPoints;

		public void SetDefaultWidthAndHeight(int w, int h)
		{
			default_width = w;
			default_height = h;

			Size = new Size(w,h);
		}

		/// <summary>
		/// Whether this item can be fixed.
		/// </summary>
		public bool fixable = false;
		
		/// <summary>
		/// Whether a workaround can be applied.
		/// </summary>
		public bool canWorkAround = false;
        
		protected bool _blank;
		protected Node monitoredItem;
		protected Node monitoredChild;
		protected Node node_workaroundcount; 
		protected Node node_awt;

	    protected float fontSize = (float) SkinningDefs.TheInstance.GetDoubleData("lozenge_font_size", 10);
		
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

		public virtual void base_work(Boolean UseMaskPath)
		{
			BackColor = Color.FromArgb(27,53,155);
			brush = new SolidBrush( BackColor );

			if (PaintBackground == false)
			{
				SetStyle(ControlStyles.AllPaintingInWmPaint, true);
				SetStyle(ControlStyles.DoubleBuffer,true);
				SetStyle(ControlStyles.UserPaint, true);
			}
			else
			{
				SetStyle(ControlStyles.AllPaintingInWmPaint, true);
				SetStyle(ControlStyles.DoubleBuffer,true);
				SetStyle(ControlStyles.UserPaint, true);
			}
			infoFont = SkinningDefs.TheInstance.GetFont(fontSize, FontStyle.Bold);

            // Preliminarey Code for masking to 2 rounded Rects.
			if (UseMaskPath)
			{
				DoMask();
			}

			UpdateVisibility();
		}

		protected virtual void DoMask ()
		{
			Point[] pp = getMaskBoundingPolygon();
			
			GraphicsPath mPath = new GraphicsPath(); 
			mPath.AddPolygon(pp);
			// Create a region from the Path.
			Region region = new Region(mPath);
			// Create a graphics object.
			Graphics graphics = CreateGraphics();
			// Get the handle to the region.
			IntPtr ptr = region.GetHrgn(graphics);
			// Call the Win32 window region code.
			SetWindowRgn(Handle, ptr, true);
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

		public QuadStatusLozenge(Boolean UseMaskPath)
		{
			base_work(UseMaskPath);

			string loc = AppInfo.TheInstance.Location + "\\";
			backGraphic = Repository.TheInstance.GetImage(loc + "\\images\\lozenges\\lozenge_grey.png");
			icon = Repository.TheInstance.GetImage(loc + "\\images\\icons\\Icon_Blank.png");
			icon.Tag = null; // Throws if it doesn't exist!
			_blank = true;
			padTop = 2;

			SetupDefaultMaskBounds();

			timer = new StopControlledTimer();
			timer.Interval = 1000;
			timer.Tick += _timer_Tick;
		}

		/// <summary>
		/// Creates a status lozenge for a service as seen on the racing screen.
		/// </summary>
		public QuadStatusLozenge(Node n, Random r, Boolean UseMaskPath)
		{
			base_work(UseMaskPath);

			_blank = false;
			monitoredItem = n;

			int randTime = r.Next(200) - 100;

			timer = new StopControlledTimer();
			timer.Interval = 900 + randTime;
			timer.Tick += _timer_Tick;

			desc = monitoredItem.GetAttribute("desc");
			shortdesc = monitoredItem.GetAttribute("shortdesc");
			iconname = monitoredItem.GetAttribute("icon");
			GetIcon();
			padTop = 2;

			// Get the FixItQueue.
			fixItQueue = monitoredItem.Tree.GetNamedNode("FixItQueue");

			// Watch for changes on the monitored item.
			monitoredItem.AttributesChanged += monitoredItem_AttributesChanged;

			ArrayList al = monitoredItem.getChildren();
			if (al.Count>0)
			{
				monitoredChild = (Node)al[0];
				monitoredChild.ParentChanged += monitoredChild_ParentChanged;
			}
			else
			{
				monitoredChild = null;
			}

			// Need to connect to global workaround count.
			node_workaroundcount = monitoredItem.Tree.GetNamedNode("AppliedWorkArounds");
			node_workaroundcount.AttributesChanged += node_workaroundcount_AttributesChanged;
			workaround_count = node_workaroundcount.GetIntAttribute("num",0);

			// Need to watch the Advanced Warning Technology node to switch on the display of the down time.
			node_awt = monitoredItem.Tree.GetNamedNode("AdvancedWarningTechnology");

			// Check when we mouse over so that we can display fix and workaround buttons as required...
			MouseEnter += MouseEnterHandler;
			MouseLeave += MouseLeaveHandler;
			MouseDown  += MouseDownHandler;

			CalculateState(true);
		}

		public Node getMonitoredNode()
		{
			return monitoredItem;
		}

		/// <summary>
		/// Need to be able to detach from.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			if(disposing)
			{
				// Detach from all monitered nodes.
				if (monitoredItem != null)
				{
					monitoredItem.AttributesChanged -= monitoredItem_AttributesChanged;
				}
				if (monitoredChild != null)
				{
					monitoredChild.ParentChanged -= monitoredChild_ParentChanged;
				}
				if (node_workaroundcount != null)
				{
					node_workaroundcount.AttributesChanged -= node_workaroundcount_AttributesChanged;
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
		/// Set node to be monitored.
		/// </summary>
		public void setMonitoredNode(Node NewMonitoredItem)
		{
			node_awt = NewMonitoredItem.Tree.GetNamedNode("AdvancedWarningTechnology");

			// Detach the current one.
			if(monitoredItem != null)
			{
				monitoredItem.AttributesChanged -= monitoredItem_AttributesChanged;
			}

			if(node_workaroundcount != null)
			{
				node_workaroundcount.AttributesChanged -= node_workaroundcount_AttributesChanged;
				node_workaroundcount = null;
			}

			if (NewMonitoredItem != null)
			{
				// Connect to the new one.
				monitoredItem = NewMonitoredItem;
				_blank = false;
				desc = monitoredItem.GetAttribute("desc");
				shortdesc = monitoredItem.GetAttribute("shortdesc");
				iconname = monitoredItem.GetAttribute("icon");
				NewMonitoredItem.AttributesChanged += monitoredItem_AttributesChanged;

				// Get the FixItQueue.
				fixItQueue = monitoredItem.Tree.GetNamedNode("FixItQueue");

				//need to connect to golbal workaround count.
				node_workaroundcount = monitoredItem.Tree.GetNamedNode("AppliedWorkArounds");
				node_workaroundcount.AttributesChanged += node_workaroundcount_AttributesChanged;
				workaround_count = node_workaroundcount.GetIntAttribute("num",0);

				CalculateState(true);
				GetIcon();
				Invalidate();
			}
		}

		void _timer_Tick(object sender, EventArgs e)
		{
			if(flashCount <= -2)
			{
				timer.Stop();
			}
			else
			{
				--flashCount;
				SetState();
				Invalidate();
			}
		}

		public void SetBackgroundColor(Color c1)
		{
			BackColor = c1;
			brush = new SolidBrush(BackColor);
		}

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

		public virtual void RenderResizable (Graphics g)
		{
			g.SmoothingMode = SmoothingMode.AntiAlias;
			g.CompositingQuality = CompositingQuality.HighQuality;
			g.PixelOffsetMode = PixelOffsetMode.HighQuality;

			var iconBounds = new Rectangle (0, 0, Height, Height);
			var rectangleBounds = new Rectangle (iconBounds.Right, 0, Width - iconBounds.Right, Height);
			var colourToRectangle = new Dictionary<Color, Rectangle> ();
			var bsuBounds = new Rectangle (rectangleBounds.Left, rectangleBounds.Top, rectangleBounds.Width, rectangleBounds.Height / 2);
			var statusBounds = new Rectangle (rectangleBounds.Left, bsuBounds.Bottom, rectangleBounds.Width, rectangleBounds.Height / 2);

			switch (visualState)
			{
				case VisualState.UpAndGreen:
					colourToRectangle.Add(SkinningDefs.TheInstance.GetColorData("lozenge_colour_up"), rectangleBounds);
					break;

				case VisualState.UpByMirrorAmber:
					colourToRectangle.Add(SkinningDefs.TheInstance.GetColorData("lozenge_colour_up_by_mirror"), rectangleBounds);
					break;

				case VisualState.UpDueToWorkAroundBlue:
				case VisualState.DownAndRed:
					if (slabreach && showSLAState)
					{
						colourToRectangle.Add(SkinningDefs.TheInstance.GetColorData("lozenge_colour_down_incident_sla_breach_1"), new Rectangle (rectangleBounds.Left, rectangleBounds.Top, rectangleBounds.Width, rectangleBounds.Height / 2));
						colourToRectangle.Add(SkinningDefs.TheInstance.GetColorData("lozenge_colour_down_incident_sla_breach_2"), new Rectangle (rectangleBounds.Left, rectangleBounds.Top + (rectangleBounds.Height / 2), rectangleBounds.Width, rectangleBounds.Height - (rectangleBounds.Height / 2)));
					}
					else if ((flashCount <= 0) || ((flashCount % 2) == 1))
					{
						colourToRectangle.Add(SkinningDefs.TheInstance.GetColorData("lozenge_colour_down_incident_2"), rectangleBounds);
					}
					else
					{
						colourToRectangle.Add(SkinningDefs.TheInstance.GetColorData("lozenge_colour_down_incident_1"), rectangleBounds);
					}
					break;

				case VisualState.GoingDownPurple:
					colourToRectangle.Add(SkinningDefs.TheInstance.GetColorData("lozenge_colour_going_down"), rectangleBounds);
					break;

				case VisualState.RebootingCyan:
					colourToRectangle.Add(SkinningDefs.TheInstance.GetColorData("lozenge_colour_rebooting"), rectangleBounds);
					break;
			}

			if (visualState == VisualState.UpDueToWorkAroundBlue)
			{
				int workaroundDuration = SkinningDefs.TheInstance.GetIntData("workaround_time", 120);
				int workaroundTimer = monitoredItem.GetIntAttribute("workingAround", 0);
				int workaroundWidth = (int) Algorithms.Maths.MapBetweenRanges(workaroundTimer, 0, workaroundDuration, 0, rectangleBounds.Width);

				colourToRectangle.Add(SkinningDefs.TheInstance.GetColorData("lozenge_colour_up_by_workaround"), new Rectangle (rectangleBounds.Left, rectangleBounds.Top, workaroundWidth, rectangleBounds.Height));
			}

			if (icon != null)
			{
				g.DrawImage(icon, iconBounds);
			}

			foreach (var colour in colourToRectangle.Keys)
			{
				using (var brush = new SolidBrush (colour))
				{
					g.FillRectangle(brush, colourToRectangle[colour]);
				}
			}

			int borderThickness = SkinningDefs.TheInstance.GetIntData("lozenge_border_thickness");
			using (var pen = new Pen(SkinningDefs.TheInstance.GetColorData("lozenge_border_colour"), borderThickness))
			{
				g.DrawRectangle(pen, borderThickness / 2, borderThickness / 2, Width - borderThickness, Height - borderThickness);
			}

			using (SolidBrush legendTextBrush = new SolidBrush (LegendTextColor))
			{
				var formatting = new StringFormat
				{
					FormatFlags = StringFormatFlags.NoWrap,
					LineAlignment = StringAlignment.Center,
					Alignment = StringAlignment.Near
				};

				if ((visualState != VisualState.UpAndGreen) && ! string.IsNullOrEmpty(info_stores))
				{
					using (var font = ResizingUi.FontScalerExtensions.GetFontToFit(this, FontStyle.Bold, info_stores, new SizeF (bsuBounds.Width, bsuBounds.Height)))
					{
						g.DrawString(info_stores, font, legendTextBrush, bsuBounds, formatting);
					}
				}

				if (! string.IsNullOrEmpty(info_status))
				{
					using (var font = ResizingUi.FontScalerExtensions.GetFontToFit(this, FontStyle.Bold, info_status, new SizeF (statusBounds.Width, statusBounds.Height)))
					{
						g.DrawString(info_status, font, legendTextBrush, statusBounds, CalculateStatusTextFormatting());
					}
				}
			}
		}

		public virtual void Render(Graphics g)
		{
			if (allowResizing)
			{
				RenderResizable(g);
				return;
			}

			if (PaintBackground)
			{
				g.FillRectangle(brush, 0, 0, Width, Height);
			}

			g.SmoothingMode = SmoothingMode.AntiAlias;
			g.CompositingQuality = CompositingQuality.HighQuality;
			g.PixelOffsetMode = PixelOffsetMode.HighQuality;

			RectangleF textRect_status;
			RectangleF textRect_stores;
			RectangleF roundedRect;
			
		    if (_blank)
		    {
		        Image back = backGraphic;
		        roundedRect = new RectangleF(padLeft, 0, Size.Width - padLeft - padRight, Size.Height - padBottom);
		        g.DrawImage(back, roundedRect.X + 26 + 12, roundedRect.Y, 65, 40);
		        g.DrawImage(icon, 0, roundedRect.Y, 40, 40);
		    }
		    else
		    {
		        Image back = backGraphic;

		        textRect_stores = CalculateStoresTextLocation();
		        textRect_status = CalculateStatusTextLocation();

		        roundedRect = new RectangleF(padLeft, 0, Size.Width - padLeft - padRight, Size.Height - padBottom);

		        g.DrawImage(back, roundedRect.X + 26 + 12, roundedRect.Y, image_width, image_height);

                if (icon != null)
                {
                    g.DrawImage(icon, 0, roundedRect.Y, 40, 40);
                }

		        using (SolidBrush legendTextBrush = new SolidBrush(LegendTextColor))
                {
                    if (!string.IsNullOrEmpty(info_status))
				    {
					    StringFormat sf = CalculateStatusTextFormatting();
                        g.DrawString(info_status, infoFont, legendTextBrush, textRect_status, sf);
				    }

				    if ((int)visualState != (int)VisualState.UpAndGreen)
				    {
					    if (!string.IsNullOrEmpty(info_stores))
					    {
						    StringFormat sf = CalculateStoresTextFormatting();
                            g.DrawString(info_stores, infoFont, legendTextBrush, textRect_stores, sf);
					    }
				    }
			    }
            }
		}

		/// <summary>
		/// Converts the specified number of seconds to mins:secs format.
		/// </summary>
		protected string ConvertToTime(int ticks)
		{
			if(ticks == 0) return "";

			int mins = ticks / 60;
			int secs = ticks- (mins * 60);

			return CONVERT.ToStr(mins) + ":" + CONVERT.ToStr(secs).PadLeft(2,'0');
		}

		/// <summary>
		/// Now setting text color as well.
		/// </summary>
		public virtual void SetState()
		{
			string loc = AppInfo.TheInstance.Location + "\\";
			switch(visualState)
			{
				case VisualState.UpAndGreen:
					backGraphic = Repository.TheInstance.GetImage(loc + "\\images\\lozenges\\lozenge_green.png");
					LegendTextColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("lozenge_text_green", Color.White);
				break;

				case VisualState.AutoRestoreTickingDown:
					if(this.flashCount <= 0)
					{
						backGraphic = Repository.TheInstance.GetImage(loc + "\\images\\lozenges\\lozenge_saas_light.png");
						LegendTextColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("lozenge_text_saas_light", Color.Black);
					}
					else
					{
						if(flashCount%2 == 1)
						{
							backGraphic = Repository.TheInstance.GetImage(loc + "\\images\\lozenges\\lozenge_saas.png");
							LegendTextColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("lozenge_text_saas", Color.Black);
						}
						else
						{
							backGraphic = Repository.TheInstance.GetImage(loc + "\\images\\lozenges\\lozenge_saas_light.png");
							LegendTextColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("lozenge_text_saas_light", Color.Black);
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
							backGraphic = Repository.TheInstance.GetImage(loc + "\\images\\lozenges\\lozenge_hatch.png");
							LegendTextColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("lozenge_text_hatch", Color.Black);
						}
						else
						{
							backGraphic = Repository.TheInstance.GetImage(loc + "\\images\\lozenges\\lozenge_lightred.png");
							LegendTextColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("lozenge_text_lightred", Color.White);
						}
					}
					else
					{
						if(flashCount%2 == 1)
						{
							if(slabreach && showSLAState)
							{
								backGraphic = Repository.TheInstance.GetImage(loc + "\\images\\lozenges\\lozenge_hatch.png");
								LegendTextColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("lozenge_text_hatch", Color.Black);
							}
							else
							{
								backGraphic = Repository.TheInstance.GetImage(loc + "\\images\\lozenges\\lozenge_darkred.png");
								LegendTextColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("lozenge_text_darkred", Color.White);
							}
						}
						else
						{
							backGraphic = Repository.TheInstance.GetImage(loc + "\\images\\lozenges\\lozenge_lightred.png");
							LegendTextColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("lozenge_text_lightred", Color.White);
						}
					}
					break;

				case VisualState.GoingDownPurple:
					backGraphic = Repository.TheInstance.GetImage(loc + "\\images\\lozenges\\lozenge_purple.png");
					LegendTextColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("lozenge_text_purple", Color.White);
					break;

				case VisualState.RebootingCyan:
					backGraphic = Repository.TheInstance.GetImage(loc + "\\images\\lozenges\\lozenge_lilac.png");
					LegendTextColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("lozenge_text_lilac", Color.White);
					break;

				case VisualState.UpByMirrorAmber:
					backGraphic = Repository.TheInstance.GetImage(loc + "\\images\\lozenges\\lozenge_amber.png");
					LegendTextColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("lozenge_text_amber", Color.White);
					break;

				case VisualState.UpDueToWorkAroundBlue:
					backGraphic = Repository.TheInstance.GetImage(loc + "\\images\\lozenges\\lozenge_cyan.png");
					LegendTextColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("lozenge_text_cyan", Color.White);
					break;
			}
		}

	    string GetStoreLabel (string userIndex)
	    {
	        NodeTree model = monitoredItem.Tree;
	        string bizName = SkinningDefs.TheInstance.GetData("biz");

	        Node store = model.GetNamedNode(string.Format("{0} {1}", bizName, userIndex));

	        string storeIndex = store.GetAttribute("index", userIndex);

	        return store.GetAttribute("shortdesc", storeIndex);
	    }

	    string GetUsersDownLabel (string usersDown)
	    {
	        if (string.IsNullOrEmpty(usersDown))
	        {
	            return "";
	        }

	        string [] users = usersDown.Split(',');

            StringBuilder builder = new StringBuilder ();
	        foreach (string user in users)
	        {
	            if (builder.Length > 0)
	            {
	                builder.Append(SkinningDefs.TheInstance.GetData("lozenge_business_unit_separator", ","));
	            }

	            builder.Append(GetStoreLabel(user));
	        }

	        return builder.ToString();
	    }

	    public virtual void CalculateState(bool do_klaxon)
		{
			UpdateVisibility();

			bool awt_system_on = false;
		    if (node_awt != null)
		    {
		        awt_system_on = node_awt?.GetBooleanAttribute("enabled", false) ?? false;
		    }
		    LegendTextColor = Color.White;

			VisualState oldVisualState = visualState;

			apply_workaround_display_offset = false;

			// Handle the case where we are not connected.
			if (monitoredItem == null)
			{
				return; 
			}

			visualState = VisualState.UpAndGreen;
			info_status = "";
            info_stores = GetUsersDownLabel(monitoredItem.GetAttribute("users_down"));

			canWorkAround = monitoredItem.GetBooleanAttribute("canworkaround", false);

			if("true" == monitoredItem.GetAttribute("slabreach"))
			{
				slabreach = true;
				LegendTextColor = Color.Black;
			}
			else
			{
				slabreach = false;
				LegendTextColor = Color.White;
			}

            if (monitoredItem.GetAttribute("fixable") == "true")
			{
				fixable = true;
			}
			else
			{
				fixable = false;
			}

			// If we are down we are red...
			if(! monitoredItem.GetBooleanAttribute("up", false))
			{
				// If we are rebooting then we are lilac...
				// Check our rebooting status...
				string rebooting = monitoredItem.GetAttribute("rebootingForSecs");
                if ((rebooting == "") || (rebooting == "0"))
				{
					// We are not rebooting...
					// Therefore we should be red but if there is no impact on our service we lie and say
					// that the service is up.
					Boolean hasImpact = monitoredItem.GetBooleanAttribute("has_impact",false);
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
						if(downFor != -1)
						{
							info_status = ConvertToTime(downFor);
						}
						
                        // We only show the down for time, if awt is switched on.
						if (awt_system_on == false)
						{
							info_status = "";
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
						fixable = false;
						canWorkAround = false;
					}
				}
				else
				{
					// We are rebooting so show for how long...
					info_status = ConvertToTime( CONVERT.ParseInt( rebooting ) );
					visualState = VisualState.RebootingCyan;
				}
			}
			else
			{
				// : 9-4-2007 :  says that we don't show going down in the lozenge any more
				// as this is the AWT monitor tool's job.
				// : 18-04-2007 : has now decided that we do show going down but we show it as red.
				
				string gd = monitoredItem.GetAttribute("goingDownInSecs");

				if( (gd != "") && (gd != "0") )
				{
					// We must show purple.
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
					int wa = monitoredItem.GetIntAttribute("workingAround", 0);

					if(wa == 0)
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
								// We should show Amber.
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
						info_status = ConvertToTime( wa );
						// Add on the workaround count.
						info_status = info_status + "  " + CONVERT.ToStr(workaround_count);
						// Mark for x shift to allow display.
						apply_workaround_display_offset = true;
					}
				}
			}

			OnVisualStateChanged();

			SetState();
			
			
            if(do_klaxon)
			{
				// : fix for 4181 (don't play klaxon at start of round).
				Node timeNode = monitoredItem.Tree.GetNamedNode("CurrentTime");
				
                bool atStart = ((timeNode != null) && (timeNode.GetAttribute("seconds") == "0"));

				var klaxonTriggered = monitoredItem.GetBooleanAttribute("klaxon_triggered", false);

				if ((! atStart && !klaxonTriggered)
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
				    flashCount = SkinningDefs.TheInstance.GetIntData("lozenge_flash_count", 60);
					timer.Start();
					monitoredItem.SetAttribute("klaxon_triggered", true);
				}
			}
		}

		void CheckIfRebooting(bool awt_system_on)
		{
			// We may have one of our servers rebooting so we should show that state...
			string rebooting = monitoredItem.GetAttribute("rebootingForSecs");
            if ((rebooting == "") || (rebooting == "0"))
			{
				// Nope, no reboot.
				int downFor = monitoredItem.GetIntAttribute("mirrorforsecs", -1);
				if (-1 != downFor)
				{
					info_status = ConvertToTime(downFor);
				}
				// We only show the down for time, if awt is switched on.
				if (awt_system_on == false)
				{
					info_status = "";
				}
			}
			else
			{
				// Yes, something is rebooting...
				info_status = ConvertToTime(CONVERT.ParseInt(rebooting));
			}
		}

		void UpdateVisibility ()
		{
			Visible = (monitoredItem != null) && (monitoredItem.GetChildrenOfType("Connection").Count > 0);
		}

		protected virtual void Handle_monitoredItem_AttributesChanged(Node sender, ArrayList attrs)
		{
			UpdateVisibility();

			bool do_klaxon = false;

			foreach (AttributeValuePair avp in attrs)
			{
				// Handling changed description for upgrade applied mid race.
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
					// Now doing klaxon.
					do_klaxon = true;
				}
				else if ((avp.Attribute == "up") && ! CONVERT.ParseBool(avp.Value, true))
				{
					do_klaxon = true;
				}
			}
			CalculateState(do_klaxon);
			Invalidate();
		}


		protected void monitoredItem_AttributesChanged(Node sender, ArrayList attrs)
		{
			Handle_monitoredItem_AttributesChanged(sender, attrs);
		}

		public virtual void MouseEnterHandler(object sender, EventArgs e)
		{
			if(monitoredItem != null)
			{
				if(!ItilToolTip_Quad.TheInstance.ShowFullPanel)
				{
					ItilToolTip_Quad.TheInstance.ShowToolTip_Quad(this, fixItQueue, Strings.RemoveHiddenText(desc.Replace("\r\n", " ")),
						monitoredItem.GetAttribute("incident_id"),
					0, ToolTipOffsetY);

					ItilToolTip_Quad.TheInstance.BringToFront();
				}
			}
		}

		public virtual void MouseLeaveHandler(object sender, EventArgs e)
		{
			if(monitoredItem != null)
			{
				ItilToolTip_Quad.TheInstance.HideToolTip();
			}
		}

		public virtual void MouseDownHandler(object sender, MouseEventArgs e)
		{
			if(monitoredItem != null)
			{
				ItilToolTip_Quad.TheInstance.toggleShow();
			}
		}

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

		protected void monitoredChild_ParentChanged(Node sender, Node child)
		{
			setMonitoredNode(child.Parent);
		}

		protected virtual RectangleF CalculateStoresTextLocation ()
		{
			RectangleF textRect_stores;

			if (apply_workaround_display_offset)
			{
				textRect_stores = new RectangleF(padLeft + 23, padTop + 3, Size.Width - padLeft - padRight - 25, Size.Height - padTop - padBottom);
				textRect_stores.Offset(0, SkinningDefs.TheInstance.GetFloatData("lozenge_text_bu_y_offset", 1));
			}
			else
			{
				textRect_stores = new RectangleF(36, padTop + 3, Size.Width - 36 - padRight, Size.Height - padTop - padBottom);
				textRect_stores.Offset(0, SkinningDefs.TheInstance.GetFloatData("lozenge_text_bu_y_offset", 1));
			}

			return textRect_stores;
		}

		protected virtual RectangleF CalculateStatusTextLocation ()
		{
			RectangleF textRect_status;

			if (apply_workaround_display_offset)
			{
				textRect_status = new RectangleF(padLeft + 23, padTop + 3, Size.Width - padLeft - padRight - 25, Size.Height - padTop - padBottom);
				textRect_status.Offset(0, SkinningDefs.TheInstance.GetFloatData("lozenge_text_time_y_offset", 17));
			}
			else
			{
				textRect_status = new RectangleF(padLeft + 23 + 10, padTop + 3, Size.Width - padLeft - padRight - 36, Size.Height - padTop - padBottom);
				textRect_status.Offset(0, SkinningDefs.TheInstance.GetFloatData("lozenge_text_time_y_offset", 17));
			}

			return textRect_status;
		}

		protected virtual StringFormat CalculateStatusTextFormatting ()
		{
		    StringFormat sf = new StringFormat(StringFormatFlags.NoClip)
		                      {
		                          Trimming = StringTrimming.EllipsisCharacter,
		                          LineAlignment = StringAlignment.Near,
		                          Alignment = StringAlignment.Far
		                      };

		    return sf;
		}

		protected virtual StringFormat CalculateStoresTextFormatting ()
		{
		    StringFormat sf = new StringFormat(StringFormatFlags.NoClip)
		                      {
		                          Trimming = StringTrimming.EllipsisCharacter,
		                          LineAlignment = StringAlignment.Near,
		                          Alignment = StringAlignment.Far
		                      };

		    return sf;
		}

		bool allowResizing;

		public bool AllowResizing
		{
			get => allowResizing;

			set
			{
				allowResizing = value;
				Invalidate();
			}
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);
			Invalidate();
		}

		public VisualState VisualState => visualState;

		public event EventHandler VisualStateChanged;

		void OnVisualStateChanged ()
		{
			VisualStateChanged?.Invoke(this, EventArgs.Empty);
		}

		public bool IsSlaBreached => slabreach;
	}
}