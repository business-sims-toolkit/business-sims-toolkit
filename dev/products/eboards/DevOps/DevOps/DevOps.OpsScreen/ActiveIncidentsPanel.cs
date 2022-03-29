using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using CommonGUI;
using CoreUtils;
using LibCore;
using ResizingUi.Button;
using ContentAlignment = System.Drawing.ContentAlignment;

namespace DevOps.OpsScreen
{
    internal class ActiveIncidentsPanel : FlickerFreePanel
    {
        readonly int heightPadding = 4;
        readonly int widthPadding = 10;

        Label title;

        StyledDynamicButton closeButton;

        readonly Color textColour = SkinningDefs.TheInstance.GetColorDataGivenDefault("request_panel_text_colour", Color.Black);
        readonly string fontName = SkinningDefs.TheInstance.GetData("fontname");
        
        Font titlesFont;
        Font valuesFont;

        readonly DevOpsQuadStatusLozengeGroup incidentGroup;

        readonly IDataEntryControlHolderWithShowPanel parentControl;

        public ActiveIncidentsPanel (DevOpsQuadStatusLozengeGroup incidentGroup, IDataEntryControlHolderWithShowPanel parent)
        {
            parentControl = parent;
            
            this.incidentGroup = incidentGroup;
            this.incidentGroup.ActiveIncidentsChanged += incidentGroup_ActiveIncidentsChanged;

            BasicLayout();
        }

        void incidentGroup_ActiveIncidentsChanged(object sender, EventArgs e)
        {
            Invalidate();
        }

        void BasicLayout()
        {
            titlesFont = ConstantSizeFont.NewFont(fontName, 10, FontStyle.Bold);
            valuesFont = ConstantSizeFont.NewFont(fontName, 10, FontStyle.Bold);

            title = new Label
            {
                Location = new Point(widthPadding, heightPadding),
                Text = "Active Incidents",
                ForeColor = textColour,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = titlesFont
            };
            Controls.Add(title);

            closeButton = new StyledDynamicButton("standard", "Close")
            {
                Size = new Size(100, 30),
                Font = SkinningDefs.TheInstance.GetFontWithStyle("standard_popup_control_button_font")
            };
            closeButton.Click += closeButton_Click;
            Controls.Add(closeButton);

            Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                incidentGroup.ActiveIncidentsChanged -= incidentGroup_ActiveIncidentsChanged;
            }
        }

        void closeButton_Click(object sender, EventArgs e)
        {
            parentControl.DisposeEntryPanel();
        }
        
        protected override void OnPaint(PaintEventArgs e)
        {
            List<DevOpsQuadStatusLozenge> activeIncidents = incidentGroup.ActiveIncidents;

	        var lozengeAspect = 475.0f / 200;
	        var lozengeSize = new Size(Width / 6, (int) (Width / 6f / lozengeAspect));

			foreach (DevOpsQuadStatusLozenge incident in activeIncidents)
            {
                Point lozLoc = incident.Location;

                Point scaledLoc = ScalePoint(incidentGroup.Width, incidentGroup.Height, Width, Height, lozLoc);
                scaledLoc.Y += title.Bottom;

                Rectangle lozengeRect = new Rectangle(scaledLoc, lozengeSize);
	            RenderLozenge(e.Graphics, lozengeRect, true);

				StringFormat stringFormat = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                using (SolidBrush myBrush = new SolidBrush(Color.Black))
                {
	                var textPosition = new Point(scaledLoc.X + (lozengeRect.Width / 5), scaledLoc.Y + (lozengeRect.Height / 2));
					e.Graphics.DrawString(incident.IncidentId, valuesFont, myBrush, textPosition.X, textPosition.Y, stringFormat);
                }
            }

            foreach (var freeLoc in incidentGroup.FreeIncidentLocations)
            {
                Point scaledLoc = ScalePoint(incidentGroup.Width, incidentGroup.Height, Width, Height, freeLoc.Location);
                scaledLoc.Y += title.Bottom;

                Rectangle lozengeRect = new Rectangle(scaledLoc, lozengeSize);
	            RenderLozenge(e.Graphics, lozengeRect, false);
            }
		}

	    void RenderLozenge (Graphics graphics, Rectangle lozengeBounds, bool active)
	    {
		    var background = SkinningDefs.TheInstance.GetColorData("incident_popup_background", Color.Blue);
		    var activeBackground = SkinningDefs.TheInstance.GetColorData("incident_popup_active_background", Color.White);
		    var inactiveBackground = SkinningDefs.TheInstance.GetColorData("incident_popup_inactive_background", Color.Black);
		    var borderThickness = (int) (SkinningDefs.TheInstance.GetFloatData("incident_popup_border_thickness_fraction", 0.05f) * lozengeBounds.Height);
		    var circleBounds = new Rectangle(lozengeBounds.Left, lozengeBounds.Top, lozengeBounds.Height, lozengeBounds.Height);
		    var bannerBounds = new Rectangle(circleBounds.Left + (lozengeBounds.Height / 2), lozengeBounds.Top, lozengeBounds.Width - (lozengeBounds.Height / 2), lozengeBounds.Height);

		    using (var brush = new SolidBrush(background))
		    {
			    graphics.FillRectangle(brush, bannerBounds);
			    graphics.FillEllipse(brush, circleBounds);
		    }

		    using (var brush = new SolidBrush(active ? activeBackground : inactiveBackground))
		    {
			    graphics.FillEllipse(brush, new Rectangle(circleBounds.Left + borderThickness, circleBounds.Top + borderThickness, circleBounds.Width - (2 * borderThickness), circleBounds.Height - (2 * borderThickness)));
		    }
	    }

		Point ScalePoint(int sourceWidth, int sourceHeight, int destinationWidth, int destinationHeight, Point origPoint)
        {
            float horizontalScale = destinationWidth / (float) sourceWidth;
            float verticalScale = destinationHeight / (float) sourceHeight;

            return new Point((int)(origPoint.X * horizontalScale),(int)( origPoint.Y * verticalScale));
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);

            DoSize();
        }

        void DoSize()
        {
            title.Size = new Size(Width - (2 * widthPadding), 25);

            closeButton.Location = new Point(Width - widthPadding - closeButton.Width, Height - closeButton.Height - widthPadding);

            Invalidate();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);

            if (Visible)
            {
                Invalidate();
            }
        }
        
    }
}
