using System;
using System.Xml;
using System.Collections;
using System.Collections.Generic;

using GameManagement;
using LibCore;
using CoreUtils;

using Network;

using Logging;
using System.IO;

namespace ReportBuilder
{
	public class IncidentData
	{
		public string incident_id;
		public int seconds;
		public ArrayList users_affected = new ArrayList();
		public string failure = "";
		public int duration = -1;
		public bool isFacilities = false;
		public bool isHVAC = false;
		public bool isPower = false;
		public bool isThermal = false;
		public string Zone = "";
		public string Desc = "";
		public string Note = "";
		public string hw_category = "";
		public string recommend_solution  = "";


		public IncidentData(string id, int secs)
		{
			incident_id = id;
			seconds = secs;
		}

		public IncidentData (IncidentData copy)
		{
			duration = copy.duration;
			failure = copy.failure;
			incident_id = copy.incident_id;
			seconds = copy.seconds;
			users_affected = copy.users_affected;
			isFacilities = copy.isFacilities;
			isHVAC = copy.isHVAC;
			isPower = copy.isPower;
			isThermal = copy.isThermal;
			Zone = copy.Zone;
			Desc = copy.Desc;
			Note = copy.Note;
			hw_category = copy.hw_category;
			recommend_solution = copy.recommend_solution;
		}
	}

	/// <summary>
	/// Summary description for OpsIncidentsReport.
	/// </summary>
	public class OpsIncidentsReport
	{
		protected NetworkProgressionGameFile _gameFile;
		protected string RowHeight = SkinningDefs.TheInstance.GetIntData("incident_table_row_height", 23).ToString();

		protected Hashtable incidentToData;
		protected ArrayList events;

		Dictionary<string, List<string>> incidentIdToAffectedNodeNames;

		protected string biz_type;

		protected char[] space = { ' ' };

        protected string header_colour = SkinningDefs.TheInstance.GetData("fis_reports_green_header_colour", "176,196,222");
		protected string text_colour = "0,0,0";
		protected string table_border_color = "211,211,211";

		bool includeReportNotes = false;
		string columnReportNotes = "";
		bool collateIncidents;
		bool specialRound2Behaviour;
		List<IncidentData> closedIncidents;

		bool restrictDisplayedIncidentNumbers = false;

		public OpsIncidentsReport()
		{
			restrictDisplayedIncidentNumbers = SkinningDefs.TheInstance.GetBoolData("restrict_display_incidents_mod100", false);

			incidentIdToAffectedNodeNames = new Dictionary<string, List<string>> ();

			biz_type = CoreUtils.SkinningDefs.TheInstance.GetData("biz");
			string hcol = CoreUtils.SkinningDefs.TheInstance.GetData("table_header_color");
			if(hcol != "")
			{
				header_colour = hcol;
			}
			//
			hcol = CoreUtils.SkinningDefs.TheInstance.GetData("table_header_text_color");
			if(hcol != "")
			{
				text_colour = hcol;
			}
			//
			hcol = CoreUtils.SkinningDefs.TheInstance.GetData("table_border_color");
			if(hcol != "")
			{
				table_border_color = hcol;
			}

			collateIncidents = true;
			specialRound2Behaviour = true;
			closedIncidents = new List<IncidentData> ();
		}

		public void enableReportNotes(string column_title)
		{
			includeReportNotes = true;
			columnReportNotes = column_title;
		}

		/// <summary>
		/// If passed true, then record multiple nonoverlapping occurrences of the same incident ID separately.
		/// </summary>
		public void SeparateIncidents (bool separate)
		{
			collateIncidents = ! separate;
			specialRound2Behaviour = ! separate;
		}

