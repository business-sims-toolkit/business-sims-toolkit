using System;
using System.Collections.Generic;
using Network;

namespace IncidentManagement
{
	public abstract class ModelActionBase : IEvent, ICloneable
	{
		public int doAfterSecs = 0;
		public int cancelWithIncident = 0;

		public ModelActionBase ()
		{
		}

		abstract public void ApplyAction (NodeTree nt);
		abstract public void ApplyAction (INodeChanger nodeChanger);

		public virtual string GetTarget ()
		{
			return "";
		}

		public virtual void ApplyActionNow (NodeTree nt)
		{
		}

		public virtual void ApplyActionNow (INodeChanger nodeChanger)
		{
		}

		public abstract object Clone ();

		public virtual System.Collections.Generic.IList<string> GetNamesOfNodesBrokenByAction ()
		{
			return null;
		}

		public abstract Dictionary<Node, Dictionary<string, string>> GetAllTargetsAndAttributes (NodeTree model);
	}
}