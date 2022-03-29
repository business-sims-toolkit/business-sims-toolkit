using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Algorithms;
using CoreUtils;
using DiscreteSimGUI;
using LibCore;
using Network;
// ReSharper disable ObjectCreationAsStatement

namespace DevOps.OpsScreen
{
	public class DevOpsQuadStatusLozenge : QuadStatusLozenge
	{
	    protected Color textColour;

		public string LozengeName { get; set; }

		protected string iconName;

		protected Image infoPanelImage;
		protected string errorCode;

	    Color infoPanelColor;

        DevOpsLozengePopupMenu tooltip;
        DevOpsLozengePopupMenu menu;

        readonly Timer popupTimer;
	    readonly Timer lozengeFlashTimer;

	    int lozengeFlashCount;

		public DevOpsQuadStatusLozenge(Node referenceNode, Point position, Size size, string name)
			: base(referenceNode, new Random(1), false)
		{
		    errorCode = monitoredItem.GetAttribute("error_codes");

			Location = position;
			Size = size;

			LozengeName = name;

			string loc = AppInfo.TheInstance.Location + "\\";
			backGraphic = Repository.TheInstance.GetImage(loc + "images\\lozenges\\lozengeback.png");

            popupTimer = new Timer { Interval = 500 };
            popupTimer.Tick += popupTimer_Tick;

		    const int flashInterval = 500;
		    const int flashDuration = 5000;

		    lozengeFlashTimer = new Timer {Interval = flashInterval };
            lozengeFlashTimer.Tick += lozengeFlashTimer_Tick;
		    lozengeFlashTimer.Start();

		    lozengeFlashCount = flashDuration / flashInterval;
		}

        protected override void Dispose ()
        {
            base.Dispose();
            timer.Dispose();
            popupTimer.Dispose();
            lozengeFlashTimer.Dispose();
        }

