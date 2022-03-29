using System.Xml;
//
using LibCore;

namespace ReportBuilder
{
	/// <summary>
	/// Summary description for MS_OpsGanttReport.
	/// </summary>
	public class MS_OpsGanttReport : OpsGanttReport
	{
		/// <summary>
		/// Store a Hashtable of Business Services to ArrayLists of status over time.
		/// </summary>
		//protected double lastKnownTimeInGame = 0;

		public MS_OpsGanttReport()
		{
		}
	
		protected override void OutputServiceData(BasicXmlDocument xdoc, XmlNode root, string service)
		{
			double lastKnownTimeInGame = 0;
			if (roundScores != null) lastKnownTimeInGame = roundScores.FinalTime;

			if (BizServiceStatusStreams.ContainsKey(service))
			{
				XmlNode row = (XmlNode) xdoc.CreateElement("row");
				((XmlElement)row).SetAttribute( "colour","255,0,0" );
				((XmlElement)row).SetAttribute( "data",service );
				//
				EventStream eventStream = (EventStream) BizServiceStatusStreams[service];
				//
				foreach(ServiceEvent se in eventStream.events)
				{
					if (se.secondsIntoGameOccured == se.secondsIntoGameEnds)
					{
						continue;
					}

					int isecs = (int) (se.secondsIntoGameOccured);

					XmlNode bar = (XmlNode) xdoc.CreateElement("bar");
					((XmlElement)bar).SetAttribute( "x",CONVERT.ToStr(isecs));

					//int ilength = 25*60-isecs;
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
						SetBarAttributes((XmlElement) bar, "incident", "255,0,0", "", "");
					}
					else if (se.seType == ServiceEvent.eventType.DENIAL_OF_SERVICE)
					{
						SetBarAttributes((XmlElement) bar, "dos", "0,0,0", "0,0,0", "");
					}
					else if (se.seType == ServiceEvent.eventType.SECURITY_FLAW)
					{
						SetBarAttributes((XmlElement)bar, "security_flaw", "0,0,0", "0,0,0", "");
					}
					else if (se.seType == ServiceEvent.eventType.WORKAROUND)
					{
						SetBarAttributes((XmlElement) bar, "workaround", "0,0,204", "", "");
					}
					else if (se.seType == ServiceEvent.eventType.SLABREACH)
					{
						SetBarAttributes((XmlElement) bar, "slabreach", "", "", "orange_hatch");
					}
					else if (se.seType == ServiceEvent.eventType.WA_SLABREACH)
					{
						SetBarAttributes((XmlElement) bar, "workaround_slabreach", "", "", "blue_hatch");
					}
					else if (se.seType == ServiceEvent.eventType.DOS_SLABREACH)
					{
						SetBarAttributes((XmlElement) bar, "dos_slabreach", "", "", "dos_hatch");

						if (CoreUtils.SkinningDefs.TheInstance.GetIntData("gantt_dos_slabreach_has_border", 1) == 1)
						{
							((XmlElement)bar).SetAttribute("bordercolour", "0,0,0");
						}
					}
					else if (se.seType == ServiceEvent.eventType.NO_POWER)
					{
						SetBarAttributes((XmlElement) bar, "nopower", "", "", "nopower");
					}
					else if (se.seType == ServiceEvent.eventType.NO_POWER_SLA_BREACH)
					{
						SetBarAttributes((XmlElement) bar, "nopower_slabreach", "", "", "no_power_sla");
					}
					else if (se.seType == ServiceEvent.eventType.INSTALL || se.seType == ServiceEvent.eventType.UPGRADE)
					{
						SetBarAttributes((XmlElement) bar, "install_upgrade", "255,0,255", "", "");
					}
					else if (se.seType == ServiceEvent.eventType.MIRROR)
					{
						SetBarAttributes((XmlElement) bar, "mirror", "255,171,0", "", "");
					}
					else if (se.seType == ServiceEvent.eventType.WARNING)
					{
						SetBarAttributes((XmlElement) bar, "warning", "255,0,0", "", "");
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

	}
}
