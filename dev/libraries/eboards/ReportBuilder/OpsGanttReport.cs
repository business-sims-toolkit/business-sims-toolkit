using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Globalization;
using System.Text;
using CoreUtils;
//
using GameManagement;
using LibCore;
using Network;
using Logging;

namespace ReportBuilder
{
	/// <summary>
	/// Summary description for OpsGanttReport.
	/// </summary>
	public class OpsGanttReport
	{
		protected class CompareLabelsByShortName : IComparer
		{
			protected Hashtable longNamesToShortDescs;
			protected Dictionary<string, int> serviceNameToPriority;

			public CompareLabelsByShortName (Hashtable longNamesToShortDescs, NetworkProgressionGameFile gameFile)
			{
				this.longNamesToShortDescs = longNamesToShortDescs;

				serviceNameToPriority = new Dictionary<string, int> ();
				for (int round = 1; round <= gameFile.LastRoundPlayed; round++)
				{
					NodeTree model = gameFile.GetNetworkModel(round, GameFile.GamePhase.OPERATIONS);

					foreach (Node service in model.GetNodesWithAttributeValue("type", "biz_service"))
					{
						int priority = service.GetIntAttribute("gantt_order", 0);
						if (priority == 0)
						{
							priority = service.GetIntAttribute("priority", 0);
						}

						serviceNameToPriority[service.GetAttribute("name")] = priority;

						foreach (Node node in service.getChildren())
						{
							LinkNode linkNode = node as LinkNode;
							if ((linkNode != null) && (linkNode.To.GetAttribute("type") == "biz_service_user"))
							{
								serviceNameToPriority[linkNode.To.GetAttribute("name")] = priority;
							}
						}
					}
				}
			}

			public int Compare (object x, object y)
			{
				string nameX = (string) x;
				string nameY = (string) y;

				int priorityX = 0;
				int priorityY = 0;

				if (serviceNameToPriority.ContainsKey(nameX))
				{
					priorityX = serviceNameToPriority[nameX];
				}
				else
				{
					if (nameX.ToLower().Equals("security"))
					{
						priorityX = 8;
					}
				}

				if (serviceNameToPriority.ContainsKey(nameY))
				{
					priorityY = serviceNameToPriority[nameY];
				}
				else
				{
					if (nameY.ToLower().Equals("security"))
					{
						priorityY = 8;
					}
				}

				int comparison = 0;
				if ((priorityX != 0) && (priorityY != 0))
				{
					comparison = priorityX - priorityY;
				}
				else if ((priorityX != 0) && (priorityY == 0))
				{
					comparison = -1;
				}
				else if ((priorityX == 0) && (priorityY != 0))
				{
					comparison = 1;
				}

				if (comparison == 0)
				{
					comparison = String.Compare((string) longNamesToShortDescs[nameX], (string) longNamesToShortDescs[nameY]);
				}

				return comparison;
			}
		}

		/// <summary>
		/// Store a Hashtable of Business Services to ArrayLists of status over time.
		/// </summary>
		protected double lastKnownTimeInGame = 0;
		protected int roundMins = 25;
		protected Hashtable mappings = new Hashtable();

		protected Hashtable BizServiceStatusStreams = new Hashtable();

		protected Hashtable LongNamesToShortDescs = new Hashtable ();

		protected Hashtable serviceToUserCount = new Hashtable();

		protected ArrayList BizServices = new ArrayList();

		protected bool aggregateCarEvents = false;

		protected bool showOnlyDownedServices;

		protected NodeTree model;

		protected Hashtable bizToTracker = new Hashtable();
		protected Hashtable serverNameToTracker = new Hashtable ();
		protected Hashtable appNameToTracker = new Hashtable ();

		protected ArrayList LostRevenues = new ArrayList();
		protected List<Dictionary<int, int>> LostRevenuePerStore = new List<Dictionary<int,int>>();

		protected int yaxis_width = -1;

		protected string serviceStartsWith;
		protected bool store_level;
		protected bool RevenueHoverRequired;
		protected bool HideWarningsIfAWTOFF = true;
		protected int prev_rev;
		protected XmlNode revenues;
		public abstract class KeyItem
		{
			public string Legend;
			public System.Drawing.Color? BorderColour;

			public KeyItem (string legend)
			{
				Legend = legend;
				BorderColour = null;
			}

			protected KeyItem (XmlElement element)
			{
				Legend = LibCore.BasicXmlDocument.GetStringAttribute(element, "legend", "");

				string borderColourString = LibCore.BasicXmlDocument.GetStringAttribute(element, "border_colour", "");
				if (borderColourString != "")
				{
					BorderColour = LibCore.CONVERT.ParseComponentColor(borderColourString);
				}
				else
				{
					BorderColour = null;
				}
			}

			public abstract void AddToXml (LibCore.BasicXmlDocument xml, XmlElement parent);

			public static KeyItem FromXml (XmlElement element)
			{
				switch (element.Name)
				{
					case "coloured_key_item":
						return new SolidColourKeyItem (element);

					case "patterned_key_item":
						return new PatternedKeyItem (element);
				}

				return null;
			}
		}

		public class SolidColourKeyItem : KeyItem
		{
			public System.Drawing.Color Colour;

			public SolidColourKeyItem (string legend, System.Drawing.Color colour)
				: base (legend)
			{
				Colour = colour;
			}

			public SolidColourKeyItem (string legend, string colour)
				: this (legend, LibCore.CONVERT.ParseComponentColor(colour))
			{
			}

			public SolidColourKeyItem (XmlElement element)
				: base (element)
			{
				Colour = LibCore.CONVERT.ParseComponentColor(BasicXmlDocument.GetStringAttribute(element, "colour", ""));
			}

			public override void AddToXml (LibCore.BasicXmlDocument xml, XmlElement parent)
			{
				XmlNode us = xml.AppendNewChild(parent, "coloured_key_item");
				LibCore.BasicXmlDocument.AppendAttribute(us, "legend", Legend);
				LibCore.BasicXmlDocument.AppendAttribute(us, "colour", LibCore.CONVERT.ToComponentStr(Colour));

				if (BorderColour.HasValue && (BorderColour.Value != System.Drawing.Color.Transparent))
				{
					LibCore.BasicXmlDocument.AppendAttribute(us, "border_colour", LibCore.CONVERT.ToComponentStr(BorderColour.Value));
				}
			}
		}

		public class PatternedKeyItem : KeyItem
		{
			public string Pattern;

			public PatternedKeyItem (string legend, string pattern)
				: base (legend)
			{
				Pattern = pattern;
			}

			public PatternedKeyItem (XmlElement element)
				: base (element)
			{
				Pattern = BasicXmlDocument.GetStringAttribute(element, "pattern");
			}

			public override void AddToXml (LibCore.BasicXmlDocument xml, XmlElement parent)
			{
				XmlNode us = xml.AppendNewChild(parent, "patterned_key_item");
				LibCore.BasicXmlDocument.AppendAttribute(us, "legend", Legend);
				LibCore.BasicXmlDocument.AppendAttribute(us, "pattern", Pattern);

				if (BorderColour.HasValue && (BorderColour.Value != System.Drawing.Color.Transparent))
				{
					LibCore.BasicXmlDocument.AppendAttribute(us, "border_colour", LibCore.CONVERT.ToComponentStr(BorderColour.Value));
				}
			}
		}

		protected Dictionary<string, KeyItem> legendToKeyItem;

		protected void AddKeyItem (string legend, KeyItem keyItem)
		{
			if (! legendToKeyItem.ContainsKey(legend))
			{
				legendToKeyItem.Add(legend, keyItem);
			}
		}

		protected virtual void AddBusinessService(string service, string desc)
		{
			BizServices.Add(service);

			// If we are a version 2 or later game file then just add the stream tracker here...
			if(gameFile.Version > 1)
			{
				if(!this.BizServiceStatusStreams.ContainsKey( service ) )
				{
					this.BizServiceStatusStreams.Add(service, new EventStream() );
				}
			}
		}

