using System.Linq;
using Network;

namespace DevOpsEngine
{
	public class FeatureOptimalityChecker
	{
		public static int CheckOptimalityForFeature (Node featureNode)
		{

			var fullProducts = featureNode.GetChildrenAsList()
				.Where(p => ! p.GetBooleanAttribute("is_prototype", false)).ToList();



			return 0;
		}
	}
}
