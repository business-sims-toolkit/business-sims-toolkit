using System;

namespace IncidentManagement
{
	public interface ISlaManager : IDisposable
	{
		void ResetBsuSlaBreaches ();
	}
}