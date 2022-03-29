using IncidentManagement;

using Network;

namespace DevOps.OpsEngine
{
    public class DevOpsIncidentApplier : IncidentApplier
    {
        
        public IIncidentSlotTracker IncidentSlotTracker
        {
            get;
            set;
        }

        public DevOpsIncidentApplier(NodeTree network):
            base(network)
        {
            
        }


        protected override void incidentEntryQueue_ChildAdded(Node sender, Node child)
        {
	        var incident = GetIncident(child.GetAttribute("id"));

			if ((incident != null) && (incident.GetNamesOfNodesBrokenByAction().Count > 0))
	        {
		        if (IncidentSlotTracker.GetRemainingSlots() <= 0)
		        {
			        string id = child.GetAttribute("id");
			        string error = "Cannot display Incident " + id +
			                       " as the maximum number of Incidents are already being displayed.";
			        child.Parent.DeleteChildTree(child);
			        OutputError(error);

			        return;
		        }
			}

			base.incidentEntryQueue_ChildAdded(sender, child);
        }
    }
}