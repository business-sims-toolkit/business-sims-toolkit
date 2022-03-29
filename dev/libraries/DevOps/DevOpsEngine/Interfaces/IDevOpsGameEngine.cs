using DevOpsEngine.RequestsManagers;
using IncidentManagement;

namespace DevOpsEngine.Interfaces
{
	public interface IDevOpsGameEngine
	{
		DevOpsRequestsManager RequestsManager { get; }
		DevelopingAppTerminator AppTerminator { get; }
		IncidentApplier IncidentApplier { get; }
		void Reset ();
	}
}
