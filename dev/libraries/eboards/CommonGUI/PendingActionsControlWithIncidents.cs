using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;

using Network;
using TransitionObjects;
using CoreUtils;
using LibCore;

using IncidentManagement;

namespace CommonGUI
{
	public class PendingActionsControlWithIncidents : PendingActionsControl
	{
		protected class StartTimeComparer : IComparer
		{
			public StartTimeComparer ()
			{
			}

			int IComparer.Compare (object a, object b)
			{
				ReportBuilder.IncidentData incidentA = a as ReportBuilder.IncidentData;
				ReportBuilder.IncidentData incidentB = b as ReportBuilder.IncidentData;

				return incidentA.seconds - incidentB.seconds;
			}
		}

		protected Node fixItQueueNode;
		protected Node incidentEntryQueueNode;
		protected Panel scrollablePanel;
		protected Label incidentsTitle;
		protected GameManagement.NetworkProgressionGameFile gameFile;
		protected bool restrictDisplayedIncidentNumbers = false;

		public PendingActionsControlWithIncidents (IDataEntryControlHolder mainPanel, NodeTree model,
		                                           ProjectManager prjmanager, Boolean IsTrainingMode,
		                                           Color OperationsBackColor, Color GroupPanelBackColor,
		                                           GameManagement.NetworkProgressionGameFile gameFile)
			: base (mainPanel, model, prjmanager, IsTrainingMode, OperationsBackColor, GroupPanelBackColor)
		{
			this.gameFile = gameFile;

			AdditionalConfigSetup();

			fixItQueueNode = model.GetNamedNode("FixItQueue");
			fixItQueueNode.ChildAdded += fixItQueueNode_ChildAdded;

			incidentEntryQueueNode = model.GetNamedNode("enteredIncidents");
			incidentEntryQueueNode.ChildAdded += incidentEntryQueueNode_ChildAdded;

			restrictDisplayedIncidentNumbers = SkinningDefs.TheInstance.GetBoolData("restrict_display_incidents_mod100",false);

			BuildDisplay();
		}

		protected override void Dispose (bool disposing)
		{
			if (fixItQueueNode != null)
			{
				fixItQueueNode.ChildAdded -= fixItQueueNode_ChildAdded;
				fixItQueueNode = null;
			}

			if (incidentEntryQueueNode != null)
			{
				incidentEntryQueueNode.ChildAdded -= incidentEntryQueueNode_ChildAdded;
			}

			base.Dispose(disposing);
		}

		/// <summary>
		/// This is a helper method to allow extra setup to happen before the Builddisplay is called in the constructor  
		/// </summary>
		protected virtual void AdditionalConfigSetup()
		{
		}


		protected override void BuildControls ()
		{
			base.BuildControls();

		    BackColor = Color.Orange;

            string openIncidentsTextReplacement = SkinningDefs.TheInstance.GetData("esm_open_incidents_text_replacement", "Open Incidents");
		    incidentsTitle = new Label
		    {
		        Font = SkinningDefs.TheInstance.GetFont(12,
		            SkinningDefs.TheInstance.GetBoolData("ops_title_use_bold_font", true)
		                ? FontStyle.Bold : FontStyle.Regular),
		        Text = openIncidentsTextReplacement,
		        TextAlign = ContentAlignment.MiddleLeft,
		        Size = new Size(Width - title.Left, 20),
		        BackColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("popup_title_background_colour",
		            MyOperationsBackColor),
		        ForeColor = MyTitleForeColor,
		        Location = new Point(350, 0)
		    };
		    Controls.Add(incidentsTitle);
	
			scrollablePanel = new Panel ();
			scrollablePanel.Location = new Point (0, title.Bottom + 10);
			Controls.Add(scrollablePanel);
		}

