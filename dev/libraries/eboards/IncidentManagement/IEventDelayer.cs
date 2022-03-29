using System;
using System.Collections;
using Network;

namespace IncidentManagement
{
	public interface IEventDelayer : IDisposable
	{
		bool AddEvent (IEvent scheduledEvent, int doAfterSecs, NodeTree model);
		bool AddEvent (string eventXml, int doAfterSecs, NodeTree model);
		bool RemoveEvent (IEvent scheduledEvent);

		Hashtable GetFutureEventsWithAttributeValues (ArrayList avps);

		Hashtable GetAllFutureIDefEvents ();

		void SetModelCounter (NodeTree _Network, string p, string p_2);

		void SetEventManager (EventDelayManager transitionEventDelayManager);

		void Clear ();
	}
}