		public OpsGanttReport(bool showOnlyDownedServices = false)
		{
			this.showOnlyDownedServices = showOnlyDownedServices;
			legendToKeyItem = new Dictionary<string, KeyItem> ();
		}

		public void SetBarAttributes (XmlElement bar, string name, string defaultColour, string defaultBorder, string defaultPattern)
		{
			SetBarAttributes(bar, name, defaultColour, defaultBorder, defaultPattern, "", false);
		}

		public void SetBarAttributes (XmlElement bar, string name, string defaultColour, string defaultBorder, string defaultPattern, string legend)
		{
			SetBarAttributes(bar, name, defaultColour, defaultBorder, defaultPattern, legend, false);
		}

		public void SetBarAttributes (XmlElement bar, string name, string defaultColour, string defaultBorder, string defaultPattern, string legend, bool bordered)
		{
			string colour = CoreUtils.SkinningDefs.TheInstance.GetData("gantt_" + name + "_colour");
			string border = CoreUtils.SkinningDefs.TheInstance.GetData("gantt_" + name + "_border");
			string pattern = CoreUtils.SkinningDefs.TheInstance.GetData("gantt_" + name + "_pattern");

			KeyItem keyItem = null;

			if (colour != "")
			{
				bar.SetAttribute("colour", colour);
				keyItem = new SolidColourKeyItem (legend, colour);
			}
			else if (pattern != "")
			{
				bar.SetAttribute("fill", pattern);
				keyItem = new PatternedKeyItem (legend, pattern);
			}
			else if (defaultColour != "")
			{
				bar.SetAttribute("colour", defaultColour);
				keyItem = new SolidColourKeyItem (legend, defaultColour);
			}
			else if (defaultPattern != "")
			{
				bar.SetAttribute("fill", defaultPattern);
				keyItem = new PatternedKeyItem (legend, defaultPattern);
			}

			if (bordered)
			{
				bar.SetAttribute("bordercolour", "0,0,0");
				keyItem.BorderColour = System.Drawing.Color.Black;
			}
			else if (border != "")
			{
				bar.SetAttribute("bordercolour", border);
				keyItem.BorderColour = LibCore.CONVERT.ParseComponentColor(border);
			}
			else if (defaultBorder != "")
			{
				bar.SetAttribute("bordercolour", defaultBorder);
				keyItem.BorderColour = LibCore.CONVERT.ParseComponentColor(defaultBorder);
			}

			AddKeyItem(legend, keyItem);
		}

		protected virtual void OutputServiceData(BasicXmlDocument xdoc, XmlNode root, string service)
		{
			string Denial_oF_Service_Key_Title = CoreUtils.SkinningDefs.TheInstance.GetData("gantt_dos_title","Denial Of Service");


			lastKnownTimeInGame = 0;
			if (roundScores != null) lastKnownTimeInGame = roundScores.FinalTime;

			if(BizServiceStatusStreams.ContainsKey(service))
			{
				XmlNode row = (XmlNode) xdoc.CreateElement("row");
				((XmlElement)row).SetAttribute( "colour","255,0,0" );
				((XmlElement)row).SetAttribute("data", LibCore.Strings.RemoveHiddenText(service));
				//
				EventStream eventStream = (EventStream) BizServiceStatusStreams[service];
				//
				foreach(ServiceEvent se in eventStream.events)
				{
					// Warnings can overlap with the subsequent failure, so truncate them.
					if (se.seType == ServiceEvent.eventType.WARNING)
					{
						if (eventStream.events.IndexOf(se) < (eventStream.events.Count - 1))
						{
							ServiceEvent nextEvent = (ServiceEvent) eventStream.events[1 + eventStream.events.IndexOf(se)];
							se.secondsIntoGameEnds = Math.Min(se.secondsIntoGameEnds, nextEvent.secondsIntoGameOccured);
						}
					}

					if (se.seType == ServiceEvent.eventType.WARNING)
					{
						if ((this.HideWarningsIfAWTOFF) & (AWT_Active == false))
						{
							continue;
						}
					}

					if (se.secondsIntoGameOccured == se.secondsIntoGameEnds)
					{
						continue;
					}

					int isecs = (int) (se.secondsIntoGameOccured);

					XmlNode bar = (XmlNode) xdoc.CreateElement("bar");
					((XmlElement)bar).SetAttribute( "x",CONVERT.ToStr(isecs));

					//int ilength = roundMins*60-isecs;
					int ilength = (int)(lastKnownTimeInGame - isecs);
					int reportedLength = ilength;
					if(se.secondsIntoGameEnds != 0)
					{
						int isecsEnd = (int) (se.secondsIntoGameEnds);
						ilength = isecsEnd - isecs;
					}
					//
					reportedLength = (int) se.GetLength(lastKnownTimeInGame);
					//
					if(0 == ilength) ilength = 1;
					if(0 == reportedLength) reportedLength = 1;
					((XmlElement)bar).SetAttribute( "length",CONVERT.ToStr(ilength) );
					//
					if (se.seType == ServiceEvent.eventType.INCIDENT)
					{
						SetBarAttributes((XmlElement) bar, "incident", "255,0,0", "", "", "Incident");
					}
					else if (se.seType == ServiceEvent.eventType.REQUEST)
					{
						SetBarAttributes((XmlElement) bar, "request", "255,255,0", "", "", "Request");
					}
                    else if (se.seType == ServiceEvent.eventType.INFORMATION_REQUEST)
                    {
                        SetBarAttributes((XmlElement)bar, "info", "255,0,255", "", "", "Info");
                    }
                    else if (se.seType == ServiceEvent.eventType.DENIAL_OF_SERVICE)
					{
						SetBarAttributes((XmlElement)bar, "dos", "255,255,255", "0,0,0", "", Denial_oF_Service_Key_Title);
					}
					else if (se.seType == ServiceEvent.eventType.SECURITY_FLAW)
					{
						SetBarAttributes((XmlElement)bar, "security_flaw", "255,255,255", "0,0,0", "", "Security Flaw");
					}
					else if (se.seType == ServiceEvent.eventType.SECURITY_FLAW_SLABREACH)
					{
						SetBarAttributes((XmlElement)bar, "security_flaw", "128,128,128", "0,0,0", "", "Security Flaw (SLA Breached)");
					}
					else if (se.seType == ServiceEvent.eventType.COMPLIANCE_INCIDENT)
					{
						SetBarAttributes((XmlElement) bar, "compliance_incident", "255,255,0", "0,0,0", "", "Compliance Incident");
					}
					else if (se.seType == ServiceEvent.eventType.COMPLIANCE_INCIDENT_SLA_BREACH)
					{
						SetBarAttributes((XmlElement) bar, "compliance_incident", "128,128,0", "0,0,0", "", "Compliance Incident (SLA Breached)");
					}
					else if (se.seType == ServiceEvent.eventType.WORKAROUND)
					{
						SetBarAttributes((XmlElement) bar, "workaround", "0,102,255", "", "", "Workaround");
					}
					else if (se.seType == ServiceEvent.eventType.SLABREACH)
					{
						SetBarAttributes((XmlElement) bar, "slabreach", "", "", "orange_hatch", "Incident (SLA Breached)");
					}
					else if (se.seType == ServiceEvent.eventType.REQUEST_SLA_BREACH)
					{
						SetBarAttributes((XmlElement) bar, "request_slabreach", "", "", "yellow_hatch", "Request (SLA Breached)");
					}
                    else if (se.seType == ServiceEvent.eventType.INFORMATION_REQUEST_SLA_BREACH)
                    {
                        SetBarAttributes((XmlElement)bar, "info_slabreach", "", "", "magenta_hatch", "Info (SLA Breached)");
                    }
                    else if (se.seType == ServiceEvent.eventType.WA_SLABREACH)
					{
						SetBarAttributes((XmlElement) bar, "workaround_slabreach", "", "", "blue_hatch", "Workaround (SLA Breached)");
					}
					else if (se.seType == ServiceEvent.eventType.DOS_SLABREACH)
					{
						if (CoreUtils.SkinningDefs.TheInstance.GetIntData("gantt_dos_slabreach_has_border", 1) == 1)
						{
							SetBarAttributes((XmlElement) bar, "dos_slabreach", "", "", "dos_hatch", "Denial Of Service (SLA Breached)", true);
							((XmlElement) bar).SetAttribute("bordercolour", "0,0,0");
						}
						else
						{
							SetBarAttributes((XmlElement) bar, "dos_slabreach", "", "", "dos_hatch", "Denial Of Service (SLA Breached)");
						}
					}
					else if (se.seType == ServiceEvent.eventType.NO_POWER)
					{
						SetBarAttributes((XmlElement) bar, "nopower", "", "", "nopower", "No Power");
					}
					else if (se.seType == ServiceEvent.eventType.NO_POWER_SLA_BREACH)
					{
						SetBarAttributes((XmlElement) bar, "nopower_slabreach", "", "", "no_power_sla", "No Power (SLA Breached)");
					}
					else if (se.seType == ServiceEvent.eventType.INSTALL || se.seType == ServiceEvent.eventType.UPGRADE)
					{
						SetBarAttributes((XmlElement) bar, "install_upgrade", "221,160,221", "", "", "Upgrade");
					}
					else if (se.seType == ServiceEvent.eventType.MIRROR)
					{
						SetBarAttributes((XmlElement) bar, "mirror", "255,204,0", "", "", "Mirror");
					}
					else if (se.seType == ServiceEvent.eventType.WARNING)
					{
						SetBarAttributes((XmlElement) bar, "warning", "255,200,200", "", "", "Warning");
					}

					// 23-05-2007 - We set the bar's description to be "1,2,3 of 4 : time down".
					string desc = "";

					if(se.users_affected != "")
					{
						desc = se.users_affected;

						if(serviceToUserCount.ContainsKey(service))
						{
							int numUsers = (int) serviceToUserCount[service];
							desc += " of " + CONVERT.ToStr(numUsers);
						}

						desc += ": ";
					}

					int mins = reportedLength / 60;
					int secs = reportedLength % 60;
					if(mins < 1)
					{
						desc += "Incident lasts " + CONVERT.ToStr(secs) + " seconds.";
					}
					else if(mins == 1)
					{
						desc += "Incident lasts 1 Minute " + CONVERT.ToStr(secs) + " seconds.";
					}
					else
					{
						desc += "Incident lasts " + CONVERT.ToStr(mins) + " Minutes " + CONVERT.ToStr(secs) + " seconds.";
					}
					((XmlElement)bar).SetAttribute("description", desc);
					//
					row.AppendChild(bar);
				}
				//
				root.AppendChild(row);
			}
		}