		public string BuildReport(NetworkProgressionGameFile gameFile, int round, ArrayList Scores)
		{
			if ((Scores != null) && (Scores.Count >= round))
			{
				RoundScores scores = Scores[round - 1] as RoundScores;
				if (scores != null)
				{
					lastKnownTimeInGame = (int) scores.FinalTime;
				}
			}

			_gameFile = gameFile;
			//Create the xml report
			string reportFile = gameFile.GetRoundFile(round, "OpsIncidentsReport_Round" + round + ".xml" , GameManagement.GameFile.GamePhase.OPERATIONS);
			
			LibCore.BasicXmlDocument xdoc = LibCore.BasicXmlDocument.Create();
			XmlNode root = (XmlNode) xdoc.CreateElement("table");
			((XmlElement)root).SetAttribute( "columns","1" );
			((XmlElement)root).SetAttribute( "rowheight", RowHeight );
			((XmlElement)root).SetAttribute( "border_colour", table_border_color );
			string rowColour = SkinningDefs.TheInstance.GetData("table_row_colour", "255,255,255");
			((XmlElement)root).SetAttribute( "row_colour_1", rowColour);
			((XmlElement)root).SetAttribute( "row_colour_2", SkinningDefs.TheInstance.GetData("table_row_colour_alternate", rowColour));
			//	((XmlElement)root).SetAttribute( "heights", "0.04,0.8");
			xdoc.AppendChild(root);

			// Special case for round 2, need to show round 1 and round 2 results
			if ((round == 2) && specialRound2Behaviour)
			{
				for (int r=1; r<=2; r++)
				{
					AddRoundTable(r, xdoc, root);
				}
			}
			else
			{
				AddRoundTable(round, xdoc, root);
			}

			xdoc.SaveToURL("",reportFile);

			return reportFile;
		}

		int lastKnownTimeInGame = 0;

		/// <summary>
		/// This used to modify the display incident number 
		/// In some sims, we use extra incidents with very high numbers (>100) for redirected incidents 
		/// but we want to ensure that the display matchs the original incidents that the facilitator entered
		///   The faciliator might use 20 and the internal incidents will be 120, 220, 320 across the 3 rounds)
		///   so if we us (internal incident mod 100), the report will look correct across the 3 rounds 
		/// So if switched on in the skin, we process the incident numbers
		/// </summary>
		/// <param name="incident_id"></param>
		/// <returns></returns>
		protected virtual string getIncident_display_string(string incident_id)
		{
			string display_incident = incident_id;

			if (restrictDisplayedIncidentNumbers)
			{
				int inc_id = CONVERT.ParseIntSafe(display_incident, -1);
				if (inc_id != -1)
				{
					display_incident = CONVERT.ToStr(inc_id % 100);
				}
			}
			return display_incident;
		}

