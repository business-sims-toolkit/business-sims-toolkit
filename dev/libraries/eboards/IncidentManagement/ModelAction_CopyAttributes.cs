using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;

using LibCore;
using Network;

namespace IncidentManagement
{
	/// <summary>
	/// Summary description for IDefCopyAttributes.
	/// </summary>
	public class ModelAction_CopyAttributes : TargetedModelAction
	{
		string from = "";

		public override object Clone ()
		{
			ModelAction_CopyAttributes inc = new ModelAction_CopyAttributes();
			inc.target = this.target;
			inc.doAfterSecs = this.doAfterSecs;
			inc.from = this.from;
			return inc;
		}

		public ModelAction_CopyAttributes ()
		{
		}

		public ModelAction_CopyAttributes (XmlNode n)
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
					else if (a.Name == "i_from")
					{
						from = a.Value;
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
            var oldFromAttribute = new List<AttributeValuePair> { new AttributeValuePair("from", from) };
            var newFromAttribute = Node.RemapSpecialAttributes(nt, oldFromAttribute);

            var newFrom = newFromAttribute[0].Value;

            var oldTargetAttribute = new List<AttributeValuePair> { new AttributeValuePair("target", target) };
            var newTargetAttribute = Node.RemapSpecialAttributes(nt, oldTargetAttribute);

            var newTarget = newTargetAttribute[0].Value;

			Node fromNode = nt.GetNamedNode(newFrom);
			Node targetNode = nt.GetNamedNode(newTarget);

			ArrayList newAttrs = new ArrayList();

			foreach (AttributeValuePair avp in fromNode.AttributesAsArrayList)
			{
				if (avp.Attribute.ToLower() != "name")
				{
					newAttrs.Add(avp);
				}
			}

			targetNode.SetAttributes(newAttrs);
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