using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;

using Network;
using TransitionObjects;
using LibCore;

using IncidentManagement;

namespace CommonGUI
{
	/// <summary>
	/// This is very much the normal PendingAction Controls but it will also display the virtual incidents (91,92,93,94)
	/// These are fully fledged incidents as these have no direct effect and duration etc 
	/// So this means that that the pending controls can't work out where to place them in the list 
	/// We just put them to the end for now
	/// </summary>
	public class IBM_CLD_PendingActionsControlWithIncidents : PendingActionsControlWithIncidents
	{
		protected ArrayList Allowed_Extra_IncidentNumbers = new ArrayList();
	
		public IBM_CLD_PendingActionsControlWithIncidents (OpsControlPanelBase mainPanel, NodeTree model,
		                                           ProjectManager prjmanager, Boolean IsTrainingMode,
		                                           Color OperationsBackColor, Color GroupPanelBackColor,
		                                           GameManagement.NetworkProgressionGameFile gameFile)
			: base (mainPanel, model, prjmanager, IsTrainingMode, OperationsBackColor, GroupPanelBackColor, gameFile)
		{
		}

		/// <summary>
		/// This is used to set up the numbers that are virtual incidents 
		/// We still need to display them but the have little details behind them 
		/// </summary>
		protected override void AdditionalConfigSetup()
		{
			Allowed_Extra_IncidentNumbers.Add("91");
			Allowed_Extra_IncidentNumbers.Add("92");
			Allowed_Extra_IncidentNumbers.Add("93");
			Allowed_Extra_IncidentNumbers.Add("94");
		}

		/// <summary>
		/// This is a copy of the absolute parent function 
		/// </summary>
		protected virtual void BuildDisplayBase()
		{
			if (PendingAction_Times.Count > 0)
			{
				CountDisplayOPs = 0;
				PendingAction_Times.Sort();
				foreach (int whenvalue in PendingAction_Times)
				{
					string displayStr = string.Empty;
					object target = null;
					int buildcount = 0;

					if (PendingAction_DisplayStrings.ContainsKey(whenvalue))
					{
						displayStr = (string)PendingAction_DisplayStrings[whenvalue];
						buildcount++;
					}
					if (PendingAction_Objects.ContainsKey(whenvalue))
					{
						target = (object)PendingAction_Objects[whenvalue];
						buildcount++;
					}
					if (buildcount == 2)
					{
						BuildButton(displayStr, target);
					}
				}
			}
		}


		protected override void BuildDisplay ()
		{
			scrollablePanel.SuspendLayout();
			scrollablePanel.Controls.Clear();
			scrollablePanel.ResumeLayout(false);

			xoffset = 0;
			yoffset = 0;

			BuildDisplayBase();

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
				ReportBuilder.OpsIncidentsReport oir = new ReportBuilder.OpsIncidentsReport ();
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
			ArrayList virtual_incidents = new ArrayList();

			foreach (ReportBuilder.IncidentData incidentData in unFilteredIncidents)
			{
				string incident = incidentData.incident_id;
				if (Allowed_Extra_IncidentNumbers.Contains(incident))
				{
					virtual_incidents.Add(incidentData);
				}
				else
				{
					if (incidentData.duration >= 0)
					{
						// Skip any incidents with text in the name (eg suzuka_overload).
						int incidentNumber = CONVERT.ParseIntSafe(incident, -1);
						if (CONVERT.ToStr(incidentNumber) == incident)
						{
							// Skip any that are install penalties.
							OpsControlPanelBase ocp = _mainPanel as OpsControlPanelBase;
							IncidentDefinition incidentDefinition = ocp.IncidentApplier.GetIncident(incident);
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

				Label label = new Label ();
				label.Text = incident;
				label.ForeColor = MyTitleForeColor;
				label.BackColor = Color.Transparent;
				label.Font = MyDefaultSkinFontNormal8;
				label.TextAlign = ContentAlignment.MiddleCenter;
				label.Location = new Point (incidentsTitle.Left, y);
				label.Tag = "incident";
				y += 25;
				scrollablePanel.Controls.Add(label);
			}

			foreach (ReportBuilder.IncidentData incidentData in virtual_incidents)
			{
				string incident = incidentData.incident_id;

				Label label = new Label();
				label.Text = incident;
				label.ForeColor = MyTitleForeColor;
				label.BackColor = Color.Transparent;
				label.Font = MyDefaultSkinFontNormal8;
				label.TextAlign = ContentAlignment.MiddleCenter;
				label.Location = new Point(incidentsTitle.Left, y);
				label.Tag = "incident";
				y += 25;
				scrollablePanel.Controls.Add(label);
			}

			scrollablePanel.ResumeLayout(false);
			scrollablePanel.AutoScroll = true;

			AutoChangeBackgroundColour();
		}

	}
}