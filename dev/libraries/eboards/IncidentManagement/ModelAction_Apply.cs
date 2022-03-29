using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;

using LibCore;
using Network;

namespace IncidentManagement
{
	/// <summary>
	/// Summary description for IDef_Apply.
	/// </summary>
	public class ModelAction_Apply : TargetedModelAction
	{
		// Store the attribute/value pairs to apply to the target node.
		public ArrayList valuesToApply = new ArrayList();
		protected bool targetParent = false;
		bool append = false;

		public override object Clone()
		{
			ModelAction_Apply newApply = new ModelAction_Apply();
			newApply.target = this.target;
			newApply.doAfterSecs = this.doAfterSecs;
			foreach(AttributeValuePair avp in valuesToApply)
			{
				newApply.valuesToApply.Add( avp.Clone() );
			}
			return newApply;
		}

		protected ModelAction_Apply()
		{
		}

		public ModelAction_Apply(string t, int delay, AttributeValuePair avp)
		{
			target = t;
			doAfterSecs = delay;
			valuesToApply.Add(avp);
		}

		public ModelAction_Apply(XmlNode n)
		{
			foreach(XmlAttribute a in n.Attributes)
			{
				if(a.Name.StartsWith("i_"))
				{
					// This is an IDef command.
					if(a.Name == "i_name")
					{
						// This is the target node to apply the attributes to.
						target = a.Value;
					}
					else if(a.Name == "i_parentOf")
					{
						// This is the target node to apply the attributes to.
						target = a.Value;
						targetParent = true;
					}
					else if(a.Name == "i_doAfterSecs")
					{
						doAfterSecs = CONVERT.ParseInt(a.Value);
					}
					else if(a.Name == "i_cancelWithIncident")
					{
						cancelWithIncident = CONVERT.ParseInt(a.Value);
					}
					else if (a.Name == "i_newname")
					{
						valuesToApply.Add(new AttributeValuePair ("name", a.Value));
					}
					else if (a.Name == "i_append")
					{
						append = CONVERT.ParseBool(a.Value, false);
					}
				}
				else
				{
					// This is an attribute to apply.
					AttributeValuePair avp = new AttributeValuePair();
					avp.Attribute = a.Name;
					avp.Value = a.Value;
					valuesToApply.Add(avp);
				}
			}
		}

		override public void ApplyAction(NodeTree nt)
		{
			if(doAfterSecs > 0)
			{
				GlobalEventDelayer.TheInstance.Delayer.AddEvent(this, doAfterSecs, nt);
			}
			else
			{
				ApplyActionNow(nt);
			}
		}

		protected void OutputError(NodeTree targetTree, string errorText)
		{
			Node errorsNode = targetTree.GetNamedNode("FacilitatorNotifiedErrors");
			Node error = new Node(errorsNode, "error", "", new AttributeValuePair( "text", errorText ) );
		}

		public override void ApplyActionNow(NodeTree nt)
		{
            var oldTargetAttribute = new List<AttributeValuePair> { new AttributeValuePair("target", target) };
            var newTargetAttribute = Node.RemapSpecialAttributes(nt, oldTargetAttribute);

            var newTarget = newTargetAttribute[0].Value;

			Node n = nt.FindNodeWithSearchAttributesIfPresent(newTarget);

			if (n == null)
			{
				OutputError(nt, "Cannot apply action to " + target + " as it does not exist.");
				return;
			}

			if(targetParent)
			{
				n = n.Parent;
			}

			if(null != n)
			{
				List<AttributeValuePair> newAttributes = Node.RemapSpecialAttributes(nt, valuesToApply);

				if (append)
				{
					foreach (AttributeValuePair avp in newAttributes)
					{
						avp.Value = n.GetAttribute(avp.Attribute) + avp.Value;
					}
				}

				n.SetAttributes(newAttributes);
			}
			else
			{
				OutputError(nt, "Cannot apply incident to " + target + " as it does not exist.");
			}
		}

		public override IList<string> GetNamesOfNodesBrokenByAction ()
		{
			foreach (AttributeValuePair avp in valuesToApply)
			{
				if ((avp.Attribute == "up" && ! CONVERT.ParseBool(avp.Value, false)) || 
				    (avp.Attribute == "incident_id" && !string.IsNullOrEmpty(avp.Value)))
				{
					System.Collections.Generic.List<string> names = new System.Collections.Generic.List<string> ();
					names.Add(target);
					return names;
				}
			}

			return null;
		}

		public override void ApplyAction (INodeChanger nodeChanger)
		{
			throw new NotImplementedException();
		}

		public override Dictionary<Node, Dictionary<string, string>> GetAllTargetsAndAttributes (NodeTree model)
		{
			var nodeToAttributeToValue = new Dictionary<Node, Dictionary<string, string>> ();
			var node = model.GetNamedNode(target);

			if (node != null)
			{
				nodeToAttributeToValue.Add(node, new Dictionary<string, string> ());

				foreach (AttributeValuePair avp in valuesToApply)
				{
					nodeToAttributeToValue[node].Add(avp.Attribute, avp.Value);
				}
			}

			return nodeToAttributeToValue;
		}
	}
}