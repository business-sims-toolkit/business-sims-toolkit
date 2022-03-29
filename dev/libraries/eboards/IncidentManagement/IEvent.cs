using Network;

namespace IncidentManagement
{
	public interface IEvent
	{
		void ApplyActionNow(NodeTree nt);
		void ApplyActionNow (INodeChanger nodeChanger);
	}
}