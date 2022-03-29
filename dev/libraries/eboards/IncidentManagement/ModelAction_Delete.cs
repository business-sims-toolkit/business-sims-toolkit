using System;
using System.Collections.Generic;
using System.Xml;

using LibCore;
using Network;

namespace IncidentManagement
{
	/// <summary>
	/// Summary description for IDef_Apply.
	/// </summary>
	public class ModelAction_Delete : TargetedModelAction
	{
		public override object Clone ()
		{
			ModelAction_Delete del = new ModelAction_Delete(this.target, this.doAfterSecs);
			return del;
		}

		public ModelAction_Delete (string t, int delay)
		{
			target = t;
			doAfterSecs = delay;
		}

		public ModelAction_Delete (XmlNode n)
		{
			foreach (XmlAttribute a in n.Attributes)
			{
				if (a.Name.StartsWith("i_"))
				{
					// This is an IDef command.
					if (a.Name == "i_name")
					{
						// This is the target node to apply the attributes to.
						target = a.Value;
					}
					else if (a.Name == "i_doAfterSecs")
					{
						doAfterSecs = CONVERT.ParseInt(a.Value);
					}
				}
			}
		}

		override public void ApplyAction (NodeTree nt)
		{
			if (doAfterSecs > 0)
			{
				GlobalEventDelayer.TheInstance.Delayer.AddEvent(this, doAfterSecs, nt);
			}
			else
			{
				ApplyActionNow(nt);
			}
		}

		public override void ApplyActionNow (NodeTree nt)
		{
            var oldTargetAttribute = new List<AttributeValuePair> { new AttributeValuePair("target", target) };
            var newTargetAttribute = Node.RemapSpecialAttributes(nt, oldTargetAttribute);

            var newTarget = newTargetAttribute[0].Value;

            Node n = nt.GetNamedNode(newTarget);
			if (null != n)
			{
				//n.Delete();
				n.Parent.DeleteChildTree(n);
			}
		}

		public override void ApplyAction (INodeChanger nodeChanger)
		{
			throw new NotImplementedException();
		}

		public override Dictionary<Node, Dictionary<string, string>> GetAllTargetsAndAttributes (NodeTree model)
		{
			return new Dictionary<Node, Dictionary<string, string>>();
		}
	}
}