		protected RoundScores roundScores;
		protected int currentRound;
		protected NetworkProgressionGameFile gameFile;
		protected bool AWT_Active = false;

		protected int? round;

		public virtual string BuildReport(NetworkProgressionGameFile _gameFile, int round, string _serviceStartsWith,
			Boolean _RevenueHoverRequired,	RoundScores rs)
		{
			this.round = round;

			if (!string.IsNullOrEmpty(CoreUtils.SkinningDefs.TheInstance.GetData("GanttBiz")) && !_serviceStartsWith.StartsWith("All"))
			{
				char[] serviceNumber = _serviceStartsWith.ToCharArray(_serviceStartsWith.Length - 1, 1);
				string bizName = CoreUtils.SkinningDefs.TheInstance.GetData("biz");
				serviceStartsWith = bizName + " " + serviceNumber[0];
			}
			else
			{
				serviceStartsWith = _serviceStartsWith;
			}
			RevenueHoverRequired = _RevenueHoverRequired;
			//
			store_level = true;
			gameFile = _gameFile;
			currentRound = round;
			roundScores = rs;
			//lastKnownTimeInGame = 0;
			// Pull the network file so that we can get the biz services.
			model = gameFile.NetworkModel;

			//
			// Sales game is special and we should get the round 5 model!
			//
			if(gameFile.IsSalesGame)
			{
				string fname = gameFile.GetRoundFile(5, "network.xml", GameFile.GamePhase.OPERATIONS);
				if(File.Exists(fname))
				{
					System.IO.StreamReader file = new System.IO.StreamReader(fname);
					string xmldata = file.ReadToEnd();
					file.Close();
					file = null;

					model = new NodeTree(xmldata);
				}
			}
			else // Pull the actual Network for the round we are displaying if it's not the current one...
			{
				if(gameFile.CurrentRound != round)
				{
					string fname = gameFile.GetRoundFile(round, "network.xml", GameFile.GamePhase.OPERATIONS);
					if(File.Exists(fname))
					{
						System.IO.StreamReader file = new System.IO.StreamReader(fname);
						string xmldata = file.ReadToEnd();
						file.Close();
						file = null;

						model = new NodeTree(xmldata);
					}
				}
			}

			// Get the long-to-short name mappings for all business services and BSUs.
			foreach (Node service in model.GetNodesWithAttributeValue("type", "biz_service"))
			{
				string name = service.GetAttribute("name");
				string shortname = service.GetAttribute("desc");

				if (SkinningDefs.TheInstance.GetBoolData("gantt_use_short_desc", false))
				{
					shortname = service.GetAttribute("shortdesc");
				}

				LongNamesToShortDescs[name] = shortname.Replace("\r\n", " ");
			}
			foreach (Node service in model.GetNodesWithAttributeValue("type", "biz_service_user"))
			{
				string name = service.GetAttribute("name");
				string shortname = service.GetAttribute("desc");

				if (SkinningDefs.TheInstance.GetBoolData("gantt_use_short_desc", false))
				{
					shortname = service.GetAttribute("shortdesc");
				}

				LongNamesToShortDescs[name] = shortname.Replace("\r\n", " ");

				// : fix for 3601: make sure that, eg Store 4 Security adds a mapping for Security
				// as well as Store 4 Security.
				if (name.StartsWith(_serviceStartsWith))
				{
					name = name.Substring(_serviceStartsWith.Length + 1);
					LongNamesToShortDescs[name] = shortname.Replace("\r\n", " ");
				}
			}

			ArrayList servicesToRemove = new ArrayList ();

			ArrayList a_biz_services;
			//
			string model_uuid = model.uuid;
			//
			ArrayList types = new ArrayList();
			string _type = "biz_service";
			//
			if (serviceStartsWith == CoreUtils.SkinningDefs.TheInstance.GetData("allbiz"))
			{
				types.Add("biz_service");
				a_biz_services = model.GetNodesWithAttributeValue("type",_type);

				// : Remove any that are uninstalled.
				if (model.GetNamedNode("uninstalled") != null)
				{
					foreach (Node n in a_biz_services)
					{
						foreach (Node m in model.GetNamedNode("uninstalled"))
						{
							if ((m.GetAttribute("type") == "biz_service") && (m.GetAttribute("name") == n.GetAttribute("name")))
							{
								servicesToRemove.Add(n);
								break;
							}
						}
					}
				}
			}
			else
			{
				types.Add("biz_service_user");
				store_level = false;
				_type = "biz_service_user";
				a_biz_services = model.GetNodesWithAttributeValue("type",_type);

				// : Fix for 4064 (reports show up for unused businesses in single-store mode).
				// Remove any business services that are unconnected.
				ArrayList connections = model.GetNodesWithAttributeValue("type", "Connection");

				foreach (Node serviceUser in a_biz_services)
				{
					bool connected = false;

					foreach (LinkNode connection in connections)
					{
						if (connection.To == serviceUser)
						{
							connected = true;
							break;
						}
					}

					if (! connected)
					{
						servicesToRemove.Add(serviceUser);
					}

					// Find the service we use...
					if (model.GetNamedNode("uninstalled") != null)
					{
						foreach (LinkNode connection in serviceUser.BackLinks)
						{
							if (connection.From.GetAttribute("type") == "biz_service")
							{
								// Is it uninstalled?  Remove if so.
								foreach (Node m in model.GetNamedNode("uninstalled"))
								{
									if (m == connection.From)
									{
										servicesToRemove.Add(serviceUser);
										break;
									}
								}
							}
						}
					}
				}
			}

			// 22-08-2007 : Don't watch businesses that were retired at the end
			// of the last round.
			NodeTree pNetworkModel = null;

			if(round >= 2)
			{
				string xmlFileName = gameFile.GetNetworkFile(round, GameFile.GamePhase.TRANSITION);
				System.IO.StreamReader file = new System.IO.StreamReader(xmlFileName);
				string xmldata = file.ReadToEnd();
				file.Close();
				file = null;
				pNetworkModel = new NodeTree(xmldata);

				////get whether we have AWT switched ON or OFF
				//Node AdvancedWarningTechnology = pNetworkModel.GetNamedNode("AdvancedWarningTechnology");
				//if (AdvancedWarningTechnology != null)
				//{
				//  AWT_Active = AdvancedWarningTechnology.GetBooleanAttribute("enabled", false);
				//}
			}

			// Ditch any unwanted services.
			foreach (Node n in servicesToRemove)
			{
				a_biz_services.Remove(n);
			}

			//
			// Get all the biz_services that we are interested in...
			//
			foreach(Node service in a_biz_services)
			{
				string name = service.GetAttribute("name");
				string func = service.GetAttribute("biz_service_function");
				string shortname = service.GetAttribute("desc");
				if (SkinningDefs.TheInstance.GetBoolData("gantt_use_short_desc", false))
				{
					shortname = service.GetAttribute("shortdesc");
				}

				if(gameFile.Version == 1)
				{
					// 22-08-2007 : Don't watch businesses that were retired at the end
					// of the last round.
					if(null != pNetworkModel)
					{
						Node old_biz = pNetworkModel.GetNamedNode(name);
						if(null != old_biz)
						{
							if(old_biz.GetBooleanAttribute("retired",false))
							{
								// Don't watch this biz as it's no longer used.
								// (Players can't un-retire a business).
								continue;
							}
						}
					}
				}
				//
				string dd = CoreUtils.SkinningDefs.TheInstance.GetData("biz");

				if(!name.StartsWith( CoreUtils.SkinningDefs.TheInstance.GetData("biz") ))
				{
					// This is a top level biz_service so check how many users it has.
					// Can't do this any more as there is no longer a one to one correlation between
					// biz users and node children.

					//serviceToUserCount.Add(name,service.getChildren().Count);

					int count = 0;
/*
					ArrayList children = service.getChildren();

					if(name == "Broker ITS")
					{
						int num = children.Count;

						int x = 0;
					}*/

					foreach(Node user in service)
					{
						string type = user.GetAttribute("type");
						if(type == "Connection")
						{
							++count;
						}
					}

					serviceToUserCount.Add(name, count);
				}

				if(gameFile.Version == 1)
				{
					if (func != string.Empty)
					{
						if ( ! mappings.ContainsKey(name))
						{
							mappings.Add(name, func);
						}
					}
					else
					{
						if(name.StartsWith(serviceStartsWith))
						{
							if ( ! mappings.ContainsKey(name))
							{
								mappings.Add(name, name);
							}
						}
					}
				}

				if(name.StartsWith(serviceStartsWith))
				{
					// This is a biz service we are interested in.
					AddBusinessService(name, shortname);
				}
				else if(serviceStartsWith == CoreUtils.SkinningDefs.TheInstance.GetData("allbiz"))
				{
					//looking at all cars so just add it
					AddBusinessService(name, shortname);
				}
			}

			if(gameFile.Version == 1)
			{
				foreach (string name in this.mappings.Values)
				{
					if (! BizServiceStatusStreams.ContainsKey(name))
					{
						this.BizServiceStatusStreams.Add(name,new EventStream());
					}
				}
			}

			roundMins = model.GetNamedNode("CurrentTime").GetIntAttribute("round_duration_secs", 1500) / 60;
			//set lost revenue array to be roundMins usually 25
			for (int i = 0; i <= roundMins; i++)
			{
				this.LostRevenues.Add("0");
			}

			// Pull the logfile to get data from.
			string logFile = gameFile.GetRoundFile(round,"NetworkIncidents.log", GameManagement.GameFile.GamePhase.OPERATIONS);

			BasicIncidentLogReader biLogReader = new BasicIncidentLogReader(logFile);
			foreach(string service in BizServices)
			{
				biLogReader.WatchApplyAttributes(service, this.biLogReader_LineFound );
				//
				LatchEdgeTracker let = new LatchEdgeTracker();
				let.TrackBooleanAttribute("up",true);
				let.TrackBooleanAttribute("denial_of_service", false);
				let.TrackBooleanAttribute("security_flaw", false);
				let.TrackBooleanAttribute("compliance_incident", false);
                let.TrackBooleanAttribute("awaiting_saas_auto_restore", false);
                let.TrackStringAttribute("error_code");
                //
				let.TrackBooleanAttribute("retired",false);
				//
				let.TrackBooleanAttribute("slabreach",false);
				let.TrackBooleanAttribute("upByMirror",false);
				let.TrackStringAttribute("incident_id");
				let.TrackStringAttribute("users_down");
				let.TrackCounterAttribute("rebootingForSecs");
				let.TrackCounterAttribute("workingAround");
				bizToTracker.Add(service, let);
			}

			foreach (Node serverNode in model.GetNodesWithAttributeValue("type", "Server"))
			{
				string name = serverNode.GetAttribute("name");
				biLogReader.WatchApplyAttributes(name, this.biLogReader_ServerLineFound);

				LatchEdgeTracker let = new LatchEdgeTracker ();
				let.TrackStringAttribute("danger_level");
				serverNameToTracker.Add(name, let);
			}

			ArrayList apps = new ArrayList ();
			apps.AddRange(model.GetNodesWithAttributeValue("type", "App"));
			apps.AddRange(model.GetNodesWithAttributeValue("type", "Database"));
			foreach (Node appNode in apps)
			{
				string name = appNode.GetAttribute("name");
				biLogReader.WatchApplyAttributes(name, this.biLogReader_AppLineFound);

				if (appNameToTracker.ContainsKey(name) == false)
				{
					LatchEdgeTracker let = new LatchEdgeTracker();
					let.TrackStringAttribute("danger_level");
					appNameToTracker.Add(name, let);
				}
			}

			//watch for costed events
			biLogReader.WatchCreatedNodes("CostedEvents", this.biLogReader_CostedEventFound );
			biLogReader.WatchApplyAttributes("Revenue", this.biLogReader_RevenueFound);
			biLogReader.WatchApplyAttributes("ApplicationsProcessed", this.biLogReader_ApplicationsProcessedFound);
			WatchAdditionalItems(biLogReader);

			biLogReader.WatchApplyAttributes("AdvancedWarningTechnology", this.biLogReader_AWT_Changed);

			biLogReader.Run();

			// Before we continue any further we have to Merge Trackers that are upgraded services that should
			// be reported as one track.

			//if roundscores null, then we know this round not been run, we just want an empty report
			if (roundScores != null)
			{
				if(gameFile.Version == 1)
				{
					MergeTrackers();
				}

				ConvertTrackersToStreams();
			}

			return WriteReport();
		}


