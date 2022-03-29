using System.Collections.Generic;

using DevOpsEngine.ModelProperties;
using Network;

namespace DevOpsEngine.Interfaces
{
	public interface IRequestsManager
	{
		List<EnclosureProperties> GetEnclosures (Node featureNode);
		List<string> GetEnclosureNames ();
		bool CanDeployFeature (string developmentName);
	}
}
