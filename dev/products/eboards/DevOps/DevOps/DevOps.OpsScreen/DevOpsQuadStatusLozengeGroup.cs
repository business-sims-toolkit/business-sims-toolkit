using System;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

using CommonGUI;
using LibCore;
using Network;
using CoreUtils;
using DevOps.OpsEngine;

namespace DevOps.OpsScreen
{
    public class DevOpsQuadStatusLozengeGroup : FlickerFreePanel, IIncidentSlotTracker
    {
        readonly NodeTree networkFile;
        readonly Node businessServiceGroup;

        Dictionary<int, Rectangle> indexToBounds;
        readonly Dictionary<string, int> incidentNameToPositionIndex;
        readonly Dictionary<int, DevOpsQuadStatusLozenge> positionIndexToLozenge;

        readonly int maxBrokenIncidents = SkinningDefs.TheInstance.GetIntData("max_broken_incidents");

        public int RemainingIncidentSlots => (maxBrokenIncidents - positionIndexToLozenge.Count);

	    public List<DevOpsQuadStatusLozenge> ActiveIncidents => new List<DevOpsQuadStatusLozenge> (positionIndexToLozenge.Values);

	    public List<Rectangle> FreeIncidentLocations => indexToBounds.Keys.Where(i => ! positionIndexToLozenge.ContainsKey(i)).Select(i => indexToBounds[i]).ToList();

	    IWatermarker watermarker;

	    public IWatermarker Watermarker
	    {
		    get => watermarker;

		    set
		    {
			    watermarker = value;
			    Invalidate();
		    }
	    }

	    public DevOpsQuadStatusLozengeGroup (NodeTree nodeTree)
        {
            networkFile = nodeTree;

            businessServiceGroup = networkFile.GetNamedNode("Business Services Group");
            businessServiceGroup.ChildAdded += businessServiceGroup_ChildAdded;
            businessServiceGroup.ChildRemoved += businessServiceGroup_ChildRemoved;

            foreach (var child in businessServiceGroup.GetChildrenAsList())
            {
                child.AttributesChanged += businessService_AttributesChanged;
            }

            positionIndexToLozenge = new Dictionary<int, DevOpsQuadStatusLozenge> ();
			incidentNameToPositionIndex = new Dictionary<string, int> ();

			DoSize();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);

            DoSize();
        }

        void DoSize()
        {
		    const int numColumns = 4;
		    const int numRows = 2;

		    indexToBounds = new Dictionary<int, Rectangle>();

		    const int defaultLozengeWidth = 500;
		    const int defaultLozengeHeight = 200;

		    var targetLozengeWidth = (Width * 9 / 10) / numColumns;
		    var targetLozengeHeight = (Height * 9 / 10) / numRows;
		    var lozengeScale = Math.Min(targetLozengeWidth * 1.0f / defaultLozengeWidth, targetLozengeHeight * 1.0f / defaultLozengeHeight);
		    var lozengeSize = new Size((int) (defaultLozengeWidth * lozengeScale), (int) (defaultLozengeHeight * lozengeScale));

		    var widthPadding = (Width - (numColumns * lozengeSize.Width)) / (numColumns + 1);
		    var heightPadding = (Height - (numRows * lozengeSize.Height)) / (numRows + 1);

		    var index = 0;
		    for (var row = 0; row < numRows; row++)
		    {
			    for (var col = 0; col < numColumns; col++)
			    {
				    indexToBounds.Add(index, new Rectangle(widthPadding + ((lozengeSize.Width + widthPadding) * col), heightPadding + ((lozengeSize.Height + heightPadding) * row), lozengeSize.Width, lozengeSize.Height));

				    if (positionIndexToLozenge.ContainsKey(index))
				    {
					    positionIndexToLozenge[index].Bounds = indexToBounds[index];
				    }

				    index++;
			    }
		    }

            Invalidate();
        }

		public event EventHandler ActiveIncidentsChanged;