		protected virtual string WriteReport()
		{
			//
			// Output info about each business service's availability...
			//
			// TODO : Should this really be stored in the last round played dir rather than the
			// round dir for the report required?!?!
			//
			string reportFile = gameFile.GetRoundFile(gameFile.LastRoundPlayed/*.CurrentRound*/, "OpsGanttReport_" + serviceStartsWith + ".xml" , gameFile.LastPhasePlayed);//.CurrentPhase);
			LibCore.BasicXmlDocument xdoc = LibCore.BasicXmlDocument.Create();
			var root = xdoc.CreateElement("opsgantt");
			xdoc.AppendChild(root);

            root.AppendAttribute("stripe_alternate_rows", SkinningDefs.TheInstance.GetBoolData("gantt_shade_alternate_rows", false));

            GenerateGanttReport(xdoc, root);
			//
			xdoc.SaveToURL("",reportFile);
			return reportFile;
		}

		protected virtual void OrderServiceNames (ArrayList names)
		{
			// : Sort the names by their short versions (as shown in their labels), so the labels
			// end up displayed in a sensible-looking order (bug 4734).
			names.Sort(new CompareLabelsByShortName (LongNamesToShortDescs, gameFile));
		}

		protected virtual void GenerateGanttReport(LibCore.BasicXmlDocument xdoc, XmlNode root)
		{
			//
			XmlNode xtitle = (XmlNode) xdoc.CreateElement("xAxis");
			//
			string minMaxSteps_str = "0," + CONVERT.ToStr(roundMins) + ",5";

			((XmlElement)xtitle).SetAttribute( "title","Time" );
			((XmlElement)xtitle).SetAttribute( "minMaxSteps",minMaxSteps_str);
			((XmlElement)xtitle).SetAttribute( "autoScale","false" );
			//
			root.AppendChild(xtitle);
			//
			XmlNode yaxis = (XmlNode) xdoc.CreateElement("yAxis");
			//
			if(-1 != yaxis_width)
			{
				((XmlElement)yaxis).SetAttribute( "width", CONVERT.ToStr(yaxis_width) );
			}
			//
			((XmlElement)yaxis).SetAttribute( "title","Service" );

			int max = 0;
			foreach (string serviceName in BizServiceStatusStreams.Keys)
			{
				if (! (showOnlyDownedServices && (((EventStream) BizServiceStatusStreams[serviceName]).events.Count == 0)))
				{
					max++;
				}
			}

			((XmlElement)yaxis).SetAttribute( "minMaxSteps","0," + max + ",1");
			((XmlElement)yaxis).SetAttribute( "align","centre_properly" );
			//
			string labels = "";
			List<string> colours = new List<string>();
			bool addComma = false;

			//order names alphabetically
			ArrayList names = new ArrayList(BizServiceStatusStreams.Keys);

			// Map all names to labels that do not have store/car x at the begining...
			Hashtable nameToLabel = new Hashtable();
			ArrayList newNames = new ArrayList();

			foreach(string str in names)
			{
				string s;

				if(str.StartsWith(serviceStartsWith))
				{
					s = str.Substring(serviceStartsWith.Length).Trim();
				}
				else
				{
					s = str;
				}

				nameToLabel[s] = str;
				newNames.Add(s);
			}

			OrderServiceNames(newNames);
			string use_striped = CoreUtils.SkinningDefs.TheInstance.GetData("gantt_use_striped_colours", "false");

			// Old version 1 game files... (Gannt chart is much more complex and error prone...
			foreach(string str2 in newNames)
			{
				string str = (string) nameToLabel[str2];

				if (! (showOnlyDownedServices && (((EventStream) BizServiceStatusStreams[str]).events.Count == 0)))
				{
					if (bizToTracker.ContainsKey(str))
					{
						string avail = "100";

						if (roundScores != null)
						{
							avail = GetAvailability(str);
						}
						//string avail = GetAvailability((EventStream) BizServiceStatusStreams[str] );
						//get the availability and add here
						if (addComma) labels += ",";

						// : Fix for 3762 (names in Gantt were coming up emtpy except for all stores).
						labels += LibCore.Strings.RemoveHiddenText((string) LongNamesToShortDescs[str]) + " " + avail + "%";

						addComma = true;
					}
					else
					{
						// This must be a mapped service.
						// Find a valid service that maps to this...

						string mappedService = GetActiveTrackerForMappedService(str);
						if ("" != mappedService)
						{
							string avail = "100";
							if (roundScores != null)
							{
								avail = GetAvailability(mappedService);
							}
							if (addComma) labels += ",";

							labels += LibCore.Strings.RemoveHiddenText((string) LongNamesToShortDescs[str]) + " " + avail + "%";

							addComma = true;
						}
						else
						{
							if (addComma) labels += ",";
							labels += str2 + " " + "0" + "%";
							addComma = true;
						}
					}
					if (use_striped == "true")
					{
						ArrayList services = model.GetNamedNode("Business Services Group").GetChildrenOfType("biz_service");
						String func = ""; ;
						foreach (Node n in services)
						{
							if (n.GetAttribute("name") == str)
							{
								func = n.GetAttribute("function");
							}
						}

						switch (func)
						{
							case "IT":
								colours.Add("62, 143, 205");
								break;
							case "HR":
								colours.Add("211, 17, 29");
								break;
							case "FM":
								colours.Add("126, 184, 40");
								break;
							case "Other":
								colours.Add("248, 175, 20");
								break;
							default:
								colours.Add("255, 255, 255");
								break;
						}
					}
				}
			}
			//
			((XmlElement)yaxis).SetAttribute("labels",labels );

			if (use_striped == "true")
			{
				colours.Reverse();
				StringBuilder builder = new StringBuilder();

				foreach (string c in colours)
				{
					builder.Append(c).Append(", ");
				}
				((XmlElement) yaxis).SetAttribute("colours", builder.ToString());
			}
			//
			root.AppendChild(yaxis);
			//
			XmlNode rows = (XmlNode) xdoc.CreateElement("rows");
			int width = 60 * roundMins;
			((XmlElement)rows).SetAttribute( "length",CONVERT.ToStr(width) );
			root.AppendChild(rows);
			//
			foreach(string str2 in newNames)
			{
				string service = (string) nameToLabel[str2];

				if (! (showOnlyDownedServices && (((EventStream) BizServiceStatusStreams[service]).events.Count == 0)))
				{
					OutputServiceData(xdoc, rows, service);
				}
			}

			if ((RevenueHoverRequired))
			{
				if (store_level)
				{
					//save the lost revenues for each minute
					revenues = (XmlNode)xdoc.CreateElement("revenue_lost");

					//
					prev_rev = 0;
					for (int i = 0; i < this.LostRevenues.Count; i++)
					{
						string tmp = (string)this.LostRevenues[i];

						if (tmp == string.Empty) tmp = "0";
						int rev = CONVERT.ParseInt(tmp);

						int lost_rev;
						if (rev == 0)
						{
							lost_rev = 0;
						}
						else
						{
							lost_rev = rev - prev_rev;
							prev_rev = rev;
						}

						AddToNode(i, lost_rev, xdoc);

					}

					//
					root.AppendChild(revenues);
				}
				else if (LostRevenuePerStore.Count != 0)
				{

					int store = CONVERT.ParseInt(serviceStartsWith.Remove(0, serviceStartsWith.Length - 2));
					revenues = (XmlNode)xdoc.CreateElement("revenue_lost");

					int[] LocalLostRevenues = new int[roundMins + 1];

					foreach (KeyValuePair<int, int> avp in LostRevenuePerStore[store - 1])
					{

						int minute = (int)Math.Ceiling((double)(avp.Key / 60));
						LocalLostRevenues[minute] = avp.Value;
					}

					for (int i = 0; i < LocalLostRevenues.Length; i++)
					{

						AddToNode(i, LocalLostRevenues[i], xdoc);

					}

					root.AppendChild(revenues);

				}
			}

			XmlElement key = xdoc.AppendNewChild(root, "key");
			foreach (KeyItem keyItem in legendToKeyItem.Values)
			{
				keyItem.AddToXml(xdoc, key);
			}
		}

