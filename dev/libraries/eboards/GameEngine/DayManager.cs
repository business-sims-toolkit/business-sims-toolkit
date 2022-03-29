using System.Collections;
using LibCore;
using Network;
using IncidentManagement;

namespace GameEngine
{
	/// <summary>
	/// This class handles the Day Events 
	/// </summary>
	public class DayManager : BaseClass
	{
		/// <summary>
		/// 
		/// </summary>
		//public static readonly DayManager TheInstance = new DayManager();
		const string Node_CurrentDay = "CurrentDay";
		//
		Hashtable eventsDue = new Hashtable();

		NodeTree MyNodeTreeRoot = null;
		Node MyCurrentDayNode = null;
		int _DayTick = 0;

		/// <summary>
		/// 
		/// </summary>
		public void Clear()
		{
			// TODO : If events can fire events on being discarded/fired then these should
			// be fired rather than letting the events just fade into oblivion.
			eventsDue.Clear();
		}

		public DayManager()
		{
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="rt"></param>
		public void SetNodeTreeRoot(NodeTree rt)
		{
			MyNodeTreeRoot = rt;
			//
			// Disconnect from old node
			//
			if (MyCurrentDayNode != null)	
			{
				MyCurrentDayNode.AttributesChanged -= MyCurrentDayNode_AttributesChanged;
			}
			//
			if (MyNodeTreeRoot != null)
			{
				// Attach to new Node
				MyCurrentDayNode = MyNodeTreeRoot.GetNamedNode(Node_CurrentDay);
				if (MyCurrentDayNode != null)
				{
					MyCurrentDayNode.AttributesChanged += MyCurrentDayNode_AttributesChanged;
				}
			}
		}

		public void Dispose()
		{
			if(MyCurrentDayNode != null)
			{
				MyCurrentDayNode.AttributesChanged -= MyCurrentDayNode_AttributesChanged;
				MyCurrentDayNode = null;
			}
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="e"></param>
		/// <param name="DaysToDelay"></param>
		/// <param name="nt"></param>
		public void AddEvent(IEvent e, int DaysToDelay, NodeTree nt)
		{
			lock(this)
			{
				// Assumming single thread for now for NeoSwiff apps.
				// Should add LibCore.LockAcquire / LockRelease funcs to
				// be really thread safe.
				int dueAtTick = _DayTick + DaysToDelay;
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
		}

		/// <summary>
		/// Removes an Event 
		/// </summary>
		/// <param name="e">The event to kill</param>
		public void RemoveEvent(IEvent e)
		{
			lock(this)
			{
				PendingEvent pe_tokill = null;
				foreach (PendingEvent pe in eventsDue)
				{
					if (pe.theEvent == e)
					{
						pe_tokill = pe;
					}
				}
				eventsDue.Remove(pe_tokill);
			}
		}

		void MyCurrentDayNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			lock(this)
			{
				//Get the 
				// Assumming single thread for now for NeoSwiff apps.
				// Should add LibCore.LockAcquire / LockRelease funcs to
				// be really thread safe.
				//
				if(eventsDue.ContainsKey(_DayTick))
				{
					ArrayList array = (ArrayList) eventsDue[_DayTick];
					foreach(PendingEvent pe in array)
					{
						pe.theEvent.ApplyActionNow(pe.theNodeTree);
					}
					//
					eventsDue.Remove(_DayTick);
				}
				//
				if(eventsDue.ContainsKey(_DayTick))
				{
					ArrayList array = (ArrayList) eventsDue[_DayTick];
					foreach(PendingEvent pe in array)
					{
						pe.theEvent.ApplyActionNow(pe.theNodeTree);
					}
					//
					eventsDue.Remove(_DayTick);
				}
			}
		}
	}
}