        void OnActiveIncidentsChanged()
        {
            Invalidate();
            ActiveIncidentsChanged?.Invoke(this, EventArgs.Empty);
        }

        void DisplayIncident (Node incidentNode)
        {
            if (positionIndexToLozenge.Count >= maxBrokenIncidents)
            {
                DisplayError(incidentNode);
                return;
            }

	        var name = incidentNode.GetAttribute("name");
            if (incidentNameToPositionIndex.ContainsKey(name))
            {
                return;
            }

	        // Create a new incident lozenge.
	        // Get the left-most location.
	        var index = indexToBounds.Keys.First(i => ! positionIndexToLozenge.ContainsKey(i));
	        var bounds = indexToBounds[index];

            var lozenge = new DevOpsQuadStatusLozenge (incidentNode, bounds.Location, bounds.Size, name);
            lozenge.OnLozengeFixed += RemoveLozenge;

            positionIndexToLozenge.Add(index, lozenge);
			incidentNameToPositionIndex.Add(name, index);
            Controls.Add(lozenge);
            lozenge.BringToFront();

            OnActiveIncidentsChanged();
        }

        void RemoveIncident(Node incidentNode)
        {
            var incidentName = incidentNode.GetAttribute("name");

            // Check that this incident is currently being displayed.
            if (! incidentNameToPositionIndex.ContainsKey(incidentName))
            {
                throw new Exception("Incident to be removed is not currently being displayed.");
            }

	        var index = incidentNameToPositionIndex[incidentName];
			var incidentLozenge = positionIndexToLozenge[index];

            incidentLozenge.Dispose();
            positionIndexToLozenge.Remove(index);
	        incidentNameToPositionIndex.Remove(incidentName);

            OnActiveIncidentsChanged();
        }

        void DisplayError(Node incidentNode)
        {
            var incidentName = incidentNode.GetAttribute("name");
            var incidentId = incidentNode.GetAttribute("incident_id");

            if (!string.IsNullOrEmpty(incidentId))
            {
                incidentId = "BLANK";
            }

            var errorStr = CONVERT.Format("Unable to display incident: {0} (ID: {1}) as the maximum number of incidents are already being displayed. ",
                incidentName, incidentId);

            var errorsNode = networkFile.GetNamedNode("FacilitatorNotifiedErrors");
            var error = new Node(errorsNode, "error", "", new AttributeValuePair("text", errorStr));
        }

        void RemoveLozenge(object sender, EventArgs eventArgs)
        {
            var lozenge = (DevOpsQuadStatusLozenge)sender;
            var name = lozenge.LozengeName;

            if (!incidentNameToPositionIndex.ContainsKey(name))
            {
                throw new Exception("Lozenge is requesting to be removed but it's not being tracked.");
            }

            var incidentNode = lozenge.MonitoredItem;

            RemoveIncident(incidentNode);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DisposeMonitoring();
                if (businessServiceGroup != null)
                {
                    businessServiceGroup.ChildAdded -= businessServiceGroup_ChildAdded;
                    businessServiceGroup.ChildRemoved -= businessServiceGroup_ChildRemoved;
                }
            }

