using System.Collections.Generic;
using System.Collections.Specialized;
using Network;

namespace IncidentManagement
{
	public abstract class TargetedModelAction : ModelActionBase
	{
		protected string target = "";

		public override string GetTarget ()
		{
			return target;
		}

		public void SetTarget (string t)
		{
			target = t;
		}

		public TargetedModelAction ()
		{
		}

		public virtual void AlterTargets (StringDictionary mappedTargets)
		{
			if (mappedTargets.ContainsKey(target))
			{
				target = mappedTargets[target];
			}
		}

		public virtual List<Node> GetAllTargets (NodeTree model)
		{
			var list = new List<Node> ();
			var node = model.GetNamedNode(target);
			if (node != null)
			{
				list.Add(node);
			}

			return list;
		}
	}
}