		protected override void BuildDisplay ()
		{
			scrollablePanel.SuspendLayout();
			scrollablePanel.Controls.Clear();
			scrollablePanel.ResumeLayout(false);

			xoffset = 15;
			yoffset = 0;

			base.BuildDisplay();

			if (gameFile == null)
			{
				return;
			}

			Node timeNode = MyNodeTreeHandle.GetNamedNode("CurrentTime");
			int time = timeNode.GetIntAttribute("seconds", 0);

			ArrayList unFilteredIncidents;

			// In a normal game, we can use the network log to get the incidents and the order they
			// happened in.
			if (! gameFile.IsSalesGame)
			{
				ReportBuilder.OpsIncidentsReport oir = new ReportBuilder.OpsIncidentsReport();
				oir.BuildReport(gameFile, gameFile.CurrentRound, null);
				unFilteredIncidents = oir.GetOutstandingIncidents();
			}
			else
			{
				// In a sales game, the network log will be inappropriate, as it will refer
				// to the saved game not the current round.  Work around this.
				unFilteredIncidents = new ArrayList ();

				Hashtable nodesToIncidents = MyNodeTreeHandle.GetNodesWithAttribute("incident_id");
				foreach (Node node in nodesToIncidents.Keys)
				{
					string incident = (string) nodesToIncidents[node];
					int startTime = 0;
					int duration = 0;
					bool knownDuration = false;
					ReportBuilder.IncidentData incidentData = null;

					// Because we don't know the real start time, we guess it from
					// how long things have been down.
					int maxValue = 100000;
					int downFor = node.GetIntAttribute("downforsecs", maxValue);
					int mirroredFor = node.GetIntAttribute("mirrorforsecs", maxValue);

					// The smaller of the two is a decent guess at how long the incident's been active
					duration = Math.Min(downFor, mirroredFor);
					if (duration == maxValue)
					{
						knownDuration = false;
						duration = 100000;
					}
					else
					{
						knownDuration = true;
					}
					startTime = time - duration;

					// Have we recorded this incident already?  Find it if so.
					foreach (ReportBuilder.IncidentData compare in unFilteredIncidents)
					{
						if (compare.incident_id == incident)
						{
							incidentData = compare;
							break;
						}
					}

					// If we didn't already have it, add it.
					if (incidentData == null)
					{
						incidentData = new ReportBuilder.IncidentData (incident, startTime);
						incidentData.duration = duration;
						unFilteredIncidents.Add(incidentData);
					}
					else
					{
						// We already knew about it: just update the timings.
						if (knownDuration)
						{
							incidentData.seconds = Math.Max(incidentData.seconds, startTime);
							incidentData.duration = Math.Min(incidentData.duration, duration);
						}
					}
				}
			}

			// Filter out any fake incidents.
			ArrayList incidents = new ArrayList ();
			foreach (ReportBuilder.IncidentData incidentData in unFilteredIncidents)
			{
				if (incidentData.duration >= 0)
				{
					string incident = incidentData.incident_id;

					// Skip any incidents with text in the name (eg suzuka_overload).
					int incidentNumber = CONVERT.ParseIntSafe(incident, -1);
					if (CONVERT.ToStr(incidentNumber) == incident)
					{
						// Skip any that are install penalties.
						IncidentDefinition incidentDefinition = _mainPanel.IncidentApplier.GetIncident(incident);

						if ((incidentDefinition != null) && !incidentDefinition.IsPenalty)
						{
							if (incidents.IndexOf(incident) == -1)
							{
								incidents.Add(incidentData);
							}
						}
					}
				}
			}

			// Order them by start time.
			incidents.Sort(new StartTimeComparer ());

			scrollablePanel.SuspendLayout();
			scrollablePanel.AutoScroll = true;
			scrollablePanel.AutoScrollPosition = new Point (0, 0);

			// Remove any old labels.
			ArrayList kill = new ArrayList ();
			foreach (Control control in scrollablePanel.Controls)
			{
				Label label = control as Label;
				if ((label != null) && (((string) label.Tag) == "incident"))
				{
					kill.Add(label);
				}
			}
			foreach (Control control in kill)
			{
				scrollablePanel.Controls.Remove(control);
			}

			int y = 0;
			foreach (ReportBuilder.IncidentData incidentData in incidents)
			{
				string incident = incidentData.incident_id;

				if (restrictDisplayedIncidentNumbers)
				{
					int inc_id = CONVERT.ParseIntSafe(incident, -1);
					if (inc_id != -1)
					{
						incident = CONVERT.ToStr(inc_id % 100);
					}
				}

				Label label = new Label ();
				label.Text = incident;
				label.ForeColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("pending_action_text_colour", Color.Black);
				label.Font = MyDefaultSkinFontNormal8;
				label.TextAlign = ContentAlignment.MiddleCenter;
				label.Location = new Point (incidentsTitle.Left, y);
			    label.Size = new Size(incidentsTitle.PreferredWidth, 25);
				label.Tag = "incident";
				y += 25;
				scrollablePanel.Controls.Add(label);
			}
			scrollablePanel.ResumeLayout(false);
			scrollablePanel.AutoScroll = true;

			AutoChangeBackgroundColour();
		}