        protected override void GetIcon ()
        {
            string loc = AppInfo.TheInstance.Location + "\\";

            icon = Repository.TheInstance.GetImage(loc + @"images\icons\" + iconname + "_default.png");

            if (icon == null)
            {
                throw new Exception("Icon doesn't exist.");
            }
        }

        protected override void OnMouseEnter (EventArgs e)
        {
            if (menu == null)
            {
                ShowTooltip();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            CloseTooltip();
        }

        void CloseTooltip ()
        {
            popupTimer.Stop();

            if (tooltip != null)
            {
                tooltip.Close();
                tooltip = null;
            }
        }

        void CloseMenu ()
        {
            if (menu != null)
            {
                menu.Close();
                menu = null;
            }
        }

        void ShowTooltip ()
        {
            popupTimer.Start();
        }

        void popupTimer_Tick (object sender, EventArgs args)
        {
            popupTimer.Stop();

            if (monitoredItem != null)
            {
                tooltip = new DevOpsLozengePopupMenu
                {
                    BackColor = Color.FromArgb(247, 148, 51)
                };
                tooltip.AddHeading(LozengeName, null);

                tooltip.FormClosed += tooltip_Closed;

                tooltip.Show(TopLevelControl, this, PointToScreen(new Point((Width - tooltip.Width) / 2, 0)));
            }
        }

        void tooltip_Closed (object sender, EventArgs args)
        {
            if (tooltip != null)
            {
                tooltip.Dispose();
                tooltip = null;
            }
        }

        void menu_Closed (object sender, EventArgs args)
        {
            if (menu != null)
            {
                menu.Dispose();
                menu = null;
            }
        }

        public override void MouseDownHandler (object sender, MouseEventArgs e)
        {
            CloseTooltip();

            menu = new DevOpsLozengePopupMenu
            {
                BackColor = Color.FromArgb(247, 148, 51)
            };
            menu.AddHeading(LozengeName, null);
            menu.AddDivider(8, false);
            menu.AddItem("Fix", @"lozenges\server_edit.png").Chosen += fix_Chosen;
            menu.AddItem("Fix By Consultancy", @"lozenges\server_lightning.png").Chosen += fixByConsultancy_Chosen;
            menu.AddItem("Workaround", @"lozenges\arrow_rotate_clockwise.png").Chosen += workaround_Chosen;
            menu.AddItem("First Line Fix", @"lozenges\1stline_fix.png").Chosen += firstLineFix_Chosen;
            menu.AddItem("Close Menu", @"lozenges\cancel.png").Chosen += close_Chosen;

            menu.FormClosed += menu_Closed;

            menu.Show(TopLevelControl, null, PointToScreen (new Point ((Width - menu.Width) / 2, 0)));
        }

        void Fix (string type)
        {
            new Node (monitoredItem.Tree.GetNamedNode("FixItQueue"), type, "", new AttributeValuePair ("incident_id", monitoredItem.GetAttribute("incident_id")));
            CloseMenu();
        }

        void fix_Chosen (object sender, EventArgs args)
        {
            Fix("fix");
        }

        void fixByConsultancy_Chosen (object sender, EventArgs args)
        {
            Fix("fix by consultancy");
        }

        void workaround_Chosen (object sender, EventArgs args)
        {
            Fix("workaround");
        }

        void firstLineFix_Chosen (object sender, EventArgs args)
        {
            Fix("first_line_fix");
        }

        void close_Chosen (object sender, EventArgs args)
        {
            CloseMenu();
        }


	    void lozengeFlashTimer_Tick(object sender, EventArgs e)
	    {
	        if (--lozengeFlashCount <= 0)
	        {
                lozengeFlashTimer.Stop();
	        }

            Invalidate();
	    }

        public bool Status => visualState == VisualState.UpAndGreen;

	    public string StatusString
		{
			get
			{
				if (visualState == VisualState.UpDueToWorkAroundBlue)
				{
					return "workaround";
				}
				else
				{
					if (slabreach)
					{
						return "slabreach";
					}
					else
					{
						return "down";
					}
				}
			}
		}

		public Node MonitoredItem => monitoredItem;

	    public string IncidentId => monitoredItem.GetAttribute("incident_id");

	    public event EventHandler OnLozengeFixed;

		void OnStatusUp()
		{
		    OnLozengeFixed?.Invoke(this, EventArgs.Empty);
		}

		public override void Render(Graphics g)
		{
			g.SmoothingMode = SmoothingMode.AntiAlias;
			g.InterpolationMode = InterpolationMode.HighQualityBicubic;
			g.CompositingQuality = CompositingQuality.HighQuality;
			g.PixelOffsetMode = PixelOffsetMode.HighQuality;

		    var borderPercentage = SkinningDefs.TheInstance.GetFloatData("lozenge_border_width_percent", 0.06f);
		    var borderWidth = (int)(Height * borderPercentage);

		    var iconCentreOffset = Height / 2;
            
            var infoPanelRectangle = new Rectangle(iconCentreOffset, borderWidth, Width - iconCentreOffset - borderWidth, Height - borderWidth * 2);


            var lozengeBackgroundColour = SkinningDefs.TheInstance.GetColorData("lozenge_background_colour", Color.FromArgb(37, 56, 88));

            using (var lozengeBrush = new SolidBrush(lozengeBackgroundColour))
            using (var borderPen = new Pen(lozengeBackgroundColour, borderWidth))
            using (var infoPanelBrush = new SolidBrush(infoPanelColor))
            {
                // Background rectangle
                g.FillRectangle(lozengeBrush, new Rectangle(iconCentreOffset, 0, Width - iconCentreOffset, Height));


                if (lozengeFlashCount > 0 && lozengeFlashCount % 2 == 0)
                {
                    using (var flashBrush = new SolidBrush(Maths.Lerp(0.25f, infoPanelColor, Color.Black)))
                    {
                        g.FillRectangle(flashBrush, infoPanelRectangle);
                    }
                }
                else
                {
                    // Rectangle showing incident status
                    g.FillRectangle(infoPanelBrush, infoPanelRectangle);
                }

                

                if (visualState == VisualState.UpDueToWorkAroundBlue)
                {
                    var timeDown = monitoredItem.GetIntAttribute("workingAround", 0);
                    var workAroundTime = monitoredItem.GetIntAttribute("workaround_time", 120);

                    var progressPercent = timeDown / (float)workAroundTime;

                    using (var workAroundBrush = new SolidBrush(SkinningDefs.TheInstance.GetColorData("lozenge_incident_workaround_colour", Color.DeepSkyBlue)))
                    {
                        g.FillRectangle(workAroundBrush, new Rectangle(infoPanelRectangle.X, infoPanelRectangle.Y, (int)(infoPanelRectangle.Width * progressPercent), infoPanelRectangle.Height));
                    }
                }

                const int correctionOffset = 1;
                // White portion of circle
                g.FillEllipse(Brushes.White, new Rectangle(correctionOffset, correctionOffset, Height - 2 * correctionOffset, Height - 2 * correctionOffset));
                // Border for circle
                g.DrawEllipse(borderPen, new Rectangle(borderWidth / 2, borderWidth / 2, Height - borderWidth, Height - borderWidth));

            }


            var iconRectangle = new Rectangle(new Point(Height / 12, Height / 12), new Size(Height * 5 / 6, Height * 5 / 6));

			GetIcon();
			if (icon != null)
			{
				g.DrawImage(icon, iconRectangle);
			}

			// Users down string
			var textPosition = new Point(Height, Height / 9);
			DrawOutlinedText(g, info_stores, textPosition, ResizingUi.FontScalerExtensions.GetFontSizeInPixelsToFit(this, infoFont.Name, FontStyle.Bold, info_stores, new SizeF (Width, Height / 4.0f)));
		}

		void DrawOutlinedText(Graphics g, string text, Point position, float fontSize = 10.0f)
		{
			GraphicsPath gPath = new GraphicsPath();

			gPath.AddString(text, infoFont.FontFamily, (int) FontStyle.Bold,
				g.DpiY * fontSize / 72, position, new StringFormat());

			g.DrawPath(Pens.White, gPath);
			using (Brush brush = new SolidBrush(Color.White))
			{
				g.FillPath(brush, gPath);
			}
		}

		public override void CalculateState(bool doKlaxon)
		{
			base.CalculateState(doKlaxon);

			if (visualState == VisualState.UpAndGreen)
			{
			    if (! monitoredItem.GetBooleanAttribute("canWorkaround", true))
			    {
			        OnStatusUp();
                }
			}
		}

		public override void SetState()
		{
			BackColor = Color.Transparent;

			string loc = AppInfo.TheInstance.Location + "\\images\\lozenges\\";

			switch(visualState)
			{
				case VisualState.ComplianceIncident:
				case VisualState.SecurityFlaw:
				case VisualState.DenialOfService:
				case VisualState.DownAndRed:
					if (slabreach && showSLAState)
					{
						infoPanelImage = Repository.TheInstance.GetImage(loc + "lozenge_hatch.png");
					    infoPanelColor = SkinningDefs.TheInstance.GetColorData("lozenge_incident_slabreach_colour", Color.Pink);
                    }
					else
					{
						infoPanelImage = Repository.TheInstance.GetImage(loc + "lozenge_lightred.png");
					    infoPanelColor = SkinningDefs.TheInstance.GetColorData("lozenge_incident_down_colour", Color.Red);
                    }
					break;
				case VisualState.UpDueToWorkAroundBlue:
					infoPanelImage = Repository.TheInstance.GetImage(loc + "lozenge_blue.png");
					break;
			}
		}
	}
}
