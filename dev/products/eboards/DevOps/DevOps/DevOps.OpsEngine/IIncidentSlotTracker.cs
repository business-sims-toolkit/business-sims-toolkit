namespace DevOps.OpsEngine
{
	public interface IIncidentSlotTracker
	{
        int RemainingIncidentSlots { get; }
		int GetRemainingSlots();
	}
}
