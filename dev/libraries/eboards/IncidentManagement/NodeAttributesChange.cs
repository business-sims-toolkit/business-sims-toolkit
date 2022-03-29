using System.Collections.Generic;
using System.Text;

using Network;

namespace IncidentManagement
{
	public class NodeAttributesChange : INodeChangeOperation
	{
		Node node;
		List<AttributeValuePair> attributes;

		public NodeAttributesChange (Node node, AttributeValuePair attributeValuePair)
		{
			this.node = node;
			attributes = new List<AttributeValuePair> ();
			attributes.Add(attributeValuePair);
		}

		public NodeAttributesChange (Node node, List<AttributeValuePair> attributes)
		{
			this.node = node;
			this.attributes = new List<AttributeValuePair> (attributes);
		}

		public void Apply ()
		{
			node.SetAttributes(attributes);
		}

		public override string ToString ()
		{
			StringBuilder builder = new StringBuilder ();
			builder.AppendFormat("Apply '{0}' ", node.GetAttribute("name"));

			bool first = true;
			foreach (AttributeValuePair avp in attributes)
			{
				if (first)
				{
					builder.Append(", ");
					first = false;
				}
				else
				{
					builder.Append(" ");
				}

				builder.AppendFormat("{0}='{1}'", avp.Attribute, avp.Value);
			}

			return builder.ToString();
		}
	}
}