using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Xml;

using LibCore;
using Network;

namespace IncidentManagement
{
	/// <summary>
	/// Summary description for IDef_AddNodes.
	/// </summary>
	public class ModelAction_AddNodes : TargetedModelAction
	{
		// An array of nodes to add.
		System.Collections.Specialized.StringCollection nodesToAdd = new StringCollection();
		//
		public override object Clone ()
		{
			ModelAction_AddNodes newAddNodes = new ModelAction_AddNodes();
			newAddNodes.target = this.target;
			newAddNodes.doAfterSecs = this.doAfterSecs;
			foreach (string str in nodesToAdd)
			{
				newAddNodes.nodesToAdd.Add(str);
			}
			return newAddNodes;
		}

		public override Dictionary<Node, Dictionary<string, string>> GetAllTargetsAndAttributes (NodeTree model)
		{
			return new Dictionary<Node, Dictionary<string, string>> ();
		}

		protected ModelAction_AddNodes ()
		{
		}

		public ModelAction_AddNodes (XmlNode n)
		{
			foreach (XmlAttribute a in n.Attributes)
			{
				if (a.Name == "i_to")
				{
					target = a.Value;
				}
				else if (a.Name == "i_doAfterSecs")
				{
					doAfterSecs = CONVERT.ParseInt(a.Value);
				}
			}
			//
			foreach (XmlNode child in n.ChildNodes)
			{
				if (child.NodeType == XmlNodeType.Element)
				{
					if (child.Name == "node")
					{
						foreach (XmlAttribute a in child.Attributes)
						{
							if (a.Name == "i_name")
							{
								nodesToAdd.Add(a.Value);
							}
						}
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
			foreach (string namedNode in nodesToAdd)
			{
				Node child = nt.GetNamedNode(namedNode);
				if (!n.HasChild(child))
				{
					Node oldParent = child.Parent;
					n.AddChild(child);
					n.Tree.FireMovedNode(oldParent, child);
				}
			}
		}

		public override void ApplyAction (INodeChanger nodeChanger)
		{
			throw new NotImplementedException();
		}
	}
}