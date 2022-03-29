using System.Collections;
//
namespace ReportBuilder
{
	/// <summary>
	/// Summary description for ServiceEvent.
	/// </summary>
	public class ServiceEvent
	{
		public enum eventType
		{
			OK,
			INCIDENT,
			WORKAROUND,
			SLABREACH,
			WA_SLABREACH,
			DOS_SLABREACH,
			INSTALL,
			UPGRADE,
			MIRROR,
			WARNING,
			DENIAL_OF_SERVICE,
			SECURITY_FLAW,
			SECURITY_FLAW_SLABREACH,
			COMPLIANCE_INCIDENT,
			COMPLIANCE_INCIDENT_SLA_BREACH,
			THERMAL,
			THERMAL_WARNING,
			THERMAL_SLA_BREACH,
			NO_POWER,
			NO_POWER_SLA_BREACH,
			VMOTION_GREEN,
			VMOTION_RED,
			INCIDENT_ON_SAAS,
			REQUEST,
			REQUEST_SLA_BREACH,
            INFORMATION_REQUEST,
            INFORMATION_REQUEST_SLA_BREACH
		};

		public double secondsIntoGameOccured;
		public double secondsIntoGameEnds;
		public bool up;
		public string reason = "";
		public string short_reason = "";

		public string zoneName = "";
		public string incidentId = "";
		
		public eventType seType;

		//public ArrayList subEvents = new ArrayList();
		public string users_affected = "";

		public ServiceEvent last = null;
		public ServiceEvent next = null;

		public double GetLength(double endOfGame)
		{
			double length = 0;
			if(secondsIntoGameEnds == 0)
			{
				length = endOfGame - secondsIntoGameOccured;
			}
			else
			{
				length = secondsIntoGameEnds - secondsIntoGameOccured;
			}

			ServiceEvent plast = last;
			while(null != plast)
			{
				length += plast.secondsIntoGameEnds - plast.secondsIntoGameOccured;
				plast = plast.last;
			}
			//
			ServiceEvent pnext = next;
			while(null != pnext)
			{
				if(pnext.secondsIntoGameEnds == 0.0)
				{
					length += endOfGame - pnext.secondsIntoGameOccured;
				}
				else
				{
					length += pnext.secondsIntoGameEnds - pnext.secondsIntoGameOccured;
				}
				pnext = pnext.next;
			}
			//
			return length;
		}

		public ServiceEvent(double seconds, bool isUp, eventType etype)
		{
			secondsIntoGameOccured = seconds;
			up = isUp;
			reason = "";
			seType = etype;
		}

		public ServiceEvent(double seconds, bool isUp, string theReason, eventType etype)
		{
			secondsIntoGameOccured = seconds;
			up = isUp;
			reason = theReason;
			seType = etype;
		}

		public ServiceEvent(double seconds, bool isUp, string theReason, eventType etype, string tmp_incidentId)
		{
			secondsIntoGameOccured = seconds;
			up = isUp;
			reason = theReason;
			seType = etype;
			incidentId = tmp_incidentId;
		}

		public void SetZoneAffected (int zone)
		{
			if (zone != -1)
			{
				zoneName = LibCore.CONVERT.ToStr(zone);
			}
			else
			{
				zoneName = "";
			}
		}

		public void SetIncidentId (string id)
		{
			incidentId = id;
		}

		public void SetUsersAffected(string str)
		{
			users_affected = str;
		}
		/*
		public void AddSubEvent(string e)
		{
			if(!subEvents.Contains(e))
			{
				subEvents.Add(e);
			}
		}*/
	}

	public class EventStream
	{
		public ArrayList events = new ArrayList();
		public ServiceEvent lastEvent = null;
	}
}