		protected void FillTable(XmlDocument xdoc, XmlNode boardtable, int _round)
		{
			XmlNode row, cell;

			// Pull the logfile to get data from.
			string logFile = _gameFile.GetRoundFile(_round,"NetworkIncidents.log", GameManagement.GameFile.GamePhase.OPERATIONS);
			if (_gameFile.LastRoundPlayed == _round && _gameFile.LastPhasePlayed == GameManagement.GameFile.GamePhase.TRANSITION)
			{
				//no ops phase yet so don't set ops log file
				logFile = "";
				return;
			}

			if(File.Exists(logFile))
			{
				incidentToData = new Hashtable();
				events = new ArrayList();

				BasicIncidentLogReader biLogReader = new BasicIncidentLogReader(logFile);

				biLogReader.WatchCreatedNodes("enteredIncidents", this.biLogReader_enteredIncidents);
				biLogReader.WatchApplyAttributes("", this.biLogReader_apply);
				biLogReader.WatchCreatedNodes("FixItQueue", this.biLogReader_FixItQueue);
				biLogReader.WatchCreatedNodes("CostedEvents", this.biLogReader_CostedEvents);

				biLogReader.Run();

				// Remove any incidents that had no effect as these were prevented by overlaps or
				// upgrades.
				foreach(IncidentData idata in this.incidentToData.Values)
				{
					// But don't remove ones that should be visible anyway.
					if (idata.incident_id.ToLower().IndexOf("prevented") == -1)
					{
						if (idata.users_affected.Count == 0)
						{
							events.Remove(idata);
						}
					}
				}

				// Sort the list by the time the incident started, not by when it was fixed!

				foreach(IncidentData idata in this.events)
				{
					// Don't include prevented incidents at all.
					if (idata.incident_id.ToLower().StartsWith("(prevented)"))
					{
						continue;
					}

					// Output data to the report table.
					row = (XmlNode) xdoc.CreateElement("rowdata");
					boardtable.AppendChild(row);

					int mins = idata.seconds/60;
					int secs = idata.seconds - mins*60;

					string time = LibCore.CONVERT.ToStr(mins).PadLeft(2,' ') + ":" + LibCore.CONVERT.ToStr(secs).PadLeft(2,'0');

					cell = (XmlNode) xdoc.CreateElement("cell");
					((XmlElement)cell).SetAttribute( "val", time );
					row.AppendChild(cell);

					cell = (XmlNode) xdoc.CreateElement("cell");
					((XmlElement)cell).SetAttribute( "val", getIncident_display_string(idata.incident_id));
					row.AppendChild(cell);

					idata.users_affected.Sort();
					string users = "";

					if(idata.users_affected.Count == 4)
					{
						users = "All";
					}
					else
					{
						for(int i=0; i<idata.users_affected.Count; ++i)
						{
							if(i>0) users += ",";
							users += idata.users_affected[i];
						}
					}

					cell = (XmlNode) xdoc.CreateElement("cell");
					((XmlElement)cell).SetAttribute( "val", users );
					row.AppendChild(cell);

					cell = (XmlNode) xdoc.CreateElement("cell");
					((XmlElement)cell).SetAttribute( "val", idata.failure );
					row.AppendChild(cell);

					if (includeReportNotes)
					{
						cell = (XmlNode)xdoc.CreateElement("cell");
						((XmlElement)cell).SetAttribute("val", idata.Note);
						row.AppendChild(cell);
					}

					cell = (XmlNode) xdoc.CreateElement("cell");

					if(idata.duration == -1)
					{
						idata.duration = lastKnownTimeInGame - idata.seconds;
					}
/*
					if(idata.duration == -1)
					{
						((XmlElement)cell).SetAttribute( "val", "Not Fixed" );
					}
					else*/
					{
						mins = idata.duration/60;
						secs = idata.duration - mins*60;

						time = LibCore.CONVERT.ToStr(mins).PadLeft(2,' ') + ":" + LibCore.CONVERT.ToStr(secs).PadLeft(2,'0');

						((XmlElement)cell).SetAttribute( "val", time );
					}
					row.AppendChild(cell);
				}
			}
		}

		public ArrayList GetOutstandingIncidents ()
		{
			return new ArrayList (incidentToData.Values);
		}

