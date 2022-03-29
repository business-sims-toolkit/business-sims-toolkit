using System.Collections;
using Network;

namespace IncidentManagement
{
	/// <summary>
	/// An EventDelayManager manages the actual management of how an event is delayed.
	/// E.g. During Ops phase the event is simply delayed by N seconds,
	/// but in the Transition phase the event is logged into a calendar and it cannot conflict
	/// with another event.
	/// </summary>
	public abstract class EventDelayManager
	{
		//
		// A hash table from tick due for event to an array of events to do at that time.
		//
		internal Hashtable eventsDue = new Hashtable();

		internal Hashtable modelCounterToEventsDue = new Hashtable();
		internal Hashtable modelCounterToTargetAttribute = new Hashtable();

		internal virtual bool FireEvent(PendingEvent pe)
		{
			pe.theEvent.ApplyActionNow(pe.theNodeTree);
			return true;
		}

		public virtual void Clear()
		{
			eventsDue.Clear();
			modelCounterToEventsDue.Clear();
			modelCounterToTargetAttribute.Clear();
		}

		protected EventDelayManager()
		{
		}

		public virtual bool AddEvent(Node extendedNode, IEvent e, int onSecond, NodeTree nt)
		{
			lock(this)
			{
				if(this.modelCounterToEventsDue.ContainsKey(extendedNode))
				{
					Hashtable events = (Hashtable) modelCounterToEventsDue[extendedNode];
					// Assumming single thread for now for NeoSwiff apps.
					// Should add LibCore.LockAcquire / LockRelease funcs to
					// be really thread safe.
					int dueAtTick = onSecond;
					//int dueAtTick = EventDelayer.TheInstance._tick + onSecond;
					//
					PendingEvent pe = new PendingEvent();
					pe.theEvent = e;
					pe.theNodeTree = nt;
					//
					if(events.ContainsKey(dueAtTick))
					{
						ArrayList array = (ArrayList) events[dueAtTick];
						array.Add(pe);
					}
					else
					{
						// Create an array list for events at this time.
						ArrayList array = new ArrayList();
						array.Add(pe);
						events.Add(dueAtTick,array);
					}
				}
				else
				{
					return false;
				}
			}

			return true;
		}

		public virtual bool AddEvent(IEvent e, int onSecond, NodeTree nt)
		{
			lock(this)
			{
				// Assumming single thread for now for NeoSwiff apps.
				// Should add LibCore.LockAcquire / LockRelease funcs to
				// be really thread safe.
				int dueAtTick = onSecond;
				//int dueAtTick = EventDelayer.TheInstance._tick + onSecond;
				//
				PendingEvent pe = new PendingEvent();
				pe.theEvent = e;
				pe.theNodeTree = nt;
				//
				if(eventsDue.ContainsKey(dueAtTick))
				{
					ArrayList array = (ArrayList) eventsDue[dueAtTick];
					array.Add(pe);
				}
				else
				{
					// Create an array list for events at this time.
					ArrayList array = new ArrayList();
					array.Add(pe);
					eventsDue.Add(dueAtTick,array);
				}
			}

			return true;
		}

		/// <summary>
		/// Removes an Event 
		/// </summary>
		/// <param name="e">The event to kill</param>
		public virtual bool RemoveEvent(IEvent e)
		{
			lock(this)
			{
				PendingEvent pe_tokill = null;
				foreach(ArrayList array in eventsDue.Values)
				{
					foreach(PendingEvent pe in array)
					{
						if (pe.theEvent == e)
						{
							pe_tokill = pe;
						}
					}

					if(pe_tokill != null)
					{
						array.Remove(pe_tokill);
						pe_tokill = null;
						return true;
					}
				}
			}

			return false;
		}
	}
}