		protected void AddToNode(int i, int lost_rev, LibCore.BasicXmlDocument xdoc)
		{
			XmlNode row = (XmlNode)xdoc.CreateElement("revenue");
			((XmlElement)row).SetAttribute("minute", CONVERT.ToStr(i));
			string val = FormatWithCommas(lost_rev);

			((XmlElement)row).SetAttribute("lost", "$" + val);
			revenues.AppendChild(row);

		}


		protected string FormatWithCommas(int tmpAmount)
		{
			string moneystr = string.Empty;
			int Amount = tmpAmount;
			CultureInfo ukCulture = new CultureInfo("en-GB");

			if (Amount>1000)
			{
				moneystr = Amount.ToString("0,0",ukCulture);
			}
			else
			{
				moneystr = Amount.ToString();
			}

			return moneystr;
		}

		protected string GetActiveTrackerForMappedService(string service)
		{
			foreach(string mappedService in mappings.Keys)
			{
				string mappedTo = (string) mappings[mappedService];
				if(mappedTo == service)
				{
					if(this.bizToTracker.ContainsKey(mappedService))
					{
						return mappedService;
					}
				}
			}

			return "";
		}

		protected void MergeTrackers()
		{
			// : This code should only run on old-format files.
			if (gameFile.Version > 1)
			{
				return;
			}

			// Hashtable to ArrayList of mergable streams...
			Hashtable mergesToDo = new Hashtable();

			foreach(string str in this.mappings.Values)
			{
				ArrayList trackersToMerge = new ArrayList();

				foreach(string str2 in mappings.Keys)
				{
					if( ((string)mappings[str2]) == str )
					{
						trackersToMerge.Add(str2);
					}
				}

				if(trackersToMerge.Count > 1)
				{
					// Trackers need to be merged.
					mergesToDo[str] = trackersToMerge;
				}
			}
			//
			// Do we have any redundant streams?
			// A redundant stream is one that has another mergable non-retired stream but it has no non-zero time events on it.
			//
			//
			foreach(string str in mergesToDo.Keys)
			{
				ArrayList trackersToMerge = (ArrayList) mergesToDo[str];

				if(trackersToMerge.Count >= 2)
				{
					ArrayList nonRetired = new ArrayList();

					// Count non-retired streams on this merge...
					foreach(string str_let in trackersToMerge)
					{
						LatchEdgeTracker let = (LatchEdgeTracker) bizToTracker[ str_let ];

						if(let != null)
						{
							if( false == let.GetLatchedEvent( let.Count-1 ).GetBoolEventActive("retired") )
							{
								nonRetired.Add(str_let);
							}
						}
					}

					if(nonRetired.Count > 1)
					{
						ArrayList RemoveStreams = new ArrayList();

						for(int i=0; i<nonRetired.Count; i++)
						{
							string str_let = (string) nonRetired[i];
							LatchEdgeTracker let = (LatchEdgeTracker) bizToTracker[ str_let ];

							int count = 0;

							for(int ii=0; ii<let.Count; ++ii)
							{
								LatchedEvent le = let.GetLatchedEvent(ii);
								if(le.time != 0.0)
								{
									++count;
								}
							}

							if(count == 0)
							{
								// This stream has nothing on it.
								// Therefore, add it to streams to remove unless it is the last one and
								// we would be removing all the streams.
								if(i < (let.Count-1))
								{
									// Safe to remove...
									RemoveStreams.Add(str_let);
								}
								else
								{
									// This is the last stream so check if we will accidentally remove all of them!
									if(RemoveStreams.Count != (let.Count-1))
									{
										RemoveStreams.Add(str_let);
									}
								}
							}
						}

						foreach(string str_let in RemoveStreams)
						{
							trackersToMerge.Remove(str_let);
							bizToTracker.Remove(str_let);
						}
					}
				}
			}
			//
			// Merge trackers...
			//
			foreach(string str in mergesToDo.Keys)
			{
				ArrayList trackersToMerge = (ArrayList) mergesToDo[str];
				//
				// TODO : Should in theory be able to support more than one upgrade
				// in a particular round.
				//
				// Assume for now that we only have two to merge! (Dangerous I know : LP).
				//
				if(trackersToMerge.Count == 2)
				{
					string first = (string) trackersToMerge[0];
					string upgrade = (string) trackersToMerge[1];

					LatchEdgeTracker letFirst = (LatchEdgeTracker) bizToTracker[ first ];
					LatchEdgeTracker letUpgrade = (LatchEdgeTracker) bizToTracker[ upgrade ];

					//
					// If the first version isn't marked as retired then the upgrades are the other way around.
					//
					if( (letFirst.Count == 0) || (false == letFirst.GetLatchedEvent( letFirst.Count-1 ).GetBoolEventActive("retired") ) )
					{
						//
						// Hack alert! If you produce a new service in transition (e.g. through skip) then don't install
						// we'll have two versions neither of which is retired. In this case we jump through a hoop of
						// hack and pick the service that has the most things happening on it to be the first service.
						//
						// First check if the other service isn't marked as retired either.
						//
						// Actually, only swap if the second is marked as retired and has enough data...
						//
						if( (letUpgrade.Count > 0) && (true == letUpgrade.GetLatchedEvent( letUpgrade.Count-1 ).GetBoolEventActive("retired")))
						{
							first = (string) trackersToMerge[1];
							upgrade = (string) trackersToMerge[0];

							letFirst = (LatchEdgeTracker) bizToTracker[ first ];
							letUpgrade = (LatchEdgeTracker) bizToTracker[ upgrade ];
						}
					}
					//
					// The first version must now be marked as retired for us to proceed.
					//
					if(letFirst.Count > 0)
					{
						if( true == letFirst.GetLatchedEvent( letFirst.Count-1 ).GetBoolEventActive("retired") )
						{
							letFirst.StoreInFile("letFirst.latch");
							letUpgrade.StoreInFile("letUpgrade.latch");
							// Removed the retired events for the initial stream...
							ArrayList remove = new ArrayList();
							//
							for(int i=0; i<letFirst.Count; ++i)
							{
								LatchedEvent le = letFirst.GetLatchedEvent(i);
								if( true == le.GetBoolEventActive("retired") )
								{
									// remove this event...
									remove.Add(le);
								}
							}

							foreach(LatchedEvent le in remove)
							{
								letFirst.RemoveLatchedEvent(le);
							}

							for(int i=0; i<letUpgrade.Count; ++i)
							{
								LatchedEvent le = letUpgrade.GetLatchedEvent(i);
								// TODO : Do this better???
								// Don't add the first event on the upgrade stream.
								if(le.time != 0.0)
								{
									letFirst.AddLatchedEvent( (LatchedEvent) le.Clone());
								}
							}
						}
					}

					bizToTracker.Remove(upgrade);
				}
			}
		}