            base.Dispose(disposing);
        }

        void DisposeMonitoring()
        {
            foreach (var lozenge in positionIndexToLozenge.Values)
            {
                var node = lozenge.MonitoredItem;
                node.AttributesChanged -= businessService_AttributesChanged;

                lozenge.Dispose();
            }

            positionIndexToLozenge.Clear();
			incidentNameToPositionIndex.Clear();
        }

        void businessServiceGroup_ChildAdded(Node sender, Node child)
        {
            child.AttributesChanged += businessService_AttributesChanged;
        }

        void businessServiceGroup_ChildRemoved (Node sender, Node child)
        {
            var name = child.GetAttribute("name");

            Debug.Assert(!incidentNameToPositionIndex.ContainsKey(name));

            child.AttributesChanged -= businessService_AttributesChanged;
        }

        void businessService_AttributesChanged (Node sender, ArrayList attrs)
        {
            var name = sender.GetAttribute("name");

            if (!incidentNameToPositionIndex.ContainsKey(name)) // Incident is not already being tracked.
            {
                var status = CONVERT.ParseBool(sender.GetAttribute("up")).Value;
                // Status is down, could be breached and/or working around.
                if (!status)
                {
                    // Try to display it, if there's still space for it.
                    DisplayIncident(sender);
                }
            }
        }

        int IIncidentSlotTracker.GetRemainingSlots()
        {
            return RemainingIncidentSlots;
        }

        protected override void OnPaint (PaintEventArgs e)
        {
	        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

			e.Graphics.FillRectangle(Brushes.White, 0, 0, Width, Height);

			watermarker?.Draw(this, e.Graphics);

            foreach (var index in indexToBounds.Keys)
            {
	            var lozengeRect = indexToBounds[index];
	            using (var path = new GraphicsPath ())
	            {
		            var shadowAngle = (float) (60 * Math.PI / 180);
		            var shadowLength = Height;

					var circleCentre = new Point (lozengeRect.X + (lozengeRect.Height / 2), lozengeRect.Y + (lozengeRect.Height / 2));
		            var basePosition = new Point ((int) (circleCentre.X + ((lozengeRect.Height / 2) * Math.Cos(shadowAngle + (Math.PI / 2)))), (int) (circleCentre.Y + ((lozengeRect.Height / 2) * Math.Sin(shadowAngle + (Math.PI / 2)))));

					var points = new []
		            {
			            basePosition,
			            new Point (lozengeRect.Right, lozengeRect.Y),
			            new Point ((int) (lozengeRect.Right + (shadowLength * Math.Cos(shadowAngle))), (int) (lozengeRect.Y + (shadowLength * Math.Sin(shadowAngle)))),
			            new Point ((int) (basePosition.X + (shadowLength * Math.Cos(shadowAngle))), (int) (basePosition.Y + (shadowLength * Math.Sin(shadowAngle))))
		            };

		            path.AddLines(points);

	                using (var brush = new LinearGradientBrush(points[0], points[2],
	                    SkinningDefs.TheInstance.GetColorData("lozenge_shadow_starting_colour", Color.FromArgb(80, 0, 0, 0)),
	                    SkinningDefs.TheInstance.GetColorData("lozenge_shadow_ending_colour", Color.FromArgb(0, 0, 0, 0))))
                    {
						e.Graphics.FillPath(brush, path);
					}

	                if (positionIndexToLozenge.ContainsKey(index))
	                {
	                    continue;
	                }

	                var borderWidth = (int) (lozengeRect.Height * SkinningDefs.TheInstance.GetFloatData("lozenge_border_width_percent", 0.06f));

	                var lozengeBackgroundColour = SkinningDefs.TheInstance.GetColorData("lozenge_background_colour", Color.FromArgb(37, 56, 88));

	                using (var lozengeBrush = new SolidBrush(lozengeBackgroundColour))
	                using (var borderPen = new Pen(lozengeBackgroundColour, borderWidth))
	                {
	                    var circleRadius = lozengeRect.Height / 2;
	                    var rectangleWidth = lozengeRect.Width - circleRadius;

	                    e.Graphics.FillRectangle(lozengeBrush, new Rectangle(lozengeRect.X + circleRadius, lozengeRect.Y, rectangleWidth, lozengeRect.Height));
	                    e.Graphics.FillEllipse(Brushes.White, new Rectangle(lozengeRect.X + 1, lozengeRect.Y + 1, lozengeRect.Height - 2, lozengeRect.Height - 2));
	                    e.Graphics.DrawEllipse(borderPen, new Rectangle(lozengeRect.X + borderWidth / 2, lozengeRect.Y + borderWidth / 2, lozengeRect.Height - borderWidth, lozengeRect.Height - borderWidth));
	                }
	            }
			}
        }
    }
}