		protected void fixItQueueNode_ChildAdded (Node sender, Node child)
		{
			BuildDisplay();
		}

		protected void incidentEntryQueueNode_ChildAdded(Node sender, Node child)
		{
			BuildDisplay();
		}

		protected void AutoChangeBackgroundColour()
		{
			scrollablePanel.AutoScrollMargin = new Size (0, 0);
			scrollablePanel.AutoScroll = false;
			scrollablePanel.AutoScrollPosition = new Point (0, 0);

			// Now work out if the autoscroll has been enabled.  The only way to do this is to
			// measure our children's extents...
			Rectangle rect = new Rectangle (0, 0, 0, 0);
			foreach (Control control in scrollablePanel.Controls)
			{
				rect.Location = new Point (Math.Min(rect.Left, control.Left), Math.Min(rect.Top, control.Top));
				rect.Width = Math.Max(rect.Width, control.Right - rect.Left);
				rect.Height = Math.Max(rect.Height, control.Bottom - rect.Top);
			}
			bool autoScroll = (rect.Right > scrollablePanel.Width) || (rect.Bottom > scrollablePanel.Height);

			// Now if we have a scrollbar, change the background colour, to mask the nasty redraw bugs
			// in the background!
			string background = SkinningDefs.TheInstance.GetData("pending_panel_background");
			if (autoScroll && (background != ""))
			{
				scrollablePanel.BackColor = CONVERT.ParseComponentColor(background);
			}
			else
			{
				scrollablePanel.BackColor = Color.Transparent;
			}

			scrollablePanel.AutoScroll = true;
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);

			scrollablePanel.Size = new Size (Width - scrollablePanel.Left - 20, Height - scrollablePanel.Top - 40);

		    if (SkinningDefs.TheInstance.GetBoolData("use_custom_popup_header", false))
		    {
                title.Bounds = new Rectangle(0, 0, (int)(Width * 0.7), SkinningDefs.TheInstance.GetIntData("ops_popup_title_height", 25));
                incidentsTitle.Bounds = new Rectangle(title.Right, title.Top, Width - title.Right, title.Height);

                scrollablePanel.Bounds = new Rectangle(10, incidentsTitle.Bottom + 5, Width - 20, (cancelButton.Top - 10) - incidentsTitle.Bottom + 5);

                BuildDisplay();
		    }
		    else
		    {
			    title.Bounds = new Rectangle (0, 0, 350, SkinningDefs.TheInstance.GetIntData("ops_popup_title_height", 25));
			    incidentsTitle.Bounds = new Rectangle (title.Right, title.Top, Width - title.Right, title.Height);
			}

			AutoChangeBackgroundColour();
		}

		protected override void BuildButton (string DisplayString, object n1)
		{
			Color upColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_text_colour", Color.Black);
			Color downColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_down_text_colour", Color.White);
			Color hoverColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_hover_text_colour", Color.Green);
			Color disabledColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_disabled_text_colour", Color.DarkGray);

			Label text = new Label();
			text.Font = MyDefaultSkinFontNormal8;
			text.Text = DisplayString;
			text.Size = new Size(235,20);
			text.Location = new Point(5+xoffset,yoffset+2);
			text.BackColor = MyOperationsBackColor;
			text.ForeColor = DisplayTextForeColor;

			//text.BackColor =Color.Cyan;
			scrollablePanel.Controls.Add(text);

			ImageTextButton button = new StyledDynamicButtonCommon ("standard", "Cancel");
			button.Font = MyDefaultSkinFontBold10;
			button.Size = new Size(60,20);
			button.Location = new Point(240+xoffset,yoffset);
			button.Tag = n1;
			button.Click += button_Click;
			scrollablePanel.Controls.Add(button);

			focusJumper.Add(button);

			yoffset += 25;
			CountDisplayOPs++;
		}
	}
}