		protected virtual void ConvertTrackersToStreams()
		{
			// Before the incidents, do the AWT warnings for the servers.
			// Warnings need to come first because they will typically be followed,
			// and superceded, by incidents.
			if ((CoreUtils.SkinningDefs.TheInstance.GetIntData("gantt_show_warnings", 0) == 1)
				&& (currentRound >= CoreUtils.SkinningDefs.TheInstance.GetIntData("gantt_warnings_start_round", 0)))
			{
				foreach (string serverName in serverNameToTracker.Keys)
				{
					Node server = gameFile.NetworkModel.GetNamedNode(serverName);

					if (server == null)
					{
						continue;
					}

					LatchEdgeTracker let = serverNameToTracker[serverName] as LatchEdgeTracker;

					for (int i = 0; i < let.Count; i++)
					{
						LatchedEvent le = let.GetLatchedEvent(i);
						string dangerLevel = le.GetStringEvent("danger_level");
						if (dangerLevel != "")
						{
							int level = CONVERT.ParseInt(dangerLevel);

							// This server contains apps...
							foreach (Node app in server.getChildren())
							{
								DoAppWarningEvent(app, le, (level >= 33));
							}
						}
					}
				}
				// And for the apps.
				foreach (string appName in appNameToTracker.Keys)
				{
					Node app = gameFile.NetworkModel.GetNamedNode(appName);
					LatchEdgeTracker let = appNameToTracker[appName] as LatchEdgeTracker;

					for (int i = 0; i < let.Count; i++)
					{
						LatchedEvent le = let.GetLatchedEvent(i);
						string dangerLevel = le.GetStringEvent("danger_level");
						if (dangerLevel != "")
						{
							int level = CONVERT.ParseInt(dangerLevel);

							DoAppWarningEvent(app, le, (level >= 33));
						}
					}
				}
			}

			// Then do the proper incidents.
			foreach(string biz in bizToTracker.Keys)
			{
				LatchEdgeTracker let = (LatchEdgeTracker) bizToTracker[biz];
				//
				int count = let.Count;
				for(int i=0; i<count; ++i)
				{
					LatchedEvent le = let.GetLatchedEvent(i);
					// Pull the incident_id
					string incident_id = le.GetStringEvent("incident_id");

					// Always show rebooting first!
					if(le.GetCounterEventActive("rebootingForSecs"))
					{
						AddEvent(biz, le.time, false,/*reason*/"", ServiceEvent.eventType.INSTALL);
						//
						string users_down = le.GetStringEvent("users_down");
						if("" != users_down)
						{
							this.SetUsersAffected(users_down, biz);
						}
					}
					else if(le.GetCounterEventActive("workingAround"))
					{
						// New mode that shows SLA breaches in workaround...
						if(le.GetBoolEventActive("slabreach"))
						{
							AddEvent(biz, le.time, false,/*reason*/"", ServiceEvent.eventType.WA_SLABREACH);
							//
							string users_down = le.GetStringEvent("users_down");
							if("" != users_down)
							{
								this.SetUsersAffected(users_down, biz);
							}
						}
						else
						{
							AddEvent(biz, le.time, false,"", ServiceEvent.eventType.WORKAROUND);
							//
							string users_down = le.GetStringEvent("users_down");
							if("" != users_down)
							{
								this.SetUsersAffected(users_down, biz);
							}
						}
					}
					else if(le.GetBoolEventActive("slabreach"))
					{
						if(le.GetBoolEventActive("denial_of_service") == true)
						{
							AddEvent(biz, le.time, false,"", ServiceEvent.eventType.DOS_SLABREACH);
						}
						else if (le.GetBoolEventActive("security_flaw") == true)
						{
							AddEvent(biz, le.time, false, "", ServiceEvent.eventType.SECURITY_FLAW_SLABREACH);
						}
						else if (le.GetBoolEventActive("compliance_incident") == true)
						{
							AddEvent(biz, le.time, false, "", ServiceEvent.eventType.COMPLIANCE_INCIDENT_SLA_BREACH);
						}
						else
						{
                            AddEvent(biz, le.time, false, "", ServiceEvent.eventType.SLABREACH);
						}
						//
						string users_down = le.GetStringEvent("users_down");
						if("" != users_down)
						{
							this.SetUsersAffected(users_down, biz);
						}
					}
					else if(le.GetBoolEventActive("upByMirror") == true)
					{
						AddEvent(biz, le.time, false, "", ServiceEvent.eventType.MIRROR);
						//
						string users_down = le.GetStringEvent("users_down");
						if("" != users_down)
						{
							this.SetUsersAffected(users_down, biz);
						}
					}
					else if(! le.GetBoolEventActive("up"))
					{
						if(incident_id != "")
						{
							if(le.GetBoolEventActive("denial_of_service") == true)
							{
								AddEvent(biz, le.time, false,"", ServiceEvent.eventType.DENIAL_OF_SERVICE);
							}
							else if (le.GetBoolEventActive("security_flaw") == true)
							{
								AddEvent(biz, le.time, false, "", ServiceEvent.eventType.SECURITY_FLAW);
							}
							else if (le.GetBoolEventActive("compliance_incident") == true)
							{
								AddEvent(biz, le.time, false, "", ServiceEvent.eventType.COMPLIANCE_INCIDENT);
							}
							else if (le.GetBoolEventActive("awaiting_saas_auto_restore"))
							{
								AddEvent(biz, le.time, false, "",  ServiceEvent.eventType.INCIDENT_ON_SAAS);
							}
							else
							{
								AddEvent(biz, le.time, false, "", ServiceEvent.eventType.INCIDENT);
							}

							string users_down = le.GetStringEvent("users_down");
							if (! string.IsNullOrEmpty(users_down))
							{
								SetUsersAffected(users_down, biz);
							}
						}
					}
					else if (le.GetBoolEventActive("up"))
					{
						AddEvent(biz, le.time, true, "", ServiceEvent.eventType.INCIDENT);
					}
				}
			}
		}

