using System.Collections.Generic;

using CommonGUI;
using CoreUtils;
using DevOps.OpsEngine;
using Network;

namespace DevOps.OpsScreen.FacilitatorControls
{
    internal class IncidentPanel : FlickerFreePanel, IIncidentSlotTracker
    {
        public IncidentPanel (NodeTree networkModel)
        {
            maxBrokenIncidents = SkinningDefs.TheInstance.GetIntData("max_broken_incidents");

            incidentNameToPositionIndex = new Dictionary<string, int>();
        }

        public int RemainingIncidentSlots => maxBrokenIncidents - incidentNameToPositionIndex.Count;

        public int GetRemainingSlots ()
        {
            return RemainingIncidentSlots;
        }

        readonly int maxBrokenIncidents;
        readonly Dictionary<string, int> incidentNameToPositionIndex;

    }
}
