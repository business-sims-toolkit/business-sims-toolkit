using System.Collections;
using Network;

namespace TransitionObjects
{
	/// <summary>
	/// Summary description for CalendartRunner.
	/// </summary>
	public class CalendarRunner// : ITimedClass
	{
		//Node Name Definitions  
		const string Node_CalendarName = "Calendar";

		//Nodes that we need to Watch for UI events
		Node calendarNode = null;

		Node dayNode;

		Node businessNotifiedEvents;

		//My List of the running projects 
		//protected ArrayList MyRunningCalendarEvents = null;

		//Support Varibles 
		string dataDir = string.Empty;
		bool isTraining = false;

		#region Constructor, Connections and Dispose

		public CalendarRunner(NodeTree nt, bool isTrainingFlag)
		{
			isTraining = isTrainingFlag;

			businessNotifiedEvents = nt.GetNamedNode("BusinessNotifiedEvents");
			setCalendarNode( nt.GetNamedNode("Calendar") );
			dayNode = nt.GetNamedNode("CurrentDay");
			dayNode.AttributesChanged += dayNode_AttributesChanged;
			//
			// If there is something in the calendar events for today then fire it...
			//
			ProcessCalendarEvents( dayNode.GetAttribute("day") );
			//
			calendarNode.ChildAdded += calendarNode_ChildAdded;
			//calendarNode.AttributesChanged += new Network.Node.AttributesChangedEventHandler(calendarNode_AttributesChanged);
			//MyRunningCalendarEvents = new ArrayList();
		}
		/// <summary>
		/// Setting all the handles to null, detachs the Event handlers
		/// </summary>
		public void Dispose()
		{
			dayNode.AttributesChanged -= dayNode_AttributesChanged;
			setCalendarNode(null);
		}

		#endregion Constructor, Connections and Dispose
	
		#region Calendar Event Handlers	

		/// <summary>
		/// Connect up the ProjectsCreateRequestedNode
		/// </summary>
		/// <param name="nt"></param>
		protected void setCalendarNode(Node nt)
		{
			//disconnect from previous
			if (calendarNode != null)
			{
				calendarNode = null;
			}
			if(null != nt)
			{
				//Connect up to the new node 
				calendarNode = nt;
			}
		}

		#endregion Calendar Event Handlers		

		void calendarNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			//
		}

		void ProcessCalendarNode(Node n, string day)
		{
			if(n.GetAttribute("day") == day)
			{
				// This event is due today. If it has flash pass it on to any Business Event Viewer...
				string flash = n.GetAttribute("gamemoviefile");
				string trainingflash = n.GetAttribute("trainingmoviefile");

				if (isTraining == true)
				{
					if ("" != trainingflash)
					{
						flash = trainingflash;
					}
				}

				if("" != flash)
				{
					ArrayList atts = new ArrayList();
					atts.Add( new AttributeValuePair("swf",flash) );
					atts.Add( new AttributeValuePair("duration",n.GetAttribute("duration")) );
					atts.Add(new AttributeValuePair("loop", n.GetBooleanAttribute("loop", true)));
					Node newNode = new Node(businessNotifiedEvents,"shownBizEvent", "", atts);
				}
			}
		}

		void ProcessCalendarEvents(string day)
		{

			// Roll over our calendar events and see if we have any events to do today...
			foreach(Node n in this.calendarNode)
			{
				ProcessCalendarNode(n,day);
			}
		}

		void dayNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			foreach(AttributeValuePair avp in attrs)
			{
				if(avp.Attribute == "day")
				{
					ProcessCalendarEvents( avp.Value );
					return;
				}
			}
		}

		void calendarNode_ChildAdded(Node sender, Node child)
		{
			// If an event is added for today then we should fire it immediately!
			// If a game wants to disallow putting calendar events on the current day then
			// it is up to the thing that enters the event to say you can't do that particular
			// action on that day.
			ProcessCalendarNode( child, dayNode.GetAttribute("dat") );
		}
	}
}
