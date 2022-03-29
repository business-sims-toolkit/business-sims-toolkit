using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;

using LibCore;
using Network;

namespace IncidentManagement
{
	/// <summary>
	/// Summary description for IDefMoveMatching.
	/// </summary>
	public class ModelAction_MoveMatching : TargetedModelAction
	{
		string attribute = "";
		string val = "";

		public override object Clone ()
		{
			ModelAction_MoveMatching inc = new ModelAction_MoveMatching();
			inc.target = this.target;
			inc.doAfterSecs = this.doAfterSecs;
			inc.attribute = this.attribute;
			inc.val = this.val;
			return inc;
		}

		public override Dictionary<Node, Dictionary<string, string>> GetAllTargetsAndAttributes (NodeTree model)
		{
			return new Dictionary<Node, Dictionary<string, string>> ();
		}

		ModelAction_MoveMatching ()
		{
		}

		public ModelAction_MoveMatching (XmlNode n)
		{
			foreach (XmlAttribute a in n.Attributes)
			{
				if (a.Name.StartsWith("i_"))
				{
					// This is an IDef command.
					if (a.Name == "i_to")
					{
						// This is the target node to apply the attributes to.
						target = a.Value;
					}
					else if (a.Name == "i_attribute")
					{
						attribute = a.Value;
					}
					else if (a.Name == "i_value")
					{
						val = a.Value;
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

            Node targetNode = nt.GetNamedNode(newTarget);
			ArrayList nodes = nt.GetNodesWithAttributeValue(attribute, val);
			foreach (Node n in nodes)
			{
				if (!targetNode.HasChild(n))
				{
					Node oldParent = n.Parent;
					targetNode.AddChild(n);
					targetNode.Tree.FireMovedNode(oldParent, n);
				}
			}
		}

		public override void ApplyAction (INodeChanger nodeChanger)
		{
			throw new NotImplementedException();
		}
	}
}