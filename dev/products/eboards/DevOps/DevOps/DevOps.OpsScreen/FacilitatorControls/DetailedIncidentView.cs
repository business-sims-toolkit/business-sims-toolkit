using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

using Algorithms;
using CoreUtils;
using IncidentManagement;
using LibCore;
using Network;
using ResizingUi;
// ReSharper disable ObjectCreationAsStatement

namespace DevOps.OpsScreen.FacilitatorControls
{
	internal class DetailedIncidentView : CascadedBackgroundPanel
	{
	    readonly IncidentDefinition breakIncident;
	    readonly IncidentDefinition warningIncident;

	    readonly NodeTree model;
	    readonly Node fixItQueue;
	    readonly Node incidentQueue;

		DevOpsLozengePopupMenu menu;
        
	    readonly Dictionary<IncidentColumns, RectangleF> columnToBounds;

		static Dictionary<string, HatchFillImage> nameToHatchedImage = new Dictionary<string, HatchFillImage>();

		public enum MatchStates
		{
			Exact,
			Partial,
			None
		}

		public MatchStates MatchState => isMatchedExactly ? MatchStates.Exact :
			isPartiallySelected ? MatchStates.Partial : MatchStates.None;

		struct Action
		{
			public string IncidentId { get; set; }
            public string ActionType { get; set; }
			public bool Fix { get; set; }

            public bool Close { get; set; }
		}

        public Color RowColour { get; set; }

	    List<Node> businessServicesImpacted;

	    readonly Node businessServicesGroup;

	    const int minimumServiceRowHeight = 20;

	    public int PreferredHeight => businessServicesImpacted.Count * minimumServiceRowHeight;

		public string BreakIncidentId => breakIncident?.ID ?? "";
		public string WarningIncidentId => warningIncident?.ID ?? "";


		bool notSelected;
		public bool ActivelyNotSelected
		{
			get => notSelected;
			set
			{
				notSelected = value;
				if (notSelected)
				{
					isPartiallySelected = isMatchedExactly = false;
				}
				Invalidate();
			}
		}

		bool isPartiallySelected;
		public bool IsPartiallySelected
		{
			get => isPartiallySelected;
			set
			{
				isPartiallySelected = value;
				Invalidate();
			}
		}

		bool isMatchedExactly;
		public bool IsMatchedExactly
		{
			get => isMatchedExactly;
			set
			{
				isMatchedExactly = value;
				if (isMatchedExactly)
				{
					isPartiallySelected = false;
				}
				Invalidate();
			}
		}

		public DetailedIncidentView (NodeTree model, IncidentDefinition breakIncident, IncidentDefinition warningIncident)
		{
			this.model = model;
			incidentQueue = model.GetNamedNode("enteredIncidents");
			fixItQueue = model.GetNamedNode("FixItQueue");

			this.breakIncident = breakIncident;
			this.warningIncident = warningIncident;

		    if (breakIncident?.ID == "86")
		    {

		    }

            businessServicesImpacted = (breakIncident ?? warningIncident).GetBusinessServicesAffected(model);
            
		    Visible = businessServicesImpacted.Any();

		    businessServicesGroup = model.GetNamedNode("Business Services Group");
            businessServicesGroup.ChildAdded += businessServicesGroup_ChildAdded;

            foreach (var businessService in businessServicesImpacted)
		    {
		        businessService.AttributesChanged += businessService_AttributesChanged;
		    }
		    

		    columnToBounds = new Dictionary<IncidentColumns, RectangleF>();
		}
		
		public void ProcessIncidentId (string id)
		{
			if (id != BreakIncidentId && id != WarningIncidentId)
			{
				return;
			}

			var fix = id == BreakIncidentId ? model.GetNodesWithAttributeValue("incident_id", id).Count > 0 : 
				warningIncident.GetBusinessServicesAffected(model).Any(b => b.GetIntAttribute("danger_level", 0) == 100);

			ProcessAction(new Action
			{
				ActionType = fix ? "entrypanel_fix" : "IncidentNumber",
				IncidentId = id,
				Fix = fix
			});
			
		}

        void businessServicesGroup_ChildAdded(Node sender, Node child)
        {
            foreach (var businessService in businessServicesImpacted)
            {
                businessService.AttributesChanged -= businessService_AttributesChanged;
            }

            businessServicesImpacted = (breakIncident ?? warningIncident).GetBusinessServicesAffected(model);

            foreach (var businessService in businessServicesImpacted)
            {
                businessService.AttributesChanged += businessService_AttributesChanged;
            }

            Visible = businessServicesImpacted.Any();
        }

