using Network;

namespace IncidentManagement
{
	/// <summary>
	/// A TransitionEventDelayManager checks delayed events into the Calendar and also
	/// removes them from the calendar if they are removed.
	/// </summary>
	public class TransitionEventDelayManager : EventDelayManager
	{
		NodeTree model;
		Node calendarNode;

		public TransitionEventDelayManager(NodeTree _model)
		{
			model = _model;
			calendarNode = model.GetNamedNode("Calendar");
		}

		public override bool AddEvent(IEvent e, int onDay, NodeTree nt)
		{
//			// First check that the day is free. If not then fire the error!
//			foreach(Node eventNode in calendarNode)
//			{
//				if(eventNode.GetAttribute("day") == CONVERT.ToStr(onDay))
//				{
//					if("true" == eventNode.GetAttribute("block"))
//					{
//						return false;
//					}
//				}
//			}
//			// Add a blocking event in the calendar...
//			IncidentDefinition idef = e as IncidentDefinition;
//			if(null != idef)
//			{
//				ArrayList attrs = new ArrayList();
//				attrs.Add( new AttributeValuePair("block","true") );
//				attrs.Add( new AttributeValuePair("day",CONVERT.ToStr(onDay)) );
//				attrs.Add( new AttributeValuePair("showName",idef.Description) );
//				attrs.Add( new AttributeValuePair("type",idef.Type) );
//				attrs.Add( new AttributeValuePair("status","active") );
//				attrs.Add( new AttributeValuePair("target",(string)idef.GetAttribute("target")));
//				Node newEvent = new Node(calendarNode, idef.Type, "", attrs);
//			}

			return base.AddEvent(e,onDay,nt);
		}

		public override bool RemoveEvent(IEvent e)
		{
			if(base.RemoveEvent(e))
			{
				// TODO : This event was successfully removed from the future so remove the event from
				// the calendar. (We don't remove events from the past as there doesn't seem
				// much point? - We can easily change this behaviour if need be).
				return true;
			}
			return false;
		}

		new internal virtual bool FireEvent(PendingEvent pe)
		{
			pe.theEvent.ApplyActionNow(pe.theNodeTree);
			return true;
		}

	}
}
