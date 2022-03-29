using System.Collections;
using Network;

namespace BusinessServiceRules
{
	/// <summary>
	/// A MultipleServiceMonitor monitors all the services on a car/airport terminal, etc.
	/// It attaches a ServiceStatusMonitor to each business service user.
	/// </summary>
	public class MultipleServiceMonitor
	{
		ServiceDownCounter _sdc;
		ArrayList monitors = new ArrayList();

		Node monitoredEntity;
		// All sub nodes with type="biz_service" are monitored for
		// having a valid path all the way to the root node of the NodeTree.
		public MultipleServiceMonitor(string namedNode, NodeTree nt, ServiceDownCounter sdc)
		{
			_sdc = sdc;
			monitoredEntity = nt.GetNamedNode(namedNode);

			if(null != monitoredEntity)
			{
				monitoredEntity.ChildAdded +=monitoredEntity_ChildAdded;
				monitoredEntity.ChildRemoved+=monitoredEntity_ChildRemoved;
				AttachAllServices();
			}
		}

		protected void AttachAllServices()
		{
			foreach(Node n in monitoredEntity)
			{
				if("biz_service_user" == n.Type)
				{
					// Add this service to the watch list.
					ServiceStatusMonitor ssm = new ServiceStatusMonitor(n,_sdc);
					monitors.Add(ssm);
				}
			}
		}

		/// <summary>
		/// Dispose ...
		/// </summary>
		public void Dispose()
		{
			foreach(ServiceStatusMonitor ssm in monitors)
			{
				ssm.Dispose();
			}
			if (monitoredEntity != null)
			{
				monitoredEntity.ChildAdded -=monitoredEntity_ChildAdded;
				monitoredEntity.ChildRemoved -=monitoredEntity_ChildRemoved;
				monitoredEntity = null;
			}
		}

		void monitoredEntity_ChildAdded(Node sender, Node child)
		{
			if("biz_service_user" == child.Type)
			{
				// Add this service to the watch list.
				ServiceStatusMonitor ssm = new ServiceStatusMonitor(child,_sdc);
				monitors.Add(ssm);
			}
		}

		void monitoredEntity_ChildRemoved(Node sender, Node child)
		{
			if("biz_service_user" == child.Type)
			{
				ServiceStatusMonitor FoundItem = null;
				foreach (ServiceStatusMonitor ssm in monitors)
				{
					if (ssm.getMonitoredItem() == child)
					{
						FoundItem = ssm;
					}
				}
				if (FoundItem != null)
				{
					monitors.Remove(FoundItem);
					FoundItem.Dispose();
				}
			}

		}
	}
}