		protected virtual void DoAppWarningEvent (Node app, LatchedEvent le, bool inWarning)
		{
			ArrayList affectedServices = new ArrayList ();

			//Issue 8575 the warning system will have a problem if there is an support tech node inbetween the app and
			//the links to the BSUs. We fixed it in AOSE R2 incident 3 by removing the unneeded support tech node
			//It was much quicker than upgrading this code to walk down through support tech nodes to get to the links
			//A job for the future when someone has more than 5 mins

			// The app contains links to various BSUs...
			foreach (Node child in app.getChildren())
			{
				LinkNode linkNode = child as LinkNode;
				if (linkNode != null)
				{
					Node bsu = linkNode.To;

					// The BSU will also be linked to by services that use it.
					foreach (LinkNode serviceLink in bsu.BackLinks)
					{
						Node service = serviceLink.From;
						if ((service.GetAttribute("type") == "biz_service") && ! affectedServices.Contains(service))
						{
							affectedServices.Add(service);
						}
					}
				}
			}

			foreach (Node service in affectedServices)
			{
				ServiceEvent.eventType eventType = ServiceEvent.eventType.OK;
				if (inWarning)
				{
					eventType = ServiceEvent.eventType.WARNING;
				}

				AddEvent(service.GetAttribute("name"), le.time, ! inWarning, "", eventType);
			}
		}

		protected string GetAvailability(string service)
		{
			double availability = 0.0;
			bool up = true;
			double lastTime = 0.0;
			double downFor = 0.0;

			bool isBSU = (serviceStartsWith != CoreUtils.SkinningDefs.TheInstance.GetData("allbiz"));

			double numUsers = 4;

			if(serviceToUserCount.ContainsKey(service))
			{
				numUsers = (int) serviceToUserCount[service];
			}

			// Prevent divide by zero just in case.
			if(numUsers == 0) numUsers = 4;

			// : Fix for 4064 (availability values differ for the same service between
			// the all and single store modes).
			if (isBSU)
			{
				numUsers = 1;
			}

			double numDown = 0;
			char[] comma = { ',' };

			if(bizToTracker.ContainsKey(service))
			{
				LatchEdgeTracker let = (LatchEdgeTracker) bizToTracker[service];

				if (let.Count == 0) return "100";

				for(int i=0; i<let.Count; ++i)
				{
					LatchedEvent le = let.GetLatchedEvent(i);
					double lengthOfLastEvent = le.time - lastTime;

					string nd_str = le.GetStringEvent("users_down");

					if(!up)
					{
						downFor += lengthOfLastEvent * numDown;
					}

					up = le.GetBoolEventActive("up");

					// : Wrongly calculated numDown as 1 when users_down was blank.
					if (isBSU)
					{
						if (up)
						{
							numDown = 0;
						}
						else
						{
							numDown = 1;
						}
					}
					else
					{
						if (nd_str.Length == 0)
						{
							numDown = 0;
						}
						else
						{
							numDown = nd_str.Split(',').Length;
						}
					}

					lastTime = le.time;
				}
			}
			else
			{
				string s = service;
			}

			double lengthOfFinalEvent = roundScores.FinalTime - lastTime;

			if(!up)
			{
				downFor += lengthOfFinalEvent * numDown;
			}

			availability = (roundScores.FinalTime*numUsers - downFor) / numUsers;

			// : Fix so it doesn't flash up NaN before playing any data are available.
			if (roundScores.FinalTime <= 0.0f)
			{
				availability = 1;
			}
			else
			{
				availability = availability / roundScores.FinalTime;
			}

			availability = (double)Math.Round( availability*100.0, 0);
			//availability = (double)Math.Round( ( (upFor / roundScores.FinalTime)*100.0) / numUsers , 0);

			return CONVERT.ToStr(availability);
		}