		void biLogReader_CostedEvents(object sender, string key, string line, double time)
		{
			if(time > lastKnownTimeInGame) lastKnownTimeInGame = (int) time;

			string incident_id = BasicIncidentLogReader.ExtractValue(line, "incident_id");
			string failure = "";

			// : Incidents prevented by an upgrade are just given as costed_events with no
			// useful information.  Try to extract it from the text.
			if (incident_id == "")
			{
				string search = "Cannot Apply ";

				int start = line.ToLower().IndexOf(search.ToLower());
				if (start != -1)
				{
					string subString = line.Substring(start + search.Length);

					string [] words = subString.Split(' ');
					if (words.Length >= 2)
					{
						incident_id = words[1];
					}

					if (words.Length >= 4)
					{
						string target = words[3];

						Network.Node node = _gameFile.NetworkModel.GetNamedNode(target);
						if (node != null)
						{
							failure = node.GetAttribute("type") + " " + target;
						}
					}
				}
			}

			// Server-turn-off incidents can show up as costed events without first appearing
			// in the enteredIncidents queue.  Specifically handle them here, rather than
			// blithely catching all incidents that don't go through enteredIncidents,
			// so as not to break anything.
			if (incident_id.ToLower().Contains("turn_off") && ! incidentToData.ContainsKey(incident_id))
			{
				IncidentData idata = new IncidentData (incident_id, (int) time);
				incidentToData.Add(incident_id, idata);
			}

			// Look for some extra properties and tag them.
			if (incidentToData.ContainsKey(incident_id))
			{
				IncidentData idata = (IncidentData) incidentToData[incident_id];

				// Zone.
				string zoneOf = BasicIncidentLogReader.ExtractValue(line, "zoneof");
				if (zoneOf != "")
				{
					Node zoneNode = _gameFile.NetworkModel.GetNamedNode(zoneOf);
					if (zoneNode != null)
					{
						string zone = zoneNode.GetAttribute("zone");
						if (zone == "")
						{
							zone = zoneNode.GetAttribute("proczone");
						}
						if (zone == "")
						{
							string name = zoneNode.GetAttribute("name");
							if ((name.Length == 2) && Char.IsDigit(name[1]))
							{
								zone = name[1].ToString();
							}
						}

						idata.Zone = zone;
					}
				}

				// Description.
				string desc = BasicIncidentLogReader.ExtractValue(line, "desc");
				if ((desc != "") && (idata.Desc == ""))
				{
					idata.Desc = desc;
				}

				// Report Note
				string report_note = BasicIncidentLogReader.ExtractValue(line, "report_note");
				if ((report_note != "") && (idata.Note == ""))
				{
					idata.Note = report_note;
				}

				// Power- or temperature-related?
				string dontCount = BasicIncidentLogReader.ExtractValue(line, "dont_count");
				if ((dontCount == "") || !CONVERT.ParseBool(dontCount, false))
				{
					if (CONVERT.ParseBool(BasicIncidentLogReader.ExtractValue(line, "hvac"), false))
					{
						idata.isHVAC = true;
					}

					if (CONVERT.ParseBool(BasicIncidentLogReader.ExtractValue(line, "power"), false))
					{
						idata.isPower = true;
					}

					if (CONVERT.ParseBool(BasicIncidentLogReader.ExtractValue(line, "facilities"), false))
					{
						idata.isFacilities = true;
					}

					if (CONVERT.ParseBool(BasicIncidentLogReader.ExtractValue(line, "thermal"), false))
					{
						idata.isThermal = true;
					}
				}
			}

			string type = BasicIncidentLogReader.ExtractValue(line, "type");
			if("incident" == type)
			{
				if(incidentToData.ContainsKey(incident_id))
				{
					IncidentData idata = (IncidentData) incidentToData[incident_id];
					idata.failure = BasicIncidentLogReader.ExtractValue(line, "failure");
				}

				return;
			}
				// Remove prevented incidents from our list any prevented incidents.
			else if("prevented_incident" == type)
			{
				// Search backwards through our current active events and then our previous events
				// for this id and remove the first one we find.
				if(incidentToData.ContainsKey(incident_id))
				{
					//incidentToData.Remove(incident_id);
					IncidentData idata = (IncidentData) incidentToData[incident_id];

					// : Manky fix for 3819 ("(Prevented)" appears multiple times)
					if (! idata.incident_id.ToLower().StartsWith("(prevented)"))
					{
						idata.incident_id = "(Prevented) " + idata.incident_id;
						if ((idata.failure == null) || (idata.failure == ""))
						{
							idata.failure = failure;
						}
					}
					return;
				}

				for(int i=this.events.Count-1; i>=0; --i)
				{
					IncidentData idata = (IncidentData) events[i];
					if(idata.incident_id == incident_id)
					{
						// : Manky fix for 3819 ("(Prevented)" appears multiple times)
						if (! idata.incident_id.ToLower().StartsWith("(prevented)"))
						{
							idata.incident_id = "(Prevented) " + idata.incident_id;
							if ((idata.failure == null) || (idata.failure == ""))
							{
								idata.failure = failure;
							}
						}
						//events.RemoveAt(i);
						return;
					}
				}

				// : Bug 6247: if we get here, then we must have just been told about a prevented incident that
				// we didn't even know was an incident.  Add it in.
				IncidentData iData = new IncidentData ("(Prevented) " + incident_id, (int) time);
				if ((iData.failure == null) || (iData.failure == ""))
				{
					iData.failure = failure;
				}
				incidentToData[incident_id] = iData;
				events.Add(iData);
			}
			else if ( (type == "entity_fix_by_consultancy") || (type == "entity fix by consultancy") )
			{
				// A fix by consultancy.
				FixIncident(incident_id, time);
			}
			else if ((type.StartsWith("entity fix") || type.StartsWith("fix")) && (incident_id != string.Empty))
			{
				// A fix.
				FixIncident(incident_id, time);
			}
		}

