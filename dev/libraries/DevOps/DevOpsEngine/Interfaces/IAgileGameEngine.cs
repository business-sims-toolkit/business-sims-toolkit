using DevOpsEngine.RequestsManagers;
using IncidentManagement;

namespace DevOpsEngine.Interfaces
{
	public interface IAgileGameEngine
	{
		//int MaxMetrics { get; }
		AgileRequestsManager RequestsManager { get; }
		DevelopingAppTerminator AppTerminator { get; }
		//IncidentApplier IncidentApplier { get; }
		//AgileComplaints AgileComplaints { get; }


		void Reset();
	}
}