		protected string GetAvailability(EventStream eventStream)
		{
			double downtime = 0;

			double lastKnownTimeInGame = 0;
			if (roundScores != null) lastKnownTimeInGame = roundScores.FinalTime;

			foreach(ServiceEvent se in eventStream.events)
			{
				if (se.seType == ServiceEvent.eventType.MIRROR || se.seType == ServiceEvent.eventType.SLABREACH
					|| se.seType == ServiceEvent.eventType.WARNING || se.seType == ServiceEvent.eventType.WORKAROUND)
				{
					continue;
				}
				if (se.secondsIntoGameEnds > 0)
				{
					downtime += se.secondsIntoGameEnds - se.secondsIntoGameOccured;
				}
				else
				{
					downtime += lastKnownTimeInGame - se.secondsIntoGameOccured;
				}
			}

			double avail = downtime / 1500f;
			double tmp =  (double)Math.Round((1f - avail) * 100f, 0);

			return CONVERT.ToStr((int)tmp);
		}

		protected void SetUsersAffected(string users, string service)
		{
			if (mappings.ContainsKey(service)) service = (string)mappings[service];

			EventStream eventStream = (EventStream) BizServiceStatusStreams[service];

			if( (eventStream != null) && (eventStream.lastEvent != null) )
			{
				eventStream.lastEvent.SetUsersAffected(users);
			}
		}

		protected void ConnectEvents(ServiceEvent last, ServiceEvent newEvent)
		{
			if(null != last)
			{
				if(!last.up || (last.seType == ServiceEvent.eventType.WORKAROUND))
				{
					last.next = newEvent;
					newEvent.last = last;
				}
			}
		}

		protected virtual void AddEvent(string service, double seconds, bool up, string reason, ServiceEvent.eventType etype)
		{
			ServiceEvent se = new ServiceEvent(seconds,up,reason,etype);
			//
			if (mappings.ContainsKey(service)) service = (string)mappings[service];

			if (this.BizServiceStatusStreams.ContainsKey(service))
			{
				EventStream eventStream = (EventStream) BizServiceStatusStreams[service];

				if (eventStream == null) return;
				if(eventStream.lastEvent != null)
				{
					// If this is a workaround event and the last one was a workaround event then we ignore it!
					if(etype == ServiceEvent.eventType.WORKAROUND)
					{
						if(eventStream.lastEvent.seType == ServiceEvent.eventType.WORKAROUND)
						{
							// Don't add as it's just a continued WorkAround event...
							return;
						}
					} // WA_SLABREACH
					else if(etype == ServiceEvent.eventType.WA_SLABREACH)
					{
						if(eventStream.lastEvent.seType == ServiceEvent.eventType.WA_SLABREACH)
						{
							// Don't add as it's just a continued WorkAround event...
							return;
						}
					}
					else if(etype == ServiceEvent.eventType.DOS_SLABREACH)
					{
						if(eventStream.lastEvent.seType == ServiceEvent.eventType.DOS_SLABREACH)
						{
							// Don't add as it's just a continued DOS SLA event...
							return;
						}
					}

					// Set this event's end point...
					eventStream.lastEvent.secondsIntoGameEnds = seconds;
				}
				// Only Add "down" events...
				if(!up)
				{
					eventStream.events.Add(se);
					ConnectEvents(eventStream.lastEvent,se);
					eventStream.lastEvent = se;
				}
				else
				{
					eventStream.lastEvent = null;
				}
			}
			else
			{
				if(!up)
				{
					EventStream eventStream = new EventStream();
					eventStream.lastEvent = se;
					eventStream.events.Add(se);
					BizServiceStatusStreams[service] = eventStream;
				}
			}
		}

		protected virtual void biLogReader_CostedEventFound(object sender, string key, string line, double time)
		{
			//lastKnownTimeInGame = time;
			string type	= BasicIncidentLogReader.ExtractValue(line,"type");
			string target = BasicIncidentLogReader.ExtractValue(line,"target");
			if (type == "workaround fixed")
			{
				//set workaround as ended
				//AddEvent(target, lastKnownTimeInGame, true,"", ServiceEvent.eventType.WORKAROUND);
				AddEvent(target, time, true,"", ServiceEvent.eventType.INCIDENT);
			}
		}
		//
		// If we are doing the overall report then we must also register for all biz_service_users and
		// not just the biz_services themselves. After we see a biz_service go down we will then see
		// any biz_service_users go down as welland then see a costed event with the incident id. This
		// will let us know how many have gone down.
		//
		protected virtual void biLogReader_LineFound(object sender, string key, string line, double time)
		{
			//lastKnownTimeInGame = time;
			if(bizToTracker.ContainsKey(key))
			{
				LatchEdgeTracker let = (LatchEdgeTracker) bizToTracker[key];
				let.CheckLine(line, time);
			}
		}

		// We also want to watch for danger_level changes on the servers.
		protected void biLogReader_ServerLineFound (object sender, string key, string line, double time)
		{
			if (serverNameToTracker.ContainsKey(key))
			{
				LatchEdgeTracker let = serverNameToTracker[key] as LatchEdgeTracker;
				let.CheckLine(line, time);
			}
		}

		protected void biLogReader_AWT_Changed(object sender, string key, string line, double time)
		{
			string enabled_str  = BasicIncidentLogReader.ExtractValue(line, "enabled");

			if (enabled_str != string.Empty)
			{
				if (enabled_str.ToLower().Equals("true"))
				{
					this.AWT_Active = true;
				}
				else
				{
					this.AWT_Active = false;
				}
			}
		}

		// And on the apps.
		protected void biLogReader_AppLineFound (object sender, string key, string line, double time)
		{
			if (appNameToTracker.ContainsKey(key))
			{
				LatchEdgeTracker let = appNameToTracker[key] as LatchEdgeTracker;
				let.CheckLine(line, time);
			}
		}

		protected void biLogReader_RevenueFound(object sender, string key, string line, double time)
		{
			string lost_rev = BasicIncidentLogReader.ExtractValue(line,"revenue_lost");
			string all_lost_rev = BasicIncidentLogReader.ExtractValue(line, "all_lost_revenues");

			if (lost_rev != string.Empty)
			{
				int inx = (int)(time/60);
				this.LostRevenues[inx] = lost_rev;
			}

			if (all_lost_rev == CONVERT.ToStr(true))
			 {
				bool allStoresTaken = true;
				int i = 1;
				while (allStoresTaken)
				{
					string businessType = CoreUtils.SkinningDefs.TheInstance.GetData("biz");

					string storeLost = BasicIncidentLogReader.ExtractValue(line,"lost" + businessType + i);
					string RevLostTime = BasicIncidentLogReader.ExtractValue(line, "i_doAfterSecs");
					AttributeValuePair avp = new AttributeValuePair(RevLostTime, storeLost);


					if (storeLost == null || storeLost == "")
					{
						allStoresTaken = false;
					}
					else
					{
						while (LostRevenuePerStore.Count < i)
						{
							Dictionary<int, int> blankList = new Dictionary<int, int>();
							LostRevenuePerStore.Add(blankList);
						}
						LostRevenuePerStore[i - 1].Add(CONVERT.ParseInt(RevLostTime), CONVERT.ParseInt(storeLost));
					}

					i++;
				}
			}
		}

		protected void biLogReader_ApplicationsProcessedFound (object sender, string key, string line, double time)
		{
			string lost_rev = BasicIncidentLogReader.ExtractValue(line, "apps_lost");

			if (lost_rev != string.Empty)
			{
				int inx = (int) (time / 60);
				this.LostRevenues[inx] = lost_rev;
			}
		}

		protected virtual void WatchAdditionalItems (BasicIncidentLogReader logReader)
		{
		}
	}
}