		void FixIncident(string incident_id, double time)
		{
			IncidentData idata = (IncidentData) incidentToData[incident_id];
			incidentToData.Remove(incident_id);

			// Hacky workaround for 3889: if we get an incident fixed twice in rapid succession
			// (without being re-raised inbetween), then exit gracefully.
			if (idata == null) return;

			int duration = ((int)time) - idata.seconds;
			if(duration < 1) duration = 1;

			idata.duration = duration;

			if (! collateIncidents)
			{
				closedIncidents.Add(idata);
			}
		}

		void biLogReader_FixItQueue(object sender, string key, string line, double time)
		{
			if(time > lastKnownTimeInGame) lastKnownTimeInGame = (int) time;
		}

		void biLogReader_apply(object sender, string key, string line, double time)
		{
			if(time > lastKnownTimeInGame) lastKnownTimeInGame = (int) time;

			string incident_id = BasicIncidentLogReader.ExtractValueGivenDefault(line, "incident_id", null);
			string i_name = BasicIncidentLogReader.ExtractValue(line, "i_name");
			int workaroundTime = CONVERT.ParseIntSafe(BasicIncidentLogReader.ExtractValue(line, "workingAround"), 0);

			if ((incident_id == "") // but not null, which would mean it wasn't specified on the line at all
				&& (workaroundTime <= 0))
			{
				string incidentId = null;

				// Find the nodes affected by this incident.
				foreach (string tryIncidentId in incidentIdToAffectedNodeNames.Keys)
				{
					if (incidentIdToAffectedNodeNames[tryIncidentId].Contains(i_name))
					{
						incidentId = tryIncidentId;
						break;
					}
				}

				if (! string.IsNullOrEmpty(incidentId))
				{
					System.Diagnostics.Debug.Assert(incidentIdToAffectedNodeNames.ContainsKey(incidentId));
					System.Diagnostics.Debug.Assert(incidentIdToAffectedNodeNames[incidentId].Contains(i_name));

					incidentIdToAffectedNodeNames[incidentId].Remove(i_name);
					if (incidentIdToAffectedNodeNames[incidentId].Count == 0)
					{
						incidentIdToAffectedNodeNames.Remove(incidentId);
					}

					FixIncident(incidentId, time);
				}
			}
			else if (! string.IsNullOrEmpty(incident_id))
			{
				if (! incidentIdToAffectedNodeNames.ContainsKey(incident_id))
				{
					incidentIdToAffectedNodeNames.Add(incident_id, new List<string> ());
				}

				if (! incidentIdToAffectedNodeNames[incident_id].Contains(i_name))
				{
					incidentIdToAffectedNodeNames[incident_id].Add(i_name);
				}
			}

			if (incident_id == null)
			{
				incident_id = "";
			}

			if(!this.incidentToData.ContainsKey(incident_id))
			{
				return;
			}

			IncidentData idata = (IncidentData) incidentToData[incident_id];

			if(i_name.StartsWith(biz_type))
			{
				string[] parts = i_name.Split(space);
				if(parts.Length > 1)
				{
					string which_one = parts[ 1 ];

					if(!idata.users_affected.Contains(which_one))
					{
						idata.users_affected.Add(which_one);
					}
				}
			}
		}

