using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;

using LibCore;
using Network;

namespace IncidentManagement
{
	public struct AttributeIntPair
	{
		public string Attribute;
		public int Integer;
	}
	/// <summary>
	/// Summary description for IDef_IncrementAt.
	/// </summary>
	public class ModelAction_IncrementAttribute : TargetedModelAction
	{
		// Store the attributes to increment on the target node
		// along with the amount to increment by.
		protected ArrayList valuesToIncrement = new ArrayList();

		bool useZoneOf = false;
		string useZoneName = "";

		public override object Clone ()
		{
			ModelAction_IncrementAttribute inc = new ModelAction_IncrementAttribute();
			inc.target = this.target;
			inc.doAfterSecs = this.doAfterSecs;
			foreach (AttributeValuePair avp in this.valuesToIncrement)
			{
				inc.valuesToIncrement.Add(avp.Clone());
			}
			return inc;
		}

		public override Dictionary<Node, Dictionary<string, string>> GetAllTargetsAndAttributes (NodeTree model)
		{
			var results = new Dictionary<Node, Dictionary<string, string>> ();
			var node = model.GetNamedNode(target);

			if (node != null)
			{
				results.Add(node, new Dictionary<string, string> ());

				foreach (AttributeValuePair avp in valuesToIncrement)
				{
					results[node][avp.Attribute] = avp.Value;
				}
			}

			return results;
		}

		protected ModelAction_IncrementAttribute ()
		{
		}

		public ModelAction_IncrementAttribute (XmlNode n)
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
					else if (a.Name == "i_zoneOf")
					{
						useZoneName = a.Value;
						useZoneOf = true;
					}
					else if (a.Name == "i_doAfterSecs")
					{
						doAfterSecs = CONVERT.ParseInt(a.Value);
					}
				}
				else
				{
					// This is an attribute to apply.
					AttributeIntPair aip = new AttributeIntPair ();
					aip.Attribute = a.Name;
					aip.Integer = CONVERT.ParseInt(a.Value);
					valuesToIncrement.Add(aip);
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
			if (useZoneOf)
			{
                var oldZoneName = new List<AttributeValuePair> { new AttributeValuePair("target", useZoneName) };
                var newZoneName = Node.RemapSpecialAttributes(nt, oldZoneName);

                var newZone = newZoneName[0].Value;

				Node zoneOfTarget = nt.GetNamedNode(newZone);
				if (zoneOfTarget == null)
				{
					// Must already be in use: look for it as a location rather than a name.
					ArrayList nodes = nt.GetNodesWithAttributeValue("location", useZoneName);

					// Find a non-project node.
					foreach (Node node in nodes)
					{
						if (node.Type.ToLower() == "node")
						{
							zoneOfTarget = node;
						}
					}
				}
				if (zoneOfTarget != null)
				{
					string zone = zoneOfTarget.GetAttribute("zone");
					if (zone == "")
					{
						zone = zoneOfTarget.GetAttribute("proczone");
					}

					target = "Zone" + zone;
				}
			}

			Node n = nt.GetNamedNode(target);
			foreach (AttributeIntPair aip in valuesToIncrement)
			{
				string val = n.GetAttribute(aip.Attribute);
				int ival = CONVERT.ParseInt(val);
				ival += aip.Integer;
				n.SetAttribute(aip.Attribute, ival);
			}
		}

		public override void ApplyAction (INodeChanger nodeChanger)
		{
			throw new NotImplementedException();
		}
	}
}