        void businessService_AttributesChanged (object sender, ArrayList attrs)
	    {
            Invalidate();
	    }

	    protected override void Dispose (bool disposing)
	    {
	        if (disposing)
	        {
	            businessServicesGroup.ChildAdded -= businessServicesGroup_ChildAdded;

                foreach (var businessService in businessServicesImpacted)
	            {
	                businessService.AttributesChanged -= businessService_AttributesChanged;
	            }
            }

            base.Dispose(disposing);
	    }

		

        protected override void OnSizeChanged(EventArgs e)
        {
            DoSize();
            Invalidate();
        }

	    void DoSize ()
	    {
	        columnToBounds.Clear();

	        var x = 0f;
	        var baseColumnWidth = Width / (float)IncidentColumnInfo.ColumnToWidthFactor.Values.Sum();
	        
            foreach (var column in IncidentColumnInfo.ColumnOrder)
	        {
	            var columnWidth = IncidentColumnInfo.ColumnToWidthFactor[column] * baseColumnWidth;
	            var columnBounds = new RectangleF(x, 0, columnWidth, Height);
	            x += columnWidth;
	            columnToBounds[column] = columnBounds;
	        }
            

	    }

        protected override void OnPaint (PaintEventArgs e)
		{
			base.OnPaint(e);

		    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
		    e.Graphics.CompositingQuality = CompositingQuality.HighQuality;
		    e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
		    e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

			var totalBounds = new Rectangle(0, 0, Width, Height);

			var backgroundBounds = totalBounds;

			if (isPartiallySelected || isMatchedExactly)
			{
				const int borderWidth = 2;
				backgroundBounds.Inflate(-borderWidth, -borderWidth);

				var borderColour = isMatchedExactly ? Color.Aqua : Color.FromArgb(160, Color.HotPink);

				using (var highlightBrush = new SolidBrush(borderColour))
				{
					e.Graphics.FillRectangle(highlightBrush, totalBounds);
				}
			}

			var rowBackColour = !Enabled || notSelected ? RowColour.Shade(0.3f) : RowColour;
			
			using (var backBrush = new SolidBrush(rowBackColour))
			{
				e.Graphics.FillRectangle(backBrush, backgroundBounds);
			}

		    var businessServicesShortNames = businessServicesImpacted.Select(s => s.GetAttribute("shortdesc")).ToList();

            var businessServicesShortNamesStr =
		        string.Join(",", businessServicesImpacted.Select(s => s.GetAttribute("shortdesc")));
		    var icons = businessServicesImpacted.Select(s =>
		        Repository.TheInstance.GetImage(AppInfo.TheInstance.Location +
		                                        $@"\images\icons\{s.GetAttribute("icon")}_default.png") as Image).ToList();

            var columnContent = new Dictionary<IncidentColumns, string>
            {
                { IncidentColumns.Incident, breakIncident?.ID ?? ""},
                { IncidentColumns.Event, warningIncident?.ID },
                { IncidentColumns.Service, businessServicesShortNamesStr},
                { IncidentColumns.Impact, breakIncident != null ? $"{string.Join(",", breakIncident.GetBusinessServiceUsersAffected(model).Select(bsu => bsu.Parent.GetAttribute("shortdesc")).Distinct())} - {string.Join(",", breakIncident.GetChannelsAffected(model).Select(s => char.ToUpper(s[0])))}" : ""},
                { IncidentColumns.Failure, breakIncident != null ? breakIncident.Attributes.ContainsKey("ci_name") ? breakIncident.Attributes["ci_name"] : "" : ""},
                { IncidentColumns.Question, breakIncident != null ? breakIncident.Attributes["question"] : ""},
                { IncidentColumns.Answer, breakIncident != null ? breakIncident.Attributes["answer"] : ""}
            };
            
            using (var font = SkinningDefs.TheInstance.GetFont(10))
            using (var servicesFont = SkinningDefs.TheInstance.GetFont(7))
            {
                foreach (var column in IncidentColumnInfo.ColumnOrder)
                {
                    var bounds = columnToBounds[column];

                    
                    var stringAlignment = new StringFormat
                    {
                        LineAlignment = StringAlignment.Center,
                        Alignment = StringAlignment.Center
                    };

                    switch (column)
                    {
                        case IncidentColumns.Service:
                            {
                                if (businessServicesShortNames.Count == 1)
                                {

                                }
                                Debug.Assert(businessServicesShortNames.Count == icons.Count);

                                var serviceHeight = Math.Max(minimumServiceRowHeight, !businessServicesShortNames.Any() ? bounds.Height : bounds.Height / businessServicesShortNames.Count);


                                const int padding = 2;
                                var iconWidth = minimumServiceRowHeight - 2 * padding;

                                for (var i = 0; i < businessServicesShortNames.Count; i++)
                                {
                                    var rowBounds = new RectangleF(bounds.X, bounds.Y + i * serviceHeight, bounds.Width,
                                        serviceHeight);

                                    var icon = icons[i];
                                    var serviceName = businessServicesShortNames[i];


                                    var iconBounds = rowBounds.AlignRectangle(iconWidth, iconWidth,
                                        StringAlignment.Near, StringAlignment.Center, padding);
                                        
                                        
                                    
                                    e.Graphics.DrawImage(icon, iconBounds);


                                    var textBounds = new RectangleFFromBounds
                                    {
                                        Left = iconBounds.Right + padding,
                                        Right = rowBounds.Right,
                                        Top = rowBounds.Top,
                                        Bottom = rowBounds.Bottom
                                    }.ToRectangleF();

                                    e.Graphics.DrawString(serviceName, servicesFont, Brushes.Black, textBounds, new StringFormat
                                    {
                                        LineAlignment = StringAlignment.Center,
                                        Alignment = StringAlignment.Far
                                    });
                                }

                                
                            }
                            
                            break;
                        case IncidentColumns.Incident:
                        {
                            var incidentActive = businessServicesImpacted.Any(s => !s.GetBooleanAttribute("up", true));
                            var isInWorkAround = businessServicesImpacted.Any(s => s.GetIntAttribute("workingAround", 0) > 0);
                            var isBreached = businessServicesImpacted.Any(s => s.GetBooleanAttribute("slabreach", false));

                            if (incidentActive || isInWorkAround || isBreached)
                            {
                                var incidentColour = SkinningDefs.TheInstance.GetColorData("incident_colour");
                                var slaBreachColour = SkinningDefs.TheInstance.GetColorData("incident_breached_colour");
                                

                                var incidentId = businessServicesImpacted.First().GetAttribute("incident_id");
                                var backColour = isBreached ? slaBreachColour : incidentColour;

	                            var blockedByOtherIncident = incidentId != breakIncident?.ID;

	                            var hatchedName = isBreached ? "hatched_breached" : "hatched_incident";
								
								if (blockedByOtherIncident)
                                {
										if (!nameToHatchedImage.ContainsKey(hatchedName))
										{
											var hatchProperties = new HatchFillProperties
											{
												LineColour = backColour.Tint(0.4f),
												AltLineColour = CONVERT.ParseHtmlColor("#c8c5c5"),
												LineWidth = 10,
												AltLineWidth = 10,
												Angle = -45
											};

											nameToHatchedImage[hatchedName] = new HatchFillImage(hatchProperties);
										}
										

	                                var hatchedImage = nameToHatchedImage[hatchedName];

									hatchedImage.RenderToBounds(e.Graphics, bounds);

                                }
								else
								{
									using (var incidentBackBrush = new SolidBrush(backColour))
									{
										e.Graphics.FillRectangle(incidentBackBrush, bounds);
									}
								}
                                
                                if (isInWorkAround)
                                {
                                    var workaroundColour = SkinningDefs.TheInstance.GetColorData("lozenge_incident_workaround_colour");

                                    var workAroundTimeRemaining = businessServicesImpacted.Max(s => s.GetIntAttribute("workingAround", 0));
                                    var workAroundTotalTime = businessServicesImpacted.Max(s => s.GetIntAttribute("workaround_time", 120));

                                    var progressFraction = workAroundTimeRemaining / (float) workAroundTotalTime;

                                    using (var workAroundBrush = new SolidBrush(workaroundColour))
                                    {
                                        e.Graphics.FillRectangle(workAroundBrush,
                                            bounds.AlignRectangle(bounds.Width * progressFraction, bounds.Height * 0.2f,
                                                StringAlignment.Near, StringAlignment.Far));
                                    }
                                }
                            }

                            e.Graphics.DrawString(columnContent[column], font, Brushes.Black, bounds, stringAlignment);
                        }
                            
                            break;
                        case IncidentColumns.Event:
                            var spikeActive = businessServicesImpacted.Any(s => s.GetIntAttribute("danger_level", 0) == 100);

                            if (spikeActive && !string.IsNullOrEmpty(columnContent[column]))
                            {
                                using (var eventBackBrush = new SolidBrush(CONVERT.ParseHtmlColor("#f2a813")))
                                {
                                    e.Graphics.FillRectangle(eventBackBrush, bounds);
                                }
                            }
                            e.Graphics.DrawString(columnContent[column], font, Brushes.Black, bounds, stringAlignment);
                            break;
                        case IncidentColumns.Failure:
                        case IncidentColumns.Impact:
                        case IncidentColumns.Question:
                        case IncidentColumns.Answer:
                            e.Graphics.DrawString(columnContent[column], font, Brushes.Black, bounds, stringAlignment);
                            break;
                    }
                }
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
        

		protected override void OnMouseClick (MouseEventArgs e)
		{
			base.OnMouseClick(e);

		    menu?.Close();

			if (notSelected)
			{
				return;
			}

		    menu = new DevOpsLozengePopupMenu
		    {
		        BackColor = Color.FromArgb(247, 148, 51)
		    };
		    menu.AddHeading(string.Join(", ", (breakIncident ?? warningIncident).GetBusinessServicesAffected(model).Select(s => s.GetAttribute("shortdesc"))));
			menu.AddDivider();

			if (warningIncident != null)
			{
				var active = (warningIncident.GetBusinessServicesAffected(model).First().GetIntAttribute("danger_level", 0) == 100);

				AddItem(menu, $"Spike ({warningIncident.ID})", new Action { Fix = false, IncidentId = warningIncident.ID, ActionType = "IncidentNumber" }).Enabled = ! active;
				AddItem(menu, $"Cancel spike (remove {warningIncident.ID})", new Action { Fix = true, IncidentId = warningIncident.ID, ActionType = "entrypanel_fix" }).Enabled = active;
			}

			if (breakIncident != null)
			{
				var active = (model.GetNodesWithAttributeValue("incident_id", breakIncident.ID).Count > 0);

				AddItem(menu, $"Incident ({breakIncident.ID})", new Action { Fix = false, IncidentId = breakIncident.ID, ActionType = "IncidentNumber"}).Enabled = ! active;
				AddItem(menu, $"Fix incident (remove {breakIncident.ID})", new Action { Fix = true, IncidentId = breakIncident.ID, ActionType = "entrypanel_fix" }, @"lozenges\server_edit.png").Enabled = active;

			    AddItem(menu, "Fix By Consultancy", new Action { Fix = true, IncidentId = breakIncident.ID, ActionType = "fix by consultancy" }, @"lozenges\server_lightning.png").Enabled = active;
			    AddItem(menu, "Workaround", new Action { Fix = true, IncidentId = breakIncident.ID, ActionType = "workaround" }, @"lozenges\arrow_rotate_clockwise.png").Enabled = active;
			    AddItem(menu, "First Line Fix", new Action { Fix = true, IncidentId = breakIncident.ID, ActionType = "first_line_fix" }, @"lozenges\1stline_fix.png").Enabled = active;
            }

			AddItem(menu, "Close Menu", new Action { Close = true }, @"lozenges\cancel.png").Enabled = true;

			menu.FormClosed += menu_Closed;

			menu.Show(TopLevelControl, this, PointToScreen(new Point (e.X, e.Y)));
		}

	    // ReSharper disable once ParameterHidesMember
	    DevOpsLozengePopupMenu.DevOpsLozengePopupMenuItem AddItem (DevOpsLozengePopupMenu menu, string text, Action action, string imageFileName = "")
		{
			var item = menu.AddItem(text, imageFileName);
			item.Chosen += menuItem_Chosen;
			item.Tag = action;

			return item;
		}

		void menuItem_Chosen (object sender, EventArgs args)
		{
			if (menu == null)
			{
				return;
			}

			var menuItem = (DevOpsLozengePopupMenu.DevOpsLozengePopupMenuItem) sender;
			var action = (Action) (menuItem.Tag);

		    if (!action.Close)
		    {
				ProcessAction(action);
		    }

		    menu.Close();
		}

		void ProcessAction (Action action)
		{
			if (action.Fix)
			{
				new Node(fixItQueue, action.ActionType, "",
					new AttributeValuePair("incident_id", action.IncidentId));
			}
			else
			{
				new Node(incidentQueue, action.ActionType, "", new AttributeValuePair("id", action.IncidentId));
			}
		}
        
	}
}