		void biLogReader_enteredIncidents(object sender, string key, string line, double time)
		{
			if(time > lastKnownTimeInGame) lastKnownTimeInGame = (int) time;

			string id = BasicIncidentLogReader.ExtractValue(line, "id", true);
			// : it used to be possible to enter null incident IDs under certain circumstances.
			// Filter these out now in case they're still present in old games.
			if (id == "")
			{
				return;
			}

			if(!incidentToData.ContainsKey(id))
			{
				IncidentData idata = new IncidentData( id, (int) time );
				incidentToData[id] = idata;
				events.Add(idata);
			}
			else
			{
				// The previous incident may be a dead one that got stopped by thte system. If so
				// then remove and insert a new one.
				IncidentData idata = (IncidentData) incidentToData[id];
				if(idata.users_affected.Count == 0)
				{
					incidentToData.Remove(id);
					events.Remove(idata);
					//
					idata = new IncidentData( id, (int) time );
					incidentToData[id] = idata;
					events.Add(idata);
				}
			}
		}

		void AddRoundTable(int round, BasicXmlDocument xdoc, XmlNode root)
		{
			int NumColumns = 5;
			string colwidths = "0.2,0.2,0.175,0.275,0.15";

			if (includeReportNotes)
			{
				NumColumns = 6;
				colwidths = "0.1,0.2,0.15,0.25,0.15,0.15";
			}

			XmlNode row = (XmlNode) xdoc.CreateElement("rowdata");
			root.AppendChild(row);
			XmlNode cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement)cell).SetAttribute( "val","Round " + CONVERT.ToStr(round) );
			((XmlElement)cell).SetAttribute( "align", SkinningDefs.TheInstance.GetData("incident_table_round_alignment", "left"));
			((XmlElement)cell).SetAttribute( "colour",header_colour );

			string new_colour = SkinningDefs.TheInstance.GetData("incident_table_header_text_colour");
			if (new_colour != "")
			{
				((XmlElement)cell).SetAttribute( "textcolour", new_colour );
			}
			else
			{
				((XmlElement)cell).SetAttribute( "textcolour",text_colour );
			}

			if (SkinningDefs.TheInstance.GetIntData("table_header_bold", 1) != 0)
			{
				((XmlElement)cell).SetAttribute( "textstyle", "bold" );
			}

			if (SkinningDefs.TheInstance.GetIntData("table_header_tabbed", 0) != 0)
			{
				((XmlElement)cell).SetAttribute( "tabbed", "true" );
			}

			row.AppendChild(cell);

			//add the title table
			bool useBold = (SkinningDefs.TheInstance.GetIntData("table_header_bold", 1) != 0);

			XmlNode titles = (XmlNode) xdoc.CreateElement("table");
			((XmlElement)titles).SetAttribute( "columns", CONVERT.ToStr(NumColumns) );
			((XmlElement)titles).SetAttribute( "widths", colwidths);
			((XmlElement)titles).SetAttribute( "border_colour", table_border_color );
			string rowColour = SkinningDefs.TheInstance.GetData("table_row_colour", "255,255,255");
			((XmlElement)titles).SetAttribute( "row_colour_1", rowColour);
			((XmlElement)titles).SetAttribute( "row_colour_2", SkinningDefs.TheInstance.GetData("table_row_colour_alternate", rowColour));
			root.AppendChild(titles);

			XmlNode titlerow = (XmlNode) xdoc.CreateElement("rowdata");
			titles.AppendChild(titlerow);

			cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement)cell).SetAttribute( "val", "Time");
			((XmlElement)cell).SetAttribute( "colour",header_colour );
			((XmlElement)cell).SetAttribute( "textcolour",text_colour );
			if (useBold)
			{
				((XmlElement)cell).SetAttribute( "textstyle", "bold");
			}
			titlerow.AppendChild(cell);

			cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement)cell).SetAttribute( "val","Incident Number" );
			((XmlElement)cell).SetAttribute( "colour",header_colour );
			((XmlElement)cell).SetAttribute( "textcolour",text_colour );
			if (useBold)
			{
				((XmlElement)cell).SetAttribute( "textstyle", "bold");
			}
			titlerow.AppendChild(cell);

			cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement)cell).SetAttribute( "val", "Clients");
			((XmlElement)cell).SetAttribute( "colour",header_colour );
			((XmlElement)cell).SetAttribute( "textcolour",text_colour );
			if (useBold)
			{
				((XmlElement)cell).SetAttribute( "textstyle", "bold");
			}
			titlerow.AppendChild(cell);

			cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement)cell).SetAttribute( "val", "IT Failure");
			((XmlElement)cell).SetAttribute( "colour",header_colour );
			((XmlElement)cell).SetAttribute( "textcolour",text_colour );
			if (useBold)
			{
				((XmlElement)cell).SetAttribute( "textstyle", "bold");
			}
			titlerow.AppendChild(cell);


			if (includeReportNotes)
			{
				string col_title = "Notes";
				if (string.IsNullOrEmpty(this.columnReportNotes) == false)
				{
					col_title = this.columnReportNotes;
				}

				cell = (XmlNode)xdoc.CreateElement("cell");
				((XmlElement)cell).SetAttribute("val", col_title);
				((XmlElement)cell).SetAttribute("colour", header_colour);
				((XmlElement)cell).SetAttribute("textcolour", text_colour);
				if (useBold)
				{
					((XmlElement)cell).SetAttribute("textstyle", "bold");
				}
				titlerow.AppendChild(cell);
			}

			cell = (XmlNode) xdoc.CreateElement("cell");
			((XmlElement)cell).SetAttribute( "val", "Duration");
			((XmlElement)cell).SetAttribute( "colour",header_colour );
			((XmlElement)cell).SetAttribute( "textcolour",text_colour );
			if (useBold)
			{
				((XmlElement)cell).SetAttribute( "textstyle", "bold");
			}
			titlerow.AppendChild(cell);

			//add incidents table
			XmlNode boardtable = (XmlNode) xdoc.CreateElement("table");
			((XmlElement)boardtable).SetAttribute( "columns", CONVERT.ToStr(NumColumns) );
			((XmlElement)boardtable).SetAttribute( "widths", colwidths);
			((XmlElement)boardtable).SetAttribute( "border_colour", table_border_color );
			rowColour = SkinningDefs.TheInstance.GetData("table_row_colour", "255,255,255");
			((XmlElement)boardtable).SetAttribute( "row_colour_1", rowColour);
			((XmlElement)boardtable).SetAttribute( "row_colour_2", SkinningDefs.TheInstance.GetData("table_row_colour_alternate", rowColour));
			root.AppendChild(boardtable);

			FillTable(xdoc, boardtable, round);
		}

		public List<IncidentData> Incidents
		{
			get
			{
				List<IncidentData> incidents = new List<IncidentData> ();

				foreach (IncidentData incident in incidentToData.Values)
				{
					IncidentData copy = new IncidentData (incident);

					if (incident.duration == -1)
					{
						copy.duration = lastKnownTimeInGame - copy.seconds;
					}

					incidents.Add(copy);
				}

				foreach (IncidentData incident in closedIncidents)
				{
					incidents.Add(incident);
				}

				return incidents;
			}
		}
	}
}