using System;
using System.Collections;
using System.Xml;

using LibCore;
using Network;

namespace Polestar_PM.OpsEngine
{
	/// <summary>
	///  IncidentCountMonitor watches a [CostedEvents] node in the model to track 
	///  the number of incidents that have occured in the round.  
	///  It only looks at the "incidents" children added to the cost events 
	///  
	///NODE PROCESSING 
	///This class is looking at child nodes being added to the costed events 
	///The CostMonitor class also does this and it deletes the children as they are processed.
	///So There may be a race condition 
	///  If the CostMonitor processes and deletes the child before the IncidentCountMonitor
	///  then the IncidentCountMonitor will still get the event but the child may be null
	///  
	///In general, we need some way of deletring a child after all the processing has been acchieved.
	///One way would be to add two methods to all nodes
	///  AddSuicideNote         --  setting a internal boolean suicide note variable
	///  PerformSuicideIfNeeded --  Check if the boolean is treue and deleted itself. 
	/// 
	///So using the context of the above example 
	///  CostMonitor calls the AddSuicideNote()
	///  and once the node has finished servicing all the ChildAdded handlers, 
	///  its runs the PerformSuicideIfNeeded method 
	/// </summary>
	public class IncidentCountMonitor
	{
		protected Node monitoredEntity;
		protected Node outputEntity;

		protected int totalIncidentCount = 0;

		public IncidentCountMonitor(NodeTree nt)
		{
			monitoredEntity = nt.GetNamedNode("CostedEvents");
			monitoredEntity.ChildAdded += new Network.Node.NodeChildAddedEventHandler(monitoredEntity_ChildAdded);
			outputEntity = nt.GetNamedNode("IncidentCount");
		}

		private void IncrementCount()
		{
			if (outputEntity != null)
			{
				totalIncidentCount = outputEntity.GetIntAttribute("count",0);
				totalIncidentCount++;
				outputEntity.SetAttribute("count", CONVERT.ToStr(totalIncidentCount));
			}
		}

		private void monitoredEntity_ChildAdded(Node sender, Node child)
		{
			string val = child.GetAttribute("type");
			if("incident" == val)
			{
				 IncrementCount();
			}
		}

		/// <summary>
		/// The Dispose method that unhooks the CostMonitor from the model node that is being watched.
		/// </summary>
		public void Dispose()
		{
			monitoredEntity.ChildAdded -= new Network.Node.NodeChildAddedEventHandler(monitoredEntity_ChildAdded);
			monitoredEntity = null;
			outputEntity = null;
		}

	}
}

